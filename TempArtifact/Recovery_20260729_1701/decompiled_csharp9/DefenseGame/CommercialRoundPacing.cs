using UnityEngine;

namespace DefenseGame
{
	public static class CommercialRoundPacing
	{
		public const int FirstHurdleRound = 20;

		public const int HurdleInterval = 10;

		public static CommercialRoundTuning Resolve(int round, bool bossLike)
		{
			int safeRound = Mathf.Max(1, round);
			int hurdleRound = ResolveRelevantHurdleRound(safeRound);
			int hurdleTier = Mathf.Max(0, (hurdleRound - 20) / 10);
			CommercialRoundPhase phase = ResolvePhase(safeRound, hurdleRound);
			CommercialRoundTuning tuning = new CommercialRoundTuning
			{
				phase = phase,
				hurdleRound = hurdleRound,
				hurdleTier = hurdleTier,
				healthMultiplier = 1f,
				attackMultiplier = 1f,
				spawnCountMultiplier = 1f,
				spawnIntervalMultiplier = 1f
			};
			switch (phase)
			{
			case CommercialRoundPhase.BuildUp:
				tuning.healthMultiplier = 1.05f + Mathf.Min(0.05f, (float)hurdleTier * 0.01f);
				tuning.attackMultiplier = 1.03f + Mathf.Min(0.04f, (float)hurdleTier * 0.01f);
				tuning.spawnCountMultiplier = 1.06f;
				tuning.spawnIntervalMultiplier = 0.96f;
				break;
			case CommercialRoundPhase.Hurdle:
				tuning.healthMultiplier = (bossLike ? 1.2f : 1.1f) + Mathf.Min(0.16f, (float)hurdleTier * 0.04f);
				tuning.attackMultiplier = (bossLike ? 1.1f : 1.06f) + Mathf.Min(0.1f, (float)hurdleTier * 0.025f);
				tuning.spawnCountMultiplier = 1.08f;
				tuning.spawnIntervalMultiplier = 0.94f;
				break;
			case CommercialRoundPhase.Relief:
				tuning.healthMultiplier = 0.82f + Mathf.Min(0.05f, (float)hurdleTier * 0.01f);
				tuning.attackMultiplier = 0.9f + Mathf.Min(0.04f, (float)hurdleTier * 0.01f);
				tuning.spawnCountMultiplier = 0.86f;
				tuning.spawnIntervalMultiplier = 1.12f;
				break;
			}
			return tuning;
		}

		public static bool IsMajorHurdleRound(int round)
		{
			return round >= 20 && round % 10 == 0;
		}

		public static bool TryGetApproachingHurdleIndex(int round, out int hurdleIndex)
		{
			int hurdleRound = GetNextOrCurrentHurdleRound(round);
			bool applies = round >= hurdleRound - 2 && round <= hurdleRound;
			hurdleIndex = (applies ? Mathf.Max(0, (hurdleRound - 20) / 10) : (-1));
			return applies;
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
			CommercialRoundTuning tuning = Resolve(round, bossLike);
			healthMultiplier = tuning.healthMultiplier;
			attackMultiplier = tuning.attackMultiplier;
		}

		public static int ApplySpawnCount(int round, bool bossRound, int count)
		{
			CommercialRoundTuning tuning = Resolve(round, bossRound);
			return Mathf.Max(0, Mathf.RoundToInt((float)Mathf.Max(0, count) * tuning.spawnCountMultiplier));
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
			int previous = round / 10 * 10;
			if (round - previous <= 3)
			{
				return Mathf.Max(20, previous);
			}
			return Mathf.Max(20, previous + 10);
		}

		private static int GetNextOrCurrentHurdleRound(int round)
		{
			if (round <= 20)
			{
				return 20;
			}
			int remainder = round % 10;
			return (remainder == 0) ? round : (round + (10 - remainder));
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
}
