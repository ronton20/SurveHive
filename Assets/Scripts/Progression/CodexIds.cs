using SurveHive.Data;
using SurveHive.Pickups;

namespace SurveHive.Progression
{
    /// <summary>
    /// PLAN 5A — the codex entry id scheme. One save-stable string per
    /// discoverable thing, namespaced by category so the four sources can never
    /// collide: <c>skill:&lt;SkillDefinitionSO.Id&gt;</c>, <c>set:&lt;element&gt;</c>,
    /// <c>enemy:&lt;EnemyStatsSO asset name&gt;</c>, <c>item:&lt;ItemDropType&gt;</c>.
    /// Pure + EditMode-tested. Ids are built only on first encounter per run
    /// (never per frame), so the string concatenation here stays off hot paths.
    /// </summary>
    public static class CodexIds
    {
        // Indexed by (int)SkillElement / (int)ItemDropType so runtime lookups
        // never call enum ToString (which allocates via reflection).
        private static readonly string[] SetIds =
        {
            "set:Physical", "set:Fire", "set:Poison", "set:Electric", "set:Frost", "set:Honey",
        };

        private static readonly string[] ItemIds =
        {
            "item:HoneyJar", "item:Magnet", "item:WaxShield", "item:RoyalBomb",
        };

        public static string ForSkill(SkillDefinitionSO skill)
        {
            return skill != null && !string.IsNullOrEmpty(skill.Id) ? "skill:" + skill.Id : null;
        }

        public static string ForSet(SkillElement element)
        {
            int index = (int)element;
            return index >= 0 && index < SetIds.Length ? SetIds[index] : null;
        }

        public static string ForEnemy(EnemyStatsSO stats)
        {
            return stats != null ? "enemy:" + stats.name : null;
        }

        public static string ForItem(ItemDropType type)
        {
            int index = (int)type;
            return index >= 0 && index < ItemIds.Length ? ItemIds[index] : null;
        }
    }
}
