using NUnit.Framework;
using SurveHive.Enemies;
using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// Phase 3B enemy defenses: typed shield pools and armor, applied as the
    /// ordered pipeline shield → armor → HP reading the incoming DamageType.
    /// </summary>
    public sealed class EnemyDefenseTests
    {
        [Test]
        public void PhysicalShield_SoaksPhysical_MagicBypasses()
        {
            var defense = new EnemyDefense();
            defense.Configure(physicalShield: 50f, magicShield: 0f, armorPercent: 0f);

            Assert.AreEqual(0f, defense.Absorb(30f, DamageType.Physical), "shield soaks the physical hit");
            Assert.AreEqual(20f, defense.PhysicalShield, 0.001f);

            Assert.AreEqual(40f, defense.Absorb(40f, DamageType.Magic), "magic bypasses a physical shield");
            Assert.AreEqual(20f, defense.PhysicalShield, 0.001f, "magic hit left the physical pool untouched");
        }

        [Test]
        public void MagicShield_SoaksMagic_PhysicalBypasses()
        {
            var defense = new EnemyDefense();
            defense.Configure(physicalShield: 0f, magicShield: 50f, armorPercent: 0f);

            Assert.AreEqual(0f, defense.Absorb(30f, DamageType.Magic));
            Assert.AreEqual(20f, defense.MagicShield, 0.001f);

            Assert.AreEqual(40f, defense.Absorb(40f, DamageType.Physical), "physical bypasses a magic shield");
            Assert.AreEqual(20f, defense.MagicShield, 0.001f);
        }

        [Test]
        public void ShieldBreak_PassesRemainderThrough()
        {
            var defense = new EnemyDefense();
            defense.Configure(physicalShield: 20f, magicShield: 0f, armorPercent: 0f);

            Assert.AreEqual(30f, defense.Absorb(50f, DamageType.Physical), "overflow past the pool lands");
            Assert.AreEqual(0f, defense.PhysicalShield);
            Assert.IsFalse(defense.AnyShieldUp);

            Assert.AreEqual(10f, defense.Absorb(10f, DamageType.Physical), "broken shield absorbs nothing");
        }

        [Test]
        public void Armor_ReducesPhysicalOnly()
        {
            var defense = new EnemyDefense();
            defense.Configure(physicalShield: 0f, magicShield: 0f, armorPercent: 50f);

            Assert.AreEqual(50f, defense.Mitigate(100f, DamageType.Physical), 0.001f);
            Assert.AreEqual(100f, defense.Mitigate(100f, DamageType.Magic), 0.001f, "armor ignores magic");
        }

        [Test]
        public void Configure_ResetsChippedPools_ForPooledReuse()
        {
            var defense = new EnemyDefense();
            defense.Configure(physicalShield: 50f, magicShield: 50f, armorPercent: 30f);
            defense.Absorb(50f, DamageType.Physical);
            defense.Absorb(50f, DamageType.Magic);
            Assert.IsFalse(defense.AnyShieldUp);

            defense.Configure(physicalShield: 40f, magicShield: 10f, armorPercent: 0f);
            Assert.AreEqual(40f, defense.PhysicalShield);
            Assert.AreEqual(10f, defense.MagicShield);
            Assert.AreEqual(0f, defense.ArmorPercent);
        }

        [Test]
        public void OnShieldAbsorbed_ReportsAbsorbedAndRemainder()
        {
            var defense = new EnemyDefense();
            defense.Configure(physicalShield: 20f, magicShield: 0f, armorPercent: 0f);

            float reportedAbsorbed = -1f, reportedRemainder = -1f;
            defense.OnShieldAbsorbed += (absorbed, remainder) =>
            {
                reportedAbsorbed = absorbed;
                reportedRemainder = remainder;
            };

            defense.Absorb(50f, DamageType.Physical);
            Assert.AreEqual(20f, reportedAbsorbed);
            Assert.AreEqual(30f, reportedRemainder);
        }

        // End-to-end order through a real HealthComponent: shield soaks first,
        // armor reduces what got past, HP takes the rest.
        [Test]
        public void Pipeline_ShieldThenArmorThenHp()
        {
            var go = new GameObject("defense-pipeline-test");
            try
            {
                var health = go.AddComponent<HealthComponent>();
                health.Initialize(100f);

                var defense = new EnemyDefense();
                defense.Configure(physicalShield: 20f, magicShield: 0f, armorPercent: 50f);
                health.SetDamageAbsorber(defense);
                health.SetDamageMitigator(defense);

                // 50 physical: 20 shielded → 30 through → armor halves → 15 HP.
                health.TakeDamage(50f, DamageType.Physical, null);
                Assert.AreEqual(85f, health.CurrentHealth, 0.001f);

                // 30 magic: bypasses the (broken) physical shield AND armor.
                health.TakeDamage(30f, DamageType.Magic, null);
                Assert.AreEqual(55f, health.CurrentHealth, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Pipeline_FullyAbsorbedHit_DealsNoHpDamage()
        {
            var go = new GameObject("defense-absorb-test");
            try
            {
                var health = go.AddComponent<HealthComponent>();
                health.Initialize(100f);

                var defense = new EnemyDefense();
                defense.Configure(physicalShield: 0f, magicShield: 200f, armorPercent: 0f);
                health.SetDamageAbsorber(defense);
                health.SetDamageMitigator(defense);

                bool damagedFired = false;
                health.OnDamaged += _ => damagedFired = true;

                health.TakeDamage(60f, DamageType.Magic, null);
                Assert.AreEqual(100f, health.CurrentHealth, "shield soaked the whole hit");
                Assert.IsFalse(damagedFired, "OnDamaged must not fire for a fully absorbed hit");
                Assert.AreEqual(140f, defense.MagicShield, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
