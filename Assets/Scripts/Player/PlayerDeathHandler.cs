using SurveHive.Health;
using UnityEngine;
using UnityEngine.InputSystem;
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

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
            }

            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (!_isDead)
            {
                return;
            }

            if (WasRestartRequested())
            {
                Restart();
            }
        }

        private static bool WasRestartRequested()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                return true;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            return false;
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
