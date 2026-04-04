using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenseGame
{
    public class MonsterDatabase : MonoBehaviour
    {
        [SerializeField] private List<MonsterDefinition> monsters = new List<MonsterDefinition>();
        [SerializeField] private List<MonsterDefinition> bosses = new List<MonsterDefinition>();
        [SerializeField] private bool generateStarterMonsters = true;
        [SerializeField] private int starterMonsterCount = 20;

        public IReadOnlyList<MonsterDefinition> Monsters => monsters;
        public IReadOnlyList<MonsterDefinition> Bosses => bosses;

        private static readonly SkillEffectType[] MonsterSkillPattern =
        {
            SkillEffectType.DirectDamage,
            SkillEffectType.AreaDamage,
            SkillEffectType.HealSelf,
            SkillEffectType.MoveSpeedBoost,
            SkillEffectType.CriticalBoost,
            SkillEffectType.ManaSurge
        };

        private void Awake()
        {
            if (generateStarterMonsters && monsters.Count == 0 && bosses.Count == 0)
            {
                GenerateStarterMonsters(starterMonsterCount);
            }
        }

        public void GenerateStarterMonsters(int totalCount)
        {
            monsters.Clear();
            bosses.Clear();

            for (int i = 0; i < totalCount; i++)
            {
                CharacterGrade grade = ResolveGrade(i, totalCount);
                int gradeIndex = (int)grade;
                MonsterDefinition definition = new MonsterDefinition
                {
                    id = $"mob_{i + 1:D2}",
                    displayName = $"Monster {i + 1:D2}",
                    grade = grade,
                    rewardGold = 4 + gradeIndex * 2,
                    stats = new CombatStats
                    {
                        maxHealth = 60f + gradeIndex * 30f + i * 2f,
                        attackPower = 7f + gradeIndex * 5f,
                        criticalChance = Mathf.Clamp01(0.05f + gradeIndex * 0.03f),
                        criticalDamageMultiplier = 1.5f + gradeIndex * 0.08f,
                        attackSpeed = 0.8f + gradeIndex * 0.08f,
                        maxMana = 100f + gradeIndex * 12f,
                        attackRange = 1.3f + gradeIndex * 0.1f,
                        moveSpeed = 1.3f + gradeIndex * 0.08f,
                        projectileSpeed = 0f
                    }
                };

                definition.skills = BuildSkills(definition.displayName, grade, false, i);
                monsters.Add(definition);
            }

            for (int i = 0; i < 5; i++)
            {
                MonsterDefinition boss = new MonsterDefinition
                {
                    id = $"boss_{i + 1:D2}",
                    displayName = $"Boss {i + 1:D2}",
                    grade = i < 2 ? CharacterGrade.Legendary : CharacterGrade.Mythic,
                    isBoss = true,
                    rewardGold = 40 + i * 10,
                    stats = new CombatStats
                    {
                        maxHealth = 1200f + i * 450f,
                        attackPower = 30f + i * 8f,
                        criticalChance = 0.15f + i * 0.02f,
                        criticalDamageMultiplier = 1.8f,
                        attackSpeed = 0.9f + i * 0.06f,
                        maxMana = 120f,
                        attackRange = 1.8f,
                        moveSpeed = 1.2f + i * 0.05f,
                        projectileSpeed = 0f
                    }
                };

                boss.skills = BuildSkills(boss.displayName, boss.grade, true, i + 100);
                bosses.Add(boss);
            }
        }

        public MonsterDefinition GetRandomMonsterForRound(int round)
        {
            CharacterGrade maxGrade = CharacterGrade.Mythic;
            if (round <= 3) maxGrade = CharacterGrade.Normal;
            else if (round <= 6) maxGrade = CharacterGrade.Rare;
            else if (round <= 9) maxGrade = CharacterGrade.Epic;
            else if (round <= 14) maxGrade = CharacterGrade.Legendary;

            List<MonsterDefinition> candidates = monsters.Where(m => m.grade <= maxGrade).ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        public MonsterDefinition GetBossForRound(int round)
        {
            if (bosses.Count == 0)
            {
                return null;
            }

            int index = Mathf.Clamp((round / 10) - 1, 0, bosses.Count - 1);
            return bosses[index];
        }

        private List<SkillDefinition> BuildSkills(string ownerName, CharacterGrade grade, bool bossOverride, int seed)
        {
            int count = GradeRules.GetSkillCount(grade, bossOverride);
            List<SkillDefinition> result = new List<SkillDefinition>(count);

            for (int i = 0; i < count; i++)
            {
                SkillEffectType effectType = MonsterSkillPattern[(seed + i) % MonsterSkillPattern.Length];
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
                skill.id = $"{ownerName}_claw_{index}";
                skill.displayName = "Savage Strike";
                skill.description = "Heavy damage to a single defender.";
                skill.power = 1.7f;
                skill.cooldown = 5f;
            }
            else if (effectType == SkillEffectType.AreaDamage)
            {
                skill.id = $"{ownerName}_roar_{index}";
                skill.displayName = "Crushing Roar";
                skill.description = "Damages nearby defenders.";
                skill.power = 1.1f;
                skill.radius = 2.5f;
                skill.cooldown = 7f;
            }
            else if (effectType == SkillEffectType.HealSelf)
            {
                skill.id = $"{ownerName}_regen_{index}";
                skill.displayName = "Dark Regeneration";
                skill.description = "Recovers part of its max health.";
                skill.power = 0.22f;
                skill.cooldown = 8f;
            }
            else if (effectType == SkillEffectType.MoveSpeedBoost)
            {
                skill.id = $"{ownerName}_charge_{index}";
                skill.displayName = "Rush";
                skill.description = "Temporarily increases move speed.";
                skill.power = 0.5f;
                skill.duration = 4f;
                skill.cooldown = 9f;
            }
            else if (effectType == SkillEffectType.CriticalBoost)
            {
                skill.id = $"{ownerName}_frenzy_{index}";
                skill.displayName = "Frenzy";
                skill.description = "Temporarily increases critical chance.";
                skill.power = 0.2f;
                skill.duration = 5f;
                skill.cooldown = 9f;
            }
            else
            {
                skill.id = $"{ownerName}_mana_{index}";
                skill.displayName = "Mana Hunger";
                skill.description = "Quickly restores mana.";
                skill.effectType = SkillEffectType.ManaSurge;
                skill.power = 0.4f;
                skill.cooldown = 6f;
            }

            return skill;
        }

        private CharacterGrade ResolveGrade(int index, int totalCount)
        {
            float ratio = totalCount <= 1 ? 1f : (float)index / (totalCount - 1);
            if (ratio < 0.35f) return CharacterGrade.Normal;
            if (ratio < 0.65f) return CharacterGrade.Rare;
            if (ratio < 0.85f) return CharacterGrade.Epic;
            if (ratio < 0.96f) return CharacterGrade.Legendary;
            return CharacterGrade.Mythic;
        }
    }
}

