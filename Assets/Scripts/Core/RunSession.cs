using System;
using SurveHive.Currency;
using SurveHive.Data;
using UnityEngine;

namespace SurveHive.Core
{
    public sealed class RunSession : MonoBehaviour
    {
        public static RunSession Instance { get; private set; }

        [SerializeField] private RunCurrencyWallet _currencyWallet;
        [SerializeField] private RuntimeMetaProgressionStoreSO _metaProgressionStore;

        private int _killCount;
        private float _elapsedSeconds;

        public event Action<int> OnKillCountChanged;

        public RunCurrencyWallet Currency => _currencyWallet;

        public int KillCount => _killCount;

        // Active run time: scaled deltaTime, so pauses and hit-stop don't count.
        public float ElapsedSeconds => _elapsedSeconds;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            _elapsedSeconds += Time.deltaTime;
        }

        public void RegisterKill()
        {
            _killCount++;
            OnKillCountChanged?.Invoke(_killCount);
        }

        public void EndRun()
        {
            if (_metaProgressionStore != null)
            {
                _metaProgressionStore.BankRunCurrency(_currencyWallet.TotalCurrency);
            }
        }
    }
}
