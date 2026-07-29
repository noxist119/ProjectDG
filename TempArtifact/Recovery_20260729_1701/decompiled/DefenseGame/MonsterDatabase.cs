using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenseGame;

public class MonsterDatabase : MonoBehaviour
{
	[Serializable]
	private class RoundGradeUnlockStep
	{
		public int firstRound = 1;

		public CharacterGrade maxGrade = CharacterGrade.Normal;
	}

	[Serializable]
	private class IndexGradeStep
	{
		public int firstIndex = 0;

		public CharacterGrade grade = CharacterGrade.Normal;
	}

	[Serializable]
	private class StarterGradeDistributionStep
	{
		[Range(0f, 1.01f)]
		public float upperRatioExclusive = 1f;

		public CharacterGrade grade = CharacterGrade.Normal;
	}

	[Serializable]
	private class MonsterStatBalance
	{
		[Header("Regular Base")]
		public float baseHealth = 70f;

		public float healthPerGrade = 34f;

		public float healthPerSeed = 2f;

		public float baseAttackPower = 8f;

		public float attackPowerPerGrade = 5.5f;

		public float baseCriticalChance = 0.05f;

		public float criticalChancePerGrade = 0.025f;

		public float baseCriticalDamageMultiplier = 1.5f;

		public float criticalDamageMultiplierPerGrade = 0.08f;

		public float baseAttackSpeed = 0.82f;

		public float attackSpeedPerGrade = 0.07f;

		public float baseMaxMana = 100f;

		public float maxManaPerGrade = 14f;

		public float baseAttackRange = 1.35f;

		public float attackRangePerGrade = 0.08f;

		public float alternatingAttackRangeBonus = 0.12f;

		public float baseMoveSpeed = 1.35f;

		public float moveSpeedPerGrade = 0.08f;

		public float projectileSpeed = 0f;

		public float manaRegenPerSecondRate = 0.05f;

		public float manaGainWhenHitRate = 0.1f;

		public float manaGainPerAttackRate = 0.15f;

		[Header("Role Modifiers")]
		public float chargerMoveSpeedMultiplier = 1.25f;

		public float chargerAttackSpeedMultiplier = 1.1f;

		public float chargerAttackRange = 1.2f;

		public float chargerManaGainPerAttackRate = 0.16f;

		public float bruteHealthMultiplier = 1.45f;

		public float bruteAttackMultiplier = 1.2f;

		public float bruteMoveSpeedMultiplier = 0.82f;

		public float bruteAttackRangeBonus = 0.15f;

		public float bruteManaGainWhenHitRate = 0.12f;

		public float casterMaxManaMultiplier = 1.3f;

		public float casterAttackRangeBonus = 2.4f;

		public float casterManaRegenPerSecondRate = 0.06f;

		public float casterManaGainPerAttackRate = 0.16f;

		public float eliteHealthMultiplier = 1.2f;

		public float eliteAttackMultiplier = 1.18f;

		public float eliteCriticalChanceBonus = 0.1f;

		public float eliteAttackRangeBonus = 0.45f;

		public float eliteManaGainPerAttackRate = 0.17f;

		[Header("Mid Boss")]
		public float midBossHealthMultiplier = 2.2f;

		public float midBossHealthMultiplierPerSeed = 0.08f;

		public float midBossAttackMultiplier = 1.12f;

		public float midBossAttackMultiplierPerSeed = 0.02f;

		public float midBossAttackSpeedMultiplier = 0.92f;

		public float midBossMoveSpeedMultiplier = 0.82f;

		public float midBossMaxMana = 105f;

		public float midBossManaRegenRate = 0.065f;

		public float midBossManaGainWhenHitRate = 0.13f;

		public float midBossManaGainPerAttackRate = 0.17f;

		public float midBossRangeBonus = 0.25f;

		public float midBossCasterRangeBonus = 0.7f;

		[Header("Boss Base")]
		public float bossBaseHealth = 940f;

		public float bossHealthPerSeed = 190f;

		public float bossBaseAttackPower = 20f;

		public float bossAttackPowerPerSeed = 3.8f;

		public float bossBaseCriticalChance = 0.11f;

		public float bossCriticalChancePerSeed = 0.014f;

		public float bossCriticalDamageMultiplier = 1.72f;

		public float bossBaseAttackSpeed = 0.88f;

		public float bossAttackSpeedPerSeed = 0.035f;

		public float bossMaxMana = 120f;

		public float bossManaRegenPerSecondRate = 0.06f;

		public float bossManaGainWhenHitRate = 0.12f;

		public float bossManaGainPerAttackRate = 0.18f;

		public float bossAttackRange = 2f;

		public float bossBaseMoveSpeed = 1.08f;

		public float bossMoveSpeedPerSeed = 0.035f;
	}

	[Serializable]
	private class BossSeedStatModifierStep
	{
		public int minSeed = 0;

		public int maxSeed = -1;

		public float healthMultiplier = 1f;

		public float attackMultiplier = 1f;

		public float attackSpeedMultiplier = 1f;

		public float moveSpeedMultiplier = 1f;

		public float attackRangeBonus = 0f;

		public float manaRegenBonus = 0f;

		public float maxManaOverride = 0f;

		public bool AppliesTo(int seed)
		{
			if (seed < minSeed)
			{
				return false;
			}
			return maxSeed < 0 || seed <= maxSeed;
		}
	}

	[Serializable]
	private class BossEncounterBalanceStep
	{
		[Tooltip("Boss encounter index. 0 is round 10, 1 is round 20, 2 is round 30.")]
		public int encounterIndex;

		[TextArea]
		public string designerNote;

		[TextArea]
		public string description;

		[Header("Core Scale")]
		[Range(0.1f, 6f)]
		public float healthScale = 1f;

		[Range(0.1f, 4f)]
		public float attackScale = 1f;

		[Range(0.1f, 3f)]
		public float manaScale = 1f;

		[Range(0.1f, 3f)]
		public float skillPowerScale = 1f;

		[Range(0.7f, 1.5f)]
		public float cooldownScale = 1f;

		public float manaThresholdOffset;

		[Header("After Last Row Growth")]
		public float extraHealthScalePerEncounter = 0.22f;

		public float extraAttackScalePerEncounter = 0.09f;

		public float extraManaScalePerEncounter = 0.05f;

		public float extraSkillPowerScalePerEncounter = 0.07f;

		public float extraCooldownScalePerEncounter = -0.025f;

		public float extraManaThresholdOffsetPerEncounter = -1.5f;

		[Header("Early Death Pact Softening")]
		public bool convertDeathPactToDamage = true;

		public string convertedDeathPactName = "Reaper Pressure";

		[TextArea]
		public string convertedDeathPactDescription = "Early boss version: heavy single-target damage instead of instant death.";

		public float convertedDeathPactPower = 1.5f;

		public float extraConvertedDeathPactPowerPerEncounter = 0.18f;

		public float convertedDeathPactCooldown = 12f;

		public float convertedDeathPactManaThreshold = 92f;

		[Header("Skill Caps")]
		public int maxMassStunTargets = 1;

		public int maxSummonRushTargets = 2;

		public int maxManaBurnTargets = 1;

		public float maxStunDuration = 1.05f;

		public float maxAreaDamagePower = 0.92f;

		public float maxAreaRadius = 3.2f;

		public float maxSummonRushPower = 0.7f;

		public float maxManaBurnRatio = 0.22f;

		public float maxGoldDrain = 6f;

		public float maxBossFortifyRatio = 0.075f;

		public float maxBossFortifyDuration = 3.2f;

		public float maxRallyPower = 0.13f;

		public float maxRallyDuration = 3.4f;

		[Header("After Last Row Skill Growth")]
		public int extraTargetsPerEncounter = 0;

		public float extraSkillCapPerEncounter = 0.08f;

		public float extraDurationCapPerEncounter = 0.15f;

		public float extraRadiusCapPerEncounter = 0.12f;

		[Header("Minimum Cooldowns")]
		public float minimumControlCooldown = 9.5f;

		public float minimumSingleStunCooldown = 8f;

		public float minimumManaBurnCooldown = 9.4f;

		public float minimumGoldDrainCooldown = 10.5f;
	}

	[SerializeField]
	private List<MonsterDefinition> monsters = new List<MonsterDefinition>();

	[SerializeField]
	private List<MonsterDefinition> midBosses = new List<MonsterDefinition>();

	[SerializeField]
	private List<MonsterDefinition> bosses = new List<MonsterDefinition>();

	[SerializeField]
	private bool generateStarterMonsters = true;

	[SerializeField]
	private int starterMonsterCount = 30;

	[SerializeField]
	private GamePresentationConfig presentationConfig;

	[SerializeField]
	private MonsterCombatTuningConfig combatTuningConfig;

	[Header("Regular Swarm Balance")]
	[SerializeField]
	[Range(0.45f, 1f)]
	private float regularHealthMultiplier = 0.6f;

	[SerializeField]
	[Range(0.55f, 1f)]
	private float regularAttackMultiplier = 0.82f;

	[SerializeField]
	[Range(0.25f, 1f)]
	private float regularRewardMultiplier = 0.62f;

	[Header("Monster Grade Unlock Tables")]
	[SerializeField]
	private List<RoundGradeUnlockStep> regularGradeUnlockSteps = new List<RoundGradeUnlockStep>
	{
		new RoundGradeUnlockStep
		{
			firstRound = 1,
			maxGrade = CharacterGrade.Normal
		},
		new RoundGradeUnlockStep
		{
			firstRound = 4,
			maxGrade = CharacterGrade.Rare
		},
		new RoundGradeUnlockStep
		{
			firstRound = 7,
			maxGrade = CharacterGrade.Epic
		},
		new RoundGradeUnlockStep
		{
			firstRound = 10,
			maxGrade = CharacterGrade.Legendary
		},
		new RoundGradeUnlockStep
		{
			firstRound = 15,
			maxGrade = CharacterGrade.Mythic
		}
	};

	[SerializeField]
	private List<RoundGradeUnlockStep> midBossGradeUnlockSteps = new List<RoundGradeUnlockStep>
	{
		new RoundGradeUnlockStep
		{
			firstRound = 1,
			maxGrade = CharacterGrade.Rare
		},
		new RoundGradeUnlockStep
		{
			firstRound = 6,
			maxGrade = CharacterGrade.Epic
		},
		new RoundGradeUnlockStep
		{
			firstRound = 11,
			maxGrade = CharacterGrade.Legendary
		},
		new RoundGradeUnlockStep
		{
			firstRound = 19,
			maxGrade = CharacterGrade.Mythic
		}
	};

	[SerializeField]
	private List<IndexGradeStep> midBossRosterGradeSteps = new List<IndexGradeStep>
	{
		new IndexGradeStep
		{
			firstIndex = 0,
			grade = CharacterGrade.Rare
		},
		new IndexGradeStep
		{
			firstIndex = 2,
			grade = CharacterGrade.Epic
		},
		new IndexGradeStep
		{
			firstIndex = 5,
			grade = CharacterGrade.Legendary
		},
		new IndexGradeStep
		{
			firstIndex = 7,
			grade = CharacterGrade.Mythic
		}
	};

	[SerializeField]
	private List<StarterGradeDistributionStep> starterGradeDistributionSteps = new List<StarterGradeDistributionStep>
	{
		new StarterGradeDistributionStep
		{
			upperRatioExclusive = 0.35f,
			grade = CharacterGrade.Normal
		},
		new StarterGradeDistributionStep
		{
			upperRatioExclusive = 0.65f,
			grade = CharacterGrade.Rare
		},
		new StarterGradeDistributionStep
		{
			upperRatioExclusive = 0.85f,
			grade = CharacterGrade.Epic
		},
		new StarterGradeDistributionStep
		{
			upperRatioExclusive = 0.96f,
			grade = CharacterGrade.Legendary
		},
		new StarterGradeDistributionStep
		{
			upperRatioExclusive = 1.01f,
			grade = CharacterGrade.Mythic
		}
	};

	[Header("Monster Stat Balance")]
	[SerializeField]
	private MonsterStatBalance monsterStatBalance = new MonsterStatBalance();

