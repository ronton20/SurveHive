using NUnit.Framework;
using SurveHive.UI;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// Shared-tooltip placement (<see cref="TooltipLayout.Clamp"/>): the panel
    /// (pivot top-left, canvas-centre coordinates) must stay fully on screen
    /// wherever the mouse goes — that's the whole point of the rework, the old
    /// pinned difficulty tooltip sat off-screen.
    /// </summary>
    public sealed class TooltipLayoutTests
    {
        private static readonly Vector2 Canvas = new Vector2(1920f, 1080f);
        private static readonly Vector2 Panel = new Vector2(400f, 200f);

        [Test]
        public void Clamp_Inside_IsUntouched()
        {
            var desired = new Vector2(100f, 50f);

            Assert.AreEqual(desired, TooltipLayout.Clamp(desired, Panel, Canvas));
        }

        [Test]
        public void Clamp_RightEdge_PullsPanelFullyInside()
        {
            Vector2 result = TooltipLayout.Clamp(new Vector2(900f, 0f), Panel, Canvas);

            Assert.AreEqual(960f - Panel.x, result.x);
            Assert.AreEqual(0f, result.y);
        }

        [Test]
        public void Clamp_BottomEdge_LiftsPanelFullyInside()
        {
            Vector2 result = TooltipLayout.Clamp(new Vector2(0f, -520f), Panel, Canvas);

            Assert.AreEqual(0f, result.x);
            Assert.AreEqual(-540f + Panel.y, result.y);
        }

        [Test]
        public void Clamp_TopLeftCorner_PinsToEdges()
        {
            Vector2 result = TooltipLayout.Clamp(new Vector2(-5000f, 5000f), Panel, Canvas);

            Assert.AreEqual(-960f, result.x);
            Assert.AreEqual(540f, result.y);
        }

        [Test]
        public void Clamp_PanelLargerThanCanvas_PinsTopLeft()
        {
            var huge = new Vector2(2500f, 1500f);

            Vector2 result = TooltipLayout.Clamp(Vector2.zero, huge, Canvas);

            Assert.AreEqual(-960f, result.x);
            Assert.AreEqual(540f, result.y);
        }
    }
}
