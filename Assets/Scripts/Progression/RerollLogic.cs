using System;

namespace SurveHive.Progression
{
    /// <summary>
    /// Power-up reroll rules (PLAN 1C): the bought meta rank is the per-run
    /// stock (hard-capped), and a reroll re-picks exactly one card from the
    /// eligible pool, never landing on a card already on screen. Pure logic
    /// over caller-provided buffers so EditMode tests pin the semantics.
    /// </summary>
    public static class RerollLogic
    {
        /// <summary>Per-run reroll stock from the bought meta rank.</summary>
        public static int StockFromRank(int rank, int maxPerRun)
        {
            if (rank < 0)
            {
                return 0;
            }

            return rank > maxPerRun ? maxPerRun : rank;
        }

        /// <summary>
        /// Picks one replacement from the eligible buffer (first
        /// <paramref name="eligibleCount"/> slots), excluding every db index in
        /// <paramref name="shownDbIndices"/> (first <paramref name="shownCount"/>
        /// slots — including the card being replaced). Zeroes excluded weights
        /// in place. Returns the picked db index, or -1 when nothing else is
        /// eligible.
        /// </summary>
        public static int PickReplacement(
            int[] eligible, float[] weights, int eligibleCount,
            int[] shownDbIndices, int shownCount,
            int[] resultScratch, float[] weightScratch, Random rng)
        {
            for (int i = 0; i < eligibleCount; i++)
            {
                for (int s = 0; s < shownCount; s++)
                {
                    if (eligible[i] == shownDbIndices[s])
                    {
                        weights[i] = 0f;
                        break;
                    }
                }
            }

            int picked = SkillOfferSelector.Select(
                eligible, weights, eligibleCount, 1, resultScratch, weightScratch, rng);
            return picked > 0 ? resultScratch[0] : -1;
        }
    }
}
