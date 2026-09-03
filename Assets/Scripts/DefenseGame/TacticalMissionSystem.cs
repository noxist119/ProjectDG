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
            public string contractGrade;
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
            public int startGold;
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

        private const int MaxMissionOffers = 3;
        private const float CompletionToastHoldDuration = 3f;
        private const float CompletionToastFadeDuration = 0.22f;

        // Before selection this contains the three offers; after selection it contains one active mission.
        private readonly List<MissionInstance> activeMissions = new List<MissionInstance>();
        private bool missionSelected;
        private bool offerRefreshQueued;
        // Unselected offers belong to one preparation round only. A selected contract owns
        // its deadline independently and is never replaced by this draft marker.
        private int offersGeneratedForRound = int.MinValue;
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
        private int missionCursor; // Legacy save field; the active draft now uses the Mission RNG channel.
        private string lastOfferSignature = string.Empty;
        private int completedMissionCount;
        private float toastTimer;
        private bool subscribed;
        private bool resolvingMission;
        private int pendingMissionSupportSummons;
        private bool runStarted;

        public int PendingMissionSupportSummons => pendingMissionSupportSummons;
        // Compatibility surface for older smoke callers: this now means an opening three-choice draft exists, not a fixed trio.
        public bool HasInitialStrategyFork => !missionSelected && gameController != null && gameController.CurrentRound <= 0 && activeMissions.Count == MaxMissionOffers;
        public bool HasActiveMissionSelection => missionSelected;
        public bool IsChoicePanelOpen => !missionSelected && panelRoot != null && panelRoot.activeSelf;
        public int MissionOfferCount => missionSelected ? 0 : activeMissions.Count;
        public int CompletedMissionCount => completedMissionCount;
        public bool HasPendingMissionOffers => !missionSelected && activeMissions.Count > 0;

        // Read-only automation/telemetry surface. This deliberately exposes broad intent,
        // rather than the private MissionKind enum, so external test policies remain stable
        // when individual missions are added or renamed.
        public string GetMissionOfferAutomationTag(int index)
        {
            if (missionSelected || index < 0 || index >= activeMissions.Count || activeMissions[index] == null)
            {
                return string.Empty;
            }

            switch (GetMissionCategory(activeMissions[index].kind))
            {
                case "TEMPO": return "growth";
                case "GREED": return "economy";
                case "SAFE": return "safety";
                default: return "balanced";
            }
        }

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
            lastOfferSignature = string.Empty;
            completedMissionCount = 0;
            toastTimer = 0f;
            resolvingMission = false;
            pendingMissionSupportSummons = 0;
            runStarted = false;
            missionSelected = false;
            offerRefreshQueued = false;
            offersGeneratedForRound = int.MinValue;
        }

        private void WireUi()
        {
            if (summaryButton != null)
            {
                summaryButton.onClick.RemoveListener(TogglePanel);
                summaryButton.onClick.AddListener(TogglePanel);
                SetChildText(summaryButton.transform, "MissionOpenHint", "\ubcf4\uae30");
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePanel);
                closeButton.onClick.AddListener(ClosePanel);
                SetChildText(closeButton.transform, "Text", "\ub098\uc911\uc5d0");
            }

            if (optionButtons == null)
            {
                return;
            }

            for (int i = 0; i < optionButtons.Length; i++)
            {
                Button button = optionButtons[i];
                if (button == null)
                {
                    continue;
                }

                int optionIndex = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => TrySelectMission(optionIndex));
                SetChildText(button.transform, "PickLabel", "\uc120\ud0dd");
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

        private bool CanShowMissionOffers()
        {
            return gameController != null && !gameController.IsRoundRunning;
        }

        private void RefillMissions()
        {
            if (gameController == null || missionSelected || activeMissions.Count > 0 || !CanShowMissionOffers())
            {
                return;
            }

            ClearExpiredCooldowns();
            int round = gameController.CurrentRound;
            int minimumValid = GetMissionBracketMinimum(round);
            List<MissionKind> bracketPool = BuildCandidatePool(round);
            List<MissionKind> eligible = new List<MissionKind>();
            for (int i = 0; i < bracketPool.Count; i++)
            {
                if (IsMissionEligibleForOffer(bracketPool[i]))
                {
                    eligible.Add(bracketPool[i]);
                }
            }

            // A bracket never borrows a late or early mission just to fill a row. The authored pools
            // contain more than the stated minimum; this guard simply makes an infeasible state explicit.
            if (eligible.Count < Mathf.Min(MaxMissionOffers, minimumValid))
            {
                offerRefreshQueued = false;
                return;
            }

            string signature = string.Empty;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                activeMissions.Clear();
                List<MissionKind> remaining = new List<MissionKind>(eligible);
                HashSet<string> usedCategories = new HashSet<string>();
                while (activeMissions.Count < MaxMissionOffers && remaining.Count > 0)
                {
                    List<MissionKind> preferred = new List<MissionKind>();
                    for (int i = 0; i < remaining.Count; i++)
                    {
                        if (!usedCategories.Contains(GetMissionCategory(remaining[i])))
                        {
                            preferred.Add(remaining[i]);
                        }
                    }

                    List<MissionKind> source = preferred.Count > 0 ? preferred : remaining;
                    int selectedIndex = gameController.RunContentRandom.Range(
                        RunContentRandomChannel.Mission, 0, source.Count, "mission.draft.pick");
                    MissionKind kind = source[selectedIndex];
                    remaining.Remove(kind);
                    usedCategories.Add(GetMissionCategory(kind));
                    MissionInstance mission = CreateMission(kind, GetNextTier(kind));
                    // Generation has two gates: the pool gate above prevents obviously
                    // invalid families, and this authored-card gate verifies its actual
                    // target/deadline against the current board and economy.
                    if (mission != null && IsMissionFeasibleForCurrentRun(mission))
                    {
                        activeMissions.Add(mission);
                        gameController.RunContentRandom.RecordOutcome(RunContentRandomChannel.Mission, "mission.offer", mission.kind.ToString());
                    }
                }

                signature = BuildOfferSignature();
                if (activeMissions.Count == MaxMissionOffers && signature != lastOfferSignature)
                {
                    break;
                }
            }

            if (activeMissions.Count == MaxMissionOffers)
            {
                lastOfferSignature = signature;
                offersGeneratedForRound = round;
            }
            offerRefreshQueued = false;
        }

        private string BuildOfferSignature()
        {
            List<string> ids = new List<string>(activeMissions.Count);
            for (int i = 0; i < activeMissions.Count; i++)
            {
                if (activeMissions[i] != null)
                {
                    ids.Add(activeMissions[i].kind.ToString());
                }
            }
            ids.Sort(System.StringComparer.Ordinal);
            return string.Join("|", ids.ToArray());
        }

        private int GetMissionBracketMinimum(int round)
        {
            return round < 10 ? 8 : round < 30 ? 10 : 8;
        }

        private void AddOffer(MissionKind kind, int tier, bool initial)
        {
            MissionInstance mission = CreateMission(kind, tier);
            if (mission == null)
            {
                return;
            }

            if (initial)
            {
                ConfigureMissionForSelection(mission, true);
            }

            activeMissions.Add(mission);
            gameController?.RunContentRandom.RecordOutcome(RunContentRandomChannel.Mission, "mission.offer", mission.kind.ToString());
        }

        public bool TrySelectMission(int index)
        {
            if (missionSelected || !CanShowMissionOffers() || index < 0 || index >= activeMissions.Count)
            {
                return false;
            }

            MissionInstance selected = activeMissions[index];
            if (selected == null)
            {
                return false;
            }

            // Board/economy can change while the optional overlay is open. Do not arm a
            // card that became impossible after it was drafted; replace the whole draft.
            if (!IsMissionFeasibleForCurrentRun(selected))
            {
                activeMissions.Clear();
                offersGeneratedForRound = int.MinValue;
                RefillMissions();
                RefreshUi();
                gameController?.NotifyPostRoundChoiceStateChanged();
                return false;
            }

            ArmSelectedMission(selected);
            activeMissions.Clear();
            activeMissions.Add(selected);
            gameController?.RunContentRandom.RecordOutcome(RunContentRandomChannel.Mission, "mission.selected", selected.kind.ToString());
            missionSelected = true;
            offerRefreshQueued = false;
            SetPanelOpen(false);
            RefreshUi();
            gameController?.NotifyPostRoundChoiceStateChanged();
            return true;
        }

        private static List<MissionKind> BuildCandidatePool(int round)
        {
            if (round < 10)
            {
                return new List<MissionKind>
                {
                    MissionKind.PerfectDefense, MissionKind.SummonSprint, MissionKind.HighGradeForge,
                    MissionKind.MergeRush, MissionKind.RoleCollector, MissionKind.LeanDefense,
                    MissionKind.EmptySlotDiscipline, MissionKind.RareUpgrade, MissionKind.MonsterHunter,
                    MissionKind.KillStreak, MissionKind.GoldReserve
                };
            }

            if (round < 20)
            {
                return new List<MissionKind>
                {
                    MissionKind.PerfectDefense, MissionKind.SummonSprint, MissionKind.MergeRush,
                    MissionKind.RoleCollector, MissionKind.LeanDefense, MissionKind.BossPreparation,
                    MissionKind.EmptySlotDiscipline, MissionKind.RareUpgrade, MissionKind.LegendaryHunt,
                    MissionKind.MonsterHunter, MissionKind.NoSummonHold, MissionKind.KillStreak,
                    MissionKind.HighGradeForge, MissionKind.SpendDownGambit, MissionKind.GradeRainbow
                };
            }

            if (round < 30)
            {
                return new List<MissionKind>
                {
                    MissionKind.PerfectDefense, MissionKind.MergeRush, MissionKind.RoleCollector,
                    MissionKind.BossPreparation, MissionKind.RareUpgrade, MissionKind.LegendaryHunt,
                    MissionKind.MonsterHunter, MissionKind.BossSlayer, MissionKind.NoSummonHold,
                    MissionKind.KillStreak, MissionKind.HighGradeForge, MissionKind.SpendDownGambit,
                    MissionKind.UltimateRecipeChase, MissionKind.GradeRainbow
                };
            }

            return new List<MissionKind>
            {
                MissionKind.BossPreparation, MissionKind.LegendaryHunt, MissionKind.MonsterHunter,
                MissionKind.BossSlayer, MissionKind.NoSummonHold, MissionKind.KillStreak,
                MissionKind.HighGradeForge, MissionKind.SpendDownGambit, MissionKind.UltimateRecipeChase,
                MissionKind.GradeRainbow, MissionKind.RoleCollector, MissionKind.MergeRush
            };
        }

        private static string GetMissionCategory(MissionKind kind)
        {
            switch (kind)
            {
                case MissionKind.PerfectDefense: return "SAFE";
                case MissionKind.SummonSprint:
                case MissionKind.MergeRush:
                case MissionKind.SpendDownGambit: return "TEMPO";
                case MissionKind.GoldReserve:
                case MissionKind.NoSummonHold:
                case MissionKind.LeanDefense: return "GREED";
                default: return "BUILD";
            }
        }

        public static int GetDraftPoolCountForValidation(int round)
        {
            return BuildCandidatePool(round).Count;
        }

        // Editor smoke uses the same candidate pools and Mission-channel draw rule to check seed repeatability.
        public static string[] BuildDraftForValidation(int round, RunContentRandomService random)
        {
            List<MissionKind> remaining = BuildCandidatePool(round);
            List<string> result = new List<string>(MaxMissionOffers);
            HashSet<string> categories = new HashSet<string>();
            while (result.Count < MaxMissionOffers && remaining.Count > 0)
            {
                List<MissionKind> preferred = new List<MissionKind>();
                for (int index = 0; index < remaining.Count; index++)
                {
                    if (!categories.Contains(GetMissionCategory(remaining[index])))
                    {
                        preferred.Add(remaining[index]);
                    }
                }

                List<MissionKind> source = preferred.Count > 0 ? preferred : remaining;
                int selected = random.Range(RunContentRandomChannel.Mission, 0, source.Count, "mission.draft.pick");
                MissionKind kind = source[selected];
                remaining.Remove(kind);
                categories.Add(GetMissionCategory(kind));
                result.Add(kind.ToString());
            }
            return result.ToArray();
        }

        // Pure validation surface for Smoke. It mirrors the pre-generation gate without
        // constructing runtime units or spending resources.
        public static string[] BuildFeasibleDraftForValidation(int round, int gold, int summonCost, int boardUnitCount, int emptySlotCount, int life, RunContentRandomService random)
        {
            List<MissionKind> remaining = BuildCandidatePool(round);
            remaining.RemoveAll(kind => !IsMissionKindFeasibleForValidation(kind, round, gold, summonCost, boardUnitCount, emptySlotCount, life));
            List<string> result = new List<string>(MaxMissionOffers);
            HashSet<string> categories = new HashSet<string>();
            while (result.Count < MaxMissionOffers && remaining.Count > 0)
            {
                List<MissionKind> preferred = remaining.FindAll(kind => !categories.Contains(GetMissionCategory(kind)));
                List<MissionKind> source = preferred.Count > 0 ? preferred : remaining;
                MissionKind selected = source[random.Range(RunContentRandomChannel.Mission, 0, source.Count, "mission.validation.pick")];
                remaining.Remove(selected);
                categories.Add(GetMissionCategory(selected));
                result.Add(selected.ToString());
            }
            return result.ToArray();
        }

        public static bool IsMissionKindFeasibleForValidation(string missionKindName, int round, int gold, int summonCost, int boardUnitCount, int emptySlotCount, int life)
        {
            return System.Enum.TryParse(missionKindName, out MissionKind kind) &&
                   IsMissionKindFeasibleForValidation(kind, round, gold, summonCost, boardUnitCount, emptySlotCount, life);
        }

        private static bool IsMissionKindFeasibleForValidation(MissionKind kind, int round, int gold, int summonCost, int boardUnitCount, int emptySlotCount, int life)
        {
            int affordableSummons = Mathf.Max(0, gold) / Mathf.Max(1, summonCost);
            switch (kind)
            {
                case MissionKind.NoSummonHold:
                    return round >= 10 && boardUnitCount >= 6;
                case MissionKind.HighGradeForge:
                    return round >= 5 && boardUnitCount + affordableSummons >= 3;
                case MissionKind.PerfectDefense:
                    return boardUnitCount + Mathf.Min(Mathf.Max(0, emptySlotCount), affordableSummons) >= 3;
                case MissionKind.SummonSprint:
                    return affordableSummons >= 3 && emptySlotCount > 0;
                case MissionKind.MergeRush:
                    return boardUnitCount >= 2 || affordableSummons >= 2;
                case MissionKind.LeanDefense:
                    return boardUnitCount >= 4;
                case MissionKind.EmptySlotDiscipline:
                    return boardUnitCount >= 2 && emptySlotCount + Mathf.Max(0, boardUnitCount - 1) >= 2;
                case MissionKind.RareUpgrade:
                    return boardUnitCount + affordableSummons >= 3;
                case MissionKind.LegendaryHunt:
                    return round >= 10 && boardUnitCount >= 4;
                case MissionKind.MonsterHunter:
                    return boardUnitCount > 0;
                case MissionKind.KillStreak:
                    return boardUnitCount >= 2 && life > 1;
                case MissionKind.BossPreparation:
                case MissionKind.BossSlayer:
                    return boardUnitCount >= 3;
                case MissionKind.UltimateRecipeChase:
                    return round >= 20 && boardUnitCount >= 5;
                case MissionKind.LastStandGambit:
                    return round == 0 && life <= 7 && boardUnitCount <= 2;
                default:
                    return true;
            }
        }

        private bool IsMissionEligibleForOffer(MissionKind kind)
        {
            if (gameController == null)
            {
                return false;
            }

            int goldThreshold = Mathf.Max(30, gameController.SummonCost * 3);
            if (!IsMissionKindFeasibleForValidation(kind, gameController.CurrentRound, gameController.Gold, gameController.SummonCost, gameController.BoardUnitCount, gameController.EmptySlotCount, gameController.Life))
            {
                return false;
            }
            switch (kind)
            {
                case MissionKind.GoldReserve:
                case MissionKind.SpendDownGambit:
                    return gameController.Gold >= goldThreshold;
                case MissionKind.RoleCollector:
                    return CountDistinctRoles() < 5;
                case MissionKind.GradeRainbow:
                    return CountDistinctGrades() < 4;
                case MissionKind.LeanDefense:
                    return gameController.BoardUnitCount >= 5;
                case MissionKind.BossPreparation:
                    return GetRoundsUntilNextBoss() >= 2 && CountUnitsAtLeast(CharacterGrade.Legendary) < 2;
                case MissionKind.HighGradeForge:
                    return gameController.CurrentRound >= 5;
                case MissionKind.NoSummonHold:
                    return gameController.CurrentRound >= 10 && gameController.BoardUnitCount >= 6;
                case MissionKind.UltimateRecipeChase:
                    return gameController.CurrentRound >= 20 && GetRoundsUntilNextBoss() >= 2 && boardManager != null && boardManager.HasAnyUltimateRecipeProgress();
                default:
                    return true;
            }
        }

        private bool IsMissionFeasibleForCurrentRun(MissionInstance mission)
        {
            if (mission == null || gameController == null)
            {
                return false;
            }

            int round = gameController.CurrentRound;
            int roundsRemaining = mission.targetRound - round;
            if (mission.targetRound <= round || roundsRemaining <= 0 || mission.targetRound < mission.earliestCompleteRound)
            {
                return false;
            }

            int summonCost = Mathf.Max(1, gameController.SummonCost);
            int affordableNow = Mathf.Max(0, gameController.Gold) / summonCost;
            int conservativeFutureSummons = Mathf.Max(0, roundsRemaining - 1) * 2;
            int availableSummons = affordableNow + conservativeFutureSummons;
            int boardCount = Mathf.Max(0, gameController.BoardUnitCount);
            int emptySlots = Mathf.Max(0, gameController.EmptySlotCount);
            int potentialBoardCount = boardCount + Mathf.Min(emptySlots, availableSummons);

            if (!IsMissionKindFeasibleForValidation(mission.kind, round, gameController.Gold, summonCost, boardCount, emptySlots, gameController.Life))
            {
                return false;
            }

            switch (mission.kind)
            {
                case MissionKind.PerfectDefense:
                    return potentialBoardCount >= mission.target;
                case MissionKind.SummonSprint:
                    return availableSummons >= mission.target && emptySlots > 0;
                case MissionKind.MergeRush:
                    return boardCount >= mission.target * 2 || availableSummons >= mission.target * 2;
                case MissionKind.EmptySlotDiscipline:
                    return emptySlots + Mathf.Max(0, boardCount - 1) >= mission.target && (boardCount >= 2 || availableSummons >= 2);
                case MissionKind.HighGradeForge:
                    return roundsRemaining >= 3 && boardCount + availableSummons >= 3;
                case MissionKind.RareUpgrade:
                    return potentialBoardCount >= mission.target;
                case MissionKind.LegendaryHunt:
                    return roundsRemaining >= 4 && boardCount >= 4;
                case MissionKind.MonsterHunter:
                    return roundsRemaining >= 2 && boardCount > 0;
                case MissionKind.KillStreak:
                    return roundsRemaining >= 2 && boardCount >= 2 && gameController.Life > 1;
                case MissionKind.GoldReserve:
                    return gameController.Gold + roundsRemaining * Mathf.Max(summonCost * 2, 20) >= mission.target;
                case MissionKind.NoSummonHold:
                    return round >= 10 && boardCount >= 6;
                default:
                    return true;
            }
        }

        // A drafted card is already authored with its target, deadline, grade, and reward. Selection only
        // starts its telemetry window; it must never downgrade the visible contract into a legacy mini mission.
        private void ArmSelectedMission(MissionInstance mission)
        {
            if (mission == null || gameController == null)
            {
                return;
            }

            mission.startRound = gameController.CurrentRound;
            mission.startLife = gameController.Life;
            mission.startGold = gameController.Gold;
            mission.startSummons = totalSummons;
            mission.startMerges = totalMerges;
            mission.startRarePlusMerges = totalRarePlusMerges;
            mission.startEpicPlusMerges = totalEpicPlusMerges;
            mission.startLegendaryPlusMerges = totalLegendaryPlusMerges;
            mission.startFinalMerges = totalFinalMerges;
            mission.startKills = totalKills;
            mission.startBossKills = totalBossKills;
            mission.description = BuildConditionDescription(mission);
            mission.rewardText = BuildRewardText(mission);
        }
        private void ConfigureMissionForSelection(MissionInstance mission, bool initialOffer)
        {
            if (mission == null || gameController == null)
            {
                return;
            }

            int round = gameController.CurrentRound;
            mission.startRound = round;
            mission.startLife = gameController.Life;
            mission.startGold = gameController.Gold;
            mission.startSummons = totalSummons;
            mission.startMerges = totalMerges;
            mission.startRarePlusMerges = totalRarePlusMerges;
            mission.startEpicPlusMerges = totalEpicPlusMerges;
            mission.startLegendaryPlusMerges = totalLegendaryPlusMerges;
            mission.startFinalMerges = totalFinalMerges;
            mission.startKills = totalKills;
            mission.startBossKills = totalBossKills;
            mission.earliestCompleteRound = round + 1;
            mission.roundGoldBonus = 0;
            mission.summonDiscount = 0f;
            mission.supportSummonReward = 0;
            mission.rouletteGoldMin = 0;
            mission.rouletteGoldMax = 0;
            mission.jackpotGold = 0;
            mission.jackpotChance = 0f;
            mission.expiresOnRoundStart = false;

            int summonCost = Mathf.Max(1, gameController.SummonCost);
            switch (mission.kind)
            {
                case MissionKind.PerfectDefense:
                    mission.targetRound = round + 1;
                    mission.target = 0;
                    mission.goldReward = initialOffer ? 8 : Mathf.Clamp(8 + summonCost / 2, 10, 28);
                    break;
                case MissionKind.SummonSprint:
                    mission.targetRound = initialOffer ? 2 : round + 2;
                    mission.target = initialOffer ? 3 : (round <= 10 ? 3 : round <= 25 ? 4 : 5);
                    mission.goldReward = initialOffer ? 12 : Mathf.Clamp(Mathf.RoundToInt(mission.target * summonCost * 0.38f), 10, 42);
                    break;
                case MissionKind.LastStandGambit:
                    // This initial contract settles at the end of R2, as its player-facing condition states.
                    mission.targetRound = 2;
                    mission.target = 2;
                    mission.secondaryTarget = 7;
                    mission.goldReward = 18;
                    break;
                case MissionKind.GoldReserve:
                    mission.targetRound = round + 1;
                    mission.target = Mathf.CeilToInt(mission.startGold * 0.75f);
                    mission.goldReward = Mathf.Clamp(8 + summonCost, 14, 35);
                    break;
                case MissionKind.MergeRush:
                    mission.targetRound = round + 2;
                    mission.target = round < 10 ? 1 : round < 25 ? 2 : 3;
                    mission.goldReward = Mathf.Clamp(Mathf.RoundToInt(mission.target * summonCost * 0.55f), 12, 48);
                    break;
                case MissionKind.RoleCollector:
                    mission.targetRound = round + 2;
                    mission.target = Mathf.Min(5, CountDistinctRoles() + 1);
                    mission.goldReward = Mathf.Clamp(10 + summonCost, 14, 34);
                    break;
                case MissionKind.LeanDefense:
                    mission.targetRound = round + 1;
                    mission.target = Mathf.Max(3, gameController.BoardUnitCount - 2);
                    mission.secondaryTarget = 2;
                    mission.goldReward = Mathf.Clamp(13 + summonCost, 18, 40);
                    mission.rouletteGoldMin = 4;
                    mission.rouletteGoldMax = 10 + summonCost / 2;
                    break;
                case MissionKind.BossPreparation:
                    mission.targetRound = GetNextBossRound(round);
                    mission.target = Mathf.Min(2, CountUnitsAtLeast(CharacterGrade.Legendary) + 1);
                    mission.goldReward = Mathf.Clamp(14 + summonCost, 20, 48);
                    break;
                case MissionKind.NoSummonHold:
                    mission.targetRound = round + 1;
                    mission.secondaryTarget = round < 20 ? 1 : 2;
                    mission.goldReward = Mathf.Clamp(8 + summonCost / 2, 12, 28);
                    break;
                case MissionKind.HighGradeForge:
                    mission.targetRound = round + 4;
                    mission.target = 1;
                    mission.secondaryTarget = (int)(round < 10 ? CharacterGrade.Rare : round < 20 ? CharacterGrade.Epic : round < 35 ? CharacterGrade.Legendary : CharacterGrade.Mythic);
                    mission.goldReward = Mathf.Clamp(18 + summonCost, 24, 58);
                    mission.rouletteGoldMin = 6;
                    mission.rouletteGoldMax = 16 + summonCost / 2;
                    mission.jackpotChance = 0.12f;
                    mission.jackpotGold = 18 + summonCost;
                    break;
                case MissionKind.SpendDownGambit:
                    mission.targetRound = round + 1;
                    mission.target = Mathf.FloorToInt(mission.startGold * 0.35f);
                    mission.goldReward = Mathf.Clamp(10 + summonCost, 16, 40);
                    mission.rouletteGoldMin = 5;
                    mission.rouletteGoldMax = 15 + summonCost / 2;
                    mission.jackpotChance = 0.10f;
                    mission.jackpotGold = 16 + summonCost;
                    break;
                case MissionKind.UltimateRecipeChase:
                    mission.targetRound = GetNextBossRound(round);
                    mission.target = 1;
                    mission.goldReward = Mathf.Clamp(22 + summonCost, 30, 64);
                    mission.rouletteGoldMin = 8;
                    mission.rouletteGoldMax = 20 + summonCost / 2;
                    mission.jackpotChance = 0.15f;
                    mission.jackpotGold = 22 + summonCost;
                    break;
                case MissionKind.GradeRainbow:
                    mission.targetRound = round + 2;
                    mission.target = Mathf.Min(4, CountDistinctGrades() + 1);
                    mission.goldReward = Mathf.Clamp(11 + summonCost, 16, 36);
                    break;
            }

            mission.description = BuildConditionDescription(mission);
            mission.rewardText = BuildRewardText(mission);
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
                    mission.secondaryTarget = Mathf.Min(2, tier / 2);
                    mission.targetRound = round + 3 + Mathf.Min(2, tier / 2);
                    mission.goldReward = 42 + tier * 14;
                    mission.title = "골드 창고 " + ToRoman(displayTier);
                    mission.description = "라운드가 오기 전까지 목표 골드를 보유하세요. 소환을 참을수록 보상이 커집니다.";
                    mission.rewardText = "+" + mission.goldReward + "골드";
                    mission.color = new Color(1f, 0.76f, 0.22f);
                    mission.expiresOnRoundStart = true;
                    break;
                case MissionKind.PerfectDefense:
                    mission.target = Mathf.Clamp(3 + tier / 2, 3, 5);
                    mission.targetRound = round + 1;
                    mission.goldReward = 34 + tier * 10;
                    mission.roundGoldBonus = 1 + tier / 2;
                    mission.title = "무결 방어 " + ToRoman(displayTier);
                    mission.description = "필요 전력을 갖춘 뒤 다음 라운드를 체력 손실 없이 막아내세요.";
                    mission.rewardText = "+" + mission.goldReward + "골드, 라운드 보너스 +" + mission.roundGoldBonus;
                    mission.color = new Color(0.42f, 1f, 0.72f);
                    break;
                case MissionKind.MergeRush:
                    mission.target = 1 + Mathf.Min(4, tier + round / 7);
                    mission.secondaryTarget = 1 + Mathf.Min(2, tier / 2);
                    mission.targetRound = round + 2;
                    mission.earliestCompleteRound = round;
                    mission.goldReward = 36 + tier * 12;
                    mission.title = "합성 러시 " + ToRoman(displayTier);
                    mission.description = "제한 라운드 안에 합성하고, 레어 이상 결과까지 만들어 성장 속도를 끌어올리세요.";
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
                    mission.secondaryTarget = Mathf.Min(2, tier / 2);
                    mission.targetRound = round + 2;
                    mission.earliestCompleteRound = round;
                    mission.goldReward = 26 + tier * 9;
                    mission.title = "소환 스퍼트 " + ToRoman(displayTier);
                    mission.description = "빠르게 전장을 채우되, 정해진 피해 안에서 다음 전투를 버텨내세요.";
                    mission.rewardText = "+" + mission.goldReward + "골드";
                    mission.color = new Color(0.30f, 0.76f, 1f);
                    break;
                case MissionKind.LastStandGambit:
                    mission.targetRound = 4;
                    mission.earliestCompleteRound = 0;
                    mission.goldReward = 18;
                    mission.supportSummonReward = 0;
                    mission.title = "배수의 진";
                    mission.description = "R3까지 HP 7 이하를 감수하고 소환 2회·유닛 2기 이하를 유지하세요.";
                    mission.rewardText = "+18골드";
                    mission.color = new Color(1f, 0.38f, 0.28f);
                    mission.expiresOnRoundStart = true;
                    break;
                case MissionKind.EmptySlotDiscipline:
                    mission.target = Mathf.Clamp(2 + tier, 2, 4);
                    mission.secondaryTarget = 1 + Mathf.Min(2, tier / 2);
                    mission.targetRound = round + 1;
                    mission.goldReward = 38 + tier * 11;
                    mission.title = "빈칸 운영 " + ToRoman(displayTier);
                    mission.description = "다음 라운드 종료 시 빈 슬롯을 남기면서 레어 이상 합성도 달성하세요.";
                    mission.rewardText = "+" + mission.goldReward + "골드";
                    mission.color = new Color(0.66f, 0.92f, 1f);
                    break;
                case MissionKind.RareUpgrade:
                    mission.target = 2 + Mathf.Min(4, tier);
                    mission.secondaryTarget = Mathf.Clamp(2 + tier / 2, 2, 4);
                    mission.targetRound = round + 4;
                    mission.goldReward = 32 + tier * 11;
                    mission.title = "레어 라인업 " + ToRoman(displayTier);
                    mission.description = "레어 이상 유닛과 역할 조합을 함께 확보해 전투 안정성을 올리세요.";
                    mission.rewardText = "+" + mission.goldReward + "골드";
                    mission.color = new Color(0.25f, 0.62f, 1f);
                    break;
                case MissionKind.LegendaryHunt:
                    mission.target = 1 + tier / 2;
                    mission.secondaryTarget = 1;
                    mission.targetRound = round + 5;
                    mission.goldReward = 58 + tier * 18;
                    mission.roundGoldBonus = 1 + tier / 2;
                    mission.title = "전설 탐색 " + ToRoman(displayTier);
                    mission.description = "전설 이상 유닛을 만들고 에픽 이상 합성 성과까지 남기세요.";
                    mission.rewardText = "+" + mission.goldReward + "골드, 라운드 보너스 +" + mission.roundGoldBonus;
                    mission.color = new Color(1f, 0.68f, 0.20f);
                    break;
                case MissionKind.MonsterHunter:
                    mission.target = 12 + round * 3 + tier * 7;
                    mission.secondaryTarget = Mathf.Min(2, tier / 2);
                    mission.targetRound = round + 2;
                    mission.earliestCompleteRound = round;
                    mission.goldReward = 34 + tier * 10;
                    mission.title = "몬스터 사냥 " + ToRoman(displayTier);
                    mission.description = "정해진 피해 안에서 제한 처치 수를 달성해 추가 골드를 받으세요.";
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

            mission.description = BuildConditionDescription(mission);
            mission.contractGrade = GetContractGrade(mission);
            ApplyRewardPacing(mission);
            mission.title = "[" + mission.contractGrade + "] " + mission.title;
            mission.accentColor = Color.Lerp(mission.color, Color.white, 0.24f);
            return mission;
        }

        private string BuildConditionDescription(MissionInstance mission)
        {
            if (mission == null)
            {
                return string.Empty;
            }

            switch (mission.kind)
            {
                case MissionKind.GoldReserve:
                    return $"R{mission.targetRound} \uc2dc\uc791 \uc804\uae4c\uc9c0 {mission.target}G \uc774\uc0c1\uc744 \ubcf4\uc720\ud558\uace0 HP \uc190\uc2e4 {mission.secondaryTarget} \uc774\ud558\ub85c \ubc84\ud2f0\uc138\uc694.";
                case MissionKind.PerfectDefense:
                    return $"\uc720\ub2db {mission.target}\uae30 \uc774\uc0c1\uc73c\ub85c R{mission.targetRound}\uc744 HP \uc190\uc2e4 0\uc73c\ub85c \ud074\ub9ac\uc5b4\ud558\uc138\uc694.";
                case MissionKind.MergeRush:
                    return $"R{mission.targetRound}\uae4c\uc9c0 \ud569\uc131 {mission.target}\ud68c, \ub808\uc5b4+ \uacb0\uacfc {mission.secondaryTarget}\ud68c\ub97c \uc644\ub8cc\ud558\uc138\uc694.";
                case MissionKind.RoleCollector:
                    return $"R{mission.targetRound}\uae4c\uc9c0 \uc11c\ub85c \ub2e4\ub978 \uc5ed\ud560 \uc720\ub2db {mission.target}\uc885\uc744 \ubcf4\uc720\ud558\uc138\uc694.";
                case MissionKind.LeanDefense:
                    return $"R{mission.targetRound} \uc885\ub8cc \uc2dc \uc720\ub2db {mission.target}\uae30 \uc774\ud558, HP \uc190\uc2e4 {mission.secondaryTarget} \uc774\ud558\ub85c \ubc84\ud2f0\uc138\uc694.";
                case MissionKind.BossPreparation:
                    return $"R{mission.targetRound} \uc2dc\uc791 \uc804\uae4c\uc9c0 \uc804\uc124 \uc774\uc0c1 \uc720\ub2db {mission.target}\uae30\ub97c \ubcf4\uc720\ud558\uc138\uc694.";
                case MissionKind.SummonSprint:
                    return $"R{mission.targetRound}\uae4c\uc9c0 \uc9c1\uc811 \uc18c\ud658 {mission.target}\ud68c\ub97c \ud558\uace0 HP \uc190\uc2e4 {mission.secondaryTarget} \uc774\ud558\ub85c \ubc84\ud2f0\uc138\uc694.";
                case MissionKind.LastStandGambit:
                    return $"R{mission.targetRound} \uc885\ub8cc \uc804\uae4c\uc9c0 HP 1~7, \uc9c1\uc811 \uc18c\ud658 2\ud68c \uc774\ud558, \ubcf4\ub4dc \uc720\ub2db 2\uae30 \uc774\ud558\ub97c \uc720\uc9c0\ud558\uc138\uc694.";
                case MissionKind.EmptySlotDiscipline:
                    return $"R{mission.targetRound} \uc885\ub8cc \ud6c4 \ube48 \uc2ac\ub86f {mission.target}\uce78\uacfc \ub808\uc5b4+ \ud569\uc131 {mission.secondaryTarget}\ud68c\ub97c \ub0a8\uae30\uc138\uc694.";
                case MissionKind.RareUpgrade:
                    return $"R{mission.targetRound}\uae4c\uc9c0 \ub808\uc5b4+ \uc720\ub2db {mission.target}\uae30\uc640 \ub2e4\ub978 \uc5ed\ud560 {mission.secondaryTarget}\uc885\uc744 \ubcf4\uc720\ud558\uc138\uc694.";
                case MissionKind.LegendaryHunt:
                    return $"R{mission.targetRound}\uae4c\uc9c0 \uc804\uc124+ \uc720\ub2db {mission.target}\uae30\uc640 \uc5d0\ud53d+ \ud569\uc131 {mission.secondaryTarget}\ud68c\ub97c \ubcf4\uc720\ud558\uc138\uc694.";
                case MissionKind.MonsterHunter:
                    return $"R{mission.targetRound}\uae4c\uc9c0 HP \uc190\uc2e4 {mission.secondaryTarget} \uc774\ud558\ub85c \ubaac\uc2a4\ud130 {mission.target}\ub9c8\ub9ac\ub97c \ucc98\uce58\ud558\uc138\uc694.";
                case MissionKind.BossSlayer:
                    return $"R{mission.targetRound}\uae4c\uc9c0 \ubcf4\uc2a4 {mission.target}\ub9c8\ub9ac\ub97c \ucc98\uce58\ud558\uc138\uc694.";
                case MissionKind.NoSummonHold:
                    return $"R{mission.targetRound}\uc5d0\uc11c \uc9c1\uc811 \uc18c\ud658 0\ud68c, HP \uc190\uc2e4 {mission.secondaryTarget} \uc774\ud558\ub85c \ud074\ub9ac\uc5b4\ud558\uc138\uc694.";
                case MissionKind.KillStreak:
                    return $"R{mission.targetRound}\uc5d0\uc11c HP \uc190\uc2e4 0\uc73c\ub85c \ubaac\uc2a4\ud130 {mission.target}\ub9c8\ub9ac\ub97c \ucc98\uce58\ud558\uc138\uc694.";
                case MissionKind.HighGradeForge:
                    return $"R{mission.targetRound}\uae4c\uc9c0 {CharacterGradeUtility.GetDisplayName((CharacterGrade)mission.secondaryTarget)} \uc774\uc0c1 \ud569\uc131\uc744 {mission.target}\ud68c \uc644\ub8cc\ud558\uc138\uc694.";
                case MissionKind.SpendDownGambit:
                    return $"R{mission.targetRound} \uc2dc\uc791 \uc804 \ubcf4\uc720 \uace8\ub4dc\ub97c {mission.target}G \uc774\ud558\ub85c \ub9cc\ub4dc\uc138\uc694.";
                case MissionKind.UltimateRecipeChase:
                    return $"R{mission.targetRound}\uae4c\uc9c0 \ucd08\uc6d4 \ub808\uc2dc\ud53c \uc900\ube44 \ub610\ub294 \ucd08\uc6d4 \ud569\uc131\uc744 {mission.target}\ud68c \uc644\ub8cc\ud558\uc138\uc694.";
                case MissionKind.GradeRainbow:
                    return $"R{mission.targetRound}\uae4c\uc9c0 \uc11c\ub85c \ub2e4\ub978 \ub4f1\uae09 \uc720\ub2db {mission.target}\uc885\uc744 \ubcf4\uc720\ud558\uc138\uc694.";
                default:
                    return string.IsNullOrEmpty(mission.description) ? string.Empty : mission.description;
            }
        }

        private void ApplyRewardPacing(MissionInstance mission)
        {
            if (mission == null)
            {
                return;
            }
            mission.rewardText = BuildRewardText(mission);
        }
        private static string GetContractGrade(MissionInstance mission)
        {
            if (mission == null) return "안전";
            if (mission.kind == MissionKind.BossSlayer || mission.kind == MissionKind.UltimateRecipeChase || mission.jackpotChance >= 0.25f) return "전설";
            if (mission.kind == MissionKind.HighGradeForge || mission.kind == MissionKind.SpendDownGambit || mission.jackpotChance > 0f || mission.rouletteGoldMax > 0) return "도박";
            if (mission.goldReward >= 48 || mission.targetRound - mission.startRound >= 3) return "도전";
            return "안전";
        }

        private string BuildRewardText(MissionInstance mission)
        {
            if (mission == null)
            {
                return string.Empty;
            }

            string text = "확정: " + (mission.goldReward > 0 ? "+" + mission.goldReward + "골드" : "성장 보상");
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
                text += " | 추가: 룰렛 " + mission.rouletteGoldMin + "~" + mission.rouletteGoldMax + "G";
            }

            if (mission.jackpotGold > 0 && mission.jackpotChance > 0f)
            {
                text += " / JACKPOT " + Mathf.RoundToInt(mission.jackpotChance * 100f) + "%";
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
            if (resolvingMission || gameController == null || !missionSelected || activeMissions.Count != 1)
            {
                return;
            }

            MissionInstance mission = activeMissions[0];
            if (IsMissionComplete(mission, roundCompleted, completedRound))
            {
                CompleteMission(mission, roundCompleted ? completedRound : gameController.CurrentRound);
            }
            else if (IsMissionExpired(mission, roundCompleted, completedRound))
            {
                ExpireMission(0);
            }

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
                    return !gameController.IsRoundRunning && gameController.CurrentRound >= mission.targetRound &&
                        gameController.Gold >= mission.target && gameController.Life >= mission.startLife - mission.secondaryTarget;
                case MissionKind.PerfectDefense:
                    return roundCompleted && completedRound == mission.targetRound &&
                        gameController.Life >= mission.startLife && gameController.BoardUnitCount >= mission.target;
                case MissionKind.MergeRush:
                    return totalMerges - mission.startMerges >= mission.target &&
                        totalRarePlusMerges - mission.startRarePlusMerges >= mission.secondaryTarget;
                case MissionKind.RoleCollector:
                    return CountDistinctRoles() >= mission.target;
                case MissionKind.LeanDefense:
                    return roundCompleted && completedRound == mission.targetRound &&
                        gameController.BoardUnitCount <= mission.target &&
                        Mathf.Max(0, mission.startLife - gameController.Life) <= mission.secondaryTarget;
                case MissionKind.BossPreparation:
                    return !gameController.IsRoundRunning && gameController.CurrentRound >= mission.targetRound &&
                        CountUnitsAtLeast(CharacterGrade.Legendary) >= mission.target;
                case MissionKind.SummonSprint:
                    return totalSummons - mission.startSummons >= mission.target &&
                        Mathf.Max(0, mission.startLife - gameController.Life) <= mission.secondaryTarget;
                case MissionKind.LastStandGambit:
                    return roundCompleted && completedRound == mission.targetRound &&
                        IsLastStandGambitConditionMet(gameController.Life, totalSummons - mission.startSummons, gameController.BoardUnitCount, completedRound);
                case MissionKind.NoSummonHold:
                    return roundCompleted && completedRound == mission.targetRound &&
                        totalSummons == mission.startSummons &&
                        Mathf.Max(0, mission.startLife - gameController.Life) <= mission.secondaryTarget;
                case MissionKind.HighGradeForge:
                    CharacterGrade requiredForgeGrade = (CharacterGrade)Mathf.Clamp(mission.secondaryTarget, (int)CharacterGrade.Normal, (int)CharacterGrade.Transcendent);
                    return GetMergeResultsAtLeast(requiredForgeGrade) - GetStartMergeResultsAtLeast(mission, requiredForgeGrade) >= mission.target;
                case MissionKind.SpendDownGambit:
                    int goldBeforeClearReward = Mathf.Max(0, gameController.Gold - gameController.LastRoundClearGoldReward);
                    return roundCompleted && completedRound == mission.targetRound && goldBeforeClearReward <= mission.target;
                case MissionKind.EmptySlotDiscipline:
                    return roundCompleted && completedRound == mission.targetRound &&
                        gameController.EmptySlotCount >= mission.target && totalRarePlusMerges - mission.startRarePlusMerges >= mission.secondaryTarget;
                case MissionKind.RareUpgrade:
                    return CountUnitsAtLeast(CharacterGrade.Rare) >= mission.target && CountDistinctRoles() >= mission.secondaryTarget;
                case MissionKind.LegendaryHunt:
                    return CountUnitsAtLeast(CharacterGrade.Legendary) >= mission.target && totalEpicPlusMerges - mission.startEpicPlusMerges >= mission.secondaryTarget;
                case MissionKind.MonsterHunter:
                    return totalKills - mission.startKills >= mission.target && Mathf.Max(0, mission.startLife - gameController.Life) <= mission.secondaryTarget;
                case MissionKind.BossSlayer:
                    return totalBossKills - mission.startBossKills >= mission.target;
                case MissionKind.KillStreak:
                    return roundCompleted && completedRound == mission.targetRound &&
                        totalKills - mission.startKills >= mission.target && gameController.Life >= mission.startLife;
                case MissionKind.UltimateRecipeChase:
                    return !gameController.IsRoundRunning && gameController.CurrentRound >= mission.targetRound &&
                        (gameController.CanMergeUltimate() || totalFinalMerges - mission.startFinalMerges >= mission.target);
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
            missionSelected = false;
            offerRefreshQueued = true;
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
                int roll = gameController.RunContentRandom.Range(RunContentRandomChannel.Mission, min, max + 1, "mission.reward.roulette");
                goldReward += roll;
                highlights.Add("룰렛 +" + roll + "G");
            }

            if (mission.jackpotGold > 0 && mission.jackpotChance > 0f && gameController.RunContentRandom.Value(RunContentRandomChannel.Mission, "mission.reward.jackpot") <= mission.jackpotChance)
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
            string banner = "미션 완료! " + rewardSummary;
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
            missionSelected = false;
            offerRefreshQueued = true;
            recentlyExpiredKeys[mission.Key] = (gameController != null ? gameController.CurrentRound : 0) + 3;
            ShowFailureToast();

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
            RefreshUi();
        }

        private void HandleRoundBoardPreparation(int round)
        {
            if (gameController == null || gameController.IsRoundRunning)
            {
                return;
            }

            bool refreshUnselectedDraft = !missionSelected && activeMissions.Count > 0 && offersGeneratedForRound < round;
            if (refreshUnselectedDraft)
            {
                activeMissions.Clear();
                offerRefreshQueued = true;
                gameController.RequestBanner("전술계약 갱신", new Color(0.72f, 0.88f, 1f), 1.5f);
            }

            if (offerRefreshQueued || (!missionSelected && activeMissions.Count == 0))
            {
                RefillMissions();
            }

            // LastStand never grants a support unit. This clears only stale runtime state from earlier versions.
            pendingMissionSupportSummons = 0;
            RefreshUi();
        }
        // Called by DefenseGameController only after Result Continue and every higher-priority choice has closed.
        public bool TryOpenQueuedMissionChoice()
        {
            if (gameController == null || gameController.IsRoundRunning || missionSelected || activeMissions.Count == 0 || IsChoicePanelOpen)
            {
                return false;
            }

            SetPanelOpen(true);
            RefreshUi();
            return panelRoot != null && panelRoot.activeSelf;
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
            missionSelected = false;
            offerRefreshQueued = false;
            pendingMissionSupportSummons = 0;
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
            gameController?.NotifyPostRoundChoiceStateChanged();
        }

        // Combat may begin while the optional contract panel is visible through an external/UI action.
        // It is never a progression blocker, so close it without mutating the offers or selection.
        public void CloseChoicePanelForCombat()
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

            if (missionSelected && activeMissions.Count == 1)
            {
                summaryText.text = "\uc804\uc220 \uacc4\uc57d \uc9c4\ud589 \uc911  " + activeMissions[0].title;
                summaryText.color = activeMissions[0].accentColor;
            }
            else if (activeMissions.Count > 0)
            {
                summaryText.text = "\uc804\uc220 \uacc4\uc57d  ·  \uc0c8 \uacc4\uc57d " + activeMissions.Count + "\uac1c";
                summaryText.color = Color.white;
            }
            else
            {
                summaryText.text = "\uc804\uc220 \uacc4\uc57d \ub300\uae30";
                summaryText.color = Color.white;
            }
        }

        private void RefreshPanel()
        {
            if (panelHeaderText != null)
            {
                panelHeaderText.text = "\uc804\uc220 \uacc4\uc57d \uc120\ud0dd";
            }

            if (activeCardRoot != null)
            {
                bool showActiveCard = missionSelected && activeMissions.Count == 1;
                activeCardRoot.SetActive(showActiveCard);
                if (showActiveCard)
                {
                    MissionInstance active = activeMissions[0];
                    SetText(activeTitleText, "\uc804\uc220 \uacc4\uc57d \uc9c4\ud589 \uc911  " + active.title);
                    SetText(activeDescriptionText, active.description);
                    SetText(activeProgressText, GetProgressText(active));
                }
            }

            int buttonCount = optionButtons != null ? optionButtons.Length : 0;
            for (int i = 0; i < buttonCount; i++)
            {
                bool show = !missionSelected && i < activeMissions.Count;
                if (optionButtons[i] != null)
                {
                    optionButtons[i].gameObject.SetActive(show);
                    optionButtons[i].interactable = show && CanShowMissionOffers();
                    SetChildText(optionButtons[i].transform, "PickLabel", "\uc120\ud0dd");
                }

                if (!show)
                {
                    continue;
                }

                MissionInstance mission = activeMissions[i];
                SetText(GetText(optionTitleTexts, i), mission.title);
                SetText(GetText(optionDescriptionTexts, i), mission.description);
				SetText(GetText(optionRewardTexts, i), "\ubcf4\uc0c1  " + mission.rewardText);

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
                    return "HP " + gameController.Life + " / 7 \uC774\uD558 | \uC18C\uD658 " + Mathf.Max(0, totalSummons - mission.startSummons) + " / 2 | \uC720\uB2DB " + gameController.BoardUnitCount + " / 2 | R" + mission.targetRound + "\uAE4C\uC9C0";
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
            return life > 0 && life <= 7 && currentRound <= 2 && summonsSinceMissionStart <= 2 && boardUnitCount <= 2;
        }

        private bool HasOfferedMission(MissionKind kind)
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
            ShowToast("\uBBF8\uC158 \uC644\uB8CC! \uBCF4\uC0C1 \uD68D\uB4DD", string.Empty);
        }

        private void ShowFailureToast()
        {
            ShowToast("\uBBF8\uC158 \uC2E4\uD328", string.Empty);
        }

        private void ShowToast(string title, string reward)
        {
            SetText(completionToastTitleText, title);
            SetText(completionToastRewardText, reward);
            completionToastRoot.SetActive(true);
            toastTimer = CompletionToastHoldDuration + CompletionToastFadeDuration;

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
            float alpha = toastTimer <= CompletionToastFadeDuration
                ? Mathf.Clamp01(toastTimer / CompletionToastFadeDuration)
                : 1f;

            if (completionToastGroup != null)
            {
                completionToastGroup.alpha = alpha;
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
