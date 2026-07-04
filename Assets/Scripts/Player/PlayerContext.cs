using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Player
{
    /// <summary>
    /// Static access to the live player's stats/health for systems that deal
    /// damage on the player's behalf (projectiles, skills, pickups). Registered
    /// by <see cref="PlayerBootstrap"/>; mirrors the project's other scene
    /// singletons (PoolManager, EnemyRegistry).
    /// </summary>
    public static class PlayerContext
    {
        public static PlayerStats Stats { get; private set; }

        public static HealthComponent Health { get; private set; }

        public static Transform Transform { get; private set; }

        public static void Register(PlayerStats stats, HealthComponent health, Transform transform)
        {
            Stats = stats;
            Health = health;
            Transform = transform;
        }

        public static void Clear()
        {
            Stats = null;
            Health = null;
            Transform = null;
        }
    }
}
