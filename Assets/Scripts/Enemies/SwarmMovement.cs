using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Enemies
{
    /// <summary>
    /// Swarm rank movement (PLAN 4C): chases like the controller's default
    /// steering but weaves a perpendicular sine wobble with a per-instance
    /// phase, so a pack spawned together fans out into a living cloud instead
    /// of a single stacked column. Zero-alloc; pooled-safe.
    /// </summary>
    [RequireComponent(typeof(EnemyController), typeof(HealthComponent))]
    public sealed class SwarmMovement : MonoBehaviour
    {
        [SerializeField] private EnemyController _enemyController;
        [SerializeField] private HealthComponent _health;
        // Sideways speed as a fraction of MoveSpeed at the wobble peak.
        [SerializeField] private float _wobbleAmplitude = 0.6f;
        [SerializeField] private float _wobbleHz = 1.6f;

        private float _phase;

        private void OnEnable()
        {
            _phase = Random.value * (Mathf.PI * 2f);
        }

        private void OnDisable()
        {
            _enemyController.ClearMovementOverride();
        }

        private void FixedUpdate()
        {
            if (_health.IsDead || _enemyController.Stats == null)
            {
                return;
            }

            Transform target = _enemyController.Target;
            if (target == null)
            {
                return;
            }

            Vector2 direction = ((Vector2)(target.position - transform.position)).normalized;
            Vector2 tangent = new Vector2(-direction.y, direction.x);
            float wobble = Mathf.Sin(Time.time * (_wobbleHz * Mathf.PI * 2f) + _phase) * _wobbleAmplitude;

            Vector2 velocity = (direction + tangent * wobble).normalized * _enemyController.Stats.MoveSpeed;
            _enemyController.SetMovementOverride(velocity);
        }
    }
}
