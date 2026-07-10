using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// PLAN 3B-2d — a lagging "damage trail" for a fill bar. A second image sits
    /// behind the main fill and eases down to the new value after a brief hold, so
    /// a hit flashes a shrinking chunk of the trail colour instead of the bar just
    /// snapping. Heals/spawns snap up instantly (no lag upward). Owned by a bar
    /// MonoBehaviour: call <see cref="SetTarget"/> on health change and
    /// <see cref="Tick"/> from its Update on unscaled time (so it animates while the
    /// game is paused). Zero-GC — value-type maths, and Tick early-outs once settled.
    /// Serializable so the trail <see cref="Image"/> is wired in the inspector.
    /// </summary>
    [System.Serializable]
    public sealed class UIBarTrail
    {
        [SerializeField] private Image _trailImage;

        private const float HoldSeconds = 0.20f;
        private const float DrainPerSecond = 0.85f;

        private float _shown = 1f;
        private float _target = 1f;
        private float _hold;

        /// <summary>Jump the trail straight to a value with no animation.</summary>
        public void Snap(float ratio)
        {
            _shown = _target = Mathf.Clamp01(ratio);
            _hold = 0f;
            Apply();
        }

        /// <summary>New health value: drain toward it (damage) or snap up (heal).</summary>
        public void SetTarget(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            _target = ratio;
            if (ratio >= _shown)
            {
                _shown = ratio;
                _hold = 0f;
            }
            else
            {
                _hold = HoldSeconds;
            }

            Apply();
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (_trailImage == null || _shown <= _target)
            {
                return;
            }

            if (_hold > 0f)
            {
                _hold -= unscaledDeltaTime;
                return;
            }

            _shown = Mathf.MoveTowards(_shown, _target, DrainPerSecond * unscaledDeltaTime);
            Apply();
        }

        private void Apply()
        {
            if (_trailImage != null)
            {
                UIBarFiller.SetFill(_trailImage, _shown);
            }
        }
    }
}
