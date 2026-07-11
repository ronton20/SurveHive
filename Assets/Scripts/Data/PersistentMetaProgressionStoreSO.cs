using SurveHive.Core;
using SurveHive.Persistence;
using UnityEngine;

namespace SurveHive.Data
{
    /// <summary>
    /// Save-file-backed meta store: lazily loads the JSON save on first access
    /// (a corrupt/missing file just yields a fresh <see cref="SaveData"/>) and
    /// persists after every mutation. Also owns the saved settings + best-run
    /// blocks for the menus/pause phases.
    /// </summary>
    [CreateAssetMenu(menuName = "SurveHive/Persistent Meta Progression Store", fileName = "PersistentMetaProgressionStore")]
    public sealed class PersistentMetaProgressionStoreSO : MetaProgressionStoreSO
    {
        private MetaProgressionState _state;
        private SaveData _save;

        private void OnEnable()
        {
            SaveFileStore.PathChanged += InvalidateCache;
        }

        private void OnDisable()
        {
            SaveFileStore.PathChanged -= InvalidateCache;
        }

        private void InvalidateCache()
        {
            _state = null;
            _save = null;
        }

        public override int BankedCurrency
        {
            get
            {
                EnsureLoaded();
                return _state.BankedCurrency;
            }
        }

        public SettingsData Settings
        {
            get
            {
                EnsureLoaded();
                return _save.settings;
            }
        }

        public BestRunData BestRun
        {
            get
            {
                EnsureLoaded();
                return _save.bestRun;
            }
        }

        /// <summary>Last-selected difficulty tier; writes persist immediately.</summary>
        public DifficultyTier SelectedDifficulty
        {
            get
            {
                EnsureLoaded();
                return (DifficultyTier)_save.selectedDifficulty;
            }
            set
            {
                EnsureLoaded();
                if (_save.selectedDifficulty == (int)value)
                {
                    return;
                }

                _save.selectedDifficulty = (int)value;
                Persist();
            }
        }

        public override void BankRunCurrency(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            EnsureLoaded();
            _state.Bank(amount);
            Persist();
        }

        public override bool TrySpendCurrency(int amount)
        {
            EnsureLoaded();
            if (!_state.TrySpend(amount))
            {
                return false;
            }

            Persist();
            return true;
        }

        public override int BankedJelly
        {
            get
            {
                EnsureLoaded();
                return _state.BankedJelly;
            }
        }

        public override void BankJelly(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            EnsureLoaded();
            _state.BankJelly(amount);
            Persist();
        }

        public override bool TrySpendJelly(int amount)
        {
            EnsureLoaded();
            if (!_state.TrySpendJelly(amount))
            {
                return false;
            }

            Persist();
            return true;
        }

        public override int GetUpgradeRank(string upgradeId)
        {
            EnsureLoaded();
            return _state.GetRank(upgradeId);
        }

        public override void SetUpgradeRank(string upgradeId, int rank)
        {
            EnsureLoaded();
            _state.SetRank(upgradeId, rank);
            Persist();
        }

        public override void RecordRunResult(int timeSeconds, int kills, int level, bool victory)
        {
            EnsureLoaded();
            BestRunData best = _save.bestRun;
            best.runsPlayed++;
            if (victory)
            {
                best.victories++;
            }

            best.bestTimeSeconds = Mathf.Max(best.bestTimeSeconds, timeSeconds);
            best.bestKills = Mathf.Max(best.bestKills, kills);
            best.bestLevel = Mathf.Max(best.bestLevel, level);
            Persist();
        }

        public override void RecordStageClear(string stageId, int difficulty)
        {
            EnsureLoaded();
            if (_state.HasStageClear(stageId, difficulty))
            {
                return;
            }

            _state.RecordStageClear(stageId, difficulty);
            Persist();
        }

        public override bool HasStageClear(string stageId, int difficulty)
        {
            EnsureLoaded();
            return _state.HasStageClear(stageId, difficulty);
        }

        public override bool IsCodexUnlocked(string entryId)
        {
            EnsureLoaded();
            return _state.IsCodexUnlocked(entryId);
        }

        public override void UnlockCodexEntries(System.Collections.Generic.List<string> entryIds)
        {
            if (entryIds == null || entryIds.Count == 0)
            {
                return;
            }

            EnsureLoaded();
            bool changed = false;
            for (int i = 0; i < entryIds.Count; i++)
            {
                changed |= _state.UnlockCodexEntry(entryIds[i]);
            }

            if (changed)
            {
                Persist();
            }
        }

        /// <summary>Persists settings edits made through <see cref="Settings"/>.</summary>
        public void SaveSettings()
        {
            EnsureLoaded();
            Persist();
            // Settings edits go live immediately (feedback toggles, PLAN 3C).
            FeedbackSettings.Apply(_save.settings);
        }

        private void EnsureLoaded()
        {
            if (_state != null)
            {
                return;
            }

            _save = SaveFileStore.Load() ?? new SaveData();
            _state = new MetaProgressionState();
            _state.LoadFrom(_save);
            // First touch of the save each session (AudioService reads settings
            // at scene boot) — push the feedback toggles live before any
            // gameplay system checks them.
            FeedbackSettings.Apply(_save.settings);
        }

        private void Persist()
        {
            _state.WriteTo(_save);
            SaveFileStore.Save(_save);
        }
    }
}
