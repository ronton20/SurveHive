using SurveHive.Core;
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
    /// PLAN 5E — rotating cosmetics shop. Additive/idempotent: appends the new
    /// deals.* keys to the string table, adds a DEALS button to the MainMenu
    /// home stack (re-asserting the 8-row layout), rebuilds its own
    /// DailyDealsPanel sub-tree (title, rollover countdown, jelly balance, and
    /// three deal cards driven by <see cref="DailyDealsUI"/>), and wires the
    /// controller. Reuses the 5C cosmetic catalog + placeholder sprites — no
    /// new assets. Rebuilds only its own generated nodes.
    /// </summary>
    public static class RotatingShopBuilder
    {
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string CatalogPath = "Assets/Data/Cosmetics/CosmeticCatalog.asset";
        private const string PersistentStorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";
        private const string SwatchSpritePath = "Assets/Sprites/Cosmetics/Swatch.png";

        // Honey/hive palette (mirrors CosmeticsBuilder / CodexBuilder).
        private static readonly Color HoneyGold = new Color(1f, 0.765f, 0.043f);
        private static readonly Color Amber = new Color(0.961f, 0.651f, 0.137f);
        private static readonly Color Wax = new Color(0.91f, 0.847f, 0.627f);
        private static readonly Color CombBrown = new Color(0.549f, 0.353f, 0.169f);
        private static readonly Color DeepBrown = new Color(0.227f, 0.141f, 0.086f);
        private static readonly Color RoyalCream = new Color(0.96f, 0.93f, 0.8f);

        [MenuItem("SurveHive/Build Rotating Shop (5E)")]
        public static void Apply()
        {
            // New deals.* keys → the authored string table (append-only pass).
            LocalizationBuilder.Apply();

            BuildMenuPanel();

            Debug.Log("RotatingShopBuilder: DEALS button + DailyDealsPanel built.");
        }

        private static void BuildMenuPanel()
        {
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            // Load assets only after the scene switch (pre-switch instances
            // serialize as fileID 0).
            var catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalogSO>(CatalogPath);
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite panelSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelPanel");
            Sprite buttonSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelButton");
            var swatch = AssetDatabase.LoadAssetAtPath<Sprite>(SwatchSpritePath);
            if (catalog == null || store == null || font == null || panelSprite == null
                || buttonSprite == null || swatch == null)
            {
                Debug.LogError("RotatingShopBuilder: missing catalog/store/font/sprites for MainMenu.");
                return;
            }

            GameObject canvas = GameObject.Find("Canvas");
            var controllerGo = GameObject.Find("MainMenuController");
            var controller = controllerGo != null ? controllerGo.GetComponent<MainMenuController>() : null;
            Transform mainPanel = canvas != null ? canvas.transform.Find("MainPanel") : null;
            if (canvas == null || controller == null || mainPanel == null)
            {
                Debug.LogError("RotatingShopBuilder: MainMenu canvas/controller/MainPanel not found.");
                return;
            }

            Button dealsButton = EnsureHomeDealsButton(mainPanel, font, buttonSprite);
            RelayoutHomeButtons(mainPanel);

            // Rebuild our own panel sub-tree from scratch each run.
            Transform existingPanel = canvas.transform.Find("DailyDealsPanel");
            if (existingPanel != null)
            {
                Object.DestroyImmediate(existingPanel.gameObject);
            }

            var panelGo = new GameObject("DailyDealsPanel", typeof(RectTransform));
            var panelRect = (RectTransform)panelGo.transform;
            panelRect.SetParent(canvas.transform, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(1600f, 920f);

            Image panelImage = panelGo.AddComponent<Image>();
            panelImage.sprite = panelSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.pixelsPerUnitMultiplier = 2f;
            panelImage.color = new Color(0.13f, 0.08f, 0.05f, 0.97f);

            TMP_Text title = CreateTopText(panelRect, "Title", font, 56f, HoneyGold, -36f, 70f, 800f,
                TextAlignmentOptions.Center);
            title.text = Loc.Get(LocKeys.DealsTitle);

            TMP_Text timerText = CreateTopText(panelRect, "TimerText", font, 30f, Wax, -108f, 44f, 800f,
                TextAlignmentOptions.Center);
            timerText.text = Loc.Get(LocKeys.DealsTimerPrefix) + "00:00:00";

            TMP_Text jellyText = CreateJellyText(panelRect, font);

            Button backButton = CreateButton(panelRect, "BackButton", "BACK", font, buttonSprite,
                Vector2.zero, new Vector2(220f, 70f), 28f);
            var backRect = (RectTransform)backButton.transform;
            backRect.anchorMin = new Vector2(0f, 1f);
            backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(36f, -36f);

            TMP_Text allOwnedText = CreateTopText(panelRect, "AllOwnedText", font, 32f, RoyalCream,
                -460f, 80f, 1200f, TextAlignmentOptions.Center);
            allOwnedText.text = Loc.Get(LocKeys.DealsAllOwned);
            allOwnedText.gameObject.SetActive(false);

            var cards = new DailyDealCardUI[Progression.RotatingShop.DealsPerDay];
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = BuildDealCard(panelRect, $"DealCard{i}", font, panelSprite, buttonSprite,
                    new Vector2((i - 1) * 480f, -70f));
            }

            var dealsUi = panelGo.AddComponent<DailyDealsUI>();
            var so = new SerializedObject(dealsUi);
            so.FindProperty("_store").objectReferenceValue = store;
            so.FindProperty("_catalog").objectReferenceValue = catalog;
            so.FindProperty("_jellyText").objectReferenceValue = jellyText;
            so.FindProperty("_timerText").objectReferenceValue = timerText;
            so.FindProperty("_allOwnedText").objectReferenceValue = allOwnedText;
            so.FindProperty("_swatchSprite").objectReferenceValue = swatch;
            SerializedProperty cardsProp = so.FindProperty("_cards");
            cardsProp.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dealsUi);

            // Panels rest inactive; MainMenuController.Awake activates the home panel.
            panelGo.SetActive(false);

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("_dealsPanel").objectReferenceValue = panelGo;
            controllerSo.FindProperty("_dealsButton").objectReferenceValue = dealsButton;
            controllerSo.FindProperty("_dealsBackButton").objectReferenceValue = backButton;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static Button EnsureHomeDealsButton(Transform mainPanel, TMP_FontAsset font, Sprite buttonSprite)
        {
            Transform existing = mainPanel.Find("DealsButton");
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            return CreateButton((RectTransform)mainPanel, "DealsButton",
                Loc.Get(LocKeys.DealsMenuButton), font, buttonSprite,
                Vector2.zero, new Vector2(525f, 98f), 40f);
        }

        // Re-assert the bottom-left home stack with DEALS slotted between STYLE
        // and AWARDS (top→bottom: Play, Shop, Codex, Style, Deals, Awards,
        // Settings, Quit).
        private static void RelayoutHomeButtons(Transform mainPanel)
        {
            PlaceHomeButton(mainPanel, "PlayButton", 7);
            PlaceHomeButton(mainPanel, "ShopButton", 6);
            PlaceHomeButton(mainPanel, "CodexButton", 5);
            PlaceHomeButton(mainPanel, "StyleButton", 4);
            PlaceHomeButton(mainPanel, "DealsButton", 3);
            PlaceHomeButton(mainPanel, "AwardsButton", 2);
            PlaceHomeButton(mainPanel, "SettingsButton", 1);
            PlaceHomeButton(mainPanel, "QuitButton", 0);
        }

        // Mirrors CosmeticsBuilder.PlaceHomeButton / AchievementsBuilder.
        private static void PlaceHomeButton(Transform panel, string name, int rowFromBottom)
        {
            Transform button = panel.Find(name);
            if (button == null)
            {
                Debug.LogWarning($"RotatingShopBuilder: home button '{name}' not found.");
                return;
            }

            var rect = (RectTransform)button;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(525f, 98f);
            rect.anchoredPosition = new Vector2(72f, 64f + rowFromBottom * 116f);
        }

        private static DailyDealCardUI BuildDealCard(
            RectTransform panel, string name, TMP_FontAsset font, Sprite panelSprite, Sprite buttonSprite,
            Vector2 centerOffset)
        {
            var cardGo = new GameObject(name, typeof(RectTransform));
            var cardRect = (RectTransform)cardGo.transform;
            cardRect.SetParent(panel, false);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = centerOffset;
            cardRect.sizeDelta = new Vector2(440f, 620f);

            Image bg = cardGo.AddComponent<Image>();
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 2f;
            bg.color = new Color(CombBrown.r, CombBrown.g, CombBrown.b, 0.85f);
            bg.raycastTarget = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.SetParent(cardRect, false);
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -36f);
            iconRect.sizeDelta = new Vector2(150f, 150f);
            Image iconImage = iconGo.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            TMP_Text nameText = CreateTopText(cardRect, "Name", font, 32f, HoneyGold, -204f, 80f, 400f,
                TextAlignmentOptions.Center);
            TMP_Text descriptionText = CreateTopText(cardRect, "Description", font, 22f, Wax, -292f, 130f, 380f,
                TextAlignmentOptions.Top);
            TMP_Text priceText = CreateTopText(cardRect, "Price", font, 34f, RoyalCream, -430f, 50f, 400f,
                TextAlignmentOptions.Center);

            Button buyButton = CreateButton(cardRect, "BuyButton", Loc.Get(LocKeys.DealsBuy), font,
                buttonSprite, Vector2.zero, new Vector2(300f, 80f), 30f);
            var buyRect = (RectTransform)buyButton.transform;
            buyRect.anchorMin = new Vector2(0.5f, 0f);
            buyRect.anchorMax = new Vector2(0.5f, 0f);
            buyRect.pivot = new Vector2(0.5f, 0f);
            buyRect.anchoredPosition = new Vector2(0f, 28f);
            var buyLabel = buyButton.GetComponentInChildren<TMP_Text>();

            var card = cardGo.AddComponent<DailyDealCardUI>();
            var so = new SerializedObject(card);
            so.FindProperty("_iconImage").objectReferenceValue = iconImage;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_descriptionText").objectReferenceValue = descriptionText;
            so.FindProperty("_priceText").objectReferenceValue = priceText;
            so.FindProperty("_buyButton").objectReferenceValue = buyButton;
            so.FindProperty("_buyLabel").objectReferenceValue = buyLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(card);

            return card;
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
            // Rest-state placeholder; DailyDealsUI paints the live glyph+number.
            tmp.text = CurrencyGlyphs.Jelly + "0";
            return tmp;
        }

        // ------------------------------------------------------------------
        // Small UI factories (CosmeticsBuilder mold).
        // ------------------------------------------------------------------
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
