using SurveHive.Core;
using SurveHive.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// One cell in the tabbed shop's category grid: the upgrade's icon with a
    /// <c>[current]/[max]</c> level label under it, a click target, and a
    /// selection highlight the panel toggles when this cell is the one shown in
    /// the detail pane. Menu-only, so the tiny refresh-time string is fine.
    /// </summary>
    public sealed class MetaShopIconUI : MonoBehaviour
    {
        [SerializeField] private MetaUpgradeSO _upgrade;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Button _button;
        // Border/glow shown only while this cell is the selected one.
        [SerializeField] private Image _selectionHighlight;

        // Dimmed once the upgrade is maxed so a full category reads at a glance.
        private static readonly Color IconNormal = Color.white;
        private static readonly Color IconMaxed = new Color(0.62f, 0.58f, 0.5f, 1f);

        public MetaUpgradeSO Upgrade => _upgrade;

        public Button Button => _button;

        /// <summary>The tab this cell belongs to — used to filter the grid.</summary>
        public MetaShopCategory Category => _upgrade.Category;

        /// <summary>Assigns the upgrade this cell represents and shows its icon.</summary>
        public void Bind(MetaUpgradeSO upgrade)
        {
            _upgrade = upgrade;

            if (_iconImage != null)
            {
                bool hasIcon = upgrade.Icon != null;
                _iconImage.enabled = hasIcon;
                if (hasIcon)
                {
                    _iconImage.sprite = upgrade.Icon;
                }
            }
        }

        public void Refresh(IMetaProgressionStore store)
        {
            int rank = store.GetUpgradeRank(_upgrade.UpgradeId);
            bool maxed = rank >= _upgrade.MaxRank;

            _levelText.text = rank + "/" + _upgrade.MaxRank;

            if (_iconImage != null)
            {
                _iconImage.color = maxed ? IconMaxed : IconNormal;
            }
        }

        public void SetSelected(bool selected)
        {
            if (_selectionHighlight != null)
            {
                _selectionHighlight.enabled = selected;
            }
        }
    }
}
