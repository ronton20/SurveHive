using System.Text;
using SurveHive.Core;
using SurveHive.Data;
using TMPro;
using UnityEngine;

namespace SurveHive.UI
{
    /// <summary>
    /// Main-menu achievements panel (PLAN 5D): a scrollable list of every
    /// catalog entry with its reward and unlocked state, plus an UNLOCKED n/m
    /// counter. Rebuilt each time the panel opens — menu path, allocations
    /// are fine here.
    /// </summary>
    public sealed class AchievementsUI : MonoBehaviour
    {
        [SerializeField] private MetaProgressionStoreSO _store;
        [SerializeField] private AchievementCatalogSO _catalog;
        [SerializeField] private CosmeticCatalogSO _cosmeticCatalog;
        [SerializeField] private AchievementEntryUI _entryPrefab;
        [SerializeField] private RectTransform _listContent;
        [SerializeField] private TMP_Text _counterText;

        private readonly StringBuilder _rewardBuilder = new StringBuilder(64);

        private void OnEnable()
        {
            Rebuild();
        }

        private void Rebuild()
        {
            if (_store == null || _catalog == null || _entryPrefab == null || _listContent == null)
            {
                return;
            }

            for (int i = _listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_listContent.GetChild(i).gameObject);
            }

            AchievementSO[] achievements = _catalog.Achievements;
            int unlockedCount = 0;
            for (int i = 0; i < achievements.Length; i++)
            {
                AchievementSO achievement = achievements[i];
                if (achievement == null)
                {
                    continue;
                }

                bool unlocked = _store.IsAchievementUnlocked(achievement.AchievementId);
                if (unlocked)
                {
                    unlockedCount++;
                }

                AchievementEntryUI row = Instantiate(_entryPrefab, _listContent);
                row.Bind(achievement, unlocked, _cosmeticCatalog, _rewardBuilder);
            }

            if (_counterText != null)
            {
                _counterText.text = Loc.Get(LocKeys.AchievementsUnlockedPrefix)
                    + unlockedCount + "/" + achievements.Length;
            }
        }
    }
}
