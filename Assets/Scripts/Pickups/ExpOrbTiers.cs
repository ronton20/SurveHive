using UnityEngine;

namespace SurveHive.Pickups
{
    /// <summary>
    /// Visual tiers for EXP orbs by stored value: bigger and hotter-colored the
    /// more EXP an orb holds (matters once nearby drops merge into one orb).
    /// The orb sprite is neutral white so these tints read purely.
    /// </summary>
    public static class ExpOrbTiers
    {
        // value < 10 → 0, < 30 → 1, < 120 → 2, else 3.
        private static readonly float[] Thresholds = { 10f, 30f, 120f };

        private static readonly Color[] Colors =
        {
            new Color(0.55f, 0.95f, 0.3f),  // green — trash trickle
            new Color(0.35f, 0.85f, 1f),    // cyan — merged / elite
            new Color(1f, 0.7f, 0.25f),     // orange — big pile
            new Color(0.85f, 0.55f, 1f),    // royal purple — boss-grade
        };

        private static readonly float[] Scales = { 1f, 1.3f, 1.6f, 2f };

        public const int TierCount = 4;

        public static int GetTier(float value)
        {
            for (int i = 0; i < Thresholds.Length; i++)
            {
                if (value < Thresholds[i])
                {
                    return i;
                }
            }

            return Thresholds.Length;
        }

        public static Color GetColor(int tier)
        {
            return Colors[Mathf.Clamp(tier, 0, Colors.Length - 1)];
        }

        public static float GetScale(int tier)
        {
            return Scales[Mathf.Clamp(tier, 0, Scales.Length - 1)];
        }
    }
}
