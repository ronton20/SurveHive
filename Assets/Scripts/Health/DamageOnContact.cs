using SurveHive.Combat.Status;
using UnityEngine;

namespace SurveHive.Health
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class DamageOnContact : MonoBehaviour
    {
        [SerializeField] private float _damage = 10f;
        // Touch attacks are physical by default; magic-touch enemies can override.
        [SerializeField] private DamageType _damageType = DamageType.Physical;
        [SerializeField] private float _tickInterval = 1f;
        [SerializeField] private string _targetTag = "Player";
        // Optional: stunned/frozen owners deal no contact damage.
        [SerializeField] private StatusEffectReceiver _statusReceiver;

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

            if (_statusReceiver != null && _statusReceiver.IsAttackDisabled)
            {
                return;
            }

            if (!other.CompareTag(_targetTag))
            {
                return;
            }

            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(_damage, _damageType, gameObject);
                _cooldownRemaining = _tickInterval;
            }
        }
    }
}
