using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame;

public class DefenseGameController : MonoBehaviour
{
	private enum FateCardType
	{
		MonsterCrush,
		CombatDraft,
		FullHeal,
		ForbiddenSummon,
		GamblerGold,
		LastBarrier,
		GoldLoan,
		RareMercenaries,
		EpicAdvance,
		MythicLease,
		BlackMarket,
		TimeStop,
		ThunderStrike,
		ManaFlood,
		WallRepair,
		SmugglerRoute,
		LifeForge,
		GradeRigging
	}

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

	[Serializable]
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

	[Serializable]
	private sealed class EarlyRunTuningLogStore
	{
		public List<EarlyRunTuningLogEntry> entries = new List<EarlyRunTuningLogEntry>();
	}

	private const string EarlyRunTuningLogPrefsKey = "DefenseGame.EarlyRunTuningLog.v1";

	private const int EarlyRunTuningLogMaxEntries = 60;

	private const int EarlyRunRequiredRoundCount = 10;

	private const int RunClipMaxEvents = 6;

	[Header("Core References")]
	[SerializeField]
	private CharacterDatabase characterDatabase;

	[SerializeField]
	private MonsterDatabase monsterDatabase;

	[SerializeField]
	private DefenseBoardManager boardManager;

	[SerializeField]
	private RoundManager roundManager;

	private AugmentManager augmentManager;

	[SerializeField]
	private DefenderUnit defaultUnitPrefab;

	[Header("Economy")]
	[SerializeField]
	private int startGold = 36;

	[SerializeField]
	private int summonCost = 10;

	[SerializeField]
	private int summonCostIncreasePerSummon = 1;

	[SerializeField]
	private int earlySummonCostRampRoundLimit = 5;

	[SerializeField]
	private int earlySummonCostIncreasePerSummon = 1;

	[SerializeField]
	private int maxSummonCost = 80;

	[SerializeField]
	private int life = 10;

	[SerializeField]
	private int roundStartGold = 1;

	[SerializeField]
	[Range(0f, 2f)]
	private float roundStartGoldPerRoundMultiplier = 0.4f;

	[SerializeField]
	private int roundClearBaseGold = 5;

	[SerializeField]
	private float roundClearPerRoundGold = 0.75f;

	[SerializeField]
	private int victoryStreakGoldBonus = 0;

	[Header("Unit Selling")]
	[SerializeField]
	private bool enableUnitSelling = true;

	[SerializeField]
	[Range(0.1f, 0.5f)]
	private float unitSellRefundRate = 0.33f;

	[SerializeField]
	private int normalUnitSellBaseValue = 10;

	[SerializeField]
	private int rareUnitSellBaseValue = 16;

	[SerializeField]
	private int epicUnitSellBaseValue = 25;

	[SerializeField]
	private int legendaryUnitSellBaseValue = 40;

	[SerializeField]
	private int mythicUnitSellBaseValue = 64;

	[SerializeField]
	private int transcendentUnitSellBaseValue = 100;

	[Header("Early Run Fun Pacing")]
	[SerializeField]
	private bool enableEarlyRunFunPacing = true;

	[SerializeField]
	private int earlyFunRoundLimit = 5;

	[SerializeField]
	private int earlyFallbackRewardRound = 3;

	[SerializeField]
	private CharacterGrade earlyFallbackRewardGrade = CharacterGrade.Normal;

	[SerializeField]
	private int earlyFallbackGoldReward = 4;

	[SerializeField]
	private int earlyCrisisRound = 5;

	[SerializeField]
	private int earlyBossPrepRewardRound = 4;

	[SerializeField]
	private int earlyBossPrepGoldReward = 0;

	[Header("Lucky Summon Comeback")]
	[SerializeField]
	private bool enableLuckySummonComeback = true;

	[SerializeField]
	private int luckySummonVisibleStreak = 3;

	[SerializeField]
	private int luckySummonNormalStreakThreshold = 7;

	[SerializeField]
	private int luckySummonEarliestRound = 4;

	[SerializeField]
	[Range(1f, 3f)]
	private float luckySummonSafeCostMultiplier = 1.5f;

	[SerializeField]
	[Range(0f, 1f)]
	private float luckySummonJackpotEpicChance = 0.25f;

	[SerializeField]
	[Range(0f, 1f)]
	private float luckySummonJackpotRefundRate = 0.5f;

	[SerializeField]
	private int earlyLeakGraceRoundLimit = 4;

	[SerializeField]
	private int earlyRoundLeakDamageCap = 3;

	[SerializeField]
	private bool enableFirstBossSummonRushBonus = true;

	[SerializeField]
	private int firstBossSummonRushRound = 10;

	[SerializeField]
	private int firstBossSummonRushMinSummons = 34;

	[SerializeField]
	private int firstBossSummonRushMinMerges = 15;

	[SerializeField]
	[Range(0f, 0.5f)]
	private float firstBossSummonRushAttackBonus = 0.06f;

	[SerializeField]
	[Range(0f, 0.8f)]
	private float firstBossSummonRushBossDamageBonus = 0.1f;

	[SerializeField]
	[Range(0f, 1f)]
	private float firstBossSummonRushMaxBossDamageBonus = 0.18f;

	[Header("Combat Readability")]
	[SerializeField]
	private float tileContributionBannerMinDamage = 180f;

	[SerializeField]
	private float bossTileContributionBannerMinDamage = 80f;

	[SerializeField]
	private float tileContributionFeedbackStep = 250f;

	[SerializeField]
	private float bossTileContributionFeedbackStep = 120f;

	[SerializeField]
	private float combatFeedbackCooldown = 2f;

	[SerializeField]
	private float topDamageFeedbackStep = 700f;

	[Header("Early Run Telemetry")]
	[SerializeField]
	private bool enableEarlyRoundTelemetry = true;

	[SerializeField]
	private int earlyTelemetryRoundLimit = 10;

	[SerializeField]
	private float slowEarlyClearSeconds = 54f;

	[SerializeField]
	private int lowEarlyGoldThreshold = 14;

	[SerializeField]
	private int lowEarlySummonThreshold = 2;

	[SerializeField]
	private int earlyTelemetryTargetSampleCount = 20;

	[SerializeField]
	[Range(0f, 1f)]
	private float highBossHealthWarningRatio = 0.3f;

	[SerializeField]
	[Range(0.1f, 1f)]
	private float earlyLowLifeRecoveryRatio = 0.5f;

	[Header("Fate Intervention")]
	[SerializeField]
	private bool enableFateIntervention = true;

	[SerializeField]
	private bool useOneShotFateCard = true;

	[SerializeField]
	private int maxFateGauge = 100;

	[SerializeField]
	[Range(0.02f, 1f)]
	private float fateChoiceTimeScale = 0.1f;

	[SerializeField]
	private int startingFateGauge = 0;

	[SerializeField]
	private int maxFateDebt = 100;

	[SerializeField]
	private int fateGaugeOnLowSummon = 0;

	[SerializeField]
	private int fateGaugeOnRoundClear = 0;

	[SerializeField]
	private int fateGaugeOnBossKill = 0;

	[SerializeField]
	private int fateGaugeOnLowLife = 0;

	[SerializeField]
	private int fateDebtPerContractLife = 14;

	[SerializeField]
	private int fateDebtRepayPerRound = 10;

	[SerializeField]
	private int fateDebtRepayPerBossRound = 10;

	[SerializeField]
	private int fateShopRerollGaugeCost = 16;

	[SerializeField]
	private int fateGradeLockGaugeCost = 18;

	[SerializeField]
	private int fateNormalBanGaugeCost = 16;

	[SerializeField]
	private int fateForceShopGaugeCost = 14;

	[SerializeField]
	private int fateSurvivalGaugeCost = 20;

	[SerializeField]
	private int fateShopRerollDebt = 5;

	[SerializeField]
	private int fateGradeLockDebt = 10;

	[SerializeField]
	private int fateNormalBanDebt = 8;

	[SerializeField]
	private int fateForceShopDebt = 8;

	[SerializeField]
	private int fateSurvivalDebt = 18;

	[SerializeField]
	private int fateSurvivalLifeRecover = 4;

	[SerializeField]
	private int fateSurvivalGold = 12;

	[SerializeField]
	private int fateSurvivalNormalBanSummons = 3;

	[SerializeField]
	private int ultimateRecipeBingoFateGaugeBonus = 0;

	[SerializeField]
	[Range(0f, 0.5f)]
	private float maxFateDebtShopCostPenalty = 0.2f;

	[SerializeField]
	[Range(1f, 2f)]
	private float fateCardBacklashMonsterCountMultiplier = 1.5f;

	[SerializeField]
	[Range(0f, 1.2f)]
	private float maxFateDebtBossHealthBonus = 1.2f;

	[SerializeField]
	[Range(0f, 0.95f)]
	private float fateCardMonsterStatCrushRatio = 0.9f;

	[SerializeField]
	private int fateCardCombatGold = 60;

	[SerializeField]
	private int fateCardMonsterCrushDebt = 94;

	[SerializeField]
	private int fateCardCombatDraftDebt = 80;

	[SerializeField]
	private int fateCardFullHealDebt = 82;

	[SerializeField]
	private int fateCardForbiddenSummonDebt = 82;

	[SerializeField]
	private int fateCardGamblerDebt = 58;

	[SerializeField]
	private int fateCardLastBarrierDebt = 72;

	[SerializeField]
	[Range(0f, 1f)]
	private float fateCardForbiddenSummonCostPenalty = 0.4f;

	[SerializeField]
	private int fateCardForbiddenSummonTaxRounds = 3;

	[SerializeField]
	[Range(0f, 1f)]
	private float fateCardGamblerGoldSuccessRate = 0.7f;

	[SerializeField]
	private int fateCardGamblerGoldFallbackGain = 20;

	[SerializeField]
	private int fateCardGamblerFailLifeCost = 2;

	[SerializeField]
	private float fateCardGamblerFailStunDuration = 2f;

	[SerializeField]
	private int fateCardGoldLoanDebt = 82;

	[SerializeField]
	private int fateCardGoldLoanGold = 60;

	[SerializeField]
	private int fateCardRareMercenaryDebt = 68;

	[SerializeField]
	private int fateCardRareMercenaryCount = 2;

	[SerializeField]
	private int fateCardEpicAdvanceDebt = 72;

	[SerializeField]
	private int fateCardEpicAdvanceGold = 25;

	[SerializeField]
	[Range(0f, 1f)]
	private float fateCardEpicAdvanceCostPenalty = 0.3f;

	[SerializeField]
	private int fateCardMythicLeaseDebt = 96;

	[SerializeField]
	private int fateCardBlackMarketDebt = 58;

	[SerializeField]
	private int fateCardBlackMarketGold = 45;

	[SerializeField]
	[Range(0f, 1f)]
	private float fateCardBlackMarketManaRestoreRatio = 0.6f;

	[SerializeField]
	private int fateCardTimeStopDebt = 64;

	[SerializeField]
	private float fateCardTimeStopDuration = 4f;

	[SerializeField]
	private int fateCardThunderDebt = 68;

	[SerializeField]
	[Range(0f, 1f)]
	private float fateCardThunderDamageRatio = 0.25f;

	[SerializeField]
	private int fateCardManaFloodDebt = 54;

	[SerializeField]
	private int fateCardManaFloodLifeCost = 2;

	[SerializeField]
	private int fateCardWallRepairDebt = 68;

	[SerializeField]
	private int fateCardWallRepairLife = 3;

	[SerializeField]
	[Range(0f, 1f)]
	private float fateCardWallRepairCostPenalty = 0.25f;

	[SerializeField]
	private int fateCardSmugglerRouteDebt = 62;

	[SerializeField]
	[Range(0f, 1f)]
	private float fateCardSmugglerRouteDiscount = 0.4f;

	[SerializeField]
	private int fateCardSmugglerRouteRounds = 3;

	[SerializeField]
	private int fateCardSmugglerRouteGold = 45;

	[SerializeField]
	private int fateCardLifeForgeDebt = 70;

	[SerializeField]
	private int fateCardLifeForgeMaxLife = 3;

	[SerializeField]
	private int fateCardGradeRiggingDebt = 74;

	[SerializeField]
	private int fateCardGradeRiggingSummons = 4;

	[SerializeField]
	private int fateCardGradeRiggingGold = 35;

	private int maxLife;

	private int currentSummonBaseCost;

	private int roundGoldBonus;

	private int currentRoundLeakDamageTaken;

	private int victoryStreak;

	private float summonCostDiscountRate;

	private float temporaryShopSummonDiscountRate;

	private int temporaryShopSummonDiscountUntilRound;

	private int currentRoundResolvedMonsters;

	private int currentRoundKilledMonsters;

	private int lastRoundShopOpenRound = -1;

	private bool gameOverRaised;

	private bool debugRoundAdvanceInProgress;

	private Coroutine defeatAdjudicationRoutine;

	private Coroutine defeatFinalizeRoutine;

	private float defeatPreviousTimeScale = 1f;

	private float defeatPreviousFixedDeltaTime = 0.02f;

	private bool defeatTimeScaleCaptured;

	private bool fateChoiceSlowMotionActive;

	private float fateChoicePreviousTimeScale = 1f;

	private float fateChoicePreviousFixedDeltaTime = 0.02f;

	public const float DefeatSlowMotionDurationRealtime = 5f;

	public const float DefeatSlowMotionTargetScale = 0.1f;

	private const float DefeatFinalizePaddingRealtime = 0.1f;

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

	private int initialPreparationSummons;

	private bool initialPreparationClosed;

	private bool earlyRunMomentTriggered;

	private bool earlyFallbackRewardGranted;

	private bool earlyBossPrepRewardGranted;

	private bool badLuckInsuranceOfferPending;

	private bool badLuckInsuranceOffered;

	private int luckySummonNormalStreak;

	private bool luckySummonReady;

	private bool luckySummonConsumed;

	private bool luckySummonChoiceOpen;

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

	private int fateBossDebtAnchor;

	private int fateGradeLockSummonsRemaining;

	private CharacterGrade fateGradeLockMinimum = CharacterGrade.Normal;

	private int fateNormalBanSummonsRemaining;

	private bool fateForceNextShop;

	private bool fateCardUsed;

	private bool fateCardChoicePanelOpen;

	private int pendingPostRoundChoiceRound = -1;

	private int fateMonsterSurgeRound = -1;

	private readonly FateCardType[] fateCardChoices = new FateCardType[3];

	private bool fateCardChoicesInitialized;

	private int fateLeakShieldRound = -1;

	private bool fateLeakShieldFeedbackShown;

	private int fateSummonTaxUntilRound = -1;

	private float fateSummonTaxRate;

	private int fateSummonDiscountUntilRound = -1;

	private float fateSummonDiscountRate;

	private string fateCardLastTitle = "미사용";

	private string fateCardLastDetail = "운명 카드 대기";

	private int fateCardLastDebt;

	private int fateMonsterCrushRound = -1;

	private int fateTimeStopRound = -1;

	private int fateTimeStopAppliedCount;

	private int fateThunderStrikeRound = -1;

	private int fateThunderStrikeAppliedCount;

	private int fateCombatEditingRound = -1;

	private bool fateCombatEditingUnlocked;

	private int runFateMonsterCrushCount;

	private int runFateCombatDraftCount;

	private int runFateFullHealCount;

	private bool firstLegendaryMergeRecorded;

	private bool lifeOneClutchRecorded;

	private bool fateSurvivalClutchRecorded;

	private bool runDefeatMomentRecorded;

	private readonly List<string> runClipEvents = new List<string>();

	private readonly HashSet<string> rewardedUltimateRecipeNames = new HashSet<string>();

	public static DefenseGameController Active { get; private set; }

	public static bool IsDefeatSlowMotionActive { get; private set; }

	public int Gold { get; private set; }

	public int Life => life;

	public int MaxLife => (maxLife > 0) ? maxLife : life;

	public string LifeHudSummary => "HP " + Life + "/" + MaxLife;

	public int SummonCost => ResolveSummonCost();

	public int CurrentRound => ((Object)(object)roundManager != (Object)null) ? roundManager.CurrentRound : 0;

	public bool IsRoundRunning => (Object)(object)roundManager != (Object)null && roundManager.IsRoundRunning;

	public bool IsCombatInteractionLocked => IsRoundRunning && CurrentRound > 0 && (Object)(object)roundManager != (Object)null && roundManager.CurrentRoundSpawnedCount > 0 && !FateCombatEditingActive;

	public bool IsBossRound => (Object)(object)roundManager != (Object)null && roundManager.IsBossRound;

	public int NextBossRound => ((Object)(object)roundManager != (Object)null) ? roundManager.GetNextBossRound(CurrentRound) : 10;

	public int RoundsUntilNextBoss => Mathf.Max(0, NextBossRound - CurrentRound);

	public int BoardUnitCount => ((Object)(object)boardManager != (Object)null) ? boardManager.UnitCount : 0;

	public int BoardCapacity => ((Object)(object)boardManager != (Object)null) ? boardManager.UnlockedSlotCount : 0;

	public int EmptySlotCount => ((Object)(object)boardManager != (Object)null) ? boardManager.EmptySlotCount : 0;

	public int CharacterCount => ((Object)(object)characterDatabase != (Object)null) ? characterDatabase.Characters.Count : 0;

	public int MonsterCount => ((Object)(object)monsterDatabase != (Object)null) ? monsterDatabase.Monsters.Count : 0;

	public int RoundTargetCount => ((Object)(object)roundManager != (Object)null) ? roundManager.CurrentRoundTargetCount : 0;

	public int RoundResolvedMonsterCount => currentRoundResolvedMonsters;

	public float RoundProgress01 => (RoundTargetCount > 0) ? Mathf.Clamp01((float)currentRoundResolvedMonsters / (float)RoundTargetCount) : ((CurrentRound > 0 && !IsRoundRunning) ? 1f : 0f);

	public string CurrentStateSummary => "Gold " + Gold + " | Life " + Life + " | Round " + CurrentRound + (IsBossRound ? " Boss" : string.Empty);

	public int LastRoundClearGoldReward { get; private set; }

	public MergeResultInfo? LastMergeResult { get; private set; }

	public string BestSynergySummary => (bestSynergyCount > 0) ? (bestSynergyTitle + " (" + bestSynergyCount + "개 활성)") : "활성 시너지 없음";

	public string CurrentSynergySummary => (currentSynergyCount > 0) ? (currentSynergyTitle + " x" + currentSynergyCount) : "시너지 없음";

	public string TopDamageSummary => (topDamageHeroDamage > 0f) ? (topDamageHeroName + "  " + Mathf.RoundToInt(topDamageHeroDamage).ToString("N0")) : "기록 없음";

	public string RoundTopDamageSummary => (roundTopDamageHeroDamage > 0f) ? (roundTopDamageHeroName + "  " + Mathf.RoundToInt(roundTopDamageHeroDamage).ToString("N0")) : "기록 없음";

	public string DamageLeaderboardSummary => BuildDamageLeaderboardSummary(damageByHero, 3);

	public string RoundDamageLeaderboardSummary => BuildDamageLeaderboardSummary(currentRoundDamageByHero, 3);

	public string CurrentTileContributionSummary => BuildCurrentTileContributionSummary();

	public string CurrentBossPressureSummary => BuildBossPressureSummary(total: false);

	public string BossPressureSummary => BuildBossPressureSummary(total: true);

	public string CurrentBuildGoalSummary => ComposeBuildGoalGuideSummary();

	public string CurrentDangerSummary => BuildCurrentDangerSummary();

	public string EarlyRunTelemetrySummary => earlyRunTelemetrySummary;

	public string EarlyRunTuningLoopSummary => earlyRunTelemetrySummary + " / " + earlyRunLogCoverageSummary + " / 긴급지원 " + earlyRunRecoveryOfferCount + "회";

	public string EarlyRunTuningHint => earlyRunTuningHint;

	public bool EarlyRunRecoveryRecommended => earlyRunRecoveryRecommended;

	public string EarlyRunRecoveryReason => earlyRunRecoveryReason;

	public string EarlyRunRecoveryCause => earlyRunRecoveryCause;

	public string EarlyRunLogCoverageSummary => earlyRunLogCoverageSummary;

	public string EarlyRunActionSummary => BuildEarlyRunActionSummary();

	public bool BadLuckInsuranceAvailable => badLuckInsuranceOfferPending;

	public string BadLuckInsuranceReason => badLuckInsuranceReason;

	public int LuckySummonNormalStreak => Mathf.Max(0, luckySummonNormalStreak);

	public int LuckySummonThreshold => Mathf.Max(1, luckySummonNormalStreakThreshold);

	public bool LuckySummonProgressVisible => enableLuckySummonComeback && !luckySummonConsumed && (LuckySummonNormalStreak >= Mathf.Max(1, luckySummonVisibleStreak) || LuckySummonReady);

	public bool LuckySummonReady => enableLuckySummonComeback && !luckySummonConsumed && (luckySummonReady || (LuckySummonNormalStreak >= LuckySummonThreshold && GetSummonRateRound() >= Mathf.Max(4, luckySummonEarliestRound)));

	public bool LuckySummonChoiceOpen => luckySummonChoiceOpen;

	public string RecommendedDeckSummary => BuildRecommendedDeckSummary();

	public string RecommendedBuildName => BuildRecommendedBuildName();

	public string RunNextGoalHeadline => BuildRunNextGoalHeadline();

	public int EarnedGrowthCurrency => earnedGrowthCurrency;

	public int BestKillCombo => bestKillCombo;

	public int CriticalHitCount => criticalHitCount;

	public float TotalDamageDealt => totalDamageDealt;

	public int RunBossKillCount => totalBossKills;

	public int RunBossScore => CalculateRunBossScore();

	public string RunMvpName => (topDamageHeroDamage > 0f) ? topDamageHeroName : "MVP 대기";

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

	public bool FateCardWasUsed => fateCardUsed;

	public string FateCardLastTitle => fateCardLastTitle;

	public string FateCardLastDetail => fateCardLastDetail;

	public int FateCardLastDebt => fateCardLastDebt;

	public float FateDebtBossHealthMultiplier => ResolveFateDebtBossHealthMultiplier();

	public int FateGauge => fateGauge;

	public int MaxFateGauge => Mathf.Max(1, maxFateGauge);

	public int FateDebt => fateDebt;

	public int MaxFateDebt => Mathf.Max(1, maxFateDebt);

	public float FateGauge01 => (!enableFateIntervention) ? 0f : ((!useOneShotFateCard) ? Mathf.Clamp01((float)fateGauge / (float)MaxFateGauge) : (CanOpenFateCard ? 1f : 0f));

	public string FateHudSummary => BuildReadableFateHudSummary();

	public string FateCardStatusSummary => (!useOneShotFateCard) ? ("보스HP x" + FateDebtBossHealthMultiplier.ToString("0.00")) : (fateCardUsed ? ("계약 완료: " + fateCardLastTitle) : (CanUseFateCard ? "선택 중: 전투 0.1배" : (CanOpenFateCard ? "운명카드 준비: 눌러서 선택" : "전투 중 위기 시 개방")));

	public string FateGradeLockHudLabel => (!useOneShotFateCard) ? BuildFateActionLabel("Rare+", fateGradeLockGaugeCost, fateGradeLockDebt) : (CanUseFateCard ? GetFateCardChoiceLabel(1) : (CanOpenFateCard ? "봉인\n카드 개방 후 공개" : "봉인\n전투 중 공개"));

	public string FateNormalBanHudLabel => (!useOneShotFateCard) ? BuildFateActionLabel("No Normal", fateNormalBanGaugeCost, fateNormalBanDebt) : (CanUseFateCard ? GetFateCardChoiceLabel(2) : (CanOpenFateCard ? "봉인\n카드 개방 후 공개" : "봉인\n전투 중 공개"));

	public string FateForceShopHudLabel => (!useOneShotFateCard) ? BuildFateActionLabel("Force Shop", fateForceShopGaugeCost, fateForceShopDebt) : (fateCardUsed ? "계약 완료\n재등장 없음" : (CanUseFateCard ? "선택 후 사라짐\n0.1배 슬로우" : (CanOpenFateCard ? "운명카드\n눌러서 개방" : "운명카드\n전투 중 개방")));

	public bool FateSurvivalCrisisActive => IsFateSurvivalCrisisActive();

	public string FateSurvivalHudLabel => (!useOneShotFateCard) ? (FateSurvivalCrisisActive ? ("빚지고 살기\n지금 " + Mathf.Max(0, fateSurvivalGaugeCost) + "F/+" + Mathf.Max(0, fateSurvivalDebt)) : ("빚지고 살기\n" + Mathf.Max(0, fateSurvivalGaugeCost) + "F / +" + Mathf.Max(0, fateSurvivalDebt))) : (CanUseFateCard ? GetFateCardChoiceLabel(0) : (CanOpenFateCard ? "운명카드\n눌러서 개방" : "운명카드\n전투 중 개방"));

	public Color FateSurvivalHudColor => ResolveFateCardChoiceColor(0);

	public Color FateGradeLockHudColor => ResolveFateCardChoiceColor(1);

	public Color FateNormalBanHudColor => ResolveFateCardChoiceColor(2);

	public bool CanOpenFateCard => IsFateCardCombatChoiceAvailable();

	public bool CanUseFateCard => IsFateCardCombatChoiceAvailable() && fateCardChoicePanelOpen;

	public bool CanUseFateGradeLock => useOneShotFateCard ? IsFateCardChoiceAvailable(1) : CanSpendFateGauge(fateGradeLockGaugeCost);

	public bool CanUseFateNormalBan => useOneShotFateCard ? IsFateCardChoiceAvailable(2) : CanSpendFateGauge(fateNormalBanGaugeCost);

	public bool CanUseFateForcedShop => !useOneShotFateCard && CanSpendFateGauge(fateForceShopGaugeCost);

	public bool CanUseFateSurvival => useOneShotFateCard ? IsFateCardChoiceAvailable(0) : (CanSpendFateGauge(fateSurvivalGaugeCost) && (CurrentRound >= 4 || Life < MaxLife));

	public bool ShouldShowFatePanel => enableFateIntervention && (!useOneShotFateCard || CanUseFateCard);

	public bool ShouldShowFateCardEntryButton => enableFateIntervention && useOneShotFateCard && CanOpenFateCard && !fateCardChoicePanelOpen;

	public bool FateChoiceSlowMotionActive => fateChoiceSlowMotionActive;

	public bool FateCardChoicePanelOpen => fateCardChoicePanelOpen;

	public bool FateLeakShieldActive => enableFateIntervention && useOneShotFateCard && fateLeakShieldRound > 0 && CurrentRound == fateLeakShieldRound;

