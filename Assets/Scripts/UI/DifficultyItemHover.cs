using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SurveHive.UI
{
    /// <summary>
    /// Lives on the difficulty dropdown's item template, so TMP_Dropdown
    /// clones it onto every row: hovering a row relays its label up to
    /// <see cref="DifficultySelectUI"/>, which shows the unlock-task tooltip
    /// for locked tiers (and hides it for unlocked ones). Menu-path only.
    /// </summary>
    public sealed class DifficultyItemHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private DifficultySelectUI _select;
        private TMP_Text _label;

        private void Awake()
        {
            _select = GetComponentInParent<DifficultySelectUI>(true);
            _label = GetComponentInChildren<TMP_Text>(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_select != null && _label != null)
            {
                _select.ShowUnlockTooltipForLabel(_label.text);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_select != null)
            {
                _select.HideUnlockTooltip();
            }
        }
    }
}
