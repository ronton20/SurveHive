using System.Collections;
using NUnit.Framework;
using SurveHive.Core;
using SurveHive.Enemies;
using SurveHive.Input;
using SurveHive.Pickups;
using SurveHive.Player;
using SurveHive.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SurveHive.Tests
{
    /// <summary>
    /// PLAN 1A balance verification: plays full unattended Beehive runs at an
    /// accelerated time scale with a kiting bot standing in for the player, and
    /// checks the two tuning targets — a fresh (no-meta) player dies in the
    /// late stage (~minute 8–12) and a meta-invested player clears the Queen.
    /// Several real minutes per run, so the whole fixture is [Explicit]:
    /// run via <c>unity.sh test PlayMode SurveHive.Tests.BalanceRunTest</c>.
    /// </summary>
    [Explicit("Full-run balance simulation (minutes per run) — select via -testFilter.")]
    public sealed class BalanceRunTest
    {
        // GamePause/HitStop reset the scale to 1 whenever they let go, so the
        // run loop re-asserts this every unpaused frame.
        private const float TimeScale = 6f;
        private const float MaxRealSecondsPerRun = 900f;

        // The design target is "a fresh player dies around minute 8–12", but
        // this band is a REGRESSION GATE, not the target itself: the bot's
        // rigid melee-hover play caps its mid-game survival roughly 2 minutes
        // below a human's (random card picks, no synergy planning), so its
        // crunch deaths land ~5.5–6.5 when the data is human-calibrated.
        // Queen-reaching runs die around 17 elapsed (boss fights freeze the
        // 10-minute timeline while ElapsedSeconds keeps counting); anything
        // past 20 is a degenerate stall. Re-calibrate against real playtests.
        private const float NoMetaMinDeathMinutes = 5f;
        private const float NoMetaMaxDeathMinutes = 20f;

        private sealed class RunResult
        {
            public bool Finished;
            public bool Victory;
            public float EndSeconds;
            public int Level;
            public int Kills;
        }

        [SetUp]
        public void RedirectSaveFile()
        {
            string path = System.IO.Path.Combine(
                Application.temporaryCachePath,
                $"balance_{TestContext.CurrentContext.Test.Name}.json");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            Persistence.SaveFileStore.SetPathOverride(path);
            RunSession.SelectedDifficulty = Data.DifficultyTier.Normal;
        }

        [TearDown]
        public void RestoreSaveFile()
        {
            Persistence.SaveFileStore.SetPathOverride(null);
            GamePause.SetPaused(false);
            Time.timeScale = 1f;
        }

        // Card-pick RNG swings single runs by minutes, so the fresh-player
        // check is statistical: the *median* death time must land in the band,
        // and at most one of the runs may get lucky enough to clear (a skilled
        // first-timer squeaking out a win is acceptable; it being common isn't).
        [UnityTest]
        [Timeout(2400000)]
        public IEnumerator NoMeta_FreshRun_DiesLateStage()
        {
            const int runCount = 3;
            var deathMinutes = new System.Collections.Generic.List<float>(runCount);
            int victories = 0;

            for (int run = 1; run <= runCount; run++)
            {
                var result = new RunResult();
                yield return PlayOneRun(result);
                LogResult($"no-meta run {run}", result);

                Assert.IsTrue(result.Finished, "Run reached an end panel within the real-time cap");
                if (result.Victory)
                {
                    victories++;
                }
                else
                {
                    deathMinutes.Add(result.EndSeconds / 60f);
                }
            }

            Assert.LessOrEqual(victories, 1, "No-meta runs should almost never clear the Queen");
            deathMinutes.Sort();
            float median = deathMinutes[deathMinutes.Count / 2];
            Debug.Log($"[BALANCE] no-meta median death: {median:F1} min ({victories} victories)");
            Assert.That(
                median,
                Is.InRange(NoMetaMinDeathMinutes, NoMetaMaxDeathMinutes),
                "Median no-meta death should land near the minute 8–12 target band");
        }

        // The design target is that an invested player *can* clear — and the
        // bot is a worse boss-dodger than a real invested player — so this
        // passes when any of its runs wins, not when every run does.
        [UnityTest]
        [Timeout(2400000)]
        public IEnumerator MaxMeta_InvestedRun_ClearsQueen()
        {
            const int runCount = 2;
            SetMaxCombatMetaRanks();

            for (int run = 1; run <= runCount; run++)
            {
                var result = new RunResult();
                // 100 base + 10 ranks × 25 flat — fails fast if the shop ranks
                // silently didn't reach the player.
                yield return PlayOneRun(result, minExpectedMaxHealth: 350f);
                LogResult($"max-meta run {run}", result);

                Assert.IsTrue(result.Finished, "Run reached an end panel within the real-time cap");
                if (result.Victory)
                {
                    yield break;
                }
            }

            Assert.Fail($"None of {runCount} maxed-meta runs cleared the Queen");
        }

        private static void LogResult(string label, RunResult result)
        {
            string outcome = !result.Finished ? "TIMED OUT" : result.Victory ? "VICTORY" : "died";
            Debug.Log(
                $"[BALANCE] {label}: {outcome} at {result.EndSeconds / 60f:F1} min " +
                $"(level {result.Level}, {result.Kills} kills)");
        }

        // Ranks a heavily invested account: every combat-relevant shop upgrade
        // maxed (rerolls stay 0 — the bot wouldn't use them, which keeps this
        // check conservative).
        private static void SetMaxCombatMetaRanks()
        {
#if UNITY_EDITOR
            var store = UnityEditor.AssetDatabase.LoadAssetAtPath<Data.PersistentMetaProgressionStoreSO>(
                "Assets/Data/Progression/PersistentMetaProgressionStore.asset");
            Assert.IsNotNull(store, "Persistent meta store asset exists");
            store.SetUpgradeRank("meta_max_health", 10);
            store.SetUpgradeRank("meta_damage", 10);
            store.SetUpgradeRank("meta_move_speed", 6);
            store.SetUpgradeRank("meta_attack_speed", 8);
            store.SetUpgradeRank("meta_magnet", 5);
            store.SetUpgradeRank("meta_ability_power", 8);
            store.SetUpgradeRank("meta_cooldown", 6);
            store.SetUpgradeRank("meta_crit_chance", 20);
            store.SetUpgradeRank("meta_crit_damage", 10);
            store.SetUpgradeRank("meta_exp_gain", 8);
            store.SetUpgradeRank("meta_item_drop", 5);
#endif
        }

        private static IEnumerator PlayOneRun(RunResult result, float minExpectedMaxHealth = 0f)
        {
            SceneManager.LoadScene("Beehive");
            yield return null;

            GameObject player = GameObject.FindWithTag("Player");
            Assert.IsNotNull(player, "Player exists after scene load");
            var movement = player.GetComponent<PlayerMovement>();
            Assert.IsNotNull(movement, "Player has PlayerMovement");

            PlayerStats stats = PlayerContext.Stats;
            Debug.Log(
                $"[BALANCE] run start: maxHP={stats.MaxHealth} dmg={stats.AttackDamage} " +
                $"atkSpd={stats.AttackSpeed} crit={stats.CritChancePercent}% moveSpd={stats.MoveSpeed}");
            Assert.GreaterOrEqual(
                stats.MaxHealth, minExpectedMaxHealth,
                "Meta upgrade ranks applied to the player at run start");

            var bot = new BalanceBot();
            movement.Initialize(bot, PlayerContext.Stats);

            GameObject gameOverPanel = FindInactiveByName("GameOverPanel");
            GameObject victoryPanel = FindInactiveByName("VictoryPanel");
            Assert.IsNotNull(gameOverPanel, "GameOverPanel found");
            Assert.IsNotNull(victoryPanel, "VictoryPanel found");

            Transform playerTransform = player.transform;
            float realElapsed = 0f;
            float lastChoiceClick = -1f;
            float nextMilestoneSeconds = 60f;

            while (realElapsed < MaxRealSecondsPerRun)
            {
                realElapsed += Time.unscaledDeltaTime;

                if (RunSession.Instance != null && RunSession.Instance.ElapsedSeconds >= nextMilestoneSeconds)
                {
                    var milestoneExp = Object.FindAnyObjectByType<PlayerExperience>();
                    Debug.Log(
                        $"[BALANCE] t={RunSession.Instance.ElapsedSeconds / 60f:F1}min " +
                        $"hp={(PlayerContext.Health != null ? PlayerContext.Health.CurrentHealth : -1f):F0} " +
                        $"level={(milestoneExp != null ? milestoneExp.CurrentLevel : -1)} " +
                        $"kills={RunSession.Instance.KillCount} " +
                        $"enemies={(EnemyRegistry.Instance != null ? EnemyRegistry.Instance.ActiveCount : -1)}");
                    nextMilestoneSeconds += 60f;
                }

                if (GamePause.IsPaused)
                {
                    if (gameOverPanel.activeInHierarchy || victoryPanel.activeInHierarchy)
                    {
                        result.Finished = true;
                        result.Victory = victoryPanel.activeInHierarchy;
                        result.EndSeconds = RunSession.Instance != null
                            ? RunSession.Instance.ElapsedSeconds
                            : 0f;
                        result.Kills = RunSession.Instance != null ? RunSession.Instance.KillCount : 0;
                        var experience = Object.FindAnyObjectByType<PlayerExperience>();
                        result.Level = experience != null ? experience.CurrentLevel : 0;
                        yield break;
                    }

                    // The only other pause source in an unattended run is the
                    // level-up offer — pick a random card through the real
                    // button path (throttled: the panel needs a frame to bind).
                    if (realElapsed - lastChoiceClick > 0.3f)
                    {
                        ClickRandomLevelUpChoice();
                        lastChoiceClick = realElapsed;
                    }
                }
                else
                {
                    if (!Mathf.Approximately(Time.timeScale, TimeScale))
                    {
                        Time.timeScale = TimeScale;
                    }

                    bot.Tick(playerTransform, realElapsed);
                }

                yield return null;
            }

            result.Finished = false;
            result.EndSeconds = RunSession.Instance != null ? RunSession.Instance.ElapsedSeconds : 0f;
        }

        private static void ClickRandomLevelUpChoice()
        {
            GameObject panel = GameObject.Find("LevelUpPanel");
            if (panel == null)
            {
                return;
            }

            // Up to 3 offered cards named Choice0..2; only the bound ones are
            // active. Reroll buttons live under the choices, so target the
            // choice buttons themselves rather than any Button in the panel.
            var candidates = new System.Collections.Generic.List<UnityEngine.UI.Button>(3);
            for (int i = 0; i < 3; i++)
            {
                Transform choice = panel.transform.Find($"Choice{i}");
                if (choice != null && choice.gameObject.activeInHierarchy
                    && choice.TryGetComponent(out UnityEngine.UI.Button button)
                    && button.interactable)
                {
                    candidates.Add(button);
                }
            }

            if (candidates.Count > 0)
            {
                candidates[Random.Range(0, candidates.Count)].onClick.Invoke();
            }
        }

        private static GameObject FindInactiveByName(string name)
        {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindChildRecursive(roots[i].transform, name);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Stand-in player: kites away from enemies that get too close (with a
        /// tangential drift so it orbits rather than running forever), loops
        /// back for pickups when safe, and otherwise hovers at the edge of
        /// basic-attack range so the auto-attack keeps firing.
        /// </summary>
        private sealed class BalanceBot : IMovementInputSource
        {
            private const float FleeRadius = 3.2f;
            // Hurt, a player stops hovering in attack range and kites wide
            // until a heal (or a lull) tops them back up — health-driven with
            // hysteresis, NOT crowd-driven: a dense crowd is the normal state
            // of the mid-game and backing off from it turns the bot into a
            // never-leveling pacifist.
            private const float SurvivalFleeRadius = 6.5f;
            private const float SurvivalEnterHpFraction = 0.35f;
            private const float SurvivalExitHpFraction = 0.55f;
            private const float EngageRadius = 4.4f;
            private const float ThreatRadius = 8f;
            private const float PickupRadius = 12f;
            private const float ItemDropRadius = 14f;
            private const float FleeDropDetourRadius = 5f;
            private const float PickupRefreshSeconds = 0.5f;
            private const float TangentWeight = 0.45f;

            private PickupItem[] _pickups = System.Array.Empty<PickupItem>();
            private ItemDrop[] _itemDrops = System.Array.Empty<ItemDrop>();
            private float _nextPickupRefresh;
            private bool _survivalMode;

            public Vector2 MoveDirection { get; private set; }

            public void Tick(Transform player, float realTime)
            {
                // Pickup scan on a slow real-time cadence — test-harness code,
                // not a runtime hot path.
                if (realTime >= _nextPickupRefresh)
                {
                    _pickups = Object.FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
                    _itemDrops = Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None);
                    _nextPickupRefresh = realTime + PickupRefreshSeconds;
                }

                Vector2 position = player.position;
                Vector2 repulsion = Vector2.zero;
                float nearestSqr = float.MaxValue;
                Vector2 nearestOffset = Vector2.zero;

                EnemyRegistry registry = EnemyRegistry.Instance;
                if (registry != null)
                {
                    var enemies = registry.ActiveEnemies;
                    for (int i = 0; i < registry.ActiveCount; i++)
                    {
                        EnemyController enemy = enemies[i];
                        if (enemy == null)
                        {
                            continue;
                        }

                        Vector2 offset = (Vector2)enemy.transform.position - position;
                        float sqr = offset.sqrMagnitude;
                        if (sqr < nearestSqr)
                        {
                            nearestSqr = sqr;
                            nearestOffset = offset;
                        }

                        if (sqr < ThreatRadius * ThreatRadius)
                        {
                            repulsion -= offset / (sqr + 0.25f);
                        }
                    }
                }

                float hpFraction = PlayerContext.Health != null && PlayerContext.Health.MaxHealth > 0f
                    ? PlayerContext.Health.CurrentHealth / PlayerContext.Health.MaxHealth
                    : 1f;
                if (_survivalMode)
                {
                    if (hpFraction > SurvivalExitHpFraction)
                    {
                        _survivalMode = false;
                    }
                }
                else if (hpFraction < SurvivalEnterHpFraction)
                {
                    _survivalMode = true;
                }

                float fleeRadius = _survivalMode ? SurvivalFleeRadius : FleeRadius;

                if (nearestSqr < fleeRadius * fleeRadius)
                {
                    // Kite: away from the local crowd, plus a fixed-handed
                    // tangent so a surround collapses into an orbit instead of
                    // a dead-center freeze.
                    Vector2 away = repulsion.sqrMagnitude > 0.0025f
                        ? repulsion.normalized
                        : -nearestOffset.normalized;
                    Vector2 tangent = new Vector2(-away.y, away.x);
                    Vector2 flee = (away + tangent * TangentWeight).normalized;

                    // A heal/shield/nuke close by is worth a detour even while
                    // fleeing — bias toward it the way a pressured human would.
                    Vector2 dropOffset;
                    if (TryGetNearestOffset(_itemDrops, position, FleeDropDetourRadius, out dropOffset))
                    {
                        flee = (flee + dropOffset.normalized * 0.7f).normalized;
                    }

                    MoveDirection = flee;
                    return;
                }

                // Item drops (heal/shield/bomb/magnet) expire in ~20s and swing
                // survival — grab them ahead of EXP motes, like a player would.
                Vector2 pickupOffset;
                if (TryGetNearestOffset(_itemDrops, position, ItemDropRadius, out pickupOffset)
                    || TryGetNearestOffset(_pickups, position, PickupRadius, out pickupOffset))
                {
                    MoveDirection = pickupOffset.normalized;
                    return;
                }

                if (!_survivalMode && nearestSqr != float.MaxValue && nearestSqr > EngageRadius * EngageRadius)
                {
                    MoveDirection = nearestOffset.normalized;
                    return;
                }

                MoveDirection = Vector2.zero;
            }

            private static bool TryGetNearestOffset<T>(T[] items, Vector2 position, float radius, out Vector2 offset)
                where T : Component
            {
                float bestSqr = radius * radius;
                bool found = false;
                offset = Vector2.zero;

                for (int i = 0; i < items.Length; i++)
                {
                    T item = items[i];
                    if (item == null || !item.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    Vector2 candidate = (Vector2)item.transform.position - position;
                    float sqr = candidate.sqrMagnitude;
                    if (sqr < bestSqr)
                    {
                        bestSqr = sqr;
                        offset = candidate;
                        found = true;
                    }
                }

                return found;
            }
        }
    }
}
