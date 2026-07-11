using System.Text;
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
    /// The main-menu codex panel (PLAN 5A), in the <see cref="MetaShopUI"/>
    /// tabbed mold: category tabs on the left (Power-Ups / Sets / Enemies /
    /// Items), a read-only detail pane, and a scrollable, **sectioned** entry
    /// grid — power-ups group by lane (Passives / Enhancements / Abilities),
    /// enemies by world. Entries the player hasn't encountered yet show as
    /// black silhouettes with a "???" detail; discovered power-ups list what
    /// every level grants (via <see cref="CodexSkillLevels"/>) and discovered
    /// enemies describe their behavior (numbers scale with difficulty/time, so
    /// none are shown). Discovery state comes from the persistent store,
    /// written during runs by <see cref="CodexTracker"/>. Sections and cells
    /// spawn once at startup; menu-only path, so the string work here is fine.
    /// </summary>
    public sealed class CodexUI : MonoBehaviour
    {
        public const int CategoryCount = 4;
        public const int CategoryPowerUps = 0;
        public const int CategorySets = 1;
        public const int CategoryEnemies = 2;
        public const int CategoryItems = 3;

        // Section sub-grid geometry (mirrors the shop/codex icon grids).
        private const int GridColumns = 7;
        private const float GridCell = 124f;
        private const float GridSpacing = 12f;
        private static readonly Color HeaderAmber = new Color(0.961f, 0.651f, 0.137f);

        [SerializeField] private MetaProgressionStoreSO _store;
        [SerializeField] private CodexCatalogSO _catalog;

        [Header("Tabbed layout")]
        [SerializeField] private CodexEntryUI _entryPrefab;
        // The scroll content sections + cells spawn under.
        [SerializeField] private RectTransform _gridContent;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private TMP_FontAsset _sectionFont;
        // Parallel arrays indexed by category.
        [SerializeField] private Button[] _tabButtons;
        [SerializeField] private Image[] _tabHighlights;

        [Header("Detail pane")]
        [SerializeField] private Image _detailIcon;
        [SerializeField] private TMP_Text _detailName;
        [SerializeField] private TMP_Text _detailDescription;
        [SerializeField] private TMP_Text _progressText;

        // Stand-in glyph for set-bonus entries (tinted per element).
        [SerializeField] private Sprite _setIcon;

        private struct Entry
        {
            public string Id;
            public int Category;
            // Section header this entry files under (null/empty = headerless).
            public string Section;
            public string DisplayName;
            public string Description;
            public Sprite Icon;
            public Color Tint;
            public bool Unlocked;
        }

        private Entry[] _entries;
        private CodexEntryUI[] _cells;
        private UnityAction[] _cellHandlers;
        private UnityAction[] _tabHandlers;
        // Section blocks (header and/or sub-grid roots) with their category,
        // toggled wholesale on tab switch.
        private readonly System.Collections.Generic.List<GameObject> _sectionBlocks =
            new System.Collections.Generic.List<GameObject>();
        private readonly System.Collections.Generic.List<int> _sectionBlockCategories =
            new System.Collections.Generic.List<int>();
        private int _activeCategory = CategoryPowerUps;
        private int _selectedIndex = -1;

        private void Awake()
        {
            BuildEntries();
            SpawnCells();
            WireTabs();
        }

        private void OnEnable()
        {
            // OnEnable can fire before Awake on the very first activation; guard it.
            if (_entries == null)
            {
                return;
            }

            RefreshUnlocks();
            ApplyCategory(_activeCategory);
        }

        private void OnDestroy()
        {
            if (_cells != null)
            {
                for (int i = 0; i < _cells.Length; i++)
                {
                    if (_cells[i] != null && _cells[i].Button != null)
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
        }

        // ------------------------------------------------------------------
        // Entry table: flattened from the catalog once at startup, in section
        // order (lane by lane, world by world) so cells spawn contiguously.
        // ------------------------------------------------------------------
        private void BuildEntries()
        {
            var builder = new StringBuilder(512);
            SkillDefinitionSO[] skills = CatalogSkills();
            SetBonusSO[] sets = CatalogSets();
            CodexCatalogSO.EnemyGroup[] enemyGroups = _catalog != null && _catalog.EnemyGroups != null
                ? _catalog.EnemyGroups
                : System.Array.Empty<CodexCatalogSO.EnemyGroup>();
            CodexCatalogSO.ItemEntry[] items = _catalog != null && _catalog.Items != null
                ? _catalog.Items
                : System.Array.Empty<CodexCatalogSO.ItemEntry>();

            var list = new System.Collections.Generic.List<Entry>(64);

            // Power-ups, one section per lane.
            AddSkillLane(list, skills, builder, PowerUpLane.Passive, Loc.Get(LocKeys.CodexSectionPassives));
            AddSkillLane(list, skills, builder, PowerUpLane.Enhancement, Loc.Get(LocKeys.CodexSectionEnhancements));
            AddSkillLane(list, skills, builder, PowerUpLane.Ability, Loc.Get(LocKeys.CodexSectionAbilities));

            for (int i = 0; i < sets.Length; i++)
            {
                SetBonusSO set = sets[i];
                if (set == null)
                {
                    continue;
                }

                list.Add(new Entry
                {
                    Id = CodexIds.ForSet(set.Element),
                    Category = CategorySets,
                    DisplayName = set.SetName,
                    Description = BuildSetDescription(set, builder),
                    Icon = _setIcon,
                    Tint = ElementPalette.GetColor(set.Element),
                });
            }

            // Enemies, one section per world.
            for (int g = 0; g < enemyGroups.Length; g++)
            {
                EnemyStatsSO[] enemies = enemyGroups[g].Enemies;
                for (int i = 0; enemies != null && i < enemies.Length; i++)
                {
                    EnemyStatsSO enemy = enemies[i];
                    if (enemy == null)
                    {
                        continue;
                    }

                    list.Add(new Entry
                    {
                        Id = CodexIds.ForEnemy(enemy),
                        Category = CategoryEnemies,
                        Section = enemyGroups[g].WorldName,
                        DisplayName = enemy.DisplayName,
                        Description = enemy.CodexDescription,
                        Icon = EnemySprite(enemy),
                        Tint = enemy.SpriteTint,
                    });
                }
            }

            for (int i = 0; i < items.Length; i++)
            {
                CodexCatalogSO.ItemEntry item = items[i];
                list.Add(new Entry
                {
                    Id = CodexIds.ForItem(item.Type),
                    Category = CategoryItems,
                    DisplayName = item.DisplayName,
                    Description = item.Description,
                    Icon = item.Icon,
                    Tint = Color.white,
                });
            }

            _entries = list.ToArray();
        }

        private void AddSkillLane(
            System.Collections.Generic.List<Entry> list, SkillDefinitionSO[] skills,
            StringBuilder builder, PowerUpLane lane, string sectionName)
        {
            for (int i = 0; i < skills.Length; i++)
            {
                SkillDefinitionSO skill = skills[i];
                if (skill == null || skill.Lane != lane)
                {
                    continue;
                }

                list.Add(new Entry
                {
                    Id = CodexIds.ForSkill(skill),
                    Category = CategoryPowerUps,
                    Section = sectionName,
                    DisplayName = skill.DisplayName,
                    Description = BuildSkillDescription(skill, builder),
                    Icon = skill.Icon,
                    Tint = Color.white,
                });
            }
        }

        private SkillDefinitionSO[] CatalogSkills()
        {
            return _catalog != null && _catalog.SkillDatabase != null
                && _catalog.SkillDatabase.Skills != null
                    ? _catalog.SkillDatabase.Skills
                    : System.Array.Empty<SkillDefinitionSO>();
        }

        private SetBonusSO[] CatalogSets()
        {
            return _catalog != null && _catalog.SkillDatabase != null
                && _catalog.SkillDatabase.SetBonuses != null
                    ? _catalog.SkillDatabase.SetBonuses
                    : System.Array.Empty<SetBonusSO>();
        }

        // Blurb + the full per-level breakdown (shown once discovered).
        private static string BuildSkillDescription(SkillDefinitionSO skill, StringBuilder builder)
        {
            builder.Length = 0;
            builder.Append(skill.Description);
            builder.Append("\n\n");
            CodexSkillLevels.AppendLevels(builder, skill);
            return builder.ToString();
        }

        private static string BuildSetDescription(SetBonusSO set, StringBuilder builder)
        {
            builder.Length = 0;
            for (int i = 0; i < set.TierCount; i++)
            {
                SetBonusTier tier = set.GetTier(i);
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(tier.PiecesRequired);
                builder.Append(Loc.Get(LocKeys.SetPiecesSuffix));
                builder.Append(" — ");
                builder.Append(tier.Description);
            }

            if (!string.IsNullOrEmpty(set.SignatureDescription))
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(set.SignatureDescription);
            }

            return builder.ToString();
        }

        // Menu-path prefab peek: the enemy's world sprite doubles as its portrait.
        private static Sprite EnemySprite(EnemyStatsSO enemy)
        {
            if (enemy.Prefab == null)
            {
                return null;
            }

            var renderer = enemy.Prefab.GetComponentInChildren<SpriteRenderer>(true);
            return renderer != null ? renderer.sprite : null;
        }

        // ------------------------------------------------------------------
        // Cells + section blocks + tabs.
        // ------------------------------------------------------------------
        private void SpawnCells()
        {
            bool canSpawn = _entryPrefab != null && _gridContent != null && _entries.Length > 0;
            if (!canSpawn)
            {
                _cells = System.Array.Empty<CodexEntryUI>();
                _cellHandlers = System.Array.Empty<UnityAction>();
                return;
            }

            _cells = new CodexEntryUI[_entries.Length];
            _cellHandlers = new UnityAction[_entries.Length];

            int currentCategory = -1;
            string currentSection = null;
            RectTransform currentGrid = null;

            for (int i = 0; i < _entries.Length; i++)
            {
                bool newBlock = currentGrid == null
                    || _entries[i].Category != currentCategory
                    || _entries[i].Section != currentSection;
                if (newBlock)
                {
                    currentCategory = _entries[i].Category;
                    currentSection = _entries[i].Section;

                    if (!string.IsNullOrEmpty(currentSection))
                    {
                        RegisterBlock(CreateSectionHeader(currentSection), currentCategory);
                    }

                    currentGrid = CreateSectionGrid(currentCategory, currentSection);
                    RegisterBlock(currentGrid.gameObject, currentCategory);
                }

                CodexEntryUI cell = Instantiate(_entryPrefab, currentGrid);
                cell.name = "Entry_" + _entries[i].Id;
                cell.Bind(i, _entries[i].Icon, _entries[i].Tint);
                _cells[i] = cell;

                int captured = i;
                _cellHandlers[i] = () => Select(captured);
                cell.Button.onClick.AddListener(_cellHandlers[i]);
            }
        }

        private void RegisterBlock(GameObject block, int category)
        {
            _sectionBlocks.Add(block);
            _sectionBlockCategories.Add(category);
        }

        private GameObject CreateSectionHeader(string label)
        {
            var headerGo = new GameObject("Section_" + label, typeof(RectTransform));
            headerGo.transform.SetParent(_gridContent, false);

            var text = headerGo.AddComponent<TextMeshProUGUI>();
            if (_sectionFont != null)
            {
                text.font = _sectionFont;
            }

            text.fontSize = 28f;
            text.color = HeaderAmber;
            text.alignment = TextAlignmentOptions.BottomLeft;
            text.raycastTarget = false;
            text.text = label;

            var element = headerGo.AddComponent<LayoutElement>();
            element.minHeight = 44f;
            element.preferredHeight = 44f;
            return headerGo;
        }

        private RectTransform CreateSectionGrid(int category, string section)
        {
            var gridGo = new GameObject(
                string.IsNullOrEmpty(section) ? "Grid_" + category : "Grid_" + section,
                typeof(RectTransform));
            gridGo.transform.SetParent(_gridContent, false);

            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(GridCell, GridCell);
            grid.spacing = new Vector2(GridSpacing, GridSpacing);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GridColumns;

            return (RectTransform)gridGo.transform;
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

                int captured = i;
                _tabHandlers[i] = () => ApplyCategory(captured);
                _tabButtons[i].onClick.AddListener(_tabHandlers[i]);
            }
        }

        private void RefreshUnlocks()
        {
            int unlocked = 0;
            for (int i = 0; i < _entries.Length; i++)
            {
                _entries[i].Unlocked = _store != null && _store.IsCodexUnlocked(_entries[i].Id);
                if (_entries[i].Unlocked)
                {
                    unlocked++;
                }

                if (_cells.Length > i && _cells[i] != null)
                {
                    _cells[i].SetUnlocked(_entries[i].Unlocked);
                }
            }

            if (_progressText != null)
            {
                _progressText.text =
                    Loc.Get(LocKeys.CodexDiscoveredPrefix) + unlocked + "/" + _entries.Length;
            }
        }

        private void ApplyCategory(int category)
        {
            _activeCategory = category;

            // Whole section blocks (headers + sub-grids) toggle; cells ride
            // along with their parent grid.
            for (int i = 0; i < _sectionBlocks.Count; i++)
            {
                _sectionBlocks[i].SetActive(_sectionBlockCategories[i] == category);
            }

            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }

            if (_tabHighlights != null)
            {
                for (int i = 0; i < _tabHighlights.Length; i++)
                {
                    if (_tabHighlights[i] != null)
                    {
                        _tabHighlights[i].enabled = i == category;
                    }
                }
            }

            // Keep the selection if it belongs to this tab, else fall to the
            // tab's first entry.
            if (_selectedIndex < 0 || _entries[_selectedIndex].Category != category)
            {
                Select(FirstEntryInCategory(category));
            }
            else
            {
                RefreshDetail();
            }
        }

        private int FirstEntryInCategory(int category)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Category == category)
                {
                    return i;
                }
            }

            return -1;
        }

        private void Select(int entryIndex)
        {
            _selectedIndex = entryIndex;
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].SetSelected(i == entryIndex);
            }

            RefreshDetail();
        }

        private void RefreshDetail()
        {
            bool hasSelection = _selectedIndex >= 0 && _selectedIndex < _entries.Length;
            if (!hasSelection)
            {
                if (_detailIcon != null)
                {
                    _detailIcon.enabled = false;
                }

                if (_detailName != null)
                {
                    _detailName.text = string.Empty;
                }

                if (_detailDescription != null)
                {
                    _detailDescription.text = string.Empty;
                }

                return;
            }

            Entry entry = _entries[_selectedIndex];
            if (_detailIcon != null)
            {
                _detailIcon.sprite = entry.Icon;
                _detailIcon.enabled = entry.Icon != null;
                _detailIcon.color = entry.Unlocked
                    ? entry.Tint
                    : new Color(0.05f, 0.04f, 0.03f, 0.9f);
            }

            if (_detailName != null)
            {
                _detailName.text = entry.Unlocked
                    ? entry.DisplayName
                    : Loc.Get(LocKeys.CodexUnknownName);
            }

            if (_detailDescription != null)
            {
                _detailDescription.text = entry.Unlocked
                    ? entry.Description
                    : Loc.Get(LocKeys.CodexUnknownDescription);
            }
        }
    }
}
