using System;
using UnityEngine;

namespace DefenseGame
{
    [Serializable]
    public class AttackBehavior
    {
        [Header("Range")]
        public bool useCustomAttackRange;
        public float customAttackRange = 6f;

        [Header("Basic Attack Extras")]
        public float splashRadius;
        [Range(0f, 1f)] public float splashDamageRatio;
        public int additionalPierceCount;

        public float ResolveAttackRange(float baseRange)
        {
            return useCustomAttackRange ? customAttackRange : baseRange;
        }
    }
}
