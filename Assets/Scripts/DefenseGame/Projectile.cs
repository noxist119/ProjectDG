using UnityEngine;

namespace DefenseGame
{
    public class Projectile : MonoBehaviour
    {
        private MonsterUnit target;
        private float damage;
        private float speed;
        private bool critical;

        public void Initialize(MonsterUnit targetMonster, float projectileDamage, float projectileSpeed, bool isCritical)
        {
            target = targetMonster;
            damage = projectileDamage;
            speed = projectileSpeed;
            critical = isCritical;
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
                target.TakeDamage(damage, critical);
                Destroy(gameObject);
            }
        }
    }
}
