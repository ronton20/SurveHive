using UnityEngine;

namespace SurveHive.Data
{
    /// <summary>
    /// The full achievement roster (PLAN 5D), authored by AchievementsBuilder.
    /// The run tracker scans it against live stats and the menu panel lists it.
    /// Menu/run-boot path only — linear lookups are fine.
    /// </summary>
    [CreateAssetMenu(menuName = "SurveHive/Achievement Catalog", fileName = "AchievementCatalog")]
    public sealed class AchievementCatalogSO : ScriptableObject
    {
        [SerializeField] private AchievementSO[] _achievements = new AchievementSO[0];

        public AchievementSO[] Achievements => _achievements;
    }
}
