using SurveHive.Core;
using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Combat
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private int _poolId;
        [SerializeField] private string _targetTag = "Enemy";
        [SerializeField] private float _knockbackImpulse = 2.5f;

        private Vector2 _direction;
        private float _damage;
        private float _speed;
        private float _remainingRange;
        private bool _released;

        public void Launch(Vector2 direction, float damage, float speed, float maxRange)
        {
            _direction = direction;
            _damage = damage;
            _speed = speed;
            _remainingRange = maxRange;
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

            if (other.TryGetComponent(out IDamageable damageable))
            {
                DamageService.DealDamage(damageable, other.transform.position, _damage, true, gameObject);
            }

            if (other.TryGetComponent(out Enemies.EnemyController enemy))
            {
                enemy.ApplyKnockback(_direction * _knockbackImpulse);
            }

            ReleaseSelf();
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
