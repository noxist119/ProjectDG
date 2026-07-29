using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
	internal static class CombatRuntimeQuery
	{
		private const float MinimumRange = 0.1f;

		public static float ScheduleNextRefresh(Object owner, float interval)
		{
			float safeInterval = Mathf.Max(0.02f, interval);
			int instanceId = ((owner != null) ? owner.GetInstanceID() : 0);
			float stagger = (float)Mathf.Abs(instanceId % 17) / 16f;
			return Time.unscaledTime + safeInterval * Mathf.Lerp(0.85f, 1.15f, stagger);
		}

		public static bool IsValidMonsterTarget(MonsterUnit target, Vector3 origin, float range)
		{
			if (target == null || !target.CanBeCombatTargeted)
			{
				return false;
			}
			float checkedRange = Mathf.Max(0.1f, range);
			return (target.transform.position - origin).sqrMagnitude <= checkedRange * checkedRange;
		}

		public static bool IsValidDefenderTarget(DefenderUnit target)
		{
			return target != null && target.CanBeCombatTargeted;
		}

		public static MonsterUnit FindNearestMonster(IReadOnlyList<MonsterUnit> candidates, Vector3 origin, float range)
		{
			if (candidates == null)
			{
				return null;
			}
			float checkedRange = Mathf.Max(0.1f, range);
			float bestSqrDistance = checkedRange * checkedRange;
			MonsterUnit bestTarget = null;
			for (int i = 0; i < candidates.Count; i++)
			{
				MonsterUnit candidate = candidates[i];
				if (!(candidate == null) && candidate.CanBeCombatTargeted)
				{
					float sqrDistance = (candidate.transform.position - origin).sqrMagnitude;
					if (!(sqrDistance > bestSqrDistance))
					{
						bestSqrDistance = sqrDistance;
						bestTarget = candidate;
					}
				}
			}
			return bestTarget;
		}

		public static MonsterUnit FindRandomMonster(IReadOnlyList<MonsterUnit> candidates, Vector3 origin, float range)
		{
			if (candidates == null)
			{
				return null;
			}
			float checkedRange = Mathf.Max(0.1f, range);
			float checkedRangeSqr = checkedRange * checkedRange;
			MonsterUnit selected = null;
			int validCount = 0;
			for (int i = 0; i < candidates.Count; i++)
			{
				MonsterUnit candidate = candidates[i];
				if (!(candidate == null) && candidate.CanBeCombatTargeted && !((candidate.transform.position - origin).sqrMagnitude > checkedRangeSqr))
				{
					validCount++;
					if (Random.Range(0, validCount) == 0)
					{
						selected = candidate;
					}
				}
			}
			return selected;
		}

		public static DefenderUnit FindNearestDefender(IReadOnlyList<DefenderUnit> candidates, Vector3 origin)
		{
			if (candidates == null)
			{
				return null;
			}
			float bestSqrDistance = float.MaxValue;
			DefenderUnit bestTarget = null;
			for (int i = 0; i < candidates.Count; i++)
			{
				DefenderUnit candidate = candidates[i];
				if (IsValidDefenderTarget(candidate))
				{
					float sqrDistance = (candidate.transform.position - origin).sqrMagnitude;
					if (!(sqrDistance >= bestSqrDistance))
					{
						bestSqrDistance = sqrDistance;
						bestTarget = candidate;
					}
				}
			}
			return bestTarget;
		}
	}
}
