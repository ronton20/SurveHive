using UnityEngine;

namespace SurveHive.Data
{
    /// <summary>
    /// The full cosmetic roster (PLAN 5C), authored by CosmeticsBuilder. Both
    /// the menu panel and the run-time <c>CosmeticApplier</c> resolve equipped
    /// ids against this. Menu/spawn path only — linear lookups are fine.
    /// </summary>
    [CreateAssetMenu(menuName = "SurveHive/Cosmetic Catalog", fileName = "CosmeticCatalog")]
    public sealed class CosmeticCatalogSO : ScriptableObject
    {
        [SerializeField] private CosmeticSO[] _cosmetics = new CosmeticSO[0];

        public CosmeticSO[] Cosmetics => _cosmetics;

        public CosmeticSO FindById(string cosmeticId)
        {
            if (string.IsNullOrEmpty(cosmeticId) || _cosmetics == null)
            {
                return null;
            }

            for (int i = 0; i < _cosmetics.Length; i++)
            {
                if (_cosmetics[i] != null && _cosmetics[i].CosmeticId == cosmeticId)
                {
                    return _cosmetics[i];
                }
            }

            return null;
        }
    }
}
