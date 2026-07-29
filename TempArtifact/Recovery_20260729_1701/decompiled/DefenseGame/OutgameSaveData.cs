using System;
using System.Collections.Generic;

namespace DefenseGame;

[Serializable]
public class OutgameSaveData
{
	public int gold;

	public int diamonds;

	public int dailyShopDate;

	public int dailyShopPurchaseFlags;

	public int metaProgressionVersion;

	public int earnedChestKeys;

	public int earnedChestProgress;

	public int earnedRarePity;

	public int earnedEpicPity;

	public int premiumEpicPity;

	public int premiumLegendaryPity;

	public int premiumWishlistPity;

	public string wishlistCharacterId;

	public int highestRoundReached;

	public int hurdleFailureSupportFlags;

	public int hurdleClearRewardFlags;

	public bool initialRosterGranted;

	public List<OutgameCardRecord> cards = new List<OutgameCardRecord>();

	public int seasonId;

	public int weeklyBossScore;

	public int weeklyBestRunScore;

	public int weeklyBossKills;

	public int seasonMissionClaimFlags;

	public int coopBossScore;

	public int coopMvpCount;

	public string lastCoopMvpName;

	public string lastDeckShareCode;

	public string lastReplayDigest;
}
