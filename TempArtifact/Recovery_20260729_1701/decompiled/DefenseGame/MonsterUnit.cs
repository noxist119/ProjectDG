using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseGame;

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

	public bool CanBeCombatTargeted => !isDying && currentHealth > 0f && !IsPetrified;

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
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		if (options.duration <= 0f)
		{
			return 0;
		}
		direction.y = 0f;
		if (((Vector3)(ref direction)).sqrMagnitude <= 0.0001f)
		{
			direction = Vector3.forward;
		}
		((Vector3)(ref direction)).Normalize();
		float num = Mathf.Max(0.1f, length);
		float num2 = Mathf.Max(0.05f, halfWidth);
		List<StatusTargetCandidate> list = new List<StatusTargetCandidate>();
		PruneMissingActiveMonsters();
		for (int i = 0; i < ActiveMonsters.Count; i++)
		{
			MonsterUnit monsterUnit = ActiveMonsters[i];
			if (!CanReceiveControlStatus(monsterUnit, options.excludeBosses))
			{
				continue;
			}
			Vector3 val = ((Component)monsterUnit).transform.position - origin;
			val.y = 0f;
			float num3 = Vector3.Dot(val, direction);
			if (!(num3 < 0f) && !(num3 > num))
			{
				Vector3 val2 = direction * num3;
				Vector3 val3 = val - val2;
				if (!(((Vector3)(ref val3)).magnitude > num2))
				{
					list.Add(new StatusTargetCandidate
					{
						target = monsterUnit,
						sortDistance = num3
					});
				}
			}
		}
		return ApplyPetrifyToCandidates(list, options);
	}

	public static int ApplyPetrifyRadius(Vector3 center, float radius, PetrifyTargetOptions options)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		if (options.duration <= 0f)
		{
			return 0;
		}
		float num = Mathf.Max(0.1f, radius);
		float num2 = num * num;
		List<StatusTargetCandidate> list = new List<StatusTargetCandidate>();
		PruneMissingActiveMonsters();
		for (int i = 0; i < ActiveMonsters.Count; i++)
		{
			MonsterUnit monsterUnit = ActiveMonsters[i];
			if (CanReceiveControlStatus(monsterUnit, options.excludeBosses))
			{
				Vector3 val = ((Component)monsterUnit).transform.position - center;
				val.y = 0f;
				float sqrMagnitude = ((Vector3)(ref val)).sqrMagnitude;
				if (!(sqrMagnitude > num2))
				{
					list.Add(new StatusTargetCandidate
					{
						target = monsterUnit,
						sortDistance = sqrMagnitude
					});
				}
			}
		}
		return ApplyPetrifyToCandidates(list, options);
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
		int num = 0;
		for (int num2 = ActiveMonsters.Count - 1; num2 >= 0; num2--)
		{
			MonsterUnit monsterUnit = ActiveMonsters[num2];
			if (monsterUnit.definition != null && monsterUnit.definition.threatLevel == threatLevel)
			{
				num++;
			}
		}
		return num;
	}

	private static int ApplyPetrifyToCandidates(List<StatusTargetCandidate> candidates, PetrifyTargetOptions options)
	{
		if (candidates == null || candidates.Count == 0)
		{
			return 0;
		}
		candidates.Sort((StatusTargetCandidate left, StatusTargetCandidate right) => left.sortDistance.CompareTo(right.sortDistance));
		int num = ((options.maxTargets <= 0) ? candidates.Count : Mathf.Min(options.maxTargets, candidates.Count));
		int num2 = 0;
		for (int num3 = 0; num3 < num; num3++)
		{
			MonsterUnit target = candidates[num3].target;
			if (CanReceiveControlStatus(target, options.excludeBosses))
			{
				target.ApplyPetrify(options.duration, options.materialOverride);
				options.onApplied?.Invoke(target);
				num2++;
			}
		}
		return num2;
	}

	private static bool CanReceiveControlStatus(MonsterUnit monster, bool excludeBosses = false)
	{
		return (Object)(object)monster != (Object)null && !monster.isDying && monster.currentHealth > 0f && !monster.IsStatusEffectImmune && (!excludeBosses || !monster.IsBoss);
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
		for (int num = ActiveMonsters.Count - 1; num >= 0; num--)
		{
			if ((Object)(object)ActiveMonsters[num] == (Object)null)
			{
				ActiveMonsters.RemoveAt(num);
			}
		}
	}

	public void AdoptRuntimeTemplate(MonsterUnit template)
	{
		if (!((Object)(object)template == (Object)null))
		{
			if ((Object)(object)deathEffectPrefab == (Object)null)
			{
				deathEffectPrefab = template.deathEffectPrefab;
			}
			if (tintRenderers == null || tintRenderers.Length == 0)
			{
				tintRenderers = ((Component)this).GetComponentsInChildren<Renderer>(true);
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
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		if (definition == null)
		{
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
		float num = definition.stats.manaRegenPerSecondRate;
		if (definition.role == MonsterRole.Caster)
		{
			num += 0.025f;
		}
		currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * num * Time.deltaTime);
		attackCooldown -= Time.deltaTime;
		floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
		if (IsControlLocked)
		{
			if ((Object)(object)animationDriver == (Object)null || !animationDriver.IsLocked)
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
		DefenderUnit defenderUnit = ResolveTauntTarget();
		if ((Object)(object)defenderUnit == (Object)null && TryCastSkill())
		{
			return;
		}
		DefenderUnit defenderUnit2 = (((Object)(object)defenderUnit != (Object)null) ? defenderUnit : FindNearestDefender());
		if ((Object)(object)defenderUnit2 != (Object)null)
		{
			float effectiveAttackRange = GetEffectiveAttackRange();
			Vector3 val = ((Component)defenderUnit2).transform.position - ((Component)this).transform.position;
			bool flag = ((Vector3)(ref val)).sqrMagnitude <= effectiveAttackRange * effectiveAttackRange;
			bool flag2 = !IsRangedAttacker() || CanAnyLivingDefenderRetaliate();
			if (flag && flag2)
			{
				if (attackCooldown <= 0f && CanStartActionAnimation())
				{
					PerformAttack(defenderUnit2);
				}
				animationDriver?.PlayMoving(isMoving: false);
				return;
			}
			if (flag && !flag2)
			{
				MoveTowardsDefender(defenderUnit2);
				return;
			}
			if ((Object)(object)defenderUnit != (Object)null)
			{
				MoveTowardsDefender(defenderUnit);
				return;
			}
		}
		MoveTowardsGoal();
	}

	public void Initialize(MonsterDefinition newDefinition, Transform goalPoint, int spawnRound = 0)
	{
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Unknown result type (might be due to invalid IL or missing references)
		definition = newDefinition;
		outgameHealthMultiplier = 1f;
		appliedFateBossHealthMultiplier = 1f;
		outgameAttackMultiplier = 1f;
		if ((Object)(object)OutgameProgressionSystem.Active != (Object)null)
		{
			OutgameProgressionSystem.Active.ResolveMonsterBalanceMultipliers(definition, out outgameHealthMultiplier, out outgameAttackMultiplier);
		}
		CommercialRoundPacing.ResolveCombatMultipliers(spawnRound, definition != null && definition.IsBossLike, out var healthMultiplier, out var attackMultiplier);
		outgameHealthMultiplier *= healthMultiplier;
		outgameAttackMultiplier *= attackMultiplier;
		DailyFortuneRule today = DailyFortuneSystem.Today;
		if (today != null && definition != null && definition.IsBossLike)
		{
			outgameHealthMultiplier *= today.BossHealthMultiplier;
		}
		if (definition != null && definition.IsBossLike && (Object)(object)DefenseGameController.Active != (Object)null)
		{
			appliedFateBossHealthMultiplier = Mathf.Max(1f, DefenseGameController.Active.FateDebtBossHealthMultiplier);
			outgameHealthMultiplier *= appliedFateBossHealthMultiplier;
		}
		goal = goalPoint;
		laneGoalPosition = (Vector3)(((Object)(object)goal != (Object)null) ? new Vector3(((Component)this).transform.position.x, goal.position.y, goal.position.z) : ((Component)this).transform.position);
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
		RestorePetrifyMaterials(reapplyVisuals: false);
		RestorePetrifyAnimations(resumeAnimation: false);
		defenders.Clear();
		IReadOnlyList<DefenderUnit> activeInstances = DefenderUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			DefenderUnit defenderUnit = activeInstances[i];
			if ((Object)(object)defenderUnit != (Object)null)
			{
				defenders.Add(defenderUnit);
			}
		}
		InvalidateRuntimeCaches();
		skillCooldowns.Clear();
		if ((Object)(object)gameController == (Object)null)
		{
			gameController = Object.FindObjectOfType<DefenseGameController>();
		}
		((Object)((Component)this).gameObject).name = definition.displayName;
		ApplyVisuals();
		EnsureAnimationDriver();
		UnitAnimatorLodController.AttachOrRefresh(((Component)this).gameObject, animationDriver, defender: false, IsBoss);
		EnsureHitFlashFeedback();
		floatingUi = FloatingCombatUI.Attach(((Component)this).transform, definition.displayName, definition.accentColor, definition.grade, GetFloatingUiFallbackHeight());
		if ((Object)(object)gameController != (Object)null && gameController.FateMonsterStatCrushActive)
		{
			ApplyFateStatCrush(gameController.FateMonsterStatCrushRatio);
		}
		floatingUi.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
		animationDriver?.PlaySpawn();
		MonsterUnit.OnMonsterSpawned?.Invoke(this);
	}

	public bool RefreshFateDebtBossHealthPressure()
	{
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		if (!IsBoss || currentHealth <= 0f || (Object)(object)DefenseGameController.Active == (Object)null)
		{
			return false;
		}
		float num = Mathf.Max(1f, DefenseGameController.Active.FateDebtBossHealthMultiplier);
		float num2 = Mathf.Max(1f, appliedFateBossHealthMultiplier);
		if (num <= num2 + 0.001f)
		{
			return false;
		}
		float maxHealth = MaxHealth;
		outgameHealthMultiplier *= num / num2;
		appliedFateBossHealthMultiplier = num;
		float num3 = Mathf.Max(0f, MaxHealth - maxHealth);
		currentHealth = Mathf.Min(MaxHealth, currentHealth + num3);
		floatingUi?.ShowStatus("운명 대가  HP +" + Mathf.RoundToInt((num - num2) * 100f) + "%", new Color(1f, 0.3f, 0.38f), 1.8f);
		floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
		RuntimeCombatFeedback.ShowGroundPulse(((Component)this).transform.position, new Color(0.95f, 0.2f, 0.32f), 1.25f, 0.7f, 0.12f);
		return num3 > 0f;
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
		float num = 1.48f;
		if (definition.role == MonsterRole.Brute || definition.role == MonsterRole.Elite)
		{
			num = 1.72f;
		}
		else if (definition.role == MonsterRole.Caster)
		{
			num = 1.58f;
		}
		else if (definition.role == MonsterRole.Charger)
		{
			num = 1.42f;
		}
		if (definition.grade == CharacterGrade.Legendary)
		{
			num += 0.08f;
		}
		else if (definition.grade == CharacterGrade.Mythic)
		{
			num += 0.14f;
		}
		return num;
	}

	public void TakeDamage(float damage, bool critical)
	{
		TakeDamage(damage, critical, null);
	}

	public void TakeDamage(float damage, bool critical, DefenderUnit source)
	{
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		if (!CanBeCombatTargeted)
		{
			return;
		}
		float num = damage * (1f - GetRoleDamageReduction());
		if ((Object)(object)source != (Object)null && num > 0f)
		{
			LastDamageSource = source;
			LastDamageSkill = DefenderUnit.CurrentDamageSkillContext;
		}
		currentHealth -= num;
		currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * definition.stats.manaGainWhenHitRate);
		TryTriggerRoleTrait();
		hitFlashFeedback?.PlayHit(critical);
		floatingUi?.ShowDamage(num, critical, healing: false);
		floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
		DefenderUnit.ReportDamageDealt(source, this, num, critical);
		if ((Object)(object)source != (Object)null && !resolvingDamageReflect && damageReflectTimer > 0f && damageReflectRatio > 0f && num > 0f)
		{
			float num2 = num * Mathf.Clamp01(damageReflectRatio);
			resolvingDamageReflect = true;
			try
			{
				source.TakeDamage(num2, critical: false, this);
				floatingUi?.ShowStatus("반사 " + Mathf.RoundToInt(num2), new Color(0.58f, 1f, 0.48f), 0.65f);
				RuntimeCombatFeedback.ShowGroundPulse(((Component)source).transform.position, new Color(0.58f, 1f, 0.48f), 0.42f, 0.38f, 0.06f);
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
			currentHealth = 0f;
			skillCastLocked = false;
			ClearPendingImpacts();
			animationDriver?.PlayMoving(isMoving: false);
			floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
			PlayDeathEffect();
			if (IsBoss)
			{
				((MonoBehaviour)this).StartCoroutine(BossDeathPresentationRoutine());
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
		Color color = (Color)((definition != null) ? definition.accentColor : new Color(1f, 0.32f, 0.18f));
		float radius = (IsMajorBoss ? bossDeathPulseRadius : (bossDeathPulseRadius * 0.72f));
		floatingUi?.ShowStatus(IsMajorBoss ? "BOSS DOWN" : "MID BOSS DOWN", Color.Lerp(color, Color.white, 0.35f), duration);
		RuntimeCombatFeedback.ShowBossDefeat(((Component)this).transform.position, color, radius, duration);
		RuntimeCameraShake.Request(IsMajorBoss ? 0.18f : 0.11f, Mathf.Min(0.5f, duration * 0.42f));
		RuntimeAudioUtility.PlayHit();
		Vector3 originalScale = ((Component)this).transform.localScale;
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			float pulse = Mathf.Sin(t * MathF.PI * 4f) * (1f - t) * 0.055f;
			((Component)this).transform.localScale = originalScale * (1f + pulse);
			yield return null;
		}
		((Component)this).transform.localScale = originalScale;
		CompleteDeath();
	}

	private void CompleteDeath()
	{
		MonsterUnit.OnMonsterKilled?.Invoke(this);
		Object.Destroy((Object)(object)((Component)this).gameObject);
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
			float num = Mathf.Clamp01(ratio);
			fateStatCrushRatio = Mathf.Max(fateStatCrushRatio, num);
			currentHealth = Mathf.Max(1f, currentHealth * (1f - num));
			currentMana = 0f;
			attackCooldown = Mathf.Max(attackCooldown, 1.2f);
			hitFlashFeedback?.PlayHit(critical: true);
			floatingUi?.ShowDamage(MaxHealth * num, critical: true, healing: false);
			floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, definition.stats.maxMana);
		}
	}

	public void ApplyStun(float duration)
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		if (CanBeCombatTargeted && !IsStatusEffectImmune)
		{
			float num = ResolveControlDuration(duration);
			if (!(num <= 0f))
			{
				stunTimer = Mathf.Max(stunTimer, num);
				hitFlashFeedback?.PlayHit(critical: true);
				floatingUi?.ShowTimedStatus("STUN · 행동 불가", new Color(1f, 0.88f, 0.22f, 1f), num);
				RuntimeCombatFeedback.ShowGroundPulse(((Component)this).transform.position, new Color(1f, 0.82f, 0.18f, 1f), IsBoss ? 0.82f : 0.58f, 0.62f, 0.1f);
			}
		}
	}

	public void ApplyPetrify(float duration, Material materialOverride = null)
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		if (CanReceiveControlStatus(this))
		{
			float num = ResolveControlDuration(duration);
			if (!(num <= 0f))
			{
				petrifyTimer = Mathf.Max(petrifyTimer, num);
				attackCooldown = Mathf.Max(attackCooldown, Mathf.Min(num, 1.2f));
				skillCastLocked = false;
				ClearPendingImpacts();
				animationDriver?.ForceIdle();
				FreezePetrifyAnimations();
				floatingUi?.ShowStatus("PETRIFY", new Color(0.72f, 0.78f, 0.82f, 1f), Mathf.Min(num, 1.2f));
				RuntimeCombatFeedback.ShowGroundPulse(((Component)this).transform.position, new Color(0.7f, 0.76f, 0.8f, 1f), IsBoss ? 0.95f : 0.62f, 0.46f, 0.09f);
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

	public void ApplyKnockback(float distance, Vector3 sourcePosition)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		if (CanBeCombatTargeted && !IsStatusEffectImmune && !(distance <= 0f))
		{
			Vector3 val = ((Component)this).transform.position - laneGoalPosition;
			val.y = 0f;
			if (((Vector3)(ref val)).sqrMagnitude <= 0.0001f)
			{
				val = ((Component)this).transform.position - sourcePosition;
				val.y = 0f;
			}
			if (((Vector3)(ref val)).sqrMagnitude <= 0.0001f)
			{
				val = Vector3.back;
			}
			Vector3 val2 = ((Component)this).transform.position + ((Vector3)(ref val)).normalized * distance;
			val2.x = Mathf.Clamp(val2.x, laneGoalPosition.x - 0.6f, laneGoalPosition.x + 0.6f);
			val2.y = ((Component)this).transform.position.y;
			((Component)this).transform.position = val2;
			hitFlashFeedback?.PlayHit(critical: true);
		}
	}

	public void ApplyTaunt(DefenderUnit source, float duration)
	{
		if (CanBeCombatTargeted && !IsStatusEffectImmune && !((Object)(object)source == (Object)null) && !(source.CurrentHealth <= 0f) && !(duration <= 0f))
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
			((MonoBehaviour)this).StartCoroutine(PoisonRoutine(Mathf.Max(0f, damagePerTick), Mathf.Max(0f, duration), Mathf.Max(0.2f, tickInterval), source));
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
			yield return (object)new WaitForSeconds(tickInterval);
		}
	}

	private void MoveTowardsGoal()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)goal == (Object)null))
		{
			Vector3 val = BuildMoveTarget();
			FaceTarget(val);
			float num = definition.stats.moveSpeed * (1f + moveSpeedBonus) * (1f - slowRatio) * (1f - fateStatCrushRatio) * globalMoveSpeedMultiplier;
			((Component)this).transform.position = Vector3.MoveTowards(((Component)this).transform.position, val, num * Time.deltaTime);
			animationDriver?.PlayMoving(isMoving: true);
			if (Vector3.Distance(((Component)this).transform.position, laneGoalPosition) <= 0.05f)
			{
				MonsterUnit.OnMonsterEscaped?.Invoke(this);
				Object.Destroy((Object)(object)((Component)this).gameObject);
			}
		}
	}

	private void MoveTowardsDefender(DefenderUnit target)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)target == (Object)null))
		{
			Vector3 position = ((Component)target).transform.position;
			position.y = ((Component)this).transform.position.y;
			FaceTarget(position);
			float num = definition.stats.moveSpeed * (1f + moveSpeedBonus) * (1f - slowRatio) * (1f - fateStatCrushRatio) * globalMoveSpeedMultiplier;
			((Component)this).transform.position = Vector3.MoveTowards(((Component)this).transform.position, position, num * Time.deltaTime);
			animationDriver?.PlayMoving(isMoving: true);
		}
	}

	private void PerformAttack(DefenderUnit target)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)target == (Object)null))
		{
			FaceTarget(((Component)target).transform.position);
			float num = Mathf.Max(0.2f, definition.stats.attackSpeed * (1f + attackSpeedBonus) * (1f - attackSpeedSlowRatio) * (1f - fateStatCrushRatio));
			attackCooldown = 1f / num;
			bool flag = Random.value <= Mathf.Clamp01(definition.stats.criticalChance + critChanceBonus);
			float damage = GetEffectiveAttackPower() * (flag ? definition.stats.criticalDamageMultiplier : 1f);
			QueueBasicAttackImpact(target, damage, flag);
			if ((Object)(object)animationDriver != (Object)null && animationDriver.PlayAttack())
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
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		if (!pendingBasicAttack.isValid)
		{
			return;
		}
		PendingMonsterAttack pending = pendingBasicAttack;
		pendingBasicAttack = default(PendingMonsterAttack);
		CancelPendingAttackFallback();
		if (!((Object)(object)pending.target == (Object)null))
		{
			FaceTarget(((Component)pending.target).transform.position);
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
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = ResolveBasicAttackProjectilePrefab();
		if ((Object)(object)val == (Object)null || (Object)(object)pending.target == (Object)null)
		{
			return false;
		}
		Vector3 val2 = ((Component)this).transform.position + Vector3.up * ResolveBasicAttackEffectHeight();
		Quaternion rotation = (((Object)(object)pending.target != (Object)null) ? RuntimeEffectUtility.FaceTowards(val2, ((Component)pending.target).transform.position + Vector3.up * ResolveBasicAttackEffectHeight(), ((Component)this).transform.rotation) : ((Component)this).transform.rotation);
		Projectile projectile = Projectile.Spawn(val, val2, rotation);
		if ((Object)(object)projectile == (Object)null)
		{
			return false;
		}
		PlayBasicAttackMuzzleEffect(pending.target);
		float projectileSpeed = ((definition != null) ? Mathf.Max(2f, definition.stats.projectileSpeed) : 8f);
		projectile.Initialize(pending.target, pending.damage, projectileSpeed, pending.critical, this, ResolveBasicAttackEffectHeight());
		return true;
	}

	private void ResolveBasicAttackDamage(PendingMonsterAttack pending)
	{
		ResolveBasicAttackProjectileImpact(pending.target, pending.damage, pending.critical);
	}

	internal void ResolveBasicAttackProjectileImpact(DefenderUnit target, float damage, bool critical)
	{
		if (!((Object)(object)target == (Object)null))
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
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = ResolveBasicAttackMuzzleEffectPrefab();
		if (!((Object)(object)val == (Object)null))
		{
			Vector3 val2 = ((Component)this).transform.position + Vector3.up * 0.06f;
			Quaternion rotation = (((Object)(object)target != (Object)null) ? RuntimeEffectUtility.FaceTowards(val2, ((Component)target).transform.position, ((Component)this).transform.rotation) : ((Component)this).transform.rotation);
			RuntimeEffectUtility.PlayOneShot(val, val2, rotation, IsBoss ? 0.48f : 0.28f);
		}
	}

	private void PlayBasicAttackHitEffect(DefenderUnit target)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)target == (Object)null))
		{
			GameObject val = ResolveBasicAttackHitEffectPrefab();
			if ((Object)(object)val != (Object)null)
			{
				Quaternion rotation = RuntimeEffectUtility.FaceTowards(((Component)this).transform.position, ((Component)target).transform.position, ((Component)this).transform.rotation);
				RuntimeEffectUtility.PlayOneShot(val, ((Component)target).transform.position + Vector3.up * 0.06f, rotation, IsBoss ? 0.54f : 0.3f);
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
			SkillDefinition skillDefinition = definition.skills[i];
			float num = Mathf.Clamp((skillDefinition.manaThreshold <= 0f) ? definition.stats.maxMana : skillDefinition.manaThreshold, 0f, definition.stats.maxMana);
			if (!(currentMana < num) && CanCastSkill(skillDefinition) && (!skillCooldowns.TryGetValue(skillDefinition.id, out var value) || !(value > 0f)))
			{
				skillCooldowns[skillDefinition.id] = skillDefinition.cooldown;
				currentMana = 0f;
				animationDriver?.PlayMoving(isMoving: false);
				((MonoBehaviour)this).StartCoroutine(CastSkillSequence(skillDefinition));
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
			yield return (object)new WaitForSeconds(warningDelay);
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
		if ((Object)(object)target == (Object)null)
		{
			yield break;
		}
		Vector3 startPosition = ((Component)this).transform.position;
		Vector3 direction = ((Component)target).transform.position - startPosition;
		direction.y = 0f;
		float distance = ((Vector3)(ref direction)).magnitude;
		if (distance <= 0.1f)
		{
			yield break;
		}
		float desiredStopDistance = Mathf.Clamp(GetEffectiveAttackRange() * 0.55f, 0.9f, 1.45f);
		float travelDistance = Mathf.Min(1.25f, Mathf.Max(0f, distance - desiredStopDistance));
		if (!(travelDistance <= 0.05f))
		{
			Vector3 endPosition = startPosition + ((Vector3)(ref direction)).normalized * travelDistance;
			FaceTarget(((Component)target).transform.position);
			animationDriver?.PlayMoving(isMoving: true);
			float elapsed = 0f;
			while (elapsed < 0.18f && (Object)(object)target != (Object)null)
			{
				elapsed += Time.deltaTime;
				((Component)this).transform.position = Vector3.Lerp(startPosition, endPosition, Mathf.Clamp01(elapsed / 0.18f));
				yield return null;
			}
			((Component)this).transform.position = endPosition;
			animationDriver?.PlayMoving(isMoving: false);
		}
	}

	private bool CanCastSkill(SkillDefinition skill)
	{
		if (skill == null)
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
		bool flag = false;
		bool flag2 = (Object)(object)animationDriver != (Object)null && animationDriver.PlaySkill(skillSlot);
		if (!flag2 && (Object)(object)animationDriver != (Object)null)
		{
			flag = animationDriver.PlayAttack();
			flag2 = flag;
		}
		NotifySkillPresentation(skill);
		if (flag2)
		{
			float fallbackDelay = (flag ? animationDriver.AttackImpactFallbackDelay : animationDriver.SkillImpactFallbackDelay);
			SchedulePendingSkillFallback(pendingSkillCast.sequence, fallbackDelay);
		}
		else
		{
			ResolvePendingSkillCast();
		}
	}

	private void ApplySkillEffect(SkillDefinition skill)
	{
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_06bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_0454: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b8f: Unknown result type (might be due to invalid IL or missing references)
		//IL_061c: Unknown result type (might be due to invalid IL or missing references)
		//IL_062f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0517: Unknown result type (might be due to invalid IL or missing references)
		//IL_074c: Unknown result type (might be due to invalid IL or missing references)
		//IL_053b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a59: Unknown result type (might be due to invalid IL or missing references)
		//IL_09be: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aaf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0771: Unknown result type (might be due to invalid IL or missing references)
		//IL_0814: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_08dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0924: Unknown result type (might be due to invalid IL or missing references)
		if (skill == null)
		{
			return;
		}
		int num = 0;
		float num2 = 0f;
		int goldDrained = 0;
		if (skill.effectType == SkillEffectType.DirectDamage)
		{
			DefenderUnit defenderUnit = (skill.isGlobalTargeting ? FindNearestDefender() : FindNearestDefenderForSkill(skill));
			if ((Object)(object)defenderUnit != (Object)null)
			{
				FaceTarget(((Component)defenderUnit).transform.position);
				ShowSkillImpactFeedback(((Component)defenderUnit).transform.position, skill, 0.72f, IsBoss);
				float num3 = GetEffectiveAttackPower() * skill.power;
				defenderUnit.TakeDamage(num3, critical: false, this);
				num++;
				num2 += num3;
			}
		}
		else if (skill.effectType == SkillEffectType.AreaDamage)
		{
			List<DefenderUnit> livingDefendersWithinRange = GetLivingDefendersWithinRange(Mathf.Max(0.1f, skill.radius));
			if (livingDefendersWithinRange.Count > 0)
			{
				FaceTarget(((Component)livingDefendersWithinRange[0]).transform.position);
			}
			ShowSkillImpactFeedback(((Component)this).transform.position, skill, Mathf.Max(0.8f, skill.radius), IsBoss);
			for (int i = 0; i < livingDefendersWithinRange.Count; i++)
			{
				float num4 = GetEffectiveAttackPower() * skill.power;
				livingDefendersWithinRange[i].TakeDamage(num4, critical: false, this);
				num++;
				num2 += num4;
			}
		}
		else if (skill.effectType == SkillEffectType.HealSelf)
		{
			Heal(MaxHealth * skill.power);
			ShowSkillImpactFeedback(((Component)this).transform.position, skill, 0.82f, shake: false);
			num = 1;
		}
		else if (skill.effectType == SkillEffectType.AttackSpeedBoost)
		{
			attackSpeedBonus = skill.power;
			attackSpeedBuffTimer = skill.duration;
			ShowSkillImpactFeedback(((Component)this).transform.position, skill, 0.82f, shake: false);
			num = 1;
		}
		else if (skill.effectType == SkillEffectType.CriticalBoost)
		{
			critChanceBonus = skill.power;
			critBuffTimer = skill.duration;
			ShowSkillImpactFeedback(((Component)this).transform.position, skill, 0.82f, shake: false);
			num = 1;
		}
		else if (skill.effectType == SkillEffectType.MoveSpeedBoost)
		{
			moveSpeedBonus = skill.power;
			moveSpeedBuffTimer = skill.duration;
			ShowSkillImpactFeedback(((Component)this).transform.position, skill, 0.82f, shake: false);
			num = 1;
		}
		else if (skill.effectType == SkillEffectType.ManaSurge)
		{
			currentMana = Mathf.Min(definition.stats.maxMana, currentMana + definition.stats.maxMana * skill.power);
			ShowSkillImpactFeedback(((Component)this).transform.position, skill, 0.82f, shake: false);
			num = 1;
		}
		else if (skill.effectType == SkillEffectType.SummonRush)
		{
			float range = ResolveSkillCastRange(skill);
			List<DefenderUnit> livingDefendersWithinRange2 = GetLivingDefendersWithinRange(range);
			int num5 = Mathf.Max(0, skill.hitCount);
			if (livingDefendersWithinRange2.Count > num5)
			{
				livingDefendersWithinRange2.RemoveRange(num5, livingDefendersWithinRange2.Count - num5);
			}
			if (livingDefendersWithinRange2.Count > 0)
			{
				FaceTarget(((Component)livingDefendersWithinRange2[0]).transform.position);
			}
			for (int j = 0; j < livingDefendersWithinRange2.Count; j++)
			{
				ShowSkillImpactFeedback(((Component)livingDefendersWithinRange2[j]).transform.position, skill, 0.64f, shake: false);
				float num6 = GetEffectiveAttackPower() * skill.power;
				livingDefendersWithinRange2[j].TakeDamage(num6, critical: false, this);
				num++;
				num2 += num6;
			}
		}
		else if (skill.effectType == SkillEffectType.Stun)
		{
			DefenderUnit defenderUnit2 = (skill.isGlobalTargeting ? FindNearestDefender() : FindNearestDefenderForSkill(skill));
			if ((Object)(object)defenderUnit2 != (Object)null)
			{
				FaceTarget(((Component)defenderUnit2).transform.position);
				ShowSkillImpactFeedback(((Component)defenderUnit2).transform.position, skill, 0.7f, IsBoss);
				defenderUnit2.ApplyStun(skill.duration);
				num++;
			}
		}
		else if (skill.effectType == SkillEffectType.MassStun)
		{
			if (IsBoss)
			{
				ShowSkillImpactFeedback(((Component)this).transform.position, skill, ResolveSkillFeedbackRadius(skill), shake: true);
			}
			List<DefenderUnit> list = (skill.isGlobalTargeting ? GetRandomDefenders(Mathf.Max(1, skill.hitCount)) : GetRandomDefendersWithinRange(Mathf.Max(1, skill.hitCount), ResolveSkillCastRange(skill)));
			if (list.Count > 0)
			{
				FaceTarget(((Component)list[0]).transform.position);
			}
			for (int k = 0; k < list.Count; k++)
			{
				ShowSkillImpactFeedback(((Component)list[k]).transform.position, skill, 0.66f, k == 0 && IsBoss);
				list[k].ApplyStun(skill.duration);
				float num7 = GetEffectiveAttackPower() * Mathf.Max(0f, skill.power);
				if (num7 > 0f)
				{
					list[k].TakeDamage(num7, critical: false, this);
					num2 += num7;
				}
				num++;
			}
		}
		else if (skill.effectType == SkillEffectType.DeathPact)
		{
			DefenderUnit defenderUnit3 = (skill.isGlobalTargeting ? GetRandomDefender() : GetRandomDefenderWithinRange(ResolveSkillCastRange(skill)));
			if ((Object)(object)defenderUnit3 != (Object)null)
			{
				FaceTarget(((Component)defenderUnit3).transform.position);
				ShowSkillImpactFeedback(((Component)defenderUnit3).transform.position, skill, 0.92f, shake: true);
				defenderUnit3.KillByBossSkill();
				RuntimeCameraShake.Request(0.12f, 0.28f);
				num++;
			}
		}
		else if (skill.effectType == SkillEffectType.BossFortify)
		{
			Heal(MaxHealth * skill.power);
			attackSpeedBonus = Mathf.Max(attackSpeedBonus, 0.12f);
			attackSpeedBuffTimer = Mathf.Max(attackSpeedBuffTimer, skill.duration);
			ShowSkillImpactFeedback(((Component)this).transform.position, skill, 1.05f, shake: true);
			num = 1;
		}
		else if (skill.effectType == SkillEffectType.GoldDrain)
		{
			int amount = Mathf.Max(1, Mathf.RoundToInt(skill.power));
			int num8 = (((Object)(object)gameController != (Object)null) ? gameController.RemoveGold(amount) : 0);
			if (num8 > 0)
			{
				gameController.RequestBanner("탐욕의 징수  -" + num8 + "G", definition.accentColor, 2.2f);
			}
			goldDrained = num8;
			num = ((num8 > 0) ? 1 : 0);
			ShowSkillImpactFeedback(((Component)this).transform.position, skill, 0.86f, shake: false);
		}
		else if (skill.effectType == SkillEffectType.ManaBurn)
		{
			List<DefenderUnit> list2 = (skill.isGlobalTargeting ? GetRandomDefenders(Mathf.Max(1, skill.hitCount)) : GetRandomDefendersWithinRange(Mathf.Max(1, skill.hitCount), ResolveSkillCastRange(skill)));
			if (list2.Count > 0)
			{
				FaceTarget(((Component)list2[0]).transform.position);
			}
			for (int l = 0; l < list2.Count; l++)
			{
				ShowSkillImpactFeedback(((Component)list2[l]).transform.position, skill, 0.66f, l == 0 && IsBoss);
				list2[l].DrainMana(skill.power);
				num++;
			}
		}
		else if (skill.effectType == SkillEffectType.MonsterRally)
		{
			IReadOnlyList<MonsterUnit> activeInstances = ActiveInstances;
			float num9 = ((skill.radius > 0.1f) ? skill.radius : defaultRallyRadius);
			for (int m = 0; m < activeInstances.Count; m++)
			{
				if ((Object)(object)activeInstances[m] != (Object)null && (skill.isGlobalTargeting || Vector3.Distance(((Component)this).transform.position, ((Component)activeInstances[m]).transform.position) <= num9))
				{
					activeInstances[m].ApplyRally(skill.power, skill.duration);
					ShowSkillImpactFeedback(((Component)activeInstances[m]).transform.position, skill, ((Object)(object)activeInstances[m] == (Object)(object)this) ? 0.92f : 0.62f, (Object)(object)activeInstances[m] == (Object)(object)this && IsBoss);
					num++;
				}
			}
		}
		else if (skill.effectType == SkillEffectType.AttackPowerReduction)
		{
			DefenderUnit defenderUnit4 = FindNearestDefenderForSkill(skill);
			if ((Object)(object)defenderUnit4 != (Object)null)
			{
				FaceTarget(((Component)defenderUnit4).transform.position);
				ShowSkillImpactFeedback(((Component)defenderUnit4).transform.position, skill, 0.72f, IsBoss);
				defenderUnit4.ApplyAttackPowerReduction(skill.power, skill.duration, skill.hitEffectPrefab);
				num = 1;
			}
		}
		else if (skill.effectType == SkillEffectType.DamageReflect)
		{
			damageReflectRatio = Mathf.Max(damageReflectRatio, Mathf.Clamp01(skill.power));
			damageReflectTimer = Mathf.Max(damageReflectTimer, skill.duration);
			ShowSkillImpactFeedback(((Component)this).transform.position, skill, 1.05f, shake: false);
			floatingUi?.ShowTimedStatus("피해 반사 " + Mathf.RoundToInt(damageReflectRatio * 100f) + "%", new Color(0.58f, 1f, 0.48f), damageReflectTimer);
			num = 1;
		}
		if (IsBoss && (Object)(object)gameController != (Object)null)
		{
			gameController.RecordBossSkillImpact(skill, num, num2, goldDrained, IsMajorBoss);
			if (IsMajorBoss)
			{
				string text = ((num > 0) ? (num + "기 적중") : "범위 밖");
				text = ((!(num2 > 0f)) ? (text + ((num > 0) ? "  |  상태/버프 적용" : "  |  피해 없음")) : (text + "  |  피해 " + Mathf.RoundToInt(num2)));
				gameController.RequestBanner("보스 적중: " + skill.displayName + "  |  " + text, ResolveSkillFeedbackColor(skill), 2.1f);
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
			PendingMonsterSkill pendingMonsterSkill = pendingSkillCast;
			pendingSkillCast = default(PendingMonsterSkill);
			CancelPendingSkillFallback();
			ApplySkillEffect(pendingMonsterSkill.skill);
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
		float delay = (((Object)(object)animationDriver != (Object)null) ? animationDriver.AttackImpactFallbackDelay : 0.12f);
		pendingAttackImpactRoutine = ((MonoBehaviour)this).StartCoroutine(ResolvePendingAttackAfterDelay(sequence, delay));
	}

	private IEnumerator ResolvePendingAttackAfterDelay(int sequence, float delay)
	{
		yield return (object)new WaitForSeconds(delay);
		pendingAttackImpactRoutine = null;
		if (pendingBasicAttack.isValid && pendingBasicAttack.sequence == sequence)
		{
			ResolvePendingBasicAttack();
		}
	}

	private void SchedulePendingSkillFallback(int sequence, float fallbackDelay = -1f)
	{
		CancelPendingSkillFallback();
		float delay = ((fallbackDelay > 0f) ? fallbackDelay : (((Object)(object)animationDriver != (Object)null) ? animationDriver.SkillImpactFallbackDelay : 0.2f));
		pendingSkillImpactRoutine = ((MonoBehaviour)this).StartCoroutine(ResolvePendingSkillAfterDelay(sequence, delay));
	}

	private IEnumerator ResolvePendingSkillAfterDelay(int sequence, float delay)
	{
		yield return (object)new WaitForSeconds(delay);
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
			((MonoBehaviour)this).StopCoroutine(pendingAttackImpactRoutine);
			pendingAttackImpactRoutine = null;
		}
	}

	private void CancelPendingSkillFallback()
	{
		if (pendingSkillImpactRoutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(pendingSkillImpactRoutine);
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
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = targetPosition - ((Component)this).transform.position;
		val.y = 0f;
		if (!(((Vector3)(ref val)).sqrMagnitude <= 0.0001f))
		{
			Quaternion val2 = Quaternion.LookRotation(((Vector3)(ref val)).normalized, Vector3.up);
			((Component)this).transform.rotation = val2 * Quaternion.Euler(0f, facingOffsetY, 0f);
		}
	}

	private Vector3 BuildMoveTarget()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		if (Time.unscaledTime >= nextSeparationRefreshTime)
		{
			RefreshSeparationOffset();
		}
		Vector3 val = laneGoalPosition + cachedSeparationOffset;
		val.x = Mathf.Clamp(val.x, laneGoalPosition.x - 0.6f, laneGoalPosition.x + 0.6f);
		return val;
	}

	private void RefreshSeparationOffset()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = Vector3.zero;
		float num = Mathf.Max(0.01f, separationRadius);
		float num2 = num * num;
		IReadOnlyList<MonsterUnit> activeInstances = ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if (!((Object)(object)monsterUnit == (Object)null) && !((Object)(object)monsterUnit == (Object)(object)this))
			{
				Vector3 val2 = ((Component)this).transform.position - ((Component)monsterUnit).transform.position;
				val2.y = 0f;
				float sqrMagnitude = ((Vector3)(ref val2)).sqrMagnitude;
				if (!(sqrMagnitude <= 0.0001f) && !(sqrMagnitude > num2))
				{
					float num3 = Mathf.Sqrt(sqrMagnitude);
					val += val2 / num3 * ((num - num3) / num);
				}
			}
		}
		cachedSeparationOffset = ((((Vector3)(ref val)).sqrMagnitude > 0.0001f) ? (((Vector3)(ref val)).normalized * separationStrength) : Vector3.zero);
		nextSeparationRefreshTime = CombatRuntimeQuery.ScheduleNextRefresh((Object)(object)this, separationRefreshInterval);
	}

	private DefenderUnit FindNearestDefender()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (CombatRuntimeQuery.IsValidDefenderTarget(cachedCombatTarget) && Time.unscaledTime < nextTargetRefreshTime)
		{
			return cachedCombatTarget;
		}
		cachedCombatTarget = CombatRuntimeQuery.FindNearestDefender(defenders, ((Component)this).transform.position);
		nextTargetRefreshTime = CombatRuntimeQuery.ScheduleNextRefresh((Object)(object)this, targetRefreshInterval);
		return cachedCombatTarget;
	}

	private DefenderUnit ResolveTauntTarget()
	{
		if (tauntTimer <= 0f || (Object)(object)tauntTarget == (Object)null || tauntTarget.CurrentHealth <= 0f)
		{
			tauntTarget = null;
			tauntTimer = 0f;
			return null;
		}
		return tauntTarget;
	}

	private DefenderUnit GetRandomDefender()
	{
		List<DefenderUnit> randomDefenders = GetRandomDefenders(1);
		return (randomDefenders.Count > 0) ? randomDefenders[0] : null;
	}

	private List<DefenderUnit> GetRandomDefenders(int count)
	{
		List<DefenderUnit> list = new List<DefenderUnit>();
		for (int i = 0; i < defenders.Count; i++)
		{
			DefenderUnit defenderUnit = defenders[i];
			if (IsLivingDefender(defenderUnit))
			{
				list.Add(defenderUnit);
			}
		}
		ShuffleAndTrim(list, count);
		return list;
	}

	private static void ShuffleAndTrim(List<DefenderUnit> candidates, int count)
	{
		if (candidates != null)
		{
			for (int i = 0; i < candidates.Count; i++)
			{
				int index = Random.Range(i, candidates.Count);
				DefenderUnit value = candidates[i];
				candidates[i] = candidates[index];
				candidates[index] = value;
			}
			int num = Mathf.Max(0, count);
			if (candidates.Count > num)
			{
				candidates.RemoveRange(num, candidates.Count - num);
			}
		}
	}

	private int CountLivingDefenders()
	{
		int num = 0;
		for (int num2 = defenders.Count - 1; num2 >= 0; num2--)
		{
			DefenderUnit defenderUnit = defenders[num2];
			if ((Object)(object)defenderUnit == (Object)null)
			{
				defenders.RemoveAt(num2);
			}
			else if (defenderUnit.CurrentHealth > 0f)
			{
				num++;
			}
		}
		return num;
	}

	private void NotifySkillPresentation(SkillDefinition skill)
	{
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		ShowSkillCastFeedback(skill);
		if (skill != null && !((Object)(object)gameController == (Object)null) && IsBoss)
		{
			bool flag = skill.effectType == SkillEffectType.DeathPact || skill.effectType == SkillEffectType.MassStun || skill.effectType == SkillEffectType.BossFortify || skill.effectType == SkillEffectType.DirectDamage || skill.effectType == SkillEffectType.AreaDamage || skill.effectType == SkillEffectType.SummonRush || skill.effectType == SkillEffectType.GoldDrain || skill.effectType == SkillEffectType.ManaBurn || skill.effectType == SkillEffectType.MonsterRally;
			string text = (IsMajorBoss ? "보스 스킬: " : "중간보스 스킬: ");
			gameController.RecordBossSkillCast(skill, IsMajorBoss);
			gameController.RequestBanner(text + skill.displayName + " 발동!", definition.accentColor, flag ? 2.6f : 2f);
		}
	}

	private bool IsRangedAttacker()
	{
		return definition != null && definition.attackBehavior != null && definition.attackBehavior.basicAttackType == BasicAttackType.Ranged;
	}

	private bool CanAnyLivingDefenderRetaliate()
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		for (int num = defenders.Count - 1; num >= 0; num--)
		{
			DefenderUnit defenderUnit = defenders[num];
			if (!IsLivingDefender(defenderUnit))
			{
				if ((Object)(object)defenderUnit == (Object)null)
				{
					defenders.RemoveAt(num);
				}
			}
			else
			{
				float num2 = Mathf.Max(0.1f, defenderUnit.CurrentAttackRange - retaliationRangeMargin);
				Vector3 val = ((Component)this).transform.position - ((Component)defenderUnit).transform.position;
				if (((Vector3)(ref val)).sqrMagnitude <= num2 * num2)
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

	private bool HasDefenderWithinSkillRange(SkillDefinition skill)
	{
		return (Object)(object)FindNearestDefenderForSkill(skill) != (Object)null;
	}

	private DefenderUnit FindNearestDefenderForSkill(SkillDefinition skill)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		float num = ResolveSkillCastRange(skill);
		float num2 = num * num;
		float num3 = num2;
		DefenderUnit result = null;
		Vector3 position = ((Component)this).transform.position;
		for (int num4 = defenders.Count - 1; num4 >= 0; num4--)
		{
			DefenderUnit defenderUnit = defenders[num4];
			if (!IsLivingDefender(defenderUnit))
			{
				if ((Object)(object)defenderUnit == (Object)null)
				{
					defenders.RemoveAt(num4);
				}
			}
			else
			{
				Vector3 val = position - ((Component)defenderUnit).transform.position;
				float sqrMagnitude = ((Vector3)(ref val)).sqrMagnitude;
				if (sqrMagnitude <= num3)
				{
					num3 = sqrMagnitude;
					result = defenderUnit;
				}
			}
		}
		return result;
	}

	private List<DefenderUnit> GetLivingDefendersWithinRange(float range)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Max(0.1f, range);
		float num2 = num * num;
		Vector3 position = ((Component)this).transform.position;
		List<DefenderUnit> list = new List<DefenderUnit>();
		for (int num3 = defenders.Count - 1; num3 >= 0; num3--)
		{
			DefenderUnit defenderUnit = defenders[num3];
			if (!IsLivingDefender(defenderUnit))
			{
				if ((Object)(object)defenderUnit == (Object)null)
				{
					defenders.RemoveAt(num3);
				}
			}
			else
			{
				Vector3 val = position - ((Component)defenderUnit).transform.position;
				float sqrMagnitude = ((Vector3)(ref val)).sqrMagnitude;
				if (!(sqrMagnitude > num2))
				{
					int num4;
					for (num4 = list.Count; num4 > 0; num4--)
					{
						val = position - ((Component)list[num4 - 1]).transform.position;
						if (!(((Vector3)(ref val)).sqrMagnitude > sqrMagnitude))
						{
							break;
						}
					}
					list.Insert(num4, defenderUnit);
				}
			}
		}
		return list;
	}

	private DefenderUnit GetRandomDefenderWithinRange(float range)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Max(0.1f, range);
		float num2 = num * num;
		Vector3 position = ((Component)this).transform.position;
		DefenderUnit result = null;
		int num3 = 0;
		for (int i = 0; i < defenders.Count; i++)
		{
			DefenderUnit defenderUnit = defenders[i];
			if (!IsLivingDefender(defenderUnit))
			{
				continue;
			}
			Vector3 val = position - ((Component)defenderUnit).transform.position;
			if (!(((Vector3)(ref val)).sqrMagnitude > num2))
			{
				num3++;
				if (Random.Range(0, num3) == 0)
				{
					result = defenderUnit;
				}
			}
		}
		return result;
	}

	private List<DefenderUnit> GetRandomDefendersWithinRange(int count, float range)
	{
		List<DefenderUnit> livingDefendersWithinRange = GetLivingDefendersWithinRange(range);
		ShuffleAndTrim(livingDefendersWithinRange, count);
		return livingDefendersWithinRange;
	}

	private float ResolveSkillCastRange(SkillDefinition skill)
	{
		if (skill == null)
		{
			return 0f;
		}
		if (skill.effectType == SkillEffectType.AreaDamage)
		{
			return Mathf.Max(0.1f, skill.radius);
		}
		if (skill.useCustomCastRange)
		{
			return Mathf.Max(0.5f, skill.castRange);
		}
		if (skill.effectType == SkillEffectType.SummonRush)
		{
			return Mathf.Max(GetEffectiveAttackRange(), defaultRushCastRange);
		}
		return Mathf.Max(0.5f, GetEffectiveAttackRange());
	}

	private void NotifySkillWarning(SkillDefinition skill, float duration)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		ShowSkillWarningFeedback(skill, duration);
		if (skill != null && !((Object)(object)gameController == (Object)null) && IsBoss)
		{
			string text = (IsMajorBoss ? "보스 경고: " : "중간보스 경고: ");
			Color color = (Color)((definition != null) ? Color.Lerp(definition.accentColor, Color.white, 0.25f) : new Color(1f, 0.45f, 0.28f));
			gameController.RequestBanner(text + skill.displayName + " 준비!", color, Mathf.Max(1.15f, duration + 0.65f));
		}
	}

	private void ShowSkillCastFeedback(SkillDefinition skill)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		if (skill != null && definition != null)
		{
			Color val = ResolveSkillFeedbackColor(skill);
			float radius = ResolveSkillFeedbackRadius(skill);
			RuntimeCombatFeedback.ShowGroundPulse(((Component)this).transform.position, val, radius, IsMajorBoss ? 0.8f : (IsBoss ? 0.62f : 0.34f));
			DefenderUnit defenderUnit = FindNearestDefender();
			Vector3 val2 = ((Component)this).transform.position + Vector3.up * 0.06f;
			Quaternion rotation = (((Object)(object)defenderUnit != (Object)null) ? RuntimeEffectUtility.FaceTowards(val2, ((Component)defenderUnit).transform.position, ((Component)this).transform.rotation) : ((Component)this).transform.rotation);
			RuntimeEffectUtility.PlayOneShot(skill.muzzleEffectPrefab, val2, rotation, IsMajorBoss ? 0.85f : (IsBoss ? 0.65f : 0.3f));
			floatingUi?.ShowStatus(skill.displayName, Color.Lerp(val, Color.white, 0.25f), IsMajorBoss ? 1.25f : (IsBoss ? 1f : 0.62f));
			if (IsBoss)
			{
				RuntimeCameraShake.Request(IsMajorBoss ? 0.06f : 0.035f, IsMajorBoss ? 0.18f : 0.12f);
			}
		}
	}

	private void ShowSkillWarningFeedback(SkillDefinition skill, float duration)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		if (skill != null && definition != null)
		{
			Color color = Color.Lerp(ResolveSkillFeedbackColor(skill), Color.white, 0.18f);
			RuntimeCombatFeedback.ShowGroundWarning(((Component)this).transform.position, color, ResolveSkillFeedbackRadius(skill) * 1.12f, Mathf.Max(0.2f, duration + 0.2f));
			floatingUi?.ShowStatus("!", color, Mathf.Max(0.45f, duration));
		}
	}

	private void ShowSkillImpactFeedback(Vector3 position, SkillDefinition skill, float radius, bool shake)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		if (skill != null && definition != null)
		{
			RuntimeCombatFeedback.ShowGroundPulse(position, ResolveSkillFeedbackColor(skill), Mathf.Max(0.18f, radius), IsMajorBoss ? 0.7f : (IsBoss ? 0.46f : 0.3f));
			Quaternion rotation = RuntimeEffectUtility.FaceTowards(((Component)this).transform.position, position, ((Component)this).transform.rotation);
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
			return ((Object)(object)skill.areaEffectPrefab != (Object)null) ? skill.areaEffectPrefab : skill.hitEffectPrefab;
		}
		return ((Object)(object)skill.hitEffectPrefab != (Object)null) ? skill.hitEffectPrefab : skill.areaEffectPrefab;
	}

	private Color ResolveSkillFeedbackColor(SkillDefinition skill)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		Color val = (Color)((definition != null) ? definition.accentColor : new Color(1f, 0.35f, 0.22f));
		if (skill == null)
		{
			return val;
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
			return Color.Lerp(val, new Color(1f, 0.5f, 0.12f, 1f), 0.45f);
		default:
			return val;
		}
	}

	private float ResolveSkillFeedbackRadius(SkillDefinition skill)
	{
		if (skill == null)
		{
			return IsMajorBoss ? 1.65f : (IsBoss ? 1.15f : 0.74f);
		}
		float num = (IsMajorBoss ? 1.55f : (IsBoss ? 1.05f : 0.64f));
		if (skill.effectType == SkillEffectType.AreaDamage || skill.effectType == SkillEffectType.MonsterRally)
		{
			return Mathf.Max(num, skill.radius);
		}
		if (skill.effectType == SkillEffectType.BossFortify || skill.effectType == SkillEffectType.DeathPact)
		{
			return num * 1.2f;
		}
		if (skill.effectType == SkillEffectType.DamageReflect)
		{
			return num * 1.25f;
		}
		return num;
	}

	private void NotifySkill(SkillDefinition skill)
	{
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		if (skill != null && !((Object)(object)gameController == (Object)null) && IsBoss)
		{
			bool flag = skill.effectType == SkillEffectType.DeathPact || skill.effectType == SkillEffectType.MassStun || skill.effectType == SkillEffectType.BossFortify || skill.effectType == SkillEffectType.DirectDamage || skill.effectType == SkillEffectType.AreaDamage || skill.effectType == SkillEffectType.SummonRush || skill.effectType == SkillEffectType.GoldDrain || skill.effectType == SkillEffectType.ManaBurn || skill.effectType == SkillEffectType.MonsterRally || skill.effectType == SkillEffectType.AttackPowerReduction || skill.effectType == SkillEffectType.DamageReflect;
			string text = (IsMajorBoss ? "보스 스킬: " : "중간보스 스킬: ");
			gameController.RequestBanner(text + skill.displayName + " 발동!", definition.accentColor, flag ? 2.6f : 2f);
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
			if (tauntTimer <= 0f || (Object)(object)tauntTarget == (Object)null || tauntTarget.CurrentHealth <= 0f)
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
			SkillDefinition skillDefinition = definition.skills[i];
			if (skillCooldowns.ContainsKey(skillDefinition.id))
			{
				skillCooldowns[skillDefinition.id] = Mathf.Max(0f, skillCooldowns[skillDefinition.id] - Time.deltaTime);
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
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		if (tintRenderers == null || tintRenderers.Length == 0)
		{
			tintRenderers = ((Component)this).GetComponentsInChildren<Renderer>(true);
		}
		Color val = ResolveReadabilityTint();
		for (int i = 0; i < tintRenderers.Length; i++)
		{
			ApplyRendererTint(tintRenderers[i], val);
		}
		Color tint = (RuntimeRenderBatchingUtility.UsePerInstanceUnitTint ? val : Color.white);
		GpuSkinnedUnitRenderer.AttachOrRefresh(((Component)this).gameObject, tintRenderers, tint, isDefender: false, IsBoss);
		((Component)this).transform.localScale = Vector3.one * ResolveVisualScale();
		ConfigureBossReadabilityMarker();
	}

	private Color ResolveReadabilityTint()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		if (definition == null)
		{
			return Color.white;
		}
		if (!IsBoss)
		{
			return definition.accentColor;
		}
		Color val = (IsMajorBoss ? new Color(1f, 0.28f, 0.18f, 1f) : new Color(1f, 0.78f, 0.18f, 1f));
		return Color.Lerp(definition.accentColor, val, IsMajorBoss ? 0.52f : 0.38f);
	}

	private void ConfigureBossReadabilityMarker()
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Expected O, but got Unknown
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		if (!IsBoss)
		{
			if ((Object)(object)bossReadabilityMarker != (Object)null)
			{
				Object.Destroy((Object)(object)bossReadabilityMarker);
				bossReadabilityMarker = null;
			}
			return;
		}
		if ((Object)(object)bossReadabilityMarker == (Object)null)
		{
			bossReadabilityMarker = new GameObject("BossReadabilityMarker");
			bossReadabilityMarker.transform.SetParent(((Component)this).transform, false);
			LineRenderer val = bossReadabilityMarker.AddComponent<LineRenderer>();
			val.useWorldSpace = false;
			val.loop = true;
			val.positionCount = 64;
			val.numCornerVertices = 4;
			val.numCapVertices = 4;
			((Renderer)val).material = new Material(Shader.Find("Sprites/Default"));
		}
		LineRenderer component = bossReadabilityMarker.GetComponent<LineRenderer>();
		if (!((Object)(object)component == (Object)null))
		{
			float num = (IsMajorBoss ? 1.08f : 0.82f);
			float widthMultiplier = (IsMajorBoss ? 0.12f : 0.085f);
			Color val2 = (IsMajorBoss ? new Color(1f, 0.18f, 0.1f, 0.92f) : new Color(1f, 0.78f, 0.12f, 0.88f));
			component.widthMultiplier = widthMultiplier;
			component.startColor = val2;
			component.endColor = val2;
			for (int i = 0; i < component.positionCount; i++)
			{
				float num2 = MathF.PI * 2f * (float)i / (float)component.positionCount;
				component.SetPosition(i, new Vector3(Mathf.Cos(num2) * num, 0.035f, Mathf.Sin(num2) * num));
			}
		}
	}

	private void ApplyRendererTint(Renderer renderer, Color color)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		if ((Object)(object)renderer == (Object)null)
		{
			return;
		}
		PrepareRuntimeUnitRenderer(renderer);
		if (!RuntimeRenderBatchingUtility.UsePerInstanceUnitTint)
		{
			renderer.SetPropertyBlock((MaterialPropertyBlock)null);
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
			renderer.shadowCastingMode = (ShadowCastingMode)0;
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
		if ((Object)(object)animationDriver == (Object)null)
		{
			animationDriver = ((Component)this).GetComponent<UnitAnimationDriver>();
			if ((Object)(object)animationDriver == (Object)null)
			{
				animationDriver = ((Component)this).gameObject.AddComponent<UnitAnimationDriver>();
			}
		}
		BindAnimationDriver();
	}

	private void BindAnimationDriver()
	{
		if (!((Object)(object)subscribedAnimationDriver == (Object)(object)animationDriver))
		{
			UnbindAnimationDriver();
			subscribedAnimationDriver = animationDriver;
			if ((Object)(object)subscribedAnimationDriver != (Object)null)
			{
				subscribedAnimationDriver.ImpactTriggered += HandleAnimationImpact;
			}
		}
	}

	private void UnbindAnimationDriver()
	{
		if ((Object)(object)subscribedAnimationDriver != (Object)null)
		{
			subscribedAnimationDriver.ImpactTriggered -= HandleAnimationImpact;
			subscribedAnimationDriver = null;
		}
	}

	private bool CanStartActionAnimation()
	{
		return (Object)(object)animationDriver == (Object)null || !animationDriver.IsLocked;
	}

	private bool IsActionAnimationLocked()
	{
		return (Object)(object)animationDriver != (Object)null && animationDriver.IsLocked;
	}

	private void ApplyPetrifyMaterials(Material materialOverride)
	{
		Material val = ResolvePetrifyMaterial(materialOverride);
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		if (tintRenderers == null || tintRenderers.Length == 0)
		{
			tintRenderers = ((Component)this).GetComponentsInChildren<Renderer>(true);
		}
		if (petrifyMaterialSnapshots.Count == 0)
		{
			for (int i = 0; i < tintRenderers.Length; i++)
			{
				Renderer val2 = tintRenderers[i];
				if (CanSwapPetrifyMaterial(val2))
				{
					Material[] sharedMaterials = val2.sharedMaterials;
					if (sharedMaterials != null && sharedMaterials.Length != 0)
					{
						petrifyMaterialSnapshots.Add(new RendererMaterialSnapshot
						{
							renderer = val2,
							materials = sharedMaterials
						});
					}
				}
			}
		}
		for (int j = 0; j < petrifyMaterialSnapshots.Count; j++)
		{
			Renderer renderer = petrifyMaterialSnapshots[j].renderer;
			Material[] materials = petrifyMaterialSnapshots[j].materials;
			if (!((Object)(object)renderer == (Object)null) && materials != null && materials.Length != 0)
			{
				Material[] array = (Material[])(object)new Material[materials.Length];
				for (int k = 0; k < array.Length; k++)
				{
					array[k] = val;
				}
				renderer.sharedMaterials = array;
				renderer.SetPropertyBlock((MaterialPropertyBlock)null);
			}
		}
	}

	private void RestorePetrifyMaterials(bool reapplyVisuals = true)
	{
		if (petrifyMaterialSnapshots.Count == 0)
		{
			return;
		}
		for (int num = petrifyMaterialSnapshots.Count - 1; num >= 0; num--)
		{
			Renderer renderer = petrifyMaterialSnapshots[num].renderer;
			Material[] materials = petrifyMaterialSnapshots[num].materials;
			if ((Object)(object)renderer != (Object)null && materials != null)
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
		Animator[] componentsInChildren = ((Component)this).GetComponentsInChildren<Animator>(true);
		foreach (Animator val in componentsInChildren)
		{
			if ((Object)(object)val == (Object)null)
			{
				continue;
			}
			bool flag = false;
			for (int j = 0; j < petrifyAnimatorSnapshots.Count; j++)
			{
				if ((Object)(object)petrifyAnimatorSnapshots[j].animator == (Object)(object)val)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				petrifyAnimatorSnapshots.Add(new AnimatorSpeedSnapshot
				{
					animator = val,
					speed = val.speed
				});
			}
			val.speed = 0f;
		}
	}

	private void RestorePetrifyAnimations(bool resumeAnimation = true)
	{
		for (int num = petrifyAnimatorSnapshots.Count - 1; num >= 0; num--)
		{
			AnimatorSpeedSnapshot animatorSpeedSnapshot = petrifyAnimatorSnapshots[num];
			if (animatorSpeedSnapshot != null && (Object)(object)animatorSpeedSnapshot.animator != (Object)null)
			{
				animatorSpeedSnapshot.animator.speed = animatorSpeedSnapshot.speed;
			}
		}
		petrifyAnimatorSnapshots.Clear();
		if (resumeAnimation && (Object)(object)animationDriver != (Object)null && !isDying)
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
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)materialOverride != (Object)null)
		{
			return materialOverride;
		}
		if ((Object)(object)defaultPetrifyMaterial != (Object)null)
		{
			return defaultPetrifyMaterial;
		}
		if ((Object)(object)fallbackPetrifyMaterial != (Object)null)
		{
			return fallbackPetrifyMaterial;
		}
		Shader val = Shader.Find("Universal Render Pipeline/Lit");
		if ((Object)(object)val == (Object)null)
		{
			val = Shader.Find("Standard");
		}
		if ((Object)(object)val == (Object)null)
		{
			val = Shader.Find("Sprites/Default");
		}
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		fallbackPetrifyMaterial = new Material(val);
		((Object)fallbackPetrifyMaterial).name = "RuntimeFallbackPetrifyMaterial";
		Color val2 = default(Color);
		((Color)(ref val2))._002Ector(0.58f, 0.62f, 0.64f, 1f);
		fallbackPetrifyMaterial.color = val2;
		if (fallbackPetrifyMaterial.HasProperty("_BaseColor"))
		{
			fallbackPetrifyMaterial.SetColor("_BaseColor", val2);
		}
		return fallbackPetrifyMaterial;
	}

	private void EnsureHitFlashFeedback()
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)hitFlashFeedback == (Object)null)
		{
			hitFlashFeedback = ((Component)this).GetComponent<HitFlashFeedback>();
			if ((Object)(object)hitFlashFeedback == (Object)null)
			{
				hitFlashFeedback = ((Component)this).gameObject.AddComponent<HitFlashFeedback>();
			}
		}
		hitFlashFeedback.Configure(tintRenderers, (definition != null) ? definition.accentColor : Color.white, RuntimeRenderBatchingUtility.UsePerInstanceUnitTint);
	}

	private void PlayDeathEffect()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		RuntimeEffectUtility.PlayOneShot(deathEffectPrefab, ((Component)this).transform.position + deathEffectOffset, Quaternion.identity, 3f);
	}

	private float GetEffectiveAttackPower()
	{
		return (definition != null) ? (definition.stats.attackPower * outgameAttackMultiplier * (1f - fateStatCrushRatio)) : 0f;
	}

	private float GetEffectiveAttackRange()
	{
		if (definition == null)
		{
			return 0f;
		}
		float num = definition.attackBehavior.ResolveAttackRange(definition.stats.attackRange);
		return IsRangedAttacker() ? Mathf.Min(Mathf.Max(0.5f, maximumRangedAttackRange), num) : num;
	}

	private void ApplyBasicAttackExtensions(DefenderUnit primaryTarget, float damage)
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)primaryTarget == (Object)null || definition == null)
		{
			return;
		}
		float splashRadius = definition.attackBehavior.splashRadius;
		float splashDamageRatio = definition.attackBehavior.splashDamageRatio;
		int num = Mathf.Max(0, definition.attackBehavior.additionalPierceCount);
		if (splashRadius > 0f && splashDamageRatio > 0f)
		{
			float num2 = splashRadius * splashRadius;
			Vector3 position = ((Component)primaryTarget).transform.position;
			for (int i = 0; i < defenders.Count; i++)
			{
				DefenderUnit defenderUnit = defenders[i];
				if (IsLivingDefender(defenderUnit) && !((Object)(object)defenderUnit == (Object)(object)primaryTarget))
				{
					Vector3 val = position - ((Component)defenderUnit).transform.position;
					if (((Vector3)(ref val)).sqrMagnitude <= num2)
					{
						defenderUnit.TakeDamage(damage * splashDamageRatio, critical: false, this);
					}
				}
			}
		}
		if (num <= 0)
		{
			return;
		}
		List<DefenderUnit> livingDefendersWithinRange = GetLivingDefendersWithinRange(float.MaxValue);
		int num3 = 0;
		for (int j = 0; j < livingDefendersWithinRange.Count; j++)
		{
			if (num3 >= num)
			{
				break;
			}
			DefenderUnit defenderUnit2 = livingDefendersWithinRange[j];
			if (!((Object)(object)defenderUnit2 == (Object)(object)primaryTarget))
			{
				defenderUnit2.TakeDamage(damage, critical: false, this);
				num3++;
			}
		}
	}

	private void HandleDefenderSpawned(DefenderUnit defender)
	{
		if ((Object)(object)defender != (Object)null && !defenders.Contains(defender))
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
		if ((Object)(object)cachedCombatTarget == (Object)(object)defender)
		{
			InvalidateTargetCache();
		}
		if ((Object)(object)tauntTarget == (Object)(object)defender)
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
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		InvalidateTargetCache();
		cachedSeparationOffset = Vector3.zero;
		nextSeparationRefreshTime = 0f;
	}
}
