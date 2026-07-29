using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame;

public class RunShopSystem : MonoBehaviour
{
	private enum OfferType
	{
		RandomUnit,
		RareUnit,
		RiskChest,
		MergeAssist,
		TileReroll,
		BossIntel,
		Coupon,
		FieldMedic,
		FateMergeContract,
		FateBossContract,
		FateShopReroll,
		FateGradeLock,
		FateNormalBan,
		FateForceShop,
		RecoveryRareUnit,
		RecoveryBossPrep,
		InsuranceMergeMaterial,
		InsuranceRecoveryTicket,
		InsuranceBossCounter,
		AugmentPower,
		AugmentGuard,
		AugmentSkill
	}

	private sealed class ShopOffer
	{
		public OfferType type;

		public string title;

		public string description;

		public int cost;

		public string priceLabel;

		public Color color;

		public int debtCostPenalty;
	}

	[SerializeField]
	private DefenseGameController gameController;

	[SerializeField]
	private DefenseBoardManager boardManager;

	[SerializeField]
	private BoardTileModifierSystem tileModifierSystem;

	[SerializeField]
	private AugmentManager augmentManager;

	[SerializeField]
	private bool enableRegularShop = false;

	[SerializeField]
	private int firstShopRound = 11;

	[SerializeField]
	private int shopInterval = 8;

	[SerializeField]
	private bool enableEarlyMiniShop = true;

	[SerializeField]
	private int earlyMiniShopRound = 3;

	[SerializeField]
	private int earlyMiniShopOfferCount = 3;

	[SerializeField]
	private int miniShopInterval = 8;

	[Header("Opportunity Cost Pricing")]
	[SerializeField]
	[Range(0.5f, 1.5f)]
	private float regularShopOpportunityRate = 1.35f;

	[SerializeField]
	[Range(0.5f, 1.5f)]
	private float miniShopOpportunityRate = 1.15f;

	[SerializeField]
	[Range(0.5f, 1.5f)]
	private float recoveryShopOpportunityRate = 1.1f;

	[SerializeField]
	private bool enableEarlyRecoveryShop = true;

	[SerializeField]
	private int earlyRecoveryShopFirstRound = 4;

	[SerializeField]
	private int earlyRecoveryShopLastRound = 10;

	[SerializeField]
	private int earlyRecoveryShopOfferCount = 3;

	[SerializeField]
	private bool enableLuckyShopAppearances = true;

	[SerializeField]
	[Range(0f, 1f)]
	private float earlyMiniShopAppearanceChance = 1f;

	[SerializeField]
	[Range(0f, 1f)]
	private float earlyRecoveryShopAppearanceChance = 0.85f;

	[SerializeField]
	[Range(0f, 1f)]
	private float regularShopAppearanceChance = 0.65f;

	[SerializeField]
	[Range(0f, 1f)]
	private float missedShopChanceBonus = 0.18f;

	[SerializeField]
	[Range(0f, 1f)]
	private float maxLuckyShopAppearanceChance = 0.88f;

	[SerializeField]
	private int guaranteedEarlyMiniShopAfterMisses = 0;

	[SerializeField]
	private int guaranteedEarlyRecoveryShopAfterMisses = 1;

	[SerializeField]
	private int guaranteedRegularShopAfterMisses = 1;

	[SerializeField]
	private bool guaranteeFirstRegularShop = true;

	private GameObject panelRoot;

	private Text headerText;

	private Text subtitleText;

	private Button[] offerButtons;

	private Text[] offerTitleTexts;

	private Text[] offerDescriptionTexts;

	private Text[] offerPriceTexts;

	private Image[] offerAccentImages;

	private Button closeButton;

	private Button reopenButton;

	private Image modalImage;

	private Image headerPillImage;

	private Image topLineImage;

	private Image bottomLineImage;

	private Image leftRailImage;

	private Image rightRailImage;

	private Text footerHintText;

	private readonly List<ShopOffer> currentOffers = new List<ShopOffer>();

	private bool subscribed;

	private int lastMiniShopRound = -1;

	private bool earlyRecoveryShopShown;

	private bool currentShopIsMini;

	private bool currentShopIsRecovery;

	private bool currentShopIsInsurance;

	private bool currentRecoveryShopPurchaseRecorded;

	private int earlyMiniShopMisses;

	private int earlyRecoveryShopMisses;

	private int regularShopMisses;

	[SerializeField]
	[Range(3f, 12f)]
	private int recentOfferHistorySize = 6;

	private readonly List<OfferType> recentOfferHistory = new List<OfferType>();

	public void Configure(DefenseGameController controller, DefenseBoardManager board, BoardTileModifierSystem tileSystem, AugmentManager augments, GameObject root, Text header, Text subtitle, Button[] buttons, Text[] titles, Text[] descriptions, Text[] prices, Image[] accents, Button close, Button reopen = null)
	{
		Unsubscribe();
		gameController = controller;
		boardManager = board;
		tileModifierSystem = tileSystem;
		augmentManager = augments;
		panelRoot = root;
		headerText = header;
		subtitleText = subtitle;
		offerButtons = buttons;
		offerTitleTexts = titles;
		offerDescriptionTexts = descriptions;
		offerPriceTexts = prices;
		offerAccentImages = accents;
		closeButton = close;
		reopenButton = reopen;
		CacheVisualIdentityTargets();
		WireUi();
		Subscribe();
		SetOpen(open: false);
		UpdateReopenButton();
	}

	private void OnEnable()
	{
		Subscribe();
	}

	private void OnDisable()
	{
		Unsubscribe();
	}

	private void Subscribe()
	{
		if (!subscribed && !((Object)(object)gameController == (Object)null))
		{
			gameController.OnRoundShopPhase += HandleRoundShopPhase;
			gameController.OnRoundStarted += HandleRoundStarted;
			gameController.OnGameOver += HandleGameOver;
			subscribed = true;
		}
	}

	private void Unsubscribe()
	{
		if (subscribed && !((Object)(object)gameController == (Object)null))
		{
			gameController.OnRoundShopPhase -= HandleRoundShopPhase;
			gameController.OnRoundStarted -= HandleRoundStarted;
			gameController.OnGameOver -= HandleGameOver;
			subscribed = false;
		}
	}

