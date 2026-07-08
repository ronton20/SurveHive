using NUnit.Framework;
using SurveHive.Enemies;

namespace SurveHive.Tests
{
    /// <summary>
    /// PLAN 1A anti-stall enrage: the Queen's damage factor stays 1 until the
    /// enrage starts, ramps linearly, and clamps at the configured maximum —
    /// so a patience-war chip-fest has a hard ceiling.
    /// </summary>
    public sealed class QueenEnrageTests
    {
        [Test]
        public void BeforeEnrageStart_FactorIsOne()
        {
            Assert.AreEqual(1f, QueenBossController.EnrageFactor(0f, 60f, 60f, 2.5f));
            Assert.AreEqual(1f, QueenBossController.EnrageFactor(60f, 60f, 60f, 2.5f));
        }

        [Test]
        public void MidRamp_FactorIsLinear()
        {
            Assert.AreEqual(1.75f, QueenBossController.EnrageFactor(90f, 60f, 60f, 2.5f), 1e-4f);
        }

        [Test]
        public void PastRamp_FactorClampsAtMax()
        {
            Assert.AreEqual(2.5f, QueenBossController.EnrageFactor(120f, 60f, 60f, 2.5f), 1e-4f);
            Assert.AreEqual(2.5f, QueenBossController.EnrageFactor(3600f, 60f, 60f, 2.5f), 1e-4f);
        }

        [Test]
        public void ZeroRampDuration_JumpsStraightToMax()
        {
            Assert.AreEqual(2.5f, QueenBossController.EnrageFactor(60.01f, 60f, 0f, 2.5f), 1e-4f);
        }

        [Test]
        public void DisabledMultiplier_StaysOne()
        {
            Assert.AreEqual(1f, QueenBossController.EnrageFactor(999f, 60f, 60f, 1f));
        }
    }
}
