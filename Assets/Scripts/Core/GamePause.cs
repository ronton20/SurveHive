namespace SurveHive.Core
{
    /// <summary>
    /// Central owner of gameplay pauses (level-up screen, death screen). Anything
    /// that toys with <c>Time.timeScale</c> briefly (e.g. <see cref="HitStop"/>)
    /// must defer to <see cref="IsPaused"/> so it never overrides a real pause.
    /// </summary>
    public static class GamePause
    {
        public static bool IsPaused { get; private set; }

        public static void SetPaused(bool paused)
        {
            IsPaused = paused;
            UnityEngine.Time.timeScale = paused ? 0f : 1f;
        }
    }
}
