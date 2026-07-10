using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Additive, idempotent PC-first relayout of the MainMenu panels. They were
    /// authored as narrow ~900×1100 <b>portrait</b> boxes centred on screen — once
    /// the canvas was retargeted to a 1920×1080 landscape reference they read as a
    /// thin mobile column floating in a wide frame.
    ///
    /// - <b>WorldSelect / Settings</b> become large landscape "windows" that fill
    ///   most of the screen.
    /// - <b>Home</b> becomes a full-screen, <b>transparent</b> container (its panel
    ///   image is disabled so a background can show through): title top-left, and
    ///   the four primary actions stacked top-to-bottom in the <b>bottom-left</b>,
    ///   at 75% button size (font unchanged). That leaves the centre/right open for
    ///   future background art.
    ///
    /// Only RectTransforms + the home panel image toggle + the title alignment are
    /// rewritten — no wiring or child hierarchies change (the world-select
    /// difficulty picker and settings controls ride along untouched). The ShopPanel
    /// is left to <see cref="MetaShopTabsBuilder"/>. Safe to re-run.
    /// </summary>
    public static class PcMenuLayoutBuilder
    {
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

        // Landscape PC "window": fills most of a 1920×1080 screen with a margin.
        private static readonly Vector2 PanelSize = new Vector2(1720f, 980f);
        // Home buttons at 75% of the previous 700×130 (font size left as authored).
        private static readonly Vector2 HomeButtonSize = new Vector2(525f, 98f);
        private const float HomeButtonPitch = 116f;   // top-to-bottom stack spacing
        private const float HomeLeftMargin = 72f;      // from the screen's left edge

        [MenuItem("SurveHive/Fit Menus To PC (Landscape)")]
        public static void Apply()
        {
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("PcMenuLayoutBuilder: Canvas not found in MainMenu.");
                return;
            }

            WidenPanel(canvas.transform, "WorldSelectPanel");
            WidenPanel(canvas.transform, "SettingsPanel");

            LayoutHome(canvas.transform);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("PcMenuLayoutBuilder: MainMenu panels relaid to landscape PC layout.");
        }

        private static void WidenPanel(Transform canvas, string name)
        {
            Transform panel = canvas.Find(name);
            if (panel == null)
            {
                Debug.LogWarning($"PcMenuLayoutBuilder: panel '{name}' not found.");
                return;
            }

            var rect = (RectTransform)panel;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = PanelSize;
        }

        // Home: full-screen transparent container, title top-left, buttons in a
        // bottom-left vertical stack (top→bottom: Play, Hive Upgrades, Settings, Quit).
        private static void LayoutHome(Transform canvas)
        {
            Transform main = canvas.Find("MainPanel");
            if (main == null)
            {
                Debug.LogWarning("PcMenuLayoutBuilder: MainPanel not found.");
                return;
            }

            // Fill the screen and drop the opaque backdrop so a background can show.
            var rect = (RectTransform)main;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            if (main.TryGetComponent(out Image panelImage))
            {
                panelImage.enabled = false;
            }

            AnchorTopLeft(main, "Title", new Vector2(HomeLeftMargin, -48f));
            AnchorTopLeft(main, "Subtitle", new Vector2(HomeLeftMargin + 6f, -210f));

            // Bottom-left stack: index 0 is the topmost (Play), higher = further up.
            PlaceHomeButton(main, "PlayButton", 3);
            PlaceHomeButton(main, "ShopButton", 2);
            PlaceHomeButton(main, "SettingsButton", 1);
            PlaceHomeButton(main, "QuitButton", 0);
        }

        private static void PlaceHomeButton(Transform panel, string name, int rowFromBottom)
        {
            Transform button = panel.Find(name);
            if (button == null)
            {
                Debug.LogWarning($"PcMenuLayoutBuilder: home button '{name}' not found.");
                return;
            }

            var rect = (RectTransform)button;
            rect.anchorMin = Vector2.zero;    // screen bottom-left
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = HomeButtonSize;
            rect.anchoredPosition = new Vector2(HomeLeftMargin, 64f + rowFromBottom * HomeButtonPitch);
        }

        // Left-aligned, anchored to the panel's top-left corner.
        private static void AnchorTopLeft(Transform panel, string name, Vector2 position)
        {
            Transform child = panel.Find(name);
            if (child == null)
            {
                return;
            }

            var rect = (RectTransform)child;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            if (child.TryGetComponent(out TMP_Text text))
            {
                text.alignment = TextAlignmentOptions.TopLeft;
            }
        }
    }
}
