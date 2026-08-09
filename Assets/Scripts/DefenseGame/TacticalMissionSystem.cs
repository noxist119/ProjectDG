using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
    public class TacticalMissionSystem : MonoBehaviour
    {
        private enum MissionKind
        {
            GoldReserve,
            PerfectDefense,
            MergeRush,
            RoleCollector,
            LeanDefense,
            BossPreparation,
            SummonSprint,
            LastStandGambit,
            EmptySlotDiscipline,
            RareUpgrade,
            LegendaryHunt,
            MonsterHunter,
            BossSlayer,
            NoSummonHold,
            KillStreak,
            HighGradeForge,
            SpendDownGambit,
            UltimateRecipeChase,
            GradeRainbow
        }

        private sealed class MissionInstance
        {
            public MissionKind kind;
            public int tier;
            public string title;
            public string description;
            public string rewardText;
            public int target;
            public int secondaryTarget;
            public int targetRound;
            public int goldReward;
            public int roundGoldBonus;
            public int rouletteGoldMin;
            public int rouletteGoldMax;
            public int jackpotGold;
            public float jackpotChance;
            public float summonDiscount;
            public int supportSummonReward;
            public bool expiresOnRoundStart;
            public int earliestCompleteRound;
            public int completedRound;
            public Color color;
            public Color accentColor;
            public int startRound;
            public int startLife;
            public int startSummons;
            public int startMerges;
            public int startRarePlusMerges;
            public int startEpicPlusMerges;
            public int startLegendaryPlusMerges;
            public int startFinalMerges;
            public int startKills;
            public int startBossKills;

            public string Key => kind + ":" + tier;
        }

        [SerializeField] private DefenseGameController gameController;
        [SerializeField] private DefenseBoardManager boardManager;
        [SerializeField] private Button summaryButton;
        [SerializeField] private Text summaryText;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text panelHeaderText;
        [SerializeField] private GameObject activeCardRoot;
        [SerializeField] private Text activeTitleText;
        [SerializeField] private Text activeDescriptionText;
        [SerializeField] private Text activeProgressText;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private Text[] optionTitleTexts;
        [SerializeField] private Text[] optionDescriptionTexts;
        [SerializeField] private Text[] optionRewardTexts;
        [SerializeField] private Image[] optionAccentImages;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject completionToastRoot;
        [SerializeField] private CanvasGroup completionToastGroup;
        [SerializeField] private Text completionToastTitleText;
        [SerializeField] private Text completionToastRewardText;

        private const int MaxActiveMissions = 3;
        private const float CompletionToastDuration = 2.4f;

        private readonly List<MissionInstance> activeMissions = new List<MissionInstance>();
        private readonly Dictionary<MissionKind, int> completedFamilyLevels = new Dictionary<MissionKind, int>();
        private readonly HashSet<string> completedMissionKeys = new HashSet<string>();
        private readonly Dictionary<string, int> recentlyExpiredKeys = new Dictionary<string, int>();
        private readonly List<string> recentCompletionFeed = new List<string>();
        private int totalSummons;
        private int totalMerges;
        private int totalRarePlusMerges;
        private int totalEpicPlusMerges;
        private int totalLegendaryPlusMerges;
        private int totalFinalMerges;
        private int totalKills;
        private int totalBossKills;
        private int missionCursor;
        private int completedMissionCount;
        private float toastTimer;
        private bool subscribed;
        private bool resolvingMission;
        private int pendingMissionSupportSummons;
        private bool missionSupportWaitingForSlot;
        private bool runStarted;

        public int PendingMissionSupportSummons => pendingMissionSupportSummons;
        public bool HasInitialStrategyFork => HasActiveMission(MissionKind.SummonSprint) && HasActiveMission(MissionKind.LastStandGambit);

        public void Configure(
            DefenseGameController controller,
            DefenseBoardManager board,
            Button missionSummaryButton,
            Text missionSummaryText,
            GameObject missionPanelRoot,
            Text missionPanelHeader,
            GameObject activeMissionCard,
            Text activeTitle,
            Text activeDescription,
            Text activeProgress,
            Button[] missionOptionButtons,
            Text[] missionOptionTitles,
            Text[] missionOptionDescriptions,
            Text[] missionOptionRewards,
            Image[] missionOptionAccents,
            Button missionCloseButton,
            GameObject missionCompletionToastRoot = null,
            CanvasGroup missionCompletionToastGroup = null,
            Text missionCompletionToastTitle = null,
            Text missionCompletionToastReward = null)
        {
            Unsubscribe();
            gameController = controller;
            boardManager = board;
            summaryButton = missionSummaryButton;
            summaryText = missionSummaryText;
            panelRoot = missionPanelRoot;
            panelHeaderText = missionPanelHeader;
            activeCardRoot = activeMissionCard;
            activeTitleText = activeTitle;
            activeDescriptionText = activeDescription;
            activeProgressText = activeProgress;
            optionButtons = missionOptionButtons;
            optionTitleTexts = missionOptionTitles;
            optionDescriptionTexts = missionOptionDescriptions;
            optionRewardTexts = missionOptionRewards;
            optionAccentImages = missionOptionAccents;
            closeButton = missionCloseButton;
            completionToastRoot = missionCompletionToastRoot;
            completionToastGroup = missionCompletionToastGroup;
            completionToastTitleText = missionCompletionToastTitle;
            completionToastRewardText = missionCompletionToastReward;

            ResetRunState();
            WireUi();
            Subscribe();
            RefillMissions();
            SetPanelOpen(false);
            HideCompletionToast();
            RefreshUi();
        }

        private void OnEnable()
        {
            WireUi();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            UpdateCompletionToast();
        }

        private void ResetRunState()
        {
            activeMissions.Clear();
            completedFamilyLevels.Clear();
            completedMissionKeys.Clear();
            recentlyExpiredKeys.Clear();
            recentCompletionFeed.Clear();
            totalSummons = 0;
            totalMerges = 0;
            totalRarePlusMerges = 0;
            totalEpicPlusMerges = 0;
            totalLegendaryPlusMerges = 0;
            totalFinalMerges = 0;
            totalKills = 0;
            totalBossKills = 0;
            missionCursor = 0;
            completedMissionCount = 0;
            toastTimer = 0f;
            resolvingMission = false;
            pendingMissionSupportSummons = 0;
            missionSupportWaitingForSlot = false;
            runStarted = false;
        }

        private void WireUi()
        {
            if (summaryButton != null)
            {
                summaryButton.onClick.RemoveListener(TogglePanel);
                summaryButton.onClick.AddListener(TogglePanel);
                SetChildText(summaryButton.transform, "MissionOpenHint", "보기");
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePanel);
                closeButton.onClick.AddListener(ClosePanel);
            }

            if (optionButtons == null)
            {
                return;
            }

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == null)
                {
                    continue;
                }

                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].interactable = true;
                SetChildText(optionButtons[i].transform, "PickLabel", "진행");
            }
        }

        private void Subscribe()
        {
            if (subscribed || gameController == null)
            {
                return;
            }

            gameController.OnStateChanged += HandleStateChanged;
            gameController.OnMergeCompleted += HandleMergeCompleted;
            gameController.OnRoundStarted += HandleRoundStarted;
            gameController.OnRoundMissionSettlement += HandleRoundMissionSettlement;
            gameController.OnRoundBoardPreparation += HandleRoundBoardPreparation;
            gameController.OnGameOver += HandleGameOver;
            gameController.OnPlayerSummoned += HandleUnitSummoned;
            MonsterUnit.OnMonsterKilled += HandleMonsterKilled;
            MonsterUnit.OnMonsterEscaped += HandleMonsterEscaped;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || gameController == null)
            {
                return;
            }

            gameController.OnStateChanged -= HandleStateChanged;
            gameController.OnMergeCompleted -= HandleMergeCompleted;
            gameController.OnRoundStarted -= HandleRoundStarted;
            gameController.OnRoundMissionSettlement -= HandleRoundMissionSettlement;
            gameController.OnRoundBoardPreparation -= HandleRoundBoardPreparation;
            gameController.OnGameOver -= HandleGameOver;
            gameController.OnPlayerSummoned -= HandleUnitSummoned;
            MonsterUnit.OnMonsterKilled -= HandleMonsterKilled;
            MonsterUnit.OnMonsterEscaped -= HandleMonsterEscaped;
            subscribed = false;
        }

        private void RefillMissions()
        {
            if (gameController == null)
            {
                return;
            }

            ClearExpiredCooldowns();

            if (activeMissions.Count == 0 && completedMissionCount == 0 && gameController.CurrentRound <= 0)
            {
                AddInitialStrategyMission(MissionKind.SummonSprint);
                AddInitialStrategyMission(MissionKind.LastStandGambit);
            }

            int guard = 0;
            while (activeMissions.Count < MaxActiveMissions && guard < 40)
            {
                MissionInstance mission = CreateNextMissionCandidate(guard);
                guard++;

                if (mission == null || IsMissionActive(mission.Key) || completedMissionKeys.Contains(mission.Key) || IsRecentlyExpired(mission.Key))
                {
                    continue;
                }

                activeMissions.Add(mission);
            }
        }

        private void AddInitialStrategyMission(MissionKind kind)
        {
            MissionInstance mission = CreateMission(kind, GetNextTier(kind));
            if (mission != null && !IsMissionActive(mission.Key) && !completedMissionKeys.Contains(mission.Key))
            {
                activeMissions.Add(mission);
            }
        }

        private MissionInstance CreateNextMissionCandidate(int attempt)
        {
            MissionKind[] candidateOrder = BuildCandidateOrder();
            if (candidateOrder.Length == 0)
            {
                return null;
            }

            int index = Mathf.Abs(missionCursor) % candidateOrder.Length;
            MissionKind kind = candidateOrder[index];
            int tier = GetNextTier(kind);

            while (completedMissionKeys.Contains(kind + ":" + tier))
            {
                tier++;
            }

            missionCursor++;
            return CreateMission(kind, tier);
        }

        private MissionKind[] BuildCandidateOrder()
        {
            int round = gameController != null ? gameController.CurrentRound : 0;
            if (round <= 0)
            {
                return new[]
                {
                    MissionKind.SummonSprint,
                    MissionKind.LastStandGambit,
                    MissionKind.PerfectDefense,
                    MissionKind.MonsterHunter,
                    MissionKind.MergeRush,
                    MissionKind.RoleCollector
                };
            }

            List<MissionKind> candidates = new List<MissionKind>
            {
                MissionKind.GoldReserve,
                MissionKind.SummonSprint,
                MissionKind.MergeRush,
                MissionKind.PerfectDefense,
                MissionKind.RoleCollector,
                MissionKind.MonsterHunter
            };

            if (round >= 2)
            {
                candidates.Add(MissionKind.EmptySlotDiscipline);
                candidates.Add(MissionKind.RareUpgrade);
                candidates.Add(MissionKind.KillStreak);
                candidates.Add(MissionKind.SpendDownGambit);
            }

            if (round >= 4)
            {
                candidates.Add(MissionKind.LeanDefense);
                candidates.Add(MissionKind.NoSummonHold);
                candidates.Add(MissionKind.GradeRainbow);
            }

            if (round >= 5)
            {
                candidates.Add(MissionKind.BossPreparation);
                candidates.Add(MissionKind.HighGradeForge);
            }

            if (round >= 6)
            {
                candidates.Add(MissionKind.LegendaryHunt);
            }

            if (round >= 8 || GetRoundsUntilNextBoss() <= 2)
            {
                candidates.Add(MissionKind.BossSlayer);
            }

            if (round >= 9)
            {
                candidates.Add(MissionKind.UltimateRecipeChase);
            }

            return candidates.ToArray();
        }

        private MissionInstance CreateMission(MissionKind kind, int tier)
        {
            int round = gameController != null ? gameController.CurrentRound : 0;
            int displayTier = tier + 1;
            MissionInstance mission = new MissionInstance
            {
                kind = kind,
                tier = tier,
                startRound = round,
                startLife = gameController != null ? gameController.Life : 0,
                startSummons = totalSummons,
                startMerges = totalMerges,
                startRarePlusMerges = totalRarePlusMerges,
                startEpicPlusMerges = totalEpicPlusMerges,
                startLegendaryPlusMerges = totalLegendaryPlusMerges,
                startFinalMerges = totalFinalMerges,
                startKills = totalKills,
                startBossKills = totalBossKills,
                earliestCompleteRound = round + 1,
                color = new Color(1f, 0.78f, 0.30f),
                accentColor = new Color(1f, 0.92f, 0.58f),
                goldReward = 26 + tier * 9
            };

            switch (kind)
            {
                case MissionKind.GoldReserve:
                    mission.target = 95 + tier * 40 + round * 5;
                    mission.targetRound = round + 3 + Mathf.Min(2, tier / 2);
                    mission.goldReward = 42 + tier * 14;
                    mission.title = "골드 창고 " + ToRoman(displayTier);
                    mission.description = "라운드가 오기 전까지 목표 골드를 보유하세요. 소환을 참을수록 보상이 커집니다.";
                    mission.rewardText = "+" + mission.goldReward + "골드";
                    mission.color = new Color(1f, 0.76f, 0.22f);
                    mission.expiresOnRoundStart = true;
                    break;
                case MissionKind.PerfectDefense:
                    mission.targetRound = round + 1;
                    mission.goldReward = 34 + tier * 10;
                    mission.roundGoldBonus = 1 + tier / 2;
                    mission.title = "무결 방어 " + ToRoman(displayTier);
                    mission.description = "다음 라운드를 체력 손실 없이 막아내세요.";
                    mission.rewardText = "+" + mission.goldReward + "골드, 라운드 보너스 +" + mission.roundGoldBonus;
                    mission.color = new Color(0.42f, 1f, 0.72f);
                    break;
                case MissionKind.MergeRush:
                    mission.target = 1 + Mathf.Min(4, tier + round / 7);
                    mission.targetRound = round + 2;
                    mission.earliestCompleteRound = round;
                    mission.goldReward = 36 + tier * 12;
                    mission.title = "합성 러시 " + ToRoman(displayTier);
                    mission.description = "제한 라운드 안에 합성을 성공시켜 성장 속도를 끌어올리세요.";
                    mission.rewardText = "+" + mission.goldReward + "골드";
                    mission.color = new Color(0.86f, 0.48f, 1f);
                    break;
                case MissionKind.RoleCollector:
                    mission.target = Mathf.Clamp(4 + tier, 4, 6);
                    mission.targetRound = round + 4;
                    mission.goldReward = 30 + tier * 10;
                    mission.summonDiscount = Mathf.Min(0.03f + tier * 0.01f, 0.08f);
                    mission.title = "역할 컬렉터 " + ToRoman(displayTier);
                    mission.description = "서로 다른 역할의 유닛을 모아 시너지 선택지를 넓히세요.";
                    mission.rewardText = "+" + mission.goldReward + "골드, 소환비 할인";
                    mission.color = new Color(0.46f, 0.86f, 1f);
                    break;
                case MissionKind.LeanDefense:
                    mission.target = Mathf.Max(4, 6 - Mathf.Min(2, tier));
                    mission.secondaryTarget = 2;
                    mission.targetRound = round + 1;
                    mission.goldReward = 48 + tier * 13;
                    mission.title = "소수 정예 " + ToRoman(displayTier);
                    mission.description = "적은 유닛으로 다음 라운드를 버텨내면 높은 보상을 받습니다.";
                    mission.rewardText = "+" + mission.goldReward + "골드";
                    mission.color = new Color(1f, 0.52f, 0.34f);
                    break;
                case MissionKind.BossPreparation:
                    mission.target = 1 + Mathf.Min(3, tier);
                    mission.targetRound = GetNextBossRound(round);
                    mission.goldReward = 40 + tier * 14;
                    mission.roundGoldBonus = 2 + tier;
                    mission.title = "보스 브레이커 " + ToRoman(displayTier);
                    mission.description = "다음 보스가 오기 전까지 전설 이상 유닛을 준비하세요.";
                    mission.rewardText = "+" + mission.goldReward + "골드, 라운드 보너스 +" + mission.roundGoldBonus;
                    mission.color = new Color(1f, 0.36f, 0.46f);
                    mission.expiresOnRoundStart = true;
                    break;
                case MissionKind.SummonSprint:
                    mission.target = 3 + Mathf.Min(5, tier + round / 5);
                    mission.targetRound = round + 2;
                    mission.earliestCompleteRound = round;
                    mission.goldReward = 26 + tier * 9;
                    mission.title = "소환 스퍼트 " + ToRoman(displayTier);
                    mission.description = "빠르게 전장을 채워 초반 화력을 확보하세요.";
                    mission.rewardText = "+" + mission.goldReward + "골드";
                    mission.color = new Color(0.30f, 0.76f, 1f);
                    break;
                case MissionKind.LastStandGambit:
                    mission.targetRound = 4;
                    mission.earliestCompleteRound = 0;
                    mission.goldReward = 0;
                    mission.supportSummonReward = 1;
                    mission.title = "배수의 진";
                    mission.description = "R3까지 HP 7 이하를 감수하고 소환 2회·유닛 2기 이하를 유지하세요. 성공 시 다음 준비 단계에 무료 지원 유닛이 옵니다.";
                    mission.rewardText = "다음 준비 단계 지원 유닛 1개 예약";
                    mission.color = new Color(1f, 0.38f, 0.28f);
                    mission.expiresOnRoundStart = true;
                    break;
                case MissionKind.EmptySlotDiscipline:
                    mission.target = Mathf.Clamp(2 + tier, 2, 4);
                    mission.targetRound = round + 1;
                    mission.goldReward = 38 + tier * 11;
                    mission.title = "빈칸 운영 " + ToRoman(displayTier);
                    mission.description = "다음 라운드 종료 시 빈 슬롯을 남겨 합성 여지를 유지하세요.";
                    mission.rewardText = "+" + mission.goldReward + "골드";
                    mission.color = new Color(0.66f, 0.92f, 1f);
                    break;
                case MissionKind.RareUpgrade:
                    mission.target = 2 + Mathf.Min(4, tier);
                    mission.targetRound = round + 4;
                    mission.goldReward = 32 + tier * 11;
                    mission.title = "레어 라인업 " + ToRoman(displayTier);
                    mission.description = "레어 이상 유닛을 확보해 전투 안정성을 올리세요.";
                    mission.rewardText = "+" + mission.goldReward + "골드";
                    mission.color = new Color(0.25f, 0.62f, 1f);
                    break;
                case MissionKind.LegendaryHunt:
                    mission.target = 1 + tier / 2;
                    mission.targetRound = round + 5;
                    mission.goldReward = 58 + tier * 18;
                    mission.roundGoldBonus = 1 + tier / 2;
                    mission.title = "전설 탐색 " + ToRoman(displayTier);
                    mission.description = "전설 이상 유닛을 만들어 판을 뒤집을 힘을 모으세요.";
                    mission.rewardText = "+" + mission.goldReward + "골드, 라운드 보너스 +" + mission.roundGoldBonus;
                    mission.color = new Color(1f, 0.68f, 0.20f);
                    break;
                case MissionKind.MonsterHunter:
                    mission.target = 12 + round * 3 + tier * 7;
                    mission.targetRound = round + 2;
                    mission.earliestCompleteRound = round;
                    mission.goldReward = 34 + tier * 10;
                    mission.title = "몬스터 사냥 " + ToRoman(displayTier);
                    mission.description = "제한 라운드 안에 몬스터를 처치해 추가 골드를 받으세요.";
                    mission.rewardText = "+" + mission.goldReward + "골드";
                    mission.color = new Color(0.52f, 1f, 0.58f);
                    break;
                case MissionKind.BossSlayer:
                    mission.target = 1;
                    mission.targetRound = GetNextBossRound(round);
                    mission.goldReward = 80 + tier * 22;
                    mission.roundGoldBonus = 3 + tier;
                    mission.rouletteGoldMin = 8 + tier * 4;
                    mission.rouletteGoldMax = 28 + tier * 8;
                    mission.jackpotChance = Mathf.Min(0.18f + tier * 0.025f, 0.32f);
                    mission.jackpotGold = 45 + tier * 15;
                    mission.title = "보스 처단 " + ToRoman(displayTier);
                    mission.description = "다음 보스 라운드에서 보스를 쓰러뜨리세요.";
                    mission.rewardText = "+" + mission.goldReward + "골드, 라운드 보너스 +" + mission.roundGoldBonus;
                    mission.color = new Color(1f, 0.24f, 0.26f);
                    break;
                case MissionKind.NoSummonHold:
                    mission.targetRound = round + 1;
                    mission.secondaryTarget = Mathf.Min(1 + tier / 2, 2);
                    mission.goldReward = 24 + tier * 9;
                    mission.rouletteGoldMin = 10 + tier * 4;
                    mission.rouletteGoldMax = 30 + tier * 7;
                    mission.summonDiscount = Mathf.Min(0.04f + tier * 0.01f, 0.10f);
                    mission.title = "봉인된 지갑 " + ToRoman(displayTier);
                    mission.description = "다음 라운드 동안 소환하지 않고 버텨보세요. 이미 만든 덱을 믿는 고위험 계약입니다.";
                    mission.color = new Color(0.38f, 1f, 0.92f);
                    break;
                case MissionKind.KillStreak:
                    mission.target = 10 + round * 3 + tier * 6;
                    mission.targetRound = round + 2;
                    mission.goldReward = 28 + tier * 10;
                    mission.rouletteGoldMin = 6 + tier * 3;
                    mission.rouletteGoldMax = 22 + tier * 7;
                    mission.jackpotChance = Mathf.Min(0.12f + tier * 0.02f, 0.26f);
                    mission.jackpotGold = 28 + tier * 12;
                    mission.title = "처치 콤보 " + ToRoman(displayTier);
                    mission.description = "체력을 잃지 않은 채 제한 라운드 안에 몬스터를 몰아 잡으세요. 끊기지 않으면 잭팟이 붙습니다.";
                    mission.color = new Color(0.52f, 1f, 0.48f);
                    break;
                case MissionKind.HighGradeForge:
                    mission.target = 1;
                    mission.secondaryTarget = (int)(tier >= 4 ? CharacterGrade.Legendary : tier >= 2 ? CharacterGrade.Epic : CharacterGrade.Rare);
                    mission.targetRound = round + 4;
                    mission.earliestCompleteRound = round;
                    mission.goldReward = 36 + tier * 12;
                    mission.rouletteGoldMin = 12 + tier * 5;
                    mission.rouletteGoldMax = 42 + tier * 10;
                    mission.jackpotChance = Mathf.Min(0.15f + tier * 0.025f, 0.34f);
                    mission.jackpotGold = 35 + tier * 14;
                    mission.title = "고등급 도박 " + ToRoman(displayTier);
                    mission.description = "제한 시간 안에 합성으로 " + CharacterGradeUtility.GetDisplayName((CharacterGrade)mission.secondaryTarget) + " 이상 결과를 뽑으세요. 성공하면 정산 룰렛이 크게 돌아갑니다.";
                    mission.color = new Color(1f, 0.48f, 0.92f);
                    break;
                case MissionKind.SpendDownGambit:
                    mission.target = Mathf.Max(6, Mathf.RoundToInt((gameController != null ? gameController.SummonCost : 10) * 0.65f) + tier * 2);
                    mission.targetRound = round + 1;
                    mission.goldReward = 18 + tier * 7;
                    mission.rouletteGoldMin = 14 + tier * 5;
                    mission.rouletteGoldMax = 44 + tier * 11;
                    mission.jackpotChance = Mathf.Min(0.10f + tier * 0.018f, 0.24f);
                    mission.jackpotGold = 32 + tier * 10;
                    mission.title = "올인 운영 " + ToRoman(displayTier);
                    mission.description = "다음 라운드 종료 시 골드를 거의 남기지 마세요. 전부 쏟아붓고 살아남으면 큰 보상이 따라옵니다.";
                    mission.color = new Color(1f, 0.42f, 0.30f);
                    break;
                case MissionKind.UltimateRecipeChase:
                    mission.target = 1;
                    mission.targetRound = GetNextBossRound(round);
                    mission.goldReward = 64 + tier * 20;
                    mission.roundGoldBonus = 2 + tier / 2;
                    mission.rouletteGoldMin = 18 + tier * 7;
                    mission.rouletteGoldMax = 60 + tier * 14;
                    mission.jackpotChance = Mathf.Min(0.18f + tier * 0.03f, 0.38f);
                    mission.jackpotGold = 60 + tier * 18;
                    mission.title = "레시피 추적 " + ToRoman(displayTier);
                    mission.description = "다음 보스 전까지 초월 합성 재료를 완성하거나 초월 합성을 성공시키세요. 한 판의 목표를 크게 바꿉니다.";
                    mission.color = new Color(0.92f, 0.54f, 1f);
                    mission.expiresOnRoundStart = true;
                    break;
                case MissionKind.GradeRainbow:
                    mission.target = Mathf.Clamp(3 + tier / 2, 3, 5);
                    mission.targetRound = round + 3;
                    mission.goldReward = 30 + tier * 10;
                    mission.summonDiscount = Mathf.Min(0.03f + tier * 0.012f, 0.09f);
                    mission.rouletteGoldMin = 8 + tier * 3;
                    mission.rouletteGoldMax = 26 + tier * 7;
                    mission.title = "등급 무지개 " + ToRoman(displayTier);
                    mission.description = "서로 다른 등급의 유닛을 동시에 보유하세요. 마구 합치기보다 판을 설계하는 미션입니다.";
                    mission.color = new Color(0.42f, 0.72f, 1f);
                    break;
            }

            ApplyRewardPacing(mission);
            mission.accentColor = Color.Lerp(mission.color, Color.white, 0.24f);
            return mission;
        }

        private void ApplyRewardPacing(MissionInstance mission)
        {
            if (mission == null)
            {
                return;
            }

            int round = gameController != null ? gameController.CurrentRound : 0;
            float goldMultiplier = 1f;
            if (round <= 2)
            {
                goldMultiplier = 0.25f;
            }
            else if (round <= 5)
            {
                goldMultiplier = 0.42f;
            }
            else if (round <= 9)
            {
                goldMultiplier = 0.62f;
            }
            else if (round <= 14)
            {
                goldMultiplier = 0.82f;
            }

            int rewardFloor = round <= 2 ? 5 : round <= 5 ? 7 : 10;
            if (mission.goldReward > 0)
            {
                mission.goldReward = Mathf.Max(rewardFloor, Mathf.RoundToInt(mission.goldReward * goldMultiplier));
            }
            if (mission.rouletteGoldMax > 0)
            {
                mission.rouletteGoldMin = Mathf.Max(1, Mathf.RoundToInt(mission.rouletteGoldMin * goldMultiplier));
                mission.rouletteGoldMax = Mathf.Max(mission.rouletteGoldMin, Mathf.RoundToInt(mission.rouletteGoldMax * goldMultiplier));
            }

            if (mission.jackpotGold > 0)
            {
                mission.jackpotGold = Mathf.Max(rewardFloor, Mathf.RoundToInt(mission.jackpotGold * goldMultiplier));
                mission.jackpotChance = Mathf.Clamp01(mission.jackpotChance * Mathf.Lerp(0.72f, 1f, goldMultiplier));
            }

            if (round <= 5)
            {
                mission.roundGoldBonus = 0;
                mission.summonDiscount = Mathf.Min(mission.summonDiscount, 0.015f);
            }
            else if (round <= 9)
            {
                mission.roundGoldBonus = Mathf.Min(mission.roundGoldBonus, 1);
                mission.summonDiscount = Mathf.Min(mission.summonDiscount, 0.03f);
            }
            else if (round <= 14)
            {
                mission.roundGoldBonus = Mathf.Min(mission.roundGoldBonus, 2);
                mission.summonDiscount = Mathf.Min(mission.summonDiscount, 0.05f);
            }

            mission.rewardText = BuildRewardText(mission);
        }

        private string BuildRewardText(MissionInstance mission)
        {
            if (mission == null)
            {
                return string.Empty;
            }

            string text = mission.goldReward > 0 ? "+" + mission.goldReward + "골드" : string.Empty;
            if (mission.roundGoldBonus > 0)
            {
                text += ", 라운드 보너스 +" + mission.roundGoldBonus;
            }

            if (mission.summonDiscount > 0f)
            {
                text += ", 소환비 할인";
            }

            if (mission.rouletteGoldMax > 0)
            {
                text += ", 룰렛 " + mission.rouletteGoldMin + "~" + mission.rouletteGoldMax + "G";
            }

            if (mission.jackpotGold > 0 && mission.jackpotChance > 0f)
            {
                text += ", 잭팟 " + Mathf.RoundToInt(mission.jackpotChance * 100f) + "%";
            }

            if (mission.supportSummonReward > 0)
            {
                text += (text.Length > 0 ? ", " : string.Empty) + "지원 유닛 " + mission.supportSummonReward + "개 예약";
            }

            return string.IsNullOrEmpty(text) ? "보상 없음" : text;
        }

        private string BuildRewardSummary(int goldReward, int roundGoldBonus, float summonDiscount)
        {
            string text = goldReward > 0 ? "+" + goldReward + "골드" : "보상 없음";
            if (roundGoldBonus > 0)
            {
                text += ", 라운드 보너스 +" + roundGoldBonus;
            }

            if (summonDiscount > 0f)
            {
                text += ", 소환비 -" + Mathf.RoundToInt(summonDiscount * 100f) + "%";
            }

            return text;
        }

        private void EvaluateMissions(bool roundCompleted = false, int completedRound = 0)
        {
            if (resolvingMission || gameController == null)
            {
                return;
            }

            // Determine the eligible set before paying. AddGold may synchronously invoke OnStateChanged.
            List<MissionInstance> completed = new List<MissionInstance>();
            List<MissionInstance> expired = new List<MissionInstance>();
            for (int i = 0; i < activeMissions.Count; i++)
            {
                MissionInstance mission = activeMissions[i];
                if (IsMissionComplete(mission, roundCompleted, completedRound))
                {
                    completed.Add(mission);
                }
                else if (IsMissionExpired(mission, roundCompleted, completedRound))
                {
                    expired.Add(mission);
                }
            }

            int resolvedRound = roundCompleted ? completedRound : gameController.CurrentRound;
            for (int i = 0; i < completed.Count; i++)
            {
                CompleteMission(completed[i], resolvedRound);
            }

            for (int i = 0; i < expired.Count; i++)
            {
                int index = activeMissions.IndexOf(expired[i]);
                if (index >= 0)
                {
                    ExpireMission(index);
                }
            }

            RefillMissions();
            RefreshUi();
        }
        private bool IsMissionComplete(MissionInstance mission, bool roundCompleted, int completedRound)
        {
            if (mission == null || gameController == null)
            {
                return false;
            }

            int checkRound = roundCompleted ? completedRound : gameController.CurrentRound;
            if (checkRound < mission.earliestCompleteRound)
            {
                return false;
            }

            switch (mission.kind)
            {
                case MissionKind.GoldReserve:
                    return gameController.Gold >= mission.target;
                case MissionKind.PerfectDefense:
                    return roundCompleted && completedRound == mission.targetRound && gameController.Life >= mission.startLife;
                case MissionKind.MergeRush:
                    return totalMerges - mission.startMerges >= mission.target;
                case MissionKind.RoleCollector:
                    return CountDistinctRoles() >= mission.target;
                case MissionKind.LeanDefense:
                    return roundCompleted &&
                        completedRound == mission.targetRound &&
                        gameController.BoardUnitCount <= mission.target &&
                        Mathf.Max(0, mission.startLife - gameController.Life) <= mission.secondaryTarget;
                case MissionKind.BossPreparation:
                    return CountUnitsAtLeast(CharacterGrade.Legendary) >= mission.target;
                case MissionKind.SummonSprint:
                    return totalSummons - mission.startSummons >= mission.target;
                case MissionKind.LastStandGambit:
                    return IsLastStandGambitConditionMet(gameController.Life, totalSummons - mission.startSummons, gameController.BoardUnitCount, checkRound);
                case MissionKind.EmptySlotDiscipline:
                    return roundCompleted && completedRound == mission.targetRound && gameController.EmptySlotCount >= mission.target;
                case MissionKind.RareUpgrade:
                    return CountUnitsAtLeast(CharacterGrade.Rare) >= mission.target;
                case MissionKind.LegendaryHunt:
                    return CountUnitsAtLeast(CharacterGrade.Legendary) >= mission.target;
                case MissionKind.MonsterHunter:
                    return totalKills - mission.startKills >= mission.target;
                case MissionKind.BossSlayer:
                    return totalBossKills - mission.startBossKills >= mission.target;
                case MissionKind.NoSummonHold:
                    return roundCompleted &&
                        completedRound == mission.targetRound &&
                        totalSummons == mission.startSummons &&
                        Mathf.Max(0, mission.startLife - gameController.Life) <= mission.secondaryTarget;
                case MissionKind.KillStreak:
                    return totalKills - mission.startKills >= mission.target &&
                        gameController.Life >= mission.startLife;
                case MissionKind.HighGradeForge:
                    CharacterGrade requiredForgeGrade = (CharacterGrade)Mathf.Clamp(mission.secondaryTarget, (int)CharacterGrade.Normal, (int)CharacterGrade.Transcendent);
                    return GetMergeResultsAtLeast(requiredForgeGrade) - GetStartMergeResultsAtLeast(mission, requiredForgeGrade) >= mission.target;
                case MissionKind.SpendDownGambit:
                    int goldBeforeClearReward = Mathf.Max(0, gameController.Gold - gameController.LastRoundClearGoldReward);
                    return roundCompleted && completedRound == mission.targetRound && goldBeforeClearReward <= mission.target;
                case MissionKind.UltimateRecipeChase:
                    return gameController.CanMergeUltimate() || totalFinalMerges - mission.startFinalMerges >= mission.target;
                case MissionKind.GradeRainbow:
                    return CountDistinctGrades() >= mission.target;
                default:
                    return false;
            }
        }

        private bool IsMissionExpired(MissionInstance mission, bool roundCompleted, int completedRound)
        {
            if (mission == null || mission.targetRound <= 0 || gameController == null)
            {
                return false;
            }

            if (mission.expiresOnRoundStart)
            {
                return gameController.IsRoundRunning && gameController.CurrentRound >= mission.targetRound;
            }

            return roundCompleted && completedRound >= mission.targetRound;
        }

        private void CompleteMission(MissionInstance mission, int completedRound)
        {
            if (mission == null || gameController == null || resolvingMission || completedMissionKeys.Contains(mission.Key))
            {
                return;
            }

            int index = activeMissions.IndexOf(mission);
            if (index < 0)
            {
                return;
            }

            // Record completion before payout. AddGold can synchronously publish OnStateChanged.
            activeMissions.RemoveAt(index);
            completedMissionKeys.Add(mission.Key);
            completedFamilyLevels[mission.kind] = GetCompletedFamilyLevel(mission.kind) + 1;
            completedMissionCount++;
            mission.completedRound = Mathf.Max(0, completedRound);

            resolvingMission = true;
            try
            {
                PayMissionRewardImmediately(mission);
            }
            finally
            {
                resolvingMission = false;
            }
        }

        private void PayMissionRewardImmediately(MissionInstance mission)
        {
            int goldReward = Mathf.Max(0, mission.goldReward);
            int roundGoldBonus = Mathf.Max(0, mission.roundGoldBonus);
            float summonDiscount = Mathf.Max(0f, mission.summonDiscount);
            List<string> highlights = new List<string>();

            if (mission.rouletteGoldMax > 0)
            {
                int min = Mathf.Max(0, mission.rouletteGoldMin);
                int max = Mathf.Max(min, mission.rouletteGoldMax);
                int roll = UnityEngine.Random.Range(min, max + 1);
                goldReward += roll;
                highlights.Add("룰렛 +" + roll + "G");
            }

            if (mission.jackpotGold > 0 && mission.jackpotChance > 0f && UnityEngine.Random.value <= mission.jackpotChance)
            {
                int jackpot = Mathf.Max(1, mission.jackpotGold);
                goldReward += jackpot;
                highlights.Add("JACKPOT! +" + jackpot + "G");
            }

            if (goldReward > 0)
            {
                gameController.AddGold(goldReward);
            }
            if (roundGoldBonus > 0)
            {
                gameController.AddRoundGoldBonus(roundGoldBonus);
            }
            if (summonDiscount > 0f)
            {
                gameController.AddSummonCostDiscount(summonDiscount);
            }
            if (mission.supportSummonReward > 0)
            {
                pendingMissionSupportSummons += mission.supportSummonReward;
                missionSupportWaitingForSlot = false;
            }

            string rewardSummary = mission.supportSummonReward > 0
                ? "다음 준비 단계 지원 유닛 " + mission.supportSummonReward + "개 예약"
                : BuildRewardSummary(goldReward, roundGoldBonus, summonDiscount);
            if (highlights.Count > 0 && mission.supportSummonReward <= 0)
            {
                rewardSummary += " · " + string.Join(" · ", highlights.ToArray());
            }

            AddCompletionFeed("미션 완료! " + mission.title + "  " + rewardSummary);
            ShowCompletionToast(mission, rewardSummary);
            string banner = mission.kind == MissionKind.LastStandGambit
                ? "배수의 진 성공! 다음 준비 단계 지원 유닛 예약"
                : "미션 완료! " + rewardSummary;
            gameController.RequestBanner(banner, mission.color, 2.8f);
            RuntimeCameraShake.Request(0.055f, 0.18f);
        }
        private void ExpireMission(int index)
        {
            if (index < 0 || index >= activeMissions.Count)
            {
                return;
            }

            MissionInstance mission = activeMissions[index];
            activeMissions.RemoveAt(index);
            recentlyExpiredKeys[mission.Key] = (gameController != null ? gameController.CurrentRound : 0) + 3;

            if (gameController != null)
            {
                gameController.RequestBanner("미션 갱신  " + mission.title + " 조건이 바뀌었어요", new Color(0.75f, 0.86f, 1f), 1.8f);
            }
        }

        private void HandleStateChanged()
        {
            if (runStarted && gameController != null && !gameController.IsRoundRunning && gameController.CurrentRound <= 0)
            {
                ResetRunState();
                RefillMissions();
                RefreshUi();
                return;
            }

            EvaluateMissions();
        }

        private void HandleUnitSummoned(CharacterDefinition definition)
        {
            totalSummons++;
            EvaluateMissions();
        }

        private void HandleMergeCompleted(MergeResultInfo result)
        {
            totalMerges++;
            if ((int)result.resultGrade >= (int)CharacterGrade.Rare)
            {
                totalRarePlusMerges++;
            }

            if ((int)result.resultGrade >= (int)CharacterGrade.Epic)
            {
                totalEpicPlusMerges++;
            }

            if ((int)result.resultGrade >= (int)CharacterGrade.Legendary)
            {
                totalLegendaryPlusMerges++;
            }

            if (result.isFinalMerge)
            {
                totalFinalMerges++;
            }

            EvaluateMissions();
        }

        private void HandleRoundStarted(int round)
        {
            runStarted = true;
            for (int i = 0; i < activeMissions.Count; i++)
            {
                MissionInstance mission = activeMissions[i];
                if (mission.targetRound == round)
                {
                    mission.startLife = gameController != null ? gameController.Life : mission.startLife;
                }
            }

            EvaluateMissions();
        }

        private void HandleRoundMissionSettlement(int round)
        {
            EvaluateMissions(true, round);
            RefillMissions();
            RefreshUi();
        }

        private void HandleRoundBoardPreparation(int round)
        {
            if (pendingMissionSupportSummons <= 0 || gameController == null)
            {
                return;
            }

            if (gameController.EmptySlotCount <= 0 || !gameController.TryGrantMissionSupportUnit())
            {
                missionSupportWaitingForSlot = true;
                gameController.RequestBanner("지원 유닛 대기: 빈 보드 슬롯 필요", new Color(1f, 0.78f, 0.30f), 2.4f);
                RefreshUi();
                return;
            }

            pendingMissionSupportSummons--;
            missionSupportWaitingForSlot = false;
            gameController.RequestBanner("미션 지원 유닛 도착!", new Color(0.48f, 1f, 0.72f), 2.2f);
            RefreshUi();
        }

        private void HandleMonsterKilled(MonsterUnit monster)
        {
            totalKills++;
            if (monster != null && monster.IsBoss)
            {
                totalBossKills++;
            }

            EvaluateMissions();
        }

        private void HandleMonsterEscaped(MonsterUnit monster)
        {
            EvaluateMissions();
        }

        private void HandleGameOver()
        {
            activeMissions.Clear();
            pendingMissionSupportSummons = 0;
            missionSupportWaitingForSlot = false;
            HideCompletionToast();
            RefreshUi();
        }

        private void TogglePanel()
        {
            SetPanelOpen(panelRoot == null || !panelRoot.activeSelf);
        }

        private void ClosePanel()
        {
            SetPanelOpen(false);
        }

        private void SetPanelOpen(bool open)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(open);
            }
        }

        private void RefreshUi()
        {
            RefreshSummary();
            RefreshPanel();
        }

        private void RefreshSummary()
        {
            if (summaryText == null)
            {
                return;
            }

            summaryText.text = "미션 " + activeMissions.Count + "/" + MaxActiveMissions + "  완료 " + completedMissionCount;
            if (pendingMissionSupportSummons > 0)
            {
                summaryText.text += missionSupportWaitingForSlot ? "  |  지원 유닛 대기" : "  |  지원 유닛 예약";
            }
            summaryText.color = activeMissions.Count > 0 ? activeMissions[0].accentColor : Color.white;
        }

        private void RefreshPanel()
        {
            if (panelHeaderText != null)
            {
                panelHeaderText.text = "자동 미션 보드";
            }

            if (activeCardRoot != null)
            {
                bool showActiveCard = activeMissions.Count == 0;
                activeCardRoot.SetActive(showActiveCard);
                if (showActiveCard)
                {
                    SetText(activeTitleText, "최근 완료");
                    string feed = recentCompletionFeed.Count > 0
                        ? string.Join("\n", recentCompletionFeed.ToArray())
                        : "조건을 만족하면 보상은 즉시 획득합니다.\n지원 유닛 보상은 다음 준비 단계에 도착합니다.";
                    SetText(activeDescriptionText, feed);
                    SetText(activeProgressText, "완료 " + completedMissionCount + "개  |  진행 " + activeMissions.Count + "개" + (pendingMissionSupportSummons > 0 ? "  |  지원 " + pendingMissionSupportSummons + "개" : string.Empty));
                }
            }

            int buttonCount = optionButtons != null ? optionButtons.Length : 0;
            for (int i = 0; i < buttonCount; i++)
            {
                bool show = i < activeMissions.Count;
                if (optionButtons[i] != null)
                {
                    optionButtons[i].gameObject.SetActive(show);
                    optionButtons[i].interactable = true;
                    SetChildText(optionButtons[i].transform, "PickLabel", "진행");
                }

                if (!show)
                {
                    continue;
                }

                MissionInstance mission = activeMissions[i];
                SetText(GetText(optionTitleTexts, i), mission.title);
                SetText(GetText(optionDescriptionTexts, i), mission.description + "\n" + GetProgressText(mission));
                SetText(GetText(optionRewardTexts, i), "클리어 즉시: " + mission.rewardText);

                Image accent = GetImage(optionAccentImages, i);
                if (accent != null)
                {
                    accent.color = mission.color;
                }
            }
        }

        private string GetProgressText(MissionInstance mission)
        {
            if (mission == null || gameController == null)
            {
                return string.Empty;
            }

            string deadline = mission.targetRound > 0 ? "  |  ROUND " + mission.targetRound + "까지" : string.Empty;
            switch (mission.kind)
            {
                case MissionKind.GoldReserve:
                    return Mathf.Min(gameController.Gold, mission.target) + " / " + mission.target + "G" + deadline;
                case MissionKind.PerfectDefense:
                    return "체력 손실 " + Mathf.Max(0, mission.startLife - gameController.Life) + " / 0" + deadline;
                case MissionKind.MergeRush:
                    return Mathf.Min(totalMerges - mission.startMerges, mission.target) + " / " + mission.target + " 합성" + deadline;
                case MissionKind.RoleCollector:
                    return CountDistinctRoles() + " / " + mission.target + " 역할" + deadline;
                case MissionKind.LeanDefense:
                    return "유닛 " + gameController.BoardUnitCount + " / " + mission.target + ", 손실 " + Mathf.Max(0, mission.startLife - gameController.Life) + " / " + mission.secondaryTarget + deadline;
                case MissionKind.BossPreparation:
                    return CountUnitsAtLeast(CharacterGrade.Legendary) + " / " + mission.target + " 전설+" + deadline;
                case MissionKind.SummonSprint:
                    return Mathf.Min(totalSummons - mission.startSummons, mission.target) + " / " + mission.target + " 소환" + deadline;
                case MissionKind.LastStandGambit:
                    return "HP " + gameController.Life + " / 7 이하 | 소환 " + Mathf.Max(0, totalSummons - mission.startSummons) + " / 2 | 유닛 " + gameController.BoardUnitCount + " / 2 | R3까지";
                case MissionKind.EmptySlotDiscipline:
                    return gameController.EmptySlotCount + " / " + mission.target + " 빈칸" + deadline;
                case MissionKind.RareUpgrade:
                    return CountUnitsAtLeast(CharacterGrade.Rare) + " / " + mission.target + " 레어+" + deadline;
                case MissionKind.LegendaryHunt:
                    return CountUnitsAtLeast(CharacterGrade.Legendary) + " / " + mission.target + " 전설+" + deadline;
                case MissionKind.MonsterHunter:
                    return Mathf.Min(totalKills - mission.startKills, mission.target) + " / " + mission.target + " 처치" + deadline;
                case MissionKind.BossSlayer:
                    return Mathf.Min(totalBossKills - mission.startBossKills, mission.target) + " / " + mission.target + " 보스 처치" + deadline;
                case MissionKind.NoSummonHold:
                    return "소환 " + (totalSummons - mission.startSummons) + " / 0, 손실 " + Mathf.Max(0, mission.startLife - gameController.Life) + " / " + mission.secondaryTarget + deadline;
                case MissionKind.KillStreak:
                    return Mathf.Min(totalKills - mission.startKills, mission.target) + " / " + mission.target + " 처치, 체력 손실 " + Mathf.Max(0, mission.startLife - gameController.Life) + " / 0" + deadline;
                case MissionKind.HighGradeForge:
                    CharacterGrade forgeGrade = (CharacterGrade)Mathf.Clamp(mission.secondaryTarget, (int)CharacterGrade.Normal, (int)CharacterGrade.Transcendent);
                    int forgeProgress = GetMergeResultsAtLeast(forgeGrade) - GetStartMergeResultsAtLeast(mission, forgeGrade);
                    return Mathf.Min(forgeProgress, mission.target) + " / " + mission.target + " " + CharacterGradeUtility.GetDisplayName(forgeGrade) + "+ 합성" + deadline;
                case MissionKind.SpendDownGambit:
                    return gameController.Gold + " / " + mission.target + "G 이하로 종료" + deadline;
                case MissionKind.UltimateRecipeChase:
                    return gameController.GetUltimateMergeStatus() + "  |  초월 합성 " + Mathf.Min(totalFinalMerges - mission.startFinalMerges, mission.target) + " / " + mission.target + deadline;
                case MissionKind.GradeRainbow:
                    return CountDistinctGrades() + " / " + mission.target + " 등급" + deadline;
                default:
                    return string.Empty;
            }
        }

        public static bool IsLastStandGambitConditionMet(int life, int summonsSinceMissionStart, int boardUnitCount, int currentRound)
        {
            return life > 0 && life <= 7 && currentRound <= 3 && summonsSinceMissionStart <= 2 && boardUnitCount <= 2;
        }

        private bool HasActiveMission(MissionKind kind)
        {
            for (int i = 0; i < activeMissions.Count; i++)
            {
                if (activeMissions[i] != null && activeMissions[i].kind == kind)
                {
                    return true;
                }
            }

            return false;
        }
        private int CountDistinctRoles()
        {
            DefenderUnit[] defenders = boardManager != null ? boardManager.GetAliveDefenders() : new DefenderUnit[0];
            HashSet<CharacterRole> roles = new HashSet<CharacterRole>();
            for (int i = 0; i < defenders.Length; i++)
            {
                if (defenders[i] != null)
                {
                    roles.Add(defenders[i].Role);
                }
            }

            return roles.Count;
        }

        private int CountUnitsAtLeast(CharacterGrade grade)
        {
            DefenderUnit[] defenders = boardManager != null ? boardManager.GetAliveDefenders() : new DefenderUnit[0];
            int count = 0;
            for (int i = 0; i < defenders.Length; i++)
            {
                if (defenders[i] != null && (int)defenders[i].Grade >= (int)grade)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountDistinctGrades()
        {
            DefenderUnit[] defenders = boardManager != null ? boardManager.GetAliveDefenders() : new DefenderUnit[0];
            HashSet<CharacterGrade> grades = new HashSet<CharacterGrade>();
            for (int i = 0; i < defenders.Length; i++)
            {
                if (defenders[i] != null)
                {
                    grades.Add(defenders[i].Grade);
                }
            }

            return grades.Count;
        }

        private int GetMergeResultsAtLeast(CharacterGrade grade)
        {
            if ((int)grade <= (int)CharacterGrade.Rare)
            {
                return totalRarePlusMerges;
            }

            if ((int)grade <= (int)CharacterGrade.Epic)
            {
                return totalEpicPlusMerges;
            }

            if ((int)grade <= (int)CharacterGrade.Legendary)
            {
                return totalLegendaryPlusMerges;
            }

            return totalFinalMerges;
        }

        private int GetStartMergeResultsAtLeast(MissionInstance mission, CharacterGrade grade)
        {
            if (mission == null)
            {
                return 0;
            }

            if ((int)grade <= (int)CharacterGrade.Rare)
            {
                return mission.startRarePlusMerges;
            }

            if ((int)grade <= (int)CharacterGrade.Epic)
            {
                return mission.startEpicPlusMerges;
            }

            if ((int)grade <= (int)CharacterGrade.Legendary)
            {
                return mission.startLegendaryPlusMerges;
            }

            return mission.startFinalMerges;
        }

        private int GetNextTier(MissionKind kind)
        {
            return GetCompletedFamilyLevel(kind);
        }

        private int GetCompletedFamilyLevel(MissionKind kind)
        {
            int level;
            return completedFamilyLevels.TryGetValue(kind, out level) ? level : 0;
        }

        private bool IsMissionActive(string key)
        {
            for (int i = 0; i < activeMissions.Count; i++)
            {
                if (activeMissions[i] != null && activeMissions[i].Key == key)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsRecentlyExpired(string key)
        {
            int round = gameController != null ? gameController.CurrentRound : 0;
            int lockedUntilRound;
            return recentlyExpiredKeys.TryGetValue(key, out lockedUntilRound) && lockedUntilRound > round;
        }

        private void ClearExpiredCooldowns()
        {
            int round = gameController != null ? gameController.CurrentRound : 0;
            List<string> expired = null;
            foreach (KeyValuePair<string, int> entry in recentlyExpiredKeys)
            {
                if (entry.Value <= round)
                {
                    if (expired == null)
                    {
                        expired = new List<string>();
                    }

                    expired.Add(entry.Key);
                }
            }

            if (expired == null)
            {
                return;
            }

            for (int i = 0; i < expired.Count; i++)
            {
                recentlyExpiredKeys.Remove(expired[i]);
            }
        }

        private int GetNextBossRound(int round)
        {
            int next = Mathf.Max(10, ((round / 10) + 1) * 10);
            return next <= round ? round + 10 : next;
        }

        private int GetRoundsUntilNextBoss()
        {
            int round = gameController != null ? gameController.CurrentRound : 0;
            return Mathf.Max(0, GetNextBossRound(round) - round);
        }

        private void AddCompletionFeed(string message)
        {
            recentCompletionFeed.Insert(0, message);
            while (recentCompletionFeed.Count > 3)
            {
                recentCompletionFeed.RemoveAt(recentCompletionFeed.Count - 1);
            }
        }

        private void ShowCompletionToast(MissionInstance mission, string rewardSummary)
        {
            if (completionToastRoot == null || mission == null)
            {
                return;
            }

            ShowToast("미션 완료!", rewardSummary);
        }


        private void ShowToast(string title, string reward)
        {
            SetText(completionToastTitleText, title);
            SetText(completionToastRewardText, reward);
            completionToastRoot.SetActive(true);
            toastTimer = CompletionToastDuration;

            if (completionToastGroup != null)
            {
                completionToastGroup.alpha = 1f;
            }

            RectTransform rect = completionToastRoot.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
            }
        }

        private void UpdateCompletionToast()
        {
            if (completionToastRoot == null || !completionToastRoot.activeSelf)
            {
                return;
            }

            toastTimer -= Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(toastTimer / CompletionToastDuration);
            float alpha = Mathf.Min(Mathf.Clamp01((CompletionToastDuration - toastTimer) / 0.18f), Mathf.Clamp01(normalized / 0.2f));

            if (completionToastGroup != null)
            {
                completionToastGroup.alpha = alpha;
            }

            RectTransform rect = completionToastRoot.GetComponent<RectTransform>();
            if (rect != null)
            {
                float pop = Mathf.Sin((1f - normalized) * Mathf.PI);
                rect.localScale = Vector3.one * Mathf.Lerp(0.98f, 1.08f, pop);
            }

            if (toastTimer <= 0f)
            {
                HideCompletionToast();
            }
        }

        private void HideCompletionToast()
        {
            toastTimer = 0f;
            if (completionToastGroup != null)
            {
                completionToastGroup.alpha = 0f;
            }

            if (completionToastRoot != null)
            {
                completionToastRoot.SetActive(false);
            }
        }

        private string ToRoman(int value)
        {
            if (value <= 1) return "I";
            if (value == 2) return "II";
            if (value == 3) return "III";
            if (value == 4) return "IV";
            if (value == 5) return "V";
            return value.ToString();
        }

        private Text GetText(Text[] texts, int index)
        {
            return texts != null && index >= 0 && index < texts.Length ? texts[index] : null;
        }

        private Image GetImage(Image[] images, int index)
        {
            return images != null && index >= 0 && index < images.Length ? images[index] : null;
        }

        private void SetText(Text target, string value)
        {
            if (target != null && target.text != value)
            {
                target.text = value;
            }
        }

        private void SetChildText(Transform root, string childName, string value)
        {
            if (root == null)
            {
                return;
            }

            Transform child = root.Find(childName);
            Text text = child != null ? child.GetComponent<Text>() : null;
            SetText(text, value);
        }
    }
}
