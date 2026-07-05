using System.Text;
using SurveHive.Data;
using SurveHive.Stage;
using TMPro;
using UnityEngine;

namespace SurveHive.UI
{
    /// <summary>
    /// Upper-screen "incoming" warning banner (Combat 2.0 Phase 2A): when the
    /// stage director signals an event ~5s out, shows the threat name and a
    /// live countdown, fading in/out. Subscribes to <see cref="StageDirector"/>.
    /// </summary>
    public sealed class WaveWarningBanner : MonoBehaviour
    {
        [SerializeField] private StageDirector _director;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeSeconds = 0.35f;

        private string _title = string.Empty;
        private float _remaining;
        private float _total;
        private int _lastShownSecond = -1;
        private readonly StringBuilder _builder = new StringBuilder(48);

        private void Awake()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        private void OnEnable()
        {
            if (_director != null)
            {
                _director.OnStageWarning += HandleWarning;
            }
        }

        private void OnDisable()
        {
            if (_director != null)
            {
                _director.OnStageWarning -= HandleWarning;
            }
        }

        private void HandleWarning(StageTimelineEvent stageEvent, float leadSeconds)
        {
            _title = BuildTitle(stageEvent);
            _total = leadSeconds;
            _remaining = leadSeconds;
            _lastShownSecond = -1;
        }

        private static string BuildTitle(StageTimelineEvent stageEvent)
        {
            switch (stageEvent.Type)
            {
                case StageEventType.Miniboss:
                case StageEventType.FinalBoss:
                    return stageEvent.EnemyStats != null
                        ? stageEvent.EnemyStats.DisplayName.ToUpperInvariant()
                        : "BOSS";
                default:
                    return "DANGER WAVE";
            }
        }

        private void Update()
        {
            if (_remaining <= 0f)
            {
                if (_canvasGroup != null && _canvasGroup.alpha > 0f)
                {
                    _canvasGroup.alpha = 0f;
                }

                return;
            }

            _remaining -= Time.deltaTime;

            int seconds = Mathf.Max(0, Mathf.CeilToInt(_remaining));
            if (seconds != _lastShownSecond)
            {
                _lastShownSecond = seconds;
                RefreshText(seconds);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = ComputeAlpha();
            }
        }

        private void RefreshText(int seconds)
        {
            if (_text == null)
            {
                return;
            }

            _builder.Clear();
            _builder.Append(_title).Append("\nINCOMING IN ").Append(seconds).Append('s');
            _text.text = _builder.ToString();
        }

        private float ComputeAlpha()
        {
            float elapsed = _total - _remaining;
            if (elapsed < _fadeSeconds)
            {
                return Mathf.Clamp01(elapsed / _fadeSeconds);
            }

            if (_remaining < _fadeSeconds)
            {
                return Mathf.Clamp01(_remaining / _fadeSeconds);
            }

            return 1f;
        }
    }
}
