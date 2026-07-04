using TMPro;
using UnityEngine;

namespace SurveHive.UI
{
    /// <summary>
    /// Center-screen announcement banner ("QUEEN BEE"): fades in, holds, fades
    /// out. Driven by scaled time; zero allocations after Show.
    /// </summary>
    public sealed class BossBannerUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeSeconds = 0.35f;
        [SerializeField] private float _holdSeconds = 2.2f;

        private float _remaining;
        private float _total;

        private void Awake()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        public void Show(string message)
        {
            if (_text != null)
            {
                _text.text = message;
            }

            _total = (_fadeSeconds * 2f) + _holdSeconds;
            _remaining = _total;
        }

        private void Update()
        {
            if (_remaining <= 0f || _canvasGroup == null)
            {
                return;
            }

            _remaining -= Time.deltaTime;
            float elapsed = _total - _remaining;

            float alpha;
            if (elapsed < _fadeSeconds)
            {
                alpha = elapsed / _fadeSeconds;
            }
            else if (_remaining < _fadeSeconds)
            {
                alpha = Mathf.Max(0f, _remaining / _fadeSeconds);
            }
            else
            {
                alpha = 1f;
            }

            _canvasGroup.alpha = alpha;
        }
    }
}
