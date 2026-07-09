using SurveHive.Health;
using SurveHive.Progression;
using SurveHive.View;
using UnityEngine;

namespace SurveHive.Combat.Status
{
    /// <summary>
    /// Scene-facing wrapper around a <see cref="StatusEffectBuffer"/>: ticks the
    /// buffer, deals DoT damage with colored damage numbers, feeds freeze-break
    /// damage, and drives the status readability scheme (PLAN 2A): the
    /// highest-priority effect tints the sprite via <see cref="StatusTintPalette"/>,
    /// two-plus stacked effects pulse between the top two tints, and the hit
    /// flash is hue-shifted to match so the status reads mid-flash too.
    /// Pooled-safe: fully resets in OnEnable. Zero-GC per frame.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public sealed class StatusEffectReceiver : MonoBehaviour
    {
        // The tint goes through the SpriteFlash shader's _Tint (via
        // MaterialPropertyBlock), NOT renderer.color: the rig's animation
        // clips keyframe the renderer color every frame and clobber any
        // direct write (they ate the rank tints too, silently).
        private static readonly int TintProperty = Shader.PropertyToID("_Tint");

        [SerializeField] private HealthComponent _health;
        [SerializeField] private SpriteRenderer _renderer;

        private readonly StatusEffectBuffer _buffer = new StatusEffectBuffer();
        private Color _baseTint = Color.white;
        private int _lastTintKey = -1;
        private MaterialPropertyBlock _propertyBlock;
        // Optional sibling (looked up, not serialized, so existing pooled
        // prefabs need no re-wiring): receives the status-hued flash color.
        private HitFlash _hitFlash;

        public StatusEffectBuffer Buffer => _buffer;

        public float MoveSpeedMultiplier => _buffer.MoveSpeedMultiplier;

        public bool IsAttackDisabled => _buffer.IsAttackDisabled;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            TryGetComponent(out _hitFlash);
        }

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

            // Elemental set bonuses (Phase 3C) amplify the owning element's
            // status here — the single choke point every applier flows through.
            _buffer.Apply(
                type,
                potency * ElementSets.GetStatusPotencyMultiplier(type),
                duration * ElementSets.GetStatusDurationMultiplier(type));
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
            // Burn/poison DoTs are elemental effects — always magic damage.
            _health.TakeDamage(rounded, DamageType.Magic, gameObject);
            DamagePopupSpawner.Spawn(transform.position, rounded, color, DamagePopupSpawner.DotSizeMultiplier);
        }

        private void RefreshTint()
        {
            if (_renderer == null)
            {
                return;
            }

            int activeCount = StatusTintPalette.GetTopTwoActive(
                _buffer, out StatusEffectType first, out StatusEffectType second);

            // A two-status pulse animates every frame; otherwise the tint (and
            // the flash hue) only needs rewriting when the active set changes.
            if (activeCount >= 2)
            {
                _lastTintKey = -1;
                Color firstTint = StatusTintPalette.GetTint(first);
                WriteTint(StatusTintPalette.GetSpriteColor(
                    _baseTint,
                    StatusTintPalette.GetPulsedTint(firstTint, StatusTintPalette.GetTint(second), Time.time)));
                // Flash keeps the dominant status hue (no-op while unchanged).
                PushFlashColor(StatusTintPalette.GetFlashColor(firstTint));
                return;
            }

            int tintKey = activeCount == 0 ? 0 : 1 + (int)first;
            if (tintKey == _lastTintKey)
            {
                return;
            }

            _lastTintKey = tintKey;
            if (activeCount == 0)
            {
                WriteTint(_baseTint);
                PushFlashColor(Color.white);
            }
            else
            {
                Color tint = StatusTintPalette.GetTint(first);
                WriteTint(StatusTintPalette.GetSpriteColor(_baseTint, tint));
                PushFlashColor(StatusTintPalette.GetFlashColor(tint));
            }
        }

        private void WriteTint(Color color)
        {
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(TintProperty, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        private void PushFlashColor(Color color)
        {
            if (_hitFlash != null)
            {
                _hitFlash.SetFlashColor(color);
            }
        }
    }
}
