using NUnit.Framework;
using SurveHive.Data;
using SurveHive.Persistence;
using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// Difficulty unlock gating (1B follow-up): Easy/Normal always open, Hard
    /// opens on a Normal clear, Extreme needs a Hard clear plus the next
    /// stage's Normal clear; stage-clear records round-trip in the v3 save.
    /// </summary>
    public sealed class DifficultyUnlockTests
    {
        [Test]
        public void TiersWithoutRequirements_AreAlwaysUnlocked()
        {
            var store = ScriptableObject.CreateInstance<RuntimeMetaProgressionStoreSO>();
            try
            {
                var easy = new DifficultySO.TierSettings { tier = DifficultyTier.Easy };
                Assert.IsTrue(DifficultyUnlocks.IsUnlocked(easy, store));
                Assert.IsTrue(DifficultyUnlocks.IsUnlocked(easy, null), "no store still opens ungated tiers");
            }
            finally
            {
                Object.DestroyImmediate(store);
            }
        }

        [Test]
        public void Hard_UnlocksAfterNormalClear()
        {
            var store = ScriptableObject.CreateInstance<RuntimeMetaProgressionStoreSO>();
            try
            {
                DifficultySO.TierSettings hard = MakeHardTier();

                Assert.IsFalse(DifficultyUnlocks.IsUnlocked(hard, store), "locked before any clear");

                store.RecordStageClear("Beehive", (int)DifficultyTier.Easy);
                Assert.IsFalse(DifficultyUnlocks.IsUnlocked(hard, store), "an Easy clear doesn't count");

                store.RecordStageClear("Beehive", (int)DifficultyTier.Normal);
                Assert.IsTrue(DifficultyUnlocks.IsUnlocked(hard, store), "Normal clear opens Hard");
            }
            finally
            {
                Object.DestroyImmediate(store);
            }
        }

        [Test]
        public void Extreme_NeedsHardClearAndNextStageNormalClear()
        {
            var store = ScriptableObject.CreateInstance<RuntimeMetaProgressionStoreSO>();
            try
            {
                DifficultySO.TierSettings extreme = MakeExtremeTier();

                store.RecordStageClear("Beehive", (int)DifficultyTier.Hard);
                Assert.IsFalse(DifficultyUnlocks.IsUnlocked(extreme, store),
                    "Hard clear alone isn't enough — the Garden gate is still open");

                store.RecordStageClear("Garden", (int)DifficultyTier.Normal);
                Assert.IsTrue(DifficultyUnlocks.IsUnlocked(extreme, store), "both tasks met");
            }
            finally
            {
                Object.DestroyImmediate(store);
            }
        }

        [Test]
        public void StageClears_RoundTripInSave()
        {
            var state = new MetaProgressionState();
            state.RecordStageClear("Beehive", (int)DifficultyTier.Normal);
            state.RecordStageClear("Beehive", (int)DifficultyTier.Hard);
            state.RecordStageClear("Garden", (int)DifficultyTier.Easy);

            var data = new SaveData();
            state.WriteTo(data);
            SaveData loaded = SaveDataSerializer.FromJson(SaveDataSerializer.ToJson(data));
            var restored = new MetaProgressionState();
            restored.LoadFrom(loaded);

            Assert.IsTrue(restored.HasStageClear("Beehive", (int)DifficultyTier.Normal));
            Assert.IsTrue(restored.HasStageClear("Beehive", (int)DifficultyTier.Hard));
            Assert.IsFalse(restored.HasStageClear("Beehive", (int)DifficultyTier.Extreme));
            Assert.IsTrue(restored.HasStageClear("Garden", (int)DifficultyTier.Easy));
            Assert.IsFalse(restored.HasStageClear("Garden", (int)DifficultyTier.Normal));
        }

        [Test]
        public void V2Save_MigratesToEmptyClearRecord()
        {
            SaveData loaded = SaveDataSerializer.FromJson("{\"version\":2,\"bankedCurrency\":10}");

            Assert.IsNotNull(loaded);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual(0, loaded.stageClearIds.Length);
            Assert.AreEqual(0, loaded.stageClearMasks.Length);
        }

        [Test]
        public void MismatchedClearArrays_AreTrimmedTogether()
        {
            SaveData loaded = SaveDataSerializer.FromJson(
                "{\"version\":3,\"stageClearIds\":[\"Beehive\",\"Garden\"],\"stageClearMasks\":[2]}");

            Assert.AreEqual(1, loaded.stageClearIds.Length);
            Assert.AreEqual(1, loaded.stageClearMasks.Length);

            var state = new MetaProgressionState();
            state.LoadFrom(loaded);
            Assert.IsTrue(state.HasStageClear("Beehive", (int)DifficultyTier.Normal));
        }

        private static DifficultySO.TierSettings MakeHardTier()
        {
            return new DifficultySO.TierSettings
            {
                tier = DifficultyTier.Hard,
                unlockRequirements = new[]
                {
                    new DifficultySO.UnlockRequirement
                    {
                        stageId = "Beehive", stageName = "The Beehive", clearTier = DifficultyTier.Normal,
                    },
                },
            };
        }

        private static DifficultySO.TierSettings MakeExtremeTier()
        {
            return new DifficultySO.TierSettings
            {
                tier = DifficultyTier.Extreme,
                unlockRequirements = new[]
                {
                    new DifficultySO.UnlockRequirement
                    {
                        stageId = "Beehive", stageName = "The Beehive", clearTier = DifficultyTier.Hard,
                    },
                    new DifficultySO.UnlockRequirement
                    {
                        stageId = "Garden", stageName = "the Garden", clearTier = DifficultyTier.Normal,
                    },
                },
            };
        }
    }
}
