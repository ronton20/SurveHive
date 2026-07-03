using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SurveHive.Input
{
    public sealed class ClickToMoveInputSource : IMovementInputSource
    {
        private const float ArrivalThresholdSqr = 0.01f;

        private readonly InputAction _pointerPositionAction;
        private readonly InputAction _pointerClickAction;
        private readonly Camera _camera;
        private readonly Transform _selfTransform;

        private Vector2 _moveDirection;
        private bool _hasTarget;
        private Vector3 _targetPosition;

        private bool _pendingClick;
        private Vector2 _pendingClickScreenPosition;

        public ClickToMoveInputSource(InputAction pointerPositionAction, InputAction pointerClickAction, Camera camera, Transform selfTransform)
        {
            _pointerPositionAction = pointerPositionAction;
            _pointerClickAction = pointerClickAction;
            _camera = camera;
            _selfTransform = selfTransform;
            _pointerClickAction.performed += OnPointerClick;
        }

        public Vector2 MoveDirection => _moveDirection;

        public void Dispose()
        {
            _pointerClickAction.performed -= OnPointerClick;
        }

        public void Tick()
        {
            if (_pendingClick)
            {
                _pendingClick = false;
                ProcessClick();
            }

            if (!_hasTarget)
            {
                _moveDirection = Vector2.zero;
                return;
            }

            Vector3 toTarget = _targetPosition - _selfTransform.position;
            if (toTarget.sqrMagnitude <= ArrivalThresholdSqr)
            {
                _hasTarget = false;
                _moveDirection = Vector2.zero;
                return;
            }

            _moveDirection = ((Vector2)toTarget).normalized;
        }

        // Only capture the click here; the actual UI/pause checks and raycast run in
        // Tick(). IsPointerOverGameObject() must not be called from inside an input
        // callback — it would report last frame's UI state and log a warning.
        private void OnPointerClick(InputAction.CallbackContext context)
        {
            _pendingClickScreenPosition = _pointerPositionAction.ReadValue<Vector2>();
            _pendingClick = true;
        }

        private void ProcessClick()
        {
            // Don't issue a move order while paused (e.g. the level-up / game-over
            // panel is open) or when the click lands on a UI element — otherwise
            // picking a skill also queues a walk to that screen point.
            if (Time.timeScale == 0f)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            float distanceToPlane = _selfTransform.position.z - _camera.transform.position.z;
            Vector3 worldPos = _camera.ScreenToWorldPoint(new Vector3(_pendingClickScreenPosition.x, _pendingClickScreenPosition.y, distanceToPlane));
            worldPos.z = _selfTransform.position.z;
            _targetPosition = worldPos;
            _hasTarget = true;
        }
    }
}
