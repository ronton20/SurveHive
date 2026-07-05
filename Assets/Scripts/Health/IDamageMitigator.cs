namespace SurveHive.Health
{
    /// <summary>
    /// Transforms an incoming damage amount before it reaches health (e.g. the
    /// player's armor). Registered on a HealthComponent at runtime; applied after
    /// the <see cref="IDamageAbsorber"/> check so only damage that actually lands
    /// gets reduced.
    /// </summary>
    public interface IDamageMitigator
    {
        /// <summary>Returns the (reduced) damage that should be applied.</summary>
        float Mitigate(float amount);
    }
}
