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
        // Codex unlock flags (PLAN 5A): ids formatted by Progression.CodexIds.
        private readonly HashSet<string> _codexUnlocks = new HashSet<string>();
        // Cosmetics (PLAN 5C): purchased ids + equipped id per (int)CosmeticSlot
        // ("" = the default look for that slot).
        private readonly HashSet<string> _ownedCosmetics = new HashSet<string>();
        private readonly string[] _equippedCosmetics = new string[Data.CosmeticSlots.Count];
        private int _bankedCurrency;
        // Premium currency, Royal Jelly (PLAN 5B) — separate pool from honey.
        private int _bankedJelly;

        public int BankedCurrency => _bankedCurrency;

        public int BankedJelly => _bankedJelly;

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

        public void BankJelly(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _bankedJelly += amount;
        }

        public bool TrySpendJelly(int amount)
        {
            if (amount <= 0 || amount > _bankedJelly)
            {
                return false;
            }

            _bankedJelly -= amount;
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

        /// <summary>Records one codex entry; returns true when it was newly unlocked.</summary>
        public bool UnlockCodexEntry(string entryId)
        {
            return !string.IsNullOrEmpty(entryId) && _codexUnlocks.Add(entryId);
        }

        public bool IsCodexUnlocked(string entryId)
        {
            return !string.IsNullOrEmpty(entryId) && _codexUnlocks.Contains(entryId);
        }

        public int CodexUnlockCount => _codexUnlocks.Count;

        /// <summary>Records a cosmetic purchase; returns true when newly owned.</summary>
        public bool UnlockCosmetic(string cosmeticId)
        {
            return !string.IsNullOrEmpty(cosmeticId) && _ownedCosmetics.Add(cosmeticId);
        }

        public bool IsCosmeticOwned(string cosmeticId)
        {
            return !string.IsNullOrEmpty(cosmeticId) && _ownedCosmetics.Contains(cosmeticId);
        }

        /// <summary>Equipped cosmetic id for a slot; "" means the default look.</summary>
        public string GetEquippedCosmetic(int slot)
        {
            return slot >= 0 && slot < _equippedCosmetics.Length && _equippedCosmetics[slot] != null
                ? _equippedCosmetics[slot]
                : string.Empty;
        }

        public void SetEquippedCosmetic(int slot, string cosmeticId)
        {
            if (slot < 0 || slot >= _equippedCosmetics.Length)
            {
                return;
            }

            _equippedCosmetics[slot] = cosmeticId ?? string.Empty;
        }

        public void LoadFrom(SaveData data)
        {
            _ranks.Clear();
            _stageClearMasks.Clear();
            _codexUnlocks.Clear();
            _ownedCosmetics.Clear();
            _bankedCurrency = data.bankedCurrency;
            _bankedJelly = data.bankedJelly;

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

            for (int i = 0; i < data.codexIds.Length; i++)
            {
                UnlockCodexEntry(data.codexIds[i]);
            }

            for (int i = 0; i < data.ownedCosmeticIds.Length; i++)
            {
                UnlockCosmetic(data.ownedCosmeticIds[i]);
            }

            for (int i = 0; i < _equippedCosmetics.Length; i++)
            {
                _equippedCosmetics[i] = i < data.equippedCosmeticIds.Length && data.equippedCosmeticIds[i] != null
                    ? data.equippedCosmeticIds[i]
                    : string.Empty;
            }
        }

        public void WriteTo(SaveData data)
        {
            data.bankedCurrency = _bankedCurrency;
            data.bankedJelly = _bankedJelly;
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

            data.codexIds = new string[_codexUnlocks.Count];
            index = 0;
            foreach (string entryId in _codexUnlocks)
            {
                data.codexIds[index] = entryId;
                index++;
            }

            data.ownedCosmeticIds = new string[_ownedCosmetics.Count];
            index = 0;
            foreach (string cosmeticId in _ownedCosmetics)
            {
                data.ownedCosmeticIds[index] = cosmeticId;
                index++;
            }

            data.equippedCosmeticIds = new string[_equippedCosmetics.Length];
            for (int i = 0; i < _equippedCosmetics.Length; i++)
            {
                data.equippedCosmeticIds[i] = _equippedCosmetics[i] ?? string.Empty;
            }
        }
    }
}
