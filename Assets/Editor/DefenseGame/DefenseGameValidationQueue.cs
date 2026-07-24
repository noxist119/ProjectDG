using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DefenseGame.Editor
{
    [InitializeOnLoad]
    public static class DefenseGameValidationQueue
    {
        private const string QueueFileName = "DefenseGameValidationQueue.txt";
        private const string VerticalSmokeFileName = "DefenseGame_PlayModeSmoke.json";
        private const string BossSmokeFileName = "DefenseGame_BossAnimationSmoke.json";
        private const string PlaytestFileName = "DefenseGame_Playtest20_Human3.json";
        private static double nextCheckTime;

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string ResultDirectory => Path.Combine(ProjectRoot, "BatchPlaytestResults");
        private static string QueuePath => Path.Combine(ResultDirectory, QueueFileName);

        static DefenseGameValidationQueue()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("DefenseGame/Validation/Run Vertical + Boss Smoke + Human 3 Strategies x20")]
        public static void StartFullValidation()
        {
            Directory.CreateDirectory(ResultDirectory);
            File.WriteAllText(QueuePath, "start");
            nextCheckTime = 0d;
            Debug.Log("[DefenseGameValidationQueue] validation queued");
        }

        [MenuItem("DefenseGame/Validation/Run Vertical Smoke Only")]
        public static void StartVerticalSmokeOnly()
        {
            Directory.CreateDirectory(ResultDirectory);
            File.WriteAllText(QueuePath, "vertical_only_start");
            nextCheckTime = 0d;
            Debug.Log("[DefenseGameValidationQueue] vertical smoke queued");
        }

        [MenuItem("DefenseGame/Validation/Validate Commercial Hurdle Policy")]
        public static void ValidateCommercialHurdlePolicy()
        {
            CommercialRoundTuning buildUp = CommercialRoundPacing.Resolve(19, false);
            CommercialRoundTuning hurdle20 = CommercialRoundPacing.Resolve(20, true);
            CommercialRoundTuning relief21 = CommercialRoundPacing.Resolve(21, false);
            CommercialRoundTuning hurdle30 = CommercialRoundPacing.Resolve(30, true);

            if (buildUp.phase != CommercialRoundPhase.BuildUp ||
                hurdle20.phase != CommercialRoundPhase.Hurdle ||
                relief21.phase != CommercialRoundPhase.Relief ||
                hurdle30.phase != CommercialRoundPhase.Hurdle)
            {
                throw new InvalidOperationException("Commercial hurdle phase schedule is invalid.");
            }

            if (hurdle20.healthMultiplier <= buildUp.healthMultiplier ||
                relief21.healthMultiplier >= 1f ||
                relief21.spawnCountMultiplier >= 1f ||
                hurdle30.healthMultiplier <= hurdle20.healthMultiplier)
            {
                throw new InvalidOperationException("Commercial hurdle multipliers are not ordered correctly.");
            }

            if (CommercialRoundPacing.GetNextHurdleRound(19) != 20 ||
                CommercialRoundPacing.GetNextHurdleRound(20) != 30 ||
                CommercialRoundPacing.GetNextHurdleRound(49) != 50)
            {
                throw new InvalidOperationException("Commercial next-hurdle schedule is invalid.");
            }

            OutgameProgressionConfig config = ScriptableObject.CreateInstance<OutgameProgressionConfig>();
            try
            {
                if (config.scaleMonstersWithCollectionGrowth ||
                    config.startingEarnedChestKeys < 1 ||
                    config.earnedChestProgressTarget <= 0 ||
                    config.premiumChestEpicPityDraws > 10 ||
                    config.premiumChestLegendaryPityDraws > 40)
                {
                    throw new InvalidOperationException("Commercial chest defaults are invalid.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }

            Debug.Log("[DefenseGameValidationQueue] commercial hurdle + chest policy valid.");
        }


        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < nextCheckTime ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                !File.Exists(QueuePath))
            {
                return;
            }

            nextCheckTime = EditorApplication.timeSinceStartup + 0.75d;
            string state;
            try
            {
                state = File.ReadAllText(QueuePath).Trim();
            }
            catch (Exception)
            {
                return;
            }

            switch (state)
            {
                case "vertical_only_start":
                    BeginStage("vertical_only_running", VerticalSmokeFileName, DefenseGamePlayModeSmoke.RunPlayModeSmoke);
                    break;
                case "vertical_only_running":
                    if (File.Exists(Path.Combine(ResultDirectory, VerticalSmokeFileName)))
                    {
                        File.WriteAllText(QueuePath, "complete");
                        Debug.Log("[DefenseGameValidationQueue] vertical smoke complete: " + ResultDirectory);
                    }
                    break;
                case "start":
                    BeginStage("vertical_running", VerticalSmokeFileName, DefenseGamePlayModeSmoke.RunPlayModeSmoke);
                    break;
                case "vertical_running":
                    ContinueWhenOutputExists(VerticalSmokeFileName, "boss_running", BossSmokeFileName, DefenseGameBossAnimationSmoke.RunBossAnimationSmoke);
                    break;
                case "boss_running":
                    ContinueWhenOutputExists(BossSmokeFileName, "playtest_running", PlaytestFileName, DefenseGameBatchPlaytest.RunHumanStrategies20);
                    break;
                case "playtest_running":
                    if (File.Exists(Path.Combine(ResultDirectory, PlaytestFileName)))
                    {
                        File.WriteAllText(QueuePath, "complete");
                        Debug.Log("[DefenseGameValidationQueue] validation complete: " + ResultDirectory);
                    }
                    break;
            }
        }

        private static void ContinueWhenOutputExists(string completedOutput, string nextState, string nextOutput, Action nextAction)
        {
            if (!File.Exists(Path.Combine(ResultDirectory, completedOutput)))
            {
                return;
            }

            BeginStage(nextState, nextOutput, nextAction);
        }

        private static void BeginStage(string state, string outputFileName, Action action)
        {
            Directory.CreateDirectory(ResultDirectory);
            string outputPath = Path.Combine(ResultDirectory, outputFileName);
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            File.WriteAllText(QueuePath, state);
            nextCheckTime = EditorApplication.timeSinceStartup + 1d;
            try
            {
                action?.Invoke();
            }
            catch (Exception exception)
            {
                File.WriteAllText(QueuePath, "failed:" + state + ":" + exception.GetType().Name);
                Debug.LogException(exception);
            }
        }
    }
}
