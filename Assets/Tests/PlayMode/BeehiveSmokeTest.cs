using System.Collections;
using NUnit.Framework;
using SurveHive.Core;
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
            float elapsed = 0f;
            while (elapsed < 8f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsNotNull(EnemyRegistry.Instance, "EnemyRegistry singleton is up");
            Assert.Greater(EnemyRegistry.Instance.ActiveCount, 0, "Enemies have spawned");

            GameObject playerBody = GameObject.Find("Player/Body");
            Assert.IsNotNull(playerBody, "Player bee rig Body exists");
            var renderer = playerBody.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer.sprite, "Player Body has a sprite resolved");
        }
    }
}
