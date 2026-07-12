using SurveHive.Data;

namespace SurveHive.Progression
{
    /// <summary>
    /// Pure achievement logic (PLAN 5D), EditMode-tested: threshold checks over
    /// an <see cref="AchievementRunStats"/> snapshot, and the one-shot grant
    /// that marks an achievement unlocked and pays its rewards (Royal Jelly
    /// and/or a cosmetic) through the store seam. Granting is idempotent —
    /// an already-unlocked id never pays twice.
    /// </summary>
    public static class AchievementRules
    {
        public static bool IsSatisfied(AchievementSO achievement, in AchievementRunStats stats)
        {
            if (achievement == null)
            {
                return false;
            }

            switch (achievement.ConditionType)
            {
                case AchievementConditionType.KillsInRun:
                    return stats.Kills >= achievement.Threshold;
                case AchievementConditionType.ReachLevel:
                    return stats.Level >= achievement.Threshold;
                case AchievementConditionType.SurviveSeconds:
                    return stats.SurvivedSeconds >= achievement.Threshold;
                case AchievementConditionType.SetTierActive:
                    return stats.MaxSetTier >= achievement.Threshold;
                case AchievementConditionType.ClearStage:
                    return stats.ClearedDifficulty >= achievement.Threshold;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Marks the achievement unlocked and grants its rewards; returns true
        /// only when it was newly unlocked (so callers can toast/report once).
        /// </summary>
        public static bool TryGrant(MetaProgressionStoreSO store, AchievementSO achievement)
        {
            if (store == null || achievement == null
                || string.IsNullOrEmpty(achievement.AchievementId)
                || store.IsAchievementUnlocked(achievement.AchievementId))
            {
                return false;
            }

            store.UnlockAchievement(achievement.AchievementId);
            if (achievement.JellyReward > 0)
            {
                store.BankJelly(achievement.JellyReward);
            }

            if (!string.IsNullOrEmpty(achievement.CosmeticRewardId))
            {
                store.UnlockCosmetic(achievement.CosmeticRewardId);
            }

            return true;
        }
    }
}
