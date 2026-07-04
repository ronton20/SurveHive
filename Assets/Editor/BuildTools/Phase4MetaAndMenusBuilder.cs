using SurveHive.Core;
using SurveHive.Currency;
using SurveHive.Data;
using SurveHive.Health;
using SurveHive.Player;
using SurveHive.Progression;
using SurveHive.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Phase 4 (PLAN.md §6): meta & menus. Sub-phase 4A — save-file-backed meta
    /// store, the six flat-stat shop upgrade assets, and applying purchased
    /// ranks to the player at run start. Sub-phase 4B — MainMenu scene (home /
    /// world select / Hive Upgrades shop / settings shell) and results-screen
    /// routing back to the menu. 4C (pause/settings) extends this same pass.
    /// Additive over Phases 0-3; idempotent.
    /// </summary>
    public static class Phase4MetaAndMenusBuilder
    {
        private const string ScenePath = "Assets/Scenes/Beehive.unity";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string MetaFolder = "Assets/Data/Meta";
        private const string PersistentStorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";
        private const string UiKitTexturePath = "Assets/ThirdParty/PixelUI/UI SIMPLE PIXEL UNSPLIT.png";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";

        private static readonly Color HoneyGold = new Color(1f, 0.765f, 0.043f);
        private static readonly Color Amber = new Color(0.961f, 0.651f, 0.137f);
        private static readonly Color Wax = new Color(0.91f, 0.847f, 0.627f);
        private static readonly Color CombBrown = new Color(0.549f, 0.353f, 0.169f);
        private static readonly Color DeepBrown = new Color(0.227f, 0.141f, 0.086f);

        private static readonly string[] MetaUpgradePaths =
        {
            MetaFolder + "/MaxHealth.asset",
            MetaFolder + "/Damage.asset",
            MetaFolder + "/MoveSpeed.asset",
            MetaFolder + "/AttackSpeed.asset",
            MetaFolder + "/Magnet.asset",
            MetaFolder + "/CurrencyGain.asset",
        };

        [MenuItem("SurveHive/Apply Phase 4 Meta & Menus")]
        public static void Apply()
        {
            // 4A: persistent store + shop upgrade definitions.
            EnsurePersistentStore();
            EnsureMetaUpgradeAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4B: menu scene + build settings, then the run scene's routing.
            BuildMainMenuScene();
            EnsureBuildSettingsScenes();

            ApplySceneChanges();

            Debug.Log("SurveHive Phase 4 meta & menus build complete.");
        }

        // ------------------------------------------------------------------
        // 4A: assets.
        // ------------------------------------------------------------------
        private static void EnsurePersistentStore()
        {
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            if (store == null)
            {
                store = ScriptableObject.CreateInstance<PersistentMetaProgressionStoreSO>();
                AssetDatabase.CreateAsset(store, PersistentStorePath);
            }
        }

        private static void EnsureMetaUpgradeAssets()
        {
            EnsureFolder(MetaFolder);

            EnsureUpgrade("MaxHealth", "meta_max_health", "Thick Comb Walls",
                "Permanently raises max health.", MetaStatType.MaxHealth,
                maxRank: 10, baseCost: 50, costGrowth: 1.35f, effectPerRank: 10f);
            EnsureUpgrade("Damage", "meta_damage", "Royal Jelly Diet",
                "Permanently raises all damage.", MetaStatType.AttackDamage,
                maxRank: 10, baseCost: 60, costGrowth: 1.4f, effectPerRank: 4f);
            EnsureUpgrade("MoveSpeed", "meta_move_speed", "Stronger Wings",
                "Permanently raises move speed.", MetaStatType.MoveSpeed,
                maxRank: 5, baseCost: 40, costGrowth: 1.5f, effectPerRank: 2f);
            EnsureUpgrade("AttackSpeed", "meta_attack_speed", "Rapid Reflexes",
                "Permanently raises attack speed.", MetaStatType.AttackSpeed,
                maxRank: 8, baseCost: 60, costGrowth: 1.45f, effectPerRank: 3f);
            EnsureUpgrade("Magnet", "meta_magnet", "Nectar Scent",
                "Permanently widens pickup range.", MetaStatType.MagnetRadius,
                maxRank: 5, baseCost: 30, costGrowth: 1.5f, effectPerRank: 8f);
            EnsureUpgrade("CurrencyGain", "meta_currency_gain", "Honey Hoarder",
                "Permanently raises honey gained in runs.", MetaStatType.CurrencyGain,
                maxRank: 10, baseCost: 80, costGrowth: 1.5f, effectPerRank: 5f);
        }

        private static void EnsureUpgrade(
            string assetName, string id, string displayName, string description,
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
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_statType").enumValueIndex = (int)statType;
            so.FindProperty("_maxRank").intValue = maxRank;
            so.FindProperty("_baseCost").intValue = baseCost;
            so.FindProperty("_costGrowth").floatValue = costGrowth;
            so.FindProperty("_effectPerRank").floatValue = effectPerRank;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(upgrade);
        }

        // ------------------------------------------------------------------
        // 4B: MainMenu scene. Rebuilt from scratch every run (it is fully
        // generated, so regeneration IS the idempotency).
        // ------------------------------------------------------------------
        private static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite panelSprite = LoadUiKitSprite("PixelPanel");
            Sprite buttonSprite = LoadUiKitSprite("PixelButton");

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = DeepBrown;

            var canvasGo = new GameObject("Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            // Match height: the menu is a vertical stack, so the full 1920
            // reference height must always be visible — landscape (PC) included.
            // Portrait still gets the full 1080 width (scale is height-driven,
            // so a 1080×1920 screen resolves to exactly 1080 canvas width).
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            // --- Main (home) panel ---
            GameObject mainPanel = CreatePanel(canvasGo.transform, "MainPanel", panelSprite, new Vector2(900f, 1100f));

            CreateMenuText(mainPanel.transform, "Title", "SURVEHIVE", font, 110f, HoneyGold,
                anchorY: 1f, offsetY: -120f, new Vector2(860f, 150f));
            CreateMenuText(mainPanel.transform, "Subtitle", "Defend. Sting. Survive.", font, 34f, Wax,
                anchorY: 1f, offsetY: -270f, new Vector2(860f, 60f));

            Button playButton = CreateMenuButton(mainPanel.transform, "PlayButton", "PLAY",
                font, buttonSprite, new Vector2(0f, 120f));
            Button shopButton = CreateMenuButton(mainPanel.transform, "ShopButton", "HIVE UPGRADES",
                font, buttonSprite, new Vector2(0f, -30f));
            Button settingsButton = CreateMenuButton(mainPanel.transform, "SettingsButton", "SETTINGS",
                font, buttonSprite, new Vector2(0f, -180f));
            Button quitButton = CreateMenuButton(mainPanel.transform, "QuitButton", "QUIT",
                font, buttonSprite, new Vector2(0f, -330f));

            // --- World select panel ---
            GameObject worldPanel = CreatePanel(canvasGo.transform, "WorldSelectPanel", panelSprite, new Vector2(900f, 1100f));

            CreateMenuText(worldPanel.transform, "Title", "SELECT WORLD", font, 70f, HoneyGold,
                anchorY: 1f, offsetY: -100f, new Vector2(860f, 100f));

            Button beehiveButton = CreateMenuButton(worldPanel.transform, "BeehiveButton", "THE BEEHIVE",
                font, buttonSprite, new Vector2(0f, 200f));
            Button gardenButton = CreateMenuButton(worldPanel.transform, "GardenButton", "GARDEN - LOCKED",
                font, buttonSprite, new Vector2(0f, 50f));
            gardenButton.interactable = false;
            Button woodsButton = CreateMenuButton(worldPanel.transform, "WoodsButton", "WOODS - LOCKED",
                font, buttonSprite, new Vector2(0f, -100f));
            woodsButton.interactable = false;

            // Difficulty seam: locked to Normal until a difficulty system exists —
            // the dropdown reserves the UI spot and the flow around it.
            TMP_Dropdown difficultyDropdown = CreateDifficultyDropdown(worldPanel.transform, font, buttonSprite);

            Button worldBackButton = CreateMenuButton(worldPanel.transform, "BackButton", "BACK",
                font, buttonSprite, new Vector2(0f, -420f));

            // --- Hive Upgrades (shop) panel ---
            GameObject shopPanel = CreatePanel(canvasGo.transform, "ShopPanel", panelSprite, new Vector2(1000f, 1700f));

            CreateMenuText(shopPanel.transform, "Title", "HIVE UPGRADES", font, 70f, HoneyGold,
                anchorY: 1f, offsetY: -70f, new Vector2(940f, 100f));
            TMP_Text balanceText = CreateMenuText(shopPanel.transform, "BalanceText", "HONEY: 0", font, 44f, Amber,
                anchorY: 1f, offsetY: -170f, new Vector2(940f, 70f));

            var upgrades = new MetaUpgradeSO[MetaUpgradePaths.Length];
            var rows = new MetaShopRowUI[MetaUpgradePaths.Length];
            for (int i = 0; i < MetaUpgradePaths.Length; i++)
            {
                upgrades[i] = AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>(MetaUpgradePaths[i]);
                rows[i] = CreateShopRow(shopPanel.transform, upgrades[i], i, font, panelSprite, buttonSprite);
            }

            Button shopBackButton = CreateMenuButton(shopPanel.transform, "BackButton", "BACK",
                font, buttonSprite, new Vector2(0f, -730f));

            MetaShopUI shopUi = shopPanel.AddComponent<MetaShopUI>();
            var shopSerialized = new SerializedObject(shopUi);
            shopSerialized.FindProperty("_store").objectReferenceValue = store;
            shopSerialized.FindProperty("_balanceText").objectReferenceValue = balanceText;
            SerializedProperty rowsProp = shopSerialized.FindProperty("_rows");
            rowsProp.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                rowsProp.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
            }

            shopSerialized.ApplyModifiedPropertiesWithoutUndo();

            // --- Settings panel (4C: real controls, shared with the pause menu) ---
            GameObject settingsPanel = CreatePanel(canvasGo.transform, "SettingsPanel", panelSprite, new Vector2(900f, 1100f));

            CreateMenuText(settingsPanel.transform, "Title", "SETTINGS", font, 70f, HoneyGold,
                anchorY: 1f, offsetY: -100f, new Vector2(860f, 100f));
            BuildSettingsControls(settingsPanel, store, font, panelSprite, buttonSprite);
            Button settingsBackButton = CreateMenuButton(settingsPanel.transform, "BackButton", "BACK",
                font, buttonSprite, new Vector2(0f, -430f));

            // --- Controller ---
            var controllerGo = new GameObject("MainMenuController");
            var controller = controllerGo.AddComponent<MainMenuController>();
            var controllerSerialized = new SerializedObject(controller);
            controllerSerialized.FindProperty("_mainPanel").objectReferenceValue = mainPanel;
            controllerSerialized.FindProperty("_worldSelectPanel").objectReferenceValue = worldPanel;
            controllerSerialized.FindProperty("_shopPanel").objectReferenceValue = shopPanel;
            controllerSerialized.FindProperty("_settingsPanel").objectReferenceValue = settingsPanel;
            controllerSerialized.FindProperty("_playButton").objectReferenceValue = playButton;
            controllerSerialized.FindProperty("_shopButton").objectReferenceValue = shopButton;
            controllerSerialized.FindProperty("_settingsButton").objectReferenceValue = settingsButton;
            controllerSerialized.FindProperty("_quitButton").objectReferenceValue = quitButton;
            controllerSerialized.FindProperty("_worldSelectBackButton").objectReferenceValue = worldBackButton;
            controllerSerialized.FindProperty("_shopBackButton").objectReferenceValue = shopBackButton;
            controllerSerialized.FindProperty("_settingsBackButton").objectReferenceValue = settingsBackButton;
            controllerSerialized.FindProperty("_startBeehiveButton").objectReferenceValue = beehiveButton;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            // Non-home panels start inactive; the controller re-asserts this in
            // Awake, but the saved scene should match the resting state.
            worldPanel.SetActive(false);
            shopPanel.SetActive(false);
            settingsPanel.SetActive(false);

            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        private static void EnsureBuildSettingsScenes()
        {
            var beehiveScene = new EditorBuildSettingsScene(ScenePath, true);
            var menuScene = new EditorBuildSettingsScene(MenuScenePath, true);
            // Menu first: it is the boot scene of a built player.
            EditorBuildSettings.scenes = new[] { menuScene, beehiveScene };
        }

        private static GameObject CreatePanel(Transform canvas, string name, Sprite panelSprite, Vector2 size)
        {
            var panelGo = new GameObject(name, typeof(RectTransform));
            panelGo.transform.SetParent(canvas, false);

            var rect = (RectTransform)panelGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            Image image = panelGo.AddComponent<Image>();
            image.sprite = panelSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = new Color(DeepBrown.r, DeepBrown.g, DeepBrown.b, 0.97f);
            image.raycastTarget = false;

            return panelGo;
        }

        private static TMP_Text CreateMenuText(
            Transform parent, string name, string text, TMP_FontAsset font, float fontSize, Color color,
            float anchorY, float offsetY, Vector2 size)
        {
            var textGo = new GameObject(name, typeof(RectTransform));
            textGo.transform.SetParent(parent, false);

            var rect = (RectTransform)textGo.transform;
            rect.anchorMin = new Vector2(0.5f, anchorY);
            rect.anchorMax = new Vector2(0.5f, anchorY);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, offsetY);
            rect.sizeDelta = size;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            tmp.text = text;
            return tmp;
        }

        private static Button CreateMenuButton(
            Transform parent, string name, string label, TMP_FontAsset font, Sprite buttonSprite,
            Vector2 centerOffset)
        {
            return CreateButton(parent, name, label, font, buttonSprite, centerOffset, new Vector2(620f, 120f), 40f);
        }

        private static Button CreateButton(
            Transform parent, string name, string label, TMP_FontAsset font, Sprite buttonSprite,
            Vector2 centerOffset, Vector2 size, float fontSize)
        {
            var buttonGo = new GameObject(name, typeof(RectTransform));
            buttonGo.transform.SetParent(parent, false);

            var rect = (RectTransform)buttonGo.transform;
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

            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.7f);
            colors.pressedColor = Amber;
            colors.disabledColor = new Color(0.45f, 0.42f, 0.38f);
            button.colors = colors;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(buttonGo.transform, false);
            var labelRect = (RectTransform)labelGo.transform;
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

        private static TMP_Dropdown CreateDifficultyDropdown(Transform parent, TMP_FontAsset font, Sprite buttonSprite)
        {
            var resources = new TMP_DefaultControls.Resources();
            GameObject dropdownGo = TMP_DefaultControls.CreateDropdown(resources);
            dropdownGo.name = "DifficultyDropdown";
            dropdownGo.transform.SetParent(parent, false);

            var rect = (RectTransform)dropdownGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -260f);
            rect.sizeDelta = new Vector2(620f, 90f);

            var dropdown = dropdownGo.GetComponent<TMP_Dropdown>();
            dropdown.options.Clear();
            dropdown.options.Add(new TMP_Dropdown.OptionData("NORMAL"));
            dropdown.value = 0;
            // Single option until a difficulty system exists.
            dropdown.interactable = false;

            var image = dropdownGo.GetComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = CombBrown;

            foreach (TMP_Text text in dropdownGo.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = font;
                text.fontSize = 34f;
                text.color = Wax;
            }

            return dropdown;
        }

        private static MetaShopRowUI CreateShopRow(
            Transform parent, MetaUpgradeSO upgrade, int index, TMP_FontAsset font,
            Sprite panelSprite, Sprite buttonSprite)
        {
            var rowGo = new GameObject($"Row_{upgrade.name}", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);

            var rect = (RectTransform)rowGo.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -240f - (index * 180f));
            rect.sizeDelta = new Vector2(940f, 170f);

            Image bg = rowGo.AddComponent<Image>();
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 2f;
            bg.color = new Color(CombBrown.r, CombBrown.g, CombBrown.b, 0.85f);
            bg.raycastTarget = false;

            TMP_Text nameText = CreateRowText(rowGo.transform, "Name", font, 38f, HoneyGold,
                new Vector2(20f, -14f), new Vector2(620f, 50f), TextAlignmentOptions.Left);
            TMP_Text descText = CreateRowText(rowGo.transform, "Description", font, 26f, Wax,
                new Vector2(20f, -66f), new Vector2(620f, 46f), TextAlignmentOptions.Left);
            TMP_Text rankText = CreateRowText(rowGo.transform, "Rank", font, 28f, Amber,
                new Vector2(20f, -116f), new Vector2(620f, 44f), TextAlignmentOptions.Left);

            Button buyButton = CreateButton(rowGo.transform, "BuyButton", "BUY", font, buttonSprite,
                Vector2.zero, new Vector2(220f, 90f), 34f);
            var buyRect = (RectTransform)buyButton.transform;
            buyRect.anchorMin = new Vector2(1f, 0.5f);
            buyRect.anchorMax = new Vector2(1f, 0.5f);
            buyRect.pivot = new Vector2(1f, 0.5f);
            buyRect.anchoredPosition = new Vector2(-20f, 20f);

            TMP_Text costText = CreateRowText(rowGo.transform, "Cost", font, 30f, Amber,
                Vector2.zero, new Vector2(220f, 44f), TextAlignmentOptions.Center);
            var costRect = (RectTransform)costText.transform;
            costRect.anchorMin = new Vector2(1f, 0.5f);
            costRect.anchorMax = new Vector2(1f, 0.5f);
            costRect.pivot = new Vector2(1f, 1f);
            costRect.anchoredPosition = new Vector2(-20f, -30f);

            var row = rowGo.AddComponent<MetaShopRowUI>();
            var rowSerialized = new SerializedObject(row);
            rowSerialized.FindProperty("_upgrade").objectReferenceValue = upgrade;
            rowSerialized.FindProperty("_nameText").objectReferenceValue = nameText;
            rowSerialized.FindProperty("_descriptionText").objectReferenceValue = descText;
            rowSerialized.FindProperty("_rankText").objectReferenceValue = rankText;
            rowSerialized.FindProperty("_costText").objectReferenceValue = costText;
            rowSerialized.FindProperty("_buyButton").objectReferenceValue = buyButton;
            rowSerialized.ApplyModifiedPropertiesWithoutUndo();

            return row;
        }

        private static TMP_Text CreateRowText(
            Transform parent, string name, TMP_FontAsset font, float fontSize, Color color,
            Vector2 topLeftOffset, Vector2 size, TextAlignmentOptions alignment)
        {
            var textGo = new GameObject(name, typeof(RectTransform));
            textGo.transform.SetParent(parent, false);

            var rect = (RectTransform)textGo.transform;
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
            tmp.text = name;
            return tmp;
        }

        // ------------------------------------------------------------------
        // 4C: settings controls (shared by menu + pause) and the pause menu.
        // ------------------------------------------------------------------
        private static void BuildSettingsControls(
            GameObject parent, PersistentMetaProgressionStoreSO store, TMP_FontAsset font,
            Sprite panelSprite, Sprite buttonSprite)
        {
            CreateMenuText(parent.transform, "MusicLabel", "MUSIC", font, 34f, Wax,
                anchorY: 0.5f, offsetY: 280f, new Vector2(620f, 44f));
            Slider musicSlider = CreateSlider(parent.transform, "MusicSlider", panelSprite, buttonSprite,
                new Vector2(0f, 195f));

            CreateMenuText(parent.transform, "SfxLabel", "SFX", font, 34f, Wax,
                anchorY: 0.5f, offsetY: 110f, new Vector2(620f, 44f));
            Slider sfxSlider = CreateSlider(parent.transform, "SfxSlider", panelSprite, buttonSprite,
                new Vector2(0f, 25f));

            Button vibrationButton = CreateMenuButton(parent.transform, "VibrationButton", "VIBRATION: ON",
                font, buttonSprite, new Vector2(0f, -90f));
            Button qualityButton = CreateMenuButton(parent.transform, "QualityButton", "QUALITY: DEFAULT",
                font, buttonSprite, new Vector2(0f, -230f));

            var settingsUi = parent.AddComponent<SettingsPanelUI>();
            var serialized = new SerializedObject(settingsUi);
            serialized.FindProperty("_store").objectReferenceValue = store;
            serialized.FindProperty("_musicSlider").objectReferenceValue = musicSlider;
            serialized.FindProperty("_sfxSlider").objectReferenceValue = sfxSlider;
            serialized.FindProperty("_vibrationButton").objectReferenceValue = vibrationButton;
            serialized.FindProperty("_vibrationLabel").objectReferenceValue =
                vibrationButton.GetComponentInChildren<TMP_Text>(true);
            serialized.FindProperty("_qualityButton").objectReferenceValue = qualityButton;
            serialized.FindProperty("_qualityLabel").objectReferenceValue =
                qualityButton.GetComponentInChildren<TMP_Text>(true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Slider CreateSlider(
            Transform parent, string name, Sprite trackSprite, Sprite handleSprite, Vector2 centerOffset)
        {
            var sliderGo = new GameObject(name, typeof(RectTransform));
            sliderGo.transform.SetParent(parent, false);

            var rect = (RectTransform)sliderGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = centerOffset;
            rect.sizeDelta = new Vector2(620f, 50f);

            var backgroundGo = new GameObject("Background", typeof(RectTransform));
            backgroundGo.transform.SetParent(sliderGo.transform, false);
            var backgroundRect = (RectTransform)backgroundGo.transform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = backgroundGo.AddComponent<Image>();
            backgroundImage.sprite = trackSprite;
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.pixelsPerUnitMultiplier = 2f;
            backgroundImage.color = new Color(DeepBrown.r * 0.6f, DeepBrown.g * 0.6f, DeepBrown.b * 0.6f);

            var fillAreaGo = new GameObject("FillArea", typeof(RectTransform));
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = (RectTransform)fillAreaGo.transform;
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(10f, 10f);
            fillAreaRect.offsetMax = new Vector2(-10f, -10f);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRect = (RectTransform)fillGo.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fillGo.AddComponent<Image>();
            fillImage.sprite = trackSprite;
            fillImage.type = Image.Type.Sliced;
            fillImage.pixelsPerUnitMultiplier = 2f;
            fillImage.color = HoneyGold;

            var handleAreaGo = new GameObject("HandleSlideArea", typeof(RectTransform));
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            var handleAreaRect = (RectTransform)handleAreaGo.transform;
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(20f, 0f);
            handleAreaRect.offsetMax = new Vector2(-20f, 0f);

            var handleGo = new GameObject("Handle", typeof(RectTransform));
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRect = (RectTransform)handleGo.transform;
            handleRect.sizeDelta = new Vector2(44f, 70f);
            Image handleImage = handleGo.AddComponent<Image>();
            handleImage.sprite = handleSprite;
            handleImage.type = Image.Type.Sliced;
            handleImage.pixelsPerUnitMultiplier = 2f;
            handleImage.color = Amber;

            Slider slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
        }

        // Pause menu in the run scene: HUD pause button + frozen-run panel with
        // resume / settings / abandon. Fully regenerated each run (destroy +
        // rebuild) — regeneration IS the idempotency.
        private static void BuildPauseMenu(PersistentMetaProgressionStoreSO store)
        {
            Transform canvas = GameObject.Find("Canvas").transform;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite panelSprite = LoadUiKitSprite("PixelPanel");
            Sprite buttonSprite = LoadUiKitSprite("PixelButton");

            Transform existing = canvas.Find("PauseRoot");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var rootGo = new GameObject("PauseRoot", typeof(RectTransform));
            rootGo.transform.SetParent(canvas, false);
            var rootRect = (RectTransform)rootGo.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // HUD pause button, top-right below the counters.
            Button pauseButton = CreateButton(rootGo.transform, "PauseButton", "II", font, buttonSprite,
                Vector2.zero, new Vector2(90f, 90f), 36f);
            var pauseButtonRect = (RectTransform)pauseButton.transform;
            pauseButtonRect.anchorMin = new Vector2(1f, 1f);
            pauseButtonRect.anchorMax = new Vector2(1f, 1f);
            pauseButtonRect.pivot = new Vector2(1f, 1f);
            pauseButtonRect.anchoredPosition = new Vector2(-25f, -140f);

            // Frozen-run panel.
            GameObject pausePanel = CreatePanel(rootGo.transform, "PausePanel", panelSprite, new Vector2(760f, 900f));
            CreateMenuText(pausePanel.transform, "Title", "PAUSED", font, 80f, HoneyGold,
                anchorY: 1f, offsetY: -60f, new Vector2(700f, 110f));
            Button resumeButton = CreateMenuButton(pausePanel.transform, "ResumeButton", "RESUME",
                font, buttonSprite, new Vector2(0f, 120f));
            Button settingsButton = CreateMenuButton(pausePanel.transform, "SettingsButton", "SETTINGS",
                font, buttonSprite, new Vector2(0f, -30f));
            Button abandonButton = CreateMenuButton(pausePanel.transform, "AbandonButton", "ABANDON RUN",
                font, buttonSprite, new Vector2(0f, -180f));

            // Settings sub-panel over the pause panel.
            GameObject settingsPanel = CreatePanel(rootGo.transform, "PauseSettingsPanel", panelSprite, new Vector2(820f, 1000f));
            CreateMenuText(settingsPanel.transform, "Title", "SETTINGS", font, 64f, HoneyGold,
                anchorY: 1f, offsetY: -50f, new Vector2(760f, 90f));
            BuildSettingsControls(settingsPanel, store, font, panelSprite, buttonSprite);
            Button settingsBackButton = CreateMenuButton(settingsPanel.transform, "BackButton", "BACK",
                font, buttonSprite, new Vector2(0f, -380f));

            var controller = rootGo.AddComponent<PauseMenuController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("_pausePanel").objectReferenceValue = pausePanel;
            serialized.FindProperty("_settingsPanel").objectReferenceValue = settingsPanel;
            serialized.FindProperty("_pauseButton").objectReferenceValue = pauseButton;
            serialized.FindProperty("_resumeButton").objectReferenceValue = resumeButton;
            serialized.FindProperty("_settingsButton").objectReferenceValue = settingsButton;
            serialized.FindProperty("_settingsBackButton").objectReferenceValue = settingsBackButton;
            serialized.FindProperty("_abandonButton").objectReferenceValue = abandonButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            pausePanel.SetActive(false);
            settingsPanel.SetActive(false);
        }

        private static Sprite LoadUiKitSprite(string spriteName)
        {
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(UiKitTexturePath);
            for (int i = 0; i < subAssets.Length; i++)
            {
                if (subAssets[i] is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            Debug.LogError($"Phase4: UI kit sprite '{spriteName}' not found.");
            return null;
        }

        // ------------------------------------------------------------------
        // 4A: scene wiring.
        // ------------------------------------------------------------------
        private static void ApplySceneChanges()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Reload by path after Refresh/OpenScene — instances held across the
            // asset-database refresh come back destroyed and serialize as null.
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            var upgrades = new MetaUpgradeSO[MetaUpgradePaths.Length];
            for (int i = 0; i < MetaUpgradePaths.Length; i++)
            {
                upgrades[i] = AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>(MetaUpgradePaths[i]);
            }

            GameObject playerGo = GameObject.Find("Player");
            GameObject bootstrapGo = GameObject.Find("GameBootstrap");

            // RunSession: persistent store + player level source for best-run stats.
            var session = bootstrapGo.GetComponent<RunSession>();
            var sessionSerialized = new SerializedObject(session);
            sessionSerialized.FindProperty("_metaProgressionStore").objectReferenceValue = store;
            sessionSerialized.FindProperty("_playerExperience").objectReferenceValue =
                playerGo.GetComponent<PlayerExperience>();
            sessionSerialized.ApplyModifiedPropertiesWithoutUndo();

            // Player: apply purchased meta ranks at run start.
            if (!playerGo.TryGetComponent(out MetaUpgradeApplier applier))
            {
                applier = playerGo.AddComponent<MetaUpgradeApplier>();
            }

            var applierSerialized = new SerializedObject(applier);
            applierSerialized.FindProperty("_store").objectReferenceValue = store;
            applierSerialized.FindProperty("_stats").objectReferenceValue =
                playerGo.GetComponent<PlayerStats>();
            applierSerialized.FindProperty("_health").objectReferenceValue =
                playerGo.GetComponent<HealthComponent>();
            applierSerialized.FindProperty("_wallet").objectReferenceValue =
                bootstrapGo.GetComponent<RunCurrencyWallet>();

            SerializedProperty upgradeList = applierSerialized.FindProperty("_upgrades");
            upgradeList.arraySize = upgrades.Length;
            for (int i = 0; i < upgrades.Length; i++)
            {
                upgradeList.GetArrayElementAtIndex(i).objectReferenceValue = upgrades[i];
            }

            applierSerialized.ApplyModifiedPropertiesWithoutUndo();

            // 4B: results screens route back to the menu (and retry via button).
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite buttonSprite = LoadUiKitSprite("PixelButton");
            AddResultsRouting("GameOverPanel", "GameOverHint", "Press R to retry", font, buttonSprite);
            AddResultsRouting("VictoryPanel", "VictoryHint",
                "The Queen has fallen.\nPress R to fly again", font, buttonSprite);

            // 4C: in-run pause menu with the shared settings block.
            BuildPauseMenu(store);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // Adds RETRY + HIVE buttons to an end-of-run panel and rewrites its hint
        // (tap-to-restart is gone — a tap would race the new buttons).
        private static void AddResultsRouting(
            string panelName, string hintName, string hintText, TMP_FontAsset font, Sprite buttonSprite)
        {
            Transform canvas = GameObject.Find("Canvas").transform;
            Transform panel = FindChildIncludingInactive(canvas, panelName);
            if (panel == null)
            {
                Debug.LogError($"Phase4: panel '{panelName}' not found for results routing.");
                return;
            }

            // The victory panel is a fixed 760×420 card (Phase 3) — too cramped
            // for the stats block + hint + button row. Grow it; the game-over
            // panel is a fullscreen overlay and needs no resize.
            if (panelName == "VictoryPanel")
            {
                ((RectTransform)panel).sizeDelta = new Vector2(760f, 700f);
            }

            // Hint sits above the button row at the panel bottom (clear of the
            // center stats block on both panels).
            Transform hint = FindChildIncludingInactive(panel, hintName);
            if (hint != null && hint.TryGetComponent(out TMP_Text hintTmp))
            {
                hintTmp.text = hintText;
                var hintRect = (RectTransform)hint;
                hintRect.anchorMin = new Vector2(0.5f, 0f);
                hintRect.anchorMax = new Vector2(0.5f, 0f);
                hintRect.pivot = new Vector2(0.5f, 0f);
                hintRect.anchoredPosition = new Vector2(0f, 150f);
                hintRect.sizeDelta = new Vector2(700f, 80f);
            }

            // Button row inside the panel's bottom edge.
            Transform existingRetry = FindChildIncludingInactive(panel, "RetryButton");
            Button retryButton = existingRetry != null
                ? existingRetry.GetComponent<Button>()
                : CreateButton(panel, "RetryButton", "RETRY", font, buttonSprite,
                    Vector2.zero, new Vector2(280f, 100f), 36f);
            var retryRect = (RectTransform)retryButton.transform;
            retryRect.anchorMin = new Vector2(0.5f, 0f);
            retryRect.anchorMax = new Vector2(0.5f, 0f);
            retryRect.pivot = new Vector2(1f, 0f);
            retryRect.anchoredPosition = new Vector2(-15f, 30f);

            Transform existingMenu = FindChildIncludingInactive(panel, "MenuButton");
            Button menuButton = existingMenu != null
                ? existingMenu.GetComponent<Button>()
                : CreateButton(panel, "MenuButton", "HIVE", font, buttonSprite,
                    Vector2.zero, new Vector2(280f, 100f), 36f);
            var menuRect = (RectTransform)menuButton.transform;
            menuRect.anchorMin = new Vector2(0.5f, 0f);
            menuRect.anchorMax = new Vector2(0.5f, 0f);
            menuRect.pivot = new Vector2(0f, 0f);
            menuRect.anchoredPosition = new Vector2(15f, 30f);

            WireSceneLoadButton(retryButton, "Beehive");
            WireSceneLoadButton(menuButton, "MainMenu");
        }

        private static void WireSceneLoadButton(Button button, string sceneName)
        {
            if (!button.TryGetComponent(out SceneLoadButton loader))
            {
                loader = button.gameObject.AddComponent<SceneLoadButton>();
            }

            var serialized = new SerializedObject(loader);
            serialized.FindProperty("_sceneName").stringValue = sceneName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindChildIncludingInactive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }

                Transform found = FindChildIncludingInactive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(assetFolderPath)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(assetFolderPath);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
