namespace SurveHive.Health
{
    /// <summary>
    /// Intercepts incoming damage before it reaches health (the player's Wax
    /// Shield charges, enemy shield pools). Registered on a HealthComponent at
    /// runtime. Absorption is type-aware and may be partial: only the returned
    /// remainder continues down the pipeline (mitigator → HP).
    /// </summary>
    public interface IDamageAbsorber
    {
        /// <summary>Returns the damage left over after absorption (0 = fully absorbed).</summary>
        float Absorb(float amount, DamageType damageType);
    }
}
