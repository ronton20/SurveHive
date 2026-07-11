using UnityEngine;

namespace SurveHive.Data
{
    /// <summary>
    /// One purchasable hero cosmetic (PLAN 5C) — purely visual. Color-slot
    /// entries carry a body tint (applied through the SpriteFlash shader's
    /// _Tint so animation clips can't clobber it); hat/stinger entries carry an
    /// overlay sprite plus its attach offset on the Body rig. An empty save
    /// slot means the default look (no tint / no overlay), so the catalog only
    /// holds the unlockable extras.
    /// </summary>
    [CreateAssetMenu(menuName = "SurveHive/Cosmetic", fileName = "Cosmetic")]
    public sealed class CosmeticSO : ScriptableObject
    {
        [SerializeField] private string _cosmeticId;
        [SerializeField] private CosmeticSlot _slot;
        [SerializeField] private string _displayName;
        [TextArea]
        [SerializeField] private string _description;
        [SerializeField] private int _jellyCost = 1;

        [Header("Color slot")]
        [SerializeField] private Color _tint = Color.white;

        [Header("Hat / Stinger slots")]
        [SerializeField] private Sprite _sprite;
        // Hats: local position on the Body child (flips with the rig's facing).
        // Stingers: where the menu preview floats the projectile.
        [SerializeField] private Vector2 _attachOffset;
        // Sorting order relative to the body renderer (+1 above, -1 behind).
        [SerializeField] private int _sortingOffset = 1;
        // Stinger slot: section header in the Hive Style grid — skins group by
        // shape, with the color variants (tint over one neutral shape sprite)
        // listed inside the section. Empty for other slots.
        [SerializeField] private string _shapeGroup;

        public string CosmeticId => _cosmeticId;

        public CosmeticSlot Slot => _slot;

        public string DisplayName => _displayName;

        public string Description => _description;

        public int JellyCost => _jellyCost;

        public Color Tint => _tint;

        public Sprite Sprite => _sprite;

        public Vector2 AttachOffset => _attachOffset;

        public int SortingOffset => _sortingOffset;

        public string ShapeGroup => _shapeGroup;
    }
}
