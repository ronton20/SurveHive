using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Enemies;
using SurveHive.Player;
using UnityEngine;

namespace SurveHive.Combat.Skills
{
    /// <summary>
    /// Owns the player's equipped active skills (VS-style auto-firing weapons):
    /// per-skill level + cooldown state in fixed arrays, one firing routine per
    /// <see cref="ActiveSkillBehavior"/>. Cooldowns respect the player's
    /// cooldown-reduction stat. Zero allocations after Awake.
    /// </summary>
    public sealed class ActiveSkillManager : MonoBehaviour
    {
        private const int MaxEquipped = 8;
        private const int MaxChainTargets = 16;
        // A skill whose fire condition failed (e.g. no target in range) retries
        // quickly instead of waiting out its full cooldown.
        private const float RetryInterval = 0.25f;

        [SerializeField] private PlayerStats _stats;
        [SerializeField] private NearestEnemyTargeter _targeter;
        // Scaled/enabled to visualize the Aura skill (Pollen Cloud) radius.
        [SerializeField] private SpriteRenderer _auraVisual;

        private readonly ActiveSkillSO[] _equipped = new ActiveSkillSO[MaxEquipped];
        private readonly int[] _levels = new int[MaxEquipped];
        private readonly float[] _cooldowns = new float[MaxEquipped];
        private readonly EnemyController[] _chainTargets = new EnemyController[MaxChainTargets];
        private int _equippedCount;

        public int EquippedCount => _equippedCount;

        public ActiveSkillSO GetEquipped(int index) => _equipped[index];

        /// <summary>Equips the skill at level 1, or raises its level by one.</summary>
        public void AddOrLevelUp(ActiveSkillSO skill)
        {
            if (skill == null)
            {
                return;
            }

            for (int i = 0; i < _equippedCount; i++)
            {
                if (_equipped[i] == skill)
                {
                    _levels[i] = Mathf.Min(_levels[i] + 1, skill.MaxLevel);
                    RefreshAuraVisual();
                    return;
                }
            }

            if (_equippedCount >= MaxEquipped)
            {
                return;
            }

            _equipped[_equippedCount] = skill;
            _levels[_equippedCount] = 1;
            _cooldowns[_equippedCount] = 0.5f;
            _equippedCount++;
            RefreshAuraVisual();
        }

        // Ability Power passive (Combat 2.0 1C) scales all active-skill damage.
        private float AbilityDamage(float baseDamage)
        {
            return baseDamage * _stats.AbilityPowerMultiplier;
        }

        public int GetLevel(ActiveSkillSO skill)
        {
            for (int i = 0; i < _equippedCount; i++)
            {
                if (_equipped[i] == skill)
                {
                    return _levels[i];
                }
            }

            return 0;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _equippedCount; i++)
            {
                _cooldowns[i] -= deltaTime;
                if (_cooldowns[i] > 0f)
                {
                    continue;
                }

                ActiveSkillSO skill = _equipped[i];
                ActiveSkillLevelStats stats = skill.GetLevelStats(_levels[i]);

                if (Fire(skill, in stats))
                {
                    _cooldowns[i] = stats.Cooldown * _stats.ActiveCooldownMultiplier;
                    PlayFireSfx(skill.Behavior);
                }
                else
                {
                    _cooldowns[i] = RetryInterval;
                }
            }
        }

        // Pollen Cloud (Aura) ticks every ~0.25s regardless of hitting anyone —
        // re-triggering a "fire" sound at that rate would be noise, not feedback.
        private static void PlayFireSfx(ActiveSkillBehavior behavior)
        {
            if (behavior == ActiveSkillBehavior.Aura || AudioService.Instance == null)
            {
                return;
            }

            SfxId id;
            switch (behavior)
            {
                case ActiveSkillBehavior.RadialVolley:
                    id = SfxId.SkillStingerBarrage;
                    break;
                case ActiveSkillBehavior.PiercingShot:
                    id = SfxId.SkillPiercingLance;
                    break;
                case ActiveSkillBehavior.LobbedPuddle:
                    id = SfxId.SkillHoneySplash;
                    break;
                case ActiveSkillBehavior.ChainArc:
                    id = SfxId.SkillStaticWings;
                    break;
                case ActiveSkillBehavior.HomingBolt:
                    id = SfxId.SkillEmberSting;
                    break;
                default:
                    return;
            }

            AudioService.Instance.PlaySfx(id);
        }

