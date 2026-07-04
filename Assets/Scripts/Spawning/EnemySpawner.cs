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

        private float _elapsedSeconds;
        private float _spawnTimer;

        private void Update()
        {
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

            return interval;
        }

        private void SpawnOne()
        {
            EnemyStatsSO stats = PickWeightedEnemy();
            if (stats == null)
            {
                return;
            }

            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float radius = Random.Range(_config.SpawnRadiusMin, _config.SpawnRadiusMax);
            Vector3 spawnPosition = _player.position + (Vector3)(randomDirection * radius);
            SpawnAt(stats, spawnPosition);
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
                float healthMultiplier = _config.HealthMultiplierAt(_elapsedSeconds);
                float damageMultiplier = _config.DamageMultiplierAt(_elapsedSeconds);
                controller.Initialize(stats, _player, healthMultiplier, damageMultiplier);
            }

            if (instance.TryGetComponent(out EnemyLoot loot))
            {
                loot.Initialize(_playerExperience, _currencyWallet, _player);
            }

            return instance;
        }

        public Transform Player => _player;

        private EnemyStatsSO PickWeightedEnemy()
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
                return null;
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
                    return entries[i].enemyStats;
                }
            }

            return entries[entries.Length - 1].enemyStats;
        }
    }
}
