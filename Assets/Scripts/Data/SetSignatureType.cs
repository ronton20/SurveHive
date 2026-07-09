namespace SurveHive.Data
{
    /// <summary>
    /// The signature effect an element's set grants at its top (4-piece) tier
    /// (PLAN 2B / TODO #27) — one build-defining payoff per element on top of
    /// the potency/duration scaling. Serialized by integer index on
    /// <see cref="SetBonusSO"/> assets — append new signatures at the end,
    /// never insert or reorder.
    /// </summary>
    public enum SetSignatureType
    {
        None = 0,
        // Fire: a burning enemy's death spreads Burn to a nearby enemy.
        BurnSpread = 1,
        // Frost: a chilled/frozen enemy shatters for AoE magic damage on death.
        FrostShatter = 2,
        // Electric: a stunned enemy's death arcs the Stun to a nearby enemy.
        StunChain = 3,
        // Poison: a poisoned enemy's death leaves a lingering toxic pool.
        PoisonPool = 4,
        // Honey: a slowed enemy's death leaves a sticky slow zone.
        HoneySlick = 5,
        // Physical: basic attacks execute enemies below a health threshold.
        Execute = 6
    }
}
