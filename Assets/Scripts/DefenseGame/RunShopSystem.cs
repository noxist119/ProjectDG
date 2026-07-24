using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
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

        [SerializeField] private DefenseGameController gameController;
        [SerializeField] private DefenseBoardManager boardManager;
        [SerializeField] private BoardTileModifierSystem tileModifierSystem;
        [SerializeField] private AugmentManager augmentManager;
        [SerializeField] private bool enableRegularShop = false;
        [SerializeField] private int firstShopRound = 11;
        [SerializeField] private int shopInterval = 8;
        [SerializeField] private bool enableEarlyMiniShop = true;
        [SerializeField] private int earlyMiniShopRound = 3;
        [SerializeField] private int earlyMiniShopOfferCount = 3;
        [SerializeField] private int miniShopInterval = 8;
        [Header("Opportunity Cost Pricing")]
        [SerializeField] [Range(0.5f, 1.5f)] private float regularShopOpportunityRate = 1.35f;
        [SerializeField] [Range(0.5f, 1.5f)] private float miniShopOpportunityRate = 1.15f;
        [SerializeField] [Range(0.5f, 1.5f)] private float recoveryShopOpportunityRate = 1.10f;
        [SerializeField] private bool enableEarlyRecoveryShop = true;
        [SerializeField] private int earlyRecoveryShopFirstRound = 4;
        [SerializeField] private int earlyRecoveryShopLastRound = 10;
        [SerializeField] private int earlyRecoveryShopOfferCount = 3;
        [SerializeField] private bool enableLuckyShopAppearances = true;
        [SerializeField] [Range(0f, 1f)] private float earlyMiniShopAppearanceChance = 1f;
        [SerializeField] [Range(0f, 1f)] private float earlyRecoveryShopAppearanceChance = 0.85f;
        [SerializeField] [Range(0f, 1f)] private float regularShopAppearanceChance = 0.65f;
        [SerializeField] [Range(0f, 1f)] private float missedShopChanceBonus = 0.18f;
        [SerializeField] [Range(0f, 1f)] private float maxLuckyShopAppearanceChance = 0.88f;
        [SerializeField] private int guaranteedEarlyMiniShopAfterMisses = 0;
        [SerializeField] private int guaranteedEarlyRecoveryShopAfterMisses = 1;
        [SerializeField] private int guaranteedRegularShopAfterMisses = 1;
        [SerializeField] private bool guaranteeFirstRegularShop = true;

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

        public void Configure(
            DefenseGameController controller,
            DefenseBoardManager board,
            BoardTileModifierSystem tileSystem,
            AugmentManager augments,
            GameObject root,
            Text header,
            Text subtitle,
            Button[] buttons,
            Text[] titles,
            Text[] descriptions,
            Text[] prices,
            Image[] accents,
            Button close,
            Button reopen = null)
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
            SetOpen(false);
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
            if (subscribed || gameController == null)
            {
                return;
            }

            gameController.OnRoundShopPhase += HandleRoundShopPhase;
            gameController.OnRoundStarted += HandleRoundStarted;
            gameController.OnGameOver += HandleGameOver;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || gameController == null)
            {
                return;
            }

            gameController.OnRoundShopPhase -= HandleRoundShopPhase;
            gameController.OnRoundStarted -= HandleRoundStarted;
            gameController.OnGameOver -= HandleGameOver;
            subscribed = false;
        }

        private void WireUi()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            if (reopenButton != null)
            {
                reopenButton.onClick.RemoveListener(Open);
                reopenButton.onClick.AddListener(Open);
            }

            if (offerButtons == null)
            {
                return;
            }

            for (int i = 0; i < offerButtons.Length; i++)
            {
                int index = i;
                if (offerButtons[i] == null)
                {
                    continue;
                }

                offerButtons[i].onClick.RemoveAllListeners();
                offerButtons[i].onClick.AddListener(() => BuyOffer(index));
            }
        }

        private void HandleRoundShopPhase(int round)
        {
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
            }

            if (currentShopIsInsurance && currentOffers.Count > 0)
            {
                return;
            }

            if (gameController != null && gameController.ConsumeFateForcedShopRequest(round))
            {
                currentShopIsMini = false;
                currentShopIsRecovery = false;
                currentShopIsInsurance = false;
                currentRecoveryShopPurchaseRecorded = false;
                regularShopMisses = 0;
                BuildOffers(round, false, false, false);
                RefreshUi(round);
                SetOpen(true);
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
                BuildOffers(round, false, true, false);
                RefreshUi(round);
                SetOpen(true);
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
                BuildOffers(round, true, false, false);
                RefreshUi(round);
                SetOpen(true);
                gameController?.RecordRoundShopOpened(round);
                int offerCount = Mathf.Clamp(earlyMiniShopOfferCount, 1, 3);
                gameController?.RequestBanner("초반 선택지 입고!  소형 상점 " + offerCount + "개", new Color(0.48f, 1f, 0.74f), 2.4f);
                gameController?.RecordR3BoosterOffer();
                return;
            }

            int interval = Mathf.Max(1, shopInterval);
            if (!enableRegularShop || round < firstShopRound || (round - firstShopRound) % interval != 0)
            {
                return;
            }

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
            BuildOffers(round, false, false, false);
            RefreshUi(round);
            SetOpen(true);
            gameController?.RecordRoundShopOpened(round);
            gameController?.RequestBanner("전투 상점 입고!  이번 판 전용 상품 3개", new Color(0.38f, 0.82f, 1f), 2.4f);
        }


        private void HandleRoundStarted(int round)
        {
            currentOffers.Clear();
            currentShopIsMini = false;
            currentShopIsRecovery = false;
            currentShopIsInsurance = false;
            currentRecoveryShopPurchaseRecorded = false;
            SetOpen(false);
        }

        private bool ShouldOpenEarlyMiniShop(int round)
        {
            int firstRound = Mathf.Max(1, earlyMiniShopRound);
            int interval = Mathf.Max(1, miniShopInterval);
            bool scheduledRound = round >= firstRound && (round - firstRound) % interval == 0;
            if (!enableEarlyMiniShop ||
                lastMiniShopRound == round ||
                round < firstRound ||
                !scheduledRound)
            {
                if (scheduledRound)
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
            if (!enableEarlyRecoveryShop ||
                earlyRecoveryShopShown ||
                gameController == null ||
                !gameController.EarlyRunRecoveryRecommended ||
                gameController.BadLuckInsuranceAvailable ||
                round < Mathf.Max(1, earlyRecoveryShopFirstRound) ||
                round > Mathf.Max(earlyRecoveryShopFirstRound, earlyRecoveryShopLastRound))
            {
                return false;
            }

            int guaranteedMisses = Mathf.Max(0, guaranteedEarlyRecoveryShopAfterMisses);
            if (!RollLuckyShopAppearance(earlyRecoveryShopAppearanceChance, earlyRecoveryShopMisses, guaranteedMisses))
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

            int safeGuaranteedAfterMisses = Mathf.Max(0, guaranteedAfterMisses);
            if (safeGuaranteedAfterMisses > 0 && missCount >= safeGuaranteedAfterMisses)
            {
                return true;
            }

            float chanceCeiling = Mathf.Clamp(maxLuckyShopAppearanceChance, Mathf.Clamp01(baseChance), 1f);
            float chance = Mathf.Min(chanceCeiling, Mathf.Clamp01(baseChance + Mathf.Max(0, missCount) * missedShopChanceBonus));
            return UnityEngine.Random.value <= chance;
        }

        private void HandleGameOver()
        {
            currentOffers.Clear();
            SetOpen(false);
            lastMiniShopRound = -1;
            earlyRecoveryShopShown = true;
            currentShopIsMini = false;
            currentShopIsRecovery = false;
            currentShopIsInsurance = false;
            currentRecoveryShopPurchaseRecorded = false;
            earlyMiniShopMisses = 0;
            earlyRecoveryShopMisses = 0;
            regularShopMisses = 0;
        }

        private void BuildOffers(int round, bool miniShop, bool recoveryShop, bool insuranceShop)
        {
            currentOffers.Clear();
            if (insuranceShop)
            {
                currentOffers.Add(CreateOffer(ResolveInsuranceOfferType(round), round));
                return;
            }

            List<OfferType> pool = recoveryShop
                ? BuildRecoveryShopPool()
                : miniShop ? BuildMiniShopPool(round) : BuildRegularShopPool(round);

            RemoveUnavailableAugmentOffers(pool);
            int offerCount = recoveryShop ? Mathf.Clamp(earlyRecoveryShopOfferCount, 1, 3) : miniShop ? Mathf.Clamp(earlyMiniShopOfferCount, 1, 3) : 3;
            if (recoveryShop)
            {
                AddOffer(OfferType.RecoveryRareUnit, round, miniShop, recoveryShop);
                pool.Remove(OfferType.RecoveryRareUnit);
            }
            else if (miniShop)
            {
                OfferType anchor = ResolveMiniShopAnchor(pool);
                if (pool.Contains(anchor))
                {
                    AddOffer(anchor, round, miniShop, recoveryShop);
                    pool.Remove(anchor);
                }
            }

            else if (round >= firstShopRound)
            {
                AddOffer(OfferType.BossIntel, round, miniShop, recoveryShop);
                pool.Remove(OfferType.BossIntel);
            }

            OfferType practicalChoice = gameController != null && gameController.Life <= Mathf.CeilToInt(gameController.MaxLife * 0.5f)
                ? OfferType.FieldMedic
                : OfferType.Coupon;
            if (currentOffers.Count < offerCount && pool.Contains(practicalChoice))
            {
                AddOffer(practicalChoice, round, miniShop, recoveryShop);
                pool.Remove(practicalChoice);
            }

            for (int i = 0; i < offerCount && pool.Count > 0; i++)
            {
                if (currentOffers.Count >= offerCount)
                {
                    break;
                }

                int index = UnityEngine.Random.Range(0, pool.Count);
                OfferType type = pool[index];
                pool.RemoveAt(index);
                AddOffer(type, round, miniShop, recoveryShop);
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
                OfferType.Coupon
            };
        }

        private List<OfferType> BuildMiniShopPool(int round)
        {
            if (round <= 7)
            {
                return new List<OfferType>
                {
                    OfferType.RandomUnit,
                    OfferType.MergeAssist,
                    OfferType.Coupon
                };
            }

            List<OfferType> pool = new List<OfferType>
            {
                OfferType.RandomUnit,
                OfferType.RareUnit,
                OfferType.MergeAssist,
                OfferType.TileReroll,
                OfferType.BossIntel,
                OfferType.Coupon
            };
            if (NeedsFieldMedic())
            {
                pool.Add(OfferType.FieldMedic);
            }

            if (round >= 11)
            {
                pool.Add(OfferType.RiskChest);
                pool.Add(OfferType.AugmentPower);
            }

            if (round >= 18)
            {
                pool.Add(OfferType.AugmentGuard);
                pool.Add(OfferType.AugmentSkill);
                pool.Add(OfferType.FateMergeContract);
                pool.Add(OfferType.FateBossContract);
            }

            return pool;
        }

        private List<OfferType> BuildRegularShopPool(int round)
        {
            List<OfferType> pool = new List<OfferType>
            {
                OfferType.RandomUnit,
                OfferType.RareUnit,
                OfferType.RiskChest,
                OfferType.MergeAssist,
                OfferType.TileReroll,
                OfferType.BossIntel,
                OfferType.Coupon,
                OfferType.AugmentPower
            };
            if (NeedsFieldMedic())
            {
                pool.Add(OfferType.FieldMedic);
            }

            if (round >= 18)
            {
                pool.Add(OfferType.AugmentGuard);
                pool.Add(OfferType.AugmentSkill);
                pool.Add(OfferType.FateMergeContract);
                pool.Add(OfferType.FateBossContract);
            }

            return pool;
        }

        private OfferType ResolveMiniShopAnchor(List<OfferType> pool)
        {
            if (pool == null || pool.Count == 0)
            {
                return OfferType.RandomUnit;
            }

            if (gameController != null && gameController.BoardUnitCount >= 2 && pool.Contains(OfferType.MergeAssist))
            {
                return OfferType.MergeAssist;
            }

            return pool.Contains(OfferType.RandomUnit) ? OfferType.RandomUnit : pool[0];
        }

        private bool NeedsFieldMedic()
        {
            if (gameController != null && gameController.Life < gameController.MaxLife)
            {
                return true;
            }

            DefenderUnit[] defenders = boardManager != null ? boardManager.GetAliveDefenders() : new DefenderUnit[0];
            for (int i = 0; i < defenders.Length; i++)
            {
                if (defenders[i] != null && defenders[i].HealthRatio < 0.92f)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ApplyFixedRoundShopPrice(ShopOffer offer, int round, bool recoveryShop)
        {
            if (offer == null || offer.cost <= 0)
            {
                return;
            }

            offer.cost = recoveryShop
                ? ResolveFixedRecoveryShopPrice(offer.type, round)
                : ResolveFixedMiniShopPrice(offer.type, round);
        }

        private static int ResolveFixedMiniShopPrice(OfferType type, int round)
        {
            int target = ResolveFixedMiniShopSpendTarget(round);
            switch (type)
            {
                case OfferType.RandomUnit:
                    return Mathf.Max(1, target - 1);
                case OfferType.Coupon:
                    return Mathf.Max(1, target - 2);
                case OfferType.TileReroll:
                    return Mathf.Max(1, target - 4);
                case OfferType.FieldMedic:
                    return Mathf.Max(1, target - 3);
                case OfferType.RiskChest:
                    return Mathf.Max(1, target - 4);
                default:
                    return target;
            }
        }

        private static int ResolveFixedMiniShopSpendTarget(int round)
        {
            if (round <= 7) return 20;
            if (round <= 15) return 34;
            if (round <= 23) return 50;
            if (round <= 31) return 68;
            if (round <= 39) return 88;
            if (round <= 47) return 110;

            int extraTiers = Mathf.Max(0, (round - 48) / 8);
            return 132 + extraTiers * 22;
        }

        private static int ResolveFixedRecoveryShopPrice(OfferType type, int round)
        {
            int target = round <= 5 ? 8 : round <= 7 ? 10 : 12;
            switch (type)
            {
                case OfferType.RecoveryBossPrep:
                case OfferType.FieldMedic:
                    return Mathf.Max(1, target - 2);
                case OfferType.MergeAssist:
                    return target + 2;
                default:
                    return target;
            }
        }

        private void RemoveUnavailableAugmentOffers(List<OfferType> pool)
        {
            if (pool == null)
            {
                return;
            }

            if (augmentManager == null || augmentManager.HasChosenAugment("power_core"))
            {
                pool.Remove(OfferType.AugmentPower);
            }
            if (augmentManager == null || augmentManager.HasChosenAugment("guardian_heart"))
            {
                pool.Remove(OfferType.AugmentGuard);
            }
            if (augmentManager == null || augmentManager.HasChosenAugment("skill_overload"))
            {
                pool.Remove(OfferType.AugmentSkill);
            }
        }

        private OfferType ResolveInsuranceOfferType(int round)
        {
            if (gameController == null)
            {
                return OfferType.InsuranceMergeMaterial;
            }

            float lifeRatio = gameController.MaxLife > 0
                ? Mathf.Clamp01((float)gameController.Life / gameController.MaxLife)
                : 1f;
            if (lifeRatio <= 0.55f)
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
            ShopOffer offer = CreateOffer(type, round);
            if (offer == null)
            {
                return;
            }

            bool miniPrefix = miniShop && !recoveryShop;
            if (miniPrefix)
            {
                offer.title = "소형 " + offer.title;
            }

            if (miniShop || recoveryShop)
            {
                // Small shop prices follow a deterministic round economy schedule.
                // Player gold, summon cost, fortune and debt never silently reprice them.
                ApplyFixedRoundShopPrice(offer, round, recoveryShop);
            }
            else
            {
                ApplyRoundShopInflation(offer, round);
                ApplySummonOpportunityCostFloor(offer, false, false);
                ApplyDailyFortuneOfferModifier(offer);
                ApplyFateOfferModifier(offer);
            }
            ApplyReadableFateOfferText(offer, miniPrefix);
            currentOffers.Add(offer);
        }


        private static void ApplyRoundShopInflation(ShopOffer offer, int round)
        {
            if (offer == null || offer.cost <= 0)
            {
                return;
            }

            float multiplier = round >= 21 ? 1.30f : round >= 15 ? 1.15f : 1f;
            if (multiplier <= 1f)
            {
                return;
            }

            offer.cost = Mathf.Max(1, Mathf.CeilToInt(offer.cost * multiplier));
            offer.description += " 장기 진행 물가가 적용됩니다.";
        }

        private void ApplySummonOpportunityCostFloor(ShopOffer offer, bool miniShop, bool recoveryShop)
        {
            if (offer == null || offer.cost <= 0 || gameController == null)
            {
                return;
            }

            float summonEquivalent = ResolveMinimumSummonEquivalent(offer.type);
            if (summonEquivalent <= 0f)
            {
                return;
            }

            float shopRate = miniShop
                ? miniShopOpportunityRate
                : recoveryShop ? recoveryShopOpportunityRate : regularShopOpportunityRate;
            int summonReferenceCost = Mathf.Max(1, gameController.SummonCost);
            int minimumCost = Mathf.CeilToInt(summonReferenceCost * summonEquivalent * Mathf.Max(0.1f, shopRate));
            offer.cost = Mathf.Max(offer.cost, minimumCost);
        }

        private static float ResolveMinimumSummonEquivalent(OfferType type)
        {
            switch (type)
            {
                case OfferType.RandomUnit:
                    return 2.40f;
                case OfferType.RareUnit:
                    return 5.20f;
                case OfferType.RiskChest:
                    return 6.30f;
                case OfferType.MergeAssist:
                    return 3.20f;
                case OfferType.TileReroll:
                    return 3.40f;
                case OfferType.BossIntel:
                    return 6.80f;
                case OfferType.Coupon:
                    return 1.20f;
                case OfferType.FieldMedic:
                    return 3.60f;
                case OfferType.RecoveryRareUnit:
                    return 3.80f;
                case OfferType.RecoveryBossPrep:
                    return 5.00f;
                case OfferType.AugmentPower:
                case OfferType.AugmentGuard:
                case OfferType.AugmentSkill:
                    return 8.00f;
                default:
                    return 0f;
            }
        }

        private static void ApplyDailyFortuneOfferModifier(ShopOffer offer)
        {
            if (offer == null || offer.cost <= 0)
            {
                return;
            }

            DailyFortuneRule fortune = DailyFortuneSystem.Today;
            if (fortune == null || fortune.shopDiscountRate <= 0f)
            {
                return;
            }

            offer.cost = Mathf.Max(1, Mathf.RoundToInt(offer.cost * fortune.ShopCostMultiplier));
            offer.description += " 오늘의 운세 할인 적용.";
        }

        private void ApplyFateOfferModifier(ShopOffer offer)
        {
            if (offer == null || offer.cost <= 0 || gameController == null)
            {
                return;
            }

            int previousCost = offer.cost;
            int adjustedCost = gameController.ApplyFateShopDebtCost(offer.cost);
            if (adjustedCost != offer.cost)
            {
                offer.cost = adjustedCost;
                offer.debtCostPenalty = Mathf.Max(0, adjustedCost - previousCost);
                offer.description += " 운명 빚 상점가 반영.";
            }
        }

        private static void ApplyReadableFateOfferText(ShopOffer offer, bool miniPrefix)
        {
            if (offer == null)
            {
                return;
            }

            string prefix = miniPrefix ? "소형 " : string.Empty;
            switch (offer.type)
            {
                case OfferType.FateMergeContract:
                    offer.title = prefix + "운명 계약: 합성 올인";
                    offer.description = "라이프 -2. 합성 재료와 소환비 할인을 받아 저점 판을 밀어 올립니다.";
                    offer.priceLabel = "라이프 -2";
                    break;
                case OfferType.FateBossContract:
                    offer.title = prefix + "운명 계약: 보스 사냥";
                    offer.description = "라이프 -2. 보스 피해와 소량의 보스 전 골드를 받지만 빚이 크게 남습니다.";
                    offer.priceLabel = "라이프 -2";
                    break;
                case OfferType.FateShopReroll:
                    offer.title = prefix + "운명 개입: 상점 리롤";
                    offer.description = "운명을 써서 상품 3개를 다시 뽑습니다. 대가는 빚으로 남습니다.";
                    offer.priceLabel = "운명";
                    break;
                case OfferType.FateGradeLock:
                    offer.title = prefix + "운명 개입: Rare+ 3회";
                    offer.description = "다음 3회 소환을 레어 이상으로 고정합니다. 초반 저점을 줄입니다.";
                    offer.priceLabel = "운명";
                    break;
                case OfferType.FateNormalBan:
                    offer.title = prefix + "운명 개입: 일반 제외 4회";
                    offer.description = "다음 4회 소환에서 일반을 제외합니다. 빈 손 판을 비틉니다.";
                    offer.priceLabel = "운명";
                    break;
                case OfferType.FateForceShop:
                    offer.title = prefix + "운명 개입: 다음 상점";
                    offer.description = "다음 라운드 상점을 확정합니다. 필요한 선택지를 직접 끌어옵니다.";
                    offer.priceLabel = "운명";
                    break;
            }
        }

        private ShopOffer CreateOffer(OfferType type, int round)
        {
            int scale = Mathf.Max(0, round / 3);
            switch (type)
            {
                case OfferType.RandomUnit:
                    return new ShopOffer
                    {
                        type = type,
                        title = "긴급 소환권",
                        description = "현재 라운드 확률표로 랜덤 유닛 1마리를 즉시 배치합니다.",
                        cost = 14 + scale * 3,
                        color = new Color(0.34f, 0.82f, 1f)
                    };
                case OfferType.RareUnit:
                    return new ShopOffer
                    {
                        type = type,
                        title = "레어 보급상자",
                        description = "레어 유닛 1마리를 즉시 배치합니다. 초반 허들 돌파용입니다.",
                        cost = 26 + scale * 7,
                        color = new Color(0.30f, 0.60f, 1f)
                    };
                case OfferType.RiskChest:
                    return new ShopOffer
                    {
                        type = type,
                        title = "위험한 상자",
                        description = "라이프 -1. 레어 이상 확정, 낮은 확률로 에픽/전설까지 튀는 도박 상자입니다.",
                        cost = 38 + scale * 9,
                        priceLabel = "골드+HP",
                        color = new Color(1f, 0.42f, 0.82f)
                    };
                case OfferType.MergeAssist:
                    return new ShopOffer
                    {
                        type = type,
                        title = "합성 부스터",
                        description = "보유 중인 등급의 부족 재료를 보급해 첫 합성선에 닿도록 도와줍니다.",
                        cost = 9 + scale * 2,
                        color = new Color(0.78f, 1f, 0.34f)
                    };
                case OfferType.TileReroll:
                    return new ShopOffer
                    {
                        type = type,
                        title = "타일 재배치",
                        description = "전술 타일을 즉시 다시 뽑습니다. 현재 배치가 답답할 때 좋습니다.",
                        cost = 15 + scale * 4,
                        color = new Color(0.42f, 1f, 0.88f)
                    };
                case OfferType.BossIntel:
                    return new ShopOffer
                    {
                        type = type,
                        title = "보스 정보상",
                        description = "현재 보유 유닛에게 공격 +8%, 보스 피해 +30%를 영구 적용합니다.",
                        cost = 22 + scale * 5,
                        color = new Color(1f, 0.70f, 0.20f)
                    };
                case OfferType.Coupon:
                    return new ShopOffer
                    {
                        type = type,
                        title = "4라운드 소환 패스",
                        description = "다음 4라운드 동안 소환 비용을 18% 낮춥니다. 초반에 여러 번 소환할수록 이득이 커집니다.",
                        cost = 8 + scale * 2,
                        color = new Color(0.56f, 0.92f, 1f)
                    };
                case OfferType.FateMergeContract:
                    return new ShopOffer
                    {
                        type = type,
                        title = "운명 계약: 합성 올인",
                        description = "라이프 -2와 운명 빚을 남깁니다. 합성 재료 보급과 소환비 7% 할인을 받습니다.",
                        cost = 0,
                        priceLabel = "라이프 -2",
                        color = new Color(1f, 0.48f, 0.82f)
                    };
                case OfferType.FateBossContract:
                    return new ShopOffer
                    {
                        type = type,
                        title = "운명 계약: 보스 사냥",
                        description = "라이프 -2와 운명 빚을 남깁니다. 보스 피해 +16%, 보스 전 소량의 골드를 받습니다.",
                        cost = 0,
                        priceLabel = "라이프 -2",
                        color = new Color(1f, 0.66f, 0.24f)
                    };
                case OfferType.FateShopReroll:
                    return new ShopOffer
                    {
                        type = type,
                        title = "운명 개입: 상점 리롤",
                        description = "운명 게이지를 써서 현재 상점 상품 3개를 다시 뽑습니다. 빚은 다음 상점/보스에 정산됩니다.",
                        cost = 0,
                        priceLabel = "운명",
                        color = new Color(0.78f, 0.50f, 1f)
                    };
                case OfferType.FateGradeLock:
                    return new ShopOffer
                    {
                        type = type,
                        title = "운명 개입: 레어 잠금",
                        description = "운명 게이지를 써서 다음 2회 소환을 레어 이상으로 잠급니다. 빚은 다음 상점/보스에 정산됩니다.",
                        cost = 0,
                        priceLabel = "운명",
                        color = new Color(1f, 0.46f, 0.92f)
                    };
                case OfferType.FateNormalBan:
                    return new ShopOffer
                    {
                        type = type,
                        title = "운명 개입: 일반 금지",
                        description = "운명 게이지를 써서 다음 3회 소환에서 일반 등급을 제외합니다. 저점 판을 직접 비틉니다.",
                        cost = 0,
                        priceLabel = "운명",
                        color = new Color(0.44f, 1f, 0.78f)
                    };
                case OfferType.FateForceShop:
                    return new ShopOffer
                    {
                        type = type,
                        title = "운명 개입: 상점 강제",
                        description = "운명 게이지를 써서 다음 라운드 상점을 확정 등장시킵니다. 필요한 선택지를 직접 끌어옵니다.",
                        cost = 0,
                        priceLabel = "운명",
                        color = new Color(0.52f, 0.78f, 1f)
                    };
                case OfferType.RecoveryRareUnit:
                    return new ShopOffer
                    {
                        type = type,
                        title = "구제 레어 지원",
                        description = "레어 유닛 1마리를 낮은 비용으로 배치합니다. 빈 슬롯이 필요합니다.",
                        cost = 2 + scale,
                        color = new Color(0.32f, 0.68f, 1f)
                    };
                case OfferType.RecoveryBossPrep:
                    return new ShopOffer
                    {
                        type = type,
                        title = "보스 대비 패키지",
                        description = "모든 생존 유닛 체력 30% 회복, 공격력 +5%, 보스 피해 +20%.",
                        cost = 3 + scale,
                        color = new Color(1f, 0.58f, 0.24f)
                    };
                case OfferType.InsuranceMergeMaterial:
                    return new ShopOffer
                    {
                        type = type,
                        title = "합성 재료 보험",
                        description = "가장 가까운 합성선의 부족 재료를 1회 보급합니다.",
                        cost = 0,
                        priceLabel = "보험 1회",
                        color = new Color(0.76f, 1f, 0.34f)
                    };
                case OfferType.InsuranceRecoveryTicket:
                    return new ShopOffer
                    {
                        type = type,
                        title = "회복권 보험",
                        description = "골드 +4와 모든 생존 유닛 체력 20% 회복. 초반 저점을 작게 복구합니다.",
                        cost = 0,
                        priceLabel = "보험 1회",
                        color = new Color(0.40f, 1f, 0.68f)
                    };
                case OfferType.InsuranceBossCounter:
                    return new ShopOffer
                    {
                        type = type,
                        title = "보스 대응권",
                        description = "모든 생존 유닛 체력 18% 회복, 보스 피해 +8%.",
                        cost = 0,
                        priceLabel = "보험 1회",
                        color = new Color(1f, 0.64f, 0.22f)
                    };
                case OfferType.FieldMedic:
                    return new ShopOffer
                    {
                        type = type,
                        title = "현장 의무병",
                        description = "모든 생존 유닛 체력 25% 회복. 방어선 HP가 절반 이하면 HP도 1 회복합니다.",
                        cost = 18 + scale * 4,
                        color = new Color(0.48f, 1f, 0.60f)
                    };
                case OfferType.AugmentPower:
                    return new ShopOffer
                    {
                        type = type,
                        title = "증강체: 화력 코어",
                        description = "이번 판 동안 모든 현재·미래 유닛 공격력 +20%. 1회만 획득 가능합니다.",
                        cost = 28 + scale * 6,
                        color = new Color(1f, 0.57f, 0.28f)
                    };
                case OfferType.AugmentGuard:
                    return new ShopOffer
                    {
                        type = type,
                        title = "증강체: 수호자의 심장",
                        description = "이번 판 동안 모든 현재·미래 유닛 최대 체력 +24%. 1회만 획득 가능합니다.",
                        cost = 26 + scale * 6,
                        color = new Color(0.35f, 1f, 0.62f)
                    };
                case OfferType.AugmentSkill:
                    return new ShopOffer
                    {
                        type = type,
                        title = "증강체: 스킬 과부하",
                        description = "이번 판 동안 모든 현재·미래 유닛 스킬 위력 +22%. 1회만 획득 가능합니다.",
                        cost = 30 + scale * 7,
                        color = new Color(0.96f, 0.50f, 1f)
                    };
                default:
                    return null;
            }
        }

        private void BuyOffer(int index)
        {
            if (gameController != null && gameController.IsRoundRunning)
            {
                HandleRoundStarted(gameController.CurrentRound);
                return;
            }

            if (index < 0 || index >= currentOffers.Count || gameController == null)
            {
                return;
            }

            ShopOffer offer = currentOffers[index];
            if (offer == null)
            {
                return;
            }

            if (gameController.Gold < offer.cost)
            {
                gameController.RequestBanner("골드 부족  " + offer.title + " 구매 불가", new Color(1f, 0.42f, 0.32f), 1.8f);
                return;
            }

            gameController.RemoveGold(offer.cost);

            bool applied = ApplyOffer(offer);
            if (!applied)
            {
                gameController.AddGold(offer.cost);
                gameController.RequestBanner("구매 실패  빈 슬롯 또는 대상이 필요합니다", new Color(1f, 0.48f, 0.30f), 1.8f);
                return;
            }

            gameController.RecordFateShopCostPenalty(offer.debtCostPenalty);
            gameController.RequestBanner("구매 완료!  " + offer.title, offer.color, 2.2f);
            if (offer.type == OfferType.FateShopReroll)
            {
                BuildOffers(gameController.CurrentRound, currentShopIsMini, currentShopIsRecovery, currentShopIsInsurance);
                RefreshUi(gameController.CurrentRound);
                return;
            }

            if (currentShopIsMini && (offer.type == OfferType.MergeAssist || offer.type == OfferType.FateMergeContract))
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
                SetOpen(false);
                return;
            }

            if (currentShopIsMini)
            {
                currentOffers.Clear();
                currentShopIsMini = false;
                SetOpen(false);
                return;
            }

            if (currentShopIsInsurance)
            {
                gameController.MarkBadLuckInsuranceClaimed(offer.title);
                currentOffers.Clear();
                SetOpen(false);
                return;
            }

            currentOffers.RemoveAt(index);
            RefreshUi(gameController.CurrentRound);
            if (currentOffers.Count == 0)
            {
                SetOpen(false);
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
                    if (!gameController.TrySpendLifeForContract(1, "위험한 상자"))
                    {
                        return false;
                    }

                    float roll = UnityEngine.Random.value;
                    CharacterGrade grade = roll < 0.10f ? CharacterGrade.Legendary : roll < 0.38f ? CharacterGrade.Epic : CharacterGrade.Rare;
                    return gameController.TryGrantRandomUnitByGrade(grade);
                case OfferType.MergeAssist:
                    return gameController.TryGrantMergeAssistUnit();
                case OfferType.TileReroll:
                    if (tileModifierSystem == null)
                    {
                        return false;
                    }

                    tileModifierSystem.RerollTiles(true, "상점 타일 재배치");
                    return true;
                case OfferType.BossIntel:
                    DefenderUnit[] defenders = boardManager != null ? boardManager.GetAliveDefenders() : new DefenderUnit[0];
                    if (defenders.Length == 0)
                    {
                        return false;
                    }

                    for (int i = 0; i < defenders.Length; i++)
                    {
                        if (defenders[i] == null)
                        {
                            continue;
                        }

                        defenders[i].AddAttackPowerBonus(0.08f);
                        defenders[i].AddBossDamageBonus(0.30f);
                    }

                    return true;
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
                    if (!gameController.TrySpendLifeForContract(2, "합성 올인"))
                    {
                        return false;
                    }

                    bool mergeGranted = gameController.TryGrantMergeAssistUnit();
                    gameController.AddSummonCostDiscount(0.07f);
                    if (!mergeGranted)
                    {
                        gameController.AddGold(6);
                    }

                    return true;
                case OfferType.FateBossContract:
                    if (!gameController.TrySpendLifeForContract(2, "보스 사냥"))
                    {
                        return false;
                    }

                    DefenderUnit[] fateBossUnits = boardManager != null ? boardManager.GetAliveDefenders() : new DefenderUnit[0];
                    bool buffed = false;
                    for (int i = 0; i < fateBossUnits.Length; i++)
                    {
                        if (fateBossUnits[i] == null)
                        {
                            continue;
                        }

                        fateBossUnits[i].AddBossDamageBonus(0.16f);
                        buffed = true;
                    }

                    if (!buffed)
                    {
                        gameController.TryGrantRandomUnitByGrade(CharacterGrade.Rare);
                    }

                    gameController.AddGold(6);
                    gameController.AddRoundGoldBonus(1);
                    return true;
                case OfferType.RecoveryRareUnit:
                    return gameController.TryGrantRandomUnitByGrade(CharacterGrade.Rare);
                case OfferType.RecoveryBossPrep:
                    DefenderUnit[] bossPrepUnits = boardManager != null ? boardManager.GetAliveDefenders() : new DefenderUnit[0];
                    if (bossPrepUnits.Length == 0)
                    {
                        return false;
                    }

                    for (int i = 0; i < bossPrepUnits.Length; i++)
                    {
                        if (bossPrepUnits[i] == null)
                        {
                            continue;
                        }

                        bossPrepUnits[i].Heal(bossPrepUnits[i].MaxHealth * 0.30f);
                        bossPrepUnits[i].AddAttackPowerBonus(0.05f);
                        bossPrepUnits[i].AddBossDamageBonus(0.20f);
                    }

                    return true;
                case OfferType.InsuranceMergeMaterial:
                    return gameController.TryGrantMergeAssistUnit() || gameController.TryGrantRandomUnitByGrade(CharacterGrade.Rare);
                case OfferType.InsuranceRecoveryTicket:
                    gameController.AddGold(4);
                    HealAliveDefenders(0.20f);
                    return true;
                case OfferType.InsuranceBossCounter:
                    DefenderUnit[] counterUnits = boardManager != null ? boardManager.GetAliveDefenders() : new DefenderUnit[0];
                    if (counterUnits.Length == 0)
                    {
                        return gameController.TryGrantRandomUnitByGrade(CharacterGrade.Rare);
                    }

                    for (int i = 0; i < counterUnits.Length; i++)
                    {
                        if (counterUnits[i] == null)
                        {
                            continue;
                        }

                        counterUnits[i].Heal(counterUnits[i].MaxHealth * 0.18f);
                        counterUnits[i].AddBossDamageBonus(0.08f);
                    }

                    return true;
                case OfferType.FieldMedic:
                    DefenderUnit[] units = boardManager != null ? boardManager.GetAliveDefenders() : new DefenderUnit[0];
                    if (units.Length == 0)
                    {
                        return false;
                    }

                    for (int i = 0; i < units.Length; i++)
                    {
                        units[i]?.Heal(units[i].MaxHealth * 0.25f);
                    }


                    if (gameController.Life <= Mathf.CeilToInt(gameController.MaxLife * 0.5f))
                    {
                        gameController.RecoverLife(1);
                    }
                    return true;
                case OfferType.AugmentPower:
                    return augmentManager != null && augmentManager.TryGrantShopAugment("power_core");
                case OfferType.AugmentGuard:
                    return augmentManager != null && augmentManager.TryGrantShopAugment("guardian_heart");
                case OfferType.AugmentSkill:
                    return augmentManager != null && augmentManager.TryGrantShopAugment("skill_overload");
                default:
                    return false;
            }
        }

        private void HealAliveDefenders(float ratio)
        {
            DefenderUnit[] units = boardManager != null ? boardManager.GetAliveDefenders() : new DefenderUnit[0];
            for (int i = 0; i < units.Length; i++)
            {
                if (units[i] != null)
                {
                    units[i].Heal(units[i].MaxHealth * Mathf.Clamp01(ratio));
                }
            }
        }

        private void CacheVisualIdentityTargets()
        {
            Transform modal = panelRoot != null ? panelRoot.transform.Find("RunShopModal") : null;
            if (modal == null)
            {
                return;
            }

            modalImage = modal.GetComponent<Image>();
            headerPillImage = GetChildImage(modal, "RunShopHeaderPill");
            topLineImage = GetChildImage(modal, "RunShopTopLine");
            bottomLineImage = GetChildImage(modal, "RunShopBottomLine");
            leftRailImage = GetChildImage(modal, "RunShopLeftRail");
            rightRailImage = GetChildImage(modal, "RunShopRightRail");
            Transform footer = modal.Find("RunShopFooterHint");
            footerHintText = footer != null ? footer.GetComponent<Text>() : null;
        }

        private void ApplyShopVisualIdentity()
        {
            Color accent;
            Color modalColor;
            Color cardColor;
            Color dockColor;
            Vector2 headerSize;
            bool showTop;
            bool showBottom;
            bool showLeft;
            bool showRight;
            Vector2 topSize;
            Vector2 bottomSize;

            if (currentShopIsInsurance)
            {
                accent = new Color(0.68f, 0.96f, 0.30f, 1f);
                modalColor = new Color(0.06f, 0.20f, 0.16f, 0.99f);
                cardColor = new Color(0.08f, 0.26f, 0.18f, 0.99f);
                dockColor = new Color(0.04f, 0.14f, 0.11f, 0.99f);
                headerSize = new Vector2(430f, 74f);
                showTop = true;
                showBottom = true;
                showLeft = true;
                showRight = false;
                topSize = new Vector2(720f, 6f);
                bottomSize = new Vector2(310f, 6f);
            }
            else if (currentShopIsRecovery)
            {
                accent = new Color(1f, 0.62f, 0.18f, 1f);
                modalColor = new Color(0.22f, 0.12f, 0.08f, 0.99f);
                cardColor = new Color(0.27f, 0.16f, 0.10f, 0.99f);
                dockColor = new Color(0.16f, 0.08f, 0.05f, 0.99f);
                headerSize = new Vector2(500f, 76f);
                showTop = true;
                showBottom = false;
                showLeft = true;
                showRight = true;
                topSize = new Vector2(300f, 7f);
                bottomSize = new Vector2(300f, 5f);
            }
            else if (currentShopIsMini)
            {
                accent = new Color(0.22f, 0.92f, 0.78f, 1f);
                modalColor = new Color(0.04f, 0.20f, 0.25f, 0.99f);
                cardColor = new Color(0.05f, 0.26f, 0.29f, 0.99f);
                dockColor = new Color(0.03f, 0.14f, 0.18f, 0.99f);
                headerSize = new Vector2(440f, 68f);
                showTop = true;
                showBottom = true;
                showLeft = false;
                showRight = false;
                topSize = new Vector2(630f, 5f);
                bottomSize = new Vector2(280f, 5f);
            }
            else
            {
                accent = new Color(0.28f, 0.78f, 1f, 1f);
                modalColor = new Color(0.065f, 0.11f, 0.30f, 0.99f);
                cardColor = new Color(0.09f, 0.16f, 0.36f, 0.99f);
                dockColor = new Color(0.055f, 0.09f, 0.23f, 0.99f);
                headerSize = new Vector2(360f, 66f);
                showTop = true;
                showBottom = true;
                showLeft = false;
                showRight = false;
                topSize = new Vector2(720f, 5f);
                bottomSize = new Vector2(720f, 5f);
            }

            if (modalImage != null) modalImage.color = modalColor;
            if (headerPillImage != null)
            {
                headerPillImage.color = accent;
                headerPillImage.rectTransform.sizeDelta = headerSize;
            }

            SetIdentityLine(topLineImage, showTop, accent, topSize);
            SetIdentityLine(bottomLineImage, showBottom, new Color(accent.r, accent.g, accent.b, 0.78f), bottomSize);
            SetIdentityLine(leftRailImage, showLeft, accent, new Vector2(currentShopIsRecovery ? 9f : 7f, 620f));
            SetIdentityLine(rightRailImage, showRight, new Color(accent.r, accent.g, accent.b, 0.82f), new Vector2(currentShopIsRecovery ? 9f : 7f, 620f));

            if (headerText != null) headerText.color = Color.white;
            if (subtitleText != null) subtitleText.color = Color.Lerp(Color.white, accent, 0.34f);
            if (reopenButton != null && reopenButton.targetGraphic is Image reopenImage) reopenImage.color = accent;

            if (footerHintText != null)
            {
                footerHintText.color = Color.Lerp(Color.white, accent, 0.30f);
                footerHintText.text = currentShopIsInsurance
                    ? "추천된 보험 1개를 확인하세요."
                    : currentShopIsRecovery
                    ? "3개 중 1개만 선택할 수 있습니다. 선택 즉시 긴급 지원이 종료됩니다."
                    : currentShopIsMini
                    ? "3개 중 1개를 구매하면 소형 전투 상점이 종료됩니다."
                    : "구매하지 않고 닫으면 이번 상점은 지나갑니다.";
            }

            int count = offerButtons != null ? offerButtons.Length : 0;
            for (int i = 0; i < count; i++)
            {
                Button button = offerButtons[i];
                if (button == null)
                {
                    continue;
                }

                RectTransform rect = button.GetComponent<RectTransform>();
                float y = -142f - i * 202f;
                float x = 0f;
                Vector2 size = new Vector2(820f, 178f);
                if (currentShopIsRecovery)
                {
                    x = i % 2 == 0 ? -22f : 22f;
                    size = new Vector2(770f, 178f);
                }
                else if (currentShopIsMini)
                {
                    x = (i - 1) * 16f;
                    size = new Vector2(790f, 178f);
                }
                else if (currentShopIsInsurance)
                {
                    y = -230f;
                    size = new Vector2(780f, 220f);
                }

                rect.anchoredPosition = new Vector2(x, y);
                rect.sizeDelta = size;
                if (button.targetGraphic is Image cardImage) cardImage.color = cardColor;
                Outline outline = button.GetComponent<Outline>();
                if (outline != null) outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.96f);
                Image dock = GetChildImage(button.transform, "PriceDock");
                if (dock != null) dock.color = dockColor;
                Image badgeBack = GetChildImage(button.transform, "RunShopIconBadgeBack");
                if (badgeBack != null) badgeBack.color = Color.Lerp(modalColor, accent, 0.46f);
            }
        }

        private static void SetIdentityLine(Image image, bool visible, Color color, Vector2 size)
        {
            if (image == null)
            {
                return;
            }

            image.gameObject.SetActive(visible);
            image.color = color;
            image.rectTransform.sizeDelta = size;
        }

        private static Image GetChildImage(Transform parent, string childName)
        {
            Transform child = parent != null ? parent.Find(childName) : null;
            return child != null ? child.GetComponent<Image>() : null;
        }

        private void RefreshUi(int round)
        {
            ApplyShopVisualIdentity();
            if (headerText != null)
            {
                headerText.text = currentShopIsInsurance
                    ? "운 나쁨 보험"
                    : currentShopIsRecovery ? "긴급 지원" : currentShopIsMini ? "소형 전투 상점" : "전투 상점";
            }

            if (subtitleText != null)
            {
                string phaseLabel = round <= 7 ? "초반 성장" : round <= 17 ? "중반 빌드" : "후반 고효율";
                string opportunityLabel = currentShopIsMini
                    ? "고정 선택가 " + ResolveFixedMiniShopSpendTarget(round) + "G 안팎"
                    : currentShopIsRecovery
                    ? "위기 지원 고정가"
                    : gameController != null ? "현재 소환비 " + gameController.SummonCost + "G" : "소환비 확인";
                subtitleText.text = currentShopIsInsurance
                    ? "보험 추천 1개  |  " + (gameController != null ? gameController.BadLuckInsuranceReason : "초반 저점 복구") + "  |  " + DailyFortuneSystem.TodaySummary
                    : currentShopIsRecovery
                    ? "3개 중 1개  |  " + opportunityLabel + "  |  " + (gameController != null ? gameController.EarlyRunRecoveryCause : "위기 복구")
                    : currentShopIsMini
                    ? "ROUND " + round + "  |  " + phaseLabel + "  |  " + opportunityLabel + "  |  3개 중 1개"
                    : "ROUND " + round + "  |  " + opportunityLabel + "  |  유닛 소환 vs 상점 투자";
            }

            int buttonCount = offerButtons != null ? offerButtons.Length : 0;
            for (int i = 0; i < buttonCount; i++)
            {
                bool show = i < currentOffers.Count;
                if (offerButtons[i] != null)
                {
                    offerButtons[i].gameObject.SetActive(show);
                }

                if (!show)
                {
                    continue;
                }

                ShopOffer offer = currentOffers[i];
                SetText(GetText(offerTitleTexts, i), offer.title);
                SetText(GetText(offerDescriptionTexts, i), offer.description);
                SetText(GetText(offerPriceTexts, i), BuildOfferPriceLabel(offer));
                Image accent = GetImage(offerAccentImages, i);
                if (accent != null)
                {
                    accent.color = offer.color;
                }
            }
        }

        private string BuildOfferPriceLabel(ShopOffer offer)
        {
            if (offer == null)
            {
                return string.Empty;
            }

            int summonEquivalent = ResolveOfferSummonEquivalent(offer);
            if (!string.IsNullOrWhiteSpace(offer.priceLabel))
            {
                if (offer.priceLabel == "골드+HP")
                {
                    return offer.cost + "G + HP -1 / 소환 약 " + summonEquivalent + "회";
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

            return offer.cost + "G / 소환 약 " + summonEquivalent + "회";
        }

        private int ResolveOfferSummonEquivalent(ShopOffer offer)
        {
            int summonReferenceCost = gameController != null ? Mathf.Max(1, gameController.SummonCost) : 10;
            return offer != null && offer.cost > 0 ? Mathf.Max(1, Mathf.CeilToInt((float)offer.cost / summonReferenceCost)) : 0;
        }

        private void SetOpen(bool open)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(open);
            }

            UpdateReopenButton();
        }

        private void Close()
        {
            // Insurance is a separate safety choice and may be reopened until it is claimed.
            // Every paid shop is optional: closing it means passing this appearance entirely.
            if (currentShopIsInsurance)
            {
                SetOpen(false);
                return;
            }

            bool passedShop = currentOffers.Count > 0;
            string passedShopLabel = currentShopIsRecovery
                ? "긴급 지원"
                : currentShopIsMini ? "소형 전투상점" : "전투상점";

            currentOffers.Clear();
            currentShopIsMini = false;
            currentShopIsRecovery = false;
            currentShopIsInsurance = false;
            currentRecoveryShopPurchaseRecorded = false;
            SetOpen(false);

            if (passedShop)
            {
                gameController?.RequestBanner(
                    passedShopLabel + " 패스",
                    new Color(0.66f, 0.72f, 0.84f),
                    1.6f);
            }
        }

        private void Open()
        {
            if (currentOffers.Count == 0)
            {
                UpdateReopenButton();
                return;
            }

            SetOpen(true);
        }

        private void UpdateReopenButton()
        {
            if (reopenButton != null)
            {
                bool panelOpen = panelRoot != null && panelRoot.activeSelf;
                reopenButton.gameObject.SetActive(currentOffers.Count > 0 && !panelOpen);
                Text label = reopenButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = currentShopIsInsurance ? "보험" : currentShopIsRecovery ? "지원" : "상점";
                }
            }
        }

        private static Text GetText(Text[] texts, int index)
        {
            return texts != null && index >= 0 && index < texts.Length ? texts[index] : null;
        }

        private static Image GetImage(Image[] images, int index)
        {
            return images != null && index >= 0 && index < images.Length ? images[index] : null;
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
