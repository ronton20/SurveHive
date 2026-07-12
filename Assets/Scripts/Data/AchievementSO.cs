using UnityEngine;

namespace SurveHive.Data
{
    /// <summary>
    /// One achievement (PLAN 5D): a threshold condition over signals the game
    /// already emits, plus its rewards — Royal Jelly and/or a cosmetic unlock
    /// from the 5C catalog. Unlocked ids persist in the save; the id doubles as
    /// the key a Steam backend maps to its API name later.
    /// </summary>
    [CreateAssetMenu(menuName = "SurveHive/Achievement", fileName = "Achievement")]
    public sealed class AchievementSO : ScriptableObject
    {
        [SerializeField] private string _achievementId;
        [SerializeField] private string _displayName;
        [TextArea]
        [SerializeField] private string _description;

        [Header("Condition")]
        [SerializeField] private AchievementConditionType _conditionType;
        // KillsInRun: kill count · ReachLevel: hero level · SurviveSeconds:
        // seconds · SetTierActive: tier number (1 = first) · ClearStage:
        // minimum (int)DifficultyTier.
        [SerializeField] private int _threshold = 1;

        [Header("Rewards")]
        [SerializeField] private int _jellyReward;
        // Cosmetic granted on unlock ("" = none); must match a CosmeticSO id.
        [SerializeField] private string _cosmeticRewardId = string.Empty;

        public string AchievementId => _achievementId;

        public string DisplayName => _displayName;

        public string Description => _description;

        public AchievementConditionType ConditionType => _conditionType;

        public int Threshold => _threshold;

        public int JellyReward => _jellyReward;

        public string CosmeticRewardId => _cosmeticRewardId;
    }
}
