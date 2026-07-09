using System;
using SurveHive.Combat.Status;
using SurveHive.Data;

namespace SurveHive.Progression
{
    /// <summary>
    /// Run-scoped elemental set state (TODO #19): how many enhancements+abilities
    /// of each element the player owns, and the bonuses the resulting tiers grant.
    /// Static service in the PlayerContext/GamePause mold — re-initialized by
    /// LevelUpUIController.Awake on every run, fed after each pick, queried from
    /// hot paths (status application, basic-attack fire) without lookups.
    /// Multipliers are cached per element on change so queries are branch + array
    /// reads only (zero-GC).
    /// </summary>
    public static class ElementSets
    {
        public const int ElementCount = 6;

        private static readonly SetBonusSO[] _bonusByElement = new SetBonusSO[ElementCount];
        private static readonly int[] _pieces = new int[ElementCount];
        private static readonly int[] _tierIndex = new int[ElementCount];
        private static readonly float[] _potencyMultiplier = new float[ElementCount];
        private static readonly float[] _durationMultiplier = new float[ElementCount];
        private static float _attackDamageMultiplier = 1f;
        // 0 while the Physical set's top-tier Execute signature is inactive; the
        // basic-attack hot path reads this single field to gate low-HP executes.
        private static float _executeThresholdFraction;

        // A HUD/consumer can run before LevelUpUIController.Awake initializes us
        // (Unity gives no cross-object Awake/OnEnable ordering) — without this the
        // default all-zero _tierIndex would read as "tier I active on everything".
        static ElementSets()
        {
            RecomputeMultipliers();
        }

        /// <summary>Raised whenever any element's piece count changes (drives the HUD).</summary>
        public static event Action OnChanged;

        public static float AttackDamageMultiplier => _attackDamageMultiplier;

        /// <summary>
        /// Fraction of max HP at or below which a basic attack executes an enemy
        /// (Physical top-tier signature). 0 when the signature is inactive.
        /// </summary>
        public static float ExecuteThresholdFraction => _executeThresholdFraction;

        /// <summary>Wires the per-element configs and resets all counts for a fresh run.</summary>
        public static void Initialize(SetBonusSO[] bonuses)
        {
            for (int i = 0; i < ElementCount; i++)
            {
                _bonusByElement[i] = null;
            }

            if (bonuses != null)
            {
                for (int i = 0; i < bonuses.Length; i++)
                {
                    SetBonusSO bonus = bonuses[i];
                    if (bonus != null)
                    {
                        _bonusByElement[(int)bonus.Element] = bonus;
                    }
                }
            }

            for (int i = 0; i < ElementCount; i++)
            {
                _pieces[i] = 0;
            }

            RecomputeMultipliers();
            // Consumers that subscribed before this ran (enable-order is
            // arbitrary) must drop any stale previous-run display.
            OnChanged?.Invoke();
        }

        /// <summary>
        /// Pushes fresh owned-piece counts (indexed by SkillElement). Fires
        /// <see cref="OnChanged"/> only when something actually changed.
        /// </summary>
        public static void UpdateCounts(int[] piecesByElement)
        {
            bool changed = false;
            for (int i = 0; i < ElementCount; i++)
            {
                if (_pieces[i] != piecesByElement[i])
                {
                    _pieces[i] = piecesByElement[i];
                    changed = true;
                }
            }

            if (!changed)
            {
                return;
            }

            RecomputeMultipliers();
            OnChanged?.Invoke();
        }

        public static int GetPieces(SkillElement element) => _pieces[(int)element];

        /// <summary>Active tier index for the element (-1 = no set bonus active).</summary>
        public static int GetTierIndex(SkillElement element) => _tierIndex[(int)element];

        public static SetBonusSO GetBonus(SkillElement element) => _bonusByElement[(int)element];

        /// <summary>True when the element's set is at its top (4-piece) tier.</summary>
        public static bool IsTopTierActive(SkillElement element)
        {
            SetBonusSO bonus = _bonusByElement[(int)element];
            return bonus != null && bonus.TierCount > 0 && _tierIndex[(int)element] == bonus.TopTierIndex;
        }

        /// <summary>
        /// The element's unlocked signature effect, or <see cref="SetSignatureType.None"/>
        /// unless the top tier is active and the set actually defines one.
        /// </summary>
        public static SetSignatureType GetSignature(SkillElement element)
        {
            SetBonusSO bonus = _bonusByElement[(int)element];
            if (bonus == null || !IsTopTierActive(element))
            {
                return SetSignatureType.None;
            }

            return bonus.Signature;
        }

        /// <summary>Potency multiplier for a status about to be applied to an enemy.</summary>
        public static float GetStatusPotencyMultiplier(StatusEffectType status)
        {
            return _potencyMultiplier[(int)StatusElement(status)];
        }

        /// <summary>Duration multiplier for a status about to be applied to an enemy.</summary>
        public static float GetStatusDurationMultiplier(StatusEffectType status)
        {
            return _durationMultiplier[(int)StatusElement(status)];
        }

        // Which element's set amplifies each status: burn is fire's payoff,
        // poison poison's, stun electric's, freeze+cold frost's, slow honey's.
        private static SkillElement StatusElement(StatusEffectType status)
        {
            switch (status)
            {
                case StatusEffectType.Burn:
                    return SkillElement.Fire;
                case StatusEffectType.Poison:
                    return SkillElement.Poison;
                case StatusEffectType.Stun:
                    return SkillElement.Electric;
                case StatusEffectType.Freeze:
                case StatusEffectType.Cold:
                    return SkillElement.Frost;
                default:
                    return SkillElement.Honey;
            }
        }

        private static void RecomputeMultipliers()
        {
            _attackDamageMultiplier = 1f;
            _executeThresholdFraction = 0f;
            for (int i = 0; i < ElementCount; i++)
            {
                SetBonusSO bonus = _bonusByElement[i];
                int tier = bonus != null ? bonus.GetTierIndex(_pieces[i]) : -1;
                _tierIndex[i] = tier;

                if (tier < 0)
                {
                    _potencyMultiplier[i] = 1f;
                    _durationMultiplier[i] = 1f;
                    continue;
                }

                SetBonusTier row = bonus.GetTier(tier);
                _potencyMultiplier[i] = 1f + (row.StatusPotencyBonusPercent / 100f);
                _durationMultiplier[i] = 1f + (row.StatusDurationBonusPercent / 100f);
                if (row.AttackDamageBonusPercent > 0f)
                {
                    _attackDamageMultiplier *= 1f + (row.AttackDamageBonusPercent / 100f);
                }

                // Physical top-tier Execute signature: cache its HP threshold for
                // the basic-attack hot path (SignaturePotency is a percent).
                if (tier == bonus.TopTierIndex && bonus.Signature == SetSignatureType.Execute)
                {
                    _executeThresholdFraction = bonus.SignaturePotency / 100f;
                }
            }
        }
    }
}
