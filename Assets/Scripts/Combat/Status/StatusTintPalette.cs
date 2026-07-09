using UnityEngine;

namespace SurveHive.Combat.Status
{
    /// <summary>
    /// The status-effect readability scheme (PLAN 2A): one signature tint per
    /// status, a fixed display priority, and the pure selection/blend rules
    /// used by <see cref="StatusEffectReceiver"/> — kept static and pure so
    /// the policy is EditMode-testable.
    /// </summary>
    public static class StatusTintPalette
    {
        // Display priority, most important first: hard CC reads above damage-
        // over-time, which reads above movement debuffs.
        private static readonly StatusEffectType[] Priority =
        {
            StatusEffectType.Freeze,
            StatusEffectType.Stun,
            StatusEffectType.Burn,
            StatusEffectType.Poison,
            StatusEffectType.Cold,
            StatusEffectType.Slow,
        };

        // Indexed by StatusEffectType (append-only enum). Deliberately loud —
        // these read at gameplay zoom on ~30px sprites over dark rank tints.
        private static readonly Color[] Tints =
        {
            new Color(1f, 0.4f, 0.05f),   // Burn — ember orange
            new Color(0.25f, 0.95f, 0.1f), // Poison — toxic green
            new Color(0.4f, 0.55f, 1f),   // Slow — dusk blue
            new Color(0.25f, 0.8f, 1f),   // Freeze — ice cyan
            new Color(1f, 0.9f, 0.1f),    // Stun — spark yellow
            new Color(0.5f, 0.75f, 1f),   // Cold — frost blue
        };

        /// <summary>How strongly the hit flash leans into the status hue.</summary>
        public const float FlashHueStrength = 0.65f;

        /// <summary>
        /// How far the sprite color is pushed toward the status tint. A lerp —
        /// not a multiply — so the cue stays loud on dark elite rank tints.
        /// </summary>
        public const float TintStrength = 0.75f;

        /// <summary>Stacked-status pulse rate (full A→B→A cycles per second).</summary>
        public const float PulseHz = 2.5f;

        public static Color GetTint(StatusEffectType type)
        {
            return Tints[(int)type];
        }

        /// <summary>The sprite color for a status over a base rank tint.</summary>
        public static Color GetSpriteColor(Color baseTint, Color statusTint)
        {
            Color color = Color.Lerp(baseTint, statusTint, TintStrength);
            color.a = baseTint.a;
            return color;
        }

        /// <summary>
        /// Finds the two highest-priority active statuses. Returns how many are
        /// active overall capped at 2 (0 = none); <paramref name="first"/> and
        /// <paramref name="second"/> are only meaningful up to that count.
        /// </summary>
        public static int GetTopTwoActive(
            StatusEffectBuffer buffer, out StatusEffectType first, out StatusEffectType second)
        {
            first = default;
            second = default;
            int found = 0;

            for (int i = 0; i < Priority.Length; i++)
            {
                if (!buffer.IsActive(Priority[i]))
                {
                    continue;
                }

                if (found == 0)
                {
                    first = Priority[i];
                }
                else
                {
                    second = Priority[i];
                    return 2;
                }

                found++;
            }

            return found;
        }

        /// <summary>
        /// The sprite tint for a pulse phase: a single status holds its tint
        /// steady; two statuses ping-pong between both so the stack reads.
        /// <paramref name="time"/> is any monotonic clock (Time.time).
        /// </summary>
        public static Color GetPulsedTint(Color first, Color second, float time)
        {
            float phase = Mathf.PingPong(time * PulseHz * 2f, 1f);
            return Color.Lerp(first, second, phase);
        }

        /// <summary>
        /// The hit-flash color while a status is active: still bright enough to
        /// read as a hit, but hue-shifted so the status stays identifiable in
        /// dense combat where enemies are flashing near-constantly.
        /// </summary>
        public static Color GetFlashColor(Color statusTint)
        {
            return Color.Lerp(Color.white, statusTint, FlashHueStrength);
        }
    }
}
