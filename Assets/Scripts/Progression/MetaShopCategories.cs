using SurveHive.Data;

namespace SurveHive.Progression
{
    /// <summary>
    /// Pure, EditMode-tested mapping of each <see cref="MetaStatType"/> to its
    /// Hive Upgrades shop tab. The tab grouping (TODO #25) is deterministic from
    /// the stat a upgrade raises, so it lives in code rather than as a serialized
    /// field — no per-asset authoring, no default-value migration risk.
    /// </summary>
    public static class MetaShopCategories
    {
        /// <summary>Number of tabs — one per <see cref="MetaShopCategory"/> value.</summary>
        public const int Count = 3;

        public static MetaShopCategory For(MetaStatType stat)
        {
            switch (stat)
            {
                case MetaStatType.MaxHealth:
                case MetaStatType.MoveSpeed:
                case MetaStatType.MagnetRadius:
                    return MetaShopCategory.Survival;

                case MetaStatType.CurrencyGain:
                case MetaStatType.ExpGain:
                case MetaStatType.ItemDropRate:
                case MetaStatType.Rerolls:
                    return MetaShopCategory.Utility;

                // Everything else is offensive: AttackDamage, AttackSpeed,
                // AbilityPower, CooldownReduction, CritChance, CritDamage.
                default:
                    return MetaShopCategory.Combat;
            }
        }
    }
}
