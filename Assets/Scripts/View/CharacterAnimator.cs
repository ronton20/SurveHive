using UnityEngine;

namespace SurveHive.View
{
    /// <summary>
    /// Drives the shared PixelFantasy monster Animator by playing its states
    /// directly (Idle / Runing / Attack). The pack controller's AnyState
    /// transitions re-fire every frame while their bool conditions hold, which
    /// restarts the clip and freezes it on frame 0 — so bools are bypassed
    /// entirely. The attack animation takes priority over movement states (death
    /// aside) and its playback speed scales with the attack-speed stat. Also
    /// flips the visual root to face the move direction (pack art faces right).
    /// </summary>
    public sealed class CharacterAnimator : MonoBehaviour
    {
        // "Runing" is the pack's own state name (sic).
        private static readonly int IdleState = Animator.StringToHash("Idle");
        private static readonly int RunState = Animator.StringToHash("Runing");
        private static readonly int AttackState = Animator.StringToHash("Attack");
        private static readonly int HitParam = Animator.StringToHash("Hit");

        [SerializeField] private Animator _animator;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private float _movingSpeedThreshold = 0.05f;
        // How long an explicit FaceDirection call (e.g. firing at a target) wins
        // over movement-based facing.
        [SerializeField] private float _faceOverrideDuration = 0.15f;
        // Attack clip speed = 1 + (attackSpeed - 1) * factor, clamped to max —
        // faster attack stat reads as snappier swings without becoming a blur.
        [SerializeField] private float _attackAnimSpeedFactor = 0.5f;
        [SerializeField] private float _maxAttackAnimSpeed = 2.5f;

        private float _faceOverrideRemaining;
        private float _faceOverrideSign;
        private bool _isMoving;
        private bool _isAttacking;
        private float _attackElapsed;
        private bool _isDead;

        private void OnEnable()
        {
            // Pooled instances must not inherit animation state from a previous life.
            _isMoving = false;
            _isAttacking = false;
            _isDead = false;
            _faceOverrideRemaining = 0f;
            _animator.speed = 1f;
            _animator.Play(IdleState, 0, 0f);
        }

        private void Update()
        {
            if (_isDead)
            {
                return;
            }

            Vector2 velocity = _rigidbody.linearVelocity;
            bool moving = velocity.sqrMagnitude > _movingSpeedThreshold * _movingSpeedThreshold;

            if (_isAttacking)
            {
                _attackElapsed += Time.deltaTime;
                if (IsAttackFinished())
                {
                    _isAttacking = false;
                    _animator.speed = 1f;
                    _isMoving = moving;
                    _animator.Play(moving ? RunState : IdleState, 0, 0f);
                }
            }
            else if (!_isDead && moving != _isMoving)
            {
                _isMoving = moving;
                _animator.Play(moving ? RunState : IdleState, 0, 0f);
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

        /// <summary>
        /// Plays the attack clip from the start, overriding whatever is playing
        /// (except death). Playback speed scales with the attack-speed stat.
        /// </summary>
        public void PlayAttack(float attackSpeedMultiplier)
        {
            if (_isDead)
            {
                return;
            }

            float speed = 1f + ((attackSpeedMultiplier - 1f) * _attackAnimSpeedFactor);
            _animator.speed = Mathf.Clamp(speed, 0.75f, _maxAttackAnimSpeed);
            _animator.Play(AttackState, 0, 0f);
            _isAttacking = true;
            _attackElapsed = 0f;
        }

        public void PlayHit()
        {
            // The attack clip owns the body while it plays; the Hit trigger only
            // feeds the controller's FX layer overlay, so it never interrupts it.
            _animator.SetTrigger(HitParam);
        }

        public void SetDead()
        {
            _isDead = true;
            _isAttacking = false;
            _animator.speed = 1f;
        }

        private bool IsAttackFinished()
        {
            // Animator.Play only takes effect on the next animator update, so an
            // immediate state query can still report the previous state — treat
            // the first moments after PlayAttack as still-attacking.
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == AttackState)
            {
                return !_animator.IsInTransition(0) && state.normalizedTime >= 1f;
            }

            return _attackElapsed > 0.1f;
        }

        private void ApplyFacing(float sign)
        {
            Vector3 scale = _visualRoot.localScale;
            scale.x = sign * Mathf.Abs(scale.x);
            _visualRoot.localScale = scale;
        }
    }
}
