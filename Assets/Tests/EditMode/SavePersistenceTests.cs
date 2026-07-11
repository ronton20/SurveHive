using System.IO;
using NUnit.Framework;
using SurveHive.Persistence;

namespace SurveHive.Tests
{
    /// <summary>
    /// Save foundation: JSON round-trip fidelity, corrupt-input tolerance
    /// (always null → fresh save, never an exception), and safe file writes.
    /// </summary>
    public sealed class SavePersistenceTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SurveHiveSaveTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            SaveFileStore.SetPathOverride(Path.Combine(_tempDir, "save.json"));
        }

        [TearDown]
        public void TearDown()
        {
            SaveFileStore.SetPathOverride(null);
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        [Test]
        public void RoundTrip_PreservesAllFields()
        {
            var data = new SaveData
            {
                bankedCurrency = 1234,
                bankedJelly = 17,
                upgradeIds = new[] { "meta_max_health", "meta_damage" },
                upgradeRanks = new[] { 3, 7 },
                codexIds = new[] { "skill:swift_wings", "enemy:WorkerBee", "item:HoneyJar" },
            };
            data.settings.musicVolume = 0.25f;
            data.settings.sfxVolume = 0.75f;
            data.settings.vibration = false;
            data.settings.qualityLevel = 2;
            data.settings.showEnemyHealthBars = false;
            data.settings.showDamageNumbers = false;
            data.settings.screenShake = false;
            data.settings.hitStop = false;
            data.settings.statusTints = false;
            data.bestRun.bestTimeSeconds = 600;
            data.bestRun.bestKills = 999;
            data.bestRun.bestLevel = 21;
            data.bestRun.runsPlayed = 14;
            data.bestRun.victories = 2;

            SaveData loaded = SaveDataSerializer.FromJson(SaveDataSerializer.ToJson(data));

            Assert.IsNotNull(loaded);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual(1234, loaded.bankedCurrency);
            Assert.AreEqual(17, loaded.bankedJelly);
            CollectionAssert.AreEqual(data.upgradeIds, loaded.upgradeIds);
            CollectionAssert.AreEqual(data.upgradeRanks, loaded.upgradeRanks);
            Assert.AreEqual(0.25f, loaded.settings.musicVolume);
            Assert.AreEqual(0.75f, loaded.settings.sfxVolume);
            Assert.IsFalse(loaded.settings.vibration);
            Assert.AreEqual(2, loaded.settings.qualityLevel);
            Assert.IsFalse(loaded.settings.showEnemyHealthBars);
            Assert.IsFalse(loaded.settings.showDamageNumbers);
            Assert.IsFalse(loaded.settings.screenShake);
            Assert.IsFalse(loaded.settings.hitStop);
            Assert.IsFalse(loaded.settings.statusTints);
            Assert.AreEqual(600, loaded.bestRun.bestTimeSeconds);
            Assert.AreEqual(999, loaded.bestRun.bestKills);
            Assert.AreEqual(21, loaded.bestRun.bestLevel);
            Assert.AreEqual(14, loaded.bestRun.runsPlayed);
            Assert.AreEqual(2, loaded.bestRun.victories);
            CollectionAssert.AreEqual(data.codexIds, loaded.codexIds);
        }

        [Test]
        public void FromJson_PreV5Save_DefaultsCodexEmpty()
        {
            // A v4 save knows nothing of the codex — the field initializer must
            // land an empty (never null) unlock list.
            SaveData loaded = SaveDataSerializer.FromJson("{\"version\":4,\"bankedCurrency\":10}");

            Assert.IsNotNull(loaded);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.IsNotNull(loaded.codexIds);
            Assert.AreEqual(0, loaded.codexIds.Length);
            Assert.AreEqual(10, loaded.bankedCurrency);
        }

        [Test]
        public void FromJson_PreV6Save_DefaultsJellyZero()
        {
            // A v5 save knows nothing of Royal Jelly — the field initializer
            // must land a zero balance while keeping everything it did carry.
            SaveData loaded = SaveDataSerializer.FromJson(
                "{\"version\":5,\"bankedCurrency\":250,\"codexIds\":[\"enemy:WorkerBee\"]}");

            Assert.IsNotNull(loaded);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual(0, loaded.bankedJelly);
            Assert.AreEqual(250, loaded.bankedCurrency);
            Assert.AreEqual(1, loaded.codexIds.Length);
        }

        [Test]
        public void FromJson_NegativeJelly_ClampsToZero()
        {
            SaveData loaded = SaveDataSerializer.FromJson("{\"version\":6,\"bankedJelly\":-8}");

            Assert.IsNotNull(loaded);
            Assert.AreEqual(0, loaded.bankedJelly);
        }

        [Test]
        public void FromJson_GarbageInput_ReturnsNull()
        {
            Assert.IsNull(SaveDataSerializer.FromJson(null));
            Assert.IsNull(SaveDataSerializer.FromJson(""));
            Assert.IsNull(SaveDataSerializer.FromJson("not json at all {{{"));
            Assert.IsNull(SaveDataSerializer.FromJson("\"just a string\""));
        }

        [Test]
        public void FromJson_BadVersion_ReturnsNull()
        {
            Assert.IsNull(SaveDataSerializer.FromJson("{\"version\":0}"));
            Assert.IsNull(SaveDataSerializer.FromJson("{\"version\":-3}"));
            Assert.IsNull(SaveDataSerializer.FromJson(
                $"{{\"version\":{SaveData.CurrentVersion + 1}}}"));
        }

        [Test]
        public void FromJson_MismatchedRankArrays_TruncatesToPairs()
        {
            SaveData loaded = SaveDataSerializer.FromJson(
                "{\"version\":1,\"upgradeIds\":[\"a\",\"b\",\"c\"],\"upgradeRanks\":[1,2]}");

            Assert.IsNotNull(loaded);
            Assert.AreEqual(2, loaded.upgradeIds.Length);
            Assert.AreEqual(2, loaded.upgradeRanks.Length);
        }

        [Test]
        public void FromJson_NegativeCurrency_ClampsToZero()
        {
            SaveData loaded = SaveDataSerializer.FromJson("{\"version\":1,\"bankedCurrency\":-50}");

            Assert.IsNotNull(loaded);
            Assert.AreEqual(0, loaded.bankedCurrency);
        }

        [Test]
        public void FromJson_PreV4Save_DefaultsFeedbackTogglesOn()
        {
            // A v3 save knows nothing of the feedback toggles — the field
            // initializers must land every layer on.
            SaveData loaded = SaveDataSerializer.FromJson(
                "{\"version\":3,\"settings\":{\"musicVolume\":0.5,\"sfxVolume\":0.5,\"vibration\":false,\"qualityLevel\":1}}");

            Assert.IsNotNull(loaded);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.IsTrue(loaded.settings.showEnemyHealthBars);
            Assert.IsTrue(loaded.settings.showDamageNumbers);
            Assert.IsTrue(loaded.settings.screenShake);
            Assert.IsTrue(loaded.settings.hitStop);
            Assert.IsTrue(loaded.settings.statusTints);
            // ...while the fields the old save did carry are kept.
            Assert.IsFalse(loaded.settings.vibration);
            Assert.AreEqual(1, loaded.settings.qualityLevel);
        }

        [Test]
        public void FileStore_SaveThenLoad_RoundTrips()
        {
            var data = new SaveData { bankedCurrency = 77 };

            Assert.IsTrue(SaveFileStore.Save(data));
            SaveData loaded = SaveFileStore.Load();

            Assert.IsNotNull(loaded);
            Assert.AreEqual(77, loaded.bankedCurrency);
        }

        [Test]
        public void FileStore_MissingFile_ReturnsNull()
        {
            Assert.IsNull(SaveFileStore.Load());
        }

        [Test]
        public void FileStore_CorruptFile_ReturnsNull()
        {
            File.WriteAllText(SaveFileStore.SavePath, "\0\0\0 truncated garbage");

            Assert.IsNull(SaveFileStore.Load());
        }

        [Test]
        public void FileStore_Save_LeavesNoTempFile()
        {
            SaveFileStore.Save(new SaveData());

            Assert.IsFalse(File.Exists(SaveFileStore.SavePath + ".tmp"));
            Assert.IsTrue(File.Exists(SaveFileStore.SavePath));
        }

        [Test]
        public void FileStore_Overwrite_ReplacesPreviousSave()
        {
            SaveFileStore.Save(new SaveData { bankedCurrency = 1 });
            SaveFileStore.Save(new SaveData { bankedCurrency = 2 });

            Assert.AreEqual(2, SaveFileStore.Load().bankedCurrency);
        }
    }
}
