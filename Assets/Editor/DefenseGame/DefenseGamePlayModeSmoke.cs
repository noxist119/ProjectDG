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
            bool runResetStateValid = false;
            bool runSeedRepeatValid = false;
            bool runContentChannelIsolationValid = false;
            if (controller != null)
            {
                int expectedBaseMaxLife = controller.MaxLife;
                controller.IncreaseMaxLife(3);
                controller.ResetRunForRetry();
                runResetStateValid = controller.MaxLife == expectedBaseMaxLife && controller.Life == expectedBaseMaxLife;

                controller.SetRunContentSeedOverride(314159);
                controller.ResetRunForRetry();
                int firstSummonSample = controller.RunContentRandom.Range(RunContentRandomChannel.Summon, 0, int.MaxValue, "smoke.summon");
                int firstBoardSample = controller.RunContentRandom.Range(RunContentRandomChannel.Board, 0, int.MaxValue, "smoke.board");
                controller.ResetRunForRetry();
                int secondSummonSample = controller.RunContentRandom.Range(RunContentRandomChannel.Summon, 0, int.MaxValue, "smoke.summon");
                int secondBoardSample = controller.RunContentRandom.Range(RunContentRandomChannel.Board, 0, int.MaxValue, "smoke.board");
                runSeedRepeatValid = firstSummonSample == secondSummonSample && firstBoardSample == secondBoardSample;
                RunContentRandomService summonWithShop = new RunContentRandomService();
                summonWithShop.Reset(314159);
                int summonWithShopFirst = summonWithShop.Range(RunContentRandomChannel.Summon, 0, int.MaxValue, "smoke.summon.first");
                summonWithShop.Range(RunContentRandomChannel.Shop, 0, int.MaxValue, "smoke.shop.first");
                summonWithShop.Range(RunContentRandomChannel.Shop, 0, int.MaxValue, "smoke.shop.second");
                summonWithShop.Range(RunContentRandomChannel.Shop, 0, int.MaxValue, "smoke.shop.third");
                int summonWithShopSecond = summonWithShop.Range(RunContentRandomChannel.Summon, 0, int.MaxValue, "smoke.summon.second");

                RunContentRandomService summonControl = new RunContentRandomService();
                summonControl.Reset(314159);
                int summonControlFirst = summonControl.Range(RunContentRandomChannel.Summon, 0, int.MaxValue, "smoke.summon.first");
                int summonControlSecond = summonControl.Range(RunContentRandomChannel.Summon, 0, int.MaxValue, "smoke.summon.second");

                RunContentRandomService shopAfterSummon = new RunContentRandomService();
                shopAfterSummon.Reset(314159);
                shopAfterSummon.Range(RunContentRandomChannel.Summon, 0, int.MaxValue, "smoke.summon.first");
                shopAfterSummon.Range(RunContentRandomChannel.Summon, 0, int.MaxValue, "smoke.summon.second");
                int shopAfterSummonSample = shopAfterSummon.Range(RunContentRandomChannel.Shop, 0, int.MaxValue, "smoke.shop.first");

                RunContentRandomService shopControl = new RunContentRandomService();
                shopControl.Reset(314159);
                int shopControlSample = shopControl.Range(RunContentRandomChannel.Shop, 0, int.MaxValue, "smoke.shop.first");

                RunContentRandomService unrelatedChannelProbe = new RunContentRandomService();
                unrelatedChannelProbe.Reset(314159);
                int unrelatedSummonSample = unrelatedChannelProbe.Range(RunContentRandomChannel.Summon, 0, int.MaxValue, "smoke.unrelated.summon");
                unrelatedChannelProbe.Value(RunContentRandomChannel.Augment, "smoke.unrelated.augment");
                unrelatedChannelProbe.Value(RunContentRandomChannel.Board, "smoke.unrelated.board");
                unrelatedChannelProbe.Value(RunContentRandomChannel.Merge, "smoke.unrelated.merge");
                RunContentRandomService unrelatedControl = new RunContentRandomService();
                unrelatedControl.Reset(314159);
                int unrelatedControlSample = unrelatedControl.Range(RunContentRandomChannel.Summon, 0, int.MaxValue, "smoke.unrelated.summon");

                runContentChannelIsolationValid =
                    summonWithShopFirst == summonControlFirst &&
                    summonWithShopSecond == summonControlSecond &&
                    shopAfterSummonSample == shopControlSample &&
                    unrelatedSummonSample == unrelatedControlSample;
                controller.SetRunContentSeedOverride(null);
                controller.ResetRunForRetry();
            }
            if (!runResetStateValid)
            {
                notes.Add("재시작 시 런 중 최대 체력 증가가 기본값으로 복원되지 않습니다.");
            }
            if (!runSeedRepeatValid)
            {
                notes.Add("동일 콘텐츠 시드 재시작의 난수 시작점이 일치하지 않습니다.");
            }
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
                                        Approximately(fateEntryRect.sizeDelta, new Vector2(238f, 82f)) &&
                                        Approximately(fateEntryRect.anchoredPosition, new Vector2(-150f, 478f)) &&
                                        fateEntryShadow != null &&
                                        Approximately(fateEntryShadow.effectDistance, new Vector2(0f, -4f)) &&
                                        fateEntryShadow.useGraphicAlpha &&
                                        fateEntryOutline != null &&
                                        Approximately(fateEntryOutline.effectDistance, new Vector2(2f, -2f)) &&
                                        fateEntryOutline.useGraphicAlpha;
            bool fateEntryPastelColorValid = fateEntryGraphic != null &&
                                              Approximately(fateEntryGraphic.color, new Color(0.40f, 0.21f, 0.85f, 0.98f)) &&
                                              fateEntryText != null &&
                                              fateEntryOutline != null &&
                                              Approximately(fateEntryText.color, new Color(1.00f, 0.98f, 0.94f, 1f)) &&
                                              Approximately(fateEntryOutline.effectColor, new Color(1.00f, 0.78f, 0.34f, 0.94f));
            bool fateEntryIdleAtFullHealth = controller != null && controller.Life > 3 && !controller.FateSurvivalCrisisActive;
            if (!fateEntryLayoutValid || !fateEntryPastelColorValid || !fateEntryIdleAtFullHealth)
            {
                string actualBackground = fateEntryGraphic != null ? fateEntryGraphic.color.ToString() : "null";
                string actualText = fateEntryText != null ? fateEntryText.color.ToString() : "null";
                notes.Add("운명카드 버튼의 하단 HUD 정렬, 와인/골드 팔레트 또는 HP 3 초과 정지 상태가 유효하지 않습니다. " +
                          "background=" + actualBackground + ", text=" + actualText);
            }

            Button summonHudButton = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(button => button != null && button.name == "SummonButton");
            Text summonCostHudText = UnityEngine.Object.FindObjectsOfType<Text>(true)
                .FirstOrDefault(text => text != null && text.name == "SummonCostText");
            Text luckySummonHudText = UnityEngine.Object.FindObjectsOfType<Text>(true)
                .FirstOrDefault(text => text != null && text.name == "LuckySummonProgressText");
            Image luckySummonBadge = UnityEngine.Object.FindObjectsOfType<Image>(true)
                .FirstOrDefault(image => image != null && image.name == "LuckySummonProgressBadge");
            RectTransform summonHudRect = summonHudButton != null ? summonHudButton.GetComponent<RectTransform>() : null;
            Image summonHudImage = summonHudButton != null ? summonHudButton.targetGraphic as Image : null;
            bool summonHudReadable = summonHudRect != null &&
                                     Approximately(summonHudRect.sizeDelta, new Vector2(226f, 88f)) &&
                                     summonCostHudText != null &&
                                     summonCostHudText.text.EndsWith(" GOLD", StringComparison.Ordinal) &&
                                     summonCostHudText.fontSize >= 18 &&
                                     luckySummonHudText != null &&
                                     luckySummonHudText.fontSize >= 17 &&
                                     luckySummonBadge != null &&
                                     luckySummonHudText.transform.parent == luckySummonBadge.transform &&
                                     summonHudImage != null &&
                                     summonHudImage.sprite != null &&
                                     !summonHudImage.sprite.name.StartsWith("RuntimeRoundedPanel", StringComparison.Ordinal);
            if (!summonHudReadable)
            {
                notes.Add("소환 버튼의 행운 진행도 배지, GOLD 비용 표기 또는 Assets/Art/Ui 버튼 스프라이트 우선 적용이 유효하지 않습니다.");
            }
            RuntimeSceneBootstrap runtimeBootstrap = UnityEngine.Object.FindObjectOfType<RuntimeSceneBootstrap>();
            Transform runtimeStageRoot = runtimeBootstrap != null ? runtimeBootstrap.transform.Find("RuntimeStageRoot") : null;
            Transform runtimeCameraRoot = runtimeBootstrap != null ? runtimeBootstrap.transform.Find("RuntimeCombatCameraRoot") : null;
            int initialBuildCount = runtimeBootstrap != null ? runtimeBootstrap.RuntimeSceneBuildCount : -1;
            int initialCameraCount = UnityEngine.Object.FindObjectsOfType<Camera>(true).Length;
            bool stageHiddenInLobby = runtimeBootstrap != null &&
                                      runtimeStageRoot != null && !runtimeStageRoot.gameObject.activeInHierarchy &&
                                      runtimeCameraRoot != null && !runtimeCameraRoot.gameObject.activeInHierarchy &&
                                      runtimeStageRoot.Find("BoardSlots") != null && !runtimeStageRoot.Find("BoardSlots").gameObject.activeInHierarchy &&
                                      runtimeStageRoot.Find("SpawnPoints") != null && !runtimeStageRoot.Find("SpawnPoints").gameObject.activeInHierarchy &&
                                      runtimeStageRoot.Find("Templates") != null && !runtimeStageRoot.Find("Templates").gameObject.activeInHierarchy;

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
            bool stageVisibleInPreparation = runtimeBootstrap != null && runtimeBootstrap.IsGameplayStageVisible;
            bool initialPreparationBattleStartUiPathValid = ValidateInitialPreparationBattleStartUiPath(controller, out string initialPreparationBattleStartUiPathSummary);
            if (!initialPreparationBattleStartUiPathValid)
            {
                notes.Add("Pass 2K initial preparation UI battle-start validation failed. " + initialPreparationBattleStartUiPathSummary);
            }
            bool pass2BPreparationSkipValid = ValidatePass2BPreparationSkip(controller, out string pass2BPreparationSkipSummary);
            if (!pass2BPreparationSkipValid)
            {
                notes.Add("Pass 2B preparation skip validation failed. " + pass2BPreparationSkipSummary);
            }
            int directPlayerSummonEvents = 0;
            Action<CharacterDefinition> directPlayerSummonHandler = definition => directPlayerSummonEvents++;
            if (controller != null)
            {
                controller.OnPlayerSummoned += directPlayerSummonHandler;
            }
            bool forecastAvailabilityBeforePlayerSummon = controller != null && controller.CanChooseBossForecastBet;
            int forecastShopRoleBeforePlayerSummon = controller != null ? controller.BossForecastPreferredShopRoleIndex : -1;
            bool overdriveSelectedForPlayerSummonTest = controller != null && controller.TrySetCombatMode(CombatGameMode.Overdrive);
            bool paidPlayerSummonGranted = controller != null && controller.TrySummon();
            bool screenSpaceCombatHudValid = ValidateScreenSpaceCombatHud();
            bool dragTransactionSafetyValid = ValidateDragTransactionSafety();
            bool dragCombatSuspensionValid = ValidateDraggedUnitCombatSuspension();
            bool boardCapacityPacingValid = ValidateBoardCapacityPacing(out string boardCapacityPacingSummary);
            bool pass1DGradeRulesValid = ValidatePass1DGradeUpgradeRules(controller);
            bool pass1DPressureValid = ValidatePass1DPressureRules();
            bool pass2OCombatHudLayoutValid = ValidatePass2OCombatHudLayout(controller);
            bool gradeUpgradeBarUiValid = pass2OCombatHudLayoutValid;
            bool pass2NCombatEconomyUiValid = pass2OCombatHudLayoutValid;
            bool pass2MSummonGradeLuckValid = ValidatePass2MSummonGradeLuck(controller);
            bool pass1EMilestoneValid = ValidatePass1EMilestoneRules(controller, out string pass1EMilestoneSummary);
            if (!pass1DGradeRulesValid || !pass1DPressureValid || !gradeUpgradeBarUiValid || !pass1EMilestoneValid)
            {
                notes.Add("Pass 1D grade upgrade, classic pressure, or Pass 1E milestone validation failed. " + pass1EMilestoneSummary);
            }
            if (!pass2MSummonGradeLuckValid)
            {
                notes.Add("Pass 2M summon grade luck validation failed.");
            }
            if (!pass2NCombatEconomyUiValid)
            {
                notes.Add("Pass 2N/2O combat economy HUD layout, combat purchase, or Info tooltip validation failed.");
            }
            if (!screenSpaceCombatHudValid || !dragTransactionSafetyValid || !dragCombatSuspensionValid || !boardCapacityPacingValid)
            {
                notes.Add("Screen-space combat HUD, anchor priority, drag transaction, drag combat suspension, or board capacity pacing validation failed. " + boardCapacityPacingSummary);
            }
            if (controller != null)
            {
                controller.OnPlayerSummoned -= directPlayerSummonHandler;
            }
            bool playerDirectSummonIsolationValid = overdriveSelectedForPlayerSummonTest &&
                                                    paidPlayerSummonGranted &&
                                                    directPlayerSummonEvents == 1 &&
                                                    controller != null &&
                                                    controller.CanChooseBossForecastBet == forecastAvailabilityBeforePlayerSummon &&
                                                    controller.BossForecastPreferredShopRoleIndex == forecastShopRoleBeforePlayerSummon;
            if (!playerDirectSummonIsolationValid)
            {
                notes.Add("Player direct summon event or Boss Forecast state changed unexpectedly.");
            }

            bool firstBossPreparationRewardRulesValid = controller != null &&
                                                        controller.FirstBossPreparationRewardMinSummons == 14 &&
                                                        controller.FirstBossPreparationRewardMinMerges == 4 &&
                                                        Mathf.Approximately(controller.FirstBossPreparationRewardAttackBonus, 0.06f) &&
                                                        Mathf.Approximately(controller.FirstBossPreparationRewardBossDamageBonus, 0.10f) &&
                                                        Mathf.Approximately(controller.FirstBossPreparationRewardMaxBossDamageBonus, 0.18f) &&
                                                        !DefenseGameController.IsFirstBossPreparationRewardConditionMet(13, 3, 14, 4) &&
                                                        DefenseGameController.IsFirstBossPreparationRewardConditionMet(14, 0, 14, 4) &&
                                                        DefenseGameController.IsFirstBossPreparationRewardConditionMet(0, 4, 14, 4);
            if (!firstBossPreparationRewardRulesValid)
            {
                notes.Add("First boss preparation reward thresholds, OR condition, or bonus values are invalid.");
            }

            bool bossForecastTimingValid = ValidateBossForecastTimingAndShopBias(controller, out string bossForecastTimingSummary);
            if (!bossForecastTimingValid)
            {
                notes.Add("Boss Forecast R4 preparation timing or one-shot shop bias validation failed. " + bossForecastTimingSummary);
            }
            controller?.TrySetCombatMode(CombatGameMode.Classic);

            TacticalMissionSystem tacticalMissionSystem = UnityEngine.Object.FindObjectOfType<TacticalMissionSystem>();
            bool tacticalMissionRiskRewardValid = tacticalMissionSystem != null &&
                                                  tacticalMissionSystem.HasInitialStrategyFork &&
                                                  TacticalMissionSystem.IsLastStandGambitConditionMet(7, 2, 2, 3) &&
                                                  !TacticalMissionSystem.IsLastStandGambitConditionMet(0, 0, 0, 1) &&
                                                  !TacticalMissionSystem.IsLastStandGambitConditionMet(7, 3, 2, 2) &&
                                                  !TacticalMissionSystem.IsLastStandGambitConditionMet(7, 2, 2, 4);
            if (!tacticalMissionRiskRewardValid)
            {
                notes.Add("전술 미션 초기 전략 분기 또는 배수의 진 조건(HP 7 포함/HP 0·3회 소환 실패) 검증에 실패했습니다.");
            }

            bool tacticalMissionChoiceValid = tacticalMissionSystem != null &&
                                               tacticalMissionSystem.MissionOfferCount == 3 &&
                                               !tacticalMissionSystem.HasActiveMissionSelection &&
                                               tacticalMissionSystem.TrySelectMission(1) &&
                                               tacticalMissionSystem.HasActiveMissionSelection &&
                                               tacticalMissionSystem.MissionOfferCount == 0 &&
                                               !tacticalMissionSystem.TrySelectMission(0);
            if (!tacticalMissionChoiceValid)
            {
                notes.Add("전술 미션은 3개 제안 중 하나만 선택해 추적해야 합니다.");
            }

            OutgameProgressionSystem smokeProgression = OutgameProgressionSystem.Active;
            bool roundDiamondRewardValid = smokeProgression != null &&
                                            smokeProgression.ResolveRoundClearDiamondReward(1) == 2 &&
                                            smokeProgression.ResolveRoundClearDiamondReward(10) == 2 &&
                                            smokeProgression.ResolveRoundClearDiamondReward(11) == 3 &&
                                            smokeProgression.ResolveRoundClearDiamondReward(20) == 3 &&
                                            smokeProgression.ResolveRoundClearDiamondReward(21) == 4 &&
                                            smokeProgression.ResolveRoundClearDiamondReward(61) == 8;
            if (!roundDiamondRewardValid)
            {
                notes.Add("라운드 클리어 다이아 보상 구간 검증에 실패했습니다.");
            }
            SimpleGameHUD simpleHud = UnityEngine.Object.FindObjectOfType<SimpleGameHUD>();
            bool bannerBurstQueueValid = false;
            bool stalePostRoundBannerQueued = false;
            if (simpleHud != null && controller != null)
            {
                ResetRoundBannerForSmoke(simpleHud);
                controller.RequestBanner("SMOKE_REWARD_A", Color.yellow, 1f);
                controller.RequestBanner("SMOKE_REWARD_B", Color.cyan, 1f);
                controller.RequestBanner("SMOKE_REWARD_C", Color.magenta, 1f);
                bool initialOrder = simpleHud.CurrentRoundBannerMessage == "SMOKE_REWARD_A" && simpleHud.PendingPostRoundBannerCount == 2;
                AdvanceRoundBannerForSmoke(simpleHud);
                bool secondOrder = simpleHud.CurrentRoundBannerMessage == "SMOKE_REWARD_B" && simpleHud.PendingPostRoundBannerCount == 1;
                AdvanceRoundBannerForSmoke(simpleHud);
                bool thirdOrder = simpleHud.CurrentRoundBannerMessage == "SMOKE_REWARD_C" && simpleHud.PendingPostRoundBannerCount == 0;
                bannerBurstQueueValid = initialOrder && secondOrder && thirdOrder && simpleHud.PendingPostRoundBannerCount <= 4;

                ResetRoundBannerForSmoke(simpleHud);
                controller.RequestBanner("SMOKE_QUEUE_0", Color.white, 1f);
                controller.RequestBanner("SMOKE_QUEUE_0", Color.white, 1f);
                controller.RequestBanner("SMOKE_QUEUE_1", Color.white, 1f);
                controller.RequestBanner("SMOKE_QUEUE_2", Color.white, 1f);
                controller.RequestBanner("SMOKE_QUEUE_3", Color.white, 1f);
                controller.RequestBanner("SMOKE_QUEUE_4", Color.white, 1f);
                controller.RequestBanner("SMOKE_QUEUE_5", Color.white, 1f);
                bool boundedAndDeduplicated = simpleHud.CurrentRoundBannerMessage == "SMOKE_QUEUE_0" &&
                                              simpleHud.PendingPostRoundBannerCount == 4;
                bannerBurstQueueValid &= boundedAndDeduplicated;

                ResetRoundBannerForSmoke(simpleHud);
                controller.RequestBanner("SMOKE_STALE_A", Color.white, 1f);
                controller.RequestBanner("SMOKE_STALE_B", Color.white, 1f);
                stalePostRoundBannerQueued = simpleHud.PendingPostRoundBannerCount == 1;
            }
            if (!bannerBurstQueueValid)
            {
                notes.Add("전투 종료 후 보상 배너 3개가 순서대로 표시되지 않거나 bounded queue 검증에 실패했습니다.");
            }
            Button returnToLobbyButton = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(button => button != null && button.name == "OutgameNavLobby");
            returnToLobbyButton?.onClick.Invoke();
            bool stageHiddenAfterReturn = runtimeBootstrap != null &&
                                          !runtimeBootstrap.IsGameplayStageVisible &&
                                          runtimeStageRoot != null && !runtimeStageRoot.gameObject.activeInHierarchy &&
                                          runtimeCameraRoot != null && !runtimeCameraRoot.gameObject.activeInHierarchy;
            bool runtimeStageLifecycleValid = stageHiddenInLobby &&
                                              stageVisibleInPreparation &&
                                              stageHiddenAfterReturn &&
                                              runtimeBootstrap != null && runtimeBootstrap.RuntimeSceneBuildCount == initialBuildCount &&
                                              UnityEngine.Object.FindObjectsOfType<Camera>(true).Length == initialCameraCount;
            if (!runtimeStageLifecycleValid)
            {
                notes.Add("Outgame stage gate failed: lobbyHidden=" + stageHiddenInLobby + ", preparationVisible=" + stageVisibleInPreparation + ", returnHidden=" + stageHiddenAfterReturn + ", build=" + (runtimeBootstrap != null ? runtimeBootstrap.RuntimeSceneBuildCount : -1) + "/" + initialBuildCount + ", cameras=" + UnityEngine.Object.FindObjectsOfType<Camera>(true).Length + "/" + initialCameraCount);
            }
            Button inventoryButton = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(button => button != null && button.name == "OutgameNavInventory");
            bool inventoryStageHidden = inventoryButton != null;
            if (inventoryStageHidden)
            {
                inventoryButton.onClick.Invoke();
                inventoryStageHidden = runtimeBootstrap != null && !runtimeBootstrap.IsGameplayStageVisible;
                returnToLobbyButton?.onClick.Invoke();
            }
            if (!inventoryStageHidden)
            {
                notes.Add("Inventory did not keep the combat stage disabled.");
            }

            Button dailyFateCupButton = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(button => button != null && button.name == "LobbyDailyFateCupButton");
            Text dailyFateCupText = UnityEngine.Object.FindObjectsOfType<Text>(true)
                .FirstOrDefault(textComponent => textComponent != null && textComponent.name == "LobbyDailyFateCupText");
            bool dailyFateCupUiValid = controller != null &&
                                           dailyFateCupButton != null &&
                                           dailyFateCupText != null &&
                                           dailyFateCupText.text.Contains("데일리") &&
                                           DailyFateCupRules.TodaySeed != 0 &&
                                           !string.IsNullOrWhiteSpace(controller.LuckProtectionLedgerSummary);
            if (!dailyFateCupUiValid)
            {
                notes.Add("데일리 운명컵 로비 버튼, 동일 시드 또는 운 보호 장부 UI가 유효하지 않습니다.");
            }

            RectTransform bossForecastOverlay = UnityEngine.Object.FindObjectsOfType<RectTransform>(true)
                .FirstOrDefault(rect => rect != null && rect.name == "BossForecastBetOverlay");
            int bossForecastChoices = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .Count(button => button != null && button.name.StartsWith("BossForecastChoice_", StringComparison.Ordinal));
            bool bossForecastUiValid = controller != null &&
                                       bossForecastOverlay != null &&
                                       bossForecastChoices == 3;
            if (!bossForecastUiValid)
            {
                notes.Add("R10 보스 예고 베팅 팝업 또는 3개 전략 선택지가 유효하지 않습니다.");
            }

            Button lobbyShopButton = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(button => button != null && button.name == "OutgameNavShop");
            bool outgameShopValid = lobbyShopButton != null;
            if (outgameShopValid)
            {
                lobbyShopButton.onClick.Invoke();
                RectTransform shopModal = UnityEngine.Object.FindObjectsOfType<RectTransform>(true)
                    .FirstOrDefault(rect => rect != null && rect.name == "ShopModal");
                int dailyCards = UnityEngine.Object.FindObjectsOfType<Button>(true)
                    .Count(button => button != null && button.name.StartsWith("DailyOfferCard_", StringComparison.Ordinal));
                int cashCards = UnityEngine.Object.FindObjectsOfType<Button>(true)
                    .Count(button => button != null && button.name.StartsWith("CashBundleCard_", StringComparison.Ordinal));
                string[] chestButtonNames = { "FiveDrawCard", "TwentyDrawCard", "FiftyDrawCard", "HundredDrawCard" };
                int chestCards = chestButtonNames.Count(name => UnityEngine.Object.FindObjectsOfType<Button>(true)
                    .Any(button => button != null && button.name == name));
                Text shopGold = UnityEngine.Object.FindObjectsOfType<Text>(true)
                    .FirstOrDefault(text => text != null && text.name == "ShopGoldText");
                int productIcons = UnityEngine.Object.FindObjectsOfType<Image>(true)
                    .Count(image => image != null && image.name == "ShopProductIcon" && image.sprite != null);
                string[] sectionIconNames = { "CashSectionIcon", "DailySectionIcon", "ChestSectionIcon", "HeaderGoldIcon", "HeaderDiamondIcon" };
                int sectionIcons = sectionIconNames.Count(name => UnityEngine.Object.FindObjectsOfType<Image>(true)
                    .Any(image => image != null && image.name == name && image.sprite != null));
                bool decorativeShopArtValid = productIcons == 10 && sectionIcons == sectionIconNames.Length;
                Button firstCashProduct = UnityEngine.Object.FindObjectsOfType<Button>(true)
                    .FirstOrDefault(button => button != null && button.name == "CashBundleCard_0");
                GameObject purchaseConfirmOverlay = UnityEngine.Object.FindObjectsOfType<RectTransform>(true)
                    .Where(rect => rect != null && rect.name == "ShopPurchaseConfirmOverlay")
                    .Select(rect => rect.gameObject)
                    .FirstOrDefault();
                Button purchaseCancelButton = UnityEngine.Object.FindObjectsOfType<Button>(true)
                    .FirstOrDefault(button => button != null && button.name == "ShopPurchaseConfirmCancelButton");
                bool purchaseConfirmationValid = firstCashProduct != null &&
                                                 purchaseConfirmOverlay != null &&
                                                 !purchaseConfirmOverlay.activeSelf;
                if (purchaseConfirmationValid)
                {
                    firstCashProduct.onClick.Invoke();
                    purchaseConfirmationValid = purchaseConfirmOverlay.activeSelf && purchaseCancelButton != null;
                    purchaseCancelButton?.onClick.Invoke();
                    purchaseConfirmationValid &= !purchaseConfirmOverlay.activeSelf;
                }
                outgameShopValid = shopModal != null &&
                                   Approximately(shopModal.anchorMin, Vector2.zero) &&
                                   Approximately(shopModal.anchorMax, Vector2.one) &&
                                   dailyCards == 3 &&
                                   cashCards == 3 &&
                                   chestCards == 4 &&
                                   shopGold != null &&
                                   shopGold.text.Contains("GOLD") &&
                                   decorativeShopArtValid &&
                                   purchaseConfirmationValid && runtimeBootstrap != null && !runtimeBootstrap.IsGameplayStageVisible;
                if (!outgameShopValid)
                {
                    notes.Add("SHOP DETAIL modal=" + (shopModal != null) + ", size=" + (shopModal != null ? shopModal.sizeDelta.ToString() : "null") + ", daily=" + dailyCards + ", cash=" + cashCards + ", chest=" + chestCards + ", gold=" + (shopGold != null ? shopGold.text : "null") + ", productIcons=" + productIcons + ", sectionIcons=" + sectionIcons + ", confirm=" + purchaseConfirmationValid);
                }
                Button shopClose = UnityEngine.Object.FindObjectsOfType<Button>(true)
                    .FirstOrDefault(button => button != null && button.name == "ShopCloseButton");
                shopClose?.onClick.Invoke();
            }
            if (!outgameShopValid)
            {
                notes.Add("로비 상점의 현금 꾸러미 3개, 일일 상품 3개, 상자 5/20/50/100개 또는 GOLD/DIA 표시가 유효하지 않습니다.");
            }

            Image resultGoldIcon = UnityEngine.Object.FindObjectsOfType<Image>(true)
                .FirstOrDefault(image => image != null && image.name == "ResultRewardGoldIcon");
            Image resultDiamondIcon = UnityEngine.Object.FindObjectsOfType<Image>(true)
                .FirstOrDefault(image => image != null && image.name == "ResultRewardDiamondIcon");
            bool resultRewardIconsValid = resultGoldIcon != null &&
                                          resultGoldIcon.sprite != null &&
                                          resultDiamondIcon != null &&
                                          resultDiamondIcon.sprite != null;
            if (!resultRewardIconsValid)
            {
                notes.Add("승리 결과 보상 칩에 골드 또는 다이아 아이콘이 연결되지 않았습니다.");
            }

            Button rankingButton = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(button => button != null && button.name == "OutgameNavRanking");
            bool rankingPageValid = rankingButton != null;
            if (rankingPageValid)
            {
                rankingButton.onClick.Invoke();
                RectTransform rankingOverlay = UnityEngine.Object.FindObjectsOfType<RectTransform>(true)
                    .FirstOrDefault(rect => rect != null && rect.name == "SeasonRankingOverlay");
                RectTransform rankingModal = UnityEngine.Object.FindObjectsOfType<RectTransform>(true)
                    .FirstOrDefault(rect => rect != null && rect.name == "SeasonRankingModal");
                int topCards = UnityEngine.Object.FindObjectsOfType<Image>(true)
                    .Count(image => image != null && image.name.StartsWith("RankingTopCard_", StringComparison.Ordinal));
                int rankingRows = UnityEngine.Object.FindObjectsOfType<Image>(true)
                    .Count(image => image != null && image.name.StartsWith("RankingRow_", StringComparison.Ordinal));
                Text rankingPlayerSummary = UnityEngine.Object.FindObjectsOfType<Text>(true)
                    .FirstOrDefault(textComponent => textComponent != null && textComponent.name == "RankingPlayerSummary");
                rankingPageValid = rankingOverlay != null &&
                                   rankingOverlay.gameObject.activeSelf &&
                                   rankingModal != null &&
                                   Approximately(rankingModal.anchorMin, Vector2.zero) &&
                                   Approximately(rankingModal.anchorMax, Vector2.one) &&
                                   topCards == 3 &&
                                   rankingRows == 9 &&
                                   rankingPlayerSummary != null &&
                                   rankingPlayerSummary.text.Contains("내 순위") && runtimeBootstrap != null && !runtimeBootstrap.IsGameplayStageVisible;
                if (!rankingPageValid)
                {
                    notes.Add("RANK DETAIL overlay=" + (rankingOverlay != null && rankingOverlay.gameObject.activeSelf) + ", modal=" + (rankingModal != null) + ", size=" + (rankingModal != null ? rankingModal.sizeDelta.ToString() : "null") + ", top=" + topCards + ", rows=" + rankingRows + ", player=" + (rankingPlayerSummary != null ? rankingPlayerSummary.text : "null"));
                }
                Button rankingClose = UnityEngine.Object.FindObjectsOfType<Button>(true)
                    .FirstOrDefault(button => button != null && button.name == "RankingCloseButton");
                rankingClose?.onClick.Invoke();
            }
            if (!rankingPageValid)
            {
                notes.Add("시즌 랭킹의 상위 3명 포디움, 4~12위 리스트, 내 순위 강조 또는 전용 아트가 유효하지 않습니다.");
            }

            Button yahtzeeButton = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(button => button != null && button.name == "OutgameNavYahtzee");
            bool yahtzeeModeUiValid = yahtzeeButton != null;
            if (yahtzeeModeUiValid)
            {
                yahtzeeButton.onClick.Invoke();
                RectTransform yahtzeeOverlay = UnityEngine.Object.FindObjectsOfType<RectTransform>(true)
                    .FirstOrDefault(rect => rect != null && rect.name == "YahtzeeOverlay");
                RectTransform yahtzeeModal = UnityEngine.Object.FindObjectsOfType<RectTransform>(true)
                    .FirstOrDefault(rect => rect != null && rect.name == "YahtzeeModal");
                int diceCount = UnityEngine.Object.FindObjectsOfType<Button>(true)
                    .Count(button => button != null && button.name.StartsWith("YahtzeeDie_", StringComparison.Ordinal));
                int chestOpenButtons = UnityEngine.Object.FindObjectsOfType<Button>(true)
                    .Count(button => button != null && (button.name == "YahtzeeOpenOneButton" || button.name == "YahtzeeOpenTenButton" || button.name == "YahtzeeOpenAllButton"));
                Button yahtzeeHold = UnityEngine.Object.FindObjectsOfType<Button>(true)
                    .FirstOrDefault(button => button != null && button.name == "YahtzeeHoldButton");
                Button yahtzeeReroll = UnityEngine.Object.FindObjectsOfType<Button>(true)
                    .FirstOrDefault(button => button != null && button.name == "YahtzeeRerollButton");
                Button yahtzeeConfirm = UnityEngine.Object.FindObjectsOfType<Button>(true)
                    .FirstOrDefault(button => button != null && button.name == "YahtzeeConfirmButton");
                bool yahtzeeAdvanceRemoved = !UnityEngine.Object.FindObjectsOfType<Button>(true)
                    .Any(button => button != null && button.name == "YahtzeeAdvanceButton");
                int diceFaceCount = UnityEngine.Object.FindObjectsOfType<Image>(true)
                    .Count(image => image != null && image.name == "Face" && image.transform.parent != null && image.transform.parent.name.StartsWith("YahtzeeDie_", StringComparison.Ordinal) && image.sprite != null);
                Text yahtzeeStatus = UnityEngine.Object.FindObjectsOfType<Text>(true)
                    .FirstOrDefault(textComponent => textComponent != null && textComponent.name == "YahtzeeStatusText");
                YahtzeeProgressionSystem yahtzeeProgression = UnityEngine.Object.FindObjectOfType<YahtzeeProgressionSystem>();
                yahtzeeModeUiValid = yahtzeeOverlay != null &&
                                     yahtzeeOverlay.gameObject.activeSelf &&
                                     yahtzeeModal != null &&
                                     Approximately(yahtzeeModal.anchorMin, Vector2.zero) &&
                                     Approximately(yahtzeeModal.anchorMax, Vector2.one) &&
                                     diceCount == 3 &&
                                     diceFaceCount == 3 &&
                                     chestOpenButtons == 3 &&
                                     yahtzeeHold != null &&
                                     yahtzeeReroll != null &&
                                     yahtzeeConfirm != null &&
                                     yahtzeeAdvanceRemoved &&
                                     yahtzeeStatus != null &&
                                     yahtzeeProgression != null && runtimeBootstrap != null && !runtimeBootstrap.IsGameplayStageVisible;
                if (!yahtzeeModeUiValid)
                {
                    notes.Add("YATZY DETAIL overlay=" + (yahtzeeOverlay != null && yahtzeeOverlay.gameObject.activeSelf) + ", modal=" + (yahtzeeModal != null) + ", dice=" + diceCount + ", faces=" + diceFaceCount + ", chest=" + chestOpenButtons + ", hold=" + (yahtzeeHold != null) + ", reroll=" + (yahtzeeReroll != null) + ", confirm=" + (yahtzeeConfirm != null) + ", advanceRemoved=" + yahtzeeAdvanceRemoved + ", status=" + (yahtzeeStatus != null) + ", system=" + (yahtzeeProgression != null));
                }
            }
            if (!yahtzeeModeUiValid)
            {
                notes.Add("얏찌 전체화면, 3개 주사위, 상자 1/10/전체 개봉 또는 진행 저장 시스템이 유효하지 않습니다.");
            }

            bool yahtzeeMultiplierLogicValid = YahtzeeProgressionSystem.ResolveMultiplier(1, 1, 1) == 1 &&
                                               YahtzeeProgressionSystem.ResolveMultiplier(2, 2, 2) == 2 &&
                                               YahtzeeProgressionSystem.ResolveMultiplier(3, 3, 3) == 3 &&
                                               YahtzeeProgressionSystem.ResolveMultiplier(4, 4, 4) == 4 &&
                                               YahtzeeProgressionSystem.ResolveMultiplier(5, 5, 5) == 5 &&
                                               YahtzeeProgressionSystem.ResolveMultiplier(6, 6, 6) == 6 &&
                                               YahtzeeProgressionSystem.ResolveMultiplier(6, 6, 5) == 1 &&
                                               YahtzeeProgressionSystem.AreAllDiceHeld(true, true, true) &&
                                               !YahtzeeProgressionSystem.AreAllDiceHeld(true, true, false);
            if (!yahtzeeMultiplierLogicValid)
            {
                notes.Add("얏찌 트리플 x1~x6 배수 계산이 올바르지 않습니다.");
            }

            bool yahtzeeTicketMilestoneLogicValid =
                DefenseGameController.IsYahtzeeTicketMilestoneRound(10, true) &&
                DefenseGameController.IsYahtzeeTicketMilestoneRound(20, true) &&
                DefenseGameController.IsYahtzeeTicketMilestoneRound(30, true) &&
                !DefenseGameController.IsYahtzeeTicketMilestoneRound(10, false) &&
                !DefenseGameController.IsYahtzeeTicketMilestoneRound(9, true);
            if (!yahtzeeTicketMilestoneLogicValid)
            {
                notes.Add("Yahtzee ticket boss milestone validation failed.");
            }

            HashSet<int> ticketMilestones = new HashSet<int>();
            int runYahtzeeTicketsEarned = 0;
            if (DefenseGameController.TryRegisterYahtzeeTicketMilestone(ticketMilestones, 10, true)) runYahtzeeTicketsEarned++;
            bool yahtzeeTicketDuplicateBlocked = !DefenseGameController.TryRegisterYahtzeeTicketMilestone(ticketMilestones, 10, true);
            bool yahtzeeTicketRegularRoundBlocked = !DefenseGameController.TryRegisterYahtzeeTicketMilestone(ticketMilestones, 11, false);
            if (DefenseGameController.TryRegisterYahtzeeTicketMilestone(ticketMilestones, 20, true)) runYahtzeeTicketsEarned++;
            if (DefenseGameController.TryRegisterYahtzeeTicketMilestone(ticketMilestones, 30, true)) runYahtzeeTicketsEarned++;
            bool yahtzeeTicketRunAccumulationValid = runYahtzeeTicketsEarned == 3 &&
                                                     ticketMilestones.Count == 3 &&
                                                     yahtzeeTicketDuplicateBlocked &&
                                                     yahtzeeTicketRegularRoundBlocked;
            ticketMilestones.Clear();
            bool yahtzeeTicketNewRunResetValid = DefenseGameController.TryRegisterYahtzeeTicketMilestone(ticketMilestones, 10, true);
            if (!yahtzeeTicketRunAccumulationValid || !yahtzeeTicketNewRunResetValid)
            {
                notes.Add("Yahtzee ticket duplicate, run accumulation, or new-run reset validation failed.");
            }

            bool earlyMiniShopChoicesValid = ValidateRoundTieredMiniShop(out string earlyMiniShopSummary);
            bool choiceScheduleValid = ValidateChoiceSchedule(out string choiceScheduleSummary);
            bool recipePacingTelemetryValid = ValidateRecipePacingTelemetry(controller, out string recipePacingTelemetrySummary);
            if (!earlyMiniShopChoicesValid)
            {
                notes.Add("R11 소형 전투상점의 3개 선택지 분류/가격 검증에 실패했습니다. " + earlyMiniShopSummary);
            }
            if (!choiceScheduleValid)
            {
                notes.Add("Choice schedule validation failed. " + choiceScheduleSummary);
            }
            if (!recipePacingTelemetryValid)
            {
                notes.Add("Recipe pacing telemetry validation failed. " + recipePacingTelemetrySummary);
            }

            bool ultimateRecipeUxValid = ValidateUltimateRecipeUx(controller, out string ultimateRecipeUxSummary);
            if (!ultimateRecipeUxValid)
            {
                notes.Add("Ultimate recipe UX data/layout validation failed. " + ultimateRecipeUxSummary);
            }

            bool ultimateMergeInheritanceIsolationValid = ValidateUltimateMergeInheritanceSeparation(out string ultimateMergeInheritanceSummary);
            if (!ultimateMergeInheritanceIsolationValid)
            {
                notes.Add("Ultimate and normal merge inheritance isolation validation failed. " + ultimateMergeInheritanceSummary);
            }

            GamePresentationConfig presentation = AssetDatabase.LoadAssetAtPath<GamePresentationConfig>("Assets/Data/DefenseGamePresentationConfig.asset");
            bool defaultVfxConfigured = presentation != null &&
                                        presentation.projectilePrefab != null &&
                                        presentation.defaultMuzzleEffectPrefab != null &&
                                        presentation.defaultHitEffectPrefab != null &&
                                        presentation.defaultAreaEffectPrefab != null &&
                                        presentation.diceAutoDormantEffectPrefab != null;
            if (!defaultVfxConfigured)
            {
                notes.Add("DefenseGamePresentationConfig 기본 투사체/머즐/히트/범위 VFX 중 빈 참조가 있습니다.");
            }
            bool animationMaterialEventsValid = ValidateAnimationMaterialEvents(out string animationMaterialEventsSummary);
            if (!animationMaterialEventsValid)
            {
                notes.Add("OverrideMaterial/ResetMaterial 애니메이션 이벤트의 적용·원본 복구 검증에 실패했습니다. " + animationMaterialEventsSummary);
            }

            CharacterDatabase hero56Database = UnityEngine.Object.FindObjectOfType<CharacterDatabase>();
            CharacterDefinition hero56 = hero56Database != null ? hero56Database.GetCharacterById("hero_56") : null;
            SkillDefinition hero56Skill = hero56 != null && hero56.skills != null && hero56.skills.Count > 0 ? hero56.skills[0] : null;
            bool diceAutoCycleValid = hero56Skill != null &&
                                      hero56Skill.effectType == SkillEffectType.AreaDamage &&
                                      Mathf.Approximately(hero56Skill.power, 4.2f) &&
                                      Mathf.Approximately(hero56Skill.radius, 4.5f) &&
                                      Mathf.Approximately(hero56Skill.cooldown, 0f) &&
                                      Mathf.Approximately(hero56.stats.maxMana, 100f) &&
                                      Mathf.Approximately(hero56.stats.manaRegenPerSecondRate, 0f) &&
                                      Mathf.Approximately(hero56.stats.manaGainWhenHitRate, 0f) &&
                                      Mathf.Approximately(hero56.stats.manaGainPerAttackRate, 0.15f) &&
                                      Mathf.Approximately(DefenderUnit.ResolveAlternatingRoundStartMana(100f, true), 50f) &&
                                      Mathf.Approximately(DefenderUnit.ResolveAlternatingRoundStartMana(100f, false), 0f);
            if (!diceAutoCycleValid)
            {
                notes.Add("Dice Auto rest/active mana cycle validation failed.");
            }
            CharacterDefinition hero51 = hero56Database != null ? hero56Database.GetCharacterById("hero_51") : null;
            bool thunderControlDamageConversionValid = hero51 != null &&
                                                       hero51.stats != null &&
                                                       hero51.grade == CharacterGrade.Transcendent &&
                                                       Mathf.Approximately(hero51.stats.attackPower, 48f);
            if (!thunderControlDamageConversionValid)
            {
                notes.Add("Thunder Control runtime Attack Power validation failed.");
            }

            CharacterDefinition hero53 = hero56Database != null ? hero56Database.GetCharacterById("hero_53") : null;
            SkillDefinition hero53Skill = hero53 != null && hero53.skills != null && hero53.skills.Count > 0 ? hero53.skills[0] : null;
            bool feverEngineApexDpsValid = hero53Skill != null &&
                                           hero53.grade == CharacterGrade.Transcendent &&
                                           Mathf.Approximately(hero53.stats.maxHealth, 380f) &&
                                           Mathf.Approximately(hero53.stats.attackPower, 52f) &&
                                           Mathf.Approximately(hero53.stats.criticalChance, 0.25f) &&
                                           Mathf.Approximately(hero53.stats.criticalDamageMultiplier, 2.1f) &&
                                           Mathf.Approximately(hero53.stats.attackSpeed, 1.7f) &&
                                           Mathf.Approximately(hero53.stats.maxMana, 190f) &&
                                           Mathf.Approximately(hero53.stats.manaRegenPerSecondRate, 0.05f) &&
                                           Mathf.Approximately(hero53.stats.manaGainWhenHitRate, 0.10f) &&
                                           Mathf.Approximately(hero53.stats.manaGainPerAttackRate, 0.17f) &&
                                           hero53Skill.effectType == SkillEffectType.AttackSpeedBoost &&
                                           Mathf.Approximately(hero53Skill.power, 1f) &&
                                           Mathf.Approximately(hero53Skill.duration, 8f) &&
                                           Mathf.Approximately(hero53Skill.cooldown, 11f) &&
                                           Mathf.Approximately(hero53Skill.manaThreshold, 100f);
            if (!feverEngineApexDpsValid)
            {
                notes.Add("Fever Engine apex sustained DPS validation failed.");
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

            CharacterDefinition hero54 = database != null ? database.GetCharacterById("hero_54") : null;
            SkillDefinition hero54Skill = hero54 != null && hero54.skills != null && hero54.skills.Count > 0 ? hero54.skills[0] : null;
            bool gargoyleLoopDurationValid = hero54Skill != null &&
                                             hero54Skill.effectType == SkillEffectType.Taunt &&
                                             Mathf.Approximately(hero54Skill.duration, 5f) &&
                                             (hero54Skill.growthTargets & SkillGrowthTarget.Duration) != 0 &&
                                             Mathf.Approximately(UnitAnimationDriver.ResolveSkill03LoopHoldDuration(hero54Skill.duration, 0.35f), 5f) &&
                                             Mathf.Approximately(UnitAnimationDriver.ResolveSkill03LoopHoldDuration(6.5f, 0.35f), 6.5f);
            if (!gargoyleLoopDurationValid)
            {
                notes.Add("Dice Gargoyle의 5초 Skill03_Loop 또는 아웃게임 지속시간 성장 연결이 유효하지 않습니다.");
            }

            bool longCombatAccelerationValid =
                Mathf.Approximately(RoundManager.ResolveCombatAccelerationStartSeconds(false), 30f) &&
                Mathf.Approximately(RoundManager.ResolveCombatAccelerationStartSeconds(true), 40f) &&
                RoundManager.ResolveCombatTimeScaleMultiplier(29.99f) == 1 &&
                RoundManager.ResolveCombatTimeScaleMultiplier(30f) == 2 &&
                RoundManager.ResolveCombatTimeScaleMultiplier(39.99f, 40f) == 1 &&
                RoundManager.ResolveCombatTimeScaleMultiplier(40f, 40f) == 2 &&
                RoundManager.ResolveCombatTimeScaleMultiplier(35f) == 3 &&
                RoundManager.ResolveCombatTimeScaleMultiplier(45f) == 5 &&
                RoundManager.ResolveCombatTimeScaleMultiplier(90f) == 10;
            if (!longCombatAccelerationValid)
            {
                notes.Add("장기전 가속 단계가 30초 2배, 이후 5초마다 1배 증가 규칙과 일치하지 않습니다.");
            }

            bool retryTimeScaleResetValid = ValidateDefeatRetryTimeScaleReset(controller, out string retryTimeScaleResetSummary);
            if (!retryTimeScaleResetValid)
            {
                notes.Add("패배 후 재시도 Time.timeScale/fixedDeltaTime 복구 검증에 실패했습니다. " + retryTimeScaleResetSummary);
            }

            bool choiceReadabilityValid = ValidateChoiceReadability(out string choiceReadabilitySummary);
            if (!choiceReadabilityValid)
            {
                notes.Add("미션/보스 대비/행운 소환 선택지의 가독성 또는 세로 안전영역 검증에 실패했습니다. " + choiceReadabilitySummary);
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

            bool passed = safeAreaExists && safeAreaAnchorsValid && portraitProfilesValid && hpTen && hpTextTen && runResetStateValid && runSeedRepeatValid && runContentChannelIsolationValid && simultaneousDeathPolicyValid && fateEntryLayoutValid && fateEntryPastelColorValid && fateEntryIdleAtFullHealth && summonHudReadable && initialPreparationFlowValid && initialPreparationBattleStartUiPathValid && runtimeStageLifecycleValid && inventoryStageHidden && dailyFateCupUiValid && bossForecastUiValid && bossForecastTimingValid && firstBossPreparationRewardRulesValid && outgameShopValid && resultRewardIconsValid && rankingPageValid && yahtzeeModeUiValid && yahtzeeMultiplierLogicValid && yahtzeeTicketMilestoneLogicValid && yahtzeeTicketRunAccumulationValid && yahtzeeTicketNewRunResetValid && playerDirectSummonIsolationValid && tacticalMissionRiskRewardValid && tacticalMissionChoiceValid && roundDiamondRewardValid && bannerBurstQueueValid && earlyMiniShopChoicesValid && choiceScheduleValid && recipePacingTelemetryValid && ultimateRecipeUxValid && ultimateMergeInheritanceIsolationValid && diceAutoCycleValid && thunderControlDamageConversionValid && feverEngineApexDpsValid && hero32SignatureValid && gargoyleLoopDurationValid && longCombatAccelerationValid && retryTimeScaleResetValid && choiceReadabilityValid && defaultVfxConfigured && animationMaterialEventsValid && screenSpaceCombatHudValid && dragTransactionSafetyValid && dragCombatSuspensionValid && boardCapacityPacingValid && pass1DGradeRulesValid && pass1DPressureValid && gradeUpgradeBarUiValid && pass2MSummonGradeLuckValid && pass2NCombatEconomyUiValid && pass2OCombatHudLayoutValid && pass1EMilestoneValid && pass2BPreparationSkipValid && runtimeErrors == 0;
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
                runResetStateValid = runResetStateValid,
                runSeedRepeatValid = runSeedRepeatValid,
                runContentChannelIsolationValid = runContentChannelIsolationValid,
                fateEntryLayoutValid = fateEntryLayoutValid,
                fateEntryPastelColorValid = fateEntryPastelColorValid,
                fateEntryIdleAtFullHealth = fateEntryIdleAtFullHealth,
                summonHudReadable = summonHudReadable,
                initialPreparationFlowValid = initialPreparationFlowValid,
                initialPreparationBattleStartUiPathValid = initialPreparationBattleStartUiPathValid,
                initialPreparationBattleStartUiPathSummary = initialPreparationBattleStartUiPathSummary,
                runtimeStageLifecycleValid = runtimeStageLifecycleValid,
                inventoryStageHidden = inventoryStageHidden,
                dailyFateCupUiValid = dailyFateCupUiValid,
                bossForecastUiValid = bossForecastUiValid,
                bossForecastTimingValid = bossForecastTimingValid,
                bossForecastTimingSummary = bossForecastTimingSummary,
                firstBossPreparationRewardRulesValid = firstBossPreparationRewardRulesValid,
                resultRewardIconsValid = resultRewardIconsValid,
                rankingPageValid = rankingPageValid,
                yahtzeeModeUiValid = yahtzeeModeUiValid,
                yahtzeeMultiplierLogicValid = yahtzeeMultiplierLogicValid,
                yahtzeeTicketMilestoneLogicValid = yahtzeeTicketMilestoneLogicValid,
                yahtzeeTicketRunAccumulationValid = yahtzeeTicketRunAccumulationValid,
                yahtzeeTicketNewRunResetValid = yahtzeeTicketNewRunResetValid,
                playerDirectSummonIsolationValid = playerDirectSummonIsolationValid,
                tacticalMissionRiskRewardValid = tacticalMissionRiskRewardValid,
                tacticalMissionChoiceValid = tacticalMissionChoiceValid,
                roundDiamondRewardValid = roundDiamondRewardValid,
                bannerBurstQueueValid = bannerBurstQueueValid,
                earlyMiniShopChoicesValid = earlyMiniShopChoicesValid,
                earlyMiniShopSummary = earlyMiniShopSummary,
                choiceScheduleValid = choiceScheduleValid,
                choiceScheduleSummary = choiceScheduleSummary,
                recipePacingTelemetryValid = recipePacingTelemetryValid,
                recipePacingTelemetrySummary = recipePacingTelemetrySummary,
                ultimateRecipeUxValid = ultimateRecipeUxValid,
                ultimateRecipeUxSummary = ultimateRecipeUxSummary,
                ultimateMergeInheritanceIsolationValid = ultimateMergeInheritanceIsolationValid,
                diceAutoCycleValid = diceAutoCycleValid,
                thunderControlDamageConversionValid = thunderControlDamageConversionValid,
                feverEngineApexDpsValid = feverEngineApexDpsValid,
                simultaneousDeathPolicyValid = simultaneousDeathPolicyValid,
                hero32SignatureValid = hero32SignatureValid,
                gargoyleLoopDurationValid = gargoyleLoopDurationValid,
                longCombatAccelerationValid = longCombatAccelerationValid,
                retryTimeScaleResetValid = retryTimeScaleResetValid,
                retryTimeScaleResetSummary = retryTimeScaleResetSummary,
                choiceReadabilityValid = choiceReadabilityValid,
                choiceReadabilitySummary = choiceReadabilitySummary,
                defaultVfxConfigured = defaultVfxConfigured,
                animationMaterialEventsValid = animationMaterialEventsValid,
                animationMaterialEventsSummary = animationMaterialEventsSummary,
                boardCapacityPacingValid = boardCapacityPacingValid,
                boardCapacityPacingSummary = boardCapacityPacingSummary,
                gradeUpgradeBarUiValid = gradeUpgradeBarUiValid,
                pass2MSummonGradeLuckValid = pass2MSummonGradeLuckValid,
                pass2NCombatEconomyUiValid = pass2NCombatEconomyUiValid,
                pass2OCombatHudLayoutValid = pass2OCombatHudLayoutValid,
                pass1EMilestoneValid = pass1EMilestoneValid,
                pass1EMilestoneSummary = pass1EMilestoneSummary,
                pass2BPreparationSkipValid = pass2BPreparationSkipValid,
                pass2BPreparationSkipSummary = pass2BPreparationSkipSummary,
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

        private static bool ValidateDefeatRetryTimeScaleReset(DefenseGameController controller, out string summary)
        {
            RoundManager roundManager = UnityEngine.Object.FindObjectOfType<RoundManager>();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo isRoundRunning = typeof(RoundManager).GetField("<IsRoundRunning>k__BackingField", flags);
            FieldInfo accelerationActive = typeof(RoundManager).GetField("combatTimeAccelerationActive", flags);
            FieldInfo baselineCaptured = typeof(RoundManager).GetField("combatSpeedBaselineCaptured", flags);
            FieldInfo baselineScale = typeof(RoundManager).GetField("combatSpeedBaselineTimeScale", flags);
            FieldInfo baselineFixed = typeof(RoundManager).GetField("combatSpeedBaselineFixedDeltaTime", flags);
            FieldInfo appliedScale = typeof(RoundManager).GetField("combatSpeedAppliedTimeScale", flags);
            MethodInfo captureFate = typeof(DefenseGameController).GetMethod("CaptureFateChoiceSlowMotion", flags);
            MethodInfo restoreFate = typeof(DefenseGameController).GetMethod("RestoreFateChoiceSlowMotion", flags);
            MethodInfo captureDefeat = typeof(DefenseGameController).GetMethod("CaptureDefeatTimeScale", flags);
            MethodInfo restoreDefeat = typeof(DefenseGameController).GetMethod("RestoreDefeatTimeScale", flags);
            if (controller == null || roundManager == null || isRoundRunning == null || accelerationActive == null || baselineCaptured == null || baselineScale == null || baselineFixed == null || appliedScale == null || captureFate == null || restoreFate == null || captureDefeat == null || restoreDefeat == null)
            {
                summary = "reflection_target_missing";
                return false;
            }

            float originalScale = Time.timeScale;
            float originalFixed = Time.fixedDeltaTime;
            try
            {
                controller.ResetRunForRetry();
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
                bool baseline = Mathf.Approximately(Time.timeScale, 1f) && Mathf.Approximately(Time.fixedDeltaTime, 0.02f);

                captureFate.Invoke(controller, null);
                Time.timeScale = 0.1f;
                Time.fixedDeltaTime = 0.002f;
                restoreFate.Invoke(controller, null);
                bool fateRestored = Mathf.Approximately(Time.timeScale, 1f) && Mathf.Approximately(Time.fixedDeltaTime, 0.02f);

                isRoundRunning.SetValue(roundManager, true);
                accelerationActive.SetValue(roundManager, true);
                baselineCaptured.SetValue(roundManager, true);
                baselineScale.SetValue(roundManager, 1f);
                baselineFixed.SetValue(roundManager, 0.02f);
                appliedScale.SetValue(roundManager, 2f);
                Time.timeScale = 2f;
                Time.fixedDeltaTime = 0.04f;
                roundManager.BeginDefeatCinematic();
                bool cinematicBaseline = Mathf.Approximately(Time.timeScale, 1f) && Mathf.Approximately(Time.fixedDeltaTime, 0.02f);

                captureDefeat.Invoke(controller, null);
                Time.timeScale = DefenseGameController.DefeatSlowMotionTargetScale;
                Time.fixedDeltaTime = 0.002f;
                restoreDefeat.Invoke(controller, null);
                bool defeatRestored = Mathf.Approximately(Time.timeScale, 1f) && Mathf.Approximately(Time.fixedDeltaTime, 0.02f);

                controller.ResetRunForRetry();
                bool retryRestored = Mathf.Approximately(Time.timeScale, 1f) && Mathf.Approximately(Time.fixedDeltaTime, 0.02f);
                summary = "baseline=" + baseline + ", fate=" + fateRestored + ", defeat=" + cinematicBaseline + ", slowmo=" + defeatRestored + ", retry=" + retryRestored;
                return baseline && fateRestored && cinematicBaseline && defeatRestored && retryRestored;
            }
            finally
            {
                restoreFate.Invoke(controller, null);
                restoreDefeat.Invoke(controller, null);
                controller.ResetRunForRetry();
                Time.timeScale = originalScale;
                Time.fixedDeltaTime = originalFixed;
            }
        }

        private static bool ValidateChoiceReadability(out string summary)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            LuckySummonChoiceUI luckyUi = UnityEngine.Object.FindObjectOfType<LuckySummonChoiceUI>(true);
            BossForecastBetUI bossForecastUi = UnityEngine.Object.FindObjectOfType<BossForecastBetUI>(true);
            typeof(LuckySummonChoiceUI).GetMethod("Refresh", flags)?.Invoke(luckyUi, null);
            typeof(BossForecastBetUI).GetMethod("Refresh", flags)?.Invoke(bossForecastUi, null);

            Text bossTitle = UnityEngine.Object.FindObjectsOfType<Text>(true).FirstOrDefault(textComponent => textComponent != null && textComponent.name == "BossForecastTitle");
            Text bossInstruction = UnityEngine.Object.FindObjectsOfType<Text>(true).FirstOrDefault(textComponent => textComponent != null && textComponent.name == "BossForecastInstruction");
            Button[] bossChoices = UnityEngine.Object.FindObjectsOfType<Button>(true).Where(button => button != null && button.name.StartsWith("BossForecastChoice_", StringComparison.Ordinal)).OrderBy(button => button.name).ToArray();
            Button[] luckyChoices = UnityEngine.Object.FindObjectsOfType<Button>(true).Where(button => button != null && button.name.StartsWith("LuckySummonChoice", StringComparison.Ordinal)).OrderBy(button => button.name).ToArray();
            Button[] missionChoices = UnityEngine.Object.FindObjectsOfType<Button>(true).Where(button => button != null && button.name.StartsWith("MissionOption_", StringComparison.Ordinal)).OrderBy(button => button.name).ToArray();
            Text activeDescription = UnityEngine.Object.FindObjectsOfType<Text>(true).FirstOrDefault(textComponent => textComponent != null && textComponent.name == "ActiveMissionDescription");
            Text activeProgress = UnityEngine.Object.FindObjectsOfType<Text>(true).FirstOrDefault(textComponent => textComponent != null && textComponent.name == "ActiveMissionProgress");
            bool bossCopy = bossTitle != null && bossTitle.text == "R10 \ubcf4\uc2a4 \ub300\ube44" && bossInstruction != null && bossInstruction.text.Contains("R10\uc744 \uc5b4\ub5bb\uac8c \uc900\ube44") && bossChoices.Length == 3 &&
                bossChoices[0].GetComponentInChildren<Text>(true).text.Contains("\uc720\ub2db \ud655\ubcf4") && bossChoices[1].GetComponentInChildren<Text>(true).text.Contains("\uace0\ub4f1\uae09 \ub178\ub9ac\uae30") && bossChoices[2].GetComponentInChildren<Text>(true).text.Contains("\uc548\uc804\ud558\uac8c \ubc84\ud2f0\uae30");
            bool luckyCopy = luckyChoices.Length == 3 && luckyChoices[0].GetComponentInChildren<Text>(true).text.Contains("\ud569\uc131 \uc7ac\ub8cc \ubcf4\ucda9") && luckyChoices[1].GetComponentInChildren<Text>(true).text.Contains("\ub808\uc5b4 \uc774\uc0c1 \ud655\uc815") && luckyChoices[2].GetComponentInChildren<Text>(true).text.Contains("\uc5d0\ud53d 25% \ub3c4\uc804");
            bool missionFonts = missionChoices.Length == 3 && missionChoices.All(button =>
            {
                Text title = button.transform.Find("Title")?.GetComponent<Text>();
                Text description = button.transform.Find("Description")?.GetComponent<Text>();
                Text reward = button.transform.Find("Reward")?.GetComponent<Text>();
                return title != null && description != null && reward != null && title.fontSize >= 29 && description.fontSize >= 22 && reward.fontSize >= 23 && reward.fontStyle == FontStyle.Bold && !title.resizeTextForBestFit && !description.resizeTextForBestFit && !reward.resizeTextForBestFit;
            });
            bool activeFonts = activeDescription != null && activeProgress != null && activeDescription.fontSize >= 23 && activeProgress.fontSize > activeDescription.fontSize && activeProgress.fontStyle == FontStyle.Bold;
            bool portraitBounds = missionChoices.All(button => IsInsideOverlay(button.GetComponent<RectTransform>(), "TacticalMissionOverlay"));
            summary = "boss=" + bossCopy + ", lucky=" + luckyCopy + ", missionFonts=" + missionFonts + ", active=" + activeFonts + ", portrait=" + portraitBounds;
            return bossCopy && luckyCopy && missionFonts && activeFonts && portraitBounds;
        }

        private static bool IsInsideOverlay(RectTransform child, string overlayName)
        {
            RectTransform overlay = UnityEngine.Object.FindObjectsOfType<RectTransform>(true).FirstOrDefault(rect => rect != null && rect.name == overlayName);
            if (child == null || overlay == null)
            {
                return false;
            }

            Vector3[] corners = new Vector3[4];
            child.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                if (!overlay.rect.Contains(overlay.InverseTransformPoint(corners[i])))
                {
                    return false;
                }
            }

            return true;
        }
        private static bool ValidateAnimationMaterialEvents(out string summary)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                summary = "shader_missing";
                return false;
            }

            Material[] previousCatalog = AnimationEventMaterialRegistry.GetConfiguredMaterials();
            GameObject root = null;
            Material original = null;
            Material replacement = null;
            try
            {
                root = new GameObject("AnimationMaterialEventSmoke");
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.transform.SetParent(root.transform, false);
                Renderer renderer = visual.GetComponent<Renderer>();
                original = new Material(shader) { name = "SmokeOriginalMaterial", color = Color.white };
                replacement = new Material(shader) { name = "SmokeOverrideMaterial", color = Color.magenta };
                renderer.sharedMaterial = original;
                AnimationEventMaterialRegistry.Configure(new[] { replacement });
                AnimationMaterialOverrideController controller = root.AddComponent<AnimationMaterialOverrideController>();

                bool overrideCall = controller.OverrideMaterial("SmokeOverrideMaterial");
                Material afterOverride = renderer.sharedMaterial;
                bool applied = overrideCall && afterOverride == replacement;
                bool resetCall = controller.ResetMaterial("SmokeOverrideMaterial");
                Material afterReset = renderer.sharedMaterial;
                bool reset = resetCall && afterReset == original;
                summary = "overrideCall=" + overrideCall +
                          ", afterOverride=" + (afterOverride != null ? afterOverride.name : "null") +
                          ", expectedOverride=" + replacement.name +
                          ", resetCall=" + resetCall +
                          ", afterReset=" + (afterReset != null ? afterReset.name : "null") +
                          ", expectedReset=" + original.name;
                return applied && reset;
            }
            finally
            {
                AnimationEventMaterialRegistry.Configure(previousCatalog);
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
                if (original != null)
                {
                    UnityEngine.Object.DestroyImmediate(original);
                }
                if (replacement != null)
                {
                    UnityEngine.Object.DestroyImmediate(replacement);
                }
            }
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

        private static void ResetRoundBannerForSmoke(SimpleGameHUD hud)
        {
            if (hud == null)
            {
                return;
            }

            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(SimpleGameHUD).GetMethod("ClearPostRoundBannerQueue", Flags)?.Invoke(hud, null);
            typeof(SimpleGameHUD).GetField("roundBannerTimer", Flags)?.SetValue(hud, 0f);
            typeof(SimpleGameHUD).GetMethod("UpdateRoundBanner", Flags)?.Invoke(hud, null);
        }
        private static void AdvanceRoundBannerForSmoke(SimpleGameHUD hud)
        {
            if (hud == null)
            {
                return;
            }

            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo timerField = typeof(SimpleGameHUD).GetField("roundBannerTimer", Flags);
            MethodInfo updateMethod = typeof(SimpleGameHUD).GetMethod("UpdateRoundBanner", Flags);
            timerField?.SetValue(hud, 0f);
            updateMethod?.Invoke(hud, null);
        }
        private static bool ValidateScreenSpaceCombatHud()
        {
            GameObject anchorProbe = new GameObject("FloatingCombatUiAnchorProbe");
            GameObject head = new GameObject("Head");
            head.transform.SetParent(anchorProbe.transform, false);
            GameObject explicitAnchor = new GameObject("FloatingUIAnchor");
            explicitAnchor.transform.SetParent(anchorProbe.transform, false);
            FloatingCombatUI smokeUi = FloatingCombatUI.Attach(anchorProbe.transform, "Smoke", Color.white, CharacterGrade.Normal);
            SharedFloatingCombatCanvas sharedRoot = UnityEngine.Object.FindObjectOfType<SharedFloatingCombatCanvas>();
            Canvas sharedCanvas = sharedRoot != null ? sharedRoot.GetComponent<Canvas>() : null;
            CanvasScaler scaler = sharedRoot != null ? sharedRoot.GetComponent<CanvasScaler>() : null;
            bool canvasValid = sharedCanvas != null &&
                               sharedCanvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                               sharedCanvas.worldCamera == null &&
                               sharedCanvas.sortingOrder == -10 &&
                               scaler != null &&
                               scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize;

            const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
            MethodInfo resolveAnchor = typeof(FloatingCombatUI).GetMethod("ResolveAnchor", Flags);
            object[] arguments = { anchorProbe.transform, 0f };
            Transform resolvedAnchor = resolveAnchor != null ? resolveAnchor.Invoke(null, arguments) as Transform : null;
            bool anchorPriorityValid = resolvedAnchor == explicitAnchor.transform;
            bool uiCreated = smokeUi != null;
            if (smokeUi != null)
            {
                UnityEngine.Object.DestroyImmediate(smokeUi.gameObject);
            }
            UnityEngine.Object.DestroyImmediate(anchorProbe);

            return canvasValid && anchorPriorityValid && uiCreated;
        }
        private static bool ValidateDragTransactionSafety()
        {
            GameObject boardRoot = new GameObject("DragTransactionSmokeBoard");
            DefenseBoardManager board = boardRoot.AddComponent<DefenseBoardManager>();
            GameObject sourceObject = new GameObject("DragTransactionSource");
            BoardSlot sourceSlot = sourceObject.AddComponent<BoardSlot>();
            GameObject occupiedObject = new GameObject("DragTransactionOccupied");
            BoardSlot occupiedSlot = occupiedObject.AddComponent<BoardSlot>();
            GameObject emptyObject = new GameObject("DragTransactionEmpty");
            BoardSlot emptySlot = emptyObject.AddComponent<BoardSlot>();
            board.Configure(new List<BoardSlot> { sourceSlot, occupiedSlot, emptySlot }, null);

            GameObject firstObject = new GameObject("DragTransactionFirst");
            firstObject.AddComponent<BoxCollider>();
            DefenderUnit firstUnit = firstObject.AddComponent<DefenderUnit>();
            GameObject secondObject = new GameObject("DragTransactionSecond");
            secondObject.AddComponent<BoxCollider>();
            DefenderUnit secondUnit = secondObject.AddComponent<DefenderUnit>();
            sourceSlot.AssignUnit(firstUnit);
            occupiedSlot.AssignUnit(secondUnit);

            const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo beginDrag = typeof(DefenseBoardManager).GetMethod("TryBeginDrag", InstancePrivate);
            FieldInfo draggedField = typeof(DefenseBoardManager).GetField("draggedUnit", InstancePrivate);
            beginDrag?.Invoke(board, new object[] { firstUnit });
            bool beginPreservesLogicalOccupancy = board.UnitCount == 2 &&
                                                  sourceSlot.OccupiedUnit == firstUnit &&
                                                  firstUnit.CurrentSlot == sourceSlot &&
                                                  SharedFloatingCombatCanvas.IsPoseRefreshOverrideActive(firstUnit.transform);

            bool emptyDropValid = board.TryMoveUnit(firstUnit, emptySlot) &&
                                  board.UnitCount == 2 &&
                                  emptySlot.OccupiedUnit == firstUnit &&
                                  sourceSlot.IsEmpty &&
                                  occupiedSlot.OccupiedUnit == secondUnit;
            bool swapValid = board.TryMoveUnit(firstUnit, occupiedSlot) &&
                             board.UnitCount == 2 &&
                             occupiedSlot.OccupiedUnit == firstUnit &&
                             emptySlot.OccupiedUnit == secondUnit;
            board.CancelActiveDrag();
            bool cancelRestoresTracking = draggedField != null &&
                                          draggedField.GetValue(board) == null &&
                                          firstUnit.CurrentSlot != null &&
                                          firstUnit.CurrentSlot.OccupiedUnit == firstUnit &&
                                          !SharedFloatingCombatCanvas.IsPoseRefreshOverrideActive(firstUnit.transform);

            beginDrag?.Invoke(board, new object[] { firstUnit });
            board.enabled = false;
            bool disableClearsDrag = draggedField != null &&
                                     draggedField.GetValue(board) == null &&
                                     firstUnit.CurrentSlot != null &&
                                     firstUnit.CurrentSlot.OccupiedUnit == firstUnit &&
                                     !SharedFloatingCombatCanvas.IsPoseRefreshOverrideActive(firstUnit.transform);
            board.enabled = true;

            beginDrag?.Invoke(board, new object[] { firstUnit });
            board.ClearAllDeployedUnits();
            bool clearRemovesLogicalUnits = board.UnitCount == 0 &&
                                            draggedField != null &&
                                            draggedField.GetValue(board) == null &&
                                            !SharedFloatingCombatCanvas.IsPoseRefreshOverrideActive(firstUnit.transform);

            UnityEngine.Object.DestroyImmediate(boardRoot);
            UnityEngine.Object.DestroyImmediate(sourceObject);
            UnityEngine.Object.DestroyImmediate(occupiedObject);
            UnityEngine.Object.DestroyImmediate(emptyObject);
            bool boardBoundsValid = ValidateBoardDragBounds();
            return beginPreservesLogicalOccupancy && emptyDropValid && swapValid &&
                   cancelRestoresTracking && disableClearsDrag && clearRemovesLogicalUnits && boardBoundsValid;
        }

        private static bool ValidatePass2MSummonGradeLuck(DefenseGameController controller)
        {
            CharacterDatabase database = controller != null ? controller.GetComponent<CharacterDatabase>() : null;
            if (database == null)
            {
                return false;
            }

            int[] expectedCosts = { 50, 100, 200, 400, 800, 1600, 3200, 0 };
            bool costsValid = DefenseGameController.SummonGradeLuckMaximumLevel == 7;
            for (int level = 0; level <= DefenseGameController.SummonGradeLuckMaximumLevel; level++)
            {
                costsValid &= DefenseGameController.ResolveSummonGradeLuckCost(level) == expectedCosts[level];
            }

            SummonGradeRateSnapshot r13Lv0 = database.GetSummonGradeRateSnapshot(13, 0, false);
            SummonGradeRateSnapshot r13Lv1 = database.GetSummonGradeRateSnapshot(13, 1, false);
            SummonGradeRateSnapshot r13Lv7 = database.GetSummonGradeRateSnapshot(13, 7, false);
            SummonGradeRateSnapshot r19Lv0 = database.GetSummonGradeRateSnapshot(19, 0, false);
            SummonGradeRateSnapshot r19Lv2 = database.GetSummonGradeRateSnapshot(19, 2, false);
            SummonGradeRateSnapshot r19Lv7 = database.GetSummonGradeRateSnapshot(19, 7, false);
            SummonGradeRateSnapshot r28Lv0 = database.GetSummonGradeRateSnapshot(28, 0, false);
            SummonGradeRateSnapshot r28Lv2 = database.GetSummonGradeRateSnapshot(28, 2, false);
            SummonGradeRateSnapshot r28Lv7 = database.GetSummonGradeRateSnapshot(28, 7, false);
            bool referenceRatesValid = Approximately(r13Lv0.EpicPlus, 0.050f) && Approximately(r13Lv1.EpicPlus, 0.060f) && Approximately(r13Lv7.EpicPlus, 0.120f)
                && Approximately(r19Lv0.EpicPlus, 0.095f) && Approximately(r19Lv2.EpicPlus, 0.115f) && Approximately(r19Lv7.EpicPlus, 0.165f)
                && Approximately(r28Lv0.EpicPlus, 0.155f) && Approximately(r28Lv2.EpicPlus, 0.175f) && Approximately(r28Lv7.EpicPlus, 0.225f);

            int[] rounds = { 1, 3, 5, 7, 9, 11, 13, 16, 19, 22, 25, 28, 31, 34, 37, 40, 43, 46, 49 };
            float[] expectedEpicPlus = { 0f, 0f, 0.004f, 0.012f, 0.025f, 0.038f, 0.050f, 0.072f, 0.095f, 0.115f, 0.140f, 0.155f, 0.170f, 0.185f, 0.200f, 0.215f, 0.230f, 0.245f, 0.260f };
            float[] expectedRare = { 0.040f, 0.060f, 0.085f, 0.105f, 0.130f, 0.155f, 0.180f, 0.215f, 0.240f, 0.260f, 0.275f, 0.290f, 0.305f, 0.315f, 0.325f, 0.335f, 0.345f, 0.350f, 0.355f };
            bool milestoneRatesValid = true;
            for (int i = 0; i < rounds.Length; i++)
            {
                SummonGradeRateSnapshot baseRates = database.GetSummonGradeRateSnapshot(rounds[i], 0, false);
                SummonGradeRateSnapshot maxLuckRates = database.GetSummonGradeRateSnapshot(rounds[i], DefenseGameController.SummonGradeLuckMaximumLevel, false);
                milestoneRatesValid &= Approximately(baseRates.EpicPlus, expectedEpicPlus[i])
                    && Approximately(baseRates.rare, expectedRare[i])
                    && Approximately(baseRates.Total, 1f)
                    && Approximately(maxLuckRates.rare, expectedRare[i])
                    && Approximately(maxLuckRates.Total, 1f);
            }

            SummonGradeRateSnapshot early = database.GetSummonGradeRateSnapshot(3, DefenseGameController.SummonGradeLuckMaximumLevel, false);
            SummonGradeRateSnapshot epicOnly = database.GetSummonGradeRateSnapshot(7, DefenseGameController.SummonGradeLuckMaximumLevel, false);
            SummonGradeRateSnapshot legendLocked = database.GetSummonGradeRateSnapshot(9, DefenseGameController.SummonGradeLuckMaximumLevel, false);
            bool noEarlyUnlock = Approximately(early.epic, 0f) && Approximately(early.legendary, 0f) && Approximately(early.mythic, 0f)
                && Approximately(epicOnly.legendary, 0f) && Approximately(epicOnly.mythic, 0f)
                && Approximately(legendLocked.mythic, 0f);
            Button luckButton = UnityEngine.Object.FindObjectsOfType<Button>(true).FirstOrDefault(button => button != null && button.name == "SummonGradeLuckUpgrade");
            return costsValid && referenceRatesValid && milestoneRatesValid && noEarlyUnlock && luckButton != null;
        }

        private static bool ValidatePass1DGradeUpgradeRules(DefenseGameController controller)
        {
            bool baseCosts = DefenseGameController.ResolveGradeUpgradeBaseCost(CharacterGrade.Normal) == 20 &&
                             DefenseGameController.ResolveGradeUpgradeBaseCost(CharacterGrade.Rare) == 30 &&
                             DefenseGameController.ResolveGradeUpgradeBaseCost(CharacterGrade.Epic) == 45 &&
                             DefenseGameController.ResolveGradeUpgradeBaseCost(CharacterGrade.Legendary) == 65 &&
                             DefenseGameController.ResolveGradeUpgradeBaseCost(CharacterGrade.Mythic) == 90 &&
                             DefenseGameController.ResolveGradeUpgradeBaseCost(CharacterGrade.Transcendent) == 120;
            bool escalation = DefenseGameController.ResolveGradeUpgradeCost(CharacterGrade.Normal, 0) == 20 &&
                              DefenseGameController.ResolveGradeUpgradeCost(CharacterGrade.Normal, 1) == 30 &&
                              DefenseGameController.ResolveGradeUpgradeCost(CharacterGrade.Transcendent, 0) == 120 &&
                              DefenseGameController.ResolveGradeUpgradeCost(CharacterGrade.Transcendent, 1) == 175;
            bool constantsValid = baseCosts && escalation &&
                                  Approximately(1f + DefenseGameController.GradeUpgradeAttackPerLevel, 1.08f) &&
                                  Approximately(1f + DefenseGameController.GradeUpgradeHealthPerLevel, 1.05f) &&
                                  DefenseGameController.GradeUpgradeMaximumLevel == 10;
            return constantsValid && ValidatePass1DGradeUpgradeRuntime(controller) &&
                   ValidatePass1DMergeInheritanceRegression();
        }

        private static bool ValidatePass1DGradeUpgradeRuntime(DefenseGameController controller)
        {
            if (controller == null)
            {
                return false;
            }

            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo goldField = typeof(DefenseGameController).GetField("<Gold>k__BackingField", Flags);
            FieldInfo boardField = typeof(DefenseGameController).GetField("boardManager", Flags);
            FieldInfo roundField = typeof(DefenseGameController).GetField("roundManager", Flags);
            FieldInfo roundRunningField = typeof(RoundManager).GetField("<IsRoundRunning>k__BackingField", Flags);
            if (goldField == null || boardField == null || roundField == null || roundRunningField == null)
            {
                return false;
            }

            DefenseBoardManager originalBoard = boardField.GetValue(controller) as DefenseBoardManager;
            RoundManager originalRounds = roundField.GetValue(controller) as RoundManager;
            int originalGold = controller.Gold;
            GameObject root = new GameObject("Pass1DGradeUpgradeRuntimeSmoke");
            DefenseBoardManager board = root.AddComponent<DefenseBoardManager>();
            GameObject roundsObject = new GameObject("Pass1DGradeUpgradeRuntimeRounds");
            RoundManager rounds = roundsObject.AddComponent<RoundManager>();
            List<BoardSlot> slots = new List<BoardSlot>();
            List<GameObject> units = new List<GameObject>();
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    slots.Add(CreateDragBoundsSlot(root.transform, "GradeUpgradeSlot_" + i, new Vector3(i, 0f, 0f)));
                }
                board.Configure(slots, null);
                boardField.SetValue(controller, board);
                roundField.SetValue(controller, rounds);
                controller.ResetRunForRetry();
                goldField.SetValue(controller, 10000);

                DefenderUnit normal = CreatePass1DProbeUnit(root.transform, "normal", CharacterGrade.Normal, 100f, 10f, false, units);
                DefenderUnit rare = CreatePass1DProbeUnit(root.transform, "rare", CharacterGrade.Rare, 100f, 20f, false, units);
                DefenderUnit temporaryNormal = CreatePass1DProbeUnit(root.transform, "temporary", CharacterGrade.Normal, 100f, 10f, true, units);
                slots[0].AssignUnit(normal);
                slots[1].AssignUnit(rare);

                int goldBefore = controller.Gold;
                int normalCost = controller.GetGradeUpgradeCost(CharacterGrade.Normal);
                bool firstPurchase = controller.TryUpgradeGrade(CharacterGrade.Normal) &&
                                     controller.GetGradeUpgradeLevel(CharacterGrade.Normal) == 1 &&
                                     controller.Gold == goldBefore - normalCost &&
                                     Approximately(normal.EffectiveAttackPower, 10.8f) &&
                                     Approximately(normal.MaxHealth, 105f) &&
                                     Approximately(rare.EffectiveAttackPower, 20f) &&
                                     Approximately(rare.MaxHealth, 100f) &&
                                     Approximately(temporaryNormal.EffectiveAttackPower, 10f) &&
                                     Approximately(temporaryNormal.MaxHealth, 100f);

                DefenderUnit futureNormal = CreatePass1DProbeUnit(root.transform, "future", CharacterGrade.Normal, 100f, 10f, false, units);
                bool futureApplied = Approximately(futureNormal.EffectiveAttackPower, 10.8f) && Approximately(futureNormal.MaxHealth, 105f);

                goldField.SetValue(controller, 10000);
                bool capReached = true;
                while (controller.GetGradeUpgradeLevel(CharacterGrade.Normal) < DefenseGameController.GradeUpgradeMaximumLevel)
                {
                    capReached &= controller.TryUpgradeGrade(CharacterGrade.Normal);
                }
                bool maxStops = capReached && !controller.TryUpgradeGrade(CharacterGrade.Normal) &&
                                controller.GetGradeUpgradeLevel(CharacterGrade.Normal) == DefenseGameController.GradeUpgradeMaximumLevel;

                roundRunningField.SetValue(rounds, true);
                goldField.SetValue(controller, 10000);
                int rareCost = controller.GetGradeUpgradeCost(CharacterGrade.Rare);
                int goldBeforeCombatUpgrade = controller.Gold;
                bool combatUpgradeAppliesImmediately = controller.CanUpgradeGrade(CharacterGrade.Rare) &&
                                                     controller.TryUpgradeGrade(CharacterGrade.Rare) &&
                                                     controller.GetGradeUpgradeLevel(CharacterGrade.Rare) == 1 &&
                                                     controller.Gold == goldBeforeCombatUpgrade - rareCost &&
                                                     Approximately(rare.EffectiveAttackPower, 21.6f) &&
                                                     Approximately(rare.MaxHealth, 105f);
                roundRunningField.SetValue(rounds, false);

                controller.ResetRunForRetry();
                bool resetClears = controller.GetGradeUpgradeLevel(CharacterGrade.Normal) == 0 &&
                                   controller.GetGradeUpgradeLevel(CharacterGrade.Rare) == 0;
                return firstPurchase && futureApplied && maxStops && combatUpgradeAppliesImmediately && resetClears;
            }
            finally
            {
                boardField.SetValue(controller, originalBoard);
                roundField.SetValue(controller, originalRounds);
                goldField.SetValue(controller, originalGold);
                for (int i = 0; i < units.Count; i++)
                {
                    if (units[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(units[i]);
                    }
                }
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(roundsObject);
                controller.ResetRunForRetry();
            }
        }

        private static DefenderUnit CreatePass1DProbeUnit(Transform parent, string id, CharacterGrade grade, float health, float attack, bool temporary, List<GameObject> units)
        {
            GameObject unitObject = new GameObject("Pass1DProbe_" + id);
            unitObject.transform.SetParent(parent, false);
            DefenderUnit unit = unitObject.AddComponent<DefenderUnit>();
            CharacterDefinition definition = new CharacterDefinition
            {
                id = "pass1d_" + id,
                displayName = id,
                grade = grade,
                stats = new CombatStats { maxHealth = health, attackPower = attack },
                attackBehavior = new AttackBehavior()
            };
            if (temporary)
            {
                unit.InitializeSummon(definition);
            }
            else
            {
                unit.Initialize(definition);
            }
            units.Add(unitObject);
            return unit;
        }

        private static bool ValidatePass1DMergeInheritanceRegression()
        {
            GameObject resultObject = new GameObject("Pass1DMergeResultProbe");
            DefenderUnit result = resultObject.AddComponent<DefenderUnit>();
            try
            {
                CharacterDefinition definition = new CharacterDefinition
                {
                    id = "pass1d_merge_result",
                    displayName = "Merge Result",
                    grade = CharacterGrade.Rare,
                    stats = new CombatStats { maxHealth = 100f, attackPower = 10f },
                    attackBehavior = new AttackBehavior()
                };
                result.Initialize(definition);
                result.SetRunGradeUpgradeBonuses(0.16f, 0.10f, false);
                result.ApplyMergeInheritance(15f, 150f);
                bool exactIntrinsicInheritance = Approximately(result.EffectiveAttackPowerWithoutRunGradeUpgrade, 15f) &&
                                                 Approximately(result.MaxHealthWithoutRunGradeUpgrade, 150f);
                bool resultUpgradeAppliedOnce = Approximately(result.EffectiveAttackPower, 16.6f) &&
                                                Approximately(result.MaxHealth, 160f);

                GameObject higherResultObject = new GameObject("Pass1DHigherMergeResultProbe");
                DefenderUnit higherResult = higherResultObject.AddComponent<DefenderUnit>();
                try
                {
                    higherResult.Initialize(definition);
                    higherResult.SetRunGradeUpgradeBonuses(0.32f, 0.20f, false);
                    higherResult.ApplyMergeInheritance(15f, 150f);
                    bool strongerAtHigherResultUpgrade = higherResult.EffectiveAttackPower > result.EffectiveAttackPower &&
                                                         higherResult.MaxHealth > result.MaxHealth;
                    return exactIntrinsicInheritance && resultUpgradeAppliedOnce && strongerAtHigherResultUpgrade;
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(higherResultObject);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(resultObject);
            }
        }
        private static bool ValidatePass2OCombatHudLayout(DefenseGameController controller)
        {
            GameObject root = UnityEngine.Object.FindObjectsOfType<Transform>(true)
                .FirstOrDefault(transform => transform != null && transform.name == "GradeUpgradeBar")?.gameObject;
            CanvasGroup canvasGroup = root != null ? root.GetComponent<CanvasGroup>() : null;
            Button[] gradeButtons = root != null
                ? root.GetComponentsInChildren<Button>(true).Where(button => button != null && button.name.StartsWith("GradeUpgrade_")).ToArray()
                : Array.Empty<Button>();
            Button luckButton = root != null ? root.GetComponentsInChildren<Button>(true).FirstOrDefault(button => button != null && button.name == "SummonGradeLuckUpgrade") : null;
            Button infoButton = root != null ? root.GetComponentsInChildren<Button>(true).FirstOrDefault(button => button != null && button.name == "SummonGradeLuckInfoButton") : null;
            Transform tooltip = root != null ? root.transform.Find("SummonGradeLuckInfoTooltip") : null;
            RectTransform luckRect = luckButton != null ? luckButton.GetComponent<RectTransform>() : null;
            Text luckLabel = luckButton != null ? luckButton.GetComponentInChildren<Text>(true) : null;
            Button fateEntryButton = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(button => button != null && button.name == "FatePanelReopenButton");
            RectTransform fateEntryRect = fateEntryButton != null ? fateEntryButton.GetComponent<RectTransform>() : null;
            bool baseHudRemoved = !UnityEngine.Object.FindObjectsOfType<Transform>(true).Any(transform => transform != null &&
                (transform.name == "HintText" || transform.name == "UltimateRecipeHudPanel"));

            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo roundField = typeof(DefenseGameController).GetField("roundManager", Flags);
            FieldInfo roundRunningField = typeof(RoundManager).GetField("<IsRoundRunning>k__BackingField", Flags);
            FieldInfo goldField = typeof(DefenseGameController).GetField("<Gold>k__BackingField", Flags);
            MethodInfo notify = typeof(DefenseGameController).GetMethod("NotifyStateChanged", Flags);
            RoundManager rounds = controller != null && roundField != null ? roundField.GetValue(controller) as RoundManager : null;
            if (controller == null || canvasGroup == null || gradeButtons.Length != 6 || luckButton == null || luckRect == null || luckLabel == null || infoButton == null || tooltip == null || fateEntryButton == null || fateEntryRect == null ||
                rounds == null || roundRunningField == null || goldField == null || notify == null)
            {
                return false;
            }

            bool originalRunning = (bool)roundRunningField.GetValue(rounds);
            int originalGold = controller.Gold;
            try
            {
                goldField.SetValue(controller, 10000);
                roundRunningField.SetValue(rounds, false);
                notify.Invoke(controller, null);
                bool preparationVisible = root.activeInHierarchy && luckButton.gameObject.activeInHierarchy &&
                                          Approximately(canvasGroup.alpha, 1f) && canvasGroup.blocksRaycasts && canvasGroup.interactable &&
                                          gradeButtons.All(button => button.interactable) && luckButton.interactable && infoButton.interactable;

                goldField.SetValue(controller, 0);
                notify.Invoke(controller, null);
                bool visibleWithInsufficientGold = luckButton.gameObject.activeInHierarchy && !luckButton.interactable;

                goldField.SetValue(controller, 10000);
                notify.Invoke(controller, null);

                roundRunningField.SetValue(rounds, true);
                notify.Invoke(controller, null);
                bool combatVisibleAndInteractive = Approximately(canvasGroup.alpha, 1f) && canvasGroup.blocksRaycasts && canvasGroup.interactable &&
                                                  gradeButtons.All(button => button.interactable) && luckButton.interactable && infoButton.interactable;

                int luckGoldBefore = controller.Gold;
                luckButton.onClick.Invoke();
                bool combatLuckPurchase = controller.SummonGradeLuckLevel == 1 && controller.Gold == luckGoldBefore - 50 &&
                                          luckLabel.text.Contains("Lv.1") && luckLabel.text.Contains("100 GOLD");
                bool fateDoesNotOverlapEconomy = !HasMajorRectOverlap(fateEntryRect, luckRect) &&
                                                  gradeButtons.All(button => !HasMajorRectOverlap(fateEntryRect, button.GetComponent<RectTransform>()));
                bool fateIndependentAction = fateEntryButton.gameObject != root && fateEntryButton.transform.parent != root.transform;

                infoButton.onClick.Invoke();
                bool tooltipOpens = tooltip.gameObject.activeSelf;
                controller.ResetRunForRetry();
                bool tooltipClosesOnReset = !tooltip.gameObject.activeSelf && controller.SummonGradeLuckLevel == 0;

                return baseHudRemoved && preparationVisible && visibleWithInsufficientGold && combatVisibleAndInteractive &&
                       luckRect.sizeDelta.x >= 340f && luckRect.sizeDelta.y >= 80f &&
                       luckLabel.text.Contains("\uace0\ub4f1\uae09 \ud655\ub960") && combatLuckPurchase &&
                       fateDoesNotOverlapEconomy && fateIndependentAction && tooltipOpens && tooltipClosesOnReset;
            }
            finally
            {
                roundRunningField.SetValue(rounds, originalRunning);
                goldField.SetValue(controller, originalGold);
                notify.Invoke(controller, null);
            }
        }
        private static bool HasMajorRectOverlap(RectTransform first, RectTransform second)
        {
            if (first == null || second == null)
            {
                return true;
            }

            Vector3[] firstCorners = new Vector3[4];
            Vector3[] secondCorners = new Vector3[4];
            first.GetWorldCorners(firstCorners);
            second.GetWorldCorners(secondCorners);
            Rect firstBounds = Rect.MinMaxRect(firstCorners[0].x, firstCorners[0].y, firstCorners[2].x, firstCorners[2].y);
            Rect secondBounds = Rect.MinMaxRect(secondCorners[0].x, secondCorners[0].y, secondCorners[2].x, secondCorners[2].y);
            float overlapWidth = Mathf.Min(firstBounds.xMax, secondBounds.xMax) - Mathf.Max(firstBounds.xMin, secondBounds.xMin);
            float overlapHeight = Mathf.Min(firstBounds.yMax, secondBounds.yMax) - Mathf.Max(firstBounds.yMin, secondBounds.yMin);
            return overlapWidth > 12f && overlapHeight > 12f;
        }

        private static bool ValidatePass1DPressureRules()
        {
            bool classic = Approximately(ClassicRoundPressure.ResolveClassicRoundHealthMultiplier(1), 1f) &&
                           Approximately(ClassicRoundPressure.ResolveClassicRoundAttackMultiplier(1), 1f) &&
                           Approximately(ClassicRoundPressure.ResolveClassicRoundHealthMultiplier(10), 1.12f) &&
                           Approximately(ClassicRoundPressure.ResolveClassicRoundAttackMultiplier(10), 1.05f) &&
                           Approximately(ClassicRoundPressure.ResolveClassicRoundHealthMultiplier(20), 1.30f) &&
                           Approximately(ClassicRoundPressure.ResolveClassicRoundAttackMultiplier(20), 1.12f) &&
                           Approximately(ClassicRoundPressure.ResolveClassicRoundHealthMultiplier(30), 1.55f) &&
                           Approximately(ClassicRoundPressure.ResolveClassicRoundAttackMultiplier(30), 1.22f) &&
                           Approximately(ClassicRoundPressure.ResolveClassicRoundHealthMultiplier(40), 1.85f) &&
                           Approximately(ClassicRoundPressure.ResolveClassicRoundAttackMultiplier(40), 1.34f) &&
                           Approximately(ClassicRoundPressure.ResolveClassicRoundHealthMultiplier(50), 2.15f) &&
                           Approximately(ClassicRoundPressure.ResolveClassicRoundAttackMultiplier(50), 1.46f) &&
                           Approximately(ClassicRoundPressure.ResolveClassicRoundHealthMultiplier(100), 3.25f) &&
                           Approximately(ClassicRoundPressure.ResolveClassicRoundAttackMultiplier(100), 1.85f);
            bool challenge = ClassicRoundPressure.IsChallengeRound(35) && !ClassicRoundPressure.IsChallengeRound(30) &&
                             Approximately(ClassicRoundPressure.ResolveChallengeHealthMultiplier(35), 1.25f) &&
                             Approximately(ClassicRoundPressure.ResolveChallengeAttackMultiplier(35), 1.15f) &&
                             Approximately(ClassicRoundPressure.ResolveChallengeSpawnCountMultiplier(35), 1.20f) &&
                             Approximately(ClassicRoundPressure.ResolveChallengeSpawnIntervalMultiplier(35), 0.85f);
            bool capAndFloor = ClassicRoundPressure.ApplyChallengeSpawnCount(35, 60, 60) == 60 &&
                               ClassicRoundPressure.ApplyChallengeSpawnCount(30, 50, 60) == 50 &&
                               Approximately(ClassicRoundPressure.ApplyChallengeSpawnInterval(35, 0.30f, 0.28f), 0.28f) &&
                               Approximately(ClassicRoundPressure.ApplyChallengeSpawnInterval(30, 0.30f, 0.28f), 0.30f);
            bool scope = ClassicRoundPressure.AppliesTo(CombatModeProfile.CreateClassic(), false) &&
                         !ClassicRoundPressure.AppliesTo(CombatModeProfile.CreateClassic(), true) &&
                         !ClassicRoundPressure.AppliesTo(CombatModeProfile.CreateOverdrive(), false);
            return classic && challenge && capAndFloor && scope;
        }
        private static bool ValidatePass1EMilestoneRules(DefenseGameController controller, out string summary)
        {
            CombatModeProfile classic = CombatModeProfile.CreateClassic();
            CombatModeProfile overdrive = CombatModeProfile.CreateOverdrive();
            bool challenge = ClassicRoundPressure.IsChallengeRound(5) && ClassicRoundPressure.IsChallengeRound(15) &&
                             !ClassicRoundPressure.IsChallengeRound(10) &&
                             ClassicRoundPressure.AppliesTo(classic, false) &&
                             !ClassicRoundPressure.AppliesTo(classic, true) &&
                             !ClassicRoundPressure.AppliesTo(overdrive, false);
            bool hurdle = CommercialRoundPacing.GetNextHurdleRound(0) == CommercialRoundPacing.FirstHurdleRound &&
                          CommercialRoundPacing.GetNextHurdleRound(20) == 30 &&
                          CommercialRoundPacing.TryGetApproachingHurdleIndex(18, out _) &&
                          !CommercialRoundPacing.TryGetApproachingHurdleIndex(17, out _);

            GameObject boardRoot = new GameObject("Pass1EMilestoneBoard");
            DefenseBoardManager board = boardRoot.AddComponent<DefenseBoardManager>();
            List<BoardSlot> slots = new List<BoardSlot>();
            for (int i = 0; i < 15; i++)
            {
                slots.Add(CreateDragBoundsSlot(boardRoot.transform, "Pass1ESlot_" + i, new Vector3(i, 0f, 0f)));
            }

            try
            {
                board.Configure(slots, null);
                bool slotReadout = board.UnlockedSlotCount == 10 &&
                                   board.GetSlotUnlockRound(0) == 0 &&
                                   board.GetSlotUnlockRound(10) == 8 &&
                                   board.GetSlotUnlockRound(11) == 16 &&
                                   board.GetSlotUnlockRound(12) == 24 &&
                                   board.GetSlotUnlockRound(13) == 32 &&
                                   board.GetSlotUnlockRound(14) == 40 &&
                                   board.GetNextSlotUnlockRound(0) == 8;
                int[] completedRounds = { 7, 15, 23, 31, 39, 40 };
                int[] expectedCounts = { 11, 12, 13, 14, 15, 15 };
                for (int i = 0; i < completedRounds.Length; i++)
                {
                    board.RefreshSlotLocks(completedRounds[i]);
                    slotReadout &= board.UnlockedSlotCount == expectedCounts[i];
                }

                int goldBefore = controller != null ? controller.Gold : 0;
                int lifeBefore = controller != null ? controller.Life : 0;
                int boardCountBefore = controller != null ? controller.BoardUnitCount : 0;
                int summonCostBefore = controller != null ? controller.SummonCost : 0;
                UnityEngine.Random.State randomBefore = UnityEngine.Random.state;
                NextRoundMilestone first = NextRoundMilestoneResolver.Resolve(14, classic, 20, 16, 19, 16);
                NextRoundMilestone second = NextRoundMilestoneResolver.Resolve(14, classic, 20, 16, 19, 16);
                bool deterministic = first.nextRound == second.nextRound &&
                                     first.isClassicChallengeRound == second.isClassicChallengeRound &&
                                     first.isApproachingMajorHurdle == second.isApproachingMajorHurdle &&
                                     first.nextHurdleRound == second.nextHurdleRound &&
                                     first.slotUnlockRound == second.slotUnlockRound &&
                                     first.roundsUntilAugment == second.roundsUntilAugment &&
                                     first.roundsUntilRunShop == second.roundsUntilRunShop &&
                                     UnityEngine.Random.state.Equals(randomBefore) &&
                                     (controller == null || (controller.Gold == goldBefore && controller.Life == lifeBefore && controller.BoardUnitCount == boardCountBefore && controller.SummonCost == summonCostBefore));
                NextRoundMilestone boss = NextRoundMilestoneResolver.Resolve(9, classic, 10, 11, 11, 16);
                NextRoundMilestone challengeNext = NextRoundMilestoneResolver.Resolve(14, classic, 20, 16, 19, 16);
                NextRoundMilestone overdriveChallenge = NextRoundMilestoneResolver.Resolve(14, overdrive, 20, 16, 19, 16);
                bool scope = boss.isBossRound && !boss.isClassicChallengeRound && challengeNext.isClassicChallengeRound && !overdriveChallenge.isClassicChallengeRound;
                summary = "challenge=" + challenge + ", hurdle=" + hurdle + ", slots=" + slotReadout + ", readonly=" + deterministic + ", scope=" + scope;
                return challenge && hurdle && slotReadout && deterministic && scope;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(boardRoot);
            }
        }
        private static bool ValidateBoardCapacityPacing(out string summary)
        {
            GameObject boardRoot = new GameObject("BoardCapacityPacingSmokeBoard");
            DefenseBoardManager board = boardRoot.AddComponent<DefenseBoardManager>();
            List<BoardSlot> slots = new List<BoardSlot>();
            for (int i = 0; i < 15; i++)
            {
                slots.Add(CreateDragBoundsSlot(boardRoot.transform, "BoardCapacitySlot_" + i, new Vector3(i, 0f, 0f)));
            }

            try
            {
                board.Configure(slots, null);
                int[] completedRounds = { 0, 6, 7, 14, 15, 22, 23, 30, 31, 38, 39, 99 };
                int[] expectedCounts = { 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15 };
                int[] actualCounts = new int[completedRounds.Length];
                bool valid = true;
                for (int i = 0; i < completedRounds.Length; i++)
                {
                    board.RefreshSlotLocks(completedRounds[i]);
                    actualCounts[i] = board.UnlockedSlotCount;
                    valid &= actualCounts[i] == expectedCounts[i] && actualCounts[i] <= 15;
                }
                summary = "R1=" + actualCounts[0] + ", R7=" + actualCounts[1] + ", R8=" + actualCounts[2] +
                          ", R15=" + actualCounts[3] + ", R16=" + actualCounts[4] + ", R23=" + actualCounts[5] +
                          ", R24=" + actualCounts[6] + ", R31=" + actualCounts[7] + ", R32=" + actualCounts[8] +
                          ", R39=" + actualCounts[9] + ", R40=" + actualCounts[10] + ", R100=" + actualCounts[11];
                return valid;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(boardRoot);
            }
        }

        private static bool ValidateBoardDragBounds()
        {
            const float BackSpacing = 1.2f;
            const float FrontSpacing = BackSpacing * 1.42f;
            const float RowSpacing = 1.28f;
            GameObject parent = new GameObject("DragBoundsParent");
            GameObject boardRoot = new GameObject("DragBoundsBoard");
            parent.transform.position = new Vector3(4f, 0f, -3f);
            parent.transform.rotation = Quaternion.Euler(0f, 23f, 0f);
            parent.transform.localScale = new Vector3(1.2f, 1f, 0.85f);
            boardRoot.transform.SetParent(parent.transform, false);
            DefenseBoardManager board = boardRoot.AddComponent<DefenseBoardManager>();
            List<BoardSlot> slots = new List<BoardSlot>();
            float backWidth = 9f * BackSpacing;
            for (int i = 0; i < 10; i++)
            {
                slots.Add(CreateDragBoundsSlot(boardRoot.transform, "DragBoundsBack_" + i, new Vector3(-backWidth * 0.5f + i * BackSpacing, 0f, -RowSpacing)));
            }
            float frontWidth = 4f * FrontSpacing;
            for (int j = 0; j < 5; j++)
            {
                slots.Add(CreateDragBoundsSlot(boardRoot.transform, "DragBoundsFront_" + j, new Vector3(-frontWidth * 0.5f + j * FrontSpacing, 0f, 0f)));
            }
            try
            {
                const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
                board.Configure(slots, null);
                FieldInfo boundsField = typeof(DefenseBoardManager).GetField("cachedBoardDragBounds", Flags);
                FieldInfo extentField = typeof(DefenseBoardManager).GetField("draggedUnitLocalExtent", Flags);
                MethodInfo clampMethod = typeof(DefenseBoardManager).GetMethod("ClampDragPointToBoard", Flags);
                object bounds = boundsField != null ? boundsField.GetValue(board) : null;
                Type boundsType = bounds != null ? bounds.GetType() : null;
                bool valid = boundsType != null && (bool)boundsType.GetField("isValid").GetValue(bounds);
                float maxZ = valid ? (float)boundsType.GetField("maxZ").GetValue(bounds) : 0f;
                float spacingX = valid ? (float)boundsType.GetField("slotSpacingX").GetValue(bounds) : 0f;
                float spacingZ = valid ? (float)boundsType.GetField("slotSpacingZ").GetValue(bounds) : 0f;
                bool actualLayoutSpacing = Mathf.Abs(spacingX - BackSpacing) <= 0.001f &&
                                           Mathf.Abs(spacingZ - RowSpacing) <= 0.001f;
                BoardSlot[] outerSlots = { slots[0], slots[9], slots[12], slots[14] };
                bool centerReachable = true;
                for (int i = 0; i < outerSlots.Length; i++)
                {
                    Vector3 rawWorld = outerSlots[i].UnitAnchor.position;
                    rawWorld.y = 1.4f;
                    Vector3 clampedWorld = clampMethod != null ? (Vector3)clampMethod.Invoke(board, new object[] { rawWorld }) : rawWorld;
                    Vector3 expectedLocal = board.transform.InverseTransformPoint(rawWorld);
                    Vector3 clampedLocal = board.transform.InverseTransformPoint(clampedWorld);
                    centerReachable &= Mathf.Abs(expectedLocal.x - clampedLocal.x) <= 0.001f &&
                                       Mathf.Abs(expectedLocal.z - clampedLocal.z) <= 0.001f;
                }
                extentField?.SetValue(board, new Vector2(9f, 9f));
                bool largeUnitCenterReachable = true;
                for (int j = 0; j < outerSlots.Length; j++)
                {
                    Vector3 rawWorld = outerSlots[j].UnitAnchor.position;
                    rawWorld.y = 1.4f;
                    Vector3 clampedWorld = clampMethod != null ? (Vector3)clampMethod.Invoke(board, new object[] { rawWorld }) : rawWorld;
                    Vector3 expectedLocal = board.transform.InverseTransformPoint(rawWorld);
                    Vector3 clampedLocal = board.transform.InverseTransformPoint(clampedWorld);
                    largeUnitCenterReachable &= Mathf.Abs(expectedLocal.x - clampedLocal.x) <= 0.001f &&
                                                Mathf.Abs(expectedLocal.z - clampedLocal.z) <= 0.001f;
                }
                return valid && slots[10].IsLocked && maxZ > slots[10].transform.localPosition.z &&
                       actualLayoutSpacing && centerReachable && largeUnitCenterReachable;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        private static BoardSlot CreateDragBoundsSlot(Transform parent, string name, Vector3 localPosition)
        {
            GameObject slotObject = new GameObject(name);
            slotObject.transform.SetParent(parent, false);
            slotObject.transform.localPosition = localPosition;
            return slotObject.AddComponent<BoardSlot>();
        }

        private static bool ValidateDraggedUnitCombatSuspension()
        {
            GameObject probeObject = new GameObject("DragCombatSuspensionProbe");
            DefenderUnit unit = probeObject.AddComponent<DefenderUnit>();
            unit.Initialize(new CharacterDefinition
            {
                id = "drag_combat_suspension_probe",
                displayName = "Drag Combat Suspension Probe",
                stats = new CombatStats { maxHealth = 100f },
                attackBehavior = new AttackBehavior()
            });
            try
            {
                unit.SetBoardDragCombatSuspended(true);
                bool suspended = unit.IsBoardDragCombatSuspended && unit.CanBeCombatTargeted;
                unit.SetBoardDragCombatSuspended(false);
                bool resumed = !unit.IsBoardDragCombatSuspended && unit.CanBeCombatTargeted;
                return suspended && resumed;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probeObject);
            }
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

        private static bool ValidateUltimateMergeInheritanceSeparation(out string summary)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo ultimateMerge = typeof(DefenseBoardManager).GetMethod("ExecuteUltimateMerge", Flags);
            MethodInfo normalMerge = typeof(DefenseBoardManager).GetMethod(nameof(DefenseBoardManager.TryMergeUnitsOfGrade), Flags);
            MethodInfo applyInheritance = typeof(DefenderUnit).GetMethod(nameof(DefenderUnit.ApplyMergeInheritance), Flags);
            bool ultimateCallsInheritance = MethodContainsDirectCall(ultimateMerge, applyInheritance);
            bool normalCallsInheritance = MethodContainsDirectCall(normalMerge, applyInheritance);
            summary = "ultimateCall=" + ultimateCallsInheritance + ", normalCall=" + normalCallsInheritance;
            return ultimateMerge != null && normalMerge != null && applyInheritance != null && !ultimateCallsInheritance && normalCallsInheritance;
        }

        private static bool MethodContainsDirectCall(MethodInfo method, MethodInfo target)
        {
            MethodBody body = method != null ? method.GetMethodBody() : null;
            byte[] il = body != null ? body.GetILAsByteArray() : null;
            if (il == null || target == null)
            {
                return false;
            }

            for (int index = 0; index <= il.Length - 5; index++)
            {
                byte opcode = il[index];
                if (opcode != 0x28 && opcode != 0x6F)
                {
                    continue;
                }

                int metadataToken = BitConverter.ToInt32(il, index + 1);
                if (metadataToken == target.MetadataToken)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ValidateUltimateRecipeUx(DefenseGameController controller, out string summary)
        {
            UltimateRecipeOption[] options = controller != null ? controller.GetAllUltimateRecipeOptions() : Array.Empty<UltimateRecipeOption>();
            UltimateRecipeOption[] relatedOptions = controller != null ? controller.GetRelatedUltimateRecipeOptions() : Array.Empty<UltimateRecipeOption>();
            Dictionary<string, string> expectedResults = new Dictionary<string, string>
            {
                { "Thunder Control Rite", "hero_51" },
                { "Volcanic Core Rite", "hero_52" },
                { "Fever Engine Rite", "hero_53" },
                { "Soul Battery Rite", "hero_54" },
                { "Iron Bastion Rite", "hero_55" },
                { "Clockwork Barrage Rite", "hero_56" },
                { "Fractured Arsenal Rite", "hero_57" }
            };
            string[] removedRecipeNames =
            {
                "Venom Bulwark Rite",
                "Crown Overflow Rite",
                "Eclipse Overflow Rite",
                "Dragon Overflow Rite"
            };
            // Every active recipe is validated against the fixed-result contract so adding a
            // future recipe (for example hero_58) does not require updating this smoke test.
            bool allActiveFixedResultsValid = options.All(option =>
                !string.IsNullOrWhiteSpace(option.resultCharacterId) &&
                option.resultDefinition != null &&
                option.resultDefinition.id == option.resultCharacterId &&
                option.resultDefinition.grade == CharacterGrade.Transcendent &&
                option.resultSummary == option.resultDefinition.displayName);

            // Keep the current seven recipes as explicit regression coverage, without
            // treating this dictionary as the complete list of recipes.
            bool knownRecipeMappingsValid =
                expectedResults.All(pair => options.Any(option => option.recipeName == pair.Key && option.resultCharacterId == pair.Value)) &&
                removedRecipeNames.All(recipeName => options.All(option => option.recipeName != recipeName));
            bool fixedResultDataValid = allActiveFixedResultsValid && knownRecipeMappingsValid;
            bool structuredDataValid = options.Length > 0 && fixedResultDataValid;
            for (int i = 0; i < options.Length; i++)
            {
                UltimateRecipeOption option = options[i];
                int materialProgress = option.materials != null
                    ? option.materials.Sum(material => Mathf.Min(material.ownedCount, material.requiredCount))
                    : -1;
                int missingCount = option.materials != null
                    ? option.materials.Sum(material => Mathf.Max(0, material.requiredCount - material.ownedCount))
                    : -1;
                structuredDataValid &= !string.IsNullOrWhiteSpace(option.recipeName) &&
                                       option.materials != null && option.materials.Length > 0 &&
                                       materialProgress == option.progress &&
                                       missingCount == option.missingMaterialCount &&
                                       (!option.isReady || option.materials.All(material => material.isReady));
                if (i == 0)
                {
                    continue;
                }

                UltimateRecipeOption previous = options[i - 1];
                bool orderValid = previous.isReady || !option.isReady;
                if (previous.isReady == option.isReady)
                {
                    orderValid &= previous.missingMaterialCount <= option.missingMaterialCount;
                    if (previous.missingMaterialCount == option.missingMaterialCount)
                    {
                        int previousNormalized = previous.progress * Mathf.Max(1, option.required);
                        int currentNormalized = option.progress * Mathf.Max(1, previous.required);
                        orderValid &= previousNormalized >= currentNormalized;
                        if (previousNormalized == currentNormalized)
                        {
                            orderValid &= previous.definitionOrder <= option.definitionOrder;
                        }
                    }
                }
                structuredDataValid &= orderValid;
            }

            bool relatedFilterValid = relatedOptions.All(option =>
                option.isReady ||
                option.progress > 0 ||
                (option.materials != null && option.materials.Sum(material => material.ownedCount) > 0));
            bool hiddenZeroProgressValid = options
                .Where(option => !option.isReady && option.progress <= 0 && (option.materials == null || option.materials.Sum(material => material.ownedCount) <= 0))
                .All(option => relatedOptions.All(related => related.recipeName != option.recipeName));

            RectTransform detailPanel = UnityEngine.Object.FindObjectsOfType<RectTransform>(true)
                .FirstOrDefault(rect => rect != null && rect.name == "UltimateRecipeDetailPanel");
            RectTransform optionContent = UnityEngine.Object.FindObjectsOfType<RectTransform>(true)
                .FirstOrDefault(rect => rect != null && rect.name == "UltimateRecipeOptionContent");
            Button optionTemplate = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(button => button != null && button.name == "UltimateRecipeOptionTemplate");
            RectTransform materialContent = UnityEngine.Object.FindObjectsOfType<RectTransform>(true)
                .FirstOrDefault(rect => rect != null && rect.name == "UltimateRecipeMaterialContent");
            Image materialTemplate = UnityEngine.Object.FindObjectsOfType<Image>(true)
                .FirstOrDefault(image => image != null && image.name == "UltimateRecipeMaterialTemplate");
            Image resultPortrait = UnityEngine.Object.FindObjectsOfType<Image>(true)
                .FirstOrDefault(image => image != null && image.name == "ResultPortrait" && image.transform.parent != null && image.transform.parent.name == "ResultCard");
            Button confirmButton = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(button => button != null && button.name == "UltimateRecipeConfirmButton");
            UltimateRecipeSelectionUI selection = UnityEngine.Object.FindObjectOfType<UltimateRecipeSelectionUI>(true);
            bool layoutValid = detailPanel != null && optionContent != null && optionTemplate != null && materialContent != null && materialTemplate != null && resultPortrait != null && confirmButton != null && selection != null;
            summary = "all=" + options.Length + ", fixedResults=" + fixedResultDataValid + ", related=" + relatedOptions.Length + ", relatedFilter=" + relatedFilterValid + ", zeroHidden=" + hiddenZeroProgressValid + ", dynamicOptionContent=" + (optionContent != null) + ", dynamicMaterialContent=" + (materialContent != null) + ", selection=" + (selection != null) + ", confirm=" + (confirmButton != null);
            return structuredDataValid && relatedFilterValid && hiddenZeroProgressValid && layoutValid;
        }

        private static bool ValidateInitialPreparationBattleStartUiPath(DefenseGameController controller, out string summary)
        {
            RoundManager rounds = UnityEngine.Object.FindObjectOfType<RoundManager>();
            Button lobbyEntryButton = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(button => button != null && button.name == "LobbyBattleButton");
            Button battleButton = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(button => button != null && button.name == "BattleButton");
            Button lobbyNavigationButton = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(button => button != null && button.name == "OutgameNavLobby");
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo currentRoundField = typeof(RoundManager).GetField("<CurrentRound>k__BackingField", flags);
            MethodInfo requestForecast = typeof(DefenseGameController).GetMethod("RequestBossForecastBetIfNeeded", flags);
            MethodInfo notifyStateChanged = typeof(DefenseGameController).GetMethod("NotifyStateChanged", flags);
            if (controller == null || rounds == null || lobbyEntryButton == null || battleButton == null || lobbyNavigationButton == null || currentRoundField == null || requestForecast == null || notifyStateChanged == null)
            {
                summary = "ui_or_reflection_target_missing";
                return false;
            }

            try
            {
                controller.ResetRunForRetry();
                lobbyNavigationButton.onClick.Invoke();
                bool lobbyHiddenHud = controller.BoardUnitCount == 0 &&
                                      !controller.IsRoundRunning &&
                                      !battleButton.gameObject.activeInHierarchy;

                lobbyEntryButton.onClick.Invoke();
                bool zeroSummonReady = controller.BoardUnitCount == 0 &&
                                       !controller.IsRoundRunning &&
                                       controller.BlockingChoiceReason == "None" &&
                                       battleButton.gameObject.activeInHierarchy &&
                                       battleButton.interactable;
                battleButton.onClick.Invoke();
                bool zeroSummonStarts = zeroSummonReady && controller.IsRoundRunning && controller.BoardUnitCount == 0;

                controller.ResetRunForRetry();
                lobbyNavigationButton.onClick.Invoke();
                lobbyEntryButton.onClick.Invoke();
                bool normalSummonGranted = controller.TrySummon();
                bool normalSummonReady = normalSummonGranted && battleButton.interactable;
                battleButton.onClick.Invoke();
                bool normalSummonStarts = normalSummonReady && controller.IsRoundRunning && controller.BoardUnitCount > 0;

                controller.ResetRunForRetry();
                lobbyNavigationButton.onClick.Invoke();
                lobbyEntryButton.onClick.Invoke();
                bool retryZeroSummonReady = controller.BoardUnitCount == 0 &&
                                            !controller.IsRoundRunning &&
                                            controller.BlockingChoiceReason == "None" &&
                                            battleButton.interactable;
                battleButton.onClick.Invoke();
                bool retryZeroSummonStarts = retryZeroSummonReady && controller.IsRoundRunning && controller.BoardUnitCount == 0;

                controller.ResetRunForRetry();
                lobbyNavigationButton.onClick.Invoke();
                lobbyEntryButton.onClick.Invoke();
                currentRoundField.SetValue(rounds, 3);
                requestForecast.Invoke(controller, new object[] { 3 });
                // Production round completion raises the choice request before its final
                // state refresh. Reproduce that completed lifecycle before checking the HUD.
                notifyStateChanged.Invoke(controller, null);
                bool blockingChoicePreventsStart = controller.BlockingChoiceReason == "BossForecast" &&
                                                   !battleButton.interactable;
                battleButton.onClick.Invoke();
                blockingChoicePreventsStart &= !controller.IsRoundRunning;

                summary = "lobbyHudHidden=" + lobbyHiddenHud +
                          ", zeroReady=" + zeroSummonReady +
                          ", zeroStarts=" + zeroSummonStarts +
                          ", normalStarts=" + normalSummonStarts +
                          ", retryZeroStarts=" + retryZeroSummonStarts +
                          ", choiceBlocks=" + blockingChoicePreventsStart;
                return lobbyHiddenHud && zeroSummonStarts && normalSummonStarts &&
                       retryZeroSummonStarts && blockingChoicePreventsStart;
            }
            finally
            {
                controller.ResetRunForRetry();
                lobbyNavigationButton.onClick.Invoke();
            }
        }
        private static bool ValidatePass2BPreparationSkip(DefenseGameController controller, out string summary)
        {
            RoundManager rounds = UnityEngine.Object.FindObjectOfType<RoundManager>();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo currentRoundField = typeof(RoundManager).GetField("<CurrentRound>k__BackingField", flags);
            MethodInfo requestForecast = typeof(DefenseGameController).GetMethod("RequestBossForecastBetIfNeeded", flags);
            MethodInfo notifyStateChanged = typeof(DefenseGameController).GetMethod("NotifyStateChanged", flags);
            if (controller == null || rounds == null || currentRoundField == null || requestForecast == null)
            {
                summary = "reflection_target_missing";
                return false;
            }

            try
            {
                controller.ResetRunForRetry();
                bool zeroSummonStarts = controller.BoardUnitCount == 0 &&
                                       controller.BlockingChoiceReason == "None";
                controller.StartRound();
                zeroSummonStarts &= controller.IsRoundRunning;

                rounds.CompleteCurrentRoundForDebug();
                bool laterNoActionStarts = !controller.IsRoundRunning && controller.BlockingChoiceReason == "None";
                controller.StartRound();
                laterNoActionStarts &= controller.IsRoundRunning;

                controller.ResetRunForRetry();
                bool initialSummon = controller.TrySummon();
                controller.StartRound();
                rounds.CompleteCurrentRoundForDebug();
                CharacterGrade upgradedGrade = new[]
                {
                    CharacterGrade.Normal, CharacterGrade.Rare, CharacterGrade.Epic,
                    CharacterGrade.Legendary, CharacterGrade.Mythic, CharacterGrade.Transcendent
                }.FirstOrDefault(grade => controller.CountUnitsOfGrade(grade) > 0);
                bool upgraded = initialSummon && controller.TryUpgradeGrade(upgradedGrade);
                controller.StartRound();
                bool upgradeOnlyStarts = upgraded && controller.IsRoundRunning;

                controller.ResetRunForRetry();
                currentRoundField.SetValue(rounds, 3);
                requestForecast.Invoke(controller, new object[] { 3 });
                controller.StartRound();
                bool realChoiceBlocks = !controller.IsRoundRunning && controller.BlockingChoiceReason == "BossForecast";
                bool choiceResolved = controller.TryChooseBossForecastBet(BossForecastBet.Supply);
                controller.StartRound();
                bool resolvedChoiceStarts = choiceResolved && controller.IsRoundRunning;

                summary = "zero=" + zeroSummonStarts + ", later=" + laterNoActionStarts +
                          ", upgrade=" + upgradeOnlyStarts + ", blocks=" + realChoiceBlocks +
                          ", resolved=" + resolvedChoiceStarts;
                return zeroSummonStarts && laterNoActionStarts && upgradeOnlyStarts &&
                       realChoiceBlocks && resolvedChoiceStarts;
            }
            finally
            {
                controller.ResetRunForRetry();
            }
        }
        private static bool ValidateBossForecastTimingAndShopBias(DefenseGameController controller, out string summary)
        {
            RunShopSystem shop = UnityEngine.Object.FindObjectOfType<RunShopSystem>();
            RoundManager roundManager = UnityEngine.Object.FindObjectOfType<RoundManager>();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo roundField = typeof(RoundManager).GetField("<CurrentRound>k__BackingField", flags);
            MethodInfo request = typeof(DefenseGameController).GetMethod("RequestBossForecastBetIfNeeded", flags);
            MethodInfo buildOffers = typeof(RunShopSystem).GetMethod("BuildOffers", flags);
            FieldInfo dailyField = typeof(DefenseGameController).GetField("dailyFateCupEnabled", flags);
            if (controller == null || shop == null || roundManager == null || roundField == null || request == null || buildOffers == null || dailyField == null)
            {
                summary = "reflection_target_missing";
                return false;
            }

            int originalRound = (int)roundField.GetValue(roundManager);
            bool originalDaily = (bool)dailyField.GetValue(controller);
            int requests = 0;
            Action requestHandler = () => requests++;
            controller.OnBossForecastBetRequested += requestHandler;
            try
            {
                roundField.SetValue(roundManager, 1);
                request.Invoke(controller, new object[] { 1 });
                bool noEarlyRequest = requests == 0 && !controller.CanChooseBossForecastBet &&
                                      !DefenseGameController.IsBossForecastPreparationRound(1, false) &&
                                      !DefenseGameController.IsBossForecastPreparationRound(3, true);

                roundField.SetValue(roundManager, 3);
                request.Invoke(controller, new object[] { 3 });
                request.Invoke(controller, new object[] { 3 });
                bool requestedOnceAtPreparation = requests == 1 && controller.CanChooseBossForecastBet;
                bool choiceApplied = controller.TryChooseBossForecastBet(BossForecastBet.Supply) &&
                                     controller.BossForecastPreferredShopRoleIndex == 0;

                dailyField.SetValue(controller, true);
                buildOffers.Invoke(shop, new object[] { 4, false, true, false, false });
                bool recoveryPreserved = controller.BossForecastPreferredShopRoleIndex == 0;
                buildOffers.Invoke(shop, new object[] { 11, true, false, false, true });
                bool firstEligibleConsumed = controller.BossForecastPreferredShopRoleIndex < 0;
                buildOffers.Invoke(shop, new object[] { 19, true, false, false, true });
                bool laterShopUnbiased = controller.BossForecastPreferredShopRoleIndex < 0;

                summary = "early=" + noEarlyRequest + ", r4=" + requestedOnceAtPreparation + ", choice=" + choiceApplied + ", recovery=" + recoveryPreserved + ", consumed=" + firstEligibleConsumed + ", later=" + laterShopUnbiased;
                return noEarlyRequest && requestedOnceAtPreparation && choiceApplied && recoveryPreserved && firstEligibleConsumed && laterShopUnbiased;
            }
            finally
            {
                controller.OnBossForecastBetRequested -= requestHandler;
                dailyField.SetValue(controller, originalDaily);
                roundField.SetValue(roundManager, originalRound);
                controller.ResetRunForRetry();
            }
        }

        private static bool ValidateChoiceSchedule(out string summary)
        {
            RunShopSystem shop = UnityEngine.Object.FindObjectOfType<RunShopSystem>();
            AugmentManager augments = UnityEngine.Object.FindObjectOfType<AugmentManager>();
            CombatModeProfile overdrive = CombatModeProfile.CreateOverdrive();
            bool miniSchedule = shop != null && !shop.IsScheduledMiniShopRound(3) && shop.IsScheduledMiniShopRound(11) && shop.IsScheduledMiniShopRound(19) && shop.IsScheduledMiniShopRound(27);
            bool classicSchedule = augments != null && augments.GetNextScheduledChoiceRoundAfterSelection(6) == 11 && augments.GetNextScheduledChoiceRoundAfterSelection(11) == 16;
            bool overdriveSchedule = overdrive.firstAugmentChoiceRound == 6 && overdrive.augmentChoiceInterval == 4;
            summary = "mini=" + miniSchedule + ", classic=R6/R11/R16:" + classicSchedule + ", overdrive=R6/+4:" + overdriveSchedule;
            return miniSchedule && classicSchedule && overdriveSchedule;
        }

        private static bool ValidateRecipePacingTelemetry(DefenseGameController controller, out string summary)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo report = typeof(DefenseGameController).GetMethod("ReportTranscendentRecipePacing", flags);
            if (controller == null || report == null)
            {
                summary = "telemetry_method_missing";
                return false;
            }

            int gold = controller.Gold;
            int life = controller.Life;
            int board = controller.BoardUnitCount;
            int summonCost = controller.SummonCost;
            string randomBefore = JsonUtility.ToJson(UnityEngine.Random.state);
            int snapshotCount = controller.TranscendentRecipePacingSnapshotCount;
            report.Invoke(controller, new object[] { 10 });
            report.Invoke(controller, new object[] { 15 });
            report.Invoke(controller, new object[] { 20 });
            string randomAfter = JsonUtility.ToJson(UnityEngine.Random.state);
            bool stateUntouched = controller.Gold == gold && controller.Life == life && controller.BoardUnitCount == board && controller.SummonCost == summonCost && randomBefore == randomAfter;
            bool snapshotsValid = controller.TranscendentRecipePacingSnapshotCount == snapshotCount + 3 &&
                                  controller.LastTranscendentRecipePacingSnapshot.Contains("[DG_RECIPE_PACING]") &&
                                  controller.LastTranscendentRecipePacingSnapshot.Contains("Round=20") &&
                                  controller.LastTranscendentRecipePacingSnapshot.Contains("Best=");
            summary = "state=" + stateUntouched + ", snapshots=" + snapshotsValid;
            return stateUntouched && snapshotsValid && DefenseGameController.IsTranscendentRecipePacingSnapshotRound(10) && DefenseGameController.IsTranscendentRecipePacingSnapshotRound(15) && DefenseGameController.IsTranscendentRecipePacingSnapshotRound(20) && !DefenseGameController.IsTranscendentRecipePacingSnapshotRound(11);
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
            FieldInfo dailyField = typeof(DefenseGameController).GetField("dailyFateCupEnabled", instanceFlags);
            FieldInfo recentHistoryField = typeof(RunShopSystem).GetField("recentOfferHistory", instanceFlags);
            if (buildOffers == null || offersField == null || goldField == null || summonCostField == null || dailyField == null || recentHistoryField == null)
            {
                summary = "reflection_target_missing";
                return false;
            }

            object originalGold = goldField.GetValue(controller);
            object originalSummonCost = summonCostField.GetValue(controller);
            object originalDaily = dailyField.GetValue(controller);
            FieldInfo contentSeedOverrideEnabledField = typeof(DefenseGameController).GetField("runContentSeedOverrideEnabled", instanceFlags);
            FieldInfo contentSeedOverrideField = typeof(DefenseGameController).GetField("runContentSeedOverride", instanceFlags);
            if (contentSeedOverrideEnabledField == null || contentSeedOverrideField == null)
            {
                summary = "content_seed_override_reflection_missing";
                return false;
            }
            object originalContentSeedOverrideEnabled = contentSeedOverrideEnabledField.GetValue(controller);
            object originalContentSeedOverride = contentSeedOverrideField.GetValue(controller);
            UnityEngine.Random.State originalRandomState = UnityEngine.Random.state;
            IList offers = null;
            try
            {
                dailyField.SetValue(controller, true);
                controller.SetRunContentSeedOverride(731903);
                controller.ResetRunForRetry();
                object recentHistory = recentHistoryField.GetValue(shop);
                recentHistory?.GetType().GetMethod("Clear")?.Invoke(recentHistory, null);
                goldField.SetValue(controller, 34);
                summonCostField.SetValue(controller, 16);
                buildOffers.Invoke(shop, new object[] { 11, true, false, false, false });
                offers = offersField.GetValue(shop) as IList;
                if (offers == null || offers.Count != 3)
                {
                    summary = "offer_count=" + (offers != null ? offers.Count : -1);
                    return false;
                }
                List<string> snapshots = new List<string>();
                Dictionary<string, int> firstPrices = new Dictionary<string, int>();
                bool fixedPricesValid = true;
                bool couponDurationValid = true;
                for (int i = 0; i < offers.Count; i++)
                {
                    object offer = offers[i];
                    Type offerType = offer.GetType();
                    string typeName = offerType.GetField("type", instanceFlags)?.GetValue(offer)?.ToString() ?? string.Empty;
                    string title = offerType.GetField("title", instanceFlags)?.GetValue(offer) as string ?? string.Empty;
                    string description = offerType.GetField("description", instanceFlags)?.GetValue(offer) as string ?? string.Empty;
                    int cost = (int)(offerType.GetField("cost", instanceFlags)?.GetValue(offer) ?? int.MaxValue);
                    fixedPricesValid &= !string.IsNullOrWhiteSpace(typeName) && !string.IsNullOrWhiteSpace(title) && cost >= 6 && cost <= 120;
                    firstPrices[typeName] = cost;
                    if (typeName == "Coupon")
                    {
                        couponDurationValid &= title.Contains("4라운드") && description.Contains("18%");
                    }
                    snapshots.Add(typeName + "=" + cost + "G");
                }

                recentHistory?.GetType().GetMethod("Clear")?.Invoke(recentHistory, null);
                controller.ResetRunForRetry();
                goldField.SetValue(controller, 1);
                summonCostField.SetValue(controller, 60);
                buildOffers.Invoke(shop, new object[] { 11, true, false, false, false });
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
                return firstPrices.Count == 3 && fixedPricesValid && pricesInvariant && couponDurationValid;
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
                dailyField.SetValue(controller, originalDaily);
                contentSeedOverrideEnabledField.SetValue(controller, originalContentSeedOverrideEnabled);
                contentSeedOverrideField.SetValue(controller, originalContentSeedOverride);
                controller.ResetRunForRetry();
                UnityEngine.Random.state = originalRandomState;
            }
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.001f;
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
            public bool runResetStateValid;
            public bool runSeedRepeatValid;
            public bool runContentChannelIsolationValid;
            public bool fateEntryLayoutValid;
            public bool fateEntryPastelColorValid;
            public bool fateEntryIdleAtFullHealth;
            public bool summonHudReadable;
            public bool initialPreparationFlowValid;
            public bool initialPreparationBattleStartUiPathValid;
            public string initialPreparationBattleStartUiPathSummary;
            public bool runtimeStageLifecycleValid;
            public bool inventoryStageHidden;
            public bool dailyFateCupUiValid;
            public bool bossForecastUiValid;
            public bool bossForecastTimingValid;
            public string bossForecastTimingSummary;
            public bool firstBossPreparationRewardRulesValid;
            public bool resultRewardIconsValid;
            public bool rankingPageValid;
            public bool yahtzeeModeUiValid;
            public bool yahtzeeMultiplierLogicValid;
            public bool yahtzeeTicketMilestoneLogicValid;
            public bool yahtzeeTicketRunAccumulationValid;
            public bool yahtzeeTicketNewRunResetValid;
            public bool playerDirectSummonIsolationValid;
            public bool tacticalMissionRiskRewardValid;
            public bool tacticalMissionChoiceValid;
            public bool roundDiamondRewardValid;
            public bool bannerBurstQueueValid;
            public bool earlyMiniShopChoicesValid;
            public string earlyMiniShopSummary;
            public bool choiceScheduleValid;
            public string choiceScheduleSummary;
            public bool recipePacingTelemetryValid;
            public string recipePacingTelemetrySummary;
            public bool ultimateRecipeUxValid;
            public string ultimateRecipeUxSummary;
            public bool diceAutoCycleValid;
            public bool thunderControlDamageConversionValid;
            public bool feverEngineApexDpsValid;
            public bool ultimateMergeInheritanceIsolationValid;
            public bool simultaneousDeathPolicyValid;
            public bool hero32SignatureValid;
            public bool gargoyleLoopDurationValid;
            public bool longCombatAccelerationValid;
            public bool retryTimeScaleResetValid;
            public string retryTimeScaleResetSummary;
            public bool choiceReadabilityValid;
            public string choiceReadabilitySummary;
            public bool defaultVfxConfigured;
            public bool animationMaterialEventsValid;
            public string animationMaterialEventsSummary;
            public bool boardCapacityPacingValid;
            public string boardCapacityPacingSummary;
            public bool gradeUpgradeBarUiValid;
            public bool pass2MSummonGradeLuckValid;
            public bool pass2NCombatEconomyUiValid;
            public bool pass2OCombatHudLayoutValid;
            public bool pass1EMilestoneValid;
            public string pass1EMilestoneSummary;
            public bool pass2BPreparationSkipValid;
            public string pass2BPreparationSkipSummary;
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
