using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DefenseGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame.Editor
{
    public static class DefenseGameBatchPlaytest
    {
        private const int DefaultTargetRuns = 20;
        private const int DefaultTargetRound = 10;
        private const int Phase2ClassicR30Runs = 12;
        private const int Phase2OverdriveR30Runs = 12;
        private const int Phase2ClassicR50Runs = 6;
        private const float BatchTimeScale = 40f;
        private const float BatchFixedDeltaTime = 0.025f;
        private const float BatchMaximumDeltaTime = 0.33f;
        private const double RoundTimeoutSeconds = 45d;
        private const double RunTimeoutSeconds = 300d;
        private const string ScenePath = "Assets/Scenes/DG.unity";
        private const string OutputDirectoryName = "BatchPlaytestResults";
        private const string ClassicOutputFileName = "DefenseGame_Playtest20_ClassicBaseline.json";
        private const string OverdriveOutputFileName = "DefenseGame_Playtest20_Overdrive.json";
        private const string OverdrivePairedOutputFileName = "DefenseGame_Playtest30_OverdrivePaired.json";
        private const string OverdriveFairPairedOutputFileName = "DefenseGame_Playtest30_OverdriveFairPaired.json";
        private const string MissingScriptReportFileName = "RuntimeMissingScripts.json";
        private const string Phase2ClassicR30OutputFileName = "DefenseGame_Phase2_Classic_R30.json";
        private const string Phase2OverdriveR30OutputFileName = "DefenseGame_Phase2_Overdrive_R30.json";
        private const string Phase2ClassicR50OutputFileName = "DefenseGame_Phase2_Classic_R50.json";
        private const string Phase2FClassicRepeatAOutputFileName = "DefenseGame_Phase2F_Classic_R30_RepeatA.json";
        private const string Phase2FClassicRepeatBOutputFileName = "DefenseGame_Phase2F_Classic_R30_RepeatB.json";
        private const string Phase2FOverdriveRepeatAOutputFileName = "DefenseGame_Phase2F_Overdrive_R30_RepeatA.json";
        private const string Phase2FOverdriveRepeatBOutputFileName = "DefenseGame_Phase2F_Overdrive_R30_RepeatB.json";
        private static readonly int[] PressureCheckpointRounds = { 3, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 20, 30 };
        private static string OutputDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputDirectoryName));
        private static string OutputPath => Path.Combine(OutputDirectory, requestedOutputFileName);
        private static string MissingScriptReportPath => Path.Combine(OutputDirectory, MissingScriptReportFileName);

        private static readonly List<RunResult> Results = new List<RunResult>();
        private static CombatGameMode requestedCombatMode = CombatGameMode.Classic;
        private static int requestedRunCount = DefaultTargetRuns;
        private static int requestedTargetRound = DefaultTargetRound;
        private static bool pairedSeedMode;
        private static bool fairStrategyPolicy;
        private static string requestedOutputFileName = ClassicOutputFileName;
        private static DefenseGameController controller;
        private static RunResult current;
        private static int runIndex;
        private static double nextActionTime;
        private static double runStartEditorTime;
        private static double roundStartEditorTime;
        private static int lastObservedRound;
        private static bool waitingRoundEnd;
        private static bool started;
        private static bool previousEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions previousEnterPlayModeOptions;
        private static LogType previousFilterLogType;
        private static float previousAudioListenerVolume = 1f;
        private static bool previousAudioListenerPause;
        private static float previousFixedDeltaTime;
        private static float previousMaximumDeltaTime;
        private static int previousTargetFrameRate;
        private static int previousVSyncCount;
        private static bool previousRunInBackground;
        private static int missingScriptWarningsObserved;
        private static readonly List<string> MissingScriptWarningSamples = new List<string>();
        private static bool r10EncounterTelemetryActive;
        private static double r10BossSpawnEditorTime;
        private static PostBossRoundTelemetry activePostBossRoundTelemetry;

        [MenuItem("DefenseGame/Batch Playtest/Run Human Strategies 20 Total")]
        public static void RunHumanStrategies20()
        {
            RunHumanStrategies(CombatGameMode.Classic, DefaultTargetRuns, false, false, DefaultTargetRound, ClassicOutputFileName);
        }

        [MenuItem("DefenseGame/Batch Playtest/Run Overdrive Human Strategies 20 Total")]
        public static void RunOverdriveHumanStrategies20()
        {
            RunHumanStrategies(CombatGameMode.Overdrive, DefaultTargetRuns, false, false, DefaultTargetRound, OverdriveOutputFileName);
        }

        [MenuItem("DefenseGame/Batch Playtest/Run Overdrive Paired Seeds 30 Total")]
        public static void RunOverdrivePairedStrategies30()
        {
            RunHumanStrategies(CombatGameMode.Overdrive, 30, true, false, DefaultTargetRound, OverdrivePairedOutputFileName);
        }

        [MenuItem("DefenseGame/Batch Playtest/Run Overdrive Fair Paired Seeds 30 Total")]
        public static void RunOverdriveFairPairedStrategies30()
        {
            RunHumanStrategies(CombatGameMode.Overdrive, 30, true, true, DefaultTargetRound, OverdriveFairPairedOutputFileName);
        }

        [MenuItem("DefenseGame/Batch Playtest/Phase2 Classic R30")]
        public static void RunPhase2ClassicR30()
        {
            RunHumanStrategies(CombatGameMode.Classic, Phase2ClassicR30Runs, true, false, 30, Phase2ClassicR30OutputFileName);
        }

        [MenuItem("DefenseGame/Batch Playtest/Phase2 Overdrive R30")]
        public static void RunPhase2OverdriveR30()
        {
            RunHumanStrategies(CombatGameMode.Overdrive, Phase2OverdriveR30Runs, true, false, 30, Phase2OverdriveR30OutputFileName);
        }

        [MenuItem("DefenseGame/Batch Playtest/Phase2F Classic R30 Repeat A")]
        public static void RunPhase2FClassicR30RepeatA()
        {
            RunHumanStrategies(CombatGameMode.Classic, Phase2ClassicR30Runs, true, false, 30, Phase2FClassicRepeatAOutputFileName);
        }

        [MenuItem("DefenseGame/Batch Playtest/Phase2F Classic R30 Repeat B")]
        public static void RunPhase2FClassicR30RepeatB()
        {
            RunHumanStrategies(CombatGameMode.Classic, Phase2ClassicR30Runs, true, false, 30, Phase2FClassicRepeatBOutputFileName);
        }

        [MenuItem("DefenseGame/Batch Playtest/Phase2F Overdrive R30 Repeat A")]
        public static void RunPhase2FOverdriveR30RepeatA()
        {
            RunHumanStrategies(CombatGameMode.Overdrive, Phase2OverdriveR30Runs, true, false, 30, Phase2FOverdriveRepeatAOutputFileName);
        }

        [MenuItem("DefenseGame/Batch Playtest/Phase2F Overdrive R30 Repeat B")]
        public static void RunPhase2FOverdriveR30RepeatB()
        {
            RunHumanStrategies(CombatGameMode.Overdrive, Phase2OverdriveR30Runs, true, false, 30, Phase2FOverdriveRepeatBOutputFileName);
        }

        [MenuItem("DefenseGame/Batch Playtest/Phase2G Classic R30 Repeat A")]
        public static void RunPhase2GClassicR30RepeatA()
        {
            RunHumanStrategies(CombatGameMode.Classic, Phase2ClassicR30Runs, true, false, 30, "DefenseGame_Phase2G_Classic_R30_RepeatA.json");
        }

        [MenuItem("DefenseGame/Batch Playtest/Phase2G Classic R30 Repeat B")]
        public static void RunPhase2GClassicR30RepeatB()
        {
            RunHumanStrategies(CombatGameMode.Classic, Phase2ClassicR30Runs, true, false, 30, "DefenseGame_Phase2G_Classic_R30_RepeatB.json");
        }

        [MenuItem("DefenseGame/Batch Playtest/Phase2G Overdrive R30 Repeat A")]
        public static void RunPhase2GOverdriveR30RepeatA()
        {
            RunHumanStrategies(CombatGameMode.Overdrive, Phase2OverdriveR30Runs, true, false, 30, "DefenseGame_Phase2G_Overdrive_R30_RepeatA.json");
        }

        [MenuItem("DefenseGame/Batch Playtest/Phase2G Overdrive R30 Repeat B")]
        public static void RunPhase2GOverdriveR30RepeatB()
        {
            RunHumanStrategies(CombatGameMode.Overdrive, Phase2OverdriveR30Runs, true, false, 30, "DefenseGame_Phase2G_Overdrive_R30_RepeatB.json");
        }

        [MenuItem("DefenseGame/Batch Playtest/Phase2H Classic R30 Baseline")]
        public static void RunPhase2HClassicR30Baseline()
        {
            RunHumanStrategies(CombatGameMode.Classic, Phase2ClassicR30Runs, true, false, 30, "DefenseGame_Phase2H_Classic_R30_Baseline.json");
        }

        [MenuItem("DefenseGame/Batch Playtest/Phase2H Overdrive R30 Baseline")]
        public static void RunPhase2HOverdriveR30Baseline()
        {
            RunHumanStrategies(CombatGameMode.Overdrive, Phase2OverdriveR30Runs, true, false, 30, "DefenseGame_Phase2H_Overdrive_R30_Baseline.json");
        }

        [MenuItem("DefenseGame/Batch Playtest/Phase2 Classic R50")]
        public static void RunPhase2ClassicR50()
        {
            RunHumanStrategies(CombatGameMode.Classic, Phase2ClassicR50Runs, true, false, 50, Phase2ClassicR50OutputFileName);
        }

        private static void RunHumanStrategies(CombatGameMode combatMode, int runCount, bool usePairedSeeds, bool useFairPolicy, int targetRound, string outputFileName)
        {
            requestedCombatMode = combatMode;
            requestedRunCount = Mathf.Max(3, runCount);
            requestedTargetRound = Mathf.Max(10, targetRound);
            pairedSeedMode = usePairedSeeds;
            fairStrategyPolicy = useFairPolicy;
            requestedOutputFileName = string.IsNullOrWhiteSpace(outputFileName) ? ClassicOutputFileName : outputFileName;
            Results.Clear();
            controller = null;
            current = null;
            runIndex = 0;
            nextActionTime = 0d;
            runStartEditorTime = 0d;
            roundStartEditorTime = 0d;
            lastObservedRound = 0;
            waitingRoundEnd = false;
            started = false;
            missingScriptWarningsObserved = 0;
            MissingScriptWarningSamples.Clear();
            UnsubscribeR10EncounterTelemetry();

            Directory.CreateDirectory(OutputDirectory);
            if (File.Exists(OutputPath))
            {
                File.Delete(OutputPath);
            }

            if (File.Exists(OutputPath + ".partial"))
            {
                File.Delete(OutputPath + ".partial");
            }

            if (File.Exists(MissingScriptReportPath))
            {
                File.Delete(MissingScriptReportPath);
            }

            previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            previousFilterLogType = Debug.unityLogger.filterLogType;
            previousAudioListenerVolume = AudioListener.volume;
            previousAudioListenerPause = AudioListener.pause;
            previousFixedDeltaTime = Time.fixedDeltaTime;
            previousMaximumDeltaTime = Time.maximumDeltaTime;
            previousTargetFrameRate = Application.targetFrameRate;
            previousVSyncCount = QualitySettings.vSyncCount;
            previousRunInBackground = Application.runInBackground;
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Time.fixedDeltaTime = BatchFixedDeltaTime;
            Time.maximumDeltaTime = BatchMaximumDeltaTime;
            Application.targetFrameRate = 240;
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Debug.unityLogger.filterLogType = LogType.Error;
            Application.logMessageReceived -= HandleLogMessage;
            Application.logMessageReceived += HandleLogMessage;
            SubscribeR10EncounterTelemetry();

            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            controller?.SetRunContentSeedOverride(null);
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                nextActionTime = EditorApplication.timeSinceStartup + 1.0d;
            }
        }

        private static void ApplyBatchSpeedSettings()
        {
            if (!Mathf.Approximately(Time.timeScale, BatchTimeScale))
            {
                Time.timeScale = BatchTimeScale;
            }

            if (!Mathf.Approximately(Time.fixedDeltaTime, BatchFixedDeltaTime))
            {
                Time.fixedDeltaTime = BatchFixedDeltaTime;
            }

            if (!Mathf.Approximately(Time.maximumDeltaTime, BatchMaximumDeltaTime))
            {
                Time.maximumDeltaTime = BatchMaximumDeltaTime;
            }

            Application.targetFrameRate = 240;
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            ApplyBatchSpeedSettings();
            if (EditorApplication.timeSinceStartup < nextActionTime)
            {
                return;
            }

            if (!started)
            {
                controller = UnityEngine.Object.FindObjectOfType<DefenseGameController>();
                if (controller == null)
                {
                    nextActionTime = EditorApplication.timeSinceStartup + 0.5d;
                    return;
                }

                ApplyBatchSpeedSettings();
                started = true;
                WriteRuntimeMissingScriptReport("batch_start");
                StartRun();
                return;
            }

            if (controller == null)
            {
                FinishAll("controller_missing");
                return;
            }

            if (current != null && EditorApplication.timeSinceStartup - runStartEditorTime > RunTimeoutSeconds)
            {
                current.timeout = true;
                current.notes.Add("run_timeout_R" + Mathf.Max(1, controller.CurrentRound));
                CompleteRun();
                return;
            }
            if (current.runtimeErrorCount > 0)
            {
                current.technicalFailure = true;
                current.notes.Add("runtime_error_R" + controller.CurrentRound);
                CompleteRun();
                return;
            }

            if (!ValidateRunInvariants())
            {
                CompleteRun();
                return;
            }

            ResolveBlockingChoices();

            if (waitingRoundEnd)
            {
                if (controller.Life <= 0)
                {
                    FinalizeActivePostBossRoundTelemetry(false);
                    current.notes.Add("life_zero_during_round_R" + lastObservedRound);
                    CompleteRun();
                    return;
                }

                if (controller.IsRoundRunning)
                {
                    ObserveActivePostBossRoundTelemetry();
                    ObserveActiveBossHealth();
                    TryUsePreferredFateCardByStrategy();
                    if (EditorApplication.timeSinceStartup - roundStartEditorTime > RoundTimeoutSeconds)
                    {
                        current.timeout = true;
                        current.r10BossHealthRemaining01 = ResolveRemainingBossHealth01();
                        current.notes.Add("round_timeout_R" + lastObservedRound + "_bossHp_" + FormatFloat(current.r10BossHealthRemaining01));
                        CompleteRun();
                    }

                    nextActionTime = EditorApplication.timeSinceStartup + 0.15d;
                    return;
                }

                FinalizeActivePostBossRoundTelemetry(true);
                current.reachedRound = Mathf.Max(current.reachedRound, controller.CurrentRound);
                current.endGold = controller.Gold;
                current.endLife = controller.Life;
                current.r10BossHealthRemaining01 = ResolveRemainingBossHealth01();
                if (current.activeBossRound == lastObservedRound)
                {
                    FinalizeActiveBossAttempt();
                }
                CaptureMilestoneSnapshotIfNeeded();
                if (lastObservedRound >= requestedTargetRound)
                {
                    CompleteRun();
                    return;
                }

                waitingRoundEnd = false;
                nextActionTime = EditorApplication.timeSinceStartup + 0.35d;
                return;
            }

            if (controller.Life <= 0 || controller.CurrentRound > requestedTargetRound)
            {
                CompleteRun();
                return;
            }

            if (!ResolveBlockingChoices())
            {
                TrackPreparationFingerprint();
                nextActionTime = EditorApplication.timeSinceStartup + 0.15d;
                return;
            }

            ResetPreparationFingerprint();
            TryUsePreferredFateCardByStrategy();
            TryUseFateSummonQualityBoost();
            TryUseFateSurvivalInCrisis();
            TryUpgradeGradeByStrategy();
            TryMergeReadyUltimateRecipe();
            ExecutePrepPolicy();
            CaptureMilestoneSnapshotIfNeeded();
            StartNextRound();
        }

        private static void StartRun()
        {
            ResetR10EncounterTelemetry();
            int seedIndex = pairedSeedMode ? runIndex / 3 : runIndex;
            int contentSeed = 90210 + seedIndex * 7919;
            current = new RunResult
            {
                index = runIndex + 1,
                strategy = ResolveStrategy(runIndex),
                contentSeed = contentSeed,
                startGold = controller.Gold
            };

            controller.SetRunContentSeedOverride(null);
            controller.ResetRunForRetry();
            if (controller.DailyFateCupEnabled)
            {
                controller.TrySetDailyFateCupEnabled(false);
            }

            if (!controller.TrySetCombatMode(requestedCombatMode) &&
                controller.CurrentCombatMode != requestedCombatMode)
            {
                current.notes.Add("combat_mode_switch_failed_" + requestedCombatMode);
            }

            controller.SetRunContentSeedOverride(contentSeed);
            controller.ResetRunForRetry();
            SubscribeRunTrace();

            current.bossForecastBet = BossForecastBet.None.ToString();

            runStartEditorTime = EditorApplication.timeSinceStartup;
            current.startGold = controller.Gold;
            lastObservedRound = controller.CurrentRound;
            waitingRoundEnd = false;
            activePostBossRoundTelemetry = null;
            nextActionTime = EditorApplication.timeSinceStartup + 0.3d;
        }

        private static string ResolveStrategy(int index)
        {
            int strategyIndex = index % 3;
            if (pairedSeedMode)
            {
                int seedIndex = index / 3;
                strategyIndex = (strategyIndex + seedIndex) % 3;
            }

            switch (strategyIndex)
            {
                case 0:
                    return "summon-heavy";
                case 1:
                    return "balanced";
                default:
                    return "shop-save";
            }
        }

        private static BossForecastBet ResolveBossForecastBet(string strategy)
        {
            if (strategy == "summon-heavy")
            {
                return BossForecastBet.Supply;
            }

            if (strategy == "balanced")
            {
                return BossForecastBet.Build;
            }

            return BossForecastBet.Tactical;
        }

        private static void CompleteRun()
        {
            ObserveActivePostBossRoundTelemetry();
            FinalizeActivePostBossRoundTelemetry(false);
            ObserveActiveBossHealth();
            FinalizeActiveBossAttempt();
            bool bossAttemptAccountingValid = ValidateBossAttemptAccounting(requireFinalizedWhenNoActive: true);
            current.reachedRound = Mathf.Max(current.reachedRound, controller.CurrentRound);
            current.endGold = controller.Gold;
            current.endLife = controller.Life;
            current.r10BossHealthRemaining01 = ResolveRemainingBossHealth01();
            if (current.r10BossHealthRemaining01 <= 0f && current.lastObservedBossHealth01 >= 0f)
            {
                current.r10BossHealthRemaining01 = current.lastObservedBossHealth01;
            }
            current.totalSummons = controller.RunTotalPlayerSummons;
            current.totalMerges = controller.RunTotalMerges;
            current.totalGradeUpgradeLevels = ResolveTotalGradeUpgradeLevels();
            current.totalLeakDamage = controller.RunTotalLeakDamage;
            CopyIntCounts(controller.RunLeakDamageByRound, current.leakDamageByRound);
            CopyIntCounts(controller.RunEscapedMonsterCountByRound, current.escapedMonsterCountByRound);
            TacticalMissionSystem missionSystem = UnityEngine.Object.FindObjectOfType<TacticalMissionSystem>();
            current.missionCompletedCount = missionSystem != null ? missionSystem.CompletedMissionCount : current.missionCompletedCount;
            CaptureFateCardSnapshot("complete");
            FinalizeRunTrace();
            UnsubscribeRunTrace();
            current.bossForecastBonusScore = controller.BossForecastBonusScore;
            current.bossForecastSuccess = controller.BossForecastBonusScore > 0;
            current.technicalFailure = current.runtimeErrorCount > 0 || current.timeout || current.softLock || current.invariantFailure || !bossAttemptAccountingValid;
            current.victory = controller.Life > 0 && lastObservedRound >= requestedTargetRound && !current.technicalFailure;
            current.defeat = controller.Life <= 0 && !current.technicalFailure;
            if (current.defeat)
            {
                current.gameplayDefeatRound = Mathf.Max(1, controller.CurrentRound);
            }
            if (current.technicalFailure && current.technicalFailureRound <= 0)
            {
                current.technicalFailureRound = Mathf.Max(1, controller.CurrentRound);
            }
            if (!current.victory && !current.defeat && !current.technicalFailure)
            {
                current.technicalFailure = true;
                current.technicalFailureRound = Mathf.Max(1, controller.CurrentRound);
                current.notes.Add("nonterminal_completion_R" + controller.CurrentRound);
            }
            if (lastObservedRound >= 4 && current.bossForecastBet == BossForecastBet.None.ToString())
            {
                current.validationCoverageWarnings.Add("boss_forecast_not_selected_by_R4");
            }
            if (current.missionOfferObserved && current.missionChoiceCount <= 0)
            {
                current.validationCoverageWarnings.Add("mission_offer_not_selected");
            }
            CaptureMilestoneSnapshotIfNeeded();
            Results.Add(current);
            File.WriteAllText(OutputPath + ".partial", BuildJson("partial"), Encoding.UTF8);

            runIndex++;
            if (runIndex >= requestedRunCount)
            {
                FinishAll("complete");
                return;
            }

            StartRun();
        }

        private static void CopyIntCounts(IReadOnlyDictionary<int, int> source, Dictionary<int, int> destination)
        {
            if (destination == null) return;
            destination.Clear();
            if (source == null) return;
            foreach (KeyValuePair<int, int> pair in source)
            {
                if (pair.Key > 0 && pair.Value > 0) destination[pair.Key] = pair.Value;
            }
        }

        private static void SubscribeR10EncounterTelemetry()
        {
            MonsterUnit.OnMonsterSpawned -= HandleR10MonsterSpawned;
            MonsterUnit.OnMonsterKilled -= HandleR10MonsterKilled;
            MonsterUnit.OnMonsterEscaped -= HandleR10MonsterEscaped;
            DefenderUnit.OnDamageDealt -= HandleR10DamageDealt;
            MonsterUnit.OnMonsterSpawned += HandleR10MonsterSpawned;
            MonsterUnit.OnMonsterKilled += HandleR10MonsterKilled;
            MonsterUnit.OnMonsterEscaped += HandleR10MonsterEscaped;
            DefenderUnit.OnDamageDealt += HandleR10DamageDealt;
        }

        private static void UnsubscribeR10EncounterTelemetry()
        {
            MonsterUnit.OnMonsterSpawned -= HandleR10MonsterSpawned;
            MonsterUnit.OnMonsterKilled -= HandleR10MonsterKilled;
            MonsterUnit.OnMonsterEscaped -= HandleR10MonsterEscaped;
            DefenderUnit.OnDamageDealt -= HandleR10DamageDealt;
        }

        private static void ResetR10EncounterTelemetry()
        {
            r10EncounterTelemetryActive = false;
            r10BossSpawnEditorTime = -1d;
        }

        private static bool IsTrackingR10Encounter()
        {
            return r10EncounterTelemetryActive && current != null && controller != null &&
                current.activeBossRound == 10 && controller.CurrentRound == 10;
        }

        private static void HandleR10MonsterSpawned(MonsterUnit monster)
        {
            if (!IsTrackingR10Encounter() || monster == null)
            {
                return;
            }

            if (!monster.IsBoss)
            {
                current.r10SupportSpawnCount++;
                return;
            }

            if (r10BossSpawnEditorTime >= 0d)
            {
                return;
            }

            r10BossSpawnEditorTime = EditorApplication.timeSinceStartup;
            current.r10LifeAtBossSpawn = controller.Life;
            current.r10LeakDamageBeforeBossSpawn = Mathf.Max(0, current.r10LifeAtRoundStart - controller.Life);
        }

        private static void HandleR10MonsterKilled(MonsterUnit monster)
        {
            if (IsTrackingR10Encounter() && r10BossSpawnEditorTime < 0d && monster != null && !monster.IsBoss)
            {
                current.r10SupportKillsBeforeBossSpawn++;
            }
        }

        private static void HandleR10MonsterEscaped(MonsterUnit monster)
        {
            if (IsTrackingR10Encounter() && r10BossSpawnEditorTime < 0d && monster != null && !monster.IsBoss)
            {
                current.r10SupportEscapesBeforeBossSpawn++;
            }
        }

        private static void HandleR10DamageDealt(DefenderUnit source, MonsterUnit target, float damage, bool critical)
        {
            if (!IsTrackingR10Encounter() || r10BossSpawnEditorTime < 0d || current.r10BossFirstDamagedSeconds >= 0f ||
                target == null || !target.IsBoss || target.MaxHealth <= 0f)
            {
                return;
            }

            current.r10BossFirstDamagedSeconds = Mathf.Max(0f, (float)(EditorApplication.timeSinceStartup - r10BossSpawnEditorTime));
            current.r10BossHealthAtFirstDamage01 = Mathf.Clamp01(target.CurrentHealth / target.MaxHealth);
        }

        private static void FinishAll(string status)
        {
            EditorApplication.update -= Tick;
            UnsubscribeRunTrace();
            UnsubscribeR10EncounterTelemetry();
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            controller?.SetRunContentSeedOverride(null);

            string json = BuildJson(status);
            File.WriteAllText(OutputPath, json, Encoding.UTF8);
            WriteRuntimeMissingScriptReport("batch_finish");
            Time.timeScale = 1f;
            Time.fixedDeltaTime = previousFixedDeltaTime;
            Time.maximumDeltaTime = previousMaximumDeltaTime;
            AudioListener.volume = previousAudioListenerVolume;
            AudioListener.pause = previousAudioListenerPause;
            Application.targetFrameRate = previousTargetFrameRate;
            Application.runInBackground = previousRunInBackground;
            QualitySettings.vSyncCount = previousVSyncCount;
            Debug.unityLogger.filterLogType = previousFilterLogType;
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
            Application.logMessageReceived -= HandleLogMessage;
            Debug.Log("[DefenseGameBatchPlaytest] wrote " + OutputPath + "\n" + json);
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(status == "complete" ? 0 : 1);
            }
        }

        private static void SubscribeRunTrace()
        {
            if (controller == null)
            {
                return;
            }

            controller.OnUnitSummoned -= RecordSummonedUnitTrace;
            controller.OnUnitSummoned += RecordSummonedUnitTrace;
        }

        private static void UnsubscribeRunTrace()
        {
            if (controller != null)
            {
                controller.OnUnitSummoned -= RecordSummonedUnitTrace;
            }
        }

        private static void RecordSummonedUnitTrace(CharacterDefinition definition)
        {
            if (current == null || definition == null)
            {
                return;
            }

            current.summonSequenceTrace.Add("R" + Mathf.Max(1, controller.CurrentRound) + ":" + definition.id + ":" + (int)definition.grade);
        }

        private static void FinalizeRunTrace()
        {
            if (current == null)
            {
                return;
            }

            current.summonSequenceHash = ComputeTraceHash(current.summonSequenceTrace);
            current.mergeSequenceHash = ComputeTraceHash(current.mergeTrace);
            current.augmentChoiceIdsHash = ComputeTraceHash(current.augmentChoiceTrace);
            current.missionChoiceIdsHash = ComputeTraceHash(current.missionChoiceTrace);
            current.shopPurchaseIdsHash = ComputeTraceHash(current.shopPurchaseTrace);
            current.runContentChannels.Clear();
            if (controller != null && controller.RunContentRandom != null)
            {
                foreach (RunContentRandomChannel channel in System.Enum.GetValues(typeof(RunContentRandomChannel)))
                {
                    RunContentChannelTrace trace = new RunContentChannelTrace
                    {
                        channel = channel.ToString(),
                        seed = controller.RunContentRandom.GetChannelSeed(channel),
                        drawCount = controller.RunContentRandom.GetDrawCount(channel),
                        outcomeHash = controller.RunContentRandom.GetOutcomeHash(channel),
                        tracePrefix = string.Join(" | ", controller.RunContentRandom.GetTracePrefix(channel))
                    };
                    current.runContentChannels.Add(trace);
                }
            }
        }

        private static string ComputeTraceHash(List<string> trace)
        {
            const ulong OffsetBasis = 14695981039346656037UL;
            const ulong Prime = 1099511628211UL;
            ulong hash = OffsetBasis;
            if (trace != null)
            {
                for (int i = 0; i < trace.Count; i++)
                {
                    string entry = trace[i] ?? string.Empty;
                    for (int c = 0; c < entry.Length; c++)
                    {
                        hash ^= entry[c];
                        hash *= Prime;
                    }

                    hash ^= 10;
                    hash *= Prime;
                }
            }

            return hash.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static bool ValidateRunInvariants()
        {
            if (controller == null || current == null)
            {
                return false;
            }

            bool valid = controller.Gold >= 0 && controller.Life <= controller.MaxLife &&
                         (controller.BoardCapacity <= 0 || controller.BoardUnitCount <= controller.BoardCapacity);
            valid &= ValidateBossAttemptAccounting(requireFinalizedWhenNoActive: false);
            foreach (CharacterGrade grade in new[] { CharacterGrade.Normal, CharacterGrade.Rare, CharacterGrade.Epic, CharacterGrade.Legendary, CharacterGrade.Mythic, CharacterGrade.Transcendent })
            {
                valid &= controller.GetGradeUpgradeLevel(grade) <= DefenseGameController.GradeUpgradeMaximumLevel;
            }

            if (controller.CurrentRound < current.lastControllerRound)
            {
                valid = false;
            }

            current.lastControllerRound = controller.CurrentRound;
            if (!valid)
            {
                current.invariantFailure = true;
                current.notes.Add("invariant_failure_R" + controller.CurrentRound);
            }

            return valid;
        }
        private static bool ResolveBlockingChoices()
        {
            if (controller == null || current == null)
            {
                return false;
            }

            string blockingChoiceReason = controller.BlockingChoiceReason;
            if (blockingChoiceReason != "None")
            {
                current.lastBlockingChoiceReason = blockingChoiceReason;
            }

            bool resolved = true;
            if (controller.CanChooseBossForecastBet)
            {
                BossForecastBet forecast = ResolveBossForecastBet(current.strategy);
                if (controller.TryChooseBossForecastBet(forecast))
                {
                    current.bossForecastBet = forecast.ToString();
                    current.notes.Add("boss_forecast_R" + controller.CurrentRound + "_" + forecast);
                }
                else
                {
                    resolved = false;
                }
            }

            TacticalMissionSystem missionSystem = UnityEngine.Object.FindObjectOfType<TacticalMissionSystem>();
            if (missionSystem != null && !missionSystem.HasActiveMissionSelection && missionSystem.MissionOfferCount > 0)
            {
                current.missionOfferObserved = true;
                if (!TryChooseMissionByStrategy(missionSystem))
                {
                    resolved = false;
                }
                else
                {
                    current.missionChoiceCount++;
                }
            }

            if (GameObject.Find("AugmentChoiceOverlay")?.activeInHierarchy == true)
            {
                if (!ChooseAugmentIfOpen())
                {
                    resolved = false;
                }
                else
                {
                    current.augmentChoiceCount++;
                }
            }

            if (GameObject.Find("RunShopOverlay")?.activeInHierarchy == true)
            {
                if (current.lastShopOpenRound != controller.CurrentRound)
                {
                    current.shopOpenCount++;
                    current.lastShopOpenRound = controller.CurrentRound;
                }
                HandleShopIfOpen();
                if (GameObject.Find("RunShopOverlay")?.activeInHierarchy == true)
                {
                    resolved = false;
                }
            }

            if (controller.LuckySummonChoiceOpen)
            {
                if (!TryResolveLuckyChoiceByStrategy())
                {
                    resolved = false;
                }
                else
                {
                    current.luckySummonChoiceCount++;
                }
            }

            if (IsFateChoiceVisible())
            {
                if (!TryResolveFateChoice())
                {
                    resolved = false;
                }
            }

            if (IsResultOverlayOpen())
            {
                CloseResultOverlayIfOpen();
                if (IsResultOverlayOpen())
                {
                    resolved = false;
                }
            }

            return resolved && !controller.IsBlockingChoiceOpen;
        }

        private static bool TryChooseMissionByStrategy(TacticalMissionSystem missionSystem)
        {
            if (missionSystem == null || missionSystem.MissionOfferCount <= 0)
            {
                return false;
            }

            string[] preferred = current.strategy == "summon-heavy"
                ? new[] { "growth", "balanced", "safety" }
                : current.strategy == "shop-save"
                    ? new[] { "economy", "safety", "balanced" }
                    : new[] { "safety", "balanced", "growth" };
            for (int p = 0; p < preferred.Length; p++)
            {
                for (int i = 0; i < missionSystem.MissionOfferCount; i++)
                {
                    if (string.Equals(missionSystem.GetMissionOfferAutomationTag(i), preferred[p], StringComparison.Ordinal) && missionSystem.TrySelectMission(i))
                    {
                        current.notes.Add("mission_" + preferred[p]);
                        current.missionChoiceTrace.Add("R" + Mathf.Max(1, controller.CurrentRound) + ":" + preferred[p] + ":" + i);
                        return true;
                    }
                }
            }

            if (missionSystem.TrySelectMission(0))
            {
                current.missionChoiceTrace.Add("R" + Mathf.Max(1, controller.CurrentRound) + ":fallback:0");
                return true;
            }

            return false;
        }

        private static bool TryResolveLuckyChoiceByStrategy()
        {
            LuckySummonChoice choice = current.strategy == "summon-heavy"
                ? LuckySummonChoice.SafeRare
                : current.strategy == "balanced" ? LuckySummonChoice.MergeLink : LuckySummonChoice.Jackpot;
            if (controller.TryResolveLuckySummonChoice(choice))
            {
                return true;
            }

            if (controller.TryResolveLuckySummonChoice(LuckySummonChoice.MergeLink) ||
                controller.TryResolveLuckySummonChoice(LuckySummonChoice.SafeRare) ||
                controller.TryResolveLuckySummonChoice(LuckySummonChoice.Jackpot))
            {
                return true;
            }

            controller.CancelLuckySummonChoice();
            return !controller.LuckySummonChoiceOpen;
        }

        private static bool IsFateChoiceVisible()
        {
            GameObject backdrop = GameObject.Find("FateChoiceBackdrop");
            return backdrop != null && backdrop.activeInHierarchy;
        }

        private static bool TryResolveFateChoice()
        {
            if (controller == null || !controller.CanOpenFateCard)
            {
                return false;
            }

            int choice = FindPreferredFateChoiceIndex(ResolveFateCardPreferences());
            return choice >= 0 && controller.TryActivateFateCardChoice(choice);
        }

        private static bool IsResultOverlayOpen()
        {
            GameObject overlay = GameObject.Find("RoundResultOverlay");
            return overlay != null && overlay.activeInHierarchy;
        }

        private static void TrackPreparationFingerprint()
        {
            if (current == null || controller == null || controller.IsRoundRunning)
            {
                return;
            }

            string fingerprint = controller.CurrentRound + "|" + controller.Gold + "|" + controller.Life + "|" + controller.BoardUnitCount + "|" +
                                 controller.IsBlockingChoiceOpen + "|" + IsResultOverlayOpen() + "|" + IsFateChoiceVisible();
            if (string.Equals(current.lastPreparationFingerprint, fingerprint, StringComparison.Ordinal))
            {
                current.preparationFingerprintRepeats++;
            }
            else
            {
                current.lastPreparationFingerprint = fingerprint;
                current.preparationFingerprintRepeats = 0;
            }

            if (current.preparationFingerprintRepeats >= 20)
            {
                current.softLock = true;
                current.notes.Add("soft_lock_" + fingerprint);
                CompleteRun();
            }
        }

        private static void ResetPreparationFingerprint()
        {
            if (current != null)
            {
                current.lastPreparationFingerprint = string.Empty;
                current.preparationFingerprintRepeats = 0;
            }
        }

        private static void TryUpgradeGradeByStrategy()
        {
            if (controller == null || current == null || controller.IsRoundRunning)
            {
                return;
            }

            if (current.strategy == "balanced" && controller.BoardUnitCount < MinimumBoardUnits())
            {
                return;
            }

            int reserve = ResolveUpgradeGoldReserve();
            CharacterGrade bestGrade = CharacterGrade.Normal;
            int bestCount = 0;
            int bestCost = int.MaxValue;
            foreach (CharacterGrade grade in new[] { CharacterGrade.Transcendent, CharacterGrade.Mythic, CharacterGrade.Legendary, CharacterGrade.Epic, CharacterGrade.Rare, CharacterGrade.Normal })
            {
                int count = controller.CountUnitsOfGrade(grade);
                if (count <= 0)
                {
                    continue;
                }

                int cost = controller.GetGradeUpgradeCost(grade);
                if (!controller.CanUpgradeGrade(grade) || controller.Gold - cost < reserve)
                {
                    continue;
                }

                bool isBetterCandidate = count > bestCount ||
                    (count == bestCount && (cost < bestCost ||
                        (cost == bestCost && (int)grade < (int)bestGrade)));
                if (isBetterCandidate)
                {
                    bestGrade = grade;
                    bestCount = count;
                    bestCost = cost;
                }
            }

            if (bestCount > 0 && controller.TryUpgradeGrade(bestGrade))
            {
                current.gradeUpgradePurchaseCount++;
                current.firstGradeUpgradeRound = current.firstGradeUpgradeRound <= 0 ? Mathf.Max(1, controller.CurrentRound + 1) : current.firstGradeUpgradeRound;
                IncrementGradeUpgradeCount(bestGrade);
            }
        }

        private static int ResolveUpgradeGoldReserve()
        {
            if (controller == null || current == null)
            {
                return 0;
            }

            if (current.strategy == "summon-heavy")
            {
                int nextRound = controller.CurrentRound + 1;
                return controller.SummonCost * (nextRound <= 5 ? 2 : 1);
            }

            return ResolveGoldReserve();
        }

        private static void IncrementGradeUpgradeCount(CharacterGrade grade)
        {
            switch (grade)
            {
                case CharacterGrade.Normal: current.gradeUpgradeNormalCount++; break;
                case CharacterGrade.Rare: current.gradeUpgradeRareCount++; break;
                case CharacterGrade.Epic: current.gradeUpgradeEpicCount++; break;
                case CharacterGrade.Legendary: current.gradeUpgradeLegendaryCount++; break;
                case CharacterGrade.Mythic: current.gradeUpgradeMythicCount++; break;
                case CharacterGrade.Transcendent: current.gradeUpgradeTranscendentCount++; break;
            }
        }

        private static void TryMergeReadyUltimateRecipe()
        {
            if (controller == null || current == null || controller.IsRoundRunning || controller.ReadyUltimateRecipeCount <= 0)
            {
                return;
            }

            if (current.strategy == "summon-heavy" && controller.EmptySlotCount > 1)
            {
                return;
            }

            UltimateRecipeOption[] options = controller.GetReadyUltimateRecipeOptions();
            if (options.Length > 0 && controller.TryMergeUltimateRecipe(options[0].recipeName))
            {
                current.ultimateRecipeMergeCount++;
                current.highestMergeGrade = Mathf.Max(current.highestMergeGrade, (int)CharacterGrade.Transcendent);
            }
        }

        private static void BeginPostBossRoundTelemetry(int round)
        {
            activePostBossRoundTelemetry = null;
            if (current == null || controller == null || round < 11 || round > 15)
            {
                return;
            }

            activePostBossRoundTelemetry = new PostBossRoundTelemetry
            {
                round = round,
                reached = true,
                lifeAtStart = controller.Life,
                goldAtStart = controller.Gold,
                boardUnitCountAtStart = controller.BoardUnitCount,
                boardCapacityAtStart = controller.BoardCapacity,
                highestOwnedGradeAtStart = ResolveHighestOwnedGrade(),
                totalSummonsAtStart = controller.RunTotalPlayerSummons,
                totalMergesAtStart = controller.RunTotalMerges,
                totalGradeUpgradeLevelsAtStart = ResolveTotalGradeUpgradeLevels(),
                targetMonsterCount = controller.RoundTargetCount,
                isHordeRound = controller.IsCurrentRoundHorde,
                isBossRound = controller.IsBossRound,
                isMidBossRound = controller.IsMidBossRound,
                firstLeakSeconds = -1f
            };
            current.postBossRoundTelemetry[round] = activePostBossRoundTelemetry;
            ObserveActivePostBossRoundTelemetry();
        }

        private static void ObserveActivePostBossRoundTelemetry()
        {
            if (activePostBossRoundTelemetry == null || controller == null)
            {
                return;
            }

            PostBossRoundTelemetry telemetry = activePostBossRoundTelemetry;
            telemetry.totalSpawned = Mathf.Max(telemetry.totalSpawned, controller.RoundSpawnedMonsterCount);
            telemetry.totalKilled = Mathf.Max(telemetry.totalKilled, controller.RoundKilledMonsterCount);
            telemetry.totalEscaped = ResolveRoundCount(controller.RunEscapedMonsterCountByRound, telemetry.round);
            telemetry.leakDamage = ResolveRoundCount(controller.RunLeakDamageByRound, telemetry.round);
            telemetry.peakActiveMonsters = Mathf.Max(telemetry.peakActiveMonsters, controller.RoundPeakActiveMonsterCount, MonsterUnit.ActiveCount);
            if (telemetry.firstLeakSeconds < 0f && telemetry.leakDamage > 0)
            {
                telemetry.firstLeakSeconds = Mathf.Max(0f, (float)(EditorApplication.timeSinceStartup - roundStartEditorTime));
            }
        }

        private static void FinalizeActivePostBossRoundTelemetry(bool actuallyCleared)
        {
            if (activePostBossRoundTelemetry == null || controller == null)
            {
                return;
            }

            ObserveActivePostBossRoundTelemetry();
            PostBossRoundTelemetry telemetry = activePostBossRoundTelemetry;
            telemetry.actuallyCleared = actuallyCleared && controller.Life > 0;
            telemetry.lifeAtEnd = controller.Life;
            telemetry.goldAtEnd = controller.Gold;
            telemetry.boardUnitCountAtEnd = controller.BoardUnitCount;
            telemetry.boardCapacityAtEnd = controller.BoardCapacity;
            telemetry.highestOwnedGradeAtEnd = ResolveHighestOwnedGrade();
            telemetry.totalSummonsAtEnd = controller.RunTotalPlayerSummons;
            telemetry.totalMergesAtEnd = controller.RunTotalMerges;
            telemetry.totalGradeUpgradeLevelsAtEnd = ResolveTotalGradeUpgradeLevels();
            telemetry.roundDurationSeconds = Mathf.Max(0f, (float)(EditorApplication.timeSinceStartup - roundStartEditorTime));
            activePostBossRoundTelemetry = null;
        }

        private static int ResolveRoundCount(IReadOnlyDictionary<int, int> counts, int round)
        {
            return counts != null && counts.TryGetValue(round, out int value) ? Mathf.Max(0, value) : 0;
        }
        private static void CaptureMilestoneSnapshotIfNeeded()
        {
            if (current == null || controller == null)
            {
                return;
            }

            int round = controller.CurrentRound;
            if (Array.IndexOf(PressureCheckpointRounds, round) < 0 || current.milestones.ContainsKey(round))
            {
                return;
            }

            current.milestones[round] = new MilestoneSnapshot
            {
                life = controller.Life,
                gold = controller.Gold,
                boardUnitCount = controller.BoardUnitCount,
                boardCapacity = controller.BoardCapacity,
                highestOwnedGrade = ResolveHighestOwnedGrade(),
                totalSummons = controller.RunTotalPlayerSummons,
                totalMerges = controller.RunTotalMerges,
                totalGradeUpgradeLevels = ResolveTotalGradeUpgradeLevels(),
                summonCost = controller.SummonCost,
                ultimateRecipeCount = controller.ReadyUltimateRecipeCount,
                targetMonsterCount = controller.RoundTargetCount,
                leakDamage = controller.RunTotalLeakDamage,
                isHordeRound = controller.IsCurrentRoundHorde,
                isBossRound = controller.IsBossRound,
                isMidBossRound = controller.IsMidBossRound
            };
        }

        private static int ResolveHighestOwnedGrade()
        {
            int highest = (int)CharacterGrade.Normal;
            DefenderUnit[] units = UnityEngine.Object.FindObjectsOfType<DefenderUnit>();
            for (int i = 0; i < units.Length; i++)
            {
                if (units[i] != null && !units[i].IsTemporarySummon)
                {
                    highest = Mathf.Max(highest, (int)units[i].Grade);
                }
            }
            return highest;
        }

        private static int ResolveTotalGradeUpgradeLevels()
        {
            int total = 0;
            foreach (CharacterGrade grade in new[] { CharacterGrade.Normal, CharacterGrade.Rare, CharacterGrade.Epic, CharacterGrade.Legendary, CharacterGrade.Mythic, CharacterGrade.Transcendent })
            {
                total += controller.GetGradeUpgradeLevel(grade);
            }
            return total;
        }

        private static void ExecutePrepPolicy()
        {
            TryMaintainHumanMergeTempo();

            int reserve = ResolveGoldReserve();
            int safetyLoops = 0;
            while (safetyLoops < 24)
            {
                int summonCost = controller.SummonCost;
                bool canSpend = controller.Gold >= summonCost &&
                                (controller.Gold - summonCost >= reserve || controller.BoardUnitCount < MinimumBoardUnits());
                if (!canSpend)
                {
                    break;
                }

                if (controller.EmptySlotCount <= 0 && !TryMergeOneAvailable())
                {
                    break;
                }

                if (controller.EmptySlotCount <= 0)
                {
                    break;
                }

                if (!controller.TrySummon())
                {
                    break;
                }

                current.summons++;
                RefreshFirstRareRound();
                TryMaintainHumanMergeTempo();
                safetyLoops++;
            }

            TryMaintainHumanMergeTempo();
            RefreshFirstRareRound();
        }

        private static void TryMaintainHumanMergeTempo()
        {
            if (controller == null || current == null)
            {
                return;
            }

            int safetyLoops = 0;
            while (safetyLoops < 8 && controller.BoardUnitCount >= MinimumBoardUnits() + 2)
            {
                int beforeCount = controller.BoardUnitCount;
                bool merged = TryMerge(CharacterGrade.Normal) ||
                              TryMerge(CharacterGrade.Rare) ||
                              TryMerge(CharacterGrade.Epic);

                int nextRound = controller.CurrentRound + 1;
                if (!merged && (nextRound >= 8 || controller.BoardUnitCount >= MinimumBoardUnits() + 4))
                {
                    merged = TryMerge(CharacterGrade.Legendary);
                }

                if (!merged || controller.BoardUnitCount >= beforeCount)
                {
                    break;
                }

                safetyLoops++;
            }
        }

        private static bool TryMergeOneAvailable()
        {
            return TryMerge(CharacterGrade.Normal) ||
                   TryMerge(CharacterGrade.Rare) ||
                   TryMerge(CharacterGrade.Epic) ||
                   TryMerge(CharacterGrade.Legendary);
        }

        private static void TryUseFateSurvivalInCrisis()
        {
            if (controller == null || current == null || current.fateUses > 0)
            {
                return;
            }

            int nextRound = controller.CurrentRound + 1;
            if (nextRound < 5 || nextRound > 9 || controller.MaxLife <= 0)
            {
                return;
            }

            float lifeRatio = (float)controller.Life / controller.MaxLife;
            if (fairStrategyPolicy)
            {
                if (lifeRatio > 0.75f || !controller.CanUseFateSurvival)
                {
                    return;
                }

                if (controller.TryActivateFateSurvival())
                {
                    RecordFateUse("crisis_slot0", 0);
                }
                return;
            }
            float threshold = current.strategy == "shop-save"
                ? 0.78f
                : current.strategy == "balanced" ? 0.75f : 0.72f;
            if (lifeRatio > threshold || !controller.CanUseFateSurvival)
            {
                return;
            }

            if (controller.TryActivateFateSurvival())
            {
                RecordFateUse("crisis_slot0", 0);
            }
        }

        private static void TryUsePreferredFateCardByStrategy()
        {
            if (controller == null || current == null || current.fateUses > 0 || !controller.CanOpenFateCard)
            {
                return;
            }

            int nextRound = controller.CurrentRound + 1;
            if (!ShouldUseFateCardForStrategy(nextRound))
            {
                return;
            }

            int choiceIndex = FindPreferredFateChoiceIndex(ResolveFateCardPreferences());
            if (!controller.TryOpenFateCardChoicePanel())
            {
                return;
            }

            if (choiceIndex < 0)
            {
                return;
            }

            if (controller.TryActivateFateCardChoice(choiceIndex))
            {
                RecordFateUse("strategy_prefer", choiceIndex);
            }
        }

        private static bool ShouldUseFateCardForStrategy(int nextRound)
        {
            if (controller == null || current == null || !controller.IsRoundRunning || controller.CurrentRound < 5)
            {
                return false;
            }

            float lifeRatio = controller.MaxLife > 0 ? (float)controller.Life / controller.MaxLife : 1f;
            if (fairStrategyPolicy)
            {
                return controller.CurrentRound >= 6 && lifeRatio <= 0.78f;
            }
            if (controller.CurrentRound >= requestedTargetRound)
            {
                return true;
            }

            if (current.strategy == "summon-heavy")
            {
                return controller.CurrentRound >= 7 && lifeRatio <= 0.75f;
            }

            if (current.strategy == "balanced")
            {
                return controller.CurrentRound >= 6 && lifeRatio <= 0.78f;
            }

            if (current.strategy == "shop-save")
            {
                return controller.CurrentRound >= 6 && (lifeRatio <= 0.82f || controller.BoardUnitCount < MinimumBoardUnits());
            }

            return false;
        }

        private static string[] ResolveFateCardPreferences()
        {
            if (current == null)
            {
                return new string[0];
            }

            if (current.strategy == "summon-heavy")
            {
                return new[] { "등급 조작", "에픽 선불", "용병 호출", "황금 대출", "밀수 루트", "응급 방벽", "왕의 공포", "금단의 소환", "전장 개방" };
            }

            if (current.strategy == "balanced")
            {
                return new[] { "응급 방벽", "황금 대출", "용병 호출", "등급 조작", "에픽 선불", "밀수 루트", "왕의 공포", "최후의 방벽", "피의 계약", "암시장 개장", "생명 주조" };
            }

            return new[] { "암시장 개장", "황금 대출", "응급 방벽", "용병 호출", "등급 조작", "밀수 루트", "왕의 공포", "최후의 방벽", "피의 계약", "전장 개방", "생명 주조" };
        }

        private static int FindPreferredFateChoiceIndex(string[] preferences)
        {
            if (controller == null || preferences == null || preferences.Length == 0)
            {
                return -1;
            }

            for (int p = 0; p < preferences.Length; p++)
            {
                for (int i = 0; i < 3; i++)
                {
                    string label = controller.GetFateCardChoiceHudLabel(i);
                    if (!string.IsNullOrWhiteSpace(label) && !ShouldSkipRiskyFateChoice(label) && label.IndexOf(preferences[p], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return i;
                    }
                }
            }

            for (int i = 0; i < 3; i++)
            {
                string label = controller.GetFateCardChoiceHudLabel(i);
                if (!string.IsNullOrWhiteSpace(label) && !ShouldSkipRiskyFateChoice(label))
                {
                    return i;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                if (!string.IsNullOrWhiteSpace(controller.GetFateCardChoiceHudLabel(i)))
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool ShouldSkipRiskyFateChoice(string label)
        {
            if (controller == null || string.IsNullOrWhiteSpace(label))
            {
                return false;
            }

            int nextRound = controller.CurrentRound + 1;
            float lifeRatio = controller.MaxLife > 0 ? (float)controller.Life / controller.MaxLife : 1f;
            if (fairStrategyPolicy)
            {
                return controller.CurrentRound >= 6 && lifeRatio <= 0.78f;
            }
            if (ContainsAny(label, "시간 정지", "심판 번개") && !controller.IsRoundRunning)
            {
                return false;
            }

            if (ContainsAny(label, "생명 주조") && nextRound <= 6 && controller.Gold >= controller.SummonCost * 2)
            {
                return true;
            }

            bool riskyLifeCost = ContainsAny(label, "HP-", "HP -", "HP 절반", "HP 1", "라이프 -", "라이프-", "도박");
            if (!riskyLifeCost)
            {
                return false;
            }

            return nextRound <= 4 || lifeRatio <= 0.85f || ContainsAny(label, "도박사의 판");
        }

        private static void RecordFateUse(string trigger, int choiceIndex)
        {
            if (current == null || controller == null)
            {
                return;
            }

            current.fateUses++;
            current.fateTrigger = trigger ?? string.Empty;
            current.fateChoiceIndex = choiceIndex;
            current.fateActivationRound = Mathf.Max(1, controller.CurrentRound);
            CaptureFateCardSnapshot(trigger);
            if (!string.IsNullOrWhiteSpace(current.fateCardTitle))
            {
                current.notes.Add("fate_" + current.fateActivationRound + "_" + current.fateCardTitle);
            }
        }

        private static void CaptureFateCardSnapshot(string trigger)
        {
            if (current == null || controller == null || !controller.FateCardWasUsed)
            {
                return;
            }

            current.fateCardTitle = controller.FateCardLastTitle;
            current.fateCardDetail = controller.FateCardLastDetail;
            current.fateCardDebt = controller.FateCardLastDebt;
            if (string.IsNullOrWhiteSpace(current.fateTrigger))
            {
                current.fateTrigger = trigger ?? string.Empty;
            }
        }

        private static void TryUseFateSummonQualityBoost()
        {
            if (controller == null || current == null || current.strategy != "summon-heavy" || controller.CanUseFateCard)
            {
                return;
            }

            int nextRound = controller.CurrentRound + 1;
            if (nextRound < 3 || nextRound > 6 || current.fateUses > 0 || !controller.CanUseFateNormalBan)
            {
                return;
            }

            if (controller.TryActivateFateNormalBan(4))
            {
                RecordFateUse("summon_quality_slot2", 2);
            }
        }

        private static int ResolveGoldReserve()
        {
            if (fairStrategyPolicy)
            {
                int fairNextRound = controller.CurrentRound + 1;
                return fairNextRound <= 3 ? 12 : fairNextRound <= 6 ? 24 : 20;
            }

            if (current.strategy == "summon-heavy")
            {
                return 0;
            }

            if (current.strategy == "balanced")
            {
                int nextBalancedRound = controller.CurrentRound + 1;
                if (nextBalancedRound <= 3)
                {
                    return controller.SummonCost * 2;
                }

                return controller.SummonCost;
            }

            int nextRound = controller.CurrentRound + 1;
            if (nextRound <= 3)
            {
                return current.strategy == "shop-save" ? 10 : 12;
            }

            if (nextRound <= 6)
            {
                return current.strategy == "shop-save" ? 22 : 32;
            }

            return 20;
        }

        private static int MinimumBoardUnits()
        {
            int nextRound = controller.CurrentRound + 1;
            if (nextRound <= 2)
            {
                return 3;
            }

            if (!fairStrategyPolicy && current != null && current.strategy == "shop-save" && nextRound <= 5)
            {
                return 6;
            }

            if (nextRound <= 5)
            {
                return 5;
            }

            return 7;
        }

        private static void MergeAllPossible()
        {
            for (int pass = 0; pass < 4; pass++)
            {
                bool merged = false;
                merged |= TryMerge(CharacterGrade.Normal);
                merged |= TryMerge(CharacterGrade.Rare);
                merged |= TryMerge(CharacterGrade.Epic);
                merged |= TryMerge(CharacterGrade.Legendary);
                if (!merged)
                {
                    break;
                }
            }
        }

        private static bool TryMerge(CharacterGrade grade)
        {
            if (!controller.TryMerge(grade))
            {
                return false;
            }

            current.merges++;
            current.mergeTrace.Add("R" + Mathf.Max(1, controller.CurrentRound) + ":" + grade);
            current.highestMergeGrade = Mathf.Max(current.highestMergeGrade, (int)grade + 1);
            if (current.firstMergeRound <= 0)
            {
                current.firstMergeRound = Mathf.Max(1, controller.CurrentRound + 1);
            }

            RefreshFirstRareRound();
            return true;
        }

        private static void RefreshFirstRareRound()
        {
            if (current.firstRarePlusRound > 0)
            {
                return;
            }

            DefenderUnit[] units = UnityEngine.Object.FindObjectsOfType<DefenderUnit>();
            for (int i = 0; i < units.Length; i++)
            {
                if (units[i] != null && (int)units[i].Grade >= (int)CharacterGrade.Rare)
                {
                    current.firstRarePlusRound = Mathf.Max(1, controller.CurrentRound + 1);
                    return;
                }
            }
        }

        private static void StartNextRound()
        {
            int nextRound = controller.CurrentRound + 1;
            roundStartEditorTime = EditorApplication.timeSinceStartup;
            controller.StartRound();
            if (!controller.IsRoundRunning)
            {
                nextActionTime = EditorApplication.timeSinceStartup + 0.15d;
                return;
            }

            lastObservedRound = nextRound;
            BeginPostBossRoundTelemetry(nextRound);
            if (controller.IsBossRound)
            {
                current.bossAttempts++;
                current.activeBossRound = nextRound;
                current.activeBossStartTime = roundStartEditorTime;
                current.bossKillCountAtRoundStart = controller.RunBossKillCount;
                current.lastObservedBossHealth01 = -1f;
                if (nextRound == 10)
                {
                    r10EncounterTelemetryActive = true;
                    current.r10LifeAtRoundStart = controller.Life;
                    current.r10BossFirstDamagedSeconds = -1f;
                    current.r10BossHealthAtFirstDamage01 = -1f;
                    current.r10BossStartSnapshot = new MilestoneSnapshot
                    {
                        life = controller.Life,
                        gold = controller.Gold,
                        boardUnitCount = controller.BoardUnitCount,
                        boardCapacity = controller.BoardCapacity,
                        highestOwnedGrade = ResolveHighestOwnedGrade(),
                        totalSummons = controller.RunTotalPlayerSummons,
                        totalMerges = controller.RunTotalMerges,
                        totalGradeUpgradeLevels = ResolveTotalGradeUpgradeLevels(),
                        summonCost = controller.SummonCost,
                        targetMonsterCount = controller.RoundTargetCount,
                        isHordeRound = controller.IsCurrentRoundHorde,
                        isBossRound = controller.IsBossRound,
                        isMidBossRound = controller.IsMidBossRound
                    };
                }
            }
            waitingRoundEnd = true;
            nextActionTime = EditorApplication.timeSinceStartup + 0.2d;
        }

        private static void FinalizeActiveBossAttempt()
        {
            if (current == null || controller == null || current.activeBossRound <= 0)
            {
                return;
            }

            current.bossCombatDurationSeconds += Mathf.Max(0f, (float)(EditorApplication.timeSinceStartup - current.activeBossStartTime));
            if (controller.RunBossKillCount > current.bossKillCountAtRoundStart)
            {
                current.bossClears++;
                if (current.activeBossRound == 10)
                {
                    current.r10BossCleared = true;
                }
            }
            else
            {
                current.bossFailures++;
                current.bossHealthRemainingOnFailure01 = current.lastObservedBossHealth01;
                if (current.activeBossRound == 10)
                {
                    current.r10BossHealthRemainingOnFailure01 = current.lastObservedBossHealth01;
                }
            }

            r10EncounterTelemetryActive = false;
            current.activeBossRound = 0;
        }

        private static bool ValidateBossAttemptAccounting(bool requireFinalizedWhenNoActive)
        {
            if (current == null)
            {
                return false;
            }

            int resolvedAttempts = current.bossClears + current.bossFailures;
            bool valid = resolvedAttempts <= current.bossAttempts;
            if (requireFinalizedWhenNoActive && current.activeBossRound <= 0)
            {
                valid &= resolvedAttempts == current.bossAttempts;
            }

            if (!valid)
            {
                string warning = "boss_attempt_accounting_mismatch_attempts_" + current.bossAttempts + "_resolved_" + resolvedAttempts + "_active_" + current.activeBossRound;
                if (!current.validationCoverageWarnings.Contains(warning))
                {
                    current.validationCoverageWarnings.Add(warning);
                }
            }

            return valid;
        }

        private static void ObserveActiveBossHealth()
        {
            if (current == null || current.activeBossRound <= 0)
            {
                return;
            }

            float observed = ResolveRemainingBossHealth01();
            if (observed > 0f)
            {
                current.lastObservedBossHealth01 = observed;
            }
        }
        private static float ResolveRemainingBossHealth01()
        {
            float highest = 0f;
            bool found = false;
            IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
            for (int i = 0; i < monsters.Count; i++)
            {
                MonsterUnit monster = monsters[i];
                if (monster == null || !monster.IsBoss || monster.MaxHealth <= 0f)
                {
                    continue;
                }

                found = true;
                highest = Mathf.Max(highest, Mathf.Clamp01(monster.CurrentHealth / monster.MaxHealth));
            }

            return found ? highest : 0f;
        }
        private static void HandleShopIfOpen()
        {
            GameObject overlay = GameObject.Find("RunShopOverlay");
            if (overlay == null || !overlay.activeInHierarchy)
            {
                return;
            }

            int round = Mathf.Max(1, controller.CurrentRound);
            if (round == 3)
            {
                current.r3ShopSeen = true;
            }

            if (round == 6)
            {
                current.r6ShopSeen = true;
            }

            bool shopFocused = current.strategy == "shop-save" && !fairStrategyPolicy;
            bool shouldConsiderPurchase = current.shopPurchases < ResolveShopPurchaseLimit();
            bool bought = false;
            if (shouldConsiderPurchase)
            {
                Button[] buttons = overlay.GetComponentsInChildren<Button>(true);
                Button bestButton = null;
                int bestCost = 0;
                int bestScore = int.MinValue;
                for (int i = 0; i < buttons.Length; i++)
                {
                    Button button = buttons[i];
                    if (button == null || !button.gameObject.activeInHierarchy || !button.name.StartsWith("RunShopOffer_", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int offerCost = ResolveOfferGoldCost(button);
                    int reserve = ResolveShopGoldReserve(shopFocused, round);
                    if (offerCost < 0 || controller.Gold < offerCost + reserve)
                    {
                        continue;
                    }

                    int score = ScoreShopOfferForStrategy(button, offerCost, shopFocused);
                    int minimumScore = ResolveShopMinimumScore(shopFocused, offerCost);
                    if (score >= minimumScore && score > bestScore)
                    {
                        bestButton = button;
                        bestCost = offerCost;
                        bestScore = score;
                    }
                }

                if (bestButton != null)
                {
                    int before = controller.Gold;
                    bestButton.onClick.Invoke();
                    if (controller.Gold < before || overlay == null || !overlay.activeInHierarchy || !bestButton.gameObject.activeInHierarchy)
                    {
                        bought = true;
                        current.shopPurchases++;
                        current.shopGoldSpent += before - controller.Gold;
                        current.notes.Add("shop_" + Mathf.Max(1, controller.CurrentRound) + "_" + CompactNote(BuildButtonSearchText(bestButton), 24) + "_" + bestCost + "G");
                    current.shopPurchaseTrace.Add("R" + Mathf.Max(1, controller.CurrentRound) + ":" + bestButton.name + ":" + CompactNote(BuildButtonSearchText(bestButton), 96) + ":" + bestCost);
                    }
                }
            }

            if (!bought)
            {
                Button close = FindButton("RunShopCloseButton");
                if (close != null)
                {
                    close.onClick.Invoke();
                }
            }
        }

        private static int ResolveShopPurchaseLimit()
        {
            if (current == null)
            {
                return 0;
            }

            if (!fairStrategyPolicy && current.strategy == "shop-save")
            {
                return 2;
            }

            return 1;
        }

        private static int ResolveShopGoldReserve(bool shopFocused, int round)
        {
            if (controller == null || current == null)
            {
                return 0;
            }

            if (fairStrategyPolicy)
            {
                return round <= 3 ? Mathf.Max(0, controller.SummonCost / 2) : Mathf.Max(0, controller.SummonCost);
            }

            if (shopFocused)
            {
                return round <= 3 ? 0 : Mathf.Max(0, controller.SummonCost / 2);
            }

            if (current.strategy == "balanced")
            {
                return round <= 3 ? Mathf.Max(0, controller.SummonCost / 2) : Mathf.Max(0, controller.SummonCost);
            }

            return round <= 3 ? 0 : Mathf.Max(0, controller.SummonCost);
        }

        private static int ResolveShopMinimumScore(bool shopFocused, int offerCost)
        {
            if (fairStrategyPolicy)
            {
                return offerCost <= 0 ? 30 : 45;
            }

            if (shopFocused)
            {
                return offerCost <= 0 ? 20 : 35;
            }

            if (current != null && current.strategy == "summon-heavy")
            {
                return offerCost <= 0 ? 35 : 65;
            }

            return offerCost <= 0 ? 30 : 45;
        }

        private static int ResolveOfferGoldCost(Button button)
        {
            if (button == null)
            {
                return -1;
            }

            Text[] texts = button.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null || !string.Equals(text.name, "Price", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(text.text))
                {
                    continue;
                }

                int goldMarker = text.text.IndexOf('G');
                if (goldMarker > 0)
                {
                    string numeric = text.text.Substring(0, goldMarker).Trim();
                    if (int.TryParse(numeric, out int cost))
                    {
                        return Mathf.Max(0, cost);
                    }
                }

                if (ContainsAny(text.text, "무료", "보험", "운명", "라이프", "HP -"))
                {
                    return 0;
                }

                if (ContainsAny(text.text, "골드+HP"))
                {
                    return 999;
                }
            }

            return -1;
        }

        private static int ScoreShopOfferForStrategy(Button button, int offerCost, bool shopFocused)
        {
            string text = BuildButtonSearchText(button);
            bool risky = ContainsAny(text, "위험", "HP -", "HP-", "라이프 -", "라이프-", "빚", "계약");
            int score = offerCost <= 0 ? 70 : Mathf.Max(0, 125 - offerCost);
            score += ContainsAny(text, "보험", "구제", "대응권") ? 92 : 0;
            score += ContainsAny(text, "긴급 소환", "소환권", "레어 보급", "레어 지원", "용병") ? 70 : 0;
            score += ContainsAny(text, "보스 대비", "보스 정보", "보스 피해", "화력", "증강체") ? 62 : 0;
            score += ContainsAny(text, "합성", "재료", "부스터") ? 60 : 0;
            score += ContainsAny(text, "쿠폰", "할인", "소환비") ? 42 : 0;
            score += ContainsAny(text, "회복", "의무병", "수호", "방벽", "체력") ? 32 : 0;

            if (shopFocused)
            {
                score += ContainsAny(text, "구제", "레어", "용병") ? 90 : 0;
                score += ContainsAny(text, "회복", "의무병", "수호", "방벽", "체력") ? 72 : 0;
                score += ContainsAny(text, "합성", "재료", "Merge") ? 58 : 0;
                score += ContainsAny(text, "할인", "쿠폰", "소환비") ? 42 : 0;
                score += ContainsAny(text, "화력", "스킬", "공격") ? 32 : 0;
                score -= risky ? ResolveRiskyShopPenalty() : 0;
                return score;
            }

            score += ContainsAny(text, "합성", "재료", "레어", "화력", "스킬") ? 42 : 0;
            score += ContainsAny(text, "회복", "수호") ? 22 : 0;
            score -= risky ? ResolveRiskyShopPenalty() : 0;
            return score;
        }

        private static int ResolveRiskyShopPenalty()
        {
            if (controller == null || controller.MaxLife <= 0)
            {
                return 120;
            }

            float lifeRatio = (float)controller.Life / controller.MaxLife;
            return lifeRatio <= 0.55f ? 190 : lifeRatio <= 0.75f ? 145 : 105;
        }

        private static string BuildButtonSearchText(Button button)
        {
            if (button == null)
            {
                return string.Empty;
            }

            Text[] texts = button.GetComponentsInChildren<Text>(true);
            StringBuilder builder = new StringBuilder(128);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null || string.IsNullOrWhiteSpace(text.text))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(text.text);
            }

            return builder.ToString();
        }

        private static bool ContainsAny(string text, params string[] tokens)
        {
            if (string.IsNullOrWhiteSpace(text) || tokens == null)
            {
                return false;
            }

            for (int i = 0; i < tokens.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(tokens[i]) && text.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ChooseAugmentIfOpen()
        {
            GameObject overlay = GameObject.Find("AugmentChoiceOverlay");
            if (overlay == null || !overlay.activeInHierarchy)
            {
                return true;
            }

            Button choice = FindButton("AugmentChoice_0");
            if (choice == null || !choice.gameObject.activeInHierarchy)
            {
                return false;
            }

            choice.onClick.Invoke();
            if (!overlay.activeInHierarchy)
            {
                current.augmentChoiceTrace.Add("R" + Mathf.Max(1, controller.CurrentRound) + ":" + choice.name + ":" + CompactNote(BuildButtonSearchText(choice), 96));
            }
            return !overlay.activeInHierarchy;
        }

        private static void CloseResultOverlayIfOpen()
        {
            GameObject overlay = GameObject.Find("RoundResultOverlay");
            if (overlay == null || !overlay.activeInHierarchy)
            {
                return;
            }

            Button continueButton = FindButton("ResultContinueButton");
            if (continueButton != null && continueButton.gameObject.activeInHierarchy)
            {
                continueButton.onClick.Invoke();
            }
        }

        private static Button FindButton(string name)
        {
            Button[] buttons = UnityEngine.Object.FindObjectsOfType<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == name)
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private static void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && current != null)
            {
                current.runtimeErrorCount++;
                if (current.runtimeErrorSamples.Count < 8 && !string.IsNullOrWhiteSpace(condition))
                {
                    current.runtimeErrorSamples.Add(CompactNote(condition, 120));
                }
            }

            if (type == LogType.Warning && !string.IsNullOrEmpty(condition) && condition.Contains("referenced script") && condition.Contains("missing"))
            {
                missingScriptWarningsObserved++;
                if (MissingScriptWarningSamples.Count < 12 && !MissingScriptWarningSamples.Contains(condition))
                {
                    MissingScriptWarningSamples.Add(condition);
                }
            }
        }

        private static void WriteRuntimeMissingScriptReport(string phase)
        {
            Directory.CreateDirectory(OutputDirectory);
            GameObject[] objects = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
            List<string> entries = new List<string>();
            int totalMissing = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject gameObject = objects[i];
                if (gameObject == null)
                {
                    continue;
                }

                int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
                if (missingCount <= 0)
                {
                    continue;
                }

                totalMissing += missingCount;
                string scenePath = gameObject.scene.IsValid() ? gameObject.scene.path : string.Empty;
                entries.Add("    {"
                    + "\"path\":\"" + EscapeJson(BuildHierarchyPath(gameObject)) + "\","
                    + "\"scene\":\"" + EscapeJson(scenePath) + "\","
                    + "\"activeInHierarchy\":" + JsonBool(gameObject.activeInHierarchy) + ","
                    + "\"persistent\":" + JsonBool(EditorUtility.IsPersistent(gameObject)) + ","
                    + "\"missingCount\":" + missingCount
                    + "}");
            }

            StringBuilder builder = new StringBuilder(4096);
            builder.AppendLine("{");
            builder.AppendLine("  \"phase\": \"" + EscapeJson(phase) + "\",");
            builder.AppendLine("  \"warningsObserved\": " + missingScriptWarningsObserved + ",");
            builder.AppendLine("  \"liveObjectsWithMissingScripts\": " + entries.Count + ",");
            builder.AppendLine("  \"totalMissingScriptsOnLiveObjects\": " + totalMissing + ",");
            builder.AppendLine("  \"liveAuditPassed\": " + JsonBool(totalMissing == 0) + ",");
            builder.AppendLine("  \"historyStatus\": \"" + (missingScriptWarningsObserved == 0
                ? "clean"
                : totalMissing == 0 ? "warning_history_only_no_live_missing_scripts" : "active_missing_scripts_detected") + "\",");
            builder.AppendLine("  \"warningSamples\": [");
            for (int i = 0; i < MissingScriptWarningSamples.Count; i++)
            {
                builder.Append("    \"").Append(EscapeJson(MissingScriptWarningSamples[i])).Append('"');
                if (i < MissingScriptWarningSamples.Count - 1)
                {
                    builder.Append(',');
                }

                builder.AppendLine();
            }

            builder.AppendLine("  ],");
            builder.AppendLine("  \"objects\": [");
            for (int i = 0; i < entries.Count; i++)
            {
                builder.Append(entries[i]);
                if (i < entries.Count - 1)
                {
                    builder.Append(',');
                }

                builder.AppendLine();
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            File.WriteAllText(MissingScriptReportPath, builder.ToString(), Encoding.UTF8);
        }

        private static string BuildHierarchyPath(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return string.Empty;
            }

            List<string> names = new List<string>();
            Transform currentTransform = gameObject.transform;
            while (currentTransform != null)
            {
                names.Add(currentTransform.name);
                currentTransform = currentTransform.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private static string EscapeJson(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static string CompactNote(string value, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "none";
            }

            string compact = value.Replace("\r", " ").Replace("\n", " ").Trim();
            int safeMax = Mathf.Max(4, maxChars);
            return compact.Length <= safeMax ? compact : compact.Substring(0, safeMax - 1) + "…";
        }

        private static string JsonBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string BuildJson(string status)
        {
            int r10Clears = 0;
            int reachedTarget = 0;
            int victories = 0;
            int defeats = 0;
            int technicalFailures = 0;
            int runtimeErrorRuns = 0;
            int invariantFailures = 0;
            int timeouts = 0;
            int softLocks = 0;
            int bossAttempts = 0;
            int bossClears = 0;
            int bossFailures = 0;
            int totalShopPurchases = 0;
            int totalMissionChoices = 0;
            int totalMissionCompleted = 0;
            int totalGradeUpgradePurchases = 0;
            int totalUltimateRecipeMerges = 0;
            float reachedRoundSum = 0f;
            float endLifeSum = 0f;
            float endGoldSum = 0f;
            Dictionary<string, StrategySummary> strategySummaries = new Dictionary<string, StrategySummary>();
            for (int i = 0; i < Results.Count; i++)
            {
                RunResult result = Results[i];
                if (result.r10BossCleared) r10Clears++;
                if (result.reachedRound >= requestedTargetRound) reachedTarget++;
                if (result.victory) victories++;
                if (result.defeat) defeats++;
                if (result.technicalFailure) technicalFailures++;
                if (result.runtimeErrorCount > 0) runtimeErrorRuns++;
                if (result.invariantFailure) invariantFailures++;
                if (result.timeout) timeouts++;
                if (result.softLock) softLocks++;
                bossAttempts += result.bossAttempts;
                bossClears += result.bossClears;
                bossFailures += result.bossFailures;
                totalShopPurchases += result.shopPurchases;
                totalMissionChoices += result.missionChoiceCount;
                totalMissionCompleted += result.missionCompletedCount;
                totalGradeUpgradePurchases += result.gradeUpgradePurchaseCount;
                totalUltimateRecipeMerges += result.ultimateRecipeMergeCount;
                reachedRoundSum += result.reachedRound;
                endLifeSum += result.endLife;
                endGoldSum += result.endGold;
                if (!strategySummaries.TryGetValue(result.strategy, out StrategySummary summary))
                {
                    summary = new StrategySummary();
                    strategySummaries[result.strategy] = summary;
                }
                summary.runs++;
                summary.reachedRoundSum += result.reachedRound;
                summary.endLifeSum += result.endLife;
                summary.endGoldSum += result.endGold;
                if (result.reachedRound >= requestedTargetRound) summary.reachedTarget++;
                if (result.r10BossCleared) summary.r10BossClears++;
            }

            StringBuilder builder = new StringBuilder(16384);
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"" + EscapeJson(status) + "\",");
            builder.AppendLine("  \"combatMode\": \"" + requestedCombatMode + "\",");
            builder.AppendLine("  \"targetRound\": " + requestedTargetRound + ",");
            builder.AppendLine("  \"pairedSeedMode\": " + JsonBool(pairedSeedMode) + ",");
            builder.AppendLine("  \"fairStrategyPolicy\": " + JsonBool(fairStrategyPolicy) + ",");
            builder.AppendLine("  \"runs\": " + Results.Count + ",");
            builder.AppendLine("  \"targetRuns\": " + requestedRunCount + ",");
            builder.AppendLine("  \"r10Clears\": " + r10Clears + ",");
            builder.AppendLine("  \"r10SuccessRate\": " + FormatRatio(r10Clears, Results.Count) + ",");
            builder.AppendLine("  \"reachedTargetCount\": " + reachedTarget + ",");
            builder.AppendLine("  \"reachedTargetRate\": " + FormatRatio(reachedTarget, Results.Count) + ",");
            builder.AppendLine("  \"victories\": " + victories + ",");
            builder.AppendLine("  \"defeats\": " + defeats + ",");
            builder.AppendLine("  \"technicalFailureCount\": " + technicalFailures + ",");
            builder.AppendLine("  \"runtimeErrorRunCount\": " + runtimeErrorRuns + ",");
            builder.AppendLine("  \"invariantFailureCount\": " + invariantFailures + ",");
            builder.AppendLine("  \"timeouts\": " + timeouts + ",");
            builder.AppendLine("  \"softLocks\": " + softLocks + ",");
            builder.AppendLine("  \"averageReachedRound\": " + FormatFloat(Results.Count > 0 ? reachedRoundSum / Results.Count : -1f) + ",");
            builder.AppendLine("  \"averageEndLife\": " + FormatFloat(Results.Count > 0 ? endLifeSum / Results.Count : -1f) + ",");
            builder.AppendLine("  \"averageEndGold\": " + FormatFloat(Results.Count > 0 ? endGoldSum / Results.Count : -1f) + ",");
            builder.AppendLine("  \"bossAttempts\": " + bossAttempts + ",");
            builder.AppendLine("  \"bossClears\": " + bossClears + ",");
            builder.AppendLine("  \"bossFailures\": " + bossFailures + ",");
            builder.AppendLine("  \"bossClearRate\": " + FormatRatio(bossClears, bossAttempts) + ",");
            builder.AppendLine("  \"shopPurchases\": " + totalShopPurchases + ",");
            builder.AppendLine("  \"totalMissionChoices\": " + totalMissionChoices + ",");
            builder.AppendLine("  \"totalMissionCompleted\": " + totalMissionCompleted + ",");
            builder.AppendLine("  \"totalGradeUpgradePurchases\": " + totalGradeUpgradePurchases + ",");
            builder.AppendLine("  \"totalUltimateRecipeMerges\": " + totalUltimateRecipeMerges + ",");
            builder.AppendLine("  \"strategySummary\": [");
            int strategyIndex = 0;
            foreach (KeyValuePair<string, StrategySummary> pair in strategySummaries)
            {
                StrategySummary summary = pair.Value;
                builder.Append("    {\"strategy\":\"").Append(EscapeJson(pair.Key)).Append("\",");
                builder.Append("\"runs\":").Append(summary.runs).Append(',');
                builder.Append("\"reachedTarget\":").Append(summary.reachedTarget).Append(',');
                builder.Append("\"r10BossClears\":").Append(summary.r10BossClears).Append(',');
                builder.Append("\"reachedTargetRate\":").Append(FormatRatio(summary.reachedTarget, summary.runs)).Append(',');
                builder.Append("\"averageReachedRound\":").Append(FormatFloat(summary.reachedRoundSum / Mathf.Max(1, summary.runs))).Append(',');
                builder.Append("\"averageEndLife\":").Append(FormatFloat(summary.endLifeSum / Mathf.Max(1, summary.runs))).Append(',');
                builder.Append("\"averageEndGold\":").Append(FormatFloat(summary.endGoldSum / Mathf.Max(1, summary.runs))).Append('}');
                if (++strategyIndex < strategySummaries.Count) builder.Append(',');
                builder.AppendLine();
            }
            builder.AppendLine("  ],");
            builder.AppendLine("  \"results\": [");
            for (int i = 0; i < Results.Count; i++)
            {
                RunResult result = Results[i];
                builder.Append("    {");
                builder.Append("\"index\":").Append(result.index).Append(',');
                builder.Append("\"contentSeed\":").Append(result.contentSeed).Append(',');
                builder.Append("\"strategy\":\"").Append(EscapeJson(result.strategy)).Append("\",");
                builder.Append("\"reachedRound\":").Append(result.reachedRound).Append(',');
                builder.Append("\"r10BossCleared\":").Append(JsonBool(result.r10BossCleared)).Append(',');
                builder.Append("\"victory\":").Append(JsonBool(result.victory)).Append(',');
                builder.Append("\"defeat\":").Append(JsonBool(result.defeat)).Append(',');
                builder.Append("\"technicalFailure\":").Append(JsonBool(result.technicalFailure)).Append(',');
                builder.Append("\"timeout\":").Append(JsonBool(result.timeout)).Append(',');
                builder.Append("\"softLock\":").Append(JsonBool(result.softLock)).Append(',');
                builder.Append("\"invariantFailure\":").Append(JsonBool(result.invariantFailure)).Append(',');
                builder.Append("\"runtimeErrorCount\":").Append(result.runtimeErrorCount).Append(',');
                builder.Append("\"runtimeErrorSamples\":\"").Append(EscapeJson(string.Join(" | ", result.runtimeErrorSamples))).Append("\",");
                builder.Append("\"validationCoverageWarnings\":\"").Append(EscapeJson(string.Join(" | ", result.validationCoverageWarnings))).Append("\",");
                builder.Append("\"endGold\":").Append(result.endGold).Append(',');
                builder.Append("\"endLife\":").Append(result.endLife).Append(',');
                builder.Append("\"gameplayDefeatRound\":").Append(result.gameplayDefeatRound).Append(',');
                builder.Append("\"technicalFailureRound\":").Append(result.technicalFailureRound).Append(',');
                builder.Append("\"totalLeakDamage\":").Append(result.totalLeakDamage).Append(',');
                builder.Append("\"leakDamageByRound\":");
                AppendIntCountsJson(builder, result.leakDamageByRound);
                builder.Append(',');
                builder.Append("\"escapedMonsterCountByRound\":");
                AppendIntCountsJson(builder, result.escapedMonsterCountByRound);
                builder.Append(',');
                builder.Append("\"totalSummons\":").Append(result.totalSummons).Append(',');
                builder.Append("\"totalMerges\":").Append(result.totalMerges).Append(',');
                builder.Append("\"summonSequenceHash\":\"").Append(EscapeJson(result.summonSequenceHash)).Append("\",");
                builder.Append("\"mergeSequenceHash\":\"").Append(EscapeJson(result.mergeSequenceHash)).Append("\",");
                builder.Append("\"augmentChoiceIdsHash\":\"").Append(EscapeJson(result.augmentChoiceIdsHash)).Append("\",");
                builder.Append("\"missionChoiceIdsHash\":\"").Append(EscapeJson(result.missionChoiceIdsHash)).Append("\",");
                builder.Append("\"shopPurchaseIdsHash\":\"").Append(EscapeJson(result.shopPurchaseIdsHash)).Append("\",");
                builder.Append("\"runContentChannels\":[");
                for (int channelIndex = 0; channelIndex < result.runContentChannels.Count; channelIndex++)
                {
                    RunContentChannelTrace channel = result.runContentChannels[channelIndex];
                    if (channelIndex > 0) builder.Append(',');
                    builder.Append('{');
                    builder.Append("\"channel\":\"").Append(EscapeJson(channel.channel)).Append("\",");
                    builder.Append("\"seed\":").Append(channel.seed).Append(',');
                    builder.Append("\"drawCount\":").Append(channel.drawCount).Append(',');
                    builder.Append("\"outcomeHash\":\"").Append(EscapeJson(channel.outcomeHash)).Append("\",");
                    builder.Append("\"tracePrefix\":\"").Append(EscapeJson(channel.tracePrefix)).Append("\"");
                    builder.Append('}');
                }
                builder.Append("],");
                builder.Append("\"highestMergeGrade\":").Append(result.highestMergeGrade).Append(',');
                builder.Append("\"gradeUpgradePurchaseCount\":").Append(result.gradeUpgradePurchaseCount).Append(',');
                builder.Append("\"firstGradeUpgradeRound\":").Append(result.firstGradeUpgradeRound).Append(',');
                builder.Append("\"gradeUpgradeNormalCount\":").Append(result.gradeUpgradeNormalCount).Append(',');
                builder.Append("\"gradeUpgradeRareCount\":").Append(result.gradeUpgradeRareCount).Append(',');
                builder.Append("\"gradeUpgradeEpicCount\":").Append(result.gradeUpgradeEpicCount).Append(',');
                builder.Append("\"gradeUpgradeLegendaryCount\":").Append(result.gradeUpgradeLegendaryCount).Append(',');
                builder.Append("\"gradeUpgradeMythicCount\":").Append(result.gradeUpgradeMythicCount).Append(',');
                builder.Append("\"gradeUpgradeTranscendentCount\":").Append(result.gradeUpgradeTranscendentCount).Append(',');
                builder.Append("\"emptyGradeUpgradeAttemptCount\":").Append(result.emptyGradeUpgradeAttemptCount).Append(',');
                builder.Append("\"lastBlockingChoiceReason\":\"").Append(EscapeJson(result.lastBlockingChoiceReason)).Append("\",");
                builder.Append("\"totalGradeUpgradeLevels\":").Append(result.totalGradeUpgradeLevels).Append(',');
                builder.Append("\"ultimateRecipeMergeCount\":").Append(result.ultimateRecipeMergeCount).Append(',');
                builder.Append("\"augmentChoiceCount\":").Append(result.augmentChoiceCount).Append(',');
                builder.Append("\"missionChoiceCount\":").Append(result.missionChoiceCount).Append(',');
                builder.Append("\"missionCompletedCount\":").Append(result.missionCompletedCount).Append(',');
                builder.Append("\"shopOpenCount\":").Append(result.shopOpenCount).Append(',');
                builder.Append("\"shopPurchaseCount\":").Append(result.shopPurchases).Append(',');
                builder.Append("\"luckySummonChoiceCount\":").Append(result.luckySummonChoiceCount).Append(',');
                builder.Append("\"bossAttempts\":").Append(result.bossAttempts).Append(',');
                builder.Append("\"bossClears\":").Append(result.bossClears).Append(',');
                builder.Append("\"bossFailures\":").Append(result.bossFailures).Append(',');
                builder.Append("\"bossCombatDurationSeconds\":").Append(FormatFloat(result.bossCombatDurationSeconds)).Append(',');
                builder.Append("\"lastObservedBossHealth01\":").Append(FormatFloat(result.lastObservedBossHealth01)).Append(',');
                builder.Append("\"bossHealthRemainingOnFailure01\":").Append(FormatFloat(result.bossHealthRemainingOnFailure01)).Append(',');
                builder.Append("\"r10BossHealthRemainingOnFailure01\":").Append(FormatFloat(result.r10BossHealthRemainingOnFailure01)).Append(',');
                builder.Append("\"r10LifeAtBossSpawn\":").Append(result.r10LifeAtBossSpawn).Append(',');
                builder.Append("\"r10SupportSpawnCount\":").Append(result.r10SupportSpawnCount).Append(',');
                builder.Append("\"r10SupportKillsBeforeBossSpawn\":").Append(result.r10SupportKillsBeforeBossSpawn).Append(',');
                builder.Append("\"r10SupportEscapesBeforeBossSpawn\":").Append(result.r10SupportEscapesBeforeBossSpawn).Append(',');
                builder.Append("\"r10LeakDamageBeforeBossSpawn\":").Append(result.r10LeakDamageBeforeBossSpawn).Append(',');
                builder.Append("\"r10BossFirstDamagedSeconds\":").Append(FormatFloat(result.r10BossFirstDamagedSeconds)).Append(',');
                builder.Append("\"r10BossHealthAtFirstDamage01\":").Append(FormatFloat(result.r10BossHealthAtFirstDamage01)).Append(',');
                builder.Append("\"r10BossStartSnapshot\":");
                AppendSnapshotJson(builder, result.r10BossStartSnapshot);
                builder.Append(',');
                builder.Append("\"bossForecastBet\":\"").Append(EscapeJson(result.bossForecastBet)).Append("\",");
                builder.Append("\"fateUses\":").Append(result.fateUses).Append(',');
                builder.Append("\"postBossRoundTelemetry\":");
                AppendPostBossRoundTelemetryJson(builder, result.postBossRoundTelemetry);
                builder.Append(',');
                builder.Append("\"milestones\":");
                AppendMilestonesJson(builder, result.milestones);
                builder.Append(',');
                builder.Append("\"notes\":\"").Append(EscapeJson(string.Join(";", result.notes))).Append("\"");
                builder.Append('}');
                if (i < Results.Count - 1) builder.Append(',');
                builder.AppendLine();
            }
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }
        private static void AppendIntCountsJson(StringBuilder builder, Dictionary<int, int> counts)
        {
            builder.Append('{');
            bool first = true;
            if (counts != null)
            {
                foreach (KeyValuePair<int, int> pair in counts)
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    builder.Append('"').Append(pair.Key).Append("\":").Append(pair.Value);
                    first = false;
                }
            }

            builder.Append('}');
        }

        private static void AppendSnapshotJson(StringBuilder builder, MilestoneSnapshot snapshot)
        {
            if (snapshot == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append("{\"life\":").Append(snapshot.life).Append(',');
            builder.Append("\"gold\":").Append(snapshot.gold).Append(',');
            builder.Append("\"boardUnitCount\":").Append(snapshot.boardUnitCount).Append(',');
            builder.Append("\"boardCapacity\":").Append(snapshot.boardCapacity).Append(',');
            builder.Append("\"highestOwnedGrade\":").Append(snapshot.highestOwnedGrade).Append(',');
            builder.Append("\"summonCost\":").Append(snapshot.summonCost).Append(',');
            builder.Append("\"totalSummons\":").Append(snapshot.totalSummons).Append(',');
            builder.Append("\"totalMerges\":").Append(snapshot.totalMerges).Append(',');
            builder.Append("\"totalGradeUpgradeLevels\":").Append(snapshot.totalGradeUpgradeLevels).Append(',');
            builder.Append("\"targetMonsterCount\":").Append(snapshot.targetMonsterCount).Append(',');
            builder.Append("\"leakDamage\":").Append(snapshot.leakDamage).Append(',');
            builder.Append("\"isHordeRound\":").Append(JsonBool(snapshot.isHordeRound)).Append(',');
            builder.Append("\"isBossRound\":").Append(JsonBool(snapshot.isBossRound)).Append(',');
            builder.Append("\"isMidBossRound\":").Append(JsonBool(snapshot.isMidBossRound)).Append('}');
        }
        private static void AppendPostBossRoundTelemetryJson(StringBuilder builder, Dictionary<int, PostBossRoundTelemetry> telemetryByRound)
        {
            builder.Append('[');
            bool first = true;
            if (telemetryByRound != null)
            {
                foreach (KeyValuePair<int, PostBossRoundTelemetry> pair in telemetryByRound)
                {
                    if (!first) builder.Append(',');
                    PostBossRoundTelemetry telemetry = pair.Value;
                    builder.Append("{\"round\":").Append(telemetry.round).Append(',');
                    builder.Append("\"reached\":").Append(JsonBool(telemetry.reached)).Append(',');
                    builder.Append("\"actuallyCleared\":").Append(JsonBool(telemetry.actuallyCleared)).Append(',');
                    builder.Append("\"lifeAtStart\":").Append(telemetry.lifeAtStart).Append(',');
                    builder.Append("\"lifeAtEnd\":").Append(telemetry.lifeAtEnd).Append(',');
                    builder.Append("\"goldAtStart\":").Append(telemetry.goldAtStart).Append(',');
                    builder.Append("\"goldAtEnd\":").Append(telemetry.goldAtEnd).Append(',');
                    builder.Append("\"boardUnitCountAtStart\":").Append(telemetry.boardUnitCountAtStart).Append(',');
                    builder.Append("\"boardUnitCountAtEnd\":").Append(telemetry.boardUnitCountAtEnd).Append(',');
                    builder.Append("\"boardCapacityAtStart\":").Append(telemetry.boardCapacityAtStart).Append(',');
                    builder.Append("\"boardCapacityAtEnd\":").Append(telemetry.boardCapacityAtEnd).Append(',');
                    builder.Append("\"highestOwnedGradeAtStart\":").Append(telemetry.highestOwnedGradeAtStart).Append(',');
                    builder.Append("\"highestOwnedGradeAtEnd\":").Append(telemetry.highestOwnedGradeAtEnd).Append(',');
                    builder.Append("\"totalSummonsAtStart\":").Append(telemetry.totalSummonsAtStart).Append(',');
                    builder.Append("\"totalSummonsAtEnd\":").Append(telemetry.totalSummonsAtEnd).Append(',');
                    builder.Append("\"totalMergesAtStart\":").Append(telemetry.totalMergesAtStart).Append(',');
                    builder.Append("\"totalMergesAtEnd\":").Append(telemetry.totalMergesAtEnd).Append(',');
                    builder.Append("\"totalGradeUpgradeLevelsAtStart\":").Append(telemetry.totalGradeUpgradeLevelsAtStart).Append(',');
                    builder.Append("\"totalGradeUpgradeLevelsAtEnd\":").Append(telemetry.totalGradeUpgradeLevelsAtEnd).Append(',');
                    builder.Append("\"targetMonsterCount\":").Append(telemetry.targetMonsterCount).Append(',');
                    builder.Append("\"totalSpawned\":").Append(telemetry.totalSpawned).Append(',');
                    builder.Append("\"totalKilled\":").Append(telemetry.totalKilled).Append(',');
                    builder.Append("\"totalEscaped\":").Append(telemetry.totalEscaped).Append(',');
                    builder.Append("\"leakDamage\":").Append(telemetry.leakDamage).Append(',');
                    builder.Append("\"peakActiveMonsters\":").Append(telemetry.peakActiveMonsters).Append(',');
                    builder.Append("\"firstLeakSeconds\":").Append(FormatFloat(telemetry.firstLeakSeconds)).Append(',');
                    builder.Append("\"roundDurationSeconds\":").Append(FormatFloat(telemetry.roundDurationSeconds)).Append(',');
                    builder.Append("\"isHordeRound\":").Append(JsonBool(telemetry.isHordeRound)).Append(',');
                    builder.Append("\"isBossRound\":").Append(JsonBool(telemetry.isBossRound)).Append(',');
                    builder.Append("\"isMidBossRound\":").Append(JsonBool(telemetry.isMidBossRound)).Append('}');
                    first = false;
                }
            }
            builder.Append(']');
        }
        private static void AppendMilestonesJson(StringBuilder builder, Dictionary<int, MilestoneSnapshot> milestones)
        {
            builder.Append('[');
            bool first = true;
            foreach (KeyValuePair<int, MilestoneSnapshot> pair in milestones)
            {
                if (!first) builder.Append(',');
                MilestoneSnapshot snapshot = pair.Value;
                builder.Append("{\"round\":").Append(pair.Key).Append(',');
                builder.Append("\"life\":").Append(snapshot.life).Append(',');
                builder.Append("\"gold\":").Append(snapshot.gold).Append(',');
                builder.Append("\"boardUnitCount\":").Append(snapshot.boardUnitCount).Append(',');
                builder.Append("\"boardCapacity\":").Append(snapshot.boardCapacity).Append(',');
                builder.Append("\"highestOwnedGrade\":").Append(snapshot.highestOwnedGrade).Append(',');
                builder.Append("\"totalSummons\":").Append(snapshot.totalSummons).Append(',');
                builder.Append("\"totalMerges\":").Append(snapshot.totalMerges).Append(',');
                builder.Append("\"totalGradeUpgradeLevels\":").Append(snapshot.totalGradeUpgradeLevels).Append(',');
                builder.Append("\"summonCost\":").Append(snapshot.summonCost).Append(',');
                builder.Append("\"ultimateRecipeCount\":").Append(snapshot.ultimateRecipeCount).Append(',');
                builder.Append("\"targetMonsterCount\":").Append(snapshot.targetMonsterCount).Append(',');
                builder.Append("\"leakDamage\":").Append(snapshot.leakDamage).Append(',');
                builder.Append("\"isHordeRound\":").Append(JsonBool(snapshot.isHordeRound)).Append(',');
                builder.Append("\"isBossRound\":").Append(JsonBool(snapshot.isBossRound)).Append(',');
                builder.Append("\"isMidBossRound\":").Append(JsonBool(snapshot.isMidBossRound)).Append('}');
                first = false;
            }
            builder.Append(']');
        }
        private static void AddCount(Dictionary<string, int> target, string key, int amount)
        {
            if (target == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            target[key] = target.TryGetValue(key, out int currentCount) ? currentCount + amount : amount;
        }

        private static string FormatFloat(float value)
        {
            return value < 0f ? "-1" : value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string FormatRatio(int numerator, int denominator)
        {
            return denominator <= 0 ? "0.00" : ((float)numerator / denominator).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
        }

        private sealed class StrategySummary
        {
            public int runs;
            public int reachedTarget;
            public int r10BossClears;
            public float reachedRoundSum;
            public float endLifeSum;
            public float endGoldSum;
        }
        [Serializable]
        private sealed class MilestoneSnapshot
        {
            public int life;
            public int gold;
            public int boardUnitCount;
            public int boardCapacity;
            public int highestOwnedGrade;
            public int totalSummons;
            public int totalMerges;
            public int totalGradeUpgradeLevels;
            public int summonCost;
            public int ultimateRecipeCount;
            public int targetMonsterCount;
            public int leakDamage;
            public bool isHordeRound;
            public bool isBossRound;
            public bool isMidBossRound;
        }
        [Serializable]
        private sealed class PostBossRoundTelemetry
        {
            public int round;
            public bool reached;
            public bool actuallyCleared;
            public int lifeAtStart;
            public int lifeAtEnd;
            public int goldAtStart;
            public int goldAtEnd;
            public int boardUnitCountAtStart;
            public int boardUnitCountAtEnd;
            public int boardCapacityAtStart;
            public int boardCapacityAtEnd;
            public int highestOwnedGradeAtStart;
            public int highestOwnedGradeAtEnd;
            public int totalSummonsAtStart;
            public int totalSummonsAtEnd;
            public int totalMergesAtStart;
            public int totalMergesAtEnd;
            public int totalGradeUpgradeLevelsAtStart;
            public int totalGradeUpgradeLevelsAtEnd;
            public int targetMonsterCount;
            public int totalSpawned;
            public int totalKilled;
            public int totalEscaped;
            public int leakDamage;
            public int peakActiveMonsters;
            public float firstLeakSeconds;
            public float roundDurationSeconds;
            public bool isHordeRound;
            public bool isBossRound;
            public bool isMidBossRound;
        }
        [Serializable]
        private sealed class RunContentChannelTrace
        {
            public string channel = string.Empty;
            public uint seed;
            public int drawCount;
            public string outcomeHash = string.Empty;
            public string tracePrefix = string.Empty;
        }

        [Serializable]
        private sealed class RunResult
        {
            public int index;
            public int contentSeed;
            public string strategy;
            public int startGold;
            public int reachedRound;
            public int gameplayDefeatRound;
            public int technicalFailureRound;
            public int totalLeakDamage;
            public readonly Dictionary<int, int> leakDamageByRound = new Dictionary<int, int>();
            public readonly Dictionary<int, int> escapedMonsterCountByRound = new Dictionary<int, int>();
            public bool victory;
            public bool defeat;
            public bool softLock;
            public bool invariantFailure;
            public bool technicalFailure;
            public bool missionOfferObserved;
            public int bossFailures;
            public int bossKillCountAtRoundStart;
            public float lastObservedBossHealth01 = -1f;
            public float bossHealthRemainingOnFailure01 = -1f;
            public float r10BossHealthRemainingOnFailure01 = -1f;
            public int r10LifeAtRoundStart;
            public int r10LifeAtBossSpawn = -1;
            public int r10SupportSpawnCount;
            public int r10SupportKillsBeforeBossSpawn;
            public int r10SupportEscapesBeforeBossSpawn;
            public int r10LeakDamageBeforeBossSpawn;
            public float r10BossFirstDamagedSeconds = -1f;
            public float r10BossHealthAtFirstDamage01 = -1f;
            public readonly List<string> validationCoverageWarnings = new List<string>();
            public int runtimeErrorCount;
            public readonly List<string> runtimeErrorSamples = new List<string>();
            public int totalSummons;
            public int totalMerges;
            public string summonSequenceHash = string.Empty;
            public string mergeSequenceHash = string.Empty;
            public string augmentChoiceIdsHash = string.Empty;
            public string missionChoiceIdsHash = string.Empty;
            public string shopPurchaseIdsHash = string.Empty;
            public readonly List<string> summonSequenceTrace = new List<string>();
            public readonly List<string> mergeTrace = new List<string>();
            public readonly List<string> augmentChoiceTrace = new List<string>();
            public readonly List<string> missionChoiceTrace = new List<string>();
            public readonly List<string> shopPurchaseTrace = new List<string>();
            public readonly List<RunContentChannelTrace> runContentChannels = new List<RunContentChannelTrace>();
            public int highestMergeGrade;
            public int gradeUpgradePurchaseCount;
            public int firstGradeUpgradeRound;
            public int gradeUpgradeNormalCount;
            public int gradeUpgradeRareCount;
            public int gradeUpgradeEpicCount;
            public int gradeUpgradeLegendaryCount;
            public int gradeUpgradeMythicCount;
            public int gradeUpgradeTranscendentCount;
            public int emptyGradeUpgradeAttemptCount;
            public string lastBlockingChoiceReason = "None";
            public int totalGradeUpgradeLevels;
            public int ultimateRecipeMergeCount;
            public int augmentChoiceCount;
            public int missionChoiceCount;
            public int missionCompletedCount;
            public int shopOpenCount;
            public int lastShopOpenRound = -1;
            public int luckySummonChoiceCount;
            public int bossAttempts;
            public int bossClears;
            public float bossCombatDurationSeconds;
            public MilestoneSnapshot r10BossStartSnapshot;
            public int activeBossRound;
            public double activeBossStartTime;
            public int lastControllerRound;
            public string lastPreparationFingerprint = string.Empty;
            public int preparationFingerprintRepeats;
            public readonly Dictionary<int, PostBossRoundTelemetry> postBossRoundTelemetry = new Dictionary<int, PostBossRoundTelemetry>();
            public readonly Dictionary<int, MilestoneSnapshot> milestones = new Dictionary<int, MilestoneSnapshot>();
            public bool r10BossCleared;
            public int summons;
            public int merges;
            public int shopPurchases;
            public int shopGoldSpent;
            public int fateUses;
            public bool r3ShopSeen;
            public bool r6ShopSeen;
            public int firstRarePlusRound;
            public int firstMergeRound;
            public string fateCardTitle = string.Empty;
            public string fateCardDetail = string.Empty;
            public int fateCardDebt;
            public int fateChoiceIndex = -1;
            public int fateActivationRound;
            public string fateTrigger = string.Empty;
            public int endGold;
            public int endLife;
            public float r10BossHealthRemaining01 = -1f;
            public string bossForecastBet = string.Empty;
            public bool bossForecastSuccess;
            public int bossForecastBonusScore;
            public bool timeout;
            public readonly List<string> notes = new List<string>();
        }
    }
}
