using System;
using System.Collections.Generic;
using SurveHive.Core;
using SurveHive.Data;
using UnityEngine;

namespace SurveHive.Progression
{
    /// <summary>
    /// PLAN 5D — run-scoped achievement watching. Listens only to signals the
    /// game already emits (kill counter, level-ups, set-tier changes, run end)
    /// plus scaled run time, and checks them against the still-locked slice of
    /// the catalog — a tiny in-place list scan, zero-GC on the combat path.
    /// An unlock fires the toast event and the platform backend immediately,
    /// but rewards + save writes are deferred to <see cref="FlushGrants"/>
    /// (run end / scene teardown, CodexTracker mold) so combat frames stay
    /// free of file IO. A crash mid-run loses only that run's unlocks.
    /// </summary>
    public sealed class AchievementTracker : MonoBehaviour
    {
        [SerializeField] private MetaProgressionStoreSO _store;
        [SerializeField] private AchievementCatalogSO _catalog;
        [SerializeField] private PlayerExperience _playerExperience;

        public static AchievementTracker Instance { get; private set; }

        /// <summary>Fired once per newly-unlocked achievement (toast UI hook).</summary>
        public static event Action<AchievementSO> Unlocked;

        // Catalog entries still locked this run; satisfied ones move to
        // _pendingGrants (swap-remove, no per-frame allocation).
        private readonly List<AchievementSO> _pendingChecks = new List<AchievementSO>();
        private readonly List<AchievementSO> _pendingGrants = new List<AchievementSO>();
        private AchievementRunStats _stats = AchievementRunStats.Empty;
        // Update only needs to re-evaluate while a time-based condition is
        // still locked; event-driven conditions evaluate on their events.
        private int _pendingTimeConditions;
        private bool _subscribed;

        /// <summary>Unlocks awaiting their reward/persist flush (tests).</summary>
        public int PendingGrantCount => _pendingGrants.Count;

        private void Awake()
        {
            Instance = this;

            if (_catalog == null || _store == null)
            {
                return;
            }

            AchievementSO[] achievements = _catalog.Achievements;
            for (int i = 0; i < achievements.Length; i++)
            {
                AchievementSO achievement = achievements[i];
                if (achievement == null || string.IsNullOrEmpty(achievement.AchievementId)
                    || _store.IsAchievementUnlocked(achievement.AchievementId))
                {
                    continue;
                }

                _pendingChecks.Add(achievement);
                if (achievement.ConditionType == AchievementConditionType.SurviveSeconds)
                {
                    _pendingTimeConditions++;
                }
            }
        }

        private void Start()
        {
            // RunSession.Awake has run by now; scene wiring may omit either in
            // menu-less test scenes, so both hooks stay optional.
            if (RunSession.Instance != null)
            {
                RunSession.Instance.OnKillCountChanged += HandleKillCountChanged;
            }

            if (_playerExperience != null)
            {
                _playerExperience.OnLevelUp += HandleLevelUp;
            }

            _subscribed = true;
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
            if (_subscribed)
            {
                if (RunSession.Instance != null)
                {
                    RunSession.Instance.OnKillCountChanged -= HandleKillCountChanged;
                }

                if (_playerExperience != null)
                {
                    _playerExperience.OnLevelUp -= HandleLevelUp;
                }
            }

            FlushGrants();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (_pendingTimeConditions == 0)
            {
                return;
            }

            // Scaled time, mirroring RunSession: pauses/hit-stop don't count.
            _stats.SurvivedSeconds += Time.deltaTime;
            Evaluate();
        }

        /// <summary>
        /// Run-end hook (RunSession.EndRun — death, victory, and abandon all
        /// route through it). A victory resolves ClearStage conditions, then
        /// rewards/persist flush while the run is already over.
        /// </summary>
        public static void ReportRunEnd(bool victory, int difficulty)
        {
            if (Instance == null)
            {
                return;
            }

            if (victory && difficulty > Instance._stats.ClearedDifficulty)
            {
                Instance._stats.ClearedDifficulty = difficulty;
            }

            Instance.Evaluate();
            Instance.FlushGrants();
        }

        /// <summary>
        /// Grants rewards + persists every queued unlock (one pass; TryGrant is
        /// idempotent). Called at run end and again at teardown so quit-to-menu
        /// without EndRun still lands; also the test entry point.
        /// </summary>
        public void FlushGrants()
        {
            for (int i = 0; i < _pendingGrants.Count; i++)
            {
                AchievementRules.TryGrant(_store, _pendingGrants[i]);
            }

            _pendingGrants.Clear();
        }

        private void HandleKillCountChanged(int killCount)
        {
            _stats.Kills = killCount;
            Evaluate();
        }

        private void HandleLevelUp(int level)
        {
            if (level > _stats.Level)
            {
                _stats.Level = level;
            }

            Evaluate();
        }

        private void HandleElementSetsChanged()
        {
            int maxTier = 0;
            for (int i = 0; i < ElementSets.ElementCount; i++)
            {
                int tier = ElementSets.GetTierIndex((SkillElement)i) + 1;
                if (tier > maxTier)
                {
                    maxTier = tier;
                }
            }

            if (maxTier > _stats.MaxSetTier)
            {
                _stats.MaxSetTier = maxTier;
            }

            Evaluate();
        }

        private void Evaluate()
        {
            for (int i = _pendingChecks.Count - 1; i >= 0; i--)
            {
                AchievementSO achievement = _pendingChecks[i];
                if (!AchievementRules.IsSatisfied(achievement, in _stats))
                {
                    continue;
                }

                _pendingChecks.RemoveAt(i);
                if (achievement.ConditionType == AchievementConditionType.SurviveSeconds)
                {
                    _pendingTimeConditions--;
                }

                _pendingGrants.Add(achievement);
                AchievementBackends.Active.ReportUnlock(achievement.AchievementId);
                Unlocked?.Invoke(achievement);
            }
        }
    }
}
