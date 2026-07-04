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

        public void LoadFrom(SaveData data)
        {
            _ranks.Clear();
            _bankedCurrency = data.bankedCurrency;

            for (int i = 0; i < data.upgradeIds.Length; i++)
            {
                SetRank(data.upgradeIds[i], data.upgradeRanks[i]);
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
        }
    }
}
