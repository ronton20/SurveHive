namespace SurveHive.Progression
{
    /// <summary>
    /// The three level-up offer lanes (Combat 2.0). Each lane has its own distinct
    /// selection cap (Passive 5 / Enhancement 3 / Ability 5) and its own card
    /// banner. Serialized by integer index on <c>SkillDefinitionSO</c> assets —
    /// append new lanes at the end, never insert.
    /// </summary>
    public enum PowerUpLane
    {
        // Enhancements to the player itself (never the attacks): move speed, HP,
        // armor, attack power/speed, ability cooldown/power, crit, lifesteal, magnet.
        Passive = 0,

        // Modifiers to the basic auto-attack: multishot, pierce, ignite, range, etc.
        Enhancement = 1,

        // Active auto-firing skills, separate from the basic attack.
        Ability = 2
    }
}
