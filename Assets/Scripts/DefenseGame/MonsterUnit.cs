using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenseGame
{
    public class MonsterUnit : MonoBehaviour
    {
        [SerializeField] private Renderer[] tintRenderers;

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
        private bool enraged;
        private readonly List<DefenderUnit> defenders = new List<DefenderUnit>();
        private readonly Dictionary<string, float> skillCooldowns = new Dictionary<string, float>();

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
            TickBossPhase();

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
            enraged = false;
            defenders.Clear();
            defenders.AddRange(FindObjectsOfType<DefenderUnit>());
            skillCooldowns.Clear();
            gameObject.name = definition.displayName;
            ApplyVisuals();
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
            if (skill.effectType == SkillEffectType.DirectDamage)
            {
                DefenderUnit singleTarget = FindNearestDefender();
                if (singleTarget != null)
                {
                    singleTarget.TakeDamage(definition.stats.attackPower * skill.power, false);
                }
            }
            else if (skill.effectType == SkillEffectType.AreaDamage)
            {
                DefenderUnit[] targets = FindObjectsOfType<DefenderUnit>();
                for (int i = 0; i < targets.Length; i++)
                {
                    if (Vector3.Distance(transform.position, targets[i].transform.position) <= skill.radius)
                    {
                        targets[i].TakeDamage(definition.stats.attackPower * skill.power, false);
                    }
                }
            }
            else if (skill.effectType == SkillEffectType.HealSelf)
            {
                Heal(MaxHealth * skill.power);
            }
            else if (skill.effectType == SkillEffectType.AttackSpeedBoost)
            {
                attackSpeedBonus = skill.power;
                attackSpeedBuffTimer = skill.duration;
            }
            else if (skill.effectType == SkillEffectType.CriticalBoost)
            {
                critChanceBonus = skill.power;
                critBuffTimer = skill.duration;
            }
            else if (skill.effectType == SkillEffectType.MoveSpeedBoost)
            {
                moveSpeedBonus = skill.power;
                moveSpeedBuffTimer = skill.duration;
            }
            else if (skill.effectType == SkillEffectType.ManaSurge)
            {
                currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * skill.power);
            }
            else if (skill.effectType == SkillEffectType.SummonRush)
            {
                List<DefenderUnit> targets = defenders.Where(defender => defender != null)
                    .OrderBy(defender => Vector3.Distance(transform.position, defender.transform.position))
                    .Take(skill.hitCount)
                    .ToList();

                for (int i = 0; i < targets.Count; i++)
                {
                    targets[i].TakeDamage(definition.stats.attackPower * skill.power, false);
                }
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

        private void TickBossPhase()
        {
            if (!IsBoss || enraged)
            {
                return;
            }

            if (currentHealth <= MaxHealth * 0.5f)
            {
                enraged = true;
                attackSpeedBonus += 0.35f;
                moveSpeedBonus += 0.2f;
                critChanceBonus += 0.12f;
            }
        }

        private void ApplyVisuals()
        {
            if (tintRenderers == null || tintRenderers.Length == 0)
            {
                tintRenderers = GetComponentsInChildren<Renderer>(true);
            }

            for (int i = 0; i < tintRenderers.Length; i++)
            {
                if (tintRenderers[i] != null && tintRenderers[i].material != null)
                {
                    tintRenderers[i].material.color = definition.accentColor;
                }
            }

            transform.localScale = IsBoss ? Vector3.one * 1.7f : Vector3.one;
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
