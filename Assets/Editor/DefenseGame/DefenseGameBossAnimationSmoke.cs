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
    public static class DefenseGameBossAnimationSmoke
    {
        private const string ScenePath = "Assets/Scenes/DG.unity";
        private const string OutputDirectoryName = "BatchPlaytestResults";
        private const string OutputFileName = "DefenseGame_BossAnimationSmoke.json";
        private const string MonsterCombatTuningPath = "Assets/Data/MonsterCombatTuningConfig.asset";

        private static readonly string[] MonsterIds =
        {
            "mob_01", "mob_02", "mob_03", "mob_04", "mob_05", "mob_06", "mob_07", "mob_08", "mob_09", "mob_10", "mob_11"
        };

        private static readonly string[] PrefabPaths =
        {
            "Assets/Prefabs/Minimi/Boss_Golem.prefab",
            "Assets/Prefabs/Minimi/Boss_Golem_Minion_Type1.prefab",
            "Assets/Prefabs/Minimi/Boss_Golem_Minion_Type2.prefab",
            "Assets/Prefabs/Minimi/Boss_Leon.prefab",
            "Assets/Prefabs/Minimi/Boss_Leon_Minion_Type1.prefab",
            "Assets/Prefabs/Minimi/Boss_Leon_Minion_Type2.prefab",
            "Assets/Prefabs/Minimi/Boss_Magician.prefab",
            "Assets/Prefabs/Minimi/Boss_Magician_Minion_Type1.prefab",
            "Assets/Prefabs/Minimi/Boss_Magician_Minion_Type2.prefab",
            "Assets/Prefabs/Minimi/Boss_Slime.prefab",
            "Assets/Prefabs/Minimi/Boss_TurtleShell.prefab"
        };

        private static readonly bool[] ExpectedMelee = { true, true, true, true, false, true, false, true, true, true, true };
        private static readonly float[] ExpectedRange = { 2.6f, 1.8f, 1.9f, 2.4f, 3f, 1.8f, 3f, 1.5f, 1.8f, 2.2f, 2f };
        private static readonly bool[] ExpectedSkillAnimation = { true, false, false, true, true, true, true, true, false, true, true };

        private static double evaluateAt;
        private static bool running;
        private static int runtimeErrors;
        private static bool previousEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions previousEnterPlayModeOptions;

        private static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputDirectoryName, OutputFileName));

        [MenuItem("DefenseGame/Smoke Tests/Boss and Monster Animations")]
        public static void RunBossAnimationSmoke()
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
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                evaluateAt = EditorApplication.timeSinceStartup + 2.5d;
            }
        }

        private static void Tick()
        {
            if (!running || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < evaluateAt)
            {
                return;
            }

            SmokeReport report;
            try
            {
                report = Evaluate();
            }
            catch (Exception exception)
            {
                report = new SmokeReport
                {
                    status = "exception",
                    passed = false,
                    runtimeErrors = runtimeErrors + 1,
                    notes = new[] { exception.ToString() }
                };
            }

            File.WriteAllText(OutputPath, JsonUtility.ToJson(report, true));
            Finish(report.passed ? 0 : 1);
        }

        private static SmokeReport Evaluate()
        {
            MonsterDatabase database = UnityEngine.Object.FindObjectOfType<MonsterDatabase>();
            MonsterCombatTuningConfig combatTuning = AssetDatabase.LoadAssetAtPath<MonsterCombatTuningConfig>(MonsterCombatTuningPath);
            if (database != null && combatTuning != null)
            {
                database.ApplyCombatTuningConfig(combatTuning);
            }

            List<string> notes = new List<string>();
            MonsterSmokeResult[] monsters = new MonsterSmokeResult[MonsterIds.Length];
            bool passed = database != null && runtimeErrors == 0;
            if (database == null)
            {
                notes.Add("MonsterDatabase를 찾지 못했습니다.");
            }
            else if (combatTuning == null)
            {
                passed = false;
                notes.Add("MonsterCombatTuningConfig asset을 찾지 못했습니다: " + MonsterCombatTuningPath);
            }

            for (int i = 0; i < MonsterIds.Length; i++)
            {
                monsters[i] = EvaluateMonster(database, i);
                if (!monsters[i].passed)
                {
                    passed = false;
                    notes.Add(MonsterIds[i] + " smoke failed: " + monsters[i].failureReason);
                }
            }

            return new SmokeReport
            {
                status = passed ? "pass" : "fail",
                passed = passed,
                runtimeErrors = runtimeErrors,
                monsters = monsters,
                notes = notes.ToArray()
            };
        }

        private static MonsterSmokeResult EvaluateMonster(MonsterDatabase database, int index)
        {
            string monsterId = MonsterIds[index];
            string prefabPath = PrefabPaths[index];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return MonsterSmokeResult.Failure(monsterId, prefabPath, "prefab_load_failed");
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = "Smoke_" + monsterId;
            instance.SetActive(false);
            int missingScripts = CountMissingScripts(instance);
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Animator animator = instance.GetComponentInChildren<Animator>(true);
            RuntimeAnimatorController animatorController = animator != null ? animator.runtimeAnimatorController : null;
            AnimationClip[] clips = animatorController != null ? animatorController.animationClips : Array.Empty<AnimationClip>();
            string[] clipNames = clips.Where(clip => clip != null).Select(clip => clip.name).Distinct().OrderBy(name => name).ToArray();
            string[] eventKeys = ResolveAnimationEventKeys(clips);
            string expectedAttackEvent = ExpectedMelee[index] ? "AttackHit" : "FireProjectile";
            bool hasAttackClip = clipNames.Any(name => ContainsIgnoreCase(name, "attack"));
            bool attackEventBound = eventKeys.Contains(expectedAttackEvent);
            bool hasSkillClip = clipNames.Any(name => ContainsIgnoreCase(name, "skill") || ContainsIgnoreCase(name, "taunt"));
            bool skillEventBound = !ExpectedSkillAnimation[index] || hasSkillClip && eventKeys.Contains("SkillHit");

            MonsterDefinition definition = FindDefinition(database, monsterId);
            AttackBehavior attack = definition != null ? definition.attackBehavior : null;
            float actualRange = attack != null && definition.stats != null
                ? attack.ResolveAttackRange(definition.stats.attackRange)
                : -1f;
            bool typeAndRangeBound = attack != null && attack.IsMelee == ExpectedMelee[index] && Mathf.Abs(actualRange - ExpectedRange[index]) <= 0.16f;
            bool attackVisualBound = attack != null &&
                                     (attack.hitEffectPrefab != null || attack.muzzleEffectPrefab != null || !attack.IsMelee && attack.projectilePrefabOverride != null);
            bool skillVisualBound = !ExpectedSkillAnimation[index] || definition != null && definition.skills != null && definition.skills.Any(skill =>
                skill != null && (skill.projectilePrefab != null || skill.muzzleEffectPrefab != null || skill.hitEffectPrefab != null || skill.areaEffectPrefab != null));
            bool skillDefinitionBound = HasExpectedSkillDefinition(monsterId, definition);

            bool passed = missingScripts == 0 && renderers.Length > 0 && animatorController != null && hasAttackClip &&
                          attackEventBound && skillEventBound && definition != null && typeAndRangeBound && attackVisualBound && skillVisualBound && skillDefinitionBound;
            string reason = passed
                ? string.Empty
                : string.Join(",", new[]
                {
                    missingScripts == 0 ? null : "missing_scripts=" + missingScripts,
                    renderers.Length > 0 ? null : "no_renderer",
                    animatorController != null ? null : "no_animator_controller",
                    hasAttackClip ? null : "missing_attack_clip",
                    attackEventBound ? null : "missing_" + expectedAttackEvent,
                    skillEventBound ? null : "missing_SkillHit",
                    definition != null ? null : "monster_definition_unbound",
                    typeAndRangeBound ? null : "attack_type_or_range_mismatch=" + actualRange.ToString("0.00"),
                    attackVisualBound ? null : "attack_vfx_unbound",
                    skillVisualBound ? null : "skill_vfx_unbound",
                    skillDefinitionBound ? null : "skill_definition_mismatch"
                }.Where(value => !string.IsNullOrEmpty(value)));

            UnityEngine.Object.Destroy(instance);
            return new MonsterSmokeResult
            {
                monsterId = monsterId,
                prefabPath = prefabPath,
                passed = passed,
                expectedAttackEvent = expectedAttackEvent,
                actualRange = actualRange,
                missingScripts = missingScripts,
                rendererCount = renderers.Length,
                animatorController = animatorController != null ? animatorController.name : string.Empty,
                clipNames = clipNames,
                eventKeys = eventKeys,
                attackEventBound = attackEventBound,
                skillEventBound = skillEventBound,
                attackVisualBound = attackVisualBound,
                skillVisualBound = skillVisualBound,
                failureReason = reason
            };
        }

        private static MonsterDefinition FindDefinition(MonsterDatabase database, string monsterId)
        {
            if (database == null)
            {
                return null;
            }

            return database.Monsters.Concat(database.MidBosses).Concat(database.Bosses)
                .FirstOrDefault(definition => definition != null && string.Equals(definition.id, monsterId, StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasExpectedSkillDefinition(string monsterId, MonsterDefinition definition)
        {
            if (monsterId != "mob_10" && monsterId != "mob_11")
            {
                return true;
            }

            if (definition == null || definition.skills == null || definition.skills.Count != 1 || definition.skills[0] == null)
            {
                return false;
            }

            SkillDefinition skill = definition.skills[0];
            if (Mathf.Abs(skill.power - 0.10f) > 0.001f || Mathf.Abs(skill.duration - 5f) > 0.01f)
            {
                return false;
            }

            return monsterId == "mob_10"
                ? skill.effectType == SkillEffectType.AttackPowerReduction
                : skill.effectType == SkillEffectType.DamageReflect;
        }

        private static string[] ResolveAnimationEventKeys(AnimationClip[] clips)
        {
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip == null)
                {
                    continue;
                }

                AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
                for (int eventIndex = 0; eventIndex < events.Length; eventIndex++)
                {
                    AnimationEvent animationEvent = events[eventIndex];
                    if (animationEvent != null && !string.IsNullOrWhiteSpace(animationEvent.functionName))
                    {
                        keys.Add(animationEvent.functionName);
                    }
                }
            }

            return keys.OrderBy(key => key).ToArray();
        }

        private static int CountMissingScripts(GameObject root)
        {
            int count = 0;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null)
                {
                    count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transforms[i].gameObject);
                }
            }

            return count;
        }

        private static bool ContainsIgnoreCase(string value, string fragment)
        {
            return value != null && value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                runtimeErrors++;
            }
        }

        private static void Finish(int exitCode)
        {
            running = false;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            Application.logMessageReceived -= HandleLogMessage;
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
        }

        [Serializable]
        private sealed class SmokeReport
        {
            public string status;
            public bool passed;
            public int runtimeErrors;
            public MonsterSmokeResult[] monsters = Array.Empty<MonsterSmokeResult>();
            public string[] notes = Array.Empty<string>();
        }

        [Serializable]
        private sealed class MonsterSmokeResult
        {
            public string monsterId;
            public string prefabPath;
            public bool passed;
            public string expectedAttackEvent;
            public float actualRange;
            public int missingScripts;
            public int rendererCount;
            public string animatorController;
            public string[] clipNames = Array.Empty<string>();
            public string[] eventKeys = Array.Empty<string>();
            public bool attackEventBound;
            public bool skillEventBound;
            public bool attackVisualBound;
            public bool skillVisualBound;
            public string failureReason;

            public static MonsterSmokeResult Failure(string monsterId, string prefabPath, string reason)
            {
                return new MonsterSmokeResult { monsterId = monsterId, prefabPath = prefabPath, passed = false, failureReason = reason };
            }
        }
    }
}
