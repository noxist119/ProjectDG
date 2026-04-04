using UnityEngine;

namespace DefenseGame
{
    public class DefenderUnit : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private Projectile projectilePrefab;

        private CharacterDefinition definition;
        private BoardSlot currentSlot;
        private float currentHealth;
        private float currentMana;
        private float attackCooldown;
        private float attackSpeedBonus;
        private float critChanceBonus;
        private float attackSpeedBuffTimer;
        private float critBuffTimer;
        private readonly System.Collections.Generic.List<MonsterUnit> monsters = new System.Collections.Generic.List<MonsterUnit>();
        private readonly System.Collections.Generic.Dictionary<string, float> skillCooldowns = new System.Collections.Generic.Dictionary<string, float>();

        public static event System.Action<DefenderUnit> OnDefenderSpawned;
        public static event System.Action<DefenderUnit> OnDefenderRemoved;

        public CharacterDefinition Definition => definition;
        public CharacterGrade Grade => definition != null ? definition.grade : CharacterGrade.Normal;
        public BoardSlot CurrentSlot => currentSlot;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => definition != null ? definition.stats.maxHealth : 0f;
        public float CurrentMana => currentMana;
        public float MaxMana => definition != null ? definition.stats.maxMana : 0f;

        private void OnEnable()
        {
            MonsterUnit.OnMonsterSpawned += HandleMonsterSpawned;
            MonsterUnit.OnMonsterKilled += HandleMonsterRemoved;
            MonsterUnit.OnMonsterEscaped += HandleMonsterRemoved;
        }

        private void OnDisable()
        {
            MonsterUnit.OnMonsterSpawned -= HandleMonsterSpawned;
            MonsterUnit.OnMonsterKilled -= HandleMonsterRemoved;
            MonsterUnit.OnMonsterEscaped -= HandleMonsterRemoved;
        }

        private void Update()
        {
            if (definition == null)
            {
                return;
            }

            TickBuffs();
            TickSkillCooldowns();

            currentMana = Mathf.Min(MaxMana, currentMana + 6f * Time.deltaTime);
            attackCooldown -= Time.deltaTime;

            if (TryCastSkill())
            {
                return;
            }

            MonsterUnit target = FindNearestTarget();
            if (target != null && attackCooldown <= 0f)
            {
                PerformAttack(target);
            }
        }

        public void Initialize(CharacterDefinition newDefinition)
        {
            definition = newDefinition;
            currentHealth = definition.stats.maxHealth;
            currentMana = 0f;
            attackCooldown = 0f;
            attackSpeedBonus = 0f;
            critChanceBonus = 0f;
            attackSpeedBuffTimer = 0f;
            critBuffTimer = 0f;
            monsters.Clear();
            monsters.AddRange(FindObjectsOfType<MonsterUnit>());
            skillCooldowns.Clear();
            gameObject.name = $"{definition.displayName}_{definition.grade}";
            OnDefenderSpawned?.Invoke(this);
        }

        public void SetSlot(BoardSlot slot)
        {
            currentSlot = slot;
        }

        public void RemoveFromBoard()
        {
            if (currentSlot != null)
            {
                currentSlot.Clear();
                currentSlot = null;
            }
        }

        public void TakeDamage(float damage, bool critical)
        {
            currentHealth -= damage;
            currentMana = Mathf.Min(MaxMana, currentMana + damage * 0.35f);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        }

        private void PerformAttack(MonsterUnit target)
        {
            float effectiveAttackSpeed = Mathf.Max(0.2f, definition.stats.attackSpeed * (1f + attackSpeedBonus));
            attackCooldown = 1f / effectiveAttackSpeed;

            bool critical = Random.value <= Mathf.Clamp01(definition.stats.criticalChance + critChanceBonus);
            float damage = definition.stats.attackPower * (critical ? definition.stats.criticalDamageMultiplier : 1f);
            currentMana = Mathf.Min(MaxMana, currentMana + 12f);

            if (projectilePrefab == null)
            {
                target.TakeDamage(damage, critical);
                return;
            }

            Transform launchPoint = firePoint != null ? firePoint : transform;
            Projectile projectile = Instantiate(projectilePrefab, launchPoint.position, Quaternion.identity);
            projectile.Initialize(target, damage, definition.stats.projectileSpeed, critical);
        }

