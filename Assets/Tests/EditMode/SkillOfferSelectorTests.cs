using NUnit.Framework;
using SurveHive.Progression;

namespace SurveHive.Tests
{
    public sealed class SkillOfferSelectorTests
    {
        [Test]
        public void Select_ReturnsDistinctEntries()
        {
            int[] eligible = { 10, 11, 12, 13, 14 };
            float[] weights = { 1f, 1f, 1f, 1f, 1f };
            int[] result = new int[3];
            float[] scratch = new float[eligible.Length];
            var rng = new System.Random(1234);

            for (int run = 0; run < 200; run++)
            {
                int picked = SkillOfferSelector.Select(eligible, weights, eligible.Length, 3, result, scratch, rng);
                Assert.AreEqual(3, picked);
                Assert.AreNotEqual(result[0], result[1]);
                Assert.AreNotEqual(result[0], result[2]);
                Assert.AreNotEqual(result[1], result[2]);
            }
        }

        [Test]
        public void Select_ClampsToEligibleCount()
        {
            int[] eligible = { 7, 8 };
            float[] weights = { 1f, 1f };
            int[] result = new int[3];
            float[] scratch = new float[2];
            var rng = new System.Random(42);

            int picked = SkillOfferSelector.Select(eligible, weights, 2, 3, result, scratch, rng);
            Assert.AreEqual(2, picked);
        }

        [Test]
        public void Select_ZeroWeightEntriesAreNeverPicked()
        {
            int[] eligible = { 1, 2, 3 };
            float[] weights = { 1f, 0f, 1f };
            int[] result = new int[3];
            float[] scratch = new float[3];
            var rng = new System.Random(7);

            for (int run = 0; run < 200; run++)
            {
                int picked = SkillOfferSelector.Select(eligible, weights, 3, 3, result, scratch, rng);
                Assert.AreEqual(2, picked);
                for (int i = 0; i < picked; i++)
                {
                    Assert.AreNotEqual(2, result[i]);
                }
            }
        }

        [Test]
        public void Select_RarityWeights_ProduceExpectedDistribution()
        {
            // One common, one rare, one epic card competing for a single slot.
            int[] eligible = { 0, 1, 2 };
            float[] weights =
            {
                SkillRarityWeights.CommonWeight,
                SkillRarityWeights.RareWeight,
                SkillRarityWeights.EpicWeight,
            };
            int[] result = new int[1];
            float[] scratch = new float[3];
            var rng = new System.Random(2026);

            const int draws = 30000;
            int commonCount = 0, rareCount = 0, epicCount = 0;
            for (int i = 0; i < draws; i++)
            {
                SkillOfferSelector.Select(eligible, weights, 3, 1, result, scratch, rng);
                switch (result[0])
                {
                    case 0: commonCount++; break;
                    case 1: rareCount++; break;
                    case 2: epicCount++; break;
                }
            }

            // Expected proportions: 1 / 0.4 / 0.15 of 1.55 total.
            float total = SkillRarityWeights.CommonWeight + SkillRarityWeights.RareWeight + SkillRarityWeights.EpicWeight;
            AssertWithinTolerance(commonCount / (float)draws, SkillRarityWeights.CommonWeight / total, 0.02f, "common share");
            AssertWithinTolerance(rareCount / (float)draws, SkillRarityWeights.RareWeight / total, 0.02f, "rare share");
            AssertWithinTolerance(epicCount / (float)draws, SkillRarityWeights.EpicWeight / total, 0.02f, "epic share");

            // The headline behavior: epics appear noticeably less often than commons.
            Assert.Greater(commonCount, rareCount, "common should beat rare");
            Assert.Greater(rareCount, epicCount, "rare should beat epic");
        }

        private static void AssertWithinTolerance(float actual, float expected, float tolerance, string label)
        {
            Assert.That(actual, Is.InRange(expected - tolerance, expected + tolerance),
                $"{label}: expected ~{expected:F3}, got {actual:F3}");
        }
    }
}
