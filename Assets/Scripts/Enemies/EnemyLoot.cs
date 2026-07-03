using SurveHive.Core;
using SurveHive.Currency;
using SurveHive.Data;
using SurveHive.Health;
using SurveHive.Pickups;
using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Enemies
{
    [RequireComponent(typeof(HealthComponent), typeof(EnemyController))]
    public sealed class EnemyLoot : MonoBehaviour
    {
        private HealthComponent _health;
        private EnemyController _enemyController;

        private PlayerExperience _playerExperience;
        private RunCurrencyWallet _currencyWallet;
        private Transform _playerTransform;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            _enemyController = GetComponent<EnemyController>();
        }

        private void OnEnable()
        {
            _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            _health.OnDied -= HandleDied;
        }

        public void Initialize(PlayerExperience playerExperience, RunCurrencyWallet currencyWallet, Transform playerTransform)
        {
            _playerExperience = playerExperience;
            _currencyWallet = currencyWallet;
            _playerTransform = playerTransform;
        }

        private void HandleDied()
        {
            if (PoolManager.Instance == null)
            {
                return;
            }

            EnemyStatsSO stats = _enemyController.Stats;
            Vector3 position = transform.position;

            GameObject expPickup = PoolManager.Instance.Get(PoolIds.ExpPickup, position, Quaternion.identity);
            if (expPickup.TryGetComponent(out PickupItem expItem))
            {
                expItem.Initialize(PickupType.Exp, stats.ExpReward, _playerExperience, _currencyWallet, _playerTransform);
            }

            if (Random.value <= stats.CurrencyDropChance)
            {
                int amount = Random.Range(stats.CurrencyDropMin, stats.CurrencyDropMax + 1);
                GameObject currencyPickup = PoolManager.Instance.Get(PoolIds.CurrencyPickup, position, Quaternion.identity);
                if (currencyPickup.TryGetComponent(out PickupItem currencyItem))
                {
                    currencyItem.Initialize(PickupType.Currency, amount, _playerExperience, _currencyWallet, _playerTransform);
                }
            }

            PoolManager.Instance.Release(stats.PoolId, gameObject);
        }
    }
}
