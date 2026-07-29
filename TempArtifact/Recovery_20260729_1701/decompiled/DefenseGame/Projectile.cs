using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame;

public class Projectile : MonoBehaviour
{
	private MonsterUnit target;

	private DefenderUnit defenderTarget;

	private float damage;

	private float speed;

	private bool critical;

	private float splashRadius;

	private float splashDamageRatio;

	private int additionalPierceCount;

	private DefenderUnit owner;

	private MonsterUnit monsterOwner;

	private float defenderTargetHeight;

	private Action<MonsterUnit> monsterImpactHandler;

	private GameObject hitEffectPrefab;

	private const int MaxPoolPerPrefab = 48;

	private static readonly Dictionary<GameObject, Stack<Projectile>> Pools = new Dictionary<GameObject, Stack<Projectile>>();

	private GameObject sourcePrefab;

	private bool recycling;

	private TrailRenderer[] cachedTrails;

	private ParticleSystem[] cachedParticles;

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void ResetPools()
	{
		Pools.Clear();
	}

	public static Projectile Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
	{
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)prefab == (Object)null)
		{
			return null;
		}
		Projectile projectile = null;
		if (Pools.TryGetValue(prefab, out var value))
		{
			while (value.Count > 0 && (Object)(object)projectile == (Object)null)
			{
				projectile = value.Pop();
			}
		}
		if ((Object)(object)projectile == (Object)null)
		{
			GameObject val = Object.Instantiate<GameObject>(prefab, position, rotation);
			if ((Object)(object)val == (Object)null)
			{
				return null;
			}
			projectile = val.GetComponent<Projectile>();
			if ((Object)(object)projectile == (Object)null)
			{
				projectile = val.AddComponent<Projectile>();
			}
		}
		projectile.sourcePrefab = prefab;
		projectile.recycling = false;
		((Component)projectile).transform.SetPositionAndRotation(position, rotation);
		((Component)projectile).gameObject.SetActive(true);
		projectile.PrepareForSpawn();
		return projectile;
	}

	private void PrepareForSpawn()
	{
		if (cachedTrails == null)
		{
			cachedTrails = ((Component)this).GetComponentsInChildren<TrailRenderer>(true);
		}
		for (int i = 0; i < cachedTrails.Length; i++)
		{
			if ((Object)(object)cachedTrails[i] != (Object)null)
			{
				cachedTrails[i].Clear();
			}
		}
		if (cachedParticles == null)
		{
			cachedParticles = ((Component)this).GetComponentsInChildren<ParticleSystem>(true);
		}
		for (int j = 0; j < cachedParticles.Length; j++)
		{
			if (!((Object)(object)cachedParticles[j] == (Object)null))
			{
				cachedParticles[j].Clear(true);
				cachedParticles[j].Play(true);
			}
		}
	}

	public void Initialize(MonsterUnit targetMonster, float projectileDamage, float projectileSpeed, bool isCritical, float projectileSplashRadius = 0f, float projectileSplashDamageRatio = 0f, int projectileAdditionalPierceCount = 0, DefenderUnit projectileOwner = null, Action<MonsterUnit> projectileMonsterImpactHandler = null, GameObject projectileHitEffectPrefab = null)
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		target = targetMonster;
		defenderTarget = null;
		damage = projectileDamage;
		speed = projectileSpeed;
		critical = isCritical;
		splashRadius = projectileSplashRadius;
		splashDamageRatio = projectileSplashDamageRatio;
		additionalPierceCount = projectileAdditionalPierceCount;
		owner = projectileOwner;
		monsterOwner = null;
		defenderTargetHeight = 0f;
		monsterImpactHandler = projectileMonsterImpactHandler;
		hitEffectPrefab = projectileHitEffectPrefab;
		if ((Object)(object)target != (Object)null)
		{
			((Component)this).transform.rotation = RuntimeEffectUtility.FaceTowards(((Component)this).transform.position, ((Component)target).transform.position, ((Component)this).transform.rotation);
		}
	}

	public void Initialize(DefenderUnit targetDefender, float projectileDamage, float projectileSpeed, bool isCritical, MonsterUnit projectileOwner, float targetHeight)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		target = null;
		defenderTarget = targetDefender;
		damage = projectileDamage;
		speed = projectileSpeed;
		critical = isCritical;
		splashRadius = 0f;
		splashDamageRatio = 0f;
		additionalPierceCount = 0;
		owner = null;
		monsterOwner = projectileOwner;
		monsterImpactHandler = null;
		hitEffectPrefab = null;
		defenderTargetHeight = Mathf.Max(0f, targetHeight);
		if ((Object)(object)defenderTarget != (Object)null)
		{
			Vector3 val = ((Component)defenderTarget).transform.position + Vector3.up * defenderTargetHeight;
			((Component)this).transform.rotation = RuntimeEffectUtility.FaceTowards(((Component)this).transform.position, val, ((Component)this).transform.rotation);
		}
	}

	private void Update()
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)defenderTarget != (Object)null)
		{
			UpdateDefenderProjectile();
			return;
		}
		if ((Object)(object)target == (Object)null || !target.CanBeCombatTargeted)
		{
			Recycle();
			return;
		}
		Vector3 val = ((Component)target).transform.position - ((Component)this).transform.position;
		Vector3 val2 = val;
		val2.y = 0f;
		if (((Vector3)(ref val2)).sqrMagnitude > 1E-06f)
		{
			((Component)this).transform.rotation = Quaternion.LookRotation(((Vector3)(ref val2)).normalized, Vector3.up);
		}
		((Component)this).transform.position = Vector3.MoveTowards(((Component)this).transform.position, ((Component)target).transform.position, speed * Time.deltaTime);
		if (!(Vector3.Distance(((Component)this).transform.position, ((Component)target).transform.position) <= 0.2f))
		{
			return;
		}
		MonsterUnit monsterUnit = target;
		RuntimeEffectUtility.PlayOneShot(hitEffectPrefab, ((Component)monsterUnit).transform.position, ((Component)this).transform.rotation);
		RuntimeAudioUtility.PlayHit();
		if (monsterImpactHandler != null)
		{
			monsterImpactHandler(monsterUnit);
		}
		else
		{
			monsterUnit.TakeDamage(damage, critical, owner);
			ApplySplashDamage(monsterUnit);
		}
		if (additionalPierceCount > 0)
		{
			additionalPierceCount--;
			target = FindNextTarget(monsterUnit);
			if ((Object)(object)target != (Object)null)
			{
				return;
			}
		}
		Recycle();
	}

	private void UpdateDefenderProjectile()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		if (!CombatRuntimeQuery.IsValidDefenderTarget(defenderTarget))
		{
			Recycle();
			return;
		}
		Vector3 val = ((Component)defenderTarget).transform.position + Vector3.up * defenderTargetHeight;
		Vector3 val2 = val - ((Component)this).transform.position;
		Vector3 val3 = val2;
		val3.y = 0f;
		if (((Vector3)(ref val3)).sqrMagnitude > 1E-06f)
		{
			((Component)this).transform.rotation = Quaternion.LookRotation(((Vector3)(ref val3)).normalized, Vector3.up);
		}
		((Component)this).transform.position = Vector3.MoveTowards(((Component)this).transform.position, val, speed * Time.deltaTime);
		Vector3 val4 = ((Component)this).transform.position - val;
		if (!(((Vector3)(ref val4)).sqrMagnitude > 0.04f))
		{
			DefenderUnit defenderUnit = defenderTarget;
			MonsterUnit monsterUnit = monsterOwner;
			float num = damage;
			bool flag = critical;
			Recycle();
			if ((Object)(object)monsterUnit != (Object)null)
			{
				monsterUnit.ResolveBasicAttackProjectileImpact(defenderUnit, num, flag);
			}
			else
			{
				defenderUnit.TakeDamage(num, flag, null);
			}
		}
	}

	private void Recycle()
	{
		if (recycling)
		{
			return;
		}
		recycling = true;
		target = null;
		defenderTarget = null;
		owner = null;
		monsterOwner = null;
		defenderTargetHeight = 0f;
		monsterImpactHandler = null;
		hitEffectPrefab = null;
		damage = 0f;
		splashRadius = 0f;
		splashDamageRatio = 0f;
		additionalPierceCount = 0;
		if ((Object)(object)sourcePrefab == (Object)null)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
			return;
		}
		((Component)this).gameObject.SetActive(false);
		if (!Pools.TryGetValue(sourcePrefab, out var value))
		{
			value = new Stack<Projectile>();
			Pools[sourcePrefab] = value;
		}
		if (value.Count >= 48)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
		else
		{
			value.Push(this);
		}
	}

	private void ApplySplashDamage(MonsterUnit primaryTarget)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		if (splashRadius <= 0f || splashDamageRatio <= 0f || (Object)(object)primaryTarget == (Object)null)
		{
			return;
		}
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if (!((Object)(object)monsterUnit == (Object)null) && !((Object)(object)monsterUnit == (Object)(object)primaryTarget) && monsterUnit.CanBeCombatTargeted && Vector3.Distance(((Component)primaryTarget).transform.position, ((Component)monsterUnit).transform.position) <= splashRadius)
			{
				monsterUnit.TakeDamage(damage * splashDamageRatio, critical: false, owner);
			}
		}
	}

	private MonsterUnit FindNextTarget(MonsterUnit previousTarget)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		MonsterUnit result = null;
		float num = float.MaxValue;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if (!((Object)(object)monsterUnit == (Object)null) && !((Object)(object)monsterUnit == (Object)(object)previousTarget) && monsterUnit.CanBeCombatTargeted)
			{
				float num2 = Vector3.Distance(((Component)this).transform.position, ((Component)monsterUnit).transform.position);
				if (num2 < num)
				{
					num = num2;
					result = monsterUnit;
				}
			}
		}
		return result;
	}
}
