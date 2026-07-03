using UnityEngine;

namespace SurveHive.Input
{
    public sealed class OnScreenJoystickInputSource : IMovementInputSource
    {
        private readonly OnScreenJoystickUI _joystick;

        public OnScreenJoystickInputSource(OnScreenJoystickUI joystick)
        {
            _joystick = joystick;
        }

        public Vector2 MoveDirection => _joystick != null ? _joystick.CurrentDirection : Vector2.zero;
    }
}
