using UnityEngine;

namespace SurveHive.Data
{
    /// <summary>
    /// The ordered list of every permanent shop upgrade. The Hive Upgrades shop
    /// reads this to build its grid at runtime — add an upgrade by dropping its
    /// <see cref="MetaUpgradeSO"/> into this asset, no scene or builder edits.
    /// </summary>
    [CreateAssetMenu(menuName = "SurveHive/Meta Upgrade Catalog", fileName = "MetaUpgradeCatalog")]
    public sealed class MetaUpgradeCatalogSO : ScriptableObject
    {
        [SerializeField] private MetaUpgradeSO[] _upgrades;

        public MetaUpgradeSO[] Upgrades => _upgrades;
    }
}
