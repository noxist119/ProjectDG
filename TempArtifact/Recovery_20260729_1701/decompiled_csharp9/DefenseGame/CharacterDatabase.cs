using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenseGame
{
	public class CharacterDatabase : MonoBehaviour
	{
		[Serializable]
		private class SummonGradeRateMilestone
		{
			public int round = 1;

			[Range(0f, 1f)]
			public float normalChance = 0.62f;

			[Range(0f, 1f)]
			public float rareChance = 0.24f;

			[Range(0f, 1f)]
			public float epicChance = 0.1f;

			[Range(0f, 1f)]
			public float legendaryChance = 0.035f;

			[Range(0f, 1f)]
			public float mythicChance = 0.005f;

			public SummonGradeRateMilestone(int round, float normalChance, float rareChance, float epicChance, float legendaryChance, float mythicChance)
			{
				this.round = round;
				this.normalChance = normalChance;
				this.rareChance = rareChance;
				this.epicChance = epicChance;
				this.legendaryChance = legendaryChance;
				this.mythicChance = mythicChance;
			}

			public SummonGradeRates ToRates()
			{
				return new SummonGradeRates(Mathf.Max(0f, normalChance), Mathf.Max(0f, rareChance), Mathf.Max(0f, epicChance), Mathf.Max(0f, legendaryChance), Mathf.Max(0f, mythicChance));
			}
		}

		private struct SummonGradeRates
		{
			public float normal;

			public float rare;

			public float epic;

			public float legendary;

			public float mythic;

			public SummonGradeRates(float normal, float rare, float epic, float legendary, float mythic)
			{
				this.normal = normal;
				this.rare = rare;
				this.epic = epic;
				this.legendary = legendary;
				this.mythic = mythic;
			}
		}

		[SerializeField]
		private List<CharacterDefinition> characters = new List<CharacterDefinition>();

		[SerializeField]
		private bool generateStarterCharacters = true;

		[SerializeField]
		private int starterCharacterCount = 54;

		[SerializeField]
		private GamePresentationConfig presentationConfig;

		[SerializeField]
		private CharacterCombatTuningConfig combatTuningConfig;

		[SerializeField]
		private List<SummonGradeRateMilestone> summonGradeRateMilestones = new List<SummonGradeRateMilestone>
		{
			new SummonGradeRateMilestone(1, 0.96f, 0.04f, 0f, 0f, 0f),
			new SummonGradeRateMilestone(3, 0.94f, 0.06f, 0f, 0f, 0f),
			new SummonGradeRateMilestone(5, 0.91f, 0.085f, 0.005f, 0f, 0f),
			new SummonGradeRateMilestone(7, 0.88f, 0.105f, 0.015f, 0f, 0f),
			new SummonGradeRateMilestone(9, 0.84f, 0.13f, 0.028f, 0.002f, 0f),
			new SummonGradeRateMilestone(11, 0.8f, 0.155f, 0.04f, 0.005f, 0f),
			new SummonGradeRateMilestone(13, 0.76f, 0.18f, 0.052f, 0.008f, 0f),
			new SummonGradeRateMilestone(16, 0.7f, 0.215f, 0.07f, 0.015f, 0f),
			new SummonGradeRateMilestone(19, 0.65f, 0.24f, 0.087f, 0.022f, 0.001f),
			new SummonGradeRateMilestone(22, 0.61f, 0.26f, 0.1f, 0.027f, 0.003f),
			new SummonGradeRateMilestone(25, 0.57f, 0.275f, 0.115f, 0.035f, 0.005f),
			new SummonGradeRateMilestone(28, 0.54f, 0.29f, 0.125f, 0.038f, 0.007f),
			new SummonGradeRateMilestone(31, 0.51f, 0.305f, 0.135f, 0.041f, 0.009f),
			new SummonGradeRateMilestone(34, 0.485f, 0.315f, 0.145f, 0.044f, 0.011f),
			new SummonGradeRateMilestone(37, 0.46f, 0.325f, 0.15f, 0.052f, 0.013f),
			new SummonGradeRateMilestone(40, 0.435f, 0.335f, 0.155f, 0.06f, 0.015f),
			new SummonGradeRateMilestone(43, 0.41f, 0.345f, 0.16f, 0.068f, 0.017f),
			new SummonGradeRateMilestone(46, 0.39f, 0.35f, 0.165f, 0.075f, 0.02f),
			new SummonGradeRateMilestone(49, 0.37f, 0.355f, 0.17f, 0.082f, 0.023f)
		};

		private const int NormalHeroMax = 5;

		private const int RareHeroMax = 10;

		private const int EpicHeroMax = 20;

		private const int LegendaryHeroMax = 30;

		private const int MythicHeroMax = 50;

		private const int TranscendentHeroMax = 100;

		private static readonly string[] NormalNames = new string[12]
		{
			"Stone Guard", "Wind Archer", "Copper Gunner", "Lantern Mage", "Oak Fighter", "Wave Scout", "Dust Spear", "Iron Brawler", "Torch Adept", "Field Medic",
			"Mist Hunter", "Hammer Kid"
		};

		private static readonly string[] RareNames = new string[8] { "Azure Ranger", "Ruby Caster", "Verdant Monk", "Storm Javelin", "Moon Shot", "Steel Captain", "Blaze Tactician", "Echo Dancer" };

		private static readonly string[] EpicNames = new string[6] { "Frost Oracle", "Thunder Duelist", "Bloom Witch", "Sand Reaper", "Nova Mechanic", "Tide Caller" };

		private static readonly string[] LegendaryNames = new string[3] { "Solar Marshal", "Void Huntress", "Abyss Engineer" };

		private static readonly string[] MythicNames = new string[1] { "Celestial Sovereign" };

		private static readonly string[] TranscendentNames = new string[3] { "Origin Crown", "Eclipse Architect", "Infinity Dragon" };

		public IReadOnlyList<CharacterDefinition> Characters => characters;

		private void Awake()
		{
			if (generateStarterCharacters && characters.Count == 0)
			{
				GenerateStarterCharacters(ResolveStarterGenerationCount());
			}
			else
			{
				ApplyDefinitionOverrides();
			}
		}

		public void ApplyPresentationConfig(GamePresentationConfig config)
		{
			presentationConfig = config;
			EnsureCapacityForPresentationConfig(config);
			ApplyDefinitionOverrides();
		}

		public void ApplyCombatTuningConfig(CharacterCombatTuningConfig config)
		{
			combatTuningConfig = config;
			if (generateStarterCharacters)
			{
				RefreshGeneratedCharactersFromConfigs();
			}
			else
			{
				ApplyDefinitionOverrides();
			}
		}

		public void RefreshGeneratedCharactersFromConfigs()
		{
			if (generateStarterCharacters)
			{
				GenerateStarterCharacters(ResolveStarterGenerationCount());
			}
			else
			{
				ApplyDefinitionOverrides();
			}
		}

		public void ExpandGeneratedCharacterContent(int additionalCount)
		{
			if (!generateStarterCharacters)
			{
				ApplyDefinitionOverrides();
				return;
			}
			int currentHighest = Mathf.Max(GetHighestGeneratedCharacterIndex(), characters.Count);
			int requestedCount = currentHighest + Mathf.Max(0, additionalCount);
			GenerateStarterCharacters(Mathf.Max(ResolveStarterGenerationCount(), requestedCount));
		}

		private int ResolveStarterGenerationCount()
		{
			int count = Mathf.Max(1, starterCharacterCount);
			if (presentationConfig != null)
			{
				count = Mathf.Max(count, presentationConfig.GetHighestConfiguredCharacterIndex());
			}
			if (combatTuningConfig != null)
			{
				count = Mathf.Max(count, combatTuningConfig.GetHighestConfiguredCharacterIndex());
			}
			return count;
		}

		public void GenerateStarterCharacters(int totalCount)
		{
			characters.Clear();
			List<CharacterDefinition> roster = new List<CharacterDefinition>();
			BuildRoster(roster, NormalNames, CharacterGrade.Normal, 0);
			BuildRoster(roster, RareNames, CharacterGrade.Rare, NormalNames.Length);
			BuildRoster(roster, EpicNames, CharacterGrade.Epic, NormalNames.Length + RareNames.Length);
			BuildRoster(roster, LegendaryNames, CharacterGrade.Legendary, NormalNames.Length + RareNames.Length + EpicNames.Length);
			BuildRoster(roster, MythicNames, CharacterGrade.Mythic, NormalNames.Length + RareNames.Length + EpicNames.Length + LegendaryNames.Length);
			BuildRoster(roster, TranscendentNames, CharacterGrade.Transcendent, NormalNames.Length + RareNames.Length + EpicNames.Length + LegendaryNames.Length + MythicNames.Length);
			characters.AddRange(roster.Take(Mathf.Max(1, totalCount)));
			ApplyCanonicalHeroGradeOverrides();
			if (totalCount > roster.Count)
			{
				for (int i = roster.Count; i < totalCount; i++)
				{
					CharacterGrade grade = ResolveStarterGrade(i, totalCount);
					characters.Add(CreateDefinition($"Hero {i + 1:D2}", grade, i));
				}
			}
			ApplyDefinitionOverrides();
		}

		private void EnsureCapacityForPresentationConfig(GamePresentationConfig config)
		{
			if (generateStarterCharacters && !(config == null))
			{
				int configuredCharacterCount = ResolveStarterGenerationCount();
				if (configuredCharacterCount > GetHighestGeneratedCharacterIndex())
				{
					GenerateStarterCharacters(configuredCharacterCount);
				}
			}
		}

		public List<CharacterDefinition> GetCharactersByGrade(CharacterGrade grade, bool deployableOnly = false)
		{
			return characters.Where((CharacterDefinition c) => c != null && c.grade == grade && (!deployableOnly || IsDeployable(c))).ToList();
		}

		public List<CharacterDefinition> GetDeployableCharacters()
		{
			return characters.Where((CharacterDefinition c) => c != null && IsDeployable(c)).ToList();
		}

		public CharacterDefinition GetCharacterById(string characterId)
		{
			if (string.IsNullOrWhiteSpace(characterId))
			{
				return null;
			}
			return characters.FirstOrDefault((CharacterDefinition character) => character != null && character.id == characterId);
		}

		public CharacterDefinition GetRandomCharacterByIds(IEnumerable<string> characterIds, bool deployableOnly = false)
		{
			if (characterIds == null)
			{
				return null;
			}
			List<CharacterDefinition> candidates = (from character in characterIds.Select(GetCharacterById)
				where character != null && (!deployableOnly || IsDeployable(character))
				select character).ToList();
			if (candidates.Count == 0)
			{
				return null;
			}
			return candidates[UnityEngine.Random.Range(0, candidates.Count)];
		}

		public CharacterDefinition GetRandomCharacterByGrade(CharacterGrade grade, bool deployableOnly = false)
		{
			List<CharacterDefinition> candidates = GetCharactersByGrade(grade, deployableOnly);
			if (candidates.Count == 0)
			{
				return null;
			}
			return candidates[UnityEngine.Random.Range(0, candidates.Count)];
		}

		public CharacterDefinition GetRandomCharacterByGradeOrLower(CharacterGrade grade, bool deployableOnly = false)
		{
			for (int gradeIndex = (int)grade; gradeIndex >= 0; gradeIndex--)
			{
				CharacterDefinition candidate = GetRandomCharacterByGrade((CharacterGrade)gradeIndex, deployableOnly);
				if (candidate != null)
				{
					return candidate;
				}
			}
			return null;
		}

		public CharacterDefinition GetRandomSummonableCharacter(bool deployableOnly = false)
		{
			return GetRandomSummonableCharacter(1, deployableOnly);
		}

		public CharacterDefinition GetRandomSummonableCharacter(int currentRound, bool deployableOnly = false)
		{
			CharacterGrade grade = RollSummonGradeForRound(currentRound);
			CharacterDefinition selected = GetRandomCharacterByGradeOrLower(grade, deployableOnly);
			if (selected != null || !deployableOnly)
			{
				return selected;
			}
			List<CharacterDefinition> fallback = GetDeployableCharacters();
			return (fallback.Count > 0) ? fallback[UnityEngine.Random.Range(0, fallback.Count)] : null;
		}

		private CharacterGrade RollSummonGradeForRound(int currentRound)
		{
			SummonGradeRates rates = ResolveSummonGradeRates(currentRound);
			float total = rates.normal + rates.rare + rates.epic + rates.legendary + rates.mythic;
			if (total <= 0.0001f)
			{
				return CharacterGrade.Normal;
			}
			float roll = UnityEngine.Random.value * total;
			if (roll < rates.normal)
			{
				return CharacterGrade.Normal;
			}
			roll -= rates.normal;
			if (roll < rates.rare)
			{
				return CharacterGrade.Rare;
			}
			roll -= rates.rare;
			if (roll < rates.epic)
			{
				return CharacterGrade.Epic;
			}
			roll -= rates.epic;
			if (roll < rates.legendary)
			{
				return CharacterGrade.Legendary;
			}
			return CharacterGrade.Mythic;
		}

		private SummonGradeRates ResolveSummonGradeRates(int currentRound)
		{
			int round = Mathf.Max(1, currentRound);
			if (summonGradeRateMilestones == null || summonGradeRateMilestones.Count == 0)
			{
				return ApplyDailyFortuneSummonRates(new SummonGradeRates(0.96f, 0.04f, 0f, 0f, 0f), round);
			}
			SummonGradeRateMilestone selected = null;
			SummonGradeRateMilestone earliest = null;
			for (int i = 0; i < summonGradeRateMilestones.Count; i++)
			{
				SummonGradeRateMilestone milestone = summonGradeRateMilestones[i];
				if (milestone != null)
				{
					if (earliest == null || milestone.round < earliest.round)
					{
						earliest = milestone;
					}
					if (milestone.round <= round && (selected == null || milestone.round > selected.round))
					{
						selected = milestone;
					}
				}
			}
			SummonGradeRateMilestone resolved = selected ?? earliest;
			if (resolved == null)
			{
				return ApplyDailyFortuneSummonRates(new SummonGradeRates(0.96f, 0.04f, 0f, 0f, 0f), round);
			}
			return ApplyDailyFortuneSummonRates(resolved.ToRates(), round);
		}

		private SummonGradeRates ApplyDailyFortuneSummonRates(SummonGradeRates rates, int currentRound)
		{
			DailyFortuneRule fortune = DailyFortuneSystem.Today;
			float epicBonus = ((fortune != null) ? Mathf.Max(0f, fortune.epicSummonChanceBonus) : 0f);
			if (currentRound <= 4)
			{
				epicBonus = 0f;
			}
			else if (currentRound <= 8)
			{
				epicBonus = Mathf.Min(epicBonus, 0.005f);
			}
			else if (currentRound <= 12)
			{
				epicBonus = Mathf.Min(epicBonus, 0.015f);
			}
			if (epicBonus <= 0f)
			{
				return rates;
			}
			float remainingBonus = epicBonus;
			float fromNormal = Mathf.Min(rates.normal, remainingBonus);
			rates.normal -= fromNormal;
			rates.epic += fromNormal;
			remainingBonus -= fromNormal;
			if (remainingBonus > 0f)
			{
				float fromRare = Mathf.Min(rates.rare, remainingBonus);
				rates.rare -= fromRare;
				rates.epic += fromRare;
			}
			return rates;
		}

		private static bool IsDeployable(CharacterDefinition character)
		{
			return OutgameProgressionSystem.Active == null || OutgameProgressionSystem.Active.CanDeployCharacter(character);
		}

		private void ApplyCanonicalHeroGradeOverrides()
		{
			for (int i = 0; i < characters.Count; i++)
			{
				CharacterDefinition definition = characters[i];
				if (definition != null && TryResolveCanonicalHeroGrade(definition.id, out var grade))
				{
					definition.grade = grade;
					definition.mergeValue = (int)(1 + grade);
				}
			}
		}

		private void ApplyCanonicalHeroBalance(CharacterDefinition definition)
		{
			if (definition != null && TryResolveCanonicalHeroGrade(definition.id, out var grade))
			{
				int parsedSeed;
				int seed = (TryParseHeroSeed(definition.id, out parsedSeed) ? parsedSeed : 0);
				definition.grade = grade;
				definition.mergeValue = (int)(1 + grade);
				definition.stats = BuildStats(definition.grade, definition.role, seed);
				definition.tags = CharacterTagUtility.BuildDefaultTags(definition.role, seed, definition.grade);
				definition.accentColor = ResolveColor(definition.grade, definition.role);
				definition.description = BuildDescription(definition.displayName, definition.grade, definition.role);
				ApplySignatureTranscendentBalance(definition);
			}
		}

		private void ApplySignatureTranscendentBalance(CharacterDefinition definition)
		{
			if (definition != null && definition.stats != null)
			{
				switch (definition.id)
				{
				case "hero_05":
					definition.stats.maxHealth = 108f;
					definition.stats.attackPower = 11f;
					definition.stats.attackSpeed = 1f;
					definition.stats.maxMana = 100f;
					definition.stats.manaRegenPerSecondRate = 0.05f;
					definition.stats.manaGainWhenHitRate = 0.1f;
					definition.stats.manaGainPerAttackRate = 0.15f;
					definition.stats.attackRange = 2.8f;
					break;
				case "hero_32":
					definition.displayName = "Dire Wolf";
					definition.description = "원거리 야성형 신화. 야성의 추적탄으로 공격력 220% 피해, 4초간 35% 둔화·공격력 30%/초 중독을 주고 5초간 공격속도가 25% 증가합니다.";
					break;
				case "hero_55":
					definition.displayName = "Dice Armor";
					definition.description = "근거리 방어형 초월. 철벽 돌진으로 적을 밀어내며 스킬을 쓸수록 최대 40%까지 단단해집니다.";
					definition.stats.maxHealth = 650f;
					definition.stats.attackPower = 54f;
					definition.stats.criticalChance = 0.2f;
					definition.stats.criticalDamageMultiplier = 2f;
					definition.stats.attackSpeed = 1.05f;
					definition.stats.maxMana = 120f;
					definition.stats.manaRegenPerSecondRate = 0.065f;
					definition.stats.manaGainWhenHitRate = 0.16f;
					definition.stats.manaGainPerAttackRate = 0.18f;
					definition.stats.attackRange = 2.8f;
					definition.stats.projectileSpeed = 0f;
					break;
				case "hero_56":
					definition.displayName = "Dice Auto";
					definition.description = "원거리 교대 폭격형 초월. 소환 다음 라운드부터 기동과 휴식을 번갈아 반복하며 기동 시 420% 광역 폭격을 가합니다.";
					definition.stats.maxHealth = 390f;
					definition.stats.attackPower = 82f;
					definition.stats.criticalChance = 0.2f;
					definition.stats.criticalDamageMultiplier = 2.1f;
					definition.stats.attackSpeed = 1f;
					definition.stats.maxMana = 100f;
					definition.stats.manaRegenPerSecondRate = 0f;
					definition.stats.manaGainWhenHitRate = 0f;
					definition.stats.manaGainPerAttackRate = 0f;
					definition.stats.attackRange = 9.5f;
					definition.stats.projectileSpeed = 22f;
					break;
				case "hero_57":
					definition.displayName = "Dice Broken";
					definition.description = "원거리 변칙 사격형 초월. 기본 공격과 120%×5 난사가 무작위 적을 노리며 같은 적에게 탄환이 중복될 수 있습니다.";
					definition.stats.maxHealth = 410f;
					definition.stats.attackPower = 64f;
					definition.stats.criticalChance = 0.25f;
					definition.stats.criticalDamageMultiplier = 2.15f;
					definition.stats.attackSpeed = 1.5f;
					definition.stats.maxMana = 120f;
					definition.stats.manaRegenPerSecondRate = 0.06f;
					definition.stats.manaGainWhenHitRate = 0.08f;
					definition.stats.manaGainPerAttackRate = 0.15f;
					definition.stats.attackRange = 9f;
					definition.stats.projectileSpeed = 22f;
					break;
				}
			}
		}

		private bool TryResolveCanonicalHeroGrade(string characterId, out CharacterGrade grade)
		{
			grade = CharacterGrade.Normal;
			if (!TryParseHeroSeed(characterId, out var seed))
			{
				return false;
			}
			int heroNumber = seed + 1;
			if (heroNumber <= 5)
			{
				grade = CharacterGrade.Normal;
			}
			else if (heroNumber <= 10)
			{
				grade = CharacterGrade.Rare;
			}
			else if (heroNumber <= 20)
			{
				grade = CharacterGrade.Epic;
			}
			else if (heroNumber <= 30)
			{
				grade = CharacterGrade.Legendary;
			}
			else if (heroNumber <= 50)
			{
				grade = CharacterGrade.Mythic;
			}
			else
			{
				if (heroNumber > 100)
				{
					return false;
				}
				grade = CharacterGrade.Transcendent;
			}
			return true;
		}

		private int GetHighestGeneratedCharacterIndex()
		{
			int highestIndex = 0;
			for (int i = 0; i < characters.Count; i++)
			{
				CharacterDefinition definition = characters[i];
				if (definition != null && TryParseHeroSeed(definition.id, out var seed))
				{
					highestIndex = Mathf.Max(highestIndex, seed + 1);
				}
			}
			return highestIndex;
		}

		private bool TryParseHeroSeed(string characterId, out int seed)
		{
			seed = -1;
			if (string.IsNullOrWhiteSpace(characterId))
			{
				return false;
			}
			string[] parts = characterId.Split('_');
			if (parts.Length == 0 || !int.TryParse(parts[^1], out var parsed))
			{
				return false;
			}
			seed = parsed - 1;
			return seed >= 0;
		}

		private void BuildRoster(List<CharacterDefinition> roster, string[] names, CharacterGrade grade, int offset)
		{
			for (int i = 0; i < names.Length; i++)
			{
				roster.Add(CreateDefinition(names[i], grade, offset + i));
			}
		}

		private CharacterDefinition CreateDefinition(string name, CharacterGrade grade, int seed)
		{
			string id = $"hero_{seed + 1:D2}";
			CharacterRole role = ((combatTuningConfig != null) ? combatTuningConfig.ResolveRole(id, ResolveRole(seed)) : ResolveRole(seed));
			CharacterDefinition definition = new CharacterDefinition();
			definition.id = id;
			definition.displayName = name;
			definition.description = BuildDescription(name, grade, role);
			definition.grade = grade;
			definition.role = role;
			definition.tags = CharacterTagUtility.BuildDefaultTags(role, seed, grade);
			definition.accentColor = ResolveColor(grade, role);
			definition.mergeValue = (int)(1 + grade);
			definition.stats = BuildStats(grade, role, seed);
			definition.skills = BuildSkills(definition.displayName, grade, role, seed);
			return definition;
		}

		private CombatStats BuildStats(CharacterGrade grade, CharacterRole role, int seed)
		{
			CombatStats stats = new CombatStats();
			stats.maxHealth = 85f + (float)grade * 40f + (float)seed * 1.5f;
			stats.attackPower = 9f + (float)grade * 6f + (float)(seed % 4);
			stats.criticalChance = Mathf.Clamp01(0.08f + (float)grade * 0.035f);
			stats.criticalDamageMultiplier = 1.5f + (float)grade * 0.1f;
			stats.attackSpeed = 1f + (float)grade * 0.1f;
			stats.maxMana = 100f + (float)grade * 18f;
			stats.attackRange = 4.75f + (float)grade * 0.3f + (float)(seed % 4) * 0.2f;
			stats.projectileSpeed = 11f + (float)grade * 1.5f;
			stats.moveSpeed = 0f;
			stats.manaRegenPerSecondRate = 0.05f;
			stats.manaGainWhenHitRate = 0.1f;
			stats.manaGainPerAttackRate = 0.15f;
			switch (role)
			{
			case CharacterRole.Vanguard:
				stats.maxHealth *= 1.35f;
				stats.attackRange = 2.35f + (float)(seed % 3) * 0.2f;
				stats.attackPower *= 1.1f;
				stats.manaGainWhenHitRate = 0.12f;
				stats.manaGainPerAttackRate = 0.16f;
				break;
			case CharacterRole.Ranger:
				stats.attackRange += 3.4f;
				stats.attackSpeed *= 1.1f;
				stats.projectileSpeed += 2f;
				stats.manaGainPerAttackRate = 0.17f;
				break;
			case CharacterRole.Mage:
				stats.attackPower *= 1.25f;
				stats.maxMana *= 1.2f;
				stats.attackRange += 1.7f;
				stats.manaRegenPerSecondRate = 0.06f;
				stats.manaGainPerAttackRate = 0.16f;
				break;
			case CharacterRole.Support:
				stats.maxHealth *= 1.1f;
				stats.attackSpeed *= 0.92f;
				stats.maxMana *= 1.35f;
				stats.attackRange += 1.2f;
				stats.manaRegenPerSecondRate = 0.07f;
				stats.manaGainWhenHitRate = 0.12f;
				stats.manaGainPerAttackRate = 0.14f;
				break;
			case CharacterRole.Assassin:
				stats.criticalChance += 0.14f;
				stats.criticalDamageMultiplier += 0.35f;
				stats.attackSpeed *= 1.2f;
				stats.maxHealth *= 0.85f;
				stats.attackRange = 3f + (float)(seed % 2) * 0.25f;
				stats.manaGainPerAttackRate = 0.18f;
				break;
			case CharacterRole.Summoner:
				stats.attackPower *= 0.95f;
				stats.maxMana *= 1.45f;
				stats.attackRange += 2.2f;
				stats.manaRegenPerSecondRate = 0.08f;
				stats.manaGainPerAttackRate = 0.14f;
				break;
			}
			stats.criticalChance = Mathf.Clamp01(stats.criticalChance);
			return stats;
		}

		private List<SkillDefinition> BuildSkills(string ownerName, CharacterGrade grade, CharacterRole role, int seed)
		{
			List<SkillEffectType> pool = BuildRoleSkillPool(role);
			int count = GradeRules.GetSkillCount(grade);
			List<SkillDefinition> result = new List<SkillDefinition>(count);
			for (int i = 0; i < count; i++)
			{
				SkillEffectType effectType = pool[(seed + i) % pool.Count];
				result.Add(CreateSkill(ownerName, role, effectType, i));
			}
			return result;
		}

		private List<SkillEffectType> BuildRoleSkillPool(CharacterRole role)
		{
			return role switch
			{
				CharacterRole.Vanguard => new List<SkillEffectType>
				{
					SkillEffectType.Taunt,
					SkillEffectType.DirectDamage,
					SkillEffectType.Stun,
					SkillEffectType.DefenseBuff,
					SkillEffectType.AreaDamage
				}, 
				CharacterRole.Ranger => new List<SkillEffectType>
				{
					SkillEffectType.MultiShot,
					SkillEffectType.Slow,
					SkillEffectType.Poison,
					SkillEffectType.AttackSpeedBoost,
					SkillEffectType.DirectDamage
				}, 
				CharacterRole.Mage => new List<SkillEffectType>
				{
					SkillEffectType.AreaDamage,
					SkillEffectType.GroundAreaDamage,
					SkillEffectType.Slow,
					SkillEffectType.Stun,
					SkillEffectType.ManaSurge
				}, 
				CharacterRole.Support => new List<SkillEffectType>
				{
					SkillEffectType.ShieldAlly,
					SkillEffectType.HealSelf,
					SkillEffectType.DefenseBuff,
					SkillEffectType.ManaSurge,
					SkillEffectType.AttackSpeedBoost,
					SkillEffectType.CriticalBoost
				}, 
				CharacterRole.Assassin => new List<SkillEffectType>
				{
					SkillEffectType.Execute,
					SkillEffectType.LifeSteal,
					SkillEffectType.Stun,
					SkillEffectType.CriticalBoost,
					SkillEffectType.DirectDamage
				}, 
				_ => new List<SkillEffectType>
				{
					SkillEffectType.SummonRush,
					SkillEffectType.Transform,
					SkillEffectType.ShieldAlly,
					SkillEffectType.ManaSurge,
					SkillEffectType.AreaDamage
				}, 
			};
		}

		private SkillDefinition CreateSkill(string ownerName, CharacterRole role, SkillEffectType effectType, int index)
		{
			SkillDefinition skill = new SkillDefinition();
			skill.id = $"{ownerName}_{effectType}_{index}";
			skill.effectType = effectType;
			skill.category = SkillDefinitionUtility.ResolveCategory(effectType);
			skill.manaThreshold = ((role == CharacterRole.Support) ? 80f : 100f);
			skill.hitCount = ((effectType != SkillEffectType.MultiShot) ? 1 : 3);
			skill.radius = 2.6f;
			skill.secondaryPower = 0.35f;
			switch (effectType)
			{
			case SkillEffectType.DirectDamage:
				skill.displayName = "Power Shot";
				skill.description = "Current target takes amplified burst damage.";
				skill.power = 1.85f;
				skill.cooldown = 5f;
				break;
			case SkillEffectType.AreaDamage:
				skill.displayName = "Burst Nova";
				skill.description = "Deals damage to nearby enemies around the impact area.";
				skill.power = 1.2f;
				skill.radius = 3.2f;
				skill.cooldown = 6f;
				break;
			case SkillEffectType.HealSelf:
				skill.displayName = "Battle Prayer";
				skill.description = "Recover a portion of max health.";
				skill.power = 0.3f;
				skill.cooldown = 8f;
				break;
			case SkillEffectType.AttackSpeedBoost:
				skill.displayName = "Rapid Tempo";
				skill.description = "Gain a temporary attack speed boost.";
				skill.power = 0.45f;
				skill.duration = 5f;
				skill.cooldown = 9f;
				break;
			case SkillEffectType.CriticalBoost:
				skill.displayName = "Predator Focus";
				skill.description = "Temporarily raises critical chance.";
				skill.power = 0.24f;
				skill.duration = 5f;
				skill.cooldown = 9f;
				break;
			case SkillEffectType.ManaSurge:
				skill.displayName = "Mana Current";
				skill.description = "Instantly recover mana to chain casts.";
				skill.power = 0.48f;
				skill.cooldown = 7f;
				break;
			case SkillEffectType.MultiShot:
				skill.displayName = "Arrow Bloom";
				skill.description = "Fire several rapid shots across multiple enemies.";
				skill.power = 0.8f;
				skill.hitCount = 3;
				skill.cooldown = 7f;
				break;
			case SkillEffectType.Execute:
				skill.displayName = "Execution Cut";
				skill.description = "Deals extra damage to low health enemies.";
				skill.power = 2.2f;
				skill.cooldown = 7f;
				break;
			case SkillEffectType.SummonRush:
				skill.displayName = "Spirit Guardian";
				skill.description = "Summons a fragile spirit ally into the monster lane.";
				skill.power = 0.25f;
				skill.secondaryPower = 0.25f;
				skill.hitCount = 1;
				skill.radius = 3.2f;
				skill.cooldown = 10f;
				break;
			case SkillEffectType.Slow:
				skill.displayName = "Frost Thread";
				skill.description = "Slows the target monster for a short time.";
				skill.power = 0.42f;
				skill.duration = 3.5f;
				skill.cooldown = 7f;
				break;
			case SkillEffectType.Stun:
				skill.displayName = "Impact Seal";
				skill.description = "Briefly stuns the target monster.";
				skill.power = 0f;
				skill.duration = 1.25f;
				skill.cooldown = 8f;
				break;
			case SkillEffectType.ShieldAlly:
				skill.displayName = "Guardian Veil";
				skill.description = "Grants a shield to the lowest health ally.";
				skill.power = 0.32f;
				skill.duration = 5f;
				skill.cooldown = 8f;
				skill.manaThreshold = 80f;
				break;
			case SkillEffectType.LifeSteal:
				skill.displayName = "Blood Recall";
				skill.description = "Deals damage and restores health from the damage dealt.";
				skill.power = 1.25f;
				skill.secondaryPower = 0.45f;
				skill.cooldown = 7f;
				break;
			case SkillEffectType.GroundAreaDamage:
				skill.displayName = "Arcane Field";
				skill.description = "Creates a damaging field that repeatedly hits monsters in the area.";
				skill.power = 0.34f;
				skill.secondaryPower = 0.55f;
				skill.radius = 3.4f;
				skill.duration = 3.2f;
				skill.cooldown = 8f;
				break;
			case SkillEffectType.Poison:
				skill.displayName = "Toxic Mark";
				skill.description = "Poisons the target, dealing damage over time.";
				skill.power = 0.28f;
				skill.secondaryPower = 0.8f;
				skill.duration = 4.2f;
				skill.cooldown = 7f;
				break;
			case SkillEffectType.DefenseBuff:
				skill.displayName = "Iron Oath";
				skill.description = "Grants a defensive shield to nearby allies.";
				skill.power = 0.22f;
				skill.radius = 4.2f;
				skill.duration = 5f;
				skill.cooldown = 8f;
				skill.manaThreshold = 85f;
				break;
			case SkillEffectType.Taunt:
				skill.displayName = "Challenge Roar";
				skill.description = "Taunts nearby monsters, forcing them to attack this unit.";
				skill.power = 0f;
				skill.radius = 3.8f;
				skill.duration = 3.5f;
				skill.cooldown = 8f;
				skill.manaThreshold = 90f;
				break;
			case SkillEffectType.Transform:
				skill.displayName = "Awakened Form";
				skill.description = "Transforms into an empowered combat state for a short time.";
				skill.power = 0.34f;
				skill.secondaryPower = 0.18f;
				skill.duration = 6f;
				skill.cooldown = 10f;
				break;
			default:
				skill.displayName = "Armor Rend";
				skill.description = "Crushes a target with a defense-breaking hit.";
				skill.power = 1.6f;
				skill.cooldown = 6f;
				break;
			}
			return skill;
		}

		private string BuildDescription(string name, CharacterGrade grade, CharacterRole role)
		{
			return name + " is a " + grade.ToString() + " " + role.ToString() + " who protects the last defense line.";
		}

		private CharacterRole ResolveRole(int seed)
		{
			return (seed % 6) switch
			{
				0 => CharacterRole.Vanguard, 
				1 => CharacterRole.Ranger, 
				2 => CharacterRole.Mage, 
				3 => CharacterRole.Support, 
				4 => CharacterRole.Assassin, 
				_ => CharacterRole.Summoner, 
			};
		}

		private Color ResolveColor(CharacterGrade grade, CharacterRole role)
		{
			Color baseColor = Color.white;
			switch (grade)
			{
			case CharacterGrade.Normal:
				baseColor = new Color(0.75f, 0.75f, 0.75f);
				break;
			case CharacterGrade.Rare:
				baseColor = new Color(0.35f, 0.7f, 1f);
				break;
			case CharacterGrade.Epic:
				baseColor = new Color(0.35f, 1f, 0.7f);
				break;
			case CharacterGrade.Legendary:
				baseColor = new Color(1f, 0.76f, 0.25f);
				break;
			case CharacterGrade.Mythic:
				baseColor = new Color(1f, 0.35f, 0.35f);
				break;
			case CharacterGrade.Transcendent:
				baseColor = new Color(0.92f, 0.42f, 1f);
				break;
			}
			if (role == CharacterRole.Assassin)
			{
				baseColor *= new Color(1.05f, 0.85f, 0.95f);
			}
			if (role == CharacterRole.Support)
			{
				baseColor *= new Color(0.9f, 1.05f, 1.05f);
			}
			return baseColor;
		}

		private CharacterGrade ResolveStarterGrade(int index, int totalCount)
		{
			float ratio = ((totalCount <= 1) ? 1f : ((float)index / (float)(totalCount - 1)));
			if (ratio < 0.4f)
			{
				return CharacterGrade.Normal;
			}
			if (ratio < 0.7f)
			{
				return CharacterGrade.Rare;
			}
			if (ratio < 0.88f)
			{
				return CharacterGrade.Epic;
			}
			if (ratio < 0.97f)
			{
				return CharacterGrade.Legendary;
			}
			return CharacterGrade.Mythic;
		}

		private void ApplyDefinitionOverrides()
		{
			ApplyCanonicalHeroGradeOverrides();
			for (int i = 0; i < characters.Count; i++)
			{
				if (presentationConfig != null)
				{
					presentationConfig.ApplyToCharacter(characters[i]);
				}
				if (combatTuningConfig != null)
				{
					combatTuningConfig.ApplyToCharacter(characters[i]);
				}
				ApplyCanonicalHeroBalance(characters[i]);
			}
			RemoveUnconfiguredGeneratedCharacters();
			SortCharactersByGrade();
		}

		private void RemoveUnconfiguredGeneratedCharacters()
		{
			if (generateStarterCharacters && (!(presentationConfig == null) || !(combatTuningConfig == null)))
			{
				characters = characters.Where(IsConfiguredGeneratedCharacter).ToList();
			}
		}

		private bool IsConfiguredGeneratedCharacter(CharacterDefinition definition)
		{
			if (definition == null)
			{
				return false;
			}
			bool hasPresentation = presentationConfig == null || definition.prefab != null;
			bool hasCombatTuning = combatTuningConfig == null || combatTuningConfig.HasExplicitEntry(definition.id);
			return hasPresentation && hasCombatTuning;
		}

		private void SortCharactersByGrade()
		{
			characters = (from character in characters
				where character != null
				orderby character.grade, TryParseHeroSeed(character.id, out var seed) ? seed : int.MaxValue, character.displayName
				select character).ToList();
		}
	}
}
