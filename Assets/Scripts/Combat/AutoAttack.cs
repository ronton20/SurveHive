using SurveHive.Core;
using SurveHive.Player;
using SurveHive.View;
using UnityEngine;

namespace SurveHive.Combat
{
    public sealed class AutoAttack : MonoBehaviour
    {
        [SerializeField] private NearestEnemyTargeter _targeter;
        [SerializeField] private PlayerStats _stats;
        [SerializeField] private int _projectilePoolId;
        [SerializeField] private float _projectileSpeed = 10f;
        // Piercing shots travel farther so they actually reach enemies beyond the
        // first one (base attack range is short).
        [SerializeField] private float _pierceRangeMultiplier = 2.5f;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _shootClip;
        [SerializeField] private CharacterAnimator _characterAnimator;

        private float _cooldownRemaining;

        private void Update()
        {
            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining -= Time.deltaTime;
                return;
            }

            Transform target = _targeter.CurrentTarget;
            if (target == null)
            {
                return;
            }

            float sqrRange = _stats.AttackRange * _stats.AttackRange;
            if ((target.position - transform.position).sqrMagnitude > sqrRange)
            {
                return;
            }

            FireAtTarget(target);
            _cooldownRemaining = _stats.EffectiveAttackInterval;
        }

        private void FireAtTarget(Transform target)
        {
            if (PoolManager.Instance == null)
            {
                return;
            }

            Vector2 direction = ((Vector2)(target.position - transform.position)).normalized;
            int projectileCount = Mathf.Max(1, _stats.ProjectileCount);

            if (_characterAnimator != null)
            {
                _characterAnimator.PlayAttack(_stats.AttackSpeed);
                _characterAnimator.FaceDirection(direction.x);
            }

            if (_audioSource != null && _shootClip != null)
            {
                float volumeScale = AudioService.Instance != null ? AudioService.Instance.SfxVolume : 1f;
                _audioSource.PlayOneShot(_shootClip, volumeScale);
            }

            // Enhancement modifiers (Combat 2.0 1D): Multishot spreads damage over
            // more projectiles (~1.5× total per extra), Piercing Stinger trades raw
            // damage for pass-through, and the elemental stingers roll their procs
            // per hit. Built once per volley; each shot differs only by direction.
            int pierce = _stats.BasicAttackPierce;
            float perProjectileDamage = CombatMath.MultishotPerProjectileDamage(_stats.EffectiveAttackDamage, projectileCount);
            // Level-based pierce penalty (1.0 when no pierce, so always safe to apply).
            perProjectileDamage *= _stats.PierceDamageMultiplier;
            // Physical set bonus (Phase 3C): committing to physical picks sharpens
            // the basic attack itself. 1.0 while the set is inactive.
            perProjectileDamage *= Progression.ElementSets.AttackDamageMultiplier;
            float range = pierce > 0 ? _stats.AttackRange * _pierceRangeMultiplier : _stats.AttackRange;

            var payload = new BasicAttackPayload
            {
                Damage = perProjectileDamage,
                Speed = _projectileSpeed,
                Range = range,
                Pierce = pierce,
                BurnChance = _stats.BurnStingerChance,
                BurnDps = _stats.BurnStingerDps,
                BurnDuration = _stats.BurnStingerDuration,
                PoisonChance = _stats.PoisonStingerChance,
                PoisonDps = _stats.PoisonStingerDps,
                PoisonDuration = _stats.PoisonStingerDuration,
                FreezeChance = _stats.FrostStingerChance,
                FreezeThreshold = _stats.FrostStingerBreakThreshold,
                FreezeDuration = _stats.FrostStingerDuration,
                BounceChance = _stats.ShockStingerChance,
                BounceCount = _stats.ShockStingerBounces,
                BounceRange = _stats.ShockBounceRange,
                BounceDamageFalloff = _stats.ShockBounceDamageFalloff,
                BounceChanceFalloff = _stats.ShockBounceChanceFalloff,
            };

            for (int i = 0; i < projectileCount; i++)
            {
                float angleOffset = (i - (projectileCount - 1) / 2f) * 10f;
                Vector2 shotDirection = Quaternion.Euler(0f, 0f, angleOffset) * direction;

                GameObject projectileObj = PoolManager.Instance.Get(_projectilePoolId, transform.position, Quaternion.identity);
                if (projectileObj.TryGetComponent(out Projectile projectile))
                {
                    projectile.Launch(shotDirection, in payload);
                }
            }
        }
    }
}
