using SurveHive.Data;
using SurveHive.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Refactor pass — converts the Hive Upgrades shop from 13 hand-baked cards
    /// (emitted into the scene by <see cref="Phase4MetaAndMenusBuilder"/> /
    /// <see cref="MetaShopExpansionBuilder"/>) into a data-driven grid: one
    /// reusable <c>MetaShopCard.prefab</c> plus a <c>MetaUpgradeCatalog</c> asset
    /// listing every upgrade, which <see cref="MetaShopUI"/> instantiates at
    /// runtime under a <see cref="GridLayoutGroup"/>.
    ///
    /// Additive and idempotent: re-running rebuilds the prefab/catalog and rewires
    /// the panel; it does not touch the tuned data assets or other scene content.
    /// Adding a future upgrade is then just: create its <see cref="MetaUpgradeSO"/>
    /// and drop it into the catalog asset — no scene or builder edit.
    /// </summary>
    public static class ShopDataDrivenBuilder
    {
        private const string MetaFolder = "Assets/Data/Meta";
        private const string CatalogPath = MetaFolder + "/MetaUpgradeCatalog.asset";
        private const string PrefabFolder = "Assets/Prefabs/UI";
        private const string CardPrefabPath = PrefabFolder + "/MetaShopCard.prefab";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";

        // Same order the baked shop used (Phase-4A six, then the 1C additions).
        private static readonly string[] AllUpgradeNames =
        {
            "MaxHealth", "Damage", "MoveSpeed", "AttackSpeed", "Magnet", "CurrencyGain",
            "ExpGain", "AbilityPower", "CooldownReduction", "CritChance", "CritDamage",
            "ItemDropRate", "Rerolls",
        };

        // Matches the baked card metrics so the grid reads identically.
        private const int GridColumns = 2;
        private static readonly Vector2 CardSize = new Vector2(490f, 410f);
        private static readonly Vector2 CardSpacing = new Vector2(20f, 20f);

        [MenuItem("SurveHive/Refactor Shop To Data-Driven Grid")]
        public static void Apply()
        {
            MetaUpgradeCatalogSO catalog = EnsureCatalog();
            MetaShopCardUI cardPrefab = BuildCardPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            MigrateMenuScene(catalog, cardPrefab);

            Debug.Log("SurveHive shop data-driven refactor complete.");
        }

        // ------------------------------------------------------------------
        // Catalog asset: every upgrade, in shop order.
        // ------------------------------------------------------------------
        private static MetaUpgradeCatalogSO EnsureCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MetaUpgradeCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MetaUpgradeCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var so = new SerializedObject(catalog);
            SerializedProperty upgrades = so.FindProperty("_upgrades");
            upgrades.arraySize = AllUpgradeNames.Length;
            for (int i = 0; i < AllUpgradeNames.Length; i++)
            {
                var upgrade = AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>($"{MetaFolder}/{AllUpgradeNames[i]}.asset");
                if (upgrade == null)
                {
                    Debug.LogError($"ShopDataDrivenBuilder: upgrade asset '{AllUpgradeNames[i]}' missing.");
                }

                upgrades.GetArrayElementAtIndex(i).objectReferenceValue = upgrade;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        // ------------------------------------------------------------------
        // Card prefab: built once from the shared CreateShopCard factory, with no
        // upgrade bound (MetaShopUI.Bind sets it per instance at runtime).
        // ------------------------------------------------------------------
        private static MetaShopCardUI BuildCardPrefab()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite panelSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelPanel");
            Sprite buttonSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelButton");

            // A template upgrade is only needed to satisfy the factory; the prefab
            // stores no upgrade (bound at runtime), so clear it after building.
            var templateUpgrade = AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>($"{MetaFolder}/{AllUpgradeNames[0]}.asset");

            MetaShopCardUI card = Phase4MetaAndMenusBuilder.CreateShopCard(
                null, templateUpgrade, Vector2.zero, CardSize, font, panelSprite, buttonSprite);
            card.gameObject.name = "MetaShopCard";
            card.Bind(null);
            EditorUtility.SetDirty(card);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(card.gameObject, CardPrefabPath);
            Object.DestroyImmediate(card.gameObject);

            return saved.GetComponent<MetaShopCardUI>();
        }

        // ------------------------------------------------------------------
        // MainMenu: strip the baked cards, put a GridLayoutGroup on the scroll
        // content, and wire MetaShopUI to the catalog + prefab + content.
        // ------------------------------------------------------------------
        private static void MigrateMenuScene(MetaUpgradeCatalogSO catalog, MetaShopCardUI cardPrefab)
        {
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            // Re-load after the scene switch — pre-switch instances wire as fileID 0.
            catalog = AssetDatabase.LoadAssetAtPath<MetaUpgradeCatalogSO>(CatalogPath);
            cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath).GetComponent<MetaShopCardUI>();

            GameObject canvas = GameObject.Find("Canvas");
            Transform shopPanel = canvas != null ? canvas.transform.Find("ShopPanel") : null;
            if (shopPanel == null)
            {
                Debug.LogError("ShopDataDrivenBuilder: ShopPanel not found in MainMenu.");
                return;
            }

            Transform content = shopPanel.Find("ShopScroll/Viewport/Content");
            if (content == null)
            {
                Debug.LogError("ShopDataDrivenBuilder: ShopScroll/Viewport/Content not found — run the Phase 1C shop expansion first.");
                return;
            }

            var contentRect = (RectTransform)content;
            ConfigureGrid(contentRect);

            // Remove the baked Card_* children; the grid is spawned at runtime now.
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(content.GetChild(i).gameObject);
            }

            var shopUi = shopPanel.GetComponent<MetaShopUI>();
            var so = new SerializedObject(shopUi);
            so.FindProperty("_catalog").objectReferenceValue = catalog;
            so.FindProperty("_cardPrefab").objectReferenceValue = cardPrefab;
            so.FindProperty("_content").objectReferenceValue = contentRect;
            so.FindProperty("_rows").arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(shopUi);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // GridLayoutGroup + ContentSizeFitter replace the hand-computed RowPitch
        // math: rows now flow into a fixed-column grid that sizes the content.
        private static void ConfigureGrid(RectTransform content)
        {
            if (!content.TryGetComponent(out GridLayoutGroup grid))
            {
                grid = content.gameObject.AddComponent<GridLayoutGroup>();
            }

            grid.cellSize = CardSize;
            grid.spacing = CardSpacing;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GridColumns;
            grid.padding = new RectOffset(10, 10, 10, 10);

            if (!content.TryGetComponent(out ContentSizeFitter fitter))
            {
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
