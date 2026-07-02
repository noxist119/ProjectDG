using System;

namespace DefenseGame
{
    [Serializable]
    public struct UnitSynergyBonus
    {
        public float attackPowerBonus;
        public float attackSpeedBonus;
        public float critChanceBonus;
        public float rangeBonus;
        public float manaRegenRateBonus;
        public float maxHealthBonus;
        public float splashRadiusBonus;
        public float splashDamageRatioBonus;
        public float skillPowerBonus;
        public float criticalDamageBonus;
        public float bossDamageBonus;
        public float damageReductionBonus;
        public float manaGainWhenHitRateBonus;
        public float manaGainPerAttackRateBonus;

        public bool HasAnyValue =>
            Math.Abs(attackPowerBonus) > 0.0001f ||
            Math.Abs(attackSpeedBonus) > 0.0001f ||
            Math.Abs(critChanceBonus) > 0.0001f ||
            Math.Abs(rangeBonus) > 0.0001f ||
            Math.Abs(manaRegenRateBonus) > 0.0001f ||
            Math.Abs(maxHealthBonus) > 0.0001f ||
            Math.Abs(splashRadiusBonus) > 0.0001f ||
            Math.Abs(splashDamageRatioBonus) > 0.0001f ||
            Math.Abs(skillPowerBonus) > 0.0001f ||
            Math.Abs(criticalDamageBonus) > 0.0001f ||
            Math.Abs(bossDamageBonus) > 0.0001f ||
            Math.Abs(damageReductionBonus) > 0.0001f ||
            Math.Abs(manaGainWhenHitRateBonus) > 0.0001f ||
            Math.Abs(manaGainPerAttackRateBonus) > 0.0001f;

        public void Add(UnitSynergyBonus other)
        {
            attackPowerBonus += other.attackPowerBonus;
            attackSpeedBonus += other.attackSpeedBonus;
            critChanceBonus += other.critChanceBonus;
            rangeBonus += other.rangeBonus;
            manaRegenRateBonus += other.manaRegenRateBonus;
            maxHealthBonus += other.maxHealthBonus;
            splashRadiusBonus += other.splashRadiusBonus;
            splashDamageRatioBonus += other.splashDamageRatioBonus;
            skillPowerBonus += other.skillPowerBonus;
            criticalDamageBonus += other.criticalDamageBonus;
            bossDamageBonus += other.bossDamageBonus;
            damageReductionBonus += other.damageReductionBonus;
            manaGainWhenHitRateBonus += other.manaGainWhenHitRateBonus;
            manaGainPerAttackRateBonus += other.manaGainPerAttackRateBonus;
        }
    }
}
