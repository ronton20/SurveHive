using SurveHive.Health;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// HUD boss health bar: hidden until a boss spawns, then tracks its
    /// HealthComponent until death/despawn. Visibility via CanvasGroup so this
    /// component stays alive to receive Track calls.
    /// PLAN 3B-2d polish: a lagging damage trail (dramatic on big boss hits) and a
    /// low-HP colour shift toward red so the "almost dead" moment reads.
    /// </summary>
    public sealed class BossHealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private UIBarTrail _trail = new UIBarTrail();

        // Below this ratio the fill lerps from its authored colour toward danger red.
        private const float LowHealthRatio = 0.35f;
        private static readonly Color LowHealthColor = new Color(0.85f, 0.14f, 0.14f);

        private HealthComponent _tracked;
        private Color _fullColor = Color.white;

        private void Awake()
        {
            if (_fillImage != null)
            {
                _fullColor = _fillImage.color;
            }

            SetVisible(false);
        }

        private void OnDisable()
        {
            Untrack();
        }

        private void Update()
        {
            if (_tracked == null)
            {
                return;
            }

            _trail.Tick(Time.unscaledDeltaTime);
        }

        public void Track(HealthComponent health, string displayName)
        {
            Untrack();

            _tracked = health;
            if (_tracked == null)
            {
                return;
            }

            _tracked.OnHealthChanged += HandleHealthChanged;
            _tracked.OnDied += HandleDied;

            if (_nameText != null)
            {
                _nameText.text = displayName;
            }

            UIBarFiller.SetFill(_fillImage, 1f);
            if (_fillImage != null)
            {
                _fillImage.color = _fullColor;
            }

            _trail.Snap(1f);
            SetVisible(true);
        }

        private void Untrack()
        {
            if (_tracked != null)
            {
                _tracked.OnHealthChanged -= HandleHealthChanged;
                _tracked.OnDied -= HandleDied;
                _tracked = null;
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            float ratio = max > 0f ? current / max : 0f;
            UIBarFiller.SetFill(_fillImage, ratio);
            _trail.SetTarget(ratio);

            if (_fillImage != null)
            {
                _fillImage.color = ratio >= LowHealthRatio
                    ? _fullColor
                    : Color.Lerp(LowHealthColor, _fullColor, ratio / LowHealthRatio);
            }
        }

        private void HandleDied()
        {
            Untrack();
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
            }
        }
    }
}
