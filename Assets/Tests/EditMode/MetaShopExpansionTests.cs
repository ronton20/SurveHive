using NUnit.Framework;
using SurveHive.Data;
using SurveHive.Health;
using SurveHive.Pickups;
using SurveHive.Player;
using SurveHive.Progression;
using UnityEditor;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// Phase 1C meta shop expansion: reroll semantics (stock from rank,
    /// replace exactly one, never duplicate a shown card, keep the charge when
    /// the pool is empty) and each new stat landing on the player at run start
    /// through the real MetaUpgradeApplier path.
    /// </summary>
    public sealed class MetaShopExpansionTests
    {
        // ------------------------------------------------------------------
        // Reroll logic.
        // ------------------------------------------------------------------
        [Test]
        public void RerollStock_IsBoughtRank_CappedAtMaxPerRun()
        {
            Assert.AreEqual(0, RerollLogic.StockFromRank(0, 3));
            Assert.AreEqual(2, RerollLogic.StockFromRank(2, 3));
            Assert.AreEqual(3, RerollLogic.StockFromRank(3, 3));
            Assert.AreEqual(3, RerollLogic.StockFromRank(99, 3), "stock hard-caps at 3/run");
            Assert.AreEqual(0, RerollLogic.StockFromRank(-1, 3));
        }

        [Test]
        public void Reroll_NeverPicksAShownCard()
        {
            // Eligible pool 0..4; cards 0,1,2 are on screen — every pick over
            // many rolls must come from {3, 4}.
            var rng = new System.Random(1234);
            var resultScratch = new int[1];
            var weightScratch = new float[5];
            int[] shown = { 0, 1, 2 };

            for (int attempt = 0; attempt < 200; attempt++)
            {
                int[] eligible = { 0, 1, 2, 3, 4 };
                float[] weights = { 1f, 1f, 1f, 1f, 1f };
                int picked = RerollLogic.PickReplacement(
                    eligible, weights, eligible.Length, shown, shown.Length,
                    resultScratch, weightScratch, rng);
                Assert.IsTrue(picked == 3 || picked == 4, $"picked {picked}, a card already shown");
            }
        }

        [Test]
        public void Reroll_WithNoOtherEligibleSkill_ReturnsMinusOne()
        {
            var rng = new System.Random(5);
            var resultScratch = new int[1];
            var weightScratch = new float[3];
            int[] eligible = { 0, 1, 2 };
            float[] weights = { 1f, 1f, 1f };
            int[] shown = { 0, 1, 2 };

            int picked = RerollLogic.PickReplacement(
                eligible, weights, eligible.Length, shown, shown.Length,
                resultScratch, weightScratch, rng);
            Assert.AreEqual(-1, picked, "nothing off-screen to offer");
        }

        [Test]
        public void Reroll_ReplacesExactlyOneCard()
        {
            var rng = new System.Random(42);
            var resultScratch = new int[1];
            var weightScratch = new float[4];
            int[] eligible = { 10, 11, 12, 13 };
            float[] weights = { 1f, 1f, 1f, 1f };
            int[] shown = { 10, 11, 12 };

            int picked = RerollLogic.PickReplacement(
                eligible, weights, eligible.Length, shown, shown.Length,
                resultScratch, weightScratch, rng);
            Assert.AreEqual(13, picked);

            // The other two cards are untouched — replacing is the caller
            // binding `picked` into ONE slot; shown stays caller-owned.
            Assert.AreEqual(10, shown[0]);
            Assert.AreEqual(11, shown[1]);
        }

        // ------------------------------------------------------------------
        // New stats land at run start via the real applier.
        // ------------------------------------------------------------------
        [Test]
        public void NewMetaStats_ApplyToPlayerAtRunStart()
        {
            var store = ScriptableObject.CreateInstance<RuntimeMetaProgressionStoreSO>();
            var upgrades = new[]
            {
                MakeUpgrade("meta_exp_gain", MetaStatType.ExpGain, effectPerRank: 5f, rank: 2, store),
                MakeUpgrade("meta_ability_power", MetaStatType.AbilityPower, effectPerRank: 4f, rank: 5, store),
                MakeUpgrade("meta_cooldown", MetaStatType.CooldownReduction, effectPerRank: 3f, rank: 2, store),
                MakeUpgrade("meta_crit_chance", MetaStatType.CritChance, effectPerRank: 2f, rank: 20, store),
                MakeUpgrade("meta_crit_damage", MetaStatType.CritDamage, effectPerRank: 5f, rank: 4, store),
                MakeUpgrade("meta_item_drop", MetaStatType.ItemDropRate, effectPerRank: 10f, rank: 5, store),
            };

            var playerGo = new GameObject("Player", typeof(PlayerStats), typeof(HealthComponent),
                typeof(PlayerExperience), typeof(MetaUpgradeApplier));
            try
            {
                var applier = playerGo.GetComponent<MetaUpgradeApplier>();
                var stats = playerGo.GetComponent<PlayerStats>();
                var experience = playerGo.GetComponent<PlayerExperience>();

                var so = new SerializedObject(applier);
                so.FindProperty("_store").objectReferenceValue = store;
                so.FindProperty("_stats").objectReferenceValue = stats;
                so.FindProperty("_health").objectReferenceValue = playerGo.GetComponent<HealthComponent>();
                so.FindProperty("_experience").objectReferenceValue = experience;
                SerializedProperty upgradesProp = so.FindProperty("_upgrades");
                upgradesProp.arraySize = upgrades.Length;
                for (int i = 0; i < upgrades.Length; i++)
                {
                    upgradesProp.GetArrayElementAtIndex(i).objectReferenceValue = upgrades[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                InvokeAwake(applier);

                Assert.AreEqual(1.1f, experience.GainMultiplier, 0.001f, "EXP gain +10% (2 ranks x 5)");
                Assert.AreEqual(1.2f, stats.AbilityPowerMultiplier, 0.001f, "ability power +20% (5 ranks x 4)");
                Assert.AreEqual(0.94f, stats.ActiveCooldownMultiplier, 0.001f, "cooldowns cut 6% (2 ranks x 3)");
                Assert.AreEqual(40f, stats.CritChancePercent, 0.001f, "crit chance maxes at +40% on the 0% base");
                Assert.AreEqual(1.7f, stats.CritDamageMultiplier, 0.001f, "crit damage 1.5 + 20% (4 ranks x 5)");
                Assert.AreEqual(1.5f, ItemDrops.DropChanceMultiplier, 0.001f, "drop rolls x1.5 (5 ranks x 10)");
            }
            finally
            {
                Object.DestroyImmediate(playerGo);
                foreach (MetaUpgradeSO upgrade in upgrades)
                {
                    Object.DestroyImmediate(upgrade);
                }

                Object.DestroyImmediate(store);
                ItemDrops.SetDropChanceMultiplier(1f);
            }
        }

        [Test]
        public void ApplierAwake_ResetsDropRateStatic_FromPreviousRun()
        {
            ItemDrops.SetDropChanceMultiplier(2.5f);

            var playerGo = new GameObject("Player", typeof(PlayerStats), typeof(HealthComponent),
                typeof(MetaUpgradeApplier));
            try
            {
                // No store/upgrades wired: Awake must still reset the static.
                InvokeAwake(playerGo.GetComponent<MetaUpgradeApplier>());
                Assert.AreEqual(1f, ItemDrops.DropChanceMultiplier, 0.001f,
                    "a fresh run never inherits the previous run's drop-rate rank");
            }
            finally
            {
                Object.DestroyImmediate(playerGo);
                ItemDrops.SetDropChanceMultiplier(1f);
            }
        }

        private static MetaUpgradeSO MakeUpgrade(
            string id, MetaStatType statType, float effectPerRank, int rank, RuntimeMetaProgressionStoreSO store)
        {
            var upgrade = ScriptableObject.CreateInstance<MetaUpgradeSO>();
            var so = new SerializedObject(upgrade);
            so.FindProperty("_upgradeId").stringValue = id;
            so.FindProperty("_statType").intValue = (int)statType;
            so.FindProperty("_maxRank").intValue = 20;
            so.FindProperty("_effectPerRank").floatValue = effectPerRank;
            so.ApplyModifiedPropertiesWithoutUndo();
            store.SetUpgradeRank(id, rank);
            return upgrade;
        }

        private static void InvokeAwake(Component component)
        {
            component.GetType()
                .GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(component, null);
        }
    }
}