	[SerializeField]
	private List<BossSeedStatModifierStep> bossSeedStatModifierSteps = new List<BossSeedStatModifierStep>
	{
		new BossSeedStatModifierStep
		{
			minSeed = 1,
			maxSeed = 1,
			healthMultiplier = 1.12f,
			attackMultiplier = 1.03f,
			attackSpeedMultiplier = 0.9f,
			moveSpeedMultiplier = 0.78f
		},
		new BossSeedStatModifierStep
		{
			minSeed = 2,
			maxSeed = 2,
			healthMultiplier = 1.1f,
			moveSpeedMultiplier = 0.86f,
			attackRangeBonus = 0.35f,
			manaRegenBonus = 0.01f
		},
		new BossSeedStatModifierStep
		{
			minSeed = 3,
			maxSeed = 3,
			healthMultiplier = 0.92f,
			attackSpeedMultiplier = 1.1f,
			moveSpeedMultiplier = 1.12f
		},
		new BossSeedStatModifierStep
		{
			minSeed = 4,
			maxSeed = -1,
			healthMultiplier = 1.16f,
			attackMultiplier = 1.08f,
			maxManaOverride = 100f,
			manaRegenBonus = 0.015f
		}
	};

	[Header("Boss Progression Balance")]
	[SerializeField]
	private List<BossEncounterBalanceStep> bossEncounterBalanceSteps = new List<BossEncounterBalanceStep>
	{
		new BossEncounterBalanceStep
		{
			encounterIndex = 0,
			designerNote = "R10 / human 3-strategy target clear rate 65-75% / 2026-07 20-run baseline 90% decisive pressure retune",
			description = "Boss 1: requires a real summon-versus-shop commitment and punishes underbuilt boards.",
			healthScale = 5f,
			attackScale = 3f,
			manaScale = 1.05f,
			skillPowerScale = 1.85f,
			cooldownScale = 0.85f,
			manaThresholdOffset = -10f,
			convertedDeathPactPower = 1.5f,
			maxMassStunTargets = 2,
			maxStunDuration = 1.25f,
			maxAreaDamagePower = 1.6f,
			maxAreaRadius = 3.2f,
			maxManaBurnTargets = 1,
			maxManaBurnRatio = 0.22f,
			maxGoldDrain = 6f,
			maxBossFortifyRatio = 0.075f,
			maxBossFortifyDuration = 3.2f,
			maxRallyPower = 0.13f,
			maxRallyDuration = 3.4f
		},
		new BossEncounterBalanceStep
		{
			encounterIndex = 1,
			designerNote = "Day 1 / 30-90m / Epic x1 + placement",
			description = "Boss 2: one Epic unit plus clean placement should remain enough.",
			healthScale = 1.2f,
			attackScale = 0.84f,
			manaScale = 0.9f,
			skillPowerScale = 0.82f,
			cooldownScale = 1.18f,
			manaThresholdOffset = 6f,
			convertedDeathPactPower = 1.68f,
			maxMassStunTargets = 1,
			maxStunDuration = 1.21f,
			maxAreaDamagePower = 1.07f,
			maxAreaRadius = 3.36f,
			maxManaBurnTargets = 1,
			maxManaBurnRatio = 0.27f,
			maxGoldDrain = 8.2f,
			maxBossFortifyRatio = 0.093f,
			maxBossFortifyDuration = 3.55f,
			maxRallyPower = 0.155f,
			maxRallyDuration = 3.78f
		},
		new BossEncounterBalanceStep
		{
			encounterIndex = 2,
			designerNote = "Day 1 / 90-180m / Epic x2 or strong Rare synergy",
			description = "Boss 3: expects two Epic units or a strong Rare synergy line.",
			healthScale = 1.38f,
			attackScale = 0.96f,
			manaScale = 0.98f,
			skillPowerScale = 0.94f,
			cooldownScale = 1.1f,
			manaThresholdOffset = 3f,
			convertedDeathPactPower = 1.86f,
			maxMassStunTargets = 2,
			maxStunDuration = 1.37f,
			maxAreaDamagePower = 1.22f,
			maxAreaRadius = 3.52f,
			maxManaBurnTargets = 2,
			maxManaBurnRatio = 0.32f,
			maxGoldDrain = 10.4f,
			maxBossFortifyRatio = 0.111f,
			maxBossFortifyDuration = 3.9f,
			maxRallyPower = 0.18f,
			maxRallyDuration = 4.16f
		},
		new BossEncounterBalanceStep
		{
			encounterIndex = 3,
			designerNote = "Day 2 start / Legendary prep begins",
			description = "Boss 4: Legendary preparation starts to matter, but strong growth can still pass.",
			healthScale = 1.34f,
			attackScale = 1.08f,
			manaScale = 1.06f,
			skillPowerScale = 1.05f,
			cooldownScale = 1.04f,
			manaThresholdOffset = 0f,
			convertedDeathPactPower = 2.04f,
			maxMassStunTargets = 3,
			maxStunDuration = 1.53f,
			maxAreaDamagePower = 1.37f,
			maxAreaRadius = 3.68f,
			maxManaBurnTargets = 3,
			maxManaBurnRatio = 0.37f,
			maxGoldDrain = 12.6f,
			maxBossFortifyRatio = 0.129f,
			maxBossFortifyDuration = 4.25f,
			maxRallyPower = 0.205f,
			maxRallyDuration = 4.54f
		},
		new BossEncounterBalanceStep
		{
			encounterIndex = 4,
			designerNote = "Day 2 / Legendary x1 stable clear target",
			description = "Boss 5: one Legendary unit becomes the stable clear benchmark.",
			healthScale = 1.58f,
			attackScale = 1.22f,
			manaScale = 1.14f,
			skillPowerScale = 1.16f,
			cooldownScale = 1f,
			manaThresholdOffset = -1.5f,
			convertDeathPactToDamage = false,
			maxMassStunTargets = 3,
			maxStunDuration = 1.65f,
			maxAreaDamagePower = 1.52f,
			maxAreaRadius = 3.84f,
			maxManaBurnTargets = 3,
			maxManaBurnRatio = 0.42f,
			maxGoldDrain = 14.8f,
			maxBossFortifyRatio = 0.147f,
			maxBossFortifyDuration = 4.6f,
			maxRallyPower = 0.23f,
			maxRallyDuration = 4.92f
		},
		new BossEncounterBalanceStep
		{
			encounterIndex = 5,
			designerNote = "Day 2 / Legendary x1 + synergy",
			description = "Boss 6: expects a Legendary anchor or several grown Epic units.",
			healthScale = 1.82f,
			attackScale = 1.36f,
			manaScale = 1.19f,
			skillPowerScale = 1.23f,
			cooldownScale = 0.975f,
			manaThresholdOffset = -3f,
			convertDeathPactToDamage = false,
			maxMassStunTargets = 3,
			maxStunDuration = 1.8f,
			maxAreaDamagePower = 1.66f,
			maxAreaRadius = 4f,
			maxManaBurnTargets = 3,
			maxManaBurnRatio = 0.47f,
			maxGoldDrain = 17f,
			maxBossFortifyRatio = 0.165f,
			maxBossFortifyDuration = 4.95f,
			maxRallyPower = 0.255f,
			maxRallyDuration = 5.3f
		},
		new BossEncounterBalanceStep
		{
			encounterIndex = 6,
			designerNote = "Day 2 / strong Legendary or wide growth",
			description = "Boss 7: demands visible outgame growth, synergy, and placement response.",
			healthScale = 2.08f,
			attackScale = 1.5f,
			manaScale = 1.24f,
			skillPowerScale = 1.3f,
			cooldownScale = 0.95f,
			manaThresholdOffset = -4.5f,
			convertDeathPactToDamage = false,
			maxMassStunTargets = 3,
			maxStunDuration = 1.95f,
			maxAreaDamagePower = 1.8f,
			maxAreaRadius = 4.16f,
			maxManaBurnTargets = 3,
			maxManaBurnRatio = 0.52f,
			maxGoldDrain = 19.2f,
			maxBossFortifyRatio = 0.183f,
			maxBossFortifyDuration = 5.3f,
			maxRallyPower = 0.28f,
			maxRallyDuration = 5.68f
		},
		new BossEncounterBalanceStep
		{
			encounterIndex = 7,
			designerNote = "Day 2+ / long progression ramp",
			description = "Boss 8+: long progression target for deck growth, placement, and boss-skill response.",
			healthScale = 2.36f,
			attackScale = 1.66f,
			manaScale = 1.29f,
			skillPowerScale = 1.37f,
			cooldownScale = 0.925f,
			manaThresholdOffset = -6f,
			convertDeathPactToDamage = false,
			maxMassStunTargets = 3,
			maxStunDuration = 2.1f,
			maxAreaDamagePower = 1.94f,
			maxAreaRadius = 4.32f,
			maxManaBurnTargets = 3,
			maxManaBurnRatio = 0.57f,
			maxGoldDrain = 21.4f,
			maxBossFortifyRatio = 0.201f,
			maxBossFortifyDuration = 5.65f,
			maxRallyPower = 0.305f,
			maxRallyDuration = 6.06f
		}
	};

	private static readonly string[] MonsterNames = new string[30]
	{
		"Rot Fang", "Cave Skitter", "Mud Lurker", "Bone Pup", "Ash Beetle", "Howl Rat", "Night Creeper", "Ruin Stalker", "Gloom Toad", "Red Gnaw",
		"Iron Shell", "Fog Brute", "Warp Caster", "Mire Charger", "Hex Lizard", "Stone Mauler", "Acid Husk", "Void Wolf", "Rage Minotaur", "Feral Shaman",
		"Blight Colossus", "Storm Ravager", "Crimson Giant", "Grave Herald", "Obsidian Breaker", "Starved Dragon", "Specter Lord", "World Eater", "Black Tempest", "Chaos Devourer"
	};

	private static readonly string[] MidBossNames = new string[8] { "Iron Jailor", "Glass Prophet", "Moss Colossus", "Crimson Butcher", "Void Taxer", "Storm Matron", "Ash Cannon", "Frost Warden" };

	private static readonly string[] BossNames = new string[8] { "Gatebreaker Rhogar", "Queen Morva", "Leviathan Kron", "Throne of Ash", "Myth Seraph Null", "Clockwork Tyrant", "Abyssal Collector", "Solar Executioner" };

	public IReadOnlyList<MonsterDefinition> Monsters => monsters;

	public IReadOnlyList<MonsterDefinition> MidBosses => midBosses;

	public IReadOnlyList<MonsterDefinition> Bosses => bosses;

	private void Awake()
	{
		if (generateStarterMonsters && monsters.Count == 0 && midBosses.Count == 0 && bosses.Count == 0)
		{
			GenerateStarterMonsters(starterMonsterCount);
			return;
		}
		ApplyPresentationRoster();
		ApplyDefinitionOverrides();
	}

	public void ApplyPresentationConfig(GamePresentationConfig config)
	{
		presentationConfig = config;
		ApplyPresentationRoster();
		ApplyDefinitionOverrides();
	}

	public void ApplyCombatTuningConfig(MonsterCombatTuningConfig config)
	{
		combatTuningConfig = config;
		ApplyDefinitionOverrides();
	}

	public void GenerateStarterMonsters(int totalCount)
	{
		monsters.Clear();
		midBosses.Clear();
		bosses.Clear();
		int num = Mathf.Max(10, totalCount);
		for (int i = 0; i < num; i++)
		{
			CharacterGrade grade = ResolveGrade(i, num);
			string name = ((i < MonsterNames.Length) ? MonsterNames[i] : $"Monster {i + 1:D2}");
			monsters.Add(CreateMonster(name, grade, i));
		}
		for (int j = 0; j < MidBossNames.Length; j++)
		{
			midBosses.Add(CreateMidBoss(MidBossNames[j], j));
		}
		for (int k = 0; k < BossNames.Length; k++)
		{
			bosses.Add(CreateBoss(BossNames[k], k));
		}
		ApplyPresentationRoster();
		ApplyDefinitionOverrides();
	}

	public MonsterDefinition GetRandomMonsterForRound(int round)
	{
		List<MonsterDefinition> list = monsters.Where((MonsterDefinition monster) => monster != null && monster.threatLevel == MonsterThreatLevel.Regular && monster.minRound <= round && monster.grade <= ResolveMaxRegularGrade(round)).ToList();
		if (list.Count == 0)
		{
			list = monsters.Where((MonsterDefinition monster) => monster != null && monster.threatLevel == MonsterThreatLevel.Regular && monster.minRound <= round).ToList();
		}
		if (list.Count == 0)
		{
			list = monsters.Where((MonsterDefinition monster) => monster != null && monster.threatLevel == MonsterThreatLevel.Regular).ToList();
		}
		return (list.Count > 0) ? list[Random.Range(0, list.Count)] : null;
	}

