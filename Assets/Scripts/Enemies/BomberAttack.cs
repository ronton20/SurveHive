using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Enemies
{
    /// <summary>
    /// Suicide bomber behavior (PLAN 4B, reworked): a fast bee that chases the
    /// player and — once it closes to <see cref="_fuseTriggerRange"/> — commits to
    /// a countdown while *still chasing*, flashing an angry red glow and beeping on
    /// an accelerating cadence. After <see cref="_fuseSeconds"/> it detonates in an
    /// AoE wherever it ended up. Dying for any reason also triggers the blast, so
    /// point-blank kills stay dangerous and ranged kills are the counter-play.
    /// Zero-alloc; pooled-safe.
    /// </summary>
    [RequireComponent(typeof(EnemyController), typeof(HealthComponent))]
    public sealed class BomberAttack : MonoBehaviour
    {
        private enum State { Rush, Countdown }

        [SerializeField] private EnemyController _enemyController;
        [SerializeField] private HealthComponent _health;
        // The rig's Body renderer, flashed red as the countdown cue.
        [SerializeField] private SpriteRenderer _renderer;
        // Distance at which the (committed) countdown lights.
        [SerializeField] private float _fuseTriggerRange = 3f;
        [SerializeField] private float _fuseSeconds = 3f;
        [SerializeField] private float _blastRadius = 2.8f;
        // Multiplier on the run-scaled contact damage.
        [SerializeField] private float _blastDamageMultiplier = 2.5f;
        [SerializeField] private int _blastVfxPoolId = PoolIds.BomberBlastVfx;
        // Beep cadence tightens from max (countdown start) to min (about to blow).
        [SerializeField] private float _maxBeepInterval = 0.5f;
        [SerializeField] private float _minBeepInterval = 0.12f;

        // HDR red so the URP Bloom pass makes the countdown glow (Phase 6C).
        private static readonly Color GlowColor = new Color(2.4f, 0.12f, 0.12f, 1f);

        private State _state;
        private float _timer;
        private float _beepTimer;
        private Color _restColor;
        private bool _exploded;

        private void OnEnable()
        {
            _state = State.Rush;
            _exploded = false;
            _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            _health.OnDied -= HandleDied;
            // Defensive: a rework kept no movement override, but a pooled release
            // mid-countdown must never leave one armed for the next spawn.
            _enemyController.ClearMovementOverride();
        }

        private void Update()
        {
            // Death mid-countdown explodes through HandleDied, not the timer.
            if (_health.IsDead || _exploded)
            {
                return;
            }

            switch (_state)
            {
                case State.Rush:
                    Transform target = _enemyController.Target;
                    if (target != null
                        && ((Vector2)(target.position - transform.position)).sqrMagnitude
                            <= _fuseTriggerRange * _fuseTriggerRange)
                    {
                        BeginCountdown();
                    }

                    break;

                case State.Countdown:
                    // Committed: keep chasing (no movement override) so it tries to
                    // stay on the player — the counter-play is to juke or kill it.
                    TickCountdown();
                    break;
            }
        }

        private void BeginCountdown()
        {
            _state = State.Countdown;
            _timer = _fuseSeconds;
            _beepTimer = 0f;
            _restColor = _renderer != null ? _renderer.color : Color.white;
        }

        private void TickCountdown()
        {
            // Stuns/freezes hold the countdown (and the beeping) instead of defusing.
            bool held = _enemyController.StatusReceiver != null
                && _enemyController.StatusReceiver.IsAttackDisabled;
            if (held)
            {
                return;
            }

            _timer -= Time.deltaTime;

            // Urgency ramps as the timer drains: faster flash, faster beeps.
            float urgency = 1f - Mathf.Clamp01(_timer / Mathf.Max(0.01f, _fuseSeconds));

            if (_renderer != null)
            {
                float flashRate = Mathf.Lerp(6f, 22f, urgency);
                float pulse = Mathf.PingPong(Time.time * flashRate, 1f);
                _renderer.color = Color.Lerp(_restColor, GlowColor, pulse);
            }

            _beepTimer -= Time.deltaTime;
            if (_beepTimer <= 0f)
            {
                if (AudioService.Instance != null)
                {
                    AudioService.Instance.PlaySfx(SfxId.UIClick);
                }

                _beepTimer = Mathf.Lerp(_maxBeepInterval, _minBeepInterval, urgency);
            }

            if (_timer <= 0f)
            {
                Explode();
            }
        }

        private void HandleDied()
        {
            Explode();
        }

        private void Explode()
        {
            if (_exploded)
            {
                return;
            }

            _exploded = true;

            Transform target = _enemyController.Target;
            if (target != null
                && ((Vector2)(target.position - transform.position)).sqrMagnitude
                    <= _blastRadius * _blastRadius
                && target.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(
                    _enemyController.ScaledContactDamage * _blastDamageMultiplier,
                    DamageType.Physical, gameObject);
            }

            if (PoolManager.Instance != null && PoolManager.Instance.HasPool(_blastVfxPoolId))
            {
                PoolManager.Instance.Get(_blastVfxPoolId, transform.position, Quaternion.identity);
            }

            if (AudioService.Instance != null)
            {
                AudioService.Instance.PlaySfx(SfxId.SkillEmberSting);
            }

            // _restColor is only captured when the countdown lit; a death during
            // Rush must not stamp an unset (clear) color on the corpse.
            if (_renderer != null && _state == State.Countdown)
            {
                _renderer.color = _restColor;
            }

            _enemyController.ClearMovementOverride();

            // Blast consumes the bomber: no shields/armor on this rank, so
            // CurrentHealth kills exactly (skipped when a kill triggered us).
            if (!_health.IsDead)
            {
                _health.TakeDamage(_health.CurrentHealth, DamageType.Physical, gameObject);
            }
        }
    }
}