	public bool FateMonsterStatCrushActive => enableFateIntervention && useOneShotFateCard && fateMonsterCrushRound > 0 && CurrentRound == fateMonsterCrushRound;

	public float FateMonsterStatCrushRatio => Mathf.Clamp01(fateCardMonsterStatCrushRatio);

	public bool FateCombatEditingActive => enableFateIntervention && useOneShotFateCard && fateCombatEditingUnlocked && fateCombatEditingRound > 0 && CurrentRound == fateCombatEditingRound;

	public string EarlyRunTuningDecisionSummary => BuildEarlyRunTuningDecisionSummary();

	public int EarlyRunTuningSampleCount => GetEarlyRunLogSampleCount();

	public int EarlyRunTuningTargetSampleCount => Mathf.Max(1, earlyTelemetryTargetSampleCount);

	public string SeasonReplayDigestSummary => BuildSeasonReplayDigestSummary();

	public string LastMergeFailureReason => ((Object)(object)boardManager != (Object)null) ? boardManager.LastMergeFailureReason : string.Empty;

	public int ReadyUltimateRecipeCount => ((Object)(object)boardManager != (Object)null) ? boardManager.GetReadyUltimateRecipeOptions(characterDatabase).Length : 0;

	public event Action OnStateChanged;

	public event Action<MergeResultInfo> OnMergeCompleted;

	public event Action<CharacterDefinition> OnUnitSummoned;

	public event Action<int> OnRoundCountdownChanged;

	public event Action<int> OnRoundStarted;

	public event Action<int> OnRoundMissionSettlement;

	public event Action<int> OnRoundEconomySettlement;

	public event Action<int> OnRoundBoardPreparation;

	public event Action<int> OnRoundShopPhase;

	public event Action<int> OnRoundAugmentChoicePhase;

	public event Action<int> OnRoundCompleted;

	public event Action OnGameOver;

	public event Action OnLuckySummonChoiceRequested;

	public event Action<string, Color, float> OnBannerRequested;

	public bool WasRoundShopOpened(int round)
	{
		return round > 0 && lastRoundShopOpenRound == round;
	}

	public string GetFateCardChoiceHudLabel(int index)
	{
		return useOneShotFateCard ? GetFateCardChoiceLabel(index) : string.Empty;
	}

