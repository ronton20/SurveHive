using System.IO;
using SurveHive.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// PLAN 3B-2d — additive, idempotent health-bar readability pass. Does NOT
    /// rebuild anything: it finds the already-built bars and (a) polishes their
    /// size/contrast, (b) adds a lagging "damage trail" image behind the player and
    /// boss fills, and (c) adds a numeric HP readout over the player bar, wiring the
    /// new <see cref="HealthBarUI"/>/<see cref="BossHealthBarUI"/> serialized fields.
    /// Enemy prefabs get a size/contrast bump so tiny bars read better. Re-runnable:
    /// every child is find-or-create and every property is set to a fixed value.
    /// </summary>
    public static class HealthBarPolishBuilder
    {
        private const string ScenePath = "Assets/Scenes/Beehive.unity";
        private const string UiKitTexturePath = "Assets/ThirdParty/PixelUI/UI SIMPLE PIXEL UNSPLIT.png";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";
        private const string EnemyPrefabFolder = "Assets/Prefabs/Enemies";

        private static readonly Color DeepBrown = new Color(0.227f, 0.141f, 0.086f);
        private static readonly Color Wax = new Color(0.91f, 0.847f, 0.627f);
        // A pale honey trail so the chunk that drains after a hit flashes bright.
        private static readonly Color TrailHoney = new Color(1f, 0.92f, 0.62f, 0.85f);

        [MenuItem("SurveHive/Polish Health Bars")]
        public static void Apply()
        {
            PolishEnemyPrefabs();

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Sprite square = LoadUiKitSprite("PixelSquare");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

            PolishPlayerBar(square, font);
            PolishBossBar(square);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("HealthBarPolishBuilder: player + boss + enemy health bars polished.");
        }

        // ------------------------------------------------------------------
        // Player HUD bar: bigger, framed, health-graded fill + trail + readout.
        // ------------------------------------------------------------------
        private static void PolishPlayerBar(Sprite square, TMP_FontAsset font)
        {
            GameObject canvasGo = GameObject.Find("Canvas");
            Transform canvas = canvasGo.transform;
            GameObject bg = canvas.Find("HealthBarBackground").gameObject;

            var bgRect = (RectTransform)bg.transform;
            bgRect.sizeDelta = new Vector2(320f, 34f);
            Image bgImage = bg.GetComponent<Image>();
            bgImage.color = new Color(DeepBrown.r, DeepBrown.g, DeepBrown.b, 0.95f);

            GameObject fill = bg.transform.Find("HealthBarFill").gameObject;
            FrameInset((RectTransform)fill.transform, 3f);

            // Trail sits behind the fill; identical inset so the revealed chunk aligns.
            GameObject trail = EnsureImageChild(bg.transform, "HealthBarTrail", square, TrailHoney);
            FrameInset((RectTransform)trail.transform, 3f);
            trail.transform.SetAsFirstSibling();
            fill.transform.SetSiblingIndex(1);

            // Numeric readout on top of the bar.
            GameObject readout = EnsureUiChild(bg.transform, "HealthReadout");
            TMP_Text readoutTmp = EnsureTmp(readout, font, 18f, Wax, TextAlignmentOptions.Center);
            var readoutRect = (RectTransform)readout.transform;
            readoutRect.anchorMin = Vector2.zero;
            readoutRect.anchorMax = Vector2.one;
            readoutRect.offsetMin = Vector2.zero;
            readoutRect.offsetMax = Vector2.zero;
            readout.transform.SetAsLastSibling();

            var so = new SerializedObject(bg.GetComponent<HealthBarUI>());
            so.FindProperty("_readoutText").objectReferenceValue = readoutTmp;
            so.FindProperty("_tintByHealth").boolValue = true;
            so.FindProperty("_trail").FindPropertyRelative("_trailImage").objectReferenceValue =
                trail.GetComponent<Image>();
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bg);
        }

        // ------------------------------------------------------------------
        // Boss bar: add a damage trail behind the fill.
        // ------------------------------------------------------------------
        private static void PolishBossBar(Sprite square)
        {
            Transform canvas = GameObject.Find("Canvas").transform;
            Transform bar = FindDeep(canvas, "BossHealthBar");
            GameObject fill = bar.Find("Fill").gameObject;

            GameObject trail = EnsureImageChild(bar, "Trail", square, TrailHoney);
            // Mirror the boss fill's rect (left-anchored, 2px inset) so it tracks 1:1.
            var trailRect = (RectTransform)trail.transform;
            trailRect.anchorMin = Vector2.zero;
            trailRect.anchorMax = new Vector2(0f, 1f);
            trailRect.pivot = new Vector2(0f, 0.5f);
            trailRect.offsetMin = new Vector2(2f, 2f);
            trailRect.offsetMax = new Vector2(-2f, -2f);
            Image trailImage = trail.GetComponent<Image>();
            trailImage.type = Image.Type.Sliced;
            trailImage.pixelsPerUnitMultiplier = 4f;
            trailImage.raycastTarget = false;
            trail.transform.SetAsFirstSibling();
            fill.transform.SetSiblingIndex(1);

            var so = new SerializedObject(bar.GetComponent<BossHealthBarUI>());
            so.FindProperty("_trail").FindPropertyRelative("_trailImage").objectReferenceValue = trailImage;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bar.gameObject);
        }

        // ------------------------------------------------------------------
        // Enemy prefabs: bigger canvas, opaque background, framed fill.
        // ------------------------------------------------------------------
        private static void PolishEnemyPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { EnemyPrefabFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".prefab") || Path.GetDirectoryName(path)?.Replace('\\', '/') != EnemyPrefabFolder)
                {
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Transform bar = root.transform.Find("HealthBarCanvas");
                    if (bar == null)
                    {
                        continue;
                    }

                    ((RectTransform)bar).sizeDelta = new Vector2(120f, 18f);

                    Transform bg = bar.Find("Background");
                    if (bg != null && bg.TryGetComponent(out Image bgImage))
                    {
                        bgImage.color = new Color(0f, 0f, 0f, 0.85f);
                    }

                    Transform fill = bar.Find("Fill");
                    if (fill != null)
                    {
                        FrameInset((RectTransform)fill, 2f);
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        // ------------------------------------------------------------------
        // Helpers.
        // ------------------------------------------------------------------

        // Full-rect anchors with a uniform pixel inset — a consistent dark frame
        // shows around the fill for contrast, and UIBarFiller still drives anchorMax.x.
        private static void FrameInset(RectTransform rect, float pad)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(pad, pad);
            rect.offsetMax = new Vector2(-pad, -pad);
        }

        private static GameObject EnsureUiChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject EnsureImageChild(Transform parent, string name, Sprite sprite, Color color)
        {
            GameObject go = EnsureUiChild(parent, name);
            if (!go.TryGetComponent(out Image image))
            {
                image = go.AddComponent<Image>();
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 4f;
            image.color = color;
            image.raycastTarget = false;
            return go;
        }

        private static TMP_Text EnsureTmp(GameObject go, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            if (!go.TryGetComponent(out TextMeshProUGUI tmp))
            {
                tmp = go.AddComponent<TextMeshProUGUI>();
            }

            tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.text = "100 / 100";
            return tmp;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }

                Transform found = FindDeep(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
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

            Debug.LogError($"HealthBarPolishBuilder: UI kit sprite '{spriteName}' not found.");
            return null;
        }
    }
}
