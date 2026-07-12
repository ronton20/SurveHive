using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// Pulses a UI <see cref="Graphic"/>'s color between two tints to draw the
    /// eye — used by the "Daily Deals!" call-to-action tucked into the Hive Style
    /// panel. Pure cosmetic and zero-GC: caches the target in Awake and lerps by
    /// an unscaled-time sine each frame (keeps flashing regardless of timescale).
    /// The owning Button must use <c>Transition.None</c> so its color block does
    /// not fight the pulse.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class UiFlashPulse : MonoBehaviour
    {
        [SerializeField] private Graphic _target;
        [SerializeField] private Color _dimColor = new Color(1f, 0.765f, 0.043f);
        [SerializeField] private Color _brightColor = Color.white;
        [SerializeField] private float _cyclesPerSecond = 1.6f;

        private float _omega;

        private void Awake()
        {
            if (_target == null)
            {
                _target = GetComponent<Graphic>();
            }

            _omega = _cyclesPerSecond * 2f * Mathf.PI;
        }

        private void OnEnable()
        {
            // Start bright so it reads as active the instant the panel opens.
            if (_target != null)
            {
                _target.color = _brightColor;
            }
        }

        private void Update()
        {
            if (_target == null)
            {
                return;
            }

            float t = 0.5f * (Mathf.Sin(Time.unscaledTime * _omega) + 1f);
            _target.color = Color.Lerp(_dimColor, _brightColor, t);
        }
    }
}
