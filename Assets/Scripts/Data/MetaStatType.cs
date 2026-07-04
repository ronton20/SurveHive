namespace SurveHive.Data
{
    /// <summary>Which permanent player stat a meta upgrade raises.</summary>
    public enum MetaStatType
    {
        MaxHealth = 0,      // flat HP per rank
        AttackDamage = 1,   // percent per rank
        MoveSpeed = 2,      // percent per rank
        AttackSpeed = 3,    // percent per rank
        MagnetRadius = 4,   // percent per rank
        CurrencyGain = 5,   // percent per rank
    }
}
