using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Enemies
{
    /// <summary>
    /// Telegraphed charge: the enemy stops, pulses red for the telegraph window,
    /// then dashes in the locked direction at a speed multiple. Runs on a cycle
    /// by itself (miniboss) or on demand via <see cref="TriggerCharge"/> (Queen
    /// pattern). Zero-alloc state machine; pooled-safe.
    /// </summary>
    [RequireComponent(typeof(EnemyController), typeof(HealthComponent))]
    public sealed class ChargeAttack : MonoBehaviour
    {
        private enum State { Idle, Telegraph, Charging }

        [SerializeField] private EnemyController _enemyController;
        [SerializeField] private HealthComponent _health;
        // The rig's Body renderer, pulsed as the telegraph cue.
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private bool _autoRun = true;
        [SerializeField] private float _intervalSeconds = 5f;
        [SerializeField] private float _telegraphSeconds = 0.9f;
        [SerializeField] private float _chargeSeconds = 0.7f;
        [SerializeField] private float _chargeSpeedMultiplier = 5f;

        private static readonly Color TelegraphColor = new Color(1f, 0.25f, 0.2f);

        private State _state;
        private float _timer;
        private Vector2 _chargeDirection;
        private Color _restColor;

        public bool IsBusy => _state != State.Idle;

        private void OnEnable()
        {
            _state = State.Idle;
            _timer = _intervalSeconds;
        }

        private void OnDisable()
        {
            // Pooled release mid-charge must not leave the override armed.
            _enemyController.ClearMovementOverride();
        }

        /// <summary>Starts a charge cycle now (used by boss pattern controllers).</summary>
        public void TriggerCharge()
        {
            if (_state == State.Idle && !_health.IsDead)
            {
                BeginTelegraph();
            }
        }

        private void Update()
        {
            if (_health.IsDead)
            {
                if (_state != State.Idle)
                {
                    EndCharge();
                }

                return;
            }

            _timer -= Time.deltaTime;

            switch (_state)
            {
                case State.Idle:
                    if (_autoRun && _timer <= 0f)
                    {
                        BeginTelegraph();
                    }

                    break;

                case State.Telegraph:
                    // Pulse toward red so the dash is readable before it hits.
                    if (_renderer != null)
                    {
                        float pulse = Mathf.PingPong(Time.time * 6f, 1f);
                        _renderer.color = Color.Lerp(_restColor, TelegraphColor, pulse);
                    }

                    if (_timer <= 0f)
                    {
                        BeginCharge();
                    }

                    break;

                case State.Charging:
                    if (_timer <= 0f)
                    {
                        EndCharge();
                    }

                    break;
            }
        }

        private void BeginTelegraph()
        {
            _state = State.Telegraph;
            _timer = _telegraphSeconds;
            _restColor = _renderer != null ? _renderer.color : Color.white;
            _enemyController.SetMovementOverride(Vector2.zero);
        }

        private void BeginCharge()
        {
            _state = State.Charging;
            _timer = _chargeSeconds;

            if (_renderer != null)
            {
                _renderer.color = _restColor;
            }

            Transform target = _enemyController.Target;
            _chargeDirection = target != null
                ? ((Vector2)(target.position - transform.position)).normalized
                : Vector2.right;

            float speed = _enemyController.Stats != null
                ? _enemyController.Stats.MoveSpeed * _chargeSpeedMultiplier
                : _chargeSpeedMultiplier;
            _enemyController.SetMovementOverride(_chargeDirection * speed);
        }

        private void EndCharge()
        {
            _state = State.Idle;
            _timer = _intervalSeconds;
            _enemyController.ClearMovementOverride();

            if (_renderer != null)
            {
                _renderer.color = _restColor;
            }
        }
    }
}
