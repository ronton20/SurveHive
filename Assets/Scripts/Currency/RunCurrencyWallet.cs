using System;
using UnityEngine;

namespace SurveHive.Currency
{
    public sealed class RunCurrencyWallet : MonoBehaviour
    {
        private int _totalCurrency;
        // Premium currency, Royal Jelly (PLAN 5B). Deliberately untouched by the
        // gain multipliers below — jelly stays scarce at every difficulty/meta level.
        private int _totalJelly;
        // Meta-shop Currency Gain multiplier, set once at run start.
        private float _gainMultiplier = 1f;
        // Difficulty-tier honey compensation (PLAN 1B), set once at run start.
        private float _difficultyGainMultiplier = 1f;

        public event Action<int> OnCurrencyChanged;

        public event Action<int> OnJellyChanged;

        public int TotalCurrency => _totalCurrency;

        public int TotalJelly => _totalJelly;

        public float GainMultiplier => _gainMultiplier;

        public float DifficultyGainMultiplier => _difficultyGainMultiplier;

        public void AddGainPercent(float percent)
        {
            _gainMultiplier += percent / 100f;
        }

        public void SetDifficultyGainMultiplier(float multiplier)
        {
            _difficultyGainMultiplier = Mathf.Max(0f, multiplier);
        }

        public void AddCurrency(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _totalCurrency += Mathf.RoundToInt(amount * _gainMultiplier * _difficultyGainMultiplier);
            OnCurrencyChanged?.Invoke(_totalCurrency);
        }

        public void AddJelly(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _totalJelly += amount;
            OnJellyChanged?.Invoke(_totalJelly);
        }
    }
}
