using SurveHive.Data;
using SurveHive.Health;
using SurveHive.Player;

namespace SurveHive.Progression
{
    public static class SkillEffectApplier
    {
        public static void Apply(SkillDefinitionSO skill, PlayerStats stats, HealthComponent health)
        {
            switch (skill.EffectType)
            {
                case SkillEffectType.MoveSpeedPercent:
                    stats.IncreaseMoveSpeedPercent(skill.Magnitude);
                    break;
                case SkillEffectType.MaxHealthFlat:
                    stats.IncreaseMaxHealthFlat(skill.Magnitude);
                    health.IncreaseMaxHealth(skill.Magnitude);
                    break;
                case SkillEffectType.AttackRangePercent:
                    stats.IncreaseAttackRangePercent(skill.Magnitude);
                    break;
                case SkillEffectType.AttackDamagePercent:
                    stats.IncreaseAttackDamagePercent(skill.Magnitude);
                    break;
                case SkillEffectType.AttackCooldownPercent:
                    stats.DecreaseAttackCooldownPercent(skill.Magnitude);
                    break;
                case SkillEffectType.AttackSpeedPercent:
                    stats.IncreaseAttackSpeedPercent(skill.Magnitude);
                    break;
                case SkillEffectType.ProjectileCountFlat:
                    stats.IncreaseProjectileCountFlat((int)skill.Magnitude);
                    break;
            }
        }
    }
}
