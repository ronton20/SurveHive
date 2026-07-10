using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurveHive.UI
{
    /// <summary>
    /// The game's single shared tooltip: shows on hover, follows the mouse,
    /// sizes itself to its text, and clamps to the screen (via the pure
    /// <see cref="TooltipLayout"/>). Lives on its own top-sorted overlay
    /// canvas (no GraphicRaycaster — it never blocks the pointer), one per
    /// scene, built by <c>TooltipBuilder</c>. Callers go through the static
    /// <see cref="Show"/>/<see cref="Hide"/> (difficulty unlock tasks today;
    /// status/set-effect info later) or put a <see cref="TooltipTrigger"/>
    /// on any hoverable UI element.
    /// </summary>
    public sealed class TooltipUI : MonoBehaviour
    {
        public static TooltipUI Instance { get; private set; }

        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _text;
        // Below-right of the cursor so the pointer never sits inside the panel.
        [SerializeField] private Vector2 _cursorOffset = new Vector2(18f, -20f);
        [SerializeField] private float _maxTextWidth = 420f;
        // Total horizontal / vertical padding — must match the text child's
        // inset from the panel so the sized panel hugs the text.
        [SerializeField] private Vector2 _padding = new Vector2(40f, 32f);

        private void Awake()
        {
            Instance = this;
            _panel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static void Show(string text)
        {
            if (Instance != null)
            {
                Instance.ShowInternal(text);
            }
        }

        public static void Hide()
        {
            if (Instance != null)
            {
                Instance._panel.gameObject.SetActive(false);
            }
        }

        private void ShowInternal(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                _panel.gameObject.SetActive(false);
                return;
            }

            _text.text = text;
            Vector2 preferred = _text.GetPreferredValues(text, _maxTextWidth, 0f);
            _panel.sizeDelta = new Vector2(
                Mathf.Min(preferred.x, _maxTextWidth) + _padding.x,
                preferred.y + _padding.y);
            _panel.gameObject.SetActive(true);
            // Place immediately — no one-frame flash at the previous position.
            FollowPointer();
        }

        private void Update()
        {
            if (_panel.gameObject.activeSelf)
            {
                FollowPointer();
            }
        }

        private void FollowPointer()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 screen = mouse.position.ReadValue();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, screen, null, out Vector2 local))
            {
                return;
            }

            _panel.anchoredPosition = TooltipLayout.Clamp(
                local + _cursorOffset, _panel.sizeDelta, _canvasRect.rect.size);
        }
    }
}
