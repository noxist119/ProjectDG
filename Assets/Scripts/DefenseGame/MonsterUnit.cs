using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenseGame
{
    public class MonsterUnit : MonoBehaviour
    {
        private static readonly List<MonsterUnit> ActiveMonsters = new List<MonsterUnit>();
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static Material defaultPetrifyMaterial;
        private static Material fallbackPetrifyMaterial;

        [SerializeField] private Renderer[] tintRenderers;
        [SerializeField] private float facingOffsetY = 0f;
        [SerializeField] private float separationRadius = 0.74f;
        [SerializeField] private float separationStrength = 0.9f;
        [SerializeField] private float globalMoveSpeedMultiplier = 0.7f;
        [SerializeField] private GameObject deathEffectPrefab;
        [SerializeField] private Vector3 deathEffectOffset = new Vector3(0f, 0.6f, 0f);
        [SerializeField] private float bossDeathPresentationDelay = 1.35f;
        [SerializeField] private float bossDeathPulseRadius = 2.25f;

        private MonsterDefinition definition;
        private Transform goal;
        private Vector3 laneGoalPosition;
        private FloatingCombatUI floatingUi;
        private UnitAnimationDriver animationDriver;
        private HitFlashFeedback hitFlashFeedback;
        private DefenseGameController gameController;
        private float currentHealth;
        private float outgameHealthMultiplier = 1f;
        private float outgameAttackMultiplier = 1f;
        private float currentMana;
        private float attackCooldown;
        private float attackSpeedBonus;
        private float critChanceBonus;
        private float moveSpeedBonus;
        private float attackSpeedSlowRatio;
        private float attackSpeedSlowTimer;
        private float attackSpeedBuffTimer;
        private float critBuffTimer;
        private float moveSpeedBuffTimer;
        private float slowRatio;
        private float slowTimer;
        private float stunTimer;
        private float petrifyTimer;
        private DefenderUnit tauntTarget;
        private float tauntTimer;
        private bool enraged;
        private bool roleTraitTriggered;
        private bool skillCastLocked;
        private MaterialPropertyBlock visualPropertyBlock;
        private GameObject bossReadabilityMarker;
        private UnitAnimationDriver subscribedAnimationDriver;
        private PendingMonsterAttack pendingBasicAttack;
        private PendingMonsterSkill pendingSkillCast;
        private Coroutine pendingAttackImpactRoutine;
        private Coroutine pendingSkillImpactRoutine;
        private int impactSequence;
        private bool isDying;
        private readonly List<DefenderUnit> defenders = new List<DefenderUnit>();
        private readonly Dictionary<string, float> skillCooldowns = new Dictionary<string, float>();
        private readonly List<RendererMaterialSnapshot> petrifyMaterialSnapshots = new List<RendererMaterialSnapshot>();
        private readonly List<AnimatorSpeedSnapshot> petrifyAnimatorSnapshots = new List<AnimatorSpeedSnapshot>();

        public static event System.Action<MonsterUnit> OnMonsterSpawned;
        public static event System.Action<MonsterUnit> OnMonsterKilled;
        public static event System.Action<MonsterUnit> OnMonsterEscaped;

        public static int ActiveCount
        {
            get
            {
                PruneMissingActiveMonsters();
                return ActiveMonsters.Count;
            }
        }

        public static IReadOnlyList<MonsterUnit> ActiveInstances
        {
            get
            {
                PruneMissingActiveMonsters();
                return ActiveMonsters;
            }
        }

        public static void ConfigurePetrifyMaterial(Material material)
        {
            defaultPetrifyMaterial = material;
        }

        public static int ApplyPetrifyLine(Vector3 origin, Vector3 direction, float length, float halfWidth, PetrifyTargetOptions options)
        {
            if (options.duration <= 0f)
            {
                return 0;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.forward;
            }

            direction.Normalize();
            float checkedLength = Mathf.Max(0.1f, length);
            float checkedHalfWidth = Mathf.Max(0.05f, halfWidth);
            List<StatusTargetCandidate> candidates = new List<StatusTargetCandidate>();
            PruneMissingActiveMonsters();
            for (int i = 0; i < ActiveMonsters.Count; i++)
            {
                MonsterUnit monster = ActiveMonsters[i];
                if (!CanReceiveControlStatus(monster, options.excludeBosses))
                {
                    continue;
                }

                Vector3 offset = monster.transform.position - origin;
                offset.y = 0f;
                float forwardDistance = Vector3.Dot(offset, direction);
                if (forwardDistance < 0f || forwardDistance > checkedLength)
                {
                    continue;
                }

                Vector3 closestPoint = direction * forwardDistance;
                if ((offset - closestPoint).magnitude > checkedHalfWidth)
                {
                    continue;
                }

                candidates.Add(new StatusTargetCandidate
                {
                    target = monster,
                    sortDistance = forwardDistance
                });
            }

            return ApplyPetrifyToCandidates(candidates, options);
        }

        public static int ApplyPetrifyRadius(Vector3 center, float radius, PetrifyTargetOptions options)
        {
            if (options.duration <= 0f)
            {
                return 0;
            }

            float checkedRadius = Mathf.Max(0.1f, radius);
            float checkedRadiusSqr = checkedRadius * checkedRadius;
            List<StatusTargetCandidate> candidates = new List<StatusTargetCandidate>();
            PruneMissingActiveMonsters();
            for (int i = 0; i < ActiveMonsters.Count; i++)
            {
                MonsterUnit monster = ActiveMonsters[i];
                if (!CanReceiveControlStatus(monster, options.excludeBosses))
                {
                    continue;
                }

                Vector3 offset = monster.transform.position - center;
                offset.y = 0f;
                float sqrDistance = offset.sqrMagnitude;
                if (sqrDistance > checkedRadiusSqr)
                {
                    continue;
                }

                candidates.Add(new StatusTargetCandidate
                {
                    target = monster,
                    sortDistance = sqrDistance
                });
            }

            return ApplyPetrifyToCandidates(candidates, options);
        }

        public MonsterDefinition Definition => definition;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => definition != null ? definition.stats.maxHealth * outgameHealthMultiplier : 0f;
        public float CurrentMana => currentMana;
        public bool IsBoss => definition != null && definition.IsBossLike;
        private bool IsMajorBoss => definition != null && definition.IsMajorBoss;
        public bool IsStunned => IsControlLocked;
        public bool IsPetrified => petrifyTimer > 0f;
        public bool CanBeCombatTargeted => !isDying && currentHealth > 0f && !IsPetrified;
        public DefenderUnit LastDamageSource { get; private set; }
        public SkillDefinition LastDamageSkill { get; private set; }
        public float CurrentAttackRange => definition != null ? GetEffectiveAttackRange() : 0f;

        public struct PetrifyTargetOptions
        {
            public float duration;
            public int maxTargets;
            public bool excludeBosses;
            public Material materialOverride;
            public System.Action<MonsterUnit> onApplied;
        }

        private struct StatusTargetCandidate
        {
            public MonsterUnit target;
            public float sortDistance;
        }

        private sealed class RendererMaterialSnapshot
        {
            public Renderer renderer;
            public Material[] materials;
        }

        private sealed class AnimatorSpeedSnapshot
        {
            public Animator animator;
            public float speed;
        }

        private struct PendingMonsterAttack
        {
            public bool isValid;
            public int sequence;
            public DefenderUnit target;
            public float damage;
            public bool critical;
        }

        private struct PendingMonsterSkill
        {
            public bool isValid;
            public int sequence;
            public SkillDefinition skill;
        }

        private void OnEnable()
        {
            RegisterActiveMonster();
            DefenderUnit.OnDefenderSpawned += HandleDefenderSpawned;
            DefenderUnit.OnDefenderRemoved += HandleDefenderRemoved;
        }

        private void OnDisable()
        {
            UnregisterActiveMonster();
            DefenderUnit.OnDefenderSpawned -= HandleDefenderSpawned;
            DefenderUnit.OnDefenderRemoved -= HandleDefenderRemoved;
            UnbindAnimationDriver();
            ClearPendingImpacts();
            ClearPetrifyStatus();
        }

        public static int CountActive(MonsterThreatLevel threatLevel)
        {
            PruneMissingActiveMonsters();
            int count = 0;
            for (int i = ActiveMonsters.Count - 1; i >= 0; i--)
            {
                MonsterUnit monster = ActiveMonsters[i];
                if (monster.definition != null && monster.definition.threatLevel == threatLevel)
                {
                    count++;
                }
            }

            return count;
        }

        private static int ApplyPetrifyToCandidates(List<StatusTargetCandidate> candidates, PetrifyTargetOptions options)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return 0;
            }

            candidates.Sort((left, right) => left.sortDistance.CompareTo(right.sortDistance));
            int limit = options.maxTargets <= 0 ? candidates.Count : Mathf.Min(options.maxTargets, candidates.Count);
            int applied = 0;
            for (int i = 0; i < limit; i++)
            {
                MonsterUnit target = candidates[i].target;
                if (!CanReceiveControlStatus(target, options.excludeBosses))
                {
                    continue;
                }

                target.ApplyPetrify(options.duration, options.materialOverride);
                options.onApplied?.Invoke(target);
                applied++;
            }

            return applied;
        }

        private static bool CanReceiveControlStatus(MonsterUnit monster, bool excludeBosses = false)
        {
            return monster != null && !monster.isDying && monster.currentHealth > 0f && (!excludeBosses || !monster.IsBoss);
        }

        private void RegisterActiveMonster()
        {
            if (!ActiveMonsters.Contains(this))
            {
                ActiveMonsters.Add(this);
            }
        }

        private void UnregisterActiveMonster()
        {
            ActiveMonsters.Remove(this);
        }

        private static void PruneMissingActiveMonsters()
        {
            for (int i = ActiveMonsters.Count - 1; i >= 0; i--)
            {
                if (ActiveMonsters[i] == null)
                {
                    ActiveMonsters.RemoveAt(i);
                }
            }
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

            if (isDying)
            {
                animationDriver?.PlayMoving(false);
                floatingUi?.SetValues(0f, MaxHealth, currentMana, definition.stats.maxMana);
                return;
            }

            TickBuffs();
            TickSkillCooldowns();
            TickBossPhase();

            float manaRegenRate = definition.stats.manaRegenPerSecondRate;
            if (definition.role == MonsterRole.Caster)
            {
                manaRegenRate += 0.025f;
            }

            currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * manaRegenRate * Time.deltaTime);
            attackCooldown -= Time.deltaTime;
            floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);

            if (IsControlLocked)
            {
                if (animationDriver == null || !animationDriver.IsLocked)
                {
                    animationDriver?.ForceIdle();
                }

                return;
            }

            if (skillCastLocked || IsActionAnimationLocked())
            {
                animationDriver?.PlayMoving(false);
                return;
            }

            DefenderUnit forcedTarget = ResolveTauntTarget();
            if (forcedTarget == null && TryCastSkill())
            {
                return;
            }

            DefenderUnit target = forcedTarget != null ? forcedTarget : FindNearestDefender();
            if (target != null)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance <= GetEffectiveAttackRange())
                {
                    if (attackCooldown <= 0f && CanStartActionAnimation())
                    {
                        PerformAttack(target);
                    }

                    animationDriver?.PlayMoving(false);
                    return;
                }

                if (forcedTarget != null)
                {
                    MoveTowardsDefender(forcedTarget);
                    return;
                }
            }

            MoveTowardsGoal();
        }

        public void Initialize(MonsterDefinition newDefinition, Transform goalPoint)
        {
            definition = newDefinition;
            outgameHealthMultiplier = 1f;
            outgameAttackMultiplier = 1f;
            if (OutgameProgressionSystem.Active != null)
            {
                OutgameProgressionSystem.Active.ResolveMonsterBalanceMultipliers(definition, out outgameHealthMultiplier, out outgameAttackMultiplier);
            }

            DailyFortuneRule fortune = DailyFortuneSystem.Today;
            if (fortune != null && definition != null && definition.IsBossLike)
            {
                outgameHealthMultiplier *= fortune.BossHealthMultiplier;
            }

            if (definition != null && definition.IsBossLike && DefenseGameController.Active != null)
            {
                outgameHealthMultiplier *= DefenseGameController.Active.FateDebtBossHealthMultiplier;
            }

            goal = goalPoint;
            laneGoalPosition = goal != null
                ? new Vector3(transform.position.x, goal.position.y, goal.position.z)
                : transform.position;
            currentHealth = MaxHealth;
            currentMana = 0f;
            attackCooldown = 0f;
            attackSpeedBonus = 0f;
            critChanceBonus = 0f;
            moveSpeedBonus = 0f;
            attackSpeedSlowRatio = 0f;
            attackSpeedSlowTimer = 0f;
            attackSpeedBuffTimer = 0f;
            critBuffTimer = 0f;
            moveSpeedBuffTimer = 0f;
            slowRatio = 0f;
            slowTimer = 0f;
            stunTimer = 0f;
            petrifyTimer = 0f;
            tauntTarget = null;
            tauntTimer = 0f;
            enraged = false;
            roleTraitTriggered = false;
            skillCastLocked = false;
            isDying = false;
            RestorePetrifyMaterials(false);
            RestorePetrifyAnimations(false);
            defenders.Clear();
            defenders.AddRange(FindObjectsOfType<DefenderUnit>());
            skillCooldowns.Clear();
            if (gameController == null)
            {
                gameController = FindObjectOfType<DefenseGameController>();
            }

            gameObject.name = definition.displayName;
            ApplyVisuals();
            EnsureAnimationDriver();
            EnsureHitFlashFeedback();
            floatingUi = FloatingCombatUI.Attach(transform, definition.displayName, definition.accentColor, definition.grade, GetFloatingUiFallbackHeight());
            floatingUi.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
            animationDriver?.PlaySpawn();
            OnMonsterSpawned?.Invoke(this);
        }

        private float GetFloatingUiFallbackHeight()
        {
            if (definition == null)
            {
                return 1.55f;
            }

            if (IsMajorBoss)
            {
                return 3.25f;
            }

            if (definition.threatLevel == MonsterThreatLevel.MidBoss)
            {
                return 2.35f;
            }

            float height = 1.48f;
            if (definition.role == MonsterRole.Brute || definition.role == MonsterRole.Elite) height = 1.72f;
            else if (definition.role == MonsterRole.Caster) height = 1.58f;
            else if (definition.role == MonsterRole.Charger) height = 1.42f;

            if (definition.grade == CharacterGrade.Legendary) height += 0.08f;
            else if (definition.grade == CharacterGrade.Mythic) height += 0.14f;

            return height;
        }

        public void TakeDamage(float damage, bool critical)
        {
            TakeDamage(damage, critical, null);
        }

        public void TakeDamage(float damage, bool critical, DefenderUnit source)
        {
            if (!CanBeCombatTargeted)
            {
                return;
            }

            float finalDamage = damage * (1f - GetRoleDamageReduction());
            if (source != null && finalDamage > 0f)
            {
                LastDamageSource = source;
                LastDamageSkill = DefenderUnit.CurrentDamageSkillContext;
            }

            currentHealth -= finalDamage;
            currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * definition.stats.manaGainWhenHitRate);
            TryTriggerRoleTrait();
            hitFlashFeedback?.PlayHit(critical);
            floatingUi?.ShowDamage(finalDamage, critical, false);
            floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
            DefenderUnit.ReportDamageDealt(source, this, finalDamage, critical);

            if (currentHealth <= 0f)
            {
                BeginDeath();
            }
        }

        public int GetRewardGold()
        {
            return definition != null ? definition.rewardGold : 0;
        }

        private void BeginDeath()
        {
            if (isDying)
            {
                return;
            }

            isDying = true;
            currentHealth = 0f;
            skillCastLocked = false;
            ClearPendingImpacts();
            animationDriver?.PlayMoving(false);
            floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
            PlayDeathEffect();

            if (IsBoss)
            {
                StartCoroutine(BossDeathPresentationRoutine());
                return;
            }

            CompleteDeath();
        }

        private IEnumerator BossDeathPresentationRoutine()
        {
            float duration = IsMajorBoss ? bossDeathPresentationDelay : bossDeathPresentationDelay * 0.75f;
            duration = Mathf.Clamp(duration, 0.75f, 2.25f);
            Color color = definition != null ? definition.accentColor : new Color(1f, 0.32f, 0.18f);
            float radius = IsMajorBoss ? bossDeathPulseRadius : bossDeathPulseRadius * 0.72f;

            floatingUi?.ShowStatus(IsMajorBoss ? "BOSS DOWN" : "MID BOSS DOWN", Color.Lerp(color, Color.white, 0.35f), duration);
            RuntimeCombatFeedback.ShowBossDefeat(transform.position, color, radius, duration);
            RuntimeCameraShake.Request(IsMajorBoss ? 0.18f : 0.11f, Mathf.Min(0.5f, duration * 0.42f));
            RuntimeAudioUtility.PlayHit();

            Vector3 originalScale = transform.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI * 4f) * (1f - t) * 0.055f;
                transform.localScale = originalScale * (1f + pulse);
                yield return null;
            }

            transform.localScale = originalScale;
            CompleteDeath();
        }

        private void CompleteDeath()
        {
            OnMonsterKilled?.Invoke(this);
            Destroy(gameObject);
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
            floatingUi?.ShowDamage(amount, false, true);
            floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
        }

        public void ApplySlow(float ratio, float duration)
        {
            if (!CanBeCombatTargeted)
            {
                return;
            }

            slowRatio = Mathf.Max(slowRatio, Mathf.Clamp01(ratio));
            slowTimer = Mathf.Max(slowTimer, duration);
            hitFlashFeedback?.PlayHit(false);
        }

        public void ApplyAttackSpeedSlow(float ratio, float duration)
        {
            if (!CanBeCombatTargeted)
            {
                return;
            }

            attackSpeedSlowRatio = Mathf.Max(attackSpeedSlowRatio, Mathf.Clamp01(ratio));
            attackSpeedSlowTimer = Mathf.Max(attackSpeedSlowTimer, duration);
            hitFlashFeedback?.PlayHit(false);
        }

        public void ApplyStun(float duration)
        {
            if (!CanBeCombatTargeted)
            {
                return;
            }

            float effectiveDuration = ResolveControlDuration(duration);
            stunTimer = Mathf.Max(stunTimer, effectiveDuration);
            hitFlashFeedback?.PlayHit(true);
        }

        public void ApplyPetrify(float duration, Material materialOverride = null)
        {
            float effectiveDuration = ResolveControlDuration(duration);
            if (effectiveDuration <= 0f)
            {
                return;
            }

            petrifyTimer = Mathf.Max(petrifyTimer, effectiveDuration);
            attackCooldown = Mathf.Max(attackCooldown, Mathf.Min(effectiveDuration, 1.2f));
            skillCastLocked = false;
            ClearPendingImpacts();
            animationDriver?.ForceIdle();
            FreezePetrifyAnimations();
            floatingUi?.ShowStatus("PETRIFY", new Color(0.72f, 0.78f, 0.82f, 1f), Mathf.Min(effectiveDuration, 1.2f));
            RuntimeCombatFeedback.ShowGroundPulse(transform.position, new Color(0.70f, 0.76f, 0.80f, 1f), IsBoss ? 0.95f : 0.62f, 0.46f, 0.09f);
            hitFlashFeedback?.PlayHit(true);
            ApplyPetrifyMaterials(materialOverride);
        }

        private float ResolveControlDuration(float duration)
        {
            if (duration <= 0f)
            {
                return 0f;
            }

            return IsMajorBoss ? duration * 0.45f : IsBoss ? duration * 0.65f : duration;
        }

        public void ApplyKnockback(float distance, Vector3 sourcePosition)
        {
            if (!CanBeCombatTargeted || distance <= 0f)
            {
                return;
            }

            Vector3 awayFromGoal = transform.position - laneGoalPosition;
            awayFromGoal.y = 0f;
            if (awayFromGoal.sqrMagnitude <= 0.0001f)
            {
                awayFromGoal = transform.position - sourcePosition;
                awayFromGoal.y = 0f;
            }

            if (awayFromGoal.sqrMagnitude <= 0.0001f)
            {
                awayFromGoal = Vector3.back;
            }

            Vector3 pushedPosition = transform.position + awayFromGoal.normalized * distance;
            pushedPosition.x = Mathf.Clamp(pushedPosition.x, laneGoalPosition.x - 0.6f, laneGoalPosition.x + 0.6f);
            pushedPosition.y = transform.position.y;
            transform.position = pushedPosition;
            hitFlashFeedback?.PlayHit(true);
        }

        public void ApplyTaunt(DefenderUnit source, float duration)
        {
            if (!CanBeCombatTargeted || source == null || source.CurrentHealth <= 0f || duration <= 0f)
            {
                return;
            }

            tauntTarget = source;
            tauntTimer = Mathf.Max(tauntTimer, duration);
            hitFlashFeedback?.PlayHit(false);
        }

        public void ApplyPoison(float damagePerTick, float duration, float tickInterval, DefenderUnit source)
        {
            if (!CanBeCombatTargeted)
            {
                return;
            }

            StartCoroutine(PoisonRoutine(Mathf.Max(0f, damagePerTick), Mathf.Max(0f, duration), Mathf.Max(0.2f, tickInterval), source));
            hitFlashFeedback?.PlayHit(false);
        }

        public void ApplyRally(float amount, float duration)
        {
            attackSpeedBonus = Mathf.Max(attackSpeedBonus, amount);
            moveSpeedBonus = Mathf.Max(moveSpeedBonus, amount * 0.65f);
            attackSpeedBuffTimer = Mathf.Max(attackSpeedBuffTimer, duration);
            moveSpeedBuffTimer = Mathf.Max(moveSpeedBuffTimer, duration);
            hitFlashFeedback?.PlayHit(false);
        }

        private IEnumerator PoisonRoutine(float damagePerTick, float duration, float tickInterval, DefenderUnit source)
        {
            float elapsed = 0f;
            while (elapsed < duration && currentHealth > 0f)
            {
                TakeDamage(damagePerTick, false, source);
                yield return new WaitForSeconds(tickInterval);
                elapsed += tickInterval;
            }
        }

        private void MoveTowardsGoal()
        {
            if (goal == null)
            {
                return;
            }

            Vector3 moveTarget = BuildMoveTarget();
            FaceTarget(moveTarget);
            float moveSpeed = definition.stats.moveSpeed * (1f + moveSpeedBonus) * (1f - slowRatio) * globalMoveSpeedMultiplier;
            transform.position = Vector3.MoveTowards(transform.position, moveTarget, moveSpeed * Time.deltaTime);
            animationDriver?.PlayMoving(true);

            if (Vector3.Distance(transform.position, laneGoalPosition) <= 0.05f)
            {
                OnMonsterEscaped?.Invoke(this);
                Destroy(gameObject);
            }
        }

        private void MoveTowardsDefender(DefenderUnit target)
        {
            if (target == null)
            {
                return;
            }

            Vector3 moveTarget = target.transform.position;
            moveTarget.y = transform.position.y;
            FaceTarget(moveTarget);
            float moveSpeed = definition.stats.moveSpeed * (1f + moveSpeedBonus) * (1f - slowRatio) * globalMoveSpeedMultiplier;
            transform.position = Vector3.MoveTowards(transform.position, moveTarget, moveSpeed * Time.deltaTime);
            animationDriver?.PlayMoving(true);
        }

        private void PerformAttack(DefenderUnit target)
        {
            if (target == null)
            {
                return;
            }

            FaceTarget(target.transform.position);
            float effectiveAttackSpeed = Mathf.Max(0.2f, definition.stats.attackSpeed * (1f + attackSpeedBonus) * (1f - attackSpeedSlowRatio));
            attackCooldown = 1f / effectiveAttackSpeed;

            bool critical = Random.value <= Mathf.Clamp01(definition.stats.criticalChance + critChanceBonus);
            float damage = GetEffectiveAttackPower() * (critical ? definition.stats.criticalDamageMultiplier : 1f);
            QueueBasicAttackImpact(target, damage, critical);

            bool animationStarted = animationDriver != null && animationDriver.PlayAttack();
            if (animationStarted)
            {
                SchedulePendingAttackFallback(pendingBasicAttack.sequence);
            }
            else
            {
                ResolvePendingBasicAttack();
            }
        }

        private void QueueBasicAttackImpact(DefenderUnit target, float damage, bool critical)
        {
            CancelPendingAttackFallback();
            pendingBasicAttack = new PendingMonsterAttack
            {
                isValid = true,
                sequence = ++impactSequence,
                target = target,
                damage = damage,
                critical = critical
            };
        }

        private void ResolvePendingBasicAttack()
        {
            if (!pendingBasicAttack.isValid)
            {
                return;
            }

            PendingMonsterAttack pending = pendingBasicAttack;
            pendingBasicAttack = default;
            CancelPendingAttackFallback();

            if (pending.target == null)
            {
                return;
            }

            FaceTarget(pending.target.transform.position);
            GainManaFromBasicAttack();
            if (TryLaunchBasicAttackProjectile(pending))
            {
                return;
            }

            PlayBasicAttackMuzzleEffect();
            PlayBasicAttackHitEffect(pending.target);
            pending.target.TakeDamage(pending.damage, pending.critical, this);
            ApplyBasicAttackExtensions(pending.target, pending.damage);
        }

        private bool TryLaunchBasicAttackProjectile(PendingMonsterAttack pending)
        {
            GameObject projectilePrefab = ResolveBasicAttackProjectilePrefab();
            if (projectilePrefab == null || pending.target == null)
            {
                return false;
            }

            PlayBasicAttackMuzzleEffect();
            StartCoroutine(BasicAttackProjectileRoutine(projectilePrefab, pending));
            return true;
        }

        private IEnumerator BasicAttackProjectileRoutine(GameObject projectilePrefab, PendingMonsterAttack pending)
        {
            Vector3 startPosition = transform.position + Vector3.up * ResolveBasicAttackEffectHeight();
            GameObject projectile = Instantiate(projectilePrefab, startPosition, transform.rotation);
            if (projectile == null)
            {
                ResolveBasicAttackDamage(pending);
                yield break;
            }

            projectile.SetActive(true);
            float speed = definition != null ? Mathf.Max(2f, definition.stats.projectileSpeed) : 8f;
            float elapsed = 0f;
            float maxDuration = 1.35f;
            while (pending.target != null && elapsed < maxDuration)
            {
                Vector3 targetPosition = pending.target.transform.position + Vector3.up * ResolveBasicAttackEffectHeight();
                Vector3 direction = targetPosition - projectile.transform.position;
                if (direction.sqrMagnitude <= 0.04f)
                {
                    break;
                }

                projectile.transform.position = Vector3.MoveTowards(projectile.transform.position, targetPosition, speed * Time.deltaTime);
                if (direction.sqrMagnitude > 0.001f)
                {
                    projectile.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (projectile != null)
            {
                Destroy(projectile);
            }

            ResolveBasicAttackDamage(pending);
        }

        private void ResolveBasicAttackDamage(PendingMonsterAttack pending)
        {
            if (pending.target == null)
            {
                return;
            }

            PlayBasicAttackHitEffect(pending.target);
            pending.target.TakeDamage(pending.damage, pending.critical, this);
            ApplyBasicAttackExtensions(pending.target, pending.damage);
        }

        private float ResolveBasicAttackEffectHeight()
        {
            return IsBoss ? 0.72f : 0.48f;
        }

        private void PlayBasicAttackMuzzleEffect()
        {
            GameObject effectPrefab = ResolveBasicAttackMuzzleEffectPrefab();
            if (effectPrefab == null)
            {
                return;
            }

            RuntimeEffectUtility.PlayOneShot(effectPrefab, transform.position + Vector3.up * 0.06f, transform.rotation, IsBoss ? 0.48f : 0.28f);
        }

        private void PlayBasicAttackHitEffect(DefenderUnit target)
        {
            if (target == null)
            {
                return;
            }

            GameObject effectPrefab = ResolveBasicAttackHitEffectPrefab();
            if (effectPrefab == null)
            {
                return;
            }

            RuntimeEffectUtility.PlayOneShot(effectPrefab, target.transform.position + Vector3.up * 0.06f, Quaternion.identity, IsBoss ? 0.54f : 0.30f);
        }

        private GameObject ResolveBasicAttackProjectilePrefab()
        {
            return definition != null && definition.attackBehavior != null ? definition.attackBehavior.projectilePrefabOverride : null;
        }

        private GameObject ResolveBasicAttackMuzzleEffectPrefab()
        {
            return definition != null && definition.attackBehavior != null ? definition.attackBehavior.muzzleEffectPrefab : null;
        }

        private GameObject ResolveBasicAttackHitEffectPrefab()
        {
            return definition != null && definition.attackBehavior != null ? definition.attackBehavior.hitEffectPrefab : null;
        }

        private void GainManaFromBasicAttack()
        {
            if (definition == null || definition.stats.maxMana <= 0f || definition.stats.manaGainPerAttackRate <= 0f)
            {
                return;
            }

            currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * definition.stats.manaGainPerAttackRate);
            floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
        }

        private bool TryCastSkill()
        {
            if (!CanStartActionAnimation())
            {
                return false;
            }

            if (definition.skills == null || definition.skills.Count == 0)
            {
                return false;
            }

            if (currentMana <= 0f)
            {
                return false;
            }

            for (int i = 0; i < definition.skills.Count; i++)
            {
                SkillDefinition skill = definition.skills[i];
                float requiredMana = Mathf.Clamp(skill.manaThreshold <= 0f ? definition.stats.maxMana : skill.manaThreshold, 0f, definition.stats.maxMana);
                if (currentMana < requiredMana)
                {
                    continue;
                }

                if (!CanCastSkill(skill))
                {
                    continue;
                }

                if (skillCooldowns.TryGetValue(skill.id, out float cooldown) && cooldown > 0f)
                {
                    continue;
                }

                skillCooldowns[skill.id] = skill.cooldown;
                currentMana = 0f;
                animationDriver?.PlayMoving(false);
                StartCoroutine(CastSkillSequence(skill));
                return true;
            }

            return false;
        }

        private IEnumerator CastSkillSequence(SkillDefinition skill)
        {
            skillCastLocked = true;
            float warningDelay = IsMajorBoss ? 0.85f : IsBoss ? 0.45f : 0f;
            if (warningDelay > 0f)
            {
                NotifySkillWarning(skill, warningDelay);
                yield return new WaitForSeconds(warningDelay);
            }

            CastSkill(skill);
            skillCastLocked = false;
        }

        private bool CanCastSkill(SkillDefinition skill)
        {
            if (skill == null)
            {
                return false;
            }

            if (skill.effectType == SkillEffectType.DeathPact ||
                skill.effectType == SkillEffectType.Stun ||
                skill.effectType == SkillEffectType.MassStun ||
                skill.effectType == SkillEffectType.SummonRush ||
                skill.effectType == SkillEffectType.DirectDamage ||
                skill.effectType == SkillEffectType.AreaDamage ||
                skill.effectType == SkillEffectType.GoldDrain ||
                skill.effectType == SkillEffectType.ManaBurn)
            {
                return CountLivingDefenders() > 0;
            }

            if (skill.effectType == SkillEffectType.HealSelf)
            {
                return currentHealth < MaxHealth * 0.98f;
            }

            return true;
        }

        private void CastSkill(SkillDefinition skill)
        {
            QueueSkillImpact(skill);
            bool animationStarted = animationDriver != null && animationDriver.PlaySkill();
            NotifySkillPresentation(skill);
            if (animationStarted)
            {
                SchedulePendingSkillFallback(pendingSkillCast.sequence);
            }
            else
            {
                ResolvePendingSkillCast();
            }
        }

        private void ApplySkillEffect(SkillDefinition skill)
        {
            if (skill == null)
            {
                return;
            }

            int bossAffectedTargets = 0;
            float bossDamageDone = 0f;
            int bossGoldDrained = 0;

            if (skill.effectType == SkillEffectType.DirectDamage)
            {
                DefenderUnit singleTarget = FindNearestDefender();
                if (singleTarget != null)
                {
                    FaceTarget(singleTarget.transform.position);
                    ShowSkillImpactFeedback(singleTarget.transform.position, skill, 0.72f, IsBoss);
                    float damage = GetEffectiveAttackPower() * skill.power;
                    singleTarget.TakeDamage(damage, false, this);
                    bossAffectedTargets++;
                    bossDamageDone += damage;
                }
            }
            else if (skill.effectType == SkillEffectType.AreaDamage)
            {
                DefenderUnit[] targets = FindObjectsOfType<DefenderUnit>();
                if (targets.Length > 0)
                {
                    FaceTarget(targets[0].transform.position);
                }
                ShowSkillImpactFeedback(transform.position, skill, Mathf.Max(0.8f, skill.radius), IsBoss);
                for (int i = 0; i < targets.Length; i++)
                {
                    if (Vector3.Distance(transform.position, targets[i].transform.position) <= skill.radius)
                    {
                        float damage = GetEffectiveAttackPower() * skill.power;
                        targets[i].TakeDamage(damage, false, this);
                        bossAffectedTargets++;
                        bossDamageDone += damage;
                    }
                }
            }
            else if (skill.effectType == SkillEffectType.HealSelf)
            {
                Heal(MaxHealth * skill.power);
                ShowSkillImpactFeedback(transform.position, skill, 0.82f, false);
                bossAffectedTargets = 1;
            }
            else if (skill.effectType == SkillEffectType.AttackSpeedBoost)
            {
                attackSpeedBonus = skill.power;
                attackSpeedBuffTimer = skill.duration;
                ShowSkillImpactFeedback(transform.position, skill, 0.82f, false);
                bossAffectedTargets = 1;
            }
            else if (skill.effectType == SkillEffectType.CriticalBoost)
            {
                critChanceBonus = skill.power;
                critBuffTimer = skill.duration;
                ShowSkillImpactFeedback(transform.position, skill, 0.82f, false);
                bossAffectedTargets = 1;
            }
            else if (skill.effectType == SkillEffectType.MoveSpeedBoost)
            {
                moveSpeedBonus = skill.power;
                moveSpeedBuffTimer = skill.duration;
                ShowSkillImpactFeedback(transform.position, skill, 0.82f, false);
                bossAffectedTargets = 1;
            }
            else if (skill.effectType == SkillEffectType.ManaSurge)
            {
                currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * skill.power);
                ShowSkillImpactFeedback(transform.position, skill, 0.82f, false);
                bossAffectedTargets = 1;
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
                    ShowSkillImpactFeedback(targets[i].transform.position, skill, 0.64f, false);
                    float damage = GetEffectiveAttackPower() * skill.power;
                    targets[i].TakeDamage(damage, false, this);
                    bossAffectedTargets++;
                    bossDamageDone += damage;
                }
            }
            else if (skill.effectType == SkillEffectType.Stun)
            {
                DefenderUnit target = FindNearestDefender();
                if (target != null)
                {
                    FaceTarget(target.transform.position);
                    ShowSkillImpactFeedback(target.transform.position, skill, 0.70f, IsBoss);
                    target.ApplyStun(skill.duration);
                    bossAffectedTargets++;
                }
            }
            else if (skill.effectType == SkillEffectType.MassStun)
            {
                List<DefenderUnit> targets = GetRandomDefenders(Mathf.Max(1, skill.hitCount));
                if (targets.Count > 0)
                {
                    FaceTarget(targets[0].transform.position);
                }

                for (int i = 0; i < targets.Count; i++)
                {
                    ShowSkillImpactFeedback(targets[i].transform.position, skill, 0.66f, i == 0 && IsBoss);
                    targets[i].ApplyStun(skill.duration);
                    bossAffectedTargets++;
                }
            }
            else if (skill.effectType == SkillEffectType.DeathPact)
            {
                DefenderUnit target = GetRandomDefender();
                if (target != null)
                {
                    FaceTarget(target.transform.position);
                    ShowSkillImpactFeedback(target.transform.position, skill, 0.92f, true);
                    target.KillByBossSkill();
                    RuntimeCameraShake.Request(0.12f, 0.28f);
                    bossAffectedTargets++;
                }
            }
            else if (skill.effectType == SkillEffectType.BossFortify)
            {
                Heal(MaxHealth * skill.power);
                attackSpeedBonus = Mathf.Max(attackSpeedBonus, 0.12f);
                attackSpeedBuffTimer = Mathf.Max(attackSpeedBuffTimer, skill.duration);
                ShowSkillImpactFeedback(transform.position, skill, 1.05f, true);
                bossAffectedTargets = 1;
            }
            else if (skill.effectType == SkillEffectType.GoldDrain)
            {
                int drain = Mathf.Max(1, Mathf.RoundToInt(skill.power));
                int removed = gameController != null ? gameController.RemoveGold(drain) : 0;
                if (removed > 0)
                {
                    gameController.RequestBanner("탐욕의 징수  -" + removed + "G", definition.accentColor, 2.2f);
                }
                bossGoldDrained = removed;
                bossAffectedTargets = removed > 0 ? 1 : 0;
                ShowSkillImpactFeedback(transform.position, skill, 0.86f, false);
            }
            else if (skill.effectType == SkillEffectType.ManaBurn)
            {
                List<DefenderUnit> targets = GetRandomDefenders(Mathf.Max(1, skill.hitCount));
                if (targets.Count > 0)
                {
                    FaceTarget(targets[0].transform.position);
                }

                for (int i = 0; i < targets.Count; i++)
                {
                    ShowSkillImpactFeedback(targets[i].transform.position, skill, 0.66f, i == 0 && IsBoss);
                    targets[i].DrainMana(skill.power);
                    bossAffectedTargets++;
                }
            }
            else if (skill.effectType == SkillEffectType.MonsterRally)
            {
                IReadOnlyList<MonsterUnit> allies = ActiveInstances;
                for (int i = 0; i < allies.Count; i++)
                {
                    if (allies[i] != null)
                    {
                        allies[i].ApplyRally(skill.power, skill.duration);
                        ShowSkillImpactFeedback(allies[i].transform.position, skill, allies[i] == this ? 0.92f : 0.62f, allies[i] == this && IsBoss);
                        bossAffectedTargets++;
                    }
                }
            }

            if (IsBoss && gameController != null)
            {
                gameController.RecordBossSkillImpact(skill, bossAffectedTargets, bossDamageDone, bossGoldDrained, IsMajorBoss);
            }
        }

        private void QueueSkillImpact(SkillDefinition skill)
        {
            CancelPendingSkillFallback();
            pendingSkillCast = new PendingMonsterSkill
            {
                isValid = true,
                sequence = ++impactSequence,
                skill = skill
            };
        }

        private void ResolvePendingSkillCast()
        {
            if (!pendingSkillCast.isValid)
            {
                return;
            }

            PendingMonsterSkill pending = pendingSkillCast;
            pendingSkillCast = default;
            CancelPendingSkillFallback();
            ApplySkillEffect(pending.skill);
        }

        private void HandleAnimationImpact(AnimationImpactType impactType)
        {
            if (ShouldResolvePendingBasicAttack(impactType))
            {
                ResolvePendingBasicAttack();
                return;
            }

            if (impactType == AnimationImpactType.Skill && pendingSkillCast.isValid)
            {
                ResolvePendingSkillCast();
                return;
            }

            if (impactType == AnimationImpactType.Auto)
            {
                if (pendingBasicAttack.isValid)
                {
                    ResolvePendingBasicAttack();
                }
                else if (pendingSkillCast.isValid)
                {
                    ResolvePendingSkillCast();
                }
            }
        }

        private bool ShouldResolvePendingBasicAttack(AnimationImpactType impactType)
        {
            if (!pendingBasicAttack.isValid)
            {
                return false;
            }

            if (impactType == AnimationImpactType.Auto || impactType == AnimationImpactType.Attack)
            {
                return true;
            }

            bool isMelee = definition != null && definition.attackBehavior != null && definition.attackBehavior.IsMelee;
            return isMelee
                ? impactType == AnimationImpactType.AttackHit
                : impactType == AnimationImpactType.FireProjectile;
        }

        private void SchedulePendingAttackFallback(int sequence)
        {
            CancelPendingAttackFallback();
            float delay = animationDriver != null ? animationDriver.AttackImpactFallbackDelay : 0.12f;
            pendingAttackImpactRoutine = StartCoroutine(ResolvePendingAttackAfterDelay(sequence, delay));
        }

        private IEnumerator ResolvePendingAttackAfterDelay(int sequence, float delay)
        {
            yield return new WaitForSeconds(delay);
            pendingAttackImpactRoutine = null;
            if (pendingBasicAttack.isValid && pendingBasicAttack.sequence == sequence)
            {
                ResolvePendingBasicAttack();
            }
        }

        private void SchedulePendingSkillFallback(int sequence)
        {
            CancelPendingSkillFallback();
            float delay = animationDriver != null ? animationDriver.SkillImpactFallbackDelay : 0.2f;
            pendingSkillImpactRoutine = StartCoroutine(ResolvePendingSkillAfterDelay(sequence, delay));
        }

        private IEnumerator ResolvePendingSkillAfterDelay(int sequence, float delay)
        {
            yield return new WaitForSeconds(delay);
            pendingSkillImpactRoutine = null;
            if (pendingSkillCast.isValid && pendingSkillCast.sequence == sequence)
            {
                ResolvePendingSkillCast();
            }
        }

        private void CancelPendingAttackFallback()
        {
            if (pendingAttackImpactRoutine != null)
            {
                StopCoroutine(pendingAttackImpactRoutine);
                pendingAttackImpactRoutine = null;
            }
        }

        private void CancelPendingSkillFallback()
        {
            if (pendingSkillImpactRoutine != null)
            {
                StopCoroutine(pendingSkillImpactRoutine);
                pendingSkillImpactRoutine = null;
            }
        }

        private void ClearPendingImpacts()
        {
            pendingBasicAttack = default;
            pendingSkillCast = default;
            CancelPendingAttackFallback();
            CancelPendingSkillFallback();
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
            IReadOnlyList<MonsterUnit> others = ActiveInstances;

            for (int i = 0; i < others.Count; i++)
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

                if (defender.CurrentHealth <= 0f)
                {
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

        private DefenderUnit ResolveTauntTarget()
        {
            if (tauntTimer <= 0f || tauntTarget == null || tauntTarget.CurrentHealth <= 0f)
            {
                tauntTarget = null;
                tauntTimer = 0f;
                return null;
            }

            return tauntTarget;
        }

        private DefenderUnit GetRandomDefender()
        {
            List<DefenderUnit> candidates = GetRandomDefenders(1);
            return candidates.Count > 0 ? candidates[0] : null;
        }

        private List<DefenderUnit> GetRandomDefenders(int count)
        {
            List<DefenderUnit> candidates = defenders.Where(defender => defender != null && defender.CurrentHealth > 0f).ToList();
            for (int i = 0; i < candidates.Count; i++)
            {
                int swapIndex = Random.Range(i, candidates.Count);
                DefenderUnit temp = candidates[i];
                candidates[i] = candidates[swapIndex];
                candidates[swapIndex] = temp;
            }

            return candidates.Take(Mathf.Max(0, count)).ToList();
        }

        private int CountLivingDefenders()
        {
            int count = 0;
            for (int i = defenders.Count - 1; i >= 0; i--)
            {
                DefenderUnit defender = defenders[i];
                if (defender == null)
                {
                    defenders.RemoveAt(i);
                    continue;
                }

                if (defender.CurrentHealth > 0f)
                {
                    count++;
                }
            }

            return count;
        }

        private void NotifySkillPresentation(SkillDefinition skill)
        {
            ShowSkillCastFeedback(skill);

            if (skill == null || gameController == null || !IsBoss)
            {
                return;
            }

            bool majorBossSkill =
                skill.effectType == SkillEffectType.DeathPact ||
                skill.effectType == SkillEffectType.MassStun ||
                skill.effectType == SkillEffectType.BossFortify ||
                skill.effectType == SkillEffectType.GoldDrain ||
                skill.effectType == SkillEffectType.ManaBurn ||
                skill.effectType == SkillEffectType.MonsterRally;
            string prefix = IsMajorBoss ? "보스 스킬: " : "중간보스 스킬: ";
            gameController.RecordBossSkillCast(skill, IsMajorBoss);
            gameController.RequestBanner(prefix + skill.displayName + " 발동!", definition.accentColor, majorBossSkill ? 2.6f : 2.0f);
        }

        private void NotifySkillWarning(SkillDefinition skill, float duration)
        {
            ShowSkillWarningFeedback(skill, duration);

            if (skill == null || gameController == null || !IsBoss)
            {
                return;
            }

            string prefix = IsMajorBoss ? "보스 경고: " : "중간보스 경고: ";
            Color warningColor = definition != null ? Color.Lerp(definition.accentColor, Color.white, 0.25f) : new Color(1f, 0.45f, 0.28f);
            gameController.RequestBanner(prefix + skill.displayName + " 준비!", warningColor, Mathf.Max(1.15f, duration + 0.65f));
        }

        private void ShowSkillCastFeedback(SkillDefinition skill)
        {
            if (skill == null || definition == null)
            {
                return;
            }

            Color color = ResolveSkillFeedbackColor(skill);
            float radius = ResolveSkillFeedbackRadius(skill);
            RuntimeCombatFeedback.ShowGroundPulse(transform.position, color, radius, IsBoss ? 0.62f : 0.34f);
            RuntimeEffectUtility.PlayOneShot(skill.muzzleEffectPrefab, transform.position + Vector3.up * 0.06f, Quaternion.identity, IsBoss ? 0.65f : 0.30f);
            floatingUi?.ShowStatus(skill.displayName, Color.Lerp(color, Color.white, 0.25f), IsBoss ? 1.0f : 0.62f);

            if (IsBoss)
            {
                RuntimeCameraShake.Request(IsMajorBoss ? 0.06f : 0.035f, IsMajorBoss ? 0.18f : 0.12f);
            }
        }

        private void ShowSkillWarningFeedback(SkillDefinition skill, float duration)
        {
            if (skill == null || definition == null)
            {
                return;
            }

            Color color = Color.Lerp(ResolveSkillFeedbackColor(skill), Color.white, 0.18f);
            RuntimeCombatFeedback.ShowGroundWarning(transform.position, color, ResolveSkillFeedbackRadius(skill) * 1.12f, Mathf.Max(0.2f, duration + 0.2f));
            floatingUi?.ShowStatus("!", color, Mathf.Max(0.45f, duration));
        }

        private void ShowSkillImpactFeedback(Vector3 position, SkillDefinition skill, float radius, bool shake)
        {
            if (skill == null || definition == null)
            {
                return;
            }

            RuntimeCombatFeedback.ShowGroundPulse(position, ResolveSkillFeedbackColor(skill), Mathf.Max(0.18f, radius), IsBoss ? 0.46f : 0.30f);
            RuntimeEffectUtility.PlayOneShot(ResolveSkillImpactEffectPrefab(skill), position + Vector3.up * 0.06f, Quaternion.identity, IsBoss ? 0.65f : 0.30f);
            RuntimeAudioUtility.PlayHit();

            if (shake)
            {
                RuntimeCameraShake.Request(IsMajorBoss ? 0.08f : 0.045f, IsMajorBoss ? 0.22f : 0.14f);
            }
        }

        private GameObject ResolveSkillImpactEffectPrefab(SkillDefinition skill)
        {
            if (skill == null)
            {
                return null;
            }

            if (skill.effectType == SkillEffectType.AreaDamage ||
                skill.effectType == SkillEffectType.SummonRush ||
                skill.effectType == SkillEffectType.MonsterRally ||
                skill.effectType == SkillEffectType.BossFortify ||
                skill.effectType == SkillEffectType.HealSelf)
            {
                return skill.areaEffectPrefab != null ? skill.areaEffectPrefab : skill.hitEffectPrefab;
            }

            return skill.hitEffectPrefab != null ? skill.hitEffectPrefab : skill.areaEffectPrefab;
        }

        private Color ResolveSkillFeedbackColor(SkillDefinition skill)
        {
            Color baseColor = definition != null ? definition.accentColor : new Color(1f, 0.35f, 0.22f);
            if (skill == null)
            {
                return baseColor;
            }

            switch (skill.effectType)
            {
                case SkillEffectType.Stun:
                case SkillEffectType.MassStun:
                    return new Color(1f, 0.88f, 0.22f, 0.95f);
                case SkillEffectType.ManaBurn:
                    return new Color(0.30f, 0.85f, 1f, 0.95f);
                case SkillEffectType.BossFortify:
                case SkillEffectType.HealSelf:
                case SkillEffectType.MonsterRally:
                    return new Color(0.40f, 1f, 0.62f, 0.95f);
                case SkillEffectType.DeathPact:
                case SkillEffectType.GoldDrain:
                    return new Color(1f, 0.24f, 0.22f, 0.98f);
                case SkillEffectType.AreaDamage:
                case SkillEffectType.SummonRush:
                    return Color.Lerp(baseColor, new Color(1f, 0.50f, 0.12f, 1f), 0.45f);
                default:
                    return baseColor;
            }
        }

        private float ResolveSkillFeedbackRadius(SkillDefinition skill)
        {
            if (skill == null)
            {
                return IsMajorBoss ? 1.65f : IsBoss ? 1.15f : 0.74f;
            }

            float baseRadius = IsMajorBoss ? 1.55f : IsBoss ? 1.05f : 0.64f;
            if (skill.effectType == SkillEffectType.AreaDamage || skill.effectType == SkillEffectType.MonsterRally)
            {
                return Mathf.Max(baseRadius, skill.radius);
            }

            if (skill.effectType == SkillEffectType.BossFortify || skill.effectType == SkillEffectType.DeathPact)
            {
                return baseRadius * 1.2f;
            }

            return baseRadius;
        }

        private void NotifySkill(SkillDefinition skill)
        {
            if (skill == null || gameController == null)
            {
                return;
            }

            if (!IsBoss)
            {
                return;
            }

            bool majorBossSkill =
                skill.effectType == SkillEffectType.DeathPact ||
                skill.effectType == SkillEffectType.MassStun ||
                skill.effectType == SkillEffectType.BossFortify ||
                skill.effectType == SkillEffectType.GoldDrain ||
                skill.effectType == SkillEffectType.ManaBurn ||
                skill.effectType == SkillEffectType.MonsterRally;
            string prefix = IsMajorBoss ? "보스 스킬: " : "중간보스 스킬: ";
            gameController.RequestBanner(prefix + skill.displayName + " 발동!", definition.accentColor, majorBossSkill ? 2.6f : 2.0f);
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

            if (slowTimer > 0f)
            {
                slowTimer -= Time.deltaTime;
                if (slowTimer <= 0f)
                {
                    slowRatio = 0f;
                }
            }

            if (attackSpeedSlowTimer > 0f)
            {
                attackSpeedSlowTimer -= Time.deltaTime;
                if (attackSpeedSlowTimer <= 0f)
                {
                    attackSpeedSlowRatio = 0f;
                }
            }

            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
            }

            if (petrifyTimer > 0f)
            {
                petrifyTimer -= Time.deltaTime;
                if (petrifyTimer <= 0f)
                {
                    EndPetrify();
                }
            }

            if (tauntTimer > 0f)
            {
                tauntTimer -= Time.deltaTime;
                if (tauntTimer <= 0f || tauntTarget == null || tauntTarget.CurrentHealth <= 0f)
                {
                    tauntTarget = null;
                    tauntTimer = 0f;
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

        private float GetRoleDamageReduction()
        {
            if (definition == null)
            {
                return 0f;
            }

            if (IsMajorBoss)
            {
                return 0.10f;
            }

            if (definition.threatLevel == MonsterThreatLevel.MidBoss)
            {
                return 0.05f;
            }

            if (definition.role == MonsterRole.Brute)
            {
                return 0.15f;
            }

            if (definition.role == MonsterRole.Elite)
            {
                return 0.06f;
            }

            return 0f;
        }

        private void TryTriggerRoleTrait()
        {
            if (definition == null || roleTraitTriggered)
            {
                return;
            }

            if (definition.role == MonsterRole.Charger && currentHealth <= MaxHealth * 0.60f)
            {
                roleTraitTriggered = true;
                moveSpeedBonus += 0.45f;
                attackSpeedBonus += 0.12f;
                moveSpeedBuffTimer = Mathf.Max(moveSpeedBuffTimer, 4f);
                attackSpeedBuffTimer = Mathf.Max(attackSpeedBuffTimer, 4f);
            }
            else if (definition.role == MonsterRole.Elite && currentHealth <= MaxHealth * 0.50f)
            {
                roleTraitTriggered = true;
                critChanceBonus += 0.18f;
                currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * 0.35f);
                critBuffTimer = Mathf.Max(critBuffTimer, 5f);
            }
        }

        private void ApplyVisuals()
        {
            if (tintRenderers == null || tintRenderers.Length == 0)
            {
                tintRenderers = GetComponentsInChildren<Renderer>(true);
            }

            Color tintColor = ResolveReadabilityTint();
            for (int i = 0; i < tintRenderers.Length; i++)
            {
                ApplyRendererTint(tintRenderers[i], tintColor);
            }

            transform.localScale = Vector3.one * ResolveVisualScale();
            ConfigureBossReadabilityMarker();
        }

        private Color ResolveReadabilityTint()
        {
            if (definition == null)
            {
                return Color.white;
            }

            if (!IsBoss)
            {
                return definition.accentColor;
            }

            Color bossSignalColor = IsMajorBoss ? new Color(1f, 0.28f, 0.18f, 1f) : new Color(1f, 0.78f, 0.18f, 1f);
            return Color.Lerp(definition.accentColor, bossSignalColor, IsMajorBoss ? 0.52f : 0.38f);
        }

        private void ConfigureBossReadabilityMarker()
        {
            bool shouldShow = IsBoss;
            if (!shouldShow)
            {
                if (bossReadabilityMarker != null)
                {
                    Destroy(bossReadabilityMarker);
                    bossReadabilityMarker = null;
                }

                return;
            }

            if (bossReadabilityMarker == null)
            {
                bossReadabilityMarker = new GameObject("BossReadabilityMarker");
                bossReadabilityMarker.transform.SetParent(transform, false);
                LineRenderer line = bossReadabilityMarker.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = 64;
                line.numCornerVertices = 4;
                line.numCapVertices = 4;
                line.material = new Material(Shader.Find("Sprites/Default"));
            }

            LineRenderer markerLine = bossReadabilityMarker.GetComponent<LineRenderer>();
            if (markerLine == null)
            {
                return;
            }

            float radius = IsMajorBoss ? 1.08f : 0.82f;
            float width = IsMajorBoss ? 0.12f : 0.085f;
            Color markerColor = IsMajorBoss ? new Color(1f, 0.18f, 0.10f, 0.92f) : new Color(1f, 0.78f, 0.12f, 0.88f);
            markerLine.widthMultiplier = width;
            markerLine.startColor = markerColor;
            markerLine.endColor = markerColor;
            for (int i = 0; i < markerLine.positionCount; i++)
            {
                float angle = Mathf.PI * 2f * i / markerLine.positionCount;
                markerLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.035f, Mathf.Sin(angle) * radius));
            }
        }

        private void ApplyRendererTint(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            PrepareRuntimeUnitRenderer(renderer);
            if (!RuntimeRenderBatchingUtility.UsePerInstanceUnitTint)
            {
                renderer.SetPropertyBlock(null);
                return;
            }

            if (visualPropertyBlock == null)
            {
                visualPropertyBlock = new MaterialPropertyBlock();
            }

            renderer.GetPropertyBlock(visualPropertyBlock);
            visualPropertyBlock.SetColor(BaseColorId, color);
            visualPropertyBlock.SetColor(ColorId, color);
            renderer.SetPropertyBlock(visualPropertyBlock);
        }

        private static void PrepareRuntimeUnitRenderer(Renderer renderer)
        {
            if (RuntimeRenderBatchingUtility.ForceRuntimeUnitCastShadowsOff)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            RuntimeRenderBatchingUtility.PrepareRenderer(renderer);
        }

        private float ResolveVisualScale()
        {
            if (definition == null || definition.visualScale <= 0.01f)
            {
                return IsMajorBoss ? 1.7f : definition != null && definition.threatLevel == MonsterThreatLevel.MidBoss ? 1.32f : 1f;
            }

            return definition.visualScale;
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

            BindAnimationDriver();
        }

        private void BindAnimationDriver()
        {
            if (subscribedAnimationDriver == animationDriver)
            {
                return;
            }

            UnbindAnimationDriver();
            subscribedAnimationDriver = animationDriver;
            if (subscribedAnimationDriver != null)
            {
                subscribedAnimationDriver.ImpactTriggered += HandleAnimationImpact;
            }
        }

        private void UnbindAnimationDriver()
        {
            if (subscribedAnimationDriver != null)
            {
                subscribedAnimationDriver.ImpactTriggered -= HandleAnimationImpact;
                subscribedAnimationDriver = null;
            }
        }

        private bool CanStartActionAnimation()
        {
            return animationDriver == null || !animationDriver.IsLocked;
        }

        private bool IsActionAnimationLocked()
        {
            return animationDriver != null && animationDriver.IsLocked;
        }

        private bool IsControlLocked => stunTimer > 0f || petrifyTimer > 0f;

        private void ApplyPetrifyMaterials(Material materialOverride)
        {
            Material petrifyMaterial = ResolvePetrifyMaterial(materialOverride);
            if (petrifyMaterial == null)
            {
                return;
            }

            if (tintRenderers == null || tintRenderers.Length == 0)
            {
                tintRenderers = GetComponentsInChildren<Renderer>(true);
            }

            if (petrifyMaterialSnapshots.Count == 0)
            {
                for (int i = 0; i < tintRenderers.Length; i++)
                {
                    Renderer renderer = tintRenderers[i];
                    if (!CanSwapPetrifyMaterial(renderer))
                    {
                        continue;
                    }

                    Material[] originalMaterials = renderer.sharedMaterials;
                    if (originalMaterials == null || originalMaterials.Length == 0)
                    {
                        continue;
                    }

                    petrifyMaterialSnapshots.Add(new RendererMaterialSnapshot
                    {
                        renderer = renderer,
                        materials = originalMaterials
                    });
                }
            }

            for (int i = 0; i < petrifyMaterialSnapshots.Count; i++)
            {
                Renderer renderer = petrifyMaterialSnapshots[i].renderer;
                Material[] originalMaterials = petrifyMaterialSnapshots[i].materials;
                if (renderer == null || originalMaterials == null || originalMaterials.Length == 0)
                {
                    continue;
                }

                Material[] petrifiedMaterials = new Material[originalMaterials.Length];
                for (int materialIndex = 0; materialIndex < petrifiedMaterials.Length; materialIndex++)
                {
                    petrifiedMaterials[materialIndex] = petrifyMaterial;
                }

                renderer.sharedMaterials = petrifiedMaterials;
                renderer.SetPropertyBlock(null);
            }
        }

        private void RestorePetrifyMaterials(bool reapplyVisuals = true)
        {
            if (petrifyMaterialSnapshots.Count == 0)
            {
                return;
            }

            for (int i = petrifyMaterialSnapshots.Count - 1; i >= 0; i--)
            {
                Renderer renderer = petrifyMaterialSnapshots[i].renderer;
                Material[] materials = petrifyMaterialSnapshots[i].materials;
                if (renderer != null && materials != null)
                {
                    renderer.sharedMaterials = materials;
                }
            }

            petrifyMaterialSnapshots.Clear();
            if (reapplyVisuals && definition != null)
            {
                ApplyVisuals();
                EnsureHitFlashFeedback();
            }
        }

        private void FreezePetrifyAnimations()
        {
            Animator[] animators = GetComponentsInChildren<Animator>(true);
            for (int animatorIndex = 0; animatorIndex < animators.Length; animatorIndex++)
            {
                Animator animator = animators[animatorIndex];
                if (animator == null)
                {
                    continue;
                }

                bool hasSnapshot = false;
                for (int snapshotIndex = 0; snapshotIndex < petrifyAnimatorSnapshots.Count; snapshotIndex++)
                {
                    if (petrifyAnimatorSnapshots[snapshotIndex].animator == animator)
                    {
                        hasSnapshot = true;
                        break;
                    }
                }

                if (!hasSnapshot)
                {
                    petrifyAnimatorSnapshots.Add(new AnimatorSpeedSnapshot
                    {
                        animator = animator,
                        speed = animator.speed
                    });
                }

                animator.speed = 0f;
            }
        }

        private void RestorePetrifyAnimations(bool resumeAnimation = true)
        {
            for (int i = petrifyAnimatorSnapshots.Count - 1; i >= 0; i--)
            {
                AnimatorSpeedSnapshot snapshot = petrifyAnimatorSnapshots[i];
                if (snapshot != null && snapshot.animator != null)
                {
                    snapshot.animator.speed = snapshot.speed;
                }
            }

            petrifyAnimatorSnapshots.Clear();
            if (resumeAnimation && animationDriver != null && !isDying)
            {
                animationDriver.ForceIdle();
            }
        }

        private void EndPetrify(bool reapplyVisuals = true, bool resumeAnimation = true)
        {
            petrifyTimer = 0f;
            RestorePetrifyMaterials(reapplyVisuals);
            RestorePetrifyAnimations(resumeAnimation);
        }

        private void ClearPetrifyStatus()
        {
            EndPetrify(false, false);
        }

        private static bool CanSwapPetrifyMaterial(Renderer renderer)
        {
            return renderer is MeshRenderer || renderer is SkinnedMeshRenderer;
        }

        private static Material ResolvePetrifyMaterial(Material materialOverride)
        {
            if (materialOverride != null)
            {
                return materialOverride;
            }

            if (defaultPetrifyMaterial != null)
            {
                return defaultPetrifyMaterial;
            }

            if (fallbackPetrifyMaterial != null)
            {
                return fallbackPetrifyMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                return null;
            }

            fallbackPetrifyMaterial = new Material(shader);
            fallbackPetrifyMaterial.name = "RuntimeFallbackPetrifyMaterial";
            Color color = new Color(0.58f, 0.62f, 0.64f, 1f);
            fallbackPetrifyMaterial.color = color;
            if (fallbackPetrifyMaterial.HasProperty("_BaseColor"))
            {
                fallbackPetrifyMaterial.SetColor("_BaseColor", color);
            }

            return fallbackPetrifyMaterial;
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

            hitFlashFeedback.Configure(tintRenderers, definition != null ? definition.accentColor : Color.white, RuntimeRenderBatchingUtility.UsePerInstanceUnitTint);
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

        private float GetEffectiveAttackPower()
        {
            return definition != null ? definition.stats.attackPower * outgameAttackMultiplier : 0f;
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
                        defender.TakeDamage(damage * splashDamageRatio, false, this);
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
                additionalTargets[i].TakeDamage(damage, false, this);
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
            if (tauntTarget == defender)
            {
                tauntTarget = null;
                tauntTimer = 0f;
            }
        }
    }
}
