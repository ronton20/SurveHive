using SurveHive.Core;
using SurveHive.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// One shop card: name, description, owned rank, next-rank cost, buy button.
    /// Menu-only path, so the small refresh-time string allocations are fine.
    /// </summary>
    public sealed class MetaShopCardUI : MonoBehaviour
    {
        [SerializeField] private MetaUpgradeSO _upgrade;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;
        // The concrete value change: "+25 → +50 Max HP".
        [SerializeField] private TMP_Text _effectText;
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private Button _buyButton;

        public MetaUpgradeSO Upgrade => _upgrade;

        public Button BuyButton => _buyButton;

        /// <summary>
        /// Assigns the upgrade this row represents. Used when the shop spawns rows
        /// from a prefab at runtime; baked rows keep their serialized <c>_upgrade</c>.
        /// </summary>
        public void Bind(MetaUpgradeSO upgrade)
        {
            _upgrade = upgrade;
        }

        public void Refresh(IMetaProgressionStore store)
        {
            int rank = store.GetUpgradeRank(_upgrade.UpgradeId);
            bool maxed = rank >= _upgrade.MaxRank;

            _nameText.text = _upgrade.DisplayName;
            if (_descriptionText != null)
            {
                _descriptionText.text = _upgrade.Description;
            }

            if (_effectText != null)
            {
                _effectText.text = _upgrade.FormatEffectTransition(rank);
            }

            _rankText.text = $"Rank {rank}/{_upgrade.MaxRank}";

            if (maxed)
            {
                _costText.text = "MAX";
                _buyButton.interactable = false;
            }
            else
            {
                int cost = _upgrade.CostForRank(rank);
                _costText.text = cost.ToString();
                _buyButton.interactable = store.BankedCurrency >= cost;
            }
        }
    }
}
