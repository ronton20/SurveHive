using System.Collections;
using SurveHive.Core;
using UnityEngine;

namespace SurveHive.UI
{
    /// <summary>
    /// Reusable, allocation-light UI transition coroutines (Phase 3B-2c). Callers
    /// host these on an always-active MonoBehaviour (<c>StartCoroutine</c>) so a
    /// panel that starts inactive doesn't need its own coroutine runner — the
    /// coroutine only touches the target's <see cref="CanvasGroup"/> / transform.
    ///
    /// All timing uses <see cref="Time.unscaledDeltaTime"/>: the level-up and pause
    /// screens play while the game is frozen at <c>timeScale 0</c>, so scaled time
    /// would leave them stuck at their start frame.
    /// </summary>
    public static class UiAnim
    {
        /// <summary>Default fade time for panel reveals (seconds, unscaled).</summary>
        public const float FadeDuration = 0.16f;

        /// <summary>
        /// Fades <paramref name="group"/> from its current alpha to 1. The panel is
        /// expected to already be active and its raycast/interactable flags already
        /// set by the caller (so it's clickable for the whole fade, tests included).
        /// </summary>
        public static IEnumerator FadeIn(CanvasGroup group, float duration)
        {
            float start = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float e = Easing.OutCubic(Mathf.Clamp01(elapsed / duration));
                group.alpha = Mathf.LerpUnclamped(start, 1f, e);
                yield return null;
            }

            group.alpha = 1f;
        }

        /// <summary>
        /// Fades <paramref name="group"/> to 0. Interactivity is the caller's job —
        /// it should drop <c>interactable</c>/<c>blocksRaycasts</c> up front so input
        /// falls through immediately rather than waiting out the fade.
        /// </summary>
        public static IEnumerator FadeOut(CanvasGroup group, float duration)
        {
            float start = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float e = Easing.OutCubic(Mathf.Clamp01(elapsed / duration));
                group.alpha = Mathf.LerpUnclamped(start, 0f, e);
                yield return null;
            }

            group.alpha = 0f;
        }
    }
}
