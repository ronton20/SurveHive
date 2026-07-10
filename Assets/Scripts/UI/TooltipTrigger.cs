using UnityEngine;
using UnityEngine.EventSystems;

namespace SurveHive.UI
{
    /// <summary>
    /// Puts a hover tooltip on any raycastable UI element: shows the shared
    /// <see cref="TooltipUI"/> on pointer-enter, hides on exit/disable. Text
    /// is authored in the Inspector or pushed at bind time via
    /// <see cref="SetText"/> (which live-refreshes an already-open tooltip),
    /// so dynamic sources — status effects, set effects — reuse the same path.
    /// </summary>
    public sealed class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, TextArea] private string _text;

        private bool _hovering;

        public void SetText(string text)
        {
            _text = text;
            if (_hovering)
            {
                TooltipUI.Show(_text);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(_text))
            {
                return;
            }

            _hovering = true;
            TooltipUI.Show(_text);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovering = false;
            TooltipUI.Hide();
        }

        private void OnDisable()
        {
            // Covers panels deactivating (or pooled rows despawning) mid-hover,
            // where pointer-exit never fires.
            if (_hovering)
            {
                _hovering = false;
                TooltipUI.Hide();
            }
        }
    }
}
