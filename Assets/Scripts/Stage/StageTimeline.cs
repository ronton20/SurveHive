using SurveHive.Data;

namespace SurveHive.Stage
{
    /// <summary>
    /// Pure timeline-crossing logic (no Unity scene dependencies, no allocations)
    /// so EditMode tests can pin down event firing: each event fires exactly once,
    /// on the frame its normalized time is first reached.
    /// </summary>
    public static class StageTimeline
    {
        /// <summary>
        /// Writes the indices of events whose time lies in (previous, current]
        /// into <paramref name="results"/> and returns how many were written.
        /// Events at exactly 0 fire on the first call (previous &lt; 0 sentinel
        /// recommended). Caller guarantees the buffer is large enough.
        /// </summary>
        public static int CollectNewlyFired(
            StageTimelineEvent[] events, float previous, float current, int[] results)
        {
            if (events == null || current <= previous)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < events.Length && count < results.Length; i++)
            {
                float time = events[i].NormalizedTime;
                if (time > previous && time <= current)
                {
                    results[count] = i;
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Like <see cref="CollectNewlyFired"/> but for warnings: writes the
        /// indices of events whose (time − lead) lies in (previous, current], i.e.
        /// events about to fire <paramref name="leadNormalized"/> ahead. Fires once
        /// per event as its warning window is crossed.
        /// </summary>
        public static int CollectNewlyWarned(
            StageTimelineEvent[] events, float previous, float current, float leadNormalized, int[] results)
        {
            if (events == null || current <= previous)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < events.Length && count < results.Length; i++)
            {
                float warnTime = events[i].NormalizedTime - leadNormalized;
                if (warnTime > previous && warnTime <= current)
                {
                    results[count] = i;
                    count++;
                }
            }

            return count;
        }
    }
}
