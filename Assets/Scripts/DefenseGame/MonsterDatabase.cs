using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenseGame
{
    public class MonsterDatabase : MonoBehaviour
    {
        [SerializeField] private List<MonsterDefinition> monsters = new List<MonsterDefinition>();
        [SerializeField] private List<MonsterDefinition> midBosses = new List<MonsterDefinition>();
        [SerializeField] private List<MonsterDefinition> bosses = new List<MonsterDefinition>();
        [SerializeField] private bool generateStarterMonsters = true;
        [SerializeField] private int starterMonsterCount = 30;
        [SerializeField] private GamePresentationConfig presentationConfig;
        [SerializeField] private MonsterCombatTuningConfig combatTuningConfig;

        [Header("Regular Swarm Balance")]
        [SerializeField] [Range(0.45f, 1f)] private float regularHealthMultiplier = 0.60f;
        [SerializeField] [Range(0.55f, 1f)] private float regularAttackMultiplier = 0.82f;
        [SerializeField] [Range(0.25f, 1f)] private float regularRewardMultiplier = 0.62f;

        [Header("Monster Grade Unlock Tables")]
        [SerializeField] private List<RoundGradeUnlockStep> regularGradeUnlockSteps = new List<RoundGradeUnlockStep>
        {
            new RoundGradeUnlockStep { firstRound = 1, maxGrade = CharacterGrade.Normal },
            new RoundGradeUnlockStep { firstRound = 4, maxGrade = CharacterGrade.Rare },
            new RoundGradeUnlockStep { firstRound = 7, maxGrade = CharacterGrade.Epic },
            new RoundGradeUnlockStep { firstRound = 10, maxGrade = CharacterGrade.Legendary },
            new RoundGradeUnlockStep { firstRound = 15, maxGrade = CharacterGrade.Mythic }
        };
        [SerializeField] private List<RoundGradeUnlockStep> midBossGradeUnlockSteps = new List<RoundGradeUnlockStep>
        {
            new RoundGradeUnlockStep { firstRound = 1, maxGrade = CharacterGrade.Rare },
            new RoundGradeUnlockStep { firstRound = 6, maxGrade = CharacterGrade.Epic },
            new RoundGradeUnlockStep { firstRound = 11, maxGrade = CharacterGrade.Legendary },
            new RoundGradeUnlockStep { firstRound = 19, maxGrade = CharacterGrade.Mythic }
        };
        [SerializeField] private List<IndexGradeStep> midBossRosterGradeSteps = new List<IndexGradeStep>
        {
            new IndexGradeStep { firstIndex = 0, grade = CharacterGrade.Rare },
            new IndexGradeStep { firstIndex = 2, grade = CharacterGrade.Epic },
            new IndexGradeStep { firstIndex = 5, grade = CharacterGrade.Legendary },
            new IndexGradeStep { firstIndex = 7, grade = CharacterGrade.Mythic }
        };
        [SerializeField] private List<StarterGradeDistributionStep> starterGradeDistributionSteps = new List<StarterGradeDistributionStep>
        {
            new StarterGradeDistributionStep { upperRatioExclusive = 0.35f, grade = CharacterGrade.Normal },
            new StarterGradeDistributionStep { upperRatioExclusive = 0.65f, grade = CharacterGrade.Rare },
            new StarterGradeDistributionStep { upperRatioExclusive = 0.85f, grade = CharacterGrade.Epic },
            new StarterGradeDistributionStep { upperRatioExclusive = 0.96f, grade = CharacterGrade.Legendary },
            new StarterGradeDistributionStep { upperRatioExclusive = 1.01f, grade = CharacterGrade.Mythic }
        };

        [Header("Monster Stat Balance")]
        [SerializeField] private MonsterStatBalance monsterStatBalance = new MonsterStatBalance();
        [SerializeField] private List<BossSeedStatModifierStep> bossSeedStatModifierSteps = new List<BossSeedStatModifierStep>
        {
            new BossSeedStatModifierStep { minSeed = 1, maxSeed = 1, healthMultiplier = 1.12f, attackMultiplier = 1.03f, attackSpeedMultiplier = 0.90f, moveSpeedMultiplier = 0.78f },
            new BossSeedStatModifierStep { minSeed = 2, maxSeed = 2, healthMultiplier = 1.10f, moveSpeedMultiplier = 0.86f, attackRangeBonus = 0.35f, manaRegenBonus = 0.01f },
            new BossSeedStatModifierStep { minSeed = 3, maxSeed = 3, healthMultiplier = 0.92f, attackSpeedMultiplier = 1.10f, moveSpeedMultiplier = 1.12f },
            new BossSeedStatModifierStep { minSeed = 4, maxSeed = -1, healthMultiplier = 1.16f, attackMultiplier = 1.08f, maxManaOverride = 100f, manaRegenBonus = 0.015f }
        };

        [Header("Boss Progression Balance")]
        [SerializeField] private List<BossEncounterBalanceStep> bossEncounterBalanceSteps = new List<BossEncounterBalanceStep>
        {
            new BossEncounterBalanceStep
            {
                encounterIndex = 0,
                designerNote = "R10 / human 3-strategy target clear rate 65-75% / 2026-07 20-run baseline 90% decisive pressure retune",
                description = "Boss 1: requires a real summon-versus-shop commitment and punishes underbuilt boards.",
                healthScale = 5.00f,
                attackScale = 3.00f,
                manaScale = 1.05f,
                skillPowerScale = 1.85f,
                cooldownScale = 0.85f,
                manaThresholdOffset = -10f,
                convertedDeathPactPower = 1.50f,
                maxMassStunTargets = 2,
                maxStunDuration = 1.50f,
                maxAreaDamagePower = 1.70f,
                maxAreaRadius = 3.20f,
                maxManaBurnTargets = 2,
                maxManaBurnRatio = 0.30f,
                maxGoldDrain = 8f,
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
                healthScale = 1.20f,
                attackScale = 0.84f,
                manaScale = 0.90f,
                skillPowerScale = 0.82f,
                cooldownScale = 1.18f,
                manaThresholdOffset = 6f,
                convertedDeathPactPower = 1.68f,
                maxMassStunTargets = 3,
                maxStunDuration = 1.70f,
                maxAreaDamagePower = 1.85f,
                maxAreaRadius = 3.36f,
                maxManaBurnTargets = 2,
                maxManaBurnRatio = 0.40f,
                maxGoldDrain = 12f,
                maxBossFortifyRatio = 0.10f,
                maxBossFortifyDuration = 3.55f,
                maxRallyPower = 0.20f,
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
                cooldownScale = 1.10f,
                manaThresholdOffset = 3f,
                convertedDeathPactPower = 1.86f,
                maxMassStunTargets = 3,
                maxStunDuration = 2.00f,
                maxAreaDamagePower = 2.05f,
                maxAreaRadius = 3.52f,
                maxManaBurnTargets = 3,
                maxManaBurnRatio = 0.50f,
                maxGoldDrain = 18f,
                maxBossFortifyRatio = 0.15f,
                maxBossFortifyDuration = 3.9f,
                maxRallyPower = 0.28f,
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
                maxStunDuration = 2.30f,
                maxAreaDamagePower = 2.30f,
                maxAreaRadius = 3.68f,
                maxManaBurnTargets = 3,
                maxManaBurnRatio = 0.60f,
                maxGoldDrain = 25f,
                maxBossFortifyRatio = 0.20f,
                maxBossFortifyDuration = 4.25f,
                maxRallyPower = 0.35f,
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
                cooldownScale = 1.00f,
                manaThresholdOffset = -1.5f,
                convertDeathPactToDamage = false,
                maxMassStunTargets = 3,
                maxStunDuration = 2.50f,
                maxAreaDamagePower = 2.45f,
                maxAreaRadius = 3.84f,
                maxManaBurnTargets = 3,
                maxManaBurnRatio = 0.65f,
                maxGoldDrain = 30f,
                maxBossFortifyRatio = 0.24f,
                maxBossFortifyDuration = 4.6f,
                maxRallyPower = 0.40f,
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
                maxStunDuration = 2.70f,
                maxAreaDamagePower = 2.60f,
                maxAreaRadius = 4.00f,
                maxManaBurnTargets = 3,
                maxManaBurnRatio = 0.70f,
                maxGoldDrain = 35f,
                maxBossFortifyRatio = 0.27f,
                maxBossFortifyDuration = 4.95f,
                maxRallyPower = 0.43f,
                maxRallyDuration = 5.3f
            },
            new BossEncounterBalanceStep
            {
                encounterIndex = 6,
                designerNote = "Day 2 / strong Legendary or wide growth",
                description = "Boss 7: demands visible outgame growth, synergy, and placement response.",
                healthScale = 2.08f,
                attackScale = 1.50f,
                manaScale = 1.24f,
                skillPowerScale = 1.30f,
                cooldownScale = 0.95f,
                manaThresholdOffset = -4.5f,
                convertDeathPactToDamage = false,
                maxMassStunTargets = 3,
                maxStunDuration = 2.90f,
                maxAreaDamagePower = 2.75f,
                maxAreaRadius = 4.16f,
                maxManaBurnTargets = 3,
                maxManaBurnRatio = 0.75f,
                maxGoldDrain = 40f,
                maxBossFortifyRatio = 0.30f,
                maxBossFortifyDuration = 5.3f,
                maxRallyPower = 0.46f,
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
                maxStunDuration = 3.00f,
                maxAreaDamagePower = 2.90f,
                maxAreaRadius = 4.32f,
                maxManaBurnTargets = 3,
                maxManaBurnRatio = 0.80f,
                maxGoldDrain = 45f,
                maxBossFortifyRatio = 0.33f,
                maxBossFortifyDuration = 5.65f,
                maxRallyPower = 0.50f,
                maxRallyDuration = 6.06f
            }
        };

        private static readonly string[] MonsterNames =
        {
            "Rot Fang", "Cave Skitter", "Mud Lurker", "Bone Pup", "Ash Beetle", "Howl Rat",
            "Night Creeper", "Ruin Stalker", "Gloom Toad", "Red Gnaw", "Iron Shell", "Fog Brute",
            "Warp Caster", "Mire Charger", "Hex Lizard", "Stone Mauler", "Acid Husk", "Void Wolf",
            "Rage Minotaur", "Feral Shaman", "Blight Colossus", "Storm Ravager", "Crimson Giant", "Grave Herald",
            "Obsidian Breaker", "Starved Dragon", "Specter Lord", "World Eater", "Black Tempest", "Chaos Devourer"
        };

        private static readonly string[] MidBossNames =
        {
            "Iron Jailor", "Glass Prophet", "Moss Colossus", "Crimson Butcher",
            "Void Taxer", "Storm Matron", "Ash Cannon", "Frost Warden"
        };

        private static readonly string[] BossNames =
        {
            "Gatebreaker Rhogar", "Queen Morva", "Leviathan Kron", "Throne of Ash", "Myth Seraph Null",
            "Clockwork Tyrant", "Abyssal Collector", "Solar Executioner"
        };

        public IReadOnlyList<MonsterDefinition> Monsters => monsters;
        public IReadOnlyList<MonsterDefinition> MidBosses => midBosses;
        public IReadOnlyList<MonsterDefinition> Bosses => bosses;

        private void Awake()
        {
            if (generateStarterMonsters && monsters.Count == 0 && midBosses.Count == 0 && bosses.Count == 0)
            {
                GenerateStarterMonsters(starterMonsterCount);
            }
            else
            {
                ApplyPresentationRoster();
                ApplyDefinitionOverrides();
            }
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

            int desiredCount = Mathf.Max(10, totalCount);
            for (int i = 0; i < desiredCount; i++)
            {
                CharacterGrade grade = ResolveGrade(i, desiredCount);
                string name = i < MonsterNames.Length ? MonsterNames[i] : $"Monster {i + 1:D2}";
                monsters.Add(CreateMonster(name, grade, i));
            }

            for (int i = 0; i < MidBossNames.Length; i++)
            {
                midBosses.Add(CreateMidBoss(MidBossNames[i], i));
            }

            for (int i = 0; i < BossNames.Length; i++)
            {
                bosses.Add(CreateBoss(BossNames[i], i));
            }

            ApplyPresentationRoster();
            ApplyDefinitionOverrides();
        }

        public MonsterDefinition GetRandomMonsterForRound(int round)
        {
            List<MonsterDefinition> candidates = monsters
                .Where(monster => monster != null &&
                    monster.threatLevel == MonsterThreatLevel.Regular &&
                    monster.minRound <= round &&
                    monster.grade <= ResolveMaxRegularGrade(round))
                .ToList();

            if (candidates.Count == 0)
            {
                candidates = monsters.Where(monster =>
                    monster != null &&
                    monster.threatLevel == MonsterThreatLevel.Regular &&
                    monster.minRound <= round).ToList();
            }

            if (candidates.Count == 0)
            {
                candidates = monsters.Where(monster => monster != null && monster.threatLevel == MonsterThreatLevel.Regular).ToList();
            }

            return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
        }

        public MonsterDefinition GetMidBossForRound(int round)
        {
            List<MonsterDefinition> candidates = midBosses
                .Where(monster => monster != null && monster.minRound <= round && monster.grade <= ResolveMaxMidBossGrade(round))
                .ToList();

            if (candidates.Count == 0)
            {
                candidates = midBosses.Where(monster => monster != null && monster.minRound <= round).ToList();
            }

            if (candidates.Count == 0)
            {
                candidates = midBosses.Where(monster => monster != null).ToList();
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            int index = Mathf.Abs(round + Random.Range(0, candidates.Count)) % candidates.Count;
            return candidates[index];
        }

        public bool HasMidBossForRound(int round)
        {
            return midBosses.Any(monster => monster != null && monster.minRound <= round);
        }

        public MonsterDefinition GetBossForRound(int round)
        {
            if (bosses.Count == 0)
            {
                return null;
            }

            List<IGrouping<string, MonsterDefinition>> groups = bosses
                .Where(monster => monster != null)
                .GroupBy(monster => string.IsNullOrWhiteSpace(monster.rosterSourceId) ? monster.id : monster.rosterSourceId)
                .OrderBy(group => group.Min(monster => monster.rosterIndex))
                .ToList();

            if (groups.Count == 0)
            {
                return null;
            }

            int encounterIndex = Mathf.Max(0, (round / 10) - 1);
            int groupIndex = encounterIndex % groups.Count;
            int desiredVariantIndex = encounterIndex / groups.Count;
            List<MonsterDefinition> variants = groups[groupIndex]
                .Where(monster => monster.minRound <= round)
                .OrderBy(monster => monster.variantIndex)
                .ToList();

            if (variants.Count == 0)
            {
                variants = groups[groupIndex].OrderBy(monster => monster.variantIndex).ToList();
            }

            MonsterDefinition selected = variants
                .Where(monster => monster.variantIndex <= desiredVariantIndex)
                .OrderByDescending(monster => monster.variantIndex)
                .FirstOrDefault();

            selected = selected != null ? selected : variants[0];
            return CreateBossEncounterDefinition(selected, round, encounterIndex);
        }

        private MonsterDefinition CreateBossEncounterDefinition(MonsterDefinition source, int round, int encounterIndex)
        {
            MonsterDefinition definition = CloneMonsterDefinition(source);
            int tier = Mathf.Max(0, encounterIndex);

            definition.displayName = source.displayName + ResolveBossEncounterSuffix(tier);
            definition.description = BuildBossEncounterDescription(tier);
            definition.minRound = round;
            definition.rewardGold = Mathf.RoundToInt(source.rewardGold * (1f + tier * 0.14f));

            float healthScale = ResolveBossEncounterHealthScale(tier);
            float attackScale = ResolveBossEncounterAttackScale(tier);
            float manaScale = ResolveBossEncounterManaScale(tier);
            definition.stats.maxHealth *= healthScale;
            definition.stats.attackPower *= attackScale;
            definition.stats.maxMana *= manaScale;
            definition.stats.manaRegenPerSecondRate *= Mathf.Lerp(0.88f, 1.24f, Mathf.Clamp01(tier / 8f));
            definition.stats.manaGainWhenHitRate *= Mathf.Lerp(0.82f, 1.18f, Mathf.Clamp01(tier / 8f));
            definition.stats.manaGainPerAttackRate *= Mathf.Lerp(0.86f, 1.16f, Mathf.Clamp01(tier / 8f));
            definition.visualScale = Mathf.Clamp(source.visualScale + tier * 0.012f, 1.58f, 2.05f);

            if (definition.skills != null)
            {
                for (int i = 0; i < definition.skills.Count; i++)
                {
                    TuneBossSkillForEncounter(definition.skills[i], tier);
                }
            }

            return definition;
        }

        private string ResolveBossEncounterSuffix(int encounterIndex)
        {
            int loop = encounterIndex / Mathf.Max(1, BossNames.Length);
            if (loop <= 0)
            {
                return string.Empty;
            }

            return " " + ToRoman(loop + 1);
        }

        private string BuildBossEncounterDescription(int encounterIndex)
        {
            BossEncounterBalanceStep balance = ResolveBossEncounterBalance(encounterIndex, out _);
            if (balance != null && !string.IsNullOrWhiteSpace(balance.description))
            {
                return balance.description;
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
            BossEncounterBalanceStep balance = ResolveBossEncounterBalance(encounterIndex, out int overflow);
            if (balance != null)
            {
                return Mathf.Max(0.1f, balance.healthScale + overflow * balance.extraHealthScalePerEncounter);
            }

            if (encounterIndex <= 0) return 1.50f;
            if (encounterIndex == 1) return 1.20f;
            if (encounterIndex == 2) return 1.38f;
            if (encounterIndex == 3) return 1.34f;
            if (encounterIndex == 4) return 1.58f;
            if (encounterIndex == 5) return 1.82f;
            if (encounterIndex == 6) return 2.08f;
            if (encounterIndex == 7) return 2.36f;
            return 2.36f + (encounterIndex - 7) * 0.22f;
        }

        private float ResolveBossEncounterAttackScale(int encounterIndex)
        {
            BossEncounterBalanceStep balance = ResolveBossEncounterBalance(encounterIndex, out int overflow);
            if (balance != null)
            {
                return Mathf.Max(0.1f, balance.attackScale + overflow * balance.extraAttackScalePerEncounter);
            }

            if (encounterIndex <= 0) return 1.32f;
            if (encounterIndex == 1) return 0.84f;
            if (encounterIndex == 2) return 0.96f;
            if (encounterIndex == 3) return 1.08f;
            if (encounterIndex == 4) return 1.22f;
            if (encounterIndex == 5) return 1.36f;
            if (encounterIndex == 6) return 1.50f;
            if (encounterIndex == 7) return 1.66f;
            return 1.66f + (encounterIndex - 7) * 0.09f;
        }

        private float ResolveBossEncounterManaScale(int encounterIndex)
        {
            BossEncounterBalanceStep balance = ResolveBossEncounterBalance(encounterIndex, out int overflow);
            if (balance != null)
            {
                return Mathf.Max(0.1f, balance.manaScale + overflow * balance.extraManaScalePerEncounter);
            }

            if (encounterIndex <= 0) return 1.05f;
            if (encounterIndex == 1) return 0.90f;
            if (encounterIndex == 2) return 0.98f;
            if (encounterIndex == 3) return 1.06f;
            if (encounterIndex == 4) return 1.14f;
            return 1.14f + Mathf.Min(0.38f, (encounterIndex - 4) * 0.05f);
        }

        private float ResolveBossEncounterSkillPowerScale(int encounterIndex)
        {
            BossEncounterBalanceStep balance = ResolveBossEncounterBalance(encounterIndex, out int overflow);
            if (balance != null)
            {
                return Mathf.Max(0.1f, balance.skillPowerScale + overflow * balance.extraSkillPowerScalePerEncounter);
            }

            if (encounterIndex <= 0) return 1.12f;
            if (encounterIndex == 1) return 0.82f;
            if (encounterIndex == 2) return 0.94f;
            if (encounterIndex == 3) return 1.05f;
            if (encounterIndex == 4) return 1.16f;
            return 1.16f + Mathf.Min(0.45f, (encounterIndex - 4) * 0.07f);
        }

        private float ResolveBossSkillCooldownScale(int encounterIndex)
        {
            BossEncounterBalanceStep balance = ResolveBossEncounterBalance(encounterIndex, out int overflow);
            if (balance != null)
            {
                return Mathf.Clamp(balance.cooldownScale + overflow * balance.extraCooldownScalePerEncounter, 0.70f, 1.45f);
            }

            if (encounterIndex <= 0) return 1.00f;
            if (encounterIndex == 1) return 1.18f;
            if (encounterIndex == 2) return 1.10f;
            if (encounterIndex == 3) return 1.04f;
            if (encounterIndex == 4) return 1.00f;
            return Mathf.Max(0.84f, 1.00f - (encounterIndex - 4) * 0.025f);
        }

        private float ResolveBossSkillManaThresholdOffset(int encounterIndex)
        {
            BossEncounterBalanceStep balance = ResolveBossEncounterBalance(encounterIndex, out int overflow);
            if (balance != null)
            {
                return Mathf.Clamp(balance.manaThresholdOffset + overflow * balance.extraManaThresholdOffsetPerEncounter, -20f, 25f);
            }

            if (encounterIndex <= 0) return 0f;
            if (encounterIndex == 1) return 6f;
            if (encounterIndex == 2) return 3f;
            if (encounterIndex == 3) return 0f;
            return Mathf.Max(-8f, -(encounterIndex - 3) * 1.5f);
        }

        private BossEncounterBalanceStep ResolveBossEncounterBalance(int encounterIndex, out int overflow)
        {
            overflow = 0;
            if (bossEncounterBalanceSteps == null || bossEncounterBalanceSteps.Count == 0)
            {
                return null;
            }

            int tier = Mathf.Max(0, encounterIndex);
            BossEncounterBalanceStep best = null;
            int bestIndex = int.MinValue;
            for (int i = 0; i < bossEncounterBalanceSteps.Count; i++)
            {
                BossEncounterBalanceStep candidate = bossEncounterBalanceSteps[i];
                if (candidate == null || candidate.encounterIndex > tier || candidate.encounterIndex < bestIndex)
                {
                    continue;
                }

                best = candidate;
                bestIndex = candidate.encounterIndex;
            }

            if (best == null)
            {
                return null;
            }

            overflow = Mathf.Max(0, tier - best.encounterIndex);
            return best;
        }

        private void TuneBossSkillForEncounter(SkillDefinition skill, int encounterIndex)
        {
            if (skill == null)
            {
                return;
            }

            int tier = Mathf.Max(0, encounterIndex);
            BossEncounterBalanceStep balance = ResolveBossEncounterBalance(tier, out int balanceOverflow);
            float skillPowerScale = ResolveBossEncounterSkillPowerScale(tier);
            skill.power *= skillPowerScale;
            skill.cooldown = Mathf.Max(4.5f, skill.cooldown * ResolveBossSkillCooldownScale(tier));
            skill.manaThreshold = Mathf.Clamp(skill.manaThreshold + ResolveBossSkillManaThresholdOffset(tier), 70f, 115f);

            switch (skill.effectType)
            {
                case SkillEffectType.DeathPact:
                    if (balance != null && balance.convertDeathPactToDamage)
                    {
                        skill.effectType = SkillEffectType.DirectDamage;
                        skill.displayName = tier <= 1 ? "사신의 압박" : "불완전 처형";
                        skill.description = "초반 보스용으로 조정된 강한 단일 피해입니다. 즉사 대신 큰 피해를 줍니다.";
                        skill.power = 1.50f + tier * 0.18f;
                        skill.duration = 0f;
                        skill.hitCount = 1;
                        skill.displayName = balance.convertedDeathPactName;
                        skill.description = balance.convertedDeathPactDescription;
                        skill.power = balance.convertedDeathPactPower + balanceOverflow * balance.extraConvertedDeathPactPowerPerEncounter;
                        skill.cooldown = Mathf.Max(skill.cooldown, balance.convertedDeathPactCooldown);
                        skill.manaThreshold = Mathf.Max(skill.manaThreshold, balance.convertedDeathPactManaThreshold);
                    }
                    else
                    {
                        skill.cooldown = Mathf.Max(skill.cooldown, 12f);
                    }
                    break;
                case SkillEffectType.MassStun:
                    skill.hitCount = Mathf.Min(skill.hitCount, ResolveBossSkillTargetCap(balance, balanceOverflow, balance != null ? balance.maxMassStunTargets : (tier <= 1 ? 1 : tier == 2 ? 2 : 3)));
                    skill.duration = Mathf.Min(skill.duration, ResolveBossSkillDurationCap(balance, balanceOverflow, balance != null ? balance.maxStunDuration : 1.05f + tier * 0.16f));
                    skill.cooldown = Mathf.Max(skill.cooldown, balance != null ? balance.minimumControlCooldown : 9.5f - Mathf.Min(1.2f, tier * 0.25f));
                    break;
                case SkillEffectType.Stun:
                    skill.duration = Mathf.Min(skill.duration, ResolveBossSkillDurationCap(balance, balanceOverflow, balance != null ? balance.maxStunDuration : 1.25f + tier * 0.18f));
                    skill.cooldown = Mathf.Max(skill.cooldown, balance != null ? balance.minimumSingleStunCooldown : 8f - Mathf.Min(1.2f, tier * 0.2f));
                    break;
                case SkillEffectType.AreaDamage:
                    skill.radius = Mathf.Min(skill.radius, ResolveBossSkillRadiusCap(balance, balanceOverflow, balance != null ? balance.maxAreaRadius : 3.2f + tier * 0.16f));
                    skill.power = Mathf.Min(skill.power, ResolveBossSkillPowerCap(balance, balanceOverflow, balance != null ? balance.maxAreaDamagePower : 0.92f + tier * 0.15f));
                    break;
                case SkillEffectType.SummonRush:
                    skill.hitCount = Mathf.Min(skill.hitCount, ResolveBossSkillTargetCap(balance, balanceOverflow, balance != null ? balance.maxSummonRushTargets : (tier <= 1 ? 2 : 3)));
                    skill.power = Mathf.Min(skill.power, ResolveBossSkillPowerCap(balance, balanceOverflow, balance != null ? balance.maxSummonRushPower : 0.70f + tier * 0.12f));
                    break;
                case SkillEffectType.ManaBurn:
                    skill.hitCount = Mathf.Min(skill.hitCount, ResolveBossSkillTargetCap(balance, balanceOverflow, balance != null ? balance.maxManaBurnTargets : (tier <= 1 ? 1 : tier == 2 ? 2 : 3)));
                    skill.power = Mathf.Min(skill.power, ResolveBossSkillPowerCap(balance, balanceOverflow, balance != null ? balance.maxManaBurnRatio : 0.22f + tier * 0.05f));
                    skill.cooldown = Mathf.Max(skill.cooldown, balance != null ? balance.minimumManaBurnCooldown : 9.4f);
                    break;
                case SkillEffectType.GoldDrain:
                    skill.power = Mathf.Min(skill.power, ResolveBossSkillPowerCap(balance, balanceOverflow, balance != null ? balance.maxGoldDrain : 6f + tier * 2.2f));
                    skill.cooldown = Mathf.Max(skill.cooldown, balance != null ? balance.minimumGoldDrainCooldown : 10.5f);
                    break;
                case SkillEffectType.BossFortify:
                    skill.power = Mathf.Min(skill.power, ResolveBossSkillPowerCap(balance, balanceOverflow, balance != null ? balance.maxBossFortifyRatio : 0.075f + tier * 0.018f));
                    skill.duration = Mathf.Min(skill.duration, ResolveBossSkillDurationCap(balance, balanceOverflow, balance != null ? balance.maxBossFortifyDuration : 3.2f + tier * 0.35f));
                    break;
                case SkillEffectType.MonsterRally:
                    skill.power = Mathf.Min(skill.power, ResolveBossSkillPowerCap(balance, balanceOverflow, balance != null ? balance.maxRallyPower : 0.13f + tier * 0.025f));
                    skill.duration = Mathf.Min(skill.duration, ResolveBossSkillDurationCap(balance, balanceOverflow, balance != null ? balance.maxRallyDuration : 3.4f + tier * 0.38f));
                    break;
            }
        }

        private int ResolveBossSkillTargetCap(BossEncounterBalanceStep balance, int overflow, int baseValue)
        {
            int extra = balance != null ? overflow * Mathf.Max(0, balance.extraTargetsPerEncounter) : 0;
            return Mathf.Max(1, baseValue + extra);
        }

        private float ResolveBossSkillPowerCap(BossEncounterBalanceStep balance, int overflow, float baseValue)
        {
            float extra = balance != null ? overflow * Mathf.Max(0f, balance.extraSkillCapPerEncounter) : 0f;
            return Mathf.Max(0f, baseValue + extra);
        }

        private float ResolveBossSkillDurationCap(BossEncounterBalanceStep balance, int overflow, float baseValue)
        {
            float extra = balance != null ? overflow * Mathf.Max(0f, balance.extraDurationCapPerEncounter) : 0f;
            return Mathf.Max(0.1f, baseValue + extra);
        }

        private float ResolveBossSkillRadiusCap(BossEncounterBalanceStep balance, int overflow, float baseValue)
        {
            float extra = balance != null ? overflow * Mathf.Max(0f, balance.extraRadiusCapPerEncounter) : 0f;
            return Mathf.Max(0.1f, baseValue + extra);
        }

        private MonsterDefinition CloneMonsterDefinition(MonsterDefinition source)
        {
            if (source == null)
            {
                return null;
            }

            MonsterDefinition clone = new MonsterDefinition();
            clone.id = source.id;
            clone.displayName = source.displayName;
            clone.description = source.description;
            clone.grade = source.grade;
            clone.role = source.role;
            clone.threatLevel = source.threatLevel;
            clone.minRound = source.minRound;
            clone.rosterSourceId = source.rosterSourceId;
            clone.rosterIndex = source.rosterIndex;
            clone.variantIndex = source.variantIndex;
            clone.accentColor = source.accentColor;
            clone.prefab = source.prefab;
            clone.isBoss = source.isBoss;
            clone.visualScale = source.visualScale;
            clone.rewardGold = source.rewardGold;
            clone.stats = CloneCombatStats(source.stats);
            clone.attackBehavior = CloneAttackBehavior(source.attackBehavior);
            clone.skills = CloneSkills(source.skills);
            return clone;
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
            List<SkillDefinition> result = new List<SkillDefinition>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                result.Add(CloneSkill(source[i]));
            }

            return result;
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
            string[] numerals = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
            if (value > 0 && value <= numerals.Length)
            {
                return numerals[value - 1];
            }

            return value.ToString();
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
            definition.threatLevel = MonsterThreatLevel.Regular;
            definition.minRound = 1;
            definition.rosterSourceId = definition.id;
            definition.rosterIndex = seed;
            definition.variantIndex = 0;
            definition.isBoss = false;
            definition.rewardGold = 3 + (int)grade;
            definition.accentColor = ResolveColor(grade, role);
            definition.stats = BuildStats(grade, role, seed, false);
            definition.skills = BuildSkills(name, grade, role, MonsterThreatLevel.Regular, seed);
            ApplyRegularSwarmProfile(definition, true);
            return definition;
        }

        private MonsterDefinition CreateMidBoss(string name, int seed)
        {
            CharacterGrade grade = ResolveMidBossGrade(seed);
            MonsterRole role = ResolveMidBossRole(seed);
            MonsterDefinition definition = new MonsterDefinition();
            definition.id = $"midboss_{seed + 1:D2}";
            definition.displayName = name;
            definition.description = name + " enters as a mid-boss with a disruptive skill pattern.";
            definition.grade = grade;
            definition.role = role;
            definition.threatLevel = MonsterThreatLevel.MidBoss;
            definition.minRound = 1;
            definition.rosterSourceId = definition.id;
            definition.rosterIndex = seed;
            definition.variantIndex = 0;
            definition.isBoss = false;
            definition.rewardGold = 18 + seed * 3 + (int)grade * 4;
            definition.accentColor = Color.Lerp(ResolveColor(grade, role), new Color(1f, 0.42f, 0.18f), 0.35f);
            definition.visualScale = ResolveVisualScale(MonsterThreatLevel.MidBoss, grade, role, 0);
            definition.stats = BuildMidBossStats(grade, role, seed);
            definition.skills = BuildMidBossSkills(name, grade, role, seed);
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
            definition.threatLevel = MonsterThreatLevel.Boss;
            definition.minRound = 1;
            definition.rosterSourceId = definition.id;
            definition.rosterIndex = seed;
            definition.variantIndex = 0;
            definition.isBoss = true;
            definition.rewardGold = 45 + seed * 15;
            definition.accentColor = new Color(1f, 0.25f + seed * 0.08f, 0.25f);
            definition.visualScale = ResolveVisualScale(MonsterThreatLevel.Boss, grade, MonsterRole.Boss, 0);
            definition.stats = BuildStats(grade, MonsterRole.Boss, seed, true);
            definition.skills = BuildSkills(name, grade, MonsterRole.Boss, MonsterThreatLevel.Boss, seed);
            return definition;
        }

        private CombatStats BuildMidBossStats(CharacterGrade grade, MonsterRole role, int seed)
        {
            MonsterStatBalance balance = ResolveMonsterStatBalance();
            CombatStats stats = BuildStats(grade, role, seed + 12, false);
            stats.maxHealth *= balance.midBossHealthMultiplier + seed * balance.midBossHealthMultiplierPerSeed;
            stats.attackPower *= balance.midBossAttackMultiplier + seed * balance.midBossAttackMultiplierPerSeed;
            stats.attackSpeed *= balance.midBossAttackSpeedMultiplier;
            stats.moveSpeed *= balance.midBossMoveSpeedMultiplier;
            stats.maxMana = balance.midBossMaxMana;
            stats.manaRegenPerSecondRate = balance.midBossManaRegenRate;
            stats.manaGainWhenHitRate = balance.midBossManaGainWhenHitRate;
            stats.manaGainPerAttackRate = balance.midBossManaGainPerAttackRate;
            stats.attackRange += role == MonsterRole.Caster ? balance.midBossCasterRangeBonus : balance.midBossRangeBonus;
            return stats;
        }

        private CombatStats BuildStats(CharacterGrade grade, MonsterRole role, int seed, bool isBoss)
        {
            MonsterStatBalance balance = ResolveMonsterStatBalance();
            int gradeIndex = (int)grade;
            CombatStats stats = new CombatStats();
            stats.maxHealth = balance.baseHealth + gradeIndex * balance.healthPerGrade + seed * balance.healthPerSeed;
            stats.attackPower = balance.baseAttackPower + gradeIndex * balance.attackPowerPerGrade;
            stats.criticalChance = Mathf.Clamp01(balance.baseCriticalChance + gradeIndex * balance.criticalChancePerGrade);
            stats.criticalDamageMultiplier = balance.baseCriticalDamageMultiplier + gradeIndex * balance.criticalDamageMultiplierPerGrade;
            stats.attackSpeed = balance.baseAttackSpeed + gradeIndex * balance.attackSpeedPerGrade;
            stats.maxMana = balance.baseMaxMana + gradeIndex * balance.maxManaPerGrade;
            stats.attackRange = balance.baseAttackRange + gradeIndex * balance.attackRangePerGrade + (seed % 2) * balance.alternatingAttackRangeBonus;
            stats.moveSpeed = balance.baseMoveSpeed + gradeIndex * balance.moveSpeedPerGrade;
            stats.projectileSpeed = balance.projectileSpeed;
            stats.manaRegenPerSecondRate = balance.manaRegenPerSecondRate;
            stats.manaGainWhenHitRate = balance.manaGainWhenHitRate;
            stats.manaGainPerAttackRate = balance.manaGainPerAttackRate;

            if (role == MonsterRole.Charger)
            {
                stats.moveSpeed *= balance.chargerMoveSpeedMultiplier;
                stats.attackSpeed *= balance.chargerAttackSpeedMultiplier;
                stats.attackRange = balance.chargerAttackRange;
                stats.manaGainPerAttackRate = balance.chargerManaGainPerAttackRate;
            }
            else if (role == MonsterRole.Brute)
            {
                stats.maxHealth *= balance.bruteHealthMultiplier;
                stats.attackPower *= balance.bruteAttackMultiplier;
                stats.moveSpeed *= balance.bruteMoveSpeedMultiplier;
                stats.attackRange += balance.bruteAttackRangeBonus;
                stats.manaGainWhenHitRate = balance.bruteManaGainWhenHitRate;
            }
            else if (role == MonsterRole.Caster)
            {
                stats.maxMana *= balance.casterMaxManaMultiplier;
                stats.attackRange += balance.casterAttackRangeBonus;
                stats.manaRegenPerSecondRate = balance.casterManaRegenPerSecondRate;
                stats.manaGainPerAttackRate = balance.casterManaGainPerAttackRate;
            }
            else if (role == MonsterRole.Elite)
            {
                stats.maxHealth *= balance.eliteHealthMultiplier;
                stats.attackPower *= balance.eliteAttackMultiplier;
                stats.criticalChance += balance.eliteCriticalChanceBonus;
                stats.attackRange += balance.eliteAttackRangeBonus;
                stats.manaGainPerAttackRate = balance.eliteManaGainPerAttackRate;
            }

            if (isBoss)
            {
                stats.maxHealth = balance.bossBaseHealth + seed * balance.bossHealthPerSeed;
                stats.attackPower = balance.bossBaseAttackPower + seed * balance.bossAttackPowerPerSeed;
                stats.criticalChance = balance.bossBaseCriticalChance + seed * balance.bossCriticalChancePerSeed;
                stats.criticalDamageMultiplier = balance.bossCriticalDamageMultiplier;
                stats.attackSpeed = balance.bossBaseAttackSpeed + seed * balance.bossAttackSpeedPerSeed;
                stats.maxMana = balance.bossMaxMana;
                stats.manaRegenPerSecondRate = balance.bossManaRegenPerSecondRate;
                stats.manaGainWhenHitRate = balance.bossManaGainWhenHitRate;
                stats.manaGainPerAttackRate = balance.bossManaGainPerAttackRate;
                stats.attackRange = balance.bossAttackRange;
                stats.moveSpeed = balance.bossBaseMoveSpeed + seed * balance.bossMoveSpeedPerSeed;
                ApplyBossSeedStatModifier(stats, seed);
            }

            stats.criticalChance = Mathf.Clamp01(stats.criticalChance);
            return stats;
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
                BossSeedStatModifierStep step = bossSeedStatModifierSteps[i];
                if (step == null || !step.AppliesTo(seed))
                {
                    continue;
                }

                stats.maxHealth *= Mathf.Max(0.01f, step.healthMultiplier);
                stats.attackPower *= Mathf.Max(0.01f, step.attackMultiplier);
                stats.attackSpeed *= Mathf.Max(0.01f, step.attackSpeedMultiplier);
                stats.moveSpeed *= Mathf.Max(0.01f, step.moveSpeedMultiplier);
                stats.attackRange += step.attackRangeBonus;
                stats.manaRegenPerSecondRate += step.manaRegenBonus;
                if (step.maxManaOverride > 0f)
                {
                    stats.maxMana = step.maxManaOverride;
                }

                return;
            }
        }

        private void ApplyRegularSwarmProfile(MonsterDefinition definition, bool scaleReward)
        {
            if (definition == null || definition.threatLevel != MonsterThreatLevel.Regular || definition.stats == null)
            {
                return;
            }

            int gradeIndex = Mathf.Max(0, (int)definition.grade);
            float healthMultiplier = Mathf.Clamp(regularHealthMultiplier + gradeIndex * 0.025f + definition.variantIndex * 0.015f, 0.45f, 0.88f);
            float attackMultiplier = Mathf.Clamp(regularAttackMultiplier + gradeIndex * 0.015f, 0.55f, 0.98f);

            definition.stats.maxHealth *= healthMultiplier;
            definition.stats.attackPower *= attackMultiplier;
            definition.stats.moveSpeed *= 1.02f;
            definition.visualScale = ResolveVisualScale(definition.threatLevel, definition.grade, definition.role, definition.variantIndex);

            if (scaleReward)
            {
                definition.rewardGold = Mathf.Max(1, Mathf.RoundToInt(definition.rewardGold * Mathf.Clamp(regularRewardMultiplier, 0.25f, 1f)));
            }
        }

        private float ResolveVisualScale(MonsterThreatLevel threatLevel, CharacterGrade grade, MonsterRole role, int variantIndex)
        {
            int gradeIndex = Mathf.Max(0, (int)grade);
            int variant = Mathf.Max(0, variantIndex);

            if (threatLevel == MonsterThreatLevel.Boss)
            {
                return Mathf.Clamp(1.68f + variant * 0.045f, 1.6f, 1.95f);
            }

            if (threatLevel == MonsterThreatLevel.MidBoss)
            {
                float midBossScale = 1.18f + gradeIndex * 0.025f + variant * 0.025f;
                if (role == MonsterRole.Brute) midBossScale += 0.08f;
                else if (role == MonsterRole.Charger) midBossScale -= 0.04f;
                return Mathf.Clamp(midBossScale, 1.12f, 1.42f);
            }

            float scale = 0.74f + gradeIndex * 0.025f + variant * 0.012f;
            if (role == MonsterRole.Brute) scale += 0.08f;
            else if (role == MonsterRole.Elite) scale += 0.04f;
            else if (role == MonsterRole.Charger) scale -= 0.04f;
            else if (role == MonsterRole.Caster) scale -= 0.02f;

            return Mathf.Clamp(scale, 0.66f, 0.96f);
        }

        private List<SkillDefinition> BuildSkills(string ownerName, CharacterGrade grade, MonsterRole role, MonsterThreatLevel threatLevel, int seed)
        {
            if (threatLevel == MonsterThreatLevel.Boss)
            {
                return BuildBossSkills(ownerName, seed);
            }

            if (threatLevel == MonsterThreatLevel.MidBoss)
            {
                return BuildMidBossSkills(ownerName, grade, role, seed);
            }

            List<SkillEffectType> pool = BuildRoleSkillPool(role, false);
            int count = GradeRules.GetSkillCount(grade, false);
            List<SkillDefinition> result = new List<SkillDefinition>(count);

            for (int i = 0; i < count; i++)
            {
                SkillEffectType effectType = pool[(seed + i) % pool.Count];
                result.Add(CreateSkill(ownerName, effectType, false, i));
            }

            return result;
        }

        private List<SkillDefinition> BuildMidBossSkills(string ownerName, CharacterGrade grade, MonsterRole role, int seed)
        {
            List<SkillDefinition> result = new List<SkillDefinition>(2);
            int pattern = Mathf.Abs(seed) % 6;
            if (pattern == 0)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.Stun, "감옥 족쇄", "가장 가까운 유닛을 짧게 기절시킵니다.", 0f, 1.35f, 0f, 1, 72f, 7.5f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.BossFortify, "철갑 보수", "체력을 회복하고 잠시 공격 속도를 올립니다.", 0.07f, 3.5f, 0f, 1, 92f, 10f, 1));
            }
            else if (pattern == 1)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.ManaBurn, "마나 누수", "무작위 유닛 2기의 마나를 태웁니다.", 0.30f, 0f, 0f, 2, 82f, 8f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.AreaDamage, "유리 파동", "주변 유닛에게 광역 피해를 줍니다.", 0.95f, 0f, 3.2f, 1, 94f, 8f, 1));
            }
            else if (pattern == 2)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.MonsterRally, "돌격 명령", "5.5m 안의 몬스터 무리를 잠시 강화합니다.", 0.16f, 4f, 0f, 1, 78f, 8f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.SummonRush, "파쇄 돌진", "여러 유닛에게 빠르게 피해를 줍니다.", 0.72f, 0f, 0f, 3, 96f, 9f, 1));
            }
            else if (pattern == 3)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.GoldDrain, "통행세 징수", "보유 골드를 일부 빼앗습니다.", 8f + seed, 0f, 0f, 1, 84f, 10f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.MoveSpeedBoost, "광란 질주", "잠시 이동 속도가 크게 증가합니다.", 0.55f, 3.5f, 0f, 1, 70f, 8f, 1));
            }
            else if (pattern == 4)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.MassStun, "서리 봉인", "무작위 유닛 2기를 짧게 묶습니다.", 0f, 1.0f, 0f, 2, 88f, 9f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.HealSelf, "빙결 재생", "체력을 일부 회복합니다.", 0.10f, 0f, 0f, 1, 92f, 10f, 1));
            }
            else
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.CriticalBoost, "학살 본능", "잠시 치명타 확률을 높입니다.", 0.22f, 4.5f, 0f, 1, 76f, 8f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.DirectDamage, "참수 일격", "가장 가까운 유닛에게 강한 피해를 줍니다.", 1.55f, 0f, 0f, 1, 96f, 8.5f, 1));
            }

            return result;
        }

