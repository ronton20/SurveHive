namespace SurveHive.Health
{
    /// <summary>
    /// Transforms an incoming damage amount before it reaches health (e.g. the
    /// player's armor, enemy armor). Registered on a HealthComponent at runtime;
    /// applied after the <see cref="IDamageAbsorber"/> so only damage that got
    /// past any shield gets reduced. Reads the damage type — enemy armor reduces
    /// physical hits only.
    /// </summary>
    public interface IDamageMitigator
    {
        /// <summary>Returns the (reduced) damage that should be applied.</summary>
        float Mitigate(float amount, DamageType damageType);
    }
}
