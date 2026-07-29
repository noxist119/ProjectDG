using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame;

public class RuntimeAreaDamageZone : MonoBehaviour
{
	private Vector3 center;

	private float radius;

	private float damagePerTick;

	private float duration;

	private float tickInterval;

	private float elapsed;

	private float tickTimer;

	private DefenderUnit source;

	private SkillDefinition sourceSkill;

	public void Configure(Vector3 zoneCenter, float zoneRadius, float zoneDamagePerTick, float zoneDuration, float zoneTickInterval, DefenderUnit zoneSource, SkillDefinition zoneSourceSkill = null)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		center = zoneCenter;
		radius = Mathf.Max(0.1f, zoneRadius);
		damagePerTick = Mathf.Max(0f, zoneDamagePerTick);
		duration = Mathf.Max(0.1f, zoneDuration);
		tickInterval = Mathf.Max(0.2f, zoneTickInterval);
		source = zoneSource;
		sourceSkill = zoneSourceSkill;
		elapsed = 0f;
		tickTimer = 0f;
	}

	private void Update()
	{
		elapsed += Time.deltaTime;
		tickTimer -= Time.deltaTime;
		if (tickTimer <= 0f)
		{
			ApplyTick();
			tickTimer += tickInterval;
		}
		if (elapsed >= duration)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
	}

	private void ApplyTick()
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		if (damagePerTick <= 0f)
		{
			return;
		}
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monster = activeInstances[i];
			if ((Object)(object)monster != (Object)null && monster.CanBeCombatTargeted && Vector3.Distance(center, ((Component)monster).transform.position) <= radius)
			{
				DefenderUnit.RunWithSkillDamageContext(sourceSkill, delegate
				{
					monster.TakeDamage(damagePerTick, critical: false, source);
				});
			}
		}
	}
}
