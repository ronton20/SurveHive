using System.Text;
using SurveHive.Currency;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    public sealed class CurrencyCounterUI : MonoBehaviour
    {
        [SerializeField] private Text _currencyText;
        [SerializeField] private RunCurrencyWallet _wallet;

        private readonly StringBuilder _stringBuilder = new StringBuilder(16);

        private void OnEnable()
        {
            _wallet.OnCurrencyChanged += HandleCurrencyChanged;
            HandleCurrencyChanged(_wallet.TotalCurrency);
        }

        private void OnDisable()
        {
            _wallet.OnCurrencyChanged -= HandleCurrencyChanged;
        }

        private void HandleCurrencyChanged(int total)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append(total);
            _currencyText.text = _stringBuilder.ToString();
        }
    }
}
