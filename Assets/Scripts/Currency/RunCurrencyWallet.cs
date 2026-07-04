using System;
using UnityEngine;

namespace SurveHive.Currency
{
    public sealed class RunCurrencyWallet : MonoBehaviour
    {
        private int _totalCurrency;
        // Meta-shop Currency Gain multiplier, set once at run start.
        private float _gainMultiplier = 1f;

        public event Action<int> OnCurrencyChanged;

        public int TotalCurrency => _totalCurrency;

        public float GainMultiplier => _gainMultiplier;

        public void AddGainPercent(float percent)
        {
            _gainMultiplier += percent / 100f;
        }

        public void AddCurrency(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _totalCurrency += Mathf.RoundToInt(amount * _gainMultiplier);
            OnCurrencyChanged?.Invoke(_totalCurrency);
        }
    }
}
