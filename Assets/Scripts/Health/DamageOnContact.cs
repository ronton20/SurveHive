using UnityEngine;

namespace SurveHive.Health
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class DamageOnContact : MonoBehaviour
    {
        [SerializeField] private float _damage = 10f;
        [SerializeField] private float _tickInterval = 1f;
        [SerializeField] private string _targetTag = "Player";

        private float _cooldownRemaining;

        public void Configure(float damage, float tickInterval)
        {
            _damage = damage;
            _tickInterval = tickInterval;
        }

        private void OnEnable()
        {
            // Pooled instances must not inherit a leftover cooldown from a previous life.
            _cooldownRemaining = 0f;
        }

        private void Update()
        {
            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining -= Time.deltaTime;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDealDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDealDamage(other);
        }

        private void TryDealDamage(Collider2D other)
        {
            if (_cooldownRemaining > 0f)
            {
                return;
            }

            if (!other.CompareTag(_targetTag))
            {
                return;
            }

            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(_damage, gameObject);
                _cooldownRemaining = _tickInterval;
            }
        }
    }
}
