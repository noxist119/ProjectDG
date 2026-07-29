using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DefenseGame.Editor
{
	public static class BossStatusImmunitySmoke
	{
		[Serializable]
		private sealed class SmokeReport
		{
			public string status;

			public bool passed;

			public bool controlsBlocked;

			public bool presentationUnchanged;

			public bool damageOverTimeBlocked;

			public bool directDamageAllowed;

			public int runtimeErrors;

			public string[] notes = Array.Empty<string>();
		}

		private const string OutputDirectoryName = "BatchPlaytestResults";

		private const string OutputFileName = "DefenseGame_BossStatusImmunitySmoke.json";

		private const float OriginalAnimatorSpeed = 1.25f;

		private static bool running;

		private static double evaluateAt;

		private static int runtimeErrors;

		private static MonsterUnit boss;

		private static Animator bossAnimator;

		private static float initialHealth;

		private static Vector3 initialPosition;

		private static bool previousEnterPlayModeOptionsEnabled;

		private static EnterPlayModeOptions previousEnterPlayModeOptions;

		private static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BatchPlaytestResults", "DefenseGame_BossStatusImmunitySmoke.json"));

		[MenuItem("DefenseGame/Smoke Tests/Boss Status Immunity")]
		public static void RunBossStatusImmunitySmoke()
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
				SetupBoss();
				evaluateAt = EditorApplication.timeSinceStartup + 0.35;
			}
			catch (Exception ex)
			{
				Finish(controlsBlocked: false, presentationUnchanged: false, damageOverTimeBlocked: false, directDamageAllowed: false, new string[1] { ex.ToString() });
			}
		}

		private static void SetupBoss()
		{
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Expected O, but got Unknown
			//IL_00f9: Expected O, but got Unknown
			GameObject bossObject = new GameObject("BossStatusImmunitySmoke_Boss");
			bossObject.transform.position = new Vector3(2f, 0f, 2f);
			GameObject visualObject = new GameObject("AnimatedVisual");
			visualObject.transform.SetParent(bossObject.transform, worldPositionStays: false);
			bossAnimator = visualObject.AddComponent<Animator>();
			bossAnimator.speed = 1.25f;
			boss = bossObject.AddComponent<MonsterUnit>();
			boss.Initialize(new MonsterDefinition
			{
				id = "smoke_status_immune_boss",
				displayName = "Status Immune Boss",
				role = (MonsterRole)5,
				threatLevel = (MonsterThreatLevel)2,
				isBoss = true,
				stats = new CombatStats
				{
					maxHealth = 1000f,
					attackPower = 20f,
					attackSpeed = 1f,
					maxMana = 100f,
					attackRange = 1.5f,
					moveSpeed = 0f
				}
			}, (Transform)null, 0);
			bossAnimator.speed = 1.25f;
			initialHealth = boss.CurrentHealth;
			initialPosition = ((Component)(object)boss).transform.position;
			boss.ApplySlow(0.8f, 5f);
			boss.ApplyAttackSpeedSlow(0.8f, 5f);
			boss.ApplyPoison(50f, 1f, 0.2f, (DefenderUnit)null);
			boss.ApplyKnockback(10f, Vector3.zero);
			boss.ApplyStun(5f);
			boss.ApplyPetrify(5f, (Material)null);
		}

		private static void Tick()
		{
			if (running && EditorApplication.isPlaying && !(EditorApplication.timeSinceStartup < evaluateAt))
			{
				bool controlsBlocked = (UnityEngine.Object)(object)boss != null && boss.IsStatusEffectImmune && !boss.IsStunned && !boss.IsPetrified;
				bool presentationUnchanged = bossAnimator != null && Mathf.Approximately(bossAnimator.speed, 1.25f);
				bool positionUnchanged = (UnityEngine.Object)(object)boss != null && Vector3.Distance(((Component)(object)boss).transform.position, initialPosition) <= 0.001f;
				bool damageOverTimeBlocked = (UnityEngine.Object)(object)boss != null && Mathf.Approximately(boss.CurrentHealth, initialHealth);
				float healthBeforeDirectDamage = (((UnityEngine.Object)(object)boss != null) ? boss.CurrentHealth : 0f);
				MonsterUnit obj = boss;
				if (obj != null)
				{
					obj.TakeDamage(100f, false, (DefenderUnit)null);
				}
				bool directDamageAllowed = (UnityEngine.Object)(object)boss != null && boss.CurrentHealth < healthBeforeDirectDamage;
				Finish(controlsBlocked, presentationUnchanged && positionUnchanged, damageOverTimeBlocked, directDamageAllowed, Array.Empty<string>());
			}
		}

		private static void HandleLogMessage(string condition, string stackTrace, LogType type)
		{
			if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
			{
				runtimeErrors++;
			}
		}

		private static void Finish(bool controlsBlocked, bool presentationUnchanged, bool damageOverTimeBlocked, bool directDamageAllowed, string[] notes)
		{
			bool passed = controlsBlocked && presentationUnchanged && damageOverTimeBlocked && directDamageAllowed && runtimeErrors == 0;
			SmokeReport report = new SmokeReport
			{
				status = (passed ? "pass" : "fail"),
				passed = passed,
				controlsBlocked = controlsBlocked,
				presentationUnchanged = presentationUnchanged,
				damageOverTimeBlocked = damageOverTimeBlocked,
				directDamageAllowed = directDamageAllowed,
				runtimeErrors = runtimeErrors,
				notes = (notes ?? Array.Empty<string>())
			};
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
				EditorApplication.Exit((!passed) ? 1 : 0);
			}
		}
	}
}
