namespace SurveHive.Data
{
    public enum SfxId
    {
        Hit,
        Kill,
        Pickup,
        LevelUp,
        PlayerHurt,
        PlayerDeath,
        Victory,
        UIClick,
        BossStinger,
        SkillStingerBarrage,
        SkillPiercingLance,
        SkillHoneySplash,
        SkillPollenCloud,
        SkillStaticWings,
        SkillEmberSting,
        // Appended (enum is int-serialized in AudioLibrary.asset — never insert).
        UIHover,
    }
}
