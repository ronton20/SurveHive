using SurveHive.Progression;

namespace SurveHive.UI
{
    /// <summary>
    /// A power-up the player owns this run, for the pause-menu build list
    /// (Combat 2.0 1F). Built on demand while the game is paused.
    /// </summary>
    public readonly struct OwnedPowerUp
    {
        public readonly string Name;
        public readonly PowerUpLane Lane;
        public readonly SkillElement Element;
        public readonly int Level;

        public OwnedPowerUp(string name, PowerUpLane lane, SkillElement element, int level)
        {
            Name = name;
            Lane = lane;
            Element = element;
            Level = level;
        }
    }
}
