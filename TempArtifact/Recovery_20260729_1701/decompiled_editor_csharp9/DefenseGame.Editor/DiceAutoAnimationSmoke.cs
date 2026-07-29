using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DefenseGame.Editor
{
	public static class DiceAutoAnimationSmoke
	{
		[Serializable]
		private sealed class SmokeReport
		{
			public string status;

			public bool passed;

			public bool animatorFound;

			public string animatorController;

			public int sampleCount;

			public bool spawnLoopSeen;

			public float firstSpawnLoopTime;

			public bool idleAfterSettled;

			public bool spawnAfterSettled;

			public int stateChangesAfterSettled;

			public bool idleAfterSpawnLoop;

			public bool spawnAfterSpawnLoop;

			public int stateChangesAfterSpawnLoop;

			public bool finalSpawnLoop;

			public string finalState;

			public string finalNextState;

			public int runtimeErrors;

			public StateSample[] samples = Array.Empty<StateSample>();

			public string[] notes = Array.Empty<string>();
		}

		[Serializable]
		private sealed class StateSample
		{
			public float time;

			public string currentState;

			public float normalizedTime;

			public bool inTransition;

			public string nextState;

			public float nextNormalizedTime;
		}

		private const string ScenePath = "Assets/Scenes/DG.unity";

		private const string HeroId = "hero_56";

		private const string OutputDirectoryName = "BatchPlaytestResults";

		private const string OutputFileName = "DiceAutoAnimationSmoke.json";

		private const double ObservationSeconds = 3.0;

		private const double SampleIntervalSeconds = 0.033;

		private const double SettledAfterSeconds = 0.75;

		private const double SpawnLoopSettleSeconds = 0.25;

		private static readonly List<StateSample> samples = new List<StateSample>();

		private static bool running;

		private static double setupAt;

		private static double evaluateAt;

		private static double nextSampleAt;

		private static int runtimeErrors;

		private static string setupError;

		private static DefenderUnit unit;

		private static Animator animator;

		private static bool previousEnterPlayModeOptionsEnabled;

		private static EnterPlayModeOptions previousEnterPlayModeOptions;

		private static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BatchPlaytestResults", "DiceAutoAnimationSmoke.json"));

		[MenuItem("DefenseGame/Smoke Tests/Dice Auto Animation")]
		public static void RunDiceAutoAnimationSmoke()
		{
			if (!running)
			{
				running = true;
				setupAt = 0.0;
				evaluateAt = 0.0;
				nextSampleAt = 0.0;
				runtimeErrors = 0;
				setupError = string.Empty;
				unit = null;
				animator = null;
				samples.Clear();
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
				EditorSceneManager.OpenScene("Assets/Scenes/DG.unity");
				EditorApplication.isPlaying = true;
			}
		}

		private static void HandlePlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.EnteredPlayMode)
			{
				try
				{
					SetupDiceAuto();
				}
				catch (Exception ex)
				{
					setupError = ex.ToString();
				}
				setupAt = EditorApplication.timeSinceStartup;
				evaluateAt = setupAt + 3.0;
				nextSampleAt = setupAt;
			}
		}

		private static void SetupDiceAuto()
		{
			CharacterDatabase database = UnityEngine.Object.FindObjectOfType<CharacterDatabase>();
			if ((UnityEngine.Object)(object)database == null)
			{
				throw new InvalidOperationException("CharacterDatabase not found in DG scene.");
			}
			CharacterDefinition definition = database.GetCharacterById("hero_56");
			if (definition == null)
			{
				throw new InvalidOperationException("hero_56 definition not found.");
			}
			GameObject prefab = ((definition.prefab != null) ? definition.prefab : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minimi/dice_auto.prefab"));
			if (prefab == null)
			{
				throw new InvalidOperationException("Dice Auto prefab not found.");
			}
			GameObject instance = UnityEngine.Object.Instantiate(prefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
			instance.name = "DiceAutoAnimationSmoke_hero_56";
			unit = instance.GetComponent<DefenderUnit>();
			if ((UnityEngine.Object)(object)unit == null)
			{
				unit = instance.AddComponent<DefenderUnit>();
			}
			Transform firePoint = instance.transform.Find("FirePoint");
			if (firePoint == null)
			{
				GameObject firePointObject = new GameObject("FirePoint");
				firePointObject.transform.SetParent(instance.transform, worldPositionStays: false);
				firePointObject.transform.localPosition = new Vector3(0f, 0.8f, 0.6f);
				firePoint = firePointObject.transform;
			}
			unit.ConfigureRuntimePieces((Projectile)null, firePoint, instance.GetComponentsInChildren<Renderer>(includeInactive: true), (GameObject)null, (GameObject)null, (GameObject)null, (GameObject)null, (GameObject)null);
			instance.SetActive(value: true);
			unit.Initialize(definition);
			animator = instance.GetComponentInChildren<Animator>(includeInactive: true);
			if (animator == null)
			{
				throw new InvalidOperationException("Animator not found on Dice Auto instance.");
			}
		}

		private static void Tick()
		{
			if (running && EditorApplication.isPlaying && !(evaluateAt <= 0.0))
			{
				double now = EditorApplication.timeSinceStartup;
				if (animator != null && now >= nextSampleAt)
				{
					samples.Add(CaptureSample());
					nextSampleAt = now + 0.033;
				}
				if (!(now < evaluateAt))
				{
					Finish(BuildReport());
				}
			}
		}

		private static StateSample CaptureSample()
		{
			AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
			bool inTransition = animator.IsInTransition(0);
			string nextName = string.Empty;
			float nextNormalizedTime = 0f;
			if (inTransition)
			{
				AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
				nextName = ResolveStateName(next);
				nextNormalizedTime = next.normalizedTime;
			}
			return new StateSample
			{
				time = (float)(EditorApplication.timeSinceStartup - setupAt),
				currentState = ResolveStateName(current),
				normalizedTime = current.normalizedTime,
				inTransition = inTransition,
				nextState = nextName,
				nextNormalizedTime = nextNormalizedTime
			};
		}

		private static SmokeReport BuildReport()
		{
			List<string> notes = new List<string>();
			if (!string.IsNullOrWhiteSpace(setupError))
			{
				notes.Add(setupError);
			}
			StateSample[] settled = samples.Where((StateSample sample) => (double)sample.time >= 0.75).ToArray();
			bool animatorFound = animator != null;
			bool spawnLoopSeen = samples.Any((StateSample sample) => sample.currentState == "Spawn_loop" || sample.nextState == "Spawn_loop");
			float firstSpawnLoopTime = samples.FirstOrDefault((StateSample sample) => sample.currentState == "Spawn_loop" || sample.nextState == "Spawn_loop")?.time ?? (-1f);
			StateSample[] afterSpawnLoop = ((firstSpawnLoopTime >= 0f) ? samples.Where((StateSample sample) => sample.time >= firstSpawnLoopTime + 0.25f).ToArray() : Array.Empty<StateSample>());
			bool idleAfterSettled = settled.Any((StateSample sample) => sample.currentState == "Idle" || sample.nextState == "Idle");
			bool spawnAfterSettled = settled.Any((StateSample sample) => sample.currentState == "Spawn" || sample.nextState == "Spawn");
			int stateChangesAfterSettled = CountStateChanges(settled);
			bool idleAfterSpawnLoop = afterSpawnLoop.Any((StateSample sample) => sample.currentState == "Idle" || sample.nextState == "Idle");
			bool spawnAfterSpawnLoop = afterSpawnLoop.Any((StateSample sample) => sample.currentState == "Spawn" || sample.nextState == "Spawn");
			int stateChangesAfterSpawnLoop = CountStateChanges(afterSpawnLoop);
			StateSample finalSample = ((samples.Count > 0) ? samples[samples.Count - 1] : null);
			bool finalSpawnLoop = finalSample != null && finalSample.currentState == "Spawn_loop" && string.IsNullOrEmpty(finalSample.nextState);
			bool passed = animatorFound && spawnLoopSeen && finalSpawnLoop && !idleAfterSpawnLoop && !spawnAfterSpawnLoop && stateChangesAfterSpawnLoop <= 1 && runtimeErrors == 0 && string.IsNullOrWhiteSpace(setupError);
			if (!animatorFound)
			{
				notes.Add("animator_missing");
			}
			if (!spawnLoopSeen)
			{
				notes.Add("spawn_loop_not_seen");
			}
			if (idleAfterSettled)
			{
				notes.Add("idle_seen_after_settled");
			}
			if (idleAfterSpawnLoop)
			{
				notes.Add("idle_seen_after_spawn_loop");
			}
			if (spawnAfterSpawnLoop)
			{
				notes.Add("spawn_seen_after_spawn_loop");
			}
			if (stateChangesAfterSpawnLoop > 1)
			{
				notes.Add("state_changes_after_spawn_loop=" + stateChangesAfterSpawnLoop);
			}
			if (!finalSpawnLoop)
			{
				notes.Add("final_state_not_spawn_loop");
			}
			return new SmokeReport
			{
				status = (passed ? "pass" : "fail"),
				passed = passed,
				animatorFound = animatorFound,
				animatorController = ((animator != null && animator.runtimeAnimatorController != null) ? animator.runtimeAnimatorController.name : string.Empty),
				sampleCount = samples.Count,
				spawnLoopSeen = spawnLoopSeen,
				firstSpawnLoopTime = firstSpawnLoopTime,
				idleAfterSettled = idleAfterSettled,
				spawnAfterSettled = spawnAfterSettled,
				stateChangesAfterSettled = stateChangesAfterSettled,
				idleAfterSpawnLoop = idleAfterSpawnLoop,
				spawnAfterSpawnLoop = spawnAfterSpawnLoop,
				stateChangesAfterSpawnLoop = stateChangesAfterSpawnLoop,
				finalSpawnLoop = finalSpawnLoop,
				finalState = ((finalSample != null) ? finalSample.currentState : string.Empty),
				finalNextState = ((finalSample != null) ? finalSample.nextState : string.Empty),
				runtimeErrors = runtimeErrors,
				samples = samples.ToArray(),
				notes = notes.ToArray()
			};
		}

		private static int CountStateChanges(StateSample[] observedSamples)
		{
			int changes = 0;
			string previous = string.Empty;
			for (int i = 0; i < observedSamples.Length; i++)
			{
				string current = observedSamples[i].currentState;
				if (string.IsNullOrEmpty(previous))
				{
					previous = current;
				}
				else if (current != previous)
				{
					changes++;
					previous = current;
				}
			}
			return changes;
		}

		private static string ResolveStateName(AnimatorStateInfo stateInfo)
		{
			if (stateInfo.IsName("Spawn_loop") || stateInfo.IsName("Base Layer.Spawn_loop"))
			{
				return "Spawn_loop";
			}
			if (stateInfo.IsName("Spawn") || stateInfo.IsName("Base Layer.Spawn"))
			{
				return "Spawn";
			}
			if (stateInfo.IsName("Idle") || stateInfo.IsName("Base Layer.Idle"))
			{
				return "Idle";
			}
			if (stateInfo.IsName("Walk") || stateInfo.IsName("Base Layer.Walk"))
			{
				return "Walk";
			}
			if (stateInfo.IsName("Attack01") || stateInfo.IsName("Base Layer.Attack.Attack01"))
			{
				return "Attack01";
			}
			if (stateInfo.IsName("Attack02") || stateInfo.IsName("Base Layer.Attack.Attack02"))
			{
				return "Attack02";
			}
			if (stateInfo.IsName("Skill01") || stateInfo.IsName("Base Layer.Skill.Skill01"))
			{
				return "Skill01";
			}
			if (stateInfo.IsName("Skill02") || stateInfo.IsName("Base Layer.Skill.Skill02"))
			{
				return "Skill02";
			}
			return "hash_" + stateInfo.shortNameHash;
		}

		private static void HandleLogMessage(string condition, string stackTrace, LogType type)
		{
			if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
			{
				runtimeErrors++;
			}
		}

		private static void Finish(SmokeReport report)
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
