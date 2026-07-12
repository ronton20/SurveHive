namespace SurveHive.Progression
{
    /// <summary>
    /// The live stat snapshot achievements are checked against (PLAN 5D). The
    /// run tracker mutates one instance in place from existing gameplay
    /// signals; the evaluation in <see cref="AchievementRules"/> only reads it.
    /// </summary>
    public struct AchievementRunStats
    {
        public int Kills;

        public int Level;

        public float SurvivedSeconds;

        /// <summary>Highest active set tier as a 1-based number (0 = no set active).</summary>
        public int MaxSetTier;

        /// <summary>(int)DifficultyTier of a victorious run end, or -1 while unresolved.</summary>
        public int ClearedDifficulty;

        public static AchievementRunStats Empty => new AchievementRunStats { ClearedDifficulty = -1 };
    }
}
