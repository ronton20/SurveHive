namespace SurveHive.Data
{
    /// <summary>
    /// What an achievement's threshold is measured against (PLAN 5D). Every
    /// condition maps onto a signal the game already emits — run kills, hero
    /// level, survival time, elemental set tiers, and stage clears — so the
    /// tracker never needs new gameplay plumbing.
    /// </summary>
    public enum AchievementConditionType
    {
        /// <summary>Kills in a single run reach the threshold.</summary>
        KillsInRun = 0,

        /// <summary>Hero level in a single run reaches the threshold.</summary>
        ReachLevel = 1,

        /// <summary>Seconds survived in a single run reach the threshold.</summary>
        SurviveSeconds = 2,

        /// <summary>Any elemental set reaches tier number ≥ threshold (1 = first tier).</summary>
        SetTierActive = 3,

        /// <summary>A stage is cleared on difficulty ≥ threshold ((int)DifficultyTier).</summary>
        ClearStage = 4,
    }
}
