using System;
using SurveHive.Combat;
using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Enemies
{
    /// <summary>
    /// An enemy's defensive layers (TODO #23): typed shield pools and armor,
    /// applied as the ordered pipeline shield → armor → HP by registering as
    /// both the absorber and the mitigator on the enemy's HealthComponent.
    /// A physical shield soaks physical damage only (magic bypasses it), a
    /// magic shield soaks magic only, and armor reduces the physical damage
    /// that gets past shields. Pure logic (no scene dependencies) so EditMode
    /// tests cover the routing; one instance per enemy, reconfigured on spawn.
    /// </summary>
    public sealed class EnemyDefense : IDamageAbsorber, IDamageMitigator
    {
        private float _physicalShield;
        private float _magicShield;
        private float _maxPhysicalShield;
        private float _maxMagicShield;
        private float _armorPercent;

        /// <summary>
        /// A hit just chipped a shield pool: (absorbed, remainder). Feeds hit
        /// feedback (remainder 0 = the HP path never fires OnDamaged) and the
        /// health bar's shield cue. Not raised by <see cref="Configure"/>.
        /// </summary>
        public event Action<float, float> OnShieldAbsorbed;

        public float PhysicalShield => _physicalShield;

        public float MagicShield => _magicShield;

        public float MaxPhysicalShield => _maxPhysicalShield;

        public float MaxMagicShield => _maxMagicShield;

        public float ArmorPercent => _armorPercent;

        public bool AnyShieldUp => _physicalShield > 0f || _magicShield > 0f;

        /// <summary>Resets all layers for a (re)spawn — pooled enemies must not inherit chipped pools.</summary>
        public void Configure(float physicalShield, float magicShield, float armorPercent)
        {
            _maxPhysicalShield = Mathf.Max(0f, physicalShield);
            _maxMagicShield = Mathf.Max(0f, magicShield);
            _physicalShield = _maxPhysicalShield;
            _magicShield = _maxMagicShield;
            _armorPercent = Mathf.Clamp(armorPercent, 0f, 90f);
        }

        public float Absorb(float amount, DamageType damageType)
        {
            if (amount <= 0f)
            {
                return amount;
            }

            float absorbed;
            if (damageType == DamageType.Physical && _physicalShield > 0f)
            {
                absorbed = Mathf.Min(_physicalShield, amount);
                _physicalShield -= absorbed;
            }
            else if (damageType == DamageType.Magic && _magicShield > 0f)
            {
                absorbed = Mathf.Min(_magicShield, amount);
                _magicShield -= absorbed;
            }
            else
            {
                return amount;
            }

            float remainder = amount - absorbed;
            OnShieldAbsorbed?.Invoke(absorbed, remainder);
            return remainder;
        }

        public float Mitigate(float amount, DamageType damageType)
        {
            if (damageType != DamageType.Physical)
            {
                return amount;
            }

            return CombatMath.MitigateByArmor(amount, _armorPercent);
        }
    }
}
