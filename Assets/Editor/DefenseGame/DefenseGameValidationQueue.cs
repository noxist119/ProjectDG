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
