using SurveHive.Core;
using SurveHive.Currency;
using SurveHive.Data;
using SurveHive.Enemies;
using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Spawning
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private WaveSpawnerConfigSO _config;
        [SerializeField] private Transform _player;
        [SerializeField] private PlayerExperience _playerExperience;
        [SerializeField] private RunCurrencyWallet _currencyWallet;
        [SerializeField] private DifficultySO _difficulty;

        private float _elapsedSeconds;
        private float _spawnTimer;
        // Difficulty-tier multipliers (PLAN 1B), resolved once at run start on
        // top of the config's per-minute ramp. Identity when unwired.
        private float _difficultyHealthMultiplier = 1f;
        private float _difficultyDamageMultiplier = 1f;
        private float _difficultySpawnRateMultiplier = 1f;

        private void Awake()
        {
            if (_difficulty != null)
            {
                DifficultySO.TierSettings tier = _difficulty.GetSettings(RunSession.SelectedDifficulty);
                _difficultyHealthMultiplier = tier.enemyHealthMultiplier;
                _difficultyDamageMultiplier = tier.enemyDamageMultiplier;
                _difficultySpawnRateMultiplier = Mathf.Max(0.1f, tier.spawnRateMultiplier);
            }
        }

        private void Update()
        {
            // Freeze the regular drip while a boss owns the arena (PLAN adj.
            // 2026-07-05): the boss fight is the focus, and its own summons still
            // spawn directly. The timer freezes with it and resumes on boss death.
            if (SurveHive.Stage.StageDirector.Instance != null
                && SurveHive.Stage.StageDirector.Instance.IsBossActive)
            {
                return;
            }

            _elapsedSeconds += Time.deltaTime;
            _spawnTimer -= Time.deltaTime;

            if (_spawnTimer > 0f)
            {
                return;
            }

            _spawnTimer = GetCurrentSpawnInterval();

            if (EnemyRegistry.Instance != null && EnemyRegistry.Instance.ActiveCount >= _config.MaxConcurrentEnemies)
            {
                return;
            }

            SpawnOne();
        }

        private float GetCurrentSpawnInterval()
        {
            float elapsedMinutes = _elapsedSeconds / 60f;
            float interval = _config.InitialSpawnInterval - elapsedMinutes * _config.IntervalRampPerMinute;
            interval = Mathf.Max(_config.MinSpawnInterval, interval);

            // Stage-driven escalation (PLAN §5.1): the stage director's curve
            // multiplies spawn frequency on top of the config's own ramp.
            if (SurveHive.Stage.StageDirector.Instance != null)
            {
                interval /= SurveHive.Stage.StageDirector.Instance.CurrentSpawnRateMultiplier;
            }

            return interval / _difficultySpawnRateMultiplier;
        }

        private void SpawnOne()
        {
            WaveSpawnerConfigSO.WaveEntry entry = PickWeightedEnemy();
            if (entry.enemyStats == null)
            {
                return;
            }

            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float radius = Random.Range(_config.SpawnRadiusMin, _config.SpawnRadiusMax);
            Vector3 spawnPosition = _player.position + (Vector3)(randomDirection * radius);

            // Swarm ranks (PLAN 4C) arrive as a cluster around the pick point.
            int packSize = WaveSpawnerConfigSO.ClampPackSize(entry.packSize);
            SpawnAt(entry.enemyStats, spawnPosition);
            for (int i = 1; i < packSize; i++)
            {
                Vector3 offset = Random.insideUnitCircle * 1.2f;
                SpawnAt(entry.enemyStats, spawnPosition + offset);
            }
        }

        /// <summary>
        /// Spawns and fully initializes one enemy at a world position, applying
        /// the current time-based stat scaling. Used by the regular drip and by
        /// the stage director's strong-wave/boss bursts (which bypass the
        /// concurrent-enemy cap by design).
        /// </summary>
        public GameObject SpawnAt(EnemyStatsSO stats, Vector3 position)
        {
            if (stats == null || PoolManager.Instance == null)
            {
                return null;
            }

            GameObject instance = PoolManager.Instance.Get(stats.PoolId, position, Quaternion.identity);

            if (instance.TryGetComponent(out EnemyController controller))
            {
                float healthMultiplier = _config.HealthMultiplierAt(_elapsedSeconds) * _difficultyHealthMultiplier;
                float damageMultiplier = _config.DamageMultiplierAt(_elapsedSeconds) * _difficultyDamageMultiplier;
                controller.Initialize(stats, _player, healthMultiplier, damageMultiplier);
            }

            if (instance.TryGetComponent(out EnemyLoot loot))
            {
                loot.Initialize(_playerExperience, _currencyWallet, _player);
            }

            return instance;
        }

        public Transform Player => _player;

        private WaveSpawnerConfigSO.WaveEntry PickWeightedEnemy()
        {
            WaveSpawnerConfigSO.WaveEntry[] entries = _config.Entries;
            float totalWeight = 0f;

            for (int i = 0; i < entries.Length; i++)
            {
                if (_elapsedSeconds >= entries[i].unlockTimeSeconds)
                {
                    totalWeight += entries[i].spawnWeight;
                }
            }

            if (totalWeight <= 0f)
            {
                return default;
            }

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < entries.Length; i++)
            {
                if (_elapsedSeconds < entries[i].unlockTimeSeconds)
                {
                    continue;
                }

                cumulative += entries[i].spawnWeight;
                if (roll <= cumulative)
                {
                    return entries[i];
                }
            }

            return entries[entries.Length - 1];
        }
    }
}
