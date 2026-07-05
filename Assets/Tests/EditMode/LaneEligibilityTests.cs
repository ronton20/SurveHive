using System.Collections.Generic;
using NUnit.Framework;
using SurveHive.Progression;

namespace SurveHive.Tests
{
    /// <summary>
    /// Combat 2.0 per-lane offer caps (PLAN 1B): once a lane hits its distinct-pick
    /// cap, no new pick from it is offered, but owned picks keep being offered until
    /// they max out.
    /// </summary>
    public sealed class LaneEligibilityTests
    {
        // Lane indices match PowerUpLane: Passive=0, Enhancement=1, Ability=2.
        private static readonly int[] Caps = { 5, 3, 5 };

        private static HashSet<int> Eligible(int[] lanes, int[] levels, int[] maxLevels)
        {
            int n = lanes.Length;
            var eligible = new int[n];
            var scratch = new int[Caps.Length];
            int count = LaneEligibility.BuildEligible(
                lanes, levels, maxLevels, n, Caps, Caps.Length, scratch, eligible);

            var set = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                set.Add(eligible[i]);
            }

            return set;
        }

        [Test]
        public void PassiveLaneFull_NoNewPassiveOffered_OwnedStillOffered()
        {
            // 5 owned passives (cap 5) + 3 un-owned passives; none maxed.
            int[] lanes = { 0, 0, 0, 0, 0, 0, 0, 0 };
            int[] levels = { 1, 1, 1, 1, 1, 0, 0, 0 };
            int[] maxLevels = { 4, 4, 4, 4, 4, 4, 4, 4 };

            var set = Eligible(lanes, levels, maxLevels);

            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3, 4 }, set,
                "only the 5 owned passives remain offerable once the lane is full");
        }

        [Test]
        public void PassiveLaneBelowCap_UnownedStillOffered()
        {
            // Only 3 owned (cap 5): un-owned passives stay eligible.
            int[] lanes = { 0, 0, 0, 0, 0, 0, 0, 0 };
            int[] levels = { 1, 1, 1, 0, 0, 0, 0, 0 };
            int[] maxLevels = { 4, 4, 4, 4, 4, 4, 4, 4 };

            var set = Eligible(lanes, levels, maxLevels);

            Assert.AreEqual(8, set.Count, "all 8 passives offerable while below the cap");
        }

        [Test]
        public void MaxedOwnedSkill_ExcludedButStillCountsTowardCap()
        {
            // 5 owned passives (index 0 is maxed), 1 un-owned. Lane is full (5),
            // so index 5 gets no new pick; index 0 is maxed so it drops out too.
            int[] lanes = { 0, 0, 0, 0, 0, 0 };
            int[] levels = { 4, 1, 1, 1, 1, 0 };
            int[] maxLevels = { 4, 4, 4, 4, 4, 4 };

            var set = Eligible(lanes, levels, maxLevels);

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, set);
        }

        [Test]
        public void CapsAreIndependentPerLane()
        {
            // passive: 1 owned + 1 un-owned (cap 5 → both fine)
            // enhancement: 3 owned + 1 un-owned (cap 3 → un-owned blocked)
            // ability: 1 un-owned (cap 5 → offered)
            int[] lanes = { 0, 0, 1, 1, 1, 1, 2 };
            int[] levels = { 1, 0, 1, 1, 1, 0, 0 };
            int[] maxLevels = { 5, 5, 5, 5, 5, 5, 5 };

            var set = Eligible(lanes, levels, maxLevels);

            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3, 4, 6 }, set,
                "only the 4th enhancement (index 5) is blocked by its full lane");
        }

        [Test]
        public void UncappedMaxLevel_TreatedAsNeverMaxed()
        {
            // maxLevels 0 = uncapped: a high-level owned skill is still offerable.
            int[] lanes = { 2 };
            int[] levels = { 99 };
            int[] maxLevels = { 0 };

            var set = Eligible(lanes, levels, maxLevels);

            CollectionAssert.AreEquivalent(new[] { 0 }, set);
        }
    }
}
