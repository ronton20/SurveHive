using System;
using UnityEngine;

namespace SurveHive.Data
{
    [CreateAssetMenu(menuName = "SurveHive/Wave Config", fileName = "NewWaveConfig")]
    public sealed class WaveSpawnerConfigSO : ScriptableObject
    {
        [Serializable]
        public struct WaveEntry
        {
            public EnemyStatsSO enemyStats;
            public float spawnWeight;
            public float unlockTimeSeconds;
            // How many spawn per pick (swarm ranks); 0 on pre-4C assets = 1.
            public int packSize;
        }

        /// <summary>Entries serialized before packSize existed hold 0 — both mean a single spawn.</summary>
        public static int ClampPackSize(int rawPackSize)
        {
            return rawPackSize < 1 ? 1 : rawPackSize;
        }

        [SerializeField] private WaveEntry[] _entries;
        [SerializeField] private float _initialSpawnInterval = 2f;
        [SerializeField] private float _minSpawnInterval = 0.3f;
        [SerializeField] private float _intervalRampPerMinute = 0.15f;
        [SerializeField] private int _maxConcurrentEnemies = 60;
        [SerializeField] private float _spawnRadiusMin = 6f;
        [SerializeField] private float _spawnRadiusMax = 9f;

        [Header("Difficulty Scaling (per minute of run time)")]
        [SerializeField] private float _healthScalePerMinute = 0.18f;
        [SerializeField] private float _damageScalePerMinute = 0.1f;

        public WaveEntry[] Entries => _entries;

        // Stepped per whole minute (not continuous): during minute 0 the
        // multiplier is exactly 1, so early enemies die in the intended hit
        // count (20 HP worker = two 10-damage stings) instead of surviving on
        // a fractional sliver from a few seconds of drift.
        public float HealthMultiplierAt(float elapsedSeconds)
        {
            return 1f + Mathf.Floor(elapsedSeconds / 60f) * _healthScalePerMinute;
        }

        public float DamageMultiplierAt(float elapsedSeconds)
        {
            return 1f + Mathf.Floor(elapsedSeconds / 60f) * _damageScalePerMinute;
        }

        public float InitialSpawnInterval => _initialSpawnInterval;

        public float MinSpawnInterval => _minSpawnInterval;

        public float IntervalRampPerMinute => _intervalRampPerMinute;

        public int MaxConcurrentEnemies => _maxConcurrentEnemies;

        public float SpawnRadiusMin => _spawnRadiusMin;

        public float SpawnRadiusMax => _spawnRadiusMax;
    }
}
