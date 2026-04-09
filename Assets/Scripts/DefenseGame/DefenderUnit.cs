using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenseGame
{
    public class DefenderUnit : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Renderer[] tintRenderers;

        private CharacterDefinition definition;
        private BoardSlot currentSlot;
        private FloatingCombatUI floatingUi;
        private UnitAnimationDriver animationDriver;
        private HitFlashFeedback hitFlashFeedback;
        private float currentHealth;
        private float currentMana;
        private float attackCooldown;
        private float attackSpeedBonus;
        private float critChanceBonus;
        private float attackRangeBonus;
        private float splashRadiusBonus;
        private float splashDamageRatioBonus;
        private float attackSpeedBuffTimer;
        private float critBuffTimer;
        private readonly List<MonsterUnit> monsters = new List<MonsterUnit>();
        private readonly Dictionary<string, float> skillCooldowns = new Dictionary<string, float>();
        private Quaternion defaultFacingRotation = Quaternion.identity;
        private bool hasDefaultFacing;

        public static event System.Action<DefenderUnit> OnDefenderSpawned;
        public static event System.Action<DefenderUnit> OnDefenderRemoved;

        public CharacterDefinition Definition => definition;
        public CharacterGrade Grade => definition != null ? definition.grade : CharacterGrade.Normal;
        public BoardSlot CurrentSlot => currentSlot;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => definition != null ? definition.stats.maxHealth : 0f;
        public float CurrentMana => currentMana;
        public float MaxMana => definition != null ? definition.stats.maxMana : 0f;
        public float CurrentAttackRange => definition != null ? GetEffectiveAttackRange() : 0f;

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

        public void ConfigureRuntimePieces(Projectile projectileTemplate, Transform launchPoint, Renderer[] renderers)
        {
            projectilePrefab = projectileTemplate;
            firePoint = launchPoint;
            tintRenderers = renderers;
        }

        public void AdoptRuntimeTemplate(DefenderUnit template)
        {
            if (template == null)
            {
                return;
            }

            projectilePrefab = template.projectilePrefab;
            if (firePoint == null)
            {
                Transform existingPoint = transform.Find("FirePoint");
                if (existingPoint != null)
                {
                    firePoint = existingPoint;
                }
                else
                {
                    GameObject firePointObject = new GameObject("FirePoint");
                    firePointObject.transform.SetParent(transform);
                    firePointObject.transform.localPosition = new Vector3(0f, 0.8f, 0.6f);
                    firePoint = firePointObject.transform;
                }
            }

            if (tintRenderers == null || tintRenderers.Length == 0)
            {
                tintRenderers = GetComponentsInChildren<Renderer>(true);
            }

            EnsureAnimationDriver();
            EnsureHitFlashFeedback();
            EnsureInteractionCollider();
        }

        private void Update()
        {
            if (definition == null)
            {
                return;
            }

            TickBuffs();
            TickSkillCooldowns();

            currentMana = Mathf.Min(MaxMana, currentMana + MaxMana * definition.stats.manaRegenPerSecondRate * Time.deltaTime);
            attackCooldown -= Time.deltaTime;
            floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
            animationDriver?.PlayMoving(false);

            if (TryCastSkill())
            {
                return;
            }

            MonsterUnit target = FindNearestTarget();
            if (target != null && attackCooldown <= 0f)
            {
                PerformAttack(target);
            }
            else if (target == null && !HasAnyLivingMonster())
            {
                ResetFacingToDefault();
                if (animationDriver == null || !animationDriver.IsLocked)
                {
                    animationDriver?.ForceIdle();
                }
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
            attackRangeBonus = 0f;
            splashRadiusBonus = 0f;
            splashDamageRatioBonus = 0f;
            attackSpeedBuffTimer = 0f;
            critBuffTimer = 0f;
            monsters.Clear();
            monsters.AddRange(FindObjectsOfType<MonsterUnit>());
            skillCooldowns.Clear();
            gameObject.name = definition.displayName + "_" + definition.grade;
            ApplyVisuals();
            EnsureAnimationDriver();
            EnsureHitFlashFeedback();
            EnsureInteractionCollider();
            floatingUi = FloatingCombatUI.Attach(transform, definition.displayName, definition.accentColor);
            floatingUi.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
            animationDriver?.PlaySpawn();
            OnDefenderSpawned?.Invoke(this);
        }

        public void SetSlot(BoardSlot slot)
        {
            currentSlot = slot;
            defaultFacingRotation = transform.rotation;
            hasDefaultFacing = true;
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
            currentMana = Mathf.Min(MaxMana, currentMana + MaxMana * definition.stats.manaGainWhenHitRate);
            hitFlashFeedback?.PlayHit(critical);
            floatingUi?.ShowDamage(damage, critical, false);
            floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
            floatingUi?.ShowDamage(amount, false, true);
            floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
        }

        public void PlayWinAnimation()
        {
            animationDriver?.PlayWin();
        }

        public void AddAttackRangeBonus(float amount)
        {
            attackRangeBonus += amount;
        }

        public void AddBasicAttackSplash(float radiusBonus, float damageRatioBonus)
        {
            splashRadiusBonus += radiusBonus;
            splashDamageRatioBonus += damageRatioBonus;
        }

        public void ResetFacingToDefault()
        {
            if (!hasDefaultFacing)
            {
                return;
            }

            transform.rotation = defaultFacingRotation;
        }

        private void PerformAttack(MonsterUnit target)
        {
            FaceTarget(target.transform.position);
            float effectiveAttackSpeed = Mathf.Max(0.2f, definition.stats.attackSpeed * (1f + attackSpeedBonus));
            attackCooldown = 1f / effectiveAttackSpeed;

            bool critical = Random.value <= Mathf.Clamp01(definition.stats.criticalChance + critChanceBonus);
            float damage = definition.stats.attackPower * (critical ? definition.stats.criticalDamageMultiplier : 1f);
            currentMana = Mathf.Min(MaxMana, currentMana + MaxMana * definition.stats.manaGainPerAttackRate);

            if (projectilePrefab == null)
            {
                animationDriver?.PlayAttack();
                target.TakeDamage(damage, critical);
                ApplyBasicAttackSplash(target, damage);
                return;
            }

            Transform launchPoint = firePoint != null ? firePoint : transform;
            Projectile projectile = Instantiate(projectilePrefab, launchPoint.position, Quaternion.identity);
            projectile.gameObject.SetActive(true);
            projectile.Initialize(
                target,
                damage,
                definition.stats.projectileSpeed,
                critical,
                GetBasicAttackSplashRadius(),
                GetBasicAttackSplashDamageRatio(),
                definition.attackBehavior != null ? definition.attackBehavior.additionalPierceCount : 0);
            animationDriver?.PlayAttack();
        }

        private bool TryCastSkill()
        {
            if (definition.skills == null || definition.skills.Count == 0)
            {
                return false;
            }

            if (currentMana < MaxMana)
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
                MonsterUnit target = FindNearestTarget();
                if (target != null)
                {
                    FaceTarget(target.transform.position);
                    target.TakeDamage(definition.stats.attackPower * skill.power, false);
                }
            }
            else if (skill.effectType == SkillEffectType.AreaDamage)
            {
                MonsterUnit[] areaTargets = FindObjectsOfType<MonsterUnit>();
                for (int i = 0; i < areaTargets.Length; i++)
                {
                    if (Vector3.Distance(transform.position, areaTargets[i].transform.position) <= skill.radius)
                    {
                        areaTargets[i].TakeDamage(definition.stats.attackPower * skill.power, false);
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
            else if (skill.effectType == SkillEffectType.ManaSurge)
            {
                currentMana = Mathf.Min(MaxMana, currentMana + MaxMana * skill.power);
            }
            else if (skill.effectType == SkillEffectType.MultiShot)
            {
                List<MonsterUnit> targets = GetNearestTargets(skill.hitCount);
                for (int i = 0; i < targets.Count; i++)
                {
                    targets[i].TakeDamage(definition.stats.attackPower * skill.power, false);
                }
            }
            else if (skill.effectType == SkillEffectType.Execute)
            {
                MonsterUnit target = FindNearestTarget();
                if (target != null)
                {
                    FaceTarget(target.transform.position);
                    float multiplier = target.CurrentHealth <= target.MaxHealth * 0.35f ? skill.power * 1.8f : skill.power;
                    target.TakeDamage(definition.stats.attackPower * multiplier, true);
                }
            }
            else if (skill.effectType == SkillEffectType.SummonRush)
            {
                MonsterUnit[] allTargets = FindObjectsOfType<MonsterUnit>();
                int hits = 0;
                for (int i = 0; i < allTargets.Length && hits < skill.hitCount; i++)
                {
                    allTargets[i].TakeDamage(definition.stats.attackPower * skill.power, false);
                    hits++;
                }
            }
            else if (skill.effectType == SkillEffectType.ShieldBreak)
            {
                MonsterUnit target = FindNearestTarget();
                if (target != null)
                {
                    FaceTarget(target.transform.position);
                    target.TakeDamage(definition.stats.attackPower * skill.power, false);
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

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
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
                if (distance <= GetEffectiveAttackRange() && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = monster;
                }
            }

            return bestTarget;
        }

        private List<MonsterUnit> GetNearestTargets(int count)
        {
            return monsters.Where(monster => monster != null)
                .OrderBy(monster => Vector3.Distance(transform.position, monster.transform.position))
                .Take(count)
                .ToList();
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

        private void EnsureInteractionCollider()
        {
            if (GetComponentInChildren<Collider>(true) != null)
            {
                return;
            }

            if (tintRenderers == null || tintRenderers.Length == 0)
            {
                tintRenderers = GetComponentsInChildren<Renderer>(true);
            }

            Bounds bounds = new Bounds(transform.position, Vector3.one);
            bool initialized = false;
            for (int i = 0; i < tintRenderers.Length; i++)
            {
                Renderer renderer = tintRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            BoxCollider collider = gameObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
            }

            Vector3 worldCenter = initialized ? bounds.center : transform.position + Vector3.up * 0.9f;
            Vector3 worldSize = initialized ? bounds.size : new Vector3(1f, 1.8f, 1f);
            collider.center = transform.InverseTransformPoint(worldCenter);
            collider.size = new Vector3(
                Mathf.Max(0.6f, worldSize.x),
                Mathf.Max(1.2f, worldSize.y),
                Mathf.Max(0.6f, worldSize.z));
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
            if (!HasAnyLivingMonster())
            {
                ResetFacingToDefault();
                if (animationDriver == null || !animationDriver.IsLocked)
                {
                    animationDriver?.ForceIdle();
                }
            }
        }

        private bool HasAnyLivingMonster()
        {
            for (int i = monsters.Count - 1; i >= 0; i--)
            {
                if (monsters[i] == null)
                {
                    monsters.RemoveAt(i);
                    continue;
                }

                return true;
            }

            return false;
        }

        private float GetEffectiveAttackRange()
        {
            float baseRange = definition.stats.attackRange;
            if (definition.attackBehavior != null)
            {
                baseRange = definition.attackBehavior.ResolveAttackRange(baseRange);
            }

            return Mathf.Max(0.5f, baseRange + attackRangeBonus);
        }

        private float GetBasicAttackSplashRadius()
        {
            float radius = definition.attackBehavior != null ? definition.attackBehavior.splashRadius : 0f;
            return Mathf.Max(0f, radius + splashRadiusBonus);
        }

        private float GetBasicAttackSplashDamageRatio()
        {
            float ratio = definition.attackBehavior != null ? definition.attackBehavior.splashDamageRatio : 0f;
            return Mathf.Clamp01(ratio + splashDamageRatioBonus);
        }

        private void ApplyBasicAttackSplash(MonsterUnit primaryTarget, float baseDamage)
        {
            float splashRadius = GetBasicAttackSplashRadius();
            float splashDamageRatio = GetBasicAttackSplashDamageRatio();
            if (primaryTarget == null || splashRadius <= 0f || splashDamageRatio <= 0f)
            {
                return;
            }

            MonsterUnit[] nearbyMonsters = FindObjectsOfType<MonsterUnit>();
            for (int i = 0; i < nearbyMonsters.Length; i++)
            {
                MonsterUnit monster = nearbyMonsters[i];
                if (monster == null || monster == primaryTarget)
                {
                    continue;
                }

                if (Vector3.Distance(primaryTarget.transform.position, monster.transform.position) <= splashRadius)
                {
                    monster.TakeDamage(baseDamage * splashDamageRatio, false);
                }
            }
        }
    }
}
