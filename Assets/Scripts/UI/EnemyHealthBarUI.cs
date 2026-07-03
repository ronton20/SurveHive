using SurveHive.Health;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    public sealed class EnemyHealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private HealthComponent _health;

        private void OnEnable()
        {
            _health.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(_health.CurrentHealth, _health.MaxHealth);
        }

        private void OnDisable()
        {
            _health.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(float current, float max)
        {
            UIBarFiller.SetFill(_fillImage, max > 0f ? current / max : 0f);
        }
    }
}
