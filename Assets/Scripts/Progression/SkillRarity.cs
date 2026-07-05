namespace SurveHive.Progression
{
    public enum SkillRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2
    }

    /// <summary>
    /// Rarity tier → offer weight (replaces the old flat per-skill weight) and
    /// card frame color hook. Pure data so EditMode tests can verify the
    /// distribution.
    /// </summary>
    public static class SkillRarityWeights
    {
        public const float CommonWeight = 1f;
        // Rare bumped 0.4 -> 0.65 and Epic 0.15 -> 0.22 (2026-07-05): rares were
        // barely showing across a full run. Still Common > Rare > Epic.
        public const float RareWeight = 0.65f;
        public const float EpicWeight = 0.22f;

        public static float GetWeight(SkillRarity rarity)
        {
            switch (rarity)
            {
                case SkillRarity.Rare:
                    return RareWeight;
                case SkillRarity.Epic:
                    return EpicWeight;
                default:
                    return CommonWeight;
            }
        }
    }
}
