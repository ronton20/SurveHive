namespace SurveHive.Combat
{
    /// <summary>
    /// Per-shot configuration the basic attack hands each <see cref="Projectile"/>
    /// (Combat 2.0 Enhancement lane). A value type built once per volley in
    /// <c>AutoAttack</c> and copied into the projectile's own mutable state on
    /// launch (bounce falloffs mutate the projectile's copy, not this).
    /// </summary>
    public struct BasicAttackPayload
    {
        public float Damage;
        public float Speed;
        public float Range;

        // Piercing Stinger: enemies passed through before the shot expires.
        public int Pierce;

        // Burning Stinger (fire DoT).
        public float BurnChance;
        public float BurnDps;
        public float BurnDuration;

        // Poison Stinger (poison DoT).
        public float PoisonChance;
        public float PoisonDps;
        public float PoisonDuration;

        // Frost Stinger (chance to freeze).
        public float FreezeChance;
        public float FreezeThreshold;
        public float FreezeDuration;

        // Shock Stinger (chance to bounce to another enemy, with falloff).
        public float BounceChance;
        public int BounceCount;
        public float BounceRange;
        public float BounceDamageFalloff;
        public float BounceChanceFalloff;
    }
}
