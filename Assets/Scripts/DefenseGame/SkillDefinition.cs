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
        ManaSurge = 6
    }

    [Serializable]
    public class SkillDefinition
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public SkillEffectType effectType;
        public float power = 1f;
        public float duration = 3f;
        public float radius = 2.5f;
        public float manaThreshold = 100f;
        public float cooldown = 4f;
    }
}

