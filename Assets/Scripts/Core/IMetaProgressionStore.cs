namespace SurveHive.Core
{
    public interface IMetaProgressionStore
    {
        int BankedCurrency { get; }

        void BankRunCurrency(int amount);

        bool TrySpendCurrency(int amount);

        int GetUpgradeRank(string upgradeId);

        void SetUpgradeRank(string upgradeId, int rank);
    }
}
