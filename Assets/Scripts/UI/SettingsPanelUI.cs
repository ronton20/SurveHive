using SurveHive.Data;
using SurveHive.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// Settings block (used by both the main-menu settings panel and the in-run
    /// pause menu): music/SFX sliders, vibration and quality cycle-buttons.
    /// Changes apply live and persist immediately through the 4A save.
    /// Music volume is stored for the Phase 5 audio service; SFX drives the
    /// master AudioListener today (SFX is the only audio that exists yet).
    /// </summary>
    public sealed class SettingsPanelUI : MonoBehaviour
    {
        [SerializeField] private PersistentMetaProgressionStoreSO _store;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Button _vibrationButton;
        [SerializeField] private TMP_Text _vibrationLabel;
        [SerializeField] private Button _qualityButton;
        [SerializeField] private TMP_Text _qualityLabel;

        // Guards against handler feedback while pushing stored values into the UI.
        private bool _loading;

        private void Awake()
        {
            _musicSlider.onValueChanged.AddListener(HandleMusicChanged);
            _sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
            _vibrationButton.onClick.AddListener(HandleVibrationClicked);
            _qualityButton.onClick.AddListener(HandleQualityClicked);
        }

        private void OnDestroy()
        {
            _musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);
            _sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
            _vibrationButton.onClick.RemoveListener(HandleVibrationClicked);
            _qualityButton.onClick.RemoveListener(HandleQualityClicked);
        }

        private void OnEnable()
        {
            _loading = true;
            SettingsData settings = _store.Settings;
            _musicSlider.value = settings.musicVolume;
            _sfxSlider.value = settings.sfxVolume;
            RefreshVibrationLabel(settings);
            RefreshQualityLabel(settings);
            ApplyLive(settings);
            _loading = false;
        }

        private void HandleMusicChanged(float value)
        {
            if (_loading)
            {
                return;
            }

            _store.Settings.musicVolume = value;
            SaveAndApply();
        }

        private void HandleSfxChanged(float value)
        {
            if (_loading)
            {
                return;
            }

            _store.Settings.sfxVolume = value;
            SaveAndApply();
        }

        private void HandleVibrationClicked()
        {
            SettingsData settings = _store.Settings;
            settings.vibration = !settings.vibration;
            RefreshVibrationLabel(settings);
            SaveAndApply();
        }

        private void HandleQualityClicked()
        {
            SettingsData settings = _store.Settings;
            // Cycle: -1 (project default) -> 0..n-1 -> back to -1.
            int next = settings.qualityLevel + 1;
            if (next >= QualitySettings.names.Length)
            {
                next = -1;
            }

            settings.qualityLevel = next;
            RefreshQualityLabel(settings);
            SaveAndApply();
        }

        private void RefreshVibrationLabel(SettingsData settings)
        {
            _vibrationLabel.text = settings.vibration ? "VIBRATION: ON" : "VIBRATION: OFF";
        }

        private void RefreshQualityLabel(SettingsData settings)
        {
            string name = settings.qualityLevel < 0
                ? "DEFAULT"
                : QualitySettings.names[settings.qualityLevel].ToUpperInvariant();
            _qualityLabel.text = $"QUALITY: {name}";
        }

        private void SaveAndApply()
        {
            ApplyLive(_store.Settings);
            _store.SaveSettings();
        }

        private static void ApplyLive(SettingsData settings)
        {
            AudioListener.volume = settings.sfxVolume;
            if (settings.qualityLevel >= 0 && settings.qualityLevel < QualitySettings.names.Length
                && QualitySettings.GetQualityLevel() != settings.qualityLevel)
            {
                QualitySettings.SetQualityLevel(settings.qualityLevel, applyExpensiveChanges: false);
            }
        }
    }
}
