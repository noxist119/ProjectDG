using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseGame;

public class DefenderUnit : MonoBehaviour
{
	private struct PendingBasicAttack
	{
		public bool isValid;

		public int sequence;

		public MonsterUnit target;

		public float damage;

		public bool critical;
	}

	private struct PendingSkillCast
	{
		public bool isValid;

		public int sequence;

		public SkillDefinition skill;

		public MonsterUnit target;

		public float skillMultiplier;
	}

	private static readonly List<DefenderUnit> ActiveDefenders = new List<DefenderUnit>();

	private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

	private static readonly int ColorId = Shader.PropertyToID("_Color");

	private static readonly Color HealFeedbackColor = new Color(0.38f, 1f, 0.62f, 1f);

	private static readonly Color ManaFeedbackColor = new Color(0.36f, 0.78f, 1f, 1f);

	private static readonly Color ShieldFeedbackColor = new Color(0.55f, 0.88f, 1f, 1f);

	private static readonly Color BuffFeedbackColor = new Color(1f, 0.86f, 0.3f, 1f);

	private static readonly Color AttackSpeedFeedbackColor = new Color(0.52f, 1f, 0.92f, 1f);

	private static readonly Color DebuffFeedbackColor = new Color(1f, 0.36f, 0.28f, 1f);

	[SerializeField]
	private Transform firePoint;

	[SerializeField]
	private Projectile projectilePrefab;

	[SerializeField]
	private GameObject defaultSummonedUnitPrefab;

	[SerializeField]
	private GameObject defaultMuzzleEffectPrefab;

	[SerializeField]
	private GameObject defaultHitEffectPrefab;

	[SerializeField]
	private GameObject defaultAreaEffectPrefab;

	[SerializeField]
	private GameObject deathEffectPrefab;

	[SerializeField]
	private Vector3 deathEffectOffset = new Vector3(0f, 0.6f, 0f);

	[SerializeField]
	private Renderer[] tintRenderers;

	[Header("Runtime Performance")]
	[SerializeField]
	[Min(0.02f)]
	private float basicTargetRefreshInterval = 0.08f;

	private CharacterDefinition definition;

	private BoardSlot currentSlot;

	private FloatingCombatUI floatingUi;

	private UnitAnimationDriver animationDriver;

	private HitFlashFeedback hitFlashFeedback;

	private float currentHealth;

	private float currentMana;

	private float currentShield;

	private float attackCooldown;

	private float attackSpeedBonus;

	private float critChanceBonus;

	private float attackRangeBonus;

	private float splashRadiusBonus;

	private float splashDamageRatioBonus;

	private float permanentAttackPowerBonus;

	private float permanentAttackSpeedBonus;

	private float permanentCritChanceBonus;

	private float permanentManaRegenRateBonus;

	private float permanentMaxHealthBonus;

	private float permanentSkillPowerBonus;

	private float permanentSkillParameterGrowthBonus;

	private float permanentCriticalDamageBonus;

	private float permanentBossDamageBonus;

	private float permanentDamageReductionBonus;

	private float tileAttackPowerBonus;

	private float tileAttackSpeedBonus;

	private float tileManaRegenRateBonus;

	private float tileMaxHealthBonus;

	private float tileSkillPowerBonus;

	private float tileBossDamageBonus;

	private float tileAttackRangeBonus;

	private float tileDamageReductionBonus;

	private float tileLifeStealRatio;

	private float temporaryAttackPowerBonus;

	private float temporaryAttackSpeedBonus;

	private float temporaryAttackPowerReduction;

	private float temporaryAttackPowerReductionTimer;

	private float temporaryCombatBoostTimer;

	private UnitSynergyBonus synergyBonus;

	private float attackSpeedBuffTimer;

	private float critBuffTimer;

	private float shieldTimer;

	private float stunTimer;

	private float temporaryDamageReductionBonus;

	private float temporaryDamageReductionTimer;

	private float thornsReturnRatio;

	private float thornsTimer;

	private readonly List<MonsterUnit> monsters = new List<MonsterUnit>();

	private readonly Dictionary<string, float> skillCooldowns = new Dictionary<string, float>();

	private Quaternion defaultFacingRotation = Quaternion.identity;

	private bool hasDefaultFacing;

	private MaterialPropertyBlock visualPropertyBlock;

	private UnitAnimationDriver subscribedAnimationDriver;

	private PendingBasicAttack pendingBasicAttack;

	private PendingSkillCast pendingSkillCast;

	private Coroutine pendingAttackImpactRoutine;

	private Coroutine pendingSkillImpactRoutine;

	private int impactSequence;

	private bool isTemporarySummon;

	private bool isDying;

	private DefenseGameController alternatingRoundController;

	private bool alternatingNextRoundWillBurst;

	private bool alternatingBurstPending;

	private bool alternatingDormant;

	private int alternatingLastRound = -1;

	private readonly List<GameObject> ownedSupportEffects = new List<GameObject>();

	private GameObject activeShieldEffect;

	private MonsterUnit cachedBasicAttackTarget;

	private float nextBasicTargetRefreshTime;

	internal static SkillDefinition CurrentDamageSkillContext { get; private set; }

	public static IReadOnlyList<DefenderUnit> ActiveInstances
	{
		get
		{
			PruneMissingActiveDefenders();
			return ActiveDefenders;
		}
	}

	public CharacterDefinition Definition => definition;

	public float EffectiveAttackPower => (definition != null) ? GetEffectiveAttackPower() : 0f;

	public float ActiveAttackPowerReductionRatio => (temporaryAttackPowerReductionTimer > 0f) ? temporaryAttackPowerReduction : 0f;

	public CharacterGrade Grade => (definition != null) ? definition.grade : CharacterGrade.Normal;

	public CharacterRole Role => (definition != null) ? definition.role : CharacterRole.Vanguard;

	public BoardSlot CurrentSlot => currentSlot;

	public bool IsTemporarySummon => isTemporarySummon;

	public float CurrentHealth => currentHealth;

	public float MaxHealth => (definition != null) ? (definition.stats.maxHealth * Mathf.Max(0.1f, 1f + permanentMaxHealthBonus + synergyBonus.maxHealthBonus + tileMaxHealthBonus)) : 0f;

	public float CurrentMana => currentMana;

	public float MaxMana => (definition != null) ? definition.stats.maxMana : 0f;

	public float HealthRatio => (MaxHealth > 0f) ? Mathf.Clamp01(currentHealth / MaxHealth) : 0f;

	public bool CanBeCombatTargeted => !isDying && currentHealth > 0f;

	public bool IsStunned => stunTimer > 0f;

	public float CurrentAttackRange => (definition != null) ? GetEffectiveAttackRange() : 0f;

	public static event Action<DefenderUnit> OnDefenderSpawned;

	public static event Action<DefenderUnit> OnDefenderRemoved;

	public static event Action<DefenderUnit, MonsterUnit, float, bool> OnDamageDealt;

	public static event Action<DefenderUnit, SkillDefinition, MonsterUnit> OnSkillCast;

	public static event Action<DefenderUnit, float, bool, MonsterUnit> OnShieldResolved;

	public static event Action<DefenderUnit, MonsterUnit, float> OnDamageTaken;

