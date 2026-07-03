using System.Text;
using SurveHive.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    public sealed class ExpBarUI : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private PlayerExperience _playerExperience;

        private readonly StringBuilder _stringBuilder = new StringBuilder(16);

        private void OnEnable()
        {
            _playerExperience.OnExpChanged += HandleExpChanged;
            _playerExperience.OnLevelUp += HandleLevelUp;
            HandleLevelUp(_playerExperience.CurrentLevel);
        }

        private void OnDisable()
        {
            _playerExperience.OnExpChanged -= HandleExpChanged;
            _playerExperience.OnLevelUp -= HandleLevelUp;
        }

        private void HandleExpChanged(float current, float max)
        {
            UIBarFiller.SetFill(_fillImage, max > 0f ? current / max : 0f);
        }

        private void HandleLevelUp(int level)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("Lv. ");
            _stringBuilder.Append(level);
            _levelText.SetText(_stringBuilder);
        }
    }
}
