using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
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

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetPools()
		{
			Pools.Clear();
		}

		public static Projectile Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
		{
			if (prefab == null)
			{
				return null;
			}
			Projectile projectile = null;
			if (Pools.TryGetValue(prefab, out var pool))
			{
				while (pool.Count > 0 && projectile == null)
				{
					projectile = pool.Pop();
				}
			}
			if (projectile == null)
			{
				GameObject projectileObject = UnityEngine.Object.Instantiate(prefab, position, rotation);
				if (projectileObject == null)
				{
					return null;
				}
				projectile = projectileObject.GetComponent<Projectile>();
				if (projectile == null)
				{
					projectile = projectileObject.AddComponent<Projectile>();
				}
			}
			projectile.sourcePrefab = prefab;
			projectile.recycling = false;
			projectile.transform.SetPositionAndRotation(position, rotation);
			projectile.gameObject.SetActive(value: true);
			projectile.PrepareForSpawn();
			return projectile;
		}

		private void PrepareForSpawn()
		{
			if (cachedTrails == null)
			{
				cachedTrails = GetComponentsInChildren<TrailRenderer>(includeInactive: true);
			}
			for (int i = 0; i < cachedTrails.Length; i++)
			{
				if (cachedTrails[i] != null)
				{
					cachedTrails[i].Clear();
				}
			}
			if (cachedParticles == null)
			{
				cachedParticles = GetComponentsInChildren<ParticleSystem>(includeInactive: true);
			}
			for (int j = 0; j < cachedParticles.Length; j++)
			{
				if (!(cachedParticles[j] == null))
				{
					cachedParticles[j].Clear(withChildren: true);
					cachedParticles[j].Play(withChildren: true);
				}
			}
		}

		public void Initialize(MonsterUnit targetMonster, float projectileDamage, float projectileSpeed, bool isCritical, float projectileSplashRadius = 0f, float projectileSplashDamageRatio = 0f, int projectileAdditionalPierceCount = 0, DefenderUnit projectileOwner = null, Action<MonsterUnit> projectileMonsterImpactHandler = null, GameObject projectileHitEffectPrefab = null)
		{
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
			if (target != null)
			{
				base.transform.rotation = RuntimeEffectUtility.FaceTowards(base.transform.position, target.transform.position, base.transform.rotation);
			}
		}

		public void Initialize(DefenderUnit targetDefender, float projectileDamage, float projectileSpeed, bool isCritical, MonsterUnit projectileOwner, float targetHeight)
		{
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
			if (defenderTarget != null)
			{
				Vector3 targetPosition = defenderTarget.transform.position + Vector3.up * defenderTargetHeight;
				base.transform.rotation = RuntimeEffectUtility.FaceTowards(base.transform.position, targetPosition, base.transform.rotation);
			}
		}

		private void Update()
		{
			if (defenderTarget != null)
			{
				UpdateDefenderProjectile();
				return;
			}
			if (target == null || !target.CanBeCombatTargeted)
			{
				Recycle();
				return;
			}
			Vector3 movement = target.transform.position - base.transform.position;
			Vector3 facing = movement;
			facing.y = 0f;
			if (facing.sqrMagnitude > 1E-06f)
			{
				base.transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
			}
			base.transform.position = Vector3.MoveTowards(base.transform.position, target.transform.position, speed * Time.deltaTime);
			if (!(Vector3.Distance(base.transform.position, target.transform.position) <= 0.2f))
			{
				return;
			}
			MonsterUnit hitTarget = target;
			RuntimeEffectUtility.PlayOneShot(hitEffectPrefab, hitTarget.transform.position, base.transform.rotation);
			RuntimeAudioUtility.PlayHit();
			if (monsterImpactHandler != null)
			{
				monsterImpactHandler(hitTarget);
			}
			else
			{
				hitTarget.TakeDamage(damage, critical, owner);
				ApplySplashDamage(hitTarget);
			}
			if (additionalPierceCount > 0)
			{
				additionalPierceCount--;
				target = FindNextTarget(hitTarget);
				if (target != null)
				{
					return;
				}
			}
			Recycle();
		}

		private void UpdateDefenderProjectile()
		{
			if (!CombatRuntimeQuery.IsValidDefenderTarget(defenderTarget))
			{
				Recycle();
				return;
			}
			Vector3 targetPosition = defenderTarget.transform.position + Vector3.up * defenderTargetHeight;
			Vector3 movement = targetPosition - base.transform.position;
			Vector3 facing = movement;
			facing.y = 0f;
			if (facing.sqrMagnitude > 1E-06f)
			{
				base.transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
			}
			base.transform.position = Vector3.MoveTowards(base.transform.position, targetPosition, speed * Time.deltaTime);
			if (!((base.transform.position - targetPosition).sqrMagnitude > 0.04f))
			{
				DefenderUnit hitTarget = defenderTarget;
				MonsterUnit attacker = monsterOwner;
				float impactDamage = damage;
				bool impactCritical = critical;
				Recycle();
				if (attacker != null)
				{
					attacker.ResolveBasicAttackProjectileImpact(hitTarget, impactDamage, impactCritical);
				}
				else
				{
					hitTarget.TakeDamage(impactDamage, impactCritical, null);
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
			if (sourcePrefab == null)
			{
				UnityEngine.Object.Destroy(base.gameObject);
				return;
			}
			base.gameObject.SetActive(value: false);
			if (!Pools.TryGetValue(sourcePrefab, out var pool))
			{
				pool = new Stack<Projectile>();
				Pools[sourcePrefab] = pool;
			}
			if (pool.Count >= 48)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
			else
			{
				pool.Push(this);
			}
		}

		private void ApplySplashDamage(MonsterUnit primaryTarget)
		{
			if (splashRadius <= 0f || splashDamageRatio <= 0f || primaryTarget == null)
			{
				return;
			}
			IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
			for (int i = 0; i < monsters.Count; i++)
			{
				MonsterUnit monster = monsters[i];
				if (!(monster == null) && !(monster == primaryTarget) && monster.CanBeCombatTargeted && Vector3.Distance(primaryTarget.transform.position, monster.transform.position) <= splashRadius)
				{
					monster.TakeDamage(damage * splashDamageRatio, critical: false, owner);
				}
			}
		}

		private MonsterUnit FindNextTarget(MonsterUnit previousTarget)
		{
			IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
			MonsterUnit bestTarget = null;
			float bestDistance = float.MaxValue;
			for (int i = 0; i < monsters.Count; i++)
			{
				MonsterUnit monster = monsters[i];
				if (!(monster == null) && !(monster == previousTarget) && monster.CanBeCombatTargeted)
				{
					float distance = Vector3.Distance(base.transform.position, monster.transform.position);
					if (distance < bestDistance)
					{
						bestDistance = distance;
						bestTarget = monster;
					}
				}
			}
			return bestTarget;
		}
	}
}
