using System.Text;
using SurveHive.Data;
using SurveHive.Health;
using SurveHive.Player;
using SurveHive.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    public sealed class LevelUpUIController : MonoBehaviour
    {
        [SerializeField] private SkillDatabaseSO _database;
        [SerializeField] private PlayerExperience _playerExperience;
        [SerializeField] private PlayerStats _playerStats;
        [SerializeField] private HealthComponent _playerHealth;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button[] _choiceButtons;
        [SerializeField] private Text[] _choiceNameTexts;
        [SerializeField] private Text[] _choiceDescriptionTexts;

        // Move speed is intentionally excluded here — it grows only through power-ups
        // (the Swift Wings skill), keeping it a rarer, more meaningful stat.
        [Header("Automatic Per-Level Stat Bonuses")]
        [SerializeField] private float _bonusMaxHealthPerLevel = 5f;
        [SerializeField] private float _bonusDamagePercentPerLevel = 3f;
        [SerializeField] private float _bonusAttackSpeedPercentPerLevel = 3f;

        // Times each skill (by database index) has been taken this run.
        private int[] _skillLevels;
        // Reused each open to hold the eligible (not-maxed) skill indices, shuffled.
        private int[] _indexBuffer;
        private SkillDefinitionSO[] _currentChoices;
        private int[] _currentChoiceDbIndices;
        private CanvasGroup _canvasGroup;
        private readonly StringBuilder _descriptionBuilder = new StringBuilder(96);

        private void Awake()
        {
            int skillCount = _database.Skills.Length;
            _skillLevels = new int[skillCount];
            _indexBuffer = new int[skillCount];
            _currentChoices = new SkillDefinitionSO[_choiceButtons.Length];
            _currentChoiceDbIndices = new int[_choiceButtons.Length];

            // The panel is kept active so this controller keeps running and stays
            // subscribed to level-up events; visibility is driven by a CanvasGroup
            // instead of SetActive (which would deactivate this very component).
            if (!_panelRoot.TryGetComponent(out _canvasGroup))
            {
                _canvasGroup = _panelRoot.AddComponent<CanvasGroup>();
            }

            Hide();
        }

        private void OnEnable()
        {
            _playerExperience.OnLevelUp += HandleLevelUp;

            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                int choiceIndex = i;
                _choiceButtons[i].onClick.AddListener(() => HandleChoiceSelected(choiceIndex));
            }
        }

        private void OnDisable()
        {
            _playerExperience.OnLevelUp -= HandleLevelUp;

            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                _choiceButtons[i].onClick.RemoveAllListeners();
            }
        }

        private void HandleLevelUp(int level)
        {
            ShowChoices();
        }

        private void ShowChoices()
        {
            ApplyAutoLevelBonus();

            int eligibleCount = BuildEligibleBuffer();
            ShufflePrefix(eligibleCount);

            int choiceCount = Mathf.Min(_choiceButtons.Length, eligibleCount);

            // Nothing left to offer (every skill maxed): resume without a panel and
            // drain any queued level-ups so the game doesn't stay paused.
            if (choiceCount == 0)
            {
                while (_playerExperience.TryDequeuePendingLevelUp(out int _))
                {
                    ApplyAutoLevelBonus();
                }

                Hide();
                Time.timeScale = 1f;
                return;
            }

            for (int i = 0; i < choiceCount; i++)
            {
                int dbIndex = _indexBuffer[i];
                SkillDefinitionSO skill = _database.Skills[dbIndex];
                _currentChoices[i] = skill;
                _currentChoiceDbIndices[i] = dbIndex;

                _choiceNameTexts[i].text = skill.DisplayName;
                _choiceDescriptionTexts[i].text = BuildDescription(skill, _skillLevels[dbIndex]);
                _choiceButtons[i].gameObject.SetActive(true);
            }

            for (int i = choiceCount; i < _choiceButtons.Length; i++)
            {
                _choiceButtons[i].gameObject.SetActive(false);
            }

            Show();
            Time.timeScale = 0f;
        }

        private void ApplyAutoLevelBonus()
        {
            _playerStats.IncreaseMaxHealthFlat(_bonusMaxHealthPerLevel);
            _playerHealth.IncreaseMaxHealth(_bonusMaxHealthPerLevel);
            _playerStats.IncreaseAttackDamagePercent(_bonusDamagePercentPerLevel);
            _playerStats.IncreaseAttackSpeedPercent(_bonusAttackSpeedPercentPerLevel);
        }

        private int BuildEligibleBuffer()
        {
            int count = 0;
            SkillDefinitionSO[] skills = _database.Skills;
            for (int i = 0; i < skills.Length; i++)
            {
                SkillDefinitionSO skill = skills[i];
                if (skill.HasLevelCap && _skillLevels[i] >= skill.MaxLevel)
                {
                    continue;
                }

                _indexBuffer[count] = i;
                count++;
            }

            return count;
        }

        private void ShufflePrefix(int eligibleCount)
        {
            int shuffleCount = Mathf.Min(_choiceButtons.Length, eligibleCount);
            for (int i = 0; i < shuffleCount; i++)
            {
                int swapIndex = Random.Range(i, eligibleCount);
                (_indexBuffer[i], _indexBuffer[swapIndex]) = (_indexBuffer[swapIndex], _indexBuffer[i]);
            }
        }

        private string BuildDescription(SkillDefinitionSO skill, int currentLevel)
        {
            _descriptionBuilder.Clear();

            if (currentLevel <= 0)
            {
                _descriptionBuilder.Append("New!  →  Lv. 1");
            }
            else
            {
                _descriptionBuilder.Append("Lv. ");
                _descriptionBuilder.Append(currentLevel);
                _descriptionBuilder.Append("  →  Lv. ");
                _descriptionBuilder.Append(currentLevel + 1);
            }

            _descriptionBuilder.Append('\n');
            _descriptionBuilder.Append('\n');
            _descriptionBuilder.Append(skill.Description);
            return _descriptionBuilder.ToString();
        }

        private void HandleChoiceSelected(int choiceIndex)
        {
            SkillDefinitionSO chosen = _currentChoices[choiceIndex];
            if (chosen != null)
            {
                SkillEffectApplier.Apply(chosen, _playerStats, _playerHealth);
                _skillLevels[_currentChoiceDbIndices[choiceIndex]]++;
            }

            if (_playerExperience.TryDequeuePendingLevelUp(out int _))
            {
                ShowChoices();
            }
            else
            {
                Hide();
                Time.timeScale = 1f;
            }
        }

        private void Show()
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        private void Hide()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }
}
