using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame;

public class AugmentManager : MonoBehaviour
{
	[SerializeField]
	private DefenseGameController gameController;

	[SerializeField]
	private int firstChoiceRound = 5;

	[SerializeField]
	private int minChoiceInterval = 5;

	[SerializeField]
	private int maxChoiceInterval = 5;

	[SerializeField]
	private int shopOverlapAllowedRound = 30;

	[SerializeField]
	private int rareHeroAugmentUnlockRound = 6;

	[SerializeField]
	private int mythicHeroAugmentUnlockRound = 10;

	[SerializeField]
	private float normalHeroAugmentOfferChance = 0.55f;

	[SerializeField]
	private float rareHeroAugmentOfferChance = 0.28f;

	[SerializeField]
	private float mythicHeroAugmentOfferChance = 0.1f;

	[SerializeField]
	private float extraHeroCopyOfferBonus = 0.08f;

	[SerializeField]
	private List<AugmentDefinition> augmentPool = new List<AugmentDefinition>();

	[SerializeField]
	[Range(3f, 18f)]
	private int recentAugmentHistorySize = 9;

	private GameObject panelRoot;

	private Text headerText;

	private Text[] styleTexts;

	private Text[] titleTexts;

	private Text[] descriptionTexts;

	private Image[] accentImages;

	private Button[] choiceButtons;

	private Button closeButton;

	private Button reopenButton;

	private readonly List<AugmentDefinition> currentChoices = new List<AugmentDefinition>();

	private readonly List<AugmentDefinition> chosenAugments = new List<AugmentDefinition>();

	private readonly List<string> recentAugmentOfferIds = new List<string>();

	private readonly Dictionary<string, int> buildupStacks = new Dictionary<string, int>();

	private readonly Dictionary<string, bool> heroAugmentOfferRolls = new Dictionary<string, bool>();

	private readonly Dictionary<DefenderUnit, float> hero54StoredDamage = new Dictionary<DefenderUnit, float>();

	private readonly Dictionary<DefenderUnit, int> hero07KillCounts = new Dictionary<DefenderUnit, int>();

	private readonly Dictionary<DefenderUnit, int> hero07StrikeCounts = new Dictionary<DefenderUnit, int>();

	private readonly HashSet<DefenderUnit> hero07ReaperActive = new HashSet<DefenderUnit>();

	private readonly Dictionary<DefenderUnit, float> hero11MaxHealthGrowth = new Dictionary<DefenderUnit, float>();

	private readonly Dictionary<DefenderUnit, float> hero31MaxHealthGrowth = new Dictionary<DefenderUnit, float>();

	private int skillChainCastCount;

	private bool subscribed;

	private bool resolvingHeroAugmentDamage;

	private bool resolvingHeroSkillEcho;

	private float nextEconomyBannerTime;

	private float hoardIncomeTimer;

	private const int GoldJackpotMinimumReward = 8;

	private const float GoldJackpotRollThreshold01 = 0.8f;

	private int nextChoiceRound = -1;

	private int pendingChoiceRound = -1;

	public bool IsChoiceOpen => (Object)(object)panelRoot != (Object)null && panelRoot.activeSelf;

	public bool HasPendingChoice => HasPendingChoiceData && IsPendingChoiceReady();

	private bool HasPendingChoiceData => pendingChoiceRound > 0 && currentChoices.Count > 0;

	public event Action<int> OnChoiceShown;

	public event Action<AugmentDefinition> OnChoiceSelected;

	public event Action OnChoiceClosed;

	public bool HasChosenAugment(string id)
	{
		return HasChosen(id);
	}

	public bool TryGrantShopAugment(string id)
	{
		if (string.IsNullOrWhiteSpace(id) || HasChosen(id))
		{
			return false;
		}
		EnsureDefaultPool();
		EnsureHeroSpecificAugments();
		AugmentDefinition augmentDefinition = augmentPool.Find((AugmentDefinition augment) => augment != null && string.Equals(augment.id, id, StringComparison.Ordinal));
		if (augmentDefinition == null)
		{
			return false;
		}
		chosenAugments.Add(augmentDefinition);
		ApplyEconomyAugment(augmentDefinition);
		DefenderUnit[] array = Object.FindObjectsOfType<DefenderUnit>();
		for (int num = 0; num < array.Length; num++)
		{
			ApplyPermanentAugmentToDefender(array[num], augmentDefinition);
		}
		this.OnChoiceSelected?.Invoke(augmentDefinition);
		return true;
	}

	public void Configure(DefenseGameController controller, GameObject root, Text header, Text[] titles, Text[] descriptions, Button[] buttons, Text[] styles = null, Image[] accents = null, Button close = null, Button reopen = null)
	{
		Unsubscribe();
		gameController = controller;
		gameController?.RegisterAugmentManager(this);
		panelRoot = root;
		headerText = header;
		titleTexts = titles;
		descriptionTexts = descriptions;
		choiceButtons = buttons;
		styleTexts = styles;
		accentImages = accents;
		closeButton = close;
		reopenButton = reopen;
		if ((Object)(object)closeButton != (Object)null)
		{
			((Component)closeButton).gameObject.SetActive(false);
		}
		EnsureDefaultPool();
		EnsureHeroSpecificAugments();
		BindButtons();
		if ((Object)(object)panelRoot != (Object)null)
		{
			panelRoot.SetActive(false);
		}
		currentChoices.Clear();
		chosenAugments.Clear();
		recentAugmentOfferIds.Clear();
		buildupStacks.Clear();
		hero54StoredDamage.Clear();
		hero07KillCounts.Clear();
		hero07StrikeCounts.Clear();
		hero07ReaperActive.Clear();
		hero11MaxHealthGrowth.Clear();
		hero31MaxHealthGrowth.Clear();
		skillChainCastCount = 0;
		nextChoiceRound = -1;
		pendingChoiceRound = -1;
		hoardIncomeTimer = 0f;
		EnsureChoiceSchedule();
		UpdateReopenButton();
		Subscribe();
	}

	public bool WillOfferChoice(int round)
	{
		EnsureChoiceSchedule();
		return HasPendingChoice || (round >= firstChoiceRound && round >= nextChoiceRound);
	}

	public void OpenPendingChoice()
	{
		if (!HasPendingChoice)
		{
			UpdateReopenButton();
			return;
		}
		RefreshChoiceUi(pendingChoiceRound);
		SetChoiceOpen(open: true);
		this.OnChoiceShown?.Invoke(pendingChoiceRound);
	}

