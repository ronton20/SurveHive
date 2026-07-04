using System;
using SurveHive.Data;
using SurveHive.Spawning;
using UnityEngine;

namespace SurveHive.Stage
{
    /// <summary>
    /// Runs the stage timeline: tracks normalized progress over the configured
    /// duration, escalates the spawn rate via the config curve, and fires
    /// timeline events — strong waves spawn immediately in formation (surround
    /// ring / directional flood); miniboss & final boss events are broadcast via
    /// <see cref="OnBossEvent"/> for the boss systems (PLAN 3B) to handle.
    /// </summary>
    public sealed class StageDirector : MonoBehaviour
    {
        private const int MaxSimultaneousEvents = 8;

        public static StageDirector Instance { get; private set; }

        [SerializeField] private StageConfigSO _config;
        [SerializeField] private EnemySpawner _spawner;
        // Flood waves spawn just past the visible half-width (PPU 16 ≈ 20u wide).
        [SerializeField] private float _waveSpawnRadius = 13f;
        [SerializeField] private float _floodSpreadDegrees = 50f;

        private readonly int[] _firedBuffer = new int[MaxSimultaneousEvents];
        private float _elapsedSeconds;
        private float _previousNormalized = -1f;

        /// <summary>Any timeline event fired (waves included) — feeds HUD/banners.</summary>
        public event Action<StageTimelineEvent> OnStageEvent;

        /// <summary>Miniboss / final boss requested — handled by the boss systems.</summary>
        public event Action<StageTimelineEvent> OnBossEvent;

        public StageConfigSO Config => _config;

        public float Progress => _config != null && _config.TotalDurationSeconds > 0f
            ? Mathf.Clamp01(_elapsedSeconds / _config.TotalDurationSeconds)
            : 0f;

        public float CurrentSpawnRateMultiplier => _config != null
            ? _config.GetSpawnRateMultiplier(Progress)
            : 1f;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (_config == null)
            {
                return;
            }

            _elapsedSeconds += Time.deltaTime;
            float current = Progress;

            int fired = StageTimeline.CollectNewlyFired(_config.Events, _previousNormalized, current, _firedBuffer);
            for (int i = 0; i < fired; i++)
            {
                ExecuteEvent(_config.Events[_firedBuffer[i]]);
            }

            _previousNormalized = current;
        }

        private void ExecuteEvent(in StageTimelineEvent stageEvent)
        {
            switch (stageEvent.Type)
            {
                case StageEventType.StrongWaveRing:
                    SpawnRingWave(stageEvent.EnemyStats, stageEvent.Count);
                    break;
                case StageEventType.StrongWaveFlood:
                    SpawnFloodWave(stageEvent.EnemyStats, stageEvent.Count);
                    break;
                case StageEventType.Miniboss:
                case StageEventType.FinalBoss:
                    OnBossEvent?.Invoke(stageEvent);
                    break;
            }

            OnStageEvent?.Invoke(stageEvent);
        }

        // Surround ring: evenly spaced circle closing in from all sides.
        private void SpawnRingWave(EnemyStatsSO stats, int count)
        {
            if (_spawner == null || _spawner.Player == null || count <= 0)
            {
                return;
            }

            Vector3 center = _spawner.Player.position;
            float step = 360f / count;
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = Quaternion.Euler(0f, 0f, step * i) * Vector2.right;
                _spawner.SpawnAt(stats, center + (Vector3)(direction * _waveSpawnRadius));
            }
        }

        // Directional flood: a dense arc pouring in from one random side.
        private void SpawnFloodWave(EnemyStatsSO stats, int count)
        {
            if (_spawner == null || _spawner.Player == null || count <= 0)
            {
                return;
            }

            Vector3 center = _spawner.Player.position;
            float baseAngle = UnityEngine.Random.Range(0f, 360f);
            for (int i = 0; i < count; i++)
            {
                float angle = baseAngle + UnityEngine.Random.Range(-_floodSpreadDegrees, _floodSpreadDegrees);
                float radius = _waveSpawnRadius + UnityEngine.Random.Range(0f, 4f);
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                _spawner.SpawnAt(stats, center + (Vector3)(direction * radius));
            }
        }
    }
}
