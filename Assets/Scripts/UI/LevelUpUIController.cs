using System.Text;
using SurveHive.Combat.Skills;
using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Health;
using SurveHive.Player;
using SurveHive.Progression;
using TMPro;
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
        [SerializeField] private ActiveSkillManager _activeSkillManager;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button[] _choiceButtons;
        [SerializeField] private TMP_Text[] _choiceNameTexts;
        [SerializeField] private TMP_Text[] _choiceDescriptionTexts;
        [SerializeField] private Image[] _choiceIcons;

        // Move speed is intentionally excluded here — it grows only through power-ups
        // (the Swift Wings skill), keeping it a rarer, more meaningful stat.
        [Header("Automatic Per-Level Stat Bonuses")]
        [SerializeField] private float _bonusMaxHealthPerLevel = 5f;
        [SerializeField] private float _bonusDamagePercentPerLevel = 3f;
        [SerializeField] private float _bonusAttackSpeedPercentPerLevel = 3f;

        [Header("Rarity & Lucky Picks")]
        // Chance per offered card to roll "lucky": the pick grants +2 levels and
        // the card shows a distinct background so the roll is visible up front.
        [SerializeField, Range(0f, 1f)] private float _luckyChance = 0.08f;
        [SerializeField] private Color _commonCardColor = new Color(0.91f, 0.847f, 0.627f);
        [SerializeField] private Color _rareCardColor = new Color(1f, 0.72f, 0.28f);
        [SerializeField] private Color _epicCardColor = new Color(0.82f, 0.6f, 0.98f);
        [SerializeField] private Color _luckyCardColor = new Color(0.68f, 0.92f, 0.45f);

        // Times each skill (by database index) has been taken this run.
        private int[] _skillLevels;
        // Reused each open: eligible (not-maxed) database indices + their weights.
        private int[] _indexBuffer;
        private float[] _weightBuffer;
        private float[] _weightScratch;
        private int[] _selectedBuffer;
        private SkillDefinitionSO[] _currentChoices;
        private int[] _currentChoiceDbIndices;
        private bool[] _currentChoiceLucky;
        private Image[] _choiceBackgrounds;
        private CanvasGroup _canvasGroup;
        private readonly StringBuilder _descriptionBuilder = new StringBuilder(96);
        private readonly System.Random _rng = new System.Random();

        private void Awake()
        {
            int skillCount = _database.Skills.Length;
            _skillLevels = new int[skillCount];
            _indexBuffer = new int[skillCount];
            _weightBuffer = new float[skillCount];
            _weightScratch = new float[skillCount];
            _selectedBuffer = new int[_choiceButtons.Length];
            _currentChoices = new SkillDefinitionSO[_choiceButtons.Length];
            _currentChoiceDbIndices = new int[_choiceButtons.Length];
            _currentChoiceLucky = new bool[_choiceButtons.Length];

            _choiceBackgrounds = new Image[_choiceButtons.Length];
            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                _choiceBackgrounds[i] = _choiceButtons[i].GetComponent<Image>();
            }

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
            if (AudioService.Instance != null)
            {
                AudioService.Instance.PlaySfx(SfxId.LevelUp);
            }

            ShowChoices();
        }

        private void ShowChoices()
        {
            ApplyAutoLevelBonus();

            int eligibleCount = BuildEligibleBuffer();
            int choiceCount = SkillOfferSelector.Select(
                _indexBuffer, _weightBuffer, eligibleCount, _choiceButtons.Length,
                _selectedBuffer, _weightScratch, _rng);

            // Nothing left to offer (every skill maxed): resume without a panel and
            // drain any queued level-ups so the game doesn't stay paused.
            if (choiceCount == 0)
            {
                while (_playerExperience.TryDequeuePendingLevelUp(out int _))
                {
                    ApplyAutoLevelBonus();
                }

                Hide();
                GamePause.SetPaused(false);
                return;
            }

            for (int i = 0; i < choiceCount; i++)
            {
                int dbIndex = _selectedBuffer[i];
                SkillDefinitionSO skill = _database.Skills[dbIndex];
                _currentChoices[i] = skill;
                _currentChoiceDbIndices[i] = dbIndex;

                // Lucky only matters when the skill has 2+ levels of headroom.
                bool canDoubleLevel = !skill.HasLevelCap || _skillLevels[dbIndex] + 2 <= skill.MaxLevel;
                _currentChoiceLucky[i] = canDoubleLevel && _rng.NextDouble() < _luckyChance;

                _choiceNameTexts[i].text = skill.DisplayName;
                _choiceDescriptionTexts[i].text = BuildDescription(skill, _skillLevels[dbIndex], _currentChoiceLucky[i]);

                if (_choiceBackgrounds[i] != null)
                {
                    _choiceBackgrounds[i].color = _currentChoiceLucky[i] ? _luckyCardColor : GetRarityColor(skill.Rarity);
                }

                if (_choiceIcons != null && i < _choiceIcons.Length && _choiceIcons[i] != null)
                {
                    _choiceIcons[i].sprite = skill.Icon;
                    _choiceIcons[i].enabled = skill.Icon != null;
                }

                _choiceButtons[i].gameObject.SetActive(true);
            }

            for (int i = choiceCount; i < _choiceButtons.Length; i++)
            {
                _choiceButtons[i].gameObject.SetActive(false);
            }

            Show();
            GamePause.SetPaused(true);
        }

        private Color GetRarityColor(SkillRarity rarity)
        {
            switch (rarity)
            {
                case SkillRarity.Rare:
                    return _rareCardColor;
                case SkillRarity.Epic:
                    return _epicCardColor;
                default:
                    return _commonCardColor;
            }
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
                _weightBuffer[count] = SkillRarityWeights.GetWeight(skill.Rarity);
                count++;
            }

            return count;
        }

        private string BuildDescription(SkillDefinitionSO skill, int currentLevel, bool lucky)
        {
            _descriptionBuilder.Clear();

            int levelGain = lucky ? 2 : 1;
            int targetLevel = currentLevel + levelGain;
            if (skill.HasLevelCap)
            {
                targetLevel = Mathf.Min(targetLevel, skill.MaxLevel);
            }

            if (lucky)
            {
                _descriptionBuilder.Append("LUCKY! +2 levels\n");
            }

            if (currentLevel <= 0 && !lucky)
            {
                _descriptionBuilder.Append("New!");
            }
            else
            {
                if (currentLevel <= 0)
                {
                    _descriptionBuilder.Append("New");
                }
                else
                {
                    _descriptionBuilder.Append("Lv. ");
                    _descriptionBuilder.Append(currentLevel);
                }

                _descriptionBuilder.Append("  →  ");

                // Taking this pick hits the cap: make that explicit.
                if (skill.HasLevelCap && targetLevel >= skill.MaxLevel)
                {
                    _descriptionBuilder.Append("MAX");
                }
                else
                {
                    _descriptionBuilder.Append("Lv. ");
                    _descriptionBuilder.Append(targetLevel);
                }
            }

            _descriptionBuilder.Append('\n');
            _descriptionBuilder.Append('\n');

            // New skills read their flavor text; upgrades show exactly which
            // numbers change instead (e.g. "Basic Attack DMG 10 → 11").
            if (currentLevel <= 0)
            {
                _descriptionBuilder.Append(skill.Description);
            }
            else
            {
                int applications = Mathf.Max(1, targetLevel - currentLevel);
                SkillStatPreview.AppendUpgradeLines(
                    _descriptionBuilder, skill, currentLevel, applications, _playerStats, skill.MaxLevel);
            }

            return _descriptionBuilder.ToString();
        }

        private void HandleChoiceSelected(int choiceIndex)
        {
            SkillDefinitionSO chosen = _currentChoices[choiceIndex];
            if (chosen != null)
            {
                int dbIndex = _currentChoiceDbIndices[choiceIndex];
                int applications = _currentChoiceLucky[choiceIndex] ? 2 : 1;
                for (int i = 0; i < applications; i++)
                {
                    if (chosen.HasLevelCap && _skillLevels[dbIndex] >= chosen.MaxLevel)
                    {
                        break;
                    }

                    SkillEffectApplier.Apply(chosen, _playerStats, _playerHealth, _activeSkillManager);
                    _skillLevels[dbIndex]++;
                }
            }

            if (_playerExperience.TryDequeuePendingLevelUp(out int _))
            {
                ShowChoices();
            }
            else
            {
                Hide();
                GamePause.SetPaused(false);
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
