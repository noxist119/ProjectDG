using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenseGame;

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
		if ((Object)(object)outgameNavigationRoot != (Object)null)
		{
			if (Application.isPlaying)
			{
				Object.Destroy((Object)(object)outgameNavigationRoot);
			}
			else
			{
				Object.DestroyImmediate((Object)(object)outgameNavigationRoot);
			}
			outgameNavigationRoot = null;
		}
		if ((Object)(object)root != (Object)null)
		{
			if (Application.isPlaying)
			{
				Object.Destroy((Object)(object)root);
			}
			else
			{
				Object.DestroyImmediate((Object)(object)root);
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
		if ((Object)(object)gameController != (Object)null && gameController.CurrentRound <= 0 && !gameController.IsRoundRunning)
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
		if (!subscribed && !((Object)(object)gameController == (Object)null))
		{
			gameController.OnRoundStarted += HandleRoundStarted;
			gameController.OnRoundCompleted += HandleRoundCompleted;
			gameController.OnGameOver += HandleGameOver;
			if ((Object)(object)outgameProgression != (Object)null)
			{
				outgameProgression.OnProgressChanged += HandleProgressChanged;
			}
			subscribed = true;
		}
	}

	private void Unsubscribe()
	{
		if (subscribed && !((Object)(object)gameController == (Object)null))
		{
			gameController.OnRoundStarted -= HandleRoundStarted;
			gameController.OnRoundCompleted -= HandleRoundCompleted;
			gameController.OnGameOver -= HandleGameOver;
			if ((Object)(object)outgameProgression != (Object)null)
			{
				outgameProgression.OnProgressChanged -= HandleProgressChanged;
			}
			subscribed = false;
		}
	}

	private void SubscribeCollectionClosed()
	{
		if (!((Object)(object)subscribedCollectionUI == (Object)(object)characterCollectionUI))
		{
			UnsubscribeCollectionClosed();
			subscribedCollectionUI = characterCollectionUI;
			if ((Object)(object)subscribedCollectionUI != (Object)null)
			{
				subscribedCollectionUI.OnClosed += HandleCollectionClosed;
				subscribedCollectionUI.OnOpened += HandleCollectionOpened;
			}
		}
	}

	private void UnsubscribeCollectionClosed()
	{
		if ((Object)(object)subscribedCollectionUI != (Object)null)
		{
			subscribedCollectionUI.OnClosed -= HandleCollectionClosed;
			subscribedCollectionUI.OnOpened -= HandleCollectionOpened;
			subscribedCollectionUI = null;
		}
	}

	private void Build(Transform parent)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Expected O, but got Unknown
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		root = new GameObject("MetaFlowOverlayRoot", new Type[1] { typeof(RectTransform) });
		root.transform.SetParent(parent, false);
		RectTransform component = root.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		BuildLobbyOverlay(root.transform);
		BuildMatchmakingOverlay(root.transform);
		BuildResultOverlay(root.transform);
		BuildLoadoutOverlay(root.transform);
		BuildOutgamePlaceholderOverlay(root.transform);
		BuildSeasonRankingOverlay(root.transform);
		BuildExitConfirmOverlay(root.transform);
		outgameNavigationRoot = new GameObject("OutgameNavigationRoot", new Type[1] { typeof(RectTransform) });
		outgameNavigationRoot.transform.SetParent(parent, false);
		RectTransform component2 = outgameNavigationRoot.GetComponent<RectTransform>();
		component2.anchorMin = Vector2.zero;
		component2.anchorMax = Vector2.one;
		component2.offsetMin = Vector2.zero;
		component2.offsetMax = Vector2.zero;
		BuildOutgameBottomNav(outgameNavigationRoot.transform);
	}

	private void BuildLobbyOverlay(Transform parent)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Expected O, but got Unknown
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0398: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03de: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0415: Unknown result type (might be due to invalid IL or missing references)
		//IL_0424: Unknown result type (might be due to invalid IL or missing references)
		//IL_0429: Unknown result type (might be due to invalid IL or missing references)
		//IL_0438: Unknown result type (might be due to invalid IL or missing references)
		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
		//IL_0472: Unknown result type (might be due to invalid IL or missing references)
		//IL_0481: Unknown result type (might be due to invalid IL or missing references)
		//IL_0490: Unknown result type (might be due to invalid IL or missing references)
		//IL_049f: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_050a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0519: Unknown result type (might be due to invalid IL or missing references)
		//IL_0528: Unknown result type (might be due to invalid IL or missing references)
		//IL_054d: Unknown result type (might be due to invalid IL or missing references)
		//IL_055c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0575: Unknown result type (might be due to invalid IL or missing references)
		//IL_0584: Unknown result type (might be due to invalid IL or missing references)
		//IL_0593: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_05de: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_060b: Unknown result type (might be due to invalid IL or missing references)
		//IL_061a: Unknown result type (might be due to invalid IL or missing references)
		//IL_063f: Unknown result type (might be due to invalid IL or missing references)
		//IL_064e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0667: Unknown result type (might be due to invalid IL or missing references)
		//IL_0676: Unknown result type (might be due to invalid IL or missing references)
		//IL_0685: Unknown result type (might be due to invalid IL or missing references)
		//IL_0694: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_06df: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_070c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0730: Unknown result type (might be due to invalid IL or missing references)
		//IL_073f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0758: Unknown result type (might be due to invalid IL or missing references)
		//IL_0767: Unknown result type (might be due to invalid IL or missing references)
		//IL_0776: Unknown result type (might be due to invalid IL or missing references)
		//IL_0785: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_07fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_082e: Unknown result type (might be due to invalid IL or missing references)
		//IL_083d: Unknown result type (might be due to invalid IL or missing references)
		//IL_084c: Unknown result type (might be due to invalid IL or missing references)
		//IL_085b: Unknown result type (might be due to invalid IL or missing references)
		//IL_086a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0879: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_08b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0915: Unknown result type (might be due to invalid IL or missing references)
		//IL_0924: Unknown result type (might be due to invalid IL or missing references)
		//IL_0933: Unknown result type (might be due to invalid IL or missing references)
		//IL_0942: Unknown result type (might be due to invalid IL or missing references)
		//IL_0951: Unknown result type (might be due to invalid IL or missing references)
		//IL_0960: Unknown result type (might be due to invalid IL or missing references)
		//IL_0986: Unknown result type (might be due to invalid IL or missing references)
		//IL_0995: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a06: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a15: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a24: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a33: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a42: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a71: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a80: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a8f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a9e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ac6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ade: Expected O, but got Unknown
		//IL_0b08: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b17: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b26: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b35: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b44: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b53: Unknown result type (might be due to invalid IL or missing references)
		lobbyOverlay = CreateOverlayRoot(parent, "LobbyOverlay", Color.clear);
		((Graphic)lobbyOverlay.GetComponent<Image>()).raycastTarget = false;
		Image val = CreatePanel(lobbyOverlay.transform, "LobbyModal", new Vector2(0f, 76f), new Vector2(0f, -152f), Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: false, shadow: false);
		RollRollUiResource.TryApplySprite(val, "Common/loot-box-background", (Type)0, preserveAspect: false);
		((Graphic)val).color = Color.white;
		CreatePanel(((Component)val).transform, "TopBanner", new Vector2(0f, -170f), new Vector2(760f, 104f), new Color(0.84f, 0.92f, 1f, 0.18f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreateText(((Component)val).transform, "LobbyTitle", "로비", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(260f, 50f), 38, (TextAnchor)4, bold: true);
		CreateText(((Component)val).transform, "LobbySubTitle", "전투를 준비하세요.", new Color(0.86f, 0.91f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(720f, 40f), 22, (TextAnchor)4, bold: false);
		lobbyModeText = CreateText(((Component)val).transform, "LobbyModeText", "SERVICE", new Color(0.43f, 1f, 0.8f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -120f), new Vector2(162f, 34f), 19, (TextAnchor)3, bold: true);
		lobbyModeButton = CreateButton(((Component)val).transform, "LobbyModeButton", "테스트 진입", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-44f, -300f), new Vector2(156f, 54f), new Color(0.23f, 0.72f, 0.82f, 1f), new UnityAction(TogglePlayMode), 18);
		lobbyFortuneText = CreateText(((Component)val).transform, "LobbyFortuneText", DailyFortuneSystem.TodaySummary, new Color(1f, 0.88f, 0.4f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(720f, 30f), 18, (TextAnchor)4, bold: true);
		Image val2 = CreatePanel(((Component)val).transform, "LobbyReadyPanel", new Vector2(0f, -650f), new Vector2(760f, 250f), new Color(0.1f, 0.16f, 0.38f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		CreateShopArtwork(((Component)val2).transform, "LobbyReadyIcon", "Icons/icon-main-menu-battle", new Vector2(0f, -26f), new Vector2(92f, 92f), Color.white, new Vector2(0.5f, 1f));
		CreateText(((Component)val2).transform, "LobbyReadyTitle", "전투 준비 완료", new Color(1f, 0.88f, 0.36f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(480f, 44f), 32, (TextAnchor)4, bold: true);
		CreateText(((Component)val2).transform, "LobbyReadyBody", "전장에 입장한 뒤 유닛을 소환하고\n다음 라운드로 전투를 시작하세요.", new Color(0.84f, 0.91f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(620f, 66f), 21, (TextAnchor)4, bold: false);
		Image val3 = CreatePanel(((Component)val).transform, "LobbyStatusPanel", new Vector2(0f, -950f), new Vector2(760f, 194f), new Color(0.08f, 0.13f, 0.33f, 0.9f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		CreateText(((Component)val3).transform, "LobbyStatusTitle", "오늘의 준비 현황", new Color(0.42f, 0.94f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(480f, 36f), 24, (TextAnchor)4, bold: true);
		Image val4 = CreatePanel(((Component)val3).transform, "LobbyCollectionStatus", new Vector2(-238f, -70f), new Vector2(220f, 92f), new Color(0.15f, 0.23f, 0.5f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		Image val5 = CreatePanel(((Component)val3).transform, "LobbyChestStatus", new Vector2(0f, -70f), new Vector2(220f, 92f), new Color(0.14f, 0.39f, 0.38f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		Image val6 = CreatePanel(((Component)val3).transform, "LobbyRecordStatus", new Vector2(238f, -70f), new Vector2(220f, 92f), new Color(0.32f, 0.22f, 0.56f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreateText(((Component)val4).transform, "LobbyCollectionLabel", "컬렉션", new Color(0.8f, 0.9f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(190f, 28f), 17, (TextAnchor)4, bold: true);
		CreateText(((Component)val5).transform, "LobbyChestLabel", "무료 상자", new Color(0.72f, 1f, 0.8f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(190f, 28f), 17, (TextAnchor)4, bold: true);
		CreateText(((Component)val6).transform, "LobbyRecordLabel", "최고 기록", new Color(0.9f, 0.82f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(190f, 28f), 17, (TextAnchor)4, bold: true);
		lobbyCollectionSummaryText = CreateText(((Component)val4).transform, "LobbyCollectionValue", "보유 영웅", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(198f, 38f), 16, (TextAnchor)4, bold: false);
		lobbyChestStatusText = CreateText(((Component)val5).transform, "LobbyChestValue", "상자 준비", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(198f, 38f), 17, (TextAnchor)4, bold: true);
		lobbyRecordStatusText = CreateText(((Component)val6).transform, "LobbyRecordValue", "최고 R1", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(198f, 38f), 18, (TextAnchor)4, bold: true);
		lobbyBattleButton = CreateButton(((Component)val).transform, "LobbyBattleButton", "전장 입장", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 500f), new Vector2(420f, 150f), new Color(0.98f, 0.2f, 0.13f, 1f), new UnityAction(HandleEnterPreparationPressed), 45);
		CreateText(((Component)val).transform, "LobbyBottomHint", "전장 입장 후 유닛을 소환하면 전투를 시작할 수 있습니다.", new Color(0.88f, 0.92f, 1f, 0.88f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 400f), new Vector2(760f, 38f), 19, (TextAnchor)4, bold: false);
	}

	private void BuildOutgameBottomNav(Transform parent)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Expected O, but got Unknown
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Expected O, but got Unknown
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Expected O, but got Unknown
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Expected O, but got Unknown
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Expected O, but got Unknown
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Expected O, but got Unknown
		Image val = CreatePanel(parent, "OutgameBottomNavDock", new Vector2(0f, 0f), new Vector2(0f, 152f), new Color(0.88f, 0.93f, 1f, 0.96f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), rounded: false, shadow: true);
		CreatePanel(((Component)val).transform, "DockTopLine", new Vector2(0f, 150f), new Vector2(0f, 4f), new Color(1f, 1f, 1f, 0.7f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), rounded: false, shadow: false);
		RectTransform parent2 = CreateOutgameNavTabsLayer(parent, "OutgameBottomNavTabs");
		hubShopButton = CreateOutgameNavButton((Transform)(object)parent2, "OutgameNavShop", "상점", "SHOP", new Vector2(-432f, 140f), new Color(1f, 0.58f, 0.76f), new UnityAction(ToggleShop));
		hubInventoryButton = CreateOutgameNavButton((Transform)(object)parent2, "OutgameNavInventory", "인벤", "CARD", new Vector2(-216f, 140f), new Color(0.98f, 0.36f, 0.36f), new UnityAction(ShowCollectionTab));
		hubLobbyButton = CreateOutgameNavButton((Transform)(object)parent2, "OutgameNavLobby", "로비", "HOME", new Vector2(0f, 140f), new Color(0.3f, 0.62f, 1f), new UnityAction(ShowLobbyTab));
		hubYahtzeeButton = CreateOutgameNavButton((Transform)(object)parent2, "OutgameNavYahtzee", "얏찌", "DICE", new Vector2(216f, 140f), new Color(1f, 0.62f, 0.22f), (UnityAction)delegate
		{
			ShowOutgamePlaceholder("얏찌", "주사위 기반 보너스 컨텐츠 자리입니다.\n운빨존많겜식 일일/주간 변동 컨텐츠 후보로 남겨둡니다.");
		});
		((UnityEventBase)hubYahtzeeButton.onClick).RemoveAllListeners();
		((UnityEvent)hubYahtzeeButton.onClick).AddListener((UnityAction)delegate
		{
			ShowOutgamePlaceholder("얏찌", "얏찌에서 무료 상자 게이지와 상자 키를 획득하는 구조입니다.\n게임만 해도 같은 유닛 풀을 얻으며, 실제 얏찌 룰 확정 후 점수·족보 보상이 이 게이지에 연결됩니다.");
		});
		hubRankingButton = CreateOutgameNavButton((Transform)(object)parent2, "OutgameNavRanking", "랭킹", "CUP", new Vector2(432f, 140f), new Color(0.74f, 0.52f, 1f), new UnityAction(ShowSeasonRanking));
		HighlightOutgameNav(hubLobbyButton);
	}

	private RectTransform CreateOutgameNavTabsLayer(Transform parent, string name)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0f, 0f);
		component.anchorMax = new Vector2(1f, 0f);
		component.pivot = new Vector2(0.5f, 0f);
		component.anchoredPosition = Vector2.zero;
		component.sizeDelta = new Vector2(0f, 232f);
		val.transform.SetAsLastSibling();
		return component;
	}

	private Button CreateOutgameNavButton(Transform parent, string name, string label, string icon, Vector2 position, Color accent, UnityAction action)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		Button val = CreateButton(parent, name, string.Empty, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), position, new Vector2(164f, 118f), new Color(0.93f, 0.96f, 1f, 0.98f), action, 20);
		Image component = ((Component)val).GetComponent<Image>();
		if ((Object)(object)component != (Object)null && RollRollUiResource.TryApplySprite(component, "Lobby/000-main-lobby-bottom-menu-background", (Type)1, preserveAspect: false))
		{
			((Graphic)component).color = Color.white;
		}
		Image val2 = CreateShopArtwork(((Component)val).transform, "NavIcon", ResolveOutgameNavIconPath(val, active: false), new Vector2(0f, 22f), new Vector2(48f, 48f), Color.white, new Vector2(0.5f, 0.5f));
		((Graphic)val2).raycastTarget = false;
		Text childText = GetChildText(((Component)val).transform, "Label");
		if ((Object)(object)childText != (Object)null)
		{
			((Object)((Component)childText).gameObject).name = "NavLabel";
			childText.text = RuntimeKoreanTextUtility.Clean("NavLabel", label);
			((Graphic)childText).color = new Color(0.2f, 0.25f, 0.42f);
			childText.fontSize = 20;
			childText.resizeTextForBestFit = true;
			childText.resizeTextMinSize = 16;
			childText.resizeTextMaxSize = 20;
			childText.alignment = (TextAnchor)4;
			((Graphic)childText).rectTransform.anchorMin = new Vector2(0f, 0f);
			((Graphic)childText).rectTransform.anchorMax = new Vector2(1f, 0f);
			((Graphic)childText).rectTransform.pivot = new Vector2(0.5f, 0f);
			((Graphic)childText).rectTransform.anchoredPosition = new Vector2(0f, 10f);
			((Graphic)childText).rectTransform.sizeDelta = new Vector2(-18f, 36f);
			AddReadableOutline(childText);
			((Component)childText).transform.SetAsLastSibling();
		}
		outgameNavBasePositions[val] = position;
		SetOutgameNavButtonState(val, active: false);
		return val;
	}

	private void BuildOutgamePlaceholderOverlay(Transform parent)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		outgamePlaceholderOverlay = CreateOverlayRoot(parent, "OutgamePlaceholderOverlay", Color.clear);
		((Graphic)outgamePlaceholderOverlay.GetComponent<Image>()).raycastTarget = false;
		Image val = CreatePanel(outgamePlaceholderOverlay.transform, "PlaceholderModal", new Vector2(0f, 76f), new Vector2(0f, -152f), new Color(0.1f, 0.16f, 0.42f, 1f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: false, shadow: false);
		val.sprite = null;
		val.type = (Type)0;
		val.preserveAspect = false;
		CreatePanel(((Component)val).transform, "PlaceholderGlow", new Vector2(0f, -40f), new Vector2(600f, 88f), new Color(0.36f, 0.78f, 1f, 0.18f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreateText(((Component)val).transform, "PlaceholderTitle", "준비중", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(520f, 56f), 38, (TextAnchor)4, bold: true);
		CreateText(((Component)val).transform, "PlaceholderBody", "아웃게임 컨텐츠 화면 자리입니다.", new Color(0.86f, 0.92f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(610f, 170f), 24, (TextAnchor)4, bold: false);
	}

	private void BuildSeasonRankingOverlay(Transform parent)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_0394: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03df: Unknown result type (might be due to invalid IL or missing references)
		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0428: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		//IL_046d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0494: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_050a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0519: Unknown result type (might be due to invalid IL or missing references)
		//IL_0528: Unknown result type (might be due to invalid IL or missing references)
		//IL_0557: Unknown result type (might be due to invalid IL or missing references)
		//IL_0566: Unknown result type (might be due to invalid IL or missing references)
		//IL_0575: Unknown result type (might be due to invalid IL or missing references)
		//IL_0584: Unknown result type (might be due to invalid IL or missing references)
		//IL_0593: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_060e: Unknown result type (might be due to invalid IL or missing references)
		//IL_061d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0680: Unknown result type (might be due to invalid IL or missing references)
		//IL_068f: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_070e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0713: Unknown result type (might be due to invalid IL or missing references)
		//IL_0722: Unknown result type (might be due to invalid IL or missing references)
		//IL_0740: Unknown result type (might be due to invalid IL or missing references)
		//IL_074f: Unknown result type (might be due to invalid IL or missing references)
		//IL_075e: Unknown result type (might be due to invalid IL or missing references)
		//IL_076d: Unknown result type (might be due to invalid IL or missing references)
		//IL_077c: Unknown result type (might be due to invalid IL or missing references)
		//IL_078b: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_07de: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_07fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_080b: Unknown result type (might be due to invalid IL or missing references)
		seasonRankingOverlay = CreateOverlayRoot(parent, "SeasonRankingOverlay", Color.clear);
		((Graphic)seasonRankingOverlay.GetComponent<Image>()).raycastTarget = false;
		Image val = CreatePanel(seasonRankingOverlay.transform, "SeasonRankingModal", new Vector2(0f, 76f), new Vector2(0f, -152f), Color32.op_Implicit(new Color32((byte)40, (byte)4, (byte)4, byte.MaxValue)), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: false, shadow: false);
		val.sprite = null;
		val.type = (Type)0;
		val.preserveAspect = false;
		Image val2 = CreateShopArtwork(((Component)val).transform, "RankingTopVisual", "DiceTower/top-rank-visual-img", new Vector2(0f, 5f), new Vector2(820f, 430f), new Color(1f, 1f, 1f, 0.3f), new Vector2(0.5f, 1f));
		((Graphic)val2).rectTransform.anchoredPosition3D = new Vector3(0f, 5f, 0f);
		((Transform)((Graphic)val2).rectTransform).localScale = new Vector3(1.5f, 1.5f, 1f);
		Image val3 = CreatePanel(((Component)val).transform, "RankingHeader", new Vector2(0f, -190f), new Vector2(858f, 108f), new Color(0.37f, 0.2f, 0.76f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		CreateShopArtwork(((Component)val3).transform, "RankingHeaderTrophy", "Icons/icon-main-menu-trophy-activated", new Vector2(34f, -22f), new Vector2(66f, 66f), Color.white, new Vector2(0f, 1f));
		CreateText(((Component)val3).transform, "RankingTitle", "시즌 랭킹", new Color(1f, 0.94f, 0.72f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(112f, -18f), new Vector2(300f, 48f), 37, (TextAnchor)3, bold: true);
		rankingSeasonText = CreateText(((Component)val3).transform, "RankingSeasonText", "SEASON 1 · 주간 보스 리그", new Color(0.86f, 0.82f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(114f, -65f), new Vector2(420f, 28f), 18, (TextAnchor)3, bold: true);
		Image val4 = CreatePanel(((Component)val).transform, "RankingPodium", new Vector2(0f, -360f), new Vector2(850f, 414f), new Color(0.11f, 0.07f, 0.33f, 0.72f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreateText(((Component)val4).transform, "PodiumHint", "이번 주 최고의 수호자", new Color(0.94f, 0.86f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(420f, 30f), 21, (TextAnchor)4, bold: true);
		BuildRankingTopCard(((Component)val4).transform, 1, 0f, -58f, new Vector2(300f, 326f), "DiceTower/rank-gold-bg", "RankedGrade/ranked-gold", new Color(1f, 0.8f, 0.22f));
		BuildRankingTopCard(((Component)val4).transform, 2, -274f, -94f, new Vector2(250f, 280f), "DiceTower/rank-silver-bg", "RankedGrade/ranked-silver", new Color(0.78f, 0.88f, 1f));
		BuildRankingTopCard(((Component)val4).transform, 3, 274f, -94f, new Vector2(250f, 280f), "DiceTower/rank-bronze-bg", "RankedGrade/ranked-bronze", new Color(1f, 0.63f, 0.36f));
		Image val5 = CreatePanel(((Component)val).transform, "RankingList", new Vector2(0f, -808f), new Vector2(850f, 894f), new Color(0.09f, 0.07f, 0.3f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		CreateText(((Component)val5).transform, "RankingListTitle", "전체 랭킹", new Color(0.98f, 0.9f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -20f), new Vector2(240f, 34f), 25, (TextAnchor)3, bold: true);
		CreateText(((Component)val5).transform, "RankingListGuide", "순위     플레이어                                  점수", new Color(0.66f, 0.72f, 0.94f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(-66f, 28f), 16, (TextAnchor)4, bold: true);
		for (int i = 0; i < rankingRowPanels.Length; i++)
		{
			BuildRankingRow(((Component)val5).transform, i, -92f - (float)i * 86f);
		}
		Image val6 = CreatePanel(((Component)val).transform, "RankingPlayerFooter", new Vector2(0f, -1756f), new Vector2(850f, 126f), new Color(0.17f, 0.42f, 0.66f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		CreateShopArtwork(((Component)val6).transform, "RankingPlayerTrophy", "Icons/icon-trophy", new Vector2(28f, -24f), new Vector2(72f, 72f), Color.white, new Vector2(0f, 1f));
		rankingPlayerSummaryText = CreateText(((Component)val6).transform, "RankingPlayerSummary", "내 순위 -위  |  레드X  0점", Color.white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(112f, -20f), new Vector2(-140f, 42f), 26, (TextAnchor)3, bold: true);
		rankingPlayerProgressText = CreateText(((Component)val6).transform, "RankingPlayerProgress", "최고 런 0점 · 보스 처치 0회", new Color(0.72f, 0.94f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(112f, -68f), new Vector2(-140f, 32f), 18, (TextAnchor)3, bold: true);
		seasonRankingOverlay.SetActive(false);
	}

	private void BuildRankingTopCard(Transform parent, int rank, float x, float y, Vector2 size, string backgroundPath, string badgePath, Color accent)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		int num = rank - 1;
		Image val = CreatePanel(parent, "RankingTopCard_" + num, new Vector2(x, y), size, new Color(0.18f, 0.14f, 0.48f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		ApplyRankingPanelSprite(val, backgroundPath);
		rankingTopCardPanels[num] = val;
		float num2 = ((rank == 1) ? 116f : 96f);
		CreateShopArtwork(((Component)val).transform, "TopBadge", badgePath, new Vector2(0f, -20f), new Vector2(num2, num2), Color.white, new Vector2(0.5f, 1f));
		CreateText(((Component)val).transform, "TopRank", rank.ToString(), Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(72f, 46f), (rank == 1) ? 35 : 29, (TextAnchor)4, bold: true);
		rankingTopNameTexts[num] = CreateText(((Component)val).transform, "TopName", "플레이어", Color.white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, (rank == 1) ? (-150f) : (-126f)), new Vector2(-24f, 42f), (rank == 1) ? 25 : 22, (TextAnchor)4, bold: true);
		ApplyTopRankingNameStyle(rankingTopNameTexts[num]);
		Image val2 = CreatePanel(((Component)val).transform, "TopScorePlate", new Vector2(0f, (rank == 1) ? (-208f) : (-178f)), new Vector2(size.x - 34f, 58f), new Color(0.08f, 0.06f, 0.25f, 0.88f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreateShopArtwork(((Component)val2).transform, "TopTrophy", "Icons/icon-trophy", new Vector2(18f, -10f), new Vector2(40f, 40f), Color.white, new Vector2(0f, 1f));
		rankingTopScoreTexts[num] = CreateText(((Component)val2).transform, "TopScore", "0", accent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(66f, -8f), new Vector2(-78f, 40f), (rank == 1) ? 25 : 22, (TextAnchor)5, bold: true);
	}

	private void BuildRankingRow(Transform parent, int index, float y)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		Image val = CreatePanel(parent, "RankingRow_" + index, new Vector2(0f, y), new Vector2(790f, 76f), Color32.op_Implicit(new Color32((byte)133, (byte)131, (byte)164, byte.MaxValue)), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		val.type = (Type)1;
		rankingRowPanels[index] = val;
		CreateShopArtwork(((Component)val).transform, "RankBadge", "RankedGrade/ranked-bronze-small", new Vector2(18f, -10f), new Vector2(56f, 56f), Color.white, new Vector2(0f, 1f));
		rankingRowRankTexts[index] = CreateText(((Component)val).transform, "Rank", (index + 4).ToString(), Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25f, -17f), new Vector2(42f, 40f), 19, (TextAnchor)4, bold: true);
		rankingRowNameTexts[index] = CreateText(((Component)val).transform, "PlayerName", "플레이어", Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(106f, -17f), new Vector2(350f, 40f), 22, (TextAnchor)3, bold: true);
		ApplyRankingRowPlayerNameStyle(rankingRowNameTexts[index]);
		Image val2 = CreatePanel(((Component)val).transform, "ScorePlate", new Vector2(-18f, -10f), new Vector2(242f, 56f), new Color(0.08f, 0.07f, 0.25f, 0.78f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), rounded: true, shadow: false);
		CreateShopArtwork(((Component)val2).transform, "Trophy", "Icons/icon-trophy", new Vector2(12f, -9f), new Vector2(38f, 38f), Color.white, new Vector2(0f, 1f));
		rankingRowScoreTexts[index] = CreateText(((Component)val2).transform, "Score", "0", new Color(1f, 0.86f, 0.36f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(58f, -7f), new Vector2(-70f, 40f), 22, (TextAnchor)5, bold: true);
	}

	private void ApplyRankingPanelSprite(Image image, string resourcePath)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		Sprite val = RollRollUiResource.LoadSprite(resourcePath);
		if (!((Object)(object)image == (Object)null) && !((Object)(object)val == (Object)null))
		{
			image.sprite = val;
			image.type = (Type)0;
			((Graphic)image).color = Color.white;
		}
	}

	private void BuildExitConfirmOverlay(Transform parent)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Expected O, but got Unknown
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Expected O, but got Unknown
		exitConfirmOverlay = CreateOverlayRoot(parent, "ExitConfirmOverlay", new Color(0.02f, 0.03f, 0.09f, 0.76f));
		Image val = CreatePanel(exitConfirmOverlay.transform, "ExitConfirmModal", new Vector2(0f, 34f), new Vector2(720f, 420f), new Color(0.08f, 0.13f, 0.32f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
		CreatePanel(((Component)val).transform, "ExitConfirmGlow", new Vector2(0f, -44f), new Vector2(560f, 76f), new Color(1f, 0.44f, 0.32f, 0.16f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreateText(((Component)val).transform, "ExitConfirmTitle", "나가시겠습니까?", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(560f, 58f), 38, (TextAnchor)4, bold: true);
		CreateText(((Component)val).transform, "ExitConfirmBody", "현재 도전을 종료하고 로비로 이동합니다.", new Color(0.86f, 0.92f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(600f, 96f), 25, (TextAnchor)4, bold: false);
		exitConfirmLeaveButton = CreateButton(((Component)val).transform, "ExitConfirmLeaveButton", "나가기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 70f), new Vector2(260f, 74f), new Color(0.92f, 0.28f, 0.22f, 1f), new UnityAction(ConfirmExitToOutgame), 27);
		exitConfirmContinueButton = CreateButton(((Component)val).transform, "ExitConfirmContinueButton", "계속하기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(18f, 70f), new Vector2(260f, 74f), new Color(0.3f, 0.62f, 1f, 1f), new UnityAction(HideExitConfirm), 27);
	}

	private void BuildShopOverlay(Transform parent)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0402: Unknown result type (might be due to invalid IL or missing references)
		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
		//IL_0497: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0511: Unknown result type (might be due to invalid IL or missing references)
		//IL_0520: Unknown result type (might be due to invalid IL or missing references)
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_053e: Unknown result type (might be due to invalid IL or missing references)
		//IL_054d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0566: Unknown result type (might be due to invalid IL or missing references)
		//IL_0572: Unknown result type (might be due to invalid IL or missing references)
		//IL_057e: Expected O, but got Unknown
		//IL_0599: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05df: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0616: Unknown result type (might be due to invalid IL or missing references)
		//IL_0625: Unknown result type (might be due to invalid IL or missing references)
		//IL_062a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0639: Unknown result type (might be due to invalid IL or missing references)
		//IL_0664: Unknown result type (might be due to invalid IL or missing references)
		//IL_0673: Unknown result type (might be due to invalid IL or missing references)
		//IL_0682: Unknown result type (might be due to invalid IL or missing references)
		//IL_0691: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_06af: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_070b: Unknown result type (might be due to invalid IL or missing references)
		//IL_071a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0729: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_07fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0809: Unknown result type (might be due to invalid IL or missing references)
		//IL_082c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0839: Unknown result type (might be due to invalid IL or missing references)
		//IL_0845: Expected O, but got Unknown
		//IL_08e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_090d: Unknown result type (might be due to invalid IL or missing references)
		//IL_091c: Unknown result type (might be due to invalid IL or missing references)
		//IL_092b: Unknown result type (might be due to invalid IL or missing references)
		//IL_093a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0964: Unknown result type (might be due to invalid IL or missing references)
		//IL_0973: Unknown result type (might be due to invalid IL or missing references)
		//IL_0978: Unknown result type (might be due to invalid IL or missing references)
		//IL_0987: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_09fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a2f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a3e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a4d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a5c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a6b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a7a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ab3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0acc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0adb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0af9: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_088c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0871: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b97: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ba6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bb5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bdd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cd9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d01: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d10: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d1f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d58: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d67: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d6c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d7b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0da7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0db6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dc5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dd4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0de3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0df2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e18: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e27: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e40: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e4f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e5e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e6d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e97: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ea6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eb5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ec4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ed3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ef8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f04: Expected O, but got Unknown
		//IL_0f26: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f35: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f44: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f53: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f62: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f7b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f87: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f93: Expected O, but got Unknown
		//IL_0fb5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fc4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fd3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fe2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ff1: Unknown result type (might be due to invalid IL or missing references)
		//IL_100a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1016: Unknown result type (might be due to invalid IL or missing references)
		//IL_1022: Expected O, but got Unknown
		//IL_1044: Unknown result type (might be due to invalid IL or missing references)
		//IL_1053: Unknown result type (might be due to invalid IL or missing references)
		//IL_1062: Unknown result type (might be due to invalid IL or missing references)
		//IL_1071: Unknown result type (might be due to invalid IL or missing references)
		//IL_1080: Unknown result type (might be due to invalid IL or missing references)
		//IL_1099: Unknown result type (might be due to invalid IL or missing references)
		//IL_10a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_10b1: Expected O, but got Unknown
		//IL_10d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_10e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_10f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1100: Unknown result type (might be due to invalid IL or missing references)
		//IL_110f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1128: Unknown result type (might be due to invalid IL or missing references)
		//IL_1134: Unknown result type (might be due to invalid IL or missing references)
		//IL_1140: Expected O, but got Unknown
		//IL_1162: Unknown result type (might be due to invalid IL or missing references)
		//IL_1171: Unknown result type (might be due to invalid IL or missing references)
		//IL_1180: Unknown result type (might be due to invalid IL or missing references)
		//IL_118f: Unknown result type (might be due to invalid IL or missing references)
		//IL_119e: Unknown result type (might be due to invalid IL or missing references)
		//IL_11b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_11c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_11cf: Expected O, but got Unknown
		//IL_11f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_1220: Unknown result type (might be due to invalid IL or missing references)
		//IL_124c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1278: Unknown result type (might be due to invalid IL or missing references)
		//IL_12a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_12b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_12c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_12d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_12e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_12f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1326: Unknown result type (might be due to invalid IL or missing references)
		//IL_1335: Unknown result type (might be due to invalid IL or missing references)
		//IL_1344: Unknown result type (might be due to invalid IL or missing references)
		//IL_1353: Unknown result type (might be due to invalid IL or missing references)
		//IL_1362: Unknown result type (might be due to invalid IL or missing references)
		//IL_1371: Unknown result type (might be due to invalid IL or missing references)
		//IL_139a: Unknown result type (might be due to invalid IL or missing references)
		//IL_13a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_13c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_13d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_13e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_13ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_141e: Unknown result type (might be due to invalid IL or missing references)
		//IL_142d: Unknown result type (might be due to invalid IL or missing references)
		//IL_143c: Unknown result type (might be due to invalid IL or missing references)
		//IL_144b: Unknown result type (might be due to invalid IL or missing references)
		//IL_145a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1469: Unknown result type (might be due to invalid IL or missing references)
		//IL_149a: Unknown result type (might be due to invalid IL or missing references)
		//IL_14a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_14b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_14c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_14d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_14e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c1f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c04: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c2c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c38: Expected O, but got Unknown
		//IL_0c9b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c80: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c65: Unknown result type (might be due to invalid IL or missing references)
		shopOverlay = CreateOverlayRoot(parent, "OutgameShopOverlay", Color.clear);
		((Graphic)shopOverlay.GetComponent<Image>()).raycastTarget = false;
		Image val = CreatePanel(shopOverlay.transform, "ShopModal", new Vector2(0f, 76f), new Vector2(0f, -152f), Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: false, shadow: false);
		RollRollUiResource.TryApplySprite(val, "Common/background", (Type)0, preserveAspect: false);
		((Graphic)val).color = Color.white;
		CreatePanel(((Component)val).transform, "ShopHeader", new Vector2(0f, -95f), new Vector2(850f, 104f), new Color(0.98f, 0.78f, 0.18f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreatePanel(((Component)val).transform, "ShopGoldCurrencyChip", new Vector2(-128f, -129f), new Vector2(260f, 58f), new Color(0.08f, 0.12f, 0.3f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreatePanel(((Component)val).transform, "ShopDiamondCurrencyChip", new Vector2(140f, -129f), new Vector2(250f, 58f), new Color(0.12f, 0.1f, 0.34f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreateShopArtwork(((Component)val).transform, "HeaderGoldIcon", "Icons/goods_icon_gold", new Vector2(-222f, -137f), new Vector2(42f, 42f), new Color(1f, 1f, 1f, 0.98f), new Vector2(0.5f, 1f));
		CreateShopArtwork(((Component)val).transform, "HeaderDiamondIcon", "Icons/goods_icon_ruby", new Vector2(48f, -137f), new Vector2(42f, 42f), new Color(1f, 1f, 1f, 0.98f), new Vector2(0.5f, 1f));
		CreateText(((Component)val).transform, "ShopTitle", "상점", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-338f, -120f), new Vector2(140f, 48f), 38, (TextAnchor)4, bold: true);
		shopGoldText = CreateText(((Component)val).transform, "ShopGoldText", "GOLD 0", new Color(1f, 0.84f, 0.28f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-104f, -137f), new Vector2(190f, 42f), 20, (TextAnchor)5, bold: true);
		shopDiamondText = CreateText(((Component)val).transform, "DiamondText", "DIA 0", new Color(0.46f, 0.94f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(162f, -137f), new Vector2(180f, 42f), 20, (TextAnchor)5, bold: true);
		shopGoldText.resizeTextForBestFit = true;
		shopGoldText.resizeTextMinSize = 16;
		shopGoldText.resizeTextMaxSize = 20;
		shopDiamondText.resizeTextForBestFit = true;
		shopDiamondText.resizeTextMinSize = 16;
		shopDiamondText.resizeTextMaxSize = 20;
		shopModeText = CreateText(((Component)val).transform, "ShopModeText", "SERVICE", new Color(0.7f, 0.84f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -293f), new Vector2(340f, 32f), 17, (TextAnchor)3, bold: true);
		shopTestDiamondButton = CreateButton(((Component)val).transform, "TestCurrencyButton", "테스트 재화 충전", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-48f, -283f), new Vector2(224f, 52f), new Color(0.19f, 0.8f, 0.72f, 1f), new UnityAction(RechargeTestDiamonds), 18);
		Image val2 = CreatePanel(((Component)val).transform, "CashBundleSection", new Vector2(0f, -363f), new Vector2(820f, 270f), new Color(0.11f, 0.17f, 0.39f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		CreateShopArtwork(((Component)val2).transform, "CashSectionIcon", "GradeAndGoodsIcons/goods_icon_reward_box", new Vector2(26f, -20f), new Vector2(56f, 56f), Color.white, new Vector2(0f, 1f));
		CreateText(((Component)val2).transform, "CashBundleTitle", "오늘의 꾸러미 · 현금 상품", new Color(1f, 0.88f, 0.42f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, -16f), new Vector2(500f, 36f), 25, (TextAnchor)3, bold: true);
		CreatePanel(((Component)val2).transform, "CashSectionDivider", new Vector2(0f, -54f), new Vector2(760f, 3f), new Color(1f, 0.78f, 0.28f, 0.48f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		string[] array = new string[3] { "골드 주머니\n10,000 GOLD\n₩3,300", "다이아 주머니\n1,200 DIA\n₩6,600", "성장 꾸러미\n20,000G + 2,000 DIA\n₩9,900" };
		string[] array2 = new string[3] { "GradeAndGoodsIcons/goods_icon_gold_group", "GradeAndGoodsIcons/goods_icon_ruby_group", "GradeAndGoodsIcons/goods_icon_reward_box" };
		for (int i = 0; i < shopCashBundleButtons.Length; i++)
		{
			int index = i;
			shopCashBundleButtons[i] = CreateButton(((Component)val2).transform, "CashBundleCard_" + i, array[i], new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-260f + (float)i * 260f, -70f), new Vector2(220f, 180f), new Color(0.66f, 0.24f + (float)i * 0.08f, 0.76f, 0.98f), (UnityAction)delegate
			{
				ShowCashBundlePurchaseConfirm(index);
			}, 20);
			DecorateShopProductCard(shopCashBundleButtons[i], array2[i], (Color)(i switch
			{
				1 => new Color(0.54f, 0.9f, 1f, 1f), 
				0 => new Color(1f, 0.72f, 0.18f, 1f), 
				_ => new Color(0.94f, 0.55f, 1f, 1f), 
			}), compact: false);
		}
		Image val3 = CreatePanel(((Component)val).transform, "DailyShopSection", new Vector2(0f, -657f), new Vector2(820f, 286f), new Color(0.11f, 0.17f, 0.39f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		CreateShopArtwork(((Component)val3).transform, "DailySectionIcon", "GradeAndGoodsIcons/goods_icon_gold", new Vector2(26f, -20f), new Vector2(56f, 56f), Color.white, new Vector2(0f, 1f));
		CreateText(((Component)val3).transform, "DailyShopTitle", "일일 상점", new Color(0.56f, 0.94f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, -18f), new Vector2(240f, 36f), 27, (TextAnchor)3, bold: true);
		shopDailyResetText = CreateText(((Component)val3).transform, "DailyResetText", "갱신까지 00:00", new Color(0.78f, 0.84f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -20f), new Vector2(300f, 32f), 17, (TextAnchor)5, bold: false);
		CreatePanel(((Component)val3).transform, "DailySectionDivider", new Vector2(0f, -58f), new Vector2(760f, 3f), new Color(0.34f, 0.88f, 1f, 0.46f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		string[] array3 = new string[3] { "일일 무료 선물\n+500 GOLD\n무료", "영웅 카드 x5\n1,200 GOLD\n일일 1회", "프리미엄 카드 x3\n250 DIA\n일일 1회" };
		string[] array4 = new string[3] { "GradeAndGoodsIcons/goods_icon_gold_group", "GradeAndGoodsIcons/goods_icon_reward_box", "GradeAndGoodsIcons/goods_icon_ruby_group" };
		for (int num = 0; num < shopDailyOfferButtons.Length; num++)
		{
			int index2 = num;
			shopDailyOfferButtons[num] = CreateButton(((Component)val3).transform, "DailyOfferCard_" + num, array3[num], new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-260f + (float)num * 260f, -78f), new Vector2(220f, 180f), (num == 0) ? new Color(0.3f, 0.7f, 0.32f, 1f) : new Color(0.25f, 0.38f + (float)num * 0.08f, 0.78f, 1f), (UnityAction)delegate
			{
				ShowDailyOfferPurchaseConfirm(index2);
			}, 20);
			DecorateShopProductCard(shopDailyOfferButtons[num], array4[num], (Color)(num switch
			{
				1 => new Color(0.42f, 0.82f, 1f, 1f), 
				0 => new Color(0.54f, 1f, 0.52f, 1f), 
				_ => new Color(0.78f, 0.55f, 1f, 1f), 
			}), compact: false);
		}
		Image val4 = CreatePanel(((Component)val).transform, "ChestShopSection", new Vector2(0f, -967f), new Vector2(820f, 438f), new Color(0.11f, 0.17f, 0.39f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		CreateShopArtwork(((Component)val4).transform, "ChestSectionIcon", "Lobby/top-panel-icon-chest-empty", new Vector2(26f, -20f), new Vector2(56f, 56f), Color.white, new Vector2(0f, 1f));
		CreateText(((Component)val4).transform, "ChestShopTitle", "영웅 카드 상자 · 다이아 상품", new Color(1f, 0.82f, 0.3f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, -16f), new Vector2(520f, 38f), 27, (TextAnchor)3, bold: true);
		CreatePanel(((Component)val4).transform, "ChestSectionDivider", new Vector2(0f, -54f), new Vector2(760f, 3f), new Color(1f, 0.76f, 0.24f, 0.46f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		shopEarnedDrawButton = CreateButton(((Component)val4).transform, "EarnedDrawButton", "무료 상자 열기", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -62f), new Vector2(360f, 58f), new Color(0.24f, 0.72f, 0.4f, 1f), new UnityAction(ShowEarnedChestConfirm), 20);
		shopWishlistButton = CreateButton(((Component)val4).transform, "WishlistButton", "위시 영웅 설정", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(12f, -62f), new Vector2(360f, 58f), new Color(0.5f, 0.34f, 0.78f, 1f), new UnityAction(CycleWishlist), 19);
		shopSingleDrawButton = CreateButton(((Component)val4).transform, "FiveDrawCard", "5개", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-288f, -150f), new Vector2(182f, 124f), new Color(0.2f, 0.66f, 0.78f, 1f), (UnityAction)delegate
		{
			ShowPremiumChestPurchaseConfirm(5);
		}, 20);
		shopTenDrawButton = CreateButton(((Component)val4).transform, "TwentyDrawCard", "20개", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-96f, -150f), new Vector2(182f, 124f), new Color(0.34f, 0.54f, 0.88f, 1f), (UnityAction)delegate
		{
			ShowPremiumChestPurchaseConfirm(20);
		}, 20);
		shopFiftyDrawButton = CreateButton(((Component)val4).transform, "FiftyDrawCard", "50개", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(96f, -150f), new Vector2(182f, 124f), new Color(0.58f, 0.38f, 0.9f, 1f), (UnityAction)delegate
		{
			ShowPremiumChestPurchaseConfirm(50);
		}, 20);
		shopHundredDrawButton = CreateButton(((Component)val4).transform, "HundredDrawCard", "100개", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(288f, -150f), new Vector2(182f, 124f), new Color(0.92f, 0.48f, 0.24f, 1f), (UnityAction)delegate
		{
			ShowPremiumChestPurchaseConfirm(100);
		}, 20);
		DecorateShopProductCard(shopSingleDrawButton, "GradeAndGoodsIcons/goods_icon_reward_box", new Color(0.38f, 0.9f, 1f, 1f), compact: true);
		DecorateShopProductCard(shopTenDrawButton, "GradeAndGoodsIcons/goods_icon_reward_box", new Color(0.45f, 0.68f, 1f, 1f), compact: true);
		DecorateShopProductCard(shopFiftyDrawButton, "GradeAndGoodsIcons/goods_icon_reward_box", new Color(0.75f, 0.5f, 1f, 1f), compact: true);
		DecorateShopProductCard(shopHundredDrawButton, "GradeAndGoodsIcons/goods_icon_reward_box", new Color(1f, 0.58f, 0.28f, 1f), compact: true);
		shopRatesText = CreateText(((Component)val4).transform, "RatesText", string.Empty, new Color(0.83f, 0.91f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(-44f, 74f), 16, (TextAnchor)4, bold: false);
		shopCollectionText = CreateText(((Component)val4).transform, "CollectionText", string.Empty, new Color(1f, 0.92f, 0.5f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(-44f, 38f), 20, (TextAnchor)4, bold: true);
		Image val5 = CreatePanel(((Component)val).transform, "DrawResults", new Vector2(0f, -1437f), new Vector2(820f, 330f), new Color(0.07f, 0.1f, 0.26f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		CreateText(((Component)val5).transform, "ResultTitle", "최근 구매 상태", new Color(0.45f, 0.96f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(360f, 34f), 24, (TextAnchor)4, bold: true);
		shopResultText = CreateText(((Component)val5).transform, "ResultBody", "구매하면 팝업으로 획득 결과가 표시됩니다.", new Color(0.91f, 0.94f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(-52f, 238f), 18, (TextAnchor)0, bold: false);
		BuildShopBottomNavigation(shopOverlay.transform);
		BuildShopPurchaseConfirm(shopOverlay.transform);
		BuildShopPurchaseResultPopup(shopOverlay.transform);
		RefreshShop();
	}

	private void BuildShopBottomNavigation(Transform parent)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Expected O, but got Unknown
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Expected O, but got Unknown
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Expected O, but got Unknown
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Expected O, but got Unknown
		Image val = CreatePanel(parent, "ShopBottomNavDock", new Vector2(0f, 0f), new Vector2(0f, 152f), new Color(0.88f, 0.93f, 1f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), rounded: false, shadow: true);
		CreatePanel(((Component)val).transform, "ShopDockTopLine", new Vector2(0f, 150f), new Vector2(0f, 4f), new Color(1f, 1f, 1f, 0.7f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), rounded: false, shadow: false);
		RectTransform parent2 = CreateOutgameNavTabsLayer(parent, "ShopBottomNavTabs");
		Button button = CreateOutgameNavButton((Transform)(object)parent2, "ShopNavShop", "상점", "SHOP", new Vector2(-432f, 140f), new Color(1f, 0.58f, 0.76f), null);
		Button button2 = CreateOutgameNavButton((Transform)(object)parent2, "ShopNavInventory", "인벤", "CARD", new Vector2(-216f, 140f), new Color(0.98f, 0.36f, 0.36f), (UnityAction)delegate
		{
			HideShop();
			ShowCollectionTab();
		});
		Button button3 = CreateOutgameNavButton((Transform)(object)parent2, "ShopNavLobby", "로비", "HOME", new Vector2(0f, 140f), new Color(0.3f, 0.62f, 1f), (UnityAction)delegate
		{
			HideShop();
			ShowLobby();
		});
		Button button4 = CreateOutgameNavButton((Transform)(object)parent2, "ShopNavYahtzee", "얏찌", "DICE", new Vector2(216f, 140f), new Color(1f, 0.62f, 0.22f), (UnityAction)delegate
		{
			ShowOutgamePlaceholder("얏찌", "얏찌에서 무료 상자 게이지와 상자 키를 획득하는 구조입니다.");
		});
		Button button5 = CreateOutgameNavButton((Transform)(object)parent2, "ShopNavRanking", "랭킹", "CUP", new Vector2(432f, 140f), new Color(0.74f, 0.52f, 1f), new UnityAction(ShowSeasonRanking));
		SetOutgameNavButtonState(button, active: true);
		SetOutgameNavButtonState(button2, active: false);
		SetOutgameNavButtonState(button3, active: false);
		SetOutgameNavButtonState(button4, active: false);
		SetOutgameNavButtonState(button5, active: false);
	}

	private void BuildShopPurchaseConfirm(Transform parent)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Expected O, but got Unknown
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Expected O, but got Unknown
		shopPurchaseConfirmOverlay = CreateOverlayRoot(parent, "ShopPurchaseConfirmOverlay", new Color(0.01f, 0.02f, 0.08f, 0.82f));
		Image val = CreatePanel(shopPurchaseConfirmOverlay.transform, "ShopPurchaseConfirmModal", new Vector2(0f, 36f), new Vector2(680f, 500f), new Color(0.1f, 0.27f, 0.62f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
		CreatePanel(((Component)val).transform, "ShopPurchaseConfirmHeader", new Vector2(0f, -18f), new Vector2(620f, 86f), new Color(0.98f, 0.78f, 0.18f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		shopPurchaseConfirmTitleText = CreateText(((Component)val).transform, "ShopPurchaseConfirmTitle", "구매 확인", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(500f, 44f), 31, (TextAnchor)4, bold: true);
		CreateShopArtwork(((Component)val).transform, "ShopPurchaseConfirmIcon", "GradeAndGoodsIcons/goods_icon_reward_box", new Vector2(0f, -126f), new Vector2(108f, 108f), Color.white, new Vector2(0.5f, 1f));
		shopPurchaseConfirmBodyText = CreateText(((Component)val).transform, "ShopPurchaseConfirmBody", string.Empty, new Color(0.94f, 0.96f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -244f), new Vector2(570f, 116f), 23, (TextAnchor)4, bold: true);
		CreateButton(((Component)val).transform, "ShopPurchaseConfirmCancelButton", "취소", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 40f), new Vector2(250f, 74f), new Color(0.28f, 0.42f, 0.72f, 1f), new UnityAction(HideShopPurchaseConfirm), 25);
		shopPurchaseConfirmButton = CreateButton(((Component)val).transform, "ShopPurchaseConfirmButton", "구매", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(14f, 40f), new Vector2(250f, 74f), new Color(0.95f, 0.52f, 0.16f, 1f), new UnityAction(ConfirmPendingShopPurchase), 25);
		shopPurchaseConfirmOverlay.SetActive(false);
	}

	private void BuildShopPurchaseResultPopup(Transform parent)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_038c: Unknown result type (might be due to invalid IL or missing references)
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Unknown result type (might be due to invalid IL or missing references)
		//IL_0417: Unknown result type (might be due to invalid IL or missing references)
		//IL_0426: Unknown result type (might be due to invalid IL or missing references)
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_0444: Unknown result type (might be due to invalid IL or missing references)
		//IL_045d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0469: Unknown result type (might be due to invalid IL or missing references)
		//IL_0475: Expected O, but got Unknown
		//IL_04b3: Unknown result type (might be due to invalid IL or missing references)
		shopPurchaseResultOverlay = CreateOverlayRoot(parent, "ShopPurchaseResultOverlay", new Color(0.01f, 0.02f, 0.08f, 0.58f));
		shopPurchaseResultCanvasGroup = shopPurchaseResultOverlay.AddComponent<CanvasGroup>();
		Image val = CreatePanel(shopPurchaseResultOverlay.transform, "ShopPurchaseResultModal", new Vector2(0f, 38f), new Vector2(720f, 560f), new Color(0.09f, 0.18f, 0.48f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
		shopPurchaseResultModalRect = ((Graphic)val).rectTransform;
		CreatePanel(((Component)val).transform, "ShopPurchaseResultHeader", new Vector2(0f, -20f), new Vector2(640f, 88f), new Color(0.98f, 0.78f, 0.18f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		shopPurchaseResultTitleText = CreateText(((Component)val).transform, "ShopPurchaseResultTitle", "구매 완료", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(540f, 48f), 34, (TextAnchor)4, bold: true);
		CreatePanel(((Component)val).transform, "ShopPurchaseResultIconGlow", new Vector2(0f, -138f), new Vector2(170f, 132f), new Color(0.6f, 0.42f, 1f, 0.34f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		shopPurchaseResultIconImage = CreateShopArtwork(((Component)val).transform, "ShopPurchaseResultIcon", "GradeAndGoodsIcons/goods_icon_reward_box", new Vector2(0f, -120f), new Vector2(118f, 118f), Color.white, new Vector2(0.5f, 1f));
		shopPurchaseResultBodyText = CreateText(((Component)val).transform, "ShopPurchaseResultBody", "영웅 카드 x5를 구매했습니다.", new Color(0.94f, 0.97f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -254f), new Vector2(610f, 152f), 25, (TextAnchor)4, bold: false);
		shopPurchaseResultBodyText.resizeTextForBestFit = true;
		shopPurchaseResultBodyText.resizeTextMinSize = 16;
		shopPurchaseResultBodyText.resizeTextMaxSize = 25;
		((Graphic)shopPurchaseResultBodyText).rectTransform.anchoredPosition = new Vector2(0f, -266f);
		((Graphic)shopPurchaseResultBodyText).rectTransform.sizeDelta = new Vector2(610f, 112f);
		shopPurchaseResultCurrencyText = CreateText(((Component)val).transform, "ShopPurchaseResultCurrency", string.Empty, new Color(1f, 0.91f, 0.42f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -350f), new Vector2(600f, 48f), 25, (TextAnchor)4, bold: true);
		((Component)shopPurchaseResultCurrencyText).gameObject.SetActive(false);
		CreateButton(((Component)val).transform, "ShopPurchaseResultCloseButton", "확인", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(270f, 74f), new Color(0.28f, 0.62f, 1f, 1f), new UnityAction(HideShopPurchaseResultPopup), 26);
		Transform val2 = ((Component)val).transform.Find("ShopPurchaseResultCloseButton");
		if ((Object)(object)val2 != (Object)null)
		{
			RectTransform component = ((Component)val2).GetComponent<RectTransform>();
			if ((Object)(object)component != (Object)null)
			{
				component.anchoredPosition = new Vector2(0f, 34f);
			}
		}
		shopPurchaseResultOverlay.SetActive(false);
	}

	private void BuildMatchmakingOverlay(Transform parent)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_036a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0379: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03aa: Expected O, but got Unknown
		matchmakingOverlay = CreateOverlayRoot(parent, "MatchmakingOverlay", new Color(0.02f, 0.04f, 0.12f, 0.72f));
		Image val = CreatePanel(matchmakingOverlay.transform, "MatchmakingModal", Vector2.zero, new Vector2(620f, 420f), new Color(0.16f, 0.2f, 0.44f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
		CreatePanel(((Component)val).transform, "PulseA", new Vector2(0f, -132f), new Vector2(156f, 82f), new Color(0.2f, 1f, 0.92f, 0.14f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreatePanel(((Component)val).transform, "PulseB", new Vector2(0f, -132f), new Vector2(210f, 118f), new Color(1f, 0.32f, 0.64f, 0.08f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreateText(((Component)val).transform, "MatchTitle", "전투 준비 중", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(560f, 42f), 28, (TextAnchor)4, bold: true);
		queueTimerText = CreateText(((Component)val).transform, "QueueTimer", "00.00", new Color(0.28f, 1f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(220f, 68f), 46, (TextAnchor)4, bold: true);
		queueStatusText = CreateText(((Component)val).transform, "QueueStatus", "라운드 전장을 준비하는 중...", new Color(0.84f, 0.9f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -226f), new Vector2(540f, 58f), 18, (TextAnchor)4, bold: false);
		queueStatusText.resizeTextForBestFit = true;
		queueStatusText.resizeTextMinSize = 14;
		queueStatusText.resizeTextMaxSize = 18;
		matchmakingCancelButton = CreateButton(((Component)val).transform, "MatchmakingCancelButton", "닫기", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -326f), new Vector2(200f, 64f), new Color(0.95f, 0.45f, 0.3f, 1f), new UnityAction(CancelMatchmaking), 25);
	}

	private void BuildResultOverlay(Transform parent)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_034a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		//IL_039c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0419: Unknown result type (might be due to invalid IL or missing references)
		//IL_0428: Unknown result type (might be due to invalid IL or missing references)
		//IL_0437: Unknown result type (might be due to invalid IL or missing references)
		//IL_046b: Unknown result type (might be due to invalid IL or missing references)
		//IL_047a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0489: Unknown result type (might be due to invalid IL or missing references)
		//IL_0498: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04df: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0507: Unknown result type (might be due to invalid IL or missing references)
		//IL_0516: Unknown result type (might be due to invalid IL or missing references)
		//IL_0525: Unknown result type (might be due to invalid IL or missing references)
		//IL_0534: Unknown result type (might be due to invalid IL or missing references)
		//IL_055c: Unknown result type (might be due to invalid IL or missing references)
		//IL_056b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0570: Unknown result type (might be due to invalid IL or missing references)
		//IL_057f: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_062a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0639: Unknown result type (might be due to invalid IL or missing references)
		//IL_0648: Unknown result type (might be due to invalid IL or missing references)
		//IL_0657: Unknown result type (might be due to invalid IL or missing references)
		//IL_0666: Unknown result type (might be due to invalid IL or missing references)
		//IL_0675: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0776: Unknown result type (might be due to invalid IL or missing references)
		//IL_0785: Unknown result type (might be due to invalid IL or missing references)
		//IL_079e: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_07bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_07cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_07fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0809: Unknown result type (might be due to invalid IL or missing references)
		//IL_0818: Unknown result type (might be due to invalid IL or missing references)
		//IL_0827: Unknown result type (might be due to invalid IL or missing references)
		//IL_0836: Unknown result type (might be due to invalid IL or missing references)
		//IL_0845: Unknown result type (might be due to invalid IL or missing references)
		//IL_087b: Unknown result type (might be due to invalid IL or missing references)
		//IL_088f: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_08de: Unknown result type (might be due to invalid IL or missing references)
		//IL_090e: Unknown result type (might be due to invalid IL or missing references)
		//IL_091d: Unknown result type (might be due to invalid IL or missing references)
		//IL_092c: Unknown result type (might be due to invalid IL or missing references)
		//IL_093b: Unknown result type (might be due to invalid IL or missing references)
		//IL_094a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0963: Unknown result type (might be due to invalid IL or missing references)
		//IL_096f: Unknown result type (might be due to invalid IL or missing references)
		//IL_097b: Expected O, but got Unknown
		//IL_099c: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_09fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a09: Expected O, but got Unknown
		//IL_0a2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a3d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a56: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a65: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		resultOverlay = CreateOverlayRoot(parent, "RoundResultOverlay", new Color(0.03f, 0.05f, 0.15f, 0.74f));
		Image val = CreatePanel(resultOverlay.transform, "ResultModal", new Vector2(0f, 24f), new Vector2(830f, 1300f), new Color(0.13f, 0.17f, 0.42f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
		Image val2 = CreateShopArtwork(((Component)val).transform, "ResultVictoryTrumpetLeft", "InGame/ingame-duel-mode-victory-trumpet", new Vector2(-240f, -30f), new Vector2(198f, 158f), Color.white, new Vector2(0.5f, 1f));
		RegisterResultVictoryDecoration(val2);
		Image image = CreateShopArtwork(((Component)val).transform, "ResultVictoryTrumpetRight", "InGame/ingame-duel-mode-victory-trumpet", new Vector2(240f, -30f), new Vector2(198f, 158f), Color.white, new Vector2(0.5f, 1f));
		if ((Object)(object)val2 != (Object)null)
		{
			((Transform)((Graphic)val2).rectTransform).localScale = new Vector3(-1f, 1f, 1f);
		}
		RegisterResultVictoryDecoration(image);
		RegisterResultVictoryDecoration(CreateShopArtwork(((Component)val).transform, "ResultVictoryBanner", "InGame/ingame-duel-mode-victory-title-background", new Vector2(0f, -108f), new Vector2(720f, 182f), Color.white, new Vector2(0.5f, 1f)));
		RegisterResultVictoryDecoration(CreateShopArtwork(((Component)val).transform, "ResultVictoryTrophy", "GradeAndGoodsIcons/icon-trophy", new Vector2(0f, -46f), new Vector2(82f, 82f), Color.white, new Vector2(0.5f, 1f)));
		RegisterResultVictoryDecoration(CreateShopArtwork(((Component)val).transform, "ResultVictoryStarLeft", "InGame/minimi-star", new Vector2(-290f, -195f), new Vector2(30f, 30f), new Color(0.28f, 0.94f, 1f, 0.96f), new Vector2(0.5f, 1f)));
		RegisterResultVictoryDecoration(CreateShopArtwork(((Component)val).transform, "ResultVictoryStarRight", "InGame/minimi-star", new Vector2(290f, -195f), new Vector2(26f, 26f), new Color(1f, 0.66f, 0.24f, 0.96f), new Vector2(0.5f, 1f)));
		resultTitleText = CreateText(((Component)val).transform, "ResultTitle", "승리", new Color(1f, 0.84f, 0.18f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(500f, 66f), 56, (TextAnchor)4, bold: true);
		resultRibbonImage = CreatePanel(((Component)val).transform, "ResultRibbon", new Vector2(0f, -300f), new Vector2(650f, 140f), new Color(0.17f, 0.42f, 1f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		resultSummaryText = CreateText(((Component)val).transform, "ResultSummary", "라운드 1 클리어", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(650f, 40f), 28, (TextAnchor)4, bold: true);
		resultMetaText = CreateText(((Component)val).transform, "ResultMeta", "연속 클리어 +1", new Color(0.95f, 0.9f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(700f, 40f), 22, (TextAnchor)4, bold: true);
		Image val3 = CreatePanel(((Component)val).transform, "ResultRecapPanel", new Vector2(0f, -470f), new Vector2(730f, 306f), new Color(0.07f, 0.12f, 0.33f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		CreateShopArtwork(((Component)val3).transform, "ResultScoreTrophy", "GradeAndGoodsIcons/icon-trophy", new Vector2(-246f, -36f), new Vector2(40f, 40f), Color.white, new Vector2(0.5f, 1f));
		resultScoreText = CreateText(((Component)val3).transform, "ResultScore", "RUN SCORE A / 000점", new Color(1f, 0.85f, 0.24f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(660f, 44f), 34, (TextAnchor)4, bold: true);
		resultRecapText = CreateText(((Component)val3).transform, "ResultRecap", "이번 판 사건 3개\nCARD 1  결과 대기\nCARD 2  결과 대기\nCARD 3  결과 대기", new Color(0.9f, 0.96f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(660f, 132f), 27, (TextAnchor)0, bold: false);
		resultNextText = CreateText(((Component)val3).transform, "ResultNext", "다음 라운드 준비", new Color(0.62f, 1f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -238f), new Vector2(660f, 96f), 25, (TextAnchor)0, bold: true);
		resultRecapText.resizeTextForBestFit = true;
		resultRecapText.resizeTextMinSize = 12;
		resultRecapText.resizeTextMaxSize = 16;
		resultNextText.resizeTextForBestFit = true;
		resultNextText.resizeTextMinSize = 11;
		resultNextText.resizeTextMaxSize = 14;
		ApplyReadableResultTextLayout();
		Image val4 = CreatePanel(((Component)val).transform, "RewardPanel", new Vector2(0f, -800f), new Vector2(690f, 200f), new Color(0.18f, 0.15f, 0.52f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		CreateText(((Component)val4).transform, "RewardHeader", "전투 보상", new Color(1f, 0.9f, 0.46f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(250f, 32f), 24, (TextAnchor)4, bold: true);
		resultRewardGoldText = CreateRewardChip(((Component)val4).transform, "RewardGold", "ResultRewardGoldIcon", "골드", "GradeAndGoodsIcons/goods_icon_gold", new Vector2(-145f, -58f), new Color(1f, 0.74f, 0.2f), "+000");
		resultRewardCoreText = CreateRewardChip(((Component)val4).transform, "RewardDiamond", "ResultRewardDiamondIcon", "다이아", "GradeAndGoodsIcons/goods_icon_ruby", new Vector2(145f, -58f), new Color(0.3f, 0.84f, 1f), "+000");
		resultRetryButton = CreateButton(((Component)val).transform, "ResultRetryButton", "새 판 다시하기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-120f, 58f), new Vector2(306f, 78f), new Color(0.3f, 0.86f, 0.36f, 1f), new UnityAction(RetryFromResult), 27);
		resultContinueButton = CreateButton(((Component)val).transform, "ResultContinueButton", "계속하기", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(190f, 75f), new Vector2(240f, 100f), new Color(0.3f, 0.62f, 1f, 1f), new UnityAction(ContinueFromResult), 28);
		CreateShopArtwork(((Component)resultContinueButton).transform, "ContinueStar", "InGame/minimi-star", new Vector2(-114f, 0f), new Vector2(28f, 28f), new Color(1f, 0.91f, 0.34f, 1f), new Vector2(0.5f, 0.5f));
	}

	private void RegisterResultVictoryDecoration(Image image)
	{
		if ((Object)(object)image != (Object)null)
		{
			resultVictoryDecorations.Add(((Component)image).gameObject);
		}
	}

	private void ApplyReadableResultTextLayout()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		ConfigureResultRibbonText(resultSummaryText, new Vector2(0f, -318f), new Vector2(610f, 50f), 28, 21);
		ConfigureResultRibbonText(resultMetaText, new Vector2(0f, -370f), new Vector2(610f, 46f), 22, 18);
		ConfigureResultText(resultScoreText, new Vector2(0f, -36f), new Vector2(680f, 58f), 36, (TextAnchor)4, 34, 36);
		ConfigureResultText(resultRecapText, new Vector2(0f, -86f), new Vector2(650f, 124f), 23, (TextAnchor)0, 20, 23);
		ConfigureResultText(resultNextText, new Vector2(0f, -222f), new Vector2(650f, 62f), 20, (TextAnchor)4, 18, 20);
		AddReadableOutline(resultScoreText);
		AddReadableOutline(resultRecapText);
		AddReadableOutline(resultNextText);
		AddReadableOutline(resultSummaryText);
		AddReadableOutline(resultMetaText);
	}

	private static void ConfigureResultRibbonText(Text text, Vector2 position, Vector2 size, int fontSize, int minSize)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)text == (Object)null))
		{
			RectTransform rectTransform = ((Graphic)text).rectTransform;
			if ((Object)(object)rectTransform != (Object)null)
			{
				rectTransform.anchoredPosition = position;
				rectTransform.sizeDelta = size;
			}
			text.fontSize = fontSize;
			text.alignment = (TextAnchor)4;
			text.horizontalOverflow = (HorizontalWrapMode)0;
			text.verticalOverflow = (VerticalWrapMode)0;
			text.resizeTextForBestFit = true;
			text.resizeTextMinSize = minSize;
			text.resizeTextMaxSize = fontSize;
		}
	}

	private static void ConfigureResultText(Text text, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, int minSize, int maxSize)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)text == (Object)null))
		{
			RectTransform rectTransform = ((Graphic)text).rectTransform;
			if ((Object)(object)rectTransform != (Object)null)
			{
				rectTransform.anchoredPosition = position;
				rectTransform.sizeDelta = size;
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
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		loadoutOverlay = CreateOverlayRoot(parent, "LoadoutOverlay", Color.clear);
		((Graphic)loadoutOverlay.GetComponent<Image>()).raycastTarget = false;
		Image val = CreatePanel(loadoutOverlay.transform, "LoadoutModal", new Vector2(0f, 76f), new Vector2(0f, -152f), new Color(0.27f, 0.38f, 0.74f, 1f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: false, shadow: false);
		val.sprite = null;
		val.type = (Type)0;
		val.preserveAspect = false;
		CreatePanel(((Component)val).transform, "LoadoutHeader", new Vector2(0f, -18f), new Vector2(900f, 112f), new Color(0.96f, 0.8f, 0.2f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		loadoutHeaderText = CreateText(((Component)val).transform, "LoadoutHeaderText", "이번 라운드 추천 조합", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(520f, 48f), 36, (TextAnchor)4, bold: true);
		loadoutSummaryText = CreateText(((Component)val).transform, "LoadoutSummaryText", "현재 라운드 흐름에 맞춘 운영 참고용 조합입니다.", new Color(0.88f, 0.92f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(760f, 32f), 18, (TextAnchor)4, bold: false);
		BuildRecommendationNotice(((Component)val).transform, -172f);
		CreateText(((Component)val).transform, "DeckHeader", "추천 핵심 유닛 (참고용)", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -286f), new Vector2(620f, 32f), 24, (TextAnchor)4, bold: true);
		BuildLoadoutDeckCards(((Component)val).transform);
		CreateText(((Component)val).transform, "RosterHeader", "보유 유닛 참고", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -760f), new Vector2(620f, 32f), 24, (TextAnchor)4, bold: true);
		BuildLoadoutRosterCards(((Component)val).transform);
	}

	private void BuildRecommendationNotice(Transform parent, float anchoredY)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		Image val = CreatePanel(parent, "RecommendedCompositionNotice", new Vector2(0f, anchoredY), new Vector2(650f, 64f), new Color(0.1f, 0.18f, 0.4f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreateText(((Component)val).transform, "RecommendationNoticeTitle", "자동 추천", new Color(1f, 0.86f, 0.34f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(160f, 34f), 21, (TextAnchor)3, bold: true);
		CreateText(((Component)val).transform, "RecommendationNoticeHint", "선택 기능 없음 · 소환 확률 영향 없음", new Color(0.82f, 0.9f, 1f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(420f, 34f), 18, (TextAnchor)5, bold: false);
	}

	private void BuildLobbyFeaturedCards(Transform parent)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < cardsPerPreset; i++)
		{
			float num = -344f + (float)i * 172f;
			Image val = CreatePanel(parent, "LobbyFeaturedCard_" + i, new Vector2(num, -690f), new Vector2(154f, 230f), new Color(0.94f, 0.96f, 0.99f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			Image item = CreatePanel(((Component)val).transform, "Accent", new Vector2(0f, -8f), new Vector2(124f, 42f), Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Image item2 = CreatePanel(((Component)val).transform, "Portrait", new Vector2(0f, -52f), new Vector2(116f, 86f), new Color(0.84f, 0.9f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreatePanel(((Component)val).transform, "LabelBack", new Vector2(0f, 38f), new Vector2(142f, 78f), new Color(0.04f, 0.07f, 0.18f, 0.82f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			Text val2 = CreateText(((Component)val).transform, "Name", "Hero", Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(140f, 28f), 18, (TextAnchor)4, bold: true);
			Text val3 = CreateText(((Component)val).transform, "Grade", "일반", new Color(0.82f, 0.92f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(132f, 24f), 16, (TextAnchor)4, bold: true);
			val2.resizeTextForBestFit = true;
			val2.resizeTextMinSize = 12;
			val2.resizeTextMaxSize = 18;
			val3.resizeTextForBestFit = true;
			val3.resizeTextMinSize = 11;
			val3.resizeTextMaxSize = 16;
			AddReadableOutline(val2);
			AddReadableOutline(val3);
			lobbyFeaturedAccentImages.Add(item);
			lobbyFeaturedPortraitImages.Add(item2);
			lobbyFeaturedNameTexts.Add(val2);
			lobbyFeaturedGradeTexts.Add(val3);
		}
	}

	private void BuildLoadoutDeckCards(Transform parent)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < cardsPerPreset; i++)
		{
			float num = -352f + (float)i * 176f;
			Image val = CreatePanel(parent, "LoadoutDeckCard_" + i, new Vector2(num, -384f), new Vector2(156f, 210f), new Color(0.95f, 0.97f, 0.99f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			Image item = CreatePanel(((Component)val).transform, "Accent", new Vector2(0f, -8f), new Vector2(130f, 54f), Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Image item2 = CreatePanel(((Component)val).transform, "Portrait", new Vector2(0f, -76f), new Vector2(104f, 66f), new Color(0.86f, 0.91f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreatePanel(((Component)val).transform, "LabelBack", new Vector2(0f, 40f), new Vector2(140f, 74f), new Color(0.04f, 0.07f, 0.18f, 0.82f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			Text val2 = CreateText(((Component)val).transform, "Name", "Hero", Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(130f, 30f), 18, (TextAnchor)4, bold: true);
			Text val3 = CreateText(((Component)val).transform, "Detail", "일반 / 역할", new Color(0.82f, 0.92f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(132f, 36f), 15, (TextAnchor)4, bold: false);
			AddReadableOutline(val2);
			AddReadableOutline(val3);
			loadoutDeckAccentImages.Add(item);
			loadoutDeckPortraitImages.Add(item2);
			loadoutDeckNameTexts.Add(val2);
			loadoutDeckDetailTexts.Add(val3);
		}
	}

	private void BuildLoadoutRosterCards(Transform parent)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < 8; i++)
		{
			int num = i % 2;
			int num2 = i / 2;
			float num3 = -210f + (float)num * 420f;
			float num4 = -848f - (float)num2 * 132f;
			Image val = CreatePanel(parent, "LoadoutRosterCard_" + i, new Vector2(num3, num4), new Vector2(360f, 112f), new Color(0.14f, 0.18f, 0.42f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			Image item = CreatePanel(((Component)val).transform, "Accent", new Vector2(18f, -16f), new Vector2(76f, 78f), Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
			Image item2 = CreatePanel(((Component)val).transform, "Portrait", new Vector2(18f, -16f), new Vector2(76f, 78f), new Color(0.86f, 0.91f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
			Text item3 = CreateText(((Component)val).transform, "Name", "Hero", Color.white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(114f, -18f), new Vector2(-144f, 28f), 18, (TextAnchor)3, bold: true);
			Text item4 = CreateText(((Component)val).transform, "Detail", "등급 / 역할 / 스킬", new Color(0.85f, 0.9f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(114f, -52f), new Vector2(-144f, 44f), 15, (TextAnchor)3, bold: false);
			loadoutRosterAccentImages.Add(item);
			loadoutRosterPortraitImages.Add(item2);
			loadoutRosterNameTexts.Add(item3);
			loadoutRosterDetailTexts.Add(item4);
		}
	}

	private Text CreateRewardChip(Transform parent, string name, string iconName, string title, string iconResourcePath, Vector2 anchoredPosition, Color accentColor, string value)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		Color color = Color.Lerp(new Color(0.08f, 0.12f, 0.34f, 0.98f), accentColor, 0.18f);
		Image val = CreatePanel(parent, name, anchoredPosition, new Vector2(254f, 126f), color, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		CreatePanel(((Component)val).transform, "Accent", new Vector2(0f, -8f), new Vector2(206f, 5f), accentColor, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreateText(((Component)val).transform, "Title", title, new Color(0.94f, 0.97f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(22f, -26f), new Vector2(136f, 28f), 20, (TextAnchor)3, bold: true);
		CreateShopArtwork(((Component)val).transform, iconName, iconResourcePath, new Vector2(-78f, 24f), new Vector2(68f, 68f), Color.white, new Vector2(0.5f, 0f));
		return CreateText(((Component)val).transform, "Value", value, accentColor, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(38f, 28f), new Vector2(132f, 48f), 34, (TextAnchor)4, bold: true);
	}

	private void WireButtons()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Expected O, but got Unknown
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Expected O, but got Unknown
		if ((Object)(object)battleButton != (Object)null)
		{
			((UnityEventBase)battleButton.onClick).RemoveAllListeners();
			AddButtonListener(battleButton, new UnityAction(HandleBattlePressed));
		}
		if ((Object)(object)lobbyButton != (Object)null)
		{
			((UnityEventBase)lobbyButton.onClick).RemoveAllListeners();
			AddButtonListener(lobbyButton, new UnityAction(HandleExitToOutgamePressed));
		}
		if ((Object)(object)loadoutButton != (Object)null)
		{
			((UnityEventBase)loadoutButton.onClick).RemoveAllListeners();
			AddButtonListener(loadoutButton, new UnityAction(ToggleLoadout));
		}
		if ((Object)(object)lobbyModeButton != (Object)null)
		{
			((UnityEventBase)lobbyModeButton.onClick).RemoveAllListeners();
			AddButtonListener(lobbyModeButton, new UnityAction(TogglePlayMode));
		}
	}

	private void BuildPresets()
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		presets.Clear();
		string[] array = new string[4] { "안정 성장", "웨이브 정리", "제어 운영", "보스 대비" };
		string[] array2 = new string[4] { "안정적인 초반 운영과 균형 잡힌 합성 흐름을 위한 추천입니다.", "공격 템포와 광역 압박으로 몰려오는 웨이브를 정리하는 추천입니다.", "마나 순환과 군중 제어형 스킬을 활용하는 운영 추천입니다.", "보스 라운드에 대비해 집중 화력과 유지력을 챙기는 추천입니다." };
		Color[] array3 = (Color[])(object)new Color[4]
		{
			new Color(0.19f, 0.92f, 0.92f),
			new Color(1f, 0.58f, 0.26f),
			new Color(0.4f, 0.86f, 0.44f),
			new Color(1f, 0.38f, 0.46f)
		};
		int deployableCharacterCount = GetDeployableCharacterCount();
		int num = Mathf.Max(1, presetCount);
		for (int i = 0; i < num; i++)
		{
			PresetDefinition presetDefinition = new PresetDefinition
			{
				name = array[i % array.Length],
				description = array2[i % array2.Length],
				accentColor = array3[i % array3.Length]
			};
			if (deployableCharacterCount > 0)
			{
				int num2 = i * cardsPerPreset % deployableCharacterCount;
				for (int j = 0; j < cardsPerPreset; j++)
				{
					presetDefinition.characterIndices.Add((num2 + j) % deployableCharacterCount);
				}
			}
			presets.Add(presetDefinition);
		}
	}

	private void ApplyRecommendedPreset()
	{
		if (presets.Count != 0)
		{
			int round = ResolveUpcomingRecommendationRound();
			selectedPresetIndex = ResolveRecommendedPresetIndex(round);
			PresetDefinition presetDefinition = presets[selectedPresetIndex];
			if ((Object)(object)loadoutHeaderText != (Object)null)
			{
				loadoutHeaderText.text = "R" + round + " 추천 조합 · " + presetDefinition.name;
			}
			if ((Object)(object)loadoutSummaryText != (Object)null)
			{
				loadoutSummaryText.text = presetDefinition.description;
			}
			UpdateLobbyFeaturedCards(presetDefinition);
			UpdateLoadoutDeckCards(presetDefinition);
			UpdateLoadoutRosterCards(presetDefinition);
		}
	}

	private void RefreshLobbyPreparationStatus()
	{
		if ((Object)(object)lobbyCollectionSummaryText != (Object)null)
		{
			lobbyCollectionSummaryText.text = (((Object)(object)outgameProgression != (Object)null) ? outgameProgression.BuildCollectionSummary() : "보유 영웅 정보 없음");
			lobbyCollectionSummaryText.resizeTextForBestFit = true;
			lobbyCollectionSummaryText.resizeTextMinSize = 12;
			lobbyCollectionSummaryText.resizeTextMaxSize = 16;
		}
		if ((Object)(object)lobbyChestStatusText != (Object)null)
		{
			if ((Object)(object)outgameProgression != (Object)null)
			{
				lobbyChestStatusText.text = "보유 " + outgameProgression.EarnedChestKeys + "개  |  " + outgameProgression.EarnedChestProgress + "/" + outgameProgression.EarnedChestProgressTarget;
			}
			else
			{
				lobbyChestStatusText.text = "상자 준비 정보 없음";
			}
		}
		if ((Object)(object)lobbyRecordStatusText != (Object)null)
		{
			int num = ((!((Object)(object)outgameProgression != (Object)null)) ? 1 : Mathf.Max(1, outgameProgression.HighestRoundReached));
			lobbyRecordStatusText.text = "최고 R" + num;
		}
	}

	private int ResolveUpcomingRecommendationRound()
	{
		if ((Object)(object)gameController == (Object)null)
		{
			return 1;
		}
		int num = Mathf.Max(0, gameController.CurrentRound);
		return gameController.IsRoundRunning ? Mathf.Max(1, num) : (num + 1);
	}

	private int ResolveRecommendedPresetIndex(int round)
	{
		int num = (Mathf.Max(1, round) - 1) % 10 + 1;
		int num2 = ((num > 3) ? ((num <= 6) ? 1 : ((num <= 9) ? 2 : 3)) : 0);
		return Mathf.Clamp(num2, 0, Mathf.Max(0, presets.Count - 1));
	}

	private void UpdateLobbyFeaturedCards(PresetDefinition preset)
	{
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < lobbyFeaturedNameTexts.Count; i++)
		{
			CharacterDefinition presetCharacter = GetPresetCharacter(preset, i);
			if (presetCharacter == null)
			{
				((Graphic)lobbyFeaturedAccentImages[i]).color = new Color(0.75f, 0.78f, 0.86f);
				ApplyCharacterPortrait(lobbyFeaturedPortraitImages, i, null);
				lobbyFeaturedNameTexts[i].text = "비어 있음";
				lobbyFeaturedGradeTexts[i].text = "미설정";
				SetCardLabelColors(lobbyFeaturedNameTexts[i], lobbyFeaturedGradeTexts[i]);
			}
			else
			{
				((Graphic)lobbyFeaturedAccentImages[i]).color = GetGradeColor(presetCharacter.grade, presetCharacter.accentColor);
				ApplyCharacterPortrait(lobbyFeaturedPortraitImages, i, presetCharacter);
				lobbyFeaturedNameTexts[i].text = presetCharacter.displayName;
				lobbyFeaturedGradeTexts[i].text = GetGradeName(presetCharacter.grade) + " / " + BuildCardLevelLabel(presetCharacter);
				SetCardLabelColors(lobbyFeaturedNameTexts[i], lobbyFeaturedGradeTexts[i]);
			}
		}
	}

	private void UpdateLoadoutDeckCards(PresetDefinition preset)
	{
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < loadoutDeckNameTexts.Count; i++)
		{
			CharacterDefinition presetCharacter = GetPresetCharacter(preset, i);
			if (presetCharacter == null)
			{
				((Graphic)loadoutDeckAccentImages[i]).color = new Color(0.78f, 0.82f, 0.88f);
				ApplyCharacterPortrait(loadoutDeckPortraitImages, i, null);
				loadoutDeckNameTexts[i].text = "빈 슬롯";
				loadoutDeckDetailTexts[i].text = "유닛을 추가하면 여기에 표시됩니다.";
				SetCardLabelColors(loadoutDeckNameTexts[i], loadoutDeckDetailTexts[i]);
			}
			else
			{
				((Graphic)loadoutDeckAccentImages[i]).color = GetGradeColor(presetCharacter.grade, presetCharacter.accentColor);
				ApplyCharacterPortrait(loadoutDeckPortraitImages, i, presetCharacter);
				loadoutDeckNameTexts[i].text = presetCharacter.displayName;
				loadoutDeckDetailTexts[i].text = GetGradeName(presetCharacter.grade) + " / " + BuildCardLevelLabel(presetCharacter);
				SetCardLabelColors(loadoutDeckNameTexts[i], loadoutDeckDetailTexts[i]);
			}
		}
	}

	private void UpdateLoadoutRosterCards(PresetDefinition preset)
	{
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		int deployableCharacterCount = GetDeployableCharacterCount();
		int num = ((deployableCharacterCount > 0) ? (selectedPresetIndex * 7 % deployableCharacterCount) : 0);
		for (int i = 0; i < loadoutRosterNameTexts.Count; i++)
		{
			CharacterDefinition character = GetCharacter(num + i);
			if (character == null)
			{
				((Graphic)loadoutRosterAccentImages[i]).color = new Color(0.72f, 0.76f, 0.84f);
				ApplyCharacterPortrait(loadoutRosterPortraitImages, i, null);
				loadoutRosterNameTexts[i].text = "후보 없음";
				loadoutRosterDetailTexts[i].text = "캐릭터 데이터가 늘어나면 이 자리가 채워집니다.";
				continue;
			}
			((Graphic)loadoutRosterAccentImages[i]).color = GetGradeColor(character.grade, character.accentColor);
			ApplyCharacterPortrait(loadoutRosterPortraitImages, i, character);
			loadoutRosterNameTexts[i].text = character.displayName;
			string text = ((character.skills != null && character.skills.Count > 0) ? character.skills[0].displayName : "기본 공격");
			loadoutRosterDetailTexts[i].text = GetGradeName(character.grade) + " / " + BuildCardLevelLabel(character) + " / " + text;
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
		if ((Object)(object)characterDatabase == (Object)null)
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
		int index2 = index % deployableCharacters.Count;
		return deployableCharacters[index2];
	}

	private int GetDeployableCharacterCount()
	{
		return ((Object)(object)characterDatabase != (Object)null) ? characterDatabase.GetDeployableCharacters().Count : 0;
	}

	private void HandleEnterPreparationPressed()
	{
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)gameController == (Object)null) && !gameController.IsRoundRunning)
		{
			if (matchmakingRoutine != null)
			{
				((MonoBehaviour)this).StopCoroutine(matchmakingRoutine);
				matchmakingRoutine = null;
			}
			HideResult();
			HideLoadout();
			HideShop();
			HideLobby();
			HideMatchmaking();
			HideOutgamePlaceholder();
			HideExitConfirm();
			if ((Object)(object)characterCollectionUI != (Object)null)
			{
				characterCollectionUI.Close();
			}
			SetGameplayHudVisible(visible: true);
			gameController.RequestBanner("준비 단계  유닛을 소환한 뒤 다음 라운드를 누르세요", new Color(0.72f, 0.86f, 0.58f), 3f);
		}
	}

	private void HandleBattlePressed()
	{
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)gameController == (Object)null || (Object)(object)buttonBinder == (Object)null || gameController.IsRoundRunning)
		{
			return;
		}
		if ((Object)(object)augmentManager != (Object)null && augmentManager.HasPendingChoice)
		{
			augmentManager.OpenPendingChoice();
			gameController.RequestBanner("무료 증강체 1개를 선택해야 다음 라운드로 진행할 수 있습니다", new Color(0.52f, 0.9f, 1f), 2.2f);
			return;
		}
		if (matchmakingRoutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(matchmakingRoutine);
			matchmakingRoutine = null;
		}
		if ((Object)(object)resultOverlay != (Object)null && resultOverlay.activeSelf)
		{
			HideResult();
		}
		if ((Object)(object)characterCollectionUI != (Object)null)
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
			if ((Object)(object)queueTimerText != (Object)null)
			{
				queueTimerText.text = elapsed.ToString("00.00");
			}
			if ((Object)(object)queueStatusText != (Object)null)
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
			((MonoBehaviour)this).StopCoroutine(matchmakingRoutine);
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
		if ((Object)(object)exitConfirmOverlay == (Object)null)
		{
			ConfirmExitToOutgame();
		}
		else
		{
			exitConfirmOverlay.SetActive(true);
		}
	}

	private void HideExitConfirm()
	{
		if ((Object)(object)exitConfirmOverlay != (Object)null)
		{
			exitConfirmOverlay.SetActive(false);
		}
	}

	private void ConfirmExitToOutgame()
	{
		if ((Object)(object)gameController != (Object)null)
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
				((MonoBehaviour)this).StopCoroutine(resultRoutine);
			}
			RuntimeAudioUtility.PlayVictory();
			resultRoutine = ((MonoBehaviour)this).StartCoroutine(ShowRoundResultAfterFlow(round));
		}
	}

	private IEnumerator ShowRoundResultAfterFlow(int round)
	{
		yield return (object)new WaitForSeconds(0.35f);
		if ((Object)(object)gameController != (Object)null && gameController.Life > 0 && !defeatPresented)
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
			((MonoBehaviour)this).StopCoroutine(resultRoutine);
			resultRoutine = null;
		}
		HideMatchmaking();
		HideLoadout();
		HideShop();
		resultRoutine = ((MonoBehaviour)this).StartCoroutine(ShowGameOverResultAfterCinematic(((Object)(object)gameController != (Object)null) ? gameController.CurrentRound : 0));
	}

	private IEnumerator ShowGameOverResultAfterCinematic(int round)
	{
		float minimumDelay = 5.15f;
		yield return (object)new WaitForSecondsRealtime(Mathf.Max(minimumDelay, defeatResultRevealDelay));
		ShowResult(victory: false, round);
		resultRoutine = null;
	}

	private void ShowLobby()
	{
		if (!((Object)(object)gameController != (Object)null) || !gameController.IsRoundRunning)
		{
			HideSeasonRanking();
			BuildPresets();
			ApplyRecommendedPreset();
			RefreshLobbyPreparationStatus();
			SetGameplayHudVisible(visible: false);
			if ((Object)(object)lobbyOverlay != (Object)null)
			{
				lobbyOverlay.SetActive(true);
			}
			if ((Object)(object)outgameNavigationRoot != (Object)null)
			{
				outgameNavigationRoot.SetActive(true);
				outgameNavigationRoot.transform.SetAsLastSibling();
			}
			HighlightOutgameNav(hubLobbyButton);
		}
	}

	private void HideLobby()
	{
		if ((Object)(object)lobbyOverlay != (Object)null)
		{
			lobbyOverlay.SetActive(false);
		}
		if ((Object)(object)outgameNavigationRoot != (Object)null)
		{
			outgameNavigationRoot.SetActive(false);
		}
	}

	private void HandleLobbyPressed()
	{
		if (!((Object)(object)gameController != (Object)null) || !gameController.IsRoundRunning)
		{
			HideResult();
			HideLoadout();
			HideShop();
			HideOutgamePlaceholder();
			HideExitConfirm();
			if ((Object)(object)characterCollectionUI != (Object)null && characterCollectionUI.IsOpen)
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
		if ((Object)(object)gameController != (Object)null && gameController.IsRoundRunning)
		{
			return;
		}
		if ((Object)(object)loadoutOverlay != (Object)null && loadoutOverlay.activeSelf)
		{
			HideLoadout();
			return;
		}
		SetGameplayHudVisible(visible: false);
		HideResult();
		HideShop();
		HideOutgamePlaceholder();
		HideExitConfirm();
		if ((Object)(object)characterCollectionUI != (Object)null && characterCollectionUI.IsOpen)
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
		if (!((Object)(object)characterCollectionUI == (Object)null))
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
		if (!((Object)(object)characterCollectionUI == (Object)null))
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
		if ((Object)(object)outgameNavigationRoot != (Object)null)
		{
			outgameNavigationRoot.SetActive(true);
			outgameNavigationRoot.transform.SetAsLastSibling();
		}
		HighlightOutgameNav(hubInventoryButton);
	}

	private void HandleCollectionClosed()
	{
		if ((!((Object)(object)matchmakingOverlay != (Object)null) || !matchmakingOverlay.activeSelf) && (!((Object)(object)resultOverlay != (Object)null) || !resultOverlay.activeSelf) && (!((Object)(object)shopOverlay != (Object)null) || !shopOverlay.activeSelf) && (!((Object)(object)loadoutOverlay != (Object)null) || !loadoutOverlay.activeSelf) && (!((Object)(object)seasonRankingOverlay != (Object)null) || !seasonRankingOverlay.activeSelf) && (!((Object)(object)outgamePlaceholderOverlay != (Object)null) || !outgamePlaceholderOverlay.activeSelf))
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
		return (Object)(object)gameController == (Object)null || (!gameController.IsRoundRunning && gameController.CurrentRound <= 0);
	}

	private void ToggleShop()
	{
		if ((Object)(object)gameController != (Object)null && gameController.IsRoundRunning)
		{
			return;
		}
		if (((Scene)(ref shopScene)).IsValid() && ((Scene)(ref shopScene)).isLoaded)
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
		if ((Object)(object)characterCollectionUI != (Object)null && characterCollectionUI.IsOpen)
		{
			characterCollectionUI.Close();
		}
		ShowShop();
		HighlightOutgameNav(hubShopButton);
	}

	private void ShowShop()
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		if (!((Scene)(ref shopScene)).IsValid() || !((Scene)(ref shopScene)).isLoaded)
		{
			BuildShopScene();
		}
		RefreshShop();
		if ((Object)(object)shopOverlay != (Object)null)
		{
			shopOverlay.SetActive(true);
		}
		if (((Scene)(ref shopScene)).IsValid() && ((Scene)(ref shopScene)).isLoaded)
		{
			SceneManager.SetActiveScene(shopScene);
		}
	}

	private void HideShop()
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		if (drawRevealRoutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(drawRevealRoutine);
			drawRevealRoutine = null;
		}
		if (shopPurchaseResultRoutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(shopPurchaseResultRoutine);
			shopPurchaseResultRoutine = null;
		}
		if (shopCurrencyCountRoutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(shopCurrencyCountRoutine);
			shopCurrencyCountRoutine = null;
		}
		if (((Scene)(ref gameplayScene)).IsValid() && ((Scene)(ref gameplayScene)).isLoaded)
		{
			SceneManager.SetActiveScene(gameplayScene);
		}
		if (((Scene)(ref shopScene)).IsValid() && ((Scene)(ref shopScene)).isLoaded)
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
		if ((Object)(object)lobbyOverlay != (Object)null && lobbyOverlay.activeSelf)
		{
			HighlightOutgameNav(hubLobbyButton);
		}
	}

	private void BuildShopScene()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		gameplayScene = ((Component)this).gameObject.scene;
		shopScene = SceneManager.CreateScene("OutgameShop");
		shopSceneCanvasRoot = new GameObject("OutgameShopCanvas", new Type[4]
		{
			typeof(RectTransform),
			typeof(Canvas),
			typeof(CanvasScaler),
			typeof(GraphicRaycaster)
		});
		SceneManager.MoveGameObjectToScene(shopSceneCanvasRoot, shopScene);
		Canvas component = shopSceneCanvasRoot.GetComponent<Canvas>();
		component.renderMode = (RenderMode)0;
		component.sortingOrder = 300;
		CanvasScaler component2 = shopSceneCanvasRoot.GetComponent<CanvasScaler>();
		component2.uiScaleMode = (ScaleMode)1;
		component2.screenMatchMode = (ScreenMatchMode)1;
		component2.referenceResolution = new Vector2(1080f, 1920f);
		component2.matchWidthOrHeight = 0.84f;
		shopSceneCanvasRoot.AddComponent<RuntimeKoreanTextCleaner>();
		BuildShopOverlay(shopSceneCanvasRoot.transform);
	}

	private void TogglePlayMode()
	{
		if (!((Object)(object)outgameProgression == (Object)null) && (!((Object)(object)gameController != (Object)null) || !gameController.IsRoundRunning))
		{
			OutgamePlayMode playMode = ((!outgameProgression.IsTestMode) ? OutgamePlayMode.Test : OutgamePlayMode.Service);
			gameController?.ClearBoardForProfileChange();
			outgameProgression.SwitchPlayMode(playMode);
		}
	}

	private void RechargeTestDiamonds()
	{
		outgameProgression?.RechargeTestCurrency();
	}

	private void ShowCashBundlePurchaseConfirm(int index)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Expected O, but got Unknown
		string[] array = new string[3] { "골드 주머니", "다이아 주머니", "성장 꾸러미" };
		string[] array2 = new string[3] { "10,000 GOLD", "1,200 DIA", "20,000 GOLD + 2,000 DIA" };
		string[] array3 = new string[3] { "₩3,300", "₩6,600", "₩9,900" };
		int safeIndex = Mathf.Clamp(index, 0, array.Length - 1);
		ShowShopPurchaseConfirm(array[safeIndex], array2[safeIndex] + "\n가격 " + array3[safeIndex] + "\n구매하시겠습니까?", "구매", (UnityAction)delegate
		{
			HandleCashBundlePurchase(safeIndex);
		});
	}

	private void ShowDailyOfferPurchaseConfirm(int index)
	{
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Expected O, but got Unknown
		if (!((Object)(object)outgameProgression == (Object)null))
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
			ShowShopPurchaseConfirm(title, body, confirmLabel, (UnityAction)delegate
			{
				TryPurchaseDailyOffer(safeIndex);
			});
		}
	}

	private void ShowPremiumChestPurchaseConfirm(int drawCount)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		if (!((Object)(object)outgameProgression == (Object)null))
		{
			int num = outgameProgression.ResolvePremiumChestCost(drawCount);
			ShowShopPurchaseConfirm("영웅 카드 상자 " + drawCount + "개", num.ToString("N0") + " DIA가 차감됩니다.\n상자를 여시겠습니까?", "구매", (UnityAction)delegate
			{
				TryOpenChest(drawCount);
			});
		}
	}

	private void ShowEarnedChestConfirm()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		if (!((Object)(object)outgameProgression == (Object)null))
		{
			ShowShopPurchaseConfirm("무료 영웅 상자", "보유 상자 1개를 사용합니다.\n상자를 여시겠습니까?", "열기", new UnityAction(TryOpenEarnedChest));
		}
	}

	private void ShowShopPurchaseConfirm(string title, string body, string confirmLabel, UnityAction action)
	{
		if (!((Object)(object)shopPurchaseConfirmOverlay == (Object)null) && action != null)
		{
			HideShopPurchaseResultPopup();
			pendingShopPurchaseAction = action;
			if ((Object)(object)shopPurchaseConfirmTitleText != (Object)null)
			{
				shopPurchaseConfirmTitleText.text = title;
			}
			if ((Object)(object)shopPurchaseConfirmBodyText != (Object)null)
			{
				shopPurchaseConfirmBodyText.text = body;
			}
			SetButtonLabel(shopPurchaseConfirmButton, confirmLabel);
			shopPurchaseConfirmOverlay.transform.SetAsLastSibling();
			shopPurchaseConfirmOverlay.SetActive(true);
		}
	}

	private void HideShopPurchaseConfirm()
	{
		pendingShopPurchaseAction = null;
		GameObject obj = shopPurchaseConfirmOverlay;
		if (obj != null)
		{
			obj.SetActive(false);
		}
	}

	private void ConfirmPendingShopPurchase()
	{
		UnityAction val = pendingShopPurchaseAction;
		pendingShopPurchaseAction = null;
		GameObject obj = shopPurchaseConfirmOverlay;
		if (obj != null)
		{
			obj.SetActive(false);
		}
		if (val != null)
		{
			val.Invoke();
		}
	}

	private void SetShopResultHint(string message)
	{
		if ((Object)(object)shopResultText != (Object)null)
		{
			shopResultText.text = message;
		}
	}

	private void ShowShopPurchaseResultPopup(string title, string body, string currencyLine, string iconResourcePath, Color accentColor)
	{
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		SetShopResultHint(title + "\n팝업으로 획득 결과를 확인하세요.");
		if ((Object)(object)shopPurchaseResultOverlay == (Object)null)
		{
			return;
		}
		if ((Object)(object)shopPurchaseResultTitleText != (Object)null)
		{
			shopPurchaseResultTitleText.text = title;
		}
		if ((Object)(object)shopPurchaseResultBodyText != (Object)null)
		{
			shopPurchaseResultBodyText.text = body;
		}
		if ((Object)(object)shopPurchaseResultCurrencyText != (Object)null)
		{
			bool active = !string.IsNullOrWhiteSpace(currencyLine);
			((Component)shopPurchaseResultCurrencyText).gameObject.SetActive(active);
			shopPurchaseResultCurrencyText.text = currencyLine;
			((Graphic)shopPurchaseResultCurrencyText).color = Color.Lerp(new Color(1f, 0.84f, 0.26f), accentColor, 0.24f);
		}
		if ((Object)(object)shopPurchaseResultIconImage != (Object)null)
		{
			Sprite val = RollRollUiResource.LoadSprite(iconResourcePath);
			if ((Object)(object)val != (Object)null)
			{
				shopPurchaseResultIconImage.sprite = val;
			}
			((Graphic)shopPurchaseResultIconImage).color = Color.white;
		}
		shopPurchaseResultOverlay.transform.SetAsLastSibling();
		shopPurchaseResultOverlay.SetActive(true);
		if (shopPurchaseResultRoutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(shopPurchaseResultRoutine);
		}
		shopPurchaseResultRoutine = ((MonoBehaviour)this).StartCoroutine(AnimateShopPurchaseResultPopup());
	}

	private void HideShopPurchaseResultPopup()
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		if (shopPurchaseResultRoutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(shopPurchaseResultRoutine);
			shopPurchaseResultRoutine = null;
		}
		if ((Object)(object)shopPurchaseResultOverlay != (Object)null)
		{
			shopPurchaseResultOverlay.SetActive(false);
		}
		if ((Object)(object)shopPurchaseResultCanvasGroup != (Object)null)
		{
			shopPurchaseResultCanvasGroup.alpha = 1f;
		}
		if ((Object)(object)shopPurchaseResultModalRect != (Object)null)
		{
			((Transform)shopPurchaseResultModalRect).localScale = Vector3.one;
		}
	}

	private IEnumerator AnimateShopPurchaseResultPopup()
	{
		if ((Object)(object)shopPurchaseResultCanvasGroup != (Object)null)
		{
			shopPurchaseResultCanvasGroup.alpha = 0f;
			shopPurchaseResultCanvasGroup.blocksRaycasts = true;
		}
		if ((Object)(object)shopPurchaseResultModalRect != (Object)null)
		{
			((Transform)shopPurchaseResultModalRect).localScale = Vector3.one * 0.88f;
		}
		float elapsed = 0f;
		while (elapsed < 0.22f)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / 0.22f);
			float eased = Mathf.SmoothStep(0f, 1f, t);
			if ((Object)(object)shopPurchaseResultCanvasGroup != (Object)null)
			{
				shopPurchaseResultCanvasGroup.alpha = eased;
			}
			if ((Object)(object)shopPurchaseResultModalRect != (Object)null)
			{
				float scale = Mathf.Lerp(0.88f, 1.04f, eased);
				((Transform)shopPurchaseResultModalRect).localScale = Vector3.one * scale;
			}
			yield return null;
		}
		if ((Object)(object)shopPurchaseResultCanvasGroup != (Object)null)
		{
			shopPurchaseResultCanvasGroup.alpha = 1f;
		}
		if ((Object)(object)shopPurchaseResultModalRect != (Object)null)
		{
			((Transform)shopPurchaseResultModalRect).localScale = Vector3.one;
		}
		yield return null;
		shopPurchaseResultRoutine = null;
	}

	private void SetShopCurrencyText(int gold, int diamonds)
	{
		displayedShopGold = gold;
		displayedShopDiamonds = diamonds;
		if ((Object)(object)shopGoldText != (Object)null)
		{
			shopGoldText.text = "GOLD " + gold.ToString("N0");
		}
		if ((Object)(object)shopDiamondText != (Object)null)
		{
			shopDiamondText.text = "DIA " + diamonds.ToString("N0");
		}
	}

	private void PlayShopCurrencyChange(int fromGold, int fromDiamonds)
	{
		if (!((Object)(object)outgameProgression == (Object)null))
		{
			int num = fromGold;
			int num2 = fromDiamonds;
			if (shopCurrencyCountRoutine != null)
			{
				num = displayedShopGold;
				num2 = displayedShopDiamonds;
				((MonoBehaviour)this).StopCoroutine(shopCurrencyCountRoutine);
				shopCurrencyCountRoutine = null;
			}
			int gold = outgameProgression.Gold;
			int diamonds = outgameProgression.Diamonds;
			if (num == gold && num2 == diamonds)
			{
				SetShopCurrencyText(gold, diamonds);
			}
			else
			{
				shopCurrencyCountRoutine = ((MonoBehaviour)this).StartCoroutine(AnimateShopCurrencyText(num, num2, gold, diamonds));
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
			int gold = Mathf.RoundToInt(Mathf.Lerp((float)startGold, (float)targetGold, t));
			int diamonds = Mathf.RoundToInt(Mathf.Lerp((float)startDiamonds, (float)targetDiamonds, t));
			SetShopCurrencyText(gold, diamonds);
			float pulse = 1f + Mathf.Sin(t * MathF.PI) * 0.07f;
			if ((Object)(object)shopGoldText != (Object)null)
			{
				((Transform)((Graphic)shopGoldText).rectTransform).localScale = Vector3.one * pulse;
			}
			if ((Object)(object)shopDiamondText != (Object)null)
			{
				((Transform)((Graphic)shopDiamondText).rectTransform).localScale = Vector3.one * pulse;
			}
			yield return null;
		}
		SetShopCurrencyText(targetGold, targetDiamonds);
		if ((Object)(object)shopGoldText != (Object)null)
		{
			((Transform)((Graphic)shopGoldText).rectTransform).localScale = Vector3.one;
		}
		if ((Object)(object)shopDiamondText != (Object)null)
		{
			((Transform)((Graphic)shopDiamondText).rectTransform).localScale = Vector3.one;
		}
		shopCurrencyCountRoutine = null;
	}

	private string BuildDailyOfferPurchaseBody(int index, List<OutgameDrawResult> results, string fallbackMessage)
	{
		string text = index switch
		{
			0 => "일일 무료 선물을 받았습니다.", 
			1 => "영웅 카드 x" + outgameProgression.Settings.dailyCardPackDrawCount + "를 구매했습니다.", 
			_ => "프리미엄 카드 x" + outgameProgression.Settings.dailyPremiumPackDrawCount + "를 구매했습니다.", 
		};
		string text2 = BuildDrawPopupSummary(results);
		if (!string.IsNullOrEmpty(text2))
		{
			return text + "\n" + text2;
		}
		return string.IsNullOrWhiteSpace(fallbackMessage) ? text : (text + "\n" + fallbackMessage);
	}

	private static string BuildCurrencyChangeLine(int beforeGold, int beforeDiamonds, int afterGold, int afterDiamonds)
	{
		int num = afterGold - beforeGold;
		int num2 = afterDiamonds - beforeDiamonds;
		string text = string.Empty;
		if (num != 0)
		{
			text = "GOLD " + FormatSignedCurrency(num);
		}
		if (num2 != 0)
		{
			if (!string.IsNullOrEmpty(text))
			{
				text += "  |  ";
			}
			text = text + "DIA " + FormatSignedCurrency(num2);
		}
		return text;
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
		string text = "획득 카드 " + results.Count + "장";
		int num = 0;
		for (int i = 0; i < results.Count; i++)
		{
			OutgameDrawResult outgameDrawResult = results[i];
			if (outgameDrawResult != null && outgameDrawResult.character != null)
			{
				string text2 = (outgameDrawResult.firstAcquisition ? "NEW" : (outgameDrawResult.leveledUp ? "LEVEL UP" : "획득"));
				if (outgameDrawResult.wishlistHit)
				{
					text2 += " / 위시";
				}
				else if (outgameDrawResult.pityTriggered)
				{
					text2 += " / 보장";
				}
				text = text + "\n[" + text2 + "] " + outgameDrawResult.character.displayName + " Lv." + outgameDrawResult.level;
				num++;
				if (num >= 5)
				{
					break;
				}
			}
		}
		if (results.Count > num)
		{
			text = text + "\n외 " + (results.Count - num) + "장";
		}
		return text;
	}

	private void HandleCashBundlePurchase(int index)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)outgameProgression == (Object)null))
		{
			int[] array = new int[3] { 10000, 0, 20000 };
			int[] array2 = new int[3] { 0, 1200, 2000 };
			string[] array3 = new string[3] { "골드 주머니", "다이아 주머니", "성장 꾸러미" };
			string[] array4 = new string[3] { "GradeAndGoodsIcons/goods_icon_gold", "GradeAndGoodsIcons/goods_icon_ruby", "GradeAndGoodsIcons/goods_icon_reward_box" };
			Color[] array5 = (Color[])(object)new Color[3]
			{
				new Color(1f, 0.74f, 0.2f),
				new Color(0.3f, 0.84f, 1f),
				new Color(0.72f, 0.54f, 1f)
			};
			int num = Mathf.Clamp(index, 0, array3.Length - 1);
			if (!outgameProgression.IsTestMode)
			{
				ShowShopPurchaseResultPopup("구매 준비중", array3[num] + " 결제 SDK 연결 후 실제 구매가 진행됩니다.", string.Empty, array4[num], array5[num]);
				return;
			}
			int gold = outgameProgression.Gold;
			int diamonds = outgameProgression.Diamonds;
			outgameProgression.GrantTestShopCurrency(array[num], array2[num]);
			RefreshShop();
			PlayShopCurrencyChange(gold, diamonds);
			ShowShopPurchaseResultPopup("구매 완료", array3[num] + "를 구매했습니다.", BuildCurrencyChangeLine(gold, diamonds, outgameProgression.Gold, outgameProgression.Diamonds), array4[num], array5[num]);
		}
	}

	private void TryPurchaseDailyOffer(int index)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)outgameProgression == (Object)null || drawRevealRoutine != null)
		{
			return;
		}
		int gold = outgameProgression.Gold;
		int diamonds = outgameProgression.Diamonds;
		if (!outgameProgression.TryPurchaseDailyShopOffer(index, out var results, out var message))
		{
			RefreshShop();
			ShowShopPurchaseResultPopup("구매 실패", message, string.Empty, "Icons/icon-main-menu-shop", new Color(1f, 0.48f, 0.42f));
			return;
		}
		RefreshShop();
		PlayShopCurrencyChange(gold, diamonds);
		string iconResourcePath = index switch
		{
			1 => "GradeAndGoodsIcons/goods_icon_reward_box", 
			0 => "GradeAndGoodsIcons/goods_icon_gold", 
			_ => "GradeAndGoodsIcons/goods_icon_ruby", 
		};
		Color accentColor = (Color)(index switch
		{
			1 => new Color(0.38f, 0.9f, 1f), 
			0 => new Color(1f, 0.74f, 0.2f), 
			_ => new Color(0.78f, 0.55f, 1f), 
		});
		ShowShopPurchaseResultPopup("구매 완료", BuildDailyOfferPurchaseBody(index, results, message), BuildCurrencyChangeLine(gold, diamonds, outgameProgression.Gold, outgameProgression.Diamonds), iconResourcePath, accentColor);
		if (results != null && results.Count > 0)
		{
			RuntimeAudioUtility.PlayReroll();
			drawRevealRoutine = ((MonoBehaviour)this).StartCoroutine(RevealDrawResults(results));
		}
	}

	private void TryOpenEarnedChest()
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)outgameProgression == (Object)null) && drawRevealRoutine == null)
		{
			if (!outgameProgression.TryOpenEarnedChest(out var results))
			{
				RefreshShop();
				ShowShopPurchaseResultPopup("개봉 실패", "무료 상자가 없습니다. 전투 보상이나 게이지 보상으로 상자를 채우세요.", string.Empty, "GradeAndGoodsIcons/goods_icon_reward_box", new Color(1f, 0.48f, 0.42f));
				return;
			}
			RuntimeAudioUtility.PlayReroll();
			RefreshShop();
			string text = BuildDrawPopupSummary(results);
			ShowShopPurchaseResultPopup("획득 완료", "무료 영웅 상자 1개를 열었습니다." + (string.IsNullOrEmpty(text) ? string.Empty : ("\n" + text)), string.Empty, "GradeAndGoodsIcons/goods_icon_reward_box", new Color(0.38f, 0.9f, 1f));
			drawRevealRoutine = ((MonoBehaviour)this).StartCoroutine(RevealDrawResults(results));
		}
	}

	private void CycleWishlist()
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)outgameProgression == (Object)null) && drawRevealRoutine == null)
		{
			outgameProgression.CycleWishlist();
			string wishlistDisplayName = outgameProgression.GetWishlistDisplayName();
			RefreshShop();
			ShowShopPurchaseResultPopup("위시 변경 완료", "위시 영웅을 " + wishlistDisplayName + "으로 변경했습니다.\n프리미엄 상자에서 확률 보정되고 20회 안에 확정됩니다.", string.Empty, "Icons/icon-main-menu-collection", new Color(0.78f, 0.55f, 1f));
		}
	}

	private void TryOpenChest(int drawCount)
	{
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)outgameProgression == (Object)null) && drawRevealRoutine == null)
		{
			int gold = outgameProgression.Gold;
			int diamonds = outgameProgression.Diamonds;
			if (!outgameProgression.TryOpenChest(drawCount, out var results))
			{
				RefreshShop();
				ShowShopPurchaseResultPopup("구매 실패", "다이아가 부족합니다.", string.Empty, "GradeAndGoodsIcons/goods_icon_ruby", new Color(1f, 0.48f, 0.42f));
				return;
			}
			RuntimeAudioUtility.PlayReroll();
			RefreshShop();
			PlayShopCurrencyChange(gold, diamonds);
			string text = BuildDrawPopupSummary(results);
			ShowShopPurchaseResultPopup("구매 완료", "영웅 카드 x" + drawCount + "를 구매했습니다." + (string.IsNullOrEmpty(text) ? string.Empty : ("\n" + text)), BuildCurrencyChangeLine(gold, diamonds, outgameProgression.Gold, outgameProgression.Diamonds), "GradeAndGoodsIcons/goods_icon_reward_box", new Color(0.38f, 0.9f, 1f));
			drawRevealRoutine = ((MonoBehaviour)this).StartCoroutine(RevealDrawResults(results));
		}
	}

	private IEnumerator RevealDrawResults(List<OutgameDrawResult> results)
	{
		SetShopDrawButtonsInteractable(interactable: false);
		SetShopResultHint("상자를 여는 중...\n획득 결과는 팝업으로 표시됩니다.");
		float lockSeconds = ((results == null) ? 0.45f : Mathf.Clamp(0.35f + (float)results.Count * 0.018f, 0.45f, 1.2f));
		yield return (object)new WaitForSecondsRealtime(lockSeconds);
		drawRevealRoutine = null;
		SetShopDrawButtonsInteractable(interactable: true);
		RefreshShop();
	}

	private static string BuildBulkDrawSummary(List<OutgameDrawResult> results)
	{
		int[] array = new int[6];
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < results.Count; i++)
		{
			OutgameDrawResult outgameDrawResult = results[i];
			if (outgameDrawResult != null && outgameDrawResult.character != null)
			{
				int num3 = Mathf.Clamp((int)outgameDrawResult.character.grade, 0, array.Length - 1);
				array[num3]++;
				if (outgameDrawResult.firstAcquisition)
				{
					num++;
				}
				if (outgameDrawResult.leveledUp)
				{
					num2++;
				}
			}
		}
		return "영웅 카드 " + results.Count + "장 획득 완료\n일반 " + array[0] + " / 레어 " + array[1] + " / 희귀 " + array[2] + "\n전설 " + array[3] + " / 신화 " + array[4] + " / 초월 " + array[5] + "\nNEW " + num + " / LEVEL UP " + num2 + "\n세부 보유량은 도감에서 확인할 수 있습니다.";
	}

	private void RefreshShop()
	{
		if ((Object)(object)outgameProgression == (Object)null)
		{
			return;
		}
		if (shopCurrencyCountRoutine == null)
		{
			SetShopCurrencyText(outgameProgression.Gold, outgameProgression.Diamonds);
		}
		if ((Object)(object)shopRatesText != (Object)null)
		{
			shopRatesText.text = outgameProgression.BuildRateText();
		}
		if ((Object)(object)shopCollectionText != (Object)null)
		{
			shopCollectionText.text = outgameProgression.BuildCollectionSummary();
		}
		if ((Object)(object)shopModeText != (Object)null)
		{
			shopModeText.text = (outgameProgression.IsTestMode ? "TEST MODE / 전체 영웅 사용 가능" : "SERVICE MODE / 보유 영웅만 출전");
		}
		if ((Object)(object)shopTestDiamondButton != (Object)null)
		{
			((Component)shopTestDiamondButton).gameObject.SetActive(outgameProgression.IsTestMode);
			Text componentInChildren = ((Component)shopTestDiamondButton).GetComponentInChildren<Text>();
			if ((Object)(object)componentInChildren != (Object)null)
			{
				componentInChildren.text = "다이아 +" + outgameProgression.Settings.testDiamondRechargeAmount.ToString("N0");
			}
		}
		if ((Object)(object)shopSingleDrawButton != (Object)null)
		{
			Text componentInChildren2 = ((Component)shopSingleDrawButton).GetComponentInChildren<Text>();
			if ((Object)(object)componentInChildren2 != (Object)null)
			{
				componentInChildren2.text = "1회 뽑기  " + outgameProgression.Settings.singleChestCost + " DIA";
			}
		}
		if ((Object)(object)shopTenDrawButton != (Object)null)
		{
			Text componentInChildren3 = ((Component)shopTenDrawButton).GetComponentInChildren<Text>();
			if ((Object)(object)componentInChildren3 != (Object)null)
			{
				componentInChildren3.text = "10회 뽑기  " + outgameProgression.Settings.tenChestCost + " DIA";
			}
		}
		if ((Object)(object)shopEarnedDrawButton != (Object)null)
		{
			Text componentInChildren4 = ((Component)shopEarnedDrawButton).GetComponentInChildren<Text>();
			if ((Object)(object)componentInChildren4 != (Object)null)
			{
				componentInChildren4.text = "무료 상자 열기  |  보유 " + outgameProgression.EarnedChestKeys + "개  (게이지 " + outgameProgression.EarnedChestProgress + "/" + outgameProgression.EarnedChestProgressTarget + ")";
			}
			((Selectable)shopEarnedDrawButton).interactable = outgameProgression.EarnedChestKeys > 0 && drawRevealRoutine == null;
		}
		if ((Object)(object)shopWishlistButton != (Object)null)
		{
			Text componentInChildren5 = ((Component)shopWishlistButton).GetComponentInChildren<Text>();
			if ((Object)(object)componentInChildren5 != (Object)null)
			{
				componentInChildren5.text = "위시 영웅  |  " + outgameProgression.GetWishlistDisplayName() + "  >";
			}
		}
		if ((Object)(object)shopSingleDrawButton != (Object)null)
		{
			Text componentInChildren6 = ((Component)shopSingleDrawButton).GetComponentInChildren<Text>();
			if ((Object)(object)componentInChildren6 != (Object)null)
			{
				componentInChildren6.text = "프리미엄 1회  " + outgameProgression.Settings.singleChestCost + " DIA";
			}
			((Selectable)shopSingleDrawButton).interactable = outgameProgression.Diamonds >= outgameProgression.Settings.singleChestCost && drawRevealRoutine == null;
		}
		if ((Object)(object)shopTenDrawButton != (Object)null)
		{
			Text componentInChildren7 = ((Component)shopTenDrawButton).GetComponentInChildren<Text>();
			if ((Object)(object)componentInChildren7 != (Object)null)
			{
				componentInChildren7.text = "프리미엄 10회  " + outgameProgression.Settings.tenChestCost + " DIA";
			}
			((Selectable)shopTenDrawButton).interactable = outgameProgression.Diamonds >= outgameProgression.Settings.tenChestCost && drawRevealRoutine == null;
		}
		if ((Object)(object)shopDailyResetText != (Object)null)
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
		if ((Object)(object)shopTestDiamondButton != (Object)null)
		{
			Text componentInChildren8 = ((Component)shopTestDiamondButton).GetComponentInChildren<Text>();
			if ((Object)(object)componentInChildren8 != (Object)null)
			{
				componentInChildren8.text = "GOLD/DIA 테스트 충전";
			}
		}
	}

	private void RefreshDailyOfferButton(int index, string title, string reward, string footer, bool affordable)
	{
		if (index >= 0 && index < shopDailyOfferButtons.Length && !((Object)(object)shopDailyOfferButtons[index] == (Object)null))
		{
			bool flag = (Object)(object)outgameProgression != (Object)null && outgameProgression.IsDailyShopOfferPurchased(index);
			SetButtonLabel(shopDailyOfferButtons[index], flag ? (title + "\n구매 완료\n내일 갱신") : (title + "\n" + reward + "\n" + footer));
			((Selectable)shopDailyOfferButtons[index]).interactable = !flag && affordable && drawRevealRoutine == null;
		}
	}

	private void RefreshChestPackButton(Button button, int drawCount)
	{
		if (!((Object)(object)button == (Object)null) && !((Object)(object)outgameProgression == (Object)null))
		{
			int num = outgameProgression.ResolvePremiumChestCost(drawCount);
			SetButtonLabel(button, drawCount + "개\n" + num.ToString("N0") + " DIA");
			((Selectable)button).interactable = outgameProgression.Diamonds >= num && drawRevealRoutine == null;
		}
	}

	private void RefreshModeUi()
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)outgameProgression == (Object)null)
		{
			return;
		}
		if ((Object)(object)lobbyModeText != (Object)null)
		{
			lobbyModeText.text = (outgameProgression.IsTestMode ? "TEST MODE" : "SERVICE");
			((Graphic)lobbyModeText).color = (outgameProgression.IsTestMode ? new Color(1f, 0.83f, 0.34f) : new Color(0.43f, 1f, 0.8f));
		}
		if ((Object)(object)lobbyModeButton != (Object)null)
		{
			Text componentInChildren = ((Component)lobbyModeButton).GetComponentInChildren<Text>();
			if ((Object)(object)componentInChildren != (Object)null)
			{
				componentInChildren.text = (outgameProgression.IsTestMode ? "서비스 진입" : "테스트 진입");
			}
		}
		if ((Object)(object)lobbyFortuneText != (Object)null)
		{
			lobbyFortuneText.text = DailyFortuneSystem.TodaySummary;
		}
	}

	private void SetGameplayHudVisible(bool visible)
	{
		if ((Object)(object)gameplayHudRoot != (Object)null)
		{
			gameplayHudRoot.SetActive(visible);
		}
	}

	private void HighlightOutgameNav(Button activeButton)
	{
		SetOutgameNavButtonState(hubShopButton, (Object)(object)activeButton == (Object)(object)hubShopButton);
		SetOutgameNavButtonState(hubInventoryButton, (Object)(object)activeButton == (Object)(object)hubInventoryButton);
		SetOutgameNavButtonState(hubLobbyButton, (Object)(object)activeButton == (Object)(object)hubLobbyButton);
		SetOutgameNavButtonState(hubYahtzeeButton, (Object)(object)activeButton == (Object)(object)hubYahtzeeButton);
		SetOutgameNavButtonState(hubRankingButton, (Object)(object)activeButton == (Object)(object)hubRankingButton);
	}

	private void SetOutgameNavButtonState(Button button, bool active)
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)button == (Object)null))
		{
			Image component = ((Component)button).GetComponent<Image>();
			if ((Object)(object)component != (Object)null)
			{
				component.sprite = RuntimeUiSkinUtility.GetRoundedPanelSprite();
				component.type = (Type)1;
				component.preserveAspect = false;
				((Graphic)component).color = (active ? new Color(0.28f, 0.38f, 0.9f, 1f) : new Color(0.94f, 0.97f, 1f, 0.99f));
			}
			if (active)
			{
				((Component)button).transform.SetAsLastSibling();
			}
			Transform val = ((Component)button).transform.Find("NavIcon");
			Image val2 = (((Object)(object)val != (Object)null) ? ((Component)val).GetComponent<Image>() : null);
			Sprite val3 = RollRollUiResource.LoadSprite(ResolveOutgameNavIconPath(button, active));
			if ((Object)(object)val2 != (Object)null && (Object)(object)val3 != (Object)null)
			{
				val2.sprite = val3;
				((Graphic)val2).color = Color.white;
				val2.type = (Type)0;
				val2.preserveAspect = true;
			}
			Text childText = GetChildText(((Component)button).transform, "NavLabel");
			if ((Object)(object)childText != (Object)null)
			{
				ApplyOutgameNavLabelStyle(childText, active);
			}
			AnimateOutgameNavButton(button, active);
		}
	}

	private void AnimateOutgameNavButton(Button button, bool active)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)button == (Object)null) && outgameNavBasePositions.TryGetValue(button, out var value))
		{
			if (outgameNavAnimationRoutines.TryGetValue(button, out var value2) && value2 != null)
			{
				((MonoBehaviour)this).StopCoroutine(value2);
			}
			outgameNavAnimationRoutines[button] = ((MonoBehaviour)this).StartCoroutine(AnimateOutgameNavButtonRoutine(button, value, active));
		}
	}

	private IEnumerator AnimateOutgameNavButtonRoutine(Button button, Vector2 basePosition, bool active)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)button == (Object)null)
		{
			yield break;
		}
		RectTransform rect = ((Component)button).GetComponent<RectTransform>();
		if (!((Object)(object)rect == (Object)null))
		{
			Vector2 startPosition = rect.anchoredPosition;
			Vector2 targetPosition = basePosition + (Vector2)(active ? new Vector2(0f, 32f) : Vector2.zero);
			Vector3 startScale = ((Transform)rect).localScale;
			Vector3 targetScale = (Vector3)(active ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one);
			float elapsed = 0f;
			while (elapsed < 0.18f)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = Mathf.Clamp01(elapsed / 0.18f);
				float eased = Mathf.SmoothStep(0f, 1f, t);
				rect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, eased);
				((Transform)rect).localScale = Vector3.Lerp(startScale, targetScale, eased);
				yield return null;
			}
			rect.anchoredPosition = targetPosition;
			((Transform)rect).localScale = targetScale;
			outgameNavAnimationRoutines.Remove(button);
		}
	}

	private static string ResolveOutgameNavIconPath(Button button, bool active)
	{
		string text = (active ? "-activated" : string.Empty);
		switch (((Object)(object)button != (Object)null) ? ((Object)button).name : string.Empty)
		{
		case "OutgameNavShop":
		case "ShopNavShop":
			return "Icons/icon-main-menu-shop" + text;
		case "OutgameNavInventory":
		case "ShopNavInventory":
			return "Icons/icon-main-menu-collection" + text;
		case "OutgameNavLobby":
		case "ShopNavLobby":
			return "Icons/icon-main-menu-battle" + text;
		case "OutgameNavYahtzee":
		case "ShopNavYahtzee":
			return "Icons/icon-main-menu-roll" + text;
		case "OutgameNavRanking":
		case "ShopNavRanking":
			return "Icons/icon-main-menu-trophy" + text;
		default:
			return "Icons/icon-main-menu-battle" + text;
		}
	}

	private void SetShopDrawButtonsInteractable(bool interactable)
	{
		if ((Object)(object)shopSingleDrawButton != (Object)null)
		{
			((Selectable)shopSingleDrawButton).interactable = interactable;
		}
		if ((Object)(object)shopEarnedDrawButton != (Object)null)
		{
			((Selectable)shopEarnedDrawButton).interactable = interactable && (Object)(object)outgameProgression != (Object)null && outgameProgression.EarnedChestKeys > 0;
		}
		if ((Object)(object)shopWishlistButton != (Object)null)
		{
			((Selectable)shopWishlistButton).interactable = interactable;
		}
		if ((Object)(object)shopTenDrawButton != (Object)null)
		{
			((Selectable)shopTenDrawButton).interactable = interactable;
		}
		if ((Object)(object)shopFiftyDrawButton != (Object)null)
		{
			((Selectable)shopFiftyDrawButton).interactable = interactable;
		}
		if ((Object)(object)shopHundredDrawButton != (Object)null)
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
		if ((Object)(object)loadoutOverlay != (Object)null)
		{
			loadoutOverlay.SetActive(true);
		}
	}

	private void HideLoadout()
	{
		if ((Object)(object)loadoutOverlay != (Object)null)
		{
			loadoutOverlay.SetActive(false);
		}
		if ((Object)(object)lobbyOverlay != (Object)null && lobbyOverlay.activeSelf)
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
		if ((Object)(object)characterCollectionUI != (Object)null && characterCollectionUI.IsOpen)
		{
			characterCollectionUI.Close();
		}
		ShowLobby();
		if ((Object)(object)outgamePlaceholderOverlay != (Object)null)
		{
			Text childText = GetChildText(outgamePlaceholderOverlay.transform, "PlaceholderTitle");
			if ((Object)(object)childText != (Object)null)
			{
				childText.text = title;
			}
			Text childText2 = GetChildText(outgamePlaceholderOverlay.transform, "PlaceholderBody");
			if ((Object)(object)childText2 != (Object)null)
			{
				childText2.text = body;
			}
			outgamePlaceholderOverlay.SetActive(true);
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
		if ((Object)(object)characterCollectionUI != (Object)null && characterCollectionUI.IsOpen)
		{
			characterCollectionUI.Close();
		}
		ShowLobby();
		RefreshSeasonRanking();
		if ((Object)(object)seasonRankingOverlay != (Object)null)
		{
			seasonRankingOverlay.SetActive(true);
		}
		HighlightOutgameNav(hubRankingButton);
	}

	private void RefreshSeasonRanking()
	{
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Unknown result type (might be due to invalid IL or missing references)
		int score = (((Object)(object)outgameProgression != (Object)null) ? outgameProgression.WeeklyBossScore : 0);
		int num = ((!((Object)(object)outgameProgression != (Object)null)) ? 1 : Mathf.Max(1, outgameProgression.CurrentSeasonId));
		List<RankingEntry> list = new List<RankingEntry>
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
			new RankingEntry("레드X", score, isPlayer: true)
		};
		list.Sort(delegate(RankingEntry left, RankingEntry right)
		{
			int num8 = right.Score.CompareTo(left.Score);
			return (num8 != 0) ? num8 : string.CompareOrdinal(left.Name, right.Name);
		});
		if ((Object)(object)rankingSeasonText != (Object)null)
		{
			rankingSeasonText.text = "SEASON " + num + " · 주간 보스 리그";
		}
		for (int num2 = 0; num2 < rankingTopNameTexts.Length; num2++)
		{
			RankingEntry rankingEntry = list[num2];
			rankingTopNameTexts[num2].text = (rankingEntry.IsPlayer ? (rankingEntry.Name + " (나)") : rankingEntry.Name);
			rankingTopScoreTexts[num2].text = rankingEntry.Score.ToString("N0");
			if ((Object)(object)rankingTopCardPanels[num2] != (Object)null)
			{
				((Graphic)rankingTopCardPanels[num2]).color = (Color)(rankingEntry.IsPlayer ? new Color(0.72f, 1f, 1f, 1f) : Color.white);
			}
		}
		for (int num3 = 0; num3 < rankingRowPanels.Length; num3++)
		{
			int num4 = num3 + 3;
			RankingEntry rankingEntry2 = list[num4];
			rankingRowRankTexts[num3].text = (num4 + 1).ToString();
			rankingRowNameTexts[num3].text = (rankingEntry2.IsPlayer ? (rankingEntry2.Name + "  ·  나") : rankingEntry2.Name);
			ApplyRankingRowPlayerNameStyle(rankingRowNameTexts[num3]);
			rankingRowScoreTexts[num3].text = rankingEntry2.Score.ToString("N0");
			((Graphic)rankingRowPanels[num3]).color = Color32.op_Implicit(new Color32((byte)133, (byte)131, (byte)164, byte.MaxValue));
		}
		int num5 = list.FindIndex((RankingEntry entry) => entry.IsPlayer) + 1;
		if ((Object)(object)rankingPlayerSummaryText != (Object)null)
		{
			rankingPlayerSummaryText.text = "내 순위 " + num5 + "위  |  레드X  " + score.ToString("N0") + "점";
		}
		if ((Object)(object)rankingPlayerProgressText != (Object)null)
		{
			int num6 = (((Object)(object)outgameProgression != (Object)null) ? outgameProgression.WeeklyBestRunScore : 0);
			int num7 = (((Object)(object)outgameProgression != (Object)null) ? outgameProgression.WeeklyBossKills : 0);
			rankingPlayerProgressText.text = "최고 런 " + num6.ToString("N0") + "점 · 보스 처치 " + num7 + "회";
		}
	}

	private void CloseSeasonRanking()
	{
		HideSeasonRanking();
		ShowLobby();
	}

	private void HideSeasonRanking()
	{
		if ((Object)(object)seasonRankingOverlay != (Object)null)
		{
			seasonRankingOverlay.SetActive(false);
		}
	}

	private void HideOutgamePlaceholder()
	{
		if ((Object)(object)outgamePlaceholderOverlay != (Object)null)
		{
			outgamePlaceholderOverlay.SetActive(false);
		}
	}

	private void ShowMatchmaking()
	{
		SetGameplayHudVisible(visible: false);
		HideExitConfirm();
		if ((Object)(object)matchmakingOverlay != (Object)null)
		{
			matchmakingOverlay.SetActive(true);
		}
		if ((Object)(object)queueTimerText != (Object)null)
		{
			queueTimerText.text = "00.00";
		}
		if ((Object)(object)queueStatusText != (Object)null)
		{
			queueStatusText.text = "라운드 전장을 준비하는 중...";
		}
		RuntimeAudioUtility.PlayMatching();
	}

	private void HideMatchmaking()
	{
		if ((Object)(object)matchmakingOverlay != (Object)null)
		{
			matchmakingOverlay.SetActive(false);
		}
	}

	private void ShowResult(bool victory, int round)
	{
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_069f: Unknown result type (might be due to invalid IL or missing references)
		//IL_068e: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b9: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)resultOverlay == (Object)null)
		{
			return;
		}
		HideExitConfirm();
		resultOverlay.SetActive(true);
		for (int i = 0; i < resultVictoryDecorations.Count; i++)
		{
			if ((Object)(object)resultVictoryDecorations[i] != (Object)null)
			{
				resultVictoryDecorations[i].SetActive(victory);
			}
		}
		if ((Object)(object)resultRibbonImage != (Object)null)
		{
			((Graphic)resultRibbonImage).color = (victory ? new Color(0.17f, 0.42f, 1f, 0.92f) : new Color(0.7f, 0.18f, 0.22f, 0.92f));
		}
		Color requestedColor = (victory ? new Color(1f, 0.84f, 0.18f) : new Color(1f, 0.45f, 0.45f));
		if ((Object)(object)resultTitleText != (Object)null)
		{
			resultTitleText.text = (victory ? "승리" : "패배");
			RuntimeUiSkinUtility.ApplyReadableTextColor(resultTitleText, requestedColor, uiSkin);
		}
		if ((Object)(object)resultSummaryText != (Object)null)
		{
			resultSummaryText.text = (victory ? ("라운드 " + round + " 클리어") : ("라운드 " + round + " 에서 패배"));
		}
		if ((Object)(object)resultMetaText != (Object)null)
		{
			resultMetaText.text = (victory ? "연속 클리어 +1  |  다음 라운드 준비 완료" : "덱을 다시 정비하고 재도전할 수 있습니다.");
		}
		if ((Object)(object)resultScoreText != (Object)null)
		{
			resultScoreText.text = (victory ? "RUN SCORE A / 000점" : "RUN SCORE C / 000점");
		}
		if ((Object)(object)resultRecapText != (Object)null)
		{
			resultRecapText.text = "딜러 기록 없음  |  시너지 없음\n타일 기여 없음  |  초반 1~10R 계측 대기";
		}
		if ((Object)(object)resultNextText != (Object)null)
		{
			resultNextText.text = (victory ? "다음 행동\n도감 강화: 주력 카드 성장 확인\n상점 뽑기: 부족한 등급 보충\n다음 보스 대비: 보스 타일과 딜러 유지" : "다음 행동\n도감 강화: 약한 주력 카드 보강\n상점 뽑기: 부족한 등급 보충\n다음 보스 대비: 실패 원인 재정비");
		}
		if ((Object)(object)gameController != (Object)null)
		{
			if ((Object)(object)resultTitleText != (Object)null)
			{
				resultTitleText.text = (victory ? "승리" : "패배");
			}
			if ((Object)(object)resultSummaryText != (Object)null)
			{
				resultSummaryText.text = gameController.RunNextGoalHeadline;
			}
			if ((Object)(object)resultMetaText != (Object)null)
			{
				RectTransform rectTransform = ((Graphic)resultMetaText).rectTransform;
				if ((Object)(object)rectTransform != (Object)null)
				{
					rectTransform.sizeDelta = new Vector2(700f, 40f);
				}
				resultMetaText.fontSize = 22;
				resultMetaText.alignment = (TextAnchor)4;
				resultMetaText.text = (victory ? ("이번 결과: 라운드 " + round + " 클리어") : ("이번 결과: 라운드 " + round + " 패배"));
			}
			if ((Object)(object)resultScoreText != (Object)null)
			{
				resultScoreText.text = "RUN SCORE " + gameController.RunPerformanceGrade + " / " + gameController.RunPerformanceScore + "점";
			}
			if ((Object)(object)resultRecapText != (Object)null)
			{
				resultRecapText.text = gameController.RunResultFocusSummary;
			}
			if ((Object)(object)resultNextText != (Object)null)
			{
				resultNextText.text = gameController.RunResultNextCompactSummary;
			}
		}
		ApplyReadableResultTextLayout();
		int num = ((victory && (Object)(object)gameController != (Object)null && gameController.LastRoundClearGoldReward > 0) ? gameController.LastRoundClearGoldReward : (victory ? (110 + round * 18) : Mathf.Max(20, 40 + round * 6)));
		int num2 = (victory ? (6 + Mathf.Max(1, round / 2)) : (((Object)(object)gameController != (Object)null) ? gameController.EarnedGrowthCurrency : Mathf.Max(2, round / 3 + 2)));
		int amount = (((Object)(object)outgameProgression != (Object)null) ? outgameProgression.ResolveBattleDiamondReward(num2) : num2);
		if (!resultRewardGranted && (Object)(object)outgameProgression != (Object)null)
		{
			outgameProgression.AddDiamonds(amount);
			if ((Object)(object)gameController != (Object)null)
			{
				outgameProgression.RecordSeasonRun(gameController.RunPerformanceScore, gameController.RunBossScore, gameController.RunBossKillCount, gameController.RunMvpName, round, victory);
			}
			resultRewardGranted = true;
		}
		if ((Object)(object)resultMetaText != (Object)null && (Object)(object)gameController != (Object)null)
		{
			string text = (victory ? ("이번 결과: 라운드 " + round + " 클리어") : ("이번 결과: 라운드 " + round + " 패배"));
			if ((Object)(object)outgameProgression != (Object)null && !string.IsNullOrWhiteSpace(outgameProgression.LastSeasonRewardSummary))
			{
				text = text + "  |  " + outgameProgression.LastSeasonRewardSummary;
			}
			resultMetaText.text = text;
		}
		if ((Object)(object)resultRewardGoldText != (Object)null)
		{
			resultRewardGoldText.text = "+" + num;
		}
		if ((Object)(object)resultRewardCoreText != (Object)null)
		{
			resultRewardCoreText.text = "+" + amount;
		}
		if ((Object)(object)resultContinueButton != (Object)null)
		{
			RectTransform component = ((Component)resultContinueButton).GetComponent<RectTransform>();
			if ((Object)(object)component != (Object)null)
			{
				component.anchoredPosition = (victory ? new Vector2(0f, 75f) : new Vector2(190f, 75f));
				component.sizeDelta = (victory ? new Vector2(340f, 100f) : new Vector2(220f, 100f));
			}
			SetButtonLabel(resultContinueButton, victory ? "계속하기" : "재정비");
		}
		if ((Object)(object)resultRetryButton != (Object)null)
		{
			((Component)resultRetryButton).gameObject.SetActive(!victory);
			if (!victory)
			{
				SetButtonLabel(resultRetryButton, "새 판 다시하기");
			}
		}
	}

	private void HideResult()
	{
		if ((Object)(object)resultOverlay != (Object)null)
		{
			resultOverlay.SetActive(false);
		}
	}

	private void ContinueFromResult()
	{
		bool flag = !defeatPresented;
		HideResult();
		if (flag)
		{
			gameController?.ReleasePostRoundChoiceFlow();
		}
		if ((Object)(object)gameController != (Object)null && !gameController.IsRoundRunning && defeatPresented)
		{
			defeatPresented = false;
			gameController.ResetRunForRetry();
			ShowLobby();
		}
	}

	private void RetryFromResult()
	{
		bool flag = defeatPresented;
		defeatPresented = false;
		HideResult();
		if (flag && (Object)(object)gameController != (Object)null && !gameController.IsRoundRunning)
		{
			gameController.ResetRunForRetry();
		}
		HandleEnterPreparationPressed();
	}

	private GameObject CreateOverlayRoot(Transform parent, string name, Color blockerColor)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		Image val2 = val.AddComponent<Image>();
		((Graphic)val2).color = blockerColor;
		((Graphic)val2).raycastTarget = true;
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		return val;
	}

	private Image CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, bool rounded, bool shadow)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		Image val2 = val.AddComponent<Image>();
		((Graphic)val2).color = color;
		((Graphic)val2).raycastTarget = false;
		RuntimeUiSkinUtility.ApplyImageSkin(val2, uiSkin, name, isButton: false, rounded);
		RollRollUiResource.TryApplyElementSprite(val2, name, isButton: false, rounded);
		RectTransform rectTransform = ((Graphic)val2).rectTransform;
		rectTransform.anchorMin = anchorMin;
		rectTransform.anchorMax = anchorMax;
		rectTransform.pivot = pivot;
		rectTransform.anchoredPosition = anchoredPosition;
		rectTransform.sizeDelta = size;
		if (shadow)
		{
			Shadow val3 = val.AddComponent<Shadow>();
			val3.effectColor = new Color(0f, 0f, 0f, 0.35f);
			val3.effectDistance = new Vector2(0f, -7f);
		}
		return val2;
	}

	private Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color backgroundColor, UnityAction onClick, int fontSize)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		Image val2 = val.AddComponent<Image>();
		((Graphic)val2).color = backgroundColor;
		RuntimeUiSkinUtility.ApplyImageSkin(val2, uiSkin, name, isButton: true, rounded: true);
		RollRollUiResource.TryApplyElementSprite(val2, name, isButton: true, rounded: true);
		((Graphic)val2).raycastTarget = true;
		Shadow val3 = val.AddComponent<Shadow>();
		val3.effectColor = new Color(0f, 0f, 0f, 0.34f);
		val3.effectDistance = new Vector2(0f, -6f);
		Button val4 = val.AddComponent<Button>();
		AddButtonListener(val4, onClick);
		RectTransform component = ((Component)val4).GetComponent<RectTransform>();
		component.anchorMin = anchorMin;
		component.anchorMax = anchorMax;
		component.pivot = pivot;
		component.anchoredPosition = anchoredPosition;
		component.sizeDelta = size;
		CreateText(val.transform, "Label", label, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, fontSize, (TextAnchor)4, bold: true);
		return val4;
	}

	private Image CreateShopArtwork(Transform parent, string name, string resourcePath, Vector2 anchoredPosition, Vector2 size, Color color, Vector2 anchor)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		Image val2 = val.AddComponent<Image>();
		((Graphic)val2).color = color;
		((Graphic)val2).raycastTarget = false;
		val2.preserveAspect = true;
		val2.sprite = RollRollUiResource.LoadSprite(resourcePath);
		RectTransform rectTransform = ((Graphic)val2).rectTransform;
		rectTransform.anchorMin = anchor;
		rectTransform.anchorMax = anchor;
		rectTransform.pivot = anchor;
		rectTransform.anchoredPosition = anchoredPosition;
		rectTransform.sizeDelta = size;
		return val2;
	}

	private void DecorateShopProductCard(Button button, string iconResourcePath, Color accent, bool compact)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)button == (Object)null))
		{
			Image component = ((Component)button).GetComponent<Image>();
			if ((Object)(object)component != (Object)null)
			{
				component.sprite = RuntimeUiSkinUtility.GetRoundedPanelSprite();
				component.type = (Type)1;
				component.preserveAspect = false;
				((Graphic)component).color = Color.Lerp(new Color(0.18f, 0.24f, 0.52f, 1f), accent, 0.58f);
			}
			float num = (compact ? 70f : 96f);
			float num2 = (compact ? 30f : 42f);
			CreateShopArtwork(((Component)button).transform, "ShopProductIcon", iconResourcePath, new Vector2(0f, num2), new Vector2(num, num), Color.white, new Vector2(0.5f, 0.5f));
			Text childText = GetChildText(((Component)button).transform, "Label");
			if ((Object)(object)childText != (Object)null)
			{
				RectTransform rectTransform = ((Graphic)childText).rectTransform;
				rectTransform.anchorMin = new Vector2(0f, 0f);
				rectTransform.anchorMax = new Vector2(1f, 0f);
				rectTransform.pivot = new Vector2(0.5f, 0f);
				rectTransform.anchoredPosition = new Vector2(0f, compact ? 5f : 8f);
				rectTransform.sizeDelta = new Vector2(-20f, compact ? 56f : 78f);
				childText.fontSize = (compact ? 17 : 18);
				childText.resizeTextForBestFit = true;
				childText.resizeTextMinSize = (compact ? 13 : 14);
				childText.resizeTextMaxSize = (compact ? 17 : 18);
				((Component)childText).transform.SetAsLastSibling();
			}
			Outline val = ((Component)button).gameObject.AddComponent<Outline>();
			((Shadow)val).effectColor = new Color(accent.r, accent.g, accent.b, 0.72f);
			((Shadow)val).effectDistance = new Vector2(2f, -2f);
		}
	}

	private static void AddButtonListener(Button button, UnityAction onClick)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		if (!((Object)(object)button == (Object)null))
		{
			((UnityEvent)button.onClick).AddListener(new UnityAction(RuntimeAudioUtility.PlayButton));
			if (onClick != null)
			{
				((UnityEvent)button.onClick).AddListener(onClick);
			}
		}
	}

	private static void SetButtonLabel(Button button, string label)
	{
		if (!((Object)(object)button == (Object)null))
		{
			Text componentInChildren = ((Component)button).GetComponentInChildren<Text>();
			if ((Object)(object)componentInChildren != (Object)null)
			{
				componentInChildren.text = label;
			}
		}
	}

	private Text CreateText(Transform parent, string name, string value, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, bool bold)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		Text val2 = val.AddComponent<Text>();
		val2.font = font;
		val2.text = RuntimeKoreanTextUtility.Clean(name, value);
		((Graphic)val2).color = RuntimeUiSkinUtility.ResolveReadableTextColor(parent, color, uiSkin);
		val2.fontSize = fontSize;
		val2.alignment = alignment;
		val2.fontStyle = (FontStyle)(bold ? 1 : 0);
		((Graphic)val2).raycastTarget = false;
		RectTransform component = ((Component)val2).GetComponent<RectTransform>();
		component.anchorMin = anchorMin;
		component.anchorMax = anchorMax;
		component.pivot = pivot;
		component.anchoredPosition = anchoredPosition;
		component.sizeDelta = size;
		Shadow val3 = val.AddComponent<Shadow>();
		val3.effectColor = new Color(0f, 0f, 0f, 0.38f);
		val3.effectDistance = new Vector2(2f, -2f);
		return val2;
	}

	private Text GetChildText(Transform parent, string childName)
	{
		if ((Object)(object)parent == (Object)null || string.IsNullOrWhiteSpace(childName))
		{
			return null;
		}
		Transform val = parent.Find(childName);
		if ((Object)(object)val != (Object)null)
		{
			return ((Component)val).GetComponent<Text>();
		}
		Text[] componentsInChildren = ((Component)parent).GetComponentsInChildren<Text>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if ((Object)(object)componentsInChildren[i] != (Object)null && ((Object)componentsInChildren[i]).name == childName)
			{
				return componentsInChildren[i];
			}
		}
		return null;
	}

	private void MoveRectInto(Transform newParent, Transform oldParent, string childName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)newParent == (Object)null) && !((Object)(object)oldParent == (Object)null) && !string.IsNullOrWhiteSpace(childName))
		{
			Transform val = oldParent.Find(childName);
			if (!((Object)(object)val == (Object)null))
			{
				val.SetParent(newParent, false);
				RectTransform component = ((Component)val).GetComponent<RectTransform>();
				SetRect(component, anchorMin, anchorMax, pivot, anchoredPosition, size);
			}
		}
	}

	private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)rect == (Object)null))
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
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)primaryText != (Object)null)
		{
			((Graphic)primaryText).color = new Color(0.98f, 0.99f, 1f, 1f);
			AddReadableOutline(primaryText);
		}
		if ((Object)(object)secondaryText != (Object)null)
		{
			((Graphic)secondaryText).color = new Color(0.82f, 0.92f, 1f, 1f);
			AddReadableOutline(secondaryText);
		}
	}

	private void ApplyCharacterPortrait(List<Image> portraitImages, int index, CharacterDefinition definition)
	{
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		if (portraitImages == null || index < 0 || index >= portraitImages.Count)
		{
			return;
		}
		Image val = portraitImages[index];
		if (!((Object)(object)val == (Object)null))
		{
			((Component)val).gameObject.SetActive(true);
			((Behaviour)val).enabled = true;
			((Graphic)val).raycastTarget = false;
			Sprite val2 = RollRollUiResource.ResolveCharacterSprite(definition);
			if ((Object)(object)val2 != (Object)null && definition != null)
			{
				val.sprite = val2;
				val.type = (Type)0;
				val.preserveAspect = true;
				((Graphic)val).color = Color.white;
			}
			else
			{
				RollRollUiResource.TryApplyElementSprite(val, "Portrait", isButton: false, rounded: true);
				((Graphic)val).color = new Color(0.8f, 0.84f, 0.95f, 1f);
			}
		}
	}

	private static void ApplyOutgameNavLabelStyle(Text label, bool active)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)label == (Object)null))
		{
			((Graphic)label).color = (active ? Color.white : Color32.op_Implicit(new Color32((byte)26, (byte)34, (byte)75, byte.MaxValue)));
			Shadow component = ((Component)label).GetComponent<Shadow>();
			if ((Object)(object)component != (Object)null)
			{
				component.effectColor = (active ? new Color(0f, 0f, 0f, 0.72f) : new Color(1f, 1f, 1f, 0.92f));
				component.effectDistance = new Vector2(1.2f, -1.2f);
			}
			Outline val = ((Component)label).GetComponent<Outline>();
			if ((Object)(object)val == (Object)null)
			{
				val = ((Component)label).gameObject.AddComponent<Outline>();
			}
			((Shadow)val).effectColor = (active ? new Color(0f, 0f, 0f, 0.88f) : new Color(1f, 1f, 1f, 0.96f));
			((Shadow)val).effectDistance = new Vector2(1.1f, -1.1f);
		}
	}

	private static void ApplyTopRankingNameStyle(Text text)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)text == (Object)null))
		{
			((Graphic)text).color = Color.white;
			Shadow component = ((Component)text).GetComponent<Shadow>();
			if ((Object)(object)component != (Object)null)
			{
				component.effectColor = Color.black;
				component.effectDistance = new Vector2(1.5f, -1.5f);
			}
			Outline val = ((Component)text).GetComponent<Outline>();
			if ((Object)(object)val == (Object)null)
			{
				val = ((Component)text).gameObject.AddComponent<Outline>();
			}
			((Shadow)val).effectColor = Color.black;
			((Shadow)val).effectDistance = new Vector2(1.2f, -1.2f);
		}
	}

	private static void ApplyRankingRowPlayerNameStyle(Text text)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)text == (Object)null))
		{
			((Graphic)text).color = Color.white;
			Shadow val = ((Component)text).GetComponent<Shadow>();
			if ((Object)(object)val == (Object)null)
			{
				val = ((Component)text).gameObject.AddComponent<Shadow>();
			}
			val.effectColor = Color.black;
			val.effectDistance = new Vector2(2f, -2f);
			Outline val2 = ((Component)text).GetComponent<Outline>();
			if ((Object)(object)val2 == (Object)null)
			{
				val2 = ((Component)text).gameObject.AddComponent<Outline>();
			}
			((Shadow)val2).effectColor = Color.black;
			((Shadow)val2).effectDistance = new Vector2(1.2f, -1.2f);
		}
	}

	private void AddReadableOutline(Text text)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)text == (Object)null))
		{
			Outline val = ((Component)text).GetComponent<Outline>();
			if ((Object)(object)val == (Object)null)
			{
				val = ((Component)text).gameObject.AddComponent<Outline>();
			}
			((Shadow)val).effectColor = new Color(0f, 0f, 0f, 0.76f);
			((Shadow)val).effectDistance = new Vector2(1.5f, -1.5f);
		}
	}

	private Sprite GetRoundedSprite()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)roundedSprite != (Object)null)
		{
			return roundedSprite;
		}
		int num = 64;
		float num2 = 18f;
		Texture2D val = new Texture2D(num, num, (TextureFormat)5, false);
		((Texture)val).wrapMode = (TextureWrapMode)1;
		Color[] array = (Color[])(object)new Color[num * num];
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				float num3 = Mathf.Clamp((float)j, num2, (float)num - num2 - 1f);
				float num4 = Mathf.Clamp((float)i, num2, (float)num - num2 - 1f);
				float num5 = Vector2.Distance(new Vector2((float)j, (float)i), new Vector2(num3, num4));
				float num6 = Mathf.Clamp01(num2 + 0.5f - num5);
				array[i * num + j] = new Color(1f, 1f, 1f, num6);
			}
		}
		val.SetPixels(array);
		val.Apply();
		roundedSprite = Sprite.Create(val, new Rect(0f, 0f, (float)num, (float)num), new Vector2(0.5f, 0.5f), 100f, 0u, (SpriteMeshType)0, new Vector4(num2, num2, num2, num2));
		return roundedSprite;
	}

	private string GetGradeName(CharacterGrade grade)
	{
		return CharacterGradeUtility.GetDisplayName(grade);
	}

	private string BuildCardLevelLabel(CharacterDefinition definition)
	{
		if (definition == null || (Object)(object)outgameProgression == (Object)null)
		{
			return "미획득";
		}
		int displayCardLevel = outgameProgression.GetDisplayCardLevel(definition.id);
		if (displayCardLevel > 0)
		{
			return "Lv." + displayCardLevel;
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
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		return CharacterGradeUtility.GetColor(grade, fallback);
	}
}
