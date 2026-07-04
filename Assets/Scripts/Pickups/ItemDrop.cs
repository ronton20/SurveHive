using SurveHive.Combat;
using SurveHive.Core;
using SurveHive.Enemies;
using SurveHive.Player;
using UnityEngine;

namespace SurveHive.Pickups
{
    /// <summary>
    /// Pooled world item drop: sits until the player walks into it, applies its
    /// effect (heal / vacuum / shield / nuke), and self-releases (also after a
    /// lifetime so drops don't accumulate forever).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ItemDrop : MonoBehaviour
    {
        [SerializeField] private int _poolId;
        [SerializeField] private ItemDropType _type;
        [SerializeField] private string _targetTag = "Player";
        [SerializeField] private float _lifetimeSeconds = 20f;
        [SerializeField, Range(0f, 1f)] private float _healFraction = 0.3f;
        [SerializeField] private float _magnetVacuumSeconds = 3f;
        [SerializeField] private int _shieldCharges = 3;
        [SerializeField] private float _nukeDamage = 300f;
        [SerializeField] private float _nukeRadius = 14f;

        private float _remainingLifetime;
        private bool _released;

        private void OnEnable()
        {
            _remainingLifetime = _lifetimeSeconds;
            _released = false;
        }

        private void Update()
        {
            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
            {
                ReleaseSelf();
                return;
            }

            // Item drops share the exact pickup range (and Nectar Sense /
            // vacuum scaling) of EXP/currency motes.
            Transform player = Player.PlayerContext.Transform;
            if (player != null)
            {
                PickupMotion.Step(transform, player);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_released || !other.CompareTag(_targetTag))
            {
                return;
            }

            ApplyEffect();
            ReleaseSelf();
        }

        private void ApplyEffect()
        {
            switch (_type)
            {
                case ItemDropType.HoneyJar:
                    if (PlayerContext.Health != null)
                    {
                        PlayerContext.Health.Heal(PlayerContext.Health.MaxHealth * _healFraction);
                    }

                    break;

                case ItemDropType.Magnet:
                    PickupMotion.ActivateVacuum(_magnetVacuumSeconds);
                    break;

                case ItemDropType.WaxShield:
                    if (PlayerShield.Instance != null)
                    {
                        PlayerShield.Instance.AddCharges(_shieldCharges);
                    }

                    break;

                case ItemDropType.RoyalBomb:
                    DetonateNuke();
                    break;
            }
        }

        private void DetonateNuke()
        {
            if (PoolManager.Instance != null && PoolManager.Instance.HasPool(PoolIds.NukeVfx))
            {
                PoolManager.Instance.Get(PoolIds.NukeVfx, transform.position, Quaternion.identity);
            }

            if (HitStop.Instance != null)
            {
                HitStop.Instance.Request(0.1f);
            }

            if (EnemyRegistry.Instance == null)
            {
                return;
            }

            float sqrRadius = _nukeRadius * _nukeRadius;
            Vector3 center = transform.position;
            var enemies = EnemyRegistry.Instance.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                {
                    continue;
                }

                if ((enemy.transform.position - center).sqrMagnitude > sqrRadius)
                {
                    continue;
                }

                DamageService.DealDamage(enemy.Health, enemy.transform.position, _nukeDamage, false, gameObject);
            }
        }

        private void ReleaseSelf()
        {
            if (_released)
            {
                return;
            }

            _released = true;

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Release(_poolId, gameObject);
            }
        }
    }
}
