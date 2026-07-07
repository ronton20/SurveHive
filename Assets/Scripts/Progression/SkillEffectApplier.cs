using SurveHive.Combat.Skills;
using SurveHive.Data;
using SurveHive.Health;
using SurveHive.Player;

namespace SurveHive.Progression
{
    public static class SkillEffectApplier
    {
        /// <summary>
        /// Applies one level of <paramref name="skill"/>. <paramref name="currentLevel"/>
        /// is the level owned *before* this application (0 = first take) so skills with
        /// per-level magnitude tables resolve the right step.
        /// </summary>
        public static void Apply(SkillDefinitionSO skill, int currentLevel, PlayerStats stats, HealthComponent health, ActiveSkillManager activeSkills)
        {
            float magnitude = skill.MagnitudeForLevel(currentLevel);
            switch (skill.EffectType)
            {
                case SkillEffectType.MoveSpeedPercent:
                    stats.IncreaseMoveSpeedPercent(magnitude);
                    break;
                case SkillEffectType.MaxHealthFlat:
                    stats.IncreaseMaxHealthFlat(magnitude);
                    health.IncreaseMaxHealth(magnitude);
                    break;
                case SkillEffectType.AttackRangePercent:
                    stats.IncreaseAttackRangePercent(magnitude);
                    break;
                case SkillEffectType.AttackDamagePercent:
                    stats.IncreaseAttackDamagePercent(magnitude);
                    break;
                case SkillEffectType.AttackCooldownPercent:
                    stats.DecreaseAttackCooldownPercent(magnitude);
                    break;
                case SkillEffectType.AttackSpeedPercent:
                    stats.IncreaseAttackSpeedPercent(magnitude);
                    break;
                case SkillEffectType.ProjectileCountFlat:
                    stats.IncreaseProjectileCountFlat((int)magnitude);
                    break;
                case SkillEffectType.MagnetRadiusPercent:
                    stats.IncreaseMagnetRadiusPercent(magnitude);
                    break;
                case SkillEffectType.CritChanceFlat:
                    stats.IncreaseCritChanceFlat(magnitude);
                    break;
                case SkillEffectType.CritDamagePercent:
                    stats.IncreaseCritDamagePercent(magnitude);
                    break;
                case SkillEffectType.LifestealFlat:
                    stats.IncreaseLifestealFlat(magnitude);
                    break;
                case SkillEffectType.ActiveCooldownPercent:
                    stats.DecreaseActiveCooldownPercent(magnitude);
                    break;
                case SkillEffectType.ArmorPercent:
                    stats.IncreaseArmorPercent(magnitude);
                    break;
                case SkillEffectType.AbilityPowerPercent:
                    stats.IncreaseAbilityPowerPercent(magnitude);
                    break;
                case SkillEffectType.BasicAttackPierceFlat:
                    stats.LevelUpPierce();
                    break;
                case SkillEffectType.IgniteChanceFlat:
                    stats.LevelUpBurnStinger(magnitude);
                    break;
                case SkillEffectType.PoisonStingerChance:
                    stats.LevelUpPoisonStinger(magnitude);
                    break;
                case SkillEffectType.FrostStingerChance:
                    stats.LevelUpFrostStinger(magnitude);
                    break;
                case SkillEffectType.ElectricStingerChance:
                    stats.LevelUpShockStinger(magnitude);
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
