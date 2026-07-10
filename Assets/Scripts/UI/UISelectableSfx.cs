using SurveHive.Core;
using SurveHive.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// Click + hover SFX for non-Button selectables that <see cref="UIClickSfx"/>
    /// can't cover — it requires a <see cref="Button"/> and hooks its <c>onClick</c>,
    /// which dropdowns and toggles don't have. This plays the shared UI click on
    /// pointer-click and the quiet hover tick on pointer-enter, gated on the host
    /// selectable being interactable. It coexists with the host's own click handling
    /// (e.g. <c>TMP_Dropdown</c> opening its list) — Unity dispatches the pointer
    /// event to every <see cref="IPointerClickHandler"/> on the object.
    /// </summary>
    public sealed class UISelectableSfx : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        private Selectable _selectable;

        private void Awake()
        {
            TryGetComponent(out _selectable);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Play(SfxId.UIClick);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Play(SfxId.UIHover);
        }

        private void Play(SfxId id)
        {
            // Suppress only when the sibling selectable is explicitly non-interactable
            // (greyed out); a missing selectable still counts as clickable.
            if (_selectable != null && !_selectable.IsInteractable())
            {
                return;
            }

            if (AudioService.Instance != null)
            {
                AudioService.Instance.PlaySfx(id);
            }
        }
    }
}
