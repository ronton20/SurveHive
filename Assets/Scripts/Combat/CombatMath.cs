using UnityEngine;

namespace SurveHive.Combat
{
    /// <summary>
    /// Pure combat formulas shared across systems (kept Unity-light so EditMode
    /// tests can verify them without a scene).
    /// </summary>
    public static class CombatMath
    {
        /// <summary>
        /// Reduces incoming damage by a percentage (0-100, already capped by the
        /// caller). Never drops a landing hit below 1 so armor can't fully negate
        /// damage.
        /// </summary>
        public static float MitigateByArmor(float amount, float armorPercent)
        {
            if (armorPercent <= 0f || amount <= 0f)
            {
                return amount;
            }

            float reduced = amount * (1f - armorPercent / 100f);
            return Mathf.Max(1f, reduced);
        }

        /// <summary>
        /// Per-projectile damage for a Multishot basic attack: each added
        /// projectile makes every shot hit softer, but total output grows ~1.5×
        /// per extra projectile (Combat 2.0 Enhancement spec). At count 1 this is
        /// just the base damage.
        /// </summary>
        public static float MultishotPerProjectileDamage(float baseDamage, int projectileCount)
        {
            if (projectileCount <= 1)
            {
                return baseDamage;
            }

            float total = baseDamage * Mathf.Pow(1.5f, projectileCount - 1);
            return total / projectileCount;
        }

        /// <summary>
        /// Enemies a basic attack passes through at a given Piercing Stinger level:
        /// 0 when unowned, +2 per level, and "everything" (<see cref="int.MaxValue"/>)
        /// once the max level is reached.
        /// </summary>
        public static int PierceCount(int level, int maxLevel)
        {
            if (level <= 0)
            {
                return 0;
            }

            if (level >= maxLevel)
            {
                return int.MaxValue;
            }

            return level * 2;
        }

        /// <summary>
        /// Damage multiplier for a piercing basic attack. The penalty starts at
        /// <paramref name="basePenalty"/> and lightens by <paramref name="penaltyStep"/>
        /// each level, and is removed entirely at max level (pierce everything at
        /// full damage). e.g. base 0.30 / step 0.10 over 3 levels → −30% / −20% / −0%.
        /// </summary>
        public static float PierceDamageMultiplier(int level, int maxLevel, float basePenalty, float penaltyStep)
        {
            if (level <= 0 || level >= maxLevel)
            {
                return 1f;
            }

            float penalty = Mathf.Max(0f, basePenalty - ((level - 1) * penaltyStep));
            return 1f - penalty;
        }
    }
}
