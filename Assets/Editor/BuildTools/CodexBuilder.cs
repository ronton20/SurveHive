using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Pickups;
using SurveHive.Progression;
using SurveHive.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// PLAN 5A — the Codex. Four passes, all find-or-create/idempotent:
    /// (1) author the <see cref="CodexCatalogSO"/> (skill database + the authored
    /// enemy stats + item-drop display entries), (2) (re)build the
    /// <c>CodexEntryIcon</c> grid-cell prefab, (3) build the MainMenu CodexPanel
    /// (tab column / detail pane / icon grid in the shop's tabbed mold), add the
    /// CODEX home button, and rewire <see cref="MainMenuController"/> +
    /// <see cref="CodexUI"/>, (4) drop a <see cref="CodexTracker"/> into the
    /// Beehive scene wired to the persistent store. Rebuilds only its own
    /// generated nodes; tuned data assets are never touched.
    /// </summary>
    public static class CodexBuilder
    {
        private const string CatalogPath = "Assets/Data/Progression/CodexCatalog.asset";
        private const string EntryPrefabPath = "Assets/Prefabs/UI/CodexEntryIcon.prefab";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string RunScenePath = "Assets/Scenes/Beehive.unity";
        private const string SkillDatabasePath = "Assets/Data/Skills/SkillDatabase.asset";
        private const string EnemiesFolder = "Assets/Data/Enemies";
        private const string PersistentStorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";
        private const string PictoFolder = "Assets/ThirdParty/IconsTemp/Icons/PictoIcon_128";

        // Honey/hive palette (mirrors Phase4MetaAndMenusBuilder).
        private static readonly Color HoneyGold = new Color(1f, 0.765f, 0.043f);
        private static readonly Color Amber = new Color(0.961f, 0.651f, 0.137f);
        private static readonly Color Wax = new Color(0.91f, 0.847f, 0.627f);
        private static readonly Color CombBrown = new Color(0.549f, 0.353f, 0.169f);
        private static readonly Color DeepBrown = new Color(0.227f, 0.141f, 0.086f);

        // Spawn order in the enemies tab: fodder → elites → bosses. One group
        // per world (playtest follow-up 2026-07-11) — new worlds append here.
        private const string BeehiveWorldName = "The Beehive";
        private static readonly string[] EnemyAssets =
        {
            "WorkerBee", "SwarmlingBee", "SpitterBee", "WarriorBee", "BomberBee",
            "QueensGuard", "QueensRoyalGuard", "QueenBee",
        };

        // Codex behavior blurbs (numbers scale with difficulty/run time, so the
        // codex describes what each enemy does instead). Authored only where the
        // asset's field is still empty, so hand tuning survives re-runs.
        private static readonly (string Asset, string Description)[] EnemyDescriptions =
        {
            ("WorkerBee", "A drone of the corrupted hive. Drifts straight at you — harmless alone, dangerous in a crowd."),
            ("SwarmlingBee", "Tiny and frantic. Floods in as a pack from one direction — don't let them box you in."),
            ("SpitterBee", "Keeps its distance and spits venom bolts. Weave between the shots or close the gap fast."),
            ("WarriorBee", "A hardened soldier that presses the chase and hits far harder than the drones."),
            ("BomberBee", "Rushes you and detonates in a damaging burst. Drop it at range — or don't be there."),
            ("QueensGuard", "An elite protector of the brood — tough, quick, and generous with its drops."),
            ("QueensRoyalGuard", "The Queen's champion, gating the mid-stage. Winds up long, punishing charges — sidestep the rush, then strike."),
            ("QueenBee", "The corrupted heart of the hive. Summons workers to shield her, unleashes stinger patterns, and enrages if the fight drags on."),
        };

        // Item-drop display rows (placeholder pictos — final art in ASSET_GENERATION.md).
        private static readonly (ItemDropType Type, string Name, string Description, string Picto)[] Items =
        {
            (ItemDropType.HoneyJar, "Honey Jar", "Restores a chunk of your health on pickup.", "Flask_01"),
            (ItemDropType.Magnet, "Nectar Magnet", "Vacuums every EXP and honey mote to you.", "Magnetic"),
            (ItemDropType.WaxShield, "Wax Shield", "Absorbs the next few hits you take.", "Defense"),
            (ItemDropType.RoyalBomb, "Royal Bomb", "Detonates royal-jelly energy, nuking everything on screen.", "Bomb"),
        };

        // Stand-in glyph for set-bonus entries (tinted per element at runtime).
        private const string SetGlyphPicto = "Sparkle";

        [MenuItem("SurveHive/Build Codex (5A)")]
        public static void Apply()
        {
            // New codex.* keys → the authored string table (append-only pass).
            LocalizationBuilder.Apply();

            EnsureCatalog();
            BuildEntryPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildMenuPanel();
            BuildRunTracker();

            Debug.Log("CodexBuilder: codex catalog, panel, and run tracker built.");
        }

        // ------------------------------------------------------------------
        // 1) Catalog asset.
        // ------------------------------------------------------------------
        private static void EnsureCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CodexCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CodexCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var so = new SerializedObject(catalog);
            so.FindProperty("_skillDatabase").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<SkillDatabaseSO>(SkillDatabasePath);

            EnsureEnemyDescriptions();

            SerializedProperty groups = so.FindProperty("_enemyGroups");
            groups.arraySize = 1;
            SerializedProperty beehive = groups.GetArrayElementAtIndex(0);
            beehive.FindPropertyRelative("WorldName").stringValue = BeehiveWorldName;
            SerializedProperty enemies = beehive.FindPropertyRelative("Enemies");
            enemies.arraySize = EnemyAssets.Length;
            for (int i = 0; i < EnemyAssets.Length; i++)
            {
                var stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(
                    $"{EnemiesFolder}/{EnemyAssets[i]}.asset");
                if (stats == null)
                {
                    Debug.LogError($"CodexBuilder: enemy asset '{EnemyAssets[i]}' missing.");
                }

                enemies.GetArrayElementAtIndex(i).objectReferenceValue = stats;
            }

            SerializedProperty items = so.FindProperty("_items");
            items.arraySize = Items.Length;
            for (int i = 0; i < Items.Length; i++)
            {
                SerializedProperty row = items.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Type").intValue = (int)Items[i].Type;
                row.FindPropertyRelative("DisplayName").stringValue = Items[i].Name;
                row.FindPropertyRelative("Description").stringValue = Items[i].Description;
                row.FindPropertyRelative("Icon").objectReferenceValue = LoadPicto(Items[i].Picto);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        // Fill each enemy's codex blurb only where it's still empty.
        private static void EnsureEnemyDescriptions()
        {
            foreach ((string asset, string description) in EnemyDescriptions)
            {
                var stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>($"{EnemiesFolder}/{asset}.asset");
                if (stats == null)
                {
                    continue;
                }

                var statsSo = new SerializedObject(stats);
                SerializedProperty blurb = statsSo.FindProperty("_codexDescription");
                if (string.IsNullOrEmpty(blurb.stringValue))
                {
                    blurb.stringValue = description;
                    statsSo.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(stats);
                }
            }
        }

        private static Sprite LoadPicto(string picto)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PictoFolder}/Icon_PictoIcon_{picto}.Png");
            if (sprite == null)
            {
                Debug.LogWarning($"CodexBuilder: placeholder picto '{picto}' not found.");
            }

            return sprite;
        }

        // ------------------------------------------------------------------
        // 2) Grid-cell prefab: icon + selection border on a button
        //    (MetaShopIcon minus the rank label).
        // ------------------------------------------------------------------
        private static void BuildEntryPrefab()
        {
            Sprite panelSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelPanel");

            var cellGo = new GameObject("CodexEntryIcon", typeof(RectTransform));
            var cellRect = (RectTransform)cellGo.transform;
            cellRect.sizeDelta = new Vector2(124f, 124f);

            Image bg = cellGo.AddComponent<Image>();
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 2f;
            bg.color = new Color(CombBrown.r, CombBrown.g, CombBrown.b, 0.85f);

            var button = cellGo.AddComponent<Button>();
            button.targetGraphic = bg;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.7f);
            colors.pressedColor = Amber;
            colors.selectedColor = Color.white;
            button.colors = colors;
            cellGo.AddComponent<UIClickSfx>();

            Image highlight = CreateStretchedImage(cellRect, "Selection", panelSprite,
                new Color(HoneyGold.r, HoneyGold.g, HoneyGold.b, 0.55f));
            highlight.enabled = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.SetParent(cellRect, false);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(88f, 88f);
            Image iconImage = iconGo.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var cell = cellGo.AddComponent<CodexEntryUI>();
            var cellSo = new SerializedObject(cell);
            cellSo.FindProperty("_iconImage").objectReferenceValue = iconImage;
            cellSo.FindProperty("_button").objectReferenceValue = button;
            cellSo.FindProperty("_selectionHighlight").objectReferenceValue = highlight;
            cellSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cell);

            PrefabUtility.SaveAsPrefabAsset(cellGo, EntryPrefabPath);
            Object.DestroyImmediate(cellGo);
        }

        // ------------------------------------------------------------------
        // 3) MainMenu: CODEX home button + CodexPanel.
        // ------------------------------------------------------------------
        private static void BuildMenuPanel()
        {
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            // Load assets only after the scene switch (pre-switch instances
            // serialize as fileID 0).
            var entryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EntryPrefabPath)
                .GetComponent<CodexEntryUI>();
            var catalog = AssetDatabase.LoadAssetAtPath<CodexCatalogSO>(CatalogPath);
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite panelSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelPanel");
            Sprite buttonSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelButton");
            Sprite setGlyph = LoadPicto(SetGlyphPicto);
            if (catalog == null || store == null || font == null || panelSprite == null || buttonSprite == null)
            {
                Debug.LogError("CodexBuilder: missing catalog/store/font/sprites for MainMenu.");
                return;
            }

            GameObject canvas = GameObject.Find("Canvas");
            var controllerGo = GameObject.Find("MainMenuController");
            var controller = controllerGo != null ? controllerGo.GetComponent<MainMenuController>() : null;
            Transform mainPanel = canvas != null ? canvas.transform.Find("MainPanel") : null;
            if (canvas == null || controller == null || mainPanel == null)
            {
                Debug.LogError("CodexBuilder: MainMenu canvas/controller/MainPanel not found.");
                return;
            }

            Button codexButton = EnsureHomeCodexButton(mainPanel, font, buttonSprite);
            RelayoutHomeButtons(mainPanel);

            // Rebuild our own panel sub-tree from scratch each run.
            Transform existingPanel = canvas.transform.Find("CodexPanel");
            if (existingPanel != null)
            {
                Object.DestroyImmediate(existingPanel.gameObject);
            }

            var panelGo = new GameObject("CodexPanel", typeof(RectTransform));
            var panelRect = (RectTransform)panelGo.transform;
            panelRect.SetParent(canvas.transform, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(1840f, 1000f);

            Image panelImage = panelGo.AddComponent<Image>();
            panelImage.sprite = panelSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.pixelsPerUnitMultiplier = 2f;
            panelImage.color = new Color(0.13f, 0.08f, 0.05f, 0.97f);

            TMP_Text title = CreateTopText(panelRect, "Title", font, 56f, HoneyGold, -36f, 70f, 800f,
                TextAlignmentOptions.Center);
            title.text = Loc.Get(LocKeys.CodexTitle);
            TMP_Text progress = CreateTopText(panelRect, "ProgressText", font, 30f, Amber, -110f, 44f, 800f,
                TextAlignmentOptions.Center);
            progress.text = Loc.Get(LocKeys.CodexDiscoveredPrefix);

            Button backButton = CreateButton(panelRect, "BackButton", "BACK", font, buttonSprite,
                Vector2.zero, new Vector2(220f, 70f), 28f);
            var backRect = (RectTransform)backButton.transform;
            backRect.anchorMin = new Vector2(0f, 1f);
            backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(36f, -36f);

            var tabButtons = new Button[CodexUI.CategoryCount];
            var tabHighlights = new Image[CodexUI.CategoryCount];
            BuildTabColumn(panelRect, font, buttonSprite, panelSprite, tabButtons, tabHighlights);

            RectTransform grid = BuildEntryGrid(panelRect, out ScrollRect entryScroll);
            BuildDetailPane(panelRect, font, panelSprite,
                out Image detailIcon, out TMP_Text detailName, out TMP_Text detailDescription);

            var codexUi = panelGo.AddComponent<CodexUI>();
            var so = new SerializedObject(codexUi);
            so.FindProperty("_store").objectReferenceValue = store;
            so.FindProperty("_catalog").objectReferenceValue = catalog;
            so.FindProperty("_entryPrefab").objectReferenceValue = entryPrefab;
            so.FindProperty("_gridContent").objectReferenceValue = grid;
            so.FindProperty("_scrollRect").objectReferenceValue = entryScroll;
            so.FindProperty("_sectionFont").objectReferenceValue = font;
            so.FindProperty("_detailIcon").objectReferenceValue = detailIcon;
            so.FindProperty("_detailName").objectReferenceValue = detailName;
            so.FindProperty("_detailDescription").objectReferenceValue = detailDescription;
            so.FindProperty("_progressText").objectReferenceValue = progress;
            so.FindProperty("_setIcon").objectReferenceValue = setGlyph;

            SerializedProperty tabsProp = so.FindProperty("_tabButtons");
            SerializedProperty highlightsProp = so.FindProperty("_tabHighlights");
            tabsProp.arraySize = tabButtons.Length;
            highlightsProp.arraySize = tabHighlights.Length;
            for (int i = 0; i < tabButtons.Length; i++)
            {
                tabsProp.GetArrayElementAtIndex(i).objectReferenceValue = tabButtons[i];
                highlightsProp.GetArrayElementAtIndex(i).objectReferenceValue = tabHighlights[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(codexUi);

            // Panels rest inactive; MainMenuController.Awake activates the home panel.
            panelGo.SetActive(false);

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("_codexPanel").objectReferenceValue = panelGo;
            controllerSo.FindProperty("_codexButton").objectReferenceValue = codexButton;
            controllerSo.FindProperty("_codexBackButton").objectReferenceValue = backButton;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static Button EnsureHomeCodexButton(Transform mainPanel, TMP_FontAsset font, Sprite buttonSprite)
        {
            Transform existing = mainPanel.Find("CodexButton");
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            return CreateButton((RectTransform)mainPanel, "CodexButton",
                Loc.Get(LocKeys.CodexMenuButton), font, buttonSprite,
                Vector2.zero, new Vector2(525f, 98f), 40f);
        }

        // Re-assert the PcMenuLayoutBuilder bottom-left stack with CODEX slotted
        // between HIVE UPGRADES and SETTINGS (top→bottom: Play, Shop, Codex,
        // Settings, Quit).
        private static void RelayoutHomeButtons(Transform mainPanel)
        {
            PlaceHomeButton(mainPanel, "PlayButton", 4);
            PlaceHomeButton(mainPanel, "ShopButton", 3);
            PlaceHomeButton(mainPanel, "CodexButton", 2);
            PlaceHomeButton(mainPanel, "SettingsButton", 1);
            PlaceHomeButton(mainPanel, "QuitButton", 0);
        }

        // Mirrors PcMenuLayoutBuilder.PlaceHomeButton.
        private static void PlaceHomeButton(Transform panel, string name, int rowFromBottom)
        {
            Transform button = panel.Find(name);
            if (button == null)
            {
                Debug.LogWarning($"CodexBuilder: home button '{name}' not found.");
                return;
            }

            var rect = (RectTransform)button;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(525f, 98f);
            rect.anchoredPosition = new Vector2(72f, 64f + rowFromBottom * 116f);
        }

        private static void BuildTabColumn(
            RectTransform panel, TMP_FontAsset font, Sprite buttonSprite, Sprite panelSprite,
            Button[] tabButtons, Image[] tabHighlights)
        {
            var columnGo = new GameObject("TabColumn", typeof(RectTransform));
            var columnRect = (RectTransform)columnGo.transform;
            columnRect.SetParent(panel, false);
            columnRect.anchorMin = new Vector2(0.5f, 0.5f);
            columnRect.anchorMax = new Vector2(0.5f, 0.5f);
            columnRect.pivot = new Vector2(0.5f, 0.5f);
            columnRect.anchoredPosition = new Vector2(-780f, -40f);
            columnRect.sizeDelta = new Vector2(250f, 700f);

            string[] labelKeys =
            {
                LocKeys.CodexTabPowerUps, LocKeys.CodexTabSets,
                LocKeys.CodexTabEnemies, LocKeys.CodexTabItems,
            };
            for (int i = 0; i < labelKeys.Length; i++)
            {
                var center = new Vector2(0f, 240f - (i * 160f));
                Button tab = CreateButton(columnRect, $"Tab{i}", Loc.Get(labelKeys[i]), font, buttonSprite,
                    center, new Vector2(236f, 110f), 26f);

                Image highlight = CreateStretchedImage((RectTransform)tab.transform, "TabHighlight",
                    panelSprite, new Color(1f, 1f, 1f, 0.4f));
                highlight.enabled = false;

                tabButtons[i] = tab;
                tabHighlights[i] = highlight;
            }
        }

        // Scrollable, vertically-stacked section area (playtest follow-up
        // 2026-07-11): CodexUI spawns one header + sub-grid per section into
        // the returned content, so the sectioned tabs can outgrow the window.
        private static RectTransform BuildEntryGrid(RectTransform panel, out ScrollRect scroll)
        {
            var scrollGo = new GameObject("EntryScroll", typeof(RectTransform));
            var scrollRect = (RectTransform)scrollGo.transform;
            scrollRect.SetParent(panel, false);
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.anchoredPosition = new Vector2(-140f, -60f);
            scrollRect.sizeDelta = new Vector2(980f, 620f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            var viewport = (RectTransform)viewportGo.transform;
            viewport.SetParent(scrollRect, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewport.pivot = new Vector2(0.5f, 1f);
            viewportGo.AddComponent<RectMask2D>();
            // ScrollRect needs a Graphic on the viewport to catch drag events.
            Image viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = Color.clear;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            var content = (RectTransform)contentGo.transform;
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            return content;
        }

        private static void BuildDetailPane(
            RectTransform panel, TMP_FontAsset font, Sprite panelSprite,
            out Image detailIcon, out TMP_Text detailName, out TMP_Text detailDescription)
        {
            var paneGo = new GameObject("DetailPanel", typeof(RectTransform));
            var paneRect = (RectTransform)paneGo.transform;
            paneRect.SetParent(panel, false);
            paneRect.anchorMin = new Vector2(0.5f, 0.5f);
            paneRect.anchorMax = new Vector2(0.5f, 0.5f);
            paneRect.pivot = new Vector2(0.5f, 0.5f);
            paneRect.anchoredPosition = new Vector2(620f, -60f);
            paneRect.sizeDelta = new Vector2(560f, 620f);

            Image bg = paneGo.AddComponent<Image>();
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 2f;
            bg.color = new Color(DeepBrown.r, DeepBrown.g, DeepBrown.b, 0.85f);
            bg.raycastTarget = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.SetParent(paneRect, false);
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -32f);
            iconRect.sizeDelta = new Vector2(128f, 128f);
            detailIcon = iconGo.AddComponent<Image>();
            detailIcon.preserveAspect = true;
            detailIcon.raycastTarget = false;

            detailName = CreateTopText(paneRect, "Name", font, 40f, HoneyGold, -180f, 60f, 520f,
                TextAlignmentOptions.Center);
            // Tall enough for a blurb + a full per-level breakdown (the codex
            // lists what every level grants once a power-up is discovered).
            detailDescription = CreateTopText(paneRect, "Description", font, 24f, Wax, -252f, 356f, 500f,
                TextAlignmentOptions.Top);
        }

        // ------------------------------------------------------------------
        // 4) Beehive: the run-scoped discovery tracker.
        // ------------------------------------------------------------------
        private static void BuildRunTracker()
        {
            EditorSceneManager.OpenScene(RunScenePath, OpenSceneMode.Single);

            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            if (store == null)
            {
                Debug.LogError("CodexBuilder: persistent store missing for Beehive tracker.");
                return;
            }

            GameObject trackerGo = GameObject.Find("CodexTracker");
            if (trackerGo == null)
            {
                trackerGo = new GameObject("CodexTracker");
            }

            if (!trackerGo.TryGetComponent(out CodexTracker tracker))
            {
                tracker = trackerGo.AddComponent<CodexTracker>();
            }

            var so = new SerializedObject(tracker);
            so.FindProperty("_store").objectReferenceValue = store;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(trackerGo);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // ------------------------------------------------------------------
        // Small UI factories (MetaShopTabsBuilder mold).
        // ------------------------------------------------------------------
        private static Image CreateStretchedImage(RectTransform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Button CreateButton(
            RectTransform parent, string name, string label, TMP_FontAsset font, Sprite buttonSprite,
            Vector2 centerOffset, Vector2 size, float fontSize)
        {
            var buttonGo = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)buttonGo.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = centerOffset;
            rect.sizeDelta = size;

            Image image = buttonGo.AddComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = HoneyGold;

            var button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.7f);
            colors.pressedColor = Amber;
            colors.disabledColor = new Color(0.45f, 0.42f, 0.38f);
            button.colors = colors;
            buttonGo.AddComponent<UIClickSfx>();

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.SetParent(buttonGo.transform, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.font = font;
            labelTmp.fontSize = fontSize;
            labelTmp.color = DeepBrown;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.textWrappingMode = TextWrappingModes.Normal;
            labelTmp.raycastTarget = false;
            labelTmp.text = label;

            return button;
        }

        private static TMP_Text CreateTopText(
            RectTransform parent, string name, TMP_FontAsset font, float fontSize, Color color,
            float topOffset, float height, float width, TextAlignmentOptions alignment)
        {
            var textGo = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)textGo.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, topOffset);
            rect.sizeDelta = new Vector2(width, height);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            tmp.text = name;
            return tmp;
        }
    }
}
