namespace SurveHive.Data
{
    /// <summary>
    /// Run difficulty tiers (TODO #30). Serialized by integer value in the
    /// save file and in <see cref="DifficultySO"/> rows — append-only, never
    /// insert or reorder.
    /// </summary>
    public enum DifficultyTier
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Extreme = 3,
    }
}
