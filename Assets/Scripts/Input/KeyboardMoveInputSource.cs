using UnityEngine;
using UnityEngine.InputSystem;

namespace SurveHive.Input
{
    public sealed class KeyboardMoveInputSource : IMovementInputSource
    {
        private readonly InputAction _moveAction;

        public KeyboardMoveInputSource(InputAction moveAction)
        {
            _moveAction = moveAction;
        }

        public Vector2 MoveDirection => _moveAction.ReadValue<Vector2>();
    }
}
