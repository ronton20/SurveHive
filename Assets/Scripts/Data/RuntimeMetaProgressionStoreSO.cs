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
    }
}
