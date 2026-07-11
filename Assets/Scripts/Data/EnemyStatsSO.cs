using UnityEngine;

namespace SurveHive.Data
{
    [CreateAssetMenu(menuName = "SurveHive/Enemy Stats", fileName = "NewEnemyStats")]
    public sealed class EnemyStatsSO : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private int _rank;
        [SerializeField] private float _maxHealth = 20f;
        // Defensive layers (PLAN 3B), applied shield → armor → HP by EnemyDefense.
        // Shields are flat pools soaking only their own damage type and scale with
        // the run's health multiplier; armor is a % reduction to physical hits
        // that got past shields. 0s (the default) = no layer; elites+ carry these.
        [SerializeField, Range(0f, 90f)] private float _armorPercent;
        [SerializeField] private float _physicalShield;
        [SerializeField] private float _magicShield;
        [SerializeField] private float _moveSpeed = 2f;
        [SerializeField] private float _contactDamage = 5f;
        [SerializeField] private float _contactDamageInterval = 1f;
        [SerializeField] private float _expReward = 5f;
        [SerializeField, Range(0f, 1f)] private float _currencyDropChance = 0.2f;
        [SerializeField] private int _currencyDropMin = 1;
        [SerializeField] private int _currencyDropMax = 3;
        // Chance to drop a world item (honey jar / magnet / shield / bomb);
        // elites+ carry the meaningful values.
        [SerializeField, Range(0f, 1f)] private float _itemDropChance;
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
        // Codex behavior blurb (playtest follow-up 2026-07-11): what this enemy
        // does, since raw HP/damage numbers shift with difficulty and run time.
        [SerializeField, TextArea] private string _codexDescription;

        public string DisplayName => _displayName;

        public int Rank => _rank;

        public float MaxHealth => _maxHealth;

        public float ArmorPercent => _armorPercent;

        public float PhysicalShield => _physicalShield;

        public float MagicShield => _magicShield;

        public float MoveSpeed => _moveSpeed;

        public float ContactDamage => _contactDamage;

        public float ContactDamageInterval => _contactDamageInterval;

        public float ExpReward => _expReward;

        public float CurrencyDropChance => _currencyDropChance;

        public int CurrencyDropMin => _currencyDropMin;

        public int CurrencyDropMax => _currencyDropMax;

        public float ItemDropChance => _itemDropChance;

        public Color SpriteTint => _spriteTint;

        public float Scale => _scale;

        public float KnockbackResistance => _knockbackResistance;

        public float DeathHitStopSeconds => _deathHitStopSeconds;

        public GameObject Prefab => _prefab;

        public int PoolId => _poolId;

        public string CodexDescription => _codexDescription;
    }
}
