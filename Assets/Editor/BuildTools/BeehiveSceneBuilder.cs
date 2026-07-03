using System.IO;
using SurveHive.Combat;
using SurveHive.Core;
using SurveHive.Currency;
using SurveHive.Data;
using SurveHive.Enemies;
using SurveHive.Health;
using SurveHive.Input;
using SurveHive.Pickups;
using SurveHive.Player;
using SurveHive.Progression;
using SurveHive.Spawning;
using SurveHive.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurveHive.BuildTools
{
    public static class BeehiveSceneBuilder
    {
        [MenuItem("SurveHive/Build Beehive Vertical Slice")]
        public static void Build()
        {
            EnsureTagExists("Enemy");

            Sprite playerSprite = CreatePlaceholderSprite("Player", new Color(1f, 0.85f, 0.1f));
            Sprite workerSprite = CreatePlaceholderSprite("WorkerBee", new Color(0.55f, 0.35f, 0.15f));
            Sprite warriorSprite = CreatePlaceholderSprite("WarriorBee", new Color(0.5f, 0.05f, 0.05f));
            Sprite expSprite = CreatePlaceholderSprite("ExpPickup", new Color(0.2f, 0.9f, 0.3f));
            Sprite currencySprite = CreatePlaceholderSprite("CurrencyPickup", new Color(1f, 0.84f, 0f));
            Sprite projectileSprite = CreatePlaceholderSprite("Projectile", Color.white);

            InputActionAsset inputActions = BuildInputActionsAsset();

            GameObject workerPrefab = BuildEnemyPrefab("WorkerBee", workerSprite, "Assets/Prefabs/Enemies/WorkerBee.prefab");
            GameObject warriorPrefab = BuildEnemyPrefab("WarriorBee", warriorSprite, "Assets/Prefabs/Enemies/WarriorBee.prefab");
            GameObject expPickupPrefab = BuildPickupPrefab("ExpPickup", expSprite, PoolIds.ExpPickup, "Assets/Prefabs/Pickups/ExpPickup.prefab");
            GameObject currencyPickupPrefab = BuildPickupPrefab("CurrencyPickup", currencySprite, PoolIds.CurrencyPickup, "Assets/Prefabs/Pickups/CurrencyPickup.prefab");
            GameObject projectilePrefab = BuildProjectilePrefab(projectileSprite, PoolIds.Projectile, "Assets/Prefabs/Projectiles/Stinger.prefab");

            EnemyStatsSO workerStats = CreateEnemyStats("Assets/Data/Enemies/WorkerBee.asset", "Worker Bee", 0, 20f, 2.2f, 4f, 1f, 4f, 0.25f, 1, 2, workerPrefab, PoolIds.WorkerBee);
            EnemyStatsSO warriorStats = CreateEnemyStats("Assets/Data/Enemies/WarriorBee.asset", "Warrior Bee", 1, 45f, 2.6f, 8f, 1f, 9f, 0.35f, 2, 4, warriorPrefab, PoolIds.WarriorBee);

            WaveSpawnerConfigSO waveConfig = CreateWaveConfig(workerStats, warriorStats);
            LevelCurveSO levelCurve = CreateLevelCurve();
            RuntimeMetaProgressionStoreSO metaStore = CreateMetaStore();
            SkillDatabaseSO skillDatabase = CreateSkillDatabase();

            BuildScene(inputActions, playerSprite, waveConfig, levelCurve, skillDatabase, metaStore,
                workerPrefab, warriorPrefab, expPickupPrefab, currencyPickupPrefab, projectilePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("SurveHive Beehive vertical slice build complete.");
        }

        // Additive pass on top of an already-built scene: unlike Build(), this edits the
        // existing Beehive.unity/enemy prefabs in place rather than recreating everything
        // from scratch, so it doesn't hit AssetDatabase.CreateAsset's "already exists"
        // failure for the ScriptableObject data assets Build() creates.
        [MenuItem("SurveHive/Add Health Bars, Damage Numbers, SFX")]
        public static void BuildAdditions()
        {
            AddHealthBarToEnemyPrefab("Assets/Prefabs/Enemies/WorkerBee.prefab");
            AddHealthBarToEnemyPrefab("Assets/Prefabs/Enemies/WarriorBee.prefab");

            BuildDamageNumberPrefab("Assets/Prefabs/UI/DamageNumber.prefab");
            BuildShootSfx("Assets/Audio/Shoot.wav");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            WireSceneAdditions();

            Debug.Log("SurveHive Beehive additions build complete.");
        }

        // Additive pass: adds a full-screen game-over panel to the HUD canvas and a
        // PlayerDeathHandler on the player, wired to freeze + show the panel on death.
        // Idempotent: safe to re-run.
        [MenuItem("SurveHive/Add Game Over + Death Handling")]
        public static void BuildGameOverAndDeath()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Beehive.unity", OpenSceneMode.Single);

            GameObject canvasGo = GameObject.Find("Canvas");
            GameObject playerGo = GameObject.Find("Player");

            Transform existingPanel = canvasGo.transform.Find("GameOverPanel");
            GameObject gameOverPanelGo;
            if (existingPanel != null)
            {
                gameOverPanelGo = existingPanel.gameObject;
            }
            else
            {
                gameOverPanelGo = CreateUIImage("GameOverPanel", canvasGo.transform, new Color(0f, 0f, 0f, 0.85f));
                StretchFull(gameOverPanelGo);

                GameObject titleGo = CreateUIText("GameOverTitle", gameOverPanelGo.transform, "YOU DIED");
                SetAnchoredRect(titleGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(900f, 200f));
                Text titleText = titleGo.GetComponent<Text>();
                titleText.fontSize = 96;
                titleText.color = new Color(0.9f, 0.15f, 0.15f, 1f);

                GameObject hintGo = CreateUIText("GameOverHint", gameOverPanelGo.transform, "Press R or tap to restart");
                SetAnchoredRect(hintGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(900f, 80f));
                hintGo.GetComponent<Text>().fontSize = 40;

                gameOverPanelGo.SetActive(false);
            }

            if (!playerGo.TryGetComponent(out PlayerDeathHandler deathHandler))
            {
                deathHandler = playerGo.AddComponent<PlayerDeathHandler>();
            }

            HealthComponent playerHealth = playerGo.GetComponent<HealthComponent>();
            var deathSerialized = new SerializedObject(deathHandler);
            deathSerialized.FindProperty("_health").objectReferenceValue = playerHealth;
            deathSerialized.FindProperty("_gameOverPanel").objectReferenceValue = gameOverPanelGo;
            deathSerialized.ApplyModifiedPropertiesWithoutUndo();

            EnsureSceneInBuildSettings("Assets/Scenes/Beehive.unity");

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("SurveHive game-over + death handling build complete.");
        }

        // Additive pass: adds the URP Pixel Perfect Camera to the scene's main camera
        // (PLAN.md Phase 0 rendering foundation). Idempotent: safe to re-run.
        [MenuItem("SurveHive/Apply Pixel Perfect Camera")]
        public static void ApplyPixelPerfectCamera()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Beehive.unity", OpenSceneMode.Single);

            GameObject cameraGo = GameObject.FindWithTag("MainCamera");
            ConfigurePixelPerfectCamera(cameraGo);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("SurveHive pixel perfect camera applied.");
        }

        // PLAN.md rendering foundation, revised in Phase 1: the PixelFantasy art is
        // authored at PPU 16, so the whole game runs at PPU 16 with a 320x180
        // reference resolution (integer 6x scale at 1080p) and an upscaled render
        // texture so rotated sprites/VFX stay crisp.
        public static void ConfigurePixelPerfectCamera(GameObject cameraGo)
        {
            if (!cameraGo.TryGetComponent(out PixelPerfectCamera pixelPerfect))
            {
                pixelPerfect = cameraGo.AddComponent<PixelPerfectCamera>();
            }

            pixelPerfect.assetsPPU = 16;
            pixelPerfect.refResolutionX = 320;
            pixelPerfect.refResolutionY = 180;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.None;
        }

        // Runtime scene reload (SceneManager.LoadScene by name) requires the scene to
        // be present and enabled in the Build Settings scene list.
        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    if (!scenes[i].enabled)
                    {
                        scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                        EditorBuildSettings.scenes = scenes.ToArray();
                    }

                    return;
                }
            }

            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddHealthBarToEnemyPrefab(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (root.transform.Find("HealthBarCanvas") != null)
                {
                    return;
                }

                HealthComponent health = root.GetComponent<HealthComponent>();
                BuildEnemyHealthBar(root.transform, health);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildEnemyHealthBar(Transform parent, HealthComponent health)
        {
            var canvasGo = new GameObject("HealthBarCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            canvasGo.transform.localScale = Vector3.one * 0.01f;

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            var rect = (RectTransform)canvasGo.transform;
            rect.sizeDelta = new Vector2(100f, 14f);

            GameObject bgGo = CreateUIImage("Background", canvasGo.transform, new Color(0f, 0f, 0f, 0.6f));
            StretchFull(bgGo);

            GameObject fillGo = CreateUIImage("Fill", canvasGo.transform, new Color(0.85f, 0.1f, 0.1f, 1f));
            Image fillImage = fillGo.GetComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            StretchFull(fillGo);

            EnemyHealthBarUI healthBarUi = canvasGo.AddComponent<EnemyHealthBarUI>();
            var serialized = new SerializedObject(healthBarUi);
            serialized.FindProperty("_fillImage").objectReferenceValue = fillImage;
            serialized.FindProperty("_health").objectReferenceValue = health;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(assetFolderPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(assetFolderPath);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static GameObject BuildDamageNumberPrefab(string prefabPath)
        {
            EnsureFolder(Path.GetDirectoryName(prefabPath)?.Replace('\\', '/'));

            var go = new GameObject("DamageNumber", typeof(RectTransform));
            go.transform.localScale = Vector3.one * 0.02f;

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;

            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(200f, 80f);

            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 40;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "0";

            DamageNumberPopup popup = go.AddComponent<DamageNumberPopup>();
            var serialized = new SerializedObject(popup);
            serialized.FindProperty("_text").objectReferenceValue = text;
            serialized.FindProperty("_poolId").intValue = PoolIds.DamageNumber;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void BuildShootSfx(string path)
        {
            const int sampleRate = 22050;
            const float duration = 0.12f;
            int sampleCount = (int)(sampleRate * duration);
            var samples = new short[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float frequency = Mathf.Lerp(950f, 280f, t / duration);
                float envelope = 1f - (t / duration);
                float sample = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.5f;
                samples[i] = (short)(sample * short.MaxValue);
            }

            byte[] wavBytes = EncodeWav(samples, sampleRate);

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(path, wavBytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var audioImporter = (AudioImporter)AssetImporter.GetAtPath(path);
            AudioImporterSampleSettings sampleSettings = audioImporter.defaultSampleSettings;
            sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
            sampleSettings.compressionFormat = AudioCompressionFormat.PCM;
            audioImporter.defaultSampleSettings = sampleSettings;
            audioImporter.SaveAndReimport();
        }

        private static byte[] EncodeWav(short[] samples, int sampleRate)
        {
            const int bitsPerSample = 16;
            const int channels = 1;
            int byteRate = sampleRate * channels * (bitsPerSample / 8);
            int dataSize = samples.Length * (bitsPerSample / 8);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)(channels * (bitsPerSample / 8)));
            writer.Write((short)bitsPerSample);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            foreach (short sample in samples)
            {
                writer.Write(sample);
            }

            return stream.ToArray();
        }

        private static void WireSceneAdditions()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Beehive.unity", OpenSceneMode.Single);

            // Reload fresh references post-scene-switch rather than trusting any
            // pre-switch in-memory ones - same fake-null risk as EditorSceneManager.NewScene.
            GameObject damageNumberPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/DamageNumber.prefab");
            AudioClip shootClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Shoot.wav");

            GameObject gameBootstrapGo = GameObject.Find("GameBootstrap");
            GameBootstrap gameBootstrap = gameBootstrapGo.GetComponent<GameBootstrap>();
            var bootstrapSerialized = new SerializedObject(gameBootstrap);
            SerializedProperty poolsProp = bootstrapSerialized.FindProperty("_pools");

            bool hasDamageNumberPool = false;
            for (int i = 0; i < poolsProp.arraySize; i++)
            {
                if (poolsProp.GetArrayElementAtIndex(i).FindPropertyRelative("poolId").intValue == PoolIds.DamageNumber)
                {
                    hasDamageNumberPool = true;
                    break;
                }
            }

            if (!hasDamageNumberPool)
            {
                int newIndex = poolsProp.arraySize;
                poolsProp.arraySize = newIndex + 1;
                SerializedProperty entry = poolsProp.GetArrayElementAtIndex(newIndex);
                entry.FindPropertyRelative("poolId").intValue = PoolIds.DamageNumber;
                entry.FindPropertyRelative("prefab").objectReferenceValue = damageNumberPrefab;
                entry.FindPropertyRelative("prewarmCount").intValue = 20;
                entry.FindPropertyRelative("maxSize").intValue = 100;
            }

            bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject playerGo = GameObject.Find("Player");
            AutoAttack autoAttack = playerGo.GetComponent<AutoAttack>();

            if (!playerGo.TryGetComponent(out AudioSource audioSource))
            {
                audioSource = playerGo.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            var autoAttackSerialized = new SerializedObject(autoAttack);
            autoAttackSerialized.FindProperty("_audioSource").objectReferenceValue = audioSource;
            autoAttackSerialized.FindProperty("_shootClip").objectReferenceValue = shootClip;
            autoAttackSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void EnsureTagExists(string tag)
        {
            string[] existingTags = InternalEditorUtility.tags;
            for (int i = 0; i < existingTags.Length; i++)
            {
                if (existingTags[i] == tag)
                {
                    return;
                }
            }

            InternalEditorUtility.AddTag(tag);
        }

        private static Sprite CreatePlaceholderSprite(string assetName, Color color)
        {
            const int size = 64;
            string pngPath = $"Assets/Sprites/{assetName}.png";

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float radius = size / 2f;
            Vector2 center = new Vector2(radius, radius);
            Color32 opaque = color;
            Color32 clear = new Color32(0, 0, 0, 0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - center.x;
                    float dy = y + 0.5f - center.y;
                    bool inside = (dx * dx) + (dy * dy) <= radius * radius;
                    pixels[(y * size) + x] = inside ? opaque : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            File.WriteAllBytes(pngPath, png);
            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 64f;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        }

        private static InputActionAsset BuildInputActionsAsset()
        {
            const string path = "Assets/Settings/GameplayControls.inputactions";

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap gameplayMap = asset.AddActionMap("Gameplay");

            InputAction moveAction = gameplayMap.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            gameplayMap.AddAction("PointerPosition", InputActionType.PassThrough, binding: "<Mouse>/position");
            gameplayMap.AddAction("PointerClick", InputActionType.Button, binding: "<Mouse>/leftButton");

            asset.AddControlScheme("Keyboard&Mouse").WithRequiredDevice("<Keyboard>").WithRequiredDevice("<Mouse>");
            asset.AddControlScheme("Touch").WithRequiredDevice("<Touchscreen>");

            string json = asset.ToJson();
            Object.DestroyImmediate(asset);

            File.WriteAllText(path, json);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }

        private static GameObject BuildEnemyPrefab(string name, Sprite sprite, string prefabPath)
        {
            var go = new GameObject(name);
            go.tag = "Enemy";

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

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

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildPickupPrefab(string name, Sprite sprite, int poolId, string prefabPath)
        {
            var go = new GameObject(name);
            go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            var pickup = go.AddComponent<PickupItem>();
            var serialized = new SerializedObject(pickup);
            serialized.FindProperty("_poolId").intValue = poolId;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildProjectilePrefab(Sprite sprite, int poolId, string prefabPath)
        {
            var go = new GameObject("Stinger");
            go.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            var projectile = go.AddComponent<Projectile>();
            var serialized = new SerializedObject(projectile);
            serialized.FindProperty("_poolId").intValue = poolId;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static EnemyStatsSO CreateEnemyStats(
            string assetPath, string displayName, int rank, float maxHealth, float moveSpeed,
            float contactDamage, float contactInterval, float expReward, float currencyDropChance,
            int currencyMin, int currencyMax, GameObject prefab, int poolId)
        {
            var so = ScriptableObject.CreateInstance<EnemyStatsSO>();
            var serialized = new SerializedObject(so);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_rank").intValue = rank;
            serialized.FindProperty("_maxHealth").floatValue = maxHealth;
            serialized.FindProperty("_moveSpeed").floatValue = moveSpeed;
            serialized.FindProperty("_contactDamage").floatValue = contactDamage;
            serialized.FindProperty("_contactDamageInterval").floatValue = contactInterval;
            serialized.FindProperty("_expReward").floatValue = expReward;
            serialized.FindProperty("_currencyDropChance").floatValue = currencyDropChance;
            serialized.FindProperty("_currencyDropMin").intValue = currencyMin;
            serialized.FindProperty("_currencyDropMax").intValue = currencyMax;
            serialized.FindProperty("_spriteTint").colorValue = Color.white;
            serialized.FindProperty("_prefab").objectReferenceValue = prefab;
            serialized.FindProperty("_poolId").intValue = poolId;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(so, assetPath);
            return so;
        }

        private static WaveSpawnerConfigSO CreateWaveConfig(EnemyStatsSO workerStats, EnemyStatsSO warriorStats)
        {
            var waveConfig = ScriptableObject.CreateInstance<WaveSpawnerConfigSO>();
            var serialized = new SerializedObject(waveConfig);

            SerializedProperty entriesProp = serialized.FindProperty("_entries");
            entriesProp.arraySize = 2;

            SerializedProperty entry0 = entriesProp.GetArrayElementAtIndex(0);
            entry0.FindPropertyRelative("enemyStats").objectReferenceValue = workerStats;
            entry0.FindPropertyRelative("spawnWeight").floatValue = 1f;
            entry0.FindPropertyRelative("unlockTimeSeconds").floatValue = 0f;

            SerializedProperty entry1 = entriesProp.GetArrayElementAtIndex(1);
            entry1.FindPropertyRelative("enemyStats").objectReferenceValue = warriorStats;
            entry1.FindPropertyRelative("spawnWeight").floatValue = 0.4f;
            entry1.FindPropertyRelative("unlockTimeSeconds").floatValue = 45f;

            serialized.FindProperty("_initialSpawnInterval").floatValue = 1.5f;
            serialized.FindProperty("_minSpawnInterval").floatValue = 0.25f;
            serialized.FindProperty("_intervalRampPerMinute").floatValue = 0.2f;
            serialized.FindProperty("_maxConcurrentEnemies").intValue = 60;
            serialized.FindProperty("_spawnRadiusMin").floatValue = 6f;
            serialized.FindProperty("_spawnRadiusMax").floatValue = 9f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(waveConfig, "Assets/Data/Waves/BeehiveWaveConfig.asset");
            return waveConfig;
        }

        private static LevelCurveSO CreateLevelCurve()
        {
            var levelCurve = ScriptableObject.CreateInstance<LevelCurveSO>();
            AssetDatabase.CreateAsset(levelCurve, "Assets/Data/Progression/LevelCurve.asset");
            return levelCurve;
        }

        private static RuntimeMetaProgressionStoreSO CreateMetaStore()
        {
            var metaStore = ScriptableObject.CreateInstance<RuntimeMetaProgressionStoreSO>();
            AssetDatabase.CreateAsset(metaStore, "Assets/Data/Progression/RuntimeMetaProgressionStore.asset");
            return metaStore;
        }

        private static SkillDefinitionSO CreateSkill(
            string assetPath, string id, string displayName, string description,
            SkillEffectType effectType, float magnitude, float weight)
        {
            var so = ScriptableObject.CreateInstance<SkillDefinitionSO>();
            var serialized = new SerializedObject(so);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_description").stringValue = description;
            serialized.FindProperty("_effectType").enumValueIndex = (int)effectType;
            serialized.FindProperty("_magnitude").floatValue = magnitude;
            serialized.FindProperty("_weight").floatValue = weight;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(so, assetPath);
            return so;
        }

        private static SkillDatabaseSO CreateSkillDatabase()
        {
            SkillDefinitionSO swiftWings = CreateSkill("Assets/Data/Skills/SwiftWings.asset", "swift_wings", "Swift Wings", "+10% move speed.", SkillEffectType.MoveSpeedPercent, 10f, 1f);
            SkillDefinitionSO thickerChitin = CreateSkill("Assets/Data/Skills/ThickerChitin.asset", "thicker_chitin", "Thicker Chitin", "+20 max health.", SkillEffectType.MaxHealthFlat, 20f, 1f);
            SkillDefinitionSO longerStinger = CreateSkill("Assets/Data/Skills/LongerStinger.asset", "longer_stinger", "Longer Stinger", "+15% attack range.", SkillEffectType.AttackRangePercent, 15f, 1f);
            SkillDefinitionSO twinStingers = CreateSkill("Assets/Data/Skills/TwinStingers.asset", "twin_stingers", "Twin Stingers", "+1 projectile per attack.", SkillEffectType.ProjectileCountFlat, 1f, 0.6f);

            var skills = new[] { swiftWings, thickerChitin, longerStinger, twinStingers };

            var database = ScriptableObject.CreateInstance<SkillDatabaseSO>();
            var serialized = new SerializedObject(database);
            SerializedProperty skillsProp = serialized.FindProperty("_skills");
            skillsProp.arraySize = skills.Length;
            for (int i = 0; i < skills.Length; i++)
            {
                skillsProp.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(database, "Assets/Data/Skills/SkillDatabase.asset");
            return database;
        }

        private static void BuildScene(
            InputActionAsset inputActions,
            Sprite playerSprite,
            WaveSpawnerConfigSO waveConfig,
            LevelCurveSO levelCurve,
            SkillDatabaseSO skillDatabase,
            RuntimeMetaProgressionStoreSO metaStore,
            GameObject workerPrefab,
            GameObject warriorPrefab,
            GameObject expPickupPrefab,
            GameObject currencyPickupPrefab,
            GameObject projectilePrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // EditorSceneManager.NewScene(..., NewSceneMode.Single) unloads unreferenced assets,
            // which invalidates in-memory ScriptableObject/InputActionAsset instances created
            // earlier in this same method (nothing referenced them yet, so they get GC'd even
            // though their .asset files are already saved on disk). Reload fresh references from
            // disk post-switch instead of relying on the pre-switch instances.
            inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/GameplayControls.inputactions");
            waveConfig = AssetDatabase.LoadAssetAtPath<WaveSpawnerConfigSO>("Assets/Data/Waves/BeehiveWaveConfig.asset");
            levelCurve = AssetDatabase.LoadAssetAtPath<LevelCurveSO>("Assets/Data/Progression/LevelCurve.asset");
            skillDatabase = AssetDatabase.LoadAssetAtPath<SkillDatabaseSO>("Assets/Data/Skills/SkillDatabase.asset");
            metaStore = AssetDatabase.LoadAssetAtPath<RuntimeMetaProgressionStoreSO>("Assets/Data/Progression/RuntimeMetaProgressionStore.asset");

            var bootstrapGo = new GameObject("GameBootstrap");
            EnemyRegistry enemyRegistry = bootstrapGo.AddComponent<EnemyRegistry>();
            PoolManager poolManager = bootstrapGo.AddComponent<PoolManager>();
            RunCurrencyWallet currencyWallet = bootstrapGo.AddComponent<RunCurrencyWallet>();
            RunSession runSession = bootstrapGo.AddComponent<RunSession>();
            GameBootstrap gameBootstrap = bootstrapGo.AddComponent<GameBootstrap>();

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            ConfigurePixelPerfectCamera(cameraGo);
            CameraFollow cameraFollow = cameraGo.AddComponent<CameraFollow>();

            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            SpriteRenderer playerSr = playerGo.AddComponent<SpriteRenderer>();
            playerSr.sprite = playerSprite;
            Rigidbody2D playerRb = playerGo.AddComponent<Rigidbody2D>();
            playerRb.gravityScale = 0f;
            playerRb.freezeRotation = true;
            CircleCollider2D playerCol = playerGo.AddComponent<CircleCollider2D>();
            playerCol.isTrigger = true;
            playerCol.radius = 0.5f;
            HealthComponent playerHealth = playerGo.AddComponent<HealthComponent>();
            PlayerStats playerStats = playerGo.AddComponent<PlayerStats>();
            PlayerExperience playerExperience = playerGo.AddComponent<PlayerExperience>();
            PlayerInputController playerInputController = playerGo.AddComponent<PlayerInputController>();
            PlayerMovement playerMovement = playerGo.AddComponent<PlayerMovement>();
            NearestEnemyTargeter targeter = playerGo.AddComponent<NearestEnemyTargeter>();
            AutoAttack autoAttack = playerGo.AddComponent<AutoAttack>();
            PlayerBootstrap playerBootstrap = playerGo.AddComponent<PlayerBootstrap>();

            var spawnerGo = new GameObject("EnemySpawner");
            EnemySpawner enemySpawner = spawnerGo.AddComponent<EnemySpawner>();

            var canvasGo = new GameObject("Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject healthBarBgGo = CreateUIImage("HealthBarBackground", canvasGo.transform, new Color(0f, 0f, 0f, 0.4f));
            SetAnchoredRect(healthBarBgGo, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(300f, 30f));
            GameObject healthBarFillGo = CreateUIImage("HealthBarFill", healthBarBgGo.transform, new Color(0.85f, 0.1f, 0.1f, 1f));
            Image healthFillImage = healthBarFillGo.GetComponent<Image>();
            healthFillImage.type = Image.Type.Filled;
            healthFillImage.fillMethod = Image.FillMethod.Horizontal;
            StretchFull(healthBarFillGo);
            HealthBarUI healthBarUi = healthBarBgGo.AddComponent<HealthBarUI>();

            GameObject expBarBgGo = CreateUIImage("ExpBarBackground", canvasGo.transform, new Color(0f, 0f, 0f, 0.4f));
            SetAnchoredRect(expBarBgGo, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -60f), new Vector2(300f, 20f));
            GameObject expBarFillGo = CreateUIImage("ExpBarFill", expBarBgGo.transform, new Color(0.2f, 0.6f, 1f, 1f));
            Image expFillImage = expBarFillGo.GetComponent<Image>();
            expFillImage.type = Image.Type.Filled;
            expFillImage.fillMethod = Image.FillMethod.Horizontal;
            StretchFull(expBarFillGo);
            GameObject levelTextGo = CreateUIText("LevelText", canvasGo.transform, "Lv. 1");
            SetAnchoredRect(levelTextGo, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(340f, -60f), new Vector2(80f, 20f));
            ExpBarUI expBarUi = expBarBgGo.AddComponent<ExpBarUI>();

            GameObject currencyTextGo = CreateUIText("CurrencyText", canvasGo.transform, "0");
            SetAnchoredRect(currencyTextGo, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-120f, -20f), new Vector2(100f, 30f));
            CurrencyCounterUI currencyCounterUi = currencyTextGo.AddComponent<CurrencyCounterUI>();

            GameObject joystickBgGo = CreateUIImage("JoystickBackground", canvasGo.transform, new Color(1f, 1f, 1f, 0.25f));
            SetAnchoredRect(joystickBgGo, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(150f, 150f), new Vector2(200f, 200f));
            GameObject joystickHandleGo = CreateUIImage("JoystickHandle", joystickBgGo.transform, new Color(1f, 1f, 1f, 0.6f));
            SetAnchoredRect(joystickHandleGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f));
            OnScreenJoystickUI joystickUi = joystickBgGo.AddComponent<OnScreenJoystickUI>();

            GameObject levelUpPanelGo = CreateUIImage("LevelUpPanel", canvasGo.transform, new Color(0f, 0f, 0f, 0.75f));
            SetAnchoredRect(levelUpPanelGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 500f));
            // Must start ACTIVE: LevelUpUIController lives on this object and needs
            // its OnEnable to subscribe to level-up events; visibility is driven by
            // a CanvasGroup (alpha 0 in Awake), not SetActive.
            levelUpPanelGo.SetActive(true);

            var choiceButtons = new Button[3];
            var choiceNameTexts = new Text[3];
            var choiceDescriptionTexts = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject buttonGo = CreateUIImage($"Choice{i}", levelUpPanelGo.transform, new Color(0.2f, 0.2f, 0.2f, 1f));
                SetAnchoredRect(buttonGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f + (i * 300f), 0f), new Vector2(280f, 400f));
                Button button = buttonGo.AddComponent<Button>();
                button.targetGraphic = buttonGo.GetComponent<Image>();

                GameObject nameTextGo = CreateUIText($"Choice{i}Name", buttonGo.transform, "Skill Name");
                SetAnchoredRect(nameTextGo, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -60f), new Vector2(-10f, 60f));
                Text nameText = nameTextGo.GetComponent<Text>();

                GameObject descTextGo = CreateUIText($"Choice{i}Desc", buttonGo.transform, "Description");
                SetAnchoredRect(descTextGo, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 10f), new Vector2(-10f, 300f));
                Text descText = descTextGo.GetComponent<Text>();

                choiceButtons[i] = button;
                choiceNameTexts[i] = nameText;
                choiceDescriptionTexts[i] = descText;
            }

            LevelUpUIController levelUpController = levelUpPanelGo.AddComponent<LevelUpUIController>();

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            InputSystemUIInputModule uiInputModule = eventSystemGo.AddComponent<InputSystemUIInputModule>();
            uiInputModule.AssignDefaultActions();

            var poolEntries = new[]
            {
                new PoolPrewarmEntry { poolId = PoolIds.WorkerBee, prefab = workerPrefab, prewarmCount = 20, maxSize = 80 },
                new PoolPrewarmEntry { poolId = PoolIds.WarriorBee, prefab = warriorPrefab, prewarmCount = 10, maxSize = 40 },
                new PoolPrewarmEntry { poolId = PoolIds.ExpPickup, prefab = expPickupPrefab, prewarmCount = 30, maxSize = 150 },
                new PoolPrewarmEntry { poolId = PoolIds.CurrencyPickup, prefab = currencyPickupPrefab, prewarmCount = 10, maxSize = 50 },
                new PoolPrewarmEntry { poolId = PoolIds.Projectile, prefab = projectilePrefab, prewarmCount = 20, maxSize = 100 }
            };

            var bootstrapSerialized = new SerializedObject(gameBootstrap);
            SerializedProperty poolsProp = bootstrapSerialized.FindProperty("_pools");
            poolsProp.arraySize = poolEntries.Length;
            for (int i = 0; i < poolEntries.Length; i++)
            {
                SerializedProperty entryProp = poolsProp.GetArrayElementAtIndex(i);
                entryProp.FindPropertyRelative("poolId").intValue = poolEntries[i].poolId;
                entryProp.FindPropertyRelative("prefab").objectReferenceValue = poolEntries[i].prefab;
                entryProp.FindPropertyRelative("prewarmCount").intValue = poolEntries[i].prewarmCount;
                entryProp.FindPropertyRelative("maxSize").intValue = poolEntries[i].maxSize;
            }
            bootstrapSerialized.FindProperty("_poolParent").objectReferenceValue = bootstrapGo.transform;
            bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

            var runSessionSerialized = new SerializedObject(runSession);
            runSessionSerialized.FindProperty("_currencyWallet").objectReferenceValue = currencyWallet;
            runSessionSerialized.FindProperty("_metaProgressionStore").objectReferenceValue = metaStore;
            runSessionSerialized.ApplyModifiedPropertiesWithoutUndo();

            var cameraFollowSerialized = new SerializedObject(cameraFollow);
            cameraFollowSerialized.FindProperty("_target").objectReferenceValue = playerGo.transform;
            cameraFollowSerialized.ApplyModifiedPropertiesWithoutUndo();

            var playerExpSerialized = new SerializedObject(playerExperience);
            playerExpSerialized.FindProperty("_levelCurve").objectReferenceValue = levelCurve;
            playerExpSerialized.ApplyModifiedPropertiesWithoutUndo();

            var pic = new SerializedObject(playerInputController);
            pic.FindProperty("_actionsAsset").objectReferenceValue = inputActions;
            pic.FindProperty("_joystickUi").objectReferenceValue = joystickUi;
            pic.FindProperty("_worldCamera").objectReferenceValue = camera;
            pic.ApplyModifiedPropertiesWithoutUndo();

            var pb = new SerializedObject(playerBootstrap);
            pb.FindProperty("_movement").objectReferenceValue = playerMovement;
            pb.FindProperty("_inputController").objectReferenceValue = playerInputController;
            pb.FindProperty("_stats").objectReferenceValue = playerStats;
            pb.ApplyModifiedPropertiesWithoutUndo();

            var autoAttackSerialized = new SerializedObject(autoAttack);
            autoAttackSerialized.FindProperty("_targeter").objectReferenceValue = targeter;
            autoAttackSerialized.FindProperty("_stats").objectReferenceValue = playerStats;
            autoAttackSerialized.FindProperty("_projectilePoolId").intValue = PoolIds.Projectile;
            autoAttackSerialized.ApplyModifiedPropertiesWithoutUndo();

            var spawnerSerialized = new SerializedObject(enemySpawner);
            spawnerSerialized.FindProperty("_config").objectReferenceValue = waveConfig;
            spawnerSerialized.FindProperty("_player").objectReferenceValue = playerGo.transform;
            spawnerSerialized.FindProperty("_playerExperience").objectReferenceValue = playerExperience;
            spawnerSerialized.FindProperty("_currencyWallet").objectReferenceValue = currencyWallet;
            spawnerSerialized.ApplyModifiedPropertiesWithoutUndo();

            var healthBarSerialized = new SerializedObject(healthBarUi);
            healthBarSerialized.FindProperty("_fillImage").objectReferenceValue = healthFillImage;
            healthBarSerialized.FindProperty("_playerHealth").objectReferenceValue = playerHealth;
            healthBarSerialized.ApplyModifiedPropertiesWithoutUndo();

            var expBarSerialized = new SerializedObject(expBarUi);
            expBarSerialized.FindProperty("_fillImage").objectReferenceValue = expFillImage;
            expBarSerialized.FindProperty("_levelText").objectReferenceValue = levelTextGo.GetComponent<Text>();
            expBarSerialized.FindProperty("_playerExperience").objectReferenceValue = playerExperience;
            expBarSerialized.ApplyModifiedPropertiesWithoutUndo();

            var currencySerialized = new SerializedObject(currencyCounterUi);
            currencySerialized.FindProperty("_currencyText").objectReferenceValue = currencyTextGo.GetComponent<Text>();
            currencySerialized.FindProperty("_wallet").objectReferenceValue = currencyWallet;
            currencySerialized.ApplyModifiedPropertiesWithoutUndo();

            var levelUpSerialized = new SerializedObject(levelUpController);
            levelUpSerialized.FindProperty("_database").objectReferenceValue = skillDatabase;
            levelUpSerialized.FindProperty("_playerExperience").objectReferenceValue = playerExperience;
            levelUpSerialized.FindProperty("_playerStats").objectReferenceValue = playerStats;
            levelUpSerialized.FindProperty("_playerHealth").objectReferenceValue = playerHealth;
            levelUpSerialized.FindProperty("_panelRoot").objectReferenceValue = levelUpPanelGo;

            SerializedProperty buttonsProp = levelUpSerialized.FindProperty("_choiceButtons");
            SerializedProperty namesProp = levelUpSerialized.FindProperty("_choiceNameTexts");
            SerializedProperty descsProp = levelUpSerialized.FindProperty("_choiceDescriptionTexts");
            buttonsProp.arraySize = choiceButtons.Length;
            namesProp.arraySize = choiceNameTexts.Length;
            descsProp.arraySize = choiceDescriptionTexts.Length;
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = choiceButtons[i];
                namesProp.GetArrayElementAtIndex(i).objectReferenceValue = choiceNameTexts[i];
                descsProp.GetArrayElementAtIndex(i).objectReferenceValue = choiceDescriptionTexts[i];
            }
            levelUpSerialized.ApplyModifiedPropertiesWithoutUndo();

            const string scenePath = "Assets/Scenes/Beehive.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static GameObject CreateUIImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private static GameObject CreateUIText(string name, Transform parent, string text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text textComponent = go.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.fontSize = 24;
            return go;
        }

        private static void SetAnchoredRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void StretchFull(GameObject go)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
