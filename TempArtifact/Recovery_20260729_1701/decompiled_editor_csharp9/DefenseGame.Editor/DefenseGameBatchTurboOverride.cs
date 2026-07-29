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

		private const float TurboTimeScale = 40f;

		private const float TurboFixedDeltaTime = 0.025f;

		private const float TurboMaximumDeltaTime = 0.33f;

		private static readonly bool Active;

		static DefenseGameBatchTurboOverride()
		{
			Active = Application.isBatchMode && Environment.GetCommandLineArgs().Any((string argument) => argument.IndexOf("DefenseGame.Editor.DefenseGameBatchPlaytest.RunHumanStrategies20", StringComparison.OrdinalIgnoreCase) >= 0);
			if (Active)
			{
				EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
				EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
				EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(KeepTurboSettings));
				EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(KeepTurboSettings));
			}
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
			Time.timeScale = 40f;
			Time.fixedDeltaTime = 0.025f;
			Time.maximumDeltaTime = 0.33f;
			Application.runInBackground = true;
			Application.targetFrameRate = 240;
			QualitySettings.vSyncCount = 0;
		}
	}
}
