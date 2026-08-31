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
        private const string OutputFileName = "DefenseGame_Pass2Y_Overdrive_R10_R15.json";
        private const float ValidationTimeScale = 8f;
        private const double ActionDelaySeconds = 0.14d;
        private const double StartupTimeoutSeconds = 15d;
        private const double RunTimeoutSeconds = 180d;
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
        private static bool shopPurchaseAttempted;
        private static DefenseGameController controller;
        private static ValidationReport report;
        private static readonly HashSet<int> RecordedRoundStarts = new HashSet<int>();

        private static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputDirectoryName, OutputFileName));

        [MenuItem("DefenseGame/Validation/Pass 2Y Persistent Overdrive R10-R15 UI Flow")]
        public static void Run()
        {
            if (running)
            {
                return;
            }

            running = true;
            runtimeErrors = 0;
            shopPurchaseAttempted = false;
            controller = null;
            RecordedRoundStarts.Clear();
            blockerObservedAt = -1d;
            noActionBlockObservedAt = -1d;
            report = new ValidationReport
            {
                status = "running",
                validationMode = "EventSystem UI clicks only; no StartRound/debug round jump/reward fixture",
                validationTimeScale = ValidationTimeScale,
                startedUtc = DateTime.UtcNow.ToString("O"),
                roundSnapshots = new List<RoundSnapshot>(),
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
            Application.logMessageReceived += HandleLogMessage;
            MonsterUnit.OnMonsterSpawned -= HandleMonsterSpawned;
            MonsterUnit.OnMonsterSpawned += HandleMonsterSpawned;
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

        private static bool ShouldFinishForOutcome(double now)
        {
            if (controller.CurrentRound >= 15 && controller.IsRoundRunning)
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
                Finish("timeout", "Validation wall-clock timeout before R15.");
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
                    Log("clicked_result_continue_r" + controller.CurrentRound);
                    return;
                }
            }

            if (TryResolveChoice("AugmentChoiceOverlay", "AugmentChoice_")) return;
            if (TryResolveTacticalMission()) return;
            if (TryResolveRunShop()) return;
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

            PrepareUsingOnlyUiClicks();
            if (Click(battleButton))
            {
                Log("clicked_battle_r" + (controller.CurrentRound + 1));
            }
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

            Button normalUpgrade = FindButton("GradeUpgrade_Normal");
            if (normalUpgrade != null && normalUpgrade.gameObject.activeInHierarchy && normalUpgrade.interactable && controller.BoardUnitCount > 0)
            {
                Click(normalUpgrade);
                Log("clicked_normal_upgrade_r" + controller.CurrentRound);
            }
        }

        private static bool TryResolveTacticalMission()
        {
            GameObject overlay = FindObject("TacticalMissionOverlay");
            if (overlay == null || !overlay.activeInHierarchy)
            {
                return false;
            }

            Button laterButton = FindButton("MissionCloseButton");
            if (Click(laterButton))
            {
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

            if (!shopPurchaseAttempted)
            {
                Button offer = overlay.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(candidate => candidate != null && candidate.gameObject.activeInHierarchy && candidate.interactable && candidate.name.StartsWith("RunShopOffer_", StringComparison.Ordinal));
                shopPurchaseAttempted = true;
                if (Click(offer))
                {
                    Log("clicked_choice_" + offer.name + "_r" + controller.CurrentRound);
                    return true;
                }
            }

            Button close = FindButton("RunShopCloseButton");
            if (Click(close))
            {
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
            if (!running || controller == null || monster == null || !monster.IsBoss || controller.CurrentRound != 10)
            {
                return;
            }

            report.r10BossSpawned = true;
            report.r10BossSpawnName = monster.Definition != null ? monster.Definition.displayName : monster.gameObject.name;
            report.r10BossSpawnHealth = monster.MaxHealth;
            report.r10BossActiveHealth01 = monster.MaxHealth > 0f ? Mathf.Clamp01(monster.CurrentHealth / monster.MaxHealth) : -1f;
            Log("r10_boss_spawned_" + report.r10BossSpawnName);
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
                horde = controller.IsCurrentRoundHorde
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
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                runtimeErrors++;
            }
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
            report.finishedUtc = DateTime.UtcNow.ToString("O");
            report.passed = status == "reached_r15" && report.r10BossWarningSeen && report.r10BossSpawned && report.r10BossCleared && report.r10ContinueClicked && report.r11StartedAfterR10 && !report.invisibleBlockerObserved && runtimeErrors == 0;
            File.WriteAllText(OutputPath, JsonUtility.ToJson(report, true));

            running = false;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            Application.logMessageReceived -= HandleLogMessage;
            MonsterUnit.OnMonsterSpawned -= HandleMonsterSpawned;
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
            public float validationTimeScale;
            public string startedUtc;
            public string finishedUtc;
            public bool passed;
            public int runtimeErrors;
            public bool overdriveSelected;
            public bool enteredBattlePreparation;
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
            public bool invisibleBlockerObserved;
            public int finalRound;
            public int finalLife;
            public int finalGold;
            public string lastBlockingChoiceReason;
            public int lastActiveChoicePanelCount;
            public bool lastBattleButtonInteractable;
            public string lastActiveChoicePanelNames;
            public List<RoundSnapshot> roundSnapshots;
            public List<string> actionLog;
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
        }
    }
}