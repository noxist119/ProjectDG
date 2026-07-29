using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame;

public class SimpleGameHUD : MonoBehaviour
{
	[SerializeField]
	private DefenseGameController gameController;

	[SerializeField]
	private Text goldText;

	[SerializeField]
	private Text lifeText;

	[SerializeField]
	private Text roundText;

	[SerializeField]
	private Text boardText;

	[SerializeField]
	private Text contentText;

	[SerializeField]
	private Text hintText;

	[SerializeField]
	private Text mergeResultText;

	[SerializeField]
	private Text mergeCelebrationText;

	[SerializeField]
	private Text mergeCelebrationSubText;

	[SerializeField]
	private Text countdownText;

	[SerializeField]
	private Text roundBannerText;

	[SerializeField]
	private GameObject bossWarningPanel;

	[SerializeField]
	private CanvasGroup bossWarningCanvasGroup;

	[SerializeField]
	private Text bossWarningTitleText;

	[SerializeField]
	private Text bossWarningSubText;

	[SerializeField]
	private DefenseBoardManager boardManager;

	[Header("Casual HUD")]
	[SerializeField]
	private Text playerNameText;

	[SerializeField]
	private Text rankText;

	[SerializeField]
	private Text stateText;

	[SerializeField]
	private Text battleButtonText;

	[SerializeField]
	private Text summonButtonText;

	[SerializeField]
	private Image luckySummonProgressBadge;

	[SerializeField]
	private Text summonCostText;

	[SerializeField]
	private Text luckySummonProgressText;

	[SerializeField]
	private Text deckSummaryText;

	[SerializeField]
	private Text capacityText;

	[SerializeField]
	private Text normalMergeText;

	[SerializeField]
	private Text rareMergeText;

	[SerializeField]
	private Text epicMergeText;

	[SerializeField]
	private Text legendaryMergeText;

	[SerializeField]
	private Text mythicMergeText;

	[SerializeField]
	private Text transcendentMergeText;

	[SerializeField]
	private Text ultimateRecipeHudText;

	[SerializeField]
	private Text bossRoundHudText;

	[SerializeField]
	private Text synergyInsightText;

	[SerializeField]
	private Text recipeInsightText;

	[SerializeField]
	private Text tileInsightText;

	[SerializeField]
	private Text topDamageInsightText;

	[SerializeField]
	private Text earlyRunInsightText;

	[SerializeField]
	private Text fateGaugeText;

	[SerializeField]
	private Image fateGaugeFill;

	[SerializeField]
	private Text fateDebtText;

	[SerializeField]
	private Text fateCostBenefitText;

	[SerializeField]
	private Button fateGradeLockButton;

	[SerializeField]
	private Text fateGradeLockButtonText;

	[SerializeField]
	private Button fateNormalBanButton;

	[SerializeField]
	private Text fateNormalBanButtonText;

	[SerializeField]
	private Button fateForceShopButton;

	[SerializeField]
	private Text fateForceShopButtonText;

	[SerializeField]
	private Button fateSurvivalButton;

	[SerializeField]
	private Text fateSurvivalButtonText;

	[SerializeField]
	private GameObject fatePanelRoot;

	[SerializeField]
	private CanvasGroup fatePanelCanvasGroup;

	[SerializeField]
	private Button fatePanelReopenButton;

	[SerializeField]
	private Text fatePanelReopenButtonText;

	[SerializeField]
	private Image roundProgressFill;

	[SerializeField]
	private Image lifeProgressFill;

	[SerializeField]
	private Button battleButton;

	[SerializeField]
	private Button summonButton;

	private Image summonButtonImage;

	[Header("Unit Sell UI")]
	[SerializeField]
	private GameObject unitSellPanel;

	[SerializeField]
	private Text unitSellTitleText;

	[SerializeField]
	private Text unitSellDetailText;

	[SerializeField]
	private Text unitSellButtonText;

	[SerializeField]
	private Button unitSellButton;

	[SerializeField]
	private string hintMessage = "Space Round | S Summon | 1-5 Merge";

	[SerializeField]
	private float mergeBannerTimer;

	[SerializeField]
	private string mergeBannerMessage = string.Empty;

	[SerializeField]
	private float mergeCelebrationTimer;

	[SerializeField]
	private float roundBannerTimer;

	[SerializeField]
	private float bossWarningTimer;

	private const float MergeBannerDuration = 2f;

	private const float MergeCelebrationDuration = 0.8f;

	private const float BossWarningDuration = 3.4f;

	private const float OpeningTutorialDuration = 10f;

	private const float OpeningTutorialStageDuration = 2.5f;

	private const float DefeatCinematicDuration = 5f;

	private const float DefeatCinematicFadeOutDuration = 0.25f;

	private const float FatePanelClosedYOffset = 226f;

	private const float FatePanelSlideSpeed = 13f;

	private const float FatePanelFadeSpeed = 6f;

	private static readonly Color FateEntryIdleColor = new Color(0.4f, 0.21f, 0.85f, 0.98f);

	private static readonly Color FateEntryCrisisColor = new Color(0.91f, 0.3f, 0.36f, 0.98f);

	private static readonly Color FateEntryTextColor = new Color(1f, 0.98f, 0.94f, 1f);

	private static readonly Color FateEntryOutlineColor = new Color(1f, 0.78f, 0.34f, 0.94f);

	private static readonly Color FateEntryCrisisOutlineColor = new Color(1f, 0.88f, 0.54f, 1f);

	[Header("Opening Tutorial")]
	[SerializeField]
	private bool enableOpeningTutorial = true;

	private float openingTutorialStartTime = -1f;

	private int openingTutorialStage = -1;

	private bool openingTutorialCompleted;

	private Graphic openingTutorialGraphic;

	private Color openingTutorialOriginalColor;

	private RectTransform openingTutorialRect;

	private Vector3 openingTutorialOriginalScale = Vector3.one;

	private DefenderUnit selectedUnit;

	private DefenderUnit pendingSellConfirmUnit;

	private float pendingSellConfirmExpireTime;

	private bool fateSurvivalVisualInitialized;

	private Vector3 fateSurvivalBaseScale = Vector3.one;

	private Color fateSurvivalBaseColor = Color.white;

	private RectTransform fatePanelRect;

	private Vector2 fatePanelOpenPosition;

	private Vector2 fatePanelClosedPosition;

	private bool fatePanelMotionInitialized;

	private bool fateEntryButtonEmphasisActive;

	private bool fateEntryButtonVisualInitialized;

	private Vector3 fateEntryButtonBaseScale = Vector3.one;

	private Color fateEntryButtonBaseColor = Color.white;

	private Outline fateEntryButtonOutline;

	private bool fatePanelTargetOpen = true;

	private bool fatePanelVisible = true;

	private GameObject fateChoiceBackdrop;

	private CanvasGroup fateChoiceBackdropCanvasGroup;

	private GameObject defeatCinematicPanel;

	private CanvasGroup defeatCinematicCanvasGroup;

	private Text defeatCinematicTitleText;

	private Text defeatCinematicSubtitleText;

	private Text defeatCinematicDetailText;

	private float defeatCinematicTimer;

	private bool defeatCinematicActive;

	private Button ultimateMergeButton;

	private Image[] ultimateReadyLines;

	private Image ultimateReadyBadge;

	private Text ultimateReadyBadgeText;

	private Vector3 ultimateMergeBaseScale = Vector3.one;

	private int ultimateReadyCount;

	private int previousUltimateReadyCount;

	private bool ultimateReadyStateInitialized;

