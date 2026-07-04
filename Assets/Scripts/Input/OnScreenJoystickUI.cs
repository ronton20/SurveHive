using UnityEngine;
using UnityEngine.EventSystems;

namespace SurveHive.Input
{
    /// <summary>
    /// Floating touch joystick: this component lives on a fullscreen invisible
    /// touch zone. The stick appears where the finger first lands, the handle
    /// tracks the drag from that origin, and everything hides again on release.
    /// The whole zone is disabled on non-touch platforms by
    /// <see cref="PlayerInputController"/>.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class OnScreenJoystickUI : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;
        [SerializeField] private float _handleRange = 100f;

        private RectTransform _zone;
        private Vector2 _currentDirection;

        public Vector2 CurrentDirection => _currentDirection;

        private void Awake()
        {
            _zone = (RectTransform)transform;
            SetVisible(false);
        }

        private void OnDisable()
        {
            _currentDirection = Vector2.zero;
            SetVisible(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _zone, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                return;
            }

            _background.anchoredPosition = localPoint;
            _handle.anchoredPosition = Vector2.zero;
            _currentDirection = Vector2.zero;
            SetVisible(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                return;
            }

            Vector2 clamped = Vector2.ClampMagnitude(localPoint, _handleRange);
            _handle.anchoredPosition = clamped;
            _currentDirection = clamped / _handleRange;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _currentDirection = Vector2.zero;
            _handle.anchoredPosition = Vector2.zero;
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (_background != null)
            {
                _background.gameObject.SetActive(visible);
            }
        }
    }
}
