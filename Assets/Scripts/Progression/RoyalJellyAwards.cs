using SurveHive.Data;

namespace SurveHive.Progression
{
    /// <summary>
    /// The full table of Royal Jelly payouts (PLAN 5B / TODO #31). Premium
    /// currency is deliberately scarce — no run multiplier touches these, and
    /// the big awards are one-time (first clear per stage+difficulty). Pure so
    /// the numbers are EditMode-tested; achievements (5D) append here later.
    /// </summary>
    public static class RoyalJellyAwards
    {
        /// <summary>Every miniboss kill.</summary>
        public const int MinibossKill = 1;

        /// <summary>Every final-boss (world boss) kill.</summary>
        public const int FinalBossKill = 3;

        /// <summary>
        /// One-time bonus for the first clear of a stage on a difficulty,
        /// scaling with the tier: Easy 10 → Extreme 25.
        /// </summary>
        public static int FirstClear(DifficultyTier tier)
        {
            int index = (int)tier;
            if (index < 0)
            {
                index = 0;
            }
            else if (index > (int)DifficultyTier.Extreme)
            {
                index = (int)DifficultyTier.Extreme;
            }

            return 10 + 5 * index;
        }
    }
}
