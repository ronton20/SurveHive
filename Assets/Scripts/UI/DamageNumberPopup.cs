using System.Text;
using SurveHive.Core;
using TMPro;
using UnityEngine;

namespace SurveHive.UI
{
    public sealed class DamageNumberPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private int _poolId;
        [SerializeField] private float _lifetime = 0.7f;
        [SerializeField] private float _riseSpeed = 1.5f;

        private readonly StringBuilder _stringBuilder = new StringBuilder(8);
        private float _elapsed;
        private bool _released;
        private float _baseFontSize;
        private bool _baseFontSizeCached;

        public void Show(float damageAmount)
        {
            Show(damageAmount, Color.white, 1f);
        }

        // Pooled instances carry style from their previous life, so every Show
        // sets color and size explicitly (crit = gold/large, DoT = tinted/small).
        public void Show(float damageAmount, Color color, float sizeMultiplier)
        {
            if (!_baseFontSizeCached)
            {
                _baseFontSize = _text.fontSize;
                _baseFontSizeCached = true;
            }

            _stringBuilder.Clear();
            _stringBuilder.Append(Mathf.RoundToInt(damageAmount));
            _text.SetText(_stringBuilder);
            _text.color = color;
            _text.fontSize = _baseFontSize * sizeMultiplier;
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
