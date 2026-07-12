using System;
using System.Collections.Generic;
using NUnit.Framework;
using SurveHive.Data;
using SurveHive.Persistence;
using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// PLAN 5E — rotating cosmetics shop: the date-seeded pick is deterministic,
    /// distinct, capped, and owned-exclusive (by candidate construction); the
    /// deal price discounts correctly; the day's picks round-trip through the
    /// save schema (v9) with old saves migrating to "never picked"; and buying
    /// at the deal price spends exactly the discounted amount.
    /// </summary>
    public sealed class RotatingShopTests
    {
        private static readonly string[] Candidates =
        {
            "color_ruby", "color_sapphire", "color_emerald", "color_amethyst", "color_onyx",
            "hat_crown", "hat_tophat", "hat_daisy",
            "stinger_needle_amber", "stinger_barb_sapphire", "stinger_blade_venom",
        };

        // ------------------------------------------------------------------
        // Day stamp + rollover clock.
        // ------------------------------------------------------------------
        [Test]
        public void DayStamp_EncodesLocalDate()
        {
            Assert.That(RotatingShop.DayStamp(new DateTime(2026, 7, 12, 15, 30, 0)), Is.EqualTo(20260712));
            Assert.That(RotatingShop.DayStamp(new DateTime(2026, 7, 12, 23, 59, 59)), Is.EqualTo(20260712));
            Assert.That(RotatingShop.DayStamp(new DateTime(2026, 7, 13, 0, 0, 0)), Is.EqualTo(20260713));
        }

        [Test]
        public void SecondsUntilRollover_CountsToLocalMidnight()
        {
            Assert.That(RotatingShop.SecondsUntilRollover(new DateTime(2026, 7, 12, 23, 59, 30)), Is.EqualTo(30));
            Assert.That(RotatingShop.SecondsUntilRollover(new DateTime(2026, 7, 12, 0, 0, 0)), Is.EqualTo(86400));
            Assert.That(RotatingShop.SecondsUntilRollover(new DateTime(2026, 7, 12, 12, 0, 0)), Is.EqualTo(43200));
        }

        // ------------------------------------------------------------------
        // Deal price: 30% off, rounded half-up, never below 1.
        // ------------------------------------------------------------------
        [Test]
        public void DealPrice_DiscountsRoundedHalfUp()
        {
            Assert.That(RotatingShop.DealPrice(0), Is.EqualTo(0), "free stays free");
            Assert.That(RotatingShop.DealPrice(1), Is.EqualTo(1), "never below 1");
            Assert.That(RotatingShop.DealPrice(3), Is.EqualTo(2));
            Assert.That(RotatingShop.DealPrice(10), Is.EqualTo(7));
            Assert.That(RotatingShop.DealPrice(15), Is.EqualTo(11), "10.5 rounds up");
            Assert.That(RotatingShop.DealPrice(18), Is.EqualTo(13), "12.6 rounds up");
        }

        [Test]
        public void DealPrice_NeverExceedsListPrice()
        {
            for (int cost = 1; cost <= 30; cost++)
            {
                Assert.That(RotatingShop.DealPrice(cost), Is.LessThanOrEqualTo(cost));
                Assert.That(RotatingShop.DealPrice(cost), Is.GreaterThanOrEqualTo(1));
            }
        }

        // ------------------------------------------------------------------
        // The date-seeded pick.
        // ------------------------------------------------------------------
        [Test]
        public void Pick_IsDeterministicForADay()
        {
            var first = new List<string>();
            var second = new List<string>();
            RotatingShop.Pick(Candidates, 20260712, first);
            RotatingShop.Pick(Candidates, 20260712, second);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.Count, Is.EqualTo(RotatingShop.DealsPerDay));
        }

        [Test]
        public void Pick_ReturnsDistinctEntriesFromTheCandidates()
        {
            var results = new List<string>();
            RotatingShop.Pick(Candidates, 20260712, results);

            Assert.That(results, Is.Unique);
            foreach (string id in results)
            {
                Assert.That(Candidates, Does.Contain(id));
            }
        }

        [Test]
        public void Pick_CapsAtTheCandidateCount()
        {
            var results = new List<string>();
            RotatingShop.Pick(new[] { "hat_crown", "color_onyx" }, 20260712, results);
            Assert.That(results, Is.EquivalentTo(new[] { "hat_crown", "color_onyx" }));

            RotatingShop.Pick(new string[0], 20260712, results);
            Assert.That(results, Is.Empty, "no candidates → no deals");

            RotatingShop.Pick(null, 20260712, results);
            Assert.That(results, Is.Empty);
        }

        // ------------------------------------------------------------------
        // Save round-trip + migration.
        // ------------------------------------------------------------------
        [Test]
        public void State_RoundTripsTheDailyDeals()
        {
            var state = new MetaProgressionState();
            state.SetDailyDeals(20260712, new List<string> { "hat_crown", "color_onyx" });

            var data = new SaveData();
            state.WriteTo(data);
            SaveData reloaded = SaveDataSerializer.FromJson(SaveDataSerializer.ToJson(data));

            var restored = new MetaProgressionState();
            restored.LoadFrom(reloaded);
            Assert.That(restored.DailyDealDay, Is.EqualTo(20260712));
            Assert.That(restored.GetDailyDealIds(), Is.EqualTo(new[] { "hat_crown", "color_onyx" }));
        }

        [Test]
        public void State_V8SaveMigratesToNeverPicked()
        {
            // A pre-5E save has neither deals field; the initializers hold.
            SaveData old = SaveDataSerializer.FromJson("{\"version\":8,\"bankedJelly\":9}");

            var state = new MetaProgressionState();
            state.LoadFrom(old);
            Assert.That(state.DailyDealDay, Is.EqualTo(-1), "never matches a real day stamp");
            Assert.That(state.GetDailyDealIds(), Is.Empty);
        }

        // ------------------------------------------------------------------
        // Buying at the deal price.
        // ------------------------------------------------------------------
        [Test]
        public void Purchase_AtDealPriceSpendsTheDiscountedAmount()
        {
            var store = ScriptableObject.CreateInstance<RuntimeMetaProgressionStoreSO>();
            var hat = MakeCosmetic("hat_crown", CosmeticSlot.Hat, 15);
            try
            {
                int dealPrice = RotatingShop.DealPrice(15);
                store.BankJelly(dealPrice);

                Assert.That(CosmeticShop.TryPurchase(store, hat), Is.False,
                    "list price (15) unaffordable at the deal budget");
                Assert.That(CosmeticShop.TryPurchase(store, hat, dealPrice), Is.True);
                Assert.That(store.BankedJelly, Is.EqualTo(0), "spent exactly the deal price");
                Assert.That(store.IsCosmeticOwned("hat_crown"), Is.True);

                Assert.That(CosmeticShop.TryPurchase(store, hat, dealPrice), Is.False, "double buy rejected");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(store);
                UnityEngine.Object.DestroyImmediate(hat);
            }
        }

        [Test]
        public void Store_PersistsTheDayFreeze()
        {
            var store = ScriptableObject.CreateInstance<RuntimeMetaProgressionStoreSO>();
            try
            {
                Assert.That(store.GetDailyDealDay(), Is.EqualTo(-1), "fresh store never picked");

                store.SetDailyDeals(20260712, new List<string> { "color_ruby" });
                Assert.That(store.GetDailyDealDay(), Is.EqualTo(20260712));
                Assert.That(store.GetDailyDealIds(), Is.EqualTo(new[] { "color_ruby" }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(store);
            }
        }

        private static CosmeticSO MakeCosmetic(string id, CosmeticSlot slot, int cost)
        {
            var cosmetic = ScriptableObject.CreateInstance<CosmeticSO>();
            var so = new UnityEditor.SerializedObject(cosmetic);
            so.FindProperty("_cosmeticId").stringValue = id;
            so.FindProperty("_slot").intValue = (int)slot;
            so.FindProperty("_jellyCost").intValue = cost;
            so.ApplyModifiedPropertiesWithoutUndo();
            return cosmetic;
        }
    }
}
