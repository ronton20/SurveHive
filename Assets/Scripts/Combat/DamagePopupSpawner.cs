using SurveHive.Core;
using SurveHive.UI;
using UnityEngine;

namespace SurveHive.Combat
{
    /// <summary>
    /// Central spawn point for pooled damage numbers so every damage source
    /// (projectiles, skills, DoT ticks) styles them consistently.
    /// </summary>
    public static class DamagePopupSpawner
    {
        public static readonly Color NormalColor = Color.white;
        // Honey gold, oversized — crits must read at a glance.
        public static readonly Color CritColor = new Color(1f, 0.765f, 0.043f);
        public static readonly Color BurnColor = new Color(1f, 0.55f, 0.2f);
        public static readonly Color PoisonColor = new Color(0.55f, 0.85f, 0.25f);

        public const float CritSizeMultiplier = 1.4f;
        public const float DotSizeMultiplier = 0.8f;

        public static void Spawn(Vector3 worldPosition, float amount, Color color, float sizeMultiplier)
        {
            // PLAN 3C: damage numbers are a player-toggleable feedback layer.
            if (!FeedbackSettings.DamageNumbers || PoolManager.Instance == null)
            {
                return;
            }

            // No-grow get: when a pierce volley hits dozens of enemies in one
            // frame, dropping the overflow numbers is invisible — instantiating
            // (and later destroying) a burst of popup canvases is a visible hitch.
            Vector3 popupPosition = worldPosition + (Vector3.up * 0.6f);
            if (!PoolManager.Instance.TryGet(PoolIds.DamageNumber, popupPosition, Quaternion.identity, out GameObject popupObj))
            {
                return;
            }

            if (popupObj.TryGetComponent(out DamageNumberPopup popup))
            {
                popup.Show(amount, color, sizeMultiplier);
            }
        }
    }
}
