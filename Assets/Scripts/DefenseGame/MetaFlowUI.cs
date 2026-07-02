using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenseGame
{
    public class MetaFlowUI : MonoBehaviour
    {
        [SerializeField] private DefenseGameController gameController;
        [SerializeField] private GameUIButtonBinder buttonBinder;
        [SerializeField] private AugmentManager augmentManager;
        [SerializeField] private CharacterDatabase characterDatabase;
        [SerializeField] private OutgameProgressionSystem outgameProgression;
        [SerializeField] private CharacterCollectionUI characterCollectionUI;
        [SerializeField] private float matchmakingDuration = 1.6f;
        [SerializeField] private int presetCount = 4;
        [SerializeField] private int cardsPerPreset = 5;
        [SerializeField] private float defeatResultRevealDelay = 5.15f;

        private readonly List<PresetDefinition> presets = new List<PresetDefinition>();
        private readonly List<Image> lobbyFeaturedAccentImages = new List<Image>();
        private readonly List<Image> lobbyFeaturedPortraitImages = new List<Image>();
        private readonly List<Text> lobbyFeaturedNameTexts = new List<Text>();
        private readonly List<Text> lobbyFeaturedGradeTexts = new List<Text>();
        private readonly List<Image> loadoutDeckAccentImages = new List<Image>();
        private readonly List<Image> loadoutDeckPortraitImages = new List<Image>();
        private readonly List<Text> loadoutDeckNameTexts = new List<Text>();
        private readonly List<Text> loadoutDeckDetailTexts = new List<Text>();
        private readonly List<Image> loadoutRosterAccentImages = new List<Image>();
        private readonly List<Image> loadoutRosterPortraitImages = new List<Image>();
        private readonly List<Text> loadoutRosterNameTexts = new List<Text>();
        private readonly List<Text> loadoutRosterDetailTexts = new List<Text>();

        private Font font;
        private UiSkinResources uiSkin;
        private GameObject root;
        private GameObject gameplayHudRoot;
        private GameObject lobbyOverlay;
        private GameObject matchmakingOverlay;
        private GameObject resultOverlay;
        private GameObject loadoutOverlay;
        private GameObject outgamePlaceholderOverlay;
        private GameObject exitConfirmOverlay;
        private GameObject shopOverlay;
        private GameObject shopSceneCanvasRoot;
        private Text lobbyPresetNameText;
        private Text lobbyPresetDescriptionText;
        private Text lobbyModeText;
        private Text lobbyFortuneText;
        private Text queueTimerText;
        private Text queueStatusText;
        private Text resultTitleText;
        private Text resultSummaryText;
        private Text resultRewardGoldText;
        private Text resultRewardCoreText;
        private Text resultMetaText;
        private Text resultScoreText;
        private Text resultRecapText;
        private Text resultNextText;
        private Text shopDiamondText;
        private Text shopRatesText;
        private Text shopCollectionText;
        private Text shopResultText;
        private Text shopModeText;
        private Text loadoutHeaderText;
        private Text loadoutSummaryText;
        private Button battleButton;
        private Button lobbyButton;
        private Button loadoutButton;
        private Button lobbyBattleButton;
        private Button resultContinueButton;
        private Button resultRetryButton;
        private Button matchmakingCancelButton;
        private Button loadoutCloseButton;
        private Button lobbyCollectionButton;
        private Button lobbyShopButton;
        private Button lobbyModeButton;
        private Button loadoutCollectionButton;
        private Button hubShopButton;
        private Button hubInventoryButton;
        private Button hubLobbyButton;
        private Button hubYahtzeeButton;
        private Button hubRankingButton;
        private Button placeholderCloseButton;
        private Button exitConfirmLeaveButton;
        private Button exitConfirmContinueButton;
        private Button shopSingleDrawButton;
        private Button shopTenDrawButton;
        private Button shopTestDiamondButton;
        private Coroutine matchmakingRoutine;
        private Coroutine resultRoutine;
        private Coroutine drawRevealRoutine;
        private Sprite roundedSprite;
        private CharacterCollectionUI subscribedCollectionUI;
        private int selectedPresetIndex;
        private bool subscribed;
        private bool defeatPresented;
        private bool resultRewardGranted;
        private Scene gameplayScene;
        private Scene shopScene;

        private sealed class PresetDefinition
        {
            public string name;
            public string description;
            public Color accentColor;
            public readonly List<int> characterIndices = new List<int>();
        }

        public void Configure(
            DefenseGameController controller,
            GameUIButtonBinder binder,
            AugmentManager augments,
            CharacterDatabase database,
            OutgameProgressionSystem progression,
            CharacterCollectionUI collection,
            Font uiFont,
            Transform canvasRoot,
            GameObject gameplayHud,
            Button externalBattleButton,
            Button externalLobbyButton,
            Button externalLoadoutButton,
            UiSkinResources skin = null)
        {
            gameController = controller;
            buttonBinder = binder;
            augmentManager = augments;
            characterDatabase = database;
            outgameProgression = progression;
            characterCollectionUI = collection;
            font = uiFont;
            uiSkin = skin;
            gameplayHudRoot = gameplayHud;
            battleButton = externalBattleButton;
            lobbyButton = externalLobbyButton;
            loadoutButton = externalLoadoutButton;

            if (root != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(root);
                }
                else
                {
                    DestroyImmediate(root);
                }
            }

            Build(canvasRoot);
            BuildPresets();
            ApplyRecommendedPreset();
            WireButtons();
            Subscribe();
            SubscribeCollectionClosed();

            HideMatchmaking();
            HideResult();
            HideLoadout();
            HideOutgamePlaceholder();
            HideExitConfirm();
            HideShop();
            RefreshModeUi();

            if (gameController != null && gameController.CurrentRound <= 0 && !gameController.IsRoundRunning)
            {
                ShowLobby();
            }
            else
            {
                HideLobby();
            }
        }

        private void OnEnable()
        {
            Subscribe();
            SubscribeCollectionClosed();
        }

        private void OnDisable()
        {
            Unsubscribe();
            UnsubscribeCollectionClosed();
        }

        private void Subscribe()
        {
            if (subscribed || gameController == null)
            {
                return;
            }

            gameController.OnRoundStarted += HandleRoundStarted;
            gameController.OnRoundCompleted += HandleRoundCompleted;
            gameController.OnGameOver += HandleGameOver;
            if (outgameProgression != null)
            {
                outgameProgression.OnProgressChanged += HandleProgressChanged;
            }
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || gameController == null)
            {
                return;
            }

            gameController.OnRoundStarted -= HandleRoundStarted;
            gameController.OnRoundCompleted -= HandleRoundCompleted;
            gameController.OnGameOver -= HandleGameOver;
            if (outgameProgression != null)
            {
                outgameProgression.OnProgressChanged -= HandleProgressChanged;
            }
            subscribed = false;
        }

        private void SubscribeCollectionClosed()
        {
            if (subscribedCollectionUI == characterCollectionUI)
            {
                return;
            }

            UnsubscribeCollectionClosed();
            subscribedCollectionUI = characterCollectionUI;
            if (subscribedCollectionUI != null)
            {
                subscribedCollectionUI.OnClosed += HandleCollectionClosed;
            }
        }

        private void UnsubscribeCollectionClosed()
        {
            if (subscribedCollectionUI != null)
            {
                subscribedCollectionUI.OnClosed -= HandleCollectionClosed;
                subscribedCollectionUI = null;
            }
        }

        private void Build(Transform parent)
        {
            root = new GameObject("MetaFlowOverlayRoot", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            BuildLobbyOverlay(root.transform);
            BuildMatchmakingOverlay(root.transform);
            BuildResultOverlay(root.transform);
            BuildLoadoutOverlay(root.transform);
            BuildOutgamePlaceholderOverlay(root.transform);
            BuildExitConfirmOverlay(root.transform);
        }

        private void BuildLobbyOverlay(Transform parent)
        {
            lobbyOverlay = CreateOverlayRoot(parent, "LobbyOverlay", new Color(0.03f, 0.05f, 0.15f, 0.60f));
            Image modal = CreatePanel(lobbyOverlay.transform, "LobbyModal", new Vector2(0f, -18f), new Vector2(930f, 1340f), new Color(0.06f, 0.16f, 0.46f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, true);

            CreatePanel(modal.transform, "TopBanner", new Vector2(0f, -34f), new Vector2(760f, 104f), new Color(0.84f, 0.92f, 1f, 0.18f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreateText(modal.transform, "LobbyTitle", "로비", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(260f, 50f), 38, TextAnchor.MiddleCenter, true);
            CreateText(modal.transform, "LobbySubTitle", "이번 라운드의 추천 조합을 참고하고 전투를 준비하세요.", new Color(0.86f, 0.91f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(720f, 40f), 22, TextAnchor.MiddleCenter, false);
            lobbyModeText = CreateText(modal.transform, "LobbyModeText", "SERVICE", new Color(0.43f, 1f, 0.80f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -48f), new Vector2(162f, 34f), 19, TextAnchor.MiddleLeft, true);
            lobbyModeButton = CreateButton(modal.transform, "LobbyModeButton", "테스트 진입", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-44f, -42f), new Vector2(156f, 54f), new Color(0.23f, 0.72f, 0.82f, 1f), TogglePlayMode, 18);
            lobbyFortuneText = CreateText(modal.transform, "LobbyFortuneText", DailyFortuneSystem.TodaySummary, new Color(1f, 0.88f, 0.40f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(720f, 30f), 18, TextAnchor.MiddleCenter, true);

            Image coreFactory = CreatePanel(modal.transform, "CoreFactory", new Vector2(0f, -214f), new Vector2(760f, 226f), new Color(0.12f, 0.16f, 0.37f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            coreFactory.gameObject.AddComponent<RectMask2D>();
            CreatePanel(coreFactory.transform, "FactoryGlow", new Vector2(-132f, -78f), new Vector2(116f, 86f), new Color(0.23f, 0.92f, 0.98f, 0.18f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreatePanel(coreFactory.transform, "FactoryStage", new Vector2(0f, -78f), new Vector2(116f, 86f), new Color(0.34f, 0.15f, 0.76f, 0.86f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreatePanel(coreFactory.transform, "FactoryCore", new Vector2(132f, -78f), new Vector2(116f, 86f), new Color(0.16f, 0.98f, 0.90f, 0.22f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreateText(modal.transform, "FactoryLabel", "이번 라운드 추천 조합", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -192f), new Vector2(420f, 44f), 32, TextAnchor.MiddleCenter, true);
            CreateText(modal.transform, "FactoryHint", "운영 참고용 정보이며 실제 소환 유닛이나 확률에는 영향을 주지 않습니다.", new Color(0.82f, 0.90f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -398f), new Vector2(760f, 52f), 21, TextAnchor.MiddleCenter, false);

            lobbyPresetNameText = CreateText(modal.transform, "LobbyPresetName", "R1 추천 · 안정 성장", new Color(1f, 0.90f, 0.42f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -490f), new Vector2(620f, 44f), 32, TextAnchor.MiddleCenter, true);
            lobbyPresetDescriptionText = CreateText(modal.transform, "LobbyPresetDescription", string.Empty, new Color(0.87f, 0.92f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -536f), new Vector2(820f, 54f), 22, TextAnchor.MiddleCenter, false);

            MoveRectInto(coreFactory.transform, modal.transform, "FactoryLabel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(380f, 42f));
            MoveRectInto(coreFactory.transform, modal.transform, "FactoryHint", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(720f, 46f));
            SetRect(lobbyPresetNameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -492f), new Vector2(620f, 44f));
            SetRect(lobbyPresetDescriptionText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -540f), new Vector2(780f, 54f));

            BuildRecommendationNotice(modal.transform, -616f);
            BuildLobbyFeaturedCards(modal.transform);
            lobbyCollectionButton = CreateButton(modal.transform, "LobbyCollectionButton", "도감", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-260f, 120f), new Vector2(200f, 78f), new Color(0.26f, 0.56f, 1f, 0.98f), ToggleCollection, 27);
            lobbyShopButton = CreateButton(modal.transform, "LobbyShopButton", "상점", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(260f, 120f), new Vector2(200f, 78f), new Color(0.02f, 0.72f, 0.88f, 0.98f), ToggleShop, 27);

            lobbyBattleButton = CreateButton(modal.transform, "LobbyBattleButton", "전투 시작", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 118f), new Vector2(340f, 88f), new Color(0.98f, 0.20f, 0.13f, 1f), HandleBattlePressed, 33);
            CreateText(modal.transform, "LobbyBottomHint", "준비가 끝났다면 전투 시작을 눌러 라운드를 시작하세요.", new Color(0.88f, 0.92f, 1f, 0.88f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(760f, 38f), 19, TextAnchor.MiddleCenter, false);
            BuildOutgameBottomNav(lobbyOverlay.transform);
        }

        private void BuildOutgameBottomNav(Transform parent)
        {
            Image dock = CreatePanel(parent, "OutgameBottomNavDock", new Vector2(0f, 0f), new Vector2(0f, 152f), new Color(0.88f, 0.93f, 1f, 0.96f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), false, true);
            CreatePanel(dock.transform, "DockTopLine", new Vector2(0f, 150f), new Vector2(0f, 4f), new Color(1f, 1f, 1f, 0.70f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), false, false);

            hubShopButton = CreateOutgameNavButton(dock.transform, "OutgameNavShop", "상점", "SHOP", new Vector2(-432f, 76f), new Color(1f, 0.58f, 0.76f), ToggleShop);
            hubInventoryButton = CreateOutgameNavButton(dock.transform, "OutgameNavInventory", "인벤", "CARD", new Vector2(-216f, 76f), new Color(0.98f, 0.36f, 0.36f), ToggleCollection);
            hubLobbyButton = CreateOutgameNavButton(dock.transform, "OutgameNavLobby", "로비", "HOME", new Vector2(0f, 76f), new Color(0.30f, 0.62f, 1f), ShowLobbyTab);
            hubYahtzeeButton = CreateOutgameNavButton(dock.transform, "OutgameNavYahtzee", "얏찌", "DICE", new Vector2(216f, 76f), new Color(1f, 0.62f, 0.22f), () => ShowOutgamePlaceholder("얏찌", "주사위 기반 보너스 컨텐츠 자리입니다.\n운빨존많겜식 일일/주간 변동 컨텐츠 후보로 남겨둡니다."));
            hubRankingButton = CreateOutgameNavButton(dock.transform, "OutgameNavRanking", "랭킹", "CUP", new Vector2(432f, 76f), new Color(0.74f, 0.52f, 1f), ShowSeasonRanking);
            HighlightOutgameNav(hubLobbyButton);
        }

        private Button CreateOutgameNavButton(Transform parent, string name, string label, string icon, Vector2 position, Color accent, UnityEngine.Events.UnityAction action)
        {
            Button button = CreateButton(parent, name, string.Empty, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), position, new Vector2(164f, 118f), new Color(0.93f, 0.96f, 1f, 0.98f), action, 20);
            Image iconPlate = CreatePanel(button.transform, "IconPlate", new Vector2(0f, 33f), new Vector2(68f, 58f), accent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, false);
            CreateText(iconPlate.transform, "IconText", icon, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 14, TextAnchor.MiddleCenter, true);
            Text labelText = CreateText(button.transform, "NavLabel", label, new Color(0.20f, 0.25f, 0.42f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(132f, 30f), 20, TextAnchor.MiddleCenter, true);
            AddReadableOutline(labelText);
            return button;
        }

        private void BuildOutgamePlaceholderOverlay(Transform parent)
        {
            outgamePlaceholderOverlay = CreateOverlayRoot(parent, "OutgamePlaceholderOverlay", new Color(0.03f, 0.05f, 0.15f, 0.72f));
            Image modal = CreatePanel(outgamePlaceholderOverlay.transform, "PlaceholderModal", new Vector2(0f, 40f), new Vector2(760f, 560f), new Color(0.10f, 0.16f, 0.42f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, true);
            CreatePanel(modal.transform, "PlaceholderGlow", new Vector2(0f, -40f), new Vector2(600f, 88f), new Color(0.36f, 0.78f, 1f, 0.18f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreateText(modal.transform, "PlaceholderTitle", "준비중", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(520f, 56f), 38, TextAnchor.MiddleCenter, true);
            CreateText(modal.transform, "PlaceholderBody", "아웃게임 컨텐츠 화면 자리입니다.", new Color(0.86f, 0.92f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(610f, 170f), 24, TextAnchor.MiddleCenter, false);
            placeholderCloseButton = CreateButton(modal.transform, "PlaceholderCloseButton", "닫기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(220f, 72f), new Color(0.30f, 0.62f, 1f, 1f), HideOutgamePlaceholder, 26);
        }

        private void BuildExitConfirmOverlay(Transform parent)
        {
            exitConfirmOverlay = CreateOverlayRoot(parent, "ExitConfirmOverlay", new Color(0.02f, 0.03f, 0.09f, 0.76f));
            Image modal = CreatePanel(exitConfirmOverlay.transform, "ExitConfirmModal", new Vector2(0f, 34f), new Vector2(720f, 420f), new Color(0.08f, 0.13f, 0.32f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, true);
            CreatePanel(modal.transform, "ExitConfirmGlow", new Vector2(0f, -44f), new Vector2(560f, 76f), new Color(1f, 0.44f, 0.32f, 0.16f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreateText(modal.transform, "ExitConfirmTitle", "나가시겠습니까?", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(560f, 58f), 38, TextAnchor.MiddleCenter, true);
            CreateText(modal.transform, "ExitConfirmBody", "현재 도전을 종료하고 로비로 이동합니다.", new Color(0.86f, 0.92f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(600f, 96f), 25, TextAnchor.MiddleCenter, false);
            exitConfirmLeaveButton = CreateButton(modal.transform, "ExitConfirmLeaveButton", "나가기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 70f), new Vector2(260f, 74f), new Color(0.92f, 0.28f, 0.22f, 1f), ConfirmExitToOutgame, 27);
            exitConfirmContinueButton = CreateButton(modal.transform, "ExitConfirmContinueButton", "계속하기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(18f, 70f), new Vector2(260f, 74f), new Color(0.30f, 0.62f, 1f, 1f), HideExitConfirm, 27);
        }

        private void BuildShopOverlay(Transform parent)
        {
            shopOverlay = CreateOverlayRoot(parent, "OutgameShopOverlay", new Color(0.02f, 0.04f, 0.13f, 0.86f));
            Image modal = CreatePanel(shopOverlay.transform, "ShopModal", new Vector2(0f, 18f), new Vector2(900f, 1250f), new Color(0.13f, 0.18f, 0.40f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, true);

            CreatePanel(modal.transform, "ShopHeader", new Vector2(0f, -18f), new Vector2(830f, 112f), new Color(0.12f, 0.68f, 0.80f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreateText(modal.transform, "ShopTitle", "상점", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-300f, -45f), new Vector2(180f, 48f), 38, TextAnchor.MiddleCenter, true);
            shopDiamondText = CreateText(modal.transform, "DiamondText", "DIA 0", new Color(1f, 0.96f, 0.62f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(138f, -46f), new Vector2(250f, 42f), 30, TextAnchor.MiddleRight, true);
            CreateButton(modal.transform, "ShopCloseButton", "닫기", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -38f), new Vector2(94f, 62f), new Color(0.92f, 0.36f, 0.29f, 1f), HideShop, 22);
            shopModeText = CreateText(modal.transform, "ShopModeText", "SERVICE", new Color(0.48f, 1f, 0.83f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -124f), new Vector2(250f, 34f), 19, TextAnchor.MiddleLeft, true);
            shopTestDiamondButton = CreateButton(modal.transform, "TestDiamondButton", "다이아 +10,000", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-48f, -116f), new Vector2(216f, 54f), new Color(0.19f, 0.80f, 0.72f, 1f), RechargeTestDiamonds, 19);

            Image product = CreatePanel(modal.transform, "HeroChestProduct", new Vector2(0f, -188f), new Vector2(780f, 362f), new Color(0.18f, 0.24f, 0.53f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            CreatePanel(product.transform, "ChestIcon", new Vector2(-260f, -70f), new Vector2(152f, 152f), new Color(0.97f, 0.74f, 0.22f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreateText(product.transform, "ChestIconLabel", "CARD", new Color(0.20f, 0.20f, 0.30f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-260f, -119f), new Vector2(146f, 44f), 23, TextAnchor.MiddleCenter, true);
            CreateText(product.transform, "ProductTitle", "영웅 카드 상자", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(88f, -40f), new Vector2(410f, 42f), 32, TextAnchor.MiddleLeft, true);
            CreateText(product.transform, "ProductInfo", "카드를 모아 명함을 해금하고\n중복 카드로 영웅을 성장시킵니다.", new Color(0.86f, 0.92f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(88f, -92f), new Vector2(410f, 66f), 20, TextAnchor.UpperLeft, false);
            shopSingleDrawButton = CreateButton(product.transform, "SingleDrawButton", "1회 뽑기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 38f), new Vector2(292f, 72f), new Color(0.23f, 0.80f, 0.73f, 1f), () => TryOpenChest(1), 23);
            shopTenDrawButton = CreateButton(product.transform, "TenDrawButton", "10회 뽑기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(16f, 38f), new Vector2(340f, 72f), new Color(0.98f, 0.58f, 0.23f, 1f), () => TryOpenChest(10), 23);

            shopRatesText = CreateText(modal.transform, "RatesText", string.Empty, new Color(0.83f, 0.91f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -586f), new Vector2(780f, 42f), 19, TextAnchor.MiddleCenter, false);
            shopCollectionText = CreateText(modal.transform, "CollectionText", string.Empty, new Color(1f, 0.92f, 0.50f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -644f), new Vector2(760f, 38f), 23, TextAnchor.MiddleCenter, true);
            Image resultPanel = CreatePanel(modal.transform, "DrawResults", new Vector2(0f, -716f), new Vector2(780f, 430f), new Color(0.08f, 0.12f, 0.29f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            CreateText(resultPanel.transform, "ResultTitle", "획득 결과", new Color(0.45f, 0.96f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(320f, 34f), 25, TextAnchor.MiddleCenter, true);
            shopResultText = CreateText(resultPanel.transform, "ResultBody", "상자를 열면 획득 카드가 여기에 표시됩니다.", new Color(0.91f, 0.94f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(700f, 320f), 20, TextAnchor.UpperLeft, false);
            RefreshShop();
        }

        private void BuildMatchmakingOverlay(Transform parent)
        {
            matchmakingOverlay = CreateOverlayRoot(parent, "MatchmakingOverlay", new Color(0.02f, 0.04f, 0.12f, 0.72f));
            Image modal = CreatePanel(matchmakingOverlay.transform, "MatchmakingModal", Vector2.zero, new Vector2(620f, 420f), new Color(0.16f, 0.20f, 0.44f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, true);

            CreatePanel(modal.transform, "PulseA", new Vector2(0f, -132f), new Vector2(156f, 82f), new Color(0.20f, 1f, 0.92f, 0.14f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreatePanel(modal.transform, "PulseB", new Vector2(0f, -132f), new Vector2(210f, 118f), new Color(1f, 0.32f, 0.64f, 0.08f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreateText(modal.transform, "MatchTitle", "전투 준비 중", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(560f, 42f), 28, TextAnchor.MiddleCenter, true);
            queueTimerText = CreateText(modal.transform, "QueueTimer", "00.00", new Color(0.28f, 1f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(220f, 68f), 46, TextAnchor.MiddleCenter, true);
            queueStatusText = CreateText(modal.transform, "QueueStatus", "라운드 전장을 준비하는 중...", new Color(0.84f, 0.90f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -226f), new Vector2(540f, 58f), 18, TextAnchor.MiddleCenter, false);
            queueStatusText.resizeTextForBestFit = true;
            queueStatusText.resizeTextMinSize = 14;
            queueStatusText.resizeTextMaxSize = 18;
            matchmakingCancelButton = CreateButton(modal.transform, "MatchmakingCancelButton", "닫기", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -326f), new Vector2(200f, 64f), new Color(0.95f, 0.45f, 0.30f, 1f), CancelMatchmaking, 25);
        }
        private void BuildResultOverlay(Transform parent)
        {
            resultOverlay = CreateOverlayRoot(parent, "RoundResultOverlay", new Color(0.03f, 0.05f, 0.15f, 0.74f));
            Image modal = CreatePanel(resultOverlay.transform, "ResultModal", new Vector2(0f, 24f), new Vector2(830f, 1120f), new Color(0.13f, 0.17f, 0.42f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, true);
            CreatePanel(modal.transform, "ResultRibbon", new Vector2(0f, -126f), new Vector2(620f, 112f), new Color(0.17f, 0.42f, 1f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);

            resultTitleText = CreateText(modal.transform, "ResultTitle", "승리", new Color(1f, 0.84f, 0.18f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(500f, 66f), 56, TextAnchor.MiddleCenter, true);
            resultSummaryText = CreateText(modal.transform, "ResultSummary", "라운드 1 클리어", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -204f), new Vector2(520f, 34f), 24, TextAnchor.MiddleCenter, true);
            resultMetaText = CreateText(modal.transform, "ResultMeta", "연속 클리어 +1", new Color(0.95f, 0.90f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -254f), new Vector2(660f, 44f), 18, TextAnchor.MiddleCenter, true);

            Image recapPanel = CreatePanel(modal.transform, "ResultRecapPanel", new Vector2(0f, -356f), new Vector2(730f, 390f), new Color(0.08f, 0.13f, 0.35f, 0.86f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            resultScoreText = CreateText(recapPanel.transform, "ResultScore", "RUN SCORE A / 000점", new Color(1f, 0.85f, 0.24f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(660f, 44f), 34, TextAnchor.MiddleCenter, true);
            resultRecapText = CreateText(recapPanel.transform, "ResultRecap", "MVP 기록 대기\n시너지 준비  |  콤보 준비\n사건 다음 판 대박 조합 노리기", new Color(0.90f, 0.96f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(660f, 132f), 27, TextAnchor.UpperLeft, false);
            resultNextText = CreateText(recapPanel.transform, "ResultNext", "다음 추천덱\n카드 조각 목표  |  상점 보충", new Color(0.62f, 1f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -238f), new Vector2(660f, 96f), 25, TextAnchor.UpperLeft, true);
            resultRecapText.resizeTextForBestFit = true;
            resultRecapText.resizeTextMinSize = 12;
            resultRecapText.resizeTextMaxSize = 16;
            resultNextText.resizeTextForBestFit = true;
            resultNextText.resizeTextMinSize = 11;
            resultNextText.resizeTextMaxSize = 14;
            ApplyReadableResultTextLayout();

            Image rewardPanel = CreatePanel(modal.transform, "RewardPanel", new Vector2(0f, -774f), new Vector2(610f, 178f), new Color(0.23f, 0.18f, 0.60f, 0.88f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            CreateText(rewardPanel.transform, "RewardHeader", "보상", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(200f, 30f), 23, TextAnchor.MiddleCenter, true);
            resultRewardGoldText = CreateRewardChip(rewardPanel.transform, "RewardGold", "골드", new Vector2(-130f, -58f), new Color(1f, 0.78f, 0.24f), "+000");
            resultRewardCoreText = CreateRewardChip(rewardPanel.transform, "RewardCore", "다이아", new Vector2(130f, -58f), new Color(0.28f, 0.88f, 1f), "+000");

            resultRetryButton = CreateButton(modal.transform, "ResultRetryButton", "새 판 다시하기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-120f, 58f), new Vector2(306f, 78f), new Color(0.30f, 0.86f, 0.36f, 1f), RetryFromResult, 27);
            resultContinueButton = CreateButton(modal.transform, "ResultContinueButton", "계속", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(190f, 58f), new Vector2(220f, 72f), new Color(0.30f, 0.62f, 1f, 1f), ContinueFromResult, 24);
        }

        private void ApplyReadableResultTextLayout()
        {
            ConfigureResultText(resultScoreText, new Vector2(0f, -40f), new Vector2(660f, 56f), 34, TextAnchor.MiddleCenter, 34, 34);
            ConfigureResultText(resultRecapText, new Vector2(0f, -98f), new Vector2(660f, 166f), 24, TextAnchor.UpperLeft, 18, 24);
            ConfigureResultText(resultNextText, new Vector2(0f, -270f), new Vector2(660f, 106f), 19, TextAnchor.UpperLeft, 12, 19);
        }

        private static void ConfigureResultText(Text text, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, int minSize, int maxSize)
        {
            if (text == null)
            {
                return;
            }

            RectTransform rect = text.rectTransform;
            if (rect != null)
            {
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }

            text.fontSize = fontSize;
            text.alignment = alignment;
            text.resizeTextForBestFit = minSize != maxSize;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = maxSize;
        }

        private void BuildLoadoutOverlay(Transform parent)
        {
            loadoutOverlay = CreateOverlayRoot(parent, "LoadoutOverlay", new Color(0.03f, 0.05f, 0.16f, 0.78f));
            Image modal = CreatePanel(loadoutOverlay.transform, "LoadoutModal", new Vector2(0f, 28f), new Vector2(970f, 1400f), new Color(0.27f, 0.38f, 0.74f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, true);

            CreatePanel(modal.transform, "LoadoutHeader", new Vector2(0f, -18f), new Vector2(900f, 112f), new Color(0.96f, 0.80f, 0.20f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            loadoutHeaderText = CreateText(modal.transform, "LoadoutHeaderText", "이번 라운드 추천 조합", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(520f, 48f), 36, TextAnchor.MiddleCenter, true);
            loadoutSummaryText = CreateText(modal.transform, "LoadoutSummaryText", "현재 라운드 흐름에 맞춘 운영 참고용 조합입니다.", new Color(0.88f, 0.92f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(760f, 32f), 18, TextAnchor.MiddleCenter, false);
            loadoutCloseButton = CreateButton(modal.transform, "LoadoutCloseButton", "닫기", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -34f), new Vector2(94f, 68f), new Color(0.93f, 0.36f, 0.28f, 1f), HideLoadout, 24);

            loadoutCollectionButton = CreateButton(modal.transform, "LoadoutCollectionButton", "도감", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-144f, -34f), new Vector2(96f, 68f), new Color(0.53f, 0.67f, 0.96f, 1f), ToggleCollection, 24);
            BuildRecommendationNotice(modal.transform, -172f);

            CreateText(modal.transform, "DeckHeader", "추천 핵심 유닛 (참고용)", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -286f), new Vector2(620f, 32f), 24, TextAnchor.MiddleCenter, true);
            BuildLoadoutDeckCards(modal.transform);

            CreateText(modal.transform, "RosterHeader", "보유 유닛 참고", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -760f), new Vector2(620f, 32f), 24, TextAnchor.MiddleCenter, true);
            BuildLoadoutRosterCards(modal.transform);
        }

        private void BuildRecommendationNotice(Transform parent, float anchoredY)
        {
            Image notice = CreatePanel(parent, "RecommendedCompositionNotice", new Vector2(0f, anchoredY), new Vector2(650f, 64f), new Color(0.10f, 0.18f, 0.40f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreateText(notice.transform, "RecommendationNoticeTitle", "자동 추천", new Color(1f, 0.86f, 0.34f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(160f, 34f), 21, TextAnchor.MiddleLeft, true);
            CreateText(notice.transform, "RecommendationNoticeHint", "선택 기능 없음 · 소환 확률 영향 없음", new Color(0.82f, 0.90f, 1f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(420f, 34f), 18, TextAnchor.MiddleRight, false);
        }

        private void BuildLobbyFeaturedCards(Transform parent)
        {
            for (int i = 0; i < cardsPerPreset; i++)
            {
                float x = -344f + i * 172f;
                Image card = CreatePanel(parent, "LobbyFeaturedCard_" + i, new Vector2(x, -690f), new Vector2(154f, 230f), new Color(0.94f, 0.96f, 0.99f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
                Image accent = CreatePanel(card.transform, "Accent", new Vector2(0f, -8f), new Vector2(124f, 42f), Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
                Image portrait = CreatePanel(card.transform, "Portrait", new Vector2(0f, -52f), new Vector2(116f, 86f), new Color(0.84f, 0.90f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
                CreatePanel(card.transform, "LabelBack", new Vector2(0f, 38f), new Vector2(142f, 78f), new Color(0.04f, 0.07f, 0.18f, 0.82f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), true, false);
                Text nameText = CreateText(card.transform, "Name", "Hero", Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(140f, 28f), 18, TextAnchor.MiddleCenter, true);
                Text gradeText = CreateText(card.transform, "Grade", "일반", new Color(0.82f, 0.92f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(132f, 24f), 16, TextAnchor.MiddleCenter, true);
                nameText.resizeTextForBestFit = true;
                nameText.resizeTextMinSize = 12;
                nameText.resizeTextMaxSize = 18;
                gradeText.resizeTextForBestFit = true;
                gradeText.resizeTextMinSize = 11;
                gradeText.resizeTextMaxSize = 16;
                AddReadableOutline(nameText);
                AddReadableOutline(gradeText);
                lobbyFeaturedAccentImages.Add(accent);
                lobbyFeaturedPortraitImages.Add(portrait);
                lobbyFeaturedNameTexts.Add(nameText);
                lobbyFeaturedGradeTexts.Add(gradeText);
            }
        }

        private void BuildLoadoutDeckCards(Transform parent)
        {
            for (int i = 0; i < cardsPerPreset; i++)
            {
                float x = -352f + i * 176f;
                Image card = CreatePanel(parent, "LoadoutDeckCard_" + i, new Vector2(x, -384f), new Vector2(156f, 210f), new Color(0.95f, 0.97f, 0.99f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
                Image accent = CreatePanel(card.transform, "Accent", new Vector2(0f, -8f), new Vector2(130f, 54f), Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
                Image portrait = CreatePanel(card.transform, "Portrait", new Vector2(0f, -76f), new Vector2(104f, 66f), new Color(0.86f, 0.91f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
                CreatePanel(card.transform, "LabelBack", new Vector2(0f, 40f), new Vector2(140f, 74f), new Color(0.04f, 0.07f, 0.18f, 0.82f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), true, false);
                Text nameText = CreateText(card.transform, "Name", "Hero", Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(130f, 30f), 18, TextAnchor.MiddleCenter, true);
                Text detailText = CreateText(card.transform, "Detail", "일반 / 역할", new Color(0.82f, 0.92f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(132f, 36f), 15, TextAnchor.MiddleCenter, false);
                AddReadableOutline(nameText);
                AddReadableOutline(detailText);
                loadoutDeckAccentImages.Add(accent);
                loadoutDeckPortraitImages.Add(portrait);
                loadoutDeckNameTexts.Add(nameText);
                loadoutDeckDetailTexts.Add(detailText);
            }
        }

        private void BuildLoadoutRosterCards(Transform parent)
        {
            for (int i = 0; i < 8; i++)
            {
                int column = i % 2;
                int row = i / 2;
                float x = -210f + column * 420f;
                float y = -848f - row * 132f;
                Image card = CreatePanel(parent, "LoadoutRosterCard_" + i, new Vector2(x, y), new Vector2(360f, 112f), new Color(0.14f, 0.18f, 0.42f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
                Image accent = CreatePanel(card.transform, "Accent", new Vector2(18f, -16f), new Vector2(76f, 78f), Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), true, false);
                Text nameText = CreateText(card.transform, "Name", "Hero", Color.white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(114f, -18f), new Vector2(-144f, 28f), 18, TextAnchor.MiddleLeft, true);
                Text detailText = CreateText(card.transform, "Detail", "등급 / 역할 / 스킬", new Color(0.85f, 0.90f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(114f, -52f), new Vector2(-144f, 44f), 15, TextAnchor.MiddleLeft, false);
                loadoutRosterAccentImages.Add(accent);
                loadoutRosterPortraitImages.Add(accent);
                loadoutRosterNameTexts.Add(nameText);
                loadoutRosterDetailTexts.Add(detailText);
            }
        }

        private Text CreateRewardChip(Transform parent, string name, string title, Vector2 anchoredPosition, Color accentColor, string value)
        {
            Image chip = CreatePanel(parent, name, anchoredPosition, new Vector2(200f, 132f), new Color(0.96f, 0.97f, 0.99f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            CreatePanel(chip.transform, "Accent", new Vector2(0f, -10f), new Vector2(124f, 42f), accentColor, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreateText(chip.transform, "Title", title, new Color(0.22f, 0.26f, 0.38f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(124f, 22f), 16, TextAnchor.MiddleCenter, true);
            return CreateText(chip.transform, "Value", value, accentColor, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(150f, 40f), 30, TextAnchor.MiddleCenter, true);
        }

        private void WireButtons()
        {
            if (battleButton != null)
            {
                battleButton.onClick.RemoveAllListeners();
                AddButtonListener(battleButton, HandleBattlePressed);
            }

            if (lobbyButton != null)
            {
                lobbyButton.onClick.RemoveAllListeners();
                AddButtonListener(lobbyButton, HandleExitToOutgamePressed);
            }

            if (loadoutButton != null)
            {
                loadoutButton.onClick.RemoveAllListeners();
                AddButtonListener(loadoutButton, ToggleLoadout);
            }

            if (lobbyCollectionButton != null)
            {
                lobbyCollectionButton.onClick.RemoveAllListeners();
                AddButtonListener(lobbyCollectionButton, ToggleCollection);
            }

            if (lobbyShopButton != null)
            {
                lobbyShopButton.onClick.RemoveAllListeners();
                AddButtonListener(lobbyShopButton, ToggleShop);
            }

            if (lobbyModeButton != null)
            {
                lobbyModeButton.onClick.RemoveAllListeners();
                AddButtonListener(lobbyModeButton, TogglePlayMode);
            }

            if (loadoutCollectionButton != null)
            {
                loadoutCollectionButton.onClick.RemoveAllListeners();
                AddButtonListener(loadoutCollectionButton, ToggleCollection);
            }
        }

        private void BuildPresets()
        {
            presets.Clear();

            string[] names =
            {
                "안정 성장",
                "웨이브 정리",
                "제어 운영",
                "보스 대비"
            };

            string[] descriptions =
            {
                "안정적인 초반 운영과 균형 잡힌 합성 흐름을 위한 추천입니다.",
                "공격 템포와 광역 압박으로 몰려오는 웨이브를 정리하는 추천입니다.",
                "마나 순환과 군중 제어형 스킬을 활용하는 운영 추천입니다.",
                "보스 라운드에 대비해 집중 화력과 유지력을 챙기는 추천입니다."
            };

            Color[] colors =
            {
                new Color(0.19f, 0.92f, 0.92f),
                new Color(1f, 0.58f, 0.26f),
                new Color(0.40f, 0.86f, 0.44f),
                new Color(1f, 0.38f, 0.46f)
            };

            int characterCount = GetDeployableCharacterCount();
            int safePresetCount = Mathf.Max(1, presetCount);

            for (int i = 0; i < safePresetCount; i++)
            {
                PresetDefinition preset = new PresetDefinition
                {
                    name = names[i % names.Length],
                    description = descriptions[i % descriptions.Length],
                    accentColor = colors[i % colors.Length]
                };

                if (characterCount > 0)
                {
                    int startIndex = (i * cardsPerPreset) % characterCount;
                    for (int slotIndex = 0; slotIndex < cardsPerPreset; slotIndex++)
                    {
                        preset.characterIndices.Add((startIndex + slotIndex) % characterCount);
                    }
                }

                presets.Add(preset);
            }
        }

        private void ApplyRecommendedPreset()
        {
            if (presets.Count == 0)
            {
                return;
            }

            int upcomingRound = ResolveUpcomingRecommendationRound();
            selectedPresetIndex = ResolveRecommendedPresetIndex(upcomingRound);
            PresetDefinition preset = presets[selectedPresetIndex];

            if (lobbyPresetNameText != null)
            {
                lobbyPresetNameText.text = "R" + upcomingRound + " 추천 · " + preset.name;
                RuntimeUiSkinUtility.ApplyReadableTextColor(lobbyPresetNameText, preset.accentColor, uiSkin);
            }

            if (lobbyPresetDescriptionText != null)
            {
                lobbyPresetDescriptionText.text = preset.description;
            }

            if (loadoutHeaderText != null)
            {
                loadoutHeaderText.text = "R" + upcomingRound + " 추천 조합 · " + preset.name;
            }

            if (loadoutSummaryText != null)
            {
                loadoutSummaryText.text = preset.description;
            }

            UpdateLobbyFeaturedCards(preset);
            UpdateLoadoutDeckCards(preset);
            UpdateLoadoutRosterCards(preset);
        }

        private int ResolveUpcomingRecommendationRound()
        {
            if (gameController == null)
            {
                return 1;
            }

            int currentRound = Mathf.Max(0, gameController.CurrentRound);
            return gameController.IsRoundRunning ? Mathf.Max(1, currentRound) : currentRound + 1;
        }

        private int ResolveRecommendedPresetIndex(int round)
        {
            int cycleRound = ((Mathf.Max(1, round) - 1) % 10) + 1;
            int recommendedIndex = cycleRound <= 3 ? 0 : cycleRound <= 6 ? 1 : cycleRound <= 9 ? 2 : 3;
            return Mathf.Clamp(recommendedIndex, 0, Mathf.Max(0, presets.Count - 1));
        }

        private void UpdateLobbyFeaturedCards(PresetDefinition preset)
        {
            for (int i = 0; i < lobbyFeaturedNameTexts.Count; i++)
            {
                CharacterDefinition definition = GetPresetCharacter(preset, i);
                if (definition == null)
                {
                    lobbyFeaturedAccentImages[i].color = new Color(0.75f, 0.78f, 0.86f);
                    ApplyCharacterPortrait(lobbyFeaturedPortraitImages, i, null);
                    lobbyFeaturedNameTexts[i].text = "비어 있음";
                    lobbyFeaturedGradeTexts[i].text = "미설정";
                    SetCardLabelColors(lobbyFeaturedNameTexts[i], lobbyFeaturedGradeTexts[i]);
                    continue;
                }

                lobbyFeaturedAccentImages[i].color = GetGradeColor(definition.grade, definition.accentColor);
                ApplyCharacterPortrait(lobbyFeaturedPortraitImages, i, definition);
                lobbyFeaturedNameTexts[i].text = definition.displayName;
                lobbyFeaturedGradeTexts[i].text = GetGradeName(definition.grade) + " / " + BuildCardLevelLabel(definition);
                SetCardLabelColors(lobbyFeaturedNameTexts[i], lobbyFeaturedGradeTexts[i]);
            }
        }

        private void UpdateLoadoutDeckCards(PresetDefinition preset)
        {
            for (int i = 0; i < loadoutDeckNameTexts.Count; i++)
            {
                CharacterDefinition definition = GetPresetCharacter(preset, i);
                if (definition == null)
                {
                    loadoutDeckAccentImages[i].color = new Color(0.78f, 0.82f, 0.88f);
                    ApplyCharacterPortrait(loadoutDeckPortraitImages, i, null);
                    loadoutDeckNameTexts[i].text = "빈 슬롯";
                    loadoutDeckDetailTexts[i].text = "유닛을 추가하면 여기에 표시됩니다.";
                    SetCardLabelColors(loadoutDeckNameTexts[i], loadoutDeckDetailTexts[i]);
                    continue;
                }

                loadoutDeckAccentImages[i].color = GetGradeColor(definition.grade, definition.accentColor);
                ApplyCharacterPortrait(loadoutDeckPortraitImages, i, definition);
                loadoutDeckNameTexts[i].text = definition.displayName;
                loadoutDeckDetailTexts[i].text = GetGradeName(definition.grade) + " / " + BuildCardLevelLabel(definition);
                SetCardLabelColors(loadoutDeckNameTexts[i], loadoutDeckDetailTexts[i]);
            }
        }

        private void UpdateLoadoutRosterCards(PresetDefinition preset)
        {
            int characterCount = GetDeployableCharacterCount();
            int startIndex = characterCount <= 0 ? 0 : (selectedPresetIndex * 7) % characterCount;

            for (int i = 0; i < loadoutRosterNameTexts.Count; i++)
            {
                CharacterDefinition definition = GetCharacter(startIndex + i);
                if (definition == null)
                {
                    loadoutRosterAccentImages[i].color = new Color(0.72f, 0.76f, 0.84f);
                    ApplyCharacterPortrait(loadoutRosterPortraitImages, i, null);
                    loadoutRosterNameTexts[i].text = "후보 없음";
                    loadoutRosterDetailTexts[i].text = "캐릭터 데이터가 늘어나면 이 자리가 채워집니다.";
                    continue;
                }

                loadoutRosterAccentImages[i].color = GetGradeColor(definition.grade, definition.accentColor);
                ApplyCharacterPortrait(loadoutRosterPortraitImages, i, definition);
                loadoutRosterNameTexts[i].text = definition.displayName;
                string skillSummary = definition.skills != null && definition.skills.Count > 0
                    ? definition.skills[0].displayName
                    : "기본 공격";
                loadoutRosterDetailTexts[i].text = GetGradeName(definition.grade) + " / " + BuildCardLevelLabel(definition) + " / " + skillSummary;
            }
        }

        private CharacterDefinition GetPresetCharacter(PresetDefinition preset, int slotIndex)
        {
            if (preset == null || slotIndex < 0 || slotIndex >= preset.characterIndices.Count)
            {
                return null;
            }

            return GetCharacter(preset.characterIndices[slotIndex]);
        }

        private CharacterDefinition GetCharacter(int index)
        {
            if (characterDatabase == null)
            {
                return null;
            }

            if (index < 0)
            {
                return null;
            }

            List<CharacterDefinition> deployableCharacters = characterDatabase.GetDeployableCharacters();
            if (deployableCharacters.Count == 0)
            {
                return null;
            }

            int safeIndex = index % deployableCharacters.Count;
            return deployableCharacters[safeIndex];
        }

        private int GetDeployableCharacterCount()
        {
            return characterDatabase != null ? characterDatabase.GetDeployableCharacters().Count : 0;
        }

        private void HandleBattlePressed()
        {
            if (gameController == null || buttonBinder == null || gameController.IsRoundRunning)
            {
                return;
            }

            if (matchmakingRoutine != null)
            {
                StopCoroutine(matchmakingRoutine);
                matchmakingRoutine = null;
            }

            if (resultOverlay != null && resultOverlay.activeSelf)
            {
                HideResult();
            }

            if (characterCollectionUI != null)
            {
                characterCollectionUI.Close();
            }

            HideLoadout();
            HideShop();
            HideLobby();
            HideMatchmaking();
            buttonBinder.OnClickStartRound();
        }

        private IEnumerator RunMatchmaking()
        {
            float elapsed = 0f;
            while (elapsed < matchmakingDuration)
            {
                elapsed += Time.deltaTime;
                if (queueTimerText != null)
                {
                    queueTimerText.text = elapsed.ToString("00.00");
                }

                if (queueStatusText != null)
                {
                    queueStatusText.text = elapsed < matchmakingDuration * 0.55f
                        ? "라운드 전장을 준비하는 중..."
                        : "전투 필드를 정리하는 중...";
                }

                yield return null;
            }

            HideMatchmaking();
            buttonBinder.OnClickStartRound();
            matchmakingRoutine = null;
        }

        private void CancelMatchmaking()
        {
            if (matchmakingRoutine != null)
            {
                StopCoroutine(matchmakingRoutine);
                matchmakingRoutine = null;
            }

            HideMatchmaking();
            ShowLobby();
        }

        private void HandleExitToOutgamePressed()
        {
            ShowExitConfirm();
        }

        private void ShowExitConfirm()
        {
            if (exitConfirmOverlay == null)
            {
                ConfirmExitToOutgame();
                return;
            }

            exitConfirmOverlay.SetActive(true);
        }

        private void HideExitConfirm()
        {
            if (exitConfirmOverlay != null)
            {
                exitConfirmOverlay.SetActive(false);
            }
        }

        private void ConfirmExitToOutgame()
        {
            if (gameController != null)
            {
                gameController.ExitToOutgame();
            }

            HideExitConfirm();
            HideResult();
            HideLoadout();
            HideShop();
            HideOutgamePlaceholder();
            ShowLobby();
        }

        private void HandleRoundStarted(int round)
        {
            defeatPresented = false;
            resultRewardGranted = false;
            SetGameplayHudVisible(true);
            HideLobby();
            HideMatchmaking();
            HideLoadout();
            HideOutgamePlaceholder();
            HideShop();
            HideResult();
            HideExitConfirm();
        }

        private void HandleRoundCompleted(int round)
        {
            if (defeatPresented)
            {
                return;
            }

            if (resultRoutine != null)
            {
                StopCoroutine(resultRoutine);
            }

            RuntimeAudioUtility.PlayVictory();
            resultRoutine = StartCoroutine(ShowRoundResultAfterFlow(round));
        }

        private IEnumerator ShowRoundResultAfterFlow(int round)
        {
            if (augmentManager != null && (augmentManager.IsChoiceOpen || augmentManager.WillOfferChoice(round)))
            {
                while (augmentManager.IsChoiceOpen)
                {
                    yield return null;
                }

                yield return new WaitForSeconds(0.25f);
            }
            else
            {
                yield return new WaitForSeconds(0.35f);
            }

            if (gameController != null && gameController.Life > 0 && !defeatPresented)
            {
                ShowResult(true, round);
            }

            resultRoutine = null;
        }

        private void HandleGameOver()
        {
            defeatPresented = true;

            if (resultRoutine != null)
            {
                StopCoroutine(resultRoutine);
                resultRoutine = null;
            }

            HideMatchmaking();
            HideLoadout();
            HideShop();
            resultRoutine = StartCoroutine(ShowGameOverResultAfterCinematic(gameController != null ? gameController.CurrentRound : 0));
        }

        private IEnumerator ShowGameOverResultAfterCinematic(int round)
        {
            float minimumDelay = DefenseGameController.DefeatSlowMotionDurationRealtime + 0.15f;
            yield return new WaitForSecondsRealtime(Mathf.Max(minimumDelay, defeatResultRevealDelay));
            ShowResult(false, round);
            resultRoutine = null;
        }

        private void ShowLobby()
        {
            if (gameController != null && gameController.IsRoundRunning)
            {
                return;
            }

            ApplyRecommendedPreset();
            SetGameplayHudVisible(false);
            if (lobbyOverlay != null)
            {
                lobbyOverlay.SetActive(true);
            }

            HighlightOutgameNav(hubLobbyButton);
        }

        private void HideLobby()
        {
            if (lobbyOverlay != null)
            {
                lobbyOverlay.SetActive(false);
            }
        }

        private void HandleLobbyPressed()
        {
            if (gameController != null && gameController.IsRoundRunning)
            {
                return;
            }

            HideResult();
            HideLoadout();
            HideShop();
            HideOutgamePlaceholder();
            HideExitConfirm();
            ShowLobby();
        }

        private void ShowLobbyTab()
        {
            HandleLobbyPressed();
        }

        private void ToggleLoadout()
        {
            if (gameController != null && gameController.IsRoundRunning)
            {
                return;
            }

            if (loadoutOverlay != null && loadoutOverlay.activeSelf)
            {
                HideLoadout();
            }
            else
            {
                SetGameplayHudVisible(false);
                HideResult();
                HideShop();
                HideOutgamePlaceholder();
                HideExitConfirm();
                ShowLoadout();
                HighlightOutgameNav(hubInventoryButton);
            }
        }

        public void ToggleCollectionPanel()
        {
            ToggleCollection();
        }

        private void ToggleCollection()
        {
            if (characterCollectionUI == null)
            {
                return;
            }

            bool willOpen = !characterCollectionUI.IsOpen;
            if (willOpen)
            {
                SetGameplayHudVisible(false);
                HideResult();
                HideLoadout();
                HideShop();
                HideOutgamePlaceholder();
                HideExitConfirm();

                if (ShouldShowOutgameLobbyAfterCollection())
                {
                    ShowLobby();
                }
                else
                {
                    HideLobby();
                }

                HighlightOutgameNav(hubInventoryButton);
            }

            characterCollectionUI.Toggle();
        }

        private void HandleCollectionClosed()
        {
            if (matchmakingOverlay != null && matchmakingOverlay.activeSelf ||
                resultOverlay != null && resultOverlay.activeSelf ||
                shopOverlay != null && shopOverlay.activeSelf ||
                loadoutOverlay != null && loadoutOverlay.activeSelf ||
                outgamePlaceholderOverlay != null && outgamePlaceholderOverlay.activeSelf)
            {
                return;
            }

            if (ShouldShowOutgameLobbyAfterCollection())
            {
                SetGameplayHudVisible(false);
                ShowLobby();
                HighlightOutgameNav(hubLobbyButton);
                return;
            }

            HideLobby();
            SetGameplayHudVisible(true);
        }

        private bool ShouldShowOutgameLobbyAfterCollection()
        {
            return gameController == null || (!gameController.IsRoundRunning && gameController.CurrentRound <= 0);
        }

        private void ToggleShop()
        {
            if (gameController != null && gameController.IsRoundRunning)
            {
                return;
            }

            if (shopScene.IsValid() && shopScene.isLoaded)
            {
                HideShop();
            }
            else
            {
                SetGameplayHudVisible(false);
                HideLoadout();
                HideResult();
                HideOutgamePlaceholder();
                HideExitConfirm();
                ShowShop();
                HighlightOutgameNav(hubShopButton);
            }
        }

        private void ShowShop()
        {
            if (!shopScene.IsValid() || !shopScene.isLoaded)
            {
                BuildShopScene();
            }

            RefreshShop();
            if (shopOverlay != null)
            {
                shopOverlay.SetActive(true);
            }

            if (shopScene.IsValid() && shopScene.isLoaded)
            {
                SceneManager.SetActiveScene(shopScene);
            }
        }

        private void HideShop()
        {
            if (drawRevealRoutine != null)
            {
                StopCoroutine(drawRevealRoutine);
                drawRevealRoutine = null;
            }

            if (gameplayScene.IsValid() && gameplayScene.isLoaded)
            {
                SceneManager.SetActiveScene(gameplayScene);
            }

            if (shopScene.IsValid() && shopScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(shopScene);
            }

            shopScene = default(Scene);
            shopSceneCanvasRoot = null;
            shopOverlay = null;
            shopDiamondText = null;
            shopRatesText = null;
            shopCollectionText = null;
            shopResultText = null;
            shopModeText = null;
            shopSingleDrawButton = null;
            shopTenDrawButton = null;
            shopTestDiamondButton = null;

            if (lobbyOverlay != null && lobbyOverlay.activeSelf)
            {
                HighlightOutgameNav(hubLobbyButton);
            }
        }

        private void BuildShopScene()
        {
            gameplayScene = gameObject.scene;
            shopScene = SceneManager.CreateScene("OutgameShop");
            shopSceneCanvasRoot = new GameObject("OutgameShopCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(shopSceneCanvasRoot, shopScene);

            Canvas shopCanvas = shopSceneCanvasRoot.GetComponent<Canvas>();
            shopCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            shopCanvas.sortingOrder = 300;

            CanvasScaler scaler = shopSceneCanvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.84f;
            shopSceneCanvasRoot.AddComponent<RuntimeKoreanTextCleaner>();
            BuildShopOverlay(shopSceneCanvasRoot.transform);
        }

        private void TogglePlayMode()
        {
            if (outgameProgression == null || gameController != null && gameController.IsRoundRunning)
            {
                return;
            }

            OutgamePlayMode nextMode = outgameProgression.IsTestMode ? OutgamePlayMode.Service : OutgamePlayMode.Test;
            gameController?.ClearBoardForProfileChange();
            outgameProgression.SwitchPlayMode(nextMode);
        }

        private void RechargeTestDiamonds()
        {
            outgameProgression?.RechargeTestDiamonds();
        }

        private void TryOpenChest(int drawCount)
        {
            if (outgameProgression == null || drawRevealRoutine != null)
            {
                return;
            }

            if (!outgameProgression.TryOpenChest(drawCount, out List<OutgameDrawResult> results))
            {
                if (shopResultText != null)
                {
                    shopResultText.text = "다이아가 부족합니다.";
                }

                RefreshShop();
                return;
            }

            RuntimeAudioUtility.PlayReroll();
            RefreshShop();
            drawRevealRoutine = StartCoroutine(RevealDrawResults(results));
        }

        private IEnumerator RevealDrawResults(List<OutgameDrawResult> results)
        {
            SetShopDrawButtonsInteractable(false);
            if (shopResultText != null)
            {
                shopResultText.text = "상자를 여는 중...";
            }

            yield return new WaitForSeconds(0.45f);
            string output = string.Empty;
            for (int i = 0; i < results.Count; i++)
            {
                OutgameDrawResult result = results[i];
                if (result == null || result.character == null)
                {
                    continue;
                }

                string status = result.firstAcquisition ? "NEW" : result.leveledUp ? "LEVEL UP" : "카드 획득";
                output += "[" + status + "] " + result.character.displayName + "  Lv." + result.level;
                output += result.requiredCopies > 0 ? "  (" + result.remainingCopies + "/" + result.requiredCopies + ")\n" : "  (MAX)\n";
                if (shopResultText != null)
                {
                    shopResultText.text = output;
                }

                yield return new WaitForSeconds(0.12f);
            }

            if (shopResultText != null && string.IsNullOrEmpty(output))
            {
                shopResultText.text = "획득 결과가 없습니다.";
            }

            SetShopDrawButtonsInteractable(true);
            drawRevealRoutine = null;
        }

        private void RefreshShop()
        {
            if (outgameProgression == null)
            {
                return;
            }

            if (shopDiamondText != null)
            {
                shopDiamondText.text = "DIA " + outgameProgression.Diamonds;
            }

            if (shopRatesText != null)
            {
                shopRatesText.text = outgameProgression.BuildRateText();
            }

            if (shopCollectionText != null)
            {
                shopCollectionText.text = outgameProgression.BuildCollectionSummary();
            }

            if (shopModeText != null)
            {
                shopModeText.text = outgameProgression.IsTestMode ? "TEST MODE / 전체 영웅 사용 가능" : "SERVICE MODE / 보유 영웅만 출전";
            }

            if (shopTestDiamondButton != null)
            {
                shopTestDiamondButton.gameObject.SetActive(outgameProgression.IsTestMode);
                Text label = shopTestDiamondButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = "다이아 +" + outgameProgression.Settings.testDiamondRechargeAmount.ToString("N0");
                }
            }

            if (shopSingleDrawButton != null)
            {
                Text label = shopSingleDrawButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = "1회 뽑기  " + outgameProgression.Settings.singleChestCost + " DIA";
                }
            }

            if (shopTenDrawButton != null)
            {
                Text label = shopTenDrawButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = "10회 뽑기  " + outgameProgression.Settings.tenChestCost + " DIA";
                }
            }
        }

        private void RefreshModeUi()
        {
            if (outgameProgression == null)
            {
                return;
            }

            if (lobbyModeText != null)
            {
                lobbyModeText.text = outgameProgression.IsTestMode ? "TEST MODE" : "SERVICE";
                lobbyModeText.color = outgameProgression.IsTestMode ? new Color(1f, 0.83f, 0.34f) : new Color(0.43f, 1f, 0.80f);
            }

            if (lobbyModeButton != null)
            {
                Text label = lobbyModeButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = outgameProgression.IsTestMode ? "서비스 진입" : "테스트 진입";
                }
            }

            if (lobbyFortuneText != null)
            {
                lobbyFortuneText.text = DailyFortuneSystem.TodaySummary;
            }
        }

        private void SetGameplayHudVisible(bool visible)
        {
            if (gameplayHudRoot != null)
            {
                gameplayHudRoot.SetActive(visible);
            }
        }

        private void HighlightOutgameNav(Button activeButton)
        {
            SetOutgameNavButtonState(hubShopButton, activeButton == hubShopButton);
            SetOutgameNavButtonState(hubInventoryButton, activeButton == hubInventoryButton);
            SetOutgameNavButtonState(hubLobbyButton, activeButton == hubLobbyButton);
            SetOutgameNavButtonState(hubYahtzeeButton, activeButton == hubYahtzeeButton);
            SetOutgameNavButtonState(hubRankingButton, activeButton == hubRankingButton);
        }

        private void SetOutgameNavButtonState(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? new Color(0.28f, 0.55f, 1f, 1f) : new Color(0.93f, 0.96f, 1f, 0.98f);
            }

            Text label = GetChildText(button.transform, "NavLabel");
            if (label != null)
            {
                label.color = active ? Color.white : new Color(0.20f, 0.25f, 0.42f);
            }
        }

        private void SetShopDrawButtonsInteractable(bool interactable)
        {
            if (shopSingleDrawButton != null)
            {
                shopSingleDrawButton.interactable = interactable;
            }

            if (shopTenDrawButton != null)
            {
                shopTenDrawButton.interactable = interactable;
            }
        }

        private void HandleProgressChanged()
        {
            RefreshShop();
            RefreshModeUi();
            BuildPresets();
            ApplyRecommendedPreset();
        }

        private void ShowLoadout()
        {
            ApplyRecommendedPreset();
            if (loadoutOverlay != null)
            {
                loadoutOverlay.SetActive(true);
            }
        }

        private void HideLoadout()
        {
            if (loadoutOverlay != null)
            {
                loadoutOverlay.SetActive(false);
            }

            if (lobbyOverlay != null && lobbyOverlay.activeSelf)
            {
                HighlightOutgameNav(hubLobbyButton);
            }
        }

        private void ShowOutgamePlaceholder(string title, string body)
        {
            SetGameplayHudVisible(false);
            HideLoadout();
            HideShop();
            HideResult();
            HideExitConfirm();
            ShowLobby();

            if (outgamePlaceholderOverlay != null)
            {
                Text titleText = GetChildText(outgamePlaceholderOverlay.transform, "PlaceholderTitle");
                if (titleText != null)
                {
                    titleText.text = title;
                }

                Text bodyText = GetChildText(outgamePlaceholderOverlay.transform, "PlaceholderBody");
                if (bodyText != null)
                {
                    bodyText.text = body;
                }

                outgamePlaceholderOverlay.SetActive(true);
            }

            HighlightOutgameNav(title.Contains("랭킹") ? hubRankingButton : hubYahtzeeButton);
        }

        private void ShowSeasonRanking()
        {
            string body = outgameProgression != null
                ? outgameProgression.BuildSeasonRankingSummary()
                : "주간 보스 점수, 협동 MVP, 시즌 미션 보상을 불러올 수 없습니다.";
            ShowOutgamePlaceholder("시즌 랭킹", body);
        }

        private void HideOutgamePlaceholder()
        {
            if (outgamePlaceholderOverlay != null)
            {
                outgamePlaceholderOverlay.SetActive(false);
            }
        }

        private void ShowMatchmaking()
        {
            SetGameplayHudVisible(false);
            HideExitConfirm();
            if (matchmakingOverlay != null)
            {
                matchmakingOverlay.SetActive(true);
            }

            if (queueTimerText != null)
            {
                queueTimerText.text = "00.00";
            }

            if (queueStatusText != null)
            {
                queueStatusText.text = "라운드 전장을 준비하는 중...";
            }

            RuntimeAudioUtility.PlayMatching();
        }

        private void HideMatchmaking()
        {
            if (matchmakingOverlay != null)
            {
                matchmakingOverlay.SetActive(false);
            }
        }

        private void ShowResult(bool victory, int round)
        {
            if (resultOverlay == null)
            {
                return;
            }

            HideExitConfirm();
            resultOverlay.SetActive(true);
            Color accent = victory ? new Color(1f, 0.84f, 0.18f) : new Color(1f, 0.45f, 0.45f);

            if (resultTitleText != null)
            {
                resultTitleText.text = victory ? "승리" : "패배";
                RuntimeUiSkinUtility.ApplyReadableTextColor(resultTitleText, accent, uiSkin);
            }

            if (resultSummaryText != null)
            {
                resultSummaryText.text = victory ? "라운드 " + round + " 클리어" : "라운드 " + round + " 에서 패배";
            }

            if (resultMetaText != null)
            {
                resultMetaText.text = victory
                    ? "연속 클리어 +1  |  다음 라운드 준비 완료"
                    : "덱을 다시 정비하고 재도전할 수 있습니다.";
            }

            if (resultScoreText != null)
            {
                resultScoreText.text = victory ? "RUN SCORE A / 000점" : "RUN SCORE C / 000점";
            }

            if (resultRecapText != null)
            {
                resultRecapText.text = "딜러 기록 없음  |  시너지 없음\n타일 기여 없음  |  초반 1~10R 계측 대기";
            }

            if (resultNextText != null)
            {
                resultNextText.text = victory
                    ? "다음 행동\n도감 강화: 주력 카드 성장 확인\n상점 뽑기: 부족한 등급 보충\n다음 보스 대비: 보스 타일과 딜러 유지"
                    : "다음 행동\n도감 강화: 약한 주력 카드 보강\n상점 뽑기: 부족한 등급 보충\n다음 보스 대비: 실패 원인 재정비";
            }

            if (gameController != null)
            {
                if (resultTitleText != null)
                {
                    resultTitleText.text = victory ? "승리" : "패배";
                }

                if (resultSummaryText != null)
                {
                    resultSummaryText.text = gameController.RunNextGoalHeadline;
                }

                if (resultMetaText != null)
                {
                    RectTransform metaRect = resultMetaText.rectTransform;
                    if (metaRect != null)
                    {
                        metaRect.sizeDelta = new Vector2(660f, 44f);
                    }

                    resultMetaText.fontSize = 22;
                    resultMetaText.alignment = TextAnchor.MiddleCenter;
                    resultMetaText.text = victory ? "이번 결과: 라운드 " + round + " 클리어" : "이번 결과: 라운드 " + round + " 패배";
                }

                if (resultScoreText != null)
                {
                    resultScoreText.text = "RUN SCORE " + gameController.RunPerformanceGrade + " / " + gameController.RunPerformanceScore + "점";
                }

                if (resultRecapText != null)
                {
                    resultRecapText.text = gameController.RunResultFocusSummary;
                }

                if (resultNextText != null)
                {
                    resultNextText.text = gameController.RunResultNextCompactSummary;
                }
            }

            ApplyReadableResultTextLayout();

            int goldReward = victory && gameController != null && gameController.LastRoundClearGoldReward > 0
                ? gameController.LastRoundClearGoldReward
                : victory ? 110 + round * 18 : Mathf.Max(20, 40 + round * 6);
            int coreReward = victory ? 6 + Mathf.Max(1, round / 2) : gameController != null ? gameController.EarnedGrowthCurrency : Mathf.Max(2, round / 3 + 2);
            int diamondReward = outgameProgression != null ? outgameProgression.ResolveBattleDiamondReward(coreReward) : coreReward;
            if (!resultRewardGranted && outgameProgression != null)
            {
                outgameProgression.AddDiamonds(diamondReward);
                if (gameController != null)
                {
                    outgameProgression.RecordSeasonRun(
                        gameController.RunPerformanceScore,
                        gameController.RunBossScore,
                        gameController.RunBossKillCount,
                        gameController.RunMvpName,
                        round,
                        victory);
                }

                resultRewardGranted = true;
            }

            if (resultMetaText != null && gameController != null)
            {
                string meta = victory ? "이번 결과: 라운드 " + round + " 클리어" : "이번 결과: 라운드 " + round + " 패배";
                if (outgameProgression != null && !string.IsNullOrWhiteSpace(outgameProgression.LastSeasonRewardSummary))
                {
                    meta += "  |  " + outgameProgression.LastSeasonRewardSummary;
                }

                resultMetaText.text = meta;
            }

            if (resultNextText != null && gameController != null)
            {
                string loopSummary = outgameProgression != null
                    ? outgameProgression.BuildSeasonResultLoopSummary()
                    : gameController.SeasonReplayDigestSummary;
                if (!string.IsNullOrWhiteSpace(loopSummary))
                {
                    resultNextText.text = gameController.RunResultNextCompactSummary + "\n" + loopSummary;
                }
            }

            if (resultRewardGoldText != null)
            {
                resultRewardGoldText.text = "+" + goldReward;
            }

            if (resultRewardCoreText != null)
            {
                resultRewardCoreText.text = "+" + diamondReward;
            }

            if (resultContinueButton != null)
            {
                RectTransform continueRect = resultContinueButton.GetComponent<RectTransform>();
                if (continueRect != null)
                {
                    continueRect.anchoredPosition = victory ? new Vector2(0f, 58f) : new Vector2(190f, 58f);
                    continueRect.sizeDelta = victory ? new Vector2(340f, 78f) : new Vector2(220f, 72f);
                }

                SetButtonLabel(resultContinueButton, victory ? "계속하기" : "재정비");
            }

            if (resultRetryButton != null)
            {
                resultRetryButton.gameObject.SetActive(!victory);
                if (!victory)
                {
                    SetButtonLabel(resultRetryButton, "새 판 다시하기");
                }
            }
        }

        private void HideResult()
        {
            if (resultOverlay != null)
            {
                resultOverlay.SetActive(false);
            }
        }

        private void ContinueFromResult()
        {
            HideResult();
            if (gameController != null && !gameController.IsRoundRunning && defeatPresented)
            {
                defeatPresented = false;
                gameController.ResetRunForRetry();
                ShowLobby();
            }
        }

        private void RetryFromResult()
        {
            bool shouldResetRun = defeatPresented;
            defeatPresented = false;
            HideResult();
            if (shouldResetRun && gameController != null && !gameController.IsRoundRunning)
            {
                gameController.ResetRunForRetry();
            }

            HandleBattlePressed();
        }

        private GameObject CreateOverlayRoot(Transform parent, string name, Color blockerColor)
        {
            GameObject overlay = new GameObject(name, typeof(RectTransform));
            overlay.transform.SetParent(parent, false);
            Image blocker = overlay.AddComponent<Image>();
            blocker.color = blockerColor;
            blocker.raycastTarget = true;

            RectTransform rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return overlay;
        }

        private Image CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, bool rounded, bool shadow)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform));
            panelObject.transform.SetParent(parent, false);
            Image image = panelObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            RuntimeUiSkinUtility.ApplyImageSkin(image, uiSkin, name, false, rounded);
            RollRollUiResource.TryApplyElementSprite(image, name, false, rounded);

            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            if (shadow)
            {
                Shadow shadowComponent = panelObject.AddComponent<Shadow>();
                shadowComponent.effectColor = new Color(0f, 0f, 0f, 0.35f);
                shadowComponent.effectDistance = new Vector2(0f, -7f);
            }

            return image;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color backgroundColor, UnityEngine.Events.UnityAction onClick, int fontSize)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = backgroundColor;
            RuntimeUiSkinUtility.ApplyImageSkin(image, uiSkin, name, true, true);
            RollRollUiResource.TryApplyElementSprite(image, name, true, true);
            image.raycastTarget = true;

            Shadow shadow = buttonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.34f);
            shadow.effectDistance = new Vector2(0f, -6f);

            Button button = buttonObject.AddComponent<Button>();
            AddButtonListener(button, onClick);

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            CreateText(buttonObject.transform, "Label", label, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, fontSize, TextAnchor.MiddleCenter, true);
            return button;
        }

        private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction onClick)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.AddListener(RuntimeAudioUtility.PlayButton);
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }
        }

        private Text CreateText(Transform parent, string name, string value, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, bool bold)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.text = RuntimeKoreanTextUtility.Clean(name, value);
            text.color = RuntimeUiSkinUtility.ResolveReadableTextColor(parent, color, uiSkin);
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.raycastTarget = false;

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.38f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        private Text GetChildText(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child.GetComponent<Text>();
            }

            Text[] texts = parent.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == childName)
                {
                    return texts[i];
                }
            }

            return null;
        }

        private void MoveRectInto(Transform newParent, Transform oldParent, string childName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            if (newParent == null || oldParent == null || string.IsNullOrWhiteSpace(childName))
            {
                return;
            }

            Transform child = oldParent.Find(childName);
            if (child == null)
            {
                return;
            }

            child.SetParent(newParent, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, pivot, anchoredPosition, size);
        }

        private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private void SetCardLabelColors(Text primaryText, Text secondaryText)
        {
            if (primaryText != null)
            {
                primaryText.color = new Color(0.98f, 0.99f, 1f, 1f);
                AddReadableOutline(primaryText);
            }

            if (secondaryText != null)
            {
                secondaryText.color = new Color(0.82f, 0.92f, 1f, 1f);
                AddReadableOutline(secondaryText);
            }
        }

        private void ApplyCharacterPortrait(List<Image> portraitImages, int index, CharacterDefinition definition)
        {
            if (portraitImages == null || index < 0 || index >= portraitImages.Count)
            {
                return;
            }

            Image portraitImage = portraitImages[index];
            if (portraitImage == null)
            {
                return;
            }

            Sprite sprite = RollRollUiResource.ResolveCharacterSprite(definition);
            if (sprite != null && definition != null)
            {
                portraitImage.sprite = sprite;
                portraitImage.type = Image.Type.Simple;
                portraitImage.preserveAspect = true;
                portraitImage.color = Color.white;
                return;
            }

            RollRollUiResource.TryApplyElementSprite(portraitImage, "Portrait", false, true);
            portraitImage.color = new Color(0.80f, 0.84f, 0.95f, 1f);
        }

        private void AddReadableOutline(Text text)
        {
            if (text == null)
            {
                return;
            }

            Outline outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.76f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private Sprite GetRoundedSprite()
        {
            if (roundedSprite != null)
            {
                return roundedSprite;
            }

            int size = 64;
            float radius = 18f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nearestX = Mathf.Clamp(x, radius, size - radius - 1f);
                    float nearestY = Mathf.Clamp(y, radius, size - radius - 1f);
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return roundedSprite;
        }

        private string GetGradeName(CharacterGrade grade)
        {
            return CharacterGradeUtility.GetDisplayName(grade);
        }

        private string BuildCardLevelLabel(CharacterDefinition definition)
        {
            if (definition == null || outgameProgression == null)
            {
                return "미획득";
            }

            int level = outgameProgression.GetDisplayCardLevel(definition.id);
            if (level > 0)
            {
                return "Lv." + level;
            }

            return "미획득";
        }

        private string GetRoleName(CharacterRole role)
        {
            if (role == CharacterRole.Vanguard) return "전위";
            if (role == CharacterRole.Ranger) return "사수";
            if (role == CharacterRole.Mage) return "마법";
            if (role == CharacterRole.Support) return "지원";
            if (role == CharacterRole.Assassin) return "암살";
            return "소환";
        }

        private Color GetGradeColor(CharacterGrade grade, Color fallback)
        {
            return CharacterGradeUtility.GetColor(grade, fallback);
        }
    }
}
