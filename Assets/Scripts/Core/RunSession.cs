using SurveHive.Currency;
using SurveHive.Data;
using UnityEngine;

namespace SurveHive.Core
{
    public sealed class RunSession : MonoBehaviour
    {
        [SerializeField] private RunCurrencyWallet _currencyWallet;
        [SerializeField] private RuntimeMetaProgressionStoreSO _metaProgressionStore;

        public RunCurrencyWallet Currency => _currencyWallet;

        public void EndRun()
        {
            if (_metaProgressionStore != null)
            {
                _metaProgressionStore.BankRunCurrency(_currencyWallet.TotalCurrency);
            }
        }
    }
}
