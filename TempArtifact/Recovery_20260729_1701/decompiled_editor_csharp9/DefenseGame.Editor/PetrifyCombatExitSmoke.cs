using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DefenseGame.Editor
{
	public static class PetrifyCombatExitSmoke
	{
		[Serializable]
		private sealed class SmokeReport
		{
			public string status;

			public bool passed;

			public bool immediatePetrified;

			public bool damageBlocked;

			public bool released;

			public bool animationResumed;

			public bool damageRestored;

			public int runtimeErrors;

			public string[] notes = Array.Empty<string>();
		}

		private const string OutputDirectoryName = "BatchPlaytestResults";

		private const string OutputFileName = "DefenseGame_PetrifyCombatExitSmoke.json";

		private const float OriginalAnimatorSpeed = 1.35f;

		private static bool running;

		private static double evaluateAt;

		private static int runtimeErrors;

		private static MonsterUnit monster;

		private static Animator animator;

		private static float initialHealth;

		private static bool immediatePetrified;

		private static bool damageBlocked;

		private static bool previousEnterPlayModeOptionsEnabled;

		private static EnterPlayModeOptions previousEnterPlayModeOptions;

		private static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BatchPlaytestResults", "DefenseGame_PetrifyCombatExitSmoke.json"));

		[MenuItem("DefenseGame/Smoke Tests/Petrify Combat Exit")]
		public static void RunPetrifyCombatExitSmoke()
		{
			if (!running)
			{
				running = true;
				runtimeErrors = 0;
				Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? string.Empty);
				if (File.Exists(OutputPath))
				{
					File.Delete(OutputPath);
				}
				previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
				previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
				EditorSettings.enterPlayModeOptionsEnabled = true;
				EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
				Application.logMessageReceived -= HandleLogMessage;
				Application.logMessageReceived += HandleLogMessage;
				EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
				EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
				EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(Tick));
				EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(Tick));
				EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
				EditorApplication.isPlaying = true;
			}
		}

		private static void HandlePlayModeStateChanged(PlayModeStateChange state)
		{
			if (state != PlayModeStateChange.EnteredPlayMode)
			{
				return;
			}
			try
			{
				SetupPetrifiedMonster();
				evaluateAt = EditorApplication.timeSinceStartup + 0.4;
			}
			catch (Exception ex)
			{
				SmokeReport smokeReport = new SmokeReport();
				smokeReport.status = "exception";
				smokeReport.passed = false;
				smokeReport.runtimeErrors = runtimeErrors + 1;
				smokeReport.notes = new string[1] { ex.ToString() };
				WriteAndFinish(smokeReport);
			}
		}

		private static void SetupPetrifiedMonster()
		{
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Expected O, but got Unknown
			//IL_00d2: Expected O, but got Unknown
			GameObject monsterObject = new GameObject("PetrifyCombatExitSmoke_Monster");
			GameObject visualObject = new GameObject("AnimatedVisual");
			visualObject.transform.SetParent(monsterObject.transform, worldPositionStays: false);
			animator = visualObject.AddComponent<Animator>();
			animator.speed = 1.35f;
			monster = monsterObject.AddComponent<MonsterUnit>();
			monster.Initialize(new MonsterDefinition
			{
				id = "smoke_petrify_target",
				displayName = "Petrify Smoke Target",
				role = (MonsterRole)0,
				threatLevel = (MonsterThreatLevel)0,
				stats = new CombatStats
				{
					maxHealth = 100f,
					attackPower = 5f,
					attackSpeed = 1f,
					maxMana = 100f,
					attackRange = 1.5f,
					moveSpeed = 0f
				}
			}, (Transform)null, 0);
			initialHealth = monster.CurrentHealth;
			animator.speed = 1.35f;
			monster.ApplyPetrify(0.2f, (Material)null);
			immediatePetrified = monster.IsPetrified && !monster.CanBeCombatTargeted && Mathf.Approximately(animator.speed, 0f);
			monster.TakeDamage(25f, false, (DefenderUnit)null);
			damageBlocked = Mathf.Approximately(monster.CurrentHealth, initialHealth);
		}

		private static void Tick()
		{
			if (running && EditorApplication.isPlaying && !(EditorApplication.timeSinceStartup < evaluateAt))
			{
				bool released = (UnityEngine.Object)(object)monster != null && !monster.IsPetrified && monster.CanBeCombatTargeted;
				bool animationResumed = animator != null && Mathf.Approximately(animator.speed, 1.35f);
				float healthBeforeReleasedHit = (((UnityEngine.Object)(object)monster != null) ? monster.CurrentHealth : 0f);
				MonsterUnit obj = monster;
				if (obj != null)
				{
					obj.TakeDamage(25f, false, (DefenderUnit)null);
				}
				bool damageRestored = (UnityEngine.Object)(object)monster != null && monster.CurrentHealth < healthBeforeReleasedHit;
				bool passed = immediatePetrified && damageBlocked && released && animationResumed && damageRestored && runtimeErrors == 0;
				WriteAndFinish(new SmokeReport
				{
					status = (passed ? "pass" : "fail"),
					passed = passed,
					immediatePetrified = immediatePetrified,
					damageBlocked = damageBlocked,
					released = released,
					animationResumed = animationResumed,
					damageRestored = damageRestored,
					runtimeErrors = runtimeErrors,
					notes = Array.Empty<string>()
				});
			}
		}

		private static void HandleLogMessage(string condition, string stackTrace, LogType type)
		{
			if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
			{
				runtimeErrors++;
			}
		}

		private static void WriteAndFinish(SmokeReport report)
		{
			File.WriteAllText(OutputPath, JsonUtility.ToJson(report, prettyPrint: true));
			running = false;
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(Tick));
			EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
			Application.logMessageReceived -= HandleLogMessage;
			EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
			EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
			EditorApplication.isPlaying = false;
			if (Application.isBatchMode)
			{
				EditorApplication.Exit((!report.passed) ? 1 : 0);
			}
		}
	}
}
