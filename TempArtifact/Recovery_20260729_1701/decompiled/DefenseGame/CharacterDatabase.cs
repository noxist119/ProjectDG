using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenseGame;

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

	private struct SummonGradeRates(float normal, float rare, float epic, float legendary, float mythic)
	{
		public float normal = normal;

		public float rare = rare;

		public float epic = epic;

		public float legendary = legendary;

		public float mythic = mythic;
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
		int num = Mathf.Max(GetHighestGeneratedCharacterIndex(), characters.Count);
		int num2 = num + Mathf.Max(0, additionalCount);
		GenerateStarterCharacters(Mathf.Max(ResolveStarterGenerationCount(), num2));
	}

	private int ResolveStarterGenerationCount()
	{
		int num = Mathf.Max(1, starterCharacterCount);
		if ((Object)(object)presentationConfig != (Object)null)
		{
			num = Mathf.Max(num, presentationConfig.GetHighestConfiguredCharacterIndex());
		}
		if ((Object)(object)combatTuningConfig != (Object)null)
		{
			num = Mathf.Max(num, combatTuningConfig.GetHighestConfiguredCharacterIndex());
		}
		return num;
	}

	public void GenerateStarterCharacters(int totalCount)
	{
		characters.Clear();
		List<CharacterDefinition> list = new List<CharacterDefinition>();
		BuildRoster(list, NormalNames, CharacterGrade.Normal, 0);
		BuildRoster(list, RareNames, CharacterGrade.Rare, NormalNames.Length);
		BuildRoster(list, EpicNames, CharacterGrade.Epic, NormalNames.Length + RareNames.Length);
		BuildRoster(list, LegendaryNames, CharacterGrade.Legendary, NormalNames.Length + RareNames.Length + EpicNames.Length);
		BuildRoster(list, MythicNames, CharacterGrade.Mythic, NormalNames.Length + RareNames.Length + EpicNames.Length + LegendaryNames.Length);
		BuildRoster(list, TranscendentNames, CharacterGrade.Transcendent, NormalNames.Length + RareNames.Length + EpicNames.Length + LegendaryNames.Length + MythicNames.Length);
		characters.AddRange(list.Take(Mathf.Max(1, totalCount)));
		ApplyCanonicalHeroGradeOverrides();
		if (totalCount > list.Count)
		{
			for (int i = list.Count; i < totalCount; i++)
			{
				CharacterGrade grade = ResolveStarterGrade(i, totalCount);
				characters.Add(CreateDefinition($"Hero {i + 1:D2}", grade, i));
			}
		}
		ApplyDefinitionOverrides();
	}

	private void EnsureCapacityForPresentationConfig(GamePresentationConfig config)
	{
		if (generateStarterCharacters && !((Object)(object)config == (Object)null))
		{
			int num = ResolveStarterGenerationCount();
			if (num > GetHighestGeneratedCharacterIndex())
			{
				GenerateStarterCharacters(num);
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
		List<CharacterDefinition> list = (from character in characterIds.Select(GetCharacterById)
			where character != null && (!deployableOnly || IsDeployable(character))
			select character).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		return list[Random.Range(0, list.Count)];
	}

	public CharacterDefinition GetRandomCharacterByGrade(CharacterGrade grade, bool deployableOnly = false)
	{
		List<CharacterDefinition> charactersByGrade = GetCharactersByGrade(grade, deployableOnly);
		if (charactersByGrade.Count == 0)
		{
			return null;
		}
		return charactersByGrade[Random.Range(0, charactersByGrade.Count)];
	}

	public CharacterDefinition GetRandomCharacterByGradeOrLower(CharacterGrade grade, bool deployableOnly = false)
	{
		for (int num = (int)grade; num >= 0; num--)
		{
			CharacterDefinition randomCharacterByGrade = GetRandomCharacterByGrade((CharacterGrade)num, deployableOnly);
			if (randomCharacterByGrade != null)
			{
				return randomCharacterByGrade;
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
		CharacterDefinition randomCharacterByGradeOrLower = GetRandomCharacterByGradeOrLower(grade, deployableOnly);
		if (randomCharacterByGradeOrLower != null || !deployableOnly)
		{
			return randomCharacterByGradeOrLower;
		}
		List<CharacterDefinition> deployableCharacters = GetDeployableCharacters();
		return (deployableCharacters.Count > 0) ? deployableCharacters[Random.Range(0, deployableCharacters.Count)] : null;
	}

	private CharacterGrade RollSummonGradeForRound(int currentRound)
	{
		SummonGradeRates summonGradeRates = ResolveSummonGradeRates(currentRound);
		float num = summonGradeRates.normal + summonGradeRates.rare + summonGradeRates.epic + summonGradeRates.legendary + summonGradeRates.mythic;
		if (num <= 0.0001f)
		{
			return CharacterGrade.Normal;
		}
		float num2 = Random.value * num;
		if (num2 < summonGradeRates.normal)
		{
			return CharacterGrade.Normal;
		}
		num2 -= summonGradeRates.normal;
		if (num2 < summonGradeRates.rare)
		{
			return CharacterGrade.Rare;
		}
		num2 -= summonGradeRates.rare;
		if (num2 < summonGradeRates.epic)
		{
			return CharacterGrade.Epic;
		}
		num2 -= summonGradeRates.epic;
		if (num2 < summonGradeRates.legendary)
		{
			return CharacterGrade.Legendary;
		}
		return CharacterGrade.Mythic;
	}

	private SummonGradeRates ResolveSummonGradeRates(int currentRound)
	{
		int num = Mathf.Max(1, currentRound);
		if (summonGradeRateMilestones == null || summonGradeRateMilestones.Count == 0)
		{
			return ApplyDailyFortuneSummonRates(new SummonGradeRates(0.96f, 0.04f, 0f, 0f, 0f), num);
		}
		SummonGradeRateMilestone summonGradeRateMilestone = null;
		SummonGradeRateMilestone summonGradeRateMilestone2 = null;
		for (int i = 0; i < summonGradeRateMilestones.Count; i++)
		{
			SummonGradeRateMilestone summonGradeRateMilestone3 = summonGradeRateMilestones[i];
			if (summonGradeRateMilestone3 != null)
			{
				if (summonGradeRateMilestone2 == null || summonGradeRateMilestone3.round < summonGradeRateMilestone2.round)
				{
					summonGradeRateMilestone2 = summonGradeRateMilestone3;
				}
				if (summonGradeRateMilestone3.round <= num && (summonGradeRateMilestone == null || summonGradeRateMilestone3.round > summonGradeRateMilestone.round))
				{
					summonGradeRateMilestone = summonGradeRateMilestone3;
				}
			}
		}
		SummonGradeRateMilestone summonGradeRateMilestone4 = summonGradeRateMilestone ?? summonGradeRateMilestone2;
		if (summonGradeRateMilestone4 == null)
		{
			return ApplyDailyFortuneSummonRates(new SummonGradeRates(0.96f, 0.04f, 0f, 0f, 0f), num);
		}
		return ApplyDailyFortuneSummonRates(summonGradeRateMilestone4.ToRates(), num);
	}

	private SummonGradeRates ApplyDailyFortuneSummonRates(SummonGradeRates rates, int currentRound)
	{
		DailyFortuneRule today = DailyFortuneSystem.Today;
		float num = ((today != null) ? Mathf.Max(0f, today.epicSummonChanceBonus) : 0f);
		if (currentRound <= 4)
		{
			num = 0f;
		}
		else if (currentRound <= 8)
		{
			num = Mathf.Min(num, 0.005f);
		}
		else if (currentRound <= 12)
		{
			num = Mathf.Min(num, 0.015f);
		}
		if (num <= 0f)
		{
			return rates;
		}
		float num2 = num;
		float num3 = Mathf.Min(rates.normal, num2);
		rates.normal -= num3;
		rates.epic += num3;
		num2 -= num3;
		if (num2 > 0f)
		{
			float num4 = Mathf.Min(rates.rare, num2);
			rates.rare -= num4;
			rates.epic += num4;
		}
		return rates;
	}

	private static bool IsDeployable(CharacterDefinition character)
	{
		return (Object)(object)OutgameProgressionSystem.Active == (Object)null || OutgameProgressionSystem.Active.CanDeployCharacter(character);
	}

	private void ApplyCanonicalHeroGradeOverrides()
	{
		for (int i = 0; i < characters.Count; i++)
		{
			CharacterDefinition characterDefinition = characters[i];
			if (characterDefinition != null && TryResolveCanonicalHeroGrade(characterDefinition.id, out var grade))
			{
				characterDefinition.grade = grade;
				characterDefinition.mergeValue = (int)(1 + grade);
			}
		}
	}

	private void ApplyCanonicalHeroBalance(CharacterDefinition definition)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		if (definition != null && TryResolveCanonicalHeroGrade(definition.id, out var grade))
		{
			int seed2;
			int seed = (TryParseHeroSeed(definition.id, out seed2) ? seed2 : 0);
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
		int num = seed + 1;
		if (num <= 5)
		{
			grade = CharacterGrade.Normal;
		}
		else if (num <= 10)
		{
			grade = CharacterGrade.Rare;
		}
		else if (num <= 20)
		{
			grade = CharacterGrade.Epic;
		}
		else if (num <= 30)
		{
			grade = CharacterGrade.Legendary;
		}
		else if (num <= 50)
		{
			grade = CharacterGrade.Mythic;
		}
		else
		{
			if (num > 100)
			{
				return false;
			}
			grade = CharacterGrade.Transcendent;
		}
		return true;
	}

	private int GetHighestGeneratedCharacterIndex()
	{
		int num = 0;
		for (int i = 0; i < characters.Count; i++)
		{
			CharacterDefinition characterDefinition = characters[i];
			if (characterDefinition != null && TryParseHeroSeed(characterDefinition.id, out var seed))
			{
				num = Mathf.Max(num, seed + 1);
			}
		}
		return num;
	}

	private bool TryParseHeroSeed(string characterId, out int seed)
	{
		seed = -1;
		if (string.IsNullOrWhiteSpace(characterId))
		{
			return false;
		}
		string[] array = characterId.Split('_');
		if (array.Length == 0 || !int.TryParse(array[^1], out var result))
		{
			return false;
		}
		seed = result - 1;
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
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		string text = $"hero_{seed + 1:D2}";
		CharacterRole role = (((Object)(object)combatTuningConfig != (Object)null) ? combatTuningConfig.ResolveRole(text, ResolveRole(seed)) : ResolveRole(seed));
		CharacterDefinition characterDefinition = new CharacterDefinition();
		characterDefinition.id = text;
		characterDefinition.displayName = name;
		characterDefinition.description = BuildDescription(name, grade, role);
		characterDefinition.grade = grade;
		characterDefinition.role = role;
		characterDefinition.tags = CharacterTagUtility.BuildDefaultTags(role, seed, grade);
		characterDefinition.accentColor = ResolveColor(grade, role);
		characterDefinition.mergeValue = (int)(1 + grade);
		characterDefinition.stats = BuildStats(grade, role, seed);
		characterDefinition.skills = BuildSkills(characterDefinition.displayName, grade, role, seed);
		return characterDefinition;
	}

	private CombatStats BuildStats(CharacterGrade grade, CharacterRole role, int seed)
	{
		CombatStats combatStats = new CombatStats();
		combatStats.maxHealth = 85f + (float)grade * 40f + (float)seed * 1.5f;
		combatStats.attackPower = 9f + (float)grade * 6f + (float)(seed % 4);
		combatStats.criticalChance = Mathf.Clamp01(0.08f + (float)grade * 0.035f);
		combatStats.criticalDamageMultiplier = 1.5f + (float)grade * 0.1f;
		combatStats.attackSpeed = 1f + (float)grade * 0.1f;
		combatStats.maxMana = 100f + (float)grade * 18f;
		combatStats.attackRange = 4.75f + (float)grade * 0.3f + (float)(seed % 4) * 0.2f;
		combatStats.projectileSpeed = 11f + (float)grade * 1.5f;
		combatStats.moveSpeed = 0f;
		combatStats.manaRegenPerSecondRate = 0.05f;
		combatStats.manaGainWhenHitRate = 0.1f;
		combatStats.manaGainPerAttackRate = 0.15f;
		switch (role)
		{
		case CharacterRole.Vanguard:
			combatStats.maxHealth *= 1.35f;
			combatStats.attackRange = 2.35f + (float)(seed % 3) * 0.2f;
			combatStats.attackPower *= 1.1f;
			combatStats.manaGainWhenHitRate = 0.12f;
			combatStats.manaGainPerAttackRate = 0.16f;
			break;
		case CharacterRole.Ranger:
			combatStats.attackRange += 3.4f;
			combatStats.attackSpeed *= 1.1f;
			combatStats.projectileSpeed += 2f;
			combatStats.manaGainPerAttackRate = 0.17f;
			break;
		case CharacterRole.Mage:
			combatStats.attackPower *= 1.25f;
			combatStats.maxMana *= 1.2f;
			combatStats.attackRange += 1.7f;
			combatStats.manaRegenPerSecondRate = 0.06f;
			combatStats.manaGainPerAttackRate = 0.16f;
			break;
		case CharacterRole.Support:
			combatStats.maxHealth *= 1.1f;
			combatStats.attackSpeed *= 0.92f;
			combatStats.maxMana *= 1.35f;
			combatStats.attackRange += 1.2f;
			combatStats.manaRegenPerSecondRate = 0.07f;
			combatStats.manaGainWhenHitRate = 0.12f;
			combatStats.manaGainPerAttackRate = 0.14f;
			break;
		case CharacterRole.Assassin:
			combatStats.criticalChance += 0.14f;
			combatStats.criticalDamageMultiplier += 0.35f;
			combatStats.attackSpeed *= 1.2f;
			combatStats.maxHealth *= 0.85f;
			combatStats.attackRange = 3f + (float)(seed % 2) * 0.25f;
			combatStats.manaGainPerAttackRate = 0.18f;
			break;
		case CharacterRole.Summoner:
			combatStats.attackPower *= 0.95f;
			combatStats.maxMana *= 1.45f;
			combatStats.attackRange += 2.2f;
			combatStats.manaRegenPerSecondRate = 0.08f;
			combatStats.manaGainPerAttackRate = 0.14f;
			break;
		}
		combatStats.criticalChance = Mathf.Clamp01(combatStats.criticalChance);
		return combatStats;
	}

	private List<SkillDefinition> BuildSkills(string ownerName, CharacterGrade grade, CharacterRole role, int seed)
	{
		List<SkillEffectType> list = BuildRoleSkillPool(role);
		int skillCount = GradeRules.GetSkillCount(grade);
		List<SkillDefinition> list2 = new List<SkillDefinition>(skillCount);
		for (int i = 0; i < skillCount; i++)
		{
			SkillEffectType effectType = list[(seed + i) % list.Count];
			list2.Add(CreateSkill(ownerName, role, effectType, i));
		}
		return list2;
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
		SkillDefinition skillDefinition = new SkillDefinition();
		skillDefinition.id = $"{ownerName}_{effectType}_{index}";
		skillDefinition.effectType = effectType;
		skillDefinition.category = SkillDefinitionUtility.ResolveCategory(effectType);
		skillDefinition.manaThreshold = ((role == CharacterRole.Support) ? 80f : 100f);
		skillDefinition.hitCount = ((effectType != SkillEffectType.MultiShot) ? 1 : 3);
		skillDefinition.radius = 2.6f;
		skillDefinition.secondaryPower = 0.35f;
		switch (effectType)
		{
		case SkillEffectType.DirectDamage:
			skillDefinition.displayName = "Power Shot";
			skillDefinition.description = "Current target takes amplified burst damage.";
			skillDefinition.power = 1.85f;
			skillDefinition.cooldown = 5f;
			break;
		case SkillEffectType.AreaDamage:
			skillDefinition.displayName = "Burst Nova";
			skillDefinition.description = "Deals damage to nearby enemies around the impact area.";
			skillDefinition.power = 1.2f;
			skillDefinition.radius = 3.2f;
			skillDefinition.cooldown = 6f;
			break;
		case SkillEffectType.HealSelf:
			skillDefinition.displayName = "Battle Prayer";
			skillDefinition.description = "Recover a portion of max health.";
			skillDefinition.power = 0.3f;
			skillDefinition.cooldown = 8f;
			break;
		case SkillEffectType.AttackSpeedBoost:
			skillDefinition.displayName = "Rapid Tempo";
			skillDefinition.description = "Gain a temporary attack speed boost.";
			skillDefinition.power = 0.45f;
			skillDefinition.duration = 5f;
			skillDefinition.cooldown = 9f;
			break;
		case SkillEffectType.CriticalBoost:
			skillDefinition.displayName = "Predator Focus";
			skillDefinition.description = "Temporarily raises critical chance.";
			skillDefinition.power = 0.24f;
			skillDefinition.duration = 5f;
			skillDefinition.cooldown = 9f;
			break;
		case SkillEffectType.ManaSurge:
			skillDefinition.displayName = "Mana Current";
			skillDefinition.description = "Instantly recover mana to chain casts.";
			skillDefinition.power = 0.48f;
			skillDefinition.cooldown = 7f;
			break;
		case SkillEffectType.MultiShot:
			skillDefinition.displayName = "Arrow Bloom";
			skillDefinition.description = "Fire several rapid shots across multiple enemies.";
			skillDefinition.power = 0.8f;
			skillDefinition.hitCount = 3;
			skillDefinition.cooldown = 7f;
			break;
		case SkillEffectType.Execute:
			skillDefinition.displayName = "Execution Cut";
			skillDefinition.description = "Deals extra damage to low health enemies.";
			skillDefinition.power = 2.2f;
			skillDefinition.cooldown = 7f;
			break;
		case SkillEffectType.SummonRush:
			skillDefinition.displayName = "Spirit Guardian";
			skillDefinition.description = "Summons a fragile spirit ally into the monster lane.";
			skillDefinition.power = 0.25f;
			skillDefinition.secondaryPower = 0.25f;
			skillDefinition.hitCount = 1;
			skillDefinition.radius = 3.2f;
			skillDefinition.cooldown = 10f;
			break;
		case SkillEffectType.Slow:
			skillDefinition.displayName = "Frost Thread";
			skillDefinition.description = "Slows the target monster for a short time.";
			skillDefinition.power = 0.42f;
			skillDefinition.duration = 3.5f;
			skillDefinition.cooldown = 7f;
			break;
		case SkillEffectType.Stun:
			skillDefinition.displayName = "Impact Seal";
			skillDefinition.description = "Briefly stuns the target monster.";
			skillDefinition.power = 0f;
			skillDefinition.duration = 1.25f;
			skillDefinition.cooldown = 8f;
			break;
		case SkillEffectType.ShieldAlly:
			skillDefinition.displayName = "Guardian Veil";
			skillDefinition.description = "Grants a shield to the lowest health ally.";
			skillDefinition.power = 0.32f;
			skillDefinition.duration = 5f;
			skillDefinition.cooldown = 8f;
			skillDefinition.manaThreshold = 80f;
			break;
		case SkillEffectType.LifeSteal:
			skillDefinition.displayName = "Blood Recall";
			skillDefinition.description = "Deals damage and restores health from the damage dealt.";
			skillDefinition.power = 1.25f;
			skillDefinition.secondaryPower = 0.45f;
			skillDefinition.cooldown = 7f;
			break;
		case SkillEffectType.GroundAreaDamage:
			skillDefinition.displayName = "Arcane Field";
			skillDefinition.description = "Creates a damaging field that repeatedly hits monsters in the area.";
			skillDefinition.power = 0.34f;
			skillDefinition.secondaryPower = 0.55f;
			skillDefinition.radius = 3.4f;
			skillDefinition.duration = 3.2f;
			skillDefinition.cooldown = 8f;
			break;
		case SkillEffectType.Poison:
			skillDefinition.displayName = "Toxic Mark";
			skillDefinition.description = "Poisons the target, dealing damage over time.";
			skillDefinition.power = 0.28f;
			skillDefinition.secondaryPower = 0.8f;
			skillDefinition.duration = 4.2f;
			skillDefinition.cooldown = 7f;
			break;
		case SkillEffectType.DefenseBuff:
			skillDefinition.displayName = "Iron Oath";
			skillDefinition.description = "Grants a defensive shield to nearby allies.";
			skillDefinition.power = 0.22f;
			skillDefinition.radius = 4.2f;
			skillDefinition.duration = 5f;
			skillDefinition.cooldown = 8f;
			skillDefinition.manaThreshold = 85f;
			break;
		case SkillEffectType.Taunt:
			skillDefinition.displayName = "Challenge Roar";
			skillDefinition.description = "Taunts nearby monsters, forcing them to attack this unit.";
			skillDefinition.power = 0f;
			skillDefinition.radius = 3.8f;
			skillDefinition.duration = 3.5f;
			skillDefinition.cooldown = 8f;
			skillDefinition.manaThreshold = 90f;
			break;
		case SkillEffectType.Transform:
			skillDefinition.displayName = "Awakened Form";
			skillDefinition.description = "Transforms into an empowered combat state for a short time.";
			skillDefinition.power = 0.34f;
			skillDefinition.secondaryPower = 0.18f;
			skillDefinition.duration = 6f;
			skillDefinition.cooldown = 10f;
			break;
		default:
			skillDefinition.displayName = "Armor Rend";
			skillDefinition.description = "Crushes a target with a defense-breaking hit.";
			skillDefinition.power = 1.6f;
			skillDefinition.cooldown = 6f;
			break;
		}
		return skillDefinition;
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
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		Color val = Color.white;
		switch (grade)
		{
		case CharacterGrade.Normal:
			((Color)(ref val))._002Ector(0.75f, 0.75f, 0.75f);
			break;
		case CharacterGrade.Rare:
			((Color)(ref val))._002Ector(0.35f, 0.7f, 1f);
			break;
		case CharacterGrade.Epic:
			((Color)(ref val))._002Ector(0.35f, 1f, 0.7f);
			break;
		case CharacterGrade.Legendary:
			((Color)(ref val))._002Ector(1f, 0.76f, 0.25f);
			break;
		case CharacterGrade.Mythic:
			((Color)(ref val))._002Ector(1f, 0.35f, 0.35f);
			break;
		case CharacterGrade.Transcendent:
			((Color)(ref val))._002Ector(0.92f, 0.42f, 1f);
			break;
		}
		if (role == CharacterRole.Assassin)
		{
			val *= new Color(1.05f, 0.85f, 0.95f);
		}
		if (role == CharacterRole.Support)
		{
			val *= new Color(0.9f, 1.05f, 1.05f);
		}
		return val;
	}

	private CharacterGrade ResolveStarterGrade(int index, int totalCount)
	{
		float num = ((totalCount <= 1) ? 1f : ((float)index / (float)(totalCount - 1)));
		if (num < 0.4f)
		{
			return CharacterGrade.Normal;
		}
		if (num < 0.7f)
		{
			return CharacterGrade.Rare;
		}
		if (num < 0.88f)
		{
			return CharacterGrade.Epic;
		}
		if (num < 0.97f)
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
			if ((Object)(object)presentationConfig != (Object)null)
			{
				presentationConfig.ApplyToCharacter(characters[i]);
			}
			if ((Object)(object)combatTuningConfig != (Object)null)
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
		if (generateStarterCharacters && (!((Object)(object)presentationConfig == (Object)null) || !((Object)(object)combatTuningConfig == (Object)null)))
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
		bool flag = (Object)(object)presentationConfig == (Object)null || (Object)(object)definition.prefab != (Object)null;
		bool flag2 = (Object)(object)combatTuningConfig == (Object)null || combatTuningConfig.HasExplicitEntry(definition.id);
		return flag && flag2;
	}

	private void SortCharactersByGrade()
	{
		characters = (from character in characters
			where character != null
			orderby character.grade, TryParseHeroSeed(character.id, out var seed) ? seed : int.MaxValue, character.displayName
			select character).ToList();
	}
}
