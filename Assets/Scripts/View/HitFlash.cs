using SurveHive.Health;
using UnityEngine;

namespace SurveHive.View
{
    /// <summary>
    /// Flashes the sprite for a few frames whenever the paired
    /// <see cref="HealthComponent"/> takes damage. Requires the renderer to use
    /// the SurveHive/SpriteFlash shader; drives it through a
    /// MaterialPropertyBlock so no material instances are created. The flash
    /// color defaults to white; status effects hue-shift it via
    /// <see cref="SetFlashColor"/> so statuses stay readable mid-flash (PLAN 2A).
    /// </summary>
    public sealed class HitFlash : MonoBehaviour
    {
        private static readonly int FlashAmountProperty = Shader.PropertyToID("_FlashAmount");
        private static readonly int FlashColorProperty = Shader.PropertyToID("_FlashColor");

        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private HealthComponent _health;
        [SerializeField] private float _flashDuration = 0.12f;

        private MaterialPropertyBlock _propertyBlock;
        private float _flashRemaining;
        private Color _flashColor = Color.white;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _health.OnDamaged += HandleDamaged;
            _flashRemaining = 0f;
            _flashColor = Color.white;
            SetFlash(0f);
        }

        /// <summary>Sets the color the sprite flashes toward (pooled-safe: resets to white on enable).</summary>
        public void SetFlashColor(Color color)
        {
            if (_flashColor == color)
            {
                return;
            }

            _flashColor = color;
            if (_flashRemaining > 0f)
            {
                SetFlash(Mathf.Clamp01(_flashRemaining / _flashDuration));
            }
        }

        private void OnDisable()
        {
            _health.OnDamaged -= HandleDamaged;
        }

        private void HandleDamaged(float amount)
        {
            _flashRemaining = _flashDuration;
            SetFlash(1f);
        }

        private void Update()
        {
            if (_flashRemaining <= 0f)
            {
                return;
            }

            _flashRemaining -= Time.deltaTime;
            SetFlash(Mathf.Clamp01(_flashRemaining / _flashDuration));
        }

        private void SetFlash(float amount)
        {
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(FlashAmountProperty, amount);
            _propertyBlock.SetColor(FlashColorProperty, _flashColor);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
