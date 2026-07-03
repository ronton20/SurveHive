using UnityEngine;

namespace SurveHive.View
{
    /// <summary>
    /// Drives the shared PixelFantasy monster Animator (Idle/movement bools,
    /// Hit/Attack triggers) from rigidbody motion, and flips the visual root
    /// horizontally to face the move direction (the pack's sprites face right).
    /// </summary>
    public sealed class CharacterAnimator : MonoBehaviour
    {
        private static readonly int IdleParam = Animator.StringToHash("Idle");
        private static readonly int WalkParam = Animator.StringToHash("Walk");
        private static readonly int RunParam = Animator.StringToHash("Run");
        private static readonly int MoveParam = Animator.StringToHash("Move");
        private static readonly int HitParam = Animator.StringToHash("Hit");
        private static readonly int AttackParam = Animator.StringToHash("Attack");
        private static readonly int DieParam = Animator.StringToHash("Die");

        [SerializeField] private Animator _animator;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private float _movingSpeedThreshold = 0.05f;
        // How long an explicit FaceDirection call (e.g. firing at a target) wins
        // over movement-based facing.
        [SerializeField] private float _faceOverrideDuration = 0.15f;

        private float _faceOverrideRemaining;
        private float _faceOverrideSign;
        private bool _isMoving;

        private void OnEnable()
        {
            // Pooled instances must not inherit animation state from a previous life.
            _isMoving = false;
            _faceOverrideRemaining = 0f;
            _animator.SetBool(DieParam, false);
            _animator.SetBool(IdleParam, true);
            SetMovementBools(false);
        }

        private void Update()
        {
            Vector2 velocity = _rigidbody.linearVelocity;
            bool moving = velocity.sqrMagnitude > _movingSpeedThreshold * _movingSpeedThreshold;
            if (moving != _isMoving)
            {
                _isMoving = moving;
                _animator.SetBool(IdleParam, !moving);
                SetMovementBools(moving);
            }

            if (_faceOverrideRemaining > 0f)
            {
                _faceOverrideRemaining -= Time.deltaTime;
                ApplyFacing(_faceOverrideSign);
            }
            else if (moving && Mathf.Abs(velocity.x) > 0.01f)
            {
                ApplyFacing(Mathf.Sign(velocity.x));
            }
        }

        public void FaceDirection(float xDirection)
        {
            if (Mathf.Abs(xDirection) < 0.01f)
            {
                return;
            }

            _faceOverrideSign = Mathf.Sign(xDirection);
            _faceOverrideRemaining = _faceOverrideDuration;
            ApplyFacing(_faceOverrideSign);
        }

        public void PlayAttack()
        {
            _animator.SetTrigger(AttackParam);
        }

        public void PlayHit()
        {
            _animator.SetTrigger(HitParam);
        }

        // The shared controller has separate Walk/Run/Move bools whose usage varies
        // per state machine revision; setting all three keeps every variant walking.
        private void SetMovementBools(bool moving)
        {
            _animator.SetBool(WalkParam, moving);
            _animator.SetBool(RunParam, moving);
            _animator.SetBool(MoveParam, moving);
        }

        private void ApplyFacing(float sign)
        {
            Vector3 scale = _visualRoot.localScale;
            scale.x = sign * Mathf.Abs(scale.x);
            _visualRoot.localScale = scale;
        }
    }
}
