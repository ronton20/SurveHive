using System.Collections;
using NUnit.Framework;
using SurveHive.Combat.Skills;
using SurveHive.Combat.Status;
using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Enemies;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SurveHive.Tests
{
    /// <summary>
    /// End-to-end smoke test: boots the Beehive scene and lets it run for a few
    /// seconds of game time. Any unhandled exception or error log fails the test,
    /// so it catches broken wiring (animator params, pools, TMP, rig setup) that
    /// static scene validation can't see.
    /// </summary>
    public sealed class BeehiveSmokeTest
    {
        // Phase 4: run end banks to the persistent save — redirect it to a temp
        // file so tests never touch the developer's real save.
        [SetUp]
        public void RedirectSaveFile()
        {
            string path = System.IO.Path.Combine(Application.temporaryCachePath, "smoke_test_save.json");
            // Start from a clean save every run — a leftover file from a previous
            // test session would pre-unlock codex entries and mask discoveries.
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            Persistence.SaveFileStore.SetPathOverride(path);
        }

        [TearDown]
        public void RestoreSaveFile()
        {
            Persistence.SaveFileStore.SetPathOverride(null);
        }

        [UnityTest]
        public IEnumerator BeehiveScene_RunsWithoutErrors_AndSpawnsEnemies()
        {
            SceneManager.LoadScene("Beehive");
            yield return null;

            GameObject player = GameObject.FindWithTag("Player");
            Assert.IsNotNull(player, "Player exists after scene load");
            Assert.IsNotNull(RunSession.Instance, "RunSession singleton is up");

            // GameObject.Find only sees active objects: the level-up panel must be
            // active (its controller subscribes in OnEnable; CanvasGroup hides it).
            Assert.IsNotNull(GameObject.Find("LevelUpPanel"), "LevelUpPanel is active at boot");

            // ~8 seconds of simulated gameplay: enemies spawn, chase, and the
            // player auto-attacks (spawn radius ~11, approach ~2.2u/s).
            yield return RunGameSeconds(8f);

            Assert.IsNotNull(EnemyRegistry.Instance, "EnemyRegistry singleton is up");
            Assert.Greater(EnemyRegistry.Instance.ActiveCount, 0, "Enemies have spawned");

            GameObject playerBody = GameObject.Find("Player/Body");
            Assert.IsNotNull(playerBody, "Player bee rig Body exists");
            var renderer = playerBody.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer.sprite, "Player Body has a sprite resolved");

            // --- Phase 2: active skills + status effects live in the scene ---
            var skillManager = player.GetComponent<ActiveSkillManager>();
            Assert.IsNotNull(skillManager, "Player has ActiveSkillManager");

#if UNITY_EDITOR
            // Equip a spread of behaviors directly (radial volley, aura, chain)
            // and let them fire for a few seconds — any wiring error (pools,
            // prefabs, status application) surfaces as an error log and fails.
            var barrage = UnityEditor.AssetDatabase.LoadAssetAtPath<ActiveSkillSO>(
                "Assets/Data/Skills/Actives/StingerBarrage.asset");
            var pollen = UnityEditor.AssetDatabase.LoadAssetAtPath<ActiveSkillSO>(
                "Assets/Data/Skills/Actives/PollenCloud.asset");
            var staticWings = UnityEditor.AssetDatabase.LoadAssetAtPath<ActiveSkillSO>(
                "Assets/Data/Skills/Actives/StaticWings.asset");
            Assert.IsNotNull(barrage, "StingerBarrage asset exists");
            Assert.IsNotNull(pollen, "PollenCloud asset exists");
            Assert.IsNotNull(staticWings, "StaticWings asset exists");

            skillManager.AddOrLevelUp(barrage);
            skillManager.AddOrLevelUp(pollen);
            skillManager.AddOrLevelUp(staticWings);
            Assert.AreEqual(3, skillManager.EquippedCount, "Three active skills equipped");
            Assert.AreEqual(1, skillManager.GetLevel(barrage), "Barrage at level 1");
            skillManager.AddOrLevelUp(barrage);
            Assert.AreEqual(2, skillManager.GetLevel(barrage), "Barrage leveled to 2");
#endif

            // Status effect applied to a live enemy takes hold and slows it.
            Assert.Greater(EnemyRegistry.Instance.ActiveCount, 0);
            EnemyController enemy = EnemyRegistry.Instance.ActiveEnemies[0];
            Assert.IsNotNull(enemy.StatusReceiver, "Enemy has StatusEffectReceiver");
            enemy.StatusReceiver.ApplyEffect(StatusEffectType.Slow, 0.4f, 2f);
            Assert.Less(enemy.StatusReceiver.MoveSpeedMultiplier, 1f, "Slow reduces move speed");

            // Let the skills fire and the slow tick out with zero errors.
            yield return RunGameSeconds(4f);

            // --- Phase 5A: the run's encounters queue codex discoveries and
            // flushing persists them to the (redirected, fresh) save. ---
            var codex = Progression.CodexTracker.Instance;
            Assert.IsNotNull(codex, "CodexTracker is up");
            Assert.Greater(codex.PendingUnlockCount, 0, "Enemy spawns queued codex discoveries");
            codex.FlushPending();
            Assert.AreEqual(0, codex.PendingUnlockCount, "Flush drains the pending queue");

            Persistence.SaveData save = Persistence.SaveFileStore.Load();
            Assert.IsNotNull(save, "Save written by codex flush");
            Assert.Greater(save.codexIds.Length, 0, "Codex unlocks persisted to the save");
        }

        // Advances scaled game time, clicking through any level-up offer that
        // pauses the run (skills kill fast enough to trigger them mid-test) —
        // which also exercises the rarity-card selection path. An unscaled-time
        // cap guards against hanging at timeScale 0.
        private static IEnumerator RunGameSeconds(float seconds)
        {
            float elapsed = 0f;
            float unscaledElapsed = 0f;
            while (elapsed < seconds && unscaledElapsed < seconds + 30f)
            {
                if (GamePause.IsPaused)
                {
                    ClickFirstLevelUpChoice();
                }
                else
                {
                    elapsed += Time.deltaTime;
                }

                unscaledElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsFalse(GamePause.IsPaused, "Run resumed after any level-up offers");
        }

        private static void ClickFirstLevelUpChoice()
        {
            GameObject panel = GameObject.Find("LevelUpPanel");
            if (panel == null)
            {
                return;
            }

            var buttons = panel.GetComponentsInChildren<UnityEngine.UI.Button>(false);
            if (buttons.Length > 0)
            {
                buttons[0].onClick.Invoke();
            }
        }
    }
}
