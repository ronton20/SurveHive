using UnityEngine;
using UnityEngine.InputSystem;

namespace SurveHive.Input
{
    /// <summary>
    /// Selects the movement source by platform: floating touch joystick on
    /// mobile, keyboard (WASD/arrows) everywhere else. The joystick touch zone
    /// is fully disabled on non-touch platforms so no joystick UI ever shows
    /// on PC.
    /// </summary>
    public sealed class PlayerInputController : MonoBehaviour, IMovementInputSource
    {
        [SerializeField] private InputActionAsset _actionsAsset;
        [SerializeField] private InputSourceMode _modeOverride = InputSourceMode.Auto;
        [SerializeField] private OnScreenJoystickUI _joystickUi;

        private KeyboardMoveInputSource _keyboardSource;
        private OnScreenJoystickInputSource _joystickSource;
        private InputSourceMode _resolvedMode;

        public Vector2 MoveDirection { get; private set; }

        private void Awake()
        {
            InputActionMap gameplayMap = _actionsAsset.FindActionMap("Gameplay", throwIfNotFound: true);
            InputAction moveAction = gameplayMap.FindAction("Move", throwIfNotFound: true);

            _resolvedMode = _modeOverride == InputSourceMode.Auto ? ResolvePlatformDefault() : _modeOverride;

            _keyboardSource = new KeyboardMoveInputSource(moveAction);

            bool useJoystick = _resolvedMode == InputSourceMode.Touch && _joystickUi != null;
            if (useJoystick)
            {
                _joystickSource = new OnScreenJoystickInputSource(_joystickUi);
            }

            if (_joystickUi != null)
            {
                _joystickUi.gameObject.SetActive(useJoystick);
            }

            gameplayMap.Enable();
        }

        private void Update()
        {
            MoveDirection = _joystickSource != null
                ? _joystickSource.MoveDirection
                : _keyboardSource.MoveDirection;
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
