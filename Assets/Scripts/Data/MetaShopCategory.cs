namespace SurveHive.Data
{
    /// <summary>
    /// Which tab a permanent shop upgrade sits under in the Hive Upgrades shop.
    /// Derived from <see cref="MetaStatType"/> (see <c>MetaShopCategories</c>), so
    /// it is not itself serialized on any asset today — but treat it append-only
    /// in case a future save/analytics path ever persists it by index.
    /// </summary>
    public enum MetaShopCategory
    {
        Combat = 0,
        Survival = 1,
        Utility = 2,
    }
}
