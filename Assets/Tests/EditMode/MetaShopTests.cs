using NUnit.Framework;
using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Persistence;
using SurveHive.Progression;
using UnityEditor;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// Meta shop math and transactions: escalating cost curve, spend/bank
    /// bookkeeping, rank caps, and the authored Phase 4 upgrade assets.
    /// </summary>
    public sealed class MetaShopTests
    {
        private static readonly string[] MetaUpgradePaths =
        {
            "Assets/Data/Meta/MaxHealth.asset",
            "Assets/Data/Meta/Damage.asset",
            "Assets/Data/Meta/MoveSpeed.asset",
            "Assets/Data/Meta/AttackSpeed.asset",
            "Assets/Data/Meta/Magnet.asset",
            "Assets/Data/Meta/CurrencyGain.asset",
        };

        /// <summary>In-memory store double built on the real state logic.</summary>
        private sealed class FakeStore : IMetaProgressionStore
        {
            private readonly MetaProgressionState _state = new MetaProgressionState();

            public int BankedCurrency => _state.BankedCurrency;

            public void BankRunCurrency(int amount) => _state.Bank(amount);

            public bool TrySpendCurrency(int amount) => _state.TrySpend(amount);

            public int GetUpgradeRank(string upgradeId) => _state.GetRank(upgradeId);

            public void SetUpgradeRank(string upgradeId, int rank) => _state.SetRank(upgradeId, rank);

            public void RecordStageClear(string stageId, int difficulty) => _state.RecordStageClear(stageId, difficulty);

            public bool HasStageClear(string stageId, int difficulty) => _state.HasStageClear(stageId, difficulty);
        }

        private static MetaUpgradeSO CreateUpgrade(
            string id, MetaStatType stat, int maxRank, int baseCost, float costGrowth, float effectPerRank)
        {
            var upgrade = ScriptableObject.CreateInstance<MetaUpgradeSO>();
            var so = new SerializedObject(upgrade);
            so.FindProperty("_upgradeId").stringValue = id;
            so.FindProperty("_displayName").stringValue = id;
            so.FindProperty("_statType").enumValueIndex = (int)stat;
            so.FindProperty("_maxRank").intValue = maxRank;
            so.FindProperty("_baseCost").intValue = baseCost;
            so.FindProperty("_costGrowth").floatValue = costGrowth;
            so.FindProperty("_effectPerRank").floatValue = effectPerRank;
            so.ApplyModifiedPropertiesWithoutUndo();
            return upgrade;
        }

        [Test]
        public void CostForRank_EscalatesMonotonically()
        {
            Assert.AreEqual(50, MetaUpgradeMath.CostForRank(50, 1.5f, 0));
            Assert.AreEqual(75, MetaUpgradeMath.CostForRank(50, 1.5f, 1));

            int previous = 0;
            for (int rank = 0; rank < 10; rank++)
            {
                int cost = MetaUpgradeMath.CostForRank(50, 1.5f, rank);
                Assert.Greater(cost, previous, $"cost should escalate at rank {rank}");
                previous = cost;
            }
        }

        [Test]
        public void TotalEffect_StacksLinearly()
        {
            Assert.AreEqual(0f, MetaUpgradeMath.TotalEffectAtRank(4f, 0));
            Assert.AreEqual(12f, MetaUpgradeMath.TotalEffectAtRank(4f, 3));
            Assert.AreEqual(0f, MetaUpgradeMath.TotalEffectAtRank(4f, -2), "negative ranks clamp to 0");
        }

        [Test]
        public void State_BankAndSpend_Bookkeeping()
        {
            var state = new MetaProgressionState();

            state.Bank(100);
            state.Bank(-5); // ignored
            Assert.AreEqual(100, state.BankedCurrency);

            Assert.IsFalse(state.TrySpend(101), "cannot overspend");
            Assert.IsFalse(state.TrySpend(0), "zero spend rejected");
            Assert.IsTrue(state.TrySpend(40));
            Assert.AreEqual(60, state.BankedCurrency);
        }

        [Test]
        public void State_SaveDataRoundTrip_PreservesRanksAndCurrency()
        {
            var state = new MetaProgressionState();
            state.Bank(250);
            state.SetRank("meta_a", 3);
            state.SetRank("meta_b", 1);

            var data = new SaveData();
            state.WriteTo(data);

            var restored = new MetaProgressionState();
            restored.LoadFrom(data);

            Assert.AreEqual(250, restored.BankedCurrency);
            Assert.AreEqual(3, restored.GetRank("meta_a"));
            Assert.AreEqual(1, restored.GetRank("meta_b"));
            Assert.AreEqual(0, restored.GetRank("meta_missing"));
        }

        [Test]
        public void TryPurchase_SpendsAndRanksUp()
        {
            var store = new FakeStore();
            store.BankRunCurrency(100);
            MetaUpgradeSO upgrade = CreateUpgrade("meta_test", MetaStatType.MaxHealth, 3, 40, 2f, 10f);

            Assert.IsTrue(MetaShop.TryPurchase(store, upgrade), "rank 0 -> 1 costs 40");
            Assert.AreEqual(1, store.GetUpgradeRank("meta_test"));
            Assert.AreEqual(60, store.BankedCurrency);

            Assert.IsFalse(MetaShop.TryPurchase(store, upgrade), "rank 1 -> 2 costs 80, only 60 left");
            Assert.AreEqual(1, store.GetUpgradeRank("meta_test"));
            Assert.AreEqual(60, store.BankedCurrency, "failed purchase must not spend");

            Object.DestroyImmediate(upgrade);
        }

        [Test]
        public void TryPurchase_RespectsMaxRank()
        {
            var store = new FakeStore();
            store.BankRunCurrency(1000000);
            MetaUpgradeSO upgrade = CreateUpgrade("meta_capped", MetaStatType.MoveSpeed, 2, 10, 1.5f, 2f);

            Assert.IsTrue(MetaShop.TryPurchase(store, upgrade));
            Assert.IsTrue(MetaShop.TryPurchase(store, upgrade));
            int bankedAtCap = store.BankedCurrency;

            Assert.IsFalse(MetaShop.TryPurchase(store, upgrade), "at max rank");
            Assert.AreEqual(2, store.GetUpgradeRank("meta_capped"));
            Assert.AreEqual(bankedAtCap, store.BankedCurrency, "capped purchase must not spend");

            Object.DestroyImmediate(upgrade);
        }

        [Test]
        public void AuthoredUpgradeAssets_AreValidAndUnique()
        {
            var seenIds = new System.Collections.Generic.HashSet<string>();
            var seenStats = new System.Collections.Generic.HashSet<MetaStatType>();

            foreach (string path in MetaUpgradePaths)
            {
                var upgrade = AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>(path);
                Assert.IsNotNull(upgrade, $"Meta upgrade asset missing: {path}");
                Assert.IsFalse(string.IsNullOrEmpty(upgrade.UpgradeId), $"{path} has empty id");
                Assert.IsTrue(seenIds.Add(upgrade.UpgradeId), $"{path} duplicates id {upgrade.UpgradeId}");
                Assert.IsTrue(seenStats.Add(upgrade.StatType), $"{path} duplicates stat {upgrade.StatType}");
                Assert.Greater(upgrade.MaxRank, 0, $"{path} max rank");
                Assert.Greater(upgrade.BaseCost, 0, $"{path} base cost");
                Assert.Greater(upgrade.CostGrowth, 1f, $"{path} cost must escalate");
                Assert.Greater(upgrade.EffectPerRank, 0f, $"{path} effect per rank");
            }
        }
    }
}