	public MonsterDefinition GetMidBossForRound(int round)
	{
		List<MonsterDefinition> list = midBosses.Where((MonsterDefinition monster) => monster != null && monster.minRound <= round && monster.grade <= ResolveMaxMidBossGrade(round)).ToList();
		if (list.Count == 0)
		{
			list = midBosses.Where((MonsterDefinition monster) => monster != null && monster.minRound <= round).ToList();
		}
		if (list.Count == 0)
		{
			list = midBosses.Where((MonsterDefinition monster) => monster != null).ToList();
		}
		if (list.Count == 0)
		{
			return null;
		}
		int index = Mathf.Abs(round + Random.Range(0, list.Count)) % list.Count;
		return list[index];
	}

	public bool HasMidBossForRound(int round)
	{
		return midBosses.Any((MonsterDefinition monster) => monster != null && monster.minRound <= round);
	}

	public MonsterDefinition GetBossForRound(int round)
	{
		if (bosses.Count == 0)
		{
			return null;
		}
		List<IGrouping<string, MonsterDefinition>> list = (from monster in bosses
			where monster != null
			group monster by string.IsNullOrWhiteSpace(monster.rosterSourceId) ? monster.id : monster.rosterSourceId into @group
			orderby @group.Min((MonsterDefinition monster) => monster.rosterIndex)
			select @group).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		int num = Mathf.Max(0, round / 10 - 1);
		int index = num % list.Count;
		int desiredVariantIndex = num / list.Count;
		List<MonsterDefinition> list2 = (from monster in list[index]
			where monster.minRound <= round
			orderby monster.variantIndex
			select monster).ToList();
		if (list2.Count == 0)
		{
			list2 = list[index].OrderBy((MonsterDefinition monster) => monster.variantIndex).ToList();
		}
		MonsterDefinition monsterDefinition = (from monster in list2
			where monster.variantIndex <= desiredVariantIndex
			orderby monster.variantIndex descending
			select monster).FirstOrDefault();
		monsterDefinition = ((monsterDefinition != null) ? monsterDefinition : list2[0]);
		return CreateBossEncounterDefinition(monsterDefinition, round, num);
	}

	private MonsterDefinition CreateBossEncounterDefinition(MonsterDefinition source, int round, int encounterIndex)
	{
		MonsterDefinition monsterDefinition = CloneMonsterDefinition(source);
		int num = Mathf.Max(0, encounterIndex);
		monsterDefinition.displayName = source.displayName + ResolveBossEncounterSuffix(num);
		monsterDefinition.description = BuildBossEncounterDescription(num);
		monsterDefinition.minRound = round;
		monsterDefinition.rewardGold = Mathf.RoundToInt((float)source.rewardGold * (1f + (float)num * 0.14f));
		float num2 = ResolveBossEncounterHealthScale(num);
		float num3 = ResolveBossEncounterAttackScale(num);
		float num4 = ResolveBossEncounterManaScale(num);
		monsterDefinition.stats.maxHealth *= num2;
		monsterDefinition.stats.attackPower *= num3;
		monsterDefinition.stats.maxMana *= num4;
		monsterDefinition.stats.manaRegenPerSecondRate *= Mathf.Lerp(0.88f, 1.24f, Mathf.Clamp01((float)num / 8f));
		monsterDefinition.stats.manaGainWhenHitRate *= Mathf.Lerp(0.82f, 1.18f, Mathf.Clamp01((float)num / 8f));
		monsterDefinition.stats.manaGainPerAttackRate *= Mathf.Lerp(0.86f, 1.16f, Mathf.Clamp01((float)num / 8f));
		monsterDefinition.visualScale = Mathf.Clamp(source.visualScale + (float)num * 0.012f, 1.58f, 2.05f);
		if (monsterDefinition.skills != null)
		{
			for (int i = 0; i < monsterDefinition.skills.Count; i++)
			{
				TuneBossSkillForEncounter(monsterDefinition.skills[i], num);
			}
		}
		return monsterDefinition;
	}

	private string ResolveBossEncounterSuffix(int encounterIndex)
	{
		int num = encounterIndex / Mathf.Max(1, BossNames.Length);
		if (num <= 0)
		{
			return string.Empty;
		}
		return " " + ToRoman(num + 1);
	}

	private string BuildBossEncounterDescription(int encounterIndex)
	{
		int overflow;
		BossEncounterBalanceStep bossEncounterBalanceStep = ResolveBossEncounterBalance(encounterIndex, out overflow);
		if (bossEncounterBalanceStep != null && !string.IsNullOrWhiteSpace(bossEncounterBalanceStep.description))
		{
			return bossEncounterBalanceStep.description;
		}
		if (encounterIndex <= 0)
		{
			return "첫 보스 관문입니다. 희귀 유닛 1개가 있으면 클리어 흐름이 보이도록 조정되어 있습니다.";
		}
		if (encounterIndex == 1)
		{
			return "두 번째 보스 관문입니다. 희귀 유닛 1개와 안정적인 배치가 있으면 넘길 수 있습니다.";
		}
		if (encounterIndex == 2)
		{
			return "세 번째 보스 관문입니다. 희귀 유닛 2개 이상 또는 강한 레어 시너지가 필요합니다.";
		}
		if (encounterIndex <= 4)
		{
			return "중반 보스 관문입니다. 전설 준비와 배치 대응이 점점 중요해집니다.";
		}
		return "장기 진행 보스입니다. 덱 성장, 배치, 보스 스킬 대응이 모두 필요합니다.";
	}

	private float ResolveBossEncounterHealthScale(int encounterIndex)
	{
		int overflow;
		BossEncounterBalanceStep bossEncounterBalanceStep = ResolveBossEncounterBalance(encounterIndex, out overflow);
		if (bossEncounterBalanceStep != null)
		{
			return Mathf.Max(0.1f, bossEncounterBalanceStep.healthScale + (float)overflow * bossEncounterBalanceStep.extraHealthScalePerEncounter);
		}
		if (encounterIndex <= 0)
		{
			return 1.5f;
		}
		return encounterIndex switch
		{
			1 => 1.2f, 
			2 => 1.38f, 
			3 => 1.34f, 
			4 => 1.58f, 
			5 => 1.82f, 
			6 => 2.08f, 
			7 => 2.36f, 
			_ => 2.36f + (float)(encounterIndex - 7) * 0.22f, 
		};
	}

	private float ResolveBossEncounterAttackScale(int encounterIndex)
	{
		int overflow;
		BossEncounterBalanceStep bossEncounterBalanceStep = ResolveBossEncounterBalance(encounterIndex, out overflow);
		if (bossEncounterBalanceStep != null)
		{
			return Mathf.Max(0.1f, bossEncounterBalanceStep.attackScale + (float)overflow * bossEncounterBalanceStep.extraAttackScalePerEncounter);
		}
		if (encounterIndex <= 0)
		{
			return 1.32f;
		}
		return encounterIndex switch
		{
			1 => 0.84f, 
			2 => 0.96f, 
			3 => 1.08f, 
			4 => 1.22f, 
			5 => 1.36f, 
			6 => 1.5f, 
			7 => 1.66f, 
			_ => 1.66f + (float)(encounterIndex - 7) * 0.09f, 
		};
	}

	private float ResolveBossEncounterManaScale(int encounterIndex)
	{
		int overflow;
		BossEncounterBalanceStep bossEncounterBalanceStep = ResolveBossEncounterBalance(encounterIndex, out overflow);
		if (bossEncounterBalanceStep != null)
		{
			return Mathf.Max(0.1f, bossEncounterBalanceStep.manaScale + (float)overflow * bossEncounterBalanceStep.extraManaScalePerEncounter);
		}
		if (encounterIndex <= 0)
		{
			return 1.05f;
		}
		return encounterIndex switch
		{
			1 => 0.9f, 
			2 => 0.98f, 
			3 => 1.06f, 
			4 => 1.14f, 
			_ => 1.14f + Mathf.Min(0.38f, (float)(encounterIndex - 4) * 0.05f), 
		};
	}

	private float ResolveBossEncounterSkillPowerScale(int encounterIndex)
	{
		int overflow;
		BossEncounterBalanceStep bossEncounterBalanceStep = ResolveBossEncounterBalance(encounterIndex, out overflow);
		if (bossEncounterBalanceStep != null)
		{
			return Mathf.Max(0.1f, bossEncounterBalanceStep.skillPowerScale + (float)overflow * bossEncounterBalanceStep.extraSkillPowerScalePerEncounter);
		}
		if (encounterIndex <= 0)
		{
			return 1.12f;
		}
		return encounterIndex switch
		{
			1 => 0.82f, 
			2 => 0.94f, 
			3 => 1.05f, 
			4 => 1.16f, 
			_ => 1.16f + Mathf.Min(0.45f, (float)(encounterIndex - 4) * 0.07f), 
		};
	}

	private float ResolveBossSkillCooldownScale(int encounterIndex)
	{
		int overflow;
		BossEncounterBalanceStep bossEncounterBalanceStep = ResolveBossEncounterBalance(encounterIndex, out overflow);
		if (bossEncounterBalanceStep != null)
		{
			return Mathf.Clamp(bossEncounterBalanceStep.cooldownScale + (float)overflow * bossEncounterBalanceStep.extraCooldownScalePerEncounter, 0.7f, 1.45f);
		}
		if (encounterIndex <= 0)
		{
			return 1f;
		}
		return encounterIndex switch
		{
			1 => 1.18f, 
			2 => 1.1f, 
			3 => 1.04f, 
			4 => 1f, 
			_ => Mathf.Max(0.84f, 1f - (float)(encounterIndex - 4) * 0.025f), 
		};
	}

	private float ResolveBossSkillManaThresholdOffset(int encounterIndex)
	{
		int overflow;
		BossEncounterBalanceStep bossEncounterBalanceStep = ResolveBossEncounterBalance(encounterIndex, out overflow);
		if (bossEncounterBalanceStep != null)
		{
			return Mathf.Clamp(bossEncounterBalanceStep.manaThresholdOffset + (float)overflow * bossEncounterBalanceStep.extraManaThresholdOffsetPerEncounter, -20f, 25f);
		}
		if (encounterIndex <= 0)
		{
			return 0f;
		}
		return encounterIndex switch
		{
			1 => 6f, 
			2 => 3f, 
			3 => 0f, 
			_ => Mathf.Max(-8f, (float)(-(encounterIndex - 3)) * 1.5f), 
		};
	}

	private BossEncounterBalanceStep ResolveBossEncounterBalance(int encounterIndex, out int overflow)
	{
		overflow = 0;
		if (bossEncounterBalanceSteps == null || bossEncounterBalanceSteps.Count == 0)
		{
			return null;
		}
		int num = Mathf.Max(0, encounterIndex);
		BossEncounterBalanceStep bossEncounterBalanceStep = null;
		int num2 = int.MinValue;
		for (int i = 0; i < bossEncounterBalanceSteps.Count; i++)
		{
			BossEncounterBalanceStep bossEncounterBalanceStep2 = bossEncounterBalanceSteps[i];
			if (bossEncounterBalanceStep2 != null && bossEncounterBalanceStep2.encounterIndex <= num && bossEncounterBalanceStep2.encounterIndex >= num2)
			{
				bossEncounterBalanceStep = bossEncounterBalanceStep2;
				num2 = bossEncounterBalanceStep2.encounterIndex;
			}
		}
		if (bossEncounterBalanceStep == null)
		{
			return null;
		}
		overflow = Mathf.Max(0, num - bossEncounterBalanceStep.encounterIndex);
		return bossEncounterBalanceStep;
	}

