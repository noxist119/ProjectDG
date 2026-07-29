using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenseGame
{
	public class MetaFlowUI : MonoBehaviour
	{
		private sealed class RankingEntry
		{
			public string Name { get; }

			public int Score { get; }

			public bool IsPlayer { get; }

			public RankingEntry(string name, int score, bool isPlayer = false)
			{
				Name = name;
				Score = Mathf.Max(0, score);
				IsPlayer = isPlayer;
			}
		}

		private sealed class PresetDefinition
		{
			public string name;

			public string description;

			public Color accentColor;

			public readonly List<int> characterIndices = new List<int>();
		}

		[SerializeField]
		private DefenseGameController gameController;

		[SerializeField]
		private GameUIButtonBinder buttonBinder;

		[SerializeField]
		private AugmentManager augmentManager;

		[SerializeField]
		private CharacterDatabase characterDatabase;

		[SerializeField]
		private OutgameProgressionSystem outgameProgression;

		[SerializeField]
		private CharacterCollectionUI characterCollectionUI;

		[SerializeField]
		private float matchmakingDuration = 1.6f;

		[SerializeField]
		private int presetCount = 4;

		[SerializeField]
		private int cardsPerPreset = 5;

		[SerializeField]
		private float defeatResultRevealDelay = 5.15f;

		private readonly List<PresetDefinition> presets = new List<PresetDefinition>();

		private readonly Dictionary<Button, Vector2> outgameNavBasePositions = new Dictionary<Button, Vector2>();

		private readonly Dictionary<Button, Coroutine> outgameNavAnimationRoutines = new Dictionary<Button, Coroutine>();

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

		private readonly Image[] rankingTopCardPanels = (Image[])(object)new Image[3];

		private readonly Text[] rankingTopNameTexts = (Text[])(object)new Text[3];

		private readonly Text[] rankingTopScoreTexts = (Text[])(object)new Text[3];

		private readonly Image[] rankingRowPanels = (Image[])(object)new Image[9];

		private readonly Text[] rankingRowRankTexts = (Text[])(object)new Text[9];

		private readonly Text[] rankingRowNameTexts = (Text[])(object)new Text[9];

		private readonly Text[] rankingRowScoreTexts = (Text[])(object)new Text[9];

		private readonly List<GameObject> resultVictoryDecorations = new List<GameObject>();

		private Font font;

		private UiSkinResources uiSkin;

		private GameObject root;

		private GameObject outgameNavigationRoot;

		private GameObject gameplayHudRoot;

		private GameObject lobbyOverlay;

		private GameObject matchmakingOverlay;

		private GameObject resultOverlay;

		private GameObject loadoutOverlay;

		private GameObject outgamePlaceholderOverlay;

		private GameObject seasonRankingOverlay;

		private GameObject exitConfirmOverlay;

		private GameObject shopOverlay;

		private GameObject shopPurchaseConfirmOverlay;

		private GameObject shopPurchaseResultOverlay;

		private GameObject shopSceneCanvasRoot;

		private Text lobbyModeText;

		private Text lobbyFortuneText;

		private Text lobbyCollectionSummaryText;

		private Text lobbyChestStatusText;

		private Text lobbyRecordStatusText;

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

		private Image resultRibbonImage;

		private Text shopGoldText;

		private Text shopDiamondText;

		private Text shopDailyResetText;

		private Text shopRatesText;

		private Text shopCollectionText;

		private Text shopResultText;

		private Text shopPurchaseConfirmTitleText;

		private Text shopPurchaseConfirmBodyText;

		private Text shopPurchaseResultTitleText;

		private Text shopPurchaseResultBodyText;

		private Text shopPurchaseResultCurrencyText;

		private Image shopPurchaseResultIconImage;

		private RectTransform shopPurchaseResultModalRect;

		private CanvasGroup shopPurchaseResultCanvasGroup;

		private readonly Button[] shopDailyOfferButtons = (Button[])(object)new Button[3];

		private readonly Button[] shopCashBundleButtons = (Button[])(object)new Button[3];

		private Text shopModeText;

		private Text loadoutHeaderText;

		private Text rankingSeasonText;

		private Text rankingPlayerSummaryText;

		private Text rankingPlayerProgressText;

		private Text loadoutSummaryText;

		private Button battleButton;

		private Button lobbyButton;

		private Button loadoutButton;

		private Button lobbyBattleButton;

		private Button resultContinueButton;

		private Button resultRetryButton;

		private Button matchmakingCancelButton;

		private Button lobbyModeButton;

		private Button hubShopButton;

		private Button hubInventoryButton;

		private Button hubLobbyButton;

		private Button hubYahtzeeButton;

		private Button hubRankingButton;

		private Button exitConfirmLeaveButton;

		private Button exitConfirmContinueButton;

		private Button shopEarnedDrawButton;

		private Button shopSingleDrawButton;

		private Button shopWishlistButton;

		private Button shopTenDrawButton;

		private Button shopFiftyDrawButton;

		private Button shopHundredDrawButton;

		private Button shopTestDiamondButton;

		private Button shopPurchaseConfirmButton;

		private Coroutine matchmakingRoutine;

		private Coroutine resultRoutine;

		private Coroutine drawRevealRoutine;

		private Coroutine shopPurchaseResultRoutine;

		private Coroutine shopCurrencyCountRoutine;

		private UnityAction pendingShopPurchaseAction;

		private Sprite roundedSprite;

		private CharacterCollectionUI subscribedCollectionUI;

		private int selectedPresetIndex;

		private bool subscribed;

		private bool defeatPresented;

		private bool resultRewardGranted;

		private int displayedShopGold;

		private int displayedShopDiamonds;

		private Scene gameplayScene;

		private Scene shopScene;

		public void Configure(DefenseGameController controller, GameUIButtonBinder binder, AugmentManager augments, CharacterDatabase database, OutgameProgressionSystem progression, CharacterCollectionUI collection, Font uiFont, Transform canvasRoot, GameObject gameplayHud, Button externalBattleButton, Button externalLobbyButton, Button externalLoadoutButton, UiSkinResources skin = null)
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
			if (outgameNavigationRoot != null)
			{
				if (Application.isPlaying)
				{
					UnityEngine.Object.Destroy(outgameNavigationRoot);
				}
				else
				{
					UnityEngine.Object.DestroyImmediate(outgameNavigationRoot);
				}
				outgameNavigationRoot = null;
			}
			if (root != null)
			{
				if (Application.isPlaying)
				{
					UnityEngine.Object.Destroy(root);
				}
				else
				{
					UnityEngine.Object.DestroyImmediate(root);
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
			if (!subscribed && !(gameController == null))
			{
				gameController.OnRoundStarted += HandleRoundStarted;
				gameController.OnRoundCompleted += HandleRoundCompleted;
				gameController.OnGameOver += HandleGameOver;
				if (outgameProgression != null)
				{
					outgameProgression.OnProgressChanged += HandleProgressChanged;
				}
				subscribed = true;
			}
		}

		private void Unsubscribe()
		{
			if (subscribed && !(gameController == null))
			{
				gameController.OnRoundStarted -= HandleRoundStarted;
				gameController.OnRoundCompleted -= HandleRoundCompleted;
				gameController.OnGameOver -= HandleGameOver;
				if (outgameProgression != null)
				{
					outgameProgression.OnProgressChanged -= HandleProgressChanged;
				}
				subscribed = false;
			}
		}

		private void SubscribeCollectionClosed()
		{
			if (!(subscribedCollectionUI == characterCollectionUI))
			{
				UnsubscribeCollectionClosed();
				subscribedCollectionUI = characterCollectionUI;
				if (subscribedCollectionUI != null)
				{
					subscribedCollectionUI.OnClosed += HandleCollectionClosed;
					subscribedCollectionUI.OnOpened += HandleCollectionOpened;
				}
			}
		}

		private void UnsubscribeCollectionClosed()
		{
			if (subscribedCollectionUI != null)
			{
				subscribedCollectionUI.OnClosed -= HandleCollectionClosed;
				subscribedCollectionUI.OnOpened -= HandleCollectionOpened;
				subscribedCollectionUI = null;
			}
		}

		private void Build(Transform parent)
		{
			root = new GameObject("MetaFlowOverlayRoot", typeof(RectTransform));
			root.transform.SetParent(parent, worldPositionStays: false);
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
			BuildSeasonRankingOverlay(root.transform);
			BuildExitConfirmOverlay(root.transform);
			outgameNavigationRoot = new GameObject("OutgameNavigationRoot", typeof(RectTransform));
			outgameNavigationRoot.transform.SetParent(parent, worldPositionStays: false);
			RectTransform navRootRect = outgameNavigationRoot.GetComponent<RectTransform>();
			navRootRect.anchorMin = Vector2.zero;
			navRootRect.anchorMax = Vector2.one;
			navRootRect.offsetMin = Vector2.zero;
			navRootRect.offsetMax = Vector2.zero;
			BuildOutgameBottomNav(outgameNavigationRoot.transform);
		}

		private void BuildLobbyOverlay(Transform parent)
		{
			lobbyOverlay = CreateOverlayRoot(parent, "LobbyOverlay", Color.clear);
			((Graphic)lobbyOverlay.GetComponent<Image>()).raycastTarget = false;
			Image modal = CreatePanel(lobbyOverlay.transform, "LobbyModal", new Vector2(0f, 76f), new Vector2(0f, -152f), Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: false, shadow: false);
			RollRollUiResource.TryApplySprite(modal, "Common/loot-box-background", (Type)0, preserveAspect: false);
			((Graphic)modal).color = Color.white;
			CreatePanel(((Component)(object)modal).transform, "TopBanner", new Vector2(0f, -170f), new Vector2(760f, 104f), new Color(0.84f, 0.92f, 1f, 0.18f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreateText(((Component)(object)modal).transform, "LobbyTitle", "로비", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(260f, 50f), 38, TextAnchor.MiddleCenter, bold: true);
			CreateText(((Component)(object)modal).transform, "LobbySubTitle", "전투를 준비하세요.", new Color(0.86f, 0.91f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(720f, 40f), 22, TextAnchor.MiddleCenter, bold: false);
			lobbyModeText = CreateText(((Component)(object)modal).transform, "LobbyModeText", "SERVICE", new Color(0.43f, 1f, 0.8f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -120f), new Vector2(162f, 34f), 19, TextAnchor.MiddleLeft, bold: true);
			lobbyModeButton = CreateButton(((Component)(object)modal).transform, "LobbyModeButton", "테스트 진입", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-44f, -300f), new Vector2(156f, 54f), new Color(0.23f, 0.72f, 0.82f, 1f), TogglePlayMode, 18);
			lobbyFortuneText = CreateText(((Component)(object)modal).transform, "LobbyFortuneText", DailyFortuneSystem.TodaySummary, new Color(1f, 0.88f, 0.4f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(720f, 30f), 18, TextAnchor.MiddleCenter, bold: true);
			Image readyPanel = CreatePanel(((Component)(object)modal).transform, "LobbyReadyPanel", new Vector2(0f, -650f), new Vector2(760f, 250f), new Color(0.1f, 0.16f, 0.38f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CreateShopArtwork(((Component)(object)readyPanel).transform, "LobbyReadyIcon", "Icons/icon-main-menu-battle", new Vector2(0f, -26f), new Vector2(92f, 92f), Color.white, new Vector2(0.5f, 1f));
			CreateText(((Component)(object)readyPanel).transform, "LobbyReadyTitle", "전투 준비 완료", new Color(1f, 0.88f, 0.36f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(480f, 44f), 32, TextAnchor.MiddleCenter, bold: true);
			CreateText(((Component)(object)readyPanel).transform, "LobbyReadyBody", "전장에 입장한 뒤 유닛을 소환하고\n다음 라운드로 전투를 시작하세요.", new Color(0.84f, 0.91f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(620f, 66f), 21, TextAnchor.MiddleCenter, bold: false);
			Image statusPanel = CreatePanel(((Component)(object)modal).transform, "LobbyStatusPanel", new Vector2(0f, -950f), new Vector2(760f, 194f), new Color(0.08f, 0.13f, 0.33f, 0.9f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CreateText(((Component)(object)statusPanel).transform, "LobbyStatusTitle", "오늘의 준비 현황", new Color(0.42f, 0.94f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(480f, 36f), 24, TextAnchor.MiddleCenter, bold: true);
			Image collectionStatus = CreatePanel(((Component)(object)statusPanel).transform, "LobbyCollectionStatus", new Vector2(-238f, -70f), new Vector2(220f, 92f), new Color(0.15f, 0.23f, 0.5f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Image chestStatus = CreatePanel(((Component)(object)statusPanel).transform, "LobbyChestStatus", new Vector2(0f, -70f), new Vector2(220f, 92f), new Color(0.14f, 0.39f, 0.38f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Image recordStatus = CreatePanel(((Component)(object)statusPanel).transform, "LobbyRecordStatus", new Vector2(238f, -70f), new Vector2(220f, 92f), new Color(0.32f, 0.22f, 0.56f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreateText(((Component)(object)collectionStatus).transform, "LobbyCollectionLabel", "컬렉션", new Color(0.8f, 0.9f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(190f, 28f), 17, TextAnchor.MiddleCenter, bold: true);
			CreateText(((Component)(object)chestStatus).transform, "LobbyChestLabel", "무료 상자", new Color(0.72f, 1f, 0.8f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(190f, 28f), 17, TextAnchor.MiddleCenter, bold: true);
			CreateText(((Component)(object)recordStatus).transform, "LobbyRecordLabel", "최고 기록", new Color(0.9f, 0.82f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(190f, 28f), 17, TextAnchor.MiddleCenter, bold: true);
			lobbyCollectionSummaryText = CreateText(((Component)(object)collectionStatus).transform, "LobbyCollectionValue", "보유 영웅", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(198f, 38f), 16, TextAnchor.MiddleCenter, bold: false);
			lobbyChestStatusText = CreateText(((Component)(object)chestStatus).transform, "LobbyChestValue", "상자 준비", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(198f, 38f), 17, TextAnchor.MiddleCenter, bold: true);
			lobbyRecordStatusText = CreateText(((Component)(object)recordStatus).transform, "LobbyRecordValue", "최고 R1", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(198f, 38f), 18, TextAnchor.MiddleCenter, bold: true);
			lobbyBattleButton = CreateButton(((Component)(object)modal).transform, "LobbyBattleButton", "전장 입장", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 500f), new Vector2(420f, 150f), new Color(0.98f, 0.2f, 0.13f, 1f), HandleEnterPreparationPressed, 45);
			CreateText(((Component)(object)modal).transform, "LobbyBottomHint", "전장 입장 후 유닛을 소환하면 전투를 시작할 수 있습니다.", new Color(0.88f, 0.92f, 1f, 0.88f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 400f), new Vector2(760f, 38f), 19, TextAnchor.MiddleCenter, bold: false);
		}

		private void BuildOutgameBottomNav(Transform parent)
		{
			Image dock = CreatePanel(parent, "OutgameBottomNavDock", new Vector2(0f, 0f), new Vector2(0f, 152f), new Color(0.88f, 0.93f, 1f, 0.96f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), rounded: false, shadow: true);
			CreatePanel(((Component)(object)dock).transform, "DockTopLine", new Vector2(0f, 150f), new Vector2(0f, 4f), new Color(1f, 1f, 1f, 0.7f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), rounded: false, shadow: false);
			RectTransform tabsLayer = CreateOutgameNavTabsLayer(parent, "OutgameBottomNavTabs");
			hubShopButton = CreateOutgameNavButton(tabsLayer, "OutgameNavShop", "상점", "SHOP", new Vector2(-432f, 140f), new Color(1f, 0.58f, 0.76f), ToggleShop);
			hubInventoryButton = CreateOutgameNavButton(tabsLayer, "OutgameNavInventory", "인벤", "CARD", new Vector2(-216f, 140f), new Color(0.98f, 0.36f, 0.36f), ShowCollectionTab);
			hubLobbyButton = CreateOutgameNavButton(tabsLayer, "OutgameNavLobby", "로비", "HOME", new Vector2(0f, 140f), new Color(0.3f, 0.62f, 1f), ShowLobbyTab);
			hubYahtzeeButton = CreateOutgameNavButton(tabsLayer, "OutgameNavYahtzee", "얏찌", "DICE", new Vector2(216f, 140f), new Color(1f, 0.62f, 0.22f), delegate
			{
				ShowOutgamePlaceholder("얏찌", "주사위 기반 보너스 컨텐츠 자리입니다.\n운빨존많겜식 일일/주간 변동 컨텐츠 후보로 남겨둡니다.");
			});
			((UnityEventBase)(object)hubYahtzeeButton.onClick).RemoveAllListeners();
			((UnityEvent)(object)hubYahtzeeButton.onClick).AddListener((UnityAction)delegate
			{
				ShowOutgamePlaceholder("얏찌", "얏찌에서 무료 상자 게이지와 상자 키를 획득하는 구조입니다.\n게임만 해도 같은 유닛 풀을 얻으며, 실제 얏찌 룰 확정 후 점수·족보 보상이 이 게이지에 연결됩니다.");
			});
			hubRankingButton = CreateOutgameNavButton(tabsLayer, "OutgameNavRanking", "랭킹", "CUP", new Vector2(432f, 140f), new Color(0.74f, 0.52f, 1f), ShowSeasonRanking);
			HighlightOutgameNav(hubLobbyButton);
		}

		private RectTransform CreateOutgameNavTabsLayer(Transform parent, string name)
		{
			GameObject layerObject = new GameObject(name, typeof(RectTransform));
			layerObject.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rect = layerObject.GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(0f, 0f);
			rect.anchorMax = new Vector2(1f, 0f);
			rect.pivot = new Vector2(0.5f, 0f);
			rect.anchoredPosition = Vector2.zero;
			rect.sizeDelta = new Vector2(0f, 232f);
			layerObject.transform.SetAsLastSibling();
			return rect;
		}

		private Button CreateOutgameNavButton(Transform parent, string name, string label, string icon, Vector2 position, Color accent, UnityAction action)
		{
			Button button = CreateButton(parent, name, string.Empty, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), position, new Vector2(164f, 118f), new Color(0.93f, 0.96f, 1f, 0.98f), action, 20);
			Image background = ((Component)(object)button).GetComponent<Image>();
			if ((UnityEngine.Object)(object)background != null && RollRollUiResource.TryApplySprite(background, "Lobby/000-main-lobby-bottom-menu-background", (Type)1, preserveAspect: false))
			{
				((Graphic)background).color = Color.white;
			}
			Image navIcon = CreateShopArtwork(((Component)(object)button).transform, "NavIcon", ResolveOutgameNavIconPath(button, active: false), new Vector2(0f, 22f), new Vector2(48f, 48f), Color.white, new Vector2(0.5f, 0.5f));
			((Graphic)navIcon).raycastTarget = false;
			Text labelText = GetChildText(((Component)(object)button).transform, "Label");
			if ((UnityEngine.Object)(object)labelText != null)
			{
				((Component)(object)labelText).gameObject.name = "NavLabel";
				labelText.text = RuntimeKoreanTextUtility.Clean("NavLabel", label);
				((Graphic)labelText).color = new Color(0.2f, 0.25f, 0.42f);
				labelText.fontSize = 20;
				labelText.resizeTextForBestFit = true;
				labelText.resizeTextMinSize = 16;
				labelText.resizeTextMaxSize = 20;
				labelText.alignment = TextAnchor.MiddleCenter;
				((Graphic)labelText).rectTransform.anchorMin = new Vector2(0f, 0f);
				((Graphic)labelText).rectTransform.anchorMax = new Vector2(1f, 0f);
				((Graphic)labelText).rectTransform.pivot = new Vector2(0.5f, 0f);
				((Graphic)labelText).rectTransform.anchoredPosition = new Vector2(0f, 10f);
				((Graphic)labelText).rectTransform.sizeDelta = new Vector2(-18f, 36f);
				AddReadableOutline(labelText);
				((Component)(object)labelText).transform.SetAsLastSibling();
			}
			outgameNavBasePositions[button] = position;
			SetOutgameNavButtonState(button, active: false);
			return button;
		}

		private void BuildOutgamePlaceholderOverlay(Transform parent)
		{
			outgamePlaceholderOverlay = CreateOverlayRoot(parent, "OutgamePlaceholderOverlay", Color.clear);
			((Graphic)outgamePlaceholderOverlay.GetComponent<Image>()).raycastTarget = false;
			Image modal = CreatePanel(outgamePlaceholderOverlay.transform, "PlaceholderModal", new Vector2(0f, 76f), new Vector2(0f, -152f), new Color(0.1f, 0.16f, 0.42f, 1f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: false, shadow: false);
			modal.sprite = null;
			modal.type = (Type)0;
			modal.preserveAspect = false;
			CreatePanel(((Component)(object)modal).transform, "PlaceholderGlow", new Vector2(0f, -40f), new Vector2(600f, 88f), new Color(0.36f, 0.78f, 1f, 0.18f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreateText(((Component)(object)modal).transform, "PlaceholderTitle", "준비중", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(520f, 56f), 38, TextAnchor.MiddleCenter, bold: true);
			CreateText(((Component)(object)modal).transform, "PlaceholderBody", "아웃게임 컨텐츠 화면 자리입니다.", new Color(0.86f, 0.92f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(610f, 170f), 24, TextAnchor.MiddleCenter, bold: false);
		}

		private void BuildSeasonRankingOverlay(Transform parent)
		{
			seasonRankingOverlay = CreateOverlayRoot(parent, "SeasonRankingOverlay", Color.clear);
			((Graphic)seasonRankingOverlay.GetComponent<Image>()).raycastTarget = false;
			Image modal = CreatePanel(seasonRankingOverlay.transform, "SeasonRankingModal", new Vector2(0f, 76f), new Vector2(0f, -152f), new Color32(40, 4, 4, byte.MaxValue), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: false, shadow: false);
			modal.sprite = null;
			modal.type = (Type)0;
			modal.preserveAspect = false;
			Image rankingTopVisual = CreateShopArtwork(((Component)(object)modal).transform, "RankingTopVisual", "DiceTower/top-rank-visual-img", new Vector2(0f, 5f), new Vector2(820f, 430f), new Color(1f, 1f, 1f, 0.3f), new Vector2(0.5f, 1f));
			((Graphic)rankingTopVisual).rectTransform.anchoredPosition3D = new Vector3(0f, 5f, 0f);
			((Graphic)rankingTopVisual).rectTransform.localScale = new Vector3(1.5f, 1.5f, 1f);
			Image header = CreatePanel(((Component)(object)modal).transform, "RankingHeader", new Vector2(0f, -190f), new Vector2(858f, 108f), new Color(0.37f, 0.2f, 0.76f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CreateShopArtwork(((Component)(object)header).transform, "RankingHeaderTrophy", "Icons/icon-main-menu-trophy-activated", new Vector2(34f, -22f), new Vector2(66f, 66f), Color.white, new Vector2(0f, 1f));
			CreateText(((Component)(object)header).transform, "RankingTitle", "시즌 랭킹", new Color(1f, 0.94f, 0.72f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(112f, -18f), new Vector2(300f, 48f), 37, TextAnchor.MiddleLeft, bold: true);
			rankingSeasonText = CreateText(((Component)(object)header).transform, "RankingSeasonText", "SEASON 1 · 주간 보스 리그", new Color(0.86f, 0.82f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(114f, -65f), new Vector2(420f, 28f), 18, TextAnchor.MiddleLeft, bold: true);
			Image podium = CreatePanel(((Component)(object)modal).transform, "RankingPodium", new Vector2(0f, -360f), new Vector2(850f, 414f), new Color(0.11f, 0.07f, 0.33f, 0.72f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreateText(((Component)(object)podium).transform, "PodiumHint", "이번 주 최고의 수호자", new Color(0.94f, 0.86f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(420f, 30f), 21, TextAnchor.MiddleCenter, bold: true);
			BuildRankingTopCard(((Component)(object)podium).transform, 1, 0f, -58f, new Vector2(300f, 326f), "DiceTower/rank-gold-bg", "RankedGrade/ranked-gold", new Color(1f, 0.8f, 0.22f));
			BuildRankingTopCard(((Component)(object)podium).transform, 2, -274f, -94f, new Vector2(250f, 280f), "DiceTower/rank-silver-bg", "RankedGrade/ranked-silver", new Color(0.78f, 0.88f, 1f));
			BuildRankingTopCard(((Component)(object)podium).transform, 3, 274f, -94f, new Vector2(250f, 280f), "DiceTower/rank-bronze-bg", "RankedGrade/ranked-bronze", new Color(1f, 0.63f, 0.36f));
			Image list = CreatePanel(((Component)(object)modal).transform, "RankingList", new Vector2(0f, -808f), new Vector2(850f, 894f), new Color(0.09f, 0.07f, 0.3f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CreateText(((Component)(object)list).transform, "RankingListTitle", "전체 랭킹", new Color(0.98f, 0.9f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -20f), new Vector2(240f, 34f), 25, TextAnchor.MiddleLeft, bold: true);
			CreateText(((Component)(object)list).transform, "RankingListGuide", "순위     플레이어                                  점수", new Color(0.66f, 0.72f, 0.94f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(-66f, 28f), 16, TextAnchor.MiddleCenter, bold: true);
			for (int index = 0; index < rankingRowPanels.Length; index++)
			{
				BuildRankingRow(((Component)(object)list).transform, index, -92f - (float)index * 86f);
			}
			Image playerFooter = CreatePanel(((Component)(object)modal).transform, "RankingPlayerFooter", new Vector2(0f, -1756f), new Vector2(850f, 126f), new Color(0.17f, 0.42f, 0.66f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CreateShopArtwork(((Component)(object)playerFooter).transform, "RankingPlayerTrophy", "Icons/icon-trophy", new Vector2(28f, -24f), new Vector2(72f, 72f), Color.white, new Vector2(0f, 1f));
			rankingPlayerSummaryText = CreateText(((Component)(object)playerFooter).transform, "RankingPlayerSummary", "내 순위 -위  |  레드X  0점", Color.white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(112f, -20f), new Vector2(-140f, 42f), 26, TextAnchor.MiddleLeft, bold: true);
			rankingPlayerProgressText = CreateText(((Component)(object)playerFooter).transform, "RankingPlayerProgress", "최고 런 0점 · 보스 처치 0회", new Color(0.72f, 0.94f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(112f, -68f), new Vector2(-140f, 32f), 18, TextAnchor.MiddleLeft, bold: true);
			seasonRankingOverlay.SetActive(value: false);
		}

		private void BuildRankingTopCard(Transform parent, int rank, float x, float y, Vector2 size, string backgroundPath, string badgePath, Color accent)
		{
			int index = rank - 1;
			Image card = CreatePanel(parent, "RankingTopCard_" + index, new Vector2(x, y), size, new Color(0.18f, 0.14f, 0.48f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			ApplyRankingPanelSprite(card, backgroundPath);
			rankingTopCardPanels[index] = card;
			float badgeSize = ((rank == 1) ? 116f : 96f);
			CreateShopArtwork(((Component)(object)card).transform, "TopBadge", badgePath, new Vector2(0f, -20f), new Vector2(badgeSize, badgeSize), Color.white, new Vector2(0.5f, 1f));
			CreateText(((Component)(object)card).transform, "TopRank", rank.ToString(), Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(72f, 46f), (rank == 1) ? 35 : 29, TextAnchor.MiddleCenter, bold: true);
			rankingTopNameTexts[index] = CreateText(((Component)(object)card).transform, "TopName", "플레이어", Color.white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, (rank == 1) ? (-150f) : (-126f)), new Vector2(-24f, 42f), (rank == 1) ? 25 : 22, TextAnchor.MiddleCenter, bold: true);
			ApplyTopRankingNameStyle(rankingTopNameTexts[index]);
			Image scorePlate = CreatePanel(((Component)(object)card).transform, "TopScorePlate", new Vector2(0f, (rank == 1) ? (-208f) : (-178f)), new Vector2(size.x - 34f, 58f), new Color(0.08f, 0.06f, 0.25f, 0.88f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreateShopArtwork(((Component)(object)scorePlate).transform, "TopTrophy", "Icons/icon-trophy", new Vector2(18f, -10f), new Vector2(40f, 40f), Color.white, new Vector2(0f, 1f));
			rankingTopScoreTexts[index] = CreateText(((Component)(object)scorePlate).transform, "TopScore", "0", accent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(66f, -8f), new Vector2(-78f, 40f), (rank == 1) ? 25 : 22, TextAnchor.MiddleRight, bold: true);
		}

		private void BuildRankingRow(Transform parent, int index, float y)
		{
			Image row = CreatePanel(parent, "RankingRow_" + index, new Vector2(0f, y), new Vector2(790f, 76f), new Color32(133, 131, 164, byte.MaxValue), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			row.type = (Type)1;
			rankingRowPanels[index] = row;
			CreateShopArtwork(((Component)(object)row).transform, "RankBadge", "RankedGrade/ranked-bronze-small", new Vector2(18f, -10f), new Vector2(56f, 56f), Color.white, new Vector2(0f, 1f));
			rankingRowRankTexts[index] = CreateText(((Component)(object)row).transform, "Rank", (index + 4).ToString(), Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25f, -17f), new Vector2(42f, 40f), 19, TextAnchor.MiddleCenter, bold: true);
			rankingRowNameTexts[index] = CreateText(((Component)(object)row).transform, "PlayerName", "플레이어", Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(106f, -17f), new Vector2(350f, 40f), 22, TextAnchor.MiddleLeft, bold: true);
			ApplyRankingRowPlayerNameStyle(rankingRowNameTexts[index]);
			Image scorePlate = CreatePanel(((Component)(object)row).transform, "ScorePlate", new Vector2(-18f, -10f), new Vector2(242f, 56f), new Color(0.08f, 0.07f, 0.25f, 0.78f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), rounded: true, shadow: false);
			CreateShopArtwork(((Component)(object)scorePlate).transform, "Trophy", "Icons/icon-trophy", new Vector2(12f, -9f), new Vector2(38f, 38f), Color.white, new Vector2(0f, 1f));
			rankingRowScoreTexts[index] = CreateText(((Component)(object)scorePlate).transform, "Score", "0", new Color(1f, 0.86f, 0.36f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(58f, -7f), new Vector2(-70f, 40f), 22, TextAnchor.MiddleRight, bold: true);
		}

		private void ApplyRankingPanelSprite(Image image, string resourcePath)
		{
			Sprite sprite = RollRollUiResource.LoadSprite(resourcePath);
			if (!((UnityEngine.Object)(object)image == null) && !(sprite == null))
			{
				image.sprite = sprite;
				image.type = (Type)0;
				((Graphic)image).color = Color.white;
			}
		}

		private void BuildExitConfirmOverlay(Transform parent)
		{
			exitConfirmOverlay = CreateOverlayRoot(parent, "ExitConfirmOverlay", new Color(0.02f, 0.03f, 0.09f, 0.76f));
			Image modal = CreatePanel(exitConfirmOverlay.transform, "ExitConfirmModal", new Vector2(0f, 34f), new Vector2(720f, 420f), new Color(0.08f, 0.13f, 0.32f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
			CreatePanel(((Component)(object)modal).transform, "ExitConfirmGlow", new Vector2(0f, -44f), new Vector2(560f, 76f), new Color(1f, 0.44f, 0.32f, 0.16f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreateText(((Component)(object)modal).transform, "ExitConfirmTitle", "나가시겠습니까?", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(560f, 58f), 38, TextAnchor.MiddleCenter, bold: true);
			CreateText(((Component)(object)modal).transform, "ExitConfirmBody", "현재 도전을 종료하고 로비로 이동합니다.", new Color(0.86f, 0.92f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(600f, 96f), 25, TextAnchor.MiddleCenter, bold: false);
			exitConfirmLeaveButton = CreateButton(((Component)(object)modal).transform, "ExitConfirmLeaveButton", "나가기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 70f), new Vector2(260f, 74f), new Color(0.92f, 0.28f, 0.22f, 1f), ConfirmExitToOutgame, 27);
			exitConfirmContinueButton = CreateButton(((Component)(object)modal).transform, "ExitConfirmContinueButton", "계속하기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(18f, 70f), new Vector2(260f, 74f), new Color(0.3f, 0.62f, 1f, 1f), HideExitConfirm, 27);
		}

		private void BuildShopOverlay(Transform parent)
		{
			shopOverlay = CreateOverlayRoot(parent, "OutgameShopOverlay", Color.clear);
			((Graphic)shopOverlay.GetComponent<Image>()).raycastTarget = false;
			Image modal = CreatePanel(shopOverlay.transform, "ShopModal", new Vector2(0f, 76f), new Vector2(0f, -152f), Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: false, shadow: false);
			RollRollUiResource.TryApplySprite(modal, "Common/background", (Type)0, preserveAspect: false);
			((Graphic)modal).color = Color.white;
			CreatePanel(((Component)(object)modal).transform, "ShopHeader", new Vector2(0f, -95f), new Vector2(850f, 104f), new Color(0.98f, 0.78f, 0.18f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreatePanel(((Component)(object)modal).transform, "ShopGoldCurrencyChip", new Vector2(-128f, -129f), new Vector2(260f, 58f), new Color(0.08f, 0.12f, 0.3f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreatePanel(((Component)(object)modal).transform, "ShopDiamondCurrencyChip", new Vector2(140f, -129f), new Vector2(250f, 58f), new Color(0.12f, 0.1f, 0.34f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreateShopArtwork(((Component)(object)modal).transform, "HeaderGoldIcon", "Icons/goods_icon_gold", new Vector2(-222f, -137f), new Vector2(42f, 42f), new Color(1f, 1f, 1f, 0.98f), new Vector2(0.5f, 1f));
			CreateShopArtwork(((Component)(object)modal).transform, "HeaderDiamondIcon", "Icons/goods_icon_ruby", new Vector2(48f, -137f), new Vector2(42f, 42f), new Color(1f, 1f, 1f, 0.98f), new Vector2(0.5f, 1f));
			CreateText(((Component)(object)modal).transform, "ShopTitle", "상점", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-338f, -120f), new Vector2(140f, 48f), 38, TextAnchor.MiddleCenter, bold: true);
			shopGoldText = CreateText(((Component)(object)modal).transform, "ShopGoldText", "GOLD 0", new Color(1f, 0.84f, 0.28f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-104f, -137f), new Vector2(190f, 42f), 20, TextAnchor.MiddleRight, bold: true);
			shopDiamondText = CreateText(((Component)(object)modal).transform, "DiamondText", "DIA 0", new Color(0.46f, 0.94f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(162f, -137f), new Vector2(180f, 42f), 20, TextAnchor.MiddleRight, bold: true);
			shopGoldText.resizeTextForBestFit = true;
			shopGoldText.resizeTextMinSize = 16;
			shopGoldText.resizeTextMaxSize = 20;
			shopDiamondText.resizeTextForBestFit = true;
			shopDiamondText.resizeTextMinSize = 16;
			shopDiamondText.resizeTextMaxSize = 20;
			shopModeText = CreateText(((Component)(object)modal).transform, "ShopModeText", "SERVICE", new Color(0.7f, 0.84f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -293f), new Vector2(340f, 32f), 17, TextAnchor.MiddleLeft, bold: true);
			shopTestDiamondButton = CreateButton(((Component)(object)modal).transform, "TestCurrencyButton", "테스트 재화 충전", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-48f, -283f), new Vector2(224f, 52f), new Color(0.19f, 0.8f, 0.72f, 1f), RechargeTestDiamonds, 18);
			Image cashSection = CreatePanel(((Component)(object)modal).transform, "CashBundleSection", new Vector2(0f, -363f), new Vector2(820f, 270f), new Color(0.11f, 0.17f, 0.39f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CreateShopArtwork(((Component)(object)cashSection).transform, "CashSectionIcon", "GradeAndGoodsIcons/goods_icon_reward_box", new Vector2(26f, -20f), new Vector2(56f, 56f), Color.white, new Vector2(0f, 1f));
			CreateText(((Component)(object)cashSection).transform, "CashBundleTitle", "오늘의 꾸러미 · 현금 상품", new Color(1f, 0.88f, 0.42f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, -16f), new Vector2(500f, 36f), 25, TextAnchor.MiddleLeft, bold: true);
			CreatePanel(((Component)(object)cashSection).transform, "CashSectionDivider", new Vector2(0f, -54f), new Vector2(760f, 3f), new Color(1f, 0.78f, 0.28f, 0.48f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			string[] cashLabels = new string[3] { "골드 주머니\n10,000 GOLD\n₩3,300", "다이아 주머니\n1,200 DIA\n₩6,600", "성장 꾸러미\n20,000G + 2,000 DIA\n₩9,900" };
			string[] cashIcons = new string[3] { "GradeAndGoodsIcons/goods_icon_gold_group", "GradeAndGoodsIcons/goods_icon_ruby_group", "GradeAndGoodsIcons/goods_icon_reward_box" };
			for (int i = 0; i < shopCashBundleButtons.Length; i++)
			{
				int index = i;
				shopCashBundleButtons[i] = CreateButton(((Component)(object)cashSection).transform, "CashBundleCard_" + i, cashLabels[i], new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-260f + (float)i * 260f, -70f), new Vector2(220f, 180f), new Color(0.66f, 0.24f + (float)i * 0.08f, 0.76f, 0.98f), delegate
				{
					ShowCashBundlePurchaseConfirm(index);
				}, 20);
				DecorateShopProductCard(shopCashBundleButtons[i], cashIcons[i], i switch
				{
					1 => new Color(0.54f, 0.9f, 1f, 1f), 
					0 => new Color(1f, 0.72f, 0.18f, 1f), 
					_ => new Color(0.94f, 0.55f, 1f, 1f), 
				}, compact: false);
			}
			Image dailySection = CreatePanel(((Component)(object)modal).transform, "DailyShopSection", new Vector2(0f, -657f), new Vector2(820f, 286f), new Color(0.11f, 0.17f, 0.39f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CreateShopArtwork(((Component)(object)dailySection).transform, "DailySectionIcon", "GradeAndGoodsIcons/goods_icon_gold", new Vector2(26f, -20f), new Vector2(56f, 56f), Color.white, new Vector2(0f, 1f));
			CreateText(((Component)(object)dailySection).transform, "DailyShopTitle", "일일 상점", new Color(0.56f, 0.94f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, -18f), new Vector2(240f, 36f), 27, TextAnchor.MiddleLeft, bold: true);
			shopDailyResetText = CreateText(((Component)(object)dailySection).transform, "DailyResetText", "갱신까지 00:00", new Color(0.78f, 0.84f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -20f), new Vector2(300f, 32f), 17, TextAnchor.MiddleRight, bold: false);
			CreatePanel(((Component)(object)dailySection).transform, "DailySectionDivider", new Vector2(0f, -58f), new Vector2(760f, 3f), new Color(0.34f, 0.88f, 1f, 0.46f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			string[] dailyLabels = new string[3] { "일일 무료 선물\n+500 GOLD\n무료", "영웅 카드 x5\n1,200 GOLD\n일일 1회", "프리미엄 카드 x3\n250 DIA\n일일 1회" };
			string[] dailyIcons = new string[3] { "GradeAndGoodsIcons/goods_icon_gold_group", "GradeAndGoodsIcons/goods_icon_reward_box", "GradeAndGoodsIcons/goods_icon_ruby_group" };
			for (int i2 = 0; i2 < shopDailyOfferButtons.Length; i2++)
			{
				int index2 = i2;
				shopDailyOfferButtons[i2] = CreateButton(((Component)(object)dailySection).transform, "DailyOfferCard_" + i2, dailyLabels[i2], new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-260f + (float)i2 * 260f, -78f), new Vector2(220f, 180f), (i2 == 0) ? new Color(0.3f, 0.7f, 0.32f, 1f) : new Color(0.25f, 0.38f + (float)i2 * 0.08f, 0.78f, 1f), delegate
				{
					ShowDailyOfferPurchaseConfirm(index2);
				}, 20);
				DecorateShopProductCard(shopDailyOfferButtons[i2], dailyIcons[i2], i2 switch
				{
					1 => new Color(0.42f, 0.82f, 1f, 1f), 
					0 => new Color(0.54f, 1f, 0.52f, 1f), 
					_ => new Color(0.78f, 0.55f, 1f, 1f), 
				}, compact: false);
			}
			Image chestSection = CreatePanel(((Component)(object)modal).transform, "ChestShopSection", new Vector2(0f, -967f), new Vector2(820f, 438f), new Color(0.11f, 0.17f, 0.39f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CreateShopArtwork(((Component)(object)chestSection).transform, "ChestSectionIcon", "Lobby/top-panel-icon-chest-empty", new Vector2(26f, -20f), new Vector2(56f, 56f), Color.white, new Vector2(0f, 1f));
			CreateText(((Component)(object)chestSection).transform, "ChestShopTitle", "영웅 카드 상자 · 다이아 상품", new Color(1f, 0.82f, 0.3f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, -16f), new Vector2(520f, 38f), 27, TextAnchor.MiddleLeft, bold: true);
			CreatePanel(((Component)(object)chestSection).transform, "ChestSectionDivider", new Vector2(0f, -54f), new Vector2(760f, 3f), new Color(1f, 0.76f, 0.24f, 0.46f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			shopEarnedDrawButton = CreateButton(((Component)(object)chestSection).transform, "EarnedDrawButton", "무료 상자 열기", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -62f), new Vector2(360f, 58f), new Color(0.24f, 0.72f, 0.4f, 1f), ShowEarnedChestConfirm, 20);
			shopWishlistButton = CreateButton(((Component)(object)chestSection).transform, "WishlistButton", "위시 영웅 설정", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(12f, -62f), new Vector2(360f, 58f), new Color(0.5f, 0.34f, 0.78f, 1f), CycleWishlist, 19);
			shopSingleDrawButton = CreateButton(((Component)(object)chestSection).transform, "FiveDrawCard", "5개", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-288f, -150f), new Vector2(182f, 124f), new Color(0.2f, 0.66f, 0.78f, 1f), delegate
			{
				ShowPremiumChestPurchaseConfirm(5);
			}, 20);
			shopTenDrawButton = CreateButton(((Component)(object)chestSection).transform, "TwentyDrawCard", "20개", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-96f, -150f), new Vector2(182f, 124f), new Color(0.34f, 0.54f, 0.88f, 1f), delegate
			{
				ShowPremiumChestPurchaseConfirm(20);
			}, 20);
			shopFiftyDrawButton = CreateButton(((Component)(object)chestSection).transform, "FiftyDrawCard", "50개", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(96f, -150f), new Vector2(182f, 124f), new Color(0.58f, 0.38f, 0.9f, 1f), delegate
			{
				ShowPremiumChestPurchaseConfirm(50);
			}, 20);
			shopHundredDrawButton = CreateButton(((Component)(object)chestSection).transform, "HundredDrawCard", "100개", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(288f, -150f), new Vector2(182f, 124f), new Color(0.92f, 0.48f, 0.24f, 1f), delegate
			{
				ShowPremiumChestPurchaseConfirm(100);
			}, 20);
			DecorateShopProductCard(shopSingleDrawButton, "GradeAndGoodsIcons/goods_icon_reward_box", new Color(0.38f, 0.9f, 1f, 1f), compact: true);
			DecorateShopProductCard(shopTenDrawButton, "GradeAndGoodsIcons/goods_icon_reward_box", new Color(0.45f, 0.68f, 1f, 1f), compact: true);
			DecorateShopProductCard(shopFiftyDrawButton, "GradeAndGoodsIcons/goods_icon_reward_box", new Color(0.75f, 0.5f, 1f, 1f), compact: true);
			DecorateShopProductCard(shopHundredDrawButton, "GradeAndGoodsIcons/goods_icon_reward_box", new Color(1f, 0.58f, 0.28f, 1f), compact: true);
			shopRatesText = CreateText(((Component)(object)chestSection).transform, "RatesText", string.Empty, new Color(0.83f, 0.91f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(-44f, 74f), 16, TextAnchor.MiddleCenter, bold: false);
			shopCollectionText = CreateText(((Component)(object)chestSection).transform, "CollectionText", string.Empty, new Color(1f, 0.92f, 0.5f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(-44f, 38f), 20, TextAnchor.MiddleCenter, bold: true);
			Image resultPanel = CreatePanel(((Component)(object)modal).transform, "DrawResults", new Vector2(0f, -1437f), new Vector2(820f, 330f), new Color(0.07f, 0.1f, 0.26f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CreateText(((Component)(object)resultPanel).transform, "ResultTitle", "최근 구매 상태", new Color(0.45f, 0.96f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(360f, 34f), 24, TextAnchor.MiddleCenter, bold: true);
			shopResultText = CreateText(((Component)(object)resultPanel).transform, "ResultBody", "구매하면 팝업으로 획득 결과가 표시됩니다.", new Color(0.91f, 0.94f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(-52f, 238f), 18, TextAnchor.UpperLeft, bold: false);
			BuildShopBottomNavigation(shopOverlay.transform);
			BuildShopPurchaseConfirm(shopOverlay.transform);
			BuildShopPurchaseResultPopup(shopOverlay.transform);
			RefreshShop();
		}

		private void BuildShopBottomNavigation(Transform parent)
		{
			Image dock = CreatePanel(parent, "ShopBottomNavDock", new Vector2(0f, 0f), new Vector2(0f, 152f), new Color(0.88f, 0.93f, 1f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), rounded: false, shadow: true);
			CreatePanel(((Component)(object)dock).transform, "ShopDockTopLine", new Vector2(0f, 150f), new Vector2(0f, 4f), new Color(1f, 1f, 1f, 0.7f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), rounded: false, shadow: false);
			RectTransform tabsLayer = CreateOutgameNavTabsLayer(parent, "ShopBottomNavTabs");
			Button shopTab = CreateOutgameNavButton(tabsLayer, "ShopNavShop", "상점", "SHOP", new Vector2(-432f, 140f), new Color(1f, 0.58f, 0.76f), null);
			Button inventoryTab = CreateOutgameNavButton(tabsLayer, "ShopNavInventory", "인벤", "CARD", new Vector2(-216f, 140f), new Color(0.98f, 0.36f, 0.36f), delegate
			{
				HideShop();
				ShowCollectionTab();
			});
			Button lobbyTab = CreateOutgameNavButton(tabsLayer, "ShopNavLobby", "로비", "HOME", new Vector2(0f, 140f), new Color(0.3f, 0.62f, 1f), delegate
			{
				HideShop();
				ShowLobby();
			});
			Button yahtzeeTab = CreateOutgameNavButton(tabsLayer, "ShopNavYahtzee", "얏찌", "DICE", new Vector2(216f, 140f), new Color(1f, 0.62f, 0.22f), delegate
			{
				ShowOutgamePlaceholder("얏찌", "얏찌에서 무료 상자 게이지와 상자 키를 획득하는 구조입니다.");
			});
			Button rankingTab = CreateOutgameNavButton(tabsLayer, "ShopNavRanking", "랭킹", "CUP", new Vector2(432f, 140f), new Color(0.74f, 0.52f, 1f), ShowSeasonRanking);
			SetOutgameNavButtonState(shopTab, active: true);
			SetOutgameNavButtonState(inventoryTab, active: false);
			SetOutgameNavButtonState(lobbyTab, active: false);
			SetOutgameNavButtonState(yahtzeeTab, active: false);
			SetOutgameNavButtonState(rankingTab, active: false);
		}

		private void BuildShopPurchaseConfirm(Transform parent)
		{
			shopPurchaseConfirmOverlay = CreateOverlayRoot(parent, "ShopPurchaseConfirmOverlay", new Color(0.01f, 0.02f, 0.08f, 0.82f));
			Image confirmModal = CreatePanel(shopPurchaseConfirmOverlay.transform, "ShopPurchaseConfirmModal", new Vector2(0f, 36f), new Vector2(680f, 500f), new Color(0.1f, 0.27f, 0.62f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
			CreatePanel(((Component)(object)confirmModal).transform, "ShopPurchaseConfirmHeader", new Vector2(0f, -18f), new Vector2(620f, 86f), new Color(0.98f, 0.78f, 0.18f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			shopPurchaseConfirmTitleText = CreateText(((Component)(object)confirmModal).transform, "ShopPurchaseConfirmTitle", "구매 확인", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(500f, 44f), 31, TextAnchor.MiddleCenter, bold: true);
			CreateShopArtwork(((Component)(object)confirmModal).transform, "ShopPurchaseConfirmIcon", "GradeAndGoodsIcons/goods_icon_reward_box", new Vector2(0f, -126f), new Vector2(108f, 108f), Color.white, new Vector2(0.5f, 1f));
			shopPurchaseConfirmBodyText = CreateText(((Component)(object)confirmModal).transform, "ShopPurchaseConfirmBody", string.Empty, new Color(0.94f, 0.96f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -244f), new Vector2(570f, 116f), 23, TextAnchor.MiddleCenter, bold: true);
			CreateButton(((Component)(object)confirmModal).transform, "ShopPurchaseConfirmCancelButton", "취소", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 40f), new Vector2(250f, 74f), new Color(0.28f, 0.42f, 0.72f, 1f), HideShopPurchaseConfirm, 25);
			shopPurchaseConfirmButton = CreateButton(((Component)(object)confirmModal).transform, "ShopPurchaseConfirmButton", "구매", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(14f, 40f), new Vector2(250f, 74f), new Color(0.95f, 0.52f, 0.16f, 1f), ConfirmPendingShopPurchase, 25);
			shopPurchaseConfirmOverlay.SetActive(value: false);
		}

		private void BuildShopPurchaseResultPopup(Transform parent)
		{
			shopPurchaseResultOverlay = CreateOverlayRoot(parent, "ShopPurchaseResultOverlay", new Color(0.01f, 0.02f, 0.08f, 0.58f));
			shopPurchaseResultCanvasGroup = shopPurchaseResultOverlay.AddComponent<CanvasGroup>();
			Image resultModal = CreatePanel(shopPurchaseResultOverlay.transform, "ShopPurchaseResultModal", new Vector2(0f, 38f), new Vector2(720f, 560f), new Color(0.09f, 0.18f, 0.48f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
			shopPurchaseResultModalRect = ((Graphic)resultModal).rectTransform;
			CreatePanel(((Component)(object)resultModal).transform, "ShopPurchaseResultHeader", new Vector2(0f, -20f), new Vector2(640f, 88f), new Color(0.98f, 0.78f, 0.18f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			shopPurchaseResultTitleText = CreateText(((Component)(object)resultModal).transform, "ShopPurchaseResultTitle", "구매 완료", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(540f, 48f), 34, TextAnchor.MiddleCenter, bold: true);
			CreatePanel(((Component)(object)resultModal).transform, "ShopPurchaseResultIconGlow", new Vector2(0f, -138f), new Vector2(170f, 132f), new Color(0.6f, 0.42f, 1f, 0.34f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			shopPurchaseResultIconImage = CreateShopArtwork(((Component)(object)resultModal).transform, "ShopPurchaseResultIcon", "GradeAndGoodsIcons/goods_icon_reward_box", new Vector2(0f, -120f), new Vector2(118f, 118f), Color.white, new Vector2(0.5f, 1f));
			shopPurchaseResultBodyText = CreateText(((Component)(object)resultModal).transform, "ShopPurchaseResultBody", "영웅 카드 x5를 구매했습니다.", new Color(0.94f, 0.97f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -254f), new Vector2(610f, 152f), 25, TextAnchor.MiddleCenter, bold: false);
			shopPurchaseResultBodyText.resizeTextForBestFit = true;
			shopPurchaseResultBodyText.resizeTextMinSize = 16;
			shopPurchaseResultBodyText.resizeTextMaxSize = 25;
			((Graphic)shopPurchaseResultBodyText).rectTransform.anchoredPosition = new Vector2(0f, -266f);
			((Graphic)shopPurchaseResultBodyText).rectTransform.sizeDelta = new Vector2(610f, 112f);
			shopPurchaseResultCurrencyText = CreateText(((Component)(object)resultModal).transform, "ShopPurchaseResultCurrency", string.Empty, new Color(1f, 0.91f, 0.42f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -350f), new Vector2(600f, 48f), 25, TextAnchor.MiddleCenter, bold: true);
			((Component)(object)shopPurchaseResultCurrencyText).gameObject.SetActive(value: false);
			CreateButton(((Component)(object)resultModal).transform, "ShopPurchaseResultCloseButton", "확인", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(270f, 74f), new Color(0.28f, 0.62f, 1f, 1f), HideShopPurchaseResultPopup, 26);
			Transform closeButtonTransform = ((Component)(object)resultModal).transform.Find("ShopPurchaseResultCloseButton");
			if (closeButtonTransform != null)
			{
				RectTransform closeButtonRect = closeButtonTransform.GetComponent<RectTransform>();
				if (closeButtonRect != null)
				{
					closeButtonRect.anchoredPosition = new Vector2(0f, 34f);
				}
			}
			shopPurchaseResultOverlay.SetActive(value: false);
		}

		private void BuildMatchmakingOverlay(Transform parent)
		{
			matchmakingOverlay = CreateOverlayRoot(parent, "MatchmakingOverlay", new Color(0.02f, 0.04f, 0.12f, 0.72f));
			Image modal = CreatePanel(matchmakingOverlay.transform, "MatchmakingModal", Vector2.zero, new Vector2(620f, 420f), new Color(0.16f, 0.2f, 0.44f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
			CreatePanel(((Component)(object)modal).transform, "PulseA", new Vector2(0f, -132f), new Vector2(156f, 82f), new Color(0.2f, 1f, 0.92f, 0.14f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreatePanel(((Component)(object)modal).transform, "PulseB", new Vector2(0f, -132f), new Vector2(210f, 118f), new Color(1f, 0.32f, 0.64f, 0.08f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreateText(((Component)(object)modal).transform, "MatchTitle", "전투 준비 중", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(560f, 42f), 28, TextAnchor.MiddleCenter, bold: true);
			queueTimerText = CreateText(((Component)(object)modal).transform, "QueueTimer", "00.00", new Color(0.28f, 1f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(220f, 68f), 46, TextAnchor.MiddleCenter, bold: true);
			queueStatusText = CreateText(((Component)(object)modal).transform, "QueueStatus", "라운드 전장을 준비하는 중...", new Color(0.84f, 0.9f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -226f), new Vector2(540f, 58f), 18, TextAnchor.MiddleCenter, bold: false);
			queueStatusText.resizeTextForBestFit = true;
			queueStatusText.resizeTextMinSize = 14;
			queueStatusText.resizeTextMaxSize = 18;
			matchmakingCancelButton = CreateButton(((Component)(object)modal).transform, "MatchmakingCancelButton", "닫기", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -326f), new Vector2(200f, 64f), new Color(0.95f, 0.45f, 0.3f, 1f), CancelMatchmaking, 25);
		}

		private void BuildResultOverlay(Transform parent)
		{
			resultOverlay = CreateOverlayRoot(parent, "RoundResultOverlay", new Color(0.03f, 0.05f, 0.15f, 0.74f));
			Image modal = CreatePanel(resultOverlay.transform, "ResultModal", new Vector2(0f, 24f), new Vector2(830f, 1300f), new Color(0.13f, 0.17f, 0.42f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
			Image leftTrumpet = CreateShopArtwork(((Component)(object)modal).transform, "ResultVictoryTrumpetLeft", "InGame/ingame-duel-mode-victory-trumpet", new Vector2(-240f, -30f), new Vector2(198f, 158f), Color.white, new Vector2(0.5f, 1f));
			RegisterResultVictoryDecoration(leftTrumpet);
			Image rightTrumpet = CreateShopArtwork(((Component)(object)modal).transform, "ResultVictoryTrumpetRight", "InGame/ingame-duel-mode-victory-trumpet", new Vector2(240f, -30f), new Vector2(198f, 158f), Color.white, new Vector2(0.5f, 1f));
			if ((UnityEngine.Object)(object)leftTrumpet != null)
			{
				((Graphic)leftTrumpet).rectTransform.localScale = new Vector3(-1f, 1f, 1f);
			}
			RegisterResultVictoryDecoration(rightTrumpet);
			RegisterResultVictoryDecoration(CreateShopArtwork(((Component)(object)modal).transform, "ResultVictoryBanner", "InGame/ingame-duel-mode-victory-title-background", new Vector2(0f, -108f), new Vector2(720f, 182f), Color.white, new Vector2(0.5f, 1f)));
			RegisterResultVictoryDecoration(CreateShopArtwork(((Component)(object)modal).transform, "ResultVictoryTrophy", "GradeAndGoodsIcons/icon-trophy", new Vector2(0f, -46f), new Vector2(82f, 82f), Color.white, new Vector2(0.5f, 1f)));
			RegisterResultVictoryDecoration(CreateShopArtwork(((Component)(object)modal).transform, "ResultVictoryStarLeft", "InGame/minimi-star", new Vector2(-290f, -195f), new Vector2(30f, 30f), new Color(0.28f, 0.94f, 1f, 0.96f), new Vector2(0.5f, 1f)));
			RegisterResultVictoryDecoration(CreateShopArtwork(((Component)(object)modal).transform, "ResultVictoryStarRight", "InGame/minimi-star", new Vector2(290f, -195f), new Vector2(26f, 26f), new Color(1f, 0.66f, 0.24f, 0.96f), new Vector2(0.5f, 1f)));
			resultTitleText = CreateText(((Component)(object)modal).transform, "ResultTitle", "승리", new Color(1f, 0.84f, 0.18f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(500f, 66f), 56, TextAnchor.MiddleCenter, bold: true);
			resultRibbonImage = CreatePanel(((Component)(object)modal).transform, "ResultRibbon", new Vector2(0f, -300f), new Vector2(650f, 140f), new Color(0.17f, 0.42f, 1f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			resultSummaryText = CreateText(((Component)(object)modal).transform, "ResultSummary", "라운드 1 클리어", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(650f, 40f), 28, TextAnchor.MiddleCenter, bold: true);
			resultMetaText = CreateText(((Component)(object)modal).transform, "ResultMeta", "연속 클리어 +1", new Color(0.95f, 0.9f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(700f, 40f), 22, TextAnchor.MiddleCenter, bold: true);
			Image recapPanel = CreatePanel(((Component)(object)modal).transform, "ResultRecapPanel", new Vector2(0f, -470f), new Vector2(730f, 306f), new Color(0.07f, 0.12f, 0.33f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CreateShopArtwork(((Component)(object)recapPanel).transform, "ResultScoreTrophy", "GradeAndGoodsIcons/icon-trophy", new Vector2(-246f, -36f), new Vector2(40f, 40f), Color.white, new Vector2(0.5f, 1f));
			resultScoreText = CreateText(((Component)(object)recapPanel).transform, "ResultScore", "RUN SCORE A / 000점", new Color(1f, 0.85f, 0.24f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(660f, 44f), 34, TextAnchor.MiddleCenter, bold: true);
			resultRecapText = CreateText(((Component)(object)recapPanel).transform, "ResultRecap", "이번 판 사건 3개\nCARD 1  결과 대기\nCARD 2  결과 대기\nCARD 3  결과 대기", new Color(0.9f, 0.96f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(660f, 132f), 27, TextAnchor.UpperLeft, bold: false);
			resultNextText = CreateText(((Component)(object)recapPanel).transform, "ResultNext", "다음 라운드 준비", new Color(0.62f, 1f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -238f), new Vector2(660f, 96f), 25, TextAnchor.UpperLeft, bold: true);
			resultRecapText.resizeTextForBestFit = true;
			resultRecapText.resizeTextMinSize = 12;
			resultRecapText.resizeTextMaxSize = 16;
			resultNextText.resizeTextForBestFit = true;
			resultNextText.resizeTextMinSize = 11;
			resultNextText.resizeTextMaxSize = 14;
			ApplyReadableResultTextLayout();
			Image rewardPanel = CreatePanel(((Component)(object)modal).transform, "RewardPanel", new Vector2(0f, -800f), new Vector2(690f, 200f), new Color(0.18f, 0.15f, 0.52f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CreateText(((Component)(object)rewardPanel).transform, "RewardHeader", "전투 보상", new Color(1f, 0.9f, 0.46f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(250f, 32f), 24, TextAnchor.MiddleCenter, bold: true);
			resultRewardGoldText = CreateRewardChip(((Component)(object)rewardPanel).transform, "RewardGold", "ResultRewardGoldIcon", "골드", "GradeAndGoodsIcons/goods_icon_gold", new Vector2(-145f, -58f), new Color(1f, 0.74f, 0.2f), "+000");
			resultRewardCoreText = CreateRewardChip(((Component)(object)rewardPanel).transform, "RewardDiamond", "ResultRewardDiamondIcon", "다이아", "GradeAndGoodsIcons/goods_icon_ruby", new Vector2(145f, -58f), new Color(0.3f, 0.84f, 1f), "+000");
			resultRetryButton = CreateButton(((Component)(object)modal).transform, "ResultRetryButton", "새 판 다시하기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-120f, 58f), new Vector2(306f, 78f), new Color(0.3f, 0.86f, 0.36f, 1f), RetryFromResult, 27);
			resultContinueButton = CreateButton(((Component)(object)modal).transform, "ResultContinueButton", "계속하기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(190f, 75f), new Vector2(240f, 100f), new Color(0.3f, 0.62f, 1f, 1f), ContinueFromResult, 28);
			CreateShopArtwork(((Component)(object)resultContinueButton).transform, "ContinueStar", "InGame/minimi-star", new Vector2(-114f, 0f), new Vector2(28f, 28f), new Color(1f, 0.91f, 0.34f, 1f), new Vector2(0.5f, 0.5f));
		}

		private void RegisterResultVictoryDecoration(Image image)
		{
			if ((UnityEngine.Object)(object)image != null)
			{
				resultVictoryDecorations.Add(((Component)(object)image).gameObject);
			}
		}

		private void ApplyReadableResultTextLayout()
		{
			ConfigureResultRibbonText(resultSummaryText, new Vector2(0f, -318f), new Vector2(610f, 50f), 28, 21);
			ConfigureResultRibbonText(resultMetaText, new Vector2(0f, -370f), new Vector2(610f, 46f), 22, 18);
			ConfigureResultText(resultScoreText, new Vector2(0f, -36f), new Vector2(680f, 58f), 36, TextAnchor.MiddleCenter, 34, 36);
			ConfigureResultText(resultRecapText, new Vector2(0f, -86f), new Vector2(650f, 124f), 23, TextAnchor.UpperLeft, 20, 23);
			ConfigureResultText(resultNextText, new Vector2(0f, -222f), new Vector2(650f, 62f), 20, TextAnchor.MiddleCenter, 18, 20);
			AddReadableOutline(resultScoreText);
			AddReadableOutline(resultRecapText);
			AddReadableOutline(resultNextText);
			AddReadableOutline(resultSummaryText);
			AddReadableOutline(resultMetaText);
		}

		private static void ConfigureResultRibbonText(Text text, Vector2 position, Vector2 size, int fontSize, int minSize)
		{
			if (!((UnityEngine.Object)(object)text == null))
			{
				RectTransform rect = ((Graphic)text).rectTransform;
				if (rect != null)
				{
					rect.anchoredPosition = position;
					rect.sizeDelta = size;
				}
				text.fontSize = fontSize;
				text.alignment = TextAnchor.MiddleCenter;
				text.horizontalOverflow = HorizontalWrapMode.Wrap;
				text.verticalOverflow = VerticalWrapMode.Truncate;
				text.resizeTextForBestFit = true;
				text.resizeTextMinSize = minSize;
				text.resizeTextMaxSize = fontSize;
			}
		}

		private static void ConfigureResultText(Text text, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, int minSize, int maxSize)
		{
			if (!((UnityEngine.Object)(object)text == null))
			{
				RectTransform rect = ((Graphic)text).rectTransform;
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
		}

		private void BuildLoadoutOverlay(Transform parent)
		{
			loadoutOverlay = CreateOverlayRoot(parent, "LoadoutOverlay", Color.clear);
			((Graphic)loadoutOverlay.GetComponent<Image>()).raycastTarget = false;
			Image modal = CreatePanel(loadoutOverlay.transform, "LoadoutModal", new Vector2(0f, 76f), new Vector2(0f, -152f), new Color(0.27f, 0.38f, 0.74f, 1f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: false, shadow: false);
			modal.sprite = null;
			modal.type = (Type)0;
			modal.preserveAspect = false;
			CreatePanel(((Component)(object)modal).transform, "LoadoutHeader", new Vector2(0f, -18f), new Vector2(900f, 112f), new Color(0.96f, 0.8f, 0.2f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			loadoutHeaderText = CreateText(((Component)(object)modal).transform, "LoadoutHeaderText", "이번 라운드 추천 조합", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(520f, 48f), 36, TextAnchor.MiddleCenter, bold: true);
			loadoutSummaryText = CreateText(((Component)(object)modal).transform, "LoadoutSummaryText", "현재 라운드 흐름에 맞춘 운영 참고용 조합입니다.", new Color(0.88f, 0.92f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(760f, 32f), 18, TextAnchor.MiddleCenter, bold: false);
			BuildRecommendationNotice(((Component)(object)modal).transform, -172f);
			CreateText(((Component)(object)modal).transform, "DeckHeader", "추천 핵심 유닛 (참고용)", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -286f), new Vector2(620f, 32f), 24, TextAnchor.MiddleCenter, bold: true);
			BuildLoadoutDeckCards(((Component)(object)modal).transform);
			CreateText(((Component)(object)modal).transform, "RosterHeader", "보유 유닛 참고", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -760f), new Vector2(620f, 32f), 24, TextAnchor.MiddleCenter, bold: true);
			BuildLoadoutRosterCards(((Component)(object)modal).transform);
		}

		private void BuildRecommendationNotice(Transform parent, float anchoredY)
		{
			Image notice = CreatePanel(parent, "RecommendedCompositionNotice", new Vector2(0f, anchoredY), new Vector2(650f, 64f), new Color(0.1f, 0.18f, 0.4f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreateText(((Component)(object)notice).transform, "RecommendationNoticeTitle", "자동 추천", new Color(1f, 0.86f, 0.34f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(160f, 34f), 21, TextAnchor.MiddleLeft, bold: true);
			CreateText(((Component)(object)notice).transform, "RecommendationNoticeHint", "선택 기능 없음 · 소환 확률 영향 없음", new Color(0.82f, 0.9f, 1f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(420f, 34f), 18, TextAnchor.MiddleRight, bold: false);
		}

		private void BuildLobbyFeaturedCards(Transform parent)
		{
			for (int i = 0; i < cardsPerPreset; i++)
			{
				float x = -344f + (float)i * 172f;
				Image card = CreatePanel(parent, "LobbyFeaturedCard_" + i, new Vector2(x, -690f), new Vector2(154f, 230f), new Color(0.94f, 0.96f, 0.99f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
				Image accent = CreatePanel(((Component)(object)card).transform, "Accent", new Vector2(0f, -8f), new Vector2(124f, 42f), Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
				Image portrait = CreatePanel(((Component)(object)card).transform, "Portrait", new Vector2(0f, -52f), new Vector2(116f, 86f), new Color(0.84f, 0.9f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
				CreatePanel(((Component)(object)card).transform, "LabelBack", new Vector2(0f, 38f), new Vector2(142f, 78f), new Color(0.04f, 0.07f, 0.18f, 0.82f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
				Text nameText = CreateText(((Component)(object)card).transform, "Name", "Hero", Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(140f, 28f), 18, TextAnchor.MiddleCenter, bold: true);
				Text gradeText = CreateText(((Component)(object)card).transform, "Grade", "일반", new Color(0.82f, 0.92f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(132f, 24f), 16, TextAnchor.MiddleCenter, bold: true);
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
				float x = -352f + (float)i * 176f;
				Image card = CreatePanel(parent, "LoadoutDeckCard_" + i, new Vector2(x, -384f), new Vector2(156f, 210f), new Color(0.95f, 0.97f, 0.99f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
				Image accent = CreatePanel(((Component)(object)card).transform, "Accent", new Vector2(0f, -8f), new Vector2(130f, 54f), Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
				Image portrait = CreatePanel(((Component)(object)card).transform, "Portrait", new Vector2(0f, -76f), new Vector2(104f, 66f), new Color(0.86f, 0.91f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
				CreatePanel(((Component)(object)card).transform, "LabelBack", new Vector2(0f, 40f), new Vector2(140f, 74f), new Color(0.04f, 0.07f, 0.18f, 0.82f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
				Text nameText = CreateText(((Component)(object)card).transform, "Name", "Hero", Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(130f, 30f), 18, TextAnchor.MiddleCenter, bold: true);
				Text detailText = CreateText(((Component)(object)card).transform, "Detail", "일반 / 역할", new Color(0.82f, 0.92f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(132f, 36f), 15, TextAnchor.MiddleCenter, bold: false);
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
				float x = -210f + (float)column * 420f;
				float y = -848f - (float)row * 132f;
				Image card = CreatePanel(parent, "LoadoutRosterCard_" + i, new Vector2(x, y), new Vector2(360f, 112f), new Color(0.14f, 0.18f, 0.42f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
				Image accent = CreatePanel(((Component)(object)card).transform, "Accent", new Vector2(18f, -16f), new Vector2(76f, 78f), Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
				Image portrait = CreatePanel(((Component)(object)card).transform, "Portrait", new Vector2(18f, -16f), new Vector2(76f, 78f), new Color(0.86f, 0.91f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
				Text nameText = CreateText(((Component)(object)card).transform, "Name", "Hero", Color.white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(114f, -18f), new Vector2(-144f, 28f), 18, TextAnchor.MiddleLeft, bold: true);
				Text detailText = CreateText(((Component)(object)card).transform, "Detail", "등급 / 역할 / 스킬", new Color(0.85f, 0.9f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(114f, -52f), new Vector2(-144f, 44f), 15, TextAnchor.MiddleLeft, bold: false);
				loadoutRosterAccentImages.Add(accent);
				loadoutRosterPortraitImages.Add(portrait);
				loadoutRosterNameTexts.Add(nameText);
				loadoutRosterDetailTexts.Add(detailText);
			}
		}

		private Text CreateRewardChip(Transform parent, string name, string iconName, string title, string iconResourcePath, Vector2 anchoredPosition, Color accentColor, string value)
		{
			Color chipColor = Color.Lerp(new Color(0.08f, 0.12f, 0.34f, 0.98f), accentColor, 0.18f);
			Image chip = CreatePanel(parent, name, anchoredPosition, new Vector2(254f, 126f), chipColor, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CreatePanel(((Component)(object)chip).transform, "Accent", new Vector2(0f, -8f), new Vector2(206f, 5f), accentColor, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreateText(((Component)(object)chip).transform, "Title", title, new Color(0.94f, 0.97f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(22f, -26f), new Vector2(136f, 28f), 20, TextAnchor.MiddleLeft, bold: true);
			CreateShopArtwork(((Component)(object)chip).transform, iconName, iconResourcePath, new Vector2(-78f, 24f), new Vector2(68f, 68f), Color.white, new Vector2(0.5f, 0f));
			return CreateText(((Component)(object)chip).transform, "Value", value, accentColor, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(38f, 28f), new Vector2(132f, 48f), 34, TextAnchor.MiddleCenter, bold: true);
		}

		private void WireButtons()
		{
			if ((UnityEngine.Object)(object)battleButton != null)
			{
				((UnityEventBase)(object)battleButton.onClick).RemoveAllListeners();
				AddButtonListener(battleButton, HandleBattlePressed);
			}
			if ((UnityEngine.Object)(object)lobbyButton != null)
			{
				((UnityEventBase)(object)lobbyButton.onClick).RemoveAllListeners();
				AddButtonListener(lobbyButton, HandleExitToOutgamePressed);
			}
			if ((UnityEngine.Object)(object)loadoutButton != null)
			{
				((UnityEventBase)(object)loadoutButton.onClick).RemoveAllListeners();
				AddButtonListener(loadoutButton, ToggleLoadout);
			}
			if ((UnityEngine.Object)(object)lobbyModeButton != null)
			{
				((UnityEventBase)(object)lobbyModeButton.onClick).RemoveAllListeners();
				AddButtonListener(lobbyModeButton, TogglePlayMode);
			}
		}

		private void BuildPresets()
		{
			presets.Clear();
			string[] names = new string[4] { "안정 성장", "웨이브 정리", "제어 운영", "보스 대비" };
			string[] descriptions = new string[4] { "안정적인 초반 운영과 균형 잡힌 합성 흐름을 위한 추천입니다.", "공격 템포와 광역 압박으로 몰려오는 웨이브를 정리하는 추천입니다.", "마나 순환과 군중 제어형 스킬을 활용하는 운영 추천입니다.", "보스 라운드에 대비해 집중 화력과 유지력을 챙기는 추천입니다." };
			Color[] colors = new Color[4]
			{
				new Color(0.19f, 0.92f, 0.92f),
				new Color(1f, 0.58f, 0.26f),
				new Color(0.4f, 0.86f, 0.44f),
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
					int startIndex = i * cardsPerPreset % characterCount;
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
			if (presets.Count != 0)
			{
				int upcomingRound = ResolveUpcomingRecommendationRound();
				selectedPresetIndex = ResolveRecommendedPresetIndex(upcomingRound);
				PresetDefinition preset = presets[selectedPresetIndex];
				if ((UnityEngine.Object)(object)loadoutHeaderText != null)
				{
					loadoutHeaderText.text = "R" + upcomingRound + " 추천 조합 · " + preset.name;
				}
				if ((UnityEngine.Object)(object)loadoutSummaryText != null)
				{
					loadoutSummaryText.text = preset.description;
				}
				UpdateLobbyFeaturedCards(preset);
				UpdateLoadoutDeckCards(preset);
				UpdateLoadoutRosterCards(preset);
			}
		}

		private void RefreshLobbyPreparationStatus()
		{
			if ((UnityEngine.Object)(object)lobbyCollectionSummaryText != null)
			{
				lobbyCollectionSummaryText.text = ((outgameProgression != null) ? outgameProgression.BuildCollectionSummary() : "보유 영웅 정보 없음");
				lobbyCollectionSummaryText.resizeTextForBestFit = true;
				lobbyCollectionSummaryText.resizeTextMinSize = 12;
				lobbyCollectionSummaryText.resizeTextMaxSize = 16;
			}
			if ((UnityEngine.Object)(object)lobbyChestStatusText != null)
			{
				if (outgameProgression != null)
				{
					lobbyChestStatusText.text = "보유 " + outgameProgression.EarnedChestKeys + "개  |  " + outgameProgression.EarnedChestProgress + "/" + outgameProgression.EarnedChestProgressTarget;
				}
				else
				{
					lobbyChestStatusText.text = "상자 준비 정보 없음";
				}
			}
			if ((UnityEngine.Object)(object)lobbyRecordStatusText != null)
			{
				int highestRound = ((!(outgameProgression != null)) ? 1 : Mathf.Max(1, outgameProgression.HighestRoundReached));
				lobbyRecordStatusText.text = "최고 R" + highestRound;
			}
		}

		private int ResolveUpcomingRecommendationRound()
		{
			if (gameController == null)
			{
				return 1;
			}
			int currentRound = Mathf.Max(0, gameController.CurrentRound);
			return gameController.IsRoundRunning ? Mathf.Max(1, currentRound) : (currentRound + 1);
		}

		private int ResolveRecommendedPresetIndex(int round)
		{
			int cycleRound = (Mathf.Max(1, round) - 1) % 10 + 1;
			int recommendedIndex = ((cycleRound > 3) ? ((cycleRound <= 6) ? 1 : ((cycleRound <= 9) ? 2 : 3)) : 0);
			return Mathf.Clamp(recommendedIndex, 0, Mathf.Max(0, presets.Count - 1));
		}

		private void UpdateLobbyFeaturedCards(PresetDefinition preset)
		{
			for (int i = 0; i < lobbyFeaturedNameTexts.Count; i++)
			{
				CharacterDefinition definition = GetPresetCharacter(preset, i);
				if (definition == null)
				{
					((Graphic)lobbyFeaturedAccentImages[i]).color = new Color(0.75f, 0.78f, 0.86f);
					ApplyCharacterPortrait(lobbyFeaturedPortraitImages, i, null);
					lobbyFeaturedNameTexts[i].text = "비어 있음";
					lobbyFeaturedGradeTexts[i].text = "미설정";
					SetCardLabelColors(lobbyFeaturedNameTexts[i], lobbyFeaturedGradeTexts[i]);
				}
				else
				{
					((Graphic)lobbyFeaturedAccentImages[i]).color = GetGradeColor(definition.grade, definition.accentColor);
					ApplyCharacterPortrait(lobbyFeaturedPortraitImages, i, definition);
					lobbyFeaturedNameTexts[i].text = definition.displayName;
					lobbyFeaturedGradeTexts[i].text = GetGradeName(definition.grade) + " / " + BuildCardLevelLabel(definition);
					SetCardLabelColors(lobbyFeaturedNameTexts[i], lobbyFeaturedGradeTexts[i]);
				}
			}
		}

		private void UpdateLoadoutDeckCards(PresetDefinition preset)
		{
			for (int i = 0; i < loadoutDeckNameTexts.Count; i++)
			{
				CharacterDefinition definition = GetPresetCharacter(preset, i);
				if (definition == null)
				{
					((Graphic)loadoutDeckAccentImages[i]).color = new Color(0.78f, 0.82f, 0.88f);
					ApplyCharacterPortrait(loadoutDeckPortraitImages, i, null);
					loadoutDeckNameTexts[i].text = "빈 슬롯";
					loadoutDeckDetailTexts[i].text = "유닛을 추가하면 여기에 표시됩니다.";
					SetCardLabelColors(loadoutDeckNameTexts[i], loadoutDeckDetailTexts[i]);
				}
				else
				{
					((Graphic)loadoutDeckAccentImages[i]).color = GetGradeColor(definition.grade, definition.accentColor);
					ApplyCharacterPortrait(loadoutDeckPortraitImages, i, definition);
					loadoutDeckNameTexts[i].text = definition.displayName;
					loadoutDeckDetailTexts[i].text = GetGradeName(definition.grade) + " / " + BuildCardLevelLabel(definition);
					SetCardLabelColors(loadoutDeckNameTexts[i], loadoutDeckDetailTexts[i]);
				}
			}
		}

		private void UpdateLoadoutRosterCards(PresetDefinition preset)
		{
			int characterCount = GetDeployableCharacterCount();
			int startIndex = ((characterCount > 0) ? (selectedPresetIndex * 7 % characterCount) : 0);
			for (int i = 0; i < loadoutRosterNameTexts.Count; i++)
			{
				CharacterDefinition definition = GetCharacter(startIndex + i);
				if (definition == null)
				{
					((Graphic)loadoutRosterAccentImages[i]).color = new Color(0.72f, 0.76f, 0.84f);
					ApplyCharacterPortrait(loadoutRosterPortraitImages, i, null);
					loadoutRosterNameTexts[i].text = "후보 없음";
					loadoutRosterDetailTexts[i].text = "캐릭터 데이터가 늘어나면 이 자리가 채워집니다.";
					continue;
				}
				((Graphic)loadoutRosterAccentImages[i]).color = GetGradeColor(definition.grade, definition.accentColor);
				ApplyCharacterPortrait(loadoutRosterPortraitImages, i, definition);
				loadoutRosterNameTexts[i].text = definition.displayName;
				string skillSummary = ((definition.skills != null && definition.skills.Count > 0) ? definition.skills[0].displayName : "기본 공격");
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
			return (characterDatabase != null) ? characterDatabase.GetDeployableCharacters().Count : 0;
		}

		private void HandleEnterPreparationPressed()
		{
			if (!(gameController == null) && !gameController.IsRoundRunning)
			{
				if (matchmakingRoutine != null)
				{
					StopCoroutine(matchmakingRoutine);
					matchmakingRoutine = null;
				}
				HideResult();
				HideLoadout();
				HideShop();
				HideLobby();
				HideMatchmaking();
				HideOutgamePlaceholder();
				HideExitConfirm();
				if (characterCollectionUI != null)
				{
					characterCollectionUI.Close();
				}
				SetGameplayHudVisible(visible: true);
				gameController.RequestBanner("준비 단계  유닛을 소환한 뒤 다음 라운드를 누르세요", new Color(0.72f, 0.86f, 0.58f), 3f);
			}
		}

		private void HandleBattlePressed()
		{
			if (gameController == null || buttonBinder == null || gameController.IsRoundRunning)
			{
				return;
			}
			if (augmentManager != null && augmentManager.HasPendingChoice)
			{
				augmentManager.OpenPendingChoice();
				gameController.RequestBanner("무료 증강체 1개를 선택해야 다음 라운드로 진행할 수 있습니다", new Color(0.52f, 0.9f, 1f), 2.2f);
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
				if ((UnityEngine.Object)(object)queueTimerText != null)
				{
					queueTimerText.text = elapsed.ToString("00.00");
				}
				if ((UnityEngine.Object)(object)queueStatusText != null)
				{
					queueStatusText.text = ((elapsed < matchmakingDuration * 0.55f) ? "라운드 전장을 준비하는 중..." : "전투 필드를 정리하는 중...");
				}
				yield return null;
			}
			matchmakingRoutine = null;
			HandleEnterPreparationPressed();
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
			}
			else
			{
				exitConfirmOverlay.SetActive(value: true);
			}
		}

		private void HideExitConfirm()
		{
			if (exitConfirmOverlay != null)
			{
				exitConfirmOverlay.SetActive(value: false);
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
			SetGameplayHudVisible(visible: true);
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
			if (!defeatPresented)
			{
				if (resultRoutine != null)
				{
					StopCoroutine(resultRoutine);
				}
				RuntimeAudioUtility.PlayVictory();
				resultRoutine = StartCoroutine(ShowRoundResultAfterFlow(round));
			}
		}

		private IEnumerator ShowRoundResultAfterFlow(int round)
		{
			yield return new WaitForSeconds(0.35f);
			if (gameController != null && gameController.Life > 0 && !defeatPresented)
			{
				ShowResult(victory: true, round);
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
			resultRoutine = StartCoroutine(ShowGameOverResultAfterCinematic((gameController != null) ? gameController.CurrentRound : 0));
		}

		private IEnumerator ShowGameOverResultAfterCinematic(int round)
		{
			float minimumDelay = 5.15f;
			yield return new WaitForSecondsRealtime(Mathf.Max(minimumDelay, defeatResultRevealDelay));
			ShowResult(victory: false, round);
			resultRoutine = null;
		}

		private void ShowLobby()
		{
			if (!(gameController != null) || !gameController.IsRoundRunning)
			{
				HideSeasonRanking();
				BuildPresets();
				ApplyRecommendedPreset();
				RefreshLobbyPreparationStatus();
				SetGameplayHudVisible(visible: false);
				if (lobbyOverlay != null)
				{
					lobbyOverlay.SetActive(value: true);
				}
				if (outgameNavigationRoot != null)
				{
					outgameNavigationRoot.SetActive(value: true);
					outgameNavigationRoot.transform.SetAsLastSibling();
				}
				HighlightOutgameNav(hubLobbyButton);
			}
		}

		private void HideLobby()
		{
			if (lobbyOverlay != null)
			{
				lobbyOverlay.SetActive(value: false);
			}
			if (outgameNavigationRoot != null)
			{
				outgameNavigationRoot.SetActive(value: false);
			}
		}

		private void HandleLobbyPressed()
		{
			if (!(gameController != null) || !gameController.IsRoundRunning)
			{
				HideResult();
				HideLoadout();
				HideShop();
				HideOutgamePlaceholder();
				HideExitConfirm();
				if (characterCollectionUI != null && characterCollectionUI.IsOpen)
				{
					characterCollectionUI.Close();
				}
				ShowLobby();
			}
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
				return;
			}
			SetGameplayHudVisible(visible: false);
			HideResult();
			HideShop();
			HideOutgamePlaceholder();
			HideExitConfirm();
			if (characterCollectionUI != null && characterCollectionUI.IsOpen)
			{
				characterCollectionUI.Close();
			}
			ShowLobby();
			ShowLoadout();
			HighlightOutgameNav(hubInventoryButton);
		}

		public void ToggleCollectionPanel()
		{
			ToggleCollection();
		}

		private void ShowCollectionTab()
		{
			if (!(characterCollectionUI == null))
			{
				if (characterCollectionUI.IsOpen)
				{
					HandleCollectionOpened();
				}
				else
				{
					ToggleCollection();
				}
			}
		}

		private void ToggleCollection()
		{
			if (!(characterCollectionUI == null))
			{
				if (!characterCollectionUI.IsOpen)
				{
					SetGameplayHudVisible(visible: false);
					HideResult();
					HideLoadout();
					HideShop();
					HideOutgamePlaceholder();
					HideExitConfirm();
					ShowLobby();
					HighlightOutgameNav(hubInventoryButton);
				}
				characterCollectionUI.Toggle();
			}
		}

		private void HandleCollectionOpened()
		{
			if (outgameNavigationRoot != null)
			{
				outgameNavigationRoot.SetActive(value: true);
				outgameNavigationRoot.transform.SetAsLastSibling();
			}
			HighlightOutgameNav(hubInventoryButton);
		}

		private void HandleCollectionClosed()
		{
			if ((!(matchmakingOverlay != null) || !matchmakingOverlay.activeSelf) && (!(resultOverlay != null) || !resultOverlay.activeSelf) && (!(shopOverlay != null) || !shopOverlay.activeSelf) && (!(loadoutOverlay != null) || !loadoutOverlay.activeSelf) && (!(seasonRankingOverlay != null) || !seasonRankingOverlay.activeSelf) && (!(outgamePlaceholderOverlay != null) || !outgamePlaceholderOverlay.activeSelf))
			{
				if (ShouldShowOutgameLobbyAfterCollection())
				{
					SetGameplayHudVisible(visible: false);
					ShowLobby();
					HighlightOutgameNav(hubLobbyButton);
				}
				else
				{
					HideLobby();
					SetGameplayHudVisible(visible: true);
				}
			}
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
				return;
			}
			SetGameplayHudVisible(visible: false);
			HideLoadout();
			HideResult();
			HideSeasonRanking();
			HideOutgamePlaceholder();
			HideExitConfirm();
			if (characterCollectionUI != null && characterCollectionUI.IsOpen)
			{
				characterCollectionUI.Close();
			}
			ShowShop();
			HighlightOutgameNav(hubShopButton);
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
				shopOverlay.SetActive(value: true);
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
			if (shopPurchaseResultRoutine != null)
			{
				StopCoroutine(shopPurchaseResultRoutine);
				shopPurchaseResultRoutine = null;
			}
			if (shopCurrencyCountRoutine != null)
			{
				StopCoroutine(shopCurrencyCountRoutine);
				shopCurrencyCountRoutine = null;
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
			shopGoldText = null;
			shopDiamondText = null;
			shopDailyResetText = null;
			shopRatesText = null;
			shopCollectionText = null;
			shopResultText = null;
			shopModeText = null;
			shopPurchaseConfirmOverlay = null;
			shopPurchaseConfirmTitleText = null;
			shopPurchaseConfirmBodyText = null;
			shopPurchaseConfirmButton = null;
			shopPurchaseResultOverlay = null;
			shopPurchaseResultTitleText = null;
			shopPurchaseResultBodyText = null;
			shopPurchaseResultCurrencyText = null;
			shopPurchaseResultIconImage = null;
			shopPurchaseResultModalRect = null;
			shopPurchaseResultCanvasGroup = null;
			pendingShopPurchaseAction = null;
			shopSingleDrawButton = null;
			shopTenDrawButton = null;
			shopFiftyDrawButton = null;
			shopHundredDrawButton = null;
			shopTestDiamondButton = null;
			shopEarnedDrawButton = null;
			shopWishlistButton = null;
			for (int i = 0; i < shopDailyOfferButtons.Length; i++)
			{
				shopDailyOfferButtons[i] = null;
			}
			for (int j = 0; j < shopCashBundleButtons.Length; j++)
			{
				shopCashBundleButtons[j] = null;
			}
			if (lobbyOverlay != null && lobbyOverlay.activeSelf)
			{
				HighlightOutgameNav(hubLobbyButton);
			}
		}

		private void BuildShopScene()
		{
			gameplayScene = base.gameObject.scene;
			shopScene = SceneManager.CreateScene("OutgameShop");
			shopSceneCanvasRoot = new GameObject("OutgameShopCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			SceneManager.MoveGameObjectToScene(shopSceneCanvasRoot, shopScene);
			Canvas shopCanvas = shopSceneCanvasRoot.GetComponent<Canvas>();
			shopCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
			shopCanvas.sortingOrder = 300;
			CanvasScaler scaler = shopSceneCanvasRoot.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = (ScaleMode)1;
			scaler.screenMatchMode = (ScreenMatchMode)1;
			scaler.referenceResolution = new Vector2(1080f, 1920f);
			scaler.matchWidthOrHeight = 0.84f;
			shopSceneCanvasRoot.AddComponent<RuntimeKoreanTextCleaner>();
			BuildShopOverlay(shopSceneCanvasRoot.transform);
		}

		private void TogglePlayMode()
		{
			if (!(outgameProgression == null) && (!(gameController != null) || !gameController.IsRoundRunning))
			{
				OutgamePlayMode nextMode = ((!outgameProgression.IsTestMode) ? OutgamePlayMode.Test : OutgamePlayMode.Service);
				gameController?.ClearBoardForProfileChange();
				outgameProgression.SwitchPlayMode(nextMode);
			}
		}

		private void RechargeTestDiamonds()
		{
			outgameProgression?.RechargeTestCurrency();
		}

		private void ShowCashBundlePurchaseConfirm(int index)
		{
			string[] productNames = new string[3] { "골드 주머니", "다이아 주머니", "성장 꾸러미" };
			string[] rewards = new string[3] { "10,000 GOLD", "1,200 DIA", "20,000 GOLD + 2,000 DIA" };
			string[] prices = new string[3] { "₩3,300", "₩6,600", "₩9,900" };
			int safeIndex = Mathf.Clamp(index, 0, productNames.Length - 1);
			ShowShopPurchaseConfirm(productNames[safeIndex], rewards[safeIndex] + "\n가격 " + prices[safeIndex] + "\n구매하시겠습니까?", "구매", delegate
			{
				HandleCashBundlePurchase(safeIndex);
			});
		}

		private void ShowDailyOfferPurchaseConfirm(int index)
		{
			if (!(outgameProgression == null))
			{
				string title;
				string body;
				string confirmLabel;
				switch (index)
				{
				case 0:
					title = "일일 무료 선물";
					body = "+" + outgameProgression.Settings.dailyFreeGold.ToString("N0") + " GOLD\n무료로 받으시겠습니까?";
					confirmLabel = "받기";
					break;
				case 1:
					title = "영웅 카드 x" + outgameProgression.Settings.dailyCardPackDrawCount;
					body = outgameProgression.Settings.dailyCardPackGoldCost.ToString("N0") + " GOLD가 차감됩니다.\n구매하시겠습니까?";
					confirmLabel = "구매";
					break;
				default:
					title = "프리미엄 카드 x" + outgameProgression.Settings.dailyPremiumPackDrawCount;
					body = outgameProgression.Settings.dailyPremiumPackDiamondCost.ToString("N0") + " DIA가 차감됩니다.\n구매하시겠습니까?";
					confirmLabel = "구매";
					break;
				}
				int safeIndex = Mathf.Clamp(index, 0, shopDailyOfferButtons.Length - 1);
				ShowShopPurchaseConfirm(title, body, confirmLabel, delegate
				{
					TryPurchaseDailyOffer(safeIndex);
				});
			}
		}

		private void ShowPremiumChestPurchaseConfirm(int drawCount)
		{
			if (!(outgameProgression == null))
			{
				int cost = outgameProgression.ResolvePremiumChestCost(drawCount);
				ShowShopPurchaseConfirm("영웅 카드 상자 " + drawCount + "개", cost.ToString("N0") + " DIA가 차감됩니다.\n상자를 여시겠습니까?", "구매", delegate
				{
					TryOpenChest(drawCount);
				});
			}
		}

		private void ShowEarnedChestConfirm()
		{
			if (!(outgameProgression == null))
			{
				ShowShopPurchaseConfirm("무료 영웅 상자", "보유 상자 1개를 사용합니다.\n상자를 여시겠습니까?", "열기", TryOpenEarnedChest);
			}
		}

		private void ShowShopPurchaseConfirm(string title, string body, string confirmLabel, UnityAction action)
		{
			if (!(shopPurchaseConfirmOverlay == null) && action != null)
			{
				HideShopPurchaseResultPopup();
				pendingShopPurchaseAction = action;
				if ((UnityEngine.Object)(object)shopPurchaseConfirmTitleText != null)
				{
					shopPurchaseConfirmTitleText.text = title;
				}
				if ((UnityEngine.Object)(object)shopPurchaseConfirmBodyText != null)
				{
					shopPurchaseConfirmBodyText.text = body;
				}
				SetButtonLabel(shopPurchaseConfirmButton, confirmLabel);
				shopPurchaseConfirmOverlay.transform.SetAsLastSibling();
				shopPurchaseConfirmOverlay.SetActive(value: true);
			}
		}

		private void HideShopPurchaseConfirm()
		{
			pendingShopPurchaseAction = null;
			shopPurchaseConfirmOverlay?.SetActive(value: false);
		}

		private void ConfirmPendingShopPurchase()
		{
			UnityAction action = pendingShopPurchaseAction;
			pendingShopPurchaseAction = null;
			shopPurchaseConfirmOverlay?.SetActive(value: false);
			action?.Invoke();
		}

		private void SetShopResultHint(string message)
		{
			if ((UnityEngine.Object)(object)shopResultText != null)
			{
				shopResultText.text = message;
			}
		}

		private void ShowShopPurchaseResultPopup(string title, string body, string currencyLine, string iconResourcePath, Color accentColor)
		{
			SetShopResultHint(title + "\n팝업으로 획득 결과를 확인하세요.");
			if (shopPurchaseResultOverlay == null)
			{
				return;
			}
			if ((UnityEngine.Object)(object)shopPurchaseResultTitleText != null)
			{
				shopPurchaseResultTitleText.text = title;
			}
			if ((UnityEngine.Object)(object)shopPurchaseResultBodyText != null)
			{
				shopPurchaseResultBodyText.text = body;
			}
			if ((UnityEngine.Object)(object)shopPurchaseResultCurrencyText != null)
			{
				bool hasCurrencyLine = !string.IsNullOrWhiteSpace(currencyLine);
				((Component)(object)shopPurchaseResultCurrencyText).gameObject.SetActive(hasCurrencyLine);
				shopPurchaseResultCurrencyText.text = currencyLine;
				((Graphic)shopPurchaseResultCurrencyText).color = Color.Lerp(new Color(1f, 0.84f, 0.26f), accentColor, 0.24f);
			}
			if ((UnityEngine.Object)(object)shopPurchaseResultIconImage != null)
			{
				Sprite sprite = RollRollUiResource.LoadSprite(iconResourcePath);
				if (sprite != null)
				{
					shopPurchaseResultIconImage.sprite = sprite;
				}
				((Graphic)shopPurchaseResultIconImage).color = Color.white;
			}
			shopPurchaseResultOverlay.transform.SetAsLastSibling();
			shopPurchaseResultOverlay.SetActive(value: true);
			if (shopPurchaseResultRoutine != null)
			{
				StopCoroutine(shopPurchaseResultRoutine);
			}
			shopPurchaseResultRoutine = StartCoroutine(AnimateShopPurchaseResultPopup());
		}

		private void HideShopPurchaseResultPopup()
		{
			if (shopPurchaseResultRoutine != null)
			{
				StopCoroutine(shopPurchaseResultRoutine);
				shopPurchaseResultRoutine = null;
			}
			if (shopPurchaseResultOverlay != null)
			{
				shopPurchaseResultOverlay.SetActive(value: false);
			}
			if (shopPurchaseResultCanvasGroup != null)
			{
				shopPurchaseResultCanvasGroup.alpha = 1f;
			}
			if (shopPurchaseResultModalRect != null)
			{
				shopPurchaseResultModalRect.localScale = Vector3.one;
			}
		}

		private IEnumerator AnimateShopPurchaseResultPopup()
		{
			if (shopPurchaseResultCanvasGroup != null)
			{
				shopPurchaseResultCanvasGroup.alpha = 0f;
				shopPurchaseResultCanvasGroup.blocksRaycasts = true;
			}
			if (shopPurchaseResultModalRect != null)
			{
				shopPurchaseResultModalRect.localScale = Vector3.one * 0.88f;
			}
			float elapsed = 0f;
			while (elapsed < 0.22f)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = Mathf.Clamp01(elapsed / 0.22f);
				float eased = Mathf.SmoothStep(0f, 1f, t);
				if (shopPurchaseResultCanvasGroup != null)
				{
					shopPurchaseResultCanvasGroup.alpha = eased;
				}
				if (shopPurchaseResultModalRect != null)
				{
					float scale = Mathf.Lerp(0.88f, 1.04f, eased);
					shopPurchaseResultModalRect.localScale = Vector3.one * scale;
				}
				yield return null;
			}
			if (shopPurchaseResultCanvasGroup != null)
			{
				shopPurchaseResultCanvasGroup.alpha = 1f;
			}
			if (shopPurchaseResultModalRect != null)
			{
				shopPurchaseResultModalRect.localScale = Vector3.one;
			}
			yield return null;
			shopPurchaseResultRoutine = null;
		}

		private void SetShopCurrencyText(int gold, int diamonds)
		{
			displayedShopGold = gold;
			displayedShopDiamonds = diamonds;
			if ((UnityEngine.Object)(object)shopGoldText != null)
			{
				shopGoldText.text = "GOLD " + gold.ToString("N0");
			}
			if ((UnityEngine.Object)(object)shopDiamondText != null)
			{
				shopDiamondText.text = "DIA " + diamonds.ToString("N0");
			}
		}

		private void PlayShopCurrencyChange(int fromGold, int fromDiamonds)
		{
			if (!(outgameProgression == null))
			{
				int startGold = fromGold;
				int startDiamonds = fromDiamonds;
				if (shopCurrencyCountRoutine != null)
				{
					startGold = displayedShopGold;
					startDiamonds = displayedShopDiamonds;
					StopCoroutine(shopCurrencyCountRoutine);
					shopCurrencyCountRoutine = null;
				}
				int targetGold = outgameProgression.Gold;
				int targetDiamonds = outgameProgression.Diamonds;
				if (startGold == targetGold && startDiamonds == targetDiamonds)
				{
					SetShopCurrencyText(targetGold, targetDiamonds);
				}
				else
				{
					shopCurrencyCountRoutine = StartCoroutine(AnimateShopCurrencyText(startGold, startDiamonds, targetGold, targetDiamonds));
				}
			}
		}

		private IEnumerator AnimateShopCurrencyText(int startGold, int startDiamonds, int targetGold, int targetDiamonds)
		{
			float elapsed = 0f;
			SetShopCurrencyText(startGold, startDiamonds);
			while (elapsed < 0.72f)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.72f));
				int gold = Mathf.RoundToInt(Mathf.Lerp(startGold, targetGold, t));
				int diamonds = Mathf.RoundToInt(Mathf.Lerp(startDiamonds, targetDiamonds, t));
				SetShopCurrencyText(gold, diamonds);
				float pulse = 1f + Mathf.Sin(t * MathF.PI) * 0.07f;
				if ((UnityEngine.Object)(object)shopGoldText != null)
				{
					((Graphic)shopGoldText).rectTransform.localScale = Vector3.one * pulse;
				}
				if ((UnityEngine.Object)(object)shopDiamondText != null)
				{
					((Graphic)shopDiamondText).rectTransform.localScale = Vector3.one * pulse;
				}
				yield return null;
			}
			SetShopCurrencyText(targetGold, targetDiamonds);
			if ((UnityEngine.Object)(object)shopGoldText != null)
			{
				((Graphic)shopGoldText).rectTransform.localScale = Vector3.one;
			}
			if ((UnityEngine.Object)(object)shopDiamondText != null)
			{
				((Graphic)shopDiamondText).rectTransform.localScale = Vector3.one;
			}
			shopCurrencyCountRoutine = null;
		}

		private string BuildDailyOfferPurchaseBody(int index, List<OutgameDrawResult> results, string fallbackMessage)
		{
			string main = index switch
			{
				0 => "일일 무료 선물을 받았습니다.", 
				1 => "영웅 카드 x" + outgameProgression.Settings.dailyCardPackDrawCount + "를 구매했습니다.", 
				_ => "프리미엄 카드 x" + outgameProgression.Settings.dailyPremiumPackDrawCount + "를 구매했습니다.", 
			};
			string drawSummary = BuildDrawPopupSummary(results);
			if (!string.IsNullOrEmpty(drawSummary))
			{
				return main + "\n" + drawSummary;
			}
			return string.IsNullOrWhiteSpace(fallbackMessage) ? main : (main + "\n" + fallbackMessage);
		}

		private static string BuildCurrencyChangeLine(int beforeGold, int beforeDiamonds, int afterGold, int afterDiamonds)
		{
			int goldDelta = afterGold - beforeGold;
			int diamondDelta = afterDiamonds - beforeDiamonds;
			string line = string.Empty;
			if (goldDelta != 0)
			{
				line = "GOLD " + FormatSignedCurrency(goldDelta);
			}
			if (diamondDelta != 0)
			{
				if (!string.IsNullOrEmpty(line))
				{
					line += "  |  ";
				}
				line = line + "DIA " + FormatSignedCurrency(diamondDelta);
			}
			return line;
		}

		private static string FormatSignedCurrency(int amount)
		{
			return ((amount > 0) ? "+" : string.Empty) + amount.ToString("N0");
		}

		private static string BuildDrawPopupSummary(List<OutgameDrawResult> results)
		{
			if (results == null || results.Count == 0)
			{
				return string.Empty;
			}
			if (results.Count > 10)
			{
				return BuildBulkDrawSummary(results);
			}
			string output = "획득 카드 " + results.Count + "장";
			int shown = 0;
			for (int i = 0; i < results.Count; i++)
			{
				OutgameDrawResult result = results[i];
				if (result != null && result.character != null)
				{
					string status = (result.firstAcquisition ? "NEW" : (result.leveledUp ? "LEVEL UP" : "획득"));
					if (result.wishlistHit)
					{
						status += " / 위시";
					}
					else if (result.pityTriggered)
					{
						status += " / 보장";
					}
					output = output + "\n[" + status + "] " + result.character.displayName + " Lv." + result.level;
					shown++;
					if (shown >= 5)
					{
						break;
					}
				}
			}
			if (results.Count > shown)
			{
				output = output + "\n외 " + (results.Count - shown) + "장";
			}
			return output;
		}

		private void HandleCashBundlePurchase(int index)
		{
			if (!(outgameProgression == null))
			{
				int[] goldAmounts = new int[3] { 10000, 0, 20000 };
				int[] diamondAmounts = new int[3] { 0, 1200, 2000 };
				string[] productNames = new string[3] { "골드 주머니", "다이아 주머니", "성장 꾸러미" };
				string[] iconPaths = new string[3] { "GradeAndGoodsIcons/goods_icon_gold", "GradeAndGoodsIcons/goods_icon_ruby", "GradeAndGoodsIcons/goods_icon_reward_box" };
				Color[] accents = new Color[3]
				{
					new Color(1f, 0.74f, 0.2f),
					new Color(0.3f, 0.84f, 1f),
					new Color(0.72f, 0.54f, 1f)
				};
				int safeIndex = Mathf.Clamp(index, 0, productNames.Length - 1);
				if (!outgameProgression.IsTestMode)
				{
					ShowShopPurchaseResultPopup("구매 준비중", productNames[safeIndex] + " 결제 SDK 연결 후 실제 구매가 진행됩니다.", string.Empty, iconPaths[safeIndex], accents[safeIndex]);
					return;
				}
				int beforeGold = outgameProgression.Gold;
				int beforeDiamonds = outgameProgression.Diamonds;
				outgameProgression.GrantTestShopCurrency(goldAmounts[safeIndex], diamondAmounts[safeIndex]);
				RefreshShop();
				PlayShopCurrencyChange(beforeGold, beforeDiamonds);
				ShowShopPurchaseResultPopup("구매 완료", productNames[safeIndex] + "를 구매했습니다.", BuildCurrencyChangeLine(beforeGold, beforeDiamonds, outgameProgression.Gold, outgameProgression.Diamonds), iconPaths[safeIndex], accents[safeIndex]);
			}
		}

		private void TryPurchaseDailyOffer(int index)
		{
			if (outgameProgression == null || drawRevealRoutine != null)
			{
				return;
			}
			int beforeGold = outgameProgression.Gold;
			int beforeDiamonds = outgameProgression.Diamonds;
			if (!outgameProgression.TryPurchaseDailyShopOffer(index, out var results, out var message))
			{
				RefreshShop();
				ShowShopPurchaseResultPopup("구매 실패", message, string.Empty, "Icons/icon-main-menu-shop", new Color(1f, 0.48f, 0.42f));
				return;
			}
			RefreshShop();
			PlayShopCurrencyChange(beforeGold, beforeDiamonds);
			string iconPath = index switch
			{
				1 => "GradeAndGoodsIcons/goods_icon_reward_box", 
				0 => "GradeAndGoodsIcons/goods_icon_gold", 
				_ => "GradeAndGoodsIcons/goods_icon_ruby", 
			};
			Color accent = index switch
			{
				1 => new Color(0.38f, 0.9f, 1f), 
				0 => new Color(1f, 0.74f, 0.2f), 
				_ => new Color(0.78f, 0.55f, 1f), 
			};
			ShowShopPurchaseResultPopup("구매 완료", BuildDailyOfferPurchaseBody(index, results, message), BuildCurrencyChangeLine(beforeGold, beforeDiamonds, outgameProgression.Gold, outgameProgression.Diamonds), iconPath, accent);
			if (results != null && results.Count > 0)
			{
				RuntimeAudioUtility.PlayReroll();
				drawRevealRoutine = StartCoroutine(RevealDrawResults(results));
			}
		}

		private void TryOpenEarnedChest()
		{
			if (!(outgameProgression == null) && drawRevealRoutine == null)
			{
				if (!outgameProgression.TryOpenEarnedChest(out var results))
				{
					RefreshShop();
					ShowShopPurchaseResultPopup("개봉 실패", "무료 상자가 없습니다. 전투 보상이나 게이지 보상으로 상자를 채우세요.", string.Empty, "GradeAndGoodsIcons/goods_icon_reward_box", new Color(1f, 0.48f, 0.42f));
					return;
				}
				RuntimeAudioUtility.PlayReroll();
				RefreshShop();
				string summary = BuildDrawPopupSummary(results);
				ShowShopPurchaseResultPopup("획득 완료", "무료 영웅 상자 1개를 열었습니다." + (string.IsNullOrEmpty(summary) ? string.Empty : ("\n" + summary)), string.Empty, "GradeAndGoodsIcons/goods_icon_reward_box", new Color(0.38f, 0.9f, 1f));
				drawRevealRoutine = StartCoroutine(RevealDrawResults(results));
			}
		}

		private void CycleWishlist()
		{
			if (!(outgameProgression == null) && drawRevealRoutine == null)
			{
				outgameProgression.CycleWishlist();
				string wishlistName = outgameProgression.GetWishlistDisplayName();
				RefreshShop();
				ShowShopPurchaseResultPopup("위시 변경 완료", "위시 영웅을 " + wishlistName + "으로 변경했습니다.\n프리미엄 상자에서 확률 보정되고 20회 안에 확정됩니다.", string.Empty, "Icons/icon-main-menu-collection", new Color(0.78f, 0.55f, 1f));
			}
		}

		private void TryOpenChest(int drawCount)
		{
			if (!(outgameProgression == null) && drawRevealRoutine == null)
			{
				int beforeGold = outgameProgression.Gold;
				int beforeDiamonds = outgameProgression.Diamonds;
				if (!outgameProgression.TryOpenChest(drawCount, out var results))
				{
					RefreshShop();
					ShowShopPurchaseResultPopup("구매 실패", "다이아가 부족합니다.", string.Empty, "GradeAndGoodsIcons/goods_icon_ruby", new Color(1f, 0.48f, 0.42f));
					return;
				}
				RuntimeAudioUtility.PlayReroll();
				RefreshShop();
				PlayShopCurrencyChange(beforeGold, beforeDiamonds);
				string summary = BuildDrawPopupSummary(results);
				ShowShopPurchaseResultPopup("구매 완료", "영웅 카드 x" + drawCount + "를 구매했습니다." + (string.IsNullOrEmpty(summary) ? string.Empty : ("\n" + summary)), BuildCurrencyChangeLine(beforeGold, beforeDiamonds, outgameProgression.Gold, outgameProgression.Diamonds), "GradeAndGoodsIcons/goods_icon_reward_box", new Color(0.38f, 0.9f, 1f));
				drawRevealRoutine = StartCoroutine(RevealDrawResults(results));
			}
		}

		private IEnumerator RevealDrawResults(List<OutgameDrawResult> results)
		{
			SetShopDrawButtonsInteractable(interactable: false);
			SetShopResultHint("상자를 여는 중...\n획득 결과는 팝업으로 표시됩니다.");
			float lockSeconds = ((results == null) ? 0.45f : Mathf.Clamp(0.35f + (float)results.Count * 0.018f, 0.45f, 1.2f));
			yield return new WaitForSecondsRealtime(lockSeconds);
			drawRevealRoutine = null;
			SetShopDrawButtonsInteractable(interactable: true);
			RefreshShop();
		}

		private static string BuildBulkDrawSummary(List<OutgameDrawResult> results)
		{
			int[] gradeCounts = new int[6];
			int newCount = 0;
			int levelUpCount = 0;
			for (int i = 0; i < results.Count; i++)
			{
				OutgameDrawResult result = results[i];
				if (result != null && result.character != null)
				{
					int gradeIndex = Mathf.Clamp((int)result.character.grade, 0, gradeCounts.Length - 1);
					gradeCounts[gradeIndex]++;
					if (result.firstAcquisition)
					{
						newCount++;
					}
					if (result.leveledUp)
					{
						levelUpCount++;
					}
				}
			}
			return "영웅 카드 " + results.Count + "장 획득 완료\n일반 " + gradeCounts[0] + " / 레어 " + gradeCounts[1] + " / 희귀 " + gradeCounts[2] + "\n전설 " + gradeCounts[3] + " / 신화 " + gradeCounts[4] + " / 초월 " + gradeCounts[5] + "\nNEW " + newCount + " / LEVEL UP " + levelUpCount + "\n세부 보유량은 도감에서 확인할 수 있습니다.";
		}

		private void RefreshShop()
		{
			if (outgameProgression == null)
			{
				return;
			}
			if (shopCurrencyCountRoutine == null)
			{
				SetShopCurrencyText(outgameProgression.Gold, outgameProgression.Diamonds);
			}
			if ((UnityEngine.Object)(object)shopRatesText != null)
			{
				shopRatesText.text = outgameProgression.BuildRateText();
			}
			if ((UnityEngine.Object)(object)shopCollectionText != null)
			{
				shopCollectionText.text = outgameProgression.BuildCollectionSummary();
			}
			if ((UnityEngine.Object)(object)shopModeText != null)
			{
				shopModeText.text = (outgameProgression.IsTestMode ? "TEST MODE / 전체 영웅 사용 가능" : "SERVICE MODE / 보유 영웅만 출전");
			}
			if ((UnityEngine.Object)(object)shopTestDiamondButton != null)
			{
				((Component)(object)shopTestDiamondButton).gameObject.SetActive(outgameProgression.IsTestMode);
				Text label = ((Component)(object)shopTestDiamondButton).GetComponentInChildren<Text>();
				if ((UnityEngine.Object)(object)label != null)
				{
					label.text = "다이아 +" + outgameProgression.Settings.testDiamondRechargeAmount.ToString("N0");
				}
			}
			if ((UnityEngine.Object)(object)shopSingleDrawButton != null)
			{
				Text label2 = ((Component)(object)shopSingleDrawButton).GetComponentInChildren<Text>();
				if ((UnityEngine.Object)(object)label2 != null)
				{
					label2.text = "1회 뽑기  " + outgameProgression.Settings.singleChestCost + " DIA";
				}
			}
			if ((UnityEngine.Object)(object)shopTenDrawButton != null)
			{
				Text label3 = ((Component)(object)shopTenDrawButton).GetComponentInChildren<Text>();
				if ((UnityEngine.Object)(object)label3 != null)
				{
					label3.text = "10회 뽑기  " + outgameProgression.Settings.tenChestCost + " DIA";
				}
			}
			if ((UnityEngine.Object)(object)shopEarnedDrawButton != null)
			{
				Text label4 = ((Component)(object)shopEarnedDrawButton).GetComponentInChildren<Text>();
				if ((UnityEngine.Object)(object)label4 != null)
				{
					label4.text = "무료 상자 열기  |  보유 " + outgameProgression.EarnedChestKeys + "개  (게이지 " + outgameProgression.EarnedChestProgress + "/" + outgameProgression.EarnedChestProgressTarget + ")";
				}
				((Selectable)shopEarnedDrawButton).interactable = outgameProgression.EarnedChestKeys > 0 && drawRevealRoutine == null;
			}
			if ((UnityEngine.Object)(object)shopWishlistButton != null)
			{
				Text label5 = ((Component)(object)shopWishlistButton).GetComponentInChildren<Text>();
				if ((UnityEngine.Object)(object)label5 != null)
				{
					label5.text = "위시 영웅  |  " + outgameProgression.GetWishlistDisplayName() + "  >";
				}
			}
			if ((UnityEngine.Object)(object)shopSingleDrawButton != null)
			{
				Text label6 = ((Component)(object)shopSingleDrawButton).GetComponentInChildren<Text>();
				if ((UnityEngine.Object)(object)label6 != null)
				{
					label6.text = "프리미엄 1회  " + outgameProgression.Settings.singleChestCost + " DIA";
				}
				((Selectable)shopSingleDrawButton).interactable = outgameProgression.Diamonds >= outgameProgression.Settings.singleChestCost && drawRevealRoutine == null;
			}
			if ((UnityEngine.Object)(object)shopTenDrawButton != null)
			{
				Text label7 = ((Component)(object)shopTenDrawButton).GetComponentInChildren<Text>();
				if ((UnityEngine.Object)(object)label7 != null)
				{
					label7.text = "프리미엄 10회  " + outgameProgression.Settings.tenChestCost + " DIA";
				}
				((Selectable)shopTenDrawButton).interactable = outgameProgression.Diamonds >= outgameProgression.Settings.tenChestCost && drawRevealRoutine == null;
			}
			if ((UnityEngine.Object)(object)shopDailyResetText != null)
			{
				shopDailyResetText.text = outgameProgression.BuildDailyShopResetLabel();
			}
			RefreshDailyOfferButton(0, "일일 무료 선물", "+" + outgameProgression.Settings.dailyFreeGold.ToString("N0") + " GOLD", "무료", affordable: true);
			RefreshDailyOfferButton(1, "영웅 카드 x" + outgameProgression.Settings.dailyCardPackDrawCount, outgameProgression.Settings.dailyCardPackGoldCost.ToString("N0") + " GOLD", "일일 1회", outgameProgression.Gold >= outgameProgression.Settings.dailyCardPackGoldCost);
			RefreshDailyOfferButton(2, "프리미엄 카드 x" + outgameProgression.Settings.dailyPremiumPackDrawCount, outgameProgression.Settings.dailyPremiumPackDiamondCost.ToString("N0") + " DIA", "일일 1회", outgameProgression.Diamonds >= outgameProgression.Settings.dailyPremiumPackDiamondCost);
			RefreshChestPackButton(shopSingleDrawButton, 5);
			RefreshChestPackButton(shopTenDrawButton, 20);
			RefreshChestPackButton(shopFiftyDrawButton, 50);
			RefreshChestPackButton(shopHundredDrawButton, 100);
			if ((UnityEngine.Object)(object)shopTestDiamondButton != null)
			{
				Text label8 = ((Component)(object)shopTestDiamondButton).GetComponentInChildren<Text>();
				if ((UnityEngine.Object)(object)label8 != null)
				{
					label8.text = "GOLD/DIA 테스트 충전";
				}
			}
		}

		private void RefreshDailyOfferButton(int index, string title, string reward, string footer, bool affordable)
		{
			if (index >= 0 && index < shopDailyOfferButtons.Length && !((UnityEngine.Object)(object)shopDailyOfferButtons[index] == null))
			{
				bool purchased = outgameProgression != null && outgameProgression.IsDailyShopOfferPurchased(index);
				SetButtonLabel(shopDailyOfferButtons[index], purchased ? (title + "\n구매 완료\n내일 갱신") : (title + "\n" + reward + "\n" + footer));
				((Selectable)shopDailyOfferButtons[index]).interactable = !purchased && affordable && drawRevealRoutine == null;
			}
		}

		private void RefreshChestPackButton(Button button, int drawCount)
		{
			if (!((UnityEngine.Object)(object)button == null) && !(outgameProgression == null))
			{
				int cost = outgameProgression.ResolvePremiumChestCost(drawCount);
				SetButtonLabel(button, drawCount + "개\n" + cost.ToString("N0") + " DIA");
				((Selectable)button).interactable = outgameProgression.Diamonds >= cost && drawRevealRoutine == null;
			}
		}

		private void RefreshModeUi()
		{
			if (outgameProgression == null)
			{
				return;
			}
			if ((UnityEngine.Object)(object)lobbyModeText != null)
			{
				lobbyModeText.text = (outgameProgression.IsTestMode ? "TEST MODE" : "SERVICE");
				((Graphic)lobbyModeText).color = (outgameProgression.IsTestMode ? new Color(1f, 0.83f, 0.34f) : new Color(0.43f, 1f, 0.8f));
			}
			if ((UnityEngine.Object)(object)lobbyModeButton != null)
			{
				Text label = ((Component)(object)lobbyModeButton).GetComponentInChildren<Text>();
				if ((UnityEngine.Object)(object)label != null)
				{
					label.text = (outgameProgression.IsTestMode ? "서비스 진입" : "테스트 진입");
				}
			}
			if ((UnityEngine.Object)(object)lobbyFortuneText != null)
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
			SetOutgameNavButtonState(hubShopButton, (UnityEngine.Object)(object)activeButton == (UnityEngine.Object)(object)hubShopButton);
			SetOutgameNavButtonState(hubInventoryButton, (UnityEngine.Object)(object)activeButton == (UnityEngine.Object)(object)hubInventoryButton);
			SetOutgameNavButtonState(hubLobbyButton, (UnityEngine.Object)(object)activeButton == (UnityEngine.Object)(object)hubLobbyButton);
			SetOutgameNavButtonState(hubYahtzeeButton, (UnityEngine.Object)(object)activeButton == (UnityEngine.Object)(object)hubYahtzeeButton);
			SetOutgameNavButtonState(hubRankingButton, (UnityEngine.Object)(object)activeButton == (UnityEngine.Object)(object)hubRankingButton);
		}

		private void SetOutgameNavButtonState(Button button, bool active)
		{
			if (!((UnityEngine.Object)(object)button == null))
			{
				Image image = ((Component)(object)button).GetComponent<Image>();
				if ((UnityEngine.Object)(object)image != null)
				{
					image.sprite = RuntimeUiSkinUtility.GetRoundedPanelSprite();
					image.type = (Type)1;
					image.preserveAspect = false;
					((Graphic)image).color = (active ? new Color(0.28f, 0.38f, 0.9f, 1f) : new Color(0.94f, 0.97f, 1f, 0.99f));
				}
				if (active)
				{
					((Component)(object)button).transform.SetAsLastSibling();
				}
				Transform iconTransform = ((Component)(object)button).transform.Find("NavIcon");
				Image navIcon = ((iconTransform != null) ? iconTransform.GetComponent<Image>() : null);
				Sprite iconSprite = RollRollUiResource.LoadSprite(ResolveOutgameNavIconPath(button, active));
				if ((UnityEngine.Object)(object)navIcon != null && iconSprite != null)
				{
					navIcon.sprite = iconSprite;
					((Graphic)navIcon).color = Color.white;
					navIcon.type = (Type)0;
					navIcon.preserveAspect = true;
				}
				Text label = GetChildText(((Component)(object)button).transform, "NavLabel");
				if ((UnityEngine.Object)(object)label != null)
				{
					ApplyOutgameNavLabelStyle(label, active);
				}
				AnimateOutgameNavButton(button, active);
			}
		}

		private void AnimateOutgameNavButton(Button button, bool active)
		{
			if (!((UnityEngine.Object)(object)button == null) && outgameNavBasePositions.TryGetValue(button, out var basePosition))
			{
				if (outgameNavAnimationRoutines.TryGetValue(button, out var routine) && routine != null)
				{
					StopCoroutine(routine);
				}
				outgameNavAnimationRoutines[button] = StartCoroutine(AnimateOutgameNavButtonRoutine(button, basePosition, active));
			}
		}

		private IEnumerator AnimateOutgameNavButtonRoutine(Button button, Vector2 basePosition, bool active)
		{
			if ((UnityEngine.Object)(object)button == null)
			{
				yield break;
			}
			RectTransform rect = ((Component)(object)button).GetComponent<RectTransform>();
			if (!(rect == null))
			{
				Vector2 startPosition = rect.anchoredPosition;
				Vector2 targetPosition = basePosition + (active ? new Vector2(0f, 32f) : Vector2.zero);
				Vector3 startScale = rect.localScale;
				Vector3 targetScale = (active ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one);
				float elapsed = 0f;
				while (elapsed < 0.18f)
				{
					elapsed += Time.unscaledDeltaTime;
					float t = Mathf.Clamp01(elapsed / 0.18f);
					float eased = Mathf.SmoothStep(0f, 1f, t);
					rect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, eased);
					rect.localScale = Vector3.Lerp(startScale, targetScale, eased);
					yield return null;
				}
				rect.anchoredPosition = targetPosition;
				rect.localScale = targetScale;
				outgameNavAnimationRoutines.Remove(button);
			}
		}

		private static string ResolveOutgameNavIconPath(Button button, bool active)
		{
			string suffix = (active ? "-activated" : string.Empty);
			switch (((UnityEngine.Object)(object)button != null) ? ((UnityEngine.Object)(object)button).name : string.Empty)
			{
			case "OutgameNavShop":
			case "ShopNavShop":
				return "Icons/icon-main-menu-shop" + suffix;
			case "OutgameNavInventory":
			case "ShopNavInventory":
				return "Icons/icon-main-menu-collection" + suffix;
			case "OutgameNavLobby":
			case "ShopNavLobby":
				return "Icons/icon-main-menu-battle" + suffix;
			case "OutgameNavYahtzee":
			case "ShopNavYahtzee":
				return "Icons/icon-main-menu-roll" + suffix;
			case "OutgameNavRanking":
			case "ShopNavRanking":
				return "Icons/icon-main-menu-trophy" + suffix;
			default:
				return "Icons/icon-main-menu-battle" + suffix;
			}
		}

		private void SetShopDrawButtonsInteractable(bool interactable)
		{
			if ((UnityEngine.Object)(object)shopSingleDrawButton != null)
			{
				((Selectable)shopSingleDrawButton).interactable = interactable;
			}
			if ((UnityEngine.Object)(object)shopEarnedDrawButton != null)
			{
				((Selectable)shopEarnedDrawButton).interactable = interactable && outgameProgression != null && outgameProgression.EarnedChestKeys > 0;
			}
			if ((UnityEngine.Object)(object)shopWishlistButton != null)
			{
				((Selectable)shopWishlistButton).interactable = interactable;
			}
			if ((UnityEngine.Object)(object)shopTenDrawButton != null)
			{
				((Selectable)shopTenDrawButton).interactable = interactable;
			}
			if ((UnityEngine.Object)(object)shopFiftyDrawButton != null)
			{
				((Selectable)shopFiftyDrawButton).interactable = interactable;
			}
			if ((UnityEngine.Object)(object)shopHundredDrawButton != null)
			{
				((Selectable)shopHundredDrawButton).interactable = interactable;
			}
		}

		private void HandleProgressChanged()
		{
			RefreshShop();
			RefreshModeUi();
			BuildPresets();
			ApplyRecommendedPreset();
			RefreshLobbyPreparationStatus();
		}

		private void ShowLoadout()
		{
			HideSeasonRanking();
			ApplyRecommendedPreset();
			if (loadoutOverlay != null)
			{
				loadoutOverlay.SetActive(value: true);
			}
		}

		private void HideLoadout()
		{
			if (loadoutOverlay != null)
			{
				loadoutOverlay.SetActive(value: false);
			}
			if (lobbyOverlay != null && lobbyOverlay.activeSelf)
			{
				HighlightOutgameNav(hubLobbyButton);
			}
		}

		private void ShowOutgamePlaceholder(string title, string body)
		{
			HideSeasonRanking();
			SetGameplayHudVisible(visible: false);
			HideLoadout();
			HideShop();
			HideResult();
			HideExitConfirm();
			if (characterCollectionUI != null && characterCollectionUI.IsOpen)
			{
				characterCollectionUI.Close();
			}
			ShowLobby();
			if (outgamePlaceholderOverlay != null)
			{
				Text titleText = GetChildText(outgamePlaceholderOverlay.transform, "PlaceholderTitle");
				if ((UnityEngine.Object)(object)titleText != null)
				{
					titleText.text = title;
				}
				Text bodyText = GetChildText(outgamePlaceholderOverlay.transform, "PlaceholderBody");
				if ((UnityEngine.Object)(object)bodyText != null)
				{
					bodyText.text = body;
				}
				outgamePlaceholderOverlay.SetActive(value: true);
			}
			HighlightOutgameNav(title.Contains("랭킹") ? hubRankingButton : hubYahtzeeButton);
		}

		private void ShowSeasonRanking()
		{
			SetGameplayHudVisible(visible: false);
			HideLoadout();
			HideShop();
			HideResult();
			HideOutgamePlaceholder();
			HideExitConfirm();
			if (characterCollectionUI != null && characterCollectionUI.IsOpen)
			{
				characterCollectionUI.Close();
			}
			ShowLobby();
			RefreshSeasonRanking();
			if (seasonRankingOverlay != null)
			{
				seasonRankingOverlay.SetActive(value: true);
			}
			HighlightOutgameNav(hubRankingButton);
		}

		private void RefreshSeasonRanking()
		{
			int playerScore = ((outgameProgression != null) ? outgameProgression.WeeklyBossScore : 0);
			int seasonId = ((!(outgameProgression != null)) ? 1 : Mathf.Max(1, outgameProgression.CurrentSeasonId));
			List<RankingEntry> entries = new List<RankingEntry>
			{
				new RankingEntry("KR주사위", 3180),
				new RankingEntry("별빛기사", 2870),
				new RankingEntry("행운의눈", 2640),
				new RankingEntry("DiceMaster", 2410),
				new RankingEntry("보라별", 2180),
				new RankingEntry("바람주사위", 1960),
				new RankingEntry("황금망치", 1740),
				new RankingEntry("용감한큐브", 1510),
				new RankingEntry("밤의왕관", 1290),
				new RankingEntry("빛의수호자", 1070),
				new RankingEntry("신비한탑", 840),
				new RankingEntry("레드X", playerScore, isPlayer: true)
			};
			entries.Sort(delegate(RankingEntry left, RankingEntry right)
			{
				int num = right.Score.CompareTo(left.Score);
				return (num != 0) ? num : string.CompareOrdinal(left.Name, right.Name);
			});
			if ((UnityEngine.Object)(object)rankingSeasonText != null)
			{
				rankingSeasonText.text = "SEASON " + seasonId + " · 주간 보스 리그";
			}
			for (int index = 0; index < rankingTopNameTexts.Length; index++)
			{
				RankingEntry entry = entries[index];
				rankingTopNameTexts[index].text = (entry.IsPlayer ? (entry.Name + " (나)") : entry.Name);
				rankingTopScoreTexts[index].text = entry.Score.ToString("N0");
				if ((UnityEngine.Object)(object)rankingTopCardPanels[index] != null)
				{
					((Graphic)rankingTopCardPanels[index]).color = (entry.IsPlayer ? new Color(0.72f, 1f, 1f, 1f) : Color.white);
				}
			}
			for (int index2 = 0; index2 < rankingRowPanels.Length; index2++)
			{
				int entryIndex = index2 + 3;
				RankingEntry entry2 = entries[entryIndex];
				rankingRowRankTexts[index2].text = (entryIndex + 1).ToString();
				rankingRowNameTexts[index2].text = (entry2.IsPlayer ? (entry2.Name + "  ·  나") : entry2.Name);
				ApplyRankingRowPlayerNameStyle(rankingRowNameTexts[index2]);
				rankingRowScoreTexts[index2].text = entry2.Score.ToString("N0");
				((Graphic)rankingRowPanels[index2]).color = new Color32(133, 131, 164, byte.MaxValue);
			}
			int playerRank = entries.FindIndex((RankingEntry rankingEntry) => rankingEntry.IsPlayer) + 1;
			if ((UnityEngine.Object)(object)rankingPlayerSummaryText != null)
			{
				rankingPlayerSummaryText.text = "내 순위 " + playerRank + "위  |  레드X  " + playerScore.ToString("N0") + "점";
			}
			if ((UnityEngine.Object)(object)rankingPlayerProgressText != null)
			{
				int bestRun = ((outgameProgression != null) ? outgameProgression.WeeklyBestRunScore : 0);
				int bossKills = ((outgameProgression != null) ? outgameProgression.WeeklyBossKills : 0);
				rankingPlayerProgressText.text = "최고 런 " + bestRun.ToString("N0") + "점 · 보스 처치 " + bossKills + "회";
			}
		}

		private void CloseSeasonRanking()
		{
			HideSeasonRanking();
			ShowLobby();
		}

		private void HideSeasonRanking()
		{
			if (seasonRankingOverlay != null)
			{
				seasonRankingOverlay.SetActive(value: false);
			}
		}

		private void HideOutgamePlaceholder()
		{
			if (outgamePlaceholderOverlay != null)
			{
				outgamePlaceholderOverlay.SetActive(value: false);
			}
		}

		private void ShowMatchmaking()
		{
			SetGameplayHudVisible(visible: false);
			HideExitConfirm();
			if (matchmakingOverlay != null)
			{
				matchmakingOverlay.SetActive(value: true);
			}
			if ((UnityEngine.Object)(object)queueTimerText != null)
			{
				queueTimerText.text = "00.00";
			}
			if ((UnityEngine.Object)(object)queueStatusText != null)
			{
				queueStatusText.text = "라운드 전장을 준비하는 중...";
			}
			RuntimeAudioUtility.PlayMatching();
		}

		private void HideMatchmaking()
		{
			if (matchmakingOverlay != null)
			{
				matchmakingOverlay.SetActive(value: false);
			}
		}

		private void ShowResult(bool victory, int round)
		{
			if (resultOverlay == null)
			{
				return;
			}
			HideExitConfirm();
			resultOverlay.SetActive(value: true);
			for (int i = 0; i < resultVictoryDecorations.Count; i++)
			{
				if (resultVictoryDecorations[i] != null)
				{
					resultVictoryDecorations[i].SetActive(victory);
				}
			}
			if ((UnityEngine.Object)(object)resultRibbonImage != null)
			{
				((Graphic)resultRibbonImage).color = (victory ? new Color(0.17f, 0.42f, 1f, 0.92f) : new Color(0.7f, 0.18f, 0.22f, 0.92f));
			}
			Color accent = (victory ? new Color(1f, 0.84f, 0.18f) : new Color(1f, 0.45f, 0.45f));
			if ((UnityEngine.Object)(object)resultTitleText != null)
			{
				resultTitleText.text = (victory ? "승리" : "패배");
				RuntimeUiSkinUtility.ApplyReadableTextColor(resultTitleText, accent, uiSkin);
			}
			if ((UnityEngine.Object)(object)resultSummaryText != null)
			{
				resultSummaryText.text = (victory ? ("라운드 " + round + " 클리어") : ("라운드 " + round + " 에서 패배"));
			}
			if ((UnityEngine.Object)(object)resultMetaText != null)
			{
				resultMetaText.text = (victory ? "연속 클리어 +1  |  다음 라운드 준비 완료" : "덱을 다시 정비하고 재도전할 수 있습니다.");
			}
			if ((UnityEngine.Object)(object)resultScoreText != null)
			{
				resultScoreText.text = (victory ? "RUN SCORE A / 000점" : "RUN SCORE C / 000점");
			}
			if ((UnityEngine.Object)(object)resultRecapText != null)
			{
				resultRecapText.text = "딜러 기록 없음  |  시너지 없음\n타일 기여 없음  |  초반 1~10R 계측 대기";
			}
			if ((UnityEngine.Object)(object)resultNextText != null)
			{
				resultNextText.text = (victory ? "다음 행동\n도감 강화: 주력 카드 성장 확인\n상점 뽑기: 부족한 등급 보충\n다음 보스 대비: 보스 타일과 딜러 유지" : "다음 행동\n도감 강화: 약한 주력 카드 보강\n상점 뽑기: 부족한 등급 보충\n다음 보스 대비: 실패 원인 재정비");
			}
			if (gameController != null)
			{
				if ((UnityEngine.Object)(object)resultTitleText != null)
				{
					resultTitleText.text = (victory ? "승리" : "패배");
				}
				if ((UnityEngine.Object)(object)resultSummaryText != null)
				{
					resultSummaryText.text = gameController.RunNextGoalHeadline;
				}
				if ((UnityEngine.Object)(object)resultMetaText != null)
				{
					RectTransform metaRect = ((Graphic)resultMetaText).rectTransform;
					if (metaRect != null)
					{
						metaRect.sizeDelta = new Vector2(700f, 40f);
					}
					resultMetaText.fontSize = 22;
					resultMetaText.alignment = TextAnchor.MiddleCenter;
					resultMetaText.text = (victory ? ("이번 결과: 라운드 " + round + " 클리어") : ("이번 결과: 라운드 " + round + " 패배"));
				}
				if ((UnityEngine.Object)(object)resultScoreText != null)
				{
					resultScoreText.text = "RUN SCORE " + gameController.RunPerformanceGrade + " / " + gameController.RunPerformanceScore + "점";
				}
				if ((UnityEngine.Object)(object)resultRecapText != null)
				{
					resultRecapText.text = gameController.RunResultFocusSummary;
				}
				if ((UnityEngine.Object)(object)resultNextText != null)
				{
					resultNextText.text = gameController.RunResultNextCompactSummary;
				}
			}
			ApplyReadableResultTextLayout();
			int goldReward = ((victory && gameController != null && gameController.LastRoundClearGoldReward > 0) ? gameController.LastRoundClearGoldReward : (victory ? (110 + round * 18) : Mathf.Max(20, 40 + round * 6)));
			int coreReward = (victory ? (6 + Mathf.Max(1, round / 2)) : ((gameController != null) ? gameController.EarnedGrowthCurrency : Mathf.Max(2, round / 3 + 2)));
			int diamondReward = ((outgameProgression != null) ? outgameProgression.ResolveBattleDiamondReward(coreReward) : coreReward);
			if (!resultRewardGranted && outgameProgression != null)
			{
				outgameProgression.AddDiamonds(diamondReward);
				if (gameController != null)
				{
					outgameProgression.RecordSeasonRun(gameController.RunPerformanceScore, gameController.RunBossScore, gameController.RunBossKillCount, gameController.RunMvpName, round, victory);
				}
				resultRewardGranted = true;
			}
			if ((UnityEngine.Object)(object)resultMetaText != null && gameController != null)
			{
				string meta = (victory ? ("이번 결과: 라운드 " + round + " 클리어") : ("이번 결과: 라운드 " + round + " 패배"));
				if (outgameProgression != null && !string.IsNullOrWhiteSpace(outgameProgression.LastSeasonRewardSummary))
				{
					meta = meta + "  |  " + outgameProgression.LastSeasonRewardSummary;
				}
				resultMetaText.text = meta;
			}
			if ((UnityEngine.Object)(object)resultRewardGoldText != null)
			{
				resultRewardGoldText.text = "+" + goldReward;
			}
			if ((UnityEngine.Object)(object)resultRewardCoreText != null)
			{
				resultRewardCoreText.text = "+" + diamondReward;
			}
			if ((UnityEngine.Object)(object)resultContinueButton != null)
			{
				RectTransform continueRect = ((Component)(object)resultContinueButton).GetComponent<RectTransform>();
				if (continueRect != null)
				{
					continueRect.anchoredPosition = (victory ? new Vector2(0f, 75f) : new Vector2(190f, 75f));
					continueRect.sizeDelta = (victory ? new Vector2(340f, 100f) : new Vector2(220f, 100f));
				}
				SetButtonLabel(resultContinueButton, victory ? "계속하기" : "재정비");
			}
			if ((UnityEngine.Object)(object)resultRetryButton != null)
			{
				((Component)(object)resultRetryButton).gameObject.SetActive(!victory);
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
				resultOverlay.SetActive(value: false);
			}
		}

		private void ContinueFromResult()
		{
			bool completedRound = !defeatPresented;
			HideResult();
			if (completedRound)
			{
				gameController?.ReleasePostRoundChoiceFlow();
			}
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
			HandleEnterPreparationPressed();
		}

		private GameObject CreateOverlayRoot(Transform parent, string name, Color blockerColor)
		{
			GameObject overlay = new GameObject(name, typeof(RectTransform));
			overlay.transform.SetParent(parent, worldPositionStays: false);
			Image blocker = overlay.AddComponent<Image>();
			((Graphic)blocker).color = blockerColor;
			((Graphic)blocker).raycastTarget = true;
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
			panelObject.transform.SetParent(parent, worldPositionStays: false);
			Image image = panelObject.AddComponent<Image>();
			((Graphic)image).color = color;
			((Graphic)image).raycastTarget = false;
			RuntimeUiSkinUtility.ApplyImageSkin(image, uiSkin, name, isButton: false, rounded);
			RollRollUiResource.TryApplyElementSprite(image, name, isButton: false, rounded);
			RectTransform rect = ((Graphic)image).rectTransform;
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

		private Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color backgroundColor, UnityAction onClick, int fontSize)
		{
			GameObject buttonObject = new GameObject(name, typeof(RectTransform));
			buttonObject.transform.SetParent(parent, worldPositionStays: false);
			Image image = buttonObject.AddComponent<Image>();
			((Graphic)image).color = backgroundColor;
			RuntimeUiSkinUtility.ApplyImageSkin(image, uiSkin, name, isButton: true, rounded: true);
			RollRollUiResource.TryApplyElementSprite(image, name, isButton: true, rounded: true);
			((Graphic)image).raycastTarget = true;
			Shadow shadow = buttonObject.AddComponent<Shadow>();
			shadow.effectColor = new Color(0f, 0f, 0f, 0.34f);
			shadow.effectDistance = new Vector2(0f, -6f);
			Button button = buttonObject.AddComponent<Button>();
			AddButtonListener(button, onClick);
			RectTransform rect = ((Component)(object)button).GetComponent<RectTransform>();
			rect.anchorMin = anchorMin;
			rect.anchorMax = anchorMax;
			rect.pivot = pivot;
			rect.anchoredPosition = anchoredPosition;
			rect.sizeDelta = size;
			CreateText(buttonObject.transform, "Label", label, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, fontSize, TextAnchor.MiddleCenter, bold: true);
			return button;
		}

		private Image CreateShopArtwork(Transform parent, string name, string resourcePath, Vector2 anchoredPosition, Vector2 size, Color color, Vector2 anchor)
		{
			GameObject artworkObject = new GameObject(name, typeof(RectTransform));
			artworkObject.transform.SetParent(parent, worldPositionStays: false);
			Image artwork = artworkObject.AddComponent<Image>();
			((Graphic)artwork).color = color;
			((Graphic)artwork).raycastTarget = false;
			artwork.preserveAspect = true;
			artwork.sprite = RollRollUiResource.LoadSprite(resourcePath);
			RectTransform rect = ((Graphic)artwork).rectTransform;
			rect.anchorMin = anchor;
			rect.anchorMax = anchor;
			rect.pivot = anchor;
			rect.anchoredPosition = anchoredPosition;
			rect.sizeDelta = size;
			return artwork;
		}

		private void DecorateShopProductCard(Button button, string iconResourcePath, Color accent, bool compact)
		{
			if (!((UnityEngine.Object)(object)button == null))
			{
				Image cardImage = ((Component)(object)button).GetComponent<Image>();
				if ((UnityEngine.Object)(object)cardImage != null)
				{
					cardImage.sprite = RuntimeUiSkinUtility.GetRoundedPanelSprite();
					cardImage.type = (Type)1;
					cardImage.preserveAspect = false;
					((Graphic)cardImage).color = Color.Lerp(new Color(0.18f, 0.24f, 0.52f, 1f), accent, 0.58f);
				}
				float iconSize = (compact ? 70f : 96f);
				float iconY = (compact ? 30f : 42f);
				CreateShopArtwork(((Component)(object)button).transform, "ShopProductIcon", iconResourcePath, new Vector2(0f, iconY), new Vector2(iconSize, iconSize), Color.white, new Vector2(0.5f, 0.5f));
				Text label = GetChildText(((Component)(object)button).transform, "Label");
				if ((UnityEngine.Object)(object)label != null)
				{
					RectTransform labelRect = ((Graphic)label).rectTransform;
					labelRect.anchorMin = new Vector2(0f, 0f);
					labelRect.anchorMax = new Vector2(1f, 0f);
					labelRect.pivot = new Vector2(0.5f, 0f);
					labelRect.anchoredPosition = new Vector2(0f, compact ? 5f : 8f);
					labelRect.sizeDelta = new Vector2(-20f, compact ? 56f : 78f);
					label.fontSize = (compact ? 17 : 18);
					label.resizeTextForBestFit = true;
					label.resizeTextMinSize = (compact ? 13 : 14);
					label.resizeTextMaxSize = (compact ? 17 : 18);
					((Component)(object)label).transform.SetAsLastSibling();
				}
				Outline outline = ((Component)(object)button).gameObject.AddComponent<Outline>();
				((Shadow)outline).effectColor = new Color(accent.r, accent.g, accent.b, 0.72f);
				((Shadow)outline).effectDistance = new Vector2(2f, -2f);
			}
		}

		private static void AddButtonListener(Button button, UnityAction onClick)
		{
			if (!((UnityEngine.Object)(object)button == null))
			{
				((UnityEvent)(object)button.onClick).AddListener((UnityAction)RuntimeAudioUtility.PlayButton);
				if (onClick != null)
				{
					((UnityEvent)(object)button.onClick).AddListener(onClick);
				}
			}
		}

		private static void SetButtonLabel(Button button, string label)
		{
			if (!((UnityEngine.Object)(object)button == null))
			{
				Text text = ((Component)(object)button).GetComponentInChildren<Text>();
				if ((UnityEngine.Object)(object)text != null)
				{
					text.text = label;
				}
			}
		}

		private Text CreateText(Transform parent, string name, string value, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, bool bold)
		{
			GameObject textObject = new GameObject(name, typeof(RectTransform));
			textObject.transform.SetParent(parent, worldPositionStays: false);
			Text text = textObject.AddComponent<Text>();
			text.font = font;
			text.text = RuntimeKoreanTextUtility.Clean(name, value);
			((Graphic)text).color = RuntimeUiSkinUtility.ResolveReadableTextColor(parent, color, uiSkin);
			text.fontSize = fontSize;
			text.alignment = alignment;
			text.fontStyle = (bold ? FontStyle.Bold : FontStyle.Normal);
			((Graphic)text).raycastTarget = false;
			RectTransform rect = ((Component)(object)text).GetComponent<RectTransform>();
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
			Text[] texts = parent.GetComponentsInChildren<Text>(includeInactive: true);
			for (int i = 0; i < texts.Length; i++)
			{
				if ((UnityEngine.Object)(object)texts[i] != null && ((UnityEngine.Object)(object)texts[i]).name == childName)
				{
					return texts[i];
				}
			}
			return null;
		}

		private void MoveRectInto(Transform newParent, Transform oldParent, string childName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
		{
			if (!(newParent == null) && !(oldParent == null) && !string.IsNullOrWhiteSpace(childName))
			{
				Transform child = oldParent.Find(childName);
				if (!(child == null))
				{
					child.SetParent(newParent, worldPositionStays: false);
					RectTransform rect = child.GetComponent<RectTransform>();
					SetRect(rect, anchorMin, anchorMax, pivot, anchoredPosition, size);
				}
			}
		}

		private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
		{
			if (!(rect == null))
			{
				rect.anchorMin = anchorMin;
				rect.anchorMax = anchorMax;
				rect.pivot = pivot;
				rect.anchoredPosition = anchoredPosition;
				rect.sizeDelta = size;
			}
		}

		private void SetCardLabelColors(Text primaryText, Text secondaryText)
		{
			if ((UnityEngine.Object)(object)primaryText != null)
			{
				((Graphic)primaryText).color = new Color(0.98f, 0.99f, 1f, 1f);
				AddReadableOutline(primaryText);
			}
			if ((UnityEngine.Object)(object)secondaryText != null)
			{
				((Graphic)secondaryText).color = new Color(0.82f, 0.92f, 1f, 1f);
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
			if (!((UnityEngine.Object)(object)portraitImage == null))
			{
				((Component)(object)portraitImage).gameObject.SetActive(value: true);
				((Behaviour)(object)portraitImage).enabled = true;
				((Graphic)portraitImage).raycastTarget = false;
				Sprite sprite = RollRollUiResource.ResolveCharacterSprite(definition);
				if (sprite != null && definition != null)
				{
					portraitImage.sprite = sprite;
					portraitImage.type = (Type)0;
					portraitImage.preserveAspect = true;
					((Graphic)portraitImage).color = Color.white;
				}
				else
				{
					RollRollUiResource.TryApplyElementSprite(portraitImage, "Portrait", isButton: false, rounded: true);
					((Graphic)portraitImage).color = new Color(0.8f, 0.84f, 0.95f, 1f);
				}
			}
		}

		private static void ApplyOutgameNavLabelStyle(Text label, bool active)
		{
			if (!((UnityEngine.Object)(object)label == null))
			{
				((Graphic)label).color = (active ? Color.white : ((Color)new Color32(26, 34, 75, byte.MaxValue)));
				Shadow shadow = ((Component)(object)label).GetComponent<Shadow>();
				if ((UnityEngine.Object)(object)shadow != null)
				{
					shadow.effectColor = (active ? new Color(0f, 0f, 0f, 0.72f) : new Color(1f, 1f, 1f, 0.92f));
					shadow.effectDistance = new Vector2(1.2f, -1.2f);
				}
				Outline outline = ((Component)(object)label).GetComponent<Outline>();
				if ((UnityEngine.Object)(object)outline == null)
				{
					outline = ((Component)(object)label).gameObject.AddComponent<Outline>();
				}
				((Shadow)outline).effectColor = (active ? new Color(0f, 0f, 0f, 0.88f) : new Color(1f, 1f, 1f, 0.96f));
				((Shadow)outline).effectDistance = new Vector2(1.1f, -1.1f);
			}
		}

		private static void ApplyTopRankingNameStyle(Text text)
		{
			if (!((UnityEngine.Object)(object)text == null))
			{
				((Graphic)text).color = Color.white;
				Shadow shadow = ((Component)(object)text).GetComponent<Shadow>();
				if ((UnityEngine.Object)(object)shadow != null)
				{
					shadow.effectColor = Color.black;
					shadow.effectDistance = new Vector2(1.5f, -1.5f);
				}
				Outline outline = ((Component)(object)text).GetComponent<Outline>();
				if ((UnityEngine.Object)(object)outline == null)
				{
					outline = ((Component)(object)text).gameObject.AddComponent<Outline>();
				}
				((Shadow)outline).effectColor = Color.black;
				((Shadow)outline).effectDistance = new Vector2(1.2f, -1.2f);
			}
		}

		private static void ApplyRankingRowPlayerNameStyle(Text text)
		{
			if (!((UnityEngine.Object)(object)text == null))
			{
				((Graphic)text).color = Color.white;
				Shadow shadow = ((Component)(object)text).GetComponent<Shadow>();
				if ((UnityEngine.Object)(object)shadow == null)
				{
					shadow = ((Component)(object)text).gameObject.AddComponent<Shadow>();
				}
				shadow.effectColor = Color.black;
				shadow.effectDistance = new Vector2(2f, -2f);
				Outline outline = ((Component)(object)text).GetComponent<Outline>();
				if ((UnityEngine.Object)(object)outline == null)
				{
					outline = ((Component)(object)text).gameObject.AddComponent<Outline>();
				}
				((Shadow)outline).effectColor = Color.black;
				((Shadow)outline).effectDistance = new Vector2(1.2f, -1.2f);
			}
		}

		private void AddReadableOutline(Text text)
		{
			if (!((UnityEngine.Object)(object)text == null))
			{
				Outline outline = ((Component)(object)text).GetComponent<Outline>();
				if ((UnityEngine.Object)(object)outline == null)
				{
					outline = ((Component)(object)text).gameObject.AddComponent<Outline>();
				}
				((Shadow)outline).effectColor = new Color(0f, 0f, 0f, 0.76f);
				((Shadow)outline).effectDistance = new Vector2(1.5f, -1.5f);
			}
		}

		private Sprite GetRoundedSprite()
		{
			if (roundedSprite != null)
			{
				return roundedSprite;
			}
			int size = 64;
			float radius = 18f;
			Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, mipChain: false);
			texture.wrapMode = TextureWrapMode.Clamp;
			Color[] pixels = new Color[size * size];
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float nearestX = Mathf.Clamp(x, radius, (float)size - radius - 1f);
					float nearestY = Mathf.Clamp(y, radius, (float)size - radius - 1f);
					float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
					float alpha = Mathf.Clamp01(radius + 0.5f - distance);
					pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
				}
			}
			texture.SetPixels(pixels);
			texture.Apply();
			roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
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
			return role switch
			{
				CharacterRole.Vanguard => "전위", 
				CharacterRole.Ranger => "사수", 
				CharacterRole.Mage => "마법", 
				CharacterRole.Support => "지원", 
				CharacterRole.Assassin => "암살", 
				_ => "소환", 
			};
		}

		private Color GetGradeColor(CharacterGrade grade, Color fallback)
		{
			return CharacterGradeUtility.GetColor(grade, fallback);
		}
	}
}
