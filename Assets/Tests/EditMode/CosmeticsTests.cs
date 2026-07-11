using NUnit.Framework;
using SurveHive.Data;
using SurveHive.Persistence;
using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// PLAN 5C — cosmetics: owned/equipped state round-trips through the save
    /// schema (v7), old saves migrate to the default look, and the shop
    /// transaction spends Royal Jelly + unlocks + equips with all the failure
    /// paths (unaffordable, double-buy, equipping unowned) rejected.
    /// </summary>
    public sealed class CosmeticsTests
    {
        private RuntimeMetaProgressionStoreSO _store;
        private CosmeticSO _hat;

        [SetUp]
        public void SetUp()
        {
            _store = ScriptableObject.CreateInstance<RuntimeMetaProgressionStoreSO>();
            _hat = MakeCosmetic("hat_crown", CosmeticSlot.Hat, 15);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_store);
            Object.DestroyImmediate(_hat);
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

        // ------------------------------------------------------------------
        // State + save round-trip.
        // ------------------------------------------------------------------
        [Test]
        public void State_RoundTripsOwnedAndEquipped()
        {
            var state = new MetaProgressionState();
            Assert.That(state.UnlockCosmetic("color_ruby"), Is.True);
            Assert.That(state.UnlockCosmetic("color_ruby"), Is.False, "double unlock reports not-new");
            state.UnlockCosmetic("hat_crown");
            state.SetEquippedCosmetic((int)CosmeticSlot.Color, "color_ruby");
            state.SetEquippedCosmetic((int)CosmeticSlot.Hat, "hat_crown");

            var data = new SaveData();
            state.WriteTo(data);
            SaveData reloaded = SaveDataSerializer.FromJson(SaveDataSerializer.ToJson(data));

            var restored = new MetaProgressionState();
            restored.LoadFrom(reloaded);
            Assert.That(restored.IsCosmeticOwned("color_ruby"), Is.True);
            Assert.That(restored.IsCosmeticOwned("hat_crown"), Is.True);
            Assert.That(restored.IsCosmeticOwned("stinger_gold"), Is.False);
            Assert.That(restored.GetEquippedCosmetic((int)CosmeticSlot.Color), Is.EqualTo("color_ruby"));
            Assert.That(restored.GetEquippedCosmetic((int)CosmeticSlot.Hat), Is.EqualTo("hat_crown"));
            Assert.That(restored.GetEquippedCosmetic((int)CosmeticSlot.Stinger), Is.Empty);
        }

        [Test]
        public void State_V6SaveMigratesToDefaultLook()
        {
            // A pre-5C save has neither cosmetic field; JsonUtility leaves the
            // initializers (empty arrays) in place.
            SaveData old = SaveDataSerializer.FromJson("{\"version\":6,\"bankedJelly\":9}");

            var state = new MetaProgressionState();
            state.LoadFrom(old);
            Assert.That(state.IsCosmeticOwned("color_ruby"), Is.False);
            for (int slot = 0; slot < CosmeticSlots.Count; slot++)
            {
                Assert.That(state.GetEquippedCosmetic(slot), Is.Empty);
            }
        }

        [Test]
        public void State_OutOfRangeSlotIsSafe()
        {
            var state = new MetaProgressionState();
            state.SetEquippedCosmetic(-1, "x");
            state.SetEquippedCosmetic(99, "x");
            Assert.That(state.GetEquippedCosmetic(-1), Is.Empty);
            Assert.That(state.GetEquippedCosmetic(99), Is.Empty);
        }

        // ------------------------------------------------------------------
        // Shop transactions.
        // ------------------------------------------------------------------
        [Test]
        public void Purchase_SpendsJellyAndUnlocks()
        {
            _store.BankJelly(20);

            Assert.That(CosmeticShop.TryPurchase(_store, _hat), Is.True);
            Assert.That(_store.BankedJelly, Is.EqualTo(5));
            Assert.That(_store.IsCosmeticOwned("hat_crown"), Is.True);
        }

        [Test]
        public void Purchase_FailsWhenUnaffordable()
        {
            _store.BankJelly(14);

            Assert.That(CosmeticShop.TryPurchase(_store, _hat), Is.False);
            Assert.That(_store.BankedJelly, Is.EqualTo(14), "no partial spend");
            Assert.That(_store.IsCosmeticOwned("hat_crown"), Is.False);
        }

        [Test]
        public void Purchase_FailsWhenAlreadyOwned()
        {
            _store.BankJelly(40);
            Assert.That(CosmeticShop.TryPurchase(_store, _hat), Is.True);

            Assert.That(CosmeticShop.TryPurchase(_store, _hat), Is.False, "double buy rejected");
            Assert.That(_store.BankedJelly, Is.EqualTo(25), "second buy spends nothing");
        }

        [Test]
        public void Equip_RequiresOwnership()
        {
            Assert.That(CosmeticShop.TryEquip(_store, CosmeticSlot.Hat, "hat_crown"), Is.False);
            Assert.That(_store.GetEquippedCosmetic((int)CosmeticSlot.Hat), Is.Empty);

            _store.BankJelly(15);
            CosmeticShop.TryPurchase(_store, _hat);
            Assert.That(CosmeticShop.TryEquip(_store, CosmeticSlot.Hat, "hat_crown"), Is.True);
            Assert.That(_store.GetEquippedCosmetic((int)CosmeticSlot.Hat), Is.EqualTo("hat_crown"));
        }

        [Test]
        public void Equip_EmptyIdRevertsToDefault()
        {
            _store.BankJelly(15);
            CosmeticShop.TryPurchase(_store, _hat);
            CosmeticShop.TryEquip(_store, CosmeticSlot.Hat, "hat_crown");

            Assert.That(CosmeticShop.TryEquip(_store, CosmeticSlot.Hat, string.Empty), Is.True);
            Assert.That(_store.GetEquippedCosmetic((int)CosmeticSlot.Hat), Is.Empty);
        }

        [Test]
        public void Purchase_NullArgumentsRejected()
        {
            Assert.That(CosmeticShop.TryPurchase(null, _hat), Is.False);
            Assert.That(CosmeticShop.TryPurchase(_store, null), Is.False);
            Assert.That(CosmeticShop.TryEquip(null, CosmeticSlot.Color, "x"), Is.False);
        }
    }
}
