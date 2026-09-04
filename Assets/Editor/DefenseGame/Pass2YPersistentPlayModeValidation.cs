using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefenseGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefenseGame.Editor
{
    // Test-only long-running UI validation. Production gameplay code is intentionally untouched.
    public static class Pass2YPersistentPlayModeValidation
    {
        private const string ScenePath = "Assets/Scenes/DG.unity";
        private const string OutputDirectoryName = "BatchPlaytestResults";
        private const string PersistentOutputFileName = "DefenseGame_Pass2Y_Overdrive_R10_R15.json";
        private const string ProgressionAuditOutputFileName = "DefenseGame_Pass3A_R1_R15_Overdrive.json";
        private const string MidgameAuditOutputFileName = "DefenseGame_Pass4_R1_R30_Overdrive.json";
        private const string Pass5OutputFileName = "DefenseGame_Pass5_R1_R30_Overdrive.json";
        private const string Pass6OutputFileName = "DefenseGame_Pass6_R1_R30_Overdrive.json";
        private const string Pass13OutputFileName = "DefenseGame_Pass13_R1_R20_Overdrive.json";
        private const float ValidationTimeScale = 8f;
        private const double ActionDelaySeconds = 0.14d;
        private const double StartupTimeoutSeconds = 15d;
        private const double RunTimeoutSeconds = 360d;
        private const double ChoiceBlockTimeoutSeconds = 5d;

        private static bool running;
        private static bool previousEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions previousEnterPlayModeOptions;
        private static bool previousRunInBackground;
        private static int previousVSyncCount;
        private static int previousTargetFrameRate;
        private static float previousTimeScale;
        private static double nextActionAt;
        private static double startedAt;
        private static double blockerObservedAt = -1d;
        private static double noActionBlockObservedAt = -1d;
        private static int runtimeErrors;
        private static readonly List<string> runtimeErrorMessages = new List<string>();
        private static bool shopPurchaseAttempted;
        private static bool stopAfterRunShopRecovery;
        private static bool skipRunShopPurchase;
        private static bool progressionAudit;
        private static bool pass5Validation;
        private static bool pass6EconomyValidation;
        private static bool pass5MissionWasActive;
        private static int pass5CompletedMissionCountAtSelection;
        private static int progressionAuditMaxRound = 15;
        private static string outputFileName = PersistentOutputFileName;
        private static DefenseGameController controller;
        private static ValidationReport report;
        private static readonly HashSet<int> RecordedRoundStarts = new HashSet<int>();
        private static bool pass9ChoiceFlowValidation;
        private static bool pass13IntegrationValidation;

        private static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputDirectoryName, outputFileName));

        [MenuItem("DefenseGame/Validation/Pass 2Y Persistent Overdrive R10-R15 UI Flow")]
        public static void Run()
        {
            pass5Validation = false;
            pass6EconomyValidation = false;
            Begin(false, false, false);
        }

        [MenuItem("DefenseGame/Validation/Pass 2Z RunShop Purchase Recovery UI Flow")]
        public static void RunShopPurchaseRecovery()
        {
            pass5Validation = false;
            pass6EconomyValidation = false;
            Begin(true, false, false);
        }

        [MenuItem("DefenseGame/Validation/Pass 2Z RunShop Later Recovery UI Flow")]
        public static void RunShopLaterRecovery()
        {
            pass5Validation = false;
            pass6EconomyValidation = false;
            Begin(true, true, false);
        }
        [MenuItem("DefenseGame/Validation/Pass 9 RunShop Purchase to Contract UI Flow")]
        public static void RunPass9RunShopPurchaseToContract()
        {
            pass5Validation = false;
            pass6EconomyValidation = false;
            pass9ChoiceFlowValidation = true;
            Begin(true, false, false);
        }

        [MenuItem("DefenseGame/Validation/Pass 9 RunShop Later to Contract UI Flow")]
        public static void RunPass9RunShopLaterToContract()
        {
            pass5Validation = false;
            pass6EconomyValidation = false;
            pass9ChoiceFlowValidation = true;
            Begin(true, true, false);
        }


        [MenuItem("DefenseGame/Validation/Pass 3A R1-R15 Progression Balance Audit")]
        public static void RunPass3AProgressionAudit()
        {
            pass5Validation = false;
            pass6EconomyValidation = false;
            progressionAuditMaxRound = 15;
            Begin(false, false, true);
        }

        [MenuItem("DefenseGame/Validation/Pass 13 R1-R20 Full Run Integration Validation")]
        public static void RunPass13FullRunIntegrationValidation()
        {
            pass5Validation = false;
            pass6EconomyValidation = false;
            pass13IntegrationValidation = true;
            progressionAuditMaxRound = 20;
            Begin(false, false, true);
        }

        [MenuItem("DefenseGame/Validation/Pass 4 R1-R30 Midgame Balance Audit")]
        public static void RunPass4MidgameAudit()
        {
            pass5Validation = false;
            pass6EconomyValidation = false;
            progressionAuditMaxRound = 30;
            Begin(false, false, true);
        }

        [MenuItem("DefenseGame/Validation/Pass 5 Boss Stability and Choice Progression")]
        public static void RunPass5BossStabilityAndChoiceProgression()
        {
            pass5Validation = true;
            pass6EconomyValidation = false;
            progressionAuditMaxRound = 30;
            Begin(false, false, true);
        }

        [MenuItem("DefenseGame/Validation/Pass 6 Gold to Power Progression")]
        public static void RunPass6GoldToPowerProgression()
        {
            pass5Validation = false;
            pass6EconomyValidation = true;
            progressionAuditMaxRound = 30;
            Begin(false, false, true);
        }

        private static void Begin(bool stopAfterRecovery, bool skipPurchase, bool audit)
        {
            if (running)
            {
                return;
            }

            running = true;
            progressionAudit = audit;
            stopAfterRunShopRecovery = stopAfterRecovery;
            skipRunShopPurchase = skipPurchase;
            outputFileName = stopAfterRecovery
                ? (skipPurchase ? "DefenseGame_Pass2Z_RunShopLater.json" : "DefenseGame_Pass2Z_RunShopPurchase.json")
                : (pass13IntegrationValidation ? Pass13OutputFileName : (pass6EconomyValidation ? Pass6OutputFileName : (pass5Validation ? Pass5OutputFileName : (progressionAudit ? (progressionAuditMaxRound > 15 ? MidgameAuditOutputFileName : ProgressionAuditOutputFileName) : PersistentOutputFileName))));
            runtimeErrors = 0;
            runtimeErrorMessages.Clear();
            pass5MissionWasActive = false;
            pass5CompletedMissionCountAtSelection = 0;
            shopPurchaseAttempted = skipRunShopPurchase;
            controller = null;
            RecordedRoundStarts.Clear();
            blockerObservedAt = -1d;
            noActionBlockObservedAt = -1d;
            report = new ValidationReport
            {
                status = "running",
                validationMode = "EventSystem UI clicks only; no StartRound/debug round jump/reward fixture",
                runShopScenario = stopAfterRecovery ? (skipPurchase ? "later" : "purchase_then_close") : "persistent",
                validationTimeScale = ValidationTimeScale,
                startedUtc = DateTime.UtcNow.ToString("O"),
                roundSnapshots = new List<RoundSnapshot>(),
                roundAudits = new List<RoundAuditEntry>(),
                runtimeErrorMessages = new List<string>(),
                actionLog = new List<string>()
            };

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? string.Empty);
            if (File.Exists(OutputPath))
            {
                File.Delete(OutputPath);
            }

            previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            previousRunInBackground = Application.runInBackground;
            previousVSyncCount = QualitySettings.vSyncCount;
            previousTargetFrameRate = Application.targetFrameRate;
            previousTimeScale = Time.timeScale;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            Application.runInBackground = true;
            Application.targetFrameRate = 240;
            QualitySettings.vSyncCount = 0;

            Application.logMessageReceived -= HandleLogMessage;
            MonsterUnit.OnMonsterSpawned -= HandleMonsterSpawned;
            MonsterUnit.OnMonsterKilled -= HandleMonsterKilled;
            MonsterUnit.OnMonsterEscaped -= HandleMonsterEscaped;
            MonsterUnit.OnMonsterSpawned += HandleMonsterSpawned;
            MonsterUnit.OnMonsterKilled += HandleMonsterKilled;
            MonsterUnit.OnMonsterEscaped += HandleMonsterEscaped;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Application.logMessageReceived -= HandleLogMessage;
                Application.logMessageReceived += HandleLogMessage;
                startedAt = EditorApplication.timeSinceStartup;
                nextActionAt = startedAt + 1d;
                Log("entered_play_mode");
            }
        }

        private static void Tick()
        {
            if (!running || !EditorApplication.isPlaying)
            {
                return;
            }

            Time.timeScale = ValidationTimeScale;
            Application.runInBackground = true;
            Application.targetFrameRate = 240;
            QualitySettings.vSyncCount = 0;

            double now = EditorApplication.timeSinceStartup;
            if (controller == null)
            {
                controller = UnityEngine.Object.FindObjectOfType<DefenseGameController>();
                if (controller == null)
                {
                    if (now - startedAt > StartupTimeoutSeconds)
                    {
                        Finish("environment_blocked", "DefenseGameController was not created before startup timeout.");
                    }
                    return;
                }
            }

            ObserveRuntimeState(now);
            if (ShouldFinishForOutcome(now))
            {
                return;
            }

            if (now < nextActionAt)
            {
                return;
            }

            DriveUi(now);
            nextActionAt = now + ActionDelaySeconds;
        }

        private static void ObserveRuntimeState(double now)
        {
            int round = controller.CurrentRound;
            if (progressionAudit)
            {
                ObserveRoundAudit(round);
            }
            ObservePass5MissionSettlement();
            if (controller.IsRoundRunning && round >= 10 && round <= 15 && RecordedRoundStarts.Add(round))
            {
                report.roundSnapshots.Add(CaptureRoundSnapshot(round, "round_started"));
                Log("round_" + round + "_started");
            }

            if (round == 10 && controller.IsRoundRunning && report.r10Start == null)
            {
                report.r10Start = CaptureRoundSnapshot(10, "r10_start");
                report.r10BossKillCountAtStart = controller.RunBossKillCount;
            }

            if (progressionAudit && round % 10 == 0 && controller.IsRoundRunning)
            {
                GameObject warningPanel = FindObject("BossWarningPanel");
                Text warningTitle = FindText("BossWarningTitle");
                RoundAuditEntry bossAudit = report.roundAudits.FirstOrDefault(entry => entry != null && entry.round == round);
                if (warningPanel != null && warningPanel.activeInHierarchy && warningTitle != null && bossAudit != null)
                {
                    bossAudit.bossWarningSeen = true;
                    bossAudit.bossWarningTitle = warningTitle.text ?? string.Empty;
                }
            }

            if (round == 10 && controller.IsRoundRunning)
            {
                GameObject warningPanel = FindObject("BossWarningPanel");
                Text warningTitle = FindText("BossWarningTitle");
                if (warningPanel != null && warningPanel.activeInHierarchy && warningTitle != null)
                {
                    report.r10BossWarningSeen = true;
                    report.r10BossWarningTitle = warningTitle.text ?? string.Empty;
                }

                MonsterUnit activeBoss = MonsterUnit.ActiveInstances.FirstOrDefault(monster => monster != null && monster.IsBoss);
                if (activeBoss != null)
                {
                    report.r10BossActiveHealth01 = activeBoss.MaxHealth > 0f ? Mathf.Clamp01(activeBoss.CurrentHealth / activeBoss.MaxHealth) : -1f;
                }
            }

            if (report.r10BossSpawned && !controller.IsRoundRunning && controller.CurrentRound == 10 && !report.r10ResultObserved)
            {
                report.r10ResultObserved = true;
                report.r10BossCleared = controller.Life > 0 && controller.RunBossKillCount > report.r10BossKillCountAtStart;
                report.r10EndLife = controller.Life;
                report.r10EndGold = controller.Gold;
                report.r10BossHealthRemaining01 = report.r10BossCleared ? 0f : report.r10BossActiveHealth01;
                RoundAuditEntry audit = report.roundAudits.FirstOrDefault(entry => entry != null && entry.round == 10);
                if (audit != null)
                {
                    audit.bossKilled = report.r10BossCleared;
                }
                Log("r10_result_" + (report.r10BossCleared ? "clear" : "not_clear"));
            }

            if (controller.CurrentRound >= 11 && controller.IsRoundRunning)
            {
                report.r11StartedAfterR10 = report.r10ResultObserved && report.r10ContinueClicked;
            }

            string blocker = controller.BlockingChoiceReason ?? "None";
            int activePanels = CountActiveChoicePanels();
            if (!controller.IsRoundRunning && blocker != "None")
            {
                if (blockerObservedAt < 0d)
                {
                    blockerObservedAt = now;
                }
                report.lastBlockingChoiceReason = blocker;
                report.lastActiveChoicePanelCount = activePanels;
                report.lastBattleButtonInteractable = IsInteractable("BattleButton");
                if (activePanels != 1)
                {
                    report.invisibleBlockerObserved = true;
                }
                if (now - blockerObservedAt > ChoiceBlockTimeoutSeconds)
                {
                    Finish("ui_blocked", "Choice state persisted without one visible actionable panel. blocker=" + blocker + ", activePanels=" + activePanels);
                }
            }
            else
            {
                blockerObservedAt = -1d;
            }

            bool battleAvailable = IsInteractable("BattleButton");
            if (!controller.IsRoundRunning && blocker == "None" && activePanels == 0 && !battleAvailable)
            {
                if (noActionBlockObservedAt < 0d)
                {
                    noActionBlockObservedAt = now;
                }

                report.invisibleBlockerObserved = true;
                report.lastBlockingChoiceReason = blocker;
                report.lastActiveChoicePanelCount = activePanels;
                report.lastBattleButtonInteractable = false;
                report.lastActiveChoicePanelNames = DescribeActiveChoicePanels();
                if (now - noActionBlockObservedAt > ChoiceBlockTimeoutSeconds)
                {
                    Finish("ui_blocked", "BattleButton remained disabled for a preparation state with BlockingChoiceReason=None and no active choice panel.");
                }
            }
            else
            {
                noActionBlockObservedAt = -1d;
            }
        }

        private static void ObservePass5MissionSettlement()
        {
            if (!pass5Validation || report == null || !report.tacticalMissionSelectedByUi)
            {
                return;
            }

            TacticalMissionSystem missionSystem = UnityEngine.Object.FindObjectOfType<TacticalMissionSystem>();
            if (missionSystem == null)
            {
                return;
            }

            if (missionSystem.HasActiveMissionSelection)
            {
                pass5MissionWasActive = true;
                return;
            }

            if (pass5MissionWasActive && !report.tacticalMissionSettlementObserved)
            {
                report.tacticalMissionSettlementObserved = true;
                report.tacticalMissionSettlementResult = missionSystem.CompletedMissionCount > pass5CompletedMissionCountAtSelection ? "completed" : "failed";
                Log("mission_settlement_" + report.tacticalMissionSettlementResult + "_r" + controller.CurrentRound);
            }
        }

        private static void ObserveRoundAudit(int round)
        {
            if (round < 1 || round > progressionAuditMaxRound || report == null)
            {
                return;
            }

            RoundAuditEntry audit = report.roundAudits.FirstOrDefault(entry => entry != null && entry.round == round);
            if (controller.IsRoundRunning)
            {
                if (audit == null)
                {
                    audit = new RoundAuditEntry
                    {
                        round = round,
                        kind = round % 10 == 0 ? "boss" : (controller.IsCurrentRoundHorde ? "horde" : "regular"),
                        start = CaptureRoundSnapshot(round, "start"),
                        choiceFlow = new List<string>()
                    };
                    report.roundAudits.Add(audit);
                }
                return;
            }

            if (audit != null && audit.end == null)
            {
                audit.end = CaptureRoundSnapshot(round, "end");
                audit.outcome = controller.Life > 0 ? "cleared" : "defeat";
            }
        }

        private static void RecordChoiceFlow(string action)
        {
            if (!progressionAudit || report == null || controller == null || string.IsNullOrEmpty(action))
            {
                return;
            }

            RoundAuditEntry audit = report.roundAudits.FirstOrDefault(entry => entry != null && entry.round == controller.CurrentRound);
            if (audit != null && audit.choiceFlow != null)
            {
                audit.choiceFlow.Add(action);
            }
        }
        private static bool ShouldFinishForOutcome(double now)
        {
            if (progressionAudit && controller.CurrentRound == progressionAuditMaxRound && !controller.IsRoundRunning)
            {
                RoundAuditEntry finalAudit = report.roundAudits.FirstOrDefault(entry => entry != null && entry.round == progressionAuditMaxRound);
                if (finalAudit != null && finalAudit.end != null)
                {
                    report.finalRound = controller.CurrentRound;
                    report.finalLife = controller.Life;
                    report.finalGold = controller.Gold;
                    Finish(finalAudit.outcome == "cleared" ? "r" + progressionAuditMaxRound + "_completed" : "defeat_after_r10", "Completed R" + progressionAuditMaxRound + " through UI flow.");
                    return true;
                }
            }

            if (stopAfterRunShopRecovery && report.r4RunShopSeen && (report.r4RunShopPurchaseClicked || report.r4RunShopCloseClicked) && (!pass9ChoiceFlowValidation || report.tacticalMissionSelectedByUi) && controller.CurrentRound >= 5 && controller.IsRoundRunning)
            {
                report.r5StartedAfterRunShop = true;
                report.finalRound = controller.CurrentRound;
                report.finalLife = controller.Life;
                report.finalGold = controller.Gold;
                Finish("r4_shop_recovered", "RunShop was resolved through visible UI and BattleButton started R5.");
                return true;
            }

            if (!progressionAudit && controller.CurrentRound >= 15 && controller.IsRoundRunning)
            {
                report.r15Reached = true;
                report.finalRound = controller.CurrentRound;
                report.finalLife = controller.Life;
                report.finalGold = controller.Gold;
                Finish("reached_r15", "Reached R15 through UI flow.");
                return true;
            }

            if (controller.Life <= 0 && !controller.IsRoundRunning)
            {
                report.finalRound = controller.CurrentRound;
                report.finalLife = controller.Life;
                report.finalGold = controller.Gold;
                Finish(controller.CurrentRound < 10 ? "defeat_before_r10" : "defeat_after_r10", "Gameplay defeat recorded without debug bypass.");
                return true;
            }

            if (now - startedAt > RunTimeoutSeconds)
            {
                report.finalRound = controller.CurrentRound;
                report.finalLife = controller.Life;
                report.finalGold = controller.Gold;
                Finish("timeout", "Validation wall-clock timeout before R" + progressionAuditMaxRound + ".");
                return true;
            }

            return false;
        }

        private static void DriveUi(double now)
        {
            if (!report.overdriveSelected)
            {
                Button modeButton = FindButton("LobbyCombatModeButton");
                if (modeButton != null && modeButton.gameObject.activeInHierarchy && modeButton.interactable)
                {
                    Click(modeButton);
                    report.overdriveSelected = controller.IsOverdriveMode;
                    Log("clicked_overdrive_mode=" + report.overdriveSelected);
                }
                return;
            }

            if (!report.enteredBattlePreparation)
            {
                Button lobbyBattle = FindButton("LobbyBattleButton");
                if (lobbyBattle != null && lobbyBattle.gameObject.activeInHierarchy && lobbyBattle.interactable)
                {
                    Click(lobbyBattle);
                    report.enteredBattlePreparation = true;
                    Log("clicked_lobby_battle");
                }
                return;
            }

            if (controller.IsRoundRunning)
            {
                return;
            }

            if (pass13IntegrationValidation && controller.CurrentRound <= 0 && !report.initialMissionAutoOpenResolved)
            {
                TryResolvePass13InitialMissionAutoOpen();
                return;
            }

            // A RunShop modal owns the post-result choice state. If a delayed Result overlay
            // is still animating behind it, resolve the actual open choice first.
            if (TryResolveChoice("AugmentChoiceOverlay", "AugmentChoice_")) return;
            if (TryResolveRunShop()) return;

            GameObject resultOverlay = FindObject("RoundResultOverlay");
            if (resultOverlay != null && resultOverlay.activeInHierarchy)
            {
                Button continueButton = FindButton("ResultContinueButton");
                if (Click(continueButton))
                {
                    if (controller.CurrentRound == 10)
                    {
                        report.r10ContinueClicked = true;
                    }
                    RecordChoiceFlow("ResultContinue");
                    Log("clicked_result_continue_r" + controller.CurrentRound);
                    return;
                }
            }

            if (TryResolveTacticalMission()) return;
            if (TryResolveChoice("LuckySummonChoiceOverlay", "LuckySummonChoice")) return;
            if (TryResolveChoice("Fate", "Fate")) return;

            Button battleButton = FindButton("BattleButton");
            if (battleButton == null || !battleButton.gameObject.activeInHierarchy || !battleButton.interactable)
            {
                report.lastBlockingChoiceReason = controller.BlockingChoiceReason;
                report.lastBattleButtonInteractable = false;
                report.lastActiveChoicePanelCount = CountActiveChoicePanels();
                return;
            }

            if (pass6EconomyValidation && TrySpendGoldThroughVisibleUi())
            {
                return;
            }

            PrepareUsingOnlyUiClicks();
            if (Click(battleButton))
            {
                Log("clicked_battle_r" + (controller.CurrentRound + 1));
            }
        }

        private static bool TrySpendGoldThroughVisibleUi()
        {
            if (controller == null || controller.IsRoundRunning || controller.CurrentRound < 10)
            {
                return false;
            }

            CharacterGrade[] spendingOrder = { CharacterGrade.Normal, CharacterGrade.Rare, CharacterGrade.Epic, CharacterGrade.Legendary, CharacterGrade.Mythic };
            for (int i = 0; i < spendingOrder.Length; i++)
            {
                CharacterGrade grade = spendingOrder[i];
                if (!controller.CanUpgradeGrade(grade))
                {
                    continue;
                }

                int previousLevel = controller.GetGradeUpgradeLevel(grade);
                Button upgrade = FindButton("GradeUpgrade_" + grade);
                if (Click(upgrade) && controller.GetGradeUpgradeLevel(grade) > previousLevel)
                {
                    report.actualUiGradeUpgradeSpendCount++;
                    Log("pass6_clicked_grade_upgrade_" + grade + "_r" + controller.CurrentRound);
                    return true;
                }
            }

            if (controller.CanUpgradeSummonGradeLuck())
            {
                int previousLuck = controller.SummonGradeLuckLevel;
                Button luck = FindButton("SummonGradeLuckUpgrade");
                if (Click(luck) && controller.SummonGradeLuckLevel > previousLuck)
                {
                    report.actualUiLuckUpgradeSpendCount++;
                    Log("pass6_clicked_grade_luck_r" + controller.CurrentRound);
                    return true;
                }
            }

            Button summon = FindButton("SummonButton");
            if (controller.BoardUnitCount < 8 && controller.EmptySlotCount > 0 && Click(summon))
            {
                report.actualUiSummonSpendCount++;
                Log("pass6_clicked_summon_r" + controller.CurrentRound);
                return true;
            }

            return false;
        }
        private static bool TryMergeGradeThroughVisibleUi(CharacterGrade grade)
        {
            if (controller == null || controller.CountUnitsOfGrade(grade) < 3)
            {
                return false;
            }

            int mergesBefore = controller.RunTotalMerges;
            Button merge = FindButton(grade + "GradeCard");
            if (!Click(merge) || controller.RunTotalMerges <= mergesBefore)
            {
                return false;
            }

            report.actualUiMergeCompleted = true;
            report.actualUiMergeSpendCount++;
            Log("pass6_clicked_merge_" + grade + "_r" + controller.CurrentRound);
            return true;
        }

        private static void PrepareUsingOnlyUiClicks()
        {
            // A deliberately conservative human-like preparation: one normal summon and one Normal-grade upgrade attempt.
            // Both actions consume only naturally earned in-run Gold through their visible buttons.
            Button summon = FindButton("SummonButton");
            if (summon != null && summon.gameObject.activeInHierarchy && summon.interactable && controller.EmptySlotCount > 0)
            {
                Click(summon);
                Log("clicked_summon_r" + controller.CurrentRound);
            }

            if (pass5Validation && !report.actualUiMergeCompleted && controller.CountUnitsOfGrade(CharacterGrade.Normal) >= 3)
            {
                int mergesBefore = controller.RunTotalMerges;
                Button normalMerge = FindButton("NormalGradeCard");
                if (Click(normalMerge) && controller.RunTotalMerges > mergesBefore)
                {
                    report.actualUiMergeCompleted = true;
                    Log("clicked_normal_merge_r" + controller.CurrentRound);
                }
            }

            Button normalUpgrade = FindButton("GradeUpgrade_Normal");
            if (normalUpgrade != null && normalUpgrade.gameObject.activeInHierarchy && normalUpgrade.interactable && controller.BoardUnitCount > 0)
            {
                Click(normalUpgrade);
                Log("clicked_normal_upgrade_r" + controller.CurrentRound);
            }
        }

        private static bool TryResolvePass13InitialMissionAutoOpen()
        {
            SimpleGameHUD hud = UnityEngine.Object.FindObjectOfType<SimpleGameHUD>();
            TacticalMissionSystem missionSystem = UnityEngine.Object.FindObjectOfType<TacticalMissionSystem>();
            if (hud == null || missionSystem == null || !hud.IsOpeningTutorialCompleteForCurrentRun)
            {
                return true;
            }

            if (!missionSystem.IsChoicePanelOpen)
            {
                return true;
            }

            report.initialMissionAutoOpenObserved = missionSystem.MissionOfferCount == 3 && !missionSystem.HasActiveMissionSelection;
            Button laterButton = FindButton("MissionCloseButton");
            if (Click(laterButton))
            {
                report.initialMissionAutoOpenResolved = !missionSystem.IsChoicePanelOpen && !missionSystem.HasActiveMissionSelection && IsInteractable("BattleButton");
                RecordChoiceFlow("InitialTacticalMission:Later");
                Log("resolved_initial_mission_auto_open=" + report.initialMissionAutoOpenResolved);
            }

            return true;
        }

        private static bool TryResolveTacticalMission()
        {
            GameObject overlay = FindObject("TacticalMissionOverlay");
            if (overlay == null || !overlay.activeInHierarchy)
            {
                return false;
            }

            if ((pass5Validation || (pass9ChoiceFlowValidation && report.r4RunShopSeen)) && !report.tacticalMissionSelectedByUi)
            {
                Button option = FindButton("MissionOption_0");
                if (Click(option))
                {
                    TacticalMissionSystem missionSystem = UnityEngine.Object.FindObjectOfType<TacticalMissionSystem>();
                    report.tacticalMissionSelectedByUi = true;
                    pass5CompletedMissionCountAtSelection = missionSystem != null ? missionSystem.CompletedMissionCount : 0;
                    RecordChoiceFlow("TacticalMission:Select");
                    Log("clicked_mission_select_r" + controller.CurrentRound);
                }
                return true;
            }

            Button laterButton = FindButton("MissionCloseButton");
            if (Click(laterButton))
            {
                if (pass5Validation && report.tacticalMissionSettlementObserved)
                {
                    report.nextTacticalMissionOfferResolvedByUi = true;
                }
                RecordChoiceFlow("TacticalMission:Later");
                Log("clicked_mission_later_r" + controller.CurrentRound);
            }
            return true;
        }

        private static bool TryResolveRunShop()
        {
            GameObject overlay = FindObject("RunShopOverlay");
            if (overlay == null || !overlay.activeInHierarchy)
            {
                return false;
            }

            if (controller.CurrentRound == 4 || pass9ChoiceFlowValidation)
            {
                report.r4RunShopSeen = true;
            }

            if (!shopPurchaseAttempted)
            {
                Button offer = overlay.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(candidate => candidate != null && candidate.gameObject.activeInHierarchy && candidate.interactable && candidate.name.StartsWith("RunShopOffer_", StringComparison.Ordinal));
                shopPurchaseAttempted = true;
                if (Click(offer))
                {
                    if (controller.CurrentRound == 4 || pass9ChoiceFlowValidation) report.r4RunShopPurchaseClicked = true;
                    report.actualUiRunShopPurchaseCompleted = true;
                    RecordChoiceFlow("RunShop:Purchase");
                    Log("clicked_choice_" + offer.name + "_r" + controller.CurrentRound);
                    return true;
                }
            }

            Button close = FindButton("RunShopCloseButton");
            if (Click(close))
            {
                if (controller.CurrentRound == 4 || pass9ChoiceFlowValidation) report.r4RunShopCloseClicked = true;
                RecordChoiceFlow("RunShop:Later");
                Log("clicked_run_shop_close_r" + controller.CurrentRound);
                return true;
            }

            return false;
        }
        private static bool TryResolveChoice(string overlayNameFragment, string preferredButtonNameFragment)
        {
            GameObject overlay = FindObjectContaining(overlayNameFragment);
            if (overlay == null || !overlay.activeInHierarchy)
            {
                return false;
            }

            Button button = overlay.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(candidate => candidate != null && candidate.gameObject.activeInHierarchy && candidate.interactable && candidate.name.IndexOf(preferredButtonNameFragment, StringComparison.OrdinalIgnoreCase) >= 0 && candidate.name.IndexOf("Close", StringComparison.OrdinalIgnoreCase) < 0 && candidate.name.IndexOf("Later", StringComparison.OrdinalIgnoreCase) < 0)
                ?? overlay.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(candidate => candidate != null && candidate.gameObject.activeInHierarchy && candidate.interactable && candidate.name.IndexOf("Close", StringComparison.OrdinalIgnoreCase) < 0 && candidate.name.IndexOf("Later", StringComparison.OrdinalIgnoreCase) < 0);
            if (!Click(button))
            {
                return false;
            }

            RecordChoiceFlow(overlayNameFragment + ":" + button.name);
            Log("clicked_choice_" + button.name + "_r" + controller.CurrentRound);
            return true;
        }

        private static bool Click(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            {
                return false;
            }

            EventSystem eventSystem = EventSystem.current;
            GameObject temporaryEventSystem = null;
            if (eventSystem == null)
            {
                temporaryEventSystem = new GameObject("Pass2Y_TestEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem = temporaryEventSystem.GetComponent<EventSystem>();
            }

            try
            {
                PointerEventData eventData = new PointerEventData(eventSystem)
                {
                    button = PointerEventData.InputButton.Left
                };
                ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerClickHandler);
                return true;
            }
            finally
            {
                if (temporaryEventSystem != null)
                {
                    UnityEngine.Object.DestroyImmediate(temporaryEventSystem);
                }
            }
        }

        private static void HandleMonsterSpawned(MonsterUnit monster)
        {
            if (!running || controller == null || monster == null || !monster.IsBoss)
            {
                return;
            }

            int round = controller.CurrentRound;
            string displayName = monster.Definition != null ? monster.Definition.displayName : monster.gameObject.name;
            if (round == 10)
            {
                report.r10BossSpawned = true;
                report.r10BossSpawnName = displayName;
                report.r10BossSpawnHealth = monster.MaxHealth;
                report.r10BossActiveHealth01 = monster.MaxHealth > 0f ? Mathf.Clamp01(monster.CurrentHealth / monster.MaxHealth) : -1f;
                Log("r10_boss_spawned_" + report.r10BossSpawnName);
            }

            if (!progressionAudit)
            {
                return;
            }

            RoundAuditEntry audit = report.roundAudits.FirstOrDefault(entry => entry != null && entry.round == round);
            if (audit != null)
            {
                audit.bossName = displayName;
                audit.bossMaxHealth = monster.MaxHealth;
                audit.bossHealthRemaining01 = monster.MaxHealth > 0f ? Mathf.Clamp01(monster.CurrentHealth / monster.MaxHealth) : -1f;
            }
        }

        private static void HandleMonsterKilled(MonsterUnit monster)
        {
            UpdateAuditBossResult(monster, 0f, true);
        }

        private static void HandleMonsterEscaped(MonsterUnit monster)
        {
            float healthRemaining01 = monster != null && monster.MaxHealth > 0f ? Mathf.Clamp01(monster.CurrentHealth / monster.MaxHealth) : -1f;
            UpdateAuditBossResult(monster, healthRemaining01, false);
        }

        private static void UpdateAuditBossResult(MonsterUnit monster, float healthRemaining01, bool killed)
        {
            if (!running || !progressionAudit || controller == null || monster == null || !monster.IsBoss)
            {
                return;
            }

            RoundAuditEntry audit = report.roundAudits.FirstOrDefault(entry => entry != null && entry.round == controller.CurrentRound);
            if (audit != null)
            {
                audit.bossHealthRemaining01 = healthRemaining01;
                audit.bossKilled |= killed;
            }
        }
        private static RoundSnapshot CaptureRoundSnapshot(int round, string phase)
        {
            Dictionary<CharacterGrade, int> counts = new Dictionary<CharacterGrade, int>();
            foreach (CharacterGrade grade in Enum.GetValues(typeof(CharacterGrade)))
            {
                counts[grade] = 0;
            }
            foreach (DefenderUnit unit in UnityEngine.Object.FindObjectsOfType<DefenderUnit>())
            {
                if (unit != null && unit.CurrentSlot != null)
                {
                    counts[unit.Grade]++;
                }
            }

            RuntimeBgmController bgm = UnityEngine.Object.FindObjectOfType<RuntimeBgmController>();
            AudioSource bgmSource = bgm != null ? bgm.GetComponent<AudioSource>() : null;

            return new RoundSnapshot
            {
                round = round,
                phase = phase,
                life = controller.Life,
                gold = controller.Gold,
                boardUnits = controller.BoardUnitCount,
                normal = counts[CharacterGrade.Normal],
                rare = counts[CharacterGrade.Rare],
                epic = counts[CharacterGrade.Epic],
                legendary = counts[CharacterGrade.Legendary],
                mythic = counts[CharacterGrade.Mythic],
                transcendent = counts[CharacterGrade.Transcendent],
                playerSummons = controller.RunTotalPlayerSummons,
                merges = controller.RunTotalMerges,
                blockingChoiceReason = controller.BlockingChoiceReason,
                targetCount = controller.RoundTargetCount,
                horde = controller.IsCurrentRoundHorde,
                battleButtonInteractable = IsInteractable("BattleButton"),
                activeChoicePanels = DescribeActiveChoicePanels(),
                bgmClipName = bgmSource != null && bgmSource.clip != null ? bgmSource.clip.name : string.Empty,
                bgmPlaying = bgmSource != null && bgmSource.isPlaying
            };
        }

        private static readonly string[] ChoicePanelNames = { "RoundResultOverlay", "AugmentChoiceOverlay", "TacticalMissionOverlay", "RunShopOverlay", "LuckySummonChoiceOverlay", "Fate" };

        private static int CountActiveChoicePanels()
        {
            return ChoicePanelNames.Count(panelName =>
            {
                GameObject panel = FindObjectContaining(panelName);
                return panel != null && panel.activeInHierarchy;
            });
        }

        private static string DescribeActiveChoicePanels()
        {
            string[] active = ChoicePanelNames.Where(panelName =>
            {
                GameObject panel = FindObjectContaining(panelName);
                return panel != null && panel.activeInHierarchy;
            }).ToArray();
            return active.Length > 0 ? string.Join(",", active) : "none";
        }

        private static bool IsInteractable(string buttonName)
        {
            Button button = FindButton(buttonName);
            return button != null && button.gameObject.activeInHierarchy && button.interactable;
        }

        private static Button FindButton(string name)
        {
            Button[] matches = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .Where(button => button != null && button.name == name)
                .ToArray();
            return matches.FirstOrDefault(button => button.gameObject.activeInHierarchy && button.interactable)
                ?? matches.FirstOrDefault(button => button.gameObject.activeInHierarchy)
                ?? matches.FirstOrDefault();
        }

        private static Text FindText(string name)
        {
            Text[] matches = UnityEngine.Object.FindObjectsOfType<Text>(true)
                .Where(text => text != null && text.name == name)
                .ToArray();
            return matches.FirstOrDefault(text => text.gameObject.activeInHierarchy) ?? matches.FirstOrDefault();
        }

        private static GameObject FindObject(string name)
        {
            GameObject[] matches = UnityEngine.Object.FindObjectsOfType<Transform>(true)
                .Select(transform => transform != null ? transform.gameObject : null)
                .Where(gameObject => gameObject != null && gameObject.name == name)
                .ToArray();
            return matches.FirstOrDefault(gameObject => gameObject.activeInHierarchy) ?? matches.FirstOrDefault();
        }

        private static GameObject FindObjectContaining(string fragment)
        {
            GameObject[] matches = UnityEngine.Object.FindObjectsOfType<Transform>(true)
                .Select(transform => transform != null ? transform.gameObject : null)
                .Where(gameObject => gameObject != null && gameObject.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            return matches.FirstOrDefault(gameObject => gameObject.activeInHierarchy) ?? matches.FirstOrDefault();
        }

        private static void Log(string entry)
        {
            if (report != null && report.actionLog != null && report.actionLog.Count < 160)
            {
                report.actionLog.Add(Math.Round(EditorApplication.timeSinceStartup - startedAt, 2) + "s " + entry);
            }
        }

        private static void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            string message = string.IsNullOrEmpty(condition) ? "<empty>" : condition;
            if (!runtimeErrorMessages.Contains(message) && runtimeErrorMessages.Count < 20)
            {
                runtimeErrorMessages.Add(message);
            }

            if (!IsExternalEditorServiceError(message))
            {
                runtimeErrors++;
            }
        }

        private static bool IsExternalEditorServiceError(string condition)
        {
            if (string.IsNullOrEmpty(condition))
            {
                return false;
            }

            return condition.IndexOf("[Licensing::", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   condition.IndexOf("Access token is unavailable", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   condition.IndexOf("Unable to update licenses", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   condition.IndexOf("No ULF license found", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   condition.IndexOf("Code 500 while updating license", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        private static void Finish(string status, string reason)
        {
            if (!running)
            {
                return;
            }

            report.status = status;
            report.reason = reason;
            report.runtimeErrors = runtimeErrors;
            report.runtimeErrorMessages = new List<string>(runtimeErrorMessages);
            report.finishedUtc = DateTime.UtcNow.ToString("O");
            report.passed = ((status == "reached_r15" || (progressionAudit && status == "r" + progressionAuditMaxRound + "_completed")) && report.r10BossWarningSeen && report.r10BossSpawned && report.r10BossCleared && report.r10ContinueClicked && report.r11StartedAfterR10 && (!pass13IntegrationValidation || (report.initialMissionAutoOpenObserved && report.initialMissionAutoOpenResolved)) && !report.invisibleBlockerObserved && runtimeErrors == 0) || (status == "r4_shop_recovered" && report.r4RunShopSeen && (report.r4RunShopPurchaseClicked || report.r4RunShopCloseClicked) && (!pass9ChoiceFlowValidation || report.tacticalMissionSelectedByUi) && report.r5StartedAfterRunShop && !report.invisibleBlockerObserved && runtimeErrors == 0);
            File.WriteAllText(OutputPath, JsonUtility.ToJson(report, true));
            pass13IntegrationValidation = false;

            running = false;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            Application.logMessageReceived -= HandleLogMessage;
            MonsterUnit.OnMonsterSpawned -= HandleMonsterSpawned;
            MonsterUnit.OnMonsterKilled -= HandleMonsterKilled;
            MonsterUnit.OnMonsterEscaped -= HandleMonsterEscaped;
            Time.timeScale = previousTimeScale;
            Application.runInBackground = previousRunInBackground;
            Application.targetFrameRate = previousTargetFrameRate;
            QualitySettings.vSyncCount = previousVSyncCount;
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(report.passed ? 0 : 1);
            }
        }

        [Serializable]
        private sealed class ValidationReport
        {
            public string status;
            public string reason;
            public string validationMode;
            public string runShopScenario;
            public float validationTimeScale;
            public string startedUtc;
            public string finishedUtc;
            public bool passed;
            public int runtimeErrors;
            public bool overdriveSelected;
            public bool enteredBattlePreparation;
            public bool initialMissionAutoOpenObserved;
            public bool initialMissionAutoOpenResolved;
            public RoundSnapshot r10Start;
            public int r10BossKillCountAtStart;
            public bool r10BossWarningSeen;
            public string r10BossWarningTitle;
            public bool r10BossSpawned;
            public string r10BossSpawnName;
            public float r10BossSpawnHealth;
            public float r10BossActiveHealth01 = -1f;
            public bool r10ResultObserved;
            public bool r10BossCleared;
            public float r10BossHealthRemaining01 = -1f;
            public int r10EndLife;
            public int r10EndGold;
            public bool r10ContinueClicked;
            public bool r11StartedAfterR10;
            public bool r15Reached;
            public bool actualUiMergeCompleted;
            public int actualUiMergeSpendCount;
            public int actualUiSummonSpendCount;
            public int actualUiGradeUpgradeSpendCount;
            public int actualUiLuckUpgradeSpendCount;
            public bool actualUiRunShopPurchaseCompleted;
            public bool tacticalMissionSelectedByUi;
            public bool tacticalMissionSettlementObserved;
            public string tacticalMissionSettlementResult;
            public bool nextTacticalMissionOfferResolvedByUi;
            public bool invisibleBlockerObserved;
            public bool r4RunShopSeen;
            public bool r4RunShopPurchaseClicked;
            public bool r4RunShopCloseClicked;
            public bool r5StartedAfterRunShop;
            public int finalRound;
            public int finalLife;
            public int finalGold;
            public string lastBlockingChoiceReason;
            public int lastActiveChoicePanelCount;
            public bool lastBattleButtonInteractable;
            public string lastActiveChoicePanelNames;
            public List<RoundSnapshot> roundSnapshots;
            public List<RoundAuditEntry> roundAudits;
            public List<string> runtimeErrorMessages;
            public List<string> actionLog;
        }
        [Serializable]
        private sealed class RoundAuditEntry
        {
            public int round;
            public string kind;
            public RoundSnapshot start;
            public RoundSnapshot end;
            public string outcome;
            public string bossName;
            public float bossMaxHealth;
            public float bossHealthRemaining01 = -1f;
            public bool bossWarningSeen;
            public string bossWarningTitle;
            public bool bossKilled;
            public List<string> choiceFlow;
        }
        [Serializable]
        private sealed class RoundSnapshot
        {
            public int round;
            public string phase;
            public int life;
            public int gold;
            public int boardUnits;
            public int normal;
            public int rare;
            public int epic;
            public int legendary;
            public int mythic;
            public int transcendent;
            public int playerSummons;
            public int merges;
            public string blockingChoiceReason;
            public int targetCount;
            public bool horde;
            public bool battleButtonInteractable;
            public string activeChoicePanels;
            public string bgmClipName;
            public bool bgmPlaying;
        }
    }
}