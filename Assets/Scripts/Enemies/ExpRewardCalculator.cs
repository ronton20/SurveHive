using UnityEngine;

namespace SurveHive.Enemies
{
    /// <summary>
    /// EXP reward formula: a per-rank base (the dominant term) plus a share of
    /// the enemy's actual max health, so rewards scale automatically with both
    /// rank and the per-minute health scaling. Pure and EditMode-tested.
    /// </summary>
    public static class ExpRewardCalculator
    {
        // Worker, Warrior, Queen's Guard, Royal Guard, Queen.
        private static readonly float[] RankBase = { 3f, 6f, 14f, 80f, 250f };

        public const float HealthFactor = 0.08f;

        /// <summary>maxHealth is the spawned enemy's effective (scaled) max HP.</summary>
        public static float Calculate(int rank, float maxHealth)
        {
            int index = Mathf.Clamp(rank, 0, RankBase.Length - 1);
            return Mathf.Max(1f, Mathf.Round(RankBase[index] + (maxHealth * HealthFactor)));
        }
    }
}
