using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Enemies;
using SurveHive.Health;
using SurveHive.Spawning;
using SurveHive.Stage;
using SurveHive.UI;
using SurveHive.View;
using TMPro;
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
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";
        // Queen Bee body: royal-tinted BossPack1 dragon until custom art lands.
        private const string QueenLibraryPath = "Assets/ThirdParty/PixelFantasy/PixelMonsters/BossPack1/Dragon/BlueDragon.asset";
        private const string RoyalGuardStatsPath = "Assets/Data/Enemies/QueensRoyalGuard.asset";
        private const string QueenStatsPath = "Assets/Data/Enemies/QueenBee.asset";
        private const string RoyalGuardPrefabPath = "Assets/Prefabs/Enemies/QueensRoyalGuard.prefab";
        private const string QueenPrefabPath = "Assets/Prefabs/Enemies/QueenBee.prefab";
        private const string EnemyStingerPrefabPath = "Assets/Prefabs/Projectiles/EnemyStinger.prefab";

        private static readonly Color HoneyGold = new Color(1f, 0.765f, 0.043f);
        private static readonly Color DeepBrown = new Color(0.227f, 0.141f, 0.086f);
        private static readonly Color Wax = new Color(0.91f, 0.847f, 0.627f);
        private static readonly Color DangerRed = new Color(0.851f, 0.282f, 0.231f);
        private static readonly Color RoyalPurple = new Color(0.482f, 0.176f, 0.545f);

        [MenuItem("SurveHive/Apply Phase 3 Run Structure")]
        public static void Apply()
        {
            // 3B: bosses.
            Material flashMaterial = Phase1LookAndFeelBuilder.EnsureFlashMaterial();
            GameObject royalGuardPrefab = EnsureBossPrefab(
                RoyalGuardPrefabPath, "QueensRoyalGuard", flashMaterial, null, isQueen: false);
            GameObject queenPrefab = EnsureBossPrefab(
                QueenPrefabPath, "QueenBee", flashMaterial, QueenLibraryPath, isQueen: true);
            EnemyStatsSO royalGuardStats = EnsureRoyalGuardStats(royalGuardPrefab);
            EnemyStatsSO queenStats = EnsureQueenStats(queenPrefab);
            EnsureEnemyStingerPrefab();

            // 3A: stage timeline (boss events now point at the real boss ranks).
            StageConfigSO stageConfig = EnsureStageConfig(royalGuardStats, queenStats);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ApplySceneChanges(stageConfig);

            Debug.Log("SurveHive Phase 3 run structure build complete.");
        }

        // ------------------------------------------------------------------
        // 3B: boss prefabs, stats and the enemy stinger projectile.
        // ------------------------------------------------------------------
        private static GameObject EnsureBossPrefab(
            string prefabPath, string name, Material flashMaterial, string libraryPath, bool isQueen)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                var go = new GameObject(name);
                go.tag = "Enemy";

                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;

                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.6f;

                go.AddComponent<HealthComponent>();
                go.AddComponent<DamageOnContact>();
                go.AddComponent<EnemyController>();
                go.AddComponent<EnemyLoot>();
                Phase1LookAndFeelBuilder.BuildQueensGuardHealthBar(go.transform, go.GetComponent<HealthComponent>());

                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);
            }

            // Rig + feel + status wiring, shared with the trash ranks.
            if (libraryPath != null)
            {
                Phase1LookAndFeelBuilder.RebuildEnemyPrefabVisuals(prefabPath, flashMaterial, libraryPath);
            }
            else
            {
                Phase1LookAndFeelBuilder.RebuildEnemyPrefabVisuals(prefabPath, flashMaterial);
            }

            Phase2CombatDepthBuilder.EnsureEnemyStatusReceiver(prefabPath);

            // Boss behaviors.
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform body = contents.transform.Find("Body");
                var bodyRenderer = body != null ? body.GetComponent<SpriteRenderer>() : null;

                if (!contents.TryGetComponent(out ChargeAttack charge))
                {
                    charge = contents.AddComponent<ChargeAttack>();
                }

                var chargeSerialized = new SerializedObject(charge);
                chargeSerialized.FindProperty("_enemyController").objectReferenceValue = contents.GetComponent<EnemyController>();
                chargeSerialized.FindProperty("_health").objectReferenceValue = contents.GetComponent<HealthComponent>();
                chargeSerialized.FindProperty("_renderer").objectReferenceValue = bodyRenderer;
                // Queen charges only when her pattern brain says so.
                chargeSerialized.FindProperty("_autoRun").boolValue = !isQueen;
                chargeSerialized.FindProperty("_intervalSeconds").floatValue = isQueen ? 6f : 5f;
                chargeSerialized.FindProperty("_chargeSpeedMultiplier").floatValue = isQueen ? 6f : 5f;
                chargeSerialized.ApplyModifiedPropertiesWithoutUndo();

                if (isQueen)
                {
                    if (!contents.TryGetComponent(out QueenBossController queenController))
                    {
                        queenController = contents.AddComponent<QueenBossController>();
                    }

                    var queenSerialized = new SerializedObject(queenController);
                    queenSerialized.FindProperty("_enemyController").objectReferenceValue = contents.GetComponent<EnemyController>();
                    queenSerialized.FindProperty("_health").objectReferenceValue = contents.GetComponent<HealthComponent>();
                    queenSerialized.FindProperty("_chargeAttack").objectReferenceValue = charge;
                    queenSerialized.FindProperty("_renderer").objectReferenceValue = bodyRenderer;
                    queenSerialized.FindProperty("_stingerPoolId").intValue = PoolIds.EnemyStinger;
                    queenSerialized.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        private static EnemyStatsSO EnsureRoyalGuardStats(GameObject prefab)
        {
            return EnsureEnemyStats(RoyalGuardStatsPath, "Queen's Royal Guard", rank: 3,
                maxHealth: 900f, moveSpeed: 2.6f, contactDamage: 18f, contactInterval: 1f,
                expReward: 150f, dropChance: 1f, dropMin: 8, dropMax: 15,
                tint: new Color(1f, 0.45f, 0.85f), scale: 1.7f, knockbackResistance: 8f,
                deathHitStop: 0.08f, prefab: prefab, poolId: PoolIds.QueensRoyalGuard);
        }

        private static EnemyStatsSO EnsureQueenStats(GameObject prefab)
        {
            return EnsureEnemyStats(QueenStatsPath, "Queen Bee", rank: 4,
                maxHealth: 3500f, moveSpeed: 1.6f, contactDamage: 25f, contactInterval: 0.8f,
                expReward: 500f, dropChance: 1f, dropMin: 40, dropMax: 60,
                tint: new Color(1f, 0.6f, 1f), scale: 1.4f, knockbackResistance: 25f,
                deathHitStop: 0.12f, prefab: prefab, poolId: PoolIds.QueenBee);
        }

        private static EnemyStatsSO EnsureEnemyStats(
            string path, string displayName, int rank, float maxHealth, float moveSpeed,
            float contactDamage, float contactInterval, float expReward, float dropChance,
            int dropMin, int dropMax, Color tint, float scale, float knockbackResistance,
            float deathHitStop, GameObject prefab, int poolId)
        {
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(path);
            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<EnemyStatsSO>();
                AssetDatabase.CreateAsset(stats, path);
            }

            var serialized = new SerializedObject(stats);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_rank").intValue = rank;
            serialized.FindProperty("_maxHealth").floatValue = maxHealth;
            serialized.FindProperty("_moveSpeed").floatValue = moveSpeed;
            serialized.FindProperty("_contactDamage").floatValue = contactDamage;
            serialized.FindProperty("_contactDamageInterval").floatValue = contactInterval;
            serialized.FindProperty("_expReward").floatValue = expReward;
            serialized.FindProperty("_currencyDropChance").floatValue = dropChance;
            serialized.FindProperty("_currencyDropMin").intValue = dropMin;
            serialized.FindProperty("_currencyDropMax").intValue = dropMax;
            serialized.FindProperty("_spriteTint").colorValue = tint;
            serialized.FindProperty("_scale").floatValue = scale;
            serialized.FindProperty("_knockbackResistance").floatValue = knockbackResistance;
            serialized.FindProperty("_deathHitStopSeconds").floatValue = deathHitStop;
            serialized.FindProperty("_prefab").objectReferenceValue = prefab;
            serialized.FindProperty("_poolId").intValue = poolId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stats);
            return stats;
        }

        private static void EnsureEnemyStingerPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EnemyStingerPrefabPath) == null)
            {
                var go = new GameObject("EnemyStinger");
                go.AddComponent<SpriteRenderer>();
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                go.AddComponent<EnemyProjectile>();
                PrefabUtility.SaveAsPrefabAsset(go, EnemyStingerPrefabPath);
                Object.DestroyImmediate(go);
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(EnemyStingerPrefabPath);
            try
            {
                var renderer = contents.GetComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Stinger.png");
                // Hostile recolor so player and enemy shots never read the same.
                renderer.color = new Color(1f, 0.4f, 0.75f);
                renderer.sortingOrder = 1;

                var col = contents.GetComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.22f;

                var serialized = new SerializedObject(contents.GetComponent<EnemyProjectile>());
                serialized.FindProperty("_poolId").intValue = PoolIds.EnemyStinger;
                serialized.FindProperty("_targetTag").stringValue = "Player";
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, EnemyStingerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // ------------------------------------------------------------------
        // Stage config: 10-minute run, spawn rate ramping 1x -> 3.5x, events
        // at 25% (ring wave), 50% (miniboss), 75% (flood wave), 100% (Queen).
        // ------------------------------------------------------------------
        private static StageConfigSO EnsureStageConfig(EnemyStatsSO royalGuardStats, EnemyStatsSO queenStats)
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
            SetEvent(events, 1, StageEventType.Miniboss, 0.5f, royalGuardStats, 1);
            SetEvent(events, 2, StageEventType.StrongWaveFlood, 0.75f, queensGuard, 20);
            SetEvent(events, 3, StageEventType.FinalBoss, 1f, queenStats, 1);
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

            // --- 3B: pools, boss HUD, banner, victory panel, boss spawner ---
            GameObject bootstrapGo = GameObject.Find("GameBootstrap");
            var bootstrapSerialized = new SerializedObject(bootstrapGo.GetComponent<GameBootstrap>());
            SerializedProperty pools = bootstrapSerialized.FindProperty("_pools");
            EnsurePoolEntry(pools, PoolIds.QueensRoyalGuard,
                AssetDatabase.LoadAssetAtPath<GameObject>(RoyalGuardPrefabPath), 1, 2);
            EnsurePoolEntry(pools, PoolIds.QueenBee,
                AssetDatabase.LoadAssetAtPath<GameObject>(QueenPrefabPath), 1, 2);
            EnsurePoolEntry(pools, PoolIds.EnemyStinger,
                AssetDatabase.LoadAssetAtPath<GameObject>(EnemyStingerPrefabPath), 16, 48);
            bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            BossHealthBarUI bossBar = BuildBossHealthBar(font);
            BossBannerUI banner = BuildBossBanner(font);
            GameObject victoryPanel = BuildVictoryPanel(font);

            GameObject cameraGo = GameObject.FindWithTag("MainCamera");
            GameObject bossDirectorGo = GameObject.Find("StageDirector");
            if (!bossDirectorGo.TryGetComponent(out BossSpawner bossSpawner))
            {
                bossSpawner = bossDirectorGo.AddComponent<BossSpawner>();
            }

            var bossSpawnerSerialized = new SerializedObject(bossSpawner);
            bossSpawnerSerialized.FindProperty("_director").objectReferenceValue = director;
            bossSpawnerSerialized.FindProperty("_spawner").objectReferenceValue =
                spawnerGo != null ? spawnerGo.GetComponent<EnemySpawner>() : null;
            bossSpawnerSerialized.FindProperty("_bossHealthBar").objectReferenceValue = bossBar;
            bossSpawnerSerialized.FindProperty("_banner").objectReferenceValue = banner;
            bossSpawnerSerialized.FindProperty("_shaker").objectReferenceValue =
                cameraGo != null ? cameraGo.GetComponent<CameraShaker>() : null;
            bossSpawnerSerialized.FindProperty("_summonStats").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<EnemyStatsSO>("Assets/Data/Enemies/WorkerBee.asset");
            bossSpawnerSerialized.FindProperty("_victoryPanel").objectReferenceValue = victoryPanel;
            bossSpawnerSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static BossHealthBarUI BuildBossHealthBar(TMP_FontAsset font)
        {
            Transform canvas = GameObject.Find("Canvas").transform;
            GameObject barGo = EnsureUiChild(canvas, "BossHealthBar");

            var rect = (RectTransform)barGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 26f);
            rect.sizeDelta = new Vector2(540f, 22f);

            Sprite square = LoadUiKitSprite("PixelSquare");

            if (!barGo.TryGetComponent(out Image background))
            {
                background = barGo.AddComponent<Image>();
            }

            background.sprite = square;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 4f;
            background.color = new Color(DeepBrown.r, DeepBrown.g, DeepBrown.b, 0.95f);
            background.raycastTarget = false;

            GameObject fillGo = EnsureUiChild(barGo.transform, "Fill");
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

            fill.sprite = square;
            fill.type = Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = 4f;
            fill.color = RoyalPurple;
            fill.raycastTarget = false;

            GameObject nameGo = EnsureUiChild(barGo.transform, "BossName");
            var nameRect = (RectTransform)nameGo.transform;
            nameRect.anchorMin = new Vector2(0.5f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0f, 4f);
            nameRect.sizeDelta = new Vector2(540f, 26f);

            if (!nameGo.TryGetComponent(out TextMeshProUGUI nameTmp))
            {
                nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            }

            nameTmp.font = font;
            nameTmp.fontSize = 24f;
            nameTmp.color = Wax;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.textWrappingMode = TextWrappingModes.Normal;
            nameTmp.raycastTarget = false;

            if (!barGo.TryGetComponent(out CanvasGroup group))
            {
                group = barGo.AddComponent<CanvasGroup>();
            }

            group.interactable = false;
            group.blocksRaycasts = false;

            if (!barGo.TryGetComponent(out BossHealthBarUI barUi))
            {
                barUi = barGo.AddComponent<BossHealthBarUI>();
            }

            var serialized = new SerializedObject(barUi);
            serialized.FindProperty("_fillImage").objectReferenceValue = fill;
            serialized.FindProperty("_nameText").objectReferenceValue = nameTmp;
            serialized.FindProperty("_canvasGroup").objectReferenceValue = group;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return barUi;
        }

        private static BossBannerUI BuildBossBanner(TMP_FontAsset font)
        {
            Transform canvas = GameObject.Find("Canvas").transform;
            GameObject bannerGo = EnsureUiChild(canvas, "BossBanner");

            var rect = (RectTransform)bannerGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 160f);
            rect.sizeDelta = new Vector2(900f, 90f);

            if (!bannerGo.TryGetComponent(out TextMeshProUGUI tmp))
            {
                tmp = bannerGo.AddComponent<TextMeshProUGUI>();
            }

            tmp.font = font;
            tmp.fontSize = 64f;
            tmp.color = DangerRed;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            tmp.text = "";

            if (!bannerGo.TryGetComponent(out CanvasGroup group))
            {
                group = bannerGo.AddComponent<CanvasGroup>();
            }

            group.interactable = false;
            group.blocksRaycasts = false;

            if (!bannerGo.TryGetComponent(out BossBannerUI banner))
            {
                banner = bannerGo.AddComponent<BossBannerUI>();
            }

            var serialized = new SerializedObject(banner);
            serialized.FindProperty("_text").objectReferenceValue = tmp;
            serialized.FindProperty("_canvasGroup").objectReferenceValue = group;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return banner;
        }

        // Victory overlay (3B interim; 3C upgrades it into the full results
        // screen). Starts inactive — BossSpawner activates it on the Queen kill.
        private static GameObject BuildVictoryPanel(TMP_FontAsset font)
        {
            Transform canvas = GameObject.Find("Canvas").transform;
            Transform existing = FindChildIncludingInactive(canvas, "VictoryPanel");
            GameObject panelGo = existing != null ? existing.gameObject : null;
            if (panelGo == null)
            {
                panelGo = new GameObject("VictoryPanel", typeof(RectTransform));
                panelGo.transform.SetParent(canvas, false);
            }

            panelGo.SetActive(true);

            var rect = (RectTransform)panelGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(760f, 420f);

            if (!panelGo.TryGetComponent(out Image panelImage))
            {
                panelImage = panelGo.AddComponent<Image>();
            }

            panelImage.sprite = LoadUiKitSprite("PixelPanel");
            panelImage.type = Image.Type.Sliced;
            panelImage.pixelsPerUnitMultiplier = 2f;
            panelImage.color = new Color(DeepBrown.r, DeepBrown.g, DeepBrown.b, 0.97f);
            panelImage.raycastTarget = false;

            GameObject titleGo = EnsureUiChild(panelGo.transform, "VictoryTitle");
            var titleRect = (RectTransform)titleGo.transform;
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -70f);
            titleRect.sizeDelta = new Vector2(700f, 110f);

            if (!titleGo.TryGetComponent(out TextMeshProUGUI titleTmp))
            {
                titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            }

            titleTmp.font = font;
            titleTmp.fontSize = 80f;
            titleTmp.color = HoneyGold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.textWrappingMode = TextWrappingModes.Normal;
            titleTmp.raycastTarget = false;
            titleTmp.text = "HIVE CLEARED!";

            GameObject hintGo = EnsureUiChild(panelGo.transform, "VictoryHint");
            var hintRect = (RectTransform)hintGo.transform;
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 50f);
            hintRect.sizeDelta = new Vector2(700f, 80f);

            if (!hintGo.TryGetComponent(out TextMeshProUGUI hintTmp))
            {
                hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
            }

            hintTmp.font = font;
            hintTmp.fontSize = 30f;
            hintTmp.color = Wax;
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.textWrappingMode = TextWrappingModes.Normal;
            hintTmp.raycastTarget = false;
            hintTmp.text = "The Queen has fallen.\nPress R / tap to fly again";

            panelGo.SetActive(false);
            return panelGo;
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

        private static Transform FindChildIncludingInactive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
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
