using UnityEngine;

namespace SurveHive.Data
{
    [CreateAssetMenu(menuName = "SurveHive/Skill Database", fileName = "SkillDatabase")]
    public sealed class SkillDatabaseSO : ScriptableObject
    {
        [SerializeField] private SkillDefinitionSO[] _skills;
        // Elemental set bonus configs (Phase 3C), one per SkillElement.
        [SerializeField] private SetBonusSO[] _setBonuses;

        public SkillDefinitionSO[] Skills => _skills;

        public SetBonusSO[] SetBonuses => _setBonuses;
    }
}
