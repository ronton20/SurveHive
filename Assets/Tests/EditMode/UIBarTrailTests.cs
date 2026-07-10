using System.Reflection;
using NUnit.Framework;
using SurveHive.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.Tests
{
    /// <summary>
    /// PLAN 3B-2d — the lagging damage-trail behaviour. Damage holds then drains to
    /// the new value (so a hit flashes a shrinking chunk); heals snap up instantly.
    /// Observed through the trail Image's driven anchorMax.x (what UIBarFiller sets).
    /// </summary>
    public sealed class UIBarTrailTests
    {
        private static Image MakeFill()
        {
            var go = new GameObject("trail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Image image = go.GetComponent<Image>();
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            return image;
        }

        private static UIBarTrail Wire(Image image)
        {
            var trail = new UIBarTrail();
            typeof(UIBarTrail)
                .GetField("_trailImage", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(trail, image);
            return trail;
        }

        private static float Shown(Image image)
        {
            return image.rectTransform.anchorMax.x;
        }

        [Test]
        public void Damage_HoldsThenDrainsToTarget()
        {
            Image image = MakeFill();
            UIBarTrail trail = Wire(image);
            trail.Snap(1f);

            trail.SetTarget(0.4f);
            // During the hold window the trail has not moved yet.
            trail.Tick(0.05f);
            Assert.AreEqual(1f, Shown(image), 1e-3f, "Trail should hold before draining.");

            // Plenty of time to clear the hold and drain fully.
            for (int i = 0; i < 200; i++)
            {
                trail.Tick(0.05f);
            }

            Assert.AreEqual(0.4f, Shown(image), 1e-3f, "Trail should settle on the damaged value.");
            Object.DestroyImmediate(image.gameObject);
        }

        [Test]
        public void Heal_SnapsUpImmediately()
        {
            Image image = MakeFill();
            UIBarTrail trail = Wire(image);
            trail.Snap(0.3f);

            trail.SetTarget(0.9f);
            Assert.AreEqual(0.9f, Shown(image), 1e-3f, "Healing should snap the trail up with no lag.");
            Object.DestroyImmediate(image.gameObject);
        }
    }
}
