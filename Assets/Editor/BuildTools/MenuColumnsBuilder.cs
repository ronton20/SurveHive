using SurveHive.Core;
using SurveHive.Data;
using SurveHive.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Splits the MainMenu home screen into two columns and folds the daily-deals
    /// shop into Hive Style. Authoritative, idempotent, and meant to run <b>last</b>
    /// (after the 5A–5E menu passes):
    ///
    /// - <b>Left (game):</b> Play, Codex, Awards, Settings, Quit — re-asserted in
    ///   the bottom-left stack.
    /// - <b>Right (player):</b> a framed showcase of the equipped bee over the
    ///   HIVE UPGRADES + STYLE buttons in the bottom-right.
    /// - The old home DEALS button is removed; a flashing "Daily Deals!"
    ///   call-to-action is added top-right <i>inside</i> the Hive Style panel, and
    ///   the controller's <c>_dealsButton</c> is repointed to it (Deals' Back now
    ///   returns to Style via <see cref="MainMenuController.CloseDeals"/>).
    ///
    /// Reuses the 5C cosmetic catalog + bee library — no new art. Rebuilds only
    /// its own generated nodes (equipped preview + flash button) and repositions
    /// pre-existing home buttons.
    /// </summary>
    public static class MenuColumnsBuilder
    {
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string CatalogPath = "Assets/Data/Cosmetics/CosmeticCatalog.asset";
        private const string PersistentStorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";
        private const string BeeLibraryPath = "Assets/ThirdParty/PixelFantasy/PixelMonsters/Pack1/Bee/YellowBee.asset";

        private const string PreviewNodeName = "EquippedBeePreview";
        private const string FlashButtonName = "DailyDealsFlashButton";

        // Home-button geometry (mirrors PcMenuLayoutBuilder / the 5x builders).
        private static readonly Vector2 HomeButtonSize = new Vector2(525f, 98f);
        private const float HomeButtonPitch = 116f;
        private const float SideMargin = 72f;
        private const float BottomMargin = 64f;

        // Honey/hive palette (mirrors CosmeticsBuilder / RotatingShopBuilder).
        private static readonly Color HoneyGold = new Color(1f, 0.765f, 0.043f);
        private static readonly Color Amber = new Color(0.961f, 0.651f, 0.137f);
        private static readonly Color DeepBrown = new Color(0.227f, 0.141f, 0.086f);

        [MenuItem("SurveHive/Split Menu Into Columns")]
        public static void Apply()
        {
            // New deals.flash_button key → the authored string table (append-only).
            LocalizationBuilder.Apply();

            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            // Load assets only after the scene switch (pre-switch instances
            // serialize as fileID 0).
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite panelSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelPanel");
            Sprite buttonSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelButton");
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            var catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalogSO>(CatalogPath);
            var beeLibrary = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(BeeLibraryPath);
            if (font == null || panelSprite == null || buttonSprite == null
                || store == null || catalog == null)
            {
                Debug.LogError("MenuColumnsBuilder: missing font/sprites/store/catalog for MainMenu.");
                return;
            }

            GameObject canvas = GameObject.Find("Canvas");
            var controllerGo = GameObject.Find("MainMenuController");
            var controller = controllerGo != null ? controllerGo.GetComponent<MainMenuController>() : null;
            Transform mainPanel = canvas != null ? canvas.transform.Find("MainPanel") : null;
            Transform stylePanel = canvas != null ? canvas.transform.Find("HiveStylePanel") : null;
            if (canvas == null || controller == null || mainPanel == null || stylePanel == null)
            {
                Debug.LogError("MenuColumnsBuilder: MainMenu canvas/controller/MainPanel/HiveStylePanel not found.");
                return;
            }

            LayoutLeftColumn(mainPanel);
            RemoveHomeDealsButton(mainPanel);
            LayoutRightColumn(mainPanel, panelSprite, store, catalog, beeLibrary);

            Button flashButton = BuildFlashButton((RectTransform)stylePanel, font, buttonSprite);
            NudgeStyleJelly(stylePanel);

            // Repoint the deals entry point to the in-Style flash button; the
            // controller adds the ShowDeals listener from this reference in Awake.
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("_dealsButton").objectReferenceValue = flashButton;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("MenuColumnsBuilder: two-column home + in-Style Daily Deals flash button built.");
        }

        // ------------------------------------------------------------------
        // Left column — game actions, bottom-left (top→bottom).
        // ------------------------------------------------------------------
        private static void LayoutLeftColumn(Transform mainPanel)
        {
            PlaceLeftButton(mainPanel, "PlayButton", 4);
            PlaceLeftButton(mainPanel, "CodexButton", 3);
            PlaceLeftButton(mainPanel, "AwardsButton", 2);
            PlaceLeftButton(mainPanel, "SettingsButton", 1);
            PlaceLeftButton(mainPanel, "QuitButton", 0);
        }

        private static void PlaceLeftButton(Transform panel, string name, int rowFromBottom)
        {
            Transform button = panel.Find(name);
            if (button == null)
            {
                Debug.LogWarning($"MenuColumnsBuilder: home button '{name}' not found.");
                return;
            }

            var rect = (RectTransform)button;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = HomeButtonSize;
            rect.anchoredPosition = new Vector2(SideMargin, BottomMargin + rowFromBottom * HomeButtonPitch);
        }

        // The old home DEALS button is superseded by the in-Style flash button.
        private static void RemoveHomeDealsButton(Transform mainPanel)
        {
            Transform deals = mainPanel.Find("DealsButton");
            if (deals != null)
            {
                Object.DestroyImmediate(deals.gameObject);
            }
        }

        // ------------------------------------------------------------------
        // Right column — player actions + equipped showcase, bottom-right.
        // ------------------------------------------------------------------
        private static void LayoutRightColumn(
            Transform mainPanel, Sprite panelSprite,
            PersistentMetaProgressionStoreSO store, CosmeticCatalogSO catalog, SpriteLibraryAsset beeLibrary)
        {
            // top→bottom: [ equipped bee ] / HIVE UPGRADES / STYLE
            PlaceRightButton(mainPanel, "StyleButton", 0);
            PlaceRightButton(mainPanel, "ShopButton", 1);
            BuildEquippedPreview((RectTransform)mainPanel, panelSprite, store, catalog, beeLibrary);
        }

        private static void PlaceRightButton(Transform panel, string name, int rowFromBottom)
        {
            Transform button = panel.Find(name);
            if (button == null)
            {
                Debug.LogWarning($"MenuColumnsBuilder: home button '{name}' not found.");
                return;
            }

            var rect = (RectTransform)button;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = HomeButtonSize;
            rect.anchoredPosition = new Vector2(-SideMargin, BottomMargin + rowFromBottom * HomeButtonPitch);
        }

        private static void BuildEquippedPreview(
            RectTransform mainPanel, Sprite panelSprite,
            PersistentMetaProgressionStoreSO store, CosmeticCatalogSO catalog, SpriteLibraryAsset beeLibrary)
        {
            // Rebuild our own node from scratch each run.
            Transform existing = mainPanel.Find(PreviewNodeName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var boxGo = new GameObject(PreviewNodeName, typeof(RectTransform));
            var boxRect = (RectTransform)boxGo.transform;
            boxRect.SetParent(mainPanel, false);
            boxRect.anchorMin = new Vector2(1f, 0f);
            boxRect.anchorMax = new Vector2(1f, 0f);
            boxRect.pivot = new Vector2(1f, 0f);
            boxRect.sizeDelta = new Vector2(525f, 300f);
            // Sits just above the two right-column buttons (rows 0 + 1).
            boxRect.anchoredPosition = new Vector2(-SideMargin, BottomMargin + 2 * HomeButtonPitch + 20f);

            Image boxImage = boxGo.AddComponent<Image>();
            boxImage.sprite = panelSprite;
            boxImage.type = Image.Type.Sliced;
            boxImage.pixelsPerUnitMultiplier = 2f;
            boxImage.color = new Color(DeepBrown.r, DeepBrown.g, DeepBrown.b, 0.85f);
            boxImage.raycastTarget = false;

            var bodyGo = new GameObject("PreviewBody", typeof(RectTransform));
            var bodyRect = (RectTransform)bodyGo.transform;
            bodyRect.SetParent(boxRect, false);
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.anchoredPosition = Vector2.zero;
            bodyRect.sizeDelta = new Vector2(240f, 240f);
            Image body = bodyGo.AddComponent<Image>();
            body.preserveAspect = true;
            body.raycastTarget = false;
            body.sprite = beeLibrary != null ? beeLibrary.GetSprite("Idle", "0") : null;

            var hatGo = new GameObject("PreviewHat", typeof(RectTransform));
            var hatRect = (RectTransform)hatGo.transform;
            hatRect.SetParent(bodyRect, false);
            hatRect.anchorMin = new Vector2(0.5f, 0.5f);
            hatRect.anchorMax = new Vector2(0.5f, 0.5f);
            hatRect.pivot = new Vector2(0.5f, 0.5f);
            hatRect.anchoredPosition = Vector2.zero;
            hatRect.sizeDelta = Vector2.zero;
            Image hat = hatGo.AddComponent<Image>();
            hat.raycastTarget = false;
            hat.enabled = false;

            var preview = boxGo.AddComponent<MainMenuBeePreview>();
            var so = new SerializedObject(preview);
            so.FindProperty("_store").objectReferenceValue = store;
            so.FindProperty("_catalog").objectReferenceValue = catalog;
            so.FindProperty("_bodyImage").objectReferenceValue = body;
            so.FindProperty("_hatImage").objectReferenceValue = hat;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(preview);
        }

        // ------------------------------------------------------------------
        // Hive Style panel — flashing "Daily Deals!" call-to-action (top-right).
        // ------------------------------------------------------------------
        private static Button BuildFlashButton(RectTransform stylePanel, TMP_FontAsset font, Sprite buttonSprite)
        {
            Transform existing = stylePanel.Find(FlashButtonName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var buttonGo = new GameObject(FlashButtonName, typeof(RectTransform));
            var rect = (RectTransform)buttonGo.transform;
            rect.SetParent(stylePanel, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-40f, -32f);
            rect.sizeDelta = new Vector2(380f, 88f);

            Image image = buttonGo.AddComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = HoneyGold;

            var button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;
            // Transition.None so the pulse owns the image color unchallenged.
            button.transition = Selectable.Transition.None;
            buttonGo.AddComponent<UIClickSfx>();

            var pulse = buttonGo.AddComponent<UiFlashPulse>();
            var pulseSo = new SerializedObject(pulse);
            pulseSo.FindProperty("_target").objectReferenceValue = image;
            pulseSo.FindProperty("_dimColor").colorValue = Amber;
            pulseSo.FindProperty("_brightColor").colorValue = Color.white;
            pulseSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pulse);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.SetParent(buttonGo.transform, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = 30f;
            label.color = DeepBrown;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            label.text = Loc.Get(LocKeys.DealsFlashButton);

            return button;
        }

        // Slide the Hive Style jelly readout below the new top-right flash button.
        private static void NudgeStyleJelly(Transform stylePanel)
        {
            Transform jelly = stylePanel.Find("JellyText");
            if (jelly == null)
            {
                return;
            }

            var rect = (RectTransform)jelly;
            rect.anchoredPosition = new Vector2(-48f, -150f);
        }
    }
}
