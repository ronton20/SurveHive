using NUnit.Framework;
using SurveHive.Data;
using SurveHive.Persistence;
using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// PLAN 5D — achievements: condition thresholds evaluate correctly against
    /// the run-stat snapshot, granting unlocks + pays rewards exactly once
    /// (jelly and cosmetic), and unlocked ids round-trip through the save
    /// schema (v8) with old saves migrating to nothing-unlocked.
    /// </summary>
    public sealed class AchievementTests
    {
        private RuntimeMetaProgressionStoreSO _store;

        [SetUp]
        public void SetUp()
        {
            _store = ScriptableObject.CreateInstance<RuntimeMetaProgressionStoreSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_store);
        }

        private static AchievementSO MakeAchievement(
            string id, AchievementConditionType condition, int threshold,
            int jelly = 0, string cosmeticId = "")
        {
            var achievement = ScriptableObject.CreateInstance<AchievementSO>();
            var so = new UnityEditor.SerializedObject(achievement);
            so.FindProperty("_achievementId").stringValue = id;
            so.FindProperty("_conditionType").intValue = (int)condition;
            so.FindProperty("_threshold").intValue = threshold;
            so.FindProperty("_jellyReward").intValue = jelly;
            so.FindProperty("_cosmeticRewardId").stringValue = cosmeticId;
            so.ApplyModifiedPropertiesWithoutUndo();
            return achievement;
        }

        // ------------------------------------------------------------------
        // Condition evaluation.
        // ------------------------------------------------------------------
        [Test]
        public void Rules_KillsInRun_SatisfiedAtThreshold()
        {
            AchievementSO achievement = MakeAchievement("a", AchievementConditionType.KillsInRun, 250);
            var stats = AchievementRunStats.Empty;

            stats.Kills = 249;
            Assert.That(AchievementRules.IsSatisfied(achievement, in stats), Is.False);
            stats.Kills = 250;
            Assert.That(AchievementRules.IsSatisfied(achievement, in stats), Is.True);

            Object.DestroyImmediate(achievement);
        }

        [Test]
        public void Rules_ReachLevel_SatisfiedAtThreshold()
        {
            AchievementSO achievement = MakeAchievement("a", AchievementConditionType.ReachLevel, 10);
            var stats = AchievementRunStats.Empty;

            stats.Level = 9;
            Assert.That(AchievementRules.IsSatisfied(achievement, in stats), Is.False);
            stats.Level = 10;
            Assert.That(AchievementRules.IsSatisfied(achievement, in stats), Is.True);

            Object.DestroyImmediate(achievement);
        }

        [Test]
        public void Rules_SurviveSeconds_SatisfiedAtThreshold()
        {
            AchievementSO achievement = MakeAchievement("a", AchievementConditionType.SurviveSeconds, 300);
            var stats = AchievementRunStats.Empty;

            stats.SurvivedSeconds = 299.5f;
            Assert.That(AchievementRules.IsSatisfied(achievement, in stats), Is.False);
            stats.SurvivedSeconds = 300f;
            Assert.That(AchievementRules.IsSatisfied(achievement, in stats), Is.True);

            Object.DestroyImmediate(achievement);
        }

        [Test]
        public void Rules_SetTierActive_UsesOneBasedTier()
        {
            AchievementSO achievement = MakeAchievement("a", AchievementConditionType.SetTierActive, 3);
            var stats = AchievementRunStats.Empty;

            stats.MaxSetTier = 2;
            Assert.That(AchievementRules.IsSatisfied(achievement, in stats), Is.False);
            stats.MaxSetTier = 3;
            Assert.That(AchievementRules.IsSatisfied(achievement, in stats), Is.True);

            Object.DestroyImmediate(achievement);
        }

        [Test]
        public void Rules_ClearStage_UnresolvedRunNeverSatisfies()
        {
            // Threshold Easy (0): a run without a victory (-1) must not pass.
            AchievementSO achievement = MakeAchievement(
                "a", AchievementConditionType.ClearStage, (int)DifficultyTier.Easy);
            var stats = AchievementRunStats.Empty;

            Assert.That(AchievementRules.IsSatisfied(achievement, in stats), Is.False);
            stats.ClearedDifficulty = (int)DifficultyTier.Easy;
            Assert.That(AchievementRules.IsSatisfied(achievement, in stats), Is.True);

            Object.DestroyImmediate(achievement);
        }

        [Test]
        public void Rules_ClearStage_HigherDifficultyCountsForLowerThreshold()
        {
            AchievementSO achievement = MakeAchievement(
                "a", AchievementConditionType.ClearStage, (int)DifficultyTier.Hard);
            var stats = AchievementRunStats.Empty;

            stats.ClearedDifficulty = (int)DifficultyTier.Normal;
            Assert.That(AchievementRules.IsSatisfied(achievement, in stats), Is.False);
            stats.ClearedDifficulty = (int)DifficultyTier.Extreme;
            Assert.That(AchievementRules.IsSatisfied(achievement, in stats), Is.True);

            Object.DestroyImmediate(achievement);
        }

        // ------------------------------------------------------------------
        // Granting: unlock + rewards, exactly once.
        // ------------------------------------------------------------------
        [Test]
        public void TryGrant_UnlocksAndPaysJelly()
        {
            AchievementSO achievement = MakeAchievement(
                "first_sting", AchievementConditionType.KillsInRun, 1, jelly: 5);

            Assert.That(AchievementRules.TryGrant(_store, achievement), Is.True);
            Assert.That(_store.IsAchievementUnlocked("first_sting"), Is.True);
            Assert.That(_store.BankedJelly, Is.EqualTo(5));

            Object.DestroyImmediate(achievement);
        }

        [Test]
        public void TryGrant_SecondGrantRejectedAndPaysNothing()
        {
            AchievementSO achievement = MakeAchievement(
                "first_sting", AchievementConditionType.KillsInRun, 1, jelly: 5);

            AchievementRules.TryGrant(_store, achievement);
            Assert.That(AchievementRules.TryGrant(_store, achievement), Is.False);
            Assert.That(_store.BankedJelly, Is.EqualTo(5), "reward must not pay twice");

            Object.DestroyImmediate(achievement);
        }

        [Test]
        public void TryGrant_UnlocksCosmeticReward()
        {
            AchievementSO achievement = MakeAchievement(
                "apex", AchievementConditionType.ClearStage, (int)DifficultyTier.Extreme,
                jelly: 15, cosmeticId: "hat_crown");

            Assert.That(AchievementRules.TryGrant(_store, achievement), Is.True);
            Assert.That(_store.IsCosmeticOwned("hat_crown"), Is.True);

            Object.DestroyImmediate(achievement);
        }

        [Test]
        public void TryGrant_RejectsMissingStoreOrId()
        {
            AchievementSO blankId = MakeAchievement("", AchievementConditionType.KillsInRun, 1);

            Assert.That(AchievementRules.TryGrant(null, blankId), Is.False);
            Assert.That(AchievementRules.TryGrant(_store, null), Is.False);
            Assert.That(AchievementRules.TryGrant(_store, blankId), Is.False);

            Object.DestroyImmediate(blankId);
        }

        // ------------------------------------------------------------------
        // Persistence: state + save round-trip and migration.
        // ------------------------------------------------------------------
        [Test]
        public void State_RoundTripsUnlockedAchievements()
        {
            var state = new MetaProgressionState();
            Assert.That(state.UnlockAchievement("first_sting"), Is.True);
            Assert.That(state.UnlockAchievement("first_sting"), Is.False, "double unlock reports not-new");
            state.UnlockAchievement("queenslayer");

            var data = new SaveData();
            state.WriteTo(data);
            SaveData reloaded = SaveDataSerializer.FromJson(SaveDataSerializer.ToJson(data));

            var restored = new MetaProgressionState();
            restored.LoadFrom(reloaded);
            Assert.That(restored.IsAchievementUnlocked("first_sting"), Is.True);
            Assert.That(restored.IsAchievementUnlocked("queenslayer"), Is.True);
            Assert.That(restored.IsAchievementUnlocked("apex_of_the_hive"), Is.False);
        }

        [Test]
        public void Save_V7MigratesToNothingUnlocked()
        {
            SaveData loaded = SaveDataSerializer.FromJson(
                "{\"version\":7,\"bankedJelly\":12,\"ownedCosmeticIds\":[\"hat_crown\"]}");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(loaded.unlockedAchievementIds, Is.Empty);

            var state = new MetaProgressionState();
            state.LoadFrom(loaded);
            Assert.That(state.IsAchievementUnlocked("first_sting"), Is.False);
            Assert.That(state.IsCosmeticOwned("hat_crown"), Is.True, "older blocks still load");
        }
    }
}
