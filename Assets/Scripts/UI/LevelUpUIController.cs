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

        // Combat 2.0 card taxonomy (optional — guarded so the controller still runs
        // before the banner-UI builder pass wires these). Banner text/background
        // shows the lane (Passive/Enhancement/Ability); the gem tints by element.
        [SerializeField] private TMP_Text[] _choiceBanners;
        [SerializeField] private Image[] _choiceBannerBackgrounds;
        [SerializeField] private Image[] _choiceElementGems;
        // Shows the lane's owned/cap (e.g. "2/5") so players can see how much room
        // a lane has left before committing a pick (Combat 2.0 1F).
        [SerializeField] private TMP_Text[] _choiceLaneCounters;
        // Set-progress line rendered BELOW each card (3C UX — kept out of the card
        // so long descriptions never overflow it).
        [SerializeField] private TMP_Text[] _choiceSetTexts;
        // Offer context: "LEVEL UP!" vs "MINIBOSS KILLED!" (the forced-lucky beat).
        [SerializeField] private TMP_Text _titleText;

        // Phase 1C rerolls (optional — guarded so the controller runs before the
        // builder pass wires them): one button per card replaces just that card;
        // stock = the bought meta rank, spent per use, refilled each run.
        [Header("Power-Up Rerolls (1C)")]
        [SerializeField] private MetaProgressionStoreSO _metaStore;
        [SerializeField] private MetaUpgradeSO _rerollUpgrade;
        [SerializeField] private Button[] _rerollButtons;
        [SerializeField] private TMP_Text _rerollCountText;
        [SerializeField] private int _maxRerollsPerRun = 3;

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

        // Combat 2.0 (PLAN 1B): distinct-pick caps per lane. Once a lane is full,
        // no new pick from it is offered; owned picks keep leveling.
        [Header("Lane Selection Caps")]
        [SerializeField] private int _passiveCap = 5;
        [SerializeField] private int _enhancementCap = 3;
        [SerializeField] private int _abilityCap = 5;

        // Times each skill (by database index) has been taken this run.
        private int[] _skillLevels;
        // Reused each open: eligible (not-maxed) database indices + their weights.
        private int[] _indexBuffer;
        private float[] _weightBuffer;
        private float[] _weightScratch;
        private int[] _selectedBuffer;
        // Combat 2.0 (PLAN 1B): per-skill lane + cap tables, cached once (static
        // for the run) so lane gating stays zero-GC per level-up.
        private int[] _skillLanes;
        private int[] _skillMaxLevels;
        private readonly int[] _laneCaps = new int[3];
        private readonly int[] _ownedPerLane = new int[3];
        // Phase 3C: per-skill element cache + scratch counts pushed to ElementSets
        // after every pick (owned distinct enhancements+abilities per element).
        private int[] _skillElements;
        private readonly int[] _elementPieces = new int[ElementSets.ElementCount];
        private SkillDefinitionSO[] _currentChoices;
        private int[] _currentChoiceDbIndices;
        private bool[] _currentChoiceLucky;
        private int _currentChoiceCount;
        private bool _currentOfferForceLucky;
        private int _rerollStock;
        private readonly int[] _rerollResultScratch = new int[1];
        private Image[] _choiceBackgrounds;
        private CanvasGroup _canvasGroup;
        private readonly StringBuilder _descriptionBuilder = new StringBuilder(96);
        private readonly StringBuilder _counterBuilder = new StringBuilder(8);
        private readonly System.Random _rng = new System.Random();

        private void Awake()
        {
            int skillCount = _database.Skills.Length;
            _skillLevels = new int[skillCount];
            _indexBuffer = new int[skillCount];
            _weightBuffer = new float[skillCount];
            _weightScratch = new float[skillCount];

            _skillLanes = new int[skillCount];
            _skillMaxLevels = new int[skillCount];
            _skillElements = new int[skillCount];
            for (int i = 0; i < skillCount; i++)
            {
                SkillDefinitionSO skill = _database.Skills[i];
                _skillLanes[i] = (int)skill.Lane;
                _skillElements[i] = (int)skill.Element;
                // 0 = uncapped, matching LaneEligibility's convention.
                _skillMaxLevels[i] = skill.HasLevelCap ? skill.MaxLevel : 0;
            }

            // Fresh run: reset set counts and hand the per-element configs over.
            ElementSets.Initialize(_database.SetBonuses);
            _selectedBuffer = new int[_choiceButtons.Length];
            _currentChoices = new SkillDefinitionSO[_choiceButtons.Length];
            _currentChoiceDbIndices = new int[_choiceButtons.Length];
            _currentChoiceLucky = new bool[_choiceButtons.Length];

            _choiceBackgrounds = new Image[_choiceButtons.Length];
            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                _choiceBackgrounds[i] = _choiceButtons[i].GetComponent<Image>();
            }

            // Per-run reroll stock from the bought meta rank (1C).
            _rerollStock = RerollLogic.StockFromRank(
                _metaStore != null && _rerollUpgrade != null
                    ? _metaStore.GetUpgradeRank(_rerollUpgrade.UpgradeId)
                    : 0,
                _maxRerollsPerRun);

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

            if (_rerollButtons != null)
            {
                for (int i = 0; i < _rerollButtons.Length; i++)
                {
                    int choiceIndex = i;
                    _rerollButtons[i].onClick.AddListener(() => HandleReroll(choiceIndex));
                }
            }
        }

        private void OnDisable()
        {
            _playerExperience.OnLevelUp -= HandleLevelUp;

            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                _choiceButtons[i].onClick.RemoveAllListeners();
            }

            if (_rerollButtons != null)
            {
                for (int i = 0; i < _rerollButtons.Length; i++)
                {
                    _rerollButtons[i].onClick.RemoveAllListeners();
                }
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

            // Phase 2B: a miniboss reward makes this whole offer guaranteed-lucky.
            // Remembered for the offer's lifetime so rerolled cards stay lucky.
            _currentOfferForceLucky = _playerExperience.ConsumeForcedLucky();

            if (_titleText != null)
            {
                _titleText.text = _currentOfferForceLucky ? "MINIBOSS KILLED!" : "LEVEL UP!";
            }

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

            _currentChoiceCount = choiceCount;
            for (int i = 0; i < choiceCount; i++)
            {
                BindChoice(i, _selectedBuffer[i]);
                _choiceButtons[i].gameObject.SetActive(true);
            }

            for (int i = choiceCount; i < _choiceButtons.Length; i++)
            {
                _choiceButtons[i].gameObject.SetActive(false);
            }

            RefreshRerollUI();
            Show();
            GamePause.SetPaused(true);
        }

        // Fills card slot i with a skill — shared by the initial offer and by
        // rerolls, so a replaced card re-rolls its lucky chance and re-renders
        // rarity/banner/set exactly like a fresh one.
        private void BindChoice(int i, int dbIndex)
        {
            SkillDefinitionSO skill = _database.Skills[dbIndex];
            _currentChoices[i] = skill;
            _currentChoiceDbIndices[i] = dbIndex;

            // Lucky only matters when the skill has 2+ levels of headroom.
            bool canDoubleLevel = !skill.HasLevelCap || _skillLevels[dbIndex] + 2 <= skill.MaxLevel;
            _currentChoiceLucky[i] = canDoubleLevel && (_currentOfferForceLucky || _rng.NextDouble() < _luckyChance);

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

            ApplyCardTaxonomy(i, skill);

            if (_choiceSetTexts != null && i < _choiceSetTexts.Length && _choiceSetTexts[i] != null)
            {
                _choiceSetTexts[i].text = BuildSetLine(skill, _skillLevels[dbIndex]);
            }
        }

        // Spends one reroll to replace card i with a fresh eligible pick that
        // isn't already on screen. Keeps the stock when nothing else is
        // offerable (the pool is that thin — don't waste the charge).
        private void HandleReroll(int choiceIndex)
        {
            if (_rerollStock <= 0 || choiceIndex >= _currentChoiceCount || _currentChoices[choiceIndex] == null)
            {
                return;
            }

            int eligibleCount = BuildEligibleBuffer();
            int replacement = RerollLogic.PickReplacement(
                _indexBuffer, _weightBuffer, eligibleCount,
                _currentChoiceDbIndices, _currentChoiceCount,
                _rerollResultScratch, _weightScratch, _rng);
            if (replacement < 0)
            {
                return;
            }

            _rerollStock--;
            BindChoice(choiceIndex, replacement);
            RefreshRerollUI();
        }

        // Reroll controls: hidden entirely at zero stock at run start (no rank
        // bought), otherwise per-card buttons + the remaining count, greying
        // out once the stock is spent.
        private void RefreshRerollUI()
        {
            if (_rerollButtons == null || _rerollButtons.Length == 0)
            {
                return;
            }

            bool anyStockThisRun = _rerollStock > 0 || HasRerollRank();
            for (int i = 0; i < _rerollButtons.Length; i++)
            {
                bool visible = anyStockThisRun && i < _currentChoiceCount;
                _rerollButtons[i].gameObject.SetActive(visible);
                _rerollButtons[i].interactable = _rerollStock > 0;
            }

            if (_rerollCountText != null)
            {
                _rerollCountText.gameObject.SetActive(anyStockThisRun);
                _counterBuilder.Clear();
                _counterBuilder.Append("REROLLS: ");
                _counterBuilder.Append(_rerollStock);
                _rerollCountText.text = _counterBuilder.ToString();
            }
        }

        private bool HasRerollRank()
        {
            return _metaStore != null && _rerollUpgrade != null
                && _metaStore.GetUpgradeRank(_rerollUpgrade.UpgradeId) > 0;
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

        // Drives the lane banner + element gem for choice i. All slots are guarded
        // so the controller works before the banner-UI builder pass wires them.
        private void ApplyCardTaxonomy(int i, SkillDefinitionSO skill)
        {
            if (_choiceBanners != null && i < _choiceBanners.Length && _choiceBanners[i] != null)
            {
                _choiceBanners[i].text = GetLaneLabel(skill.Lane);
            }

            if (_choiceBannerBackgrounds != null && i < _choiceBannerBackgrounds.Length && _choiceBannerBackgrounds[i] != null)
            {
                _choiceBannerBackgrounds[i].color = GetLaneColor(skill.Lane);
            }

            if (_choiceElementGems != null && i < _choiceElementGems.Length && _choiceElementGems[i] != null)
            {
                _choiceElementGems[i].color = GetElementColor(skill.Element);
            }

            // Owned/cap for this card's lane (populated by BuildEligibleBuffer,
            // which runs before this loop). Runs on the paused level-up screen.
            if (_choiceLaneCounters != null && i < _choiceLaneCounters.Length && _choiceLaneCounters[i] != null)
            {
                int lane = (int)skill.Lane;
                _counterBuilder.Clear();
                _counterBuilder.Append(_ownedPerLane[lane]);
                _counterBuilder.Append('/');
                _counterBuilder.Append(_laneCaps[lane]);
                _choiceLaneCounters[i].text = _counterBuilder.ToString();
            }
        }

        private static string GetLaneLabel(PowerUpLane lane)
        {
            switch (lane)
            {
                case PowerUpLane.Enhancement:
                    return "ENHANCEMENT";
                case PowerUpLane.Ability:
                    return "ABILITY";
                default:
                    return "PASSIVE";
            }
        }

        private static Color GetLaneColor(PowerUpLane lane)
        {
            switch (lane)
            {
                // Steel blue: the player-self lane.
                case PowerUpLane.Passive:
                    return new Color(0.29f, 0.48f, 0.71f);
                // Amber: the attack-shaping lane.
                case PowerUpLane.Enhancement:
                    return new Color(0.96f, 0.65f, 0.14f);
                // Royal purple: the active-skill lane.
                default:
                    return new Color(0.48f, 0.18f, 0.55f);
            }
        }

        private static Color GetElementColor(SkillElement element)
        {
            return ElementPalette.GetColor(element);
        }

        // Recounts owned distinct enhancements+abilities per element and pushes
        // them to the ElementSets service (which no-ops when nothing changed).
        private void RefreshElementSets()
        {
            for (int i = 0; i < _elementPieces.Length; i++)
            {
                _elementPieces[i] = 0;
            }

            for (int i = 0; i < _skillLevels.Length; i++)
            {
                if (_skillLevels[i] <= 0 || _skillLanes[i] == (int)PowerUpLane.Passive)
                {
                    continue;
                }

                _elementPieces[_skillElements[i]]++;
            }

            ElementSets.UpdateCounts(_elementPieces);
        }

        private void ApplyAutoLevelBonus()
        {
            _playerStats.IncreaseMaxHealthFlat(_bonusMaxHealthPerLevel);
            _playerHealth.IncreaseMaxHealth(_bonusMaxHealthPerLevel);
            _playerStats.IncreaseAttackDamagePercent(_bonusDamagePercentPerLevel);
            _playerStats.IncreaseAttackSpeedPercent(_bonusAttackSpeedPercentPerLevel);
        }

        /// <summary>Cap on distinct picks for a lane (Combat 2.0 1F build list).</summary>
        public int GetLaneCap(PowerUpLane lane)
        {
            switch (lane)
            {
                case PowerUpLane.Enhancement:
                    return _enhancementCap;
                case PowerUpLane.Ability:
                    return _abilityCap;
                default:
                    return _passiveCap;
            }
        }

        /// <summary>
        /// Fills <paramref name="results"/> with every power-up owned this run
        /// (run-level ≥ 1). Called only while paused, so allocation is fine.
        /// </summary>
        public void GetOwnedPowerUps(System.Collections.Generic.List<OwnedPowerUp> results)
        {
            results.Clear();
            SkillDefinitionSO[] skills = _database.Skills;
            for (int i = 0; i < skills.Length; i++)
            {
                if (_skillLevels[i] <= 0)
                {
                    continue;
                }

                SkillDefinitionSO skill = skills[i];
                results.Add(new OwnedPowerUp(skill.DisplayName, skill.Lane, skill.Element, _skillLevels[i]));
            }
        }

        private int BuildEligibleBuffer()
        {
            _laneCaps[(int)PowerUpLane.Passive] = _passiveCap;
            _laneCaps[(int)PowerUpLane.Enhancement] = _enhancementCap;
            _laneCaps[(int)PowerUpLane.Ability] = _abilityCap;

            SkillDefinitionSO[] skills = _database.Skills;
            int count = LaneEligibility.BuildEligible(
                _skillLanes, _skillLevels, _skillMaxLevels, skills.Length,
                _laneCaps, _laneCaps.Length, _ownedPerLane, _indexBuffer);

            // Weights align to the eligible indices just written to _indexBuffer.
            for (int k = 0; k < count; k++)
            {
                _weightBuffer[k] = SkillRarityWeights.GetWeight(skills[_indexBuffer[k]].Rarity);
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

        // The below-card set line: what taking this card does for its element's
        // set — the tier it unlocks, or progress toward the next threshold and
        // what that will grant. Empty for passives, configless elements, and
        // owned cards (leveling never adds a set piece). Runs on the paused
        // offer screen, so the ToString is fine.
        private string BuildSetLine(SkillDefinitionSO skill, int currentLevel)
        {
            if (currentLevel > 0 || skill.Lane == PowerUpLane.Passive)
            {
                return string.Empty;
            }

            SetBonusSO bonus = ElementSets.GetBonus(skill.Element);
            if (bonus == null)
            {
                return string.Empty;
            }

            int piecesAfter = ElementSets.GetPieces(skill.Element) + 1;
            int tierAfter = bonus.GetTierIndex(piecesAfter);

            _descriptionBuilder.Clear();
            _descriptionBuilder.Append("<color=");
            _descriptionBuilder.Append(ElementPalette.GetHex(skill.Element));
            _descriptionBuilder.Append('>');
            _descriptionBuilder.Append(bonus.SetName);
            _descriptionBuilder.Append(" SET</color> ");

            if (tierAfter > ElementSets.GetTierIndex(skill.Element))
            {
                _descriptionBuilder.Append("— unlocks: ");
                _descriptionBuilder.Append(bonus.GetTier(tierAfter).Description);
            }
            else if (tierAfter + 1 < bonus.TierCount)
            {
                SetBonusTier next = bonus.GetTier(tierAfter + 1);
                _descriptionBuilder.Append(piecesAfter);
                _descriptionBuilder.Append('/');
                _descriptionBuilder.Append(next.PiecesRequired);
                _descriptionBuilder.Append(" — at ");
                _descriptionBuilder.Append(next.PiecesRequired);
                _descriptionBuilder.Append(": ");
                _descriptionBuilder.Append(next.Description);
            }
            else
            {
                _descriptionBuilder.Append("— maxed");
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

                    SkillEffectApplier.Apply(chosen, _skillLevels[dbIndex], _playerStats, _playerHealth, _activeSkillManager);
                    _skillLevels[dbIndex]++;
                }

                RefreshElementSets();
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
