namespace SurveHive.Health
{
    /// <summary>
    /// Intercepts incoming damage before it reaches health (Wax Shield).
    /// Registered on a HealthComponent at runtime.
    /// </summary>
    public interface IDamageAbsorber
    {
        /// <summary>Returns true if the hit was fully absorbed.</summary>
        bool TryAbsorb(float amount);
    }
}
