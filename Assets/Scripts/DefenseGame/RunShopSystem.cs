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
            AugmentSkill,
            VanguardDrill,
            TargetingLens,
            ManaBattery,
            SalvageCrate,
            TwinRecruit,
            FusionWorkshop,
            CriticalForge,
            ArcaneConductor,
            EpicDraft,
            BossRaidWager
        }

        private enum OfferRarity
        {
            Normal,
            Rare,
            Legendary
        }

        private enum OfferRole
        {
            Supply,
            Build,
            Tactical,
            Wild
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
            public OfferRarity rarity;
        }

        [SerializeField] private DefenseGameController gameController;
        [SerializeField] private DefenseBoardManager boardManager;
        [SerializeField] private BoardTileModifierSystem tileModifierSystem;
        [SerializeField] private AugmentManager augmentManager;
        [SerializeField] private bool enableRegularShop = false;
        [SerializeField] private int firstShopRound = 11;
        [SerializeField] private int shopInterval = 8;
        [SerializeField] private bool enableEarlyMiniShop = true;
        [SerializeField] private int earlyMiniShopRound = 11;
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
        [SerializeField, Range(3, 12)] private int recentOfferHistorySize = 6;
        [SerializeField, Range(1, 5)] private int retryOfferHistoryRuns = 3;
        private readonly List<OfferType> recentOfferHistory = new List<OfferType>();

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
                recentOfferHistory.Clear();
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
                BuildOffers(round, true, false, false, true);
                RefreshUi(round);
                SetOpen(true);
                gameController?.RecordRoundShopOpened(round);
                int offerCount = Mathf.Clamp(earlyMiniShopOfferCount, 1, 3);
                gameController?.RequestBanner("전투 선택지 입고!  소형 상점 " + offerCount + "개", new Color(0.48f, 1f, 0.74f), 2.4f);
                gameController?.RecordFirstMiniShopOffer();
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
            BuildOffers(round, false, false, false, true);
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

        public bool IsPanelOpen => panelRoot != null && panelRoot.activeSelf;

        public int GetNextScheduledMiniShopRound(int completedRound)
        {
            if (!enableEarlyMiniShop)
            {
                return -1;
            }

            int firstRound = Mathf.Max(1, earlyMiniShopRound);
            int interval = Mathf.Max(1, miniShopInterval);
            int nextRound = Mathf.Max(1, completedRound + 1);
            if (nextRound <= firstRound)
            {
                return firstRound;
            }

            int elapsed = nextRound - firstRound;
            return firstRound + Mathf.CeilToInt((float)elapsed / interval) * interval;
        }

        public bool IsScheduledMiniShopRound(int round)
        {
            int firstRound = Mathf.Max(1, earlyMiniShopRound);
            int interval = Mathf.Max(1, miniShopInterval);
            return enableEarlyMiniShop && round >= firstRound && (round - firstRound) % interval == 0;
        }

        private bool ShouldOpenEarlyMiniShop(int round)
        {
            int firstRound = Mathf.Max(1, earlyMiniShopRound);
            bool scheduledRound = IsScheduledMiniShopRound(round);
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
            recentOfferHistory.Clear();
        }

        private void BuildOffers(int round, bool miniShop, bool recoveryShop, bool insuranceShop, bool consumeForecastPreference = false)
        {
            currentOffers.Clear();
            if (insuranceShop)
            {
                currentOffers.Add(CreateOffer(ResolveInsuranceOfferType(round), round));
                return;
            }

            int offerCount = recoveryShop ? Mathf.Clamp(earlyRecoveryShopOfferCount, 1, 3) : miniShop ? Mathf.Clamp(earlyMiniShopOfferCount, 1, 3) : 3;
            List<OfferType> pool = recoveryShop
                ? BuildRecoveryShopPool()
                : miniShop ? BuildMiniShopPool(round) : BuildRegularShopPool(round);

            RemoveUnavailableAugmentOffers(pool);
            RemoveRecentlyOfferedTypes(pool, offerCount);
            bool deterministicContent = gameController != null && (gameController.DailyFateCupEnabled || gameController.HasRunContentSeedOverride);
            if (!deterministicContent)
            {
                RemoveRecentRunShopTypes(pool, round, miniShop, recoveryShop, offerCount);
            }

            int preferredRole = gameController != null ? gameController.BossForecastPreferredShopRoleIndex : -1;
            bool applyForecastPreference = consumeForecastPreference && preferredRole >= 0;
            int contentSeed = gameController != null ? gameController.ActiveRunContentSeed : DailyFateCupRules.TodaySeed;
            OfferRole[] miniRoles = miniShop
                ? BuildMiniShopRoleSlots(round, offerCount, preferredRole, deterministicContent, contentSeed)
                : null;
            while (currentOffers.Count < offerCount && pool.Count > 0)
            {
                OfferRarity rarity = RollOfferRarity(round);
                bool usePreferredRole = miniShop || (applyForecastPreference && currentOffers.Count == 0);
                OfferRole preferredSlot = miniShop ? miniRoles[currentOffers.Count] : (OfferRole)preferredRole;
                int index = usePreferredRole
                    ? FindOfferIndexForRoleAndRarity(pool, preferredSlot, rarity)
                    : FindOfferIndexForRarity(pool, rarity);
                OfferType type = pool[index];
                pool.RemoveAt(index);
                AddOffer(type, round, miniShop, recoveryShop);
            }

            if (applyForecastPreference && currentOffers.Count > 0)
            {
                gameController?.ConsumeBossForecastPreferredShopRole();
            }

            RememberCurrentOffers();
            if (!deterministicContent)
            {
                SaveRecentRunShopTypes(round, miniShop, recoveryShop);
            }
        }

        private void RemoveRecentlyOfferedTypes(List<OfferType> pool, int minimumPoolSize)
        {
            if (pool == null || recentOfferHistory.Count == 0)
            {
                return;
            }

            int safeMinimum = Mathf.Max(1, minimumPoolSize);
            for (int i = pool.Count - 1; i >= 0 && pool.Count > safeMinimum; i--)
            {
                if (recentOfferHistory.Contains(pool[i]))
                {
                    pool.RemoveAt(i);
                }
            }
        }

        private static string GetRetryShopHistoryKey(int round, bool miniShop, bool recoveryShop)
        {
            string context = recoveryShop ? "recovery" : miniShop ? "mini" : "regular";
            return "DefenseGame.RetryShopHistory." + context + "." + round;
        }

        private static string GetLegacyLastRunMiniShopKey(int round)
        {
            return "DefenseGame.LastRunMiniShopOffers." + round;
        }

        private void RemoveRecentRunShopTypes(List<OfferType> pool, int round, bool miniShop, bool recoveryShop, int minimumPoolSize)
        {
            if (pool == null)
            {
                return;
            }

            string key = GetRetryShopHistoryKey(round, miniShop, recoveryShop);
            string saved = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(saved) && miniShop)
            {
                saved = PlayerPrefs.GetString(GetLegacyLastRunMiniShopKey(round), string.Empty);
            }

            if (string.IsNullOrEmpty(saved))
            {
                return;
            }

            HashSet<int> recentTypes = new HashSet<int>();
            string[] tokens = saved.Split(',');
            for (int i = 0; i < tokens.Length; i++)
            {
                int value;
                if (int.TryParse(tokens[i], out value))
                {
                    recentTypes.Add(value);
                }
            }

            int safeMinimum = Mathf.Max(1, minimumPoolSize);
            for (int i = pool.Count - 1; i >= 0 && pool.Count > safeMinimum; i--)
            {
                if (recentTypes.Contains((int)pool[i]))
                {
                    pool.RemoveAt(i);
                }
            }
        }

        private void SaveRecentRunShopTypes(int round, bool miniShop, bool recoveryShop)
        {
            string key = GetRetryShopHistoryKey(round, miniShop, recoveryShop);
            List<int> history = new List<int>();
            string saved = PlayerPrefs.GetString(key, string.Empty);
            string[] tokens = saved.Split(',');
            for (int i = 0; i < tokens.Length; i++)
            {
                int value;
                if (int.TryParse(tokens[i], out value) && !history.Contains(value))
                {
                    history.Add(value);
                }
            }

            for (int i = 0; i < currentOffers.Count; i++)
            {
                ShopOffer offer = currentOffers[i];
                if (offer == null)
                {
                    continue;
                }

                int value = (int)offer.type;
                history.Remove(value);
                history.Add(value);
            }

            int offersPerRun = Mathf.Max(1, currentOffers.Count);
            int historyLimit = offersPerRun * Mathf.Max(1, retryOfferHistoryRuns);
            while (history.Count > historyLimit)
            {
                history.RemoveAt(0);
            }

            List<string> serialized = new List<string>(history.Count);
            for (int i = 0; i < history.Count; i++)
            {
                serialized.Add(history[i].ToString());
            }

            PlayerPrefs.SetString(key, string.Join(",", serialized.ToArray()));
            PlayerPrefs.Save();
        }
        private void RememberCurrentOffers()
        {
            for (int i = 0; i < currentOffers.Count; i++)
            {
                ShopOffer offer = currentOffers[i];
                if (offer == null)
                {
                    continue;
                }

                recentOfferHistory.Remove(offer.type);
                recentOfferHistory.Add(offer.type);
            }

            int historyLimit = Mathf.Max(3, recentOfferHistorySize);
            while (recentOfferHistory.Count > historyLimit)
            {
                recentOfferHistory.RemoveAt(0);
            }
        }

        private static List<OfferType> BuildRecoveryShopPool()
        {
            // Emergency support still presents a real strategic choice instead of a fixed rare-unit slot.
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

        private static OfferRole[] BuildMiniShopRoleSlots(int round, int offerCount, int preferredRole, bool deterministicContent, int contentSeed)
        {
            int progressionPhase = Mathf.Max(0, (round - 3) / 8) % 4;
            int pack;
            if (deterministicContent)
            {
                pack = (contentSeed ^ (round * 397)) & 3;
            }
            else
            {
                string key = "DefenseGame.LastRunMiniShopRolePack." + round;
                int previousPack = PlayerPrefs.GetInt(key, -1);
                pack = UnityEngine.Random.Range(0, 4);
                if (pack == previousPack)
                {
                    pack = (pack + UnityEngine.Random.Range(1, 4)) % 4;
                }

                PlayerPrefs.SetInt(key, pack);
                PlayerPrefs.Save();
            }

            int phase = (progressionPhase + pack) % 4;
            OfferRole[] template = phase == 0
                ? new[] { OfferRole.Supply, OfferRole.Build, OfferRole.Tactical }
                : phase == 1
                    ? new[] { OfferRole.Build, OfferRole.Wild, OfferRole.Tactical }
                    : phase == 2
                        ? new[] { OfferRole.Supply, OfferRole.Wild, OfferRole.Build }
                        : new[] { OfferRole.Tactical, OfferRole.Supply, OfferRole.Wild };
            OfferRole[] slots = new OfferRole[Mathf.Max(1, offerCount)];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = template[i % template.Length];
            }

            if (slots.Length > 0)
            {
                if (preferredRole == 0) slots[0] = OfferRole.Supply;
                else if (preferredRole == 1) slots[0] = OfferRole.Build;
                else if (preferredRole == 2) slots[0] = OfferRole.Tactical;
            }

            return slots;
        }

        private static OfferRarity RollOfferRarity(int round)
        {
            float roll = UnityEngine.Random.value;
            float legendaryChance = round <= 5 ? 0.02f : round <= 10 ? 0.06f : round <= 15 ? 0.10f : 0.13f;
            float rareChance = round <= 5 ? 0.23f : round <= 10 ? 0.26f : round <= 15 ? 0.30f : 0.32f;
            return roll < legendaryChance ? OfferRarity.Legendary : roll < legendaryChance + rareChance ? OfferRarity.Rare : OfferRarity.Normal;
        }

        private static OfferRarity GetOfferRarity(OfferType type)
        {
            switch (type)
            {
                case OfferType.RareUnit:
                case OfferType.MergeAssist:
                case OfferType.BossIntel:
                case OfferType.AugmentPower:
                case OfferType.AugmentGuard:
                case OfferType.AugmentSkill:
                case OfferType.ManaBattery:
                case OfferType.TwinRecruit:
                case OfferType.FusionWorkshop:
                case OfferType.CriticalForge:
                case OfferType.ArcaneConductor:
                case OfferType.RecoveryRareUnit:
                case OfferType.RecoveryBossPrep:
                    return OfferRarity.Rare;
                case OfferType.EpicDraft:
                case OfferType.BossRaidWager:
                case OfferType.FateMergeContract:
                case OfferType.FateBossContract:
                case OfferType.FateShopReroll:
                case OfferType.FateGradeLock:
                case OfferType.FateNormalBan:
                case OfferType.FateForceShop:
                    return OfferRarity.Legendary;
                default:
                    return OfferRarity.Normal;
            }
        }

        private static int FindOfferIndexForRarity(List<OfferType> pool, OfferRarity rarity)
        {
            List<int> candidates = new List<int>();
            for (int i = 0; i < pool.Count; i++)
            {
                if (GetOfferRarity(pool[i]) == rarity)
                {
                    candidates.Add(i);
                }
            }

            return candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : UnityEngine.Random.Range(0, pool.Count);
        }

        private static int FindOfferIndexForRoleAndRarity(List<OfferType> pool, OfferRole role, OfferRarity rarity)
        {
            List<int> candidates = new List<int>();
            for (int i = 0; i < pool.Count; i++)
            {
                if (GetOfferRole(pool[i]) == role && GetOfferRarity(pool[i]) == rarity)
                {
                    candidates.Add(i);
                }
            }

            return candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : FindOfferIndexForRole(pool, role);
        }

        private static string GetOfferRarityLabel(OfferRarity rarity)
        {
            return rarity == OfferRarity.Legendary ? "\uc804\uc124" : rarity == OfferRarity.Rare ? "\ub808\uc5b4" : "\uc77c\ubc18";
        }

        private static Color GetOfferRarityColor(OfferRarity rarity)
        {
            return rarity == OfferRarity.Legendary ? new Color(1f, 0.72f, 0.20f, 1f) :
                rarity == OfferRarity.Rare ? new Color(0.24f, 0.62f, 1f, 1f) : new Color(0.68f, 0.70f, 0.78f, 1f);
        }
        private static int FindOfferIndexForRole(List<OfferType> pool, OfferRole role)
        {
            List<int> candidates = new List<int>();
            for (int i = 0; i < pool.Count; i++)
            {
                if (GetOfferRole(pool[i]) == role)
                {
                    candidates.Add(i);
                }
            }

            return candidates.Count > 0
                ? candidates[UnityEngine.Random.Range(0, candidates.Count)]
                : UnityEngine.Random.Range(0, pool.Count);
        }

        private static OfferRole GetOfferRole(OfferType type)
        {
            switch (type)
            {
                case OfferType.RandomUnit:
                case OfferType.RareUnit:
                case OfferType.TwinRecruit:
                case OfferType.EpicDraft:
                    return OfferRole.Supply;
                case OfferType.MergeAssist:
                case OfferType.TileReroll:
                case OfferType.Coupon:
                case OfferType.SalvageCrate:
                case OfferType.FusionWorkshop:
                    return OfferRole.Build;
                case OfferType.RiskChest:
                case OfferType.BossRaidWager:
                case OfferType.FateMergeContract:
                case OfferType.FateBossContract:
                case OfferType.FateShopReroll:
                case OfferType.FateGradeLock:
                case OfferType.FateNormalBan:
                case OfferType.FateForceShop:
                    return OfferRole.Wild;
                default:
                    return OfferRole.Tactical;
            }
        }

        private List<OfferType> BuildMiniShopPool(int round)
        {
            // Every mini-shop starts with fourteen actual effects, then gains fate contracts later.
            List<OfferType> pool = new List<OfferType>
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
                OfferType.AugmentSkill,
                OfferType.VanguardDrill,
                OfferType.TargetingLens,
                OfferType.ManaBattery,
                OfferType.SalvageCrate
            };

            AddProgressionShopOffers(pool, round);
            if (NeedsFieldMedic())
            {
                pool.Add(OfferType.FieldMedic);
            }

            if (round >= 8)
            {
                pool.Add(OfferType.FateMergeContract);
                pool.Add(OfferType.FateBossContract);
            }

            if (round >= 12)
            {
                pool.Add(OfferType.FateShopReroll);
                pool.Add(OfferType.FateGradeLock);
            }

            if (round >= 18)
            {
                pool.Add(OfferType.FateNormalBan);
                pool.Add(OfferType.FateForceShop);
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
                OfferType.AugmentPower,
                OfferType.AugmentGuard,
                OfferType.AugmentSkill,
                OfferType.VanguardDrill,
                OfferType.TargetingLens,
                OfferType.ManaBattery,
                OfferType.SalvageCrate
            };

            AddProgressionShopOffers(pool, round);
            if (NeedsFieldMedic())
            {
                pool.Add(OfferType.FieldMedic);
            }

            if (round >= 18)
            {
                pool.Add(OfferType.FateMergeContract);
                pool.Add(OfferType.FateBossContract);
            }

            return pool;
        }


        private static void AddProgressionShopOffers(List<OfferType> pool, int round)
        {
            if (pool == null)
            {
                return;
            }

            if (round >= 5) pool.Add(OfferType.TwinRecruit);
            if (round >= 6) pool.Add(OfferType.FusionWorkshop);
            if (round >= 7) pool.Add(OfferType.CriticalForge);
            if (round >= 9) pool.Add(OfferType.ArcaneConductor);
            if (round >= 11) pool.Add(OfferType.EpicDraft);
            if (round >= 11) pool.Add(OfferType.BossRaidWager);
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
                case OfferType.SalvageCrate:
                    return Mathf.Max(1, target - 2);
                case OfferType.TargetingLens:
                case OfferType.ManaBattery:
                    return Mathf.Max(1, target - 1);
                case OfferType.TwinRecruit:
                    return target + 5;
                case OfferType.FusionWorkshop:
                    return target + 3;
                case OfferType.CriticalForge:
                case OfferType.ArcaneConductor:
                    return target + 5;
                case OfferType.EpicDraft:
                    return target + 14;
                case OfferType.BossRaidWager:
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

            offer.rarity = GetOfferRarity(type);
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
                case OfferType.VanguardDrill:
                case OfferType.TargetingLens:
                case OfferType.ManaBattery:
                    return 8.00f;
                case OfferType.SalvageCrate:
                    return 3.00f;
                case OfferType.TwinRecruit:
                    return 4.50f;
                case OfferType.FusionWorkshop:
                    return 4.00f;
                case OfferType.CriticalForge:
                case OfferType.ArcaneConductor:
                    return 7.00f;
                case OfferType.EpicDraft:
                    return 10.00f;
                case OfferType.BossRaidWager:
                    return 5.00f;
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
                case OfferType.VanguardDrill:
                    return new ShopOffer
                    {
                        type = type,
                        title = "\uc804\uc5f4 \ub3cc\ud30c \ud6c8\ub828",
                        description = "\ud604\uc7ac \uc0dd\uc874 \uc720\ub2db \uacf5\uaca9\ub825 +12%, \uacf5\uaca9 \uc18d\ub3c4 +6%. \uc989\uc2dc \uc804\ub825\uc744 \ub04c\uc5b4\uc62c\ub9bd\ub2c8\ub2e4.",
                        cost = 22 + scale * 5,
                        color = new Color(1f, 0.52f, 0.28f)
                    };
                case OfferType.TargetingLens:
                    return new ShopOffer
                    {
                        type = type,
                        title = "\uc870\uc900 \ub80c\uc988",
                        description = "\ud604\uc7ac \uc0dd\uc874 \uc720\ub2db \uc0ac\uac70\ub9ac +0.75m, \uce58\uba85\ud0c0 \ud655\ub960 +8%. \ud6c4\ubc29 \ud3ec\ub300\ub97c \uc55e\ub2f9\uae41\ub2c8\ub2e4.",
                        cost = 20 + scale * 5,
                        color = new Color(0.36f, 0.82f, 1f)
                    };
                case OfferType.ManaBattery:
                    return new ShopOffer
                    {
                        type = type,
                        title = "\uacfc\ucda9\uc804 \ubc30\ud130\ub9ac",
                        description = "\ud604\uc7ac \uc0dd\uc874 \uc720\ub2db \ub9c8\ub098 \uc7ac\uc0dd +3.5%, \uc2a4\ud0ac \uc704\ub825 +12%. \uc2a4\ud0ac \ud68c\uc804\uc744 \ub192\uc785\ub2c8\ub2e4.",
                        cost = 20 + scale * 5,
                        color = new Color(0.88f, 0.48f, 1f)
                    };
                case OfferType.SalvageCrate:
                    return new ShopOffer
                    {
                        type = type,
                        title = "\uc7ac\ud65c\uc6a9 \uc0c1\uc790",
                        description = "\ud569\uc131 \uc7ac\ub8cc\ub97c \ubcf4\uae09\ud558\uace0 \uace8\ub4dc\ub97c \ud68c\uc218\ud569\ub2c8\ub2e4. \ud569\uc131\uc774 \uc5b4\ub824\uc6b4 \ud310\uc5d0\uc11c \uc720\uc6a9\ud569\ub2c8\ub2e4.",
                        cost = 18 + scale * 4,
                        color = new Color(0.50f, 1f, 0.68f)
                    };
                case OfferType.TwinRecruit:
                    return new ShopOffer
                    {
                        type = type,
                        title = "쌍둥이 소환 계약",
                        description = "현재 라운드 확률표로 랜덤 유닛 2마리를 연속 배치합니다. 빈 슬롯 2칸이 가장 효율적입니다.",
                        cost = 25 + scale * 5,
                        color = new Color(0.30f, 0.84f, 1f)
                    };
                case OfferType.FusionWorkshop:
                    return new ShopOffer
                    {
                        type = type,
                        title = "합성 공방 이용권",
                        description = "가장 가까운 합성선의 재료를 보급하고 다음 3라운드 소환 비용을 10% 낮춥니다.",
                        cost = 23 + scale * 5,
                        color = new Color(0.62f, 1f, 0.32f)
                    };
                case OfferType.CriticalForge:
                    return new ShopOffer
                    {
                        type = type,
                        title = "치명 개조 키트",
                        description = "현재 생존 유닛 공격력 +8%, 치명타 확률 +10%. 치명 빌드의 시동을 겁니다.",
                        cost = 25 + scale * 6,
                        color = new Color(1f, 0.66f, 0.24f)
                    };
                case OfferType.ArcaneConductor:
                    return new ShopOffer
                    {
                        type = type,
                        title = "마나 공명 도체",
                        description = "현재 생존 유닛 공격속도 +4%, 마나 재생 +2.5%, 스킬 위력 +16%.",
                        cost = 27 + scale * 6,
                        color = new Color(0.70f, 0.48f, 1f)
                    };
                case OfferType.EpicDraft:
                    return new ShopOffer
                    {
                        type = type,
                        title = "에픽 지명 소환",
                        description = "에픽 등급 유닛 1마리를 즉시 배치합니다. 중후반 조합 방향을 크게 바꿉니다.",
                        cost = 48 + scale * 9,
                        color = new Color(1f, 0.50f, 0.92f)
                    };
                case OfferType.BossRaidWager:
                    return new ShopOffer
                    {
                        type = type,
                        title = "보스 담보 계약",
                        description = "라이프 -1. 현재 생존 유닛 보스 피해 +35%, 이후 라운드 클리어 보상 +2G.",
                        cost = 24 + scale * 5,
                        priceLabel = "골드+HP",
                        color = new Color(1f, 0.34f, 0.28f)
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
                gameController.RecordFirstMiniShopPurchase();
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
                case OfferType.VanguardDrill:
                    return ApplyCombatPackage(0.12f, 0.06f, 0f, 0f, 0f, 0f);
                case OfferType.TargetingLens:
                    return ApplyCombatPackage(0f, 0f, 0.75f, 0.08f, 0f, 0f);
                case OfferType.ManaBattery:
                    return ApplyCombatPackage(0f, 0f, 0f, 0f, 0.035f, 0.12f);
                case OfferType.SalvageCrate:
                    bool salvageGranted = gameController.TryGrantMergeAssistUnit();
                    gameController.AddGold(salvageGranted ? 4 : 8);
                    return true;
                case OfferType.TwinRecruit:
                    bool firstRecruit = gameController.TryGrantRandomSummonableUnit();
                    bool secondRecruit = gameController.TryGrantRandomSummonableUnit();
                    return firstRecruit || secondRecruit;
                case OfferType.FusionWorkshop:
                    gameController.TryGrantMergeAssistUnit();
                    gameController.AddTemporaryShopSummonDiscount(0.10f, 3);
                    return true;
                case OfferType.CriticalForge:
                    return ApplyCombatPackage(0.08f, 0f, 0f, 0.10f, 0f, 0f);
                case OfferType.ArcaneConductor:
                    return ApplyCombatPackage(0f, 0.04f, 0f, 0f, 0.025f, 0.16f);
                case OfferType.EpicDraft:
                    return gameController.TryGrantRandomUnitByGrade(CharacterGrade.Epic);
                case OfferType.BossRaidWager:
                    DefenderUnit[] raidUnits = boardManager != null ? boardManager.GetAliveDefenders() : new DefenderUnit[0];
                    if (raidUnits.Length == 0 || !gameController.TrySpendLifeForContract(1, "보스 담보 계약"))
                    {
                        return false;
                    }

                    for (int i = 0; i < raidUnits.Length; i++)
                    {
                        if (raidUnits[i] != null)
                        {
                            raidUnits[i].AddBossDamageBonus(0.35f);
                        }
                    }

                    gameController.AddRoundGoldBonus(2);
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

        private bool ApplyCombatPackage(float attackPower, float attackSpeed, float range, float criticalChance, float manaRegen, float skillPower)
        {
            DefenderUnit[] defenders = boardManager != null ? boardManager.GetAliveDefenders() : new DefenderUnit[0];
            bool applied = false;
            for (int i = 0; i < defenders.Length; i++)
            {
                DefenderUnit defender = defenders[i];
                if (defender == null)
                {
                    continue;
                }

                if (attackPower > 0f) defender.AddAttackPowerBonus(attackPower);
                if (attackSpeed > 0f) defender.AddPermanentAttackSpeedBonus(attackSpeed);
                if (range > 0f) defender.AddAttackRangeBonus(range);
                if (criticalChance > 0f) defender.AddPermanentCriticalChanceBonus(criticalChance);
                if (manaRegen > 0f) defender.AddManaRegenRateBonus(manaRegen);
                if (skillPower > 0f) defender.AddSkillPowerBonus(skillPower);
                applied = true;
            }

            return applied;
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
                float y = -158f - i * 176f;
                float x = 0f;
                Vector2 size = new Vector2(790f, 164f);
                if (currentShopIsRecovery)
                {
                    x = 0f;
                    size = new Vector2(790f, 164f);
                }
                else if (currentShopIsMini)
                {
                    x = 0f;
                    size = new Vector2(790f, 164f);
                }
                else if (currentShopIsInsurance)
                {
                    y = -234f;
                    size = new Vector2(790f, 192f);
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
                SetText(GetText(offerTitleTexts, i), FormatOfferTitle(offer));
                SetText(GetText(offerDescriptionTexts, i), offer.description);
                SetText(GetText(offerPriceTexts, i), BuildOfferPriceLabel(offer));
                Outline outline = offerButtons[i].GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = GetOfferRarityColor(offer.rarity);
                }

                Image accent = GetImage(offerAccentImages, i);
                if (accent != null)
                {
                    accent.color = offer.color;
                }

                Image icon = GetChildImage(offerButtons[i].transform, "RunShopOfferIcon");
                if (icon != null)
                {
                    icon.color = Color.Lerp(Color.white, offer.color, 0.28f);
                }
            }
        }

        private static string FormatOfferTitle(ShopOffer offer)
        {
            if (offer == null)
            {
                return string.Empty;
            }

            string role = GetOfferRole(offer.type) == OfferRole.Supply ? "\ubcf4\uae09" :
                GetOfferRole(offer.type) == OfferRole.Build ? "\ube4c\ub4dc" :
                GetOfferRole(offer.type) == OfferRole.Tactical ? "\uc804\uc220" : "\ubcc0\uc218";
            return GetOfferRarityLabel(offer.rarity) + "  |  " + role + "  |  " + offer.title;
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
