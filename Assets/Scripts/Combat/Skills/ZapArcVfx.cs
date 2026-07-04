using SurveHive.Core;
using UnityEngine;

namespace SurveHive.Combat.Skills
{
    /// <summary>
    /// Pooled lightning-arc segment for the chain skill: stretched between two
    /// world points (sprite authored 1 unit long, facing right), quick fade-out,
    /// then self-release.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ZapArcVfx : MonoBehaviour
    {
        [SerializeField] private int _poolId;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private float _lifetime = 0.18f;

        private float _elapsed;
        private bool _released;

        public void Show(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;

            transform.position = (from + to) * 0.5f;
            transform.rotation = Quaternion.FromToRotation(Vector3.right, delta);
            transform.localScale = new Vector3(Mathf.Max(0.1f, length), 1f, 1f);

            _elapsed = 0f;
            Color color = _renderer.color;
            color.a = 1f;
            _renderer.color = color;
        }

        private void OnEnable()
        {
            _elapsed = 0f;
            _released = false;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            Color color = _renderer.color;
            color.a = 1f - Mathf.Clamp01(_elapsed / _lifetime);
            _renderer.color = color;

            if (_elapsed >= _lifetime)
            {
                ReleaseSelf();
            }
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