        private bool TryCastSkill()
        {
            if (definition.skills == null || definition.skills.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < definition.skills.Count; i++)
            {
                SkillDefinition skill = definition.skills[i];
                if (currentMana < skill.manaThreshold)
                {
                    continue;
                }

                if (skillCooldowns.TryGetValue(skill.id, out float cooldown) && cooldown > 0f)
                {
                    continue;
                }

                currentMana = Mathf.Max(0f, currentMana - skill.manaThreshold);
                CastSkill(skill);
                skillCooldowns[skill.id] = skill.cooldown;
                return true;
            }

            return false;
        }

        private void CastSkill(SkillDefinition skill)
        {
            switch (skill.effectType)
            {
                case SkillEffectType.DirectDamage:
                    MonsterUnit target = FindNearestTarget();
                    if (target != null)
                    {
                        target.TakeDamage(definition.stats.attackPower * skill.power, false);
                    }
                    break;

                case SkillEffectType.AreaDamage:
                    MonsterUnit[] areaTargets = FindObjectsOfType<MonsterUnit>();
                    for (int i = 0; i < areaTargets.Length; i++)
                    {
                        if (Vector3.Distance(transform.position, areaTargets[i].transform.position) <= skill.radius)
                        {
                            areaTargets[i].TakeDamage(definition.stats.attackPower * skill.power, false);
                        }
                    }
                    break;

                case SkillEffectType.HealSelf:
                    Heal(MaxHealth * skill.power);
                    break;

                case SkillEffectType.AttackSpeedBoost:
                    attackSpeedBonus = skill.power;
                    attackSpeedBuffTimer = skill.duration;
                    break;

                case SkillEffectType.CriticalBoost:
                    critChanceBonus = skill.power;
                    critBuffTimer = skill.duration;
                    break;

                case SkillEffectType.ManaSurge:
                    currentMana = Mathf.Min(MaxMana, currentMana + MaxMana * skill.power);
                    break;
            }
        }

        private MonsterUnit FindNearestTarget()
        {
            MonsterUnit bestTarget = null;
            float bestDistance = float.MaxValue;

            for (int i = monsters.Count - 1; i >= 0; i--)
            {
                MonsterUnit monster = monsters[i];
                if (monster == null)
                {
                    monsters.RemoveAt(i);
                    continue;
                }

                float distance = Vector3.Distance(transform.position, monster.transform.position);
                if (distance <= definition.stats.attackRange && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = monster;
                }
            }

            return bestTarget;
        }

        private void TickBuffs()
        {
            if (attackSpeedBuffTimer > 0f)
            {
                attackSpeedBuffTimer -= Time.deltaTime;
                if (attackSpeedBuffTimer <= 0f)
                {
                    attackSpeedBonus = 0f;
                }
            }

            if (critBuffTimer > 0f)
            {
                critBuffTimer -= Time.deltaTime;
                if (critBuffTimer <= 0f)
                {
                    critChanceBonus = 0f;
                }
            }
        }

        private void TickSkillCooldowns()
        {
            if (definition.skills == null)
            {
                return;
            }

            for (int i = 0; i < definition.skills.Count; i++)
            {
                SkillDefinition skill = definition.skills[i];
                if (!skillCooldowns.ContainsKey(skill.id))
                {
                    continue;
                }

                skillCooldowns[skill.id] = Mathf.Max(0f, skillCooldowns[skill.id] - Time.deltaTime);
            }
        }

        private void Die()
        {
            RemoveFromBoard();
            OnDefenderRemoved?.Invoke(this);
            Destroy(gameObject);
        }

        private void HandleMonsterSpawned(MonsterUnit monster)
        {
            if (monster != null && !monsters.Contains(monster))
            {
                monsters.Add(monster);
            }
        }

        private void HandleMonsterRemoved(MonsterUnit monster)
        {
            monsters.Remove(monster);
        }
    }
}

