using System;
using UnityEngine;

namespace DefenseGame
{
    public enum SkillEffectType
    {
        DirectDamage = 0,
        AreaDamage = 1,
        HealSelf = 2,
        AttackSpeedBoost = 3,
        CriticalBoost = 4,
        MoveSpeedBoost = 5,
        ManaSurge = 6,
        MultiShot = 7,
        Execute = 8,
        ShieldBreak = 9,
        SummonRush = 10,
        Slow = 11,
        Stun = 12,
        ShieldAlly = 13,
        DeathPact = 14,
        MassStun = 15,
        BossFortify = 16,
        GoldDrain = 17,
        ManaBurn = 18,
        MonsterRally = 19,
        LifeSteal = 20,
        GroundAreaDamage = 21,
        Poison = 22,
        DefenseBuff = 23,
        Transform = 24,
        Taunt = 25,
        HealthDrainPercent = 26,
        LinePierceDamage = 27,
        ManaRestoreAdjacent = 28,
        DamageStun = 29,
        PercentHealthDamage = 30,
        HealLowestAllies = 31,
        DamageSlow = 32,
        StoneLine = 33,
        DamageGroundField = 34,
        FixedPoison = 35,
        ThornsAura = 36,
        DeathPoisonField = 37,
        AllyAttackSpeedBoost = 38,
        FrontKnockbackGuard = 39,
        RandomMultiShot = 40
    }

    public enum SkillCategory
    {
        Auto = 0,
        Damage = 1,
        AreaAttack = 2,
        Buff = 3,
        LifeSteal = 4,
        Slow = 5,
        GroundDamage = 6,
        Poison = 7,
        Defense = 8,
        Stun = 9,
        Heal = 10,
        ManaCharge = 11,
        Transform = 12,
        Summon = 13,
        BossSpecial = 14
    }

    public enum SkillDeliveryType
    {
        Auto = 0,
        Melee = 1,
        Projectile = 2,
        GroundArea = 3,
        Instant = 4
    }

    [System.Flags]
    public enum SkillGrowthTarget
    {
        None = 0,
        Power = 1 << 0,
        SecondaryPower = 1 << 1,
        Duration = 1 << 2,
        Radius = 1 << 3,
        HitCount = 1 << 4
    }

    [Serializable]
    public class SkillDefinition
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public SkillEffectType effectType;
        public SkillCategory category = SkillCategory.Auto;
        public SkillDeliveryType deliveryType = SkillDeliveryType.Auto;
        public bool useCustomCastRange;
        public float castRange = 6f;
        [Tooltip("Monster skills only. Allows an explicitly designed boss mechanic to ignore distance when selecting defenders.")]
        public bool isGlobalTargeting;
        public float power = 1f;
        public float secondaryPower = 0.35f;
        public float duration = 3f;
        public float radius = 2.5f;
        public float manaThreshold = 100f;
        public float cooldown = 4f;
        public int hitCount = 1;

        [Header("Outgame Growth")]
        public SkillGrowthTarget growthTargets = SkillGrowthTarget.None;
        public float growthStepRatio = 0.05f;

        [Header("Skill Resources")]
        public GameObject projectilePrefab;
        public GameObject muzzleEffectPrefab;
        public GameObject hitEffectPrefab;
        public GameObject areaEffectPrefab;

