using SurveHive.Core;
using SurveHive.Data;
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
    /// PLAN 3B-1 — reworks the Hive Upgrades shop from a flat scrolling card grid
    /// (the retired data-driven card pass) into the tabbed layout from TODO #25:
    /// category tabs on the left (Combat / Survival / Utility), a detail pane up
    /// top for the selected upgrade with the BUY button, and a bottom grid of just
    /// the current category's upgrade icons (each with its rank/max).
    ///
    /// Three passes: (1) wire a placeholder icon onto every <see cref="MetaUpgradeSO"/>
    /// that lacks one, (2) (re)build the <c>MetaShopIcon</c> grid-cell prefab, and
    /// (3) rebuild the ShopPanel's tab column / detail pane / icon grid and rewire
    /// <see cref="MetaShopUI"/>. Additive and idempotent — it removes and recreates
    /// only its own generated sub-tree (the old scroll + cards, and its own nodes),
    /// never touching tuned data assets, Title, BalanceText, or the Back button.
    /// </summary>
    public static class MetaShopTabsBuilder
    {
        private const string MetaFolder = "Assets/Data/Meta";
        private const string PrefabFolder = "Assets/Prefabs/UI";
        private const string IconPrefabPath = PrefabFolder + "/MetaShopIcon.prefab";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string CatalogPath = MetaFolder + "/MetaUpgradeCatalog.asset";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";
        private const string PictoFolder = "Assets/ThirdParty/IconsTemp/Icons/PictoIcon_128";

        // Honey/hive palette (mirrors Phase4MetaAndMenusBuilder).
        private static readonly Color HoneyGold = new Color(1f, 0.765f, 0.043f);
        private static readonly Color Amber = new Color(0.961f, 0.651f, 0.137f);
        private static readonly Color Wax = new Color(0.91f, 0.847f, 0.627f);
        private static readonly Color CombBrown = new Color(0.549f, 0.353f, 0.169f);
        private static readonly Color DeepBrown = new Color(0.227f, 0.141f, 0.086f);

        // Placeholder picto per upgrade (final art tracked in ASSET_GENERATION.md §2.9).
        private static readonly (string Upgrade, string Picto)[] IconMap =
        {
            ("MaxHealth", "Heart"),
            ("Damage", "Sword"),
            ("MoveSpeed", "Speedmeter"),
            ("AttackSpeed", "Hammer"),
            ("Magnet", "Magnetic"),
            ("CurrencyGain", "Money"),
            ("ExpGain", "Star"),
            ("AbilityPower", "Thunder"),
            ("CooldownReduction", "Time"),
            ("CritChance", "Target"),
            ("CritDamage", "Star_Circle"),
            ("ItemDropRate", "Gift"),
            ("Rerolls", "Random"),
        };

        [MenuItem("SurveHive/Rework Shop To Tabbed Layout")]
        public static void Apply()
        {
            EnsureUpgradeIcons();
            EnsureCatalog();
            MetaShopIconUI iconPrefab = BuildIconPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RebuildShopPanel(iconPrefab);

            Debug.Log("SurveHive shop tabbed-layout rework complete.");
        }

        // ------------------------------------------------------------------
        // Catalog asset: every upgrade, in shop order (owned here since the
        // superseded ShopDataDrivenBuilder that used to author it was removed).
        // Find-or-create + repopulate; never touches the tuned upgrade assets.
        // ------------------------------------------------------------------
        private static void EnsureCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MetaUpgradeCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MetaUpgradeCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var so = new SerializedObject(catalog);
            SerializedProperty upgrades = so.FindProperty("_upgrades");
            upgrades.arraySize = IconMap.Length;
            for (int i = 0; i < IconMap.Length; i++)
            {
                var upgrade = AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>($"{MetaFolder}/{IconMap[i].Upgrade}.asset");
                if (upgrade == null)
                {
                    Debug.LogError($"MetaShopTabsBuilder: upgrade asset '{IconMap[i].Upgrade}' missing.");
                }

                upgrades.GetArrayElementAtIndex(i).objectReferenceValue = upgrade;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        // ------------------------------------------------------------------
        // 1) Placeholder icons — set only where empty so future art survives re-runs.
        // ------------------------------------------------------------------
        private static void EnsureUpgradeIcons()
        {
            foreach ((string upgradeName, string picto) in IconMap)
            {
                var upgrade = AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>($"{MetaFolder}/{upgradeName}.asset");
                if (upgrade == null)
                {
                    Debug.LogError($"MetaShopTabsBuilder: upgrade asset '{upgradeName}' missing.");
                    continue;
                }

                var so = new SerializedObject(upgrade);
                SerializedProperty iconProp = so.FindProperty("_icon");
                if (iconProp.objectReferenceValue == null)
                {
                    iconProp.objectReferenceValue = LoadPicto(picto);
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(upgrade);
                }
            }
        }

        private static Sprite LoadPicto(string picto)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PictoFolder}/Icon_PictoIcon_{picto}.Png");
            if (sprite == null)
            {
                Debug.LogWarning($"MetaShopTabsBuilder: placeholder picto '{picto}' not found.");
            }

            return sprite;
        }

        // ------------------------------------------------------------------
        // 2) Grid-cell prefab: icon + rank/max label + selection border on a button.
        // ------------------------------------------------------------------
        private static MetaShopIconUI BuildIconPrefab()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite panelSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelPanel");

            var cellGo = new GameObject("MetaShopIcon", typeof(RectTransform));
            var cellRect = (RectTransform)cellGo.transform;
            cellRect.sizeDelta = new Vector2(200f, 220f);

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

            // Selection border — a stretched gold frame, off until selected.
            Image highlight = CreateStretchedImage(cellRect, "Selection", panelSprite,
                new Color(HoneyGold.r, HoneyGold.g, HoneyGold.b, 0.55f));
            highlight.enabled = false;

            // Icon — centered, above the label.
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.SetParent(cellRect, false);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 22f);
            iconRect.sizeDelta = new Vector2(120f, 120f);
            Image iconImage = iconGo.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            // Level label — "3/10" along the bottom.
            TMP_Text levelText = CreateText(cellRect, "Level", font, 30f, Amber,
                new Vector2(0.5f, 0f), new Vector2(0f, 32f), new Vector2(180f, 44f),
                TextAlignmentOptions.Center);

            var icon = cellGo.AddComponent<MetaShopIconUI>();
            var iconSo = new SerializedObject(icon);
            iconSo.FindProperty("_iconImage").objectReferenceValue = iconImage;
            iconSo.FindProperty("_levelText").objectReferenceValue = levelText;
            iconSo.FindProperty("_button").objectReferenceValue = button;
            iconSo.FindProperty("_selectionHighlight").objectReferenceValue = highlight;
            iconSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(icon);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(cellGo, IconPrefabPath);
            Object.DestroyImmediate(cellGo);

            return saved.GetComponent<MetaShopIconUI>();
        }

        // ------------------------------------------------------------------
        // 3) ShopPanel: tab column (left) + detail pane (top-right) + icon grid.
        // ------------------------------------------------------------------
        private static void RebuildShopPanel(MetaShopIconUI iconPrefab)
        {
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            // Re-load after the scene switch — pre-switch instances wire as fileID 0.
            iconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IconPrefabPath).GetComponent<MetaShopIconUI>();
            var catalog = AssetDatabase.LoadAssetAtPath<MetaUpgradeCatalogSO>(CatalogPath);
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(
                "Assets/Data/Progression/PersistentMetaProgressionStore.asset");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite panelSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelPanel");
            Sprite buttonSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelButton");

            GameObject canvas = GameObject.Find("Canvas");
            Transform shopPanel = canvas != null ? canvas.transform.Find("ShopPanel") : null;
            if (shopPanel == null)
            {
                Debug.LogError("MetaShopTabsBuilder: ShopPanel not found in MainMenu.");
                return;
            }

            TMP_Text balanceText = FindText(shopPanel, "BalanceText");

            // PC landscape relayout: the portrait-era ShopPanel (1060×1880) fell off
            // both edges of a 1080-tall screen once the canvas was retargeted to
            // 1920×1080. Fill the screen and re-anchor the preserved chrome (Title /
            // BalanceText / BackButton) to the new bounds; the tab column / grid /
            // detail pane below lay out as three side-by-side columns.
            var shopRect = (RectTransform)shopPanel;
            shopRect.anchorMin = new Vector2(0.5f, 0.5f);
            shopRect.anchorMax = new Vector2(0.5f, 0.5f);
            shopRect.pivot = new Vector2(0.5f, 0.5f);
            shopRect.anchoredPosition = Vector2.zero;
            shopRect.sizeDelta = new Vector2(1840f, 1000f);

            RepositionTop(shopPanel, "Title", -36f);
            RepositionTop(shopPanel, "BalanceText", -120f);
            RepositionBottom(shopPanel, "BackButton", 34f);

            // Remove the previous layout (scroll + baked cards) and our own generated
            // nodes so the rebuild is idempotent. Title/BalanceText/BackButton stay.
            DestroyChild(shopPanel, "ShopScroll");
            DestroyChild(shopPanel, "TabColumn");
            DestroyChild(shopPanel, "DetailPanel");
            DestroyChild(shopPanel, "IconGrid");
            for (int i = shopPanel.childCount - 1; i >= 0; i--)
            {
                Transform child = shopPanel.GetChild(i);
                if (child.name.StartsWith("Card_") || child.name.StartsWith("Icon_"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            var tabButtons = new Button[MetaShopCategories.Count];
            var tabHighlights = new Image[MetaShopCategories.Count];
            BuildTabColumn(shopPanel, font, buttonSprite, panelSprite, tabButtons, tabHighlights);

            MetaShopDetailUI detail = BuildDetailPanel(shopPanel, font, panelSprite, buttonSprite);
            RectTransform grid = BuildIconGrid(shopPanel, panelSprite);

            var shopUi = shopPanel.GetComponent<MetaShopUI>();
            if (shopUi == null)
            {
                shopUi = shopPanel.gameObject.AddComponent<MetaShopUI>();
            }

            var so = new SerializedObject(shopUi);
            so.FindProperty("_store").objectReferenceValue = store;
            so.FindProperty("_catalog").objectReferenceValue = catalog;
            so.FindProperty("_balanceText").objectReferenceValue = balanceText;
            so.FindProperty("_iconPrefab").objectReferenceValue = iconPrefab;
            so.FindProperty("_gridContent").objectReferenceValue = grid;
            so.FindProperty("_detail").objectReferenceValue = detail;

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
            EditorUtility.SetDirty(shopUi);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void BuildTabColumn(
            Transform shopPanel, TMP_FontAsset font, Sprite buttonSprite, Sprite panelSprite,
            Button[] tabButtons, Image[] tabHighlights)
        {
            var columnGo = new GameObject("TabColumn", typeof(RectTransform));
            var columnRect = (RectTransform)columnGo.transform;
            columnRect.SetParent(shopPanel, false);
            columnRect.anchorMin = new Vector2(0.5f, 0.5f);
            columnRect.anchorMax = new Vector2(0.5f, 0.5f);
            columnRect.pivot = new Vector2(0.5f, 0.5f);
            columnRect.anchoredPosition = new Vector2(-780f, -10f);
            columnRect.sizeDelta = new Vector2(250f, 680f);

            string[] labelKeys = { LocKeys.ShopTabCombat, LocKeys.ShopTabSurvival, LocKeys.ShopTabUtility };
            for (int i = 0; i < MetaShopCategories.Count; i++)
            {
                var center = new Vector2(0f, 190f - (i * 185f));
                Button tab = CreateButton(columnRect, $"Tab{i}", Loc.Get(labelKeys[i]), font, buttonSprite,
                    center, new Vector2(236f, 130f), 30f);

                Image highlight = CreateStretchedImage((RectTransform)tab.transform, "TabHighlight",
                    panelSprite, new Color(1f, 1f, 1f, 0.4f));
                highlight.enabled = false;

                tabButtons[i] = tab;
                tabHighlights[i] = highlight;
            }
        }

        private static MetaShopDetailUI BuildDetailPanel(
            Transform shopPanel, TMP_FontAsset font, Sprite panelSprite, Sprite buttonSprite)
        {
            var panelGo = new GameObject("DetailPanel", typeof(RectTransform));
            var panelRect = (RectTransform)panelGo.transform;
            panelRect.SetParent(shopPanel, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(620f, -40f);
            panelRect.sizeDelta = new Vector2(560f, 660f);

            Image bg = panelGo.AddComponent<Image>();
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 2f;
            bg.color = new Color(DeepBrown.r, DeepBrown.g, DeepBrown.b, 0.85f);
            bg.raycastTarget = false;

            // Icon up top.
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.SetParent(panelRect, false);
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -28f);
            iconRect.sizeDelta = new Vector2(120f, 120f);
            Image iconImage = iconGo.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            TMP_Text nameText = CreateTopText(panelRect, "Name", font, 44f, HoneyGold, -164f, 60f, 520f, TextAlignmentOptions.Center);
            TMP_Text descText = CreateTopText(panelRect, "Description", font, 26f, Wax, -232f, 180f, 500f, TextAlignmentOptions.Top);
            TMP_Text rankText = CreateTopText(panelRect, "Rank", font, 30f, Amber, -428f, 44f, 520f, TextAlignmentOptions.Center);
            TMP_Text effectText = CreateTopText(panelRect, "Effect", font, 30f, HoneyGold, -482f, 44f, 520f, TextAlignmentOptions.Center);
            TMP_Text costText = CreateTopText(panelRect, "Cost", font, 34f, Amber, -540f, 48f, 520f, TextAlignmentOptions.Center);

            Button buyButton = CreateButton(panelRect, "BuyButton", Loc.Get(LocKeys.ShopBuy), font, buttonSprite,
                Vector2.zero, new Vector2(320f, 96f), 36f);
            var buyRect = (RectTransform)buyButton.transform;
            buyRect.anchorMin = new Vector2(0.5f, 0f);
            buyRect.anchorMax = new Vector2(0.5f, 0f);
            buyRect.pivot = new Vector2(0.5f, 0f);
            buyRect.anchoredPosition = new Vector2(0f, 26f);
            TMP_Text buyLabel = buyButton.transform.Find("Label").GetComponent<TMP_Text>();

            var detail = panelGo.AddComponent<MetaShopDetailUI>();
            var so = new SerializedObject(detail);
            so.FindProperty("_iconImage").objectReferenceValue = iconImage;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_descriptionText").objectReferenceValue = descText;
            so.FindProperty("_rankText").objectReferenceValue = rankText;
            so.FindProperty("_effectText").objectReferenceValue = effectText;
            so.FindProperty("_costText").objectReferenceValue = costText;
            so.FindProperty("_buyButton").objectReferenceValue = buyButton;
            so.FindProperty("_buyLabel").objectReferenceValue = buyLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(detail);

            return detail;
        }

        private static RectTransform BuildIconGrid(Transform shopPanel, Sprite panelSprite)
        {
            var gridGo = new GameObject("IconGrid", typeof(RectTransform));
            var gridRect = (RectTransform)gridGo.transform;
            gridRect.SetParent(shopPanel, false);
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = new Vector2(-160f, -10f);
            gridRect.sizeDelta = new Vector2(950f, 600f);

            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(172f, 186f);
            grid.spacing = new Vector2(14f, 14f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.padding = new RectOffset(12, 12, 12, 12);

            return gridRect;
        }

        // ------------------------------------------------------------------
        // Small UI factories.
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

        // Center-anchored text (used inside grid cells).
        private static TMP_Text CreateText(
            RectTransform parent, string name, TMP_FontAsset font, float fontSize, Color color,
            Vector2 anchor, Vector2 anchoredPosition, Vector2 size, TextAlignmentOptions alignment)
        {
            var textGo = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)textGo.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

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

        // Top-anchored text (used inside the detail panel).
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

        // Re-anchor a preserved chrome child to the panel's top edge at the given
        // offset (keeps its own size). Used after the panel is resized so Title /
        // BalanceText don't drift off the top of the new landscape bounds.
        private static void RepositionTop(Transform parent, string name, float offsetY)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                return;
            }

            var rect = (RectTransform)child;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, offsetY);
        }

        // Re-anchor a preserved chrome child to the panel's bottom edge (BackButton).
        private static void RepositionBottom(Transform parent, string name, float offsetY)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                return;
            }

            var rect = (RectTransform)child;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, offsetY);
        }

        private static void DestroyChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static TMP_Text FindText(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }
    }
}
