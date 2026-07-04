using System;
using SurveHive.Core;
using UnityEngine;

namespace SurveHive.Health
{
    public sealed class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 100f;

        // Serialized + ReadOnly so the live value is visible (but not editable) in
        // the Inspector during Play mode. Overwritten at runtime by Awake/Initialize.
        [SerializeField, ReadOnly] private float _currentHealth;
        private bool _isDead;

        public event Action<float, float> OnHealthChanged;
        // Fired only for actual damage taken (not heals or max-health changes),
        // with the damage amount — feeds hit feedback (flash, shake, knockback).
        public event Action<float> OnDamaged;
        public event Action OnDied;

        public bool IsDead => _isDead;

        public float CurrentHealth => _currentHealth;

        public float MaxHealth => _maxHealth;

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        public void TakeDamage(float amount, GameObject instigator)
        {
            if (_isDead || amount <= 0f)
            {
                return;
            }

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnDamaged?.Invoke(amount);

            if (_currentHealth <= 0f)
            {
                _isDead = true;
                OnDied?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (_isDead || amount <= 0f)
            {
                return;
            }

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void SetMaxHealth(float value, bool refill)
        {
            _maxHealth = value;
            _currentHealth = refill ? _maxHealth : Mathf.Min(_currentHealth, _maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void IncreaseMaxHealth(float delta)
        {
            _maxHealth += delta;
            _currentHealth += delta;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void Initialize(float maxHealth)
        {
            _maxHealth = maxHealth;
            _isDead = false;
            _currentHealth = _maxHealth;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }
}
