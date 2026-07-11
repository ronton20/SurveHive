namespace SurveHive.Core
{
    public interface IMetaProgressionStore
    {
        int BankedCurrency { get; }

        void BankRunCurrency(int amount);

        bool TrySpendCurrency(int amount);

        /// <summary>Banked premium currency, Royal Jelly (PLAN 5B).</summary>
        int BankedJelly { get; }

        void BankJelly(int amount);

        bool TrySpendJelly(int amount);

        int GetUpgradeRank(string upgradeId);

        void SetUpgradeRank(string upgradeId, int rank);

        /// <summary>Records a stage victory on a difficulty ((int)DifficultyTier).</summary>
        void RecordStageClear(string stageId, int difficulty);

        /// <summary>Whether the stage was ever cleared on a difficulty ((int)DifficultyTier).</summary>
        bool HasStageClear(string stageId, int difficulty);
    }
}
