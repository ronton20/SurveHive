using SurveHive.Player;
using UnityEngine;

namespace SurveHive.Pickups
{
    /// <summary>
    /// The single source of truth for pickup attraction: EXP/currency motes and
    /// world item drops all share the same base radius, the Nectar Sense magnet
    /// multiplier, and the Magnet drop's global vacuum.
    /// </summary>
    public static class PickupMotion
    {
        public const float BaseAttractRadius = 3f;
        public const float BaseAttractSpeed = 8f;
        public const float VacuumSpeedMultiplier = 2.5f;

        private static float _vacuumUntilTime;

        public static void ActivateVacuum(float durationSeconds)
        {
            _vacuumUntilTime = Time.time + durationSeconds;
        }

        public static bool VacuumActive => Time.time < _vacuumUntilTime;

        public static float CurrentAttractRadius()
        {
            PlayerStats stats = PlayerContext.Stats;
            return BaseAttractRadius * (stats != null ? stats.MagnetRadiusMultiplier : 1f);
        }

        /// <summary>Drifts a pickup toward the player when in range (or vacuumed).</summary>
        public static void Step(Transform mover, Transform player)
        {
            bool vacuum = VacuumActive;
            Vector3 toPlayer = player.position - mover.position;

            if (!vacuum)
            {
                float radius = CurrentAttractRadius();
                if (toPlayer.sqrMagnitude > radius * radius)
                {
                    return;
                }
            }

            float step = (vacuum ? BaseAttractSpeed * VacuumSpeedMultiplier : BaseAttractSpeed) * Time.deltaTime;
            mover.position += toPlayer.normalized * Mathf.Min(step, toPlayer.magnitude);
        }
    }
}
