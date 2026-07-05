using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Enemies;
using SurveHive.Health;
using SurveHive.Spawning;
using SurveHive.UI;
using SurveHive.View;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurveHive.Stage
{
    /// <summary>
    /// Handles the stage director's miniboss/final-boss events: spawns the boss
    /// offscreen, announces it (banner + screen shake), hooks the HUD boss
    /// health bar, and — when the final boss dies — ends the run in victory
    /// (banks currency, shows the victory panel, restart input reloads).
    /// </summary>
    public sealed class BossSpawner : MonoBehaviour
    {
        [SerializeField] private StageDirector _director;
        [SerializeField] private EnemySpawner _spawner;
        [SerializeField] private BossHealthBarUI _bossHealthBar;
        [SerializeField] private BossBannerUI _banner;
        [SerializeField] private CameraShaker _shaker;
        // Corrupted workers the Queen summons.
        [SerializeField] private EnemyStatsSO _summonStats;
        [SerializeField] private GameObject _victoryPanel;
        [SerializeField] private float _spawnDistance = 12f;
        [SerializeField] private float _spawnShakeAmplitude = 0.4f;

        // The boss currently gating the timeline (miniboss or final). Killing it
        // resumes the stage (miniboss) or wins the run (final boss).
        private HealthComponent _gatingBossHealth;
        private bool _gatingBossIsFinal;
        private bool _victoryShown;

        private void OnEnable()
        {
            if (_director != null)
            {
                _director.OnBossEvent += HandleBossEvent;
            }
        }

        private void OnDisable()
        {
            if (_director != null)
            {
                _director.OnBossEvent -= HandleBossEvent;
            }

            UnhookGatingBoss();
        }

        private void HandleBossEvent(StageTimelineEvent stageEvent)
        {
            if (_spawner == null || _spawner.Player == null || stageEvent.EnemyStats == null)
            {
                return;
            }

            Vector2 direction = Random.insideUnitCircle.normalized;
            Vector3 position = _spawner.Player.position + (Vector3)(direction * _spawnDistance);
            GameObject bossGo = _spawner.SpawnAt(stageEvent.EnemyStats, position);
            if (bossGo == null)
            {
                return;
            }

            if (_banner != null)
            {
                _banner.Show(stageEvent.EnemyStats.DisplayName);
            }

            if (_shaker != null)
            {
                _shaker.Shake(_spawnShakeAmplitude);
            }

            HealthComponent health = bossGo.GetComponent<HealthComponent>();
            if (_bossHealthBar != null && health != null)
            {
                _bossHealthBar.Track(health, stageEvent.EnemyStats.DisplayName);
            }

            if (bossGo.TryGetComponent(out QueenBossController queen))
            {
                queen.Initialize(_spawner, _summonStats);
            }

            // Gate the timeline on this boss (freeze progress + drip). Only if it
            // has health to die by, else the run would soft-lock frozen.
            if (health != null)
            {
                UnhookGatingBoss();
                _gatingBossHealth = health;
                _gatingBossIsFinal = stageEvent.Type == StageEventType.FinalBoss;
                _gatingBossHealth.OnDied += HandleGatingBossDied;

                if (StageDirector.Instance != null)
                {
                    StageDirector.Instance.SetBossActive(true);
                }
            }
        }

        private void HandleGatingBossDied()
        {
            bool wasFinal = _gatingBossIsFinal;
            UnhookGatingBoss();

            // Resume the timeline + drip (miniboss); for the final boss the
            // victory pause takes over immediately anyway.
            if (StageDirector.Instance != null)
            {
                StageDirector.Instance.SetBossActive(false);
            }

            if (wasFinal)
            {
                // Killing the Queen = world clear (PLAN §5.2): first winnable run.
                ShowVictory();
            }
        }

        private void ShowVictory()
        {
            if (_victoryShown)
            {
                return;
            }

            _victoryShown = true;

            if (RunSession.Instance != null)
            {
                RunSession.Instance.EndRun(victory: true);
            }

            if (_victoryPanel != null)
            {
                _victoryPanel.SetActive(true);
            }

            GamePause.SetPaused(true);
        }

        private void UnhookGatingBoss()
        {
            if (_gatingBossHealth != null)
            {
                _gatingBossHealth.OnDied -= HandleGatingBossDied;
                _gatingBossHealth = null;
            }
        }

        private void Update()
        {
            if (!_victoryShown)
            {
                return;
            }

            if (RestartInput.WasRequested())
            {
                GamePause.SetPaused(false);
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }
}
