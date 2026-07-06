using SurveHive.Core;
using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Enemies
{
    /// <summary>
    /// Pooled enemy-fired projectile (boss stinger bursts): flies straight,
    /// damages the player on contact, releases at range end. Player-side hit
    /// feedback (flash/shake) comes from the player's own damage handling.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private int _poolId;
        [SerializeField] private string _targetTag = "Player";
        // Boss stingers are physical; future casters can override per prefab.
        [SerializeField] private DamageType _damageType = DamageType.Physical;

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
            if (_released || !other.CompareTag(_targetTag))
            {
                return;
            }

            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(_damage, _damageType, gameObject);
            }

            ReleaseSelf();
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
