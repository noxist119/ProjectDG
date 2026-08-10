using System;
using System.IO;
using DefenseGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DefenseGame.Editor
{
    public static class PetrifyCombatExitSmoke
    {
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
        private static bool damageAppliedWhilePetrified;
        private static bool previousEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions previousEnterPlayModeOptions;

        private static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputDirectoryName, OutputFileName));

        [MenuItem("DefenseGame/Smoke Tests/Petrify Combat Exit")]
        public static void RunPetrifyCombatExitSmoke()
        {
            if (running)
            {
                return;
            }

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
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
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
                SetupPetrifiedMonster();
                evaluateAt = EditorApplication.timeSinceStartup + 0.4d;
            }
            catch (Exception exception)
            {
                WriteAndFinish(new SmokeReport
                {
                    status = "exception",
                    passed = false,
                    runtimeErrors = runtimeErrors + 1,
                    notes = new[] { exception.ToString() }
                });
            }
        }

        private static void SetupPetrifiedMonster()
        {
            GameObject monsterObject = new GameObject("PetrifyCombatExitSmoke_Monster");
            GameObject visualObject = new GameObject("AnimatedVisual");
            visualObject.transform.SetParent(monsterObject.transform, false);
            animator = visualObject.AddComponent<Animator>();
            animator.speed = OriginalAnimatorSpeed;
            monster = monsterObject.AddComponent<MonsterUnit>();
            monster.Initialize(new MonsterDefinition
            {
                id = "smoke_petrify_target",
                displayName = "Petrify Smoke Target",
                role = MonsterRole.Grunt,
                threatLevel = MonsterThreatLevel.Regular,
                stats = new CombatStats
                {
                    maxHealth = 100f,
                    attackPower = 5f,
                    attackSpeed = 1f,
                    maxMana = 100f,
                    attackRange = 1.5f,
                    moveSpeed = 0f
                }
            }, null);

            initialHealth = monster.CurrentHealth;
            animator.speed = OriginalAnimatorSpeed;
            monster.ApplyPetrify(0.2f);
            immediatePetrified = monster.IsPetrified &&
                                 monster.CanBeCombatTargeted &&
                                 Mathf.Approximately(animator.speed, 0f);
            monster.TakeDamage(25f, false, null);
            damageAppliedWhilePetrified = monster.CurrentHealth < initialHealth;
        }

        private static void Tick()
        {
            if (!running || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < evaluateAt)
            {
                return;
            }

            bool released = monster != null && !monster.IsPetrified && monster.CanBeCombatTargeted;
            bool animationResumed = animator != null && Mathf.Approximately(animator.speed, OriginalAnimatorSpeed);
            float healthBeforeReleasedHit = monster != null ? monster.CurrentHealth : 0f;
            monster?.TakeDamage(25f, false, null);
            bool damageRestored = monster != null && monster.CurrentHealth < healthBeforeReleasedHit;
            bool passed = immediatePetrified && damageAppliedWhilePetrified && released && animationResumed && damageRestored && runtimeErrors == 0;

            WriteAndFinish(new SmokeReport
            {
                status = passed ? "pass" : "fail",
                passed = passed,
                immediatePetrified = immediatePetrified,
                damageAppliedWhilePetrified = damageAppliedWhilePetrified,
                released = released,
                animationResumed = animationResumed,
                damageRestored = damageRestored,
                runtimeErrors = runtimeErrors,
                notes = Array.Empty<string>()
            });
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
            public bool immediatePetrified;
            public bool damageAppliedWhilePetrified;
            public bool released;
            public bool animationResumed;
            public bool damageRestored;
            public int runtimeErrors;
            public string[] notes = Array.Empty<string>();
        }
    }
}
