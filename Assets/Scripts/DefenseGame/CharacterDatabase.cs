using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenseGame
{
    public class CharacterDatabase : MonoBehaviour
    {
        [SerializeField] private List<CharacterDefinition> characters = new List<CharacterDefinition>();
        [SerializeField] private bool generateStarterCharacters = true;
        [SerializeField] private int starterCharacterCount = 54;
        [SerializeField] private GamePresentationConfig presentationConfig;
        [SerializeField] private CharacterCombatTuningConfig combatTuningConfig;
        // Each entry is an inclusive start round and remains fixed until the next entry.
        [SerializeField] private List<SummonGradeRateMilestone> summonGradeRateMilestones = new List<SummonGradeRateMilestone>
        {
            new SummonGradeRateMilestone(1, 0.960f, 0.040f, 0.000f, 0.000f, 0.000f),
            new SummonGradeRateMilestone(3, 0.940f, 0.060f, 0.000f, 0.000f, 0.000f),
            new SummonGradeRateMilestone(5, 0.910f, 0.085f, 0.005f, 0.000f, 0.000f),
            new SummonGradeRateMilestone(7, 0.880f, 0.105f, 0.015f, 0.000f, 0.000f),
            new SummonGradeRateMilestone(9, 0.840f, 0.130f, 0.028f, 0.002f, 0.000f),
            new SummonGradeRateMilestone(11, 0.800f, 0.155f, 0.040f, 0.005f, 0.000f),
            new SummonGradeRateMilestone(13, 0.760f, 0.180f, 0.052f, 0.008f, 0.000f),
            new SummonGradeRateMilestone(16, 0.700f, 0.215f, 0.070f, 0.015f, 0.000f),
            new SummonGradeRateMilestone(19, 0.650f, 0.240f, 0.087f, 0.022f, 0.001f),
            new SummonGradeRateMilestone(22, 0.610f, 0.260f, 0.100f, 0.027f, 0.003f),
            new SummonGradeRateMilestone(25, 0.570f, 0.275f, 0.115f, 0.035f, 0.005f),
            new SummonGradeRateMilestone(28, 0.540f, 0.290f, 0.125f, 0.038f, 0.007f),
            new SummonGradeRateMilestone(31, 0.510f, 0.305f, 0.135f, 0.041f, 0.009f),
            new SummonGradeRateMilestone(34, 0.485f, 0.315f, 0.145f, 0.044f, 0.011f),
            new SummonGradeRateMilestone(37, 0.460f, 0.325f, 0.150f, 0.052f, 0.013f),
            new SummonGradeRateMilestone(40, 0.435f, 0.335f, 0.155f, 0.060f, 0.015f),
            new SummonGradeRateMilestone(43, 0.410f, 0.345f, 0.160f, 0.068f, 0.017f),
            new SummonGradeRateMilestone(46, 0.390f, 0.350f, 0.165f, 0.075f, 0.020f),
            new SummonGradeRateMilestone(49, 0.370f, 0.355f, 0.170f, 0.082f, 0.023f)
        };

        private const int NormalHeroMax = 5;
        private const int RareHeroMax = 10;
        private const int EpicHeroMax = 20;
        private const int LegendaryHeroMax = 30;
        private const int MythicHeroMax = 50;
        private const int TranscendentHeroMax = 100;

        private static readonly string[] NormalNames =
        {
            "Stone Guard", "Wind Archer", "Copper Gunner", "Lantern Mage", "Oak Fighter", "Wave Scout",
            "Dust Spear", "Iron Brawler", "Torch Adept", "Field Medic", "Mist Hunter", "Hammer Kid"
        };

        [System.Serializable]
        private class SummonGradeRateMilestone
        {
            public int round = 1;
            [Range(0f, 1f)] public float normalChance = 0.62f;
            [Range(0f, 1f)] public float rareChance = 0.24f;
            [Range(0f, 1f)] public float epicChance = 0.10f;
            [Range(0f, 1f)] public float legendaryChance = 0.035f;
            [Range(0f, 1f)] public float mythicChance = 0.005f;

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
                return new SummonGradeRates(
                    Mathf.Max(0f, normalChance),
                    Mathf.Max(0f, rareChance),
                    Mathf.Max(0f, epicChance),
                    Mathf.Max(0f, legendaryChance),
                    Mathf.Max(0f, mythicChance));
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

        private static readonly string[] RareNames =
        {
            "Azure Ranger", "Ruby Caster", "Verdant Monk", "Storm Javelin", "Moon Shot", "Steel Captain",
            "Blaze Tactician", "Echo Dancer"
        };

        private static readonly string[] EpicNames =
        {
            "Frost Oracle", "Thunder Duelist", "Bloom Witch", "Sand Reaper", "Nova Mechanic", "Tide Caller"
        };

        private static readonly string[] LegendaryNames =
        {
            "Solar Marshal", "Void Huntress", "Abyss Engineer"
        };

        private static readonly string[] MythicNames =
        {
            "Celestial Sovereign"
        };

        private static readonly string[] TranscendentNames =
        {
            "Origin Crown", "Eclipse Architect", "Infinity Dragon"
        };

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
                return;
            }

            ApplyDefinitionOverrides();
        }

        public void RefreshGeneratedCharactersFromConfigs()
        {
            if (generateStarterCharacters)
            {
                GenerateStarterCharacters(ResolveStarterGenerationCount());
                return;
            }

            ApplyDefinitionOverrides();
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
            if (!generateStarterCharacters || config == null)
            {
                return;
            }

            int configuredCharacterCount = ResolveStarterGenerationCount();
            if (configuredCharacterCount > GetHighestGeneratedCharacterIndex())
            {
                GenerateStarterCharacters(configuredCharacterCount);
            }
        }

        public List<CharacterDefinition> GetCharactersByGrade(CharacterGrade grade, bool deployableOnly = false)
        {
            return characters
                .Where(c => c != null && c.grade == grade && (!deployableOnly || IsDeployable(c)))
                .ToList();
        }

        public List<CharacterDefinition> GetDeployableCharacters()
        {
            return characters.Where(c => c != null && IsDeployable(c)).ToList();
        }

        public CharacterDefinition GetCharacterById(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return null;
            }

            return characters.FirstOrDefault(character => character != null && character.id == characterId);
        }

        public CharacterDefinition GetRandomCharacterByIds(IEnumerable<string> characterIds, bool deployableOnly = false)
        {
            if (characterIds == null)
            {
                return null;
            }

            List<CharacterDefinition> candidates = characterIds
                .Select(GetCharacterById)
                .Where(character => character != null && (!deployableOnly || IsDeployable(character)))
                .ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        public CharacterDefinition GetRandomCharacterByGrade(CharacterGrade grade, bool deployableOnly = false)
        {
            List<CharacterDefinition> candidates = GetCharactersByGrade(grade, deployableOnly);
            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        public CharacterDefinition GetRandomCharacterByGradeOrLower(CharacterGrade grade, bool deployableOnly = false)
        {
            for (int gradeIndex = (int)grade; gradeIndex >= (int)CharacterGrade.Normal; gradeIndex--)
            {
                CharacterDefinition candidate = GetRandomCharacterByGrade((CharacterGrade)gradeIndex, deployableOnly);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        public CharacterDefinition GetRandomCombatDeckCharacterByGradeOrLower(CharacterGrade grade)
        {
            OutgameProgressionSystem progression = OutgameProgressionSystem.Active;
            List<CharacterDefinition> deck = progression != null
                ? progression.GetCombatDeckCharacters()
                : GetDeployableCharacters();

            for (int gradeIndex = (int)grade; gradeIndex >= (int)CharacterGrade.Normal; gradeIndex--)
            {
                List<CharacterDefinition> candidates = deck
                    .Where(character => character != null && character.grade == (CharacterGrade)gradeIndex)
                    .ToList();
                if (candidates.Count > 0)
                {
                    return candidates[Random.Range(0, candidates.Count)];
                }
            }

            return null;
        }

        public CharacterDefinition GetRandomCombatDeckCharacterByGrade(CharacterGrade grade)
        {
            OutgameProgressionSystem progression = OutgameProgressionSystem.Active;
            List<CharacterDefinition> deck = progression != null
                ? progression.GetCombatDeckCharacters()
                : GetDeployableCharacters();
            List<CharacterDefinition> candidates = deck
                .Where(character => character != null && character.grade == grade)
                .ToList();
            return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
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
            return fallback.Count > 0 ? fallback[Random.Range(0, fallback.Count)] : null;
        }

        public CharacterDefinition GetRandomCombatDeckSummonableCharacter(int currentRound)
        {
            return GetRandomCombatDeckCharacterByGradeOrLower(RollSummonGradeForRound(currentRound));
        }

        private CharacterGrade RollSummonGradeForRound(int currentRound)
        {
            SummonGradeRates rates = ResolveSummonGradeRates(currentRound);
            float total = rates.normal + rates.rare + rates.epic + rates.legendary + rates.mythic;
            if (total <= 0.0001f)
            {
                return CharacterGrade.Normal;
            }

            float roll = Random.value * total;
            if (roll < rates.normal) return CharacterGrade.Normal;
            roll -= rates.normal;
            if (roll < rates.rare) return CharacterGrade.Rare;
            roll -= rates.rare;
            if (roll < rates.epic) return CharacterGrade.Epic;
            roll -= rates.epic;
            if (roll < rates.legendary) return CharacterGrade.Legendary;
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
                if (milestone == null)
                {
                    continue;
                }

                if (earliest == null || milestone.round < earliest.round)
                {
                    earliest = milestone;
                }

                if (milestone.round <= round && (selected == null || milestone.round > selected.round))
                {
                    selected = milestone;
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
            float epicBonus = fortune != null ? Mathf.Max(0f, fortune.epicSummonChanceBonus) : 0f;
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
                if (definition == null || !TryResolveCanonicalHeroGrade(definition.id, out CharacterGrade grade))
                {
                    continue;
                }

                definition.grade = grade;
                definition.mergeValue = 1 + (int)grade;
            }
        }

        private void ApplyCanonicalHeroBalance(CharacterDefinition definition)
        {
            if (definition == null || !TryResolveCanonicalHeroGrade(definition.id, out CharacterGrade grade))
            {
                return;
            }

            int seed = TryParseHeroSeed(definition.id, out int parsedSeed) ? parsedSeed : 0;
            definition.grade = grade;
            definition.mergeValue = 1 + (int)grade;
            definition.stats = BuildStats(definition.grade, definition.role, seed);
            definition.tags = CharacterTagUtility.BuildDefaultTags(definition.role, seed, definition.grade);
            definition.accentColor = ResolveColor(definition.grade, definition.role);
            definition.description = BuildDescription(definition.displayName, definition.grade, definition.role);
            ApplySignatureTranscendentBalance(definition);
        }

        private void ApplySignatureTranscendentBalance(CharacterDefinition definition)
        {
            if (definition == null || definition.stats == null)
            {
                return;
            }

            switch (definition.id)
            {
                case "hero_05":
                    // Dice Shield Rig is a normal frontline unit; keep its body durable
                    // without letting a single copy out-tank an entire early board.
                    definition.stats.maxHealth = 108f;
                    definition.stats.attackPower = 11f;
                    definition.stats.attackSpeed = 1.0f;
                    definition.stats.maxMana = 100f;
                    definition.stats.manaRegenPerSecondRate = 0.05f;
                    definition.stats.manaGainWhenHitRate = 0.10f;
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
                    definition.stats.criticalChance = 0.20f;
                    definition.stats.criticalDamageMultiplier = 2.0f;
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
                    definition.stats.criticalChance = 0.20f;
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

        private bool TryResolveCanonicalHeroGrade(string characterId, out CharacterGrade grade)
        {
            grade = CharacterGrade.Normal;
            if (!TryParseHeroSeed(characterId, out int seed))
            {
                return false;
            }

            int heroNumber = seed + 1;
            if (heroNumber <= NormalHeroMax)
            {
                grade = CharacterGrade.Normal;
            }
            else if (heroNumber <= RareHeroMax)
            {
                grade = CharacterGrade.Rare;
            }
            else if (heroNumber <= EpicHeroMax)
            {
                grade = CharacterGrade.Epic;
            }
            else if (heroNumber <= LegendaryHeroMax)
            {
                grade = CharacterGrade.Legendary;
            }
            else if (heroNumber <= MythicHeroMax)
            {
                grade = CharacterGrade.Mythic;
            }
            else if (heroNumber <= TranscendentHeroMax)
            {
                grade = CharacterGrade.Transcendent;
            }
            else
            {
                return false;
            }

            return true;
        }

        private int GetHighestGeneratedCharacterIndex()
        {
            int highestIndex = 0;
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterDefinition definition = characters[i];
                if (definition == null || !TryParseHeroSeed(definition.id, out int seed))
                {
                    continue;
                }

                highestIndex = Mathf.Max(highestIndex, seed + 1);
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
            if (parts.Length == 0 || !int.TryParse(parts[parts.Length - 1], out int parsed))
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
            int gradeIndex = (int)grade;
            string id = $"hero_{seed + 1:D2}";
            CharacterRole role = combatTuningConfig != null
                ? combatTuningConfig.ResolveRole(id, ResolveRole(seed))
                : ResolveRole(seed);
            CharacterDefinition definition = new CharacterDefinition();
            definition.id = id;
            definition.displayName = name;
            definition.description = BuildDescription(name, grade, role);
            definition.grade = grade;
            definition.role = role;
            definition.tags = CharacterTagUtility.BuildDefaultTags(role, seed, grade);
            definition.accentColor = ResolveColor(grade, role);
            definition.mergeValue = 1 + gradeIndex;
            definition.stats = BuildStats(grade, role, seed);
            definition.skills = BuildSkills(definition.displayName, grade, role, seed);
            return definition;
        }

        private CombatStats BuildStats(CharacterGrade grade, CharacterRole role, int seed)
        {
            int gradeIndex = (int)grade;
            CombatStats stats = new CombatStats();
            stats.maxHealth = 85f + gradeIndex * 40f + seed * 1.5f;
            stats.attackPower = 9f + gradeIndex * 6f + (seed % 4);
            stats.criticalChance = Mathf.Clamp01(0.08f + gradeIndex * 0.035f);
            stats.criticalDamageMultiplier = 1.5f + gradeIndex * 0.1f;
            stats.attackSpeed = 1f + gradeIndex * 0.1f;
            stats.maxMana = 100f + gradeIndex * 18f;
            stats.attackRange = 4.75f + gradeIndex * 0.3f + (seed % 4) * 0.2f;
            stats.projectileSpeed = 11f + gradeIndex * 1.5f;
            stats.moveSpeed = 0f;
            stats.manaRegenPerSecondRate = 0.05f;
            stats.manaGainWhenHitRate = 0.10f;
            stats.manaGainPerAttackRate = 0.15f;

            if (role == CharacterRole.Vanguard)
            {
                stats.maxHealth *= 1.35f;
                stats.attackRange = 2.35f + (seed % 3) * 0.2f;
                stats.attackPower *= 1.1f;
                stats.manaGainWhenHitRate = 0.12f;
                stats.manaGainPerAttackRate = 0.16f;
            }
            else if (role == CharacterRole.Ranger)
            {
                stats.attackRange += 3.4f;
                stats.attackSpeed *= 1.1f;
                stats.projectileSpeed += 2f;
                stats.manaGainPerAttackRate = 0.17f;
            }
            else if (role == CharacterRole.Mage)
            {
                stats.attackPower *= 1.25f;
                stats.maxMana *= 1.2f;
                stats.attackRange += 1.7f;
                stats.manaRegenPerSecondRate = 0.06f;
                stats.manaGainPerAttackRate = 0.16f;
            }
            else if (role == CharacterRole.Support)
            {
                stats.maxHealth *= 1.1f;
                stats.attackSpeed *= 0.92f;
                stats.maxMana *= 1.35f;
                stats.attackRange += 1.2f;
                stats.manaRegenPerSecondRate = 0.07f;
                stats.manaGainWhenHitRate = 0.12f;
                stats.manaGainPerAttackRate = 0.14f;
            }
            else if (role == CharacterRole.Assassin)
            {
                stats.criticalChance += 0.14f;
                stats.criticalDamageMultiplier += 0.35f;
                stats.attackSpeed *= 1.2f;
                stats.maxHealth *= 0.85f;
                stats.attackRange = 3f + (seed % 2) * 0.25f;
                stats.manaGainPerAttackRate = 0.18f;
            }
            else if (role == CharacterRole.Summoner)
            {
                stats.attackPower *= 0.95f;
                stats.maxMana *= 1.45f;
                stats.attackRange += 2.2f;
                stats.manaRegenPerSecondRate = 0.08f;
                stats.manaGainPerAttackRate = 0.14f;
            }

            stats.criticalChance = Mathf.Clamp01(stats.criticalChance);
            return stats;
        }

        private List<SkillDefinition> BuildSkills(string ownerName, CharacterGrade grade, CharacterRole role, int seed)
        {
            List<SkillEffectType> pool = BuildRoleSkillPool(role);
            int count = GradeRules.GetSkillCount(grade, false);
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
            if (role == CharacterRole.Vanguard) return new List<SkillEffectType> { SkillEffectType.Taunt, SkillEffectType.DirectDamage, SkillEffectType.Stun, SkillEffectType.DefenseBuff, SkillEffectType.AreaDamage };
            if (role == CharacterRole.Ranger) return new List<SkillEffectType> { SkillEffectType.MultiShot, SkillEffectType.Slow, SkillEffectType.Poison, SkillEffectType.AttackSpeedBoost, SkillEffectType.DirectDamage };
            if (role == CharacterRole.Mage) return new List<SkillEffectType> { SkillEffectType.AreaDamage, SkillEffectType.GroundAreaDamage, SkillEffectType.Slow, SkillEffectType.Stun, SkillEffectType.ManaSurge };
            if (role == CharacterRole.Support) return new List<SkillEffectType> { SkillEffectType.ShieldAlly, SkillEffectType.HealSelf, SkillEffectType.DefenseBuff, SkillEffectType.ManaSurge, SkillEffectType.AttackSpeedBoost, SkillEffectType.CriticalBoost };
            if (role == CharacterRole.Assassin) return new List<SkillEffectType> { SkillEffectType.Execute, SkillEffectType.LifeSteal, SkillEffectType.Stun, SkillEffectType.CriticalBoost, SkillEffectType.DirectDamage };
            return new List<SkillEffectType> { SkillEffectType.SummonRush, SkillEffectType.Transform, SkillEffectType.ShieldAlly, SkillEffectType.ManaSurge, SkillEffectType.AreaDamage };
        }

        private SkillDefinition CreateSkill(string ownerName, CharacterRole role, SkillEffectType effectType, int index)
        {
            SkillDefinition skill = new SkillDefinition();
            skill.id = $"{ownerName}_{effectType}_{index}";
            skill.effectType = effectType;
            skill.category = SkillDefinitionUtility.ResolveCategory(effectType);
            skill.manaThreshold = role == CharacterRole.Support ? 80f : 100f;
            skill.hitCount = effectType == SkillEffectType.MultiShot ? 3 : 1;
            skill.radius = 2.6f;
            skill.secondaryPower = 0.35f;

            if (effectType == SkillEffectType.DirectDamage)
            {
                skill.displayName = "Power Shot";
                skill.description = "Current target takes amplified burst damage.";
                skill.power = 1.85f;
                skill.cooldown = 5f;
            }
            else if (effectType == SkillEffectType.AreaDamage)
            {
                skill.displayName = "Burst Nova";
                skill.description = "Deals damage to nearby enemies around the impact area.";
                skill.power = 1.2f;
                skill.radius = 3.2f;
                skill.cooldown = 6f;
            }
            else if (effectType == SkillEffectType.HealSelf)
            {
                skill.displayName = "Battle Prayer";
                skill.description = "Recover a portion of max health.";
                skill.power = 0.3f;
                skill.cooldown = 8f;
            }
            else if (effectType == SkillEffectType.AttackSpeedBoost)
            {
                skill.displayName = "Rapid Tempo";
                skill.description = "Gain a temporary attack speed boost.";
                skill.power = 0.45f;
                skill.duration = 5f;
                skill.cooldown = 9f;
            }
            else if (effectType == SkillEffectType.CriticalBoost)
            {
                skill.displayName = "Predator Focus";
                skill.description = "Temporarily raises critical chance.";
                skill.power = 0.24f;
                skill.duration = 5f;
                skill.cooldown = 9f;
            }
            else if (effectType == SkillEffectType.ManaSurge)
            {
                skill.displayName = "Mana Current";
                skill.description = "Instantly recover mana to chain casts.";
                skill.power = 0.48f;
                skill.cooldown = 7f;
            }
            else if (effectType == SkillEffectType.MultiShot)
            {
                skill.displayName = "Arrow Bloom";
                skill.description = "Fire several rapid shots across multiple enemies.";
                skill.power = 0.8f;
                skill.hitCount = 3;
                skill.cooldown = 7f;
            }
            else if (effectType == SkillEffectType.Execute)
            {
                skill.displayName = "Execution Cut";
                skill.description = "Deals extra damage to low health enemies.";
                skill.power = 2.2f;
                skill.cooldown = 7f;
            }
            else if (effectType == SkillEffectType.SummonRush)
            {
                skill.displayName = "Spirit Guardian";
                skill.description = "Summons a fragile spirit ally into the monster lane.";
                skill.power = 0.25f;
                skill.secondaryPower = 0.25f;
                skill.hitCount = 1;
                skill.radius = 3.2f;
                skill.cooldown = 10f;
            }
            else if (effectType == SkillEffectType.Slow)
            {
                skill.displayName = "Frost Thread";
                skill.description = "Slows the target monster for a short time.";
                skill.power = 0.42f;
                skill.duration = 3.5f;
                skill.cooldown = 7f;
            }
            else if (effectType == SkillEffectType.Stun)
            {
                skill.displayName = "Impact Seal";
                skill.description = "Briefly stuns the target monster.";
                skill.power = 0f;
                skill.duration = 1.25f;
                skill.cooldown = 8f;
            }
            else if (effectType == SkillEffectType.ShieldAlly)
            {
                skill.displayName = "Guardian Veil";
                skill.description = "Grants a shield to the lowest health ally.";
                skill.power = 0.32f;
                skill.duration = 5f;
                skill.cooldown = 8f;
                skill.manaThreshold = 80f;
            }
            else if (effectType == SkillEffectType.LifeSteal)
            {
                skill.displayName = "Blood Recall";
                skill.description = "Deals damage and restores health from the damage dealt.";
                skill.power = 1.25f;
                skill.secondaryPower = 0.45f;
                skill.cooldown = 7f;
            }
            else if (effectType == SkillEffectType.GroundAreaDamage)
            {
                skill.displayName = "Arcane Field";
                skill.description = "Creates a damaging field that repeatedly hits monsters in the area.";
                skill.power = 0.34f;
                skill.secondaryPower = 0.55f;
                skill.radius = 3.4f;
                skill.duration = 3.2f;
                skill.cooldown = 8f;
            }
            else if (effectType == SkillEffectType.Poison)
            {
                skill.displayName = "Toxic Mark";
                skill.description = "Poisons the target, dealing damage over time.";
                skill.power = 0.28f;
                skill.secondaryPower = 0.8f;
                skill.duration = 4.2f;
                skill.cooldown = 7f;
            }
            else if (effectType == SkillEffectType.DefenseBuff)
            {
                skill.displayName = "Iron Oath";
                skill.description = "Grants a defensive shield to nearby allies.";
                skill.power = 0.22f;
                skill.radius = 4.2f;
                skill.duration = 5f;
                skill.cooldown = 8f;
                skill.manaThreshold = 85f;
            }
            else if (effectType == SkillEffectType.Taunt)
            {
                skill.displayName = "Challenge Roar";
                skill.description = "Taunts nearby monsters, forcing them to attack this unit.";
                skill.power = 0f;
                skill.radius = 3.8f;
                skill.duration = 3.5f;
                skill.cooldown = 8f;
                skill.manaThreshold = 90f;
            }
            else if (effectType == SkillEffectType.Transform)
            {
                skill.displayName = "Awakened Form";
                skill.description = "Transforms into an empowered combat state for a short time.";
                skill.power = 0.34f;
                skill.secondaryPower = 0.18f;
                skill.duration = 6f;
                skill.cooldown = 10f;
            }
            else
            {
                skill.displayName = "Armor Rend";
                skill.description = "Crushes a target with a defense-breaking hit.";
                skill.power = 1.6f;
                skill.cooldown = 6f;
            }

            return skill;
        }

        private string BuildDescription(string name, CharacterGrade grade, CharacterRole role)
        {
            return name + " is a " + grade + " " + role + " who protects the last defense line.";
        }

        private CharacterRole ResolveRole(int seed)
        {
            int value = seed % 6;
            if (value == 0) return CharacterRole.Vanguard;
            if (value == 1) return CharacterRole.Ranger;
            if (value == 2) return CharacterRole.Mage;
            if (value == 3) return CharacterRole.Support;
            if (value == 4) return CharacterRole.Assassin;
            return CharacterRole.Summoner;
        }

        private Color ResolveColor(CharacterGrade grade, CharacterRole role)
        {
            Color baseColor = Color.white;
            if (grade == CharacterGrade.Normal) baseColor = new Color(0.75f, 0.75f, 0.75f);
            else if (grade == CharacterGrade.Rare) baseColor = new Color(0.35f, 0.7f, 1f);
            else if (grade == CharacterGrade.Epic) baseColor = new Color(0.35f, 1f, 0.7f);
            else if (grade == CharacterGrade.Legendary) baseColor = new Color(1f, 0.76f, 0.25f);
            else if (grade == CharacterGrade.Mythic) baseColor = new Color(1f, 0.35f, 0.35f);
            else if (grade == CharacterGrade.Transcendent) baseColor = new Color(0.92f, 0.42f, 1f);

            if (role == CharacterRole.Assassin) baseColor *= new Color(1.05f, 0.85f, 0.95f);
            if (role == CharacterRole.Support) baseColor *= new Color(0.9f, 1.05f, 1.05f);
            return baseColor;
        }

        private CharacterGrade ResolveStarterGrade(int index, int totalCount)
        {
            float ratio = totalCount <= 1 ? 1f : (float)index / (totalCount - 1);
            if (ratio < 0.4f) return CharacterGrade.Normal;
            if (ratio < 0.7f) return CharacterGrade.Rare;
            if (ratio < 0.88f) return CharacterGrade.Epic;
            if (ratio < 0.97f) return CharacterGrade.Legendary;
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
            if (!generateStarterCharacters || (presentationConfig == null && combatTuningConfig == null))
            {
                return;
            }

            characters = characters
                .Where(IsConfiguredGeneratedCharacter)
                .ToList();
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
            characters = characters
                .Where(character => character != null)
                .OrderBy(character => character.grade)
                .ThenBy(character => TryParseHeroSeed(character.id, out int seed) ? seed : int.MaxValue)
                .ThenBy(character => character.displayName)
                .ToList();
        }
    }

    [CreateAssetMenu(fileName = "OutgameProgressionConfig", menuName = "Defense Game/Outgame Progression")]
    public class OutgameProgressionConfig : ScriptableObject
    {
        [Header("Profiles")]
        public OutgamePlayMode defaultPlayMode = OutgamePlayMode.Service;
        public int testStartingDiamonds = 999999;
        public int testDiamondRechargeAmount = 10000;
        public int testStartingGold = 999999;
        public int testGoldRechargeAmount = 10000;
        public int serviceStarterCharacterCount = 5;
        public List<string> serviceStarterCharacterIds = new List<string>
        {
            "hero_01",
            "hero_02",
            "hero_03",
            "hero_04",
            "hero_05"
        };

        [Header("Shop Economy")]
        public int startingGold = 3000;
        public int startingDiamonds = 500;
        public int singleChestCost = 100;
        public int tenChestCost = 900;
        public int fiveChestCost = 480;
        public int twentyChestCost = 1800;
        public int fiftyChestCost = 4250;
        public int hundredChestCost = 8000;
        public int dailyFreeGold = 500;
        public int dailyCardPackGoldCost = 1200;
        public int dailyCardPackDrawCount = 5;
        public int dailyPremiumPackDiamondCost = 250;
        public int dailyPremiumPackDrawCount = 3;
        public int diamondsPerBattleRewardPoint = 1;

        [Header("Earned Chest Loop")]
        public int progressionVersion = 3;
        public int startingEarnedChestKeys = 1;
        public int migrationEarnedChestKeys = 1;
        public int earnedChestProgressTarget = 100;
        public int earnedChestRarePityDraws = 8;
        public int earnedChestEpicPityDraws = 30;
        public int premiumChestEpicPityDraws = 10;
        public int premiumChestLegendaryPityDraws = 40;
        public int premiumWishlistPityDraws = 20;
        [Range(0f, 1f)] public float premiumWishlistChance = 0.12f;
        public int hurdleFailureSupportChestKeys = 1;

        [Header("Card Growth")]
        public int initialUnlockCopies = 1;
        public int duplicateCopiesForLevelTwo = 2;
        public int additionalCopiesPerLevel = 1;
        public int maxCardLevel = 20;

        [Header("Chest Grade Rates")]
        [Range(0f, 1f)] public float normalRate = 0.65f;
        [Range(0f, 1f)] public float rareRate = 0.23f;
        [Range(0f, 1f)] public float epicRate = 0.085f;
        [Range(0f, 1f)] public float legendaryRate = 0.028f;
        [Range(0f, 1f)] public float mythicRate = 0.006f;
        [Range(0f, 1f)] public float transcendentRate = 0.001f;
        [Header("Premium Chest Grade Rates")]
        [Range(0f, 1f)] public float premiumNormalRate = 0.45f;
        [Range(0f, 1f)] public float premiumRareRate = 0.32f;
        [Range(0f, 1f)] public float premiumEpicRate = 0.16f;
        [Range(0f, 1f)] public float premiumLegendaryRate = 0.055f;
        [Range(0f, 1f)] public float premiumMythicRate = 0.013f;
        [Range(0f, 1f)] public float premiumTranscendentRate = 0.002f;

        [Header("Unit Growth Per Card Level")]
        [Range(0f, 0.2f)] public float attackPowerPerGrowthLevel = 0.03f;
        [Range(0f, 0.2f)] public float maxHealthPerGrowthLevel = 0.03f;

        [Header("Monster Balance From Collection Growth")]
        public bool scaleMonstersWithCollectionGrowth = false;
        [Range(0f, 0.2f)] public float regularHealthPerAverageGrowthLevel = 0.018f;
        [Range(0f, 0.2f)] public float regularAttackPerAverageGrowthLevel = 0.012f;
        [Range(0f, 0.2f)] public float bossHealthPerAverageGrowthLevel = 0.025f;
        [Range(0f, 0.2f)] public float bossAttackPerAverageGrowthLevel = 0.016f;
        [Range(0f, 5f)] public float maxMonsterHealthBonus = 0.75f;
        [Range(0f, 5f)] public float maxMonsterAttackBonus = 0.5f;
    }

    public enum OutgamePlayMode
    {
        Service = 0,
        Test = 1
    }

    [System.Serializable]
    public class OutgameCardRecord
    {
        public string characterId;
        public int totalCopies;
        public int upgradeCopies;
        public int level;
    }

    public enum OutgameChestType
    {
        Earned = 0,
        Premium = 1
    }


    [System.Serializable]
    public class OutgameSaveData
    {
        public int gold;
        public int diamonds;
        public int dailyShopDate;
        public int dailyShopPurchaseFlags;
        public int metaProgressionVersion;
        public int earnedChestKeys;
        public int earnedChestProgress;
        public int earnedRarePity;
        public int earnedEpicPity;
        public int premiumEpicPity;
        public int premiumLegendaryPity;
        public int premiumWishlistPity;
        public string wishlistCharacterId;
        public int highestRoundReached;
        public int hurdleFailureSupportFlags;
        public int hurdleClearRewardFlags;
        public bool initialRosterGranted;
        public List<OutgameCardRecord> cards = new List<OutgameCardRecord>();
        public int seasonId;
        public int weeklyBossScore;
        public int weeklyBestRunScore;
        public int weeklyBossKills;
        public int seasonMissionClaimFlags;
        public int coopBossScore;
        public int coopMvpCount;
        public string lastCoopMvpName;
        public string lastDeckShareCode;
        public string lastReplayDigest;
        public int dailyFateCupDate;
        public int dailyFateCupAttempts;
        public int dailyFateCupBestScore;
        public int dailyFateCupBestRound;
        public string dailyFateCupBestReplay;
        // Persisted service/test combat deck.  JsonUtility treats a missing field from
        // older saves as null, which is repaired during the normal load migration.
        public List<string> combatDeckCharacterIds = new List<string>();
    }

    public sealed class OutgameDrawResult
    {
        public CharacterDefinition character;
        public bool firstAcquisition;
        public bool leveledUp;
        public int level;
        public int remainingCopies;
        public int requiredCopies;
        public OutgameChestType chestType;
        public bool wishlistHit;
        public bool pityTriggered;
    }

    public class OutgameProgressionSystem : MonoBehaviour
    {
        private const string ServiceSaveKey = "DefenseGame.OutgameProgression.Service.v1";
        private const string TestSaveKey = "DefenseGame.OutgameProgression.Test.v1";
        private const string PlayModeKey = "DefenseGame.OutgameProgression.PlayMode.v1";
        private const int BossScoreMissionFlag = 1 << 0;
        private const int BossKillMissionFlag = 1 << 1;
        private const int RunScoreMissionFlag = 1 << 2;
        private const int BossScoreMissionTarget = 1200;
        private const int BossKillMissionTarget = 3;
        private const int RunScoreMissionTarget = 135;

        [SerializeField] private OutgameProgressionConfig config;
        [SerializeField] private CharacterDatabase characterDatabase;

        private OutgameProgressionConfig runtimeConfig;
        private OutgameSaveData saveData;
        private OutgamePlayMode currentPlayMode;
        private string lastSeasonRewardSummary = string.Empty;

        public static OutgameProgressionSystem Active { get; private set; }
        public event System.Action OnProgressChanged;

        public OutgameProgressionConfig Settings
        {
            get
            {
                if (config != null)
                {
                    return config;
                }

                if (runtimeConfig == null)
                {
                    runtimeConfig = ScriptableObject.CreateInstance<OutgameProgressionConfig>();
                }

                return runtimeConfig;
            }
        }

        public int Gold => EnsureSaveData().gold;
        public int Diamonds => EnsureSaveData().diamonds;
        public int CurrentSeasonId => EnsureSaveData().seasonId;
        public int WeeklyBossScore => EnsureSaveData().weeklyBossScore;
        public int WeeklyBestRunScore => EnsureSaveData().weeklyBestRunScore;
        public int WeeklyBossKills => EnsureSaveData().weeklyBossKills;
        public int EarnedChestKeys => EnsureSaveData().earnedChestKeys;
        public int EarnedChestProgress => EnsureSaveData().earnedChestProgress;
        public int EarnedChestProgressTarget => Mathf.Max(1, Settings.earnedChestProgressTarget);
        public int HighestRoundReached => EnsureSaveData().highestRoundReached;
        public string WishlistCharacterId => EnsureSaveData().wishlistCharacterId;
        public OutgamePlayMode CurrentPlayMode => currentPlayMode;
        public bool IsTestMode => currentPlayMode == OutgamePlayMode.Test;
        public string LastSeasonRewardSummary => lastSeasonRewardSummary;
        public int DailyFateCupBestScore => EnsureSaveData().dailyFateCupBestScore;
        public int DailyFateCupAttempts => EnsureSaveData().dailyFateCupAttempts;
        public const int CombatDeckSlotCount = 5;
        public bool HasCombatDeckConfiguration => EnsureSaveData().combatDeckCharacterIds != null && EnsureSaveData().combatDeckCharacterIds.Count > 0;

        private void Awake()
        {
            Active = this;
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        public void Configure(OutgameProgressionConfig progressionConfig, CharacterDatabase database)
        {
            config = progressionConfig;
            characterDatabase = database;
            currentPlayMode = (OutgamePlayMode)PlayerPrefs.GetInt(PlayModeKey, (int)Settings.defaultPlayMode);
            Load();
            EnsureInitialRoster();
            EnsureCombatDeck();
            OnProgressChanged?.Invoke();
        }

        public void SwitchPlayMode(OutgamePlayMode playMode)
        {
            if (currentPlayMode == playMode)
            {
                return;
            }

            Save();
            currentPlayMode = playMode;
            PlayerPrefs.SetInt(PlayModeKey, (int)currentPlayMode);
            PlayerPrefs.Save();
            Load();
            EnsureInitialRoster();
            EnsureCombatDeck();
            OnProgressChanged?.Invoke();
        }

        public void RechargeTestDiamonds()
        {
            if (IsTestMode)
            {
                AddDiamonds(Mathf.Max(1, Settings.testDiamondRechargeAmount));
            }
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            EnsureSaveData().gold += amount;
            Save();
            OnProgressChanged?.Invoke();
        }

        public void RechargeTestCurrency()
        {
            if (!IsTestMode)
            {
                return;
            }

            OutgameSaveData data = EnsureSaveData();
            data.gold += Mathf.Max(1, Settings.testGoldRechargeAmount);
            data.diamonds += Mathf.Max(1, Settings.testDiamondRechargeAmount);
            Save();
            OnProgressChanged?.Invoke();
        }

        public void AddDiamonds(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            EnsureSaveData().diamonds += amount;
            Save();
            OnProgressChanged?.Invoke();
        }

        public bool TrySpendGold(int amount)
        {
            int cost = Mathf.Max(0, amount);
            OutgameSaveData data = EnsureSaveData();
            if (data.gold < cost)
            {
                return false;
            }

            data.gold -= cost;
            Save();
            OnProgressChanged?.Invoke();
            return true;
        }

        public bool TrySpendDiamonds(int amount)
        {
            int cost = Mathf.Max(0, amount);
            OutgameSaveData data = EnsureSaveData();
            if (data.diamonds < cost)
            {
                return false;
            }

            data.diamonds -= cost;
            Save();
            OnProgressChanged?.Invoke();
            return true;
        }

        public bool GrantYahtzeeCards(int drawCount, out List<OutgameDrawResult> results)
        {
            results = new List<OutgameDrawResult>();
            if (characterDatabase == null || drawCount <= 0)
            {
                return false;
            }

            DrawCardsInto(results, OutgameChestType.Premium, Mathf.Clamp(drawCount, 1, 100));
            Save();
            OnProgressChanged?.Invoke();
            return results.Count > 0;
        }

        public bool GrantTestShopCurrency(int gold, int diamonds)
        {
            if (!IsTestMode)
            {
                return false;
            }

            OutgameSaveData data = EnsureSaveData();
            data.gold += Mathf.Max(0, gold);
            data.diamonds += Mathf.Max(0, diamonds);
            Save();
            OnProgressChanged?.Invoke();
            return true;
        }

        public int ResolveBattleDiamondReward(int rewardPoints)
        {
            return Mathf.Max(0, rewardPoints * Mathf.Max(0, Settings.diamondsPerBattleRewardPoint));
        }

        public void RecordSeasonRun(int runScore, int bossScore, int bossKills, string mvpName, int round, bool victory)
        {
            OutgameSaveData data = EnsureSaveData();
            EnsureCurrentSeason(data);

            int safeRunScore = Mathf.Max(0, runScore);
            int safeBossScore = Mathf.Max(0, bossScore);
            int safeBossKills = Mathf.Max(0, bossKills);
            data.weeklyBestRunScore = Mathf.Max(data.weeklyBestRunScore, safeRunScore);
            data.weeklyBossScore = Mathf.Max(data.weeklyBossScore, safeBossScore);
            data.weeklyBossKills = Mathf.Max(data.weeklyBossKills, safeBossKills);

            int coopScore = Mathf.Max(0, Mathf.RoundToInt(safeBossScore * 0.65f + safeRunScore * 0.25f + safeBossKills * 180f + Mathf.Max(0, round) * 12f));
            data.coopBossScore = Mathf.Max(data.coopBossScore, coopScore);
            if (safeBossKills > 0 || victory)
            {
                int runMvpCount = safeBossKills > 0 ? safeBossKills : 1;
                data.coopMvpCount = Mathf.Max(data.coopMvpCount, runMvpCount);
                data.lastCoopMvpName = string.IsNullOrWhiteSpace(mvpName) ? "MVP 대기" : mvpName;
            }

            data.lastDeckShareCode = BuildDeckShareCode(safeRunScore, safeBossScore, safeBossKills, mvpName, round, victory);
            data.lastReplayDigest = BuildReplayDigest(safeRunScore, safeBossScore, safeBossKills, mvpName, round, victory);
            DefenseGameController controller = DefenseGameController.Active;
            if (controller != null && controller.DailyFateCupEnabled)
            {
                RecordDailyFateCupRun(data, safeRunScore, round, data.lastReplayDigest);
            }

            int reward = GrantSeasonMissionRewards(data);
            lastSeasonRewardSummary = reward > 0 ? "시즌 미션 보상 +" + reward + " DIA" : string.Empty;
            string chestRewardSummary = GrantCommercialBattleRewards(data, round, victory);
            if (!string.IsNullOrWhiteSpace(chestRewardSummary))
            {
                lastSeasonRewardSummary = string.IsNullOrWhiteSpace(lastSeasonRewardSummary) ? chestRewardSummary : lastSeasonRewardSummary + " | " + chestRewardSummary;
            }
            Save();
            OnProgressChanged?.Invoke();
        }

        private static void RecordDailyFateCupRun(OutgameSaveData data, int score, int round, string replayDigest)
        {
            if (data == null)
            {
                return;
            }

            int today = DailyFateCupRules.TodayKey;
            if (data.dailyFateCupDate != today)
            {
                data.dailyFateCupDate = today;
                data.dailyFateCupAttempts = 0;
                data.dailyFateCupBestScore = 0;
                data.dailyFateCupBestRound = 0;
                data.dailyFateCupBestReplay = string.Empty;
            }

            data.dailyFateCupAttempts++;
            if (score >= data.dailyFateCupBestScore)
            {
                data.dailyFateCupBestScore = Mathf.Max(0, score);
                data.dailyFateCupBestRound = Mathf.Max(0, round);
                data.dailyFateCupBestReplay = replayDigest ?? string.Empty;
            }
        }

        public string BuildDailyFateCupSummary()
        {
            OutgameSaveData data = EnsureSaveData();
            int today = DailyFateCupRules.TodayKey;
            if (data.dailyFateCupDate != today)
            {
                data.dailyFateCupDate = today;
                data.dailyFateCupAttempts = 0;
                data.dailyFateCupBestScore = 0;
                data.dailyFateCupBestRound = 0;
                data.dailyFateCupBestReplay = string.Empty;
                Save();
            }

            string replay = string.IsNullOrWhiteSpace(data.dailyFateCupBestReplay)
                ? "첫 도전 대기"
                : data.dailyFateCupBestReplay;
            return DailyFateCupRules.TodayLabel
                + "\n개인 최고  " + data.dailyFateCupBestScore.ToString("N0") + "점"
                + "  |  R" + data.dailyFateCupBestRound
                + "  |  도전 " + data.dailyFateCupAttempts + "회"
                + "\n" + replay
                + "\n서버 랭킹 연결 전 로컬 동일 시드 기록";
        }
        public string BuildSeasonRankingSummary()
        {
            OutgameSaveData data = EnsureSaveData();
            if (EnsureCurrentSeason(data))
            {
                Save();
            }

            string mvpName = string.IsNullOrWhiteSpace(data.lastCoopMvpName) ? "MVP 대기" : data.lastCoopMvpName;
            string deckShare = string.IsNullOrWhiteSpace(data.lastDeckShareCode) ? "대기" : data.lastDeckShareCode;
            string replayDigest = string.IsNullOrWhiteSpace(data.lastReplayDigest) ? "최근 런 없음" : data.lastReplayDigest;
            int rivalScore = Mathf.Max(900, data.weeklyBossScore - 160);
            int localRank = data.weeklyBossScore >= rivalScore ? 1 : 2;
            return "WEEK " + data.seasonId + " 프리시즌"
                + "\n주간 목표  보스 점수 / 협동 MVP / 런 점수 A 갱신"
                + "\n주간 보스 점수  " + data.weeklyBossScore.ToString("N0")
                + "  |  최고 런 " + data.weeklyBestRunScore.ToString("N0")
                + "\n비동기 친구 보스 랭킹  " + localRank + "위  |  나 " + data.weeklyBossScore.ToString("N0") + " / 라이벌 " + rivalScore.ToString("N0")
                + "\n협동 보스 준비  " + mvpName
                + "  |  MVP " + data.coopMvpCount + "회  |  협동 점수 " + data.coopBossScore.ToString("N0")
                + "\n덱 공유  " + deckShare + "  |  리플레이  " + replayDigest
                + "\n" + BuildSeasonMissionLine(data, BossScoreMissionFlag, "보스 점수 " + BossScoreMissionTarget.ToString("N0"), data.weeklyBossScore, BossScoreMissionTarget, 180)
                + "\n" + BuildSeasonMissionLine(data, BossKillMissionFlag, "보스 처치 " + BossKillMissionTarget + "회", data.weeklyBossKills, BossKillMissionTarget, 220)
                + "\n" + BuildSeasonMissionLine(data, RunScoreMissionFlag, "런 점수 A 달성", data.weeklyBestRunScore, RunScoreMissionTarget, 260);
        }

        public string BuildSeasonResultLoopSummary()
        {
            string daily = DefenseGameController.Active != null && DefenseGameController.Active.DailyFateCupEnabled
                ? BuildDailyFateCupSummary() + "\n"
                : string.Empty;
            return daily + BuildChestEconomySummary() + "\n" + BuildSeasonLegacyResultLoopSummary();
        }

        private string BuildSeasonLegacyResultLoopSummary()
        {
            OutgameSaveData data = EnsureSaveData();
            if (EnsureCurrentSeason(data))
            {
                Save();
            }

            string deckShare = string.IsNullOrWhiteSpace(data.lastDeckShareCode) ? "대기" : data.lastDeckShareCode;
            string replayDigest = string.IsNullOrWhiteSpace(data.lastReplayDigest) ? "최근 런 없음" : data.lastReplayDigest;
            string mvpName = string.IsNullOrWhiteSpace(data.lastCoopMvpName) ? "MVP 대기" : data.lastCoopMvpName;
            return "협동 " + data.coopBossScore.ToString("N0")
                + " / " + ResolveNextSeasonGoal(data)
                + " | 덱 " + deckShare
                + " | 리플레이 " + replayDigest
                + " | MVP " + mvpName;
        }

        private static string ResolveNextSeasonGoal(OutgameSaveData data)
        {
            if (data == null)
            {
                return "시즌 목표 대기";
            }

            if (data.weeklyBossScore < BossScoreMissionTarget)
            {
                return "보스 점수 " + data.weeklyBossScore.ToString("N0") + "/" + BossScoreMissionTarget.ToString("N0");
            }

            if (data.weeklyBossKills < BossKillMissionTarget)
            {
                return "보스 처치 " + data.weeklyBossKills + "/" + BossKillMissionTarget;
            }

            if (data.weeklyBestRunScore < RunScoreMissionTarget)
            {
                return "런 점수 A " + data.weeklyBestRunScore + "/" + RunScoreMissionTarget;
            }

            return "친구 보스 점수 갱신";
        }

        private static string BuildDeckShareCode(int runScore, int bossScore, int bossKills, string mvpName, int round, bool victory)
        {
            unchecked
            {
                int hash = 23;
                hash = hash * 31 + runScore;
                hash = hash * 31 + bossScore;
                hash = hash * 31 + bossKills;
                hash = hash * 31 + round;
                hash = hash * 31 + (victory ? 1 : 0);
                string safeMvp = string.IsNullOrWhiteSpace(mvpName) ? "MVP" : mvpName.Trim();
                for (int i = 0; i < safeMvp.Length; i++)
                {
                    hash = hash * 31 + safeMvp[i];
                }

                return "DG-" + Mathf.Abs(hash % 100000).ToString("D5");
            }
        }

        private static string BuildReplayDigest(int runScore, int bossScore, int bossKills, string mvpName, int round, bool victory)
        {
            string safeMvp = string.IsNullOrWhiteSpace(mvpName) ? "MVP 대기" : mvpName.Trim();
            string result = victory ? "승" : "패";
            return result + " R" + Mathf.Max(0, round) + " / " + safeMvp + " / 보스 " + bossKills + " / " + runScore.ToString("N0");
        }

        private int GrantSeasonMissionRewards(OutgameSaveData data)
        {
            int reward = 0;
            reward += TryGrantSeasonMissionReward(data, BossScoreMissionFlag, data.weeklyBossScore >= BossScoreMissionTarget, 180);
            reward += TryGrantSeasonMissionReward(data, BossKillMissionFlag, data.weeklyBossKills >= BossKillMissionTarget, 220);
            reward += TryGrantSeasonMissionReward(data, RunScoreMissionFlag, data.weeklyBestRunScore >= RunScoreMissionTarget, 260);
            if (reward > 0)
            {
                data.diamonds += reward;
            }

            return reward;
        }

        private static int TryGrantSeasonMissionReward(OutgameSaveData data, int flag, bool achieved, int reward)
        {
            reward = ResolveCommercialSeasonReward(flag);
            if (data == null || !achieved || (data.seasonMissionClaimFlags & flag) != 0)
            {
                return 0;
            }

            data.seasonMissionClaimFlags |= flag;
            return Mathf.Max(0, reward);
        }

        private static string BuildSeasonMissionLine(OutgameSaveData data, int flag, string title, int value, int target, int reward)
        {
            reward = ResolveCommercialSeasonReward(flag);
            bool claimed = data != null && (data.seasonMissionClaimFlags & flag) != 0;
            string state = claimed ? "수령 완료" : value >= target ? "보상 대기" : "진행 중";
            return title + "  " + Mathf.Min(value, target).ToString("N0") + "/" + target.ToString("N0") + "  |  " + state + " +" + reward + " DIA";
        }

        private static int ResolveCommercialSeasonReward(int flag)
        {
            if (flag == BossScoreMissionFlag) return 60;
            if (flag == BossKillMissionFlag) return 80;
            if (flag == RunScoreMissionFlag) return 120;
            return 0;
        }


        private static bool EnsureCurrentSeason(OutgameSaveData data)
        {
            if (data == null)
            {
                return false;
            }

            int currentSeason = ResolveCurrentSeasonId();
            if (data.seasonId == currentSeason)
            {
                return false;
            }

            data.seasonId = currentSeason;
            data.weeklyBossScore = 0;
            data.weeklyBestRunScore = 0;
            data.weeklyBossKills = 0;
            data.seasonMissionClaimFlags = 0;
            data.coopBossScore = 0;
            data.coopMvpCount = 0;
            data.lastCoopMvpName = string.Empty;
            return true;
        }

        private static int ResolveCurrentSeasonId()
        {
            System.DateTime utcNow = System.DateTime.UtcNow;
            int week = Mathf.Clamp((utcNow.DayOfYear - 1) / 7 + 1, 1, 53);
            return utcNow.Year * 100 + week;
        }

        public bool TryOpenChest(int drawCount, out List<OutgameDrawResult> results)
        {
            return TryOpenPremiumChest(drawCount, out results);
        }

        public int ResolvePremiumChestCost(int drawCount)
        {
            switch (drawCount)
            {
                case 5:
                    return Mathf.Max(1, Settings.fiveChestCost);
                case 10:
                    return Mathf.Max(1, Settings.tenChestCost);
                case 20:
                    return Mathf.Max(1, Settings.twentyChestCost);
                case 50:
                    return Mathf.Max(1, Settings.fiftyChestCost);
                case 100:
                    return Mathf.Max(1, Settings.hundredChestCost);
                default:
                    return Mathf.Max(1, Settings.singleChestCost) * Mathf.Max(1, drawCount);
            }
        }

        public bool IsDailyShopOfferPurchased(int offerIndex)
        {
            OutgameSaveData data = EnsureSaveData();
            EnsureDailyShopState(data);
            int flag = 1 << Mathf.Clamp(offerIndex, 0, 30);
            return (data.dailyShopPurchaseFlags & flag) != 0;
        }

        public bool TryPurchaseDailyShopOffer(int offerIndex, out List<OutgameDrawResult> results, out string message)
        {
            results = new List<OutgameDrawResult>();
            message = string.Empty;
            OutgameSaveData data = EnsureSaveData();
            EnsureDailyShopState(data);
            int safeIndex = Mathf.Clamp(offerIndex, 0, 2);
            int flag = 1 << safeIndex;
            if ((data.dailyShopPurchaseFlags & flag) != 0)
            {
                message = "오늘 이미 구매한 상품입니다.";
                return false;
            }

            if (safeIndex == 0)
            {
                int reward = Mathf.Max(1, Settings.dailyFreeGold);
                data.gold += reward;
                message = "일일 무료 선물 +" + reward.ToString("N0") + " GOLD";
            }
            else if (safeIndex == 1)
            {
                int cost = Mathf.Max(1, Settings.dailyCardPackGoldCost);
                if (data.gold < cost)
                {
                    message = "골드가 부족합니다.";
                    return false;
                }

                data.gold -= cost;
                DrawCardsInto(results, OutgameChestType.Earned, Mathf.Max(1, Settings.dailyCardPackDrawCount));
                message = "일일 영웅 카드 묶음을 구매했습니다.";
            }
            else
            {
                int cost = Mathf.Max(1, Settings.dailyPremiumPackDiamondCost);
                if (data.diamonds < cost)
                {
                    message = "다이아가 부족합니다.";
                    return false;
                }

                data.diamonds -= cost;
                DrawCardsInto(results, OutgameChestType.Premium, Mathf.Max(1, Settings.dailyPremiumPackDrawCount));
                message = "일일 프리미엄 묶음을 구매했습니다.";
            }

            data.dailyShopPurchaseFlags |= flag;
            Save();
            OnProgressChanged?.Invoke();
            return true;
        }

        public bool TryOpenPremiumChest(int drawCount, out List<OutgameDrawResult> results)
        {
            results = new List<OutgameDrawResult>();
            if (characterDatabase == null || drawCount <= 0)
            {
                return false;
            }

            int cost = ResolvePremiumChestCost(drawCount);
            OutgameSaveData data = EnsureSaveData();
            if (data.diamonds < cost)
            {
                return false;
            }

            data.diamonds -= cost;
            DrawCardsInto(results, OutgameChestType.Premium, drawCount);

            Save();
            OnProgressChanged?.Invoke();
            return results.Count > 0;
        }

        public bool TryOpenEarnedChest(out List<OutgameDrawResult> results)
        {
            results = new List<OutgameDrawResult>();
            OutgameSaveData data = EnsureSaveData();
            if (characterDatabase == null || data.earnedChestKeys <= 0)
            {
                return false;
            }

            data.earnedChestKeys--;
            OutgameDrawResult result = DrawCard(OutgameChestType.Earned);
            if (result != null)
            {
                results.Add(result);
            }

            Save();
            OnProgressChanged?.Invoke();
            return results.Count > 0;
        }

        public bool CycleWishlist()
        {
            if (characterDatabase == null)
            {
                return false;
            }

            List<CharacterDefinition> candidates = characterDatabase.Characters
                .Where(character => character != null)
                .OrderBy(character => character.grade)
                .ThenBy(character => character.id)
                .ToList();
            if (candidates.Count == 0)
            {
                return false;
            }

            OutgameSaveData data = EnsureSaveData();
            int currentIndex = candidates.FindIndex(character => character.id == data.wishlistCharacterId);
            int nextIndex = (currentIndex + 1) % candidates.Count;
            data.wishlistCharacterId = candidates[nextIndex].id;
            data.premiumWishlistPity = 0;
            Save();
            OnProgressChanged?.Invoke();
            return true;
        }

        public bool SetWishlistCharacter(string characterId)
        {
            CharacterDefinition target = characterDatabase != null
                ? characterDatabase.Characters.FirstOrDefault(character => character != null && character.id == characterId)
                : null;
            if (target == null)
            {
                return false;
            }

            OutgameSaveData data = EnsureSaveData();
            data.wishlistCharacterId = target.id;
            data.premiumWishlistPity = 0;
            Save();
            OnProgressChanged?.Invoke();
            return true;
        }

        public void GrantYahtzeeChestProgress(int progress)
        {
            if (progress <= 0)
            {
                return;
            }

            AddEarnedChestProgress(EnsureSaveData(), progress);
            Save();
            OnProgressChanged?.Invoke();
        }

        public string BuildChestEconomySummary()
        {
            OutgameSaveData data = EnsureSaveData();
            int nextHurdle = CommercialRoundPacing.GetNextHurdleRound(data.highestRoundReached);
            return "\ubb34\ub8cc \uc0c1\uc790 " + data.earnedChestKeys
                + "\uac1c  |  \uac8c\uc774\uc9c0 " + data.earnedChestProgress + "/" + Mathf.Max(1, Settings.earnedChestProgressTarget)
                + "  |  \ub2e4\uc74c \uc131\uc7a5 \ud5c8\ub4e4 R" + nextHurdle;
        }

        public string GetWishlistDisplayName()
        {
            CharacterDefinition wishlist = ResolveWishlistCharacter();
            return wishlist != null ? wishlist.displayName : "\ubbf8\uc124\uc815";
        }

        public bool IsOwned(string characterId)
        {
            OutgameCardRecord record = FindRecord(characterId);
            return record != null && record.level > 0;
        }

        public bool CanDeployCharacter(CharacterDefinition character)
        {
            return character != null && (IsTestMode || IsOwned(character.id));
        }

        public List<CharacterDefinition> GetCombatDeckCharacters()
        {
            EnsureCombatDeck();
            if (characterDatabase == null)
            {
                return new List<CharacterDefinition>();
            }

            return EnsureSaveData().combatDeckCharacterIds
                .Select(characterDatabase.GetCharacterById)
                .Where(CanDeployCharacter)
                .ToList();
        }

        public bool TrySetCombatDeckSlot(int slotIndex, string characterId)
        {
            if (slotIndex < 0 || slotIndex >= CombatDeckSlotCount || characterDatabase == null)
            {
                return false;
            }

            CharacterDefinition character = characterDatabase.GetCharacterById(characterId);
            if (!CanDeployCharacter(character))
            {
                return false;
            }

            EnsureCombatDeck();
            List<string> deck = EnsureSaveData().combatDeckCharacterIds;
            int existingIndex = deck.IndexOf(character.id);
            if (existingIndex >= 0 && existingIndex != slotIndex)
            {
                return false;
            }

            while (deck.Count <= slotIndex)
            {
                deck.Add(string.Empty);
            }

            deck[slotIndex] = character.id;
            NormalizeCombatDeck(deck);
            Save();
            OnProgressChanged?.Invoke();
            return true;
        }

        public bool ApplyCombatDeck(IEnumerable<string> characterIds)
        {
            if (characterDatabase == null)
            {
                return false;
            }

            List<string> next = characterIds == null ? new List<string>() : characterIds.ToList();
            NormalizeCombatDeck(next);
            if (next.Count == 0)
            {
                return false;
            }

            EnsureSaveData().combatDeckCharacterIds = next;
            Save();
            OnProgressChanged?.Invoke();
            return true;
        }

        public int GetCardLevel(string characterId)
        {
            OutgameCardRecord record = FindRecord(characterId);
            return record != null ? record.level : 0;
        }

        public int GetDisplayCardLevel(string characterId)
        {
            int level = GetCardLevel(characterId);
            if (level > 0)
            {
                return level;
            }

            return IsTestMode ? 1 : 0;
        }

        public string BuildProgressText(string characterId)
        {
            OutgameCardRecord record = FindRecord(characterId);
            if (record == null || record.level <= 0)
            {
                return IsTestMode ? "Lv.1  |  테스트 기본 보유" : "미획득  |  첫 카드 획득 시 해금";
            }

            if (record.level >= Settings.maxCardLevel)
            {
                return "Lv." + record.level + "  |  최대 성장";
            }

            return "Lv." + record.level + "  |  카드 " + record.upgradeCopies + "/" + RequiredCopiesForNextLevel(record.level);
        }

        public int CountUpgradeableCards()
        {
            List<OutgameCardRecord> records = EnsureSaveData().cards;
            int count = 0;
            for (int i = 0; i < records.Count; i++)
            {
                OutgameCardRecord record = records[i];
                if (record == null || record.level <= 0 || record.level >= Settings.maxCardLevel)
                {
                    continue;
                }

                if (record.upgradeCopies >= RequiredCopiesForNextLevel(record.level))
                {
                    count++;
                }
            }

            return count;
        }

        public string BuildCollectionSummary()
        {
            int total = characterDatabase != null ? characterDatabase.Characters.Count : 0;
            int owned = 0;
            if (IsTestMode)
            {
                owned = total;
            }
            else
            {
                List<OutgameCardRecord> records = EnsureSaveData().cards;
                for (int i = 0; i < records.Count; i++)
                {
                    if (records[i] != null && records[i].level > 0)
                    {
                        owned++;
                    }
                }
            }

            string prefix = IsTestMode ? "전체 보유 영웅 " : "보유 영웅 ";
            return prefix + owned + "/" + total + "  |  평균 성장 Lv." + GetAverageGrowthLevel().ToString("0.0");
        }

        public string BuildRateText()
        {
            return "\ubb34\ub8cc: " + BuildLegacyRateText()
                + "  |  " + Mathf.Max(1, Settings.earnedChestRarePityDraws) + "\ud68c \ub0b4 \ub808\uc5b4+ / "
                + Mathf.Max(1, Settings.earnedChestEpicPityDraws) + "\ud68c \ub0b4 \ud76c\uadc0+"
                + "\n\ud504\ub9ac\ubbf8\uc5c4: \uc77c\ubc18 " + FormatPercent(Settings.premiumNormalRate)
                + "  \ub808\uc5b4 " + FormatPercent(Settings.premiumRareRate)
                + "  \ud76c\uadc0 " + FormatPercent(Settings.premiumEpicRate)
                + "  |  10\ud68c \ub0b4 \ud76c\uadc0+ / 40\ud68c \ub0b4 \uc804\uc124+ / \uc704\uc2dc \ubcf4\uc815";
        }

        private string BuildLegacyRateText()
        {
            return "일반 " + FormatPercent(Settings.normalRate) +
                   "  레어 " + FormatPercent(Settings.rareRate) +
                   "  희귀 " + FormatPercent(Settings.epicRate) +
                   "  전설 " + FormatPercent(Settings.legendaryRate) +
                   "  신화 " + FormatPercent(Settings.mythicRate) +
                   "  초월 " + FormatPercent(Settings.transcendentRate);
        }

        public void ApplyGrowthToDefender(DefenderUnit unit, CharacterDefinition definition)
        {
            if (unit == null || definition == null)
            {
                return;
            }

            int growthLevel = Mathf.Max(0, GetCardLevel(definition.id) - 1);
            unit.ApplyOutgameGrowth(growthLevel, Settings.attackPowerPerGrowthLevel, Settings.maxHealthPerGrowthLevel);
        }

        public void ResolveMonsterBalanceMultipliers(MonsterDefinition monster, out float healthMultiplier, out float attackMultiplier)
        {
            healthMultiplier = 1f;
            attackMultiplier = 1f;
            if (!Settings.scaleMonstersWithCollectionGrowth || monster == null)
            {
                return;
            }

            float averageGrowthLevel = GetAverageGrowthLevel();
            bool isBoss = monster.IsBossLike;
            float healthBonus = averageGrowthLevel * (isBoss ? Settings.bossHealthPerAverageGrowthLevel : Settings.regularHealthPerAverageGrowthLevel);
            float attackBonus = averageGrowthLevel * (isBoss ? Settings.bossAttackPerAverageGrowthLevel : Settings.regularAttackPerAverageGrowthLevel);
            healthMultiplier += Mathf.Min(healthBonus, Mathf.Min(Settings.maxMonsterHealthBonus, 0.15f));
            attackMultiplier += Mathf.Min(attackBonus, Mathf.Min(Settings.maxMonsterAttackBonus, 0.10f));
        }

        private string GrantCommercialBattleRewards(OutgameSaveData data, int round, bool victory)
        {
            int safeRound = Mathf.Max(0, round);
            bool newHighestRound = victory && safeRound > data.highestRoundReached;
            if (newHighestRound)
            {
                data.highestRoundReached = safeRound;
            }

            int progress = victory
                ? 8 + Mathf.Min(50, safeRound) / 4
                : 6 + Mathf.Min(50, safeRound) / 5;
            if (newHighestRound)
            {
                progress += 3;
            }

            if (victory && safeRound > 0 && safeRound % 10 == 0)
            {
                progress += 25;
            }

            if (victory && CommercialRoundPacing.IsMajorHurdleRound(safeRound))
            {
                int hurdleIndex = Mathf.Clamp((safeRound - CommercialRoundPacing.FirstHurdleRound) / CommercialRoundPacing.HurdleInterval, 0, 30);
                int flag = 1 << hurdleIndex;
                if ((data.hurdleClearRewardFlags & flag) == 0)
                {
                    data.hurdleClearRewardFlags |= flag;
                    progress += 50;
                }
            }

            int supportKeys = 0;
            if (!victory && CommercialRoundPacing.TryGetApproachingHurdleIndex(safeRound, out int supportIndex))
            {
                supportIndex = Mathf.Clamp(supportIndex, 0, 30);
                int supportFlag = 1 << supportIndex;
                if ((data.hurdleFailureSupportFlags & supportFlag) == 0)
                {
                    data.hurdleFailureSupportFlags |= supportFlag;
                    supportKeys = Mathf.Max(0, Settings.hurdleFailureSupportChestKeys);
                    data.earnedChestKeys += supportKeys;
                }
            }

            int progressKeys = AddEarnedChestProgress(data, progress);
            int totalKeys = progressKeys + supportKeys;
            int goldReward = victory ? 60 + safeRound * 12 : 35 + safeRound * 8;
            data.gold += Mathf.Max(0, goldReward);
            string summary = "상점 골드 +" + goldReward.ToString("N0")
                + " / \ubb34\ub8cc \uc0c1\uc790 \uac8c\uc774\uc9c0 +" + progress + " (" + data.earnedChestProgress + "/" + Mathf.Max(1, Settings.earnedChestProgressTarget) + ")";
            if (totalKeys > 0)
            {
                summary += " / \uc0c1\uc790 +" + totalKeys;
            }

            if (supportKeys > 0)
            {
                summary += " / \uccab \ud5c8\ub4e4 \uc2e4\ud328 \uc9c0\uc6d0";
            }

            return summary;
        }

        private int AddEarnedChestProgress(OutgameSaveData data, int progress)
        {
            if (data == null || progress <= 0)
            {
                return 0;
            }

            int target = Mathf.Max(1, Settings.earnedChestProgressTarget);
            data.earnedChestProgress = Mathf.Max(0, data.earnedChestProgress) + progress;
            int gainedKeys = data.earnedChestProgress / target;
            if (gainedKeys > 0)
            {
                data.earnedChestKeys += gainedKeys;
                data.earnedChestProgress %= target;
            }

            return gainedKeys;
        }

        private void DrawCardsInto(List<OutgameDrawResult> results, OutgameChestType chestType, int drawCount)
        {
            if (results == null)
            {
                return;
            }

            int safeCount = Mathf.Clamp(drawCount, 0, 100);
            for (int i = 0; i < safeCount; i++)
            {
                OutgameDrawResult result = DrawCard(chestType);
                if (result != null)
                {
                    results.Add(result);
                }
            }
        }

        private void EnsureDailyShopState(OutgameSaveData data)
        {
            if (data == null)
            {
                return;
            }

            System.DateTime now = System.DateTime.Now;
            int dateKey = now.Year * 10000 + now.Month * 100 + now.Day;
            if (data.dailyShopDate == dateKey)
            {
                return;
            }

            data.dailyShopDate = dateKey;
            data.dailyShopPurchaseFlags = 0;
            Save();
        }

        public string BuildDailyShopResetLabel()
        {
            System.DateTime now = System.DateTime.Now;
            System.TimeSpan remaining = now.Date.AddDays(1) - now;
            return "일일 상품 갱신까지 " + Mathf.Max(0, remaining.Hours).ToString("00") + ":" + Mathf.Max(0, remaining.Minutes).ToString("00");
        }


        private OutgameDrawResult DrawCard(OutgameChestType chestType)
        {
            OutgameSaveData data = EnsureSaveData();
            CharacterGrade minimumGrade = ResolvePityMinimumGrade(data, chestType);
            bool pityTriggered = (int)minimumGrade > (int)CharacterGrade.Normal;
            CharacterDefinition character = ResolveDrawCharacter(data, chestType, minimumGrade, out bool wishlistHit);
            if (character == null)
            {
                character = characterDatabase.GetRandomSummonableCharacter();
            }

            if (character == null)
            {
                return null;
            }

            UpdateChestPity(data, chestType, character.grade, wishlistHit);
            OutgameCardRecord record = GetOrCreateRecord(character.id);
            bool wasOwned = record.level > 0;
            int previousLevel = record.level;
            record.totalCopies++;
            record.upgradeCopies++;
            ApplyAvailableLevelUps(record);

            return new OutgameDrawResult
            {
                character = character,
                firstAcquisition = !wasOwned && record.level > 0,
                leveledUp = record.level > previousLevel && wasOwned,
                level = record.level,
                remainingCopies = record.upgradeCopies,
                requiredCopies = record.level < Settings.maxCardLevel ? RequiredCopiesForNextLevel(record.level) : 0,
                chestType = chestType,
                wishlistHit = wishlistHit,
                pityTriggered = pityTriggered
            };
        }

        private CharacterDefinition ResolveDrawCharacter(
            OutgameSaveData data,
            OutgameChestType chestType,
            CharacterGrade minimumGrade,
            out bool wishlistHit)
        {
            wishlistHit = false;
            CharacterDefinition wishlist = chestType == OutgameChestType.Premium ? ResolveWishlistCharacter() : null;
            if (wishlist != null && (int)wishlist.grade >= (int)minimumGrade)
            {
                int wishlistPityTarget = Mathf.Max(1, Settings.premiumWishlistPityDraws);
                bool guaranteedWishlist = data.premiumWishlistPity >= wishlistPityTarget - 1;
                bool randomWishlist = Random.value < Mathf.Clamp01(Settings.premiumWishlistChance);
                if (guaranteedWishlist || randomWishlist)
                {
                    wishlistHit = true;
                    return wishlist;
                }
            }

            CharacterGrade rolledGrade = chestType == OutgameChestType.Premium ? RollPremiumGrade() : RollGrade();
            if ((int)rolledGrade < (int)minimumGrade)
            {
                rolledGrade = minimumGrade;
            }

            return characterDatabase.GetRandomCharacterByGradeOrLower(rolledGrade);
        }

        private CharacterGrade ResolvePityMinimumGrade(OutgameSaveData data, OutgameChestType chestType)
        {
            if (chestType == OutgameChestType.Earned)
            {
                if (data.earnedEpicPity >= Mathf.Max(1, Settings.earnedChestEpicPityDraws) - 1)
                {
                    return CharacterGrade.Epic;
                }

                if (data.earnedRarePity >= Mathf.Max(1, Settings.earnedChestRarePityDraws) - 1)
                {
                    return CharacterGrade.Rare;
                }

                return CharacterGrade.Normal;
            }

            if (data.premiumLegendaryPity >= Mathf.Max(1, Settings.premiumChestLegendaryPityDraws) - 1)
            {
                return CharacterGrade.Legendary;
            }

            if (data.premiumEpicPity >= Mathf.Max(1, Settings.premiumChestEpicPityDraws) - 1)
            {
                return CharacterGrade.Epic;
            }

            return CharacterGrade.Normal;
        }

        private void UpdateChestPity(OutgameSaveData data, OutgameChestType chestType, CharacterGrade grade, bool wishlistHit)
        {
            if (chestType == OutgameChestType.Earned)
            {
                data.earnedRarePity = (int)grade >= (int)CharacterGrade.Rare ? 0 : IncrementPity(data.earnedRarePity);
                data.earnedEpicPity = (int)grade >= (int)CharacterGrade.Epic ? 0 : IncrementPity(data.earnedEpicPity);
                return;
            }

            data.premiumEpicPity = (int)grade >= (int)CharacterGrade.Epic ? 0 : IncrementPity(data.premiumEpicPity);
            data.premiumLegendaryPity = (int)grade >= (int)CharacterGrade.Legendary ? 0 : IncrementPity(data.premiumLegendaryPity);
            data.premiumWishlistPity = wishlistHit ? 0 : IncrementPity(data.premiumWishlistPity);
        }

        private CharacterDefinition ResolveWishlistCharacter()
        {
            string wishlistId = EnsureSaveData().wishlistCharacterId;
            return characterDatabase != null && !string.IsNullOrWhiteSpace(wishlistId)
                ? characterDatabase.Characters.FirstOrDefault(character => character != null && character.id == wishlistId)
                : null;
        }

        private static int IncrementPity(int value)
        {
            return value >= 1000000 ? 1000000 : Mathf.Max(0, value) + 1;
        }

        private void EnsureInitialRoster()
        {
            OutgameSaveData data = EnsureSaveData();
            if (data.initialRosterGranted)
            {
                return;
            }

            if (!IsTestMode && characterDatabase != null)
            {
                int granted = 0;
                List<string> starterIds = Settings.serviceStarterCharacterIds;
                if (starterIds != null)
                {
                    for (int i = 0; i < starterIds.Count && granted < Settings.serviceStarterCharacterCount; i++)
                    {
                        CharacterDefinition starter = characterDatabase.Characters.FirstOrDefault(character => character != null && character.id == starterIds[i]);
                        if (starter != null)
                        {
                            GrantInitialCard(starter);
                            granted++;
                        }
                    }
                }

                for (int i = 0; i < characterDatabase.Characters.Count && granted < Settings.serviceStarterCharacterCount; i++)
                {
                    CharacterDefinition fallback = characterDatabase.Characters[i];
                    if (fallback != null && !IsOwned(fallback.id))
                    {
                        GrantInitialCard(fallback);
                        granted++;
                    }
                }
            }

            data.initialRosterGranted = true;
            Save();
        }

        private void EnsureCombatDeck()
        {
            OutgameSaveData data = EnsureSaveData();
            List<string> deck = data.combatDeckCharacterIds ?? new List<string>();
            List<string> before = new List<string>(deck);
            NormalizeCombatDeck(deck);
            bool changed = data.combatDeckCharacterIds == null || !before.SequenceEqual(deck);
            data.combatDeckCharacterIds = deck;
            if (changed)
            {
                Save();
            }
        }

        private void NormalizeCombatDeck(List<string> deck)
        {
            if (deck == null)
            {
                return;
            }

            HashSet<string> used = new HashSet<string>();
            for (int index = deck.Count - 1; index >= 0; index--)
            {
                CharacterDefinition character = characterDatabase != null ? characterDatabase.GetCharacterById(deck[index]) : null;
                if (!CanDeployCharacter(character) || !used.Add(character.id))
                {
                    deck.RemoveAt(index);
                }
            }

            if (characterDatabase == null)
            {
                return;
            }

            foreach (CharacterDefinition character in characterDatabase.GetDeployableCharacters())
            {
                if (deck.Count >= CombatDeckSlotCount)
                {
                    break;
                }

                if (used.Add(character.id))
                {
                    deck.Add(character.id);
                }
            }
        }

        private void GrantInitialCard(CharacterDefinition character)
        {
            OutgameCardRecord record = GetOrCreateRecord(character.id);
            if (record.level > 0)
            {
                return;
            }

            int copies = Mathf.Max(1, Settings.initialUnlockCopies);
            record.totalCopies += copies;
            record.upgradeCopies += copies;
            ApplyAvailableLevelUps(record);
        }

        private CharacterGrade RollGrade()
        {
            float total = Settings.normalRate + Settings.rareRate + Settings.epicRate +
                          Settings.legendaryRate + Settings.mythicRate + Settings.transcendentRate;
            float roll = Random.value * Mathf.Max(0.001f, total);
            if ((roll -= Settings.normalRate) < 0f) return CharacterGrade.Normal;
            if ((roll -= Settings.rareRate) < 0f) return CharacterGrade.Rare;
            if ((roll -= Settings.epicRate) < 0f) return CharacterGrade.Epic;
            if ((roll -= Settings.legendaryRate) < 0f) return CharacterGrade.Legendary;
            if ((roll -= Settings.mythicRate) < 0f) return CharacterGrade.Mythic;
            return CharacterGrade.Transcendent;
        }

        private CharacterGrade RollPremiumGrade()
        {
            float total = Settings.premiumNormalRate + Settings.premiumRareRate + Settings.premiumEpicRate +
                          Settings.premiumLegendaryRate + Settings.premiumMythicRate + Settings.premiumTranscendentRate;
            float roll = Random.value * Mathf.Max(0.001f, total);
            if ((roll -= Settings.premiumNormalRate) < 0f) return CharacterGrade.Normal;
            if ((roll -= Settings.premiumRareRate) < 0f) return CharacterGrade.Rare;
            if ((roll -= Settings.premiumEpicRate) < 0f) return CharacterGrade.Epic;
            if ((roll -= Settings.premiumLegendaryRate) < 0f) return CharacterGrade.Legendary;
            if ((roll -= Settings.premiumMythicRate) < 0f) return CharacterGrade.Mythic;
            return CharacterGrade.Transcendent;
        }


        private void ApplyAvailableLevelUps(OutgameCardRecord record)
        {
            while (record.level < Settings.maxCardLevel)
            {
                int required = record.level == 0 ? Mathf.Max(1, Settings.initialUnlockCopies) : RequiredCopiesForNextLevel(record.level);
                if (record.upgradeCopies < required)
                {
                    break;
                }

                record.upgradeCopies -= required;
                record.level++;
            }
        }

        private int RequiredCopiesForNextLevel(int currentLevel)
        {
            return Mathf.Max(1, Settings.duplicateCopiesForLevelTwo + Mathf.Max(0, currentLevel - 1) * Settings.additionalCopiesPerLevel);
        }

        private float GetAverageGrowthLevel()
        {
            List<OutgameCardRecord> records = EnsureSaveData().cards;
            float totalGrowth = 0f;
            int ownedCount = 0;
            for (int i = 0; i < records.Count; i++)
            {
                OutgameCardRecord record = records[i];
                if (record == null || record.level <= 0)
                {
                    continue;
                }

                totalGrowth += Mathf.Max(0, record.level - 1);
                ownedCount++;
            }

            return ownedCount > 0 ? totalGrowth / ownedCount : 0f;
        }

        private OutgameCardRecord FindRecord(string characterId)
        {
            List<OutgameCardRecord> records = EnsureSaveData().cards;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i] != null && records[i].characterId == characterId)
                {
                    return records[i];
                }
            }

            return null;
        }

        private OutgameCardRecord GetOrCreateRecord(string characterId)
        {
            OutgameCardRecord record = FindRecord(characterId);
            if (record != null)
            {
                return record;
            }

            record = new OutgameCardRecord { characterId = characterId };
            EnsureSaveData().cards.Add(record);
            return record;
        }

        private OutgameSaveData EnsureSaveData()
        {
            if (saveData == null)
            {
                Load();
            }

            return saveData;
        }

        private void Load()
        {
            string json = PlayerPrefs.GetString(ResolveSaveKey(), string.Empty);
            int initialGold = IsTestMode ? Settings.testStartingGold : Settings.startingGold;
            int initialDiamonds = IsTestMode ? Settings.testStartingDiamonds : Settings.startingDiamonds;
            saveData = string.IsNullOrEmpty(json) ? new OutgameSaveData { gold = initialGold, diamonds = initialDiamonds } : JsonUtility.FromJson<OutgameSaveData>(json);
            if (saveData == null)
            {
                saveData = new OutgameSaveData { gold = initialGold, diamonds = initialDiamonds };
            }

            if (saveData.cards == null)
            {
                saveData.cards = new List<OutgameCardRecord>();
            }
            if (saveData.combatDeckCharacterIds == null)
            {
                saveData.combatDeckCharacterIds = new List<string>();
            }
            int previousVersion = saveData.metaProgressionVersion;
            int targetVersion = Mathf.Max(3, Settings.progressionVersion);
            bool migrated = previousVersion < targetVersion;
            if (previousVersion < 2)
            {
                int migrationKeys = string.IsNullOrEmpty(json)
                    ? Mathf.Max(0, Settings.startingEarnedChestKeys)
                    : Mathf.Max(0, Settings.migrationEarnedChestKeys);
                saveData.earnedChestKeys = Mathf.Max(0, saveData.earnedChestKeys) + migrationKeys;
            }

            if (previousVersion < 3)
            {
                saveData.gold = Mathf.Max(0, saveData.gold) + initialGold;
            }

            saveData.metaProgressionVersion = targetVersion;
            saveData.gold = Mathf.Max(0, saveData.gold);

            saveData.earnedChestKeys = Mathf.Max(0, saveData.earnedChestKeys);
            saveData.earnedChestProgress = Mathf.Max(0, saveData.earnedChestProgress);
            int progressTarget = Mathf.Max(1, Settings.earnedChestProgressTarget);
            if (saveData.earnedChestProgress >= progressTarget)
            {
                saveData.earnedChestKeys += saveData.earnedChestProgress / progressTarget;
                saveData.earnedChestProgress %= progressTarget;
                migrated = true;
            }

            EnsureDailyShopState(saveData);

            EnsureCurrentSeason(saveData);
            lastSeasonRewardSummary = string.Empty;
            if (migrated)
            {
                PlayerPrefs.SetString(ResolveSaveKey(), JsonUtility.ToJson(saveData));
                PlayerPrefs.Save();
            }

        }

        private void Save()
        {
            PlayerPrefs.SetString(ResolveSaveKey(), JsonUtility.ToJson(EnsureSaveData()));
            PlayerPrefs.Save();
        }

        private string ResolveSaveKey()
        {
            return IsTestMode ? TestSaveKey : ServiceSaveKey;
        }

        private static string FormatPercent(float value)
        {
            return (value * 100f).ToString(value * 100f < 1f ? "0.0" : "0.#") + "%";
        }
    }
}
