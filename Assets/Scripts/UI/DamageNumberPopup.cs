using System.Text;
using SurveHive.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    public sealed class DamageNumberPopup : MonoBehaviour
    {
        [SerializeField] private Text _text;
        [SerializeField] private int _poolId;
        [SerializeField] private float _lifetime = 0.7f;
        [SerializeField] private float _riseSpeed = 1.5f;

        private readonly StringBuilder _stringBuilder = new StringBuilder(8);
        private float _elapsed;
        private bool _released;

        public void Show(float damageAmount)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append(Mathf.RoundToInt(damageAmount));
            _text.text = _stringBuilder.ToString();
        }

        private void OnEnable()
        {
            _elapsed = 0f;
            _released = false;

            Color color = _text.color;
            color.a = 1f;
            _text.color = color;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.position += Vector3.up * (_riseSpeed * Time.deltaTime);

            float remaining = 1f - Mathf.Clamp01(_elapsed / _lifetime);
            Color color = _text.color;
            color.a = remaining;
            _text.color = color;

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
