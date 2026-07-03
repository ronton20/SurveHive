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
