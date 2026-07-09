namespace SurveHive.Core
{
    /// <summary>
    /// PLAN 3A localization seam — the single source of truth for every
    /// user-facing UI-chrome string key. Call sites reference these consts and
    /// resolve them through <see cref="Loc"/>; the English text for each key
    /// lives in <see cref="LocDefaults"/> (and, overridably, in the
    /// <c>StringTable</c> Resources asset the <c>LocalizationBuilder</c> authors).
    ///
    /// Scope: this seam covers UI chrome only. Authored content — skill/upgrade/
    /// set names + descriptions and enemy display names — stays authoritative on
    /// its ScriptableObject, so a full translation later localizes the SO text and
    /// this table together, not one at the expense of the other.
    /// </summary>
    public static class LocKeys
    {
        // Shared / HUD
        public const string LevelPrefix = "hud.level_prefix";
        public const string OwnedLevelPrefix = "hud.owned_level_prefix";
        public const string Max = "common.max";

        // Level-up offer screen
        public const string LevelUpTitle = "levelup.title";
        public const string LevelUpTitleMiniboss = "levelup.title_miniboss";
        public const string LevelUpLucky = "levelup.lucky";
        public const string LevelUpNew = "levelup.new";
        public const string LevelUpNewExclaim = "levelup.new_exclaim";
        public const string RerollsPrefix = "levelup.rerolls_prefix";

        // Power-up lanes
        public const string LanePassive = "lane.passive";
        public const string LaneEnhancement = "lane.enhancement";
        public const string LaneAbility = "lane.ability";
        public const string LanePassivePlural = "lane.passives";
        public const string LaneEnhancementPlural = "lane.enhancements";
        public const string LaneAbilityPlural = "lane.abilities";

        // Elemental set lines (offer card + owned build view)
        public const string SetLabel = "set.label";
        public const string SetUnlocks = "set.unlocks";
        public const string SetAtPrefix = "set.at_prefix";
        public const string SetMaxed = "set.maxed";
        public const string SetBonusesHeader = "set.bonuses_header";
        public const string SetPiecesSuffix = "set.pieces_suffix";
        public const string SetAtOpen = "set.at_open";
        public const string OwnedEmpty = "owned.empty";

        // Wave / boss warning banner
        public const string WaveBoss = "wave.boss";
        public const string WaveDanger = "wave.danger";
        public const string WaveIncomingPrefix = "wave.incoming_prefix";
        public const string WaveSecondsSuffix = "wave.seconds_suffix";

        // Meta shop
        public const string ShopHoneyPrefix = "shop.honey_prefix";
        public const string ShopRankPrefix = "shop.rank_prefix";
        public const string ShopTabCombat = "shop.tab_combat";
        public const string ShopTabSurvival = "shop.tab_survival";
        public const string ShopTabUtility = "shop.tab_utility";
        public const string ShopBuy = "shop.buy";
        public const string ShopCostPrefix = "shop.cost_prefix";

        // Settings
        public const string SettingsVibrationOn = "settings.vibration_on";
        public const string SettingsVibrationOff = "settings.vibration_off";
        public const string SettingsQualityPrefix = "settings.quality_prefix";
        public const string SettingsQualityDefault = "settings.quality_default";

        // Run results
        public const string ResultsTime = "results.time";
        public const string ResultsKills = "results.kills";
        public const string ResultsLevel = "results.level";
        public const string ResultsHoneyBanked = "results.honey_banked";

        // Difficulty select
        public const string DifficultyLockedSuffix = "difficulty.locked_suffix";
        public const string DifficultyUnlockPrefix = "difficulty.unlock_prefix";
        public const string DifficultyClearPrefix = "difficulty.clear_prefix";
        public const string DifficultyOn = "difficulty.on";
    }
}
