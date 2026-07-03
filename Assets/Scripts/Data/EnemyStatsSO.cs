using UnityEngine;

namespace SurveHive.Data
{
    [CreateAssetMenu(menuName = "SurveHive/Enemy Stats", fileName = "NewEnemyStats")]
    public sealed class EnemyStatsSO : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private int _rank;
        [SerializeField] private float _maxHealth = 20f;
        [SerializeField] private float _moveSpeed = 2f;
        [SerializeField] private float _contactDamage = 5f;
        [SerializeField] private float _contactDamageInterval = 1f;
        [SerializeField] private float _expReward = 5f;
        [SerializeField, Range(0f, 1f)] private float _currencyDropChance = 0.2f;
        [SerializeField] private int _currencyDropMin = 1;
        [SerializeField] private int _currencyDropMax = 3;
        [SerializeField] private Color _spriteTint = Color.white;
        // Uniform world-scale of the whole enemy — ranks share one rig and read
        // bigger/smaller through this rather than separate art.
        [SerializeField] private float _scale = 1f;
        // Divides incoming knockback impulses; heavier ranks budge less.
        [SerializeField] private float _knockbackResistance = 1f;
        // Micro time-freeze on this enemy's death (0 = none); reserved for elites+.
        [SerializeField] private float _deathHitStopSeconds;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _poolId;

        public string DisplayName => _displayName;

        public int Rank => _rank;

        public float MaxHealth => _maxHealth;

        public float MoveSpeed => _moveSpeed;

        public float ContactDamage => _contactDamage;

        public float ContactDamageInterval => _contactDamageInterval;

        public float ExpReward => _expReward;

        public float CurrencyDropChance => _currencyDropChance;

        public int CurrencyDropMin => _currencyDropMin;

        public int CurrencyDropMax => _currencyDropMax;

        public Color SpriteTint => _spriteTint;

        public float Scale => _scale;

        public float KnockbackResistance => _knockbackResistance;

        public float DeathHitStopSeconds => _deathHitStopSeconds;

        public GameObject Prefab => _prefab;

        public int PoolId => _poolId;
    }
}
