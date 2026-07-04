using SurveHive.Data;
using SurveHive.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SurveHive.UI
{
    /// <summary>
    /// The Hive Upgrades shop panel: shows banked honey and the six permanent
    /// upgrade rows; buying goes through <see cref="MetaShop"/> against the
    /// persistent store and refreshes everything.
    /// </summary>
    public sealed class MetaShopUI : MonoBehaviour
    {
        [SerializeField] private MetaProgressionStoreSO _store;
        [SerializeField] private MetaShopRowUI[] _rows;
        [SerializeField] private TMP_Text _balanceText;

        private UnityAction[] _buyHandlers;

        private void Awake()
        {
            _buyHandlers = new UnityAction[_rows.Length];
            for (int i = 0; i < _rows.Length; i++)
            {
                MetaShopRowUI row = _rows[i];
                _buyHandlers[i] = () => Purchase(row);
                row.BuyButton.onClick.AddListener(_buyHandlers[i]);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i].BuyButton.onClick.RemoveListener(_buyHandlers[i]);
            }
        }

        private void OnEnable()
        {
            RefreshAll();
        }

        private void Purchase(MetaShopRowUI row)
        {
            if (MetaShop.TryPurchase(_store, row.Upgrade))
            {
                RefreshAll();
            }
        }

        private void RefreshAll()
        {
            if (_balanceText != null)
            {
                _balanceText.text = $"HONEY: {_store.BankedCurrency}";
            }

            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i].Refresh(_store);
            }
        }
    }
}
