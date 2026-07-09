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
    /// item drop rate, and the per-run power-up reroll stock) and the level-up
    /// screen's reroll controls (per-card REROLL buttons + remaining count).
    /// Additive and idempotent over the existing scenes/assets.
    ///
    /// The menu-scene shop layout this pass originally built (a scrollable 2-column
    /// card grid) was superseded by the 3B-1 tabbed layout (<see cref="MetaShopTabsBuilder"/>),
    /// so the card/scroll code was removed; this pass now only authors the upgrade
    /// assets and the in-run reroll controls.
    /// </summary>
    public static class MetaShopExpansionBuilder
    {
        private const string MetaFolder = "Assets/Data/Meta";
        private const string PersistentStorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";
        private const string RerollUpgradePath = MetaFolder + "/Rerolls.asset";
        private const string RunScenePath = "Assets/Scenes/Beehive.unity";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";

        private static readonly Color Amber = new Color(0.961f, 0.651f, 0.137f);
        private static readonly Color CombBrown = new Color(0.549f, 0.353f, 0.169f);

        // Every upgrade id, in catalog order — read by WireApplier to hand the
        // player every non-reroll upgrade. (The menu-scene layout that also used
        // this list moved to MetaShopTabsBuilder.)
        private static readonly string[] AllUpgradeNames =
        {
            "MaxHealth", "Damage", "MoveSpeed", "AttackSpeed", "Magnet", "CurrencyGain",
            "ExpGain", "AbilityPower", "CooldownReduction", "CritChance", "CritDamage",
            "ItemDropRate", "Rerolls",
        };

        [MenuItem("SurveHive/Apply Meta Shop Expansion (Phase 1C)")]
        public static void Apply()
        {
            EnsureNewUpgradeAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

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
