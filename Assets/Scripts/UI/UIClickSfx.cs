using SurveHive.Core;
using SurveHive.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// Drop-on component for any menu/shop/pause <see cref="Button"/>: plays the
    /// shared UI click SFX. Added by the scene builders to every button they create.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIClickSfx : MonoBehaviour
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

        private static void HandleClick()
        {
            if (AudioService.Instance != null)
            {
                AudioService.Instance.PlaySfx(SfxId.UIClick);
            }
        }
    }
}
