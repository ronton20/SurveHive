using SurveHive.Combat.Status;
using SurveHive.Core;
using SurveHive.Enemies;
using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Combat
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Projectile : MonoBehaviour
    {
        private const int MaxTracked = 16;

        [SerializeField] private int _poolId;
        [SerializeField] private string _targetTag = "Enemy";
        [SerializeField] private float _knockbackImpulse = 2.5f;

        private Vector2 _direction;
        private float _damage;
        private float _speed;
        private float _remainingRange;

        // Enhancement modifiers, copied from the payload and mutated in flight.
        private int _pierceRemaining;
        private float _burnChance, _burnDps, _burnDuration;
        private float _poisonChance, _poisonDps, _poisonDuration;
        private float _freezeChance, _freezeThreshold, _freezeDuration;
        private float _bounceChance, _bounceRange, _bounceDamageFalloff, _bounceChanceFalloff;
        private int _bounceCount;

        // Enemies already hit this flight (avoids re-damaging on a bounce back).
        private readonly EntityId[] _visited = new EntityId[MaxTracked];
        private int _visitedCount;
        private bool _released;

        public void Launch(Vector2 direction, float damage, float speed, float maxRange)
        {
            var payload = new BasicAttackPayload { Damage = damage, Speed = speed, Range = maxRange };
            Launch(direction, in payload);
        }

        public void Launch(Vector2 direction, in BasicAttackPayload payload)
        {
            _direction = direction;
            _damage = payload.Damage;
            _speed = payload.Speed;
            _remainingRange = payload.Range;

            _pierceRemaining = payload.Pierce;
            _burnChance = payload.BurnChance;
            _burnDps = payload.BurnDps;
            _burnDuration = payload.BurnDuration;
            _poisonChance = payload.PoisonChance;
            _poisonDps = payload.PoisonDps;
            _poisonDuration = payload.PoisonDuration;
            _freezeChance = payload.FreezeChance;
            _freezeThreshold = payload.FreezeThreshold;
            _freezeDuration = payload.FreezeDuration;
            _bounceChance = payload.BounceChance;
            _bounceCount = payload.BounceCount;
            _bounceRange = payload.BounceRange;
            _bounceDamageFalloff = payload.BounceDamageFalloff;
            _bounceChanceFalloff = payload.BounceChanceFalloff;

            _visitedCount = 0;

            // Point the sprite along the flight direction (art faces right).
            transform.rotation = Quaternion.FromToRotation(Vector3.right, direction);
        }

        private void OnEnable()
        {
            _released = false;
        }

        private void Update()
        {
            float step = _speed * Time.deltaTime;
            transform.position += (Vector3)(_direction * step);
            _remainingRange -= step;

            if (_remainingRange <= 0f)
            {
                ReleaseSelf();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(_targetTag))
            {
                return;
            }

            bool hasEnemy = other.TryGetComponent(out EnemyController enemy);
            EntityId id = hasEnemy ? enemy.GetEntityId() : other.GetEntityId();
            // A bounced shot can pass back over an enemy it already hit — never
            // damage the same target twice in one flight.
            if (IsVisited(id))
            {
                return;
            }

            AddVisited(id);

            if (other.TryGetComponent(out IDamageable damageable))
            {
                DamageService.DealDamage(damageable, other.transform.position, _damage, true, gameObject);
            }

            if (hasEnemy)
            {
                enemy.ApplyKnockback(_direction * _knockbackImpulse);
                ApplyStatuses(enemy);
            }

            // Pierce takes priority: pass straight through until the budget runs out.
            if (_pierceRemaining > 0)
            {
                _pierceRemaining--;
                return;
            }

            // Shock bounce: redirect toward a fresh nearby enemy with falloff.
            if (_bounceCount > 0 && hasEnemy && Random.value * 100f < _bounceChance &&
                TryBounce(enemy.transform.position))
            {
                return;
            }

            ReleaseSelf();
        }

        private void ApplyStatuses(EnemyController enemy)
        {
            StatusEffectReceiver receiver = enemy.StatusReceiver;
            if (receiver == null)
            {
                return;
            }

            if (_burnChance > 0f && Random.value * 100f < _burnChance)
            {
                receiver.ApplyEffect(StatusEffectType.Burn, _burnDps, _burnDuration);
            }

            if (_poisonChance > 0f && Random.value * 100f < _poisonChance)
            {
                receiver.ApplyEffect(StatusEffectType.Poison, _poisonDps, _poisonDuration);
            }

            if (_freezeChance > 0f && Random.value * 100f < _freezeChance)
            {
                receiver.ApplyEffect(StatusEffectType.Freeze, _freezeThreshold, _freezeDuration);
            }
        }

        // Redirects the shot toward the nearest un-hit enemy within bounce range,
        // applying the per-bounce damage/chance falloff. Returns false if none.
        private bool TryBounce(Vector3 fromPosition)
        {
            if (EnemyRegistry.Instance == null)
            {
                return false;
            }

            var enemies = EnemyRegistry.Instance.ActiveEnemies;
            float bestSqr = _bounceRange * _bounceRange;
            EnemyController best = null;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController candidate = enemies[i];
                if (candidate == null || candidate.Health == null || candidate.Health.IsDead)
                {
                    continue;
                }

                if (IsVisited(candidate.GetEntityId()))
                {
                    continue;
                }

                float sqr = (candidate.transform.position - fromPosition).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }

            if (best == null)
            {
                return false;
            }

            transform.position = fromPosition;
            _direction = ((Vector2)(best.transform.position - fromPosition)).normalized;
            transform.rotation = Quaternion.FromToRotation(Vector3.right, _direction);
            _damage *= _bounceDamageFalloff;
            _bounceChance *= _bounceChanceFalloff;
            _bounceCount--;
            // Fresh range so the redirected shot can reach the new target.
            _remainingRange = _bounceRange;
            return true;
        }

        private bool IsVisited(EntityId id)
        {
            for (int i = 0; i < _visitedCount; i++)
            {
                if (_visited[i] == id)
                {
                    return true;
                }
            }

            return false;
        }

        private void AddVisited(EntityId id)
        {
            if (_visitedCount < MaxTracked)
            {
                _visited[_visitedCount] = id;
                _visitedCount++;
            }
        }

        private void ReleaseSelf()
        {
            // A single physics step can overlap multiple colliders (or a hit and a
            // range expiry) before this instance is actually deactivated, so guard
            // against releasing the same pooled instance back more than once.
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
