using System.Collections.Generic;
using SurveHive.Combat;
using SurveHive.Core;
using SurveHive.Currency;
using SurveHive.Data;
using SurveHive.Enemies;
using SurveHive.Health;
using SurveHive.Player;
using SurveHive.Progression;
using SurveHive.Spawning;
using SurveHive.UI;
using SurveHive.View;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Phase 1 (PLAN.md): art swap onto the PixelFantasy bee rig, game-feel
    /// components (hit flash, knockback, screen shake, hit-stop, death VFX), and
    /// the honey-palette UI reskin (DEVNIK pixel kit + BoldPixels TMP font).
    /// Additive pass over the already-built Beehive scene; idempotent.
    /// </summary>
    public static class Phase1LookAndFeelBuilder
    {
        // --- Source assets (Asset Store packs, see Assets/ThirdParty) ---
        private const string BeeTexturePath = "Assets/ThirdParty/PixelFantasy/PixelMonsters/Pack1/Bee/YellowBee.png";
        private const string BeeLibraryPath = "Assets/ThirdParty/PixelFantasy/PixelMonsters/Pack1/Bee/YellowBee.asset";
        private const string MonsterControllerPath = "Assets/ThirdParty/PixelFantasy/PixelMonsters/Common/Animation/Controller.controller";
        private const string UiKitTexturePath = "Assets/ThirdParty/PixelUI/UI SIMPLE PIXEL UNSPLIT.png";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";
        private const string DeathVfxSourcePath = "Assets/ThirdParty/SpriteEffects/25 sprite effects/_prefabs/Hit_1_normal.prefab";
        private const string IconFolder = "Assets/ThirdParty/IconsTemp/Icons/PictoIcon_128";

        // --- Our generated/derived assets ---
        private const string FlashMaterialPath = "Assets/Materials/SpriteFlash.mat";
        private const string DeathVfxPrefabPath = "Assets/Prefabs/VFX/DeathPoof.prefab";
        private const string QueensGuardPrefabPath = "Assets/Prefabs/Enemies/QueensGuard.prefab";
        private const string QueensGuardStatsPath = "Assets/Data/Enemies/QueensGuard.asset";
        private const string ScenePath = "Assets/Scenes/Beehive.unity";

        // Honey/hive palette (PLAN.md section 1).
        private static readonly Color HoneyGold = new Color(1f, 0.765f, 0.043f);
        private static readonly Color Wax = new Color(0.91f, 0.847f, 0.627f);
        private static readonly Color CombBrown = new Color(0.549f, 0.353f, 0.169f);
        private static readonly Color DeepBrown = new Color(0.227f, 0.141f, 0.086f);
        private static readonly Color DangerRed = new Color(0.851f, 0.282f, 0.231f);

        // Rank tints multiply the yellow bee art (player stays untinted/vivid).
        private static readonly Color WorkerTint = new Color(0.85f, 0.8f, 0.62f);
        private static readonly Color WarriorTint = new Color(1f, 0.5f, 0.4f);
        private static readonly Color QueensGuardTint = new Color(0.85f, 0.55f, 1f);

        [MenuItem("SurveHive/Apply Phase 1 Look & Feel")]
        public static void Apply()
        {
            EnsureTmpEssentialResources();
            ConvertVfxMaterialsToUrp();
            EnsureCharacterSpriteImport();

            Material flashMaterial = EnsureFlashMaterial();
            Sprite stingerSprite = CreatePixelSprite("Stinger", StingerPixels);
            Sprite expSprite = CreatePixelSprite("ExpMote", ExpMotePixels);
            Sprite currencySprite = CreatePixelSprite("HoneyDrop", HoneyDropPixels);
            UiKitSprites uiKit = SliceUiKit();

            GameObject deathVfxPrefab = BuildDeathVfxPrefab();

            RebuildEnemyPrefabVisuals("Assets/Prefabs/Enemies/WorkerBee.prefab", flashMaterial);
            RebuildEnemyPrefabVisuals("Assets/Prefabs/Enemies/WarriorBee.prefab", flashMaterial);
            GameObject queensGuardPrefab = EnsureQueensGuardPrefab(flashMaterial);
            EnemyStatsSO queensGuardStats = EnsureQueensGuardStats(queensGuardPrefab);

            UpdateEnemyStatsVisuals("Assets/Data/Enemies/WorkerBee.asset", WorkerTint, 0.9f, 1f, 0f);
            UpdateEnemyStatsVisuals("Assets/Data/Enemies/WarriorBee.asset", WarriorTint, 1.1f, 1.5f, 0f);
            UpdateWaveConfig(queensGuardStats);

            UpdateProjectilePrefab(stingerSprite);
            UpdatePickupPrefab("Assets/Prefabs/Pickups/ExpPickup.prefab", expSprite);
            UpdatePickupPrefab("Assets/Prefabs/Pickups/CurrencyPickup.prefab", currencySprite);
            ConvertDamageNumberPrefabToTmp();
            AssignSkillIcons();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ApplySceneChanges(flashMaterial, uiKit, deathVfxPrefab, queensGuardPrefab, currencySprite);

            Debug.Log("SurveHive Phase 1 look & feel build complete.");
        }

        // TMP components NRE at runtime (TMP_Settings.defaultStyleSheet) without
        // the "TMP Essential Resources" imported into Assets/TextMesh Pro.
        private static void EnsureTmpEssentialResources()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null)
            {
                return;
            }

            string packagePath = System.IO.Path.GetFullPath(
                "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");

            // AssetDatabase.ImportPackage is asynchronous and never completes in a
            // -quit batch run; the internal immediate variant imports synchronously.
            var importImmediately = typeof(AssetDatabase).GetMethod(
                "ImportPackageImmediately",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (importImmediately != null)
            {
                importImmediately.Invoke(null, new object[] { packagePath });
            }
            else
            {
                AssetDatabase.ImportPackage(packagePath, false);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Phase1: imported TMP Essential Resources.");
        }

        // ------------------------------------------------------------------
        // VFX materials: the sprite-effects pack ships legacy particle shaders
        // (magenta under URP) - swap them for URP particle unlit equivalents.
        // ------------------------------------------------------------------
        private static void ConvertVfxMaterialsToUrp()
        {
            Shader urpParticles = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (urpParticles == null)
            {
                Debug.LogError("Phase1: URP particles shader not found; skipping VFX material conversion.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/ThirdParty/SpriteEffects" });
            int converted = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == null)
                {
                    continue;
                }

                string shaderName = material.shader.name;
                bool additive = shaderName.Contains("Additive");
                bool legacyParticle = shaderName.Contains("Particles") && !shaderName.Contains("Universal");
                bool broken = shaderName == "Hidden/InternalErrorShader";
                if (!legacyParticle && !broken)
                {
                    continue;
                }

                Texture mainTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;

                material.shader = urpParticles;
                if (mainTexture != null)
                {
                    material.SetTexture("_BaseMap", mainTexture);
                }

                // Transparent surface; additive keeps the legacy look for glows.
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", additive ? 2f : 0f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", additive
                    ? (float)UnityEngine.Rendering.BlendMode.One
                    : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                EditorUtility.SetDirty(material);
                converted++;
            }

            Debug.Log($"Phase1: converted {converted} VFX materials to URP particles.");
        }

        // Bee sheet must stay point-filtered, uncompressed, PPU 16 (pack default,
        // enforced so a stray reimport can't soften the art).
        private static void EnsureCharacterSpriteImport()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(BeeTexturePath);
            bool dirty = false;

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                dirty = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        private static Material EnsureFlashMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FlashMaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("SurveHive/SpriteFlash");
            if (shader == null)
            {
                Debug.LogError("Phase1: SurveHive/SpriteFlash shader not found.");
                return null;
            }

            EnsureFolder("Assets/Materials");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, FlashMaterialPath);
            return material;
        }

        // ------------------------------------------------------------------
        // Generated pixel sprites (stinger projectile + pickups). Authored as
        // pixel maps: k = dark outline, g = gold/green body, w = highlight.
        // ------------------------------------------------------------------
        private static readonly string[] StingerPixels =
        {
            "......kk....",
            ".kkkkkggk...",
            "kgwggggggkk.",
            ".kkkkkggk...",
            "......kk....",
        };

        private static readonly string[] ExpMotePixels =
        {
            "...k...",
            "..kgk..",
            ".kgwgk.",
            "kgwwwgk",
            ".kgwgk.",
            "..kgk..",
            "...k...",
        };

        private static readonly string[] HoneyDropPixels =
        {
            "...k...",
            "..kgk..",
            ".kgwgk.",
            ".kggwk.",
            "kgggggk",
            "kggggwk",
            ".kgggk.",
            "..kkk..",
        };

        private static Sprite CreatePixelSprite(string assetName, string[] rows)
        {
            bool isExp = assetName == "ExpMote";
            Color32 outline = new Color32(58, 36, 22, 255);
            Color32 body = isExp ? new Color32(124, 181, 24, 255) : new Color32(255, 195, 11, 255);
            Color32 highlight = isExp ? new Color32(220, 255, 160, 255) : new Color32(255, 240, 180, 255);
            Color32 clear = new Color32(0, 0, 0, 0);

            int height = rows.Length;
            int width = rows[0].Length;
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                string row = rows[y];
                for (int x = 0; x < width; x++)
                {
                    // rows[] is authored top-to-bottom; textures are bottom-up.
                    int targetY = height - 1 - y;
                    Color32 color = clear;
                    switch (row[x])
                    {
                        case 'k': color = outline; break;
                        case 'g': color = body; break;
                        case 'w': color = highlight; break;
                    }

                    pixels[(targetY * width) + x] = color;
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            string pngPath = $"Assets/Sprites/{assetName}.png";
            System.IO.File.WriteAllBytes(pngPath, png);
            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 16f;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        }

        // ------------------------------------------------------------------
        // DEVNIK UI kit slicing: the sheet ships unsplit, so detect the gray
        // (tintable) elements by scanning connected opaque regions and slice
        // the three we need: a large panel, a wide button, a small square.
        // ------------------------------------------------------------------
        private struct UiKitSprites
        {
            public Sprite Panel;
            public Sprite Button;
            public Sprite Square;
        }

        private struct DetectedRegion
        {
            public RectInt Bounds;
            public int PixelCount;
            public Vector3 AverageColor;
            // Fraction of pixels with visibly unequal RGB channels (e.g. the green
            // "Menu" tab fused to a panel) — used to reject non-gray elements.
            public float ColoredFraction;
        }

        private static UiKitSprites SliceUiKit()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(UiKitTexturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(UiKitTexturePath);
            List<DetectedRegion> regions = DetectOpaqueRegions(texture);

            DetectedRegion panel = default;
            DetectedRegion button = default;
            DetectedRegion square = default;
            bool foundPanel = false, foundButton = false, foundSquare = false;

            for (int i = 0; i < regions.Count; i++)
            {
                DetectedRegion region = regions[i];
                if (!IsGray(region.AverageColor) || region.ColoredFraction > 0.02f)
                {
                    continue;
                }

                float aspect = region.Bounds.width / (float)region.Bounds.height;
                bool squarish = aspect > 0.75f && aspect < 1.35f;

                if (squarish && region.Bounds.width >= 250 &&
                    (!foundPanel || region.Bounds.width > panel.Bounds.width))
                {
                    panel = region;
                    foundPanel = true;
                }
                else if (aspect >= 2f && aspect <= 6f && region.Bounds.width >= 200 && region.Bounds.height >= 60 &&
                    (!foundButton || region.Bounds.width > button.Bounds.width))
                {
                    button = region;
                    foundButton = true;
                }
                else if (squarish && region.Bounds.width >= 40 && region.Bounds.width < 250 &&
                    (!foundSquare || region.Bounds.width < square.Bounds.width))
                {
                    square = region;
                    foundSquare = true;
                }
            }

            if (!foundPanel || !foundButton || !foundSquare)
            {
                Debug.LogError($"Phase1: UI kit slicing failed (panel:{foundPanel} button:{foundButton} square:{foundSquare}).");
            }

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            // Reuse existing spriteIDs by name so re-runs keep the same fileIDs and
            // scene/prefab references to these sprites stay valid.
            var existingIds = new Dictionary<string, GUID>();
            SpriteRect[] existingRects = provider.GetSpriteRects();
            for (int i = 0; i < existingRects.Length; i++)
            {
                existingIds[existingRects[i].name] = existingRects[i].spriteID;
            }

            var rects = new List<SpriteRect>();
            AddSliceRect(rects, "PixelPanel", panel.Bounds, 18f, existingIds);
            AddSliceRect(rects, "PixelButton", button.Bounds, 14f, existingIds);
            AddSliceRect(rects, "PixelSquare", square.Bounds, 10f, existingIds);

            provider.SetSpriteRects(rects.ToArray());

            var nameFileIdProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameFileIdProvider != null)
            {
                var pairs = new List<SpriteNameFileIdPair>(rects.Count);
                for (int i = 0; i < rects.Count; i++)
                {
                    pairs.Add(new SpriteNameFileIdPair(rects[i].name, rects[i].spriteID));
                }

                nameFileIdProvider.SetNameFileIdPairs(pairs);
            }

            provider.Apply();
            importer.isReadable = false;
            importer.SaveAndReimport();

            var result = new UiKitSprites();
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(UiKitTexturePath);
            for (int i = 0; i < subAssets.Length; i++)
            {
                if (subAssets[i] is Sprite sprite)
                {
                    switch (sprite.name)
                    {
                        case "PixelPanel": result.Panel = sprite; break;
                        case "PixelButton": result.Button = sprite; break;
                        case "PixelSquare": result.Square = sprite; break;
                    }
                }
            }

            return result;
        }

        private static void AddSliceRect(
            List<SpriteRect> rects, string name, RectInt bounds, float border, Dictionary<string, GUID> existingIds)
        {
            float maxBorderX = Mathf.Floor((bounds.width - 2) / 2f);
            float maxBorderY = Mathf.Floor((bounds.height - 2) / 2f);
            float borderX = Mathf.Min(border, maxBorderX);
            float borderY = Mathf.Min(border, maxBorderY);

            if (!existingIds.TryGetValue(name, out GUID spriteId) || spriteId.Empty())
            {
                spriteId = GUID.Generate();
            }

            rects.Add(new SpriteRect
            {
                name = name,
                rect = new Rect(bounds.x, bounds.y, bounds.width, bounds.height),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = new Vector4(borderX, borderY, borderX, borderY),
                spriteID = spriteId,
            });
        }

        private static List<DetectedRegion> DetectOpaqueRegions(Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;
            Color32[] pixels = texture.GetPixels32();
            var visited = new bool[pixels.Length];
            var regions = new List<DetectedRegion>();
            var stack = new Stack<int>();

            for (int start = 0; start < pixels.Length; start++)
            {
                if (visited[start] || pixels[start].a < 200)
                {
                    continue;
                }

                int minX = width, minY = height, maxX = 0, maxY = 0;
                long sumR = 0, sumG = 0, sumB = 0;
                int count = 0;
                int coloredCount = 0;

                stack.Push(start);
                visited[start] = true;

                while (stack.Count > 0)
                {
                    int index = stack.Pop();
                    int x = index % width;
                    int y = index / width;

                    Color32 pixel = pixels[index];
                    sumR += pixel.r;
                    sumG += pixel.g;
                    sumB += pixel.b;
                    count++;
                    if (Mathf.Abs(pixel.r - pixel.g) > 30 || Mathf.Abs(pixel.g - pixel.b) > 30 || Mathf.Abs(pixel.r - pixel.b) > 30)
                    {
                        coloredCount++;
                    }

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;

                    TryVisit(pixels, visited, stack, x - 1, y, width, height);
                    TryVisit(pixels, visited, stack, x + 1, y, width, height);
                    TryVisit(pixels, visited, stack, x, y - 1, width, height);
                    TryVisit(pixels, visited, stack, x, y + 1, width, height);
                }

                if (count < 1000)
                {
                    continue;
                }

                regions.Add(new DetectedRegion
                {
                    Bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1),
                    PixelCount = count,
                    AverageColor = new Vector3(sumR / (float)count, sumG / (float)count, sumB / (float)count),
                    ColoredFraction = coloredCount / (float)count,
                });
            }

            return regions;
        }

        private static void TryVisit(Color32[] pixels, bool[] visited, Stack<int> stack, int x, int y, int width, int height)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return;
            }

            int index = (y * width) + x;
            if (visited[index] || pixels[index].a < 200)
            {
                return;
            }

            visited[index] = true;
            stack.Push(index);
        }

        // Gray but not pure white: keeps the tintable panel/button elements while
        // rejecting the sheet's white arrows and highlight slivers.
        private static bool IsGray(Vector3 averageColor)
        {
            float r = averageColor.x, g = averageColor.y, b = averageColor.z;
            return Mathf.Abs(r - g) <= 20f && Mathf.Abs(g - b) <= 20f && r >= 100f && r <= 246f;
        }

        // ------------------------------------------------------------------
        // Death VFX: pooled wrapper around a pack particle effect.
        // ------------------------------------------------------------------
        private static GameObject BuildDeathVfxPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(DeathVfxPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder("Assets/Prefabs/VFX");

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(DeathVfxSourcePath);
            var root = new GameObject("DeathPoof");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.SetParent(root.transform, false);
            instance.transform.localScale = Vector3.one * 0.6f;

            ParticleSystem rootSystem = instance.GetComponentInChildren<ParticleSystem>();
            PooledVfx pooledVfx = root.AddComponent<PooledVfx>();
            var serialized = new SerializedObject(pooledVfx);
            serialized.FindProperty("_poolId").intValue = PoolIds.DeathVfx;
            serialized.FindProperty("_rootSystem").objectReferenceValue = rootSystem;
            serialized.FindProperty("_maxLifetime").floatValue = 2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DeathVfxPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ------------------------------------------------------------------
        // Character rig: Animator (shared monster controller) on the root and a
        // "Body" child carrying SpriteRenderer + SpriteLibrary + SpriteResolver,
        // matching the paths the shared animation clips expect.
        // ------------------------------------------------------------------
        private static void BuildBeeRig(GameObject root, Material flashMaterial, int sortingOrder)
        {
            var libraryAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(BeeLibraryPath);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MonsterControllerPath);

            // Placeholder circle renderer on the root gets replaced by the rig.
            if (root.TryGetComponent(out SpriteRenderer rootRenderer))
            {
                Object.DestroyImmediate(rootRenderer);
            }

            Transform bodyTransform = root.transform.Find("Body");
            GameObject body;
            if (bodyTransform == null)
            {
                body = new GameObject("Body");
                body.transform.SetParent(root.transform, false);
            }
            else
            {
                body = bodyTransform.gameObject;
            }

            if (!body.TryGetComponent(out SpriteRenderer bodyRenderer))
            {
                bodyRenderer = body.AddComponent<SpriteRenderer>();
            }

            bodyRenderer.sprite = libraryAsset.GetSprite("Idle", "0");
            bodyRenderer.sharedMaterial = flashMaterial;
            bodyRenderer.sortingOrder = sortingOrder;

            if (!body.TryGetComponent(out SpriteLibrary spriteLibrary))
            {
                spriteLibrary = body.AddComponent<SpriteLibrary>();
            }

            spriteLibrary.spriteLibraryAsset = libraryAsset;

            if (!body.TryGetComponent(out SpriteResolver _))
            {
                body.AddComponent<SpriteResolver>();
            }

            if (!root.TryGetComponent(out Animator animator))
            {
                animator = root.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;

            if (!root.TryGetComponent(out CharacterAnimator characterAnimator))
            {
                characterAnimator = root.AddComponent<CharacterAnimator>();
            }

            var animatorSerialized = new SerializedObject(characterAnimator);
            animatorSerialized.FindProperty("_animator").objectReferenceValue = animator;
            animatorSerialized.FindProperty("_rigidbody").objectReferenceValue = root.GetComponent<Rigidbody2D>();
            animatorSerialized.FindProperty("_visualRoot").objectReferenceValue = body.transform;
            animatorSerialized.ApplyModifiedPropertiesWithoutUndo();

            if (!root.TryGetComponent(out HitFlash hitFlash))
            {
                hitFlash = root.AddComponent<HitFlash>();
            }

            var flashSerialized = new SerializedObject(hitFlash);
            flashSerialized.FindProperty("_renderer").objectReferenceValue = bodyRenderer;
            flashSerialized.FindProperty("_health").objectReferenceValue = root.GetComponent<HealthComponent>();
            flashSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RebuildEnemyPrefabVisuals(string prefabPath, Material flashMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                BuildBeeRig(root, flashMaterial, 0);

                if (root.TryGetComponent(out EnemyController enemyController))
                {
                    var serialized = new SerializedObject(enemyController);
                    serialized.FindProperty("_characterAnimator").objectReferenceValue =
                        root.GetComponent<CharacterAnimator>();
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                // The bee body (32px @ PPU 16 = 2 units) needs the bar above it.
                Transform healthBar = root.transform.Find("HealthBarCanvas");
                if (healthBar != null)
                {
                    healthBar.localPosition = new Vector3(0f, 1.1f, 0f);
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject EnsureQueensGuardPrefab(Material flashMaterial)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(QueensGuardPrefabPath);
            if (existing == null)
            {
                var go = new GameObject("QueensGuard");
                go.tag = "Enemy";

                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;

                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.5f;

                go.AddComponent<HealthComponent>();
                go.AddComponent<DamageOnContact>();
                go.AddComponent<EnemyController>();
                go.AddComponent<EnemyLoot>();

                HealthComponent health = go.GetComponent<HealthComponent>();
                BuildQueensGuardHealthBar(go.transform, health);

                PrefabUtility.SaveAsPrefabAsset(go, QueensGuardPrefabPath);
                Object.DestroyImmediate(go);
            }

            RebuildEnemyPrefabVisuals(QueensGuardPrefabPath, flashMaterial);
            return AssetDatabase.LoadAssetAtPath<GameObject>(QueensGuardPrefabPath);
        }

        // Mirrors BeehiveSceneBuilder.BuildEnemyHealthBar (kept private there).
        private static void BuildQueensGuardHealthBar(Transform parent, HealthComponent health)
        {
            var canvasGo = new GameObject("HealthBarCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            canvasGo.transform.localScale = Vector3.one * 0.01f;

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            var rect = (RectTransform)canvasGo.transform;
            rect.sizeDelta = new Vector2(100f, 14f);

            GameObject bgGo = CreateUIImage("Background", canvasGo.transform, new Color(0f, 0f, 0f, 0.6f));
            StretchFull(bgGo);

            GameObject fillGo = CreateUIImage("Fill", canvasGo.transform, DangerRed);
            StretchFull(fillGo);

            EnemyHealthBarUI healthBarUi = canvasGo.AddComponent<EnemyHealthBarUI>();
            var serialized = new SerializedObject(healthBarUi);
            serialized.FindProperty("_fillImage").objectReferenceValue = fillGo.GetComponent<Image>();
            serialized.FindProperty("_health").objectReferenceValue = health;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static EnemyStatsSO EnsureQueensGuardStats(GameObject prefab)
        {
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(QueensGuardStatsPath);
            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<EnemyStatsSO>();
                AssetDatabase.CreateAsset(stats, QueensGuardStatsPath);
            }

            var serialized = new SerializedObject(stats);
            serialized.FindProperty("_displayName").stringValue = "Queen's Guard";
            serialized.FindProperty("_rank").intValue = 2;
            serialized.FindProperty("_maxHealth").floatValue = 90f;
            serialized.FindProperty("_moveSpeed").floatValue = 2.4f;
            serialized.FindProperty("_contactDamage").floatValue = 12f;
            serialized.FindProperty("_contactDamageInterval").floatValue = 1f;
            serialized.FindProperty("_expReward").floatValue = 20f;
            serialized.FindProperty("_currencyDropChance").floatValue = 0.6f;
            serialized.FindProperty("_currencyDropMin").intValue = 2;
            serialized.FindProperty("_currencyDropMax").intValue = 5;
            serialized.FindProperty("_spriteTint").colorValue = QueensGuardTint;
            serialized.FindProperty("_scale").floatValue = 1.25f;
            serialized.FindProperty("_knockbackResistance").floatValue = 2.5f;
            serialized.FindProperty("_deathHitStopSeconds").floatValue = 0.05f;
            serialized.FindProperty("_prefab").objectReferenceValue = prefab;
            serialized.FindProperty("_poolId").intValue = PoolIds.QueensGuard;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return stats;
        }

        private static void UpdateEnemyStatsVisuals(
            string statsPath, Color tint, float scale, float knockbackResistance, float deathHitStop)
        {
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(statsPath);
            var serialized = new SerializedObject(stats);
            serialized.FindProperty("_spriteTint").colorValue = tint;
            serialized.FindProperty("_scale").floatValue = scale;
            serialized.FindProperty("_knockbackResistance").floatValue = knockbackResistance;
            serialized.FindProperty("_deathHitStopSeconds").floatValue = deathHitStop;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void UpdateWaveConfig(EnemyStatsSO queensGuardStats)
        {
            var config = AssetDatabase.LoadAssetAtPath<WaveSpawnerConfigSO>("Assets/Data/Waves/BeehiveWaveConfig.asset");
            var serialized = new SerializedObject(config);

            // PPU 16 view is ~20 world units wide; keep spawns just past its edge.
            serialized.FindProperty("_spawnRadiusMin").floatValue = 11f;
            serialized.FindProperty("_spawnRadiusMax").floatValue = 14f;

            SerializedProperty entries = serialized.FindProperty("_entries");
            bool hasQueensGuard = false;
            for (int i = 0; i < entries.arraySize; i++)
            {
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("enemyStats").objectReferenceValue == queensGuardStats)
                {
                    hasQueensGuard = true;
                    break;
                }
            }

            if (!hasQueensGuard)
            {
                int index = entries.arraySize;
                entries.arraySize = index + 1;
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("enemyStats").objectReferenceValue = queensGuardStats;
                entry.FindPropertyRelative("spawnWeight").floatValue = 0.2f;
                entry.FindPropertyRelative("unlockTimeSeconds").floatValue = 120f;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void UpdateProjectilePrefab(Sprite stingerSprite)
        {
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Projectiles/Stinger.prefab");
            try
            {
                var renderer = root.GetComponent<SpriteRenderer>();
                renderer.sprite = stingerSprite;
                renderer.color = Color.white;
                renderer.sortingOrder = 1;
                root.transform.localScale = Vector3.one;

                var collider = root.GetComponent<CircleCollider2D>();
                collider.radius = 0.25f;

                PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Projectiles/Stinger.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpdatePickupPrefab(string prefabPath, Sprite sprite)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var renderer = root.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = Color.white;
                root.transform.localScale = Vector3.one;

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConvertDamageNumberPrefabToTmp()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/DamageNumber.prefab");
            try
            {
                TMP_Text tmp = ReplaceTextWithTmp(root, font, 40f, Color.white, TextAlignmentOptions.Center);

                var popup = root.GetComponent<DamageNumberPopup>();
                var serialized = new SerializedObject(popup);
                serialized.FindProperty("_text").objectReferenceValue = tmp;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/UI/DamageNumber.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AssignSkillIcons()
        {
            AssignSkillIcon("Assets/Data/Skills/SwiftWings.asset", "Icon_PictoIcon_Speedmeter.Png");
            AssignSkillIcon("Assets/Data/Skills/ThickerChitin.asset", "Icon_PictoIcon_Defense.Png");
            AssignSkillIcon("Assets/Data/Skills/LongerStinger.asset", "Icon_PictoIcon_Target.Png");
            AssignSkillIcon("Assets/Data/Skills/TwinStingers.asset", "Icon_PictoIcon_Sword.Png");
        }

        private static void AssignSkillIcon(string skillPath, string iconFileName)
        {
            string iconPath = $"{IconFolder}/{iconFileName}";
            var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Phase1: skill icon not found at {iconPath}.");
                return;
            }

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }

            var icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(skillPath);
            var serialized = new SerializedObject(skill);
            serialized.FindProperty("_icon").objectReferenceValue = icon;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------
        // Scene pass: player rig, feel components, new pools, and UI reskin.
        // ------------------------------------------------------------------
        private static void ApplySceneChanges(
            Material flashMaterial, UiKitSprites uiKit, GameObject deathVfxPrefab,
            GameObject queensGuardPrefab, Sprite currencySprite)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Post-open reloads (same fake-null hazard as the other scene passes).
            flashMaterial = AssetDatabase.LoadAssetAtPath<Material>(FlashMaterialPath);
            deathVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeathVfxPrefabPath);
            queensGuardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(QueensGuardPrefabPath);
            currencySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HoneyDrop.png");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            uiKit = ReloadUiKitSprites();

            GameObject cameraGo = GameObject.FindWithTag("MainCamera");
            BeehiveSceneBuilder.ConfigurePixelPerfectCamera(cameraGo);

            if (!cameraGo.TryGetComponent(out CameraShaker shaker))
            {
                shaker = cameraGo.AddComponent<CameraShaker>();
            }

            var followSerialized = new SerializedObject(cameraGo.GetComponent<CameraFollow>());
            followSerialized.FindProperty("_shaker").objectReferenceValue = shaker;
            followSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject bootstrapGo = GameObject.Find("GameBootstrap");
            if (!bootstrapGo.TryGetComponent(out HitStop _))
            {
                bootstrapGo.AddComponent<HitStop>();
            }

            // Player rig + feedback.
            GameObject playerGo = GameObject.Find("Player");
            BuildBeeRig(playerGo, flashMaterial, 2);

            if (!playerGo.TryGetComponent(out PlayerHitFeedback hitFeedback))
            {
                hitFeedback = playerGo.AddComponent<PlayerHitFeedback>();
            }

            var feedbackSerialized = new SerializedObject(hitFeedback);
            feedbackSerialized.FindProperty("_health").objectReferenceValue = playerGo.GetComponent<HealthComponent>();
            feedbackSerialized.FindProperty("_shaker").objectReferenceValue = shaker;
            feedbackSerialized.ApplyModifiedPropertiesWithoutUndo();

            var autoAttackSerialized = new SerializedObject(playerGo.GetComponent<AutoAttack>());
            autoAttackSerialized.FindProperty("_characterAnimator").objectReferenceValue =
                playerGo.GetComponent<CharacterAnimator>();
            autoAttackSerialized.ApplyModifiedPropertiesWithoutUndo();

            // New pools.
            var bootstrapSerialized = new SerializedObject(bootstrapGo.GetComponent<GameBootstrap>());
            SerializedProperty pools = bootstrapSerialized.FindProperty("_pools");
            EnsurePoolEntry(pools, PoolIds.QueensGuard, queensGuardPrefab, 5, 20);
            EnsurePoolEntry(pools, PoolIds.DeathVfx, deathVfxPrefab, 12, 40);
            bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

            ReskinUi(font, uiKit, currencySprite, bootstrapGo);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static UiKitSprites ReloadUiKitSprites()
        {
            var result = new UiKitSprites();
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(UiKitTexturePath);
            for (int i = 0; i < subAssets.Length; i++)
            {
                if (subAssets[i] is Sprite sprite)
                {
                    switch (sprite.name)
                    {
                        case "PixelPanel": result.Panel = sprite; break;
                        case "PixelButton": result.Button = sprite; break;
                        case "PixelSquare": result.Square = sprite; break;
                    }
                }
            }

            return result;
        }

        private static void EnsurePoolEntry(SerializedProperty pools, int poolId, GameObject prefab, int prewarm, int maxSize)
        {
            for (int i = 0; i < pools.arraySize; i++)
            {
                if (pools.GetArrayElementAtIndex(i).FindPropertyRelative("poolId").intValue == poolId)
                {
                    pools.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue = prefab;
                    return;
                }
            }

            int index = pools.arraySize;
            pools.arraySize = index + 1;
            SerializedProperty entry = pools.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("poolId").intValue = poolId;
            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            entry.FindPropertyRelative("prewarmCount").intValue = prewarm;
            entry.FindPropertyRelative("maxSize").intValue = maxSize;
        }

        // ------------------------------------------------------------------
        // UI reskin: pixel kit sprites + BoldPixels TMP + honey palette.
        // ------------------------------------------------------------------
        private static void ReskinUi(TMP_FontAsset font, UiKitSprites uiKit, Sprite currencySprite, GameObject bootstrapGo)
        {
            GameObject canvasGo = GameObject.Find("Canvas");
            Transform canvas = canvasGo.transform;

            // HUD bars.
            GameObject healthBg = canvas.Find("HealthBarBackground").gameObject;
            SetSlicedSprite(healthBg, uiKit.Square, DeepBrown, 0.95f);
            GameObject healthFill = healthBg.transform.Find("HealthBarFill").gameObject;
            SetSlicedSprite(healthFill, uiKit.Square, DangerRed, 1f);

            GameObject expBg = canvas.Find("ExpBarBackground").gameObject;
            SetSlicedSprite(expBg, uiKit.Square, DeepBrown, 0.95f);
            GameObject expFill = expBg.transform.Find("ExpBarFill").gameObject;
            SetSlicedSprite(expFill, uiKit.Square, HoneyGold, 1f);

            // Level label.
            GameObject levelTextGo = canvas.Find("LevelText").gameObject;
            TMP_Text levelTmp = ReplaceTextWithTmp(levelTextGo, font, 24f, Wax, TextAlignmentOptions.Left);
            var expBarSerialized = new SerializedObject(expBg.GetComponent<ExpBarUI>());
            expBarSerialized.FindProperty("_levelText").objectReferenceValue = levelTmp;
            expBarSerialized.ApplyModifiedPropertiesWithoutUndo();

            // Currency counter + honey-drop icon.
            GameObject currencyTextGo = canvas.Find("CurrencyText").gameObject;
            TMP_Text currencyTmp = ReplaceTextWithTmp(currencyTextGo, font, 26f, HoneyGold, TextAlignmentOptions.Right);
            var currencySerialized = new SerializedObject(currencyTextGo.GetComponent<CurrencyCounterUI>());
            currencySerialized.FindProperty("_currencyText").objectReferenceValue = currencyTmp;
            currencySerialized.ApplyModifiedPropertiesWithoutUndo();

            EnsureHudIcon(canvas, "CurrencyIcon", currencySprite, HoneyGold,
                new Vector2(1f, 1f), new Vector2(-235f, -20f), new Vector2(30f, 30f));

            // Kill counter (skull icon + count) and run timer.
            RunSession runSession = bootstrapGo.GetComponent<RunSession>();
            EnsureCounterText(canvas, "KillCounterText", font, 26f, Wax, TextAlignmentOptions.Right,
                new Vector2(1f, 1f), new Vector2(-120f, -60f), new Vector2(100f, 30f),
                go =>
                {
                    if (!go.TryGetComponent(out KillCounterUI killCounter))
                    {
                        killCounter = go.AddComponent<KillCounterUI>();
                    }

                    var serialized = new SerializedObject(killCounter);
                    serialized.FindProperty("_text").objectReferenceValue = go.GetComponent<TMP_Text>();
                    serialized.FindProperty("_session").objectReferenceValue = runSession;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                });

            Sprite skullIcon = LoadIconSprite("Icon_PictoIcon_Skull.Png");
            EnsureHudIcon(canvas, "KillCounterIcon", skullIcon, Wax,
                new Vector2(1f, 1f), new Vector2(-235f, -60f), new Vector2(26f, 26f));

            EnsureCounterText(canvas, "RunTimerText", font, 30f, Wax, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(160f, 34f),
                go =>
                {
                    if (!go.TryGetComponent(out RunTimerUI timer))
                    {
                        timer = go.AddComponent<RunTimerUI>();
                    }

                    var serialized = new SerializedObject(timer);
                    serialized.FindProperty("_text").objectReferenceValue = go.GetComponent<TMP_Text>();
                    serialized.FindProperty("_session").objectReferenceValue = runSession;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                });

            // Joystick.
            Transform joystickBg = canvas.Find("JoystickBackground");
            if (joystickBg != null)
            {
                SetSlicedSprite(joystickBg.gameObject, uiKit.Square, new Color(1f, 1f, 1f, 0.12f), 1f);
                Transform handle = joystickBg.Find("JoystickHandle");
                if (handle != null)
                {
                    SetSlicedSprite(handle.gameObject, uiKit.Square, new Color(1f, 0.85f, 0.4f, 0.45f), 1f);
                }
            }

            ReskinLevelUpPanel(canvas, font, uiKit);
            ReskinGameOverPanel(canvas, font, uiKit);
        }

        private static void ReskinLevelUpPanel(Transform canvas, TMP_FontAsset font, UiKitSprites uiKit)
        {
            Transform panelTransform = canvas.Find("LevelUpPanel");
            GameObject panel = panelTransform.gameObject;
            // The panel object must stay active for its controller to subscribe to
            // level-up events; the CanvasGroup handles hiding (see controller Awake).
            panel.SetActive(true);
            SetSlicedSprite(panel, uiKit.Panel, new Color(DeepBrown.r, DeepBrown.g, DeepBrown.b, 0.97f), 1f);

            LevelUpUIController controller = panel.GetComponent<LevelUpUIController>();
            var controllerSerialized = new SerializedObject(controller);
            SerializedProperty namesProp = controllerSerialized.FindProperty("_choiceNameTexts");
            SerializedProperty descsProp = controllerSerialized.FindProperty("_choiceDescriptionTexts");
            SerializedProperty iconsProp = controllerSerialized.FindProperty("_choiceIcons");
            namesProp.arraySize = 3;
            descsProp.arraySize = 3;
            iconsProp.arraySize = 3;

            for (int i = 0; i < 3; i++)
            {
                Transform choice = panelTransform.Find($"Choice{i}");
                SetSlicedSprite(choice.gameObject, uiKit.Button, Wax, 1f);

                GameObject nameGo = choice.Find($"Choice{i}Name").gameObject;
                TMP_Text nameTmp = ReplaceTextWithTmp(nameGo, font, 26f, DeepBrown, TextAlignmentOptions.Center);
                namesProp.GetArrayElementAtIndex(i).objectReferenceValue = nameTmp;

                GameObject descGo = choice.Find($"Choice{i}Desc").gameObject;
                TMP_Text descTmp = ReplaceTextWithTmp(descGo, font, 20f, CombBrown, TextAlignmentOptions.Center);
                descsProp.GetArrayElementAtIndex(i).objectReferenceValue = descTmp;

                Transform iconTransform = choice.Find("Icon");
                GameObject iconGo;
                if (iconTransform == null)
                {
                    iconGo = new GameObject("Icon", typeof(RectTransform));
                    iconGo.transform.SetParent(choice, false);
                    var rect = (RectTransform)iconGo.transform;
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(0f, -140f);
                    rect.sizeDelta = new Vector2(96f, 96f);
                }
                else
                {
                    iconGo = iconTransform.gameObject;
                }

                if (!iconGo.TryGetComponent(out Image iconImage))
                {
                    iconImage = iconGo.AddComponent<Image>();
                }

                iconImage.color = DeepBrown;
                iconImage.raycastTarget = false;
                iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = iconImage;
            }

            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ReskinGameOverPanel(Transform canvas, TMP_FontAsset font, UiKitSprites uiKit)
        {
            Transform panelTransform = canvas.Find("GameOverPanel");
            if (panelTransform == null)
            {
                return;
            }

            SetSlicedSprite(panelTransform.gameObject, uiKit.Panel,
                new Color(DeepBrown.r * 0.6f, DeepBrown.g * 0.6f, DeepBrown.b * 0.6f, 0.97f), 1f);

            Transform title = panelTransform.Find("GameOverTitle");
            if (title != null)
            {
                ReplaceTextWithTmp(title.gameObject, font, 90f, DangerRed, TextAlignmentOptions.Center);
            }

            Transform hint = panelTransform.Find("GameOverHint");
            if (hint != null)
            {
                ReplaceTextWithTmp(hint.gameObject, font, 34f, Wax, TextAlignmentOptions.Center);
            }
        }

        private static TMP_Text ReplaceTextWithTmp(
            GameObject go, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
        {
            string existingText = null;
            Text legacy = go.GetComponentInChildren<Text>();
            if (legacy != null)
            {
                existingText = legacy.text;
                GameObject legacyGo = legacy.gameObject;
                Object.DestroyImmediate(legacy);
                go = legacyGo;
            }

            if (!go.TryGetComponent(out TextMeshProUGUI tmp))
            {
                tmp = go.AddComponent<TextMeshProUGUI>();
            }

            if (font != null)
            {
                tmp.font = font;
            }

            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            if (!string.IsNullOrEmpty(existingText))
            {
                tmp.text = existingText;
            }

            return tmp;
        }

        private static void SetSlicedSprite(GameObject go, Sprite sprite, Color color, float alphaMultiplier)
        {
            if (!go.TryGetComponent(out Image image))
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = new Color(color.r, color.g, color.b, color.a * alphaMultiplier);
        }

        private static void EnsureHudIcon(
            Transform canvas, string name, Sprite sprite, Color color,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            Transform existing = canvas.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(canvas, false);
            }
            else
            {
                go = existing.gameObject;
            }

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            if (!go.TryGetComponent(out Image image))
            {
                image = go.AddComponent<Image>();
            }

            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
        }

        private static void EnsureCounterText(
            Transform canvas, string name, TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions alignment, Vector2 anchor, Vector2 position, Vector2 rectSize,
            System.Action<GameObject> wire)
        {
            Transform existing = canvas.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(canvas, false);
            }
            else
            {
                go = existing.gameObject;
            }

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = rectSize;

            if (!go.TryGetComponent(out TextMeshProUGUI tmp))
            {
                tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = "0";
            }

            if (font != null)
            {
                tmp.font = font;
            }

            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;

            wire(go);
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

        private static GameObject CreateUIImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private static void StretchFull(GameObject go)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
