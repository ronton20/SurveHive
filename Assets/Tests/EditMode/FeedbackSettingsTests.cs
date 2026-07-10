using NUnit.Framework;
using SurveHive.Core;
using SurveHive.Persistence;

namespace SurveHive.Tests
{
    /// <summary>
    /// PLAN 3C feedback toggles: <see cref="FeedbackSettings.Apply"/> mirrors the
    /// saved bools into the static live copy, and the <c>Changed</c> event fires
    /// only when a toggle actually flipped (pooled bars re-check on it — a no-op
    /// apply must not wake them).
    /// </summary>
    public sealed class FeedbackSettingsTests
    {
        [TearDown]
        public void TearDown()
        {
            // Statics persist across tests — restore the all-on defaults.
            FeedbackSettings.Apply(new SettingsData());
        }

        [Test]
        public void Defaults_AllOn()
        {
            FeedbackSettings.Apply(new SettingsData());

            Assert.IsTrue(FeedbackSettings.EnemyHealthBars);
            Assert.IsTrue(FeedbackSettings.DamageNumbers);
            Assert.IsTrue(FeedbackSettings.ScreenShake);
            Assert.IsTrue(FeedbackSettings.HitStop);
            Assert.IsTrue(FeedbackSettings.StatusTints);
        }

        [Test]
        public void Apply_MirrorsEveryToggle()
        {
            var settings = new SettingsData
            {
                showEnemyHealthBars = false,
                showDamageNumbers = false,
                screenShake = false,
                hitStop = false,
                statusTints = false,
            };

            FeedbackSettings.Apply(settings);

            Assert.IsFalse(FeedbackSettings.EnemyHealthBars);
            Assert.IsFalse(FeedbackSettings.DamageNumbers);
            Assert.IsFalse(FeedbackSettings.ScreenShake);
            Assert.IsFalse(FeedbackSettings.HitStop);
            Assert.IsFalse(FeedbackSettings.StatusTints);
        }

        [Test]
        public void Apply_NullSettings_IsIgnored()
        {
            FeedbackSettings.Apply(new SettingsData { screenShake = false });

            FeedbackSettings.Apply(null);

            Assert.IsFalse(FeedbackSettings.ScreenShake);
            Assert.IsTrue(FeedbackSettings.DamageNumbers);
        }

        [Test]
        public void Changed_FiresOnlyWhenAToggleFlips()
        {
            FeedbackSettings.Apply(new SettingsData());
            int fired = 0;
            void Handler() => fired++;
            FeedbackSettings.Changed += Handler;
            try
            {
                FeedbackSettings.Apply(new SettingsData());
                Assert.AreEqual(0, fired, "no-op apply must not fire Changed");

                FeedbackSettings.Apply(new SettingsData { hitStop = false });
                Assert.AreEqual(1, fired);

                FeedbackSettings.Apply(new SettingsData { hitStop = false });
                Assert.AreEqual(1, fired, "unchanged re-apply must not fire Changed");

                FeedbackSettings.Apply(new SettingsData());
                Assert.AreEqual(2, fired);
            }
            finally
            {
                FeedbackSettings.Changed -= Handler;
            }
        }
    }
}
