using System.Collections.Generic;
using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// The Hive Style customization panel (PLAN 5C), in the shop's tabbed mold:
    /// slot tabs on the left (Colors / Hats / Stingers), a live hero preview and
    /// detail pane on the right, and a scrollable grid of that slot's cosmetics.
    /// Each tab leads with a DEFAULT cell (always owned, equips the natural
    /// look); the stinger tab additionally groups its skins under one section
    /// header per shape (color variants inside, priced by shape + color). The
    /// preview always shows the **selected** entry in its slot — try-before-
    /// you-buy — with the other slots showing what's equipped. Buying spends
    /// Royal Jelly through <see cref="CosmeticShop"/> and auto-equips; equipping
    /// persists instantly via the store. Menu-only path, so refresh-time
    /// string/UI work is fine.
    /// </summary>
    public sealed class CosmeticsUI : MonoBehaviour
    {
        [SerializeField] private MetaProgressionStoreSO _store;
        [SerializeField] private CosmeticCatalogSO _catalog;
        [SerializeField] private TMP_Text _jellyText;

        [Header("Tabbed grid")]
        [SerializeField] private CosmeticEntryUI _entryPrefab;
        [SerializeField] private RectTransform _gridContent;
        [SerializeField] private ScrollRect _scrollRect;
        // Renders the per-shape section headers in the stinger tab.
        [SerializeField] private TMP_FontAsset _sectionFont;
        // Parallel arrays indexed by (int)CosmeticSlot: Color=0, Hat=1, Stinger=2.
        [SerializeField] private Button[] _tabButtons;
        [SerializeField] private Image[] _tabHighlights;
        // White square used as the swatch icon for color entries + default cells.
        [SerializeField] private Sprite _swatchSprite;

        [Header("Detail pane")]
        [SerializeField] private Image _detailIcon;
        [SerializeField] private TMP_Text _detailName;
        [SerializeField] private TMP_Text _detailDescription;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TMP_Text _actionLabel;

        [Header("Hero preview")]
        [SerializeField] private Image _previewBody;
        [SerializeField] private Image _previewHat;
        [SerializeField] private Image _previewStinger;
        // Fallback preview pixels per world unit, used only when the body
        // sprite is missing — otherwise the scale is derived from how the
        // preserve-aspect body Image actually fits its sprite, so overlays sit
        // exactly as they do on the in-run rig.
        [SerializeField] private float _previewPixelsPerUnit = 96f;

        // Section header sizing (grid cells are 124px; headers stay slim).
        private const float SectionHeaderHeight = 42f;
        private const float SectionHeaderFontSize = 26f;
        private static readonly Color SectionHeaderColor = new Color(0.961f, 0.651f, 0.137f);

        // Entry table: one default row per slot (null cosmetic) followed by the
        // catalog's rows for that slot, mirrored 1:1 by _cells.
        private readonly List<CosmeticSO> _entries = new List<CosmeticSO>();
        private readonly List<CosmeticSlot> _entrySlots = new List<CosmeticSlot>();
        // Section blocks (header + sub-grid GameObjects) toggled per tab.
        private readonly List<GameObject> _sectionBlocks = new List<GameObject>();
        private readonly List<CosmeticSlot> _sectionSlots = new List<CosmeticSlot>();
        private CosmeticEntryUI[] _cells;
        private UnityAction[] _cellHandlers;
        private UnityAction[] _tabHandlers;
        private UnityAction _actionHandler;

        private CosmeticSlot _activeSlot = CosmeticSlot.Color;
        private int _selectedEntry = -1;

        private void Awake()
        {
            BuildEntries();
            BuildCells();
            WireTabs();
            WireAction();
        }

        private void OnDestroy()
        {
            if (_cells != null)
            {
                for (int i = 0; i < _cells.Length; i++)
                {
                    if (_cells[i] != null)
                    {
                        _cells[i].Button.onClick.RemoveListener(_cellHandlers[i]);
                    }
                }
            }

            if (_tabButtons != null && _tabHandlers != null)
            {
                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    if (_tabButtons[i] != null && _tabHandlers[i] != null)
                    {
                        _tabButtons[i].onClick.RemoveListener(_tabHandlers[i]);
                    }
                }
            }

            if (_actionButton != null && _actionHandler != null)
            {
                _actionButton.onClick.RemoveListener(_actionHandler);
            }
        }

        private void OnEnable()
        {
            // OnEnable can fire before Awake on the very first activation; guard it.
            if (_cells == null)
            {
                return;
            }

            ApplySlotTab(_activeSlot);
            RefreshAll();
        }

        private void BuildEntries()
        {
            _entries.Clear();
            _entrySlots.Clear();

            CosmeticSO[] catalog = _catalog != null ? _catalog.Cosmetics : null;
            for (int slot = 0; slot < CosmeticSlots.Count; slot++)
            {
                _entries.Add(null);
                _entrySlots.Add((CosmeticSlot)slot);

                if (catalog == null)
                {
                    continue;
                }

                for (int i = 0; i < catalog.Length; i++)
                {
                    if (catalog[i] != null && (int)catalog[i].Slot == slot)
                    {
                        _entries.Add(catalog[i]);
                        _entrySlots.Add((CosmeticSlot)slot);
                    }
                }
            }
        }

        // Cells spawn into section blocks: a new block starts whenever the slot
        // or the shape-group key changes (catalog order keeps groups contiguous).
        // Colors/hats have one unnamed section each; stingers get one headed
        // section per shape, after the unnamed section holding their DEFAULT cell.
        private void BuildCells()
        {
            _sectionBlocks.Clear();
            _sectionSlots.Clear();

            if (_entryPrefab == null || _gridContent == null)
            {
                _cells = System.Array.Empty<CosmeticEntryUI>();
                _cellHandlers = System.Array.Empty<UnityAction>();
                return;
            }

            _cells = new CosmeticEntryUI[_entries.Count];
            _cellHandlers = new UnityAction[_entries.Count];

            RectTransform currentGrid = null;
            string currentKey = null;
            var currentSlot = (CosmeticSlot)(-1);

            for (int i = 0; i < _entries.Count; i++)
            {
                CosmeticSO cosmetic = _entries[i];
                CosmeticSlot slot = _entrySlots[i];
                string key = cosmetic != null && cosmetic.ShapeGroup != null ? cosmetic.ShapeGroup : string.Empty;

                if (currentGrid == null || slot != currentSlot || key != currentKey)
                {
                    currentGrid = CreateSectionBlock(slot, key);
                    currentSlot = slot;
                    currentKey = key;
                }

                CosmeticEntryUI cell = Instantiate(_entryPrefab, currentGrid);
                cell.name = cosmetic != null ? $"Cell_{cosmetic.CosmeticId}" : $"Cell_default_{slot}";
                cell.Bind(i, IconFor(cosmetic), IconTintFor(cosmetic));
                _cells[i] = cell;

                int captured = i;
                _cellHandlers[i] = () => Select(captured);
                cell.Button.onClick.AddListener(_cellHandlers[i]);
            }
        }

        private RectTransform CreateSectionBlock(CosmeticSlot slot, string header)
        {
            if (!string.IsNullOrEmpty(header))
            {
                var headerGo = new GameObject($"Header_{header}", typeof(RectTransform));
                headerGo.transform.SetParent(_gridContent, false);
                var headerText = headerGo.AddComponent<TextMeshProUGUI>();
                headerText.font = _sectionFont;
                headerText.fontSize = SectionHeaderFontSize;
                headerText.color = SectionHeaderColor;
                headerText.alignment = TextAlignmentOptions.MidlineLeft;
                headerText.raycastTarget = false;
                headerText.text = header;
                var headerLayout = headerGo.AddComponent<LayoutElement>();
                headerLayout.preferredHeight = SectionHeaderHeight;

                _sectionBlocks.Add(headerGo);
                _sectionSlots.Add(slot);
            }

            var gridGo = new GameObject($"Grid_{slot}_{header}", typeof(RectTransform));
            gridGo.transform.SetParent(_gridContent, false);
            var layout = gridGo.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(124f, 124f);
            layout.spacing = new Vector2(14f, 14f);
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperLeft;
            // A fixed column count lets the grid report a preferred height to
            // the content's vertical layout.
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;

            _sectionBlocks.Add(gridGo);
            _sectionSlots.Add(slot);
            return (RectTransform)gridGo.transform;
        }

        private Sprite IconFor(CosmeticSO cosmetic)
        {
            if (cosmetic == null || cosmetic.Slot == CosmeticSlot.Color || cosmetic.Sprite == null)
            {
                return _swatchSprite;
            }

            return cosmetic.Sprite;
        }

        // Colors show their tint on the swatch; stinger skins show their color
        // tint over the neutral shape sprite (exactly how they render in-run).
        private Color IconTintFor(CosmeticSO cosmetic)
        {
            return cosmetic != null && cosmetic.Slot != CosmeticSlot.Hat ? cosmetic.Tint : Color.white;
        }

        private void WireTabs()
        {
            if (_tabButtons == null)
            {
                return;
            }

            _tabHandlers = new UnityAction[_tabButtons.Length];
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null)
                {
                    continue;
                }

                var slot = (CosmeticSlot)i;
                _tabHandlers[i] = () => ApplySlotTab(slot);
                _tabButtons[i].onClick.AddListener(_tabHandlers[i]);
            }
        }

        private void WireAction()
        {
            if (_actionButton == null)
            {
                return;
            }

            _actionHandler = OnActionClicked;
            _actionButton.onClick.AddListener(_actionHandler);
        }

        private void ApplySlotTab(CosmeticSlot slot)
        {
            _activeSlot = slot;

            for (int i = 0; i < _sectionBlocks.Count; i++)
            {
                _sectionBlocks[i].SetActive(_sectionSlots[i] == slot);
            }

            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }

            int equippedInTab = -1;
            int firstInTab = -1;
            string equippedId = _store != null ? _store.GetEquippedCosmetic((int)slot) : string.Empty;

            for (int i = 0; i < _cells.Length; i++)
            {
                if (_entrySlots[i] != slot)
                {
                    continue;
                }

                if (firstInTab < 0)
                {
                    firstInTab = i;
                }

                if (IsEntryEquipped(i, equippedId))
                {
                    equippedInTab = i;
                }
            }

            if (_tabHighlights != null)
            {
                for (int i = 0; i < _tabHighlights.Length; i++)
                {
                    if (_tabHighlights[i] != null)
                    {
                        _tabHighlights[i].enabled = (CosmeticSlot)i == slot;
                    }
                }
            }

            // Land on the currently-equipped entry (falling back to DEFAULT)
            // unless the selection already lives in this tab.
            if (_selectedEntry < 0 || _entrySlots[_selectedEntry] != slot)
            {
                Select(equippedInTab >= 0 ? equippedInTab : firstInTab);
            }
            else
            {
                RefreshDetail();
                RefreshPreview();
            }
        }

        private bool IsEntryEquipped(int entryIndex, string equippedId)
        {
            CosmeticSO cosmetic = _entries[entryIndex];
            return cosmetic == null
                ? string.IsNullOrEmpty(equippedId)
                : cosmetic.CosmeticId == equippedId;
        }

        private void Select(int entryIndex)
        {
            _selectedEntry = entryIndex;
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].SetSelected(i == entryIndex);
            }

            RefreshDetail();
            // Try-before-you-buy: the preview wears the selection immediately.
            RefreshPreview();
        }

        private void OnActionClicked()
        {
            if (_selectedEntry < 0 || _store == null)
            {
                return;
            }

            CosmeticSO cosmetic = _entries[_selectedEntry];
            CosmeticSlot slot = _entrySlots[_selectedEntry];

            if (cosmetic == null)
            {
                CosmeticShop.TryEquip(_store, slot, string.Empty);
            }
            else if (_store.IsCosmeticOwned(cosmetic.CosmeticId))
            {
                CosmeticShop.TryEquip(_store, slot, cosmetic.CosmeticId);
            }
            else if (CosmeticShop.TryPurchase(_store, cosmetic))
            {
                // A fresh purchase is worn straight out of the shop.
                CosmeticShop.TryEquip(_store, slot, cosmetic.CosmeticId);
            }

            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_jellyText != null && _store != null)
            {
                _jellyText.text = CurrencyGlyphs.Jelly + _store.BankedJelly;
            }

            for (int i = 0; i < _cells.Length; i++)
            {
                CosmeticSO cosmetic = _entries[i];
                bool owned = cosmetic == null
                    || (_store != null && _store.IsCosmeticOwned(cosmetic.CosmeticId));
                _cells[i].SetOwned(owned);

                string equippedId = _store != null
                    ? _store.GetEquippedCosmetic((int)_entrySlots[i])
                    : string.Empty;
                _cells[i].SetEquipped(IsEntryEquipped(i, equippedId));
            }

            RefreshDetail();
            RefreshPreview();
        }

        private void RefreshDetail()
        {
            if (_selectedEntry < 0 || _detailName == null)
            {
                return;
            }

            CosmeticSO cosmetic = _entries[_selectedEntry];
            _detailName.text = cosmetic != null ? cosmetic.DisplayName : Loc.Get(LocKeys.CosmeticsDefaultName);
            if (_detailDescription != null)
            {
                _detailDescription.text = cosmetic != null
                    ? cosmetic.Description
                    : Loc.Get(LocKeys.CosmeticsDefaultDescription);
            }

            if (_detailIcon != null)
            {
                _detailIcon.sprite = IconFor(cosmetic);
                _detailIcon.color = IconTintFor(cosmetic);
                _detailIcon.enabled = _detailIcon.sprite != null;
            }

            RefreshActionButton(cosmetic);
        }

        private void RefreshActionButton(CosmeticSO cosmetic)
        {
            if (_actionButton == null || _actionLabel == null || _store == null)
            {
                return;
            }

            string equippedId = _store.GetEquippedCosmetic((int)_entrySlots[_selectedEntry]);
            bool owned = cosmetic == null || _store.IsCosmeticOwned(cosmetic.CosmeticId);

            if (IsEntryEquipped(_selectedEntry, equippedId))
            {
                _actionLabel.text = Loc.Get(LocKeys.CosmeticsEquipped);
                _actionButton.interactable = false;
            }
            else if (owned)
            {
                _actionLabel.text = Loc.Get(LocKeys.CosmeticsEquip);
                _actionButton.interactable = true;
            }
            else
            {
                _actionLabel.text = Loc.Get(LocKeys.CosmeticsBuyPrefix) + CurrencyGlyphs.Jelly + cosmetic.JellyCost;
                _actionButton.interactable = _store.BankedJelly >= cosmetic.JellyCost;
            }
        }

        // The preview wears the selected entry in its slot (even before it's
        // bought), and whatever is equipped everywhere else.
        private CosmeticSO PreviewCosmeticFor(CosmeticSlot slot)
        {
            if (_selectedEntry >= 0 && _entrySlots[_selectedEntry] == slot)
            {
                return _entries[_selectedEntry];
            }

            if (_store == null || _catalog == null)
            {
                return null;
            }

            return _catalog.FindById(_store.GetEquippedCosmetic((int)slot));
        }

        private void RefreshPreview()
        {
            if (_previewBody != null)
            {
                CosmeticSO color = PreviewCosmeticFor(CosmeticSlot.Color);
                _previewBody.color = color != null ? color.Tint : Color.white;
            }

            RefreshPreviewOverlay(_previewHat, PreviewCosmeticFor(CosmeticSlot.Hat));
            RefreshPreviewOverlay(_previewStinger, PreviewCosmeticFor(CosmeticSlot.Stinger));
        }

        private void RefreshPreviewOverlay(Image overlay, CosmeticSO cosmetic)
        {
            if (overlay == null)
            {
                return;
            }

            if (cosmetic == null || cosmetic.Sprite == null)
            {
                overlay.enabled = false;
                return;
            }

            Sprite sprite = cosmetic.Sprite;
            overlay.sprite = sprite;
            overlay.color = IconTintFor(cosmetic);
            var rect = (RectTransform)overlay.transform;
            float scale = PreviewPixelsPerUnit();
            rect.sizeDelta = sprite.rect.size / sprite.pixelsPerUnit * scale;
            rect.anchoredPosition = cosmetic.AttachOffset * scale;
            overlay.enabled = true;
        }

        // How many preview pixels one Body world unit spans: the preserve-aspect
        // body Image fits its sprite by the tighter axis.
        private float PreviewPixelsPerUnit()
        {
            if (_previewBody == null || _previewBody.sprite == null)
            {
                return _previewPixelsPerUnit;
            }

            Sprite body = _previewBody.sprite;
            Vector2 worldSize = body.rect.size / body.pixelsPerUnit;
            Vector2 rectSize = ((RectTransform)_previewBody.transform).rect.size;
            return Mathf.Min(rectSize.x / worldSize.x, rectSize.y / worldSize.y);
        }
    }
}
