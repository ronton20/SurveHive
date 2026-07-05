using System.Text;
using SurveHive.Combat.Status;
using SurveHive.Data;
using SurveHive.Player;
using UnityEngine;

namespace SurveHive.Progression
{
    /// <summary>
    /// Builds the concrete "what changes" lines for a skill-upgrade card, e.g.
    /// "Basic Attack DMG 10 → 11" or "Projectiles 6 → 7". Passive previews
    /// mirror PlayerStats' exact per-application rounding so the shown number
    /// is the number the player gets; active previews read the growth table.
    /// Runs only while the level-up screen is open (paused) — allocation cost
    /// is irrelevant there.
    /// </summary>
    public static class SkillStatPreview
    {
        private const string Arrow = "  →  ";

        /// <summary>
        /// Appends one line per stat that changes when taking this card.
        /// <paramref name="applications"/> is 1, or 2 for a lucky pick.
        /// </summary>
        public static void AppendUpgradeLines(
            StringBuilder sb, SkillDefinitionSO skill, int currentLevel, int applications,
            PlayerStats stats, int activeSkillLevelCap)
        {
            switch (skill.EffectType)
            {
                case SkillEffectType.MoveSpeedPercent:
                    AppendLine(sb, "Move Speed", stats.MoveSpeed,
                        CompoundUp(stats.MoveSpeed, skill.Magnitude, applications));
                    break;
                case SkillEffectType.MaxHealthFlat:
                    AppendLine(sb, "Max HP", stats.MaxHealth,
                        stats.MaxHealth + (skill.Magnitude * applications));
                    break;
                case SkillEffectType.AttackRangePercent:
                    AppendLine(sb, "Attack Range", stats.AttackRange,
                        CompoundUp(stats.AttackRange, skill.Magnitude, applications));
                    break;
                case SkillEffectType.AttackDamagePercent:
                    AppendLine(sb, "Basic Attack DMG", Mathf.Round(stats.AttackDamage),
                        Mathf.Round(CompoundUp(stats.AttackDamage, skill.Magnitude, applications)));
                    break;
                case SkillEffectType.AttackCooldownPercent:
                    AppendLine(sb, "Attack Cooldown", stats.AttackCooldown,
                        CompoundDown(stats.AttackCooldown, skill.Magnitude, applications, 0f));
                    break;
                case SkillEffectType.AttackSpeedPercent:
                    AppendLine(sb, "Attack Speed", stats.AttackSpeed,
                        CompoundUp(stats.AttackSpeed, skill.Magnitude, applications));
                    break;
                case SkillEffectType.ProjectileCountFlat:
                    AppendLine(sb, "Projectiles", stats.ProjectileCount,
                        Mathf.Min(stats.ProjectileCount + ((int)skill.Magnitude * applications), stats.MaxProjectileCount));
                    break;
                case SkillEffectType.MagnetRadiusPercent:
                    AppendPercentLine(sb, "Pickup Range", stats.MagnetRadiusMultiplier * 100f,
                        CompoundUp(stats.MagnetRadiusMultiplier, skill.Magnitude, applications) * 100f);
                    break;
                case SkillEffectType.CritChanceFlat:
                    AppendPercentLine(sb, "Crit Chance", stats.CritChancePercent,
                        Mathf.Min(100f, stats.CritChancePercent + (skill.Magnitude * applications)));
                    break;
                case SkillEffectType.CritDamagePercent:
                    AppendPercentLine(sb, "Crit DMG", stats.CritDamageMultiplier * 100f,
                        (stats.CritDamageMultiplier + (skill.Magnitude / 100f * applications)) * 100f);
                    break;
                case SkillEffectType.LifestealFlat:
                    AppendPercentLine(sb, "Lifesteal", stats.LifestealPercent,
                        Mathf.Min(100f, stats.LifestealPercent + (skill.Magnitude * applications)));
                    break;
                case SkillEffectType.ActiveCooldownPercent:
                    AppendPercentLine(sb, "Skill Cooldowns", stats.ActiveCooldownMultiplier * 100f,
                        Mathf.Max(stats.MinActiveCooldownMultiplier,
                            CompoundDown(stats.ActiveCooldownMultiplier, skill.Magnitude, applications,
                                stats.MinActiveCooldownMultiplier)) * 100f);
                    break;
                case SkillEffectType.ArmorPercent:
                    AppendPercentLine(sb, "Armor", stats.ArmorPercent,
                        Mathf.Min(stats.MaxArmorPercent, stats.ArmorPercent + (skill.Magnitude * applications)));
                    break;
                case SkillEffectType.AbilityPowerPercent:
                    AppendPercentLine(sb, "Ability Power", stats.AbilityPowerMultiplier * 100f,
                        CompoundUp(stats.AbilityPowerMultiplier, skill.Magnitude, applications) * 100f);
                    break;
                case SkillEffectType.ActiveSkill:
                    AppendActiveSkillLines(sb, skill.ActiveSkill, currentLevel, applications, activeSkillLevelCap);
                    break;
            }
        }

