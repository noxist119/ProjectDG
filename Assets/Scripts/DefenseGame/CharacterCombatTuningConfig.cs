using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
    [CreateAssetMenu(fileName = "CharacterCombatTuningConfig", menuName = "Defense Game/Character Combat Tuning")]
    public class CharacterCombatTuningConfig : ScriptableObject
    {
        [Header("Default Support Skill FX")]
        public GameObject defaultBuffEffectPrefab;
        public GameObject defaultAttackSpeedBuffEffectPrefab;
        public GameObject defaultHealEffectPrefab;
        public GameObject defaultShieldEffectPrefab;
        public GameObject defaultManaEffectPrefab;

        [Header("Default Monster Status FX")]
        public Material defaultPetrifyMaterial;

        public List<CharacterCombatTuningEntry> entries = new List<CharacterCombatTuningEntry>();

        public void ApplyToCharacter(CharacterDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            ApplyBuiltInSignatureHero(definition);
            CharacterCombatTuningEntry entry = FindEntry(definition.id);
            if (entry == null)
            {
                return;
            }

            if (entry.overrideRole)
            {
                definition.role = entry.role;
                int seed = TryParseIndex(definition.id, out int index) ? index : 0;
                definition.tags = CharacterTagUtility.BuildDefaultTags(definition.role, seed, definition.grade);
            }

            if (definition.attackBehavior == null)
            {
                definition.attackBehavior = new AttackBehavior();
            }

            if (entry.overrideBasicAttackType)
            {
                definition.attackBehavior.basicAttackType = entry.basicAttackType;
            }

            if (entry.overrideBasicAttackRange)
            {
                definition.attackBehavior.useCustomAttackRange = true;
                definition.attackBehavior.customAttackRange = Mathf.Max(0.5f, entry.basicAttackRange);
            }

            if (entry.overrideSplash)
            {
                definition.attackBehavior.splashRadius = entry.splashRadius;
                definition.attackBehavior.splashDamageRatio = entry.splashDamageRatio;
            }

            if (entry.overridePierce)
            {
                definition.attackBehavior.additionalPierceCount = entry.additionalPierceCount;
            }

            if (entry.basicAttackProjectilePrefab != null)
            {
                definition.attackBehavior.projectilePrefabOverride = entry.basicAttackProjectilePrefab;
            }

            if (entry.basicAttackMuzzleEffectPrefab != null)
            {
                definition.attackBehavior.muzzleEffectPrefab = entry.basicAttackMuzzleEffectPrefab;
            }

            if (entry.basicAttackHitEffectPrefab != null)
            {
                definition.attackBehavior.hitEffectPrefab = entry.basicAttackHitEffectPrefab;
            }

            ApplySkillOverride(definition, 0, entry.overrideSkill01, entry.skill01);
            ApplySkillOverride(definition, 1, entry.overrideSkill02, entry.skill02);
            ApplySkillOverride(definition, 2, entry.overrideSkill03, entry.skill03);
            ApplyRequestedHeroSkillPreset(definition, entry);
            ApplySharedSkillRange(definition, entry);
        }

        public CharacterRole ResolveRole(string definitionId, CharacterRole fallback)
        {
            if (TryResolveBuiltInSignatureRole(definitionId, out CharacterRole signatureRole))
            {
                return signatureRole;
            }

            CharacterCombatTuningEntry entry = FindEntry(definitionId);
            return entry != null && entry.overrideRole ? entry.role : fallback;
        }

        private CharacterCombatTuningEntry FindEntry(string definitionId)
        {
            return entries.Find(candidate => candidate != null && candidate.characterId == definitionId);
        }

        public bool HasExplicitEntry(string definitionId)
        {
            return entries.Exists(candidate => candidate != null && candidate.characterId == definitionId) || IsBuiltInSignatureHero(definitionId);
        }

        private bool ApplyBuiltInSignatureHero(CharacterDefinition definition)
        {
            if (definition == null || !TryResolveBuiltInSignatureRole(definition.id, out CharacterRole role))
            {
                return false;
            }

            definition.role = role;
            if (definition.attackBehavior == null)
            {
                definition.attackBehavior = new AttackBehavior();
            }

            definition.attackBehavior.useCustomAttackRange = true;
            definition.attackBehavior.splashRadius = 0f;
            definition.attackBehavior.splashDamageRatio = 0f;
            definition.attackBehavior.additionalPierceCount = 0;

            switch (definition.id)
            {
                case "hero_55":
                    definition.attackBehavior.basicAttackType = BasicAttackType.Melee;
                    definition.attackBehavior.customAttackRange = 2.8f;
                    definition.skills = new List<SkillDefinition>
                    {
                        CreatePresetSkill(definition.id, "철벽 돌진", "전방의 적에게 공격력 150%의 피해를 주고 10m 밀쳐냅니다. 사용할 때마다 받는 피해가 5% 감소합니다(최대 40%).", SkillEffectType.FrontKnockbackGuard, 1.5f, 0.05f, 0f, 10f, 1, 100f, 7.5f, true, 3.4f, SkillDeliveryType.Instant, SkillGrowthTarget.Power)
                    };
                    return true;
                case "hero_56":
                    definition.attackBehavior.basicAttackType = BasicAttackType.Ranged;
                    definition.attackBehavior.customAttackRange = 9.5f;
                    definition.skills = new List<SkillDefinition>
                    {
                        CreatePresetSkill(definition.id, "격라운드 폭격", "소환된 다음 라운드부터 한 라운드씩 번갈아 기동합니다. 기동 라운드에 공격력 420%의 폭발 피해를 4.5m 범위에 한 번 가합니다.", SkillEffectType.AreaDamage, 4.2f, 0f, 0f, 4.5f, 1, 100f, 0f, true, 9.5f, SkillDeliveryType.Instant, SkillGrowthTarget.Power)
                    };
                    return true;
                case "hero_57":
                    definition.attackBehavior.basicAttackType = BasicAttackType.Ranged;
                    definition.attackBehavior.customAttackRange = 9f;
                    definition.skills = new List<SkillDefinition>
                    {
                        CreatePresetSkill(definition.id, "불규칙 난사", "무작위 적에게 공격력 120%의 탄환을 5발 발사합니다. 같은 적을 다시 노릴 수 있습니다.", SkillEffectType.RandomMultiShot, 1.2f, 0f, 0f, 2.5f, 5, 100f, 8f, true, 9f, SkillDeliveryType.Projectile, SkillGrowthTarget.Power)
                    };
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsBuiltInSignatureHero(string definitionId)
        {
            return TryResolveBuiltInSignatureRole(definitionId, out _);
        }

        private static bool TryResolveBuiltInSignatureRole(string definitionId, out CharacterRole role)
        {
            switch (definitionId)
            {
                case "hero_55":
                    role = CharacterRole.Vanguard;
                    return true;
                case "hero_56":
                case "hero_57":
                    role = CharacterRole.Ranger;
                    return true;
                default:
                    role = CharacterRole.Ranger;
                    return false;
            }
        }

        public int GetHighestConfiguredCharacterIndex()
        {
            int highestIndex = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                CharacterCombatTuningEntry entry = entries[i];
                if (entry == null || !TryParseIndex(entry.characterId, out int zeroBasedIndex))
                {
                    continue;
                }

                highestIndex = Mathf.Max(highestIndex, zeroBasedIndex + 1);
            }

            return highestIndex;
        }

        private bool TryParseIndex(string definitionId, out int index)
        {
            index = -1;
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                return false;
            }

            string[] parts = definitionId.Split('_');
            if (parts.Length == 0)
            {
                return false;
            }

            if (!int.TryParse(parts[parts.Length - 1], out int parsed))
            {
                return false;
            }

            index = parsed - 1;
            return index >= 0;
        }

        private void ApplySkillOverride(CharacterDefinition definition, int slotIndex, bool shouldOverride, SkillDefinition source)
        {
            if (!shouldOverride)
            {
                return;
            }

            if (definition.skills == null)
            {
                definition.skills = new List<SkillDefinition>();
            }

            while (definition.skills.Count <= slotIndex)
            {
                definition.skills.Add(CreateDefaultSkill(definition.id, slotIndex));
            }

            definition.skills[slotIndex] = CloneSkill(source, definition.id, slotIndex);
        }

        private void ApplySharedSkillRange(CharacterDefinition definition, CharacterCombatTuningEntry entry)
        {
            if (definition.skills == null || !entry.overrideSkillCastRange)
            {
                return;
            }

            for (int i = 0; i < definition.skills.Count; i++)
            {
                SkillDefinition skill = definition.skills[i];
                if (skill == null)
                {
                    continue;
                }

                skill.useCustomCastRange = true;
                skill.castRange = Mathf.Max(0.5f, entry.skillCastRange);
            }
        }

        private void ApplyRequestedHeroSkillPreset(CharacterDefinition definition, CharacterCombatTuningEntry entry)
        {
            if (definition == null || entry == null || entry.overrideSkill01 || entry.overrideSkill02 || entry.overrideSkill03)
            {
                return;
            }

            if (!TryCreateRequestedHeroSkill(definition.id, out SkillDefinition skill))
            {
                return;
            }

            ApplySkillResourceOverrides(skill, entry.skill01);
            definition.skills = new List<SkillDefinition> { skill };
        }

        private void ApplySkillResourceOverrides(SkillDefinition target, SkillDefinition source)
        {
            if (target == null || source == null)
            {
                return;
            }

            if (source.projectilePrefab != null)
            {
                target.projectilePrefab = source.projectilePrefab;
            }

            if (source.muzzleEffectPrefab != null)
            {
                target.muzzleEffectPrefab = source.muzzleEffectPrefab;
            }

            if (source.hitEffectPrefab != null)
            {
                target.hitEffectPrefab = source.hitEffectPrefab;
            }

            if (source.areaEffectPrefab != null)
            {
                target.areaEffectPrefab = source.areaEffectPrefab;
            }
        }

        private bool TryCreateRequestedHeroSkill(string characterId, out SkillDefinition skill)
        {
            skill = null;
            switch (characterId)
            {
                case "hero_11":
                    skill = CreatePresetSkill(characterId, "체력 흡수", "적의 현재 체력 20%를 흡수해 내 체력으로 전환합니다.", SkillEffectType.HealthDrainPercent, 0.2f, 1f, 0f, 2.5f, 1, 100f, 8f, growthTargets: SkillGrowthTarget.Power);
                    return true;
                case "hero_12":
                    skill = CreatePresetSkill(characterId, "강력한 일격", "스킬을 사용해 공격력 300%의 피해를 줍니다.", SkillEffectType.DirectDamage, 3f, 0f, 0f, 2.5f, 1, 100f, 7f, growthTargets: SkillGrowthTarget.Power);
                    return true;
                case "hero_06":
                    skill = CreatePresetSkill(characterId, "관통 창격", "5m 전방의 적들에게 공격력 200%의 관통 피해를 줍니다.", SkillEffectType.LinePierceDamage, 2f, 0.65f, 0f, 5f, 1, 100f, 8f, true, 5.5f, growthTargets: SkillGrowthTarget.Power);
                    return true;
                case "hero_13":
                    skill = CreatePresetSkill(characterId, "마나 링크", "양옆 아군의 마나를 최대 마나의 30%만큼 회복합니다.", SkillEffectType.ManaRestoreAdjacent, 0.3f, 0f, 0f, 3.2f, 2, 100f, 8f, growthTargets: SkillGrowthTarget.Power);
                    return true;
                case "hero_31":
                    skill = CreatePresetSkill(characterId, "충격 강타", "적에게 공격력 150%의 피해를 주고 3초 동안 스턴시킵니다.", SkillEffectType.DamageStun, 1.5f, 0f, 3f, 2.5f, 1, 100f, 8f, growthTargets: SkillGrowthTarget.Power);
                    return true;
                case "hero_32":
                    skill = CreatePresetSkill(characterId, "야성의 추적탄", "공격력 220%의 피해와 4초간 35% 둔화를 주고, 공격력 30%의 중독 피해를 초당 가합니다. 시전 후 5초간 공격속도가 25% 증가합니다.", SkillEffectType.DamageSlow, 2.2f, 0.35f, 4f, 2.5f, 1, 100f, 8f, true, 6f, SkillDeliveryType.Projectile, SkillGrowthTarget.Power);
                    return true;
                case "hero_07":
                    skill = CreatePresetSkill(characterId, "생명 파열", "적 현재 체력의 40%만큼 피해를 줍니다.", SkillEffectType.PercentHealthDamage, 0.4f, 0f, 0f, 2.5f, 1, 100f, 8f, growthTargets: SkillGrowthTarget.Power);
                    return true;
                case "hero_01":
                    skill = CreatePresetSkill(characterId, "폭발 범위공격", "3m 반경의 적에게 공격력 200%의 피해를 줍니다.", SkillEffectType.AreaDamage, 2f, 0f, 0f, 3f, 1, 100f, 8f, growthTargets: SkillGrowthTarget.Power);
                    return true;
                case "hero_02":
                    skill = CreatePresetSkill(characterId, "이중 회복", "체력이 낮은 아군 2명에게 최대 체력의 30%를 회복시킵니다.", SkillEffectType.HealLowestAllies, 0.3f, 0f, 0f, 2.5f, 2, 100f, 8f, growthTargets: SkillGrowthTarget.Power);
                    return true;
                case "hero_03":
                    skill = CreatePresetSkill(characterId, "약화 사격", "공격력 150%의 피해를 주고 5초 동안 이속과 공속을 40% 낮춥니다.", SkillEffectType.DamageSlow, 1.5f, 0.4f, 5f, 2.5f, 1, 100f, 8f, growthTargets: SkillGrowthTarget.SecondaryPower);
                    return true;
                case "hero_51":
                    skill = CreatePresetSkill(characterId, "감전 일격", "공격력 300%의 피해를 주고 감전된 적을 3초 동안 스턴시킵니다.", SkillEffectType.DamageStun, 3f, 0f, 3f, 2.5f, 1, 100f, 9f, growthTargets: SkillGrowthTarget.Duration);
                    return true;
                case "hero_08":
                    skill = CreatePresetSkill(characterId, "석화 찌르기", "3m 전방의 적을 석상으로 만들어 5초 동안 행동 불가 상태로 만듭니다.", SkillEffectType.StoneLine, 0f, 0.75f, 5f, 3f, 1, 100f, 10f, true, 3.2f, growthTargets: SkillGrowthTarget.Duration);
                    return true;
                case "hero_52":
                    skill = CreatePresetSkill(characterId, "용암 구체", "구체로 공격력 300%의 피해를 주고 3m 반경에 5초 동안 용암지역을 생성합니다. 지역 안 몬스터는 초당 30 피해를 받습니다.", SkillEffectType.DamageGroundField, 3f, 30f, 5f, 3f, 1, 100f, 10f, true, 6f, SkillDeliveryType.Projectile, SkillGrowthTarget.SecondaryPower);
                    return true;
                case "hero_53":
                    skill = CreatePresetSkill(characterId, "전투 가속", "8초 동안 공격속도를 50% 올립니다.", SkillEffectType.AttackSpeedBoost, 0.5f, 0f, 8f, 2.5f, 1, 100f, 11f, growthTargets: SkillGrowthTarget.Duration);
                    return true;
                case "hero_04":
                    skill = CreatePresetSkill(characterId, "독화살", "독화살을 발사해 7초 동안 적에게 초당 50 피해를 줍니다.", SkillEffectType.FixedPoison, 50f, 1f, 7f, 2.5f, 1, 100f, 9f, true, 6f, SkillDeliveryType.Projectile, SkillGrowthTarget.Power);
                    return true;
                case "hero_05":
                    skill = CreatePresetSkill(characterId, "철벽 방어막", "6초 동안 자신과 근처 아군에게 최대 체력의 45% 방어막을 생성합니다.", SkillEffectType.DefenseBuff, 0.45f, 0f, 6f, 0.1f, 1, 100f, 12f, growthTargets: SkillGrowthTarget.Power);
                    return true;
                case "hero_09":
                    skill = CreatePresetSkill(characterId, "전방 참격", "5m 전방의 모든 적에게 공격력 200%의 피해를 줍니다.", SkillEffectType.LinePierceDamage, 2f, 0.75f, 0f, 5f, 1, 100f, 9f, true, 5.5f, growthTargets: SkillGrowthTarget.Power);
                    return true;
                case "hero_21":
                    skill = CreatePresetSkill(characterId, "미니미 소환", "현재 체력과 공격력의 20% 수준인 미니미를 소환해 적과 싸우게 합니다.", SkillEffectType.SummonRush, 0.2f, 0.2f, 0f, 3.2f, 1, 100f, 13f, growthTargets: SkillGrowthTarget.Power | SkillGrowthTarget.SecondaryPower);
                    return true;
                case "hero_22":
                    skill = CreatePresetSkill(characterId, "쏜즈 오오라", "쏜즈 오오라를 생성해 받은 피해의 120%를 공격자에게 돌려줍니다.", SkillEffectType.ThornsAura, 1.2f, 0f, 8f, 2.5f, 1, 100f, 12f, growthTargets: SkillGrowthTarget.Power);
                    return true;
                case "hero_10":
                    skill = CreatePresetSkill(characterId, "초고속 가속", "7초 동안 공격속도를 300% 올립니다.", SkillEffectType.AttackSpeedBoost, 3f, 0f, 7f, 2.5f, 1, 100f, 12f, growthTargets: SkillGrowthTarget.Duration);
                    return true;
                case "hero_23":
                    skill = CreatePresetSkill(characterId, "파워 스트라이크", "스킬 사용 시 공격력 300%로 적을 공격합니다.", SkillEffectType.DirectDamage, 3f, 0f, 0f, 2.5f, 1, 100f, 7f, growthTargets: SkillGrowthTarget.Power);
                    return true;
                case "hero_54":
                    skill = CreatePresetSkill(characterId, "수호 도발", "3m 반경의 적을 5초 동안 도발하고 받는 피해를 50% 감소시킵니다. 아웃게임 성장 시 지속시간이 증가합니다.", SkillEffectType.Taunt, 0f, 0.5f, 5f, 3f, 1, 100f, 11f, growthTargets: SkillGrowthTarget.Duration);
                    return true;
                case "hero_33":
                    skill = CreatePresetSkill(characterId, "최후의 맹독", "죽을 때 전방에 10초 동안 독극물 지대를 만들어 초당 80 피해를 줍니다.", SkillEffectType.DeathPoisonField, 80f, 1f, 10f, 3f, 1, 100f, 0f, growthTargets: SkillGrowthTarget.Power);
                    return true;
                case "hero_14":
                    skill = CreatePresetSkill(characterId, "가속의 빛", "사거리 안에 있는 아군의 공격속도를 5초 동안 30% 증가시킵니다.", SkillEffectType.AllyAttackSpeedBoost, 0.3f, 0f, 5f, 3f, 1, 90f, 10f, growthTargets: SkillGrowthTarget.Power);
                    return true;
                default:
                    return false;
            }
        }

        private SkillDefinition CreatePresetSkill(string ownerId, string displayName, string description, SkillEffectType effectType, float power, float secondaryPower, float duration, float radius, int hitCount, float manaThreshold, float cooldown, bool useCustomCastRange = false, float castRange = 6f, SkillDeliveryType deliveryType = SkillDeliveryType.Auto, SkillGrowthTarget growthTargets = SkillGrowthTarget.None)
        {
            SkillDefinition skill = new SkillDefinition
            {
                id = $"{ownerId}_skill_01",
                displayName = displayName,
                description = description,
                effectType = effectType,
                category = SkillDefinitionUtility.ResolveCategory(effectType),
                deliveryType = deliveryType,
                useCustomCastRange = useCustomCastRange,
                castRange = castRange,
                power = power,
                secondaryPower = secondaryPower,
                duration = duration,
                radius = radius,
                manaThreshold = manaThreshold,
                cooldown = cooldown,
                hitCount = Mathf.Max(1, hitCount),
                growthTargets = growthTargets,
                growthStepRatio = 0.05f
            };

            ApplyDefaultSupportSkillFx(skill);
            return skill;
        }

        private void ApplyDefaultSupportSkillFx(SkillDefinition skill)
        {
            if (skill == null)
            {
                return;
            }

            switch (skill.effectType)
            {
                case SkillEffectType.AttackSpeedBoost:
                case SkillEffectType.AllyAttackSpeedBoost:
                    skill.areaEffectPrefab = defaultAttackSpeedBuffEffectPrefab != null ? defaultAttackSpeedBuffEffectPrefab : defaultBuffEffectPrefab;
                    break;
                case SkillEffectType.CriticalBoost:
                case SkillEffectType.ThornsAura:
                case SkillEffectType.Transform:
                    skill.areaEffectPrefab = defaultBuffEffectPrefab;
                    break;
                case SkillEffectType.HealSelf:
                case SkillEffectType.HealLowestAllies:
                case SkillEffectType.HealthDrainPercent:
                case SkillEffectType.LifeSteal:
                    skill.areaEffectPrefab = defaultHealEffectPrefab;
                    break;
                case SkillEffectType.ShieldAlly:
                case SkillEffectType.DefenseBuff:
                    skill.areaEffectPrefab = defaultShieldEffectPrefab;
                    break;
                case SkillEffectType.ManaRestoreAdjacent:
                case SkillEffectType.ManaSurge:
                    skill.areaEffectPrefab = defaultManaEffectPrefab;
                    break;
            }
        }

        private SkillDefinition CloneSkill(SkillDefinition source, string ownerId, int slotIndex)
        {
            SkillDefinition fallback = CreateDefaultSkill(ownerId, slotIndex);
            if (source == null)
            {
                return fallback;
            }

            SkillDefinition clone = new SkillDefinition
            {
                id = string.IsNullOrWhiteSpace(source.id) ? fallback.id : source.id,
                displayName = string.IsNullOrWhiteSpace(source.displayName) ? fallback.displayName : source.displayName,
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
                hitCount = Mathf.Max(1, source.hitCount),
                growthTargets = source.growthTargets,
                growthStepRatio = source.growthStepRatio,
                projectilePrefab = source.projectilePrefab,
                muzzleEffectPrefab = source.muzzleEffectPrefab,
                hitEffectPrefab = source.hitEffectPrefab,
                areaEffectPrefab = source.areaEffectPrefab
            };

            if (string.IsNullOrWhiteSpace(clone.description))
            {
                clone.description = fallback.description;
            }

            if (clone.areaEffectPrefab == null)
            {
                ApplyDefaultSupportSkillFx(clone);
            }

            return clone;
        }

        public static SkillDefinition CreateDefaultSkill(string ownerId, int slotIndex)
        {
            string slotName = $"Skill {slotIndex + 1:00}";
            string safeOwnerId = string.IsNullOrWhiteSpace(ownerId) ? "hero" : ownerId;
            return new SkillDefinition
            {
                id = $"{safeOwnerId}_skill_{slotIndex + 1:00}",
                displayName = slotName,
                description = "Directly tunable skill slot.",
                effectType = SkillEffectType.DirectDamage,
                category = SkillCategory.Damage,
                deliveryType = SkillDeliveryType.Auto,
                useCustomCastRange = false,
                castRange = 6f,
                power = 1f,
                secondaryPower = 0.35f,
                duration = 3f,
                radius = 2.5f,
                manaThreshold = 100f,
                cooldown = 4f,
                hitCount = 1,
                growthTargets = SkillGrowthTarget.None,
                growthStepRatio = 0.05f
            };
        }
    }

    [Serializable]
    public class CharacterCombatTuningEntry
    {
        public string characterId;

        [Header("Role")]
        public bool overrideRole;
        public CharacterRole role = CharacterRole.Ranger;

        [Header("Basic Attack")]
        public bool overrideBasicAttackType;
        public BasicAttackType basicAttackType = BasicAttackType.Ranged;

        [Header("Basic Attack Range")]
        public bool overrideBasicAttackRange;
        public float basicAttackRange = 6f;
        public bool overrideSkillCastRange;
        public float skillCastRange = 6f;

        [Header("Basic Attack Extras")]
        public bool overrideSplash;
        public float splashRadius;
        [Range(0f, 1f)] public float splashDamageRatio;
        public bool overridePierce;
        public int additionalPierceCount;

        [Header("Basic Attack Resources")]
        public GameObject basicAttackProjectilePrefab;
        public GameObject basicAttackMuzzleEffectPrefab;
        public GameObject basicAttackHitEffectPrefab;

        [Header("Skill Slots")]
        public bool overrideSkill01;
        public SkillDefinition skill01 = CharacterCombatTuningConfig.CreateDefaultSkill(null, 0);
        public bool overrideSkill02;
        public SkillDefinition skill02 = CharacterCombatTuningConfig.CreateDefaultSkill(null, 1);
        public bool overrideSkill03;
        public SkillDefinition skill03 = CharacterCombatTuningConfig.CreateDefaultSkill(null, 2);
    }
}
