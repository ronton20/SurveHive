using SurveHive.Core;
using SurveHive.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// The tabbed shop's top-half detail pane: shows everything about the
    /// currently-selected upgrade — icon, name, description, rank, the concrete
    /// stat-value transition, cost — and owns the BUY button. Purely a view;
    /// <see cref="MetaShopUI"/> drives it and handles the purchase.
    /// </summary>
    public sealed class MetaShopDetailUI : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _rankText;
        // The concrete value change: "+25 → +50 Max HP".
        [SerializeField] private TMP_Text _effectText;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private Button _buyButton;
        [SerializeField] private TMP_Text _buyLabel;

        public Button BuyButton => _buyButton;

        /// <summary>The upgrade currently shown — the one BUY purchases.</summary>
        public MetaUpgradeSO Current { get; private set; }

        private void Awake()
        {
            if (_buyLabel != null)
            {
                _buyLabel.text = Loc.Get(LocKeys.ShopBuy);
            }
        }

        /// <summary>Repaints the pane for <paramref name="upgrade"/> at its current rank.</summary>
        public void Bind(MetaUpgradeSO upgrade, IMetaProgressionStore store)
        {
            Current = upgrade;

            if (upgrade == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (_iconImage != null)
            {
                bool hasIcon = upgrade.Icon != null;
                _iconImage.enabled = hasIcon;
                if (hasIcon)
                {
                    _iconImage.sprite = upgrade.Icon;
                }
            }

            int rank = store.GetUpgradeRank(upgrade.UpgradeId);
            bool maxed = rank >= upgrade.MaxRank;

            _nameText.text = upgrade.DisplayName;

            if (_descriptionText != null)
            {
                _descriptionText.text = upgrade.Description;
            }

            _rankText.text = Loc.Get(LocKeys.ShopRankPrefix) + rank + "/" + upgrade.MaxRank;

            if (_effectText != null)
            {
                _effectText.text = upgrade.FormatEffectTransition(rank);
            }

            if (maxed)
            {
                _costText.text = Loc.Get(LocKeys.Max);
                _buyButton.interactable = false;
            }
            else
            {
                int cost = upgrade.CostForRank(rank);
                _costText.text = Loc.Get(LocKeys.ShopCostPrefix) + cost;
                _buyButton.interactable = store.BankedCurrency >= cost;
            }
        }
    }
}
