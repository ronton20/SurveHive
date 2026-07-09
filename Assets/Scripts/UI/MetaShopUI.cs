using SurveHive.Data;
using SurveHive.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SurveHive.UI
{
    /// <summary>
    /// The Hive Upgrades shop panel: shows banked honey and one card per upgrade
    /// in <see cref="_catalog"/>, spawned from <see cref="_cardPrefab"/> under
    /// <see cref="_content"/> at startup. Buying goes through <see cref="MetaShop"/>
    /// against the persistent store and refreshes everything.
    /// </summary>
    /// <remarks>
    /// If the catalog/prefab/content trio is not wired, the panel falls back to a
    /// pre-baked <see cref="_rows"/> array (the legacy scene-authored layout), so
    /// the shop keeps working while the scene migrates to the data-driven grid.
    /// </remarks>
    public sealed class MetaShopUI : MonoBehaviour
    {
        [SerializeField] private MetaProgressionStoreSO _store;

        [Header("Data-driven grid (preferred)")]
        [SerializeField] private MetaUpgradeCatalogSO _catalog;
        [SerializeField] private MetaShopCardUI _cardPrefab;
        [SerializeField] private RectTransform _content;

        [Header("Legacy baked rows (fallback)")]
        [SerializeField] private MetaShopCardUI[] _rows;

        [SerializeField] private TMP_Text _balanceText;

        // The active card set — spawned from the catalog when possible, else _rows.
        private MetaShopCardUI[] _activeCards;
        private UnityAction[] _buyHandlers;

        private void Awake()
        {
            _activeCards = BuildCards();

            _buyHandlers = new UnityAction[_activeCards.Length];
            for (int i = 0; i < _activeCards.Length; i++)
            {
                MetaShopCardUI card = _activeCards[i];
                _buyHandlers[i] = () => Purchase(card);
                card.BuyButton.onClick.AddListener(_buyHandlers[i]);
            }
        }

        private MetaShopCardUI[] BuildCards()
        {
            bool canSpawn = _catalog != null && _cardPrefab != null && _content != null
                && _catalog.Upgrades != null && _catalog.Upgrades.Length > 0;

            if (!canSpawn)
            {
                return _rows != null ? _rows : System.Array.Empty<MetaShopCardUI>();
            }

            MetaUpgradeSO[] upgrades = _catalog.Upgrades;
            var spawned = new MetaShopCardUI[upgrades.Length];
            for (int i = 0; i < upgrades.Length; i++)
            {
                MetaShopCardUI card = Instantiate(_cardPrefab, _content);
                card.name = $"Card_{upgrades[i].name}";
                card.Bind(upgrades[i]);
                spawned[i] = card;
            }

            return spawned;
        }

        private void OnDestroy()
        {
            if (_activeCards == null)
            {
                return;
            }

            for (int i = 0; i < _activeCards.Length; i++)
            {
                _activeCards[i].BuyButton.onClick.RemoveListener(_buyHandlers[i]);
            }
        }

        private void OnEnable()
        {
            RefreshAll();
        }

        private void Purchase(MetaShopCardUI card)
        {
            if (MetaShop.TryPurchase(_store, card.Upgrade))
            {
                RefreshAll();
            }
        }

        private void RefreshAll()
        {
            // OnEnable can fire before Awake on the very first activation; guard it.
            if (_activeCards == null)
            {
                return;
            }

            if (_balanceText != null)
            {
                _balanceText.text = $"HONEY: {_store.BankedCurrency}";
            }

            for (int i = 0; i < _activeCards.Length; i++)
            {
                _activeCards[i].Refresh(_store);
            }
        }
    }
}
