using NUnit.Framework;
using SurveHive.Data;

namespace SurveHive.Tests
{
    /// <summary>
    /// Phase 4C swarm packs: the packSize field was added to WaveEntry after
    /// the wave assets shipped, so entries serialized before it exist hold 0 —
    /// the clamp must read both 0 (legacy) and 1 as a single spawn.
    /// </summary>
    public sealed class WavePackSizeTests
    {
        [Test]
        public void LegacyZero_SpawnsOne()
        {
            Assert.AreEqual(1, WaveSpawnerConfigSO.ClampPackSize(0));
        }

        [Test]
        public void NegativeGarbage_SpawnsOne()
        {
            Assert.AreEqual(1, WaveSpawnerConfigSO.ClampPackSize(-3));
        }

        [Test]
        public void ExplicitSizes_PassThrough()
        {
            Assert.AreEqual(1, WaveSpawnerConfigSO.ClampPackSize(1));
            Assert.AreEqual(6, WaveSpawnerConfigSO.ClampPackSize(6));
        }
    }
}
