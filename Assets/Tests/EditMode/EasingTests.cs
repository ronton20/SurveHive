using NUnit.Framework;
using SurveHive.Core;

namespace SurveHive.Tests
{
    /// <summary>
    /// PLAN 3B-2c — the UI transition curves. Endpoints must be exact (0→0, 1→1)
    /// so faded/popped widgets always land precisely on their start/rest state;
    /// OutBack must actually overshoot past 1 mid-curve (that's the "pop").
    /// </summary>
    public sealed class EasingTests
    {
        [Test]
        public void OutCubic_PinsEndpoints()
        {
            Assert.AreEqual(0f, Easing.OutCubic(0f), 1e-6f);
            Assert.AreEqual(1f, Easing.OutCubic(1f), 1e-6f);
        }

        [Test]
        public void OutCubic_IsMonotonicDecelerating()
        {
            // Past the halfway point it's already most of the way there (decel).
            Assert.Greater(Easing.OutCubic(0.5f), 0.5f);
        }

        [Test]
        public void OutBack_PinsEndpoints()
        {
            Assert.AreEqual(0f, Easing.OutBack(0f), 1e-6f);
            Assert.AreEqual(1f, Easing.OutBack(1f), 1e-6f);
        }

        [Test]
        public void OutBack_OvershootsPastOne()
        {
            bool overshot = false;
            for (float t = 0.5f; t < 1f; t += 0.02f)
            {
                if (Easing.OutBack(t) > 1f)
                {
                    overshot = true;
                    break;
                }
            }

            Assert.IsTrue(overshot, "OutBack should exceed 1 before settling (the pop).");
        }
    }
}
