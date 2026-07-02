using System;
using UnityEngine;

namespace DefenseGame
{
    public enum BasicAttackType
    {
        Melee = 0,
        Ranged = 1
    }

    [Serializable]
    public class AttackBehavior
    {
        [Header("Basic Attack Type")]
        public BasicAttackType basicAttackType = BasicAttackType.Ranged;
        public bool useAttackTypeRange;
        public float meleeAttackRange = 2.2f;
        public float rangedAttackRange = 6f;

        [Header("Range")]
        public bool useCustomAttackRange;
        public float customAttackRange = 6f;

        [Header("Basic Attack Extras")]
        public float splashRadius;
        [Range(0f, 1f)] public float splashDamageRatio;
        public int additionalPierceCount;

        [Header("Basic Attack Resources")]
        public GameObject projectilePrefabOverride;
        public GameObject muzzleEffectPrefab;
        public GameObject hitEffectPrefab;

        public bool IsMelee => basicAttackType == BasicAttackType.Melee;

        public float ResolveAttackRange(float baseRange)
        {
            if (useCustomAttackRange)
            {
                return customAttackRange;
            }

            if (useAttackTypeRange)
            {
                return basicAttackType == BasicAttackType.Melee ? meleeAttackRange : rangedAttackRange;
            }

            return baseRange;
        }
    }
}