	private void TuneBossSkillForEncounter(SkillDefinition skill, int encounterIndex)
	{
		if (skill == null)
		{
			return;
		}
		int num = Mathf.Max(0, encounterIndex);
		int overflow;
		BossEncounterBalanceStep bossEncounterBalanceStep = ResolveBossEncounterBalance(num, out overflow);
		float num2 = ResolveBossEncounterSkillPowerScale(num);
		skill.power *= num2;
		skill.cooldown = Mathf.Max(4.5f, skill.cooldown * ResolveBossSkillCooldownScale(num));
		skill.manaThreshold = Mathf.Clamp(skill.manaThreshold + ResolveBossSkillManaThresholdOffset(num), 70f, 115f);
		switch (skill.effectType)
		{
		case SkillEffectType.DeathPact:
			if (bossEncounterBalanceStep != null && bossEncounterBalanceStep.convertDeathPactToDamage)
			{
				skill.effectType = SkillEffectType.DirectDamage;
				skill.displayName = ((num <= 1) ? "사신의 압박" : "불완전 처형");
				skill.description = "초반 보스용으로 조정된 강한 단일 피해입니다. 즉사 대신 큰 피해를 줍니다.";
				skill.power = 1.5f + (float)num * 0.18f;
				skill.duration = 0f;
				skill.hitCount = 1;
				skill.displayName = bossEncounterBalanceStep.convertedDeathPactName;
				skill.description = bossEncounterBalanceStep.convertedDeathPactDescription;
				skill.power = bossEncounterBalanceStep.convertedDeathPactPower + (float)overflow * bossEncounterBalanceStep.extraConvertedDeathPactPowerPerEncounter;
				skill.cooldown = Mathf.Max(skill.cooldown, bossEncounterBalanceStep.convertedDeathPactCooldown);
				skill.manaThreshold = Mathf.Max(skill.manaThreshold, bossEncounterBalanceStep.convertedDeathPactManaThreshold);
			}
			else
			{
				skill.cooldown = Mathf.Max(skill.cooldown, 12f);
			}
			break;
		case SkillEffectType.MassStun:
			skill.hitCount = Mathf.Min(skill.hitCount, ResolveBossSkillTargetCap(bossEncounterBalanceStep, overflow, bossEncounterBalanceStep?.maxMassStunTargets ?? ((num <= 1) ? 1 : ((num == 2) ? 2 : 3))));
			skill.duration = Mathf.Min(skill.duration, ResolveBossSkillDurationCap(bossEncounterBalanceStep, overflow, bossEncounterBalanceStep?.maxStunDuration ?? (1.05f + (float)num * 0.16f)));
			skill.cooldown = Mathf.Max(skill.cooldown, bossEncounterBalanceStep?.minimumControlCooldown ?? (9.5f - Mathf.Min(1.2f, (float)num * 0.25f)));
			break;
		case SkillEffectType.Stun:
			skill.duration = Mathf.Min(skill.duration, ResolveBossSkillDurationCap(bossEncounterBalanceStep, overflow, bossEncounterBalanceStep?.maxStunDuration ?? (1.25f + (float)num * 0.18f)));
			skill.cooldown = Mathf.Max(skill.cooldown, bossEncounterBalanceStep?.minimumSingleStunCooldown ?? (8f - Mathf.Min(1.2f, (float)num * 0.2f)));
			break;
		case SkillEffectType.AreaDamage:
			skill.radius = Mathf.Min(skill.radius, ResolveBossSkillRadiusCap(bossEncounterBalanceStep, overflow, bossEncounterBalanceStep?.maxAreaRadius ?? (3.2f + (float)num * 0.16f)));
			skill.power = Mathf.Min(skill.power, ResolveBossSkillPowerCap(bossEncounterBalanceStep, overflow, bossEncounterBalanceStep?.maxAreaDamagePower ?? (0.92f + (float)num * 0.15f)));
			break;
		case SkillEffectType.SummonRush:
			skill.hitCount = Mathf.Min(skill.hitCount, ResolveBossSkillTargetCap(bossEncounterBalanceStep, overflow, bossEncounterBalanceStep?.maxSummonRushTargets ?? ((num <= 1) ? 2 : 3)));
			skill.power = Mathf.Min(skill.power, ResolveBossSkillPowerCap(bossEncounterBalanceStep, overflow, bossEncounterBalanceStep?.maxSummonRushPower ?? (0.7f + (float)num * 0.12f)));
			break;
		case SkillEffectType.ManaBurn:
			skill.hitCount = Mathf.Min(skill.hitCount, ResolveBossSkillTargetCap(bossEncounterBalanceStep, overflow, bossEncounterBalanceStep?.maxManaBurnTargets ?? ((num <= 1) ? 1 : ((num == 2) ? 2 : 3))));
			skill.power = Mathf.Min(skill.power, ResolveBossSkillPowerCap(bossEncounterBalanceStep, overflow, bossEncounterBalanceStep?.maxManaBurnRatio ?? (0.22f + (float)num * 0.05f)));
			skill.cooldown = Mathf.Max(skill.cooldown, bossEncounterBalanceStep?.minimumManaBurnCooldown ?? 9.4f);
			break;
		case SkillEffectType.GoldDrain:
			skill.power = Mathf.Min(skill.power, ResolveBossSkillPowerCap(bossEncounterBalanceStep, overflow, bossEncounterBalanceStep?.maxGoldDrain ?? (6f + (float)num * 2.2f)));
			skill.cooldown = Mathf.Max(skill.cooldown, bossEncounterBalanceStep?.minimumGoldDrainCooldown ?? 10.5f);
			break;
		case SkillEffectType.BossFortify:
			skill.power = Mathf.Min(skill.power, ResolveBossSkillPowerCap(bossEncounterBalanceStep, overflow, bossEncounterBalanceStep?.maxBossFortifyRatio ?? (0.075f + (float)num * 0.018f)));
			skill.duration = Mathf.Min(skill.duration, ResolveBossSkillDurationCap(bossEncounterBalanceStep, overflow, bossEncounterBalanceStep?.maxBossFortifyDuration ?? (3.2f + (float)num * 0.35f)));
			break;
		case SkillEffectType.MonsterRally:
			skill.power = Mathf.Min(skill.power, ResolveBossSkillPowerCap(bossEncounterBalanceStep, overflow, bossEncounterBalanceStep?.maxRallyPower ?? (0.13f + (float)num * 0.025f)));
			skill.duration = Mathf.Min(skill.duration, ResolveBossSkillDurationCap(bossEncounterBalanceStep, overflow, bossEncounterBalanceStep?.maxRallyDuration ?? (3.4f + (float)num * 0.38f)));
			break;
		}
	}

	private int ResolveBossSkillTargetCap(BossEncounterBalanceStep balance, int overflow, int baseValue)
	{
		int num = ((balance != null) ? (overflow * Mathf.Max(0, balance.extraTargetsPerEncounter)) : 0);
		return Mathf.Max(1, baseValue + num);
	}

	private float ResolveBossSkillPowerCap(BossEncounterBalanceStep balance, int overflow, float baseValue)
	{
		float num = ((balance != null) ? ((float)overflow * Mathf.Max(0f, balance.extraSkillCapPerEncounter)) : 0f);
		return Mathf.Max(0f, baseValue + num);
	}

	private float ResolveBossSkillDurationCap(BossEncounterBalanceStep balance, int overflow, float baseValue)
	{
		float num = ((balance != null) ? ((float)overflow * Mathf.Max(0f, balance.extraDurationCapPerEncounter)) : 0f);
		return Mathf.Max(0.1f, baseValue + num);
	}

	private float ResolveBossSkillRadiusCap(BossEncounterBalanceStep balance, int overflow, float baseValue)
	{
		float num = ((balance != null) ? ((float)overflow * Mathf.Max(0f, balance.extraRadiusCapPerEncounter)) : 0f);
		return Mathf.Max(0.1f, baseValue + num);
	}

