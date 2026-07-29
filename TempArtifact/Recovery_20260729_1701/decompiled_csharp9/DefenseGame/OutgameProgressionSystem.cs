using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenseGame
{
	public class OutgameProgressionSystem : MonoBehaviour
	{
		private const string ServiceSaveKey = "DefenseGame.OutgameProgression.Service.v1";

		private const string TestSaveKey = "DefenseGame.OutgameProgression.Test.v1";

		private const string PlayModeKey = "DefenseGame.OutgameProgression.PlayMode.v1";

		private const int BossScoreMissionFlag = 1;

		private const int BossKillMissionFlag = 2;

		private const int RunScoreMissionFlag = 4;

		private const int BossScoreMissionTarget = 1200;

		private const int BossKillMissionTarget = 3;

		private const int RunScoreMissionTarget = 135;

		[SerializeField]
		private OutgameProgressionConfig config;

		[SerializeField]
		private CharacterDatabase characterDatabase;

		private OutgameProgressionConfig runtimeConfig;

		private OutgameSaveData saveData;

		private OutgamePlayMode currentPlayMode;

		private string lastSeasonRewardSummary = string.Empty;

		public static OutgameProgressionSystem Active { get; private set; }

		public OutgameProgressionConfig Settings
		{
			get
			{
				if (config != null)
				{
					return config;
				}
				if (runtimeConfig == null)
				{
					runtimeConfig = ScriptableObject.CreateInstance<OutgameProgressionConfig>();
				}
				return runtimeConfig;
			}
		}

		public int Gold => EnsureSaveData().gold;

		public int Diamonds => EnsureSaveData().diamonds;

		public int CurrentSeasonId => EnsureSaveData().seasonId;

		public int WeeklyBossScore => EnsureSaveData().weeklyBossScore;

		public int WeeklyBestRunScore => EnsureSaveData().weeklyBestRunScore;

		public int WeeklyBossKills => EnsureSaveData().weeklyBossKills;

		public int EarnedChestKeys => EnsureSaveData().earnedChestKeys;

		public int EarnedChestProgress => EnsureSaveData().earnedChestProgress;

		public int EarnedChestProgressTarget => Mathf.Max(1, Settings.earnedChestProgressTarget);

		public int HighestRoundReached => EnsureSaveData().highestRoundReached;

		public string WishlistCharacterId => EnsureSaveData().wishlistCharacterId;

		public OutgamePlayMode CurrentPlayMode => currentPlayMode;

		public bool IsTestMode => currentPlayMode == OutgamePlayMode.Test;

		public string LastSeasonRewardSummary => lastSeasonRewardSummary;

		public event Action OnProgressChanged;

		private void Awake()
		{
			Active = this;
		}

		private void OnDestroy()
		{
			if (Active == this)
			{
				Active = null;
			}
		}

		public void Configure(OutgameProgressionConfig progressionConfig, CharacterDatabase database)
		{
			config = progressionConfig;
			characterDatabase = database;
			currentPlayMode = (OutgamePlayMode)PlayerPrefs.GetInt("DefenseGame.OutgameProgression.PlayMode.v1", (int)Settings.defaultPlayMode);
			Load();
			EnsureInitialRoster();
			this.OnProgressChanged?.Invoke();
		}

		public void SwitchPlayMode(OutgamePlayMode playMode)
		{
			if (currentPlayMode != playMode)
			{
				Save();
				currentPlayMode = playMode;
				PlayerPrefs.SetInt("DefenseGame.OutgameProgression.PlayMode.v1", (int)currentPlayMode);
				PlayerPrefs.Save();
				Load();
				EnsureInitialRoster();
				this.OnProgressChanged?.Invoke();
			}
		}

		public void RechargeTestDiamonds()
		{
			if (IsTestMode)
			{
				AddDiamonds(Mathf.Max(1, Settings.testDiamondRechargeAmount));
			}
		}

		public void AddGold(int amount)
		{
			if (amount > 0)
			{
				EnsureSaveData().gold += amount;
				Save();
				this.OnProgressChanged?.Invoke();
			}
		}

		public void RechargeTestCurrency()
		{
			if (IsTestMode)
			{
				OutgameSaveData data = EnsureSaveData();
				data.gold += Mathf.Max(1, Settings.testGoldRechargeAmount);
				data.diamonds += Mathf.Max(1, Settings.testDiamondRechargeAmount);
				Save();
				this.OnProgressChanged?.Invoke();
			}
		}

		public void AddDiamonds(int amount)
		{
			if (amount > 0)
			{
				EnsureSaveData().diamonds += amount;
				Save();
				this.OnProgressChanged?.Invoke();
			}
		}

		public bool GrantTestShopCurrency(int gold, int diamonds)
		{
			if (!IsTestMode)
			{
				return false;
			}
			OutgameSaveData data = EnsureSaveData();
			data.gold += Mathf.Max(0, gold);
			data.diamonds += Mathf.Max(0, diamonds);
			Save();
			this.OnProgressChanged?.Invoke();
			return true;
		}

		public int ResolveBattleDiamondReward(int rewardPoints)
		{
			return Mathf.Max(0, rewardPoints * Mathf.Max(0, Settings.diamondsPerBattleRewardPoint));
		}

		public void RecordSeasonRun(int runScore, int bossScore, int bossKills, string mvpName, int round, bool victory)
		{
			OutgameSaveData data = EnsureSaveData();
			EnsureCurrentSeason(data);
			int safeRunScore = Mathf.Max(0, runScore);
			int safeBossScore = Mathf.Max(0, bossScore);
			int safeBossKills = Mathf.Max(0, bossKills);
			data.weeklyBestRunScore = Mathf.Max(data.weeklyBestRunScore, safeRunScore);
			data.weeklyBossScore = Mathf.Max(data.weeklyBossScore, safeBossScore);
			data.weeklyBossKills = Mathf.Max(data.weeklyBossKills, safeBossKills);
			int coopScore = Mathf.Max(0, Mathf.RoundToInt((float)safeBossScore * 0.65f + (float)safeRunScore * 0.25f + (float)safeBossKills * 180f + (float)Mathf.Max(0, round) * 12f));
			data.coopBossScore = Mathf.Max(data.coopBossScore, coopScore);
			if (safeBossKills > 0 || victory)
			{
				int runMvpCount = ((safeBossKills <= 0) ? 1 : safeBossKills);
				data.coopMvpCount = Mathf.Max(data.coopMvpCount, runMvpCount);
				data.lastCoopMvpName = (string.IsNullOrWhiteSpace(mvpName) ? "MVP 대기" : mvpName);
			}
			data.lastDeckShareCode = BuildDeckShareCode(safeRunScore, safeBossScore, safeBossKills, mvpName, round, victory);
			data.lastReplayDigest = BuildReplayDigest(safeRunScore, safeBossScore, safeBossKills, mvpName, round, victory);
			int reward = GrantSeasonMissionRewards(data);
			lastSeasonRewardSummary = ((reward > 0) ? ("시즌 미션 보상 +" + reward + " DIA") : string.Empty);
			string chestRewardSummary = GrantCommercialBattleRewards(data, round, victory);
			if (!string.IsNullOrWhiteSpace(chestRewardSummary))
			{
				lastSeasonRewardSummary = (string.IsNullOrWhiteSpace(lastSeasonRewardSummary) ? chestRewardSummary : (lastSeasonRewardSummary + " | " + chestRewardSummary));
			}
			Save();
			this.OnProgressChanged?.Invoke();
		}

		public string BuildSeasonRankingSummary()
		{
			OutgameSaveData data = EnsureSaveData();
			if (EnsureCurrentSeason(data))
			{
				Save();
			}
			string mvpName = (string.IsNullOrWhiteSpace(data.lastCoopMvpName) ? "MVP 대기" : data.lastCoopMvpName);
			string deckShare = (string.IsNullOrWhiteSpace(data.lastDeckShareCode) ? "대기" : data.lastDeckShareCode);
			string replayDigest = (string.IsNullOrWhiteSpace(data.lastReplayDigest) ? "최근 런 없음" : data.lastReplayDigest);
			int rivalScore = Mathf.Max(900, data.weeklyBossScore - 160);
			int localRank = ((data.weeklyBossScore >= rivalScore) ? 1 : 2);
			return "WEEK " + data.seasonId + " 프리시즌\n주간 목표  보스 점수 / 협동 MVP / 런 점수 A 갱신\n주간 보스 점수  " + data.weeklyBossScore.ToString("N0") + "  |  최고 런 " + data.weeklyBestRunScore.ToString("N0") + "\n비동기 친구 보스 랭킹  " + localRank + "위  |  나 " + data.weeklyBossScore.ToString("N0") + " / 라이벌 " + rivalScore.ToString("N0") + "\n협동 보스 준비  " + mvpName + "  |  MVP " + data.coopMvpCount + "회  |  협동 점수 " + data.coopBossScore.ToString("N0") + "\n덱 공유  " + deckShare + "  |  리플레이  " + replayDigest + "\n" + BuildSeasonMissionLine(data, 1, "보스 점수 " + 1200.ToString("N0"), data.weeklyBossScore, 1200, 180) + "\n" + BuildSeasonMissionLine(data, 2, "보스 처치 " + 3 + "회", data.weeklyBossKills, 3, 220) + "\n" + BuildSeasonMissionLine(data, 4, "런 점수 A 달성", data.weeklyBestRunScore, 135, 260);
		}

		public string BuildSeasonResultLoopSummary()
		{
			return BuildChestEconomySummary() + "\n" + BuildSeasonLegacyResultLoopSummary();
		}

		private string BuildSeasonLegacyResultLoopSummary()
		{
			OutgameSaveData data = EnsureSaveData();
			if (EnsureCurrentSeason(data))
			{
				Save();
			}
			string deckShare = (string.IsNullOrWhiteSpace(data.lastDeckShareCode) ? "대기" : data.lastDeckShareCode);
			string replayDigest = (string.IsNullOrWhiteSpace(data.lastReplayDigest) ? "최근 런 없음" : data.lastReplayDigest);
			string mvpName = (string.IsNullOrWhiteSpace(data.lastCoopMvpName) ? "MVP 대기" : data.lastCoopMvpName);
			return "협동 " + data.coopBossScore.ToString("N0") + " / " + ResolveNextSeasonGoal(data) + " | 덱 " + deckShare + " | 리플레이 " + replayDigest + " | MVP " + mvpName;
		}

		private static string ResolveNextSeasonGoal(OutgameSaveData data)
		{
			if (data == null)
			{
				return "시즌 목표 대기";
			}
			if (data.weeklyBossScore < 1200)
			{
				return "보스 점수 " + data.weeklyBossScore.ToString("N0") + "/" + 1200.ToString("N0");
			}
			if (data.weeklyBossKills < 3)
			{
				return "보스 처치 " + data.weeklyBossKills + "/" + 3;
			}
			if (data.weeklyBestRunScore < 135)
			{
				return "런 점수 A " + data.weeklyBestRunScore + "/" + 135;
			}
			return "친구 보스 점수 갱신";
		}

		private static string BuildDeckShareCode(int runScore, int bossScore, int bossKills, string mvpName, int round, bool victory)
		{
			int hash = 23;
			hash = hash * 31 + runScore;
			hash = hash * 31 + bossScore;
			hash = hash * 31 + bossKills;
			hash = hash * 31 + round;
			hash = hash * 31 + (victory ? 1 : 0);
			string safeMvp = (string.IsNullOrWhiteSpace(mvpName) ? "MVP" : mvpName.Trim());
			for (int i = 0; i < safeMvp.Length; i++)
			{
				hash = hash * 31 + safeMvp[i];
			}
			return "DG-" + Mathf.Abs(hash % 100000).ToString("D5");
		}

		private static string BuildReplayDigest(int runScore, int bossScore, int bossKills, string mvpName, int round, bool victory)
		{
			string safeMvp = (string.IsNullOrWhiteSpace(mvpName) ? "MVP 대기" : mvpName.Trim());
			string result = (victory ? "승" : "패");
			return result + " R" + Mathf.Max(0, round) + " / " + safeMvp + " / 보스 " + bossKills + " / " + runScore.ToString("N0");
		}

		private int GrantSeasonMissionRewards(OutgameSaveData data)
		{
			int reward = 0;
			reward += TryGrantSeasonMissionReward(data, 1, data.weeklyBossScore >= 1200, 180);
			reward += TryGrantSeasonMissionReward(data, 2, data.weeklyBossKills >= 3, 220);
			reward += TryGrantSeasonMissionReward(data, 4, data.weeklyBestRunScore >= 135, 260);
			if (reward > 0)
			{
				data.diamonds += reward;
			}
			return reward;
		}

		private static int TryGrantSeasonMissionReward(OutgameSaveData data, int flag, bool achieved, int reward)
		{
			reward = ResolveCommercialSeasonReward(flag);
			if (data == null || !achieved || (data.seasonMissionClaimFlags & flag) != 0)
			{
				return 0;
			}
			data.seasonMissionClaimFlags |= flag;
			return Mathf.Max(0, reward);
		}

		private static string BuildSeasonMissionLine(OutgameSaveData data, int flag, string title, int value, int target, int reward)
		{
			reward = ResolveCommercialSeasonReward(flag);
			string state = ((data != null && (data.seasonMissionClaimFlags & flag) != 0) ? "수령 완료" : ((value >= target) ? "보상 대기" : "진행 중"));
			return title + "  " + Mathf.Min(value, target).ToString("N0") + "/" + target.ToString("N0") + "  |  " + state + " +" + reward + " DIA";
		}

		private static int ResolveCommercialSeasonReward(int flag)
		{
			return flag switch
			{
				1 => 60, 
				2 => 80, 
				4 => 120, 
				_ => 0, 
			};
		}

		private static bool EnsureCurrentSeason(OutgameSaveData data)
		{
			if (data == null)
			{
				return false;
			}
			int currentSeason = ResolveCurrentSeasonId();
			if (data.seasonId == currentSeason)
			{
				return false;
			}
			data.seasonId = currentSeason;
			data.weeklyBossScore = 0;
			data.weeklyBestRunScore = 0;
			data.weeklyBossKills = 0;
			data.seasonMissionClaimFlags = 0;
			data.coopBossScore = 0;
			data.coopMvpCount = 0;
			data.lastCoopMvpName = string.Empty;
			return true;
		}

		private static int ResolveCurrentSeasonId()
		{
			DateTime utcNow = DateTime.UtcNow;
			int week = Mathf.Clamp((utcNow.DayOfYear - 1) / 7 + 1, 1, 53);
			return utcNow.Year * 100 + week;
		}

		public bool TryOpenChest(int drawCount, out List<OutgameDrawResult> results)
		{
			return TryOpenPremiumChest(drawCount, out results);
		}

		public int ResolvePremiumChestCost(int drawCount)
		{
			return drawCount switch
			{
				5 => Mathf.Max(1, Settings.fiveChestCost), 
				10 => Mathf.Max(1, Settings.tenChestCost), 
				20 => Mathf.Max(1, Settings.twentyChestCost), 
				50 => Mathf.Max(1, Settings.fiftyChestCost), 
				100 => Mathf.Max(1, Settings.hundredChestCost), 
				_ => Mathf.Max(1, Settings.singleChestCost) * Mathf.Max(1, drawCount), 
			};
		}

		public bool IsDailyShopOfferPurchased(int offerIndex)
		{
			OutgameSaveData data = EnsureSaveData();
			EnsureDailyShopState(data);
			int flag = 1 << Mathf.Clamp(offerIndex, 0, 30);
			return (data.dailyShopPurchaseFlags & flag) != 0;
		}

		public bool TryPurchaseDailyShopOffer(int offerIndex, out List<OutgameDrawResult> results, out string message)
		{
			results = new List<OutgameDrawResult>();
			message = string.Empty;
			OutgameSaveData data = EnsureSaveData();
			EnsureDailyShopState(data);
			int safeIndex = Mathf.Clamp(offerIndex, 0, 2);
			int flag = 1 << safeIndex;
			if ((data.dailyShopPurchaseFlags & flag) != 0)
			{
				message = "오늘 이미 구매한 상품입니다.";
				return false;
			}
			switch (safeIndex)
			{
			case 0:
			{
				int reward = Mathf.Max(1, Settings.dailyFreeGold);
				data.gold += reward;
				message = "일일 무료 선물 +" + reward.ToString("N0") + " GOLD";
				break;
			}
			case 1:
			{
				int cost2 = Mathf.Max(1, Settings.dailyCardPackGoldCost);
				if (data.gold < cost2)
				{
					message = "골드가 부족합니다.";
					return false;
				}
				data.gold -= cost2;
				DrawCardsInto(results, OutgameChestType.Earned, Mathf.Max(1, Settings.dailyCardPackDrawCount));
				message = "일일 영웅 카드 묶음을 구매했습니다.";
				break;
			}
			default:
			{
				int cost = Mathf.Max(1, Settings.dailyPremiumPackDiamondCost);
				if (data.diamonds < cost)
				{
					message = "다이아가 부족합니다.";
					return false;
				}
				data.diamonds -= cost;
				DrawCardsInto(results, OutgameChestType.Premium, Mathf.Max(1, Settings.dailyPremiumPackDrawCount));
				message = "일일 프리미엄 묶음을 구매했습니다.";
				break;
			}
			}
			data.dailyShopPurchaseFlags |= flag;
			Save();
			this.OnProgressChanged?.Invoke();
			return true;
		}

		public bool TryOpenPremiumChest(int drawCount, out List<OutgameDrawResult> results)
		{
			results = new List<OutgameDrawResult>();
			if (characterDatabase == null || drawCount <= 0)
			{
				return false;
			}
			int cost = ResolvePremiumChestCost(drawCount);
			OutgameSaveData data = EnsureSaveData();
			if (data.diamonds < cost)
			{
				return false;
			}
			data.diamonds -= cost;
			DrawCardsInto(results, OutgameChestType.Premium, drawCount);
			Save();
			this.OnProgressChanged?.Invoke();
			return results.Count > 0;
		}

		public bool TryOpenEarnedChest(out List<OutgameDrawResult> results)
		{
			results = new List<OutgameDrawResult>();
			OutgameSaveData data = EnsureSaveData();
			if (characterDatabase == null || data.earnedChestKeys <= 0)
			{
				return false;
			}
			data.earnedChestKeys--;
			OutgameDrawResult result = DrawCard(OutgameChestType.Earned);
			if (result != null)
			{
				results.Add(result);
			}
			Save();
			this.OnProgressChanged?.Invoke();
			return results.Count > 0;
		}

		public bool CycleWishlist()
		{
			if (characterDatabase == null)
			{
				return false;
			}
			List<CharacterDefinition> candidates = (from character in characterDatabase.Characters
				where character != null
				orderby character.grade, character.id
				select character).ToList();
			if (candidates.Count == 0)
			{
				return false;
			}
			OutgameSaveData data = EnsureSaveData();
			int currentIndex = candidates.FindIndex((CharacterDefinition character) => character.id == data.wishlistCharacterId);
			int nextIndex = (currentIndex + 1) % candidates.Count;
			data.wishlistCharacterId = candidates[nextIndex].id;
			data.premiumWishlistPity = 0;
			Save();
			this.OnProgressChanged?.Invoke();
			return true;
		}

		public bool SetWishlistCharacter(string characterId)
		{
			CharacterDefinition target = ((characterDatabase != null) ? characterDatabase.Characters.FirstOrDefault((CharacterDefinition character) => character != null && character.id == characterId) : null);
			if (target == null)
			{
				return false;
			}
			OutgameSaveData data = EnsureSaveData();
			data.wishlistCharacterId = target.id;
			data.premiumWishlistPity = 0;
			Save();
			this.OnProgressChanged?.Invoke();
			return true;
		}

		public void GrantYahtzeeChestProgress(int progress)
		{
			if (progress > 0)
			{
				AddEarnedChestProgress(EnsureSaveData(), progress);
				Save();
				this.OnProgressChanged?.Invoke();
			}
		}

		public string BuildChestEconomySummary()
		{
			OutgameSaveData data = EnsureSaveData();
			int nextHurdle = CommercialRoundPacing.GetNextHurdleRound(data.highestRoundReached);
			return "무료 상자 " + data.earnedChestKeys + "개  |  게이지 " + data.earnedChestProgress + "/" + Mathf.Max(1, Settings.earnedChestProgressTarget) + "  |  다음 성장 허들 R" + nextHurdle;
		}

		public string GetWishlistDisplayName()
		{
			CharacterDefinition wishlist = ResolveWishlistCharacter();
			return (wishlist != null) ? wishlist.displayName : "미설정";
		}

		public bool IsOwned(string characterId)
		{
			OutgameCardRecord record = FindRecord(characterId);
			return record != null && record.level > 0;
		}

		public bool CanDeployCharacter(CharacterDefinition character)
		{
			return character != null && (IsTestMode || IsOwned(character.id));
		}

		public int GetCardLevel(string characterId)
		{
			return FindRecord(characterId)?.level ?? 0;
		}

		public int GetDisplayCardLevel(string characterId)
		{
			int level = GetCardLevel(characterId);
			if (level > 0)
			{
				return level;
			}
			return IsTestMode ? 1 : 0;
		}

		public string BuildProgressText(string characterId)
		{
			OutgameCardRecord record = FindRecord(characterId);
			if (record == null || record.level <= 0)
			{
				return IsTestMode ? "Lv.1  |  테스트 기본 보유" : "미획득  |  첫 카드 획득 시 해금";
			}
			if (record.level >= Settings.maxCardLevel)
			{
				return "Lv." + record.level + "  |  최대 성장";
			}
			return "Lv." + record.level + "  |  카드 " + record.upgradeCopies + "/" + RequiredCopiesForNextLevel(record.level);
		}

		public int CountUpgradeableCards()
		{
			List<OutgameCardRecord> records = EnsureSaveData().cards;
			int count = 0;
			for (int i = 0; i < records.Count; i++)
			{
				OutgameCardRecord record = records[i];
				if (record != null && record.level > 0 && record.level < Settings.maxCardLevel && record.upgradeCopies >= RequiredCopiesForNextLevel(record.level))
				{
					count++;
				}
			}
			return count;
		}

		public string BuildCollectionSummary()
		{
			int total = ((characterDatabase != null) ? characterDatabase.Characters.Count : 0);
			int owned = 0;
			if (IsTestMode)
			{
				owned = total;
			}
			else
			{
				List<OutgameCardRecord> records = EnsureSaveData().cards;
				for (int i = 0; i < records.Count; i++)
				{
					if (records[i] != null && records[i].level > 0)
					{
						owned++;
					}
				}
			}
			string prefix = (IsTestMode ? "전체 보유 영웅 " : "보유 영웅 ");
			return prefix + owned + "/" + total + "  |  평균 성장 Lv." + GetAverageGrowthLevel().ToString("0.0");
		}

		public string BuildRateText()
		{
			return "무료: " + BuildLegacyRateText() + "  |  " + Mathf.Max(1, Settings.earnedChestRarePityDraws) + "회 내 레어+ / " + Mathf.Max(1, Settings.earnedChestEpicPityDraws) + "회 내 희귀+\n프리미엄: 일반 " + FormatPercent(Settings.premiumNormalRate) + "  레어 " + FormatPercent(Settings.premiumRareRate) + "  희귀 " + FormatPercent(Settings.premiumEpicRate) + "  |  10회 내 희귀+ / 40회 내 전설+ / 위시 보정";
		}

		private string BuildLegacyRateText()
		{
			return "일반 " + FormatPercent(Settings.normalRate) + "  레어 " + FormatPercent(Settings.rareRate) + "  희귀 " + FormatPercent(Settings.epicRate) + "  전설 " + FormatPercent(Settings.legendaryRate) + "  신화 " + FormatPercent(Settings.mythicRate) + "  초월 " + FormatPercent(Settings.transcendentRate);
		}

		public void ApplyGrowthToDefender(DefenderUnit unit, CharacterDefinition definition)
		{
			if (!(unit == null) && definition != null)
			{
				int growthLevel = Mathf.Max(0, GetCardLevel(definition.id) - 1);
				unit.ApplyOutgameGrowth(growthLevel, Settings.attackPowerPerGrowthLevel, Settings.maxHealthPerGrowthLevel);
			}
		}

		public void ResolveMonsterBalanceMultipliers(MonsterDefinition monster, out float healthMultiplier, out float attackMultiplier)
		{
			healthMultiplier = 1f;
			attackMultiplier = 1f;
			if (Settings.scaleMonstersWithCollectionGrowth && monster != null)
			{
				float averageGrowthLevel = GetAverageGrowthLevel();
				bool isBoss = monster.IsBossLike;
				float healthBonus = averageGrowthLevel * (isBoss ? Settings.bossHealthPerAverageGrowthLevel : Settings.regularHealthPerAverageGrowthLevel);
				float attackBonus = averageGrowthLevel * (isBoss ? Settings.bossAttackPerAverageGrowthLevel : Settings.regularAttackPerAverageGrowthLevel);
				healthMultiplier += Mathf.Min(healthBonus, Mathf.Min(Settings.maxMonsterHealthBonus, 0.15f));
				attackMultiplier += Mathf.Min(attackBonus, Mathf.Min(Settings.maxMonsterAttackBonus, 0.1f));
			}
		}

		private string GrantCommercialBattleRewards(OutgameSaveData data, int round, bool victory)
		{
			int safeRound = Mathf.Max(0, round);
			bool newHighestRound = victory && safeRound > data.highestRoundReached;
			if (newHighestRound)
			{
				data.highestRoundReached = safeRound;
			}
			int progress = (victory ? (8 + Mathf.Min(50, safeRound) / 4) : (6 + Mathf.Min(50, safeRound) / 5));
			if (newHighestRound)
			{
				progress += 3;
			}
			if (victory && safeRound > 0 && safeRound % 10 == 0)
			{
				progress += 25;
			}
			if (victory && CommercialRoundPacing.IsMajorHurdleRound(safeRound))
			{
				int hurdleIndex = Mathf.Clamp((safeRound - 20) / 10, 0, 30);
				int flag = 1 << hurdleIndex;
				if ((data.hurdleClearRewardFlags & flag) == 0)
				{
					data.hurdleClearRewardFlags |= flag;
					progress += 50;
				}
			}
			int supportKeys = 0;
			if (!victory && CommercialRoundPacing.TryGetApproachingHurdleIndex(safeRound, out var supportIndex))
			{
				supportIndex = Mathf.Clamp(supportIndex, 0, 30);
				int supportFlag = 1 << supportIndex;
				if ((data.hurdleFailureSupportFlags & supportFlag) == 0)
				{
					data.hurdleFailureSupportFlags |= supportFlag;
					supportKeys = Mathf.Max(0, Settings.hurdleFailureSupportChestKeys);
					data.earnedChestKeys += supportKeys;
				}
			}
			int progressKeys = AddEarnedChestProgress(data, progress);
			int totalKeys = progressKeys + supportKeys;
			int goldReward = (victory ? (60 + safeRound * 12) : (35 + safeRound * 8));
			data.gold += Mathf.Max(0, goldReward);
			string summary = "상점 골드 +" + goldReward.ToString("N0") + " / 무료 상자 게이지 +" + progress + " (" + data.earnedChestProgress + "/" + Mathf.Max(1, Settings.earnedChestProgressTarget) + ")";
			if (totalKeys > 0)
			{
				summary = summary + " / 상자 +" + totalKeys;
			}
			if (supportKeys > 0)
			{
				summary += " / 첫 허들 실패 지원";
			}
			return summary;
		}

		private int AddEarnedChestProgress(OutgameSaveData data, int progress)
		{
			if (data == null || progress <= 0)
			{
				return 0;
			}
			int target = Mathf.Max(1, Settings.earnedChestProgressTarget);
			data.earnedChestProgress = Mathf.Max(0, data.earnedChestProgress) + progress;
			int gainedKeys = data.earnedChestProgress / target;
			if (gainedKeys > 0)
			{
				data.earnedChestKeys += gainedKeys;
				data.earnedChestProgress %= target;
			}
			return gainedKeys;
		}

		private void DrawCardsInto(List<OutgameDrawResult> results, OutgameChestType chestType, int drawCount)
		{
			if (results == null)
			{
				return;
			}
			int safeCount = Mathf.Clamp(drawCount, 0, 100);
			for (int i = 0; i < safeCount; i++)
			{
				OutgameDrawResult result = DrawCard(chestType);
				if (result != null)
				{
					results.Add(result);
				}
			}
		}

		private void EnsureDailyShopState(OutgameSaveData data)
		{
			if (data != null)
			{
				DateTime now = DateTime.Now;
				int dateKey = now.Year * 10000 + now.Month * 100 + now.Day;
				if (data.dailyShopDate != dateKey)
				{
					data.dailyShopDate = dateKey;
					data.dailyShopPurchaseFlags = 0;
					Save();
				}
			}
		}

		public string BuildDailyShopResetLabel()
		{
			DateTime now = DateTime.Now;
			TimeSpan remaining = now.Date.AddDays(1.0) - now;
			return "일일 상품 갱신까지 " + Mathf.Max(0, remaining.Hours).ToString("00") + ":" + Mathf.Max(0, remaining.Minutes).ToString("00");
		}

		private OutgameDrawResult DrawCard(OutgameChestType chestType)
		{
			OutgameSaveData data = EnsureSaveData();
			CharacterGrade minimumGrade = ResolvePityMinimumGrade(data, chestType);
			bool pityTriggered = minimumGrade > CharacterGrade.Normal;
			bool wishlistHit;
			CharacterDefinition character = ResolveDrawCharacter(data, chestType, minimumGrade, out wishlistHit);
			if (character == null)
			{
				character = characterDatabase.GetRandomSummonableCharacter();
			}
			if (character == null)
			{
				return null;
			}
			UpdateChestPity(data, chestType, character.grade, wishlistHit);
			OutgameCardRecord record = GetOrCreateRecord(character.id);
			bool wasOwned = record.level > 0;
			int previousLevel = record.level;
			record.totalCopies++;
			record.upgradeCopies++;
			ApplyAvailableLevelUps(record);
			return new OutgameDrawResult
			{
				character = character,
				firstAcquisition = (!wasOwned && record.level > 0),
				leveledUp = (record.level > previousLevel && wasOwned),
				level = record.level,
				remainingCopies = record.upgradeCopies,
				requiredCopies = ((record.level < Settings.maxCardLevel) ? RequiredCopiesForNextLevel(record.level) : 0),
				chestType = chestType,
				wishlistHit = wishlistHit,
				pityTriggered = pityTriggered
			};
		}

		private CharacterDefinition ResolveDrawCharacter(OutgameSaveData data, OutgameChestType chestType, CharacterGrade minimumGrade, out bool wishlistHit)
		{
			wishlistHit = false;
			CharacterDefinition wishlist = ((chestType == OutgameChestType.Premium) ? ResolveWishlistCharacter() : null);
			if (wishlist != null && wishlist.grade >= minimumGrade)
			{
				int wishlistPityTarget = Mathf.Max(1, Settings.premiumWishlistPityDraws);
				bool guaranteedWishlist = data.premiumWishlistPity >= wishlistPityTarget - 1;
				bool randomWishlist = UnityEngine.Random.value < Mathf.Clamp01(Settings.premiumWishlistChance);
				if (guaranteedWishlist || randomWishlist)
				{
					wishlistHit = true;
					return wishlist;
				}
			}
			CharacterGrade rolledGrade = ((chestType == OutgameChestType.Premium) ? RollPremiumGrade() : RollGrade());
			if (rolledGrade < minimumGrade)
			{
				rolledGrade = minimumGrade;
			}
			return characterDatabase.GetRandomCharacterByGradeOrLower(rolledGrade);
		}

		private CharacterGrade ResolvePityMinimumGrade(OutgameSaveData data, OutgameChestType chestType)
		{
			if (chestType == OutgameChestType.Earned)
			{
				if (data.earnedEpicPity >= Mathf.Max(1, Settings.earnedChestEpicPityDraws) - 1)
				{
					return CharacterGrade.Epic;
				}
				if (data.earnedRarePity >= Mathf.Max(1, Settings.earnedChestRarePityDraws) - 1)
				{
					return CharacterGrade.Rare;
				}
				return CharacterGrade.Normal;
			}
			if (data.premiumLegendaryPity >= Mathf.Max(1, Settings.premiumChestLegendaryPityDraws) - 1)
			{
				return CharacterGrade.Legendary;
			}
			if (data.premiumEpicPity >= Mathf.Max(1, Settings.premiumChestEpicPityDraws) - 1)
			{
				return CharacterGrade.Epic;
			}
			return CharacterGrade.Normal;
		}

		private void UpdateChestPity(OutgameSaveData data, OutgameChestType chestType, CharacterGrade grade, bool wishlistHit)
		{
			if (chestType == OutgameChestType.Earned)
			{
				data.earnedRarePity = ((grade < CharacterGrade.Rare) ? IncrementPity(data.earnedRarePity) : 0);
				data.earnedEpicPity = ((grade < CharacterGrade.Epic) ? IncrementPity(data.earnedEpicPity) : 0);
			}
			else
			{
				data.premiumEpicPity = ((grade < CharacterGrade.Epic) ? IncrementPity(data.premiumEpicPity) : 0);
				data.premiumLegendaryPity = ((grade < CharacterGrade.Legendary) ? IncrementPity(data.premiumLegendaryPity) : 0);
				data.premiumWishlistPity = ((!wishlistHit) ? IncrementPity(data.premiumWishlistPity) : 0);
			}
		}

		private CharacterDefinition ResolveWishlistCharacter()
		{
			string wishlistId = EnsureSaveData().wishlistCharacterId;
			return (characterDatabase != null && !string.IsNullOrWhiteSpace(wishlistId)) ? characterDatabase.Characters.FirstOrDefault((CharacterDefinition character) => character != null && character.id == wishlistId) : null;
		}

		private static int IncrementPity(int value)
		{
			return (value >= 1000000) ? 1000000 : (Mathf.Max(0, value) + 1);
		}

		private void EnsureInitialRoster()
		{
			OutgameSaveData data = EnsureSaveData();
			if (data.initialRosterGranted)
			{
				return;
			}
			if (!IsTestMode && characterDatabase != null)
			{
				int granted = 0;
				List<string> starterIds = Settings.serviceStarterCharacterIds;
				if (starterIds != null)
				{
					int i;
					for (i = 0; i < starterIds.Count; i++)
					{
						if (granted >= Settings.serviceStarterCharacterCount)
						{
							break;
						}
						CharacterDefinition starter = characterDatabase.Characters.FirstOrDefault((CharacterDefinition character) => character != null && character.id == starterIds[i]);
						if (starter != null)
						{
							GrantInitialCard(starter);
							granted++;
						}
					}
				}
				for (int i2 = 0; i2 < characterDatabase.Characters.Count; i2++)
				{
					if (granted >= Settings.serviceStarterCharacterCount)
					{
						break;
					}
					CharacterDefinition fallback = characterDatabase.Characters[i2];
					if (fallback != null && !IsOwned(fallback.id))
					{
						GrantInitialCard(fallback);
						granted++;
					}
				}
			}
			data.initialRosterGranted = true;
			Save();
		}

		private void GrantInitialCard(CharacterDefinition character)
		{
			OutgameCardRecord record = GetOrCreateRecord(character.id);
			if (record.level <= 0)
			{
				int copies = Mathf.Max(1, Settings.initialUnlockCopies);
				record.totalCopies += copies;
				record.upgradeCopies += copies;
				ApplyAvailableLevelUps(record);
			}
		}

		private CharacterGrade RollGrade()
		{
			float total = Settings.normalRate + Settings.rareRate + Settings.epicRate + Settings.legendaryRate + Settings.mythicRate + Settings.transcendentRate;
			float roll = UnityEngine.Random.value * Mathf.Max(0.001f, total);
			if ((roll -= Settings.normalRate) < 0f)
			{
				return CharacterGrade.Normal;
			}
			if ((roll -= Settings.rareRate) < 0f)
			{
				return CharacterGrade.Rare;
			}
			if ((roll -= Settings.epicRate) < 0f)
			{
				return CharacterGrade.Epic;
			}
			if ((roll -= Settings.legendaryRate) < 0f)
			{
				return CharacterGrade.Legendary;
			}
			if ((roll -= Settings.mythicRate) < 0f)
			{
				return CharacterGrade.Mythic;
			}
			return CharacterGrade.Transcendent;
		}

		private CharacterGrade RollPremiumGrade()
		{
			float total = Settings.premiumNormalRate + Settings.premiumRareRate + Settings.premiumEpicRate + Settings.premiumLegendaryRate + Settings.premiumMythicRate + Settings.premiumTranscendentRate;
			float roll = UnityEngine.Random.value * Mathf.Max(0.001f, total);
			if ((roll -= Settings.premiumNormalRate) < 0f)
			{
				return CharacterGrade.Normal;
			}
			if ((roll -= Settings.premiumRareRate) < 0f)
			{
				return CharacterGrade.Rare;
			}
			if ((roll -= Settings.premiumEpicRate) < 0f)
			{
				return CharacterGrade.Epic;
			}
			if ((roll -= Settings.premiumLegendaryRate) < 0f)
			{
				return CharacterGrade.Legendary;
			}
			if ((roll -= Settings.premiumMythicRate) < 0f)
			{
				return CharacterGrade.Mythic;
			}
			return CharacterGrade.Transcendent;
		}

		private void ApplyAvailableLevelUps(OutgameCardRecord record)
		{
			while (record.level < Settings.maxCardLevel)
			{
				int required = ((record.level == 0) ? Mathf.Max(1, Settings.initialUnlockCopies) : RequiredCopiesForNextLevel(record.level));
				if (record.upgradeCopies < required)
				{
					break;
				}
				record.upgradeCopies -= required;
				record.level++;
			}
		}

		private int RequiredCopiesForNextLevel(int currentLevel)
		{
			return Mathf.Max(1, Settings.duplicateCopiesForLevelTwo + Mathf.Max(0, currentLevel - 1) * Settings.additionalCopiesPerLevel);
		}

		private float GetAverageGrowthLevel()
		{
			List<OutgameCardRecord> records = EnsureSaveData().cards;
			float totalGrowth = 0f;
			int ownedCount = 0;
			for (int i = 0; i < records.Count; i++)
			{
				OutgameCardRecord record = records[i];
				if (record != null && record.level > 0)
				{
					totalGrowth += (float)Mathf.Max(0, record.level - 1);
					ownedCount++;
				}
			}
			return (ownedCount > 0) ? (totalGrowth / (float)ownedCount) : 0f;
		}

		private OutgameCardRecord FindRecord(string characterId)
		{
			List<OutgameCardRecord> records = EnsureSaveData().cards;
			for (int i = 0; i < records.Count; i++)
			{
				if (records[i] != null && records[i].characterId == characterId)
				{
					return records[i];
				}
			}
			return null;
		}

		private OutgameCardRecord GetOrCreateRecord(string characterId)
		{
			OutgameCardRecord record = FindRecord(characterId);
			if (record != null)
			{
				return record;
			}
			record = new OutgameCardRecord
			{
				characterId = characterId
			};
			EnsureSaveData().cards.Add(record);
			return record;
		}

		private OutgameSaveData EnsureSaveData()
		{
			if (saveData == null)
			{
				Load();
			}
			return saveData;
		}

		private void Load()
		{
			string json = PlayerPrefs.GetString(ResolveSaveKey(), string.Empty);
			int initialGold = (IsTestMode ? Settings.testStartingGold : Settings.startingGold);
			int initialDiamonds = (IsTestMode ? Settings.testStartingDiamonds : Settings.startingDiamonds);
			saveData = (string.IsNullOrEmpty(json) ? new OutgameSaveData
			{
				gold = initialGold,
				diamonds = initialDiamonds
			} : JsonUtility.FromJson<OutgameSaveData>(json));
			if (saveData == null)
			{
				saveData = new OutgameSaveData
				{
					gold = initialGold,
					diamonds = initialDiamonds
				};
			}
			if (saveData.cards == null)
			{
				saveData.cards = new List<OutgameCardRecord>();
			}
			int previousVersion = saveData.metaProgressionVersion;
			int targetVersion = Mathf.Max(3, Settings.progressionVersion);
			bool migrated = previousVersion < targetVersion;
			if (previousVersion < 2)
			{
				int migrationKeys = (string.IsNullOrEmpty(json) ? Mathf.Max(0, Settings.startingEarnedChestKeys) : Mathf.Max(0, Settings.migrationEarnedChestKeys));
				saveData.earnedChestKeys = Mathf.Max(0, saveData.earnedChestKeys) + migrationKeys;
			}
			if (previousVersion < 3)
			{
				saveData.gold = Mathf.Max(0, saveData.gold) + initialGold;
			}
			saveData.metaProgressionVersion = targetVersion;
			saveData.gold = Mathf.Max(0, saveData.gold);
			saveData.earnedChestKeys = Mathf.Max(0, saveData.earnedChestKeys);
			saveData.earnedChestProgress = Mathf.Max(0, saveData.earnedChestProgress);
			int progressTarget = Mathf.Max(1, Settings.earnedChestProgressTarget);
			if (saveData.earnedChestProgress >= progressTarget)
			{
				saveData.earnedChestKeys += saveData.earnedChestProgress / progressTarget;
				saveData.earnedChestProgress %= progressTarget;
				migrated = true;
			}
			EnsureDailyShopState(saveData);
			EnsureCurrentSeason(saveData);
			lastSeasonRewardSummary = string.Empty;
			if (migrated)
			{
				PlayerPrefs.SetString(ResolveSaveKey(), JsonUtility.ToJson(saveData));
				PlayerPrefs.Save();
			}
		}

		private void Save()
		{
			PlayerPrefs.SetString(ResolveSaveKey(), JsonUtility.ToJson(EnsureSaveData()));
			PlayerPrefs.Save();
		}

		private string ResolveSaveKey()
		{
			return IsTestMode ? "DefenseGame.OutgameProgression.Test.v1" : "DefenseGame.OutgameProgression.Service.v1";
		}

		private static string FormatPercent(float value)
		{
			return (value * 100f).ToString((value * 100f < 1f) ? "0.0" : "0.#") + "%";
		}
	}
}
