using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Enemies
{
    [RequireComponent(typeof(Rigidbody2D), typeof(HealthComponent), typeof(DamageOnContact))]
    public sealed class EnemyController : MonoBehaviour
    {
        private Rigidbody2D _rigidbody;
        private HealthComponent _health;
        private DamageOnContact _damageOnContact;
        private SpriteRenderer _spriteRenderer;

        private EnemyStatsSO _stats;
        private Transform _playerTransform;

        public EnemyStatsSO Stats => _stats;

        public HealthComponent Health => _health;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _health = GetComponent<HealthComponent>();
            _damageOnContact = GetComponent<DamageOnContact>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (EnemyRegistry.Instance != null)
            {
                EnemyRegistry.Instance.Register(this);
            }
        }

        private void OnDisable()
        {
            if (EnemyRegistry.Instance != null)
            {
                EnemyRegistry.Instance.Unregister(this);
            }
        }

        public void Initialize(EnemyStatsSO stats, Transform playerTransform, float healthMultiplier, float damageMultiplier)
        {
            _stats = stats;
            _playerTransform = playerTransform;
            _health.Initialize(stats.MaxHealth * healthMultiplier);
            _damageOnContact.Configure(stats.ContactDamage * damageMultiplier, stats.ContactDamageInterval);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = stats.SpriteTint;
            }
        }

        private void FixedUpdate()
        {
            if (_playerTransform == null || _stats == null)
            {
                return;
            }

            Vector2 direction = ((Vector2)(_playerTransform.position - transform.position)).normalized;
            _rigidbody.linearVelocity = direction * _stats.MoveSpeed;
        }
    }
}
