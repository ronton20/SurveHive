using System;
using UnityEngine;

namespace SurveHive.Data
{
    /// <summary>
    /// Data-driven difficulty tier table (PLAN 1B): each tier scales enemy
    /// toughness (HP/damage, optionally spawn rate) and compensates with a
    /// honey-gain multiplier. Consumers resolve their tier once at run start;
    /// a tier missing from the table falls back to identity multipliers so a
    /// mis-wired scene plays as Normal instead of breaking.
    /// </summary>
    [CreateAssetMenu(menuName = "SurveHive/Difficulty Settings", fileName = "DifficultySettings")]
    public sealed class DifficultySO : ScriptableObject
    {
        /// <summary>One "clear stage X on difficulty Y" gate for a tier.</summary>
        [Serializable]
        public struct UnlockRequirement
        {
            // Save-record key (RunSession._stageId) + the player-facing name.
            public string stageId;
            public string stageName;
            public DifficultyTier clearTier;
        }

        [Serializable]
        public struct TierSettings
        {
            public DifficultyTier tier;
            public string displayName;
            public Sprite icon;
            public float enemyHealthMultiplier;
            public float enemyDamageMultiplier;
            public float spawnRateMultiplier;
            public float honeyGainMultiplier;
            // Append-only (serialized): empty/missing = always unlocked.
            public UnlockRequirement[] unlockRequirements;

            public static TierSettings Identity => new TierSettings
            {
                tier = DifficultyTier.Normal,
                displayName = "NORMAL",
                icon = null,
                enemyHealthMultiplier = 1f,
                enemyDamageMultiplier = 1f,
                spawnRateMultiplier = 1f,
                honeyGainMultiplier = 1f,
                unlockRequirements = null,
            };
        }

        [SerializeField] private TierSettings[] _tiers;

        public int TierCount => _tiers != null ? _tiers.Length : 0;

        public TierSettings GetTierAt(int index)
        {
            if (_tiers == null || index < 0 || index >= _tiers.Length)
            {
                return TierSettings.Identity;
            }

            return _tiers[index];
        }

        public TierSettings GetSettings(DifficultyTier tier)
        {
            if (_tiers != null)
            {
                for (int i = 0; i < _tiers.Length; i++)
                {
                    if (_tiers[i].tier == tier)
                    {
                        return _tiers[i];
                    }
                }
            }

            return TierSettings.Identity;
        }
    }
}
