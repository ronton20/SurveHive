using System.Text;
using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Progression;
using TMPro;
using UnityEngine;

namespace SurveHive.UI
{
    /// <summary>
    /// World-select difficulty picker (PLAN 1B): fills the dropdown from the
    /// difficulty tier table, restores the last-saved selection, and pushes
    /// changes into <see cref="RunSession.SelectedDifficulty"/> + the save.
    /// Locked tiers (unmet <see cref="DifficultyUnlocks"/> gates) show a
    /// LOCKED suffix, refuse selection, and surface their unlock tasks in the
    /// shared mouse-following <see cref="TooltipUI"/> — hovered via
    /// <see cref="DifficultyItemHover"/> on the dropdown rows. Lives on the
    /// world-select panel, so Awake runs on first open — before any run starts.
    /// </summary>
    public sealed class DifficultySelectUI : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _dropdown;
        [SerializeField] private DifficultySO _difficulty;
        [SerializeField] private PersistentMetaProgressionStoreSO _store;

        private bool[] _unlocked;
        private int _currentIndex;
        private readonly StringBuilder _tooltipBuilder = new StringBuilder(160);

        private void Awake()
        {
            if (_dropdown == null || _difficulty == null)
            {
                Debug.LogError("DifficultySelectUI: dropdown or difficulty table not wired.");
                return;
            }

            // Options come from the tier table so data edits (names, icons)
            // don't need a scene rebuild. Option index == table row index.
            _unlocked = new bool[_difficulty.TierCount];
            _dropdown.options.Clear();
            for (int i = 0; i < _difficulty.TierCount; i++)
            {
                DifficultySO.TierSettings tier = _difficulty.GetTierAt(i);
                _unlocked[i] = DifficultyUnlocks.IsUnlocked(tier, _store);
                string label = _unlocked[i]
                    ? tier.displayName
                    : tier.displayName + Loc.Get(LocKeys.DifficultyLockedSuffix);
                _dropdown.options.Add(new TMP_Dropdown.OptionData(label, tier.icon, Color.white));
            }

            DifficultyTier saved = _store != null ? _store.SelectedDifficulty : RunSession.SelectedDifficulty;
            _currentIndex = ClampToUnlocked(IndexOfTier(saved));
            _dropdown.SetValueWithoutNotify(_currentIndex);
            _dropdown.RefreshShownValue();
            RunSession.SelectedDifficulty = _difficulty.GetTierAt(_currentIndex).tier;

            HideUnlockTooltip();
            _dropdown.onValueChanged.AddListener(HandleValueChanged);
        }

        private void OnDestroy()
        {
            if (_dropdown != null)
            {
                _dropdown.onValueChanged.RemoveListener(HandleValueChanged);
            }
        }

        private void HandleValueChanged(int index)
        {
            // Picking a locked tier bounces back to the previous selection —
            // the hover tooltip (already up over the locked row) reads as the
            // "not yet" explanation.
            if (index >= 0 && index < _unlocked.Length && !_unlocked[index])
            {
                _dropdown.SetValueWithoutNotify(_currentIndex);
                _dropdown.RefreshShownValue();
                ShowUnlockTooltip(index);
                return;
            }

            _currentIndex = index;
            HideUnlockTooltip();

            DifficultyTier tier = _difficulty.GetTierAt(index).tier;
            RunSession.SelectedDifficulty = tier;

            if (_store != null)
            {
                _store.SelectedDifficulty = tier;
            }
        }

        /// <summary>Hover relay from <see cref="DifficultyItemHover"/> rows.</summary>
        public void ShowUnlockTooltipForLabel(string optionLabel)
        {
            for (int i = 0; i < _dropdown.options.Count; i++)
            {
                if (_dropdown.options[i].text == optionLabel)
                {
                    if (!_unlocked[i])
                    {
                        ShowUnlockTooltip(i);
                    }
                    else
                    {
                        HideUnlockTooltip();
                    }

                    return;
                }
            }
        }

        public void HideUnlockTooltip()
        {
            TooltipUI.Hide();
        }

        // Task list for a locked tier: met requirements get a green check and
        // strikethrough, open ones an empty box. Menu-path only — the string
        // build cost is fine.
        private void ShowUnlockTooltip(int index)
        {
            DifficultySO.TierSettings tier = _difficulty.GetTierAt(index);
            _tooltipBuilder.Clear();
            _tooltipBuilder.Append(Loc.Get(LocKeys.DifficultyUnlockPrefix));
            _tooltipBuilder.Append(tier.displayName);
            _tooltipBuilder.Append(':');

            if (tier.unlockRequirements != null)
            {
                for (int i = 0; i < tier.unlockRequirements.Length; i++)
                {
                    DifficultySO.UnlockRequirement requirement = tier.unlockRequirements[i];
                    bool met = DifficultyUnlocks.IsRequirementMet(requirement, _store);
                    _tooltipBuilder.Append('\n');
                    _tooltipBuilder.Append(met ? "<color=#7BE382>[X] <s>" : "[  ] ");
                    _tooltipBuilder.Append(Loc.Get(LocKeys.DifficultyClearPrefix));
                    _tooltipBuilder.Append(requirement.stageName);
                    _tooltipBuilder.Append(Loc.Get(LocKeys.DifficultyOn));
                    _tooltipBuilder.Append(GetTierName(requirement.clearTier));
                    if (met)
                    {
                        _tooltipBuilder.Append("</s></color>");
                    }
                }
            }

            TooltipUI.Show(_tooltipBuilder.ToString());
        }

        private string GetTierName(DifficultyTier tier)
        {
            return _difficulty.GetSettings(tier).displayName;
        }

        private int IndexOfTier(DifficultyTier tier)
        {
            for (int i = 0; i < _difficulty.TierCount; i++)
            {
                if (_difficulty.GetTierAt(i).tier == tier)
                {
                    return i;
                }
            }

            return 0;
        }

        // A save pointing at a now-locked tier (e.g. written before gating
        // existed) falls back to the highest unlocked tier below it.
        private int ClampToUnlocked(int index)
        {
            while (index > 0 && !_unlocked[index])
            {
                index--;
            }

            return index;
        }
    }
}
