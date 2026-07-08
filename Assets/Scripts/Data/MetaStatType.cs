namespace SurveHive.Data
{
    /// <summary>
    /// Which permanent player stat a meta upgrade raises. Serialized by value
    /// in `.asset` files — append-only, never insert or reorder.
    /// </summary>
    public enum MetaStatType
    {
        MaxHealth = 0,          // flat HP per rank
        AttackDamage = 1,       // flat damage per rank
        MoveSpeed = 2,          // percent per rank
        AttackSpeed = 3,        // percent per rank
        MagnetRadius = 4,       // percent per rank
        CurrencyGain = 5,       // percent per rank
        // Phase 1C shop expansion:
        ExpGain = 6,            // percent per rank
        AbilityPower = 7,       // percent per rank
        CooldownReduction = 8,  // percent per rank (active-skill cooldowns)
        CritChance = 9,         // percent points per rank (stacks on the 0% base)
        CritDamage = 10,        // percent per rank (on the 1.5x crit multiplier)
        ItemDropRate = 11,      // percent per rank (multiplies drop-table rolls)
        Rerolls = 12,           // flat per-run reroll stock per rank (LevelUpUIController)
    }
}
