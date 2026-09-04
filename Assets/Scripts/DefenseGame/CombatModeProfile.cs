using System;
using UnityEngine;

namespace DefenseGame
{
    public enum CombatGameMode
    {
        Classic = 0,
        Overdrive = 1
    }

    /// <summary>
    /// Shared combat rules for a run. The profile changes pacing and presentation
    /// without duplicating scenes, units, monsters, or progression systems.
    /// </summary>
    [Serializable]
    public sealed class CombatModeProfile
    {
        [Header("Identity")]
        public CombatGameMode mode = CombatGameMode.Classic;
        public string displayName = "클래식";
        [TextArea] public string description = "전략과 안정적인 성장을 중심으로 진행합니다.";

        [Header("Wave Density")]
        [Min(0.1f)] public float regularCountMultiplier = 1f;
        [Min(0.1f)] public float bossSupportCountMultiplier = 1f;
        [Min(0)] public int overdriveFirstBossSupportCountOverride;
        [Min(1)] public int maximumRegularCountPerRound = 60;
        public bool useBurstPacks;
        [Min(1)] public int minimumPackSize = 1;
        [Min(1)] public int maximumPackSize = 1;
        [Min(0.02f)] public float intraPackInterval = 0.28f;
        [Min(0.05f)] public float packGap = 0.28f;

        [Header("Horde Rounds")]
        [Min(0)] public int firstHordeRound;
        [Min(0)] public int hordeFrequency;
        [Min(0.1f)] public float hordeCountMultiplier = 1f;
        [Min(0.1f)] public float hordeHealthMultiplier = 1f;
        [Min(0.1f)] public float hordeAttackMultiplier = 1f;
        [Min(1)] public int hordeMinimumPackSize = 1;
        [Min(1)] public int hordeMaximumPackSize = 1;
        [Min(0.02f)] public float hordeIntraPackInterval = 0.28f;
        [Min(0.05f)] public float hordePackGap = 0.28f;

        [Header("Regular Monster Stats")]
        [Min(0.1f)] public float regularHealthMultiplier = 1f;
        [Min(0.1f)] public float regularAttackMultiplier = 1f;

        [Header("Boss Stats")]
        [Min(0.1f)] public float bossHealthMultiplier = 1f;
        [Min(0.1f)] public float bossAttackMultiplier = 1f;

        [Header("Augment Pacing")]
        [Min(1)] public int firstAugmentChoiceRound = 5;
        [Min(1)] public int augmentChoiceInterval = 5;
        [Min(1)] public int rareHeroAugmentUnlockRound = 6;
        [Min(1)] public int mythicHeroAugmentUnlockRound = 10;
        [Range(0f, 1f)] public float heroAugmentOfferChanceMultiplier = 1f;
        public bool guaranteeTransformingAugment;

        [Header("Kill Feedback")]
        [Min(0.2f)] public float killComboWindow = 2.2f;
        public bool useEscalatingKillFeedback;

        public bool IsOverdrive => mode == CombatGameMode.Overdrive;

        public bool IsHordeRound(int round, bool bossRound)
        {
            if (!IsOverdrive || bossRound || firstHordeRound <= 0 || hordeFrequency <= 0 || round < firstHordeRound)
            {
                return false;
            }

            return (round - firstHordeRound) % hordeFrequency == 0;
        }

        public int ApplyRegularCount(int round, bool bossRound, int baseCount)
        {
            bool hordeRound = IsHordeRound(round, bossRound);
            float multiplier = bossRound ? bossSupportCountMultiplier : regularCountMultiplier;
            if (hordeRound)
            {
                multiplier *= hordeCountMultiplier;
            }

            int maximum = Mathf.Max(1, maximumRegularCountPerRound);
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(0, baseCount) * Mathf.Max(0.1f, multiplier)), 0, maximum);
        }

        public int ApplyBossSupportCount(int round, int scaledSupportCount)
        {
            if (IsOverdrive && round == 10 && overdriveFirstBossSupportCountOverride > 0)
            {
                return overdriveFirstBossSupportCountOverride;
            }

            return Mathf.Max(0, scaledSupportCount);
        }

        public float ResolveRegularHealthMultiplier(int round, bool bossRound)
        {
            if (bossRound)
            {
                return Mathf.Max(0.1f, bossHealthMultiplier);
            }

            float multiplier = Mathf.Max(0.1f, regularHealthMultiplier);
            if (IsHordeRound(round, false))
            {
                multiplier *= Mathf.Max(0.1f, hordeHealthMultiplier);
            }

            return multiplier;
        }

        public float ResolveRegularAttackMultiplier(int round, bool bossRound)
        {
            if (bossRound)
            {
                return Mathf.Max(0.1f, bossAttackMultiplier);
            }

            float multiplier = Mathf.Max(0.1f, regularAttackMultiplier);
            if (IsHordeRound(round, false))
            {
                multiplier *= Mathf.Max(0.1f, hordeAttackMultiplier);
            }

            return multiplier;
        }

        public int ResolvePackSize(int round, int packIndex, bool hordeRound)
        {
            int minimum = Mathf.Max(1, hordeRound ? hordeMinimumPackSize : minimumPackSize);
            int maximum = Mathf.Max(minimum, hordeRound ? hordeMaximumPackSize : maximumPackSize);
            if (minimum == maximum)
            {
                return minimum;
            }

            int hash = unchecked(round * 397 ^ packIndex * 97 ^ (hordeRound ? 7919 : 0));
            int offset = (hash & int.MaxValue) % (maximum - minimum + 1);
            return minimum + offset;
        }

        public float ResolveIntraPackInterval(bool hordeRound)
        {
            return Mathf.Max(0.02f, hordeRound ? hordeIntraPackInterval : intraPackInterval);
        }

        public float ResolvePackGap(bool hordeRound)
        {
            return Mathf.Max(0.05f, hordeRound ? hordePackGap : packGap);
        }

        public static CombatModeProfile CreateClassic()
        {
            return new CombatModeProfile();
        }

        public static CombatModeProfile CreateOverdrive()
        {
            return new CombatModeProfile
            {
                mode = CombatGameMode.Overdrive,
                displayName = "폭주",
                description = "짧고 강한 물량 파도와 빠른 증강 성장을 중심으로 진행합니다.",
                regularCountMultiplier = 1.26f,
                bossSupportCountMultiplier = 1.06f,
                overdriveFirstBossSupportCountOverride = 8,
                maximumRegularCountPerRound = 84,
                useBurstPacks = true,
                minimumPackSize = 4,
                maximumPackSize = 6,
                intraPackInterval = 0.09f,
                packGap = 0.72f,
                firstHordeRound = 4,
                hordeFrequency = 3,
                hordeCountMultiplier = 1.20f,
                hordeHealthMultiplier = 0.66f,
                hordeAttackMultiplier = 0.60f,
                hordeMinimumPackSize = 6,
                hordeMaximumPackSize = 8,
                hordeIntraPackInterval = 0.065f,
                hordePackGap = 0.56f,
                regularHealthMultiplier = 0.74f,
                regularAttackMultiplier = 0.70f,
                bossHealthMultiplier = 0.72f,
                bossAttackMultiplier = 0.70f,
                firstAugmentChoiceRound = 6,
                augmentChoiceInterval = 4,
                rareHeroAugmentUnlockRound = 5,
                mythicHeroAugmentUnlockRound = 8,
                heroAugmentOfferChanceMultiplier = 1.35f,
                guaranteeTransformingAugment = true,
                killComboWindow = 2.8f,
                useEscalatingKillFeedback = true
            };
        }
    }
}
