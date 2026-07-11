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
        public const int CurrentVersion = 7;

        public int version = CurrentVersion;
        public int bankedCurrency;
        public string[] upgradeIds = new string[0];
        public int[] upgradeRanks = new int[0];
        public SettingsData settings = new SettingsData();
        public BestRunData bestRun = new BestRunData();
        // v2: last-selected difficulty tier (Data.DifficultyTier as int). The
        // initializer doubles as the v1 migration — JsonUtility leaves missing
        // fields at their default, so old saves land on Normal.
        public int selectedDifficulty = (int)Data.DifficultyTier.Normal;
        // v3: per-stage victory record as parallel id/bitmask arrays (bit n =
        // cleared on (int)DifficultyTier n) — drives Hard/Extreme unlocks.
        // Initializers double as the v2 migration (missing → no clears).
        public string[] stageClearIds = new string[0];
        public int[] stageClearMasks = new int[0];
        // v5: codex unlock flags (PLAN 5A) — the ids of every entry the player
        // has encountered, formatted by Progression.CodexIds. The initializer
        // doubles as the v4 migration (missing → nothing discovered).
        public string[] codexIds = new string[0];
        // v6: banked premium currency, Royal Jelly (PLAN 5B). The initializer
        // doubles as the v5 migration (missing → none earned yet).
        public int bankedJelly;
        // v7: cosmetics (PLAN 5C) — purchased ids plus the equipped id per
        // (int)Data.CosmeticSlot ("" = default look). Initializers double as
        // the v6 migration (missing → nothing owned, default appearance).
        public string[] ownedCosmeticIds = new string[0];
        public string[] equippedCosmeticIds = new string[0];
    }

    [Serializable]
    public sealed class SettingsData
    {
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public bool vibration = true;
        // -1 = use the project's default quality level.
        public int qualityLevel = -1;
        // v4: feedback-layer toggles (PLAN 3C). Initializers double as the v3
        // migration — JsonUtility leaves missing fields at their default, so
        // old saves land with every layer on.
        public bool showEnemyHealthBars = true;
        public bool showDamageNumbers = true;
        public bool screenShake = true;
        public bool hitStop = true;
        public bool statusTints = true;
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
