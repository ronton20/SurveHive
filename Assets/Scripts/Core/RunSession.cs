using System;
using SurveHive.Currency;
using SurveHive.Data;
using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Core
{
    public sealed class RunSession : MonoBehaviour
    {
        public static RunSession Instance { get; private set; }

        [SerializeField] private RunCurrencyWallet _currencyWallet;
        [SerializeField] private MetaProgressionStoreSO _metaProgressionStore;
        [SerializeField] private PlayerExperience _playerExperience;

        private int _killCount;
        private float _elapsedSeconds;
        private bool _runEnded;

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

        public void EndRun(bool victory)
        {
            // Guard against double-ending (e.g. the player dying to a stray hit
            // in the same frame the Queen dies) — only the first outcome banks.
            if (_runEnded)
            {
                return;
            }

            _runEnded = true;

            if (_metaProgressionStore == null)
            {
                return;
            }

            _metaProgressionStore.BankRunCurrency(_currencyWallet.TotalCurrency);
            int level = _playerExperience != null ? _playerExperience.CurrentLevel : 0;
            _metaProgressionStore.RecordRunResult(
                Mathf.FloorToInt(_elapsedSeconds), _killCount, level, victory);
        }
    }
}