        public SkillCategory ResolvedCategory => SkillDefinitionUtility.ResolveCategory(this);
        public SkillDeliveryType ResolvedDeliveryType => SkillDefinitionUtility.ResolveDeliveryType(this);
    }

    public static class SkillDefinitionUtility
    {
        public static SkillCategory ResolveCategory(SkillDefinition skill)
        {
            if (skill == null)
            {
                return SkillCategory.Auto;
            }

            if (skill.category != SkillCategory.Auto)
            {
                return skill.category;
            }

            return ResolveCategory(skill.effectType);
        }

        public static SkillDeliveryType ResolveDeliveryType(SkillDefinition skill)
        {
            if (skill == null)
            {
                return SkillDeliveryType.Instant;
            }

            if (skill.deliveryType != SkillDeliveryType.Auto)
            {
                return skill.deliveryType;
            }

            switch (skill.effectType)
            {
                case SkillEffectType.GroundAreaDamage:
                    return SkillDeliveryType.GroundArea;
                case SkillEffectType.DamageGroundField:
                case SkillEffectType.FixedPoison:
                case SkillEffectType.RandomMultiShot:
                    return SkillDeliveryType.Projectile;
                case SkillEffectType.HealSelf:
                case SkillEffectType.AttackSpeedBoost:
                case SkillEffectType.AllyAttackSpeedBoost:
                case SkillEffectType.CriticalBoost:
                case SkillEffectType.MoveSpeedBoost:
                case SkillEffectType.ManaSurge:
                case SkillEffectType.ManaRestoreAdjacent:
                case SkillEffectType.ShieldAlly:
                case SkillEffectType.HealLowestAllies:
                case SkillEffectType.DefenseBuff:
                case SkillEffectType.ThornsAura:
                case SkillEffectType.DeathPoisonField:
                case SkillEffectType.Transform:
                case SkillEffectType.Taunt:
                case SkillEffectType.FrontKnockbackGuard:
                case SkillEffectType.BossFortify:
                case SkillEffectType.GoldDrain:
                case SkillEffectType.ManaBurn:
                case SkillEffectType.MonsterRally:
                    return SkillDeliveryType.Instant;
                default:
                    return SkillDeliveryType.Melee;
            }
        }

        public static SkillCategory ResolveCategory(SkillEffectType effectType)
        {
            switch (effectType)
            {
                case SkillEffectType.DirectDamage:
                case SkillEffectType.Execute:
                case SkillEffectType.ShieldBreak:
                case SkillEffectType.PercentHealthDamage:
                case SkillEffectType.FrontKnockbackGuard:
                    return SkillCategory.Damage;
                case SkillEffectType.AreaDamage:
                case SkillEffectType.MultiShot:
                case SkillEffectType.RandomMultiShot:
                case SkillEffectType.LinePierceDamage:
                case SkillEffectType.StoneLine:
                    return SkillCategory.AreaAttack;
                case SkillEffectType.AttackSpeedBoost:
                case SkillEffectType.AllyAttackSpeedBoost:
                case SkillEffectType.CriticalBoost:
                case SkillEffectType.MoveSpeedBoost:
                case SkillEffectType.MonsterRally:
                    return SkillCategory.Buff;
                case SkillEffectType.LifeSteal:
                case SkillEffectType.HealthDrainPercent:
                    return SkillCategory.LifeSteal;
                case SkillEffectType.Slow:
                case SkillEffectType.DamageSlow:
                    return SkillCategory.Slow;
                case SkillEffectType.GroundAreaDamage:
                case SkillEffectType.DamageGroundField:
                    return SkillCategory.GroundDamage;
                case SkillEffectType.Poison:
                case SkillEffectType.FixedPoison:
                case SkillEffectType.DeathPoisonField:
                    return SkillCategory.Poison;
                case SkillEffectType.ShieldAlly:
                case SkillEffectType.DefenseBuff:
                case SkillEffectType.Taunt:
                case SkillEffectType.ThornsAura:
                case SkillEffectType.BossFortify:
                    return SkillCategory.Defense;
                case SkillEffectType.Stun:
                case SkillEffectType.MassStun:
                case SkillEffectType.DamageStun:
                    return SkillCategory.Stun;
                case SkillEffectType.HealSelf:
                case SkillEffectType.HealLowestAllies:
                    return SkillCategory.Heal;
                case SkillEffectType.ManaSurge:
                case SkillEffectType.ManaBurn:
                case SkillEffectType.ManaRestoreAdjacent:
                    return SkillCategory.ManaCharge;
                case SkillEffectType.Transform:
                    return SkillCategory.Transform;
                case SkillEffectType.SummonRush:
                    return SkillCategory.Summon;
                case SkillEffectType.DeathPact:
                case SkillEffectType.GoldDrain:
                    return SkillCategory.BossSpecial;
                default:
                    return SkillCategory.Auto;
            }
        }

        public static string GetCategoryDisplayName(SkillCategory category)
        {
            switch (category)
            {
                case SkillCategory.Damage: return "데미지형";
                case SkillCategory.AreaAttack: return "광역공격형";
                case SkillCategory.Buff: return "버프형";
                case SkillCategory.LifeSteal: return "흡혈형";
                case SkillCategory.Slow: return "슬로우형";
                case SkillCategory.GroundDamage: return "광역바닥데미지형";
                case SkillCategory.Poison: return "중독형";
                case SkillCategory.Defense: return "방어형";
                case SkillCategory.Stun: return "스턴형";
                case SkillCategory.Heal: return "힐";
                case SkillCategory.ManaCharge: return "마나 충전형";
                case SkillCategory.Transform: return "변신형";
                case SkillCategory.Summon: return "소환형";
                case SkillCategory.BossSpecial: return "보스 특수형";
                default: return "자동";
            }
        }

        public static string BuildDisplayDescription(SkillDefinition skill)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            switch (skill.effectType)
            {
                case SkillEffectType.HealthDrainPercent:
                    return "적의 현재 체력 " + FormatPercent(skill.power) + "를 흡수해 내 체력으로 전환합니다.";
                case SkillEffectType.DirectDamage:
                    return "공격력 " + FormatPercent(skill.power) + "의 피해를 줍니다.";
                case SkillEffectType.LinePierceDamage:
                    return FormatMeters(skill.radius) + " 전방의 적들에게 공격력 " + FormatPercent(skill.power) + "의 관통 피해를 줍니다.";
                case SkillEffectType.ManaRestoreAdjacent:
                    return "양옆 아군의 마나를 최대 마나의 " + FormatPercent(skill.power) + "만큼 회복합니다.";
                case SkillEffectType.DamageStun:
                    return "공격력 " + FormatPercent(skill.power) + "의 피해를 주고 " + FormatSeconds(skill.duration) + " 동안 스턴시킵니다.";
                case SkillEffectType.PercentHealthDamage:
                    return "적 현재 체력의 " + FormatPercent(skill.power) + "만큼 피해를 줍니다.";
                case SkillEffectType.AreaDamage:
                    return FormatMeters(skill.radius) + " 반경의 적에게 공격력 " + FormatPercent(skill.power) + "의 피해를 줍니다.";
                case SkillEffectType.HealLowestAllies:
                    return "체력이 낮은 아군 " + Mathf.Max(1, skill.hitCount) + "명에게 최대 체력의 " + FormatPercent(skill.power) + "를 회복시킵니다.";
                case SkillEffectType.DamageSlow:
                    return "공격력 " + FormatPercent(skill.power) + "의 피해를 주고 " + FormatSeconds(skill.duration) + " 동안 이속과 공속을 " + FormatPercent(skill.secondaryPower) + " 낮춥니다.";
                case SkillEffectType.StoneLine:
                    return FormatMeters(skill.radius) + " 전방의 적을 석상으로 만들어 " + FormatSeconds(skill.duration) + " 동안 행동 불가 상태로 만듭니다.";
                case SkillEffectType.DamageGroundField:
                    return "구체로 공격력 " + FormatPercent(skill.power) + "의 피해를 주고 " + FormatMeters(skill.radius) + " 반경에 " + FormatSeconds(skill.duration) + " 동안 용암지역을 생성합니다. 지역 안 몬스터는 초당 " + FormatNumber(skill.secondaryPower) + " 피해를 받습니다.";
                case SkillEffectType.AttackSpeedBoost:
                    return FormatSeconds(skill.duration) + " 동안 공격속도를 " + FormatPercent(skill.power) + " 올립니다.";
                case SkillEffectType.AllyAttackSpeedBoost:
                    return "사거리 안에 있는 아군의 공격속도를 " + FormatSeconds(skill.duration) + " 동안 " + FormatPercent(skill.power) + " 증가시킵니다.";
                case SkillEffectType.FixedPoison:
                    return "독화살을 발사해 " + FormatSeconds(skill.duration) + " 동안 적에게 초당 " + FormatNumber(skill.power) + " 피해를 줍니다.";
                case SkillEffectType.DefenseBuff:
                    return "최대 체력의 " + FormatPercent(skill.power) + "만큼 방어막을 생성합니다.";
                case SkillEffectType.SummonRush:
                    return BuildSummonDescription(skill);
                case SkillEffectType.ThornsAura:
                    return "쏜즈 오오라를 생성해 받은 피해의 " + FormatPercent(skill.power) + "를 공격자에게 돌려줍니다.";
                case SkillEffectType.Taunt:
                    return FormatMeters(skill.radius) + " 반경의 적을 " + FormatSeconds(skill.duration) + " 동안 도발하고 받는 피해를 " + FormatPercent(skill.secondaryPower) + " 감소시킵니다.";
                case SkillEffectType.DeathPoisonField:
                    return "죽을 때 전방에 " + FormatSeconds(skill.duration) + " 동안 독극물 지대를 만들어 초당 " + FormatNumber(skill.power) + " 피해를 줍니다.";
                case SkillEffectType.HealSelf:
                    return "최대 체력의 " + FormatPercent(skill.power) + "를 회복합니다.";
                case SkillEffectType.MultiShot:
                    return "가까운 적 " + Mathf.Max(1, skill.hitCount) + "명에게 공격력 " + FormatPercent(skill.power) + "의 피해를 줍니다.";
                case SkillEffectType.FrontKnockbackGuard:
                    return "전방의 적에게 공격력 " + FormatPercent(skill.power) + "의 피해를 주고 " + FormatMeters(skill.radius) + " 밀쳐냅니다. 사용할 때마다 받는 피해가 " + FormatPercent(skill.secondaryPower) + " 감소합니다.";
                case SkillEffectType.RandomMultiShot:
                    return "무작위 적에게 공격력 " + FormatPercent(skill.power) + "의 탄환을 " + Mathf.Max(1, skill.hitCount) + "발 발사합니다. 같은 적을 다시 노릴 수 있습니다.";
                case SkillEffectType.Execute:
                    return "적에게 공격력 " + FormatPercent(skill.power) + "의 피해를 주며, 체력이 낮은 적에게 더 강합니다.";
                case SkillEffectType.Slow:
                    return "적의 이동속도를 " + FormatSeconds(skill.duration) + " 동안 " + FormatPercent(skill.power) + " 낮춥니다.";
                case SkillEffectType.Stun:
                    return "적을 " + FormatSeconds(skill.duration) + " 동안 스턴시킵니다.";
                case SkillEffectType.ShieldAlly:
                    return "체력이 낮은 아군에게 최대 체력의 " + FormatPercent(skill.power) + "만큼 방어막을 부여합니다.";
                case SkillEffectType.LifeSteal:
                    return "공격력 " + FormatPercent(skill.power) + "의 피해를 주고 피해량의 일부를 체력으로 회복합니다.";
                case SkillEffectType.GroundAreaDamage:
                    return FormatMeters(skill.radius) + " 반경에 " + FormatSeconds(skill.duration) + " 동안 피해 장판을 생성합니다.";
                case SkillEffectType.Poison:
                    return "적을 " + FormatSeconds(skill.duration) + " 동안 중독시켜 지속 피해를 줍니다.";
                case SkillEffectType.Transform:
                    return FormatSeconds(skill.duration) + " 동안 강화된 전투 상태가 됩니다.";
                default:
                    return string.IsNullOrWhiteSpace(skill.description) ? "스킬 효과 정보가 없습니다." : skill.description;
            }
        }

        public static string BuildGrowthDisplayText(SkillDefinition skill)
        {
            if (skill == null || skill.growthTargets == SkillGrowthTarget.None || skill.growthStepRatio <= 0f)
            {
                return string.Empty;
            }

            return "성장 대상: " + BuildGrowthTargetText(skill) + " / 레벨당 +" + FormatPercent(skill.growthStepRatio) + " 선형 증가";
        }

        private static string BuildSummonDescription(SkillDefinition skill)
        {
            string health = FormatPercent(skill.power);
            string attack = FormatPercent(skill.secondaryPower > 0f ? skill.secondaryPower : skill.power);
            if (health == attack)
            {
                return "현재 체력과 공격력의 " + health + " 수준인 미니미를 소환해 적과 싸우게 합니다.";
            }

            return "현재 체력의 " + health + ", 공격력의 " + attack + " 수준인 미니미를 소환해 적과 싸우게 합니다.";
        }

        private static string BuildGrowthTargetText(SkillDefinition skill)
        {
            string result = string.Empty;
            AppendGrowthTarget(ref result, skill, SkillGrowthTarget.Power);
            AppendGrowthTarget(ref result, skill, SkillGrowthTarget.SecondaryPower);
            AppendGrowthTarget(ref result, skill, SkillGrowthTarget.Duration);
            AppendGrowthTarget(ref result, skill, SkillGrowthTarget.Radius);
            AppendGrowthTarget(ref result, skill, SkillGrowthTarget.HitCount);
            return string.IsNullOrEmpty(result) ? "주요 수치" : result;
        }

        private static void AppendGrowthTarget(ref string result, SkillDefinition skill, SkillGrowthTarget target)
        {
            if (skill == null || (skill.growthTargets & target) == 0)
            {
                return;
            }

            string label = GetGrowthTargetLabel(skill, target);
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            if (!string.IsNullOrEmpty(result))
            {
                result += ", ";
            }

            result += label;
        }

        private static string GetGrowthTargetLabel(SkillDefinition skill, SkillGrowthTarget target)
        {
            switch (target)
            {
                case SkillGrowthTarget.Power:
                    return GetPowerGrowthLabel(skill.effectType);
                case SkillGrowthTarget.SecondaryPower:
                    return GetSecondaryPowerGrowthLabel(skill.effectType);
                case SkillGrowthTarget.Duration:
                    return "지속 시간";
                case SkillGrowthTarget.Radius:
                    return "범위";
                case SkillGrowthTarget.HitCount:
                    return "대상 수";
                default:
                    return string.Empty;
            }
        }

        private static string GetPowerGrowthLabel(SkillEffectType effectType)
        {
            switch (effectType)
            {
                case SkillEffectType.HealthDrainPercent: return "흡수량";
                case SkillEffectType.ManaRestoreAdjacent: return "마나 회복량";
                case SkillEffectType.HealLowestAllies:
                case SkillEffectType.HealSelf: return "체력 회복량";
                case SkillEffectType.FixedPoison:
                case SkillEffectType.DeathPoisonField: return "초당 피해";
                case SkillEffectType.AllyAttackSpeedBoost: return "공격속도";
                case SkillEffectType.DefenseBuff:
                case SkillEffectType.ShieldAlly: return "방어막량";
                case SkillEffectType.SummonRush: return "소환체 체력";
                case SkillEffectType.ThornsAura: return "반사량";
                default: return "피해량";
            }
        }

        private static string GetSecondaryPowerGrowthLabel(SkillEffectType effectType)
        {
            switch (effectType)
            {
                case SkillEffectType.DamageSlow: return "감속률";
                case SkillEffectType.DamageGroundField: return "장판 초당 피해";
                case SkillEffectType.SummonRush: return "소환체 공격력";
                case SkillEffectType.Taunt: return "피해 감소율";
                case SkillEffectType.FrontKnockbackGuard: return "스킬당 방어력";
                default: return "보조 수치";
            }
        }

        private static string FormatPercent(float value)
        {
            return FormatNumber(value * 100f) + "%";
        }

        private static string FormatSeconds(float value)
        {
            return FormatNumber(value) + "초";
        }

        private static string FormatMeters(float value)
        {
            return FormatNumber(value) + "m";
        }

        private static string FormatNumber(float value)
        {
            float rounded = Mathf.Round(value);
            if (Mathf.Abs(value - rounded) < 0.01f)
            {
                return Mathf.RoundToInt(value).ToString();
            }

            return value.ToString("0.#");
        }
    }
}
