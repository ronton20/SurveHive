using System;
using SurveHive.Combat.Status;
using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Data
{
    public enum ActiveSkillBehavior
    {
        // Evenly spread ring of projectiles around the player.
        RadialVolley = 0,
        // Single high-speed projectile that pierces through everything in a line.
        PiercingShot = 1,
        // Glob lobbed at the target's position; spawns a damaging zone on landing.
        LobbedPuddle = 2,
        // Aura around the player; the cooldown acts as the damage tick interval.
        Aura = 3,
        // Instant arc chaining between up to Count enemies.
        ChainArc = 4,
        // Homing projectile that explodes on impact.
        HomingBolt = 5,

        // Combat 2.0 1E — distinct-gameplay behaviors:
        // Instant 360° blast around the player (damage + guaranteed status).
        Nova = 6,
        // Slow persistent orb: pierces, deals ticking damage, bounces off screen edges.
        BouncingOrb = 7,
        // Scatters several lobbed zones at random points around the player.
        ScatterZones = 8
    }

    // One row of the per-level growth table. Meaning of Count/Area varies by
    // behavior: RadialVolley count = projectiles, ChainArc count = chain targets;
    // area = puddle/aura/explosion radius or chain jump range (world units).
    [Serializable]
    public struct ActiveSkillLevelStats
    {
        public float Damage;
        public float Cooldown;
        public int Count;
        public float Area;
        [Range(0f, 100f)] public float StatusChancePercent;
        // Per-level status duration (Combat 2.0 1E, e.g. Frost Nova's slow length).
        // 0 = fall back to the skill's flat StatusDuration.
        public float StatusDuration;
    }

    [CreateAssetMenu(menuName = "SurveHive/Active Skill", fileName = "NewActiveSkill")]
    public sealed class ActiveSkillSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private ActiveSkillBehavior _behavior;
        // Physical for stinger-type abilities, Magic for elemental ones — must
        // match the offer card's SkillElement (Physical element ⇔ Physical damage).
        [SerializeField] private DamageType _damageType = DamageType.Physical;
        [SerializeField] private ActiveSkillLevelStats[] _levels;

        [Header("Delivery")]
        // Tints the pooled projectile so skills sharing a pool read as distinct
        // (e.g. blue frost vs yellow electric on the same stinger sprite). Alpha 0
        // means "leave the prefab's own colour" (the default for pre-tint skills).
        [SerializeField] private Color _projectileTint = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private float _projectileSpeed = 10f;
        // Max targeting/travel distance from the player.
        [SerializeField] private float _range = 8f;
        [SerializeField] private int _projectilePoolId = -1;
        [SerializeField] private int _impactVfxPoolId = -1;

        [Header("Zone (LobbedPuddle)")]
        [SerializeField] private int _zonePoolId = -1;
        [SerializeField] private float _zoneDuration = 3.5f;
        [SerializeField] private float _zoneTickInterval = 0.5f;

        [Header("Status Effect")]
        [SerializeField] private bool _appliesStatus;
        [SerializeField] private StatusEffectType _statusType;
        // Burn/Poison: damage per second (per stack); Slow: speed reduction 0-1;
        // Freeze: break damage threshold; Stun: unused.
        [SerializeField] private float _statusPotency;
        [SerializeField] private float _statusDuration = 2f;

        public string Id => _id;

        public string DisplayName => _displayName;

        public ActiveSkillBehavior Behavior => _behavior;

        public DamageType DamageType => _damageType;

        public Color ProjectileTint => _projectileTint;

        public int MaxLevel => _levels != null ? _levels.Length : 0;

        public float ProjectileSpeed => _projectileSpeed;

        public float Range => _range;

        public int ProjectilePoolId => _projectilePoolId;

        public int ImpactVfxPoolId => _impactVfxPoolId;

        public int ZonePoolId => _zonePoolId;

        public float ZoneDuration => _zoneDuration;

        public float ZoneTickInterval => _zoneTickInterval;

        public bool AppliesStatus => _appliesStatus;

        public StatusEffectType StatusType => _statusType;

        public float StatusPotency => _statusPotency;

        public float StatusDuration => _statusDuration;

        /// <summary>Level is 1-based; out-of-range levels clamp to the table edges.</summary>
        public ActiveSkillLevelStats GetLevelStats(int level)
        {
            int index = Mathf.Clamp(level, 1, _levels.Length) - 1;
            return _levels[index];
        }
    }
}
