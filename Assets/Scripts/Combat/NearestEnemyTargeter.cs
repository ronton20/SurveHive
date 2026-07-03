using SurveHive.Core;
using SurveHive.Enemies;
using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Combat
{
    public sealed class NearestEnemyTargeter : MonoBehaviour
    {
        [SerializeField] private float _retargetInterval = 0.15f;

        private float _retargetTimer;
        private Transform _currentTarget;
        private HealthComponent _currentTargetHealth;

        public Transform CurrentTarget => IsTargetValid() ? _currentTarget : null;

        private void Update()
        {
            _retargetTimer -= Time.deltaTime;

            if (!IsTargetValid())
            {
                _currentTarget = null;
                _currentTargetHealth = null;
            }

            if (_retargetTimer <= 0f)
            {
                FindNearest();
                _retargetTimer = _retargetInterval;
            }
        }

        private bool IsTargetValid()
        {
            return _currentTarget != null && _currentTargetHealth != null && !_currentTargetHealth.IsDead;
        }

        private void FindNearest()
        {
            if (EnemyRegistry.Instance == null)
            {
                return;
            }

            var enemies = EnemyRegistry.Instance.ActiveEnemies;
            Vector3 selfPosition = transform.position;

            float bestSqrDistance = float.MaxValue;
            Transform bestTransform = null;
            HealthComponent bestHealth = null;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null)
                {
                    continue;
                }

                HealthComponent health = enemy.Health;
                if (health == null || health.IsDead)
                {
                    continue;
                }

                float sqrDistance = (enemy.transform.position - selfPosition).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestTransform = enemy.transform;
                    bestHealth = health;
                }
            }

            _currentTarget = bestTransform;
            _currentTargetHealth = bestHealth;
        }
    }
}
