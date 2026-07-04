using System;

namespace SurveHive.Persistence
{
    /// <summary>
    /// Versioned save schema serialized with JsonUtility (public fields, no
    /// dictionaries). Upgrade ranks are stored as parallel id/rank arrays.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int bankedCurrency;
        public string[] upgradeIds = new string[0];
        public int[] upgradeRanks = new int[0];
        public SettingsData settings = new SettingsData();
        public BestRunData bestRun = new BestRunData();
    }

    [Serializable]
    public sealed class SettingsData
    {
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public bool vibration = true;
        // -1 = use the project's default quality level.
        public int qualityLevel = -1;
    }

    [Serializable]
    public sealed class BestRunData
    {
        public int bestTimeSeconds;
        public int bestKills;
        public int bestLevel;
        public int runsPlayed;
        public int victories;
    }
}
