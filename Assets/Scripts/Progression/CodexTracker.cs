using System.Collections.Generic;
using SurveHive.Data;
using SurveHive.Pickups;
using UnityEngine;

namespace SurveHive.Progression
{
    /// <summary>
    /// PLAN 5A — run-scoped codex discovery. Gameplay systems report each
    /// skill pick / enemy spawn / item pickup through the static entry points
    /// (PlayerContext/HitStop mold — null-safe when no tracker is in the scene);
    /// set-bonus tiers are caught by listening to <see cref="ElementSets"/>.
    /// First-encounter checks are reference/bool lookups (zero-GC on the spawn
    /// hot path); the entry id string is built once per newly-seen thing and
    /// queued, then the whole batch persists in one save write at scene
    /// teardown — never mid-combat, so combat frames stay free of file IO.
    /// A crash mid-run loses only that run's discoveries.
    /// </summary>
    public sealed class CodexTracker : MonoBehaviour
    {
        [SerializeField] private MetaProgressionStoreSO _store;

        public static CodexTracker Instance { get; private set; }

        private readonly HashSet<SkillDefinitionSO> _seenSkills = new HashSet<SkillDefinitionSO>();
        private readonly HashSet<EnemyStatsSO> _seenEnemies = new HashSet<EnemyStatsSO>();
        private readonly bool[] _seenItems = new bool[ItemDrops.TypeCount];
        private readonly bool[] _seenSets = new bool[ElementSets.ElementCount];
        private readonly List<string> _pendingUnlocks = new List<string>();

        /// <summary>Newly-discovered entries queued for the teardown flush (tests).</summary>
        public int PendingUnlockCount => _pendingUnlocks.Count;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            ElementSets.OnChanged += HandleElementSetsChanged;
        }

        private void OnDisable()
        {
            ElementSets.OnChanged -= HandleElementSetsChanged;
        }

        private void OnDestroy()
        {
            FlushPending();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static void ReportSkill(SkillDefinitionSO skill)
        {
            if (Instance == null || skill == null || !Instance._seenSkills.Add(skill))
            {
                return;
            }

            Instance.QueueUnlock(CodexIds.ForSkill(skill));
        }

        public static void ReportEnemy(EnemyStatsSO stats)
        {
            if (Instance == null || stats == null || !Instance._seenEnemies.Add(stats))
            {
                return;
            }

            Instance.QueueUnlock(CodexIds.ForEnemy(stats));
        }

        public static void ReportItem(ItemDropType type)
        {
            int index = (int)type;
            if (Instance == null || index < 0 || index >= ItemDrops.TypeCount
                || Instance._seenItems[index])
            {
                return;
            }

            Instance._seenItems[index] = true;
            Instance.QueueUnlock(CodexIds.ForItem(type));
        }

        /// <summary>
        /// Persists everything discovered so far (one save write). Called from
        /// OnDestroy so death, victory, and quit-to-menu all flush; also the
        /// PlayMode-test entry point.
        /// </summary>
        public void FlushPending()
        {
            if (_pendingUnlocks.Count == 0 || _store == null)
            {
                return;
            }

            _store.UnlockCodexEntries(_pendingUnlocks);
            _pendingUnlocks.Clear();
        }

        // A set tier flipping active counts as "meeting" that set effect.
        private void HandleElementSetsChanged()
        {
            for (int i = 0; i < ElementSets.ElementCount; i++)
            {
                var element = (SkillElement)i;
                if (_seenSets[i] || ElementSets.GetTierIndex(element) < 0)
                {
                    continue;
                }

                _seenSets[i] = true;
                QueueUnlock(CodexIds.ForSet(element));
            }
        }

        private void QueueUnlock(string entryId)
        {
            if (string.IsNullOrEmpty(entryId))
            {
                return;
            }

            if (_store != null && _store.IsCodexUnlocked(entryId))
            {
                return;
            }

            _pendingUnlocks.Add(entryId);
        }
    }
}
