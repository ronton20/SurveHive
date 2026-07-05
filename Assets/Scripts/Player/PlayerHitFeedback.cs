using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Health;
using SurveHive.View;
using UnityEngine;

namespace SurveHive.Player
{
    /// <summary>
    /// Screen-shake + audio feedback when the player gets hurt or dies. The
    /// white hit flash itself comes from the shared <see cref="HitFlash"/> component.
    /// </summary>
    public sealed class PlayerHitFeedback : MonoBehaviour
    {
        [SerializeField] private HealthComponent _health;
        [SerializeField] private CameraShaker _shaker;
        [SerializeField] private float _hurtShakeAmplitude = 0.3f;
        [SerializeField] private float _deathShakeAmplitude = 0.5f;

        private void OnEnable()
        {
            _health.OnDamaged += HandleDamaged;
            _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            _health.OnDamaged -= HandleDamaged;
            _health.OnDied -= HandleDied;
        }

        private void HandleDamaged(float amount)
        {
            if (_shaker != null)
            {
                _shaker.Shake(_hurtShakeAmplitude);
            }

            if (AudioService.Instance != null)
            {
                AudioService.Instance.PlaySfx(SfxId.PlayerHurt);
            }
        }

        private void HandleDied()
        {
            if (_shaker != null)
            {
                _shaker.Shake(_deathShakeAmplitude);
            }

            if (AudioService.Instance != null)
            {
                AudioService.Instance.PlaySfx(SfxId.PlayerDeath);
            }
        }
    }
}
