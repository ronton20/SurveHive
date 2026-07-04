using SurveHive.Health;
using SurveHive.Player;
using UnityEngine;

namespace SurveHive.Combat
{
    /// <summary>
    /// Single entry point for player-sourced damage: rolls crits, applies
    /// lifesteal, and spawns the styled damage number. Every player weapon and
    /// skill deals damage through here so Keen Eye / Deadly Precision / Nectar
    /// Drain automatically affect all of them.
    /// </summary>
    public static class DamageService
    {
        /// <summary>Deals damage and returns the final amount actually applied.</summary>
        public static float DealDamage(IDamageable target, Vector3 hitPosition, float baseDamage, bool canCrit, GameObject instigator)
        {
            return DealDamage(target, hitPosition, baseDamage, canCrit, instigator, true);
        }

        // showPopup=false for very fast tick sources (pollen aura at 4 ticks/s)
        // where per-hit numbers would flood the screen and the popup pool.
        public static float DealDamage(
            IDamageable target, Vector3 hitPosition, float baseDamage, bool canCrit,
            GameObject instigator, bool showPopup)
        {
            PlayerStats stats = PlayerContext.Stats;

            bool isCrit = false;
            float damage = baseDamage;
            if (canCrit && stats != null && stats.CritChancePercent > 0f &&
                Random.value * 100f < stats.CritChancePercent)
            {
                damage *= stats.CritDamageMultiplier;
                isCrit = true;
            }

            damage = Mathf.Max(1f, Mathf.Round(damage));
            target.TakeDamage(damage, instigator);

            if (stats != null && stats.LifestealPercent > 0f && PlayerContext.Health != null)
            {
                PlayerContext.Health.Heal(damage * (stats.LifestealPercent / 100f));
            }

            if (showPopup)
            {
                if (isCrit)
                {
                    DamagePopupSpawner.Spawn(hitPosition, damage, DamagePopupSpawner.CritColor, DamagePopupSpawner.CritSizeMultiplier);
                }
                else
                {
                    DamagePopupSpawner.Spawn(hitPosition, damage, DamagePopupSpawner.NormalColor, 1f);
                }
            }

            return damage;
        }
    }
}
