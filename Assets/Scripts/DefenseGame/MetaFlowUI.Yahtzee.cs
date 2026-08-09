using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
    public partial class MetaFlowUI
    {
        private YahtzeeProgressionSystem yahtzeeProgression;
        private GameObject yahtzeeOverlay;
        private Text yahtzeeTicketText, yahtzeeGoldText, yahtzeeDiamondText;
        private Text yahtzeeMultiplierText, yahtzeeSessionText, yahtzeeStatusText;
        private Text yahtzeeChestCountText, yahtzeeRewardSummaryText;
        private Button yahtzeeStartButton, yahtzeeHoldButton, yahtzeeRerollButton, yahtzeeConfirmButton;
        private Button yahtzeeOpenOneButton, yahtzeeOpenTenButton, yahtzeeOpenAllButton;
        private Image yahtzeeRewardPanel;
        private readonly Button[] yahtzeeDiceButtons = new Button[3];
        private readonly Image[] yahtzeeDiceFaces = new Image[3];
        private readonly Text[] yahtzeeDiceStateTexts = new Text[3];
        private readonly Vector2[] yahtzeeDiceBasePositions = new Vector2[3];
        private readonly Image[] yahtzeeRewardCards = new Image[3];
        private readonly Image[] yahtzeeRewardIcons = new Image[3];
        private readonly Text[] yahtzeeRewardTexts = new Text[3];

        private void BuildYahtzeeOverlay(Transform parent)
        {
            yahtzeeOverlay = CreateOverlayRoot(parent, "YahtzeeOverlay", Color.clear);
            // This is a full outgame page. Its root deliberately receives taps so
            // empty page space cannot pass clicks to the lobby battle button below.
            Image yahtzeeBlocker = yahtzeeOverlay.GetComponent<Image>();
            yahtzeeBlocker.raycastTarget = true;
            RollRollUiResource.TryApplySprite(yahtzeeBlocker, "Common/background", Image.Type.Simple, false);
            yahtzeeBlocker.color = Color.white;
            Image modal = CreatePanel(yahtzeeOverlay.transform, "YahtzeeModal", new Vector2(0f, 76f), new Vector2(0f, -152f), Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), false, false);
            RollRollUiResource.TryApplySprite(modal, "Common/background", Image.Type.Simple, false);
            modal.color = Color.white;

            Image header = CreatePanel(modal.transform, "YahtzeeHeader", new Vector2(0f, -90f), new Vector2(900f, 112f), new Color(0.98f, 0.62f, 0.16f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            CreateShopArtwork(header.transform, "YahtzeeHeaderIcon", "Icons/icon-main-menu-roll-activated", new Vector2(34f, -26f), new Vector2(64f, 64f), Color.white, new Vector2(0f, 1f));
            CreateText(header.transform, "YahtzeeTitle", "얏찌", Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(106f, -24f), new Vector2(150f, 66f), 39, TextAnchor.MiddleLeft, true);
            BuildYahtzeeCurrencyChip(header.transform, "TicketChip", "GradeAndGoodsIcons/icon-mode-ticket", new Vector2(-110f, -26f), out yahtzeeTicketText, HandleYahtzeeTicketTopUp);
            BuildYahtzeeCurrencyChip(header.transform, "GoldChip", "Icons/goods_icon_gold", new Vector2(80f, -26f), out yahtzeeGoldText, HandleYahtzeeGoldTopUp);
            BuildYahtzeeCurrencyChip(header.transform, "DiamondChip", "Icons/goods_icon_ruby", new Vector2(270f, -26f), out yahtzeeDiamondText, HandleYahtzeeDiamondTopUp);

            Image rulePanel = CreatePanel(modal.transform, "YahtzeeRulePanel", new Vector2(0f, -226f), new Vector2(900f, 170f), new Color(0.08f, 0.13f, 0.35f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            CreateText(rulePanel.transform, "YahtzeeRuleTitle", "같은 숫자 3개를 만들어 보상 배수를 확정하세요", new Color(1f, 0.86f, 0.30f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(820f, 48f), 28, TextAnchor.MiddleCenter, true);
            CreateText(rulePanel.transform, "YahtzeeRuleBody", "트리플 1·2·3·4·5·6 = 보상 x1·x2·x3·x4·x5·x6  |  비트리플 x1\n재굴림 100 GOLD · 홀드 1차 20 DIA / 2차 50 DIA · 천장/비용 상승 없음", new Color(0.82f, 0.91f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(830f, 78f), 19, TextAnchor.MiddleCenter, false);

            BuildYahtzeePlayPanel(modal.transform);
            BuildYahtzeeChestPanel(modal.transform);
            BuildYahtzeeRewardPanel(modal.transform);
            yahtzeeStatusText = CreateText(modal.transform, "YahtzeeStatusText", "티켓 1장으로 시작하고 6·6·6을 노려보세요.", new Color(1f, 0.90f, 0.48f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 166f), new Vector2(900f, 42f), 19, TextAnchor.MiddleCenter, true);
            RefreshYahtzee();
        }

        private void BuildYahtzeeCurrencyChip(Transform parent, string name, string iconPath, Vector2 position, out Text valueText, UnityEngine.Events.UnityAction plusAction)
        {
            // Keep the same dark rounded currency-chip treatment as the shop header.
            Image chip = CreatePanel(parent, name, position, new Vector2(184f, 60f), new Color(0.25f, 0.21f, 0.27f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), true, false);
            CreateShopArtwork(chip.transform, "Icon", iconPath, new Vector2(14f, -9f), new Vector2(42f, 42f), Color.white, new Vector2(0f, 1f));
            valueText = CreateText(chip.transform, "Value", "0", Color.white, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(62f, 0f), new Vector2(-92f, 0f), 18, TextAnchor.MiddleLeft, true);
            valueText.resizeTextForBestFit = true;
            valueText.resizeTextMinSize = 14;
            valueText.resizeTextMaxSize = 18;
            CreateButton(chip.transform, "PlusButton", "+", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(44f, 44f), new Color(0.18f, 0.72f, 0.88f, 1f), plusAction, 25);
        }

        private void BuildYahtzeePlayPanel(Transform parent)
        {
            Image panel = CreatePanel(parent, "YahtzeePlayPanel", new Vector2(0f, -422f), new Vector2(900f, 630f), new Color(0.10f, 0.16f, 0.42f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            CreateShopArtwork(panel.transform, "YahtzeePlayGlow", "Common/glow", new Vector2(0f, -130f), new Vector2(720f, 390f), new Color(0.38f, 0.94f, 1f, 0.30f), new Vector2(0.5f, 1f));
            yahtzeeMultiplierText = CreateText(panel.transform, "YahtzeeMultiplier", "현재 배수  x1", new Color(1f, 0.84f, 0.24f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(520f, 60f), 38, TextAnchor.MiddleCenter, true);
            yahtzeeSessionText = CreateText(panel.transform, "YahtzeeSessionInfo", "티켓으로 첫 굴림을 시작하세요", new Color(0.74f, 0.90f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(760f, 40f), 21, TextAnchor.MiddleCenter, false);

            for (int index = 0; index < 3; index++)
            {
                int capturedIndex = index;
                Vector2 position = new Vector2((index - 1) * 270f, -250f);
                yahtzeeDiceBasePositions[index] = position;
                yahtzeeDiceButtons[index] = CreateButton(panel.transform, "YahtzeeDie_" + index, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), position, new Vector2(222f, 248f), new Color(0.24f, 0.42f, 0.78f, 1f), () => HandleYahtzeeDiePressed(capturedIndex), 20);
                yahtzeeDiceFaces[index] = CreateShopArtwork(yahtzeeDiceButtons[index].transform, "Face", "Roll/icon-roll-synergy-dice-1", new Vector2(0f, 22f), new Vector2(166f, 166f), Color.white, new Vector2(0.5f, 0.5f));
                yahtzeeDiceStateTexts[index] = CreateText(yahtzeeDiceButtons[index].transform, "State", "선택", Color.white, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-22f, 40f), 20, TextAnchor.MiddleCenter, true);
            }

            yahtzeeHoldButton = CreateButton(panel.transform, "YahtzeeHoldButton", "선택 주사위 홀드 · 20 DIA", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 108f), new Vector2(380f, 76f), new Color(0.50f, 0.32f, 0.88f, 1f), HandleYahtzeeHoldPressed, 23);
            yahtzeeRerollButton = CreateButton(panel.transform, "YahtzeeRerollButton", "재굴림 · 100 GOLD", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(18f, 108f), new Vector2(380f, 76f), new Color(0.16f, 0.70f, 0.88f, 1f), HandleYahtzeeRerollPressed, 23);
            yahtzeeStartButton = CreateButton(panel.transform, "YahtzeeStartButton", "티켓 1장으로 시작", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(500f, 82f), new Color(0.20f, 0.76f, 0.42f, 1f), HandleYahtzeeStartPressed, 28);
            yahtzeeConfirmButton = CreateButton(panel.transform, "YahtzeeConfirmButton", "현재 결과 확정", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(500f, 82f), new Color(0.96f, 0.48f, 0.16f, 1f), HandleYahtzeeConfirmPressed, 28);
        }

        private void BuildYahtzeeChestPanel(Transform parent)
        {
            Image panel = CreatePanel(parent, "YahtzeeChestPanel", new Vector2(0f, -1082f), new Vector2(900f, 302f), new Color(0.08f, 0.12f, 0.32f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            CreateShopArtwork(panel.transform, "YahtzeeChestIcon", "GradeAndGoodsIcons/goods_icon_reward_box", new Vector2(34f, -24f), new Vector2(82f, 82f), Color.white, new Vector2(0f, 1f));
            CreateText(panel.transform, "YahtzeeChestTitle", "얏찌 보상 상자", new Color(1f, 0.82f, 0.28f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(124f, -24f), new Vector2(380f, 44f), 29, TextAnchor.MiddleLeft, true);
            yahtzeeChestCountText = CreateText(panel.transform, "YahtzeeChestCount", "보관 0개", new Color(0.52f, 0.96f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -28f), new Vector2(260f, 42f), 23, TextAnchor.MiddleRight, true);
            CreateText(panel.transform, "YahtzeeChestOdds", "골드 70% · 다이아 22% · 영웅 카드 8%  |  배수는 수량에만 적용", new Color(0.78f, 0.88f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(820f, 36f), 18, TextAnchor.MiddleCenter, false);
            yahtzeeOpenOneButton = CreateButton(panel.transform, "YahtzeeOpenOneButton", "1개 열기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-274f, 34f), new Vector2(240f, 78f), new Color(0.22f, 0.70f, 0.86f, 1f), () => HandleYahtzeeOpenPressed(1), 23);
            yahtzeeOpenTenButton = CreateButton(panel.transform, "YahtzeeOpenTenButton", "10개 열기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(240f, 78f), new Color(0.44f, 0.42f, 0.92f, 1f), () => HandleYahtzeeOpenPressed(10), 23);
            yahtzeeOpenAllButton = CreateButton(panel.transform, "YahtzeeOpenAllButton", "전부 열기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(274f, 34f), new Vector2(240f, 78f), new Color(0.90f, 0.46f, 0.24f, 1f), () => HandleYahtzeeOpenPressed(0), 23);
        }

        private void BuildYahtzeeRewardPanel(Transform parent)
        {
            yahtzeeRewardPanel = CreatePanel(parent, "YahtzeeRewardPanel", new Vector2(0f, -1410f), new Vector2(900f, 286f), new Color(0.10f, 0.15f, 0.38f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            CreateText(yahtzeeRewardPanel.transform, "YahtzeeRewardTitle", "최근 개봉 결과", new Color(0.48f, 0.96f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(420f, 38f), 24, TextAnchor.MiddleCenter, true);
            for (int index = 0; index < 3; index++)
            {
                float x = (index - 1) * 250f;
                yahtzeeRewardCards[index] = CreatePanel(yahtzeeRewardPanel.transform, "YahtzeeRewardCard_" + index, new Vector2(x, -62f), new Vector2(220f, 142f), new Color(0.20f, 0.34f, 0.66f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
                yahtzeeRewardIcons[index] = CreateShopArtwork(yahtzeeRewardCards[index].transform, "Icon", "GradeAndGoodsIcons/goods_icon_reward_box", new Vector2(0f, -10f), new Vector2(70f, 70f), Color.white, new Vector2(0.5f, 1f));
                yahtzeeRewardTexts[index] = CreateText(yahtzeeRewardCards[index].transform, "Label", "상자 결과", Color.white, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(-16f, 52f), 17, TextAnchor.MiddleCenter, true);
                yahtzeeRewardCards[index].gameObject.SetActive(false);
            }
            yahtzeeRewardSummaryText = CreateText(yahtzeeRewardPanel.transform, "YahtzeeRewardSummary", "상자를 열면 카드가 차례로 공개됩니다.", new Color(0.84f, 0.92f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(820f, 40f), 18, TextAnchor.MiddleCenter, false);
        }

        private void ShowYahtzee()
        {
            if (gameController != null && gameController.IsRoundRunning) return;
            SetGameplayStageVisible(false);
            SetGameplayHudVisible(false);
            HideLoadout();
            HideShop();
            HideResult();
            HideSeasonRanking();
            HideOutgamePlaceholder();
            HideExitConfirm();
            if (characterCollectionUI != null && characterCollectionUI.IsOpen) characterCollectionUI.Close();
            HideLobby();
            ShowOutgameNavigation(hubYahtzeeButton);
            RefreshYahtzee();
            if (yahtzeeOverlay != null) PlayOverlayEnter(yahtzeeOverlay, "YahtzeeModal");
        }

        private void HideYahtzee()
        {
            if (yahtzeeOverlay == null) return;
            yahtzeeOverlay.transform.DOKill();
            yahtzeeOverlay.SetActive(false);
        }

        private void RefreshYahtzee()
        {
            if (yahtzeeProgression == null || yahtzeeOverlay == null) return;
            bool active = yahtzeeProgression.SessionActive;
            if (yahtzeeTicketText != null) yahtzeeTicketText.text = "TICKET " + yahtzeeProgression.TicketCount.ToString("N0");
            if (yahtzeeGoldText != null) yahtzeeGoldText.text = "GOLD " + (outgameProgression != null ? outgameProgression.Gold : 0).ToString("N0");
            if (yahtzeeDiamondText != null) yahtzeeDiamondText.text = "DIA " + (outgameProgression != null ? outgameProgression.Diamonds : 0).ToString("N0");
            if (yahtzeeChestCountText != null) yahtzeeChestCountText.text = "보관 " + yahtzeeProgression.ChestCount + "개";
            if (yahtzeeMultiplierText != null)
            {
                int multiplier = yahtzeeProgression.CurrentMultiplier;
                yahtzeeMultiplierText.text = active && multiplier > 1 ? "TRIPLE  x" + multiplier : "현재 배수  x1";
                yahtzeeMultiplierText.color = active && multiplier > 1 ? new Color(1f, 0.42f, 0.18f) : new Color(1f, 0.84f, 0.24f);
            }
            if (yahtzeeSessionText != null)
            {
                yahtzeeSessionText.text = active
                    ? "재굴림 " + yahtzeeProgression.RerollCount + "회 · 사용 " + yahtzeeProgression.SessionGoldSpent.ToString("N0") + " GOLD · 홀드 " + yahtzeeProgression.HoldCount + "/2"
                    : "티켓 1장 · 첫 굴림 무료 · 확정 전까지 상태 자동 저장";
            }

            for (int i = 0; i < 3; i++) RefreshYahtzeeDie(i, active);
            yahtzeeStartButton?.gameObject.SetActive(!active);
            yahtzeeHoldButton?.gameObject.SetActive(active);
            yahtzeeRerollButton?.gameObject.SetActive(active);
            yahtzeeConfirmButton?.gameObject.SetActive(active);
            if (yahtzeeStartButton != null) yahtzeeStartButton.interactable = yahtzeeProgression.TicketCount > 0;
            if (yahtzeeHoldButton != null)
            {
                yahtzeeHoldButton.interactable = active && yahtzeeProgression.HoldCount < 2 && yahtzeeProgression.HasPendingHold;
                SetButtonLabel(yahtzeeHoldButton, "선택 주사위 홀드 · " + yahtzeeProgression.NextHoldCost + " DIA");
            }
            if (yahtzeeRerollButton != null) yahtzeeRerollButton.interactable = active && !yahtzeeProgression.AllDiceHeld && outgameProgression != null && outgameProgression.Gold >= YahtzeeProgressionSystem.RerollGoldCost;
            if (yahtzeeConfirmButton != null)
            {
                yahtzeeConfirmButton.interactable = active;
                SetButtonLabel(yahtzeeConfirmButton, "x" + yahtzeeProgression.CurrentMultiplier + " 결과 확정");
            }
            bool hasChest = yahtzeeProgression.ChestCount > 0;
            if (yahtzeeOpenOneButton != null) yahtzeeOpenOneButton.interactable = hasChest;
            if (yahtzeeOpenTenButton != null) yahtzeeOpenTenButton.interactable = yahtzeeProgression.ChestCount >= 10;
            if (yahtzeeOpenAllButton != null) yahtzeeOpenAllButton.interactable = hasChest;
        }

        private void RefreshYahtzeeDie(int index, bool active)
        {
            bool isHeld = active && yahtzeeProgression.IsHeld(index);
            bool isPending = active && yahtzeeProgression.IsPendingHold(index);
            int value = yahtzeeProgression.GetDie(index);
            if (yahtzeeDiceFaces[index] != null)
            {
                yahtzeeDiceFaces[index].sprite = RollRollUiResource.LoadSprite(value > 0 ? "Roll/icon-roll-synergy-dice-" + value : "Roll/icon-roll-synergy-dice-1-disabled");
                yahtzeeDiceFaces[index].color = active ? Color.white : new Color(0.65f, 0.72f, 0.88f, 0.52f);
            }
            if (yahtzeeDiceStateTexts[index] != null)
            {
                yahtzeeDiceStateTexts[index].text = isHeld ? "HOLD" : isPending ? "홀드 선택" : active ? "탭하여 선택" : "대기";
                yahtzeeDiceStateTexts[index].color = isHeld ? new Color(1f, 0.86f, 0.30f) : isPending ? new Color(0.48f, 1f, 0.88f) : Color.white;
            }
            Button button = yahtzeeDiceButtons[index];
            if (button == null) return;
            button.interactable = active && !isHeld && yahtzeeProgression.HoldCount < 2;
            Image background = button.GetComponent<Image>();
            if (background != null) background.color = isHeld ? new Color(0.92f, 0.60f, 0.18f) : isPending ? new Color(0.18f, 0.72f, 0.62f) : new Color(0.24f, 0.42f, 0.78f);
            RectTransform rect = button.transform as RectTransform;
            if (rect == null) return;
            rect.DOKill();
            Vector2 target = yahtzeeDiceBasePositions[index] + (isHeld || isPending ? new Vector2(0f, 20f) : Vector2.zero);
            rect.DOAnchorPos(target, 0.16f).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        private void HandleYahtzeeStartPressed()
        {
            if (yahtzeeProgression == null) return;
            bool success = yahtzeeProgression.TryStartSession(out string message);
            SetYahtzeeStatus(message, success);
            RefreshYahtzee();
            if (success) AnimateYahtzeeDiceRoll();
        }

        private void HandleYahtzeeDiePressed(int index)
        {
            if (yahtzeeProgression == null) return;
            bool success = yahtzeeProgression.TogglePendingHold(index, out string message);
            SetYahtzeeStatus(message, success);
            RefreshYahtzee();
        }

        private void HandleYahtzeeHoldPressed()
        {
            if (yahtzeeProgression == null) return;
            bool success = yahtzeeProgression.TryCommitHold(out string message);
            SetYahtzeeStatus(message, success);
            RefreshYahtzee();
        }

        private void HandleYahtzeeRerollPressed()
        {
            if (yahtzeeProgression == null) return;
            bool success = yahtzeeProgression.TryReroll(out string message);
            SetYahtzeeStatus(message, success);
            RefreshYahtzee();
            if (success) AnimateYahtzeeDiceRoll();
        }

        private void HandleYahtzeeConfirmPressed()
        {
            if (yahtzeeProgression == null) return;
            bool success = yahtzeeProgression.TryConfirmResult(out int multiplier, out string message);
            SetYahtzeeStatus(message, success);
            RefreshYahtzee();
            if (success && yahtzeeChestCountText != null)
            {
                yahtzeeChestCountText.transform.DOKill();
                yahtzeeChestCountText.transform.DOPunchScale(Vector3.one * (0.16f + multiplier * 0.025f), 0.38f, 8, 0.6f).SetUpdate(true);
            }
        }

        private void HandleYahtzeeOpenPressed(int count)
        {
            if (yahtzeeProgression == null) return;
            bool success = yahtzeeProgression.TryOpenChests(count, out List<YahtzeeRewardResult> rewards, out string message);
            SetYahtzeeStatus(message, success);
            RefreshYahtzee();
            if (success) RevealYahtzeeRewards(rewards);
        }

        private void HandleYahtzeeTicketTopUp()
        {
            if (yahtzeeProgression != null && yahtzeeProgression.GrantTestTickets(10))
            {
                SetYahtzeeStatus("TEST 티켓 +10", true);
                RefreshYahtzee();
                return;
            }
            OpenShopFromYahtzee("티켓 상품은 상점·패스에서 구매할 수 있습니다.");
        }

        private void HandleYahtzeeGoldTopUp()
        {
            if (outgameProgression != null && outgameProgression.GrantTestShopCurrency(10000, 0))
            {
                SetYahtzeeStatus("TEST GOLD +10,000", true);
                RefreshYahtzee();
                return;
            }
            OpenShopFromYahtzee("골드 상품을 확인하세요.");
        }

        private void HandleYahtzeeDiamondTopUp()
        {
            if (outgameProgression != null && outgameProgression.GrantTestShopCurrency(0, 10000))
            {
                SetYahtzeeStatus("TEST DIA +10,000", true);
                RefreshYahtzee();
                return;
            }
            OpenShopFromYahtzee("다이아 상품을 확인하세요.");
        }

        private void OpenShopFromYahtzee(string message)
        {
            SetYahtzeeStatus(message, true);
            HideYahtzee();
            ToggleShop();
        }

        private void SetYahtzeeStatus(string message, bool success)
        {
            if (yahtzeeStatusText == null) return;
            yahtzeeStatusText.text = message;
            yahtzeeStatusText.color = success ? new Color(0.56f, 1f, 0.76f) : new Color(1f, 0.48f, 0.38f);
            yahtzeeStatusText.transform.DOKill();
            yahtzeeStatusText.transform.DOPunchScale(Vector3.one * 0.08f, 0.28f, 6, 0.5f).SetUpdate(true);
        }

        private void AnimateYahtzeeDiceRoll()
        {
            for (int i = 0; i < yahtzeeDiceButtons.Length; i++)
            {
                if (yahtzeeDiceButtons[i] == null || yahtzeeProgression.IsHeld(i)) continue;
                Transform target = yahtzeeDiceButtons[i].transform;
                target.DOKill();
                target.localScale = Vector3.one * 0.82f;
                Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(target);
                sequence.SetDelay(i * 0.06f);
                sequence.Join(target.DOScale(1f, 0.32f).SetEase(Ease.OutBack));
                sequence.Join(target.DOPunchRotation(new Vector3(0f, 0f, 22f), 0.34f, 7, 0.55f));
            }
        }

        private void RevealYahtzeeRewards(List<YahtzeeRewardResult> rewards)
        {
            if (rewards == null || rewards.Count == 0 || yahtzeeRewardPanel == null) return;
            int visibleCount = Mathf.Min(3, rewards.Count);
            for (int i = 0; i < yahtzeeRewardCards.Length; i++) yahtzeeRewardCards[i].gameObject.SetActive(i < visibleCount);
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(yahtzeeRewardPanel.gameObject);
            for (int i = 0; i < visibleCount; i++)
            {
                ConfigureYahtzeeRewardCard(i, rewards[i]);
                Transform card = yahtzeeRewardCards[i].transform;
                card.localScale = Vector3.zero;
                sequence.Append(card.DOScale(1f, 0.20f).SetEase(Ease.OutBack));
                sequence.AppendInterval(0.05f);
            }
            if (yahtzeeRewardSummaryText != null) yahtzeeRewardSummaryText.text = BuildYahtzeeRewardSummary(rewards);
            sequence.Append(yahtzeeRewardPanel.transform.DOPunchScale(Vector3.one * 0.035f, 0.30f, 5, 0.5f));
        }

        private void ConfigureYahtzeeRewardCard(int index, YahtzeeRewardResult reward)
        {
            if (reward == null) return;
            string iconPath;
            string label;
            Color cardColor;
            if (reward.rewardType == YahtzeeRewardType.Gold)
            {
                iconPath = "Icons/goods_icon_gold";
                label = reward.amount.ToString("N0") + " GOLD\nx" + reward.multiplier;
                cardColor = new Color(0.62f, 0.42f, 0.15f, 1f);
            }
            else if (reward.rewardType == YahtzeeRewardType.Diamond)
            {
                iconPath = "Icons/goods_icon_ruby";
                label = reward.amount.ToString("N0") + " DIA\nx" + reward.multiplier;
                cardColor = new Color(0.46f, 0.25f, 0.72f, 1f);
            }
            else
            {
                iconPath = "GradeAndGoodsIcons/goods_icon_reward_box";
                label = reward.characterName + "\n카드 x" + reward.amount;
                cardColor = ResolveYahtzeeGradeColor(reward.characterGrade);
            }
            yahtzeeRewardCards[index].color = cardColor;
            Sprite sprite = RollRollUiResource.LoadSprite(iconPath);
            if (reward.rewardType == YahtzeeRewardType.CharacterCard && characterDatabase != null)
            {
                CharacterDefinition definition = characterDatabase.GetCharacterById(reward.characterId);
                Sprite portrait = RollRollUiResource.ResolveCharacterSprite(definition);
                if (portrait != null) sprite = portrait;
            }
            yahtzeeRewardIcons[index].sprite = sprite;
            yahtzeeRewardIcons[index].color = Color.white;
            yahtzeeRewardTexts[index].text = label;
        }

        private static string BuildYahtzeeRewardSummary(List<YahtzeeRewardResult> rewards)
        {
            int gold = 0, diamonds = 0, cards = 0;
            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewards[i].rewardType == YahtzeeRewardType.Gold) gold += rewards[i].amount;
                else if (rewards[i].rewardType == YahtzeeRewardType.Diamond) diamonds += rewards[i].amount;
                else cards += rewards[i].amount;
            }
            return "총 " + rewards.Count + "상자  |  GOLD +" + gold.ToString("N0") + "  ·  DIA +" + diamonds.ToString("N0") + "  ·  영웅 카드 +" + cards;
        }

        private static Color ResolveYahtzeeGradeColor(CharacterGrade grade)
        {
            switch (grade)
            {
                case CharacterGrade.Rare: return new Color(0.18f, 0.54f, 0.92f, 1f);
                case CharacterGrade.Epic: return new Color(0.14f, 0.70f, 0.48f, 1f);
                case CharacterGrade.Legendary: return new Color(0.95f, 0.66f, 0.16f, 1f);
                case CharacterGrade.Mythic: return new Color(0.92f, 0.26f, 0.34f, 1f);
                case CharacterGrade.Transcendent: return new Color(0.76f, 0.28f, 0.86f, 1f);
                default: return new Color(0.36f, 0.43f, 0.60f, 1f);
            }
        }

    }
}
