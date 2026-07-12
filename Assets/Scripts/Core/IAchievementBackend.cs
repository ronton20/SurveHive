namespace SurveHive.Core
{
    /// <summary>
    /// Platform achievement seam (PLAN 5D). The save file is the source of
    /// truth for unlocks; a backend only mirrors them outward (Steam overlay
    /// pop, etc.). Local-first: <see cref="LocalAchievementBackend"/> is the
    /// default no-op, and a Steamworks implementation slots in later without
    /// touching the tracker.
    /// </summary>
    public interface IAchievementBackend
    {
        /// <summary>Reports a newly-unlocked achievement id to the platform.</summary>
        void ReportUnlock(string achievementId);
    }

    /// <summary>The active backend; assign once at boot to swap platforms.</summary>
    public static class AchievementBackends
    {
        private static IAchievementBackend _active;

        public static IAchievementBackend Active
        {
            get => _active ?? (_active = new LocalAchievementBackend());
            set => _active = value;
        }
    }

    /// <summary>Default backend: unlocks live only in the local save.</summary>
    public sealed class LocalAchievementBackend : IAchievementBackend
    {
        public void ReportUnlock(string achievementId)
        {
        }
    }
}
