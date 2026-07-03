using SurveHive.Core;
using UnityEngine;

namespace SurveHive.View
{
    /// <summary>
    /// One-shot pooled particle effect: plays on enable and releases itself back
    /// to its pool once the root system (including children) finishes, with a
    /// hard lifetime cap as a safety net against looping systems.
    /// </summary>
    public sealed class PooledVfx : MonoBehaviour
    {
        [SerializeField] private int _poolId;
        [SerializeField] private ParticleSystem _rootSystem;
        [SerializeField] private float _maxLifetime = 3f;

        private float _elapsed;
        private bool _released;

        private void OnEnable()
        {
            _elapsed = 0f;
            _released = false;

            if (_rootSystem != null)
            {
                _rootSystem.Clear(true);
                _rootSystem.Play(true);
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (_elapsed < _maxLifetime && _rootSystem != null && _rootSystem.IsAlive(true))
            {
                return;
            }

            ReleaseSelf();
        }

        private void ReleaseSelf()
        {
            if (_released)
            {
                return;
            }

            _released = true;

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Release(_poolId, gameObject);
            }
        }
    }
}
