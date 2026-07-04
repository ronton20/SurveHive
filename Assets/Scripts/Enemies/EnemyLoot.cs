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

            if (RunSession.Instance != null)
            {
                RunSession.Instance.RegisterKill();
            }

            if (stats.DeathHitStopSeconds > 0f && HitStop.Instance != null)
            {
                HitStop.Instance.Request(stats.DeathHitStopSeconds);
            }

            if (PoolManager.Instance.HasPool(PoolIds.DeathVfx))
            {
                PoolManager.Instance.Get(PoolIds.DeathVfx, position, Quaternion.identity);
            }

            // EXP scales with rank (dominant) + effective max HP, and nearby
            // orbs merge instead of piling up.
            float expValue = ExpRewardCalculator.Calculate(stats.Rank, _health.MaxHealth);
            if (!PickupItem.TryMergeExp(position, expValue))
            {
                GameObject expPickup = PoolManager.Instance.Get(PoolIds.ExpPickup, position, Quaternion.identity);
                if (expPickup.TryGetComponent(out PickupItem expItem))
                {
                    expItem.Initialize(PickupType.Exp, expValue, _playerExperience, _currencyWallet, _playerTransform);
                }
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

            // World item drops (elites/bosses; PLAN §5.3). Uniform type roll,
            // offset slightly so the item doesn't hide under the EXP mote.
            if (Random.value <= stats.ItemDropChance)
            {
                ItemDropType dropType = ItemDrops.RollType(Random.value);
                int dropPoolId = ItemDrops.GetPoolId(dropType);
                if (PoolManager.Instance.HasPool(dropPoolId))
                {
                    Vector3 dropPosition = position + (Vector3)(Random.insideUnitCircle.normalized * 0.6f);
                    PoolManager.Instance.Get(dropPoolId, dropPosition, Quaternion.identity);
                }
            }

            // With a death animation, the corpse plays out and releases itself;
            // otherwise return to the pool immediately.
            if (TryGetComponent(out View.DeathAnimation deathAnimation))
            {
                deathAnimation.Play(stats.PoolId);
            }
            else
            {
                PoolManager.Instance.Release(stats.PoolId, gameObject);
            }
        }
    }
}
