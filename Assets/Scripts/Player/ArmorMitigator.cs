using SurveHive.Combat;
using SurveHive.Health;

namespace SurveHive.Player
{
    /// <summary>
    /// Applies the player's Armor passive as damage-taken reduction. Plain logic
    /// object (not a MonoBehaviour) registered on the player's HealthComponent by
    /// <see cref="PlayerBootstrap"/>; reads the live armor stat each hit.
    /// </summary>
    public sealed class ArmorMitigator : IDamageMitigator
    {
        private readonly PlayerStats _stats;

        public ArmorMitigator(PlayerStats stats)
        {
            _stats = stats;
        }

        public float Mitigate(float amount)
        {
            return CombatMath.MitigateByArmor(amount, _stats.ArmorPercent);
        }
    }
}
