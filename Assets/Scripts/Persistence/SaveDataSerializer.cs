using System;
using UnityEngine;

namespace SurveHive.Persistence
{
    /// <summary>
    /// Pure JSON (de)serialization + sanitizing for <see cref="SaveData"/>.
    /// Returns null for anything unreadable so callers fall back to a fresh
    /// save instead of throwing. EditMode-tested.
    /// </summary>
    public static class SaveDataSerializer
    {
        public static string ToJson(SaveData data)
        {
            return JsonUtility.ToJson(data, prettyPrint: true);
        }

        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception)
            {
                return null;
            }

            if (data == null || data.version <= 0 || data.version > SaveData.CurrentVersion)
            {
                return null;
            }

            Sanitize(data);
            Migrate(data);
            return data;
        }

        private static void Sanitize(SaveData data)
        {
            if (data.upgradeIds == null)
            {
                data.upgradeIds = new string[0];
            }

            if (data.upgradeRanks == null)
            {
                data.upgradeRanks = new int[0];
            }

            // Mismatched parallel arrays: keep only the pairs that exist.
            if (data.upgradeIds.Length != data.upgradeRanks.Length)
            {
                int count = Mathf.Min(data.upgradeIds.Length, data.upgradeRanks.Length);
                Array.Resize(ref data.upgradeIds, count);
                Array.Resize(ref data.upgradeRanks, count);
            }

            if (data.bankedCurrency < 0)
            {
                data.bankedCurrency = 0;
            }

            if (data.settings == null)
            {
                data.settings = new SettingsData();
            }

            data.settings.musicVolume = Mathf.Clamp01(data.settings.musicVolume);
            data.settings.sfxVolume = Mathf.Clamp01(data.settings.sfxVolume);

            if (data.bestRun == null)
            {
                data.bestRun = new BestRunData();
            }

            data.selectedDifficulty = Mathf.Clamp(
                data.selectedDifficulty, (int)Data.DifficultyTier.Easy, (int)Data.DifficultyTier.Extreme);

            if (data.stageClearIds == null)
            {
                data.stageClearIds = new string[0];
            }

            if (data.stageClearMasks == null)
            {
                data.stageClearMasks = new int[0];
            }

            if (data.stageClearIds.Length != data.stageClearMasks.Length)
            {
                int count = Mathf.Min(data.stageClearIds.Length, data.stageClearMasks.Length);
                Array.Resize(ref data.stageClearIds, count);
                Array.Resize(ref data.stageClearMasks, count);
            }

            for (int i = 0; i < data.stageClearMasks.Length; i++)
            {
                if (data.stageClearMasks[i] < 0)
                {
                    data.stageClearMasks[i] = 0;
                }
            }
        }

        private static void Migrate(SaveData data)
        {
            // Future schema bumps upgrade the data in place, step by step
            // (v1 -> v2 -> ...), then stamp the current version.
            data.version = SaveData.CurrentVersion;
        }
    }
}
