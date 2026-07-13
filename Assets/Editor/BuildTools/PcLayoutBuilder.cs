using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// PLAN 3B-2 (layout + text fit) — retargets the UI from the mobile-era
    /// <b>portrait</b> reference resolution (1080×1920) to a <b>landscape</b> PC
    /// one (1920×1080), matching height. On a 16:9 desktop the old portrait
    /// reference shrank every canvas to ~56% scale — the "text too small / doesn't
    /// fit PC" playtest complaint. A landscape reference at match-height keeps the
    /// scale factor at ~1.0 on 1080p (and scales up cleanly to 1440p), so all UI
    /// text and meters read at their authored size without per-element bumps.
    ///
    /// Additive and idempotent: it only rewrites the <see cref="CanvasScaler"/>
    /// values on every ScaleWithScreenSize scaler in the MainMenu and Beehive
    /// scenes — no hierarchy edits, no data assets touched. Safe to re-run.
    /// </summary>
    public static class PcLayoutBuilder
    {
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Beehive.unity",
        };

        // Landscape PC reference; match height so UI scales with vertical
        // resolution (1080 → 1440 uniform) and ultrawide only adds side margin.
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private const float MatchHeight = 1f;

        [MenuItem("SurveHive/Fit UI To PC (Landscape Canvas)")]
        public static void Apply()
        {
            foreach (string scenePath in Scenes)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                int retargeted = 0;
                CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(
                    FindObjectsInactive.Include);
                foreach (CanvasScaler scaler in scalers)
                {
                    if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                    {
                        continue;
                    }

                    scaler.referenceResolution = ReferenceResolution;
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = MatchHeight;
                    EditorUtility.SetDirty(scaler);
                    retargeted++;
                }

                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log($"PcLayoutBuilder: {scenePath} — retargeted {retargeted} canvas scaler(s) to 1920×1080 match-height.");
            }
        }
    }
}
