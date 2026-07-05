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
        // Short stat label for the value line on the card (e.g. "Max HP", "Damage").
        [SerializeField] private string _effectLabel;
        [SerializeField] private MetaStatType _statType;
        [SerializeField] private int _maxRank = 10;
        [SerializeField] private int _baseCost = 50;
        // Cost multiplier applied per owned rank (1.4 = each rank ~40% pricier).
        [SerializeField] private float _costGrowth = 1.4f;
        // Flat units for MaxHealth/AttackDamage, percent points for everything else.
        [SerializeField] private float _effectPerRank = 5f;

        public string UpgradeId => _upgradeId;

        public string DisplayName => _displayName;

        public string Description => _description;

        public string EffectLabel => _effectLabel;

        public MetaStatType StatType => _statType;

        public int MaxRank => _maxRank;

        public int BaseCost => _baseCost;

        public float CostGrowth => _costGrowth;

        public float EffectPerRank => _effectPerRank;

        /// <summary>
        /// Percent-based stats show a "+N%" effect; MaxHealth and AttackDamage are
        /// flat "+N" bonuses. Must match how <c>MetaUpgradeApplier</c> applies each.
        /// </summary>
        public bool IsPercent => _statType != MetaStatType.MaxHealth && _statType != MetaStatType.AttackDamage;

        public int CostForRank(int currentRank)
        {
            return MetaUpgradeMath.CostForRank(_baseCost, _costGrowth, currentRank);
        }

        public float TotalEffectAtRank(int rank)
        {
            return MetaUpgradeMath.TotalEffectAtRank(_effectPerRank, rank);
        }

        /// <summary>Cumulative effect at a rank as "+25" / "+15%" ("—" at rank 0).</summary>
        public string FormatEffect(int rank)
        {
            if (rank <= 0)
            {
                return "—";
            }

            int value = Mathf.RoundToInt(TotalEffectAtRank(rank));
            return IsPercent ? $"+{value}%" : $"+{value}";
        }

        /// <summary>
        /// The value line for a shop card: shows the current total effect moving
        /// to the next rank's ("+25 → +50 Max HP"), or the maxed total.
        /// </summary>
        public string FormatEffectTransition(int rank)
        {
            if (rank >= _maxRank)
            {
                return $"{FormatEffect(rank)} {_effectLabel} (MAX)";
            }

            return $"{FormatEffect(rank)} → {FormatEffect(rank + 1)} {_effectLabel}";
        }
    }
}
