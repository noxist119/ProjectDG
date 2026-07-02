using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DefenseGame.Editor
{
    [InitializeOnLoad]
    public static class DefenseGameBatchTurboOverride
    {
        private const string BatchMethodToken = "DefenseGame.Editor.DefenseGameBatchPlaytest.RunHumanStrategies20";
        private const float TurboTimeScale = 180f;
        private const float TurboFixedDeltaTime = 0.05f;
        private const float TurboMaximumDeltaTime = 0.75f;
        private static readonly bool Active;

        static DefenseGameBatchTurboOverride()
        {
            Active = Application.isBatchMode && Environment.GetCommandLineArgs().Any(argument =>
                argument.IndexOf(BatchMethodToken, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!Active)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.update -= KeepTurboSettings;
            EditorApplication.update += KeepTurboSettings;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ApplyTurboSettings();
            }
        }

        private static void KeepTurboSettings()
        {
            if (Active && EditorApplication.isPlaying)
            {
                ApplyTurboSettings();
            }
        }

        private static void ApplyTurboSettings()
        {
            Time.timeScale = TurboTimeScale;
            Time.fixedDeltaTime = TurboFixedDeltaTime;
            Time.maximumDeltaTime = TurboMaximumDeltaTime;
            Application.runInBackground = true;
            Application.targetFrameRate = 240;
            QualitySettings.vSyncCount = 0;
        }
    }
}
