using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Combat.Status
{
    /// <summary>
    /// Scene-facing wrapper around a <see cref="StatusEffectBuffer"/>: ticks the
    /// buffer, deals DoT damage with colored damage numbers, feeds freeze-break
    /// damage, and shows a cheap tint cue on the sprite while effects are active.
    /// Pooled-safe: fully resets in OnEnable.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public sealed class StatusEffectReceiver : MonoBehaviour
    {
        [SerializeField] private HealthComponent _health;
        [SerializeField] private SpriteRenderer _renderer;

        // Highest-priority active effect drives the tint (multiplied into the base
        // rank tint): Freeze > Stun > Burn > Poison > Slow.
        private static readonly Color FreezeTint = new Color(0.55f, 0.8f, 1f);
        private static readonly Color StunTint = new Color(1f, 1f, 0.45f);
        private static readonly Color BurnTint = new Color(1f, 0.55f, 0.35f);
        private static readonly Color PoisonTint = new Color(0.6f, 1f, 0.45f);
        private static readonly Color SlowTint = new Color(0.65f, 0.75f, 1f);
        private static readonly Color ColdTint = new Color(0.6f, 0.85f, 1f);

        private readonly StatusEffectBuffer _buffer = new StatusEffectBuffer();
        private Color _baseTint = Color.white;
        private int _lastTintKey = -1;

        public StatusEffectBuffer Buffer => _buffer;

        public float MoveSpeedMultiplier => _buffer.MoveSpeedMultiplier;

        public bool IsAttackDisabled => _buffer.IsAttackDisabled;

        private void OnEnable()
        {
            _buffer.Reset();
            _lastTintKey = -1;
            _health.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            _health.OnDamaged -= HandleDamaged;
        }

        /// <summary>Called by the owner on (re)spawn with its rank tint and elite flag.</summary>
        public void Configure(Color baseTint, bool diminishingStuns)
        {
            _baseTint = baseTint;
            _buffer.SetDiminishingStuns(diminishingStuns);
            _lastTintKey = -1;
            RefreshTint();
        }

        public void ApplyEffect(StatusEffectType type, float potency, float duration)
        {
            if (_health.IsDead)
            {
                return;
            }

            _buffer.Apply(type, potency, duration);
        }

        private void HandleDamaged(float amount)
        {
            _buffer.NotifyDamageTaken(amount);
        }

        private void Update()
        {
            if (_health.IsDead)
            {
                return;
            }

            _buffer.Tick(Time.deltaTime);

            if (_buffer.TryConsumeTickDamage(out float burnDamage, out float poisonDamage))
            {
                if (burnDamage > 0f)
                {
                    DealDotDamage(burnDamage, DamagePopupSpawner.BurnColor);
                }

                if (poisonDamage > 0f && !_health.IsDead)
                {
                    DealDotDamage(poisonDamage, DamagePopupSpawner.PoisonColor);
                }
            }

            RefreshTint();
        }

        private void DealDotDamage(float amount, Color color)
        {
            float rounded = Mathf.Max(1f, Mathf.Round(amount));
            _health.TakeDamage(rounded, gameObject);
            DamagePopupSpawner.Spawn(transform.position, rounded, color, DamagePopupSpawner.DotSizeMultiplier);
        }

        private void RefreshTint()
        {
            if (_renderer == null)
            {
                return;
            }

            int tintKey = 0;
            for (int i = 0; i < StatusEffectBuffer.EffectTypeCount; i++)
            {
                if (_buffer.IsActive((StatusEffectType)i))
                {
                    tintKey |= 1 << i;
                }
            }

            if (tintKey == _lastTintKey)
            {
                return;
            }

            _lastTintKey = tintKey;
            _renderer.color = _baseTint * GetStatusTint();
        }

        private Color GetStatusTint()
        {
            if (_buffer.IsActive(StatusEffectType.Freeze))
            {
                return FreezeTint;
            }

            if (_buffer.IsActive(StatusEffectType.Stun))
            {
                return StunTint;
            }

            if (_buffer.IsActive(StatusEffectType.Burn))
            {
                return BurnTint;
            }

            if (_buffer.IsActive(StatusEffectType.Poison))
            {
                return PoisonTint;
            }

            if (_buffer.IsActive(StatusEffectType.Cold))
            {
                return ColdTint;
            }

            if (_buffer.IsActive(StatusEffectType.Slow))
            {
                return SlowTint;
            }

            return Color.white;
        }
    }
}
