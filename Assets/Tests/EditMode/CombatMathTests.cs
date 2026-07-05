using NUnit.Framework;
using SurveHive.Combat;

namespace SurveHive.Tests
{
    public sealed class CombatMathTests
    {
        [Test]
        public void MitigateByArmor_ZeroArmor_NoChange()
        {
            Assert.AreEqual(10f, CombatMath.MitigateByArmor(10f, 0f));
        }

        [Test]
        public void MitigateByArmor_ReducesByPercent()
        {
            // 40% armor on a 10-damage hit → 6 lands.
            Assert.AreEqual(6f, CombatMath.MitigateByArmor(10f, 40f), 0.001f);
        }

        [Test]
        public void MitigateByArmor_NeverBelowOne()
        {
            // 80% armor on a 2-damage hit would be 0.4 — floored to 1.
            Assert.AreEqual(1f, CombatMath.MitigateByArmor(2f, 80f));
        }

        [Test]
        public void MitigateByArmor_NonPositiveAmount_Untouched()
        {
            Assert.AreEqual(0f, CombatMath.MitigateByArmor(0f, 50f));
        }
    }
}
