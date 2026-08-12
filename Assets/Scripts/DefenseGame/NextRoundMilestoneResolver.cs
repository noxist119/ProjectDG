using UnityEngine;

namespace DefenseGame
{
    public readonly struct NextRoundMilestone
    {
        public readonly int nextRound;
        public readonly bool isBossRound;
        public readonly bool isClassicChallengeRound;
        public readonly bool isApproachingMajorHurdle;
        public readonly int nextHurdleRound;
        public readonly int slotUnlockRound;
        public readonly int roundsUntilAugment;
        public readonly int roundsUntilRunShop;

        public NextRoundMilestone(
            int nextRound,
            bool isBossRound,
            bool isClassicChallengeRound,
            bool isApproachingMajorHurdle,
            int nextHurdleRound,
            int slotUnlockRound,
            int roundsUntilAugment,
            int roundsUntilRunShop)
        {
            this.nextRound = nextRound;
            this.isBossRound = isBossRound;
            this.isClassicChallengeRound = isClassicChallengeRound;
            this.isApproachingMajorHurdle = isApproachingMajorHurdle;
            this.nextHurdleRound = nextHurdleRound;
            this.slotUnlockRound = slotUnlockRound;
            this.roundsUntilAugment = roundsUntilAugment;
            this.roundsUntilRunShop = roundsUntilRunShop;
        }
    }

    /// <summary>
    /// Read-only preparation summary. It only reuses existing round schedules and never
    /// changes combat state, reward state, or UnityEngine.Random state.
    /// </summary>
    public static class NextRoundMilestoneResolver
    {
        public static NextRoundMilestone Resolve(
            int completedRound,
            CombatModeProfile combatModeProfile,
            int nextBossRound,
            int nextAugmentRound,
            int nextRunShopRound,
            int nextSlotUnlockRound)
        {
            int safeCompletedRound = Mathf.Max(0, completedRound);
            int nextRound = safeCompletedRound + 1;
            bool isBossRound = nextBossRound > 0 && nextRound == nextBossRound;
            bool classicScope = ClassicRoundPressure.AppliesTo(combatModeProfile, isBossRound);
            bool isClassicChallengeRound = classicScope && ClassicRoundPressure.IsChallengeRound(nextRound);
            bool isApproachingMajorHurdle = !isBossRound &&
                CommercialRoundPacing.TryGetApproachingHurdleIndex(nextRound, out _);
            int nextHurdleRound = CommercialRoundPacing.GetNextHurdleRound(safeCompletedRound);

            return new NextRoundMilestone(
                nextRound,
                isBossRound,
                isClassicChallengeRound,
                isApproachingMajorHurdle,
                nextHurdleRound,
                nextSlotUnlockRound,
                nextAugmentRound > 0 ? Mathf.Max(0, nextAugmentRound - nextRound) : -1,
                nextRunShopRound > 0 ? Mathf.Max(0, nextRunShopRound - nextRound) : -1);
        }
    }
}