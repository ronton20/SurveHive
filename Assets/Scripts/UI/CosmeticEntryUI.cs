using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// One Hive Style grid cell (PLAN 5C), in the <see cref="CodexEntryUI"/>
    /// mold: cosmetic icon on a selectable button with a gold selection frame
    /// plus a small badge on the equipped entry. Color cosmetics show a tinted
    /// swatch; hats/stingers show their sprite. Unowned entries render dimmed
    /// (still recognizable — you're shopping, not discovering). Spawned and
    /// driven by <see cref="CosmeticsUI"/>; menu-only path.
    /// </summary>
    public sealed class CosmeticEntryUI : MonoBehaviour
    {
        private static readonly Color UnownedTint = new Color(0.45f, 0.45f, 0.45f, 1f);

        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _button;
        [SerializeField] private Image _selectionHighlight;
        [SerializeField] private Image _equippedBadge;

        private Color _ownedTint = Color.white;

        public Button Button => _button;

        /// <summary>Index into <see cref="CosmeticsUI"/>'s entry table.</summary>
        public int EntryIndex { get; private set; }

        public void Bind(int entryIndex, Sprite icon, Color ownedTint)
        {
            EntryIndex = entryIndex;
            _ownedTint = ownedTint;
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
            }
        }

        public void SetOwned(bool owned)
        {
            if (_iconImage != null)
            {
                _iconImage.color = owned ? _ownedTint : UnownedTint * _ownedTint;
            }
        }

        public void SetEquipped(bool equipped)
        {
            if (_equippedBadge != null)
            {
                _equippedBadge.enabled = equipped;
            }
        }

        public void SetSelected(bool selected)
        {
            if (_selectionHighlight != null)
            {
                _selectionHighlight.enabled = selected;
            }
        }
    }
}
