using System;
using System.Collections.Generic;

namespace SurveHive.Progression
{
    /// <summary>
    /// PLAN 5E — the rotating cosmetics shop's pure logic, EditMode-tested.
    /// Each local calendar day deterministically features up to
    /// <see cref="DealsPerDay"/> not-yet-owned cosmetics from the 5C catalog at
    /// <see cref="DiscountPercent"/> off (the whole catalog is always buyable
    /// at list price in Hive Style — the discount is the reason to check the
    /// deals). No server: the pick is seeded by the date, and the chosen ids
    /// are frozen into the save for the day so buying one deal never re-rolls
    /// the others. Menu-only path, so the pick's list allocations are fine.
    /// </summary>
    public static class RotatingShop
    {
        public const int DealsPerDay = 3;
        public const int DiscountPercent = 30;

        /// <summary>Local-date stamp (yyyymmdd) — the pick seed and the save's rotation key.</summary>
        public static int DayStamp(DateTime localNow)
        {
            return localNow.Year * 10000 + localNow.Month * 100 + localNow.Day;
        }

        /// <summary>Whole seconds until the next local midnight (the rotation rollover).</summary>
        public static int SecondsUntilRollover(DateTime localNow)
        {
            return (int)Math.Ceiling((localNow.Date.AddDays(1) - localNow).TotalSeconds);
        }

        /// <summary>Discounted deal price: 70% of list, rounded half-up, never below 1 jelly.</summary>
        public static int DealPrice(int jellyCost)
        {
            if (jellyCost <= 0)
            {
                return 0;
            }

            int price = (jellyCost * (100 - DiscountPercent) + 50) / 100;
            return Math.Max(1, price);
        }

        /// <summary>
        /// Fills <paramref name="results"/> with up to <see cref="DealsPerDay"/>
        /// distinct picks from <paramref name="candidateIds"/> (the not-owned
        /// catalog ids), deterministic for a given day stamp.
        /// </summary>
        public static void Pick(IReadOnlyList<string> candidateIds, int dayStamp, List<string> results)
        {
            results.Clear();
            if (candidateIds == null || candidateIds.Count == 0)
            {
                return;
            }

            var pool = new List<string>(candidateIds);
            var rng = new Random(dayStamp);
            int count = Math.Min(DealsPerDay, pool.Count);
            for (int i = 0; i < count; i++)
            {
                int pick = rng.Next(pool.Count);
                results.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
        }
    }
}
