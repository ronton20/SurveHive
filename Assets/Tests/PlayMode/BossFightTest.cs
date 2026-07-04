using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SurveHive.Core;
using SurveHive.Enemies;
using SurveHive.Stage;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SurveHive.Tests
{
    /// <summary>
    /// End-to-end boss flow: fast-forwards the real stage timeline (private
    /// elapsed clock via reflection — the same path a full run takes) and
    /// asserts the miniboss spawns at 50%, the Queen at 100%, and that killing
    /// the Queen triggers the victory panel and run pause.
    /// </summary>
    public sealed class BossFightTest
    {
        private static readonly FieldInfo ElapsedField = typeof(StageDirector)
            .GetField("_elapsedSeconds", BindingFlags.NonPublic | BindingFlags.Instance);

        [UnityTest]
        public IEnumerator StageTimeline_SpawnsBosses_AndQueenDeathWinsTheRun()
        {
            SceneManager.LoadScene("Beehive");
            yield return null;

            StageDirector director = Object.FindAnyObjectByType<StageDirector>();
            Assert.IsNotNull(director, "StageDirector present");
            float duration = director.Config.TotalDurationSeconds;

            // Let the scene settle a few frames.
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            // Jump past 50%: miniboss event fires on the next director update.
            ElapsedField.SetValue(director, (duration * 0.5f) + 0.5f);
            yield return null;
            yield return null;

            EnemyController miniboss = FindEnemyWithRank(3);
            Assert.IsNotNull(miniboss, "Royal Guard (rank 3) spawned at 50%");
            Assert.IsNotNull(miniboss.GetComponent<ChargeAttack>(), "Miniboss has ChargeAttack");

            // Jump to 100%: flood wave + Queen.
            ElapsedField.SetValue(director, duration + 1f);
            yield return null;
            yield return null;

            EnemyController queen = FindEnemyWithRank(4);
            Assert.IsNotNull(queen, "Queen Bee (rank 4) spawned at 100%");
            Assert.IsNotNull(queen.GetComponent<QueenBossController>(), "Queen has pattern controller");

            // Boss HP bar is tracking her.
            GameObject bossBar = GameObject.Find("BossHealthBar");
            Assert.IsNotNull(bossBar, "BossHealthBar active in scene");
            var barGroup = bossBar.GetComponent<CanvasGroup>();
            Assert.IsNotNull(barGroup);
            Assert.Greater(barGroup.alpha, 0.5f, "Boss health bar visible while Queen lives");

            // Let her patterns tick a moment, then strike her down.
            float elapsed = 0f;
            while (elapsed < 3f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            queen.Health.TakeDamage(1_000_000f, null);
            yield return null;
            yield return null;

            GameObject victoryPanel = GameObject.Find("VictoryPanel");
            Assert.IsNotNull(victoryPanel, "VictoryPanel shown after Queen death");
            Assert.IsTrue(GamePause.IsPaused, "Run paused on victory");
            Assert.Less(barGroup.alpha, 0.5f, "Boss health bar hidden after Queen death");

            // Results block populated (time/kills/level/currency).
            Transform statsTransform = victoryPanel.transform.Find("ResultsStats");
            Assert.IsNotNull(statsTransform, "VictoryPanel has ResultsStats");
            var statsText = statsTransform.GetComponent<TMPro.TMP_Text>();
            Assert.IsNotNull(statsText);
            StringAssert.Contains("Kills", statsText.text, "Results show kill count");
            StringAssert.Contains("Honey banked", statsText.text, "Results show banked currency");

            // Leave global state clean for any test that follows.
            GamePause.SetPaused(false);
        }

        private static EnemyController FindEnemyWithRank(int rank)
        {
            if (EnemyRegistry.Instance == null)
            {
                return null;
            }

            var enemies = EnemyRegistry.Instance.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy != null && enemy.Stats != null && enemy.Stats.Rank == rank)
                {
                    return enemy;
                }
            }

            return null;
        }
    }
}
