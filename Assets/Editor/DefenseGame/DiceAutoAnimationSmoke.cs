using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefenseGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DefenseGame.Editor
{
    public static class DiceAutoAnimationSmoke
    {
        private const string ScenePath = "Assets/Scenes/DG.unity";
        private const string HeroId = "hero_56";
        private const string OutputDirectoryName = "BatchPlaytestResults";
        private const string OutputFileName = "DiceAutoAnimationSmoke.json";
        private const double ObservationSeconds = 3.0d;
        private const double SampleIntervalSeconds = 0.033d;
        private const double SettledAfterSeconds = 0.75d;
        private const double SpawnLoopSettleSeconds = 0.25d;

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

        private static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputDirectoryName, OutputFileName));

        [MenuItem("DefenseGame/Smoke Tests/Dice Auto Animation")]
        public static void RunDiceAutoAnimationSmoke()
        {
            if (running)
            {
                return;
            }

            running = true;
            setupAt = 0d;
            evaluateAt = 0d;
            nextSampleAt = 0d;
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
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            try
            {
                SetupDiceAuto();
            }
            catch (Exception exception)
            {
                setupError = exception.ToString();
            }

            setupAt = EditorApplication.timeSinceStartup;
            evaluateAt = setupAt + ObservationSeconds;
            nextSampleAt = setupAt;
        }

        private static void SetupDiceAuto()
        {
            CharacterDatabase database = UnityEngine.Object.FindObjectOfType<CharacterDatabase>();
            if (database == null)
            {
                throw new InvalidOperationException("CharacterDatabase not found in DG scene.");
            }

            CharacterDefinition definition = database.GetCharacterById(HeroId);
            if (definition == null)
            {
                throw new InvalidOperationException(HeroId + " definition not found.");
            }

            GameObject prefab = definition.prefab != null
                ? definition.prefab
                : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minimi/dice_auto.prefab");
            if (prefab == null)
            {
                throw new InvalidOperationException("Dice Auto prefab not found.");
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
            instance.name = "DiceAutoAnimationSmoke_hero_56";
            unit = instance.GetComponent<DefenderUnit>();
            if (unit == null)
            {
                unit = instance.AddComponent<DefenderUnit>();
            }

            Transform firePoint = instance.transform.Find("FirePoint");
            if (firePoint == null)
            {
                GameObject firePointObject = new GameObject("FirePoint");
                firePointObject.transform.SetParent(instance.transform, false);
                firePointObject.transform.localPosition = new Vector3(0f, 0.8f, 0.6f);
                firePoint = firePointObject.transform;
            }

            unit.ConfigureRuntimePieces(null, firePoint, instance.GetComponentsInChildren<Renderer>(true));
            instance.SetActive(true);
            unit.Initialize(definition);
            animator = instance.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                throw new InvalidOperationException("Animator not found on Dice Auto instance.");
            }
        }

        private static void Tick()
        {
            if (!running || !EditorApplication.isPlaying)
            {
                return;
            }

            if (evaluateAt <= 0d)
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (animator != null && now >= nextSampleAt)
            {
                samples.Add(CaptureSample());
                nextSampleAt = now + SampleIntervalSeconds;
            }

            if (now < evaluateAt)
            {
                return;
            }

            Finish(BuildReport());
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

            StateSample[] settled = samples.Where(sample => sample.time >= SettledAfterSeconds).ToArray();
            bool animatorFound = animator != null;
            bool spawnLoopSeen = samples.Any(sample => sample.currentState == "Spawn_loop" || sample.nextState == "Spawn_loop");
            StateSample firstSpawnLoopSample = samples.FirstOrDefault(sample => sample.currentState == "Spawn_loop" || sample.nextState == "Spawn_loop");
            float firstSpawnLoopTime = firstSpawnLoopSample != null ? firstSpawnLoopSample.time : -1f;
            StateSample[] afterSpawnLoop = firstSpawnLoopTime >= 0f
                ? samples.Where(sample => sample.time >= firstSpawnLoopTime + (float)SpawnLoopSettleSeconds).ToArray()
                : Array.Empty<StateSample>();
            bool idleAfterSettled = settled.Any(sample => sample.currentState == "Idle" || sample.nextState == "Idle");
            bool spawnAfterSettled = settled.Any(sample => sample.currentState == "Spawn" || sample.nextState == "Spawn");
            int stateChangesAfterSettled = CountStateChanges(settled);
            bool idleAfterSpawnLoop = afterSpawnLoop.Any(sample => sample.currentState == "Idle" || sample.nextState == "Idle");
            bool spawnAfterSpawnLoop = afterSpawnLoop.Any(sample => sample.currentState == "Spawn" || sample.nextState == "Spawn");
            int stateChangesAfterSpawnLoop = CountStateChanges(afterSpawnLoop);
            StateSample finalSample = samples.Count > 0 ? samples[samples.Count - 1] : null;
            bool finalSpawnLoop = finalSample != null && finalSample.currentState == "Spawn_loop" && string.IsNullOrEmpty(finalSample.nextState);
            bool stableDormant = animatorFound && spawnLoopSeen && finalSpawnLoop && !idleAfterSpawnLoop && !spawnAfterSpawnLoop && stateChangesAfterSpawnLoop <= 1;
            bool passed = stableDormant && runtimeErrors == 0 && string.IsNullOrWhiteSpace(setupError);

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
                status = passed ? "pass" : "fail",
                passed = passed,
                animatorFound = animatorFound,
                animatorController = animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : string.Empty,
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
                finalState = finalSample != null ? finalSample.currentState : string.Empty,
                finalNextState = finalSample != null ? finalSample.nextState : string.Empty,
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
                    continue;
                }

                if (current != previous)
                {
                    changes++;
                    previous = current;
                }
            }

            return changes;
        }

        private static string ResolveStateName(AnimatorStateInfo stateInfo)
        {
            if (stateInfo.IsName("Spawn_loop") || stateInfo.IsName("Base Layer.Spawn_loop")) return "Spawn_loop";
            if (stateInfo.IsName("Spawn") || stateInfo.IsName("Base Layer.Spawn")) return "Spawn";
            if (stateInfo.IsName("Idle") || stateInfo.IsName("Base Layer.Idle")) return "Idle";
            if (stateInfo.IsName("Walk") || stateInfo.IsName("Base Layer.Walk")) return "Walk";
            if (stateInfo.IsName("Attack01") || stateInfo.IsName("Base Layer.Attack.Attack01")) return "Attack01";
            if (stateInfo.IsName("Attack02") || stateInfo.IsName("Base Layer.Attack.Attack02")) return "Attack02";
            if (stateInfo.IsName("Skill01") || stateInfo.IsName("Base Layer.Skill.Skill01")) return "Skill01";
            if (stateInfo.IsName("Skill02") || stateInfo.IsName("Base Layer.Skill.Skill02")) return "Skill02";
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
            File.WriteAllText(OutputPath, JsonUtility.ToJson(report, true));
            running = false;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            Application.logMessageReceived -= HandleLogMessage;
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(report.passed ? 0 : 1);
            }
        }

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
    }
}