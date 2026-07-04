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

        [SerializeField] private Button _playButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _worldSelectBackButton;
        [SerializeField] private Button _shopBackButton;
        [SerializeField] private Button _settingsBackButton;
        [SerializeField] private Button _startBeehiveButton;

        [SerializeField] private string _beehiveSceneName = "Beehive";

        private void Awake()
        {
            // The menu can be entered from a paused run (results screen) — never
            // carry a frozen timescale into the menu.
            GamePause.SetPaused(false);

            _playButton.onClick.AddListener(ShowWorldSelect);
            _shopButton.onClick.AddListener(ShowShop);
            _settingsButton.onClick.AddListener(ShowSettings);
            _quitButton.onClick.AddListener(QuitGame);
            _worldSelectBackButton.onClick.AddListener(ShowMain);
            _shopBackButton.onClick.AddListener(ShowMain);
            _settingsBackButton.onClick.AddListener(ShowMain);
            _startBeehiveButton.onClick.AddListener(StartBeehiveRun);

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