#if false
        private List<SkillDefinition> BuildBossSkills(string ownerName, int seed)
        {
            List<SkillDefinition> result = new List<SkillDefinition>(2);
            int pattern = Mathf.Abs(seed) % 5;
            if (pattern == 0)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.MassStun, "대지 균열", "무작위 유닛 2기를 짧게 기절시킵니다.", 0f, 1.45f, 0f, 2, 82f, 8.5f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.AreaDamage, "파멸의 포효", "주변 유닛에게 광역 피해를 줍니다.", 1.35f, 0f, 3.8f, 1, 95f, 7.5f, 1));
            }
            else if (pattern == 1)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.DeathPact, "죽음의 서약", "무작위 유닛 하나를 즉시 처치합니다.", 0f, 0f, 0f, 1, 100f, 14f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.Stun, "여왕의 속박", "가장 가까운 유닛을 기절시킵니다.", 0f, 2.4f, 0f, 1, 75f, 7f, 1));
            }
            else if (pattern == 2)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.BossFortify, "심해 장갑", "체력을 회복하고 잠시 공격 속도를 올립니다.", 0.12f, 5f, 0f, 1, 80f, 10f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.ManaBurn, "마나 침식", "무작위 유닛 3기의 마나를 태웁니다.", 0.45f, 0f, 0f, 3, 92f, 9f, 1));
            }
            else if (pattern == 3)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.MonsterRally, "군단 집결", "5.5m 안의 몬스터 이동속도와 공격속도를 잠시 올립니다.", 0.22f, 5.5f, 0f, 1, 80f, 9f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.GoldDrain, "탐욕의 징수", "보유 골드를 강제로 빼앗습니다.", 18f, 0f, 0f, 1, 92f, 11f, 1));
            }
            else
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.DeathPact, "공허 처형", "무작위 유닛 하나를 전장에서 지웁니다.", 0f, 0f, 0f, 1, 95f, 13f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.MassStun, "무한 정지장", "무작위 유닛 3기를 기절시킵니다.", 0f, 1.6f, 0f, 3, 85f, 8.5f, 1));
            }

            return result;
        }

