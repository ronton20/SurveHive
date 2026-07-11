using UnityEngine;

namespace SurveHive.View
{
    /// <summary>
    /// The equipped stinger skin for the hero's auto-attack projectile (PLAN 5C
    /// follow-up): one static override read by <see cref="ProjectileSkin"/> on
    /// every pooled spawn. Set once per run by <see cref="CosmeticApplier"/>
    /// (null sprite = the prefab's default look), so no per-frame lookups and
    /// no per-instance wiring.
    /// </summary>
    public static class StingerSkin
    {
        public static Sprite OverrideSprite { get; private set; }

        public static Color Tint { get; private set; } = Color.white;

        public static void Set(Sprite sprite, Color tint)
        {
            OverrideSprite = sprite;
            Tint = tint;
        }

        public static void Clear()
        {
            Set(null, Color.white);
        }
    }
}
