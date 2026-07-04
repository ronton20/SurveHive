using SurveHive.Core;
using SurveHive.Data;

namespace SurveHive.Progression
{
    /// <summary>
    /// Purchase transaction for the flat stat shop: rank-capped, escalating
    /// cost, spend + rank-up as one operation against the store seam.
    /// </summary>
    public static class MetaShop
    {
        public static bool TryPurchase(IMetaProgressionStore store, MetaUpgradeSO upgrade)
        {
            if (store == null || upgrade == null)
            {
                return false;
            }

            int currentRank = store.GetUpgradeRank(upgrade.UpgradeId);
            if (currentRank >= upgrade.MaxRank)
            {
                return false;
            }

            if (!store.TrySpendCurrency(upgrade.CostForRank(currentRank)))
            {
                return false;
            }

            store.SetUpgradeRank(upgrade.UpgradeId, currentRank + 1);
            return true;
        }
    }
}
