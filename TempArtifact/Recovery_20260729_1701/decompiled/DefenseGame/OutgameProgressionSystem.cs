using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenseGame;

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
			if ((Object)(object)config != (Object)null)
			{
				return config;
			}
			if ((Object)(object)runtimeConfig == (Object)null)
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
		if ((Object)(object)Active == (Object)(object)this)
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
			OutgameSaveData outgameSaveData = EnsureSaveData();
			outgameSaveData.gold += Mathf.Max(1, Settings.testGoldRechargeAmount);
			outgameSaveData.diamonds += Mathf.Max(1, Settings.testDiamondRechargeAmount);
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
		OutgameSaveData outgameSaveData = EnsureSaveData();
		outgameSaveData.gold += Mathf.Max(0, gold);
		outgameSaveData.diamonds += Mathf.Max(0, diamonds);
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
		OutgameSaveData outgameSaveData = EnsureSaveData();
		EnsureCurrentSeason(outgameSaveData);
		int num = Mathf.Max(0, runScore);
		int num2 = Mathf.Max(0, bossScore);
		int num3 = Mathf.Max(0, bossKills);
		outgameSaveData.weeklyBestRunScore = Mathf.Max(outgameSaveData.weeklyBestRunScore, num);
		outgameSaveData.weeklyBossScore = Mathf.Max(outgameSaveData.weeklyBossScore, num2);
		outgameSaveData.weeklyBossKills = Mathf.Max(outgameSaveData.weeklyBossKills, num3);
		int num4 = Mathf.Max(0, Mathf.RoundToInt((float)num2 * 0.65f + (float)num * 0.25f + (float)num3 * 180f + (float)Mathf.Max(0, round) * 12f));
		outgameSaveData.coopBossScore = Mathf.Max(outgameSaveData.coopBossScore, num4);
		if (num3 > 0 || victory)
		{
			int num5 = ((num3 <= 0) ? 1 : num3);
			outgameSaveData.coopMvpCount = Mathf.Max(outgameSaveData.coopMvpCount, num5);
			outgameSaveData.lastCoopMvpName = (string.IsNullOrWhiteSpace(mvpName) ? "MVP 대기" : mvpName);
		}
		outgameSaveData.lastDeckShareCode = BuildDeckShareCode(num, num2, num3, mvpName, round, victory);
		outgameSaveData.lastReplayDigest = BuildReplayDigest(num, num2, num3, mvpName, round, victory);
		int num6 = GrantSeasonMissionRewards(outgameSaveData);
		lastSeasonRewardSummary = ((num6 > 0) ? ("시즌 미션 보상 +" + num6 + " DIA") : string.Empty);
		string text = GrantCommercialBattleRewards(outgameSaveData, round, victory);
		if (!string.IsNullOrWhiteSpace(text))
		{
			lastSeasonRewardSummary = (string.IsNullOrWhiteSpace(lastSeasonRewardSummary) ? text : (lastSeasonRewardSummary + " | " + text));
		}
		Save();
		this.OnProgressChanged?.Invoke();
	}

	public string BuildSeasonRankingSummary()
	{
		OutgameSaveData outgameSaveData = EnsureSaveData();
		if (EnsureCurrentSeason(outgameSaveData))
		{
			Save();
		}
		string text = (string.IsNullOrWhiteSpace(outgameSaveData.lastCoopMvpName) ? "MVP 대기" : outgameSaveData.lastCoopMvpName);
		string text2 = (string.IsNullOrWhiteSpace(outgameSaveData.lastDeckShareCode) ? "대기" : outgameSaveData.lastDeckShareCode);
		string text3 = (string.IsNullOrWhiteSpace(outgameSaveData.lastReplayDigest) ? "최근 런 없음" : outgameSaveData.lastReplayDigest);
		int num = Mathf.Max(900, outgameSaveData.weeklyBossScore - 160);
		int num2 = ((outgameSaveData.weeklyBossScore >= num) ? 1 : 2);
		return "WEEK " + outgameSaveData.seasonId + " 프리시즌\n주간 목표  보스 점수 / 협동 MVP / 런 점수 A 갱신\n주간 보스 점수  " + outgameSaveData.weeklyBossScore.ToString("N0") + "  |  최고 런 " + outgameSaveData.weeklyBestRunScore.ToString("N0") + "\n비동기 친구 보스 랭킹  " + num2 + "위  |  나 " + outgameSaveData.weeklyBossScore.ToString("N0") + " / 라이벌 " + num.ToString("N0") + "\n협동 보스 준비  " + text + "  |  MVP " + outgameSaveData.coopMvpCount + "회  |  협동 점수 " + outgameSaveData.coopBossScore.ToString("N0") + "\n덱 공유  " + text2 + "  |  리플레이  " + text3 + "\n" + BuildSeasonMissionLine(outgameSaveData, 1, "보스 점수 " + 1200.ToString("N0"), outgameSaveData.weeklyBossScore, 1200, 180) + "\n" + BuildSeasonMissionLine(outgameSaveData, 2, "보스 처치 " + 3 + "회", outgameSaveData.weeklyBossKills, 3, 220) + "\n" + BuildSeasonMissionLine(outgameSaveData, 4, "런 점수 A 달성", outgameSaveData.weeklyBestRunScore, 135, 260);
	}

	public string BuildSeasonResultLoopSummary()
	{
		return BuildChestEconomySummary() + "\n" + BuildSeasonLegacyResultLoopSummary();
	}

	private string BuildSeasonLegacyResultLoopSummary()
	{
		OutgameSaveData outgameSaveData = EnsureSaveData();
		if (EnsureCurrentSeason(outgameSaveData))
		{
			Save();
		}
		string text = (string.IsNullOrWhiteSpace(outgameSaveData.lastDeckShareCode) ? "대기" : outgameSaveData.lastDeckShareCode);
		string text2 = (string.IsNullOrWhiteSpace(outgameSaveData.lastReplayDigest) ? "최근 런 없음" : outgameSaveData.lastReplayDigest);
		string text3 = (string.IsNullOrWhiteSpace(outgameSaveData.lastCoopMvpName) ? "MVP 대기" : outgameSaveData.lastCoopMvpName);
		return "협동 " + outgameSaveData.coopBossScore.ToString("N0") + " / " + ResolveNextSeasonGoal(outgameSaveData) + " | 덱 " + text + " | 리플레이 " + text2 + " | MVP " + text3;
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
		int num = 23;
		num = num * 31 + runScore;
		num = num * 31 + bossScore;
		num = num * 31 + bossKills;
		num = num * 31 + round;
		num = num * 31 + (victory ? 1 : 0);
		string text = (string.IsNullOrWhiteSpace(mvpName) ? "MVP" : mvpName.Trim());
		for (int i = 0; i < text.Length; i++)
		{
			num = num * 31 + text[i];
		}
		return "DG-" + Mathf.Abs(num % 100000).ToString("D5");
	}

	private static string BuildReplayDigest(int runScore, int bossScore, int bossKills, string mvpName, int round, bool victory)
	{
		string text = (string.IsNullOrWhiteSpace(mvpName) ? "MVP 대기" : mvpName.Trim());
		string text2 = (victory ? "승" : "패");
		return text2 + " R" + Mathf.Max(0, round) + " / " + text + " / 보스 " + bossKills + " / " + runScore.ToString("N0");
	}

	private int GrantSeasonMissionRewards(OutgameSaveData data)
	{
		int num = 0;
		num += TryGrantSeasonMissionReward(data, 1, data.weeklyBossScore >= 1200, 180);
		num += TryGrantSeasonMissionReward(data, 2, data.weeklyBossKills >= 3, 220);
		num += TryGrantSeasonMissionReward(data, 4, data.weeklyBestRunScore >= 135, 260);
		if (num > 0)
		{
			data.diamonds += num;
		}
		return num;
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
		string text = ((data != null && (data.seasonMissionClaimFlags & flag) != 0) ? "수령 완료" : ((value >= target) ? "보상 대기" : "진행 중"));
		return title + "  " + Mathf.Min(value, target).ToString("N0") + "/" + target.ToString("N0") + "  |  " + text + " +" + reward + " DIA";
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
		int num = ResolveCurrentSeasonId();
		if (data.seasonId == num)
		{
			return false;
		}
		data.seasonId = num;
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
		int num = Mathf.Clamp((utcNow.DayOfYear - 1) / 7 + 1, 1, 53);
		return utcNow.Year * 100 + num;
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
		OutgameSaveData outgameSaveData = EnsureSaveData();
		EnsureDailyShopState(outgameSaveData);
		int num = 1 << Mathf.Clamp(offerIndex, 0, 30);
		return (outgameSaveData.dailyShopPurchaseFlags & num) != 0;
	}

	public bool TryPurchaseDailyShopOffer(int offerIndex, out List<OutgameDrawResult> results, out string message)
	{
		results = new List<OutgameDrawResult>();
		message = string.Empty;
		OutgameSaveData outgameSaveData = EnsureSaveData();
		EnsureDailyShopState(outgameSaveData);
		int num = Mathf.Clamp(offerIndex, 0, 2);
		int num2 = 1 << num;
		if ((outgameSaveData.dailyShopPurchaseFlags & num2) != 0)
		{
			message = "오늘 이미 구매한 상품입니다.";
			return false;
		}
		switch (num)
		{
		case 0:
		{
			int num5 = Mathf.Max(1, Settings.dailyFreeGold);
			outgameSaveData.gold += num5;
			message = "일일 무료 선물 +" + num5.ToString("N0") + " GOLD";
			break;
		}
		case 1:
		{
			int num4 = Mathf.Max(1, Settings.dailyCardPackGoldCost);
			if (outgameSaveData.gold < num4)
			{
				message = "골드가 부족합니다.";
				return false;
			}
			outgameSaveData.gold -= num4;
			DrawCardsInto(results, OutgameChestType.Earned, Mathf.Max(1, Settings.dailyCardPackDrawCount));
			message = "일일 영웅 카드 묶음을 구매했습니다.";
			break;
		}
		default:
		{
			int num3 = Mathf.Max(1, Settings.dailyPremiumPackDiamondCost);
			if (outgameSaveData.diamonds < num3)
			{
				message = "다이아가 부족합니다.";
				return false;
			}
			outgameSaveData.diamonds -= num3;
			DrawCardsInto(results, OutgameChestType.Premium, Mathf.Max(1, Settings.dailyPremiumPackDrawCount));
			message = "일일 프리미엄 묶음을 구매했습니다.";
			break;
		}
		}
		outgameSaveData.dailyShopPurchaseFlags |= num2;
		Save();
		this.OnProgressChanged?.Invoke();
		return true;
	}

	public bool TryOpenPremiumChest(int drawCount, out List<OutgameDrawResult> results)
	{
		results = new List<OutgameDrawResult>();
		if ((Object)(object)characterDatabase == (Object)null || drawCount <= 0)
		{
			return false;
		}
		int num = ResolvePremiumChestCost(drawCount);
		OutgameSaveData outgameSaveData = EnsureSaveData();
		if (outgameSaveData.diamonds < num)
		{
			return false;
		}
		outgameSaveData.diamonds -= num;
		DrawCardsInto(results, OutgameChestType.Premium, drawCount);
		Save();
		this.OnProgressChanged?.Invoke();
		return results.Count > 0;
	}

	public bool TryOpenEarnedChest(out List<OutgameDrawResult> results)
	{
		results = new List<OutgameDrawResult>();
		OutgameSaveData outgameSaveData = EnsureSaveData();
		if ((Object)(object)characterDatabase == (Object)null || outgameSaveData.earnedChestKeys <= 0)
		{
			return false;
		}
		outgameSaveData.earnedChestKeys--;
		OutgameDrawResult outgameDrawResult = DrawCard(OutgameChestType.Earned);
		if (outgameDrawResult != null)
		{
			results.Add(outgameDrawResult);
		}
		Save();
		this.OnProgressChanged?.Invoke();
		return results.Count > 0;
	}

	public bool CycleWishlist()
	{
		if ((Object)(object)characterDatabase == (Object)null)
		{
			return false;
		}
		List<CharacterDefinition> list = (from character in characterDatabase.Characters
			where character != null
			orderby character.grade, character.id
			select character).ToList();
		if (list.Count == 0)
		{
			return false;
		}
		OutgameSaveData data = EnsureSaveData();
		int num = list.FindIndex((CharacterDefinition character) => character.id == data.wishlistCharacterId);
		int index = (num + 1) % list.Count;
		data.wishlistCharacterId = list[index].id;
		data.premiumWishlistPity = 0;
		Save();
		this.OnProgressChanged?.Invoke();
		return true;
	}

	public bool SetWishlistCharacter(string characterId)
	{
		CharacterDefinition characterDefinition = (((Object)(object)characterDatabase != (Object)null) ? characterDatabase.Characters.FirstOrDefault((CharacterDefinition character) => character != null && character.id == characterId) : null);
		if (characterDefinition == null)
		{
			return false;
		}
		OutgameSaveData outgameSaveData = EnsureSaveData();
		outgameSaveData.wishlistCharacterId = characterDefinition.id;
		outgameSaveData.premiumWishlistPity = 0;
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
		OutgameSaveData outgameSaveData = EnsureSaveData();
		int nextHurdleRound = CommercialRoundPacing.GetNextHurdleRound(outgameSaveData.highestRoundReached);
		return "무료 상자 " + outgameSaveData.earnedChestKeys + "개  |  게이지 " + outgameSaveData.earnedChestProgress + "/" + Mathf.Max(1, Settings.earnedChestProgressTarget) + "  |  다음 성장 허들 R" + nextHurdleRound;
	}

	public string GetWishlistDisplayName()
	{
		CharacterDefinition characterDefinition = ResolveWishlistCharacter();
		return (characterDefinition != null) ? characterDefinition.displayName : "미설정";
	}

	public bool IsOwned(string characterId)
	{
		OutgameCardRecord outgameCardRecord = FindRecord(characterId);
		return outgameCardRecord != null && outgameCardRecord.level > 0;
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
		int cardLevel = GetCardLevel(characterId);
		if (cardLevel > 0)
		{
			return cardLevel;
		}
		return IsTestMode ? 1 : 0;
	}

	public string BuildProgressText(string characterId)
	{
		OutgameCardRecord outgameCardRecord = FindRecord(characterId);
		if (outgameCardRecord == null || outgameCardRecord.level <= 0)
		{
			return IsTestMode ? "Lv.1  |  테스트 기본 보유" : "미획득  |  첫 카드 획득 시 해금";
		}
		if (outgameCardRecord.level >= Settings.maxCardLevel)
		{
			return "Lv." + outgameCardRecord.level + "  |  최대 성장";
		}
		return "Lv." + outgameCardRecord.level + "  |  카드 " + outgameCardRecord.upgradeCopies + "/" + RequiredCopiesForNextLevel(outgameCardRecord.level);
	}

	public int CountUpgradeableCards()
	{
		List<OutgameCardRecord> cards = EnsureSaveData().cards;
		int num = 0;
		for (int i = 0; i < cards.Count; i++)
		{
			OutgameCardRecord outgameCardRecord = cards[i];
			if (outgameCardRecord != null && outgameCardRecord.level > 0 && outgameCardRecord.level < Settings.maxCardLevel && outgameCardRecord.upgradeCopies >= RequiredCopiesForNextLevel(outgameCardRecord.level))
			{
				num++;
			}
		}
		return num;
	}

	public string BuildCollectionSummary()
	{
		int num = (((Object)(object)characterDatabase != (Object)null) ? characterDatabase.Characters.Count : 0);
		int num2 = 0;
		if (IsTestMode)
		{
			num2 = num;
		}
		else
		{
			List<OutgameCardRecord> cards = EnsureSaveData().cards;
			for (int i = 0; i < cards.Count; i++)
			{
				if (cards[i] != null && cards[i].level > 0)
				{
					num2++;
				}
			}
		}
		string text = (IsTestMode ? "전체 보유 영웅 " : "보유 영웅 ");
		return text + num2 + "/" + num + "  |  평균 성장 Lv." + GetAverageGrowthLevel().ToString("0.0");
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
		if (!((Object)(object)unit == (Object)null) && definition != null)
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
			bool isBossLike = monster.IsBossLike;
			float num = averageGrowthLevel * (isBossLike ? Settings.bossHealthPerAverageGrowthLevel : Settings.regularHealthPerAverageGrowthLevel);
			float num2 = averageGrowthLevel * (isBossLike ? Settings.bossAttackPerAverageGrowthLevel : Settings.regularAttackPerAverageGrowthLevel);
			healthMultiplier += Mathf.Min(num, Mathf.Min(Settings.maxMonsterHealthBonus, 0.15f));
			attackMultiplier += Mathf.Min(num2, Mathf.Min(Settings.maxMonsterAttackBonus, 0.1f));
		}
	}

	private string GrantCommercialBattleRewards(OutgameSaveData data, int round, bool victory)
	{
		int num = Mathf.Max(0, round);
		bool flag = victory && num > data.highestRoundReached;
		if (flag)
		{
			data.highestRoundReached = num;
		}
		int num2 = (victory ? (8 + Mathf.Min(50, num) / 4) : (6 + Mathf.Min(50, num) / 5));
		if (flag)
		{
			num2 += 3;
		}
		if (victory && num > 0 && num % 10 == 0)
		{
			num2 += 25;
		}
		if (victory && CommercialRoundPacing.IsMajorHurdleRound(num))
		{
			int num3 = Mathf.Clamp((num - 20) / 10, 0, 30);
			int num4 = 1 << num3;
			if ((data.hurdleClearRewardFlags & num4) == 0)
			{
				data.hurdleClearRewardFlags |= num4;
				num2 += 50;
			}
		}
		int num5 = 0;
		if (!victory && CommercialRoundPacing.TryGetApproachingHurdleIndex(num, out var hurdleIndex))
		{
			hurdleIndex = Mathf.Clamp(hurdleIndex, 0, 30);
			int num6 = 1 << hurdleIndex;
			if ((data.hurdleFailureSupportFlags & num6) == 0)
			{
				data.hurdleFailureSupportFlags |= num6;
				num5 = Mathf.Max(0, Settings.hurdleFailureSupportChestKeys);
				data.earnedChestKeys += num5;
			}
		}
		int num7 = AddEarnedChestProgress(data, num2);
		int num8 = num7 + num5;
		int num9 = (victory ? (60 + num * 12) : (35 + num * 8));
		data.gold += Mathf.Max(0, num9);
		string text = "상점 골드 +" + num9.ToString("N0") + " / 무료 상자 게이지 +" + num2 + " (" + data.earnedChestProgress + "/" + Mathf.Max(1, Settings.earnedChestProgressTarget) + ")";
		if (num8 > 0)
		{
			text = text + " / 상자 +" + num8;
		}
		if (num5 > 0)
		{
			text += " / 첫 허들 실패 지원";
		}
		return text;
	}

	private int AddEarnedChestProgress(OutgameSaveData data, int progress)
	{
		if (data == null || progress <= 0)
		{
			return 0;
		}
		int num = Mathf.Max(1, Settings.earnedChestProgressTarget);
		data.earnedChestProgress = Mathf.Max(0, data.earnedChestProgress) + progress;
		int num2 = data.earnedChestProgress / num;
		if (num2 > 0)
		{
			data.earnedChestKeys += num2;
			data.earnedChestProgress %= num;
		}
		return num2;
	}

	private void DrawCardsInto(List<OutgameDrawResult> results, OutgameChestType chestType, int drawCount)
	{
		if (results == null)
		{
			return;
		}
		int num = Mathf.Clamp(drawCount, 0, 100);
		for (int i = 0; i < num; i++)
		{
			OutgameDrawResult outgameDrawResult = DrawCard(chestType);
			if (outgameDrawResult != null)
			{
				results.Add(outgameDrawResult);
			}
		}
	}

	private void EnsureDailyShopState(OutgameSaveData data)
	{
		if (data != null)
		{
			DateTime now = DateTime.Now;
			int num = now.Year * 10000 + now.Month * 100 + now.Day;
			if (data.dailyShopDate != num)
			{
				data.dailyShopDate = num;
				data.dailyShopPurchaseFlags = 0;
				Save();
			}
		}
	}

	public string BuildDailyShopResetLabel()
	{
		DateTime now = DateTime.Now;
		TimeSpan timeSpan = now.Date.AddDays(1.0) - now;
		return "일일 상품 갱신까지 " + Mathf.Max(0, timeSpan.Hours).ToString("00") + ":" + Mathf.Max(0, timeSpan.Minutes).ToString("00");
	}

	private OutgameDrawResult DrawCard(OutgameChestType chestType)
	{
		OutgameSaveData data = EnsureSaveData();
		CharacterGrade characterGrade = ResolvePityMinimumGrade(data, chestType);
		bool pityTriggered = characterGrade > CharacterGrade.Normal;
		bool wishlistHit;
		CharacterDefinition characterDefinition = ResolveDrawCharacter(data, chestType, characterGrade, out wishlistHit);
		if (characterDefinition == null)
		{
			characterDefinition = characterDatabase.GetRandomSummonableCharacter();
		}
		if (characterDefinition == null)
		{
			return null;
		}
		UpdateChestPity(data, chestType, characterDefinition.grade, wishlistHit);
		OutgameCardRecord orCreateRecord = GetOrCreateRecord(characterDefinition.id);
		bool flag = orCreateRecord.level > 0;
		int level = orCreateRecord.level;
		orCreateRecord.totalCopies++;
		orCreateRecord.upgradeCopies++;
		ApplyAvailableLevelUps(orCreateRecord);
		return new OutgameDrawResult
		{
			character = characterDefinition,
			firstAcquisition = (!flag && orCreateRecord.level > 0),
			leveledUp = (orCreateRecord.level > level && flag),
			level = orCreateRecord.level,
			remainingCopies = orCreateRecord.upgradeCopies,
			requiredCopies = ((orCreateRecord.level < Settings.maxCardLevel) ? RequiredCopiesForNextLevel(orCreateRecord.level) : 0),
			chestType = chestType,
			wishlistHit = wishlistHit,
			pityTriggered = pityTriggered
		};
	}

	private CharacterDefinition ResolveDrawCharacter(OutgameSaveData data, OutgameChestType chestType, CharacterGrade minimumGrade, out bool wishlistHit)
	{
		wishlistHit = false;
		CharacterDefinition characterDefinition = ((chestType == OutgameChestType.Premium) ? ResolveWishlistCharacter() : null);
		if (characterDefinition != null && characterDefinition.grade >= minimumGrade)
		{
			int num = Mathf.Max(1, Settings.premiumWishlistPityDraws);
			bool flag = data.premiumWishlistPity >= num - 1;
			bool flag2 = Random.value < Mathf.Clamp01(Settings.premiumWishlistChance);
			if (flag || flag2)
			{
				wishlistHit = true;
				return characterDefinition;
			}
		}
		CharacterGrade characterGrade = ((chestType == OutgameChestType.Premium) ? RollPremiumGrade() : RollGrade());
		if (characterGrade < minimumGrade)
		{
			characterGrade = minimumGrade;
		}
		return characterDatabase.GetRandomCharacterByGradeOrLower(characterGrade);
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
		return ((Object)(object)characterDatabase != (Object)null && !string.IsNullOrWhiteSpace(wishlistId)) ? characterDatabase.Characters.FirstOrDefault((CharacterDefinition character) => character != null && character.id == wishlistId) : null;
	}

	private static int IncrementPity(int value)
	{
		return (value >= 1000000) ? 1000000 : (Mathf.Max(0, value) + 1);
	}

	private void EnsureInitialRoster()
	{
		OutgameSaveData outgameSaveData = EnsureSaveData();
		if (outgameSaveData.initialRosterGranted)
		{
			return;
		}
		if (!IsTestMode && (Object)(object)characterDatabase != (Object)null)
		{
			int num = 0;
			List<string> starterIds = Settings.serviceStarterCharacterIds;
			if (starterIds != null)
			{
				int i;
				for (i = 0; i < starterIds.Count; i++)
				{
					if (num >= Settings.serviceStarterCharacterCount)
					{
						break;
					}
					CharacterDefinition characterDefinition = characterDatabase.Characters.FirstOrDefault((CharacterDefinition character) => character != null && character.id == starterIds[i]);
					if (characterDefinition != null)
					{
						GrantInitialCard(characterDefinition);
						num++;
					}
				}
			}
			for (int num2 = 0; num2 < characterDatabase.Characters.Count; num2++)
			{
				if (num >= Settings.serviceStarterCharacterCount)
				{
					break;
				}
				CharacterDefinition characterDefinition2 = characterDatabase.Characters[num2];
				if (characterDefinition2 != null && !IsOwned(characterDefinition2.id))
				{
					GrantInitialCard(characterDefinition2);
					num++;
				}
			}
		}
		outgameSaveData.initialRosterGranted = true;
		Save();
	}

	private void GrantInitialCard(CharacterDefinition character)
	{
		OutgameCardRecord orCreateRecord = GetOrCreateRecord(character.id);
		if (orCreateRecord.level <= 0)
		{
			int num = Mathf.Max(1, Settings.initialUnlockCopies);
			orCreateRecord.totalCopies += num;
			orCreateRecord.upgradeCopies += num;
			ApplyAvailableLevelUps(orCreateRecord);
		}
	}

	private CharacterGrade RollGrade()
	{
		float num = Settings.normalRate + Settings.rareRate + Settings.epicRate + Settings.legendaryRate + Settings.mythicRate + Settings.transcendentRate;
		float num2 = Random.value * Mathf.Max(0.001f, num);
		if ((num2 -= Settings.normalRate) < 0f)
		{
			return CharacterGrade.Normal;
		}
		if ((num2 -= Settings.rareRate) < 0f)
		{
			return CharacterGrade.Rare;
		}
		if ((num2 -= Settings.epicRate) < 0f)
		{
			return CharacterGrade.Epic;
		}
		if ((num2 -= Settings.legendaryRate) < 0f)
		{
			return CharacterGrade.Legendary;
		}
		if ((num2 -= Settings.mythicRate) < 0f)
		{
			return CharacterGrade.Mythic;
		}
		return CharacterGrade.Transcendent;
	}

	private CharacterGrade RollPremiumGrade()
	{
		float num = Settings.premiumNormalRate + Settings.premiumRareRate + Settings.premiumEpicRate + Settings.premiumLegendaryRate + Settings.premiumMythicRate + Settings.premiumTranscendentRate;
		float num2 = Random.value * Mathf.Max(0.001f, num);
		if ((num2 -= Settings.premiumNormalRate) < 0f)
		{
			return CharacterGrade.Normal;
		}
		if ((num2 -= Settings.premiumRareRate) < 0f)
		{
			return CharacterGrade.Rare;
		}
		if ((num2 -= Settings.premiumEpicRate) < 0f)
		{
			return CharacterGrade.Epic;
		}
		if ((num2 -= Settings.premiumLegendaryRate) < 0f)
		{
			return CharacterGrade.Legendary;
		}
		if ((num2 -= Settings.premiumMythicRate) < 0f)
		{
			return CharacterGrade.Mythic;
		}
		return CharacterGrade.Transcendent;
	}

	private void ApplyAvailableLevelUps(OutgameCardRecord record)
	{
		while (record.level < Settings.maxCardLevel)
		{
			int num = ((record.level == 0) ? Mathf.Max(1, Settings.initialUnlockCopies) : RequiredCopiesForNextLevel(record.level));
			if (record.upgradeCopies < num)
			{
				break;
			}
			record.upgradeCopies -= num;
			record.level++;
		}
	}

	private int RequiredCopiesForNextLevel(int currentLevel)
	{
		return Mathf.Max(1, Settings.duplicateCopiesForLevelTwo + Mathf.Max(0, currentLevel - 1) * Settings.additionalCopiesPerLevel);
	}

	private float GetAverageGrowthLevel()
	{
		List<OutgameCardRecord> cards = EnsureSaveData().cards;
		float num = 0f;
		int num2 = 0;
		for (int i = 0; i < cards.Count; i++)
		{
			OutgameCardRecord outgameCardRecord = cards[i];
			if (outgameCardRecord != null && outgameCardRecord.level > 0)
			{
				num += (float)Mathf.Max(0, outgameCardRecord.level - 1);
				num2++;
			}
		}
		return (num2 > 0) ? (num / (float)num2) : 0f;
	}

	private OutgameCardRecord FindRecord(string characterId)
	{
		List<OutgameCardRecord> cards = EnsureSaveData().cards;
		for (int i = 0; i < cards.Count; i++)
		{
			if (cards[i] != null && cards[i].characterId == characterId)
			{
				return cards[i];
			}
		}
		return null;
	}

	private OutgameCardRecord GetOrCreateRecord(string characterId)
	{
		OutgameCardRecord outgameCardRecord = FindRecord(characterId);
		if (outgameCardRecord != null)
		{
			return outgameCardRecord;
		}
		outgameCardRecord = new OutgameCardRecord
		{
			characterId = characterId
		};
		EnsureSaveData().cards.Add(outgameCardRecord);
		return outgameCardRecord;
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
		string text = PlayerPrefs.GetString(ResolveSaveKey(), string.Empty);
		int num = (IsTestMode ? Settings.testStartingGold : Settings.startingGold);
		int diamonds = (IsTestMode ? Settings.testStartingDiamonds : Settings.startingDiamonds);
		saveData = (string.IsNullOrEmpty(text) ? new OutgameSaveData
		{
			gold = num,
			diamonds = diamonds
		} : JsonUtility.FromJson<OutgameSaveData>(text));
		if (saveData == null)
		{
			saveData = new OutgameSaveData
			{
				gold = num,
				diamonds = diamonds
			};
		}
		if (saveData.cards == null)
		{
			saveData.cards = new List<OutgameCardRecord>();
		}
		int metaProgressionVersion = saveData.metaProgressionVersion;
		int num2 = Mathf.Max(3, Settings.progressionVersion);
		bool flag = metaProgressionVersion < num2;
		if (metaProgressionVersion < 2)
		{
			int num3 = (string.IsNullOrEmpty(text) ? Mathf.Max(0, Settings.startingEarnedChestKeys) : Mathf.Max(0, Settings.migrationEarnedChestKeys));
			saveData.earnedChestKeys = Mathf.Max(0, saveData.earnedChestKeys) + num3;
		}
		if (metaProgressionVersion < 3)
		{
			saveData.gold = Mathf.Max(0, saveData.gold) + num;
		}
		saveData.metaProgressionVersion = num2;
		saveData.gold = Mathf.Max(0, saveData.gold);
		saveData.earnedChestKeys = Mathf.Max(0, saveData.earnedChestKeys);
		saveData.earnedChestProgress = Mathf.Max(0, saveData.earnedChestProgress);
		int num4 = Mathf.Max(1, Settings.earnedChestProgressTarget);
		if (saveData.earnedChestProgress >= num4)
		{
			saveData.earnedChestKeys += saveData.earnedChestProgress / num4;
			saveData.earnedChestProgress %= num4;
			flag = true;
		}
		EnsureDailyShopState(saveData);
		EnsureCurrentSeason(saveData);
		lastSeasonRewardSummary = string.Empty;
		if (flag)
		{
			PlayerPrefs.SetString(ResolveSaveKey(), JsonUtility.ToJson((object)saveData));
			PlayerPrefs.Save();
		}
	}

	private void Save()
	{
		PlayerPrefs.SetString(ResolveSaveKey(), JsonUtility.ToJson((object)EnsureSaveData()));
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
