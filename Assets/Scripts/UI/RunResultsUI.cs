using System.Text;
using SurveHive.Core;
using SurveHive.Currency;
using SurveHive.Progression;
using TMPro;
using UnityEngine;

namespace SurveHive.UI
{
    /// <summary>
    /// Fills the end-of-run stats block (time survived, kills, level, currency
    /// banked) on both the death and victory panels. Populates in OnEnable —
    /// the panels are activated exactly when the run ends.
    /// </summary>
    public sealed class RunResultsUI : MonoBehaviour
    {
        [SerializeField] private RunSession _session;
        [SerializeField] private PlayerExperience _playerExperience;
        [SerializeField] private RunCurrencyWallet _wallet;
        [SerializeField] private TMP_Text _statsText;

        private readonly StringBuilder _builder = new StringBuilder(128);

        private void OnEnable()
        {
            if (_statsText == null)
            {
                return;
            }

            _builder.Clear();

            if (_session != null)
            {
                int totalSeconds = Mathf.FloorToInt(_session.ElapsedSeconds);
                _builder.Append(Loc.Get(LocKeys.ResultsTime));
                _builder.Append(totalSeconds / 60);
                _builder.Append(':');
                int seconds = totalSeconds % 60;
                if (seconds < 10)
                {
                    _builder.Append('0');
                }

                _builder.Append(seconds);
                _builder.Append('\n');
                _builder.Append(Loc.Get(LocKeys.ResultsKills));
                _builder.Append(_session.KillCount);
                _builder.Append('\n');
            }

            if (_playerExperience != null)
            {
                _builder.Append(Loc.Get(LocKeys.ResultsLevel));
                _builder.Append(_playerExperience.CurrentLevel);
                _builder.Append('\n');
            }

            if (_wallet != null)
            {
                _builder.Append(Loc.Get(LocKeys.ResultsHoneyBanked));
                _builder.Append(_wallet.TotalCurrency);
            }

            // One end-of-run allocation (not a hot path); .text keeps the value
            // readable for other systems, unlike SetText(StringBuilder).
            _statsText.text = _builder.ToString();
        }
    }
}
