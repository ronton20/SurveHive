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

        // TMP_Dropdown instantiates row clones at the scene ROOT and only then
        // parents them into the list — so Awake runs with no parent chain and
        // a cached GetComponentInParent would be null forever (the bug that
        // made hover dead and the tooltip stick after a click). Resolve at
        // event time instead; menu-path only, so the walk is fine.
        private DifficultySelectUI Select
        {
            get
            {
                if (_select == null)
                {
                    _select = GetComponentInParent<DifficultySelectUI>(true);
                }

                return _select;
            }
        }

        private void Awake()
        {
            _label = GetComponentInChildren<TMP_Text>(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            DifficultySelectUI select = Select;
            if (select != null && _label != null)
            {
                select.ShowUnlockTooltipForLabel(_label.text);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Straight to the shared tooltip — hiding must never depend on the
            // parent lookup succeeding.
            TooltipUI.Hide();
        }

        private void OnDisable()
        {
            // The dropdown destroys its list on selection/close — pointer-exit
            // never fires on the hovered row, so hide here or the tooltip sticks.
            TooltipUI.Hide();
        }
    }
}
