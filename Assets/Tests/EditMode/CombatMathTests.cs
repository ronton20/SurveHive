using NUnit.Framework;
using SurveHive.Combat;
using UnityEngine;

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

        [Test]
        public void Multishot_SingleProjectile_IsBaseDamage()
        {
            Assert.AreEqual(10f, CombatMath.MultishotPerProjectileDamage(10f, 1));
            Assert.AreEqual(10f, CombatMath.MultishotPerProjectileDamage(10f, 0));
        }

        [Test]
        public void Multishot_TotalScales1point5PerExtraProjectile()
        {
            const float baseDamage = 10f;
            for (int count = 1; count <= 5; count++)
            {
                float per = CombatMath.MultishotPerProjectileDamage(baseDamage, count);
                float total = per * count;
                float expectedTotal = baseDamage * Mathf.Pow(1.5f, count - 1);
                Assert.AreEqual(expectedTotal, total, 0.001f, $"count {count}");
            }
        }

        [Test]
        public void Multishot_PerProjectile_DecreasesAsCountGrows()
        {
            float two = CombatMath.MultishotPerProjectileDamage(10f, 2);
            float three = CombatMath.MultishotPerProjectileDamage(10f, 3);
            // Each individual projectile hits softer than the single-shot base.
            Assert.Less(two, 10f);
            Assert.AreEqual(7.5f, two, 0.001f);
            Assert.Less(three, two + 0.5f);
        }

        [Test]
        public void PierceCount_ScalesThenBecomesInfiniteAtMax()
        {
            const int max = 3;
            Assert.AreEqual(0, CombatMath.PierceCount(0, max), "unowned");
            Assert.AreEqual(2, CombatMath.PierceCount(1, max));
            Assert.AreEqual(4, CombatMath.PierceCount(2, max));
            Assert.AreEqual(int.MaxValue, CombatMath.PierceCount(3, max), "max level pierces everything");
            Assert.AreEqual(int.MaxValue, CombatMath.PierceCount(5, max), "beyond max stays infinite");
        }

        [Test]
        public void PierceDamageMultiplier_PenaltyLightensAndVanishesAtMax()
        {
            const int max = 3;
            // Unowned: no penalty. L1 −30%, L2 −20%, L3 (max) restored to full.
            Assert.AreEqual(1f, CombatMath.PierceDamageMultiplier(0, max, 0.30f, 0.10f), 0.0001f);
            Assert.AreEqual(0.70f, CombatMath.PierceDamageMultiplier(1, max, 0.30f, 0.10f), 0.0001f);
            Assert.AreEqual(0.80f, CombatMath.PierceDamageMultiplier(2, max, 0.30f, 0.10f), 0.0001f);
            Assert.AreEqual(1f, CombatMath.PierceDamageMultiplier(3, max, 0.30f, 0.10f), 0.0001f);
        }
    }
}
