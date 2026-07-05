using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Health;
using SurveHive.Spawning;
using UnityEngine;

namespace SurveHive.Enemies
{
    /// <summary>
    /// Queen Bee pattern brain: rotates through three telegraphed patterns —
    /// summon corrupted workers, radial stinger burst, charging sweep (via
    /// <see cref="ChargeAttack"/>). Initialized by the boss spawner after the
    /// pooled spawn. Zero allocations per frame.
    /// </summary>
    [RequireComponent(typeof(EnemyController), typeof(HealthComponent), typeof(ChargeAttack))]
    public sealed class QueenBossController : MonoBehaviour
    {
        private enum Pattern { Summon = 0, StingerBurst = 1, ChargeSweep = 2 }
        private enum State { Cooldown, Telegraph }

        [SerializeField] private EnemyController _enemyController;
        [SerializeField] private HealthComponent _health;
        [SerializeField] private ChargeAttack _chargeAttack;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private float _patternIntervalSeconds = 5f;
        [SerializeField] private float _telegraphSeconds = 0.8f;
        [SerializeField] private int _summonCount = 6;
        [SerializeField] private float _summonRadius = 2.5f;
        [SerializeField] private int _stingerCount = 14;
        [SerializeField] private float _stingerSpeed = 6f;
        [SerializeField] private float _stingerRange = 14f;
        [SerializeField] private float _stingerDamageFraction = 0.6f;
        [SerializeField] private int _stingerPoolId = PoolIds.EnemyStinger;

        private static readonly Color TelegraphColor = new Color(1f, 0.35f, 1f);

        private EnemySpawner _spawner;
        private EnemyStatsSO _summonStats;
        private State _state;
        private Pattern _nextPattern;
        private float _timer;
        private Color _restColor;

        public void Initialize(EnemySpawner spawner, EnemyStatsSO summonStats)
        {
            _spawner = spawner;
            _summonStats = summonStats;
        }

        private void OnEnable()
        {
            _state = State.Cooldown;
            _nextPattern = Pattern.Summon;
            _timer = _patternIntervalSeconds;
        }

        private void OnDisable()
        {
            _enemyController.ClearMovementOverride();
        }

        private void Update()
        {
            if (_health.IsDead || _chargeAttack.IsBusy)
            {
                return;
            }

            _timer -= Time.deltaTime;

            switch (_state)
            {
                case State.Cooldown:
                    if (_timer <= 0f)
                    {
                        BeginPattern();
                    }

                    break;

                case State.Telegraph:
                    if (_renderer != null)
                    {
                        float pulse = Mathf.PingPong(Time.time * 6f, 1f);
                        _renderer.color = Color.Lerp(_restColor, TelegraphColor, pulse);
                    }

                    if (_timer <= 0f)
                    {
                        ExecutePattern();
                    }

                    break;
            }
        }

        private void BeginPattern()
        {
            // The charge sweep telegraphs through ChargeAttack itself.
            if (_nextPattern == Pattern.ChargeSweep)
            {
                _chargeAttack.TriggerCharge();
                AdvancePattern();
                return;
            }

            _state = State.Telegraph;
            _timer = _telegraphSeconds;
            _restColor = _renderer != null ? _renderer.color : Color.white;
            _enemyController.SetMovementOverride(Vector2.zero);
        }

        private void ExecutePattern()
        {
            if (_renderer != null)
            {
                _renderer.color = _restColor;
            }

            _enemyController.ClearMovementOverride();

            switch (_nextPattern)
            {
                case Pattern.Summon:
                    SummonWorkers();
                    break;
                case Pattern.StingerBurst:
                    FireStingerBurst();
                    break;
            }

            AdvancePattern();
        }

        private void AdvancePattern()
        {
            _nextPattern = (Pattern)(((int)_nextPattern + 1) % 3);
            _state = State.Cooldown;
            _timer = _patternIntervalSeconds;
        }

        private void SummonWorkers()
        {
            if (_spawner == null || _summonStats == null)
            {
                return;
            }

            float step = 360f / Mathf.Max(1, _summonCount);
            for (int i = 0; i < _summonCount; i++)
            {
                Vector2 direction = Quaternion.Euler(0f, 0f, step * i) * Vector2.right;
                _spawner.SpawnAt(_summonStats, transform.position + (Vector3)(direction * _summonRadius));
            }
        }

        private void FireStingerBurst()
        {
            if (PoolManager.Instance == null || !PoolManager.Instance.HasPool(_stingerPoolId))
            {
                return;
            }

            if (AudioService.Instance != null)
            {
                AudioService.Instance.PlaySfx(SfxId.BossStinger);
            }

            float damage = _enemyController.Stats != null
                ? _enemyController.Stats.ContactDamage * _stingerDamageFraction
                : 10f;

            float step = 360f / Mathf.Max(1, _stingerCount);
            for (int i = 0; i < _stingerCount; i++)
            {
                Vector2 direction = Quaternion.Euler(0f, 0f, step * i) * Vector2.right;
                GameObject stinger = PoolManager.Instance.Get(_stingerPoolId, transform.position, Quaternion.identity);
                if (stinger.TryGetComponent(out EnemyProjectile projectile))
                {
                    projectile.Launch(direction, damage, _stingerSpeed, _stingerRange);
                }
            }
        }
    }
}
