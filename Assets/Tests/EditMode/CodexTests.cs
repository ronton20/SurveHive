using NUnit.Framework;
using SurveHive.Data;
using SurveHive.Persistence;
using SurveHive.Pickups;
using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// PLAN 5A codex foundation: entry-id formatting, unlock bookkeeping in
    /// <see cref="MetaProgressionState"/>, its save round-trip, and the
    /// store-level batch unlock.
    /// </summary>
    public sealed class CodexTests
    {
        [Test]
        public void CodexIds_FormatByCategory()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyStatsSO>();
            enemy.name = "WorkerBee";

            Assert.AreEqual("enemy:WorkerBee", CodexIds.ForEnemy(enemy));
            Assert.AreEqual("set:Fire", CodexIds.ForSet(SkillElement.Fire));
            Assert.AreEqual("set:Honey", CodexIds.ForSet(SkillElement.Honey));
            Assert.AreEqual("item:HoneyJar", CodexIds.ForItem(ItemDropType.HoneyJar));
            Assert.AreEqual("item:RoyalBomb", CodexIds.ForItem(ItemDropType.RoyalBomb));

            Object.DestroyImmediate(enemy);
        }

        [Test]
        public void CodexIds_NullOrIdlessInputs_ReturnNull()
        {
            var idlessSkill = ScriptableObject.CreateInstance<SkillDefinitionSO>();

            Assert.IsNull(CodexIds.ForSkill(null));
            Assert.IsNull(CodexIds.ForSkill(idlessSkill));
            Assert.IsNull(CodexIds.ForEnemy(null));
            Assert.IsNull(CodexIds.ForItem((ItemDropType)99));

            Object.DestroyImmediate(idlessSkill);
        }

        [Test]
        public void State_UnlockCodexEntry_TracksAndDeduplicates()
        {
            var state = new MetaProgressionState();

            Assert.IsFalse(state.IsCodexUnlocked("enemy:WorkerBee"));
            Assert.IsTrue(state.UnlockCodexEntry("enemy:WorkerBee"));
            Assert.IsFalse(state.UnlockCodexEntry("enemy:WorkerBee"), "second unlock is a no-op");
            Assert.IsFalse(state.UnlockCodexEntry(null));
            Assert.IsFalse(state.UnlockCodexEntry(""));
            Assert.IsTrue(state.IsCodexUnlocked("enemy:WorkerBee"));
            Assert.AreEqual(1, state.CodexUnlockCount);
        }

        [Test]
        public void State_CodexUnlocks_RoundTripThroughSaveData()
        {
            var state = new MetaProgressionState();
            state.UnlockCodexEntry("skill:swift_wings");
            state.UnlockCodexEntry("set:Frost");
            state.UnlockCodexEntry("item:Magnet");

            var data = new SaveData();
            state.WriteTo(data);

            var reloaded = new MetaProgressionState();
            reloaded.LoadFrom(data);

            Assert.AreEqual(3, reloaded.CodexUnlockCount);
            Assert.IsTrue(reloaded.IsCodexUnlocked("skill:swift_wings"));
            Assert.IsTrue(reloaded.IsCodexUnlocked("set:Frost"));
            Assert.IsTrue(reloaded.IsCodexUnlocked("item:Magnet"));
            Assert.IsFalse(reloaded.IsCodexUnlocked("item:HoneyJar"));
        }

        [Test]
        public void RuntimeStore_UnlockCodexEntries_BatchesAndQueries()
        {
            var store = ScriptableObject.CreateInstance<RuntimeMetaProgressionStoreSO>();
            var batch = new System.Collections.Generic.List<string>
            {
                "enemy:QueenBee", "item:WaxShield", null,
            };

            Assert.IsFalse(store.IsCodexUnlocked("enemy:QueenBee"));
            store.UnlockCodexEntries(batch);
            store.UnlockCodexEntries(null);

            Assert.IsTrue(store.IsCodexUnlocked("enemy:QueenBee"));
            Assert.IsTrue(store.IsCodexUnlocked("item:WaxShield"));
            Assert.IsFalse(store.IsCodexUnlocked("enemy:WorkerBee"));
            Assert.IsFalse(store.IsCodexUnlocked(null));

            Object.DestroyImmediate(store);
        }
    }
}