        private bool Fire(ActiveSkillSO skill, in ActiveSkillLevelStats stats)
        {
            switch (skill.Behavior)
            {
                case ActiveSkillBehavior.RadialVolley:
                    return FireRadialVolley(skill, in stats);
                case ActiveSkillBehavior.PiercingShot:
                    return FirePiercingShot(skill, in stats);
                case ActiveSkillBehavior.LobbedPuddle:
                    return FireLobbedPuddle(skill, in stats);
                case ActiveSkillBehavior.Aura:
                    return FireAuraTick(skill, in stats);
                case ActiveSkillBehavior.ChainArc:
                    return FireChainArc(skill, in stats);
                case ActiveSkillBehavior.HomingBolt:
                    return FireHomingBolt(skill, in stats);
            }

            return false;
        }

        private bool FireRadialVolley(ActiveSkillSO skill, in ActiveSkillLevelStats stats)
        {
            if (PoolManager.Instance == null)
            {
                return false;
            }

            int count = Mathf.Max(1, stats.Count);
            float startAngle = Random.Range(0f, 360f);
            float step = 360f / count;

            for (int i = 0; i < count; i++)
            {
                Vector2 direction = Quaternion.Euler(0f, 0f, startAngle + (step * i)) * Vector2.right;
                LaunchProjectile(skill, in stats, direction, null, 0, 1.5f);
            }

            return true;
        }

        private bool FirePiercingShot(ActiveSkillSO skill, in ActiveSkillLevelStats stats)
        {
            Transform target = _targeter.CurrentTarget;
            if (target == null || PoolManager.Instance == null)
            {
                return false;
            }

            Vector2 direction = ((Vector2)(target.position - transform.position)).normalized;
            LaunchProjectile(skill, in stats, direction, null, 999, 2f);
            return true;
        }

        private bool FireLobbedPuddle(ActiveSkillSO skill, in ActiveSkillLevelStats stats)
        {
            Transform target = _targeter.CurrentTarget;
            if (target == null || PoolManager.Instance == null)
            {
                return false;
            }

            Vector3 toTarget = target.position - transform.position;
            float sqrRange = skill.Range * skill.Range;
            Vector3 landingPoint = toTarget.sqrMagnitude > sqrRange
                ? transform.position + (toTarget.normalized * skill.Range)
                : target.position;

            GameObject projectileObj = PoolManager.Instance.Get(
                skill.ProjectilePoolId, transform.position, Quaternion.identity);
            if (!projectileObj.TryGetComponent(out SkillProjectile projectile))
            {
                return false;
            }

            var config = new SkillProjectileConfig
            {
                Speed = skill.ProjectileSpeed,
                Damage = AbilityDamage(stats.Damage),
                Range = skill.Range * 2f,
                ImpactVfxPoolId = skill.ImpactVfxPoolId,
                AppliesStatus = skill.AppliesStatus,
                StatusType = skill.StatusType,
                StatusChancePercent = stats.StatusChancePercent,
                StatusPotency = skill.StatusPotency,
                StatusDuration = skill.StatusDuration,
                TravelToPoint = true,
                TargetPoint = landingPoint,
                ZonePoolId = skill.ZonePoolId,
                ZoneRadius = stats.Area,
                ZoneDuration = skill.ZoneDuration,
                ZoneTickInterval = skill.ZoneTickInterval,
            };
            projectile.Launch(in config);
            return true;
        }

        private bool FireAuraTick(ActiveSkillSO skill, in ActiveSkillLevelStats stats)
        {
            if (EnemyRegistry.Instance == null)
            {
                return true;
            }

            float sqrRadius = stats.Area * stats.Area;
            Vector3 center = transform.position;
            var enemies = EnemyRegistry.Instance.ActiveEnemies;

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                {
                    continue;
                }

                if ((enemy.transform.position - center).sqrMagnitude > sqrRadius)
                {
                    continue;
                }

                // No popup: at 4 ticks/s the numbers would flood the screen —
                // health bars + poison DoT numbers carry the feedback.
                DamageService.DealDamage(enemy.Health, enemy.transform.position, AbilityDamage(stats.Damage), false, gameObject, false);

                if (skill.AppliesStatus && enemy.StatusReceiver != null &&
                    Random.value * 100f < stats.StatusChancePercent)
                {
                    enemy.StatusReceiver.ApplyEffect(skill.StatusType, skill.StatusPotency, skill.StatusDuration);
                }
            }

