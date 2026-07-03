using UnityEngine;

namespace SurveHive.Core
{
    /// <summary>
    /// Marks a serialized field as visible-but-not-editable in the Inspector.
    /// Useful for surfacing live runtime values (e.g. current health) without
    /// letting them be hand-edited. Requires the matching editor drawer.
    /// </summary>
    public sealed class ReadOnlyAttribute : PropertyAttribute
    {
    }
}
