using System;
using SurveHive.Pickups;
using UnityEngine;

namespace SurveHive.Data
{
    /// <summary>
    /// PLAN 5A — everything the codex can list, in display order. Skills and
    /// set bonuses ride along from the existing <see cref="SkillDatabaseSO"/>;
    /// enemies are the authored <see cref="EnemyStatsSO"/> assets; item drops
    /// have no SO of their own, so their display name/blurb/icon are authored
    /// here. Built and wired by the CodexBuilder pass.
    /// </summary>
    [CreateAssetMenu(menuName = "SurveHive/Codex Catalog", fileName = "CodexCatalog")]
    public sealed class CodexCatalogSO : ScriptableObject
    {
        [Serializable]
        public struct ItemEntry
        {
            public ItemDropType Type;
            public string DisplayName;
            [TextArea] public string Description;
            public Sprite Icon;
        }

        // Enemies grouped by the world they belong to (playtest follow-up
        // 2026-07-11) — the codex Enemies tab renders one section per group.
        [Serializable]
        public struct EnemyGroup
        {
            public string WorldName;
            public EnemyStatsSO[] Enemies;
        }

        [SerializeField] private SkillDatabaseSO _skillDatabase;
        [SerializeField] private EnemyGroup[] _enemyGroups;
        [SerializeField] private ItemEntry[] _items;

        public SkillDatabaseSO SkillDatabase => _skillDatabase;

        public EnemyGroup[] EnemyGroups => _enemyGroups;

        public ItemEntry[] Items => _items;
    }
}
