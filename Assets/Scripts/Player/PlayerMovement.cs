using SurveHive.Input;
using UnityEngine;

namespace SurveHive.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        private Rigidbody2D _rigidbody;
        private IMovementInputSource _inputSource;
        private PlayerStats _stats;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        public void Initialize(IMovementInputSource inputSource, PlayerStats stats)
        {
            _inputSource = inputSource;
            _stats = stats;
        }

        private void FixedUpdate()
        {
            if (_inputSource == null || _stats == null)
            {
                return;
            }

            Vector2 direction = _inputSource.MoveDirection;
            _rigidbody.linearVelocity = direction * _stats.MoveSpeed;
        }
    }
}
