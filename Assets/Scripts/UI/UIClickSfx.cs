using SurveHive.Core;
using SurveHive.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// Drop-on component for any menu/shop/pause <see cref="Button"/>: plays the
    /// shared UI click SFX on press and a subtler hover SFX on pointer-enter.
    /// Added by the scene builders to every button they create, so both cues have
    /// blanket coverage. Hover only fires for an interactable button; the hover
    /// clip is quiet + throttled in the audio library so cursor sweeps stay light.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIClickSfx : MonoBehaviour, IPointerEnterHandler
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Skip disabled/non-interactable buttons so greyed-out cards stay silent.
            if (_button.IsInteractable() && AudioService.Instance != null)
            {
                AudioService.Instance.PlaySfx(SfxId.UIHover);
            }
        }

        private static void HandleClick()
        {
            if (AudioService.Instance != null)
            {
                AudioService.Instance.PlaySfx(SfxId.UIClick);
            }
        }
    }
}
