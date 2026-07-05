using UnityEngine;

namespace SurveHive.Combat.Status
{
    /// <summary>
    /// Fixed-size status-effect state for one entity: one slot per effect type,
    /// zero allocations after construction. Pure logic (no scene dependencies)
    /// so EditMode tests can cover the stacking/expiry/diminishing-returns math.
    ///
    /// Per-effect rules (PLAN.md §4.1):
    /// - Burn: DoT, re-application refreshes duration (strongest potency wins).
    /// - Poison: DoT, re-application adds a stack (capped) and refreshes duration;
    ///   damage per second = potency * stacks.
    /// - Slow: move-speed multiplier, strongest slow and longest duration win.
    /// - Freeze: hard stop; breaks early once damage taken while frozen reaches
    ///   the applied potency (= break threshold).
    /// - Stun: full stop + no attack; with diminishing returns enabled (elites+),
    ///   each re-application inside the window halves the applied duration.
    /// </summary>
    public sealed class StatusEffectBuffer
    {
        public const int EffectTypeCount = 6;
        public const float DotTickInterval = 0.5f;
        public const int PoisonMaxStacks = 5;
        public const float StunDiminishWindowSeconds = 6f;
        public const float StunDiminishFactor = 0.5f;

        private struct Slot
        {
            public bool Active;
            public float Remaining;
            public float Potency;
            public int Stacks;
            public float TickTimer;
        }

        private readonly Slot[] _slots = new Slot[EffectTypeCount];

        private bool _diminishingStuns;
        private int _recentStunCount;
        private float _stunWindowRemaining;
        private float _freezeDamageTaken;
        private float _pendingBurnDamage;
        private float _pendingPoisonDamage;

        public bool IsActive(StatusEffectType type) => _slots[(int)type].Active;

        public float GetRemaining(StatusEffectType type)
        {
            ref readonly Slot slot = ref _slots[(int)type];
            return slot.Active ? slot.Remaining : 0f;
        }

        public int GetStacks(StatusEffectType type)
        {
            ref readonly Slot slot = ref _slots[(int)type];
            return slot.Active ? slot.Stacks : 0;
        }

        public float GetPotency(StatusEffectType type)
        {
            ref readonly Slot slot = ref _slots[(int)type];
            return slot.Active ? slot.Potency : 0f;
        }