	private MonsterDefinition CloneMonsterDefinition(MonsterDefinition source)
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		if (source == null)
		{
			return null;
		}
		MonsterDefinition monsterDefinition = new MonsterDefinition();
		monsterDefinition.id = source.id;
		monsterDefinition.displayName = source.displayName;
		monsterDefinition.description = source.description;
		monsterDefinition.grade = source.grade;
		monsterDefinition.role = source.role;
		monsterDefinition.threatLevel = source.threatLevel;
		monsterDefinition.minRound = source.minRound;
		monsterDefinition.rosterSourceId = source.rosterSourceId;
		monsterDefinition.rosterIndex = source.rosterIndex;
		monsterDefinition.variantIndex = source.variantIndex;
		monsterDefinition.accentColor = source.accentColor;
		monsterDefinition.prefab = source.prefab;
		monsterDefinition.isBoss = source.isBoss;
		monsterDefinition.visualScale = source.visualScale;
		monsterDefinition.rewardGold = source.rewardGold;
		monsterDefinition.stats = CloneCombatStats(source.stats);
		monsterDefinition.attackBehavior = CloneAttackBehavior(source.attackBehavior);
		monsterDefinition.skills = CloneSkills(source.skills);
		return monsterDefinition;
	}

	private CombatStats CloneCombatStats(CombatStats source)
	{
		if (source == null)
		{
			return new CombatStats();
		}
		return new CombatStats
		{
			maxHealth = source.maxHealth,
			attackPower = source.attackPower,
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

	private List<SkillDefinition> CloneSkills(List<SkillDefinition> source)
	{
		List<SkillDefinition> list = new List<SkillDefinition>();
		if (source == null)
		{
			return list;
		}
		for (int i = 0; i < source.Count; i++)
		{
			list.Add(CloneSkill(source[i]));
		}
		return list;
	}

	private SkillDefinition CloneSkill(SkillDefinition source)
	{
		if (source == null)
		{
			return null;
		}
		return new SkillDefinition
		{
			id = source.id,
			displayName = source.displayName,
			description = source.description,
			effectType = source.effectType,
			category = source.category,
			deliveryType = source.deliveryType,
			useCustomCastRange = source.useCustomCastRange,
			castRange = source.castRange,
			isGlobalTargeting = source.isGlobalTargeting,
			power = source.power,
			secondaryPower = source.secondaryPower,
			duration = source.duration,
			radius = source.radius,
			manaThreshold = source.manaThreshold,
			cooldown = source.cooldown,
			hitCount = source.hitCount,
			growthTargets = source.growthTargets,
			growthStepRatio = source.growthStepRatio,
			projectilePrefab = source.projectilePrefab,
			muzzleEffectPrefab = source.muzzleEffectPrefab,
			hitEffectPrefab = source.hitEffectPrefab,
			areaEffectPrefab = source.areaEffectPrefab
		};
	}

	private string ToRoman(int value)
	{
		string[] array = new string[10] { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
		if (value > 0 && value <= array.Length)
		{
			return array[value - 1];
		}
		return value.ToString();
	}

	private MonsterDefinition CreateMonster(string name, CharacterGrade grade, int seed)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		MonsterRole role = ResolveRole(seed, isBoss: false);
		MonsterDefinition monsterDefinition = new MonsterDefinition();
		monsterDefinition.id = $"mob_{seed + 1:D2}";
		monsterDefinition.displayName = name;
		monsterDefinition.description = name + " marches toward the final line as a " + role.ToString() + ".";
		monsterDefinition.grade = grade;
		monsterDefinition.role = role;
		monsterDefinition.threatLevel = MonsterThreatLevel.Regular;
		monsterDefinition.minRound = 1;
		monsterDefinition.rosterSourceId = monsterDefinition.id;
		monsterDefinition.rosterIndex = seed;
		monsterDefinition.variantIndex = 0;
		monsterDefinition.isBoss = false;
		monsterDefinition.rewardGold = (int)(3 + grade);
		monsterDefinition.accentColor = ResolveColor(grade, role);
		monsterDefinition.stats = BuildStats(grade, role, seed, isBoss: false);
		monsterDefinition.skills = BuildSkills(name, grade, role, MonsterThreatLevel.Regular, seed);
		ApplyRegularSwarmProfile(monsterDefinition, scaleReward: true);
		return monsterDefinition;
	}

	private MonsterDefinition CreateMidBoss(string name, int seed)
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		CharacterGrade characterGrade = ResolveMidBossGrade(seed);
		MonsterRole role = ResolveMidBossRole(seed);
		MonsterDefinition monsterDefinition = new MonsterDefinition();
		monsterDefinition.id = $"midboss_{seed + 1:D2}";
		monsterDefinition.displayName = name;
		monsterDefinition.description = name + " enters as a mid-boss with a disruptive skill pattern.";
		monsterDefinition.grade = characterGrade;
		monsterDefinition.role = role;
		monsterDefinition.threatLevel = MonsterThreatLevel.MidBoss;
		monsterDefinition.minRound = 1;
		monsterDefinition.rosterSourceId = monsterDefinition.id;
		monsterDefinition.rosterIndex = seed;
		monsterDefinition.variantIndex = 0;
		monsterDefinition.isBoss = false;
		monsterDefinition.rewardGold = 18 + seed * 3 + (int)characterGrade * 4;
		monsterDefinition.accentColor = Color.Lerp(ResolveColor(characterGrade, role), new Color(1f, 0.42f, 0.18f), 0.35f);
		monsterDefinition.visualScale = ResolveVisualScale(MonsterThreatLevel.MidBoss, characterGrade, role, 0);
		monsterDefinition.stats = BuildMidBossStats(characterGrade, role, seed);
		monsterDefinition.skills = BuildMidBossSkills(name, characterGrade, role, seed);
		return monsterDefinition;
	}

	private MonsterDefinition CreateBoss(string name, int seed)
	{
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		CharacterGrade grade = ((seed < 2) ? CharacterGrade.Legendary : CharacterGrade.Mythic);
		MonsterDefinition monsterDefinition = new MonsterDefinition();
		monsterDefinition.id = $"boss_{seed + 1:D2}";
		monsterDefinition.displayName = name;
		monsterDefinition.description = name + " leads the invasion with two deadly boss skills.";
		monsterDefinition.grade = grade;
		monsterDefinition.role = MonsterRole.Boss;
		monsterDefinition.threatLevel = MonsterThreatLevel.Boss;
		monsterDefinition.minRound = 1;
		monsterDefinition.rosterSourceId = monsterDefinition.id;
		monsterDefinition.rosterIndex = seed;
		monsterDefinition.variantIndex = 0;
		monsterDefinition.isBoss = true;
		monsterDefinition.rewardGold = 45 + seed * 15;
		monsterDefinition.accentColor = new Color(1f, 0.25f + (float)seed * 0.08f, 0.25f);
		monsterDefinition.visualScale = ResolveVisualScale(MonsterThreatLevel.Boss, grade, MonsterRole.Boss, 0);
		monsterDefinition.stats = BuildStats(grade, MonsterRole.Boss, seed, isBoss: true);
		monsterDefinition.skills = BuildSkills(name, grade, MonsterRole.Boss, MonsterThreatLevel.Boss, seed);
		return monsterDefinition;
	}

	private CombatStats BuildMidBossStats(CharacterGrade grade, MonsterRole role, int seed)
	{
		MonsterStatBalance monsterStatBalance = ResolveMonsterStatBalance();
		CombatStats combatStats = BuildStats(grade, role, seed + 12, isBoss: false);
		combatStats.maxHealth *= monsterStatBalance.midBossHealthMultiplier + (float)seed * monsterStatBalance.midBossHealthMultiplierPerSeed;
		combatStats.attackPower *= monsterStatBalance.midBossAttackMultiplier + (float)seed * monsterStatBalance.midBossAttackMultiplierPerSeed;
		combatStats.attackSpeed *= monsterStatBalance.midBossAttackSpeedMultiplier;
		combatStats.moveSpeed *= monsterStatBalance.midBossMoveSpeedMultiplier;
		combatStats.maxMana = monsterStatBalance.midBossMaxMana;
		combatStats.manaRegenPerSecondRate = monsterStatBalance.midBossManaRegenRate;
		combatStats.manaGainWhenHitRate = monsterStatBalance.midBossManaGainWhenHitRate;
		combatStats.manaGainPerAttackRate = monsterStatBalance.midBossManaGainPerAttackRate;
		combatStats.attackRange += ((role == MonsterRole.Caster) ? monsterStatBalance.midBossCasterRangeBonus : monsterStatBalance.midBossRangeBonus);
		return combatStats;
	}

	private CombatStats BuildStats(CharacterGrade grade, MonsterRole role, int seed, bool isBoss)
	{
		MonsterStatBalance monsterStatBalance = ResolveMonsterStatBalance();
		CombatStats combatStats = new CombatStats();
		combatStats.maxHealth = monsterStatBalance.baseHealth + (float)grade * monsterStatBalance.healthPerGrade + (float)seed * monsterStatBalance.healthPerSeed;
		combatStats.attackPower = monsterStatBalance.baseAttackPower + (float)grade * monsterStatBalance.attackPowerPerGrade;
		combatStats.criticalChance = Mathf.Clamp01(monsterStatBalance.baseCriticalChance + (float)grade * monsterStatBalance.criticalChancePerGrade);
		combatStats.criticalDamageMultiplier = monsterStatBalance.baseCriticalDamageMultiplier + (float)grade * monsterStatBalance.criticalDamageMultiplierPerGrade;
		combatStats.attackSpeed = monsterStatBalance.baseAttackSpeed + (float)grade * monsterStatBalance.attackSpeedPerGrade;
		combatStats.maxMana = monsterStatBalance.baseMaxMana + (float)grade * monsterStatBalance.maxManaPerGrade;
		combatStats.attackRange = monsterStatBalance.baseAttackRange + (float)grade * monsterStatBalance.attackRangePerGrade + (float)(seed % 2) * monsterStatBalance.alternatingAttackRangeBonus;
		combatStats.moveSpeed = monsterStatBalance.baseMoveSpeed + (float)grade * monsterStatBalance.moveSpeedPerGrade;
		combatStats.projectileSpeed = monsterStatBalance.projectileSpeed;
		combatStats.manaRegenPerSecondRate = monsterStatBalance.manaRegenPerSecondRate;
		combatStats.manaGainWhenHitRate = monsterStatBalance.manaGainWhenHitRate;
		combatStats.manaGainPerAttackRate = monsterStatBalance.manaGainPerAttackRate;
		switch (role)
		{
		case MonsterRole.Charger:
			combatStats.moveSpeed *= monsterStatBalance.chargerMoveSpeedMultiplier;
			combatStats.attackSpeed *= monsterStatBalance.chargerAttackSpeedMultiplier;
			combatStats.attackRange = monsterStatBalance.chargerAttackRange;
			combatStats.manaGainPerAttackRate = monsterStatBalance.chargerManaGainPerAttackRate;
			break;
		case MonsterRole.Brute:
			combatStats.maxHealth *= monsterStatBalance.bruteHealthMultiplier;
			combatStats.attackPower *= monsterStatBalance.bruteAttackMultiplier;
			combatStats.moveSpeed *= monsterStatBalance.bruteMoveSpeedMultiplier;
			combatStats.attackRange += monsterStatBalance.bruteAttackRangeBonus;
			combatStats.manaGainWhenHitRate = monsterStatBalance.bruteManaGainWhenHitRate;
			break;
		case MonsterRole.Caster:
			combatStats.maxMana *= monsterStatBalance.casterMaxManaMultiplier;
			combatStats.attackRange += monsterStatBalance.casterAttackRangeBonus;
			combatStats.manaRegenPerSecondRate = monsterStatBalance.casterManaRegenPerSecondRate;
			combatStats.manaGainPerAttackRate = monsterStatBalance.casterManaGainPerAttackRate;
			break;
		case MonsterRole.Elite:
			combatStats.maxHealth *= monsterStatBalance.eliteHealthMultiplier;
			combatStats.attackPower *= monsterStatBalance.eliteAttackMultiplier;
			combatStats.criticalChance += monsterStatBalance.eliteCriticalChanceBonus;
			combatStats.attackRange += monsterStatBalance.eliteAttackRangeBonus;
			combatStats.manaGainPerAttackRate = monsterStatBalance.eliteManaGainPerAttackRate;
			break;
		}
		if (isBoss)
		{
			combatStats.maxHealth = monsterStatBalance.bossBaseHealth + (float)seed * monsterStatBalance.bossHealthPerSeed;
			combatStats.attackPower = monsterStatBalance.bossBaseAttackPower + (float)seed * monsterStatBalance.bossAttackPowerPerSeed;
			combatStats.criticalChance = monsterStatBalance.bossBaseCriticalChance + (float)seed * monsterStatBalance.bossCriticalChancePerSeed;
			combatStats.criticalDamageMultiplier = monsterStatBalance.bossCriticalDamageMultiplier;
			combatStats.attackSpeed = monsterStatBalance.bossBaseAttackSpeed + (float)seed * monsterStatBalance.bossAttackSpeedPerSeed;
			combatStats.maxMana = monsterStatBalance.bossMaxMana;
			combatStats.manaRegenPerSecondRate = monsterStatBalance.bossManaRegenPerSecondRate;
			combatStats.manaGainWhenHitRate = monsterStatBalance.bossManaGainWhenHitRate;
			combatStats.manaGainPerAttackRate = monsterStatBalance.bossManaGainPerAttackRate;
			combatStats.attackRange = monsterStatBalance.bossAttackRange;
			combatStats.moveSpeed = monsterStatBalance.bossBaseMoveSpeed + (float)seed * monsterStatBalance.bossMoveSpeedPerSeed;
			ApplyBossSeedStatModifier(combatStats, seed);
		}
		combatStats.criticalChance = Mathf.Clamp01(combatStats.criticalChance);
		return combatStats;
	}

	private MonsterStatBalance ResolveMonsterStatBalance()
	{
		if (monsterStatBalance == null)
		{
			monsterStatBalance = new MonsterStatBalance();
		}
		return monsterStatBalance;
	}

	private void ApplyBossSeedStatModifier(CombatStats stats, int seed)
	{
		if (stats == null || bossSeedStatModifierSteps == null)
		{
			return;
		}
		for (int i = 0; i < bossSeedStatModifierSteps.Count; i++)
		{
			BossSeedStatModifierStep bossSeedStatModifierStep = bossSeedStatModifierSteps[i];
			if (bossSeedStatModifierStep != null && bossSeedStatModifierStep.AppliesTo(seed))
			{
				stats.maxHealth *= Mathf.Max(0.01f, bossSeedStatModifierStep.healthMultiplier);
				stats.attackPower *= Mathf.Max(0.01f, bossSeedStatModifierStep.attackMultiplier);
				stats.attackSpeed *= Mathf.Max(0.01f, bossSeedStatModifierStep.attackSpeedMultiplier);
				stats.moveSpeed *= Mathf.Max(0.01f, bossSeedStatModifierStep.moveSpeedMultiplier);
				stats.attackRange += bossSeedStatModifierStep.attackRangeBonus;
				stats.manaRegenPerSecondRate += bossSeedStatModifierStep.manaRegenBonus;
				if (bossSeedStatModifierStep.maxManaOverride > 0f)
				{
					stats.maxMana = bossSeedStatModifierStep.maxManaOverride;
				}
				break;
			}
		}
	}

	private void ApplyRegularSwarmProfile(MonsterDefinition definition, bool scaleReward)
	{
		if (definition != null && definition.threatLevel == MonsterThreatLevel.Regular && definition.stats != null)
		{
			int num = Mathf.Max(0, (int)definition.grade);
			float num2 = Mathf.Clamp(regularHealthMultiplier + (float)num * 0.025f + (float)definition.variantIndex * 0.015f, 0.45f, 0.88f);
			float num3 = Mathf.Clamp(regularAttackMultiplier + (float)num * 0.015f, 0.55f, 0.98f);
			definition.stats.maxHealth *= num2;
			definition.stats.attackPower *= num3;
			definition.stats.moveSpeed *= 1.02f;
			definition.visualScale = ResolveVisualScale(definition.threatLevel, definition.grade, definition.role, definition.variantIndex);
			if (scaleReward)
			{
				definition.rewardGold = Mathf.Max(1, Mathf.RoundToInt((float)definition.rewardGold * Mathf.Clamp(regularRewardMultiplier, 0.25f, 1f)));
			}
		}
	}

	private float ResolveVisualScale(MonsterThreatLevel threatLevel, CharacterGrade grade, MonsterRole role, int variantIndex)
	{
		int num = Mathf.Max(0, (int)grade);
		int num2 = Mathf.Max(0, variantIndex);
		switch (threatLevel)
		{
		case MonsterThreatLevel.Boss:
			return Mathf.Clamp(1.68f + (float)num2 * 0.045f, 1.6f, 1.95f);
		case MonsterThreatLevel.MidBoss:
		{
			float num4 = 1.18f + (float)num * 0.025f + (float)num2 * 0.025f;
			switch (role)
			{
			case MonsterRole.Brute:
				num4 += 0.08f;
				break;
			case MonsterRole.Charger:
				num4 -= 0.04f;
				break;
			}
			return Mathf.Clamp(num4, 1.12f, 1.42f);
		}
		default:
		{
			float num3 = 0.74f + (float)num * 0.025f + (float)num2 * 0.012f;
			switch (role)
			{
			case MonsterRole.Brute:
				num3 += 0.08f;
				break;
			case MonsterRole.Elite:
				num3 += 0.04f;
				break;
			case MonsterRole.Charger:
				num3 -= 0.04f;
				break;
			case MonsterRole.Caster:
				num3 -= 0.02f;
				break;
			}
			return Mathf.Clamp(num3, 0.66f, 0.96f);
		}
		}
	}

	private List<SkillDefinition> BuildSkills(string ownerName, CharacterGrade grade, MonsterRole role, MonsterThreatLevel threatLevel, int seed)
	{
		switch (threatLevel)
		{
		case MonsterThreatLevel.Boss:
			return BuildBossSkills(ownerName, seed);
		case MonsterThreatLevel.MidBoss:
			return BuildMidBossSkills(ownerName, grade, role, seed);
		default:
		{
			List<SkillEffectType> list = BuildRoleSkillPool(role, isBoss: false);
			int skillCount = GradeRules.GetSkillCount(grade);
			List<SkillDefinition> list2 = new List<SkillDefinition>(skillCount);
			for (int i = 0; i < skillCount; i++)
			{
				SkillEffectType effectType = list[(seed + i) % list.Count];
				list2.Add(CreateSkill(ownerName, effectType, isBoss: false, i));
			}
			return list2;
		}
		}
	}

	private List<SkillDefinition> BuildMidBossSkills(string ownerName, CharacterGrade grade, MonsterRole role, int seed)
	{
		List<SkillDefinition> list = new List<SkillDefinition>(2);
		switch (Mathf.Abs(seed) % 6)
		{
		case 0:
			list.Add(CreateBossSkill(ownerName, SkillEffectType.Stun, "감옥 족쇄", "가장 가까운 유닛을 짧게 기절시킵니다.", 0f, 1.35f, 0f, 1, 72f, 7.5f, 0));
			list.Add(CreateBossSkill(ownerName, SkillEffectType.BossFortify, "철갑 보수", "체력을 회복하고 잠시 공격 속도를 올립니다.", 0.07f, 3.5f, 0f, 1, 92f, 10f, 1));
			break;
		case 1:
			list.Add(CreateBossSkill(ownerName, SkillEffectType.ManaBurn, "마나 누수", "무작위 유닛 2기의 마나를 태웁니다.", 0.3f, 0f, 0f, 2, 82f, 8f, 0));
			list.Add(CreateBossSkill(ownerName, SkillEffectType.AreaDamage, "유리 파동", "주변 유닛에게 광역 피해를 줍니다.", 0.95f, 0f, 3.2f, 1, 94f, 8f, 1));
			break;
		case 2:
			list.Add(CreateBossSkill(ownerName, SkillEffectType.MonsterRally, "돌격 명령", "5.5m 안의 몬스터 무리를 잠시 강화합니다.", 0.16f, 4f, 0f, 1, 78f, 8f, 0));
			list.Add(CreateBossSkill(ownerName, SkillEffectType.SummonRush, "파쇄 돌진", "여러 유닛에게 빠르게 피해를 줍니다.", 0.72f, 0f, 0f, 3, 96f, 9f, 1));
			break;
		case 3:
			list.Add(CreateBossSkill(ownerName, SkillEffectType.GoldDrain, "통행세 징수", "보유 골드를 일부 빼앗습니다.", 8f + (float)seed, 0f, 0f, 1, 84f, 10f, 0));
			list.Add(CreateBossSkill(ownerName, SkillEffectType.MoveSpeedBoost, "광란 질주", "잠시 이동 속도가 크게 증가합니다.", 0.55f, 3.5f, 0f, 1, 70f, 8f, 1));
			break;
		case 4:
			list.Add(CreateBossSkill(ownerName, SkillEffectType.MassStun, "서리 봉인", "무작위 유닛 2기를 짧게 묶습니다.", 0f, 1f, 0f, 2, 88f, 9f, 0));
			list.Add(CreateBossSkill(ownerName, SkillEffectType.HealSelf, "빙결 재생", "체력을 일부 회복합니다.", 0.1f, 0f, 0f, 1, 92f, 10f, 1));
			break;
		default:
			list.Add(CreateBossSkill(ownerName, SkillEffectType.CriticalBoost, "학살 본능", "잠시 치명타 확률을 높입니다.", 0.22f, 4.5f, 0f, 1, 76f, 8f, 0));
			list.Add(CreateBossSkill(ownerName, SkillEffectType.DirectDamage, "참수 일격", "가장 가까운 유닛에게 강한 피해를 줍니다.", 1.55f, 0f, 0f, 1, 96f, 8.5f, 1));
			break;
		}
		return list;
	}

	private List<SkillDefinition> BuildBossSkills(string ownerName, int seed)
	{
		List<SkillDefinition> list = new List<SkillDefinition>(2);
		switch (Mathf.Abs(seed) % 8)
		{
		case 0:
			list.Add(CreateBossSkill(ownerName, SkillEffectType.MassStun, "대지 균열", "무작위 유닛 2기에 공격력 85% 피해를 주고 짧게 기절시킵니다.", 0.85f, 1.45f, 0f, 2, 82f, 8.5f, 0));
			list.Add(CreateBossSkill(ownerName, SkillEffectType.BossFortify, "암석 방벽", "체력을 10% 회복하고 잠시 공격 속도를 높입니다.", 0.1f, 4.5f, 0f, 1, 95f, 9.5f, 1));
			break;
		case 1:
			list.Add(CreateBossSkill(ownerName, SkillEffectType.DeathPact, "죽음의 서약", "무작위 유닛 하나를 처형합니다.", 0f, 0f, 0f, 1, 100f, 14f, 0));
			list.Add(CreateBossSkill(ownerName, SkillEffectType.Stun, "여왕의 속박", "가장 가까운 유닛을 기절시킵니다.", 0f, 2.4f, 0f, 1, 75f, 7f, 1));
			break;
		case 2:
			list.Add(CreateBossSkill(ownerName, SkillEffectType.AreaDamage, "운석 낙하", "주변 유닛에게 공격력 125%의 광역 피해를 줍니다.", 1.25f, 0f, 4f, 1, 80f, 9.5f, 0));
			list.Add(CreateBossSkill(ownerName, SkillEffectType.ManaBurn, "마나 침식", "무작위 유닛 3기의 마나를 45% 태웁니다.", 0.45f, 0f, 0f, 3, 92f, 9f, 1));
			break;
		case 3:
			list.Add(CreateBossSkill(ownerName, SkillEffectType.MonsterRally, "군단 집결", "5.5m 안의 몬스터 이동속도와 공격속도를 잠시 올립니다.", 0.22f, 5.5f, 0f, 1, 80f, 9f, 0));
			list.Add(CreateBossSkill(ownerName, SkillEffectType.GoldDrain, "탐욕의 징수", "보유 골드를 강제로 빼앗습니다.", 18f, 0f, 0f, 1, 92f, 11f, 1));
			break;
		case 4:
			list.Add(CreateBossSkill(ownerName, SkillEffectType.DeathPact, "공허 처형", "무작위 유닛 하나를 전장에서 지웁니다.", 0f, 0f, 0f, 1, 95f, 13f, 0));
			list.Add(CreateBossSkill(ownerName, SkillEffectType.MassStun, "무한 정지장", "무작위 유닛들을 기절시킵니다.", 0f, 1.6f, 0f, 3, 85f, 8.5f, 1));
			break;
		case 5:
			list.Add(CreateBossSkill(ownerName, SkillEffectType.ManaBurn, "마나 약탈", "마나가 쌓인 유닛들의 마나를 태웁니다.", 0.38f, 0f, 0f, 2, 78f, 7.8f, 0));
			list.Add(CreateBossSkill(ownerName, SkillEffectType.MonsterRally, "돌격 북소리", "5.5m 안의 몬스터 무리를 잠시 가속시킵니다.", 0.18f, 4.6f, 0f, 1, 88f, 8.8f, 1));
			break;
		case 6:
			list.Add(CreateBossSkill(ownerName, SkillEffectType.BossFortify, "보호막 충전", "보스가 체력을 회복하고 잠시 강화됩니다.", 0.1f, 4.4f, 0f, 1, 76f, 9.2f, 0));
			list.Add(CreateBossSkill(ownerName, SkillEffectType.AreaDamage, "압력 폭발", "주변 유닛에게 강한 광역 피해를 줍니다.", 1.12f, 0f, 3.4f, 1, 92f, 8.2f, 1));
			break;
		default:
			list.Add(CreateBossSkill(ownerName, SkillEffectType.Stun, "앞줄 압박", "가장 가까운 유닛을 기절시켜 전열을 무너뜨립니다.", 0f, 2f, 0f, 1, 72f, 7.4f, 0));
			list.Add(CreateBossSkill(ownerName, SkillEffectType.GoldDrain, "패배세 징수", "보유 골드를 빼앗아 운영을 흔듭니다.", 14f, 0f, 0f, 1, 88f, 9.8f, 1));
			break;
		}
		return list;
	}

	private List<SkillEffectType> BuildRoleSkillPool(MonsterRole role, bool isBoss)
	{
		if (isBoss)
		{
			return new List<SkillEffectType>
			{
				SkillEffectType.AreaDamage,
				SkillEffectType.SummonRush,
				SkillEffectType.MoveSpeedBoost,
				SkillEffectType.DirectDamage
			};
		}
		return role switch
		{
			MonsterRole.Charger => new List<SkillEffectType>
			{
				SkillEffectType.MoveSpeedBoost,
				SkillEffectType.DirectDamage
			}, 
			MonsterRole.Brute => new List<SkillEffectType>
			{
				SkillEffectType.DirectDamage,
				SkillEffectType.HealSelf,
				SkillEffectType.Stun
			}, 
			MonsterRole.Caster => new List<SkillEffectType>
			{
				SkillEffectType.AreaDamage,
				SkillEffectType.ManaSurge,
				SkillEffectType.Stun
			}, 
			MonsterRole.Elite => new List<SkillEffectType>
			{
				SkillEffectType.CriticalBoost,
				SkillEffectType.DirectDamage,
				SkillEffectType.AreaDamage,
				SkillEffectType.Stun
			}, 
			_ => new List<SkillEffectType>
			{
				SkillEffectType.DirectDamage,
				SkillEffectType.MoveSpeedBoost,
				SkillEffectType.ManaSurge
			}, 
		};
	}

	private SkillDefinition CreateBossSkill(string ownerName, SkillEffectType effectType, string displayName, string description, float power, float duration, float radius, int hitCount, float manaThreshold, float cooldown, int index)
	{
		SkillDefinition skillDefinition = new SkillDefinition();
		skillDefinition.id = $"{ownerName}_{effectType}_{index}";
		skillDefinition.displayName = displayName;
		skillDefinition.description = description;
		skillDefinition.effectType = effectType;
		skillDefinition.power = power;
		skillDefinition.duration = duration;
		skillDefinition.radius = radius;
		skillDefinition.hitCount = hitCount;
		skillDefinition.manaThreshold = manaThreshold;
		skillDefinition.cooldown = cooldown;
		skillDefinition.isGlobalTargeting = UsesGlobalMonsterTargeting(effectType);
		ApplyMonsterSkillRangeDefaults(skillDefinition);
		return skillDefinition;
	}

	private static bool UsesGlobalMonsterTargeting(SkillEffectType effectType)
	{
		return effectType == SkillEffectType.DeathPact || effectType == SkillEffectType.MassStun || effectType == SkillEffectType.GoldDrain || effectType == SkillEffectType.ManaBurn;
	}

	private static void ApplyMonsterSkillRangeDefaults(SkillDefinition skill)
	{
		if (skill != null)
		{
			if (skill.effectType == SkillEffectType.DirectDamage || skill.effectType == SkillEffectType.Stun || skill.effectType == SkillEffectType.AttackPowerReduction)
			{
				skill.useCustomCastRange = true;
				skill.castRange = 3f;
			}
			else if (skill.effectType == SkillEffectType.AreaDamage)
			{
				skill.useCustomCastRange = true;
				skill.castRange = Mathf.Max(0.5f, skill.radius);
			}
			else if (skill.effectType == SkillEffectType.SummonRush)
			{
				skill.useCustomCastRange = true;
				skill.castRange = 3.6f;
			}
			else if (skill.effectType == SkillEffectType.MonsterRally && skill.radius <= 0.1f)
			{
				skill.radius = 5.5f;
			}
		}
	}

	private SkillDefinition CreateSkill(string ownerName, SkillEffectType effectType, bool isBoss, int index)
	{
		SkillDefinition skillDefinition = new SkillDefinition();
		skillDefinition.id = $"{ownerName}_{effectType}_{index}";
		skillDefinition.effectType = effectType;
		skillDefinition.manaThreshold = (isBoss ? 80f : 100f);
		skillDefinition.radius = (isBoss ? 3.4f : 2.4f);
		skillDefinition.hitCount = ((effectType != SkillEffectType.SummonRush) ? 1 : 3);
		switch (effectType)
		{
		case SkillEffectType.DirectDamage:
			skillDefinition.displayName = (isBoss ? "King's Smash" : "Savage Strike");
			skillDefinition.description = "Heavy damage to the nearest defender.";
			skillDefinition.power = (isBoss ? 2.1f : 1.7f);
			skillDefinition.cooldown = 5f;
			break;
		case SkillEffectType.AreaDamage:
			skillDefinition.displayName = (isBoss ? "Cataclysm Roar" : "Crushing Roar");
			skillDefinition.description = "Deals area damage around the attacker.";
			skillDefinition.power = (isBoss ? 1.45f : 1.1f);
			skillDefinition.cooldown = (isBoss ? 6f : 7f);
			break;
		case SkillEffectType.HealSelf:
			skillDefinition.displayName = "Dark Regeneration";
			skillDefinition.description = "Recover a portion of max health.";
			skillDefinition.power = (isBoss ? 0.18f : 0.22f);
			skillDefinition.cooldown = 8f;
			break;
		case SkillEffectType.MoveSpeedBoost:
			skillDefinition.displayName = (isBoss ? "Tyrant Rush" : "Rush");
			skillDefinition.description = "Temporarily increases movement speed.";
			skillDefinition.power = (isBoss ? 0.65f : 0.5f);
			skillDefinition.duration = 4f;
			skillDefinition.cooldown = 9f;
			break;
		case SkillEffectType.CriticalBoost:
			skillDefinition.displayName = "Frenzy";
			skillDefinition.description = "Temporarily increases critical chance.";
			skillDefinition.power = 0.2f;
			skillDefinition.duration = 5f;
			skillDefinition.cooldown = 8f;
			break;
		case SkillEffectType.ManaSurge:
			skillDefinition.displayName = "Mana Hunger";
			skillDefinition.description = "Quickly recovers mana.";
			skillDefinition.power = 0.42f;
			skillDefinition.cooldown = 6f;
			break;
		case SkillEffectType.Stun:
			skillDefinition.displayName = (isBoss ? "Royal Shackle" : "Crushing Grip");
			skillDefinition.description = "Briefly stuns a defender.";
			skillDefinition.power = 0f;
			skillDefinition.duration = (isBoss ? 2f : 1.15f);
			skillDefinition.cooldown = (isBoss ? 7f : 9f);
			break;
		default:
			skillDefinition.displayName = (isBoss ? "Swarm Command" : "Rush Spawn");
			skillDefinition.description = "Strikes multiple defenders in rapid succession.";
			skillDefinition.effectType = SkillEffectType.SummonRush;
			skillDefinition.power = (isBoss ? 0.95f : 0.7f);
			skillDefinition.hitCount = (isBoss ? 4 : 2);
			skillDefinition.cooldown = 8f;
			break;
		}
		ApplyMonsterSkillRangeDefaults(skillDefinition);
		return skillDefinition;
	}

	private CharacterGrade ResolveMaxRegularGrade(int round)
	{
		if (TryResolveRoundGrade(regularGradeUnlockSteps, round, out var grade))
		{
			return grade;
		}
		if (round <= 3)
		{
			return CharacterGrade.Normal;
		}
		if (round <= 6)
		{
			return CharacterGrade.Rare;
		}
		if (round <= 9)
		{
			return CharacterGrade.Epic;
		}
		if (round <= 14)
		{
			return CharacterGrade.Legendary;
		}
		return CharacterGrade.Mythic;
	}

	private CharacterGrade ResolveMaxMidBossGrade(int round)
	{
		if (TryResolveRoundGrade(midBossGradeUnlockSteps, round, out var grade))
		{
			return grade;
		}
		if (round <= 5)
		{
			return CharacterGrade.Rare;
		}
		if (round <= 10)
		{
			return CharacterGrade.Epic;
		}
		if (round <= 18)
		{
			return CharacterGrade.Legendary;
		}
		return CharacterGrade.Mythic;
	}

	private CharacterGrade ResolveMidBossGrade(int seed)
	{
		if (TryResolveIndexGrade(midBossRosterGradeSteps, seed, out var grade))
		{
			return grade;
		}
		if (seed <= 1)
		{
			return CharacterGrade.Rare;
		}
		if (seed <= 4)
		{
			return CharacterGrade.Epic;
		}
		if (seed <= 6)
		{
			return CharacterGrade.Legendary;
		}
		return CharacterGrade.Mythic;
	}

	private MonsterRole ResolveMidBossRole(int seed)
	{
		return (Mathf.Abs(seed) % 4) switch
		{
			0 => MonsterRole.Brute, 
			1 => MonsterRole.Caster, 
			2 => MonsterRole.Elite, 
			_ => MonsterRole.Charger, 
		};
	}

	private MonsterRole ResolveRole(int seed, bool isBoss)
	{
		if (isBoss)
		{
			return MonsterRole.Boss;
		}
		return (seed % 5) switch
		{
			0 => MonsterRole.Grunt, 
			1 => MonsterRole.Charger, 
			2 => MonsterRole.Brute, 
			3 => MonsterRole.Caster, 
			_ => MonsterRole.Elite, 
		};
	}

	private Color ResolveColor(CharacterGrade grade, MonsterRole role)
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		Color val = default(Color);
		((Color)(ref val))._002Ector(0.45f, 0.45f, 0.45f);
		switch (grade)
		{
		case CharacterGrade.Rare:
			((Color)(ref val))._002Ector(0.35f, 0.8f, 0.95f);
			break;
		case CharacterGrade.Epic:
			((Color)(ref val))._002Ector(0.45f, 0.95f, 0.55f);
			break;
		case CharacterGrade.Legendary:
			((Color)(ref val))._002Ector(1f, 0.7f, 0.25f);
			break;
		case CharacterGrade.Mythic:
			((Color)(ref val))._002Ector(0.95f, 0.25f, 0.25f);
			break;
		}
		if (role == MonsterRole.Caster)
		{
			val *= new Color(0.85f, 0.95f, 1.1f);
		}
		if (role == MonsterRole.Brute)
		{
			val *= new Color(1.1f, 0.9f, 0.9f);
		}
		return val;
	}

	private CharacterGrade ResolveGrade(int index, int totalCount)
	{
		float num = ((totalCount <= 1) ? 1f : ((float)index / (float)(totalCount - 1)));
		if (starterGradeDistributionSteps != null && starterGradeDistributionSteps.Count > 0)
		{
			List<StarterGradeDistributionStep> list = (from step in starterGradeDistributionSteps
				where step != null
				orderby step.upperRatioExclusive
				select step).ToList();
			for (int num2 = 0; num2 < list.Count; num2++)
			{
				if (num < list[num2].upperRatioExclusive)
				{
					return list[num2].grade;
				}
			}
			return list[list.Count - 1].grade;
		}
		if (num < 0.35f)
		{
			return CharacterGrade.Normal;
		}
		if (num < 0.65f)
		{
			return CharacterGrade.Rare;
		}
		if (num < 0.85f)
		{
			return CharacterGrade.Epic;
		}
		if (num < 0.96f)
		{
			return CharacterGrade.Legendary;
		}
		return CharacterGrade.Mythic;
	}

	private bool TryResolveRoundGrade(List<RoundGradeUnlockStep> steps, int round, out CharacterGrade grade)
	{
		grade = CharacterGrade.Normal;
		if (steps == null || steps.Count == 0)
		{
			return false;
		}
		RoundGradeUnlockStep roundGradeUnlockStep = null;
		int num = int.MinValue;
		for (int i = 0; i < steps.Count; i++)
		{
			RoundGradeUnlockStep roundGradeUnlockStep2 = steps[i];
			if (roundGradeUnlockStep2 != null && roundGradeUnlockStep2.firstRound <= round && roundGradeUnlockStep2.firstRound >= num)
			{
				roundGradeUnlockStep = roundGradeUnlockStep2;
				num = roundGradeUnlockStep2.firstRound;
			}
		}
		if (roundGradeUnlockStep == null)
		{
			return false;
		}
		grade = roundGradeUnlockStep.maxGrade;
		return true;
	}

	private bool TryResolveIndexGrade(List<IndexGradeStep> steps, int index, out CharacterGrade grade)
	{
		grade = CharacterGrade.Normal;
		if (steps == null || steps.Count == 0)
		{
			return false;
		}
		IndexGradeStep indexGradeStep = null;
		int num = int.MinValue;
		for (int i = 0; i < steps.Count; i++)
		{
			IndexGradeStep indexGradeStep2 = steps[i];
			if (indexGradeStep2 != null && indexGradeStep2.firstIndex <= index && indexGradeStep2.firstIndex >= num)
			{
				indexGradeStep = indexGradeStep2;
				num = indexGradeStep2.firstIndex;
			}
		}
		if (indexGradeStep == null)
		{
			return false;
		}
		grade = indexGradeStep.grade;
		return true;
	}

	private void ApplyPresentationRoster()
	{
		if (!((Object)(object)presentationConfig == (Object)null))
		{
			ApplyPresentationRosterForThreat(MonsterThreatLevel.Regular, monsters);
			ApplyPresentationRosterForThreat(MonsterThreatLevel.MidBoss, midBosses);
			ApplyPresentationRosterForThreat(MonsterThreatLevel.Boss, bosses);
			AddBossesAsLateMidBosses();
		}
	}

	private void ApplyPresentationRosterForThreat(MonsterThreatLevel threatLevel, List<MonsterDefinition> target)
	{
		if (target == null || (Object)(object)presentationConfig == (Object)null || !presentationConfig.HasMonsterRosterEntries(threatLevel))
		{
			return;
		}
		List<MonsterPresentationOverride> monsterRosterEntries = presentationConfig.GetMonsterRosterEntries(threatLevel);
		if (monsterRosterEntries.Count == 0)
		{
			return;
		}
		target.Clear();
		for (int i = 0; i < monsterRosterEntries.Count; i++)
		{
			MonsterPresentationOverride monsterPresentationOverride = monsterRosterEntries[i];
			int num = 0;
			CharacterGrade grade = monsterPresentationOverride.grade;
			CharacterGrade characterGrade = (monsterPresentationOverride.createGradeVariants ? ((CharacterGrade)Mathf.Max((int)monsterPresentationOverride.grade, (int)monsterPresentationOverride.maxVariantGrade)) : monsterPresentationOverride.grade);
			for (int j = (int)grade; j <= (int)characterGrade; j++)
			{
				MonsterDefinition monsterDefinition = CreateMonsterFromRoster(monsterPresentationOverride, threatLevel, i, (CharacterGrade)j, num);
				if (monsterDefinition != null)
				{
					target.Add(monsterDefinition);
				}
				num++;
			}
		}
	}

	private MonsterDefinition CreateMonsterFromRoster(MonsterPresentationOverride entry, MonsterThreatLevel threatLevel, int index, CharacterGrade grade, int variantIndex)
	{
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		if (entry == null)
		{
			return null;
		}
		MonsterRole role = ResolveRosterRole(entry, threatLevel, index);
		MonsterDefinition monsterDefinition = new MonsterDefinition();
		string text = ResolveRosterBaseId(entry, threatLevel, index);
		monsterDefinition.id = ResolveRosterId(text, grade, variantIndex);
		monsterDefinition.displayName = ResolveRosterDisplayName(entry, monsterDefinition.id, grade, variantIndex);
		monsterDefinition.description = BuildRosterDescription(monsterDefinition.displayName, threatLevel, role);
		monsterDefinition.grade = grade;
		monsterDefinition.role = role;
		monsterDefinition.threatLevel = threatLevel;
		monsterDefinition.minRound = Mathf.Max(1, entry.minRound + variantIndex * ResolveVariantRoundStep(entry, threatLevel));
		monsterDefinition.rosterSourceId = text;
		monsterDefinition.rosterIndex = index;
		monsterDefinition.variantIndex = variantIndex;
		monsterDefinition.isBoss = threatLevel == MonsterThreatLevel.Boss;
		bool flag = entry.rewardGoldOverride > 0;
		monsterDefinition.rewardGold = (flag ? Mathf.RoundToInt((float)entry.rewardGoldOverride * (1f + (float)variantIndex * 0.18f)) : ResolveRosterReward(threatLevel, grade, index, variantIndex));
		monsterDefinition.accentColor = ResolveRosterVariantColor(entry, threatLevel, grade, role, index, variantIndex);
		monsterDefinition.visualScale = ResolveVisualScale(threatLevel, grade, role, variantIndex);
		monsterDefinition.prefab = entry.prefab;
		if (threatLevel == MonsterThreatLevel.MidBoss)
		{
			monsterDefinition.stats = BuildMidBossStats(grade, role, index + variantIndex);
			monsterDefinition.skills = BuildMidBossSkills(monsterDefinition.displayName, grade, role, index + variantIndex);
		}
		else
		{
			monsterDefinition.stats = BuildStats(grade, role, index + variantIndex, threatLevel == MonsterThreatLevel.Boss);
			monsterDefinition.skills = BuildSkills(monsterDefinition.displayName, grade, role, threatLevel, index + variantIndex);
		}
		ApplyRosterSkillProfile(monsterDefinition);
		ApplyVariantStatBonus(monsterDefinition.stats, entry.variantStatBonusPerTier, variantIndex, threatLevel);
		if (threatLevel == MonsterThreatLevel.Regular)
		{
			ApplyRegularSwarmProfile(monsterDefinition, !flag);
		}
		return monsterDefinition;
	}

	private void ApplyRosterSkillProfile(MonsterDefinition definition)
	{
		if (definition != null)
		{
			if (string.Equals(definition.rosterSourceId, "mob_10", StringComparison.OrdinalIgnoreCase))
			{
				definition.skills = new List<SkillDefinition> { CreateBossSkill(definition.displayName, SkillEffectType.AttackPowerReduction, "놀리기", "가장 가까운 유닛의 공격력을 10% 감소시킵니다. 5초간 지속됩니다.", 0.1f, 5f, 0f, 1, 78f, 9f, 0) };
			}
			else if (string.Equals(definition.rosterSourceId, "mob_11", StringComparison.OrdinalIgnoreCase))
			{
				definition.skills = new List<SkillDefinition> { CreateBossSkill(definition.displayName, SkillEffectType.DamageReflect, "타운트", "5초간 받은 피해의 10%를 공격자에게 돌려줍니다.", 0.1f, 5f, 0f, 1, 78f, 9f, 0) };
			}
		}
	}

	private MonsterRole ResolveRosterRole(MonsterPresentationOverride entry, MonsterThreatLevel threatLevel, int index)
	{
		if (threatLevel == MonsterThreatLevel.Boss)
		{
			return MonsterRole.Boss;
		}
		if (entry.role == MonsterRole.Boss)
		{
			return (threatLevel == MonsterThreatLevel.MidBoss) ? ResolveMidBossRole(index) : ResolveRole(index, isBoss: false);
		}
		return entry.role;
	}

	private string ResolveRosterBaseId(MonsterPresentationOverride entry, MonsterThreatLevel threatLevel, int index)
	{
		if (!string.IsNullOrWhiteSpace(entry.monsterId))
		{
			return entry.monsterId;
		}
		string arg = "mob";
		switch (threatLevel)
		{
		case MonsterThreatLevel.MidBoss:
			arg = "midboss";
			break;
		case MonsterThreatLevel.Boss:
			arg = "boss";
			break;
		}
		return $"{arg}_{index + 1:D2}";
	}

	private string ResolveRosterId(string baseId, CharacterGrade grade, int variantIndex)
	{
		return (variantIndex <= 0) ? baseId : $"{baseId}_g{(int)grade:D2}";
	}

	private string ResolveRosterDisplayName(MonsterPresentationOverride entry, string fallbackId, CharacterGrade grade, int variantIndex)
	{
		string text = ((!string.IsNullOrWhiteSpace(entry.displayName)) ? entry.displayName : ((!((Object)(object)entry.prefab != (Object)null)) ? fallbackId : ((Object)entry.prefab).name));
		return (variantIndex <= 0) ? text : (text + " " + CharacterGradeUtility.GetDisplayName(grade));
	}

	private string BuildRosterDescription(string displayName, MonsterThreatLevel threatLevel, MonsterRole role)
	{
		return threatLevel switch
		{
			MonsterThreatLevel.Boss => displayName + " is registered as a major boss encounter.", 
			MonsterThreatLevel.MidBoss => displayName + " is registered as a mid-boss threat.", 
			_ => displayName + " is registered as a regular monster with the " + role.ToString() + " role.", 
		};
	}

	private int ResolveRosterReward(MonsterThreatLevel threatLevel, CharacterGrade grade, int index, int variantIndex)
	{
		return threatLevel switch
		{
			MonsterThreatLevel.Boss => 45 + index * 15 + variantIndex * 12, 
			MonsterThreatLevel.MidBoss => 18 + index * 3 + (int)grade * 4 + variantIndex * 5, 
			_ => (int)(3 + grade + variantIndex), 
		};
	}

	private Color ResolveRosterColor(MonsterThreatLevel threatLevel, CharacterGrade grade, MonsterRole role, int index)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		if (threatLevel == MonsterThreatLevel.Boss)
		{
			return new Color(1f, 0.25f + (float)index * 0.08f, 0.25f);
		}
		Color val = ResolveColor(grade, role);
		return (threatLevel == MonsterThreatLevel.MidBoss) ? Color.Lerp(val, new Color(1f, 0.42f, 0.18f), 0.35f) : val;
	}

	private Color ResolveRosterVariantColor(MonsterPresentationOverride entry, MonsterThreatLevel threatLevel, CharacterGrade grade, MonsterRole role, int index, int variantIndex)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		Color color = CharacterGradeUtility.GetColor(grade, ResolveRosterColor(threatLevel, grade, role, index));
		bool flag = HasUsableOverrideColor(entry);
		Color val = (flag ? entry.accentColor : ResolveRosterColor(threatLevel, grade, role, index));
		if (variantIndex <= 0 && flag)
		{
			return val;
		}
		Color val2 = Color.Lerp(val, color, threatLevel switch
		{
			MonsterThreatLevel.MidBoss => 0.62f, 
			MonsterThreatLevel.Boss => 0.72f, 
			_ => 0.52f, 
		});
		if (variantIndex > 0)
		{
			val2 = Color.Lerp(val2, Color.white, Mathf.Min(0.18f, (float)variantIndex * 0.035f));
		}
		return val2;
	}

	private bool HasUsableOverrideColor(MonsterPresentationOverride entry)
	{
		if (entry == null || !entry.overrideColor)
		{
			return false;
		}
		return entry.accentColor.a > 0.01f && ((Color)(ref entry.accentColor)).maxColorComponent > 0.02f;
	}

	private int ResolveVariantRoundStep(MonsterPresentationOverride entry, MonsterThreatLevel threatLevel)
	{
		if (entry.variantRoundStep > 0)
		{
			return entry.variantRoundStep;
		}
		return threatLevel switch
		{
			MonsterThreatLevel.Boss => 10, 
			MonsterThreatLevel.MidBoss => 5, 
			_ => 3, 
		};
	}

	private void ApplyVariantStatBonus(CombatStats stats, float bonusPerTier, int variantIndex, MonsterThreatLevel threatLevel)
	{
		if (stats != null && variantIndex > 0 && !(bonusPerTier <= 0f))
		{
			float num = Mathf.Clamp(bonusPerTier, 0f, 0.35f) * (float)variantIndex;
			stats.maxHealth *= 1f + num * threatLevel switch
			{
				MonsterThreatLevel.MidBoss => 1.12f, 
				MonsterThreatLevel.Boss => 1.25f, 
				_ => 1f, 
			};
			stats.attackPower *= 1f + num * 0.72f;
			stats.maxMana *= 1f + num * 0.35f;
			stats.manaRegenPerSecondRate *= 1f + num * 0.25f;
			stats.moveSpeed *= 1f + Mathf.Min(0.12f, num * 0.18f);
		}
	}

	private void AddBossesAsLateMidBosses()
	{
		if (bosses == null || bosses.Count == 0 || midBosses == null)
		{
			return;
		}
		midBosses.RemoveAll((MonsterDefinition monster) => monster != null && !string.IsNullOrEmpty(monster.id) && monster.id.StartsWith("midboss_shadow_"));
		for (int num = 0; num < bosses.Count; num++)
		{
			MonsterDefinition monsterDefinition = bosses[num];
			if (monsterDefinition != null && !((Object)(object)monsterDefinition.prefab == (Object)null))
			{
				MonsterDefinition monsterDefinition2 = CreateBossShadowMidBoss(monsterDefinition);
				if (monsterDefinition2 != null)
				{
					midBosses.Add(monsterDefinition2);
				}
			}
		}
	}

	private MonsterDefinition CreateBossShadowMidBoss(MonsterDefinition boss)
	{
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		MonsterRole role = ResolveMidBossRole(boss.rosterIndex + boss.variantIndex);
		MonsterDefinition monsterDefinition = new MonsterDefinition();
		monsterDefinition.id = "midboss_shadow_" + boss.id;
		monsterDefinition.displayName = boss.displayName + " Echo";
		monsterDefinition.description = boss.displayName + " returns as a weakened mid-boss shadow.";
		monsterDefinition.grade = boss.grade;
		monsterDefinition.role = role;
		monsterDefinition.threatLevel = MonsterThreatLevel.MidBoss;
		monsterDefinition.minRound = ResolveBossShadowMidBossRound(boss);
		monsterDefinition.rosterSourceId = boss.rosterSourceId;
		monsterDefinition.rosterIndex = boss.rosterIndex;
		monsterDefinition.variantIndex = boss.variantIndex;
		monsterDefinition.isBoss = false;
		monsterDefinition.rewardGold = Mathf.RoundToInt(20f + (float)boss.grade * 5f + (float)boss.variantIndex * 6f);
		monsterDefinition.accentColor = Color.Lerp(boss.accentColor, new Color(1f, 0.55f, 0.16f), 0.25f);
		monsterDefinition.visualScale = ResolveVisualScale(MonsterThreatLevel.MidBoss, monsterDefinition.grade, role, boss.variantIndex);
		monsterDefinition.prefab = boss.prefab;
		monsterDefinition.stats = BuildBossShadowMidBossStats(boss, role);
		monsterDefinition.skills = BuildMidBossSkills(monsterDefinition.displayName, monsterDefinition.grade, role, boss.rosterIndex + boss.variantIndex);
		return monsterDefinition;
	}

	private int ResolveBossShadowMidBossRound(MonsterDefinition boss)
	{
		int num = 20 + boss.rosterIndex * 4 + boss.variantIndex * 8;
		return Mathf.Max(num, boss.minRound + 10);
	}

	private CombatStats BuildBossShadowMidBossStats(MonsterDefinition boss, MonsterRole role)
	{
		CombatStats combatStats = new CombatStats();
		float num = Mathf.Max(0, boss.variantIndex);
		float num2 = 0.34f + num * 0.035f;
		float num3 = 0.54f + num * 0.035f;
		combatStats.maxHealth = boss.stats.maxHealth * num2;
		combatStats.attackPower = boss.stats.attackPower * num3;
		combatStats.criticalChance = Mathf.Clamp01(boss.stats.criticalChance * 0.8f + num * 0.01f);
		combatStats.criticalDamageMultiplier = Mathf.Max(1.45f, boss.stats.criticalDamageMultiplier * 0.88f);
		combatStats.attackSpeed = Mathf.Max(0.55f, boss.stats.attackSpeed * 0.9f);
		combatStats.maxMana = 105f + num * 8f;
		combatStats.attackRange = Mathf.Max(1.45f, boss.stats.attackRange * 0.9f);
		combatStats.moveSpeed = Mathf.Max(0.72f, boss.stats.moveSpeed * 0.92f);
		combatStats.projectileSpeed = boss.stats.projectileSpeed;
		combatStats.manaRegenPerSecondRate = 0.062f + num * 0.004f;
		combatStats.manaGainWhenHitRate = 0.12f;
		combatStats.manaGainPerAttackRate = 0.16f;
		switch (role)
		{
		case MonsterRole.Brute:
			combatStats.maxHealth *= 1.18f;
			combatStats.moveSpeed *= 0.92f;
			break;
		case MonsterRole.Caster:
			combatStats.attackRange += 0.45f;
			combatStats.manaRegenPerSecondRate += 0.008f;
			break;
		case MonsterRole.Charger:
			combatStats.moveSpeed *= 1.12f;
			combatStats.attackSpeed *= 1.06f;
			break;
		case MonsterRole.Elite:
			combatStats.attackPower *= 1.08f;
			combatStats.criticalChance = Mathf.Clamp01(combatStats.criticalChance + 0.05f);
			break;
		}
		return combatStats;
	}

	private void ApplyDefinitionOverrides()
	{
		ApplyOverrides(monsters);
		ApplyOverrides(midBosses);
		ApplyOverrides(bosses);
	}

	private void ApplyOverrides(List<MonsterDefinition> definitions)
	{
		for (int i = 0; i < definitions.Count; i++)
		{
			if ((Object)(object)presentationConfig != (Object)null)
			{
				presentationConfig.ApplyToMonster(definitions[i]);
			}
			if ((Object)(object)combatTuningConfig != (Object)null)
			{
				combatTuningConfig.ApplyToMonster(definitions[i]);
			}
		}
	}
}
