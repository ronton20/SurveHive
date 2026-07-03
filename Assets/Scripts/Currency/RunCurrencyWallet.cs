using System;
using UnityEngine;

namespace SurveHive.Currency
{
    public sealed class RunCurrencyWallet : MonoBehaviour
    {
        private int _totalCurrency;

        public event Action<int> OnCurrencyChanged;

        public int TotalCurrency => _totalCurrency;

        public void AddCurrency(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _totalCurrency += amount;
            OnCurrencyChanged?.Invoke(_totalCurrency);
        }
    }
}
