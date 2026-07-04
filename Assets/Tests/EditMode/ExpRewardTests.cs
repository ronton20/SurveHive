using NUnit.Framework;
using SurveHive.Enemies;
using SurveHive.Pickups;

namespace SurveHive.Tests
{
    public sealed class ExpRewardTests
    {
        [Test]
        public void Reward_GrowsWithHealth()
        {
            Assert.Greater(ExpRewardCalculator.Calculate(0, 40f), ExpRewardCalculator.Calculate(0, 20f));
        }

        [Test]
        public void Reward_RankIsTheDominantFactor()
        {
            // A higher rank at its natural HP always beats a lower rank, even
            // when the lower rank is late-run health-scaled (~2.6x at min 9).
            Assert.Greater(ExpRewardCalculator.Calculate(1, 45f), ExpRewardCalculator.Calculate(0, 52f));
            Assert.Greater(ExpRewardCalculator.Calculate(2, 90f), ExpRewardCalculator.Calculate(1, 117f));
            Assert.Greater(ExpRewardCalculator.Calculate(3, 900f), ExpRewardCalculator.Calculate(2, 234f));
            Assert.Greater(ExpRewardCalculator.Calculate(4, 3500f), ExpRewardCalculator.Calculate(3, 900f));
        }

        [Test]
        public void Reward_MatchesEarlyGameTuning()
        {
            // Baseline worker at minute 0: ~5 EXP, as before the formula.
            Assert.AreEqual(5f, ExpRewardCalculator.Calculate(0, 20f));
        }

        [Test]
        public void OrbTiers_ThresholdsAndClamping()
        {
            Assert.AreEqual(0, ExpOrbTiers.GetTier(5f));
            Assert.AreEqual(1, ExpOrbTiers.GetTier(10f));
            Assert.AreEqual(1, ExpOrbTiers.GetTier(29f));
            Assert.AreEqual(2, ExpOrbTiers.GetTier(30f));
            Assert.AreEqual(3, ExpOrbTiers.GetTier(120f));
            Assert.AreEqual(3, ExpOrbTiers.GetTier(99999f));

            // Bigger tiers render bigger.
            Assert.Greater(ExpOrbTiers.GetScale(3), ExpOrbTiers.GetScale(0));
        }
    }
}
