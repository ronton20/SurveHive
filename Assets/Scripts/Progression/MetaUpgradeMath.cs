using UnityEngine;

namespace SurveHive.Progression
{
    /// <summary>Pure meta-shop math, EditMode-tested.</summary>
    public static class MetaUpgradeMath
    {
        /// <summary>Cost to buy the next rank when currently at <paramref name="currentRank"/>.</summary>
        public static int CostForRank(int baseCost, float costGrowth, int currentRank)
        {
            if (currentRank <= 0)
            {
                return baseCost;
            }

            return Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, currentRank));
        }

        /// <summary>Combined effect magnitude at a rank (linear per-rank stacking).</summary>
        public static float TotalEffectAtRank(float effectPerRank, int rank)
        {
            return effectPerRank * Mathf.Max(0, rank);
        }
    }
}
