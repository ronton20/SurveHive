namespace SurveHive.Enemies
{
    public enum RangedSteerMode
    {
        Chase,
        Hold,
        Flee,
    }

    /// <summary>
    /// Pure steering decision for ranged enemies: flee when the player crowds
    /// in, chase when they escape the firing band, hold (orbit) in between.
    /// Squared distances so callers never pay a square root.
    /// </summary>
    public static class RangedSteering
    {
        public static RangedSteerMode Decide(float distanceSqr, float fleeRangeSqr, float chaseRangeSqr)
        {
            if (distanceSqr < fleeRangeSqr)
            {
                return RangedSteerMode.Flee;
            }

            return distanceSqr > chaseRangeSqr ? RangedSteerMode.Chase : RangedSteerMode.Hold;
        }
    }
}