	public void CloseChoice()
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if (!HasPendingChoiceData)
		{
			HidePanel();
			return;
		}
		SetChoiceOpen(open: true);
		gameController?.RequestBanner("증강체는 무료이며 1개를 반드시 선택해야 합니다", new Color(0.52f, 0.9f, 1f), 2.2f);
	}

	private void OnEnable()
	{
		Subscribe();
	}

	private void OnDisable()
	{
		Unsubscribe();
	}

	private void Subscribe()
	{
		if (!subscribed)
		{
			if ((Object)(object)gameController != (Object)null)
			{
				gameController.OnRoundEconomySettlement += HandleRoundEconomySettlement;
				gameController.OnRoundAugmentChoicePhase += HandleRoundAugmentChoicePhase;
				gameController.OnRoundStarted += HandleRoundStarted;
			}
			DefenderUnit.OnDefenderSpawned += HandleDefenderSpawned;
			DefenderUnit.OnDamageDealt += HandleDamageDealt;
			DefenderUnit.OnSkillCast += HandleDefenderSkillCast;
			DefenderUnit.OnShieldResolved += HandleShieldResolved;
			DefenderUnit.OnDamageTaken += HandleDefenderDamageTaken;
			MonsterUnit.OnMonsterKilled += HandleMonsterKilled;
			subscribed = true;
		}
	}

	private void Update()
	{
		TickHoardInterestGold();
	}

	private void TickHoardInterestGold()
	{
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)gameController == (Object)null || chosenAugments.Count == 0)
		{
			hoardIncomeTimer = 0f;
			return;
		}
		bool flag = false;
		int num = 0;
		float num2 = 0f;
		Color accentColor = default(Color);
		((Color)(ref accentColor))._002Ector(1f, 0.9f, 0.3f);
		for (int i = 0; i < chosenAugments.Count; i++)
		{
			AugmentDefinition augmentDefinition = chosenAugments[i];
			if (augmentDefinition != null && augmentDefinition.effectType == AugmentEffectType.HoardInterestGold)
			{
				flag = true;
				int num3 = Mathf.Max(1, Mathf.RoundToInt(augmentDefinition.value));
				if (gameController.Gold >= num3)
				{
					num += Mathf.Max(1, Mathf.RoundToInt(augmentDefinition.secondaryValue));
					float num4 = Mathf.Max(0.5f, (augmentDefinition.duration > 0f) ? augmentDefinition.duration : 3f);
					num2 = ((num2 <= 0f) ? num4 : Mathf.Min(num2, num4));
					accentColor = augmentDefinition.accentColor;
				}
			}
		}
		if (!flag || !gameController.IsCombatInteractionLocked || num <= 0 || num2 <= 0f)
		{
			hoardIncomeTimer = 0f;
			return;
		}
		hoardIncomeTimer += Time.deltaTime;
		if (!(hoardIncomeTimer < num2))
		{
			int num5 = Mathf.Max(1, Mathf.FloorToInt(hoardIncomeTimer / num2));
			hoardIncomeTimer -= num2 * (float)num5;
			int amount = num * num5;
			gameController.AddGold(amount);
			ShowEconomyBanner("저축 이자 +" + amount + "G", accentColor, 1.4f, 0.8f);
		}
	}

	private void Unsubscribe()
	{
		if (subscribed)
		{
			if ((Object)(object)gameController != (Object)null)
			{
				gameController.OnRoundEconomySettlement -= HandleRoundEconomySettlement;
				gameController.OnRoundAugmentChoicePhase -= HandleRoundAugmentChoicePhase;
				gameController.OnRoundStarted -= HandleRoundStarted;
			}
			DefenderUnit.OnDefenderSpawned -= HandleDefenderSpawned;
			DefenderUnit.OnDamageDealt -= HandleDamageDealt;
			DefenderUnit.OnSkillCast -= HandleDefenderSkillCast;
			DefenderUnit.OnShieldResolved -= HandleShieldResolved;
			DefenderUnit.OnDamageTaken -= HandleDefenderDamageTaken;
			MonsterUnit.OnMonsterKilled -= HandleMonsterKilled;
			subscribed = false;
		}
	}

	private void BindButtons()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Expected O, but got Unknown
		if ((Object)(object)closeButton != (Object)null)
		{
			((UnityEvent)closeButton.onClick).RemoveListener(new UnityAction(CloseChoice));
			((UnityEvent)closeButton.onClick).AddListener(new UnityAction(CloseChoice));
		}
		if ((Object)(object)reopenButton != (Object)null)
		{
			((UnityEvent)reopenButton.onClick).RemoveListener(new UnityAction(OpenPendingChoice));
			((UnityEvent)reopenButton.onClick).AddListener(new UnityAction(OpenPendingChoice));
		}
		if (choiceButtons == null)
		{
			return;
		}
		for (int i = 0; i < choiceButtons.Length; i++)
		{
			int choiceIndex = i;
			if (!((Object)(object)choiceButtons[i] == (Object)null))
			{
				((UnityEventBase)choiceButtons[i].onClick).RemoveAllListeners();
				((UnityEvent)choiceButtons[i].onClick).AddListener((UnityAction)delegate
				{
					ChooseAugment(choiceIndex);
				});
			}
		}
	}

	private void HandleRoundEconomySettlement(int round)
	{
		ResolveRoundCompletedEconomy(round);
	}

	private void HandleRoundAugmentChoicePhase(int round)
	{
		if (ShouldDelayChoiceForShop(round))
		{
			DelayChoiceForShop(round);
		}
		else if (HasPendingChoice)
		{
			OpenPendingChoice();
		}
		else if (WillOfferChoice(round))
		{
			ShowChoices(round);
		}
	}

	private void HandleRoundStarted(int round)
	{
		DefenderUnit[] array = Object.FindObjectsOfType<DefenderUnit>();
		hero07KillCounts.Clear();
		hero07StrikeCounts.Clear();
		hero07ReaperActive.Clear();
		skillChainCastCount = 0;
		for (int i = 0; i < chosenAugments.Count; i++)
		{
			AugmentDefinition augmentDefinition = chosenAugments[i];
			if (augmentDefinition == null)
			{
				continue;
			}
			if (augmentDefinition.effectType == AugmentEffectType.RoundStartBurst)
			{
				for (int j = 0; j < array.Length; j++)
				{
					ApplyRoundStartAugment(array[j], augmentDefinition);
				}
			}
			if (augmentDefinition.effectType == AugmentEffectType.Hero05GuardianProtocol)
			{
				ApplyHero05GuardianProtocol(array, augmentDefinition);
			}
			if (IsBuildupEffect(augmentDefinition.effectType))
			{
				IncrementBuildup(augmentDefinition);
				for (int k = 0; k < array.Length; k++)
				{
					ApplyBuildupIncrement(array[k], augmentDefinition);
				}
			}
			ResolveRoundStartedEconomy(augmentDefinition, round);
		}
	}

	private void HandleDamageDealt(DefenderUnit source, MonsterUnit target, float damage, bool critical)
	{
		if ((Object)(object)source == (Object)null || (Object)(object)target == (Object)null || damage <= 0f || resolvingHeroAugmentDamage)
		{
			return;
		}
		for (int i = 0; i < chosenAugments.Count; i++)
		{
			AugmentDefinition augmentDefinition = chosenAugments[i];
			if (augmentDefinition != null)
			{
				switch (augmentDefinition.effectType)
				{
				case AugmentEffectType.Hero08PetrifyBloom:
					TryResolveHero08PetrifyBloom(source, target, augmentDefinition);
					break;
				case AugmentEffectType.Hero01VolcanicAftershock:
					TryResolveHero01VolcanicAftershock(source, target, damage, augmentDefinition);
					break;
				case AugmentEffectType.Hero03FrostResidue:
					TryResolveHero03FrostResidue(source, target, augmentDefinition);
					break;
				case AugmentEffectType.Hero13ManaNetwork:
					TryResolveHero13ManaNetwork(source, augmentDefinition);
					break;
				}
			}
		}
		ResolveHero07ReaperStrike(source, target);
		ResolveGeneralDamageAugments(source, target, damage, critical);
	}

	private void ResolveGeneralDamageAugments(DefenderUnit source, MonsterUnit target, float damage, bool critical)
	{
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)source == (Object)null || (Object)(object)target == (Object)null || target.CurrentHealth <= 0f || damage <= 0f)
		{
			return;
		}
		float num = 0f;
		Color accentColor = default(Color);
		((Color)(ref accentColor))._002Ector(1f, 0.66f, 0.24f);
		for (int i = 0; i < chosenAugments.Count; i++)
		{
			AugmentDefinition augmentDefinition = chosenAugments[i];
			if (augmentDefinition == null)
			{
				continue;
			}
			if (augmentDefinition.effectType == AugmentEffectType.ExecuteDamage && target.MaxHealth > 0f)
			{
				float num2 = Mathf.Clamp01(augmentDefinition.value);
				if (target.CurrentHealth / target.MaxHealth <= num2)
				{
					float num3 = (target.IsBoss ? Mathf.Max(0f, augmentDefinition.duration) : Mathf.Max(0f, augmentDefinition.secondaryValue));
					num += damage * num3;
					accentColor = augmentDefinition.accentColor;
				}
			}
			else if (augmentDefinition.effectType == AugmentEffectType.LowHealthFury && source.HealthRatio <= Mathf.Clamp01(augmentDefinition.value))
			{
				num += damage * Mathf.Max(0f, augmentDefinition.secondaryValue);
				accentColor = augmentDefinition.accentColor;
			}
			else if (augmentDefinition.effectType == AugmentEffectType.CriticalDoubleTap && critical && Random.value <= Mathf.Clamp01(augmentDefinition.value))
			{
				num += damage * Mathf.Max(0f, augmentDefinition.secondaryValue);
				accentColor = augmentDefinition.accentColor;
			}
		}
		if (!(num <= 0f))
		{
			resolvingHeroAugmentDamage = true;
			try
			{
				target.TakeDamage(num, critical: false, source);
			}
			finally
			{
				resolvingHeroAugmentDamage = false;
			}
			RuntimeCombatFeedback.ShowGroundPulse(((Component)target).transform.position, accentColor, target.IsBoss ? 0.66f : 0.46f, 0.34f, 0.1f);
		}
	}

	private void HandleDefenderSkillCast(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
	{
		if (!((Object)(object)source == (Object)null) && source.Definition != null && skill != null && !resolvingHeroSkillEcho)
		{
			string id = source.Definition.id;
			if (string.Equals(id, "hero_01", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero01SkillAugments(source, skill, target);
			}
			else if (string.Equals(id, "hero_02", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero02SkillAugments(source, skill);
			}
			else if (string.Equals(id, "hero_03", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero03SkillAugments(source, skill, target);
			}
			else if (string.Equals(id, "hero_04", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero04SkillAugments(source, skill, target);
			}
			else if (string.Equals(id, "hero_06", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero06SkillAugments(source, skill, target);
			}
			else if (string.Equals(id, "hero_07", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero07SkillAugments(source, skill, target);
			}
			else if (string.Equals(id, "hero_08", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero08SkillAugments(source, skill, target);
			}
			else if (string.Equals(id, "hero_09", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero09SkillAugments(source, skill, target);
			}
			else if (string.Equals(id, "hero_10", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero10SkillAugments(source, skill);
			}
			else if (string.Equals(id, "hero_11", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero11SkillAugments(source, skill, target);
			}
			else if (string.Equals(id, "hero_12", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero12SkillAugments(source, skill);
			}
			else if (string.Equals(id, "hero_13", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero13SkillAugments(source);
			}
			else if (string.Equals(id, "hero_14", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero14SkillAugments(source);
			}
			else if (string.Equals(id, "hero_21", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero21SkillAugments(source, skill);
			}
			else if (string.Equals(id, "hero_22", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero22SkillAugments(source, skill);
			}
			else if (string.Equals(id, "hero_23", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero23SkillAugments(source, skill, target);
			}
			else if (string.Equals(id, "hero_31", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero31SkillAugments(source);
			}
			else if (string.Equals(id, "hero_32", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero32SkillAugments(source, target);
			}
			else if (string.Equals(id, "hero_33", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero33SkillAugments(source, skill, target);
			}
			else if (string.Equals(id, "hero_51", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero51SkillAugments(source, skill, target);
			}
			else if (string.Equals(id, "hero_52", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero52SkillAugments(source, skill, target);
			}
			else if (string.Equals(id, "hero_54", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero54SkillAugments(source, skill);
			}
			else if (string.Equals(id, "hero_55", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero55SkillAugments(source, target);
			}
			else if (string.Equals(id, "hero_56", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero56SkillAugments(source, skill, target);
			}
			else if (string.Equals(id, "hero_57", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero57SkillAugments(source);
			}
			ResolveGeneralSkillAugments(source, target);
		}
	}

	private void ResolveGeneralSkillAugments(DefenderUnit source, MonsterUnit target)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < chosenAugments.Count; i++)
		{
			AugmentDefinition augmentDefinition = chosenAugments[i];
			if (augmentDefinition == null)
			{
				continue;
			}
			if (augmentDefinition.effectType == AugmentEffectType.SkillManaRelay)
			{
				RestoreLowestManaDefenders(((Component)source).transform.position, 99f, Mathf.Clamp01(augmentDefinition.value), 1, source);
				RuntimeCombatFeedback.ShowGroundPulse(((Component)source).transform.position, augmentDefinition.accentColor, 0.42f, 0.32f);
			}
			else if (augmentDefinition.effectType == AugmentEffectType.SkillChainBlast)
			{
				skillChainCastCount++;
				int num = Mathf.Max(2, Mathf.RoundToInt(augmentDefinition.value));
				if (skillChainCastCount >= num)
				{
					skillChainCastCount = 0;
					Vector3 val = (((Object)(object)target != (Object)null) ? ((Component)target).transform.position : ((Component)source).transform.position);
					float radius = Mathf.Max(0.5f, augmentDefinition.duration);
					float damage = source.EffectiveAttackPower * Mathf.Max(0f, augmentDefinition.secondaryValue);
					ApplyHeroAreaDamage(source, null, val, radius, damage);
					RuntimeCombatFeedback.ShowGroundPulse(val, augmentDefinition.accentColor, radius, 0.48f, 0.1f);
				}
			}
		}
	}

	private void HandleShieldResolved(DefenderUnit shielded, float blockedDamage, bool shieldBroken, MonsterUnit source)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)shielded == (Object)null) && !(blockedDamage <= 0f) && CountFieldHero("hero_05") > 0)
		{
			if (shieldBroken && HasChosen("hero05_shield_bomb_n"))
			{
				ApplyHeroAreaDamage(shielded, null, ((Component)shielded).transform.position, 2.6f, Mathf.Max(5f, blockedDamage * 0.8f));
			}
			if ((Object)(object)source != (Object)null && HasChosen("hero05_reflect_r"))
			{
				source.TakeDamage(Mathf.Max(1f, blockedDamage * 0.45f), critical: false, shielded);
			}
			if (shieldBroken && HasChosen("hero05_bastion_m"))
			{
				ShieldNearbyDefenders(((Component)shielded).transform.position, 3.8f, 0.14f, 4.5f);
			}
		}
	}

	private void HandleDefenderDamageTaken(DefenderUnit defender, MonsterUnit source, float damage)
	{
		if (!((Object)(object)defender == (Object)null) && !(damage <= 0f) && hero54StoredDamage.ContainsKey(defender))
		{
			hero54StoredDamage[defender] += damage;
		}
	}

	private void HandleMonsterKilled(MonsterUnit monster)
	{
		if ((Object)(object)gameController == (Object)null || (Object)(object)monster == (Object)null)
		{
			return;
		}
		for (int i = 0; i < chosenAugments.Count; i++)
		{
			AugmentDefinition augmentDefinition = chosenAugments[i];
			if (augmentDefinition != null && augmentDefinition.effectType == AugmentEffectType.KillGoldChance && !(Random.value > Mathf.Clamp01(augmentDefinition.value)))
			{
				int num = Mathf.Max(1, Mathf.RoundToInt(augmentDefinition.secondaryValue));
				int num2 = Random.Range(1, num + 1);
				gameController.AddGold(num2);
				ShowRandomGoldFeedback(augmentDefinition, num2, 1, num, augmentDefinition.value);
			}
		}
		ResolveHeroMonsterKilledAugments(monster);
	}

	private void ResolveHeroMonsterKilledAugments(MonsterUnit monster)
	{
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)monster == (Object)null)
		{
			return;
		}
		DefenderUnit lastDamageSource = monster.LastDamageSource;
		if ((Object)(object)lastDamageSource == (Object)null || lastDamageSource.Definition == null)
		{
			return;
		}
		string id = lastDamageSource.Definition.id;
		if (string.Equals(id, "hero_01", StringComparison.OrdinalIgnoreCase) && HasChosen("hero01_mana_refund_r") && monster.LastDamageSkill != null)
		{
			lastDamageSource.RestoreMana(0.5f);
		}
		if (string.Equals(id, "hero_04", StringComparison.OrdinalIgnoreCase) && HasChosen("hero04_contagion_r") && !monster.IsBoss)
		{
			PoisonNearestMonster(lastDamageSource, ((Component)monster).transform.position, lastDamageSource.Definition.stats.attackPower * 0.42f, 4.5f, 0.75f);
		}
		if (string.Equals(id, "hero_07", StringComparison.OrdinalIgnoreCase))
		{
			ResolveHero07Kill(lastDamageSource);
		}
		if (string.Equals(id, "hero_52", StringComparison.OrdinalIgnoreCase) && (HasChosen("hero52_meteor_extend_r") || HasChosen("hero52_star_shower_m")) && monster.LastDamageSkill != null)
		{
			int num = ((!HasChosen("hero52_star_shower_m")) ? 1 : 3);
			for (int i = 0; i < num; i++)
			{
				Vector3 val = Random.insideUnitSphere * 1.8f;
				val.y = 0f;
				SpawnHeroDamageZone(lastDamageSource, monster.LastDamageSkill, ((Component)monster).transform.position + val, 2.2f, lastDamageSource.Definition.stats.attackPower * 0.35f, 2.8f, 0.7f);
			}
		}
		if ((Object)(object)gameController != (Object)null && string.Equals(id, "hero_08", StringComparison.OrdinalIgnoreCase) && HasChosen("hero08_trophy_m") && !monster.IsBoss && Random.value <= 0.3f)
		{
			gameController.AddGold(2);
			ShowEconomyBanner("전리품 석상 +2G", new Color(0.74f, 0.7f, 1f), 1.2f, 0.8f);
		}
	}

	private void HandleDefenderSpawned(DefenderUnit defender)
	{
		if ((Object)(object)defender == (Object)null)
		{
			return;
		}
		for (int i = 0; i < chosenAugments.Count; i++)
		{
			ApplyPermanentAugmentToDefender(defender, chosenAugments[i]);
		}
		if (!((Object)(object)gameController != (Object)null) || !gameController.IsRoundRunning)
		{
			return;
		}
		for (int j = 0; j < chosenAugments.Count; j++)
		{
			AugmentDefinition augmentDefinition = chosenAugments[j];
			if (augmentDefinition != null && augmentDefinition.effectType == AugmentEffectType.RoundStartBurst)
			{
				ApplyRoundStartAugment(defender, augmentDefinition);
			}
		}
	}

	private void ShowChoices(int round)
	{
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)panelRoot == (Object)null || augmentPool.Count == 0 || choiceButtons == null)
		{
			return;
		}
		currentChoices.Clear();
		heroAugmentOfferRolls.Clear();
		pendingChoiceRound = round;
		AugmentStyle[] array = BuildChoiceStyleSlots(round);
		for (int i = 0; i < array.Length; i++)
		{
			AugmentDefinition augmentDefinition = PickChoice(array[i]);
			if (augmentDefinition != null && !currentChoices.Contains(augmentDefinition))
			{
				currentChoices.Add(augmentDefinition);
			}
		}
		FillMissingChoices();
		RememberCurrentChoices();
		if ((Object)(object)headerText != (Object)null)
		{
			headerText.text = "증강체 선택  ROUND " + round;
		}
		for (int j = 0; j < choiceButtons.Length; j++)
		{
			bool flag = j < currentChoices.Count;
			if ((Object)(object)choiceButtons[j] != (Object)null)
			{
				((Component)choiceButtons[j]).gameObject.SetActive(flag);
			}
			if (flag)
			{
				AugmentDefinition augmentDefinition2 = currentChoices[j];
				if (accentImages != null && j < accentImages.Length && (Object)(object)accentImages[j] != (Object)null)
				{
					((Graphic)accentImages[j]).color = augmentDefinition2.accentColor;
				}
				if (styleTexts != null && j < styleTexts.Length && (Object)(object)styleTexts[j] != (Object)null)
				{
					styleTexts[j].text = GetChoiceLabel(augmentDefinition2);
					((Graphic)styleTexts[j]).color = Color.white;
				}
				if (titleTexts != null && j < titleTexts.Length && (Object)(object)titleTexts[j] != (Object)null)
				{
					titleTexts[j].text = augmentDefinition2.title;
					((Graphic)titleTexts[j]).color = augmentDefinition2.accentColor;
				}
				if (descriptionTexts != null && j < descriptionTexts.Length && (Object)(object)descriptionTexts[j] != (Object)null)
				{
					descriptionTexts[j].text = augmentDefinition2.description;
				}
			}
		}
		panelRoot.SetActive(true);
		UpdateReopenButton();
		this.OnChoiceShown?.Invoke(round);
	}

	private void RefreshChoiceUi(int round)
	{
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		if (choiceButtons == null)
		{
			return;
		}
		if ((Object)(object)headerText != (Object)null)
		{
			headerText.text = "증강체 선택  ROUND " + round;
		}
		for (int i = 0; i < choiceButtons.Length; i++)
		{
			bool flag = i < currentChoices.Count;
			if ((Object)(object)choiceButtons[i] != (Object)null)
			{
				((Component)choiceButtons[i]).gameObject.SetActive(flag);
			}
			if (flag)
			{
				AugmentDefinition augmentDefinition = currentChoices[i];
				if (accentImages != null && i < accentImages.Length && (Object)(object)accentImages[i] != (Object)null)
				{
					((Graphic)accentImages[i]).color = augmentDefinition.accentColor;
				}
				if (styleTexts != null && i < styleTexts.Length && (Object)(object)styleTexts[i] != (Object)null)
				{
					styleTexts[i].text = GetChoiceLabel(augmentDefinition);
					((Graphic)styleTexts[i]).color = Color.white;
				}
				if (titleTexts != null && i < titleTexts.Length && (Object)(object)titleTexts[i] != (Object)null)
				{
					titleTexts[i].text = augmentDefinition.title;
					((Graphic)titleTexts[i]).color = augmentDefinition.accentColor;
				}
				if (descriptionTexts != null && i < descriptionTexts.Length && (Object)(object)descriptionTexts[i] != (Object)null)
				{
					descriptionTexts[i].text = augmentDefinition.description;
				}
			}
		}
	}

	private AugmentStyle[] BuildChoiceStyleSlots(int round)
	{
		int num = Mathf.Max(1, minChoiceInterval);
		int num2 = Mathf.Max(0, round - Mathf.Max(1, firstChoiceRound)) / num;
		return (num2 % 4) switch
		{
			0 => new AugmentStyle[3]
			{
				AugmentStyle.Stable,
				AugmentStyle.Growth,
				AugmentStyle.Gamble
			}, 
			1 => new AugmentStyle[3]
			{
				AugmentStyle.Growth,
				AugmentStyle.Buildup,
				AugmentStyle.Stable
			}, 
			2 => new AugmentStyle[3]
			{
				AugmentStyle.Gamble,
				AugmentStyle.Buildup,
				AugmentStyle.Growth
			}, 
			_ => new AugmentStyle[3]
			{
				AugmentStyle.Stable,
				AugmentStyle.Gamble,
				AugmentStyle.Buildup
			}, 
		};
	}

	private AugmentDefinition PickChoice(AugmentStyle style)
	{
		List<AugmentDefinition> list = new List<AugmentDefinition>();
		for (int i = 0; i < augmentPool.Count; i++)
		{
			AugmentDefinition augmentDefinition = augmentPool[i];
			if (augmentDefinition != null && augmentDefinition.style == style && !currentChoices.Contains(augmentDefinition) && !HasChosen(augmentDefinition.id) && !WasRecentlyOffered(augmentDefinition) && CanOfferAugment(augmentDefinition))
			{
				list.Add(augmentDefinition);
			}
		}
		if (list.Count == 0)
		{
			for (int j = 0; j < augmentPool.Count; j++)
			{
				AugmentDefinition augmentDefinition2 = augmentPool[j];
				if (augmentDefinition2 != null && augmentDefinition2.style == style && !currentChoices.Contains(augmentDefinition2) && CanOfferAugment(augmentDefinition2))
				{
					list.Add(augmentDefinition2);
				}
			}
		}
		return (list.Count > 0) ? list[Random.Range(0, list.Count)] : null;
	}

	private void FillMissingChoices()
	{
		int num = ((choiceButtons != null) ? Mathf.Min(3, choiceButtons.Length) : 3);
		List<AugmentDefinition> list = new List<AugmentDefinition>();
		for (int i = 0; i < augmentPool.Count; i++)
		{
			AugmentDefinition augmentDefinition = augmentPool[i];
			if (augmentDefinition != null && !currentChoices.Contains(augmentDefinition) && !HasChosen(augmentDefinition.id) && !WasRecentlyOffered(augmentDefinition) && CanOfferAugment(augmentDefinition))
			{
				list.Add(augmentDefinition);
			}
		}
		if (list.Count == 0)
		{
			for (int j = 0; j < augmentPool.Count; j++)
			{
				AugmentDefinition augmentDefinition2 = augmentPool[j];
				if (augmentDefinition2 != null && !currentChoices.Contains(augmentDefinition2) && CanOfferAugment(augmentDefinition2))
				{
					list.Add(augmentDefinition2);
				}
			}
		}
		while (currentChoices.Count < num && list.Count > 0)
		{
			int index = Random.Range(0, list.Count);
			currentChoices.Add(list[index]);
			list.RemoveAt(index);
		}
	}

	private bool WasRecentlyOffered(AugmentDefinition augment)
	{
		return augment != null && !string.IsNullOrWhiteSpace(augment.id) && recentAugmentOfferIds.Contains(augment.id);
	}

	private void RememberCurrentChoices()
	{
		for (int i = 0; i < currentChoices.Count; i++)
		{
			AugmentDefinition augmentDefinition = currentChoices[i];
			if (augmentDefinition != null && !string.IsNullOrWhiteSpace(augmentDefinition.id))
			{
				recentAugmentOfferIds.Remove(augmentDefinition.id);
				recentAugmentOfferIds.Add(augmentDefinition.id);
			}
		}
		int num = Mathf.Max(3, recentAugmentHistorySize);
		while (recentAugmentOfferIds.Count > num)
		{
			recentAugmentOfferIds.RemoveAt(0);
		}
	}

	private void ChooseAugment(int index)
	{
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		if (index >= 0 && index < currentChoices.Count)
		{
			AugmentDefinition augmentDefinition = currentChoices[index];
			chosenAugments.Add(augmentDefinition);
			ApplyEconomyAugment(augmentDefinition);
			DefenderUnit[] array = Object.FindObjectsOfType<DefenderUnit>();
			for (int i = 0; i < array.Length; i++)
			{
				ApplyPermanentAugmentToDefender(array[i], augmentDefinition);
			}
			this.OnChoiceSelected?.Invoke(augmentDefinition);
			int completedRound = ((pendingChoiceRound > 0) ? pendingChoiceRound : (((Object)(object)gameController != (Object)null) ? gameController.CurrentRound : nextChoiceRound));
			currentChoices.Clear();
			pendingChoiceRound = -1;
			ScheduleNextChoice(completedRound);
			gameController?.RequestBanner(GetChoiceLabel(augmentDefinition) + " 증강 획득: " + augmentDefinition.title, augmentDefinition.accentColor, 2.2f);
			HidePanel();
		}
	}

	private void HidePanel()
	{
		if ((Object)(object)panelRoot != (Object)null)
		{
			bool activeSelf = panelRoot.activeSelf;
			panelRoot.SetActive(false);
			UpdateReopenButton();
			if (activeSelf)
			{
				this.OnChoiceClosed?.Invoke();
			}
		}
	}

	private void SetChoiceOpen(bool open)
	{
		if ((Object)(object)panelRoot != (Object)null)
		{
			panelRoot.SetActive(open);
		}
		UpdateReopenButton();
	}

	private bool IsPendingChoiceReady()
	{
		if (!HasPendingChoiceData)
		{
			return false;
		}
		int num = (((Object)(object)gameController != (Object)null) ? gameController.CurrentRound : pendingChoiceRound);
		return num >= pendingChoiceRound;
	}

	private bool ShouldDelayChoiceForShop(int round)
	{
		return (Object)(object)gameController != (Object)null && round < Mathf.Max(1, shopOverlapAllowedRound) && gameController.WasRoundShopOpened(round) && (HasPendingChoiceData || round >= nextChoiceRound);
	}

	private void DelayChoiceForShop(int round)
	{
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.Max(round + 1, Mathf.Max(1, firstChoiceRound));
		if (HasPendingChoiceData)
		{
			pendingChoiceRound = Mathf.Max(num, pendingChoiceRound);
		}
		if (nextChoiceRound <= round)
		{
			nextChoiceRound = num;
		}
		if ((Object)(object)panelRoot != (Object)null && panelRoot.activeSelf)
		{
			panelRoot.SetActive(false);
			this.OnChoiceClosed?.Invoke();
		}
		UpdateReopenButton();
		if ((Object)(object)gameController != (Object)null && round >= firstChoiceRound)
		{
			gameController.RequestBanner("증강체 선택은 다음 라운드로 연기됩니다", new Color(0.72f, 0.88f, 1f), 1.8f);
		}
	}

	private void UpdateReopenButton()
	{
		if ((Object)(object)reopenButton != (Object)null)
		{
			((Component)reopenButton).gameObject.SetActive(HasPendingChoice && !IsChoiceOpen);
		}
	}

	private void ApplyPermanentAugmentToDefender(DefenderUnit defender, AugmentDefinition augment)
	{
		if ((Object)(object)defender == (Object)null || augment == null)
		{
			return;
		}
		switch (augment.effectType)
		{
		case AugmentEffectType.AttackPowerMultiplier:
			defender.AddAttackPowerBonus(augment.value);
			return;
		case AugmentEffectType.AttackSpeedMultiplier:
			defender.AddPermanentAttackSpeedBonus(augment.value);
			return;
		case AugmentEffectType.CriticalChance:
			defender.AddPermanentCriticalChanceBonus(augment.value);
			return;
		case AugmentEffectType.AttackRangeFlat:
			defender.AddAttackRangeBonus(augment.value);
			return;
		case AugmentEffectType.BasicAttackSplash:
			defender.AddBasicAttackSplash(augment.value, augment.secondaryValue);
			return;
		case AugmentEffectType.ManaRegenRate:
			defender.AddManaRegenRateBonus(augment.value);
			return;
		case AugmentEffectType.MaxHealthMultiplier:
			defender.AddMaxHealthBonus(augment.value);
			return;
		case AugmentEffectType.SkillPowerMultiplier:
			defender.AddSkillPowerBonus(augment.value);
			return;
		case AugmentEffectType.CriticalDamageMultiplier:
			defender.AddCriticalDamageBonus(augment.value);
			return;
		case AugmentEffectType.BossDamageMultiplier:
			defender.AddBossDamageBonus(augment.value);
			return;
		case AugmentEffectType.AttackHealthTradeoff:
			defender.AddAttackPowerBonus(Mathf.Max(0f, augment.value));
			defender.AddMaxHealthBonus(0f - Mathf.Abs(augment.secondaryValue));
			return;
		}
		if (IsBuildupEffect(augment.effectType))
		{
			ApplyBuildupTotal(defender, augment);
		}
	}

	private void ApplyRoundStartAugment(DefenderUnit defender, AugmentDefinition augment)
	{
		if (!((Object)(object)defender == (Object)null) && augment != null && augment.effectType == AugmentEffectType.RoundStartBurst)
		{
			defender.ActivateTimedCombatBoost(augment.value, augment.secondaryValue, Mathf.Max(1f, augment.duration));
		}
	}

	private void ApplyBuildupTotal(DefenderUnit defender, AugmentDefinition augment)
	{
		int num = GetBuildupStacks(augment);
		if (num > 0)
		{
			ApplyBuildupBonus(defender, augment, num);
		}
	}

	private void ApplyBuildupIncrement(DefenderUnit defender, AugmentDefinition augment)
	{
		ApplyBuildupBonus(defender, augment, 1);
	}

	private void ApplyBuildupBonus(DefenderUnit defender, AugmentDefinition augment, int stacks)
	{
		if (!((Object)(object)defender == (Object)null) && augment != null && stacks > 0)
		{
			float num = augment.value * (float)stacks;
			switch (augment.effectType)
			{
			case AugmentEffectType.ScalingAttackPowerPerRound:
				defender.AddAttackPowerBonus(num);
				break;
			case AugmentEffectType.ScalingAttackSpeedPerRound:
				defender.AddPermanentAttackSpeedBonus(num);
				break;
			case AugmentEffectType.ScalingSkillPowerPerRound:
				defender.AddSkillPowerBonus(num);
				break;
			case AugmentEffectType.ScalingManaRegenPerRound:
				defender.AddManaRegenRateBonus(num);
				break;
			case AugmentEffectType.ScalingBossDamagePerRound:
				defender.AddBossDamageBonus(num);
				break;
			}
		}
	}

	private void ApplyEconomyAugment(AugmentDefinition augment)
	{
		if (!((Object)(object)gameController == (Object)null) && augment != null)
		{
			switch (augment.effectType)
			{
			case AugmentEffectType.SummonCostDiscount:
				gameController.AddSummonCostDiscount(augment.value);
				break;
			case AugmentEffectType.RoundGoldBonus:
				gameController.AddRoundGoldBonus(Mathf.RoundToInt(augment.value));
				break;
			case AugmentEffectType.InstantGold:
				gameController.AddGold(Mathf.RoundToInt(augment.value));
				break;
			case AugmentEffectType.RandomInstantGold:
			{
				int num = Mathf.Max(0, Mathf.RoundToInt(augment.value));
				int num2 = Mathf.Max(num, Mathf.RoundToInt(augment.secondaryValue));
				int num3 = Random.Range(num, num2 + 1);
				gameController.AddGold(num3);
				ShowRandomGoldFeedback(augment, num3, num, num2);
				break;
			}
			case AugmentEffectType.WorkerGoldPerRound:
				break;
			}
		}
	}

	private void ResolveRoundStartedEconomy(AugmentDefinition augment, int round)
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)gameController == (Object)null) && augment != null)
		{
			if (augment.effectType == AugmentEffectType.WorkerGoldPerRound)
			{
				int amount = Mathf.Max(1, Mathf.RoundToInt(augment.value + (float)round * augment.secondaryValue));
				gameController.AddGold(amount);
				ShowEconomyBanner("광부 채굴 +" + amount + "G", augment.accentColor, 1.7f, 1.2f);
			}
			else if (augment.effectType == AugmentEffectType.ScalingGoldPerRound)
			{
				int num = GetBuildupStacks(augment);
				int amount2 = Mathf.Max(1, Mathf.RoundToInt(augment.value * (float)num));
				gameController.AddGold(amount2);
				ShowEconomyBanner("복리 광산 +" + amount2 + "G", augment.accentColor, 1.7f, 1.2f);
			}
		}
	}

	private void ResolveRoundCompletedEconomy(int round)
	{
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)gameController == (Object)null)
		{
			return;
		}
		for (int i = 0; i < chosenAugments.Count; i++)
		{
			AugmentDefinition augmentDefinition = chosenAugments[i];
			if (augmentDefinition != null)
			{
				if (augmentDefinition.effectType == AugmentEffectType.RoundClearRandomGold)
				{
					int num = Mathf.Max(0, Mathf.RoundToInt(augmentDefinition.value));
					int num2 = Mathf.Max(num, Mathf.RoundToInt(augmentDefinition.secondaryValue));
					int num3 = Random.Range(num, num2 + 1);
					gameController.AddGold(num3);
					ShowRandomGoldFeedback(augmentDefinition, num3, num, num2);
				}
				else if (augmentDefinition.effectType == AugmentEffectType.BossRoundBet && round > 0 && round % 10 == 0)
				{
					int num4 = Mathf.Max(1, Mathf.RoundToInt(augmentDefinition.value));
					int num5 = Mathf.Max(num4, Mathf.RoundToInt(augmentDefinition.value + augmentDefinition.secondaryValue));
					int num6 = Mathf.Clamp(Mathf.RoundToInt(augmentDefinition.value + Random.Range(0f, augmentDefinition.secondaryValue)), num4, num5);
					gameController.AddGold(num6);
					ShowRandomGoldFeedback(augmentDefinition, num6, num4, num5);
				}
				else if (augmentDefinition.effectType == AugmentEffectType.InterestGold)
				{
					int num7 = Mathf.Max(1, Mathf.RoundToInt(augmentDefinition.secondaryValue));
					int amount = Mathf.Clamp(Mathf.RoundToInt((float)gameController.Gold * Mathf.Max(0f, augmentDefinition.value)), 1, num7);
					gameController.AddGold(amount);
					ShowEconomyBanner("이자 수익 +" + amount + "G", augmentDefinition.accentColor, 1.7f, 1.2f);
				}
			}
		}
	}

	private bool IsBuildupEffect(AugmentEffectType effectType)
	{
		return effectType == AugmentEffectType.ScalingAttackPowerPerRound || effectType == AugmentEffectType.ScalingAttackSpeedPerRound || effectType == AugmentEffectType.ScalingSkillPowerPerRound || effectType == AugmentEffectType.ScalingManaRegenPerRound || effectType == AugmentEffectType.ScalingBossDamagePerRound || effectType == AugmentEffectType.ScalingGoldPerRound;
	}

	private void IncrementBuildup(AugmentDefinition augment)
	{
		if (augment != null && !string.IsNullOrEmpty(augment.id))
		{
			buildupStacks[augment.id] = GetBuildupStacks(augment) + 1;
		}
	}

	private int GetBuildupStacks(AugmentDefinition augment)
	{
		if (augment == null || string.IsNullOrEmpty(augment.id))
		{
			return 0;
		}
		int value;
		return buildupStacks.TryGetValue(augment.id, out value) ? Mathf.Max(0, value) : 0;
	}

	private bool HasChosen(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return false;
		}
		for (int i = 0; i < chosenAugments.Count; i++)
		{
			if (chosenAugments[i] != null && string.Equals(chosenAugments[i].id, id, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private bool CanOfferAugment(AugmentDefinition augment)
	{
		if (augment == null)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(augment.requiredHeroId))
		{
			return CanOfferHeroAugment(augment);
		}
		return augment.effectType switch
		{
			AugmentEffectType.Hero08PetrifyBloom => CanOfferHeroAugment(augment, "hero_08", 0.52f), 
			AugmentEffectType.Hero01VolcanicAftershock => CanOfferHeroAugment(augment, "hero_01", 0.42f), 
			AugmentEffectType.Hero03FrostResidue => CanOfferHeroAugment(augment, "hero_03", 0.42f), 
			AugmentEffectType.Hero05GuardianProtocol => CanOfferHeroAugment(augment, "hero_05", 0.42f), 
			AugmentEffectType.Hero13ManaNetwork => CanOfferHeroAugment(augment, "hero_13", 0.42f), 
			_ => true, 
		};
	}

	private bool CanOfferHeroAugment(AugmentDefinition augment)
	{
		int num = CountFieldHero(augment.requiredHeroId);
		if (num <= 0 || string.IsNullOrEmpty(augment.id))
		{
			return false;
		}
		int num2 = ((pendingChoiceRound > 0) ? pendingChoiceRound : ((!((Object)(object)gameController != (Object)null)) ? 1 : gameController.CurrentRound));
		if (augment.heroTier == HeroAugmentTier.Rare && num2 < rareHeroAugmentUnlockRound)
		{
			return false;
		}
		if (augment.heroTier == HeroAugmentTier.Mythic && num2 < mythicHeroAugmentUnlockRound)
		{
			return false;
		}
		if (!heroAugmentOfferRolls.TryGetValue(augment.id, out var value))
		{
			float heroAugmentBaseOfferChance = GetHeroAugmentBaseOfferChance(augment.heroTier);
			float heroAugmentMaxOfferChance = GetHeroAugmentMaxOfferChance(augment.heroTier);
			float num3 = Mathf.Clamp(heroAugmentBaseOfferChance + (float)Mathf.Max(0, num - 1) * extraHeroCopyOfferBonus, 0f, heroAugmentMaxOfferChance);
			value = Random.value <= num3;
			heroAugmentOfferRolls[augment.id] = value;
		}
		return value;
	}

	private float GetHeroAugmentBaseOfferChance(HeroAugmentTier tier)
	{
		return tier switch
		{
			HeroAugmentTier.Mythic => Mathf.Clamp01(mythicHeroAugmentOfferChance), 
			HeroAugmentTier.Rare => Mathf.Clamp01(rareHeroAugmentOfferChance), 
			_ => Mathf.Clamp01(normalHeroAugmentOfferChance), 
		};
	}

	private float GetHeroAugmentMaxOfferChance(HeroAugmentTier tier)
	{
		return tier switch
		{
			HeroAugmentTier.Mythic => 0.35f, 
			HeroAugmentTier.Rare => 0.6f, 
			_ => 0.85f, 
		};
	}

	private bool CanOfferHeroAugment(AugmentDefinition augment, string heroId, float baseChance)
	{
		int num = CountFieldHero(heroId);
		if (num <= 0 || augment == null || string.IsNullOrEmpty(augment.id))
		{
			return false;
		}
		if (!heroAugmentOfferRolls.TryGetValue(augment.id, out var value))
		{
			float num2 = Mathf.Clamp01(baseChance + (float)Mathf.Max(0, num - 1) * 0.18f);
			value = Random.value <= num2;
			heroAugmentOfferRolls[augment.id] = value;
		}
		return value;
	}

	private int CountFieldHero(string heroId)
	{
		if (string.IsNullOrEmpty(heroId))
		{
			return 0;
		}
		int num = 0;
		DefenderUnit[] array = Object.FindObjectsOfType<DefenderUnit>();
		for (int i = 0; i < array.Length; i++)
		{
			if (IsHero(array[i], heroId))
			{
				num++;
			}
		}
		return num;
	}

	private bool IsHero(DefenderUnit defender, string heroId)
	{
		return (Object)(object)defender != (Object)null && defender.Definition != null && string.Equals(defender.Definition.id, heroId, StringComparison.OrdinalIgnoreCase);
	}

	private void TryResolveHero08PetrifyBloom(DefenderUnit source, MonsterUnit target, AugmentDefinition augment)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (IsHero(source, "hero_08") && !(Random.value > Mathf.Clamp01(augment.value)))
		{
			MonsterUnit.ApplyPetrifyRadius(((Component)target).transform.position, Mathf.Max(0.1f, augment.secondaryValue), new MonsterUnit.PetrifyTargetOptions
			{
				duration = Mathf.Max(0.1f, augment.duration),
				maxTargets = 0,
				excludeBosses = true
			});
		}
	}

	private void TryResolveHero01VolcanicAftershock(DefenderUnit source, MonsterUnit target, float damage, AugmentDefinition augment)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		if (IsHero(source, "hero_01") && !(Random.value > Mathf.Clamp01(augment.value)))
		{
			ApplyHeroAreaDamage(source, target, ((Component)target).transform.position, Mathf.Max(0.1f, augment.duration), damage * Mathf.Max(0f, augment.secondaryValue));
		}
	}

	private void TryResolveHero03FrostResidue(DefenderUnit source, MonsterUnit target, AugmentDefinition augment)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if (IsHero(source, "hero_03") && !(Random.value > Mathf.Clamp01(augment.value)))
		{
			ApplyHeroSlowField(((Component)target).transform.position, Mathf.Max(0.1f, augment.secondaryValue), 0.38f, Mathf.Max(0.1f, augment.duration));
		}
	}

	private void TryResolveHero13ManaNetwork(DefenderUnit source, AugmentDefinition augment)
	{
		if (IsHero(source, "hero_13") && !(Random.value > Mathf.Clamp01(augment.value)))
		{
			RestoreNearbyDefenderMana(source, Mathf.Max(0.1f, augment.secondaryValue), Mathf.Max(0.01f, augment.duration));
		}
	}

	private void ApplyHeroAreaDamage(DefenderUnit source, MonsterUnit centerTarget, Vector3 center, float radius, float damage)
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)source == (Object)null || damage <= 0f)
		{
			return;
		}
		float num = radius * radius;
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		resolvingHeroAugmentDamage = true;
		try
		{
			for (int i = 0; i < activeInstances.Count; i++)
			{
				MonsterUnit monsterUnit = activeInstances[i];
				if (!((Object)(object)monsterUnit == (Object)null) && !((Object)(object)monsterUnit == (Object)(object)centerTarget))
				{
					Vector3 val = ((Component)monsterUnit).transform.position - center;
					val.y = 0f;
					if (((Vector3)(ref val)).sqrMagnitude <= num)
					{
						monsterUnit.TakeDamage(damage, critical: false, source);
					}
				}
			}
		}
		finally
		{
			resolvingHeroAugmentDamage = false;
		}
	}

	private void ApplyHeroSlowField(Vector3 center, float radius, float slowRatio, float duration)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		float num = radius * radius;
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if (!((Object)(object)monsterUnit == (Object)null) && !monsterUnit.IsBoss)
			{
				Vector3 val = ((Component)monsterUnit).transform.position - center;
				val.y = 0f;
				if (((Vector3)(ref val)).sqrMagnitude <= num)
				{
					monsterUnit.ApplySlow(Mathf.Clamp01(slowRatio), duration);
				}
			}
		}
	}

	private void RestoreNearbyDefenderMana(DefenderUnit source, float radius, float manaRatio)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)source == (Object)null)
		{
			return;
		}
		float num = radius * radius;
		DefenderUnit[] array = Object.FindObjectsOfType<DefenderUnit>();
		foreach (DefenderUnit defenderUnit in array)
		{
			if (!((Object)(object)defenderUnit == (Object)null))
			{
				Vector3 val = ((Component)defenderUnit).transform.position - ((Component)source).transform.position;
				val.y = 0f;
				if (((Vector3)(ref val)).sqrMagnitude <= num)
				{
					defenderUnit.RestoreMana(manaRatio);
				}
			}
		}
	}

	private void ApplyHero05GuardianProtocol(DefenderUnit[] defenders, AugmentDefinition augment)
	{
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		if (defenders == null || defenders.Length == 0 || augment == null)
		{
			return;
		}
		float num = Mathf.Max(0.1f, augment.secondaryValue);
		float num2 = num * num;
		float duration = Mathf.Max(1f, augment.duration);
		HashSet<DefenderUnit> hashSet = new HashSet<DefenderUnit>();
		foreach (DefenderUnit defenderUnit in defenders)
		{
			if (!IsHero(defenderUnit, "hero_05"))
			{
				continue;
			}
			foreach (DefenderUnit defenderUnit2 in defenders)
			{
				if (!((Object)(object)defenderUnit2 == (Object)null) && !hashSet.Contains(defenderUnit2))
				{
					Vector3 val = ((Component)defenderUnit2).transform.position - ((Component)defenderUnit).transform.position;
					val.y = 0f;
					if (!(((Vector3)(ref val)).sqrMagnitude > num2))
					{
						defenderUnit2.AddShield(defenderUnit2.MaxHealth * Mathf.Max(0f, augment.value), duration);
						hashSet.Add(defenderUnit2);
					}
				}
			}
		}
	}

	private void ResolveHero02SkillAugments(DefenderUnit source, SkillDefinition skill)
	{
		List<DefenderUnit> list = FindLowestHealthDefenders(HasChosen("hero02_emergency_m") ? 3 : 2, includeHealthy: true);
		if (HasChosen("hero02_battle_rx_n"))
		{
			ApplyCombatBoostToDefenders(list, 0.1f, 0.08f, 4f);
		}
		if (HasChosen("hero02_crit_heal_r") && Random.value <= 0.35f)
		{
			ApplyHealAndShieldToDefenders(list, 0.14f, 0.08f, 4f);
		}
		if (HasChosen("hero02_emergency_m"))
		{
			ApplyHealAndShieldToDefenders(list, 0.18f, 0.12f, 5f);
			ApplyCombatBoostToDefenders(list, 0.16f, 0.16f, 5f);
			for (int i = 0; i < list.Count; i++)
			{
				list[i].RestoreMana(0.08f);
			}
		}
	}

	private void ResolveHero04SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		Vector3 center = ResolveSkillCenter(source, target, 2.4f);
		float heroAttackPower = GetHeroAttackPower(source);
		if (HasChosen("hero04_toxic_pool_n"))
		{
			SpawnHeroDamageZone(source, skill, center, 2.3f, heroAttackPower * 0.28f, 3.5f, 0.7f);
			if ((Object)(object)target != (Object)null && target.IsBoss)
			{
				target.TakeDamage(heroAttackPower * 0.4f, critical: false, source);
			}
		}
		if (HasChosen("hero04_plague_cloud_m"))
		{
			ApplyHeroPoisonRadius(source, center, 3.1f, heroAttackPower * 0.36f, 5f, 0.75f, heroAttackPower * 0.9f);
			ApplyHeroSlowField(center, 3.1f, 0.28f, 3f);
		}
	}

	private void ResolveHero06SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
	{
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		float heroAttackPower = GetHeroAttackPower(source);
		if (HasChosen("hero06_spear_throw_n") && Random.value <= 0.35f)
		{
			ApplyHeroLineDamage(source, target, 5.2f, 0.58f, heroAttackPower * 0.85f);
		}
		if (HasChosen("hero06_spear_path_r"))
		{
			int num = ApplyHeroLineDamage(source, target, 5.6f, 0.7f, heroAttackPower * 0.58f);
			if (num >= 2)
			{
				source.ActivateTimedCombatBoost(0.08f, 0.18f, 4f, (GameObject)null, "창 기세", (Color?)new Color(0.78f, 0.88f, 1f));
			}
		}
		if (HasChosen("hero06_storm_lance_m"))
		{
			ApplyHeroLineDamage(source, target, 6.4f, 0.95f, heroAttackPower * 1.25f);
			TriggerSingleSkillEcho(source, skill, 0.48f);
		}
	}

	private void ResolveHero07SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
	{
		if (!((Object)(object)target == (Object)null) && HasChosen("hero07_soul_scythe_n"))
		{
			float heroAttackPower = GetHeroAttackPower(source);
			if (target.IsBoss)
			{
				target.TakeDamage(heroAttackPower * 0.45f, critical: false, source);
				return;
			}
			target.ApplySlow(0.24f, 2.5f);
			target.TakeDamage(Mathf.Min(target.MaxHealth * 0.1f, heroAttackPower * 1.15f), critical: false, source);
		}
	}

	private void ResolveHero01SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		Vector3 center = (((Object)(object)target != (Object)null) ? ((Component)target).transform.position : (((Component)source).transform.position + ((Component)source).transform.forward * 2.5f));
		float num = ((source.Definition != null) ? source.Definition.stats.attackPower : 1f);
		if (HasChosen("hero01_fire_field_n"))
		{
			SpawnHeroDamageZone(source, skill, center, 2.4f, num * 0.35f, 3f, 0.6f);
		}
		if (HasChosen("hero01_flame_pierce_m"))
		{
			ApplyHeroLineDamage(source, target, 5f, 0.65f, num * 1.05f);
		}
	}

	private void ResolveHero03SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
	{
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)target == (Object)null)
		{
			return;
		}
		float num = ((source.Definition != null) ? source.Definition.stats.attackPower : 1f);
		bool flag = HasChosen("hero03_freeze_n") || HasChosen("hero03_permafrost_r") || HasChosen("hero03_shatter_m");
		if (target.IsBoss)
		{
			if (flag)
			{
				target.TakeDamage(num * 0.25f, critical: false, source);
			}
			return;
		}
		if (HasChosen("hero03_freeze_n") && Random.value <= 0.25f)
		{
			target.ApplyStun(1.15f);
		}
		if (HasChosen("hero03_permafrost_r") && Random.value <= 0.2f)
		{
			target.ApplySlow(0.45f, 999f);
			target.ApplyAttackSpeedSlow(0.45f, 999f);
		}
		if (HasChosen("hero03_shatter_m"))
		{
			target.ApplyStun(1.2f);
			((MonoBehaviour)this).StartCoroutine(DelayedHeroAreaDamage(source, ((Component)target).transform.position, 1.2f, 2.8f, num * 0.95f));
		}
	}

	private void ResolveHero08SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)target == (Object)null)
		{
			return;
		}
		float num = ((source.Definition != null) ? source.Definition.stats.attackPower : 1f);
		if (HasChosen("hero08_petrify_spread_n"))
		{
			if (target.IsBoss)
			{
				target.TakeDamage(num * 0.25f, critical: false, source);
			}
			else
			{
				target.ApplyPetrify(2.3f);
			}
		}
		if (HasChosen("hero08_gallery_r") && CountPetrifiedNonBossMonsters() >= 3)
		{
			ApplyHeroAreaDamage(source, null, ((Component)target).transform.position, 3f, num * 1.1f);
		}
		if (HasChosen("hero08_trophy_m") && !target.IsBoss && Random.value <= 0.25f)
		{
			target.ApplyPetrify(3f);
		}
	}

	private void ResolveHero09SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
	{
		float heroAttackPower = GetHeroAttackPower(source);
		if (HasChosen("hero09_front_cleave_n"))
		{
			ApplyHeroLineDamage(source, target, 5.2f, 0.85f, heroAttackPower * 0.72f);
		}
		if (HasChosen("hero09_backline_r"))
		{
			DamageFarthestAdditionalMonsters(source, target, 2, heroAttackPower * 0.72f);
		}
		if (HasChosen("hero09_twin_dragon_m"))
		{
			ApplyHeroLineDamage(source, target, 6.5f, 1.05f, heroAttackPower * 1.1f);
			DamageFarthestAdditionalMonsters(source, target, 3, heroAttackPower * 0.85f);
		}
	}

	private void ResolveHero10SkillAugments(DefenderUnit source, SkillDefinition skill)
	{
		if (CountFieldHero("hero_10") >= 3)
		{
			if (HasChosen("hero10_storm_ritual_m"))
			{
				TriggerHeroSkillEchoes("hero_10", source, skill, 0.8f, 99);
			}
			else if (HasChosen("hero10_chorus_r"))
			{
				TriggerHeroSkillEchoes("hero_10", source, skill, 0.55f, 99);
			}
			else if (HasChosen("hero10_wind_echo_n"))
			{
				TriggerHeroSkillEchoes("hero_10", source, skill, 0.45f, 1);
			}
		}
	}

	private void ResolveHero11SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		float heroAttackPower = GetHeroAttackPower(source);
		if (HasChosen("hero11_blood_bank_n"))
		{
			AddTrackedMaxHealth(source, hero11MaxHealthGrowth, 0.03f, 0.18f);
			source.Heal(source.MaxHealth * 0.08f);
		}
		if (HasChosen("hero11_blood_surge_r"))
		{
			AddTrackedMaxHealth(source, hero11MaxHealthGrowth, 0.05f, 0.3f);
			ApplyHeroAreaDamage(source, target, ResolveSkillCenter(source, target, 2.2f), 2.6f, heroAttackPower * 0.55f);
		}
		if (HasChosen("hero11_crimson_feast_m"))
		{
			AddTrackedMaxHealth(source, hero11MaxHealthGrowth, 0.07f, 0.45f);
			Vector3 center = ResolveSkillCenter(source, target, 2.4f);
			ApplyHeroAreaDamage(source, null, center, 3.2f, heroAttackPower * 0.85f);
			source.Heal(source.MaxHealth * 0.16f);
			source.AddShield(source.MaxHealth * 0.14f, 4f);
		}
	}

	private void ResolveHero12SkillAugments(DefenderUnit source, SkillDefinition skill)
	{
		if (HasChosen("hero12_chain_cast_m"))
		{
			float num = 0.55f;
			int num2 = 0;
			while (num2 < 3 && Random.value <= num)
			{
				TriggerSingleSkillEcho(source, skill, 0.58f);
				source.RestoreMana(0.08f);
				num2++;
				num -= 0.15f;
			}
		}
		else if (HasChosen("hero12_double_cast_r") && Random.value <= 0.35f)
		{
			TriggerSingleSkillEcho(source, skill, 0.62f);
			source.RestoreMana(0.12f);
		}
		else if (HasChosen("hero12_echo_cast_n") && Random.value <= 0.2f)
		{
			TriggerSingleSkillEcho(source, skill, 0.45f);
		}
	}

	private void ResolveHero13SkillAugments(DefenderUnit source)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		if (HasChosen("hero13_overcharge_n"))
		{
			RestoreNearbyDefenderMana(source, 3.2f, 0.08f);
			BoostNearbyDefenders(((Component)source).transform.position, 3.2f, 0.12f, 3f);
		}
		if (HasChosen("hero13_mana_gamble_r") && Random.value <= 0.5f)
		{
			source.RestoreMana(0.5f);
		}
		if (HasChosen("hero13_chain_battery_m"))
		{
			RestoreLowestManaDefenders(((Component)source).transform.position, 5.5f, 0.18f, 3);
		}
	}

	private void ResolveHero14SkillAugments(DefenderUnit source)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (HasChosen("hero14_whole_army_m"))
		{
			List<DefenderUnit> defenders = FindRandomDefenders(99, excludeDead: true);
			ApplyCombatBoostToDefenders(defenders, 0.16f, 0.38f, 6f);
			RestoreLowestManaDefenders(((Component)source).transform.position, 99f, 0.1f, 3);
		}
		else if (HasChosen("hero14_remote_order_r"))
		{
			ApplyCombatBoostToDefenders(FindRandomDefenders(2, excludeDead: true), 0.12f, 0.3f, 5f);
		}
		else if (HasChosen("hero14_remote_buff_n"))
		{
			ApplyCombatBoostToDefenders(FindRandomDefenders(1, excludeDead: true), 0.08f, 0.22f, 5f);
		}
	}

	private void ResolveHero21SkillAugments(DefenderUnit source, SkillDefinition skill)
	{
		if (HasChosen("hero21_alpha_pack_m"))
		{
			source.SpawnAugmentSummonedAllies(3, 0.55f, 0.55f, skill);
			source.ActivateTimedCombatBoost(0.12f, 0.18f, 5f);
		}
		else if (HasChosen("hero21_pack_hunt_r"))
		{
			source.SpawnAugmentSummonedAllies(3, 0.42f, 0.42f, skill);
		}
		else if (HasChosen("hero21_twin_call_n"))
		{
			source.SpawnAugmentSummonedAllies(2, 0.35f, 0.35f, skill);
		}
	}

	private void ResolveHero22SkillAugments(DefenderUnit source, SkillDefinition skill)
	{
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		float heroAttackPower = GetHeroAttackPower(source);
		if (HasChosen("hero22_thorn_shell_n"))
		{
			source.AddShield(source.MaxHealth * 0.18f, 4f);
			source.ActivateTimedDamageReduction(0.16f, 4f);
		}
		if (HasChosen("hero22_reflect_field_r"))
		{
			StartHero54RetaliationWindow(source, 4.5f, 0.35f, 3f, 0f);
			ShieldNearbyDefenders(((Component)source).transform.position, 3f, 0.08f, 4f);
		}
		if (HasChosen("hero22_thorn_crown_m"))
		{
			source.ActivateTimedDamageReduction(0.34f, 5f);
			ShieldNearbyDefenders(((Component)source).transform.position, 3.6f, 0.16f, 5f);
			ApplyHeroAreaDamage(source, null, ((Component)source).transform.position, 3.4f, heroAttackPower * 0.85f);
		}
	}

	private void ResolveHero23SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
	{
		if (!((Object)(object)target == (Object)null))
		{
			float heroAttackPower = GetHeroAttackPower(source);
			if (HasChosen("hero23_power_hit_n"))
			{
				target.TakeDamage(heroAttackPower * 0.75f, critical: false, source);
			}
			if (HasChosen("hero23_weakpoint_r"))
			{
				float damage = ((target.CurrentHealth <= target.MaxHealth * 0.5f) ? (heroAttackPower * 1.35f) : (heroAttackPower * 0.55f));
				target.TakeDamage(damage, target.CurrentHealth <= target.MaxHealth * 0.5f, source);
			}
			if (HasChosen("hero23_royal_break_m"))
			{
				target.TakeDamage(heroAttackPower * 1.3f, critical: true, source);
				DamageNearestAdditionalMonsters(source, target, 3, heroAttackPower * 0.75f);
			}
		}
	}

	private void ResolveHero31SkillAugments(DefenderUnit source)
	{
		if (HasChosen("hero31_battle_rhythm_n"))
		{
			source.ActivateTimedCombatBoost(0.14f, 0.18f, 5f);
		}
		if (HasChosen("hero31_guard_stance_r"))
		{
			source.ActivateTimedDamageReduction(0.3f, 5f);
			source.AddShield(source.MaxHealth * 0.1f, 5f);
		}
		if (HasChosen("hero31_iron_growth_m"))
		{
			source.ActivateTimedCombatBoost(0.22f, 0.22f, 6f);
			source.ActivateTimedDamageReduction(0.35f, 6f);
			AddTrackedMaxHealth(source, hero31MaxHealthGrowth, 0.04f, 0.28f);
		}
	}

	private void ResolveHero32SkillAugments(DefenderUnit source, MonsterUnit target)
	{
		float heroAttackPower = GetHeroAttackPower(source);
		source.ActivateTimedCombatBoost(0f, 0.25f, 5f);
		if ((Object)(object)target != (Object)null && target.CurrentHealth > 0f)
		{
			target.ApplyPoison(heroAttackPower * 0.3f, 4f, 1f, source);
		}
		if (HasChosen("hero32_predator_rhythm_n"))
		{
			source.ActivateTimedCombatBoost(0.1f, 0.2f, 5f);
		}
		if (HasChosen("hero32_pack_hunt_r"))
		{
			DamageRandomMonsters(source, 2, GetHeroAttackPower(source) * 0.6f);
		}
		if (HasChosen("hero32_alpha_hunt_m"))
		{
			source.ActivateTimedCombatBoost(0.16f, 0.28f, 6f);
			DamageRandomMonsters(source, 4, GetHeroAttackPower(source) * 0.7f);
		}
	}

	private void ResolveHero33SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		Vector3 center = ResolveSkillCenter(source, target, 2.6f);
		float heroAttackPower = GetHeroAttackPower(source);
		if (HasChosen("hero33_poison_remnant_n"))
		{
			SpawnHeroDamageZone(source, skill, center, 2.7f, heroAttackPower * 0.32f, 4f, 0.8f);
		}
		if (HasChosen("hero33_wandering_mist_r"))
		{
			((MonoBehaviour)this).StartCoroutine(DelayedRandomHeroDamageZones(source, skill, 4f, 1, 2.4f, heroAttackPower * 0.34f, 3.2f, 0.8f));
		}
		if (HasChosen("hero33_endless_miasma_m"))
		{
			SpawnHeroDamageZone(source, skill, center, 3.2f, heroAttackPower * 0.46f, 5f, 0.75f);
			ApplyHeroSlowField(center, 3.2f, 0.28f, 3.5f);
			((MonoBehaviour)this).StartCoroutine(DelayedRandomHeroDamageZones(source, skill, 3.5f, 2, 2.6f, heroAttackPower * 0.38f, 3.2f, 0.75f));
		}
	}

	private void ResolveHero51SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
	{
		float num = ((source.Definition != null) ? source.Definition.stats.attackPower : 1f);
		if (HasChosen("hero51_chain_lightning_n"))
		{
			DamageNearestAdditionalMonsters(source, target, 2, num * 0.45f);
		}
		if (HasChosen("hero51_overload_circuit_m"))
		{
			float num2 = 0.45f;
			int num3 = 0;
			while (num3 < 5 && Random.value <= num2)
			{
				TriggerSingleSkillEcho(source, skill, 0.42f);
				num3++;
				num2 -= 0.1f;
			}
		}
		else if (HasChosen("hero51_residual_lightning_r") && Random.value <= 0.3f)
		{
			TriggerSingleSkillEcho(source, skill, 0.45f);
		}
	}

	private void ResolveHero52SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = (((Object)(object)target != (Object)null) ? ((Component)target).transform.position : (((Component)source).transform.position + ((Component)source).transform.forward * 2.4f));
		float num = ((source.Definition != null) ? source.Definition.stats.attackPower : 1f);
		if (HasChosen("hero52_burning_fallout_n"))
		{
			SpawnHeroDamageZone(source, skill, val, 2.5f, num * 0.32f, 3.5f, 0.7f);
		}
		if (HasChosen("hero52_star_shower_m"))
		{
			for (int i = 0; i < 2; i++)
			{
				Vector3 val2 = Random.insideUnitSphere * 2.2f;
				val2.y = 0f;
				SpawnHeroDamageZone(source, skill, val + val2, 1.6f, num * 0.28f, 2.2f, 0.7f);
			}
		}
	}

	private void ResolveHero54SkillAugments(DefenderUnit source, SkillDefinition skill)
	{
		if (HasChosen("hero54_fortress_m"))
		{
			StartHero54RetaliationWindow(source, 5f, 0.5f, 3.4f, 0.2f);
			source.ActivateTimedDamageReduction(0.42f, 5f);
		}
		else if (HasChosen("hero54_retribution_r"))
		{
			StartHero54RetaliationWindow(source, 4f, 0.5f, 3f, 0f);
			source.ActivateTimedDamageReduction(0.32f, 4f);
		}
		else if (HasChosen("hero54_stone_skin_n"))
		{
			source.ActivateTimedDamageReduction(0.25f, 4f);
		}
	}

	private void ResolveHero55SkillAugments(DefenderUnit source, MonsterUnit target)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		float heroAttackPower = GetHeroAttackPower(source);
		Vector3 center = ResolveSkillCenter(source, target, 2f);
		if (HasChosen("hero55_impact_guard_n"))
		{
			source.AddShield(source.MaxHealth * 0.12f, 5f);
		}
		if (HasChosen("hero55_wall_quake_r"))
		{
			ApplyHeroAreaDamage(source, target, center, 3f, heroAttackPower * 0.9f);
		}
		if (HasChosen("hero55_mobile_fortress_m"))
		{
			ApplyHeroAreaDamage(source, target, center, 3.4f, heroAttackPower * 1.4f);
			source.AddShield(source.MaxHealth * 0.2f, 6f);
			source.ActivateTimedDamageReduction(0.3f, 6f);
		}
	}

	private void ResolveHero56SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		float heroAttackPower = GetHeroAttackPower(source);
		Vector3 center = ResolveSkillCenter(source, target, 3f);
		if (HasChosen("hero56_after_blast_n"))
		{
			ApplyHeroAreaDamage(source, target, center, 3f, heroAttackPower * 0.75f);
		}
		if (HasChosen("hero56_twin_barrage_r"))
		{
			TriggerSingleSkillEcho(source, skill, 0.5f);
		}
		if (HasChosen("hero56_orbital_restrike_m"))
		{
			TriggerSingleSkillEcho(source, skill, 0.8f);
			ApplyHeroAreaDamage(source, target, center, 3.6f, heroAttackPower * 1.2f);
		}
	}

	private void ResolveHero57SkillAugments(DefenderUnit source)
	{
		float heroAttackPower = GetHeroAttackPower(source);
		if (HasChosen("hero57_spare_mag_n"))
		{
			DamageRandomMonsters(source, 2, heroAttackPower * 0.7f);
		}
		if (HasChosen("hero57_ricochet_r"))
		{
			DamageRandomMonsters(source, 4, heroAttackPower * 0.8f);
		}
		if (HasChosen("hero57_chaos_mag_m"))
		{
			DamageRandomMonsters(source, 6, heroAttackPower * 0.9f);
		}
	}

	private void TriggerHeroSkillEchoes(string heroId, DefenderUnit originalSource, SkillDefinition skill, float multiplier, int maxEchoes)
	{
		List<DefenderUnit> list = FindFieldHeroes(heroId);
		int num = 0;
		resolvingHeroSkillEcho = true;
		try
		{
			for (int i = 0; i < list.Count; i++)
			{
				DefenderUnit defenderUnit = list[i];
				if (!((Object)(object)defenderUnit == (Object)null) && !((Object)(object)defenderUnit == (Object)(object)originalSource))
				{
					defenderUnit.TriggerAugmentSkillEcho(skill, multiplier);
					num++;
					if (num >= maxEchoes)
					{
						break;
					}
				}
			}
		}
		finally
		{
			resolvingHeroSkillEcho = false;
		}
	}

	private void TriggerSingleSkillEcho(DefenderUnit source, SkillDefinition skill, float multiplier)
	{
		resolvingHeroSkillEcho = true;
		try
		{
			source.TriggerAugmentSkillEcho(skill, multiplier);
		}
		finally
		{
			resolvingHeroSkillEcho = false;
		}
	}

	private List<DefenderUnit> FindFieldHeroes(string heroId)
	{
		List<DefenderUnit> list = new List<DefenderUnit>();
		DefenderUnit[] array = Object.FindObjectsOfType<DefenderUnit>();
		for (int i = 0; i < array.Length; i++)
		{
			if (IsHero(array[i], heroId))
			{
				list.Add(array[i]);
			}
		}
		return list;
	}

	private void BoostNearbyDefenders(Vector3 center, float radius, float attackSpeedRatio, float duration)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		float num = radius * radius;
		DefenderUnit[] array = Object.FindObjectsOfType<DefenderUnit>();
		foreach (DefenderUnit defenderUnit in array)
		{
			if (!((Object)(object)defenderUnit == (Object)null))
			{
				Vector3 val = ((Component)defenderUnit).transform.position - center;
				val.y = 0f;
				if (((Vector3)(ref val)).sqrMagnitude <= num)
				{
					defenderUnit.ActivateTimedCombatBoost(0f, attackSpeedRatio, duration);
				}
			}
		}
	}

	private void ShieldNearbyDefenders(Vector3 center, float radius, float shieldRatio, float duration)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		float num = radius * radius;
		DefenderUnit[] array = Object.FindObjectsOfType<DefenderUnit>();
		foreach (DefenderUnit defenderUnit in array)
		{
			if (!((Object)(object)defenderUnit == (Object)null))
			{
				Vector3 val = ((Component)defenderUnit).transform.position - center;
				val.y = 0f;
				if (((Vector3)(ref val)).sqrMagnitude <= num)
				{
					defenderUnit.AddShield(defenderUnit.MaxHealth * shieldRatio, duration);
				}
			}
		}
	}

	private void RestoreLowestManaDefenders(Vector3 center, float radius, float manaRatio, int count, DefenderUnit excludedDefender = null)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		List<DefenderUnit> list = new List<DefenderUnit>();
		float num = radius * radius;
		DefenderUnit[] array = Object.FindObjectsOfType<DefenderUnit>();
		foreach (DefenderUnit defenderUnit in array)
		{
			if (!((Object)(object)defenderUnit == (Object)null) && !((Object)(object)defenderUnit == (Object)(object)excludedDefender) && !(defenderUnit.MaxMana <= 0f) && !(defenderUnit.CurrentHealth <= 0f))
			{
				Vector3 val = ((Component)defenderUnit).transform.position - center;
				val.y = 0f;
				if (((Vector3)(ref val)).sqrMagnitude <= num)
				{
					list.Add(defenderUnit);
				}
			}
		}
		list.Sort((DefenderUnit a, DefenderUnit b) => (a.CurrentMana / Mathf.Max(1f, a.MaxMana)).CompareTo(b.CurrentMana / Mathf.Max(1f, b.MaxMana)));
		int num2 = Mathf.Min(Mathf.Max(1, count), list.Count);
		for (int num3 = 0; num3 < num2; num3++)
		{
			list[num3].RestoreMana(manaRatio);
		}
	}

	private List<DefenderUnit> FindLowestHealthDefenders(int count, bool includeHealthy)
	{
		List<DefenderUnit> list = new List<DefenderUnit>();
		DefenderUnit[] array = Object.FindObjectsOfType<DefenderUnit>();
		foreach (DefenderUnit defenderUnit in array)
		{
			if (!((Object)(object)defenderUnit == (Object)null) && !(defenderUnit.CurrentHealth <= 0f) && (includeHealthy || !(defenderUnit.HealthRatio >= 0.98f)))
			{
				list.Add(defenderUnit);
			}
		}
		list.Sort((DefenderUnit a, DefenderUnit b) => a.HealthRatio.CompareTo(b.HealthRatio));
		int num = Mathf.Min(Mathf.Max(1, count), list.Count);
		if (num < list.Count)
		{
			list.RemoveRange(num, list.Count - num);
		}
		return list;
	}

	private List<DefenderUnit> FindRandomDefenders(int count, bool excludeDead)
	{
		List<DefenderUnit> list = new List<DefenderUnit>();
		DefenderUnit[] array = Object.FindObjectsOfType<DefenderUnit>();
		foreach (DefenderUnit defenderUnit in array)
		{
			if (!((Object)(object)defenderUnit == (Object)null) && (!excludeDead || !(defenderUnit.CurrentHealth <= 0f)))
			{
				list.Add(defenderUnit);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			int index = Random.Range(j, list.Count);
			DefenderUnit value = list[j];
			list[j] = list[index];
			list[index] = value;
		}
		int num = Mathf.Min(Mathf.Max(1, count), list.Count);
		if (num < list.Count)
		{
			list.RemoveRange(num, list.Count - num);
		}
		return list;
	}

	private void ApplyCombatBoostToDefenders(List<DefenderUnit> defenders, float attackPowerRatio, float attackSpeedRatio, float duration)
	{
		if (defenders == null)
		{
			return;
		}
		for (int i = 0; i < defenders.Count; i++)
		{
			if ((Object)(object)defenders[i] != (Object)null)
			{
				defenders[i].ActivateTimedCombatBoost(attackPowerRatio, attackSpeedRatio, duration);
			}
		}
	}

	private void ApplyHealAndShieldToDefenders(List<DefenderUnit> defenders, float healRatio, float shieldRatio, float shieldDuration)
	{
		if (defenders == null)
		{
			return;
		}
		for (int i = 0; i < defenders.Count; i++)
		{
			DefenderUnit defenderUnit = defenders[i];
			if (!((Object)(object)defenderUnit == (Object)null))
			{
				defenderUnit.Heal(defenderUnit.MaxHealth * Mathf.Max(0f, healRatio));
				if (shieldRatio > 0f)
				{
					defenderUnit.AddShield(defenderUnit.MaxHealth * shieldRatio, shieldDuration);
				}
			}
		}
	}

	private void AddTrackedMaxHealth(DefenderUnit source, Dictionary<DefenderUnit, float> tracker, float ratio, float cap)
	{
		if (!((Object)(object)source == (Object)null) && tracker != null && !(ratio <= 0f) && !(cap <= 0f))
		{
			float value;
			float num = (tracker.TryGetValue(source, out value) ? value : 0f);
			float num2 = Mathf.Min(ratio, Mathf.Max(0f, cap - num));
			if (!(num2 <= 0f))
			{
				tracker[source] = num + num2;
				source.AddMaxHealthBonus(num2);
			}
		}
	}

	private float GetHeroAttackPower(DefenderUnit source)
	{
		return ((Object)(object)source != (Object)null && source.Definition != null) ? Mathf.Max(1f, source.Definition.stats.attackPower) : 1f;
	}

	private Vector3 ResolveSkillCenter(DefenderUnit source, MonsterUnit target, float forwardDistance)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)target != (Object)null)
		{
			return ((Component)target).transform.position;
		}
		if ((Object)(object)source != (Object)null)
		{
			return ((Component)source).transform.position + ((Component)source).transform.forward * Mathf.Max(0.5f, forwardDistance);
		}
		return Vector3.zero;
	}

	private Vector3 ResolveRandomMonsterPosition(DefenderUnit source, float fallbackForwardDistance)
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		List<MonsterUnit> list = new List<MonsterUnit>();
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			if ((Object)(object)activeInstances[i] != (Object)null && activeInstances[i].CanBeCombatTargeted)
			{
				list.Add(activeInstances[i]);
			}
		}
		if (list.Count > 0)
		{
			return ((Component)list[Random.Range(0, list.Count)]).transform.position;
		}
		return ((Object)(object)source != (Object)null) ? (((Component)source).transform.position + ((Component)source).transform.forward * Mathf.Max(0.5f, fallbackForwardDistance)) : Vector3.zero;
	}

	private void ApplyHeroPoisonRadius(DefenderUnit source, Vector3 center, float radius, float damagePerTick, float duration, float tickInterval, float bossDamage)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		float num = radius * radius;
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if ((Object)(object)monsterUnit == (Object)null || !monsterUnit.CanBeCombatTargeted)
			{
				continue;
			}
			Vector3 val = ((Component)monsterUnit).transform.position - center;
			val.y = 0f;
			if (!(((Vector3)(ref val)).sqrMagnitude > num))
			{
				if (monsterUnit.IsBoss)
				{
					monsterUnit.TakeDamage(bossDamage, critical: false, source);
				}
				else
				{
					monsterUnit.ApplyPoison(damagePerTick, duration, tickInterval, source);
				}
			}
		}
	}

	private bool HasAnyChosen(params string[] ids)
	{
		if (ids == null)
		{
			return false;
		}
		for (int i = 0; i < ids.Length; i++)
		{
			if (HasChosen(ids[i]))
			{
				return true;
			}
		}
		return false;
	}

	private void ResolveHero07Kill(DefenderUnit killer)
	{
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)killer == (Object)null) && HasAnyChosen("hero07_soul_scythe_n", "hero07_reaper_form_r", "hero07_reaper_execute_m"))
		{
			int value;
			int num = ((!hero07KillCounts.TryGetValue(killer, out value)) ? 1 : (value + 1));
			hero07KillCounts[killer] = num;
			if (HasChosen("hero07_soul_scythe_n") && num % 3 == 0)
			{
				killer.RestoreMana(0.12f);
			}
			if ((HasChosen("hero07_reaper_form_r") || HasChosen("hero07_reaper_execute_m")) && num >= 10 && !hero07ReaperActive.Contains(killer))
			{
				hero07ReaperActive.Add(killer);
				hero07StrikeCounts[killer] = 0;
				killer.ActivateTimedCombatBoost(0.35f, 0.45f, 24f, (GameObject)null, "사신화", (Color?)new Color(0.62f, 0.18f, 0.86f));
			}
		}
	}

	private void ResolveHero07ReaperStrike(DefenderUnit source, MonsterUnit target)
	{
		if ((Object)(object)source == (Object)null || (Object)(object)target == (Object)null || !HasChosen("hero07_reaper_execute_m") || !hero07ReaperActive.Contains(source))
		{
			return;
		}
		int value;
		int num = ((!hero07StrikeCounts.TryGetValue(source, out value)) ? 1 : (value + 1));
		hero07StrikeCounts[source] = num;
		if (num % 3 != 0)
		{
			return;
		}
		float heroAttackPower = GetHeroAttackPower(source);
		resolvingHeroAugmentDamage = true;
		try
		{
			if (target.IsBoss)
			{
				target.TakeDamage(heroAttackPower * 2f, critical: true, source);
				return;
			}
			float damage = ((target.CurrentHealth <= target.MaxHealth * 0.45f) ? (target.MaxHealth * 2.5f) : (heroAttackPower * 1.35f));
			target.TakeDamage(damage, critical: true, source);
		}
		finally
		{
			resolvingHeroAugmentDamage = false;
		}
	}

	private int CountPetrifiedNonBossMonsters()
	{
		int num = 0;
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if ((Object)(object)monsterUnit != (Object)null && !monsterUnit.IsBoss && monsterUnit.IsPetrified)
			{
				num++;
			}
		}
		return num;
	}

	private void DamageNearestAdditionalMonsters(DefenderUnit source, MonsterUnit primaryTarget, int count, float damage)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		List<MonsterUnit> list = new List<MonsterUnit>();
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if ((Object)(object)monsterUnit != (Object)null && (Object)(object)monsterUnit != (Object)(object)primaryTarget && monsterUnit.CanBeCombatTargeted)
			{
				list.Add(monsterUnit);
			}
		}
		Vector3 origin = (((Object)(object)primaryTarget != (Object)null) ? ((Component)primaryTarget).transform.position : ((Component)source).transform.position);
		list.Sort((MonsterUnit a, MonsterUnit b) => Vector3.SqrMagnitude(((Component)a).transform.position - origin).CompareTo(Vector3.SqrMagnitude(((Component)b).transform.position - origin)));
		int num = Mathf.Min(Mathf.Max(0, count), list.Count);
		for (int num2 = 0; num2 < num; num2++)
		{
			list[num2].TakeDamage(damage, critical: false, source);
		}
	}

	private void DamageRandomMonsters(DefenderUnit source, int shotCount, float damage)
	{
		if ((Object)(object)source == (Object)null || shotCount <= 0 || damage <= 0f)
		{
			return;
		}
		List<MonsterUnit> list = new List<MonsterUnit>();
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if ((Object)(object)monsterUnit != (Object)null && monsterUnit.CanBeCombatTargeted)
			{
				list.Add(monsterUnit);
			}
		}
		for (int j = 0; j < shotCount; j++)
		{
			if (list.Count <= 0)
			{
				break;
			}
			MonsterUnit monsterUnit2 = list[Random.Range(0, list.Count)];
			monsterUnit2.TakeDamage(damage, critical: false, source);
		}
	}

	private void DamageFarthestAdditionalMonsters(DefenderUnit source, MonsterUnit primaryTarget, int count, float damage)
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		List<MonsterUnit> list = new List<MonsterUnit>();
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if ((Object)(object)monsterUnit != (Object)null && (Object)(object)monsterUnit != (Object)(object)primaryTarget && monsterUnit.CanBeCombatTargeted)
			{
				list.Add(monsterUnit);
			}
		}
		Vector3 origin = (((Object)(object)source != (Object)null) ? ((Component)source).transform.position : (((Object)(object)primaryTarget != (Object)null) ? ((Component)primaryTarget).transform.position : Vector3.zero));
		list.Sort((MonsterUnit a, MonsterUnit b) => Vector3.SqrMagnitude(((Component)b).transform.position - origin).CompareTo(Vector3.SqrMagnitude(((Component)a).transform.position - origin)));
		int num = Mathf.Min(Mathf.Max(0, count), list.Count);
		for (int num2 = 0; num2 < num; num2++)
		{
			list[num2].TakeDamage(damage, critical: false, source);
		}
	}

	private void PoisonNearestMonster(DefenderUnit source, Vector3 origin, float damagePerTick, float duration, float tickInterval)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		MonsterUnit monsterUnit = null;
		float num = float.MaxValue;
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit2 = activeInstances[i];
			if (!((Object)(object)monsterUnit2 == (Object)null) && !monsterUnit2.IsBoss && monsterUnit2.CanBeCombatTargeted)
			{
				float num2 = Vector3.SqrMagnitude(((Component)monsterUnit2).transform.position - origin);
				if (num2 < num)
				{
					num = num2;
					monsterUnit = monsterUnit2;
				}
			}
		}
		if ((Object)(object)monsterUnit != (Object)null)
		{
			monsterUnit.ApplyPoison(damagePerTick, duration, tickInterval, source);
		}
	}

	private int ApplyHeroLineDamage(DefenderUnit source, MonsterUnit anchorTarget, float length, float halfWidth, float damage)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)source == (Object)null || damage <= 0f)
		{
			return 0;
		}
		Vector3 val = (((Object)(object)anchorTarget != (Object)null) ? (((Component)anchorTarget).transform.position - ((Component)source).transform.position) : ((Component)source).transform.forward);
		val.y = 0f;
		if (((Vector3)(ref val)).sqrMagnitude <= 0.0001f)
		{
			val = ((Component)source).transform.forward;
		}
		((Vector3)(ref val)).Normalize();
		IReadOnlyList<MonsterUnit> activeInstances = MonsterUnit.ActiveInstances;
		int num = 0;
		for (int i = 0; i < activeInstances.Count; i++)
		{
			MonsterUnit monsterUnit = activeInstances[i];
			if ((Object)(object)monsterUnit == (Object)null || !monsterUnit.CanBeCombatTargeted)
			{
				continue;
			}
			Vector3 val2 = ((Component)monsterUnit).transform.position - ((Component)source).transform.position;
			val2.y = 0f;
			float num2 = Vector3.Dot(val2, val);
			if (!(num2 < 0f) && !(num2 > length))
			{
				Vector3 val3 = val * num2;
				Vector3 val4 = val2 - val3;
				if (((Vector3)(ref val4)).magnitude <= halfWidth)
				{
					monsterUnit.TakeDamage(damage, critical: false, source);
					num++;
				}
			}
		}
		return num;
	}

	private void SpawnHeroDamageZone(DefenderUnit source, SkillDefinition skill, Vector3 center, float radius, float damagePerTick, float duration, float tickInterval)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		((MonoBehaviour)this).StartCoroutine(HeroDamageZoneRoutine(source, skill, center, radius, damagePerTick, duration, tickInterval));
	}

	private IEnumerator HeroDamageZoneRoutine(DefenderUnit source, SkillDefinition skill, Vector3 center, float radius, float damagePerTick, float duration, float tickInterval)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		float elapsed = 0f;
		float interval = Mathf.Max(0.15f, tickInterval);
		float checkedRadius = Mathf.Max(0.2f, radius);
		for (; elapsed < duration; elapsed += interval)
		{
			IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
			for (int i = 0; i < monsters.Count; i++)
			{
				MonsterUnit monster = monsters[i];
				if ((Object)(object)monster != (Object)null && Vector3.Distance(center, ((Component)monster).transform.position) <= checkedRadius)
				{
					DefenderUnit.RunWithSkillDamageContext(skill, delegate
					{
						monster.TakeDamage(damagePerTick, critical: false, source);
					});
				}
			}
			yield return (object)new WaitForSeconds(interval);
		}
	}

	private IEnumerator DelayedHeroAreaDamage(DefenderUnit source, Vector3 center, float delay, float radius, float damage)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		yield return (object)new WaitForSeconds(Mathf.Max(0f, delay));
		ApplyHeroAreaDamage(source, null, center, radius, damage);
	}

	private IEnumerator DelayedRandomHeroDamageZones(DefenderUnit source, SkillDefinition skill, float delay, int count, float radius, float damagePerTick, float duration, float tickInterval)
	{
		yield return (object)new WaitForSeconds(Mathf.Max(0f, delay));
		int checkedCount = Mathf.Clamp(count, 1, 4);
		for (int i = 0; i < checkedCount; i++)
		{
			Vector3 center = ResolveRandomMonsterPosition(source, 3f + (float)i * 0.45f);
			SpawnHeroDamageZone(source, skill, center, radius, damagePerTick, duration, tickInterval);
		}
	}

	private void StartHero54RetaliationWindow(DefenderUnit source, float duration, float returnRatio, float radius, float shieldShareRatio)
	{
		hero54StoredDamage[source] = 0f;
		((MonoBehaviour)this).StartCoroutine(ResolveHero54Retaliation(source, duration, returnRatio, radius, shieldShareRatio));
	}

	private IEnumerator ResolveHero54Retaliation(DefenderUnit source, float duration, float returnRatio, float radius, float shieldShareRatio)
	{
		yield return (object)new WaitForSeconds(Mathf.Max(0.1f, duration));
		if ((Object)(object)source == (Object)null)
		{
			yield break;
		}
		float value;
		float storedDamage = (hero54StoredDamage.TryGetValue(source, out value) ? value : 0f);
		hero54StoredDamage.Remove(source);
		if (!(storedDamage <= 0f))
		{
			ApplyHeroAreaDamage(source, null, ((Component)source).transform.position, radius, storedDamage * returnRatio);
			if (shieldShareRatio > 0f)
			{
				ShieldNearbyDefenders(((Component)source).transform.position, radius, storedDamage * shieldShareRatio / Mathf.Max(1f, source.MaxHealth), 4f);
			}
		}
	}

	private void ShowRandomGoldFeedback(AugmentDefinition augment, int reward, int minGold, int maxGold, float triggerChance = 0f)
	{
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		if (augment != null)
		{
			int num = Mathf.Max(0, minGold);
			int num2 = Mathf.Max(num, maxGold);
			int num3 = Mathf.Clamp(reward, num, num2);
			string text = ((triggerChance > 0f) ? (" (" + Mathf.RoundToInt(Mathf.Clamp01(triggerChance) * 100f) + "% 발동)") : string.Empty);
			string text2 = ((num3 > 0) ? ("+" + num3 + "G") : "꽝! 0G");
			string text3 = ((num2 > num) ? (" / 최대 " + num2 + "G") : string.Empty);
			ShowEconomyBanner(augment.title + text + "  " + text2 + text3, augment.accentColor, 1.8f, 0f);
			if (IsGoldJackpot(num3, num, num2))
			{
				string detail = ((triggerChance > 0f) ? ("발동 확률 " + Mathf.RoundToInt(Mathf.Clamp01(triggerChance) * 100f) + "% / 최대 " + num2 + "G") : (augment.title + " / 최대 " + num2 + "G 중 상위 20%"));
				RuntimeAudioUtility.PlayJackpotMajor();
				RuntimeGameFeel.ShowJackpotReveal("골드 대박!", "JACKPOT", "+" + num3 + "G", Color.Lerp(augment.accentColor, new Color(1f, 0.88f, 0.18f), 0.45f), detail, 2.6f);
			}
		}
	}

	private static bool IsGoldJackpot(int reward, int minGold, int maxGold)
	{
		if (reward < 8 || maxGold <= minGold)
		{
			return false;
		}
		int num = Mathf.CeilToInt(Mathf.Lerp((float)minGold, (float)maxGold, 0.8f));
		return reward >= num;
	}

	private void ShowEconomyBanner(string message, Color color, float duration, float cooldown)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)gameController == (Object)null) && (!(cooldown > 0f) || !(Time.time < nextEconomyBannerTime)))
		{
			nextEconomyBannerTime = Time.time + Mathf.Max(0f, cooldown);
			gameController.RequestBanner(message, color, duration);
		}
	}

	private void EnsureChoiceSchedule()
	{
		if (nextChoiceRound <= 0)
		{
			nextChoiceRound = Mathf.Max(1, firstChoiceRound);
		}
	}

	private void ScheduleNextChoice(int completedRound)
	{
		nextChoiceRound = ResolveNextFixedChoiceRound(completedRound);
	}

	private int ResolveNextFixedChoiceRound(int completedRound)
	{
		int num = Mathf.Max(1, firstChoiceRound);
		int num2 = ResolveFixedChoiceInterval();
		int num3 = Mathf.Max(0, completedRound);
		if (num3 < num)
		{
			return num;
		}
		int num4 = Mathf.FloorToInt((float)(num3 - num) / (float)num2) + 1;
		return num + num4 * num2;
	}

	private int ResolveFixedChoiceInterval()
	{
		int num = Mathf.Max(1, minChoiceInterval);
		int num2 = Mathf.Max(num, maxChoiceInterval);
		return Mathf.RoundToInt((float)(num + num2) * 0.5f);
	}

	private string GetStyleLabel(AugmentStyle style)
	{
		return style switch
		{
			AugmentStyle.Stable => "안정", 
			AugmentStyle.Growth => "성장", 
			AugmentStyle.Gamble => "도박", 
			_ => "빌드", 
		};
	}

	private string GetChoiceLabel(AugmentDefinition augment)
	{
		if (augment != null && augment.heroTier != HeroAugmentTier.None)
		{
			if (augment.heroTier == HeroAugmentTier.Mythic)
			{
				return "유닛 전용 · 신화";
			}
			if (augment.heroTier == HeroAugmentTier.Rare)
			{
				return "유닛 전용 · 희귀";
			}
			return "유닛 전용 · 일반";
		}
		return (augment != null) ? GetStyleLabel(augment.style) : string.Empty;
	}

	private void EnsureDefaultPool()
	{
		if (augmentPool.Count == 0)
		{
			AddStableAugments();
			AddGrowthAugments();
			AddGambleAugments();
			AddBuildupAugments();
		}
		RefreshGeneralAugmentPool();
	}

	private void RefreshGeneralAugmentPool()
	{
		string[] array = new string[16]
		{
			"sniper_window", "panic_barrier", "senior_miner", "union_mine", "steady_contract", "coupon_storm", "first_wave_bonus", "jackpot", "risky_vault", "bounty_flip",
			"mini_lotto", "rare_bounty", "boss_bet", "overclock_growth", "fever_engine", "boss_tax_account"
		};
		for (int i = 0; i < array.Length; i++)
		{
			RemoveAugmentById(array[i]);
		}
		AddTacticalGeneralAugments();
	}

	private void AddTacticalGeneralAugments()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		AddAugmentIfMissing(CreateAugment("devils_firepower_contract", "악마의 화력 계약", "모든 유닛 공격력 +30%, 최대 체력 -20%. 버티는 힘을 팔아 빠른 처치를 노립니다.", AugmentStyle.Gamble, AugmentEffectType.AttackHealthTradeoff, 0.3f, 0.2f, 0f, new Color(1f, 0.3f, 0.24f)));
		AddAugmentIfMissing(CreateAugment("execution_protocol", "처형 프로토콜", "남은 체력 25% 이하 적에게 직전 피해의 35%를 추가로 줍니다. 보스는 12%만 적용됩니다.", AugmentStyle.Stable, AugmentEffectType.ExecuteDamage, 0.25f, 0.35f, 0.12f, new Color(1f, 0.58f, 0.18f)));
		AddAugmentIfMissing(CreateAugment("cliff_edge_counter", "벼랑 끝 반격", "체력이 40% 이하인 유닛은 적에게 주는 피해가 45% 증가합니다.", AugmentStyle.Gamble, AugmentEffectType.LowHealthFury, 0.4f, 0.45f, 0f, new Color(1f, 0.34f, 0.42f)));
		AddAugmentIfMissing(CreateAugment("critical_reload", "치명타 재장전", "치명타 적중 시 18% 확률로 직전 피해의 70%를 한 번 더 줍니다.", AugmentStyle.Gamble, AugmentEffectType.CriticalDoubleTap, 0.18f, 0.7f, 0f, new Color(1f, 0.82f, 0.24f)));
		AddAugmentIfMissing(CreateAugment("mana_relay", "마나 릴레이", "아군이 스킬을 쓸 때마다 마나가 가장 낮은 다른 아군 1명이 마나 6%를 회복합니다.", AugmentStyle.Growth, AugmentEffectType.SkillManaRelay, 0.06f, 0f, 0f, new Color(0.34f, 0.88f, 1f)));
		AddAugmentIfMissing(CreateAugment("skill_chain_reactor", "연쇄 반응로", "아군의 매 5번째 스킬이 대상 주변 2.4m에 시전자 공격력 115% 피해를 줍니다.", AugmentStyle.Growth, AugmentEffectType.SkillChainBlast, 5f, 1.15f, 2.4f, new Color(0.72f, 0.46f, 1f)));
	}

	private void EnsureHeroSpecificAugments()
	{
		RemoveHeroAugmentDrafts();
		AddHeroAugmentSet();
	}

	private void RemoveHeroAugmentDrafts()
	{
		RemoveAugmentById("hero08_stone_bloom");
		RemoveAugmentById("hero01_volcanic_aftershock");
		RemoveAugmentById("hero03_frost_residue");
		RemoveAugmentById("hero05_guardian_protocol");
		RemoveAugmentById("hero13_mana_network");
	}

	private void AddHeroAugmentSet()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0416: Unknown result type (might be due to invalid IL or missing references)
		//IL_0446: Unknown result type (might be due to invalid IL or missing references)
		//IL_0476: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0506: Unknown result type (might be due to invalid IL or missing references)
		//IL_0536: Unknown result type (might be due to invalid IL or missing references)
		//IL_0566: Unknown result type (might be due to invalid IL or missing references)
		//IL_0596: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0626: Unknown result type (might be due to invalid IL or missing references)
		//IL_0656: Unknown result type (might be due to invalid IL or missing references)
		//IL_0686: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0716: Unknown result type (might be due to invalid IL or missing references)
		//IL_0746: Unknown result type (might be due to invalid IL or missing references)
		//IL_0776: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0806: Unknown result type (might be due to invalid IL or missing references)
		//IL_0836: Unknown result type (might be due to invalid IL or missing references)
		//IL_0866: Unknown result type (might be due to invalid IL or missing references)
		//IL_0896: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0926: Unknown result type (might be due to invalid IL or missing references)
		//IL_0956: Unknown result type (might be due to invalid IL or missing references)
		//IL_0986: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a16: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a46: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a76: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b06: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b36: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b66: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b96: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bc6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bf6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c26: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c56: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c86: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cb6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d16: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d46: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d76: Unknown result type (might be due to invalid IL or missing references)
		//IL_0da6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dd6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e06: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e36: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e66: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e96: Unknown result type (might be due to invalid IL or missing references)
		AddHeroAugment("hero01_fire_field_n", "불씨 잔영", "스킬 위치에 반경 2.4m 화염 장판을 3초 생성해 0.6초마다 공격력 35% 피해를 줍니다.", "hero_01", HeroAugmentTier.Normal, new Color(1f, 0.42f, 0.24f));
		AddHeroAugment("hero01_mana_refund_r", "연소 환급", "스킬로 적을 처치하면 최대 마나의 50%를 즉시 회복합니다.", "hero_01", HeroAugmentTier.Rare, new Color(1f, 0.58f, 0.24f));
		AddHeroAugment("hero01_flame_pierce_m", "화염 관통", "스킬 후 전방 5m에 공격력 105%의 추가 관통 피해를 줍니다.", "hero_01", HeroAugmentTier.Mythic, new Color(1f, 0.25f, 0.18f));
		AddHeroAugment("hero02_battle_rx_n", "전투 처방", "스킬 사용 시 체력이 낮은 아군 2명의 공격력 +10%, 공격속도 +8%가 4초 적용됩니다.", "hero_02", HeroAugmentTier.Normal, new Color(0.38f, 1f, 0.62f));
		AddHeroAugment("hero02_crit_heal_r", "치유 치명타", "스킬 사용 시 35% 확률로 체력이 낮은 아군 2명을 14% 회복하고 4초 보호막 8%를 줍니다.", "hero_02", HeroAugmentTier.Rare, new Color(0.48f, 1f, 0.72f));
		AddHeroAugment("hero02_emergency_m", "응급 프로토콜", "체력이 낮은 아군 3명을 18% 회복하고 보호막 12%, 공격력·공격속도 +16%, 마나 +8%를 줍니다.", "hero_02", HeroAugmentTier.Mythic, new Color(0.62f, 1f, 0.86f));
		AddHeroAugment("hero03_freeze_n", "급속 냉각", "스킬이 일반 적에게 적중하면 25% 확률로 1.15초 행동 불가로 만듭니다.", "hero_03", HeroAugmentTier.Normal, new Color(0.45f, 0.86f, 1f));
		AddHeroAugment("hero03_permafrost_r", "영구 서리", "스킬이 일반 적에게 적중하면 20% 확률로 이동속도와 공격속도를 전투 종료까지 45% 낮춥니다.", "hero_03", HeroAugmentTier.Rare, new Color(0.34f, 0.74f, 1f));
		AddHeroAugment("hero03_shatter_m", "빙결 파쇄", "일반 적을 1.2초 행동 불가로 만들고 종료 시 반경 2.8m에 공격력 95% 피해를 줍니다.", "hero_03", HeroAugmentTier.Mythic, new Color(0.62f, 0.92f, 1f));
		AddHeroAugment("hero04_toxic_pool_n", "독성 잔류", "스킬 위치에 반경 2.3m 독 장판을 3.5초 생성해 0.7초마다 공격력 28% 피해를 줍니다.", "hero_04", HeroAugmentTier.Normal, new Color(0.48f, 0.96f, 0.34f));
		AddHeroAugment("hero04_contagion_r", "맹독 전염", "일반 적을 처치하면 가장 가까운 적에게 4.5초 동안 0.75초마다 공격력 42%의 독 피해를 줍니다.", "hero_04", HeroAugmentTier.Rare, new Color(0.4f, 0.86f, 0.28f));
		AddHeroAugment("hero04_plague_cloud_m", "역병 구름", "스킬 위치 반경 3.1m의 적을 5초 중독시키고 이동속도를 3초 동안 28% 낮춥니다.", "hero_04", HeroAugmentTier.Mythic, new Color(0.62f, 1f, 0.34f));
		AddHeroAugment("hero05_shield_bomb_n", "방패 폭심", "아군 보호막이 파괴되면 반경 2.6m에 막은 피해의 80%만큼 피해를 줍니다.", "hero_05", HeroAugmentTier.Normal, new Color(0.35f, 1f, 0.68f));
		AddHeroAugment("hero05_reflect_r", "수호 반사", "막은 피해 45% 반사", "hero_05", HeroAugmentTier.Rare, new Color(0.42f, 1f, 0.54f));
		AddHeroAugment("hero05_bastion_m", "불굴 성역", "아군 보호막이 파괴되면 3.8m 안의 아군에게 최대 체력 14% 보호막을 4.5초 부여합니다.", "hero_05", HeroAugmentTier.Mythic, new Color(0.72f, 1f, 0.62f));
		AddHeroAugment("hero06_spear_throw_n", "추가 투창", "스킬 사용 시 35% 확률로 전방 5.2m에 공격력 85%의 추가 관통 피해를 줍니다.", "hero_06", HeroAugmentTier.Normal, new Color(0.64f, 0.82f, 1f));
		AddHeroAugment("hero06_spear_path_r", "관통 가속", "스킬 후 전방 5.6m를 관통합니다. 2명 이상 적중하면 4초간 공격력 +8%, 공격속도 +18%를 얻습니다.", "hero_06", HeroAugmentTier.Rare, new Color(0.54f, 0.74f, 1f));
		AddHeroAugment("hero06_storm_lance_m", "폭풍창 재시전", "전방 6.4m에 공격력 125%의 넓은 관통 피해를 주고 스킬을 위력 48%로 한 번 더 시전합니다.", "hero_06", HeroAugmentTier.Mythic, new Color(0.72f, 0.9f, 1f));
		AddHeroAugment("hero07_soul_scythe_n", "영혼 수확", "스킬 대상에게 추가 피해와 2.5초 24% 둔화를 주고, 이 유닛이 3킬할 때마다 마나 12%를 회복합니다.", "hero_07", HeroAugmentTier.Normal, new Color(0.7f, 0.36f, 0.88f));
		AddHeroAugment("hero07_reaper_form_r", "10킬 사신화", "이 유닛이 10킬하면 24초 동안 공격력 +35%, 공격속도 +45%의 사신 상태가 됩니다.", "hero_07", HeroAugmentTier.Rare, new Color(0.62f, 0.2f, 0.82f));
		AddHeroAugment("hero07_reaper_execute_m", "사신의 삼격 처형", "10킬 후 사신화하며, 사신 상태에서 3번째 공격마다 일반 적 체력 45% 이하면 처형 피해를 줍니다.", "hero_07", HeroAugmentTier.Mythic, new Color(0.86f, 0.28f, 1f));
		AddHeroAugment("hero08_petrify_spread_n", "추가 석화 주시", "스킬의 주 대상이 일반 적이면 2.3초 석화합니다. 보스에게는 공격력 25% 추가 피해를 줍니다.", "hero_08", HeroAugmentTier.Normal, new Color(0.74f, 0.7f, 1f));
		AddHeroAugment("hero08_gallery_r", "석상 3개 폭발", "석화된 일반 적이 3명 이상이면 스킬 대상 주변 3m에 공격력 110% 추가 피해를 줍니다.", "hero_08", HeroAugmentTier.Rare, new Color(0.62f, 0.58f, 1f));
		AddHeroAugment("hero08_trophy_m", "석화 전리품", "스킬 대상에게 25% 확률로 3초 석화를 걸고, 일반 적 처치 시 30% 확률로 2G를 얻습니다.", "hero_08", HeroAugmentTier.Mythic, new Color(0.88f, 0.82f, 1f));
		AddHeroAugment("hero09_front_cleave_n", "전열 관통", "스킬 후 전방 5.2m에 공격력 72%의 추가 관통 피해를 줍니다.", "hero_09", HeroAugmentTier.Normal, new Color(0.88f, 0.78f, 0.44f));
		AddHeroAugment("hero09_backline_r", "후열 2연격", "스킬 대상 외 가장 먼 적 2명에게 각각 공격력 72% 추가 피해를 줍니다.", "hero_09", HeroAugmentTier.Rare, new Color(0.96f, 0.84f, 0.42f));
		AddHeroAugment("hero09_twin_dragon_m", "쌍룡 관통참", "전방 6.5m에 공격력 110% 관통 피해를 주고 가장 먼 적 3명에게 공격력 85% 피해를 줍니다.", "hero_09", HeroAugmentTier.Mythic, new Color(1f, 0.92f, 0.52f));
		AddHeroAugment("hero10_wind_echo_n", "3기 동조 시전", "같은 유닛이 3기 이상이면 스킬 사용 시 다른 1기가 위력 45%로 추가 시전합니다.", "hero_10", HeroAugmentTier.Normal, new Color(0.44f, 0.92f, 1f));
		AddHeroAugment("hero10_chorus_r", "3기 바람 합창", "같은 유닛이 3기 이상이면 스킬 사용 시 나머지 전원이 위력 55%로 추가 시전합니다.", "hero_10", HeroAugmentTier.Rare, new Color(0.38f, 0.82f, 1f));
		AddHeroAugment("hero10_storm_ritual_m", "3기 폭풍 의식", "같은 유닛이 3기 이상이면 스킬 사용 시 나머지 전원이 위력 80%로 추가 시전합니다.", "hero_10", HeroAugmentTier.Mythic, new Color(0.56f, 0.96f, 1f));
		AddHeroAugment("hero11_blood_bank_n", "체력 흡수 축적", "스킬마다 최대 체력이 3%씩, 최대 18%까지 증가하고 현재 최대 체력의 8%를 회복합니다.", "hero_11", HeroAugmentTier.Normal, new Color(0.92f, 0.26f, 0.34f));
		AddHeroAugment("hero11_blood_surge_r", "흡수 혈류 폭발", "스킬마다 최대 체력이 5%씩, 최대 30%까지 증가하고 주변 2.6m에 공격력 55% 피해를 줍니다.", "hero_11", HeroAugmentTier.Rare, new Color(0.82f, 0.18f, 0.28f));
		AddHeroAugment("hero11_crimson_feast_m", "진홍 흡수 만찬", "스킬마다 최대 체력 +7%(최대 45%), 3.2m 피해 85%, 체력 회복 16%, 보호막 14%를 얻습니다.", "hero_11", HeroAugmentTier.Mythic, new Color(1f, 0.32f, 0.42f));
		AddHeroAugment("hero12_echo_cast_n", "20% 암살 재시전", "스킬 사용 시 20% 확률로 위력 45%의 스킬을 한 번 더 시전합니다.", "hero_12", HeroAugmentTier.Normal, new Color(0.92f, 0.72f, 1f));
		AddHeroAugment("hero12_double_cast_r", "35% 이중 암살", "스킬 사용 시 35% 확률로 위력 62%의 스킬을 재시전하고 마나 12%를 회복합니다.", "hero_12", HeroAugmentTier.Rare, new Color(0.84f, 0.58f, 1f));
		AddHeroAugment("hero12_chain_cast_m", "연쇄 암살 시전", "55%부터 성공할 때마다 확률이 15%p 감소하며 최대 3회, 위력 58%로 재시전하고 매회 마나 8%를 회복합니다.", "hero_12", HeroAugmentTier.Mythic, new Color(0.98f, 0.7f, 1f));
		AddHeroAugment("hero13_overcharge_n", "주변 과충전", "스킬 사용 시 3.2m 안의 아군이 마나 8%를 회복하고 공격속도 +12%를 3초 얻습니다.", "hero_13", HeroAugmentTier.Normal, new Color(0.64f, 0.48f, 1f));
		AddHeroAugment("hero13_mana_gamble_r", "50% 마나 환급", "스킬 사용 시 50% 확률로 최대 마나의 50%를 즉시 회복합니다.", "hero_13", HeroAugmentTier.Rare, new Color(0.74f, 0.42f, 1f));
		AddHeroAugment("hero13_chain_battery_m", "저마나 3기 충전", "스킬 사용 시 5.5m 안에서 마나가 가장 낮은 아군 3명이 최대 마나의 18%를 회복합니다.", "hero_13", HeroAugmentTier.Mythic, new Color(0.88f, 0.58f, 1f));
		AddHeroAugment("hero14_remote_buff_n", "1기 원격 지휘", "스킬 사용 시 무작위 아군 1명의 공격력 +8%, 공격속도 +22%가 5초 적용됩니다.", "hero_14", HeroAugmentTier.Normal, new Color(0.98f, 0.84f, 0.38f));
		AddHeroAugment("hero14_remote_order_r", "2기 광역 명령", "스킬 사용 시 무작위 아군 2명의 공격력 +12%, 공격속도 +30%가 5초 적용됩니다.", "hero_14", HeroAugmentTier.Rare, new Color(1f, 0.76f, 0.34f));
		AddHeroAugment("hero14_whole_army_m", "전군 진격 명령", "모든 아군의 공격력 +16%, 공격속도 +38%가 6초 적용되고 마나가 낮은 아군 3명은 마나 10%를 회복합니다.", "hero_14", HeroAugmentTier.Mythic, new Color(1f, 0.92f, 0.44f));
		AddHeroAugment("hero21_twin_call_n", "미니미 2기 소환", "스킬 사용 시 본체 체력·공격력의 35%를 지닌 미니미 2기를 소환합니다.", "hero_21", HeroAugmentTier.Normal, new Color(0.52f, 0.88f, 0.48f));
		AddHeroAugment("hero21_pack_hunt_r", "미니미 3기 소환", "스킬 사용 시 본체 체력·공격력의 42%를 지닌 미니미 3기를 소환합니다.", "hero_21", HeroAugmentTier.Rare, new Color(0.44f, 0.78f, 0.38f));
		AddHeroAugment("hero21_alpha_pack_m", "강화 미니미 3기", "스킬 사용 시 체력·공격력 55%의 미니미 3기를 소환하고 본체는 5초간 공격력 +12%, 공격속도 +18%를 얻습니다.", "hero_21", HeroAugmentTier.Mythic, new Color(0.7f, 1f, 0.5f));
		AddHeroAugment("hero22_thorn_shell_n", "가시 보호 태세", "스킬 사용 시 4초 동안 최대 체력 18% 보호막과 피해 감소 16%를 얻습니다.", "hero_22", HeroAugmentTier.Normal, new Color(0.48f, 0.96f, 0.62f));
		AddHeroAugment("hero22_reflect_field_r", "4.5초 반사장", "4.5초간 받은 피해를 저장해 종료 시 주변 3m에 35%를 돌려주고, 주변 아군에게 4초 보호막 8%를 줍니다.", "hero_22", HeroAugmentTier.Rare, new Color(0.42f, 0.86f, 0.58f));
		AddHeroAugment("hero22_thorn_crown_m", "가시 왕관 폭발", "5초간 피해 감소 34%, 주변 아군 보호막 16%를 부여하고 반경 3.4m에 공격력 85% 피해를 줍니다.", "hero_22", HeroAugmentTier.Mythic, new Color(0.62f, 1f, 0.72f));
		AddHeroAugment("hero23_power_hit_n", "추가 강타", "스킬의 주 대상에게 공격력 75%의 추가 피해를 줍니다.", "hero_23", HeroAugmentTier.Normal, new Color(1f, 0.7f, 0.44f));
		AddHeroAugment("hero23_weakpoint_r", "체력 50% 약점 가격", "스킬 대상 체력이 50% 이하면 공격력 135%, 아니면 공격력 55%의 추가 피해를 줍니다.", "hero_23", HeroAugmentTier.Rare, new Color(1f, 0.58f, 0.38f));
		AddHeroAugment("hero23_royal_break_m", "왕의 연쇄 강타", "스킬 대상에게 공격력 130%의 치명 피해를 주고 가까운 적 3명에게 공격력 75% 피해를 줍니다.", "hero_23", HeroAugmentTier.Mythic, new Color(1f, 0.82f, 0.52f));
		AddHeroAugment("hero31_battle_rhythm_n", "5초 전투 리듬", "스킬 사용 시 5초 동안 공격력 +14%, 공격속도 +18%를 얻습니다.", "hero_31", HeroAugmentTier.Normal, new Color(0.74f, 0.84f, 0.96f));
		AddHeroAugment("hero31_guard_stance_r", "5초 수비 태세", "스킬 사용 시 5초 동안 피해 감소 30%와 최대 체력 10% 보호막을 얻습니다.", "hero_31", HeroAugmentTier.Rare, new Color(0.64f, 0.76f, 0.92f));
		AddHeroAugment("hero31_iron_growth_m", "강철 전투 성장", "스킬마다 최대 체력 +4%(최대 28%), 6초간 공격력·공격속도 +22%, 피해 감소 35%를 얻습니다.", "hero_31", HeroAugmentTier.Mythic, new Color(0.84f, 0.9f, 1f));
		AddHeroAugment("hero32_predator_rhythm_n", "포식 리듬", "스킬 사용 시 5초 동안 공격력 +10%, 공격속도 +20%를 얻습니다.", "hero_32", HeroAugmentTier.Normal, new Color(0.7f, 0.86f, 0.48f));
		AddHeroAugment("hero32_pack_hunt_r", "무리 사냥탄", "스킬 사용 시 무작위 적에게 공격력 60% 추가 사격 2발을 발사합니다. 같은 적을 다시 맞힐 수 있습니다.", "hero_32", HeroAugmentTier.Rare, new Color(0.58f, 0.8f, 0.4f));
		AddHeroAugment("hero32_alpha_hunt_m", "알파의 포효", "스킬 사용 시 무작위 적에게 공격력 70% 추가 사격 4발을 발사하고 6초간 공격력 +16%, 공격속도 +28%를 얻습니다.", "hero_32", HeroAugmentTier.Mythic, new Color(0.82f, 1f, 0.52f));
		AddHeroAugment("hero33_poison_remnant_n", "4초 잔류 독무", "스킬 위치에 반경 2.7m 독무를 4초 생성해 0.8초마다 공격력 32% 피해를 줍니다.", "hero_33", HeroAugmentTier.Normal, new Color(0.58f, 0.92f, 0.36f));
		AddHeroAugment("hero33_wandering_mist_r", "떠도는 독무 1회", "스킬 4초 후 무작위 적 위치에 반경 2.4m 독무를 3.2초 한 번 더 생성합니다.", "hero_33", HeroAugmentTier.Rare, new Color(0.48f, 0.82f, 0.32f));
		AddHeroAugment("hero33_endless_miasma_m", "독무 2회 확산", "반경 3.2m 독무와 28% 둔화를 만들고 3.5초 후 무작위 적 위치에 독무 2개를 추가 생성합니다.", "hero_33", HeroAugmentTier.Mythic, new Color(0.72f, 1f, 0.42f));
		AddHeroAugment("hero51_chain_lightning_n", "번개 2명 연쇄", "스킬 대상 외 가까운 적 2명에게 각각 공격력 45%의 추가 피해를 줍니다.", "hero_51", HeroAugmentTier.Normal, new Color(0.42f, 0.72f, 1f));
		AddHeroAugment("hero51_residual_lightning_r", "30% 잔류 재시전", "스킬 사용 시 30% 확률로 위력 45%의 스킬을 한 번 더 시전합니다.", "hero_51", HeroAugmentTier.Rare, new Color(0.36f, 0.6f, 1f));
		AddHeroAugment("hero51_overload_circuit_m", "번개 폭주 회로", "45%부터 성공할 때마다 확률이 10%p 감소하며 최대 5회, 위력 42%로 추가 시전합니다.", "hero_51", HeroAugmentTier.Mythic, new Color(0.64f, 0.82f, 1f));
		AddHeroAugment("hero52_burning_fallout_n", "3.5초 운석 낙진", "스킬 위치에 반경 2.5m 장판을 3.5초 생성해 0.7초마다 공격력 32% 피해를 줍니다.", "hero_52", HeroAugmentTier.Normal, new Color(1f, 0.42f, 0.28f));
		AddHeroAugment("hero52_meteor_extend_r", "처치 운석 연장", "스킬로 적을 처치하면 처치 위치에 반경 2.2m 운석 장판을 2.8초 생성합니다.", "hero_52", HeroAugmentTier.Rare, new Color(1f, 0.34f, 0.22f));
		AddHeroAugment("hero52_star_shower_m", "처치 별빛 소나기", "스킬 때 작은 장판 2개를 추가하고, 스킬로 적을 처치하면 처치 위치 주변에 운석 장판 3개를 생성합니다.", "hero_52", HeroAugmentTier.Mythic, new Color(1f, 0.58f, 0.3f));
		AddHeroAugment("hero54_stone_skin_n", "4초 돌피부", "스킬 사용 시 4초 동안 받는 피해가 25% 감소합니다.", "hero_54", HeroAugmentTier.Normal, new Color(0.72f, 0.78f, 0.82f));
		AddHeroAugment("hero54_retribution_r", "4초 응징 저장", "4초간 피해 감소 32%를 얻고 받은 피해를 저장해 종료 시 주변 3m에 50%를 돌려줍니다.", "hero_54", HeroAugmentTier.Rare, new Color(0.82f, 0.84f, 0.88f));
		AddHeroAugment("hero54_fortress_m", "5초 성채화", "5초간 피해 감소 42%, 종료 시 받은 피해 50%를 반사하고 20%만큼 주변 아군에게 보호막을 나눕니다.", "hero_54", HeroAugmentTier.Mythic, new Color(0.92f, 0.92f, 0.86f));
		AddHeroAugment("hero55_impact_guard_n", "충격 흡수판", "스킬 사용 시 5초 동안 최대 체력 12% 보호막을 얻습니다.", "hero_55", HeroAugmentTier.Normal, new Color(0.58f, 0.76f, 0.94f));
		AddHeroAugment("hero55_wall_quake_r", "철벽 충격파", "스킬 착탄 지점 반경 3m에 공격력 90%의 추가 피해를 줍니다.", "hero_55", HeroAugmentTier.Rare, new Color(0.48f, 0.68f, 0.9f));
		AddHeroAugment("hero55_mobile_fortress_m", "이동 성채", "스킬 착탄 지점 반경 3.4m에 공격력 140% 피해를 주고 6초간 보호막 20%, 피해 감소 30%를 얻습니다.", "hero_55", HeroAugmentTier.Mythic, new Color(0.74f, 0.88f, 1f));
		AddHeroAugment("hero56_after_blast_n", "잔류 폭격", "스킬 착탄 지점 반경 3m에 공격력 75%의 추가 폭발 피해를 줍니다.", "hero_56", HeroAugmentTier.Normal, new Color(1f, 0.68f, 0.34f));
		AddHeroAugment("hero56_twin_barrage_r", "쌍발 폭격", "폭격 스킬을 위력 50%로 한 번 더 시전합니다.", "hero_56", HeroAugmentTier.Rare, new Color(1f, 0.54f, 0.26f));
		AddHeroAugment("hero56_orbital_restrike_m", "궤도 재폭격", "폭격 스킬을 위력 80%로 재시전하고 착탄 지점 반경 3.6m에 공격력 120% 추가 피해를 줍니다.", "hero_56", HeroAugmentTier.Mythic, new Color(1f, 0.78f, 0.42f));
		AddHeroAugment("hero57_spare_mag_n", "예비 탄창", "스킬 사용 시 무작위 적에게 공격력 70% 추가 사격 2발을 발사합니다.", "hero_57", HeroAugmentTier.Normal, new Color(0.84f, 0.66f, 1f));
		AddHeroAugment("hero57_ricochet_r", "파손 탄창 난사", "스킬 사용 시 무작위 적에게 공격력 80% 추가 사격 4발을 발사합니다. 같은 적을 다시 맞힐 수 있습니다.", "hero_57", HeroAugmentTier.Rare, new Color(0.72f, 0.52f, 1f));
		AddHeroAugment("hero57_chaos_mag_m", "혼돈 탄창", "스킬 사용 시 무작위 적에게 공격력 90% 추가 사격 6발을 발사합니다. 같은 적을 다시 맞힐 수 있습니다.", "hero_57", HeroAugmentTier.Mythic, new Color(0.92f, 0.76f, 1f));
	}

	private void AddHeroAugment(string id, string title, string description, string heroId, HeroAugmentTier tier, Color color)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		AugmentStyle style = AugmentStyle.Gamble;
		switch (tier)
		{
		case HeroAugmentTier.Normal:
			style = AugmentStyle.Stable;
			break;
		case HeroAugmentTier.Rare:
			style = AugmentStyle.Growth;
			break;
		}
		string text = ResolveHeroAugmentTargetLabel(heroId);
		string title2 = (string.IsNullOrEmpty(text) ? title : ("[" + text + "] " + title));
		AugmentDefinition augmentDefinition = CreateAugment(id, title2, description, style, AugmentEffectType.HeroSignature, 0f, 0f, 0f, color);
		augmentDefinition.requiredHeroId = heroId;
		augmentDefinition.heroTier = tier;
		AddAugmentIfMissing(augmentDefinition);
	}

	private static string ResolveHeroAugmentTargetLabel(string heroId)
	{
		return heroId switch
		{
			"hero_01" => "화염", 
			"hero_02" => "치유", 
			"hero_03" => "빙결", 
			"hero_04" => "독", 
			"hero_05" => "방패", 
			"hero_06" => "돌격", 
			"hero_07" => "사신", 
			"hero_08" => "메두사", 
			"hero_09" => "창", 
			"hero_10" => "바람", 
			"hero_11" => "흡수", 
			"hero_12" => "암살", 
			"hero_13" => "배터리", 
			"hero_14" => "빛", 
			"hero_21" => "소환", 
			"hero_22" => "가시방패", 
			"hero_23" => "늑대", 
			"hero_31" => "전투", 
			"hero_32" => "야성", 
			"hero_33" => "감염", 
			"hero_51" => "번개", 
			"hero_52" => "운석", 
			"hero_54" => "가고일", 
			"hero_55" => "아머", 
			"hero_56" => "오토", 
			"hero_57" => "브로큰", 
			_ => string.Empty, 
		};
	}

	private void RemoveAugmentById(string id)
	{
		for (int num = augmentPool.Count - 1; num >= 0; num--)
		{
			AugmentDefinition augmentDefinition = augmentPool[num];
			if (augmentDefinition != null && string.Equals(augmentDefinition.id, id, StringComparison.Ordinal))
			{
				augmentPool.RemoveAt(num);
			}
		}
	}

	private void AddAugmentIfMissing(AugmentDefinition definition)
	{
		if (definition == null || string.IsNullOrEmpty(definition.id))
		{
			return;
		}
		for (int i = 0; i < augmentPool.Count; i++)
		{
			AugmentDefinition augmentDefinition = augmentPool[i];
			if (augmentDefinition != null && string.Equals(augmentDefinition.id, definition.id, StringComparison.Ordinal))
			{
				return;
			}
		}
		augmentPool.Add(definition);
	}

	private void AddStableAugments()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		augmentPool.Add(CreateAugment("power_core", "화력 코어", "모든 유닛 공격력 +20%. 가장 안정적인 전투력 상승입니다.", AugmentStyle.Stable, AugmentEffectType.AttackPowerMultiplier, 0.2f, 0f, 0f, new Color(1f, 0.57f, 0.28f)));
		augmentPool.Add(CreateAugment("rapid_dice", "속전속결", "모든 유닛 공격속도 +15%. 평타와 마나 수급이 빨라집니다.", AugmentStyle.Stable, AugmentEffectType.AttackSpeedMultiplier, 0.15f, 0f, 0f, new Color(0.45f, 0.9f, 1f)));
		augmentPool.Add(CreateAugment("sharp_eye", "예리한 시선", "치명타 확률 +10%. 크리티컬 빌드의 기본 축입니다.", AugmentStyle.Stable, AugmentEffectType.CriticalChance, 0.1f, 0f, 0f, new Color(1f, 0.86f, 0.32f)));
		augmentPool.Add(CreateAugment("long_shot", "전군 사거리 확장", "모든 유닛의 기본 공격 대상 탐색 사거리가 0.9m 증가합니다. 투사체 속도와 스킬 범위는 변하지 않습니다.", AugmentStyle.Stable, AugmentEffectType.AttackRangeFlat, 0.9f, 0f, 0f, new Color(0.36f, 0.68f, 1f)));
		augmentPool.Add(CreateAugment("splash_round", "연쇄 폭발탄", "평타가 주변 몬스터에게 30% 폭발 피해를 줍니다.", AugmentStyle.Stable, AugmentEffectType.BasicAttackSplash, 1.55f, 0.3f, 0f, new Color(1f, 0.35f, 0.48f)));
		augmentPool.Add(CreateAugment("mana_flow", "마나 순환", "초당 마나 회복률 +2.5%. 스킬 회전이 빨라집니다.", AugmentStyle.Stable, AugmentEffectType.ManaRegenRate, 0.025f, 0f, 0f, new Color(0.66f, 0.42f, 1f)));
		augmentPool.Add(CreateAugment("guardian_heart", "수호자의 심장", "모든 유닛 최대 체력 +24%. 보스 스킬을 버티기 좋습니다.", AugmentStyle.Stable, AugmentEffectType.MaxHealthMultiplier, 0.24f, 0f, 0f, new Color(0.35f, 1f, 0.62f)));
		augmentPool.Add(CreateAugment("skill_overload", "스킬 과부하", "스킬 위력 +22%. 마법/소환 빌드에 잘 맞습니다.", AugmentStyle.Stable, AugmentEffectType.SkillPowerMultiplier, 0.22f, 0f, 0f, new Color(0.96f, 0.5f, 1f)));
		augmentPool.Add(CreateAugment("critical_engine", "치명 증폭기", "치명타 피해 +36%. 치명타가 터질 때 손맛이 커집니다.", AugmentStyle.Stable, AugmentEffectType.CriticalDamageMultiplier, 0.36f, 0f, 0f, new Color(1f, 0.68f, 0.28f)));
		augmentPool.Add(CreateAugment("boss_breaker", "보스 브레이커", "보스에게 주는 피해 +28%. 보스 라운드 안정성이 올라갑니다.", AugmentStyle.Stable, AugmentEffectType.BossDamageMultiplier, 0.28f, 0f, 0f, new Color(1f, 0.36f, 0.3f)));
		augmentPool.Add(CreateAugment("sniper_window", "전군 사거리 보강", "모든 유닛의 기본 공격 대상 탐색 사거리가 0.5m 증가합니다. 투사체 속도와 스킬 범위는 변하지 않습니다.", AugmentStyle.Stable, AugmentEffectType.AttackRangeFlat, 0.5f, 0f, 0f, new Color(0.5f, 0.74f, 1f)));
		augmentPool.Add(CreateAugment("panic_barrier", "긴급 방벽", "모든 유닛 최대 체력 +16%. 보스 스킬을 한 번 더 버틸 여지를 만듭니다.", AugmentStyle.Stable, AugmentEffectType.MaxHealthMultiplier, 0.16f, 0f, 0f, new Color(0.48f, 1f, 0.68f)));
	}

	private void AddGrowthAugments()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		augmentPool.Add(CreateAugment("mining_worker", "골드 광부", "라운드 시작마다 2G + 라운드당 0.30G를 채굴합니다.", AugmentStyle.Growth, AugmentEffectType.WorkerGoldPerRound, 2f, 0.3f, 0f, new Color(1f, 0.78f, 0.22f)));
		augmentPool.Add(CreateAugment("senior_miner", "숙련 광부", "라운드 시작마다 4G + 라운드당 0.18G를 채굴합니다.", AugmentStyle.Growth, AugmentEffectType.WorkerGoldPerRound, 4f, 0.18f, 0f, new Color(1f, 0.65f, 0.24f)));
		augmentPool.Add(CreateAugment("gold_engine", "골드 엔진", "라운드 클리어 보상이 5G 증가합니다.", AugmentStyle.Growth, AugmentEffectType.RoundGoldBonus, 5f, 0f, 0f, new Color(0.98f, 0.82f, 0.28f)));
		augmentPool.Add(CreateAugment("cheap_summon", "소환 할인권", "이번 판 동안 소환 비용이 9% 감소합니다.", AugmentStyle.Growth, AugmentEffectType.SummonCostDiscount, 0.09f, 0f, 0f, new Color(0.48f, 0.86f, 1f)));
		augmentPool.Add(CreateAugment("opening_burst", "개전 신호탄", "라운드 시작 후 5초 동안 공격력 +30%, 공격속도 +18%.", AugmentStyle.Growth, AugmentEffectType.RoundStartBurst, 0.3f, 0.18f, 5f, new Color(0.3f, 1f, 0.86f)));
		augmentPool.Add(CreateAugment("union_mine", "공동 광산", "라운드 시작마다 3G + 라운드당 0.42G를 채굴합니다.", AugmentStyle.Growth, AugmentEffectType.WorkerGoldPerRound, 3f, 0.42f, 0f, new Color(1f, 0.72f, 0.3f)));
		augmentPool.Add(CreateAugment("steady_contract", "안정 계약", "라운드 클리어 보상 +4G. 초반 운영이 안정됩니다.", AugmentStyle.Growth, AugmentEffectType.RoundGoldBonus, 4f, 0f, 0f, new Color(0.7f, 1f, 0.66f)));
		augmentPool.Add(CreateAugment("coupon_storm", "쿠폰 폭풍", "이번 판 동안 소환 비용이 16% 감소합니다. 소환을 많이 누르는 판을 만듭니다.", AugmentStyle.Growth, AugmentEffectType.SummonCostDiscount, 0.16f, 0f, 0f, new Color(0.28f, 0.92f, 1f)));
		augmentPool.Add(CreateAugment("first_wave_bonus", "초반 보급로", "라운드 클리어 보상 +3G. 초반 운영이 조금 더 부드러워집니다.", AugmentStyle.Growth, AugmentEffectType.RoundGoldBonus, 3f, 0f, 0f, new Color(0.88f, 1f, 0.52f)));
		augmentPool.Add(CreateAugment("hoard_interest", "저축 이자", "전투 중 100G 이상 보유 시 3초마다 +2G.", AugmentStyle.Growth, AugmentEffectType.HoardInterestGold, 100f, 2f, 3f, new Color(1f, 0.9f, 0.3f)));
	}

	private void AddGambleAugments()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		augmentPool.Add(CreateAugment("jackpot", "잭팟 상자", "즉시 14~52G 중 하나를 획득합니다.", AugmentStyle.Gamble, AugmentEffectType.RandomInstantGold, 14f, 52f, 0f, new Color(0.38f, 1f, 0.48f)));
		augmentPool.Add(CreateAugment("pickpocket", "동전 약탈자", "몬스터 처치 시 16% 확률로 1~5G를 추가 획득합니다.", AugmentStyle.Gamble, AugmentEffectType.KillGoldChance, 0.16f, 5f, 0f, new Color(1f, 0.82f, 0.3f)));
		augmentPool.Add(CreateAugment("round_roulette", "라운드 룰렛", "라운드 클리어마다 2~16G를 무작위로 획득합니다.", AugmentStyle.Gamble, AugmentEffectType.RoundClearRandomGold, 2f, 16f, 0f, new Color(0.92f, 0.48f, 1f)));
		augmentPool.Add(CreateAugment("boss_bet", "보스 올인", "보스 라운드 클리어 시 24~52G를 획득합니다.", AugmentStyle.Gamble, AugmentEffectType.BossRoundBet, 24f, 28f, 0f, new Color(1f, 0.3f, 0.34f)));
		augmentPool.Add(CreateAugment("interest_bank", "이자 금고", "라운드 클리어마다 현재 골드의 6%를 받습니다. 최대 12G.", AugmentStyle.Gamble, AugmentEffectType.InterestGold, 0.06f, 12f, 0f, new Color(0.54f, 0.95f, 1f)));
		augmentPool.Add(CreateAugment("risky_vault", "위험 금고", "즉시 0~78G를 얻습니다. 운이 나쁘면 아무것도 얻지 못합니다.", AugmentStyle.Gamble, AugmentEffectType.RandomInstantGold, 0f, 78f, 0f, new Color(1f, 0.4f, 0.72f)));
		augmentPool.Add(CreateAugment("bounty_flip", "현상금 뒤집기", "몬스터 처치 시 12% 확률로 1~9G를 추가 획득합니다.", AugmentStyle.Gamble, AugmentEffectType.KillGoldChance, 0.12f, 9f, 0f, new Color(1f, 0.66f, 0.24f)));
		augmentPool.Add(CreateAugment("dopamine_button", "도파민 버튼", "즉시 1~96G를 얻습니다. 낮게 뜨면 아프지만, 높게 뜨면 판이 열립니다.", AugmentStyle.Gamble, AugmentEffectType.RandomInstantGold, 1f, 96f, 0f, new Color(1f, 0.3f, 0.86f)));
		augmentPool.Add(CreateAugment("mini_lotto", "미니 로또", "라운드 클리어마다 0~28G를 무작위로 획득합니다.", AugmentStyle.Gamble, AugmentEffectType.RoundClearRandomGold, 0f, 28f, 0f, new Color(0.76f, 0.38f, 1f)));
		augmentPool.Add(CreateAugment("rare_bounty", "희귀 현상금", "몬스터 처치 시 6% 확률로 1~18G 대박 보상을 노립니다.", AugmentStyle.Gamble, AugmentEffectType.KillGoldChance, 0.06f, 18f, 0f, new Color(1f, 0.74f, 0.18f)));
		augmentPool.Add(CreateAugment("boss_scratch", "보스 복권", "보스 라운드 클리어 시 8~88G를 획득합니다. 보스마다 복권을 긁는 느낌입니다.", AugmentStyle.Gamble, AugmentEffectType.BossRoundBet, 8f, 80f, 0f, new Color(1f, 0.25f, 0.38f)));
	}

	private void AddBuildupAugments()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		augmentPool.Add(CreateAugment("late_bloom_attack", "후반 화력 꽃", "라운드가 시작될 때마다 공격력이 2.7%씩 누적 증가합니다.", AugmentStyle.Buildup, AugmentEffectType.ScalingAttackPowerPerRound, 0.027f, 0f, 0f, new Color(1f, 0.52f, 0.35f)));
		augmentPool.Add(CreateAugment("timegear_speed", "시간 톱니", "라운드가 시작될 때마다 공격속도가 1.8%씩 누적 증가합니다.", AugmentStyle.Buildup, AugmentEffectType.ScalingAttackSpeedPerRound, 0.018f, 0f, 0f, new Color(0.46f, 0.82f, 1f)));
		augmentPool.Add(CreateAugment("arcane_archive", "비전 기록서", "라운드가 시작될 때마다 스킬 위력이 3.0%씩 누적 증가합니다.", AugmentStyle.Buildup, AugmentEffectType.ScalingSkillPowerPerRound, 0.03f, 0f, 0f, new Color(0.76f, 0.52f, 1f)));
		augmentPool.Add(CreateAugment("mana_snowball", "마나 눈덩이", "라운드가 시작될 때마다 초당 마나 회복이 0.6%씩 누적 증가합니다.", AugmentStyle.Buildup, AugmentEffectType.ScalingManaRegenPerRound, 0.006f, 0f, 0f, new Color(0.32f, 0.88f, 1f)));
		augmentPool.Add(CreateAugment("compound_mine", "복리 광산", "라운드가 시작될 때마다 누적 단계만큼 추가 골드를 채굴합니다.", AugmentStyle.Buildup, AugmentEffectType.ScalingGoldPerRound, 1.8f, 0f, 0f, new Color(1f, 0.86f, 0.28f)));
		augmentPool.Add(CreateAugment("dragon_scale", "보스 사냥 서약", "라운드가 시작될 때마다 보스 피해가 3.0%씩 누적 증가합니다.", AugmentStyle.Buildup, AugmentEffectType.ScalingBossDamagePerRound, 0.03f, 0f, 0f, new Color(1f, 0.42f, 0.3f)));
		augmentPool.Add(CreateAugment("overclock_growth", "과열 성장", "라운드가 시작될 때마다 공격속도가 1.3%씩 누적 증가합니다.", AugmentStyle.Buildup, AugmentEffectType.ScalingAttackSpeedPerRound, 0.013f, 0f, 0f, new Color(0.36f, 1f, 0.72f)));
		augmentPool.Add(CreateAugment("fever_engine", "피버 엔진", "라운드가 시작될 때마다 스킬 위력이 2.2%씩 누적 증가합니다. 장기전 스킬 빌드용입니다.", AugmentStyle.Buildup, AugmentEffectType.ScalingSkillPowerPerRound, 0.022f, 0f, 0f, new Color(1f, 0.46f, 1f)));
		augmentPool.Add(CreateAugment("boss_tax_account", "보스 세금통장", "라운드가 시작될 때마다 보스 피해가 2.0%씩 누적됩니다. 보스 허들 대응용입니다.", AugmentStyle.Buildup, AugmentEffectType.ScalingBossDamagePerRound, 0.02f, 0f, 0f, new Color(1f, 0.55f, 0.24f)));
	}

	private AugmentDefinition CreateAugment(string id, string title, string description, AugmentStyle style, AugmentEffectType effectType, float value, float secondaryValue, float duration, Color accentColor)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		return new AugmentDefinition
		{
			id = id,
			title = title,
			description = description,
			style = style,
			effectType = effectType,
			value = value,
			secondaryValue = secondaryValue,
			duration = duration,
			accentColor = accentColor
		};
	}
}
