using NUnit.Framework;
using SurveHive.Data;
using SurveHive.Progression;

namespace SurveHive.Tests
{
    /// <summary>
    /// PLAN 3B-1 — the tabbed shop groups upgrades by category, derived purely
    /// from each upgrade's stat type. Locks the TODO #25 mapping and guarantees
    /// every stat lands in exactly one tab.
    /// </summary>
    public sealed class MetaShopCategoriesTests
    {
        [Test]
        public void CombatStats_MapToCombat()
        {
            Assert.AreEqual(MetaShopCategory.Combat, MetaShopCategories.For(MetaStatType.AttackDamage));
            Assert.AreEqual(MetaShopCategory.Combat, MetaShopCategories.For(MetaStatType.AttackSpeed));
            Assert.AreEqual(MetaShopCategory.Combat, MetaShopCategories.For(MetaStatType.AbilityPower));
            Assert.AreEqual(MetaShopCategory.Combat, MetaShopCategories.For(MetaStatType.CooldownReduction));
            Assert.AreEqual(MetaShopCategory.Combat, MetaShopCategories.For(MetaStatType.CritChance));
            Assert.AreEqual(MetaShopCategory.Combat, MetaShopCategories.For(MetaStatType.CritDamage));
        }

        [Test]
        public void SurvivalStats_MapToSurvival()
        {
            Assert.AreEqual(MetaShopCategory.Survival, MetaShopCategories.For(MetaStatType.MaxHealth));
            Assert.AreEqual(MetaShopCategory.Survival, MetaShopCategories.For(MetaStatType.MoveSpeed));
            Assert.AreEqual(MetaShopCategory.Survival, MetaShopCategories.For(MetaStatType.MagnetRadius));
        }

        [Test]
        public void UtilityStats_MapToUtility()
        {
            Assert.AreEqual(MetaShopCategory.Utility, MetaShopCategories.For(MetaStatType.CurrencyGain));
            Assert.AreEqual(MetaShopCategory.Utility, MetaShopCategories.For(MetaStatType.ExpGain));
            Assert.AreEqual(MetaShopCategory.Utility, MetaShopCategories.For(MetaStatType.ItemDropRate));
            Assert.AreEqual(MetaShopCategory.Utility, MetaShopCategories.For(MetaStatType.Rerolls));
        }

        [Test]
        public void EveryStatType_HasACategory_AndTabsAreThree()
        {
            Assert.AreEqual(3, MetaShopCategories.Count, "Combat / Survival / Utility");

            foreach (MetaStatType stat in System.Enum.GetValues(typeof(MetaStatType)))
            {
                MetaShopCategory category = MetaShopCategories.For(stat);
                Assert.IsTrue(
                    category == MetaShopCategory.Combat
                    || category == MetaShopCategory.Survival
                    || category == MetaShopCategory.Utility,
                    $"{stat} maps to a real tab");
            }
        }
    }
}
