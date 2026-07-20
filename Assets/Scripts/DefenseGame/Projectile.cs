using UnityEngine;
using System;
using System.Collections.Generic;

namespace DefenseGame
{
    public class Projectile : MonoBehaviour
    {
        private MonsterUnit target;
        private float damage;
        private float speed;
        private bool critical;
        private float splashRadius;
        private float splashDamageRatio;
        private int additionalPierceCount;
        private DefenderUnit owner;
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
            if (Pools.TryGetValue(prefab, out Stack<Projectile> pool))
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
            projectile.gameObject.SetActive(true);
            projectile.PrepareForSpawn();
            return projectile;
        }

        private void PrepareForSpawn()
        {
            if (cachedTrails == null)
            {
                cachedTrails = GetComponentsInChildren<TrailRenderer>(true);
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
                cachedParticles = GetComponentsInChildren<ParticleSystem>(true);
            }

            for (int i = 0; i < cachedParticles.Length; i++)
            {
                if (cachedParticles[i] == null)
                {
                    continue;
                }

                cachedParticles[i].Clear(true);
                cachedParticles[i].Play(true);
            }
        }

        public void Initialize(MonsterUnit targetMonster, float projectileDamage, float projectileSpeed, bool isCritical, float projectileSplashRadius = 0f, float projectileSplashDamageRatio = 0f, int projectileAdditionalPierceCount = 0, DefenderUnit projectileOwner = null, Action<MonsterUnit> projectileMonsterImpactHandler = null, GameObject projectileHitEffectPrefab = null)
        {
            target = targetMonster;
            damage = projectileDamage;
            speed = projectileSpeed;
            critical = isCritical;
            splashRadius = projectileSplashRadius;
            splashDamageRatio = projectileSplashDamageRatio;
            additionalPierceCount = projectileAdditionalPierceCount;
            owner = projectileOwner;
            monsterImpactHandler = projectileMonsterImpactHandler;
            hitEffectPrefab = projectileHitEffectPrefab;
            if (target != null)
            {
                transform.rotation = RuntimeEffectUtility.FaceTowards(transform.position, target.transform.position, transform.rotation);
            }
        }

        private void Update()
        {
            if (target == null || !target.CanBeCombatTargeted)
            {
                Recycle();
                return;
            }

            Vector3 movement = target.transform.position - transform.position;
            Vector3 facing = movement;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.000001f)
            {
                transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
            }
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.transform.position) <= 0.2f)
            {
                MonsterUnit hitTarget = target;
                RuntimeEffectUtility.PlayOneShot(hitEffectPrefab, hitTarget.transform.position, transform.rotation);
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
        }

        private void Recycle()
        {
            if (recycling)
            {
                return;
            }

            recycling = true;
            target = null;
            owner = null;
            monsterImpactHandler = null;
            hitEffectPrefab = null;
            damage = 0f;
            splashRadius = 0f;
            splashDamageRatio = 0f;
            additionalPierceCount = 0;

            if (sourcePrefab == null)
            {
                Destroy(gameObject);
                return;
            }

            gameObject.SetActive(false);
            if (!Pools.TryGetValue(sourcePrefab, out Stack<Projectile> pool))
            {
                pool = new Stack<Projectile>();
                Pools[sourcePrefab] = pool;
            }

            if (pool.Count >= MaxPoolPerPrefab)
            {
                Destroy(gameObject);
                return;
            }

            pool.Push(this);
        }

        private void ApplySplashDamage(MonsterUnit primaryTarget)
        {
            if (splashRadius <= 0f || splashDamageRatio <= 0f || primaryTarget == null)
            {
                return;
            }

            var monsters = MonsterUnit.ActiveInstances;
            for (int i = 0; i < monsters.Count; i++)
            {
                MonsterUnit monster = monsters[i];
                if (monster == null || monster == primaryTarget || !monster.CanBeCombatTargeted)
                {
                    continue;
                }

                if (Vector3.Distance(primaryTarget.transform.position, monster.transform.position) <= splashRadius)
                {
                    monster.TakeDamage(damage * splashDamageRatio, false, owner);
                }
            }
        }

        private MonsterUnit FindNextTarget(MonsterUnit previousTarget)
        {
            var monsters = MonsterUnit.ActiveInstances;
            MonsterUnit bestTarget = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < monsters.Count; i++)
            {
                MonsterUnit monster = monsters[i];
                if (monster == null || monster == previousTarget || !monster.CanBeCombatTargeted)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, monster.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = monster;
                }
            }

            return bestTarget;
        }
    }
}
