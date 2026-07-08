using SurveHive.Core;
using SurveHive.Data;

namespace SurveHive.Progression
{
    /// <summary>
    /// Difficulty tier gating (1B follow-up): a tier is playable once every
    /// "clear stage X on difficulty Y" requirement on its row is met against
    /// the save's stage-clear record. Tiers without requirements (Easy,
    /// Normal) are always open. Pure logic — EditMode-tested.
    /// </summary>
    public static class DifficultyUnlocks
    {
        public static bool IsUnlocked(in DifficultySO.TierSettings tier, IMetaProgressionStore store)
        {
            if (tier.unlockRequirements == null || tier.unlockRequirements.Length == 0)
            {
                return true;
            }

            if (store == null)
            {
                return false;
            }

            for (int i = 0; i < tier.unlockRequirements.Length; i++)
            {
                if (!IsRequirementMet(tier.unlockRequirements[i], store))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsRequirementMet(in DifficultySO.UnlockRequirement requirement, IMetaProgressionStore store)
        {
            return store != null && store.HasStageClear(requirement.stageId, (int)requirement.clearTier);
        }
    }
}
