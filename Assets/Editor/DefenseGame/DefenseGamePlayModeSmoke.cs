using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefenseGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame.Editor
{
    public static class DefenseGamePlayModeSmoke
    {
        private const string ScenePath = "Assets/Scenes/DG.unity";
        private const string OutputDirectoryName = "BatchPlaytestResults";
        private const string OutputFileName = "DefenseGame_PlayModeSmoke.json";
        private static readonly string[] PrefabPaths =
        {
            "Assets/Prefabs/Minimi/Dice_armor.prefab",
            "Assets/Prefabs/Minimi/dice_auto.prefab",
            "Assets/Prefabs/Minimi/Dice_Broken.prefab"
        };

        private static readonly string[] HeroIds = { "hero_55", "hero_56", "hero_57" };
        private static double evaluateAt;
        private static int runtimeErrors;
        private static bool running;
        private static bool previousEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions previousEnterPlayModeOptions;

        private static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputDirectoryName, OutputFileName));

        [MenuItem("DefenseGame/Smoke Tests/Vertical UI and New Units")]
        public static void RunPlayModeSmoke()
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
            List<string> notes = new List<string>();
            RuntimeSafeAreaFitter safeAreaFitter = UnityEngine.Object.FindObjectsOfType<RuntimeSafeAreaFitter>(true).FirstOrDefault();
            GameObject safeRoot = safeAreaFitter != null ? safeAreaFitter.gameObject : null;
            RectTransform safeRect = safeRoot != null ? safeRoot.GetComponent<RectTransform>() : null;
            bool safeAreaExists = safeRect != null && safeAreaFitter != null;
            bool safeAreaAnchorsValid = safeRect != null &&
                                        Approximately(safeRect.anchorMin, Vector2.zero) &&
                                        Approximately(safeRect.anchorMax, Vector2.one) &&
                                        safeRect.rect.width > 0f && safeRect.rect.height > 0f;
            if (!safeAreaExists || !safeAreaAnchorsValid)
            {
                notes.Add("SafeAreaRoot 또는 RuntimeSafeAreaFitter/anchor가 유효하지 않습니다.");
            }

            DefenseGameController controller = UnityEngine.Object.FindObjectOfType<DefenseGameController>();
            bool hpTen = controller != null && controller.Life == 10 && controller.MaxLife == 10;
            Text hpText = UnityEngine.Object.FindObjectsOfType<Text>(true).FirstOrDefault(text => text != null && text.name == "TopHpText");
            bool hpTextTen = hpText != null && hpText.text.Contains("10/10");
            if (!hpTen || !hpTextTen)
            {
                notes.Add("플레이어 HP 10/10 런타임 표시가 일치하지 않습니다.");
            }

            GamePresentationConfig presentation = AssetDatabase.LoadAssetAtPath<GamePresentationConfig>("Assets/Data/DefenseGamePresentationConfig.asset");
            bool defaultVfxConfigured = presentation != null &&
                                        presentation.projectilePrefab != null &&
                                        presentation.defaultMuzzleEffectPrefab != null &&
                                        presentation.defaultHitEffectPrefab != null &&
                                        presentation.defaultAreaEffectPrefab != null;
            if (!defaultVfxConfigured)
            {
                notes.Add("DefenseGamePresentationConfig 기본 투사체/머즐/히트/범위 VFX 중 빈 참조가 있습니다.");
            }

            CharacterDatabase database = UnityEngine.Object.FindObjectOfType<CharacterDatabase>();
            PrefabSmokeResult[] prefabResults = new PrefabSmokeResult[PrefabPaths.Length];
            for (int i = 0; i < PrefabPaths.Length; i++)
            {
                prefabResults[i] = EvaluatePrefab(PrefabPaths[i], HeroIds[i], database, presentation, i);
                if (!prefabResults[i].passed)
                {
                    notes.Add(HeroIds[i] + " prefab smoke failed: " + prefabResults[i].failureReason);
                }
            }

            bool passed = safeAreaExists && safeAreaAnchorsValid && hpTen && hpTextTen && defaultVfxConfigured && runtimeErrors == 0;
            for (int i = 0; i < prefabResults.Length; i++)
            {
                passed &= prefabResults[i].passed;
            }

            return new SmokeReport
            {
                status = passed ? "pass" : "fail",
                passed = passed,
                safeAreaExists = safeAreaExists,
                safeAreaAnchorsValid = safeAreaAnchorsValid,
                hpTen = hpTen,
                hpTextTen = hpTextTen,
                defaultVfxConfigured = defaultVfxConfigured,
                runtimeErrors = runtimeErrors,
                prefabs = prefabResults,
                notes = notes.ToArray()
            };
        }

        private static PrefabSmokeResult EvaluatePrefab(string prefabPath, string heroId, CharacterDatabase database, GamePresentationConfig presentation, int index)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return PrefabSmokeResult.Failure(heroId, prefabPath, "prefab_load_failed");
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, new Vector3((index - 1) * 2.2f, 0f, 0f), Quaternion.identity);
            instance.name = "Smoke_" + heroId;
            int missingScripts = CountMissingScripts(instance);
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Animator animator = instance.GetComponentInChildren<Animator>(true);
            RuntimeAnimatorController animatorController = animator != null ? animator.runtimeAnimatorController : null;
            AnimationClip[] clips = animatorController != null ? animatorController.animationClips : Array.Empty<AnimationClip>();
            string[] clipNames = clips.Where(clip => clip != null).Select(clip => clip.name).Distinct().OrderBy(name => name).ToArray();
            string[] eventKeys = ResolveAnimationEventKeys(clips);

            bool hasIdle = clipNames.Any(name => ContainsIgnoreCase(name, "idle"));
            bool hasAttack = clipNames.Any(name => ContainsIgnoreCase(name, "attack"));
            bool hasSkill = clipNames.Any(name => ContainsIgnoreCase(name, "skill"));
            bool hasSpawn = heroId != "hero_56" || clipNames.Any(name => ContainsIgnoreCase(name, "spawn"));
            bool expectedClips = hasIdle && hasAttack && hasSkill && hasSpawn;
            bool expectedEvents = HasExpectedEvents(heroId, eventKeys);

            CharacterDefinition definition = database != null ? database.GetCharacterById(heroId) : null;
            bool presentationBound = definition != null && definition.prefab != null;
            bool combatVisualBound = HasCombatVisualBinding(definition, presentation);
            bool passed = missingScripts == 0 && renderers.Length > 0 && animatorController != null && expectedClips && expectedEvents && presentationBound && combatVisualBound;
            string reason = passed
                ? string.Empty
                : string.Join(",", new[]
                {
                    missingScripts == 0 ? null : "missing_scripts=" + missingScripts,
                    renderers.Length > 0 ? null : "no_renderer",
                    animatorController != null ? null : "no_animator_controller",
                    expectedClips ? null : "missing_expected_clip",
                    expectedEvents ? null : "missing_animation_event",
                    presentationBound ? null : "presentation_prefab_unbound",
                    combatVisualBound ? null : "combat_vfx_unbound"
                }.Where(value => !string.IsNullOrEmpty(value)));

            UnityEngine.Object.Destroy(instance);
            return new PrefabSmokeResult
            {
                heroId = heroId,
                prefabPath = prefabPath,
                passed = passed,
                missingScripts = missingScripts,
                rendererCount = renderers.Length,
                animatorController = animatorController != null ? animatorController.name : string.Empty,
                clipNames = clipNames,
                eventKeys = eventKeys,
                presentationBound = presentationBound,
                combatVisualBound = combatVisualBound,
                failureReason = reason
            };
        }

        private static bool HasExpectedEvents(string heroId, string[] eventKeys)
        {
            if (heroId == "hero_55")
            {
                return eventKeys.Contains("AttackHit") && eventKeys.Contains("SkillHit");
            }

            if (heroId == "hero_56")
            {
                return eventKeys.Contains("SkillHit");
            }

            return eventKeys.Contains("FireProjectile") && eventKeys.Contains("SkillHit");
        }

        private static bool HasCombatVisualBinding(CharacterDefinition definition, GamePresentationConfig presentation)
        {
            if (definition == null || definition.attackBehavior == null || definition.skills == null || definition.skills.Count == 0)
            {
                return false;
            }

            bool defaultCombatVfx = presentation != null && presentation.defaultHitEffectPrefab != null && presentation.defaultAreaEffectPrefab != null;
            bool attackVisual = definition.attackBehavior.IsMelee ||
                                definition.attackBehavior.projectilePrefabOverride != null ||
                                definition.attackBehavior.muzzleEffectPrefab != null ||
                                definition.attackBehavior.hitEffectPrefab != null ||
                                presentation != null && presentation.projectilePrefab != null;
            bool skillVisual = definition.skills.Any(skill => skill != null &&
                (skill.projectilePrefab != null || skill.muzzleEffectPrefab != null || skill.hitEffectPrefab != null || skill.areaEffectPrefab != null));
            return attackVisual && (skillVisual || defaultCombatVfx);
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

        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return Vector2.SqrMagnitude(left - right) <= 0.0001f;
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
            public bool safeAreaExists;
            public bool safeAreaAnchorsValid;
            public bool hpTen;
            public bool hpTextTen;
            public bool defaultVfxConfigured;
            public int runtimeErrors;
            public PrefabSmokeResult[] prefabs = Array.Empty<PrefabSmokeResult>();
            public string[] notes = Array.Empty<string>();
        }

        [Serializable]
        private sealed class PrefabSmokeResult
        {
            public string heroId;
            public string prefabPath;
            public bool passed;
            public int missingScripts;
            public int rendererCount;
            public string animatorController;
            public string[] clipNames = Array.Empty<string>();
            public string[] eventKeys = Array.Empty<string>();
            public bool presentationBound;
            public bool combatVisualBound;
            public string failureReason;

            public static PrefabSmokeResult Failure(string heroId, string prefabPath, string reason)
            {
                return new PrefabSmokeResult { heroId = heroId, prefabPath = prefabPath, passed = false, failureReason = reason };
            }
        }
    }
}
