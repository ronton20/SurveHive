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
                _audioSource.PlayOneShot(_shootClip);
            }

            for (int i = 0; i < projectileCount; i++)
            {
                float angleOffset = (i - (projectileCount - 1) / 2f) * 10f;
                Vector2 shotDirection = Quaternion.Euler(0f, 0f, angleOffset) * direction;

                GameObject projectileObj = PoolManager.Instance.Get(_projectilePoolId, transform.position, Quaternion.identity);
                if (projectileObj.TryGetComponent(out Projectile projectile))
                {
                    projectile.Launch(shotDirection, _stats.EffectiveAttackDamage, _projectileSpeed, _stats.AttackRange);
                }
            }
        }
    }
}
