using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenseGame
{
    public class CharacterDatabase : MonoBehaviour
    {
        [SerializeField] private List<CharacterDefinition> characters = new List<CharacterDefinition>();
        [SerializeField] private bool generateStarterCharacters = true;
        [SerializeField] private int starterCharacterCount = 30;

        private static readonly string[] NormalNames =
        {
            "Stone Guard", "Wind Archer", "Copper Gunner", "Lantern Mage", "Oak Fighter", "Wave Scout",
            "Dust Spear", "Iron Brawler", "Torch Adept", "Field Medic", "Mist Hunter", "Hammer Kid"
        };

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

        public IReadOnlyList<CharacterDefinition> Characters => characters;

        private void Awake()
        {
            if (generateStarterCharacters && characters.Count == 0)
            {
                GenerateStarterCharacters(starterCharacterCount);
            }
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

            characters.AddRange(roster.Take(Mathf.Max(1, totalCount)));

            if (totalCount > roster.Count)
            {
                for (int i = roster.Count; i < totalCount; i++)
                {
                    CharacterGrade grade = ResolveStarterGrade(i, totalCount);
                    characters.Add(CreateDefinition($"Hero {i + 1:D2}", grade, i));
                }
            }
        }

        public List<CharacterDefinition> GetCharactersByGrade(CharacterGrade grade)
        {
            return characters.Where(c => c.grade == grade).ToList();
        }

        public CharacterDefinition GetRandomCharacterByGrade(CharacterGrade grade)
        {
            List<CharacterDefinition> candidates = GetCharactersByGrade(grade);
            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        public CharacterDefinition GetRandomSummonableCharacter()
        {
            float roll = Random.value;
            CharacterGrade grade = CharacterGrade.Mythic;

            if (roll < 0.62f) grade = CharacterGrade.Normal;
            else if (roll < 0.86f) grade = CharacterGrade.Rare;
            else if (roll < 0.96f) grade = CharacterGrade.Epic;
            else if (roll < 0.995f) grade = CharacterGrade.Legendary;

            return GetRandomCharacterByGrade(grade);
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
            CharacterRole role = ResolveRole(seed);
            CharacterDefinition definition = new CharacterDefinition();
            definition.id = $"hero_{seed + 1:D2}";
            definition.displayName = name;
            definition.description = BuildDescription(name, grade, role);
            definition.grade = grade;
            definition.role = role;
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
            stats.attackRange = 5.25f + gradeIndex * 0.35f;
            stats.projectileSpeed = 11f + gradeIndex * 1.5f;
            stats.moveSpeed = 0f;

            if (role == CharacterRole.Vanguard)
            {
                stats.maxHealth *= 1.35f;
                stats.attackRange -= 1.25f;
                stats.attackPower *= 1.1f;
            }
            else if (role == CharacterRole.Ranger)
            {
                stats.attackRange += 2.25f;
                stats.attackSpeed *= 1.1f;
            }
            else if (role == CharacterRole.Mage)
            {
                stats.attackPower *= 1.25f;
                stats.maxMana *= 1.2f;
            }
            else if (role == CharacterRole.Support)
            {
                stats.maxHealth *= 1.1f;
                stats.attackSpeed *= 0.92f;
                stats.maxMana *= 1.35f;
            }
            else if (role == CharacterRole.Assassin)
            {
                stats.criticalChance += 0.14f;
                stats.criticalDamageMultiplier += 0.35f;
                stats.attackSpeed *= 1.2f;
                stats.maxHealth *= 0.85f;
            }
            else if (role == CharacterRole.Summoner)
            {
                stats.attackPower *= 0.95f;
                stats.maxMana *= 1.45f;
                stats.attackRange += 0.8f;
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
            if (role == CharacterRole.Vanguard) return new List<SkillEffectType> { SkillEffectType.DirectDamage, SkillEffectType.HealSelf, SkillEffectType.AreaDamage };
            if (role == CharacterRole.Ranger) return new List<SkillEffectType> { SkillEffectType.MultiShot, SkillEffectType.AttackSpeedBoost, SkillEffectType.DirectDamage };
            if (role == CharacterRole.Mage) return new List<SkillEffectType> { SkillEffectType.AreaDamage, SkillEffectType.ManaSurge, SkillEffectType.DirectDamage };
            if (role == CharacterRole.Support) return new List<SkillEffectType> { SkillEffectType.HealSelf, SkillEffectType.AttackSpeedBoost, SkillEffectType.CriticalBoost };
            if (role == CharacterRole.Assassin) return new List<SkillEffectType> { SkillEffectType.Execute, SkillEffectType.CriticalBoost, SkillEffectType.DirectDamage };
            return new List<SkillEffectType> { SkillEffectType.SummonRush, SkillEffectType.ManaSurge, SkillEffectType.AreaDamage };
        }

        private SkillDefinition CreateSkill(string ownerName, CharacterRole role, SkillEffectType effectType, int index)
        {
            SkillDefinition skill = new SkillDefinition();
            skill.id = $"{ownerName}_{effectType}_{index}";
            skill.effectType = effectType;
            skill.manaThreshold = role == CharacterRole.Support ? 80f : 100f;
            skill.hitCount = effectType == SkillEffectType.MultiShot ? 3 : 1;
            skill.radius = 2.6f;

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
                skill.displayName = "Spirit Rush";
                skill.description = "Sends phantom strikes through several monsters.";
                skill.power = 0.95f;
                skill.hitCount = 4;
                skill.cooldown = 8f;
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
    }
}
