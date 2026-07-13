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
    /// PLAN 3C — enhanced options. Relays both settings panels (MainMenu
    /// <c>SettingsPanel</c> and Beehive <c>PauseSettingsPanel</c>) into two
    /// columns — the existing audio/general controls move to the left, and five
    /// new feedback-layer toggle rows (enemy HP bars, damage numbers, screen
    /// shake, hit-stop, status colors) go on the right, each a
    /// <see cref="FeedbackToggleUI"/> button persisting through the same store
    /// as the rest of the settings. Also refreshes the localization table for
    /// the new keys.
    ///
    /// Playtest polish (same phase): every control shrunk (500-wide buttons,
    /// smaller slider handles, 28–30pt labels), both columns start below the
    /// panel title, labels get clear air above the slider handles, and every
    /// menu BACK button (world select / shop / settings / pause settings)
    /// moves to its panel's top-left corner.
    ///
    /// Additive + idempotent: rows are found-or-created by name, positions and
    /// sizes are simply re-asserted; nothing else is removed or re-wired.
    /// </summary>
    public static class EnhancedOptionsBuilder
    {
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string RunScenePath = "Assets/Scenes/Beehive.unity";
        private const string PersistentStorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";

        // Mirrors the Phase4 menu palette.
        private static readonly Color HoneyGold = new Color(1f, 0.765f, 0.043f);
        private static readonly Color Amber = new Color(0.961f, 0.651f, 0.137f);
        private static readonly Color DeepBrown = new Color(0.227f, 0.141f, 0.086f);

        // Two-column layout, both columns starting below the panel title
        // (which spans down to ~y 290 on the shallower pause panel).
        private const float LeftColumnX = -380f;
        private const float RightColumnX = 380f;
        private const float FirstRowY = 175f;
        private const float RowPitch = 105f;
        private static readonly Vector2 ToggleSize = new Vector2(500f, 90f);
        private const float ToggleFontSize = 30f;
        private const float SliderLabelFontSize = 28f;
        private static readonly Vector2 SliderLabelSize = new Vector2(500f, 40f);
        private static readonly Vector2 SliderSize = new Vector2(500f, 44f);
        // Short enough that a mid-track handle stays clear of the label above.
        private static readonly Vector2 SliderHandleSize = new Vector2(40f, 54f);
        private static readonly Vector2 BackButtonSize = new Vector2(220f, 70f);
        private static readonly Vector2 BackButtonPosition = new Vector2(36f, -36f);
        private const float BackButtonFontSize = 28f;

        // The pause settings panel is still the portrait-era 820×1000 box —
        // widen it for the second column (the MainMenu panel was already made
        // a 1720×980 landscape window by PcMenuLayoutBuilder and is left to it).
        private static readonly Vector2 PausePanelSize = new Vector2(1500f, 900f);

        private struct ControlSpec
        {
            public string Name;
            public float Y;
            public bool IsSlider;
        }

        // Left column, top to bottom: label + slider pairs with air between
        // the label and the handle, then the two cycle-buttons.
        private static readonly ControlSpec[] ExistingControls =
        {
            new ControlSpec { Name = "MusicLabel", Y = 250f },
            new ControlSpec { Name = "MusicSlider", Y = 175f, IsSlider = true },
            new ControlSpec { Name = "SfxLabel", Y = 105f },
            new ControlSpec { Name = "SfxSlider", Y = 30f, IsSlider = true },
            new ControlSpec { Name = "VibrationButton", Y = -100f },
            new ControlSpec { Name = "QualityButton", Y = -205f },
        };

        private struct ToggleSpec
        {
            public string Name;
            public FeedbackToggleKind Kind;
            public string RestingLabel;
        }

        private static readonly ToggleSpec[] Toggles =
        {
            new ToggleSpec { Name = "EnemyHpBarsToggle", Kind = FeedbackToggleKind.EnemyHealthBars, RestingLabel = "ENEMY HP BARS: ON" },
            new ToggleSpec { Name = "DamageNumbersToggle", Kind = FeedbackToggleKind.DamageNumbers, RestingLabel = "DAMAGE NUMBERS: ON" },
            new ToggleSpec { Name = "ScreenShakeToggle", Kind = FeedbackToggleKind.ScreenShake, RestingLabel = "SCREEN SHAKE: ON" },
            new ToggleSpec { Name = "HitStopToggle", Kind = FeedbackToggleKind.HitStop, RestingLabel = "HIT-STOP: ON" },
            new ToggleSpec { Name = "StatusTintsToggle", Kind = FeedbackToggleKind.StatusTints, RestingLabel = "STATUS COLORS: ON" },
        };

        [MenuItem("SurveHive/Apply Enhanced Options (3C)")]
        public static void Apply()
        {
            // New settings.* keys → the authored string table (append-only pass).
            LocalizationBuilder.Apply();

            ApplyToScene(MenuScenePath, resizePanel: false,
                extraBackButtonPanels: new[] { "WorldSelectPanel", "ShopPanel" });
            ApplyToScene(RunScenePath, resizePanel: true,
                extraBackButtonPanels: new string[0]);

            Debug.Log("EnhancedOptionsBuilder: feedback toggles built in both settings panels.");
        }

        private static void ApplyToScene(string scenePath, bool resizePanel, string[] extraBackButtonPanels)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Load assets only after the scene switch (pre-switch instances
            // serialize as fileID 0).
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite buttonSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelButton");
            if (store == null || font == null || buttonSprite == null)
            {
                Debug.LogError($"EnhancedOptionsBuilder: missing store/font/sprite for {scenePath}.");
                return;
            }

            // Each scene carries exactly one settings panel (SettingsPanelUI host).
            SettingsPanelUI[] panels = Object.FindObjectsByType<SettingsPanelUI>(
                FindObjectsInactive.Include);
            if (panels.Length != 1)
            {
                Debug.LogError($"EnhancedOptionsBuilder: expected 1 SettingsPanelUI in {scenePath}, found {panels.Length}.");
                return;
            }

            Transform panel = panels[0].transform;
            if (resizePanel)
            {
                ((RectTransform)panel).sizeDelta = PausePanelSize;
            }

            foreach (ControlSpec spec in ExistingControls)
            {
                Transform control = panel.Find(spec.Name);
                if (control == null)
                {
                    Debug.LogWarning($"EnhancedOptionsBuilder: '{spec.Name}' not found under {panel.name}.");
                    continue;
                }

                var rect = (RectTransform)control;
                rect.anchoredPosition = new Vector2(LeftColumnX, spec.Y);

                if (spec.IsSlider)
                {
                    rect.sizeDelta = SliderSize;
                    Transform handle = control.Find("HandleSlideArea/Handle");
                    if (handle != null)
                    {
                        ((RectTransform)handle).sizeDelta = SliderHandleSize;
                    }
                }
                else if (control.TryGetComponent(out TMP_Text labelText))
                {
                    // The MUSIC / SFX captions above the sliders.
                    rect.sizeDelta = SliderLabelSize;
                    labelText.fontSize = SliderLabelFontSize;
                }
                else
                {
                    // The vibration / quality cycle-buttons.
                    rect.sizeDelta = ToggleSize;
                    SetButtonLabelSize(control, ToggleFontSize);
                }
            }

            for (int i = 0; i < Toggles.Length; i++)
            {
                BuildToggleRow(panel, Toggles[i], new Vector2(RightColumnX, FirstRowY - i * RowPitch),
                    store, font, buttonSprite);
            }

            MoveBackButtonTopLeft(panel);
            foreach (string panelName in extraBackButtonPanels)
            {
                Transform other = panel.parent.Find(panelName);
                if (other != null)
                {
                    MoveBackButtonTopLeft(other);
                }
                else
                {
                    Debug.LogWarning($"EnhancedOptionsBuilder: panel '{panelName}' not found for back-button move.");
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // Playtest polish: BACK sits in the panel's top-left corner instead of
        // hugging (or overflowing) the bottom edge.
        private static void MoveBackButtonTopLeft(Transform panel)
        {
            Transform back = panel.Find("BackButton");
            if (back == null)
            {
                Debug.LogWarning($"EnhancedOptionsBuilder: no BackButton under '{panel.name}'.");
                return;
            }

            var rect = (RectTransform)back;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = BackButtonPosition;
            rect.sizeDelta = BackButtonSize;
            SetButtonLabelSize(back, BackButtonFontSize);
            EditorUtility.SetDirty(back.gameObject);
        }

        private static void SetButtonLabelSize(Transform button, float fontSize)
        {
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = fontSize;
            }
        }

        private static void BuildToggleRow(
            Transform panel, ToggleSpec spec, Vector2 position,
            PersistentMetaProgressionStoreSO store, TMP_FontAsset font, Sprite buttonSprite)
        {
            Transform existing = panel.Find(spec.Name);
            GameObject rowGo = existing != null
                ? existing.gameObject
                : CreateToggleButton(panel, spec.Name, spec.RestingLabel, font, buttonSprite);

            var rect = (RectTransform)rowGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = ToggleSize;
            SetButtonLabelSize(rowGo.transform, ToggleFontSize);

            if (!rowGo.TryGetComponent(out FeedbackToggleUI toggle))
            {
                toggle = rowGo.AddComponent<FeedbackToggleUI>();
            }

            var serialized = new SerializedObject(toggle);
            serialized.FindProperty("_store").objectReferenceValue = store;
            serialized.FindProperty("_kind").intValue = (int)spec.Kind;
            serialized.FindProperty("_button").objectReferenceValue = rowGo.GetComponent<Button>();
            serialized.FindProperty("_label").objectReferenceValue = rowGo.GetComponentInChildren<TMP_Text>(true);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(rowGo);
        }

        // Mirrors Phase4MetaAndMenusBuilder.CreateButton (kept private there).
        private static GameObject CreateToggleButton(
            Transform parent, string name, string label, TMP_FontAsset font, Sprite buttonSprite)
        {
            var buttonGo = new GameObject(name, typeof(RectTransform));
            buttonGo.transform.SetParent(parent, false);

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
            buttonGo.AddComponent<UIClickSfx>();

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(buttonGo.transform, false);
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.font = font;
            labelTmp.fontSize = ToggleFontSize;
            labelTmp.color = DeepBrown;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.textWrappingMode = TextWrappingModes.Normal;
            labelTmp.raycastTarget = false;
            labelTmp.text = label;

            return buttonGo;
        }
    }
}
