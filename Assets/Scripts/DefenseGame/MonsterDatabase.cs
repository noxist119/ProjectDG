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
        [SerializeField] private int starterMonsterCount = 30;

        private static readonly string[] MonsterNames =
        {
            "Rot Fang", "Cave Skitter", "Mud Lurker", "Bone Pup", "Ash Beetle", "Howl Rat",
            "Night Creeper", "Ruin Stalker", "Gloom Toad", "Red Gnaw", "Iron Shell", "Fog Brute",
            "Warp Caster", "Mire Charger", "Hex Lizard", "Stone Mauler", "Acid Husk", "Void Wolf",
            "Rage Minotaur", "Feral Shaman", "Blight Colossus", "Storm Ravager", "Crimson Giant", "Grave Herald",
            "Obsidian Breaker", "Starved Dragon", "Specter Lord", "World Eater", "Black Tempest", "Chaos Devourer"
        };

        private static readonly string[] BossNames =
        {
            "Gatebreaker Rhogar", "Queen Morva", "Leviathan Kron", "Throne of Ash", "Myth Seraph Null"
        };

        public IReadOnlyList<MonsterDefinition> Monsters => monsters;
        public IReadOnlyList<MonsterDefinition> Bosses => bosses;

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

            int desiredCount = Mathf.Max(10, totalCount);
            for (int i = 0; i < desiredCount; i++)
            {
                CharacterGrade grade = ResolveGrade(i, desiredCount);
                string name = i < MonsterNames.Length ? MonsterNames[i] : $"Monster {i + 1:D2}";
                monsters.Add(CreateMonster(name, grade, i));
            }

            for (int i = 0; i < BossNames.Length; i++)
            {
                bosses.Add(CreateBoss(BossNames[i], i));
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

        private MonsterDefinition CreateMonster(string name, CharacterGrade grade, int seed)
        {
            MonsterRole role = ResolveRole(seed, false);
            MonsterDefinition definition = new MonsterDefinition();
            definition.id = $"mob_{seed + 1:D2}";
            definition.displayName = name;
            definition.description = name + " marches toward the final line as a " + role + ".";
            definition.grade = grade;
            definition.role = role;
            definition.rewardGold = 4 + (int)grade * 2;
            definition.accentColor = ResolveColor(grade, role);
            definition.stats = BuildStats(grade, role, seed, false);
            definition.skills = BuildSkills(name, grade, role, false, seed);
            return definition;
        }

        private MonsterDefinition CreateBoss(string name, int seed)
        {
            CharacterGrade grade = seed < 2 ? CharacterGrade.Legendary : CharacterGrade.Mythic;
            MonsterDefinition definition = new MonsterDefinition();
            definition.id = $"boss_{seed + 1:D2}";
            definition.displayName = name;
            definition.description = name + " leads the invasion with two deadly boss skills.";
            definition.grade = grade;
            definition.role = MonsterRole.Boss;
            definition.isBoss = true;
            definition.rewardGold = 45 + seed * 15;
            definition.accentColor = new Color(1f, 0.25f + seed * 0.08f, 0.25f);
            definition.stats = BuildStats(grade, MonsterRole.Boss, seed, true);
            definition.skills = BuildSkills(name, grade, MonsterRole.Boss, true, seed);
            return definition;
        }

        private CombatStats BuildStats(CharacterGrade grade, MonsterRole role, int seed, bool isBoss)
        {
            int gradeIndex = (int)grade;
            CombatStats stats = new CombatStats();
            stats.maxHealth = 70f + gradeIndex * 34f + seed * 2f;
            stats.attackPower = 8f + gradeIndex * 5.5f;
            stats.criticalChance = Mathf.Clamp01(0.05f + gradeIndex * 0.025f);
            stats.criticalDamageMultiplier = 1.5f + gradeIndex * 0.08f;
            stats.attackSpeed = 0.82f + gradeIndex * 0.07f;
            stats.maxMana = 100f + gradeIndex * 14f;
            stats.attackRange = 1.35f + gradeIndex * 0.08f;
            stats.moveSpeed = 1.35f + gradeIndex * 0.08f;
            stats.projectileSpeed = 0f;

            if (role == MonsterRole.Charger)
            {
                stats.moveSpeed *= 1.25f;
                stats.attackSpeed *= 1.1f;
            }
            else if (role == MonsterRole.Brute)
            {
                stats.maxHealth *= 1.45f;
                stats.attackPower *= 1.2f;
                stats.moveSpeed *= 0.82f;
            }
            else if (role == MonsterRole.Caster)
            {
                stats.maxMana *= 1.3f;
                stats.attackRange += 0.4f;
            }
            else if (role == MonsterRole.Elite)
            {
                stats.maxHealth *= 1.2f;
                stats.attackPower *= 1.18f;
                stats.criticalChance += 0.1f;
            }

            if (isBoss)
            {
                stats.maxHealth = 1100f + seed * 500f;
                stats.attackPower = 28f + seed * 8f;
                stats.criticalChance = 0.14f + seed * 0.02f;
                stats.criticalDamageMultiplier = 1.85f;
                stats.attackSpeed = 0.95f + seed * 0.05f;
                stats.maxMana = 120f;
                stats.attackRange = 2f;
                stats.moveSpeed = 1.15f + seed * 0.04f;
            }

            stats.criticalChance = Mathf.Clamp01(stats.criticalChance);
            return stats;
        }

        private List<SkillDefinition> BuildSkills(string ownerName, CharacterGrade grade, MonsterRole role, bool isBoss, int seed)
        {
            List<SkillEffectType> pool = BuildRoleSkillPool(role, isBoss);
            int count = GradeRules.GetSkillCount(grade, isBoss);
            List<SkillDefinition> result = new List<SkillDefinition>(count);

            for (int i = 0; i < count; i++)
            {
                SkillEffectType effectType = pool[(seed + i) % pool.Count];
                result.Add(CreateSkill(ownerName, effectType, isBoss, i));
            }

            return result;
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

            if (role == MonsterRole.Charger) return new List<SkillEffectType> { SkillEffectType.MoveSpeedBoost, SkillEffectType.DirectDamage };
            if (role == MonsterRole.Brute) return new List<SkillEffectType> { SkillEffectType.DirectDamage, SkillEffectType.HealSelf };
            if (role == MonsterRole.Caster) return new List<SkillEffectType> { SkillEffectType.AreaDamage, SkillEffectType.ManaSurge };
            if (role == MonsterRole.Elite) return new List<SkillEffectType> { SkillEffectType.CriticalBoost, SkillEffectType.DirectDamage, SkillEffectType.AreaDamage };
            return new List<SkillEffectType> { SkillEffectType.DirectDamage, SkillEffectType.MoveSpeedBoost, SkillEffectType.ManaSurge };
        }

        private SkillDefinition CreateSkill(string ownerName, SkillEffectType effectType, bool isBoss, int index)
        {
            SkillDefinition skill = new SkillDefinition();
            skill.id = $"{ownerName}_{effectType}_{index}";
            skill.effectType = effectType;
            skill.manaThreshold = isBoss ? 80f : 100f;
            skill.radius = isBoss ? 3.4f : 2.4f;
            skill.hitCount = effectType == SkillEffectType.SummonRush ? 3 : 1;

            if (effectType == SkillEffectType.DirectDamage)
            {
                skill.displayName = isBoss ? "King's Smash" : "Savage Strike";
                skill.description = "Heavy damage to the nearest defender.";
                skill.power = isBoss ? 2.1f : 1.7f;
                skill.cooldown = 5f;
            }
            else if (effectType == SkillEffectType.AreaDamage)
            {
                skill.displayName = isBoss ? "Cataclysm Roar" : "Crushing Roar";
                skill.description = "Deals area damage around the attacker.";
                skill.power = isBoss ? 1.45f : 1.1f;
                skill.cooldown = isBoss ? 6f : 7f;
            }
            else if (effectType == SkillEffectType.HealSelf)
            {
                skill.displayName = "Dark Regeneration";
                skill.description = "Recover a portion of max health.";
                skill.power = isBoss ? 0.18f : 0.22f;
                skill.cooldown = 8f;
            }
            else if (effectType == SkillEffectType.MoveSpeedBoost)
            {
                skill.displayName = isBoss ? "Tyrant Rush" : "Rush";
                skill.description = "Temporarily increases movement speed.";
                skill.power = isBoss ? 0.65f : 0.5f;
                skill.duration = 4f;
                skill.cooldown = 9f;
            }
            else if (effectType == SkillEffectType.CriticalBoost)
            {
                skill.displayName = "Frenzy";
                skill.description = "Temporarily increases critical chance.";
                skill.power = 0.2f;
                skill.duration = 5f;
                skill.cooldown = 8f;
            }
            else if (effectType == SkillEffectType.ManaSurge)
            {
                skill.displayName = "Mana Hunger";
                skill.description = "Quickly recovers mana.";
                skill.power = 0.42f;
                skill.cooldown = 6f;
            }
            else
            {
                skill.displayName = isBoss ? "Swarm Command" : "Rush Spawn";
                skill.description = "Strikes multiple defenders in rapid succession.";
                skill.effectType = SkillEffectType.SummonRush;
                skill.power = isBoss ? 0.95f : 0.7f;
                skill.hitCount = isBoss ? 4 : 2;
                skill.cooldown = 8f;
            }

            return skill;
        }

        private MonsterRole ResolveRole(int seed, bool isBoss)
        {
            if (isBoss) return MonsterRole.Boss;
            int value = seed % 5;
            if (value == 0) return MonsterRole.Grunt;
            if (value == 1) return MonsterRole.Charger;
            if (value == 2) return MonsterRole.Brute;
            if (value == 3) return MonsterRole.Caster;
            return MonsterRole.Elite;
        }

        private Color ResolveColor(CharacterGrade grade, MonsterRole role)
        {
            Color color = new Color(0.45f, 0.45f, 0.45f);
            if (grade == CharacterGrade.Rare) color = new Color(0.35f, 0.8f, 0.95f);
            else if (grade == CharacterGrade.Epic) color = new Color(0.45f, 0.95f, 0.55f);
            else if (grade == CharacterGrade.Legendary) color = new Color(1f, 0.7f, 0.25f);
            else if (grade == CharacterGrade.Mythic) color = new Color(0.95f, 0.25f, 0.25f);

            if (role == MonsterRole.Caster) color *= new Color(0.85f, 0.95f, 1.1f);
            if (role == MonsterRole.Brute) color *= new Color(1.1f, 0.9f, 0.9f);
            return color;
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
