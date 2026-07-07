using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Data
{
    [CreateAssetMenu(menuName = "SurveHive/Skill Definition", fileName = "NewSkill")]
    public sealed class SkillDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private SkillEffectType _effectType;
        // Combat 2.0 taxonomy: which offer lane this card belongs to (drives the
        // per-lane selection cap + card banner) and its element cue. Default to
        // the neutral Passive/Physical so pre-taxonomy assets read sensibly.
        [SerializeField] private PowerUpLane _lane = PowerUpLane.Passive;
        [SerializeField] private SkillElement _element = SkillElement.Physical;
        [SerializeField] private float _magnitude;
        // Legacy flat weight — superseded by rarity-tier weighting (Phase 2);
        // kept so existing assets deserialize cleanly.
        [SerializeField] private float _weight = 1f;
        // Maximum number of times this skill can be taken. 0 = unlimited.
        [SerializeField] private int _maxLevel;
        [SerializeField] private Sprite _icon;
        [SerializeField] private SkillRarity _rarity = SkillRarity.Common;
        // Set when _effectType == ActiveSkill: the auto-firing weapon this card
        // unlocks/levels.
        [SerializeField] private ActiveSkillSO _activeSkill;
        // Optional non-linear growth: entry N-1 is the magnitude applied when
        // taking level N; levels past the end reuse the last entry. Empty =
        // flat _magnitude every level.
        [SerializeField] private float[] _magnitudePerLevel;

        public string Id => _id;

        public string DisplayName => _displayName;

        public string Description => _description;

        public SkillEffectType EffectType => _effectType;

        public PowerUpLane Lane => _lane;

        public SkillElement Element => _element;

        public float Magnitude => _magnitude;

        public float Weight => _weight;

        // 0 means no cap on how many times this skill can be taken.
        public int MaxLevel => _maxLevel;

        public bool HasLevelCap => _maxLevel > 0;

        public Sprite Icon => _icon;

        public SkillRarity Rarity => _rarity;

        public ActiveSkillSO ActiveSkill => _activeSkill;

        /// <summary>
        /// Magnitude applied when leveling up from <paramref name="currentLevel"/>
        /// (0 = first take). Falls back to the flat <see cref="Magnitude"/> when no
        /// per-level table is authored.
        /// </summary>
        public float MagnitudeForLevel(int currentLevel)
        {
            if (_magnitudePerLevel == null || _magnitudePerLevel.Length == 0)
            {
                return _magnitude;
            }

            return _magnitudePerLevel[Mathf.Clamp(currentLevel, 0, _magnitudePerLevel.Length - 1)];
        }
    }
}
