namespace SurveHive.Core
{
    /// <summary>
    /// Pure, allocation-free easing curves for UI transitions (Phase 3B-2c).
    /// All functions take and return a normalized 0..1 progress; callers feed the
    /// eased value into a <c>LerpUnclamped</c> so the overshoot in <see cref="OutBack"/>
    /// isn't clipped. Kept dependency-free so it can be unit-tested in EditMode.
    /// </summary>
    public static class Easing
    {
        /// <summary>Decelerating cubic — smooth settle, no overshoot. Good for fades.</summary>
        public static float OutCubic(float t)
        {
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }

        /// <summary>
        /// Decelerating with a slight overshoot past 1 before settling — the "pop"
        /// used for cards scaling/sliding in. Requires LerpUnclamped at the call site.
        /// </summary>
        public static float OutBack(float t)
        {
            const float overshoot = 1.70158f;
            float inv = t - 1f;
            return 1f + (overshoot + 1f) * inv * inv * inv + overshoot * inv * inv;
        }
    }
}
