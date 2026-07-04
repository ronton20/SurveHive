using SurveHive.Data;
using SurveHive.Spawning;
using SurveHive.Stage;
using SurveHive.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Phase 3 (PLAN.md §5): run structure. Sub-phase 3A — stage timeline config,
    /// stage director with strong-wave formations, and the HUD progress bar with
    /// event markers. 3B (bosses) and 3C (drops/results) extend this same pass.
    /// Additive over Phases 0-2; idempotent.
    /// </summary>
    public static class Phase3RunStructureBuilder
    {
        private const string ScenePath = "Assets/Scenes/Beehive.unity";
        private const string StageConfigPath = "Assets/Data/Stage/BeehiveStageConfig.asset";
        private const string UiKitTexturePath = "Assets/ThirdParty/PixelUI/UI SIMPLE PIXEL UNSPLIT.png";
        private const string IconFolder = "Assets/ThirdParty/IconsTemp/Icons/PictoIcon_128";

        private static readonly Color HoneyGold = new Color(1f, 0.765f, 0.043f);
        private static readonly Color DeepBrown = new Color(0.227f, 0.141f, 0.086f);
        private static readonly Color Wax = new Color(0.91f, 0.847f, 0.627f);
        private static readonly Color DangerRed = new Color(0.851f, 0.282f, 0.231f);
        private static readonly Color RoyalPurple = new Color(0.482f, 0.176f, 0.545f);

        [MenuItem("SurveHive/Apply Phase 3 Run Structure")]
        public static void Apply()
        {
            StageConfigSO stageConfig = EnsureStageConfig();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ApplySceneChanges(stageConfig);

            Debug.Log("SurveHive Phase 3 run structure build complete.");
        }

        // ------------------------------------------------------------------
        // Stage config: 10-minute run, spawn rate ramping 1x -> 3.5x, events
        // at 25% (ring wave), 50% (miniboss), 75% (flood wave), 100% (Queen).
        // Boss events carry Queen's Guard stats as a stand-in until 3B ships
        // the real miniboss/boss ranks.
        // ------------------------------------------------------------------
        private static StageConfigSO EnsureStageConfig()
        {
            EnsureFolder("Assets/Data/Stage");

            var config = AssetDatabase.LoadAssetAtPath<StageConfigSO>(StageConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<StageConfigSO>();
                AssetDatabase.CreateAsset(config, StageConfigPath);
            }

            var warrior = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>("Assets/Data/Enemies/WarriorBee.asset");
            var queensGuard = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>("Assets/Data/Enemies/QueensGuard.asset");

            var serialized = new SerializedObject(config);
            serialized.FindProperty("_totalDurationSeconds").floatValue = 600f;
            serialized.FindProperty("_spawnRateMultiplier").animationCurveValue =
                AnimationCurve.Linear(0f, 1f, 1f, 3.5f);

            SerializedProperty events = serialized.FindProperty("_events");
            events.arraySize = 4;
            SetEvent(events, 0, StageEventType.StrongWaveRing, 0.25f, warrior, 24);
            SetEvent(events, 1, StageEventType.Miniboss, 0.5f, queensGuard, 1);
            SetEvent(events, 2, StageEventType.StrongWaveFlood, 0.75f, queensGuard, 20);
            SetEvent(events, 3, StageEventType.FinalBoss, 1f, queensGuard, 1);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void SetEvent(
            SerializedProperty events, int index, StageEventType type,
            float normalizedTime, EnemyStatsSO stats, int count)
        {
            SerializedProperty entry = events.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("Type").intValue = (int)type;
            entry.FindPropertyRelative("NormalizedTime").floatValue = normalizedTime;
            entry.FindPropertyRelative("EnemyStats").objectReferenceValue = stats;
            entry.FindPropertyRelative("Count").intValue = count;
        }

        // ------------------------------------------------------------------
        // Scene: StageDirector object + HUD progress bar with event markers.
        // ------------------------------------------------------------------
        private static void ApplySceneChanges(StageConfigSO stageConfig)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            stageConfig = AssetDatabase.LoadAssetAtPath<StageConfigSO>(StageConfigPath);

            // Director.
            GameObject directorGo = GameObject.Find("StageDirector");
            if (directorGo == null)
            {
                directorGo = new GameObject("StageDirector");
            }

            if (!directorGo.TryGetComponent(out StageDirector director))
            {
                director = directorGo.AddComponent<StageDirector>();
            }

            GameObject spawnerGo = GameObject.Find("EnemySpawner");
            var directorSerialized = new SerializedObject(director);
            directorSerialized.FindProperty("_config").objectReferenceValue = stageConfig;
            directorSerialized.FindProperty("_spawner").objectReferenceValue =
                spawnerGo != null ? spawnerGo.GetComponent<EnemySpawner>() : null;
            directorSerialized.ApplyModifiedPropertiesWithoutUndo();

            BuildProgressBar(stageConfig, director);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void BuildProgressBar(StageConfigSO stageConfig, StageDirector director)
        {
            GameObject canvasGo = GameObject.Find("Canvas");
            Transform canvas = canvasGo.transform;

            Transform existing = canvas.Find("StageProgressBar");
            GameObject barGo;
            if (existing == null)
            {
                barGo = new GameObject("StageProgressBar", typeof(RectTransform));
                barGo.transform.SetParent(canvas, false);
            }
            else
            {
                barGo = existing.gameObject;
            }

            // Sits directly under the run timer (timer: top-center, y -25).
            var rect = (RectTransform)barGo.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -48f);
            rect.sizeDelta = new Vector2(420f, 12f);

            Sprite squareSprite = LoadUiKitSprite("PixelSquare");

            if (!barGo.TryGetComponent(out Image background))
            {
                background = barGo.AddComponent<Image>();
            }

            background.sprite = squareSprite;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 4f;
            background.color = new Color(DeepBrown.r, DeepBrown.g, DeepBrown.b, 0.95f);
            background.raycastTarget = false;

            // Fill (anchor-driven, see UIBarFiller).
            Transform fillTransform = barGo.transform.Find("Fill");
            GameObject fillGo;
            if (fillTransform == null)
            {
                fillGo = new GameObject("Fill", typeof(RectTransform));
                fillGo.transform.SetParent(barGo.transform, false);
            }
            else
            {
                fillGo = fillTransform.gameObject;
            }

            var fillRect = (RectTransform)fillGo.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            fillRect.pivot = new Vector2(0f, 0.5f);

            if (!fillGo.TryGetComponent(out Image fill))
            {
                fill = fillGo.AddComponent<Image>();
            }

            fill.sprite = squareSprite;
            fill.type = Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = 4f;
            fill.color = HoneyGold;
            fill.raycastTarget = false;

            // Event markers along the bar.
            StageTimelineEvent[] events = stageConfig.Events;
            for (int i = 0; i < events.Length; i++)
            {
                string markerName = $"Marker{i}";
                Transform markerTransform = barGo.transform.Find(markerName);
                GameObject markerGo;
                if (markerTransform == null)
                {
                    markerGo = new GameObject(markerName, typeof(RectTransform));
                    markerGo.transform.SetParent(barGo.transform, false);
                }
                else
                {
                    markerGo = markerTransform.gameObject;
                }

                var markerRect = (RectTransform)markerGo.transform;
                markerRect.anchorMin = new Vector2(events[i].NormalizedTime, 0.5f);
                markerRect.anchorMax = new Vector2(events[i].NormalizedTime, 0.5f);
                markerRect.pivot = new Vector2(0.5f, 0.5f);
                markerRect.anchoredPosition = new Vector2(events[i].NormalizedTime >= 1f ? -8f : 0f, 0f);
                markerRect.sizeDelta = new Vector2(18f, 18f);

                if (!markerGo.TryGetComponent(out Image markerImage))
                {
                    markerImage = markerGo.AddComponent<Image>();
                }

                markerImage.sprite = LoadIconSprite(GetMarkerIcon(events[i].Type));
                markerImage.color = GetMarkerColor(events[i].Type);
                markerImage.raycastTarget = false;
            }

            if (!barGo.TryGetComponent(out StageProgressBarUI barUi))
            {
                barUi = barGo.AddComponent<StageProgressBarUI>();
            }

            var barSerialized = new SerializedObject(barUi);
            barSerialized.FindProperty("_fillImage").objectReferenceValue = fill;
            barSerialized.FindProperty("_director").objectReferenceValue = director;
            barSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string GetMarkerIcon(StageEventType type)
        {
            switch (type)
            {
                case StageEventType.Miniboss:
                    return "Icon_PictoIcon_Skull.Png";
                case StageEventType.FinalBoss:
                    return "Icon_PictoIcon_Crown.Png";
                default:
                    return "Icon_PictoIcon_Siren.Png";
            }
        }

        private static Color GetMarkerColor(StageEventType type)
        {
            switch (type)
            {
                case StageEventType.Miniboss:
                    return DangerRed;
                case StageEventType.FinalBoss:
                    return RoyalPurple;
                default:
                    return Wax;
            }
        }

        // ------------------------------------------------------------------
        // Shared helpers (same conventions as earlier phase builders).
        // ------------------------------------------------------------------
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

            Debug.LogError($"Phase3: UI kit sprite '{spriteName}' not found.");
            return null;
        }

        private static Sprite LoadIconSprite(string iconFileName)
        {
            string iconPath = $"{IconFolder}/{iconFileName}";
            var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
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
