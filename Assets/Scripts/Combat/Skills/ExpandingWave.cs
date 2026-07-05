using SurveHive.Combat.Status;
using SurveHive.Core;
using SurveHive.Enemies;
using UnityEngine;

namespace SurveHive.Combat.Skills
{
    /// <summary>
    /// Frost Nova (Combat 2.0 1E): a ring that expands outward from the player,
    /// applying its effect to each enemy exactly once as the wave front reaches
    /// it. Primarily a crowd-control tool — low damage, guaranteed slow. Enemy
    /// lookup is a registry distance scan (no physics), hits tracked in a fixed
    /// buffer (zero-GC).
    /// </summary>
    public sealed class ExpandingWave : MonoBehaviour
    {
        private const int MaxTracked = 96;

        [SerializeField] private int _poolId;
        [SerializeField] private SpriteRenderer _renderer;
        // World radius of the sprite at localScale 1.
        [SerializeField] private float _spriteBaseRadius = 1f;

        private float _startRadius;
        private float _maxRadius;
        private float _expandDuration;
        private float _elapsed;
        private float _currentRadius;
        private float _damage;
        private bool _appliesStatus;
        private StatusEffectType _statusType;
        private float _statusPotency;
        private float _statusDuration;

        private readonly EntityId[] _hit = new EntityId[MaxTracked];
        private int _hitCount;
        private Color _originalColor = Color.white;
        private bool _released;

        private void Awake()
        {
            if (_renderer != null)
            {
                _originalColor = _renderer.color;
            }
        }

        public void Configure(
            float startRadius, float maxRadius, float expandDuration, float damage,
            bool appliesStatus, StatusEffectType statusType, float statusPotency, float statusDuration, Color tint)
        {
            _startRadius = Mathf.Max(0f, startRadius);
            _maxRadius = Mathf.Max(_startRadius, maxRadius);
            _expandDuration = Mathf.Max(0.05f, expandDuration);
            _elapsed = 0f;
            _currentRadius = _startRadius;
            _damage = damage;
            _appliesStatus = appliesStatus;
            _statusType = statusType;
            _statusPotency = statusPotency;
            _statusDuration = statusDuration;
            _hitCount = 0;
            _released = false;

            if (_renderer != null)
            {
                _renderer.color = tint.a > 0f ? tint : _originalColor;
            }

            ApplyScale();
        }

        private void OnEnable()
        {
            _released = false;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _expandDuration);
            _currentRadius = Mathf.Lerp(_startRadius, _maxRadius, t);
            ApplyScale();
            HitNewEnemies();

            if (_elapsed >= _expandDuration)
            {
                ReleaseSelf();
            }
        }

        private void ApplyScale()
        {
            if (_renderer == null)
            {
                return;
            }

            float scale = _currentRadius / Mathf.Max(0.01f, _spriteBaseRadius);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        // Applies the effect to enemies the wave front has newly reached.
        private void HitNewEnemies()
        {
            if (EnemyRegistry.Instance == null)
            {
                return;
            }

            float sqrRadius = _currentRadius * _currentRadius;
            Vector3 center = transform.position;
            var enemies = EnemyRegistry.Instance.ActiveEnemies;

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                {
                    continue;
                }

                if ((enemy.transform.position - center).sqrMagnitude > sqrRadius)
                {
                    continue;
                }

                EntityId id = enemy.GetEntityId();
                if (IsHit(id))
                {
                    continue;
                }

                MarkHit(id);
                DamageService.DealDamage(enemy.Health, enemy.transform.position, _damage, false, gameObject, false);

                if (_appliesStatus && enemy.StatusReceiver != null)
                {
                    enemy.StatusReceiver.ApplyEffect(_statusType, _statusPotency, _statusDuration);
                }
            }
        }

        private bool IsHit(EntityId id)
        {
            for (int i = 0; i < _hitCount; i++)
            {
                if (_hit[i] == id)
                {
                    return true;
                }
            }

            return false;
        }

        private void MarkHit(EntityId id)
        {
            if (_hitCount < MaxTracked)
            {
                _hit[_hitCount] = id;
                _hitCount++;
            }
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
