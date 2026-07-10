using System.Text;
using SurveHive.Health;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    public sealed class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private HealthComponent _playerHealth;

        // PLAN 3B-2d readability adds (all optional — the bar degrades gracefully
        // if a field is unwired, so older scene data keeps working):
        [SerializeField] private TMP_Text _readoutText;      // "87 / 100"
        [SerializeField] private UIBarTrail _trail = new UIBarTrail();
        [SerializeField] private bool _tintByHealth = true;  // green→amber→red by ratio

        private const float CriticalRatio = 0.25f;

        private readonly StringBuilder _readout = new StringBuilder(16);
        private float _ratio = 1f;
        private Color _fillColor = Color.white;
        private int _lastShownCurrent = -1;
        private int _lastShownMax = -1;

        private void OnEnable()
        {
            _playerHealth.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
            _trail.Snap(_ratio);
        }

        private void OnDisable()
        {
            _playerHealth.OnHealthChanged -= HandleHealthChanged;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _trail.Tick(dt);
            ApplyCriticalPulse();
        }

        private void HandleHealthChanged(float current, float max)
        {
            _ratio = max > 0f ? current / max : 0f;
            UIBarFiller.SetFill(_fillImage, _ratio);

            if (_tintByHealth && _fillImage != null)
            {
                _fillColor = HealthColorGradient.Evaluate(_ratio);
                _fillImage.color = _fillColor;
            }

            _trail.SetTarget(_ratio);
            UpdateReadout(current, max);
        }

        private void UpdateReadout(float current, float max)
        {
            if (_readoutText == null)
            {
                return;
            }

            int shownCurrent = Mathf.CeilToInt(Mathf.Max(0f, current));
            int shownMax = Mathf.CeilToInt(Mathf.Max(0f, max));
            if (shownCurrent == _lastShownCurrent && shownMax == _lastShownMax)
            {
                return;
            }

            _lastShownCurrent = shownCurrent;
            _lastShownMax = shownMax;

            _readout.Clear();
            _readout.Append(shownCurrent);
            _readout.Append(" / ");
            _readout.Append(shownMax);
            _readoutText.SetText(_readout);
        }

        // Below the critical threshold the fill throbs so "you're about to die" reads
        // even in peripheral vision. Above it, restore the solid gradient colour.
        private void ApplyCriticalPulse()
        {
            if (!_tintByHealth || _fillImage == null)
            {
                return;
            }

            if (_ratio > 0f && _ratio <= CriticalRatio)
            {
                float pulse = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
                Color c = _fillColor;
                c.a = pulse;
                _fillImage.color = c;
            }
            else if (_fillImage.color.a != _fillColor.a)
            {
                _fillImage.color = _fillColor;
            }
        }
    }
}
