using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame;

internal static class CombatRuntimeQuery
{
	private const float MinimumRange = 0.1f;

	public static float ScheduleNextRefresh(Object owner, float interval)
	{
		float num = Mathf.Max(0.02f, interval);
		int num2 = ((owner != (Object)null) ? owner.GetInstanceID() : 0);
		float num3 = (float)Mathf.Abs(num2 % 17) / 16f;
		return Time.unscaledTime + num * Mathf.Lerp(0.85f, 1.15f, num3);
	}

	public static bool IsValidMonsterTarget(MonsterUnit target, Vector3 origin, float range)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)target == (Object)null || !target.CanBeCombatTargeted)
		{
			return false;
		}
		float num = Mathf.Max(0.1f, range);
		Vector3 val = ((Component)target).transform.position - origin;
		return ((Vector3)(ref val)).sqrMagnitude <= num * num;
	}

	public static bool IsValidDefenderTarget(DefenderUnit target)
	{
		return (Object)(object)target != (Object)null && target.CanBeCombatTargeted;
	}

	public static MonsterUnit FindNearestMonster(IReadOnlyList<MonsterUnit> candidates, Vector3 origin, float range)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (candidates == null)
		{
			return null;
		}
		float num = Mathf.Max(0.1f, range);
		float num2 = num * num;
		MonsterUnit result = null;
		for (int i = 0; i < candidates.Count; i++)
		{
			MonsterUnit monsterUnit = candidates[i];
			if (!((Object)(object)monsterUnit == (Object)null) && monsterUnit.CanBeCombatTargeted)
			{
				Vector3 val = ((Component)monsterUnit).transform.position - origin;
				float sqrMagnitude = ((Vector3)(ref val)).sqrMagnitude;
				if (!(sqrMagnitude > num2))
				{
					num2 = sqrMagnitude;
					result = monsterUnit;
				}
			}
		}
		return result;
	}

	public static MonsterUnit FindRandomMonster(IReadOnlyList<MonsterUnit> candidates, Vector3 origin, float range)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if (candidates == null)
		{
			return null;
		}
		float num = Mathf.Max(0.1f, range);
		float num2 = num * num;
		MonsterUnit result = null;
		int num3 = 0;
		for (int i = 0; i < candidates.Count; i++)
		{
			MonsterUnit monsterUnit = candidates[i];
			if ((Object)(object)monsterUnit == (Object)null || !monsterUnit.CanBeCombatTargeted)
			{
				continue;
			}
			Vector3 val = ((Component)monsterUnit).transform.position - origin;
			if (!(((Vector3)(ref val)).sqrMagnitude > num2))
			{
				num3++;
				if (Random.Range(0, num3) == 0)
				{
					result = monsterUnit;
				}
			}
		}
		return result;
	}

	public static DefenderUnit FindNearestDefender(IReadOnlyList<DefenderUnit> candidates, Vector3 origin)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		if (candidates == null)
		{
			return null;
		}
		float num = float.MaxValue;
		DefenderUnit result = null;
		for (int i = 0; i < candidates.Count; i++)
		{
			DefenderUnit defenderUnit = candidates[i];
			if (IsValidDefenderTarget(defenderUnit))
			{
				Vector3 val = ((Component)defenderUnit).transform.position - origin;
				float sqrMagnitude = ((Vector3)(ref val)).sqrMagnitude;
				if (!(sqrMagnitude >= num))
				{
					num = sqrMagnitude;
					result = defenderUnit;
				}
			}
		}
		return result;
	}
}
