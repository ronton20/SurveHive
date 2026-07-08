namespace SurveHive.Core
{
    public interface IMetaProgressionStore
    {
        int BankedCurrency { get; }

        void BankRunCurrency(int amount);

        bool TrySpendCurrency(int amount);

        int GetUpgradeRank(string upgradeId);

        void SetUpgradeRank(string upgradeId, int rank);

        /// <summary>Records a stage victory on a difficulty ((int)DifficultyTier).</summary>
        void RecordStageClear(string stageId, int difficulty);

        /// <summary>Whether the stage was ever cleared on a difficulty ((int)DifficultyTier).</summary>
        bool HasStageClear(string stageId, int difficulty);
    }
}
