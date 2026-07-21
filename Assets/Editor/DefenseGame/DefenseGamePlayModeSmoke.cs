using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

            bool portraitProfilesValid = ValidatePortraitSafeAreaProfiles();
            if (!portraitProfilesValid)
            {
                notes.Add("세로 실기기 Safe Area 프로필의 정규화 anchor 검증에 실패했습니다.");
            }

            DefenseGameController controller = UnityEngine.Object.FindObjectOfType<DefenseGameController>();
            bool hpTen = controller != null && controller.Life == 10 && controller.MaxLife == 10;
            bool simultaneousDeathPolicyValid = ValidateSimultaneousDeathPolicy();
            if (!simultaneousDeathPolicyValid)
            {
                notes.Add("동시사망 승리 우선 정책 회귀 검증에 실패했습니다.");
            }

            Text hpText = UnityEngine.Object.FindObjectsOfType<Text>(true).FirstOrDefault(text => text != null && text.name == "TopHpText");
            bool hpTextTen = hpText != null && hpText.text.Contains("10/10");
            if (!hpTen || !hpTextTen)
            {
                notes.Add("플레이어 HP 10/10 런타임 표시가 일치하지 않습니다.");
            }

            Button fateEntryButton = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(button => button != null && button.name == "FatePanelReopenButton");
            RectTransform fateEntryRect = fateEntryButton != null ? fateEntryButton.GetComponent<RectTransform>() : null;
            Shadow fateEntryShadow = fateEntryButton != null ? fateEntryButton.GetComponents<Shadow>().FirstOrDefault(effect => !(effect is Outline)) : null;
            Outline fateEntryOutline = fateEntryButton != null ? fateEntryButton.GetComponent<Outline>() : null;
            Graphic fateEntryGraphic = fateEntryButton != null && fateEntryButton.targetGraphic != null
                ? fateEntryButton.targetGraphic
                : fateEntryButton != null ? fateEntryButton.GetComponent<Graphic>() : null;
            Text fateEntryText = fateEntryButton != null ? fateEntryButton.GetComponentInChildren<Text>(true) : null;
            bool fateEntryLayoutValid = fateEntryRect != null &&
                                        Approximately(fateEntryRect.sizeDelta, new Vector2(250f, 84f)) &&
                                        Approximately(fateEntryRect.anchoredPosition, new Vector2(-80f, 356f)) &&
                                        fateEntryShadow != null &&
                                        Approximately(fateEntryShadow.effectDistance, new Vector2(0f, -4f)) &&
                                        fateEntryShadow.useGraphicAlpha &&
                                        fateEntryOutline != null &&
                                        Approximately(fateEntryOutline.effectDistance, new Vector2(2f, -2f)) &&
                                        fateEntryOutline.useGraphicAlpha;
            bool fateEntryPastelColorValid = fateEntryGraphic != null &&
                                              Approximately(fateEntryGraphic.color, new Color(0.30f, 0.52f, 0.38f, 0.98f)) &&
                                              fateEntryText != null &&
                                              Approximately(fateEntryText.color, new Color(0.97f, 1.00f, 0.97f, 1f));
            bool fateEntryIdleAtFullHealth = controller != null && controller.Life > 3 && !controller.FateSurvivalCrisisActive;
            if (!fateEntryLayoutValid || !fateEntryPastelColorValid || !fateEntryIdleAtFullHealth)
            {
                string actualBackground = fateEntryGraphic != null ? fateEntryGraphic.color.ToString() : "null";
                string actualText = fateEntryText != null ? fateEntryText.color.ToString() : "null";
                notes.Add("운명카드 버튼의 하단 HUD 정렬, 녹색 팔레트 또는 HP 3 초과 정지 상태가 유효하지 않습니다. " +
                          "background=" + actualBackground + ", text=" + actualText);
            }

            Button lobbyEntryButton = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(button => button != null && button.name == "LobbyBattleButton");
            bool initialPreparationFlowValid = controller != null &&
                                               controller.CurrentRound <= 0 &&
                                               !controller.IsRoundRunning &&
                                               lobbyEntryButton != null;
            if (initialPreparationFlowValid)
            {
                lobbyEntryButton.onClick.Invoke();
                initialPreparationFlowValid = controller.CurrentRound <= 0 && !controller.IsRoundRunning;
            }
            if (!initialPreparationFlowValid)
            {
                notes.Add("전장 입장 후 다음 라운드를 누르기 전까지 R1 카운트다운이 대기하지 않습니다.");
            }

            bool earlyMiniShopChoicesValid = ValidateRoundTieredMiniShop(out string earlyMiniShopSummary);
            if (!earlyMiniShopChoicesValid)
            {
                notes.Add("R3 소형 전투상점의 3개 선택지 분류/가격 검증에 실패했습니다. " + earlyMiniShopSummary);
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
            CharacterDefinition hero32 = database != null ? database.GetCharacterById("hero_32") : null;
            SkillDefinition hero32Skill = hero32 != null && hero32.skills != null && hero32.skills.Count > 0 ? hero32.skills[0] : null;
            bool hero32SignatureValid = hero32Skill != null &&
                                        hero32Skill.effectType == SkillEffectType.DamageSlow &&
                                        Mathf.Approximately(hero32Skill.power, 2.2f) &&
                                        Mathf.Approximately(hero32Skill.secondaryPower, 0.35f) &&
                                        Mathf.Approximately(hero32Skill.duration, 4f);
            if (!hero32SignatureValid)
            {
                notes.Add("hero_32 야성의 추적탄 프리셋이 확정 수치와 일치하지 않습니다.");
            }

            PrefabSmokeResult[] prefabResults = new PrefabSmokeResult[PrefabPaths.Length];
            for (int i = 0; i < PrefabPaths.Length; i++)
            {
                prefabResults[i] = EvaluatePrefab(PrefabPaths[i], HeroIds[i], database, presentation, i);
                if (!prefabResults[i].passed)
                {
                    notes.Add(HeroIds[i] + " prefab smoke failed: " + prefabResults[i].failureReason);
                }
            }

            bool passed = safeAreaExists && safeAreaAnchorsValid && portraitProfilesValid && hpTen && hpTextTen && simultaneousDeathPolicyValid && fateEntryLayoutValid && fateEntryPastelColorValid && fateEntryIdleAtFullHealth && initialPreparationFlowValid && earlyMiniShopChoicesValid && hero32SignatureValid && defaultVfxConfigured && runtimeErrors == 0;
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
                portraitProfilesValid = portraitProfilesValid,
                hpTen = hpTen,
                hpTextTen = hpTextTen,
                fateEntryLayoutValid = fateEntryLayoutValid,
                fateEntryPastelColorValid = fateEntryPastelColorValid,
                fateEntryIdleAtFullHealth = fateEntryIdleAtFullHealth,
                initialPreparationFlowValid = initialPreparationFlowValid,
                earlyMiniShopChoicesValid = earlyMiniShopChoicesValid,
                earlyMiniShopSummary = earlyMiniShopSummary,
                simultaneousDeathPolicyValid = simultaneousDeathPolicyValid,
                hero32SignatureValid = hero32SignatureValid,
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

        private static bool ValidatePortraitSafeAreaProfiles()
        {
            return ValidatePortraitSafeAreaProfile(new Vector2Int(720, 1600), new Rect(0f, 48f, 720f, 1504f)) &&
                   ValidatePortraitSafeAreaProfile(new Vector2Int(1080, 2400), new Rect(0f, 96f, 1080f, 2220f)) &&
                   ValidatePortraitSafeAreaProfile(new Vector2Int(1179, 2556), new Rect(0f, 102f, 1179f, 2277f));
        }

        private static bool ValidateSimultaneousDeathPolicy()
        {
            return DefenseGameController.IsSimultaneousDeathVictory(12, 11, 1) &&
                   DefenseGameController.IsSimultaneousDeathVictory(12, 10, 2) &&
                   DefenseGameController.IsSimultaneousDeathVictory(12, 12, 0) &&
                   !DefenseGameController.IsSimultaneousDeathVictory(12, 11, 0) &&
                   !DefenseGameController.IsSimultaneousDeathVictory(12, 9, 2) &&
                   !DefenseGameController.IsSimultaneousDeathVictory(0, 0, 1);
        }

        private static bool ValidatePortraitSafeAreaProfile(Vector2Int screenSize, Rect safeArea)
        {
            RuntimeSafeAreaFitter.CalculateSafeAreaAnchors(safeArea, screenSize, out Vector2 anchorMin, out Vector2 anchorMax);
            return screenSize.y > screenSize.x &&
                   anchorMin.x >= 0f && anchorMin.y >= 0f &&
                   anchorMax.x <= 1f && anchorMax.y <= 1f &&
                   anchorMin.x < anchorMax.x && anchorMin.y < anchorMax.y &&
                   Approximately(anchorMin, new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y)) &&
                   Approximately(anchorMax, new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y));
        }

        private static bool ValidateRoundTieredMiniShop(out string summary)
        {
            RunShopSystem shop = UnityEngine.Object.FindObjectOfType<RunShopSystem>();
            DefenseGameController controller = UnityEngine.Object.FindObjectOfType<DefenseGameController>();
            if (shop == null || controller == null)
            {
                summary = "shop_or_controller_missing";
                return false;
            }

            BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo buildOffers = typeof(RunShopSystem).GetMethod("BuildOffers", instanceFlags);
            FieldInfo offersField = typeof(RunShopSystem).GetField("currentOffers", instanceFlags);
            FieldInfo goldField = typeof(DefenseGameController).GetField("<Gold>k__BackingField", instanceFlags);
            FieldInfo summonCostField = typeof(DefenseGameController).GetField("currentSummonBaseCost", instanceFlags);
            if (buildOffers == null || offersField == null || goldField == null || summonCostField == null)
            {
                summary = "reflection_target_missing";
                return false;
            }

            object originalGold = goldField.GetValue(controller);
            object originalSummonCost = summonCostField.GetValue(controller);
            IList offers = null;
            try
            {
                goldField.SetValue(controller, 34);
                summonCostField.SetValue(controller, 16);
                buildOffers.Invoke(shop, new object[] { 3, true, false, false });
                offers = offersField.GetValue(shop) as IList;
                if (offers == null || offers.Count != 3)
                {
                    summary = "offer_count=" + (offers != null ? offers.Count : -1);
                    return false;
                }

                HashSet<string> expectedTypes = new HashSet<string>
                {
                    "RandomUnit",
                    "MergeAssist",
                    "Coupon"
                };
                List<string> snapshots = new List<string>();
                Dictionary<string, int> expectedPrices = new Dictionary<string, int>
                {
                    { "RandomUnit", 19 },
                    { "MergeAssist", 20 },
                    { "Coupon", 18 }
                };
                Dictionary<string, int> firstPrices = new Dictionary<string, int>();
                bool fixedPricesValid = true;
                bool couponDurationValid = false;
                for (int i = 0; i < offers.Count; i++)
                {
                    object offer = offers[i];
                    Type offerType = offer.GetType();
                    string typeName = offerType.GetField("type", instanceFlags)?.GetValue(offer)?.ToString() ?? string.Empty;
                    string title = offerType.GetField("title", instanceFlags)?.GetValue(offer) as string ?? string.Empty;
                    string description = offerType.GetField("description", instanceFlags)?.GetValue(offer) as string ?? string.Empty;
                    int cost = (int)(offerType.GetField("cost", instanceFlags)?.GetValue(offer) ?? int.MaxValue);
                    expectedTypes.Remove(typeName);
                    fixedPricesValid &= expectedPrices.TryGetValue(typeName, out int expectedCost) && cost == expectedCost;
                    firstPrices[typeName] = cost;
                    if (typeName == "Coupon")
                    {
                        couponDurationValid = title.Contains("4라운드") && description.Contains("18%");
                    }
                    snapshots.Add(typeName + "=" + cost + "G");
                }

                goldField.SetValue(controller, 1);
                summonCostField.SetValue(controller, 60);
                buildOffers.Invoke(shop, new object[] { 3, true, false, false });
                IList repricedOffers = offersField.GetValue(shop) as IList;
                bool pricesInvariant = repricedOffers != null && repricedOffers.Count == 3;
                if (repricedOffers != null)
                {
                    for (int i = 0; i < repricedOffers.Count; i++)
                    {
                        object offer = repricedOffers[i];
                        Type offerType = offer.GetType();
                        string typeName = offerType.GetField("type", instanceFlags)?.GetValue(offer)?.ToString() ?? string.Empty;
                        int cost = (int)(offerType.GetField("cost", instanceFlags)?.GetValue(offer) ?? int.MaxValue);
                        pricesInvariant &= firstPrices.TryGetValue(typeName, out int firstCost) && cost == firstCost;
                    }
                }

                summary = string.Join(", ", snapshots) + " | gold/summon invariant=" + pricesInvariant;
                return expectedTypes.Count == 0 && fixedPricesValid && pricesInvariant && couponDurationValid;
            }
            catch (Exception exception)
            {
                summary = exception.GetType().Name + ":" + exception.Message;
                return false;
            }
            finally
            {
                offers?.Clear();
                goldField.SetValue(controller, originalGold);
                summonCostField.SetValue(controller, originalSummonCost);
            }
        }

        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return Vector2.SqrMagnitude(left - right) <= 0.0001f;
        }

        private static bool Approximately(Color left, Color right)
        {
            Vector4 delta = (Vector4)left - (Vector4)right;
            return delta.sqrMagnitude <= 0.0004f;
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
            public bool portraitProfilesValid;
            public bool hpTen;
            public bool hpTextTen;
            public bool fateEntryLayoutValid;
            public bool fateEntryPastelColorValid;
            public bool fateEntryIdleAtFullHealth;
            public bool initialPreparationFlowValid;
            public bool earlyMiniShopChoicesValid;
            public string earlyMiniShopSummary;
            public bool simultaneousDeathPolicyValid;
            public bool hero32SignatureValid;
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
