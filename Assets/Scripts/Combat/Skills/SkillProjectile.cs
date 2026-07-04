using SurveHive.Combat.Status;
using SurveHive.Core;
using SurveHive.Enemies;
using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Combat.Skills
{
    // Fire-time payload for a pooled skill projectile. Passed by value once per
    // shot — keeps the prefab generic and the hot path allocation-free.
    public struct SkillProjectileConfig
    {
        public Vector2 Direction;
        public float Speed;
        public float Damage;
        public float Range;
        // 0 = release on first hit; N = pass through N extra targets.
        public int PierceCount;
        public Transform HomingTarget;
        // > 0: AoE damage around the impact point.
        public float ExplodeRadius;
        public float KnockbackImpulse;
        public int ImpactVfxPoolId;

        public bool AppliesStatus;
        public StatusEffectType StatusType;
        public float StatusChancePercent;
        public float StatusPotency;
        public float StatusDuration;

        // Lobbed mode: fly to TargetPoint ignoring collisions, then drop a zone.
        public bool TravelToPoint;
        public Vector3 TargetPoint;
        public int ZonePoolId;
        public float ZoneRadius;
        public float ZoneDuration;
        public float ZoneTickInterval;
    }

    /// <summary>
    /// Generic pooled projectile for the active-skill arsenal: straight, piercing,
    /// homing-explosive, or lobbed-to-point (spawning an <see cref="AreaEffectZone"/>).
    /// All damage flows through <see cref="DamageService"/> (crit/lifesteal aware).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class SkillProjectile : MonoBehaviour
    {
        [SerializeField] private int _poolId;
        [SerializeField] private string _targetTag = "Enemy";
        // Degrees per second the homing variant can turn.
        [SerializeField] private float _homingTurnSpeed = 360f;

        private SkillProjectileConfig _config;
        private Vector2 _direction;
        private float _remainingRange;
        private bool _released;
        private Collider2D _lastHitCollider;

        private void Awake()
        {
            // Poison the default so an un-launched pooled instance self-releases.
            _remainingRange = 0f;
        }

        public void Launch(in SkillProjectileConfig config)
        {
            _config = config;
            _direction = config.Direction;
            _remainingRange = config.Range;
            _released = false;
            _lastHitCollider = null;
            transform.rotation = Quaternion.FromToRotation(Vector3.right, _direction);
        }

        private void OnEnable()
        {
            _released = false;
        }

        private void Update()
        {
            if (_config.TravelToPoint)
            {
                UpdateTravelToPoint();
                return;
            }

            if (_config.HomingTarget != null && _config.HomingTarget.gameObject.activeInHierarchy)
            {
                Vector2 toTarget = ((Vector2)(_config.HomingTarget.position - transform.position)).normalized;
                float maxRadians = _homingTurnSpeed * Mathf.Deg2Rad * Time.deltaTime;
                Vector3 rotated = Vector3.RotateTowards(_direction, toTarget, maxRadians, 0f);
                _direction = ((Vector2)rotated).normalized;
                transform.rotation = Quaternion.FromToRotation(Vector3.right, _direction);
            }

            float step = _config.Speed * Time.deltaTime;
            transform.position += (Vector3)(_direction * step);
            _remainingRange -= step;

            if (_remainingRange <= 0f)
            {
                // Explosive bolts still detonate at end-of-range for reliability.
                if (_config.ExplodeRadius > 0f)
                {
                    Explode(transform.position, null);
                }

                ReleaseSelf();
            }
        }

        private void UpdateTravelToPoint()
        {
            Vector3 toPoint = _config.TargetPoint - transform.position;
            float step = _config.Speed * Time.deltaTime;

            if (toPoint.sqrMagnitude <= step * step)
            {
                transform.position = _config.TargetPoint;
                SpawnZone();
                SpawnImpactVfx(_config.TargetPoint);
                ReleaseSelf();
                return;
            }

            transform.position += toPoint.normalized * step;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Lobbed globs sail over enemies to their landing point.
            if (_config.TravelToPoint || _released)
            {
                return;
            }

            if (!other.CompareTag(_targetTag) || other == _lastHitCollider)
            {
                return;
            }

            if (!other.TryGetComponent(out IDamageable damageable))
            {
                return;
            }

            _lastHitCollider = other;
            DamageService.DealDamage(damageable, other.transform.position, _config.Damage, true, gameObject);
            TryApplyStatus(other);

            if (_config.KnockbackImpulse > 0f && other.TryGetComponent(out EnemyController enemy))
            {
                enemy.ApplyKnockback(_direction * _config.KnockbackImpulse);
            }

            if (_config.ExplodeRadius > 0f)
            {
                Explode(transform.position, other);
                ReleaseSelf();
                return;
            }

            if (_config.PierceCount <= 0)
            {
                SpawnImpactVfx(transform.position);
                ReleaseSelf();
                return;
            }

            _config.PierceCount--;
        }

        // AoE around the impact: registry scan (no physics queries, no allocs).
        // directHit is excluded — it already took the full projectile damage.
        private void Explode(Vector3 center, Collider2D directHit)
        {
            SpawnImpactVfx(center);

            if (EnemyRegistry.Instance == null)
            {
                return;
            }

            float sqrRadius = _config.ExplodeRadius * _config.ExplodeRadius;
            var enemies = EnemyRegistry.Instance.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                {
                    continue;
                }

                if (directHit != null && enemy.transform == directHit.transform)
                {
                    continue;
                }

                if ((enemy.transform.position - center).sqrMagnitude > sqrRadius)
                {
                    continue;
                }

                DamageService.DealDamage(enemy.Health, enemy.transform.position, _config.Damage, true, gameObject);
                TryApplyStatusTo(enemy.StatusReceiver);
            }
        }

        private void SpawnZone()
        {
            if (_config.ZonePoolId < 0 || PoolManager.Instance == null)
            {
                return;
            }

            GameObject zoneObj = PoolManager.Instance.Get(_config.ZonePoolId, _config.TargetPoint, Quaternion.identity);
            if (zoneObj.TryGetComponent(out AreaEffectZone zone))
            {
                zone.Configure(
                    _config.ZoneRadius, _config.ZoneDuration, _config.ZoneTickInterval, _config.Damage,
                    _config.AppliesStatus, _config.StatusType, _config.StatusChancePercent,
                    _config.StatusPotency, _config.StatusDuration);
            }
        }

        private void TryApplyStatus(Collider2D other)
        {
            if (other.TryGetComponent(out StatusEffectReceiver receiver))
            {
                TryApplyStatusTo(receiver);
            }
        }

        private void TryApplyStatusTo(StatusEffectReceiver receiver)
        {
            if (!_config.AppliesStatus || receiver == null)
            {
                return;
            }

            if (Random.value * 100f < _config.StatusChancePercent)
            {
                receiver.ApplyEffect(_config.StatusType, _config.StatusPotency, _config.StatusDuration);
            }
        }

        private void SpawnImpactVfx(Vector3 position)
        {
            if (_config.ImpactVfxPoolId < 0 || PoolManager.Instance == null)
            {
                return;
            }

            PoolManager.Instance.Get(_config.ImpactVfxPoolId, position, Quaternion.identity);
        }

        private void ReleaseSelf()
        {
            if (_released)
            {
                return;
            }

            _released = true;

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Release(_poolId, gameObject);
            }
        }
    }
}
