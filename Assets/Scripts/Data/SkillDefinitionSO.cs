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
        [SerializeField] private float _magnitude;
        [SerializeField] private float _weight = 1f;
        // Maximum number of times this skill can be taken. 0 = unlimited.
        [SerializeField] private int _maxLevel;
        [SerializeField] private Sprite _icon;

        public string Id => _id;

        public string DisplayName => _displayName;

        public string Description => _description;

        public SkillEffectType EffectType => _effectType;

        public float Magnitude => _magnitude;

        public float Weight => _weight;

        // 0 means no cap on how many times this skill can be taken.
        public int MaxLevel => _maxLevel;

        public bool HasLevelCap => _maxLevel > 0;

        public Sprite Icon => _icon;
    }
}
