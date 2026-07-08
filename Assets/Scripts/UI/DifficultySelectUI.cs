using SurveHive.Core;
using SurveHive.Data;
using TMPro;
using UnityEngine;

namespace SurveHive.UI
{
    /// <summary>
    /// World-select difficulty picker (PLAN 1B): fills the dropdown from the
    /// difficulty tier table, restores the last-saved selection, and pushes
    /// changes into <see cref="RunSession.SelectedDifficulty"/> + the save.
    /// Lives on the world-select panel, so Awake runs on first open — before
    /// any run can start.
    /// </summary>
    public sealed class DifficultySelectUI : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _dropdown;
        [SerializeField] private DifficultySO _difficulty;
        [SerializeField] private PersistentMetaProgressionStoreSO _store;

        private void Awake()
        {
            if (_dropdown == null || _difficulty == null)
            {
                Debug.LogError("DifficultySelectUI: dropdown or difficulty table not wired.");
                return;
            }

            // Options come from the tier table so data edits (names, icons)
            // don't need a scene rebuild. Option index == table row index.
            _dropdown.options.Clear();
            for (int i = 0; i < _difficulty.TierCount; i++)
            {
                DifficultySO.TierSettings tier = _difficulty.GetTierAt(i);
                _dropdown.options.Add(new TMP_Dropdown.OptionData(tier.displayName, tier.icon, Color.white));
            }

            DifficultyTier saved = _store != null ? _store.SelectedDifficulty : RunSession.SelectedDifficulty;
            int savedIndex = IndexOfTier(saved);
            _dropdown.SetValueWithoutNotify(savedIndex);
            _dropdown.RefreshShownValue();
            RunSession.SelectedDifficulty = _difficulty.GetTierAt(savedIndex).tier;

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
            DifficultyTier tier = _difficulty.GetTierAt(index).tier;
            RunSession.SelectedDifficulty = tier;

            if (_store != null)
            {
                _store.SelectedDifficulty = tier;
            }
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
    }
}
