using System;
using System.Collections.Generic;

namespace SurveHive.Core
{
    /// <summary>
    /// PLAN 3A — the authoritative English text for every <see cref="LocKeys"/>
    /// entry. This is the source the <c>LocalizationBuilder</c> reads to author
    /// the <c>StringTable</c> Resources asset, and the code-level fallback
    /// <see cref="Loc"/> uses when that asset is absent (so the seam reads
    /// identically with or without the generated asset, and tests need no
    /// on-disk asset). Ordered/grouped to mirror <see cref="LocKeys"/>.
    ///
    /// Whenever you add a key to <see cref="LocKeys"/>, add its default here — the
    /// localization test asserts the two stay in lockstep.
    /// </summary>
    public static class LocDefaults
    {
        // Exact spacing/punctuation is preserved so resolved output reads byte-for-byte
        // like the literals it replaced (prefixes keep their trailing spaces, etc.).
        private static readonly KeyValuePair<string, string>[] Entries =
        {
            new KeyValuePair<string, string>(LocKeys.LevelPrefix, "Lv. "),
            new KeyValuePair<string, string>(LocKeys.OwnedLevelPrefix, "Lv "),
            new KeyValuePair<string, string>(LocKeys.Max, "MAX"),

            new KeyValuePair<string, string>(LocKeys.LevelUpTitle, "LEVEL UP!"),
            new KeyValuePair<string, string>(LocKeys.LevelUpTitleMiniboss, "MINIBOSS KILLED!"),
            new KeyValuePair<string, string>(LocKeys.LevelUpLucky, "LUCKY! +2 levels"),
            new KeyValuePair<string, string>(LocKeys.LevelUpNew, "New"),
            new KeyValuePair<string, string>(LocKeys.LevelUpNewExclaim, "New!"),
            new KeyValuePair<string, string>(LocKeys.RerollsPrefix, "REROLLS: "),

            new KeyValuePair<string, string>(LocKeys.LanePassive, "PASSIVE"),
            new KeyValuePair<string, string>(LocKeys.LaneEnhancement, "ENHANCEMENT"),
            new KeyValuePair<string, string>(LocKeys.LaneAbility, "ABILITY"),
            new KeyValuePair<string, string>(LocKeys.LanePassivePlural, "PASSIVES"),
            new KeyValuePair<string, string>(LocKeys.LaneEnhancementPlural, "ENHANCEMENTS"),
            new KeyValuePair<string, string>(LocKeys.LaneAbilityPlural, "ABILITIES"),

            new KeyValuePair<string, string>(LocKeys.SetLabel, "SET"),
            new KeyValuePair<string, string>(LocKeys.SetUnlocks, "— unlocks: "),
            new KeyValuePair<string, string>(LocKeys.SetAtPrefix, " — at "),
            new KeyValuePair<string, string>(LocKeys.SetMaxed, "— maxed"),
            new KeyValuePair<string, string>(LocKeys.SetBonusesHeader, "SET BONUSES"),
            new KeyValuePair<string, string>(LocKeys.SetPiecesSuffix, " pc"),
            new KeyValuePair<string, string>(LocKeys.SetAtOpen, "(at "),
            new KeyValuePair<string, string>(LocKeys.OwnedEmpty, "No power-ups yet."),

            new KeyValuePair<string, string>(LocKeys.WaveBoss, "BOSS"),
            new KeyValuePair<string, string>(LocKeys.WaveDanger, "DANGER WAVE"),
            new KeyValuePair<string, string>(LocKeys.WaveIncomingPrefix, "INCOMING IN "),
            new KeyValuePair<string, string>(LocKeys.WaveSecondsSuffix, "s"),

            new KeyValuePair<string, string>(LocKeys.ShopRankPrefix, "Rank "),
            new KeyValuePair<string, string>(LocKeys.ShopTabCombat, "COMBAT"),
            new KeyValuePair<string, string>(LocKeys.ShopTabSurvival, "SURVIVAL"),
            new KeyValuePair<string, string>(LocKeys.ShopTabUtility, "UTILITY"),

            new KeyValuePair<string, string>(LocKeys.SettingsVibrationOn, "VIBRATION: ON"),
            new KeyValuePair<string, string>(LocKeys.SettingsVibrationOff, "VIBRATION: OFF"),
            new KeyValuePair<string, string>(LocKeys.SettingsQualityPrefix, "QUALITY: "),
            new KeyValuePair<string, string>(LocKeys.SettingsQualityDefault, "DEFAULT"),
            new KeyValuePair<string, string>(LocKeys.SettingsOnSuffix, ": ON"),
            new KeyValuePair<string, string>(LocKeys.SettingsOffSuffix, ": OFF"),
            new KeyValuePair<string, string>(LocKeys.SettingsEnemyHpBars, "ENEMY HP BARS"),
            new KeyValuePair<string, string>(LocKeys.SettingsDamageNumbers, "DAMAGE NUMBERS"),
            new KeyValuePair<string, string>(LocKeys.SettingsScreenShake, "SCREEN SHAKE"),
            new KeyValuePair<string, string>(LocKeys.SettingsHitStop, "HIT-STOP"),
            new KeyValuePair<string, string>(LocKeys.SettingsStatusTints, "STATUS COLORS"),

            new KeyValuePair<string, string>(LocKeys.CodexTitle, "CODEX"),
            new KeyValuePair<string, string>(LocKeys.CodexMenuButton, "CODEX"),
            new KeyValuePair<string, string>(LocKeys.CodexTabPowerUps, "POWER-UPS"),
            new KeyValuePair<string, string>(LocKeys.CodexTabSets, "SETS"),
            new KeyValuePair<string, string>(LocKeys.CodexTabEnemies, "ENEMIES"),
            new KeyValuePair<string, string>(LocKeys.CodexTabItems, "ITEMS"),
            new KeyValuePair<string, string>(LocKeys.CodexUnknownName, "???"),
            new KeyValuePair<string, string>(LocKeys.CodexUnknownDescription, "Not yet discovered."),
            new KeyValuePair<string, string>(LocKeys.CodexDiscoveredPrefix, "DISCOVERED "),
            new KeyValuePair<string, string>(LocKeys.CodexSectionPassives, "PASSIVES"),
            new KeyValuePair<string, string>(LocKeys.CodexSectionEnhancements, "ENHANCEMENTS"),
            new KeyValuePair<string, string>(LocKeys.CodexSectionAbilities, "ABILITIES"),

            new KeyValuePair<string, string>(LocKeys.CosmeticsTitle, "HIVE STYLE"),
            new KeyValuePair<string, string>(LocKeys.CosmeticsMenuButton, "STYLE"),
            new KeyValuePair<string, string>(LocKeys.CosmeticsTabColors, "COLORS"),
            new KeyValuePair<string, string>(LocKeys.CosmeticsTabHats, "HATS"),
            new KeyValuePair<string, string>(LocKeys.CosmeticsTabStingers, "STINGERS"),
            new KeyValuePair<string, string>(LocKeys.CosmeticsDefaultName, "DEFAULT"),
            new KeyValuePair<string, string>(LocKeys.CosmeticsDefaultDescription, "The hero's natural look for this slot."),
            new KeyValuePair<string, string>(LocKeys.CosmeticsBuyPrefix, "BUY "),
            new KeyValuePair<string, string>(LocKeys.CosmeticsEquip, "EQUIP"),
            new KeyValuePair<string, string>(LocKeys.CosmeticsEquipped, "EQUIPPED"),

            new KeyValuePair<string, string>(LocKeys.DealsTitle, "DAILY DEALS"),
            new KeyValuePair<string, string>(LocKeys.DealsMenuButton, "DEALS"),
            new KeyValuePair<string, string>(LocKeys.DealsFlashButton, "DAILY DEALS!"),
            new KeyValuePair<string, string>(LocKeys.DealsTimerPrefix, "NEW DEALS IN "),
            new KeyValuePair<string, string>(LocKeys.DealsBuy, "BUY"),
            new KeyValuePair<string, string>(LocKeys.DealsSold, "SOLD"),
            new KeyValuePair<string, string>(LocKeys.DealsAllOwned, "You own every cosmetic — nothing left to discount!"),

            new KeyValuePair<string, string>(LocKeys.AchievementsTitle, "ACHIEVEMENTS"),
            new KeyValuePair<string, string>(LocKeys.AchievementsMenuButton, "AWARDS"),
            new KeyValuePair<string, string>(LocKeys.AchievementsToastTitle, "ACHIEVEMENT UNLOCKED!"),
            new KeyValuePair<string, string>(LocKeys.AchievementsRewardPrefix, "REWARD: "),
            new KeyValuePair<string, string>(LocKeys.AchievementsUnlockedPrefix, "UNLOCKED "),

            new KeyValuePair<string, string>(LocKeys.ResultsTime, "Time  "),
            new KeyValuePair<string, string>(LocKeys.ResultsKills, "Kills  "),
            new KeyValuePair<string, string>(LocKeys.ResultsLevel, "Level  "),
            new KeyValuePair<string, string>(LocKeys.ResultsHoneyBanked, "Honey banked  "),
            new KeyValuePair<string, string>(LocKeys.ResultsJellyEarned, "Royal Jelly  +"),

            new KeyValuePair<string, string>(LocKeys.DifficultyLockedSuffix, " - LOCKED"),
            new KeyValuePair<string, string>(LocKeys.DifficultyUnlockPrefix, "UNLOCK "),
            new KeyValuePair<string, string>(LocKeys.DifficultyClearPrefix, "Clear "),
            new KeyValuePair<string, string>(LocKeys.DifficultyOn, " on "),
        };

        private static Dictionary<string, string> _lookup;

        /// <summary>The default (key, English) pairs — the builder's authoring source.</summary>
        public static IReadOnlyList<KeyValuePair<string, string>> All => Entries;

        public static bool TryGet(string key, out string value)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<string, string>(Entries.Length, StringComparer.Ordinal);
                for (int i = 0; i < Entries.Length; i++)
                {
                    _lookup[Entries[i].Key] = Entries[i].Value;
                }
            }

            return _lookup.TryGetValue(key, out value);
        }
    }
}
