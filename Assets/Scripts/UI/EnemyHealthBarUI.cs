using SurveHive.Enemies;
using SurveHive.Health;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    public sealed class EnemyHealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private HealthComponent _health;

        // Shield-state cue (PLAN 3B): the fill tints while a shield pool is up so
        // the player reads "this hit is being soaked", reverting to the authored
        // colour once shields are down. Physical = steel blue, magic = violet.
        private static readonly Color PhysicalShieldTint = new Color(0.55f, 0.8f, 1f);
        private static readonly Color MagicShieldTint = new Color(0.85f, 0.55f, 1f);

        private EnemyDefense _defense;
        private Color _baseFillColor = Color.white;

        private void Awake()
        {
            // The bar canvas is a child of the enemy — reach up for its defense.
            EnemyController enemy = GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                _defense = enemy.Defense;
            }

            if (_fillImage != null)
            {
                _baseFillColor = _fillImage.color;
            }
        }

        private void OnEnable()
        {
            _health.OnHealthChanged += HandleHealthChanged;
            if (_defense != null)
            {
                _defense.OnShieldAbsorbed += HandleShieldAbsorbed;
            }

            HandleHealthChanged(_health.CurrentHealth, _health.MaxHealth);
            RefreshShieldTint();
        }

        private void OnDisable()
        {
            _health.OnHealthChanged -= HandleHealthChanged;
            if (_defense != null)
            {
                _defense.OnShieldAbsorbed -= HandleShieldAbsorbed;
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            UIBarFiller.SetFill(_fillImage, max > 0f ? current / max : 0f);
            // Also fires on spawn (HealthComponent.Initialize) — picks up the
            // freshly configured shield pools of a pooled reuse.
            RefreshShieldTint();
        }

        private void HandleShieldAbsorbed(float absorbed, float remainder)
        {
            RefreshShieldTint();
        }

        private void RefreshShieldTint()
        {
            if (_fillImage == null || _defense == null)
            {
                return;
            }

            if (_defense.PhysicalShield > 0f)
            {
                _fillImage.color = PhysicalShieldTint;
            }
            else if (_defense.MagicShield > 0f)
            {
                _fillImage.color = MagicShieldTint;
            }
            else
            {
                _fillImage.color = _baseFillColor;
            }
        }
    }
}
