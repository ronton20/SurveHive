using SurveHive.Combat.Skills;
using SurveHive.Data;
using SurveHive.Health;
using SurveHive.Player;

namespace SurveHive.Progression
{
    public static class SkillEffectApplier
    {
        public static void Apply(SkillDefinitionSO skill, PlayerStats stats, HealthComponent health, ActiveSkillManager activeSkills)
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
                case SkillEffectType.MagnetRadiusPercent:
                    stats.IncreaseMagnetRadiusPercent(skill.Magnitude);
                    break;
                case SkillEffectType.CritChanceFlat:
                    stats.IncreaseCritChanceFlat(skill.Magnitude);
                    break;
                case SkillEffectType.CritDamagePercent:
                    stats.IncreaseCritDamagePercent(skill.Magnitude);
                    break;
                case SkillEffectType.LifestealFlat:
                    stats.IncreaseLifestealFlat(skill.Magnitude);
                    break;
                case SkillEffectType.ActiveCooldownPercent:
                    stats.DecreaseActiveCooldownPercent(skill.Magnitude);
                    break;
                case SkillEffectType.ArmorPercent:
                    stats.IncreaseArmorPercent(skill.Magnitude);
                    break;
                case SkillEffectType.AbilityPowerPercent:
                    stats.IncreaseAbilityPowerPercent(skill.Magnitude);
                    break;
                case SkillEffectType.BasicAttackPierceFlat:
                    stats.LevelUpPierce();
                    break;
                case SkillEffectType.IgniteChanceFlat:
                    stats.LevelUpBurnStinger(skill.Magnitude);
                    break;
                case SkillEffectType.PoisonStingerChance:
                    stats.LevelUpPoisonStinger(skill.Magnitude);
                    break;
                case SkillEffectType.FrostStingerChance:
                    stats.LevelUpFrostStinger(skill.Magnitude);
                    break;
                case SkillEffectType.ElectricStingerChance:
                    stats.LevelUpShockStinger(skill.Magnitude);
                    break;
                case SkillEffectType.ActiveSkill:
                    if (activeSkills != null)
                    {
                        activeSkills.AddOrLevelUp(skill.ActiveSkill);
                    }

                    break;
            }
        }
    }
}
