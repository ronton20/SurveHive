using UnityEngine;

namespace SurveHive.Input
{
    public interface IMovementInputSource
    {
        Vector2 MoveDirection { get; }
    }
}
