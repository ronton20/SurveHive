using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// Drives a horizontally-stretched fill <see cref="Image"/> by moving its
    /// RectTransform right edge (anchorMax.x). This works even when the Image has
    /// no source sprite assigned — unlike <see cref="Image.fillAmount"/>, which is
    /// silently ignored without a sprite and always renders a full quad.
    /// Assumes the fill anchors from the left (anchorMin.x == 0) with zero sizeDelta.
    /// </summary>
    public static class UIBarFiller
    {
        public static void SetFill(Image fillImage, float ratio)
        {
            RectTransform rect = fillImage.rectTransform;
            Vector2 anchorMax = rect.anchorMax;
            anchorMax.x = Mathf.Clamp01(ratio);
            rect.anchorMax = anchorMax;
        }
    }
}
