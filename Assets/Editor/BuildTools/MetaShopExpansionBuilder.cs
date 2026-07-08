using SurveHive.Data;
using SurveHive.Player;
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
    /// PLAN.md Phase 1C — meta shop expansion. Seven new permanent upgrades
    /// (EXP gain, ability power, cooldown reduction, crit chance, crit damage,
    /// item drop rate, and the per-run power-up reroll stock), the shop panel
    /// converted to a scrollable 2-column grid holding all 13 cards, and the
    /// level-up screen's reroll controls (per-card REROLL buttons + remaining
    /// count). Additive and idempotent over the existing scenes/assets.
    /// </summary>
    public static class MetaShopExpansionBuilder
    {
        private const string MetaFolder = "Assets/Data/Meta";
        private const string PersistentStorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";
        private const string RerollUpgradePath = MetaFolder + "/Rerolls.asset";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string RunScenePath = "Assets/Scenes/Beehive.unity";
        private const string UiKitTexturePath = "Assets/ThirdParty/PixelUI/UI SIMPLE PIXEL UNSPLIT.png";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";

        private static readonly Color Amber = new Color(0.961f, 0.651f, 0.137f);
        private static readonly Color CombBrown = new Color(0.549f, 0.353f, 0.169f);

        // Shop order: the six Phase-4A upgrades, then the 1C additions.
        private static readonly string[] AllUpgradeNames =
        {
            "MaxHealth", "Damage", "MoveSpeed", "AttackSpeed", "Magnet", "CurrencyGain",
            "ExpGain", "AbilityPower", "CooldownReduction", "CritChance", "CritDamage",
            "ItemDropRate", "Rerolls",
        };

        private const int GridColumns = 2;
        private static readonly Vector2 CardSize = new Vector2(490f, 410f);
        private const float RowPitch = 430f;

        [MenuItem("SurveHive/Apply Meta Shop Expansion (Phase 1C)")]
        public static void Apply()
        {
            EnsureNewUpgradeAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ApplyMenuSceneChanges();
            ApplyRunSceneChanges();

            Debug.Log("SurveHive meta shop expansion (Phase 1C) build complete.");
        }

        // ------------------------------------------------------------------
        // The seven new upgrade definitions. Costs priced against the post-1A
        // (nerfed) honey income; the cross-shop cost rebalance folds into the
        // 1A round-2 pass once playtest numbers exist.
        // ------------------------------------------------------------------
        private static void EnsureNewUpgradeAssets()
        {
            EnsureUpgrade("ExpGain", "meta_exp_gain", "Wisdom of the Hive", "EXP Gain",
                "Permanently raises EXP gained in runs.", MetaStatType.ExpGain,
                maxRank: 8, baseCost: 60, costGrowth: 1.45f, effectPerRank: 5f);
            EnsureUpgrade("AbilityPower", "meta_ability_power", "Queen's Blessing", "Ability DMG",
                "Permanently raises active-skill damage.", MetaStatType.AbilityPower,
                maxRank: 8, baseCost: 80, costGrowth: 1.45f, effectPerRank: 4f);
            EnsureUpgrade("CooldownReduction", "meta_cooldown", "Efficient Glands", "Cooldown Cut",
                "Permanently shortens active-skill cooldowns.", MetaStatType.CooldownReduction,
                maxRank: 6, baseCost: 80, costGrowth: 1.5f, effectPerRank: 3f);
            // User-mandated 2026-07-08: +2%/rank, 20 ranks, 40% cap on the 0% base.
            EnsureUpgrade("CritChance", "meta_crit_chance", "Killer Instinct", "Crit Chance",
                "Permanently raises critical-hit chance.", MetaStatType.CritChance,
                maxRank: 20, baseCost: 50, costGrowth: 1.25f, effectPerRank: 2f);
            EnsureUpgrade("CritDamage", "meta_crit_damage", "Barbed Stingers", "Crit DMG",
                "Permanently raises critical-hit damage.", MetaStatType.CritDamage,
                maxRank: 10, baseCost: 70, costGrowth: 1.4f, effectPerRank: 5f);
            EnsureUpgrade("ItemDropRate", "meta_item_drop", "Forager's Instinct", "Item Drop Rate",
                "Permanently raises item drop chance.", MetaStatType.ItemDropRate,
                maxRank: 5, baseCost: 100, costGrowth: 1.6f, effectPerRank: 10f);
            // User-mandated 2026-07-08: strong feature, cost-gated hard —
            // 400 / 1,520 / 5,776 for the 3 per-run charges.
            EnsureUpgrade("Rerolls", "meta_rerolls", "Waggle Dance", "Rerolls / Run",
                "Reroll one offered power-up card. Stock refills every run.", MetaStatType.Rerolls,
                maxRank: 3, baseCost: 400, costGrowth: 3.8f, effectPerRank: 1f);
        }

        private static void EnsureUpgrade(
            string assetName, string id, string displayName, string effectLabel, string description,
            MetaStatType statType, int maxRank, int baseCost, float costGrowth, float effectPerRank)
        {
            string path = $"{MetaFolder}/{assetName}.asset";
            var upgrade = AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>(path);
            if (upgrade == null)
            {
                upgrade = ScriptableObject.CreateInstance<MetaUpgradeSO>();
                AssetDatabase.CreateAsset(upgrade, path);
            }

            var so = new SerializedObject(upgrade);
            so.FindProperty("_upgradeId").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_effectLabel").stringValue = effectLabel;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_statType").intValue = (int)statType;
            so.FindProperty("_maxRank").intValue = maxRank;
            so.FindProperty("_baseCost").intValue = baseCost;
            so.FindProperty("_costGrowth").floatValue = costGrowth;
            so.FindProperty("_effectPerRank").floatValue = effectPerRank;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(upgrade);
        }

        // ------------------------------------------------------------------
        // MainMenu: wrap the card area in a ScrollRect, move the six existing
        // cards into its content, add the seven new ones, rewire MetaShopUI.
        // ------------------------------------------------------------------
        private static void ApplyMenuSceneChanges()
        {
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            GameObject canvas = GameObject.Find("Canvas");
            Transform shopPanel = canvas != null ? canvas.transform.Find("ShopPanel") : null;
            if (shopPanel == null)
            {
                Debug.LogError("MetaShopExpansionBuilder: ShopPanel not found in MainMenu.");
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite panelSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelPanel");
            Sprite buttonSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelButton");

            RectTransform content = EnsureShopScroll(shopPanel);

            // All assets loaded after the scene switch (see DifficultyBuilder —
            // pre-switch instances go fake-null and wire as fileID 0).
            var rows = new MetaShopRowUI[AllUpgradeNames.Length];
            for (int i = 0; i < AllUpgradeNames.Length; i++)
            {
                var upgrade = AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>($"{MetaFolder}/{AllUpgradeNames[i]}.asset");
                if (upgrade == null)
                {
                    Debug.LogError($"MetaShopExpansionBuilder: upgrade asset '{AllUpgradeNames[i]}' missing.");
                    return;
                }

                rows[i] = EnsureCardInGrid(content, shopPanel, upgrade, i, font, panelSprite, buttonSprite);
            }

            content.sizeDelta = new Vector2(
                content.sizeDelta.x,
                Mathf.Ceil(AllUpgradeNames.Length / (float)GridColumns) * RowPitch + 20f);

            var shopUi = shopPanel.GetComponent<MetaShopUI>();
            var shopSerialized = new SerializedObject(shopUi);
            SerializedProperty rowsProp = shopSerialized.FindProperty("_rows");
            rowsProp.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                rowsProp.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
            }

            shopSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(shopUi);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // Scroll area between the balance line and the back button. Idempotent
        // in pieces so re-runs can retrofit an already-built scroll (the first
        // 1C pass shipped without an input surface or scrollbar and didn't
        // actually scroll under the pointer).
        private static RectTransform EnsureShopScroll(Transform shopPanel)
        {
            Transform existing = shopPanel.Find("ShopScroll");
            GameObject scrollGo;
            RectTransform viewport;
            RectTransform content;
            if (existing != null)
            {
                scrollGo = existing.gameObject;
                viewport = (RectTransform)existing.Find("Viewport");
                content = (RectTransform)viewport.Find("Content");
            }
            else
            {
                scrollGo = new GameObject("ShopScroll", typeof(RectTransform), typeof(ScrollRect));
                var scrollRect = (RectTransform)scrollGo.transform;
                scrollRect.SetParent(shopPanel, false);
                scrollRect.anchorMin = Vector2.zero;
                scrollRect.anchorMax = Vector2.one;
                scrollRect.offsetMin = new Vector2(20f, 170f);
                scrollRect.offsetMax = new Vector2(-20f, -210f);

                var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
                viewport = (RectTransform)viewportGo.transform;
                viewport.SetParent(scrollRect, false);
                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                viewport.offsetMin = Vector2.zero;
                viewport.offsetMax = Vector2.zero;

                var contentGo = new GameObject("Content", typeof(RectTransform));
                content = (RectTransform)contentGo.transform;
                content.SetParent(viewport, false);
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.anchoredPosition = Vector2.zero;
            }

            // Wheel and drag events only reach the ScrollRect when the pointer
            // is over one of ITS raycast targets — give the viewport an
            // invisible full-size surface so the whole area scrolls, not just
            // the exact pixels covered by card graphics.
            if (!viewport.TryGetComponent(out Image surface))
            {
                surface = viewport.gameObject.AddComponent<Image>();
            }

            surface.color = Color.clear;
            surface.raycastTarget = true;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 60f;
            scroll.verticalScrollbar = EnsureShopScrollbar(scrollGo.transform);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            return content;
        }

        // Visible draggable scrollbar down the right edge — both a grab handle
        // and the "there's more below" signal the cut-off grid alone didn't give.
        private static Scrollbar EnsureShopScrollbar(Transform shopScroll)
        {
            Transform existing = shopScroll.Find("Scrollbar");
            if (existing != null)
            {
                return existing.GetComponent<Scrollbar>();
            }

            var barGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            var barRect = (RectTransform)barGo.transform;
            barRect.SetParent(shopScroll, false);
            barRect.anchorMin = new Vector2(1f, 0f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(1f, 0.5f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(18f, 0f);

            var background = barGo.GetComponent<Image>();
            background.color = new Color(0.15f, 0.09f, 0.05f, 0.9f);

            var areaGo = new GameObject("Sliding Area", typeof(RectTransform));
            var area = (RectTransform)areaGo.transform;
            area.SetParent(barRect, false);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(2f, 2f);
            area.offsetMax = new Vector2(-2f, -2f);

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            var handle = (RectTransform)handleGo.transform;
            handle.SetParent(area, false);
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.offsetMin = Vector2.zero;
            handle.offsetMax = Vector2.zero;

            var handleImage = handleGo.GetComponent<Image>();
            handleImage.sprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelButton");
            handleImage.type = Image.Type.Sliced;
            handleImage.pixelsPerUnitMultiplier = 2f;
            handleImage.color = CombBrown;

            var scrollbar = barGo.GetComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            return scrollbar;
        }

        // Finds the card wherever it lives (panel root for pre-1C cards,
        // content for re-runs), reparents it into the grid, and positions it
        // by index. Missing cards are built via the shared Phase-4 factory.
        private static MetaShopRowUI EnsureCardInGrid(
            RectTransform content, Transform shopPanel, MetaUpgradeSO upgrade, int index,
            TMP_FontAsset font, Sprite panelSprite, Sprite buttonSprite)
        {
            string cardName = $"Card_{upgrade.name}";
            Transform card = content.Find(cardName);
            if (card == null)
            {
                card = shopPanel.Find(cardName);
            }

            MetaShopRowUI row;
            if (card == null)
            {
                row = Phase4MetaAndMenusBuilder.CreateShopCard(
                    content, upgrade, Vector2.zero, CardSize, font, panelSprite, buttonSprite);
                card = row.transform;
            }
            else
            {
                card.SetParent(content, false);
                row = card.GetComponent<MetaShopRowUI>();
            }

            var rect = (RectTransform)card;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            int column = index % GridColumns;
            int gridRow = index / GridColumns;
            rect.anchoredPosition = new Vector2(column == 0 ? -258f : 258f, -(215f + gridRow * RowPitch));
            rect.sizeDelta = CardSize;

            return row;
        }

        // ------------------------------------------------------------------
        // Beehive: reroll controls on the level-up offer screen.
        // ------------------------------------------------------------------
        private static void ApplyRunSceneChanges()
        {
            EditorSceneManager.OpenScene(RunScenePath, OpenSceneMode.Single);

            GameObject canvas = GameObject.Find("Canvas");
            Transform levelUpPanel = canvas != null ? canvas.transform.Find("LevelUpPanel") : null;
            if (levelUpPanel == null)
            {
                Debug.LogError("MetaShopExpansionBuilder: LevelUpPanel not found in Beehive.");
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite buttonSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelButton");
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            var rerollUpgrade = AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>(RerollUpgradePath);

            var rerollButtons = new Button[3];
            for (int i = 0; i < rerollButtons.Length; i++)
            {
                Transform choice = levelUpPanel.Find($"Choice{i}");
                if (choice == null)
                {
                    Debug.LogError($"MetaShopExpansionBuilder: Choice{i} not found on LevelUpPanel.");
                    return;
                }

                rerollButtons[i] = EnsureRerollButton(choice, font, buttonSprite);
            }

            TMP_Text countText = EnsureRerollCountText(levelUpPanel, font);

            var controller = levelUpPanel.GetComponent<LevelUpUIController>();
            var controllerSerialized = new SerializedObject(controller);
            controllerSerialized.FindProperty("_metaStore").objectReferenceValue = store;
            controllerSerialized.FindProperty("_rerollUpgrade").objectReferenceValue = rerollUpgrade;
            SerializedProperty buttonsProp = controllerSerialized.FindProperty("_rerollButtons");
            buttonsProp.arraySize = rerollButtons.Length;
            for (int i = 0; i < rerollButtons.Length; i++)
            {
                buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = rerollButtons[i];
            }

            controllerSerialized.FindProperty("_rerollCountText").objectReferenceValue = countText;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            WireApplier(store, rerollUpgrade);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static Button EnsureRerollButton(Transform choice, TMP_FontAsset font, Sprite buttonSprite)
        {
            Transform existing = choice.Find("RerollButton");
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            var go = new GameObject("RerollButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(choice, false);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 6f);
            rect.sizeDelta = new Vector2(130f, 32f);

            var image = go.GetComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = CombBrown;
            go.AddComponent<UIClickSfx>();

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = 16f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            label.text = "REROLL";

            // Hidden until the controller shows it for an offer with stock.
            go.SetActive(false);
            return go.GetComponent<Button>();
        }

        private static TMP_Text EnsureRerollCountText(Transform levelUpPanel, TMP_FontAsset font)
        {
            Transform existing = levelUpPanel.Find("RerollCount");
            if (existing != null)
            {
                return existing.GetComponent<TMP_Text>();
            }

            var go = new GameObject("RerollCount", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(levelUpPanel, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -34f);
            rect.sizeDelta = new Vector2(260f, 40f);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = 26f;
            text.color = Amber;
            text.alignment = TextAlignmentOptions.Right;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            text.text = string.Empty;
            go.SetActive(false);
            return text;
        }

        // The applier gets every non-reroll upgrade (rerolls are read by the
        // level-up controller) plus the PlayerExperience for the EXP-gain rank.
        private static void WireApplier(PersistentMetaProgressionStoreSO store, MetaUpgradeSO rerollUpgrade)
        {
            var applier = Object.FindAnyObjectByType<MetaUpgradeApplier>(FindObjectsInactive.Include);
            var experience = Object.FindAnyObjectByType<PlayerExperience>(FindObjectsInactive.Include);
            if (applier == null || experience == null)
            {
                Debug.LogError("MetaShopExpansionBuilder: MetaUpgradeApplier or PlayerExperience not found.");
                return;
            }

            var so = new SerializedObject(applier);
            SerializedProperty upgradesProp = so.FindProperty("_upgrades");
            upgradesProp.arraySize = AllUpgradeNames.Length - 1;
            int slot = 0;
            for (int i = 0; i < AllUpgradeNames.Length; i++)
            {
                var upgrade = AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>($"{MetaFolder}/{AllUpgradeNames[i]}.asset");
                if (upgrade == rerollUpgrade)
                {
                    continue;
                }

                upgradesProp.GetArrayElementAtIndex(slot).objectReferenceValue = upgrade;
                slot++;
            }

            so.FindProperty("_experience").objectReferenceValue = experience;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(applier);
        }
    }
}
