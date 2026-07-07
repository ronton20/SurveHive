using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Enemies;
using SurveHive.Health;
using SurveHive.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// PLAN.md Phase 4 — enemy variety. 4A: the Spitter Bee, a ranged rank
    /// that kites to a firing band and shoots pooled stingers. 4B: the Bomber
    /// Bee, a fast rusher that fuses and explodes in an AoE (also on death).
    /// 4C: the Swarmling, a weak/fast rank spawning in wobbling packs.
    /// Additive and idempotent over the existing scene/assets — safe to re-run.
    /// </summary>
    public static class EnemyVarietyBuilder
    {
        private const string ScenePath = "Assets/Scenes/Beehive.unity";
        private const string SpitterStatsPath = "Assets/Data/Enemies/SpitterBee.asset";
        private const string SpitterPrefabPath = "Assets/Prefabs/Enemies/SpitterBee.prefab";
        private const string BomberStatsPath = "Assets/Data/Enemies/BomberBee.asset";
        private const string BomberPrefabPath = "Assets/Prefabs/Enemies/BomberBee.prefab";
        private const string SwarmlingStatsPath = "Assets/Data/Enemies/SwarmlingBee.asset";
        private const string SwarmlingPrefabPath = "Assets/Prefabs/Enemies/SwarmlingBee.prefab";
        private const string WaveConfigPath = "Assets/Data/Waves/BeehiveWaveConfig.asset";

        // Venom green — reads apart from the sandy Worker / red Warrior /
        // purple Guard tints on the shared bee rig.
        private static readonly Color SpitterTint = new Color(0.55f, 0.95f, 0.45f);
        // Hot orange, matching the fuse/blast it dies in.
        private static readonly Color BomberTint = new Color(1f, 0.5f, 0.2f);
        // Pale blue-violet, cool against every warm rank.
        private static readonly Color SwarmlingTint = new Color(0.7f, 0.78f, 1f);

        [MenuItem("SurveHive/Apply Enemy Variety (Phase 4)")]
        public static void Apply()
        {
            Material flashMaterial = Phase1LookAndFeelBuilder.EnsureFlashMaterial();

            // 4A — ranged spitter.
            GameObject spitterPrefab = EnsureSpitterPrefab(flashMaterial);
            EnemyStatsSO spitterStats = EnsureSpitterStats(spitterPrefab);
            EnsureWaveEntry(spitterStats, spawnWeight: 0.3f, unlockSeconds: 90f, packSize: 1);

            // 4B — suicide bomber (+ its blast VFX).
            GameObject bomberPrefab = EnsureBomberPrefab(flashMaterial);
            EnemyStatsSO bomberStats = EnsureBomberStats(bomberPrefab);
            EnsureWaveEntry(bomberStats, spawnWeight: 0.25f, unlockSeconds: 150f, packSize: 1);
            // 0.7 ≈ a 4.5u visual diameter, matching the 2.2u damage radius —
            // the pack sheet at 1.1 read three times bigger than the hitbox.
            Phase2CombatDepthBuilder.EnsurePackVfxWrapper(
                "BomberBlast", "Explosion_normal", PoolIds.BomberBlastVfx, 0.7f, false, Color.clear);

            // 4C — swarm rank.
            GameObject swarmlingPrefab = EnsureSwarmlingPrefab(flashMaterial);
            EnemyStatsSO swarmlingStats = EnsureSwarmlingStats(swarmlingPrefab);
            EnsureWaveEntry(swarmlingStats, spawnWeight: 0.3f, unlockSeconds: 60f, packSize: 6);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ApplySceneChanges();

            Debug.Log("SurveHive enemy variety (Phase 4) build complete.");
        }

        // ------------------------------------------------------------------
        // Prefabs: shared trash-rank base + one behavior component each.
        // ------------------------------------------------------------------
        private static GameObject EnsureTrashEnemyPrefab(
            string prefabPath, string name, Material flashMaterial, float colliderRadius)
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
                col.radius = colliderRadius;

                go.AddComponent<HealthComponent>();
                go.AddComponent<DamageOnContact>();
                go.AddComponent<EnemyController>();
                go.AddComponent<EnemyLoot>();
                Phase1LookAndFeelBuilder.BuildQueensGuardHealthBar(go.transform, go.GetComponent<HealthComponent>());

                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);
            }

            // Rig + feel + status wiring, shared with the other bee ranks.
            Phase1LookAndFeelBuilder.RebuildEnemyPrefabVisuals(prefabPath, flashMaterial);
            Phase2CombatDepthBuilder.EnsureEnemyStatusReceiver(prefabPath);

            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        private static GameObject EnsureSpitterPrefab(Material flashMaterial)
        {
            EnsureTrashEnemyPrefab(SpitterPrefabPath, "SpitterBee", flashMaterial, 0.5f);

            GameObject contents = PrefabUtility.LoadPrefabContents(SpitterPrefabPath);
            try
            {
                Transform body = contents.transform.Find("Body");

                if (!contents.TryGetComponent(out RangedAttack ranged))
                {
                    ranged = contents.AddComponent<RangedAttack>();
                }

                var serialized = new SerializedObject(ranged);
                serialized.FindProperty("_enemyController").objectReferenceValue = contents.GetComponent<EnemyController>();
                serialized.FindProperty("_health").objectReferenceValue = contents.GetComponent<HealthComponent>();
                serialized.FindProperty("_renderer").objectReferenceValue =
                    body != null ? body.GetComponent<SpriteRenderer>() : null;
                serialized.FindProperty("_projectilePoolId").intValue = PoolIds.EnemyStinger;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, SpitterPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(SpitterPrefabPath);
        }

        private static GameObject EnsureBomberPrefab(Material flashMaterial)
        {
            EnsureTrashEnemyPrefab(BomberPrefabPath, "BomberBee", flashMaterial, 0.5f);

            GameObject contents = PrefabUtility.LoadPrefabContents(BomberPrefabPath);
            try
            {
                Transform body = contents.transform.Find("Body");

                if (!contents.TryGetComponent(out BomberAttack bomber))
                {
                    bomber = contents.AddComponent<BomberAttack>();
                }

                var serialized = new SerializedObject(bomber);
                serialized.FindProperty("_enemyController").objectReferenceValue = contents.GetComponent<EnemyController>();
                serialized.FindProperty("_health").objectReferenceValue = contents.GetComponent<HealthComponent>();
                serialized.FindProperty("_renderer").objectReferenceValue =
                    body != null ? body.GetComponent<SpriteRenderer>() : null;
                serialized.FindProperty("_blastVfxPoolId").intValue = PoolIds.BomberBlastVfx;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, BomberPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(BomberPrefabPath);
        }

        private static GameObject EnsureSwarmlingPrefab(Material flashMaterial)
        {
            EnsureTrashEnemyPrefab(SwarmlingPrefabPath, "SwarmlingBee", flashMaterial, 0.4f);

            GameObject contents = PrefabUtility.LoadPrefabContents(SwarmlingPrefabPath);
            try
            {
                if (!contents.TryGetComponent(out SwarmMovement swarm))
                {
                    swarm = contents.AddComponent<SwarmMovement>();
                }

                var serialized = new SerializedObject(swarm);
                serialized.FindProperty("_enemyController").objectReferenceValue = contents.GetComponent<EnemyController>();
                serialized.FindProperty("_health").objectReferenceValue = contents.GetComponent<HealthComponent>();
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, SwarmlingPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(SwarmlingPrefabPath);
        }

        // ------------------------------------------------------------------
        // Stats assets.
        // ------------------------------------------------------------------
        private static EnemyStatsSO EnsureSpitterStats(GameObject prefab)
        {
            EnemyStatsSO stats = EnsureStatsAsset(SpitterStatsPath);
            var serialized = new SerializedObject(stats);
            serialized.FindProperty("_displayName").stringValue = "Spitter Bee";
            serialized.FindProperty("_rank").intValue = 1;
            serialized.FindProperty("_maxHealth").floatValue = 30f;
            // The PLAN 3B/4A interleave: a magic-shielded ranged bee — physical
            // builds pop it on touch, magic builds must chew the shield first.
            serialized.FindProperty("_magicShield").floatValue = 15f;
            serialized.FindProperty("_moveSpeed").floatValue = 2.3f;
            // Weak on touch — its threat is the stinger shot (fraction 1 of this).
            serialized.FindProperty("_contactDamage").floatValue = 6f;
            serialized.FindProperty("_contactDamageInterval").floatValue = 1f;
            serialized.FindProperty("_expReward").floatValue = 8f;
            serialized.FindProperty("_currencyDropChance").floatValue = 0.25f;
            serialized.FindProperty("_currencyDropMin").intValue = 1;
            serialized.FindProperty("_currencyDropMax").intValue = 2;
            serialized.FindProperty("_itemDropChance").floatValue = 0.02f;
            serialized.FindProperty("_spriteTint").colorValue = SpitterTint;
            serialized.FindProperty("_scale").floatValue = 1f;
            serialized.FindProperty("_knockbackResistance").floatValue = 1f;
            serialized.FindProperty("_prefab").objectReferenceValue = prefab;
            serialized.FindProperty("_poolId").intValue = PoolIds.SpitterBee;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stats);
            return stats;
        }

        private static EnemyStatsSO EnsureBomberStats(GameObject prefab)
        {
            EnemyStatsSO stats = EnsureStatsAsset(BomberStatsPath);
            var serialized = new SerializedObject(stats);
            serialized.FindProperty("_displayName").stringValue = "Bomber Bee";
            serialized.FindProperty("_rank").intValue = 1;
            serialized.FindProperty("_maxHealth").floatValue = 25f;
            serialized.FindProperty("_moveSpeed").floatValue = 3.3f;
            // Blast = 2.5× this (see BomberAttack); touch alone stays mild.
            serialized.FindProperty("_contactDamage").floatValue = 6f;
            serialized.FindProperty("_contactDamageInterval").floatValue = 1f;
            serialized.FindProperty("_expReward").floatValue = 10f;
            serialized.FindProperty("_currencyDropChance").floatValue = 0.25f;
            serialized.FindProperty("_currencyDropMin").intValue = 1;
            serialized.FindProperty("_currencyDropMax").intValue = 2;
            serialized.FindProperty("_itemDropChance").floatValue = 0.02f;
            serialized.FindProperty("_spriteTint").colorValue = BomberTint;
            serialized.FindProperty("_scale").floatValue = 0.95f;
            // Easy to shove away — knockback is the melee counter-play.
            serialized.FindProperty("_knockbackResistance").floatValue = 0.7f;
            serialized.FindProperty("_prefab").objectReferenceValue = prefab;
            serialized.FindProperty("_poolId").intValue = PoolIds.BomberBee;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stats);
            return stats;
        }

        private static EnemyStatsSO EnsureSwarmlingStats(GameObject prefab)
        {
            EnemyStatsSO stats = EnsureStatsAsset(SwarmlingStatsPath);
            var serialized = new SerializedObject(stats);
            serialized.FindProperty("_displayName").stringValue = "Swarmling";
            serialized.FindProperty("_rank").intValue = 0;
            serialized.FindProperty("_maxHealth").floatValue = 8f;
            serialized.FindProperty("_moveSpeed").floatValue = 3.1f;
            serialized.FindProperty("_contactDamage").floatValue = 3f;
            serialized.FindProperty("_contactDamageInterval").floatValue = 0.8f;
            serialized.FindProperty("_expReward").floatValue = 2f;
            serialized.FindProperty("_currencyDropChance").floatValue = 0.08f;
            serialized.FindProperty("_currencyDropMin").intValue = 1;
            serialized.FindProperty("_currencyDropMax").intValue = 1;
            serialized.FindProperty("_spriteTint").colorValue = SwarmlingTint;
            serialized.FindProperty("_scale").floatValue = 0.6f;
            serialized.FindProperty("_knockbackResistance").floatValue = 0.6f;
            serialized.FindProperty("_prefab").objectReferenceValue = prefab;
            serialized.FindProperty("_poolId").intValue = PoolIds.SwarmlingBee;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stats);
            return stats;
        }

        private static EnemyStatsSO EnsureStatsAsset(string path)
        {
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(path);
            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<EnemyStatsSO>();
                AssetDatabase.CreateAsset(stats, path);
            }

            return stats;
        }

        // ------------------------------------------------------------------
        // Wave table + scene pools.
        // ------------------------------------------------------------------
        private static void EnsureWaveEntry(
            EnemyStatsSO enemyStats, float spawnWeight, float unlockSeconds, int packSize)
        {
            var config = AssetDatabase.LoadAssetAtPath<WaveSpawnerConfigSO>(WaveConfigPath);
            if (config == null)
            {
                Debug.LogError($"EnemyVariety: wave config missing at {WaveConfigPath}");
                return;
            }

            var serialized = new SerializedObject(config);
            SerializedProperty entries = serialized.FindProperty("_entries");
            int index = -1;
            for (int i = 0; i < entries.arraySize; i++)
            {
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("enemyStats").objectReferenceValue == enemyStats)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                index = entries.arraySize;
                entries.arraySize = index + 1;
            }

            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("enemyStats").objectReferenceValue = enemyStats;
            entry.FindPropertyRelative("spawnWeight").floatValue = spawnWeight;
            entry.FindPropertyRelative("unlockTimeSeconds").floatValue = unlockSeconds;
            entry.FindPropertyRelative("packSize").intValue = packSize;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void ApplySceneChanges()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject bootstrapGo = GameObject.Find("GameBootstrap");
            var bootstrapSerialized = new SerializedObject(bootstrapGo.GetComponent<GameBootstrap>());
            SerializedProperty pools = bootstrapSerialized.FindProperty("_pools");
            Phase3RunStructureBuilder.EnsurePoolEntry(pools, PoolIds.SpitterBee,
                AssetDatabase.LoadAssetAtPath<GameObject>(SpitterPrefabPath), 4, 12);
            Phase3RunStructureBuilder.EnsurePoolEntry(pools, PoolIds.BomberBee,
                AssetDatabase.LoadAssetAtPath<GameObject>(BomberPrefabPath), 4, 12);
            Phase3RunStructureBuilder.EnsurePoolEntry(pools, PoolIds.SwarmlingBee,
                AssetDatabase.LoadAssetAtPath<GameObject>(SwarmlingPrefabPath), 12, 32);
            Phase3RunStructureBuilder.EnsurePoolEntry(pools, PoolIds.BomberBlastVfx,
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/BomberBlast.prefab"), 2, 6);
            bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }
}