        public bool AnyActive
        {
            get
            {
                for (int i = 0; i < EffectTypeCount; i++)
                {
                    if (_slots[i].Active)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool IsImmobilized =>
            _slots[(int)StatusEffectType.Freeze].Active || _slots[(int)StatusEffectType.Stun].Active;

        // Stun = "full stop, no attack"; a frozen enemy can't attack either.
        public bool IsAttackDisabled => IsImmobilized;

        public float MoveSpeedMultiplier
        {
            get
            {
                if (IsImmobilized)
                {
                    return 0f;
                }

                // Strongest of the generic Slow and the frost Cold wins.
                float potency = 0f;
                ref readonly Slot slow = ref _slots[(int)StatusEffectType.Slow];
                if (slow.Active)
                {
                    potency = slow.Potency;
                }

                ref readonly Slot cold = ref _slots[(int)StatusEffectType.Cold];
                if (cold.Active)
                {
                    potency = Mathf.Max(potency, cold.Potency);
                }

                return potency > 0f ? Mathf.Clamp01(1f - potency) : 1f;
            }
        }

        public void SetDiminishingStuns(bool enabled)
        {
            _diminishingStuns = enabled;
        }

        public void Reset()
        {
            for (int i = 0; i < EffectTypeCount; i++)
            {
                _slots[i] = default;
            }

            _recentStunCount = 0;
            _stunWindowRemaining = 0f;
            _freezeDamageTaken = 0f;
            _pendingBurnDamage = 0f;
            _pendingPoisonDamage = 0f;
        }

        public void Apply(StatusEffectType type, float potency, float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            ref Slot slot = ref _slots[(int)type];

            switch (type)
            {
                case StatusEffectType.Burn:
                    slot.Potency = slot.Active ? Mathf.Max(slot.Potency, potency) : potency;
                    slot.Remaining = duration;
                    slot.Stacks = 1;
                    if (!slot.Active)
                    {
                        slot.TickTimer = DotTickInterval;
                    }

                    slot.Active = true;
                    break;

                case StatusEffectType.Poison:
                    slot.Stacks = slot.Active ? Mathf.Min(slot.Stacks + 1, PoisonMaxStacks) : 1;
                    slot.Potency = slot.Active ? Mathf.Max(slot.Potency, potency) : potency;
                    slot.Remaining = duration;
                    if (!slot.Active)
                    {
                        slot.TickTimer = DotTickInterval;
                    }

                    slot.Active = true;
                    break;

                case StatusEffectType.Slow:
                case StatusEffectType.Cold:
                    slot.Potency = slot.Active ? Mathf.Max(slot.Potency, potency) : potency;
                    slot.Remaining = Mathf.Max(slot.Active ? slot.Remaining : 0f, duration);
                    slot.Stacks = 1;
                    slot.Active = true;
                    break;

                case StatusEffectType.Freeze:
                    slot.Potency = potency;
                    slot.Remaining = duration;
                    slot.Stacks = 1;
                    slot.Active = true;
                    _freezeDamageTaken = 0f;
                    break;

                case StatusEffectType.Stun:
                    float effectiveDuration = duration;
                    if (_diminishingStuns)
                    {
                        for (int i = 0; i < _recentStunCount; i++)
                        {
                            effectiveDuration *= StunDiminishFactor;
                        }
                    }

                    _recentStunCount++;
                    _stunWindowRemaining = StunDiminishWindowSeconds;

                    slot.Remaining = Mathf.Max(slot.Active ? slot.Remaining : 0f, effectiveDuration);
                    slot.Potency = 0f;
                    slot.Stacks = 1;
                    slot.Active = true;
                    break;
            }
        }

        /// <summary>Feed damage taken by the owner so an active Freeze can break early.</summary>
        public void NotifyDamageTaken(float amount)
        {
            ref Slot freeze = ref _slots[(int)StatusEffectType.Freeze];
            if (!freeze.Active || amount <= 0f)
            {
                return;
            }

            _freezeDamageTaken += amount;
            if (_freezeDamageTaken >= freeze.Potency)
            {
                freeze.Active = false;
            }
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            if (_stunWindowRemaining > 0f)
            {
                _stunWindowRemaining -= deltaTime;
                if (_stunWindowRemaining <= 0f)
                {
                    _recentStunCount = 0;
                }
            }

            TickDot(ref _slots[(int)StatusEffectType.Burn], 1, deltaTime, ref _pendingBurnDamage);
            TickDot(ref _slots[(int)StatusEffectType.Poison], _slots[(int)StatusEffectType.Poison].Stacks, deltaTime, ref _pendingPoisonDamage);
            TickDuration(ref _slots[(int)StatusEffectType.Slow], deltaTime);
            TickDuration(ref _slots[(int)StatusEffectType.Cold], deltaTime);
            TickDuration(ref _slots[(int)StatusEffectType.Freeze], deltaTime);
            TickDuration(ref _slots[(int)StatusEffectType.Stun], deltaTime);
        }

        /// <summary>
        /// Drains DoT damage accumulated by <see cref="Tick"/> since the last call.
        /// Returns true when there is any damage to deal this frame.
        /// </summary>
        public bool TryConsumeTickDamage(out float burnDamage, out float poisonDamage)
        {
            burnDamage = _pendingBurnDamage;
            poisonDamage = _pendingPoisonDamage;
            _pendingBurnDamage = 0f;
            _pendingPoisonDamage = 0f;
            return burnDamage > 0f || poisonDamage > 0f;
        }

        private static void TickDot(ref Slot slot, int stacks, float deltaTime, ref float pendingDamage)
        {
            if (!slot.Active)
            {
                return;
            }

            slot.Remaining -= deltaTime;
            slot.TickTimer -= deltaTime;
            while (slot.TickTimer <= 0f)
            {
                pendingDamage += slot.Potency * stacks * DotTickInterval;
                slot.TickTimer += DotTickInterval;
            }

            if (slot.Remaining <= 0f)
            {
                slot.Active = false;
            }
        }

        private static void TickDuration(ref Slot slot, float deltaTime)
        {
            if (!slot.Active)
            {
                return;
            }

            slot.Remaining -= deltaTime;
            if (slot.Remaining <= 0f)
            {
                slot.Active = false;
            }
        }
    }
}
