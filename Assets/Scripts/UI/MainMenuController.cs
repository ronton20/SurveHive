using SurveHive.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// Main menu flow: switches between the home / world-select / hive-upgrades /
    /// settings panels and starts runs. Exactly one panel is active at a time.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _worldSelectPanel;
        [SerializeField] private GameObject _shopPanel;
        [SerializeField] private GameObject _settingsPanel;
        // PLAN 5A: optional codex panel — the codex builder pass wires these;
        // every touch is null-guarded so the menu still runs without them.
        [SerializeField] private GameObject _codexPanel;
        // PLAN 5C: optional cosmetics panel, wired by CosmeticsBuilder the same way.
        [SerializeField] private GameObject _cosmeticsPanel;
        // PLAN 5D: optional achievements panel, wired by AchievementsBuilder.
        [SerializeField] private GameObject _achievementsPanel;
        // PLAN 5E: optional daily-deals panel, wired by RotatingShopBuilder.
        [SerializeField] private GameObject _dealsPanel;

        [SerializeField] private Button _playButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _worldSelectBackButton;
        [SerializeField] private Button _shopBackButton;
        [SerializeField] private Button _settingsBackButton;
        [SerializeField] private Button _startBeehiveButton;
        [SerializeField] private Button _codexButton;
        [SerializeField] private Button _codexBackButton;
        [SerializeField] private Button _cosmeticsButton;
        [SerializeField] private Button _cosmeticsBackButton;
        [SerializeField] private Button _achievementsButton;
        [SerializeField] private Button _achievementsBackButton;
        [SerializeField] private Button _dealsButton;
        [SerializeField] private Button _dealsBackButton;

        [SerializeField] private string _beehiveSceneName = "Beehive";

        // 3B-2c: the newly shown panel fades in on every switch (get-or-add a
        // CanvasGroup so no scene wiring is needed). One handle — only one panel
        // is ever active/fading at a time.
        private CanvasGroup _mainGroup;
        private CanvasGroup _worldSelectGroup;
        private CanvasGroup _shopGroup;
        private CanvasGroup _settingsGroup;
        private CanvasGroup _codexGroup;
        private CanvasGroup _cosmeticsGroup;
        private CanvasGroup _achievementsGroup;
        private CanvasGroup _dealsGroup;
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            // The menu can be entered from a paused run (results screen) — never
            // carry a frozen timescale into the menu.
            GamePause.SetPaused(false);

            _mainGroup = GetOrAddGroup(_mainPanel);
            _worldSelectGroup = GetOrAddGroup(_worldSelectPanel);
            _shopGroup = GetOrAddGroup(_shopPanel);
            _settingsGroup = GetOrAddGroup(_settingsPanel);
            if (_codexPanel != null)
            {
                _codexGroup = GetOrAddGroup(_codexPanel);
            }

            if (_cosmeticsPanel != null)
            {
                _cosmeticsGroup = GetOrAddGroup(_cosmeticsPanel);
            }

            if (_achievementsPanel != null)
            {
                _achievementsGroup = GetOrAddGroup(_achievementsPanel);
            }

            if (_dealsPanel != null)
            {
                _dealsGroup = GetOrAddGroup(_dealsPanel);
            }

            _playButton.onClick.AddListener(ShowWorldSelect);
            _shopButton.onClick.AddListener(ShowShop);
            _settingsButton.onClick.AddListener(ShowSettings);
            _quitButton.onClick.AddListener(QuitGame);
            _worldSelectBackButton.onClick.AddListener(ShowMain);
            _shopBackButton.onClick.AddListener(ShowMain);
            _settingsBackButton.onClick.AddListener(ShowMain);
            _startBeehiveButton.onClick.AddListener(StartBeehiveRun);
            if (_codexButton != null)
            {
                _codexButton.onClick.AddListener(ShowCodex);
            }

            if (_codexBackButton != null)
            {
                _codexBackButton.onClick.AddListener(ShowMain);
            }

            if (_cosmeticsButton != null)
            {
                _cosmeticsButton.onClick.AddListener(ShowCosmetics);
            }

            if (_cosmeticsBackButton != null)
            {
                _cosmeticsBackButton.onClick.AddListener(ShowMain);
            }

            if (_achievementsButton != null)
            {
                _achievementsButton.onClick.AddListener(ShowAchievements);
            }

            if (_achievementsBackButton != null)
            {
                _achievementsBackButton.onClick.AddListener(ShowMain);
            }

            if (_dealsButton != null)
            {
                _dealsButton.onClick.AddListener(ShowDeals);
            }

            if (_dealsBackButton != null)
            {
                _dealsBackButton.onClick.AddListener(CloseDeals);
            }

            ShowMain();
        }

        private void OnDestroy()
        {
            _playButton.onClick.RemoveListener(ShowWorldSelect);
            _shopButton.onClick.RemoveListener(ShowShop);
            _settingsButton.onClick.RemoveListener(ShowSettings);
            _quitButton.onClick.RemoveListener(QuitGame);
            _worldSelectBackButton.onClick.RemoveListener(ShowMain);
            _shopBackButton.onClick.RemoveListener(ShowMain);
            _settingsBackButton.onClick.RemoveListener(ShowMain);
            _startBeehiveButton.onClick.RemoveListener(StartBeehiveRun);
            if (_codexButton != null)
            {
                _codexButton.onClick.RemoveListener(ShowCodex);
            }

            if (_codexBackButton != null)
            {
                _codexBackButton.onClick.RemoveListener(ShowMain);
            }

            if (_cosmeticsButton != null)
            {
                _cosmeticsButton.onClick.RemoveListener(ShowCosmetics);
            }

            if (_cosmeticsBackButton != null)
            {
                _cosmeticsBackButton.onClick.RemoveListener(ShowMain);
            }

            if (_achievementsButton != null)
            {
                _achievementsButton.onClick.RemoveListener(ShowAchievements);
            }

            if (_achievementsBackButton != null)
            {
                _achievementsBackButton.onClick.RemoveListener(ShowMain);
            }

            if (_dealsButton != null)
            {
                _dealsButton.onClick.RemoveListener(ShowDeals);
            }

            if (_dealsBackButton != null)
            {
                _dealsBackButton.onClick.RemoveListener(CloseDeals);
            }
        }

        public void ShowMain()
        {
            SetActivePanel(_mainPanel);
        }

        public void ShowWorldSelect()
        {
            SetActivePanel(_worldSelectPanel);
        }

        public void ShowShop()
        {
            SetActivePanel(_shopPanel);
        }

        public void ShowSettings()
        {
            SetActivePanel(_settingsPanel);
        }

        public void ShowCodex()
        {
            if (_codexPanel != null)
            {
                SetActivePanel(_codexPanel);
            }
        }

        public void ShowCosmetics()
        {
            if (_cosmeticsPanel != null)
            {
                SetActivePanel(_cosmeticsPanel);
            }
        }

        public void ShowAchievements()
        {
            if (_achievementsPanel != null)
            {
                SetActivePanel(_achievementsPanel);
            }
        }

        public void ShowDeals()
        {
            if (_dealsPanel != null)
            {
                SetActivePanel(_dealsPanel);
            }
        }

        // Deals is now reached from a call-to-action inside the Hive Style panel,
        // so its Back button returns there (falling back to home if Style is
        // somehow absent).
        public void CloseDeals()
        {
            if (_cosmeticsPanel != null)
            {
                SetActivePanel(_cosmeticsPanel);
            }
            else
            {
                ShowMain();
            }
        }

        public void StartBeehiveRun()
        {
            SceneManager.LoadScene(_beehiveSceneName);
        }

        private void SetActivePanel(GameObject panel)
        {
            _mainPanel.SetActive(panel == _mainPanel);
            _worldSelectPanel.SetActive(panel == _worldSelectPanel);
            _shopPanel.SetActive(panel == _shopPanel);
            _settingsPanel.SetActive(panel == _settingsPanel);
            if (_codexPanel != null)
            {
                _codexPanel.SetActive(panel == _codexPanel);
            }

            if (_cosmeticsPanel != null)
            {
                _cosmeticsPanel.SetActive(panel == _cosmeticsPanel);
            }

            if (_achievementsPanel != null)
            {
                _achievementsPanel.SetActive(panel == _achievementsPanel);
            }

            if (_dealsPanel != null)
            {
                _dealsPanel.SetActive(panel == _dealsPanel);
            }

            FadeIn(GroupFor(panel));
        }

        private CanvasGroup GroupFor(GameObject panel)
        {
            if (panel == _worldSelectPanel)
            {
                return _worldSelectGroup;
            }

            if (panel == _shopPanel)
            {
                return _shopGroup;
            }

            if (panel == _settingsPanel)
            {
                return _settingsGroup;
            }

            if (panel == _codexPanel && _codexGroup != null)
            {
                return _codexGroup;
            }

            if (panel == _cosmeticsPanel && _cosmeticsGroup != null)
            {
                return _cosmeticsGroup;
            }

            if (panel == _achievementsPanel && _achievementsGroup != null)
            {
                return _achievementsGroup;
            }

            if (panel == _dealsPanel && _dealsGroup != null)
            {
                return _dealsGroup;
            }

            return _mainGroup;
        }

        private void FadeIn(CanvasGroup group)
        {
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

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
