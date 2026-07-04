using SurveHive.Core;
using SurveHive.Currency;
using SurveHive.Data;
using SurveHive.Health;
using SurveHive.Player;
using SurveHive.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Phase 4 (PLAN.md §6): meta & menus. Sub-phase 4A — save-file-backed meta
    /// store, the six flat-stat shop upgrade assets, and applying purchased
    /// ranks to the player at run start. 4B (menus) and 4C (pause/settings)
    /// extend this same pass. Additive over Phases 0-3; idempotent.
    /// </summary>
    public static class Phase4MetaAndMenusBuilder
    {
        private const string ScenePath = "Assets/Scenes/Beehive.unity";
        private const string MetaFolder = "Assets/Data/Meta";
        private const string PersistentStorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";

        private static readonly string[] MetaUpgradePaths =
        {
            MetaFolder + "/MaxHealth.asset",
            MetaFolder + "/Damage.asset",
            MetaFolder + "/MoveSpeed.asset",
            MetaFolder + "/AttackSpeed.asset",
            MetaFolder + "/Magnet.asset",
            MetaFolder + "/CurrencyGain.asset",
        };

        [MenuItem("SurveHive/Apply Phase 4 Meta & Menus")]
        public static void Apply()
        {
            // 4A: persistent store + shop upgrade definitions.
            EnsurePersistentStore();
            EnsureMetaUpgradeAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ApplySceneChanges();

            Debug.Log("SurveHive Phase 4 meta & menus build complete.");
        }

        // ------------------------------------------------------------------
        // 4A: assets.
        // ------------------------------------------------------------------
        private static void EnsurePersistentStore()
        {
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            if (store == null)
            {
                store = ScriptableObject.CreateInstance<PersistentMetaProgressionStoreSO>();
                AssetDatabase.CreateAsset(store, PersistentStorePath);
            }
        }

        private static void EnsureMetaUpgradeAssets()
        {
            EnsureFolder(MetaFolder);

            EnsureUpgrade("MaxHealth", "meta_max_health", "Thick Comb Walls",
                "Permanently raises max health.", MetaStatType.MaxHealth,
                maxRank: 10, baseCost: 50, costGrowth: 1.35f, effectPerRank: 10f);
            EnsureUpgrade("Damage", "meta_damage", "Royal Jelly Diet",
                "Permanently raises all damage.", MetaStatType.AttackDamage,
                maxRank: 10, baseCost: 60, costGrowth: 1.4f, effectPerRank: 4f);
            EnsureUpgrade("MoveSpeed", "meta_move_speed", "Stronger Wings",
                "Permanently raises move speed.", MetaStatType.MoveSpeed,
                maxRank: 5, baseCost: 40, costGrowth: 1.5f, effectPerRank: 2f);
            EnsureUpgrade("AttackSpeed", "meta_attack_speed", "Rapid Reflexes",
                "Permanently raises attack speed.", MetaStatType.AttackSpeed,
                maxRank: 8, baseCost: 60, costGrowth: 1.45f, effectPerRank: 3f);
            EnsureUpgrade("Magnet", "meta_magnet", "Nectar Scent",
                "Permanently widens pickup range.", MetaStatType.MagnetRadius,
                maxRank: 5, baseCost: 30, costGrowth: 1.5f, effectPerRank: 8f);
            EnsureUpgrade("CurrencyGain", "meta_currency_gain", "Honey Hoarder",
                "Permanently raises honey gained in runs.", MetaStatType.CurrencyGain,
                maxRank: 10, baseCost: 80, costGrowth: 1.5f, effectPerRank: 5f);
        }

        private static void EnsureUpgrade(
            string assetName, string id, string displayName, string description,
            MetaStatType statType, int maxRank, int baseCost, float costGrowth, float effectPerRank)
        {
            string path = $"{MetaFolder}/{assetName}.asset";
            var upgrade = AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>(path);
            if (upgrade == null)
            {
                upgrade = ScriptableObject.CreateInstance<MetaUpgradeSO>();
                AssetDatabase.CreateAsset(upgrade, path);
            }

            var so = new SerializedObject(upgrade);
            so.FindProperty("_upgradeId").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_statType").enumValueIndex = (int)statType;
            so.FindProperty("_maxRank").intValue = maxRank;
            so.FindProperty("_baseCost").intValue = baseCost;
            so.FindProperty("_costGrowth").floatValue = costGrowth;
            so.FindProperty("_effectPerRank").floatValue = effectPerRank;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(upgrade);
        }

        // ------------------------------------------------------------------
        // 4A: scene wiring.
        // ------------------------------------------------------------------
        private static void ApplySceneChanges()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Reload by path after Refresh/OpenScene — instances held across the
            // asset-database refresh come back destroyed and serialize as null.
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            var upgrades = new MetaUpgradeSO[MetaUpgradePaths.Length];
            for (int i = 0; i < MetaUpgradePaths.Length; i++)
            {
                upgrades[i] = AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>(MetaUpgradePaths[i]);
            }

            GameObject playerGo = GameObject.Find("Player");
            GameObject bootstrapGo = GameObject.Find("GameBootstrap");

            // RunSession: persistent store + player level source for best-run stats.
            var session = bootstrapGo.GetComponent<RunSession>();
            var sessionSerialized = new SerializedObject(session);
            sessionSerialized.FindProperty("_metaProgressionStore").objectReferenceValue = store;
            sessionSerialized.FindProperty("_playerExperience").objectReferenceValue =
                playerGo.GetComponent<PlayerExperience>();
            sessionSerialized.ApplyModifiedPropertiesWithoutUndo();

            // Player: apply purchased meta ranks at run start.
            if (!playerGo.TryGetComponent(out MetaUpgradeApplier applier))
            {
                applier = playerGo.AddComponent<MetaUpgradeApplier>();
            }

            var applierSerialized = new SerializedObject(applier);
            applierSerialized.FindProperty("_store").objectReferenceValue = store;
            applierSerialized.FindProperty("_stats").objectReferenceValue =
                playerGo.GetComponent<PlayerStats>();
            applierSerialized.FindProperty("_health").objectReferenceValue =
                playerGo.GetComponent<HealthComponent>();
            applierSerialized.FindProperty("_wallet").objectReferenceValue =
                bootstrapGo.GetComponent<RunCurrencyWallet>();

            SerializedProperty upgradeList = applierSerialized.FindProperty("_upgrades");
            upgradeList.arraySize = upgrades.Length;
            for (int i = 0; i < upgrades.Length; i++)
            {
                upgradeList.GetArrayElementAtIndex(i).objectReferenceValue = upgrades[i];
            }

            applierSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
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
