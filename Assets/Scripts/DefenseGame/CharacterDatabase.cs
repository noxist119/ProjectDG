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

        public IReadOnlyList<CharacterDefinition> Characters => characters;

        private static readonly SkillEffectType[] CharacterSkillPattern =
        {
            SkillEffectType.DirectDamage,
            SkillEffectType.AreaDamage,
            SkillEffectType.HealSelf,
            SkillEffectType.AttackSpeedBoost,
            SkillEffectType.CriticalBoost,
            SkillEffectType.ManaSurge
        };

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

            for (int i = 0; i < totalCount; i++)
            {
                CharacterGrade grade = ResolveStarterGrade(i, totalCount);
                int gradeIndex = (int)grade;

                CharacterDefinition definition = new CharacterDefinition
                {
                    id = $"hero_{i + 1:D2}",
                    displayName = $"Hero {i + 1:D2}",
                    grade = grade,
                    mergeValue = 1 + gradeIndex,
                    stats = new CombatStats
                    {
                        maxHealth = 90f + gradeIndex * 35f + i * 2f,
                        attackPower = 10f + gradeIndex * 6f + (i % 4),
                        criticalChance = Mathf.Clamp01(0.08f + gradeIndex * 0.04f),
                        criticalDamageMultiplier = 1.5f + gradeIndex * 0.1f,
                        attackSpeed = 1f + gradeIndex * 0.12f,
                        maxMana = 100f + gradeIndex * 15f,
                        attackRange = 5.5f + gradeIndex * 0.4f,
                        moveSpeed = 0f,
                        projectileSpeed = 12f + gradeIndex
                    }
                };

                definition.skills = BuildSkills(definition.displayName, grade, false, i);
                characters.Add(definition);
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

        private List<SkillDefinition> BuildSkills(string ownerName, CharacterGrade grade, bool bossOverride, int seed)
        {
            int count = GradeRules.GetSkillCount(grade, bossOverride);
            List<SkillDefinition> result = new List<SkillDefinition>(count);

            for (int i = 0; i < count; i++)
            {
                SkillEffectType effectType = CharacterSkillPattern[(seed + i) % CharacterSkillPattern.Length];
                result.Add(CreateSkill(ownerName, effectType, i));
            }

            return result;
        }

        private SkillDefinition CreateSkill(string ownerName, SkillEffectType effectType, int index)
        {
            SkillDefinition skill = new SkillDefinition();
            skill.effectType = effectType;
            skill.manaThreshold = 100f;

            if (effectType == SkillEffectType.DirectDamage)
            {
                skill.id = $"{ownerName}_direct_{index}";
                skill.displayName = "Power Shot";
                skill.description = "Deals heavy damage to the current target.";
                skill.power = 1.9f;
                skill.cooldown = 5f;
            }
            else if (effectType == SkillEffectType.AreaDamage)
            {
                skill.id = $"{ownerName}_area_{index}";
                skill.displayName = "Burst Wave";
                skill.description = "Deals splash damage to nearby enemies.";
                skill.power = 1.25f;
                skill.radius = 2.8f;
                skill.cooldown = 6f;
            }
            else if (effectType == SkillEffectType.HealSelf)
            {
                skill.id = $"{ownerName}_heal_{index}";
                skill.displayName = "Battle Recovery";
                skill.description = "Restores a portion of max health.";
                skill.power = 0.28f;
                skill.cooldown = 8f;
            }
            else if (effectType == SkillEffectType.AttackSpeedBoost)
            {
                skill.id = $"{ownerName}_aspd_{index}";
                skill.displayName = "Rapid Fire";
                skill.description = "Temporarily boosts attack speed.";
                skill.power = 0.45f;
                skill.duration = 5f;
                skill.cooldown = 9f;
            }
            else if (effectType == SkillEffectType.CriticalBoost)
            {
                skill.id = $"{ownerName}_crit_{index}";
                skill.displayName = "Focus Sight";
                skill.description = "Temporarily boosts critical chance.";
                skill.power = 0.25f;
                skill.duration = 5f;
                skill.cooldown = 9f;
            }
            else
            {
                skill.id = $"{ownerName}_mana_{index}";
                skill.displayName = "Mana Flow";
                skill.description = "Instantly recovers mana for faster casting.";
                skill.effectType = SkillEffectType.ManaSurge;
                skill.power = 0.45f;
                skill.cooldown = 7f;
            }

            return skill;
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

