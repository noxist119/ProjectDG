using UnityEngine;

namespace DefenseGame
{
    public class MonsterUnit : MonoBehaviour
    {
        private MonsterDefinition definition;
        private Transform goal;
        private float currentHealth;
        private float currentMana;
        private float attackCooldown;
        private float attackSpeedBonus;
        private float critChanceBonus;
        private float moveSpeedBonus;
        private float attackSpeedBuffTimer;
        private float critBuffTimer;
        private float moveSpeedBuffTimer;
        private readonly System.Collections.Generic.List<DefenderUnit> defenders = new System.Collections.Generic.List<DefenderUnit>();
        private readonly System.Collections.Generic.Dictionary<string, float> skillCooldowns = new System.Collections.Generic.Dictionary<string, float>();

        public static event System.Action<MonsterUnit> OnMonsterSpawned;
        public static event System.Action<MonsterUnit> OnMonsterKilled;
        public static event System.Action<MonsterUnit> OnMonsterEscaped;

        public MonsterDefinition Definition => definition;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => definition != null ? definition.stats.maxHealth : 0f;
        public float CurrentMana => currentMana;
        public bool IsBoss => definition != null && definition.isBoss;

        private void OnEnable()
        {
            DefenderUnit.OnDefenderSpawned += HandleDefenderSpawned;
            DefenderUnit.OnDefenderRemoved += HandleDefenderRemoved;
        }

        private void OnDisable()
        {
            DefenderUnit.OnDefenderSpawned -= HandleDefenderSpawned;
            DefenderUnit.OnDefenderRemoved -= HandleDefenderRemoved;
        }

        private void Update()
        {
            if (definition == null)
            {
                return;
            }

            TickBuffs();
            TickSkillCooldowns();

            currentMana = Mathf.Min(definition.stats.maxMana, currentMana + 5f * Time.deltaTime);
            attackCooldown -= Time.deltaTime;

            if (TryCastSkill())
            {
                return;
            }

            DefenderUnit target = FindNearestDefender();
            if (target != null)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance <= definition.stats.attackRange)
                {
                    if (attackCooldown <= 0f)
                    {
                        PerformAttack(target);
                    }

                    return;
                }
            }

            MoveTowardsGoal();
        }

        public void Initialize(MonsterDefinition newDefinition, Transform goalPoint)
        {
            definition = newDefinition;
            goal = goalPoint;
            currentHealth = definition.stats.maxHealth;
            currentMana = 0f;
            attackCooldown = 0f;
            attackSpeedBonus = 0f;
            critChanceBonus = 0f;
            moveSpeedBonus = 0f;
            attackSpeedBuffTimer = 0f;
            critBuffTimer = 0f;
            moveSpeedBuffTimer = 0f;
            defenders.Clear();
            defenders.AddRange(FindObjectsOfType<DefenderUnit>());
            skillCooldowns.Clear();
            gameObject.name = definition.displayName;
            OnMonsterSpawned?.Invoke(this);
        }

        public void TakeDamage(float damage, bool critical)
        {
            currentHealth -= damage;
            currentMana = Mathf.Min(definition.stats.maxMana, currentMana + damage * 0.25f);

            if (currentHealth <= 0f)
            {
                OnMonsterKilled?.Invoke(this);
                Destroy(gameObject);
            }
        }

        public int GetRewardGold()
        {
            return definition != null ? definition.rewardGold : 0;
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        }

        private void MoveTowardsGoal()
        {
            if (goal == null)
            {
                return;
            }

            float moveSpeed = definition.stats.moveSpeed * (1f + moveSpeedBonus);
            transform.position = Vector3.MoveTowards(transform.position, goal.position, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, goal.position) <= 0.05f)
            {
                OnMonsterEscaped?.Invoke(this);
                Destroy(gameObject);
            }
        }

        private void PerformAttack(DefenderUnit target)
        {
            float effectiveAttackSpeed = Mathf.Max(0.2f, definition.stats.attackSpeed * (1f + attackSpeedBonus));
            attackCooldown = 1f / effectiveAttackSpeed;

            bool critical = Random.value <= Mathf.Clamp01(definition.stats.criticalChance + critChanceBonus);
            float damage = definition.stats.attackPower * (critical ? definition.stats.criticalDamageMultiplier : 1f);
            currentMana = Mathf.Min(definition.stats.maxMana, currentMana + 10f);
            target.TakeDamage(damage, critical);
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
                    DefenderUnit singleTarget = FindNearestDefender();
                    if (singleTarget != null)
                    {
                        singleTarget.TakeDamage(definition.stats.attackPower * skill.power, false);
                    }
                    break;

                case SkillEffectType.AreaDamage:
                    DefenderUnit[] targets = FindObjectsOfType<DefenderUnit>();
                    for (int i = 0; i < targets.Length; i++)
                    {
                        if (Vector3.Distance(transform.position, targets[i].transform.position) <= skill.radius)
                        {
                            targets[i].TakeDamage(definition.stats.attackPower * skill.power, false);
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

                case SkillEffectType.MoveSpeedBoost:
                    moveSpeedBonus = skill.power;
                    moveSpeedBuffTimer = skill.duration;
                    break;

                case SkillEffectType.ManaSurge:
                    currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * skill.power);
                    break;
            }
        }

        private DefenderUnit FindNearestDefender()
        {
            DefenderUnit bestTarget = null;
            float bestDistance = float.MaxValue;

            for (int i = defenders.Count - 1; i >= 0; i--)
            {
                DefenderUnit defender = defenders[i];
                if (defender == null)
                {
                    defenders.RemoveAt(i);
                    continue;
                }

                float distance = Vector3.Distance(transform.position, defender.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = defender;
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

            if (moveSpeedBuffTimer > 0f)
            {
                moveSpeedBuffTimer -= Time.deltaTime;
                if (moveSpeedBuffTimer <= 0f)
                {
                    moveSpeedBonus = 0f;
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

        private void HandleDefenderSpawned(DefenderUnit defender)
        {
            if (defender != null && !defenders.Contains(defender))
            {
                defenders.Add(defender);
            }
        }

        private void HandleDefenderRemoved(DefenderUnit defender)
        {
            defenders.Remove(defender);
        }
    }
}

