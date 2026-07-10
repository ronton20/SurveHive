using SurveHive.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Builds the game's shared hover tooltip (see <see cref="TooltipUI"/>) in
    /// both scenes: a dedicated <c>TooltipCanvas</c> — screen-space overlay,
    /// sorted above every other canvas, deliberately without a
    /// GraphicRaycaster so it never blocks the pointer — holding one
    /// self-sizing panel that follows the mouse. Also removes the old pinned
    /// <c>DifficultyTooltip</c> panel from the world-select menu, which the
    /// PC panel widening had pushed off-screen; the difficulty unlock tasks
    /// now go through the shared tooltip. Additive + idempotent
    /// (find-or-create by name; positions/wiring re-asserted).
    /// </summary>
    public static class TooltipBuilder
    {
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string RunScenePath = "Assets/Scenes/Beehive.unity";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";

        // Above everything else in either scene — including TMP_Dropdown's
        // spawned list/blocker canvases, which hardcode sortingOrder 30000.
        private const int SortingOrder = 32000;
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private static readonly Color DeepBrown = new Color(0.227f, 0.141f, 0.086f);
        private static readonly Color Wax = new Color(0.91f, 0.847f, 0.627f);

        [MenuItem("SurveHive/Apply Shared Tooltip")]
        public static void Apply()
        {
            ApplyToScene(MenuScenePath, removeLegacyDifficultyTooltip: true);
            ApplyToScene(RunScenePath, removeLegacyDifficultyTooltip: false);
            Debug.Log("TooltipBuilder: shared tooltip canvas built in both scenes.");
        }

        private static void ApplyToScene(string scenePath, bool removeLegacyDifficultyTooltip)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Load assets only after the scene switch (pre-switch instances
            // serialize as fileID 0).
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite panelSprite = Phase4MetaAndMenusBuilder.LoadUiKitSprite("PixelPanel");
            if (font == null || panelSprite == null)
            {
                Debug.LogError($"TooltipBuilder: missing font/sprite for {scenePath}.");
                return;
            }

            if (removeLegacyDifficultyTooltip)
            {
                GameObject legacy = GameObject.Find("Canvas/WorldSelectPanel/DifficultyTooltip");
                if (legacy == null)
                {
                    // Inactive panels aren't found by GameObject.Find.
                    GameObject canvas = GameObject.Find("Canvas");
                    Transform worldPanel = canvas != null ? canvas.transform.Find("WorldSelectPanel") : null;
                    Transform hidden = worldPanel != null ? worldPanel.Find("DifficultyTooltip") : null;
                    legacy = hidden != null ? hidden.gameObject : null;
                }

                if (legacy != null)
                {
                    Object.DestroyImmediate(legacy);
                }
            }

            BuildTooltipCanvas(font, panelSprite);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void BuildTooltipCanvas(TMP_FontAsset font, Sprite panelSprite)
        {
            GameObject canvasGo = GameObject.Find("TooltipCanvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("TooltipCanvas", typeof(RectTransform));
            }

            if (!canvasGo.TryGetComponent(out Canvas canvas))
            {
                canvas = canvasGo.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            if (!canvasGo.TryGetComponent(out CanvasScaler scaler))
            {
                scaler = canvasGo.AddComponent<CanvasScaler>();
            }

            // Mirrors the 3B-2a landscape retarget so tooltip sizes track the
            // rest of the UI.
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            // Panel: centre-anchored, top-left pivot — TooltipUI positions its
            // top-left corner in canvas-centre coordinates (TooltipLayout).
            Transform existingPanel = canvasGo.transform.Find("TooltipPanel");
            GameObject panelGo;
            RectTransform panelRect;
            if (existingPanel != null)
            {
                panelGo = existingPanel.gameObject;
                panelRect = (RectTransform)existingPanel;
            }
            else
            {
                panelGo = new GameObject("TooltipPanel", typeof(RectTransform), typeof(Image));
                panelRect = (RectTransform)panelGo.transform;
                panelRect.SetParent(canvasGo.transform, false);
            }

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(420f, 120f);

            var image = panelGo.GetComponent<Image>();
            image.sprite = panelSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = DeepBrown;
            image.raycastTarget = false;

            Transform existingText = panelRect.Find("Text");
            GameObject textGo;
            if (existingText != null)
            {
                textGo = existingText.gameObject;
            }
            else
            {
                textGo = new GameObject("Text", typeof(RectTransform));
                textGo.transform.SetParent(panelRect, false);
            }

            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            // Insets must stay in sync with TooltipUI._padding (total 40 × 32).
            textRect.offsetMin = new Vector2(20f, 16f);
            textRect.offsetMax = new Vector2(-20f, -16f);

            if (!textGo.TryGetComponent(out TextMeshProUGUI text))
            {
                text = textGo.AddComponent<TextMeshProUGUI>();
            }

            text.font = font;
            text.fontSize = 26f;
            text.color = Wax;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;

            if (!canvasGo.TryGetComponent(out TooltipUI tooltip))
            {
                tooltip = canvasGo.AddComponent<TooltipUI>();
            }

            var serialized = new SerializedObject(tooltip);
            serialized.FindProperty("_canvasRect").objectReferenceValue = (RectTransform)canvasGo.transform;
            serialized.FindProperty("_panel").objectReferenceValue = panelRect;
            serialized.FindProperty("_text").objectReferenceValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Hidden at rest — TooltipUI.Awake re-asserts this at runtime.
            panelGo.SetActive(false);
            EditorUtility.SetDirty(canvasGo);
        }
    }
}
