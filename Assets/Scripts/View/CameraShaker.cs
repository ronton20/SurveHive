using UnityEngine;

namespace SurveHive.View
{
    /// <summary>
    /// Produces a decaying random screen-shake offset that
    /// <see cref="Player.CameraFollow"/> adds on top of its follow position.
    /// Amplitude is clamped so shakes never wreck pixel-perfect readability.
    /// Runs on unscaled time so hit-stop doesn't freeze the shake itself.
    /// </summary>
    public sealed class CameraShaker : MonoBehaviour
    {
        // Pixel-perfect snapping quantizes offsets to the 1/16-unit grid, so
        // amplitudes need to be several pixels to actually read on screen.
        [SerializeField] private float _maxAmplitude = 0.5f;
        [SerializeField] private float _decayPerSecond = 1.2f;

        private float _amplitude;

        public Vector3 CurrentOffset { get; private set; }

        public void Shake(float amplitude)
        {
            _amplitude = Mathf.Min(_maxAmplitude, Mathf.Max(_amplitude, amplitude));
        }

        private void Update()
        {
            if (_amplitude <= 0f)
            {
                CurrentOffset = Vector3.zero;
                return;
            }

            Vector2 random = Random.insideUnitCircle * _amplitude;
            CurrentOffset = new Vector3(random.x, random.y, 0f);
            _amplitude = Mathf.MoveTowards(_amplitude, 0f, _decayPerSecond * Time.unscaledDeltaTime);
        }
    }
}
