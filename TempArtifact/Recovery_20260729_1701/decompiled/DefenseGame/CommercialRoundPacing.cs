using UnityEngine;

namespace DefenseGame;

public static class CommercialRoundPacing
{
	public const int FirstHurdleRound = 20;

	public const int HurdleInterval = 10;

	public static CommercialRoundTuning Resolve(int round, bool bossLike)
	{
		int round2 = Mathf.Max(1, round);
		int num = ResolveRelevantHurdleRound(round2);
		int num2 = Mathf.Max(0, (num - 20) / 10);
		CommercialRoundPhase commercialRoundPhase = ResolvePhase(round2, num);
		CommercialRoundTuning result = new CommercialRoundTuning
		{
			phase = commercialRoundPhase,
			hurdleRound = num,
			hurdleTier = num2,
			healthMultiplier = 1f,
			attackMultiplier = 1f,
			spawnCountMultiplier = 1f,
			spawnIntervalMultiplier = 1f
		};
		switch (commercialRoundPhase)
		{
		case CommercialRoundPhase.BuildUp:
			result.healthMultiplier = 1.05f + Mathf.Min(0.05f, (float)num2 * 0.01f);
			result.attackMultiplier = 1.03f + Mathf.Min(0.04f, (float)num2 * 0.01f);
			result.spawnCountMultiplier = 1.06f;
			result.spawnIntervalMultiplier = 0.96f;
			break;
		case CommercialRoundPhase.Hurdle:
			result.healthMultiplier = (bossLike ? 1.2f : 1.1f) + Mathf.Min(0.16f, (float)num2 * 0.04f);
			result.attackMultiplier = (bossLike ? 1.1f : 1.06f) + Mathf.Min(0.1f, (float)num2 * 0.025f);
			result.spawnCountMultiplier = 1.08f;
			result.spawnIntervalMultiplier = 0.94f;
			break;
		case CommercialRoundPhase.Relief:
			result.healthMultiplier = 0.82f + Mathf.Min(0.05f, (float)num2 * 0.01f);
			result.attackMultiplier = 0.9f + Mathf.Min(0.04f, (float)num2 * 0.01f);
			result.spawnCountMultiplier = 0.86f;
			result.spawnIntervalMultiplier = 1.12f;
			break;
		}
		return result;
	}

	public static bool IsMajorHurdleRound(int round)
	{
		return round >= 20 && round % 10 == 0;
	}

	public static bool TryGetApproachingHurdleIndex(int round, out int hurdleIndex)
	{
		int nextOrCurrentHurdleRound = GetNextOrCurrentHurdleRound(round);
		bool flag = round >= nextOrCurrentHurdleRound - 2 && round <= nextOrCurrentHurdleRound;
		hurdleIndex = (flag ? Mathf.Max(0, (nextOrCurrentHurdleRound - 20) / 10) : (-1));
		return flag;
	}

	public static int GetNextHurdleRound(int completedRound)
	{
		if (completedRound < 20)
		{
			return 20;
		}
		return (completedRound / 10 + 1) * 10;
	}

	public static void ResolveCombatMultipliers(int round, bool bossLike, out float healthMultiplier, out float attackMultiplier)
	{
		CommercialRoundTuning commercialRoundTuning = Resolve(round, bossLike);
		healthMultiplier = commercialRoundTuning.healthMultiplier;
		attackMultiplier = commercialRoundTuning.attackMultiplier;
	}

	public static int ApplySpawnCount(int round, bool bossRound, int count)
	{
		CommercialRoundTuning commercialRoundTuning = Resolve(round, bossRound);
		return Mathf.Max(0, Mathf.RoundToInt((float)Mathf.Max(0, count) * commercialRoundTuning.spawnCountMultiplier));
	}

	public static float ApplySpawnInterval(int round, bool bossRound, float interval)
	{
		return Mathf.Max(0.05f, interval * Resolve(round, bossRound).spawnIntervalMultiplier);
	}

	public static string BuildPhaseLabel(int round)
	{
		return Resolve(round, round > 0 && round % 10 == 0).phase switch
		{
			CommercialRoundPhase.BuildUp => "허들 전 압박", 
			CommercialRoundPhase.Hurdle => "성장 허들", 
			CommercialRoundPhase.Relief => "돌파 보너스 구간", 
			_ => "성장 구간", 
		};
	}

	private static int ResolveRelevantHurdleRound(int round)
	{
		if (round < 20)
		{
			return 20;
		}
		int num = round / 10 * 10;
		if (round - num <= 3)
		{
			return Mathf.Max(20, num);
		}
		return Mathf.Max(20, num + 10);
	}

	private static int GetNextOrCurrentHurdleRound(int round)
	{
		if (round <= 20)
		{
			return 20;
		}
		int num = round % 10;
		return (num == 0) ? round : (round + (10 - num));
	}

	private static CommercialRoundPhase ResolvePhase(int round, int hurdleRound)
	{
		if (round == hurdleRound && IsMajorHurdleRound(round))
		{
			return CommercialRoundPhase.Hurdle;
		}
		if (round >= hurdleRound - 2 && round < hurdleRound)
		{
			return CommercialRoundPhase.BuildUp;
		}
		if (round > hurdleRound && round <= hurdleRound + 3)
		{
			return CommercialRoundPhase.Relief;
		}
		return CommercialRoundPhase.Stable;
	}
}
