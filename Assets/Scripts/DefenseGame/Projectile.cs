using UnityEngine;
using System;

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
        }

        private void Update()
        {
            if (target == null || !target.CanBeCombatTargeted)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 direction = (target.transform.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            if (Vector3.Distance(transform.position, target.transform.position) <= 0.2f)
            {
                MonsterUnit hitTarget = target;
                RuntimeEffectUtility.PlayOneShot(hitEffectPrefab, hitTarget.transform.position, Quaternion.identity);
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

                Destroy(gameObject);
            }
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
