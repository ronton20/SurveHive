namespace SurveHive.Combat.Status
{
    // Values index into StatusEffectBuffer's fixed slot array — keep contiguous
    // from 0 and only append (serialized in skill/data assets by integer).
    public enum StatusEffectType
    {
        Burn = 0,
        Poison = 1,
        Slow = 2,
        Freeze = 3,
        Stun = 4
    }
}
