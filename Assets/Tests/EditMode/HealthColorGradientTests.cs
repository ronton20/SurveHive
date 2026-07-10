using NUnit.Framework;
using SurveHive.UI;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// PLAN 3B-2d — the player-bar health-ratio gradient. Endpoints and the midpoint
    /// are fixed anchors (healthy green / honey amber / danger red) and the red
    /// channel must rise monotonically as health falls, so "low = redder" always reads.
    /// </summary>
    public sealed class HealthColorGradientTests
    {
        [Test]
        public void Endpoints_AreGreenAndRed()
        {
            Color full = HealthColorGradient.Evaluate(1f);
            Color empty = HealthColorGradient.Evaluate(0f);

            Assert.Greater(full.g, full.r, "Full health should read green (more green than red).");
            Assert.Greater(empty.r, empty.g, "Empty health should read red (more red than green).");
        }

        [Test]
        public void Clamps_OutOfRangeInput()
        {
            Assert.AreEqual(HealthColorGradient.Evaluate(1f), HealthColorGradient.Evaluate(2f));
            Assert.AreEqual(HealthColorGradient.Evaluate(0f), HealthColorGradient.Evaluate(-1f));
        }

        [Test]
        public void GreenChannel_FallsAsHealthFalls()
        {
            // Green is the monotonic axis (healthy green → amber → red); the red
            // channel isn't, because honey amber at the midpoint is redder than the
            // danger-red endpoint. Green decreasing as health drops is what makes
            // "greener = healthier" read.
            float previousGreen = HealthColorGradient.Evaluate(1f).g;
            for (float ratio = 0.9f; ratio >= 0f; ratio -= 0.1f)
            {
                float green = HealthColorGradient.Evaluate(ratio).g;
                Assert.LessOrEqual(green - 1e-4f, previousGreen,
                    $"Green should not rise as health falls (ratio {ratio}).");
                previousGreen = green;
            }
        }
    }
}
