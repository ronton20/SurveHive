using UnityEngine;

namespace SurveHive.UI
{
    /// <summary>
    /// PLAN 3B-2d — maps a health ratio [0..1] to a readable fill colour so danger
    /// is legible at a glance from colour alone, not just bar length: healthy green
    /// → honey amber at half → danger red when low. Pure and allocation-free; call
    /// it on a health-changed event, never per-frame.
    /// </summary>
    public static class HealthColorGradient
    {
        private static readonly Color High = new Color(0.30f, 0.82f, 0.32f); // healthy green
        private static readonly Color Mid = new Color(0.95f, 0.74f, 0.18f);  // honey amber
        private static readonly Color Low = new Color(0.85f, 0.14f, 0.14f);  // danger red

        public static Color Evaluate(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            if (ratio >= 0.5f)
            {
                return Color.Lerp(Mid, High, (ratio - 0.5f) * 2f);
            }

            return Color.Lerp(Low, Mid, ratio * 2f);
        }
    }
}
