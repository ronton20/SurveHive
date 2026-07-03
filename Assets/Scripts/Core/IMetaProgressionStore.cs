namespace SurveHive.Core
{
    public interface IMetaProgressionStore
    {
        int BankedCurrency { get; }

        void BankRunCurrency(int amount);
    }
}
