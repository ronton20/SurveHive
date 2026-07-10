namespace SurveHive.Data
{
    /// <summary>
    /// The feedback layers a player can switch off in settings (PLAN 3C).
    /// Serialized (as int) on the FeedbackToggleUI settings rows — append only.
    /// </summary>
    public enum FeedbackToggleKind
    {
        EnemyHealthBars = 0,
        DamageNumbers = 1,
        ScreenShake = 2,
        HitStop = 3,
        StatusTints = 4,
    }
}
