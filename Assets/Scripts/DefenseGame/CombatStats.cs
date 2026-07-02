using System;
using UnityEngine;

namespace DefenseGame
{
    [Serializable]
    public class CombatStats
    {
        public float maxHealth = 100f;
        public float attackPower = 10f;
        [Range(0f, 1f)] public float criticalChance = 0.1f;
        public float criticalDamageMultiplier = 1.5f;
        public float attackSpeed = 1f;
        public float maxMana = 100f;
        [Range(0f, 1f)] public float manaRegenPerSecondRate = 0.05f;
        [Range(0f, 1f)] public float manaGainWhenHitRate = 0.10f;
        [Range(0f, 1f)] public float manaGainPerAttackRate = 0.15f;
        public float attackRange = 5f;
        public float moveSpeed = 1.5f;
        public float projectileSpeed = 10f;
    }
}

