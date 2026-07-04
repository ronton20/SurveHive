using System;
using UnityEngine;

namespace SurveHive.Player
{
    public sealed class PlayerStats : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _attackRange = 4f;
        [SerializeField] private float _attackDamage = 10f;
        [SerializeField] private float _attackCooldown = 1f;
        // Attack rate multiplier: higher = faster. Effective delay between shots is
        // _attackCooldown / _attackSpeed, so modifiers can stack multiplicatively.
        [SerializeField] private float _attackSpeed = 1f;
        [SerializeField] private int _projectileCount = 1;
        [SerializeField] private int _maxProjectileCount = 5;

        [Header("Phase 2 Combat Stats")]
        [SerializeField, Range(0f, 100f)] private float _critChancePercent = 5f;
        // Damage multiplier applied on a critical hit (1.5 = +50%).
        [SerializeField] private float _critDamageMultiplier = 1.5f;
        // Percent of damage dealt returned as healing.
        [SerializeField, Range(0f, 100f)] private float _lifestealPercent;
        // Multiplier on active-skill cooldowns (lower = faster), floored so
        // stacked cooldown reduction can't zero out cooldowns.
        [SerializeField] private float _activeCooldownMultiplier = 1f;
        [SerializeField] private float _minActiveCooldownMultiplier = 0.4f;
        // Multiplier on pickup attract radius (Nectar Sense).
        [SerializeField] private float _magnetRadiusMultiplier = 1f;

        public event Action OnStatsChanged;

        public float MoveSpeed => _moveSpeed;

        public float MaxHealth => _maxHealth;

        public float AttackRange => _attackRange;

        // Raw accumulating damage stat (2-decimal precision).
        public float AttackDamage => _attackDamage;

        // Whole-number damage actually applied on hit / shown in the popup. The raw
        // stat keeps growing in the background; only its rounded value is dealt.
        public float EffectiveAttackDamage => Mathf.Round(_attackDamage);

        public float AttackCooldown => _attackCooldown;

        public float AttackSpeed => _attackSpeed;

        public float EffectiveAttackInterval => _attackCooldown / Mathf.Max(0.01f, _attackSpeed);

        public int ProjectileCount => _projectileCount;

        public int MaxProjectileCount => _maxProjectileCount;

        public bool IsProjectileCountMaxed => _projectileCount >= _maxProjectileCount;

        public float CritChancePercent => _critChancePercent;

        public float CritDamageMultiplier => _critDamageMultiplier;

        public float LifestealPercent => _lifestealPercent;

        public float ActiveCooldownMultiplier => _activeCooldownMultiplier;

        public float MinActiveCooldownMultiplier => _minActiveCooldownMultiplier;

        public float MagnetRadiusMultiplier => _magnetRadiusMultiplier;

        public void IncreaseMoveSpeedPercent(float percent)
        {
            _moveSpeed = RoundToHundredths(_moveSpeed * (1f + percent / 100f));
            NotifyChanged();
        }

        public void IncreaseMaxHealthFlat(float amount)
        {
            _maxHealth = RoundToHundredths(_maxHealth + amount);
            NotifyChanged();
        }

        public void IncreaseAttackRangePercent(float percent)
        {
            _attackRange = RoundToHundredths(_attackRange * (1f + percent / 100f));
            NotifyChanged();
        }

        public void IncreaseAttackDamagePercent(float percent)
        {
            _attackDamage = RoundToHundredths(_attackDamage * (1f + percent / 100f));
            NotifyChanged();
        }

        public void DecreaseAttackCooldownPercent(float percent)
        {
            _attackCooldown = RoundToHundredths(_attackCooldown * (1f - percent / 100f));
            NotifyChanged();
        }

        public void IncreaseAttackSpeedPercent(float percent)
        {
            _attackSpeed = RoundToHundredths(_attackSpeed * (1f + percent / 100f));
            NotifyChanged();
        }

        public void IncreaseProjectileCountFlat(int amount)
        {
            _projectileCount = Mathf.Min(_projectileCount + amount, _maxProjectileCount);
            NotifyChanged();
        }

        public void IncreaseCritChanceFlat(float percentPoints)
        {
            _critChancePercent = Mathf.Min(100f, RoundToHundredths(_critChancePercent + percentPoints));
            NotifyChanged();
        }

        public void IncreaseCritDamagePercent(float percent)
        {
            _critDamageMultiplier = RoundToHundredths(_critDamageMultiplier + (percent / 100f));
            NotifyChanged();
        }

        public void IncreaseLifestealFlat(float percentPoints)
        {
            _lifestealPercent = Mathf.Min(100f, RoundToHundredths(_lifestealPercent + percentPoints));
            NotifyChanged();
        }

        public void DecreaseActiveCooldownPercent(float percent)
        {
            _activeCooldownMultiplier = Mathf.Max(
                _minActiveCooldownMultiplier,
                RoundToHundredths(_activeCooldownMultiplier * (1f - percent / 100f)));
            NotifyChanged();
        }

        public void IncreaseMagnetRadiusPercent(float percent)
        {
            _magnetRadiusMultiplier = RoundToHundredths(_magnetRadiusMultiplier * (1f + percent / 100f));
            NotifyChanged();
        }

        private static float RoundToHundredths(float value)
        {
            return Mathf.Round(value * 100f) / 100f;
        }

        private void NotifyChanged()
        {
            OnStatsChanged?.Invoke();
        }
    }
}