	public void Configure(DefenseGameController controller, Text gold, Text lifeLabel, Text round, Text board, Text content, Text hint, Text mergeResult, Text mergeCelebration, Text mergeCelebrationSub, Text countdown, Text roundBanner, string overrideHint = null, Text playerName = null, Text rank = null, Text state = null, Text battleLabel = null, Text summonLabel = null, Text summonCostLabel = null, Text deckSummary = null, Text capacity = null, Text normalMerge = null, Text rareMerge = null, Text epicMerge = null, Text legendaryMerge = null, Text mythicMerge = null, Text transcendentMerge = null, Text ultimateRecipeHud = null, Text bossRoundHud = null, Text synergyInsight = null, Text recipeInsight = null, Text tileInsight = null, Text topDamageInsight = null, Text earlyRunInsight = null, Text fateGauge = null, Image fateGaugeBar = null, Text fateDebt = null, Text fateCostBenefit = null, Button fateGradeLock = null, Text fateGradeLockLabel = null, Button fateNormalBan = null, Text fateNormalBanLabel = null, Button fateForceShop = null, Text fateForceShopLabel = null, Button fateSurvival = null, Text fateSurvivalLabel = null, GameObject fatePanel = null, CanvasGroup fatePanelGroup = null, Button fatePanelReopen = null, Text fatePanelReopenLabel = null, Image progressFill = null, Button battle = null, Button summon = null, GameObject bossWarning = null, CanvasGroup bossWarningGroup = null, Text bossWarningTitle = null, Text bossWarningSub = null, DefenseBoardManager boardSystem = null, GameObject sellPanel = null, Text sellTitle = null, Text sellDetail = null, Button sellButton = null, Text sellButtonLabel = null, Image lifeProgress = null, Text luckySummonProgress = null)
	{
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Invalid comparison between Unknown and I4
		Unsubscribe();
		gameController = controller;
		boardManager = boardSystem;
		goldText = gold;
		lifeText = lifeLabel;
		roundText = round;
		boardText = board;
		contentText = content;
		hintText = hint;
		mergeResultText = mergeResult;
		mergeCelebrationText = mergeCelebration;
		mergeCelebrationSubText = mergeCelebrationSub;
		countdownText = countdown;
		roundBannerText = roundBanner;
		playerNameText = playerName;
		rankText = rank;
		stateText = state;
		battleButtonText = battleLabel;
		summonButtonText = summonLabel;
		summonCostText = summonCostLabel;
		luckySummonProgressText = luckySummonProgress;
		luckySummonProgressBadge = (((Object)(object)luckySummonProgressText != (Object)null && (Object)(object)((Component)luckySummonProgressText).transform.parent != (Object)null) ? ((Component)((Component)luckySummonProgressText).transform.parent).GetComponent<Image>() : null);
		deckSummaryText = deckSummary;
		capacityText = capacity;
		normalMergeText = normalMerge;
		rareMergeText = rareMerge;
		epicMergeText = epicMerge;
		legendaryMergeText = legendaryMerge;
		mythicMergeText = mythicMerge;
		transcendentMergeText = transcendentMerge;
		ultimateRecipeHudText = ultimateRecipeHud;
		bossRoundHudText = bossRoundHud;
		synergyInsightText = synergyInsight;
		recipeInsightText = recipeInsight;
		tileInsightText = tileInsight;
		topDamageInsightText = topDamageInsight;
		earlyRunInsightText = earlyRunInsight;
		fateGaugeText = fateGauge;
		fateGaugeFill = fateGaugeBar;
		fateDebtText = fateDebt;
		fateCostBenefitText = fateCostBenefit;
		fateGradeLockButton = fateGradeLock;
		fateGradeLockButtonText = fateGradeLockLabel;
		fateNormalBanButton = fateNormalBan;
		fateNormalBanButtonText = fateNormalBanLabel;
		fateForceShopButton = fateForceShop;
		fateForceShopButtonText = fateForceShopLabel;
		fateSurvivalButton = fateSurvival;
		fateSurvivalButtonText = fateSurvivalLabel;
		fatePanelRoot = fatePanel;
		fatePanelCanvasGroup = fatePanelGroup;
		fatePanelReopenButton = fatePanelReopen;
		fatePanelReopenButtonText = fatePanelReopenLabel;
		roundProgressFill = progressFill;
		fateEntryButtonOutline = (((Object)(object)fatePanelReopenButton != (Object)null) ? ((Component)fatePanelReopenButton).GetComponent<Outline>() : null);
		battleButton = battle;
		summonButton = summon;
		summonButtonImage = (((Object)(object)summonButton != (Object)null) ? ((Component)summonButton).GetComponent<Image>() : null);
		bossWarningPanel = bossWarning;
		bossWarningCanvasGroup = bossWarningGroup;
		bossWarningTitleText = bossWarningTitle;
		bossWarningSubText = bossWarningSub;
		unitSellPanel = sellPanel;
		unitSellTitleText = sellTitle;
		unitSellDetailText = sellDetail;
		unitSellButton = sellButton;
		unitSellButtonText = sellButtonLabel;
		lifeProgressFill = lifeProgress;
		if ((Object)(object)lifeProgressFill != (Object)null && (int)lifeProgressFill.type == 3)
		{
			lifeProgressFill.type = (Type)1;
		}
		if (!string.IsNullOrWhiteSpace(overrideHint))
		{
			hintMessage = overrideHint;
		}
		WireUnitSellButton();
		WireFatePanelControls();
		InitializeFatePanelMotionIfNeeded();
		ResetOpeningTutorial();
		Subscribe();
		Refresh();
	}

	private void OnEnable()
	{
		Subscribe();
	}

	private void Start()
	{
		Refresh();
	}

	private void OnDisable()
	{
		RestoreOpeningTutorialTarget();
		ResetUltimateReadyVisuals();
		if (defeatCinematicActive)
		{
			EndDefeatCinematic(immediate: true);
		}
		Unsubscribe();
	}

	private void Update()
	{
		UpdateMergeBanner();
		UpdateRoundBanner();
		UpdateMergeCelebration();
		UpdateBossWarning();
		UpdateOpeningTutorial();
		UpdateSellConfirmationTimer();
		UpdateDefeatCinematic();
		UpdateUltimateReadyEmphasis();
		UpdateFatePanelMotion();
		UpdateFateEntryButtonEmphasis();
	}

	public void Refresh()
	{
		if (!((Object)(object)gameController == (Object)null))
		{
			SetText(playerNameText, "레드X");
			SetText(rankText, "RANK " + Mathf.Max(1, gameController.CurrentRound + 1));
			SetText(goldText, gameController.Gold.ToString());
			SetText(lifeText, gameController.LifeHudSummary);
			SetText(roundText, "ROUND " + Mathf.Max(1, gameController.CurrentRound));
			SetText(boardText, gameController.BoardUnitCount + " / " + gameController.BoardCapacity);
			SetText(contentText, gameController.LifeHudSummary);
			RefreshLifeProgressFill();
			SetText(hintText, hintMessage);
			if (mergeBannerTextAvailable() && string.IsNullOrWhiteSpace(mergeBannerMessage))
			{
				SetMergeBannerVisible(visible: false);
			}
			RefreshDynamicState();
			ApplyOpeningTutorialHint();
		}
	}

