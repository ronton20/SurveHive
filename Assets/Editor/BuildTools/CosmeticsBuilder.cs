using System.Collections.Generic;
using System.IO;
using SurveHive.Core;
using SurveHive.Data;
using SurveHive.UI;
using SurveHive.View;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// PLAN 5C — character customization. Five passes, all find-or-create /
    /// idempotent: (1) placeholder cosmetic sprites (PNGs written only when
    /// missing, so final art from ASSET_GENERATION.md survives re-runs),
    /// (2) the <see cref="CosmeticSO"/> roster + <see cref="CosmeticCatalogSO"/>
    /// (display/cost/offset fields are only authored on newly-created assets so
    /// hand tuning survives), (3) the CosmeticEntryIcon grid-cell prefab,
    /// (4) the MainMenu HiveStylePanel (slot tabs / hero preview / detail pane
    /// in the shop's tabbed mold) + STYLE home button + controller wiring,
    /// (5) the Beehive Player's Hat overlay renderer + the
    /// <see cref="CosmeticApplier"/>, and the auto-attack Stinger prefab's
    /// <see cref="ProjectileSkin"/> (stinger cosmetics re-skin the projectile).
    /// Rebuilds only its own generated nodes.
    /// </summary>
    public static class CosmeticsBuilder
    {
        private const string SpritesFolder = "Assets/Sprites/Cosmetics";
        private const string CosmeticsDataFolder = "Assets/Data/Cosmetics";
        private const string CatalogPath = CosmeticsDataFolder + "/CosmeticCatalog.asset";
        private const string EntryPrefabPath = "Assets/Prefabs/UI/CosmeticEntryIcon.prefab";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string RunScenePath = "Assets/Scenes/Beehive.unity";
        private const string PersistentStorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";
        private const string BeeLibraryPath = "Assets/ThirdParty/PixelFantasy/PixelMonsters/Pack1/Bee/YellowBee.asset";

        // Preview pixels per Body world unit: the 32px @ PPU16 hero body (2
        // units) renders at 256px in the menu preview.
        private const float PreviewPixelsPerUnit = 128f;

        // Honey/hive palette (mirrors Phase4MetaAndMenusBuilder / CodexBuilder).
        private static readonly Color HoneyGold = new Color(1f, 0.765f, 0.043f);
        private static readonly Color Amber = new Color(0.961f, 0.651f, 0.137f);
        private static readonly Color Wax = new Color(0.91f, 0.847f, 0.627f);
        private static readonly Color CombBrown = new Color(0.549f, 0.353f, 0.169f);
        private static readonly Color DeepBrown = new Color(0.227f, 0.141f, 0.086f);
        private static readonly Color RoyalCream = new Color(0.96f, 0.93f, 0.8f);

        private struct CosmeticDef
        {
            public string Id;
            public CosmeticSlot Slot;
            public string Name;
            public string Description;
            public int Cost;
            public Color Tint;
            public string SpriteFile;
            public Vector2 Offset;
            public int SortingOffset;
            // Stinger skins: the shape section they list under.
            public string ShapeGroup;
        }

        // Stinger skins are shape × color: one neutral (near-white) sprite per
        // shape, one tint per color, cost = shape base + color premium. The
        // same tint colors the in-run projectile.
        private static readonly Color StingerAmber = new Color(1f, 0.72f, 0.2f);
        private static readonly Color StingerSapphire = new Color(0.45f, 0.65f, 1f);
        private static readonly Color StingerVenom = new Color(0.45f, 1f, 0.4f);

        // The 5C roster: 5 basic colors + 3 hats + 3 stinger skins. Tints are
        // multiplicative over the yellow bee art, so they read as hue shifts.
        private static readonly CosmeticDef[] Cosmetics =
        {
            new CosmeticDef { Id = "color_ruby", Slot = CosmeticSlot.Color, Name = "Ruby Red", Description = "War paint for the hive's fiercest defender.", Cost = 3, Tint = new Color(1f, 0.5f, 0.5f) },
            new CosmeticDef { Id = "color_sapphire", Slot = CosmeticSlot.Color, Name = "Sapphire Blue", Description = "Cool as morning dew on the comb.", Cost = 3, Tint = new Color(0.55f, 0.72f, 1f) },
            new CosmeticDef { Id = "color_emerald", Slot = CosmeticSlot.Color, Name = "Emerald Green", Description = "Blends right into the garden.", Cost = 3, Tint = new Color(0.55f, 1f, 0.6f) },
            new CosmeticDef { Id = "color_amethyst", Slot = CosmeticSlot.Color, Name = "Amethyst Purple", Description = "Regal, mysterious, faintly magical.", Cost = 3, Tint = new Color(0.85f, 0.6f, 1f) },
            new CosmeticDef { Id = "color_onyx", Slot = CosmeticSlot.Color, Name = "Onyx Black", Description = "For bees who fly at midnight.", Cost = 5, Tint = new Color(0.5f, 0.47f, 0.45f) },
            new CosmeticDef { Id = "hat_crown", Slot = CosmeticSlot.Hat, Name = "Honey Crown", Description = "A dripping golden crown for hive royalty.", Cost = 15, Tint = Color.white, SpriteFile = "HatCrown", Offset = new Vector2(0f, 0.7f), SortingOffset = 1 },
            new CosmeticDef { Id = "hat_tophat", Slot = CosmeticSlot.Hat, Name = "Dapper Top Hat", Description = "For the distinguished bee about town.", Cost = 10, Tint = Color.white, SpriteFile = "HatTopHat", Offset = new Vector2(0.05f, 0.7f), SortingOffset = 1 },
            new CosmeticDef { Id = "hat_daisy", Slot = CosmeticSlot.Hat, Name = "Daisy Clip", Description = "A fresh daisy tucked behind the antenna.", Cost = 8, Tint = Color.white, SpriteFile = "HatDaisy", Offset = new Vector2(-0.2f, 0.65f), SortingOffset = 1 },
            // Needle — the slim classic (base 6; colors +0/+2/+4).
            new CosmeticDef { Id = "stinger_needle_amber", Slot = CosmeticSlot.Stinger, Name = "Amber Needle", Description = "The classic point, dipped in warm honey-gold.", Cost = 6, Tint = StingerAmber, SpriteFile = "StingerNeedle", Offset = new Vector2(0.9f, 0f), ShapeGroup = "NEEDLE" },
            new CosmeticDef { Id = "stinger_needle_sapphire", Slot = CosmeticSlot.Stinger, Name = "Sapphire Needle", Description = "A sliver of cold morning sky.", Cost = 8, Tint = StingerSapphire, SpriteFile = "StingerNeedle", Offset = new Vector2(0.9f, 0f), ShapeGroup = "NEEDLE" },
            new CosmeticDef { Id = "stinger_needle_venom", Slot = CosmeticSlot.Stinger, Name = "Venom Needle", Description = "Glows a mean, toxic green.", Cost = 10, Tint = StingerVenom, SpriteFile = "StingerNeedle", Offset = new Vector2(0.9f, 0f), ShapeGroup = "NEEDLE" },
            // Barb — hooked and cruel (base 10).
            new CosmeticDef { Id = "stinger_barb_amber", Slot = CosmeticSlot.Stinger, Name = "Amber Barb", Description = "Hooked like a wasp's grudge, gilded in gold.", Cost = 10, Tint = StingerAmber, SpriteFile = "StingerBarb", Offset = new Vector2(0.9f, 0f), ShapeGroup = "BARB" },
            new CosmeticDef { Id = "stinger_barb_sapphire", Slot = CosmeticSlot.Stinger, Name = "Sapphire Barb", Description = "Ice-blue hooks that catch the light.", Cost = 12, Tint = StingerSapphire, SpriteFile = "StingerBarb", Offset = new Vector2(0.9f, 0f), ShapeGroup = "BARB" },
            new CosmeticDef { Id = "stinger_barb_venom", Slot = CosmeticSlot.Stinger, Name = "Venom Barb", Description = "Every hook drips with spite.", Cost = 14, Tint = StingerVenom, SpriteFile = "StingerBarb", Offset = new Vector2(0.9f, 0f), ShapeGroup = "BARB" },
            // Blade — broad and regal (base 14).
            new CosmeticDef { Id = "stinger_blade_amber", Slot = CosmeticSlot.Stinger, Name = "Amber Blade", Description = "A broadsword in miniature, honey-forged.", Cost = 14, Tint = StingerAmber, SpriteFile = "StingerBlade", Offset = new Vector2(0.9f, 0f), ShapeGroup = "BLADE" },
            new CosmeticDef { Id = "stinger_blade_sapphire", Slot = CosmeticSlot.Stinger, Name = "Sapphire Blade", Description = "Cuts a cold blue arc through the swarm.", Cost = 16, Tint = StingerSapphire, SpriteFile = "StingerBlade", Offset = new Vector2(0.9f, 0f), ShapeGroup = "BLADE" },
            new CosmeticDef { Id = "stinger_blade_venom", Slot = CosmeticSlot.Stinger, Name = "Venom Blade", Description = "The Queen's executioner would be jealous.", Cost = 18, Tint = StingerVenom, SpriteFile = "StingerBlade", Offset = new Vector2(0.9f, 0f), ShapeGroup = "BLADE" },
        };

        // 5C-followup: the first stinger roster (body overlays) is superseded by
        // projectile skins — remove its builder-owned assets on re-run.
        private static readonly string[] LegacyAssets =
        {
            CosmeticsDataFolder + "/stinger_gold.asset",
            CosmeticsDataFolder + "/stinger_crystal.asset",
            CosmeticsDataFolder + "/stinger_thorn.asset",
            SpritesFolder + "/StingerGold.png",
            SpritesFolder + "/StingerCrystal.png",
            SpritesFolder + "/StingerThorn.png",
        };

        [MenuItem("SurveHive/Build Cosmetics (5C)")]
        public static void Apply()
        {
            // New cosmetics.* keys → the authored string table (append-only pass).
            LocalizationBuilder.Apply();

            foreach (string legacy in LegacyAssets)
            {
                AssetDatabase.DeleteAsset(legacy);
            }

            EnsurePlaceholderSprites();
            EnsureCosmeticAssets();
            BuildEntryPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildMenuPanel();
            BuildRunWiring();

            Debug.Log("CosmeticsBuilder: cosmetic roster, Hive Style panel, and run applier built.");
        }

        // ------------------------------------------------------------------
        // 1) Placeholder pixel art — written only when the PNG is missing, so
        //    final sprites dropped in later survive re-runs (ASSET_GENERATION
        //    §2.9 tracks the replacements).
        // ------------------------------------------------------------------
        private static void EnsurePlaceholderSprites()
        {
            if (!AssetDatabase.IsValidFolder(SpritesFolder))
            {
                AssetDatabase.CreateFolder("Assets/Sprites", "Cosmetics");
            }

            var palette = new Dictionary<char, Color32>
            {
                { 'X', new Color32(255, 195, 11, 255) },   // gold
                { 'x', new Color32(200, 140, 20, 255) },   // gold shade
                { 'R', new Color32(220, 60, 70, 255) },    // ruby jewel
                { 'B', new Color32(80, 130, 230, 255) },   // sapphire jewel
                { 'K', new Color32(35, 30, 34, 255) },     // near-black
                { 'A', new Color32(246, 166, 35, 255) },   // amber band
                { 'W', new Color32(245, 245, 240, 255) },  // petal white
                { 'O', new Color32(250, 175, 60, 255) },   // daisy center
                { 'G', new Color32(80, 160, 70, 255) },    // leaf green
                { 'g', new Color32(50, 110, 45, 255) },    // green shade
                { 'H', new Color32(255, 255, 255, 255) },  // highlight / neutral fill
                { 'h', new Color32(215, 215, 215, 255) },  // neutral light shade
                { 'd', new Color32(160, 160, 160, 255) },  // neutral dark shade
            };

            // Swatch: plain white square, tinted by the UI per color entry.
            WritePixelMap("Swatch", palette, new[]
            {
                "HHHHHHHHHH",
                "HHHHHHHHHH",
                "HHHHHHHHHH",
                "HHHHHHHHHH",
                "HHHHHHHHHH",
                "HHHHHHHHHH",
                "HHHHHHHHHH",
                "HHHHHHHHHH",
                "HHHHHHHHHH",
                "HHHHHHHHHH",
            });

            WritePixelMap("HatCrown", palette, new[]
            {
                "X....X....X.",
                "X....X....X.",
                "XX..XXX..XX.",
                "XXXXXXXXXXXX",
                "XxRxxBxxRxxX",
                "XXXXXXXXXXXX",
            });

            WritePixelMap("HatTopHat", palette, new[]
            {
                "..KKKKKKK...",
                "..KKKKKKK...",
                "..KKKKKKK...",
                "..KAAAAAK...",
                "KKKKKKKKKKKK",
                "KKKKKKKKKKKK",
            });

            WritePixelMap("HatDaisy", palette, new[]
            {
                "..WW..",
                ".WWWW.",
                "WWOOWW",
                "WWOOWW",
                ".WWWW.",
                "..WW.G",
                ".....g",
            });

            // Stinger shapes point RIGHT (projectile art faces its flight
            // direction) and stay neutral white/gray — the color variant is a
            // runtime tint, one sprite per shape.
            WritePixelMap("StingerNeedle", palette, new[]
            {
                "hHHh........",
                ".hHHHHHh....",
                "..hHHHHHHHHH",
                ".dhHHHHHd...",
                "dHHd........",
            });

            WritePixelMap("StingerBarb", palette, new[]
            {
                "..h..h......",
                ".hH.hH......",
                "hHHhHHHh....",
                "HHHHHHHHHHHH",
                "dHHdHHHd....",
                ".dH.dH......",
                "..d..d......",
            });

            WritePixelMap("StingerBlade", palette, new[]
            {
                "hhh..........",
                "hHHHHHh......",
                "hHHHHHHHHh...",
                "hHHHHHHHHHHHH",
                "dHHHHHHHHd...",
                "dHHHHHd......",
                "ddd..........",
            });

            AssetDatabase.Refresh();

            ImportCosmeticSprite("Swatch");
            foreach (CosmeticDef def in Cosmetics)
            {
                if (!string.IsNullOrEmpty(def.SpriteFile))
                {
                    ImportCosmeticSprite(def.SpriteFile);
                }
            }
        }

        private static void WritePixelMap(string fileName, Dictionary<char, Color32> palette, string[] rows)
        {
            string path = $"{SpritesFolder}/{fileName}.png";
            if (File.Exists(path))
            {
                return;
            }

            int height = rows.Length;
            int width = rows[0].Length;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var clear = new Color32[width * height];
            texture.SetPixels32(clear);

            for (int row = 0; row < height; row++)
            {
                for (int x = 0; x < width; x++)
                {
                    char key = rows[row][x];
                    if (palette.TryGetValue(key, out Color32 color))
                    {
                        // Row 0 is the top of the drawing; texture y=0 is the bottom.
                        texture.SetPixel(x, height - 1 - row, color);
                    }
                }
            }

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static void ImportCosmeticSprite(string fileName)
        {
            string path = $"{SpritesFolder}/{fileName}.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError($"CosmeticsBuilder: missing sprite {path}.");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 16f;
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------
        // 2) The CosmeticSO roster + catalog. Structural fields (id/slot/
        //    sprite) are re-asserted every run; display/cost/tint/offset are
        //    only authored when the asset is newly created, so hand tuning
        //    (e.g. nudging a hat's attach offset in the Inspector) survives.
        // ------------------------------------------------------------------
        private static void EnsureCosmeticAssets()
        {
            if (!AssetDatabase.IsValidFolder(CosmeticsDataFolder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Cosmetics");
            }

            var entries = new List<CosmeticSO>(Cosmetics.Length);
            foreach (CosmeticDef def in Cosmetics)
            {
                string path = $"{CosmeticsDataFolder}/{def.Id}.asset";
                var cosmetic = AssetDatabase.LoadAssetAtPath<CosmeticSO>(path);
                bool isNew = cosmetic == null;
                if (isNew)
                {
                    cosmetic = ScriptableObject.CreateInstance<CosmeticSO>();
                    AssetDatabase.CreateAsset(cosmetic, path);
                }

                var so = new SerializedObject(cosmetic);
                so.FindProperty("_cosmeticId").stringValue = def.Id;
                so.FindProperty("_slot").intValue = (int)def.Slot;
                so.FindProperty("_shapeGroup").stringValue = def.ShapeGroup ?? string.Empty;
                so.FindProperty("_sprite").objectReferenceValue = string.IsNullOrEmpty(def.SpriteFile)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesFolder}/{def.SpriteFile}.png");

                if (isNew)
                {
                    so.FindProperty("_displayName").stringValue = def.Name;
                    so.FindProperty("_description").stringValue = def.Description;
                    so.FindProperty("_jellyCost").intValue = def.Cost;
                    so.FindProperty("_tint").colorValue = def.Tint;
                    so.FindProperty("_attachOffset").vector2Value = def.Offset;
                    so.FindProperty("_sortingOffset").intValue = def.SortingOffset;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(cosmetic);
                entries.Add(cosmetic);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CosmeticCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var catalogSo = new SerializedObject(catalog);
            SerializedProperty list = catalogSo.FindProperty("_cosmetics");
            list.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            }

            catalogSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        // ------------------------------------------------------------------
        // 3) Grid-cell prefab: CodexEntryIcon mold + an equipped badge.
        // ------------------------------------------------------------------
        private static void BuildEntryPrefab()
        {
            Sprite panelSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelPanel");
            var swatch = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesFolder}/Swatch.png");

            var cellGo = new GameObject("CosmeticEntryIcon", typeof(RectTransform));
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

            // Small gold badge marking the equipped entry (top-right corner).
            var badgeGo = new GameObject("EquippedBadge", typeof(RectTransform));
            var badgeRect = (RectTransform)badgeGo.transform;
            badgeRect.SetParent(cellRect, false);
            badgeRect.anchorMin = Vector2.one;
            badgeRect.anchorMax = Vector2.one;
            badgeRect.pivot = Vector2.one;
            badgeRect.anchoredPosition = new Vector2(-10f, -10f);
            badgeRect.sizeDelta = new Vector2(26f, 26f);
            Image badgeImage = badgeGo.AddComponent<Image>();
            badgeImage.sprite = swatch;
            badgeImage.color = HoneyGold;
            badgeImage.raycastTarget = false;
            badgeImage.enabled = false;

            var cell = cellGo.AddComponent<CosmeticEntryUI>();
            var cellSo = new SerializedObject(cell);
            cellSo.FindProperty("_iconImage").objectReferenceValue = iconImage;
            cellSo.FindProperty("_button").objectReferenceValue = button;
            cellSo.FindProperty("_selectionHighlight").objectReferenceValue = highlight;
            cellSo.FindProperty("_equippedBadge").objectReferenceValue = badgeImage;
            cellSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cell);

            PrefabUtility.SaveAsPrefabAsset(cellGo, EntryPrefabPath);
            Object.DestroyImmediate(cellGo);
        }

        // ------------------------------------------------------------------
        // 4) MainMenu: STYLE home button + HiveStylePanel.
        // ------------------------------------------------------------------
        private static void BuildMenuPanel()
        {
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            // Load assets only after the scene switch (pre-switch instances
            // serialize as fileID 0).
            var entryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EntryPrefabPath)
                .GetComponent<CosmeticEntryUI>();
            var catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalogSO>(CatalogPath);
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite panelSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelPanel");
            Sprite buttonSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelButton");
            var swatch = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesFolder}/Swatch.png");
            var beeLibrary = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(BeeLibraryPath);
            if (catalog == null || store == null || font == null || panelSprite == null
                || buttonSprite == null || swatch == null)
            {
                Debug.LogError("CosmeticsBuilder: missing catalog/store/font/sprites for MainMenu.");
                return;
            }

            GameObject canvas = GameObject.Find("Canvas");
            var controllerGo = GameObject.Find("MainMenuController");
            var controller = controllerGo != null ? controllerGo.GetComponent<MainMenuController>() : null;
            Transform mainPanel = canvas != null ? canvas.transform.Find("MainPanel") : null;
            if (canvas == null || controller == null || mainPanel == null)
            {
                Debug.LogError("CosmeticsBuilder: MainMenu canvas/controller/MainPanel not found.");
                return;
            }

            Button styleButton = EnsureHomeStyleButton(mainPanel, font, buttonSprite);
            RelayoutHomeButtons(mainPanel);

            // Rebuild our own panel sub-tree from scratch each run.
            Transform existingPanel = canvas.transform.Find("HiveStylePanel");
            if (existingPanel != null)
            {
                Object.DestroyImmediate(existingPanel.gameObject);
            }

            var panelGo = new GameObject("HiveStylePanel", typeof(RectTransform));
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
            title.text = Loc.Get(LocKeys.CosmeticsTitle);

            TMP_Text jellyText = CreateJellyText(panelRect, font);

            Button backButton = CreateButton(panelRect, "BackButton", "BACK", font, buttonSprite,
                Vector2.zero, new Vector2(220f, 70f), 28f);
            var backRect = (RectTransform)backButton.transform;
            backRect.anchorMin = new Vector2(0f, 1f);
            backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(36f, -36f);

            var tabButtons = new Button[CosmeticSlots.Count];
            var tabHighlights = new Image[CosmeticSlots.Count];
            BuildTabColumn(panelRect, font, buttonSprite, panelSprite, tabButtons, tabHighlights);

            RectTransform grid = BuildEntryGrid(panelRect, out ScrollRect entryScroll);
            BuildPreviewPane(panelRect, panelSprite, beeLibrary,
                out Image previewBody, out Image previewHat, out Image previewStinger);
            BuildDetailPane(panelRect, font, panelSprite, buttonSprite,
                out Image detailIcon, out TMP_Text detailName, out TMP_Text detailDescription,
                out Button actionButton, out TMP_Text actionLabel);

            var cosmeticsUi = panelGo.AddComponent<CosmeticsUI>();
            var so = new SerializedObject(cosmeticsUi);
            so.FindProperty("_store").objectReferenceValue = store;
            so.FindProperty("_catalog").objectReferenceValue = catalog;
            so.FindProperty("_jellyText").objectReferenceValue = jellyText;
            so.FindProperty("_entryPrefab").objectReferenceValue = entryPrefab;
            so.FindProperty("_gridContent").objectReferenceValue = grid;
            so.FindProperty("_scrollRect").objectReferenceValue = entryScroll;
            so.FindProperty("_sectionFont").objectReferenceValue = font;
            so.FindProperty("_swatchSprite").objectReferenceValue = swatch;
            so.FindProperty("_detailIcon").objectReferenceValue = detailIcon;
            so.FindProperty("_detailName").objectReferenceValue = detailName;
            so.FindProperty("_detailDescription").objectReferenceValue = detailDescription;
            so.FindProperty("_actionButton").objectReferenceValue = actionButton;
            so.FindProperty("_actionLabel").objectReferenceValue = actionLabel;
            so.FindProperty("_previewBody").objectReferenceValue = previewBody;
            so.FindProperty("_previewHat").objectReferenceValue = previewHat;
            so.FindProperty("_previewStinger").objectReferenceValue = previewStinger;
            so.FindProperty("_previewPixelsPerUnit").floatValue = PreviewPixelsPerUnit;

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
            EditorUtility.SetDirty(cosmeticsUi);

            // Panels rest inactive; MainMenuController.Awake activates the home panel.
            panelGo.SetActive(false);

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("_cosmeticsPanel").objectReferenceValue = panelGo;
            controllerSo.FindProperty("_cosmeticsButton").objectReferenceValue = styleButton;
            controllerSo.FindProperty("_cosmeticsBackButton").objectReferenceValue = backButton;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static Button EnsureHomeStyleButton(Transform mainPanel, TMP_FontAsset font, Sprite buttonSprite)
        {
            Transform existing = mainPanel.Find("StyleButton");
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            return CreateButton((RectTransform)mainPanel, "StyleButton",
                Loc.Get(LocKeys.CosmeticsMenuButton), font, buttonSprite,
                Vector2.zero, new Vector2(525f, 98f), 40f);
        }

        // Re-assert the bottom-left home stack with STYLE slotted between CODEX
        // and SETTINGS (top→bottom: Play, Shop, Codex, Style, Settings, Quit).
        private static void RelayoutHomeButtons(Transform mainPanel)
        {
            PlaceHomeButton(mainPanel, "PlayButton", 5);
            PlaceHomeButton(mainPanel, "ShopButton", 4);
            PlaceHomeButton(mainPanel, "CodexButton", 3);
            PlaceHomeButton(mainPanel, "StyleButton", 2);
            PlaceHomeButton(mainPanel, "SettingsButton", 1);
            PlaceHomeButton(mainPanel, "QuitButton", 0);
        }

        // Mirrors CodexBuilder.PlaceHomeButton / PcMenuLayoutBuilder.
        private static void PlaceHomeButton(Transform panel, string name, int rowFromBottom)
        {
            Transform button = panel.Find(name);
            if (button == null)
            {
                Debug.LogWarning($"CosmeticsBuilder: home button '{name}' not found.");
                return;
            }

            var rect = (RectTransform)button;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(525f, 98f);
            rect.anchoredPosition = new Vector2(72f, 64f + rowFromBottom * 116f);
        }

        private static TMP_Text CreateJellyText(RectTransform panel, TMP_FontAsset font)
        {
            var textGo = new GameObject("JellyText", typeof(RectTransform));
            var rect = (RectTransform)textGo.transform;
            rect.SetParent(panel, false);
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-48f, -40f);
            rect.sizeDelta = new Vector2(400f, 60f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = 42f;
            tmp.color = RoyalCream;
            tmp.alignment = TextAlignmentOptions.Right;
            tmp.raycastTarget = false;
            // Rest-state placeholder; CosmeticsUI paints the live glyph+number.
            tmp.text = CurrencyGlyphs.Jelly + "0";
            return tmp;
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
                LocKeys.CosmeticsTabColors, LocKeys.CosmeticsTabHats, LocKeys.CosmeticsTabStingers,
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

        // Scrollable, vertically-stacked section area (CodexBuilder mold):
        // CosmeticsUI spawns one header + sub-grid per section into the
        // returned content, so the sectioned stinger tab can outgrow the window.
        private static RectTransform BuildEntryGrid(RectTransform panel, out ScrollRect scroll)
        {
            var scrollGo = new GameObject("EntryScroll", typeof(RectTransform));
            var scrollRect = (RectTransform)scrollGo.transform;
            scrollRect.SetParent(panel, false);
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.anchoredPosition = new Vector2(-240f, -60f);
            scrollRect.sizeDelta = new Vector2(840f, 620f);

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
            layout.spacing = 10f;
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

        private static void BuildPreviewPane(
            RectTransform panel, Sprite panelSprite, SpriteLibraryAsset beeLibrary,
            out Image previewBody, out Image previewHat, out Image previewStinger)
        {
            var paneGo = new GameObject("PreviewPanel", typeof(RectTransform));
            var paneRect = (RectTransform)paneGo.transform;
            paneRect.SetParent(panel, false);
            paneRect.anchorMin = new Vector2(0.5f, 0.5f);
            paneRect.anchorMax = new Vector2(0.5f, 0.5f);
            paneRect.pivot = new Vector2(0.5f, 0.5f);
            paneRect.anchoredPosition = new Vector2(620f, 160f);
            paneRect.sizeDelta = new Vector2(560f, 440f);

            Image bg = paneGo.AddComponent<Image>();
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 2f;
            bg.color = new Color(DeepBrown.r, DeepBrown.g, DeepBrown.b, 0.85f);
            bg.raycastTarget = false;

            var bodyGo = new GameObject("PreviewBody", typeof(RectTransform));
            var bodyRect = (RectTransform)bodyGo.transform;
            bodyRect.SetParent(paneRect, false);
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.anchoredPosition = new Vector2(0f, -10f);
            bodyRect.sizeDelta = new Vector2(256f, 256f);
            previewBody = bodyGo.AddComponent<Image>();
            previewBody.preserveAspect = true;
            previewBody.raycastTarget = false;
            // Mirrors the in-run rig's current default skin; the hero-art pass
            // (PLAN 6A) re-points this when the final rig lands.
            previewBody.sprite = beeLibrary != null ? beeLibrary.GetSprite("Idle", "0") : null;

            previewHat = CreatePreviewOverlay(bodyRect, "PreviewHat");
            previewStinger = CreatePreviewOverlay(bodyRect, "PreviewStinger");
        }

        private static Image CreatePreviewOverlay(RectTransform bodyRect, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(bodyRect, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            Image image = go.AddComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static void BuildDetailPane(
            RectTransform panel, TMP_FontAsset font, Sprite panelSprite, Sprite buttonSprite,
            out Image detailIcon, out TMP_Text detailName, out TMP_Text detailDescription,
            out Button actionButton, out TMP_Text actionLabel)
        {
            var paneGo = new GameObject("DetailPanel", typeof(RectTransform));
            var paneRect = (RectTransform)paneGo.transform;
            paneRect.SetParent(panel, false);
            paneRect.anchorMin = new Vector2(0.5f, 0.5f);
            paneRect.anchorMax = new Vector2(0.5f, 0.5f);
            paneRect.pivot = new Vector2(0.5f, 0.5f);
            paneRect.anchoredPosition = new Vector2(620f, -290f);
            paneRect.sizeDelta = new Vector2(560f, 400f);

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
            iconRect.anchoredPosition = new Vector2(0f, -20f);
            iconRect.sizeDelta = new Vector2(96f, 96f);
            detailIcon = iconGo.AddComponent<Image>();
            detailIcon.preserveAspect = true;
            detailIcon.raycastTarget = false;

            detailName = CreateTopText(paneRect, "Name", font, 38f, HoneyGold, -128f, 50f, 520f,
                TextAlignmentOptions.Center);
            detailDescription = CreateTopText(paneRect, "Description", font, 24f, Wax, -184f, 110f, 500f,
                TextAlignmentOptions.Top);

            actionButton = CreateButton(paneRect, "ActionButton", Loc.Get(LocKeys.CosmeticsEquip),
                font, buttonSprite, Vector2.zero, new Vector2(340f, 84f), 30f);
            var actionRect = (RectTransform)actionButton.transform;
            actionRect.anchorMin = new Vector2(0.5f, 0f);
            actionRect.anchorMax = new Vector2(0.5f, 0f);
            actionRect.pivot = new Vector2(0.5f, 0f);
            actionRect.anchoredPosition = new Vector2(0f, 28f);
            actionLabel = actionButton.GetComponentInChildren<TMP_Text>();
        }

        // ------------------------------------------------------------------
        // 5) Beehive: hat overlay renderer on the Body + the applier, plus the
        //    ProjectileSkin hook on the auto-attack Stinger prefab (5C-followup:
        //    stinger skins re-skin the projectile, not the body).
        // ------------------------------------------------------------------
        private static void BuildRunWiring()
        {
            EnsureProjectileSkin();

            EditorSceneManager.OpenScene(RunScenePath, OpenSceneMode.Single);

            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            var catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalogSO>(CatalogPath);
            if (store == null || catalog == null)
            {
                Debug.LogError("CosmeticsBuilder: store/catalog missing for Beehive wiring.");
                return;
            }

            GameObject playerGo = GameObject.Find("Player");
            Transform body = playerGo != null ? playerGo.transform.Find("Body") : null;
            if (playerGo == null || body == null)
            {
                Debug.LogError("CosmeticsBuilder: Player/Body not found in Beehive scene.");
                return;
            }

            var bodyRenderer = body.GetComponent<SpriteRenderer>();
            SpriteRenderer hat = EnsureOverlayRenderer(body, "Hat");

            // The superseded body-overlay stinger node.
            Transform legacyStinger = body.Find("Stinger");
            if (legacyStinger != null)
            {
                Object.DestroyImmediate(legacyStinger.gameObject);
            }

            if (!playerGo.TryGetComponent(out CosmeticApplier applier))
            {
                applier = playerGo.AddComponent<CosmeticApplier>();
            }

            var so = new SerializedObject(applier);
            so.FindProperty("_store").objectReferenceValue = store;
            so.FindProperty("_catalog").objectReferenceValue = catalog;
            so.FindProperty("_bodyRenderer").objectReferenceValue = bodyRenderer;
            so.FindProperty("_hatRenderer").objectReferenceValue = hat;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(applier);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // The player's auto-attack projectile applies the equipped StingerSkin
        // on every pooled spawn. Enemy/skill projectile prefabs are untouched.
        private static void EnsureProjectileSkin()
        {
            const string prefabPath = "Assets/Prefabs/Projectiles/Stinger.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var renderer = root.GetComponentInChildren<SpriteRenderer>();
                if (renderer == null)
                {
                    Debug.LogError($"CosmeticsBuilder: no SpriteRenderer on {prefabPath}.");
                    return;
                }

                if (!root.TryGetComponent(out ProjectileSkin skin))
                {
                    skin = root.AddComponent<ProjectileSkin>();
                }

                var so = new SerializedObject(skin);
                so.FindProperty("_renderer").objectReferenceValue = renderer;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Rest state: disabled, no sprite — CosmeticApplier dresses it at Awake.
        private static SpriteRenderer EnsureOverlayRenderer(Transform body, string name)
        {
            Transform existing = body.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            go.transform.SetParent(body, false);

            if (!go.TryGetComponent(out SpriteRenderer renderer))
            {
                renderer = go.AddComponent<SpriteRenderer>();
            }

            renderer.enabled = false;
            return renderer;
        }

        // ------------------------------------------------------------------
        // Small UI factories (CodexBuilder mold).
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
