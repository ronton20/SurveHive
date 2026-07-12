using SurveHive.Data;

namespace SurveHive.Progression
{
    /// <summary>
    /// Cosmetic transactions (PLAN 5C), EditMode-tested against the store seam:
    /// buying spends Royal Jelly and marks the cosmetic owned in one operation;
    /// equipping only ever succeeds for owned items (or "" — the default look,
    /// always available). Purely visual, so nothing here touches run stats.
    /// </summary>
    public static class CosmeticShop
    {
        public static bool TryPurchase(MetaProgressionStoreSO store, CosmeticSO cosmetic)
        {
            return cosmetic != null && TryPurchase(store, cosmetic, cosmetic.JellyCost);
        }

        /// <summary>List-price override — the daily deals (PLAN 5E) buy at their discounted price.</summary>
        public static bool TryPurchase(MetaProgressionStoreSO store, CosmeticSO cosmetic, int price)
        {
            if (store == null || cosmetic == null || string.IsNullOrEmpty(cosmetic.CosmeticId))
            {
                return false;
            }

            if (store.IsCosmeticOwned(cosmetic.CosmeticId))
            {
                return false;
            }

            // Zero-cost entries (achievement-granted skins listed in the shop)
            // unlock without a spend — TrySpendJelly rejects amounts <= 0.
            if (price > 0 && !store.TrySpendJelly(price))
            {
                return false;
            }

            store.UnlockCosmetic(cosmetic.CosmeticId);
            return true;
        }

        /// <summary>Equips an owned cosmetic ("" reverts the slot to default).</summary>
        public static bool TryEquip(MetaProgressionStoreSO store, CosmeticSlot slot, string cosmeticId)
        {
            if (store == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(cosmeticId))
            {
                store.SetEquippedCosmetic((int)slot, string.Empty);
                return true;
            }

            if (!store.IsCosmeticOwned(cosmeticId))
            {
                return false;
            }

            store.SetEquippedCosmetic((int)slot, cosmeticId);
            return true;
        }
    }
}
