using UnityEngine;
using UnityEngine.EventSystems;

namespace SurveHive.Input
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class OnScreenJoystickUI : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;
        [SerializeField] private float _handleRange = 100f;

        private Vector2 _currentDirection;

        public Vector2 CurrentDirection => _currentDirection;

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_background, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
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
        }
    }
}
