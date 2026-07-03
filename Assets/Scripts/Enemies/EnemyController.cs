using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Health;
using SurveHive.View;
using UnityEngine;

namespace SurveHive.Enemies
{
    [RequireComponent(typeof(Rigidbody2D), typeof(HealthComponent), typeof(DamageOnContact))]
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField] private CharacterAnimator _characterAnimator;
        [SerializeField] private float _knockbackDecayPerSecond = 14f;

        private Rigidbody2D _rigidbody;
        private HealthComponent _health;
        private DamageOnContact _damageOnContact;
        private SpriteRenderer _spriteRenderer;

        private EnemyStatsSO _stats;
        private Transform _playerTransform;
        private Vector2 _knockbackVelocity;

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
            _knockbackVelocity = Vector2.zero;
            _health.OnDamaged += HandleDamaged;
            _health.OnDied += HandleDied;

            if (EnemyRegistry.Instance != null)
            {
                EnemyRegistry.Instance.Register(this);
            }
        }

        private void OnDisable()
        {
            _health.OnDamaged -= HandleDamaged;
            _health.OnDied -= HandleDied;

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
            transform.localScale = Vector3.one * stats.Scale;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = stats.SpriteTint;
            }
        }

        public void ApplyKnockback(Vector2 impulse)
        {
            if (_stats == null)
            {
                return;
            }

            _knockbackVelocity += impulse / Mathf.Max(0.1f, _stats.KnockbackResistance);
        }

        private void HandleDamaged(float amount)
        {
            if (_characterAnimator != null)
            {
                _characterAnimator.PlayHit();
            }
        }

        // Corpses leave the registry immediately so auto-targeting and the
        // spawner's concurrent-enemy cap ignore them while the death animation
        // plays out (Unregister is a no-op when called again from OnDisable).
        private void HandleDied()
        {
            if (_characterAnimator != null)
            {
                _characterAnimator.SetDead();
            }

            if (EnemyRegistry.Instance != null)
            {
                EnemyRegistry.Instance.Unregister(this);
            }
        }

        private void FixedUpdate()
        {
            if (_playerTransform == null || _stats == null || _health.IsDead)
            {
                return;
            }

            Vector2 direction = ((Vector2)(_playerTransform.position - transform.position)).normalized;
            _rigidbody.linearVelocity = (direction * _stats.MoveSpeed) + _knockbackVelocity;

            _knockbackVelocity = Vector2.MoveTowards(
                _knockbackVelocity, Vector2.zero, _knockbackDecayPerSecond * Time.fixedDeltaTime);
        }
    }
}
