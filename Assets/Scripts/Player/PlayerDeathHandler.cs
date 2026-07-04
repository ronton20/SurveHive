using SurveHive.Core;
using SurveHive.Health;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurveHive.Player
{
    /// <summary>
    /// Ends the run when the player's health reaches zero: freezes the game, shows
    /// the game-over panel, and reloads the scene on the next restart input.
    /// </summary>
    public sealed class PlayerDeathHandler : MonoBehaviour
    {
        [SerializeField] private HealthComponent _health;
        [SerializeField] private GameObject _gameOverPanel;

        private bool _isDead;

        private void OnEnable()
        {
            _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            _health.OnDied -= HandleDied;
        }

        private void HandleDied()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;

            // Bank the run's currency on death too (victory banks via BossSpawner).
            if (RunSession.Instance != null)
            {
                RunSession.Instance.EndRun(victory: false);
            }

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
            }

            GamePause.SetPaused(true);
        }

        private void Update()
        {
            if (!_isDead)
            {
                return;
            }

            if (RestartInput.WasRequested())
            {
                Restart();
            }
        }

        private void Restart()
        {
            GamePause.SetPaused(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