	public void SetRecipeMaterialMarker(bool active, string label, Color color)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		floatingUi?.SetRecipeMarker(active, label, color);
	}

	internal static void ReportDamageDealt(DefenderUnit source, MonsterUnit target, float damage, bool critical)
	{
		if (!((Object)(object)source == (Object)null) && !((Object)(object)target == (Object)null) && !(damage <= 0f))
		{
			DefenderUnit.OnDamageDealt?.Invoke(source, target, damage, critical);
			source.ApplyTileLifeSteal(damage);
		}
	}

	private void ApplyTileLifeSteal(float damage)
	{
		if (!(tileLifeStealRatio <= 0f) && !(damage <= 0f) && !(currentHealth <= 0f) && !(MaxHealth <= 0f) && !(currentHealth >= MaxHealth))
		{
			Heal(damage * tileLifeStealRatio);
		}
	}

	internal static void RunWithSkillDamageContext(SkillDefinition skill, Action action)
	{
		if (action == null)
		{
			return;
		}
		SkillDefinition currentDamageSkillContext = CurrentDamageSkillContext;
		CurrentDamageSkillContext = skill;
		try
		{
			action();
		}
		finally
		{
			CurrentDamageSkillContext = currentDamageSkillContext;
		}
	}

	private void OnEnable()
	{
		RegisterActiveDefender();
		BindAlternatingRoundController();
		MonsterUnit.OnMonsterSpawned += HandleMonsterSpawned;
		MonsterUnit.OnMonsterKilled += HandleMonsterRemoved;
		MonsterUnit.OnMonsterEscaped += HandleMonsterRemoved;
	}

	private void OnDisable()
	{
		UnregisterActiveDefender();
		MonsterUnit.OnMonsterSpawned -= HandleMonsterSpawned;
		MonsterUnit.OnMonsterKilled -= HandleMonsterRemoved;
		MonsterUnit.OnMonsterEscaped -= HandleMonsterRemoved;
		UnbindAlternatingRoundController();
		UnbindAnimationDriver();
		ClearPendingImpacts();
		ClearOwnedSupportEffects();
		InvalidateBasicTargetCache();
	}

	private void RegisterActiveDefender()
	{
		if (!ActiveDefenders.Contains(this))
		{
			ActiveDefenders.Add(this);
		}
	}

	private void UnregisterActiveDefender()
	{
		ActiveDefenders.Remove(this);
	}

	private static void PruneMissingActiveDefenders()
	{
		for (int num = ActiveDefenders.Count - 1; num >= 0; num--)
		{
			if ((Object)(object)ActiveDefenders[num] == (Object)null)
			{
				ActiveDefenders.RemoveAt(num);
			}
		}
	}

	private bool IsAlternatingRoundBurstUnit()
	{
		return definition != null && string.Equals(definition.id, "hero_56", StringComparison.OrdinalIgnoreCase);
	}

	private void BindAlternatingRoundController()
	{
		if (!IsAlternatingRoundBurstUnit())
		{
			UnbindAlternatingRoundController();
			return;
		}
		DefenseGameController defenseGameController = (((Object)(object)DefenseGameController.Active != (Object)null) ? DefenseGameController.Active : Object.FindObjectOfType<DefenseGameController>());
		if (!((Object)(object)defenseGameController == (Object)(object)alternatingRoundController))
		{
			UnbindAlternatingRoundController();
			alternatingRoundController = defenseGameController;
			if ((Object)(object)alternatingRoundController != (Object)null)
			{
				alternatingRoundController.OnRoundStarted += HandleAlternatingRoundStarted;
			}
		}
	}

	private void UnbindAlternatingRoundController()
	{
		if ((Object)(object)alternatingRoundController != (Object)null)
		{
			alternatingRoundController.OnRoundStarted -= HandleAlternatingRoundStarted;
			alternatingRoundController = null;
		}
	}

	private void HandleAlternatingRoundStarted(int round)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		if (IsAlternatingRoundBurstUnit() && round != alternatingLastRound)
		{
			alternatingLastRound = round;
			if (alternatingNextRoundWillBurst)
			{
				alternatingBurstPending = true;
				alternatingDormant = false;
				alternatingNextRoundWillBurst = false;
				currentMana = MaxMana;
				ShowInstantSupportFeedback("기동 준비", AttackSpeedFeedbackColor, null, 0.8f);
			}
			else
			{
				alternatingBurstPending = false;
				alternatingDormant = true;
				alternatingNextRoundWillBurst = true;
				currentMana = 0f;
				animationDriver?.PlayDormantLoop();
				ShowInstantSupportFeedback("휴식 모드", ShieldFeedbackColor, null, 0.8f);
			}
		}
	}

	public void ConfigureRuntimePieces(Projectile projectileTemplate, Transform launchPoint, Renderer[] renderers, GameObject summonedUnitTemplate = null, GameObject muzzleEffectTemplate = null, GameObject hitEffectTemplate = null, GameObject areaEffectTemplate = null, GameObject deathEffectTemplate = null)
	{
		projectilePrefab = projectileTemplate;
		firePoint = launchPoint;
		tintRenderers = renderers;
		defaultSummonedUnitPrefab = summonedUnitTemplate;
		defaultMuzzleEffectPrefab = muzzleEffectTemplate;
		defaultHitEffectPrefab = hitEffectTemplate;
		defaultAreaEffectPrefab = areaEffectTemplate;
		deathEffectPrefab = deathEffectTemplate;
	}

	public void AdoptRuntimeTemplate(DefenderUnit template)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected O, but got Unknown
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)template == (Object)null)
		{
			return;
		}
		projectilePrefab = template.projectilePrefab;
		defaultSummonedUnitPrefab = template.defaultSummonedUnitPrefab;
		defaultMuzzleEffectPrefab = template.defaultMuzzleEffectPrefab;
		defaultHitEffectPrefab = template.defaultHitEffectPrefab;
		defaultAreaEffectPrefab = template.defaultAreaEffectPrefab;
		deathEffectPrefab = template.deathEffectPrefab;
		deathEffectOffset = template.deathEffectOffset;
		if ((Object)(object)firePoint == (Object)null)
		{
			Transform val = ((Component)this).transform.Find("FirePoint");
			if ((Object)(object)val != (Object)null)
			{
				firePoint = val;
			}
			else
			{
				GameObject val2 = new GameObject("FirePoint");
				val2.transform.SetParent(((Component)this).transform);
				val2.transform.localPosition = new Vector3(0f, 0.8f, 0.6f);
				firePoint = val2.transform;
			}
		}
		if (tintRenderers == null || tintRenderers.Length == 0)
		{
			tintRenderers = ((Component)this).GetComponentsInChildren<Renderer>(true);
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
		attackCooldown -= Time.deltaTime;
		floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
		bool flag = IsCombatActive();
		bool flag2 = IsAlternatingRoundBurstUnit();
		if (flag2 && (!flag || alternatingDormant || !alternatingBurstPending))
		{
			animationDriver?.PlayDormantLoop();
			return;
		}
		animationDriver?.PlayMoving(isMoving: false);
		if (flag2)
		{
			currentMana = MaxMana;
		}
		else if (flag)
		{
			RegenerateCombatMana();
		}
		if (stunTimer > 0f)
		{
			if ((Object)(object)animationDriver == (Object)null || !animationDriver.IsLocked)
			{
				animationDriver?.ForceIdle();
			}
		}
		else if (!flag)
		{
			ResetFacingToDefault();
			if ((Object)(object)animationDriver == (Object)null || !animationDriver.IsLocked)
			{
				animationDriver?.ForceIdle();
			}
		}
		else if (IsAlternatingRoundBurstUnit())
		{
			if (TryCastSkill())
			{
				alternatingBurstPending = false;
				alternatingDormant = true;
			}
		}
		else if (!TryCastSkill())
		{
			MonsterUnit monsterUnit = FindBasicAttackTarget();
			if (!((Object)(object)monsterUnit == (Object)null) && attackCooldown <= 0f && CanStartActionAnimation())
			{
				PerformAttack(monsterUnit);
			}
		}
	}

	private void RegenerateCombatMana()
	{
		if (!(MaxMana <= 0f))
		{
			float num = Mathf.Clamp01(definition.stats.manaRegenPerSecondRate + permanentManaRegenRateBonus + synergyBonus.manaRegenRateBonus + tileManaRegenRateBonus);
			if (!(num <= 0f))
			{
				currentMana = Mathf.Min(MaxMana, currentMana + MaxMana * num * Time.deltaTime);
				floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
			}
		}
	}

	public void Initialize(CharacterDefinition newDefinition)
	{
		Initialize(newDefinition, temporarySummon: false);
	}

	public void InitializeSummon(CharacterDefinition newDefinition)
	{
		Initialize(newDefinition, temporarySummon: true);
	}

	private void Initialize(CharacterDefinition newDefinition, bool temporarySummon)
	{
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		definition = newDefinition;
		isTemporarySummon = temporarySummon;
		if (isTemporarySummon)
		{
			currentSlot = null;
			hasDefaultFacing = false;
		}
		attackSpeedBonus = 0f;
		critChanceBonus = 0f;
		attackRangeBonus = 0f;
		splashRadiusBonus = 0f;
		splashDamageRatioBonus = 0f;
		permanentAttackPowerBonus = 0f;
		permanentAttackSpeedBonus = 0f;
		permanentCritChanceBonus = 0f;
		permanentManaRegenRateBonus = 0f;
		permanentMaxHealthBonus = 0f;
		permanentSkillPowerBonus = 0f;
		permanentSkillParameterGrowthBonus = 0f;
		permanentCriticalDamageBonus = 0f;
		permanentBossDamageBonus = 0f;
		permanentDamageReductionBonus = 0f;
		tileAttackPowerBonus = 0f;
		tileAttackSpeedBonus = 0f;
		tileManaRegenRateBonus = 0f;
		tileMaxHealthBonus = 0f;
		tileSkillPowerBonus = 0f;
		tileBossDamageBonus = 0f;
		tileAttackRangeBonus = 0f;
		tileDamageReductionBonus = 0f;
		tileLifeStealRatio = 0f;
		temporaryAttackPowerBonus = 0f;
		temporaryAttackSpeedBonus = 0f;
		temporaryAttackPowerReduction = 0f;
		temporaryAttackPowerReductionTimer = 0f;
		temporaryCombatBoostTimer = 0f;
		temporaryDamageReductionBonus = 0f;
		temporaryDamageReductionTimer = 0f;
		thornsReturnRatio = 0f;
		thornsTimer = 0f;
		synergyBonus = default(UnitSynergyBonus);
		isDying = false;
		alternatingNextRoundWillBurst = IsAlternatingRoundBurstUnit();
		alternatingBurstPending = false;
		alternatingDormant = IsAlternatingRoundBurstUnit();
		alternatingLastRound = -1;
		BindAlternatingRoundController();
		ClearOwnedSupportEffects();
		if (!isTemporarySummon && (Object)(object)OutgameProgressionSystem.Active != (Object)null)
		{
			OutgameProgressionSystem.Active.ApplyGrowthToDefender(this, definition);
		}
		currentHealth = MaxHealth;
		currentMana = 0f;
		currentShield = 0f;
		attackCooldown = 0f;
		attackSpeedBuffTimer = 0f;
		critBuffTimer = 0f;
		shieldTimer = 0f;
		stunTimer = 0f;
		monsters.Clear();
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if ((Object)(object)monsterUnit != (Object)null)
			{
				monsters.Add(monsterUnit);
			}
		}
		InvalidateBasicTargetCache();
		skillCooldowns.Clear();
		((Object)((Component)this).gameObject).name = (isTemporarySummon ? definition.displayName : (definition.displayName + "_" + definition.grade));
		ApplyVisuals();
		EnsureAnimationDriver();
		UnitAnimatorLodController.AttachOrRefresh(((Component)this).gameObject, animationDriver, defender: true, boss: false);
		EnsureHitFlashFeedback();
		EnsureInteractionCollider();
		floatingUi = FloatingCombatUI.Attach(((Component)this).transform, definition.displayName, definition.accentColor, definition.grade, GetFloatingUiFallbackHeight());
		floatingUi.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
		if (!isTemporarySummon && (Object)(object)currentSlot != (Object)null)
		{
			currentSlot.RefreshTileBonus(showFeedback: true);
		}
		animationDriver?.PlaySpawn();
		if (IsAlternatingRoundBurstUnit())
		{
			animationDriver?.PlayDormantLoop();
		}
		DefenderUnit.OnDefenderSpawned?.Invoke(this);
	}

	private float GetFloatingUiFallbackHeight()
	{
		if (definition == null)
		{
			return 1.55f;
		}
		float num = 1.46f;
		if (definition.grade == CharacterGrade.Legendary)
		{
			num = 1.54f;
		}
		else if (definition.grade == CharacterGrade.Mythic)
		{
			num = 1.62f;
		}
		else if (definition.grade == CharacterGrade.Transcendent)
		{
			num = 1.72f;
		}
		if (definition.role == CharacterRole.Vanguard || definition.role == CharacterRole.Summoner)
		{
			num += 0.08f;
		}
		return num;
	}

	public void SetSlot(BoardSlot slot)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		currentSlot = slot;
		defaultFacingRotation = ((Component)this).transform.rotation;
		hasDefaultFacing = true;
	}

	public void RemoveFromBoard()
	{
		if ((Object)(object)currentSlot != (Object)null)
		{
			currentSlot.Clear();
			currentSlot = null;
		}
	}

	public void DismissTemporarySummon()
	{
		if (isTemporarySummon && !isDying)
		{
			isDying = true;
			ClearPendingImpacts();
			ClearOwnedSupportEffects();
			RemoveFromBoard();
			DefenderUnit.OnDefenderRemoved?.Invoke(this);
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
	}

	public void RetireFromBoard()
	{
		if (!isTemporarySummon && !isDying)
		{
			isDying = true;
			ClearPendingImpacts();
			ClearOwnedSupportEffects();
			RemoveFromBoard();
			DefenderUnit.OnDefenderRemoved?.Invoke(this);
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
	}

	public void TakeDamage(float damage, bool critical)
	{
		TakeDamage(damage, critical, null);
	}

	public void TakeDamage(float damage, bool critical, MonsterUnit source)
	{
		float num = Mathf.Clamp01(permanentDamageReductionBonus + synergyBonus.damageReductionBonus + temporaryDamageReductionBonus + tileDamageReductionBonus);
		float num2 = damage * (1f - num);
		float num3 = 0f;
		bool arg = false;
		if (currentShield > 0f)
		{
			num3 = Mathf.Min(currentShield, num2);
			currentShield -= num3;
			num2 -= num3;
			if (currentShield <= 0f)
			{
				currentShield = 0f;
				shieldTimer = 0f;
				arg = true;
				ClearShieldEffect();
			}
		}
		if (num3 > 0f)
		{
			DefenderUnit.OnShieldResolved?.Invoke(this, num3, arg, source);
		}
		currentHealth -= num2;
		if (num2 > 0f)
		{
			DefenderUnit.OnDamageTaken?.Invoke(this, source, num2);
		}
		if (IsCombatActive())
		{
			float num4 = Mathf.Clamp01(definition.stats.manaGainWhenHitRate + synergyBonus.manaGainWhenHitRateBonus);
			currentMana = Mathf.Min(MaxMana, currentMana + MaxMana * num4);
		}
		hitFlashFeedback?.PlayHit(critical);
		RuntimeAudioUtility.PlayHit();
		if (num2 > 0f)
		{
			floatingUi?.ShowDamage(num2, critical, healing: false);
		}
		else if (num3 > 0f)
		{
			floatingUi?.ShowDamage(num3, critical: false, healing: true);
		}
		floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
		if ((Object)(object)source != (Object)null && thornsTimer > 0f && thornsReturnRatio > 0f && num2 > 0f)
		{
			source.TakeDamage(num2 * thornsReturnRatio, critical: false, this);
		}
		if (currentHealth <= 0f)
		{
			Die();
		}
	}

	public void Heal(float amount, GameObject effectPrefab = null)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		float num = currentHealth;
		currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
		float num2 = currentHealth - num;
		if (!(num2 <= 0f))
		{
			floatingUi?.ShowDamage(num2, critical: false, healing: true);
			ShowInstantSupportFeedback("회복", HealFeedbackColor, effectPrefab, 1.1f);
			floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
		}
	}

	public void AddShield(float amount, float duration, GameObject effectPrefab = null)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		if (!(amount <= 0f))
		{
			currentShield = Mathf.Max(currentShield, amount);
			shieldTimer = Mathf.Max(shieldTimer, duration);
			hitFlashFeedback?.PlayHit(critical: false);
			floatingUi?.ShowDamage(amount, critical: false, healing: true);
			ClearShieldEffect();
			activeShieldEffect = ShowTimedSupportFeedback("방어막", ShieldFeedbackColor, shieldTimer, effectPrefab);
		}
	}

	public void DrainMana(float ratio)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		if (!(MaxMana <= 0f))
		{
			float num = Mathf.Min(currentMana, MaxMana * Mathf.Clamp01(ratio));
			if (!(num <= 0f))
			{
				currentMana = Mathf.Max(0f, currentMana - num);
				hitFlashFeedback?.PlayHit(critical: false);
				ShowInstantSupportFeedback("마나 -", ManaFeedbackColor, null, 0.85f);
				floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
			}
		}
	}

	public void RestoreMana(float ratio, GameObject effectPrefab = null)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		if (!(MaxMana <= 0f) && !(ratio <= 0f))
		{
			float num = currentMana;
			currentMana = Mathf.Min(MaxMana, currentMana + MaxMana * Mathf.Clamp01(ratio));
			if (!(currentMana <= num))
			{
				hitFlashFeedback?.PlayHit(critical: false);
				ShowInstantSupportFeedback("마나 +", ManaFeedbackColor, effectPrefab, 1f);
				floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
			}
		}
	}

	public void ApplyStun(float duration)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		stunTimer = Mathf.Max(stunTimer, duration);
		attackCooldown = Mathf.Max(attackCooldown, Mathf.Min(duration, 1.2f));
		hitFlashFeedback?.PlayHit(critical: true);
		ShowTimedSupportFeedback("기절 · 행동 불가", DebuffFeedbackColor, duration, null);
		RuntimeCombatFeedback.ShowGroundPulse(((Component)this).transform.position, DebuffFeedbackColor, 0.62f, 0.72f, 0.1f);
	}

	public void ApplyMergeInheritance(float inheritedAttackPower, float inheritedMaxHealth)
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		if (definition != null)
		{
			float num = Mathf.Max(0.01f, GetEffectiveAttackPower());
			float num2 = Mathf.Max(0f, inheritedAttackPower / num - 1f);
			if (num2 > 0f)
			{
				AddAttackPowerBonus(num2);
			}
			float num3 = Mathf.Max(0.01f, MaxHealth);
			float num4 = Mathf.Max(0f, inheritedMaxHealth / num3 - 1f);
			if (num4 > 0f)
			{
				AddMaxHealthBonus(num4);
			}
			floatingUi?.ShowStatus("합성 능력 계승", new Color(1f, 0.86f, 0.3f, 1f), 1.25f);
		}
	}

	public void KillByBossSkill()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if (!(currentHealth <= 0f))
		{
			currentHealth = 0f;
			floatingUi?.ShowDamage(MaxHealth, critical: true, healing: false);
			ShowInstantSupportFeedback("처형", DebuffFeedbackColor, null, 0.9f);
			Die();
		}
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

	public void AddAttackPowerBonus(float ratioBonus)
	{
		permanentAttackPowerBonus += ratioBonus;
	}

	public void AddPermanentAttackSpeedBonus(float ratioBonus)
	{
		permanentAttackSpeedBonus += ratioBonus;
	}

	public void AddPermanentCriticalChanceBonus(float chanceBonus)
	{
		permanentCritChanceBonus += chanceBonus;
	}

	public void AddManaRegenRateBonus(float rateBonus)
	{
		permanentManaRegenRateBonus += rateBonus;
	}

	public void AddMaxHealthBonus(float ratioBonus)
	{
		float maxHealth = MaxHealth;
		permanentMaxHealthBonus += ratioBonus;
		float num = Mathf.Max(0f, MaxHealth - maxHealth);
		currentHealth = Mathf.Min(MaxHealth, currentHealth + num);
		floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
	}

	public void AddSkillPowerBonus(float ratioBonus)
	{
		permanentSkillPowerBonus += ratioBonus;
	}

	public void AddSkillParameterGrowthBonus(float ratioBonus)
	{
		permanentSkillParameterGrowthBonus += Mathf.Max(0f, ratioBonus);
	}

	public void AddSkillParameterGrowthLevels(int levelCount)
	{
		if (levelCount > 0)
		{
			permanentSkillParameterGrowthBonus += ResolveSkillGrowthStepRatio() * (float)levelCount;
		}
	}

	public void ApplyOutgameGrowth(int growthLevel, float attackPowerRatioPerLevel, float maxHealthRatioPerLevel)
	{
		if (growthLevel > 0)
		{
			permanentAttackPowerBonus += Mathf.Max(0f, attackPowerRatioPerLevel) * (float)growthLevel;
			permanentMaxHealthBonus += Mathf.Max(0f, maxHealthRatioPerLevel) * (float)growthLevel;
			AddSkillParameterGrowthLevels(growthLevel);
		}
	}

	public void AddCriticalDamageBonus(float ratioBonus)
	{
		permanentCriticalDamageBonus += ratioBonus;
	}

	public void AddBossDamageBonus(float ratioBonus)
	{
		permanentBossDamageBonus += ratioBonus;
	}

	public void SetBoardTileBonuses(float attackPowerRatio, float attackSpeedRatio, float manaRegenRate, float maxHealthRatio, float skillPowerRatio, float bossDamageRatio, float attackRangeFlat, float damageReductionRatio, float lifeStealRatio, string statusLabel = null, Color? statusColor = null)
	{
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		float maxHealth = MaxHealth;
		tileAttackPowerBonus = attackPowerRatio;
		tileAttackSpeedBonus = attackSpeedRatio;
		tileManaRegenRateBonus = manaRegenRate;
		tileMaxHealthBonus = maxHealthRatio;
		tileSkillPowerBonus = skillPowerRatio;
		tileBossDamageBonus = bossDamageRatio;
		tileAttackRangeBonus = attackRangeFlat;
		tileDamageReductionBonus = damageReductionRatio;
		tileLifeStealRatio = Mathf.Max(0f, lifeStealRatio);
		float maxHealth2 = MaxHealth;
		if (definition != null)
		{
			if (maxHealth <= 0f)
			{
				currentHealth = maxHealth2;
			}
			else
			{
				currentHealth = Mathf.Clamp(currentHealth + maxHealth2 - maxHealth, 0f, maxHealth2);
			}
			floatingUi?.SetValues(currentHealth, maxHealth2, currentMana, MaxMana);
		}
		if (!string.IsNullOrWhiteSpace(statusLabel))
		{
			ShowInstantSupportFeedback(statusLabel, (Color)(((_003F?)statusColor) ?? BuffFeedbackColor), null, 0.8f);
		}
	}

	public void ClearBoardTileBonuses()
	{
		SetBoardTileBonuses(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
	}

	public void ActivateTimedCombatBoost(float attackPowerRatioBonus, float attackSpeedRatioBonus, float duration, GameObject effectPrefab = null, string statusLabel = null, Color? statusColor = null)
	{
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		temporaryAttackPowerBonus = Mathf.Max(temporaryAttackPowerBonus, attackPowerRatioBonus);
		temporaryAttackSpeedBonus = Mathf.Max(temporaryAttackSpeedBonus, attackSpeedRatioBonus);
		temporaryCombatBoostTimer = Mathf.Max(temporaryCombatBoostTimer, duration);
		hitFlashFeedback?.PlayHit(critical: false);
		if (duration > 0f)
		{
			string label = ((!string.IsNullOrWhiteSpace(statusLabel)) ? statusLabel : ((attackSpeedRatioBonus > 0f) ? ("공속 +" + Mathf.RoundToInt(attackSpeedRatioBonus * 100f) + "%") : "전투 강화"));
			Color color = (Color)(((_003F?)statusColor) ?? BuffFeedbackColor);
			ShowTimedSupportFeedback(label, color, duration, effectPrefab);
		}
	}

	public void ApplyAttackPowerReduction(float reductionRatio, float duration, GameObject effectPrefab = null)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Clamp01(reductionRatio);
		float num2 = Mathf.Max(0f, duration);
		if (!(num <= 0f) && !(num2 <= 0f))
		{
			temporaryAttackPowerReduction = Mathf.Max(temporaryAttackPowerReduction, num);
			temporaryAttackPowerReductionTimer = Mathf.Max(temporaryAttackPowerReductionTimer, num2);
			hitFlashFeedback?.PlayHit(critical: false);
			ShowTimedSupportFeedback("공격력 -" + Mathf.RoundToInt(num * 100f) + "%", DebuffFeedbackColor, num2, effectPrefab);
		}
	}

	public void ActivateTimedDamageReduction(float reductionRatio, float duration, GameObject effectPrefab = null, string statusLabel = null, Color? statusColor = null)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		ApplyTemporaryDamageReduction(reductionRatio, duration);
		if (duration > 0f)
		{
			ShowTimedSupportFeedback((!string.IsNullOrWhiteSpace(statusLabel)) ? statusLabel : "피해 감소", (Color)(((_003F?)statusColor) ?? ShieldFeedbackColor), duration, effectPrefab);
		}
	}

	public void SetSynergyBonuses(UnitSynergyBonus bonus)
	{
		float maxHealth = MaxHealth;
		synergyBonus = bonus;
		float maxHealth2 = MaxHealth;
		if (definition != null)
		{
			if (maxHealth <= 0f)
			{
				currentHealth = maxHealth2;
			}
			else
			{
				currentHealth = Mathf.Clamp(currentHealth + maxHealth2 - maxHealth, 0f, maxHealth2);
			}
			floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
		}
	}

	public void ResetFacingToDefault()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		if (hasDefaultFacing)
		{
			((Component)this).transform.rotation = defaultFacingRotation;
		}
	}

	private void PerformAttack(MonsterUnit target)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)target == (Object)null))
		{
			FaceTarget(((Component)target).transform.position);
			float num = Mathf.Max(0.2f, definition.stats.attackSpeed * (1f + attackSpeedBonus + permanentAttackSpeedBonus + synergyBonus.attackSpeedBonus + temporaryAttackSpeedBonus + tileAttackSpeedBonus));
			attackCooldown = 1f / num;
			bool critical = Random.value <= GetEffectiveCriticalChance();
			float damage = CalculateDamageAgainst(target, 1f, critical);
			QueueBasicAttackImpact(target, damage, critical);
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

	private void QueueBasicAttackImpact(MonsterUnit target, float damage, bool critical)
	{
		CancelPendingAttackFallback();
		pendingBasicAttack = new PendingBasicAttack
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
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		if (!this.pendingBasicAttack.isValid)
		{
			return;
		}
		PendingBasicAttack pendingBasicAttack = this.pendingBasicAttack;
		this.pendingBasicAttack = default(PendingBasicAttack);
		CancelPendingAttackFallback();
		if ((Object)(object)pendingBasicAttack.target == (Object)null || !pendingBasicAttack.target.CanBeCombatTargeted)
		{
			return;
		}
		FaceTarget(((Component)pendingBasicAttack.target).transform.position);
		GainManaFromBasicAttack();
		if (pendingBasicAttack.critical)
		{
			RuntimeCameraShake.Request(0.025f, 0.08f);
		}
		RuntimeAudioUtility.PlayAttack();
		if (definition.attackBehavior != null && definition.attackBehavior.IsMelee)
		{
			Quaternion rotation = RuntimeEffectUtility.FaceTowards(((Component)this).transform.position, ((Component)pendingBasicAttack.target).transform.position, ((Component)this).transform.rotation);
			RuntimeEffectUtility.PlayOneShot(ResolveBasicHitEffectPrefab(), ((Component)pendingBasicAttack.target).transform.position, rotation);
			RuntimeAudioUtility.PlayHit();
			pendingBasicAttack.target.TakeDamage(pendingBasicAttack.damage, pendingBasicAttack.critical, this);
			ApplyBasicAttackSplash(pendingBasicAttack.target, pendingBasicAttack.damage);
			return;
		}
		GameObject val = ResolveBasicAttackProjectilePrefab();
		if ((Object)(object)val == (Object)null)
		{
			Quaternion rotation2 = RuntimeEffectUtility.FaceTowards(((Component)this).transform.position, ((Component)pendingBasicAttack.target).transform.position, ((Component)this).transform.rotation);
			RuntimeEffectUtility.PlayOneShot(ResolveBasicHitEffectPrefab(), ((Component)pendingBasicAttack.target).transform.position, rotation2);
			RuntimeAudioUtility.PlayHit();
			pendingBasicAttack.target.TakeDamage(pendingBasicAttack.damage, pendingBasicAttack.critical, this);
			ApplyBasicAttackSplash(pendingBasicAttack.target, pendingBasicAttack.damage);
			return;
		}
		Transform val2 = (((Object)(object)firePoint != (Object)null) ? firePoint : ((Component)this).transform);
		Quaternion rotation3 = RuntimeEffectUtility.FaceTowards(val2.position, ((Component)pendingBasicAttack.target).transform.position, val2.rotation);
		RuntimeEffectUtility.PlayOneShot(ResolveBasicMuzzleEffectPrefab(), val2.position, rotation3);
		Projectile projectile = InstantiateProjectile(val, val2.position, rotation3);
		if ((Object)(object)projectile == (Object)null)
		{
			RuntimeEffectUtility.PlayOneShot(ResolveBasicHitEffectPrefab(), ((Component)pendingBasicAttack.target).transform.position, rotation3);
			RuntimeAudioUtility.PlayHit();
			pendingBasicAttack.target.TakeDamage(pendingBasicAttack.damage, pendingBasicAttack.critical, this);
			ApplyBasicAttackSplash(pendingBasicAttack.target, pendingBasicAttack.damage);
		}
		else
		{
			projectile.Initialize(pendingBasicAttack.target, pendingBasicAttack.damage, definition.stats.projectileSpeed, pendingBasicAttack.critical, GetBasicAttackSplashRadius(), GetBasicAttackSplashDamageRatio(), (definition.attackBehavior != null) ? definition.attackBehavior.additionalPierceCount : 0, this, null, ResolveBasicHitEffectPrefab());
		}
	}

	private GameObject ResolveBasicAttackProjectilePrefab()
	{
		GameObject val = ((definition != null && definition.attackBehavior != null) ? definition.attackBehavior.projectilePrefabOverride : null);
		if ((Object)(object)val != (Object)null)
		{
			return val;
		}
		return ((Object)(object)projectilePrefab != (Object)null) ? ((Component)projectilePrefab).gameObject : null;
	}

	private Projectile InstantiateProjectile(GameObject projectileSource, Vector3 position, Quaternion rotation)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		return Projectile.Spawn(projectileSource, position, rotation);
	}

	private GameObject ResolveBasicMuzzleEffectPrefab()
	{
		if (definition != null && definition.attackBehavior != null && (Object)(object)definition.attackBehavior.muzzleEffectPrefab != (Object)null)
		{
			return definition.attackBehavior.muzzleEffectPrefab;
		}
		return defaultMuzzleEffectPrefab;
	}

	private GameObject ResolveBasicHitEffectPrefab()
	{
		if (definition != null && definition.attackBehavior != null && (Object)(object)definition.attackBehavior.hitEffectPrefab != (Object)null)
		{
			return definition.attackBehavior.hitEffectPrefab;
		}
		return defaultHitEffectPrefab;
	}

	private void GainManaFromBasicAttack()
	{
		float num = Mathf.Clamp01(definition.stats.manaGainPerAttackRate + synergyBonus.manaGainPerAttackRateBonus);
		if (!(MaxMana <= 0f) && !(num <= 0f))
		{
			currentMana = Mathf.Min(MaxMana, currentMana + MaxMana * num);
			floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
		}
	}

	private void QueueSkillImpact(SkillDefinition skill, MonsterUnit target, float skillMultiplier)
	{
		CancelPendingSkillFallback();
		pendingSkillCast = new PendingSkillCast
		{
			isValid = true,
			sequence = ++impactSequence,
			skill = skill,
			target = target,
			skillMultiplier = skillMultiplier
		};
	}

	private void ResolvePendingSkillCast()
	{
		if (this.pendingSkillCast.isValid)
		{
			PendingSkillCast pendingSkillCast = this.pendingSkillCast;
			this.pendingSkillCast = default(PendingSkillCast);
			CancelPendingSkillFallback();
			if (pendingSkillCast.skill != null && pendingSkillCast.skill.effectType == SkillEffectType.HealLowestAllies && FindLowestHealthAllies(GetSkillHitCount(pendingSkillCast.skill)).Count == 0)
			{
				currentMana = MaxMana;
				skillCooldowns[pendingSkillCast.skill.id] = 0f;
				floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
				animationDriver?.ForceIdle();
			}
			else
			{
				ApplySkillEffect(pendingSkillCast.skill, pendingSkillCast.target, pendingSkillCast.skillMultiplier);
			}
		}
	}

	private bool TryCastSkill()
	{
		if (!CanStartActionAnimation())
		{
			return false;
		}
		if (!IsCombatActive())
		{
			return false;
		}
		if (definition.skills == null || definition.skills.Count == 0)
		{
			return false;
		}
		if (MaxMana <= 0f || currentMana < MaxMana)
		{
			return false;
		}
		for (int i = 0; i < definition.skills.Count; i++)
		{
			SkillDefinition skillDefinition = definition.skills[i];
			MonsterUnit currentTarget = FindNearestSkillTarget(skillDefinition);
			if (CanCastSkill(skillDefinition, currentTarget) && (!skillCooldowns.TryGetValue(skillDefinition.id, out var value) || !(value > 0f)))
			{
				currentMana = 0f;
				CastSkill(skillDefinition, currentTarget);
				skillCooldowns[skillDefinition.id] = skillDefinition.cooldown;
				return true;
			}
		}
		return false;
	}

	private bool CanCastSkill(SkillDefinition skill, MonsterUnit currentTarget)
	{
		if (skill == null)
		{
			return false;
		}
		if (RequiresNearbyMonsterForActivation(skill.effectType) && !HasMonsterInSkillCastRange(skill))
		{
			return false;
		}
		switch (skill.effectType)
		{
		case SkillEffectType.DirectDamage:
		case SkillEffectType.Execute:
		case SkillEffectType.ShieldBreak:
		case SkillEffectType.Slow:
		case SkillEffectType.Stun:
		case SkillEffectType.LifeSteal:
		case SkillEffectType.Poison:
		case SkillEffectType.HealthDrainPercent:
		case SkillEffectType.LinePierceDamage:
		case SkillEffectType.DamageStun:
		case SkillEffectType.PercentHealthDamage:
		case SkillEffectType.DamageSlow:
		case SkillEffectType.StoneLine:
		case SkillEffectType.DamageGroundField:
		case SkillEffectType.FixedPoison:
		case SkillEffectType.FrontKnockbackGuard:
		case SkillEffectType.RandomMultiShot:
			return (Object)(object)currentTarget != (Object)null;
		case SkillEffectType.ShieldAlly:
			return (Object)(object)FindLowestHealthAlly() != (Object)null;
		case SkillEffectType.AttackSpeedBoost:
		case SkillEffectType.CriticalBoost:
		case SkillEffectType.ManaSurge:
		case SkillEffectType.DefenseBuff:
		case SkillEffectType.ThornsAura:
		case SkillEffectType.AllyAttackSpeedBoost:
			return true;
		case SkillEffectType.DeathPoisonField:
			return false;
		case SkillEffectType.ManaRestoreAdjacent:
			return FindAdjacentManaTargets(GetSkillRadius(skill)).Count > 0;
		case SkillEffectType.HealLowestAllies:
			return FindLowestHealthAllies(GetSkillHitCount(skill)).Count > 0;
		case SkillEffectType.Taunt:
			return HasMonsterInRadius(GetSkillRadius(skill));
		case SkillEffectType.AreaDamage:
			return (Object)(object)currentTarget != (Object)null;
		case SkillEffectType.GroundAreaDamage:
			return (Object)(object)currentTarget != (Object)null;
		case SkillEffectType.HealSelf:
			return currentHealth < MaxHealth * 0.98f;
		case SkillEffectType.Transform:
			return true;
		default:
			return (Object)(object)currentTarget != (Object)null;
		}
	}

	private bool HasMonsterInSkillCastRange(SkillDefinition skill)
	{
		if (skill == null)
		{
			return false;
		}
		return (Object)(object)FindNearestTarget(GetEffectiveSkillRange(skill)) != (Object)null;
	}

	private static bool RequiresNearbyMonsterForActivation(SkillEffectType effectType)
	{
		switch (effectType)
		{
		case SkillEffectType.AttackSpeedBoost:
		case SkillEffectType.CriticalBoost:
		case SkillEffectType.ShieldAlly:
		case SkillEffectType.DefenseBuff:
		case SkillEffectType.Transform:
		case SkillEffectType.ThornsAura:
		case SkillEffectType.AllyAttackSpeedBoost:
			return true;
		default:
			return false;
		}
	}

	private void CastSkill(SkillDefinition skill, MonsterUnit currentTarget)
	{
		float skillMultiplier = Mathf.Max(0.1f, 1f + permanentSkillPowerBonus + synergyBonus.skillPowerBonus + tileSkillPowerBonus);
		QueueSkillImpact(skill, currentTarget, skillMultiplier);
		int skillSlot = ((definition == null || definition.skills == null) ? 1 : Mathf.Max(1, definition.skills.IndexOf(skill) + 1));
		float skillDuration = Mathf.Max(0f, GetSkillDuration(skill));
		if ((Object)(object)animationDriver != (Object)null && animationDriver.PlaySkill(skillSlot, skillDuration))
		{
			SchedulePendingSkillFallback(pendingSkillCast.sequence);
		}
		else
		{
			ResolvePendingSkillCast();
		}
	}

	private void ApplySkillEffect(SkillDefinition skill, MonsterUnit currentTarget, float skillMultiplier)
	{
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_044b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0394: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0774: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_085c: Unknown result type (might be due to invalid IL or missing references)
		//IL_095f: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a59: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b0d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ba6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b98: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d21: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c30: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bab: Unknown result type (might be due to invalid IL or missing references)
		//IL_0baf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bbb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d65: Unknown result type (might be due to invalid IL or missing references)
		if (skill == null)
		{
			return;
		}
		currentTarget = ResolveCombatTarget(currentTarget, skill);
		DefenderUnit.OnSkillCast?.Invoke(this, skill, currentTarget);
		SkillDefinition currentDamageSkillContext = CurrentDamageSkillContext;
		CurrentDamageSkillContext = skill;
		try
		{
			SkillDeliveryType skillDeliveryType = SkillDefinitionUtility.ResolveDeliveryType(skill);
			if (skillDeliveryType == SkillDeliveryType.Projectile && TryLaunchSkillProjectile(skill, currentTarget, skillMultiplier))
			{
				return;
			}
			float skillPower = GetSkillPower(skill);
			float skillSecondaryPower = GetSkillSecondaryPower(skill);
			float skillDuration = GetSkillDuration(skill);
			float skillRadius = GetSkillRadius(skill);
			int skillHitCount = GetSkillHitCount(skill);
			if (skill.effectType == SkillEffectType.DirectDamage)
			{
				MonsterUnit monsterUnit = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
				if ((Object)(object)monsterUnit != (Object)null)
				{
					FaceTarget(((Component)monsterUnit).transform.position);
					PlaySkillHitEffect(skill, monsterUnit);
					monsterUnit.TakeDamage(CalculateDamageAgainst(monsterUnit, skillPower * skillMultiplier, critical: false), critical: false, this);
				}
			}
			else if (skill.effectType == SkillEffectType.FrontKnockbackGuard)
			{
				MonsterUnit monsterUnit2 = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
				if ((Object)(object)monsterUnit2 != (Object)null)
				{
					FaceTarget(((Component)monsterUnit2).transform.position);
					PlaySkillHitEffect(skill, monsterUnit2);
					monsterUnit2.TakeDamage(CalculateDamageAgainst(monsterUnit2, skillPower * skillMultiplier, critical: false), critical: false, this);
					monsterUnit2.ApplyKnockback(skillRadius, ((Component)this).transform.position);
				}
				permanentDamageReductionBonus = Mathf.Min(0.4f, permanentDamageReductionBonus + Mathf.Max(0f, skillSecondaryPower));
				ShowInstantSupportFeedback("방어력 +" + Mathf.RoundToInt(skillSecondaryPower * 100f) + "% / 누적 " + Mathf.RoundToInt(permanentDamageReductionBonus * 100f) + "%", ShieldFeedbackColor, skill.areaEffectPrefab, 0.9f);
			}
			else if (skill.effectType == SkillEffectType.AreaDamage)
			{
				Vector3 center = (((Object)(object)currentTarget != (Object)null) ? ((Component)currentTarget).transform.position : ((Component)this).transform.position);
				PlaySkillAreaEffect(skill, center);
				ApplyAreaDamageAt(center, skillRadius, skillPower * skillMultiplier, critical: false, skill);
			}
			else if (skill.effectType == SkillEffectType.HealthDrainPercent)
			{
				MonsterUnit monsterUnit3 = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
				if ((Object)(object)monsterUnit3 != (Object)null)
				{
					FaceTarget(((Component)monsterUnit3).transform.position);
					PlaySkillHitEffect(skill, monsterUnit3);
					float num = Mathf.Max(0f, monsterUnit3.CurrentHealth * skillPower * skillMultiplier);
					monsterUnit3.TakeDamage(num, critical: false, this);
					Heal(num, skill.areaEffectPrefab);
				}
			}
			else if (skill.effectType == SkillEffectType.LinePierceDamage)
			{
				ApplyLinePierceDamage(skill, currentTarget, skillMultiplier);
			}
			else if (skill.effectType == SkillEffectType.StoneLine)
			{
				ApplyStoneLine(skill, currentTarget);
			}
			else if (skill.effectType == SkillEffectType.DamageGroundField)
			{
				ApplyDamageGroundField(skill, currentTarget, skillMultiplier);
			}
			else if (skill.effectType == SkillEffectType.FixedPoison)
			{
				MonsterUnit monsterUnit4 = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
				if ((Object)(object)monsterUnit4 != (Object)null)
				{
					FaceTarget(((Component)monsterUnit4).transform.position);
					PlaySkillHitEffect(skill, monsterUnit4);
					monsterUnit4.ApplyPoison(skillPower * skillMultiplier, skillDuration, Mathf.Max(0.2f, skillSecondaryPower), this);
				}
			}
			else if (skill.effectType == SkillEffectType.HealSelf)
			{
				PlaySkillCasterEffect(skill);
				Heal(MaxHealth * skillPower * skillMultiplier, skill.areaEffectPrefab);
			}
			else if (skill.effectType == SkillEffectType.AttackSpeedBoost)
			{
				PlaySkillCasterEffect(skill);
				attackSpeedBonus = skillPower;
				attackSpeedBuffTimer = skillDuration;
				ShowTimedSupportFeedback("공속 +" + Mathf.RoundToInt(skillPower * 100f) + "%", AttackSpeedFeedbackColor, skillDuration, skill.areaEffectPrefab);
			}
			else if (skill.effectType == SkillEffectType.AllyAttackSpeedBoost)
			{
				PlaySkillCasterEffect(skill);
				ApplyAllyAttackSpeedBoost(skillPower * skillMultiplier, skillDuration, skillRadius, skill.areaEffectPrefab);
			}
			else if (skill.effectType == SkillEffectType.CriticalBoost)
			{
				PlaySkillCasterEffect(skill);
				critChanceBonus = skillPower;
				critBuffTimer = skillDuration;
				ShowTimedSupportFeedback("치명 +" + Mathf.RoundToInt(skillPower * 100f) + "%", BuffFeedbackColor, skillDuration, skill.areaEffectPrefab);
			}
			else if (skill.effectType == SkillEffectType.ManaSurge)
			{
				PlaySkillCasterEffect(skill);
				RestoreMana(skillPower * skillMultiplier, skill.areaEffectPrefab);
			}
			else if (skill.effectType == SkillEffectType.ManaRestoreAdjacent)
			{
				PlaySkillCasterEffect(skill);
				List<DefenderUnit> list = FindAdjacentManaTargets(skillRadius);
				for (int i = 0; i < list.Count; i++)
				{
					list[i].RestoreMana(skillPower * skillMultiplier, skill.areaEffectPrefab);
				}
			}
			else if (skill.effectType == SkillEffectType.MultiShot)
			{
				List<MonsterUnit> nearestTargets = GetNearestTargets(skillHitCount, GetEffectiveSkillRange(skill));
				for (int j = 0; j < nearestTargets.Count; j++)
				{
					PlaySkillHitEffect(skill, nearestTargets[j]);
					nearestTargets[j].TakeDamage(CalculateDamageAgainst(nearestTargets[j], skillPower * skillMultiplier, critical: false), critical: false, this);
				}
			}
			else if (skill.effectType == SkillEffectType.RandomMultiShot)
			{
				List<MonsterUnit> randomTargetsWithReplacement = GetRandomTargetsWithReplacement(skillHitCount, GetEffectiveSkillRange(skill));
				for (int k = 0; k < randomTargetsWithReplacement.Count; k++)
				{
					PlaySkillHitEffect(skill, randomTargetsWithReplacement[k]);
					randomTargetsWithReplacement[k].TakeDamage(CalculateDamageAgainst(randomTargetsWithReplacement[k], skillPower * skillMultiplier, critical: false), critical: false, this);
				}
			}
			else if (skill.effectType == SkillEffectType.Execute)
			{
				MonsterUnit monsterUnit5 = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
				if ((Object)(object)monsterUnit5 != (Object)null)
				{
					FaceTarget(((Component)monsterUnit5).transform.position);
					PlaySkillHitEffect(skill, monsterUnit5);
					float num2 = ((monsterUnit5.CurrentHealth <= monsterUnit5.MaxHealth * 0.35f) ? (skillPower * 1.8f) : skillPower);
					num2 *= skillMultiplier;
					monsterUnit5.TakeDamage(CalculateDamageAgainst(monsterUnit5, num2, critical: true), critical: true, this);
				}
			}
			else if (skill.effectType == SkillEffectType.SummonRush)
			{
				SpawnSummonedAllies(skill, currentTarget, skillMultiplier);
			}
			else if (skill.effectType == SkillEffectType.ShieldBreak)
			{
				MonsterUnit monsterUnit6 = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
				if ((Object)(object)monsterUnit6 != (Object)null)
				{
					FaceTarget(((Component)monsterUnit6).transform.position);
					PlaySkillHitEffect(skill, monsterUnit6);
					monsterUnit6.TakeDamage(CalculateDamageAgainst(monsterUnit6, skillPower * skillMultiplier, critical: false), critical: false, this);
				}
			}
			else if (skill.effectType == SkillEffectType.DamageStun)
			{
				MonsterUnit monsterUnit7 = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
				if ((Object)(object)monsterUnit7 != (Object)null)
				{
					FaceTarget(((Component)monsterUnit7).transform.position);
					PlaySkillHitEffect(skill, monsterUnit7);
					monsterUnit7.TakeDamage(CalculateDamageAgainst(monsterUnit7, skillPower * skillMultiplier, critical: false), critical: false, this);
					monsterUnit7.ApplyStun(skillDuration);
				}
			}
			else if (skill.effectType == SkillEffectType.PercentHealthDamage)
			{
				MonsterUnit monsterUnit8 = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
				if ((Object)(object)monsterUnit8 != (Object)null)
				{
					FaceTarget(((Component)monsterUnit8).transform.position);
					PlaySkillHitEffect(skill, monsterUnit8);
					float damage = Mathf.Max(0f, monsterUnit8.CurrentHealth * skillPower * skillMultiplier);
					monsterUnit8.TakeDamage(damage, critical: false, this);
				}
			}
			else if (skill.effectType == SkillEffectType.HealLowestAllies)
			{
				List<DefenderUnit> list2 = FindLowestHealthAllies(skillHitCount);
				if (list2.Count != 0)
				{
					PlaySkillCasterEffect(skill);
					for (int l = 0; l < list2.Count; l++)
					{
						list2[l].Heal(list2[l].MaxHealth * skillPower * skillMultiplier, skill.areaEffectPrefab);
					}
				}
			}
			else if (skill.effectType == SkillEffectType.DamageSlow)
			{
				MonsterUnit monsterUnit9 = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
				if ((Object)(object)monsterUnit9 != (Object)null)
				{
					FaceTarget(((Component)monsterUnit9).transform.position);
					PlaySkillHitEffect(skill, monsterUnit9);
					monsterUnit9.TakeDamage(CalculateDamageAgainst(monsterUnit9, skillPower * skillMultiplier, critical: false), critical: false, this);
					monsterUnit9.ApplySlow(Mathf.Clamp01(skillSecondaryPower), skillDuration);
					monsterUnit9.ApplyAttackSpeedSlow(Mathf.Clamp01(skillSecondaryPower), skillDuration);
				}
			}
			else if (skill.effectType == SkillEffectType.Slow)
			{
				MonsterUnit monsterUnit10 = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
				if ((Object)(object)monsterUnit10 != (Object)null)
				{
					FaceTarget(((Component)monsterUnit10).transform.position);
					PlaySkillHitEffect(skill, monsterUnit10);
					monsterUnit10.ApplySlow(Mathf.Clamp01(skillPower), skillDuration);
				}
			}
			else if (skill.effectType == SkillEffectType.Stun)
			{
				MonsterUnit monsterUnit11 = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
				if ((Object)(object)monsterUnit11 != (Object)null)
				{
					FaceTarget(((Component)monsterUnit11).transform.position);
					PlaySkillHitEffect(skill, monsterUnit11);
					monsterUnit11.ApplyStun(skillDuration);
				}
			}
			else if (skill.effectType == SkillEffectType.ShieldAlly)
			{
				DefenderUnit defenderUnit = FindLowestHealthAlly();
				if ((Object)(object)defenderUnit != (Object)null)
				{
					PlaySupportSkillCastFeedback();
					defenderUnit.AddShield(defenderUnit.MaxHealth * skillPower * skillMultiplier, skillDuration, skill.areaEffectPrefab);
				}
			}
			else if (skill.effectType == SkillEffectType.LifeSteal)
			{
				MonsterUnit monsterUnit12 = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
				if ((Object)(object)monsterUnit12 != (Object)null)
				{
					FaceTarget(((Component)monsterUnit12).transform.position);
					PlaySkillHitEffect(skill, monsterUnit12);
					float num3 = CalculateDamageAgainst(monsterUnit12, skillPower * skillMultiplier, critical: false);
					monsterUnit12.TakeDamage(num3, critical: false, this);
					Heal(num3 * Mathf.Max(0.05f, skillSecondaryPower), skill.areaEffectPrefab);
				}
			}
			else if (skill.effectType == SkillEffectType.GroundAreaDamage)
			{
				MonsterUnit monsterUnit13 = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
				Vector3 center2 = (((Object)(object)monsterUnit13 != (Object)null) ? ((Component)monsterUnit13).transform.position : ((Component)this).transform.position);
				PlaySkillAreaEffect(skill, center2, skillDuration);
				((MonoBehaviour)this).StartCoroutine(GroundDamageRoutine(center2, skillRadius, definition.stats.attackPower * skillPower * skillMultiplier, skillDuration, Mathf.Max(0.2f, skillSecondaryPower), skill));
			}
			else if (skill.effectType == SkillEffectType.Poison)
			{
				MonsterUnit monsterUnit14 = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
				if ((Object)(object)monsterUnit14 != (Object)null)
				{
					FaceTarget(((Component)monsterUnit14).transform.position);
					PlaySkillHitEffect(skill, monsterUnit14);
					monsterUnit14.ApplyPoison(definition.stats.attackPower * skillPower * skillMultiplier, skillDuration, Mathf.Max(0.25f, skillSecondaryPower), this);
				}
			}
			else if (skill.effectType == SkillEffectType.DefenseBuff)
			{
				PlaySupportSkillCastFeedback();
				ApplyDefenseBuff(skillPower * skillMultiplier, skillDuration, skillRadius, skill.areaEffectPrefab);
			}
			else if (skill.effectType == SkillEffectType.Taunt)
			{
				float duration = Mathf.Max(0.1f, skillDuration * skillMultiplier);
				PlayAttachedSupportEffect(ResolveSkillAreaEffectPrefab(skill), duration);
				ApplyTaunt(skillRadius, duration);
				ApplyTemporaryDamageReduction(skillSecondaryPower, duration);
			}
			else if (skill.effectType == SkillEffectType.ThornsAura)
			{
				PlaySkillCasterEffect(skill);
				ApplyThornsAura(skillPower * skillMultiplier, skillDuration);
				ShowTimedSupportFeedback("쏜즈", BuffFeedbackColor, skillDuration, skill.areaEffectPrefab);
			}
			else if (skill.effectType == SkillEffectType.Transform)
			{
				PlaySkillCasterEffect(skill);
				ActivateTimedCombatBoost(skillPower * skillMultiplier, skillSecondaryPower * skillMultiplier, skillDuration, skill.areaEffectPrefab, "전투 강화", BuffFeedbackColor);
				AddShield(MaxHealth * skillSecondaryPower * 0.5f * skillMultiplier, skillDuration, skill.areaEffectPrefab);
			}
		}
		finally
		{
			CurrentDamageSkillContext = currentDamageSkillContext;
		}
	}

	private bool TryLaunchSkillProjectile(SkillDefinition skill, MonsterUnit currentTarget, float skillMultiplier)
	{
		GameObject val = ResolveSkillProjectilePrefab(skill);
		if ((Object)(object)val == (Object)null || !CanDeliverSkillByProjectile(skill))
		{
			return false;
		}
		List<MonsterUnit> list = ResolveSkillProjectileTargets(skill, currentTarget);
		if (list.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < list.Count; i++)
		{
			MonsterUnit monsterUnit = list[i];
			if (!((Object)(object)monsterUnit == (Object)null))
			{
				LaunchSkillProjectile(monsterUnit, skill, skillMultiplier, val);
			}
		}
		return true;
	}

	private bool CanDeliverSkillByProjectile(SkillDefinition skill)
	{
		if (skill == null)
		{
			return false;
		}
		switch (skill.effectType)
		{
		case SkillEffectType.DirectDamage:
		case SkillEffectType.AreaDamage:
		case SkillEffectType.MultiShot:
		case SkillEffectType.Execute:
		case SkillEffectType.ShieldBreak:
		case SkillEffectType.Slow:
		case SkillEffectType.Stun:
		case SkillEffectType.LifeSteal:
		case SkillEffectType.GroundAreaDamage:
		case SkillEffectType.Poison:
		case SkillEffectType.HealthDrainPercent:
		case SkillEffectType.DamageStun:
		case SkillEffectType.PercentHealthDamage:
		case SkillEffectType.DamageSlow:
		case SkillEffectType.DamageGroundField:
		case SkillEffectType.FixedPoison:
		case SkillEffectType.RandomMultiShot:
			return true;
		default:
			return false;
		}
	}

	private List<MonsterUnit> ResolveSkillProjectileTargets(SkillDefinition skill, MonsterUnit currentTarget)
	{
		int num = ((skill == null || (skill.effectType != SkillEffectType.MultiShot && skill.effectType != SkillEffectType.RandomMultiShot && skill.effectType != SkillEffectType.SummonRush)) ? 1 : GetSkillHitCount(skill));
		if (num > 1)
		{
			if (skill.effectType == SkillEffectType.RandomMultiShot)
			{
				return GetRandomTargetsWithReplacement(num, GetEffectiveSkillRange(skill));
			}
			return GetNearestTargets(num, GetEffectiveSkillRange(skill));
		}
		List<MonsterUnit> list = new List<MonsterUnit>(1);
		MonsterUnit monsterUnit = ResolveCombatTarget(currentTarget, skill);
		if ((Object)(object)monsterUnit != (Object)null)
		{
			list.Add(monsterUnit);
		}
		return list;
	}

	private void LaunchSkillProjectile(MonsterUnit target, SkillDefinition skill, float skillMultiplier, GameObject skillProjectilePrefab)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		Transform val = (((Object)(object)firePoint != (Object)null) ? firePoint : ((Component)this).transform);
		Quaternion rotation = RuntimeEffectUtility.FaceTowards(val.position, ((Component)target).transform.position, val.rotation);
		RuntimeEffectUtility.PlayOneShot(ResolveSkillMuzzleEffectPrefab(skill), val.position, rotation);
		Projectile projectile = InstantiateProjectile(skillProjectilePrefab, val.position, rotation);
		if (!((Object)(object)projectile == (Object)null))
		{
			projectile.Initialize(target, 0f, Mathf.Max(0.1f, definition.stats.projectileSpeed), isCritical: false, 0f, 0f, 0, this, delegate(MonsterUnit hitTarget)
			{
				ApplyProjectileSkillImpact(skill, hitTarget, skillMultiplier);
			}, ResolveSkillHitEffectPrefab(skill));
		}
	}

	private GameObject ResolveSkillProjectilePrefab(SkillDefinition skill)
	{
		if (skill != null && (Object)(object)skill.projectilePrefab != (Object)null)
		{
			return skill.projectilePrefab;
		}
		return ((Object)(object)projectilePrefab != (Object)null) ? ((Component)projectilePrefab).gameObject : null;
	}

	private GameObject ResolveSkillMuzzleEffectPrefab(SkillDefinition skill)
	{
		if (skill != null && (Object)(object)skill.muzzleEffectPrefab != (Object)null)
		{
			return skill.muzzleEffectPrefab;
		}
		return defaultMuzzleEffectPrefab;
	}

	private GameObject ResolveSkillHitEffectPrefab(SkillDefinition skill)
	{
		if (skill != null && (Object)(object)skill.hitEffectPrefab != (Object)null)
		{
			return skill.hitEffectPrefab;
		}
		return defaultHitEffectPrefab;
	}

	private GameObject ResolveSkillAreaEffectPrefab(SkillDefinition skill)
	{
		if (skill != null && (Object)(object)skill.areaEffectPrefab != (Object)null)
		{
			return skill.areaEffectPrefab;
		}
		return defaultAreaEffectPrefab;
	}

	private void PlaySkillHitEffect(SkillDefinition skill, MonsterUnit target)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)target == (Object)null))
		{
			Quaternion rotation = RuntimeEffectUtility.FaceTowards(((Component)this).transform.position, ((Component)target).transform.position, ((Component)this).transform.rotation);
			RuntimeEffectUtility.PlayOneShot(ResolveSkillHitEffectPrefab(skill), ((Component)target).transform.position, rotation);
			RuntimeCombatFeedback.ShowGroundPulse(((Component)target).transform.position, (definition != null) ? definition.accentColor : Color.white, 0.36f, 0.34f);
			RuntimeAudioUtility.PlayHit();
		}
	}

	private void PlaySkillCasterEffect(SkillDefinition skill)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		MonsterUnit monsterUnit = FindNearestSkillTarget(skill);
		Quaternion rotation = (((Object)(object)monsterUnit != (Object)null) ? RuntimeEffectUtility.FaceTowards(((Component)this).transform.position, ((Component)monsterUnit).transform.position, ((Component)this).transform.rotation) : ((Component)this).transform.rotation);
		RuntimeEffectUtility.PlayOneShot(ResolveSkillMuzzleEffectPrefab(skill), ((Component)this).transform.position, rotation);
		RuntimeCombatFeedback.ShowGroundPulse(((Component)this).transform.position, (definition != null) ? definition.accentColor : Color.white, 0.44f, 0.32f, 0.06f);
		RuntimeAudioUtility.PlayAttack();
	}

	private void PlaySupportSkillCastFeedback()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		RuntimeCombatFeedback.ShowGroundPulse(((Component)this).transform.position, (definition != null) ? definition.accentColor : ShieldFeedbackColor, 0.44f, 0.32f, 0.06f);
		RuntimeAudioUtility.PlayAttack();
	}

	private void PlaySkillAreaEffect(SkillDefinition skill, Vector3 center, float minimumLifetime = 0f)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		Quaternion rotation = RuntimeEffectUtility.FaceTowards(((Component)this).transform.position, center, ((Component)this).transform.rotation);
		RuntimeEffectUtility.PlayOneShot(ResolveSkillAreaEffectPrefab(skill), center, rotation, minimumLifetime);
	}

	private void ApplyProjectileSkillImpact(SkillDefinition skill, MonsterUnit hitTarget, float skillMultiplier)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)this == (Object)null || !((Behaviour)this).isActiveAndEnabled || definition == null || skill == null || (Object)(object)hitTarget == (Object)null || !hitTarget.CanBeCombatTargeted)
		{
			return;
		}
		SkillDefinition currentDamageSkillContext = CurrentDamageSkillContext;
		CurrentDamageSkillContext = skill;
		try
		{
			float skillPower = GetSkillPower(skill);
			float skillSecondaryPower = GetSkillSecondaryPower(skill);
			float skillDuration = GetSkillDuration(skill);
			float skillRadius = GetSkillRadius(skill);
			FaceTarget(((Component)hitTarget).transform.position);
			if (skill.effectType == SkillEffectType.DirectDamage || skill.effectType == SkillEffectType.ShieldBreak || skill.effectType == SkillEffectType.MultiShot || skill.effectType == SkillEffectType.RandomMultiShot)
			{
				hitTarget.TakeDamage(CalculateDamageAgainst(hitTarget, skillPower * skillMultiplier, critical: false), critical: false, this);
			}
			else if (skill.effectType == SkillEffectType.AreaDamage)
			{
				PlaySkillAreaEffect(skill, ((Component)hitTarget).transform.position);
				ApplyAreaDamageAt(((Component)hitTarget).transform.position, skillRadius, skillPower * skillMultiplier, critical: false, skill);
			}
			else if (skill.effectType == SkillEffectType.Execute)
			{
				float num = ((hitTarget.CurrentHealth <= hitTarget.MaxHealth * 0.35f) ? (skillPower * 1.8f) : skillPower);
				hitTarget.TakeDamage(CalculateDamageAgainst(hitTarget, num * skillMultiplier, critical: true), critical: true, this);
			}
			else if (skill.effectType == SkillEffectType.Slow)
			{
				hitTarget.ApplySlow(Mathf.Clamp01(skillPower), skillDuration);
			}
			else if (skill.effectType == SkillEffectType.Stun)
			{
				hitTarget.ApplyStun(skillDuration);
			}
			else if (skill.effectType == SkillEffectType.LifeSteal)
			{
				float num2 = CalculateDamageAgainst(hitTarget, skillPower * skillMultiplier, critical: false);
				hitTarget.TakeDamage(num2, critical: false, this);
				Heal(num2 * Mathf.Max(0.05f, skillSecondaryPower), skill.areaEffectPrefab);
			}
			else if (skill.effectType == SkillEffectType.GroundAreaDamage)
			{
				PlaySkillAreaEffect(skill, ((Component)hitTarget).transform.position, skillDuration);
				((MonoBehaviour)this).StartCoroutine(GroundDamageRoutine(((Component)hitTarget).transform.position, skillRadius, definition.stats.attackPower * skillPower * skillMultiplier, skillDuration, Mathf.Max(0.2f, skillSecondaryPower), skill));
			}
			else if (skill.effectType == SkillEffectType.Poison)
			{
				hitTarget.ApplyPoison(definition.stats.attackPower * skillPower * skillMultiplier, skillDuration, Mathf.Max(0.25f, skillSecondaryPower), this);
			}
			else if (skill.effectType == SkillEffectType.DamageGroundField)
			{
				hitTarget.TakeDamage(CalculateDamageAgainst(hitTarget, skillPower * skillMultiplier, critical: false), critical: false, this);
				PlaySkillAreaEffect(skill, ((Component)hitTarget).transform.position, skillDuration);
				SpawnAreaDamageZone(((Component)hitTarget).transform.position, skillRadius, skillSecondaryPower * skillMultiplier, skillDuration, 1f, skill);
			}
			else if (skill.effectType == SkillEffectType.FixedPoison)
			{
				hitTarget.ApplyPoison(skillPower * skillMultiplier, skillDuration, Mathf.Max(0.2f, skillSecondaryPower), this);
			}
		}
		finally
		{
			CurrentDamageSkillContext = currentDamageSkillContext;
		}
	}

	private void ApplyAreaDamageAt(Vector3 center, float radius, float multiplier, bool critical, SkillDefinition skill = null)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		float num = Mathf.Max(0.1f, radius);
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if ((Object)(object)monsterUnit != (Object)null && monsterUnit.CanBeCombatTargeted && Vector3.Distance(center, ((Component)monsterUnit).transform.position) <= num)
			{
				if (skill != null)
				{
					PlaySkillHitEffect(skill, monsterUnit);
				}
				monsterUnit.TakeDamage(CalculateDamageAgainst(monsterUnit, multiplier, critical), critical, this);
			}
		}
	}

	private void ApplyLinePierceDamage(SkillDefinition skill, MonsterUnit currentTarget, float skillMultiplier)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		MonsterUnit monsterUnit = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
		if ((Object)(object)monsterUnit == (Object)null)
		{
			return;
		}
		Vector3 val = ((Component)monsterUnit).transform.position - ((Component)this).transform.position;
		val.y = 0f;
		if (((Vector3)(ref val)).sqrMagnitude <= 0.0001f)
		{
			val = ((Component)this).transform.forward;
		}
		((Vector3)(ref val)).Normalize();
		FaceTarget(((Component)this).transform.position + val);
		float num = Mathf.Max(0.5f, GetSkillRadius(skill));
		float num2 = Mathf.Max(0.15f, GetSkillSecondaryPower(skill));
		float skillPower = GetSkillPower(skill);
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit2 = activeInstances[i];
			if ((Object)(object)monsterUnit2 == (Object)null || !monsterUnit2.CanBeCombatTargeted)
			{
				continue;
			}
			Vector3 val2 = ((Component)monsterUnit2).transform.position - ((Component)this).transform.position;
			val2.y = 0f;
			float num3 = Vector3.Dot(val2, val);
			if (!(num3 < 0f) && !(num3 > num))
			{
				Vector3 val3 = val * num3;
				Vector3 val4 = val2 - val3;
				float magnitude = ((Vector3)(ref val4)).magnitude;
				if (!(magnitude > num2))
				{
					PlaySkillHitEffect(skill, monsterUnit2);
					monsterUnit2.TakeDamage(CalculateDamageAgainst(monsterUnit2, skillPower * skillMultiplier, critical: false), critical: false, this);
				}
			}
		}
	}

	private void ApplyStoneLine(SkillDefinition skill, MonsterUnit currentTarget)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		MonsterUnit monsterUnit = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
		if (!((Object)(object)monsterUnit == (Object)null))
		{
			Vector3 val = ((Component)monsterUnit).transform.position - ((Component)this).transform.position;
			float magnitude = ((Vector3)(ref val)).magnitude;
			val.y = 0f;
			if (((Vector3)(ref val)).sqrMagnitude <= 0.0001f)
			{
				val = ((Component)this).transform.forward;
			}
			((Vector3)(ref val)).Normalize();
			FaceTarget(((Component)this).transform.position + val);
			float length = Mathf.Max(Mathf.Max(0.5f, GetSkillRadius(skill)), magnitude + 0.05f);
			float halfWidth = Mathf.Max(0.15f, GetSkillSecondaryPower(skill));
			float skillDuration = GetSkillDuration(skill);
			int maxTargets = Mathf.Max(1, GetSkillHitCount(skill));
			MonsterUnit.PetrifyTargetOptions options = new MonsterUnit.PetrifyTargetOptions
			{
				duration = skillDuration,
				maxTargets = maxTargets,
				onApplied = delegate(MonsterUnit target)
				{
					PlaySkillHitEffect(skill, target);
				}
			};
			MonsterUnit.ApplyPetrifyLine(((Component)this).transform.position, val, length, halfWidth, options);
		}
	}

	private void ApplyDamageGroundField(SkillDefinition skill, MonsterUnit currentTarget, float skillMultiplier)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		MonsterUnit monsterUnit = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
		if (!((Object)(object)monsterUnit == (Object)null))
		{
			FaceTarget(((Component)monsterUnit).transform.position);
			PlaySkillHitEffect(skill, monsterUnit);
			float skillPower = GetSkillPower(skill);
			float skillSecondaryPower = GetSkillSecondaryPower(skill);
			float skillDuration = GetSkillDuration(skill);
			float skillRadius = GetSkillRadius(skill);
			monsterUnit.TakeDamage(CalculateDamageAgainst(monsterUnit, skillPower * skillMultiplier, critical: false), critical: false, this);
			PlaySkillAreaEffect(skill, ((Component)monsterUnit).transform.position, skillDuration);
			SpawnAreaDamageZone(((Component)monsterUnit).transform.position, skillRadius, skillSecondaryPower * skillMultiplier, skillDuration, 1f, skill);
		}
	}

	private void SpawnAreaDamageZone(Vector3 center, float radius, float damagePerTick, float duration, float tickInterval, SkillDefinition sourceSkill = null)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("RuntimeAreaDamageZone");
		val.transform.position = center;
		RuntimeAreaDamageZone runtimeAreaDamageZone = val.AddComponent<RuntimeAreaDamageZone>();
		runtimeAreaDamageZone.Configure(center, radius, damagePerTick, duration, tickInterval, this, sourceSkill);
	}

	private void ApplyTaunt(float radius, float duration)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		float num = Mathf.Max(0.1f, radius);
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if ((Object)(object)monsterUnit != (Object)null && Vector3.Distance(((Component)this).transform.position, ((Component)monsterUnit).transform.position) <= num)
			{
				monsterUnit.ApplyTaunt(this, duration);
			}
		}
	}

	private void ApplyTemporaryDamageReduction(float reduction, float duration)
	{
		if (!(reduction <= 0f) && !(duration <= 0f))
		{
			temporaryDamageReductionBonus = Mathf.Max(temporaryDamageReductionBonus, Mathf.Clamp01(reduction));
			temporaryDamageReductionTimer = Mathf.Max(temporaryDamageReductionTimer, duration);
		}
	}

	private void ApplyThornsAura(float returnRatio, float duration)
	{
		if (!(returnRatio <= 0f) && !(duration <= 0f))
		{
			thornsReturnRatio = Mathf.Max(thornsReturnRatio, returnRatio);
			thornsTimer = Mathf.Max(thornsTimer, duration);
		}
	}

	private void SpawnSummonedAllies(SkillDefinition skill, MonsterUnit currentTarget, float skillMultiplier)
	{
		MonsterUnit monsterUnit = (((Object)(object)currentTarget != (Object)null) ? currentTarget : FindNearestSkillTarget(skill));
		if (!((Object)(object)monsterUnit == (Object)null))
		{
			int skillHitCount = GetSkillHitCount(skill);
			for (int i = 0; i < skillHitCount; i++)
			{
				SpawnSummonedAlly(skill, monsterUnit, skillMultiplier, i, skillHitCount);
			}
		}
	}

	public void TriggerAugmentSkillEcho(SkillDefinition sourceSkill, float skillMultiplier)
	{
		if (sourceSkill != null && definition != null && !(currentHealth <= 0f))
		{
			MonsterUnit monsterUnit = ResolveCombatTarget(null, sourceSkill);
			if (!((Object)(object)monsterUnit == (Object)null) || !SkillNeedsMonsterTarget(sourceSkill))
			{
				ApplySkillEffect(sourceSkill, monsterUnit, Mathf.Max(0.05f, skillMultiplier));
			}
		}
	}

	public void SpawnAugmentSummonedAllies(int count, float healthRatio, float attackRatio, SkillDefinition visualSkill = null)
	{
		int num = Mathf.Clamp(count, 1, 6);
		MonsterUnit anchorTarget = FindNearestTarget(Mathf.Max(2.5f, CurrentAttackRange + 2.4f));
		SkillDefinition skill = new SkillDefinition
		{
			id = "augment_summon",
			displayName = "Augment Summon",
			effectType = SkillEffectType.SummonRush,
			power = Mathf.Max(0.05f, healthRatio),
			secondaryPower = Mathf.Max(0.05f, attackRatio),
			duration = 0f,
			radius = 2.5f,
			hitCount = num,
			areaEffectPrefab = visualSkill?.areaEffectPrefab
		};
		for (int i = 0; i < num; i++)
		{
			SpawnSummonedAlly(skill, anchorTarget, 1f, i, num);
		}
	}

	private void SpawnSummonedAlly(SkillDefinition skill, MonsterUnit anchorTarget, float skillMultiplier, int index, int count)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = ResolveSummonedUnitPrefab();
		if (!((Object)(object)val == (Object)null))
		{
			Vector3 val2 = ResolveSummonPosition(anchorTarget, index, count);
			PlaySkillAreaEffect(skill, val2);
			GameObject val3 = Object.Instantiate<GameObject>(val, val2, ((Component)this).transform.rotation);
			DefenderUnit defenderUnit = val3.GetComponent<DefenderUnit>();
			if ((Object)(object)defenderUnit == (Object)null)
			{
				defenderUnit = val3.AddComponent<DefenderUnit>();
			}
			defenderUnit.AdoptRuntimeTemplate(this);
			val3.SetActive(true);
			defenderUnit.InitializeSummon(CreateSummonedDefinition(skill, skillMultiplier));
			RuntimeAudioUtility.PlayDiceAppear();
		}
	}

	private Vector3 ResolveSummonPosition(MonsterUnit anchorTarget, int index, int count)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = ((Component)this).transform.position;
		Vector3 val = (((Object)(object)anchorTarget != (Object)null) ? ((Component)anchorTarget).transform.position : (position + ((Component)this).transform.forward * 2.4f));
		Vector3 val2 = val - position;
		val2.y = 0f;
		if (((Vector3)(ref val2)).sqrMagnitude <= 0.0001f)
		{
			val2 = ((Component)this).transform.forward;
			val2.y = 0f;
		}
		if (((Vector3)(ref val2)).sqrMagnitude <= 0.0001f)
		{
			val2 = Vector3.forward;
		}
		Vector3 normalized = ((Vector3)(ref val2)).normalized;
		Vector3 val3 = default(Vector3);
		((Vector3)(ref val3))._002Ector(normalized.z, 0f, 0f - normalized.x);
		float num = ((count <= 1) ? 0f : Mathf.Lerp(-0.72f, 0.72f, (float)index / (float)(count - 1)));
		float num2 = Mathf.Clamp(((Vector3)(ref val2)).magnitude * 0.55f, 1.55f, 3.2f);
		Vector3 result = position + normalized * num2 + val3 * num;
		result.y = position.y;
		return result;
	}

	private CharacterDefinition CreateSummonedDefinition(SkillDefinition skill, float skillMultiplier)
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		float skillPower = GetSkillPower(skill);
		float skillSecondaryPower = GetSkillSecondaryPower(skill);
		float healthRatio = Mathf.Max(0.05f, skillPower * skillMultiplier);
		float attackRatio = Mathf.Max(0.05f, ((skillSecondaryPower > 0f) ? skillSecondaryPower : skillPower) * skillMultiplier);
		GameObject prefab = ResolveSummonedUnitPrefab();
		CharacterDefinition characterDefinition = new CharacterDefinition
		{
			id = definition.id + "_summon",
			displayName = definition.displayName + " Spirit",
			description = "Temporary summoned ally.",
			grade = definition.grade,
			role = CharacterRole.Summoner,
			tags = ((definition.tags != null) ? new List<CharacterTag>(definition.tags) : new List<CharacterTag>()),
			accentColor = Color.Lerp(definition.accentColor, Color.white, 0.28f),
			prefab = prefab,
			stats = CloneCombatStats(definition.stats, healthRatio, attackRatio),
			attackBehavior = CloneAttackBehavior(definition.attackBehavior),
			skills = new List<SkillDefinition>(),
			mergeValue = 0
		};
		characterDefinition.stats.maxMana = 0f;
		characterDefinition.stats.manaRegenPerSecondRate = 0f;
		characterDefinition.stats.manaGainWhenHitRate = 0f;
		characterDefinition.stats.manaGainPerAttackRate = 0f;
		return characterDefinition;
	}

	private GameObject ResolveSummonedUnitPrefab()
	{
		if ((Object)(object)defaultSummonedUnitPrefab != (Object)null)
		{
			return defaultSummonedUnitPrefab;
		}
		if (definition != null && (Object)(object)definition.prefab != (Object)null)
		{
			return definition.prefab;
		}
		return ((Component)this).gameObject;
	}

	private CombatStats CloneCombatStats(CombatStats source, float healthRatio, float attackRatio)
	{
		if (source == null)
		{
			source = new CombatStats();
		}
		return new CombatStats
		{
			maxHealth = Mathf.Max(1f, source.maxHealth * healthRatio),
			attackPower = Mathf.Max(0.5f, source.attackPower * attackRatio),
			criticalChance = source.criticalChance,
			criticalDamageMultiplier = source.criticalDamageMultiplier,
			attackSpeed = source.attackSpeed,
			maxMana = source.maxMana,
			manaRegenPerSecondRate = source.manaRegenPerSecondRate,
			manaGainWhenHitRate = source.manaGainWhenHitRate,
			manaGainPerAttackRate = source.manaGainPerAttackRate,
			attackRange = source.attackRange,
			moveSpeed = source.moveSpeed,
			projectileSpeed = source.projectileSpeed
		};
	}

	private AttackBehavior CloneAttackBehavior(AttackBehavior source)
	{
		if (source == null)
		{
			return new AttackBehavior();
		}
		return new AttackBehavior
		{
			basicAttackType = source.basicAttackType,
			useAttackTypeRange = source.useAttackTypeRange,
			meleeAttackRange = source.meleeAttackRange,
			rangedAttackRange = source.rangedAttackRange,
			useCustomAttackRange = source.useCustomAttackRange,
			customAttackRange = source.customAttackRange,
			splashRadius = source.splashRadius,
			splashDamageRatio = source.splashDamageRatio,
			additionalPierceCount = source.additionalPierceCount,
			projectilePrefabOverride = source.projectilePrefabOverride,
			muzzleEffectPrefab = source.muzzleEffectPrefab,
			hitEffectPrefab = source.hitEffectPrefab
		};
	}

	private void HandleAnimationImpact(AnimationImpactType impactType)
	{
		if (ShouldResolvePendingBasicAttack(impactType))
		{
			ResolvePendingBasicAttack();
		}
		else if (impactType == AnimationImpactType.Skill && pendingSkillCast.isValid)
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

	private void SchedulePendingSkillFallback(int sequence)
	{
		CancelPendingSkillFallback();
		float delay = (((Object)(object)animationDriver != (Object)null) ? animationDriver.SkillImpactFallbackDelay : 0.2f);
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
		pendingBasicAttack = default(PendingBasicAttack);
		pendingSkillCast = default(PendingSkillCast);
		CancelPendingAttackFallback();
		CancelPendingSkillFallback();
	}

	private void FaceTarget(Vector3 targetPosition)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = targetPosition - ((Component)this).transform.position;
		val.y = 0f;
		if (!(((Vector3)(ref val)).sqrMagnitude <= 0.0001f))
		{
			((Component)this).transform.rotation = Quaternion.LookRotation(((Vector3)(ref val)).normalized, Vector3.up);
		}
	}

	private MonsterUnit FindNearestTarget()
	{
		return FindNearestTarget(GetEffectiveAttackRange());
	}

	private MonsterUnit FindBasicAttackTarget()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		float effectiveAttackRange = GetEffectiveAttackRange();
		if (CombatRuntimeQuery.IsValidMonsterTarget(cachedBasicAttackTarget, ((Component)this).transform.position, effectiveAttackRange) && Time.unscaledTime < nextBasicTargetRefreshTime)
		{
			return cachedBasicAttackTarget;
		}
		if (definition != null && string.Equals(definition.id, "hero_57", StringComparison.OrdinalIgnoreCase))
		{
			cachedBasicAttackTarget = CombatRuntimeQuery.FindRandomMonster(monsters, ((Component)this).transform.position, effectiveAttackRange);
		}
		else
		{
			cachedBasicAttackTarget = FindNearestTarget(effectiveAttackRange);
		}
		nextBasicTargetRefreshTime = CombatRuntimeQuery.ScheduleNextRefresh((Object)(object)this, basicTargetRefreshInterval);
		return cachedBasicAttackTarget;
	}

	private MonsterUnit FindNearestSkillTarget(SkillDefinition skill)
	{
		if (!SkillNeedsMonsterTarget(skill))
		{
			return null;
		}
		return FindNearestTarget(GetEffectiveSkillRange(skill));
	}

	private MonsterUnit FindNearestTarget(float range)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		return CombatRuntimeQuery.FindNearestMonster(monsters, ((Component)this).transform.position, range);
	}

	private List<MonsterUnit> GetNearestTargets(int count)
	{
		return GetNearestTargets(count, float.MaxValue);
	}

	private List<MonsterUnit> GetNearestTargets(int count, float range)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.Max(0, count);
		List<MonsterUnit> list = new List<MonsterUnit>(num);
		if (num == 0)
		{
			return list;
		}
		float num2 = Mathf.Max(0.1f, range);
		float num3 = num2 * num2;
		Vector3 position = ((Component)this).transform.position;
		for (int i = 0; i < monsters.Count; i++)
		{
			MonsterUnit monsterUnit = monsters[i];
			if ((Object)(object)monsterUnit == (Object)null || !monsterUnit.CanBeCombatTargeted)
			{
				continue;
			}
			Vector3 val = ((Component)monsterUnit).transform.position - position;
			float sqrMagnitude = ((Vector3)(ref val)).sqrMagnitude;
			if (sqrMagnitude > num3)
			{
				continue;
			}
			int num4;
			for (num4 = list.Count; num4 > 0; num4--)
			{
				val = ((Component)list[num4 - 1]).transform.position - position;
				float sqrMagnitude2 = ((Vector3)(ref val)).sqrMagnitude;
				if (sqrMagnitude2 <= sqrMagnitude)
				{
					break;
				}
			}
			if (num4 < num)
			{
				list.Insert(num4, monsterUnit);
				if (list.Count > num)
				{
					list.RemoveAt(list.Count - 1);
				}
			}
		}
		return list;
	}

	private List<MonsterUnit> GetRandomTargetsWithReplacement(int count, float range)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.Max(0, count);
		List<MonsterUnit> list = new List<MonsterUnit>(num);
		for (int i = 0; i < num; i++)
		{
			MonsterUnit monsterUnit = CombatRuntimeQuery.FindRandomMonster(monsters, ((Component)this).transform.position, range);
			if ((Object)(object)monsterUnit == (Object)null)
			{
				break;
			}
			list.Add(monsterUnit);
		}
		return list;
	}

	private MonsterUnit ResolveCombatTarget(MonsterUnit currentTarget, SkillDefinition skill)
	{
		if ((Object)(object)currentTarget != (Object)null && currentTarget.CanBeCombatTargeted)
		{
			return currentTarget;
		}
		return FindNearestSkillTarget(skill);
	}

	private bool SkillNeedsMonsterTarget(SkillDefinition skill)
	{
		if (skill == null)
		{
			return false;
		}
		switch (skill.effectType)
		{
		case SkillEffectType.HealSelf:
		case SkillEffectType.AttackSpeedBoost:
		case SkillEffectType.CriticalBoost:
		case SkillEffectType.ManaSurge:
		case SkillEffectType.ShieldAlly:
		case SkillEffectType.DefenseBuff:
		case SkillEffectType.Transform:
		case SkillEffectType.Taunt:
		case SkillEffectType.ManaRestoreAdjacent:
		case SkillEffectType.HealLowestAllies:
		case SkillEffectType.ThornsAura:
		case SkillEffectType.DeathPoisonField:
		case SkillEffectType.AllyAttackSpeedBoost:
			return false;
		case SkillEffectType.AreaDamage:
			return true;
		default:
			return true;
		}
	}

	private DefenderUnit FindLowestHealthAlly()
	{
		IReadOnlyList<DefenderUnit> activeInstances = ActiveInstances;
		DefenderUnit result = null;
		float num = float.MaxValue;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			DefenderUnit defenderUnit = activeInstances[i];
			if (!((Object)(object)defenderUnit == (Object)null) && !(defenderUnit.CurrentHealth <= 0f))
			{
				float healthRatio = defenderUnit.HealthRatio;
				if (healthRatio < num)
				{
					num = healthRatio;
					result = defenderUnit;
				}
			}
		}
		return result;
	}

	private List<DefenderUnit> FindLowestHealthAllies(int count)
	{
		int num = Mathf.Max(1, count);
		List<DefenderUnit> list = new List<DefenderUnit>(num);
		IReadOnlyList<DefenderUnit> activeInstances = ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			DefenderUnit defenderUnit = activeInstances[i];
			if ((Object)(object)defenderUnit == (Object)null || defenderUnit.CurrentHealth <= 0f || defenderUnit.CurrentHealth >= defenderUnit.MaxHealth - 0.01f)
			{
				continue;
			}
			float healthRatio = defenderUnit.HealthRatio;
			int num2 = list.Count;
			while (num2 > 0 && list[num2 - 1].HealthRatio > healthRatio)
			{
				num2--;
			}
			if (num2 < num)
			{
				list.Insert(num2, defenderUnit);
				if (list.Count > num)
				{
					list.RemoveAt(list.Count - 1);
				}
			}
		}
		return list;
	}

	private List<DefenderUnit> FindAdjacentManaTargets(float radius)
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		DefenderUnit defenderUnit = null;
		DefenderUnit defenderUnit2 = null;
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = Mathf.Max(0f, radius);
		IReadOnlyList<DefenderUnit> activeInstances = ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			DefenderUnit defenderUnit3 = activeInstances[i];
			if ((Object)(object)defenderUnit3 == (Object)null || (Object)(object)defenderUnit3 == (Object)(object)this || defenderUnit3.CurrentHealth <= 0f || defenderUnit3.MaxMana <= 0f || defenderUnit3.CurrentMana >= defenderUnit3.MaxMana * 0.999f)
			{
				continue;
			}
			Vector3 val = ((Component)defenderUnit3).transform.position - ((Component)this).transform.position;
			Vector2 val2 = new Vector2(val.x, val.z);
			float magnitude = ((Vector2)(ref val2)).magnitude;
			if (!(num3 > 0f) || !(magnitude > num3))
			{
				if (val.x < -0.05f && magnitude < num)
				{
					num = magnitude;
					defenderUnit = defenderUnit3;
				}
				else if (val.x > 0.05f && magnitude < num2)
				{
					num2 = magnitude;
					defenderUnit2 = defenderUnit3;
				}
			}
		}
		List<DefenderUnit> list = new List<DefenderUnit>(2);
		if ((Object)(object)defenderUnit != (Object)null)
		{
			list.Add(defenderUnit);
		}
		if ((Object)(object)defenderUnit2 != (Object)null)
		{
			list.Add(defenderUnit2);
		}
		return list;
	}

	private bool HasMonsterInRadius(float radius)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return HasMonsterInRadius(((Component)this).transform.position, radius);
	}

	private bool HasMonsterInRadius(Vector3 center, float radius)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Max(0.1f, radius);
		for (int num2 = monsters.Count - 1; num2 >= 0; num2--)
		{
			MonsterUnit monsterUnit = monsters[num2];
			if ((Object)(object)monsterUnit == (Object)null)
			{
				monsters.RemoveAt(num2);
			}
			else if (Vector3.Distance(center, ((Component)monsterUnit).transform.position) <= num)
			{
				return true;
			}
		}
		return false;
	}

	private IEnumerator GroundDamageRoutine(Vector3 center, float radius, float damagePerTick, float duration, float tickInterval, SkillDefinition sourceSkill = null)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		float elapsed = 0f;
		float interval = Mathf.Max(0.15f, tickInterval);
		float checkedRadius = Mathf.Max(0.25f, radius);
		for (; elapsed < duration; elapsed += interval)
		{
			IReadOnlyList<MonsterUnit> activeTargets = MonsterUnit.ActiveInstances;
			for (int i = 0; i < activeTargets.Count; i++)
			{
				MonsterUnit monster = activeTargets[i];
				if ((Object)(object)monster != (Object)null && monster.CanBeCombatTargeted && Vector3.Distance(center, ((Component)monster).transform.position) <= checkedRadius)
				{
					RunWithSkillDamageContext(sourceSkill, delegate
					{
						monster.TakeDamage(damagePerTick, critical: false, this);
					});
				}
			}
			yield return (object)new WaitForSeconds(interval);
		}
	}

	private void ApplyDefenseBuff(float shieldRatio, float duration, float radius, GameObject effectPrefab)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Max(0.25f, radius);
		float num2 = num * num;
		IReadOnlyList<DefenderUnit> activeInstances = ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			DefenderUnit defenderUnit = activeInstances[i];
			if (!((Object)(object)defenderUnit == (Object)null) && !(defenderUnit.CurrentHealth <= 0f))
			{
				Vector3 val = ((Component)this).transform.position - ((Component)defenderUnit).transform.position;
				if (((Vector3)(ref val)).sqrMagnitude <= num2)
				{
					defenderUnit.AddShield(defenderUnit.MaxHealth * shieldRatio, duration, effectPrefab);
				}
			}
		}
	}

	private void ShowInstantSupportFeedback(string label, Color color, GameObject effectPrefab, float effectDuration)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		floatingUi?.ShowStatus(label, color, 0.85f);
		RuntimeCombatFeedback.ShowGroundPulse(((Component)this).transform.position, color, 0.42f, 0.42f);
		PlayAttachedSupportEffect(effectPrefab, effectDuration);
	}

	private GameObject ShowTimedSupportFeedback(string label, Color color, float duration, GameObject effectPrefab)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		floatingUi?.ShowTimedStatus(label, color, duration);
		RuntimeCombatFeedback.ShowGroundPulse(((Component)this).transform.position, color, 0.48f, 0.55f);
		return PlayAttachedSupportEffect(effectPrefab, duration);
	}

	private GameObject PlayAttachedSupportEffect(GameObject effectPrefab, float duration)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)effectPrefab == (Object)null || duration <= 0f)
		{
			return null;
		}
		GameObject val = RuntimeEffectUtility.PlayAttachedTimed(effectPrefab, ((Component)this).transform, Vector3.zero, Quaternion.identity, duration);
		TrackOwnedSupportEffect(val);
		return val;
	}

	public void ClearRoundTemporaryEffects()
	{
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		attackSpeedBonus = 0f;
		critChanceBonus = 0f;
		temporaryAttackPowerBonus = 0f;
		temporaryAttackSpeedBonus = 0f;
		temporaryAttackPowerReduction = 0f;
		temporaryAttackPowerReductionTimer = 0f;
		temporaryDamageReductionBonus = 0f;
		thornsReturnRatio = 0f;
		currentShield = 0f;
		attackSpeedBuffTimer = 0f;
		critBuffTimer = 0f;
		temporaryCombatBoostTimer = 0f;
		temporaryDamageReductionTimer = 0f;
		thornsTimer = 0f;
		shieldTimer = 0f;
		stunTimer = 0f;
		if (definition != null)
		{
			floatingUi?.Configure(definition.displayName, definition.accentColor, definition.grade);
		}
		ClearOwnedSupportEffects();
		floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
	}

	private void TrackOwnedSupportEffect(GameObject effect)
	{
		if (!((Object)(object)effect == (Object)null))
		{
			PruneOwnedSupportEffects();
			if (!ownedSupportEffects.Contains(effect))
			{
				ownedSupportEffects.Add(effect);
			}
		}
	}

	private void ClearShieldEffect()
	{
		if (!((Object)(object)activeShieldEffect == (Object)null))
		{
			ownedSupportEffects.Remove(activeShieldEffect);
			RuntimeEffectUtility.DestroyEffect(activeShieldEffect);
			activeShieldEffect = null;
		}
	}

	private void ClearOwnedSupportEffects()
	{
		for (int num = ownedSupportEffects.Count - 1; num >= 0; num--)
		{
			GameObject effect = ownedSupportEffects[num];
			ownedSupportEffects.RemoveAt(num);
			RuntimeEffectUtility.DestroyEffect(effect);
		}
		activeShieldEffect = null;
	}

	private void PruneOwnedSupportEffects()
	{
		for (int num = ownedSupportEffects.Count - 1; num >= 0; num--)
		{
			if ((Object)(object)ownedSupportEffects[num] == (Object)null)
			{
				ownedSupportEffects.RemoveAt(num);
			}
		}
	}

	private void ApplyAllyAttackSpeedBoost(float attackSpeedRatio, float duration, float radius, GameObject effectPrefab)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Max(0.25f, radius);
		float num2 = num * num;
		IReadOnlyList<DefenderUnit> activeInstances = ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			DefenderUnit defenderUnit = activeInstances[i];
			if (!((Object)(object)defenderUnit == (Object)null) && !(defenderUnit.CurrentHealth <= 0f))
			{
				Vector3 val = ((Component)this).transform.position - ((Component)defenderUnit).transform.position;
				if (((Vector3)(ref val)).sqrMagnitude <= num2)
				{
					defenderUnit.ActivateTimedCombatBoost(0f, attackSpeedRatio, duration, effectPrefab, "공속 +" + Mathf.RoundToInt(attackSpeedRatio * 100f) + "%", AttackSpeedFeedbackColor);
				}
			}
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
		if (shieldTimer > 0f)
		{
			shieldTimer -= Time.deltaTime;
			if (shieldTimer <= 0f)
			{
				float num = currentShield;
				currentShield = 0f;
				ClearShieldEffect();
				if (num > 0f)
				{
					DefenderUnit.OnShieldResolved?.Invoke(this, num, arg3: true, null);
				}
			}
		}
		if (stunTimer > 0f)
		{
			stunTimer -= Time.deltaTime;
		}
		if (temporaryCombatBoostTimer > 0f)
		{
			temporaryCombatBoostTimer -= Time.deltaTime;
			if (temporaryCombatBoostTimer <= 0f)
			{
				temporaryAttackPowerBonus = 0f;
				temporaryAttackSpeedBonus = 0f;
			}
		}
		if (temporaryAttackPowerReductionTimer > 0f)
		{
			temporaryAttackPowerReductionTimer -= Time.deltaTime;
			if (temporaryAttackPowerReductionTimer <= 0f)
			{
				temporaryAttackPowerReductionTimer = 0f;
				temporaryAttackPowerReduction = 0f;
			}
		}
		if (temporaryDamageReductionTimer > 0f)
		{
			temporaryDamageReductionTimer -= Time.deltaTime;
			if (temporaryDamageReductionTimer <= 0f)
			{
				temporaryDamageReductionBonus = 0f;
			}
		}
		if (thornsTimer > 0f)
		{
			thornsTimer -= Time.deltaTime;
			if (thornsTimer <= 0f)
			{
				thornsReturnRatio = 0f;
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

	private void ApplyVisuals()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		if (tintRenderers == null || tintRenderers.Length == 0)
		{
			tintRenderers = ((Component)this).GetComponentsInChildren<Renderer>(true);
		}
		for (int i = 0; i < tintRenderers.Length; i++)
		{
			ApplyRendererTint(tintRenderers[i], definition.accentColor);
		}
		Color tint = (RuntimeRenderBatchingUtility.UsePerInstanceUnitTint ? definition.accentColor : Color.white);
		GpuSkinnedUnitRenderer.AttachOrRefresh(((Component)this).gameObject, tintRenderers, tint, isDefender: true, isBoss: false);
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

	private void EnsureInteractionCollider()
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)((Component)this).GetComponentInChildren<Collider>(true) != (Object)null)
		{
			return;
		}
		if (tintRenderers == null || tintRenderers.Length == 0)
		{
			tintRenderers = ((Component)this).GetComponentsInChildren<Renderer>(true);
		}
		Bounds bounds = default(Bounds);
		((Bounds)(ref bounds))._002Ector(((Component)this).transform.position, Vector3.one);
		bool flag = false;
		for (int i = 0; i < tintRenderers.Length; i++)
		{
			Renderer val = tintRenderers[i];
			if (!((Object)(object)val == (Object)null))
			{
				if (!flag)
				{
					bounds = val.bounds;
					flag = true;
				}
				else
				{
					((Bounds)(ref bounds)).Encapsulate(val.bounds);
				}
			}
		}
		BoxCollider val2 = ((Component)this).gameObject.GetComponent<BoxCollider>();
		if ((Object)(object)val2 == (Object)null)
		{
			val2 = ((Component)this).gameObject.AddComponent<BoxCollider>();
		}
		Vector3 val3 = (flag ? ((Bounds)(ref bounds)).center : (((Component)this).transform.position + Vector3.up * 0.9f));
		Vector3 val4 = (Vector3)(flag ? ((Bounds)(ref bounds)).size : new Vector3(1f, 1.8f, 1f));
		val2.center = ((Component)this).transform.InverseTransformPoint(val3);
		val2.size = new Vector3(Mathf.Max(0.6f, val4.x), Mathf.Max(1.2f, val4.y), Mathf.Max(0.6f, val4.z));
	}

	private void Die()
	{
		if (!isDying)
		{
			isDying = true;
			TriggerDeathPoisonField();
			PlayDeathEffect();
			RemoveFromBoard();
			DefenderUnit.OnDefenderRemoved?.Invoke(this);
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
	}

	private void PlayDeathEffect()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		RuntimeEffectUtility.PlayOneShot(deathEffectPrefab, ((Component)this).transform.position + deathEffectOffset, Quaternion.identity, 3f);
	}

	private void TriggerDeathPoisonField()
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		if (definition == null || definition.skills == null)
		{
			return;
		}
		for (int i = 0; i < definition.skills.Count; i++)
		{
			SkillDefinition skillDefinition = definition.skills[i];
			if (skillDefinition != null && skillDefinition.effectType == SkillEffectType.DeathPoisonField)
			{
				Vector3 val = ((Component)this).transform.forward;
				MonsterUnit monsterUnit = FindNearestSkillTarget(skillDefinition);
				if ((Object)(object)monsterUnit != (Object)null)
				{
					val = ((Component)monsterUnit).transform.position - ((Component)this).transform.position;
					val.y = 0f;
				}
				if (((Vector3)(ref val)).sqrMagnitude <= 0.0001f)
				{
					val = ((Component)this).transform.forward;
				}
				((Vector3)(ref val)).Normalize();
				float num = Mathf.Max(0.25f, GetSkillRadius(skillDefinition));
				float num2 = Mathf.Max(0.1f, GetSkillDuration(skillDefinition));
				float num3 = Mathf.Max(0.1f, 1f + permanentSkillPowerBonus + synergyBonus.skillPowerBonus + tileSkillPowerBonus);
				Vector3 center = ((Component)this).transform.position + val * Mathf.Max(1f, num * 0.6f);
				center.y = ((Component)this).transform.position.y;
				PlaySkillAreaEffect(skillDefinition, center, num2);
				SpawnAreaDamageZone(center, num, GetSkillPower(skillDefinition) * num3, num2, Mathf.Max(0.2f, GetSkillSecondaryPower(skillDefinition)), skillDefinition);
			}
		}
	}

	private void HandleMonsterSpawned(MonsterUnit monster)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)monster != (Object)null && !monsters.Contains(monster))
		{
			monsters.Add(monster);
		}
		if (!CombatRuntimeQuery.IsValidMonsterTarget(cachedBasicAttackTarget, ((Component)this).transform.position, GetEffectiveAttackRange()))
		{
			InvalidateBasicTargetCache();
		}
	}

	private void HandleMonsterRemoved(MonsterUnit monster)
	{
		monsters.Remove(monster);
		if ((Object)(object)cachedBasicAttackTarget == (Object)(object)monster)
		{
			InvalidateBasicTargetCache();
		}
		if (!HasAnyLivingMonster())
		{
			ResetFacingToDefault();
			if ((Object)(object)animationDriver == (Object)null || !animationDriver.IsLocked)
			{
				animationDriver?.ForceIdle();
			}
		}
	}

	private void InvalidateBasicTargetCache()
	{
		cachedBasicAttackTarget = null;
		nextBasicTargetRefreshTime = 0f;
	}

	private bool HasAnyLivingMonster()
	{
		for (int num = monsters.Count - 1; num >= 0; num--)
		{
			MonsterUnit monsterUnit = monsters[num];
			if ((Object)(object)monsterUnit == (Object)null)
			{
				monsters.RemoveAt(num);
			}
			else if (monsterUnit.CanBeCombatTargeted)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsCombatActive()
	{
		return HasAnyLivingMonster();
	}

	private bool CanStartActionAnimation()
	{
		return (Object)(object)animationDriver == (Object)null || !animationDriver.IsLocked;
	}

	private float GetEffectiveAttackRange()
	{
		float num = definition.stats.attackRange;
		if (definition.attackBehavior != null)
		{
			num = definition.attackBehavior.ResolveAttackRange(num);
		}
		return Mathf.Max(0.5f, num + attackRangeBonus + synergyBonus.rangeBonus + tileAttackRangeBonus);
	}

	private float GetEffectiveSkillRange(SkillDefinition skill)
	{
		float num = ((skill != null && skill.useCustomCastRange) ? skill.castRange : definition.stats.attackRange);
		return Mathf.Max(0.5f, num + attackRangeBonus + synergyBonus.rangeBonus + tileAttackRangeBonus);
	}

	private float GetSkillPower(SkillDefinition skill)
	{
		return GetSkillFloatValue(skill, SkillGrowthTarget.Power, skill?.power ?? 0f);
	}

	private float GetSkillSecondaryPower(SkillDefinition skill)
	{
		return GetSkillFloatValue(skill, SkillGrowthTarget.SecondaryPower, skill?.secondaryPower ?? 0f);
	}

	private float GetSkillDuration(SkillDefinition skill)
	{
		return GetSkillFloatValue(skill, SkillGrowthTarget.Duration, skill?.duration ?? 0f);
	}

	private float GetSkillRadius(SkillDefinition skill)
	{
		return GetSkillFloatValue(skill, SkillGrowthTarget.Radius, skill?.radius ?? 0f);
	}

	private int GetSkillHitCount(SkillDefinition skill)
	{
		int num = Mathf.Max(1, skill?.hitCount ?? 1);
		if (!IsSkillGrowthTarget(skill, SkillGrowthTarget.HitCount))
		{
			return num;
		}
		return Mathf.Max(1, Mathf.RoundToInt((float)num * GetSkillGrowthMultiplier(skill, SkillGrowthTarget.HitCount)));
	}

	private float GetSkillFloatValue(SkillDefinition skill, SkillGrowthTarget target, float baseValue)
	{
		return baseValue * GetSkillGrowthMultiplier(skill, target);
	}

	private float GetSkillGrowthMultiplier(SkillDefinition skill, SkillGrowthTarget target)
	{
		if (!IsSkillGrowthTarget(skill, target))
		{
			return 1f;
		}
		return Mathf.Max(0f, 1f + permanentSkillParameterGrowthBonus);
	}

	private bool IsSkillGrowthTarget(SkillDefinition skill, SkillGrowthTarget target)
	{
		return skill != null && target != SkillGrowthTarget.None && (skill.growthTargets & target) != 0;
	}

	private float ResolveSkillGrowthStepRatio()
	{
		if (definition != null && definition.skills != null)
		{
			for (int i = 0; i < definition.skills.Count; i++)
			{
				SkillDefinition skillDefinition = definition.skills[i];
				if (skillDefinition != null && skillDefinition.growthTargets != SkillGrowthTarget.None)
				{
					return Mathf.Max(0f, skillDefinition.growthStepRatio);
				}
			}
		}
		return 0.1f;
	}

	private float GetEffectiveAttackPower()
	{
		float num = 1f + permanentAttackPowerBonus + synergyBonus.attackPowerBonus + temporaryAttackPowerBonus + tileAttackPowerBonus - temporaryAttackPowerReduction;
		return definition.stats.attackPower * Mathf.Max(0.1f, num);
	}

	private float CalculateDamageAgainst(MonsterUnit target, float multiplier, bool critical)
	{
		float num = GetEffectiveAttackPower() * multiplier;
		if (critical)
		{
			num *= definition.stats.criticalDamageMultiplier + permanentCriticalDamageBonus + synergyBonus.criticalDamageBonus;
		}
		if ((Object)(object)target != (Object)null && target.IsBoss)
		{
			num *= 1f + permanentBossDamageBonus + synergyBonus.bossDamageBonus + tileBossDamageBonus;
		}
		return num;
	}

	private float GetEffectiveCriticalChance()
	{
		return Mathf.Clamp01(definition.stats.criticalChance + critChanceBonus + permanentCritChanceBonus + synergyBonus.critChanceBonus);
	}

	private float GetBasicAttackSplashRadius()
	{
		float num = ((definition.attackBehavior != null) ? definition.attackBehavior.splashRadius : 0f);
		return Mathf.Max(0f, num + splashRadiusBonus + synergyBonus.splashRadiusBonus);
	}

	private float GetBasicAttackSplashDamageRatio()
	{
		float num = ((definition.attackBehavior != null) ? definition.attackBehavior.splashDamageRatio : 0f);
		return Mathf.Clamp01(num + splashDamageRatioBonus + synergyBonus.splashDamageRatioBonus);
	}

	private void ApplyBasicAttackSplash(MonsterUnit primaryTarget, float baseDamage)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		float basicAttackSplashRadius = GetBasicAttackSplashRadius();
		float basicAttackSplashDamageRatio = GetBasicAttackSplashDamageRatio();
		if ((Object)(object)primaryTarget == (Object)null || basicAttackSplashRadius <= 0f || basicAttackSplashDamageRatio <= 0f)
		{
			return;
		}
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if (!((Object)(object)monsterUnit == (Object)null) && !((Object)(object)monsterUnit == (Object)(object)primaryTarget) && monsterUnit.CanBeCombatTargeted && Vector3.Distance(((Component)primaryTarget).transform.position, ((Component)monsterUnit).transform.position) <= basicAttackSplashRadius)
			{
				monsterUnit.TakeDamage(baseDamage * basicAttackSplashDamageRatio, critical: false, this);
			}
		}
	}
}