            return true;
        }

        private bool FireChainArc(ActiveSkillSO skill, in ActiveSkillLevelStats stats)
        {
            if (EnemyRegistry.Instance == null)
            {
                return false;
            }

            int maxTargets = Mathf.Min(Mathf.Max(1, stats.Count), MaxChainTargets);
            int found = 0;

            // First link: nearest enemy to the player within skill range.
            EnemyController current = FindNearestEnemy(transform.position, skill.Range, found);
            Vector3 previousPoint = transform.position;

            while (current != null && found < maxTargets)
            {
                _chainTargets[found] = current;
                found++;

                DamageService.DealDamage(current.Health, current.transform.position, AbilityDamage(stats.Damage), true, gameObject);

                if (skill.AppliesStatus && current.StatusReceiver != null &&
                    Random.value * 100f < stats.StatusChancePercent)
                {
                    current.StatusReceiver.ApplyEffect(skill.StatusType, skill.StatusPotency, skill.StatusDuration);
                }

                SpawnZapArc(previousPoint, current.transform.position, skill.ImpactVfxPoolId);
                previousPoint = current.transform.position;

                // Next link: nearest unvisited enemy within jump range (Area).
                current = found < maxTargets ? FindNearestEnemy(previousPoint, stats.Area, found) : null;
            }

            return found > 0;
        }

        private bool FireHomingBolt(ActiveSkillSO skill, in ActiveSkillLevelStats stats)
        {
            Transform target = _targeter.CurrentTarget;
            if (target == null || PoolManager.Instance == null)
            {
                return false;
            }

            Vector2 direction = ((Vector2)(target.position - transform.position)).normalized;
            LaunchProjectile(skill, in stats, direction, target, 0, 2f);
            return true;
        }

        private void LaunchProjectile(
            ActiveSkillSO skill, in ActiveSkillLevelStats stats, Vector2 direction,
            Transform homingTarget, int pierceCount, float knockback)
        {
            GameObject projectileObj = PoolManager.Instance.Get(
                skill.ProjectilePoolId, transform.position, Quaternion.identity);
            if (!projectileObj.TryGetComponent(out SkillProjectile projectile))
            {
                return;
            }

            var config = new SkillProjectileConfig
            {
                Direction = direction,
                Speed = skill.ProjectileSpeed,
                Damage = AbilityDamage(stats.Damage),
                Range = skill.Range,
                PierceCount = pierceCount,
                HomingTarget = homingTarget,
                ExplodeRadius = stats.Area,
                KnockbackImpulse = knockback,
                ImpactVfxPoolId = skill.ImpactVfxPoolId,
                AppliesStatus = skill.AppliesStatus,
                StatusType = skill.StatusType,
                StatusChancePercent = stats.StatusChancePercent,
                StatusPotency = skill.StatusPotency,
                StatusDuration = skill.StatusDuration,
                ZonePoolId = -1,
            };
            projectile.Launch(in config);
        }

        private EnemyController FindNearestEnemy(Vector3 origin, float maxRange, int visitedCount)
        {
            var enemies = EnemyRegistry.Instance.ActiveEnemies;
            float bestSqrDistance = maxRange * maxRange;
            EnemyController best = null;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                {
                    continue;
                }

                bool visited = false;
                for (int j = 0; j < visitedCount; j++)
                {
                    if (_chainTargets[j] == enemy)
                    {
                        visited = true;
                        break;
                    }
                }

                if (visited)
                {
                    continue;
                }

                float sqrDistance = (enemy.transform.position - origin).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = enemy;
                }
            }

            return best;
        }

        private void SpawnZapArc(Vector3 from, Vector3 to, int vfxPoolId)
        {
            if (vfxPoolId < 0 || PoolManager.Instance == null)
            {
                return;
            }

            GameObject arcObj = PoolManager.Instance.Get(vfxPoolId, from, Quaternion.identity);
            if (arcObj.TryGetComponent(out ZapArcVfx arc))
            {
                arc.Show(from, to);
            }
        }

        // The aura sprite tracks the Pollen Cloud skill: hidden until equipped,
        // scaled to the current level's radius (sprite radius = 1u at scale 1).
        private void RefreshAuraVisual()
        {
            if (_auraVisual == null)
            {
                return;
            }

            for (int i = 0; i < _equippedCount; i++)
            {
                if (_equipped[i].Behavior == ActiveSkillBehavior.Aura)
                {
                    ActiveSkillLevelStats stats = _equipped[i].GetLevelStats(_levels[i]);
                    _auraVisual.enabled = true;
                    _auraVisual.transform.localScale = new Vector3(stats.Area, stats.Area, 1f);
                    return;
                }
            }

            _auraVisual.enabled = false;
        }
    }
}
