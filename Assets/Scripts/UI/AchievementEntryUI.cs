using System.Text;
using SurveHive.Core;
using SurveHive.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// One row in the achievements panel (PLAN 5D): name, description, reward
    /// line, and an unlocked badge; locked rows render dimmed. Menu path only.
    /// </summary>
    public sealed class AchievementEntryUI : MonoBehaviour
    {
        private static readonly Color UnlockedName = new Color(1f, 0.765f, 0.043f);
        private static readonly Color LockedName = new Color(0.62f, 0.58f, 0.52f);
        private static readonly Color UnlockedBody = new Color(0.91f, 0.847f, 0.627f);
        private static readonly Color LockedBody = new Color(0.55f, 0.51f, 0.46f);

        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _rewardText;
        [SerializeField] private Image _unlockedBadge;

        public void Bind(
            AchievementSO achievement, bool unlocked, CosmeticCatalogSO cosmeticCatalog,
            StringBuilder rewardBuilder)
        {
            if (achievement == null)
            {
                return;
            }

            if (_nameText != null)
            {
                _nameText.text = achievement.DisplayName;
                _nameText.color = unlocked ? UnlockedName : LockedName;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = achievement.Description;
                _descriptionText.color = unlocked ? UnlockedBody : LockedBody;
            }

            if (_rewardText != null)
            {
                _rewardText.text = BuildRewardLine(achievement, cosmeticCatalog, rewardBuilder);
                _rewardText.color = unlocked ? UnlockedBody : LockedBody;
            }

            if (_unlockedBadge != null)
            {
                _unlockedBadge.enabled = unlocked;
            }
        }

        private static string BuildRewardLine(
            AchievementSO achievement, CosmeticCatalogSO cosmeticCatalog, StringBuilder builder)
        {
            builder.Length = 0;
            if (achievement.JellyReward > 0)
            {
                builder.Append(Loc.Get(LocKeys.AchievementsRewardPrefix));
                builder.Append(CurrencyGlyphs.Jelly);
                builder.Append('+');
                builder.Append(achievement.JellyReward);
            }

            if (!string.IsNullOrEmpty(achievement.CosmeticRewardId) && cosmeticCatalog != null)
            {
                CosmeticSO cosmetic = cosmeticCatalog.FindById(achievement.CosmeticRewardId);
                if (cosmetic != null)
                {
                    builder.Append(builder.Length > 0
                        ? "  +  "
                        : Loc.Get(LocKeys.AchievementsRewardPrefix));
                    builder.Append(cosmetic.DisplayName);
                }
            }

            return builder.ToString();
        }
    }
}
