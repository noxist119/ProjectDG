using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
    public sealed class DailyFortuneRule
    {
        public string title;
        public string summary;
        public float epicSummonChanceBonus;
        public float bossHealthBonus;
        public float shopDiscountRate;
        public int startGoldBonus;
        public int lifeRecoveryBonus;

        public float BossHealthMultiplier => 1f + Mathf.Max(0f, bossHealthBonus);
        public float ShopCostMultiplier => Mathf.Clamp01(1f - Mathf.Max(0f, shopDiscountRate));
    }

    public static class DailyFortuneSystem
    {
        private static DailyFortuneRule cachedRule;
        private static string cachedDateKey;

        private static readonly DailyFortuneRule[] Rules =
        {
            new DailyFortuneRule
            {
                title = "합성 예감",
                summary = "Epic 소환률 +5%, 보스 체력 +8%",
                epicSummonChanceBonus = 0.05f,
                bossHealthBonus = 0.08f
            },
            new DailyFortuneRule
            {
                title = "보급 장날",
                summary = "전투 상점 가격 -15%, 보스 체력 +5%",
                shopDiscountRate = 0.15f,
                bossHealthBonus = 0.05f
            },
            new DailyFortuneRule
            {
                title = "초반 순풍",
                summary = "시작 골드 +8, 보스 체력 +6%",
                startGoldBonus = 8,
                bossHealthBonus = 0.06f
            },
            new DailyFortuneRule
            {
                title = "회복의 날",
                summary = "회복 상품 생명력 +1, 보스 체력 +4%",
                lifeRecoveryBonus = 1,
                bossHealthBonus = 0.04f
            },
            new DailyFortuneRule
            {
                title = "대박 기류",
                summary = "Epic 소환률 +3%, 상점 가격 -8%",
                epicSummonChanceBonus = 0.03f,
                shopDiscountRate = 0.08f
            }
        };

        public static DailyFortuneRule Today
        {
            get
            {
                string dateKey = System.DateTime.Now.ToString("yyyyMMdd");
                if (cachedRule == null || cachedDateKey != dateKey)
                {
                    cachedDateKey = dateKey;
                    cachedRule = ResolveRule(System.DateTime.Now);
                }

                return cachedRule;
            }
        }

        public static string TodaySummary
        {
            get
            {
                DailyFortuneRule rule = Today;
                return rule != null ? "오늘의 운세: " + rule.title + " / " + rule.summary : "오늘의 운세 준비 중";
            }
        }

        private static DailyFortuneRule ResolveRule(System.DateTime date)
        {
            if (Rules == null || Rules.Length == 0)
            {
                return new DailyFortuneRule { title = "기본 운세", summary = "특별 규칙 없음" };
            }

            int seed = date.Year * 10000 + date.Month * 100 + date.Day;
            int index = Mathf.Abs(seed * 31 + date.DayOfYear * 17) % Rules.Length;
            return Rules[index];
        }
    }

    public class DefenseGameController : MonoBehaviour
    {
        private const string EarlyRunTuningLogPrefsKey = "DefenseGame.EarlyRunTuningLog.v1";
        private const int EarlyRunTuningLogMaxEntries = 60;
        private const int EarlyRunRequiredRoundCount = 10;
        private const int RunClipMaxEvents = 6;

        public static DefenseGameController Active { get; private set; }

        [Header("Core References")]
        [SerializeField] private CharacterDatabase characterDatabase;
        [SerializeField] private MonsterDatabase monsterDatabase;
        [SerializeField] private DefenseBoardManager boardManager;
        [SerializeField] private RoundManager roundManager;
        [SerializeField] private DefenderUnit defaultUnitPrefab;

        [Header("Economy")]
        [SerializeField] private int startGold = 30;
        [SerializeField] private int summonCost = 10;
        [SerializeField] private int summonCostIncreasePerSummon = 1;
        [SerializeField] private int earlySummonCostRampRoundLimit = 5;
        [SerializeField] private int earlySummonCostIncreasePerSummon = 0;
        [SerializeField] private int maxSummonCost = 80;
        [SerializeField] private int life = 10;
        [SerializeField] private int roundStartGold = 3;
        [SerializeField] [Range(0f, 2f)] private float roundStartGoldPerRoundMultiplier = 0.7f;
        [SerializeField] private int roundClearBaseGold = 7;
        [SerializeField] private int roundClearPerRoundGold = 1;
        [SerializeField] private int victoryStreakGoldBonus = 1;

        [Header("Unit Selling")]
        [SerializeField] private bool enableUnitSelling = true;
        [SerializeField] [Range(0.1f, 0.5f)] private float unitSellRefundRate = 0.33f;
        [SerializeField] private int normalUnitSellBaseValue = 10;
        [SerializeField] private int rareUnitSellBaseValue = 16;
        [SerializeField] private int epicUnitSellBaseValue = 25;
        [SerializeField] private int legendaryUnitSellBaseValue = 40;
        [SerializeField] private int mythicUnitSellBaseValue = 64;
        [SerializeField] private int transcendentUnitSellBaseValue = 100;

        [Header("Early Run Fun Pacing")]
        [SerializeField] private bool enableEarlyRunFunPacing = true;
        [SerializeField] private int earlyFunRoundLimit = 5;
        [SerializeField] private int earlyPitySummonCount = 2;
        [SerializeField] private CharacterGrade earlyPityMinimumGrade = CharacterGrade.Rare;
        [SerializeField] [Range(0f, 1f)] private float earlyPityEpicChance = 0.12f;
        [SerializeField] private int earlyFallbackRewardRound = 3;
        [SerializeField] private CharacterGrade earlyFallbackRewardGrade = CharacterGrade.Rare;
        [SerializeField] private int earlyFallbackGoldReward = 22;
        [SerializeField] private int earlyCrisisRound = 5;
        [SerializeField] private int earlyBossPrepRewardRound = 4;
        [SerializeField] private int earlyBossPrepGoldReward = 18;
        [SerializeField] private bool enableBadLuckInsurance = true;
        [SerializeField] private int badLuckInsuranceSummonThreshold = 4;
        [SerializeField] private int badLuckInsuranceLastSummon = 6;
        [SerializeField] private int badLuckInsuranceEarliestRound = 3;
        [SerializeField] private CharacterGrade badLuckInsuranceMinimumGoodGrade = CharacterGrade.Rare;
        [SerializeField] private bool enableFirstBossSummonRushBonus = true;
        [SerializeField] private int firstBossSummonRushRound = 10;
        [SerializeField] private int firstBossSummonRushMinSummons = 34;
        [SerializeField] private int firstBossSummonRushMinMerges = 15;
        [SerializeField] [Range(0f, 0.5f)] private float firstBossSummonRushAttackBonus = 0.06f;
        [SerializeField] [Range(0f, 0.8f)] private float firstBossSummonRushBossDamageBonus = 0.10f;
        [SerializeField] [Range(0f, 1f)] private float firstBossSummonRushMaxBossDamageBonus = 0.18f;

        [Header("Combat Readability")]
        [SerializeField] private float tileContributionBannerMinDamage = 180f;
        [SerializeField] private float bossTileContributionBannerMinDamage = 80f;
        [SerializeField] private float tileContributionFeedbackStep = 250f;
        [SerializeField] private float bossTileContributionFeedbackStep = 120f;
        [SerializeField] private float combatFeedbackCooldown = 2.0f;
        [SerializeField] private float topDamageFeedbackStep = 700f;

        [Header("Early Run Telemetry")]
        [SerializeField] private bool enableEarlyRoundTelemetry = true;
        [SerializeField] private int earlyTelemetryRoundLimit = 10;
        [SerializeField] private float slowEarlyClearSeconds = 54f;
        [SerializeField] private int lowEarlyGoldThreshold = 14;
        [SerializeField] private int lowEarlySummonThreshold = 2;
        [SerializeField] private int earlyTelemetryTargetSampleCount = 20;
        [SerializeField] [Range(0f, 1f)] private float highBossHealthWarningRatio = 0.30f;
        [SerializeField] [Range(0.1f, 1f)] private float earlyLowLifeRecoveryRatio = 0.50f;

        [Header("Fate Intervention")]
        [SerializeField] private bool enableFateIntervention = true;
        [SerializeField] private int maxFateGauge = 100;
        [SerializeField] private int startingFateGauge = 42;
        [SerializeField] private int maxFateDebt = 100;
        [SerializeField] private int fateGaugeOnLowSummon = 10;
        [SerializeField] private int fateGaugeOnRoundClear = 10;
        [SerializeField] private int fateGaugeOnBossKill = 18;
        [SerializeField] private int fateGaugeOnLowLife = 28;
        [SerializeField] private int fateDebtPerContractLife = 14;
        [SerializeField] private int fateDebtRepayPerRound = 10;
        [SerializeField] private int fateDebtRepayPerBossRound = 10;
        [SerializeField] private int fateShopRerollGaugeCost = 16;
        [SerializeField] private int fateGradeLockGaugeCost = 18;
        [SerializeField] private int fateNormalBanGaugeCost = 16;
        [SerializeField] private int fateForceShopGaugeCost = 14;
        [SerializeField] private int fateSurvivalGaugeCost = 20;
        [SerializeField] private int fateShopRerollDebt = 5;
        [SerializeField] private int fateGradeLockDebt = 10;
        [SerializeField] private int fateNormalBanDebt = 8;
        [SerializeField] private int fateForceShopDebt = 8;
        [SerializeField] private int fateSurvivalDebt = 18;
        [SerializeField] private int fateSurvivalLifeRecover = 4;
        [SerializeField] private int fateSurvivalGold = 12;
        [SerializeField] private int fateSurvivalNormalBanSummons = 3;
        [SerializeField] private int ultimateRecipeBingoFateGaugeBonus = 20;
        [SerializeField] [Range(0f, 0.5f)] private float maxFateDebtShopCostPenalty = 0.08f;
        [SerializeField] [Range(0f, 0.5f)] private float maxFateDebtBossHealthBonus = 0.10f;

        private int maxLife;
        private int currentSummonBaseCost;
        private int roundGoldBonus;
        private int victoryStreak;
        private float summonCostDiscountRate;
        private int currentRoundResolvedMonsters;
        private bool gameOverRaised;
        private Coroutine defeatFinalizeRoutine;
        private float defeatPreviousTimeScale = 1f;
        private float defeatPreviousFixedDeltaTime = 0.02f;
        private bool defeatTimeScaleCaptured;

        public const float DefeatSlowMotionDurationRealtime = 5f;
        public const float DefeatSlowMotionTargetScale = 0.10f;
        private const float DefeatFinalizePaddingRealtime = 0.10f;
        public static bool IsDefeatSlowMotionActive { get; private set; }
        private readonly Dictionary<string, float> damageByHero = new Dictionary<string, float>();
        private readonly Dictionary<string, float> currentRoundDamageByHero = new Dictionary<string, float>();
        private string topDamageHeroName = "없음";
        private float topDamageHeroDamage;
        private string roundTopDamageHeroName = "없음";
        private float roundTopDamageHeroDamage;
        private float totalDamageDealt;
        private int criticalHitCount;
        private int currentKillCombo;
        private int bestKillCombo;
        private float lastKillTime;
        private int bestSynergyCount;
        private string bestSynergyTitle = "시너지 없음";
        private int currentSynergyCount;
        private string currentSynergyTitle = "시너지 없음";
        private int earnedGrowthCurrency;
        private float nextCriticalBannerTime;
        private int earlySummonAttempts;
        private bool earlyRunMomentTriggered;
        private bool earlyFallbackRewardGranted;
        private bool earlyBossPrepRewardGranted;
        private int earlyBadLuckSummons;
        private CharacterGrade earlyBadLuckBestGrade = CharacterGrade.Normal;
        private bool badLuckInsuranceOfferPending;
        private bool badLuckInsuranceOffered;
        private bool badLuckInsuranceResolved;
        private string badLuckInsuranceReason = "초반 소환 보험 대기";
        private float currentRoundTileDamage;
        private float currentRoundBossTileDamage;
        private float totalBossTileDamage;
        private readonly Dictionary<BoardTileModifierType, float> currentRoundTileDamageByType = new Dictionary<BoardTileModifierType, float>();
        private int currentRoundBossSkillCasts;
        private int totalBossKills;
        private int currentRoundBossAffectedTargets;
        private int currentRoundBossGoldDrained;
        private int currentRoundBossManaBurnTargets;
        private int currentRoundBossExecutions;
        private int currentRoundBossFortifyCount;
        private int currentRoundBossRallyTargets;
        private float currentRoundBossSkillDamage;
        private string currentRoundLastBossSkill = "없음";
        private int totalBossSkillCasts;
        private int totalBossAffectedTargets;
        private int totalBossGoldDrained;
        private int totalBossManaBurnTargets;
        private int totalBossExecutions;
        private int totalBossFortifyCount;
        private int totalBossRallyTargets;
        private float totalBossSkillDamage;
        private string lastBossSkill = "없음";
        private readonly List<EarlyRoundTelemetrySnapshot> earlyRoundTelemetry = new List<EarlyRoundTelemetrySnapshot>();
        private float currentRoundStartTime;
        private int currentRoundStartGold;
        private int currentRoundSummonCount;
        private int currentRoundMergeCount;
        private bool currentRoundHadMerge;
        private CharacterGrade currentRoundHighestMergeGrade = CharacterGrade.Normal;
        private int pendingRoundSummonCount;
        private int pendingRoundMergeCount;
        private bool pendingRoundHadMerge;
        private CharacterGrade pendingRoundHighestMergeGrade = CharacterGrade.Normal;
        private int runMergeCount;
        private bool firstBossSummonRushBonusGranted;
        private string earlyRunTelemetrySummary = "1~10R 계측 대기";
        private string earlyRunTuningHint = "초반 런 데이터 대기";
        private bool earlyRunRecoveryRecommended;
        private string earlyRunRecoveryReason = "초반 런 안정";
        private string earlyRunRecoveryCause = "흐름 안정";
        private int earlyRunRecoveryOfferCount;
        private int earlyRunR3BoosterOfferCount;
        private int earlyRunR3BoosterPurchaseCount;
        private int earlyRunRecoveryShopOfferCount;
        private int earlyRunRecoveryShopPurchaseCount;
        private float earlyRunR10BossHealthRemaining01 = -1f;
        private string earlyRunLogCoverageSummary = "R1~R10 로그 0/20";
        private EarlyRunTuningLogStore earlyRunTuningLogStore;
        private bool earlyRunTuningLogRecorded;
        private bool runR3BoosterOffered;
        private bool runR3BoosterPurchased;
        private bool runRecoveryShopOffered;
        private bool runRecoveryShopPurchased;
        private bool runInsuranceOffered;
        private bool runInsuranceClaimed;
        private float runR10BossHealthRemaining01 = -1f;
        private int firstRarePlusRound = -1;
        private int firstMergeRound = -1;
        private readonly List<string> runHighlightCards = new List<string>();
        private float nextTileFeedbackTime;
        private float nextBossTileFeedbackTime;
        private float nextSynergyFeedbackTime;
        private float nextTopDamageFeedbackTime;
        private float nextTopDamageFeedbackThreshold;
        private int nextTileFeedbackDamageThreshold;
        private int nextBossTileFeedbackDamageThreshold;
        private int fateGauge;
        private int fateDebt;
        private int fateInterventionCount;
        private int fateContractCount;
        private int runFateInterventionCount;
        private int runFateContractCount;
        private int runFateShopRerollCount;
        private int runFateGradeLockCount;
        private int runFateNormalBanCount;
        private int runFateForcedShopCount;
        private int runFateSurvivalCount;
        private int runFateDebtAdded;
        private int runFateDebtRepaid;
        private int runFateShopCostPenaltyGold;
        private int runPeakFateDebt;
        private int fateGradeLockSummonsRemaining;
        private CharacterGrade fateGradeLockMinimum = CharacterGrade.Normal;
        private int fateNormalBanSummonsRemaining;
        private bool fateForceNextShop;
        private bool firstLegendaryMergeRecorded;
        private bool lifeOneClutchRecorded;
        private bool fateSurvivalClutchRecorded;
        private bool runDefeatMomentRecorded;
        private readonly List<string> runClipEvents = new List<string>();
        private readonly HashSet<string> rewardedUltimateRecipeNames = new HashSet<string>();

        private sealed class EarlyRoundTelemetrySnapshot
        {
            public int round;
            public bool cleared;
            public bool bossRound;
            public float clearTimeSeconds;
            public int startGold;
            public int endGold;
            public int endLife;
            public float endLife01;
            public int summons;
            public int merges;
            public bool hadMerge;
            public CharacterGrade highestMergeGrade;
            public float bossHealthRemaining01;
        }

        [System.Serializable]
        private sealed class EarlyRunTuningLogEntry
        {
            public long ticksUtc;
            public int reachedRound;
            public bool reachedRound10;
            public bool clearedRound10;
            public int firstRarePlusRound;
            public int firstMergeRound;
            public bool insuranceOffered;
            public bool insuranceClaimed;
            public bool r3BoosterOffered;
            public bool r3BoosterPurchased;
            public bool recoveryShopOffered;
            public bool recoveryShopPurchased;
            public bool fateContractUsed;
            public bool fateInterventionUsed;
            public int fateDebt;
            public float r10BossHealthRemaining01;
            public int endLife;
            public int endGold;
            public int boardUnits;
            public int bossKills;
            public int runScore;
            public string recommendedBuildName;
        }

        [System.Serializable]
        private sealed class EarlyRunTuningLogStore
        {
            public List<EarlyRunTuningLogEntry> entries = new List<EarlyRunTuningLogEntry>();
        }

        public event System.Action OnStateChanged;
        public event System.Action<MergeResultInfo> OnMergeCompleted;
        public event System.Action<CharacterDefinition> OnUnitSummoned;
        public event System.Action<int> OnRoundCountdownChanged;
        public event System.Action<int> OnRoundStarted;
        public event System.Action<int> OnRoundMissionSettlement;
        public event System.Action<int> OnRoundEconomySettlement;
        public event System.Action<int> OnRoundBoardPreparation;
        public event System.Action<int> OnRoundShopPhase;
        public event System.Action<int> OnRoundAugmentChoicePhase;
        public event System.Action<int> OnRoundCompleted;
        public event System.Action OnGameOver;
        public event System.Action OnBadLuckInsuranceOffered;
        public event System.Action<string, Color, float> OnBannerRequested;

        public int Gold { get; private set; }
        public int Life => life;
        public int MaxLife => maxLife > 0 ? maxLife : life;
        public string LifeHudSummary => "HP " + Life + "/" + MaxLife;
        public int SummonCost => ResolveSummonCost();
        public int CurrentRound => roundManager != null ? roundManager.CurrentRound : 0;
        public bool IsRoundRunning => roundManager != null && roundManager.IsRoundRunning;
        public bool IsCombatInteractionLocked => IsRoundRunning && CurrentRound > 0 && roundManager != null && roundManager.CurrentRoundSpawnedCount > 0;
        public bool IsBossRound => roundManager != null && roundManager.IsBossRound;
        public int NextBossRound => roundManager != null ? roundManager.GetNextBossRound(CurrentRound) : 10;
        public int RoundsUntilNextBoss => Mathf.Max(0, NextBossRound - CurrentRound);
        public int BoardUnitCount => boardManager != null ? boardManager.UnitCount : 0;
        public int BoardCapacity => boardManager != null ? boardManager.UnlockedSlotCount : 0;
        public int EmptySlotCount => boardManager != null ? boardManager.EmptySlotCount : 0;
        public int CharacterCount => characterDatabase != null ? characterDatabase.Characters.Count : 0;
        public int MonsterCount => monsterDatabase != null ? monsterDatabase.Monsters.Count : 0;
        public int RoundTargetCount => roundManager != null ? roundManager.CurrentRoundTargetCount : 0;
        public int RoundResolvedMonsterCount => currentRoundResolvedMonsters;
        public float RoundProgress01 => RoundTargetCount <= 0
            ? CurrentRound > 0 && !IsRoundRunning ? 1f : 0f
            : Mathf.Clamp01((float)currentRoundResolvedMonsters / RoundTargetCount);
        public string CurrentStateSummary => "Gold " + Gold + " | Life " + Life + " | Round " + CurrentRound + (IsBossRound ? " Boss" : string.Empty);
        public int LastRoundClearGoldReward { get; private set; }
        public MergeResultInfo? LastMergeResult { get; private set; }
        public string BestSynergySummary => bestSynergyCount > 0 ? bestSynergyTitle + " (" + bestSynergyCount + "개 활성)" : "활성 시너지 없음";
        public string CurrentSynergySummary => currentSynergyCount > 0 ? currentSynergyTitle + " x" + currentSynergyCount : "시너지 없음";
        public string TopDamageSummary => topDamageHeroDamage > 0f ? topDamageHeroName + "  " + Mathf.RoundToInt(topDamageHeroDamage).ToString("N0") : "기록 없음";
        public string RoundTopDamageSummary => roundTopDamageHeroDamage > 0f ? roundTopDamageHeroName + "  " + Mathf.RoundToInt(roundTopDamageHeroDamage).ToString("N0") : "기록 없음";
        public string DamageLeaderboardSummary => BuildDamageLeaderboardSummary(damageByHero, 3);
        public string RoundDamageLeaderboardSummary => BuildDamageLeaderboardSummary(currentRoundDamageByHero, 3);
        public string CurrentTileContributionSummary => BuildCurrentTileContributionSummary();
        public string CurrentBossPressureSummary => BuildBossPressureSummary(false);
        public string BossPressureSummary => BuildBossPressureSummary(true);
        public string CurrentBuildGoalSummary => ComposeBuildGoalGuideSummary();
        public string CurrentDangerSummary => BuildCurrentDangerSummary();
        public string EarlyRunTelemetrySummary => earlyRunTelemetrySummary;
        public string EarlyRunTuningLoopSummary => earlyRunTelemetrySummary + " / " + earlyRunLogCoverageSummary + " / 회복상점 " + earlyRunRecoveryOfferCount + "회";
        public string EarlyRunTuningHint => earlyRunTuningHint;
        public bool EarlyRunRecoveryRecommended => earlyRunRecoveryRecommended;
        public string EarlyRunRecoveryReason => earlyRunRecoveryReason;
        public string EarlyRunRecoveryCause => earlyRunRecoveryCause;
        public string EarlyRunLogCoverageSummary => earlyRunLogCoverageSummary;
        public string EarlyRunActionSummary => BuildEarlyRunActionSummary();
        public bool BadLuckInsuranceAvailable => badLuckInsuranceOfferPending;
        public string BadLuckInsuranceReason => badLuckInsuranceReason;
        public string RecommendedDeckSummary => BuildRecommendedDeckSummary();
        public string RecommendedBuildName => BuildRecommendedBuildName();
        public string RunNextGoalHeadline => BuildRunNextGoalHeadline();
        public int EarnedGrowthCurrency => earnedGrowthCurrency;
        public int BestKillCombo => bestKillCombo;
        public int CriticalHitCount => criticalHitCount;
        public float TotalDamageDealt => totalDamageDealt;
        public int RunBossKillCount => totalBossKills;
        public int RunBossScore => CalculateRunBossScore();
        public string RunMvpName => topDamageHeroDamage > 0f ? topDamageHeroName : "MVP 대기";
        public int RunPerformanceScore => CalculateRunPerformanceScore();
        public string RunPerformanceGrade => ResolveRunPerformanceGrade(RunPerformanceScore);
        public string RunResultRecapSummary => BuildRunResultRecapSummary();
        public string RunResultFocusSummary => BuildRunResultFocusSummary();
        public string RunResultNextCompactSummary => BuildRunResultNextCompactSummary();
        public string RunNextActionSummary => BuildRunNextActionSummary();
        public string RunHighlightCardsSummary => BuildRunHighlightCardsSummary();
        public string DailyFortuneSummary => DailyFortuneSystem.TodaySummary;
        public string FateInterventionSummary => BuildReadableFateInterventionSummary();
        public string FateResultSummary => BuildReadableFateResultSummary();
        public string FateCostBenefitSummary => BuildReadableFateCostBenefitSummary();
        public float FateDebtBossHealthMultiplier => ResolveFateDebtBossHealthMultiplier();
        public int FateGauge => fateGauge;
        public int MaxFateGauge => Mathf.Max(1, maxFateGauge);
        public int FateDebt => fateDebt;
        public int MaxFateDebt => Mathf.Max(1, maxFateDebt);
        public float FateGauge01 => enableFateIntervention ? Mathf.Clamp01((float)fateGauge / MaxFateGauge) : 0f;
        public string FateHudSummary => BuildReadableFateHudSummary();
        public string FateGradeLockHudLabel => BuildFateActionLabel("Rare+", fateGradeLockGaugeCost, fateGradeLockDebt);
        public string FateNormalBanHudLabel => BuildFateActionLabel("No Normal", fateNormalBanGaugeCost, fateNormalBanDebt);
        public string FateForceShopHudLabel => BuildFateActionLabel("Force Shop", fateForceShopGaugeCost, fateForceShopDebt);
        public bool FateSurvivalCrisisActive => IsFateSurvivalCrisisActive();
        public string FateSurvivalHudLabel => FateSurvivalCrisisActive
            ? "빚지고 살기\n지금 " + Mathf.Max(0, fateSurvivalGaugeCost) + "F/+" + Mathf.Max(0, fateSurvivalDebt)
            : "빚지고 살기\n" + Mathf.Max(0, fateSurvivalGaugeCost) + "F / +" + Mathf.Max(0, fateSurvivalDebt);
        public bool CanUseFateGradeLock => CanSpendFateGauge(fateGradeLockGaugeCost);
        public bool CanUseFateNormalBan => CanSpendFateGauge(fateNormalBanGaugeCost);
        public bool CanUseFateForcedShop => CanSpendFateGauge(fateForceShopGaugeCost);
        public bool CanUseFateSurvival => CanSpendFateGauge(fateSurvivalGaugeCost) && (CurrentRound >= 4 || Life < MaxLife);
        public string EarlyRunTuningDecisionSummary => BuildEarlyRunTuningDecisionSummary();
        public int EarlyRunTuningSampleCount => GetEarlyRunLogSampleCount();
        public int EarlyRunTuningTargetSampleCount => Mathf.Max(1, earlyTelemetryTargetSampleCount);
        public string SeasonReplayDigestSummary => BuildSeasonReplayDigestSummary();

        private void Awake()
        {
            Active = this;
            if (maxLife <= 0)
            {
                maxLife = life;
            }

            if (currentSummonBaseCost <= 0)
            {
                currentSummonBaseCost = summonCost;
            }

            gameOverRaised = false;
            LoadEarlyRunTuningLogStore();
            ResetRunStats();

            if (characterDatabase == null) characterDatabase = GetComponent<CharacterDatabase>();
            if (monsterDatabase == null) monsterDatabase = GetComponent<MonsterDatabase>();
            if (boardManager == null) boardManager = GetComponent<DefenseBoardManager>();
            if (roundManager == null) roundManager = GetComponent<RoundManager>();
        }

        private void OnEnable()
        {
            MonsterUnit.OnMonsterKilled += HandleMonsterKilled;
            MonsterUnit.OnMonsterEscaped += HandleMonsterEscaped;
            DefenderUnit.OnDamageDealt += HandleDamageDealt;
            DefenderUnit.OnDefenderRemoved += HandleDefenderRemoved;
            SubscribeRoundManager();
        }

        private void OnDisable()
        {
            CancelPendingDefeatFinalization();
            MonsterUnit.OnMonsterKilled -= HandleMonsterKilled;
            MonsterUnit.OnMonsterEscaped -= HandleMonsterEscaped;
            DefenderUnit.OnDamageDealt -= HandleDamageDealt;
            DefenderUnit.OnDefenderRemoved -= HandleDefenderRemoved;
            UnsubscribeRoundManager();
            if (Active == this)
            {
                Active = null;
            }
        }

        private void Start()
        {
            if (Gold <= 0)
            {
                Gold = ResolveStartGold();
            }

            NotifyStateChanged();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                TriggerDebugDefeat();
            }
        }

        public void TriggerDebugDefeat()
        {
            if (gameOverRaised)
            {
                return;
            }

            life = 0;
            TriggerLeakDefeat();
        }
