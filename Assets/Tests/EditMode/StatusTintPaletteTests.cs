using NUnit.Framework;
using SurveHive.Combat.Status;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// PLAN 2A status readability policy: tint selection priority, the
    /// stacked-status pulse, and the hue-shifted hit-flash color.
    /// </summary>
    public sealed class StatusTintPaletteTests
    {
        private static StatusEffectBuffer BufferWith(params StatusEffectType[] effects)
        {
            var buffer = new StatusEffectBuffer();
            foreach (StatusEffectType effect in effects)
            {
                buffer.Apply(effect, 0.5f, 5f);
            }

            return buffer;
        }

        [Test]
        public void NoEffects_ReportsZeroActive()
        {
            Assert.AreEqual(0, StatusTintPalette.GetTopTwoActive(BufferWith(), out _, out _));
        }

        [Test]
        public void SingleEffect_IsReportedFirst()
        {
            int count = StatusTintPalette.GetTopTwoActive(
                BufferWith(StatusEffectType.Poison), out StatusEffectType first, out _);
            Assert.AreEqual(1, count);
            Assert.AreEqual(StatusEffectType.Poison, first);
        }

        [Test]
        public void HardCC_OutranksDots_WhichOutrankMovement()
        {
            // Freeze > Stun > Burn > Poison > Cold > Slow.
            int count = StatusTintPalette.GetTopTwoActive(
                BufferWith(StatusEffectType.Slow, StatusEffectType.Burn, StatusEffectType.Freeze),
                out StatusEffectType first, out StatusEffectType second);
            Assert.AreEqual(2, count);
            Assert.AreEqual(StatusEffectType.Freeze, first);
            Assert.AreEqual(StatusEffectType.Burn, second);
        }

        [Test]
        public void EveryStatus_HasADistinctTint()
        {
            for (int i = 0; i < StatusEffectBuffer.EffectTypeCount; i++)
            {
                for (int j = i + 1; j < StatusEffectBuffer.EffectTypeCount; j++)
                {
                    Assert.AreNotEqual(
                        StatusTintPalette.GetTint((StatusEffectType)i),
                        StatusTintPalette.GetTint((StatusEffectType)j),
                        $"{(StatusEffectType)i} and {(StatusEffectType)j} share a tint");
                }
            }
        }

        [Test]
        public void PulsedTint_SweepsBetweenBothColors()
        {
            Color a = Color.red;
            Color b = Color.blue;
            float halfCycle = 1f / (StatusTintPalette.PulseHz * 2f);
            Assert.AreEqual(a, StatusTintPalette.GetPulsedTint(a, b, 0f));
            Assert.AreEqual(b, StatusTintPalette.GetPulsedTint(a, b, halfCycle));
        }

        [Test]
        public void SpriteColor_LerpsTowardStatusButKeepsBaseAlpha()
        {
            var baseTint = new Color(0.85f, 0.55f, 1f, 0.9f);
            Color status = StatusTintPalette.GetTint(StatusEffectType.Poison);
            Color composed = StatusTintPalette.GetSpriteColor(baseTint, status);
            Assert.AreEqual(0.9f, composed.a, 1e-4f, "Alpha comes from the base tint");
            Assert.AreEqual(
                Color.Lerp(baseTint, status, StatusTintPalette.TintStrength).g, composed.g, 1e-4f);
        }

        [Test]
        public void FlashColor_KeepsTheStatusHueButBrightens()
        {
            Color flash = StatusTintPalette.GetFlashColor(StatusTintPalette.GetTint(StatusEffectType.Burn));
            // Brighter than the raw tint, but not pure white.
            Assert.AreNotEqual(Color.white, flash);
            Assert.Greater(flash.g, StatusTintPalette.GetTint(StatusEffectType.Burn).g);
        }
    }
}
