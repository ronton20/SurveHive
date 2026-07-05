using System;
using System.Collections;
using SurveHive.Player;
using SurveHive.View;
using UnityEngine;

namespace SurveHive.Core
{
    /// <summary>
    /// Cinematic boss/miniboss death beat (Phase 2C): drops into slow-motion for
    /// the death animation, makes the player invulnerable, fires a shockwave +
    /// screen shake, and holds downstream events (timeline resume / victory /
    /// rewards) until it finishes. Driven on unscaled time and cooperates with
    /// <see cref="GamePause"/> (a real pause always wins the time scale).
    /// </summary>
    public sealed class BossDeathSequence : MonoBehaviour
    {
        public static BossDeathSequence Instance { get; private set; }

        /// <summary>True while the beat is playing (HitStop yields to it).</summary>
        public static bool IsPlaying { get; private set; }

        [SerializeField] private float _slowMoScale = 0.25f;
        // Real-time length of the beat (covers the death animation).
        [SerializeField] private float _durationSeconds = 1.5f;
        [SerializeField] private CameraShaker _shaker;
        [SerializeField] private float _shakeAmplitude = 0.6f;
        [SerializeField] private int _shockwavePoolId = -1;

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

            IsPlaying = false;
        }

        /// <summary>
        /// Plays the beat at <paramref name="position"/>, invoking
        /// <paramref name="onComplete"/> once it finishes. If a beat is already
        /// running, completes immediately (avoids stacking on double deaths).
        /// </summary>
        public void Play(Vector3 position, Action onComplete)
        {
            if (IsPlaying)
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(Run(position, onComplete));
        }

        private IEnumerator Run(Vector3 position, Action onComplete)
        {
            IsPlaying = true;

            if (_shockwavePoolId >= 0 && PoolManager.Instance != null)
            {
                PoolManager.Instance.Get(_shockwavePoolId, position, Quaternion.identity);
            }

            if (_shaker != null)
            {
                _shaker.Shake(_shakeAmplitude);
            }

            if (PlayerContext.Health != null)
            {
                PlayerContext.Health.SetInvulnerable(true);
            }

            // Don't fight a real pause for the time scale.
            if (!GamePause.IsPaused)
            {
                Time.timeScale = _slowMoScale;
            }

            float elapsed = 0f;
            while (elapsed < _durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = GamePause.IsPaused ? 0f : 1f;
            if (PlayerContext.Health != null)
            {
                PlayerContext.Health.SetInvulnerable(false);
            }

            IsPlaying = false;
            onComplete?.Invoke();
        }
    }
}
