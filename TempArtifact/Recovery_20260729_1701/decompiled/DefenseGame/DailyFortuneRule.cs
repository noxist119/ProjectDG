using UnityEngine;

namespace DefenseGame;

public sealed class DailyFortuneRule
{
	public string title;

	public string summary;

	public float epicSummonChanceBonus;

	public float bossHealthBonus;

	public float shopDiscountRate;

	public int startGoldBonus;

	public int lifeRecoveryBonus;

	public float BossHealthMultiplier => 1f + Mathf.Max(0f, bossHealthBonus);

	public float ShopCostMultiplier => Mathf.Clamp01(1f - Mathf.Max(0f, shopDiscountRate));
}
