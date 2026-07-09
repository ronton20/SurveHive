using System;
using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Data
{
    // One tier row of an element's set bonus. Values are the TOTAL bonus while
    // that tier is active (not deltas): potency/duration feed the element's
    // status effects, attack damage feeds the basic attack (the Physical set).
    [Serializable]
    public struct SetBonusTier
    {
        public int PiecesRequired;
        [Range(0f, 300f)] public float StatusPotencyBonusPercent;
        [Range(0f, 300f)] public float StatusDurationBonusPercent;
        [Range(0f, 100f)] public float AttackDamageBonusPercent;
        // Short player-facing line ("Burns last 30% longer").
        public string Description;
    }

    /// <summary>
    /// Elemental set bonus config (TODO #19): escalating rewards for owning
    /// 2 / 3 / 4+ enhancements+abilities of one element. Consumed at runtime
    /// through the <see cref="Progression.ElementSets"/> service; one asset per
    /// element, authored by the 3C builder pass and registered on the
    /// <see cref="SkillDatabaseSO"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "SurveHive/Set Bonus", fileName = "NewSetBonus")]
    public sealed class SetBonusSO : ScriptableObject
    {
        [SerializeField] private SkillElement _element;
        [SerializeField] private string _setName;
        // Ordered ascending by PiecesRequired (validated by the scene validator).
        [SerializeField] private SetBonusTier[] _tiers;

        // Top-tier (4-piece) signature effect (PLAN 2B). Appended fields:
        // existing assets default to None until the SetSignatureBuilder pass
        // authors them. Only meaningful while the top tier is active.
        [SerializeField] private SetSignatureType _signature = SetSignatureType.None;
        [Tooltip("AoE radius for shatter/pool/slick zones and spread/chain search range.")]
        [SerializeField] private float _signatureRadius;
        [Tooltip("Signature magnitude: shatter % of victim max HP, execute HP% threshold, pool DPS, slick slow fraction.")]
        [SerializeField] private float _signaturePotency;
        [Tooltip("Lifetime of spawned zones / applied statuses, seconds.")]
        [SerializeField] private float _signatureDuration;
        [SerializeField] private string _signatureDescription;

        public SkillElement Element => _element;

        public string SetName => _setName;

        public int TierCount => _tiers != null ? _tiers.Length : 0;

        /// <summary>Index of the top (4-piece) tier, or -1 if there are no tiers.</summary>
        public int TopTierIndex => TierCount - 1;

        public SetSignatureType Signature => _signature;

        public float SignatureRadius => _signatureRadius;

        public float SignaturePotency => _signaturePotency;

        public float SignatureDuration => _signatureDuration;

        public string SignatureDescription => _signatureDescription;

        public SetBonusTier GetTier(int tierIndex) => _tiers[tierIndex];

        /// <summary>Highest tier index unlocked by owning <paramref name="pieces"/> pieces, or -1.</summary>
        public int GetTierIndex(int pieces)
        {
            int result = -1;
            if (_tiers == null)
            {
                return result;
            }

            for (int i = 0; i < _tiers.Length; i++)
            {
                if (pieces >= _tiers[i].PiecesRequired)
                {
                    result = i;
                }
            }

            return result;
        }

        /// <summary>Authoring entry point for the builder pass and EditMode tests.</summary>
        public void Configure(SkillElement element, string setName, SetBonusTier[] tiers)
        {
            _element = element;
            _setName = setName;
            _tiers = tiers;
        }

        /// <summary>Authoring entry point for the top-tier signature (builder pass + tests).</summary>
        public void ConfigureSignature(
            SetSignatureType signature, float radius, float potency, float duration, string description)
        {
            _signature = signature;
            _signatureRadius = radius;
            _signaturePotency = potency;
            _signatureDuration = duration;
            _signatureDescription = description;
        }
    }
}
