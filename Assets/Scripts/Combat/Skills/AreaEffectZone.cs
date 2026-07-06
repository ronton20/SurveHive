using SurveHive.Combat.Status;
using SurveHive.Core;
using SurveHive.Enemies;
using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Combat.Skills
{
    /// <summary>
    /// Pooled ground zone (honey puddle): periodically damages and applies a
    /// status to every live enemy inside its radius, then releases itself when
    /// its duration ends. Enemy lookup goes through the EnemyRegistry — no
    /// physics queries, no allocations.
    /// </summary>
    public sealed class AreaEffectZone : MonoBehaviour
    {
        [SerializeField] private int _poolId;
        [SerializeField] private SpriteRenderer _renderer;
        // World radius of the zone sprite at localScale 1 (16px @ PPU 16 = 1u).
        [SerializeField] private float _spriteBaseRadius = 1f;

        private float _radius;
        private float _remainingDuration;
        private float _tickInterval;
        private float _tickDamage;
        private DamageType _damageType;
        private float _tickTimer;
        private bool _appliesStatus;
        private StatusEffectType _statusType;
        private float _statusChancePercent;
        private float _statusPotency;
        private float _statusDuration;
        private bool _released;
        private Color _originalColor = Color.white;

        private void Awake()
        {
            if (_renderer != null)
            {
                _originalColor = _renderer.color;
            }
        }

        public void Configure(
            float radius, float duration, float tickInterval, float tickDamage,
            DamageType damageType, bool appliesStatus, StatusEffectType statusType,
            float statusChancePercent, float statusPotency, float statusDuration, Color tint)
        {
            if (_renderer != null)
            {
                // Alpha 0 = no tint → keep the prefab's own colour.
                _renderer.color = tint.a > 0f ? tint : _originalColor;
            }

            _radius = radius;
            _remainingDuration = duration;
            _tickInterval = Mathf.Max(0.1f, tickInterval);
            _tickDamage = tickDamage;
            _damageType = damageType;
            _tickTimer = _tickInterval * 0.5f;
            _appliesStatus = appliesStatus;
            _statusType = statusType;
            _statusChancePercent = statusChancePercent;
            _statusPotency = statusPotency;
            _statusDuration = statusDuration;
            _released = false;

            float scale = radius / Mathf.Max(0.01f, _spriteBaseRadius);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void OnEnable()
        {
            _released = false;
            // Poisoned default: an un-configured pooled instance dies instantly.
            _remainingDuration = 0.01f;
        }

        private void Update()
        {
            _remainingDuration -= Time.deltaTime;
            if (_remainingDuration <= 0f)
            {
                ReleaseSelf();
                return;
            }

            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f)
            {
                return;
            }

            _tickTimer += _tickInterval;
            DamageEnemiesInside();
        }

        private void DamageEnemiesInside()
        {
            if (EnemyRegistry.Instance == null)
            {
                return;
            }

            float sqrRadius = _radius * _radius;
            Vector3 center = transform.position;
            var enemies = EnemyRegistry.Instance.ActiveEnemies;

            // Reverse iteration: kills unregister enemies mid-loop.
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

                DamageService.DealDamage(enemy.Health, enemy.transform.position, _tickDamage, _damageType, false, gameObject);

                if (_appliesStatus && enemy.StatusReceiver != null &&
                    Random.value * 100f < _statusChancePercent)
                {
                    enemy.StatusReceiver.ApplyEffect(_statusType, _statusPotency, _statusDuration);
                }
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
