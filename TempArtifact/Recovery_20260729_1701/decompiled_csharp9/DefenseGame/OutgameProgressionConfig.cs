using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
	[CreateAssetMenu(fileName = "OutgameProgressionConfig", menuName = "Defense Game/Outgame Progression")]
	public class OutgameProgressionConfig : ScriptableObject
	{
		[Header("Profiles")]
		public OutgamePlayMode defaultPlayMode = OutgamePlayMode.Service;

		public int testStartingDiamonds = 999999;

		public int testDiamondRechargeAmount = 10000;

		public int testStartingGold = 999999;

		public int testGoldRechargeAmount = 10000;

		public int serviceStarterCharacterCount = 5;

		public List<string> serviceStarterCharacterIds = new List<string> { "hero_01", "hero_02", "hero_03", "hero_04", "hero_05" };

		[Header("Shop Economy")]
		public int startingGold = 3000;

		public int startingDiamonds = 500;

		public int singleChestCost = 100;

		public int tenChestCost = 900;

		public int fiveChestCost = 480;

		public int twentyChestCost = 1800;

		public int fiftyChestCost = 4250;

		public int hundredChestCost = 8000;

		public int dailyFreeGold = 500;

		public int dailyCardPackGoldCost = 1200;

		public int dailyCardPackDrawCount = 5;

		public int dailyPremiumPackDiamondCost = 250;

		public int dailyPremiumPackDrawCount = 3;

		public int diamondsPerBattleRewardPoint = 1;

		[Header("Earned Chest Loop")]
		public int progressionVersion = 3;

		public int startingEarnedChestKeys = 1;

		public int migrationEarnedChestKeys = 1;

		public int earnedChestProgressTarget = 100;

		public int earnedChestRarePityDraws = 8;

		public int earnedChestEpicPityDraws = 30;

		public int premiumChestEpicPityDraws = 10;

		public int premiumChestLegendaryPityDraws = 40;

		public int premiumWishlistPityDraws = 20;

		[Range(0f, 1f)]
		public float premiumWishlistChance = 0.12f;

		public int hurdleFailureSupportChestKeys = 1;

		[Header("Card Growth")]
		public int initialUnlockCopies = 1;

		public int duplicateCopiesForLevelTwo = 2;

		public int additionalCopiesPerLevel = 1;

		public int maxCardLevel = 20;

		[Header("Chest Grade Rates")]
		[Range(0f, 1f)]
		public float normalRate = 0.65f;

		[Range(0f, 1f)]
		public float rareRate = 0.23f;

		[Range(0f, 1f)]
		public float epicRate = 0.085f;

		[Range(0f, 1f)]
		public float legendaryRate = 0.028f;

		[Range(0f, 1f)]
		public float mythicRate = 0.006f;

		[Range(0f, 1f)]
		public float transcendentRate = 0.001f;

		[Header("Premium Chest Grade Rates")]
		[Range(0f, 1f)]
		public float premiumNormalRate = 0.45f;

		[Range(0f, 1f)]
		public float premiumRareRate = 0.32f;

		[Range(0f, 1f)]
		public float premiumEpicRate = 0.16f;

		[Range(0f, 1f)]
		public float premiumLegendaryRate = 0.055f;

		[Range(0f, 1f)]
		public float premiumMythicRate = 0.013f;

		[Range(0f, 1f)]
		public float premiumTranscendentRate = 0.002f;

		[Header("Unit Growth Per Card Level")]
		[Range(0f, 0.2f)]
		public float attackPowerPerGrowthLevel = 0.03f;

		[Range(0f, 0.2f)]
		public float maxHealthPerGrowthLevel = 0.03f;

		[Header("Monster Balance From Collection Growth")]
		public bool scaleMonstersWithCollectionGrowth = false;

		[Range(0f, 0.2f)]
		public float regularHealthPerAverageGrowthLevel = 0.018f;

		[Range(0f, 0.2f)]
		public float regularAttackPerAverageGrowthLevel = 0.012f;

		[Range(0f, 0.2f)]
		public float bossHealthPerAverageGrowthLevel = 0.025f;

		[Range(0f, 0.2f)]
		public float bossAttackPerAverageGrowthLevel = 0.016f;

		[Range(0f, 5f)]
		public float maxMonsterHealthBonus = 0.75f;

		[Range(0f, 5f)]
		public float maxMonsterAttackBonus = 0.5f;
	}
}
