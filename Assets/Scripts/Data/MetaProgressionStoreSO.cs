using SurveHive.Core;
using UnityEngine;

namespace SurveHive.Data
{
    /// <summary>
    /// Serializable seam for meta progression: scene components reference this
    /// base so the backing store (in-memory for tests, save-file-backed for the
    /// real game) is swappable per asset.
    /// </summary>
    public abstract class MetaProgressionStoreSO : ScriptableObject, IMetaProgressionStore
    {
        public abstract int BankedCurrency { get; }

        public abstract void BankRunCurrency(int amount);

        public abstract bool TrySpendCurrency(int amount);

        public abstract int GetUpgradeRank(string upgradeId);

        public abstract void SetUpgradeRank(string upgradeId, int rank);

        /// <summary>End-of-run stats hook (best-run tracking); no-op by default.</summary>
        public virtual void RecordRunResult(int timeSeconds, int kills, int level, bool victory)
        {
        }

        /// <summary>Stage-victory record (difficulty unlocks); no-op by default.</summary>
        public virtual void RecordStageClear(string stageId, int difficulty)
        {
        }

        /// <summary>Whether the stage was cleared on a difficulty; false by default.</summary>
        public virtual bool HasStageClear(string stageId, int difficulty)
        {
            return false;
        }
    }
}
