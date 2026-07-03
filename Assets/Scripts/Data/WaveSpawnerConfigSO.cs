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

        public float HealthMultiplierAt(float elapsedSeconds)
        {
            return 1f + (elapsedSeconds / 60f) * _healthScalePerMinute;
        }

        public float DamageMultiplierAt(float elapsedSeconds)
        {
            return 1f + (elapsedSeconds / 60f) * _damageScalePerMinute;
        }

        public float InitialSpawnInterval => _initialSpawnInterval;

        public float MinSpawnInterval => _minSpawnInterval;

        public float IntervalRampPerMinute => _intervalRampPerMinute;

        public int MaxConcurrentEnemies => _maxConcurrentEnemies;

        public float SpawnRadiusMin => _spawnRadiusMin;

        public float SpawnRadiusMax => _spawnRadiusMax;
    }
}
