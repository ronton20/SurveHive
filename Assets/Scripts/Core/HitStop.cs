using UnityEngine;

namespace SurveHive.Core
{
    /// <summary>
    /// Micro time-freeze on impactful moments (elite kills). Counts down on
    /// unscaled time and restores the time scale afterwards, unless a real pause
    /// (<see cref="GamePause"/>) took over in the meantime.
    /// </summary>
    public sealed class HitStop : MonoBehaviour
    {
        public static HitStop Instance { get; private set; }

        [SerializeField] private float _maxDuration = 0.12f;

        private float _remaining;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Request(float duration)
        {
            if (GamePause.IsPaused || duration <= 0f)
            {
                return;
            }

            _remaining = Mathf.Max(_remaining, Mathf.Min(duration, _maxDuration));
            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (_remaining <= 0f)
            {
                return;
            }

            _remaining -= Time.unscaledDeltaTime;
            if (_remaining <= 0f && !GamePause.IsPaused)
            {
                Time.timeScale = 1f;
            }
        }
    }
}
