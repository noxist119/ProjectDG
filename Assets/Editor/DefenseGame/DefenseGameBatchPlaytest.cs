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
        private const int TargetRuns = 10;
        private const int TargetRound = 10;
        private const float BatchTimeScale = 54f;
        private const float BatchFixedDeltaTime = 0.05f;
        private const float BatchMaximumDeltaTime = 0.75f;
        private const double RoundTimeoutSeconds = 45d;
        private const double RunTimeoutSeconds = 300d;
        private const string ScenePath = "Assets/Scenes/DG.unity";
        private const string OutputDirectoryName = "BatchPlaytestResults";
        private const string OutputFileName = "DefenseGame_Playtest10_Human3.json";
        private const string MissingScriptReportFileName = "RuntimeMissingScripts.json";
        private static string OutputDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputDirectoryName));
        private static string OutputPath => Path.Combine(OutputDirectory, OutputFileName);
        private static string MissingScriptReportPath => Path.Combine(OutputDirectory, MissingScriptReportFileName);

        private static readonly List<RunResult> Results = new List<RunResult>();
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

        [MenuItem("DefenseGame/Batch Playtest/Run Human Strategies 10 Total")]
        public static void RunHumanStrategies20()
        {
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

            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
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

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

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

                Time.timeScale = BatchTimeScale;
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

            CloseResultOverlayIfOpen();
            ChooseAugmentIfOpen();

            if (waitingRoundEnd)
            {
                if (controller.Life <= 0)
                {
                    current.notes.Add("life_zero_during_round_R" + lastObservedRound);
                    CompleteRun();
                    return;
                }

                if (controller.IsRoundRunning)
                {
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

                current.reachedRound = Mathf.Max(current.reachedRound, controller.CurrentRound);
                current.endGold = controller.Gold;
                current.endLife = controller.Life;
                current.r10BossHealthRemaining01 = ResolveRemainingBossHealth01();
                waitingRoundEnd = false;
                nextActionTime = EditorApplication.timeSinceStartup + 0.35d;
                return;
            }

            if (controller.Life <= 0 || controller.CurrentRound >= TargetRound)
            {
                CompleteRun();
                return;
            }

            HandleShopIfOpen();
            CloseResultOverlayIfOpen();
            ChooseAugmentIfOpen();
            TryUseFateSummonQualityBoost();
            TryUseFateSurvivalInCrisis();
            ExecutePrepPolicy();
            StartNextRound();
        }

        private static void StartRun()
        {
            current = new RunResult
            {
                index = runIndex + 1,
                strategy = ResolveStrategy(runIndex),
                startGold = controller.Gold
            };

            controller.ResetRunForRetry();
            runStartEditorTime = EditorApplication.timeSinceStartup;
            current.startGold = controller.Gold;
            lastObservedRound = controller.CurrentRound;
            waitingRoundEnd = false;
            nextActionTime = EditorApplication.timeSinceStartup + 0.3d;
        }

        private static string ResolveStrategy(int index)
        {
            switch (index % 3)
            {
                case 0:
                    return "summon-heavy";
                case 1:
                    return "balanced";
                default:
                    return "shop-save";
            }
        }

        private static void CompleteRun()
        {
            current.reachedRound = Mathf.Max(current.reachedRound, controller.CurrentRound);
            current.endGold = controller.Gold;
            current.endLife = controller.Life;
            current.r10BossHealthRemaining01 = ResolveRemainingBossHealth01();
            current.clearedR10 = controller.Life > 0 && controller.CurrentRound >= TargetRound && !current.timeout;
            Results.Add(current);
            File.WriteAllText(OutputPath + ".partial", BuildJson("partial"), Encoding.UTF8);

            runIndex++;
            if (runIndex >= TargetRuns)
            {
                FinishAll("complete");
                return;
            }

            StartRun();
        }

        private static void FinishAll(string status)
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;

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
            EditorApplication.Exit(status == "complete" ? 0 : 1);
        }

        private static void ExecutePrepPolicy()
        {
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
                safetyLoops++;
            }

            RefreshFirstRareRound();
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
            if (controller == null || current == null)
            {
                return;
            }

            int nextRound = controller.CurrentRound + 1;
            if (nextRound < 5 || nextRound > 9 || controller.MaxLife <= 0)
            {
                return;
            }

            float lifeRatio = (float)controller.Life / controller.MaxLife;
            float threshold = current.strategy == "shop-save"
                ? 0.78f
                : current.strategy == "balanced" ? 0.75f : 0.72f;
            if (lifeRatio > threshold || !controller.CanUseFateSurvival)
            {
                return;
            }

            if (controller.TryActivateFateSurvival())
            {
                current.fateUses++;
            }
        }

        private static void TryUseFateSummonQualityBoost()
        {
            if (controller == null || current == null || current.strategy != "summon-heavy")
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
                current.fateUses++;
            }
        }

        private static int ResolveGoldReserve()
        {
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
                return 12;
            }

            if (nextRound <= 6)
            {
                return 32;
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
            lastObservedRound = controller.CurrentRound + 1;
            roundStartEditorTime = EditorApplication.timeSinceStartup;
            controller.StartRound();
            waitingRoundEnd = true;
            nextActionTime = EditorApplication.timeSinceStartup + 0.2d;
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

            bool balancedPurchaseAvailable = current.strategy == "balanced" && current.shopPurchases < 1;
            bool shopFocused = current.strategy == "shop-save";
            bool bought = false;
            if (balancedPurchaseAvailable || shopFocused)
            {
                Button[] buttons = overlay.GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    Button button = buttons[i];
                    if (button == null || !button.gameObject.activeInHierarchy || !button.name.StartsWith("RunShopOffer_", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int offerCost = ResolveOfferGoldCost(button);
                    int reserve = balancedPurchaseAvailable ? Mathf.Max(16, controller.SummonCost * 2) : 0;
                    if (offerCost < 0 || controller.Gold < offerCost + reserve)
                    {
                        continue;
                    }

                    int before = controller.Gold;
                    button.onClick.Invoke();
                    if (controller.Gold < before)
                    {
                        bought = true;
                        current.shopPurchases++;
                        current.shopGoldSpent += before - controller.Gold;
                    }

                    if (bought)
                    {
                        break;
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
                if (goldMarker <= 0)
                {
                    return -1;
                }

                string numeric = text.text.Substring(0, goldMarker).Trim();
                if (int.TryParse(numeric, out int cost))
                {
                    return Mathf.Max(0, cost);
                }
            }

            return -1;
        }

        private static void ChooseAugmentIfOpen()
        {
            GameObject overlay = GameObject.Find("AugmentChoiceOverlay");
            if (overlay == null || !overlay.activeInHierarchy)
            {
                return;
            }

            Button choice = FindButton("AugmentChoice_0");
            if (choice != null && choice.gameObject.activeInHierarchy)
            {
                choice.onClick.Invoke();
            }
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
            if (type == LogType.Warning && !string.IsNullOrEmpty(condition) && condition.Contains("referenced script") && condition.Contains("missing"))
            {
                missingScriptWarningsObserved++;
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

        private static string JsonBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string BuildJson(string status)
        {
            int cleared = 0;
            int r3Seen = 0;
            int r6Seen = 0;
            int shopPurchases = 0;
            int shopGoldSpent = 0;
            int fateUses = 0;
            int summonHeavyRuns = 0;
            int summonHeavyClears = 0;
            int summonHeavyReachedR10 = 0;
            int summonHeavyReachedR9Plus = 0;
            int balancedRuns = 0;
            int balancedClears = 0;
            int shopSaveRuns = 0;
            int shopSaveClears = 0;
            float rareRoundSum = 0f;
            int rareRoundCount = 0;
            float mergeRoundSum = 0f;
            int mergeRoundCount = 0;

            for (int i = 0; i < Results.Count; i++)
            {
                RunResult result = Results[i];
                if (result.clearedR10) cleared++;
                if (result.r3ShopSeen) r3Seen++;
                if (result.r6ShopSeen) r6Seen++;
                shopPurchases += result.shopPurchases;
                shopGoldSpent += result.shopGoldSpent;
                fateUses += result.fateUses;
                if (result.strategy == "summon-heavy")
                {
                    summonHeavyRuns++;
                    if (result.clearedR10) summonHeavyClears++;
                    if (result.reachedRound >= TargetRound)
                    {
                        summonHeavyReachedR10++;
                    }

                    if (result.reachedRound >= 9)
                    {
                        summonHeavyReachedR9Plus++;
                    }
                }
                else if (result.strategy == "balanced")
                {
                    balancedRuns++;
                    if (result.clearedR10) balancedClears++;
                }
                else if (result.strategy == "shop-save")
                {
                    shopSaveRuns++;
                    if (result.clearedR10) shopSaveClears++;
                }
                if (result.firstRarePlusRound > 0)
                {
                    rareRoundSum += result.firstRarePlusRound;
                    rareRoundCount++;
                }

                if (result.firstMergeRound > 0)
                {
                    mergeRoundSum += result.firstMergeRound;
                    mergeRoundCount++;
                }
            }

            StringBuilder builder = new StringBuilder(4096);
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"" + status + "\",");
            builder.AppendLine("  \"runs\": " + Results.Count + ",");
            builder.AppendLine("  \"targetRuns\": " + TargetRuns + ",");
            builder.AppendLine("  \"r10Clears\": " + cleared + ",");
            builder.AppendLine("  \"r10SuccessRate\": " + FormatRatio(cleared, Results.Count) + ",");
            builder.AppendLine("  \"r3ShopSeen\": " + r3Seen + ",");
            builder.AppendLine("  \"r6ShopSeen\": " + r6Seen + ",");
            builder.AppendLine("  \"shopPurchases\": " + shopPurchases + ",");
            builder.AppendLine("  \"shopGoldSpent\": " + shopGoldSpent + ",");
            builder.AppendLine("  \"fateUses\": " + fateUses + ",");
            builder.AppendLine("  \"summonHeavyRuns\": " + summonHeavyRuns + ",");
            builder.AppendLine("  \"summonHeavyR10Clears\": " + summonHeavyClears + ",");
            builder.AppendLine("  \"summonHeavyR10SuccessRate\": " + FormatRatio(summonHeavyClears, summonHeavyRuns) + ",");
            builder.AppendLine("  \"summonHeavyReachedR10\": " + summonHeavyReachedR10 + ",");
            builder.AppendLine("  \"summonHeavyReachedR9Plus\": " + summonHeavyReachedR9Plus + ",");
            builder.AppendLine("  \"balancedRuns\": " + balancedRuns + ",");
            builder.AppendLine("  \"balancedR10Clears\": " + balancedClears + ",");
            builder.AppendLine("  \"balancedR10SuccessRate\": " + FormatRatio(balancedClears, balancedRuns) + ",");
            builder.AppendLine("  \"shopSaveRuns\": " + shopSaveRuns + ",");
            builder.AppendLine("  \"shopSaveR10Clears\": " + shopSaveClears + ",");
            builder.AppendLine("  \"shopSaveR10SuccessRate\": " + FormatRatio(shopSaveClears, shopSaveRuns) + ",");
            builder.AppendLine("  \"avgFirstRarePlusRound\": " + FormatFloat(rareRoundCount > 0 ? rareRoundSum / rareRoundCount : -1f) + ",");
            builder.AppendLine("  \"avgFirstMergeRound\": " + FormatFloat(mergeRoundCount > 0 ? mergeRoundSum / mergeRoundCount : -1f) + ",");
            builder.AppendLine("  \"results\": [");
            for (int i = 0; i < Results.Count; i++)
            {
                RunResult result = Results[i];
                builder.Append("    {");
                builder.Append("\"index\":").Append(result.index).Append(',');
                builder.Append("\"strategy\":\"").Append(result.strategy).Append("\",");
                builder.Append("\"reachedRound\":").Append(result.reachedRound).Append(',');
                builder.Append("\"clearedR10\":").Append(result.clearedR10 ? "true" : "false").Append(',');
                builder.Append("\"summons\":").Append(result.summons).Append(',');
                builder.Append("\"merges\":").Append(result.merges).Append(',');
                builder.Append("\"shopPurchases\":").Append(result.shopPurchases).Append(',');
                builder.Append("\"shopGoldSpent\":").Append(result.shopGoldSpent).Append(',');
                builder.Append("\"fateUses\":").Append(result.fateUses).Append(',');
                builder.Append("\"r3ShopSeen\":").Append(result.r3ShopSeen ? "true" : "false").Append(',');
                builder.Append("\"r6ShopSeen\":").Append(result.r6ShopSeen ? "true" : "false").Append(',');
                builder.Append("\"firstRarePlusRound\":").Append(result.firstRarePlusRound).Append(',');
                builder.Append("\"firstMergeRound\":").Append(result.firstMergeRound).Append(',');
                builder.Append("\"endGold\":").Append(result.endGold).Append(',');
                builder.Append("\"endLife\":").Append(result.endLife).Append(',');
                builder.Append("\"r10BossHealthRemaining01\":").Append(FormatFloat(result.r10BossHealthRemaining01)).Append(',');
                builder.Append("\"timeout\":").Append(result.timeout ? "true" : "false").Append(',');
                builder.Append("\"notes\":\"").Append(EscapeJson(string.Join(";", result.notes))).Append("\"");
                builder.Append("}");
                if (i < Results.Count - 1)
                {
                    builder.Append(',');
                }

                builder.AppendLine();
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string FormatFloat(float value)
        {
            return value < 0f ? "-1" : value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string FormatRatio(int numerator, int denominator)
        {
            return denominator <= 0 ? "0.00" : ((float)numerator / denominator).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
        }

        [Serializable]
        private sealed class RunResult
        {
            public int index;
            public string strategy;
            public int startGold;
            public int reachedRound;
            public bool clearedR10;
            public int summons;
            public int merges;
            public int shopPurchases;
            public int shopGoldSpent;
            public int fateUses;
            public bool r3ShopSeen;
            public bool r6ShopSeen;
            public int firstRarePlusRound;
            public int firstMergeRound;
            public int endGold;
            public int endLife;
            public float r10BossHealthRemaining01 = -1f;
            public bool timeout;
            public readonly List<string> notes = new List<string>();
        }
    }
}
