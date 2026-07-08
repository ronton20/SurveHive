using NUnit.Framework;
using SurveHive.Currency;
using SurveHive.Data;
using SurveHive.Enemies;
using SurveHive.Health;
using SurveHive.Persistence;
using UnityEditor;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// Phase 1B working stage difficulty: the tier table resolves per-tier
    /// multipliers (with an identity fallback), those multipliers land on a
    /// real enemy's HP/contact damage and on honey gain, and the selected
    /// tier survives the save round-trip (including v1 migration + clamping).
    /// </summary>
    public sealed class DifficultyTierTests
    {
        [Test]
        public void TierLookup_ReturnsConfiguredRow_AndIdentityFallback()
        {
            DifficultySO table = MakeTable();
            try
            {
                DifficultySO.TierSettings hard = table.GetSettings(DifficultyTier.Hard);
                Assert.AreEqual(1.5f, hard.enemyHealthMultiplier);
                Assert.AreEqual(1.4f, hard.enemyDamageMultiplier);
                Assert.AreEqual(1.15f, hard.spawnRateMultiplier);
                Assert.AreEqual(1.5f, hard.honeyGainMultiplier);

                // A tier missing from the table plays as Normal, not broken.
                DifficultySO.TierSettings missing = table.GetSettings(DifficultyTier.Extreme);
                Assert.AreEqual(1f, missing.enemyHealthMultiplier);
                Assert.AreEqual(1f, missing.enemyDamageMultiplier);
                Assert.AreEqual(1f, missing.honeyGainMultiplier);
            }
            finally
            {
                Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void TierMultipliers_ScaleEnemyHealthAndContactDamage()
        {
            DifficultySO table = MakeTable();
            EnemyStatsSO stats = MakeEnemyStats(maxHealth: 20f, contactDamage: 5f);
            var enemyGo = new GameObject(
                "Enemy", typeof(Rigidbody2D), typeof(CircleCollider2D),
                typeof(HealthComponent), typeof(DamageOnContact), typeof(EnemyController));
            var playerGo = new GameObject("Player");
            try
            {
                // EditMode never runs Awake on AddComponent — kick the two that
                // matter via reflection (SendMessage trips the editor's
                // ShouldRunBehaviour assert) so the controller caches its
                // siblings like it would in play mode.
                InvokeAwake(enemyGo.GetComponent<HealthComponent>());
                InvokeAwake(enemyGo.GetComponent<EnemyController>());

                DifficultySO.TierSettings hard = table.GetSettings(DifficultyTier.Hard);
                var controller = enemyGo.GetComponent<EnemyController>();
                controller.Initialize(
                    stats, playerGo.transform,
                    healthMultiplier: hard.enemyHealthMultiplier,
                    damageMultiplier: hard.enemyDamageMultiplier);

                Assert.AreEqual(30f, enemyGo.GetComponent<HealthComponent>().MaxHealth, 0.001f,
                    "Hard tier scales 20 base HP by 1.5");
                Assert.AreEqual(7f, controller.ScaledContactDamage, 0.001f,
                    "Hard tier scales 5 contact damage by 1.4");
            }
            finally
            {
                Object.DestroyImmediate(enemyGo);
                Object.DestroyImmediate(playerGo);
                Object.DestroyImmediate(stats);
                Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void HoneyGain_AppliesDifficultyMultiplier_StackedWithMeta()
        {
            var walletGo = new GameObject("Wallet", typeof(RunCurrencyWallet));
            try
            {
                var wallet = walletGo.GetComponent<RunCurrencyWallet>();
                wallet.SetDifficultyGainMultiplier(1.5f);
                wallet.AddCurrency(100);
                Assert.AreEqual(150, wallet.TotalCurrency, "difficulty compensation scales honey");

                // Meta-shop gain and difficulty compensation stack multiplicatively.
                wallet.AddGainPercent(10f);
                wallet.AddCurrency(100);
                Assert.AreEqual(315, wallet.TotalCurrency, "100 * 1.1 * 1.5 = 165 on top of 150");
            }
            finally
            {
                Object.DestroyImmediate(walletGo);
            }
        }

        [Test]
        public void Save_RoundTripsSelectedDifficulty()
        {
            var data = new SaveData { selectedDifficulty = (int)DifficultyTier.Extreme };
            SaveData loaded = SaveDataSerializer.FromJson(SaveDataSerializer.ToJson(data));

            Assert.IsNotNull(loaded);
            Assert.AreEqual((int)DifficultyTier.Extreme, loaded.selectedDifficulty);
        }

        [Test]
        public void Save_V1WithoutDifficulty_MigratesToNormal()
        {
            SaveData loaded = SaveDataSerializer.FromJson("{\"version\":1,\"bankedCurrency\":50}");

            Assert.IsNotNull(loaded);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version, "old save stamped to current version");
            Assert.AreEqual((int)DifficultyTier.Normal, loaded.selectedDifficulty);
        }

        [Test]
        public void Save_OutOfRangeDifficulty_IsClampedToValidTiers()
        {
            SaveData high = SaveDataSerializer.FromJson("{\"version\":2,\"selectedDifficulty\":99}");
            Assert.AreEqual((int)DifficultyTier.Extreme, high.selectedDifficulty);

            SaveData low = SaveDataSerializer.FromJson("{\"version\":2,\"selectedDifficulty\":-4}");
            Assert.AreEqual((int)DifficultyTier.Easy, low.selectedDifficulty);
        }

        // Three-row table (Extreme deliberately missing for the fallback test).
        private static DifficultySO MakeTable()
        {
            var table = ScriptableObject.CreateInstance<DifficultySO>();
            var so = new SerializedObject(table);
            SerializedProperty tiers = so.FindProperty("_tiers");
            tiers.arraySize = 3;
            WriteTier(tiers.GetArrayElementAtIndex(0), DifficultyTier.Easy, 0.75f, 0.75f, 1f, 0.75f);
            WriteTier(tiers.GetArrayElementAtIndex(1), DifficultyTier.Normal, 1f, 1f, 1f, 1f);
            WriteTier(tiers.GetArrayElementAtIndex(2), DifficultyTier.Hard, 1.5f, 1.4f, 1.15f, 1.5f);
            so.ApplyModifiedPropertiesWithoutUndo();
            return table;
        }

        private static void WriteTier(
            SerializedProperty row, DifficultyTier tier,
            float health, float damage, float spawnRate, float honey)
        {
            row.FindPropertyRelative("tier").intValue = (int)tier;
            row.FindPropertyRelative("displayName").stringValue = tier.ToString().ToUpperInvariant();
            row.FindPropertyRelative("enemyHealthMultiplier").floatValue = health;
            row.FindPropertyRelative("enemyDamageMultiplier").floatValue = damage;
            row.FindPropertyRelative("spawnRateMultiplier").floatValue = spawnRate;
            row.FindPropertyRelative("honeyGainMultiplier").floatValue = honey;
        }

        private static void InvokeAwake(Component component)
        {
            component.GetType()
                .GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(component, null);
        }

        private static EnemyStatsSO MakeEnemyStats(float maxHealth, float contactDamage)
        {
            var stats = ScriptableObject.CreateInstance<EnemyStatsSO>();
            var so = new SerializedObject(stats);
            so.FindProperty("_maxHealth").floatValue = maxHealth;
            so.FindProperty("_contactDamage").floatValue = contactDamage;
            so.ApplyModifiedPropertiesWithoutUndo();
            return stats;
        }
    }
}
