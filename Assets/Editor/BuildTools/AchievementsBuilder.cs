using System.Collections.Generic;
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
    /// PLAN 5D — achievements. Four passes, all find-or-create / idempotent:
    /// (1) the <see cref="AchievementSO"/> roster + <see cref="AchievementCatalogSO"/>
    /// (id/condition re-asserted every run; display/threshold/reward fields only
    /// authored on newly-created assets so hand tuning survives), (2) the
    /// AchievementRow list-row prefab, (3) the MainMenu AchievementsPanel
    /// (scroll list + counter in the shop's tabbed-panel mold) + AWARDS home
    /// button + controller wiring, and (4) the Beehive
    /// <see cref="AchievementTracker"/> + HUD unlock toast.
    /// Rebuilds only its own generated nodes.
    /// </summary>
    public static class AchievementsBuilder
    {
        private const string DataFolder = "Assets/Data/Achievements";
        private const string CatalogPath = DataFolder + "/AchievementCatalog.asset";
        private const string EntryPrefabPath = "Assets/Prefabs/UI/AchievementRow.prefab";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string RunScenePath = "Assets/Scenes/Beehive.unity";
        private const string PersistentStorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";
        private const string CosmeticCatalogPath = "Assets/Data/Cosmetics/CosmeticCatalog.asset";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";
        private const string SwatchSpritePath = "Assets/Sprites/Cosmetics/Swatch.png";

        // Honey/hive palette (mirrors CodexBuilder / CosmeticsBuilder).
        private static readonly Color HoneyGold = new Color(1f, 0.765f, 0.043f);
        private static readonly Color Amber = new Color(0.961f, 0.651f, 0.137f);
        private static readonly Color Wax = new Color(0.91f, 0.847f, 0.627f);
        private static readonly Color CombBrown = new Color(0.549f, 0.353f, 0.169f);
        private static readonly Color DeepBrown = new Color(0.227f, 0.141f, 0.086f);
        private static readonly Color RoyalCream = new Color(0.96f, 0.93f, 0.8f);

        private struct AchievementDef
        {
            public string Id;
            public AchievementConditionType Condition;
            public int Threshold;
            public string Name;
            public string Description;
            public int Jelly;
            public string CosmeticId;
        }

        // The 5D roster: every condition rides a signal the game already emits.
        // Jelly stays scarce (5B) — the whole roster pays out less than a few
        // first-clears; the Extreme clear also grants the Honey Crown cosmetic.
        private static readonly AchievementDef[] Achievements =
        {
            new AchievementDef { Id = "first_sting", Condition = AchievementConditionType.KillsInRun, Threshold = 1, Name = "First Sting", Description = "Defeat your first enemy.", Jelly = 1 },
            new AchievementDef { Id = "swarm_slayer", Condition = AchievementConditionType.KillsInRun, Threshold = 250, Name = "Swarm Slayer", Description = "Defeat 250 enemies in a single run.", Jelly = 2 },
            new AchievementDef { Id = "hive_scourge", Condition = AchievementConditionType.KillsInRun, Threshold = 1000, Name = "Scourge of the Swarm", Description = "Defeat 1,000 enemies in a single run.", Jelly = 5 },
            new AchievementDef { Id = "growing_wings", Condition = AchievementConditionType.ReachLevel, Threshold = 10, Name = "Growing Wings", Description = "Reach level 10 in a single run.", Jelly = 2 },
            new AchievementDef { Id = "ascended_drone", Condition = AchievementConditionType.ReachLevel, Threshold = 20, Name = "Ascended Drone", Description = "Reach level 20 in a single run.", Jelly = 4 },
            new AchievementDef { Id = "halfway_home", Condition = AchievementConditionType.SurviveSeconds, Threshold = 300, Name = "Halfway Home", Description = "Survive for 5 minutes.", Jelly = 2 },
            new AchievementDef { Id = "attuned", Condition = AchievementConditionType.SetTierActive, Threshold = 1, Name = "Elemental Attunement", Description = "Activate any elemental set bonus.", Jelly = 2 },
            new AchievementDef { Id = "full_resonance", Condition = AchievementConditionType.SetTierActive, Threshold = 3, Name = "Full Resonance", Description = "Reach the top tier of any elemental set.", Jelly = 5 },
            new AchievementDef { Id = "queenslayer", Condition = AchievementConditionType.ClearStage, Threshold = (int)DifficultyTier.Easy, Name = "Queenslayer", Description = "Clear a stage on any difficulty.", Jelly = 5 },
            new AchievementDef { Id = "battle_hardened", Condition = AchievementConditionType.ClearStage, Threshold = (int)DifficultyTier.Hard, Name = "Battle-Hardened", Description = "Clear a stage on Hard or above.", Jelly = 10 },
            new AchievementDef { Id = "apex_of_the_hive", Condition = AchievementConditionType.ClearStage, Threshold = (int)DifficultyTier.Extreme, Name = "Apex of the Hive", Description = "Clear a stage on Extreme difficulty.", Jelly = 15, CosmeticId = "hat_crown" },
        };

        [MenuItem("SurveHive/Build Achievements (5D)")]
        public static void Apply()
        {
            // New achievements.* keys → the authored string table (append-only).
            LocalizationBuilder.Apply();

            EnsureAchievementAssets();
            BuildEntryPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildMenuPanel();
            BuildRunWiring();

            Debug.Log("AchievementsBuilder: roster, Achievements panel, tracker, and toast built.");
        }

        // ------------------------------------------------------------------
        // 1) The AchievementSO roster + catalog. Structural fields (id +
        //    condition type) are re-asserted every run; display text, threshold
        //    and rewards are only authored when the asset is newly created, so
        //    balance tuning in the Inspector survives.
        // ------------------------------------------------------------------
        private static void EnsureAchievementAssets()
        {
            if (!AssetDatabase.IsValidFolder(DataFolder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Achievements");
            }

            var entries = new List<AchievementSO>(Achievements.Length);
            foreach (AchievementDef def in Achievements)
            {
                string path = $"{DataFolder}/{def.Id}.asset";
                var achievement = AssetDatabase.LoadAssetAtPath<AchievementSO>(path);
                bool isNew = achievement == null;
                if (isNew)
                {
                    achievement = ScriptableObject.CreateInstance<AchievementSO>();
                    AssetDatabase.CreateAsset(achievement, path);
                }

                var so = new SerializedObject(achievement);
                so.FindProperty("_achievementId").stringValue = def.Id;
                so.FindProperty("_conditionType").intValue = (int)def.Condition;

                if (isNew)
                {
                    so.FindProperty("_displayName").stringValue = def.Name;
                    so.FindProperty("_description").stringValue = def.Description;
                    so.FindProperty("_threshold").intValue = def.Threshold;
                    so.FindProperty("_jellyReward").intValue = def.Jelly;
                    so.FindProperty("_cosmeticRewardId").stringValue = def.CosmeticId ?? string.Empty;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(achievement);
                entries.Add(achievement);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<AchievementCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AchievementCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var catalogSo = new SerializedObject(catalog);
            SerializedProperty list = catalogSo.FindProperty("_achievements");
            list.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            }

            catalogSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        // ------------------------------------------------------------------
        // 2) List-row prefab: name + description left, reward line right, gold
        //    badge marking unlocked rows.
        // ------------------------------------------------------------------
        private static void BuildEntryPrefab()
        {
            Sprite panelSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelPanel");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            var swatch = AssetDatabase.LoadAssetAtPath<Sprite>(SwatchSpritePath);

            var rowGo = new GameObject("AchievementRow", typeof(RectTransform));
            var rowRect = (RectTransform)rowGo.transform;
            rowRect.sizeDelta = new Vector2(1400f, 130f);

            var layoutElement = rowGo.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 130f;

            Image bg = rowGo.AddComponent<Image>();
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 2f;
            bg.color = new Color(CombBrown.r, CombBrown.g, CombBrown.b, 0.85f);
            bg.raycastTarget = false;

            TMP_Text nameText = CreateRowText(rowRect, "Name", font, 30f, HoneyGold,
                new Vector2(28f, -16f), new Vector2(820f, 40f), TextAlignmentOptions.TopLeft);
            TMP_Text descriptionText = CreateRowText(rowRect, "Description", font, 22f, Wax,
                new Vector2(28f, -62f), new Vector2(820f, 56f), TextAlignmentOptions.TopLeft);

            var rewardGo = new GameObject("Reward", typeof(RectTransform));
            var rewardRect = (RectTransform)rewardGo.transform;
            rewardRect.SetParent(rowRect, false);
            rewardRect.anchorMin = new Vector2(1f, 0.5f);
            rewardRect.anchorMax = new Vector2(1f, 0.5f);
            rewardRect.pivot = new Vector2(1f, 0.5f);
            rewardRect.anchoredPosition = new Vector2(-84f, 0f);
            rewardRect.sizeDelta = new Vector2(460f, 60f);
            var rewardText = rewardGo.AddComponent<TextMeshProUGUI>();
            rewardText.font = font;
            rewardText.fontSize = 24f;
            rewardText.color = RoyalCream;
            rewardText.alignment = TextAlignmentOptions.Right;
            rewardText.raycastTarget = false;

            // Gold badge marking an unlocked row (right edge).
            var badgeGo = new GameObject("UnlockedBadge", typeof(RectTransform));
            var badgeRect = (RectTransform)badgeGo.transform;
            badgeRect.SetParent(rowRect, false);
            badgeRect.anchorMin = new Vector2(1f, 0.5f);
            badgeRect.anchorMax = new Vector2(1f, 0.5f);
            badgeRect.pivot = new Vector2(1f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-28f, 0f);
            badgeRect.sizeDelta = new Vector2(30f, 30f);
            Image badgeImage = badgeGo.AddComponent<Image>();
            badgeImage.sprite = swatch;
            badgeImage.color = HoneyGold;
            badgeImage.raycastTarget = false;
            badgeImage.enabled = false;

            var row = rowGo.AddComponent<AchievementEntryUI>();
            var rowSo = new SerializedObject(row);
            rowSo.FindProperty("_nameText").objectReferenceValue = nameText;
            rowSo.FindProperty("_descriptionText").objectReferenceValue = descriptionText;
            rowSo.FindProperty("_rewardText").objectReferenceValue = rewardText;
            rowSo.FindProperty("_unlockedBadge").objectReferenceValue = badgeImage;
            rowSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(row);

            PrefabUtility.SaveAsPrefabAsset(rowGo, EntryPrefabPath);
            Object.DestroyImmediate(rowGo);
        }

        // ------------------------------------------------------------------
        // 3) MainMenu: AWARDS home button + AchievementsPanel.
        // ------------------------------------------------------------------
        private static void BuildMenuPanel()
        {
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            // Load assets only after the scene switch (pre-switch instances
            // serialize as fileID 0).
            var entryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EntryPrefabPath)
                .GetComponent<AchievementEntryUI>();
            var catalog = AssetDatabase.LoadAssetAtPath<AchievementCatalogSO>(CatalogPath);
            var cosmeticCatalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalogSO>(CosmeticCatalogPath);
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite panelSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelPanel");
            Sprite buttonSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelButton");
            if (catalog == null || cosmeticCatalog == null || store == null || font == null
                || panelSprite == null || buttonSprite == null)
            {
                Debug.LogError("AchievementsBuilder: missing catalog/store/font/sprites for MainMenu.");
                return;
            }

            GameObject canvas = GameObject.Find("Canvas");
            var controllerGo = GameObject.Find("MainMenuController");
            var controller = controllerGo != null ? controllerGo.GetComponent<MainMenuController>() : null;
            Transform mainPanel = canvas != null ? canvas.transform.Find("MainPanel") : null;
            if (canvas == null || controller == null || mainPanel == null)
            {
                Debug.LogError("AchievementsBuilder: MainMenu canvas/controller/MainPanel not found.");
                return;
            }

            Button awardsButton = EnsureHomeAwardsButton(mainPanel, font, buttonSprite);
            RelayoutHomeButtons(mainPanel);

            // Rebuild our own panel sub-tree from scratch each run.
            Transform existingPanel = canvas.transform.Find("AchievementsPanel");
            if (existingPanel != null)
            {
                Object.DestroyImmediate(existingPanel.gameObject);
            }

            var panelGo = new GameObject("AchievementsPanel", typeof(RectTransform));
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
            title.text = Loc.Get(LocKeys.AchievementsTitle);

            // Top-right unlock counter (JellyText mold).
            var counterGo = new GameObject("CounterText", typeof(RectTransform));
            var counterRect = (RectTransform)counterGo.transform;
            counterRect.SetParent(panelRect, false);
            counterRect.anchorMin = Vector2.one;
            counterRect.anchorMax = Vector2.one;
            counterRect.pivot = Vector2.one;
            counterRect.anchoredPosition = new Vector2(-48f, -40f);
            counterRect.sizeDelta = new Vector2(500f, 60f);
            var counterText = counterGo.AddComponent<TextMeshProUGUI>();
            counterText.font = font;
            counterText.fontSize = 36f;
            counterText.color = RoyalCream;
            counterText.alignment = TextAlignmentOptions.Right;
            counterText.raycastTarget = false;
            counterText.text = Loc.Get(LocKeys.AchievementsUnlockedPrefix) + "0/0";

            Button backButton = CreateButton(panelRect, "BackButton", "BACK", font, buttonSprite,
                Vector2.zero, new Vector2(220f, 70f), 28f);
            var backRect = (RectTransform)backButton.transform;
            backRect.anchorMin = new Vector2(0f, 1f);
            backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(36f, -36f);

            RectTransform listContent = BuildScrollList(panelRect);

            var achievementsUi = panelGo.AddComponent<AchievementsUI>();
            var so = new SerializedObject(achievementsUi);
            so.FindProperty("_store").objectReferenceValue = store;
            so.FindProperty("_catalog").objectReferenceValue = catalog;
            so.FindProperty("_cosmeticCatalog").objectReferenceValue = cosmeticCatalog;
            so.FindProperty("_entryPrefab").objectReferenceValue = entryPrefab;
            so.FindProperty("_listContent").objectReferenceValue = listContent;
            so.FindProperty("_counterText").objectReferenceValue = counterText;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(achievementsUi);

            // Panels rest inactive; MainMenuController.Awake activates the home panel.
            panelGo.SetActive(false);

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("_achievementsPanel").objectReferenceValue = panelGo;
            controllerSo.FindProperty("_achievementsButton").objectReferenceValue = awardsButton;
            controllerSo.FindProperty("_achievementsBackButton").objectReferenceValue = backButton;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static Button EnsureHomeAwardsButton(Transform mainPanel, TMP_FontAsset font, Sprite buttonSprite)
        {
            Transform existing = mainPanel.Find("AwardsButton");
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            return CreateButton((RectTransform)mainPanel, "AwardsButton",
                Loc.Get(LocKeys.AchievementsMenuButton), font, buttonSprite,
                Vector2.zero, new Vector2(525f, 98f), 40f);
        }

        // Re-assert the bottom-left home stack with AWARDS slotted between
        // STYLE and SETTINGS (top→bottom: Play, Shop, Codex, Style, Awards,
        // Settings, Quit).
        private static void RelayoutHomeButtons(Transform mainPanel)
        {
            // The 7-row stack reaches higher than before — tuck the tagline up
            // under the title so PLAY no longer overlaps it.
            Transform subtitle = mainPanel.Find("Subtitle");
            if (subtitle != null)
            {
                ((RectTransform)subtitle).anchoredPosition = new Vector2(78f, -152f);
            }

            PlaceHomeButton(mainPanel, "PlayButton", 6);
            PlaceHomeButton(mainPanel, "ShopButton", 5);
            PlaceHomeButton(mainPanel, "CodexButton", 4);
            PlaceHomeButton(mainPanel, "StyleButton", 3);
            PlaceHomeButton(mainPanel, "AwardsButton", 2);
            PlaceHomeButton(mainPanel, "SettingsButton", 1);
            PlaceHomeButton(mainPanel, "QuitButton", 0);
        }

        // Mirrors CosmeticsBuilder.PlaceHomeButton.
        private static void PlaceHomeButton(Transform panel, string name, int rowFromBottom)
        {
            Transform button = panel.Find(name);
            if (button == null)
            {
                Debug.LogWarning($"AchievementsBuilder: home button '{name}' not found.");
                return;
            }

            var rect = (RectTransform)button;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(525f, 98f);
            rect.anchoredPosition = new Vector2(72f, 64f + rowFromBottom * 116f);
        }

        // Scrollable vertical row list (CosmeticsBuilder.BuildEntryGrid mold).
        private static RectTransform BuildScrollList(RectTransform panel)
        {
            var scrollGo = new GameObject("ListScroll", typeof(RectTransform));
            var scrollRect = (RectTransform)scrollGo.transform;
            scrollRect.SetParent(panel, false);
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.anchoredPosition = new Vector2(0f, -60f);
            scrollRect.sizeDelta = new Vector2(1560f, 760f);

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
            layout.spacing = 12f;
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            return content;
        }

        // ------------------------------------------------------------------
        // 4) Beehive: the run tracker + HUD unlock toast.
        // ------------------------------------------------------------------
        private static void BuildRunWiring()
        {
            EditorSceneManager.OpenScene(RunScenePath, OpenSceneMode.Single);

            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            var catalog = AssetDatabase.LoadAssetAtPath<AchievementCatalogSO>(CatalogPath);
            var cosmeticCatalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalogSO>(CosmeticCatalogPath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite panelSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelPanel");
            if (store == null || catalog == null || cosmeticCatalog == null || font == null
                || panelSprite == null)
            {
                Debug.LogError("AchievementsBuilder: store/catalog/font missing for Beehive wiring.");
                return;
            }

            var playerExperience = Object.FindFirstObjectByType<PlayerExperience>();
            if (playerExperience == null)
            {
                Debug.LogError("AchievementsBuilder: PlayerExperience not found in Beehive scene.");
                return;
            }

            GameObject trackerGo = GameObject.Find("AchievementTracker");
            if (trackerGo == null)
            {
                trackerGo = new GameObject("AchievementTracker");
            }

            if (!trackerGo.TryGetComponent(out AchievementTracker tracker))
            {
                tracker = trackerGo.AddComponent<AchievementTracker>();
            }

            var trackerSo = new SerializedObject(tracker);
            trackerSo.FindProperty("_store").objectReferenceValue = store;
            trackerSo.FindProperty("_catalog").objectReferenceValue = catalog;
            trackerSo.FindProperty("_playerExperience").objectReferenceValue = playerExperience;
            trackerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(trackerGo);

            BuildToast(cosmeticCatalog, font, panelSprite);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // Top-center HUD banner, alpha 0 at rest — rebuilt from scratch each run.
        private static void BuildToast(CosmeticCatalogSO cosmeticCatalog, TMP_FontAsset font, Sprite panelSprite)
        {
            GameObject canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                Debug.LogError("AchievementsBuilder: Beehive Canvas not found for the toast.");
                return;
            }

            Transform existing = canvasGo.transform.Find("AchievementToast");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var toastGo = new GameObject("AchievementToast", typeof(RectTransform));
            var toastRect = (RectTransform)toastGo.transform;
            toastRect.SetParent(canvasGo.transform, false);
            toastRect.anchorMin = new Vector2(0.5f, 1f);
            toastRect.anchorMax = new Vector2(0.5f, 1f);
            toastRect.pivot = new Vector2(0.5f, 1f);
            toastRect.anchoredPosition = new Vector2(0f, -170f);
            toastRect.sizeDelta = new Vector2(680f, 150f);

            var group = toastGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            Image bg = toastGo.AddComponent<Image>();
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 2f;
            bg.color = new Color(DeepBrown.r, DeepBrown.g, DeepBrown.b, 0.92f);
            bg.raycastTarget = false;

            TMP_Text titleText = CreateTopText(toastRect, "ToastTitle", font, 24f, HoneyGold,
                -16f, 30f, 640f, TextAlignmentOptions.Center);
            titleText.text = Loc.Get(LocKeys.AchievementsToastTitle);
            TMP_Text nameText = CreateTopText(toastRect, "ToastName", font, 34f, Color.white,
                -50f, 42f, 640f, TextAlignmentOptions.Center);
            TMP_Text rewardText = CreateTopText(toastRect, "ToastReward", font, 22f, RoyalCream,
                -98f, 32f, 640f, TextAlignmentOptions.Center);

            var toast = toastGo.AddComponent<AchievementToastUI>();
            var so = new SerializedObject(toast);
            so.FindProperty("_root").objectReferenceValue = group;
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_rewardText").objectReferenceValue = rewardText;
            so.FindProperty("_cosmeticCatalog").objectReferenceValue = cosmeticCatalog;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(toast);
        }

        // ------------------------------------------------------------------
        // Small UI factories (CodexBuilder mold).
        // ------------------------------------------------------------------
        private static TMP_Text CreateRowText(
            RectTransform parent, string name, TMP_FontAsset font, float fontSize, Color color,
            Vector2 topLeftOffset, Vector2 size, TextAlignmentOptions alignment)
        {
            var textGo = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)textGo.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeftOffset;
            rect.sizeDelta = size;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
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
