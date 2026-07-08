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

        // Chosen on the menu's world-select screen and read by the run scene
        // after load; a plain static survives the scene swap without a carrier
        // object. Defaults to Normal so editor boots straight into a run scene
        // (and tests) play unscaled.
        public static DifficultyTier SelectedDifficulty { get; set; } = DifficultyTier.Normal;

        [SerializeField] private RunCurrencyWallet _currencyWallet;
        [SerializeField] private MetaProgressionStoreSO _metaProgressionStore;
        [SerializeField] private PlayerExperience _playerExperience;
        [SerializeField] private DifficultySO _difficulty;
        // Identifies this scene's stage in the save's clear record (difficulty
        // unlocks). New worlds set their own id via their builder pass.
        [SerializeField] private string _stageId = "Beehive";

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

            // Difficulty honey compensation, applied once so every pickup this
            // run pays out at the tier's rate (stacks with the meta-shop gain
            // multiplier, which MetaUpgradeApplier adds separately).
            if (_currencyWallet != null && _difficulty != null)
            {
                _currencyWallet.SetDifficultyGainMultiplier(
                    _difficulty.GetSettings(SelectedDifficulty).honeyGainMultiplier);
            }
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

            // A victory marks this stage cleared on the run's difficulty,
            // feeding the Hard/Extreme unlock gates.
            if (victory)
            {
                _metaProgressionStore.RecordStageClear(_stageId, (int)SelectedDifficulty);
            }
        }
    }
}