#endif

        private List<SkillDefinition> BuildBossSkills(string ownerName, int seed)
        {
            List<SkillDefinition> result = new List<SkillDefinition>(2);
            int pattern = Mathf.Abs(seed) % 8;
            if (pattern == 0)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.MassStun, "대지 균열", "무작위 유닛 2기에 공격력 85% 피해를 주고 짧게 기절시킵니다.", 0.85f, 1.45f, 0f, 2, 82f, 8.5f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.BossFortify, "암석 방벽", "체력을 10% 회복하고 잠시 공격 속도를 높입니다.", 0.10f, 4.5f, 0f, 1, 95f, 9.5f, 1));
            }
            else if (pattern == 1)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.DeathPact, "죽음의 서약", "무작위 유닛 하나를 처형합니다.", 0f, 0f, 0f, 1, 100f, 14f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.Stun, "여왕의 속박", "가장 가까운 유닛을 기절시킵니다.", 0f, 2.4f, 0f, 1, 75f, 7f, 1));
            }
            else if (pattern == 2)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.AreaDamage, "운석 낙하", "주변 유닛에게 공격력 125%의 광역 피해를 줍니다.", 1.25f, 0f, 4.0f, 1, 80f, 9.5f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.ManaBurn, "마나 침식", "무작위 유닛 3기의 마나를 45% 태웁니다.", 0.45f, 0f, 0f, 3, 92f, 9f, 1));
            }
            else if (pattern == 3)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.MonsterRally, "군단 집결", "5.5m 안의 몬스터 이동속도와 공격속도를 잠시 올립니다.", 0.22f, 5.5f, 0f, 1, 80f, 9f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.GoldDrain, "탐욕의 징수", "보유 골드를 강제로 빼앗습니다.", 18f, 0f, 0f, 1, 92f, 11f, 1));
            }
            else if (pattern == 4)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.DeathPact, "공허 처형", "무작위 유닛 하나를 전장에서 지웁니다.", 0f, 0f, 0f, 1, 95f, 13f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.MassStun, "무한 정지장", "무작위 유닛들을 기절시킵니다.", 0f, 1.6f, 0f, 3, 85f, 8.5f, 1));
            }
            else if (pattern == 5)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.ManaBurn, "마나 약탈", "마나가 쌓인 유닛들의 마나를 태웁니다.", 0.38f, 0f, 0f, 2, 78f, 7.8f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.MonsterRally, "돌격 북소리", "5.5m 안의 몬스터 무리를 잠시 가속시킵니다.", 0.18f, 4.6f, 0f, 1, 88f, 8.8f, 1));
            }
            else if (pattern == 6)
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.BossFortify, "보호막 충전", "보스가 체력을 회복하고 잠시 강화됩니다.", 0.10f, 4.4f, 0f, 1, 76f, 9.2f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.AreaDamage, "압력 폭발", "주변 유닛에게 강한 광역 피해를 줍니다.", 1.12f, 0f, 3.4f, 1, 92f, 8.2f, 1));
            }
            else
            {
                result.Add(CreateBossSkill(ownerName, SkillEffectType.Stun, "앞줄 압박", "가장 가까운 유닛을 기절시켜 전열을 무너뜨립니다.", 0f, 2.0f, 0f, 1, 72f, 7.4f, 0));
                result.Add(CreateBossSkill(ownerName, SkillEffectType.GoldDrain, "패배세 징수", "보유 골드를 빼앗아 운영을 흔듭니다.", 14f, 0f, 0f, 1, 88f, 9.8f, 1));
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
            if (role == MonsterRole.Brute) return new List<SkillEffectType> { SkillEffectType.DirectDamage, SkillEffectType.HealSelf, SkillEffectType.Stun };
            if (role == MonsterRole.Caster) return new List<SkillEffectType> { SkillEffectType.AreaDamage, SkillEffectType.ManaSurge, SkillEffectType.Stun };
            if (role == MonsterRole.Elite) return new List<SkillEffectType> { SkillEffectType.CriticalBoost, SkillEffectType.DirectDamage, SkillEffectType.AreaDamage, SkillEffectType.Stun };
            return new List<SkillEffectType> { SkillEffectType.DirectDamage, SkillEffectType.MoveSpeedBoost, SkillEffectType.ManaSurge };
        }

        private SkillDefinition CreateBossSkill(string ownerName, SkillEffectType effectType, string displayName, string description, float power, float duration, float radius, int hitCount, float manaThreshold, float cooldown, int index)
        {
            SkillDefinition skill = new SkillDefinition();
            skill.id = $"{ownerName}_{effectType}_{index}";
            skill.displayName = displayName;
            skill.description = description;
            skill.effectType = effectType;
            skill.power = power;
            skill.duration = duration;
            skill.radius = radius;
            skill.hitCount = hitCount;
            skill.manaThreshold = manaThreshold;
            skill.cooldown = cooldown;
            skill.isGlobalTargeting = UsesGlobalMonsterTargeting(effectType);
            ApplyMonsterSkillRangeDefaults(skill);
            return skill;
        }

        private static bool UsesGlobalMonsterTargeting(SkillEffectType effectType)
        {
            return effectType == SkillEffectType.DeathPact ||
                effectType == SkillEffectType.MassStun ||
                effectType == SkillEffectType.GoldDrain ||
                effectType == SkillEffectType.ManaBurn;
        }

        private static void ApplyMonsterSkillRangeDefaults(SkillDefinition skill)
        {
            if (skill == null)
            {
                return;
            }

            if (skill.effectType == SkillEffectType.DirectDamage ||
                skill.effectType == SkillEffectType.Stun ||
                skill.effectType == SkillEffectType.AttackPowerReduction)
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
            else if (effectType == SkillEffectType.Stun)
            {
                skill.displayName = isBoss ? "Royal Shackle" : "Crushing Grip";
                skill.description = "Briefly stuns a defender.";
                skill.power = 0f;
                skill.duration = isBoss ? 2f : 1.15f;
                skill.cooldown = isBoss ? 7f : 9f;
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

            ApplyMonsterSkillRangeDefaults(skill);
            return skill;
        }

        private CharacterGrade ResolveMaxRegularGrade(int round)
        {
            if (TryResolveRoundGrade(regularGradeUnlockSteps, round, out CharacterGrade grade))
            {
                return grade;
            }

            if (round <= 3) return CharacterGrade.Normal;
            if (round <= 6) return CharacterGrade.Rare;
            if (round <= 9) return CharacterGrade.Epic;
            if (round <= 14) return CharacterGrade.Legendary;
            return CharacterGrade.Mythic;
        }

        private CharacterGrade ResolveMaxMidBossGrade(int round)
        {
            if (TryResolveRoundGrade(midBossGradeUnlockSteps, round, out CharacterGrade grade))
            {
                return grade;
            }

            if (round <= 5) return CharacterGrade.Rare;
            if (round <= 10) return CharacterGrade.Epic;
            if (round <= 18) return CharacterGrade.Legendary;
            return CharacterGrade.Mythic;
        }

        private CharacterGrade ResolveMidBossGrade(int seed)
        {
            if (TryResolveIndexGrade(midBossRosterGradeSteps, seed, out CharacterGrade grade))
            {
                return grade;
            }

            if (seed <= 1) return CharacterGrade.Rare;
            if (seed <= 4) return CharacterGrade.Epic;
            if (seed <= 6) return CharacterGrade.Legendary;
            return CharacterGrade.Mythic;
        }

        private MonsterRole ResolveMidBossRole(int seed)
        {
            switch (Mathf.Abs(seed) % 4)
            {
                case 0: return MonsterRole.Brute;
                case 1: return MonsterRole.Caster;
                case 2: return MonsterRole.Elite;
                default: return MonsterRole.Charger;
            }
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
            if (starterGradeDistributionSteps != null && starterGradeDistributionSteps.Count > 0)
            {
                List<StarterGradeDistributionStep> ordered = starterGradeDistributionSteps
                    .Where(step => step != null)
                    .OrderBy(step => step.upperRatioExclusive)
                    .ToList();
                for (int i = 0; i < ordered.Count; i++)
                {
                    if (ratio < ordered[i].upperRatioExclusive)
                    {
                        return ordered[i].grade;
                    }
                }

                return ordered[ordered.Count - 1].grade;
            }

            if (ratio < 0.35f) return CharacterGrade.Normal;
            if (ratio < 0.65f) return CharacterGrade.Rare;
            if (ratio < 0.85f) return CharacterGrade.Epic;
            if (ratio < 0.96f) return CharacterGrade.Legendary;
            return CharacterGrade.Mythic;
        }

        private bool TryResolveRoundGrade(List<RoundGradeUnlockStep> steps, int round, out CharacterGrade grade)
        {
            grade = CharacterGrade.Normal;
            if (steps == null || steps.Count == 0)
            {
                return false;
            }

            RoundGradeUnlockStep best = null;
            int bestRound = int.MinValue;
            for (int i = 0; i < steps.Count; i++)
            {
                RoundGradeUnlockStep candidate = steps[i];
                if (candidate == null || candidate.firstRound > round || candidate.firstRound < bestRound)
                {
                    continue;
                }

                best = candidate;
                bestRound = candidate.firstRound;
            }

            if (best == null)
            {
                return false;
            }

            grade = best.maxGrade;
            return true;
        }

        private bool TryResolveIndexGrade(List<IndexGradeStep> steps, int index, out CharacterGrade grade)
        {
            grade = CharacterGrade.Normal;
            if (steps == null || steps.Count == 0)
            {
                return false;
            }

            IndexGradeStep best = null;
            int bestIndex = int.MinValue;
            for (int i = 0; i < steps.Count; i++)
            {
                IndexGradeStep candidate = steps[i];
                if (candidate == null || candidate.firstIndex > index || candidate.firstIndex < bestIndex)
                {
                    continue;
                }

                best = candidate;
                bestIndex = candidate.firstIndex;
            }

            if (best == null)
            {
                return false;
            }

            grade = best.grade;
            return true;
        }

        private void ApplyPresentationRoster()
        {
            if (presentationConfig == null)
            {
                return;
            }

            ApplyPresentationRosterForThreat(MonsterThreatLevel.Regular, monsters);
            ApplyPresentationRosterForThreat(MonsterThreatLevel.MidBoss, midBosses);
            ApplyPresentationRosterForThreat(MonsterThreatLevel.Boss, bosses);
            AddBossesAsLateMidBosses();
        }

        private void ApplyPresentationRosterForThreat(MonsterThreatLevel threatLevel, List<MonsterDefinition> target)
        {
            if (target == null || presentationConfig == null || !presentationConfig.HasMonsterRosterEntries(threatLevel))
            {
                return;
            }

            List<MonsterPresentationOverride> entries = presentationConfig.GetMonsterRosterEntries(threatLevel);
            if (entries.Count == 0)
            {
                return;
            }

            target.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                MonsterPresentationOverride entry = entries[i];
                int variantIndex = 0;
                CharacterGrade startGrade = entry.grade;
                CharacterGrade maxGrade = entry.createGradeVariants
                    ? (CharacterGrade)Mathf.Max((int)entry.grade, (int)entry.maxVariantGrade)
                    : entry.grade;

                for (int gradeIndex = (int)startGrade; gradeIndex <= (int)maxGrade; gradeIndex++)
                {
                    MonsterDefinition definition = CreateMonsterFromRoster(entry, threatLevel, i, (CharacterGrade)gradeIndex, variantIndex);
                    if (definition != null)
                    {
                        target.Add(definition);
                    }

                    variantIndex++;
                }
            }
        }

        private MonsterDefinition CreateMonsterFromRoster(MonsterPresentationOverride entry, MonsterThreatLevel threatLevel, int index, CharacterGrade grade, int variantIndex)
        {
            if (entry == null)
            {
                return null;
            }

            MonsterRole role = ResolveRosterRole(entry, threatLevel, index);
            MonsterDefinition definition = new MonsterDefinition();
            string baseId = ResolveRosterBaseId(entry, threatLevel, index);
            definition.id = ResolveRosterId(baseId, grade, variantIndex);
            definition.displayName = ResolveRosterDisplayName(entry, definition.id, grade, variantIndex);
            definition.description = BuildRosterDescription(definition.displayName, threatLevel, role);
            definition.grade = grade;
            definition.role = role;
            definition.threatLevel = threatLevel;
            definition.minRound = Mathf.Max(1, entry.minRound + variantIndex * ResolveVariantRoundStep(entry, threatLevel));
            definition.rosterSourceId = baseId;
            definition.rosterIndex = index;
            definition.variantIndex = variantIndex;
            definition.isBoss = threatLevel == MonsterThreatLevel.Boss;
            bool hasRewardOverride = entry.rewardGoldOverride > 0;
            definition.rewardGold = hasRewardOverride
                ? Mathf.RoundToInt(entry.rewardGoldOverride * (1f + variantIndex * 0.18f))
                : ResolveRosterReward(threatLevel, grade, index, variantIndex);
            definition.accentColor = ResolveRosterVariantColor(entry, threatLevel, grade, role, index, variantIndex);
            definition.visualScale = ResolveVisualScale(threatLevel, grade, role, variantIndex);
            definition.prefab = entry.prefab;

            if (threatLevel == MonsterThreatLevel.MidBoss)
            {
                definition.stats = BuildMidBossStats(grade, role, index + variantIndex);
                definition.skills = BuildMidBossSkills(definition.displayName, grade, role, index + variantIndex);
            }
            else
            {
                definition.stats = BuildStats(grade, role, index + variantIndex, threatLevel == MonsterThreatLevel.Boss);
                definition.skills = BuildSkills(definition.displayName, grade, role, threatLevel, index + variantIndex);
            }

            ApplyRosterSkillProfile(definition);
            ApplyVariantStatBonus(definition.stats, entry.variantStatBonusPerTier, variantIndex, threatLevel);
            if (threatLevel == MonsterThreatLevel.Regular)
            {
                ApplyRegularSwarmProfile(definition, !hasRewardOverride);
            }

            return definition;
        }

        private void ApplyRosterSkillProfile(MonsterDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            if (string.Equals(definition.rosterSourceId, "mob_10", System.StringComparison.OrdinalIgnoreCase))
            {
                definition.skills = new List<SkillDefinition>
                {
                    CreateBossSkill(
                        definition.displayName,
                        SkillEffectType.AttackPowerReduction,
                        "놀리기",
                        "가장 가까운 유닛의 공격력을 10% 감소시킵니다. 5초간 지속됩니다.",
                        0.10f,
                        5f,
                        0f,
                        1,
                        78f,
                        9f,
                        0)
                };
            }
            else if (string.Equals(definition.rosterSourceId, "mob_11", System.StringComparison.OrdinalIgnoreCase))
            {
                definition.skills = new List<SkillDefinition>
                {
                    CreateBossSkill(
                        definition.displayName,
                        SkillEffectType.DamageReflect,
                        "타운트",
                        "5초간 받은 피해의 10%를 공격자에게 돌려줍니다.",
                        0.10f,
                        5f,
                        0f,
                        1,
                        78f,
                        9f,
                        0)
                };
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
                return threatLevel == MonsterThreatLevel.MidBoss ? ResolveMidBossRole(index) : ResolveRole(index, false);
            }

            return entry.role;
        }

        private string ResolveRosterBaseId(MonsterPresentationOverride entry, MonsterThreatLevel threatLevel, int index)
        {
            if (!string.IsNullOrWhiteSpace(entry.monsterId))
            {
                return entry.monsterId;
            }

            string prefix = "mob";
            if (threatLevel == MonsterThreatLevel.MidBoss) prefix = "midboss";
            else if (threatLevel == MonsterThreatLevel.Boss) prefix = "boss";
            return $"{prefix}_{index + 1:D2}";
        }

        private string ResolveRosterId(string baseId, CharacterGrade grade, int variantIndex)
        {
            return variantIndex <= 0 ? baseId : $"{baseId}_g{(int)grade:D2}";
        }

        private string ResolveRosterDisplayName(MonsterPresentationOverride entry, string fallbackId, CharacterGrade grade, int variantIndex)
        {
            string baseName;
            if (!string.IsNullOrWhiteSpace(entry.displayName))
            {
                baseName = entry.displayName;
            }
            else if (entry.prefab != null)
            {
                baseName = entry.prefab.name;
            }
            else
            {
                baseName = fallbackId;
            }

            return variantIndex <= 0 ? baseName : $"{baseName} {CharacterGradeUtility.GetDisplayName(grade)}";
        }

        private string BuildRosterDescription(string displayName, MonsterThreatLevel threatLevel, MonsterRole role)
        {
            if (threatLevel == MonsterThreatLevel.Boss)
            {
                return displayName + " is registered as a major boss encounter.";
            }

            if (threatLevel == MonsterThreatLevel.MidBoss)
            {
                return displayName + " is registered as a mid-boss threat.";
            }

            return displayName + " is registered as a regular monster with the " + role + " role.";
        }

        private int ResolveRosterReward(MonsterThreatLevel threatLevel, CharacterGrade grade, int index, int variantIndex)
        {
            if (threatLevel == MonsterThreatLevel.Boss)
            {
                return 45 + index * 15 + variantIndex * 12;
            }

            if (threatLevel == MonsterThreatLevel.MidBoss)
            {
                return 18 + index * 3 + (int)grade * 4 + variantIndex * 5;
            }

            return 3 + (int)grade + variantIndex;
        }

        private Color ResolveRosterColor(MonsterThreatLevel threatLevel, CharacterGrade grade, MonsterRole role, int index)
        {
            if (threatLevel == MonsterThreatLevel.Boss)
            {
                return new Color(1f, 0.25f + index * 0.08f, 0.25f);
            }

            Color color = ResolveColor(grade, role);
            return threatLevel == MonsterThreatLevel.MidBoss
                ? Color.Lerp(color, new Color(1f, 0.42f, 0.18f), 0.35f)
                : color;
        }

        private Color ResolveRosterVariantColor(MonsterPresentationOverride entry, MonsterThreatLevel threatLevel, CharacterGrade grade, MonsterRole role, int index, int variantIndex)
        {
            Color gradeColor = CharacterGradeUtility.GetColor(grade, ResolveRosterColor(threatLevel, grade, role, index));
            bool useOverrideColor = HasUsableOverrideColor(entry);
            Color baseColor = useOverrideColor ? entry.accentColor : ResolveRosterColor(threatLevel, grade, role, index);

            if (variantIndex <= 0 && useOverrideColor)
            {
                return baseColor;
            }

            float gradeBlend = threatLevel == MonsterThreatLevel.Boss ? 0.72f : threatLevel == MonsterThreatLevel.MidBoss ? 0.62f : 0.52f;
            Color color = Color.Lerp(baseColor, gradeColor, gradeBlend);
            if (variantIndex > 0)
            {
                color = Color.Lerp(color, Color.white, Mathf.Min(0.18f, variantIndex * 0.035f));
            }

            return color;
        }

        private bool HasUsableOverrideColor(MonsterPresentationOverride entry)
        {
            if (entry == null || !entry.overrideColor)
            {
                return false;
            }

            return entry.accentColor.a > 0.01f && entry.accentColor.maxColorComponent > 0.02f;
        }

        private int ResolveVariantRoundStep(MonsterPresentationOverride entry, MonsterThreatLevel threatLevel)
        {
            if (entry.variantRoundStep > 0)
            {
                return entry.variantRoundStep;
            }

            if (threatLevel == MonsterThreatLevel.Boss)
            {
                return 10;
            }

            if (threatLevel == MonsterThreatLevel.MidBoss)
            {
                return 5;
            }

            return 3;
        }

        private void ApplyVariantStatBonus(CombatStats stats, float bonusPerTier, int variantIndex, MonsterThreatLevel threatLevel)
        {
            if (stats == null || variantIndex <= 0 || bonusPerTier <= 0f)
            {
                return;
            }

            float bonus = Mathf.Clamp(bonusPerTier, 0f, 0.35f) * variantIndex;
            float bossWeight = threatLevel == MonsterThreatLevel.Boss ? 1.25f : threatLevel == MonsterThreatLevel.MidBoss ? 1.12f : 1f;
            stats.maxHealth *= 1f + bonus * bossWeight;
            stats.attackPower *= 1f + bonus * 0.72f;
            stats.maxMana *= 1f + bonus * 0.35f;
            stats.manaRegenPerSecondRate *= 1f + bonus * 0.25f;
            stats.moveSpeed *= 1f + Mathf.Min(0.12f, bonus * 0.18f);
        }

        private void AddBossesAsLateMidBosses()
        {
            if (bosses == null || bosses.Count == 0 || midBosses == null)
            {
                return;
            }

            midBosses.RemoveAll(monster => monster != null && !string.IsNullOrEmpty(monster.id) && monster.id.StartsWith("midboss_shadow_"));

            for (int i = 0; i < bosses.Count; i++)
            {
                MonsterDefinition boss = bosses[i];
                if (boss == null || boss.prefab == null)
                {
                    continue;
                }

                MonsterDefinition shadow = CreateBossShadowMidBoss(boss);
                if (shadow != null)
                {
                    midBosses.Add(shadow);
                }
            }
        }

        private MonsterDefinition CreateBossShadowMidBoss(MonsterDefinition boss)
        {
            MonsterRole role = ResolveMidBossRole(boss.rosterIndex + boss.variantIndex);
            MonsterDefinition definition = new MonsterDefinition();
            definition.id = "midboss_shadow_" + boss.id;
            definition.displayName = boss.displayName + " Echo";
            definition.description = boss.displayName + " returns as a weakened mid-boss shadow.";
            definition.grade = boss.grade;
            definition.role = role;
            definition.threatLevel = MonsterThreatLevel.MidBoss;
            definition.minRound = ResolveBossShadowMidBossRound(boss);
            definition.rosterSourceId = boss.rosterSourceId;
            definition.rosterIndex = boss.rosterIndex;
            definition.variantIndex = boss.variantIndex;
            definition.isBoss = false;
            definition.rewardGold = Mathf.RoundToInt(20f + (int)boss.grade * 5f + boss.variantIndex * 6f);
            definition.accentColor = Color.Lerp(boss.accentColor, new Color(1f, 0.55f, 0.16f), 0.25f);
            definition.visualScale = ResolveVisualScale(MonsterThreatLevel.MidBoss, definition.grade, role, boss.variantIndex);
            definition.prefab = boss.prefab;
            definition.stats = BuildBossShadowMidBossStats(boss, role);
            definition.skills = BuildMidBossSkills(definition.displayName, definition.grade, role, boss.rosterIndex + boss.variantIndex);
            return definition;
        }

        private int ResolveBossShadowMidBossRound(MonsterDefinition boss)
        {
            int baseRound = 20 + boss.rosterIndex * 4 + boss.variantIndex * 8;
            return Mathf.Max(baseRound, boss.minRound + 10);
        }

        private CombatStats BuildBossShadowMidBossStats(MonsterDefinition boss, MonsterRole role)
        {
            CombatStats stats = new CombatStats();
            float tier = Mathf.Max(0, boss.variantIndex);
            float healthRatio = 0.34f + tier * 0.035f;
            float attackRatio = 0.54f + tier * 0.035f;

            stats.maxHealth = boss.stats.maxHealth * healthRatio;
            stats.attackPower = boss.stats.attackPower * attackRatio;
            stats.criticalChance = Mathf.Clamp01(boss.stats.criticalChance * 0.8f + tier * 0.01f);
            stats.criticalDamageMultiplier = Mathf.Max(1.45f, boss.stats.criticalDamageMultiplier * 0.88f);
            stats.attackSpeed = Mathf.Max(0.55f, boss.stats.attackSpeed * 0.9f);
            stats.maxMana = 105f + tier * 8f;
            stats.attackRange = Mathf.Max(1.45f, boss.stats.attackRange * 0.9f);
            stats.moveSpeed = Mathf.Max(0.72f, boss.stats.moveSpeed * 0.92f);
            stats.projectileSpeed = boss.stats.projectileSpeed;
            stats.manaRegenPerSecondRate = 0.062f + tier * 0.004f;
            stats.manaGainWhenHitRate = 0.12f;
            stats.manaGainPerAttackRate = 0.16f;

            if (role == MonsterRole.Brute)
            {
                stats.maxHealth *= 1.18f;
                stats.moveSpeed *= 0.92f;
            }
            else if (role == MonsterRole.Caster)
            {
                stats.attackRange += 0.45f;
                stats.manaRegenPerSecondRate += 0.008f;
            }
            else if (role == MonsterRole.Charger)
            {
                stats.moveSpeed *= 1.12f;
                stats.attackSpeed *= 1.06f;
            }
            else if (role == MonsterRole.Elite)
            {
                stats.attackPower *= 1.08f;
                stats.criticalChance = Mathf.Clamp01(stats.criticalChance + 0.05f);
            }

            return stats;
        }

        [System.Serializable]
        private class RoundGradeUnlockStep
        {
            public int firstRound = 1;
            public CharacterGrade maxGrade = CharacterGrade.Normal;
        }

        [System.Serializable]
        private class IndexGradeStep
        {
            public int firstIndex = 0;
            public CharacterGrade grade = CharacterGrade.Normal;
        }

        [System.Serializable]
        private class StarterGradeDistributionStep
        {
            [Range(0f, 1.01f)] public float upperRatioExclusive = 1f;
            public CharacterGrade grade = CharacterGrade.Normal;
        }

        [System.Serializable]
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
            public float manaGainWhenHitRate = 0.10f;
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

        [System.Serializable]
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

        [System.Serializable]
        private class BossEncounterBalanceStep
        {
            [Tooltip("Boss encounter index. 0 is round 10, 1 is round 20, 2 is round 30.")]
            public int encounterIndex;
            [TextArea] public string designerNote;
            [TextArea] public string description;

            [Header("Core Scale")]
            [Range(0.1f, 6f)] public float healthScale = 1f;
            [Range(0.1f, 4f)] public float attackScale = 1f;
            [Range(0.1f, 3f)] public float manaScale = 1f;
            [Range(0.1f, 3f)] public float skillPowerScale = 1f;
            [Range(0.7f, 1.5f)] public float cooldownScale = 1f;
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
            [TextArea] public string convertedDeathPactDescription = "Early boss version: heavy single-target damage instead of instant death.";
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
            public float maxSummonRushPower = 0.70f;
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
                if (presentationConfig != null)
                {
                    presentationConfig.ApplyToMonster(definitions[i]);
                }

                if (combatTuningConfig != null)
                {
                    combatTuningConfig.ApplyToMonster(definitions[i]);
                }
            }
        }
    }
}
