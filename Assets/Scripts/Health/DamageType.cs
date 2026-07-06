namespace SurveHive.Health
{
    /// <summary>
    /// The physical/magic split every damage application carries (TODO #20).
    /// Physical: basic attack, stingers, enhancements, enemy contact/projectiles.
    /// Magic: elemental abilities (honey/pollen/ember/static/frost) and status DoTs.
    /// Enemy defenses (PLAN 3B) read this to route shield/armor mitigation.
    /// Serialized by integer index in assets — append-only, never insert.
    /// </summary>
    public enum DamageType
    {
        Physical = 0,
        Magic = 1
    }
}
