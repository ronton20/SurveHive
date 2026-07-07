using NUnit.Framework;
using SurveHive.Data;
using UnityEditor;

namespace SurveHive.Tests
{
    /// <summary>
    /// Guards the per-level magnitude table (Phase 1A crit rework): Keen Eye
    /// must grant 5/10/15/20/30% cumulative crit over exactly 5 levels, and
    /// skills without a table keep their flat magnitude.
    /// </summary>
    public sealed class SkillMagnitudePerLevelTests
    {
        private const string KeenEyePath = "Assets/Data/Skills/KeenEye.asset";
        private const string SwiftWingsPath = "Assets/Data/Skills/SwiftWings.asset";

        [Test]
        public void KeenEye_HasFiveLevels()
        {
            SkillDefinitionSO keenEye = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(KeenEyePath);
            Assert.IsNotNull(keenEye);
            Assert.AreEqual(5, keenEye.MaxLevel);
        }

        [Test]
        public void KeenEye_GrantsFiveTenFifteenTwentyThirty()
        {
            SkillDefinitionSO keenEye = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(KeenEyePath);
            Assert.IsNotNull(keenEye);

            float[] expectedCumulative = { 5f, 10f, 15f, 20f, 30f };
            float total = 0f;
            for (int level = 0; level < expectedCumulative.Length; level++)
            {
                total += keenEye.MagnitudeForLevel(level);
                Assert.AreEqual(expectedCumulative[level], total,
                    $"cumulative crit after level {level + 1}");
            }
        }

        [Test]
        public void MagnitudeForLevel_PastTableEnd_ReusesLastEntry()
        {
            SkillDefinitionSO keenEye = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(KeenEyePath);
            Assert.IsNotNull(keenEye);
            Assert.AreEqual(10f, keenEye.MagnitudeForLevel(7));
        }

        [Test]
        public void MagnitudeForLevel_WithoutTable_FallsBackToFlatMagnitude()
        {
            SkillDefinitionSO swiftWings = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(SwiftWingsPath);
            Assert.IsNotNull(swiftWings);
            Assert.AreEqual(swiftWings.Magnitude, swiftWings.MagnitudeForLevel(0));
            Assert.AreEqual(swiftWings.Magnitude, swiftWings.MagnitudeForLevel(3));
        }
    }
}