        private static void AppendActiveSkillLines(
            StringBuilder sb, ActiveSkillSO active, int currentLevel, int applications, int levelCap)
        {
            if (active == null || currentLevel < 1)
            {
                return;
            }

            int targetLevel = Mathf.Min(currentLevel + applications, levelCap);
            ActiveSkillLevelStats before = active.GetLevelStats(currentLevel);
            ActiveSkillLevelStats after = active.GetLevelStats(targetLevel);

            if (!Mathf.Approximately(before.Damage, after.Damage))
            {
                string damageLabel = active.Behavior == ActiveSkillBehavior.Aura ||
                    active.Behavior == ActiveSkillBehavior.LobbedPuddle ? "DMG/Tick" : "DMG";
                AppendLine(sb, damageLabel, before.Damage, after.Damage);
            }

            if (before.Count != after.Count)
            {
                AppendLine(sb, active.Behavior == ActiveSkillBehavior.ChainArc ? "Chain Targets" : "Projectiles",
                    before.Count, after.Count);
            }

            if (!Mathf.Approximately(before.Area, after.Area))
            {
                AppendLine(sb, GetAreaLabel(active.Behavior), before.Area, after.Area);
            }

            if (!Mathf.Approximately(before.Cooldown, after.Cooldown))
            {
                sb.Append("Cooldown ");
                sb.Append(Round1(before.Cooldown));
                sb.Append('s');
                sb.Append(Arrow);
                sb.Append(Round1(after.Cooldown));
                sb.Append("s\n");
            }

            if (!Mathf.Approximately(before.StatusChancePercent, after.StatusChancePercent))
            {
                AppendPercentLine(sb, GetStatusLabel(active.StatusType), before.StatusChancePercent, after.StatusChancePercent);
            }
        }

        private static string GetAreaLabel(ActiveSkillBehavior behavior)
        {
            switch (behavior)
            {
                case ActiveSkillBehavior.LobbedPuddle:
                    return "Puddle Radius";
                case ActiveSkillBehavior.Aura:
                    return "Cloud Radius";
                case ActiveSkillBehavior.ChainArc:
                    return "Jump Range";
                case ActiveSkillBehavior.HomingBolt:
                    return "Blast Radius";
                default:
                    return "Area";
            }
        }

        private static string GetStatusLabel(StatusEffectType type)
        {
            switch (type)
            {
                case StatusEffectType.Burn:
                    return "Burn Chance";
                case StatusEffectType.Poison:
                    return "Poison Chance";
                case StatusEffectType.Slow:
                    return "Slow Chance";
                case StatusEffectType.Freeze:
                    return "Freeze Chance";
                default:
                    return "Stun Chance";
            }
        }

        // Mirrors PlayerStats: each application multiplies then rounds to 2dp.
        private static float CompoundUp(float value, float percent, int applications)
        {
            for (int i = 0; i < applications; i++)
            {
                value = Mathf.Round(value * (1f + percent / 100f) * 100f) / 100f;
            }

            return value;
        }

        private static float CompoundDown(float value, float percent, int applications, float floor)
        {
            for (int i = 0; i < applications; i++)
            {
                value = Mathf.Max(floor, Mathf.Round(value * (1f - percent / 100f) * 100f) / 100f);
            }

            return value;
        }

        private static void AppendLine(StringBuilder sb, string label, float before, float after)
        {
            sb.Append(label);
            sb.Append(' ');
            sb.Append(Round1(before));
            sb.Append(Arrow);
            sb.Append(Round1(after));
            sb.Append('\n');
        }

        private static void AppendPercentLine(StringBuilder sb, string label, float before, float after)
        {
            sb.Append(label);
            sb.Append(' ');
            sb.Append(Round1(before));
            sb.Append('%');
            sb.Append(Arrow);
            sb.Append(Round1(after));
            sb.Append("%\n");
        }

        private static float Round1(float value)
        {
            return Mathf.Round(value * 10f) / 10f;
        }
    }
}
