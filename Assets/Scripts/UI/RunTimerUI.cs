using System.Text;
using SurveHive.Core;
using TMPro;
using UnityEngine;

namespace SurveHive.UI
{
    /// <summary>
    /// mm:ss run clock. Rebuilds its string only when the displayed second
    /// actually changes, so steady-state frames allocate nothing.
    /// </summary>
    public sealed class RunTimerUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private RunSession _session;

        private readonly StringBuilder _stringBuilder = new StringBuilder(8);
        private int _lastShownSeconds = -1;

        private void Update()
        {
            int totalSeconds = (int)_session.ElapsedSeconds;
            if (totalSeconds == _lastShownSeconds)
            {
                return;
            }

            _lastShownSeconds = totalSeconds;

            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            _stringBuilder.Clear();
            _stringBuilder.Append(minutes);
            _stringBuilder.Append(':');
            if (seconds < 10)
            {
                _stringBuilder.Append('0');
            }

            _stringBuilder.Append(seconds);
            _text.SetText(_stringBuilder);
        }
    }
}
