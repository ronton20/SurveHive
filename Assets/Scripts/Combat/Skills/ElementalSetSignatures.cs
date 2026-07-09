using SurveHive.Combat.Status;
using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Enemies;
using SurveHive.Health;
using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Combat.Skills
{
    /// <summary>
    /// Top-tier elemental set signatures triggered by an enemy's death (PLAN 2B /
    /// TODO #27). Called once from <see cref="EnemyController"/> after the victim
    /// leaves the registry: reads its active statuses and, for each element whose
    /// set is at the 4-piece tier, fires that element's signature —
    ///   Fire  → spread Burn to the nearest enemy,
    ///   Frost → shatter for AoE magic damage,
    ///   Electric → arc the Stun to the nearest enemy,
    ///   Poison → leave a toxic pool,
    ///   Honey → leave a sticky slow zone.
    /// The Physical set's Execute signature is a basic-attack hook (in
    /// <see cref="Projectile"/>), not a death effect. Zero-GC: reuses the existing
    /// zone pool and the registry list, no allocations.
    /// </summary>
    public static class ElementalSetSignatures
    {
        /// <summary>Toxic pools / honey slicks reuse the honey-puddle zone pool.</summary>
        private const int ZonePoolId = PoolIds.HoneyPuddle;
        private const float ZoneTickInterval = 0.5f;
        private static readonly Color PoisonPoolTint = new Color(0.42f, 0.78f, 0.28f, 0.85f);
        private static readonly Color HoneySlickTint = new Color(1f, 0.78f, 0.18f, 0.85f);

        public static void OnEnemyDied(EnemyController victim)
        {
            if (victim == null || victim.StatusReceiver == null)
            {
                return;
            }

            StatusEffectBuffer buffer = victim.StatusReceiver.Buffer;
            Vector3 position = victim.transform.position;

            // Fire — a burning death spreads Burn to a nearby enemy.
            if (buffer.IsActive(StatusEffectType.Burn) &&
                ElementSets.GetSignature(SkillElement.Fire) == SetSignatureType.BurnSpread)
            {
                SetBonusSO fire = ElementSets.GetBonus(SkillElement.Fire);
                EnemyController target = FindNearestEnemy(position, fire.SignatureRadius, victim);
                if (target != null && target.StatusReceiver != null)
                {
                    float dps = Mathf.Max(1f, buffer.GetPotency(StatusEffectType.Burn));
                    target.StatusReceiver.ApplyEffect(StatusEffectType.Burn, dps, fire.SignatureDuration);
                }
            }

            // Electric — a stunned death arcs the Stun to a nearby enemy.
            if (buffer.IsActive(StatusEffectType.Stun) &&
                ElementSets.GetSignature(SkillElement.Electric) == SetSignatureType.StunChain)
            {
                SetBonusSO electric = ElementSets.GetBonus(SkillElement.Electric);
                EnemyController target = FindNearestEnemy(position, electric.SignatureRadius, victim);
                if (target != null && target.StatusReceiver != null)
                {
                    target.StatusReceiver.ApplyEffect(StatusEffectType.Stun, 0f, electric.SignatureDuration);
                }
            }

            // Frost — a chilled/frozen death shatters for AoE magic damage. Cold
            // survives the killing blow where a damage-broken Freeze would not.
            if ((buffer.IsActive(StatusEffectType.Freeze) || buffer.IsActive(StatusEffectType.Cold)) &&
                ElementSets.GetSignature(SkillElement.Frost) == SetSignatureType.FrostShatter)
            {
                SetBonusSO frost = ElementSets.GetBonus(SkillElement.Frost);
                float shatter = ShatterDamage(victim.Health != null ? victim.Health.MaxHealth : 0f, frost.SignaturePotency);
                ShatterArea(position, frost.SignatureRadius, shatter, victim);
            }

            // Poison — a poisoned death leaves a lingering toxic pool.
            if (buffer.IsActive(StatusEffectType.Poison) &&
                ElementSets.GetSignature(SkillElement.Poison) == SetSignatureType.PoisonPool)
            {
                SetBonusSO poison = ElementSets.GetBonus(SkillElement.Poison);
                SpawnZone(
                    position, poison.SignatureRadius, poison.SignatureDuration, poison.SignaturePotency,
                    StatusEffectType.Poison, poison.SignaturePotency, poison.SignatureDuration, PoisonPoolTint);
            }

            // Honey — a slowed death leaves a sticky slow zone (no damage).
            if (buffer.IsActive(StatusEffectType.Slow) &&
                ElementSets.GetSignature(SkillElement.Honey) == SetSignatureType.HoneySlick)
            {
                SetBonusSO honey = ElementSets.GetBonus(SkillElement.Honey);
                SpawnZone(
                    position, honey.SignatureRadius, honey.SignatureDuration, 0f,
                    StatusEffectType.Slow, honey.SignaturePotency, honey.SignatureDuration, HoneySlickTint);
            }
        }

        /// <summary>AoE magic damage a frost shatter deals: a percent of the victim's max HP.</summary>
        public static float ShatterDamage(float victimMaxHealth, float percentOfMaxHealth)
        {
            return Mathf.Max(1f, victimMaxHealth * (percentOfMaxHealth / 100f));
        }

        private static void ShatterArea(Vector3 center, float radius, float damage, EnemyController exclude)
        {
            if (EnemyRegistry.Instance == null)
            {
                return;
            }

            float sqrRadius = radius * radius;
            var enemies = EnemyRegistry.Instance.ActiveEnemies;
            // Reverse iteration: shatter damage can kill and unregister mid-loop.
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || enemy == exclude || enemy.Health == null || enemy.Health.IsDead)
                {
                    continue;
                }

                if ((enemy.transform.position - center).sqrMagnitude > sqrRadius)
                {
                    continue;
                }

                DamageService.DealDamage(enemy.Health, enemy.transform.position, damage, DamageType.Magic, false, enemy.gameObject);
            }
        }

        private static void SpawnZone(
            Vector3 position, float radius, float duration, float tickDamage,
            StatusEffectType status, float statusPotency, float statusDuration, Color tint)
        {
            if (radius <= 0f || duration <= 0f || PoolManager.Instance == null)
            {
                return;
            }

            GameObject zoneObj = PoolManager.Instance.Get(ZonePoolId, position, Quaternion.identity);
            if (zoneObj.TryGetComponent(out AreaEffectZone zone))
            {
                zone.Configure(
                    radius, duration, ZoneTickInterval, tickDamage, DamageType.Magic,
                    appliesStatus: true, status, statusChancePercent: 100f, statusPotency, statusDuration, tint);
            }
        }

        // Nearest live enemy within radius, excluding the dying one. Mirrors the
        // registry-walk pattern in AreaEffectZone/Projectile — zero-GC, no physics.
        private static EnemyController FindNearestEnemy(Vector3 center, float radius, EnemyController exclude)
        {
            if (EnemyRegistry.Instance == null || radius <= 0f)
            {
                return null;
            }

            var enemies = EnemyRegistry.Instance.ActiveEnemies;
            float bestSqr = radius * radius;
            EnemyController best = null;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || enemy == exclude || enemy.Health == null || enemy.Health.IsDead)
                {
                    continue;
                }

                float sqr = (enemy.transform.position - center).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = enemy;
                }
            }

            return best;
        }
    }
}
