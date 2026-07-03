using SurveHive.Health;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    public sealed class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private HealthComponent _playerHealth;

        private void OnEnable()
        {
            _playerHealth.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
        }

        private void OnDisable()
        {
            _playerHealth.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(float current, float max)
        {
            UIBarFiller.SetFill(_fillImage, max > 0f ? current / max : 0f);
        }
    }
}
