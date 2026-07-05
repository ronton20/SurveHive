namespace SurveHive.Progression
{
    /// <summary>
    /// Combat 2.0 per-lane offer gating (PLAN Phase 1B). Pure logic over
    /// caller-provided buffers (no allocations, no Unity dependencies) so EditMode
    /// tests can verify the caps deterministically.
    ///
    /// A skill is offerable this level-up when it is not maxed AND either it is
    /// already owned (level ≥ 1, so it can keep leveling) or its lane still has
    /// room for a new distinct pick (owned-in-lane &lt; that lane's cap). Once a
    /// lane hits its cap, no new pick from it is offered — but owned picks in that
    /// lane keep appearing until they max out.
    /// </summary>
    public static class LaneEligibility
    {
        /// <summary>
        /// Fills <paramref name="eligible"/> with the database indices offerable
        /// this level-up and returns the count. <paramref name="maxLevels"/> uses
        /// 0 to mean "no cap". <paramref name="lanes"/> holds each skill's lane as
        /// an int index into <paramref name="laneCaps"/>.
        /// <paramref name="ownedPerLaneScratch"/> must hold at least
        /// <paramref name="laneCount"/> entries and is overwritten.
        /// </summary>
        public static int BuildEligible(
            int[] lanes, int[] levels, int[] maxLevels, int skillCount,
            int[] laneCaps, int laneCount, int[] ownedPerLaneScratch, int[] eligible)
        {
            for (int l = 0; l < laneCount; l++)
            {
                ownedPerLaneScratch[l] = 0;
            }

            for (int i = 0; i < skillCount; i++)
            {
                if (levels[i] > 0)
                {
                    ownedPerLaneScratch[lanes[i]]++;
                }
            }

            int count = 0;
            for (int i = 0; i < skillCount; i++)
            {
                bool maxed = maxLevels[i] > 0 && levels[i] >= maxLevels[i];
                if (maxed)
                {
                    continue;
                }

                bool owned = levels[i] > 0;
                if (!owned && ownedPerLaneScratch[lanes[i]] >= laneCaps[lanes[i]])
                {
                    continue;
                }

                eligible[count] = i;
                count++;
            }

            return count;
        }
    }
}
