using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame
{
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

		public bool IsChoiceOpen => panelRoot != null && panelRoot.activeSelf;

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
			AugmentDefinition selected = augmentPool.Find((AugmentDefinition augment) => augment != null && string.Equals(augment.id, id, StringComparison.Ordinal));
			if (selected == null)
			{
				return false;
			}
			chosenAugments.Add(selected);
			ApplyEconomyAugment(selected);
			DefenderUnit[] defenders = UnityEngine.Object.FindObjectsOfType<DefenderUnit>();
			for (int i = 0; i < defenders.Length; i++)
			{
				ApplyPermanentAugmentToDefender(defenders[i], selected);
			}
			this.OnChoiceSelected?.Invoke(selected);
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
			if ((UnityEngine.Object)(object)closeButton != null)
			{
				((Component)(object)closeButton).gameObject.SetActive(value: false);
			}
			EnsureDefaultPool();
			EnsureHeroSpecificAugments();
			BindButtons();
			if (panelRoot != null)
			{
				panelRoot.SetActive(value: false);
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
				if (gameController != null)
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
			if (gameController == null || chosenAugments.Count == 0)
			{
				hoardIncomeTimer = 0f;
				return;
			}
			bool hasHoardIncome = false;
			int totalReward = 0;
			float tickInterval = 0f;
			Color rewardColor = new Color(1f, 0.9f, 0.3f);
			for (int i = 0; i < chosenAugments.Count; i++)
			{
				AugmentDefinition augment = chosenAugments[i];
				if (augment != null && augment.effectType == AugmentEffectType.HoardInterestGold)
				{
					hasHoardIncome = true;
					int requiredGold = Mathf.Max(1, Mathf.RoundToInt(augment.value));
					if (gameController.Gold >= requiredGold)
					{
						totalReward += Mathf.Max(1, Mathf.RoundToInt(augment.secondaryValue));
						float interval = Mathf.Max(0.5f, (augment.duration > 0f) ? augment.duration : 3f);
						tickInterval = ((tickInterval <= 0f) ? interval : Mathf.Min(tickInterval, interval));
						rewardColor = augment.accentColor;
					}
				}
			}
			if (!hasHoardIncome || !gameController.IsCombatInteractionLocked || totalReward <= 0 || tickInterval <= 0f)
			{
				hoardIncomeTimer = 0f;
				return;
			}
			hoardIncomeTimer += Time.deltaTime;
			if (!(hoardIncomeTimer < tickInterval))
			{
				int ticks = Mathf.Max(1, Mathf.FloorToInt(hoardIncomeTimer / tickInterval));
				hoardIncomeTimer -= tickInterval * (float)ticks;
				int reward = totalReward * ticks;
				gameController.AddGold(reward);
				ShowEconomyBanner("저축 이자 +" + reward + "G", rewardColor, 1.4f, 0.8f);
			}
		}

		private void Unsubscribe()
		{
			if (subscribed)
			{
				if (gameController != null)
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
			if ((UnityEngine.Object)(object)closeButton != null)
			{
				((UnityEvent)(object)closeButton.onClick).RemoveListener((UnityAction)CloseChoice);
				((UnityEvent)(object)closeButton.onClick).AddListener((UnityAction)CloseChoice);
			}
			if ((UnityEngine.Object)(object)reopenButton != null)
			{
				((UnityEvent)(object)reopenButton.onClick).RemoveListener((UnityAction)OpenPendingChoice);
				((UnityEvent)(object)reopenButton.onClick).AddListener((UnityAction)OpenPendingChoice);
			}
			if (choiceButtons == null)
			{
				return;
			}
			for (int i = 0; i < choiceButtons.Length; i++)
			{
				int choiceIndex = i;
				if (!((UnityEngine.Object)(object)choiceButtons[i] == null))
				{
					((UnityEventBase)(object)choiceButtons[i].onClick).RemoveAllListeners();
					((UnityEvent)(object)choiceButtons[i].onClick).AddListener((UnityAction)delegate
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
			DefenderUnit[] defenders = UnityEngine.Object.FindObjectsOfType<DefenderUnit>();
			hero07KillCounts.Clear();
			hero07StrikeCounts.Clear();
			hero07ReaperActive.Clear();
			skillChainCastCount = 0;
			for (int i = 0; i < chosenAugments.Count; i++)
			{
				AugmentDefinition augment = chosenAugments[i];
				if (augment == null)
				{
					continue;
				}
				if (augment.effectType == AugmentEffectType.RoundStartBurst)
				{
					for (int defenderIndex = 0; defenderIndex < defenders.Length; defenderIndex++)
					{
						ApplyRoundStartAugment(defenders[defenderIndex], augment);
					}
				}
				if (augment.effectType == AugmentEffectType.Hero05GuardianProtocol)
				{
					ApplyHero05GuardianProtocol(defenders, augment);
				}
				if (IsBuildupEffect(augment.effectType))
				{
					IncrementBuildup(augment);
					for (int j = 0; j < defenders.Length; j++)
					{
						ApplyBuildupIncrement(defenders[j], augment);
					}
				}
				ResolveRoundStartedEconomy(augment, round);
			}
		}

		private void HandleDamageDealt(DefenderUnit source, MonsterUnit target, float damage, bool critical)
		{
			if (source == null || target == null || damage <= 0f || resolvingHeroAugmentDamage)
			{
				return;
			}
			for (int i = 0; i < chosenAugments.Count; i++)
			{
				AugmentDefinition augment = chosenAugments[i];
				if (augment != null)
				{
					switch (augment.effectType)
					{
					case AugmentEffectType.Hero08PetrifyBloom:
						TryResolveHero08PetrifyBloom(source, target, augment);
						break;
					case AugmentEffectType.Hero01VolcanicAftershock:
						TryResolveHero01VolcanicAftershock(source, target, damage, augment);
						break;
					case AugmentEffectType.Hero03FrostResidue:
						TryResolveHero03FrostResidue(source, target, augment);
						break;
					case AugmentEffectType.Hero13ManaNetwork:
						TryResolveHero13ManaNetwork(source, augment);
						break;
					}
				}
			}
			ResolveHero07ReaperStrike(source, target);
			ResolveGeneralDamageAugments(source, target, damage, critical);
		}

		private void ResolveGeneralDamageAugments(DefenderUnit source, MonsterUnit target, float damage, bool critical)
		{
			if (source == null || target == null || target.CurrentHealth <= 0f || damage <= 0f)
			{
				return;
			}
			float extraDamage = 0f;
			Color feedbackColor = new Color(1f, 0.66f, 0.24f);
			for (int i = 0; i < chosenAugments.Count; i++)
			{
				AugmentDefinition augment = chosenAugments[i];
				if (augment == null)
				{
					continue;
				}
				if (augment.effectType == AugmentEffectType.ExecuteDamage && target.MaxHealth > 0f)
				{
					float healthThreshold = Mathf.Clamp01(augment.value);
					if (target.CurrentHealth / target.MaxHealth <= healthThreshold)
					{
						float ratio = (target.IsBoss ? Mathf.Max(0f, augment.duration) : Mathf.Max(0f, augment.secondaryValue));
						extraDamage += damage * ratio;
						feedbackColor = augment.accentColor;
					}
				}
				else if (augment.effectType == AugmentEffectType.LowHealthFury && source.HealthRatio <= Mathf.Clamp01(augment.value))
				{
					extraDamage += damage * Mathf.Max(0f, augment.secondaryValue);
					feedbackColor = augment.accentColor;
				}
				else if (augment.effectType == AugmentEffectType.CriticalDoubleTap && critical && UnityEngine.Random.value <= Mathf.Clamp01(augment.value))
				{
					extraDamage += damage * Mathf.Max(0f, augment.secondaryValue);
					feedbackColor = augment.accentColor;
				}
			}
			if (!(extraDamage <= 0f))
			{
				resolvingHeroAugmentDamage = true;
				try
				{
					target.TakeDamage(extraDamage, critical: false, source);
				}
				finally
				{
					resolvingHeroAugmentDamage = false;
				}
				RuntimeCombatFeedback.ShowGroundPulse(target.transform.position, feedbackColor, target.IsBoss ? 0.66f : 0.46f, 0.34f, 0.1f);
			}
		}

		private void HandleDefenderSkillCast(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
		{
			if (!(source == null) && source.Definition != null && skill != null && !resolvingHeroSkillEcho)
			{
				string heroId = source.Definition.id;
				if (string.Equals(heroId, "hero_01", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero01SkillAugments(source, skill, target);
				}
				else if (string.Equals(heroId, "hero_02", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero02SkillAugments(source, skill);
				}
				else if (string.Equals(heroId, "hero_03", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero03SkillAugments(source, skill, target);
				}
				else if (string.Equals(heroId, "hero_04", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero04SkillAugments(source, skill, target);
				}
				else if (string.Equals(heroId, "hero_06", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero06SkillAugments(source, skill, target);
				}
				else if (string.Equals(heroId, "hero_07", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero07SkillAugments(source, skill, target);
				}
				else if (string.Equals(heroId, "hero_08", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero08SkillAugments(source, skill, target);
				}
				else if (string.Equals(heroId, "hero_09", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero09SkillAugments(source, skill, target);
				}
				else if (string.Equals(heroId, "hero_10", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero10SkillAugments(source, skill);
				}
				else if (string.Equals(heroId, "hero_11", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero11SkillAugments(source, skill, target);
				}
				else if (string.Equals(heroId, "hero_12", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero12SkillAugments(source, skill);
				}
				else if (string.Equals(heroId, "hero_13", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero13SkillAugments(source);
				}
				else if (string.Equals(heroId, "hero_14", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero14SkillAugments(source);
				}
				else if (string.Equals(heroId, "hero_21", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero21SkillAugments(source, skill);
				}
				else if (string.Equals(heroId, "hero_22", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero22SkillAugments(source, skill);
				}
				else if (string.Equals(heroId, "hero_23", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero23SkillAugments(source, skill, target);
				}
				else if (string.Equals(heroId, "hero_31", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero31SkillAugments(source);
				}
				else if (string.Equals(heroId, "hero_32", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero32SkillAugments(source, target);
				}
				else if (string.Equals(heroId, "hero_33", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero33SkillAugments(source, skill, target);
				}
				else if (string.Equals(heroId, "hero_51", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero51SkillAugments(source, skill, target);
				}
				else if (string.Equals(heroId, "hero_52", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero52SkillAugments(source, skill, target);
				}
				else if (string.Equals(heroId, "hero_54", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero54SkillAugments(source, skill);
				}
				else if (string.Equals(heroId, "hero_55", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero55SkillAugments(source, target);
				}
				else if (string.Equals(heroId, "hero_56", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero56SkillAugments(source, skill, target);
				}
				else if (string.Equals(heroId, "hero_57", StringComparison.OrdinalIgnoreCase))
				{
					ResolveHero57SkillAugments(source);
				}
				ResolveGeneralSkillAugments(source, target);
			}
		}

		private void ResolveGeneralSkillAugments(DefenderUnit source, MonsterUnit target)
		{
			for (int i = 0; i < chosenAugments.Count; i++)
			{
				AugmentDefinition augment = chosenAugments[i];
				if (augment == null)
				{
					continue;
				}
				if (augment.effectType == AugmentEffectType.SkillManaRelay)
				{
					RestoreLowestManaDefenders(source.transform.position, 99f, Mathf.Clamp01(augment.value), 1, source);
					RuntimeCombatFeedback.ShowGroundPulse(source.transform.position, augment.accentColor, 0.42f, 0.32f);
				}
				else if (augment.effectType == AugmentEffectType.SkillChainBlast)
				{
					skillChainCastCount++;
					int requiredCasts = Mathf.Max(2, Mathf.RoundToInt(augment.value));
					if (skillChainCastCount >= requiredCasts)
					{
						skillChainCastCount = 0;
						Vector3 center = ((target != null) ? target.transform.position : source.transform.position);
						float radius = Mathf.Max(0.5f, augment.duration);
						float blastDamage = source.EffectiveAttackPower * Mathf.Max(0f, augment.secondaryValue);
						ApplyHeroAreaDamage(source, null, center, radius, blastDamage);
						RuntimeCombatFeedback.ShowGroundPulse(center, augment.accentColor, radius, 0.48f, 0.1f);
					}
				}
			}
		}

		private void HandleShieldResolved(DefenderUnit shielded, float blockedDamage, bool shieldBroken, MonsterUnit source)
		{
			if (!(shielded == null) && !(blockedDamage <= 0f) && CountFieldHero("hero_05") > 0)
			{
				if (shieldBroken && HasChosen("hero05_shield_bomb_n"))
				{
					ApplyHeroAreaDamage(shielded, null, shielded.transform.position, 2.6f, Mathf.Max(5f, blockedDamage * 0.8f));
				}
				if (source != null && HasChosen("hero05_reflect_r"))
				{
					source.TakeDamage(Mathf.Max(1f, blockedDamage * 0.45f), critical: false, shielded);
				}
				if (shieldBroken && HasChosen("hero05_bastion_m"))
				{
					ShieldNearbyDefenders(shielded.transform.position, 3.8f, 0.14f, 4.5f);
				}
			}
		}

		private void HandleDefenderDamageTaken(DefenderUnit defender, MonsterUnit source, float damage)
		{
			if (!(defender == null) && !(damage <= 0f) && hero54StoredDamage.ContainsKey(defender))
			{
				hero54StoredDamage[defender] += damage;
			}
		}

		private void HandleMonsterKilled(MonsterUnit monster)
		{
			if (gameController == null || monster == null)
			{
				return;
			}
			for (int i = 0; i < chosenAugments.Count; i++)
			{
				AugmentDefinition augment = chosenAugments[i];
				if (augment != null && augment.effectType == AugmentEffectType.KillGoldChance && !(UnityEngine.Random.value > Mathf.Clamp01(augment.value)))
				{
					int maxGold = Mathf.Max(1, Mathf.RoundToInt(augment.secondaryValue));
					int reward = UnityEngine.Random.Range(1, maxGold + 1);
					gameController.AddGold(reward);
					ShowRandomGoldFeedback(augment, reward, 1, maxGold, augment.value);
				}
			}
			ResolveHeroMonsterKilledAugments(monster);
		}

		private void ResolveHeroMonsterKilledAugments(MonsterUnit monster)
		{
			if (monster == null)
			{
				return;
			}
			DefenderUnit killer = monster.LastDamageSource;
			if (killer == null || killer.Definition == null)
			{
				return;
			}
			string killerId = killer.Definition.id;
			if (string.Equals(killerId, "hero_01", StringComparison.OrdinalIgnoreCase) && HasChosen("hero01_mana_refund_r") && monster.LastDamageSkill != null)
			{
				killer.RestoreMana(0.5f);
			}
			if (string.Equals(killerId, "hero_04", StringComparison.OrdinalIgnoreCase) && HasChosen("hero04_contagion_r") && !monster.IsBoss)
			{
				PoisonNearestMonster(killer, monster.transform.position, killer.Definition.stats.attackPower * 0.42f, 4.5f, 0.75f);
			}
			if (string.Equals(killerId, "hero_07", StringComparison.OrdinalIgnoreCase))
			{
				ResolveHero07Kill(killer);
			}
			if (string.Equals(killerId, "hero_52", StringComparison.OrdinalIgnoreCase) && (HasChosen("hero52_meteor_extend_r") || HasChosen("hero52_star_shower_m")) && monster.LastDamageSkill != null)
			{
				int zoneCount = ((!HasChosen("hero52_star_shower_m")) ? 1 : 3);
				for (int i = 0; i < zoneCount; i++)
				{
					Vector3 offset = UnityEngine.Random.insideUnitSphere * 1.8f;
					offset.y = 0f;
					SpawnHeroDamageZone(killer, monster.LastDamageSkill, monster.transform.position + offset, 2.2f, killer.Definition.stats.attackPower * 0.35f, 2.8f, 0.7f);
				}
			}
			if (gameController != null && string.Equals(killerId, "hero_08", StringComparison.OrdinalIgnoreCase) && HasChosen("hero08_trophy_m") && !monster.IsBoss && UnityEngine.Random.value <= 0.3f)
			{
				gameController.AddGold(2);
				ShowEconomyBanner("전리품 석상 +2G", new Color(0.74f, 0.7f, 1f), 1.2f, 0.8f);
			}
		}

		private void HandleDefenderSpawned(DefenderUnit defender)
		{
			if (defender == null)
			{
				return;
			}
			for (int i = 0; i < chosenAugments.Count; i++)
			{
				ApplyPermanentAugmentToDefender(defender, chosenAugments[i]);
			}
			if (!(gameController != null) || !gameController.IsRoundRunning)
			{
				return;
			}
			for (int j = 0; j < chosenAugments.Count; j++)
			{
				AugmentDefinition augment = chosenAugments[j];
				if (augment != null && augment.effectType == AugmentEffectType.RoundStartBurst)
				{
					ApplyRoundStartAugment(defender, augment);
				}
			}
		}

		private void ShowChoices(int round)
		{
			if (panelRoot == null || augmentPool.Count == 0 || choiceButtons == null)
			{
				return;
			}
			currentChoices.Clear();
			heroAugmentOfferRolls.Clear();
			pendingChoiceRound = round;
			AugmentStyle[] slots = BuildChoiceStyleSlots(round);
			for (int i = 0; i < slots.Length; i++)
			{
				AugmentDefinition choice = PickChoice(slots[i]);
				if (choice != null && !currentChoices.Contains(choice))
				{
					currentChoices.Add(choice);
				}
			}
			FillMissingChoices();
			RememberCurrentChoices();
			if ((UnityEngine.Object)(object)headerText != null)
			{
				headerText.text = "증강체 선택  ROUND " + round;
			}
			for (int j = 0; j < choiceButtons.Length; j++)
			{
				bool hasChoice = j < currentChoices.Count;
				if ((UnityEngine.Object)(object)choiceButtons[j] != null)
				{
					((Component)(object)choiceButtons[j]).gameObject.SetActive(hasChoice);
				}
				if (hasChoice)
				{
					AugmentDefinition choice2 = currentChoices[j];
					if (accentImages != null && j < accentImages.Length && (UnityEngine.Object)(object)accentImages[j] != null)
					{
						((Graphic)accentImages[j]).color = choice2.accentColor;
					}
					if (styleTexts != null && j < styleTexts.Length && (UnityEngine.Object)(object)styleTexts[j] != null)
					{
						styleTexts[j].text = GetChoiceLabel(choice2);
						((Graphic)styleTexts[j]).color = Color.white;
					}
					if (titleTexts != null && j < titleTexts.Length && (UnityEngine.Object)(object)titleTexts[j] != null)
					{
						titleTexts[j].text = choice2.title;
						((Graphic)titleTexts[j]).color = choice2.accentColor;
					}
					if (descriptionTexts != null && j < descriptionTexts.Length && (UnityEngine.Object)(object)descriptionTexts[j] != null)
					{
						descriptionTexts[j].text = choice2.description;
					}
				}
			}
			panelRoot.SetActive(value: true);
			UpdateReopenButton();
			this.OnChoiceShown?.Invoke(round);
		}

		private void RefreshChoiceUi(int round)
		{
			if (choiceButtons == null)
			{
				return;
			}
			if ((UnityEngine.Object)(object)headerText != null)
			{
				headerText.text = "증강체 선택  ROUND " + round;
			}
			for (int i = 0; i < choiceButtons.Length; i++)
			{
				bool hasChoice = i < currentChoices.Count;
				if ((UnityEngine.Object)(object)choiceButtons[i] != null)
				{
					((Component)(object)choiceButtons[i]).gameObject.SetActive(hasChoice);
				}
				if (hasChoice)
				{
					AugmentDefinition choice = currentChoices[i];
					if (accentImages != null && i < accentImages.Length && (UnityEngine.Object)(object)accentImages[i] != null)
					{
						((Graphic)accentImages[i]).color = choice.accentColor;
					}
					if (styleTexts != null && i < styleTexts.Length && (UnityEngine.Object)(object)styleTexts[i] != null)
					{
						styleTexts[i].text = GetChoiceLabel(choice);
						((Graphic)styleTexts[i]).color = Color.white;
					}
					if (titleTexts != null && i < titleTexts.Length && (UnityEngine.Object)(object)titleTexts[i] != null)
					{
						titleTexts[i].text = choice.title;
						((Graphic)titleTexts[i]).color = choice.accentColor;
					}
					if (descriptionTexts != null && i < descriptionTexts.Length && (UnityEngine.Object)(object)descriptionTexts[i] != null)
					{
						descriptionTexts[i].text = choice.description;
					}
				}
			}
		}

		private AugmentStyle[] BuildChoiceStyleSlots(int round)
		{
			int safeInterval = Mathf.Max(1, minChoiceInterval);
			int choicePhase = Mathf.Max(0, round - Mathf.Max(1, firstChoiceRound)) / safeInterval;
			return (choicePhase % 4) switch
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
			List<AugmentDefinition> candidates = new List<AugmentDefinition>();
			for (int i = 0; i < augmentPool.Count; i++)
			{
				AugmentDefinition augment = augmentPool[i];
				if (augment != null && augment.style == style && !currentChoices.Contains(augment) && !HasChosen(augment.id) && !WasRecentlyOffered(augment) && CanOfferAugment(augment))
				{
					candidates.Add(augment);
				}
			}
			if (candidates.Count == 0)
			{
				for (int j = 0; j < augmentPool.Count; j++)
				{
					AugmentDefinition augment2 = augmentPool[j];
					if (augment2 != null && augment2.style == style && !currentChoices.Contains(augment2) && CanOfferAugment(augment2))
					{
						candidates.Add(augment2);
					}
				}
			}
			return (candidates.Count > 0) ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : null;
		}

		private void FillMissingChoices()
		{
			int choiceCount = ((choiceButtons != null) ? Mathf.Min(3, choiceButtons.Length) : 3);
			List<AugmentDefinition> candidates = new List<AugmentDefinition>();
			for (int i = 0; i < augmentPool.Count; i++)
			{
				AugmentDefinition augment = augmentPool[i];
				if (augment != null && !currentChoices.Contains(augment) && !HasChosen(augment.id) && !WasRecentlyOffered(augment) && CanOfferAugment(augment))
				{
					candidates.Add(augment);
				}
			}
			if (candidates.Count == 0)
			{
				for (int j = 0; j < augmentPool.Count; j++)
				{
					AugmentDefinition augment2 = augmentPool[j];
					if (augment2 != null && !currentChoices.Contains(augment2) && CanOfferAugment(augment2))
					{
						candidates.Add(augment2);
					}
				}
			}
			while (currentChoices.Count < choiceCount && candidates.Count > 0)
			{
				int selectedIndex = UnityEngine.Random.Range(0, candidates.Count);
				currentChoices.Add(candidates[selectedIndex]);
				candidates.RemoveAt(selectedIndex);
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
				AugmentDefinition choice = currentChoices[i];
				if (choice != null && !string.IsNullOrWhiteSpace(choice.id))
				{
					recentAugmentOfferIds.Remove(choice.id);
					recentAugmentOfferIds.Add(choice.id);
				}
			}
			int historyLimit = Mathf.Max(3, recentAugmentHistorySize);
			while (recentAugmentOfferIds.Count > historyLimit)
			{
				recentAugmentOfferIds.RemoveAt(0);
			}
		}

		private void ChooseAugment(int index)
		{
			if (index >= 0 && index < currentChoices.Count)
			{
				AugmentDefinition selected = currentChoices[index];
				chosenAugments.Add(selected);
				ApplyEconomyAugment(selected);
				DefenderUnit[] defenders = UnityEngine.Object.FindObjectsOfType<DefenderUnit>();
				for (int i = 0; i < defenders.Length; i++)
				{
					ApplyPermanentAugmentToDefender(defenders[i], selected);
				}
				this.OnChoiceSelected?.Invoke(selected);
				int completedRound = ((pendingChoiceRound > 0) ? pendingChoiceRound : ((gameController != null) ? gameController.CurrentRound : nextChoiceRound));
				currentChoices.Clear();
				pendingChoiceRound = -1;
				ScheduleNextChoice(completedRound);
				gameController?.RequestBanner(GetChoiceLabel(selected) + " 증강 획득: " + selected.title, selected.accentColor, 2.2f);
				HidePanel();
			}
		}

		private void HidePanel()
		{
			if (panelRoot != null)
			{
				bool wasActive = panelRoot.activeSelf;
				panelRoot.SetActive(value: false);
				UpdateReopenButton();
				if (wasActive)
				{
					this.OnChoiceClosed?.Invoke();
				}
			}
		}

		private void SetChoiceOpen(bool open)
		{
			if (panelRoot != null)
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
			int currentRound = ((gameController != null) ? gameController.CurrentRound : pendingChoiceRound);
			return currentRound >= pendingChoiceRound;
		}

		private bool ShouldDelayChoiceForShop(int round)
		{
			return gameController != null && round < Mathf.Max(1, shopOverlapAllowedRound) && gameController.WasRoundShopOpened(round) && (HasPendingChoiceData || round >= nextChoiceRound);
		}

		private void DelayChoiceForShop(int round)
		{
			int delayedRound = Mathf.Max(round + 1, Mathf.Max(1, firstChoiceRound));
			if (HasPendingChoiceData)
			{
				pendingChoiceRound = Mathf.Max(delayedRound, pendingChoiceRound);
			}
			if (nextChoiceRound <= round)
			{
				nextChoiceRound = delayedRound;
			}
			if (panelRoot != null && panelRoot.activeSelf)
			{
				panelRoot.SetActive(value: false);
				this.OnChoiceClosed?.Invoke();
			}
			UpdateReopenButton();
			if (gameController != null && round >= firstChoiceRound)
			{
				gameController.RequestBanner("증강체 선택은 다음 라운드로 연기됩니다", new Color(0.72f, 0.88f, 1f), 1.8f);
			}
		}

		private void UpdateReopenButton()
		{
			if ((UnityEngine.Object)(object)reopenButton != null)
			{
				((Component)(object)reopenButton).gameObject.SetActive(HasPendingChoice && !IsChoiceOpen);
			}
		}

		private void ApplyPermanentAugmentToDefender(DefenderUnit defender, AugmentDefinition augment)
		{
			if (defender == null || augment == null)
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
			if (!(defender == null) && augment != null && augment.effectType == AugmentEffectType.RoundStartBurst)
			{
				defender.ActivateTimedCombatBoost(augment.value, augment.secondaryValue, Mathf.Max(1f, augment.duration));
			}
		}

		private void ApplyBuildupTotal(DefenderUnit defender, AugmentDefinition augment)
		{
			int stacks = GetBuildupStacks(augment);
			if (stacks > 0)
			{
				ApplyBuildupBonus(defender, augment, stacks);
			}
		}

		private void ApplyBuildupIncrement(DefenderUnit defender, AugmentDefinition augment)
		{
			ApplyBuildupBonus(defender, augment, 1);
		}

		private void ApplyBuildupBonus(DefenderUnit defender, AugmentDefinition augment, int stacks)
		{
			if (!(defender == null) && augment != null && stacks > 0)
			{
				float amount = augment.value * (float)stacks;
				switch (augment.effectType)
				{
				case AugmentEffectType.ScalingAttackPowerPerRound:
					defender.AddAttackPowerBonus(amount);
					break;
				case AugmentEffectType.ScalingAttackSpeedPerRound:
					defender.AddPermanentAttackSpeedBonus(amount);
					break;
				case AugmentEffectType.ScalingSkillPowerPerRound:
					defender.AddSkillPowerBonus(amount);
					break;
				case AugmentEffectType.ScalingManaRegenPerRound:
					defender.AddManaRegenRateBonus(amount);
					break;
				case AugmentEffectType.ScalingBossDamagePerRound:
					defender.AddBossDamageBonus(amount);
					break;
				}
			}
		}

		private void ApplyEconomyAugment(AugmentDefinition augment)
		{
			if (!(gameController == null) && augment != null)
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
					int minGold = Mathf.Max(0, Mathf.RoundToInt(augment.value));
					int maxGold = Mathf.Max(minGold, Mathf.RoundToInt(augment.secondaryValue));
					int reward = UnityEngine.Random.Range(minGold, maxGold + 1);
					gameController.AddGold(reward);
					ShowRandomGoldFeedback(augment, reward, minGold, maxGold);
					break;
				}
				case AugmentEffectType.WorkerGoldPerRound:
					break;
				}
			}
		}

		private void ResolveRoundStartedEconomy(AugmentDefinition augment, int round)
		{
			if (!(gameController == null) && augment != null)
			{
				if (augment.effectType == AugmentEffectType.WorkerGoldPerRound)
				{
					int reward = Mathf.Max(1, Mathf.RoundToInt(augment.value + (float)round * augment.secondaryValue));
					gameController.AddGold(reward);
					ShowEconomyBanner("광부 채굴 +" + reward + "G", augment.accentColor, 1.7f, 1.2f);
				}
				else if (augment.effectType == AugmentEffectType.ScalingGoldPerRound)
				{
					int stacks = GetBuildupStacks(augment);
					int reward2 = Mathf.Max(1, Mathf.RoundToInt(augment.value * (float)stacks));
					gameController.AddGold(reward2);
					ShowEconomyBanner("복리 광산 +" + reward2 + "G", augment.accentColor, 1.7f, 1.2f);
				}
			}
		}

		private void ResolveRoundCompletedEconomy(int round)
		{
			if (gameController == null)
			{
				return;
			}
			for (int i = 0; i < chosenAugments.Count; i++)
			{
				AugmentDefinition augment = chosenAugments[i];
				if (augment != null)
				{
					if (augment.effectType == AugmentEffectType.RoundClearRandomGold)
					{
						int minGold = Mathf.Max(0, Mathf.RoundToInt(augment.value));
						int maxGold = Mathf.Max(minGold, Mathf.RoundToInt(augment.secondaryValue));
						int reward = UnityEngine.Random.Range(minGold, maxGold + 1);
						gameController.AddGold(reward);
						ShowRandomGoldFeedback(augment, reward, minGold, maxGold);
					}
					else if (augment.effectType == AugmentEffectType.BossRoundBet && round > 0 && round % 10 == 0)
					{
						int minGold2 = Mathf.Max(1, Mathf.RoundToInt(augment.value));
						int maxGold2 = Mathf.Max(minGold2, Mathf.RoundToInt(augment.value + augment.secondaryValue));
						int reward2 = Mathf.Clamp(Mathf.RoundToInt(augment.value + UnityEngine.Random.Range(0f, augment.secondaryValue)), minGold2, maxGold2);
						gameController.AddGold(reward2);
						ShowRandomGoldFeedback(augment, reward2, minGold2, maxGold2);
					}
					else if (augment.effectType == AugmentEffectType.InterestGold)
					{
						int cap = Mathf.Max(1, Mathf.RoundToInt(augment.secondaryValue));
						int reward3 = Mathf.Clamp(Mathf.RoundToInt((float)gameController.Gold * Mathf.Max(0f, augment.value)), 1, cap);
						gameController.AddGold(reward3);
						ShowEconomyBanner("이자 수익 +" + reward3 + "G", augment.accentColor, 1.7f, 1.2f);
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
			int stacks;
			return buildupStacks.TryGetValue(augment.id, out stacks) ? Mathf.Max(0, stacks) : 0;
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
			int heroCount = CountFieldHero(augment.requiredHeroId);
			if (heroCount <= 0 || string.IsNullOrEmpty(augment.id))
			{
				return false;
			}
			int round = ((pendingChoiceRound > 0) ? pendingChoiceRound : ((!(gameController != null)) ? 1 : gameController.CurrentRound));
			if (augment.heroTier == HeroAugmentTier.Rare && round < rareHeroAugmentUnlockRound)
			{
				return false;
			}
			if (augment.heroTier == HeroAugmentTier.Mythic && round < mythicHeroAugmentUnlockRound)
			{
				return false;
			}
			if (!heroAugmentOfferRolls.TryGetValue(augment.id, out var canOffer))
			{
				float baseChance = GetHeroAugmentBaseOfferChance(augment.heroTier);
				float maxChance = GetHeroAugmentMaxOfferChance(augment.heroTier);
				float chance = Mathf.Clamp(baseChance + (float)Mathf.Max(0, heroCount - 1) * extraHeroCopyOfferBonus, 0f, maxChance);
				canOffer = UnityEngine.Random.value <= chance;
				heroAugmentOfferRolls[augment.id] = canOffer;
			}
			return canOffer;
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
			int heroCount = CountFieldHero(heroId);
			if (heroCount <= 0 || augment == null || string.IsNullOrEmpty(augment.id))
			{
				return false;
			}
			if (!heroAugmentOfferRolls.TryGetValue(augment.id, out var canOffer))
			{
				float chance = Mathf.Clamp01(baseChance + (float)Mathf.Max(0, heroCount - 1) * 0.18f);
				canOffer = UnityEngine.Random.value <= chance;
				heroAugmentOfferRolls[augment.id] = canOffer;
			}
			return canOffer;
		}

		private int CountFieldHero(string heroId)
		{
			if (string.IsNullOrEmpty(heroId))
			{
				return 0;
			}
			int count = 0;
			DefenderUnit[] defenders = UnityEngine.Object.FindObjectsOfType<DefenderUnit>();
			for (int i = 0; i < defenders.Length; i++)
			{
				if (IsHero(defenders[i], heroId))
				{
					count++;
				}
			}
			return count;
		}

		private bool IsHero(DefenderUnit defender, string heroId)
		{
			return defender != null && defender.Definition != null && string.Equals(defender.Definition.id, heroId, StringComparison.OrdinalIgnoreCase);
		}

		private void TryResolveHero08PetrifyBloom(DefenderUnit source, MonsterUnit target, AugmentDefinition augment)
		{
			if (IsHero(source, "hero_08") && !(UnityEngine.Random.value > Mathf.Clamp01(augment.value)))
			{
				MonsterUnit.ApplyPetrifyRadius(target.transform.position, Mathf.Max(0.1f, augment.secondaryValue), new MonsterUnit.PetrifyTargetOptions
				{
					duration = Mathf.Max(0.1f, augment.duration),
					maxTargets = 0,
					excludeBosses = true
				});
			}
		}

		private void TryResolveHero01VolcanicAftershock(DefenderUnit source, MonsterUnit target, float damage, AugmentDefinition augment)
		{
			if (IsHero(source, "hero_01") && !(UnityEngine.Random.value > Mathf.Clamp01(augment.value)))
			{
				ApplyHeroAreaDamage(source, target, target.transform.position, Mathf.Max(0.1f, augment.duration), damage * Mathf.Max(0f, augment.secondaryValue));
			}
		}

		private void TryResolveHero03FrostResidue(DefenderUnit source, MonsterUnit target, AugmentDefinition augment)
		{
			if (IsHero(source, "hero_03") && !(UnityEngine.Random.value > Mathf.Clamp01(augment.value)))
			{
				ApplyHeroSlowField(target.transform.position, Mathf.Max(0.1f, augment.secondaryValue), 0.38f, Mathf.Max(0.1f, augment.duration));
			}
		}

		private void TryResolveHero13ManaNetwork(DefenderUnit source, AugmentDefinition augment)
		{
			if (IsHero(source, "hero_13") && !(UnityEngine.Random.value > Mathf.Clamp01(augment.value)))
			{
				RestoreNearbyDefenderMana(source, Mathf.Max(0.1f, augment.secondaryValue), Mathf.Max(0.01f, augment.duration));
			}
		}

		private void ApplyHeroAreaDamage(DefenderUnit source, MonsterUnit centerTarget, Vector3 center, float radius, float damage)
		{
			if (source == null || damage <= 0f)
			{
				return;
			}
			float radiusSqr = radius * radius;
			IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
			resolvingHeroAugmentDamage = true;
			try
			{
				for (int i = 0; i < monsters.Count; i++)
				{
					MonsterUnit monster = monsters[i];
					if (!(monster == null) && !(monster == centerTarget))
					{
						Vector3 offset = monster.transform.position - center;
						offset.y = 0f;
						if (offset.sqrMagnitude <= radiusSqr)
						{
							monster.TakeDamage(damage, critical: false, source);
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
			float radiusSqr = radius * radius;
			IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
			for (int i = 0; i < monsters.Count; i++)
			{
				MonsterUnit monster = monsters[i];
				if (!(monster == null) && !monster.IsBoss)
				{
					Vector3 offset = monster.transform.position - center;
					offset.y = 0f;
					if (offset.sqrMagnitude <= radiusSqr)
					{
						monster.ApplySlow(Mathf.Clamp01(slowRatio), duration);
					}
				}
			}
		}

		private void RestoreNearbyDefenderMana(DefenderUnit source, float radius, float manaRatio)
		{
			if (source == null)
			{
				return;
			}
			float radiusSqr = radius * radius;
			DefenderUnit[] defenders = UnityEngine.Object.FindObjectsOfType<DefenderUnit>();
			foreach (DefenderUnit defender in defenders)
			{
				if (!(defender == null))
				{
					Vector3 offset = defender.transform.position - source.transform.position;
					offset.y = 0f;
					if (offset.sqrMagnitude <= radiusSqr)
					{
						defender.RestoreMana(manaRatio);
					}
				}
			}
		}

		private void ApplyHero05GuardianProtocol(DefenderUnit[] defenders, AugmentDefinition augment)
		{
			if (defenders == null || defenders.Length == 0 || augment == null)
			{
				return;
			}
			float radius = Mathf.Max(0.1f, augment.secondaryValue);
			float radiusSqr = radius * radius;
			float duration = Mathf.Max(1f, augment.duration);
			HashSet<DefenderUnit> shielded = new HashSet<DefenderUnit>();
			foreach (DefenderUnit source in defenders)
			{
				if (!IsHero(source, "hero_05"))
				{
					continue;
				}
				foreach (DefenderUnit target in defenders)
				{
					if (!(target == null) && !shielded.Contains(target))
					{
						Vector3 offset = target.transform.position - source.transform.position;
						offset.y = 0f;
						if (!(offset.sqrMagnitude > radiusSqr))
						{
							target.AddShield(target.MaxHealth * Mathf.Max(0f, augment.value), duration);
							shielded.Add(target);
						}
					}
				}
			}
		}

		private void ResolveHero02SkillAugments(DefenderUnit source, SkillDefinition skill)
		{
			List<DefenderUnit> allies = FindLowestHealthDefenders(HasChosen("hero02_emergency_m") ? 3 : 2, includeHealthy: true);
			if (HasChosen("hero02_battle_rx_n"))
			{
				ApplyCombatBoostToDefenders(allies, 0.1f, 0.08f, 4f);
			}
			if (HasChosen("hero02_crit_heal_r") && UnityEngine.Random.value <= 0.35f)
			{
				ApplyHealAndShieldToDefenders(allies, 0.14f, 0.08f, 4f);
			}
			if (HasChosen("hero02_emergency_m"))
			{
				ApplyHealAndShieldToDefenders(allies, 0.18f, 0.12f, 5f);
				ApplyCombatBoostToDefenders(allies, 0.16f, 0.16f, 5f);
				for (int i = 0; i < allies.Count; i++)
				{
					allies[i].RestoreMana(0.08f);
				}
			}
		}

		private void ResolveHero04SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
		{
			Vector3 center = ResolveSkillCenter(source, target, 2.4f);
			float attackPower = GetHeroAttackPower(source);
			if (HasChosen("hero04_toxic_pool_n"))
			{
				SpawnHeroDamageZone(source, skill, center, 2.3f, attackPower * 0.28f, 3.5f, 0.7f);
				if (target != null && target.IsBoss)
				{
					target.TakeDamage(attackPower * 0.4f, critical: false, source);
				}
			}
			if (HasChosen("hero04_plague_cloud_m"))
			{
				ApplyHeroPoisonRadius(source, center, 3.1f, attackPower * 0.36f, 5f, 0.75f, attackPower * 0.9f);
				ApplyHeroSlowField(center, 3.1f, 0.28f, 3f);
			}
		}

		private void ResolveHero06SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
		{
			float attackPower = GetHeroAttackPower(source);
			if (HasChosen("hero06_spear_throw_n") && UnityEngine.Random.value <= 0.35f)
			{
				ApplyHeroLineDamage(source, target, 5.2f, 0.58f, attackPower * 0.85f);
			}
			if (HasChosen("hero06_spear_path_r"))
			{
				int hitCount = ApplyHeroLineDamage(source, target, 5.6f, 0.7f, attackPower * 0.58f);
				if (hitCount >= 2)
				{
					source.ActivateTimedCombatBoost(0.08f, 0.18f, 4f, null, "창 기세", new Color(0.78f, 0.88f, 1f));
				}
			}
			if (HasChosen("hero06_storm_lance_m"))
			{
				ApplyHeroLineDamage(source, target, 6.4f, 0.95f, attackPower * 1.25f);
				TriggerSingleSkillEcho(source, skill, 0.48f);
			}
		}

		private void ResolveHero07SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
		{
			if (!(target == null) && HasChosen("hero07_soul_scythe_n"))
			{
				float attackPower = GetHeroAttackPower(source);
				if (target.IsBoss)
				{
					target.TakeDamage(attackPower * 0.45f, critical: false, source);
					return;
				}
				target.ApplySlow(0.24f, 2.5f);
				target.TakeDamage(Mathf.Min(target.MaxHealth * 0.1f, attackPower * 1.15f), critical: false, source);
			}
		}

		private void ResolveHero01SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
		{
			Vector3 center = ((target != null) ? target.transform.position : (source.transform.position + source.transform.forward * 2.5f));
			float attackPower = ((source.Definition != null) ? source.Definition.stats.attackPower : 1f);
			if (HasChosen("hero01_fire_field_n"))
			{
				SpawnHeroDamageZone(source, skill, center, 2.4f, attackPower * 0.35f, 3f, 0.6f);
			}
			if (HasChosen("hero01_flame_pierce_m"))
			{
				ApplyHeroLineDamage(source, target, 5f, 0.65f, attackPower * 1.05f);
			}
		}

		private void ResolveHero03SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
		{
			if (target == null)
			{
				return;
			}
			float attackPower = ((source.Definition != null) ? source.Definition.stats.attackPower : 1f);
			bool hasControlAugment = HasChosen("hero03_freeze_n") || HasChosen("hero03_permafrost_r") || HasChosen("hero03_shatter_m");
			if (target.IsBoss)
			{
				if (hasControlAugment)
				{
					target.TakeDamage(attackPower * 0.25f, critical: false, source);
				}
				return;
			}
			if (HasChosen("hero03_freeze_n") && UnityEngine.Random.value <= 0.25f)
			{
				target.ApplyStun(1.15f);
			}
			if (HasChosen("hero03_permafrost_r") && UnityEngine.Random.value <= 0.2f)
			{
				target.ApplySlow(0.45f, 999f);
				target.ApplyAttackSpeedSlow(0.45f, 999f);
			}
			if (HasChosen("hero03_shatter_m"))
			{
				target.ApplyStun(1.2f);
				StartCoroutine(DelayedHeroAreaDamage(source, target.transform.position, 1.2f, 2.8f, attackPower * 0.95f));
			}
		}

		private void ResolveHero08SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
		{
			if (target == null)
			{
				return;
			}
			float attackPower = ((source.Definition != null) ? source.Definition.stats.attackPower : 1f);
			if (HasChosen("hero08_petrify_spread_n"))
			{
				if (target.IsBoss)
				{
					target.TakeDamage(attackPower * 0.25f, critical: false, source);
				}
				else
				{
					target.ApplyPetrify(2.3f);
				}
			}
			if (HasChosen("hero08_gallery_r") && CountPetrifiedNonBossMonsters() >= 3)
			{
				ApplyHeroAreaDamage(source, null, target.transform.position, 3f, attackPower * 1.1f);
			}
			if (HasChosen("hero08_trophy_m") && !target.IsBoss && UnityEngine.Random.value <= 0.25f)
			{
				target.ApplyPetrify(3f);
			}
		}

		private void ResolveHero09SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
		{
			float attackPower = GetHeroAttackPower(source);
			if (HasChosen("hero09_front_cleave_n"))
			{
				ApplyHeroLineDamage(source, target, 5.2f, 0.85f, attackPower * 0.72f);
			}
			if (HasChosen("hero09_backline_r"))
			{
				DamageFarthestAdditionalMonsters(source, target, 2, attackPower * 0.72f);
			}
			if (HasChosen("hero09_twin_dragon_m"))
			{
				ApplyHeroLineDamage(source, target, 6.5f, 1.05f, attackPower * 1.1f);
				DamageFarthestAdditionalMonsters(source, target, 3, attackPower * 0.85f);
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
			float attackPower = GetHeroAttackPower(source);
			if (HasChosen("hero11_blood_bank_n"))
			{
				AddTrackedMaxHealth(source, hero11MaxHealthGrowth, 0.03f, 0.18f);
				source.Heal(source.MaxHealth * 0.08f);
			}
			if (HasChosen("hero11_blood_surge_r"))
			{
				AddTrackedMaxHealth(source, hero11MaxHealthGrowth, 0.05f, 0.3f);
				ApplyHeroAreaDamage(source, target, ResolveSkillCenter(source, target, 2.2f), 2.6f, attackPower * 0.55f);
			}
			if (HasChosen("hero11_crimson_feast_m"))
			{
				AddTrackedMaxHealth(source, hero11MaxHealthGrowth, 0.07f, 0.45f);
				Vector3 center = ResolveSkillCenter(source, target, 2.4f);
				ApplyHeroAreaDamage(source, null, center, 3.2f, attackPower * 0.85f);
				source.Heal(source.MaxHealth * 0.16f);
				source.AddShield(source.MaxHealth * 0.14f, 4f);
			}
		}

		private void ResolveHero12SkillAugments(DefenderUnit source, SkillDefinition skill)
		{
			if (HasChosen("hero12_chain_cast_m"))
			{
				float chance = 0.55f;
				int repeats = 0;
				while (repeats < 3 && UnityEngine.Random.value <= chance)
				{
					TriggerSingleSkillEcho(source, skill, 0.58f);
					source.RestoreMana(0.08f);
					repeats++;
					chance -= 0.15f;
				}
			}
			else if (HasChosen("hero12_double_cast_r") && UnityEngine.Random.value <= 0.35f)
			{
				TriggerSingleSkillEcho(source, skill, 0.62f);
				source.RestoreMana(0.12f);
			}
			else if (HasChosen("hero12_echo_cast_n") && UnityEngine.Random.value <= 0.2f)
			{
				TriggerSingleSkillEcho(source, skill, 0.45f);
			}
		}

		private void ResolveHero13SkillAugments(DefenderUnit source)
		{
			if (HasChosen("hero13_overcharge_n"))
			{
				RestoreNearbyDefenderMana(source, 3.2f, 0.08f);
				BoostNearbyDefenders(source.transform.position, 3.2f, 0.12f, 3f);
			}
			if (HasChosen("hero13_mana_gamble_r") && UnityEngine.Random.value <= 0.5f)
			{
				source.RestoreMana(0.5f);
			}
			if (HasChosen("hero13_chain_battery_m"))
			{
				RestoreLowestManaDefenders(source.transform.position, 5.5f, 0.18f, 3);
			}
		}

		private void ResolveHero14SkillAugments(DefenderUnit source)
		{
			if (HasChosen("hero14_whole_army_m"))
			{
				List<DefenderUnit> allDefenders = FindRandomDefenders(99, excludeDead: true);
				ApplyCombatBoostToDefenders(allDefenders, 0.16f, 0.38f, 6f);
				RestoreLowestManaDefenders(source.transform.position, 99f, 0.1f, 3);
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
			float attackPower = GetHeroAttackPower(source);
			if (HasChosen("hero22_thorn_shell_n"))
			{
				source.AddShield(source.MaxHealth * 0.18f, 4f);
				source.ActivateTimedDamageReduction(0.16f, 4f);
			}
			if (HasChosen("hero22_reflect_field_r"))
			{
				StartHero54RetaliationWindow(source, 4.5f, 0.35f, 3f, 0f);
				ShieldNearbyDefenders(source.transform.position, 3f, 0.08f, 4f);
			}
			if (HasChosen("hero22_thorn_crown_m"))
			{
				source.ActivateTimedDamageReduction(0.34f, 5f);
				ShieldNearbyDefenders(source.transform.position, 3.6f, 0.16f, 5f);
				ApplyHeroAreaDamage(source, null, source.transform.position, 3.4f, attackPower * 0.85f);
			}
		}

		private void ResolveHero23SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
		{
			if (!(target == null))
			{
				float attackPower = GetHeroAttackPower(source);
				if (HasChosen("hero23_power_hit_n"))
				{
					target.TakeDamage(attackPower * 0.75f, critical: false, source);
				}
				if (HasChosen("hero23_weakpoint_r"))
				{
					float damage = ((target.CurrentHealth <= target.MaxHealth * 0.5f) ? (attackPower * 1.35f) : (attackPower * 0.55f));
					target.TakeDamage(damage, target.CurrentHealth <= target.MaxHealth * 0.5f, source);
				}
				if (HasChosen("hero23_royal_break_m"))
				{
					target.TakeDamage(attackPower * 1.3f, critical: true, source);
					DamageNearestAdditionalMonsters(source, target, 3, attackPower * 0.75f);
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
			float attackPower = GetHeroAttackPower(source);
			source.ActivateTimedCombatBoost(0f, 0.25f, 5f);
			if (target != null && target.CurrentHealth > 0f)
			{
				target.ApplyPoison(attackPower * 0.3f, 4f, 1f, source);
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
			Vector3 center = ResolveSkillCenter(source, target, 2.6f);
			float attackPower = GetHeroAttackPower(source);
			if (HasChosen("hero33_poison_remnant_n"))
			{
				SpawnHeroDamageZone(source, skill, center, 2.7f, attackPower * 0.32f, 4f, 0.8f);
			}
			if (HasChosen("hero33_wandering_mist_r"))
			{
				StartCoroutine(DelayedRandomHeroDamageZones(source, skill, 4f, 1, 2.4f, attackPower * 0.34f, 3.2f, 0.8f));
			}
			if (HasChosen("hero33_endless_miasma_m"))
			{
				SpawnHeroDamageZone(source, skill, center, 3.2f, attackPower * 0.46f, 5f, 0.75f);
				ApplyHeroSlowField(center, 3.2f, 0.28f, 3.5f);
				StartCoroutine(DelayedRandomHeroDamageZones(source, skill, 3.5f, 2, 2.6f, attackPower * 0.38f, 3.2f, 0.75f));
			}
		}

		private void ResolveHero51SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
		{
			float attackPower = ((source.Definition != null) ? source.Definition.stats.attackPower : 1f);
			if (HasChosen("hero51_chain_lightning_n"))
			{
				DamageNearestAdditionalMonsters(source, target, 2, attackPower * 0.45f);
			}
			if (HasChosen("hero51_overload_circuit_m"))
			{
				float chance = 0.45f;
				int repeats = 0;
				while (repeats < 5 && UnityEngine.Random.value <= chance)
				{
					TriggerSingleSkillEcho(source, skill, 0.42f);
					repeats++;
					chance -= 0.1f;
				}
			}
			else if (HasChosen("hero51_residual_lightning_r") && UnityEngine.Random.value <= 0.3f)
			{
				TriggerSingleSkillEcho(source, skill, 0.45f);
			}
		}

		private void ResolveHero52SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
		{
			Vector3 center = ((target != null) ? target.transform.position : (source.transform.position + source.transform.forward * 2.4f));
			float attackPower = ((source.Definition != null) ? source.Definition.stats.attackPower : 1f);
			if (HasChosen("hero52_burning_fallout_n"))
			{
				SpawnHeroDamageZone(source, skill, center, 2.5f, attackPower * 0.32f, 3.5f, 0.7f);
			}
			if (HasChosen("hero52_star_shower_m"))
			{
				for (int i = 0; i < 2; i++)
				{
					Vector3 offset = UnityEngine.Random.insideUnitSphere * 2.2f;
					offset.y = 0f;
					SpawnHeroDamageZone(source, skill, center + offset, 1.6f, attackPower * 0.28f, 2.2f, 0.7f);
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
			float attackPower = GetHeroAttackPower(source);
			Vector3 center = ResolveSkillCenter(source, target, 2f);
			if (HasChosen("hero55_impact_guard_n"))
			{
				source.AddShield(source.MaxHealth * 0.12f, 5f);
			}
			if (HasChosen("hero55_wall_quake_r"))
			{
				ApplyHeroAreaDamage(source, target, center, 3f, attackPower * 0.9f);
			}
			if (HasChosen("hero55_mobile_fortress_m"))
			{
				ApplyHeroAreaDamage(source, target, center, 3.4f, attackPower * 1.4f);
				source.AddShield(source.MaxHealth * 0.2f, 6f);
				source.ActivateTimedDamageReduction(0.3f, 6f);
			}
		}

		private void ResolveHero56SkillAugments(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
		{
			float attackPower = GetHeroAttackPower(source);
			Vector3 center = ResolveSkillCenter(source, target, 3f);
			if (HasChosen("hero56_after_blast_n"))
			{
				ApplyHeroAreaDamage(source, target, center, 3f, attackPower * 0.75f);
			}
			if (HasChosen("hero56_twin_barrage_r"))
			{
				TriggerSingleSkillEcho(source, skill, 0.5f);
			}
			if (HasChosen("hero56_orbital_restrike_m"))
			{
				TriggerSingleSkillEcho(source, skill, 0.8f);
				ApplyHeroAreaDamage(source, target, center, 3.6f, attackPower * 1.2f);
			}
		}

		private void ResolveHero57SkillAugments(DefenderUnit source)
		{
			float attackPower = GetHeroAttackPower(source);
			if (HasChosen("hero57_spare_mag_n"))
			{
				DamageRandomMonsters(source, 2, attackPower * 0.7f);
			}
			if (HasChosen("hero57_ricochet_r"))
			{
				DamageRandomMonsters(source, 4, attackPower * 0.8f);
			}
			if (HasChosen("hero57_chaos_mag_m"))
			{
				DamageRandomMonsters(source, 6, attackPower * 0.9f);
			}
		}

		private void TriggerHeroSkillEchoes(string heroId, DefenderUnit originalSource, SkillDefinition skill, float multiplier, int maxEchoes)
		{
			List<DefenderUnit> heroes = FindFieldHeroes(heroId);
			int triggered = 0;
			resolvingHeroSkillEcho = true;
			try
			{
				for (int i = 0; i < heroes.Count; i++)
				{
					DefenderUnit hero = heroes[i];
					if (!(hero == null) && !(hero == originalSource))
					{
						hero.TriggerAugmentSkillEcho(skill, multiplier);
						triggered++;
						if (triggered >= maxEchoes)
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
			List<DefenderUnit> result = new List<DefenderUnit>();
			DefenderUnit[] defenders = UnityEngine.Object.FindObjectsOfType<DefenderUnit>();
			for (int i = 0; i < defenders.Length; i++)
			{
				if (IsHero(defenders[i], heroId))
				{
					result.Add(defenders[i]);
				}
			}
			return result;
		}

		private void BoostNearbyDefenders(Vector3 center, float radius, float attackSpeedRatio, float duration)
		{
			float radiusSqr = radius * radius;
			DefenderUnit[] defenders = UnityEngine.Object.FindObjectsOfType<DefenderUnit>();
			foreach (DefenderUnit defender in defenders)
			{
				if (!(defender == null))
				{
					Vector3 offset = defender.transform.position - center;
					offset.y = 0f;
					if (offset.sqrMagnitude <= radiusSqr)
					{
						defender.ActivateTimedCombatBoost(0f, attackSpeedRatio, duration);
					}
				}
			}
		}

		private void ShieldNearbyDefenders(Vector3 center, float radius, float shieldRatio, float duration)
		{
			float radiusSqr = radius * radius;
			DefenderUnit[] defenders = UnityEngine.Object.FindObjectsOfType<DefenderUnit>();
			foreach (DefenderUnit defender in defenders)
			{
				if (!(defender == null))
				{
					Vector3 offset = defender.transform.position - center;
					offset.y = 0f;
					if (offset.sqrMagnitude <= radiusSqr)
					{
						defender.AddShield(defender.MaxHealth * shieldRatio, duration);
					}
				}
			}
		}

		private void RestoreLowestManaDefenders(Vector3 center, float radius, float manaRatio, int count, DefenderUnit excludedDefender = null)
		{
			List<DefenderUnit> candidates = new List<DefenderUnit>();
			float radiusSqr = radius * radius;
			DefenderUnit[] defenders = UnityEngine.Object.FindObjectsOfType<DefenderUnit>();
			foreach (DefenderUnit defender in defenders)
			{
				if (!(defender == null) && !(defender == excludedDefender) && !(defender.MaxMana <= 0f) && !(defender.CurrentHealth <= 0f))
				{
					Vector3 offset = defender.transform.position - center;
					offset.y = 0f;
					if (offset.sqrMagnitude <= radiusSqr)
					{
						candidates.Add(defender);
					}
				}
			}
			candidates.Sort((DefenderUnit a, DefenderUnit b) => (a.CurrentMana / Mathf.Max(1f, a.MaxMana)).CompareTo(b.CurrentMana / Mathf.Max(1f, b.MaxMana)));
			int checkedCount = Mathf.Min(Mathf.Max(1, count), candidates.Count);
			for (int i2 = 0; i2 < checkedCount; i2++)
			{
				candidates[i2].RestoreMana(manaRatio);
			}
		}

		private List<DefenderUnit> FindLowestHealthDefenders(int count, bool includeHealthy)
		{
			List<DefenderUnit> candidates = new List<DefenderUnit>();
			DefenderUnit[] defenders = UnityEngine.Object.FindObjectsOfType<DefenderUnit>();
			foreach (DefenderUnit defender in defenders)
			{
				if (!(defender == null) && !(defender.CurrentHealth <= 0f) && (includeHealthy || !(defender.HealthRatio >= 0.98f)))
				{
					candidates.Add(defender);
				}
			}
			candidates.Sort((DefenderUnit a, DefenderUnit b) => a.HealthRatio.CompareTo(b.HealthRatio));
			int checkedCount = Mathf.Min(Mathf.Max(1, count), candidates.Count);
			if (checkedCount < candidates.Count)
			{
				candidates.RemoveRange(checkedCount, candidates.Count - checkedCount);
			}
			return candidates;
		}

		private List<DefenderUnit> FindRandomDefenders(int count, bool excludeDead)
		{
			List<DefenderUnit> candidates = new List<DefenderUnit>();
			DefenderUnit[] defenders = UnityEngine.Object.FindObjectsOfType<DefenderUnit>();
			foreach (DefenderUnit defender in defenders)
			{
				if (!(defender == null) && (!excludeDead || !(defender.CurrentHealth <= 0f)))
				{
					candidates.Add(defender);
				}
			}
			for (int j = 0; j < candidates.Count; j++)
			{
				int swapIndex = UnityEngine.Random.Range(j, candidates.Count);
				DefenderUnit temp = candidates[j];
				candidates[j] = candidates[swapIndex];
				candidates[swapIndex] = temp;
			}
			int checkedCount = Mathf.Min(Mathf.Max(1, count), candidates.Count);
			if (checkedCount < candidates.Count)
			{
				candidates.RemoveRange(checkedCount, candidates.Count - checkedCount);
			}
			return candidates;
		}

		private void ApplyCombatBoostToDefenders(List<DefenderUnit> defenders, float attackPowerRatio, float attackSpeedRatio, float duration)
		{
			if (defenders == null)
			{
				return;
			}
			for (int i = 0; i < defenders.Count; i++)
			{
				if (defenders[i] != null)
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
				DefenderUnit defender = defenders[i];
				if (!(defender == null))
				{
					defender.Heal(defender.MaxHealth * Mathf.Max(0f, healRatio));
					if (shieldRatio > 0f)
					{
						defender.AddShield(defender.MaxHealth * shieldRatio, shieldDuration);
					}
				}
			}
		}

		private void AddTrackedMaxHealth(DefenderUnit source, Dictionary<DefenderUnit, float> tracker, float ratio, float cap)
		{
			if (!(source == null) && tracker != null && !(ratio <= 0f) && !(cap <= 0f))
			{
				float value;
				float current = (tracker.TryGetValue(source, out value) ? value : 0f);
				float added = Mathf.Min(ratio, Mathf.Max(0f, cap - current));
				if (!(added <= 0f))
				{
					tracker[source] = current + added;
					source.AddMaxHealthBonus(added);
				}
			}
		}

		private float GetHeroAttackPower(DefenderUnit source)
		{
			return (source != null && source.Definition != null) ? Mathf.Max(1f, source.Definition.stats.attackPower) : 1f;
		}

		private Vector3 ResolveSkillCenter(DefenderUnit source, MonsterUnit target, float forwardDistance)
		{
			if (target != null)
			{
				return target.transform.position;
			}
			if (source != null)
			{
				return source.transform.position + source.transform.forward * Mathf.Max(0.5f, forwardDistance);
			}
			return Vector3.zero;
		}

		private Vector3 ResolveRandomMonsterPosition(DefenderUnit source, float fallbackForwardDistance)
		{
			List<MonsterUnit> candidates = new List<MonsterUnit>();
			IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
			for (int i = 0; i < monsters.Count; i++)
			{
				if (monsters[i] != null && monsters[i].CanBeCombatTargeted)
				{
					candidates.Add(monsters[i]);
				}
			}
			if (candidates.Count > 0)
			{
				return candidates[UnityEngine.Random.Range(0, candidates.Count)].transform.position;
			}
			return (source != null) ? (source.transform.position + source.transform.forward * Mathf.Max(0.5f, fallbackForwardDistance)) : Vector3.zero;
		}

		private void ApplyHeroPoisonRadius(DefenderUnit source, Vector3 center, float radius, float damagePerTick, float duration, float tickInterval, float bossDamage)
		{
			float radiusSqr = radius * radius;
			IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
			for (int i = 0; i < monsters.Count; i++)
			{
				MonsterUnit monster = monsters[i];
				if (monster == null || !monster.CanBeCombatTargeted)
				{
					continue;
				}
				Vector3 offset = monster.transform.position - center;
				offset.y = 0f;
				if (!(offset.sqrMagnitude > radiusSqr))
				{
					if (monster.IsBoss)
					{
						monster.TakeDamage(bossDamage, critical: false, source);
					}
					else
					{
						monster.ApplyPoison(damagePerTick, duration, tickInterval, source);
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
			if (!(killer == null) && HasAnyChosen("hero07_soul_scythe_n", "hero07_reaper_form_r", "hero07_reaper_execute_m"))
			{
				int value;
				int kills = ((!hero07KillCounts.TryGetValue(killer, out value)) ? 1 : (value + 1));
				hero07KillCounts[killer] = kills;
				if (HasChosen("hero07_soul_scythe_n") && kills % 3 == 0)
				{
					killer.RestoreMana(0.12f);
				}
				if ((HasChosen("hero07_reaper_form_r") || HasChosen("hero07_reaper_execute_m")) && kills >= 10 && !hero07ReaperActive.Contains(killer))
				{
					hero07ReaperActive.Add(killer);
					hero07StrikeCounts[killer] = 0;
					killer.ActivateTimedCombatBoost(0.35f, 0.45f, 24f, null, "사신화", new Color(0.62f, 0.18f, 0.86f));
				}
			}
		}

		private void ResolveHero07ReaperStrike(DefenderUnit source, MonsterUnit target)
		{
			if (source == null || target == null || !HasChosen("hero07_reaper_execute_m") || !hero07ReaperActive.Contains(source))
			{
				return;
			}
			int value;
			int strikes = ((!hero07StrikeCounts.TryGetValue(source, out value)) ? 1 : (value + 1));
			hero07StrikeCounts[source] = strikes;
			if (strikes % 3 != 0)
			{
				return;
			}
			float attackPower = GetHeroAttackPower(source);
			resolvingHeroAugmentDamage = true;
			try
			{
				if (target.IsBoss)
				{
					target.TakeDamage(attackPower * 2f, critical: true, source);
					return;
				}
				float executeDamage = ((target.CurrentHealth <= target.MaxHealth * 0.45f) ? (target.MaxHealth * 2.5f) : (attackPower * 1.35f));
				target.TakeDamage(executeDamage, critical: true, source);
			}
			finally
			{
				resolvingHeroAugmentDamage = false;
			}
		}

		private int CountPetrifiedNonBossMonsters()
		{
			int count = 0;
			IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
			for (int i = 0; i < monsters.Count; i++)
			{
				MonsterUnit monster = monsters[i];
				if (monster != null && !monster.IsBoss && monster.IsPetrified)
				{
					count++;
				}
			}
			return count;
		}

		private void DamageNearestAdditionalMonsters(DefenderUnit source, MonsterUnit primaryTarget, int count, float damage)
		{
			List<MonsterUnit> candidates = new List<MonsterUnit>();
			IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
			for (int i = 0; i < monsters.Count; i++)
			{
				MonsterUnit monster = monsters[i];
				if (monster != null && monster != primaryTarget && monster.CanBeCombatTargeted)
				{
					candidates.Add(monster);
				}
			}
			Vector3 origin = ((primaryTarget != null) ? primaryTarget.transform.position : source.transform.position);
			candidates.Sort((MonsterUnit a, MonsterUnit b) => Vector3.SqrMagnitude(a.transform.position - origin).CompareTo(Vector3.SqrMagnitude(b.transform.position - origin)));
			int checkedCount = Mathf.Min(Mathf.Max(0, count), candidates.Count);
			for (int i2 = 0; i2 < checkedCount; i2++)
			{
				candidates[i2].TakeDamage(damage, critical: false, source);
			}
		}

		private void DamageRandomMonsters(DefenderUnit source, int shotCount, float damage)
		{
			if (source == null || shotCount <= 0 || damage <= 0f)
			{
				return;
			}
			List<MonsterUnit> candidates = new List<MonsterUnit>();
			IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
			for (int i = 0; i < monsters.Count; i++)
			{
				MonsterUnit monster = monsters[i];
				if (monster != null && monster.CanBeCombatTargeted)
				{
					candidates.Add(monster);
				}
			}
			for (int j = 0; j < shotCount; j++)
			{
				if (candidates.Count <= 0)
				{
					break;
				}
				MonsterUnit target = candidates[UnityEngine.Random.Range(0, candidates.Count)];
				target.TakeDamage(damage, critical: false, source);
			}
		}

		private void DamageFarthestAdditionalMonsters(DefenderUnit source, MonsterUnit primaryTarget, int count, float damage)
		{
			List<MonsterUnit> candidates = new List<MonsterUnit>();
			IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
			for (int i = 0; i < monsters.Count; i++)
			{
				MonsterUnit monster = monsters[i];
				if (monster != null && monster != primaryTarget && monster.CanBeCombatTargeted)
				{
					candidates.Add(monster);
				}
			}
			Vector3 origin = ((source != null) ? source.transform.position : ((primaryTarget != null) ? primaryTarget.transform.position : Vector3.zero));
			candidates.Sort((MonsterUnit a, MonsterUnit b) => Vector3.SqrMagnitude(b.transform.position - origin).CompareTo(Vector3.SqrMagnitude(a.transform.position - origin)));
			int checkedCount = Mathf.Min(Mathf.Max(0, count), candidates.Count);
			for (int i2 = 0; i2 < checkedCount; i2++)
			{
				candidates[i2].TakeDamage(damage, critical: false, source);
			}
		}

		private void PoisonNearestMonster(DefenderUnit source, Vector3 origin, float damagePerTick, float duration, float tickInterval)
		{
			MonsterUnit best = null;
			float bestDistance = float.MaxValue;
			IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
			for (int i = 0; i < monsters.Count; i++)
			{
				MonsterUnit monster = monsters[i];
				if (!(monster == null) && !monster.IsBoss && monster.CanBeCombatTargeted)
				{
					float distance = Vector3.SqrMagnitude(monster.transform.position - origin);
					if (distance < bestDistance)
					{
						bestDistance = distance;
						best = monster;
					}
				}
			}
			if (best != null)
			{
				best.ApplyPoison(damagePerTick, duration, tickInterval, source);
			}
		}

		private int ApplyHeroLineDamage(DefenderUnit source, MonsterUnit anchorTarget, float length, float halfWidth, float damage)
		{
			if (source == null || damage <= 0f)
			{
				return 0;
			}
			Vector3 direction = ((anchorTarget != null) ? (anchorTarget.transform.position - source.transform.position) : source.transform.forward);
			direction.y = 0f;
			if (direction.sqrMagnitude <= 0.0001f)
			{
				direction = source.transform.forward;
			}
			direction.Normalize();
			IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
			int hitCount = 0;
			for (int i = 0; i < monsters.Count; i++)
			{
				MonsterUnit monster = monsters[i];
				if (monster == null || !monster.CanBeCombatTargeted)
				{
					continue;
				}
				Vector3 offset = monster.transform.position - source.transform.position;
				offset.y = 0f;
				float forwardDistance = Vector3.Dot(offset, direction);
				if (!(forwardDistance < 0f) && !(forwardDistance > length))
				{
					Vector3 closestPoint = direction * forwardDistance;
					if ((offset - closestPoint).magnitude <= halfWidth)
					{
						monster.TakeDamage(damage, critical: false, source);
						hitCount++;
					}
				}
			}
			return hitCount;
		}

		private void SpawnHeroDamageZone(DefenderUnit source, SkillDefinition skill, Vector3 center, float radius, float damagePerTick, float duration, float tickInterval)
		{
			StartCoroutine(HeroDamageZoneRoutine(source, skill, center, radius, damagePerTick, duration, tickInterval));
		}

		private IEnumerator HeroDamageZoneRoutine(DefenderUnit source, SkillDefinition skill, Vector3 center, float radius, float damagePerTick, float duration, float tickInterval)
		{
			float elapsed = 0f;
			float interval = Mathf.Max(0.15f, tickInterval);
			float checkedRadius = Mathf.Max(0.2f, radius);
			for (; elapsed < duration; elapsed += interval)
			{
				IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
				for (int i = 0; i < monsters.Count; i++)
				{
					MonsterUnit monster = monsters[i];
					if (monster != null && Vector3.Distance(center, monster.transform.position) <= checkedRadius)
					{
						DefenderUnit.RunWithSkillDamageContext(skill, delegate
						{
							monster.TakeDamage(damagePerTick, critical: false, source);
						});
					}
				}
				yield return new WaitForSeconds(interval);
			}
		}

		private IEnumerator DelayedHeroAreaDamage(DefenderUnit source, Vector3 center, float delay, float radius, float damage)
		{
			yield return new WaitForSeconds(Mathf.Max(0f, delay));
			ApplyHeroAreaDamage(source, null, center, radius, damage);
		}

		private IEnumerator DelayedRandomHeroDamageZones(DefenderUnit source, SkillDefinition skill, float delay, int count, float radius, float damagePerTick, float duration, float tickInterval)
		{
			yield return new WaitForSeconds(Mathf.Max(0f, delay));
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
			StartCoroutine(ResolveHero54Retaliation(source, duration, returnRatio, radius, shieldShareRatio));
		}

		private IEnumerator ResolveHero54Retaliation(DefenderUnit source, float duration, float returnRatio, float radius, float shieldShareRatio)
		{
			yield return new WaitForSeconds(Mathf.Max(0.1f, duration));
			if (source == null)
			{
				yield break;
			}
			float value;
			float storedDamage = (hero54StoredDamage.TryGetValue(source, out value) ? value : 0f);
			hero54StoredDamage.Remove(source);
			if (!(storedDamage <= 0f))
			{
				ApplyHeroAreaDamage(source, null, source.transform.position, radius, storedDamage * returnRatio);
				if (shieldShareRatio > 0f)
				{
					ShieldNearbyDefenders(source.transform.position, radius, storedDamage * shieldShareRatio / Mathf.Max(1f, source.MaxHealth), 4f);
				}
			}
		}

		private void ShowRandomGoldFeedback(AugmentDefinition augment, int reward, int minGold, int maxGold, float triggerChance = 0f)
		{
			if (augment != null)
			{
				int safeMinGold = Mathf.Max(0, minGold);
				int safeMaxGold = Mathf.Max(safeMinGold, maxGold);
				int safeReward = Mathf.Clamp(reward, safeMinGold, safeMaxGold);
				string chanceLabel = ((triggerChance > 0f) ? (" (" + Mathf.RoundToInt(Mathf.Clamp01(triggerChance) * 100f) + "% 발동)") : string.Empty);
				string rewardLabel = ((safeReward > 0) ? ("+" + safeReward + "G") : "꽝! 0G");
				string rangeLabel = ((safeMaxGold > safeMinGold) ? (" / 최대 " + safeMaxGold + "G") : string.Empty);
				ShowEconomyBanner(augment.title + chanceLabel + "  " + rewardLabel + rangeLabel, augment.accentColor, 1.8f, 0f);
				if (IsGoldJackpot(safeReward, safeMinGold, safeMaxGold))
				{
					string detail = ((triggerChance > 0f) ? ("발동 확률 " + Mathf.RoundToInt(Mathf.Clamp01(triggerChance) * 100f) + "% / 최대 " + safeMaxGold + "G") : (augment.title + " / 최대 " + safeMaxGold + "G 중 상위 20%"));
					RuntimeAudioUtility.PlayJackpotMajor();
					RuntimeGameFeel.ShowJackpotReveal("골드 대박!", "JACKPOT", "+" + safeReward + "G", Color.Lerp(augment.accentColor, new Color(1f, 0.88f, 0.18f), 0.45f), detail, 2.6f);
				}
			}
		}

		private static bool IsGoldJackpot(int reward, int minGold, int maxGold)
		{
			if (reward < 8 || maxGold <= minGold)
			{
				return false;
			}
			int jackpotThreshold = Mathf.CeilToInt(Mathf.Lerp(minGold, maxGold, 0.8f));
			return reward >= jackpotThreshold;
		}

		private void ShowEconomyBanner(string message, Color color, float duration, float cooldown)
		{
			if (!(gameController == null) && (!(cooldown > 0f) || !(Time.time < nextEconomyBannerTime)))
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
			int firstRound = Mathf.Max(1, firstChoiceRound);
			int interval = ResolveFixedChoiceInterval();
			int baselineRound = Mathf.Max(0, completedRound);
			if (baselineRound < firstRound)
			{
				return firstRound;
			}
			int completedIntervals = Mathf.FloorToInt((float)(baselineRound - firstRound) / (float)interval) + 1;
			return firstRound + completedIntervals * interval;
		}

		private int ResolveFixedChoiceInterval()
		{
			int minInterval = Mathf.Max(1, minChoiceInterval);
			int maxInterval = Mathf.Max(minInterval, maxChoiceInterval);
			return Mathf.RoundToInt((float)(minInterval + maxInterval) * 0.5f);
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
			string[] retiredDuplicateIds = new string[16]
			{
				"sniper_window", "panic_barrier", "senior_miner", "union_mine", "steady_contract", "coupon_storm", "first_wave_bonus", "jackpot", "risky_vault", "bounty_flip",
				"mini_lotto", "rare_bounty", "boss_bet", "overclock_growth", "fever_engine", "boss_tax_account"
			};
			for (int i = 0; i < retiredDuplicateIds.Length; i++)
			{
				RemoveAugmentById(retiredDuplicateIds[i]);
			}
			AddTacticalGeneralAugments();
		}

		private void AddTacticalGeneralAugments()
		{
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
			string targetLabel = ResolveHeroAugmentTargetLabel(heroId);
			string readableTitle = (string.IsNullOrEmpty(targetLabel) ? title : ("[" + targetLabel + "] " + title));
			AugmentDefinition augment = CreateAugment(id, readableTitle, description, style, AugmentEffectType.HeroSignature, 0f, 0f, 0f, color);
			augment.requiredHeroId = heroId;
			augment.heroTier = tier;
			AddAugmentIfMissing(augment);
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
			for (int i = augmentPool.Count - 1; i >= 0; i--)
			{
				AugmentDefinition augment = augmentPool[i];
				if (augment != null && string.Equals(augment.id, id, StringComparison.Ordinal))
				{
					augmentPool.RemoveAt(i);
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
				AugmentDefinition augment = augmentPool[i];
				if (augment != null && string.Equals(augment.id, definition.id, StringComparison.Ordinal))
				{
					return;
				}
			}
			augmentPool.Add(definition);
		}

		private void AddStableAugments()
		{
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
}