	public float GetFateMonsterCountMultiplierForRound(int round)
	{
		return (enableFateIntervention && useOneShotFateCard && fateMonsterSurgeRound > 0 && round == fateMonsterSurgeRound) ? Mathf.Max(1f, fateCardBacklashMonsterCountMultiplier) : 1f;
	}

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
		if ((Object)(object)characterDatabase == (Object)null)
		{
			characterDatabase = ((Component)this).GetComponent<CharacterDatabase>();
		}
		if ((Object)(object)monsterDatabase == (Object)null)
		{
			monsterDatabase = ((Component)this).GetComponent<MonsterDatabase>();
		}
		if ((Object)(object)boardManager == (Object)null)
		{
			boardManager = ((Component)this).GetComponent<DefenseBoardManager>();
		}
		if ((Object)(object)roundManager == (Object)null)
		{
			roundManager = ((Component)this).GetComponent<RoundManager>();
		}
	}

	private void OnEnable()
	{
		MonsterUnit.OnMonsterSpawned += HandleMonsterSpawned;
		MonsterUnit.OnMonsterKilled += HandleMonsterKilled;
		MonsterUnit.OnMonsterEscaped += HandleMonsterEscaped;
		DefenderUnit.OnDamageDealt += HandleDamageDealt;
		DefenderUnit.OnDefenderRemoved += HandleDefenderRemoved;
		SubscribeRoundManager();
	}

	private void OnDisable()
	{
		RestoreFateChoiceSlowMotion();
		CancelPendingDefeatAdjudication();
		CancelPendingDefeatFinalization();
		MonsterUnit.OnMonsterSpawned -= HandleMonsterSpawned;
		MonsterUnit.OnMonsterKilled -= HandleMonsterKilled;
		MonsterUnit.OnMonsterEscaped -= HandleMonsterEscaped;
		DefenderUnit.OnDamageDealt -= HandleDamageDealt;
		DefenderUnit.OnDefenderRemoved -= HandleDefenderRemoved;
		UnsubscribeRoundManager();
		if ((Object)(object)Active == (Object)(object)this)
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

	private void Update()
	{
		UpdateFateChoiceSlowMotion();
		if (Input.GetKeyDown((KeyCode)289))
		{
			TriggerDebugDefeat();
		}
		if (Input.GetKeyDown((KeyCode)290))
		{
			TriggerDebugAdvanceRound();
		}
	}

	public void TriggerDebugDefeat()
	{
		if (!gameOverRaised)
		{
			life = 0;
			TriggerLeakDefeat();
		}
	}

	public void TriggerDebugAdvanceRound()
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		if (gameOverRaised || life <= 0 || (Object)(object)roundManager == (Object)null)
		{
			return;
		}
		if (roundManager.IsRoundRunning)
		{
			debugRoundAdvanceInProgress = true;
			try
			{
				roundManager.CompleteCurrentRoundForDebug();
			}
			finally
			{
				debugRoundAdvanceInProgress = false;
			}
		}
		StartRound();
		this.OnBannerRequested?.Invoke("DEV  ROUND " + CurrentRound + " 시작", new Color(0.34f, 0.78f, 1f), 1.5f);
	}

	public void IncreaseMaxLife(int amount, bool healIncrease = true)
	{
		int num = Mathf.Max(0, amount);
		if (num > 0)
		{
			maxLife = Mathf.Max(1, MaxLife + num);
			if (healIncrease)
			{
				life = Mathf.Min(maxLife, life + num);
			}
			NotifyStateChanged();
		}
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
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		if (IsCombatInteractionLocked)
		{
			return false;
		}
		if (!initialPreparationClosed && initialPreparationSummons >= 3)
		{
			this.OnBannerRequested?.Invoke("초반 준비 소환은 3마리까지입니다. 전투 중 성장 선택을 시작하세요.", new Color(1f, 0.78f, 0.28f), 1.8f);
			return false;
		}
		int num = SummonCost;
		if (Gold < num || (Object)(object)characterDatabase == (Object)null || (Object)(object)boardManager == (Object)null)
		{
			return false;
		}
		RefreshLuckySummonReadiness();
		if (LuckySummonReady)
		{
			luckySummonChoiceOpen = true;
			this.OnLuckySummonChoiceRequested?.Invoke();
			NotifyStateChanged();
			return false;
		}
		bool earlyPitySummon;
		CharacterDefinition characterDefinition = SelectSummonDefinition(out earlyPitySummon);
		if (characterDefinition == null)
		{
			return false;
		}
		if (!boardManager.TrySpawnUnit(characterDefinition, defaultUnitPrefab, out var spawnedUnit))
		{
			return false;
		}
		Gold -= num;
		if (!initialPreparationClosed)
		{
			initialPreparationSummons++;
		}
		currentSummonBaseCost = Mathf.Min(maxSummonCost, currentSummonBaseCost + ResolveSummonCostIncrease());
		if (characterDefinition.grade != CharacterGrade.Transcendent)
		{
			RuntimeAudioUtility.PlayDiceAppear();
		}
		RegisterSummonExcitement(characterDefinition, earlyPitySummon, spawnedUnit);
		RecordEarlyRoundSummon(characterDefinition);
		ResolveUltimateRecipeBingoReward();
		this.OnUnitSummoned?.Invoke(characterDefinition);
		NotifyStateChanged();
		return true;
	}

	public int GetLuckySummonChoiceCost(LuckySummonChoice choice)
	{
		int num = SummonCost;
		return (choice == LuckySummonChoice.SafeRare) ? Mathf.Max(num, Mathf.CeilToInt((float)num * Mathf.Max(1f, luckySummonSafeCostMultiplier))) : num;
	}

	public bool CanChooseLuckySummon(LuckySummonChoice choice)
	{
		return LuckySummonReady && luckySummonChoiceOpen && !IsCombatInteractionLocked && (Object)(object)boardManager != (Object)null && (Object)(object)characterDatabase != (Object)null && EmptySlotCount > 0 && Gold >= GetLuckySummonChoiceCost(choice);
	}

	public void CancelLuckySummonChoice()
	{
		if (luckySummonChoiceOpen)
		{
			luckySummonChoiceOpen = false;
			NotifyStateChanged();
		}
	}

	public bool TryResolveLuckySummonChoice(LuckySummonChoice choice)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		RefreshLuckySummonReadiness();
		if (!CanChooseLuckySummon(choice))
		{
			this.OnBannerRequested?.Invoke("행운 소환 불가: 골드와 빈 슬롯을 확인하세요.", new Color(1f, 0.54f, 0.28f), 1.8f);
			return false;
		}
		int luckySummonChoiceCost = GetLuckySummonChoiceCost(choice);
		bool jackpotSuccess = false;
		bool jackpotFailed = false;
		CharacterDefinition characterDefinition = SelectLuckySummonDefinition(choice, out jackpotSuccess, out jackpotFailed);
		if (characterDefinition == null || !boardManager.TrySpawnUnit(characterDefinition, defaultUnitPrefab, out var spawnedUnit))
		{
			return false;
		}
		Gold -= luckySummonChoiceCost;
		int num = (jackpotFailed ? Mathf.Clamp(Mathf.RoundToInt((float)luckySummonChoiceCost * Mathf.Clamp01(luckySummonJackpotRefundRate)), 0, luckySummonChoiceCost) : 0);
		Gold += num;
		currentSummonBaseCost = Mathf.Min(maxSummonCost, currentSummonBaseCost + ResolveSummonCostIncrease());
		luckySummonNormalStreak = 0;
		luckySummonReady = false;
		luckySummonConsumed = true;
		luckySummonChoiceOpen = false;
		badLuckInsuranceOfferPending = false;
		if (characterDefinition.grade != CharacterGrade.Transcendent)
		{
			RuntimeAudioUtility.PlayDiceAppear();
		}
		RegisterSummonExcitement(characterDefinition, earlyPitySummon: false, spawnedUnit, trackLuckyStreak: false);
		RecordEarlyRoundSummon(characterDefinition);
		ResolveUltimateRecipeBingoReward();
		this.OnUnitSummoned?.Invoke(characterDefinition);
		string text;
		Color arg = default(Color);
		switch (choice)
		{
		case LuckySummonChoice.MergeLink:
			text = "연결의 주사위: 가장 가까운 합성 재료 획득";
			((Color)(ref arg))._002Ector(0.42f, 0.92f, 0.68f);
			break;
		case LuckySummonChoice.SafeRare:
			text = "안전의 주사위: 레어 이상 확정 획득";
			((Color)(ref arg))._002Ector(0.36f, 0.78f, 1f);
			break;
		default:
			if (jackpotSuccess)
			{
				text = "승부 성공! 에픽 유닛 획득";
				((Color)(ref arg))._002Ector(1f, 0.54f, 0.92f);
			}
			else
			{
				text = "승부 실패: 일반 유닛 + " + num + "G 환급";
				((Color)(ref arg))._002Ector(1f, 0.76f, 0.3f);
			}
			break;
		}
		AddRunHighlightCard("행운 소환", text);
		this.OnBannerRequested?.Invoke(text, arg, 2.4f);
		NotifyStateChanged();
		return true;
	}

	private CharacterDefinition SelectLuckySummonDefinition(LuckySummonChoice choice, out bool jackpotSuccess, out bool jackpotFailed)
	{
		jackpotSuccess = false;
		jackpotFailed = false;
		if ((Object)(object)characterDatabase == (Object)null)
		{
			return null;
		}
		CharacterDefinition characterDefinition;
		switch (choice)
		{
		case LuckySummonChoice.MergeLink:
			characterDefinition = characterDatabase.GetRandomCharacterByGrade(SelectMergeAssistGrade(), deployableOnly: true);
			break;
		case LuckySummonChoice.SafeRare:
			characterDefinition = characterDatabase.GetRandomSummonableCharacter(GetSummonRateRound(), deployableOnly: true);
			if (characterDefinition == null || characterDefinition.grade < CharacterGrade.Rare)
			{
				characterDefinition = characterDatabase.GetRandomCharacterByGrade(CharacterGrade.Rare, deployableOnly: true);
			}
			break;
		default:
			jackpotSuccess = Random.value < Mathf.Clamp01(luckySummonJackpotEpicChance);
			jackpotFailed = !jackpotSuccess;
			characterDefinition = characterDatabase.GetRandomCharacterByGrade(jackpotSuccess ? CharacterGrade.Epic : CharacterGrade.Normal, deployableOnly: true);
			break;
		}
		if (characterDefinition == null)
		{
			characterDefinition = characterDatabase.GetRandomSummonableCharacter(GetSummonRateRound(), deployableOnly: true);
		}
		return (characterDefinition != null) ? ApplyFateSummonIntervention(characterDefinition) : null;
	}

	private void RefreshLuckySummonReadiness()
	{
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if (enableLuckySummonComeback && !luckySummonConsumed && !luckySummonReady && luckySummonNormalStreak >= LuckySummonThreshold && GetSummonRateRound() >= Mathf.Max(4, luckySummonEarliestRound))
		{
			luckySummonReady = true;
			badLuckInsuranceOfferPending = false;
			AddRunHighlightCard("행운 누적", "일반 " + luckySummonNormalStreak + "회 연속 / 행운 소환 준비");
			this.OnBannerRequested?.Invoke("행운 소환 준비! 다음 소환에서 세 가지 주사위 중 하나를 선택하세요.", new Color(0.72f, 0.9f, 0.38f), 2.6f);
		}
	}

	public void ClearBoardForProfileChange()
	{
		if (!IsRoundRunning && !((Object)(object)boardManager == (Object)null))
		{
			boardManager.ClearAllDeployedUnits();
			NotifyStateChanged();
		}
	}

	public void ResetRunForRetry()
	{
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		CancelPendingDefeatAdjudication();
		CancelPendingDefeatFinalization();
		if ((Object)(object)roundManager != (Object)null)
		{
			roundManager.ResetRunState();
		}
		if ((Object)(object)boardManager != (Object)null)
		{
			boardManager.ClearAllDeployedUnits();
			boardManager.RefreshSlotLocks(0);
		}
		Gold = ResolveStartGold();
		life = MaxLife;
		currentSummonBaseCost = summonCost;
		summonCostDiscountRate = 0f;
		temporaryShopSummonDiscountRate = 0f;
		temporaryShopSummonDiscountUntilRound = 0;
		roundGoldBonus = 0;
		victoryStreak = 0;
		currentRoundResolvedMonsters = 0;
		LastRoundClearGoldReward = 0;
		LastMergeResult = null;
		gameOverRaised = false;
		ResetRunStats();
		this.OnBannerRequested?.Invoke("새 판 준비", new Color(0.52f, 0.82f, 1f), 1.6f);
		NotifyStateChanged();
	}

	public void ExitToOutgame()
	{
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		CancelPendingDefeatAdjudication();
		CancelPendingDefeatFinalization();
		if ((Object)(object)roundManager != (Object)null)
		{
			roundManager.ResetRunState();
		}
		if ((Object)(object)boardManager != (Object)null)
		{
			boardManager.ClearAllDeployedUnits();
			boardManager.RefreshSlotLocks(0);
		}
		Gold = ResolveStartGold();
		life = MaxLife;
		currentSummonBaseCost = summonCost;
		summonCostDiscountRate = 0f;
		temporaryShopSummonDiscountRate = 0f;
		temporaryShopSummonDiscountUntilRound = 0;
		roundGoldBonus = 0;
		victoryStreak = 0;
		currentRoundResolvedMonsters = 0;
		LastRoundClearGoldReward = 0;
		LastMergeResult = null;
		gameOverRaised = false;
		ResetRunStats();
		this.OnBannerRequested?.Invoke("아웃게임으로 이동", new Color(0.52f, 0.82f, 1f), 1.8f);
		NotifyStateChanged();
	}

	private int ResolveStartGold()
	{
		DailyFortuneRule today = DailyFortuneSystem.Today;
		return Mathf.Max(0, startGold + ((today != null) ? Mathf.Max(0, today.startGoldBonus) : 0));
	}

	public bool TryMerge(CharacterGrade grade)
	{
		if (IsCombatInteractionLocked)
		{
			return false;
		}
		if ((Object)(object)boardManager == (Object)null || (Object)(object)characterDatabase == (Object)null)
		{
			return false;
		}
		MergeResultInfo mergeResult;
		bool merged = boardManager.TryMergeUnitsOfGrade(grade, characterDatabase, out mergeResult, defaultUnitPrefab);
		return FinalizeMergeResult(merged, mergeResult);
	}

	public bool TryMergeUltimateRecipe(string recipeName)
	{
		if (IsCombatInteractionLocked || (Object)(object)boardManager == (Object)null || (Object)(object)characterDatabase == (Object)null)
		{
			return false;
		}
		MergeResultInfo mergeResult;
		bool merged = boardManager.TryMergeUltimateRecipe(recipeName, characterDatabase, out mergeResult, defaultUnitPrefab);
		return FinalizeMergeResult(merged, mergeResult);
	}

	private bool FinalizeMergeResult(bool merged, MergeResultInfo mergeResult)
	{
		if (!merged)
		{
			return false;
		}
		LastMergeResult = mergeResult;
		if (mergeResult.resultGrade < CharacterGrade.Rare)
		{
			RuntimeAudioUtility.PlayReroll();
		}
		RegisterMergeExcitement(mergeResult);
		RecordEarlyRoundMerge(mergeResult.resultGrade);
		ResolveUltimateRecipeBingoReward();
		this.OnMergeCompleted?.Invoke(mergeResult);
		NotifyStateChanged();
		return true;
	}

	public void RegisterAugmentManager(AugmentManager manager)
	{
		augmentManager = manager;
	}

	public void StartRound()
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)roundManager == (Object)null)
		{
			return;
		}
		if ((Object)(object)augmentManager != (Object)null && augmentManager.HasPendingChoice)
		{
			augmentManager.OpenPendingChoice();
			RequestBanner("무료 증강체 1개를 선택해야 다음 라운드로 진행할 수 있습니다", new Color(0.52f, 0.9f, 1f), 2.2f);
			return;
		}
		if (CurrentRound <= 0)
		{
			initialPreparationClosed = true;
		}
		Gold += CalculateRoundStartGold();
		RuntimeAudioUtility.PlayBattleStart();
		roundManager.StartNextRound();
		NotifyStateChanged();
	}

	private int CalculateRoundStartGold()
	{
		int num = Mathf.Max(0, CurrentRound);
		int num2 = Mathf.FloorToInt((float)num * Mathf.Max(0f, roundStartGoldPerRoundMultiplier));
		return Mathf.Max(0, roundStartGold + num2 + roundGoldBonus);
	}

	public void AddGold(int amount)
	{
		int num = Mathf.Max(0, amount);
		Gold += num;
		RegisterEarlyGoldExcitement(num);
		NotifyStateChanged();
	}

	public void RecordRoundShopOpened(int round)
	{
		if (round > 0)
		{
			lastRoundShopOpenRound = round;
		}
	}

	public void RecoverLife(int amount)
	{
		int num = Mathf.Max(0, amount);
		if (num > 0)
		{
			life = Mathf.Min(MaxLife, life + num);
			NotifyStateChanged();
		}
	}

	public bool TrySpendLifeForContract(int amount, string reason)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.Max(0, amount);
		if (num <= 0)
		{
			return true;
		}
		if (life <= num)
		{
			this.OnBannerRequested?.Invoke("계약 불가  생명력이 부족합니다", new Color(1f, 0.42f, 0.3f), 1.8f);
			return false;
		}
		life = Mathf.Max(1, life - num);
		string text = (string.IsNullOrWhiteSpace(reason) ? "위험 선택" : reason.Trim());
		runFateContractCount++;
		fateContractCount++;
		fateInterventionCount++;
		runFateInterventionCount++;
		AddFateDebt(num * Mathf.Max(0, fateDebtPerContractLife), text);
		AddRunHighlightCard("운명 계약", text + " / 라이프 -" + num + " / 빚 +" + num * Mathf.Max(0, fateDebtPerContractLife));
		NotifyStateChanged();
		return true;
	}

	public int ApplyFateShopDebtCost(int baseCost)
	{
		if (!enableFateIntervention || baseCost <= 0 || fateDebt <= 0)
		{
			return baseCost;
		}
		float num = 1f + Mathf.Clamp01((float)fateDebt / (float)Mathf.Max(1, maxFateDebt)) * maxFateDebtShopCostPenalty;
		return Mathf.Max(1, Mathf.RoundToInt((float)baseCost * num));
	}

	public void RecordFateShopCostPenalty(int amount)
	{
		int num = Mathf.Max(0, amount);
		if (num > 0)
		{
			runFateShopCostPenaltyGold += num;
		}
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
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		if (useOneShotFateCard)
		{
			return TryActivateFateCardChoice(1);
		}
		if (!TrySpendFateGauge(fateGradeLockGaugeCost, fateGradeLockDebt, "등급 잠금", "다음 " + Mathf.Max(1, summonCount) + "회 소환 최소 " + CharacterGradeUtility.GetDisplayName(minimumGrade)))
		{
			return false;
		}
		fateGradeLockSummonsRemaining = Mathf.Max(fateGradeLockSummonsRemaining, Mathf.Max(1, summonCount));
		if (minimumGrade > fateGradeLockMinimum)
		{
			fateGradeLockMinimum = minimumGrade;
		}
		runFateGradeLockCount++;
		this.OnBannerRequested?.Invoke("운명 개입  등급 잠금 " + fateGradeLockSummonsRemaining + "회", new Color(1f, 0.58f, 0.88f), 2.3f);
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateNormalBan(int summonCount)
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		if (useOneShotFateCard)
		{
			return TryActivateFateCardChoice(2);
		}
		if (!TrySpendFateGauge(fateNormalBanGaugeCost, fateNormalBanDebt, "일반 금지", "다음 " + Mathf.Max(1, summonCount) + "회 소환에서 일반 제외"))
		{
			return false;
		}
		fateNormalBanSummonsRemaining = Mathf.Max(fateNormalBanSummonsRemaining, Mathf.Max(1, summonCount));
		runFateNormalBanCount++;
		this.OnBannerRequested?.Invoke("운명 개입  일반 금지 " + fateNormalBanSummonsRemaining + "회", new Color(0.52f, 1f, 0.82f), 2.3f);
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateForcedShop()
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		if (!TrySpendFateGauge(fateForceShopGaugeCost, fateForceShopDebt, "상점 강제 등장", "다음 라운드 상점 확정 등장"))
		{
			return false;
		}
		fateForceNextShop = true;
		runFateForcedShopCount++;
		this.OnBannerRequested?.Invoke("운명 개입  다음 라운드 상점 확정", new Color(0.72f, 0.88f, 1f), 2.3f);
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateSurvival()
	{
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		if (useOneShotFateCard)
		{
			return TryActivateFateCardChoice(0);
		}
		if (!TrySpendFateGauge(fateSurvivalGaugeCost, fateSurvivalDebt, "빚지고 살기", "생명 회복 / 골드 보급 / 다음 상점 확정"))
		{
			return false;
		}
		int num = Mathf.Max(0, fateSurvivalLifeRecover);
		int num2 = Mathf.Max(0, fateSurvivalGold);
		if (num > 0)
		{
			life = Mathf.Min(MaxLife, life + num);
		}
		if (num2 > 0)
		{
			Gold += num2;
		}
		fateForceNextShop = true;
		fateNormalBanSummonsRemaining = Mathf.Max(fateNormalBanSummonsRemaining, Mathf.Max(0, fateSurvivalNormalBanSummons));
		runFateSurvivalCount++;
		AddRunHighlightCard("빚지고 살기", "생명 +" + num + " / 골드 +" + num2 + " / 다음 상점 확정");
		this.OnBannerRequested?.Invoke("빚지고 살기  생명 +" + num + " / 골드 +" + num2, new Color(1f, 0.62f, 0.22f), 2.4f);
		NotifyStateChanged();
		return true;
	}

	public bool TryOpenFateCardChoicePanel()
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (!IsFateCardCombatChoiceAvailable())
		{
			this.OnBannerRequested?.Invoke("운명카드는 전투 중 몬스터가 있을 때만 꺼낼 수 있습니다", new Color(1f, 0.42f, 0.3f), 1.8f);
			return false;
		}
		EnsureFateCardChoices();
		fateCardChoicePanelOpen = true;
		RuntimeAudioUtility.PlayReroll();
		this.OnBannerRequested?.Invoke("마지막 계약 개방  3장 중 1장 선택", new Color(1f, 0.36f, 0.92f), 2f);
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardChoice(int index)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		if (!IsFateCardCombatChoiceAvailable())
		{
			this.OnBannerRequested?.Invoke("운명카드는 전투 중 위기 상황에서만 사용할 수 있습니다", new Color(1f, 0.42f, 0.3f), 1.8f);
			return false;
		}
		if (!fateCardChoicePanelOpen)
		{
			this.OnBannerRequested?.Invoke("먼저 운명카드를 꺼내 선택지를 열어야 합니다", new Color(1f, 0.42f, 0.3f), 1.8f);
			return false;
		}
		if (!IsFateCardChoiceAvailable(index))
		{
			this.OnBannerRequested?.Invoke("선택할 운명 카드가 없습니다", new Color(1f, 0.42f, 0.3f), 1.6f);
			return false;
		}
		return ResolveFateCardChoice(index) switch
		{
			FateCardType.CombatDraft => TryActivateFateCardCombatDraft(), 
			FateCardType.FullHeal => TryActivateFateCardFullHeal(), 
			FateCardType.ForbiddenSummon => TryActivateFateCardForbiddenSummon(), 
			FateCardType.GamblerGold => TryActivateFateCardGamblerGold(), 
			FateCardType.LastBarrier => TryActivateFateCardLastBarrier(), 
			FateCardType.GoldLoan => TryActivateFateCardGoldLoan(), 
			FateCardType.RareMercenaries => TryActivateFateCardRareMercenaries(), 
			FateCardType.EpicAdvance => TryActivateFateCardEpicAdvance(), 
			FateCardType.MythicLease => TryActivateFateCardMythicLease(), 
			FateCardType.BlackMarket => TryActivateFateCardBlackMarket(), 
			FateCardType.TimeStop => TryActivateFateCardTimeStop(), 
			FateCardType.ThunderStrike => TryActivateFateCardThunderStrike(), 
			FateCardType.ManaFlood => TryActivateFateCardManaFlood(), 
			FateCardType.WallRepair => TryActivateFateCardWallRepair(), 
			FateCardType.SmugglerRoute => TryActivateFateCardSmugglerRoute(), 
			FateCardType.LifeForge => TryActivateFateCardLifeForge(), 
			FateCardType.GradeRigging => TryActivateFateCardGradeRigging(), 
			_ => TryActivateFateCardMonsterCrush(), 
		};
	}

	private void EnsureFateCardChoices()
	{
		if (!fateCardChoicesInitialized)
		{
			List<FateCardType> list = new List<FateCardType>
			{
				FateCardType.MonsterCrush,
				FateCardType.CombatDraft,
				FateCardType.FullHeal,
				FateCardType.ForbiddenSummon,
				FateCardType.GamblerGold,
				FateCardType.LastBarrier,
				FateCardType.GoldLoan,
				FateCardType.RareMercenaries,
				FateCardType.EpicAdvance,
				FateCardType.MythicLease,
				FateCardType.BlackMarket,
				FateCardType.TimeStop,
				FateCardType.ThunderStrike,
				FateCardType.ManaFlood,
				FateCardType.WallRepair,
				FateCardType.SmugglerRoute,
				FateCardType.LifeForge,
				FateCardType.GradeRigging
			};
			if (EmptySlotCount <= 0)
			{
				list.Remove(FateCardType.ForbiddenSummon);
				list.Remove(FateCardType.RareMercenaries);
				list.Remove(FateCardType.EpicAdvance);
				list.Remove(FateCardType.MythicLease);
			}
			if (fateCardChoices.Length != 0)
			{
				fateCardChoices[0] = TakeRandomFateCard(list, IsFateSurvivalCard);
			}
			if (fateCardChoices.Length > 1)
			{
				fateCardChoices[1] = TakeRandomFateCard(list, IsFateCombatCard);
			}
			if (fateCardChoices.Length > 2)
			{
				fateCardChoices[2] = TakeRandomFateCard(list, IsFateGrowthCard);
			}
			for (int num = fateCardChoices.Length - 1; num > 0; num--)
			{
				int num2 = Random.Range(0, num + 1);
				FateCardType fateCardType = fateCardChoices[num];
				fateCardChoices[num] = fateCardChoices[num2];
				fateCardChoices[num2] = fateCardType;
			}
			fateCardChoicesInitialized = true;
		}
	}

	private static FateCardType TakeRandomFateCard(List<FateCardType> pool, Predicate<FateCardType> predicate)
	{
		List<FateCardType> list = ((predicate != null) ? pool.FindAll(predicate) : pool);
		if (list.Count <= 0)
		{
			list = pool;
		}
		if (list.Count <= 0)
		{
			return FateCardType.MonsterCrush;
		}
		FateCardType fateCardType = list[Random.Range(0, list.Count)];
		pool.Remove(fateCardType);
		return fateCardType;
	}

	private static bool IsFateSurvivalCard(FateCardType card)
	{
		return card == FateCardType.FullHeal || card == FateCardType.LastBarrier || card == FateCardType.WallRepair || card == FateCardType.LifeForge;
	}

	private static bool IsFateCombatCard(FateCardType card)
	{
		return card == FateCardType.MonsterCrush || card == FateCardType.TimeStop || card == FateCardType.ThunderStrike || card == FateCardType.ManaFlood;
	}

	private static bool IsFateGrowthCard(FateCardType card)
	{
		return !IsFateSurvivalCard(card) && !IsFateCombatCard(card);
	}

	private FateCardType ResolveFateCardChoice(int index)
	{
		EnsureFateCardChoices();
		int num = Mathf.Clamp(index, 0, fateCardChoices.Length - 1);
		return fateCardChoices[num];
	}

	private bool IsFateCardChoiceAvailable(int index)
	{
		if (!CanUseFateCard || index < 0 || index >= fateCardChoices.Length)
		{
			return false;
		}
		EnsureFateCardChoices();
		return true;
	}

	private string GetFateCardChoiceLabel(int index)
	{
		FateCardType choice = ResolveFateCardChoice(index);
		return GetFateCardShortName(choice) + "\n즉시: " + GetFateCardShortEffect(choice) + "\n대가: 운명 빚 +" + Mathf.Max(0, GetFateCardDebt(choice));
	}

	private int GetFateCardDebt(FateCardType choice)
	{
		return choice switch
		{
			FateCardType.CombatDraft => fateCardCombatDraftDebt, 
			FateCardType.FullHeal => fateCardFullHealDebt, 
			FateCardType.ForbiddenSummon => fateCardForbiddenSummonDebt, 
			FateCardType.GamblerGold => fateCardGamblerDebt, 
			FateCardType.LastBarrier => fateCardLastBarrierDebt, 
			FateCardType.GoldLoan => fateCardGoldLoanDebt, 
			FateCardType.RareMercenaries => fateCardRareMercenaryDebt, 
			FateCardType.EpicAdvance => fateCardEpicAdvanceDebt, 
			FateCardType.MythicLease => fateCardMythicLeaseDebt, 
			FateCardType.BlackMarket => fateCardBlackMarketDebt, 
			FateCardType.TimeStop => fateCardTimeStopDebt, 
			FateCardType.ThunderStrike => fateCardThunderDebt, 
			FateCardType.ManaFlood => fateCardManaFloodDebt, 
			FateCardType.WallRepair => fateCardWallRepairDebt, 
			FateCardType.SmugglerRoute => fateCardSmugglerRouteDebt, 
			FateCardType.LifeForge => fateCardLifeForgeDebt, 
			FateCardType.GradeRigging => fateCardGradeRiggingDebt, 
			_ => fateCardMonsterCrushDebt, 
		};
	}

	private Color ResolveFateCardChoiceColor(int index)
	{
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		if (!useOneShotFateCard)
		{
			return (Color)(Mathf.Clamp(index, 0, 2) switch
			{
				1 => new Color(0.18f, 0.68f, 1f, 0.96f), 
				2 => new Color(1f, 0.34f, 0.24f, 0.96f), 
				_ => new Color(0.7f, 0.24f, 1f, 0.98f), 
			});
		}
		return (Color)(ResolveFateCardChoice(index) switch
		{
			FateCardType.CombatDraft => new Color(0.22f, 0.82f, 1f, 0.98f), 
			FateCardType.FullHeal => new Color(1f, 0.28f, 0.46f, 0.98f), 
			FateCardType.ForbiddenSummon => new Color(1f, 0.62f, 0.16f, 0.98f), 
			FateCardType.GamblerGold => new Color(1f, 0.82f, 0.18f, 0.98f), 
			FateCardType.LastBarrier => new Color(0.34f, 0.92f, 0.62f, 0.98f), 
			FateCardType.GoldLoan => new Color(1f, 0.7f, 0.18f, 0.98f), 
			FateCardType.RareMercenaries => new Color(0.18f, 0.58f, 1f, 0.98f), 
			FateCardType.EpicAdvance => new Color(0.74f, 0.36f, 1f, 0.98f), 
			FateCardType.MythicLease => new Color(1f, 0.24f, 0.7f, 0.98f), 
			FateCardType.BlackMarket => new Color(0.24f, 0.42f, 0.92f, 0.98f), 
			FateCardType.TimeStop => new Color(0.42f, 0.92f, 1f, 0.98f), 
			FateCardType.ThunderStrike => new Color(1f, 0.54f, 0.16f, 0.98f), 
			FateCardType.ManaFlood => new Color(0.18f, 0.78f, 1f, 0.98f), 
			FateCardType.WallRepair => new Color(0.26f, 0.92f, 0.46f, 0.98f), 
			FateCardType.SmugglerRoute => new Color(0.22f, 0.92f, 0.74f, 0.98f), 
			FateCardType.LifeForge => new Color(1f, 0.34f, 0.58f, 0.98f), 
			FateCardType.GradeRigging => new Color(0.92f, 0.34f, 1f, 0.98f), 
			_ => new Color(0.74f, 0.26f, 1f, 0.98f), 
		});
	}

	private string GetFateCardShortName(FateCardType choice)
	{
		return choice switch
		{
			FateCardType.CombatDraft => "전장 개방", 
			FateCardType.FullHeal => "피의 계약", 
			FateCardType.ForbiddenSummon => "금단 소환", 
			FateCardType.GamblerGold => "도박사의 판", 
			FateCardType.LastBarrier => "최후의 방벽", 
			FateCardType.GoldLoan => "황금 대출", 
			FateCardType.RareMercenaries => "용병 호출", 
			FateCardType.EpicAdvance => "에픽 선불", 
			FateCardType.MythicLease => "신화 임대", 
			FateCardType.BlackMarket => "암시장 개장", 
			FateCardType.TimeStop => "시간 정지", 
			FateCardType.ThunderStrike => "심판 번개", 
			FateCardType.ManaFlood => "마나 폭주", 
			FateCardType.WallRepair => "응급 방벽", 
			FateCardType.SmugglerRoute => "밀수 루트", 
			FateCardType.LifeForge => "생명 주조", 
			FateCardType.GradeRigging => "등급 조작", 
			_ => "왕의 공포", 
		};
	}

	private string GetFateCardShortEffect(FateCardType choice)
	{
		return choice switch
		{
			FateCardType.CombatDraft => "+" + Mathf.Max(0, fateCardCombatGold) + "G·편집·HP절반", 
			FateCardType.FullHeal => "HP회복·보스강화", 
			FateCardType.ForbiddenSummon => "전설1·소환비+" + Mathf.RoundToInt(Mathf.Clamp01(fateCardForbiddenSummonCostPenalty) * 100f) + "%", 
			FateCardType.GamblerGold => Mathf.RoundToInt(Mathf.Clamp01(fateCardGamblerGoldSuccessRate) * 100f) + "% 편집·실패기절", 
			FateCardType.LastBarrier => "누수무효·다음+" + GetFateBacklashPercent() + "%", 
			FateCardType.GoldLoan => "+" + Mathf.Max(0, fateCardGoldLoanGold) + "G·전투편집", 
			FateCardType.RareMercenaries => "레어×" + Mathf.Max(1, fateCardRareMercenaryCount) + "·HP-2", 
			FateCardType.EpicAdvance => "에픽1·소환비+" + Mathf.RoundToInt(Mathf.Clamp01(fateCardEpicAdvanceCostPenalty) * 100f) + "%", 
			FateCardType.MythicLease => "신화1·HP1", 
			FateCardType.BlackMarket => "+" + Mathf.Max(0, fateCardBlackMarketGold) + "G·마나" + Mathf.RoundToInt(Mathf.Clamp01(fateCardBlackMarketManaRestoreRatio) * 100f) + "%", 
			FateCardType.TimeStop => "등장전체 " + Mathf.RoundToInt(Mathf.Max(0.5f, fateCardTimeStopDuration)) + "초기절", 
			FateCardType.ThunderStrike => "등장전체 " + Mathf.RoundToInt(Mathf.Clamp01(fateCardThunderDamageRatio) * 100f) + "%피해", 
			FateCardType.ManaFlood => "마나회복·HP-" + Mathf.Max(1, fateCardManaFloodLifeCost), 
			FateCardType.WallRepair => "HP+" + Mathf.Max(1, fateCardWallRepairLife) + "·소환비+" + Mathf.RoundToInt(Mathf.Clamp01(fateCardWallRepairCostPenalty) * 100f) + "%", 
			FateCardType.SmugglerRoute => "+" + Mathf.Max(0, fateCardSmugglerRouteGold) + "G·편집·할인", 
			FateCardType.LifeForge => "최대HP+" + Mathf.Max(1, fateCardLifeForgeMaxLife) + "·유닛회복", 
			FateCardType.GradeRigging => "+" + Mathf.Max(0, fateCardGradeRiggingGold) + "G·Rare+" + Mathf.Max(1, fateCardGradeRiggingSummons) + "회", 
			_ => "적-" + Mathf.RoundToInt(FateMonsterStatCrushRatio * 100f) + "%·다음+" + GetFateBacklashPercent() + "%", 
		};
	}

	private string BuildFateCardChoiceSummary()
	{
		EnsureFateCardChoices();
		List<string> list = new List<string>();
		for (int i = 0; i < fateCardChoices.Length; i++)
		{
			FateCardType choice = fateCardChoices[i];
			list.Add(GetFateCardShortName(choice) + "(" + GetFateCardShortEffect(choice) + ")");
		}
		return string.Join(" / ", list);
	}

	public bool TryActivateFateCardMonsterCrush()
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		Color color = default(Color);
		((Color)(ref color))._002Ector(0.78f, 0.28f, 1f);
		if (!TryConsumeFateCard("왕의 공포", "이 라운드 몬스터 체력·공격·이속·공속 " + Mathf.RoundToInt(FateMonsterStatCrushRatio * 100f) + "% 붕괴 / " + BuildFateBacklashText(), Mathf.Max(0, fateCardMonsterCrushDebt), color))
		{
			return false;
		}
		fateMonsterCrushRound = ResolveFateCardTargetRound();
		fateMonsterSurgeRound = fateMonsterCrushRound + 1;
		ApplyFateMonsterCrushToActiveMonsters();
		runFateMonsterCrushCount++;
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardCombatDraft()
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		Color color = default(Color);
		((Color)(ref color))._002Ector(0.26f, 0.88f, 1f);
		if (!TryConsumeFateCard("전장 개방", "이 라운드 전투 중 소환·합성 허용 / 골드 +" + Mathf.Max(0, fateCardCombatGold) + " / HP 절반 지불", Mathf.Max(0, fateCardCombatDraftDebt), color))
		{
			return false;
		}
		UnlockFateCombatEditingForCurrentRound();
		Gold += Mathf.Max(0, fateCardCombatGold);
		int num = ((life > 1) ? Mathf.Max(1, Mathf.FloorToInt((float)life * 0.5f)) : 0);
		if (num > 0)
		{
			life = Mathf.Max(1, life - num);
			AddRunHighlightCard("전장 개방 대가", "HP -" + num + " / 전투 편집");
		}
		runFateCombatDraftCount++;
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardFullHeal()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		Color color = default(Color);
		((Color)(ref color))._002Ector(1f, 0.36f, 0.24f);
		if (!TryConsumeFateCard("피의 계약", "HP 전부 회복 / 모든 유닛 체력 회복 / 다음 보스 강화", Mathf.Max(0, fateCardFullHealDebt), color))
		{
			return false;
		}
		life = MaxLife;
		HealAllDefenders();
		runFateFullHealCount++;
		runFateSurvivalCount++;
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardForbiddenSummon()
	{
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)boardManager == (Object)null || EmptySlotCount <= 0)
		{
			this.OnBannerRequested?.Invoke("금단 소환 실패: 빈 슬롯이 없습니다", new Color(1f, 0.42f, 0.3f), 1.8f);
			return false;
		}
		Color val = default(Color);
		((Color)(ref val))._002Ector(1f, 0.48f, 0.92f);
		if (!TryConsumeFateCard("금단의 소환", "전설 유닛 1개 즉시 획득 / 다음 " + Mathf.Max(1, fateCardForbiddenSummonTaxRounds) + "라운드 소환비 +" + Mathf.RoundToInt(Mathf.Clamp01(fateCardForbiddenSummonCostPenalty) * 100f) + "%", Mathf.Max(0, fateCardForbiddenSummonDebt), val))
		{
			return false;
		}
		int num = ResolveFateCardTargetRound();
		fateSummonTaxRate = Mathf.Max(fateSummonTaxRate, Mathf.Clamp01(fateCardForbiddenSummonCostPenalty));
		fateSummonTaxUntilRound = Mathf.Max(fateSummonTaxUntilRound, num + Mathf.Max(1, fateCardForbiddenSummonTaxRounds) - 1);
		bool flag = TryGrantRandomUnitByGrade(CharacterGrade.Legendary);
		if (!flag)
		{
			flag = TryGrantRandomUnitByGrade(CharacterGrade.Epic);
		}
		AddRunHighlightCard("금단의 소환", flag ? ("전설 유닛 획득 / 소환비 +" + Mathf.RoundToInt(fateSummonTaxRate * 100f) + "%") : "빈 슬롯 없음 / 소환 실패");
		this.OnBannerRequested?.Invoke(flag ? "금단의 소환  전설 유닛 등장" : "금단의 소환  소환 실패", val, 2.4f);
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardGamblerGold()
	{
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		Color val = default(Color);
		((Color)(ref val))._002Ector(1f, 0.82f, 0.24f);
		if (!TryConsumeFateCard("도박사의 판", Mathf.RoundToInt(Mathf.Clamp01(fateCardGamblerGoldSuccessRate) * 100f) + "% 확률로 현재 골드만큼 획득하고 전투 편집 / 실패 시 HP -" + Mathf.Max(1, fateCardGamblerFailLifeCost) + "·현재 적 " + Mathf.Max(0.1f, fateCardGamblerFailStunDuration).ToString("0.#") + "초 기절", Mathf.Max(0, fateCardGamblerDebt), val))
		{
			return false;
		}
		if (Random.value <= Mathf.Clamp01(fateCardGamblerGoldSuccessRate))
		{
			int num = Mathf.Max(Mathf.Max(0, fateCardGamblerGoldFallbackGain), Gold);
			UnlockFateCombatEditingForCurrentRound();
			Gold += num;
			AddRunHighlightCard("도박사의 판 성공", "골드 +" + num);
			this.OnBannerRequested?.Invoke("도박 성공!  골드 +" + num + " / 전투 편집", val, 2.4f);
		}
		else
		{
			int num2 = ((life > 1) ? Mathf.Min(Mathf.Max(1, fateCardGamblerFailLifeCost), life - 1) : 0);
			if (num2 > 0)
			{
				life = Mathf.Max(1, life - num2);
			}
			int num3 = StunActiveMonstersForFate(fateCardGamblerFailStunDuration);
			AddRunHighlightCard("도박사의 판 실패", "HP -" + num2 + " / 현재 적 " + num3 + "기 기절");
			this.OnBannerRequested?.Invoke("도박 실패...  HP -" + num2 + " / 적 " + num3 + "기 기절", new Color(1f, 0.36f, 0.24f), 2.4f);
		}
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardLastBarrier()
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		int num = ResolveFateCardTargetRound();
		Color color = default(Color);
		((Color)(ref color))._002Ector(0.45f, 0.78f, 1f);
		if (!TryConsumeFateCard("최후의 방벽", "이번 라운드 방어선 돌파 피해 0 / 다음 라운드 적 +" + Mathf.RoundToInt((Mathf.Max(1f, fateCardBacklashMonsterCountMultiplier) - 1f) * 100f) + "%", Mathf.Max(0, fateCardLastBarrierDebt), color))
		{
			return false;
		}
		fateLeakShieldRound = num;
		fateLeakShieldFeedbackShown = false;
		fateMonsterSurgeRound = num + 1;
		runFateSurvivalCount++;
		AddRunHighlightCard("최후의 방벽", "R" + num + " 누수 피해 0 / 다음 적 +" + Mathf.RoundToInt((Mathf.Max(1f, fateCardBacklashMonsterCountMultiplier) - 1f) * 100f) + "%");
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardGoldLoan()
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		int num = ResolveFateCardTargetRound();
		Color val = default(Color);
		((Color)(ref val))._002Ector(1f, 0.74f, 0.22f);
		if (!TryConsumeFateCard("황금 대출", "골드 +" + Mathf.Max(0, fateCardGoldLoanGold) + "·이번 라운드 전투 편집 / " + BuildFateBacklashText(), Mathf.Max(0, fateCardGoldLoanDebt), val))
		{
			return false;
		}
		Gold += Mathf.Max(0, fateCardGoldLoanGold);
		UnlockFateCombatEditingForCurrentRound();
		fateMonsterSurgeRound = num + 1;
		AddRunHighlightCard("황금 대출", "골드 +" + Mathf.Max(0, fateCardGoldLoanGold) + " / 전투 편집 / " + BuildFateBacklashText());
		this.OnBannerRequested?.Invoke("황금 대출  골드 +" + Mathf.Max(0, fateCardGoldLoanGold) + " / 전투 편집", val, 2.3f);
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardRareMercenaries()
	{
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)boardManager == (Object)null || EmptySlotCount <= 0)
		{
			this.OnBannerRequested?.Invoke("용병 호출 실패: 빈 슬롯이 없습니다", new Color(1f, 0.42f, 0.3f), 1.8f);
			return false;
		}
		Color val = default(Color);
		((Color)(ref val))._002Ector(0.34f, 0.72f, 1f);
		if (!TryConsumeFateCard("용병 호출", "레어 유닛 최대 " + Mathf.Max(1, fateCardRareMercenaryCount) + "개 즉시 획득 / HP -2", Mathf.Max(0, fateCardRareMercenaryDebt), val))
		{
			return false;
		}
		int num = GrantFateUnitsByGrade(CharacterGrade.Rare, Mathf.Max(1, fateCardRareMercenaryCount));
		int num2 = PayFateLife(2);
		AddRunHighlightCard("용병 호출", "레어 +" + num + " / HP -" + num2);
		this.OnBannerRequested?.Invoke("용병 호출  레어 +" + num + " / HP -" + num2, val, 2.3f);
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardEpicAdvance()
	{
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)boardManager == (Object)null || EmptySlotCount <= 0)
		{
			this.OnBannerRequested?.Invoke("에픽 선불 실패: 빈 슬롯이 없습니다", new Color(1f, 0.42f, 0.3f), 1.8f);
			return false;
		}
		Color val = default(Color);
		((Color)(ref val))._002Ector(0.76f, 0.4f, 1f);
		if (!TryConsumeFateCard("에픽 선불", "에픽 유닛 1개 + 골드 +" + Mathf.Max(0, fateCardEpicAdvanceGold) + " / 다음 2라운드 소환비 +" + Mathf.RoundToInt(Mathf.Clamp01(fateCardEpicAdvanceCostPenalty) * 100f) + "%", Mathf.Max(0, fateCardEpicAdvanceDebt), val))
		{
			return false;
		}
		int num = GrantFateUnitsByGrade(CharacterGrade.Epic, 1);
		Gold += Mathf.Max(0, fateCardEpicAdvanceGold);
		ApplyFateSummonTax(fateCardEpicAdvanceCostPenalty, 2);
		AddRunHighlightCard("에픽 선불", "에픽 +" + num + " / 골드 +" + Mathf.Max(0, fateCardEpicAdvanceGold));
		this.OnBannerRequested?.Invoke("에픽 선불  에픽 +" + num, val, 2.4f);
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardMythicLease()
	{
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)boardManager == (Object)null || EmptySlotCount <= 0)
		{
			this.OnBannerRequested?.Invoke("신화 임대 실패: 빈 슬롯이 없습니다", new Color(1f, 0.42f, 0.3f), 1.8f);
			return false;
		}
		Color val = default(Color);
		((Color)(ref val))._002Ector(1f, 0.3f, 0.34f);
		if (!TryConsumeFateCard("신화 임대", "신화 유닛 1개 즉시 획득 / HP가 1로 감소 / 다음 라운드 적 +" + Mathf.RoundToInt((Mathf.Max(1f, fateCardBacklashMonsterCountMultiplier) - 1f) * 100f) + "%", Mathf.Max(0, fateCardMythicLeaseDebt), val))
		{
			return false;
		}
		int num = ResolveFateCardTargetRound();
		int num2 = GrantFateUnitsByGrade(CharacterGrade.Mythic, 1);
		if (num2 <= 0)
		{
			num2 = GrantFateUnitsByGrade(CharacterGrade.Legendary, 1);
		}
		life = Mathf.Max(1, Mathf.Min(life, 1));
		fateMonsterSurgeRound = num + 1;
		AddRunHighlightCard("신화 임대", "신화 +" + num2 + " / HP 1");
		this.OnBannerRequested?.Invoke("신화 임대  HP 1, 신화 유닛 등장", val, 2.6f);
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardBlackMarket()
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		Color val = default(Color);
		((Color)(ref val))._002Ector(0.32f, 0.52f, 1f);
		if (!TryConsumeFateCard("암시장 개장", "골드 +" + Mathf.Max(0, fateCardBlackMarketGold) + " / 모든 유닛 마나 " + Mathf.RoundToInt(Mathf.Clamp01(fateCardBlackMarketManaRestoreRatio) * 100f) + "% 회복 / 다음 전투상점 확정·가격 상승", Mathf.Max(0, fateCardBlackMarketDebt), val))
		{
			return false;
		}
		Gold += Mathf.Max(0, fateCardBlackMarketGold);
		fateForceNextShop = true;
		int num = RestoreAllDefenderManaForFate(fateCardBlackMarketManaRestoreRatio);
		runFateForcedShopCount++;
		AddRunHighlightCard("암시장 개장", "골드 +" + Mathf.Max(0, fateCardBlackMarketGold) + " / 마나 회복 " + num + "기 / 다음 상점 확정");
		this.OnBannerRequested?.Invoke("암시장 개장  " + num + "기 마나 회복 / 다음 상점 확정", val, 2.3f);
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardTimeStop()
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		int num = ResolveFateCardTargetRound();
		Color val = default(Color);
		((Color)(ref val))._002Ector(0.48f, 0.9f, 1f);
		string text = BuildFateBacklashText();
		string detail = "현재 필드 + 이번 라운드 이후 등장 몬스터 전체 " + Mathf.RoundToInt(Mathf.Max(0.5f, fateCardTimeStopDuration)) + "초 기절 / " + text;
		if (!TryConsumeFateCard("시간 정지", detail, Mathf.Max(0, fateCardTimeStopDebt), val))
		{
			return false;
		}
		fateTimeStopRound = num;
		int num2 = (fateTimeStopAppliedCount = StunActiveMonstersForFate(fateCardTimeStopDuration));
		AddRunHighlightCard("시간 정지", "현재 " + num2 + "기 + 이후 등장 전체 기절 / " + text);
		this.OnBannerRequested?.Invoke("시간 정지  현재 " + num2 + "기 + 이후 등장 전체", val, 2.3f);
		fateMonsterSurgeRound = num + 1;
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardThunderStrike()
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		int num = ResolveFateCardTargetRound();
		Color val = default(Color);
		((Color)(ref val))._002Ector(1f, 0.62f, 0.22f);
		string text = BuildFateBacklashText();
		string detail = "현재 필드 + 이번 라운드 이후 등장 몬스터 전체 최대 체력의 " + Mathf.RoundToInt(Mathf.Clamp01(fateCardThunderDamageRatio) * 100f) + "% 피해 / " + text;
		if (!TryConsumeFateCard("심판 번개", detail, Mathf.Max(0, fateCardThunderDebt), val))
		{
			return false;
		}
		fateThunderStrikeRound = num;
		int num2 = (fateThunderStrikeAppliedCount = DamageActiveMonstersForFate(fateCardThunderDamageRatio));
		AddRunHighlightCard("심판 번개", "현재 " + num2 + "기 + 이후 등장 전체 타격 / " + text);
		this.OnBannerRequested?.Invoke("심판 번개  현재 " + num2 + "기 + 이후 등장 전체", val, 2.3f);
		fateMonsterSurgeRound = num + 1;
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardManaFlood()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		Color val = default(Color);
		((Color)(ref val))._002Ector(0.28f, 0.82f, 1f);
		if (!TryConsumeFateCard("마나 폭주", "모든 유닛 마나 회복 / HP -" + Mathf.Max(1, fateCardManaFloodLifeCost), Mathf.Max(0, fateCardManaFloodDebt), val))
		{
			return false;
		}
		int num = PayFateLife(Mathf.Max(1, fateCardManaFloodLifeCost));
		int num2 = RestoreAllDefenderManaForFate(1f);
		AddRunHighlightCard("마나 폭주", "마나 회복 " + num2 + "기 / HP -" + num);
		this.OnBannerRequested?.Invoke("마나 폭주  " + num2 + "기 충전", val, 2.3f);
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardWallRepair()
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		Color val = default(Color);
		((Color)(ref val))._002Ector(0.38f, 1f, 0.58f);
		if (!TryConsumeFateCard("응급 방벽", "HP +" + Mathf.Max(1, fateCardWallRepairLife) + " / 모든 유닛 회복 / 다음 2라운드 소환비 +" + Mathf.RoundToInt(Mathf.Clamp01(fateCardWallRepairCostPenalty) * 100f) + "%", Mathf.Max(0, fateCardWallRepairDebt), val))
		{
			return false;
		}
		int num = Mathf.Max(1, fateCardWallRepairLife);
		life = Mathf.Min(MaxLife, life + num);
		HealAllDefenders();
		ApplyFateSummonTax(fateCardWallRepairCostPenalty, 2);
		AddRunHighlightCard("응급 방벽", "HP +" + num + " / 유닛 회복");
		this.OnBannerRequested?.Invoke("응급 방벽  HP +" + num, val, 2.3f);
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardSmugglerRoute()
	{
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		int num = ResolveFateCardTargetRound();
		Color val = default(Color);
		((Color)(ref val))._002Ector(0.3f, 1f, 0.82f);
		if (!TryConsumeFateCard("밀수 루트", "골드 +" + Mathf.Max(0, fateCardSmugglerRouteGold) + "·이번 라운드 전투 편집 / 이번 포함 " + Mathf.Max(1, fateCardSmugglerRouteRounds) + "라운드 소환비 -" + Mathf.RoundToInt(Mathf.Clamp01(fateCardSmugglerRouteDiscount) * 100f) + "% / " + BuildFateBacklashText(), Mathf.Max(0, fateCardSmugglerRouteDebt), val))
		{
			return false;
		}
		Gold += Mathf.Max(0, fateCardSmugglerRouteGold);
		UnlockFateCombatEditingForCurrentRound();
		ApplyFateSummonDiscount(fateCardSmugglerRouteDiscount, Mathf.Max(1, fateCardSmugglerRouteRounds));
		fateMonsterSurgeRound = num + 1;
		AddRunHighlightCard("밀수 루트", "골드 +" + Mathf.Max(0, fateCardSmugglerRouteGold) + " / 전투 편집 / 소환비 -" + Mathf.RoundToInt(Mathf.Clamp01(fateCardSmugglerRouteDiscount) * 100f) + "% / " + BuildFateBacklashText());
		this.OnBannerRequested?.Invoke("밀수 루트  골드 +" + Mathf.Max(0, fateCardSmugglerRouteGold) + " / 전투 편집", val, 2.3f);
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardLifeForge()
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		Color val = default(Color);
		((Color)(ref val))._002Ector(1f, 0.38f, 0.62f);
		if (!TryConsumeFateCard("생명 주조", "최대 HP +" + Mathf.Max(1, fateCardLifeForgeMaxLife) + "·모든 유닛 전부 회복 / 현재 골드 절반 소모", Mathf.Max(0, fateCardLifeForgeDebt), val))
		{
			return false;
		}
		int gold = Gold;
		int num = Mathf.FloorToInt((float)gold * 0.5f);
		Gold = Mathf.Max(0, Gold - num);
		int num2 = Mathf.Max(1, fateCardLifeForgeMaxLife);
		maxLife += num2;
		life = Mathf.Min(MaxLife, life + num2);
		HealAllDefenders();
		AddRunHighlightCard("생명 주조", "최대HP +" + num2 + " / 유닛 전부 회복 / 골드 -" + num);
		this.OnBannerRequested?.Invoke("생명 주조  최대HP +" + num2 + " / 유닛 전부 회복", val, 2.4f);
		NotifyStateChanged();
		return true;
	}

	public bool TryActivateFateCardGradeRigging()
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		int num = ResolveFateCardTargetRound();
		Color val = default(Color);
		((Color)(ref val))._002Ector(0.92f, 0.42f, 1f);
		if (!TryConsumeFateCard("등급 조작", "골드 +" + Mathf.Max(0, fateCardGradeRiggingGold) + "·이번 라운드 전투 편집 / 다음 " + Mathf.Max(1, fateCardGradeRiggingSummons) + "회 소환 최소 레어 / " + BuildFateBacklashText(), Mathf.Max(0, fateCardGradeRiggingDebt), val))
		{
			return false;
		}
		Gold += Mathf.Max(0, fateCardGradeRiggingGold);
		UnlockFateCombatEditingForCurrentRound();
		fateGradeLockSummonsRemaining = Mathf.Max(fateGradeLockSummonsRemaining, Mathf.Max(1, fateCardGradeRiggingSummons));
		fateGradeLockMinimum = CharacterGrade.Rare;
		fateMonsterSurgeRound = num + 1;
		runFateGradeLockCount++;
		AddRunHighlightCard("등급 조작", "골드 +" + Mathf.Max(0, fateCardGradeRiggingGold) + " / 전투 편집 / Rare+ " + fateGradeLockSummonsRemaining + "회 / " + BuildFateBacklashText());
		this.OnBannerRequested?.Invoke("등급 조작  골드 +" + Mathf.Max(0, fateCardGradeRiggingGold) + " / Rare+ " + fateGradeLockSummonsRemaining + "회", val, 2.4f);
		NotifyStateChanged();
		return true;
	}

	private void UnlockFateCombatEditingForCurrentRound()
	{
		fateCombatEditingRound = ResolveFateCardTargetRound();
		fateCombatEditingUnlocked = true;
	}

	private void ApplyFateSummonTax(float rate, int rounds)
	{
		int num = ResolveFateCardTargetRound();
		fateSummonTaxRate = Mathf.Max(fateSummonTaxRate, Mathf.Clamp01(rate));
		fateSummonTaxUntilRound = Mathf.Max(fateSummonTaxUntilRound, num + Mathf.Max(1, rounds) - 1);
	}

	private void ApplyFateSummonDiscount(float rate, int rounds)
	{
		int num = ResolveFateCardTargetRound();
		fateSummonDiscountRate = Mathf.Max(fateSummonDiscountRate, Mathf.Clamp01(rate));
		fateSummonDiscountUntilRound = Mathf.Max(fateSummonDiscountUntilRound, num + Mathf.Max(1, rounds) - 1);
	}

	private int PayFateLife(int amount)
	{
		int num = Mathf.Max(0, amount);
		int num2 = ((life > 1) ? Mathf.Min(num, life - 1) : 0);
		if (num2 > 0)
		{
			life = Mathf.Max(1, life - num2);
		}
		return num2;
	}

	private int GrantFateUnitsByGrade(CharacterGrade grade, int count)
	{
		int num = 0;
		int num2 = Mathf.Max(0, count);
		for (int i = 0; i < num2; i++)
		{
			if (EmptySlotCount <= 0)
			{
				break;
			}
			if (TryGrantRandomUnitByGrade(grade))
			{
				num++;
			}
		}
		return num;
	}

	private int StunActiveMonstersForFate(float duration)
	{
		List<MonsterUnit> list = new List<MonsterUnit>(MonsterUnit.ActiveInstances);
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			MonsterUnit monsterUnit = list[i];
			if (!((Object)(object)monsterUnit == (Object)null) && monsterUnit.CanBeCombatTargeted)
			{
				monsterUnit.ApplyStun(Mathf.Max(0.1f, duration));
				num++;
			}
		}
		return num;
	}

	private int DamageActiveMonstersForFate(float maxHealthRatio)
	{
		List<MonsterUnit> list = new List<MonsterUnit>(MonsterUnit.ActiveInstances);
		int num = 0;
		float num2 = Mathf.Clamp01(maxHealthRatio);
		for (int i = 0; i < list.Count; i++)
		{
			MonsterUnit monsterUnit = list[i];
			if (!((Object)(object)monsterUnit == (Object)null) && monsterUnit.CanBeCombatTargeted)
			{
				monsterUnit.TakeDamage(monsterUnit.MaxHealth * num2, critical: true, null);
				num++;
			}
		}
		return num;
	}

	private int RestoreAllDefenderManaForFate(float ratio)
	{
		DefenderUnit[] array = (((Object)(object)boardManager != (Object)null) ? boardManager.GetAliveDefenders() : Object.FindObjectsOfType<DefenderUnit>());
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			if (!((Object)(object)array[i] == (Object)null))
			{
				array[i].RestoreMana(Mathf.Clamp01(ratio));
				num++;
			}
		}
		return num;
	}

	private bool TryConsumeFateCard(string title, string detail, int debt, Color color)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		if (!enableFateIntervention || !useOneShotFateCard)
		{
			this.OnBannerRequested?.Invoke("마지막 계약 비활성", new Color(1f, 0.42f, 0.3f), 1.8f);
			return false;
		}
		if (!IsFateCardCombatChoiceAvailable())
		{
			this.OnBannerRequested?.Invoke("운명카드는 전투 중 위기 상황에서만 사용할 수 있습니다", new Color(1f, 0.42f, 0.3f), 1.8f);
			return false;
		}
		if (!fateCardChoicePanelOpen)
		{
			this.OnBannerRequested?.Invoke("운명카드를 먼저 꺼내야 합니다", new Color(1f, 0.42f, 0.3f), 1.8f);
			return false;
		}
		if (fateCardUsed)
		{
			this.OnBannerRequested?.Invoke("마지막 계약은 이미 사용했습니다", new Color(1f, 0.42f, 0.3f), 1.8f);
			return false;
		}
		fateCardUsed = true;
		fateCardChoicePanelOpen = false;
		RestoreFateChoiceSlowMotion();
		fateCardLastTitle = title;
		fateCardLastDetail = detail;
		fateCardLastDebt = Mathf.Max(0, debt);
		fateGauge = 0;
		fateInterventionCount++;
		fateContractCount++;
		runFateInterventionCount++;
		runFateContractCount++;
		AddFateDebt(fateCardLastDebt, title);
		ApplyFateDebtPressureToActiveBosses();
		AddRunHighlightCard("마지막 계약", title + " / " + detail + " / 빚 +" + fateCardLastDebt);
		RuntimeAudioUtility.PlayJackpotMajor();
		this.OnBannerRequested?.Invoke("마지막 계약: " + title, color, 2.8f);
		RuntimeGameFeel.ShowJackpotReveal("마지막 계약", title, "악마의 카드가 발동되었습니다", color, detail + " / 대가: 빚 +" + fateCardLastDebt, 2.8f);
		return true;
	}

	private void ApplyFateDebtPressureToActiveBosses()
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		int num = 0;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if ((Object)(object)monsterUnit != (Object)null && monsterUnit.RefreshFateDebtBossHealthPressure())
			{
				num++;
			}
		}
		if (num > 0)
		{
			AddRunHighlightCard("운명 대가 즉시 청구", "현재 보스 " + num + "기 HP 강화");
			this.OnBannerRequested?.Invoke("운명 대가 청구  현재 보스 HP 강화", new Color(0.92f, 0.24f, 0.34f), 2.2f);
		}
	}

	private bool IsFateCardCombatChoiceAvailable()
	{
		return enableFateIntervention && useOneShotFateCard && !fateCardUsed && !gameOverRaised && (Object)(object)roundManager != (Object)null && roundManager.IsRoundRunning && CurrentRound > 0 && MonsterUnit.ActiveCount > 0;
	}

	private int ResolveFateCardTargetRound()
	{
		return Mathf.Max(1, IsRoundRunning ? CurrentRound : (CurrentRound + 1));
	}

	private string BuildFateBacklashText()
	{
		return "다음 적 +" + GetFateBacklashPercent() + "%";
	}

	private int GetFateBacklashPercent()
	{
		return Mathf.RoundToInt((Mathf.Max(1f, fateCardBacklashMonsterCountMultiplier) - 1f) * 100f);
	}

	private void ApplyFateMonsterCrushToActiveMonsters()
	{
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			if ((Object)(object)activeInstances[i] != (Object)null)
			{
				activeInstances[i].ApplyFateStatCrush(FateMonsterStatCrushRatio);
			}
		}
	}

	private void HandleMonsterSpawned(MonsterUnit monster)
	{
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)monster == (Object)null || !monster.CanBeCombatTargeted)
		{
			return;
		}
		int currentRound = CurrentRound;
		if (currentRound <= 0 || !IsRoundRunning)
		{
			return;
		}
		if (fateTimeStopRound == currentRound)
		{
			monster.ApplyStun(Mathf.Max(0.1f, fateCardTimeStopDuration));
			fateTimeStopAppliedCount++;
			if (fateTimeStopAppliedCount == 1)
			{
				this.OnBannerRequested?.Invoke("시간 정지 발동  이번 라운드 등장 전체", new Color(0.48f, 0.9f, 1f), 2f);
			}
		}
		if (fateThunderStrikeRound == currentRound)
		{
			float num = Mathf.Clamp01(fateCardThunderDamageRatio);
			monster.TakeDamage(monster.MaxHealth * num, critical: true, null);
			fateThunderStrikeAppliedCount++;
			if (fateThunderStrikeAppliedCount == 1)
			{
				this.OnBannerRequested?.Invoke("심판 번개 발동  이번 라운드 등장 전체", new Color(1f, 0.62f, 0.22f), 2f);
			}
		}
		if (CanOpenFateCard)
		{
			NotifyStateChanged();
		}
	}

	private void HealAllDefenders()
	{
		DefenderUnit[] array = (((Object)(object)boardManager != (Object)null) ? boardManager.GetAliveDefenders() : Object.FindObjectsOfType<DefenderUnit>());
		for (int i = 0; i < array.Length; i++)
		{
			if ((Object)(object)array[i] != (Object)null)
			{
				array[i].Heal(array[i].MaxHealth);
			}
		}
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
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		if (!enableFateIntervention)
		{
			this.OnBannerRequested?.Invoke("운명 개입 비활성", new Color(1f, 0.42f, 0.3f), 1.8f);
			return false;
		}
		int num = Mathf.Max(0, cost);
		if (fateGauge < num)
		{
			this.OnBannerRequested?.Invoke("운명 부족  " + fateGauge + "/" + num, new Color(1f, 0.42f, 0.3f), 1.8f);
			return false;
		}
		fateGauge = Mathf.Max(0, fateGauge - num);
		fateInterventionCount++;
		runFateInterventionCount++;
		AddFateDebt(debt, title);
		AddRunHighlightCard("운명 개입", detail + " / 게이지 -" + num + " / 빚 +" + Mathf.Max(0, debt));
		return true;
	}

	public int RemoveGold(int amount)
	{
		int num = Mathf.Clamp(amount, 0, Gold);
		if (num <= 0)
		{
			return 0;
		}
		Gold -= num;
		NotifyStateChanged();
		return num;
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

	public void AddTemporaryShopSummonDiscount(float rate, int roundCount)
	{
		int num = Mathf.Max(1, roundCount);
		int num2 = (IsRoundRunning ? Mathf.Max(1, CurrentRound) : Mathf.Max(1, CurrentRound + 1));
		temporaryShopSummonDiscountRate = Mathf.Max(temporaryShopSummonDiscountRate, Mathf.Clamp(rate, 0f, 0.45f));
		temporaryShopSummonDiscountUntilRound = Mathf.Max(temporaryShopSummonDiscountUntilRound, num2 + num - 1);
		NotifyStateChanged();
	}

	private void AddFateGauge(int amount, string reason)
	{
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		if (enableFateIntervention && !useOneShotFateCard && amount > 0)
		{
			int num = fateGauge;
			fateGauge = Mathf.Clamp(fateGauge + amount, 0, Mathf.Max(1, maxFateGauge));
			if (fateGauge > num && fateGauge >= Mathf.Max(1, maxFateGauge))
			{
				this.OnBannerRequested?.Invoke("운명 개입 준비 완료", new Color(1f, 0.82f, 0.28f), 1.8f);
			}
		}
	}

	private void AddFateDebt(int amount, string reason)
	{
		if (enableFateIntervention && amount > 0)
		{
			int num = fateDebt;
			fateDebt = Mathf.Clamp(fateDebt + amount, 0, Mathf.Max(1, maxFateDebt));
			int num2 = Mathf.Max(0, fateDebt - num);
			runFateDebtAdded += num2;
			runPeakFateDebt = Mathf.Max(runPeakFateDebt, fateDebt);
			if (useOneShotFateCard)
			{
				fateBossDebtAnchor = Mathf.Max(fateBossDebtAnchor, fateDebt);
			}
		}
	}

	private void RepayFateDebt(int amount, string reason)
	{
		if (enableFateIntervention && amount > 0 && fateDebt > 0)
		{
			int num = fateDebt;
			fateDebt = Mathf.Max(0, fateDebt - amount);
			int num2 = num - fateDebt;
			runFateDebtRepaid += Mathf.Max(0, num2);
			if (num2 > 0 && num >= Mathf.Max(1, maxFateDebt) / 2 && fateDebt < Mathf.Max(1, maxFateDebt) / 2)
			{
				AddRunHighlightCard("운명 빚 정산", reason + " / -" + num2);
			}
		}
	}

	private float ResolveFateDebtBossHealthMultiplier()
	{
		int num = (useOneShotFateCard ? Mathf.Max(fateDebt, fateBossDebtAnchor) : fateDebt);
		if (!enableFateIntervention || num <= 0)
		{
			return 1f;
		}
		return 1f + Mathf.Clamp01((float)num / (float)Mathf.Max(1, maxFateDebt)) * maxFateDebtBossHealthBonus;
	}

	public bool CanSellUnit(DefenderUnit unit, out string reason)
	{
		if (!enableUnitSelling)
		{
			reason = "판매 기능 비활성";
			return false;
		}
		if ((Object)(object)unit == (Object)null || (Object)(object)unit.CurrentSlot == (Object)null)
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
		if ((Object)(object)unit == (Object)null)
		{
			return 0;
		}
		int num = ResolveUnitSellBaseValue(unit.Grade);
		return Mathf.Max(1, Mathf.RoundToInt((float)num * Mathf.Clamp(unitSellRefundRate, 0.1f, 0.5f)));
	}

	private int ResolveUnitSellBaseValue(CharacterGrade grade)
	{
		return grade switch
		{
			CharacterGrade.Rare => Mathf.Max(1, rareUnitSellBaseValue), 
			CharacterGrade.Epic => Mathf.Max(1, epicUnitSellBaseValue), 
			CharacterGrade.Legendary => Mathf.Max(1, legendaryUnitSellBaseValue), 
			CharacterGrade.Mythic => Mathf.Max(1, mythicUnitSellBaseValue), 
			CharacterGrade.Transcendent => Mathf.Max(1, transcendentUnitSellBaseValue), 
			_ => Mathf.Max(1, normalUnitSellBaseValue), 
		};
	}

	public string GetUnitSellDetail(DefenderUnit unit)
	{
		if ((Object)(object)unit == (Object)null || unit.Definition == null)
		{
			return "유닛을 선택하면 판매할 수 있습니다.";
		}
		string text = CharacterGradeUtility.GetDisplayName(unit.Grade) + " " + unit.Definition.displayName + "  |  판매가 " + GetUnitSellRefund(unit) + "G";
		if (IsUnitSellMergeCandidate(unit))
		{
			text += "  |  합성 후보";
		}
		return text;
	}

	public bool UnitSellRequiresConfirmation(DefenderUnit unit)
	{
		return false;
	}

	public bool IsUnitSellMergeCandidate(DefenderUnit unit)
	{
		if ((Object)(object)unit == (Object)null || (Object)(object)unit.CurrentSlot == (Object)null)
		{
			return false;
		}
		if ((Object)(object)boardManager != (Object)null && boardManager.IsReservedForUltimateRecipeUnit(unit))
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
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		refund = 0;
		if (!CanSellUnit(unit, out message))
		{
			return false;
		}
		refund = GetUnitSellRefund(unit);
		string text = ((unit.Definition != null && !string.IsNullOrWhiteSpace(unit.Definition.displayName)) ? unit.Definition.displayName : "유닛");
		if ((Object)(object)boardManager == (Object)null || !boardManager.TryRemoveUnitFromBoard(unit))
		{
			message = "판매 실패: 유닛 상태 확인 필요";
			return false;
		}
		Gold += refund;
		message = text + " 판매 +" + refund + "G";
		this.OnBannerRequested?.Invoke(message, new Color(1f, 0.72f, 0.28f), 2f);
		NotifyStateChanged();
		return true;
	}

	public void RequestBanner(string message, Color color, float duration)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		this.OnBannerRequested?.Invoke(message, color, duration);
	}

	public void RecordBossSkillCast(SkillDefinition skill, bool majorBoss)
	{
		if (skill != null)
		{
			currentRoundBossSkillCasts++;
			totalBossSkillCasts++;
			currentRoundLastBossSkill = (string.IsNullOrWhiteSpace(skill.displayName) ? skill.effectType.ToString() : skill.displayName);
			lastBossSkill = currentRoundLastBossSkill;
			NotifyStateChanged();
		}
	}

	public void RecordBossSkillImpact(SkillDefinition skill, int affectedTargets, float damageDone, int goldDrained, bool majorBoss)
	{
		if (skill != null)
		{
			int num = Mathf.Max(0, affectedTargets);
			float num2 = Mathf.Max(0f, damageDone);
			int num3 = Mathf.Max(0, goldDrained);
			currentRoundBossAffectedTargets += num;
			totalBossAffectedTargets += num;
			currentRoundBossSkillDamage += num2;
			totalBossSkillDamage += num2;
			currentRoundBossGoldDrained += num3;
			totalBossGoldDrained += num3;
			switch (skill.effectType)
			{
			case SkillEffectType.ManaBurn:
				currentRoundBossManaBurnTargets += num;
				totalBossManaBurnTargets += num;
				break;
			case SkillEffectType.DeathPact:
				currentRoundBossExecutions += num;
				totalBossExecutions += num;
				break;
			case SkillEffectType.BossFortify:
				currentRoundBossFortifyCount++;
				totalBossFortifyCount++;
				break;
			case SkillEffectType.MonsterRally:
				currentRoundBossRallyTargets += num;
				totalBossRallyTargets += num;
				break;
			}
			NotifyStateChanged();
		}
	}

	public void MarkEarlyRunRecoveryOffered()
	{
		earlyRunRecoveryOfferCount++;
		earlyRunRecoveryShopOfferCount++;
		runRecoveryShopOffered = true;
		earlyRunRecoveryRecommended = false;
		if (string.IsNullOrWhiteSpace(earlyRunRecoveryReason) || earlyRunRecoveryReason == "초반 런 안정")
		{
			earlyRunRecoveryReason = earlyRunRecoveryCause + " 긴급 지원 제공 완료";
		}
		UpdateEarlyRunLogCoverageSummary();
		Debug.Log((object)("[EarlyRunTelemetry] 긴급지원 제공 " + earlyRunRecoveryOfferCount + "회 / " + earlyRunLogCoverageSummary));
		NotifyStateChanged();
	}

	public void RecordR3BoosterOffer()
	{
		if (enableEarlyRoundTelemetry)
		{
			earlyRunR3BoosterOfferCount++;
			runR3BoosterOffered = true;
			UpdateEarlyRunLogCoverageSummary();
			Debug.Log((object)("[EarlyRunTelemetry] R3 부스터 노출 " + earlyRunR3BoosterPurchaseCount + "/" + earlyRunR3BoosterOfferCount + " / " + earlyRunLogCoverageSummary));
			NotifyStateChanged();
		}
	}

	public void RecordR3BoosterPurchase()
	{
		if (enableEarlyRoundTelemetry)
		{
			earlyRunR3BoosterPurchaseCount++;
			runR3BoosterPurchased = true;
			UpdateEarlyRunLogCoverageSummary();
			Debug.Log((object)("[EarlyRunTelemetry] R3 부스터 구매 " + earlyRunR3BoosterPurchaseCount + "/" + earlyRunR3BoosterOfferCount + " / " + earlyRunLogCoverageSummary));
			NotifyStateChanged();
		}
	}

	public void RecordEarlyRecoveryShopPurchase()
	{
		if (enableEarlyRoundTelemetry)
		{
			earlyRunRecoveryShopPurchaseCount++;
			runRecoveryShopPurchased = true;
			UpdateEarlyRunLogCoverageSummary();
			Debug.Log((object)("[EarlyRunTelemetry] 긴급지원 선택 " + earlyRunRecoveryShopPurchaseCount + "/" + earlyRunRecoveryShopOfferCount + " / " + earlyRunLogCoverageSummary));
			NotifyStateChanged();
		}
	}

	public void MarkBadLuckInsuranceClaimed(string choiceName)
	{
		badLuckInsuranceOfferPending = false;
		runInsuranceClaimed = true;
		earlyRunRecoveryRecommended = false;
		badLuckInsuranceReason = (string.IsNullOrWhiteSpace(choiceName) ? "운 나쁨 보험 선택 완료" : ("운 나쁨 보험 선택: " + choiceName));
		earlyRunRecoveryReason = badLuckInsuranceReason;
		earlyRunRecoveryCause = "소환 부족";
		NotifyStateChanged();
	}

	public void RecordSynergySnapshot(int activeCount, string leadingSynergyTitle)
	{
		int previousCount = currentSynergyCount;
		string previousTitle = currentSynergyTitle;
		currentSynergyCount = Mathf.Max(0, activeCount);
		currentSynergyTitle = (string.IsNullOrWhiteSpace(leadingSynergyTitle) ? "시너지 없음" : leadingSynergyTitle);
		ReportSynergyActivationFeedback(previousCount, previousTitle);
		if (activeCount > bestSynergyCount)
		{
			bestSynergyCount = activeCount;
			bestSynergyTitle = (string.IsNullOrWhiteSpace(leadingSynergyTitle) ? "시너지 조합" : leadingSynergyTitle);
		}
	}

	public void AddCharacterContent(int additionalCount)
	{
		if (!((Object)(object)characterDatabase == (Object)null))
		{
			characterDatabase.ExpandGeneratedCharacterContent(additionalCount);
			NotifyStateChanged();
		}
	}

	public void AddMonsterContent(int additionalCount)
	{
		if (!((Object)(object)monsterDatabase == (Object)null))
		{
			int totalCount = Mathf.Max(monsterDatabase.Monsters.Count + additionalCount, monsterDatabase.Monsters.Count);
			monsterDatabase.GenerateStarterMonsters(totalCount);
			NotifyStateChanged();
		}
	}

	public bool TryGrantRandomUnitByGrade(CharacterGrade grade)
	{
		if ((Object)(object)characterDatabase == (Object)null || (Object)(object)boardManager == (Object)null)
		{
			return false;
		}
		CharacterDefinition characterDefinition = characterDatabase.GetRandomCharacterByGrade(grade, deployableOnly: true);
		if (characterDefinition == null)
		{
			characterDefinition = characterDatabase.GetRandomSummonableCharacter(GetSummonRateRound(), deployableOnly: true);
		}
		if (characterDefinition == null || !boardManager.TrySpawnUnit(characterDefinition, defaultUnitPrefab, out var spawnedUnit))
		{
			return false;
		}
		RegisterGrantedUnitExcitement(characterDefinition, spawnedUnit);
		this.OnUnitSummoned?.Invoke(characterDefinition);
		NotifyStateChanged();
		return true;
	}

	public bool TryGrantRandomSummonableUnit()
	{
		if ((Object)(object)characterDatabase == (Object)null || (Object)(object)boardManager == (Object)null)
		{
			return false;
		}
		CharacterDefinition randomSummonableCharacter = characterDatabase.GetRandomSummonableCharacter(GetSummonRateRound(), deployableOnly: true);
		if (randomSummonableCharacter == null || !boardManager.TrySpawnUnit(randomSummonableCharacter, defaultUnitPrefab, out var spawnedUnit))
		{
			return false;
		}
		RegisterGrantedUnitExcitement(randomSummonableCharacter, spawnedUnit);
		this.OnUnitSummoned?.Invoke(randomSummonableCharacter);
		NotifyStateChanged();
		return true;
	}

	public bool TryGrantMergeAssistUnit()
	{
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)characterDatabase == (Object)null || (Object)(object)boardManager == (Object)null)
		{
			return false;
		}
		CharacterGrade grade = SelectMergeAssistGrade();
		int num = CountUnitsOfGrade(grade);
		int num2 = ((num <= 0) ? 1 : Mathf.Max(1, 3 - num));
		int num3 = Mathf.Min(num2, Mathf.Max(0, EmptySlotCount));
		if (num3 <= 0)
		{
			return false;
		}
		int num4 = 0;
		CharacterDefinition characterDefinition = null;
		DefenderUnit spawnedUnit = null;
		for (int i = 0; i < num3; i++)
		{
			CharacterDefinition randomCharacterByGrade = characterDatabase.GetRandomCharacterByGrade(grade, deployableOnly: true);
			if (randomCharacterByGrade == null || !boardManager.TrySpawnUnit(randomCharacterByGrade, defaultUnitPrefab, out var spawnedUnit2))
			{
				break;
			}
			num4++;
			if (characterDefinition == null)
			{
				characterDefinition = randomCharacterByGrade;
				spawnedUnit = spawnedUnit2;
			}
			this.OnUnitSummoned?.Invoke(randomCharacterByGrade);
		}
		if (num4 <= 0)
		{
			return false;
		}
		RegisterGrantedUnitExcitement(characterDefinition, spawnedUnit);
		bool flag = num + num4 >= 3;
		string displayName = CharacterGradeUtility.GetDisplayName(grade);
		this.OnBannerRequested?.Invoke(flag ? ("합성 준비 완료!  " + displayName + " 재료 " + (num + num4) + "/3") : ("합성 재료 보급  " + displayName + " +" + num4), CharacterGradeUtility.GetColor(grade, Color.white), flag ? 2.6f : 2.1f);
		NotifyStateChanged();
		return true;
	}

	public int CountUnitsOfGrade(CharacterGrade grade)
	{
		return ((Object)(object)boardManager != (Object)null) ? boardManager.CountUnitsOfGrade(grade) : 0;
	}

	public bool CanMergeUltimate()
	{
		return (Object)(object)boardManager != (Object)null && boardManager.CanMergeUltimate(characterDatabase);
	}

	public UltimateRecipeOption[] GetReadyUltimateRecipeOptions()
	{
		return ((Object)(object)boardManager != (Object)null) ? boardManager.GetReadyUltimateRecipeOptions(characterDatabase) : new UltimateRecipeOption[0];
	}

	public UltimateRecipeOption[] GetAllUltimateRecipeOptions()
	{
		return ((Object)(object)boardManager != (Object)null) ? boardManager.GetAllUltimateRecipeOptions(characterDatabase) : new UltimateRecipeOption[0];
	}

	public void SetUltimateRecipePreview(string recipeName, bool previewActive = false)
	{
		if ((Object)(object)boardManager != (Object)null)
		{
			boardManager.SetUltimateRecipePreview(recipeName, previewActive);
		}
	}

	public string GetUltimateMergeStatus()
	{
		return ((Object)(object)boardManager != (Object)null) ? boardManager.GetUltimateMergeStatus(characterDatabase) : string.Empty;
	}

	public string GetUltimateMergeDetailStatus()
	{
		return ((Object)(object)boardManager != (Object)null) ? boardManager.GetUltimateMergeDetailStatus(characterDatabase) : string.Empty;
	}

	public string GetUltimateMergeActionStatus()
	{
		return ((Object)(object)boardManager != (Object)null) ? boardManager.GetUltimateMergeActionStatus(characterDatabase) : string.Empty;
	}

	public string GetUltimateRecipeBingoStatus()
	{
		return ((Object)(object)boardManager != (Object)null) ? boardManager.GetUltimateRecipeBingoStatus(characterDatabase) : string.Empty;
	}

	private void ResolveUltimateRecipeBingoReward()
	{
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		if (!enableFateIntervention || (Object)(object)boardManager == (Object)null)
		{
			return;
		}
		string[] readyUltimateRecipeNames = boardManager.GetReadyUltimateRecipeNames(characterDatabase);
		if (readyUltimateRecipeNames == null || readyUltimateRecipeNames.Length == 0)
		{
			return;
		}
		foreach (string text in readyUltimateRecipeNames)
		{
			if (!string.IsNullOrWhiteSpace(text) && !rewardedUltimateRecipeNames.Contains(text))
			{
				rewardedUltimateRecipeNames.Add(text);
				AddFateGauge(ultimateRecipeBingoFateGaugeBonus, "초월 레시피 빙고");
				AddRunHighlightCard("레시피 빙고", text + " / 운명 +" + Mathf.Max(0, ultimateRecipeBingoFateGaugeBonus));
				this.OnBannerRequested?.Invoke("레시피 빙고 완성!  운명 +" + Mathf.Max(0, ultimateRecipeBingoFateGaugeBonus), new Color(1f, 0.8f, 0.28f), 2.6f);
			}
		}
	}

	private void HandleMonsterKilled(MonsterUnit monster)
	{
		int num = (((Object)(object)monster != (Object)null) ? monster.GetRewardGold() : 0);
		Gold += num;
		if ((Object)(object)monster != (Object)null && monster.IsBoss)
		{
			totalBossKills++;
			string text = ((monster.Definition != null && !string.IsNullOrWhiteSpace(monster.Definition.displayName)) ? monster.Definition.displayName : "보스");
			AddRunHighlightCard("보스 처치", text + " / +" + num + "G");
			AddFateGauge(fateGaugeOnBossKill, "보스 처치");
		}
		RegisterKillCombo(monster);
		currentRoundKilledMonsters = Mathf.Min(currentRoundKilledMonsters + 1, Mathf.Max(0, RoundTargetCount));
		MarkRoundMonsterResolved();
		NotifyStateChanged();
	}

	private void HandleMonsterEscaped(MonsterUnit monster)
	{
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		MarkRoundMonsterResolved();
		int num = ResolveMonsterLeakDamage(monster);
		if (num > 0)
		{
			life = Mathf.Max(0, life - num);
			AddRunHighlightCard("방어선 돌파", "HP -" + num + " / " + life + "/" + MaxLife);
			this.OnBannerRequested?.Invoke("방어선 돌파!  HP -" + num, new Color(1f, 0.38f, 0.24f), 1.8f);
		}
		NotifyStateChanged();
		if (life <= 0)
		{
			TriggerLeakDefeat();
		}
	}

	private int ResolveMonsterLeakDamage(MonsterUnit monster)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		if (FateLeakShieldActive)
		{
			if (!fateLeakShieldFeedbackShown)
			{
				fateLeakShieldFeedbackShown = true;
				AddRunHighlightCard("최후의 방벽", "이번 라운드 누수 피해 무효");
				this.OnBannerRequested?.Invoke("최후의 방벽  누수 피해 0", new Color(0.45f, 0.78f, 1f), 1.8f);
			}
			return 0;
		}
		if ((Object)(object)monster == (Object)null || monster.Definition == null)
		{
			int rawDamage = 1;
			return ApplyEarlyLeakGrace(rawDamage);
		}
		return ApplyEarlyLeakGrace(monster.Definition.threatLevel switch
		{
			MonsterThreatLevel.Boss => Mathf.Max(5, Mathf.CeilToInt((float)MaxLife * 0.3f)), 
			MonsterThreatLevel.MidBoss => 2, 
			_ => 1, 
		});
	}

	private int ApplyEarlyLeakGrace(int rawDamage)
	{
		int num = Mathf.Max(0, rawDamage);
		if (num <= 0)
		{
			return 0;
		}
		if (CurrentRound <= Mathf.Max(0, earlyLeakGraceRoundLimit) && earlyRoundLeakDamageCap > 0)
		{
			int num2 = Mathf.Max(0, earlyRoundLeakDamageCap - currentRoundLeakDamageTaken);
			num = Mathf.Min(num, num2);
		}
		currentRoundLeakDamageTaken += num;
		return num;
	}

	private void TriggerLeakDefeat()
	{
		RequestDefeatAdjudication(defenderWipe: false);
	}

	private void RequestDefeatAdjudication(bool defenderWipe)
	{
		if (!gameOverRaised && defeatAdjudicationRoutine == null)
		{
			defeatAdjudicationRoutine = ((MonoBehaviour)this).StartCoroutine(AdjudicateDefeatAfterCombatFrame(defenderWipe));
		}
	}

	private IEnumerator AdjudicateDefeatAfterCombatFrame(bool defenderWipe)
	{
		yield return (object)new WaitForEndOfFrame();
		defeatAdjudicationRoutine = null;
		if (ShouldResolveSimultaneousDeathAsVictory())
		{
			life = Mathf.Max(1, life);
			gameOverRaised = false;
			AddRunHighlightCard("동시 격파 승리", "HP 1 생존 / 라운드 승리 우선");
			this.OnBannerRequested?.Invoke("동시 격파!  HP 1로 승리", new Color(1f, 0.82f, 0.24f), 2.8f);
			RuntimeAudioUtility.PlayJackpotMajor();
			NotifyStateChanged();
			yield break;
		}
		gameOverRaised = true;
		victoryStreak = 0;
		earnedGrowthCurrency = CalculateGrowthCurrency();
		if (defenderWipe)
		{
			AddRunHighlightCard("아군 전멸", "HP 0 / 전투 지속 불가");
			this.OnBannerRequested?.Invoke("아군 전멸", new Color(1f, 0.38f, 0.26f), 2.2f);
		}
		else
		{
			AddRunHighlightCard("방어선 붕괴", "HP 0 / 몬스터 돌파");
		}
		BeginDefeatSequence();
	}

	private bool ShouldResolveSimultaneousDeathAsVictory()
	{
		if ((Object)(object)roundManager == (Object)null || RoundTargetCount <= 0)
		{
			return false;
		}
		int num = 0;
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if ((Object)(object)monsterUnit != (Object)null && monsterUnit.CurrentHealth <= 0f)
			{
				num++;
			}
		}
		return IsSimultaneousDeathVictory(RoundTargetCount, currentRoundKilledMonsters, num);
	}

	public static bool IsSimultaneousDeathVictory(int targetMonsterCount, int killedMonsterCount, int fatalMonstersPendingResolution)
	{
		int num = Mathf.Max(0, targetMonsterCount);
		int num2 = Mathf.Clamp(killedMonsterCount, 0, num);
		int num3 = Mathf.Max(0, fatalMonstersPendingResolution);
		return num > 0 && num2 + num3 >= num;
	}

	private void CancelPendingDefeatAdjudication()
	{
		if (defeatAdjudicationRoutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(defeatAdjudicationRoutine);
			defeatAdjudicationRoutine = null;
		}
	}

	private void BeginDefeatSequence()
	{
		RestoreFateChoiceSlowMotion();
		NotifyStateChanged();
		if ((Object)(object)roundManager != (Object)null && roundManager.IsRoundRunning)
		{
			roundManager.BeginDefeatCinematic();
			CancelPendingDefeatFinalization();
			defeatFinalizeRoutine = ((MonoBehaviour)this).StartCoroutine(FinalizeDefeatAfterCinematic());
		}
		this.OnGameOver?.Invoke();
	}

	private IEnumerator FinalizeDefeatAfterCinematic()
	{
		CaptureDefeatTimeScale();
		IsDefeatSlowMotionActive = true;
		float startScale = Mathf.Max(0.0001f, defeatPreviousTimeScale);
		float elapsed = 0f;
		while (elapsed < 5f)
		{
			elapsed = Mathf.Min(5f, elapsed + Time.unscaledDeltaTime);
			float normalized = Mathf.Clamp01(elapsed / 5f);
			float scale = (Time.timeScale = Mathf.SmoothStep(startScale, 0.1f, normalized));
			Time.fixedDeltaTime = Mathf.Max(0.001f, defeatPreviousFixedDeltaTime * scale / startScale);
			yield return null;
		}
		Time.timeScale = 0.1f;
		Time.fixedDeltaTime = Mathf.Max(0.001f, defeatPreviousFixedDeltaTime * 0.1f / startScale);
		if ((Object)(object)roundManager != (Object)null && roundManager.IsRoundRunning)
		{
			roundManager.ForceFailRound();
		}
		yield return (object)new WaitForSecondsRealtime(0.1f);
		RestoreDefeatTimeScale();
		defeatFinalizeRoutine = null;
	}

	private void CancelPendingDefeatFinalization()
	{
		if (defeatFinalizeRoutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(defeatFinalizeRoutine);
			defeatFinalizeRoutine = null;
		}
		RestoreDefeatTimeScale();
	}

	private void CaptureDefeatTimeScale()
	{
		if (!defeatTimeScaleCaptured)
		{
			float num = Mathf.Max(0.0001f, Time.timeScale);
			defeatPreviousTimeScale = ((num <= 0.3f) ? 1f : num);
			defeatPreviousFixedDeltaTime = ((Time.fixedDeltaTime > 0f) ? (Time.fixedDeltaTime * defeatPreviousTimeScale / num) : (0.02f * defeatPreviousTimeScale));
			defeatTimeScaleCaptured = true;
		}
	}

	private void RestoreDefeatTimeScale()
	{
		IsDefeatSlowMotionActive = false;
		if (defeatTimeScaleCaptured)
		{
			Time.timeScale = ((defeatPreviousTimeScale > 0f) ? defeatPreviousTimeScale : 1f);
			Time.fixedDeltaTime = ((defeatPreviousFixedDeltaTime > 0f) ? defeatPreviousFixedDeltaTime : 0.02f);
			defeatTimeScaleCaptured = false;
		}
	}

	private void UpdateFateChoiceSlowMotion()
	{
		if (fateCardChoicePanelOpen && !IsFateCardCombatChoiceAvailable())
		{
			fateCardChoicePanelOpen = false;
			RestoreFateChoiceSlowMotion();
			NotifyStateChanged();
		}
		if (!CanUseFateCard || IsDefeatSlowMotionActive)
		{
			RestoreFateChoiceSlowMotion();
			return;
		}
		CaptureFateChoiceSlowMotion();
		float num = (Time.timeScale = Mathf.Clamp(fateChoiceTimeScale, 0.02f, 1f));
		float num3 = Mathf.Max(0.0001f, fateChoicePreviousTimeScale);
		Time.fixedDeltaTime = Mathf.Max(0.001f, fateChoicePreviousFixedDeltaTime * num / num3);
	}

	private void CaptureFateChoiceSlowMotion()
	{
		if (!fateChoiceSlowMotionActive)
		{
			float num = (fateChoicePreviousTimeScale = Mathf.Max(0.0001f, Time.timeScale));
			fateChoicePreviousFixedDeltaTime = ((Time.fixedDeltaTime > 0f) ? Time.fixedDeltaTime : (0.02f * num));
			fateChoiceSlowMotionActive = true;
		}
	}

	private void RestoreFateChoiceSlowMotion()
	{
		if (fateChoiceSlowMotionActive)
		{
			Time.timeScale = ((fateChoicePreviousTimeScale > 0f) ? fateChoicePreviousTimeScale : 1f);
			Time.fixedDeltaTime = ((fateChoicePreviousFixedDeltaTime > 0f) ? fateChoicePreviousFixedDeltaTime : 0.02f);
			fateChoicePreviousTimeScale = 1f;
			fateChoicePreviousFixedDeltaTime = 0.02f;
			fateChoiceSlowMotionActive = false;
		}
	}

	private void HandleRoundStateChanged(int round, bool bossRound, bool running)
	{
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_0416: Unknown result type (might be due to invalid IL or missing references)
		fateCardChoicePanelOpen = false;
		if (running)
		{
			DismissTemporarySummons();
			currentRoundKilledMonsters = 0;
			currentRoundResolvedMonsters = 0;
			currentRoundLeakDamageTaken = 0;
			ResetRoundTileContribution();
			BeginEarlyRoundTelemetry(round);
			TryApplyFirstBossSummonRushBonus(round, bossRound);
			if (FateMonsterStatCrushActive)
			{
				ApplyFateMonsterCrushToActiveMonsters();
			}
			AnnounceEarlyCrisisRound(round);
			this.OnRoundStarted?.Invoke(round);
		}
		else
		{
			RestoreFateChoiceSlowMotion();
			currentRoundResolvedMonsters = Mathf.Max(currentRoundResolvedMonsters, RoundTargetCount);
			ClearRoundCombatEffects();
			if (fateMonsterCrushRound == round)
			{
				fateMonsterCrushRound = -1;
			}
			if (fateMonsterSurgeRound == round)
			{
				fateMonsterSurgeRound = -1;
			}
			if (fateTimeStopRound == round)
			{
				AddRunHighlightCard("시간 정지 결과", "R" + round + " " + fateTimeStopAppliedCount + "기 기절");
				fateTimeStopRound = -1;
				fateTimeStopAppliedCount = 0;
			}
			if (fateThunderStrikeRound == round)
			{
				AddRunHighlightCard("심판 번개 결과", "R" + round + " " + fateThunderStrikeAppliedCount + "기 타격");
				fateThunderStrikeRound = -1;
				fateThunderStrikeAppliedCount = 0;
			}
			if (fateLeakShieldRound == round)
			{
				fateLeakShieldRound = -1;
				fateLeakShieldFeedbackShown = false;
			}
			if (fateSummonTaxUntilRound > 0 && round >= fateSummonTaxUntilRound)
			{
				fateSummonTaxUntilRound = -1;
				fateSummonTaxRate = 0f;
			}
			if (fateSummonDiscountUntilRound > 0 && round >= fateSummonDiscountUntilRound)
			{
				fateSummonDiscountUntilRound = -1;
				fateSummonDiscountRate = 0f;
			}
			if (fateCombatEditingRound == round)
			{
				fateCombatEditingUnlocked = false;
				fateCombatEditingRound = -1;
			}
		}
		if (!running && (Object)(object)boardManager != (Object)null)
		{
			DismissTemporarySummons();
			if ((Object)(object)roundManager != (Object)null && roundManager.LastRoundEndedByDefeat)
			{
				CompleteEarlyRoundTelemetry(round, bossRound, cleared: false);
				RecordRoundDefeatMoment(round, bossRound);
				NotifyStateChanged();
				return;
			}
			int num = (LastRoundClearGoldReward = CalculateRoundClearGold(round));
			Gold += num;
			victoryStreak++;
			if (!debugRoundAdvanceInProgress)
			{
				CompleteEarlyRoundTelemetry(round, bossRound, cleared: true);
			}
			ResolveFateRoundClear(bossRound);
			int num3 = boardManager.RefreshSlotLocks(round, playUnlockFeedback: true);
			DefenderUnit[] aliveDefenders = boardManager.GetAliveDefenders();
			for (int i = 0; i < aliveDefenders.Length; i++)
			{
				if ((Object)(object)aliveDefenders[i] != (Object)null)
				{
					aliveDefenders[i].ResetFacingToDefault();
					aliveDefenders[i].PlayWinAnimation();
				}
			}
			this.OnBannerRequested?.Invoke("ROUND CLEAR  +" + num + "G", new Color(0.48f, 1f, 0.72f), 2.5f);
			ReportRoundCombatRecap(bossRound);
			if (num3 > 0)
			{
				int num4 = Mathf.Max(1, round + 1);
				this.OnBannerRequested?.Invoke("ROUND " + num4 + "  전방 배치칸 개방 +" + num3 + "  |  +" + num + "G", new Color(0.46f, 1f, 0.82f), 3f);
				AddRunHighlightCard("전방 슬롯 개방", "ROUND " + num4 + " / +" + num3);
			}
			this.OnRoundMissionSettlement?.Invoke(round);
			this.OnRoundEconomySettlement?.Invoke(round);
			ResolveEarlyRunFallback(round);
			ResolveEarlyBossPrepReward(round);
			this.OnRoundBoardPreparation?.Invoke(round);
			pendingPostRoundChoiceRound = round;
			this.OnRoundCompleted?.Invoke(round);
		}
		NotifyStateChanged();
	}

	private void ClearRoundCombatEffects()
	{
		RuntimeEffectUtility.ClearTrackedEffects();
		DefenderUnit[] array = (((Object)(object)boardManager != (Object)null) ? boardManager.GetAliveDefenders() : Object.FindObjectsOfType<DefenderUnit>());
		for (int i = 0; i < array.Length; i++)
		{
			if ((Object)(object)array[i] != (Object)null)
			{
				array[i].ClearRoundTemporaryEffects();
			}
		}
	}

	private void DismissTemporarySummons()
	{
		DefenderUnit[] array = Object.FindObjectsOfType<DefenderUnit>();
		for (int i = 0; i < array.Length; i++)
		{
			if ((Object)(object)array[i] != (Object)null && array[i].IsTemporarySummon)
			{
				array[i].DismissTemporarySummon();
			}
		}
	}

	private void HandleDefenderRemoved(DefenderUnit defender)
	{
		if (IsRoundRunning && !gameOverRaised && defeatAdjudicationRoutine == null && CountAliveDefendersInScene() <= 0)
		{
			life = 0;
			NotifyStateChanged();
			RequestDefeatAdjudication(defenderWipe: true);
		}
	}

	private int CountAliveDefendersInScene()
	{
		DefenderUnit[] array = Object.FindObjectsOfType<DefenderUnit>();
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			if ((Object)(object)array[i] != (Object)null && !array[i].IsTemporarySummon && array[i].CurrentHealth > 0f)
			{
				num++;
			}
		}
		return num;
	}

	private void MarkRoundMonsterResolved()
	{
		if (IsRoundRunning && RoundTargetCount > 0)
		{
			currentRoundResolvedMonsters = Mathf.Min(currentRoundResolvedMonsters + 1, RoundTargetCount);
		}
	}

	private void HandleRoundCountdownChanged(int countdown)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		this.OnRoundCountdownChanged?.Invoke(countdown);
		if (countdown > 0)
		{
			RuntimeAudioUtility.PlayCountdown();
			this.OnBannerRequested?.Invoke("ROUND " + CurrentRound + " STARTS IN", new Color(0.98f, 0.88f, 0.42f), 1.05f);
		}
	}

	private void HandleCombatTimeScaleChanged(int multiplier)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		if (multiplier >= 2 && IsRoundRunning && !FateChoiceSlowMotionActive && !IsDefeatSlowMotionActive)
		{
			this.OnBannerRequested?.Invoke("장기전 가속  ×" + multiplier, new Color(1f, 0.78f, 0.26f), 1.15f);
		}
	}

	private void SubscribeRoundManager()
	{
		if ((Object)(object)roundManager != (Object)null)
		{
			roundManager.OnRoundStateChanged -= HandleRoundStateChanged;
			roundManager.OnRoundStateChanged += HandleRoundStateChanged;
			roundManager.OnCountdownChanged -= HandleRoundCountdownChanged;
			roundManager.OnCountdownChanged += HandleRoundCountdownChanged;
			roundManager.OnCombatTimeScaleChanged -= HandleCombatTimeScaleChanged;
			roundManager.OnCombatTimeScaleChanged += HandleCombatTimeScaleChanged;
		}
	}

	private void UnsubscribeRoundManager()
	{
		if ((Object)(object)roundManager != (Object)null)
		{
			roundManager.OnRoundStateChanged -= HandleRoundStateChanged;
			roundManager.OnCountdownChanged -= HandleRoundCountdownChanged;
			roundManager.OnCombatTimeScaleChanged -= HandleCombatTimeScaleChanged;
		}
	}

	private void NotifyStateChanged()
	{
		this.OnStateChanged?.Invoke();
	}

	public void ReleasePostRoundChoiceFlow()
	{
		int num = pendingPostRoundChoiceRound;
		if (num > 0)
		{
			pendingPostRoundChoiceRound = -1;
			this.OnRoundShopPhase?.Invoke(num);
			this.OnRoundAugmentChoicePhase?.Invoke(num);
			NotifyStateChanged();
		}
	}

	private void HandleDamageDealt(DefenderUnit source, MonsterUnit target, float damage, bool critical)
	{
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)source == (Object)null || damage <= 0f)
		{
			return;
		}
		string text = ((source.Definition != null) ? source.Definition.displayName : ((Object)source).name);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "Unknown Hero";
		}
		totalDamageDealt += damage;
		RecordTileDamageContribution(source, target, damage);
		float previousDamage = topDamageHeroDamage;
		string previousHeroName = topDamageHeroName;
		if (critical)
		{
			criticalHitCount++;
			if (Time.time >= nextCriticalBannerTime)
			{
				nextCriticalBannerTime = Time.time + 1.4f;
				this.OnBannerRequested?.Invoke("CRITICAL!", new Color(1f, 0.78f, 0.24f), 0.9f);
			}
		}
		AddDamageContribution(damageByHero, text, damage);
		AddDamageContribution(currentRoundDamageByHero, text, damage);
		if (damageByHero[text] > topDamageHeroDamage)
		{
			topDamageHeroDamage = damageByHero[text];
			topDamageHeroName = text;
			ReportTopDamageFeedback(text, topDamageHeroDamage, previousHeroName, previousDamage);
		}
		if (currentRoundDamageByHero[text] > roundTopDamageHeroDamage)
		{
			roundTopDamageHeroDamage = currentRoundDamageByHero[text];
			roundTopDamageHeroName = text;
		}
	}

	private void RegisterKillCombo(MonsterUnit monster)
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		float time = Time.time;
		currentKillCombo = ((!(time - lastKillTime <= 2.2f)) ? 1 : (currentKillCombo + 1));
		lastKillTime = time;
		bestKillCombo = Mathf.Max(bestKillCombo, currentKillCombo);
		if (currentKillCombo >= 5 && currentKillCombo % 5 == 0)
		{
			this.OnBannerRequested?.Invoke(currentKillCombo + " COMBO!", new Color(1f, 0.82f, 0.24f), 1.35f);
			RuntimeCameraShake.Request(0.035f, 0.1f);
		}
		if ((Object)(object)monster != (Object)null && monster.IsBoss)
		{
			Color val = (Color)((monster.Definition != null) ? monster.Definition.accentColor : new Color(1f, 0.58f, 0.24f));
			int rewardGold = monster.GetRewardGold();
			string text = Mathf.RoundToInt(currentRoundBossTileDamage).ToString("N0");
			string unitName = ((monster.Definition != null && !string.IsNullOrWhiteSpace(monster.Definition.displayName)) ? monster.Definition.displayName : "Boss");
			string gradeLabel = ((monster.Definition != null && monster.Definition.IsMajorBoss) ? "BOSS" : "MID BOSS");
			this.OnBannerRequested?.Invoke("보스 처치!  +" + rewardGold + "G  |  대응 " + text, val, 2.8f);
			RuntimeAudioUtility.PlayJackpotMajor();
			RuntimeGameFeel.PlayJackpotPulse(((Component)monster).transform.position, val, (monster.Definition != null && monster.Definition.IsMajorBoss) ? 2.15f : 1.55f, (monster.Definition != null && monster.Definition.IsMajorBoss) ? 0.2f : 0.14f, 0.42f, 0.15f, 0.1f, 3);
			RuntimeGameFeel.ShowJackpotReveal("보스 처치!", gradeLabel, unitName, val, "+" + rewardGold + "G / 대응 " + text, 2.4f);
		}
	}

	private int CalculateGrowthCurrency()
	{
		int num = Mathf.Max(0, CurrentRound) * 2;
		int num2 = Mathf.Max(0, bestSynergyCount) * 3;
		int num3 = Mathf.Max(0, bestKillCombo / 5) * 2;
		int num4 = Mathf.Clamp(Mathf.RoundToInt(totalDamageDealt / 2500f), 0, 20);
		return Mathf.Max(3, num + num2 + num3 + num4);
	}

	private void ResetRunStats()
	{
		RestoreFateChoiceSlowMotion();
		fateCardChoicePanelOpen = false;
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
		initialPreparationSummons = 0;
		initialPreparationClosed = false;
		earlyRunMomentTriggered = false;
		earlyFallbackRewardGranted = false;
		earlyBossPrepRewardGranted = false;
		earlyRoundTelemetry.Clear();
		currentRoundStartTime = 0f;
		currentRoundStartGold = 0;
		currentRoundSummonCount = 0;
		fateMonsterSurgeRound = -1;
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
		badLuckInsuranceOfferPending = false;
		badLuckInsuranceOffered = false;
		badLuckInsuranceReason = "초반 소환 보험 대기";
		luckySummonNormalStreak = 0;
		luckySummonReady = false;
		luckySummonConsumed = false;
		luckySummonChoiceOpen = false;
		earlyRunTuningLogRecorded = false;
		runR3BoosterOffered = false;
		runR3BoosterPurchased = false;
		runRecoveryShopOffered = false;
		runRecoveryShopPurchased = false;
		runInsuranceOffered = false;
		runInsuranceClaimed = false;
		runR10BossHealthRemaining01 = -1f;
		firstRarePlusRound = -1;
		lastRoundShopOpenRound = -1;
		firstMergeRound = -1;
		fateGauge = (enableFateIntervention ? Mathf.Clamp(startingFateGauge, 0, Mathf.Max(1, maxFateGauge)) : 0);
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
		runFateMonsterCrushCount = 0;
		runFateCombatDraftCount = 0;
		runFateFullHealCount = 0;
		runFateDebtAdded = 0;
		runFateDebtRepaid = 0;
		runFateShopCostPenaltyGold = 0;
		runPeakFateDebt = 0;
		fateGradeLockSummonsRemaining = 0;
		fateBossDebtAnchor = 0;
		fateGradeLockMinimum = CharacterGrade.Normal;
		fateNormalBanSummonsRemaining = 0;
		fateForceNextShop = false;
		fateCardUsed = false;
		fateCardChoicesInitialized = false;
		pendingPostRoundChoiceRound = -1;
		fateCardLastTitle = "미사용";
		fateCardLastDetail = "운명 카드 대기";
		fateCardLastDebt = 0;
		fateMonsterCrushRound = -1;
		fateTimeStopRound = -1;
		fateTimeStopAppliedCount = 0;
		fateThunderStrikeRound = -1;
		fateThunderStrikeAppliedCount = 0;
		fateLeakShieldRound = -1;
		fateLeakShieldFeedbackShown = false;
		fateSummonTaxUntilRound = -1;
		fateSummonTaxRate = 0f;
		fateSummonDiscountUntilRound = -1;
		fateSummonDiscountRate = 0f;
		fateCombatEditingRound = -1;
		fateCombatEditingUnlocked = false;
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
		int num = ((currentSummonBaseCost > 0) ? currentSummonBaseCost : summonCost);
		float num2 = (float)num * (1f - Mathf.Clamp01(summonCostDiscountRate));
		int num3 = Mathf.Max(1, Mathf.RoundToInt(num2));
		int summonRateRound = GetSummonRateRound();
		if (temporaryShopSummonDiscountUntilRound >= summonRateRound && temporaryShopSummonDiscountRate > 0f)
		{
			num3 = Mathf.Max(1, Mathf.RoundToInt((float)num3 * (1f - Mathf.Clamp01(temporaryShopSummonDiscountRate))));
		}
		if (enableFateIntervention && useOneShotFateCard && fateSummonDiscountUntilRound >= GetSummonRateRound() && fateSummonDiscountRate > 0f)
		{
			num3 = Mathf.Max(1, Mathf.RoundToInt((float)num3 * (1f - Mathf.Clamp01(fateSummonDiscountRate))));
		}
		if (enableFateIntervention && useOneShotFateCard && fateSummonTaxUntilRound >= GetSummonRateRound() && fateSummonTaxRate > 0f)
		{
			num3 = Mathf.Max(1, Mathf.CeilToInt((float)num3 * (1f + Mathf.Clamp01(fateSummonTaxRate))));
		}
		return num3;
	}

	private int ResolveSummonCostIncrease()
	{
		int summonRateRound = GetSummonRateRound();
		if (summonRateRound <= Mathf.Max(1, earlySummonCostRampRoundLimit))
		{
			return Mathf.Max(0, earlySummonCostIncreasePerSummon);
		}
		return Mathf.Max(0, summonCostIncreasePerSummon);
	}

	private int GetSummonRateRound()
	{
		int num = CurrentRound;
		if (!IsRoundRunning)
		{
			num++;
		}
		return Mathf.Max(1, num);
	}

	private int CalculateRoundClearGold(int round)
	{
		return Mathf.Max(0, roundClearBaseGold) + Mathf.FloorToInt((float)Mathf.Max(0, round) * Mathf.Max(0f, roundClearPerRoundGold)) + victoryStreak * victoryStreakGoldBonus + roundGoldBonus;
	}

	private void ResolveFateRoundClear(bool bossRound)
	{
		if (!enableFateIntervention)
		{
			return;
		}
		AddFateGauge(fateGaugeOnRoundClear, "라운드 클리어");
		if (MaxLife > 0 && (float)life / (float)MaxLife <= earlyLowLifeRecoveryRatio)
		{
			AddFateGauge(fateGaugeOnLowLife, "낮은 생명력");
		}
		int amount = Mathf.Max(0, fateDebtRepayPerRound) + (bossRound ? Mathf.Max(0, fateDebtRepayPerBossRound) : 0);
		RepayFateDebt(amount, bossRound ? "보스 라운드 클리어" : "라운드 클리어");
		if (bossRound && useOneShotFateCard && fateBossDebtAnchor > 0)
		{
			int num = fateBossDebtAnchor;
			fateBossDebtAnchor = 0;
			if (fateCardUsed)
			{
				AddRunHighlightCard("운명 대가 정산", "보스 강화 빚 " + num + " 종료");
			}
		}
	}

	private CharacterDefinition SelectSummonDefinition(out bool earlyPitySummon)
	{
		earlyPitySummon = false;
		int summonRateRound = GetSummonRateRound();
		CharacterDefinition randomSummonableCharacter = characterDatabase.GetRandomSummonableCharacter(summonRateRound, deployableOnly: true);
		return ApplyFateSummonIntervention(randomSummonableCharacter);
	}

	private CharacterDefinition ApplyFateSummonIntervention(CharacterDefinition selected)
	{
		if (!enableFateIntervention || (Object)(object)characterDatabase == (Object)null || selected == null)
		{
			return selected;
		}
		CharacterDefinition characterDefinition = selected;
		bool flag = false;
		if (fateGradeLockSummonsRemaining > 0 && characterDefinition.grade < fateGradeLockMinimum)
		{
			CharacterDefinition randomCharacterByGrade = characterDatabase.GetRandomCharacterByGrade(fateGradeLockMinimum, deployableOnly: true);
			if (randomCharacterByGrade != null)
			{
				characterDefinition = randomCharacterByGrade;
				flag = true;
			}
		}
		if (fateNormalBanSummonsRemaining > 0 && characterDefinition.grade == CharacterGrade.Normal)
		{
			CharacterDefinition characterDefinition2 = characterDatabase.GetRandomCharacterByGrade(CharacterGrade.Rare, deployableOnly: true) ?? characterDatabase.GetRandomCharacterByGrade(CharacterGrade.Epic, deployableOnly: true) ?? characterDatabase.GetRandomSummonableCharacter(GetSummonRateRound(), deployableOnly: true);
			if (characterDefinition2 != null)
			{
				characterDefinition = characterDefinition2;
				flag = true;
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
		if (flag)
		{
			AddRunHighlightCard("확률 조작", CharacterGradeUtility.GetDisplayName(characterDefinition.grade) + " " + characterDefinition.displayName);
		}
		return characterDefinition;
	}

	private void RegisterSummonExcitement(CharacterDefinition summon, bool earlyPitySummon, DefenderUnit spawnedUnit, bool trackLuckyStreak = true)
	{
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		earlySummonAttempts++;
		if (summon != null)
		{
			int summonRateRound = GetSummonRateRound();
			bool flag = summon.grade >= CharacterGrade.Rare;
			if (!flag)
			{
				AddFateGauge(fateGaugeOnLowSummon, "저점 소환");
			}
			if (trackLuckyStreak)
			{
				TrackLuckySummonStreak(summon, summonRateRound);
			}
			if (flag)
			{
				earlyRunMomentTriggered = true;
				PlaySummonJackpotPresentation(summon, spawnedUnit, earlyPitySummon, spawnSoundAlreadyPlayed: true);
			}
			if (enableEarlyRunFunPacing && summonRateRound <= Mathf.Max(1, earlyFunRoundLimit) && !flag && earlyPitySummon)
			{
				this.OnBannerRequested?.Invoke("초반 찬스 소환!  " + CharacterGradeUtility.GetDisplayName(summon.grade) + " " + summon.displayName, summon.accentColor, 2.4f);
				RuntimeCameraShake.Request(0.055f, 0.16f);
			}
		}
	}

	private void TrackLuckySummonStreak(CharacterDefinition summon, int summonRateRound)
	{
		if (enableLuckySummonComeback && !luckySummonConsumed && summon != null)
		{
			if (summon.grade >= CharacterGrade.Rare)
			{
				luckySummonNormalStreak = 0;
				luckySummonReady = false;
				luckySummonChoiceOpen = false;
			}
			else
			{
				luckySummonNormalStreak++;
				RefreshLuckySummonReadiness();
			}
		}
	}

	private void RegisterMergeExcitement(MergeResultInfo mergeResult)
	{
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		if (mergeResult.resultGrade >= CharacterGrade.Rare)
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
		if (mergeResult.resultGrade >= CharacterGrade.Rare)
		{
			string text = (mergeResult.isFinalMerge ? "초월 완성!" : ((mergeResult.resultGrade < CharacterGrade.Epic) ? ((CurrentRound <= Mathf.Max(1, earlyFunRoundLimit)) ? "첫 레어 합성!" : "레어 합성!") : ((CurrentRound <= Mathf.Max(1, earlyFunRoundLimit)) ? "초반 대박 합성!" : "대박 합성!")));
			bool flag = mergeResult.isFinalMerge || mergeResult.resultGrade >= CharacterGrade.Epic;
			AddRunHighlightCard(TrimHighlightTitle(text), CharacterGradeUtility.GetDisplayName(mergeResult.resultGrade) + " " + mergeResult.resultCharacterName);
			this.OnBannerRequested?.Invoke(text + "  " + CharacterGradeUtility.GetDisplayName(mergeResult.resultGrade) + " " + mergeResult.resultCharacterName, mergeResult.resultColor, ShortenSummonMergePresentation(mergeResult.isFinalMerge ? 3.2f : (flag ? 2.7f : 2.3f)));
			RuntimeGameFeel.ShowJackpotReveal(text, CharacterGradeUtility.GetDisplayName(mergeResult.resultGrade), mergeResult.resultCharacterName, mergeResult.resultColor, mergeResult.isFinalMerge ? "초월 완성 / 전투 판도 변화" : (flag ? "합성 성공 / 전력 상승" : "첫 고점 / 초반 전력 상승"), ShortenSummonMergePresentation(mergeResult.isFinalMerge ? 3f : (flag ? 2.45f : 2.05f)));
			if (mergeResult.isFinalMerge)
			{
				RuntimeCameraShake.Request(0.22f, 0.48f);
			}
			else
			{
				RuntimeCameraShake.Request((mergeResult.resultGrade >= CharacterGrade.Legendary) ? 0.15f : (flag ? 0.105f : 0.075f), flag ? 0.3f : 0.2f);
			}
		}
	}

	private void RegisterGrantedUnitExcitement(CharacterDefinition definition, DefenderUnit spawnedUnit)
	{
		if (definition != null && definition.grade >= CharacterGrade.Rare)
		{
			RecordFirstRarePlusRound(definition);
			earlyRunMomentTriggered = true;
			PlaySummonJackpotPresentation(definition, spawnedUnit, earlyPitySummon: false);
		}
	}

	private void PlaySummonJackpotPresentation(CharacterDefinition definition, DefenderUnit spawnedUnit, bool earlyPitySummon, bool spawnSoundAlreadyPlayed = false)
	{
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		if (definition == null || definition.grade < CharacterGrade.Rare)
		{
			return;
		}
		bool flag = definition.grade == CharacterGrade.Transcendent;
		bool flag2 = flag || definition.grade >= CharacterGrade.Epic;
		bool flag3 = definition.grade == CharacterGrade.Legendary || definition.grade == CharacterGrade.Mythic;
		string displayName = CharacterGradeUtility.GetDisplayName(definition.grade);
		string text = (flag ? "초월 소환!" : (flag2 ? "대박 소환!" : (earlyPitySummon ? "초반 찬스 소환!" : "희귀 소환!")));
		AddRunHighlightCard(TrimHighlightTitle(text), displayName + " " + definition.displayName);
		this.OnBannerRequested?.Invoke(text + "  " + displayName + " " + definition.displayName, definition.accentColor, ShortenSummonMergePresentation(flag ? 3.3f : (flag2 ? 2.8f : 2.35f)));
		RuntimeGameFeel.ShowJackpotReveal(text, displayName, definition.displayName, definition.accentColor, BuildJackpotUnitDetail(definition, earlyPitySummon), ShortenSummonMergePresentation(flag ? 3.05f : (flag2 ? 2.6f : 2.15f)));
		Vector3 position = (((Object)(object)spawnedUnit != (Object)null) ? ((Component)spawnedUnit).transform.position : ((Component)this).transform.position);
		if (definition.grade == CharacterGrade.Mythic || flag)
		{
			RuntimeGameFeel.PlayHighGradeSummonVfx(position, definition.accentColor, definition.grade);
		}
		if (flag)
		{
			RuntimeAudioUtility.PlayJackpotUltimate();
			RuntimeGameFeel.PlayJackpotPulse(position, definition.accentColor, 2.1f, 0.22f, 0.48f, 0.12f, 0.16f, 4);
			return;
		}
		if (definition.grade == CharacterGrade.Mythic)
		{
			RuntimeAudioUtility.PlayMythicSpawn();
			RuntimeGameFeel.PlayJackpotPulse(position, definition.accentColor, 1.85f, 0.17f, 0.38f, 0.16f, 0.13f, 4);
			return;
		}
		if (flag3)
		{
			RuntimeAudioUtility.PlayJackpotMajor();
			RuntimeGameFeel.PlayJackpotPulse(position, definition.accentColor, 1.55f, 0.15f, 0.34f, 0.18f, 0.12f, 3);
			return;
		}
		if (!spawnSoundAlreadyPlayed)
		{
			RuntimeAudioUtility.PlayDiceAppear();
		}
		RuntimeGameFeel.PlayJackpotPulse(position, definition.accentColor, 1.18f, 0.09f, 0.22f, 0.3f, 0.075f, 2);
	}

	private static float ShortenSummonMergePresentation(float duration)
	{
		return Mathf.Max(0.8f, duration - 1f);
	}

	private CharacterGrade SelectMergeAssistGrade()
	{
		CharacterGrade[] array = new CharacterGrade[4]
		{
			CharacterGrade.Legendary,
			CharacterGrade.Epic,
			CharacterGrade.Rare,
			CharacterGrade.Normal
		};
		for (int i = 0; i < array.Length; i++)
		{
			if (CountUnitsOfGrade(array[i]) >= 2)
			{
				return array[i];
			}
		}
		for (int j = 0; j < array.Length; j++)
		{
			if (CountUnitsOfGrade(array[j]) == 1)
			{
				return array[j];
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
		int num = Mathf.RoundToInt(definition.stats.attackPower);
		int num2 = Mathf.RoundToInt(definition.stats.attackPower * 3f);
		string text = (earlyPitySummon ? "초반 보정" : "즉시 전력");
		return text + " / 공격 " + num + " / 기대딜 " + num2;
	}

	private void RegisterEarlyGoldExcitement(int amount)
	{
		if (enableEarlyRunFunPacing && !earlyRunMomentTriggered && CurrentRound <= Mathf.Max(1, earlyFunRoundLimit) && amount >= Mathf.Max(1, earlyFallbackGoldReward))
		{
			earlyRunMomentTriggered = true;
		}
	}

	private void ResolveEarlyRunFallback(int completedRound)
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		if (enableEarlyRunFunPacing && !earlyFallbackRewardGranted && !earlyRunMomentTriggered && completedRound >= Mathf.Max(1, earlyFallbackRewardRound))
		{
			earlyFallbackRewardGranted = true;
			if (TryGrantRandomUnitByGrade(earlyFallbackRewardGrade))
			{
				earlyRunMomentTriggered = true;
				this.OnBannerRequested?.Invoke("초반 보급!  다음 선택지가 열렸어요", new Color(0.4f, 0.86f, 1f), 2.4f);
				RuntimeCameraShake.Request(0.04f, 0.14f);
			}
			else if (earlyFallbackGoldReward > 0)
			{
				Gold += earlyFallbackGoldReward;
				earlyRunMomentTriggered = true;
				this.OnBannerRequested?.Invoke("초반 보급!  +" + earlyFallbackGoldReward + "G", new Color(1f, 0.8f, 0.3f), 2.2f);
			}
		}
	}

	private void AnnounceEarlyCrisisRound(int round)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		if (enableEarlyRunFunPacing && round == Mathf.Max(1, earlyCrisisRound))
		{
			this.OnBannerRequested?.Invoke("위기 라운드!  보스 전 배치와 합성을 점검하세요", new Color(1f, 0.58f, 0.24f), 2.6f);
			RuntimeCameraShake.Request(0.05f, 0.18f);
		}
	}

	private void ResolveEarlyBossPrepReward(int completedRound)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		if (enableEarlyRunFunPacing && !earlyBossPrepRewardGranted && completedRound >= Mathf.Max(1, earlyBossPrepRewardRound) && earlyBossPrepGoldReward > 0)
		{
			earlyBossPrepRewardGranted = true;
			Gold += earlyBossPrepGoldReward;
			this.OnBannerRequested?.Invoke("보스 대비 보급!  +" + earlyBossPrepGoldReward + "G", new Color(1f, 0.86f, 0.32f), 2.4f);
		}
	}

	private void TryApplyFirstBossSummonRushBonus(int round, bool bossRound)
	{
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		if (!enableFirstBossSummonRushBonus || firstBossSummonRushBonusGranted || !bossRound || round != Mathf.Max(1, firstBossSummonRushRound) || (Object)(object)boardManager == (Object)null)
		{
			return;
		}
		int num = Mathf.Max(0, earlySummonAttempts);
		int num2 = Mathf.Max(0, runMergeCount);
		bool flag = num >= Mathf.Max(1, firstBossSummonRushMinSummons);
		bool flag2 = num2 >= Mathf.Max(1, firstBossSummonRushMinMerges);
		if (!flag && !flag2)
		{
			return;
		}
		float num3 = Mathf.Clamp01((float)(num - Mathf.Max(1, firstBossSummonRushMinSummons)) / 12f);
		float num4 = Mathf.Clamp01((float)(num2 - Mathf.Max(1, firstBossSummonRushMinMerges)) / 6f);
		float num5 = Mathf.Max(num3, num4);
		float ratioBonus = Mathf.Max(0f, firstBossSummonRushAttackBonus);
		float num6 = Mathf.Lerp(Mathf.Max(0f, firstBossSummonRushBossDamageBonus), Mathf.Max(firstBossSummonRushBossDamageBonus, firstBossSummonRushMaxBossDamageBonus), num5);
		DefenderUnit[] aliveDefenders = boardManager.GetAliveDefenders();
		int num7 = 0;
		for (int i = 0; i < aliveDefenders.Length; i++)
		{
			if (!((Object)(object)aliveDefenders[i] == (Object)null))
			{
				aliveDefenders[i].AddAttackPowerBonus(ratioBonus);
				aliveDefenders[i].AddBossDamageBonus(num6);
				num7++;
			}
		}
		if (num7 > 0)
		{
			firstBossSummonRushBonusGranted = true;
			AddRunHighlightCard("R10 소환 압축", "소환 " + num + " / 합성 " + num2 + " / 보스 피해 +" + Mathf.RoundToInt(num6 * 100f) + "%");
			this.OnBannerRequested?.Invoke("R10 소환 압축  보스 피해 +" + Mathf.RoundToInt(num6 * 100f) + "%", new Color(1f, 0.74f, 0.22f), 1.4f);
		}
	}

	private void BeginEarlyRoundTelemetry(int round)
	{
		if (enableEarlyRoundTelemetry && round > 0 && round <= Mathf.Max(1, earlyTelemetryRoundLimit))
		{
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
	}

	private void CompleteEarlyRoundTelemetry(int round, bool bossRound, bool cleared)
	{
		if (enableEarlyRoundTelemetry && round > 0 && round <= Mathf.Max(1, earlyTelemetryRoundLimit))
		{
			float clearTimeSeconds = ((currentRoundStartTime > 0f) ? Mathf.Max(0f, Time.time - currentRoundStartTime) : 0f);
			EarlyRoundTelemetrySnapshot earlyRoundTelemetrySnapshot = new EarlyRoundTelemetrySnapshot
			{
				round = round,
				cleared = cleared,
				bossRound = bossRound,
				clearTimeSeconds = clearTimeSeconds,
				startGold = currentRoundStartGold,
				endGold = Gold,
				endLife = life,
				endLife01 = ((MaxLife > 0) ? Mathf.Clamp01((float)life / (float)MaxLife) : 1f),
				summons = currentRoundSummonCount,
				merges = currentRoundMergeCount,
				hadMerge = currentRoundHadMerge,
				highestMergeGrade = currentRoundHighestMergeGrade,
				bossHealthRemaining01 = (bossRound ? GetRemainingBossHealth01() : (-1f))
			};
			RecordAutomaticRunRecapMoments(earlyRoundTelemetrySnapshot);
			earlyRoundTelemetry.Add(earlyRoundTelemetrySnapshot);
			while (earlyRoundTelemetry.Count > Mathf.Max(1, earlyTelemetryRoundLimit))
			{
				earlyRoundTelemetry.RemoveAt(0);
			}
			RecordEarlyRunLogSample(earlyRoundTelemetrySnapshot);
			earlyRunTelemetrySummary = BuildEarlyRoundTelemetrySummary(earlyRoundTelemetrySnapshot);
			earlyRunTuningHint = BuildEarlyRoundTuningHint(earlyRoundTelemetrySnapshot);
			UpdateEarlyRunRecoveryRecommendation(earlyRoundTelemetrySnapshot);
			RequestEarlyRoundTuningBanner(earlyRoundTelemetrySnapshot);
			TryRecordEarlyRunTuningLog(earlyRoundTelemetrySnapshot, earlyRoundTelemetrySnapshot.round >= 10);
			Debug.Log((object)("[EarlyRunTelemetry] " + earlyRunTelemetrySummary + " / " + earlyRunLogCoverageSummary + " / 긴급지원 " + earlyRunRecoveryOfferCount + "회 / " + earlyRunTuningHint));
		}
	}

	private void RecordAutomaticRunRecapMoments(EarlyRoundTelemetrySnapshot snapshot)
	{
		if (snapshot != null && snapshot.cleared)
		{
			if (snapshot.bossRound && snapshot.clearTimeSeconds <= 1.05f)
			{
				AddRunHighlightCard("보스 1초 클리어", "ROUND " + snapshot.round + " / " + snapshot.clearTimeSeconds.ToString("0.0") + "s");
			}
			if (!lifeOneClutchRecorded && snapshot.endLife == 1)
			{
				lifeOneClutchRecorded = true;
				AddRunHighlightCard("생명력 1 역전", "ROUND " + snapshot.round + " 클리어");
			}
			if (!fateSurvivalClutchRecorded && runFateSurvivalCount > 0 && snapshot.endLife <= Mathf.Max(2, Mathf.CeilToInt((float)MaxLife * 0.18f)))
			{
				fateSurvivalClutchRecorded = true;
				AddRunHighlightCard("운명으로 생존", "ROUND " + snapshot.round + " / 생명 " + snapshot.endLife + " 남김");
			}
		}
	}

	private void RecordRoundDefeatMoment(int round, bool bossRound)
	{
		if (!runDefeatMomentRecorded)
		{
			runDefeatMomentRecorded = true;
			int num = Mathf.Max(1, RoundTargetCount);
			int num2 = Mathf.Clamp(currentRoundResolvedMonsters, 0, num);
			float num3 = ((num > 0) ? Mathf.Clamp01((float)num2 / (float)num) : 0f);
			if (round >= 8 || num3 >= 0.72f || bossRound)
			{
				string title = "R" + Mathf.Max(1, round) + " 아슬아슬 실패";
				string detail = (bossRound ? "보스 재도전 / 운명 생존 먼저" : ("처치 " + num2 + "/" + num + " / 운명 개입 후보"));
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
	}

	private void RecordEarlyRoundSummon(CharacterDefinition definition)
	{
		if (enableEarlyRoundTelemetry && CurrentRound <= Mathf.Max(1, earlyTelemetryRoundLimit))
		{
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
	}

	private void RecordFirstRarePlusRound(CharacterDefinition definition)
	{
		if (enableEarlyRoundTelemetry && firstRarePlusRound < 0 && definition != null && definition.grade >= CharacterGrade.Rare)
		{
			firstRarePlusRound = ResolveEarlyMomentRound();
			UpdateEarlyRunLogCoverageSummary();
		}
	}

	private int ResolveEarlyMomentRound()
	{
		int num = ((CurrentRound > 0) ? CurrentRound : GetSummonRateRound());
		return Mathf.Clamp(num, 1, Mathf.Max(1, earlyTelemetryRoundLimit));
	}

	private static string FormatEarlyMomentRound(int round)
	{
		return (round > 0) ? ("R" + round) : "-";
	}

	private void RecordEarlyRunLogSample(EarlyRoundTelemetrySnapshot snapshot)
	{
		if (snapshot != null && snapshot.round > 0 && snapshot.round <= Mathf.Max(1, earlyTelemetryRoundLimit))
		{
			if (snapshot.round == 10 && snapshot.bossRound)
			{
				earlyRunR10BossHealthRemaining01 = Mathf.Clamp01(snapshot.bossHealthRemaining01);
				runR10BossHealthRemaining01 = earlyRunR10BossHealthRemaining01;
			}
			UpdateEarlyRunLogCoverageSummary();
		}
	}

	private void TryRecordEarlyRunTuningLog(EarlyRoundTelemetrySnapshot snapshot, bool reachedRound10)
	{
		if (!enableEarlyRoundTelemetry || earlyRunTuningLogRecorded)
		{
			return;
		}
		int num = ResolveEarlyRunTuningReachedRound(snapshot);
		bool flag = gameOverRaised && num > 0 && num < 10;
		bool flag2 = reachedRound10 || num >= 10;
		if (flag || flag2)
		{
			EnsureEarlyRunTuningLogStoreLoaded();
			float r10BossHealthRemaining = -1f;
			if (snapshot != null && snapshot.round == 10 && snapshot.bossRound)
			{
				r10BossHealthRemaining = Mathf.Clamp01(snapshot.bossHealthRemaining01);
			}
			else if (flag2 && CurrentRound == 10 && IsBossRound)
			{
				r10BossHealthRemaining = Mathf.Clamp01(GetRemainingBossHealth01());
			}
			else if (runR10BossHealthRemaining01 >= 0f)
			{
				r10BossHealthRemaining = Mathf.Clamp01(runR10BossHealthRemaining01);
			}
			EarlyRunTuningLogEntry item = new EarlyRunTuningLogEntry
			{
				ticksUtc = DateTime.UtcNow.Ticks,
				reachedRound = Mathf.Clamp(num, 1, 10),
				reachedRound10 = flag2,
				clearedRound10 = (flag2 && snapshot != null && snapshot.round >= 10 && snapshot.cleared),
				firstRarePlusRound = firstRarePlusRound,
				firstMergeRound = firstMergeRound,
				insuranceOffered = (runInsuranceOffered || badLuckInsuranceOffered),
				insuranceClaimed = runInsuranceClaimed,
				r3BoosterOffered = runR3BoosterOffered,
				r3BoosterPurchased = runR3BoosterPurchased,
				recoveryShopOffered = runRecoveryShopOffered,
				recoveryShopPurchased = runRecoveryShopPurchased,
				fateContractUsed = (runFateContractCount > 0),
				fateInterventionUsed = (runFateInterventionCount > 0),
				fateDebt = fateDebt,
				r10BossHealthRemaining01 = r10BossHealthRemaining,
				endLife = (snapshot?.endLife ?? life),
				endGold = (snapshot?.endGold ?? Gold),
				boardUnits = BoardUnitCount,
				bossKills = totalBossKills,
				runScore = CalculateRunPerformanceScore(),
				recommendedBuildName = RecommendedBuildName
			};
			earlyRunTuningLogStore.entries.Add(item);
			while (earlyRunTuningLogStore.entries.Count > 60)
			{
				earlyRunTuningLogStore.entries.RemoveAt(0);
			}
			earlyRunTuningLogRecorded = true;
			SaveEarlyRunTuningLogStore();
			UpdateEarlyRunLogCoverageSummary();
			Debug.Log((object)("[EarlyRunTelemetry] 판 단위 표본 저장 " + earlyRunLogCoverageSummary));
		}
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
		string text = PlayerPrefs.GetString("DefenseGame.EarlyRunTuningLog.v1", string.Empty);
		if (!string.IsNullOrWhiteSpace(text))
		{
			try
			{
				earlyRunTuningLogStore = JsonUtility.FromJson<EarlyRunTuningLogStore>(text);
			}
			catch (Exception ex)
			{
				Debug.LogWarning((object)("[EarlyRunTelemetry] 누적 로그 로드 실패: " + ex.Message));
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
		while (earlyRunTuningLogStore.entries.Count > 60)
		{
			earlyRunTuningLogStore.entries.RemoveAt(0);
		}
	}

	private void SaveEarlyRunTuningLogStore()
	{
		EnsureEarlyRunTuningLogStoreLoaded();
		PlayerPrefs.SetString("DefenseGame.EarlyRunTuningLog.v1", JsonUtility.ToJson((object)earlyRunTuningLogStore));
		PlayerPrefs.Save();
	}

	private void UpdateEarlyRunLogCoverageSummary()
	{
		EnsureEarlyRunTuningLogStoreLoaded();
		int target = Mathf.Max(1, earlyTelemetryTargetSampleCount);
		List<EarlyRunTuningLogEntry> entries = earlyRunTuningLogStore.entries;
		int num = entries?.Count ?? 0;
		if (num <= 0)
		{
			string text = ((earlyRunR10BossHealthRemaining01 >= 0f) ? (Mathf.RoundToInt(earlyRunR10BossHealthRemaining01 * 100f) + "%") : "-");
			earlyRunLogCoverageSummary = "R1~R10 로그 0/" + target + " / 첫R+ " + FormatEarlyMomentRound(firstRarePlusRound) + " / 첫합 " + FormatEarlyMomentRound(firstMergeRound) + " / 보험 0% / R3부스터 " + earlyRunR3BoosterPurchaseCount + "/" + earlyRunR3BoosterOfferCount + " " + FormatRate(earlyRunR3BoosterPurchaseCount, earlyRunR3BoosterOfferCount) + " / 긴급지원 " + earlyRunRecoveryShopPurchaseCount + "/" + earlyRunRecoveryShopOfferCount + " " + FormatRate(earlyRunRecoveryShopPurchaseCount, earlyRunRecoveryShopOfferCount) + " / 운명개입 " + runFateInterventionCount + "회 / R10보스HP " + text;
			return;
		}
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		int num9 = 0;
		int num10 = 0;
		int num11 = 0;
		int num12 = 0;
		int num13 = 0;
		int num14 = 0;
		float num15 = 0f;
		for (int i = 0; i < num; i++)
		{
			EarlyRunTuningLogEntry earlyRunTuningLogEntry = entries[i];
			if (earlyRunTuningLogEntry != null)
			{
				if (earlyRunTuningLogEntry.firstRarePlusRound > 0)
				{
					num2++;
					num3 += earlyRunTuningLogEntry.firstRarePlusRound;
				}
				if (earlyRunTuningLogEntry.firstMergeRound > 0)
				{
					num4++;
					num5 += earlyRunTuningLogEntry.firstMergeRound;
				}
				if (earlyRunTuningLogEntry.insuranceOffered)
				{
					num6++;
				}
				if (earlyRunTuningLogEntry.r3BoosterOffered)
				{
					num7++;
				}
				if (earlyRunTuningLogEntry.r3BoosterPurchased)
				{
					num8++;
				}
				if (earlyRunTuningLogEntry.recoveryShopOffered)
				{
					num9++;
				}
				if (earlyRunTuningLogEntry.recoveryShopPurchased)
				{
					num10++;
				}
				if (earlyRunTuningLogEntry.fateContractUsed)
				{
					num11++;
				}
				if (earlyRunTuningLogEntry.fateInterventionUsed)
				{
					num12++;
				}
				num13 += Mathf.Max(0, earlyRunTuningLogEntry.fateDebt);
				if (earlyRunTuningLogEntry.r10BossHealthRemaining01 >= 0f)
				{
					num14++;
					num15 += Mathf.Clamp01(earlyRunTuningLogEntry.r10BossHealthRemaining01);
				}
			}
		}
		earlyRunLogCoverageSummary = "R1~R10 로그 " + FormatSampleProgress(num, target) + " / 첫R+ " + FormatAverageRound(num3, num2) + " / 첫합 " + FormatAverageRound(num5, num4) + " / 보험 " + num6 + "/" + num + " " + FormatRate(num6, num) + " / R3부스터 " + num8 + "/" + num7 + " " + FormatRate(num8, num7) + " / 긴급지원 " + num10 + "/" + num9 + " " + FormatRate(num10, num9) + " / 운명계약 " + num11 + "/" + num + " " + FormatRate(num11, num) + " / 운명개입 " + num12 + "/" + num + " " + FormatRate(num12, num) + " / 빚 " + FormatAverageInt(num13, num) + " / R10보스HP " + FormatAveragePercent(num15, num14);
	}

	private static string FormatSampleProgress(int count, int target)
	{
		return (count >= target) ? (target + "/" + target + "+") : (count + "/" + target);
	}

	private static string FormatAverageRound(int roundSum, int count)
	{
		if (count <= 0)
		{
			return "-";
		}
		return "평균R" + ((float)roundSum / (float)count).ToString("0.0");
	}

	private static string FormatAveragePercent(float sum, int count)
	{
		if (count <= 0)
		{
			return "-";
		}
		return "평균" + Mathf.RoundToInt(Mathf.Clamp01(sum / (float)count) * 100f) + "%";
	}

	private static string FormatAverageInt(int sum, int count)
	{
		if (count <= 0)
		{
			return "-";
		}
		return "평균" + Mathf.RoundToInt((float)sum / (float)count);
	}

	private static string FormatRate(int value, int total)
	{
		if (total <= 0)
		{
			return "0%";
		}
		return Mathf.RoundToInt(Mathf.Clamp01((float)value / (float)total) * 100f) + "%";
	}

	private int GetEarlyRunLogSampleCount()
	{
		EnsureEarlyRunTuningLogStoreLoaded();
		return (earlyRunTuningLogStore != null && earlyRunTuningLogStore.entries != null) ? earlyRunTuningLogStore.entries.Count : 0;
	}

	private string BuildEarlyRunActionSummary()
	{
		int num = Mathf.Max(1, earlyTelemetryTargetSampleCount);
		int earlyRunLogSampleCount = GetEarlyRunLogSampleCount();
		if (earlyRunLogSampleCount < num)
		{
			return "초반 검증: R1~R10 로그 " + earlyRunLogSampleCount + "/" + num + "회";
		}
		if (earlyRunR3BoosterOfferCount > 0 && earlyRunR3BoosterPurchaseCount <= 0)
		{
			return "초반 검증: R3 선택지 매력 확인";
		}
		if (earlyRunRecoveryShopOfferCount > 0 && earlyRunRecoveryShopPurchaseCount <= 0)
		{
			return "초반 검증: 긴급지원 선택률 확인";
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
		int num = Mathf.Max(1, earlyTelemetryTargetSampleCount);
		List<EarlyRunTuningLogEntry> list = ((earlyRunTuningLogStore != null) ? earlyRunTuningLogStore.entries : null);
		int num2 = list?.Count ?? 0;
		if (num2 <= 0)
		{
			return "실측 루프 0/" + num + "회: 첫 Rare+, 첫 합성, 상점, 보스 HP 기록 대기";
		}
		int num3 = Mathf.Max(0, num2 - num);
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		int num9 = 0;
		int num10 = 0;
		int num11 = 0;
		int num12 = 0;
		int num13 = 0;
		int num14 = 0;
		int num15 = 0;
		int num16 = 0;
		float num17 = 0f;
		for (int i = num3; i < num2; i++)
		{
			EarlyRunTuningLogEntry earlyRunTuningLogEntry = list[i];
			if (earlyRunTuningLogEntry != null)
			{
				num4++;
				if (earlyRunTuningLogEntry.firstRarePlusRound > 0)
				{
					num5++;
					num6 += earlyRunTuningLogEntry.firstRarePlusRound;
				}
				if (earlyRunTuningLogEntry.firstMergeRound > 0)
				{
					num7++;
					num8 += earlyRunTuningLogEntry.firstMergeRound;
				}
				if (earlyRunTuningLogEntry.reachedRound10)
				{
					num9++;
				}
				if (earlyRunTuningLogEntry.clearedRound10)
				{
					num10++;
				}
				if (earlyRunTuningLogEntry.r3BoosterOffered)
				{
					num11++;
				}
				if (earlyRunTuningLogEntry.r3BoosterPurchased)
				{
					num12++;
				}
				if (earlyRunTuningLogEntry.recoveryShopOffered)
				{
					num13++;
				}
				if (earlyRunTuningLogEntry.recoveryShopPurchased)
				{
					num14++;
				}
				if (earlyRunTuningLogEntry.fateContractUsed || earlyRunTuningLogEntry.fateInterventionUsed)
				{
					num15++;
				}
				if (earlyRunTuningLogEntry.r10BossHealthRemaining01 >= 0f)
				{
					num16++;
					num17 += Mathf.Clamp01(earlyRunTuningLogEntry.r10BossHealthRemaining01);
				}
			}
		}
		if (num4 < num)
		{
			return "실측 루프 " + num4 + "/" + num + "회: R1~R10 반복 필요";
		}
		List<string> list2 = new List<string>();
		float num18 = ((num5 > 0) ? ((float)num6 / (float)num5) : 99f);
		float num19 = ((num7 > 0) ? ((float)num8 / (float)num7) : 99f);
		float num20 = ((num4 > 0) ? ((float)num9 / (float)num4) : 0f);
		float num21 = ((num4 > 0) ? ((float)num10 / (float)num4) : 0f);
		float num22 = ((num11 > 0) ? ((float)num12 / (float)num11) : 0f);
		float num23 = ((num13 > 0) ? ((float)num14 / (float)num13) : 0f);
		float num24 = ((num4 > 0) ? ((float)num15 / (float)num4) : 0f);
		float num25 = ((num16 > 0) ? (num17 / (float)num16) : 0f);
		if (num18 > 2f)
		{
			list2.Add("Rare+가 늦음");
		}
		if (num19 > 3f)
		{
			list2.Add("첫 합성이 늦음");
		}
		if (num20 < 0.75f)
		{
			list2.Add("R10 도달률 낮음");
		}
		if (num21 < 0.45f)
		{
			list2.Add("R10 클리어율 낮음");
		}
		if (num25 >= highBossHealthWarningRatio)
		{
			list2.Add("보스 HP 과다");
		}
		if (num11 > 0 && num22 < 0.35f)
		{
			list2.Add("R3 상점 매력 부족");
		}
		if (num13 > 0 && num23 < 0.25f)
		{
			list2.Add("긴급 지원 선택률 낮음");
		}
		if (num24 < 0.3f)
		{
			list2.Add("운명 버튼 노출 부족");
		}
		if (list2.Count <= 0)
		{
			list2.Add("초반 손맛 유지");
		}
		return "실측 판정 " + num4 + "/" + num + ": " + string.Join(" / ", list2);
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
			if (grade > currentRoundHighestMergeGrade)
			{
				currentRoundHighestMergeGrade = grade;
			}
		}
		else
		{
			pendingRoundMergeCount++;
			pendingRoundHadMerge = true;
			if (grade > pendingRoundHighestMergeGrade)
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
		string text = (snapshot.hadMerge ? CharacterGradeUtility.GetDisplayName(snapshot.highestMergeGrade) : "없음");
		string text2 = "R" + snapshot.round + " " + snapshot.clearTimeSeconds.ToString("0") + "s / G" + snapshot.endGold + " / HP " + snapshot.endLife + "/" + MaxLife + " / 소환 " + snapshot.summons + " / 합성 " + text + " / 첫R+ " + FormatEarlyMomentRound(firstRarePlusRound) + " / 첫합 " + FormatEarlyMomentRound(firstMergeRound);
		if (snapshot.bossRound)
		{
			text2 = text2 + " / 보스HP " + Mathf.RoundToInt(Mathf.Clamp01(snapshot.bossHealthRemaining01) * 100f) + "%";
		}
		return text2;
	}

	private int CalculateRunPerformanceScore()
	{
		float num = ((MaxLife > 0) ? Mathf.Clamp01((float)Life / (float)MaxLife) : 0f);
		int num2 = Mathf.RoundToInt((float)CurrentRound * 12f);
		num2 += Mathf.RoundToInt(Mathf.Clamp(TotalDamageDealt / 650f, 0f, 130f));
		num2 += Mathf.Clamp(bestSynergyCount * 18, 0, 90);
		num2 += Mathf.Clamp(bestKillCombo * 3, 0, 75);
		num2 += Mathf.Clamp(criticalHitCount, 0, 50);
		num2 += Mathf.RoundToInt(num * 60f);
		if (earlyRunRecoveryRecommended)
		{
			num2 -= 20;
		}
		return Mathf.Max(0, num2);
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
		return (score >= 90) ? "B" : "C";
	}

	private string BuildRunResultRecapSummary()
	{
		string text = CurrentTileContributionSummary;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "타일 기여 없음";
		}
		return "딜러 TOP " + DamageLeaderboardSummary + "  |  시너지 " + BestSynergySummary + "\n타일 " + text + "  |  보스 " + BossPressureSummary + "\n로그 " + earlyRunLogCoverageSummary;
	}

	private string BuildRunHighlightCardsSummary()
	{
		List<string> list = new List<string>(3);
		int num = runHighlightCards.Count - 1;
		while (num >= 0 && list.Count < 3)
		{
			if (!string.IsNullOrWhiteSpace(runHighlightCards[num]))
			{
				list.Add(runHighlightCards[num]);
			}
			num--;
		}
		if (list.Count < 3 && topDamageHeroDamage > 0f)
		{
			list.Add("MVP 딜러 | " + topDamageHeroName + " " + Mathf.RoundToInt(topDamageHeroDamage).ToString("N0") + "딜");
		}
		if (list.Count < 3)
		{
			list.Add("보스 목표 | " + BossPressureSummary);
		}
		if (list.Count < 3)
		{
			list.Add("다음 빌드 | " + RecommendedBuildName);
		}
		while (list.Count < 3)
		{
			list.Add("초반 목표 | 같은 태그 2개와 레어 이상 딜러 확보");
		}
		return "이번 판의 대박 순간 3개\nCARD 1  " + list[0] + "\nCARD 2  " + list[1] + "\nCARD 3  " + list[2];
	}

	private List<string> CollectRunResultCards(int targetCount)
	{
		int num = Mathf.Max(1, targetCount);
		List<string> list = new List<string>(num);
		int num2 = runHighlightCards.Count - 1;
		while (num2 >= 0 && list.Count < num)
		{
			if (!string.IsNullOrWhiteSpace(runHighlightCards[num2]))
			{
				list.Add(runHighlightCards[num2]);
			}
			num2--;
		}
		if (list.Count < num && topDamageHeroDamage > 0f)
		{
			list.Add("MVP 딜러 | " + topDamageHeroName + " " + Mathf.RoundToInt(topDamageHeroDamage).ToString("N0") + "딜");
		}
		if (list.Count < num)
		{
			list.Add("보스 목표 | " + BossPressureSummary);
		}
		if (list.Count < num)
		{
			list.Add("다음 빌드 | " + RecommendedBuildName);
		}
		while (list.Count < num)
		{
			list.Add("초반 목표 | 같은 태그 2개와 레어 이상 딜러 확보");
		}
		return list;
	}

	private string BuildRunResultFocusSummary()
	{
		List<string> list = CollectRunResultCards(3);
		return "이번 판 사건 3개\nCARD 1  " + list[0] + "\nCARD 2  " + list[1] + "\nCARD 3  " + list[2];
	}

	private string BuildLatestRunMomentSummary()
	{
		for (int num = runHighlightCards.Count - 1; num >= 0; num--)
		{
			if (!string.IsNullOrWhiteSpace(runHighlightCards[num]))
			{
				return "사건 " + CompactRunResultText(runHighlightCards[num], 28);
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
		int upgradeableCardCount = GetUpgradeableCardCount();
		string text = CompactRunResultText(BuildRunNextGoalHeadline(), 34);
		string text2 = ((upgradeableCardCount > 0) ? ("강화 " + upgradeableCardCount + "명 가능") : ((earnedGrowthCurrency > 0) ? ("다이아 +" + earnedGrowthCurrency + " 성장") : "카드 조각 목표"));
		string text3 = (earlyRunRecoveryRecommended ? "복구 상점" : ((RoundsUntilNextBoss > 0 && RoundsUntilNextBoss <= 2) ? "보스 대비" : "상점 보충"));
		string text4 = CompactRunResultText(FateResultSummary, 24);
		string text5 = "공유 " + BuildRunShareCode();
		return text + "  |  " + text5 + "\n" + text2 + "  |  " + text3 + "  |  " + text4;
	}

	private string BuildFateInterventionSummary()
	{
		if (!enableFateIntervention)
		{
			return "운명 비활성";
		}
		string text = ((fateGradeLockSummonsRemaining > 0) ? (" 잠금 " + CharacterGradeUtility.GetDisplayName(fateGradeLockMinimum) + "x" + fateGradeLockSummonsRemaining) : ((fateNormalBanSummonsRemaining > 0) ? (" 일반금지x" + fateNormalBanSummonsRemaining) : (fateForceNextShop ? " 상점확정" : string.Empty)));
		return "운명 " + fateGauge + "/" + Mathf.Max(1, maxFateGauge) + "  빚 " + fateDebt + text;
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
		int num = Mathf.Min(Mathf.Min(Mathf.Max(1, fateShopRerollGaugeCost), Mathf.Max(1, fateNormalBanGaugeCost)), Mathf.Max(1, fateForceShopGaugeCost));
		return (fateGauge >= num) ? "운명 개입 준비" : ("운명 " + fateGauge + "/" + num);
	}

	private bool CanSpendFateGauge(int cost)
	{
		return enableFateIntervention && !useOneShotFateCard && fateGauge >= Mathf.Max(0, cost);
	}

	private bool IsFateSurvivalCrisisActive()
	{
		if (useOneShotFateCard)
		{
			return CanOpenFateCard && Life > 0 && Life <= 3;
		}
		if (!enableFateIntervention || !CanUseFateSurvival || MaxLife <= 0)
		{
			return false;
		}
		int num = (IsRoundRunning ? CurrentRound : (CurrentRound + 1));
		if (num < 5 || num > 8)
		{
			return false;
		}
		return (float)Life / (float)MaxLife <= 0.5f;
	}

	private string BuildReadableFateInterventionSummary()
	{
		if (!enableFateIntervention)
		{
			return "운명 비활성";
		}
		if (useOneShotFateCard)
		{
			if (fateCardUsed)
			{
				return "마지막 계약 완료: " + fateCardLastTitle + " | 빚 " + fateCardLastDebt;
			}
			return CanUseFateCard ? "마지막 계약 선택 중: 18장 덱 중 3장" : (CanOpenFateCard ? "마지막 계약 준비: 운명카드를 눌러 개방" : "마지막 계약 1/1: 전투 중 대기");
		}
		string text = ((fateGradeLockSummonsRemaining > 0) ? (" / Rare+ " + fateGradeLockSummonsRemaining) : ((fateNormalBanSummonsRemaining > 0) ? (" / 일반 제외 " + fateNormalBanSummonsRemaining) : (fateForceNextShop ? " / 다음 상점 확정" : string.Empty)));
		return "운명 " + fateGauge + "/" + Mathf.Max(1, maxFateGauge) + " | 빚 " + fateDebt + "/" + Mathf.Max(1, maxFateDebt) + text;
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
		int num = Mathf.Min(Mathf.Min(Mathf.Max(1, fateNormalBanGaugeCost), Mathf.Max(1, fateForceShopGaugeCost)), Mathf.Max(1, fateSurvivalGaugeCost));
		return (fateGauge >= num) ? "운명 개입 준비" : ("운명 " + fateGauge + "/" + num);
	}

	private string BuildReadableFateHudSummary()
	{
		if (!enableFateIntervention)
		{
			return "운명 비활성";
		}
		if (useOneShotFateCard)
		{
			if (fateCardUsed)
			{
				return "마지막 계약 사용 완료: " + fateCardLastTitle;
			}
			return CanUseFateCard ? "마지막 계약 선택 중 · 전투 0.1배" : (CanOpenFateCard ? "마지막 계약 준비 · 눌러서 개방" : "마지막 계약 대기 · 전투 중 개방");
		}
		string text = ((fateGradeLockSummonsRemaining > 0) ? (" / Rare+ x" + fateGradeLockSummonsRemaining) : ((fateNormalBanSummonsRemaining > 0) ? (" / 일반 제외 x" + fateNormalBanSummonsRemaining) : (fateForceNextShop ? " / 상점 확정" : string.Empty)));
		return "운명 " + fateGauge + "/" + Mathf.Max(1, maxFateGauge) + " | 빚 " + fateDebt + "/" + Mathf.Max(1, maxFateDebt) + text;
	}

	private string BuildReadableFateCostBenefitSummary()
	{
		if (!enableFateIntervention)
		{
			return "운명 비활성";
		}
		if (useOneShotFateCard)
		{
			if (!fateCardUsed)
			{
				if (CanUseFateCard)
				{
					return "선택 중 전투 0.1배: " + BuildFateCardChoiceSummary() + " | 선택 후 UI가 내려가고 대가가 남습니다";
				}
				if (CanOpenFateCard)
				{
					return "운명카드 준비: 버튼을 누르면 3장이 공개되고 선택할 때까지 전투가 0.1배로 느려집니다";
				}
				return "전투 중 몬스터가 등장하면 작은 운명카드 버튼이 올라옵니다";
			}
			return "이득: " + fateCardLastDetail + " | 대가: 빚 +" + fateCardLastDebt + ", 다음 보스HP x" + FateDebtBossHealthMultiplier.ToString("0.00");
		}
		if (runFateInterventionCount <= 0)
		{
			return "이득: 위기 때 판 살리기 | 대가: 상점가/보스HP 소폭 증가";
		}
		List<string> list = new List<string>();
		if (runFateSurvivalCount > 0)
		{
			list.Add("생존 " + runFateSurvivalCount);
		}
		if (runFateShopRerollCount > 0)
		{
			list.Add("리롤 " + runFateShopRerollCount);
		}
		if (runFateGradeLockCount > 0)
		{
			list.Add("Rare+ " + runFateGradeLockCount);
		}
		if (runFateNormalBanCount > 0)
		{
			list.Add("일반 제외 " + runFateNormalBanCount);
		}
		if (runFateForcedShopCount > 0)
		{
			list.Add("상점 확정 " + runFateForcedShopCount);
		}
		if (runFateContractCount > 0)
		{
			list.Add("계약 " + runFateContractCount);
		}
		if (list.Count <= 0)
		{
			list.Add("개입 " + runFateInterventionCount);
		}
		List<string> list2 = new List<string>();
		if (runFateDebtAdded > 0)
		{
			list2.Add("빚 +" + runFateDebtAdded);
		}
		if (runFateDebtRepaid > 0)
		{
			list2.Add("상환 -" + runFateDebtRepaid);
		}
		if (runFateShopCostPenaltyGold > 0)
		{
			list2.Add("상점 +" + runFateShopCostPenaltyGold + "G");
		}
		int num = Mathf.RoundToInt(Mathf.Clamp01((float)runPeakFateDebt / (float)Mathf.Max(1, maxFateDebt)) * maxFateDebtBossHealthBonus * 100f);
		if (num > 0)
		{
			list2.Add("보스HP +" + num + "%");
		}
		if (list2.Count <= 0)
		{
			list2.Add("대가 없음");
		}
		return "이득: " + string.Join(" / ", list) + " | 대가: " + string.Join(", ", list2);
	}

	private string BuildFateHudSummary()
	{
		if (!enableFateIntervention)
		{
			return "운명 비활성";
		}
		if (useOneShotFateCard)
		{
			if (fateCardUsed)
			{
				return "계약 사용: " + fateCardLastTitle;
			}
			return CanUseFateCard ? "마지막 계약 선택 중 · 3/18" : (CanOpenFateCard ? "운명카드 준비" : "마지막 계약 대기");
		}
		string text = ((fateGradeLockSummonsRemaining > 0) ? (" / Rare+ lock x" + fateGradeLockSummonsRemaining) : ((fateNormalBanSummonsRemaining > 0) ? (" / No Normal x" + fateNormalBanSummonsRemaining) : (fateForceNextShop ? " / Shop reserved" : string.Empty)));
		return "운명 " + fateGauge + "/" + Mathf.Max(1, maxFateGauge) + " | 빚 " + fateDebt + "/" + Mathf.Max(1, maxFateDebt) + text;
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
		if (useOneShotFateCard)
		{
			if (!fateCardUsed)
			{
				if (CanUseFateCard)
				{
					return "마지막 계약 선택 중 | 18장 덱 중 이번 3장: " + BuildFateCardChoiceSummary();
				}
				if (CanOpenFateCard)
				{
					return "마지막 계약 미사용 | 버튼을 누르면 3장 공개";
				}
				return "마지막 계약 미사용 | 전투 중 몬스터 등장 시 카드 버튼 표시";
			}
			return "이득: " + fateCardLastDetail + " | 대가: 빚 +" + fateCardLastDebt + ", 보스 HP +" + Mathf.RoundToInt((FateDebtBossHealthMultiplier - 1f) * 100f) + "%";
		}
		if (runFateInterventionCount <= 0)
		{
			return "운명 미사용 | 다음 판 목표: 상점 강제 등장권 써보기";
		}
		List<string> list = new List<string>();
		if (runFateShopRerollCount > 0)
		{
			list.Add("리롤 " + runFateShopRerollCount + "회");
		}
		if (runFateGradeLockCount > 0)
		{
			list.Add("레어 보정 " + runFateGradeLockCount + "회");
		}
		if (runFateNormalBanCount > 0)
		{
			list.Add("일반 금지 " + runFateNormalBanCount + "회");
		}
		if (runFateForcedShopCount > 0)
		{
			list.Add("상점 강제 " + runFateForcedShopCount + "회");
		}
		if (runFateContractCount > 0)
		{
			list.Add("계약 " + runFateContractCount + "회");
		}
		if (list.Count <= 0)
		{
			list.Add("운명 개입 " + runFateInterventionCount + "회");
		}
		List<string> list2 = new List<string>();
		if (runFateDebtAdded > 0)
		{
			list2.Add("빚 +" + runFateDebtAdded);
		}
		if (runFateDebtRepaid > 0)
		{
			list2.Add("상환 -" + runFateDebtRepaid);
		}
		if (runFateShopCostPenaltyGold > 0)
		{
			list2.Add("상점가 +" + runFateShopCostPenaltyGold + "G");
		}
		int num = Mathf.RoundToInt(Mathf.Clamp01((float)runPeakFateDebt / (float)Mathf.Max(1, maxFateDebt)) * maxFateDebtBossHealthBonus * 100f);
		if (num > 0)
		{
			list2.Add("보스 HP +" + num + "%");
		}
		if (list2.Count <= 0)
		{
			list2.Add("대가 없음");
		}
		return "이득: " + string.Join(" / ", list) + " | 대가: " + string.Join(", ", list2);
	}

	private string BuildSeasonReplayDigestSummary()
	{
		string text = CompactRunResultText(BuildLatestRunMomentSummary(), 24);
		return "협동 보스 " + RunBossScore.ToString("N0") + " / MVP " + RunMvpName + " | 리플레이 " + BuildRunShareCode() + " " + text;
	}

	private string BuildRunShareCode()
	{
		int num = 17;
		num = num * 31 + CurrentRound;
		num = num * 31 + RunPerformanceScore;
		num = num * 31 + totalBossKills;
		num = num * 31 + bestKillCombo;
		num = num * 31 + runFateInterventionCount;
		num = num * 31 + fateDebt;
		if (runClipEvents.Count > 0)
		{
			string text = runClipEvents[runClipEvents.Count - 1];
			for (int i = 0; i < text.Length; i++)
			{
				num = num * 31 + text[i];
			}
		}
		return "#" + Mathf.Abs(num % 100000).ToString("D5");
	}

	private static string CompactRunResultText(string value, int maxChars)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "기록 없음";
		}
		string text = value.Replace("\r", " ").Replace("\n", " ").Replace(" | ", " ")
			.Trim();
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		if (text.Length <= maxChars)
		{
			return text;
		}
		return text[..Mathf.Max(1, maxChars - 3)] + "...";
	}

	private void AddRunHighlightCard(string title, string detail)
	{
		string text = (string.IsNullOrWhiteSpace(title) ? "대박 순간" : title.Trim());
		string text2 = (string.IsNullOrWhiteSpace(detail) ? "기록 없음" : detail.Trim());
		string text3 = text + " | " + text2;
		if (!runHighlightCards.Contains(text3))
		{
			runHighlightCards.Add(text3);
			RecordRunClipEvent(text3);
			while (runHighlightCards.Count > 8)
			{
				runHighlightCards.RemoveAt(0);
			}
		}
	}

	private void RecordRunClipEvent(string entry)
	{
		if (!string.IsNullOrWhiteSpace(entry) && !runClipEvents.Contains(entry))
		{
			runClipEvents.Add(entry);
			while (runClipEvents.Count > 6)
			{
				runClipEvents.RemoveAt(0);
			}
		}
	}

	private static string TrimHighlightTitle(string title)
	{
		return string.IsNullOrWhiteSpace(title) ? "대박 순간" : title.Trim().TrimEnd(new char[2] { '!', ' ' });
	}

	private string BuildRunNextActionSummary()
	{
		int upgradeableCardCount = GetUpgradeableCardCount();
		string text = ((upgradeableCardCount > 0) ? ("도감 강화: 강화 가능 " + upgradeableCardCount + "명 / 바로 성장 확인") : ((earnedGrowthCurrency > 0) ? ("도감 강화 보상: 다이아 +" + earnedGrowthCurrency + " / 주력 카드 성장 확인") : "도감 강화 보상: 주력 딜러 카드 조각 목표 확인"));
		string text2 = (earlyRunRecoveryRecommended ? ("상점 목표: " + earlyRunRecoveryCause + " / " + earlyRunRecoveryReason) : "상점 목표: 부족한 등급과 카드 조각 보충");
		string text3 = ((RoundsUntilNextBoss > 0 && RoundsUntilNextBoss <= 2) ? ("다음 보스 대비: " + ComposeBuildGoalGuideSummary()) : ((totalBossSkillCasts > 0 && totalBossTileDamage < Mathf.Max(1f, totalBossSkillDamage * 0.35f)) ? "다음 보스 대비: 보스 타일과 군중 제어 우선" : ((!(topDamageHeroDamage <= 0f)) ? ("다음 보스 대비: " + RecommendedDeckSummary) : "다음 보스 대비: 핵심 딜러 먼저 확보")));
		string text4 = "다음 판 목표: " + RecommendedBuildName;
		return text4 + "\n" + text + "\n" + text2 + "\n" + text3 + "\n" + BuildEarlyRunActionSummary() + "\n" + DailyFortuneSummary;
	}

	private string BuildRunNextGoalHeadline()
	{
		int upgradeableCardCount = GetUpgradeableCardCount();
		string text = ((upgradeableCardCount > 0) ? ("강화 가능 " + upgradeableCardCount + "명") : ((earnedGrowthCurrency > 0) ? "도감 강화 가능" : "도감 카드 조각 목표"));
		string text2 = ((totalBossKills <= 0 && CurrentRound >= 4) ? "보스 사냥덱" : RecommendedBuildName);
		if (BadLuckInsuranceAvailable)
		{
			return "다음 판 목표: " + text2 + " / 보험 선택으로 초반 복구";
		}
		int num = Mathf.Max(1, earlyTelemetryTargetSampleCount);
		int earlyRunLogSampleCount = GetEarlyRunLogSampleCount();
		if (earlyRunLogSampleCount < num)
		{
			return "다음 판 목표: R1~R10 로그 " + earlyRunLogSampleCount + "/" + num;
		}
		return "다음 판 목표: " + text2 + " / " + text;
	}

	private int GetUpgradeableCardCount()
	{
		return ((Object)(object)OutgameProgressionSystem.Active != (Object)null) ? OutgameProgressionSystem.Active.CountUpgradeableCards() : 0;
	}

	private int CalculateRunBossScore()
	{
		int num = totalBossKills * 520;
		num += Mathf.RoundToInt(Mathf.Clamp(totalBossTileDamage, 0f, 6000f) * 0.42f);
		num += Mathf.RoundToInt(Mathf.Clamp(TotalDamageDealt, 0f, 40000f) * 0.035f);
		num += Mathf.Clamp(bestSynergyCount * 35, 0, 180);
		num += Mathf.Clamp(bestKillCombo * 4, 0, 160);
		return Mathf.Max(0, num);
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
		string ultimateMergeActionStatus = GetUltimateMergeActionStatus();
		if (!string.IsNullOrWhiteSpace(ultimateMergeActionStatus) && ultimateMergeActionStatus != "초월 레시피 없음")
		{
			return "초월 목표: " + ultimateMergeActionStatus;
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
		int num = (total ? totalBossSkillCasts : currentRoundBossSkillCasts);
		if (num <= 0)
		{
			return "압박 없음";
		}
		string text = (total ? lastBossSkill : currentRoundLastBossSkill);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "보스 스킬";
		}
		int num2 = (total ? totalBossAffectedTargets : currentRoundBossAffectedTargets);
		int num3 = (total ? totalBossGoldDrained : currentRoundBossGoldDrained);
		int num4 = (total ? totalBossManaBurnTargets : currentRoundBossManaBurnTargets);
		int num5 = (total ? totalBossExecutions : currentRoundBossExecutions);
		int num6 = (total ? totalBossFortifyCount : currentRoundBossFortifyCount);
		int num7 = (total ? totalBossRallyTargets : currentRoundBossRallyTargets);
		float num8 = (total ? totalBossSkillDamage : currentRoundBossSkillDamage);
		float num9 = (total ? totalBossTileDamage : currentRoundBossTileDamage);
		string text2 = text + " " + num + "회";
		if (num2 > 0)
		{
			text2 = text2 + " / 영향 " + num2;
		}
		if (num8 > 0f)
		{
			text2 = text2 + " / 피해 " + Mathf.RoundToInt(num8).ToString("N0");
		}
		if (num3 > 0)
		{
			text2 = text2 + " / 골드 -" + num3;
		}
		if (num4 > 0)
		{
			text2 = text2 + " / 마나 " + num4;
		}
		if (num5 > 0)
		{
			text2 = text2 + " / 즉사 " + num5;
		}
		if (num6 > 0)
		{
			text2 = text2 + " / 강화 " + num6;
		}
		if (num7 > 0)
		{
			text2 = text2 + " / 집결 " + num7;
		}
		if (num9 > 0f)
		{
			text2 = text2 + " / 대응 " + Mathf.RoundToInt(num9).ToString("N0");
		}
		return text2;
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
			return "생명력 압박: 긴급 지원/보급 선택지가 필요";
		}
		if (snapshot.clearTimeSeconds >= slowEarlyClearSeconds)
		{
			return "클리어 시간이 김: 초반 고점 보급 또는 몬스터 체력 점검";
		}
		if (snapshot.round <= 5 && snapshot.summons < lowEarlySummonThreshold && snapshot.endGold <= lowEarlyGoldThreshold)
		{
			return "소환/골드 모두 낮음: 3~5R 선택지 보상이 필요";
		}
		if (snapshot.hadMerge && snapshot.highestMergeGrade >= CharacterGrade.Rare && snapshot.clearTimeSeconds < slowEarlyClearSeconds * 0.65f)
		{
			return "폭발 구간: 레어 이상 합성 체감이 좋음";
		}
		return "흐름 안정: 현재 초반 곡선 유지";
	}

	private void UpdateEarlyRunRecoveryRecommendation(EarlyRoundTelemetrySnapshot snapshot)
	{
		if (snapshot != null && snapshot.round <= Mathf.Max(1, earlyTelemetryRoundLimit))
		{
			if (NeedsEarlyRunRecovery(snapshot))
			{
				earlyRunRecoveryRecommended = true;
				earlyRunRecoveryReason = earlyRunTuningHint;
				earlyRunRecoveryCause = ResolveEarlyRunRecoveryCause(snapshot);
			}
			else if (snapshot.round >= 7 && snapshot.cleared && snapshot.clearTimeSeconds < slowEarlyClearSeconds * 0.78f)
			{
				earlyRunRecoveryRecommended = false;
				earlyRunRecoveryReason = "초반 런 안정";
				earlyRunRecoveryCause = "흐름 안정";
			}
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
		return (snapshot.clearTimeSeconds >= slowEarlyClearSeconds && snapshot.summons >= lowEarlySummonThreshold) ? "생명력 압박" : "소환 부족";
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
		return snapshot.round <= 5 && snapshot.summons < lowEarlySummonThreshold && snapshot.endGold <= lowEarlyGoldThreshold;
	}

	private void RequestEarlyRoundTuningBanner(EarlyRoundTelemetrySnapshot snapshot)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		if (snapshot != null && snapshot.round <= Mathf.Max(1, earlyTelemetryRoundLimit) && (!snapshot.cleared || snapshot.clearTimeSeconds >= slowEarlyClearSeconds || (snapshot.bossRound && snapshot.bossHealthRemaining01 >= highBossHealthWarningRatio)))
		{
			this.OnBannerRequested?.Invoke("초반 계측  " + earlyRunTuningHint, new Color(1f, 0.72f, 0.28f), 2.6f);
		}
	}

	private float GetRemainingBossHealth01()
	{
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		float num = 0f;
		bool flag = false;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if (!((Object)(object)monsterUnit == (Object)null) && monsterUnit.IsBoss && !(monsterUnit.MaxHealth <= 0f))
			{
				flag = true;
				num = Mathf.Max(num, Mathf.Clamp01(monsterUnit.CurrentHealth / monsterUnit.MaxHealth));
			}
		}
		return flag ? num : 0f;
	}

	private void ReportRoundCombatRecap(bool bossRound)
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		if (!(topDamageHeroDamage <= 0f))
		{
			string text = "전투 리캡  " + RoundDamageLeaderboardSummary;
			string currentTileContributionSummary = CurrentTileContributionSummary;
			if (!string.IsNullOrWhiteSpace(currentTileContributionSummary) && currentTileContributionSummary != "타일 기여 없음")
			{
				text = text + "  |  타일 " + currentTileContributionSummary;
			}
			this.OnBannerRequested?.Invoke(text, bossRound ? new Color(1f, 0.72f, 0.26f) : new Color(0.4f, 0.92f, 1f), bossRound ? 2.8f : 2.2f);
		}
	}

	private string BuildCurrentTileContributionSummary()
	{
		if (currentRoundTileDamage <= 0f && currentRoundBossTileDamage <= 0f)
		{
			return "타일 기여 없음";
		}
		BoardTileModifierType leadingTileDamageType = GetLeadingTileDamageType();
		string text = ((leadingTileDamageType == BoardTileModifierType.None) ? "타일" : GetTileModifierDisplayName(leadingTileDamageType));
		string text2 = text + " " + Mathf.RoundToInt(currentRoundTileDamage).ToString("N0");
		if (currentRoundBossTileDamage > 0f)
		{
			text2 = text2 + " / 보스 " + Mathf.RoundToInt(currentRoundBossTileDamage).ToString("N0");
		}
		return text2;
	}

	private static void AddDamageContribution(Dictionary<string, float> table, string heroName, float damage)
	{
		if (table != null && !string.IsNullOrWhiteSpace(heroName) && !(damage <= 0f))
		{
			if (!table.ContainsKey(heroName))
			{
				table[heroName] = 0f;
			}
			table[heroName] += damage;
		}
	}

	private static string BuildDamageLeaderboardSummary(Dictionary<string, float> table, int maxCount)
	{
		if (table == null || table.Count <= 0)
		{
			return "기록 없음";
		}
		List<KeyValuePair<string, float>> list = new List<KeyValuePair<string, float>>(table);
		list.Sort((KeyValuePair<string, float> left, KeyValuePair<string, float> right) => right.Value.CompareTo(left.Value));
		int num = Mathf.Min(Mathf.Max(1, maxCount), list.Count);
		string text = string.Empty;
		for (int num2 = 0; num2 < num; num2++)
		{
			if (num2 > 0)
			{
				text += " / ";
			}
			text = text + (num2 + 1) + ". " + list[num2].Key + " " + Mathf.RoundToInt(list[num2].Value).ToString("N0");
		}
		return text;
	}

	private void RecordTileDamageContribution(DefenderUnit source, MonsterUnit target, float damage)
	{
		BoardSlot boardSlot = (((Object)(object)source != (Object)null) ? source.CurrentSlot : null);
		if (!((Object)(object)boardSlot == (Object)null) && boardSlot.TileModifierType != BoardTileModifierType.None && !(damage <= 0f))
		{
			BoardTileModifierType tileModifierType = boardSlot.TileModifierType;
			currentRoundTileDamage += damage;
			if (!currentRoundTileDamageByType.ContainsKey(tileModifierType))
			{
				currentRoundTileDamageByType[tileModifierType] = 0f;
			}
			currentRoundTileDamageByType[tileModifierType] += damage;
			bool flag = (Object)(object)target != (Object)null && target.IsBoss;
			if (flag)
			{
				currentRoundBossTileDamage += damage;
				totalBossTileDamage += damage;
			}
			ReportTileHitFeedback(tileModifierType, flag);
		}
	}

	private void ReportTileHitFeedback(BoardTileModifierType type, bool bossTarget)
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		if (IsRoundRunning && type != BoardTileModifierType.None)
		{
			if (bossTarget && currentRoundBossTileDamage >= (float)nextBossTileFeedbackDamageThreshold && Time.time >= nextBossTileFeedbackTime)
			{
				nextBossTileFeedbackTime = Time.time + Mathf.Max(0.4f, combatFeedbackCooldown);
				nextBossTileFeedbackDamageThreshold += Mathf.Max(1, Mathf.RoundToInt(bossTileContributionFeedbackStep));
				this.OnBannerRequested?.Invoke("보스 대응 적중!  " + GetTileModifierDisplayName(type) + " " + Mathf.RoundToInt(currentRoundBossTileDamage).ToString("N0"), new Color(1f, 0.72f, 0.24f), 1.8f);
			}
			else if (currentRoundTileDamage >= (float)nextTileFeedbackDamageThreshold && Time.time >= nextTileFeedbackTime)
			{
				nextTileFeedbackTime = Time.time + Mathf.Max(0.4f, combatFeedbackCooldown);
				nextTileFeedbackDamageThreshold += Mathf.Max(1, Mathf.RoundToInt(tileContributionFeedbackStep));
				this.OnBannerRequested?.Invoke("타일 적중  " + GetTileModifierDisplayName(type) + " " + Mathf.RoundToInt(currentRoundTileDamage).ToString("N0"), new Color(0.35f, 0.92f, 1f), 1.5f);
			}
		}
	}

	private void ReportSynergyActivationFeedback(int previousCount, string previousTitle)
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if (currentSynergyCount > 0 && !(Time.time < nextSynergyFeedbackTime) && (currentSynergyCount > previousCount || !string.Equals(currentSynergyTitle, previousTitle, StringComparison.Ordinal)))
		{
			nextSynergyFeedbackTime = Time.time + 1.8f;
			this.OnBannerRequested?.Invoke("시너지 발동  " + CurrentSynergySummary, new Color(0.38f, 1f, 0.74f), 1.8f);
		}
	}

	private void ReportTopDamageFeedback(string heroName, float damage, string previousHeroName, float previousDamage)
	{
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		if (IsRoundRunning && !(damage < Mathf.Max(1f, topDamageFeedbackStep)) && !(Time.time < nextTopDamageFeedbackTime))
		{
			bool flag = previousDamage > 0f && !string.Equals(heroName, previousHeroName, StringComparison.Ordinal);
			bool flag2 = damage >= nextTopDamageFeedbackThreshold;
			if (flag || flag2)
			{
				nextTopDamageFeedbackTime = Time.time + Mathf.Max(0.4f, combatFeedbackCooldown);
				nextTopDamageFeedbackThreshold = Mathf.Floor(damage / Mathf.Max(1f, topDamageFeedbackStep) + 1f) * Mathf.Max(1f, topDamageFeedbackStep);
				this.OnBannerRequested?.Invoke((flag ? "최고 딜러 갱신!  " : "딜러 폭주!  ") + heroName + " " + Mathf.RoundToInt(damage).ToString("N0"), new Color(1f, 0.82f, 0.24f), 1.8f);
			}
		}
	}

	private void ReportRoundTileContribution(bool bossRound)
	{
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		if (currentRoundTileDamage < tileContributionBannerMinDamage && currentRoundBossTileDamage < bossTileContributionBannerMinDamage)
		{
			return;
		}
		BoardTileModifierType leadingTileDamageType = GetLeadingTileDamageType();
		if (leadingTileDamageType != BoardTileModifierType.None)
		{
			string tileModifierDisplayName = GetTileModifierDisplayName(leadingTileDamageType);
			if (bossRound && currentRoundBossTileDamage >= bossTileContributionBannerMinDamage)
			{
				this.OnBannerRequested?.Invoke("보스전 배치 적중!  " + tileModifierDisplayName + " 보스 피해 " + Mathf.RoundToInt(currentRoundBossTileDamage).ToString("N0"), new Color(1f, 0.7f, 0.22f), 2.6f);
				return;
			}
			this.OnBannerRequested?.Invoke("전술 타일 기여  " + tileModifierDisplayName + " " + Mathf.RoundToInt(currentRoundTileDamage).ToString("N0") + " 피해", new Color(0.35f, 0.92f, 1f), 2.2f);
		}
	}

	private BoardTileModifierType GetLeadingTileDamageType()
	{
		BoardTileModifierType result = BoardTileModifierType.None;
		float num = 0f;
		foreach (KeyValuePair<BoardTileModifierType, float> item in currentRoundTileDamageByType)
		{
			if (!(item.Value <= num))
			{
				result = item.Key;
				num = item.Value;
			}
		}
		return result;
	}

	private string GetTileModifierDisplayName(BoardTileModifierType type)
	{
		return type switch
		{
			BoardTileModifierType.AttackSpeed => "가속 타일", 
			BoardTileModifierType.Mana => "마나 타일", 
			BoardTileModifierType.Guard => "수호 타일", 
			BoardTileModifierType.Range => "사거리 타일", 
			BoardTileModifierType.Overload => "과부하 타일", 
			BoardTileModifierType.BossHunter => "보스 타일", 
			BoardTileModifierType.Skill => "스킬 타일", 
			_ => "전술 타일", 
		};
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
