using NUnit.Framework;
using SurveHive.Enemies;

namespace SurveHive.Tests
{
    /// <summary>
    /// Phase 4A ranged enemies: the pure steering decision — flee inside the
    /// flee radius, chase beyond the chase radius, hold (orbit) in the band.
    /// All inputs are squared distances.
    /// </summary>
    public sealed class RangedSteeringTests
    {
        private const float FleeRangeSqr = 4.5f * 4.5f;
        private const float ChaseRangeSqr = 7.5f * 7.5f;

        [Test]
        public void PlayerTooClose_Flees()
        {
            Assert.AreEqual(RangedSteerMode.Flee,
                RangedSteering.Decide(2f * 2f, FleeRangeSqr, ChaseRangeSqr));
        }

        [Test]
        public void PlayerTooFar_Chases()
        {
            Assert.AreEqual(RangedSteerMode.Chase,
                RangedSteering.Decide(10f * 10f, FleeRangeSqr, ChaseRangeSqr));
        }

        [Test]
        public void PlayerInBand_Holds()
        {
            Assert.AreEqual(RangedSteerMode.Hold,
                RangedSteering.Decide(6f * 6f, FleeRangeSqr, ChaseRangeSqr));
        }

        [Test]
        public void ExactBoundaries_Hold()
        {
            // On-the-line distances stay in Hold so the bee doesn't jitter
            // between modes at the band edges.
            Assert.AreEqual(RangedSteerMode.Hold,
                RangedSteering.Decide(FleeRangeSqr, FleeRangeSqr, ChaseRangeSqr));
            Assert.AreEqual(RangedSteerMode.Hold,
                RangedSteering.Decide(ChaseRangeSqr, FleeRangeSqr, ChaseRangeSqr));
        }

        [Test]
        public void DegenerateBand_StillDecides()
        {
            // Flee wins when the ranges collapse onto each other.
            float rangeSqr = 5f * 5f;
            Assert.AreEqual(RangedSteerMode.Flee,
                RangedSteering.Decide(24f, rangeSqr, rangeSqr));
            Assert.AreEqual(RangedSteerMode.Chase,
                RangedSteering.Decide(26f, rangeSqr, rangeSqr));
        }
    }
}