	private void WireUi()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Expected O, but got Unknown
		if ((Object)(object)closeButton != (Object)null)
		{
			((UnityEvent)closeButton.onClick).RemoveListener(new UnityAction(Close));
			((UnityEvent)closeButton.onClick).AddListener(new UnityAction(Close));
		}
		if ((Object)(object)reopenButton != (Object)null)
		{
			((UnityEvent)reopenButton.onClick).RemoveListener(new UnityAction(Open));
			((UnityEvent)reopenButton.onClick).AddListener(new UnityAction(Open));
		}
		if (offerButtons == null)
		{
			return;
		}
		for (int i = 0; i < offerButtons.Length; i++)
		{
			int index = i;
			if (!((Object)(object)offerButtons[i] == (Object)null))
			{
				((UnityEventBase)offerButtons[i].onClick).RemoveAllListeners();
				((UnityEvent)offerButtons[i].onClick).AddListener((UnityAction)delegate
				{
					BuyOffer(index);
				});
			}
		}
	}

	private void HandleRoundShopPhase(int round)
	{
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		if (round <= 1)
		{
			lastMiniShopRound = -1;
			earlyRecoveryShopShown = false;
			currentShopIsMini = false;
			currentShopIsRecovery = false;
			currentShopIsInsurance = false;
			currentRecoveryShopPurchaseRecorded = false;
			earlyMiniShopMisses = 0;
			earlyRecoveryShopMisses = 0;
			regularShopMisses = 0;
			recentOfferHistory.Clear();
		}
		if (currentShopIsInsurance && currentOffers.Count > 0)
		{
			return;
		}
		if ((Object)(object)gameController != (Object)null && gameController.ConsumeFateForcedShopRequest(round))
		{
			currentShopIsMini = false;
			currentShopIsRecovery = false;
			currentShopIsInsurance = false;
			currentRecoveryShopPurchaseRecorded = false;
			regularShopMisses = 0;
			BuildOffers(round, miniShop: false, recoveryShop: false, insuranceShop: false);
			RefreshUi(round);
			SetOpen(open: true);
			gameController.RecordRoundShopOpened(round);
			gameController.RequestBanner("운명 개입 상점 강제 등장!", new Color(0.72f, 0.88f, 1f), 2.4f);
			return;
		}
		if (ShouldOpenEarlyRecoveryShop(round))
		{
			earlyRecoveryShopShown = true;
			currentShopIsMini = false;
			currentShopIsRecovery = true;
			currentShopIsInsurance = false;
			currentRecoveryShopPurchaseRecorded = false;
			earlyRecoveryShopMisses = 0;
			BuildOffers(round, miniShop: false, recoveryShop: true, insuranceShop: false);
			RefreshUi(round);
			SetOpen(open: true);
			gameController?.RecordRoundShopOpened(round);
			gameController?.RequestBanner("긴급 지원 도착!  3개 중 1개를 선택하세요", new Color(1f, 0.67f, 0.24f), 2.6f);
			gameController?.MarkEarlyRunRecoveryOffered();
			return;
		}
		if (ShouldOpenEarlyMiniShop(round))
		{
			lastMiniShopRound = round;
			currentShopIsMini = true;
			currentShopIsRecovery = false;
			currentShopIsInsurance = false;
			currentRecoveryShopPurchaseRecorded = false;
			earlyMiniShopMisses = 0;
			BuildOffers(round, miniShop: true, recoveryShop: false, insuranceShop: false);
			RefreshUi(round);
			SetOpen(open: true);
			gameController?.RecordRoundShopOpened(round);
			int num = Mathf.Clamp(earlyMiniShopOfferCount, 1, 3);
			gameController?.RequestBanner("초반 선택지 입고!  소형 상점 " + num + "개", new Color(0.48f, 1f, 0.74f), 2.4f);
			gameController?.RecordR3BoosterOffer();
			return;
		}
		int num2 = Mathf.Max(1, shopInterval);
		if (enableRegularShop && round >= firstShopRound && (round - firstShopRound) % num2 == 0)
		{
			if (!ShouldOpenRegularShop(round))
			{
				regularShopMisses++;
				return;
			}
			currentShopIsMini = false;
			currentShopIsRecovery = false;
			currentShopIsInsurance = false;
			currentRecoveryShopPurchaseRecorded = false;
			regularShopMisses = 0;
			BuildOffers(round, miniShop: false, recoveryShop: false, insuranceShop: false);
			RefreshUi(round);
			SetOpen(open: true);
			gameController?.RecordRoundShopOpened(round);
			gameController?.RequestBanner("전투 상점 입고!  이번 판 전용 상품 3개", new Color(0.38f, 0.82f, 1f), 2.4f);
		}
	}

	private void HandleRoundStarted(int round)
	{
		currentOffers.Clear();
		currentShopIsMini = false;
		currentShopIsRecovery = false;
		currentShopIsInsurance = false;
		currentRecoveryShopPurchaseRecorded = false;
		SetOpen(open: false);
	}

	private bool ShouldOpenEarlyMiniShop(int round)
	{
		int num = Mathf.Max(1, earlyMiniShopRound);
		int num2 = Mathf.Max(1, miniShopInterval);
		bool flag = round >= num && (round - num) % num2 == 0;
		if (!enableEarlyMiniShop || lastMiniShopRound == round || round < num || !flag)
		{
			if (flag)
			{
				earlyMiniShopMisses = 0;
			}
			return false;
		}
		if (!RollLuckyShopAppearance(earlyMiniShopAppearanceChance, 0, guaranteedEarlyMiniShopAfterMisses))
		{
			earlyMiniShopMisses++;
			return false;
		}
		return true;
	}

	private bool ShouldOpenEarlyRecoveryShop(int round)
	{
		if (!enableEarlyRecoveryShop || earlyRecoveryShopShown || (Object)(object)gameController == (Object)null || !gameController.EarlyRunRecoveryRecommended || gameController.BadLuckInsuranceAvailable || round < Mathf.Max(1, earlyRecoveryShopFirstRound) || round > Mathf.Max(earlyRecoveryShopFirstRound, earlyRecoveryShopLastRound))
		{
			return false;
		}
		int guaranteedAfterMisses = Mathf.Max(0, guaranteedEarlyRecoveryShopAfterMisses);
		if (!RollLuckyShopAppearance(earlyRecoveryShopAppearanceChance, earlyRecoveryShopMisses, guaranteedAfterMisses))
		{
			earlyRecoveryShopMisses++;
			return false;
		}
		return true;
	}

	private bool ShouldOpenRegularShop(int round)
	{
		if (guaranteeFirstRegularShop && round == firstShopRound)
		{
			return true;
		}
		return RollLuckyShopAppearance(regularShopAppearanceChance, regularShopMisses, guaranteedRegularShopAfterMisses);
	}

	private bool RollLuckyShopAppearance(float baseChance, int missCount, int guaranteedAfterMisses)
	{
		if (!enableLuckyShopAppearances)
		{
			return true;
		}
		int num = Mathf.Max(0, guaranteedAfterMisses);
		if (num > 0 && missCount >= num)
		{
			return true;
		}
		float num2 = Mathf.Clamp(maxLuckyShopAppearanceChance, Mathf.Clamp01(baseChance), 1f);
		float num3 = Mathf.Min(num2, Mathf.Clamp01(baseChance + (float)Mathf.Max(0, missCount) * missedShopChanceBonus));
		return Random.value <= num3;
	}

	private void HandleGameOver()
	{
		currentOffers.Clear();
		SetOpen(open: false);
		lastMiniShopRound = -1;
		earlyRecoveryShopShown = true;
		currentShopIsMini = false;
		currentShopIsRecovery = false;
		currentShopIsInsurance = false;
		currentRecoveryShopPurchaseRecorded = false;
		earlyMiniShopMisses = 0;
		earlyRecoveryShopMisses = 0;
		regularShopMisses = 0;
		recentOfferHistory.Clear();
	}

	private void BuildOffers(int round, bool miniShop, bool recoveryShop, bool insuranceShop)
	{
		currentOffers.Clear();
		if (insuranceShop)
		{
			currentOffers.Add(CreateOffer(ResolveInsuranceOfferType(round), round));
			return;
		}
		int num = (recoveryShop ? Mathf.Clamp(earlyRecoveryShopOfferCount, 1, 3) : (miniShop ? Mathf.Clamp(earlyMiniShopOfferCount, 1, 3) : 3));
		List<OfferType> list = (recoveryShop ? BuildRecoveryShopPool() : (miniShop ? BuildMiniShopPool(round) : BuildRegularShopPool(round)));
		RemoveUnavailableAugmentOffers(list);
		RemoveRecentlyOfferedTypes(list, num);
		while (currentOffers.Count < num && list.Count > 0)
		{
			int index = Random.Range(0, list.Count);
			OfferType type = list[index];
			list.RemoveAt(index);
			AddOffer(type, round, miniShop, recoveryShop);
		}
		RememberCurrentOffers();
	}

	private void RemoveRecentlyOfferedTypes(List<OfferType> pool, int minimumPoolSize)
	{
		if (pool == null || recentOfferHistory.Count == 0)
		{
			return;
		}
		int num = Mathf.Max(1, minimumPoolSize);
		int num2 = pool.Count - 1;
		while (num2 >= 0 && pool.Count > num)
		{
			if (recentOfferHistory.Contains(pool[num2]))
			{
				pool.RemoveAt(num2);
			}
			num2--;
		}
	}

	private void RememberCurrentOffers()
	{
		for (int i = 0; i < currentOffers.Count; i++)
		{
			ShopOffer shopOffer = currentOffers[i];
			if (shopOffer != null)
			{
				recentOfferHistory.Remove(shopOffer.type);
				recentOfferHistory.Add(shopOffer.type);
			}
		}
		int num = Mathf.Max(3, recentOfferHistorySize);
		while (recentOfferHistory.Count > num)
		{
			recentOfferHistory.RemoveAt(0);
		}
	}

	private static List<OfferType> BuildRecoveryShopPool()
	{
		return new List<OfferType>
		{
			OfferType.RecoveryRareUnit,
			OfferType.RecoveryBossPrep,
			OfferType.MergeAssist,
			OfferType.FieldMedic,
			OfferType.Coupon,
			OfferType.RandomUnit,
			OfferType.RareUnit,
			OfferType.TileReroll,
			OfferType.BossIntel,
			OfferType.AugmentGuard,
			OfferType.AugmentSkill
		};
	}

	private List<OfferType> BuildMiniShopPool(int round)
	{
		List<OfferType> list = new List<OfferType>
		{
			OfferType.RandomUnit,
			OfferType.RareUnit,
			OfferType.MergeAssist,
			OfferType.TileReroll,
			OfferType.BossIntel,
			OfferType.Coupon,
			OfferType.RiskChest,
			OfferType.AugmentPower,
			OfferType.AugmentGuard,
			OfferType.AugmentSkill
		};
		if (NeedsFieldMedic())
		{
			list.Add(OfferType.FieldMedic);
		}
		if (round >= 8)
		{
			list.Add(OfferType.FateMergeContract);
			list.Add(OfferType.FateBossContract);
		}
		if (round >= 12)
		{
			list.Add(OfferType.FateShopReroll);
			list.Add(OfferType.FateGradeLock);
		}
		if (round >= 18)
		{
			list.Add(OfferType.FateNormalBan);
			list.Add(OfferType.FateForceShop);
		}
		return list;
	}

	private List<OfferType> BuildRegularShopPool(int round)
	{
		List<OfferType> list = new List<OfferType>
		{
			OfferType.RandomUnit,
			OfferType.RareUnit,
			OfferType.RiskChest,
			OfferType.MergeAssist,
			OfferType.TileReroll,
			OfferType.BossIntel,
			OfferType.Coupon,
			OfferType.AugmentPower,
			OfferType.AugmentGuard,
			OfferType.AugmentSkill
		};
		if (NeedsFieldMedic())
		{
			list.Add(OfferType.FieldMedic);
		}
		if (round >= 18)
		{
			list.Add(OfferType.FateMergeContract);
			list.Add(OfferType.FateBossContract);
		}
		return list;
	}

	private bool NeedsFieldMedic()
	{
		if ((Object)(object)gameController != (Object)null && gameController.Life < gameController.MaxLife)
		{
			return true;
		}
		DefenderUnit[] array = (((Object)(object)boardManager != (Object)null) ? boardManager.GetAliveDefenders() : new DefenderUnit[0]);
		for (int i = 0; i < array.Length; i++)
		{
			if ((Object)(object)array[i] != (Object)null && array[i].HealthRatio < 0.92f)
			{
				return true;
			}
		}
		return false;
	}

	private static void ApplyFixedRoundShopPrice(ShopOffer offer, int round, bool recoveryShop)
	{
		if (offer != null && offer.cost > 0)
		{
			offer.cost = (recoveryShop ? ResolveFixedRecoveryShopPrice(offer.type, round) : ResolveFixedMiniShopPrice(offer.type, round));
		}
	}

	private static int ResolveFixedMiniShopPrice(OfferType type, int round)
	{
		int num = ResolveFixedMiniShopSpendTarget(round);
		return type switch
		{
			OfferType.RandomUnit => Mathf.Max(1, num - 1), 
			OfferType.Coupon => Mathf.Max(1, num - 2), 
			OfferType.TileReroll => Mathf.Max(1, num - 4), 
			OfferType.FieldMedic => Mathf.Max(1, num - 3), 
			OfferType.RiskChest => Mathf.Max(1, num - 4), 
			_ => num, 
		};
	}

	private static int ResolveFixedMiniShopSpendTarget(int round)
	{
		if (round <= 7)
		{
			return 20;
		}
		if (round <= 15)
		{
			return 34;
		}
		if (round <= 23)
		{
			return 50;
		}
		if (round <= 31)
		{
			return 68;
		}
		if (round <= 39)
		{
			return 88;
		}
		if (round <= 47)
		{
			return 110;
		}
		int num = Mathf.Max(0, (round - 48) / 8);
		return 132 + num * 22;
	}

	private static int ResolveFixedRecoveryShopPrice(OfferType type, int round)
	{
		int num = ((round <= 5) ? 8 : ((round <= 7) ? 10 : 12));
		switch (type)
		{
		case OfferType.FieldMedic:
		case OfferType.RecoveryBossPrep:
			return Mathf.Max(1, num - 2);
		case OfferType.MergeAssist:
			return num + 2;
		default:
			return num;
		}
	}

	private void RemoveUnavailableAugmentOffers(List<OfferType> pool)
	{
		if (pool != null)
		{
			if ((Object)(object)augmentManager == (Object)null || augmentManager.HasChosenAugment("power_core"))
			{
				pool.Remove(OfferType.AugmentPower);
			}
			if ((Object)(object)augmentManager == (Object)null || augmentManager.HasChosenAugment("guardian_heart"))
			{
				pool.Remove(OfferType.AugmentGuard);
			}
			if ((Object)(object)augmentManager == (Object)null || augmentManager.HasChosenAugment("skill_overload"))
			{
				pool.Remove(OfferType.AugmentSkill);
			}
		}
	}

	private OfferType ResolveInsuranceOfferType(int round)
	{
		if ((Object)(object)gameController == (Object)null)
		{
			return OfferType.InsuranceMergeMaterial;
		}
		float num = ((gameController.MaxLife > 0) ? Mathf.Clamp01((float)gameController.Life / (float)gameController.MaxLife) : 1f);
		if (num <= 0.55f)
		{
			return OfferType.InsuranceRecoveryTicket;
		}
		if (gameController.RoundsUntilNextBoss <= 2 || round >= 8)
		{
			return OfferType.InsuranceBossCounter;
		}
		return OfferType.InsuranceMergeMaterial;
	}

	private void AddOffer(OfferType type, int round, bool miniShop, bool recoveryShop)
	{
		ShopOffer shopOffer = CreateOffer(type, round);
		if (shopOffer != null)
		{
			bool flag = miniShop && !recoveryShop;
			if (flag)
			{
				shopOffer.title = "소형 " + shopOffer.title;
			}
			if (miniShop || recoveryShop)
			{
				ApplyFixedRoundShopPrice(shopOffer, round, recoveryShop);
			}
			else
			{
				ApplyRoundShopInflation(shopOffer, round);
				ApplySummonOpportunityCostFloor(shopOffer, miniShop: false, recoveryShop: false);
				ApplyDailyFortuneOfferModifier(shopOffer);
				ApplyFateOfferModifier(shopOffer);
			}
			ApplyReadableFateOfferText(shopOffer, flag);
			currentOffers.Add(shopOffer);
		}
	}

	private static void ApplyRoundShopInflation(ShopOffer offer, int round)
	{
		if (offer != null && offer.cost > 0)
		{
			float num = ((round >= 21) ? 1.3f : ((round >= 15) ? 1.15f : 1f));
			if (!(num <= 1f))
			{
				offer.cost = Mathf.Max(1, Mathf.CeilToInt((float)offer.cost * num));
				offer.description += " 장기 진행 물가가 적용됩니다.";
			}
		}
	}

	private void ApplySummonOpportunityCostFloor(ShopOffer offer, bool miniShop, bool recoveryShop)
	{
		if (offer != null && offer.cost > 0 && !((Object)(object)gameController == (Object)null))
		{
			float num = ResolveMinimumSummonEquivalent(offer.type);
			if (!(num <= 0f))
			{
				float num2 = (miniShop ? miniShopOpportunityRate : (recoveryShop ? recoveryShopOpportunityRate : regularShopOpportunityRate));
				int num3 = Mathf.Max(1, gameController.SummonCost);
				int num4 = Mathf.CeilToInt((float)num3 * num * Mathf.Max(0.1f, num2));
				offer.cost = Mathf.Max(offer.cost, num4);
			}
		}
	}

	private static float ResolveMinimumSummonEquivalent(OfferType type)
	{
		switch (type)
		{
		case OfferType.RandomUnit:
			return 2.4f;
		case OfferType.RareUnit:
			return 5.2f;
		case OfferType.RiskChest:
			return 6.3f;
		case OfferType.MergeAssist:
			return 3.2f;
		case OfferType.TileReroll:
			return 3.4f;
		case OfferType.BossIntel:
			return 6.8f;
		case OfferType.Coupon:
			return 1.2f;
		case OfferType.FieldMedic:
			return 3.6f;
		case OfferType.RecoveryRareUnit:
			return 3.8f;
		case OfferType.RecoveryBossPrep:
			return 5f;
		case OfferType.AugmentPower:
		case OfferType.AugmentGuard:
		case OfferType.AugmentSkill:
			return 8f;
		default:
			return 0f;
		}
	}

	private static void ApplyDailyFortuneOfferModifier(ShopOffer offer)
	{
		if (offer != null && offer.cost > 0)
		{
			DailyFortuneRule today = DailyFortuneSystem.Today;
			if (today != null && !(today.shopDiscountRate <= 0f))
			{
				offer.cost = Mathf.Max(1, Mathf.RoundToInt((float)offer.cost * today.ShopCostMultiplier));
				offer.description += " 오늘의 운세 할인 적용.";
			}
		}
	}

	private void ApplyFateOfferModifier(ShopOffer offer)
	{
		if (offer != null && offer.cost > 0 && !((Object)(object)gameController == (Object)null))
		{
			int cost = offer.cost;
			int num = gameController.ApplyFateShopDebtCost(offer.cost);
			if (num != offer.cost)
			{
				offer.cost = num;
				offer.debtCostPenalty = Mathf.Max(0, num - cost);
				offer.description += " 운명 빚 상점가 반영.";
			}
		}
	}

	private static void ApplyReadableFateOfferText(ShopOffer offer, bool miniPrefix)
	{
		if (offer != null)
		{
			string text = (miniPrefix ? "소형 " : string.Empty);
			switch (offer.type)
			{
			case OfferType.FateMergeContract:
				offer.title = text + "운명 계약: 합성 올인";
				offer.description = "라이프 -2. 합성 재료와 소환비 할인을 받아 저점 판을 밀어 올립니다.";
				offer.priceLabel = "라이프 -2";
				break;
			case OfferType.FateBossContract:
				offer.title = text + "운명 계약: 보스 사냥";
				offer.description = "라이프 -2. 보스 피해와 소량의 보스 전 골드를 받지만 빚이 크게 남습니다.";
				offer.priceLabel = "라이프 -2";
				break;
			case OfferType.FateShopReroll:
				offer.title = text + "운명 개입: 상점 리롤";
				offer.description = "운명을 써서 상품 3개를 다시 뽑습니다. 대가는 빚으로 남습니다.";
				offer.priceLabel = "운명";
				break;
			case OfferType.FateGradeLock:
				offer.title = text + "운명 개입: Rare+ 3회";
				offer.description = "다음 3회 소환을 레어 이상으로 고정합니다. 초반 저점을 줄입니다.";
				offer.priceLabel = "운명";
				break;
			case OfferType.FateNormalBan:
				offer.title = text + "운명 개입: 일반 제외 4회";
				offer.description = "다음 4회 소환에서 일반을 제외합니다. 빈 손 판을 비틉니다.";
				offer.priceLabel = "운명";
				break;
			case OfferType.FateForceShop:
				offer.title = text + "운명 개입: 다음 상점";
				offer.description = "다음 라운드 상점을 확정합니다. 필요한 선택지를 직접 끌어옵니다.";
				offer.priceLabel = "운명";
				break;
			}
		}
	}

	private ShopOffer CreateOffer(OfferType type, int round)
	{
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_0667: Unknown result type (might be due to invalid IL or missing references)
		//IL_066c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_03df: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0433: Unknown result type (might be due to invalid IL or missing references)
		//IL_0438: Unknown result type (might be due to invalid IL or missing references)
		//IL_0487: Unknown result type (might be due to invalid IL or missing references)
		//IL_048c: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_051d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0522: Unknown result type (might be due to invalid IL or missing references)
		//IL_0571: Unknown result type (might be due to invalid IL or missing references)
		//IL_0576: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0619: Unknown result type (might be due to invalid IL or missing references)
		//IL_061e: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0703: Unknown result type (might be due to invalid IL or missing references)
		//IL_0708: Unknown result type (might be due to invalid IL or missing references)
		//IL_074e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0753: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.Max(0, round / 3);
		return type switch
		{
			OfferType.RandomUnit => new ShopOffer
			{
				type = type,
				title = "긴급 소환권",
				description = "현재 라운드 확률표로 랜덤 유닛 1마리를 즉시 배치합니다.",
				cost = 14 + num * 3,
				color = new Color(0.34f, 0.82f, 1f)
			}, 
			OfferType.RareUnit => new ShopOffer
			{
				type = type,
				title = "레어 보급상자",
				description = "레어 유닛 1마리를 즉시 배치합니다. 초반 허들 돌파용입니다.",
				cost = 26 + num * 7,
				color = new Color(0.3f, 0.6f, 1f)
			}, 
			OfferType.RiskChest => new ShopOffer
			{
				type = type,
				title = "위험한 상자",
				description = "라이프 -1. 레어 이상 확정, 낮은 확률로 에픽/전설까지 튀는 도박 상자입니다.",
				cost = 38 + num * 9,
				priceLabel = "골드+HP",
				color = new Color(1f, 0.42f, 0.82f)
			}, 
			OfferType.MergeAssist => new ShopOffer
			{
				type = type,
				title = "합성 부스터",
				description = "보유 중인 등급의 부족 재료를 보급해 첫 합성선에 닿도록 도와줍니다.",
				cost = 9 + num * 2,
				color = new Color(0.78f, 1f, 0.34f)
			}, 
			OfferType.TileReroll => new ShopOffer
			{
				type = type,
				title = "타일 재배치",
				description = "전술 타일을 즉시 다시 뽑습니다. 현재 배치가 답답할 때 좋습니다.",
				cost = 15 + num * 4,
				color = new Color(0.42f, 1f, 0.88f)
			}, 
			OfferType.BossIntel => new ShopOffer
			{
				type = type,
				title = "보스 정보상",
				description = "현재 보유 유닛에게 공격 +8%, 보스 피해 +30%를 영구 적용합니다.",
				cost = 22 + num * 5,
				color = new Color(1f, 0.7f, 0.2f)
			}, 
			OfferType.Coupon => new ShopOffer
			{
				type = type,
				title = "4라운드 소환 패스",
				description = "다음 4라운드 동안 소환 비용을 18% 낮춥니다. 초반에 여러 번 소환할수록 이득이 커집니다.",
				cost = 8 + num * 2,
				color = new Color(0.56f, 0.92f, 1f)
			}, 
			OfferType.FateMergeContract => new ShopOffer
			{
				type = type,
				title = "운명 계약: 합성 올인",
				description = "라이프 -2와 운명 빚을 남깁니다. 합성 재료 보급과 소환비 7% 할인을 받습니다.",
				cost = 0,
				priceLabel = "라이프 -2",
				color = new Color(1f, 0.48f, 0.82f)
			}, 
			OfferType.FateBossContract => new ShopOffer
			{
				type = type,
				title = "운명 계약: 보스 사냥",
				description = "라이프 -2와 운명 빚을 남깁니다. 보스 피해 +16%, 보스 전 소량의 골드를 받습니다.",
				cost = 0,
				priceLabel = "라이프 -2",
				color = new Color(1f, 0.66f, 0.24f)
			}, 
			OfferType.FateShopReroll => new ShopOffer
			{
				type = type,
				title = "운명 개입: 상점 리롤",
				description = "운명 게이지를 써서 현재 상점 상품 3개를 다시 뽑습니다. 빚은 다음 상점/보스에 정산됩니다.",
				cost = 0,
				priceLabel = "운명",
				color = new Color(0.78f, 0.5f, 1f)
			}, 
			OfferType.FateGradeLock => new ShopOffer
			{
				type = type,
				title = "운명 개입: 레어 잠금",
				description = "운명 게이지를 써서 다음 2회 소환을 레어 이상으로 잠급니다. 빚은 다음 상점/보스에 정산됩니다.",
				cost = 0,
				priceLabel = "운명",
				color = new Color(1f, 0.46f, 0.92f)
			}, 
			OfferType.FateNormalBan => new ShopOffer
			{
				type = type,
				title = "운명 개입: 일반 금지",
				description = "운명 게이지를 써서 다음 3회 소환에서 일반 등급을 제외합니다. 저점 판을 직접 비틉니다.",
				cost = 0,
				priceLabel = "운명",
				color = new Color(0.44f, 1f, 0.78f)
			}, 
			OfferType.FateForceShop => new ShopOffer
			{
				type = type,
				title = "운명 개입: 상점 강제",
				description = "운명 게이지를 써서 다음 라운드 상점을 확정 등장시킵니다. 필요한 선택지를 직접 끌어옵니다.",
				cost = 0,
				priceLabel = "운명",
				color = new Color(0.52f, 0.78f, 1f)
			}, 
			OfferType.RecoveryRareUnit => new ShopOffer
			{
				type = type,
				title = "구제 레어 지원",
				description = "레어 유닛 1마리를 낮은 비용으로 배치합니다. 빈 슬롯이 필요합니다.",
				cost = 2 + num,
				color = new Color(0.32f, 0.68f, 1f)
			}, 
			OfferType.RecoveryBossPrep => new ShopOffer
			{
				type = type,
				title = "보스 대비 패키지",
				description = "모든 생존 유닛 체력 30% 회복, 공격력 +5%, 보스 피해 +20%.",
				cost = 3 + num,
				color = new Color(1f, 0.58f, 0.24f)
			}, 
			OfferType.InsuranceMergeMaterial => new ShopOffer
			{
				type = type,
				title = "합성 재료 보험",
				description = "가장 가까운 합성선의 부족 재료를 1회 보급합니다.",
				cost = 0,
				priceLabel = "보험 1회",
				color = new Color(0.76f, 1f, 0.34f)
			}, 
			OfferType.InsuranceRecoveryTicket => new ShopOffer
			{
				type = type,
				title = "회복권 보험",
				description = "골드 +4와 모든 생존 유닛 체력 20% 회복. 초반 저점을 작게 복구합니다.",
				cost = 0,
				priceLabel = "보험 1회",
				color = new Color(0.4f, 1f, 0.68f)
			}, 
			OfferType.InsuranceBossCounter => new ShopOffer
			{
				type = type,
				title = "보스 대응권",
				description = "모든 생존 유닛 체력 18% 회복, 보스 피해 +8%.",
				cost = 0,
				priceLabel = "보험 1회",
				color = new Color(1f, 0.64f, 0.22f)
			}, 
			OfferType.FieldMedic => new ShopOffer
			{
				type = type,
				title = "현장 의무병",
				description = "모든 생존 유닛 체력 25% 회복. 방어선 HP가 절반 이하면 HP도 1 회복합니다.",
				cost = 18 + num * 4,
				color = new Color(0.48f, 1f, 0.6f)
			}, 
			OfferType.AugmentPower => new ShopOffer
			{
				type = type,
				title = "증강체: 화력 코어",
				description = "이번 판 동안 모든 현재·미래 유닛 공격력 +20%. 1회만 획득 가능합니다.",
				cost = 28 + num * 6,
				color = new Color(1f, 0.57f, 0.28f)
			}, 
			OfferType.AugmentGuard => new ShopOffer
			{
				type = type,
				title = "증강체: 수호자의 심장",
				description = "이번 판 동안 모든 현재·미래 유닛 최대 체력 +24%. 1회만 획득 가능합니다.",
				cost = 26 + num * 6,
				color = new Color(0.35f, 1f, 0.62f)
			}, 
			OfferType.AugmentSkill => new ShopOffer
			{
				type = type,
				title = "증강체: 스킬 과부하",
				description = "이번 판 동안 모든 현재·미래 유닛 스킬 위력 +22%. 1회만 획득 가능합니다.",
				cost = 30 + num * 7,
				color = new Color(0.96f, 0.5f, 1f)
			}, 
			_ => null, 
		};
	}

	private void BuyOffer(int index)
	{
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)gameController != (Object)null && gameController.IsRoundRunning)
		{
			HandleRoundStarted(gameController.CurrentRound);
		}
		else
		{
			if (index < 0 || index >= currentOffers.Count || (Object)(object)gameController == (Object)null)
			{
				return;
			}
			ShopOffer shopOffer = currentOffers[index];
			if (shopOffer == null)
			{
				return;
			}
			if (gameController.Gold < shopOffer.cost)
			{
				gameController.RequestBanner("골드 부족  " + shopOffer.title + " 구매 불가", new Color(1f, 0.42f, 0.32f), 1.8f);
				return;
			}
			gameController.RemoveGold(shopOffer.cost);
			if (!ApplyOffer(shopOffer))
			{
				gameController.AddGold(shopOffer.cost);
				gameController.RequestBanner("구매 실패  빈 슬롯 또는 대상이 필요합니다", new Color(1f, 0.48f, 0.3f), 1.8f);
				return;
			}
			gameController.RecordFateShopCostPenalty(shopOffer.debtCostPenalty);
			gameController.RequestBanner("구매 완료!  " + shopOffer.title, shopOffer.color, 2.2f);
			if (shopOffer.type == OfferType.FateShopReroll)
			{
				BuildOffers(gameController.CurrentRound, currentShopIsMini, currentShopIsRecovery, currentShopIsInsurance);
				RefreshUi(gameController.CurrentRound);
				return;
			}
			if (currentShopIsMini && (shopOffer.type == OfferType.MergeAssist || shopOffer.type == OfferType.FateMergeContract))
			{
				gameController.RecordR3BoosterPurchase();
			}
			if (currentShopIsRecovery && !currentRecoveryShopPurchaseRecorded)
			{
				currentRecoveryShopPurchaseRecorded = true;
				gameController.RecordEarlyRecoveryShopPurchase();
			}
			if (currentShopIsRecovery)
			{
				currentOffers.Clear();
				currentShopIsRecovery = false;
				SetOpen(open: false);
				return;
			}
			if (currentShopIsMini)
			{
				currentOffers.Clear();
				currentShopIsMini = false;
				SetOpen(open: false);
				return;
			}
			if (currentShopIsInsurance)
			{
				gameController.MarkBadLuckInsuranceClaimed(shopOffer.title);
				currentOffers.Clear();
				SetOpen(open: false);
				return;
			}
			currentOffers.RemoveAt(index);
			RefreshUi(gameController.CurrentRound);
			if (currentOffers.Count == 0)
			{
				SetOpen(open: false);
			}
		}
	}

	private bool ApplyOffer(ShopOffer offer)
	{
		switch (offer.type)
		{
		case OfferType.RandomUnit:
			return gameController.TryGrantRandomSummonableUnit();
		case OfferType.RareUnit:
			return gameController.TryGrantRandomUnitByGrade(CharacterGrade.Rare);
		case OfferType.RiskChest:
		{
			if (!gameController.TrySpendLifeForContract(1, "위험한 상자"))
			{
				return false;
			}
			float value = Random.value;
			CharacterGrade grade = ((value < 0.1f) ? CharacterGrade.Legendary : ((!(value < 0.38f)) ? CharacterGrade.Rare : CharacterGrade.Epic));
			return gameController.TryGrantRandomUnitByGrade(grade);
		}
		case OfferType.MergeAssist:
			return gameController.TryGrantMergeAssistUnit();
		case OfferType.TileReroll:
			if ((Object)(object)tileModifierSystem == (Object)null)
			{
				return false;
			}
			tileModifierSystem.RerollTiles(force: true, "상점 타일 재배치");
			return true;
		case OfferType.BossIntel:
		{
			DefenderUnit[] array5 = (((Object)(object)boardManager != (Object)null) ? boardManager.GetAliveDefenders() : new DefenderUnit[0]);
			if (array5.Length == 0)
			{
				return false;
			}
			for (int m = 0; m < array5.Length; m++)
			{
				if (!((Object)(object)array5[m] == (Object)null))
				{
					array5[m].AddAttackPowerBonus(0.08f);
					array5[m].AddBossDamageBonus(0.3f);
				}
			}
			return true;
		}
		case OfferType.Coupon:
			gameController.AddTemporaryShopSummonDiscount(0.18f, 4);
			return true;
		case OfferType.FateShopReroll:
			return gameController.TrySpendFateForShopReroll();
		case OfferType.FateGradeLock:
			return gameController.TryActivateFateGradeLock(CharacterGrade.Rare, 3);
		case OfferType.FateNormalBan:
			return gameController.TryActivateFateNormalBan(4);
		case OfferType.FateForceShop:
			return gameController.TryActivateFateForcedShop();
		case OfferType.FateMergeContract:
		{
			if (!gameController.TrySpendLifeForContract(2, "합성 올인"))
			{
				return false;
			}
			bool flag2 = gameController.TryGrantMergeAssistUnit();
			gameController.AddSummonCostDiscount(0.07f);
			if (!flag2)
			{
				gameController.AddGold(6);
			}
			return true;
		}
		case OfferType.FateBossContract:
		{
			if (!gameController.TrySpendLifeForContract(2, "보스 사냥"))
			{
				return false;
			}
			DefenderUnit[] array2 = (((Object)(object)boardManager != (Object)null) ? boardManager.GetAliveDefenders() : new DefenderUnit[0]);
			bool flag = false;
			for (int j = 0; j < array2.Length; j++)
			{
				if (!((Object)(object)array2[j] == (Object)null))
				{
					array2[j].AddBossDamageBonus(0.16f);
					flag = true;
				}
			}
			if (!flag)
			{
				gameController.TryGrantRandomUnitByGrade(CharacterGrade.Rare);
			}
			gameController.AddGold(6);
			gameController.AddRoundGoldBonus(1);
			return true;
		}
		case OfferType.RecoveryRareUnit:
			return gameController.TryGrantRandomUnitByGrade(CharacterGrade.Rare);
		case OfferType.RecoveryBossPrep:
		{
			DefenderUnit[] array4 = (((Object)(object)boardManager != (Object)null) ? boardManager.GetAliveDefenders() : new DefenderUnit[0]);
			if (array4.Length == 0)
			{
				return false;
			}
			for (int l = 0; l < array4.Length; l++)
			{
				if (!((Object)(object)array4[l] == (Object)null))
				{
					array4[l].Heal(array4[l].MaxHealth * 0.3f);
					array4[l].AddAttackPowerBonus(0.05f);
					array4[l].AddBossDamageBonus(0.2f);
				}
			}
			return true;
		}
		case OfferType.InsuranceMergeMaterial:
			return gameController.TryGrantMergeAssistUnit() || gameController.TryGrantRandomUnitByGrade(CharacterGrade.Rare);
		case OfferType.InsuranceRecoveryTicket:
			gameController.AddGold(4);
			HealAliveDefenders(0.2f);
			return true;
		case OfferType.InsuranceBossCounter:
		{
			DefenderUnit[] array = (((Object)(object)boardManager != (Object)null) ? boardManager.GetAliveDefenders() : new DefenderUnit[0]);
			if (array.Length == 0)
			{
				return gameController.TryGrantRandomUnitByGrade(CharacterGrade.Rare);
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (!((Object)(object)array[i] == (Object)null))
				{
					array[i].Heal(array[i].MaxHealth * 0.18f);
					array[i].AddBossDamageBonus(0.08f);
				}
			}
			return true;
		}
		case OfferType.FieldMedic:
		{
			DefenderUnit[] array3 = (((Object)(object)boardManager != (Object)null) ? boardManager.GetAliveDefenders() : new DefenderUnit[0]);
			if (array3.Length == 0)
			{
				return false;
			}
			for (int k = 0; k < array3.Length; k++)
			{
				array3[k]?.Heal(array3[k].MaxHealth * 0.25f);
			}
			if (gameController.Life <= Mathf.CeilToInt((float)gameController.MaxLife * 0.5f))
			{
				gameController.RecoverLife(1);
			}
			return true;
		}
		case OfferType.AugmentPower:
			return (Object)(object)augmentManager != (Object)null && augmentManager.TryGrantShopAugment("power_core");
		case OfferType.AugmentGuard:
			return (Object)(object)augmentManager != (Object)null && augmentManager.TryGrantShopAugment("guardian_heart");
		case OfferType.AugmentSkill:
			return (Object)(object)augmentManager != (Object)null && augmentManager.TryGrantShopAugment("skill_overload");
		default:
			return false;
		}
	}

	private void HealAliveDefenders(float ratio)
	{
		DefenderUnit[] array = (((Object)(object)boardManager != (Object)null) ? boardManager.GetAliveDefenders() : new DefenderUnit[0]);
		for (int i = 0; i < array.Length; i++)
		{
			if ((Object)(object)array[i] != (Object)null)
			{
				array[i].Heal(array[i].MaxHealth * Mathf.Clamp01(ratio));
			}
		}
	}

	private void CacheVisualIdentityTargets()
	{
		Transform val = (((Object)(object)panelRoot != (Object)null) ? panelRoot.transform.Find("RunShopModal") : null);
		if (!((Object)(object)val == (Object)null))
		{
			modalImage = ((Component)val).GetComponent<Image>();
			headerPillImage = GetChildImage(val, "RunShopHeaderPill");
			topLineImage = GetChildImage(val, "RunShopTopLine");
			bottomLineImage = GetChildImage(val, "RunShopBottomLine");
			leftRailImage = GetChildImage(val, "RunShopLeftRail");
			rightRailImage = GetChildImage(val, "RunShopRightRail");
			Transform val2 = val.Find("RunShopFooterHint");
			footerHintText = (((Object)(object)val2 != (Object)null) ? ((Component)val2).GetComponent<Text>() : null);
		}
	}

	private void ApplyShopVisualIdentity()
	{
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_0379: Unknown result type (might be due to invalid IL or missing references)
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_040e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0431: Unknown result type (might be due to invalid IL or missing references)
		//IL_0436: Unknown result type (might be due to invalid IL or missing references)
		//IL_043c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0477: Unknown result type (might be due to invalid IL or missing references)
		//IL_0497: Unknown result type (might be due to invalid IL or missing references)
		//IL_049c: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0607: Unknown result type (might be due to invalid IL or missing references)
		//IL_0627: Unknown result type (might be due to invalid IL or missing references)
		//IL_062d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0633: Unknown result type (might be due to invalid IL or missing references)
		//IL_063e: Unknown result type (might be due to invalid IL or missing references)
		//IL_066c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0696: Unknown result type (might be due to invalid IL or missing references)
		//IL_0697: Unknown result type (might be due to invalid IL or missing references)
		//IL_069d: Unknown result type (might be due to invalid IL or missing references)
		Color val = default(Color);
		Color val2 = default(Color);
		Color color = default(Color);
		Color color2 = default(Color);
		Vector2 sizeDelta = default(Vector2);
		bool visible;
		bool visible2;
		bool visible3;
		bool visible4;
		Vector2 size = default(Vector2);
		Vector2 size2 = default(Vector2);
		if (currentShopIsInsurance)
		{
			((Color)(ref val))._002Ector(0.68f, 0.96f, 0.3f, 1f);
			((Color)(ref val2))._002Ector(0.06f, 0.2f, 0.16f, 0.99f);
			((Color)(ref color))._002Ector(0.08f, 0.26f, 0.18f, 0.99f);
			((Color)(ref color2))._002Ector(0.04f, 0.14f, 0.11f, 0.99f);
			((Vector2)(ref sizeDelta))._002Ector(430f, 74f);
			visible = true;
			visible2 = true;
			visible3 = true;
			visible4 = false;
			((Vector2)(ref size))._002Ector(720f, 6f);
			((Vector2)(ref size2))._002Ector(310f, 6f);
		}
		else if (currentShopIsRecovery)
		{
			((Color)(ref val))._002Ector(1f, 0.62f, 0.18f, 1f);
			((Color)(ref val2))._002Ector(0.22f, 0.12f, 0.08f, 0.99f);
			((Color)(ref color))._002Ector(0.27f, 0.16f, 0.1f, 0.99f);
			((Color)(ref color2))._002Ector(0.16f, 0.08f, 0.05f, 0.99f);
			((Vector2)(ref sizeDelta))._002Ector(500f, 76f);
			visible = true;
			visible2 = false;
			visible3 = true;
			visible4 = true;
			((Vector2)(ref size))._002Ector(300f, 7f);
			((Vector2)(ref size2))._002Ector(300f, 5f);
		}
		else if (currentShopIsMini)
		{
			((Color)(ref val))._002Ector(0.22f, 0.92f, 0.78f, 1f);
			((Color)(ref val2))._002Ector(0.04f, 0.2f, 0.25f, 0.99f);
			((Color)(ref color))._002Ector(0.05f, 0.26f, 0.29f, 0.99f);
			((Color)(ref color2))._002Ector(0.03f, 0.14f, 0.18f, 0.99f);
			((Vector2)(ref sizeDelta))._002Ector(440f, 68f);
			visible = true;
			visible2 = true;
			visible3 = false;
			visible4 = false;
			((Vector2)(ref size))._002Ector(630f, 5f);
			((Vector2)(ref size2))._002Ector(280f, 5f);
		}
		else
		{
			((Color)(ref val))._002Ector(0.28f, 0.78f, 1f, 1f);
			((Color)(ref val2))._002Ector(0.065f, 0.11f, 0.3f, 0.99f);
			((Color)(ref color))._002Ector(0.09f, 0.16f, 0.36f, 0.99f);
			((Color)(ref color2))._002Ector(0.055f, 0.09f, 0.23f, 0.99f);
			((Vector2)(ref sizeDelta))._002Ector(360f, 66f);
			visible = true;
			visible2 = true;
			visible3 = false;
			visible4 = false;
			((Vector2)(ref size))._002Ector(720f, 5f);
			((Vector2)(ref size2))._002Ector(720f, 5f);
		}
		if ((Object)(object)modalImage != (Object)null)
		{
			((Graphic)modalImage).color = val2;
		}
		if ((Object)(object)headerPillImage != (Object)null)
		{
			((Graphic)headerPillImage).color = val;
			((Graphic)headerPillImage).rectTransform.sizeDelta = sizeDelta;
		}
		SetIdentityLine(topLineImage, visible, val, size);
		SetIdentityLine(bottomLineImage, visible2, new Color(val.r, val.g, val.b, 0.78f), size2);
		SetIdentityLine(leftRailImage, visible3, val, new Vector2(currentShopIsRecovery ? 9f : 7f, 620f));
		SetIdentityLine(rightRailImage, visible4, new Color(val.r, val.g, val.b, 0.82f), new Vector2(currentShopIsRecovery ? 9f : 7f, 620f));
		if ((Object)(object)headerText != (Object)null)
		{
			((Graphic)headerText).color = Color.white;
		}
		if ((Object)(object)subtitleText != (Object)null)
		{
			((Graphic)subtitleText).color = Color.Lerp(Color.white, val, 0.34f);
		}
		if ((Object)(object)reopenButton != (Object)null)
		{
			Graphic targetGraphic = ((Selectable)reopenButton).targetGraphic;
			Image val3 = (Image)(object)((targetGraphic is Image) ? targetGraphic : null);
			if (val3 != null)
			{
				((Graphic)val3).color = val;
			}
		}
		if ((Object)(object)footerHintText != (Object)null)
		{
			((Graphic)footerHintText).color = Color.Lerp(Color.white, val, 0.3f);
			footerHintText.text = (currentShopIsInsurance ? "추천된 보험 1개를 확인하세요." : (currentShopIsRecovery ? "3개 중 1개만 선택할 수 있습니다. 선택 즉시 긴급 지원이 종료됩니다." : (currentShopIsMini ? "3개 중 1개를 구매하면 소형 전투 상점이 종료됩니다." : "구매하지 않고 닫으면 이번 상점은 지나갑니다.")));
		}
		int num = ((offerButtons != null) ? offerButtons.Length : 0);
		Vector2 sizeDelta2 = default(Vector2);
		for (int i = 0; i < num; i++)
		{
			Button val4 = offerButtons[i];
			if (!((Object)(object)val4 == (Object)null))
			{
				RectTransform component = ((Component)val4).GetComponent<RectTransform>();
				float num2 = -158f - (float)i * 176f;
				float num3 = 0f;
				((Vector2)(ref sizeDelta2))._002Ector(790f, 164f);
				if (currentShopIsRecovery)
				{
					num3 = 0f;
					((Vector2)(ref sizeDelta2))._002Ector(790f, 164f);
				}
				else if (currentShopIsMini)
				{
					num3 = 0f;
					((Vector2)(ref sizeDelta2))._002Ector(790f, 164f);
				}
				else if (currentShopIsInsurance)
				{
					num2 = -234f;
					((Vector2)(ref sizeDelta2))._002Ector(790f, 192f);
				}
				component.anchoredPosition = new Vector2(num3, num2);
				component.sizeDelta = sizeDelta2;
				Graphic targetGraphic2 = ((Selectable)val4).targetGraphic;
				Image val5 = (Image)(object)((targetGraphic2 is Image) ? targetGraphic2 : null);
				if (val5 != null)
				{
					((Graphic)val5).color = color;
				}
				Outline component2 = ((Component)val4).GetComponent<Outline>();
				if ((Object)(object)component2 != (Object)null)
				{
					((Shadow)component2).effectColor = new Color(val.r, val.g, val.b, 0.96f);
				}
				Image childImage = GetChildImage(((Component)val4).transform, "PriceDock");
				if ((Object)(object)childImage != (Object)null)
				{
					((Graphic)childImage).color = color2;
				}
				Image childImage2 = GetChildImage(((Component)val4).transform, "RunShopIconBadgeBack");
				if ((Object)(object)childImage2 != (Object)null)
				{
					((Graphic)childImage2).color = Color.Lerp(val2, val, 0.46f);
				}
			}
		}
	}

	private static void SetIdentityLine(Image image, bool visible, Color color, Vector2 size)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)image == (Object)null))
		{
			((Component)image).gameObject.SetActive(visible);
			((Graphic)image).color = color;
			((Graphic)image).rectTransform.sizeDelta = size;
		}
	}

	private static Image GetChildImage(Transform parent, string childName)
	{
		Transform val = (((Object)(object)parent != (Object)null) ? parent.Find(childName) : null);
		return ((Object)(object)val != (Object)null) ? ((Component)val).GetComponent<Image>() : null;
	}

	private void RefreshUi(int round)
	{
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		ApplyShopVisualIdentity();
		if ((Object)(object)headerText != (Object)null)
		{
			headerText.text = (currentShopIsInsurance ? "운 나쁨 보험" : (currentShopIsRecovery ? "긴급 지원" : (currentShopIsMini ? "소형 전투 상점" : "전투 상점")));
		}
		if ((Object)(object)subtitleText != (Object)null)
		{
			string text = ((round <= 7) ? "초반 성장" : ((round <= 17) ? "중반 빌드" : "후반 고효율"));
			string text2 = (currentShopIsMini ? ("고정 선택가 " + ResolveFixedMiniShopSpendTarget(round) + "G 안팎") : (currentShopIsRecovery ? "위기 지원 고정가" : (((Object)(object)gameController != (Object)null) ? ("현재 소환비 " + gameController.SummonCost + "G") : "소환비 확인")));
			subtitleText.text = (currentShopIsInsurance ? ("보험 추천 1개  |  " + (((Object)(object)gameController != (Object)null) ? gameController.BadLuckInsuranceReason : "초반 저점 복구") + "  |  " + DailyFortuneSystem.TodaySummary) : (currentShopIsRecovery ? ("3개 중 1개  |  " + text2 + "  |  " + (((Object)(object)gameController != (Object)null) ? gameController.EarlyRunRecoveryCause : "위기 복구")) : (currentShopIsMini ? ("ROUND " + round + "  |  " + text + "  |  " + text2 + "  |  3개 중 1개") : ("ROUND " + round + "  |  " + text2 + "  |  유닛 소환 vs 상점 투자"))));
		}
		int num = ((offerButtons != null) ? offerButtons.Length : 0);
		for (int i = 0; i < num; i++)
		{
			bool flag = i < currentOffers.Count;
			if ((Object)(object)offerButtons[i] != (Object)null)
			{
				((Component)offerButtons[i]).gameObject.SetActive(flag);
			}
			if (flag)
			{
				ShopOffer shopOffer = currentOffers[i];
				SetText(GetText(offerTitleTexts, i), shopOffer.title);
				SetText(GetText(offerDescriptionTexts, i), shopOffer.description);
				SetText(GetText(offerPriceTexts, i), BuildOfferPriceLabel(shopOffer));
				Image image = GetImage(offerAccentImages, i);
				if ((Object)(object)image != (Object)null)
				{
					((Graphic)image).color = shopOffer.color;
				}
			}
		}
	}

	private string BuildOfferPriceLabel(ShopOffer offer)
	{
		if (offer == null)
		{
			return string.Empty;
		}
		int num = ResolveOfferSummonEquivalent(offer);
		if (!string.IsNullOrWhiteSpace(offer.priceLabel))
		{
			if (offer.priceLabel == "골드+HP")
			{
				return offer.cost + "G + HP -1 / 소환 약 " + num + "회";
			}
			if (offer.priceLabel == "라이프 -2")
			{
				return "HP -2 / 운명 빚";
			}
			if (offer.priceLabel == "운명")
			{
				return "운명 1회 / 빚";
			}
			if (offer.priceLabel == "보험 1회")
			{
				return "무료 보험 1회";
			}
			return offer.priceLabel;
		}
		if (offer.cost <= 0)
		{
			return "무료 / 소환 손실 없음";
		}
		return offer.cost + "G / 소환 약 " + num + "회";
	}

	private int ResolveOfferSummonEquivalent(ShopOffer offer)
	{
		int num = (((Object)(object)gameController != (Object)null) ? Mathf.Max(1, gameController.SummonCost) : 10);
		return (offer != null && offer.cost > 0) ? Mathf.Max(1, Mathf.CeilToInt((float)offer.cost / (float)num)) : 0;
	}

	private void SetOpen(bool open)
	{
		if ((Object)(object)panelRoot != (Object)null)
		{
			panelRoot.SetActive(open);
		}
		UpdateReopenButton();
	}

	private void Close()
	{
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		if (currentShopIsInsurance)
		{
			SetOpen(open: false);
			return;
		}
		bool flag = currentOffers.Count > 0;
		string text = (currentShopIsRecovery ? "긴급 지원" : (currentShopIsMini ? "소형 전투상점" : "전투상점"));
		currentOffers.Clear();
		currentShopIsMini = false;
		currentShopIsRecovery = false;
		currentShopIsInsurance = false;
		currentRecoveryShopPurchaseRecorded = false;
		SetOpen(open: false);
		if (flag)
		{
			gameController?.RequestBanner(text + " 패스", new Color(0.66f, 0.72f, 0.84f), 1.6f);
		}
	}

	private void Open()
	{
		if (currentOffers.Count == 0)
		{
			UpdateReopenButton();
		}
		else
		{
			SetOpen(open: true);
		}
	}

	private void UpdateReopenButton()
	{
		if ((Object)(object)reopenButton != (Object)null)
		{
			bool flag = (Object)(object)panelRoot != (Object)null && panelRoot.activeSelf;
			((Component)reopenButton).gameObject.SetActive(currentOffers.Count > 0 && !flag);
			Text componentInChildren = ((Component)reopenButton).GetComponentInChildren<Text>();
			if ((Object)(object)componentInChildren != (Object)null)
			{
				componentInChildren.text = (currentShopIsInsurance ? "보험" : (currentShopIsRecovery ? "지원" : "상점"));
			}
		}
	}

	private static Text GetText(Text[] texts, int index)
	{
		return (texts != null && index >= 0 && index < texts.Length) ? texts[index] : null;
	}

	private static Image GetImage(Image[] images, int index)
	{
		return (images != null && index >= 0 && index < images.Length) ? images[index] : null;
	}

	private static void SetText(Text text, string value)
	{
		if ((Object)(object)text != (Object)null)
		{
			text.text = value;
		}
	}
}
