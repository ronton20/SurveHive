using System;
using SurveHive.Persistence;

namespace SurveHive.Core
{
    /// <summary>
    /// Process-wide live copy of the saved feedback-layer toggles (PLAN 3C).
    /// The persistent store pushes it on save load and after every settings
    /// save, so hot paths (damage numbers, shake, hit-stop, status tints,
    /// enemy bars) gate on plain static bools instead of touching the save.
    /// Everything defaults to on, which also covers tests and scenes that
    /// never load a save.
    /// </summary>
    public static class FeedbackSettings
    {
        public static bool EnemyHealthBars { get; private set; } = true;
        public static bool DamageNumbers { get; private set; } = true;
        public static bool ScreenShake { get; private set; } = true;
        public static bool HitStop { get; private set; } = true;
        public static bool StatusTints { get; private set; } = true;

        /// <summary>Raised only when <see cref="Apply"/> changed at least one toggle.</summary>
        public static event Action Changed;

        public static void Apply(SettingsData settings)
        {
            if (settings == null)
            {
                return;
            }

            bool changed =
                EnemyHealthBars != settings.showEnemyHealthBars
                || DamageNumbers != settings.showDamageNumbers
                || ScreenShake != settings.screenShake
                || HitStop != settings.hitStop
                || StatusTints != settings.statusTints;

            EnemyHealthBars = settings.showEnemyHealthBars;
            DamageNumbers = settings.showDamageNumbers;
            ScreenShake = settings.screenShake;
            HitStop = settings.hitStop;
            StatusTints = settings.statusTints;

            if (changed)
            {
                Changed?.Invoke();
            }
        }
    }
}
