using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenseGame
{
    public class MonsterUnit : MonoBehaviour
    {
        [SerializeField] private Renderer[] tintRenderers;
        [SerializeField] private float facingOffsetY = 0f;
        [SerializeField] private float separationRadius = 0.95f;
        [SerializeField] private float separationStrength = 1.1f;
        [SerializeField] private float globalMoveSpeedMultiplier = 0.7f;
        [SerializeField] private GameObject deathEffectPrefab;
        [SerializeField] private Vector3 deathEffectOffset = new Vector3(0f, 0.6f, 0f);

        private MonsterDefinition definition;
        private Transform goal;
        private Vector3 laneGoalPosition;
        private FloatingCombatUI floatingUi;
        private UnitAnimationDriver animationDriver;
        private HitFlashFeedback hitFlashFeedback;
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
        public float CurrentAttackRange => definition != null ? GetEffectiveAttackRange() : 0f;

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

        public void AdoptRuntimeTemplate(MonsterUnit template)
        {
            if (template == null)
            {
                return;
            }

            if (deathEffectPrefab == null)
            {
                deathEffectPrefab = template.deathEffectPrefab;
            }

            if (tintRenderers == null || tintRenderers.Length == 0)
            {
                tintRenderers = GetComponentsInChildren<Renderer>(true);
            }

            EnsureAnimationDriver();
            EnsureHitFlashFeedback();
        }

        public void ConfigureRuntimePieces(GameObject deathEffectTemplate, Renderer[] renderers)
        {
            deathEffectPrefab = deathEffectTemplate;
            tintRenderers = renderers;
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

            currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * definition.stats.manaRegenPerSecondRate * Time.deltaTime);
            attackCooldown -= Time.deltaTime;
            floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);

            if (TryCastSkill())
            {
                return;
            }

            DefenderUnit target = FindNearestDefender();
            if (target != null)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance <= GetEffectiveAttackRange())
                {
                    if (attackCooldown <= 0f)
                    {
                        PerformAttack(target);
                    }

                    animationDriver?.PlayMoving(false);
                    return;
                }
            }

            MoveTowardsGoal();
        }

        public void Initialize(MonsterDefinition newDefinition, Transform goalPoint)
        {
            definition = newDefinition;
            goal = goalPoint;
            laneGoalPosition = goal != null
                ? new Vector3(transform.position.x, goal.position.y, goal.position.z)
                : transform.position;
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
            EnsureAnimationDriver();
            EnsureHitFlashFeedback();
            floatingUi = FloatingCombatUI.Attach(transform, definition.displayName, definition.accentColor);
            floatingUi.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
            animationDriver?.PlaySpawn();
            OnMonsterSpawned?.Invoke(this);
        }

        public void TakeDamage(float damage, bool critical)
        {
            currentHealth -= damage;
            currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * definition.stats.manaGainWhenHitRate);
            hitFlashFeedback?.PlayHit(critical);
            floatingUi?.ShowDamage(damage, critical, false);
            floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);

            if (currentHealth <= 0f)
            {
                PlayDeathEffect();
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
            floatingUi?.ShowDamage(amount, false, true);
            floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
        }

        private void MoveTowardsGoal()
        {
            if (goal == null)
            {
                return;
            }

            Vector3 moveTarget = BuildMoveTarget();
            FaceTarget(moveTarget);
            float moveSpeed = definition.stats.moveSpeed * (1f + moveSpeedBonus) * globalMoveSpeedMultiplier;
            transform.position = Vector3.MoveTowards(transform.position, moveTarget, moveSpeed * Time.deltaTime);
            animationDriver?.PlayMoving(true);

            if (Vector3.Distance(transform.position, laneGoalPosition) <= 0.05f)
            {
                OnMonsterEscaped?.Invoke(this);
                Destroy(gameObject);
            }
        }

        private void PerformAttack(DefenderUnit target)
        {
            FaceTarget(target.transform.position);
            float effectiveAttackSpeed = Mathf.Max(0.2f, definition.stats.attackSpeed * (1f + attackSpeedBonus));
            attackCooldown = 1f / effectiveAttackSpeed;

            bool critical = Random.value <= Mathf.Clamp01(definition.stats.criticalChance + critChanceBonus);
            float damage = definition.stats.attackPower * (critical ? definition.stats.criticalDamageMultiplier : 1f);
            currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * definition.stats.manaGainPerAttackRate);
            animationDriver?.PlayAttack();
            target.TakeDamage(damage, critical);
            ApplyBasicAttackExtensions(target, damage);
        }

        private bool TryCastSkill()
        {
            if (definition.skills == null || definition.skills.Count == 0)
            {
                return false;
            }

            if (currentMana < definition.stats.maxMana)
            {
                return false;
            }

            for (int i = 0; i < definition.skills.Count; i++)
            {
                SkillDefinition skill = definition.skills[i];
                if (skillCooldowns.TryGetValue(skill.id, out float cooldown) && cooldown > 0f)
                {
                    continue;
                }

                currentMana = 0f;
                CastSkill(skill);
                skillCooldowns[skill.id] = skill.cooldown;
                return true;
            }

            return false;
        }

        private void CastSkill(SkillDefinition skill)
        {
            animationDriver?.PlaySkill();
            if (skill.effectType == SkillEffectType.DirectDamage)
            {
                DefenderUnit singleTarget = FindNearestDefender();
                if (singleTarget != null)
                {
                    FaceTarget(singleTarget.transform.position);
                    singleTarget.TakeDamage(definition.stats.attackPower * skill.power, false);
                }
            }
            else if (skill.effectType == SkillEffectType.AreaDamage)
            {
                DefenderUnit[] targets = FindObjectsOfType<DefenderUnit>();
                if (targets.Length > 0)
                {
                    FaceTarget(targets[0].transform.position);
                }
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

                if (targets.Count > 0)
                {
                    FaceTarget(targets[0].transform.position);
                }

                for (int i = 0; i < targets.Count; i++)
                {
                    targets[i].TakeDamage(definition.stats.attackPower * skill.power, false);
                }
            }
        }

        private void FaceTarget(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = lookRotation * Quaternion.Euler(0f, facingOffsetY, 0f);
        }

        private Vector3 BuildMoveTarget()
        {
            Vector3 target = laneGoalPosition;
            Vector3 separation = Vector3.zero;
            MonsterUnit[] others = FindObjectsOfType<MonsterUnit>();

            for (int i = 0; i < others.Length; i++)
            {
                MonsterUnit other = others[i];
                if (other == null || other == this)
                {
                    continue;
                }

                Vector3 delta = transform.position - other.transform.position;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance <= 0.0001f || distance > separationRadius)
                {
                    continue;
                }

                separation += delta.normalized * ((separationRadius - distance) / separationRadius);
            }

            if (separation != Vector3.zero)
            {
                target += separation.normalized * separationStrength;
            }

            target.x = Mathf.Clamp(target.x, laneGoalPosition.x - 0.6f, laneGoalPosition.x + 0.6f);
            return target;
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

        private void EnsureAnimationDriver()
        {
            if (animationDriver == null)
            {
                animationDriver = GetComponent<UnitAnimationDriver>();
                if (animationDriver == null)
                {
                    animationDriver = gameObject.AddComponent<UnitAnimationDriver>();
                }
            }
        }

        private void EnsureHitFlashFeedback()
        {
            if (hitFlashFeedback == null)
            {
                hitFlashFeedback = GetComponent<HitFlashFeedback>();
                if (hitFlashFeedback == null)
                {
                    hitFlashFeedback = gameObject.AddComponent<HitFlashFeedback>();
                }
            }

            hitFlashFeedback.Configure(tintRenderers);
        }

        private void PlayDeathEffect()
        {
            if (deathEffectPrefab == null)
            {
                return;
            }

            GameObject effect = Instantiate(deathEffectPrefab, transform.position + deathEffectOffset, Quaternion.identity);
            effect.SetActive(true);
            Destroy(effect, ResolveEffectLifetime(effect));
        }

        private float ResolveEffectLifetime(GameObject effect)
        {
            float lifetime = 3f;
            ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems[i];
                ParticleSystem.MainModule main = particleSystem.main;
                float duration = main.duration;
                if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                {
                    duration += Mathf.Max(main.startLifetime.constantMin, main.startLifetime.constantMax);
                }
                else if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                {
                    duration += main.startLifetime.constant;
                }
                else
                {
                    duration += 1f;
                }

                lifetime = Mathf.Max(lifetime, duration + 0.5f);
            }

            return lifetime;
        }

        private float GetEffectiveAttackRange()
        {
            if (definition == null)
            {
                return 0f;
            }

            return definition.attackBehavior.ResolveAttackRange(definition.stats.attackRange);
        }

        private void ApplyBasicAttackExtensions(DefenderUnit primaryTarget, float damage)
        {
            if (primaryTarget == null || definition == null)
            {
                return;
            }

            float splashRadius = definition.attackBehavior.splashRadius;
            float splashDamageRatio = definition.attackBehavior.splashDamageRatio;
            int additionalPierceCount = Mathf.Max(0, definition.attackBehavior.additionalPierceCount);

            if (splashRadius > 0f && splashDamageRatio > 0f)
            {
                DefenderUnit[] allDefenders = FindObjectsOfType<DefenderUnit>();
                for (int i = 0; i < allDefenders.Length; i++)
                {
                    DefenderUnit defender = allDefenders[i];
                    if (defender == null || defender == primaryTarget)
                    {
                        continue;
                    }

                    if (Vector3.Distance(primaryTarget.transform.position, defender.transform.position) <= splashRadius)
                    {
                        defender.TakeDamage(damage * splashDamageRatio, false);
                    }
                }
            }

            if (additionalPierceCount <= 0)
            {
                return;
            }

            List<DefenderUnit> additionalTargets = defenders
                .Where(defender => defender != null && defender != primaryTarget)
                .OrderBy(defender => Vector3.Distance(transform.position, defender.transform.position))
                .Take(additionalPierceCount)
                .ToList();

            for (int i = 0; i < additionalTargets.Count; i++)
            {
                additionalTargets[i].TakeDamage(damage, false);
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
