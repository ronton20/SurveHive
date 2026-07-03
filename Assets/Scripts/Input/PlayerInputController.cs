using UnityEngine;
using UnityEngine.InputSystem;

namespace SurveHive.Input
{
    public sealed class PlayerInputController : MonoBehaviour, IMovementInputSource
    {
        private const float KeyboardDeadzoneSqr = 0.0001f;

        [SerializeField] private InputActionAsset _actionsAsset;
        [SerializeField] private InputSourceMode _modeOverride = InputSourceMode.Auto;
        [SerializeField] private OnScreenJoystickUI _joystickUi;
        [SerializeField] private Camera _worldCamera;

        private InputAction _moveAction;
        private InputAction _pointerPositionAction;
        private InputAction _pointerClickAction;

        private KeyboardMoveInputSource _keyboardSource;
        private ClickToMoveInputSource _clickToMoveSource;
        private OnScreenJoystickInputSource _joystickSource;

        private InputSourceMode _resolvedMode;

        public Vector2 MoveDirection { get; private set; }

        private void Awake()
        {
            InputActionMap gameplayMap = _actionsAsset.FindActionMap("Gameplay", throwIfNotFound: true);
            _moveAction = gameplayMap.FindAction("Move", throwIfNotFound: true);
            _pointerPositionAction = gameplayMap.FindAction("PointerPosition", throwIfNotFound: true);
            _pointerClickAction = gameplayMap.FindAction("PointerClick", throwIfNotFound: true);

            _resolvedMode = _modeOverride == InputSourceMode.Auto ? ResolvePlatformDefault() : _modeOverride;

            _keyboardSource = new KeyboardMoveInputSource(_moveAction);
            _clickToMoveSource = new ClickToMoveInputSource(_pointerPositionAction, _pointerClickAction, _worldCamera, transform);

            if (_joystickUi != null)
            {
                _joystickSource = new OnScreenJoystickInputSource(_joystickUi);
            }

            gameplayMap.Enable();
        }

        private void Update()
        {
            _clickToMoveSource.Tick();

            if (_resolvedMode == InputSourceMode.Touch)
            {
                MoveDirection = _joystickSource != null ? _joystickSource.MoveDirection : Vector2.zero;
                return;
            }

            Vector2 keyboardDirection = _keyboardSource.MoveDirection;
            MoveDirection = keyboardDirection.sqrMagnitude > KeyboardDeadzoneSqr ? keyboardDirection : _clickToMoveSource.MoveDirection;
        }

        private void OnDestroy()
        {
            _clickToMoveSource.Dispose();
        }

        private static InputSourceMode ResolvePlatformDefault()
        {
#if UNITY_ANDROID || UNITY_IOS
            return InputSourceMode.Touch;
#else
            return InputSourceMode.KeyboardMouse;
#endif
        }
    }
}
