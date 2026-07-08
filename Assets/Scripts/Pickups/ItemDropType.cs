using SurveHive.Core;

namespace SurveHive.Pickups
{
    public enum ItemDropType
    {
        // Heal a fraction of max health.
        HoneyJar = 0,
        // Vacuum every EXP/currency pickup to the player.
        Magnet = 1,
        // Absorb the next N hits.
        WaxShield = 2,
        // Screen nuke: heavy damage to everything on screen.
        RoyalBomb = 3
    }

    /// <summary>Type → pool mapping + uniform random roll (pure, testable).</summary>
    public static class ItemDrops
    {
        public const int TypeCount = 4;

        /// <summary>
        /// Meta-shop Item Drop Rate multiplier on every drop-table roll.
        /// MetaUpgradeApplier resets it to 1 at every run start (a static must
        /// not leak a previous run's rank across restarts), then applies the
        /// bought rank.
        /// </summary>
        public static float DropChanceMultiplier { get; private set; } = 1f;

        public static void SetDropChanceMultiplier(float multiplier)
        {
            DropChanceMultiplier = multiplier < 0f ? 0f : multiplier;
        }

        public static int GetPoolId(ItemDropType type)
        {
            switch (type)
            {
                case ItemDropType.Magnet:
                    return PoolIds.MagnetDrop;
                case ItemDropType.WaxShield:
                    return PoolIds.WaxShieldDrop;
                case ItemDropType.RoyalBomb:
                    return PoolIds.RoyalBombDrop;
                default:
                    return PoolIds.HoneyJarDrop;
            }
        }

        /// <summary>Maps a roll in [0,1) to a drop type (equal weights).</summary>
        public static ItemDropType RollType(float roll01)
        {
            int index = (int)(roll01 * TypeCount);
            if (index < 0)
            {
                index = 0;
            }
            else if (index >= TypeCount)
            {
                index = TypeCount - 1;
            }

            return (ItemDropType)index;
        }
    }
}
