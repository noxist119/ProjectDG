using System;
using System.IO;
using DefenseGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DefenseGame.Editor
{
    public static class BossStatusImmunitySmoke
    {
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

        private static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputDirectoryName, OutputFileName));

        [MenuItem("DefenseGame/Smoke Tests/Boss Status Immunity")]
        public static void RunBossStatusImmunitySmoke()
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
                SetupBoss();
                evaluateAt = EditorApplication.timeSinceStartup + 0.35d;
            }
            catch (Exception exception)
            {
                Finish(false, false, false, false, new[] { exception.ToString() });
            }
        }

        private static void SetupBoss()
        {
            GameObject bossObject = new GameObject("BossStatusImmunitySmoke_Boss");
            bossObject.transform.position = new Vector3(2f, 0f, 2f);
            GameObject visualObject = new GameObject("AnimatedVisual");
            visualObject.transform.SetParent(bossObject.transform, false);
            bossAnimator = visualObject.AddComponent<Animator>();
            bossAnimator.speed = OriginalAnimatorSpeed;
            boss = bossObject.AddComponent<MonsterUnit>();
            boss.Initialize(new MonsterDefinition
            {
                id = "smoke_status_immune_boss",
                displayName = "Status Immune Boss",
                role = MonsterRole.Boss,
                threatLevel = MonsterThreatLevel.Boss,
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
            }, null);

            bossAnimator.speed = OriginalAnimatorSpeed;
            initialHealth = boss.CurrentHealth;
            initialPosition = boss.transform.position;
            boss.ApplySlow(0.8f, 5f);
            boss.ApplyAttackSpeedSlow(0.8f, 5f);
            boss.ApplyPoison(50f, 1f, 0.2f, null);
            boss.ApplyKnockback(10f, Vector3.zero);
            boss.ApplyStun(5f);
            boss.ApplyPetrify(5f);
        }

        private static void Tick()
        {
            if (!running || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < evaluateAt)
            {
                return;
            }

            bool controlsBlocked = boss != null && boss.IsStatusEffectImmune && !boss.IsStunned && !boss.IsPetrified;
            bool presentationUnchanged = bossAnimator != null && Mathf.Approximately(bossAnimator.speed, OriginalAnimatorSpeed);
            bool positionUnchanged = boss != null && Vector3.Distance(boss.transform.position, initialPosition) <= 0.001f;
            bool damageOverTimeBlocked = boss != null && Mathf.Approximately(boss.CurrentHealth, initialHealth);
            float healthBeforeDirectDamage = boss != null ? boss.CurrentHealth : 0f;
            boss?.TakeDamage(100f, false, null);
            bool directDamageAllowed = boss != null && boss.CurrentHealth < healthBeforeDirectDamage;

            Finish(
                controlsBlocked,
                presentationUnchanged && positionUnchanged,
                damageOverTimeBlocked,
                directDamageAllowed,
                Array.Empty<string>());
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
                status = passed ? "pass" : "fail",
                passed = passed,
                controlsBlocked = controlsBlocked,
                presentationUnchanged = presentationUnchanged,
                damageOverTimeBlocked = damageOverTimeBlocked,
                directDamageAllowed = directDamageAllowed,
                runtimeErrors = runtimeErrors,
                notes = notes ?? Array.Empty<string>()
            };
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
                EditorApplication.Exit(passed ? 0 : 1);
            }
        }

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
    }
}
