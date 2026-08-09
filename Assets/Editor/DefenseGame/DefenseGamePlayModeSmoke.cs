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
            if (controller != null)
            {
                int expectedBaseMaxLife = controller.MaxLife;
                controller.IncreaseMaxLife(3);
                controller.ResetRunForRetry();
                runResetStateValid = controller.MaxLife == expectedBaseMaxLife && controller.Life == expectedBaseMaxLife;

                controller.SetRunContentSeedOverride(314159);
                controller.ResetRunForRetry();
                int firstSeedSample = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                controller.ResetRunForRetry();
                int secondSeedSample = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                runSeedRepeatValid = firstSeedSample == secondSeedSample;
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
                                        Approximately(fateEntryRect.sizeDelta, new Vector2(250f, 84f)) &&
                                        Approximately(fateEntryRect.anchoredPosition, new Vector2(-80f, 356f)) &&
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
            TacticalMissionSystem tacticalMissionSystem = UnityEngine.Object.FindObjectOfType<TacticalMissionSystem>();
            bool tacticalMissionRiskRewardValid = tacticalMissionSystem != null &&
                                                  tacticalMissionSystem.HasInitialStrategyFork &&
                                                  TacticalMissionSystem.IsLastStandGambitConditionMet(7, 2, 2, 3) &&
                                                  !TacticalMissionSystem.IsLastStandGambitConditionMet(0, 0, 0, 1) &&
                                                  !TacticalMissionSystem.IsLastStandGambitConditionMet(7, 3, 2, 2) &&
                                                  !TacticalMissionSystem.IsLastStandGambitConditionMet(7, 2, 2, 4);
            if (!tacticalMissionRiskRewardValid)
            {
                notes.Add("전술 미션 초기 전략 분기 또는 배수의 굴림 조건(HP 7 포함/HP 0·3회 소환 실패) 검증에 실패했습니다.");
            }

            DefenseBoardManager missionBoard = UnityEngine.Object.FindObjectOfType<DefenseBoardManager>();
            DefenderUnit[] unitsBeforeSupport = missionBoard != null ? missionBoard.GetAliveDefenders() : Array.Empty<DefenderUnit>();
            int supportGoldBefore = controller != null ? controller.Gold : -1;
            int supportSummonCostBefore = controller != null ? controller.SummonCost : -1;
            int supportSummonEvents = 0;
            Action<CharacterDefinition> supportSummonHandler = definition => supportSummonEvents++;
            if (controller != null)
            {
                controller.OnUnitSummoned += supportSummonHandler;
            }
            bool supportGranted = stageVisibleInPreparation && controller != null && controller.EmptySlotCount > 0 && controller.TryGrantMissionSupportUnit();
            if (controller != null)
            {
                controller.OnUnitSummoned -= supportSummonHandler;
            }
            DefenderUnit[] unitsAfterSupport = missionBoard != null ? missionBoard.GetAliveDefenders() : Array.Empty<DefenderUnit>();
            DefenderUnit supportUnit = unitsAfterSupport.FirstOrDefault(unit => unit != null && !unitsBeforeSupport.Contains(unit));
            bool missionSupportUnitIsolationValid = supportGranted &&
                                                    controller != null &&
                                                    controller.Gold == supportGoldBefore &&
                                                    controller.SummonCost == supportSummonCostBefore &&
                                                    supportSummonEvents == 0 &&
                                                    supportUnit != null &&
                                                    supportUnit.Grade != CharacterGrade.Transcendent;
            if (!missionSupportUnitIsolationValid)
            {
                notes.Add("미션 지원 유닛이 일반 확률 풀/비용·소환 추적 분리 규칙을 지키지 않습니다.");
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
                                       controller.CanChooseBossForecastBet &&
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
            if (!earlyMiniShopChoicesValid)
            {
                notes.Add("R3 소형 전투상점의 3개 선택지 분류/가격 검증에 실패했습니다. " + earlyMiniShopSummary);
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
                                        presentation.defaultAreaEffectPrefab != null;
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
                RoundManager.ResolveCombatTimeScaleMultiplier(29.99f) == 1 &&
                RoundManager.ResolveCombatTimeScaleMultiplier(30f) == 2 &&
                RoundManager.ResolveCombatTimeScaleMultiplier(35f) == 3 &&
                RoundManager.ResolveCombatTimeScaleMultiplier(45f) == 5 &&
                RoundManager.ResolveCombatTimeScaleMultiplier(90f) == 10;
            if (!longCombatAccelerationValid)
            {
                notes.Add("장기전 가속 단계가 30초 2배, 이후 5초마다 1배 증가 규칙과 일치하지 않습니다.");
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

            bool passed = safeAreaExists && safeAreaAnchorsValid && portraitProfilesValid && hpTen && hpTextTen && runResetStateValid && runSeedRepeatValid && simultaneousDeathPolicyValid && fateEntryLayoutValid && fateEntryPastelColorValid && fateEntryIdleAtFullHealth && summonHudReadable && initialPreparationFlowValid && runtimeStageLifecycleValid && inventoryStageHidden && dailyFateCupUiValid && bossForecastUiValid && outgameShopValid && resultRewardIconsValid && rankingPageValid && yahtzeeModeUiValid && yahtzeeMultiplierLogicValid && yahtzeeTicketMilestoneLogicValid && yahtzeeTicketRunAccumulationValid && yahtzeeTicketNewRunResetValid && tacticalMissionRiskRewardValid && missionSupportUnitIsolationValid && earlyMiniShopChoicesValid && ultimateRecipeUxValid && ultimateMergeInheritanceIsolationValid && diceAutoCycleValid && thunderControlDamageConversionValid && feverEngineApexDpsValid && hero32SignatureValid && gargoyleLoopDurationValid && longCombatAccelerationValid && defaultVfxConfigured && animationMaterialEventsValid && runtimeErrors == 0;
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
                fateEntryLayoutValid = fateEntryLayoutValid,
                fateEntryPastelColorValid = fateEntryPastelColorValid,
                fateEntryIdleAtFullHealth = fateEntryIdleAtFullHealth,
                summonHudReadable = summonHudReadable,
                initialPreparationFlowValid = initialPreparationFlowValid,
                runtimeStageLifecycleValid = runtimeStageLifecycleValid,
                inventoryStageHidden = inventoryStageHidden,
                dailyFateCupUiValid = dailyFateCupUiValid,
                bossForecastUiValid = bossForecastUiValid,
                resultRewardIconsValid = resultRewardIconsValid,
                rankingPageValid = rankingPageValid,
                yahtzeeModeUiValid = yahtzeeModeUiValid,
                yahtzeeMultiplierLogicValid = yahtzeeMultiplierLogicValid,
                yahtzeeTicketMilestoneLogicValid = yahtzeeTicketMilestoneLogicValid,
                yahtzeeTicketRunAccumulationValid = yahtzeeTicketRunAccumulationValid,
                yahtzeeTicketNewRunResetValid = yahtzeeTicketNewRunResetValid,
                tacticalMissionRiskRewardValid = tacticalMissionRiskRewardValid,
                missionSupportUnitIsolationValid = missionSupportUnitIsolationValid,
                earlyMiniShopChoicesValid = earlyMiniShopChoicesValid,
                earlyMiniShopSummary = earlyMiniShopSummary,
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
                defaultVfxConfigured = defaultVfxConfigured,
                animationMaterialEventsValid = animationMaterialEventsValid,
                animationMaterialEventsSummary = animationMaterialEventsSummary,
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
            UnityEngine.Random.State originalRandomState = UnityEngine.Random.state;
            IList offers = null;
            try
            {
                dailyField.SetValue(controller, true);
                object recentHistory = recentHistoryField.GetValue(shop);
                recentHistory?.GetType().GetMethod("Clear")?.Invoke(recentHistory, null);
                UnityEngine.Random.InitState(731903);
                goldField.SetValue(controller, 34);
                summonCostField.SetValue(controller, 16);
                buildOffers.Invoke(shop, new object[] { 3, true, false, false });
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
                    fixedPricesValid &= !string.IsNullOrWhiteSpace(typeName) && !string.IsNullOrWhiteSpace(title) && cost >= 6 && cost <= 24;
                    firstPrices[typeName] = cost;
                    if (typeName == "Coupon")
                    {
                        couponDurationValid &= title.Contains("4라운드") && description.Contains("18%");
                    }
                    snapshots.Add(typeName + "=" + cost + "G");
                }

                recentHistory?.GetType().GetMethod("Clear")?.Invoke(recentHistory, null);
                UnityEngine.Random.InitState(731903);
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
                UnityEngine.Random.state = originalRandomState;
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
            public bool runResetStateValid;
            public bool runSeedRepeatValid;
            public bool fateEntryLayoutValid;
            public bool fateEntryPastelColorValid;
            public bool fateEntryIdleAtFullHealth;
            public bool summonHudReadable;
            public bool initialPreparationFlowValid;
            public bool runtimeStageLifecycleValid;
            public bool inventoryStageHidden;
            public bool dailyFateCupUiValid;
            public bool bossForecastUiValid;
            public bool resultRewardIconsValid;
            public bool rankingPageValid;
            public bool yahtzeeModeUiValid;
            public bool yahtzeeMultiplierLogicValid;
            public bool yahtzeeTicketMilestoneLogicValid;
            public bool yahtzeeTicketRunAccumulationValid;
            public bool yahtzeeTicketNewRunResetValid;
            public bool tacticalMissionRiskRewardValid;
            public bool missionSupportUnitIsolationValid;
            public bool earlyMiniShopChoicesValid;
            public string earlyMiniShopSummary;
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
            public bool defaultVfxConfigured;
            public bool animationMaterialEventsValid;
            public string animationMaterialEventsSummary;
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