	private void RefreshLifeProgressFill()
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Invalid comparison between Unknown and I4
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)lifeProgressFill == (Object)null)
		{
			return;
		}
		float num = (((Object)(object)gameController != (Object)null && gameController.MaxLife > 0) ? Mathf.Clamp01((float)gameController.Life / (float)gameController.MaxLife) : 0f);
		if ((int)lifeProgressFill.type == 3)
		{
			lifeProgressFill.fillAmount = num;
			return;
		}
		RectTransform rectTransform = ((Graphic)lifeProgressFill).rectTransform;
		if (!((Object)(object)rectTransform == (Object)null))
		{
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = new Vector2(num, 1f);
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;
		}
	}

	private void Subscribe()
	{
		if (!((Object)(object)gameController == (Object)null))
		{
			gameController.OnStateChanged -= Refresh;
			gameController.OnStateChanged += Refresh;
			gameController.OnMergeCompleted -= HandleMergeCompleted;
			gameController.OnMergeCompleted += HandleMergeCompleted;
			gameController.OnRoundCountdownChanged -= HandleRoundCountdownChanged;
			gameController.OnRoundCountdownChanged += HandleRoundCountdownChanged;
			gameController.OnBannerRequested -= HandleBannerRequested;
			gameController.OnBannerRequested += HandleBannerRequested;
			gameController.OnRoundStarted -= HandleRoundStarted;
			gameController.OnRoundStarted += HandleRoundStarted;
			gameController.OnGameOver -= HandleGameOver;
			gameController.OnGameOver += HandleGameOver;
			if ((Object)(object)boardManager != (Object)null)
			{
				boardManager.OnSelectedUnitChanged -= HandleSelectedUnitChanged;
				boardManager.OnSelectedUnitChanged += HandleSelectedUnitChanged;
			}
		}
	}

	private void Unsubscribe()
	{
		if ((Object)(object)gameController != (Object)null)
		{
			gameController.OnStateChanged -= Refresh;
			gameController.OnMergeCompleted -= HandleMergeCompleted;
			gameController.OnRoundCountdownChanged -= HandleRoundCountdownChanged;
			gameController.OnBannerRequested -= HandleBannerRequested;
			gameController.OnRoundStarted -= HandleRoundStarted;
			gameController.OnGameOver -= HandleGameOver;
		}
		if ((Object)(object)boardManager != (Object)null)
		{
			boardManager.OnSelectedUnitChanged -= HandleSelectedUnitChanged;
		}
	}

	private void WireUnitSellButton()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		if (!((Object)(object)unitSellButton == (Object)null))
		{
			((UnityEvent)unitSellButton.onClick).RemoveListener(new UnityAction(HandleSellButtonPressed));
			((UnityEvent)unitSellButton.onClick).AddListener(new UnityAction(HandleSellButtonPressed));
		}
	}

	private void HandleSelectedUnitChanged(DefenderUnit unit)
	{
		selectedUnit = unit;
		pendingSellConfirmUnit = null;
		pendingSellConfirmExpireTime = 0f;
		RefreshUnitSellPanel();
	}

	private void UpdateSellConfirmationTimer()
	{
		if (!((Object)(object)pendingSellConfirmUnit == (Object)null) && !(Time.unscaledTime <= pendingSellConfirmExpireTime))
		{
			pendingSellConfirmUnit = null;
			pendingSellConfirmExpireTime = 0f;
			RefreshUnitSellPanel();
		}
	}

	private void RefreshUnitSellPanel()
	{
		if ((Object)(object)unitSellPanel == (Object)null)
		{
			return;
		}
		bool flag = (Object)(object)selectedUnit != (Object)null && (Object)(object)selectedUnit.CurrentSlot != (Object)null;
		unitSellPanel.SetActive(flag);
		if ((Object)(object)hintText != (Object)null)
		{
			((Component)hintText).gameObject.SetActive(!flag);
		}
		if (flag)
		{
			string text = ((selectedUnit.Definition != null && !string.IsNullOrWhiteSpace(selectedUnit.Definition.displayName)) ? selectedUnit.Definition.displayName : "선택 유닛");
			int num = (((Object)(object)gameController != (Object)null) ? gameController.GetUnitSellRefund(selectedUnit) : 0);
			string reason = string.Empty;
			bool flag2 = (Object)(object)gameController != (Object)null && gameController.CanSellUnit(selectedUnit, out reason);
			bool flag3 = (Object)(object)gameController != (Object)null && gameController.IsUnitSellMergeCandidate(selectedUnit);
			SetText(unitSellTitleText, text + " 선택됨");
			string text2 = (((Object)(object)gameController != (Object)null) ? gameController.GetUnitSellDetail(selectedUnit) : "판매 정보 확인 중");
			if (!flag2 && !string.IsNullOrWhiteSpace(reason))
			{
				text2 = text2 + "  |  " + reason;
			}
			else if (flag3)
			{
				text2 += "  |  판매 전 합성 재료인지 확인";
			}
			SetText(unitSellDetailText, text2);
			SetText(unitSellButtonText, "판매 +" + num + "G");
			SetInteractable(unitSellButton, flag2);
		}
	}

	private void HandleSellButtonPressed()
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)gameController == (Object)null || (Object)(object)selectedUnit == (Object)null)
		{
			RefreshUnitSellPanel();
			return;
		}
		if (!gameController.CanSellUnit(selectedUnit, out var reason))
		{
			gameController.RequestBanner(reason, new Color(1f, 0.42f, 0.3f), 1.7f);
			RefreshUnitSellPanel();
			return;
		}
		bool flag = gameController.UnitSellRequiresConfirmation(selectedUnit);
		bool flag2 = (Object)(object)pendingSellConfirmUnit == (Object)(object)selectedUnit && Time.unscaledTime <= pendingSellConfirmExpireTime;
		int refund;
		string message;
		if (flag && !flag2)
		{
			pendingSellConfirmUnit = selectedUnit;
			pendingSellConfirmExpireTime = Time.unscaledTime + 2.5f;
			gameController.RequestBanner("합성 후보 또는 고등급 유닛입니다. 다시 누르면 판매합니다.", new Color(1f, 0.66f, 0.24f), 2f);
			RefreshUnitSellPanel();
		}
		else if (gameController.TrySellUnit(selectedUnit, out refund, out message))
		{
			selectedUnit = null;
			pendingSellConfirmUnit = null;
			pendingSellConfirmExpireTime = 0f;
			boardManager?.ClearSelectedUnit();
			Refresh();
		}
		else
		{
			gameController.RequestBanner(message, new Color(1f, 0.42f, 0.3f), 1.8f);
			RefreshUnitSellPanel();
		}
	}

	private void RefreshDynamicState()
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0327: Unknown result type (might be due to invalid IL or missing references)
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_060c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0569: Unknown result type (might be due to invalid IL or missing references)
		//IL_0553: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_048c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0476: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b8: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)gameController == (Object)null)
		{
			return;
		}
		bool isRoundRunning = gameController.IsRoundRunning;
		bool isCombatInteractionLocked = gameController.IsCombatInteractionLocked;
		bool fateCombatEditingActive = gameController.FateCombatEditingActive;
		SetText(stateText, fateCombatEditingActive ? "계약 편집 중" : (isCombatInteractionLocked ? "전투 진행 중" : (isRoundRunning ? "전투 준비" : "준비 단계")));
		SetColor(stateText, fateCombatEditingActive ? new Color(1f, 0.54f, 1f) : (isCombatInteractionLocked ? new Color(1f, 0.82f, 0.36f) : new Color(0.42f, 1f, 0.72f)));
		string value = ((!isRoundRunning) ? "다음 라운드" : (isCombatInteractionLocked ? "전투 중" : "전투 준비"));
		SetText(battleButtonText, value);
		SetInteractable(battleButton, !isRoundRunning);
		bool luckySummonReady = gameController.LuckySummonReady;
		bool luckySummonChoiceOpen = gameController.LuckySummonChoiceOpen;
		bool flag = !isCombatInteractionLocked && gameController.Gold >= gameController.SummonCost && gameController.EmptySlotCount > 0;
		string value2 = (isCombatInteractionLocked ? "전투 중" : (luckySummonChoiceOpen ? "선택 중" : (luckySummonReady ? "행운 소환" : (flag ? "소환" : ((gameController.EmptySlotCount <= 0) ? "자리 없음" : "골드 부족")))));
		SetText(summonButtonText, value2);
		SetText(summonCostText, gameController.SummonCost + " GOLD");
		SetText(luckySummonProgressText, luckySummonReady ? "행운 소환  READY" : (gameController.LuckySummonProgressVisible ? ("행운 소환  " + gameController.LuckySummonNormalStreak + " / " + gameController.LuckySummonThreshold) : string.Empty));
		SetColor(luckySummonProgressText, luckySummonReady ? new Color(0.2f, 0.13f, 0.05f) : new Color(0.94f, 1f, 0.78f));
		if ((Object)(object)luckySummonProgressBadge != (Object)null)
		{
			((Component)luckySummonProgressBadge).gameObject.SetActive(luckySummonReady || gameController.LuckySummonProgressVisible);
			((Graphic)luckySummonProgressBadge).color = (luckySummonReady ? new Color(0.96f, 0.72f, 0.22f, 0.98f) : new Color(0.07f, 0.2f, 0.17f, 0.96f));
		}
		if ((Object)(object)summonButtonImage != (Object)null)
		{
			((Graphic)summonButtonImage).color = (luckySummonReady ? new Color(0.56f, 0.76f, 0.2f, 1f) : new Color(0.19f, 0.78f, 0.42f, 1f));
		}
		SetInteractable(summonButton, flag && !luckySummonChoiceOpen);
		SetText(deckSummaryText, "보유 유닛 " + gameController.BoardUnitCount + " / " + gameController.BoardCapacity);
		SetText(capacityText, gameController.EmptySlotCount + "칸 남음");
		SetMergeCount(normalMergeText, CharacterGrade.Normal);
		SetMergeCount(rareMergeText, CharacterGrade.Rare);
		SetMergeCount(epicMergeText, CharacterGrade.Epic);
		SetMergeCount(legendaryMergeText, CharacterGrade.Legendary);
		SetOwnedGradeCount(mythicMergeText, CharacterGrade.Mythic);
		if ((Object)(object)transcendentMergeText != (Object)null)
		{
			int readyUltimateRecipeCount = gameController.ReadyUltimateRecipeCount;
			bool flag2 = !isCombatInteractionLocked;
			bool flag3 = flag2 && readyUltimateRecipeCount > 0;
			SetText(transcendentMergeText, (readyUltimateRecipeCount > 0) ? ("READY ×" + readyUltimateRecipeCount) : gameController.GetUltimateMergeStatus());
			SetColor(transcendentMergeText, (Color)(flag3 ? new Color(0.92f, 0.42f, 1f) : (isCombatInteractionLocked ? new Color(0.58f, 0.62f, 0.78f) : Color.white)));
			SetGradeCardInteractable(transcendentMergeText, flag2);
			SetUltimateReadyState(readyUltimateRecipeCount);
		}
		if ((Object)(object)ultimateRecipeHudText != (Object)null)
		{
			bool flag4 = gameController.CanMergeUltimate();
			string text = gameController.GetUltimateRecipeBingoStatus();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = gameController.GetUltimateMergeDetailStatus();
			}
			SetText(ultimateRecipeHudText, CompactHudLines(string.IsNullOrWhiteSpace(text) ? "초월 레시피 확인 중" : text, 2, 44));
			SetColor(ultimateRecipeHudText, flag4 ? new Color(1f, 0.86f, 0.28f) : new Color(0.76f, 0.94f, 1f));
		}
		if ((Object)(object)bossRoundHudText != (Object)null)
		{
			int num = Mathf.Max(10, gameController.NextBossRound);
			int num2 = Mathf.Max(0, gameController.RoundsUntilNextBoss);
			if (gameController.IsBossRound && gameController.IsRoundRunning)
			{
				SetText(bossRoundHudText, "보스 압박  " + gameController.CurrentBossPressureSummary);
				SetColor(bossRoundHudText, new Color(1f, 0.36f, 0.24f));
			}
			else
			{
				string text2 = ((num2 > 0) ? ("보스 R" + num + "까지 " + num2 + "R") : ("보스 R" + num));
				SetText(bossRoundHudText, text2 + "  |  " + BuildCompactBossGoalHud(gameController.CurrentBuildGoalSummary));
				SetColor(bossRoundHudText, (num2 <= 1) ? new Color(1f, 0.45f, 0.28f) : ((num2 <= 3) ? new Color(1f, 0.86f, 0.28f) : new Color(0.76f, 0.94f, 1f)));
			}
		}
		SetInsightVisible(synergyInsightText, visible: true);
		SetInsightVisible(recipeInsightText, visible: true);
		SetInsightVisible(tileInsightText, visible: true);
		SetInsightVisible(topDamageInsightText, visible: false);
		SetInsightVisible(earlyRunInsightText, visible: false);
		SetText(synergyInsightText, CompactHudLines(gameController.CurrentDangerSummary, 2, 18));
		SetText(recipeInsightText, CompactHudLines(gameController.CurrentBuildGoalSummary, 2, 18));
		SetText(tileInsightText, CompactHudLines(gameController.RoundTopDamageSummary, 2, 18));
		RefreshFateControls();
		if ((Object)(object)roundProgressFill != (Object)null)
		{
			float roundProgress = gameController.RoundProgress01;
			if (!Mathf.Approximately(roundProgressFill.fillAmount, roundProgress))
			{
				roundProgressFill.fillAmount = roundProgress;
			}
		}
		RefreshUnitSellPanel();
	}

	private void RefreshFateControls()
	{
		if (!((Object)(object)gameController == (Object)null))
		{
			bool shouldShowFatePanel = gameController.ShouldShowFatePanel;
			bool shouldShowFateCardEntryButton = gameController.ShouldShowFateCardEntryButton;
			UpdateFatePanelAvailability(shouldShowFatePanel, shouldShowFateCardEntryButton);
			if ((Object)(object)fateGaugeFill != (Object)null)
			{
				fateGaugeFill.fillAmount = gameController.FateGauge01;
			}
			SetText(fateGaugeText, gameController.FateHudSummary);
			SetText(fateDebtText, gameController.FateCardStatusSummary);
			SetText(fateCostBenefitText, CompactHudLines(gameController.FateCostBenefitSummary, 3, 32));
			if (!shouldShowFatePanel)
			{
				string value = (shouldShowFateCardEntryButton ? "봉인\n카드 개방 후 공개" : "봉인\n전투 중 공개");
				string value2 = (shouldShowFateCardEntryButton ? "운명카드\n꺼내기 대기" : "운명카드\n전투 중 개방");
				SetText(fateGradeLockButtonText, value);
				SetText(fateNormalBanButtonText, value);
				SetText(fateForceShopButtonText, value2);
				SetText(fateSurvivalButtonText, value2);
				SetInteractable(fateGradeLockButton, value: false);
				SetInteractable(fateNormalBanButton, value: false);
				SetInteractable(fateForceShopButton, value: false);
				SetInteractable(fateSurvivalButton, value: false);
				ApplyFateSurvivalEmphasis(active: false);
			}
			else
			{
				SetText(fateGradeLockButtonText, gameController.FateGradeLockHudLabel);
				SetText(fateNormalBanButtonText, gameController.FateNormalBanHudLabel);
				SetText(fateForceShopButtonText, gameController.FateForceShopHudLabel);
				SetText(fateSurvivalButtonText, gameController.FateSurvivalHudLabel);
				SetInteractable(fateGradeLockButton, gameController.CanUseFateGradeLock);
				SetInteractable(fateNormalBanButton, gameController.CanUseFateNormalBan);
				SetInteractable(fateForceShopButton, gameController.CanUseFateForcedShop);
				SetInteractable(fateSurvivalButton, gameController.CanUseFateSurvival);
				ApplyFateChoiceButtonVisuals();
				ApplyFateSurvivalEmphasis(gameController.FateSurvivalCrisisActive);
			}
		}
	}

	private void WireFatePanelControls()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		if (!((Object)(object)fatePanelReopenButton == (Object)null))
		{
			((UnityEvent)fatePanelReopenButton.onClick).RemoveListener(new UnityAction(ExpandFatePanel));
			((UnityEvent)fatePanelReopenButton.onClick).RemoveListener(new UnityAction(HandleFateEntryButtonPressed));
			((UnityEvent)fatePanelReopenButton.onClick).AddListener(new UnityAction(HandleFateEntryButtonPressed));
			((Component)fatePanelReopenButton).gameObject.SetActive(false);
		}
	}

	private void InitializeFatePanelMotionIfNeeded()
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Expected O, but got Unknown
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		if (fatePanelMotionInitialized || (Object)(object)fatePanelRoot == (Object)null)
		{
			return;
		}
		fatePanelRect = fatePanelRoot.GetComponent<RectTransform>();
		if ((Object)(object)fatePanelCanvasGroup == (Object)null)
		{
			fatePanelCanvasGroup = fatePanelRoot.GetComponent<CanvasGroup>();
		}
		if ((Object)(object)fatePanelRect != (Object)null)
		{
			fatePanelRect.anchorMin = new Vector2(0.5f, 0.5f);
			fatePanelRect.anchorMax = new Vector2(0.5f, 0.5f);
			fatePanelRect.pivot = new Vector2(0.5f, 0.5f);
			fatePanelOpenPosition = Vector2.zero;
			Transform parent = ((Transform)fatePanelRect).parent;
			RectTransform val = (RectTransform)(object)((parent is RectTransform) ? parent : null);
			float num;
			Rect rect;
			if (!((Object)(object)val != (Object)null))
			{
				num = 1920f;
			}
			else
			{
				rect = val.rect;
				num = ((Rect)(ref rect)).height;
			}
			float num2 = num;
			Vector2 val2 = fatePanelOpenPosition;
			float num3 = num2 * 0.5f;
			rect = fatePanelRect.rect;
			fatePanelClosedPosition = val2 + new Vector2(0f, 0f - (num3 + ((Rect)(ref rect)).height + 226f));
		}
		if ((Object)(object)fateChoiceBackdrop == (Object)null && (Object)(object)fatePanelRoot.transform.parent != (Object)null)
		{
			fateChoiceBackdrop = new GameObject("FateChoiceBackdrop", new Type[3]
			{
				typeof(RectTransform),
				typeof(Image),
				typeof(CanvasGroup)
			});
			fateChoiceBackdrop.transform.SetParent(fatePanelRoot.transform.parent, false);
			RectTransform component = fateChoiceBackdrop.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			Image component2 = fateChoiceBackdrop.GetComponent<Image>();
			((Graphic)component2).color = new Color(0.01f, 0.01f, 0.04f, 0.72f);
			((Graphic)component2).raycastTarget = true;
			fateChoiceBackdropCanvasGroup = fateChoiceBackdrop.GetComponent<CanvasGroup>();
			fateChoiceBackdropCanvasGroup.alpha = 0f;
			fateChoiceBackdropCanvasGroup.blocksRaycasts = false;
			fateChoiceBackdropCanvasGroup.interactable = false;
			fateChoiceBackdrop.transform.SetSiblingIndex(Mathf.Max(0, fatePanelRoot.transform.GetSiblingIndex()));
			fatePanelRoot.transform.SetAsLastSibling();
		}
		fatePanelVisible = fatePanelRoot.activeSelf;
		fatePanelTargetOpen = fatePanelVisible;
		if ((Object)(object)fatePanelCanvasGroup != (Object)null)
		{
			fatePanelCanvasGroup.alpha = (fatePanelVisible ? 1f : 0f);
			fatePanelCanvasGroup.interactable = fatePanelVisible;
			fatePanelCanvasGroup.blocksRaycasts = fatePanelVisible;
		}
		if ((Object)(object)fateChoiceBackdrop != (Object)null)
		{
			fateChoiceBackdrop.SetActive(fatePanelVisible);
		}
		fatePanelMotionInitialized = true;
	}

	private void UpdateFatePanelAvailability(bool shouldShow, bool shouldShowEntryButton)
	{
		InitializeFatePanelMotionIfNeeded();
		if (shouldShow)
		{
			if ((Object)(object)fatePanelRoot != (Object)null && !fatePanelRoot.activeSelf)
			{
				ExpandFatePanel();
			}
			else if (!fatePanelTargetOpen && !fatePanelVisible)
			{
				ExpandFatePanel();
			}
		}
		else if (fatePanelTargetOpen || fatePanelVisible)
		{
			CollapseFatePanel();
		}
		if ((Object)(object)fatePanelReopenButton != (Object)null)
		{
			bool flag = shouldShowEntryButton && !shouldShow;
			if (!flag)
			{
				flag = shouldShow && !fatePanelTargetOpen;
			}
			((Component)fatePanelReopenButton).gameObject.SetActive(flag);
			fateEntryButtonEmphasisActive = shouldShowEntryButton && !shouldShow && flag && (Object)(object)gameController != (Object)null && gameController.FateSurvivalCrisisActive;
			SetText(fatePanelReopenButtonText, (shouldShowEntryButton && !shouldShow) ? "운명 카드\n꺼내기" : "계약");
		}
	}

	private void HandleFateEntryButtonPressed()
	{
		if (!((Object)(object)gameController == (Object)null) && gameController.TryOpenFateCardChoicePanel())
		{
			ExpandFatePanel();
		}
	}

	private void ExpandFatePanel()
	{
		InitializeFatePanelMotionIfNeeded();
		fatePanelTargetOpen = true;
		fatePanelVisible = true;
		if ((Object)(object)fateChoiceBackdrop != (Object)null)
		{
			fateChoiceBackdrop.SetActive(true);
			fateChoiceBackdrop.transform.SetSiblingIndex(Mathf.Max(0, fatePanelRoot.transform.GetSiblingIndex()));
		}
		GameObject obj = fatePanelRoot;
		if (obj != null)
		{
			obj.transform.SetAsLastSibling();
		}
		if ((Object)(object)fateChoiceBackdropCanvasGroup != (Object)null)
		{
			fateChoiceBackdropCanvasGroup.blocksRaycasts = true;
			fateChoiceBackdropCanvasGroup.interactable = true;
		}
		if ((Object)(object)fatePanelRoot != (Object)null && !fatePanelRoot.activeSelf)
		{
			fatePanelRoot.SetActive(true);
		}
		if ((Object)(object)fatePanelCanvasGroup != (Object)null)
		{
			fatePanelCanvasGroup.interactable = true;
			fatePanelCanvasGroup.blocksRaycasts = true;
		}
		if ((Object)(object)fatePanelReopenButton != (Object)null)
		{
			((Component)fatePanelReopenButton).gameObject.SetActive(false);
		}
	}

	private void CollapseFatePanel()
	{
		InitializeFatePanelMotionIfNeeded();
		fatePanelTargetOpen = false;
		fatePanelVisible = true;
		if ((Object)(object)fatePanelRoot != (Object)null && !fatePanelRoot.activeSelf)
		{
			fatePanelRoot.SetActive(true);
		}
		if ((Object)(object)fatePanelCanvasGroup != (Object)null)
		{
			fatePanelCanvasGroup.interactable = false;
			fatePanelCanvasGroup.blocksRaycasts = false;
		}
	}

	private void UpdateFateEntryButtonEmphasis()
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)fatePanelReopenButton == (Object)null)
		{
			return;
		}
		if (!fateEntryButtonVisualInitialized)
		{
			fateEntryButtonVisualInitialized = true;
			fateEntryButtonBaseScale = ((Component)fatePanelReopenButton).transform.localScale;
			fateEntryButtonBaseColor = FateEntryIdleColor;
			Graphic val = (((Object)(object)((Selectable)fatePanelReopenButton).targetGraphic != (Object)null) ? ((Selectable)fatePanelReopenButton).targetGraphic : ((Component)fatePanelReopenButton).GetComponent<Graphic>());
			if ((Object)(object)val != (Object)null)
			{
				((Selectable)fatePanelReopenButton).targetGraphic = val;
				val.color = fateEntryButtonBaseColor;
			}
		}
		if ((Object)(object)fateEntryButtonOutline != (Object)null)
		{
			((Shadow)fateEntryButtonOutline).effectColor = FateEntryOutlineColor;
		}
		Graphic val2 = (((Object)(object)((Selectable)fatePanelReopenButton).targetGraphic != (Object)null) ? ((Selectable)fatePanelReopenButton).targetGraphic : ((Component)fatePanelReopenButton).GetComponent<Graphic>());
		if (!fateEntryButtonEmphasisActive || !((Component)fatePanelReopenButton).gameObject.activeInHierarchy)
		{
			((Component)fatePanelReopenButton).transform.localScale = fateEntryButtonBaseScale;
			if ((Object)(object)val2 != (Object)null)
			{
				val2.color = fateEntryButtonBaseColor;
			}
			if ((Object)(object)fatePanelReopenButtonText != (Object)null)
			{
				SetColor(fatePanelReopenButtonText, FateEntryTextColor);
			}
			return;
		}
		float num = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.6f);
		((Component)fatePanelReopenButton).transform.localScale = fateEntryButtonBaseScale * Mathf.Lerp(1f, 1.07f, num);
		if ((Object)(object)val2 != (Object)null)
		{
			val2.color = Color.Lerp(fateEntryButtonBaseColor, FateEntryCrisisColor, 0.28f + num * 0.42f);
		}
		if ((Object)(object)fatePanelReopenButtonText != (Object)null)
		{
			SetColor(fatePanelReopenButtonText, FateEntryTextColor);
		}
		if ((Object)(object)fateEntryButtonOutline != (Object)null)
		{
			((Shadow)fateEntryButtonOutline).effectColor = Color.Lerp(FateEntryOutlineColor, FateEntryCrisisOutlineColor, 0.3f + num * 0.7f);
		}
	}

	private void UpdateFatePanelMotion()
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		InitializeFatePanelMotionIfNeeded();
		if ((Object)(object)fatePanelRect == (Object)null || (Object)(object)fatePanelRoot == (Object)null || (!fatePanelRoot.activeSelf && !fatePanelTargetOpen))
		{
			return;
		}
		if (!fatePanelRoot.activeSelf)
		{
			fatePanelRoot.SetActive(true);
		}
		Vector2 val = (fatePanelTargetOpen ? fatePanelOpenPosition : fatePanelClosedPosition);
		float num = 1f - Mathf.Exp(-13f * Time.unscaledDeltaTime);
		fatePanelRect.anchoredPosition = Vector2.Lerp(fatePanelRect.anchoredPosition, val, num);
		if ((Object)(object)fatePanelCanvasGroup != (Object)null)
		{
			float num2 = (fatePanelTargetOpen ? 1f : 0f);
			fatePanelCanvasGroup.alpha = Mathf.MoveTowards(fatePanelCanvasGroup.alpha, num2, 6f * Time.unscaledDeltaTime);
		}
		if ((Object)(object)fateChoiceBackdropCanvasGroup != (Object)null)
		{
			float num3 = (fatePanelTargetOpen ? 1f : 0f);
			fateChoiceBackdropCanvasGroup.alpha = Mathf.MoveTowards(fateChoiceBackdropCanvasGroup.alpha, num3, 6f * Time.unscaledDeltaTime);
		}
		bool flag = (Object)(object)fatePanelCanvasGroup == (Object)null || fatePanelCanvasGroup.alpha <= 0.02f;
		if (!fatePanelTargetOpen && Vector2.Distance(fatePanelRect.anchoredPosition, fatePanelClosedPosition) <= 0.8f && flag)
		{
			fatePanelRect.anchoredPosition = fatePanelClosedPosition;
			fatePanelVisible = false;
			fatePanelRoot.SetActive(false);
			if ((Object)(object)fateChoiceBackdrop != (Object)null)
			{
				fateChoiceBackdrop.SetActive(false);
			}
			return;
		}
		bool flag2 = (Object)(object)fatePanelCanvasGroup == (Object)null || fatePanelCanvasGroup.alpha >= 0.98f;
		if (fatePanelTargetOpen && Vector2.Distance(fatePanelRect.anchoredPosition, fatePanelOpenPosition) <= 0.8f && flag2)
		{
			fatePanelRect.anchoredPosition = fatePanelOpenPosition;
			fatePanelVisible = true;
			if ((Object)(object)fatePanelCanvasGroup != (Object)null)
			{
				fatePanelCanvasGroup.alpha = 1f;
				fatePanelCanvasGroup.interactable = true;
				fatePanelCanvasGroup.blocksRaycasts = true;
			}
		}
	}

	private void ApplyFateChoiceButtonVisuals()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)gameController == (Object)null))
		{
			ApplyFateChoiceButtonColor(fateSurvivalButton, gameController.FateSurvivalHudColor, isSurvivalButton: true);
			ApplyFateChoiceButtonColor(fateGradeLockButton, gameController.FateGradeLockHudColor, isSurvivalButton: false);
			ApplyFateChoiceButtonColor(fateNormalBanButton, gameController.FateNormalBanHudColor, isSurvivalButton: false);
		}
	}

	private void ApplyFateChoiceButtonColor(Button target, Color color, bool isSurvivalButton)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)target == (Object)null))
		{
			Graphic targetGraphic = ((Selectable)target).targetGraphic;
			if ((Object)(object)targetGraphic != (Object)null && targetGraphic.color != color)
			{
				targetGraphic.color = color;
			}
			ColorBlock colors = ((Selectable)target).colors;
			Color val = Color.Lerp(color, Color.white, 0.16f);
			Color pressedColor = Color.Lerp(color, Color.black, 0.18f);
			Color disabledColor = default(Color);
			((Color)(ref disabledColor))._002Ector(color.r * 0.34f, color.g * 0.34f, color.b * 0.34f, 0.48f);
			((ColorBlock)(ref colors)).normalColor = color;
			((ColorBlock)(ref colors)).highlightedColor = val;
			((ColorBlock)(ref colors)).selectedColor = val;
			((ColorBlock)(ref colors)).pressedColor = pressedColor;
			((ColorBlock)(ref colors)).disabledColor = disabledColor;
			((Selectable)target).colors = colors;
			if (isSurvivalButton && fateSurvivalVisualInitialized)
			{
				fateSurvivalBaseColor = color;
			}
		}
	}

	private void ApplyFateSurvivalEmphasis(bool active)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)fateSurvivalButton == (Object)null)
		{
			return;
		}
		if (!fateSurvivalVisualInitialized)
		{
			fateSurvivalBaseScale = ((Component)fateSurvivalButton).transform.localScale;
			Graphic targetGraphic = ((Selectable)fateSurvivalButton).targetGraphic;
			fateSurvivalBaseColor = (((Object)(object)targetGraphic != (Object)null) ? targetGraphic.color : Color.white);
			fateSurvivalVisualInitialized = true;
		}
		Graphic targetGraphic2 = ((Selectable)fateSurvivalButton).targetGraphic;
		if (active)
		{
			float num = 0.5f + Mathf.Sin(Time.unscaledTime * 8f) * 0.5f;
			((Component)fateSurvivalButton).transform.localScale = fateSurvivalBaseScale * Mathf.Lerp(1.04f, 1.16f, num);
			if ((Object)(object)targetGraphic2 != (Object)null)
			{
				targetGraphic2.color = Color.Lerp(new Color(1f, 0.36f, 0.14f, 1f), new Color(1f, 0.88f, 0.2f, 1f), num);
			}
			SetColor(fateSurvivalButtonText, Color.white);
		}
		else
		{
			((Component)fateSurvivalButton).transform.localScale = fateSurvivalBaseScale;
			if ((Object)(object)targetGraphic2 != (Object)null && targetGraphic2.color != fateSurvivalBaseColor)
			{
				targetGraphic2.color = fateSurvivalBaseColor;
			}
		}
	}

	private void SetMergeCount(Text target, CharacterGrade grade)
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)target == (Object)null) && !((Object)(object)gameController == (Object)null))
		{
			int num = gameController.CountUnitsOfGrade(grade);
			bool isCombatInteractionLocked = gameController.IsCombatInteractionLocked;
			bool flag = !isCombatInteractionLocked && num >= 3;
			SetText(target, num + " / 3");
			SetColor(target, (Color)(flag ? new Color(0.42f, 1f, 0.72f) : (isCombatInteractionLocked ? new Color(0.58f, 0.62f, 0.78f) : Color.white)));
			SetGradeCardInteractable(target, value: true);
		}
	}

	private void SetOwnedGradeCount(Text target, CharacterGrade grade)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)target == (Object)null) && !((Object)(object)gameController == (Object)null))
		{
			int num = gameController.CountUnitsOfGrade(grade);
			SetText(target, num + "개");
			SetColor(target, (num > 0) ? CharacterGradeUtility.GetColor(grade, Color.white) : Color.white);
		}
	}

	private void SetGradeCardInteractable(Text target, bool value)
	{
		Button val = (((Object)(object)target != (Object)null && (Object)(object)((Component)target).transform != (Object)null) ? ((Component)((Component)target).transform).GetComponentInParent<Button>() : null);
		if ((Object)(object)val != (Object)null && ((Selectable)val).interactable != value)
		{
			((Selectable)val).interactable = value;
		}
	}

	private void SetUltimateReadyState(int readyCount)
	{
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		EnsureUltimateReadyVisuals();
		ultimateReadyCount = Mathf.Max(0, readyCount);
		if ((Object)(object)ultimateReadyBadge != (Object)null)
		{
			((Component)ultimateReadyBadge).gameObject.SetActive(ultimateReadyCount > 0);
		}
		if ((Object)(object)ultimateReadyBadgeText != (Object)null)
		{
			ultimateReadyBadgeText.text = ((ultimateReadyCount > 1) ? ("READY ×" + ultimateReadyCount) : "READY");
		}
		if (!ultimateReadyStateInitialized)
		{
			ultimateReadyStateInitialized = true;
			previousUltimateReadyCount = 0;
		}
		if (ultimateReadyCount > previousUltimateReadyCount && ultimateReadyCount > 0)
		{
			string message = ((ultimateReadyCount > 1) ? ("초월 레시피 " + ultimateReadyCount + "개 준비!  초월 버튼에서 선택하세요") : "초월 조합 준비 완료!  초월 버튼을 확인하세요");
			gameController.RequestBanner(message, new Color(1f, 0.78f, 0.22f), 2.8f);
			RuntimeAudioUtility.PlayJackpotMinor();
			RuntimeCameraShake.Request(0.04f, 0.14f);
		}
		previousUltimateReadyCount = ultimateReadyCount;
	}

	private void EnsureUltimateReadyVisuals()
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)ultimateMergeButton != (Object)null || (Object)(object)transcendentMergeText == (Object)null)
		{
			return;
		}
		ultimateMergeButton = ((Component)transcendentMergeText).GetComponentInParent<Button>();
		if (!((Object)(object)ultimateMergeButton == (Object)null))
		{
			ultimateMergeBaseScale = ((Component)ultimateMergeButton).transform.localScale;
			string[] array = new string[4] { "ReadyGlowTop", "ReadyGlowRight", "ReadyGlowBottom", "ReadyGlowLeft" };
			ultimateReadyLines = (Image[])(object)new Image[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				Transform val = ((Component)ultimateMergeButton).transform.Find(array[i]);
				ultimateReadyLines[i] = (((Object)(object)val != (Object)null) ? ((Component)val).GetComponent<Image>() : null);
			}
			Transform val2 = ((Component)ultimateMergeButton).transform.Find("ReadyBadge");
			ultimateReadyBadge = (((Object)(object)val2 != (Object)null) ? ((Component)val2).GetComponent<Image>() : null);
			Transform val3 = (((Object)(object)val2 != (Object)null) ? val2.Find("ReadyBadgeText") : null);
			ultimateReadyBadgeText = (((Object)(object)val3 != (Object)null) ? ((Component)val3).GetComponent<Text>() : null);
		}
	}

	private void UpdateUltimateReadyEmphasis()
	{
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		EnsureUltimateReadyVisuals();
		if ((Object)(object)ultimateMergeButton == (Object)null)
		{
			return;
		}
		bool flag = ultimateReadyCount > 0;
		if (ultimateReadyLines != null)
		{
			Color val = default(Color);
			((Color)(ref val))._002Ector(1f, 0.86f, 0.22f, 1f);
			Color val2 = default(Color);
			((Color)(ref val2))._002Ector(0.92f, 0.32f, 1f, 1f);
			for (int i = 0; i < ultimateReadyLines.Length; i++)
			{
				Image val3 = ultimateReadyLines[i];
				if (!((Object)(object)val3 == (Object)null))
				{
					((Component)val3).gameObject.SetActive(flag);
					if (flag)
					{
						float num = 0.5f + Mathf.Sin(Time.unscaledTime * 7.2f - (float)i * MathF.PI * 0.5f) * 0.5f;
						Color color = Color.Lerp(val, val2, Mathf.PingPong(Time.unscaledTime * 0.55f + (float)i * 0.22f, 1f));
						color.a = Mathf.Lerp(0.22f, 1f, num * num);
						((Graphic)val3).color = color;
					}
				}
			}
		}
		if (flag)
		{
			float num2 = 0.5f + Mathf.Sin(Time.unscaledTime * 5.2f) * 0.5f;
			((Component)ultimateMergeButton).transform.localScale = ultimateMergeBaseScale * Mathf.Lerp(1.02f, 1.07f, num2);
		}
		else
		{
			((Component)ultimateMergeButton).transform.localScale = ultimateMergeBaseScale;
		}
	}

	private void ResetUltimateReadyVisuals()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)ultimateMergeButton != (Object)null)
		{
			((Component)ultimateMergeButton).transform.localScale = ultimateMergeBaseScale;
		}
		if (ultimateReadyLines == null)
		{
			return;
		}
		for (int i = 0; i < ultimateReadyLines.Length; i++)
		{
			if ((Object)(object)ultimateReadyLines[i] != (Object)null)
			{
				((Component)ultimateReadyLines[i]).gameObject.SetActive(false);
			}
		}
	}

	private void SetInsightVisible(Text target, bool visible)
	{
		if (!((Object)(object)target == (Object)null) && !((Object)(object)((Component)target).transform == (Object)null) && !((Object)(object)((Component)target).transform.parent == (Object)null))
		{
			((Component)((Component)target).transform.parent).gameObject.SetActive(visible);
		}
	}

	private void UpdateMergeBanner()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		if (mergeBannerTimer > 0f)
		{
			mergeBannerTimer -= Time.deltaTime;
			if (mergeBannerTextAvailable())
			{
				SetMergeBannerVisible(visible: true);
				Color color = ((Graphic)mergeResultText).color;
				color.a = Mathf.Lerp(0.2f, 1f, Mathf.Clamp01(mergeBannerTimer / 2f));
				((Graphic)mergeResultText).color = color;
			}
		}
		else if (mergeBannerTextAvailable() && !string.IsNullOrEmpty(mergeBannerMessage))
		{
			mergeBannerMessage = string.Empty;
			SetText(mergeResultText, string.Empty);
			Color color2 = ((Graphic)mergeResultText).color;
			color2.a = 1f;
			((Graphic)mergeResultText).color = color2;
			SetMergeBannerVisible(visible: false);
		}
		else
		{
			SetMergeBannerVisible(visible: false);
		}
	}

	private void UpdateRoundBanner()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)roundBannerText == (Object)null))
		{
			if (roundBannerTimer > 0f)
			{
				roundBannerTimer -= Time.deltaTime;
				Color color = ((Graphic)roundBannerText).color;
				color.a = Mathf.Lerp(0.15f, 1f, Mathf.Clamp01(roundBannerTimer / 2.5f));
				((Graphic)roundBannerText).color = color;
			}
			else if (!string.IsNullOrEmpty(roundBannerText.text))
			{
				SetText(roundBannerText, string.Empty);
			}
		}
	}

	private void UpdateMergeCelebration()
	{
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)mergeCelebrationText == (Object)null)
		{
			return;
		}
		if (mergeCelebrationTimer > 0f)
		{
			mergeCelebrationTimer -= Time.deltaTime;
			float num = Mathf.Clamp01(mergeCelebrationTimer / 0.8f);
			float num2 = Mathf.Lerp(1f, 1.18f, num);
			RectTransform component = ((Component)mergeCelebrationText).GetComponent<RectTransform>();
			if ((Object)(object)component != (Object)null)
			{
				((Transform)component).localScale = Vector3.one * num2;
			}
			Color color = ((Graphic)mergeCelebrationText).color;
			color.a = Mathf.Lerp(0.1f, 1f, num);
			((Graphic)mergeCelebrationText).color = color;
			if ((Object)(object)mergeCelebrationSubText != (Object)null)
			{
				RectTransform component2 = ((Component)mergeCelebrationSubText).GetComponent<RectTransform>();
				if ((Object)(object)component2 != (Object)null)
				{
					((Transform)component2).localScale = Vector3.one * Mathf.Lerp(1f, 1.08f, num);
				}
				Color color2 = ((Graphic)mergeCelebrationSubText).color;
				color2.a = Mathf.Lerp(0.05f, 0.92f, num);
				((Graphic)mergeCelebrationSubText).color = color2;
			}
		}
		else
		{
			if (string.IsNullOrEmpty(mergeCelebrationText.text))
			{
				return;
			}
			SetText(mergeCelebrationText, string.Empty);
			((Graphic)mergeCelebrationText).color = new Color(((Graphic)mergeCelebrationText).color.r, ((Graphic)mergeCelebrationText).color.g, ((Graphic)mergeCelebrationText).color.b, 0f);
			RectTransform component3 = ((Component)mergeCelebrationText).GetComponent<RectTransform>();
			if ((Object)(object)component3 != (Object)null)
			{
				((Transform)component3).localScale = Vector3.one;
			}
			if ((Object)(object)mergeCelebrationSubText != (Object)null)
			{
				SetText(mergeCelebrationSubText, string.Empty);
				((Graphic)mergeCelebrationSubText).color = new Color(((Graphic)mergeCelebrationSubText).color.r, ((Graphic)mergeCelebrationSubText).color.g, ((Graphic)mergeCelebrationSubText).color.b, 0f);
				RectTransform component4 = ((Component)mergeCelebrationSubText).GetComponent<RectTransform>();
				if ((Object)(object)component4 != (Object)null)
				{
					((Transform)component4).localScale = Vector3.one;
				}
			}
		}
	}

	private void UpdateBossWarning()
	{
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)bossWarningPanel == (Object)null)
		{
			return;
		}
		if (bossWarningTimer <= 0f)
		{
			if (bossWarningPanel.activeSelf)
			{
				bossWarningPanel.SetActive(false);
			}
			return;
		}
		bossWarningTimer -= Time.unscaledDeltaTime;
		float num = Mathf.Clamp01(bossWarningTimer / 3.4f);
		float num2 = 1f - num;
		float num3 = Mathf.Clamp01(num2 / 0.16f);
		float num4 = Mathf.Clamp01(num / 0.22f);
		float alpha = Mathf.Min(num3, num4);
		if ((Object)(object)bossWarningCanvasGroup != (Object)null)
		{
			bossWarningCanvasGroup.alpha = alpha;
		}
		RectTransform component = bossWarningPanel.GetComponent<RectTransform>();
		if ((Object)(object)component != (Object)null)
		{
			float num5 = Mathf.Sin(num2 * MathF.PI);
			((Transform)component).localScale = Vector3.one * Mathf.Lerp(0.96f, 1.06f, num5);
		}
		if (bossWarningTimer <= 0f)
		{
			bossWarningPanel.SetActive(false);
		}
	}

	private void ResetOpeningTutorial()
	{
		RestoreOpeningTutorialTarget();
		openingTutorialStartTime = Time.unscaledTime;
		openingTutorialStage = -1;
		openingTutorialCompleted = false;
	}

	private void UpdateOpeningTutorial()
	{
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		if (!enableOpeningTutorial || openingTutorialCompleted || (Object)(object)gameController == (Object)null)
		{
			return;
		}
		if (openingTutorialStartTime < 0f)
		{
			openingTutorialStartTime = Time.unscaledTime;
		}
		if (gameController.CurrentRound > 1)
		{
			CompleteOpeningTutorial();
			return;
		}
		float num = Time.unscaledTime - openingTutorialStartTime;
		if (num >= 10f)
		{
			CompleteOpeningTutorial();
			return;
		}
		int num2 = Mathf.Clamp(Mathf.FloorToInt(num / 2.5f), 0, 3);
		Transform openingTutorialTarget = GetOpeningTutorialTarget(num2);
		string openingTutorialMessage = GetOpeningTutorialMessage(num2);
		if (openingTutorialStage != num2)
		{
			openingTutorialStage = num2;
			HandleBannerRequested(openingTutorialMessage, new Color(1f, 0.88f, 0.22f), 2.2f);
		}
		PulseOpeningTutorialTarget(openingTutorialTarget);
		SetText(hintText, openingTutorialMessage);
	}

	private void ApplyOpeningTutorialHint()
	{
		if (enableOpeningTutorial && !openingTutorialCompleted && openingTutorialStage >= 0)
		{
			SetText(hintText, GetOpeningTutorialMessage(openingTutorialStage));
		}
	}

	private Transform GetOpeningTutorialTarget(int stage)
	{
		switch (stage)
		{
		case 0:
			return ((Object)(object)summonButton != (Object)null) ? ((Component)summonButton).transform : null;
		case 1:
			return ((Object)(object)battleButton != (Object)null) ? ((Component)battleButton).transform : null;
		case 2:
			return ((Object)(object)normalMergeText != (Object)null && (Object)(object)((Component)normalMergeText).transform.parent != (Object)null) ? ((Component)normalMergeText).transform.parent : (((Object)(object)normalMergeText != (Object)null) ? ((Component)normalMergeText).transform : null);
		default:
			if ((Object)(object)recipeInsightText != (Object)null && (Object)(object)((Component)recipeInsightText).transform.parent != (Object)null)
			{
				return ((Component)recipeInsightText).transform.parent;
			}
			if ((Object)(object)bossRoundHudText != (Object)null && (Object)(object)((Component)bossRoundHudText).transform.parent != (Object)null)
			{
				return ((Component)bossRoundHudText).transform.parent;
			}
			return ((Object)(object)mergeResultText != (Object)null && (Object)(object)((Component)mergeResultText).transform.parent != (Object)null) ? ((Component)mergeResultText).transform.parent : (((Object)(object)mergeResultText != (Object)null) ? ((Component)mergeResultText).transform : null);
		}
	}

	private string GetOpeningTutorialMessage(int stage)
	{
		return stage switch
		{
			0 => "1. 소환한다  새 유닛을 뽑으세요", 
			1 => "2. 막는다  라운드를 눌러 막으세요", 
			2 => "3. 합친다  같은 등급 3개면 합성", 
			_ => "4. 더 센 게 나온다  R3 상점에서 방향 선택", 
		};
	}

	private void PulseOpeningTutorialTarget(Transform target)
	{
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)target == (Object)null)
		{
			RestoreOpeningTutorialTarget();
			return;
		}
		Graphic val = ((Component)target).GetComponent<Graphic>();
		if ((Object)(object)val == (Object)null)
		{
			val = ((Component)target).GetComponentInChildren<Graphic>();
		}
		if ((Object)(object)val == (Object)null)
		{
			RestoreOpeningTutorialTarget();
			return;
		}
		if ((Object)(object)openingTutorialGraphic != (Object)(object)val)
		{
			RestoreOpeningTutorialTarget();
			openingTutorialGraphic = val;
			openingTutorialOriginalColor = val.color;
			openingTutorialRect = ((Component)target).GetComponent<RectTransform>();
			openingTutorialOriginalScale = (((Object)(object)openingTutorialRect != (Object)null) ? ((Transform)openingTutorialRect).localScale : Vector3.one);
		}
		float num = 0.35f + Mathf.PingPong(Time.unscaledTime * 3.2f, 0.55f);
		Color val2 = default(Color);
		((Color)(ref val2))._002Ector(1f, 0.88f, 0.18f, openingTutorialOriginalColor.a);
		openingTutorialGraphic.color = Color.Lerp(openingTutorialOriginalColor, val2, num);
		if ((Object)(object)openingTutorialRect != (Object)null)
		{
			((Transform)openingTutorialRect).localScale = openingTutorialOriginalScale * Mathf.Lerp(1f, 1.07f, num);
		}
	}

	private void CompleteOpeningTutorial()
	{
		openingTutorialCompleted = true;
		openingTutorialStage = -1;
		RestoreOpeningTutorialTarget();
		SetText(hintText, hintMessage);
	}

	private void RestoreOpeningTutorialTarget()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)openingTutorialGraphic != (Object)null)
		{
			openingTutorialGraphic.color = openingTutorialOriginalColor;
			openingTutorialGraphic = null;
		}
		if ((Object)(object)openingTutorialRect != (Object)null)
		{
			((Transform)openingTutorialRect).localScale = openingTutorialOriginalScale;
			openingTutorialRect = null;
		}
	}

	private void HandleGameOver()
	{
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		CompleteOpeningTutorial();
		if ((Object)(object)unitSellPanel != (Object)null)
		{
			unitSellPanel.SetActive(false);
		}
		EnsureDefeatCinematicPanel();
		if (!((Object)(object)defeatCinematicPanel == (Object)null))
		{
			defeatCinematicActive = true;
			defeatCinematicTimer = 5f;
			defeatCinematicPanel.SetActive(true);
			defeatCinematicPanel.transform.SetAsLastSibling();
			if ((Object)(object)defeatCinematicCanvasGroup != (Object)null)
			{
				defeatCinematicCanvasGroup.alpha = 0f;
			}
			SetText(defeatCinematicTitleText, "패배");
			SetText(defeatCinematicSubtitleText, "방어선이 붕괴됐습니다");
			string value = (((Object)(object)gameController != (Object)null) ? ("ROUND " + Mathf.Max(1, gameController.CurrentRound) + "  |  " + gameController.LifeHudSummary) : "전투 종료");
			SetText(defeatCinematicDetailText, value);
			HandleBannerRequested("방어선 붕괴", new Color(1f, 0.38f, 0.24f), 1.2f);
		}
	}

	private void UpdateDefeatCinematic()
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		if (!defeatCinematicActive)
		{
			return;
		}
		defeatCinematicTimer -= Time.unscaledDeltaTime;
		float num = Mathf.Clamp01(1f - defeatCinematicTimer / 5f);
		if ((Object)(object)defeatCinematicPanel != (Object)null)
		{
			RectTransform component = defeatCinematicPanel.GetComponent<RectTransform>();
			if ((Object)(object)component != (Object)null)
			{
				float num2 = Mathf.Sin(num * MathF.PI);
				((Transform)component).localScale = Vector3.one * Mathf.Lerp(1.02f, 1.08f, num2 * 0.35f);
			}
		}
		if ((Object)(object)defeatCinematicCanvasGroup != (Object)null)
		{
			float num3 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.78f, num));
			float num4 = ((defeatCinematicTimer <= 0.25f) ? Mathf.Clamp01(defeatCinematicTimer / 0.25f) : 1f);
			defeatCinematicCanvasGroup.alpha = num3 * num4;
		}
		if (defeatCinematicTimer <= 0f)
		{
			EndDefeatCinematic(immediate: false);
		}
	}

	private void EndDefeatCinematic(bool immediate)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		if (defeatCinematicActive || immediate)
		{
			defeatCinematicActive = false;
			defeatCinematicTimer = 0f;
			if ((Object)(object)defeatCinematicPanel != (Object)null)
			{
				defeatCinematicPanel.SetActive(false);
				defeatCinematicPanel.transform.localScale = Vector3.one;
			}
		}
	}

	private void EnsureDefeatCinematicPanel()
	{
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Expected O, but got Unknown
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Expected O, but got Unknown
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		//IL_0399: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)defeatCinematicPanel != (Object)null))
		{
			Canvas val = (((Object)(object)roundBannerText != (Object)null) ? ((Graphic)roundBannerText).canvas : null);
			if ((Object)(object)val == (Object)null && (Object)(object)lifeProgressFill != (Object)null)
			{
				val = ((Graphic)lifeProgressFill).canvas;
			}
			if ((Object)(object)val == (Object)null)
			{
				val = Object.FindObjectOfType<Canvas>();
			}
			if ((Object)(object)val == (Object)null)
			{
				Debug.LogError((object)"[DefenseGame] Defeat cinematic could not find the gameplay Canvas.");
				return;
			}
			Transform transform = ((Component)val).transform;
			Font font = (((Object)(object)roundBannerText != (Object)null && (Object)(object)roundBannerText.font != (Object)null) ? roundBannerText.font : Resources.GetBuiltinResource<Font>("Arial.ttf"));
			defeatCinematicPanel = new GameObject("DefeatCinematicPanel", new Type[1] { typeof(RectTransform) });
			defeatCinematicPanel.transform.SetParent(transform, false);
			Canvas val2 = defeatCinematicPanel.AddComponent<Canvas>();
			val2.overrideSorting = true;
			val2.sortingOrder = 500;
			Image val3 = defeatCinematicPanel.AddComponent<Image>();
			((Graphic)val3).color = new Color(0.16f, 0.02f, 0.04f, 0.78f);
			((Graphic)val3).raycastTarget = false;
			defeatCinematicCanvasGroup = defeatCinematicPanel.AddComponent<CanvasGroup>();
			defeatCinematicCanvasGroup.blocksRaycasts = false;
			defeatCinematicCanvasGroup.interactable = false;
			RectTransform component = defeatCinematicPanel.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.pivot = new Vector2(0.5f, 0.5f);
			component.anchoredPosition = Vector2.zero;
			component.sizeDelta = Vector2.zero;
			GameObject val4 = new GameObject("DefeatCard", new Type[1] { typeof(RectTransform) });
			val4.transform.SetParent(defeatCinematicPanel.transform, false);
			Image val5 = val4.AddComponent<Image>();
			((Graphic)val5).color = new Color(0.28f, 0.02f, 0.08f, 0.84f);
			((Graphic)val5).raycastTarget = false;
			RectTransform component2 = val4.GetComponent<RectTransform>();
			component2.anchorMin = new Vector2(0.5f, 0.5f);
			component2.anchorMax = new Vector2(0.5f, 0.5f);
			component2.pivot = new Vector2(0.5f, 0.5f);
			component2.anchoredPosition = new Vector2(0f, 46f);
			component2.sizeDelta = new Vector2(720f, 300f);
			defeatCinematicTitleText = CreateDefeatCinematicText(val4.transform, font, "DefeatTitle", "패배", 76, new Color(1f, 0.3f, 0.22f), new Vector2(0f, 72f), new Vector2(650f, 94f), (TextAnchor)4, bold: true);
			defeatCinematicSubtitleText = CreateDefeatCinematicText(val4.transform, font, "DefeatSubtitle", "방어선이 붕괴됐습니다", 30, new Color(1f, 0.86f, 0.72f), new Vector2(0f, 2f), new Vector2(650f, 52f), (TextAnchor)4, bold: true);
			defeatCinematicDetailText = CreateDefeatCinematicText(val4.transform, font, "DefeatDetail", "ROUND 1  |  HP 0/20", 23, new Color(0.92f, 0.95f, 1f), new Vector2(0f, -62f), new Vector2(650f, 48f), (TextAnchor)4, bold: false);
			defeatCinematicPanel.SetActive(false);
		}
	}

	private Text CreateDefeatCinematicText(Transform parent, Font font, string name, string value, int fontSize, Color color, Vector2 position, Vector2 size, TextAnchor alignment, bool bold)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		Text val2 = val.AddComponent<Text>();
		val2.font = font;
		val2.fontSize = fontSize;
		val2.fontStyle = (FontStyle)(bold ? 1 : 0);
		val2.alignment = alignment;
		((Graphic)val2).color = color;
		val2.text = value;
		((Graphic)val2).raycastTarget = false;
		Shadow val3 = val.AddComponent<Shadow>();
		val3.effectColor = new Color(0f, 0f, 0f, 0.72f);
		val3.effectDistance = new Vector2(2f, -2f);
		RectTransform component = ((Component)val2).GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		return val2;
	}

	private void HandleMergeCompleted(MergeResultInfo result)
	{
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		mergeBannerMessage = result.BuildMessage();
		mergeBannerTimer = 2f;
		mergeCelebrationTimer = 0.8f;
		SetMergeBannerVisible(visible: true);
		if (result.isFinalMerge)
		{
			if (mergeBannerTextAvailable())
			{
				SetText(mergeResultText, "최종 합성  " + result.BuildMessage());
				SetColor(mergeResultText, result.resultColor);
			}
			SetText(mergeCelebrationText, "초월 각성!");
			SetColor(mergeCelebrationText, result.resultColor);
			SetText(mergeCelebrationSubText, result.BuildMessage());
			SetColor(mergeCelebrationSubText, new Color(1f, 0.98f, 0.9f, 0.92f));
			return;
		}
		string displayName = CharacterGradeUtility.GetDisplayName(result.sourceGrade);
		string displayName2 = CharacterGradeUtility.GetDisplayName(result.resultGrade);
		if (mergeBannerTextAvailable())
		{
			SetText(mergeResultText, "합성 결과  " + displayName + " -> " + displayName2 + "  " + result.resultCharacterName);
			SetColor(mergeResultText, result.resultColor);
		}
		SetText(mergeCelebrationText, "합성 성공!");
		SetColor(mergeCelebrationText, result.resultColor);
		SetText(mergeCelebrationSubText, displayName + " -> " + displayName2 + "  |  " + result.resultCharacterName);
		SetColor(mergeCelebrationSubText, new Color(1f, 0.98f, 0.9f, 0.92f));
	}

	private void HandleRoundCountdownChanged(int countdown)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)countdownText == (Object)null))
		{
			SetText(countdownText, (countdown > 0) ? countdown.ToString() : string.Empty);
			Color color = ((Graphic)countdownText).color;
			color.a = ((countdown > 0) ? 1f : 0f);
			((Graphic)countdownText).color = color;
		}
	}

	private void HandleRoundStarted(int round)
	{
		if (round > 0 && round % 10 == 0)
		{
			ShowBossWarning(round);
		}
	}

	private void HandleBannerRequested(string message, Color color, float duration)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)roundBannerText == (Object)null))
		{
			SetText(roundBannerText, message);
			((Graphic)roundBannerText).color = color;
			roundBannerTimer = duration;
		}
	}

	private void ShowBossWarning(int round)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)bossWarningPanel == (Object)null)
		{
			HandleBannerRequested("BOSS ROUND " + round, new Color(1f, 0.34f, 0.25f), 2.4f);
			return;
		}
		SetText(bossWarningTitleText, "보스 등장!");
		SetText(bossWarningSubText, "ROUND " + round + "  |  보스 타일과 군중제어 배치를 확인하세요");
		bossWarningTimer = 3.4f;
		bossWarningPanel.SetActive(true);
		if ((Object)(object)bossWarningCanvasGroup != (Object)null)
		{
			bossWarningCanvasGroup.alpha = 1f;
		}
		RectTransform component = bossWarningPanel.GetComponent<RectTransform>();
		if ((Object)(object)component != (Object)null)
		{
			((Transform)component).localScale = Vector3.one;
		}
	}

	private bool mergeBannerTextAvailable()
	{
		return (Object)(object)mergeResultText != (Object)null;
	}

	private void SetMergeBannerVisible(bool visible)
	{
		if (mergeBannerTextAvailable() && !((Object)(object)((Component)mergeResultText).transform == (Object)null))
		{
			GameObject val = (((Object)(object)((Component)mergeResultText).transform.parent != (Object)null) ? ((Component)((Component)mergeResultText).transform.parent).gameObject : ((Component)mergeResultText).gameObject);
			if ((Object)(object)val != (Object)null && val.activeSelf != visible)
			{
				val.SetActive(visible);
			}
		}
	}

	private void SetText(Text target, string value)
	{
		if ((Object)(object)target != (Object)null && target.text != value)
		{
			target.text = value;
		}
	}

	private static string BuildCompactBossGoalHud(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "전력 점검";
		}
		string text = value.Trim().Replace("초월 준비 완료: 초월 조합을 실행하세요.", "초월 READY").Replace("초월 목표: ", "초월 ")
			.Replace("보스 대비: ", "보스 대비 ")
			.Replace("시너지 목표: ", "시너지 ")
			.Replace("딜러 목표: ", "딜러 ")
			.Replace("초반 목표: ", "초반 ")
			.Replace(" 찾기", string.Empty);
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		text = text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ')
			.Trim();
		if (text.Length > 22)
		{
			text = text.Substring(0, 19) + "...";
		}
		return string.IsNullOrWhiteSpace(text) ? "전력 점검" : text;
	}

	private static string CompactHudLines(string value, int maxLines, int maxCharsPerLine)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		int num = Mathf.Max(1, maxLines);
		int num2 = Mathf.Max(4, maxCharsPerLine);
		string[] array = value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		string text = string.Empty;
		int num3 = 0;
		for (int i = 0; i < array.Length; i++)
		{
			if (num3 >= num)
			{
				break;
			}
			string text2 = ((array[i] != null) ? array[i].Trim() : string.Empty);
			if (!string.IsNullOrEmpty(text2))
			{
				if (text2.Length > num2)
				{
					text2 = text2.Substring(0, num2 - 3) + "...";
				}
				if (num3 > 0)
				{
					text += "\n";
				}
				text += text2;
				num3++;
			}
		}
		return string.IsNullOrEmpty(text) ? value.Trim() : text;
	}

	private void SetColor(Text target, Color value)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)target != (Object)null && ((Graphic)target).color != value)
		{
			((Graphic)target).color = value;
		}
	}

	private void SetInteractable(Button target, bool value)
	{
		if ((Object)(object)target != (Object)null && ((Selectable)target).interactable != value)
		{
			((Selectable)target).interactable = value;
		}
	}
}
