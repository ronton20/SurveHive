using NUnit.Framework;
using SurveHive.Core;
using SurveHive.Pickups;

namespace SurveHive.Tests
{
    public sealed class ItemDropsTests
    {
        [Test]
        public void EveryDropType_MapsToADistinctPool()
        {
            var seen = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < ItemDrops.TypeCount; i++)
            {
                Assert.IsTrue(seen.Add(ItemDrops.GetPoolId((ItemDropType)i)),
                    $"Pool id for {(ItemDropType)i} duplicates another drop type");
            }
        }

        [Test]
        public void RollType_CoversAllTypesUniformly()
        {
            Assert.AreEqual(ItemDropType.HoneyJar, ItemDrops.RollType(0f));
            Assert.AreEqual(ItemDropType.Magnet, ItemDrops.RollType(0.26f));
            Assert.AreEqual(ItemDropType.WaxShield, ItemDrops.RollType(0.51f));
            Assert.AreEqual(ItemDropType.RoyalBomb, ItemDrops.RollType(0.76f));
        }

        [Test]
        public void RollType_ClampsOutOfRangeRolls()
        {
            Assert.AreEqual(ItemDropType.HoneyJar, ItemDrops.RollType(-0.5f));
            Assert.AreEqual(ItemDropType.RoyalBomb, ItemDrops.RollType(1f));
            Assert.AreEqual(ItemDropType.RoyalBomb, ItemDrops.RollType(1.7f));
        }

        [Test]
        public void HoneyJarPool_IsTheDefaultMapping()
        {
            Assert.AreEqual(PoolIds.HoneyJarDrop, ItemDrops.GetPoolId(ItemDropType.HoneyJar));
        }
    }
}
