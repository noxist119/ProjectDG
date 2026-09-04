using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseGame
{
	public class MonsterUnit : MonoBehaviour
	{
		public struct PetrifyTargetOptions
		{
			public float duration;

			public int maxTargets;

			public bool excludeBosses;

			public Material materialOverride;

			public Action<MonsterUnit> onApplied;
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

		private static readonly List<MonsterUnit> ActiveMonsters = new List<MonsterUnit>();

		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		private static readonly int ColorId = Shader.PropertyToID("_Color");

		private static Material defaultPetrifyMaterial;

		private static Material fallbackPetrifyMaterial;

		[SerializeField]
		private Renderer[] tintRenderers;

		[SerializeField]
		private float facingOffsetY = 0f;

		[SerializeField]
		private float separationRadius = 0.74f;

		[SerializeField]
		private float separationStrength = 0.9f;

		[SerializeField]
		private float globalMoveSpeedMultiplier = 0.7f;

		[SerializeField]
		private GameObject deathEffectPrefab;

		[SerializeField]
		private Vector3 deathEffectOffset = new Vector3(0f, 0.6f, 0f);

		[SerializeField]
		private float bossDeathPresentationDelay = 1.35f;

		[SerializeField]
		private float bossDeathPulseRadius = 2.25f;

		[Header("Runtime Performance")]
		[SerializeField]
		[Min(0.02f)]
		private float targetRefreshInterval = 0.1f;

		[SerializeField]
		[Min(0.02f)]
		private float separationRefreshInterval = 0.08f;

		[Header("Ranged Combat Safety")]
		[SerializeField]
		[Min(0.5f)]
		private float maximumRangedAttackRange = 3f;

		[Header("Melee Contact Combat")]
		[SerializeField]
		[Min(0.8f)]
		private float maximumMeleeAttackRange = 1.6f;

		[SerializeField]
		[Range(0.03f, 0.35f)]
		private float meleeContactClearance = 0.12f;

		[SerializeField]
		[Range(0f, 0.5f)]
		private float retaliationRangeMargin = 0.12f;

		[SerializeField]
		[Min(0.5f)]
		private float defaultRushCastRange = 3.6f;

		[SerializeField]
		[Min(0.5f)]
		private float defaultRallyRadius = 5.5f;

		private MonsterDefinition definition;

		private Transform goal;

		private Vector3 laneGoalPosition;

		private Vector3 laneTravelDirection;

		private bool escapeHandled;

		private FloatingCombatUI floatingUi;

		private UnitAnimationDriver animationDriver;

		private HitFlashFeedback hitFlashFeedback;

		private DefenseGameController gameController;

		private float currentHealth;

		private float outgameHealthMultiplier = 1f;

		private float appliedFateBossHealthMultiplier = 1f;

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

		private float fateStatCrushRatio;

		private float stunTimer;

		private float petrifyTimer;

		private DefenderUnit tauntTarget;

		private float tauntTimer;

		private float damageReflectRatio;

		private float damageReflectTimer;

		private bool resolvingDamageReflect;

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

		private const float KnockbackDuration = 0.24f;

		private bool knockbackActive;

		private Vector3 knockbackStartPosition;

		private Vector3 knockbackTargetPosition;

		private float knockbackElapsed;

		private bool isDying;

		private readonly List<DefenderUnit> defenders = new List<DefenderUnit>();

		private readonly Dictionary<string, float> skillCooldowns = new Dictionary<string, float>();

		private readonly List<RendererMaterialSnapshot> petrifyMaterialSnapshots = new List<RendererMaterialSnapshot>();

		private readonly List<AnimatorSpeedSnapshot> petrifyAnimatorSnapshots = new List<AnimatorSpeedSnapshot>();

		private DefenderUnit cachedCombatTarget;

		private float nextTargetRefreshTime;

		private Vector3 cachedSeparationOffset;

		private float nextSeparationRefreshTime;

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

		public MonsterDefinition Definition => definition;

		public float CurrentHealth => currentHealth;

		public float MaxHealth => (definition != null) ? (definition.stats.maxHealth * outgameHealthMultiplier) : 0f;

		public float CurrentMana => currentMana;

		public bool IsBoss => definition != null && definition.IsBossLike;

		public bool IsStatusEffectImmune => IsBoss;

		public float ActiveDamageReflectRatio => (damageReflectTimer > 0f) ? damageReflectRatio : 0f;

		private bool IsMajorBoss => definition != null && definition.IsMajorBoss;

		public bool IsStunned => IsControlLocked;

		public bool IsPetrified => petrifyTimer > 0f;

        // Petrify locks the monster's actions and animation, but it remains on the board
        // as a valid combat target so defenders can keep damaging it.
        public bool CanBeCombatTargeted => !isDying && currentHealth > 0f;

		public DefenderUnit LastDamageSource { get; private set; }

		public SkillDefinition LastDamageSkill { get; private set; }

		public float CurrentAttackRange => (definition != null) ? GetEffectiveAttackRange() : 0f;

		private bool IsControlLocked => stunTimer > 0f || petrifyTimer > 0f;

		public static event Action<MonsterUnit> OnMonsterSpawned;

		public static event Action<MonsterUnit> OnMonsterKilled;

		public static event Action<MonsterUnit> OnMonsterEscaped;

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
				if (!(forwardDistance < 0f) && !(forwardDistance > checkedLength))
				{
					Vector3 closestPoint = direction * forwardDistance;
					if (!((offset - closestPoint).magnitude > checkedHalfWidth))
					{
						candidates.Add(new StatusTargetCandidate
						{
							target = monster,
							sortDistance = forwardDistance
						});
					}
				}
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
				if (CanReceiveControlStatus(monster, options.excludeBosses))
				{
					Vector3 offset = monster.transform.position - center;
					offset.y = 0f;
					float sqrDistance = offset.sqrMagnitude;
					if (!(sqrDistance > checkedRadiusSqr))
					{
						candidates.Add(new StatusTargetCandidate
						{
							target = monster,
							sortDistance = sqrDistance
						});
					}
				}
			}
			return ApplyPetrifyToCandidates(candidates, options);
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
			InvalidateRuntimeCaches();
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
			candidates.Sort((StatusTargetCandidate left, StatusTargetCandidate right) => left.sortDistance.CompareTo(right.sortDistance));
			int limit = ((options.maxTargets <= 0) ? candidates.Count : Mathf.Min(options.maxTargets, candidates.Count));
			int applied = 0;
			for (int i = 0; i < limit; i++)
			{
				MonsterUnit target = candidates[i].target;
				if (CanReceiveControlStatus(target, options.excludeBosses))
				{
					target.ApplyPetrify(options.duration, options.materialOverride);
					options.onApplied?.Invoke(target);
					applied++;
				}
			}
			return applied;
		}

		private static bool CanReceiveControlStatus(MonsterUnit monster, bool excludeBosses = false)
		{
			return monster != null && !monster.isDying && monster.currentHealth > 0f && !monster.IsStatusEffectImmune && (!excludeBosses || !monster.IsBoss);
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
			if (!(template == null))
			{
				if (deathEffectPrefab == null)
				{
					deathEffectPrefab = template.deathEffectPrefab;
				}
				if (tintRenderers == null || tintRenderers.Length == 0)
				{
					tintRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
				}
				EnsureAnimationDriver();
				EnsureHitFlashFeedback();
			}
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
			if (knockbackActive)
			{
				TickKnockback();
				floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
				return;
			}
			if (isDying)
			{
				animationDriver?.PlayMoving(isMoving: false);
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
				animationDriver?.PlayMoving(isMoving: false);
				return;
			}
			DefenderUnit forcedTarget = ResolveTauntTarget();
			if (forcedTarget == null && TryCastSkill())
			{
				return;
			}
			DefenderUnit target = ((forcedTarget != null) ? forcedTarget : FindNearestDefender());
			if (target != null)
			{
				float attackRange = GetEffectiveAttackRange(target);
				Vector3 targetOffset = target.transform.position - base.transform.position;
				targetOffset.y = 0f;
				bool isInsideAttackRange = targetOffset.sqrMagnitude <= attackRange * attackRange;
				bool canBeCounterAttacked = !IsRangedAttacker() || CanAnyLivingDefenderRetaliate();
				if (isInsideAttackRange && canBeCounterAttacked)
				{
					if (attackCooldown <= 0f && CanStartActionAnimation())
					{
						PerformAttack(target);
					}
					animationDriver?.PlayMoving(isMoving: false);
					return;
				}
				if (isInsideAttackRange && !canBeCounterAttacked)
				{
					MoveTowardsDefender(target);
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

		public void Initialize(MonsterDefinition newDefinition, Transform goalPoint, int spawnRound = 0, float runtimeHealthMultiplier = 1f, float runtimeAttackMultiplier = 1f)
		{
			definition = newDefinition;
			outgameHealthMultiplier = 1f;
			appliedFateBossHealthMultiplier = 1f;
			outgameAttackMultiplier = 1f;
			if (OutgameProgressionSystem.Active != null)
			{
				OutgameProgressionSystem.Active.ResolveMonsterBalanceMultipliers(definition, out outgameHealthMultiplier, out outgameAttackMultiplier);
			}
			CommercialRoundPacing.ResolveCombatMultipliers(spawnRound, definition != null && definition.IsBossLike, out var hurdleHealthMultiplier, out var hurdleAttackMultiplier);
			outgameHealthMultiplier *= hurdleHealthMultiplier * Mathf.Max(0.1f, runtimeHealthMultiplier);
			outgameAttackMultiplier *= hurdleAttackMultiplier * Mathf.Max(0.1f, runtimeAttackMultiplier);
			DailyFortuneRule fortune = DailyFortuneSystem.Today;
			if (fortune != null && definition != null && definition.IsBossLike)
			{
				outgameHealthMultiplier *= fortune.BossHealthMultiplier;
			}
			if (definition != null && definition.IsBossLike && DefenseGameController.Active != null)
			{
				appliedFateBossHealthMultiplier = Mathf.Max(1f, DefenseGameController.Active.FateDebtBossHealthMultiplier);
				outgameHealthMultiplier *= appliedFateBossHealthMultiplier;
			}
			goal = goalPoint;
			laneGoalPosition = ((goal != null) ? new Vector3(base.transform.position.x, goal.position.y, goal.position.z) : base.transform.position);
			laneTravelDirection = ResolveLaneTravelDirection(base.transform.position, laneGoalPosition);
			escapeHandled = false;
			FaceTarget(laneGoalPosition);
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
			fateStatCrushRatio = 0f;
			stunTimer = 0f;
			petrifyTimer = 0f;
			tauntTarget = null;
			tauntTimer = 0f;
			damageReflectRatio = 0f;
			damageReflectTimer = 0f;
			resolvingDamageReflect = false;
			enraged = false;
			roleTraitTriggered = false;
			skillCastLocked = false;
			isDying = false;
			knockbackActive = false;
			knockbackElapsed = 0f;
			RestorePetrifyMaterials(reapplyVisuals: false);
			RestorePetrifyAnimations(resumeAnimation: false);
			defenders.Clear();
			IReadOnlyList<DefenderUnit> activeDefenders = DefenderUnit.ActiveInstances;
			for (int i = 0; i < activeDefenders.Count; i++)
			{
				DefenderUnit defender = activeDefenders[i];
				if (defender != null)
				{
					defenders.Add(defender);
				}
			}
			InvalidateRuntimeCaches();
			skillCooldowns.Clear();
			if (gameController == null)
			{
				gameController = UnityEngine.Object.FindObjectOfType<DefenseGameController>();
			}
			base.gameObject.name = definition.displayName;
			ApplyVisuals();
			EnsureAnimationDriver();
			UnitAnimatorLodController.AttachOrRefresh(base.gameObject, animationDriver, defender: false, IsBoss);
			EnsureHitFlashFeedback();
			floatingUi = FloatingCombatUI.Attach(base.transform, definition.displayName, definition.accentColor, definition.grade, GetFloatingUiFallbackHeight());
			if (gameController != null && gameController.FateMonsterStatCrushActive)
			{
				ApplyFateStatCrush(gameController.FateMonsterStatCrushRatio);
			}
			floatingUi.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
			animationDriver?.PlaySpawn();
			MonsterUnit.OnMonsterSpawned?.Invoke(this);
		}

		public bool RefreshFateDebtBossHealthPressure()
		{
			if (!IsBoss || currentHealth <= 0f || DefenseGameController.Active == null)
			{
				return false;
			}
			float targetMultiplier = Mathf.Max(1f, DefenseGameController.Active.FateDebtBossHealthMultiplier);
			float previousMultiplier = Mathf.Max(1f, appliedFateBossHealthMultiplier);
			if (targetMultiplier <= previousMultiplier + 0.001f)
			{
				return false;
			}
			float previousMaxHealth = MaxHealth;
			outgameHealthMultiplier *= targetMultiplier / previousMultiplier;
			appliedFateBossHealthMultiplier = targetMultiplier;
			float addedHealth = Mathf.Max(0f, MaxHealth - previousMaxHealth);
			currentHealth = Mathf.Min(MaxHealth, currentHealth + addedHealth);
			floatingUi?.ShowStatus("운명 대가  HP +" + Mathf.RoundToInt((targetMultiplier - previousMultiplier) * 100f) + "%", new Color(1f, 0.3f, 0.38f), 1.8f);
			floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
			RuntimeCombatFeedback.ShowGroundPulse(base.transform.position, new Color(0.95f, 0.2f, 0.32f), 1.25f, 0.7f, 0.12f);
			return addedHealth > 0f;
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
			if (definition.role == MonsterRole.Brute || definition.role == MonsterRole.Elite)
			{
				height = 1.72f;
			}
			else if (definition.role == MonsterRole.Caster)
			{
				height = 1.58f;
			}
			else if (definition.role == MonsterRole.Charger)
			{
				height = 1.42f;
			}
			if (definition.grade == CharacterGrade.Legendary)
			{
				height += 0.08f;
			}
			else if (definition.grade == CharacterGrade.Mythic)
			{
				height += 0.14f;
			}
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
			floatingUi?.ShowDamage(finalDamage, critical, healing: false);
			floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
			DefenderUnit.ReportDamageDealt(source, this, finalDamage, critical);
			if (source != null && !resolvingDamageReflect && damageReflectTimer > 0f && damageReflectRatio > 0f && finalDamage > 0f)
			{
				float reflectedDamage = finalDamage * Mathf.Clamp01(damageReflectRatio);
				resolvingDamageReflect = true;
				try
				{
					source.TakeDamage(reflectedDamage, critical: false, this);
					floatingUi?.ShowStatus("반사 " + Mathf.RoundToInt(reflectedDamage), new Color(0.58f, 1f, 0.48f), 0.65f);
					RuntimeCombatFeedback.ShowGroundPulse(source.transform.position, new Color(0.58f, 1f, 0.48f), 0.42f, 0.38f, 0.06f);
				}
				finally
				{
					resolvingDamageReflect = false;
				}
			}
			if (currentHealth <= 0f)
			{
				BeginDeath();
			}
		}

		public int GetRewardGold()
		{
			return (definition != null) ? definition.rewardGold : 0;
		}

		private void BeginDeath()
		{
			if (!isDying)
			{
				isDying = true;
				knockbackActive = false;
				currentHealth = 0f;
				skillCastLocked = false;
				ClearPendingImpacts();
				animationDriver?.PlayMoving(isMoving: false);
				floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
				PlayDeathEffect();
				if (IsBoss)
				{
					StartCoroutine(BossDeathPresentationRoutine());
				}
				else
				{
					CompleteDeath();
				}
			}
		}

		private IEnumerator BossDeathPresentationRoutine()
		{
			float duration = (IsMajorBoss ? bossDeathPresentationDelay : (bossDeathPresentationDelay * 0.75f));
			duration = Mathf.Clamp(duration, 0.75f, 2.25f);
			Color color = ((definition != null) ? definition.accentColor : new Color(1f, 0.32f, 0.18f));
			float radius = (IsMajorBoss ? bossDeathPulseRadius : (bossDeathPulseRadius * 0.72f));
			floatingUi?.ShowStatus(IsMajorBoss ? "BOSS DOWN" : "MID BOSS DOWN", Color.Lerp(color, Color.white, 0.35f), duration);
			RuntimeCombatFeedback.ShowBossDefeat(base.transform.position, color, radius, duration);
			RuntimeCameraShake.Request(IsMajorBoss ? 0.18f : 0.11f, Mathf.Min(0.5f, duration * 0.42f));
			RuntimeAudioUtility.PlayHit();
			Vector3 originalScale = base.transform.localScale;
			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				float pulse = Mathf.Sin(t * MathF.PI * 4f) * (1f - t) * 0.055f;
				base.transform.localScale = originalScale * (1f + pulse);
				yield return null;
			}
			base.transform.localScale = originalScale;
			CompleteDeath();
		}

		private void CompleteDeath()
		{
			MonsterUnit.OnMonsterKilled?.Invoke(this);
			UnityEngine.Object.Destroy(base.gameObject);
		}

		public void Heal(float amount)
		{
			currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
			floatingUi?.ShowDamage(amount, critical: false, healing: true);
			floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
		}

		public void ApplySlow(float ratio, float duration)
		{
			if (CanBeCombatTargeted && !IsStatusEffectImmune)
			{
				slowRatio = Mathf.Max(slowRatio, Mathf.Clamp01(ratio));
				slowTimer = Mathf.Max(slowTimer, duration);
				hitFlashFeedback?.PlayHit(critical: false);
			}
		}

		public void ApplyAttackSpeedSlow(float ratio, float duration)
		{
			if (CanBeCombatTargeted && !IsStatusEffectImmune)
			{
				attackSpeedSlowRatio = Mathf.Max(attackSpeedSlowRatio, Mathf.Clamp01(ratio));
				attackSpeedSlowTimer = Mathf.Max(attackSpeedSlowTimer, duration);
				hitFlashFeedback?.PlayHit(critical: false);
			}
		}

		public void ApplyFateStatCrush(float ratio)
		{
			if (CanBeCombatTargeted)
			{
				float safeRatio = Mathf.Clamp01(ratio);
				fateStatCrushRatio = Mathf.Max(fateStatCrushRatio, safeRatio);
				currentHealth = Mathf.Max(1f, currentHealth * (1f - safeRatio));
				currentMana = 0f;
				attackCooldown = Mathf.Max(attackCooldown, 1.2f);
				hitFlashFeedback?.PlayHit(critical: true);
				floatingUi?.ShowDamage(MaxHealth * safeRatio, critical: true, healing: false);
				floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
			}
		}

		public void ApplyStun(float duration)
		{
			if (CanBeCombatTargeted && !IsStatusEffectImmune)
			{
				float effectiveDuration = ResolveControlDuration(duration);
				if (!(effectiveDuration <= 0f))
				{
					stunTimer = Mathf.Max(stunTimer, effectiveDuration);
					hitFlashFeedback?.PlayHit(critical: true);
					floatingUi?.ShowTimedStatus("STUN · 행동 불가", new Color(1f, 0.88f, 0.22f, 1f), effectiveDuration);
					RuntimeCombatFeedback.ShowGroundPulse(base.transform.position, new Color(1f, 0.82f, 0.18f, 1f), IsBoss ? 0.82f : 0.58f, 0.62f, 0.1f);
				}
			}
		}

		public void ApplyPetrify(float duration, Material materialOverride = null)
		{
			if (CanReceiveControlStatus(this))
			{
				float effectiveDuration = ResolveControlDuration(duration);
				if (!(effectiveDuration <= 0f))
				{
					petrifyTimer = Mathf.Max(petrifyTimer, effectiveDuration);
					attackCooldown = Mathf.Max(attackCooldown, Mathf.Min(effectiveDuration, 1.2f));
					skillCastLocked = false;
					ClearPendingImpacts();
					animationDriver?.ForceIdle();
					FreezePetrifyAnimations();
					floatingUi?.ShowStatus("PETRIFY", new Color(0.72f, 0.78f, 0.82f, 1f), Mathf.Min(effectiveDuration, 1.2f));
					RuntimeCombatFeedback.ShowGroundPulse(base.transform.position, new Color(0.7f, 0.76f, 0.8f, 1f), IsBoss ? 0.95f : 0.62f, 0.46f, 0.09f);
					hitFlashFeedback?.PlayHit(critical: true);
					ApplyPetrifyMaterials(materialOverride);
				}
			}
		}

		private float ResolveControlDuration(float duration)
		{
			if (duration <= 0f || IsStatusEffectImmune)
			{
				return 0f;
			}
			return IsMajorBoss ? (duration * 0.45f) : (IsBoss ? (duration * 0.65f) : duration);
		}

		public bool IsKnockbackActive => knockbackActive;

		public void ApplyKnockback(float distance, Vector3 sourcePosition)
		{
			if (!CanBeCombatTargeted || IsStatusEffectImmune || distance <= 0f)
			{
				return;
			}

			Vector3 startPosition = base.transform.position;
			Vector3 awayFromGoal = startPosition - laneGoalPosition;
			awayFromGoal.y = 0f;
			if (awayFromGoal.sqrMagnitude <= 0.0001f)
			{
				awayFromGoal = startPosition - sourcePosition;
				awayFromGoal.y = 0f;
			}
			if (awayFromGoal.sqrMagnitude <= 0.0001f)
			{
				awayFromGoal = Vector3.back;
			}

			Vector3 pushedPosition = startPosition + awayFromGoal.normalized * distance;
			pushedPosition.x = Mathf.Clamp(pushedPosition.x, laneGoalPosition.x - 0.6f, laneGoalPosition.x + 0.6f);
			pushedPosition.y = startPosition.y;
			knockbackStartPosition = startPosition;
			knockbackTargetPosition = pushedPosition;
			knockbackElapsed = 0f;
			knockbackActive = true;
			ClearPendingImpacts();
			animationDriver?.PlayMoving(isMoving: true);
			hitFlashFeedback?.PlayHit(critical: true);
		}

		private void TickKnockback()
		{
			knockbackElapsed += Time.deltaTime;
			float normalized = Mathf.Clamp01(knockbackElapsed / KnockbackDuration);
			float eased = normalized * normalized * (3f - 2f * normalized);
			base.transform.position = Vector3.Lerp(knockbackStartPosition, knockbackTargetPosition, eased);
			FaceTarget(knockbackTargetPosition);
			animationDriver?.PlayMoving(isMoving: true);
			if (normalized >= 1f)
			{
				base.transform.position = knockbackTargetPosition;
				knockbackActive = false;
				animationDriver?.PlayMoving(isMoving: false);
			}
		}
		public void ApplyTaunt(DefenderUnit source, float duration)
		{
			if (CanBeCombatTargeted && !IsStatusEffectImmune && !(source == null) && !(source.CurrentHealth <= 0f) && !(duration <= 0f))
			{
				tauntTarget = source;
				tauntTimer = Mathf.Max(tauntTimer, duration);
				hitFlashFeedback?.PlayHit(critical: false);
			}
		}

		public void ApplyPoison(float damagePerTick, float duration, float tickInterval, DefenderUnit source)
		{
			if (CanBeCombatTargeted && !IsStatusEffectImmune)
			{
				StartCoroutine(PoisonRoutine(Mathf.Max(0f, damagePerTick), Mathf.Max(0f, duration), Mathf.Max(0.2f, tickInterval), source));
				hitFlashFeedback?.PlayHit(critical: false);
			}
		}

		public void ApplyRally(float amount, float duration)
		{
			attackSpeedBonus = Mathf.Max(attackSpeedBonus, amount);
			moveSpeedBonus = Mathf.Max(moveSpeedBonus, amount * 0.65f);
			attackSpeedBuffTimer = Mathf.Max(attackSpeedBuffTimer, duration);
			moveSpeedBuffTimer = Mathf.Max(moveSpeedBuffTimer, duration);
			hitFlashFeedback?.PlayHit(critical: false);
		}

		private IEnumerator PoisonRoutine(float damagePerTick, float duration, float tickInterval, DefenderUnit source)
		{
			for (float elapsed = 0f; elapsed < duration; elapsed += tickInterval)
			{
				if (!(currentHealth > 0f))
				{
					break;
				}
				TakeDamage(damagePerTick, critical: false, source);
				yield return new WaitForSeconds(tickInterval);
			}
		}

		private void MoveTowardsGoal()
		{
			if (goal == null || escapeHandled)
			{
				return;
			}

			Vector3 moveTarget = BuildMoveTarget();
			FaceTarget(moveTarget);
			float moveSpeed = definition.stats.moveSpeed * (1f + moveSpeedBonus) * (1f - slowRatio) * (1f - fateStatCrushRatio) * globalMoveSpeedMultiplier;
			base.transform.position = Vector3.MoveTowards(base.transform.position, moveTarget, moveSpeed * Time.deltaTime);
			animationDriver?.PlayMoving(isMoving: true);

			if (HasReachedLaneGoalLine(base.transform.position, laneGoalPosition, laneTravelDirection))
			{
				HandleEscapeOnce();
			}
		}

		private void HandleEscapeOnce()
		{
			if (escapeHandled)
			{
				return;
			}

			escapeHandled = true;
			MonsterUnit.OnMonsterEscaped?.Invoke(this);
			UnityEngine.Object.Destroy(base.gameObject);
		}
		private void MoveTowardsDefender(DefenderUnit target)
		{
			if (!(target == null))
			{
				Vector3 moveTarget = target.transform.position;
				moveTarget.y = base.transform.position.y;
				if (!IsRangedAttacker())
				{
					Vector3 fromTarget = base.transform.position - moveTarget;
					fromTarget.y = 0f;
					if (fromTarget.sqrMagnitude > 0.0001f)
					{
						moveTarget += fromTarget.normalized * GetEffectiveAttackRange(target);
					}
				}
				FaceTarget(moveTarget);
				float moveSpeed = definition.stats.moveSpeed * (1f + moveSpeedBonus) * (1f - slowRatio) * (1f - fateStatCrushRatio) * globalMoveSpeedMultiplier;
				base.transform.position = Vector3.MoveTowards(base.transform.position, moveTarget, moveSpeed * Time.deltaTime);
				animationDriver?.PlayMoving(isMoving: true);
			}
		}

		private void PerformAttack(DefenderUnit target)
		{
			if (!(target == null))
			{
				FaceTarget(target.transform.position);
				float effectiveAttackSpeed = Mathf.Max(0.2f, definition.stats.attackSpeed * (1f + attackSpeedBonus) * (1f - attackSpeedSlowRatio) * (1f - fateStatCrushRatio));
				attackCooldown = 1f / effectiveAttackSpeed;
				bool critical = UnityEngine.Random.value <= Mathf.Clamp01(definition.stats.criticalChance + critChanceBonus);
				float damage = GetEffectiveAttackPower() * (critical ? definition.stats.criticalDamageMultiplier : 1f);
				QueueBasicAttackImpact(target, damage, critical);
				if (animationDriver != null && animationDriver.PlayAttack())
				{
					SchedulePendingAttackFallback(pendingBasicAttack.sequence);
				}
				else
				{
					ResolvePendingBasicAttack();
				}
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
			pendingBasicAttack = default(PendingMonsterAttack);
			CancelPendingAttackFallback();
			if (!(pending.target == null))
			{
				FaceTarget(pending.target.transform.position);
				GainManaFromBasicAttack();
				if (!TryLaunchBasicAttackProjectile(pending))
				{
					PlayBasicAttackMuzzleEffect(pending.target);
					PlayBasicAttackHitEffect(pending.target);
					pending.target.TakeDamage(pending.damage, pending.critical, this);
					ApplyBasicAttackExtensions(pending.target, pending.damage);
				}
			}
		}

		private bool TryLaunchBasicAttackProjectile(PendingMonsterAttack pending)
		{
			GameObject projectilePrefab = ResolveBasicAttackProjectilePrefab();
			if (projectilePrefab == null || pending.target == null)
			{
				return false;
			}
			Vector3 startPosition = base.transform.position + Vector3.up * ResolveBasicAttackEffectHeight();
			Quaternion launchRotation = ((pending.target != null) ? RuntimeEffectUtility.FaceTowards(startPosition, pending.target.transform.position + Vector3.up * ResolveBasicAttackEffectHeight(), base.transform.rotation) : base.transform.rotation);
			Projectile projectile = Projectile.Spawn(projectilePrefab, startPosition, launchRotation);
			if (projectile == null)
			{
				return false;
			}
			PlayBasicAttackMuzzleEffect(pending.target);
			float speed = ((definition != null) ? Mathf.Max(2f, definition.stats.projectileSpeed) : 8f);
			projectile.Initialize(pending.target, pending.damage, speed, pending.critical, this, ResolveBasicAttackEffectHeight());
			return true;
		}

		private void ResolveBasicAttackDamage(PendingMonsterAttack pending)
		{
			ResolveBasicAttackProjectileImpact(pending.target, pending.damage, pending.critical);
		}

		internal void ResolveBasicAttackProjectileImpact(DefenderUnit target, float damage, bool critical)
		{
			if (!(target == null))
			{
				PlayBasicAttackHitEffect(target);
				target.TakeDamage(damage, critical, this);
				ApplyBasicAttackExtensions(target, damage);
			}
		}

		private float ResolveBasicAttackEffectHeight()
		{
			return IsBoss ? 0.72f : 0.48f;
		}

		private void PlayBasicAttackMuzzleEffect(DefenderUnit target)
		{
			GameObject effectPrefab = ResolveBasicAttackMuzzleEffectPrefab();
			if (!(effectPrefab == null))
			{
				Vector3 origin = base.transform.position + Vector3.up * 0.06f;
				Quaternion rotation = ((target != null) ? RuntimeEffectUtility.FaceTowards(origin, target.transform.position, base.transform.rotation) : base.transform.rotation);
				RuntimeEffectUtility.PlayOneShot(effectPrefab, origin, rotation, IsBoss ? 0.48f : 0.28f);
			}
		}

		private void PlayBasicAttackHitEffect(DefenderUnit target)
		{
			if (!(target == null))
			{
				GameObject effectPrefab = ResolveBasicAttackHitEffectPrefab();
				if (effectPrefab != null)
				{
					Quaternion rotation = RuntimeEffectUtility.FaceTowards(base.transform.position, target.transform.position, base.transform.rotation);
					RuntimeEffectUtility.PlayOneShot(effectPrefab, target.transform.position + Vector3.up * 0.06f, rotation, IsBoss ? 0.54f : 0.3f);
				}
				RuntimeAudioUtility.PlayHit();
				if (IsBoss)
				{
					RuntimeCameraShake.Request(IsMajorBoss ? 0.045f : 0.025f, IsMajorBoss ? 0.13f : 0.09f);
				}
			}
		}

		private GameObject ResolveBasicAttackProjectilePrefab()
		{
			return (definition != null && definition.attackBehavior != null) ? definition.attackBehavior.projectilePrefabOverride : null;
		}

		private GameObject ResolveBasicAttackMuzzleEffectPrefab()
		{
			return (definition != null && definition.attackBehavior != null) ? definition.attackBehavior.muzzleEffectPrefab : null;
		}

		private GameObject ResolveBasicAttackHitEffectPrefab()
		{
			return (definition != null && definition.attackBehavior != null) ? definition.attackBehavior.hitEffectPrefab : null;
		}

		private void GainManaFromBasicAttack()
		{
			if (definition != null && !(definition.stats.maxMana <= 0f) && !(definition.stats.manaGainPerAttackRate <= 0f))
			{
				currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * definition.stats.manaGainPerAttackRate);
				floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
			}
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
				float requiredMana = Mathf.Clamp((skill.manaThreshold <= 0f) ? definition.stats.maxMana : skill.manaThreshold, 0f, definition.stats.maxMana);
				if (!(currentMana < requiredMana) && CanCastSkill(skill) && (!skillCooldowns.TryGetValue(skill.id, out var cooldown) || !(cooldown > 0f)))
				{
					skillCooldowns[skill.id] = skill.cooldown;
					currentMana = 0f;
					animationDriver?.PlayMoving(isMoving: false);
					StartCoroutine(CastSkillSequence(skill));
					return true;
				}
			}
			return false;
		}

		private IEnumerator CastSkillSequence(SkillDefinition skill)
		{
			skillCastLocked = true;
			float warningDelay = (IsMajorBoss ? 0.85f : (IsBoss ? 0.45f : 0f));
			if (warningDelay > 0f)
			{
				NotifySkillWarning(skill, warningDelay);
				yield return new WaitForSeconds(warningDelay);
			}
			if (skill != null && skill.effectType == SkillEffectType.SummonRush && !skill.isGlobalTargeting)
			{
				yield return PerformRushApproach(skill);
			}
			CastSkill(skill);
			skillCastLocked = false;
		}

		private IEnumerator PerformRushApproach(SkillDefinition skill)
		{
			DefenderUnit target = FindNearestDefenderForSkill(skill);
			if (target == null)
			{
				yield break;
			}
			Vector3 startPosition = base.transform.position;
			Vector3 direction = target.transform.position - startPosition;
			direction.y = 0f;
			float distance = direction.magnitude;
			if (distance <= 0.1f)
			{
				yield break;
			}
			float desiredStopDistance = Mathf.Clamp(GetEffectiveAttackRange() * 0.55f, 0.9f, 1.45f);
			float travelDistance = Mathf.Min(1.25f, Mathf.Max(0f, distance - desiredStopDistance));
			if (!(travelDistance <= 0.05f))
			{
				Vector3 endPosition = startPosition + direction.normalized * travelDistance;
				FaceTarget(target.transform.position);
				animationDriver?.PlayMoving(isMoving: true);
				float elapsed = 0f;
				while (elapsed < 0.18f && target != null)
				{
					elapsed += Time.deltaTime;
					base.transform.position = Vector3.Lerp(startPosition, endPosition, Mathf.Clamp01(elapsed / 0.18f));
					yield return null;
				}
				base.transform.position = endPosition;
				animationDriver?.PlayMoving(isMoving: false);
			}
		}

		private bool CanCastSkill(SkillDefinition skill)
		{
			if (skill == null)
			{
				return false;
			}
			if (RequiresMeleeEngagementForSkill(skill) && !HasDefenderWithinSkillRange(skill))
			{
				return false;
			}
			if (skill.effectType == SkillEffectType.DamageReflect)
			{
				return damageReflectTimer <= 0.05f && CountLivingDefenders() > 0 && HasDefenderWithinSkillRange(skill);
			}
			if (skill.effectType == SkillEffectType.GoldDrain)
			{
				return skill.isGlobalTargeting && CountLivingDefenders() > 0;
			}
			if (SkillTargetsDefenders(skill))
			{
				if (CountLivingDefenders() <= 0)
				{
					return false;
				}
				return skill.isGlobalTargeting || HasDefenderWithinSkillRange(skill);
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
			int skillSlot = ((definition == null || definition.skills == null) ? 1 : Mathf.Max(1, definition.skills.IndexOf(skill) + 1));
			bool usedAttackAnimationFallback = false;
			bool animationStarted = animationDriver != null && animationDriver.PlaySkill(skillSlot);
			if (!animationStarted && animationDriver != null && SkillTargetsDefenders(skill))
			{
				usedAttackAnimationFallback = animationDriver.PlayAttack();
				animationStarted = usedAttackAnimationFallback;
			}
			NotifySkillPresentation(skill);
			if (animationStarted)
			{
				float fallbackDelay = (usedAttackAnimationFallback ? animationDriver.AttackImpactFallbackDelay : animationDriver.SkillImpactFallbackDelay);
				SchedulePendingSkillFallback(pendingSkillCast.sequence, fallbackDelay);
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
				DefenderUnit singleTarget = (skill.isGlobalTargeting ? FindNearestDefender() : FindNearestDefenderForSkill(skill));
				if (singleTarget != null)
				{
					FaceTarget(singleTarget.transform.position);
					ShowSkillImpactFeedback(singleTarget.transform.position, skill, 0.72f, IsBoss);
					float damage = GetEffectiveAttackPower() * skill.power;
					singleTarget.TakeDamage(damage, critical: false, this);
					bossAffectedTargets++;
					bossDamageDone += damage;
				}
			}
			else if (skill.effectType == SkillEffectType.AreaDamage)
			{
				List<DefenderUnit> targets = GetLivingDefendersWithinRange(Mathf.Max(0.1f, skill.radius));
				if (targets.Count > 0)
				{
					FaceTarget(targets[0].transform.position);
				}
				ShowSkillImpactFeedback(base.transform.position, skill, Mathf.Max(0.8f, skill.radius), IsBoss);
				for (int i = 0; i < targets.Count; i++)
				{
					float damage2 = GetEffectiveAttackPower() * skill.power;
					targets[i].TakeDamage(damage2, critical: false, this);
					bossAffectedTargets++;
					bossDamageDone += damage2;
				}
			}
			else if (skill.effectType == SkillEffectType.HealSelf)
			{
				Heal(MaxHealth * skill.power);
				ShowSkillImpactFeedback(base.transform.position, skill, 0.82f, shake: false);
				bossAffectedTargets = 1;
			}
			else if (skill.effectType == SkillEffectType.AttackSpeedBoost)
			{
				attackSpeedBonus = skill.power;
				attackSpeedBuffTimer = skill.duration;
				ShowSkillImpactFeedback(base.transform.position, skill, 0.82f, shake: false);
				bossAffectedTargets = 1;
			}
			else if (skill.effectType == SkillEffectType.CriticalBoost)
			{
				critChanceBonus = skill.power;
				critBuffTimer = skill.duration;
				ShowSkillImpactFeedback(base.transform.position, skill, 0.82f, shake: false);
				bossAffectedTargets = 1;
			}
			else if (skill.effectType == SkillEffectType.MoveSpeedBoost)
			{
				moveSpeedBonus = skill.power;
				moveSpeedBuffTimer = skill.duration;
				ShowSkillImpactFeedback(base.transform.position, skill, 0.82f, shake: false);
				bossAffectedTargets = 1;
			}
			else if (skill.effectType == SkillEffectType.ManaSurge)
			{
				currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * skill.power);
				ShowSkillImpactFeedback(base.transform.position, skill, 0.82f, shake: false);
				bossAffectedTargets = 1;
			}
			else if (skill.effectType == SkillEffectType.SummonRush)
			{
				float rushRange = ResolveSkillCastRange(skill);
				List<DefenderUnit> targets2 = GetLivingDefendersWithinRange(rushRange);
				int hitLimit = Mathf.Max(0, skill.hitCount);
				if (targets2.Count > hitLimit)
				{
					targets2.RemoveRange(hitLimit, targets2.Count - hitLimit);
				}
				if (targets2.Count > 0)
				{
					FaceTarget(targets2[0].transform.position);
				}
				for (int j = 0; j < targets2.Count; j++)
				{
					ShowSkillImpactFeedback(targets2[j].transform.position, skill, 0.64f, shake: false);
					float damage3 = GetEffectiveAttackPower() * skill.power;
					targets2[j].TakeDamage(damage3, critical: false, this);
					bossAffectedTargets++;
					bossDamageDone += damage3;
				}
			}
			else if (skill.effectType == SkillEffectType.Stun)
			{
				DefenderUnit target = (skill.isGlobalTargeting ? FindNearestDefender() : FindNearestDefenderForSkill(skill));
				if (target != null)
				{
					FaceTarget(target.transform.position);
					ShowSkillImpactFeedback(target.transform.position, skill, 0.7f, IsBoss);
					target.ApplyStun(skill.duration);
					bossAffectedTargets++;
				}
			}
			else if (skill.effectType == SkillEffectType.MassStun)
			{
				if (IsBoss)
				{
					ShowSkillImpactFeedback(base.transform.position, skill, ResolveSkillFeedbackRadius(skill), shake: true);
				}
				List<DefenderUnit> targets3 = (skill.isGlobalTargeting ? GetRandomDefenders(Mathf.Max(1, skill.hitCount)) : GetRandomDefendersWithinRange(Mathf.Max(1, skill.hitCount), ResolveSkillCastRange(skill)));
				if (targets3.Count > 0)
				{
					FaceTarget(targets3[0].transform.position);
				}
				for (int k = 0; k < targets3.Count; k++)
				{
					ShowSkillImpactFeedback(targets3[k].transform.position, skill, 0.66f, k == 0 && IsBoss);
					targets3[k].ApplyStun(skill.duration);
					float damage4 = GetEffectiveAttackPower() * Mathf.Max(0f, skill.power);
					if (damage4 > 0f)
					{
						targets3[k].TakeDamage(damage4, critical: false, this);
						bossDamageDone += damage4;
					}
					bossAffectedTargets++;
				}
			}
			else if (skill.effectType == SkillEffectType.DeathPact)
			{
				DefenderUnit target2 = (skill.isGlobalTargeting ? GetRandomDefender() : GetRandomDefenderWithinRange(ResolveSkillCastRange(skill)));
				if (target2 != null)
				{
					FaceTarget(target2.transform.position);
					ShowSkillImpactFeedback(target2.transform.position, skill, 0.92f, shake: true);
					target2.KillByBossSkill();
					RuntimeCameraShake.Request(0.12f, 0.28f);
					bossAffectedTargets++;
				}
			}
			else if (skill.effectType == SkillEffectType.BossFortify)
			{
				Heal(MaxHealth * skill.power);
				attackSpeedBonus = Mathf.Max(attackSpeedBonus, 0.12f);
				attackSpeedBuffTimer = Mathf.Max(attackSpeedBuffTimer, skill.duration);
				ShowSkillImpactFeedback(base.transform.position, skill, 1.05f, shake: true);
				bossAffectedTargets = 1;
			}
			else if (skill.effectType == SkillEffectType.GoldDrain)
			{
				int drain = Mathf.Max(1, Mathf.RoundToInt(skill.power));
				int removed = ((gameController != null) ? gameController.RemoveGold(drain) : 0);
				if (removed > 0)
				{
					gameController.RequestBanner("탐욕의 징수  -" + removed + "G", definition.accentColor, 2.2f);
				}
				bossGoldDrained = removed;
				bossAffectedTargets = ((removed > 0) ? 1 : 0);
				ShowSkillImpactFeedback(base.transform.position, skill, 0.86f, shake: false);
			}
			else if (skill.effectType == SkillEffectType.ManaBurn)
			{
				List<DefenderUnit> targets4 = (skill.isGlobalTargeting ? GetRandomDefenders(Mathf.Max(1, skill.hitCount)) : GetRandomDefendersWithinRange(Mathf.Max(1, skill.hitCount), ResolveSkillCastRange(skill)));
				if (targets4.Count > 0)
				{
					FaceTarget(targets4[0].transform.position);
				}
				for (int l = 0; l < targets4.Count; l++)
				{
					ShowSkillImpactFeedback(targets4[l].transform.position, skill, 0.66f, l == 0 && IsBoss);
					targets4[l].DrainMana(skill.power);
					bossAffectedTargets++;
				}
			}
			else if (skill.effectType == SkillEffectType.MonsterRally)
			{
				IReadOnlyList<MonsterUnit> allies = ActiveInstances;
				float rallyRadius = ((skill.radius > 0.1f) ? skill.radius : defaultRallyRadius);
				for (int m = 0; m < allies.Count; m++)
				{
					if (allies[m] != null && (skill.isGlobalTargeting || Vector3.Distance(base.transform.position, allies[m].transform.position) <= rallyRadius))
					{
						allies[m].ApplyRally(skill.power, skill.duration);
						ShowSkillImpactFeedback(allies[m].transform.position, skill, (allies[m] == this) ? 0.92f : 0.62f, allies[m] == this && IsBoss);
						bossAffectedTargets++;
					}
				}
			}
			else if (skill.effectType == SkillEffectType.AttackPowerReduction)
			{
				DefenderUnit target3 = FindNearestDefenderForSkill(skill);
				if (target3 != null)
				{
					FaceTarget(target3.transform.position);
					ShowSkillImpactFeedback(target3.transform.position, skill, 0.72f, IsBoss);
					target3.ApplyAttackPowerReduction(skill.power, skill.duration, skill.hitEffectPrefab);
					bossAffectedTargets = 1;
				}
			}
			else if (skill.effectType == SkillEffectType.DamageReflect)
			{
				damageReflectRatio = Mathf.Max(damageReflectRatio, Mathf.Clamp01(skill.power));
				damageReflectTimer = Mathf.Max(damageReflectTimer, skill.duration);
				ShowSkillImpactFeedback(base.transform.position, skill, 1.05f, shake: false);
				floatingUi?.ShowTimedStatus("피해 반사 " + Mathf.RoundToInt(damageReflectRatio * 100f) + "%", new Color(0.58f, 1f, 0.48f), damageReflectTimer);
				bossAffectedTargets = 1;
			}
			if (IsBoss && gameController != null)
			{
				gameController.RecordBossSkillImpact(skill, bossAffectedTargets, bossDamageDone, bossGoldDrained, IsMajorBoss);
				if (IsMajorBoss)
				{
					string result = ((bossAffectedTargets > 0) ? (bossAffectedTargets + "기 적중") : "범위 밖");
					result = ((!(bossDamageDone > 0f)) ? (result + ((bossAffectedTargets > 0) ? "  |  상태/버프 적용" : "  |  피해 없음")) : (result + "  |  피해 " + Mathf.RoundToInt(bossDamageDone)));
					gameController.RequestBanner("보스 적중: " + skill.displayName + "  |  " + result, ResolveSkillFeedbackColor(skill), 2.1f);
				}
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
			if (pendingSkillCast.isValid)
			{
				PendingMonsterSkill pending = pendingSkillCast;
				pendingSkillCast = default(PendingMonsterSkill);
				CancelPendingSkillFallback();
				ApplySkillEffect(pending.skill);
			}
		}

		private void HandleAnimationImpact(AnimationImpactType impactType)
		{
			if (ShouldResolvePendingBasicAttack(impactType))
			{
				ResolvePendingBasicAttack();
			}
			else if ((impactType == AnimationImpactType.Skill || impactType == AnimationImpactType.Attack || impactType == AnimationImpactType.AttackHit || impactType == AnimationImpactType.FireProjectile) && pendingSkillCast.isValid && !pendingBasicAttack.isValid)
			{
				ResolvePendingSkillCast();
			}
			else if (impactType == AnimationImpactType.Auto)
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
			return (definition != null && definition.attackBehavior != null && definition.attackBehavior.IsMelee) ? (impactType == AnimationImpactType.AttackHit) : (impactType == AnimationImpactType.FireProjectile);
		}

		private void SchedulePendingAttackFallback(int sequence)
		{
			CancelPendingAttackFallback();
			float delay = ((animationDriver != null) ? animationDriver.AttackImpactFallbackDelay : 0.12f);
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

		private void SchedulePendingSkillFallback(int sequence, float fallbackDelay = -1f)
		{
			CancelPendingSkillFallback();
			float delay = ((fallbackDelay > 0f) ? fallbackDelay : ((animationDriver != null) ? animationDriver.SkillImpactFallbackDelay : 0.2f));
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
			pendingBasicAttack = default(PendingMonsterAttack);
			pendingSkillCast = default(PendingMonsterSkill);
			CancelPendingAttackFallback();
			CancelPendingSkillFallback();
		}

		private void FaceTarget(Vector3 targetPosition)
		{
			Vector3 direction = targetPosition - base.transform.position;
			direction.y = 0f;
			if (!(direction.sqrMagnitude <= 0.0001f))
			{
				Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
				base.transform.rotation = lookRotation * Quaternion.Euler(0f, facingOffsetY, 0f);
			}
		}

		private Vector3 BuildMoveTarget()
		{
			if (Time.unscaledTime >= nextSeparationRefreshTime)
			{
				RefreshSeparationOffset();
			}
			Vector3 target = laneGoalPosition + cachedSeparationOffset;
			target.x = Mathf.Clamp(target.x, laneGoalPosition.x - 0.6f, laneGoalPosition.x + 0.6f);
			return target;
		}

		public static Vector3 ResolveLateralSeparationOffset(Vector3 separation, Vector3 travelDirection, float strength)
		{
			travelDirection.y = 0f;
			if (strength <= 0f || travelDirection.sqrMagnitude <= 0.0001f)
			{
				return Vector3.zero;
			}

			Vector3 lateral = Vector3.ProjectOnPlane(separation, travelDirection.normalized);
			lateral.y = 0f;
			return lateral.sqrMagnitude > 0.0001f ? lateral.normalized * strength : Vector3.zero;
		}

		public static bool HasReachedLaneGoalLine(Vector3 position, Vector3 goalPosition, Vector3 travelDirection)
		{
			travelDirection.y = 0f;
			if (travelDirection.sqrMagnitude <= 0.0001f)
			{
				return Vector3.Distance(position, goalPosition) <= 0.05f;
			}

			Vector3 remaining = goalPosition - position;
			remaining.y = 0f;
			return Vector3.Dot(remaining, travelDirection.normalized) <= 0.05f;
		}

		private static Vector3 ResolveLaneTravelDirection(Vector3 startPosition, Vector3 goalPosition)
		{
			Vector3 direction = goalPosition - startPosition;
			direction.y = 0f;
			return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
		}

		private void RefreshSeparationOffset()
		{
			Vector3 separation = Vector3.zero;
			float checkedRadius = Mathf.Max(0.01f, separationRadius);
			float checkedRadiusSqr = checkedRadius * checkedRadius;
			IReadOnlyList<MonsterUnit> others = ActiveInstances;
			for (int i = 0; i < others.Count; i++)
			{
				MonsterUnit other = others[i];
				if (!(other == null) && !(other == this))
				{
					Vector3 delta = base.transform.position - other.transform.position;
					delta.y = 0f;
					float sqrDistance = delta.sqrMagnitude;
					if (!(sqrDistance <= 0.0001f) && !(sqrDistance > checkedRadiusSqr))
					{
						float distance = Mathf.Sqrt(sqrDistance);
						separation += delta / distance * ((checkedRadius - distance) / checkedRadius);
					}
				}
			}
			cachedSeparationOffset = ResolveLateralSeparationOffset(separation, laneTravelDirection, separationStrength);
			nextSeparationRefreshTime = CombatRuntimeQuery.ScheduleNextRefresh(this, separationRefreshInterval);
		}

		private DefenderUnit FindNearestDefender()
		{
			if (CombatRuntimeQuery.IsValidDefenderTarget(cachedCombatTarget) && Time.unscaledTime < nextTargetRefreshTime)
			{
				return cachedCombatTarget;
			}
			cachedCombatTarget = CombatRuntimeQuery.FindNearestDefender(defenders, base.transform.position);
			nextTargetRefreshTime = CombatRuntimeQuery.ScheduleNextRefresh(this, targetRefreshInterval);
			return cachedCombatTarget;
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
			return (candidates.Count > 0) ? candidates[0] : null;
		}

		private List<DefenderUnit> GetRandomDefenders(int count)
		{
			List<DefenderUnit> candidates = new List<DefenderUnit>();
			for (int i = 0; i < defenders.Count; i++)
			{
				DefenderUnit defender = defenders[i];
				if (IsLivingDefender(defender))
				{
					candidates.Add(defender);
				}
			}
			ShuffleAndTrim(candidates, count);
			return candidates;
		}

		private static void ShuffleAndTrim(List<DefenderUnit> candidates, int count)
		{
			if (candidates != null)
			{
				for (int i = 0; i < candidates.Count; i++)
				{
					int swapIndex = UnityEngine.Random.Range(i, candidates.Count);
					DefenderUnit temp = candidates[i];
					candidates[i] = candidates[swapIndex];
					candidates[swapIndex] = temp;
				}
				int limit = Mathf.Max(0, count);
				if (candidates.Count > limit)
				{
					candidates.RemoveRange(limit, candidates.Count - limit);
				}
			}
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
				}
				else if (defender.CurrentHealth > 0f)
				{
					count++;
				}
			}
			return count;
		}

		private void NotifySkillPresentation(SkillDefinition skill)
		{
			ShowSkillCastFeedback(skill);
			if (skill != null && !(gameController == null) && IsBoss)
			{
				bool majorBossSkill = skill.effectType == SkillEffectType.DeathPact || skill.effectType == SkillEffectType.MassStun || skill.effectType == SkillEffectType.BossFortify || skill.effectType == SkillEffectType.DirectDamage || skill.effectType == SkillEffectType.AreaDamage || skill.effectType == SkillEffectType.SummonRush || skill.effectType == SkillEffectType.GoldDrain || skill.effectType == SkillEffectType.ManaBurn || skill.effectType == SkillEffectType.MonsterRally;
				string prefix = (IsMajorBoss ? "보스 스킬: " : "중간보스 스킬: ");
				gameController.RecordBossSkillCast(skill, IsMajorBoss);
				gameController.RequestBanner(prefix + skill.displayName + " 발동!", definition.accentColor, majorBossSkill ? 2.6f : 2f);
			}
		}

		private bool IsRangedAttacker()
		{
			return definition != null && definition.attackBehavior != null && definition.attackBehavior.basicAttackType == BasicAttackType.Ranged;
		}

		private bool CanAnyLivingDefenderRetaliate()
		{
			for (int i = defenders.Count - 1; i >= 0; i--)
			{
				DefenderUnit defender = defenders[i];
				if (!IsLivingDefender(defender))
				{
					if (defender == null)
					{
						defenders.RemoveAt(i);
					}
				}
				else
				{
					float safeCounterRange = Mathf.Max(0.1f, defender.CurrentAttackRange - retaliationRangeMargin);
					if ((base.transform.position - defender.transform.position).sqrMagnitude <= safeCounterRange * safeCounterRange)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool IsLivingDefender(DefenderUnit defender)
		{
			return CombatRuntimeQuery.IsValidDefenderTarget(defender);
		}

		private static bool SkillTargetsDefenders(SkillDefinition skill)
		{
			if (skill == null)
			{
				return false;
			}
			return skill.effectType == SkillEffectType.DeathPact || skill.effectType == SkillEffectType.Stun || skill.effectType == SkillEffectType.MassStun || skill.effectType == SkillEffectType.SummonRush || skill.effectType == SkillEffectType.DirectDamage || skill.effectType == SkillEffectType.AreaDamage || skill.effectType == SkillEffectType.ManaBurn || skill.effectType == SkillEffectType.AttackPowerReduction;
		}

		private bool RequiresMeleeEngagementForSkill(SkillDefinition skill)
		{
			if (skill == null || IsRangedAttacker() || skill.isGlobalTargeting)
			{
				return false;
			}

			return skill.effectType != SkillEffectType.MoveSpeedBoost &&
				skill.effectType != SkillEffectType.SummonRush &&
				skill.effectType != SkillEffectType.MonsterRally;
		}

		private bool HasDefenderWithinSkillRange(SkillDefinition skill)
		{
			return FindNearestDefenderForSkill(skill) != null;
		}

		private DefenderUnit FindNearestDefenderForSkill(SkillDefinition skill)
		{
			float bestSqrDistance = float.MaxValue;
			DefenderUnit bestTarget = null;
			Vector3 origin = base.transform.position;
			for (int i = defenders.Count - 1; i >= 0; i--)
			{
				DefenderUnit defender = defenders[i];
				if (!IsLivingDefender(defender))
				{
					if (defender == null)
					{
						defenders.RemoveAt(i);
					}
				}
				else
				{
					float sqrDistance = (origin - defender.transform.position).sqrMagnitude;
					float castRange = ResolveSkillCastRange(skill, defender);
					if (sqrDistance <= castRange * castRange && sqrDistance <= bestSqrDistance)
					{
						bestSqrDistance = sqrDistance;
						bestTarget = defender;
					}
				}
			}
			return bestTarget;
		}

		private List<DefenderUnit> GetLivingDefendersWithinRange(float range)
		{
			float safeRange = Mathf.Max(0.1f, range);
			float safeRangeSqr = safeRange * safeRange;
			Vector3 origin = base.transform.position;
			List<DefenderUnit> result = new List<DefenderUnit>();
			for (int i = defenders.Count - 1; i >= 0; i--)
			{
				DefenderUnit defender = defenders[i];
				if (!IsLivingDefender(defender))
				{
					if (defender == null)
					{
						defenders.RemoveAt(i);
					}
				}
				else
				{
					float sqrDistance = (origin - defender.transform.position).sqrMagnitude;
					if (!(sqrDistance > safeRangeSqr))
					{
						int insertIndex = result.Count;
						while (insertIndex > 0 && (origin - result[insertIndex - 1].transform.position).sqrMagnitude > sqrDistance)
						{
							insertIndex--;
						}
						result.Insert(insertIndex, defender);
					}
				}
			}
			return result;
		}

		private DefenderUnit GetRandomDefenderWithinRange(float range)
		{
			float safeRange = Mathf.Max(0.1f, range);
			float safeRangeSqr = safeRange * safeRange;
			Vector3 origin = base.transform.position;
			DefenderUnit selected = null;
			int validCount = 0;
			for (int i = 0; i < defenders.Count; i++)
			{
				DefenderUnit defender = defenders[i];
				if (IsLivingDefender(defender) && !((origin - defender.transform.position).sqrMagnitude > safeRangeSqr))
				{
					validCount++;
					if (UnityEngine.Random.Range(0, validCount) == 0)
					{
						selected = defender;
					}
				}
			}
			return selected;
		}

		private List<DefenderUnit> GetRandomDefendersWithinRange(int count, float range)
		{
			List<DefenderUnit> candidates = GetLivingDefendersWithinRange(range);
			ShuffleAndTrim(candidates, count);
			return candidates;
		}

		private float ResolveSkillCastRange(SkillDefinition skill, DefenderUnit target = null)
		{
			if (skill == null)
			{
				return 0f;
			}

			float resolvedRange;
			if (skill.effectType == SkillEffectType.AreaDamage)
			{
				resolvedRange = Mathf.Max(0.1f, skill.radius);
			}
			else if (skill.useCustomCastRange)
			{
				resolvedRange = Mathf.Max(0.5f, skill.castRange);
			}
			else if (skill.effectType == SkillEffectType.SummonRush)
			{
				resolvedRange = Mathf.Max(GetEffectiveAttackRange(), defaultRushCastRange);
			}
			else
			{
				resolvedRange = Mathf.Max(0.5f, GetEffectiveAttackRange(target));
			}

			if (RequiresMeleeEngagementForSkill(skill))
			{
				resolvedRange = Mathf.Min(resolvedRange, GetEffectiveAttackRange(target));
			}

			return resolvedRange;
		}

		private void NotifySkillWarning(SkillDefinition skill, float duration)
		{
			ShowSkillWarningFeedback(skill, duration);
			if (skill != null && !(gameController == null) && IsBoss)
			{
				string prefix = (IsMajorBoss ? "보스 경고: " : "중간보스 경고: ");
				Color warningColor = ((definition != null) ? Color.Lerp(definition.accentColor, Color.white, 0.25f) : new Color(1f, 0.45f, 0.28f));
				gameController.RequestBanner(prefix + skill.displayName + " 준비!", warningColor, Mathf.Max(1.15f, duration + 0.65f));
			}
		}

		private void ShowSkillCastFeedback(SkillDefinition skill)
		{
			if (skill != null && definition != null)
			{
				Color color = ResolveSkillFeedbackColor(skill);
				float radius = ResolveSkillFeedbackRadius(skill);
				RuntimeCombatFeedback.ShowGroundPulse(base.transform.position, color, radius, IsMajorBoss ? 0.8f : (IsBoss ? 0.62f : 0.34f));
				DefenderUnit target = FindNearestDefender();
				Vector3 origin = base.transform.position + Vector3.up * 0.06f;
				Quaternion rotation = ((target != null) ? RuntimeEffectUtility.FaceTowards(origin, target.transform.position, base.transform.rotation) : base.transform.rotation);
				RuntimeEffectUtility.PlayOneShot(skill.muzzleEffectPrefab, origin, rotation, IsMajorBoss ? 0.85f : (IsBoss ? 0.65f : 0.3f));
				floatingUi?.ShowStatus(skill.displayName, Color.Lerp(color, Color.white, 0.25f), IsMajorBoss ? 1.25f : (IsBoss ? 1f : 0.62f));
				if (IsBoss)
				{
					RuntimeCameraShake.Request(IsMajorBoss ? 0.06f : 0.035f, IsMajorBoss ? 0.18f : 0.12f);
				}
			}
		}

		private void ShowSkillWarningFeedback(SkillDefinition skill, float duration)
		{
			if (skill != null && definition != null)
			{
				Color color = Color.Lerp(ResolveSkillFeedbackColor(skill), Color.white, 0.18f);
				RuntimeCombatFeedback.ShowGroundWarning(base.transform.position, color, ResolveSkillFeedbackRadius(skill) * 1.12f, Mathf.Max(0.2f, duration + 0.2f));
				floatingUi?.ShowStatus("!", color, Mathf.Max(0.45f, duration));
			}
		}

		private void ShowSkillImpactFeedback(Vector3 position, SkillDefinition skill, float radius, bool shake)
		{
			if (skill != null && definition != null)
			{
				RuntimeCombatFeedback.ShowGroundPulse(position, ResolveSkillFeedbackColor(skill), Mathf.Max(0.18f, radius), IsMajorBoss ? 0.7f : (IsBoss ? 0.46f : 0.3f));
				Quaternion rotation = RuntimeEffectUtility.FaceTowards(base.transform.position, position, base.transform.rotation);
				RuntimeEffectUtility.PlayOneShot(ResolveSkillImpactEffectPrefab(skill), position + Vector3.up * 0.06f, rotation, IsMajorBoss ? 0.9f : (IsBoss ? 0.65f : 0.3f));
				RuntimeAudioUtility.PlayHit();
				if (shake)
				{
					RuntimeCameraShake.Request(IsMajorBoss ? 0.12f : 0.045f, IsMajorBoss ? 0.3f : 0.14f);
				}
			}
		}

		private GameObject ResolveSkillImpactEffectPrefab(SkillDefinition skill)
		{
			if (skill == null)
			{
				return null;
			}
			if (skill.effectType == SkillEffectType.AreaDamage || skill.effectType == SkillEffectType.SummonRush || skill.effectType == SkillEffectType.MonsterRally || skill.effectType == SkillEffectType.BossFortify || skill.effectType == SkillEffectType.DamageReflect || skill.effectType == SkillEffectType.HealSelf)
			{
				return (skill.areaEffectPrefab != null) ? skill.areaEffectPrefab : skill.hitEffectPrefab;
			}
			return (skill.hitEffectPrefab != null) ? skill.hitEffectPrefab : skill.areaEffectPrefab;
		}

		private Color ResolveSkillFeedbackColor(SkillDefinition skill)
		{
			Color baseColor = ((definition != null) ? definition.accentColor : new Color(1f, 0.35f, 0.22f));
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
				return new Color(0.3f, 0.85f, 1f, 0.95f);
			case SkillEffectType.HealSelf:
			case SkillEffectType.BossFortify:
			case SkillEffectType.MonsterRally:
				return new Color(0.4f, 1f, 0.62f, 0.95f);
			case SkillEffectType.AttackPowerReduction:
				return new Color(0.86f, 0.46f, 1f, 0.96f);
			case SkillEffectType.DamageReflect:
				return new Color(0.58f, 1f, 0.48f, 0.96f);
			case SkillEffectType.DeathPact:
			case SkillEffectType.GoldDrain:
				return new Color(1f, 0.24f, 0.22f, 0.98f);
			case SkillEffectType.AreaDamage:
			case SkillEffectType.SummonRush:
				return Color.Lerp(baseColor, new Color(1f, 0.5f, 0.12f, 1f), 0.45f);
			default:
				return baseColor;
			}
		}

		private float ResolveSkillFeedbackRadius(SkillDefinition skill)
		{
			if (skill == null)
			{
				return IsMajorBoss ? 1.65f : (IsBoss ? 1.15f : 0.74f);
			}
			float baseRadius = (IsMajorBoss ? 1.55f : (IsBoss ? 1.05f : 0.64f));
			if (skill.effectType == SkillEffectType.AreaDamage || skill.effectType == SkillEffectType.MonsterRally)
			{
				return Mathf.Max(baseRadius, skill.radius);
			}
			if (skill.effectType == SkillEffectType.BossFortify || skill.effectType == SkillEffectType.DeathPact)
			{
				return baseRadius * 1.2f;
			}
			if (skill.effectType == SkillEffectType.DamageReflect)
			{
				return baseRadius * 1.25f;
			}
			return baseRadius;
		}

		private void NotifySkill(SkillDefinition skill)
		{
			if (skill != null && !(gameController == null) && IsBoss)
			{
				bool majorBossSkill = skill.effectType == SkillEffectType.DeathPact || skill.effectType == SkillEffectType.MassStun || skill.effectType == SkillEffectType.BossFortify || skill.effectType == SkillEffectType.DirectDamage || skill.effectType == SkillEffectType.AreaDamage || skill.effectType == SkillEffectType.SummonRush || skill.effectType == SkillEffectType.GoldDrain || skill.effectType == SkillEffectType.ManaBurn || skill.effectType == SkillEffectType.MonsterRally || skill.effectType == SkillEffectType.AttackPowerReduction || skill.effectType == SkillEffectType.DamageReflect;
				string prefix = (IsMajorBoss ? "보스 스킬: " : "중간보스 스킬: ");
				gameController.RequestBanner(prefix + skill.displayName + " 발동!", definition.accentColor, majorBossSkill ? 2.6f : 2f);
			}
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
			if (damageReflectTimer > 0f)
			{
				damageReflectTimer -= Time.deltaTime;
				if (damageReflectTimer <= 0f)
				{
					damageReflectTimer = 0f;
					damageReflectRatio = 0f;
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
				if (skillCooldowns.ContainsKey(skill.id))
				{
					skillCooldowns[skill.id] = Mathf.Max(0f, skillCooldowns[skill.id] - Time.deltaTime);
				}
			}
		}

		private void TickBossPhase()
		{
			if (IsBoss && !enraged && currentHealth <= MaxHealth * 0.5f)
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
				return 0.1f;
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
			if (definition != null && !roleTraitTriggered)
			{
				if (definition.role == MonsterRole.Charger && currentHealth <= MaxHealth * 0.6f)
				{
					roleTraitTriggered = true;
					moveSpeedBonus += 0.45f;
					attackSpeedBonus += 0.12f;
					moveSpeedBuffTimer = Mathf.Max(moveSpeedBuffTimer, 4f);
					attackSpeedBuffTimer = Mathf.Max(attackSpeedBuffTimer, 4f);
				}
				else if (definition.role == MonsterRole.Elite && currentHealth <= MaxHealth * 0.5f)
				{
					roleTraitTriggered = true;
					critChanceBonus += 0.18f;
					currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * 0.35f);
					critBuffTimer = Mathf.Max(critBuffTimer, 5f);
				}
			}
		}

		private void ApplyVisuals()
		{
			if (tintRenderers == null || tintRenderers.Length == 0)
			{
				tintRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
			}
			Color tintColor = ResolveReadabilityTint();
			for (int i = 0; i < tintRenderers.Length; i++)
			{
				ApplyRendererTint(tintRenderers[i], tintColor);
			}
			Color gpuTint = (RuntimeRenderBatchingUtility.UsePerInstanceUnitTint ? tintColor : Color.white);
			GpuSkinnedUnitRenderer.AttachOrRefresh(base.gameObject, tintRenderers, gpuTint, isDefender: false, IsBoss);
			base.transform.localScale = Vector3.one * ResolveVisualScale();
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
			Color bossSignalColor = (IsMajorBoss ? new Color(1f, 0.28f, 0.18f, 1f) : new Color(1f, 0.78f, 0.18f, 1f));
			return Color.Lerp(definition.accentColor, bossSignalColor, IsMajorBoss ? 0.52f : 0.38f);
		}

		private void ConfigureBossReadabilityMarker()
		{
			if (!IsBoss)
			{
				if (bossReadabilityMarker != null)
				{
					UnityEngine.Object.Destroy(bossReadabilityMarker);
					bossReadabilityMarker = null;
				}
				return;
			}
			if (bossReadabilityMarker == null)
			{
				bossReadabilityMarker = new GameObject("BossReadabilityMarker");
				bossReadabilityMarker.transform.SetParent(base.transform, worldPositionStays: false);
				LineRenderer line = bossReadabilityMarker.AddComponent<LineRenderer>();
				line.useWorldSpace = false;
				line.loop = true;
				line.positionCount = 64;
				line.numCornerVertices = 4;
				line.numCapVertices = 4;
				line.material = new Material(Shader.Find("Sprites/Default"));
			}
			LineRenderer markerLine = bossReadabilityMarker.GetComponent<LineRenderer>();
			if (!(markerLine == null))
			{
				float radius = (IsMajorBoss ? 1.08f : 0.82f);
				float width = (IsMajorBoss ? 0.12f : 0.085f);
				Color markerColor = (IsMajorBoss ? new Color(1f, 0.18f, 0.1f, 0.92f) : new Color(1f, 0.78f, 0.12f, 0.88f));
				markerLine.widthMultiplier = width;
				markerLine.startColor = markerColor;
				markerLine.endColor = markerColor;
				for (int i = 0; i < markerLine.positionCount; i++)
				{
					float angle = MathF.PI * 2f * (float)i / (float)markerLine.positionCount;
					markerLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.035f, Mathf.Sin(angle) * radius));
				}
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
				renderer.shadowCastingMode = ShadowCastingMode.Off;
			}
			RuntimeRenderBatchingUtility.PrepareRenderer(renderer);
		}

		private float ResolveVisualScale()
		{
			if (definition == null || definition.visualScale <= 0.01f)
			{
				return IsMajorBoss ? 1.7f : ((definition != null && definition.threatLevel == MonsterThreatLevel.MidBoss) ? 1.32f : 1f);
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
					animationDriver = base.gameObject.AddComponent<UnitAnimationDriver>();
				}
			}
			BindAnimationDriver();
		}

		private void BindAnimationDriver()
		{
			if (!(subscribedAnimationDriver == animationDriver))
			{
				UnbindAnimationDriver();
				subscribedAnimationDriver = animationDriver;
				if (subscribedAnimationDriver != null)
				{
					subscribedAnimationDriver.ImpactTriggered += HandleAnimationImpact;
				}
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

		private void ApplyPetrifyMaterials(Material materialOverride)
		{
			Material petrifyMaterial = ResolvePetrifyMaterial(materialOverride);
			if (petrifyMaterial == null)
			{
				return;
			}
			if (tintRenderers == null || tintRenderers.Length == 0)
			{
				tintRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
			}
			if (petrifyMaterialSnapshots.Count == 0)
			{
				for (int i = 0; i < tintRenderers.Length; i++)
				{
					Renderer renderer = tintRenderers[i];
					if (CanSwapPetrifyMaterial(renderer))
					{
						Material[] originalMaterials = renderer.sharedMaterials;
						if (originalMaterials != null && originalMaterials.Length != 0)
						{
							petrifyMaterialSnapshots.Add(new RendererMaterialSnapshot
							{
								renderer = renderer,
								materials = originalMaterials
							});
						}
					}
				}
			}
			for (int j = 0; j < petrifyMaterialSnapshots.Count; j++)
			{
				Renderer renderer2 = petrifyMaterialSnapshots[j].renderer;
				Material[] originalMaterials2 = petrifyMaterialSnapshots[j].materials;
				if (!(renderer2 == null) && originalMaterials2 != null && originalMaterials2.Length != 0)
				{
					Material[] petrifiedMaterials = new Material[originalMaterials2.Length];
					for (int materialIndex = 0; materialIndex < petrifiedMaterials.Length; materialIndex++)
					{
						petrifiedMaterials[materialIndex] = petrifyMaterial;
					}
					renderer2.sharedMaterials = petrifiedMaterials;
					renderer2.SetPropertyBlock(null);
				}
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
			Animator[] animators = GetComponentsInChildren<Animator>(includeInactive: true);
			foreach (Animator animator in animators)
			{
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
			EndPetrify(reapplyVisuals: false, resumeAnimation: false);
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
					hitFlashFeedback = base.gameObject.AddComponent<HitFlashFeedback>();
				}
			}
			hitFlashFeedback.Configure(tintRenderers, (definition != null) ? definition.accentColor : Color.white, RuntimeRenderBatchingUtility.UsePerInstanceUnitTint);
		}

		private void PlayDeathEffect()
		{
			RuntimeEffectUtility.PlayOneShot(deathEffectPrefab, base.transform.position + deathEffectOffset, Quaternion.identity, 3f);
		}

		private float GetEffectiveAttackPower()
		{
			return (definition != null) ? (definition.stats.attackPower * outgameAttackMultiplier * (1f - fateStatCrushRatio)) : 0f;
		}

		private float GetEffectiveAttackRange(DefenderUnit target = null)
		{
			if (definition == null)
			{
				return 0f;
			}

			float resolvedRange = definition.attackBehavior.ResolveAttackRange(definition.stats.attackRange);
			if (IsRangedAttacker())
			{
				return Mathf.Min(Mathf.Max(0.5f, maximumRangedAttackRange), resolvedRange);
			}

			float contactRange = Mathf.Min(resolvedRange, Mathf.Max(0.8f, maximumMeleeAttackRange));
			float targetRadius = target != null ? GetPlanarColliderRadius(target.gameObject) : 0.4f;
			float visualContactRange = GetPlanarColliderRadius(base.gameObject) + targetRadius + meleeContactClearance;
			return Mathf.Min(contactRange, Mathf.Max(0.8f, visualContactRange));
		}

		private static float GetPlanarColliderRadius(GameObject unitObject)
		{
			if (unitObject == null)
			{
				return 0.4f;
			}

			Collider collider = unitObject.GetComponentInChildren<Collider>(includeInactive: false);
			if (collider != null)
			{
				Bounds colliderBounds = collider.bounds;
				return Mathf.Max(0.25f, Mathf.Max(colliderBounds.extents.x, colliderBounds.extents.z));
			}

			Renderer renderer = unitObject.GetComponentInChildren<Renderer>(includeInactive: false);
			if (renderer != null)
			{
				Bounds rendererBounds = renderer.bounds;
				return Mathf.Max(0.25f, Mathf.Max(rendererBounds.extents.x, rendererBounds.extents.z));
			}

			return 0.4f;
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
				float splashRadiusSqr = splashRadius * splashRadius;
				Vector3 splashOrigin = primaryTarget.transform.position;
				for (int i = 0; i < defenders.Count; i++)
				{
					DefenderUnit defender = defenders[i];
					if (IsLivingDefender(defender) && !(defender == primaryTarget) && (splashOrigin - defender.transform.position).sqrMagnitude <= splashRadiusSqr)
					{
						defender.TakeDamage(damage * splashDamageRatio, critical: false, this);
					}
				}
			}
			if (additionalPierceCount <= 0)
			{
				return;
			}
			List<DefenderUnit> additionalTargets = GetLivingDefendersWithinRange(float.MaxValue);
			int appliedCount = 0;
			for (int j = 0; j < additionalTargets.Count; j++)
			{
				if (appliedCount >= additionalPierceCount)
				{
					break;
				}
				DefenderUnit defender2 = additionalTargets[j];
				if (!(defender2 == primaryTarget))
				{
					defender2.TakeDamage(damage, critical: false, this);
					appliedCount++;
				}
			}
		}

		private void HandleDefenderSpawned(DefenderUnit defender)
		{
			if (defender != null && !defenders.Contains(defender))
			{
				defenders.Add(defender);
			}
			if (!CombatRuntimeQuery.IsValidDefenderTarget(cachedCombatTarget))
			{
				InvalidateTargetCache();
			}
		}

		private void HandleDefenderRemoved(DefenderUnit defender)
		{
			defenders.Remove(defender);
			if (cachedCombatTarget == defender)
			{
				InvalidateTargetCache();
			}
			if (tauntTarget == defender)
			{
				tauntTarget = null;
				tauntTimer = 0f;
			}
		}

		private void InvalidateTargetCache()
		{
			cachedCombatTarget = null;
			nextTargetRefreshTime = 0f;
		}

		private void InvalidateRuntimeCaches()
		{
			InvalidateTargetCache();
			cachedSeparationOffset = Vector3.zero;
			nextSeparationRefreshTime = 0f;
		}
	}
}
