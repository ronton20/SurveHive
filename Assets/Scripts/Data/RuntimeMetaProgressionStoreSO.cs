using SurveHive.Core;
using UnityEngine;

namespace SurveHive.Data
{
    [CreateAssetMenu(menuName = "SurveHive/Runtime Meta Progression Store", fileName = "RuntimeMetaProgressionStore")]
    public sealed class RuntimeMetaProgressionStoreSO : ScriptableObject, IMetaProgressionStore
    {
        [SerializeField] private int _bankedTotal;

        public int BankedCurrency => _bankedTotal;

        public void BankRunCurrency(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _bankedTotal += amount;
        }
    }
}
