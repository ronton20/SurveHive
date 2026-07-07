using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Enemies
{
    /// <summary>
    /// Suicide bomber behavior (PLAN 4B): rushes the player (the controller's
    /// default chase), and when close enough stops and lights a fuse — a fast
    /// orange pulse — then explodes in an AoE. Dying for any reason also
    /// triggers the blast, so point-blank kills stay dangerous and ranged
    /// kills are the counter-play. Zero-alloc; pooled-safe.
    /// </summary>
    [RequireComponent(typeof(EnemyController), typeof(HealthComponent))]
    public sealed class BomberAttack : MonoBehaviour
    {
        private enum State { Rush, Fuse }

        [SerializeField] private EnemyController _enemyController;
        [SerializeField] private HealthComponent _health;
        // The rig's Body renderer, pulsed as the fuse cue.
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private float _fuseTriggerRange = 1.6f;
        [SerializeField] private float _fuseSeconds = 0.55f;
        [SerializeField] private float _blastRadius = 2.2f;
        // Multiplier on the run-scaled contact damage.
        [SerializeField] private float _blastDamageMultiplier = 2.5f;
        [SerializeField] private int _blastVfxPoolId = PoolIds.BomberBlastVfx;

        private static readonly Color FuseColor = new Color(1f, 0.55f, 0.15f);

        private State _state;
        private float _timer;
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
            // Pooled release mid-fuse must not leave the override armed.
            _enemyController.ClearMovementOverride();
        }

        private void Update()
        {
            // Death mid-fuse explodes through HandleDied, not the timer.
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
                        BeginFuse();
                    }

                    break;

                case State.Fuse:
                    // Faster pulse than a telegraph — this one is a countdown.
                    if (_renderer != null)
                    {
                        float pulse = Mathf.PingPong(Time.time * 10f, 1f);
                        _renderer.color = Color.Lerp(_restColor, FuseColor, pulse);
                    }

                    // Stuns/freezes hold the fuse instead of defusing it.
                    bool held = _enemyController.StatusReceiver != null
                        && _enemyController.StatusReceiver.IsAttackDisabled;
                    if (!held)
                    {
                        _timer -= Time.deltaTime;
                    }

                    if (_timer <= 0f)
                    {
                        Explode();
                    }

                    break;
            }
        }

        private void BeginFuse()
        {
            _state = State.Fuse;
            _timer = _fuseSeconds;
            _restColor = _renderer != null ? _renderer.color : Color.white;
            _enemyController.SetMovementOverride(Vector2.zero);
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

            // _restColor is only captured when the fuse lit; a death during
            // Rush must not stamp an unset (clear) color on the corpse.
            if (_renderer != null && _state == State.Fuse)
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
