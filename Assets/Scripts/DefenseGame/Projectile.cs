using UnityEngine;

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

        public void Initialize(MonsterUnit targetMonster, float projectileDamage, float projectileSpeed, bool isCritical, float projectileSplashRadius = 0f, float projectileSplashDamageRatio = 0f, int projectileAdditionalPierceCount = 0)
        {
            target = targetMonster;
            damage = projectileDamage;
            speed = projectileSpeed;
            critical = isCritical;
            splashRadius = projectileSplashRadius;
            splashDamageRatio = projectileSplashDamageRatio;
            additionalPierceCount = projectileAdditionalPierceCount;
        }

        private void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 direction = (target.transform.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            if (Vector3.Distance(transform.position, target.transform.position) <= 0.2f)
            {
                MonsterUnit hitTarget = target;
                hitTarget.TakeDamage(damage, critical);
                ApplySplashDamage(hitTarget);
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

            MonsterUnit[] monsters = FindObjectsOfType<MonsterUnit>();
            for (int i = 0; i < monsters.Length; i++)
            {
                MonsterUnit monster = monsters[i];
                if (monster == null || monster == primaryTarget)
                {
                    continue;
                }

                if (Vector3.Distance(primaryTarget.transform.position, monster.transform.position) <= splashRadius)
                {
                    monster.TakeDamage(damage * splashDamageRatio, false);
                }
            }
        }

        private MonsterUnit FindNextTarget(MonsterUnit previousTarget)
        {
            MonsterUnit[] monsters = FindObjectsOfType<MonsterUnit>();
            MonsterUnit bestTarget = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < monsters.Length; i++)
            {
                MonsterUnit monster = monsters[i];
                if (monster == null || monster == previousTarget)
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
