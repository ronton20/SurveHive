using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// One codex grid cell (PLAN 5A): entry icon on a selectable button with a
    /// gold selection frame, in the <see cref="MetaShopIconUI"/> mold. Locked
    /// entries render their icon as a black silhouette until discovered.
    /// Spawned and driven by <see cref="CodexUI"/>; menu-only path.
    /// </summary>
    public sealed class CodexEntryUI : MonoBehaviour
    {
        private static readonly Color SilhouetteColor = new Color(0.05f, 0.04f, 0.03f, 0.9f);

        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _button;
        [SerializeField] private Image _selectionHighlight;

        private Color _unlockedTint = Color.white;

        public Button Button => _button;

        /// <summary>Index into <see cref="CodexUI"/>'s entry table.</summary>
        public int EntryIndex { get; private set; }

        public void Bind(int entryIndex, Sprite icon, Color unlockedTint)
        {
            EntryIndex = entryIndex;
            _unlockedTint = unlockedTint;
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
            }
        }

        public void SetUnlocked(bool unlocked)
        {
            if (_iconImage != null)
            {
                _iconImage.color = unlocked ? _unlockedTint : SilhouetteColor;
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
