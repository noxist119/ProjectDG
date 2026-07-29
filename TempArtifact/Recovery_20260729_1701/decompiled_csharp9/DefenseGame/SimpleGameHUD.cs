using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame
{
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
			luckySummonProgressBadge = (((UnityEngine.Object)(object)luckySummonProgressText != null && ((Component)(object)luckySummonProgressText).transform.parent != null) ? ((Component)(object)luckySummonProgressText).transform.parent.GetComponent<Image>() : null);
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
			fateEntryButtonOutline = (((UnityEngine.Object)(object)fatePanelReopenButton != null) ? ((Component)(object)fatePanelReopenButton).GetComponent<Outline>() : null);
			battleButton = battle;
			summonButton = summon;
			summonButtonImage = (((UnityEngine.Object)(object)summonButton != null) ? ((Component)(object)summonButton).GetComponent<Image>() : null);
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
			if ((UnityEngine.Object)(object)lifeProgressFill != null && (int)lifeProgressFill.type == 3)
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
			if (!(gameController == null))
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
			if ((UnityEngine.Object)(object)lifeProgressFill == null)
			{
				return;
			}
			float ratio = ((gameController != null && gameController.MaxLife > 0) ? Mathf.Clamp01((float)gameController.Life / (float)gameController.MaxLife) : 0f);
			if ((int)lifeProgressFill.type == 3)
			{
				lifeProgressFill.fillAmount = ratio;
				return;
			}
			RectTransform rect = ((Graphic)lifeProgressFill).rectTransform;
			if (!(rect == null))
			{
				rect.anchorMin = Vector2.zero;
				rect.anchorMax = new Vector2(ratio, 1f);
				rect.offsetMin = Vector2.zero;
				rect.offsetMax = Vector2.zero;
			}
		}

		private void Subscribe()
		{
			if (!(gameController == null))
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
				if (boardManager != null)
				{
					boardManager.OnSelectedUnitChanged -= HandleSelectedUnitChanged;
					boardManager.OnSelectedUnitChanged += HandleSelectedUnitChanged;
				}
			}
		}

		private void Unsubscribe()
		{
			if (gameController != null)
			{
				gameController.OnStateChanged -= Refresh;
				gameController.OnMergeCompleted -= HandleMergeCompleted;
				gameController.OnRoundCountdownChanged -= HandleRoundCountdownChanged;
				gameController.OnBannerRequested -= HandleBannerRequested;
				gameController.OnRoundStarted -= HandleRoundStarted;
				gameController.OnGameOver -= HandleGameOver;
			}
			if (boardManager != null)
			{
				boardManager.OnSelectedUnitChanged -= HandleSelectedUnitChanged;
			}
		}

		private void WireUnitSellButton()
		{
			if (!((UnityEngine.Object)(object)unitSellButton == null))
			{
				((UnityEvent)(object)unitSellButton.onClick).RemoveListener((UnityAction)HandleSellButtonPressed);
				((UnityEvent)(object)unitSellButton.onClick).AddListener((UnityAction)HandleSellButtonPressed);
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
			if (!(pendingSellConfirmUnit == null) && !(Time.unscaledTime <= pendingSellConfirmExpireTime))
			{
				pendingSellConfirmUnit = null;
				pendingSellConfirmExpireTime = 0f;
				RefreshUnitSellPanel();
			}
		}

		private void RefreshUnitSellPanel()
		{
			if (unitSellPanel == null)
			{
				return;
			}
			bool show = selectedUnit != null && selectedUnit.CurrentSlot != null;
			unitSellPanel.SetActive(show);
			if ((UnityEngine.Object)(object)hintText != null)
			{
				((Component)(object)hintText).gameObject.SetActive(!show);
			}
			if (show)
			{
				string unitName = ((selectedUnit.Definition != null && !string.IsNullOrWhiteSpace(selectedUnit.Definition.displayName)) ? selectedUnit.Definition.displayName : "선택 유닛");
				int refund = ((gameController != null) ? gameController.GetUnitSellRefund(selectedUnit) : 0);
				string blockReason = string.Empty;
				bool canSell = gameController != null && gameController.CanSellUnit(selectedUnit, out blockReason);
				bool mergeCandidate = gameController != null && gameController.IsUnitSellMergeCandidate(selectedUnit);
				SetText(unitSellTitleText, unitName + " 선택됨");
				string detail = ((gameController != null) ? gameController.GetUnitSellDetail(selectedUnit) : "판매 정보 확인 중");
				if (!canSell && !string.IsNullOrWhiteSpace(blockReason))
				{
					detail = detail + "  |  " + blockReason;
				}
				else if (mergeCandidate)
				{
					detail += "  |  판매 전 합성 재료인지 확인";
				}
				SetText(unitSellDetailText, detail);
				SetText(unitSellButtonText, "판매 +" + refund + "G");
				SetInteractable(unitSellButton, canSell);
			}
		}

		private void HandleSellButtonPressed()
		{
			if (gameController == null || selectedUnit == null)
			{
				RefreshUnitSellPanel();
				return;
			}
			if (!gameController.CanSellUnit(selectedUnit, out var blockReason))
			{
				gameController.RequestBanner(blockReason, new Color(1f, 0.42f, 0.3f), 1.7f);
				RefreshUnitSellPanel();
				return;
			}
			bool requiresConfirm = gameController.UnitSellRequiresConfirmation(selectedUnit);
			bool confirmed = pendingSellConfirmUnit == selectedUnit && Time.unscaledTime <= pendingSellConfirmExpireTime;
			int refund;
			string message;
			if (requiresConfirm && !confirmed)
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
			if (gameController == null)
			{
				return;
			}
			bool roundRunning = gameController.IsRoundRunning;
			bool combatLocked = gameController.IsCombatInteractionLocked;
			bool fateEditing = gameController.FateCombatEditingActive;
			SetText(stateText, fateEditing ? "계약 편집 중" : (combatLocked ? "전투 진행 중" : (roundRunning ? "전투 준비" : "준비 단계")));
			SetColor(stateText, fateEditing ? new Color(1f, 0.54f, 1f) : (combatLocked ? new Color(1f, 0.82f, 0.36f) : new Color(0.42f, 1f, 0.72f)));
			string battleLabel = ((!roundRunning) ? "다음 라운드" : (combatLocked ? "전투 중" : "전투 준비"));
			SetText(battleButtonText, battleLabel);
			SetInteractable(battleButton, !roundRunning);
			bool luckySummonReady = gameController.LuckySummonReady;
			bool luckyChoiceOpen = gameController.LuckySummonChoiceOpen;
			bool canSummon = !combatLocked && gameController.Gold >= gameController.SummonCost && gameController.EmptySlotCount > 0;
			string summonLabel = (combatLocked ? "전투 중" : (luckyChoiceOpen ? "선택 중" : (luckySummonReady ? "행운 소환" : (canSummon ? "소환" : ((gameController.EmptySlotCount <= 0) ? "자리 없음" : "골드 부족")))));
			SetText(summonButtonText, summonLabel);
			SetText(summonCostText, gameController.SummonCost + " GOLD");
			SetText(luckySummonProgressText, luckySummonReady ? "행운 소환  READY" : (gameController.LuckySummonProgressVisible ? ("행운 소환  " + gameController.LuckySummonNormalStreak + " / " + gameController.LuckySummonThreshold) : string.Empty));
			SetColor(luckySummonProgressText, luckySummonReady ? new Color(0.2f, 0.13f, 0.05f) : new Color(0.94f, 1f, 0.78f));
			if ((UnityEngine.Object)(object)luckySummonProgressBadge != null)
			{
				((Component)(object)luckySummonProgressBadge).gameObject.SetActive(luckySummonReady || gameController.LuckySummonProgressVisible);
				((Graphic)luckySummonProgressBadge).color = (luckySummonReady ? new Color(0.96f, 0.72f, 0.22f, 0.98f) : new Color(0.07f, 0.2f, 0.17f, 0.96f));
			}
			if ((UnityEngine.Object)(object)summonButtonImage != null)
			{
				((Graphic)summonButtonImage).color = (luckySummonReady ? new Color(0.56f, 0.76f, 0.2f, 1f) : new Color(0.19f, 0.78f, 0.42f, 1f));
			}
			SetInteractable(summonButton, canSummon && !luckyChoiceOpen);
			SetText(deckSummaryText, "보유 유닛 " + gameController.BoardUnitCount + " / " + gameController.BoardCapacity);
			SetText(capacityText, gameController.EmptySlotCount + "칸 남음");
			SetMergeCount(normalMergeText, CharacterGrade.Normal);
			SetMergeCount(rareMergeText, CharacterGrade.Rare);
			SetMergeCount(epicMergeText, CharacterGrade.Epic);
			SetMergeCount(legendaryMergeText, CharacterGrade.Legendary);
			SetOwnedGradeCount(mythicMergeText, CharacterGrade.Mythic);
			if ((UnityEngine.Object)(object)transcendentMergeText != null)
			{
				int readyCount = gameController.ReadyUltimateRecipeCount;
				bool canOpenUltimateRecipes = !combatLocked;
				bool canMergeUltimate = canOpenUltimateRecipes && readyCount > 0;
				SetText(transcendentMergeText, (readyCount > 0) ? ("READY ×" + readyCount) : gameController.GetUltimateMergeStatus());
				SetColor(transcendentMergeText, canMergeUltimate ? new Color(0.92f, 0.42f, 1f) : (combatLocked ? new Color(0.58f, 0.62f, 0.78f) : Color.white));
				SetGradeCardInteractable(transcendentMergeText, canOpenUltimateRecipes);
				SetUltimateReadyState(readyCount);
			}
			if ((UnityEngine.Object)(object)ultimateRecipeHudText != null)
			{
				bool canMergeUltimate2 = gameController.CanMergeUltimate();
				string status = gameController.GetUltimateRecipeBingoStatus();
				if (string.IsNullOrWhiteSpace(status))
				{
					status = gameController.GetUltimateMergeDetailStatus();
				}
				SetText(ultimateRecipeHudText, CompactHudLines(string.IsNullOrWhiteSpace(status) ? "초월 레시피 확인 중" : status, 2, 44));
				SetColor(ultimateRecipeHudText, canMergeUltimate2 ? new Color(1f, 0.86f, 0.28f) : new Color(0.76f, 0.94f, 1f));
			}
			if ((UnityEngine.Object)(object)bossRoundHudText != null)
			{
				int nextBossRound = Mathf.Max(10, gameController.NextBossRound);
				int roundsLeft = Mathf.Max(0, gameController.RoundsUntilNextBoss);
				if (gameController.IsBossRound && gameController.IsRoundRunning)
				{
					SetText(bossRoundHudText, "보스 압박  " + gameController.CurrentBossPressureSummary);
					SetColor(bossRoundHudText, new Color(1f, 0.36f, 0.24f));
				}
				else
				{
					string bossRoundSummary = ((roundsLeft > 0) ? ("보스 R" + nextBossRound + "까지 " + roundsLeft + "R") : ("보스 R" + nextBossRound));
					SetText(bossRoundHudText, bossRoundSummary + "  |  " + BuildCompactBossGoalHud(gameController.CurrentBuildGoalSummary));
					SetColor(bossRoundHudText, (roundsLeft <= 1) ? new Color(1f, 0.45f, 0.28f) : ((roundsLeft <= 3) ? new Color(1f, 0.86f, 0.28f) : new Color(0.76f, 0.94f, 1f)));
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
			if ((UnityEngine.Object)(object)roundProgressFill != null)
			{
				float fill = gameController.RoundProgress01;
				if (!Mathf.Approximately(roundProgressFill.fillAmount, fill))
				{
					roundProgressFill.fillAmount = fill;
				}
			}
			RefreshUnitSellPanel();
		}

		private void RefreshFateControls()
		{
			if (!(gameController == null))
			{
				bool shouldShowFatePanel = gameController.ShouldShowFatePanel;
				bool shouldShowFateEntryButton = gameController.ShouldShowFateCardEntryButton;
				UpdateFatePanelAvailability(shouldShowFatePanel, shouldShowFateEntryButton);
				if ((UnityEngine.Object)(object)fateGaugeFill != null)
				{
					fateGaugeFill.fillAmount = gameController.FateGauge01;
				}
				SetText(fateGaugeText, gameController.FateHudSummary);
				SetText(fateDebtText, gameController.FateCardStatusSummary);
				SetText(fateCostBenefitText, CompactHudLines(gameController.FateCostBenefitSummary, 3, 32));
				if (!shouldShowFatePanel)
				{
					string lockedLabel = (shouldShowFateEntryButton ? "봉인\n카드 개방 후 공개" : "봉인\n전투 중 공개");
					string cardLabel = (shouldShowFateEntryButton ? "운명카드\n꺼내기 대기" : "운명카드\n전투 중 개방");
					SetText(fateGradeLockButtonText, lockedLabel);
					SetText(fateNormalBanButtonText, lockedLabel);
					SetText(fateForceShopButtonText, cardLabel);
					SetText(fateSurvivalButtonText, cardLabel);
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
			if (!((UnityEngine.Object)(object)fatePanelReopenButton == null))
			{
				((UnityEvent)(object)fatePanelReopenButton.onClick).RemoveListener((UnityAction)ExpandFatePanel);
				((UnityEvent)(object)fatePanelReopenButton.onClick).RemoveListener((UnityAction)HandleFateEntryButtonPressed);
				((UnityEvent)(object)fatePanelReopenButton.onClick).AddListener((UnityAction)HandleFateEntryButtonPressed);
				((Component)(object)fatePanelReopenButton).gameObject.SetActive(value: false);
			}
		}

		private void InitializeFatePanelMotionIfNeeded()
		{
			if (!fatePanelMotionInitialized && !(fatePanelRoot == null))
			{
				fatePanelRect = fatePanelRoot.GetComponent<RectTransform>();
				if (fatePanelCanvasGroup == null)
				{
					fatePanelCanvasGroup = fatePanelRoot.GetComponent<CanvasGroup>();
				}
				if (fatePanelRect != null)
				{
					fatePanelRect.anchorMin = new Vector2(0.5f, 0.5f);
					fatePanelRect.anchorMax = new Vector2(0.5f, 0.5f);
					fatePanelRect.pivot = new Vector2(0.5f, 0.5f);
					fatePanelOpenPosition = Vector2.zero;
					RectTransform parentRect = fatePanelRect.parent as RectTransform;
					float parentHeight = ((parentRect != null) ? parentRect.rect.height : 1920f);
					fatePanelClosedPosition = fatePanelOpenPosition + new Vector2(0f, 0f - (parentHeight * 0.5f + fatePanelRect.rect.height + 226f));
				}
				if (fateChoiceBackdrop == null && fatePanelRoot.transform.parent != null)
				{
					fateChoiceBackdrop = new GameObject("FateChoiceBackdrop", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
					fateChoiceBackdrop.transform.SetParent(fatePanelRoot.transform.parent, worldPositionStays: false);
					RectTransform backdropRect = fateChoiceBackdrop.GetComponent<RectTransform>();
					backdropRect.anchorMin = Vector2.zero;
					backdropRect.anchorMax = Vector2.one;
					backdropRect.offsetMin = Vector2.zero;
					backdropRect.offsetMax = Vector2.zero;
					Image backdropImage = fateChoiceBackdrop.GetComponent<Image>();
					((Graphic)backdropImage).color = new Color(0.01f, 0.01f, 0.04f, 0.72f);
					((Graphic)backdropImage).raycastTarget = true;
					fateChoiceBackdropCanvasGroup = fateChoiceBackdrop.GetComponent<CanvasGroup>();
					fateChoiceBackdropCanvasGroup.alpha = 0f;
					fateChoiceBackdropCanvasGroup.blocksRaycasts = false;
					fateChoiceBackdropCanvasGroup.interactable = false;
					fateChoiceBackdrop.transform.SetSiblingIndex(Mathf.Max(0, fatePanelRoot.transform.GetSiblingIndex()));
					fatePanelRoot.transform.SetAsLastSibling();
				}
				fatePanelVisible = fatePanelRoot.activeSelf;
				fatePanelTargetOpen = fatePanelVisible;
				if (fatePanelCanvasGroup != null)
				{
					fatePanelCanvasGroup.alpha = (fatePanelVisible ? 1f : 0f);
					fatePanelCanvasGroup.interactable = fatePanelVisible;
					fatePanelCanvasGroup.blocksRaycasts = fatePanelVisible;
				}
				if (fateChoiceBackdrop != null)
				{
					fateChoiceBackdrop.SetActive(fatePanelVisible);
				}
				fatePanelMotionInitialized = true;
			}
		}

		private void UpdateFatePanelAvailability(bool shouldShow, bool shouldShowEntryButton)
		{
			InitializeFatePanelMotionIfNeeded();
			if (shouldShow)
			{
				if (fatePanelRoot != null && !fatePanelRoot.activeSelf)
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
			if ((UnityEngine.Object)(object)fatePanelReopenButton != null)
			{
				bool showReopen = shouldShowEntryButton && !shouldShow;
				if (!showReopen)
				{
					showReopen = shouldShow && !fatePanelTargetOpen;
				}
				((Component)(object)fatePanelReopenButton).gameObject.SetActive(showReopen);
				fateEntryButtonEmphasisActive = shouldShowEntryButton && !shouldShow && showReopen && gameController != null && gameController.FateSurvivalCrisisActive;
				SetText(fatePanelReopenButtonText, (shouldShowEntryButton && !shouldShow) ? "운명 카드\n꺼내기" : "계약");
			}
		}

		private void HandleFateEntryButtonPressed()
		{
			if (!(gameController == null) && gameController.TryOpenFateCardChoicePanel())
			{
				ExpandFatePanel();
			}
		}

		private void ExpandFatePanel()
		{
			InitializeFatePanelMotionIfNeeded();
			fatePanelTargetOpen = true;
			fatePanelVisible = true;
			if (fateChoiceBackdrop != null)
			{
				fateChoiceBackdrop.SetActive(value: true);
				fateChoiceBackdrop.transform.SetSiblingIndex(Mathf.Max(0, fatePanelRoot.transform.GetSiblingIndex()));
			}
			fatePanelRoot?.transform.SetAsLastSibling();
			if (fateChoiceBackdropCanvasGroup != null)
			{
				fateChoiceBackdropCanvasGroup.blocksRaycasts = true;
				fateChoiceBackdropCanvasGroup.interactable = true;
			}
			if (fatePanelRoot != null && !fatePanelRoot.activeSelf)
			{
				fatePanelRoot.SetActive(value: true);
			}
			if (fatePanelCanvasGroup != null)
			{
				fatePanelCanvasGroup.interactable = true;
				fatePanelCanvasGroup.blocksRaycasts = true;
			}
			if ((UnityEngine.Object)(object)fatePanelReopenButton != null)
			{
				((Component)(object)fatePanelReopenButton).gameObject.SetActive(value: false);
			}
		}

		private void CollapseFatePanel()
		{
			InitializeFatePanelMotionIfNeeded();
			fatePanelTargetOpen = false;
			fatePanelVisible = true;
			if (fatePanelRoot != null && !fatePanelRoot.activeSelf)
			{
				fatePanelRoot.SetActive(value: true);
			}
			if (fatePanelCanvasGroup != null)
			{
				fatePanelCanvasGroup.interactable = false;
				fatePanelCanvasGroup.blocksRaycasts = false;
			}
		}

		private void UpdateFateEntryButtonEmphasis()
		{
			if ((UnityEngine.Object)(object)fatePanelReopenButton == null)
			{
				return;
			}
			if (!fateEntryButtonVisualInitialized)
			{
				fateEntryButtonVisualInitialized = true;
				fateEntryButtonBaseScale = ((Component)(object)fatePanelReopenButton).transform.localScale;
				fateEntryButtonBaseColor = FateEntryIdleColor;
				Graphic graphic = (((UnityEngine.Object)(object)((Selectable)fatePanelReopenButton).targetGraphic != null) ? ((Selectable)fatePanelReopenButton).targetGraphic : ((Component)(object)fatePanelReopenButton).GetComponent<Graphic>());
				if ((UnityEngine.Object)(object)graphic != null)
				{
					((Selectable)fatePanelReopenButton).targetGraphic = graphic;
					graphic.color = fateEntryButtonBaseColor;
				}
			}
			if ((UnityEngine.Object)(object)fateEntryButtonOutline != null)
			{
				((Shadow)fateEntryButtonOutline).effectColor = FateEntryOutlineColor;
			}
			Graphic targetGraphic = (((UnityEngine.Object)(object)((Selectable)fatePanelReopenButton).targetGraphic != null) ? ((Selectable)fatePanelReopenButton).targetGraphic : ((Component)(object)fatePanelReopenButton).GetComponent<Graphic>());
			if (!fateEntryButtonEmphasisActive || !((Component)(object)fatePanelReopenButton).gameObject.activeInHierarchy)
			{
				((Component)(object)fatePanelReopenButton).transform.localScale = fateEntryButtonBaseScale;
				if ((UnityEngine.Object)(object)targetGraphic != null)
				{
					targetGraphic.color = fateEntryButtonBaseColor;
				}
				if ((UnityEngine.Object)(object)fatePanelReopenButtonText != null)
				{
					SetColor(fatePanelReopenButtonText, FateEntryTextColor);
				}
				return;
			}
			float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.6f);
			((Component)(object)fatePanelReopenButton).transform.localScale = fateEntryButtonBaseScale * Mathf.Lerp(1f, 1.07f, pulse);
			if ((UnityEngine.Object)(object)targetGraphic != null)
			{
				targetGraphic.color = Color.Lerp(fateEntryButtonBaseColor, FateEntryCrisisColor, 0.28f + pulse * 0.42f);
			}
			if ((UnityEngine.Object)(object)fatePanelReopenButtonText != null)
			{
				SetColor(fatePanelReopenButtonText, FateEntryTextColor);
			}
			if ((UnityEngine.Object)(object)fateEntryButtonOutline != null)
			{
				((Shadow)fateEntryButtonOutline).effectColor = Color.Lerp(FateEntryOutlineColor, FateEntryCrisisOutlineColor, 0.3f + pulse * 0.7f);
			}
		}

		private void UpdateFatePanelMotion()
		{
			InitializeFatePanelMotionIfNeeded();
			if (fatePanelRect == null || fatePanelRoot == null || (!fatePanelRoot.activeSelf && !fatePanelTargetOpen))
			{
				return;
			}
			if (!fatePanelRoot.activeSelf)
			{
				fatePanelRoot.SetActive(value: true);
			}
			Vector2 targetPosition = (fatePanelTargetOpen ? fatePanelOpenPosition : fatePanelClosedPosition);
			float slideT = 1f - Mathf.Exp(-13f * Time.unscaledDeltaTime);
			fatePanelRect.anchoredPosition = Vector2.Lerp(fatePanelRect.anchoredPosition, targetPosition, slideT);
			if (fatePanelCanvasGroup != null)
			{
				float targetAlpha = (fatePanelTargetOpen ? 1f : 0f);
				fatePanelCanvasGroup.alpha = Mathf.MoveTowards(fatePanelCanvasGroup.alpha, targetAlpha, 6f * Time.unscaledDeltaTime);
			}
			if (fateChoiceBackdropCanvasGroup != null)
			{
				float targetBackdropAlpha = (fatePanelTargetOpen ? 1f : 0f);
				fateChoiceBackdropCanvasGroup.alpha = Mathf.MoveTowards(fateChoiceBackdropCanvasGroup.alpha, targetBackdropAlpha, 6f * Time.unscaledDeltaTime);
			}
			bool alphaReady = fatePanelCanvasGroup == null || fatePanelCanvasGroup.alpha <= 0.02f;
			if (!fatePanelTargetOpen && Vector2.Distance(fatePanelRect.anchoredPosition, fatePanelClosedPosition) <= 0.8f && alphaReady)
			{
				fatePanelRect.anchoredPosition = fatePanelClosedPosition;
				fatePanelVisible = false;
				fatePanelRoot.SetActive(value: false);
				if (fateChoiceBackdrop != null)
				{
					fateChoiceBackdrop.SetActive(value: false);
				}
				return;
			}
			bool openedAlphaReady = fatePanelCanvasGroup == null || fatePanelCanvasGroup.alpha >= 0.98f;
			if (fatePanelTargetOpen && Vector2.Distance(fatePanelRect.anchoredPosition, fatePanelOpenPosition) <= 0.8f && openedAlphaReady)
			{
				fatePanelRect.anchoredPosition = fatePanelOpenPosition;
				fatePanelVisible = true;
				if (fatePanelCanvasGroup != null)
				{
					fatePanelCanvasGroup.alpha = 1f;
					fatePanelCanvasGroup.interactable = true;
					fatePanelCanvasGroup.blocksRaycasts = true;
				}
			}
		}

		private void ApplyFateChoiceButtonVisuals()
		{
			if (!(gameController == null))
			{
				ApplyFateChoiceButtonColor(fateSurvivalButton, gameController.FateSurvivalHudColor, isSurvivalButton: true);
				ApplyFateChoiceButtonColor(fateGradeLockButton, gameController.FateGradeLockHudColor, isSurvivalButton: false);
				ApplyFateChoiceButtonColor(fateNormalBanButton, gameController.FateNormalBanHudColor, isSurvivalButton: false);
			}
		}

		private void ApplyFateChoiceButtonColor(Button target, Color color, bool isSurvivalButton)
		{
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			if (!((UnityEngine.Object)(object)target == null))
			{
				Graphic targetGraphic = ((Selectable)target).targetGraphic;
				if ((UnityEngine.Object)(object)targetGraphic != null && targetGraphic.color != color)
				{
					targetGraphic.color = color;
				}
				ColorBlock colors = ((Selectable)target).colors;
				Color highlighted = Color.Lerp(color, Color.white, 0.16f);
				Color pressed = Color.Lerp(color, Color.black, 0.18f);
				Color disabled = new Color(color.r * 0.34f, color.g * 0.34f, color.b * 0.34f, 0.48f);
				((ColorBlock)(ref colors)).normalColor = color;
				((ColorBlock)(ref colors)).highlightedColor = highlighted;
				((ColorBlock)(ref colors)).selectedColor = highlighted;
				((ColorBlock)(ref colors)).pressedColor = pressed;
				((ColorBlock)(ref colors)).disabledColor = disabled;
				((Selectable)target).colors = colors;
				if (isSurvivalButton && fateSurvivalVisualInitialized)
				{
					fateSurvivalBaseColor = color;
				}
			}
		}

		private void ApplyFateSurvivalEmphasis(bool active)
		{
			if ((UnityEngine.Object)(object)fateSurvivalButton == null)
			{
				return;
			}
			if (!fateSurvivalVisualInitialized)
			{
				fateSurvivalBaseScale = ((Component)(object)fateSurvivalButton).transform.localScale;
				Graphic graphic = ((Selectable)fateSurvivalButton).targetGraphic;
				fateSurvivalBaseColor = (((UnityEngine.Object)(object)graphic != null) ? graphic.color : Color.white);
				fateSurvivalVisualInitialized = true;
			}
			Graphic targetGraphic = ((Selectable)fateSurvivalButton).targetGraphic;
			if (active)
			{
				float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 8f) * 0.5f;
				((Component)(object)fateSurvivalButton).transform.localScale = fateSurvivalBaseScale * Mathf.Lerp(1.04f, 1.16f, pulse);
				if ((UnityEngine.Object)(object)targetGraphic != null)
				{
					targetGraphic.color = Color.Lerp(new Color(1f, 0.36f, 0.14f, 1f), new Color(1f, 0.88f, 0.2f, 1f), pulse);
				}
				SetColor(fateSurvivalButtonText, Color.white);
			}
			else
			{
				((Component)(object)fateSurvivalButton).transform.localScale = fateSurvivalBaseScale;
				if ((UnityEngine.Object)(object)targetGraphic != null && targetGraphic.color != fateSurvivalBaseColor)
				{
					targetGraphic.color = fateSurvivalBaseColor;
				}
			}
		}

		private void SetMergeCount(Text target, CharacterGrade grade)
		{
			if (!((UnityEngine.Object)(object)target == null) && !(gameController == null))
			{
				int count = gameController.CountUnitsOfGrade(grade);
				bool combatLocked = gameController.IsCombatInteractionLocked;
				bool canMerge = !combatLocked && count >= 3;
				SetText(target, count + " / 3");
				SetColor(target, canMerge ? new Color(0.42f, 1f, 0.72f) : (combatLocked ? new Color(0.58f, 0.62f, 0.78f) : Color.white));
				SetGradeCardInteractable(target, value: true);
			}
		}

		private void SetOwnedGradeCount(Text target, CharacterGrade grade)
		{
			if (!((UnityEngine.Object)(object)target == null) && !(gameController == null))
			{
				int count = gameController.CountUnitsOfGrade(grade);
				SetText(target, count + "개");
				SetColor(target, (count > 0) ? CharacterGradeUtility.GetColor(grade, Color.white) : Color.white);
			}
		}

		private void SetGradeCardInteractable(Text target, bool value)
		{
			Button button = (((UnityEngine.Object)(object)target != null && ((Component)(object)target).transform != null) ? ((Component)(object)target).transform.GetComponentInParent<Button>() : null);
			if ((UnityEngine.Object)(object)button != null && ((Selectable)button).interactable != value)
			{
				((Selectable)button).interactable = value;
			}
		}

		private void SetUltimateReadyState(int readyCount)
		{
			EnsureUltimateReadyVisuals();
			ultimateReadyCount = Mathf.Max(0, readyCount);
			if ((UnityEngine.Object)(object)ultimateReadyBadge != null)
			{
				((Component)(object)ultimateReadyBadge).gameObject.SetActive(ultimateReadyCount > 0);
			}
			if ((UnityEngine.Object)(object)ultimateReadyBadgeText != null)
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
			if ((UnityEngine.Object)(object)ultimateMergeButton != null || (UnityEngine.Object)(object)transcendentMergeText == null)
			{
				return;
			}
			ultimateMergeButton = ((Component)(object)transcendentMergeText).GetComponentInParent<Button>();
			if (!((UnityEngine.Object)(object)ultimateMergeButton == null))
			{
				ultimateMergeBaseScale = ((Component)(object)ultimateMergeButton).transform.localScale;
				string[] lineNames = new string[4] { "ReadyGlowTop", "ReadyGlowRight", "ReadyGlowBottom", "ReadyGlowLeft" };
				ultimateReadyLines = (Image[])(object)new Image[lineNames.Length];
				for (int i = 0; i < lineNames.Length; i++)
				{
					Transform line = ((Component)(object)ultimateMergeButton).transform.Find(lineNames[i]);
					ultimateReadyLines[i] = ((line != null) ? line.GetComponent<Image>() : null);
				}
				Transform badge = ((Component)(object)ultimateMergeButton).transform.Find("ReadyBadge");
				ultimateReadyBadge = ((badge != null) ? badge.GetComponent<Image>() : null);
				Transform badgeText = ((badge != null) ? badge.Find("ReadyBadgeText") : null);
				ultimateReadyBadgeText = ((badgeText != null) ? badgeText.GetComponent<Text>() : null);
			}
		}

		private void UpdateUltimateReadyEmphasis()
		{
			EnsureUltimateReadyVisuals();
			if ((UnityEngine.Object)(object)ultimateMergeButton == null)
			{
				return;
			}
			bool active = ultimateReadyCount > 0;
			if (ultimateReadyLines != null)
			{
				Color gold = new Color(1f, 0.86f, 0.22f, 1f);
				Color purple = new Color(0.92f, 0.32f, 1f, 1f);
				for (int i = 0; i < ultimateReadyLines.Length; i++)
				{
					Image line = ultimateReadyLines[i];
					if (!((UnityEngine.Object)(object)line == null))
					{
						((Component)(object)line).gameObject.SetActive(active);
						if (active)
						{
							float wave = 0.5f + Mathf.Sin(Time.unscaledTime * 7.2f - (float)i * MathF.PI * 0.5f) * 0.5f;
							Color color = Color.Lerp(gold, purple, Mathf.PingPong(Time.unscaledTime * 0.55f + (float)i * 0.22f, 1f));
							color.a = Mathf.Lerp(0.22f, 1f, wave * wave);
							((Graphic)line).color = color;
						}
					}
				}
			}
			if (active)
			{
				float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 5.2f) * 0.5f;
				((Component)(object)ultimateMergeButton).transform.localScale = ultimateMergeBaseScale * Mathf.Lerp(1.02f, 1.07f, pulse);
			}
			else
			{
				((Component)(object)ultimateMergeButton).transform.localScale = ultimateMergeBaseScale;
			}
		}

		private void ResetUltimateReadyVisuals()
		{
			if ((UnityEngine.Object)(object)ultimateMergeButton != null)
			{
				((Component)(object)ultimateMergeButton).transform.localScale = ultimateMergeBaseScale;
			}
			if (ultimateReadyLines == null)
			{
				return;
			}
			for (int i = 0; i < ultimateReadyLines.Length; i++)
			{
				if ((UnityEngine.Object)(object)ultimateReadyLines[i] != null)
				{
					((Component)(object)ultimateReadyLines[i]).gameObject.SetActive(value: false);
				}
			}
		}

		private void SetInsightVisible(Text target, bool visible)
		{
			if (!((UnityEngine.Object)(object)target == null) && !(((Component)(object)target).transform == null) && !(((Component)(object)target).transform.parent == null))
			{
				((Component)(object)target).transform.parent.gameObject.SetActive(visible);
			}
		}

		private void UpdateMergeBanner()
		{
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
			if (!((UnityEngine.Object)(object)roundBannerText == null))
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
			if ((UnityEngine.Object)(object)mergeCelebrationText == null)
			{
				return;
			}
			if (mergeCelebrationTimer > 0f)
			{
				mergeCelebrationTimer -= Time.deltaTime;
				float normalized = Mathf.Clamp01(mergeCelebrationTimer / 0.8f);
				float scale = Mathf.Lerp(1f, 1.18f, normalized);
				RectTransform titleRect = ((Component)(object)mergeCelebrationText).GetComponent<RectTransform>();
				if (titleRect != null)
				{
					titleRect.localScale = Vector3.one * scale;
				}
				Color titleColor = ((Graphic)mergeCelebrationText).color;
				titleColor.a = Mathf.Lerp(0.1f, 1f, normalized);
				((Graphic)mergeCelebrationText).color = titleColor;
				if ((UnityEngine.Object)(object)mergeCelebrationSubText != null)
				{
					RectTransform subRect = ((Component)(object)mergeCelebrationSubText).GetComponent<RectTransform>();
					if (subRect != null)
					{
						subRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.08f, normalized);
					}
					Color subColor = ((Graphic)mergeCelebrationSubText).color;
					subColor.a = Mathf.Lerp(0.05f, 0.92f, normalized);
					((Graphic)mergeCelebrationSubText).color = subColor;
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
				RectTransform titleRect2 = ((Component)(object)mergeCelebrationText).GetComponent<RectTransform>();
				if (titleRect2 != null)
				{
					titleRect2.localScale = Vector3.one;
				}
				if ((UnityEngine.Object)(object)mergeCelebrationSubText != null)
				{
					SetText(mergeCelebrationSubText, string.Empty);
					((Graphic)mergeCelebrationSubText).color = new Color(((Graphic)mergeCelebrationSubText).color.r, ((Graphic)mergeCelebrationSubText).color.g, ((Graphic)mergeCelebrationSubText).color.b, 0f);
					RectTransform subRect2 = ((Component)(object)mergeCelebrationSubText).GetComponent<RectTransform>();
					if (subRect2 != null)
					{
						subRect2.localScale = Vector3.one;
					}
				}
			}
		}

		private void UpdateBossWarning()
		{
			if (bossWarningPanel == null)
			{
				return;
			}
			if (bossWarningTimer <= 0f)
			{
				if (bossWarningPanel.activeSelf)
				{
					bossWarningPanel.SetActive(value: false);
				}
				return;
			}
			bossWarningTimer -= Time.unscaledDeltaTime;
			float remaining01 = Mathf.Clamp01(bossWarningTimer / 3.4f);
			float elapsed01 = 1f - remaining01;
			float fadeIn = Mathf.Clamp01(elapsed01 / 0.16f);
			float fadeOut = Mathf.Clamp01(remaining01 / 0.22f);
			float alpha = Mathf.Min(fadeIn, fadeOut);
			if (bossWarningCanvasGroup != null)
			{
				bossWarningCanvasGroup.alpha = alpha;
			}
			RectTransform rect = bossWarningPanel.GetComponent<RectTransform>();
			if (rect != null)
			{
				float pulse = Mathf.Sin(elapsed01 * MathF.PI);
				rect.localScale = Vector3.one * Mathf.Lerp(0.96f, 1.06f, pulse);
			}
			if (bossWarningTimer <= 0f)
			{
				bossWarningPanel.SetActive(value: false);
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
			if (!enableOpeningTutorial || openingTutorialCompleted || gameController == null)
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
			float elapsed = Time.unscaledTime - openingTutorialStartTime;
			if (elapsed >= 10f)
			{
				CompleteOpeningTutorial();
				return;
			}
			int stage = Mathf.Clamp(Mathf.FloorToInt(elapsed / 2.5f), 0, 3);
			Transform target = GetOpeningTutorialTarget(stage);
			string message = GetOpeningTutorialMessage(stage);
			if (openingTutorialStage != stage)
			{
				openingTutorialStage = stage;
				HandleBannerRequested(message, new Color(1f, 0.88f, 0.22f), 2.2f);
			}
			PulseOpeningTutorialTarget(target);
			SetText(hintText, message);
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
				return ((UnityEngine.Object)(object)summonButton != null) ? ((Component)(object)summonButton).transform : null;
			case 1:
				return ((UnityEngine.Object)(object)battleButton != null) ? ((Component)(object)battleButton).transform : null;
			case 2:
				return ((UnityEngine.Object)(object)normalMergeText != null && ((Component)(object)normalMergeText).transform.parent != null) ? ((Component)(object)normalMergeText).transform.parent : (((UnityEngine.Object)(object)normalMergeText != null) ? ((Component)(object)normalMergeText).transform : null);
			default:
				if ((UnityEngine.Object)(object)recipeInsightText != null && ((Component)(object)recipeInsightText).transform.parent != null)
				{
					return ((Component)(object)recipeInsightText).transform.parent;
				}
				if ((UnityEngine.Object)(object)bossRoundHudText != null && ((Component)(object)bossRoundHudText).transform.parent != null)
				{
					return ((Component)(object)bossRoundHudText).transform.parent;
				}
				return ((UnityEngine.Object)(object)mergeResultText != null && ((Component)(object)mergeResultText).transform.parent != null) ? ((Component)(object)mergeResultText).transform.parent : (((UnityEngine.Object)(object)mergeResultText != null) ? ((Component)(object)mergeResultText).transform : null);
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
			if (target == null)
			{
				RestoreOpeningTutorialTarget();
				return;
			}
			Graphic graphic = target.GetComponent<Graphic>();
			if ((UnityEngine.Object)(object)graphic == null)
			{
				graphic = target.GetComponentInChildren<Graphic>();
			}
			if ((UnityEngine.Object)(object)graphic == null)
			{
				RestoreOpeningTutorialTarget();
				return;
			}
			if ((UnityEngine.Object)(object)openingTutorialGraphic != (UnityEngine.Object)(object)graphic)
			{
				RestoreOpeningTutorialTarget();
				openingTutorialGraphic = graphic;
				openingTutorialOriginalColor = graphic.color;
				openingTutorialRect = target.GetComponent<RectTransform>();
				openingTutorialOriginalScale = ((openingTutorialRect != null) ? openingTutorialRect.localScale : Vector3.one);
			}
			float pulse = 0.35f + Mathf.PingPong(Time.unscaledTime * 3.2f, 0.55f);
			Color highlight = new Color(1f, 0.88f, 0.18f, openingTutorialOriginalColor.a);
			openingTutorialGraphic.color = Color.Lerp(openingTutorialOriginalColor, highlight, pulse);
			if (openingTutorialRect != null)
			{
				openingTutorialRect.localScale = openingTutorialOriginalScale * Mathf.Lerp(1f, 1.07f, pulse);
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
			if ((UnityEngine.Object)(object)openingTutorialGraphic != null)
			{
				openingTutorialGraphic.color = openingTutorialOriginalColor;
				openingTutorialGraphic = null;
			}
			if (openingTutorialRect != null)
			{
				openingTutorialRect.localScale = openingTutorialOriginalScale;
				openingTutorialRect = null;
			}
		}

		private void HandleGameOver()
		{
			CompleteOpeningTutorial();
			if (unitSellPanel != null)
			{
				unitSellPanel.SetActive(value: false);
			}
			EnsureDefeatCinematicPanel();
			if (!(defeatCinematicPanel == null))
			{
				defeatCinematicActive = true;
				defeatCinematicTimer = 5f;
				defeatCinematicPanel.SetActive(value: true);
				defeatCinematicPanel.transform.SetAsLastSibling();
				if (defeatCinematicCanvasGroup != null)
				{
					defeatCinematicCanvasGroup.alpha = 0f;
				}
				SetText(defeatCinematicTitleText, "패배");
				SetText(defeatCinematicSubtitleText, "방어선이 붕괴됐습니다");
				string detail = ((gameController != null) ? ("ROUND " + Mathf.Max(1, gameController.CurrentRound) + "  |  " + gameController.LifeHudSummary) : "전투 종료");
				SetText(defeatCinematicDetailText, detail);
				HandleBannerRequested("방어선 붕괴", new Color(1f, 0.38f, 0.24f), 1.2f);
			}
		}

		private void UpdateDefeatCinematic()
		{
			if (!defeatCinematicActive)
			{
				return;
			}
			defeatCinematicTimer -= Time.unscaledDeltaTime;
			float normalized = Mathf.Clamp01(1f - defeatCinematicTimer / 5f);
			if (defeatCinematicPanel != null)
			{
				RectTransform rect = defeatCinematicPanel.GetComponent<RectTransform>();
				if (rect != null)
				{
					float pulse = Mathf.Sin(normalized * MathF.PI);
					rect.localScale = Vector3.one * Mathf.Lerp(1.02f, 1.08f, pulse * 0.35f);
				}
			}
			if (defeatCinematicCanvasGroup != null)
			{
				float reveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.78f, normalized));
				float fade = ((defeatCinematicTimer <= 0.25f) ? Mathf.Clamp01(defeatCinematicTimer / 0.25f) : 1f);
				defeatCinematicCanvasGroup.alpha = reveal * fade;
			}
			if (defeatCinematicTimer <= 0f)
			{
				EndDefeatCinematic(immediate: false);
			}
		}

		private void EndDefeatCinematic(bool immediate)
		{
			if (defeatCinematicActive || immediate)
			{
				defeatCinematicActive = false;
				defeatCinematicTimer = 0f;
				if (defeatCinematicPanel != null)
				{
					defeatCinematicPanel.SetActive(value: false);
					defeatCinematicPanel.transform.localScale = Vector3.one;
				}
			}
		}

		private void EnsureDefeatCinematicPanel()
		{
			if (!(defeatCinematicPanel != null))
			{
				Canvas canvas = (((UnityEngine.Object)(object)roundBannerText != null) ? ((Graphic)roundBannerText).canvas : null);
				if (canvas == null && (UnityEngine.Object)(object)lifeProgressFill != null)
				{
					canvas = ((Graphic)lifeProgressFill).canvas;
				}
				if (canvas == null)
				{
					canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
				}
				if (canvas == null)
				{
					Debug.LogError("[DefenseGame] Defeat cinematic could not find the gameplay Canvas.");
					return;
				}
				Transform parent = canvas.transform;
				Font resolvedFont = (((UnityEngine.Object)(object)roundBannerText != null && roundBannerText.font != null) ? roundBannerText.font : Resources.GetBuiltinResource<Font>("Arial.ttf"));
				defeatCinematicPanel = new GameObject("DefeatCinematicPanel", typeof(RectTransform));
				defeatCinematicPanel.transform.SetParent(parent, worldPositionStays: false);
				Canvas defeatCanvas = defeatCinematicPanel.AddComponent<Canvas>();
				defeatCanvas.overrideSorting = true;
				defeatCanvas.sortingOrder = 500;
				Image blocker = defeatCinematicPanel.AddComponent<Image>();
				((Graphic)blocker).color = new Color(0.16f, 0.02f, 0.04f, 0.78f);
				((Graphic)blocker).raycastTarget = false;
				defeatCinematicCanvasGroup = defeatCinematicPanel.AddComponent<CanvasGroup>();
				defeatCinematicCanvasGroup.blocksRaycasts = false;
				defeatCinematicCanvasGroup.interactable = false;
				RectTransform rootRect = defeatCinematicPanel.GetComponent<RectTransform>();
				rootRect.anchorMin = Vector2.zero;
				rootRect.anchorMax = Vector2.one;
				rootRect.pivot = new Vector2(0.5f, 0.5f);
				rootRect.anchoredPosition = Vector2.zero;
				rootRect.sizeDelta = Vector2.zero;
				GameObject card = new GameObject("DefeatCard", typeof(RectTransform));
				card.transform.SetParent(defeatCinematicPanel.transform, worldPositionStays: false);
				Image cardImage = card.AddComponent<Image>();
				((Graphic)cardImage).color = new Color(0.28f, 0.02f, 0.08f, 0.84f);
				((Graphic)cardImage).raycastTarget = false;
				RectTransform cardRect = card.GetComponent<RectTransform>();
				cardRect.anchorMin = new Vector2(0.5f, 0.5f);
				cardRect.anchorMax = new Vector2(0.5f, 0.5f);
				cardRect.pivot = new Vector2(0.5f, 0.5f);
				cardRect.anchoredPosition = new Vector2(0f, 46f);
				cardRect.sizeDelta = new Vector2(720f, 300f);
				defeatCinematicTitleText = CreateDefeatCinematicText(card.transform, resolvedFont, "DefeatTitle", "패배", 76, new Color(1f, 0.3f, 0.22f), new Vector2(0f, 72f), new Vector2(650f, 94f), TextAnchor.MiddleCenter, bold: true);
				defeatCinematicSubtitleText = CreateDefeatCinematicText(card.transform, resolvedFont, "DefeatSubtitle", "방어선이 붕괴됐습니다", 30, new Color(1f, 0.86f, 0.72f), new Vector2(0f, 2f), new Vector2(650f, 52f), TextAnchor.MiddleCenter, bold: true);
				defeatCinematicDetailText = CreateDefeatCinematicText(card.transform, resolvedFont, "DefeatDetail", "ROUND 1  |  HP 0/20", 23, new Color(0.92f, 0.95f, 1f), new Vector2(0f, -62f), new Vector2(650f, 48f), TextAnchor.MiddleCenter, bold: false);
				defeatCinematicPanel.SetActive(value: false);
			}
		}

		private Text CreateDefeatCinematicText(Transform parent, Font font, string name, string value, int fontSize, Color color, Vector2 position, Vector2 size, TextAnchor alignment, bool bold)
		{
			GameObject textObject = new GameObject(name, typeof(RectTransform));
			textObject.transform.SetParent(parent, worldPositionStays: false);
			Text text = textObject.AddComponent<Text>();
			text.font = font;
			text.fontSize = fontSize;
			text.fontStyle = (bold ? FontStyle.Bold : FontStyle.Normal);
			text.alignment = alignment;
			((Graphic)text).color = color;
			text.text = value;
			((Graphic)text).raycastTarget = false;
			Shadow shadow = textObject.AddComponent<Shadow>();
			shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
			shadow.effectDistance = new Vector2(2f, -2f);
			RectTransform rect = ((Component)(object)text).GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.anchoredPosition = position;
			rect.sizeDelta = size;
			return text;
		}

		private void HandleMergeCompleted(MergeResultInfo result)
		{
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
			string sourceGrade = CharacterGradeUtility.GetDisplayName(result.sourceGrade);
			string resultGrade = CharacterGradeUtility.GetDisplayName(result.resultGrade);
			if (mergeBannerTextAvailable())
			{
				SetText(mergeResultText, "합성 결과  " + sourceGrade + " -> " + resultGrade + "  " + result.resultCharacterName);
				SetColor(mergeResultText, result.resultColor);
			}
			SetText(mergeCelebrationText, "합성 성공!");
			SetColor(mergeCelebrationText, result.resultColor);
			SetText(mergeCelebrationSubText, sourceGrade + " -> " + resultGrade + "  |  " + result.resultCharacterName);
			SetColor(mergeCelebrationSubText, new Color(1f, 0.98f, 0.9f, 0.92f));
		}

		private void HandleRoundCountdownChanged(int countdown)
		{
			if (!((UnityEngine.Object)(object)countdownText == null))
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
			if (!((UnityEngine.Object)(object)roundBannerText == null))
			{
				SetText(roundBannerText, message);
				((Graphic)roundBannerText).color = color;
				roundBannerTimer = duration;
			}
		}

		private void ShowBossWarning(int round)
		{
			if (bossWarningPanel == null)
			{
				HandleBannerRequested("BOSS ROUND " + round, new Color(1f, 0.34f, 0.25f), 2.4f);
				return;
			}
			SetText(bossWarningTitleText, "보스 등장!");
			SetText(bossWarningSubText, "ROUND " + round + "  |  보스 타일과 군중제어 배치를 확인하세요");
			bossWarningTimer = 3.4f;
			bossWarningPanel.SetActive(value: true);
			if (bossWarningCanvasGroup != null)
			{
				bossWarningCanvasGroup.alpha = 1f;
			}
			RectTransform rect = bossWarningPanel.GetComponent<RectTransform>();
			if (rect != null)
			{
				rect.localScale = Vector3.one;
			}
		}

		private bool mergeBannerTextAvailable()
		{
			return (UnityEngine.Object)(object)mergeResultText != null;
		}

		private void SetMergeBannerVisible(bool visible)
		{
			if (mergeBannerTextAvailable() && !(((Component)(object)mergeResultText).transform == null))
			{
				GameObject bannerRoot = ((((Component)(object)mergeResultText).transform.parent != null) ? ((Component)(object)mergeResultText).transform.parent.gameObject : ((Component)(object)mergeResultText).gameObject);
				if (bannerRoot != null && bannerRoot.activeSelf != visible)
				{
					bannerRoot.SetActive(visible);
				}
			}
		}

		private void SetText(Text target, string value)
		{
			if ((UnityEngine.Object)(object)target != null && target.text != value)
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
			string compact = value.Trim().Replace("초월 준비 완료: 초월 조합을 실행하세요.", "초월 READY").Replace("초월 목표: ", "초월 ")
				.Replace("보스 대비: ", "보스 대비 ")
				.Replace("시너지 목표: ", "시너지 ")
				.Replace("딜러 목표: ", "딜러 ")
				.Replace("초반 목표: ", "초반 ")
				.Replace(" 찾기", string.Empty);
			while (compact.Contains("  "))
			{
				compact = compact.Replace("  ", " ");
			}
			compact = compact.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ')
				.Trim();
			if (compact.Length > 22)
			{
				compact = compact.Substring(0, 19) + "...";
			}
			return string.IsNullOrWhiteSpace(compact) ? "전력 점검" : compact;
		}

		private static string CompactHudLines(string value, int maxLines, int maxCharsPerLine)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return value;
			}
			int safeMaxLines = Mathf.Max(1, maxLines);
			int safeMaxChars = Mathf.Max(4, maxCharsPerLine);
			string[] rawLines = value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
			string result = string.Empty;
			int count = 0;
			for (int i = 0; i < rawLines.Length; i++)
			{
				if (count >= safeMaxLines)
				{
					break;
				}
				string line = ((rawLines[i] != null) ? rawLines[i].Trim() : string.Empty);
				if (!string.IsNullOrEmpty(line))
				{
					if (line.Length > safeMaxChars)
					{
						line = line.Substring(0, safeMaxChars - 3) + "...";
					}
					if (count > 0)
					{
						result += "\n";
					}
					result += line;
					count++;
				}
			}
			return string.IsNullOrEmpty(result) ? value.Trim() : result;
		}

		private void SetColor(Text target, Color value)
		{
			if ((UnityEngine.Object)(object)target != null && ((Graphic)target).color != value)
			{
				((Graphic)target).color = value;
			}
		}

		private void SetInteractable(Button target, bool value)
		{
			if ((UnityEngine.Object)(object)target != null && ((Selectable)target).interactable != value)
			{
				((Selectable)target).interactable = value;
			}
		}
	}
}
