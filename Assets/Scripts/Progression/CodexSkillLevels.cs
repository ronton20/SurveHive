using System.Text;
using SurveHive.Combat;
using SurveHive.Data;

namespace SurveHive.Progression
{
    /// <summary>
    /// Builds the codex's per-level breakdown for a power-up — one line per
    /// level saying what taking that level grants. Unlike
    /// <see cref="SkillStatPreview"/> (which shows live before→after numbers on
    /// the level-up screen), this is menu-safe: it reads only the skill assets,
    /// never <c>PlayerStats</c>. Menu-only path — allocation cost is fine.
    /// </summary>
    public static class CodexSkillLevels
    {
        private const string LevelPrefix = "Lv ";
        private const string Separator = " — ";
        private const string Dot = " · ";

        /// <summary>
        /// Appends one "Lv n — ..." line per level of <paramref name="skill"/>.
        /// Uncapped skills get a single "Per level" line instead.
        /// </summary>
        public static void AppendLevels(StringBuilder sb, SkillDefinitionSO skill)
        {
            if (skill == null)
            {
                return;
            }

            if (skill.EffectType == SkillEffectType.ActiveSkill)
            {
                AppendActiveLevels(sb, skill.ActiveSkill);
                return;
            }

            if (!skill.HasLevelCap)
            {
                sb.Append("Per level");
                sb.Append(Separator);
                AppendEffect(sb, skill, 0);
                sb.Append('\n');
                return;
            }

            for (int level = 1; level <= skill.MaxLevel; level++)
            {
                sb.Append(LevelPrefix);
                sb.Append(level);
                sb.Append(Separator);
                AppendEffect(sb, skill, level);
                sb.Append('\n');
            }
        }

        private static void AppendActiveLevels(StringBuilder sb, ActiveSkillSO active)
        {
            if (active == null)
            {
                return;
            }

            for (int level = 1; level <= active.MaxLevel; level++)
            {
                ActiveSkillLevelStats stats = active.GetLevelStats(level);

                sb.Append(LevelPrefix);
                sb.Append(level);
                sb.Append(Separator);

                sb.Append(IsTicking(active.Behavior) ? "DMG/Tick " : "DMG ");
                sb.Append(Round1(stats.Damage));

                if (stats.Count > 1)
                {
                    sb.Append(Dot);
                    sb.Append('x');
                    sb.Append(stats.Count);
                }

                if (stats.Area > 0f)
                {
                    sb.Append(Dot);
                    sb.Append("Area ");
                    sb.Append(Round1(stats.Area));
                }

                sb.Append(Dot);
                sb.Append("CD ");
                sb.Append(Round1(stats.Cooldown));
                sb.Append('s');

                if (active.AppliesStatus && stats.StatusChancePercent > 0f)
                {
                    sb.Append(Dot);
                    sb.Append(StatusLabel(active.StatusType));
                    sb.Append(' ');
                    sb.Append(Round1(stats.StatusChancePercent));
                    sb.Append('%');
                }

                sb.Append('\n');
            }
        }

        // The per-level grant for stat-style skills. Level is 1-based;
        // MagnitudeForLevel takes the pre-take level (0 = first take).
        private static void AppendEffect(StringBuilder sb, SkillDefinitionSO skill, int level)
        {
            if (skill.EffectType == SkillEffectType.BasicAttackPierceFlat)
            {
                sb.Append("Pierce ");
                int count = CombatMath.PierceCount(level, skill.MaxLevel);
                if (count == int.MaxValue)
                {
                    sb.Append("ALL");
                }
                else
                {
                    sb.Append(count);
                }

                return;
            }

            float magnitude = skill.MagnitudeForLevel(level > 0 ? level - 1 : 0);
            bool reduces = skill.EffectType == SkillEffectType.AttackCooldownPercent
                || skill.EffectType == SkillEffectType.ActiveCooldownPercent;

            sb.Append(reduces ? '-' : '+');
            sb.Append(Round1(magnitude));
            if (IsPercent(skill.EffectType))
            {
                sb.Append('%');
            }

            sb.Append(' ');
            sb.Append(EffectLabel(skill.EffectType));
        }

        private static bool IsTicking(ActiveSkillBehavior behavior)
        {
            return behavior == ActiveSkillBehavior.Aura || behavior == ActiveSkillBehavior.LobbedPuddle;
        }

        private static bool IsPercent(SkillEffectType type)
        {
            return type != SkillEffectType.MaxHealthFlat
                && type != SkillEffectType.ProjectileCountFlat;
        }

        private static string EffectLabel(SkillEffectType type)
        {
            switch (type)
            {
                case SkillEffectType.MoveSpeedPercent: return "Move Speed";
                case SkillEffectType.MaxHealthFlat: return "Max HP";
                case SkillEffectType.AttackRangePercent: return "Attack Range";
                case SkillEffectType.AttackDamagePercent: return "Basic Attack DMG";
                case SkillEffectType.AttackCooldownPercent: return "Attack Cooldown";
                case SkillEffectType.ProjectileCountFlat: return "Projectiles";
                case SkillEffectType.AttackSpeedPercent: return "Attack Speed";
                case SkillEffectType.MagnetRadiusPercent: return "Pickup Range";
                case SkillEffectType.CritChanceFlat: return "Crit Chance";
                case SkillEffectType.CritDamagePercent: return "Crit DMG";
                case SkillEffectType.LifestealFlat: return "Lifesteal";
                case SkillEffectType.ActiveCooldownPercent: return "Skill Cooldowns";
                case SkillEffectType.ArmorPercent: return "Armor";
                case SkillEffectType.AbilityPowerPercent: return "Ability Power";
                case SkillEffectType.IgniteChanceFlat: return "Burn Chance";
                case SkillEffectType.PoisonStingerChance: return "Poison Chance";
                case SkillEffectType.FrostStingerChance: return "Freeze Chance";
                case SkillEffectType.ElectricStingerChance: return "Bounce Chance";
                default: return "Effect";
            }
        }

        private static string StatusLabel(Combat.Status.StatusEffectType type)
        {
            switch (type)
            {
                case Combat.Status.StatusEffectType.Burn: return "Burn";
                case Combat.Status.StatusEffectType.Poison: return "Poison";
                case Combat.Status.StatusEffectType.Slow: return "Slow";
                case Combat.Status.StatusEffectType.Freeze: return "Freeze";
                default: return "Stun";
            }
        }

        private static float Round1(float value)
        {
            return UnityEngine.Mathf.Round(value * 10f) / 10f;
        }
    }
}
