using SurveHive.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// In-run pause menu: ESC or the HUD pause button freezes the run (via the
    /// central <see cref="GamePause"/>, so spawns/damage/cooldowns all stop) and
    /// shows resume / settings / abandon. Never opens over another pause owner
    /// (level-up offer, death or victory screen).
    /// </summary>
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _settingsBackButton;
        [SerializeField] private Button _abandonButton;
        [SerializeField] private string _menuSceneName = "MainMenu";

        [Header("Combat 2.0 1F — build list (optional)")]
        [SerializeField] private GameObject _powerUpsPanel;
        [SerializeField] private Button _powerUpsButton;
        [SerializeField] private Button _powerUpsBackButton;
        [SerializeField] private OwnedPowerUpsView _powerUpsView;

        private bool _isOpen;

        // 3B-2c: panels fade in when shown (get-or-add a CanvasGroup so no scene
        // wiring is needed). Hides stay instant — a fade-out would race the panel
        // swaps. One handle is enough: only one panel is ever fading in at a time.
        private CanvasGroup _pauseGroup;
        private CanvasGroup _settingsGroup;
        private CanvasGroup _powerUpsGroup;
        private Coroutine _fadeRoutine;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _pauseGroup = GetOrAddGroup(_pausePanel);
            _settingsGroup = GetOrAddGroup(_settingsPanel);
            if (_powerUpsPanel != null)
            {
                _powerUpsGroup = GetOrAddGroup(_powerUpsPanel);
            }

            _pauseButton.onClick.AddListener(TogglePause);
            _resumeButton.onClick.AddListener(Close);
            _settingsButton.onClick.AddListener(OpenSettings);
            _settingsBackButton.onClick.AddListener(CloseSettings);
            _abandonButton.onClick.AddListener(AbandonRun);

            if (_powerUpsButton != null)
            {
                _powerUpsButton.onClick.AddListener(OpenPowerUps);
            }

            if (_powerUpsBackButton != null)
            {
                _powerUpsBackButton.onClick.AddListener(ClosePowerUps);
            }
        }

        private void OnDestroy()
        {
            _pauseButton.onClick.RemoveListener(TogglePause);
            _resumeButton.onClick.RemoveListener(Close);
            _settingsButton.onClick.RemoveListener(OpenSettings);
            _settingsBackButton.onClick.RemoveListener(CloseSettings);
            _abandonButton.onClick.RemoveListener(AbandonRun);

            if (_powerUpsButton != null)
            {
                _powerUpsButton.onClick.RemoveListener(OpenPowerUps);
            }

            if (_powerUpsBackButton != null)
            {
                _powerUpsBackButton.onClick.RemoveListener(ClosePowerUps);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }

            // Hide the HUD button whenever any pause owner holds the freeze
            // (our own menu included — it has its own RESUME) so it never
            // floats over the level-up, death, victory or pause overlays.
            bool showButton = !GamePause.IsPaused;
            if (_pauseButton.gameObject.activeSelf != showButton)
            {
                _pauseButton.gameObject.SetActive(showButton);
            }
        }

        public void TogglePause()
        {
            if (_isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            // Another system already owns the freeze (level-up cards, death or
            // victory screen) — pausing over it would fight for timeScale.
            if (_isOpen || GamePause.IsPaused)
            {
                return;
            }

            _isOpen = true;
            _settingsPanel.SetActive(false);
            if (_powerUpsPanel != null)
            {
                _powerUpsPanel.SetActive(false);
            }

            Reveal(_pausePanel, _pauseGroup);
            GamePause.SetPaused(true);
        }

        public void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
            _settingsPanel.SetActive(false);
            if (_powerUpsPanel != null)
            {
                _powerUpsPanel.SetActive(false);
            }

            _pausePanel.SetActive(false);
            GamePause.SetPaused(false);
        }

        /// <summary>The settings view, for scripted drivers/tests.</summary>
        public void ShowSettings()
        {
            if (_isOpen)
            {
                OpenSettings();
            }
        }

        private void OpenSettings()
        {
            // Swap, don't stack — the pause panel would show through the
            // settings panel's translucent frame.
            _pausePanel.SetActive(false);
            Reveal(_settingsPanel, _settingsGroup);
        }

        private void CloseSettings()
        {
            _settingsPanel.SetActive(false);
            Reveal(_pausePanel, _pauseGroup);
        }

        private void OpenPowerUps()
        {
            if (_powerUpsView != null)
            {
                _powerUpsView.Refresh();
            }

            _pausePanel.SetActive(false);
            if (_powerUpsPanel != null)
            {
                Reveal(_powerUpsPanel, _powerUpsGroup);
            }
        }

        private void ClosePowerUps()
        {
            if (_powerUpsPanel != null)
            {
                _powerUpsPanel.SetActive(false);
            }

            Reveal(_pausePanel, _pauseGroup);
        }

        // Show a panel with a quick fade-in. Reused by every open/back path so the
        // pause flow reads consistently; hides remain instant SetActive(false).
        private void Reveal(GameObject panel, CanvasGroup group)
        {
            panel.SetActive(true);
            group.alpha = 0f;
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(UiAnim.FadeIn(group, UiAnim.FadeDuration));
        }

        private static CanvasGroup GetOrAddGroup(GameObject panel)
        {
            if (!panel.TryGetComponent(out CanvasGroup group))
            {
                group = panel.AddComponent<CanvasGroup>();
            }

            return group;
        }

        private void AbandonRun()
        {
            // Abandoning still banks the honey collected (and counts the run).
            if (RunSession.Instance != null)
            {
                RunSession.Instance.EndRun(victory: false);
            }

            GamePause.SetPaused(false);
            SceneManager.LoadScene(_menuSceneName);
        }
    }
}
