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
        // Base crit is 0: all crit chance comes from the Keen Eye power-up and
        // (later) the meta-shop crit upgrade.
        [SerializeField, Range(0f, 100f)] private float _critChancePercent;
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

        [Header("Combat 2.0 Passives")]
        // Percent damage-taken reduction (Armor), capped so it can't fully negate.
        [SerializeField, Range(0f, 100f)] private float _armorPercent;
        [SerializeField] private float _maxArmorPercent = 80f;
        // Multiplier on active-skill (Ability) damage — grows via Ability Power.
        [SerializeField] private float _abilityPowerMultiplier = 1f;

        [Header("Combat 2.0 Basic-Attack Enhancements")]
        // Piercing Stinger level → pierce count (+2/level, "everything" at max) and
        // damage penalty (5%/level). Keep _pierceMaxLevel matched to the asset's
        // MaxLevel so the "pierce everything" tier lands on the last level.
        [SerializeField] private int _pierceLevel;
        [SerializeField] private int _pierceMaxLevel = 3;
        // Damage penalty starts here and lightens by _piercePenaltyStep per level,
        // gone entirely at max level: −30% / −20% / −0% over 3 levels.
        [SerializeField] private float _pierceBasePenalty = 0.30f;
        [SerializeField] private float _piercePenaltyStep = 0.10f;

        // Burning Stinger (fire DoT): chance + damage/tick, both grow per level.
        [SerializeField, Range(0f, 100f)] private float _burnStingerChance;
        [SerializeField] private float _burnStingerDps;
        [SerializeField] private float _burnStingerDpsPerLevel = 2f;
        [SerializeField] private float _burnStingerDuration = 3f;

        // Poison Stinger (poison DoT): chance + damage/tick, both grow per level.
        [SerializeField, Range(0f, 100f)] private float _poisonStingerChance;
        [SerializeField] private float _poisonStingerDps;
        [SerializeField] private float _poisonStingerDpsPerLevel = 1.5f;
        [SerializeField] private float _poisonStingerDuration = 3f;

        // Frost Stinger: chance to freeze (hard CC — rarer, chance-only).
        [SerializeField, Range(0f, 100f)] private float _frostStingerChance;
        [SerializeField] private float _frostStingerBreakThreshold = 12f;
        [SerializeField] private float _frostStingerDuration = 1.2f;

        // Shock Stinger: chance the shot bounces to another enemy; each bounce
        // reduces damage and the chance of a further bounce. Chance + bounce count
        // both grow per level.
        [SerializeField, Range(0f, 100f)] private float _shockStingerChance;
        [SerializeField] private int _shockStingerBounces;
        [SerializeField] private float _shockBounceRange = 4f;
        [SerializeField, Range(0f, 1f)] private float _shockBounceDamageFalloff = 0.65f;
        [SerializeField, Range(0f, 1f)] private float _shockBounceChanceFalloff = 0.6f;

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

        public float ArmorPercent => _armorPercent;

        public float MaxArmorPercent => _maxArmorPercent;

        public float AbilityPowerMultiplier => _abilityPowerMultiplier;

        public int PierceLevel => _pierceLevel;

        public int PierceMaxLevel => _pierceMaxLevel;

        public int BasicAttackPierce => SurveHive.Combat.CombatMath.PierceCount(_pierceLevel, _pierceMaxLevel);

        public float PierceDamageMultiplier =>
            SurveHive.Combat.CombatMath.PierceDamageMultiplier(_pierceLevel, _pierceMaxLevel, _pierceBasePenalty, _piercePenaltyStep);

        public float PierceBasePenalty => _pierceBasePenalty;

        public float PiercePenaltyStep => _piercePenaltyStep;

        public float BurnStingerChance => _burnStingerChance;

        public float BurnStingerDps => _burnStingerDps;

        public float BurnStingerDpsPerLevel => _burnStingerDpsPerLevel;

        public float BurnStingerDuration => _burnStingerDuration;

        public float PoisonStingerChance => _poisonStingerChance;

        public float PoisonStingerDps => _poisonStingerDps;

        public float PoisonStingerDpsPerLevel => _poisonStingerDpsPerLevel;

        public float PoisonStingerDuration => _poisonStingerDuration;

        public float FrostStingerChance => _frostStingerChance;

        public float FrostStingerBreakThreshold => _frostStingerBreakThreshold;

        public float FrostStingerDuration => _frostStingerDuration;

        public float ShockStingerChance => _shockStingerChance;

        public int ShockStingerBounces => _shockStingerBounces;

        public float ShockBounceRange => _shockBounceRange;

        public float ShockBounceDamageFalloff => _shockBounceDamageFalloff;

        public float ShockBounceChanceFalloff => _shockBounceChanceFalloff;

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

        public void IncreaseAttackDamageFlat(float amount)
        {
            _attackDamage = RoundToHundredths(_attackDamage + amount);
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

        // Armor is additive percentage points, capped below 100 so damage always lands.
        public void IncreaseArmorPercent(float percentPoints)
        {
            _armorPercent = Mathf.Min(_maxArmorPercent, RoundToHundredths(_armorPercent + percentPoints));
            NotifyChanged();
        }

        public void IncreaseAbilityPowerPercent(float percent)
        {
            _abilityPowerMultiplier = RoundToHundredths(_abilityPowerMultiplier * (1f + percent / 100f));
            NotifyChanged();
        }

        public void LevelUpPierce()
        {
            _pierceLevel++;
            NotifyChanged();
        }

        // Fire/poison stingers grow both proc chance and damage/tick per level.
        public void LevelUpBurnStinger(float chancePoints)
        {
            _burnStingerChance = Mathf.Min(100f, RoundToHundredths(_burnStingerChance + chancePoints));
            _burnStingerDps = RoundToHundredths(_burnStingerDps + _burnStingerDpsPerLevel);
            NotifyChanged();
        }

        public void LevelUpPoisonStinger(float chancePoints)
        {
            _poisonStingerChance = Mathf.Min(100f, RoundToHundredths(_poisonStingerChance + chancePoints));
            _poisonStingerDps = RoundToHundredths(_poisonStingerDps + _poisonStingerDpsPerLevel);
            NotifyChanged();
        }

        public void LevelUpFrostStinger(float chancePoints)
        {
            _frostStingerChance = Mathf.Min(100f, RoundToHundredths(_frostStingerChance + chancePoints));
            NotifyChanged();
        }

        // Shock grows bounce chance and one extra bounce per level.
        public void LevelUpShockStinger(float chancePoints)
        {
            _shockStingerChance = Mathf.Min(100f, RoundToHundredths(_shockStingerChance + chancePoints));
            _shockStingerBounces += 1;
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
