using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// One settings row for a single feedback layer (PLAN 3C): the label reads
    /// "NAME: ON/OFF", clicking flips the saved bool and persists it — the store
    /// pushes <see cref="FeedbackSettings"/> live on save, so the layer reacts
    /// immediately (mid-run for the pause-menu copy).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class FeedbackToggleUI : MonoBehaviour
    {
        [SerializeField] private PersistentMetaProgressionStoreSO _store;
        [SerializeField] private FeedbackToggleKind _kind;
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;

        private void Awake()
        {
            _button.onClick.AddListener(HandleClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClicked);
        }

        private void OnEnable()
        {
            RefreshLabel();
        }

        private void HandleClicked()
        {
            SettingsData settings = _store.Settings;
            SetValue(settings, !GetValue(settings));
            _store.SaveSettings();
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            // Concat is fine here: runs on open/click only, never per-frame.
            string suffix = Loc.Get(GetValue(_store.Settings)
                ? LocKeys.SettingsOnSuffix
                : LocKeys.SettingsOffSuffix);
            _label.text = Loc.Get(GetNameKey(_kind)) + suffix;
        }

        private bool GetValue(SettingsData settings)
        {
            switch (_kind)
            {
                case FeedbackToggleKind.EnemyHealthBars: return settings.showEnemyHealthBars;
                case FeedbackToggleKind.DamageNumbers: return settings.showDamageNumbers;
                case FeedbackToggleKind.ScreenShake: return settings.screenShake;
                case FeedbackToggleKind.HitStop: return settings.hitStop;
                case FeedbackToggleKind.StatusTints: return settings.statusTints;
                default: return true;
            }
        }

        private void SetValue(SettingsData settings, bool value)
        {
            switch (_kind)
            {
                case FeedbackToggleKind.EnemyHealthBars: settings.showEnemyHealthBars = value; break;
                case FeedbackToggleKind.DamageNumbers: settings.showDamageNumbers = value; break;
                case FeedbackToggleKind.ScreenShake: settings.screenShake = value; break;
                case FeedbackToggleKind.HitStop: settings.hitStop = value; break;
                case FeedbackToggleKind.StatusTints: settings.statusTints = value; break;
            }
        }

        private static string GetNameKey(FeedbackToggleKind kind)
        {
            switch (kind)
            {
                case FeedbackToggleKind.EnemyHealthBars: return LocKeys.SettingsEnemyHpBars;
                case FeedbackToggleKind.DamageNumbers: return LocKeys.SettingsDamageNumbers;
                case FeedbackToggleKind.ScreenShake: return LocKeys.SettingsScreenShake;
                case FeedbackToggleKind.HitStop: return LocKeys.SettingsHitStop;
                default: return LocKeys.SettingsStatusTints;
            }
        }
    }
}
