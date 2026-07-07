using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Enemies
{
    /// <summary>
    /// Ranged enemy behavior (PLAN 4A): kites to a firing band around the
    /// player — fleeing when crowded, chasing when out of range, orbiting in
    /// between — and on a cycle stops, pulses a telegraph, then fires one
    /// pooled <see cref="EnemyProjectile"/> at the player. Zero-alloc state
    /// machine; pooled-safe (mirrors <see cref="ChargeAttack"/>).
    /// </summary>
    [RequireComponent(typeof(EnemyController), typeof(HealthComponent))]
    public sealed class RangedAttack : MonoBehaviour
    {
        private enum State { Reposition, Telegraph }

        [SerializeField] private EnemyController _enemyController;
        [SerializeField] private HealthComponent _health;
        // The rig's Body renderer, pulsed as the telegraph cue.
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private float _fleeRange = 4.5f;
        [SerializeField] private float _chaseRange = 7.5f;
        // Fraction of MoveSpeed used to orbit the player inside the band.
        [SerializeField] private float _strafeSpeedFraction = 0.6f;
        [SerializeField] private float _fireIntervalSeconds = 2.5f;
        [SerializeField] private float _telegraphSeconds = 0.6f;
        [SerializeField] private float _projectileSpeed = 7f;
        [SerializeField] private float _projectileRange = 10f;
        // Fraction of the run-scaled contact damage each shot carries.
        [SerializeField] private float _projectileDamageFraction = 1f;
        [SerializeField] private int _projectilePoolId = PoolIds.EnemyStinger;

        // Matches the hostile stinger recolor so the cue reads as "incoming shot".
        private static readonly Color TelegraphColor = new Color(1f, 0.4f, 0.75f);

        private State _state;
        private float _timer;
        private float _strafeSign;
        private Color _restColor;
        private bool _overrideArmed;

        private void OnEnable()
        {
            _state = State.Reposition;
            _timer = _fireIntervalSeconds;
            _strafeSign = Random.value < 0.5f ? -1f : 1f;
            _overrideArmed = false;
        }

        private void OnDisable()
        {
            // Pooled release mid-telegraph must not leave the override armed.
            _enemyController.ClearMovementOverride();
        }

        private void Update()
        {
            if (_health.IsDead)
            {
                if (_state == State.Telegraph)
                {
                    CancelTelegraph();
                }

                return;
            }

            _timer -= Time.deltaTime;

            switch (_state)
            {
                case State.Reposition:
                    if (_timer <= 0f && CanFire())
                    {
                        BeginTelegraph();
                    }

                    break;

                case State.Telegraph:
                    // Pulse toward the stinger's color so the shot is readable.
                    if (_renderer != null)
                    {
                        float pulse = Mathf.PingPong(Time.time * 6f, 1f);
                        _renderer.color = Color.Lerp(_restColor, TelegraphColor, pulse);
                    }

                    if (_timer <= 0f)
                    {
                        Fire();
                    }

                    break;
            }
        }

        private void FixedUpdate()
        {
            // Telegraph holds position via the zero override set on entry.
            if (_state != State.Reposition || _health.IsDead)
            {
                return;
            }

            Transform target = _enemyController.Target;
            if (target == null || _enemyController.Stats == null)
            {
                return;
            }

            Vector2 toTarget = target.position - transform.position;
            RangedSteerMode mode = RangedSteering.Decide(
                toTarget.sqrMagnitude, _fleeRange * _fleeRange, _chaseRange * _chaseRange);

            switch (mode)
            {
                case RangedSteerMode.Chase:
                    if (_overrideArmed)
                    {
                        _enemyController.ClearMovementOverride();
                        _overrideArmed = false;
                    }

                    break;

                case RangedSteerMode.Flee:
                    SetOverride(-toTarget.normalized * _enemyController.Stats.MoveSpeed);
                    break;

                case RangedSteerMode.Hold:
                    // Orbit so the band doesn't read as a frozen turret.
                    Vector2 tangent = new Vector2(-toTarget.y, toTarget.x).normalized * _strafeSign;
                    SetOverride(tangent * (_enemyController.Stats.MoveSpeed * _strafeSpeedFraction));
                    break;
            }
        }

        private void SetOverride(Vector2 velocity)
        {
            _enemyController.SetMovementOverride(velocity);
            _overrideArmed = true;
        }

        private bool CanFire()
        {
            if (_enemyController.StatusReceiver != null && _enemyController.StatusReceiver.IsAttackDisabled)
            {
                return false;
            }

            Transform target = _enemyController.Target;
            if (target == null)
            {
                return false;
            }

            // Only wind up when the shot can actually reach.
            float rangeSqr = _projectileRange * _projectileRange;
            return ((Vector2)(target.position - transform.position)).sqrMagnitude <= rangeSqr;
        }

        private void BeginTelegraph()
        {
            _state = State.Telegraph;
            _timer = _telegraphSeconds;
            _restColor = _renderer != null ? _renderer.color : Color.white;
            SetOverride(Vector2.zero);
        }

        private void CancelTelegraph()
        {
            _state = State.Reposition;
            _timer = _fireIntervalSeconds;

            if (_renderer != null)
            {
                _renderer.color = _restColor;
            }

            _enemyController.ClearMovementOverride();
            _overrideArmed = false;
        }

        private void Fire()
        {
            Transform target = _enemyController.Target;

            if (target != null
                && PoolManager.Instance != null
                && PoolManager.Instance.HasPool(_projectilePoolId))
            {
                Vector2 direction = ((Vector2)(target.position - transform.position)).normalized;
                float damage = _enemyController.ScaledContactDamage * _projectileDamageFraction;

                GameObject shot = PoolManager.Instance.Get(_projectilePoolId, transform.position, Quaternion.identity);
                if (shot.TryGetComponent(out EnemyProjectile projectile))
                {
                    projectile.Launch(direction, damage, _projectileSpeed, _projectileRange);
                }

                if (AudioService.Instance != null)
                {
                    AudioService.Instance.PlaySfx(SfxId.BossStinger);
                }
            }

            CancelTelegraph();
        }
    }
}
