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
    }
}
