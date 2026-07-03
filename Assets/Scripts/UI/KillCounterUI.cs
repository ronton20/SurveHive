using System.Text;
using SurveHive.Core;
using TMPro;
using UnityEngine;

namespace SurveHive.UI
{
    public sealed class KillCounterUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private RunSession _session;

        private readonly StringBuilder _stringBuilder = new StringBuilder(16);

        private void OnEnable()
        {
            _session.OnKillCountChanged += HandleKillCountChanged;
            HandleKillCountChanged(_session.KillCount);
        }

        private void OnDisable()
        {
            _session.OnKillCountChanged -= HandleKillCountChanged;
        }

        private void HandleKillCountChanged(int killCount)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append(killCount);
            _text.SetText(_stringBuilder);
        }
    }
}