#endif

        public void IncreaseMaxLife(int amount, bool healIncrease = true)
        {
            int appliedAmount = Mathf.Max(0, amount);
            if (appliedAmount <= 0)
            {
                return;
            }

            maxLife = Mathf.Max(1, MaxLife + appliedAmount);
            if (healIncrease)
            {
                life = Mathf.Min(maxLife, life + appliedAmount);
            }

            NotifyStateChanged();
        }

        public void Configure(CharacterDatabase characters, MonsterDatabase monsters, DefenseBoardManager board, RoundManager rounds, DefenderUnit fallbackUnit)
        {
            UnsubscribeRoundManager();
            characterDatabase = characters;
            monsterDatabase = monsters;
            boardManager = board;
            roundManager = rounds;
            defaultUnitPrefab = fallbackUnit;
            SubscribeRoundManager();
            if (maxLife <= 0)
            {
                maxLife = life;
            }

            if (currentSummonBaseCost <= 0)
            {
                currentSummonBaseCost = summonCost;
            }

            gameOverRaised = false;
            boardManager?.RefreshSlotLocks(CurrentRound);
            ResetRunStats();

            if (Gold <= 0)
            {
                Gold = ResolveStartGold();
            }
            NotifyStateChanged();
        }

        public bool TrySummon()
        {
            if (IsCombatInteractionLocked)
            {
                return false;
            }

            int cost = SummonCost;
            if (Gold < cost || characterDatabase == null || boardManager == null)
            {
                return false;
            }

            CharacterDefinition summon = SelectSummonDefinition(out bool earlyPitySummon);
            if (summon == null)
            {
                return false;
            }

            bool spawned = boardManager.TrySpawnUnit(summon, defaultUnitPrefab, out DefenderUnit spawnedUnit);
            if (!spawned)
            {
                return false;
            }

            Gold -= cost;
            currentSummonBaseCost = Mathf.Min(maxSummonCost, currentSummonBaseCost + ResolveSummonCostIncrease());
            if (summon.grade != CharacterGrade.Transcendent)
            {
                RuntimeAudioUtility.PlayDiceAppear();
            }

            RegisterSummonExcitement(summon, earlyPitySummon, spawnedUnit);
            RecordEarlyRoundSummon(summon);
            ResolveUltimateRecipeBingoReward();
            OnUnitSummoned?.Invoke(summon);
            NotifyStateChanged();
            return true;
        }

        public void ClearBoardForProfileChange()
        {
            if (IsRoundRunning || boardManager == null)
            {
                return;
            }

            boardManager.ClearAllDeployedUnits();
            NotifyStateChanged();
        }

        public void ResetRunForRetry()
        {
            CancelPendingDefeatFinalization();
            if (roundManager != null)
            {
                roundManager.ResetRunState();
            }

            if (boardManager != null)
            {
                boardManager.ClearAllDeployedUnits();
                boardManager.RefreshSlotLocks(0);
            }

            Gold = ResolveStartGold();
            life = MaxLife;
            currentSummonBaseCost = summonCost;
            summonCostDiscountRate = 0f;
            roundGoldBonus = 0;
            victoryStreak = 0;
            currentRoundResolvedMonsters = 0;
            LastRoundClearGoldReward = 0;
            LastMergeResult = null;
            gameOverRaised = false;
            ResetRunStats();
            OnBannerRequested?.Invoke("새 판 준비", new Color(0.52f, 0.82f, 1f), 1.6f);
            NotifyStateChanged();
        }

        public void ExitToOutgame()
        {
            CancelPendingDefeatFinalization();
            if (roundManager != null)
            {
                roundManager.ResetRunState();
            }

            if (boardManager != null)
            {
                boardManager.ClearAllDeployedUnits();
                boardManager.RefreshSlotLocks(0);
            }

            Gold = ResolveStartGold();
            life = MaxLife;
            currentSummonBaseCost = summonCost;
            summonCostDiscountRate = 0f;
            roundGoldBonus = 0;
            victoryStreak = 0;
            currentRoundResolvedMonsters = 0;
            LastRoundClearGoldReward = 0;
            LastMergeResult = null;
            gameOverRaised = false;
            ResetRunStats();
            OnBannerRequested?.Invoke("아웃게임으로 이동", new Color(0.52f, 0.82f, 1f), 1.8f);
            NotifyStateChanged();
        }

        private int ResolveStartGold()
        {
            DailyFortuneRule fortune = DailyFortuneSystem.Today;
            return Mathf.Max(0, startGold + (fortune != null ? Mathf.Max(0, fortune.startGoldBonus) : 0));
        }

        public bool TryMerge(CharacterGrade grade)
        {
            if (IsCombatInteractionLocked)
            {
                return false;
            }

            if (boardManager == null || characterDatabase == null)
            {
                return false;
            }

            bool merged = boardManager.TryMergeUnitsOfGrade(grade, characterDatabase, out MergeResultInfo mergeResult, defaultUnitPrefab);
            return FinalizeMergeResult(merged, mergeResult);
        }

        public bool TryMergeUltimateRecipe(string recipeName)
        {
            if (IsCombatInteractionLocked || boardManager == null || characterDatabase == null)
            {
                return false;
            }

            bool merged = boardManager.TryMergeUltimateRecipe(recipeName, characterDatabase, out MergeResultInfo mergeResult, defaultUnitPrefab);
            return FinalizeMergeResult(merged, mergeResult);
        }

        private bool FinalizeMergeResult(bool merged, MergeResultInfo mergeResult)
        {
            if (!merged)
            {
                return false;
            }

            LastMergeResult = mergeResult;
            if ((int)mergeResult.resultGrade < (int)CharacterGrade.Rare)
            {
                RuntimeAudioUtility.PlayReroll();
            }

            RegisterMergeExcitement(mergeResult);
            RecordEarlyRoundMerge(mergeResult.resultGrade);
            ResolveUltimateRecipeBingoReward();
            OnMergeCompleted?.Invoke(mergeResult);
            NotifyStateChanged();
            return true;
        }

        public void StartRound()
        {
            if (roundManager == null)
            {
                return;
            }

            Gold += CalculateRoundStartGold();
            RuntimeAudioUtility.PlayBattleStart();
            roundManager.StartNextRound();
            NotifyStateChanged();
        }


        private int CalculateRoundStartGold()
        {
            int round = Mathf.Max(0, CurrentRound);
            int roundIncome = Mathf.FloorToInt(round * Mathf.Max(0f, roundStartGoldPerRoundMultiplier));
            return Mathf.Max(0, roundStartGold + roundIncome + roundGoldBonus);
        }
        public void AddGold(int amount)
        {
            int reward = Mathf.Max(0, amount);
            Gold += reward;
            RegisterEarlyGoldExcitement(reward);
            NotifyStateChanged();
        }

        public void RecoverLife(int amount)
        {
            int heal = Mathf.Max(0, amount);
            if (heal <= 0)
            {
                return;
            }

            life = Mathf.Min(MaxLife, life + heal);
            NotifyStateChanged();
        }

        public bool TrySpendLifeForContract(int amount, string reason)
        {
            int cost = Mathf.Max(0, amount);
            if (cost <= 0)
            {
                return true;
            }

            if (life <= cost)
            {
                OnBannerRequested?.Invoke("계약 불가  생명력이 부족합니다", new Color(1f, 0.42f, 0.30f), 1.8f);
                return false;
            }

            life = Mathf.Max(1, life - cost);
            string detail = string.IsNullOrWhiteSpace(reason) ? "위험 선택" : reason.Trim();
            runFateContractCount++;
            fateContractCount++;
            fateInterventionCount++;
            runFateInterventionCount++;
            AddFateDebt(cost * Mathf.Max(0, fateDebtPerContractLife), detail);
            AddRunHighlightCard("운명 계약", detail + " / 라이프 -" + cost + " / 빚 +" + cost * Mathf.Max(0, fateDebtPerContractLife));
            NotifyStateChanged();
            return true;
        }

        public int ApplyFateShopDebtCost(int baseCost)
        {
            if (!enableFateIntervention || baseCost <= 0 || fateDebt <= 0)
            {
                return baseCost;
            }

            float multiplier = 1f + Mathf.Clamp01((float)fateDebt / Mathf.Max(1, maxFateDebt)) * maxFateDebtShopCostPenalty;
            return Mathf.Max(1, Mathf.RoundToInt(baseCost * multiplier));
        }

        public void RecordFateShopCostPenalty(int amount)
        {
            int safeAmount = Mathf.Max(0, amount);
            if (safeAmount <= 0)
            {
                return;
            }

            runFateShopCostPenaltyGold += safeAmount;
        }

        public bool TrySpendFateForShopReroll()
        {
            if (!TrySpendFateGauge(fateShopRerollGaugeCost, fateShopRerollDebt, "상점 리롤", "현재 상점 상품 다시 뽑기"))
            {
                return false;
            }

            runFateShopRerollCount++;
            return true;
        }

        public bool TryActivateFateGradeLock(CharacterGrade minimumGrade, int summonCount)
        {
            if (!TrySpendFateGauge(fateGradeLockGaugeCost, fateGradeLockDebt, "등급 잠금", "다음 " + Mathf.Max(1, summonCount) + "회 소환 최소 " + CharacterGradeUtility.GetDisplayName(minimumGrade)))
            {
                return false;
            }

            fateGradeLockSummonsRemaining = Mathf.Max(fateGradeLockSummonsRemaining, Mathf.Max(1, summonCount));
            if ((int)minimumGrade > (int)fateGradeLockMinimum)
            {
                fateGradeLockMinimum = minimumGrade;
            }

            runFateGradeLockCount++;
            OnBannerRequested?.Invoke("운명 개입  등급 잠금 " + fateGradeLockSummonsRemaining + "회", new Color(1f, 0.58f, 0.88f), 2.3f);
            NotifyStateChanged();
            return true;
        }

        public bool TryActivateFateNormalBan(int summonCount)
        {
            if (!TrySpendFateGauge(fateNormalBanGaugeCost, fateNormalBanDebt, "일반 금지", "다음 " + Mathf.Max(1, summonCount) + "회 소환에서 일반 제외"))
            {
                return false;
            }

            fateNormalBanSummonsRemaining = Mathf.Max(fateNormalBanSummonsRemaining, Mathf.Max(1, summonCount));
            runFateNormalBanCount++;
            OnBannerRequested?.Invoke("운명 개입  일반 금지 " + fateNormalBanSummonsRemaining + "회", new Color(0.52f, 1f, 0.82f), 2.3f);
            NotifyStateChanged();
            return true;
        }

        public bool TryActivateFateForcedShop()
        {
            if (!TrySpendFateGauge(fateForceShopGaugeCost, fateForceShopDebt, "상점 강제 등장", "다음 라운드 상점 확정 등장"))
            {
                return false;
            }

            fateForceNextShop = true;
            runFateForcedShopCount++;
            OnBannerRequested?.Invoke("운명 개입  다음 라운드 상점 확정", new Color(0.72f, 0.88f, 1f), 2.3f);
            NotifyStateChanged();
            return true;
        }

        public bool TryActivateFateSurvival()
        {
            if (!TrySpendFateGauge(fateSurvivalGaugeCost, fateSurvivalDebt, "빚지고 살기", "생명 회복 / 골드 보급 / 다음 상점 확정"))
            {
                return false;
            }

            int recovered = Mathf.Max(0, fateSurvivalLifeRecover);
            int gainedGold = Mathf.Max(0, fateSurvivalGold);
            if (recovered > 0)
            {
                life = Mathf.Min(MaxLife, life + recovered);
            }

            if (gainedGold > 0)
            {
                Gold += gainedGold;
            }

            fateForceNextShop = true;
            fateNormalBanSummonsRemaining = Mathf.Max(fateNormalBanSummonsRemaining, Mathf.Max(0, fateSurvivalNormalBanSummons));
            runFateSurvivalCount++;
            AddRunHighlightCard("빚지고 살기", "생명 +" + recovered + " / 골드 +" + gainedGold + " / 다음 상점 확정");
            OnBannerRequested?.Invoke("빚지고 살기  생명 +" + recovered + " / 골드 +" + gainedGold, new Color(1f, 0.62f, 0.22f), 2.4f);
            NotifyStateChanged();
            return true;
        }

        public bool ConsumeFateForcedShopRequest(int round)
        {
            if (!enableFateIntervention || !fateForceNextShop || round <= 1)
            {
                return false;
            }

            fateForceNextShop = false;
            AddRunHighlightCard("상점 강제 등장", "ROUND " + round + " / 운명 빚 정산 예정");
            NotifyStateChanged();
            return true;
        }

        private bool TrySpendFateGauge(int cost, int debt, string title, string detail)
        {
            if (!enableFateIntervention)
            {
                OnBannerRequested?.Invoke("운명 개입 비활성", new Color(1f, 0.42f, 0.30f), 1.8f);
                return false;
            }

            int safeCost = Mathf.Max(0, cost);
            if (fateGauge < safeCost)
            {
                OnBannerRequested?.Invoke("운명 부족  " + fateGauge + "/" + safeCost, new Color(1f, 0.42f, 0.30f), 1.8f);
                return false;
            }

            fateGauge = Mathf.Max(0, fateGauge - safeCost);
            fateInterventionCount++;
            runFateInterventionCount++;
            AddFateDebt(debt, title);
            AddRunHighlightCard("운명 개입", detail + " / 게이지 -" + safeCost + " / 빚 +" + Mathf.Max(0, debt));
            return true;
        }

        public int RemoveGold(int amount)
        {
            int removed = Mathf.Clamp(amount, 0, Gold);
            if (removed <= 0)
            {
                return 0;
            }

            Gold -= removed;
            NotifyStateChanged();
            return removed;
        }

        public void AddRoundGoldBonus(int amount)
        {
            roundGoldBonus += Mathf.Max(0, amount);
            NotifyStateChanged();
        }

        public void AddSummonCostDiscount(float rate)
        {
            summonCostDiscountRate = Mathf.Clamp(summonCostDiscountRate + Mathf.Max(0f, rate), 0f, 0.55f);
            NotifyStateChanged();
        }

        private void AddFateGauge(int amount, string reason)
        {
            if (!enableFateIntervention || amount <= 0)
            {
                return;
            }

            int previous = fateGauge;
            fateGauge = Mathf.Clamp(fateGauge + amount, 0, Mathf.Max(1, maxFateGauge));
            if (fateGauge > previous && fateGauge >= Mathf.Max(1, maxFateGauge))
            {
                OnBannerRequested?.Invoke("운명 개입 준비 완료", new Color(1f, 0.82f, 0.28f), 1.8f);
            }
        }

        private void AddFateDebt(int amount, string reason)
        {
            if (!enableFateIntervention || amount <= 0)
            {
                return;
            }

            int previous = fateDebt;
            fateDebt = Mathf.Clamp(fateDebt + amount, 0, Mathf.Max(1, maxFateDebt));
            int added = Mathf.Max(0, fateDebt - previous);
            runFateDebtAdded += added;
            runPeakFateDebt = Mathf.Max(runPeakFateDebt, fateDebt);
        }

        private void RepayFateDebt(int amount, string reason)
        {
            if (!enableFateIntervention || amount <= 0 || fateDebt <= 0)
            {
                return;
            }

            int previous = fateDebt;
            fateDebt = Mathf.Max(0, fateDebt - amount);
            int repaid = previous - fateDebt;
            runFateDebtRepaid += Mathf.Max(0, repaid);
            if (repaid > 0 && previous >= Mathf.Max(1, maxFateDebt) / 2 && fateDebt < Mathf.Max(1, maxFateDebt) / 2)
            {
                AddRunHighlightCard("운명 빚 정산", reason + " / -" + repaid);
            }
        }

        private float ResolveFateDebtBossHealthMultiplier()
        {
            if (!enableFateIntervention || fateDebt <= 0)
            {
                return 1f;
            }

            return 1f + Mathf.Clamp01((float)fateDebt / Mathf.Max(1, maxFateDebt)) * maxFateDebtBossHealthBonus;
        }

        public bool CanSellUnit(DefenderUnit unit, out string reason)
        {
            if (!enableUnitSelling)
            {
                reason = "판매 기능 비활성";
                return false;
            }

            if (unit == null || unit.CurrentSlot == null)
            {
                reason = "선택된 유닛 없음";
                return false;
            }

            if (unit.IsTemporarySummon)
            {
                reason = "소환수는 판매 불가";
                return false;
            }

            if (IsRoundRunning)
            {
                reason = "전투 중 판매 불가";
                return false;
            }

            if (gameOverRaised)
            {
                reason = "런 종료 후 판매 불가";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public int GetUnitSellRefund(DefenderUnit unit)
        {
            if (unit == null)
            {
                return 0;
            }

            int baseValue = ResolveUnitSellBaseValue(unit.Grade);
            return Mathf.Max(1, Mathf.RoundToInt(baseValue * Mathf.Clamp(unitSellRefundRate, 0.1f, 0.5f)));
        }

        private int ResolveUnitSellBaseValue(CharacterGrade grade)
        {
            switch (grade)
            {
                case CharacterGrade.Rare:
                    return Mathf.Max(1, rareUnitSellBaseValue);
                case CharacterGrade.Epic:
                    return Mathf.Max(1, epicUnitSellBaseValue);
                case CharacterGrade.Legendary:
                    return Mathf.Max(1, legendaryUnitSellBaseValue);
                case CharacterGrade.Mythic:
                    return Mathf.Max(1, mythicUnitSellBaseValue);
                case CharacterGrade.Transcendent:
                    return Mathf.Max(1, transcendentUnitSellBaseValue);
                default:
                    return Mathf.Max(1, normalUnitSellBaseValue);
            }
        }

        public string GetUnitSellDetail(DefenderUnit unit)
        {
            if (unit == null || unit.Definition == null)
            {
                return "유닛을 선택하면 판매할 수 있습니다.";
            }

            string detail = CharacterGradeUtility.GetDisplayName(unit.Grade) + " " + unit.Definition.displayName
                + "  |  판매가 " + GetUnitSellRefund(unit) + "G";
            if (IsUnitSellMergeCandidate(unit))
            {
                detail += "  |  합성 후보";
            }

            return detail;
        }

        public bool UnitSellRequiresConfirmation(DefenderUnit unit)
        {
            return false;
        }

        public bool IsUnitSellMergeCandidate(DefenderUnit unit)
        {
            if (unit == null || unit.CurrentSlot == null)
            {
                return false;
            }

            if (boardManager != null && boardManager.IsReservedForUltimateRecipeUnit(unit))
            {
                return true;
            }

            if (unit.Grade == CharacterGrade.Mythic || unit.Grade == CharacterGrade.Transcendent)
            {
                return false;
            }

            return CountUnitsOfGrade(unit.Grade) >= 2;
        }

        public bool TrySellUnit(DefenderUnit unit, out int refund, out string message)
        {
            refund = 0;
            if (!CanSellUnit(unit, out message))
            {
                return false;
            }

            refund = GetUnitSellRefund(unit);
            string unitName = unit.Definition != null && !string.IsNullOrWhiteSpace(unit.Definition.displayName)
                ? unit.Definition.displayName
                : "유닛";

            if (boardManager == null || !boardManager.TryRemoveUnitFromBoard(unit))
            {
                message = "판매 실패: 유닛 상태 확인 필요";
                return false;
            }

            Gold += refund;
            message = unitName + " 판매 +" + refund + "G";
            OnBannerRequested?.Invoke(message, new Color(1f, 0.72f, 0.28f), 2.0f);
            NotifyStateChanged();
            return true;
        }

        public void RequestBanner(string message, Color color, float duration)
        {
            OnBannerRequested?.Invoke(message, color, duration);
        }

        public void RecordBossSkillCast(SkillDefinition skill, bool majorBoss)
        {
            if (skill == null)
            {
                return;
            }

            currentRoundBossSkillCasts++;
            totalBossSkillCasts++;
            currentRoundLastBossSkill = string.IsNullOrWhiteSpace(skill.displayName) ? skill.effectType.ToString() : skill.displayName;
            lastBossSkill = currentRoundLastBossSkill;
            NotifyStateChanged();
        }

        public void RecordBossSkillImpact(SkillDefinition skill, int affectedTargets, float damageDone, int goldDrained, bool majorBoss)
        {
            if (skill == null)
            {
                return;
            }

            int safeTargets = Mathf.Max(0, affectedTargets);
            float safeDamage = Mathf.Max(0f, damageDone);
            int safeGold = Mathf.Max(0, goldDrained);

            currentRoundBossAffectedTargets += safeTargets;
            totalBossAffectedTargets += safeTargets;
            currentRoundBossSkillDamage += safeDamage;
            totalBossSkillDamage += safeDamage;
            currentRoundBossGoldDrained += safeGold;
            totalBossGoldDrained += safeGold;

            switch (skill.effectType)
            {
                case SkillEffectType.ManaBurn:
                    currentRoundBossManaBurnTargets += safeTargets;
                    totalBossManaBurnTargets += safeTargets;
                    break;
                case SkillEffectType.DeathPact:
                    currentRoundBossExecutions += safeTargets;
                    totalBossExecutions += safeTargets;
                    break;
                case SkillEffectType.BossFortify:
                    currentRoundBossFortifyCount++;
                    totalBossFortifyCount++;
                    break;
                case SkillEffectType.MonsterRally:
                    currentRoundBossRallyTargets += safeTargets;
                    totalBossRallyTargets += safeTargets;
                    break;
            }

            NotifyStateChanged();
        }

        public void MarkEarlyRunRecoveryOffered()
        {
            earlyRunRecoveryOfferCount++;
            earlyRunRecoveryShopOfferCount++;
            runRecoveryShopOffered = true;
            earlyRunRecoveryRecommended = false;
            if (string.IsNullOrWhiteSpace(earlyRunRecoveryReason) || earlyRunRecoveryReason == "초반 런 안정")
            {
                earlyRunRecoveryReason = earlyRunRecoveryCause + " 회복 선택지 제공 완료";
            }

            UpdateEarlyRunLogCoverageSummary();
            Debug.Log("[EarlyRunTelemetry] 회복상점 제공 " + earlyRunRecoveryOfferCount + "회 / " + earlyRunLogCoverageSummary);
            NotifyStateChanged();
        }

        public void RecordR3BoosterOffer()
        {
            if (!enableEarlyRoundTelemetry)
            {
                return;
            }

            earlyRunR3BoosterOfferCount++;
            runR3BoosterOffered = true;
            UpdateEarlyRunLogCoverageSummary();
            Debug.Log("[EarlyRunTelemetry] R3 부스터 노출 " + earlyRunR3BoosterPurchaseCount + "/" + earlyRunR3BoosterOfferCount + " / " + earlyRunLogCoverageSummary);
            NotifyStateChanged();
        }

        public void RecordR3BoosterPurchase()
        {
            if (!enableEarlyRoundTelemetry)
            {
                return;
            }

            earlyRunR3BoosterPurchaseCount++;
            runR3BoosterPurchased = true;
            UpdateEarlyRunLogCoverageSummary();
            Debug.Log("[EarlyRunTelemetry] R3 부스터 구매 " + earlyRunR3BoosterPurchaseCount + "/" + earlyRunR3BoosterOfferCount + " / " + earlyRunLogCoverageSummary);
            NotifyStateChanged();
        }

        public void RecordEarlyRecoveryShopPurchase()
        {
            if (!enableEarlyRoundTelemetry)
            {
                return;
            }

            earlyRunRecoveryShopPurchaseCount++;
            runRecoveryShopPurchased = true;
            UpdateEarlyRunLogCoverageSummary();
            Debug.Log("[EarlyRunTelemetry] 회복상점 구매 " + earlyRunRecoveryShopPurchaseCount + "/" + earlyRunRecoveryShopOfferCount + " / " + earlyRunLogCoverageSummary);
            NotifyStateChanged();
        }

        public void MarkBadLuckInsuranceClaimed(string choiceName)
        {
            badLuckInsuranceOfferPending = false;
            badLuckInsuranceResolved = true;
            runInsuranceClaimed = true;
            earlyRunRecoveryRecommended = false;
            badLuckInsuranceReason = string.IsNullOrWhiteSpace(choiceName)
                ? "운 나쁨 보험 선택 완료"
                : "운 나쁨 보험 선택: " + choiceName;
            earlyRunRecoveryReason = badLuckInsuranceReason;
            earlyRunRecoveryCause = "소환 부족";
            NotifyStateChanged();
        }

        public void RecordSynergySnapshot(int activeCount, string leadingSynergyTitle)
        {
            int previousSynergyCount = currentSynergyCount;
            string previousSynergyTitle = currentSynergyTitle;
            currentSynergyCount = Mathf.Max(0, activeCount);
            currentSynergyTitle = string.IsNullOrWhiteSpace(leadingSynergyTitle) ? "시너지 없음" : leadingSynergyTitle;
            ReportSynergyActivationFeedback(previousSynergyCount, previousSynergyTitle);

            if (activeCount <= bestSynergyCount)
            {
                return;
            }

            bestSynergyCount = activeCount;
            bestSynergyTitle = string.IsNullOrWhiteSpace(leadingSynergyTitle) ? "시너지 조합" : leadingSynergyTitle;
        }

        public void AddCharacterContent(int additionalCount)
        {
            if (characterDatabase == null)
            {
                return;
            }

            characterDatabase.ExpandGeneratedCharacterContent(additionalCount);
            NotifyStateChanged();
        }

        public void AddMonsterContent(int additionalCount)
        {
            if (monsterDatabase == null)
            {
                return;
            }

            int nextCount = Mathf.Max(monsterDatabase.Monsters.Count + additionalCount, monsterDatabase.Monsters.Count);
            monsterDatabase.GenerateStarterMonsters(nextCount);
            NotifyStateChanged();
        }

        public bool TryGrantRandomUnitByGrade(CharacterGrade grade)
        {
            if (characterDatabase == null || boardManager == null)
            {
                return false;
            }

            CharacterDefinition definition = characterDatabase.GetRandomCharacterByGrade(grade, true);
            if (definition == null)
            {
                definition = characterDatabase.GetRandomSummonableCharacter(GetSummonRateRound(), true);
            }

            if (definition == null || !boardManager.TrySpawnUnit(definition, defaultUnitPrefab, out DefenderUnit spawnedUnit))
            {
                return false;
            }

            RegisterGrantedUnitExcitement(definition, spawnedUnit);
            OnUnitSummoned?.Invoke(definition);
            NotifyStateChanged();
            return true;
        }

        public bool TryGrantRandomSummonableUnit()
        {
            if (characterDatabase == null || boardManager == null)
            {
                return false;
            }

            CharacterDefinition definition = characterDatabase.GetRandomSummonableCharacter(GetSummonRateRound(), true);
            if (definition == null || !boardManager.TrySpawnUnit(definition, defaultUnitPrefab, out DefenderUnit spawnedUnit))
            {
                return false;
            }

            RegisterGrantedUnitExcitement(definition, spawnedUnit);
            OnUnitSummoned?.Invoke(definition);
            NotifyStateChanged();
            return true;
        }

        public bool TryGrantMergeAssistUnit()
        {
            if (characterDatabase == null || boardManager == null)
            {
                return false;
            }

            CharacterGrade targetGrade = SelectMergeAssistGrade();
            int ownedBefore = CountUnitsOfGrade(targetGrade);
            int missingForMerge = ownedBefore > 0 ? Mathf.Max(1, 3 - ownedBefore) : 1;
            int grantCount = Mathf.Min(missingForMerge, Mathf.Max(0, EmptySlotCount));
            if (grantCount <= 0)
            {
                return false;
            }

            int granted = 0;
            CharacterDefinition featuredDefinition = null;
            DefenderUnit featuredUnit = null;
            for (int i = 0; i < grantCount; i++)
            {
                CharacterDefinition definition = characterDatabase.GetRandomCharacterByGrade(targetGrade, true);
                if (definition == null ||
                    !boardManager.TrySpawnUnit(definition, defaultUnitPrefab, out DefenderUnit spawnedUnit))
                {
                    break;
                }

                granted++;
                if (featuredDefinition == null)
                {
                    featuredDefinition = definition;
                    featuredUnit = spawnedUnit;
                }

                OnUnitSummoned?.Invoke(definition);
            }

            if (granted <= 0)
            {
                return false;
            }

            RegisterGrantedUnitExcitement(featuredDefinition, featuredUnit);
            bool mergeReady = ownedBefore + granted >= 3;
            string gradeLabel = CharacterGradeUtility.GetDisplayName(targetGrade);
            OnBannerRequested?.Invoke(
                mergeReady ? "합성 준비 완료!  " + gradeLabel + " 재료 " + (ownedBefore + granted) + "/3" : "합성 재료 보급  " + gradeLabel + " +" + granted,
                CharacterGradeUtility.GetColor(targetGrade, Color.white),
                mergeReady ? 2.6f : 2.1f);
            NotifyStateChanged();
            return true;
        }

        public int CountUnitsOfGrade(CharacterGrade grade)
        {
            return boardManager != null ? boardManager.CountUnitsOfGrade(grade) : 0;
        }

        public bool CanMergeUltimate()
        {
            return boardManager != null && boardManager.CanMergeUltimate(characterDatabase);
        }

        public int ReadyUltimateRecipeCount => boardManager != null
            ? boardManager.GetReadyUltimateRecipeOptions(characterDatabase).Length
            : 0;

        public UltimateRecipeOption[] GetReadyUltimateRecipeOptions()
        {
            return boardManager != null
                ? boardManager.GetReadyUltimateRecipeOptions(characterDatabase)
                : new UltimateRecipeOption[0];
        }

        public void SetUltimateRecipePreview(string recipeName, bool previewActive = false)
        {
            if (boardManager != null)
            {
                boardManager.SetUltimateRecipePreview(recipeName, previewActive);
            }
        }

        public string GetUltimateMergeStatus()
        {
            return boardManager != null ? boardManager.GetUltimateMergeStatus(characterDatabase) : string.Empty;
        }

        public string GetUltimateMergeDetailStatus()
        {
            return boardManager != null ? boardManager.GetUltimateMergeDetailStatus(characterDatabase) : string.Empty;
        }

        public string GetUltimateMergeActionStatus()
        {
            return boardManager != null ? boardManager.GetUltimateMergeActionStatus(characterDatabase) : string.Empty;
        }

        public string GetUltimateRecipeBingoStatus()
        {
            return boardManager != null ? boardManager.GetUltimateRecipeBingoStatus(characterDatabase) : string.Empty;
        }

        private void ResolveUltimateRecipeBingoReward()
        {
            if (!enableFateIntervention || boardManager == null)
            {
                return;
            }

            string[] readyRecipes = boardManager.GetReadyUltimateRecipeNames(characterDatabase);
            if (readyRecipes == null || readyRecipes.Length == 0)
            {
                return;
            }

            for (int i = 0; i < readyRecipes.Length; i++)
            {
                string recipeName = readyRecipes[i];
                if (string.IsNullOrWhiteSpace(recipeName) || rewardedUltimateRecipeNames.Contains(recipeName))
                {
                    continue;
                }

                rewardedUltimateRecipeNames.Add(recipeName);
                AddFateGauge(ultimateRecipeBingoFateGaugeBonus, "초월 레시피 빙고");
                AddRunHighlightCard("레시피 빙고", recipeName + " / 운명 +" + Mathf.Max(0, ultimateRecipeBingoFateGaugeBonus));
                OnBannerRequested?.Invoke("레시피 빙고 완성!  운명 +" + Mathf.Max(0, ultimateRecipeBingoFateGaugeBonus), new Color(1f, 0.80f, 0.28f), 2.6f);
            }
        }

        private void HandleMonsterKilled(MonsterUnit monster)
        {
            int rewardGold = monster != null ? monster.GetRewardGold() : 0;
            Gold += rewardGold;
            if (monster != null && monster.IsBoss)
            {
                totalBossKills++;
                string bossName = monster.Definition != null && !string.IsNullOrWhiteSpace(monster.Definition.displayName)
                    ? monster.Definition.displayName
                    : "보스";
                AddRunHighlightCard("보스 처치", bossName + " / +" + rewardGold + "G");
                AddFateGauge(fateGaugeOnBossKill, "보스 처치");
            }

            RegisterKillCombo(monster);
            MarkRoundMonsterResolved();
            NotifyStateChanged();
        }

        private void HandleMonsterEscaped(MonsterUnit monster)
        {
            MarkRoundMonsterResolved();

            int leakDamage = ResolveMonsterLeakDamage(monster);
            if (leakDamage > 0)
            {
                life = Mathf.Max(0, life - leakDamage);
                AddRunHighlightCard("\uBC29\uC5B4\uC120 \uB3CC\uD30C", "HP -" + leakDamage + " / " + life + "/" + MaxLife);
                OnBannerRequested?.Invoke("\uBC29\uC5B4\uC120 \uB3CC\uD30C!  HP -" + leakDamage, new Color(1f, 0.38f, 0.24f), 1.8f);
            }

            NotifyStateChanged();
            if (life <= 0)
            {
                TriggerLeakDefeat();
            }
        }

        private int ResolveMonsterLeakDamage(MonsterUnit monster)
        {
            if (monster == null || monster.Definition == null)
            {
                return 1;
            }

            switch (monster.Definition.threatLevel)
            {
                case MonsterThreatLevel.Boss:
                    return Mathf.Max(5, Mathf.CeilToInt(MaxLife * 0.30f));
                case MonsterThreatLevel.MidBoss:
                    return 2;
                default:
                    return 1;
            }
        }

        private void TriggerLeakDefeat()
        {
            if (gameOverRaised)
            {
                return;
            }

            gameOverRaised = true;
            AddRunHighlightCard("\uBC29\uC5B4\uC120 \uBD95\uAD34", "HP 0 / \uBAAC\uC2A4\uD130 \uB3CC\uD30C");
            BeginDefeatSequence();
        }

        private void BeginDefeatSequence()
        {
            NotifyStateChanged();
            if (roundManager != null && roundManager.IsRoundRunning)
            {
                roundManager.BeginDefeatCinematic();
                CancelPendingDefeatFinalization();
                defeatFinalizeRoutine = StartCoroutine(FinalizeDefeatAfterCinematic());
            }

            OnGameOver?.Invoke();
        }

        private IEnumerator FinalizeDefeatAfterCinematic()
        {
            CaptureDefeatTimeScale();
            IsDefeatSlowMotionActive = true;

            float startScale = Mathf.Max(0.0001f, defeatPreviousTimeScale);
            float elapsed = 0f;
            while (elapsed < DefeatSlowMotionDurationRealtime)
            {
                elapsed = Mathf.Min(DefeatSlowMotionDurationRealtime, elapsed + Time.unscaledDeltaTime);
                float normalized = Mathf.Clamp01(elapsed / DefeatSlowMotionDurationRealtime);
                float scale = Mathf.SmoothStep(startScale, DefeatSlowMotionTargetScale, normalized);
                Time.timeScale = scale;
                Time.fixedDeltaTime = Mathf.Max(0.001f, defeatPreviousFixedDeltaTime * scale / startScale);
                yield return null;
            }

            Time.timeScale = DefeatSlowMotionTargetScale;
            Time.fixedDeltaTime = Mathf.Max(0.001f, defeatPreviousFixedDeltaTime * DefeatSlowMotionTargetScale / startScale);
            if (roundManager != null && roundManager.IsRoundRunning)
            {
                roundManager.ForceFailRound();
            }

            yield return new WaitForSecondsRealtime(DefeatFinalizePaddingRealtime);
            RestoreDefeatTimeScale();
            defeatFinalizeRoutine = null;
        }

        private void CancelPendingDefeatFinalization()
        {
            if (defeatFinalizeRoutine != null)
            {
                StopCoroutine(defeatFinalizeRoutine);
                defeatFinalizeRoutine = null;
            }

            RestoreDefeatTimeScale();
        }

        private void CaptureDefeatTimeScale()
        {
            if (defeatTimeScaleCaptured)
            {
                return;
            }

            float observedScale = Mathf.Max(0.0001f, Time.timeScale);
            defeatPreviousTimeScale = observedScale <= 0.30f ? 1f : observedScale;
            defeatPreviousFixedDeltaTime = Time.fixedDeltaTime > 0f
                ? Time.fixedDeltaTime * defeatPreviousTimeScale / observedScale
                : 0.02f * defeatPreviousTimeScale;
            defeatTimeScaleCaptured = true;
        }

        private void RestoreDefeatTimeScale()
        {
            IsDefeatSlowMotionActive = false;
            if (!defeatTimeScaleCaptured)
            {
                return;
            }

            Time.timeScale = defeatPreviousTimeScale > 0f ? defeatPreviousTimeScale : 1f;
            Time.fixedDeltaTime = defeatPreviousFixedDeltaTime > 0f ? defeatPreviousFixedDeltaTime : 0.02f;
            defeatTimeScaleCaptured = false;
        }

        private void HandleRoundStateChanged(int round, bool bossRound, bool running)
        {
            if (running)
            {
                DismissTemporarySummons();
                currentRoundResolvedMonsters = 0;
                ResetRoundTileContribution();
                BeginEarlyRoundTelemetry(round);
                TryApplyFirstBossSummonRushBonus(round, bossRound);
                AnnounceEarlyCrisisRound(round);
                OnRoundStarted?.Invoke(round);
            }
            else
            {
                currentRoundResolvedMonsters = Mathf.Max(currentRoundResolvedMonsters, RoundTargetCount);
                ClearRoundCombatEffects();
            }

            if (!running && boardManager != null)
            {
                DismissTemporarySummons();
                if (roundManager != null && roundManager.LastRoundEndedByDefeat)
                {
                    CompleteEarlyRoundTelemetry(round, bossRound, false);
                    RecordRoundDefeatMoment(round, bossRound);
                    NotifyStateChanged();
                    return;
                }

                int clearReward = CalculateRoundClearGold(round);
                LastRoundClearGoldReward = clearReward;
                Gold += clearReward;
                victoryStreak++;
                CompleteEarlyRoundTelemetry(round, bossRound, true);
                ResolveFateRoundClear(bossRound);
                int unlockedFrontSlots = boardManager.RefreshSlotLocks(round, true);

                DefenderUnit[] defenders = boardManager.GetAliveDefenders();
                for (int i = 0; i < defenders.Length; i++)
                {
                    if (defenders[i] != null)
                    {
                        defenders[i].ResetFacingToDefault();
                        defenders[i].PlayWinAnimation();
                    }
                }

                OnBannerRequested?.Invoke("ROUND CLEAR  +" + clearReward + "G", new Color(0.48f, 1f, 0.72f), 2.5f);
                ReportRoundCombatRecap(bossRound);
                if (unlockedFrontSlots > 0)
                {
                    int unlockedRound = Mathf.Max(1, round + 1);
                    OnBannerRequested?.Invoke("ROUND " + unlockedRound + "  \uC804\uBC29 \uBC30\uCE58\uCE78 \uAC1C\uBC29 +" + unlockedFrontSlots + "  |  +" + clearReward + "G", new Color(0.46f, 1f, 0.82f), 3.0f);
                    AddRunHighlightCard("\uC804\uBC29 \uC2AC\uB86F \uAC1C\uBC29", "ROUND " + unlockedRound + " / +" + unlockedFrontSlots);
                }
                OnRoundMissionSettlement?.Invoke(round);
                OnRoundEconomySettlement?.Invoke(round);
                ResolveEarlyRunFallback(round);
                ResolveEarlyBossPrepReward(round);
                OnRoundBoardPreparation?.Invoke(round);
                OnRoundShopPhase?.Invoke(round);
                OnRoundAugmentChoicePhase?.Invoke(round);
                OnRoundCompleted?.Invoke(round);
            }

            NotifyStateChanged();
        }

        private void ClearRoundCombatEffects()
        {
            RuntimeEffectUtility.ClearTrackedEffects();

            DefenderUnit[] defenders = boardManager != null
                ? boardManager.GetAliveDefenders()
                : FindObjectsOfType<DefenderUnit>();
            for (int i = 0; i < defenders.Length; i++)
            {
                if (defenders[i] != null)
                {
                    defenders[i].ClearRoundTemporaryEffects();
                }
            }
        }

        private void DismissTemporarySummons()
        {
            DefenderUnit[] defenders = FindObjectsOfType<DefenderUnit>();
            for (int i = 0; i < defenders.Length; i++)
            {
                if (defenders[i] != null && defenders[i].IsTemporarySummon)
                {
                    defenders[i].DismissTemporarySummon();
                }
            }
        }

        private void HandleDefenderRemoved(DefenderUnit defender)
        {
            if (!IsRoundRunning || gameOverRaised || CountAliveDefendersInScene() > 0)
            {
                return;
            }

            life = 0;
            gameOverRaised = true;
            victoryStreak = 0;
            earnedGrowthCurrency = CalculateGrowthCurrency();
            OnBannerRequested?.Invoke("아군 전멸", new Color(1f, 0.38f, 0.26f), 2.2f);
            BeginDefeatSequence();
        }

        private int CountAliveDefendersInScene()
        {
            DefenderUnit[] defenders = FindObjectsOfType<DefenderUnit>();
            int aliveCount = 0;
            for (int i = 0; i < defenders.Length; i++)
            {
                if (defenders[i] != null && !defenders[i].IsTemporarySummon && defenders[i].CurrentHealth > 0f)
                {
                    aliveCount++;
                }
            }

            return aliveCount;
        }

        private void MarkRoundMonsterResolved()
        {
            if (!IsRoundRunning || RoundTargetCount <= 0)
            {
                return;
            }

            currentRoundResolvedMonsters = Mathf.Min(currentRoundResolvedMonsters + 1, RoundTargetCount);
        }

        private void HandleRoundCountdownChanged(int countdown)
        {
            OnRoundCountdownChanged?.Invoke(countdown);
            if (countdown > 0)
            {
                RuntimeAudioUtility.PlayCountdown();
                OnBannerRequested?.Invoke("ROUND " + CurrentRound + " STARTS IN", new Color(0.98f, 0.88f, 0.42f), 1.05f);
            }
        }

        private void SubscribeRoundManager()
        {
            if (roundManager != null)
            {
                roundManager.OnRoundStateChanged -= HandleRoundStateChanged;
                roundManager.OnRoundStateChanged += HandleRoundStateChanged;
                roundManager.OnCountdownChanged -= HandleRoundCountdownChanged;
                roundManager.OnCountdownChanged += HandleRoundCountdownChanged;
            }
        }

        private void UnsubscribeRoundManager()
        {
            if (roundManager != null)
            {
                roundManager.OnRoundStateChanged -= HandleRoundStateChanged;
                roundManager.OnCountdownChanged -= HandleRoundCountdownChanged;
            }
        }

        private void NotifyStateChanged()
        {
            OnStateChanged?.Invoke();
        }

        private void HandleDamageDealt(DefenderUnit source, MonsterUnit target, float damage, bool critical)
        {
            if (source == null || damage <= 0f)
            {
                return;
            }

            string heroName = source.Definition != null ? source.Definition.displayName : source.name;
            if (string.IsNullOrWhiteSpace(heroName))
            {
                heroName = "Unknown Hero";
            }

            totalDamageDealt += damage;
            RecordTileDamageContribution(source, target, damage);
            float previousTopDamage = topDamageHeroDamage;
            string previousTopHeroName = topDamageHeroName;
            if (critical)
            {
                criticalHitCount++;
                if (Time.time >= nextCriticalBannerTime)
                {
                    nextCriticalBannerTime = Time.time + 1.4f;
                    OnBannerRequested?.Invoke("CRITICAL!", new Color(1f, 0.78f, 0.24f), 0.9f);
                }
            }

            AddDamageContribution(damageByHero, heroName, damage);
            AddDamageContribution(currentRoundDamageByHero, heroName, damage);
            if (damageByHero[heroName] > topDamageHeroDamage)
            {
                topDamageHeroDamage = damageByHero[heroName];
                topDamageHeroName = heroName;
                ReportTopDamageFeedback(heroName, topDamageHeroDamage, previousTopHeroName, previousTopDamage);
            }

            if (currentRoundDamageByHero[heroName] > roundTopDamageHeroDamage)
            {
                roundTopDamageHeroDamage = currentRoundDamageByHero[heroName];
                roundTopDamageHeroName = heroName;
            }
        }

        private void RegisterKillCombo(MonsterUnit monster)
        {
            float now = Time.time;
            currentKillCombo = now - lastKillTime <= 2.2f ? currentKillCombo + 1 : 1;
            lastKillTime = now;
            bestKillCombo = Mathf.Max(bestKillCombo, currentKillCombo);

            if (currentKillCombo >= 5 && currentKillCombo % 5 == 0)
            {
                OnBannerRequested?.Invoke(currentKillCombo + " COMBO!", new Color(1f, 0.82f, 0.24f), 1.35f);
                RuntimeCameraShake.Request(0.035f, 0.10f);
            }

            if (monster != null && monster.IsBoss)
            {
                Color color = monster.Definition != null ? monster.Definition.accentColor : new Color(1f, 0.58f, 0.24f);
                int rewardGold = monster.GetRewardGold();
                string responseDamage = Mathf.RoundToInt(currentRoundBossTileDamage).ToString("N0");
                string bossName = monster.Definition != null && !string.IsNullOrWhiteSpace(monster.Definition.displayName) ? monster.Definition.displayName : "Boss";
                string bossGrade = monster.Definition != null && monster.Definition.IsMajorBoss ? "BOSS" : "MID BOSS";
                OnBannerRequested?.Invoke("보스 처치!  +" + rewardGold + "G  |  대응 " + responseDamage, color, 2.8f);
                RuntimeAudioUtility.PlayJackpotMajor();
                RuntimeGameFeel.PlayJackpotPulse(monster.transform.position, color, monster.Definition != null && monster.Definition.IsMajorBoss ? 2.15f : 1.55f, monster.Definition != null && monster.Definition.IsMajorBoss ? 0.20f : 0.14f, 0.42f, 0.15f, 0.10f, 3);
                RuntimeGameFeel.ShowJackpotReveal("보스 처치!", bossGrade, bossName, color, "+" + rewardGold + "G / 대응 " + responseDamage, 2.4f);
            }
        }

        private int CalculateGrowthCurrency()
        {
            int roundScore = Mathf.Max(0, CurrentRound) * 2;
            int synergyScore = Mathf.Max(0, bestSynergyCount) * 3;
            int comboScore = Mathf.Max(0, bestKillCombo / 5) * 2;
            int damageScore = Mathf.Clamp(Mathf.RoundToInt(totalDamageDealt / 2500f), 0, 20);
            return Mathf.Max(3, roundScore + synergyScore + comboScore + damageScore);
        }

        private void ResetRunStats()
        {
            damageByHero.Clear();
            currentRoundDamageByHero.Clear();
            topDamageHeroName = "없음";
            topDamageHeroDamage = 0f;
            roundTopDamageHeroName = "없음";
            roundTopDamageHeroDamage = 0f;
            totalDamageDealt = 0f;
            criticalHitCount = 0;
            currentKillCombo = 0;
            bestKillCombo = 0;
            lastKillTime = -999f;
            bestSynergyCount = 0;
            bestSynergyTitle = "시너지 없음";
            currentSynergyCount = 0;
            currentSynergyTitle = "시너지 없음";
            earnedGrowthCurrency = 0;
            nextCriticalBannerTime = 0f;
            earlySummonAttempts = 0;
            earlyRunMomentTriggered = false;
            earlyFallbackRewardGranted = false;
            earlyBossPrepRewardGranted = false;
            earlyRoundTelemetry.Clear();
            currentRoundStartTime = 0f;
            currentRoundStartGold = 0;
            currentRoundSummonCount = 0;
            currentRoundMergeCount = 0;
            currentRoundHadMerge = false;
            currentRoundHighestMergeGrade = CharacterGrade.Normal;
            pendingRoundSummonCount = 0;
            pendingRoundMergeCount = 0;
            pendingRoundHadMerge = false;
            pendingRoundHighestMergeGrade = CharacterGrade.Normal;
            runMergeCount = 0;
            firstBossSummonRushBonusGranted = false;
            earlyRunTelemetrySummary = "1~10R 계측 대기";
            earlyRunTuningHint = "초반 런 데이터 대기";
            earlyRunRecoveryRecommended = false;
            earlyRunRecoveryReason = "초반 런 안정";
            earlyRunRecoveryCause = "흐름 안정";
            earlyRunRecoveryOfferCount = 0;
            earlyBadLuckSummons = 0;
            earlyBadLuckBestGrade = CharacterGrade.Normal;
            badLuckInsuranceOfferPending = false;
            badLuckInsuranceOffered = false;
            badLuckInsuranceResolved = false;
            badLuckInsuranceReason = "초반 소환 보험 대기";
            earlyRunTuningLogRecorded = false;
            runR3BoosterOffered = false;
            runR3BoosterPurchased = false;
            runRecoveryShopOffered = false;
            runRecoveryShopPurchased = false;
            runInsuranceOffered = false;
            runInsuranceClaimed = false;
            runR10BossHealthRemaining01 = -1f;
            firstRarePlusRound = -1;
            firstMergeRound = -1;
            fateGauge = enableFateIntervention ? Mathf.Clamp(startingFateGauge, 0, Mathf.Max(1, maxFateGauge)) : 0;
            fateDebt = 0;
            fateInterventionCount = 0;
            fateContractCount = 0;
            runFateInterventionCount = 0;
            runFateContractCount = 0;
            runFateShopRerollCount = 0;
            runFateGradeLockCount = 0;
            runFateNormalBanCount = 0;
            runFateForcedShopCount = 0;
            runFateSurvivalCount = 0;
            runFateDebtAdded = 0;
            runFateDebtRepaid = 0;
            runFateShopCostPenaltyGold = 0;
            runPeakFateDebt = 0;
            fateGradeLockSummonsRemaining = 0;
            fateGradeLockMinimum = CharacterGrade.Normal;
            fateNormalBanSummonsRemaining = 0;
            fateForceNextShop = false;
            firstLegendaryMergeRecorded = false;
            lifeOneClutchRecorded = false;
            fateSurvivalClutchRecorded = false;
            runDefeatMomentRecorded = false;
            runHighlightCards.Clear();
            runClipEvents.Clear();
            rewardedUltimateRecipeNames.Clear();
            totalBossTileDamage = 0f;
            totalBossKills = 0;
            currentRoundLastBossSkill = "없음";
            totalBossSkillCasts = 0;
            totalBossAffectedTargets = 0;
            totalBossGoldDrained = 0;
            totalBossManaBurnTargets = 0;
            totalBossExecutions = 0;
            totalBossFortifyCount = 0;
            totalBossRallyTargets = 0;
            totalBossSkillDamage = 0f;
            lastBossSkill = "없음";
            nextTopDamageFeedbackThreshold = Mathf.Max(1f, topDamageFeedbackStep);
            ResetRoundTileContribution();
            UpdateEarlyRunLogCoverageSummary();
        }

        private string BuildRecommendedDeckSummary()
        {
            if (bestSynergyCount >= 4)
            {
                return bestSynergyTitle + " 중심으로 고등급 유닛을 유지하세요.";
            }

            if (criticalHitCount >= 20)
            {
                return "치명타 빌드가 잘 맞았습니다. 암살/그림자 계열을 늘려보세요.";
            }

            if (topDamageHeroDamage > 0f)
            {
                return topDamageHeroName + " 중심으로 같은 태그를 모아보세요.";
            }

            return "초반에는 확정 증강체와 2개 이상 같은 태그를 우선해보세요.";
        }

        private string BuildRecommendedBuildName()
        {
            if (CanMergeUltimate())
            {
                return "초월 완성덱";
            }

            if (totalBossSkillCasts > 0 && totalBossTileDamage < Mathf.Max(1f, totalBossSkillDamage * 0.35f))
            {
                return "보스 대응덱";
            }

            if (RoundsUntilNextBoss > 0 && RoundsUntilNextBoss <= 2)
            {
                return "보스 사냥덱";
            }

            if (bestSynergyCount >= 4)
            {
                return bestSynergyTitle + " 시너지덱";
            }

            if (criticalHitCount >= 20)
            {
                return "치명타 폭발덱";
            }

            if (topDamageHeroDamage > 0f)
            {
                return topDamageHeroName + " 캐리덱";
            }

            if (earlyRunRecoveryRecommended)
            {
                return "회복 재정비덱";
            }

            return "보스 사냥덱";
        }

        private int ResolveSummonCost()
        {
            int baseCost = currentSummonBaseCost > 0 ? currentSummonBaseCost : summonCost;
            float discounted = baseCost * (1f - Mathf.Clamp01(summonCostDiscountRate));
            return Mathf.Max(1, Mathf.RoundToInt(discounted));
        }

        private int ResolveSummonCostIncrease()
        {
            int round = GetSummonRateRound();
            if (round <= Mathf.Max(1, earlySummonCostRampRoundLimit))
            {
                return Mathf.Max(0, earlySummonCostIncreasePerSummon);
            }

            return Mathf.Max(0, summonCostIncreasePerSummon);
        }

        private int GetSummonRateRound()
        {
            int round = CurrentRound;
            if (!IsRoundRunning)
            {
                round++;
            }

            return Mathf.Max(1, round);
        }

        private int CalculateRoundClearGold(int round)
        {
            return roundClearBaseGold +
                Mathf.Max(0, round) * roundClearPerRoundGold +
                victoryStreak * victoryStreakGoldBonus +
                roundGoldBonus;
        }

        private void ResolveFateRoundClear(bool bossRound)
        {
            if (!enableFateIntervention)
            {
                return;
            }

            AddFateGauge(fateGaugeOnRoundClear, "라운드 클리어");
            if (MaxLife > 0 && (float)life / MaxLife <= earlyLowLifeRecoveryRatio)
            {
                AddFateGauge(fateGaugeOnLowLife, "낮은 생명력");
            }

            int repay = Mathf.Max(0, fateDebtRepayPerRound) + (bossRound ? Mathf.Max(0, fateDebtRepayPerBossRound) : 0);
            RepayFateDebt(repay, bossRound ? "보스 라운드 클리어" : "라운드 클리어");
        }

        private CharacterDefinition SelectSummonDefinition(out bool earlyPitySummon)
        {
            earlyPitySummon = false;
            int summonRateRound = GetSummonRateRound();
            CharacterDefinition selected;
            if (ShouldForceEarlyPitySummon(summonRateRound))
            {
                CharacterDefinition guaranteed = GetEarlyPityCharacter();
                if (guaranteed != null)
                {
                    earlyPitySummon = true;
                    selected = guaranteed;
                    return ApplyFateSummonIntervention(selected);
                }
            }

            selected = characterDatabase.GetRandomSummonableCharacter(summonRateRound, true);
            return ApplyFateSummonIntervention(selected);
        }

        private CharacterDefinition ApplyFateSummonIntervention(CharacterDefinition selected)
        {
            if (!enableFateIntervention || characterDatabase == null || selected == null)
            {
                return selected;
            }

            CharacterDefinition result = selected;
            bool manipulated = false;

            if (fateGradeLockSummonsRemaining > 0 && (int)result.grade < (int)fateGradeLockMinimum)
            {
                CharacterDefinition lockedGrade = characterDatabase.GetRandomCharacterByGrade(fateGradeLockMinimum, true);
                if (lockedGrade != null)
                {
                    result = lockedGrade;
                    manipulated = true;
                }
            }

            if (fateNormalBanSummonsRemaining > 0 && result.grade == CharacterGrade.Normal)
            {
                CharacterDefinition rareOrBetter = characterDatabase.GetRandomCharacterByGrade(CharacterGrade.Rare, true)
                    ?? characterDatabase.GetRandomCharacterByGrade(CharacterGrade.Epic, true)
                    ?? characterDatabase.GetRandomSummonableCharacter(GetSummonRateRound(), true);
                if (rareOrBetter != null)
                {
                    result = rareOrBetter;
                    manipulated = true;
                }
            }

            if (fateGradeLockSummonsRemaining > 0)
            {
                fateGradeLockSummonsRemaining--;
                if (fateGradeLockSummonsRemaining <= 0)
                {
                    fateGradeLockMinimum = CharacterGrade.Normal;
                }
            }

            if (fateNormalBanSummonsRemaining > 0)
            {
                fateNormalBanSummonsRemaining--;
            }

            if (manipulated)
            {
                AddRunHighlightCard("확률 조작", CharacterGradeUtility.GetDisplayName(result.grade) + " " + result.displayName);
            }

            return result;
        }

        private bool ShouldForceEarlyPitySummon(int summonRateRound)
        {
            int pityCount = Mathf.Max(1, earlyPitySummonCount);
            if (enableBadLuckInsurance)
            {
                pityCount = Mathf.Max(pityCount, Mathf.Max(1, badLuckInsuranceSummonThreshold) + 2);
            }

            return enableEarlyRunFunPacing &&
                !earlyRunMomentTriggered &&
                summonRateRound <= Mathf.Max(1, earlyFunRoundLimit) &&
                earlySummonAttempts + 1 >= pityCount;
        }

        private CharacterDefinition GetEarlyPityCharacter()
        {
            CharacterGrade preferredGrade = UnityEngine.Random.value <= earlyPityEpicChance
                ? CharacterGrade.Epic
                : earlyPityMinimumGrade;
            CharacterDefinition definition = characterDatabase.GetRandomCharacterByGrade(preferredGrade, true);
            if (definition != null)
            {
                return definition;
            }

            return characterDatabase.GetRandomCharacterByGrade(earlyPityMinimumGrade, true);
        }

        private void RegisterSummonExcitement(CharacterDefinition summon, bool earlyPitySummon, DefenderUnit spawnedUnit)
        {
            earlySummonAttempts++;
            if (summon == null)
            {
                return;
            }

            int summonRateRound = GetSummonRateRound();
            bool highGrade = (int)summon.grade >= (int)CharacterGrade.Rare;
            if (!highGrade)
            {
                AddFateGauge(fateGaugeOnLowSummon, "저점 소환");
            }

            TrackBadLuckInsurance(summon, summonRateRound);
            if (highGrade)
            {
                earlyRunMomentTriggered = true;
                PlaySummonJackpotPresentation(summon, spawnedUnit, earlyPitySummon, true);
            }

            if (!enableEarlyRunFunPacing || summonRateRound > Mathf.Max(1, earlyFunRoundLimit))
            {
                return;
            }

            if (!highGrade && earlyPitySummon)
            {
                OnBannerRequested?.Invoke("초반 찬스 소환!  " + CharacterGradeUtility.GetDisplayName(summon.grade) + " " + summon.displayName, summon.accentColor, 2.4f);
                RuntimeCameraShake.Request(0.055f, 0.16f);
            }
        }

        private void TrackBadLuckInsurance(CharacterDefinition summon, int summonRateRound)
        {
            int requiredLowRolls = Mathf.Max(4, badLuckInsuranceSummonThreshold);
            int maxTrackedSummons = Mathf.Max(requiredLowRolls, Mathf.Max(6, badLuckInsuranceLastSummon));
            if (!enableBadLuckInsurance ||
                badLuckInsuranceOffered ||
                badLuckInsuranceResolved ||
                summon == null ||
                summonRateRound > Mathf.Max(1, earlyFunRoundLimit) ||
                earlySummonAttempts > maxTrackedSummons && earlyBadLuckSummons < requiredLowRolls)
            {
                return;
            }

            if ((int)summon.grade > (int)earlyBadLuckBestGrade)
            {
                earlyBadLuckBestGrade = summon.grade;
            }

            if ((int)earlyBadLuckBestGrade >= (int)badLuckInsuranceMinimumGoodGrade)
            {
                badLuckInsuranceResolved = true;
                badLuckInsuranceReason = "초반 고점 확보";
                return;
            }

            earlyBadLuckSummons++;
            if (earlyBadLuckSummons < requiredLowRolls)
            {
                return;
            }

            int earliestRound = Mathf.Max(3, badLuckInsuranceEarliestRound);
            if (summonRateRound < earliestRound)
            {
                badLuckInsuranceReason = "R" + earliestRound + " 이후 초반 저점 복구 대기";
                return;
            }

            badLuckInsuranceOffered = true;
            runInsuranceOffered = true;
            badLuckInsuranceOfferPending = true;
            earlyFallbackRewardGranted = true;
            badLuckInsuranceReason = "R" + summonRateRound + " 초반 " + earlyBadLuckSummons + "회 소환 저점: 추천 보험 1개 지급";
            earlyRunRecoveryRecommended = true;
            earlyRunRecoveryReason = badLuckInsuranceReason;
            earlyRunRecoveryCause = "소환 부족";
            AddRunHighlightCard("보험 발동", "R" + summonRateRound + " 저점 복구 1회");
            OnBannerRequested?.Invoke("운 나쁨 보험 발동!  추천 보험 1개", new Color(1f, 0.76f, 0.24f), 2.8f);
            OnBadLuckInsuranceOffered?.Invoke();
        }

        private void RegisterMergeExcitement(MergeResultInfo mergeResult)
        {
            if ((int)mergeResult.resultGrade >= (int)CharacterGrade.Rare)
            {
                earlyRunMomentTriggered = true;
            }

            if (!firstLegendaryMergeRecorded && mergeResult.resultGrade == CharacterGrade.Legendary)
            {
                firstLegendaryMergeRecorded = true;
                AddRunHighlightCard("첫 전설 합성", CharacterGradeUtility.GetDisplayName(mergeResult.resultGrade) + " " + mergeResult.resultCharacterName);
            }

            if (mergeResult.isFinalMerge || mergeResult.resultGrade == CharacterGrade.Transcendent)
            {
                AddRunHighlightCard("초월 완성", CharacterGradeUtility.GetDisplayName(mergeResult.resultGrade) + " " + mergeResult.resultCharacterName);
            }

            if ((int)mergeResult.resultGrade >= (int)CharacterGrade.Rare)
            {
                string label = mergeResult.isFinalMerge
                    ? "초월 완성!"
                    : (int)mergeResult.resultGrade >= (int)CharacterGrade.Epic
                        ? (CurrentRound <= Mathf.Max(1, earlyFunRoundLimit) ? "초반 대박 합성!" : "대박 합성!")
                        : (CurrentRound <= Mathf.Max(1, earlyFunRoundLimit) ? "첫 레어 합성!" : "레어 합성!");
                bool majorMerge = mergeResult.isFinalMerge || (int)mergeResult.resultGrade >= (int)CharacterGrade.Epic;
                AddRunHighlightCard(
                    TrimHighlightTitle(label),
                    CharacterGradeUtility.GetDisplayName(mergeResult.resultGrade) + " " + mergeResult.resultCharacterName);
                OnBannerRequested?.Invoke(label + "  " + CharacterGradeUtility.GetDisplayName(mergeResult.resultGrade) + " " + mergeResult.resultCharacterName, mergeResult.resultColor, ShortenSummonMergePresentation(mergeResult.isFinalMerge ? 3.2f : majorMerge ? 2.7f : 2.3f));
                RuntimeGameFeel.ShowJackpotReveal(
                    label,
                    CharacterGradeUtility.GetDisplayName(mergeResult.resultGrade),
                    mergeResult.resultCharacterName,
                    mergeResult.resultColor,
                    mergeResult.isFinalMerge ? "초월 완성 / 전투 판도 변화" : majorMerge ? "합성 성공 / 전력 상승" : "첫 고점 / 초반 전력 상승",
                    ShortenSummonMergePresentation(mergeResult.isFinalMerge ? 3.0f : majorMerge ? 2.45f : 2.05f));

                if (mergeResult.isFinalMerge)
                {
                    RuntimeCameraShake.Request(0.22f, 0.48f);
                }
                else
                {
                    RuntimeCameraShake.Request((int)mergeResult.resultGrade >= (int)CharacterGrade.Legendary ? 0.15f : majorMerge ? 0.105f : 0.075f, majorMerge ? 0.30f : 0.20f);
                }
            }
        }

        private void RegisterGrantedUnitExcitement(CharacterDefinition definition, DefenderUnit spawnedUnit)
        {
            if (definition != null && (int)definition.grade >= (int)CharacterGrade.Rare)
            {
                RecordFirstRarePlusRound(definition);
                earlyRunMomentTriggered = true;
                PlaySummonJackpotPresentation(definition, spawnedUnit, false);
            }
        }

        private void PlaySummonJackpotPresentation(CharacterDefinition definition, DefenderUnit spawnedUnit, bool earlyPitySummon, bool spawnSoundAlreadyPlayed = false)
        {
            if (definition == null || (int)definition.grade < (int)CharacterGrade.Rare)
            {
                return;
            }

            bool ultimate = definition.grade == CharacterGrade.Transcendent;
            bool visualMajor = ultimate || (int)definition.grade >= (int)CharacterGrade.Epic;
            bool legendarySound = definition.grade == CharacterGrade.Legendary || definition.grade == CharacterGrade.Mythic;
            string gradeName = CharacterGradeUtility.GetDisplayName(definition.grade);
            string prefix = ultimate
                ? "초월 소환!"
                : visualMajor ? "대박 소환!" : earlyPitySummon ? "초반 찬스 소환!" : "희귀 소환!";

            AddRunHighlightCard(TrimHighlightTitle(prefix), gradeName + " " + definition.displayName);
            OnBannerRequested?.Invoke(prefix + "  " + gradeName + " " + definition.displayName, definition.accentColor, ShortenSummonMergePresentation(ultimate ? 3.3f : visualMajor ? 2.8f : 2.35f));
            RuntimeGameFeel.ShowJackpotReveal(prefix, gradeName, definition.displayName, definition.accentColor, BuildJackpotUnitDetail(definition, earlyPitySummon), ShortenSummonMergePresentation(ultimate ? 3.05f : visualMajor ? 2.6f : 2.15f));

            Vector3 position = spawnedUnit != null ? spawnedUnit.transform.position : transform.position;
            if (definition.grade == CharacterGrade.Mythic || ultimate)
            {
                RuntimeGameFeel.PlayHighGradeSummonVfx(position, definition.accentColor, definition.grade);
            }

            if (ultimate)
            {
                RuntimeAudioUtility.PlayJackpotUltimate();
                RuntimeGameFeel.PlayJackpotPulse(position, definition.accentColor, 2.10f, 0.22f, 0.48f, 0.12f, 0.16f, 4);
            }
            else if (definition.grade == CharacterGrade.Mythic)
            {
                RuntimeAudioUtility.PlayMythicSpawn();
                RuntimeGameFeel.PlayJackpotPulse(position, definition.accentColor, 1.85f, 0.17f, 0.38f, 0.16f, 0.13f, 4);
            }
            else if (legendarySound)
            {
                RuntimeAudioUtility.PlayJackpotMajor();
                RuntimeGameFeel.PlayJackpotPulse(position, definition.accentColor, 1.55f, 0.15f, 0.34f, 0.18f, 0.12f, 3);
            }
            else
            {
                if (!spawnSoundAlreadyPlayed)
                {
                    RuntimeAudioUtility.PlayDiceAppear();
                }

                RuntimeGameFeel.PlayJackpotPulse(position, definition.accentColor, 1.18f, 0.09f, 0.22f, 0.30f, 0.075f, 2);
            }
        }

        private static float ShortenSummonMergePresentation(float duration)
        {
            return Mathf.Max(0.8f, duration - 1f);
        }

        private CharacterGrade SelectMergeAssistGrade()
        {
            CharacterGrade[] mergeGrades =
            {
                CharacterGrade.Legendary,
                CharacterGrade.Epic,
                CharacterGrade.Rare,
                CharacterGrade.Normal
            };

            for (int i = 0; i < mergeGrades.Length; i++)
            {
                if (CountUnitsOfGrade(mergeGrades[i]) >= 2)
                {
                    return mergeGrades[i];
                }
            }

            for (int i = 0; i < mergeGrades.Length; i++)
            {
                if (CountUnitsOfGrade(mergeGrades[i]) == 1)
                {
                    return mergeGrades[i];
                }
            }

            return CharacterGrade.Normal;
        }

        private static string BuildJackpotUnitDetail(CharacterDefinition definition, bool earlyPitySummon)
        {
            if (definition == null || definition.stats == null)
            {
                return earlyPitySummon ? "초반 보정 / 즉시 전력" : "즉시 전력";
            }

            int attack = Mathf.RoundToInt(definition.stats.attackPower);
            int burst = Mathf.RoundToInt(definition.stats.attackPower * 3f);
            string prefix = earlyPitySummon ? "초반 보정" : "즉시 전력";
            return prefix + " / 공격 " + attack + " / 기대딜 " + burst;
        }

        private void RegisterEarlyGoldExcitement(int amount)
        {
            if (!enableEarlyRunFunPacing ||
                earlyRunMomentTriggered ||
                CurrentRound > Mathf.Max(1, earlyFunRoundLimit) ||
                amount < Mathf.Max(1, earlyFallbackGoldReward))
            {
                return;
            }

            earlyRunMomentTriggered = true;
        }

        private void ResolveEarlyRunFallback(int completedRound)
        {
            if (!enableEarlyRunFunPacing ||
                earlyFallbackRewardGranted ||
                earlyRunMomentTriggered ||
                completedRound < Mathf.Max(1, earlyFallbackRewardRound))
            {
                return;
            }

            earlyFallbackRewardGranted = true;
            bool grantedUnit = TryGrantRandomUnitByGrade(earlyFallbackRewardGrade);
            if (grantedUnit)
            {
                earlyRunMomentTriggered = true;
                OnBannerRequested?.Invoke("초반 보급!  다음 선택지가 열렸어요", new Color(0.40f, 0.86f, 1f), 2.4f);
                RuntimeCameraShake.Request(0.04f, 0.14f);
                return;
            }

            if (earlyFallbackGoldReward > 0)
            {
                Gold += earlyFallbackGoldReward;
                earlyRunMomentTriggered = true;
                OnBannerRequested?.Invoke("초반 보급!  +" + earlyFallbackGoldReward + "G", new Color(1f, 0.80f, 0.30f), 2.2f);
            }
        }

        private void AnnounceEarlyCrisisRound(int round)
        {
            if (!enableEarlyRunFunPacing || round != Mathf.Max(1, earlyCrisisRound))
            {
                return;
            }

            OnBannerRequested?.Invoke("위기 라운드!  보스 전 배치와 합성을 점검하세요", new Color(1f, 0.58f, 0.24f), 2.6f);
            RuntimeCameraShake.Request(0.05f, 0.18f);
        }

        private void ResolveEarlyBossPrepReward(int completedRound)
        {
            if (!enableEarlyRunFunPacing ||
                earlyBossPrepRewardGranted ||
                completedRound < Mathf.Max(1, earlyBossPrepRewardRound) ||
                earlyBossPrepGoldReward <= 0)
            {
                return;
            }

            earlyBossPrepRewardGranted = true;
            Gold += earlyBossPrepGoldReward;
            OnBannerRequested?.Invoke("보스 대비 보급!  +" + earlyBossPrepGoldReward + "G", new Color(1f, 0.86f, 0.32f), 2.4f);
        }

        private void TryApplyFirstBossSummonRushBonus(int round, bool bossRound)
        {
            if (!enableFirstBossSummonRushBonus ||
                firstBossSummonRushBonusGranted ||
                !bossRound ||
                round != Mathf.Max(1, firstBossSummonRushRound) ||
                boardManager == null)
            {
                return;
            }

            int summonCount = Mathf.Max(0, earlySummonAttempts);
            int mergeCount = Mathf.Max(0, runMergeCount);
            bool enoughSummons = summonCount >= Mathf.Max(1, firstBossSummonRushMinSummons);
            bool enoughMerges = mergeCount >= Mathf.Max(1, firstBossSummonRushMinMerges);
            if (!enoughSummons && !enoughMerges)
            {
                return;
            }

            float summonProgress = Mathf.Clamp01((summonCount - Mathf.Max(1, firstBossSummonRushMinSummons)) / 12f);
            float mergeProgress = Mathf.Clamp01((mergeCount - Mathf.Max(1, firstBossSummonRushMinMerges)) / 6f);
            float intensity = Mathf.Max(summonProgress, mergeProgress);
            float attackBonus = Mathf.Max(0f, firstBossSummonRushAttackBonus);
            float bossDamageBonus = Mathf.Lerp(
                Mathf.Max(0f, firstBossSummonRushBossDamageBonus),
                Mathf.Max(firstBossSummonRushBossDamageBonus, firstBossSummonRushMaxBossDamageBonus),
                intensity);

            DefenderUnit[] defenders = boardManager.GetAliveDefenders();
            int applied = 0;
            for (int i = 0; i < defenders.Length; i++)
            {
                if (defenders[i] == null)
                {
                    continue;
                }

                defenders[i].AddAttackPowerBonus(attackBonus);
                defenders[i].AddBossDamageBonus(bossDamageBonus);
                applied++;
            }

            if (applied <= 0)
            {
                return;
            }

            firstBossSummonRushBonusGranted = true;
            AddRunHighlightCard("R10 소환 압축", "소환 " + summonCount + " / 합성 " + mergeCount + " / 보스 피해 +" + Mathf.RoundToInt(bossDamageBonus * 100f) + "%");
            OnBannerRequested?.Invoke("R10 소환 압축  보스 피해 +" + Mathf.RoundToInt(bossDamageBonus * 100f) + "%", new Color(1f, 0.74f, 0.22f), 1.4f);
        }

        private void BeginEarlyRoundTelemetry(int round)
        {
            if (!enableEarlyRoundTelemetry || round <= 0 || round > Mathf.Max(1, earlyTelemetryRoundLimit))
            {
                return;
            }

            currentRoundStartTime = Time.time;
            currentRoundStartGold = Gold;
            currentRoundSummonCount = pendingRoundSummonCount;
            currentRoundMergeCount = pendingRoundMergeCount;
            currentRoundHadMerge = pendingRoundHadMerge;
            currentRoundHighestMergeGrade = pendingRoundHighestMergeGrade;
            pendingRoundSummonCount = 0;
            pendingRoundMergeCount = 0;
            pendingRoundHadMerge = false;
            pendingRoundHighestMergeGrade = CharacterGrade.Normal;
            earlyRunTelemetrySummary = "R" + round + " 계측 중";
            earlyRunTuningHint = "클리어 시간, 골드, 소환, 합성 기록 중";
        }

        private void CompleteEarlyRoundTelemetry(int round, bool bossRound, bool cleared)
        {
            if (!enableEarlyRoundTelemetry || round <= 0 || round > Mathf.Max(1, earlyTelemetryRoundLimit))
            {
                return;
            }

            float elapsed = currentRoundStartTime > 0f ? Mathf.Max(0f, Time.time - currentRoundStartTime) : 0f;
            EarlyRoundTelemetrySnapshot snapshot = new EarlyRoundTelemetrySnapshot
            {
                round = round,
                cleared = cleared,
                bossRound = bossRound,
                clearTimeSeconds = elapsed,
                startGold = currentRoundStartGold,
                endGold = Gold,
                endLife = life,
                endLife01 = MaxLife > 0 ? Mathf.Clamp01((float)life / MaxLife) : 1f,
                summons = currentRoundSummonCount,
                merges = currentRoundMergeCount,
                hadMerge = currentRoundHadMerge,
                highestMergeGrade = currentRoundHighestMergeGrade,
                bossHealthRemaining01 = bossRound ? GetRemainingBossHealth01() : -1f
            };

            RecordAutomaticRunRecapMoments(snapshot);
            earlyRoundTelemetry.Add(snapshot);
            while (earlyRoundTelemetry.Count > Mathf.Max(1, earlyTelemetryRoundLimit))
            {
                earlyRoundTelemetry.RemoveAt(0);
            }

            RecordEarlyRunLogSample(snapshot);
            earlyRunTelemetrySummary = BuildEarlyRoundTelemetrySummary(snapshot);
            earlyRunTuningHint = BuildEarlyRoundTuningHint(snapshot);
            UpdateEarlyRunRecoveryRecommendation(snapshot);
            RequestEarlyRoundTuningBanner(snapshot);
            TryRecordEarlyRunTuningLog(snapshot, snapshot.round >= EarlyRunRequiredRoundCount);
            Debug.Log("[EarlyRunTelemetry] " + earlyRunTelemetrySummary + " / " + earlyRunLogCoverageSummary + " / 회복상점 " + earlyRunRecoveryOfferCount + "회 / " + earlyRunTuningHint);
        }

        private void RecordAutomaticRunRecapMoments(EarlyRoundTelemetrySnapshot snapshot)
        {
            if (snapshot == null || !snapshot.cleared)
            {
                return;
            }

            if (snapshot.bossRound && snapshot.clearTimeSeconds <= 1.05f)
            {
                AddRunHighlightCard("보스 1초 클리어", "ROUND " + snapshot.round + " / " + snapshot.clearTimeSeconds.ToString("0.0") + "s");
            }

            if (!lifeOneClutchRecorded && snapshot.endLife == 1)
            {
                lifeOneClutchRecorded = true;
                AddRunHighlightCard("생명력 1 역전", "ROUND " + snapshot.round + " 클리어");
            }

            if (!fateSurvivalClutchRecorded && runFateSurvivalCount > 0 && snapshot.endLife <= Mathf.Max(2, Mathf.CeilToInt(MaxLife * 0.18f)))
            {
                fateSurvivalClutchRecorded = true;
                AddRunHighlightCard("운명으로 생존", "ROUND " + snapshot.round + " / 생명 " + snapshot.endLife + " 남김");
            }
        }

        private void RecordRoundDefeatMoment(int round, bool bossRound)
        {
            if (runDefeatMomentRecorded)
            {
                return;
            }

            runDefeatMomentRecorded = true;
            int target = Mathf.Max(1, RoundTargetCount);
            int defeated = Mathf.Clamp(currentRoundResolvedMonsters, 0, target);
            float progress = target > 0 ? Mathf.Clamp01((float)defeated / target) : 0f;
            bool nearMiss = round >= 8 || progress >= 0.72f || bossRound;

            if (nearMiss)
            {
                string title = "R" + Mathf.Max(1, round) + " 아슬아슬 실패";
                string detail = bossRound
                    ? "보스 재도전 / 운명 생존 먼저"
                    : "처치 " + defeated + "/" + target + " / 운명 개입 후보";
                AddRunHighlightCard(title, detail);
            }
            else
            {
                AddRunHighlightCard("ROUND " + Mathf.Max(1, round) + " 실패", "소환 수/합성/운명 개입 보강 필요");
            }

            if (runFateSurvivalCount > 0)
            {
                AddRunHighlightCard("운명으로 버팀", "생존 개입 " + runFateSurvivalCount + "회 / R" + Mathf.Max(1, round) + "까지 도달");
            }
        }

        private void RecordEarlyRoundSummon(CharacterDefinition definition)
        {
            if (!enableEarlyRoundTelemetry || CurrentRound > Mathf.Max(1, earlyTelemetryRoundLimit))
            {
                return;
            }

            if (IsRoundRunning)
            {
                currentRoundSummonCount++;
            }
            else
            {
                pendingRoundSummonCount++;
            }

            RecordFirstRarePlusRound(definition);
        }

        private void RecordFirstRarePlusRound(CharacterDefinition definition)
        {
            if (!enableEarlyRoundTelemetry ||
                firstRarePlusRound >= 0 ||
                definition == null ||
                (int)definition.grade < (int)CharacterGrade.Rare)
            {
                return;
            }

            firstRarePlusRound = ResolveEarlyMomentRound();
            UpdateEarlyRunLogCoverageSummary();
        }

        private int ResolveEarlyMomentRound()
        {
            int round = CurrentRound > 0 ? CurrentRound : GetSummonRateRound();
            return Mathf.Clamp(round, 1, Mathf.Max(1, earlyTelemetryRoundLimit));
        }

        private static string FormatEarlyMomentRound(int round)
        {
            return round > 0 ? "R" + round : "-";
        }

        private void RecordEarlyRunLogSample(EarlyRoundTelemetrySnapshot snapshot)
        {
            if (snapshot == null || snapshot.round <= 0 || snapshot.round > Mathf.Max(1, earlyTelemetryRoundLimit))
            {
                return;
            }

            if (snapshot.round == EarlyRunRequiredRoundCount && snapshot.bossRound)
            {
                earlyRunR10BossHealthRemaining01 = Mathf.Clamp01(snapshot.bossHealthRemaining01);
                runR10BossHealthRemaining01 = earlyRunR10BossHealthRemaining01;
            }

            UpdateEarlyRunLogCoverageSummary();
        }

        private void TryRecordEarlyRunTuningLog(EarlyRoundTelemetrySnapshot snapshot, bool reachedRound10)
        {
            if (!enableEarlyRoundTelemetry || earlyRunTuningLogRecorded)
            {
                return;
            }

            int reachedRound = ResolveEarlyRunTuningReachedRound(snapshot);
            bool recordPartialRun = gameOverRaised && reachedRound > 0 && reachedRound < EarlyRunRequiredRoundCount;
            bool recordReachedRun = reachedRound10 || reachedRound >= EarlyRunRequiredRoundCount;
            if (!recordPartialRun && !recordReachedRun)
            {
                return;
            }

            EnsureEarlyRunTuningLogStoreLoaded();

            float r10BossHealth = -1f;
            if (snapshot != null && snapshot.round == EarlyRunRequiredRoundCount && snapshot.bossRound)
            {
                r10BossHealth = Mathf.Clamp01(snapshot.bossHealthRemaining01);
            }
            else if (recordReachedRun && CurrentRound == EarlyRunRequiredRoundCount && IsBossRound)
            {
                r10BossHealth = Mathf.Clamp01(GetRemainingBossHealth01());
            }
            else if (runR10BossHealthRemaining01 >= 0f)
            {
                r10BossHealth = Mathf.Clamp01(runR10BossHealthRemaining01);
            }

            EarlyRunTuningLogEntry entry = new EarlyRunTuningLogEntry
            {
                ticksUtc = System.DateTime.UtcNow.Ticks,
                reachedRound = Mathf.Clamp(reachedRound, 1, EarlyRunRequiredRoundCount),
                reachedRound10 = recordReachedRun,
                clearedRound10 = recordReachedRun && snapshot != null && snapshot.round >= EarlyRunRequiredRoundCount && snapshot.cleared,
                firstRarePlusRound = firstRarePlusRound,
                firstMergeRound = firstMergeRound,
                insuranceOffered = runInsuranceOffered || badLuckInsuranceOffered,
                insuranceClaimed = runInsuranceClaimed,
                r3BoosterOffered = runR3BoosterOffered,
                r3BoosterPurchased = runR3BoosterPurchased,
                recoveryShopOffered = runRecoveryShopOffered,
                recoveryShopPurchased = runRecoveryShopPurchased,
                fateContractUsed = runFateContractCount > 0,
                fateInterventionUsed = runFateInterventionCount > 0,
                fateDebt = fateDebt,
                r10BossHealthRemaining01 = r10BossHealth,
                endLife = snapshot != null ? snapshot.endLife : life,
                endGold = snapshot != null ? snapshot.endGold : Gold,
                boardUnits = BoardUnitCount,
                bossKills = totalBossKills,
                runScore = CalculateRunPerformanceScore(),
                recommendedBuildName = RecommendedBuildName
            };

            earlyRunTuningLogStore.entries.Add(entry);
            while (earlyRunTuningLogStore.entries.Count > EarlyRunTuningLogMaxEntries)
            {
                earlyRunTuningLogStore.entries.RemoveAt(0);
            }

            earlyRunTuningLogRecorded = true;
            SaveEarlyRunTuningLogStore();
            UpdateEarlyRunLogCoverageSummary();
            Debug.Log("[EarlyRunTelemetry] 판 단위 표본 저장 " + earlyRunLogCoverageSummary);
        }

        private int ResolveEarlyRunTuningReachedRound(EarlyRoundTelemetrySnapshot snapshot)
        {
            if (snapshot != null)
            {
                return snapshot.round;
            }

            if (CurrentRound > 0)
            {
                return CurrentRound;
            }

            if (earlyRoundTelemetry.Count > 0)
            {
                return earlyRoundTelemetry[earlyRoundTelemetry.Count - 1].round;
            }

            return 0;
        }

        private void EnsureEarlyRunTuningLogStoreLoaded()
        {
            if (earlyRunTuningLogStore == null)
            {
                LoadEarlyRunTuningLogStore();
            }
        }

        private void LoadEarlyRunTuningLogStore()
        {
            earlyRunTuningLogStore = null;
            string json = PlayerPrefs.GetString(EarlyRunTuningLogPrefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    earlyRunTuningLogStore = JsonUtility.FromJson<EarlyRunTuningLogStore>(json);
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning("[EarlyRunTelemetry] 누적 로그 로드 실패: " + exception.Message);
                }
            }

            if (earlyRunTuningLogStore == null)
            {
                earlyRunTuningLogStore = new EarlyRunTuningLogStore();
            }

            if (earlyRunTuningLogStore.entries == null)
            {
                earlyRunTuningLogStore.entries = new List<EarlyRunTuningLogEntry>();
            }

            while (earlyRunTuningLogStore.entries.Count > EarlyRunTuningLogMaxEntries)
            {
                earlyRunTuningLogStore.entries.RemoveAt(0);
            }
        }

        private void SaveEarlyRunTuningLogStore()
        {
            EnsureEarlyRunTuningLogStoreLoaded();
            PlayerPrefs.SetString(EarlyRunTuningLogPrefsKey, JsonUtility.ToJson(earlyRunTuningLogStore));
            PlayerPrefs.Save();
        }

        private void UpdateEarlyRunLogCoverageSummary()
        {
            EnsureEarlyRunTuningLogStoreLoaded();
            int target = Mathf.Max(1, earlyTelemetryTargetSampleCount);
            List<EarlyRunTuningLogEntry> entries = earlyRunTuningLogStore.entries;
            int entryCount = entries != null ? entries.Count : 0;

            if (entryCount <= 0)
            {
                string r10BossHp = earlyRunR10BossHealthRemaining01 >= 0f
                    ? Mathf.RoundToInt(earlyRunR10BossHealthRemaining01 * 100f) + "%"
                    : "-";

                earlyRunLogCoverageSummary = "R1~R10 로그 0/" + target
                    + " / 첫R+ " + FormatEarlyMomentRound(firstRarePlusRound)
                    + " / 첫합 " + FormatEarlyMomentRound(firstMergeRound)
                    + " / 보험 0%"
                    + " / R3부스터 " + earlyRunR3BoosterPurchaseCount + "/" + earlyRunR3BoosterOfferCount + " " + FormatRate(earlyRunR3BoosterPurchaseCount, earlyRunR3BoosterOfferCount)
                    + " / 회복상점 " + earlyRunRecoveryShopPurchaseCount + "/" + earlyRunRecoveryShopOfferCount + " " + FormatRate(earlyRunRecoveryShopPurchaseCount, earlyRunRecoveryShopOfferCount)
                    + " / 운명개입 " + runFateInterventionCount + "회"
                    + " / R10보스HP " + r10BossHp;
                return;
            }

            int firstRareCount = 0;
            int firstRareSum = 0;
            int firstMergeCount = 0;
            int firstMergeSum = 0;
            int insuranceOfferedCount = 0;
            int r3OfferCount = 0;
            int r3PurchaseCount = 0;
            int recoveryOfferCount = 0;
            int recoveryPurchaseCount = 0;
            int fateContractUseCount = 0;
            int fateInterventionUseCount = 0;
            int fateDebtSum = 0;
            int r10BossHpCount = 0;
            float r10BossHpSum = 0f;

            for (int i = 0; i < entryCount; i++)
            {
                EarlyRunTuningLogEntry entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                if (entry.firstRarePlusRound > 0)
                {
                    firstRareCount++;
                    firstRareSum += entry.firstRarePlusRound;
                }

                if (entry.firstMergeRound > 0)
                {
                    firstMergeCount++;
                    firstMergeSum += entry.firstMergeRound;
                }

                if (entry.insuranceOffered)
                {
                    insuranceOfferedCount++;
                }

                if (entry.r3BoosterOffered)
                {
                    r3OfferCount++;
                }

                if (entry.r3BoosterPurchased)
                {
                    r3PurchaseCount++;
                }

                if (entry.recoveryShopOffered)
                {
                    recoveryOfferCount++;
                }

                if (entry.recoveryShopPurchased)
                {
                    recoveryPurchaseCount++;
                }

                if (entry.fateContractUsed)
                {
                    fateContractUseCount++;
                }

                if (entry.fateInterventionUsed)
                {
                    fateInterventionUseCount++;
                }

                fateDebtSum += Mathf.Max(0, entry.fateDebt);

                if (entry.r10BossHealthRemaining01 >= 0f)
                {
                    r10BossHpCount++;
                    r10BossHpSum += Mathf.Clamp01(entry.r10BossHealthRemaining01);
                }
            }

            earlyRunLogCoverageSummary = "R1~R10 로그 " + FormatSampleProgress(entryCount, target)
                + " / 첫R+ " + FormatAverageRound(firstRareSum, firstRareCount)
                + " / 첫합 " + FormatAverageRound(firstMergeSum, firstMergeCount)
                + " / 보험 " + insuranceOfferedCount + "/" + entryCount + " " + FormatRate(insuranceOfferedCount, entryCount)
                + " / R3부스터 " + r3PurchaseCount + "/" + r3OfferCount + " " + FormatRate(r3PurchaseCount, r3OfferCount)
                + " / 회복상점 " + recoveryPurchaseCount + "/" + recoveryOfferCount + " " + FormatRate(recoveryPurchaseCount, recoveryOfferCount)
                + " / 운명계약 " + fateContractUseCount + "/" + entryCount + " " + FormatRate(fateContractUseCount, entryCount)
                + " / 운명개입 " + fateInterventionUseCount + "/" + entryCount + " " + FormatRate(fateInterventionUseCount, entryCount)
                + " / 빚 " + FormatAverageInt(fateDebtSum, entryCount)
                + " / R10보스HP " + FormatAveragePercent(r10BossHpSum, r10BossHpCount);
        }

        private static string FormatSampleProgress(int count, int target)
        {
            return count >= target ? target + "/" + target + "+" : count + "/" + target;
        }

        private static string FormatAverageRound(int roundSum, int count)
        {
            if (count <= 0)
            {
                return "-";
            }

            return "평균R" + ((float)roundSum / count).ToString("0.0");
        }

        private static string FormatAveragePercent(float sum, int count)
        {
            if (count <= 0)
            {
                return "-";
            }

            return "평균" + Mathf.RoundToInt(Mathf.Clamp01(sum / count) * 100f) + "%";
        }

        private static string FormatAverageInt(int sum, int count)
        {
            if (count <= 0)
            {
                return "-";
            }

            return "평균" + Mathf.RoundToInt((float)sum / count);
        }

        private static string FormatRate(int value, int total)
        {
            if (total <= 0)
            {
                return "0%";
            }

            return Mathf.RoundToInt(Mathf.Clamp01((float)value / total) * 100f) + "%";
        }

        private int GetEarlyRunLogSampleCount()
        {
            EnsureEarlyRunTuningLogStoreLoaded();
            return earlyRunTuningLogStore != null && earlyRunTuningLogStore.entries != null
                ? earlyRunTuningLogStore.entries.Count
                : 0;
        }

        private string BuildEarlyRunActionSummary()
        {
            int target = Mathf.Max(1, earlyTelemetryTargetSampleCount);
            int count = GetEarlyRunLogSampleCount();
            if (count < target)
            {
                return "초반 검증: R1~R10 로그 " + count + "/" + target + "회";
            }

            if (earlyRunR3BoosterOfferCount > 0 && earlyRunR3BoosterPurchaseCount <= 0)
            {
                return "초반 검증: R3 선택지 매력 확인";
            }

            if (earlyRunRecoveryShopOfferCount > 0 && earlyRunRecoveryShopPurchaseCount <= 0)
            {
                return "초반 검증: 회복상점 선택률 확인";
            }

            if (earlyRunR10BossHealthRemaining01 >= highBossHealthWarningRatio)
            {
                return "초반 검증: R10 보스 압박 완화";
            }

            return "초반 검증: 사건 밀도 유지";
        }

        private string BuildEarlyRunTuningDecisionSummary()
        {
            EnsureEarlyRunTuningLogStoreLoaded();
            int target = Mathf.Max(1, earlyTelemetryTargetSampleCount);
            List<EarlyRunTuningLogEntry> entries = earlyRunTuningLogStore != null ? earlyRunTuningLogStore.entries : null;
            int totalEntries = entries != null ? entries.Count : 0;
            if (totalEntries <= 0)
            {
                return "실측 루프 0/" + target + "회: 첫 Rare+, 첫 합성, 상점, 보스 HP 기록 대기";
            }

            int startIndex = Mathf.Max(0, totalEntries - target);
            int sampleCount = 0;
            int firstRareCount = 0;
            int firstRareSum = 0;
            int firstMergeCount = 0;
            int firstMergeSum = 0;
            int reachedRound10Count = 0;
            int clearedRound10Count = 0;
            int r3OfferCount = 0;
            int r3PurchaseCount = 0;
            int recoveryOfferCount = 0;
            int recoveryPurchaseCount = 0;
            int fateUseCount = 0;
            int r10BossHpCount = 0;
            float r10BossHpSum = 0f;

            for (int i = startIndex; i < totalEntries; i++)
            {
                EarlyRunTuningLogEntry entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                sampleCount++;
                if (entry.firstRarePlusRound > 0)
                {
                    firstRareCount++;
                    firstRareSum += entry.firstRarePlusRound;
                }

                if (entry.firstMergeRound > 0)
                {
                    firstMergeCount++;
                    firstMergeSum += entry.firstMergeRound;
                }

                if (entry.reachedRound10)
                {
                    reachedRound10Count++;
                }

                if (entry.clearedRound10)
                {
                    clearedRound10Count++;
                }

                if (entry.r3BoosterOffered)
                {
                    r3OfferCount++;
                }

                if (entry.r3BoosterPurchased)
                {
                    r3PurchaseCount++;
                }

                if (entry.recoveryShopOffered)
                {
                    recoveryOfferCount++;
                }

                if (entry.recoveryShopPurchased)
                {
                    recoveryPurchaseCount++;
                }

                if (entry.fateContractUsed || entry.fateInterventionUsed)
                {
                    fateUseCount++;
                }

                if (entry.r10BossHealthRemaining01 >= 0f)
                {
                    r10BossHpCount++;
                    r10BossHpSum += Mathf.Clamp01(entry.r10BossHealthRemaining01);
                }
            }

            if (sampleCount < target)
            {
                return "실측 루프 " + sampleCount + "/" + target + "회: R1~R10 반복 필요";
            }

            List<string> actions = new List<string>();
            float firstRareAvg = firstRareCount > 0 ? (float)firstRareSum / firstRareCount : 99f;
            float firstMergeAvg = firstMergeCount > 0 ? (float)firstMergeSum / firstMergeCount : 99f;
            float reachedRate = sampleCount > 0 ? (float)reachedRound10Count / sampleCount : 0f;
            float clearRate = sampleCount > 0 ? (float)clearedRound10Count / sampleCount : 0f;
            float r3PurchaseRate = r3OfferCount > 0 ? (float)r3PurchaseCount / r3OfferCount : 0f;
            float recoveryPurchaseRate = recoveryOfferCount > 0 ? (float)recoveryPurchaseCount / recoveryOfferCount : 0f;
            float fateUseRate = sampleCount > 0 ? (float)fateUseCount / sampleCount : 0f;
            float r10BossHpAvg = r10BossHpCount > 0 ? r10BossHpSum / r10BossHpCount : 0f;

            if (firstRareAvg > 2.0f)
            {
                actions.Add("Rare+가 늦음");
            }

            if (firstMergeAvg > 3.0f)
            {
                actions.Add("첫 합성이 늦음");
            }

            if (reachedRate < 0.75f)
            {
                actions.Add("R10 도달률 낮음");
            }

            if (clearRate < 0.45f)
            {
                actions.Add("R10 클리어율 낮음");
            }

            if (r10BossHpAvg >= highBossHealthWarningRatio)
            {
                actions.Add("보스 HP 과다");
            }

            if (r3OfferCount > 0 && r3PurchaseRate < 0.35f)
            {
                actions.Add("R3 상점 매력 부족");
            }

            if (recoveryOfferCount > 0 && recoveryPurchaseRate < 0.25f)
            {
                actions.Add("회복 상점 선택률 낮음");
            }

            if (fateUseRate < 0.30f)
            {
                actions.Add("운명 버튼 노출 부족");
            }

            if (actions.Count <= 0)
            {
                actions.Add("초반 손맛 유지");
            }

            return "실측 판정 " + sampleCount + "/" + target + ": " + string.Join(" / ", actions);
        }

        private void RecordEarlyRoundMerge(CharacterGrade grade)
        {
            if (!enableEarlyRoundTelemetry || CurrentRound > Mathf.Max(1, earlyTelemetryRoundLimit))
            {
                return;
            }

            runMergeCount++;

            if (firstMergeRound < 0)
            {
                firstMergeRound = ResolveEarlyMomentRound();
                UpdateEarlyRunLogCoverageSummary();
            }

            if (IsRoundRunning)
            {
                currentRoundMergeCount++;
                currentRoundHadMerge = true;
                if ((int)grade > (int)currentRoundHighestMergeGrade)
                {
                    currentRoundHighestMergeGrade = grade;
                }
            }
            else
            {
                pendingRoundMergeCount++;
                pendingRoundHadMerge = true;
                if ((int)grade > (int)pendingRoundHighestMergeGrade)
                {
                    pendingRoundHighestMergeGrade = grade;
                }
            }
        }

        private string BuildEarlyRoundTelemetrySummary(EarlyRoundTelemetrySnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "1~10R 계측 대기";
            }

            string mergeText = snapshot.hadMerge ? CharacterGradeUtility.GetDisplayName(snapshot.highestMergeGrade) : "없음";
            string summary = "R" + snapshot.round + " " + snapshot.clearTimeSeconds.ToString("0") + "s"
                + " / G" + snapshot.endGold
                + " / HP " + snapshot.endLife + "/" + MaxLife
                + " / 소환 " + snapshot.summons
                + " / 합성 " + mergeText
                + " / 첫R+ " + FormatEarlyMomentRound(firstRarePlusRound)
                + " / 첫합 " + FormatEarlyMomentRound(firstMergeRound);

            if (snapshot.bossRound)
            {
                summary += " / 보스HP " + Mathf.RoundToInt(Mathf.Clamp01(snapshot.bossHealthRemaining01) * 100f) + "%";
            }

            return summary;
        }

        private int CalculateRunPerformanceScore()
        {
            float lifeRatio = MaxLife > 0 ? Mathf.Clamp01((float)Life / MaxLife) : 0f;
            int score = Mathf.RoundToInt(CurrentRound * 12f);
            score += Mathf.RoundToInt(Mathf.Clamp(TotalDamageDealt / 650f, 0f, 130f));
            score += Mathf.Clamp(bestSynergyCount * 18, 0, 90);
            score += Mathf.Clamp(bestKillCombo * 3, 0, 75);
            score += Mathf.Clamp(criticalHitCount, 0, 50);
            score += Mathf.RoundToInt(lifeRatio * 60f);

            if (earlyRunRecoveryRecommended)
            {
                score -= 20;
            }

            return Mathf.Max(0, score);
        }

        private static string ResolveRunPerformanceGrade(int score)
        {
            if (score >= 280)
            {
                return "SSS";
            }

            if (score >= 230)
            {
                return "SS";
            }

            if (score >= 180)
            {
                return "S";
            }

            if (score >= 135)
            {
                return "A";
            }

            return score >= 90 ? "B" : "C";
        }

        private string BuildRunResultRecapSummary()
        {
            string tileSummary = CurrentTileContributionSummary;
            if (string.IsNullOrWhiteSpace(tileSummary))
            {
                tileSummary = "타일 기여 없음";
            }

            return "딜러 TOP " + DamageLeaderboardSummary
                + "  |  시너지 " + BestSynergySummary
                + "\n타일 " + tileSummary
                + "  |  보스 " + BossPressureSummary
                + "\n로그 " + earlyRunLogCoverageSummary;
        }

        private string BuildRunHighlightCardsSummary()
        {
            List<string> cards = new List<string>(3);
            for (int i = runHighlightCards.Count - 1; i >= 0 && cards.Count < 3; i--)
            {
                if (!string.IsNullOrWhiteSpace(runHighlightCards[i]))
                {
                    cards.Add(runHighlightCards[i]);
                }
            }

            if (cards.Count < 3 && topDamageHeroDamage > 0f)
            {
                cards.Add("MVP 딜러 | " + topDamageHeroName + " " + Mathf.RoundToInt(topDamageHeroDamage).ToString("N0") + "딜");
            }

            if (cards.Count < 3)
            {
                cards.Add("보스 목표 | " + BossPressureSummary);
            }

            if (cards.Count < 3)
            {
                cards.Add("다음 빌드 | " + RecommendedBuildName);
            }

            while (cards.Count < 3)
            {
                cards.Add("초반 목표 | 같은 태그 2개와 레어 이상 딜러 확보");
            }

            return "이번 판의 대박 순간 3개"
                + "\nCARD 1  " + cards[0]
                + "\nCARD 2  " + cards[1]
                + "\nCARD 3  " + cards[2];
        }

        private List<string> CollectRunResultCards(int targetCount)
        {
            int safeTarget = Mathf.Max(1, targetCount);
            List<string> cards = new List<string>(safeTarget);
            for (int i = runHighlightCards.Count - 1; i >= 0 && cards.Count < safeTarget; i--)
            {
                if (!string.IsNullOrWhiteSpace(runHighlightCards[i]))
                {
                    cards.Add(runHighlightCards[i]);
                }
            }

            if (cards.Count < safeTarget && topDamageHeroDamage > 0f)
            {
                cards.Add("MVP 딜러 | " + topDamageHeroName + " " + Mathf.RoundToInt(topDamageHeroDamage).ToString("N0") + "딜");
            }

            if (cards.Count < safeTarget)
            {
                cards.Add("보스 목표 | " + BossPressureSummary);
            }

            if (cards.Count < safeTarget)
            {
                cards.Add("다음 빌드 | " + RecommendedBuildName);
            }

            while (cards.Count < safeTarget)
            {
                cards.Add("초반 목표 | 같은 태그 2개와 레어 이상 딜러 확보");
            }

            return cards;
        }

        private string BuildRunResultFocusSummary()
        {
            List<string> cards = CollectRunResultCards(3);
            return "이번 판 사건 3개"
                + "\nCARD 1  " + cards[0]
                + "\nCARD 2  " + cards[1]
                + "\nCARD 3  " + cards[2];
        }

        private string BuildLatestRunMomentSummary()
        {
            for (int i = runHighlightCards.Count - 1; i >= 0; i--)
            {
                if (!string.IsNullOrWhiteSpace(runHighlightCards[i]))
                {
                    return "사건 " + CompactRunResultText(runHighlightCards[i], 28);
                }
            }

            if (totalBossKills > 0)
            {
                return "사건 보스 처치 " + totalBossKills + "회";
            }

            return "사건 다음 판 대박 조합 노리기";
        }

        private string BuildRunResultNextCompactSummary()
        {
            int upgradeableCards = GetUpgradeableCardCount();
            string buildAction = CompactRunResultText(BuildRunNextGoalHeadline(), 34);
            string growthAction = upgradeableCards > 0
                ? "강화 " + upgradeableCards + "명 가능"
                : earnedGrowthCurrency > 0
                    ? "다이아 +" + earnedGrowthCurrency + " 성장"
                    : "카드 조각 목표";
            string pressureAction = earlyRunRecoveryRecommended
                ? "복구 상점"
                : RoundsUntilNextBoss > 0 && RoundsUntilNextBoss <= 2
                    ? "보스 대비"
                    : "상점 보충";
            string fateAction = CompactRunResultText(FateResultSummary, 24);
            string shareAction = "공유 " + BuildRunShareCode();

            return buildAction + "  |  " + shareAction
                + "\n" + growthAction + "  |  " + pressureAction + "  |  " + fateAction;
        }

        private string BuildFateInterventionSummary()
        {
            if (!enableFateIntervention)
            {
                return "운명 비활성";
            }

            string pending = fateGradeLockSummonsRemaining > 0
                ? " 잠금 " + CharacterGradeUtility.GetDisplayName(fateGradeLockMinimum) + "x" + fateGradeLockSummonsRemaining
                : fateNormalBanSummonsRemaining > 0
                    ? " 일반금지x" + fateNormalBanSummonsRemaining
                    : fateForceNextShop
                        ? " 상점확정"
                        : string.Empty;
            return "운명 " + fateGauge + "/" + Mathf.Max(1, maxFateGauge) + "  빚 " + fateDebt + pending;
        }

        private string BuildFateResultSummary()
        {
            if (!enableFateIntervention)
            {
                return "운명 비활성";
            }

            if (runFateInterventionCount > 0)
            {
                return BuildFateCostBenefitSummary();
            }

            int nextCost = Mathf.Min(
                Mathf.Min(Mathf.Max(1, fateShopRerollGaugeCost), Mathf.Max(1, fateNormalBanGaugeCost)),
                Mathf.Max(1, fateForceShopGaugeCost));
            return fateGauge >= nextCost ? "운명 개입 준비" : "운명 " + fateGauge + "/" + nextCost;
        }

        private bool CanSpendFateGauge(int cost)
        {
            return enableFateIntervention && fateGauge >= Mathf.Max(0, cost);
        }

        private bool IsFateSurvivalCrisisActive()
        {
            if (!enableFateIntervention || !CanUseFateSurvival || MaxLife <= 0)
            {
                return false;
            }

            int nextPressureRound = IsRoundRunning ? CurrentRound : CurrentRound + 1;
            if (nextPressureRound < 5 || nextPressureRound > 8)
            {
                return false;
            }

            return (float)Life / MaxLife <= 0.50f;
        }

        private string BuildReadableFateInterventionSummary()
        {
            if (!enableFateIntervention)
            {
                return "운명 비활성";
            }

            string pending = fateGradeLockSummonsRemaining > 0
                ? " / Rare+ " + fateGradeLockSummonsRemaining
                : fateNormalBanSummonsRemaining > 0
                    ? " / 일반 제외 " + fateNormalBanSummonsRemaining
                    : fateForceNextShop
                        ? " / 다음 상점 확정"
                        : string.Empty;

            return "운명 " + fateGauge + "/" + Mathf.Max(1, maxFateGauge)
                + " | 빚 " + fateDebt + "/" + Mathf.Max(1, maxFateDebt)
                + pending;
        }

        private string BuildReadableFateResultSummary()
        {
            if (!enableFateIntervention)
            {
                return "운명 비활성";
            }

            if (runFateInterventionCount > 0)
            {
                return BuildReadableFateCostBenefitSummary();
            }

            int nextCost = Mathf.Min(
                Mathf.Min(Mathf.Max(1, fateNormalBanGaugeCost), Mathf.Max(1, fateForceShopGaugeCost)),
                Mathf.Max(1, fateSurvivalGaugeCost));
            return fateGauge >= nextCost ? "운명 개입 준비" : "운명 " + fateGauge + "/" + nextCost;
        }

        private string BuildReadableFateHudSummary()
        {
            if (!enableFateIntervention)
            {
                return "운명 비활성";
            }

            string pending = fateGradeLockSummonsRemaining > 0
                ? " / Rare+ x" + fateGradeLockSummonsRemaining
                : fateNormalBanSummonsRemaining > 0
                    ? " / 일반 제외 x" + fateNormalBanSummonsRemaining
                    : fateForceNextShop
                        ? " / 상점 확정"
                        : string.Empty;
            return "운명 " + fateGauge + "/" + Mathf.Max(1, maxFateGauge)
                + " | 빚 " + fateDebt + "/" + Mathf.Max(1, maxFateDebt)
                + pending;
        }

        private string BuildReadableFateCostBenefitSummary()
        {
            if (!enableFateIntervention)
            {
                return "운명 비활성";
            }

            if (runFateInterventionCount <= 0)
            {
                return "이득: 위기 때 판 살리기 | 대가: 상점가/보스HP 소폭 증가";
            }

            List<string> gains = new List<string>();
            if (runFateSurvivalCount > 0)
            {
                gains.Add("생존 " + runFateSurvivalCount);
            }

            if (runFateShopRerollCount > 0)
            {
                gains.Add("리롤 " + runFateShopRerollCount);
            }

            if (runFateGradeLockCount > 0)
            {
                gains.Add("Rare+ " + runFateGradeLockCount);
            }

            if (runFateNormalBanCount > 0)
            {
                gains.Add("일반 제외 " + runFateNormalBanCount);
            }

            if (runFateForcedShopCount > 0)
            {
                gains.Add("상점 확정 " + runFateForcedShopCount);
            }

            if (runFateContractCount > 0)
            {
                gains.Add("계약 " + runFateContractCount);
            }

            if (gains.Count <= 0)
            {
                gains.Add("개입 " + runFateInterventionCount);
            }

            List<string> costs = new List<string>();
            if (runFateDebtAdded > 0)
            {
                costs.Add("빚 +" + runFateDebtAdded);
            }

            if (runFateDebtRepaid > 0)
            {
                costs.Add("상환 -" + runFateDebtRepaid);
            }

            if (runFateShopCostPenaltyGold > 0)
            {
                costs.Add("상점 +" + runFateShopCostPenaltyGold + "G");
            }

            int bossPenaltyPercent = Mathf.RoundToInt(Mathf.Clamp01((float)runPeakFateDebt / Mathf.Max(1, maxFateDebt)) * maxFateDebtBossHealthBonus * 100f);
            if (bossPenaltyPercent > 0)
            {
                costs.Add("보스HP +" + bossPenaltyPercent + "%");
            }

            if (costs.Count <= 0)
            {
                costs.Add("대가 없음");
            }

            return "이득: " + string.Join(" / ", gains) + " | 대가: " + string.Join(", ", costs);
        }

        private string BuildFateHudSummary()
        {
            if (!enableFateIntervention)
            {
                return "운명 비활성";
            }

            string pending = fateGradeLockSummonsRemaining > 0
                ? " / Rare+ lock x" + fateGradeLockSummonsRemaining
                : fateNormalBanSummonsRemaining > 0
                    ? " / No Normal x" + fateNormalBanSummonsRemaining
                    : fateForceNextShop
                        ? " / Shop reserved"
                        : string.Empty;
            return "운명 " + fateGauge + "/" + Mathf.Max(1, maxFateGauge)
                + " | 빚 " + fateDebt + "/" + Mathf.Max(1, maxFateDebt)
                + pending;
        }

        private static string BuildFateActionLabel(string title, int gaugeCost, int debtCost)
        {
            return title + "\n" + Mathf.Max(0, gaugeCost) + "F / +" + Mathf.Max(0, debtCost);
        }

        private string BuildFateCostBenefitSummary()
        {
            if (!enableFateIntervention)
            {
                return "운명 비활성";
            }

            if (runFateInterventionCount <= 0)
            {
                return "운명 미사용 | 다음 판 목표: 상점 강제 등장권 써보기";
            }

            List<string> gains = new List<string>();
            if (runFateShopRerollCount > 0)
            {
                gains.Add("리롤 " + runFateShopRerollCount + "회");
            }

            if (runFateGradeLockCount > 0)
            {
                gains.Add("레어 보정 " + runFateGradeLockCount + "회");
            }

            if (runFateNormalBanCount > 0)
            {
                gains.Add("일반 금지 " + runFateNormalBanCount + "회");
            }

            if (runFateForcedShopCount > 0)
            {
                gains.Add("상점 강제 " + runFateForcedShopCount + "회");
            }

            if (runFateContractCount > 0)
            {
                gains.Add("계약 " + runFateContractCount + "회");
            }

            if (gains.Count <= 0)
            {
                gains.Add("운명 개입 " + runFateInterventionCount + "회");
            }

            List<string> costs = new List<string>();
            if (runFateDebtAdded > 0)
            {
                costs.Add("빚 +" + runFateDebtAdded);
            }

            if (runFateDebtRepaid > 0)
            {
                costs.Add("상환 -" + runFateDebtRepaid);
            }

            if (runFateShopCostPenaltyGold > 0)
            {
                costs.Add("상점가 +" + runFateShopCostPenaltyGold + "G");
            }

            int bossPenaltyPercent = Mathf.RoundToInt(Mathf.Clamp01((float)runPeakFateDebt / Mathf.Max(1, maxFateDebt)) * maxFateDebtBossHealthBonus * 100f);
            if (bossPenaltyPercent > 0)
            {
                costs.Add("보스 HP +" + bossPenaltyPercent + "%");
            }

            if (costs.Count <= 0)
            {
                costs.Add("대가 없음");
            }

            return "이득: " + string.Join(" / ", gains) + " | 대가: " + string.Join(", ", costs);
        }

        private string BuildSeasonReplayDigestSummary()
        {
            string latestMoment = CompactRunResultText(BuildLatestRunMomentSummary(), 24);
            return "협동 보스 " + RunBossScore.ToString("N0")
                + " / MVP " + RunMvpName
                + " | 리플레이 " + BuildRunShareCode()
                + " " + latestMoment;
        }

        private string BuildRunShareCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + CurrentRound;
                hash = hash * 31 + RunPerformanceScore;
                hash = hash * 31 + totalBossKills;
                hash = hash * 31 + bestKillCombo;
                hash = hash * 31 + runFateInterventionCount;
                hash = hash * 31 + fateDebt;
                if (runClipEvents.Count > 0)
                {
                    string last = runClipEvents[runClipEvents.Count - 1];
                    for (int i = 0; i < last.Length; i++)
                    {
                        hash = hash * 31 + last[i];
                    }
                }

                int code = Mathf.Abs(hash % 100000);
                return "#" + code.ToString("D5");
            }
        }

        private static string CompactRunResultText(string value, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "기록 없음";
            }

            string compact = value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace(" | ", " ")
                .Trim();
            while (compact.Contains("  "))
            {
                compact = compact.Replace("  ", " ");
            }

            if (compact.Length <= maxChars)
            {
                return compact;
            }

            int sliceLength = Mathf.Max(1, maxChars - 3);
            return compact.Substring(0, sliceLength) + "...";
        }

        private void AddRunHighlightCard(string title, string detail)
        {
            string safeTitle = string.IsNullOrWhiteSpace(title) ? "대박 순간" : title.Trim();
            string safeDetail = string.IsNullOrWhiteSpace(detail) ? "기록 없음" : detail.Trim();
            string entry = safeTitle + " | " + safeDetail;
            if (runHighlightCards.Contains(entry))
            {
                return;
            }

            runHighlightCards.Add(entry);
            RecordRunClipEvent(entry);
            while (runHighlightCards.Count > 8)
            {
                runHighlightCards.RemoveAt(0);
            }
        }

        private void RecordRunClipEvent(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                return;
            }

            if (runClipEvents.Contains(entry))
            {
                return;
            }

            runClipEvents.Add(entry);
            while (runClipEvents.Count > RunClipMaxEvents)
            {
                runClipEvents.RemoveAt(0);
            }
        }

        private static string TrimHighlightTitle(string title)
        {
            return string.IsNullOrWhiteSpace(title) ? "대박 순간" : title.Trim().TrimEnd('!', ' ');
        }

        private string BuildRunNextActionSummary()
        {
            int upgradeableCards = GetUpgradeableCardCount();
            string collectionAction = upgradeableCards > 0
                ? "도감 강화: 강화 가능 " + upgradeableCards + "명 / 바로 성장 확인"
                : earnedGrowthCurrency > 0
                    ? "도감 강화 보상: 다이아 +" + earnedGrowthCurrency + " / 주력 카드 성장 확인"
                    : "도감 강화 보상: 주력 딜러 카드 조각 목표 확인";

            string shopAction = earlyRunRecoveryRecommended
                ? "상점 목표: " + earlyRunRecoveryCause + " / " + earlyRunRecoveryReason
                : "상점 목표: 부족한 등급과 카드 조각 보충";

            string bossAction;
            if (RoundsUntilNextBoss > 0 && RoundsUntilNextBoss <= 2)
            {
                bossAction = "다음 보스 대비: " + ComposeBuildGoalGuideSummary();
            }
            else if (totalBossSkillCasts > 0 && totalBossTileDamage < Mathf.Max(1f, totalBossSkillDamage * 0.35f))
            {
                bossAction = "다음 보스 대비: 보스 타일과 군중 제어 우선";
            }
            else if (topDamageHeroDamage <= 0f)
            {
                bossAction = "다음 보스 대비: 핵심 딜러 먼저 확보";
            }
            else
            {
                bossAction = "다음 보스 대비: " + RecommendedDeckSummary;
            }

            string buildAction = "다음 판 목표: " + RecommendedBuildName;
            return buildAction + "\n" + collectionAction + "\n" + shopAction + "\n" + bossAction + "\n" + BuildEarlyRunActionSummary() + "\n" + DailyFortuneSummary;
        }

        private string BuildRunNextGoalHeadline()
        {
            int upgradeableCards = GetUpgradeableCardCount();
            string collectionHook = upgradeableCards > 0
                ? "강화 가능 " + upgradeableCards + "명"
                : earnedGrowthCurrency > 0
                ? "도감 강화 가능"
                : "도감 카드 조각 목표";

            string buildName = totalBossKills <= 0 && CurrentRound >= 4
                ? "보스 사냥덱"
                : RecommendedBuildName;

            if (BadLuckInsuranceAvailable)
            {
                return "다음 판 목표: " + buildName + " / 보험 선택으로 초반 복구";
            }

            int target = Mathf.Max(1, earlyTelemetryTargetSampleCount);
            int sampleCount = GetEarlyRunLogSampleCount();
            if (sampleCount < target)
            {
                return "다음 판 목표: R1~R10 로그 " + sampleCount + "/" + target;
            }

            return "다음 판 목표: " + buildName + " / " + collectionHook;
        }

        private int GetUpgradeableCardCount()
        {
            return OutgameProgressionSystem.Active != null ? OutgameProgressionSystem.Active.CountUpgradeableCards() : 0;
        }

        private int CalculateRunBossScore()
        {
            int score = totalBossKills * 520;
            score += Mathf.RoundToInt(Mathf.Clamp(totalBossTileDamage, 0f, 6000f) * 0.42f);
            score += Mathf.RoundToInt(Mathf.Clamp(TotalDamageDealt, 0f, 40000f) * 0.035f);
            score += Mathf.Clamp(bestSynergyCount * 35, 0, 180);
            score += Mathf.Clamp(bestKillCombo * 4, 0, 160);
            return Mathf.Max(0, score);
        }

        private string ComposeBuildGoalGuideSummary()
        {
            if (CanMergeUltimate())
            {
                return "초월 준비 완료: 초월 조합을 실행하세요.";
            }

            if (RoundsUntilNextBoss > 0 && RoundsUntilNextBoss <= 2)
            {
                if (roundTopDamageHeroDamage > 0f)
                {
                    return "보스 대비: 보스 타일 + " + roundTopDamageHeroName + " 유지";
                }

                if (topDamageHeroDamage > 0f)
                {
                    return "보스 대비: 보스 타일 + " + topDamageHeroName + " 유지";
                }

                return "보스 대비: 보스 타일과 군중제어 유닛 확보";
            }

            string ultimateStatus = GetUltimateMergeActionStatus();
            if (!string.IsNullOrWhiteSpace(ultimateStatus) && ultimateStatus != "초월 레시피 없음")
            {
                return "초월 목표: " + ultimateStatus;
            }

            if (currentSynergyCount > 0 && currentSynergyCount < 3)
            {
                return "시너지 목표: " + currentSynergyTitle + " 3개 이상";
            }

            if (topDamageHeroDamage > 0f)
            {
                return "딜러 목표: " + topDamageHeroName + " 태그 유지";
            }

            return "초반 목표: 같은 태그 2개와 레어 이상 딜러 확보";
        }

        private string BuildCurrentDangerSummary()
        {
            if (IsBossRound && IsRoundRunning)
            {
                return "보스전: " + CurrentBossPressureSummary;
            }

            if (EarlyRunRecoveryRecommended)
            {
                return "회복 필요: " + earlyRunRecoveryReason;
            }

            if (EmptySlotCount <= 0)
            {
                return "자리 부족: 합성 먼저";
            }

            if (RoundsUntilNextBoss > 0 && RoundsUntilNextBoss <= 2)
            {
                return "보스 임박 " + RoundsUntilNextBoss + "R";
            }

            if (CurrentRound > 0 && CurrentRound <= Mathf.Max(1, earlyFunRoundLimit) && !earlyRunMomentTriggered)
            {
                return "초반 고점 대기";
            }

            return "안정";
        }

        private string BuildBossPressureSummary(bool total)
        {
            int castCount = total ? totalBossSkillCasts : currentRoundBossSkillCasts;
            if (castCount <= 0)
            {
                return "압박 없음";
            }

            string skillName = total ? lastBossSkill : currentRoundLastBossSkill;
            if (string.IsNullOrWhiteSpace(skillName))
            {
                skillName = "보스 스킬";
            }

            int affectedTargets = total ? totalBossAffectedTargets : currentRoundBossAffectedTargets;
            int goldDrained = total ? totalBossGoldDrained : currentRoundBossGoldDrained;
            int manaBurnTargets = total ? totalBossManaBurnTargets : currentRoundBossManaBurnTargets;
            int executions = total ? totalBossExecutions : currentRoundBossExecutions;
            int fortifyCount = total ? totalBossFortifyCount : currentRoundBossFortifyCount;
            int rallyTargets = total ? totalBossRallyTargets : currentRoundBossRallyTargets;
            float skillDamage = total ? totalBossSkillDamage : currentRoundBossSkillDamage;
            float responseDamage = total ? totalBossTileDamage : currentRoundBossTileDamage;

            string summary = skillName + " " + castCount + "회";
            if (affectedTargets > 0)
            {
                summary += " / 영향 " + affectedTargets;
            }

            if (skillDamage > 0f)
            {
                summary += " / 피해 " + Mathf.RoundToInt(skillDamage).ToString("N0");
            }

            if (goldDrained > 0)
            {
                summary += " / 골드 -" + goldDrained;
            }

            if (manaBurnTargets > 0)
            {
                summary += " / 마나 " + manaBurnTargets;
            }

            if (executions > 0)
            {
                summary += " / 즉사 " + executions;
            }

            if (fortifyCount > 0)
            {
                summary += " / 강화 " + fortifyCount;
            }

            if (rallyTargets > 0)
            {
                summary += " / 집결 " + rallyTargets;
            }

            if (responseDamage > 0f)
            {
                summary += " / 대응 " + Mathf.RoundToInt(responseDamage).ToString("N0");
            }

            return summary;
        }

        private string BuildEarlyRoundTuningHint(EarlyRoundTelemetrySnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "초반 런 데이터 대기";
            }

            if (!snapshot.cleared)
            {
                return snapshot.bossRound ? "보스 실패: 보스 타일/제어/레어 이상 확보 필요" : "라운드 실패: 초반 화력 또는 소환 수 부족";
            }

            if (snapshot.bossRound && snapshot.bossHealthRemaining01 >= highBossHealthWarningRatio)
            {
                return "보스 체력이 많이 남음: 보스 대응 타일과 고등급 딜러 확인";
            }

            if (snapshot.round <= Mathf.Max(1, earlyTelemetryRoundLimit) && snapshot.endLife01 <= earlyLowLifeRecoveryRatio)
            {
                return "생명력 압박: 회복 상점/보급 선택지가 필요";
            }

            if (snapshot.clearTimeSeconds >= slowEarlyClearSeconds)
            {
                return "클리어 시간이 김: 초반 고점 보급 또는 몬스터 체력 점검";
            }

            if (snapshot.round <= 5 && snapshot.summons < lowEarlySummonThreshold && snapshot.endGold <= lowEarlyGoldThreshold)
            {
                return "소환/골드 모두 낮음: 3~5R 선택지 보상이 필요";
            }

            if (snapshot.hadMerge && (int)snapshot.highestMergeGrade >= (int)CharacterGrade.Rare && snapshot.clearTimeSeconds < slowEarlyClearSeconds * 0.65f)
            {
                return "폭발 구간: 레어 이상 합성 체감이 좋음";
            }

            return "흐름 안정: 현재 초반 곡선 유지";
        }

        private void UpdateEarlyRunRecoveryRecommendation(EarlyRoundTelemetrySnapshot snapshot)
        {
            if (snapshot == null || snapshot.round > Mathf.Max(1, earlyTelemetryRoundLimit))
            {
                return;
            }

            if (NeedsEarlyRunRecovery(snapshot))
            {
                earlyRunRecoveryRecommended = true;
                earlyRunRecoveryReason = earlyRunTuningHint;
                earlyRunRecoveryCause = ResolveEarlyRunRecoveryCause(snapshot);
                return;
            }

            if (snapshot.round >= 7 && snapshot.cleared && snapshot.clearTimeSeconds < slowEarlyClearSeconds * 0.78f)
            {
                earlyRunRecoveryRecommended = false;
                earlyRunRecoveryReason = "초반 런 안정";
                earlyRunRecoveryCause = "흐름 안정";
            }
        }

        private string ResolveEarlyRunRecoveryCause(EarlyRoundTelemetrySnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "소환 부족";
            }

            if (snapshot.bossRound && (!snapshot.cleared || snapshot.bossHealthRemaining01 >= highBossHealthWarningRatio))
            {
                return "보스 HP 위험";
            }

            if (snapshot.endLife01 <= earlyLowLifeRecoveryRatio)
            {
                return "생명력 압박";
            }

            if (snapshot.round <= 5 && (snapshot.summons < lowEarlySummonThreshold || snapshot.endGold <= lowEarlyGoldThreshold))
            {
                return "소환 부족";
            }

            if (!snapshot.cleared)
            {
                return "소환 부족";
            }

            return snapshot.clearTimeSeconds >= slowEarlyClearSeconds && snapshot.summons >= lowEarlySummonThreshold
                ? "생명력 압박"
                : "소환 부족";
        }

        private bool NeedsEarlyRunRecovery(EarlyRoundTelemetrySnapshot snapshot)
        {
            if (snapshot == null)
            {
                return false;
            }

            if (!snapshot.cleared)
            {
                return true;
            }

            if (snapshot.bossRound && snapshot.bossHealthRemaining01 >= highBossHealthWarningRatio)
            {
                return true;
            }

            if (snapshot.clearTimeSeconds >= slowEarlyClearSeconds)
            {
                return true;
            }

            if (snapshot.round <= Mathf.Max(1, earlyTelemetryRoundLimit) && snapshot.endLife01 <= earlyLowLifeRecoveryRatio)
            {
                return true;
            }

            return snapshot.round <= 5 &&
                snapshot.summons < lowEarlySummonThreshold &&
                snapshot.endGold <= lowEarlyGoldThreshold;
        }

        private void RequestEarlyRoundTuningBanner(EarlyRoundTelemetrySnapshot snapshot)
        {
            if (snapshot == null || snapshot.round > Mathf.Max(1, earlyTelemetryRoundLimit))
            {
                return;
            }

            if (!snapshot.cleared || snapshot.clearTimeSeconds >= slowEarlyClearSeconds || (snapshot.bossRound && snapshot.bossHealthRemaining01 >= highBossHealthWarningRatio))
            {
                OnBannerRequested?.Invoke("초반 계측  " + earlyRunTuningHint, new Color(1f, 0.72f, 0.28f), 2.6f);
            }
        }

        private float GetRemainingBossHealth01()
        {
            IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
            float highest = 0f;
            bool foundBoss = false;
            for (int i = 0; i < monsters.Count; i++)
            {
                MonsterUnit monster = monsters[i];
                if (monster == null || !monster.IsBoss || monster.MaxHealth <= 0f)
                {
                    continue;
                }

                foundBoss = true;
                highest = Mathf.Max(highest, Mathf.Clamp01(monster.CurrentHealth / monster.MaxHealth));
            }

            return foundBoss ? highest : 0f;
        }

        private void ReportRoundCombatRecap(bool bossRound)
        {
            if (topDamageHeroDamage <= 0f)
            {
                return;
            }

            string message = "전투 리캡  " + RoundDamageLeaderboardSummary;
            string tileSummary = CurrentTileContributionSummary;
            if (!string.IsNullOrWhiteSpace(tileSummary) && tileSummary != "타일 기여 없음")
            {
                message += "  |  타일 " + tileSummary;
            }

            OnBannerRequested?.Invoke(message, bossRound ? new Color(1f, 0.72f, 0.26f) : new Color(0.40f, 0.92f, 1f), bossRound ? 2.8f : 2.2f);
        }

        private string BuildCurrentTileContributionSummary()
        {
            if (currentRoundTileDamage <= 0f && currentRoundBossTileDamage <= 0f)
            {
                return "타일 기여 없음";
            }

            BoardTileModifierType leadingType = GetLeadingTileDamageType();
            string tileName = leadingType == BoardTileModifierType.None ? "타일" : GetTileModifierDisplayName(leadingType);
            string summary = tileName + " " + Mathf.RoundToInt(currentRoundTileDamage).ToString("N0");
            if (currentRoundBossTileDamage > 0f)
            {
                summary += " / 보스 " + Mathf.RoundToInt(currentRoundBossTileDamage).ToString("N0");
            }

            return summary;
        }

        private static void AddDamageContribution(Dictionary<string, float> table, string heroName, float damage)
        {
            if (table == null || string.IsNullOrWhiteSpace(heroName) || damage <= 0f)
            {
                return;
            }

            if (!table.ContainsKey(heroName))
            {
                table[heroName] = 0f;
            }

            table[heroName] += damage;
        }

        private static string BuildDamageLeaderboardSummary(Dictionary<string, float> table, int maxCount)
        {
            if (table == null || table.Count <= 0)
            {
                return "기록 없음";
            }

            List<KeyValuePair<string, float>> entries = new List<KeyValuePair<string, float>>(table);
            entries.Sort((left, right) => right.Value.CompareTo(left.Value));

            int count = Mathf.Min(Mathf.Max(1, maxCount), entries.Count);
            string summary = string.Empty;
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    summary += " / ";
                }

                summary += (i + 1) + ". " + entries[i].Key + " " + Mathf.RoundToInt(entries[i].Value).ToString("N0");
            }

            return summary;
        }

        private void RecordTileDamageContribution(DefenderUnit source, MonsterUnit target, float damage)
        {
            BoardSlot slot = source != null ? source.CurrentSlot : null;
            if (slot == null || slot.TileModifierType == BoardTileModifierType.None || damage <= 0f)
            {
                return;
            }

            BoardTileModifierType type = slot.TileModifierType;
            currentRoundTileDamage += damage;
            if (!currentRoundTileDamageByType.ContainsKey(type))
            {
                currentRoundTileDamageByType[type] = 0f;
            }

            currentRoundTileDamageByType[type] += damage;
            bool bossTarget = target != null && target.IsBoss;
            if (bossTarget)
            {
                currentRoundBossTileDamage += damage;
                totalBossTileDamage += damage;
            }

            ReportTileHitFeedback(type, bossTarget);
        }

        private void ReportTileHitFeedback(BoardTileModifierType type, bool bossTarget)
        {
            if (!IsRoundRunning || type == BoardTileModifierType.None)
            {
                return;
            }

            if (bossTarget &&
                currentRoundBossTileDamage >= nextBossTileFeedbackDamageThreshold &&
                Time.time >= nextBossTileFeedbackTime)
            {
                nextBossTileFeedbackTime = Time.time + Mathf.Max(0.4f, combatFeedbackCooldown);
                nextBossTileFeedbackDamageThreshold += Mathf.Max(1, Mathf.RoundToInt(bossTileContributionFeedbackStep));
                OnBannerRequested?.Invoke("보스 대응 적중!  " + GetTileModifierDisplayName(type) + " " + Mathf.RoundToInt(currentRoundBossTileDamage).ToString("N0"), new Color(1f, 0.72f, 0.24f), 1.8f);
                return;
            }

            if (currentRoundTileDamage >= nextTileFeedbackDamageThreshold && Time.time >= nextTileFeedbackTime)
            {
                nextTileFeedbackTime = Time.time + Mathf.Max(0.4f, combatFeedbackCooldown);
                nextTileFeedbackDamageThreshold += Mathf.Max(1, Mathf.RoundToInt(tileContributionFeedbackStep));
                OnBannerRequested?.Invoke("타일 적중  " + GetTileModifierDisplayName(type) + " " + Mathf.RoundToInt(currentRoundTileDamage).ToString("N0"), new Color(0.35f, 0.92f, 1f), 1.5f);
            }
        }

        private void ReportSynergyActivationFeedback(int previousCount, string previousTitle)
        {
            if (currentSynergyCount <= 0 || Time.time < nextSynergyFeedbackTime)
            {
                return;
            }

            bool changed = currentSynergyCount > previousCount || !string.Equals(currentSynergyTitle, previousTitle, System.StringComparison.Ordinal);
            if (!changed)
            {
                return;
            }

            nextSynergyFeedbackTime = Time.time + 1.8f;
            OnBannerRequested?.Invoke("시너지 발동  " + CurrentSynergySummary, new Color(0.38f, 1f, 0.74f), 1.8f);
        }

        private void ReportTopDamageFeedback(string heroName, float damage, string previousHeroName, float previousDamage)
        {
            if (!IsRoundRunning || damage < Mathf.Max(1f, topDamageFeedbackStep) || Time.time < nextTopDamageFeedbackTime)
            {
                return;
            }

            bool leaderChanged = previousDamage > 0f && !string.Equals(heroName, previousHeroName, System.StringComparison.Ordinal);
            bool reachedMilestone = damage >= nextTopDamageFeedbackThreshold;
            if (!leaderChanged && !reachedMilestone)
            {
                return;
            }

            nextTopDamageFeedbackTime = Time.time + Mathf.Max(0.4f, combatFeedbackCooldown);
            nextTopDamageFeedbackThreshold = Mathf.Floor(damage / Mathf.Max(1f, topDamageFeedbackStep) + 1f) * Mathf.Max(1f, topDamageFeedbackStep);
            OnBannerRequested?.Invoke((leaderChanged ? "최고 딜러 갱신!  " : "딜러 폭주!  ") + heroName + " " + Mathf.RoundToInt(damage).ToString("N0"), new Color(1f, 0.82f, 0.24f), 1.8f);
        }

        private void ReportRoundTileContribution(bool bossRound)
        {
            if (currentRoundTileDamage < tileContributionBannerMinDamage &&
                currentRoundBossTileDamage < bossTileContributionBannerMinDamage)
            {
                return;
            }

            BoardTileModifierType leadingType = GetLeadingTileDamageType();
            if (leadingType == BoardTileModifierType.None)
            {
                return;
            }

            string tileName = GetTileModifierDisplayName(leadingType);
            if (bossRound && currentRoundBossTileDamage >= bossTileContributionBannerMinDamage)
            {
                OnBannerRequested?.Invoke("보스전 배치 적중!  " + tileName + " 보스 피해 " + Mathf.RoundToInt(currentRoundBossTileDamage).ToString("N0"), new Color(1f, 0.70f, 0.22f), 2.6f);
                return;
            }

            OnBannerRequested?.Invoke("전술 타일 기여  " + tileName + " " + Mathf.RoundToInt(currentRoundTileDamage).ToString("N0") + " 피해", new Color(0.35f, 0.92f, 1f), 2.2f);
        }

        private BoardTileModifierType GetLeadingTileDamageType()
        {
            BoardTileModifierType leadingType = BoardTileModifierType.None;
            float leadingDamage = 0f;
            foreach (KeyValuePair<BoardTileModifierType, float> entry in currentRoundTileDamageByType)
            {
                if (entry.Value <= leadingDamage)
                {
                    continue;
                }

                leadingType = entry.Key;
                leadingDamage = entry.Value;
            }

            return leadingType;
        }

        private string GetTileModifierDisplayName(BoardTileModifierType type)
        {
            switch (type)
            {
                case BoardTileModifierType.AttackSpeed: return "가속 타일";
                case BoardTileModifierType.Mana: return "마나 타일";
                case BoardTileModifierType.Guard: return "수호 타일";
                case BoardTileModifierType.Range: return "사거리 타일";
                case BoardTileModifierType.Overload: return "과부하 타일";
                case BoardTileModifierType.BossHunter: return "보스 타일";
                case BoardTileModifierType.Skill: return "스킬 타일";
                default: return "전술 타일";
            }
        }

        private void ResetRoundTileContribution()
        {
            currentRoundTileDamage = 0f;
            currentRoundBossTileDamage = 0f;
            currentRoundTileDamageByType.Clear();
            currentRoundDamageByHero.Clear();
            roundTopDamageHeroName = "없음";
            roundTopDamageHeroDamage = 0f;
            currentRoundBossSkillCasts = 0;
            currentRoundBossAffectedTargets = 0;
            currentRoundBossGoldDrained = 0;
            currentRoundBossManaBurnTargets = 0;
            currentRoundBossExecutions = 0;
            currentRoundBossFortifyCount = 0;
            currentRoundBossRallyTargets = 0;
            currentRoundBossSkillDamage = 0f;
            currentRoundLastBossSkill = "없음";
            nextTileFeedbackTime = 0f;
            nextBossTileFeedbackTime = 0f;
            nextTileFeedbackDamageThreshold = Mathf.Max(1, Mathf.RoundToInt(tileContributionFeedbackStep));
            nextBossTileFeedbackDamageThreshold = Mathf.Max(1, Mathf.RoundToInt(bossTileContributionFeedbackStep));
        }
    }
}
