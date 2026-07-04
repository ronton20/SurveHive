using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Data
{
    /// <summary>
    /// One permanent shop upgrade: which stat it raises, how much per rank, and
    /// its escalating cost curve. Ranks themselves live in the meta store keyed
    /// by <see cref="UpgradeId"/>; these assets are pure definitions.
    /// </summary>
    [CreateAssetMenu(menuName = "SurveHive/Meta Upgrade", fileName = "MetaUpgrade")]
    public sealed class MetaUpgradeSO : ScriptableObject
    {
        [SerializeField] private string _upgradeId;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private MetaStatType _statType;
        [SerializeField] private int _maxRank = 10;
        [SerializeField] private int _baseCost = 50;
        // Cost multiplier applied per owned rank (1.4 = each rank ~40% pricier).
        [SerializeField] private float _costGrowth = 1.4f;
        // Flat HP for MaxHealth, percent points for everything else.
        [SerializeField] private float _effectPerRank = 5f;

        public string UpgradeId => _upgradeId;

        public string DisplayName => _displayName;

        public string Description => _description;

        public MetaStatType StatType => _statType;

        public int MaxRank => _maxRank;

        public int BaseCost => _baseCost;

        public float CostGrowth => _costGrowth;

        public float EffectPerRank => _effectPerRank;

        public int CostForRank(int currentRank)
        {
            return MetaUpgradeMath.CostForRank(_baseCost, _costGrowth, currentRank);
        }

        public float TotalEffectAtRank(int rank)
        {
            return MetaUpgradeMath.TotalEffectAtRank(_effectPerRank, rank);
        }
    }
}
