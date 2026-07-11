using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// The Hive Upgrades shop panel (3B tabbed layout): category tabs on the
    /// left (Combat / Survival / Utility), a detail pane up top showing the
    /// selected upgrade with the BUY button, and a bottom grid of just the
    /// current category's upgrade icons (each with its <c>rank/max</c>).
    ///
    /// One <see cref="MetaShopIconUI"/> is spawned per <see cref="_catalog"/>
    /// entry under <see cref="_gridContent"/> at startup and shown/hidden by tab;
    /// clicking one drives <see cref="_detail"/>. Buying goes through
    /// <see cref="MetaShop"/> against the persistent store and refreshes
    /// everything. Menu-only path, so refresh-time string work is fine.
    /// </summary>
    public sealed class MetaShopUI : MonoBehaviour
    {
        [SerializeField] private MetaProgressionStoreSO _store;
        [SerializeField] private MetaUpgradeCatalogSO _catalog;
        [SerializeField] private TMP_Text _balanceText;
        // Royal Jelly balance (PLAN 5B) — read-only until 5C/5E spend it here.
        [SerializeField] private TMP_Text _jellyText;

        [Header("Tabbed layout")]
        [SerializeField] private MetaShopIconUI _iconPrefab;
        [SerializeField] private RectTransform _gridContent;
        [SerializeField] private MetaShopDetailUI _detail;
        // Parallel arrays indexed by (int)MetaShopCategory: Combat=0, Survival=1, Utility=2.
        [SerializeField] private Button[] _tabButtons;
        [SerializeField] private Image[] _tabHighlights;

        private MetaShopIconUI[] _icons;
        private UnityAction[] _iconHandlers;
        private UnityAction[] _tabHandlers;
        private UnityAction _buyHandler;

        private MetaShopCategory _activeCategory = MetaShopCategory.Combat;
        private MetaShopIconUI _selected;

        private void Awake()
        {
            BuildIcons();
            WireTabs();
            WireBuy();
        }

        private void BuildIcons()
        {
            bool canSpawn = _catalog != null && _iconPrefab != null && _gridContent != null
                && _catalog.Upgrades != null && _catalog.Upgrades.Length > 0;

            if (!canSpawn)
            {
                _icons = System.Array.Empty<MetaShopIconUI>();
                _iconHandlers = System.Array.Empty<UnityAction>();
                return;
            }

            MetaUpgradeSO[] upgrades = _catalog.Upgrades;
            _icons = new MetaShopIconUI[upgrades.Length];
            _iconHandlers = new UnityAction[upgrades.Length];

            for (int i = 0; i < upgrades.Length; i++)
            {
                MetaShopIconUI icon = Instantiate(_iconPrefab, _gridContent);
                icon.name = $"Icon_{upgrades[i].name}";
                icon.Bind(upgrades[i]);
                _icons[i] = icon;

                MetaShopIconUI captured = icon;
                _iconHandlers[i] = () => Select(captured);
                icon.Button.onClick.AddListener(_iconHandlers[i]);
            }
        }

        private void WireTabs()
        {
            if (_tabButtons == null)
            {
                return;
            }

            _tabHandlers = new UnityAction[_tabButtons.Length];
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null)
                {
                    continue;
                }

                var category = (MetaShopCategory)i;
                _tabHandlers[i] = () => ApplyCategory(category);
                _tabButtons[i].onClick.AddListener(_tabHandlers[i]);
            }
        }

        private void WireBuy()
        {
            if (_detail == null || _detail.BuyButton == null)
            {
                return;
            }

            _buyHandler = Purchase;
            _detail.BuyButton.onClick.AddListener(_buyHandler);
        }

        private void OnDestroy()
        {
            if (_icons != null)
            {
                for (int i = 0; i < _icons.Length; i++)
                {
                    if (_icons[i] != null)
                    {
                        _icons[i].Button.onClick.RemoveListener(_iconHandlers[i]);
                    }
                }
            }

            if (_tabButtons != null && _tabHandlers != null)
            {
                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    if (_tabButtons[i] != null && _tabHandlers[i] != null)
                    {
                        _tabButtons[i].onClick.RemoveListener(_tabHandlers[i]);
                    }
                }
            }

            if (_detail != null && _detail.BuyButton != null && _buyHandler != null)
            {
                _detail.BuyButton.onClick.RemoveListener(_buyHandler);
            }
        }

        private void OnEnable()
        {
            // OnEnable can fire before Awake on the very first activation; guard it.
            if (_icons == null)
            {
                return;
            }

            RefreshBalanceAndIcons();
            ApplyCategory(_activeCategory);
        }

        private void ApplyCategory(MetaShopCategory category)
        {
            _activeCategory = category;

            MetaShopIconUI firstInCategory = null;
            for (int i = 0; i < _icons.Length; i++)
            {
                bool inCategory = _icons[i].Category == category;
                _icons[i].gameObject.SetActive(inCategory);
                if (inCategory && firstInCategory == null)
                {
                    firstInCategory = _icons[i];
                }
            }

            UpdateTabHighlights();

            // Keep the current selection if it belongs to this tab; otherwise
            // fall to the first upgrade in the newly-shown category.
            if (_selected == null || _selected.Category != category)
            {
                Select(firstInCategory);
            }
            else
            {
                RefreshDetail();
            }
        }

        private void UpdateTabHighlights()
        {
            if (_tabHighlights == null)
            {
                return;
            }

            for (int i = 0; i < _tabHighlights.Length; i++)
            {
                if (_tabHighlights[i] != null)
                {
                    _tabHighlights[i].enabled = (MetaShopCategory)i == _activeCategory;
                }
            }
        }

        private void Select(MetaShopIconUI icon)
        {
            _selected = icon;
            for (int i = 0; i < _icons.Length; i++)
            {
                _icons[i].SetSelected(_icons[i] == icon);
            }

            RefreshDetail();
        }

        private void Purchase()
        {
            if (_selected == null)
            {
                return;
            }

            if (MetaShop.TryPurchase(_store, _selected.Upgrade))
            {
                RefreshBalanceAndIcons();
                RefreshDetail();
            }
        }

        private void RefreshBalanceAndIcons()
        {
            if (_balanceText != null)
            {
                _balanceText.text = CurrencyGlyphs.Honey + _store.BankedCurrency;
            }

            if (_jellyText != null)
            {
                _jellyText.text = CurrencyGlyphs.Jelly + _store.BankedJelly;
            }

            for (int i = 0; i < _icons.Length; i++)
            {
                _icons[i].Refresh(_store);
            }
        }

        private void RefreshDetail()
        {
            if (_detail == null)
            {
                return;
            }

            _detail.Bind(_selected != null ? _selected.Upgrade : null, _store);
        }
    }
}
