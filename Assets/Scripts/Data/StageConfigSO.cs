using System;
using UnityEngine;

namespace SurveHive.Data
{
    public enum StageEventType
    {
        // Burst spawn in a surrounding ring formation.
        StrongWaveRing = 0,
        // Burst spawn flooding in from one direction.
        StrongWaveFlood = 1,
        Miniboss = 2,
        FinalBoss = 3
    }

    [Serializable]
    public struct StageTimelineEvent
    {
        public StageEventType Type;
        // When this fires, as a fraction of the stage duration (0-1).
        [Range(0f, 1f)] public float NormalizedTime;
        // Wave enemy rank or boss stats.
        public EnemyStatsSO EnemyStats;
        // Enemies in the wave burst (ignored for bosses).
        public int Count;
    }

    /// <summary>
    /// Defines one stage/run: total duration, how the base spawn rate escalates
    /// over it (distinct from per-minute stat scaling), and the timeline events
    /// (strong waves, miniboss, final boss) shown on the HUD progress bar.
    /// </summary>
    [CreateAssetMenu(menuName = "SurveHive/Stage Config", fileName = "NewStageConfig")]
    public sealed class StageConfigSO : ScriptableObject
    {
        [SerializeField] private float _totalDurationSeconds = 600f;
        // Multiplier on spawn frequency, sampled at normalized stage time —
        // 1 = the spawner's own pacing, 3 = three times as many spawns.
        [SerializeField] private AnimationCurve _spawnRateMultiplier = AnimationCurve.Linear(0f, 1f, 1f, 3f);
        [SerializeField] private StageTimelineEvent[] _events;

        public float TotalDurationSeconds => _totalDurationSeconds;

        public StageTimelineEvent[] Events => _events;

        public float GetSpawnRateMultiplier(float normalizedTime)
        {
            return Mathf.Max(0.1f, _spawnRateMultiplier.Evaluate(Mathf.Clamp01(normalizedTime)));
        }
    }
}
