using UnityEngine;

namespace SurveHive.Data
{
    [CreateAssetMenu(menuName = "SurveHive/Skill Database", fileName = "SkillDatabase")]
    public sealed class SkillDatabaseSO : ScriptableObject
    {
        [SerializeField] private SkillDefinitionSO[] _skills;

        public SkillDefinitionSO[] Skills => _skills;
    }
}
