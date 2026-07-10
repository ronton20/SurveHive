using UnityEngine;

namespace SurveHive.UI
{
    /// <summary>
    /// Pure placement math for the shared tooltip (EditMode-tested): keeps a
    /// pivot-(0,1) panel fully inside the canvas. Coordinates are canvas-local
    /// with the origin at the canvas centre (the tooltip is centre-anchored),
    /// so the canvas spans ±size/2 on each axis.
    /// </summary>
    public static class TooltipLayout
    {
        /// <summary>
        /// Clamps the desired top-left position of a panel so the whole panel
        /// stays inside the canvas. A panel larger than the canvas pins to the
        /// top-left edge.
        /// </summary>
        public static Vector2 Clamp(Vector2 desiredTopLeft, Vector2 panelSize, Vector2 canvasSize)
        {
            Vector2 halfCanvas = canvasSize * 0.5f;

            float minX = -halfCanvas.x;
            float maxX = halfCanvas.x - panelSize.x;
            float minY = -halfCanvas.y + panelSize.y;
            float maxY = halfCanvas.y;

            return new Vector2(
                maxX < minX ? minX : Mathf.Clamp(desiredTopLeft.x, minX, maxX),
                minY > maxY ? maxY : Mathf.Clamp(desiredTopLeft.y, minY, maxY));
        }
    }
}
