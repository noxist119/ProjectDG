using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseGame
{
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
		private GameObject diceAutoDormantEffectPrefab;

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


		private bool alternatingDormant;

		private int alternatingLastRound = -1;

		private GameObject activeDormantRestEffect;
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
			floatingUi?.SetRecipeMarker(active, label, color);
		}

		internal static void ReportDamageDealt(DefenderUnit source, MonsterUnit target, float damage, bool critical)
		{
			if (!(source == null) && !(target == null) && !(damage <= 0f))
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
			SkillDefinition previousSkill = CurrentDamageSkillContext;
			CurrentDamageSkillContext = skill;
			try
			{
				action();
			}
			finally
			{
				CurrentDamageSkillContext = previousSkill;
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
			ClearDormantRestEffect();
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
			for (int i = ActiveDefenders.Count - 1; i >= 0; i--)
			{
				if (ActiveDefenders[i] == null)
				{
					ActiveDefenders.RemoveAt(i);
				}
			}
		}

		private bool IsAlternatingRoundBurstUnit()
		{
			return definition != null && string.Equals(definition.id, "hero_56", StringComparison.OrdinalIgnoreCase);
		}

		public static float ResolveAlternatingRoundStartMana(float maxMana, bool activeRound)
		{
			return activeRound ? Mathf.Max(0f, maxMana) * 0.5f : 0f;
		}

		private void BindAlternatingRoundController()
		{
			if (!IsAlternatingRoundBurstUnit())
			{
				UnbindAlternatingRoundController();
				return;
			}
			DefenseGameController controller = ((DefenseGameController.Active != null) ? DefenseGameController.Active : UnityEngine.Object.FindObjectOfType<DefenseGameController>());
			if (!(controller == alternatingRoundController))
			{
				UnbindAlternatingRoundController();
				alternatingRoundController = controller;
				if (alternatingRoundController != null)
				{
					alternatingRoundController.OnRoundStarted += HandleAlternatingRoundStarted;
				}
			}
		}

		private void UnbindAlternatingRoundController()
		{
			if (alternatingRoundController != null)
			{
				alternatingRoundController.OnRoundStarted -= HandleAlternatingRoundStarted;
				alternatingRoundController = null;
			}
		}

		private void HandleAlternatingRoundStarted(int round)
		{
			if (IsAlternatingRoundBurstUnit() && round != alternatingLastRound)
			{
				alternatingLastRound = round;
					ClearDormantRestEffect();
				if (alternatingNextRoundWillBurst)
				{
					alternatingDormant = false;
					alternatingNextRoundWillBurst = false;
					currentMana = ResolveAlternatingRoundStartMana(MaxMana, activeRound: true);
					ShowInstantSupportFeedback("기동 준비", AttackSpeedFeedbackColor, null, 0.8f);
				}
				else
				{
					alternatingDormant = true;
					alternatingNextRoundWillBurst = true;
					currentMana = ResolveAlternatingRoundStartMana(MaxMana, activeRound: false);
					animationDriver?.PlayDormantLoop();
					EnsureDormantRestEffect();
					ShowInstantSupportFeedback("휴식 모드", ShieldFeedbackColor, null, 0.8f);
				}
			}
		}

		public void ConfigureRuntimePieces(Projectile projectileTemplate, Transform launchPoint, Renderer[] renderers, GameObject summonedUnitTemplate = null, GameObject muzzleEffectTemplate = null, GameObject hitEffectTemplate = null, GameObject areaEffectTemplate = null, GameObject deathEffectTemplate = null, GameObject diceAutoDormantEffectTemplate = null)
		{
			projectilePrefab = projectileTemplate;
			diceAutoDormantEffectPrefab = diceAutoDormantEffectTemplate;
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
			if (template == null)
			{
				return;
			}
			diceAutoDormantEffectPrefab = template.diceAutoDormantEffectPrefab;
			projectilePrefab = template.projectilePrefab;
			defaultSummonedUnitPrefab = template.defaultSummonedUnitPrefab;
			defaultMuzzleEffectPrefab = template.defaultMuzzleEffectPrefab;
			defaultHitEffectPrefab = template.defaultHitEffectPrefab;
			defaultAreaEffectPrefab = template.defaultAreaEffectPrefab;
			deathEffectPrefab = template.deathEffectPrefab;
			deathEffectOffset = template.deathEffectOffset;
			if (firePoint == null)
			{
				Transform existingPoint = base.transform.Find("FirePoint");
				if (existingPoint != null)
				{
					firePoint = existingPoint;
				}
				else
				{
					GameObject firePointObject = new GameObject("FirePoint");
					firePointObject.transform.SetParent(base.transform);
					firePointObject.transform.localPosition = new Vector3(0f, 0.8f, 0.6f);
					firePoint = firePointObject.transform;
				}
			}
			if (tintRenderers == null || tintRenderers.Length == 0)
			{
				tintRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
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
			bool combatActive = IsCombatActive();
			bool alternatingRoundBurstUnit = IsAlternatingRoundBurstUnit();
			if (alternatingRoundBurstUnit && (!combatActive || alternatingDormant))
			{
				EnsureDormantRestEffect();
				animationDriver?.PlayDormantLoop();
				return;
			}
			ClearDormantRestEffect();
			animationDriver?.PlayMoving(isMoving: false);
			if (combatActive)
			{
				RegenerateCombatMana();
			}
			if (stunTimer > 0f)
			{
				if (animationDriver == null || !animationDriver.IsLocked)
				{
					animationDriver?.ForceIdle();
				}
			}
			else if (!combatActive)
			{
				ResetFacingToDefault();
				if (animationDriver == null || !animationDriver.IsLocked)
				{
					animationDriver?.ForceIdle();
				}
			}
			else if (!TryCastSkill())
			{
				MonsterUnit target = FindBasicAttackTarget();
				if (!(target == null) && attackCooldown <= 0f && CanStartActionAnimation())
				{
					PerformAttack(target);
				}
			}
		}

		private void RegenerateCombatMana()
		{
			if (!(MaxMana <= 0f))
			{
				float manaRegenRate = Mathf.Clamp01(definition.stats.manaRegenPerSecondRate + permanentManaRegenRateBonus + synergyBonus.manaRegenRateBonus + tileManaRegenRateBonus);
				if (!(manaRegenRate <= 0f))
				{
					currentMana = Mathf.Min(MaxMana, currentMana + MaxMana * manaRegenRate * Time.deltaTime);
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
			alternatingDormant = IsAlternatingRoundBurstUnit();
			alternatingLastRound = -1;
			ClearDormantRestEffect();
			EnsureDormantRestEffect();
			BindAlternatingRoundController();
			ClearOwnedSupportEffects();
			if (!isTemporarySummon && OutgameProgressionSystem.Active != null)
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
			IReadOnlyList<MonsterUnit> activeMonsters = MonsterUnit.ActiveInstances;
			for (int i = 0; i < activeMonsters.Count; i++)
			{
				MonsterUnit monster = activeMonsters[i];
				if (monster != null)
				{
					monsters.Add(monster);
				}
			}
			InvalidateBasicTargetCache();
			skillCooldowns.Clear();
			base.gameObject.name = (isTemporarySummon ? definition.displayName : (definition.displayName + "_" + definition.grade));
			ApplyVisuals();
			EnsureAnimationDriver();
			UnitAnimatorLodController.AttachOrRefresh(base.gameObject, animationDriver, defender: true, boss: false);
			EnsureHitFlashFeedback();
			EnsureInteractionCollider();
			floatingUi = FloatingCombatUI.Attach(base.transform, definition.displayName, definition.accentColor, definition.grade, GetFloatingUiFallbackHeight());
			floatingUi.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
			if (!isTemporarySummon && currentSlot != null)
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
			float height = 1.46f;
			if (definition.grade == CharacterGrade.Legendary)
			{
				height = 1.54f;
			}
			else if (definition.grade == CharacterGrade.Mythic)
			{
				height = 1.62f;
			}
			else if (definition.grade == CharacterGrade.Transcendent)
			{
				height = 1.72f;
			}
			if (definition.role == CharacterRole.Vanguard || definition.role == CharacterRole.Summoner)
			{
				height += 0.08f;
			}
			return height;
		}

		public void SetSlot(BoardSlot slot)
		{
			currentSlot = slot;
			defaultFacingRotation = base.transform.rotation;
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

		public void DismissTemporarySummon()
		{
			if (isTemporarySummon && !isDying)
			{
				isDying = true;
				ClearPendingImpacts();
				ClearOwnedSupportEffects();
				RemoveFromBoard();
				DefenderUnit.OnDefenderRemoved?.Invoke(this);
				UnityEngine.Object.Destroy(base.gameObject);
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
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}

		public void TakeDamage(float damage, bool critical)
		{
			TakeDamage(damage, critical, null);
		}

		public void TakeDamage(float damage, bool critical, MonsterUnit source)
		{
			float damageReduction = Mathf.Clamp01(permanentDamageReductionBonus + synergyBonus.damageReductionBonus + temporaryDamageReductionBonus + tileDamageReductionBonus);
			float finalDamage = damage * (1f - damageReduction);
			float blockedDamage = 0f;
			bool shieldBroken = false;
			if (currentShield > 0f)
			{
				blockedDamage = Mathf.Min(currentShield, finalDamage);
				currentShield -= blockedDamage;
				finalDamage -= blockedDamage;
				if (currentShield <= 0f)
				{
					currentShield = 0f;
					shieldTimer = 0f;
					shieldBroken = true;
					ClearShieldEffect();
				}
			}
			if (blockedDamage > 0f)
			{
				DefenderUnit.OnShieldResolved?.Invoke(this, blockedDamage, shieldBroken, source);
			}
			currentHealth -= finalDamage;
			if (finalDamage > 0f)
			{
				DefenderUnit.OnDamageTaken?.Invoke(this, source, finalDamage);
			}
			if (IsCombatActive())
			{
				float manaGainRate = Mathf.Clamp01(definition.stats.manaGainWhenHitRate + synergyBonus.manaGainWhenHitRateBonus);
				currentMana = Mathf.Min(MaxMana, currentMana + MaxMana * manaGainRate);
			}
			hitFlashFeedback?.PlayHit(critical);
			RuntimeAudioUtility.PlayHit();
			if (finalDamage > 0f)
			{
				floatingUi?.ShowDamage(finalDamage, critical, healing: false);
			}
			else if (blockedDamage > 0f)
			{
				floatingUi?.ShowDamage(blockedDamage, critical: false, healing: true);
			}
			floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
			if (source != null && thornsTimer > 0f && thornsReturnRatio > 0f && finalDamage > 0f)
			{
				source.TakeDamage(finalDamage * thornsReturnRatio, critical: false, this);
			}
			if (currentHealth <= 0f)
			{
				Die();
			}
		}

		public void Heal(float amount, GameObject effectPrefab = null)
		{
			float previousHealth = currentHealth;
			currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
			float healed = currentHealth - previousHealth;
			if (!(healed <= 0f))
			{
				floatingUi?.ShowDamage(healed, critical: false, healing: true);
				ShowInstantSupportFeedback("회복", HealFeedbackColor, effectPrefab, 1.1f);
				floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
			}
		}

		public void AddShield(float amount, float duration, GameObject effectPrefab = null)
		{
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
			if (!(MaxMana <= 0f))
			{
				float drained = Mathf.Min(currentMana, MaxMana * Mathf.Clamp01(ratio));
				if (!(drained <= 0f))
				{
					currentMana = Mathf.Max(0f, currentMana - drained);
					hitFlashFeedback?.PlayHit(critical: false);
					ShowInstantSupportFeedback("마나 -", ManaFeedbackColor, null, 0.85f);
					floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
				}
			}
		}

		public void RestoreMana(float ratio, GameObject effectPrefab = null)
		{
			if (IsAlternatingRoundBurstUnit() && alternatingDormant)
			{
				return;
			}

			if (!(MaxMana <= 0f) && !(ratio <= 0f))
			{
				float previousMana = currentMana;
				currentMana = Mathf.Min(MaxMana, currentMana + MaxMana * Mathf.Clamp01(ratio));
				if (!(currentMana <= previousMana))
				{
					hitFlashFeedback?.PlayHit(critical: false);
					ShowInstantSupportFeedback("마나 +", ManaFeedbackColor, effectPrefab, 1f);
					floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
				}
			}
		}

		public void ApplyStun(float duration)
		{
			stunTimer = Mathf.Max(stunTimer, duration);
			attackCooldown = Mathf.Max(attackCooldown, Mathf.Min(duration, 1.2f));
			hitFlashFeedback?.PlayHit(critical: true);
			ShowTimedSupportFeedback("기절 · 행동 불가", DebuffFeedbackColor, duration, null);
			RuntimeCombatFeedback.ShowGroundPulse(base.transform.position, DebuffFeedbackColor, 0.62f, 0.72f, 0.1f);
		}

		public void ApplyMergeInheritance(float inheritedAttackPower, float inheritedMaxHealth)
		{
			if (definition != null)
			{
				float currentAttackPower = Mathf.Max(0.01f, GetEffectiveAttackPower());
				float attackRatio = Mathf.Max(0f, inheritedAttackPower / currentAttackPower - 1f);
				if (attackRatio > 0f)
				{
					AddAttackPowerBonus(attackRatio);
				}
				float currentMaximumHealth = Mathf.Max(0.01f, MaxHealth);
				float healthRatio = Mathf.Max(0f, inheritedMaxHealth / currentMaximumHealth - 1f);
				if (healthRatio > 0f)
				{
					AddMaxHealthBonus(healthRatio);
				}
				floatingUi?.ShowStatus("합성 능력 계승", new Color(1f, 0.86f, 0.3f, 1f), 1.25f);
			}
		}

		public void KillByBossSkill()
		{
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
			float previousMaxHealth = MaxHealth;
			permanentMaxHealthBonus += ratioBonus;
			float addedHealth = Mathf.Max(0f, MaxHealth - previousMaxHealth);
			currentHealth = Mathf.Min(MaxHealth, currentHealth + addedHealth);
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
			float previousMaxHealth = MaxHealth;
			tileAttackPowerBonus = attackPowerRatio;
			tileAttackSpeedBonus = attackSpeedRatio;
			tileManaRegenRateBonus = manaRegenRate;
			tileMaxHealthBonus = maxHealthRatio;
			tileSkillPowerBonus = skillPowerRatio;
			tileBossDamageBonus = bossDamageRatio;
			tileAttackRangeBonus = attackRangeFlat;
			tileDamageReductionBonus = damageReductionRatio;
			tileLifeStealRatio = Mathf.Max(0f, lifeStealRatio);
			float nextMaxHealth = MaxHealth;
			if (definition != null)
			{
				if (previousMaxHealth <= 0f)
				{
					currentHealth = nextMaxHealth;
				}
				else
				{
					currentHealth = Mathf.Clamp(currentHealth + nextMaxHealth - previousMaxHealth, 0f, nextMaxHealth);
				}
				floatingUi?.SetValues(currentHealth, nextMaxHealth, currentMana, MaxMana);
			}
			if (!string.IsNullOrWhiteSpace(statusLabel))
			{
				ShowInstantSupportFeedback(statusLabel, statusColor ?? BuffFeedbackColor, null, 0.8f);
			}
		}

		public void ClearBoardTileBonuses()
		{
			SetBoardTileBonuses(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
		}

		public void ActivateTimedCombatBoost(float attackPowerRatioBonus, float attackSpeedRatioBonus, float duration, GameObject effectPrefab = null, string statusLabel = null, Color? statusColor = null)
		{
			temporaryAttackPowerBonus = Mathf.Max(temporaryAttackPowerBonus, attackPowerRatioBonus);
			temporaryAttackSpeedBonus = Mathf.Max(temporaryAttackSpeedBonus, attackSpeedRatioBonus);
			temporaryCombatBoostTimer = Mathf.Max(temporaryCombatBoostTimer, duration);
			hitFlashFeedback?.PlayHit(critical: false);
			if (duration > 0f)
			{
				string resolvedLabel = ((!string.IsNullOrWhiteSpace(statusLabel)) ? statusLabel : ((attackSpeedRatioBonus > 0f) ? ("공속 +" + Mathf.RoundToInt(attackSpeedRatioBonus * 100f) + "%") : "전투 강화"));
				Color resolvedColor = statusColor ?? BuffFeedbackColor;
				ShowTimedSupportFeedback(resolvedLabel, resolvedColor, duration, effectPrefab);
			}
		}

		public void ApplyAttackPowerReduction(float reductionRatio, float duration, GameObject effectPrefab = null)
		{
			float safeRatio = Mathf.Clamp01(reductionRatio);
			float safeDuration = Mathf.Max(0f, duration);
			if (!(safeRatio <= 0f) && !(safeDuration <= 0f))
			{
				temporaryAttackPowerReduction = Mathf.Max(temporaryAttackPowerReduction, safeRatio);
				temporaryAttackPowerReductionTimer = Mathf.Max(temporaryAttackPowerReductionTimer, safeDuration);
				hitFlashFeedback?.PlayHit(critical: false);
				ShowTimedSupportFeedback("공격력 -" + Mathf.RoundToInt(safeRatio * 100f) + "%", DebuffFeedbackColor, safeDuration, effectPrefab);
			}
		}

		public void ActivateTimedDamageReduction(float reductionRatio, float duration, GameObject effectPrefab = null, string statusLabel = null, Color? statusColor = null)
		{
			ApplyTemporaryDamageReduction(reductionRatio, duration);
			if (duration > 0f)
			{
				ShowTimedSupportFeedback((!string.IsNullOrWhiteSpace(statusLabel)) ? statusLabel : "피해 감소", statusColor ?? ShieldFeedbackColor, duration, effectPrefab);
			}
		}

		public void SetSynergyBonuses(UnitSynergyBonus bonus)
		{
			float previousMaxHealth = MaxHealth;
			synergyBonus = bonus;
			float nextMaxHealth = MaxHealth;
			if (definition != null)
			{
				if (previousMaxHealth <= 0f)
				{
					currentHealth = nextMaxHealth;
				}
				else
				{
					currentHealth = Mathf.Clamp(currentHealth + nextMaxHealth - previousMaxHealth, 0f, nextMaxHealth);
				}
				floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
			}
		}

		public void ResetFacingToDefault()
		{
			if (hasDefaultFacing)
			{
				base.transform.rotation = defaultFacingRotation;
			}
		}

		private void PerformAttack(MonsterUnit target)
		{
			if (!(target == null))
			{
				FaceTarget(target.transform.position);
				float effectiveAttackSpeed = Mathf.Max(0.2f, definition.stats.attackSpeed * (1f + attackSpeedBonus + permanentAttackSpeedBonus + synergyBonus.attackSpeedBonus + temporaryAttackSpeedBonus + tileAttackSpeedBonus));
				attackCooldown = 1f / effectiveAttackSpeed;
				bool critical = UnityEngine.Random.value <= GetEffectiveCriticalChance();
				float damage = CalculateDamageAgainst(target, 1f, critical);
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
			if (!pendingBasicAttack.isValid)
			{
				return;
			}
			PendingBasicAttack pending = pendingBasicAttack;
			pendingBasicAttack = default(PendingBasicAttack);
			CancelPendingAttackFallback();
			if (pending.target == null || !pending.target.CanBeCombatTargeted)
			{
				return;
			}
			FaceTarget(pending.target.transform.position);
			GainManaFromBasicAttack();
			if (pending.critical)
			{
				RuntimeCameraShake.Request(0.025f, 0.08f);
			}
			RuntimeAudioUtility.PlayAttack();
			if (definition.attackBehavior != null && definition.attackBehavior.IsMelee)
			{
				Quaternion hitRotation = RuntimeEffectUtility.FaceTowards(base.transform.position, pending.target.transform.position, base.transform.rotation);
				RuntimeEffectUtility.PlayOneShot(ResolveBasicHitEffectPrefab(), pending.target.transform.position, hitRotation);
				RuntimeAudioUtility.PlayHit();
				pending.target.TakeDamage(pending.damage, pending.critical, this);
				ApplyBasicAttackSplash(pending.target, pending.damage);
				return;
			}
			GameObject attackProjectilePrefab = ResolveBasicAttackProjectilePrefab();
			if (attackProjectilePrefab == null)
			{
				Quaternion hitRotation2 = RuntimeEffectUtility.FaceTowards(base.transform.position, pending.target.transform.position, base.transform.rotation);
				RuntimeEffectUtility.PlayOneShot(ResolveBasicHitEffectPrefab(), pending.target.transform.position, hitRotation2);
				RuntimeAudioUtility.PlayHit();
				pending.target.TakeDamage(pending.damage, pending.critical, this);
				ApplyBasicAttackSplash(pending.target, pending.damage);
				return;
			}
			Transform launchPoint = ((firePoint != null) ? firePoint : base.transform);
			Quaternion launchRotation = RuntimeEffectUtility.FaceTowards(launchPoint.position, pending.target.transform.position, launchPoint.rotation);
			RuntimeEffectUtility.PlayOneShot(ResolveBasicMuzzleEffectPrefab(), launchPoint.position, launchRotation);
			Projectile projectile = InstantiateProjectile(attackProjectilePrefab, launchPoint.position, launchRotation);
			if (projectile == null)
			{
				RuntimeEffectUtility.PlayOneShot(ResolveBasicHitEffectPrefab(), pending.target.transform.position, launchRotation);
				RuntimeAudioUtility.PlayHit();
				pending.target.TakeDamage(pending.damage, pending.critical, this);
				ApplyBasicAttackSplash(pending.target, pending.damage);
			}
			else
			{
				projectile.Initialize(pending.target, pending.damage, definition.stats.projectileSpeed, pending.critical, GetBasicAttackSplashRadius(), GetBasicAttackSplashDamageRatio(), (definition.attackBehavior != null) ? definition.attackBehavior.additionalPierceCount : 0, this, null, ResolveBasicHitEffectPrefab());
			}
		}

		private GameObject ResolveBasicAttackProjectilePrefab()
		{
			GameObject overridePrefab = ((definition != null && definition.attackBehavior != null) ? definition.attackBehavior.projectilePrefabOverride : null);
			if (overridePrefab != null)
			{
				return overridePrefab;
			}
			return (projectilePrefab != null) ? projectilePrefab.gameObject : null;
		}

		private Projectile InstantiateProjectile(GameObject projectileSource, Vector3 position, Quaternion rotation)
		{
			return Projectile.Spawn(projectileSource, position, rotation);
		}

		private GameObject ResolveBasicMuzzleEffectPrefab()
		{
			if (definition != null && definition.attackBehavior != null && definition.attackBehavior.muzzleEffectPrefab != null)
			{
				return definition.attackBehavior.muzzleEffectPrefab;
			}
			return defaultMuzzleEffectPrefab;
		}

		private GameObject ResolveBasicHitEffectPrefab()
		{
			if (definition != null && definition.attackBehavior != null && definition.attackBehavior.hitEffectPrefab != null)
			{
				return definition.attackBehavior.hitEffectPrefab;
			}
			return defaultHitEffectPrefab;
		}

		private void GainManaFromBasicAttack()
		{
			float manaGainRate = Mathf.Clamp01(definition.stats.manaGainPerAttackRate + synergyBonus.manaGainPerAttackRateBonus);
			if (!(MaxMana <= 0f) && !(manaGainRate <= 0f))
			{
				currentMana = Mathf.Min(MaxMana, currentMana + MaxMana * manaGainRate);
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
			if (pendingSkillCast.isValid)
			{
				PendingSkillCast pending = pendingSkillCast;
				pendingSkillCast = default(PendingSkillCast);
				CancelPendingSkillFallback();
				if (pending.skill != null && pending.skill.effectType == SkillEffectType.HealLowestAllies && FindLowestHealthAllies(GetSkillHitCount(pending.skill)).Count == 0)
				{
					currentMana = MaxMana;
					skillCooldowns[pending.skill.id] = 0f;
					floatingUi?.SetValues(currentHealth, MaxHealth, currentMana, MaxMana);
					animationDriver?.ForceIdle();
				}
				else
				{
					ApplySkillEffect(pending.skill, pending.target, pending.skillMultiplier);
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
				SkillDefinition skill = definition.skills[i];
				MonsterUnit skillTarget = FindNearestSkillTarget(skill);
				if (CanCastSkill(skill, skillTarget) && (!skillCooldowns.TryGetValue(skill.id, out var cooldown) || !(cooldown > 0f)))
				{
					currentMana = 0f;
					CastSkill(skill, skillTarget);
					skillCooldowns[skill.id] = skill.cooldown;
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
				return currentTarget != null;
			case SkillEffectType.ShieldAlly:
				return FindLowestHealthAlly() != null;
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
				return currentTarget != null;
			case SkillEffectType.GroundAreaDamage:
				return currentTarget != null;
			case SkillEffectType.HealSelf:
				return currentHealth < MaxHealth * 0.98f;
			case SkillEffectType.Transform:
				return true;
			default:
				return currentTarget != null;
			}
		}

		private bool HasMonsterInSkillCastRange(SkillDefinition skill)
		{
			if (skill == null)
			{
				return false;
			}
			return FindNearestTarget(GetEffectiveSkillRange(skill)) != null;
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
			float animationHoldDuration = Mathf.Max(0f, GetSkillDuration(skill));
			if (animationDriver != null && animationDriver.PlaySkill(skillSlot, animationHoldDuration))
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
			if (skill == null)
			{
				return;
			}
			currentTarget = ResolveCombatTarget(currentTarget, skill);
			DefenderUnit.OnSkillCast?.Invoke(this, skill, currentTarget);
			SkillDefinition previousDamageSkillContext = CurrentDamageSkillContext;
			CurrentDamageSkillContext = skill;
			try
			{
				SkillDeliveryType deliveryType = SkillDefinitionUtility.ResolveDeliveryType(skill);
				if (deliveryType == SkillDeliveryType.Projectile && TryLaunchSkillProjectile(skill, currentTarget, skillMultiplier))
				{
					return;
				}
				float power = GetSkillPower(skill);
				float secondaryPower = GetSkillSecondaryPower(skill);
				float durationValue = GetSkillDuration(skill);
				float radius = GetSkillRadius(skill);
				int hitCount = GetSkillHitCount(skill);
				if (skill.effectType == SkillEffectType.DirectDamage)
				{
					MonsterUnit target = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
					if (target != null)
					{
						FaceTarget(target.transform.position);
						PlaySkillHitEffect(skill, target);
						target.TakeDamage(CalculateDamageAgainst(target, power * skillMultiplier, critical: false), critical: false, this);
					}
				}
				else if (skill.effectType == SkillEffectType.FrontKnockbackGuard)
				{
					MonsterUnit target2 = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
					if (target2 != null)
					{
						FaceTarget(target2.transform.position);
						PlaySkillHitEffect(skill, target2);
						target2.TakeDamage(CalculateDamageAgainst(target2, power * skillMultiplier, critical: false), critical: false, this);
						target2.ApplyKnockback(radius, base.transform.position);
					}
					permanentDamageReductionBonus = Mathf.Min(0.4f, permanentDamageReductionBonus + Mathf.Max(0f, secondaryPower));
					ShowInstantSupportFeedback("방어력 +" + Mathf.RoundToInt(secondaryPower * 100f) + "% / 누적 " + Mathf.RoundToInt(permanentDamageReductionBonus * 100f) + "%", ShieldFeedbackColor, skill.areaEffectPrefab, 0.9f);
				}
				else if (skill.effectType == SkillEffectType.AreaDamage)
				{
					Vector3 center = ((currentTarget != null) ? currentTarget.transform.position : base.transform.position);
					PlaySkillAreaEffect(skill, center);
					ApplyAreaDamageAt(center, radius, power * skillMultiplier, critical: false, skill);
				}
				else if (skill.effectType == SkillEffectType.HealthDrainPercent)
				{
					MonsterUnit target3 = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
					if (target3 != null)
					{
						FaceTarget(target3.transform.position);
						PlaySkillHitEffect(skill, target3);
						float damage = Mathf.Max(0f, target3.CurrentHealth * power * skillMultiplier);
						target3.TakeDamage(damage, critical: false, this);
						Heal(damage, skill.areaEffectPrefab);
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
					MonsterUnit target4 = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
					if (target4 != null)
					{
						FaceTarget(target4.transform.position);
						PlaySkillHitEffect(skill, target4);
						target4.ApplyPoison(power * skillMultiplier, durationValue, Mathf.Max(0.2f, secondaryPower), this);
					}
				}
				else if (skill.effectType == SkillEffectType.HealSelf)
				{
					PlaySkillCasterEffect(skill);
					Heal(MaxHealth * power * skillMultiplier, skill.areaEffectPrefab);
				}
				else if (skill.effectType == SkillEffectType.AttackSpeedBoost)
				{
					PlaySkillCasterEffect(skill);
					attackSpeedBonus = power;
					attackSpeedBuffTimer = durationValue;
					ShowTimedSupportFeedback("공속 +" + Mathf.RoundToInt(power * 100f) + "%", AttackSpeedFeedbackColor, durationValue, skill.areaEffectPrefab);
				}
				else if (skill.effectType == SkillEffectType.AllyAttackSpeedBoost)
				{
					PlaySkillCasterEffect(skill);
					ApplyAllyAttackSpeedBoost(power * skillMultiplier, durationValue, radius, skill.areaEffectPrefab);
				}
				else if (skill.effectType == SkillEffectType.CriticalBoost)
				{
					PlaySkillCasterEffect(skill);
					critChanceBonus = power;
					critBuffTimer = durationValue;
					ShowTimedSupportFeedback("치명 +" + Mathf.RoundToInt(power * 100f) + "%", BuffFeedbackColor, durationValue, skill.areaEffectPrefab);
				}
				else if (skill.effectType == SkillEffectType.ManaSurge)
				{
					PlaySkillCasterEffect(skill);
					RestoreMana(power * skillMultiplier, skill.areaEffectPrefab);
				}
				else if (skill.effectType == SkillEffectType.ManaRestoreAdjacent)
				{
					PlaySkillCasterEffect(skill);
					List<DefenderUnit> allies = FindAdjacentManaTargets(radius);
					for (int i = 0; i < allies.Count; i++)
					{
						allies[i].RestoreMana(power * skillMultiplier, skill.areaEffectPrefab);
					}
				}
				else if (skill.effectType == SkillEffectType.MultiShot)
				{
					List<MonsterUnit> targets = GetNearestTargets(hitCount, GetEffectiveSkillRange(skill));
					for (int j = 0; j < targets.Count; j++)
					{
						PlaySkillHitEffect(skill, targets[j]);
						targets[j].TakeDamage(CalculateDamageAgainst(targets[j], power * skillMultiplier, critical: false), critical: false, this);
					}
				}
				else if (skill.effectType == SkillEffectType.RandomMultiShot)
				{
					List<MonsterUnit> targets2 = GetRandomTargetsWithReplacement(hitCount, GetEffectiveSkillRange(skill));
					for (int k = 0; k < targets2.Count; k++)
					{
						PlaySkillHitEffect(skill, targets2[k]);
						targets2[k].TakeDamage(CalculateDamageAgainst(targets2[k], power * skillMultiplier, critical: false), critical: false, this);
					}
				}
				else if (skill.effectType == SkillEffectType.Execute)
				{
					MonsterUnit target5 = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
					if (target5 != null)
					{
						FaceTarget(target5.transform.position);
						PlaySkillHitEffect(skill, target5);
						float multiplier = ((target5.CurrentHealth <= target5.MaxHealth * 0.35f) ? (power * 1.8f) : power);
						multiplier *= skillMultiplier;
						target5.TakeDamage(CalculateDamageAgainst(target5, multiplier, critical: true), critical: true, this);
					}
				}
				else if (skill.effectType == SkillEffectType.SummonRush)
				{
					SpawnSummonedAllies(skill, currentTarget, skillMultiplier);
				}
				else if (skill.effectType == SkillEffectType.ShieldBreak)
				{
					MonsterUnit target6 = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
					if (target6 != null)
					{
						FaceTarget(target6.transform.position);
						PlaySkillHitEffect(skill, target6);
						target6.TakeDamage(CalculateDamageAgainst(target6, power * skillMultiplier, critical: false), critical: false, this);
					}
				}
				else if (skill.effectType == SkillEffectType.DamageStun)
				{
					MonsterUnit target7 = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
					if (target7 != null)
					{
						FaceTarget(target7.transform.position);
						PlaySkillHitEffect(skill, target7);
						target7.TakeDamage(CalculateDamageAgainst(target7, power * skillMultiplier, critical: false), critical: false, this);
						target7.ApplyStun(durationValue);
					}
				}
				else if (skill.effectType == SkillEffectType.PercentHealthDamage)
				{
					MonsterUnit target8 = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
					if (target8 != null)
					{
						FaceTarget(target8.transform.position);
						PlaySkillHitEffect(skill, target8);
						float damage2 = Mathf.Max(0f, target8.CurrentHealth * power * skillMultiplier);
						target8.TakeDamage(damage2, critical: false, this);
					}
				}
				else if (skill.effectType == SkillEffectType.HealLowestAllies)
				{
					List<DefenderUnit> allies2 = FindLowestHealthAllies(hitCount);
					if (allies2.Count != 0)
					{
						PlaySkillCasterEffect(skill);
						for (int l = 0; l < allies2.Count; l++)
						{
							allies2[l].Heal(allies2[l].MaxHealth * power * skillMultiplier, skill.areaEffectPrefab);
						}
					}
				}
				else if (skill.effectType == SkillEffectType.DamageSlow)
				{
					MonsterUnit target9 = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
					if (target9 != null)
					{
						FaceTarget(target9.transform.position);
						PlaySkillHitEffect(skill, target9);
						target9.TakeDamage(CalculateDamageAgainst(target9, power * skillMultiplier, critical: false), critical: false, this);
						target9.ApplySlow(Mathf.Clamp01(secondaryPower), durationValue);
						target9.ApplyAttackSpeedSlow(Mathf.Clamp01(secondaryPower), durationValue);
					}
				}
				else if (skill.effectType == SkillEffectType.Slow)
				{
					MonsterUnit target10 = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
					if (target10 != null)
					{
						FaceTarget(target10.transform.position);
						PlaySkillHitEffect(skill, target10);
						target10.ApplySlow(Mathf.Clamp01(power), durationValue);
					}
				}
				else if (skill.effectType == SkillEffectType.Stun)
				{
					MonsterUnit target11 = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
					if (target11 != null)
					{
						FaceTarget(target11.transform.position);
						PlaySkillHitEffect(skill, target11);
						target11.ApplyStun(durationValue);
					}
				}
				else if (skill.effectType == SkillEffectType.ShieldAlly)
				{
					DefenderUnit ally = FindLowestHealthAlly();
					if (ally != null)
					{
						PlaySupportSkillCastFeedback();
						ally.AddShield(ally.MaxHealth * power * skillMultiplier, durationValue, skill.areaEffectPrefab);
					}
				}
				else if (skill.effectType == SkillEffectType.LifeSteal)
				{
					MonsterUnit target12 = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
					if (target12 != null)
					{
						FaceTarget(target12.transform.position);
						PlaySkillHitEffect(skill, target12);
						float damage3 = CalculateDamageAgainst(target12, power * skillMultiplier, critical: false);
						target12.TakeDamage(damage3, critical: false, this);
						Heal(damage3 * Mathf.Max(0.05f, secondaryPower), skill.areaEffectPrefab);
					}
				}
				else if (skill.effectType == SkillEffectType.GroundAreaDamage)
				{
					MonsterUnit target13 = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
					Vector3 center2 = ((target13 != null) ? target13.transform.position : base.transform.position);
					PlaySkillAreaEffect(skill, center2, durationValue);
					StartCoroutine(GroundDamageRoutine(center2, radius, definition.stats.attackPower * power * skillMultiplier, durationValue, Mathf.Max(0.2f, secondaryPower), skill));
				}
				else if (skill.effectType == SkillEffectType.Poison)
				{
					MonsterUnit target14 = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
					if (target14 != null)
					{
						FaceTarget(target14.transform.position);
						PlaySkillHitEffect(skill, target14);
						target14.ApplyPoison(definition.stats.attackPower * power * skillMultiplier, durationValue, Mathf.Max(0.25f, secondaryPower), this);
					}
				}
				else if (skill.effectType == SkillEffectType.DefenseBuff)
				{
					PlaySupportSkillCastFeedback();
					ApplyDefenseBuff(power * skillMultiplier, durationValue, radius, skill.areaEffectPrefab);
				}
				else if (skill.effectType == SkillEffectType.Taunt)
				{
					float duration = Mathf.Max(0.1f, durationValue * skillMultiplier);
					PlayAttachedSupportEffect(ResolveSkillAreaEffectPrefab(skill), duration);
					ApplyTaunt(radius, duration);
					ApplyTemporaryDamageReduction(secondaryPower, duration);
				}
				else if (skill.effectType == SkillEffectType.ThornsAura)
				{
					PlaySkillCasterEffect(skill);
					ApplyThornsAura(power * skillMultiplier, durationValue);
					ShowTimedSupportFeedback("쏜즈", BuffFeedbackColor, durationValue, skill.areaEffectPrefab);
				}
				else if (skill.effectType == SkillEffectType.Transform)
				{
					PlaySkillCasterEffect(skill);
					ActivateTimedCombatBoost(power * skillMultiplier, secondaryPower * skillMultiplier, durationValue, skill.areaEffectPrefab, "전투 강화", BuffFeedbackColor);
					AddShield(MaxHealth * secondaryPower * 0.5f * skillMultiplier, durationValue, skill.areaEffectPrefab);
				}
			}
			finally
			{
				CurrentDamageSkillContext = previousDamageSkillContext;
			}
		}

		private bool TryLaunchSkillProjectile(SkillDefinition skill, MonsterUnit currentTarget, float skillMultiplier)
		{
			GameObject skillProjectilePrefab = ResolveSkillProjectilePrefab(skill);
			if (skillProjectilePrefab == null || !CanDeliverSkillByProjectile(skill))
			{
				return false;
			}
			List<MonsterUnit> targets = ResolveSkillProjectileTargets(skill, currentTarget);
			if (targets.Count == 0)
			{
				return false;
			}
			for (int i = 0; i < targets.Count; i++)
			{
				MonsterUnit target = targets[i];
				if (!(target == null))
				{
					LaunchSkillProjectile(target, skill, skillMultiplier, skillProjectilePrefab);
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
			int count = ((skill == null || (skill.effectType != SkillEffectType.MultiShot && skill.effectType != SkillEffectType.RandomMultiShot && skill.effectType != SkillEffectType.SummonRush)) ? 1 : GetSkillHitCount(skill));
			if (count > 1)
			{
				if (skill.effectType == SkillEffectType.RandomMultiShot)
				{
					return GetRandomTargetsWithReplacement(count, GetEffectiveSkillRange(skill));
				}
				return GetNearestTargets(count, GetEffectiveSkillRange(skill));
			}
			List<MonsterUnit> targets = new List<MonsterUnit>(1);
			MonsterUnit target = ResolveCombatTarget(currentTarget, skill);
			if (target != null)
			{
				targets.Add(target);
			}
			return targets;
		}

		private void LaunchSkillProjectile(MonsterUnit target, SkillDefinition skill, float skillMultiplier, GameObject skillProjectilePrefab)
		{
			Transform launchPoint = ((firePoint != null) ? firePoint : base.transform);
			Quaternion launchRotation = RuntimeEffectUtility.FaceTowards(launchPoint.position, target.transform.position, launchPoint.rotation);
			RuntimeEffectUtility.PlayOneShot(ResolveSkillMuzzleEffectPrefab(skill), launchPoint.position, launchRotation);
			Projectile projectile = InstantiateProjectile(skillProjectilePrefab, launchPoint.position, launchRotation);
			if (!(projectile == null))
			{
				projectile.Initialize(target, 0f, Mathf.Max(0.1f, definition.stats.projectileSpeed), isCritical: false, 0f, 0f, 0, this, delegate(MonsterUnit hitTarget)
				{
					ApplyProjectileSkillImpact(skill, hitTarget, skillMultiplier);
				}, ResolveSkillHitEffectPrefab(skill));
			}
		}

		private GameObject ResolveSkillProjectilePrefab(SkillDefinition skill)
		{
			if (skill != null && skill.projectilePrefab != null)
			{
				return skill.projectilePrefab;
			}
			return (projectilePrefab != null) ? projectilePrefab.gameObject : null;
		}

		private GameObject ResolveSkillMuzzleEffectPrefab(SkillDefinition skill)
		{
			if (skill != null && skill.muzzleEffectPrefab != null)
			{
				return skill.muzzleEffectPrefab;
			}
			return defaultMuzzleEffectPrefab;
		}

		private GameObject ResolveSkillHitEffectPrefab(SkillDefinition skill)
		{
			if (skill != null && skill.hitEffectPrefab != null)
			{
				return skill.hitEffectPrefab;
			}
			return defaultHitEffectPrefab;
		}

		private GameObject ResolveSkillAreaEffectPrefab(SkillDefinition skill)
		{
			if (skill != null && skill.areaEffectPrefab != null)
			{
				return skill.areaEffectPrefab;
			}
			return defaultAreaEffectPrefab;
		}

		private void PlaySkillHitEffect(SkillDefinition skill, MonsterUnit target)
		{
			if (!(target == null))
			{
				Quaternion effectRotation = RuntimeEffectUtility.FaceTowards(base.transform.position, target.transform.position, base.transform.rotation);
				RuntimeEffectUtility.PlayOneShot(ResolveSkillHitEffectPrefab(skill), target.transform.position, effectRotation);
				RuntimeCombatFeedback.ShowGroundPulse(target.transform.position, (definition != null) ? definition.accentColor : Color.white, 0.36f, 0.34f);
				RuntimeAudioUtility.PlayHit();
			}
		}

		private void PlaySkillCasterEffect(SkillDefinition skill)
		{
			MonsterUnit target = FindNearestSkillTarget(skill);
			Quaternion rotation = ((target != null) ? RuntimeEffectUtility.FaceTowards(base.transform.position, target.transform.position, base.transform.rotation) : base.transform.rotation);
			RuntimeEffectUtility.PlayOneShot(ResolveSkillMuzzleEffectPrefab(skill), base.transform.position, rotation);
			RuntimeCombatFeedback.ShowGroundPulse(base.transform.position, (definition != null) ? definition.accentColor : Color.white, 0.44f, 0.32f, 0.06f);
			RuntimeAudioUtility.PlayAttack();
		}

		private void PlaySupportSkillCastFeedback()
		{
			RuntimeCombatFeedback.ShowGroundPulse(base.transform.position, (definition != null) ? definition.accentColor : ShieldFeedbackColor, 0.44f, 0.32f, 0.06f);
			RuntimeAudioUtility.PlayAttack();
		}

		private void PlaySkillAreaEffect(SkillDefinition skill, Vector3 center, float minimumLifetime = 0f)
		{
			Quaternion effectRotation = RuntimeEffectUtility.FaceTowards(base.transform.position, center, base.transform.rotation);
			RuntimeEffectUtility.PlayOneShot(ResolveSkillAreaEffectPrefab(skill), center, effectRotation, minimumLifetime);
		}

		private void ApplyProjectileSkillImpact(SkillDefinition skill, MonsterUnit hitTarget, float skillMultiplier)
		{
			if (this == null || !base.isActiveAndEnabled || definition == null || skill == null || hitTarget == null || !hitTarget.CanBeCombatTargeted)
			{
				return;
			}
			SkillDefinition previousDamageSkillContext = CurrentDamageSkillContext;
			CurrentDamageSkillContext = skill;
			try
			{
				float power = GetSkillPower(skill);
				float secondaryPower = GetSkillSecondaryPower(skill);
				float durationValue = GetSkillDuration(skill);
				float radius = GetSkillRadius(skill);
				FaceTarget(hitTarget.transform.position);
				if (skill.effectType == SkillEffectType.DirectDamage || skill.effectType == SkillEffectType.ShieldBreak || skill.effectType == SkillEffectType.MultiShot || skill.effectType == SkillEffectType.RandomMultiShot)
				{
					hitTarget.TakeDamage(CalculateDamageAgainst(hitTarget, power * skillMultiplier, critical: false), critical: false, this);
				}
				else if (skill.effectType == SkillEffectType.AreaDamage)
				{
					PlaySkillAreaEffect(skill, hitTarget.transform.position);
					ApplyAreaDamageAt(hitTarget.transform.position, radius, power * skillMultiplier, critical: false, skill);
				}
				else if (skill.effectType == SkillEffectType.Execute)
				{
					float multiplier = ((hitTarget.CurrentHealth <= hitTarget.MaxHealth * 0.35f) ? (power * 1.8f) : power);
					hitTarget.TakeDamage(CalculateDamageAgainst(hitTarget, multiplier * skillMultiplier, critical: true), critical: true, this);
				}
				else if (skill.effectType == SkillEffectType.Slow)
				{
					hitTarget.ApplySlow(Mathf.Clamp01(power), durationValue);
				}
				else if (skill.effectType == SkillEffectType.Stun)
				{
					hitTarget.ApplyStun(durationValue);
				}
				else if (skill.effectType == SkillEffectType.LifeSteal)
				{
					float damage = CalculateDamageAgainst(hitTarget, power * skillMultiplier, critical: false);
					hitTarget.TakeDamage(damage, critical: false, this);
					Heal(damage * Mathf.Max(0.05f, secondaryPower), skill.areaEffectPrefab);
				}
				else if (skill.effectType == SkillEffectType.GroundAreaDamage)
				{
					PlaySkillAreaEffect(skill, hitTarget.transform.position, durationValue);
					StartCoroutine(GroundDamageRoutine(hitTarget.transform.position, radius, definition.stats.attackPower * power * skillMultiplier, durationValue, Mathf.Max(0.2f, secondaryPower), skill));
				}
				else if (skill.effectType == SkillEffectType.Poison)
				{
					hitTarget.ApplyPoison(definition.stats.attackPower * power * skillMultiplier, durationValue, Mathf.Max(0.25f, secondaryPower), this);
				}
				else if (skill.effectType == SkillEffectType.DamageGroundField)
				{
					hitTarget.TakeDamage(CalculateDamageAgainst(hitTarget, power * skillMultiplier, critical: false), critical: false, this);
					PlaySkillAreaEffect(skill, hitTarget.transform.position, durationValue);
					SpawnAreaDamageZone(hitTarget.transform.position, radius, secondaryPower * skillMultiplier, durationValue, 1f, skill);
				}
				else if (skill.effectType == SkillEffectType.FixedPoison)
				{
					hitTarget.ApplyPoison(power * skillMultiplier, durationValue, Mathf.Max(0.2f, secondaryPower), this);
				}
			}
			finally
			{
				CurrentDamageSkillContext = previousDamageSkillContext;
			}
		}

		private void ApplyAreaDamageAt(Vector3 center, float radius, float multiplier, bool critical, SkillDefinition skill = null)
		{
			IReadOnlyList<MonsterUnit> areaTargets = MonsterUnit.ActiveInstances;
			float checkedRadius = Mathf.Max(0.1f, radius);
			for (int i = 0; i < areaTargets.Count; i++)
			{
				MonsterUnit areaTarget = areaTargets[i];
				if (areaTarget != null && areaTarget.CanBeCombatTargeted && Vector3.Distance(center, areaTarget.transform.position) <= checkedRadius)
				{
					if (skill != null)
					{
						PlaySkillHitEffect(skill, areaTarget);
					}
					areaTarget.TakeDamage(CalculateDamageAgainst(areaTarget, multiplier, critical), critical, this);
				}
			}
		}

		private void ApplyLinePierceDamage(SkillDefinition skill, MonsterUnit currentTarget, float skillMultiplier)
		{
			MonsterUnit anchorTarget = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
			if (anchorTarget == null)
			{
				return;
			}
			Vector3 direction = anchorTarget.transform.position - base.transform.position;
			direction.y = 0f;
			if (direction.sqrMagnitude <= 0.0001f)
			{
				direction = base.transform.forward;
			}
			direction.Normalize();
			FaceTarget(base.transform.position + direction);
			float length = Mathf.Max(0.5f, GetSkillRadius(skill));
			float halfWidth = Mathf.Max(0.15f, GetSkillSecondaryPower(skill));
			float power = GetSkillPower(skill);
			IReadOnlyList<MonsterUnit> targets = MonsterUnit.ActiveInstances;
			for (int i = 0; i < targets.Count; i++)
			{
				MonsterUnit target = targets[i];
				if (target == null || !target.CanBeCombatTargeted)
				{
					continue;
				}
				Vector3 offset = target.transform.position - base.transform.position;
				offset.y = 0f;
				float forwardDistance = Vector3.Dot(offset, direction);
				if (!(forwardDistance < 0f) && !(forwardDistance > length))
				{
					Vector3 closestPoint = direction * forwardDistance;
					float sideDistance = (offset - closestPoint).magnitude;
					if (!(sideDistance > halfWidth))
					{
						PlaySkillHitEffect(skill, target);
						target.TakeDamage(CalculateDamageAgainst(target, power * skillMultiplier, critical: false), critical: false, this);
					}
				}
			}
		}

		private void ApplyStoneLine(SkillDefinition skill, MonsterUnit currentTarget)
		{
			MonsterUnit anchorTarget = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
			if (!(anchorTarget == null))
			{
				Vector3 direction = anchorTarget.transform.position - base.transform.position;
				float anchorDistance = direction.magnitude;
				direction.y = 0f;
				if (direction.sqrMagnitude <= 0.0001f)
				{
					direction = base.transform.forward;
				}
				direction.Normalize();
				FaceTarget(base.transform.position + direction);
				float length = Mathf.Max(Mathf.Max(0.5f, GetSkillRadius(skill)), anchorDistance + 0.05f);
				float halfWidth = Mathf.Max(0.15f, GetSkillSecondaryPower(skill));
				float durationValue = GetSkillDuration(skill);
				int maxTargets = Mathf.Max(1, GetSkillHitCount(skill));
				MonsterUnit.PetrifyTargetOptions options = new MonsterUnit.PetrifyTargetOptions
				{
					duration = durationValue,
					maxTargets = maxTargets,
					onApplied = delegate(MonsterUnit target)
					{
						PlaySkillHitEffect(skill, target);
					}
				};
				MonsterUnit.ApplyPetrifyLine(base.transform.position, direction, length, halfWidth, options);
			}
		}

		private void ApplyDamageGroundField(SkillDefinition skill, MonsterUnit currentTarget, float skillMultiplier)
		{
			MonsterUnit target = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
			if (!(target == null))
			{
				FaceTarget(target.transform.position);
				PlaySkillHitEffect(skill, target);
				float power = GetSkillPower(skill);
				float secondaryPower = GetSkillSecondaryPower(skill);
				float durationValue = GetSkillDuration(skill);
				float radius = GetSkillRadius(skill);
				target.TakeDamage(CalculateDamageAgainst(target, power * skillMultiplier, critical: false), critical: false, this);
				PlaySkillAreaEffect(skill, target.transform.position, durationValue);
				SpawnAreaDamageZone(target.transform.position, radius, secondaryPower * skillMultiplier, durationValue, 1f, skill);
			}
		}

		private void SpawnAreaDamageZone(Vector3 center, float radius, float damagePerTick, float duration, float tickInterval, SkillDefinition sourceSkill = null)
		{
			GameObject zoneObject = new GameObject("RuntimeAreaDamageZone");
			zoneObject.transform.position = center;
			RuntimeAreaDamageZone zone = zoneObject.AddComponent<RuntimeAreaDamageZone>();
			zone.Configure(center, radius, damagePerTick, duration, tickInterval, this, sourceSkill);
		}

		private void ApplyTaunt(float radius, float duration)
		{
			IReadOnlyList<MonsterUnit> activeTargets = MonsterUnit.ActiveInstances;
			float checkedRadius = Mathf.Max(0.1f, radius);
			for (int i = 0; i < activeTargets.Count; i++)
			{
				MonsterUnit monster = activeTargets[i];
				if (monster != null && Vector3.Distance(base.transform.position, monster.transform.position) <= checkedRadius)
				{
					monster.ApplyTaunt(this, duration);
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
			MonsterUnit anchorTarget = ((currentTarget != null) ? currentTarget : FindNearestSkillTarget(skill));
			if (!(anchorTarget == null))
			{
				int count = GetSkillHitCount(skill);
				for (int i = 0; i < count; i++)
				{
					SpawnSummonedAlly(skill, anchorTarget, skillMultiplier, i, count);
				}
			}
		}

		public void TriggerAugmentSkillEcho(SkillDefinition sourceSkill, float skillMultiplier)
		{
			if (sourceSkill != null && definition != null && !(currentHealth <= 0f))
			{
				MonsterUnit target = ResolveCombatTarget(null, sourceSkill);
				if (!(target == null) || !SkillNeedsMonsterTarget(sourceSkill))
				{
					ApplySkillEffect(sourceSkill, target, Mathf.Max(0.05f, skillMultiplier));
				}
			}
		}

		public void SpawnAugmentSummonedAllies(int count, float healthRatio, float attackRatio, SkillDefinition visualSkill = null)
		{
			int checkedCount = Mathf.Clamp(count, 1, 6);
			MonsterUnit anchorTarget = FindNearestTarget(Mathf.Max(2.5f, CurrentAttackRange + 2.4f));
			SkillDefinition summonSkill = new SkillDefinition
			{
				id = "augment_summon",
				displayName = "Augment Summon",
				effectType = SkillEffectType.SummonRush,
				power = Mathf.Max(0.05f, healthRatio),
				secondaryPower = Mathf.Max(0.05f, attackRatio),
				duration = 0f,
				radius = 2.5f,
				hitCount = checkedCount,
				areaEffectPrefab = visualSkill?.areaEffectPrefab
			};
			for (int i = 0; i < checkedCount; i++)
			{
				SpawnSummonedAlly(summonSkill, anchorTarget, 1f, i, checkedCount);
			}
		}

		private void SpawnSummonedAlly(SkillDefinition skill, MonsterUnit anchorTarget, float skillMultiplier, int index, int count)
		{
			GameObject sourcePrefab = ResolveSummonedUnitPrefab();
			if (!(sourcePrefab == null))
			{
				Vector3 spawnPosition = ResolveSummonPosition(anchorTarget, index, count);
				PlaySkillAreaEffect(skill, spawnPosition);
				GameObject summonedObject = UnityEngine.Object.Instantiate(sourcePrefab, spawnPosition, base.transform.rotation);
				DefenderUnit summonedUnit = summonedObject.GetComponent<DefenderUnit>();
				if (summonedUnit == null)
				{
					summonedUnit = summonedObject.AddComponent<DefenderUnit>();
				}
				summonedUnit.AdoptRuntimeTemplate(this);
				summonedObject.SetActive(value: true);
				summonedUnit.InitializeSummon(CreateSummonedDefinition(skill, skillMultiplier));
				RuntimeAudioUtility.PlayDiceAppear();
			}
		}

		private Vector3 ResolveSummonPosition(MonsterUnit anchorTarget, int index, int count)
		{
			Vector3 casterPosition = base.transform.position;
			Vector3 targetPosition = ((anchorTarget != null) ? anchorTarget.transform.position : (casterPosition + base.transform.forward * 2.4f));
			Vector3 toTarget = targetPosition - casterPosition;
			toTarget.y = 0f;
			if (toTarget.sqrMagnitude <= 0.0001f)
			{
				toTarget = base.transform.forward;
				toTarget.y = 0f;
			}
			if (toTarget.sqrMagnitude <= 0.0001f)
			{
				toTarget = Vector3.forward;
			}
			Vector3 forward = toTarget.normalized;
			Vector3 right = new Vector3(forward.z, 0f, 0f - forward.x);
			float lateralOffset = ((count <= 1) ? 0f : Mathf.Lerp(-0.72f, 0.72f, (float)index / (float)(count - 1)));
			float forwardDistance = Mathf.Clamp(toTarget.magnitude * 0.55f, 1.55f, 3.2f);
			Vector3 spawnPosition = casterPosition + forward * forwardDistance + right * lateralOffset;
			spawnPosition.y = casterPosition.y;
			return spawnPosition;
		}

		private CharacterDefinition CreateSummonedDefinition(SkillDefinition skill, float skillMultiplier)
		{
			float power = GetSkillPower(skill);
			float secondaryPower = GetSkillSecondaryPower(skill);
			float healthRatio = Mathf.Max(0.05f, power * skillMultiplier);
			float attackRatio = Mathf.Max(0.05f, ((secondaryPower > 0f) ? secondaryPower : power) * skillMultiplier);
			GameObject summonPrefab = ResolveSummonedUnitPrefab();
			CharacterDefinition summonedDefinition = new CharacterDefinition
			{
				id = definition.id + "_summon",
				displayName = definition.displayName + " Spirit",
				description = "Temporary summoned ally.",
				grade = definition.grade,
				role = CharacterRole.Summoner,
				tags = ((definition.tags != null) ? new List<CharacterTag>(definition.tags) : new List<CharacterTag>()),
				accentColor = Color.Lerp(definition.accentColor, Color.white, 0.28f),
				prefab = summonPrefab,
				stats = CloneCombatStats(definition.stats, healthRatio, attackRatio),
				attackBehavior = CloneAttackBehavior(definition.attackBehavior),
				skills = new List<SkillDefinition>(),
				mergeValue = 0
			};
			summonedDefinition.stats.maxMana = 0f;
			summonedDefinition.stats.manaRegenPerSecondRate = 0f;
			summonedDefinition.stats.manaGainWhenHitRate = 0f;
			summonedDefinition.stats.manaGainPerAttackRate = 0f;
			return summonedDefinition;
		}

		private GameObject ResolveSummonedUnitPrefab()
		{
			if (defaultSummonedUnitPrefab != null)
			{
				return defaultSummonedUnitPrefab;
			}
			if (definition != null && definition.prefab != null)
			{
				return definition.prefab;
			}
			return base.gameObject;
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

		private void SchedulePendingSkillFallback(int sequence)
		{
			CancelPendingSkillFallback();
			float delay = ((animationDriver != null) ? animationDriver.SkillImpactFallbackDelay : 0.2f);
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
			pendingBasicAttack = default(PendingBasicAttack);
			pendingSkillCast = default(PendingSkillCast);
			CancelPendingAttackFallback();
			CancelPendingSkillFallback();
		}

		private void FaceTarget(Vector3 targetPosition)
		{
			Vector3 direction = targetPosition - base.transform.position;
			direction.y = 0f;
			if (!(direction.sqrMagnitude <= 0.0001f))
			{
				base.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
			}
		}

		private MonsterUnit FindNearestTarget()
		{
			return FindNearestTarget(GetEffectiveAttackRange());
		}

		private MonsterUnit FindBasicAttackTarget()
		{
			float range = GetEffectiveAttackRange();
			if (CombatRuntimeQuery.IsValidMonsterTarget(cachedBasicAttackTarget, base.transform.position, range) && Time.unscaledTime < nextBasicTargetRefreshTime)
			{
				return cachedBasicAttackTarget;
			}
			if (definition != null && string.Equals(definition.id, "hero_57", StringComparison.OrdinalIgnoreCase))
			{
				cachedBasicAttackTarget = CombatRuntimeQuery.FindRandomMonster(monsters, base.transform.position, range);
			}
			else
			{
				cachedBasicAttackTarget = FindNearestTarget(range);
			}
			nextBasicTargetRefreshTime = CombatRuntimeQuery.ScheduleNextRefresh(this, basicTargetRefreshInterval);
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
			return CombatRuntimeQuery.FindNearestMonster(monsters, base.transform.position, range);
		}

		private List<MonsterUnit> GetNearestTargets(int count)
		{
			return GetNearestTargets(count, float.MaxValue);
		}

		private List<MonsterUnit> GetNearestTargets(int count, float range)
		{
			int limit = Mathf.Max(0, count);
			List<MonsterUnit> result = new List<MonsterUnit>(limit);
			if (limit == 0)
			{
				return result;
			}
			float checkedRange = Mathf.Max(0.1f, range);
			float checkedRangeSqr = checkedRange * checkedRange;
			Vector3 origin = base.transform.position;
			for (int i = 0; i < monsters.Count; i++)
			{
				MonsterUnit candidate = monsters[i];
				if (candidate == null || !candidate.CanBeCombatTargeted)
				{
					continue;
				}
				float candidateDistanceSqr = (candidate.transform.position - origin).sqrMagnitude;
				if (candidateDistanceSqr > checkedRangeSqr)
				{
					continue;
				}
				int insertIndex;
				for (insertIndex = result.Count; insertIndex > 0; insertIndex--)
				{
					float previousDistanceSqr = (result[insertIndex - 1].transform.position - origin).sqrMagnitude;
					if (previousDistanceSqr <= candidateDistanceSqr)
					{
						break;
					}
				}
				if (insertIndex < limit)
				{
					result.Insert(insertIndex, candidate);
					if (result.Count > limit)
					{
						result.RemoveAt(result.Count - 1);
					}
				}
			}
			return result;
		}

		private List<MonsterUnit> GetRandomTargetsWithReplacement(int count, float range)
		{
			int safeCount = Mathf.Max(0, count);
			List<MonsterUnit> result = new List<MonsterUnit>(safeCount);
			for (int i = 0; i < safeCount; i++)
			{
				MonsterUnit target = CombatRuntimeQuery.FindRandomMonster(monsters, base.transform.position, range);
				if (target == null)
				{
					break;
				}
				result.Add(target);
			}
			return result;
		}

		private MonsterUnit ResolveCombatTarget(MonsterUnit currentTarget, SkillDefinition skill)
		{
			if (currentTarget != null && currentTarget.CanBeCombatTargeted)
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
			IReadOnlyList<DefenderUnit> defenders = ActiveInstances;
			DefenderUnit bestTarget = null;
			float bestRatio = float.MaxValue;
			for (int i = 0; i < defenders.Count; i++)
			{
				DefenderUnit defender = defenders[i];
				if (!(defender == null) && !(defender.CurrentHealth <= 0f))
				{
					float ratio = defender.HealthRatio;
					if (ratio < bestRatio)
					{
						bestRatio = ratio;
						bestTarget = defender;
					}
				}
			}
			return bestTarget;
		}

		private List<DefenderUnit> FindLowestHealthAllies(int count)
		{
			int limit = Mathf.Max(1, count);
			List<DefenderUnit> result = new List<DefenderUnit>(limit);
			IReadOnlyList<DefenderUnit> defenders = ActiveInstances;
			for (int i = 0; i < defenders.Count; i++)
			{
				DefenderUnit defender = defenders[i];
				if (defender == null || defender.CurrentHealth <= 0f || defender.CurrentHealth >= defender.MaxHealth - 0.01f)
				{
					continue;
				}
				float healthRatio = defender.HealthRatio;
				int insertIndex = result.Count;
				while (insertIndex > 0 && result[insertIndex - 1].HealthRatio > healthRatio)
				{
					insertIndex--;
				}
				if (insertIndex < limit)
				{
					result.Insert(insertIndex, defender);
					if (result.Count > limit)
					{
						result.RemoveAt(result.Count - 1);
					}
				}
			}
			return result;
		}

		private List<DefenderUnit> FindAdjacentManaTargets(float radius)
		{
			DefenderUnit leftTarget = null;
			DefenderUnit rightTarget = null;
			float leftDistance = float.MaxValue;
			float rightDistance = float.MaxValue;
			float checkedRadius = Mathf.Max(0f, radius);
			IReadOnlyList<DefenderUnit> defenders = ActiveInstances;
			for (int i = 0; i < defenders.Count; i++)
			{
				DefenderUnit defender = defenders[i];
				if (defender == null || defender == this || defender.CurrentHealth <= 0f || defender.MaxMana <= 0f || defender.CurrentMana >= defender.MaxMana * 0.999f)
				{
					continue;
				}
				Vector3 offset = defender.transform.position - base.transform.position;
				float planarDistance = new Vector2(offset.x, offset.z).magnitude;
				if (!(checkedRadius > 0f) || !(planarDistance > checkedRadius))
				{
					if (offset.x < -0.05f && planarDistance < leftDistance)
					{
						leftDistance = planarDistance;
						leftTarget = defender;
					}
					else if (offset.x > 0.05f && planarDistance < rightDistance)
					{
						rightDistance = planarDistance;
						rightTarget = defender;
					}
				}
			}
			List<DefenderUnit> result = new List<DefenderUnit>(2);
			if (leftTarget != null)
			{
				result.Add(leftTarget);
			}
			if (rightTarget != null)
			{
				result.Add(rightTarget);
			}
			return result;
		}

		private bool HasMonsterInRadius(float radius)
		{
			return HasMonsterInRadius(base.transform.position, radius);
		}

		private bool HasMonsterInRadius(Vector3 center, float radius)
		{
			float checkedRadius = Mathf.Max(0.1f, radius);
			for (int i = monsters.Count - 1; i >= 0; i--)
			{
				MonsterUnit monster = monsters[i];
				if (monster == null)
				{
					monsters.RemoveAt(i);
				}
				else if (Vector3.Distance(center, monster.transform.position) <= checkedRadius)
				{
					return true;
				}
			}
			return false;
		}

		private IEnumerator GroundDamageRoutine(Vector3 center, float radius, float damagePerTick, float duration, float tickInterval, SkillDefinition sourceSkill = null)
		{
			float elapsed = 0f;
			float interval = Mathf.Max(0.15f, tickInterval);
			float checkedRadius = Mathf.Max(0.25f, radius);
			for (; elapsed < duration; elapsed += interval)
			{
				IReadOnlyList<MonsterUnit> activeTargets = MonsterUnit.ActiveInstances;
				for (int i = 0; i < activeTargets.Count; i++)
				{
					MonsterUnit monster = activeTargets[i];
					if (monster != null && monster.CanBeCombatTargeted && Vector3.Distance(center, monster.transform.position) <= checkedRadius)
					{
						RunWithSkillDamageContext(sourceSkill, delegate
						{
							monster.TakeDamage(damagePerTick, critical: false, this);
						});
					}
				}
				yield return new WaitForSeconds(interval);
			}
		}

		private void ApplyDefenseBuff(float shieldRatio, float duration, float radius, GameObject effectPrefab)
		{
			float checkedRadius = Mathf.Max(0.25f, radius);
			float checkedRadiusSqr = checkedRadius * checkedRadius;
			IReadOnlyList<DefenderUnit> defenders = ActiveInstances;
			for (int i = 0; i < defenders.Count; i++)
			{
				DefenderUnit defender = defenders[i];
				if (!(defender == null) && !(defender.CurrentHealth <= 0f) && (base.transform.position - defender.transform.position).sqrMagnitude <= checkedRadiusSqr)
				{
					defender.AddShield(defender.MaxHealth * shieldRatio, duration, effectPrefab);
				}
			}
		}

		private void ShowInstantSupportFeedback(string label, Color color, GameObject effectPrefab, float effectDuration)
		{
			floatingUi?.ShowStatus(label, color, 0.85f);
			RuntimeCombatFeedback.ShowGroundPulse(base.transform.position, color, 0.42f, 0.42f);
			PlayAttachedSupportEffect(effectPrefab, effectDuration);
		}

		private GameObject ShowTimedSupportFeedback(string label, Color color, float duration, GameObject effectPrefab)
		{
			floatingUi?.ShowTimedStatus(label, color, duration);
			RuntimeCombatFeedback.ShowGroundPulse(base.transform.position, color, 0.48f, 0.55f);
			return PlayAttachedSupportEffect(effectPrefab, duration);
		}

		private GameObject PlayAttachedSupportEffect(GameObject effectPrefab, float duration)
		{
			if (effectPrefab == null || duration <= 0f)
			{
				return null;
			}
			GameObject effect = RuntimeEffectUtility.PlayAttachedTimed(effectPrefab, base.transform, Vector3.zero, Quaternion.identity, duration);
			TrackOwnedSupportEffect(effect);
			return effect;
		}

		public void ClearRoundTemporaryEffects()
		{
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
			if (!(effect == null))
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
			if (!(activeShieldEffect == null))
			{
				ownedSupportEffects.Remove(activeShieldEffect);
				RuntimeEffectUtility.DestroyEffect(activeShieldEffect);
				activeShieldEffect = null;
			}
		}
		private void EnsureDormantRestEffect()
		{
			if (!IsAlternatingRoundBurstUnit() || !alternatingDormant || isDying || diceAutoDormantEffectPrefab == null || activeDormantRestEffect != null)
			{
				return;
			}
			activeDormantRestEffect = UnityEngine.Object.Instantiate(diceAutoDormantEffectPrefab, base.transform);
			activeDormantRestEffect.name = "DiceAutoDormantRestEffect";
			Transform effectTransform = activeDormantRestEffect.transform;
			effectTransform.localPosition = Vector3.zero;
			effectTransform.localRotation = Quaternion.identity;
			effectTransform.localScale = Vector3.one;
		}

		private void ClearDormantRestEffect()
		{
			if (activeDormantRestEffect != null)
			{
				RuntimeEffectUtility.DestroyEffect(activeDormantRestEffect);
				activeDormantRestEffect = null;
			}
		}

		private void ClearOwnedSupportEffects()
		{
			for (int i = ownedSupportEffects.Count - 1; i >= 0; i--)
			{
				GameObject effect = ownedSupportEffects[i];
				ownedSupportEffects.RemoveAt(i);
				RuntimeEffectUtility.DestroyEffect(effect);
			}
			activeShieldEffect = null;
		}

		private void PruneOwnedSupportEffects()
		{
			for (int i = ownedSupportEffects.Count - 1; i >= 0; i--)
			{
				if (ownedSupportEffects[i] == null)
				{
					ownedSupportEffects.RemoveAt(i);
				}
			}
		}

		private void ApplyAllyAttackSpeedBoost(float attackSpeedRatio, float duration, float radius, GameObject effectPrefab)
		{
			float checkedRadius = Mathf.Max(0.25f, radius);
			float checkedRadiusSqr = checkedRadius * checkedRadius;
			IReadOnlyList<DefenderUnit> defenders = ActiveInstances;
			for (int i = 0; i < defenders.Count; i++)
			{
				DefenderUnit defender = defenders[i];
				if (!(defender == null) && !(defender.CurrentHealth <= 0f) && (base.transform.position - defender.transform.position).sqrMagnitude <= checkedRadiusSqr)
				{
					defender.ActivateTimedCombatBoost(0f, attackSpeedRatio, duration, effectPrefab, "공속 +" + Mathf.RoundToInt(attackSpeedRatio * 100f) + "%", AttackSpeedFeedbackColor);
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
					float expiredShield = currentShield;
					currentShield = 0f;
					ClearShieldEffect();
					if (expiredShield > 0f)
					{
						DefenderUnit.OnShieldResolved?.Invoke(this, expiredShield, arg3: true, null);
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
				SkillDefinition skill = definition.skills[i];
				if (skillCooldowns.ContainsKey(skill.id))
				{
					skillCooldowns[skill.id] = Mathf.Max(0f, skillCooldowns[skill.id] - Time.deltaTime);
				}
			}
		}

		private void ApplyVisuals()
		{
			if (tintRenderers == null || tintRenderers.Length == 0)
			{
				tintRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
			}
			for (int i = 0; i < tintRenderers.Length; i++)
			{
				ApplyRendererTint(tintRenderers[i], definition.accentColor);
			}
			Color gpuTint = (RuntimeRenderBatchingUtility.UsePerInstanceUnitTint ? definition.accentColor : Color.white);
			GpuSkinnedUnitRenderer.AttachOrRefresh(base.gameObject, tintRenderers, gpuTint, isDefender: true, isBoss: false);
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

		private void EnsureInteractionCollider()
		{
			if (GetComponentInChildren<Collider>(includeInactive: true) != null)
			{
				return;
			}
			if (tintRenderers == null || tintRenderers.Length == 0)
			{
				tintRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
			}
			Bounds bounds = new Bounds(base.transform.position, Vector3.one);
			bool initialized = false;
			for (int i = 0; i < tintRenderers.Length; i++)
			{
				Renderer renderer = tintRenderers[i];
				if (!(renderer == null))
				{
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
			}
			BoxCollider collider = base.gameObject.GetComponent<BoxCollider>();
			if (collider == null)
			{
				collider = base.gameObject.AddComponent<BoxCollider>();
			}
			Vector3 worldCenter = (initialized ? bounds.center : (base.transform.position + Vector3.up * 0.9f));
			Vector3 worldSize = (initialized ? bounds.size : new Vector3(1f, 1.8f, 1f));
			collider.center = base.transform.InverseTransformPoint(worldCenter);
			collider.size = new Vector3(Mathf.Max(0.6f, worldSize.x), Mathf.Max(1.2f, worldSize.y), Mathf.Max(0.6f, worldSize.z));
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
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}

		private void PlayDeathEffect()
		{
			RuntimeEffectUtility.PlayOneShot(deathEffectPrefab, base.transform.position + deathEffectOffset, Quaternion.identity, 3f);
		}

		private void TriggerDeathPoisonField()
		{
			if (definition == null || definition.skills == null)
			{
				return;
			}
			for (int i = 0; i < definition.skills.Count; i++)
			{
				SkillDefinition skill = definition.skills[i];
				if (skill != null && skill.effectType == SkillEffectType.DeathPoisonField)
				{
					Vector3 direction = base.transform.forward;
					MonsterUnit target = FindNearestSkillTarget(skill);
					if (target != null)
					{
						direction = target.transform.position - base.transform.position;
						direction.y = 0f;
					}
					if (direction.sqrMagnitude <= 0.0001f)
					{
						direction = base.transform.forward;
					}
					direction.Normalize();
					float radius = Mathf.Max(0.25f, GetSkillRadius(skill));
					float duration = Mathf.Max(0.1f, GetSkillDuration(skill));
					float skillMultiplier = Mathf.Max(0.1f, 1f + permanentSkillPowerBonus + synergyBonus.skillPowerBonus + tileSkillPowerBonus);
					Vector3 center = base.transform.position + direction * Mathf.Max(1f, radius * 0.6f);
					center.y = base.transform.position.y;
					PlaySkillAreaEffect(skill, center, duration);
					SpawnAreaDamageZone(center, radius, GetSkillPower(skill) * skillMultiplier, duration, Mathf.Max(0.2f, GetSkillSecondaryPower(skill)), skill);
				}
			}
		}

		private void HandleMonsterSpawned(MonsterUnit monster)
		{
			if (monster != null && !monsters.Contains(monster))
			{
				monsters.Add(monster);
			}
			if (!CombatRuntimeQuery.IsValidMonsterTarget(cachedBasicAttackTarget, base.transform.position, GetEffectiveAttackRange()))
			{
				InvalidateBasicTargetCache();
			}
		}

		private void HandleMonsterRemoved(MonsterUnit monster)
		{
			monsters.Remove(monster);
			if (cachedBasicAttackTarget == monster)
			{
				InvalidateBasicTargetCache();
			}
			if (!HasAnyLivingMonster())
			{
				ResetFacingToDefault();
				if (animationDriver == null || !animationDriver.IsLocked)
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
			for (int i = monsters.Count - 1; i >= 0; i--)
			{
				MonsterUnit monster = monsters[i];
				if (monster == null)
				{
					monsters.RemoveAt(i);
				}
				else if (monster.CanBeCombatTargeted)
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
			return animationDriver == null || !animationDriver.IsLocked;
		}

		private float GetEffectiveAttackRange()
		{
			float baseRange = definition.stats.attackRange;
			if (definition.attackBehavior != null)
			{
				baseRange = definition.attackBehavior.ResolveAttackRange(baseRange);
			}
			return Mathf.Max(0.5f, baseRange + attackRangeBonus + synergyBonus.rangeBonus + tileAttackRangeBonus);
		}

		private float GetEffectiveSkillRange(SkillDefinition skill)
		{
			float baseRange = ((skill != null && skill.useCustomCastRange) ? skill.castRange : definition.stats.attackRange);
			return Mathf.Max(0.5f, baseRange + attackRangeBonus + synergyBonus.rangeBonus + tileAttackRangeBonus);
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
			int baseCount = Mathf.Max(1, skill?.hitCount ?? 1);
			if (!IsSkillGrowthTarget(skill, SkillGrowthTarget.HitCount))
			{
				return baseCount;
			}
			return Mathf.Max(1, Mathf.RoundToInt((float)baseCount * GetSkillGrowthMultiplier(skill, SkillGrowthTarget.HitCount)));
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
					SkillDefinition skill = definition.skills[i];
					if (skill != null && skill.growthTargets != SkillGrowthTarget.None)
					{
						return Mathf.Max(0f, skill.growthStepRatio);
					}
				}
			}
			return 0.1f;
		}

		private float GetEffectiveAttackPower()
		{
			float multiplier = 1f + permanentAttackPowerBonus + synergyBonus.attackPowerBonus + temporaryAttackPowerBonus + tileAttackPowerBonus - temporaryAttackPowerReduction;
			return definition.stats.attackPower * Mathf.Max(0.1f, multiplier);
		}

		private float CalculateDamageAgainst(MonsterUnit target, float multiplier, bool critical)
		{
			float damage = GetEffectiveAttackPower() * multiplier;
			if (critical)
			{
				damage *= definition.stats.criticalDamageMultiplier + permanentCriticalDamageBonus + synergyBonus.criticalDamageBonus;
			}
			if (target != null && target.IsBoss)
			{
				damage *= 1f + permanentBossDamageBonus + synergyBonus.bossDamageBonus + tileBossDamageBonus;
			}
			return damage;
		}

		private float GetEffectiveCriticalChance()
		{
			return Mathf.Clamp01(definition.stats.criticalChance + critChanceBonus + permanentCritChanceBonus + synergyBonus.critChanceBonus);
		}

		private float GetBasicAttackSplashRadius()
		{
			float radius = ((definition.attackBehavior != null) ? definition.attackBehavior.splashRadius : 0f);
			return Mathf.Max(0f, radius + splashRadiusBonus + synergyBonus.splashRadiusBonus);
		}

		private float GetBasicAttackSplashDamageRatio()
		{
			float ratio = ((definition.attackBehavior != null) ? definition.attackBehavior.splashDamageRatio : 0f);
			return Mathf.Clamp01(ratio + splashDamageRatioBonus + synergyBonus.splashDamageRatioBonus);
		}

		private void ApplyBasicAttackSplash(MonsterUnit primaryTarget, float baseDamage)
		{
			float splashRadius = GetBasicAttackSplashRadius();
			float splashDamageRatio = GetBasicAttackSplashDamageRatio();
			if (primaryTarget == null || splashRadius <= 0f || splashDamageRatio <= 0f)
			{
				return;
			}
			IReadOnlyList<MonsterUnit> nearbyMonsters = MonsterUnit.ActiveInstances;
			for (int i = 0; i < nearbyMonsters.Count; i++)
			{
				MonsterUnit monster = nearbyMonsters[i];
				if (!(monster == null) && !(monster == primaryTarget) && monster.CanBeCombatTargeted && Vector3.Distance(primaryTarget.transform.position, monster.transform.position) <= splashRadius)
				{
					monster.TakeDamage(baseDamage * splashDamageRatio, critical: false, this);
				}
			}
		}
	}
}
