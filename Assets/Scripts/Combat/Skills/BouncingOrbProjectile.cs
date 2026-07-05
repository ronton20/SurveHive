using SurveHive.Core;
using SurveHive.Enemies;
using UnityEngine;

namespace SurveHive.Combat.Skills
{
    /// <summary>
    /// Ball Lightning (Combat 2.0 1E): a slow persistent orb that travels in one
    /// direction, passes through enemies dealing ticking damage to everything it
    /// overlaps, and bounces off the screen edges until its lifetime runs out.
    /// Enemy overlap is tracked in a fixed buffer (zero-GC, no physics queries).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class BouncingOrbProjectile : MonoBehaviour
    {
        private const int MaxOverlap = 32;
        // Keep the orb a hair inside the viewport so it re-triggers a bounce cleanly.
        private const float EdgeMin = 0.02f;
        private const float EdgeMax = 0.98f;

        [SerializeField] private int _poolId;
        [SerializeField] private string _targetTag = "Enemy";

        private SpriteRenderer _spriteRenderer;
        private Color _originalColor = Color.white;
        private Camera _camera;

        private Vector2 _direction;
        private float _speed;
        private float _damagePerTick;
        private float _tickInterval;
        private float _tickTimer;
        private float _remainingLife;

        private readonly EnemyController[] _overlap = new EnemyController[MaxOverlap];
        private int _overlapCount;
        private bool _released;

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }

            _camera = Camera.main;
        }

        public void Launch(
            Vector2 direction, float speed, float damagePerTick, float tickInterval,
            float size, float lifetime, Color tint)
        {
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            _speed = speed;
            _damagePerTick = damagePerTick;
            _tickInterval = Mathf.Max(0.05f, tickInterval);
            _tickTimer = _tickInterval;
            _remainingLife = lifetime;
            _overlapCount = 0;
            _released = false;

            transform.localScale = new Vector3(size, size, 1f);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = tint.a > 0f ? tint : _originalColor;
            }
        }

        private void OnEnable()
        {
            _released = false;
        }

        private void OnDisable()
        {
            // Drop references so a pooled reuse starts clean.
            _overlapCount = 0;
        }

        private void Update()
        {
            _remainingLife -= Time.deltaTime;
            if (_remainingLife <= 0f)
            {
                ReleaseSelf();
                return;
            }

            transform.position += (Vector3)(_direction * (_speed * Time.deltaTime));
            BounceOffScreenEdges();

            _tickTimer -= Time.deltaTime;
            if (_tickTimer <= 0f)
            {
                _tickTimer += _tickInterval;
                DamageOverlapping();
            }
        }

        // Reflects direction when the orb reaches a viewport edge (the visible
        // screen, which tracks the player-follow camera).
        private void BounceOffScreenEdges()
        {
            if (_camera == null)
            {
                return;
            }

            Vector3 vp = _camera.WorldToViewportPoint(transform.position);
            if ((vp.x < EdgeMin && _direction.x < 0f) || (vp.x > EdgeMax && _direction.x > 0f))
            {
                _direction.x = -_direction.x;
            }

            if ((vp.y < EdgeMin && _direction.y < 0f) || (vp.y > EdgeMax && _direction.y > 0f))
            {
                _direction.y = -_direction.y;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(_targetTag) || _overlapCount >= MaxOverlap)
            {
                return;
            }

            if (!other.TryGetComponent(out EnemyController enemy))
            {
                return;
            }

            for (int i = 0; i < _overlapCount; i++)
            {
                if (_overlap[i] == enemy)
                {
                    return;
                }
            }

            _overlap[_overlapCount] = enemy;
            _overlapCount++;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent(out EnemyController enemy))
            {
                return;
            }

            RemoveOverlap(enemy);
        }

        // Ticks damage to everyone currently overlapping, compacting out any that
        // died or were pooled away without an exit event.
        private void DamageOverlapping()
        {
            int write = 0;
            for (int read = 0; read < _overlapCount; read++)
            {
                EnemyController enemy = _overlap[read];
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                {
                    continue;
                }

                DamageService.DealDamage(enemy.Health, enemy.transform.position, _damagePerTick, false, gameObject, false);
                _overlap[write] = enemy;
                write++;
            }

            _overlapCount = write;
        }

        private void RemoveOverlap(EnemyController enemy)
        {
            for (int i = 0; i < _overlapCount; i++)
            {
                if (_overlap[i] == enemy)
                {
                    _overlap[i] = _overlap[_overlapCount - 1];
                    _overlapCount--;
                    return;
                }
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
