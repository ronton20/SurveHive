namespace SurveHive.Progression
{
    /// <summary>
    /// Element tag on a power-up, orthogonal to its <see cref="PowerUpLane"/>.
    /// Drives the card's element cue and (later) the elemental set effects
    /// (TODO #19). <see cref="Physical"/> is the neutral default for non-elemental
    /// player stats. Serialized by integer index on <c>SkillDefinitionSO</c>
    /// assets — append new elements at the end, never insert.
    /// </summary>
    public enum SkillElement
    {
        Physical = 0,
        Fire = 1,
        Poison = 2,
        Electric = 3,
        Frost = 4,
        Honey = 5
    }
}
