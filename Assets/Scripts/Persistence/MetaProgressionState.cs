using System.Collections.Generic;

namespace SurveHive.Persistence
{
    /// <summary>
    /// Pure in-memory meta-progression state (banked currency + upgrade ranks)
    /// with SaveData load/write conversion. No Unity dependencies — the shop
    /// math and transactions are EditMode-tested against this class.
    /// Menu-path only, so the dictionary/array allocations here are fine.
    /// </summary>
    public sealed class MetaProgressionState
    {
        private readonly Dictionary<string, int> _ranks = new Dictionary<string, int>();
        // Per-stage cleared-difficulty bitmask (bit n = (int)DifficultyTier n).
        private readonly Dictionary<string, int> _stageClearMasks = new Dictionary<string, int>();
        private int _bankedCurrency;

        public int BankedCurrency => _bankedCurrency;

        public void Bank(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _bankedCurrency += amount;
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || amount > _bankedCurrency)
            {
                return false;
            }

            _bankedCurrency -= amount;
            return true;
        }

        public int GetRank(string upgradeId)
        {
            return !string.IsNullOrEmpty(upgradeId) && _ranks.TryGetValue(upgradeId, out int rank)
                ? rank
                : 0;
        }

        public void SetRank(string upgradeId, int rank)
        {
            if (string.IsNullOrEmpty(upgradeId) || rank < 0)
            {
                return;
            }

            _ranks[upgradeId] = rank;
        }

        public void RecordStageClear(string stageId, int difficulty)
        {
            if (string.IsNullOrEmpty(stageId) || difficulty < 0 || difficulty > 30)
            {
                return;
            }

            _stageClearMasks.TryGetValue(stageId, out int mask);
            _stageClearMasks[stageId] = mask | (1 << difficulty);
        }

        public bool HasStageClear(string stageId, int difficulty)
        {
            return !string.IsNullOrEmpty(stageId) && difficulty >= 0 && difficulty <= 30
                && _stageClearMasks.TryGetValue(stageId, out int mask)
                && (mask & (1 << difficulty)) != 0;
        }

        public void LoadFrom(SaveData data)
        {
            _ranks.Clear();
            _stageClearMasks.Clear();
            _bankedCurrency = data.bankedCurrency;

            for (int i = 0; i < data.upgradeIds.Length; i++)
            {
                SetRank(data.upgradeIds[i], data.upgradeRanks[i]);
            }

            for (int i = 0; i < data.stageClearIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(data.stageClearIds[i]) && data.stageClearMasks[i] > 0)
                {
                    _stageClearMasks[data.stageClearIds[i]] = data.stageClearMasks[i];
                }
            }
        }

        public void WriteTo(SaveData data)
        {
            data.bankedCurrency = _bankedCurrency;
            data.upgradeIds = new string[_ranks.Count];
            data.upgradeRanks = new int[_ranks.Count];

            int index = 0;
            foreach (KeyValuePair<string, int> pair in _ranks)
            {
                data.upgradeIds[index] = pair.Key;
                data.upgradeRanks[index] = pair.Value;
                index++;
            }

            data.stageClearIds = new string[_stageClearMasks.Count];
            data.stageClearMasks = new int[_stageClearMasks.Count];

            index = 0;
            foreach (KeyValuePair<string, int> pair in _stageClearMasks)
            {
                data.stageClearIds[index] = pair.Key;
                data.stageClearMasks[index] = pair.Value;
                index++;
            }
        }
    }
}
