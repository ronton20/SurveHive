using SurveHive.Persistence;
using UnityEngine;

namespace SurveHive.Data
{
    /// <summary>
    /// In-memory meta store: nothing touches disk, ranks reset with the domain.
    /// Used by tests and as a dev/dummy stand-in; the shipped game wires
    /// <see cref="PersistentMetaProgressionStoreSO"/> instead.
    /// </summary>
    [CreateAssetMenu(menuName = "SurveHive/Runtime Meta Progression Store", fileName = "RuntimeMetaProgressionStore")]
    public sealed class RuntimeMetaProgressionStoreSO : MetaProgressionStoreSO
    {
        [SerializeField] private int _bankedTotal;
        [SerializeField] private int _bankedJelly;

        private readonly MetaProgressionState _ranks = new MetaProgressionState();

        public override int BankedCurrency => _bankedTotal;

        public override void BankRunCurrency(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _bankedTotal += amount;
        }

        public override bool TrySpendCurrency(int amount)
        {
            if (amount <= 0 || amount > _bankedTotal)
            {
                return false;
            }

            _bankedTotal -= amount;
            return true;
        }

        public override int BankedJelly => _bankedJelly;

        public override void BankJelly(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _bankedJelly += amount;
        }

        public override bool TrySpendJelly(int amount)
        {
            if (amount <= 0 || amount > _bankedJelly)
            {
                return false;
            }

            _bankedJelly -= amount;
            return true;
        }

        public override int GetUpgradeRank(string upgradeId)
        {
            return _ranks.GetRank(upgradeId);
        }

        public override void SetUpgradeRank(string upgradeId, int rank)
        {
            _ranks.SetRank(upgradeId, rank);
        }

        public override void RecordStageClear(string stageId, int difficulty)
        {
            _ranks.RecordStageClear(stageId, difficulty);
        }

        public override bool HasStageClear(string stageId, int difficulty)
        {
            return _ranks.HasStageClear(stageId, difficulty);
        }

        public override bool IsCodexUnlocked(string entryId)
        {
            return _ranks.IsCodexUnlocked(entryId);
        }

        public override void UnlockCodexEntries(System.Collections.Generic.List<string> entryIds)
        {
            if (entryIds == null)
            {
                return;
            }

            for (int i = 0; i < entryIds.Count; i++)
            {
                _ranks.UnlockCodexEntry(entryIds[i]);
            }
        }

        public override bool IsCosmeticOwned(string cosmeticId)
        {
            return _ranks.IsCosmeticOwned(cosmeticId);
        }

        public override void UnlockCosmetic(string cosmeticId)
        {
            _ranks.UnlockCosmetic(cosmeticId);
        }

        public override bool IsAchievementUnlocked(string achievementId)
        {
            return _ranks.IsAchievementUnlocked(achievementId);
        }

        public override void UnlockAchievement(string achievementId)
        {
            _ranks.UnlockAchievement(achievementId);
        }

        public override string GetEquippedCosmetic(int slot)
        {
            return _ranks.GetEquippedCosmetic(slot);
        }

        public override void SetEquippedCosmetic(int slot, string cosmeticId)
        {
            _ranks.SetEquippedCosmetic(slot, cosmeticId);
        }

        public override int GetDailyDealDay()
        {
            return _ranks.DailyDealDay;
        }

        public override string[] GetDailyDealIds()
        {
            return _ranks.GetDailyDealIds();
        }

        public override void SetDailyDeals(int dayStamp, System.Collections.Generic.List<string> cosmeticIds)
        {
            _ranks.SetDailyDeals(dayStamp, cosmeticIds);
        }
    }
}
