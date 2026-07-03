using SurveHive.Health;
using UnityEngine;

namespace SurveHive.View
{
    /// <summary>
    /// Flashes the sprite white for a few frames whenever the paired
    /// <see cref="HealthComponent"/> takes damage. Requires the renderer to use
    /// the SurveHive/SpriteFlash shader; drives it through a
    /// MaterialPropertyBlock so no material instances are created.
    /// </summary>
    public sealed class HitFlash : MonoBehaviour
    {
        private static readonly int FlashAmountProperty = Shader.PropertyToID("_FlashAmount");

        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private HealthComponent _health;
        [SerializeField] private float _flashDuration = 0.08f;

        private MaterialPropertyBlock _propertyBlock;
        private float _flashRemaining;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _health.OnDamaged += HandleDamaged;
            _flashRemaining = 0f;
            SetFlash(0f);
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
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
