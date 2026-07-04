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
    /// </summary>
    public sealed class BossHealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private CanvasGroup _canvasGroup;

        private HealthComponent _tracked;

        private void Awake()
        {
            SetVisible(false);
        }

        private void OnDisable()
        {
            Untrack();
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
            UIBarFiller.SetFill(_fillImage, max > 0f ? current / max : 0f);
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
