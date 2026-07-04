using System;

namespace SurveHive.Progression
{
    /// <summary>
    /// Weighted sampling without replacement for the level-up offer. Pure logic
    /// over caller-provided buffers (no allocations, no Unity dependencies) so
    /// EditMode tests can verify the rarity distribution deterministically.
    /// </summary>
    public static class SkillOfferSelector
    {
        /// <summary>
        /// Picks up to <paramref name="pickCount"/> distinct entries from
        /// <paramref name="eligible"/> (first <paramref name="eligibleCount"/>
        /// slots), each with probability proportional to its weight. Selected
        /// values are written to <paramref name="result"/>; returns how many
        /// were picked. <paramref name="weightScratch"/> must hold at least
        /// <paramref name="eligibleCount"/> entries and is overwritten.
        /// </summary>
        public static int Select(
            int[] eligible, float[] weights, int eligibleCount, int pickCount,
            int[] result, float[] weightScratch, Random rng)
        {
            if (eligibleCount <= 0 || pickCount <= 0)
            {
                return 0;
            }

            float totalWeight = 0f;
            for (int i = 0; i < eligibleCount; i++)
            {
                weightScratch[i] = weights[i] > 0f ? weights[i] : 0f;
                totalWeight += weightScratch[i];
            }

            int picked = 0;
            int maxPicks = Math.Min(pickCount, eligibleCount);

            while (picked < maxPicks && totalWeight > 0f)
            {
                double roll = rng.NextDouble() * totalWeight;
                double cumulative = 0d;
                int chosen = -1;

                for (int i = 0; i < eligibleCount; i++)
                {
                    if (weightScratch[i] <= 0f)
                    {
                        continue;
                    }

                    cumulative += weightScratch[i];
                    if (roll <= cumulative)
                    {
                        chosen = i;
                        break;
                    }
                }

                // Floating-point edge: land on the last weighted entry.
                if (chosen < 0)
                {
                    for (int i = eligibleCount - 1; i >= 0; i--)
                    {
                        if (weightScratch[i] > 0f)
                        {
                            chosen = i;
                            break;
                        }
                    }

                    if (chosen < 0)
                    {
                        break;
                    }
                }

                result[picked] = eligible[chosen];
                picked++;
                totalWeight -= weightScratch[chosen];
                weightScratch[chosen] = 0f;
            }

            return picked;
        }
    }
}
