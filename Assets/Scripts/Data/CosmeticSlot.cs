namespace SurveHive.Data
{
    /// <summary>
    /// Where a cosmetic attaches on the hero (PLAN 5C). Values index the save's
    /// equipped-cosmetic array — append new slots, never reorder.
    /// </summary>
    public enum CosmeticSlot
    {
        Color = 0,
        Hat = 1,
        Stinger = 2,
    }

    public static class CosmeticSlots
    {
        public const int Count = 3;
    }
}
