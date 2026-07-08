using SurveHive.Combat;
using SurveHive.Core;
using SurveHive.Currency;
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
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurveHive.BuildTools
{
    public static class BeehiveSceneValidator
    {
        [MenuItem("SurveHive/Validate Beehive Vertical Slice")]
        public static void Validate()
        {
            bool ok = true;

            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Beehive.unity", OpenSceneMode.Single);

            GameObject[] roots = scene.GetRootGameObjects();
            int missingScriptCount = 0;
            foreach (GameObject root in roots)
            {
                missingScriptCount += CountMissingScriptsRecursive(root);
            }
            ok &= Check(missingScriptCount == 0, $"Missing script count == 0 (found {missingScriptCount})");

            var player = GameObject.Find("Player");
            ok &= Check(player != null, "Player GameObject exists");

            if (player != null)
            {
                ok &= Check(player.CompareTag("Player"), "Player tagged 'Player'");
                ok &= Check(player.GetComponent<Rigidbody2D>() != null, "Player has Rigidbody2D");
                ok &= Check(player.GetComponent<HealthComponent>() != null, "Player has HealthComponent");
                ok &= Check(player.GetComponent<PlayerStats>() != null, "Player has PlayerStats");

                var playerStats = player.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    // Phase 1A balance: crit starts at 0 — all crit chance comes
                    // from Keen Eye / meta upgrades.
                    var so = new SerializedObject(playerStats);
                    ok &= Check(Mathf.Approximately(so.FindProperty("_critChancePercent").floatValue, 0f),
                        "PlayerStats._critChancePercent == 0 (base crit removed in 1A)");
                }

                var pic = player.GetComponent<PlayerInputController>();
                ok &= Check(pic != null, "Player has PlayerInputController");
                if (pic != null)
                {
                    var so = new SerializedObject(pic);
                    ok &= Check(so.FindProperty("_actionsAsset").objectReferenceValue != null, "PlayerInputController._actionsAsset wired");
                    ok &= Check(so.FindProperty("_joystickUi").objectReferenceValue != null, "PlayerInputController._joystickUi wired");
                }

                var bootstrap = player.GetComponent<PlayerBootstrap>();
                ok &= Check(bootstrap != null, "Player has PlayerBootstrap");
                if (bootstrap != null)
                {
                    var so = new SerializedObject(bootstrap);
                    ok &= Check(so.FindProperty("_movement").objectReferenceValue != null, "PlayerBootstrap._movement wired");
                    ok &= Check(so.FindProperty("_inputController").objectReferenceValue != null, "PlayerBootstrap._inputController wired");
                    ok &= Check(so.FindProperty("_stats").objectReferenceValue != null, "PlayerBootstrap._stats wired");
                }

                var autoAttack = player.GetComponent<AutoAttack>();
                ok &= Check(autoAttack != null, "Player has AutoAttack");
                if (autoAttack != null)
                {
                    var so = new SerializedObject(autoAttack);
                    ok &= Check(so.FindProperty("_targeter").objectReferenceValue != null, "AutoAttack._targeter wired");
                    ok &= Check(so.FindProperty("_stats").objectReferenceValue != null, "AutoAttack._stats wired");
                }
            }

            var gameBootstrapGo = GameObject.Find("GameBootstrap");
            ok &= Check(gameBootstrapGo != null, "GameBootstrap GameObject exists");
            if (gameBootstrapGo != null)
            {
                ok &= Check(gameBootstrapGo.GetComponent<EnemyRegistry>() != null, "GameBootstrap has EnemyRegistry");
                ok &= Check(gameBootstrapGo.GetComponent<PoolManager>() != null, "GameBootstrap has PoolManager");
                ok &= Check(gameBootstrapGo.GetComponent<RunCurrencyWallet>() != null, "GameBootstrap has RunCurrencyWallet");
                ok &= Check(gameBootstrapGo.GetComponent<RunSession>() != null, "GameBootstrap has RunSession");

                var gb = gameBootstrapGo.GetComponent<GameBootstrap>();
                ok &= Check(gb != null, "GameBootstrap has GameBootstrap component");
                if (gb != null)
                {
                    var so = new SerializedObject(gb);
                    var pools = so.FindProperty("_pools");
                    ok &= Check(pools.arraySize >= 24, $"GameBootstrap._pools has >=24 entries (found {pools.arraySize})");
                    bool hasDamageNumberPool = false;
                    bool hasQueensGuardPool = false;
                    bool hasDeathVfxPool = false;
                    for (int i = 0; i < pools.arraySize; i++)
                    {
                        var entry = pools.GetArrayElementAtIndex(i);
                        ok &= Check(entry.FindPropertyRelative("prefab").objectReferenceValue != null, $"Pool entry {i} prefab wired");
                        int poolId = entry.FindPropertyRelative("poolId").intValue;
                        if (poolId == PoolIds.DamageNumber)
                        {
                            hasDamageNumberPool = true;
                        }
                        else if (poolId == PoolIds.QueensGuard)
                        {
                            hasQueensGuardPool = true;
                        }
                        else if (poolId == PoolIds.DeathVfx)
                        {
                            hasDeathVfxPool = true;
                        }
                    }
                    ok &= Check(hasDamageNumberPool, "GameBootstrap._pools includes DamageNumber pool");
                    ok &= Check(hasQueensGuardPool, "GameBootstrap._pools includes QueensGuard pool");
                    ok &= Check(hasDeathVfxPool, "GameBootstrap._pools includes DeathVfx pool");
                }

                var autoAttack2 = player.GetComponent<AutoAttack>();
                if (autoAttack2 != null)
                {
                    var so = new SerializedObject(autoAttack2);
                    ok &= Check(so.FindProperty("_audioSource").objectReferenceValue != null, "AutoAttack._audioSource wired");
                    ok &= Check(so.FindProperty("_shootClip").objectReferenceValue != null, "AutoAttack._shootClip wired");
                }
            }

            var spawnerGo = GameObject.Find("EnemySpawner");
            ok &= Check(spawnerGo != null, "EnemySpawner GameObject exists");
            if (spawnerGo != null)
            {
                var spawner = spawnerGo.GetComponent<EnemySpawner>();
                ok &= Check(spawner != null, "EnemySpawner has EnemySpawner component");
                if (spawner != null)
                {
                    var so = new SerializedObject(spawner);
                    ok &= Check(so.FindProperty("_config").objectReferenceValue != null, "EnemySpawner._config wired");
                    ok &= Check(so.FindProperty("_player").objectReferenceValue != null, "EnemySpawner._player wired");
                    ok &= Check(so.FindProperty("_playerExperience").objectReferenceValue != null, "EnemySpawner._playerExperience wired");
                    ok &= Check(so.FindProperty("_currencyWallet").objectReferenceValue != null, "EnemySpawner._currencyWallet wired");
                    // 1B: difficulty tier table feeding enemy HP/damage/spawn-rate.
                    ok &= Check(so.FindProperty("_difficulty").objectReferenceValue is Data.DifficultySO,
                        "EnemySpawner._difficulty wired to the tier table");
                }
            }

            var canvasGo = GameObject.Find("Canvas");
            ok &= Check(canvasGo != null, "Canvas GameObject exists");

            GameObject levelUpPanel = canvasGo != null ? FindChildIncludingInactive(canvasGo.transform, "LevelUpPanel") : null;
            ok &= Check(levelUpPanel != null, "LevelUpPanel exists");
            // Active by design: LevelUpUIController subscribes in OnEnable and hides
            // the panel via CanvasGroup alpha, not SetActive.
            ok &= Check(levelUpPanel != null && levelUpPanel.activeSelf, "LevelUpPanel starts active (CanvasGroup drives visibility)");
            if (levelUpPanel != null)
            {
                var controller = levelUpPanel.GetComponent<UI.LevelUpUIController>();
                ok &= Check(controller != null, "LevelUpPanel has LevelUpUIController");
                if (controller != null)
                {
                    var so = new SerializedObject(controller);
                    ok &= Check(so.FindProperty("_database").objectReferenceValue != null, "LevelUpUIController._database wired");
                    var buttons = so.FindProperty("_choiceButtons");
                    ok &= Check(buttons.arraySize == 3, $"LevelUpUIController._choiceButtons has 3 entries (found {buttons.arraySize})");
                    // Combat 2.0 1A/1F: lane banners + counters wired (run "SurveHive/Combat 2.0/1A").
                    var banners = so.FindProperty("_choiceBanners");
                    ok &= Check(banners.arraySize == 3, $"LevelUpUIController._choiceBanners has 3 entries (found {banners.arraySize})");
                    var laneCounters = so.FindProperty("_choiceLaneCounters");
                    ok &= Check(laneCounters.arraySize == 3, $"LevelUpUIController._choiceLaneCounters has 3 entries (found {laneCounters.arraySize})");
                    // 1C rerolls: store + upgrade + per-card buttons + count text.
                    ok &= Check(so.FindProperty("_metaStore").objectReferenceValue is Data.PersistentMetaProgressionStoreSO,
                        "LevelUpUIController._metaStore wired to the persistent store");
                    ok &= Check(so.FindProperty("_rerollUpgrade").objectReferenceValue != null,
                        "LevelUpUIController._rerollUpgrade wired");
                    var rerollButtons = so.FindProperty("_rerollButtons");
                    ok &= Check(rerollButtons.arraySize == 3,
                        $"LevelUpUIController._rerollButtons has 3 entries (found {rerollButtons.arraySize})");
                    for (int i = 0; i < rerollButtons.arraySize; i++)
                    {
                        ok &= Check(rerollButtons.GetArrayElementAtIndex(i).objectReferenceValue != null,
                            $"LevelUpUIController._rerollButtons[{i}] wired");
                    }

                    ok &= Check(so.FindProperty("_rerollCountText").objectReferenceValue != null,
                        "LevelUpUIController._rerollCountText wired");
                }
            }

            var eventSystemGo = GameObject.Find("EventSystem");
            ok &= Check(eventSystemGo != null, "EventSystem GameObject exists");

            ok &= ValidateEnemyHealthBar("Assets/Prefabs/Enemies/WorkerBee.prefab");
            ok &= ValidateEnemyHealthBar("Assets/Prefabs/Enemies/WarriorBee.prefab");
            ok &= ValidateEnemyHealthBar("Assets/Prefabs/Enemies/QueensGuard.prefab");
            ok &= ValidateDamageNumberPrefab("Assets/Prefabs/UI/DamageNumber.prefab");

            ok &= ValidatePhase1LookAndFeel(player, canvasGo);
            ok &= ValidatePhase2CombatDepth(player, canvasGo);
            ok &= ValidatePhase3RunStructure(canvasGo);
            ok &= ValidatePhase4MetaAndMenus(player);
            ok &= ValidateEnemyVariety();

            Debug.Log(ok ? "SurveHive Beehive scene validation PASSED." : "SurveHive Beehive scene validation FAILED - see errors above.");
        }

        // --- Phase 4 (PLAN.md): save/load, meta shop, menus, pause ---
        private static bool ValidatePhase4MetaAndMenus(GameObject player)
        {
            bool ok = true;

            // 4A: persistent store asset.
            var store = AssetDatabase.LoadAssetAtPath<Data.PersistentMetaProgressionStoreSO>(
                "Assets/Data/Progression/PersistentMetaProgressionStore.asset");
            ok &= Check(store != null, "PersistentMetaProgressionStore asset exists");

            // 4A + 1C: all thirteen shop upgrades, unique ids/stats, escalating costs.
            string[] upgradePaths =
            {
                "Assets/Data/Meta/MaxHealth.asset",
                "Assets/Data/Meta/Damage.asset",
                "Assets/Data/Meta/MoveSpeed.asset",
                "Assets/Data/Meta/AttackSpeed.asset",
                "Assets/Data/Meta/Magnet.asset",
                "Assets/Data/Meta/CurrencyGain.asset",
                "Assets/Data/Meta/ExpGain.asset",
                "Assets/Data/Meta/AbilityPower.asset",
                "Assets/Data/Meta/CooldownReduction.asset",
                "Assets/Data/Meta/CritChance.asset",
                "Assets/Data/Meta/CritDamage.asset",
                "Assets/Data/Meta/ItemDropRate.asset",
                "Assets/Data/Meta/Rerolls.asset",
            };

            var upgradeIds = new System.Collections.Generic.HashSet<string>();
            var upgradeStats = new System.Collections.Generic.HashSet<Data.MetaStatType>();
            foreach (string path in upgradePaths)
            {
                var upgrade = AssetDatabase.LoadAssetAtPath<Data.MetaUpgradeSO>(path);
                ok &= Check(upgrade != null, $"{path} exists");
                if (upgrade == null)
                {
                    continue;
                }

                ok &= Check(!string.IsNullOrEmpty(upgrade.UpgradeId), $"{path} has an upgrade id");
                ok &= Check(!string.IsNullOrEmpty(upgrade.EffectLabel), $"{path} has an effect label");
                ok &= Check(upgradeIds.Add(upgrade.UpgradeId), $"{path} id '{upgrade.UpgradeId}' unique");
                ok &= Check(upgradeStats.Add(upgrade.StatType), $"{path} stat '{upgrade.StatType}' unique");
                ok &= Check(upgrade.MaxRank > 0, $"{path} max rank > 0");
                ok &= Check(upgrade.BaseCost > 0, $"{path} base cost > 0");
                ok &= Check(upgrade.CostGrowth > 1f, $"{path} cost growth > 1 (escalating)");
                ok &= Check(upgrade.EffectPerRank > 0f, $"{path} effect per rank > 0");
            }

            // 1C: the user-mandated crit and reroll gates.
            var critChance = AssetDatabase.LoadAssetAtPath<Data.MetaUpgradeSO>("Assets/Data/Meta/CritChance.asset");
            ok &= Check(critChance != null && critChance.MaxRank * critChance.EffectPerRank == 40f,
                "meta crit chance caps at exactly +40%");
            var rerolls = AssetDatabase.LoadAssetAtPath<Data.MetaUpgradeSO>("Assets/Data/Meta/Rerolls.asset");
            ok &= Check(rerolls != null && rerolls.MaxRank == 3 && rerolls.BaseCost >= 400
                && rerolls.CostGrowth >= 3.5f, "rerolls capped at 3/run and cost-gated hard");

            // 4A: RunSession banks into the persistent store and knows the level source.
            var sessionGo = GameObject.Find("GameBootstrap");
            var session = sessionGo != null ? sessionGo.GetComponent<RunSession>() : null;
            ok &= Check(session != null, "RunSession present on GameBootstrap");
            if (session != null)
            {
                var so = new SerializedObject(session);
                ok &= Check(
                    so.FindProperty("_metaProgressionStore").objectReferenceValue is Data.PersistentMetaProgressionStoreSO,
                    "RunSession._metaProgressionStore wired to the persistent store");
                ok &= Check(so.FindProperty("_playerExperience").objectReferenceValue != null,
                    "RunSession._playerExperience wired");
                // 1B: difficulty tier table feeding the honey-gain multiplier.
                ok &= Check(so.FindProperty("_difficulty").objectReferenceValue is Data.DifficultySO,
                    "RunSession._difficulty wired to the tier table");
            }

            // 1B: the difficulty tier table itself — 4 sane rows in tier order.
            var difficulty = AssetDatabase.LoadAssetAtPath<Data.DifficultySO>(
                "Assets/Data/Progression/DifficultySettings.asset");
            ok &= Check(difficulty != null, "DifficultySettings asset exists");
            if (difficulty != null)
            {
                ok &= Check(difficulty.TierCount == 4, $"DifficultySettings has 4 tiers (found {difficulty.TierCount})");
                for (int i = 0; i < difficulty.TierCount; i++)
                {
                    Data.DifficultySO.TierSettings tier = difficulty.GetTierAt(i);
                    ok &= Check((int)tier.tier == i, $"difficulty row {i} is tier {(Data.DifficultyTier)i}");
                    ok &= Check(!string.IsNullOrEmpty(tier.displayName), $"difficulty row {i} has a display name");
                    ok &= Check(tier.icon != null, $"difficulty row {i} has an icon");
                    ok &= Check(tier.enemyHealthMultiplier > 0f && tier.enemyDamageMultiplier > 0f,
                        $"difficulty row {i} enemy multipliers > 0");
                    ok &= Check(tier.spawnRateMultiplier > 0f, $"difficulty row {i} spawn-rate multiplier > 0");
                    ok &= Check(tier.honeyGainMultiplier > 0f, $"difficulty row {i} honey multiplier > 0");
                }

                Data.DifficultySO.TierSettings normal = difficulty.GetSettings(Data.DifficultyTier.Normal);
                ok &= Check(normal.enemyHealthMultiplier == 1f && normal.enemyDamageMultiplier == 1f
                    && normal.honeyGainMultiplier == 1f, "Normal tier is the identity baseline");
            }

            // 4A: player applies purchased ranks at run start.
            var applier = player != null ? player.GetComponent<MetaUpgradeApplier>() : null;
            ok &= Check(applier != null, "Player has MetaUpgradeApplier");
            if (applier != null)
            {
                var so = new SerializedObject(applier);
                ok &= Check(so.FindProperty("_store").objectReferenceValue is Data.PersistentMetaProgressionStoreSO,
                    "MetaUpgradeApplier._store wired to the persistent store");
                ok &= Check(so.FindProperty("_stats").objectReferenceValue != null, "MetaUpgradeApplier._stats wired");
                ok &= Check(so.FindProperty("_health").objectReferenceValue != null, "MetaUpgradeApplier._health wired");
                ok &= Check(so.FindProperty("_wallet").objectReferenceValue != null, "MetaUpgradeApplier._wallet wired");
                // 1C: every upgrade except Rerolls (applied by the level-up
                // controller instead), plus the EXP-gain target.
                var upgradesProp = so.FindProperty("_upgrades");
                ok &= Check(upgradesProp.arraySize == upgradePaths.Length - 1,
                    $"MetaUpgradeApplier._upgrades has {upgradePaths.Length - 1} entries (found {upgradesProp.arraySize})");
                for (int i = 0; i < upgradesProp.arraySize; i++)
                {
                    var wired = upgradesProp.GetArrayElementAtIndex(i).objectReferenceValue as Data.MetaUpgradeSO;
                    ok &= Check(wired != null, $"MetaUpgradeApplier._upgrades[{i}] wired");
                    ok &= Check(wired == null || wired.StatType != Data.MetaStatType.Rerolls,
                        $"MetaUpgradeApplier._upgrades[{i}] is not the reroll upgrade");
                }

                ok &= Check(so.FindProperty("_experience").objectReferenceValue != null,
                    "MetaUpgradeApplier._experience wired");
            }

            // 4B: results screens route to retry / menu.
            var canvas = GameObject.Find("Canvas");
            ok &= ValidateResultsRouting(canvas, "GameOverPanel");
            ok &= ValidateResultsRouting(canvas, "VictoryPanel");

            // 4C: in-run pause menu.
            ok &= ValidatePauseMenu(canvas);

            // 4B: build settings boot the menu first, run scene second.
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            ok &= Check(buildScenes.Length == 2, $"Build settings list 2 scenes (found {buildScenes.Length})");
            if (buildScenes.Length == 2)
            {
                ok &= Check(buildScenes[0].path == "Assets/Scenes/MainMenu.unity" && buildScenes[0].enabled,
                    "Build settings scene 0 is MainMenu (enabled)");
                ok &= Check(buildScenes[1].path == "Assets/Scenes/Beehive.unity" && buildScenes[1].enabled,
                    "Build settings scene 1 is Beehive (enabled)");
            }

            // 4B: the MainMenu scene itself. Opened last — this replaces the
            // Beehive scene, so no Beehive checks may follow.
            ok &= ValidateMainMenuScene();

            return ok;
        }

        private static bool ValidatePauseMenu(GameObject canvasGo)
        {
            bool ok = true;

            GameObject root = canvasGo != null ? FindChildIncludingInactive(canvasGo.transform, "PauseRoot") : null;
            ok &= Check(root != null, "PauseRoot exists");
            if (root == null)
            {
                return false;
            }

            ok &= Check(root.activeSelf, "PauseRoot active (hosts the controller + HUD button)");

            var controller = root.GetComponent<PauseMenuController>();
            ok &= Check(controller != null, "PauseRoot has PauseMenuController");
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                string[] refs =
                {
                    "_pausePanel", "_settingsPanel", "_pauseButton",
                    "_resumeButton", "_settingsButton", "_settingsBackButton", "_abandonButton",
                };
                foreach (string field in refs)
                {
                    ok &= Check(so.FindProperty(field).objectReferenceValue != null,
                        $"PauseMenuController.{field} wired");
                }

                var pausePanel = (GameObject)so.FindProperty("_pausePanel").objectReferenceValue;
                var settingsPanel = (GameObject)so.FindProperty("_settingsPanel").objectReferenceValue;
                ok &= Check(pausePanel != null && !pausePanel.activeSelf, "PausePanel inactive at rest");
                ok &= Check(settingsPanel != null && !settingsPanel.activeSelf, "PauseSettingsPanel inactive at rest");

                var pauseButtonGo = (Object)so.FindProperty("_pauseButton").objectReferenceValue;
                var pauseButton = pauseButtonGo as UnityEngine.UI.Button;
                ok &= Check(pauseButton != null && pauseButton.gameObject.activeSelf, "HUD PauseButton active");

                if (settingsPanel != null)
                {
                    ok &= ValidateSettingsPanelUI(settingsPanel, "Beehive pause");
                }
            }

            return ok;
        }

        private static bool ValidateSettingsPanelUI(GameObject holder, string label)
        {
            bool ok = true;

            var settingsUi = holder.GetComponent<SettingsPanelUI>();
            ok &= Check(settingsUi != null, $"{label} settings panel has SettingsPanelUI");
            if (settingsUi == null)
            {
                return false;
            }

            var so = new SerializedObject(settingsUi);
            ok &= Check(so.FindProperty("_store").objectReferenceValue is Data.PersistentMetaProgressionStoreSO,
                $"{label} SettingsPanelUI._store wired to the persistent store");
            string[] refs =
            {
                "_musicSlider", "_sfxSlider",
                "_vibrationButton", "_vibrationLabel", "_qualityButton", "_qualityLabel",
            };
            foreach (string field in refs)
            {
                ok &= Check(so.FindProperty(field).objectReferenceValue != null,
                    $"{label} SettingsPanelUI.{field} wired");
            }

            return ok;
        }

        private static bool ValidateResultsRouting(GameObject canvasGo, string panelName)
        {
            bool ok = true;

            GameObject panel = canvasGo != null ? FindChildIncludingInactive(canvasGo.transform, panelName) : null;
            ok &= Check(panel != null, $"{panelName} exists for results routing");
            if (panel == null)
            {
                return false;
            }

            ok &= ValidateSceneLoadButton(panel, "RetryButton", "Beehive");
            ok &= ValidateSceneLoadButton(panel, "MenuButton", "MainMenu");
            return ok;
        }

        private static bool ValidateSceneLoadButton(GameObject panel, string buttonName, string expectedScene)
        {
            bool ok = true;

            GameObject buttonGo = FindChildIncludingInactive(panel.transform, buttonName);
            ok &= Check(buttonGo != null, $"{panel.name}/{buttonName} exists");
            if (buttonGo == null)
            {
                return false;
            }

            ok &= Check(buttonGo.GetComponent<UnityEngine.UI.Button>() != null, $"{panel.name}/{buttonName} has Button");
            var loader = buttonGo.GetComponent<SceneLoadButton>();
            ok &= Check(loader != null, $"{panel.name}/{buttonName} has SceneLoadButton");
            if (loader != null)
            {
                var so = new SerializedObject(loader);
                ok &= Check(so.FindProperty("_sceneName").stringValue == expectedScene,
                    $"{panel.name}/{buttonName} loads '{expectedScene}'");
            }

            return ok;
        }

        private static bool ValidateMainMenuScene()
        {
            bool ok = true;

            ok &= Check(System.IO.File.Exists("Assets/Scenes/MainMenu.unity"), "MainMenu scene file exists");
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

            ok &= Check(GameObject.Find("EventSystem") != null, "MainMenu has EventSystem");
            ok &= Check(GameObject.FindWithTag("MainCamera") != null, "MainMenu has camera");

            var controllerGo = GameObject.Find("MainMenuController");
            var controller = controllerGo != null ? controllerGo.GetComponent<MainMenuController>() : null;
            ok &= Check(controller != null, "MainMenuController present");
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                string[] refs =
                {
                    "_mainPanel", "_worldSelectPanel", "_shopPanel", "_settingsPanel",
                    "_playButton", "_shopButton", "_settingsButton", "_quitButton",
                    "_worldSelectBackButton", "_shopBackButton", "_settingsBackButton", "_startBeehiveButton",
                };
                foreach (string field in refs)
                {
                    ok &= Check(so.FindProperty(field).objectReferenceValue != null, $"MainMenuController.{field} wired");
                }

                var mainPanel = (GameObject)so.FindProperty("_mainPanel").objectReferenceValue;
                var shopPanel = (GameObject)so.FindProperty("_shopPanel").objectReferenceValue;
                var worldPanel = (GameObject)so.FindProperty("_worldSelectPanel").objectReferenceValue;
                var settingsPanel = (GameObject)so.FindProperty("_settingsPanel").objectReferenceValue;

                ok &= Check(mainPanel != null && mainPanel.activeSelf, "MainPanel active at rest");
                ok &= Check(worldPanel != null && !worldPanel.activeSelf, "WorldSelectPanel inactive at rest");
                ok &= Check(shopPanel != null && !shopPanel.activeSelf, "ShopPanel inactive at rest");
                ok &= Check(settingsPanel != null && !settingsPanel.activeSelf, "SettingsPanel inactive at rest");

                // 4C: real settings controls in the menu's settings panel.
                if (settingsPanel != null)
                {
                    ok &= ValidateSettingsPanelUI(settingsPanel, "MainMenu");
                }

                // Locked worlds stay locked.
                if (worldPanel != null)
                {
                    GameObject garden = FindChildIncludingInactive(worldPanel.transform, "GardenButton");
                    ok &= Check(garden != null && !garden.GetComponent<UnityEngine.UI.Button>().interactable,
                        "GardenButton locked");
                    GameObject woods = FindChildIncludingInactive(worldPanel.transform, "WoodsButton");
                    ok &= Check(woods != null && !woods.GetComponent<UnityEngine.UI.Button>().interactable,
                        "WoodsButton locked");
                    // 1B: the difficulty picker is live — 4 tiers, icon slots,
                    // and a DifficultySelectUI wired to dropdown/table/store.
                    GameObject dropdownGo = FindChildIncludingInactive(worldPanel.transform, "DifficultyDropdown");
                    ok &= Check(dropdownGo != null, "DifficultyDropdown present");
                    if (dropdownGo != null)
                    {
                        var dropdown = dropdownGo.GetComponent<TMPro.TMP_Dropdown>();
                        ok &= Check(dropdown != null && dropdown.interactable, "DifficultyDropdown interactable");
                        if (dropdown != null)
                        {
                            ok &= Check(dropdown.options.Count == 4,
                                $"DifficultyDropdown has 4 options (found {dropdown.options.Count})");
                            ok &= Check(dropdown.itemImage != null, "DifficultyDropdown item icon slot wired");
                            ok &= Check(dropdown.captionImage != null, "DifficultyDropdown caption icon slot wired");
                        }

                        var select = worldPanel.GetComponentInChildren<DifficultySelectUI>(true);
                        ok &= Check(select != null, "WorldSelectPanel has DifficultySelectUI");
                        if (select != null)
                        {
                            var selectSo = new SerializedObject(select);
                            ok &= Check(selectSo.FindProperty("_dropdown").objectReferenceValue != null,
                                "DifficultySelectUI._dropdown wired");
                            ok &= Check(selectSo.FindProperty("_difficulty").objectReferenceValue is Data.DifficultySO,
                                "DifficultySelectUI._difficulty wired");
                            ok &= Check(
                                selectSo.FindProperty("_store").objectReferenceValue is Data.PersistentMetaProgressionStoreSO,
                                "DifficultySelectUI._store wired to the persistent store");
                        }
                    }
                }

                // Shop wiring: persistent store + one row per upgrade, fully wired.
                var shopUi = shopPanel != null ? shopPanel.GetComponent<MetaShopUI>() : null;
                ok &= Check(shopUi != null, "ShopPanel has MetaShopUI");
                if (shopUi != null)
                {
                    var shopSo = new SerializedObject(shopUi);
                    ok &= Check(
                        shopSo.FindProperty("_store").objectReferenceValue is Data.PersistentMetaProgressionStoreSO,
                        "MetaShopUI._store wired to the persistent store");
                    ok &= Check(shopSo.FindProperty("_balanceText").objectReferenceValue != null,
                        "MetaShopUI._balanceText wired");

                    // 1C: the shop scrolls — cards live under ShopScroll/Viewport/Content.
                    Transform shopScroll = shopPanel.transform.Find("ShopScroll");
                    ok &= Check(shopScroll != null, "ShopPanel has ShopScroll");
                    if (shopScroll != null)
                    {
                        var scroll = shopScroll.GetComponent<UnityEngine.UI.ScrollRect>();
                        ok &= Check(scroll != null && scroll.content != null && scroll.viewport != null,
                            "ShopScroll ScrollRect wired (content + viewport)");
                        ok &= Check(scroll != null && scroll.vertical && !scroll.horizontal,
                            "ShopScroll scrolls vertically only");
                    }

                    var rowsProp = shopSo.FindProperty("_rows");
                    ok &= Check(rowsProp.arraySize == 13, $"MetaShopUI._rows has 13 entries (found {rowsProp.arraySize})");
                    for (int i = 0; i < rowsProp.arraySize; i++)
                    {
                        var row = rowsProp.GetArrayElementAtIndex(i).objectReferenceValue as MetaShopRowUI;
                        ok &= Check(row != null, $"MetaShopUI._rows[{i}] wired");
                        if (row == null)
                        {
                            continue;
                        }

                        var rowSo = new SerializedObject(row);
                        ok &= Check(rowSo.FindProperty("_upgrade").objectReferenceValue != null,
                            $"Shop row {i} upgrade wired");
                        ok &= Check(rowSo.FindProperty("_nameText").objectReferenceValue != null,
                            $"Shop row {i} name text wired");
                        ok &= Check(rowSo.FindProperty("_descriptionText").objectReferenceValue != null,
                            $"Shop row {i} description text wired");
                        ok &= Check(rowSo.FindProperty("_effectText").objectReferenceValue != null,
                            $"Shop row {i} effect/value text wired");
                        ok &= Check(rowSo.FindProperty("_rankText").objectReferenceValue != null,
                            $"Shop row {i} rank text wired");
                        ok &= Check(rowSo.FindProperty("_costText").objectReferenceValue != null,
                            $"Shop row {i} cost text wired");
                        ok &= Check(rowSo.FindProperty("_buyButton").objectReferenceValue != null,
                            $"Shop row {i} buy button wired");
                    }
                }
            }

            return ok;
        }

        // --- Phase 1 (PLAN.md): art swap, game feel, UI reskin ---
        private static bool ValidatePhase1LookAndFeel(GameObject player, GameObject canvasGo)
        {
            bool ok = true;

            // Camera foundation + shake.
            GameObject cameraGo = GameObject.FindWithTag("MainCamera");
            var pixelPerfect = cameraGo != null ? cameraGo.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>() : null;
            ok &= Check(pixelPerfect != null && pixelPerfect.assetsPPU == 16, "Pixel Perfect Camera at PPU 16");
            var shaker = cameraGo != null ? cameraGo.GetComponent<View.CameraShaker>() : null;
            ok &= Check(shaker != null, "Camera has CameraShaker");
            if (cameraGo != null && cameraGo.TryGetComponent(out Player.CameraFollow follow))
            {
                var so = new SerializedObject(follow);
                ok &= Check(so.FindProperty("_shaker").objectReferenceValue != null, "CameraFollow._shaker wired");
            }

            var bootstrapGo = GameObject.Find("GameBootstrap");
            ok &= Check(bootstrapGo != null && bootstrapGo.GetComponent<HitStop>() != null, "GameBootstrap has HitStop");

            // Player rig + feedback.
            if (player != null)
            {
                ok &= ValidateBeeRig(player, "Player");
                ok &= Check(player.GetComponent<SpriteRenderer>() == null, "Player root placeholder SpriteRenderer removed");
                ok &= Check(player.GetComponent<PlayerHitFeedback>() != null, "Player has PlayerHitFeedback");

                var autoAttack = player.GetComponent<AutoAttack>();
                if (autoAttack != null)
                {
                    var so = new SerializedObject(autoAttack);
                    ok &= Check(so.FindProperty("_characterAnimator").objectReferenceValue != null, "AutoAttack._characterAnimator wired");
                }
            }

            // Enemy prefab rigs.
            ok &= ValidateEnemyRigPrefab("Assets/Prefabs/Enemies/WorkerBee.prefab");
            ok &= ValidateEnemyRigPrefab("Assets/Prefabs/Enemies/WarriorBee.prefab");
            ok &= ValidateEnemyRigPrefab("Assets/Prefabs/Enemies/QueensGuard.prefab");

            // Queen's Guard data + wave entry.
            var queensGuardStats = AssetDatabase.LoadAssetAtPath<Data.EnemyStatsSO>("Assets/Data/Enemies/QueensGuard.asset");
            ok &= Check(queensGuardStats != null, "QueensGuard stats asset exists");
            var waveConfig = AssetDatabase.LoadAssetAtPath<Data.WaveSpawnerConfigSO>("Assets/Data/Waves/BeehiveWaveConfig.asset");
            if (waveConfig != null && queensGuardStats != null)
            {
                var so = new SerializedObject(waveConfig);
                var entries = so.FindProperty("_entries");
                bool inWaves = false;
                for (int i = 0; i < entries.arraySize; i++)
                {
                    if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("enemyStats").objectReferenceValue == queensGuardStats)
                    {
                        inWaves = true;
                    }
                }

                ok &= Check(inWaves, "QueensGuard present in wave config");
            }

            // Real sprites on projectile/pickups (no placeholder circles).
            ok &= ValidatePrefabSprite("Assets/Prefabs/Projectiles/Stinger.prefab", "Stinger");
            // Phase 3 feedback round: EXP orbs use the neutral tintable sprite.
            ok &= ValidatePrefabSprite("Assets/Prefabs/Pickups/ExpPickup.prefab", "ExpOrb");
            ok &= ValidatePrefabSprite("Assets/Prefabs/Pickups/CurrencyPickup.prefab", "HoneyDrop");

            // Death VFX must be one-shot (the pack source loops).
            var deathVfx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/DeathPoof.prefab");
            if (deathVfx != null)
            {
                bool anyLooping = false;
                ParticleSystem[] systems = deathVfx.GetComponentsInChildren<ParticleSystem>(true);
                for (int i = 0; i < systems.Length; i++)
                {
                    anyLooping |= systems[i].main.loop;
                }

                ok &= Check(systems.Length > 0 && !anyLooping, "DeathPoof particle systems are one-shot (no looping)");
            }

            // Flash material.
            var flashMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SpriteFlash.mat");
            ok &= Check(flashMaterial != null && flashMaterial.shader != null &&
                flashMaterial.shader.name == "SurveHive/SpriteFlash", "SpriteFlash material + shader exist");

            // UI: no legacy Text anywhere, BoldPixels TMP on the HUD, kit sprites applied.
            if (canvasGo != null)
            {
                int legacyTextCount = 0;
                CountComponentsRecursive<UnityEngine.UI.Text>(canvasGo.transform, ref legacyTextCount);
                ok &= Check(legacyTextCount == 0, $"No legacy UI.Text components remain (found {legacyTextCount})");

                GameObject levelText = FindChildIncludingInactive(canvasGo.transform, "LevelText");
                var levelTmp = levelText != null ? levelText.GetComponent<TMPro.TextMeshProUGUI>() : null;
                ok &= Check(levelTmp != null && levelTmp.font != null && levelTmp.font.name.Contains("BoldPixels"),
                    "LevelText uses BoldPixels TMP font");

                GameObject levelUpPanel = FindChildIncludingInactive(canvasGo.transform, "LevelUpPanel");
                var panelImage = levelUpPanel != null ? levelUpPanel.GetComponent<UnityEngine.UI.Image>() : null;
                ok &= Check(panelImage != null && panelImage.sprite != null, "LevelUpPanel uses pixel kit sprite");

                var controller = levelUpPanel != null ? levelUpPanel.GetComponent<UI.LevelUpUIController>() : null;
                if (controller != null)
                {
                    var so = new SerializedObject(controller);
                    var icons = so.FindProperty("_choiceIcons");
                    bool iconsWired = icons.arraySize == 3;
                    for (int i = 0; i < icons.arraySize; i++)
                    {
                        iconsWired &= icons.GetArrayElementAtIndex(i).objectReferenceValue != null;
                    }

                    ok &= Check(iconsWired, "LevelUpUIController._choiceIcons wired");
                }

                ok &= Check(FindChildIncludingInactive(canvasGo.transform, "KillCounterText") != null, "Kill counter exists");
                ok &= Check(FindChildIncludingInactive(canvasGo.transform, "RunTimerText") != null, "Run timer exists");
            }

            // Skill icons assigned.
            var swiftWings = AssetDatabase.LoadAssetAtPath<Data.SkillDefinitionSO>("Assets/Data/Skills/SwiftWings.asset");
            ok &= Check(swiftWings != null && swiftWings.Icon != null, "Skill icons assigned (SwiftWings)");

            return ok;
        }

        // --- Phase 2 (PLAN.md): status effects, active skill arsenal, rarity ---
        private static bool ValidatePhase2CombatDepth(GameObject player, GameObject canvasGo)
        {
            bool ok = true;

            // Player: active skill manager + aura visual.
            if (player != null)
            {
                var manager = player.GetComponent<Combat.Skills.ActiveSkillManager>();
                ok &= Check(manager != null, "Player has ActiveSkillManager");
                if (manager != null)
                {
                    var so = new SerializedObject(manager);
                    ok &= Check(so.FindProperty("_stats").objectReferenceValue != null, "ActiveSkillManager._stats wired");
                    ok &= Check(so.FindProperty("_targeter").objectReferenceValue != null, "ActiveSkillManager._targeter wired");
                    ok &= Check(so.FindProperty("_auraVisual").objectReferenceValue != null, "ActiveSkillManager._auraVisual wired");
                }

                Transform aura = player.transform.Find("PollenAura");
                ok &= Check(aura != null, "Player has PollenAura visual child");
                if (aura != null && aura.TryGetComponent(out SpriteRenderer auraRenderer))
                {
                    ok &= Check(!auraRenderer.enabled, "PollenAura renderer starts disabled");
                }
            }

            // Enemy prefabs: status receivers wired.
            ok &= ValidateEnemyStatusReceiver("Assets/Prefabs/Enemies/WorkerBee.prefab");
            ok &= ValidateEnemyStatusReceiver("Assets/Prefabs/Enemies/WarriorBee.prefab");
            ok &= ValidateEnemyStatusReceiver("Assets/Prefabs/Enemies/QueensGuard.prefab");

            // Pools registered for every skill prefab.
            var bootstrapGo = GameObject.Find("GameBootstrap");
            if (bootstrapGo != null && bootstrapGo.TryGetComponent(out GameBootstrap gb))
            {
                var so = new SerializedObject(gb);
                var pools = so.FindProperty("_pools");
                ok &= Check(HasPoolEntry(pools, PoolIds.SkillStinger), "Pool: SkillStinger registered");
                ok &= Check(HasPoolEntry(pools, PoolIds.SkillLance), "Pool: SkillLance registered");
                ok &= Check(HasPoolEntry(pools, PoolIds.SkillHoneyGlob), "Pool: SkillHoneyGlob registered");
                ok &= Check(HasPoolEntry(pools, PoolIds.SkillEmberBolt), "Pool: SkillEmberBolt registered");
                ok &= Check(HasPoolEntry(pools, PoolIds.HoneyPuddle), "Pool: HoneyPuddle registered");
                ok &= Check(HasPoolEntry(pools, PoolIds.ZapArcVfx), "Pool: ZapArcVfx registered");
                ok &= Check(HasPoolEntry(pools, PoolIds.EmberExplosionVfx), "Pool: EmberExplosionVfx registered");
                ok &= Check(HasPoolEntry(pools, PoolIds.HoneySplashVfx), "Pool: HoneySplashVfx registered");
            }

            // Level-up controller: manager reference for active skill cards.
            GameObject levelUpPanel = canvasGo != null ? FindChildIncludingInactive(canvasGo.transform, "LevelUpPanel") : null;
            if (levelUpPanel != null && levelUpPanel.TryGetComponent(out UI.LevelUpUIController controller))
            {
                var so = new SerializedObject(controller);
                ok &= Check(so.FindProperty("_activeSkillManager").objectReferenceValue != null,
                    "LevelUpUIController._activeSkillManager wired");

                // Long Phase 2 skill descriptions must word-wrap inside the cards.
                bool allWrap = true;
                var cardTexts = levelUpPanel.GetComponentsInChildren<TMPro.TMP_Text>(true);
                for (int i = 0; i < cardTexts.Length; i++)
                {
                    allWrap &= cardTexts[i].textWrappingMode == TMPro.TextWrappingModes.Normal;
                }

                ok &= Check(allWrap && cardTexts.Length > 0, "Level-up card texts use word wrapping");
            }

            // Skill database populated, all with icons; active cards resolve.
            // Minimums, not exact: the roster grows across Combat 2.0 sub-phases.
            var database = AssetDatabase.LoadAssetAtPath<Data.SkillDatabaseSO>("Assets/Data/Skills/SkillDatabase.asset");
            ok &= Check(database != null && database.Skills != null && database.Skills.Length >= 16,
                $"SkillDatabase populated (>=16 skills, found {(database != null && database.Skills != null ? database.Skills.Length : 0)})");
            if (database != null && database.Skills != null)
            {
                int activeCards = 0;
                bool allIcons = true;
                bool activeRefsOk = true;
                bool damageTypesOk = true;
                for (int i = 0; i < database.Skills.Length; i++)
                {
                    Data.SkillDefinitionSO skill = database.Skills[i];
                    if (skill == null)
                    {
                        allIcons = false;
                        continue;
                    }

                    allIcons &= skill.Icon != null;
                    if (skill.EffectType == Progression.SkillEffectType.ActiveSkill)
                    {
                        activeCards++;
                        activeRefsOk &= skill.ActiveSkill != null && skill.MaxLevel == skill.ActiveSkill.MaxLevel;
                        // Phase 3A damage typing: physical-element abilities deal
                        // physical damage, elemental ones deal magic.
                        if (skill.ActiveSkill != null)
                        {
                            bool physicalElement = skill.Element == Progression.SkillElement.Physical;
                            bool physicalDamage = skill.ActiveSkill.DamageType == Health.DamageType.Physical;
                            damageTypesOk &= physicalElement == physicalDamage;
                        }
                    }
                }

                ok &= Check(allIcons, "All database skills have icons");
                ok &= Check(activeCards >= 6, $"Database contains >=6 active skill cards (found {activeCards})");
                ok &= Check(activeRefsOk, "Active skill cards reference ActiveSkillSO with matching level caps");
                ok &= Check(damageTypesOk, "Ability damage types match card elements (physical ⇔ physical, elemental ⇔ magic)");

                // Phase 3C: one set bonus per element, tiers ascending from 2 pieces,
                // every tier actually granting something.
                Data.SetBonusSO[] setBonuses = database.SetBonuses;
                bool setsOk = setBonuses != null && setBonuses.Length == 6;
                if (setsOk)
                {
                    int coveredElements = 0;
                    for (int i = 0; i < setBonuses.Length; i++)
                    {
                        Data.SetBonusSO bonus = setBonuses[i];
                        if (bonus == null || bonus.TierCount < 3)
                        {
                            setsOk = false;
                            break;
                        }

                        coveredElements |= 1 << (int)bonus.Element;
                        int previousPieces = 1;
                        for (int t = 0; t < bonus.TierCount; t++)
                        {
                            Data.SetBonusTier tier = bonus.GetTier(t);
                            setsOk &= tier.PiecesRequired > previousPieces;
                            previousPieces = tier.PiecesRequired;
                            setsOk &= tier.StatusPotencyBonusPercent > 0f || tier.StatusDurationBonusPercent > 0f ||
                                tier.AttackDamageBonusPercent > 0f;
                        }

                        setsOk &= bonus.GetTier(0).PiecesRequired == 2;
                    }

                    setsOk &= coveredElements == 0b111111;
                }

                ok &= Check(setsOk, "SkillDatabase carries 6 valid element set bonuses (tiers from 2 pieces, all granting)");
            }

            // Phase 3C: set-tier summary lives on the offer panel, never on the
            // combat HUD (canvas root).
            ok &= Check(canvasGo == null || canvasGo.transform.Find("SetTierIndicator") == null,
                "No set-tier indicator on the combat HUD");
            GameObject offerPanelGo = canvasGo != null ? FindChildIncludingInactive(canvasGo.transform, "LevelUpPanel") : null;
            Transform setIndicator = offerPanelGo != null ? offerPanelGo.transform.Find("SetTierIndicator") : null;
            ok &= Check(setIndicator != null && setIndicator.TryGetComponent(out UI.SetTierHUD setHud) &&
                new SerializedObject(setHud).FindProperty("_text").objectReferenceValue != null,
                "SetTierIndicator on LevelUpPanel with SetTierHUD._text wired");

            // Offer-panel context title + below-card set lines wired to the controller.
            if (offerPanelGo != null && offerPanelGo.TryGetComponent(out UI.LevelUpUIController offerController))
            {
                var offerSo = new SerializedObject(offerController);
                ok &= Check(offerSo.FindProperty("_titleText").objectReferenceValue != null,
                    "LevelUpUIController._titleText wired (offer context title)");
                SerializedProperty setTexts = offerSo.FindProperty("_choiceSetTexts");
                bool setTextsOk = setTexts.arraySize == 3;
                for (int i = 0; setTextsOk && i < setTexts.arraySize; i++)
                {
                    setTextsOk &= setTexts.GetArrayElementAtIndex(i).objectReferenceValue != null;
                }

                ok &= Check(setTextsOk, "LevelUpUIController._choiceSetTexts wired (3 below-card set lines)");
            }

            // Active skill assets: 5-level tables with sane growth.
            ok &= ValidateActiveSkillGrowth("Assets/Data/Skills/Actives/StingerBarrage.asset");
            ok &= ValidateActiveSkillGrowth("Assets/Data/Skills/Actives/PiercingLance.asset");
            ok &= ValidateActiveSkillGrowth("Assets/Data/Skills/Actives/HoneySplash.asset");
            ok &= ValidateActiveSkillGrowth("Assets/Data/Skills/Actives/PollenCloud.asset");
            ok &= ValidateActiveSkillGrowth("Assets/Data/Skills/Actives/StaticWings.asset");
            ok &= ValidateActiveSkillGrowth("Assets/Data/Skills/Actives/EmberSting.asset");

            // Pooled skill prefabs exist with the right components.
            ok &= ValidateSkillPrefab("Assets/Prefabs/Skills/SkillStinger.prefab", typeof(Combat.Skills.SkillProjectile));
            ok &= ValidateSkillPrefab("Assets/Prefabs/Skills/SkillLance.prefab", typeof(Combat.Skills.SkillProjectile));
            ok &= ValidateSkillPrefab("Assets/Prefabs/Skills/HoneyGlobProjectile.prefab", typeof(Combat.Skills.SkillProjectile));
            ok &= ValidateSkillPrefab("Assets/Prefabs/Skills/EmberBolt.prefab", typeof(Combat.Skills.SkillProjectile));
            ok &= ValidateSkillPrefab("Assets/Prefabs/Skills/HoneyPuddle.prefab", typeof(Combat.Skills.AreaEffectZone));
            ok &= ValidateSkillPrefab("Assets/Prefabs/Skills/ZapArc.prefab", typeof(Combat.Skills.ZapArcVfx));
            ok &= ValidateSkillPrefab("Assets/Prefabs/VFX/EmberExplosion.prefab", typeof(View.PooledVfx));
            ok &= ValidateSkillPrefab("Assets/Prefabs/VFX/HoneySplash.prefab", typeof(View.PooledVfx));

            return ok;
        }

        // --- Phase 3 (PLAN.md): stage timeline, bosses, drops, results ---
        private static bool ValidatePhase3RunStructure(GameObject canvasGo)
        {
            bool ok = true;

            // Stage config: 4 timeline events at the plan's milestones.
            var stageConfig = AssetDatabase.LoadAssetAtPath<Data.StageConfigSO>("Assets/Data/Stage/BeehiveStageConfig.asset");
            ok &= Check(stageConfig != null, "BeehiveStageConfig asset exists");
            if (stageConfig != null)
            {
                ok &= Check(stageConfig.TotalDurationSeconds > 0f, "Stage duration is positive");
                Data.StageTimelineEvent[] events = stageConfig.Events;
                ok &= Check(events != null && events.Length == 4,
                    $"Stage config has 4 timeline events (found {(events != null ? events.Length : 0)})");
                if (events != null && events.Length == 4)
                {
                    ok &= Check(Mathf.Approximately(events[0].NormalizedTime, 0.25f) &&
                        events[0].Type == Data.StageEventType.StrongWaveRing &&
                        events[0].EnemyStats != null && events[0].Count > 0,
                        "25% strong wave (ring) configured");
                    ok &= Check(Mathf.Approximately(events[1].NormalizedTime, 0.5f) &&
                        events[1].Type == Data.StageEventType.Miniboss &&
                        events[1].EnemyStats != null && events[1].EnemyStats.Rank >= 3,
                        "50% miniboss event configured with a boss-rank enemy");
                    ok &= Check(Mathf.Approximately(events[2].NormalizedTime, 0.75f) &&
                        events[2].Type == Data.StageEventType.StrongWaveFlood &&
                        events[2].EnemyStats != null && events[2].Count > 0,
                        "75% strong wave (flood) configured");
                    ok &= Check(Mathf.Approximately(events[3].NormalizedTime, 1f) &&
                        events[3].Type == Data.StageEventType.FinalBoss &&
                        events[3].EnemyStats != null && events[3].EnemyStats.Rank >= 4,
                        "100% final boss event configured with the Queen");
                }

                ok &= Check(stageConfig.GetSpawnRateMultiplier(1f) > stageConfig.GetSpawnRateMultiplier(0f),
                    "Spawn rate curve escalates over the stage");
            }

            // Director wired in scene.
            GameObject directorGo = GameObject.Find("StageDirector");
            ok &= Check(directorGo != null, "StageDirector GameObject exists");
            if (directorGo != null && directorGo.TryGetComponent(out Stage.StageDirector director))
            {
                var so = new SerializedObject(director);
                ok &= Check(so.FindProperty("_config").objectReferenceValue != null, "StageDirector._config wired");
                ok &= Check(so.FindProperty("_spawner").objectReferenceValue != null, "StageDirector._spawner wired");
            }

            // HUD progress bar with fill + one marker per event.
            GameObject barGo = canvasGo != null ? FindChildIncludingInactive(canvasGo.transform, "StageProgressBar") : null;
            ok &= Check(barGo != null, "StageProgressBar exists on HUD");
            if (barGo != null)
            {
                var barUi = barGo.GetComponent<UI.StageProgressBarUI>();
                ok &= Check(barUi != null, "StageProgressBar has StageProgressBarUI");
                if (barUi != null)
                {
                    var so = new SerializedObject(barUi);
                    ok &= Check(so.FindProperty("_fillImage").objectReferenceValue != null &&
                        so.FindProperty("_director").objectReferenceValue != null,
                        "StageProgressBarUI fully wired");
                }

                int markers = 0;
                foreach (Transform child in barGo.transform)
                {
                    if (child.name.StartsWith("Marker"))
                    {
                        markers++;
                        var image = child.GetComponent<UnityEngine.UI.Image>();
                        ok &= Check(image != null && image.sprite != null, $"{child.name} has an icon sprite");
                    }
                }

                ok &= Check(markers == 4, $"StageProgressBar has 4 event markers (found {markers})");
            }

            ok &= ValidatePhase3Bosses(canvasGo);
            ok &= ValidatePhase3DropsAndResults(canvasGo);

            return ok;
        }

        // --- Phase 3C: item drops + run results ---
        private static bool ValidatePhase3DropsAndResults(GameObject canvasGo)
        {
            bool ok = true;

            // Drop prefabs.
            string[] dropNames = { "HoneyJarDrop", "MagnetDrop", "WaxShieldDrop", "RoyalBombDrop" };
            foreach (string dropName in dropNames)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Drops/{dropName}.prefab");
                bool valid = prefab != null && prefab.GetComponent<Pickups.ItemDrop>() != null;
                var renderer = prefab != null ? prefab.GetComponent<SpriteRenderer>() : null;
                valid &= renderer != null && renderer.sprite != null;
                ok &= Check(valid, $"{dropName} prefab exists with ItemDrop + sprite");
            }

            var nuke = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/RoyalNuke.prefab");
            ok &= Check(nuke != null && nuke.GetComponent<View.PooledVfx>() != null,
                "RoyalNuke VFX prefab exists with PooledVfx");

            // Drop pools.
            var bootstrapGo = GameObject.Find("GameBootstrap");
            if (bootstrapGo != null && bootstrapGo.TryGetComponent(out GameBootstrap gb))
            {
                var so = new SerializedObject(gb);
                var pools = so.FindProperty("_pools");
                ok &= Check(HasPoolEntry(pools, PoolIds.HoneyJarDrop) && HasPoolEntry(pools, PoolIds.MagnetDrop) &&
                    HasPoolEntry(pools, PoolIds.WaxShieldDrop) && HasPoolEntry(pools, PoolIds.RoyalBombDrop) &&
                    HasPoolEntry(pools, PoolIds.NukeVfx),
                    "All item drop pools registered");
            }

            // Elites and bosses actually drop items.
            var queensGuard = AssetDatabase.LoadAssetAtPath<Data.EnemyStatsSO>("Assets/Data/Enemies/QueensGuard.asset");
            ok &= Check(queensGuard != null && queensGuard.ItemDropChance > 0f, "QueensGuard has item drop chance");
            var royalGuard = AssetDatabase.LoadAssetAtPath<Data.EnemyStatsSO>("Assets/Data/Enemies/QueensRoyalGuard.asset");
            ok &= Check(royalGuard != null && royalGuard.ItemDropChance >= 1f, "Royal Guard always drops an item");
            var queen = AssetDatabase.LoadAssetAtPath<Data.EnemyStatsSO>("Assets/Data/Enemies/QueenBee.asset");
            ok &= Check(queen != null && queen.ItemDropChance >= 1f, "Queen always drops an item");

            // Phase 3B enemy defenses: layers land on elites+ only — fodder ranks
            // stay clean so early-game hit counts are untouched.
            var workerStats = AssetDatabase.LoadAssetAtPath<Data.EnemyStatsSO>("Assets/Data/Enemies/WorkerBee.asset");
            var warriorStats = AssetDatabase.LoadAssetAtPath<Data.EnemyStatsSO>("Assets/Data/Enemies/WarriorBee.asset");
            ok &= Check(workerStats != null && warriorStats != null &&
                workerStats.ArmorPercent == 0f && workerStats.PhysicalShield == 0f && workerStats.MagicShield == 0f &&
                warriorStats.ArmorPercent == 0f && warriorStats.PhysicalShield == 0f && warriorStats.MagicShield == 0f,
                "Worker/Warrior carry no defense layers");
            ok &= Check(queensGuard != null && queensGuard.MagicShield > 0f && queensGuard.ArmorPercent > 0f,
                "QueensGuard elite has a magic shield + armor");
            ok &= Check(royalGuard != null && royalGuard.PhysicalShield > 0f && royalGuard.ArmorPercent > 0f,
                "Royal Guard miniboss has a physical shield + armor");
            ok &= Check(queen != null && queen.PhysicalShield > 0f && queen.MagicShield > 0f && queen.ArmorPercent > 0f,
                "Queen has both shields + armor");

            // Player shield.
            var player = GameObject.Find("Player");
            if (player != null)
            {
                var shield = player.GetComponent<Player.PlayerShield>();
                ok &= Check(shield != null, "Player has PlayerShield");
                if (shield != null)
                {
                    var so = new SerializedObject(shield);
                    ok &= Check(so.FindProperty("_health").objectReferenceValue != null &&
                        so.FindProperty("_shieldVisual").objectReferenceValue != null,
                        "PlayerShield fully wired");
                }

                Transform ring = player.transform.Find("ShieldRing");
                ok &= Check(ring != null && ring.TryGetComponent(out SpriteRenderer ringRenderer) && !ringRenderer.enabled,
                    "ShieldRing visual exists and starts disabled");
            }

            // Results blocks on both end-of-run panels.
            ok &= ValidateResultsBlock(canvasGo, "GameOverPanel");
            ok &= ValidateResultsBlock(canvasGo, "VictoryPanel");

            // Floating joystick (movement rework): fullscreen touch zone, first
            // sibling (so all other UI wins raycasts), invisible but raycastable,
            // owning the background/handle visuals.
            if (canvasGo != null)
            {
                Transform zone = canvasGo.transform.Find("JoystickTouchZone");
                ok &= Check(zone != null, "JoystickTouchZone exists");
                if (zone != null)
                {
                    ok &= Check(zone.GetSiblingIndex() == 0, "JoystickTouchZone is the first canvas sibling");

                    var zoneImage = zone.GetComponent<UnityEngine.UI.Image>();
                    ok &= Check(zoneImage != null && zoneImage.raycastTarget && zoneImage.color.a == 0f,
                        "JoystickTouchZone is invisible but raycastable");

                    var joystickUi = zone.GetComponent<Input.OnScreenJoystickUI>();
                    ok &= Check(joystickUi != null, "JoystickTouchZone has OnScreenJoystickUI");
                    if (joystickUi != null)
                    {
                        var so = new SerializedObject(joystickUi);
                        ok &= Check(so.FindProperty("_background").objectReferenceValue != null &&
                            so.FindProperty("_handle").objectReferenceValue != null,
                            "OnScreenJoystickUI background/handle wired");
                    }

                    ok &= Check(zone.Find("JoystickBackground") != null,
                        "Joystick visuals are children of the touch zone");
                }
            }

            return ok;
        }

        private static bool ValidateResultsBlock(GameObject canvasGo, string panelName)
        {
            GameObject panel = canvasGo != null ? FindChildIncludingInactive(canvasGo.transform, panelName) : null;
            if (panel == null)
            {
                return Check(false, $"{panelName} exists for results block");
            }

            bool ok = true;
            var results = panel.GetComponent<UI.RunResultsUI>();
            ok &= Check(results != null, $"{panelName} has RunResultsUI");
            if (results != null)
            {
                var so = new SerializedObject(results);
                ok &= Check(so.FindProperty("_session").objectReferenceValue != null &&
                    so.FindProperty("_playerExperience").objectReferenceValue != null &&
                    so.FindProperty("_wallet").objectReferenceValue != null &&
                    so.FindProperty("_statsText").objectReferenceValue != null,
                    $"{panelName} RunResultsUI fully wired");
            }

            return ok;
        }

        // --- Phase 3B: bosses ---
        private static bool ValidatePhase3Bosses(GameObject canvasGo)
        {
            bool ok = true;

            // Boss stats + prefabs.
            var royalGuard = AssetDatabase.LoadAssetAtPath<Data.EnemyStatsSO>("Assets/Data/Enemies/QueensRoyalGuard.asset");
            ok &= Check(royalGuard != null && royalGuard.Rank == 3 && royalGuard.Prefab != null,
                "QueensRoyalGuard stats asset exists (rank 3, prefab wired)");
            var queen = AssetDatabase.LoadAssetAtPath<Data.EnemyStatsSO>("Assets/Data/Enemies/QueenBee.asset");
            ok &= Check(queen != null && queen.Rank == 4 && queen.Prefab != null,
                "QueenBee stats asset exists (rank 4, prefab wired)");

            ok &= ValidateEnemyRigPrefab("Assets/Prefabs/Enemies/QueensRoyalGuard.prefab");
            ok &= ValidateEnemyRigPrefab("Assets/Prefabs/Enemies/QueenBee.prefab");
            ok &= ValidateEnemyStatusReceiver("Assets/Prefabs/Enemies/QueensRoyalGuard.prefab");
            ok &= ValidateEnemyStatusReceiver("Assets/Prefabs/Enemies/QueenBee.prefab");

            var guardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/QueensRoyalGuard.prefab");
            if (guardPrefab != null)
            {
                var charge = guardPrefab.GetComponent<Enemies.ChargeAttack>();
                ok &= Check(charge != null, "Royal Guard has ChargeAttack");
                if (charge != null)
                {
                    var so = new SerializedObject(charge);
                    ok &= Check(so.FindProperty("_enemyController").objectReferenceValue != null &&
                        so.FindProperty("_health").objectReferenceValue != null &&
                        so.FindProperty("_renderer").objectReferenceValue != null &&
                        so.FindProperty("_autoRun").boolValue,
                        "Royal Guard ChargeAttack wired (auto-run)");
                }
            }

            var queenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/QueenBee.prefab");
            if (queenPrefab != null)
            {
                var queenController = queenPrefab.GetComponent<Enemies.QueenBossController>();
                ok &= Check(queenController != null, "Queen has QueenBossController");
                if (queenController != null)
                {
                    var so = new SerializedObject(queenController);
                    ok &= Check(so.FindProperty("_enemyController").objectReferenceValue != null &&
                        so.FindProperty("_health").objectReferenceValue != null &&
                        so.FindProperty("_chargeAttack").objectReferenceValue != null &&
                        so.FindProperty("_renderer").objectReferenceValue != null,
                        "QueenBossController fully wired");
                }

                var queenCharge = queenPrefab.GetComponent<Enemies.ChargeAttack>();
                if (queenCharge != null)
                {
                    var so = new SerializedObject(queenCharge);
                    ok &= Check(!so.FindProperty("_autoRun").boolValue, "Queen ChargeAttack is pattern-driven (no auto-run)");
                }
            }

            // Enemy stinger projectile.
            var stinger = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/EnemyStinger.prefab");
            ok &= Check(stinger != null && stinger.GetComponent<Enemies.EnemyProjectile>() != null,
                "EnemyStinger prefab exists with EnemyProjectile");

            // Pools.
            var bootstrapGo = GameObject.Find("GameBootstrap");
            if (bootstrapGo != null && bootstrapGo.TryGetComponent(out GameBootstrap gb))
            {
                var so = new SerializedObject(gb);
                var pools = so.FindProperty("_pools");
                ok &= Check(HasPoolEntry(pools, PoolIds.QueensRoyalGuard), "Pool: QueensRoyalGuard registered");
                ok &= Check(HasPoolEntry(pools, PoolIds.QueenBee), "Pool: QueenBee registered");
                ok &= Check(HasPoolEntry(pools, PoolIds.EnemyStinger), "Pool: EnemyStinger registered");
            }

            // Boss spawner + HUD pieces.
            GameObject directorGo = GameObject.Find("StageDirector");
            var bossSpawner = directorGo != null ? directorGo.GetComponent<Stage.BossSpawner>() : null;
            ok &= Check(bossSpawner != null, "StageDirector has BossSpawner");
            if (bossSpawner != null)
            {
                var so = new SerializedObject(bossSpawner);
                ok &= Check(so.FindProperty("_director").objectReferenceValue != null &&
                    so.FindProperty("_spawner").objectReferenceValue != null &&
                    so.FindProperty("_bossHealthBar").objectReferenceValue != null &&
                    so.FindProperty("_banner").objectReferenceValue != null &&
                    so.FindProperty("_shaker").objectReferenceValue != null &&
                    so.FindProperty("_summonStats").objectReferenceValue != null &&
                    so.FindProperty("_victoryPanel").objectReferenceValue != null,
                    "BossSpawner fully wired");
            }

            if (canvasGo != null)
            {
                GameObject bossBar = FindChildIncludingInactive(canvasGo.transform, "BossHealthBar");
                ok &= Check(bossBar != null && bossBar.GetComponent<UI.BossHealthBarUI>() != null,
                    "BossHealthBar exists with BossHealthBarUI");

                GameObject banner = FindChildIncludingInactive(canvasGo.transform, "BossBanner");
                ok &= Check(banner != null && banner.GetComponent<UI.BossBannerUI>() != null,
                    "BossBanner exists with BossBannerUI");

                GameObject victory = FindChildIncludingInactive(canvasGo.transform, "VictoryPanel");
                ok &= Check(victory != null && !victory.activeSelf, "VictoryPanel exists and starts inactive");
            }

            return ok;
        }

        // --- PLAN Phase 4 (enemy variety): 4A ranged / 4B bomber / 4C swarm ---
        private static bool ValidateEnemyVariety()
        {
            bool ok = true;

            // 4A — Spitter Bee.
            var spitter = AssetDatabase.LoadAssetAtPath<Data.EnemyStatsSO>("Assets/Data/Enemies/SpitterBee.asset");
            ok &= Check(spitter != null && spitter.Rank == 1 && spitter.Prefab != null
                && spitter.PoolId == PoolIds.SpitterBee,
                "SpitterBee stats asset exists (rank 1, prefab + pool id wired)");
            ok &= Check(spitter != null && spitter.MagicShield > 0f,
                "SpitterBee carries a magic shield (3B/4A interleave)");
            ok &= ValidateVarietyEnemyPrefab<Enemies.RangedAttack>(
                "Assets/Prefabs/Enemies/SpitterBee.prefab", "SpitterBee", "RangedAttack", requireRenderer: true);

            var spitterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/SpitterBee.prefab");
            if (spitterPrefab != null && spitterPrefab.TryGetComponent(out Enemies.RangedAttack ranged))
            {
                var so = new SerializedObject(ranged);
                ok &= Check(so.FindProperty("_projectilePoolId").intValue == PoolIds.EnemyStinger,
                    "SpitterBee RangedAttack fires via the EnemyStinger pool");
            }

            // 4B — Bomber Bee.
            var bomber = AssetDatabase.LoadAssetAtPath<Data.EnemyStatsSO>("Assets/Data/Enemies/BomberBee.asset");
            ok &= Check(bomber != null && bomber.Rank == 1 && bomber.Prefab != null
                && bomber.PoolId == PoolIds.BomberBee,
                "BomberBee stats asset exists (rank 1, prefab + pool id wired)");
            ok &= ValidateVarietyEnemyPrefab<Enemies.BomberAttack>(
                "Assets/Prefabs/Enemies/BomberBee.prefab", "BomberBee", "BomberAttack", requireRenderer: true);

            var bomberPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/BomberBee.prefab");
            if (bomberPrefab != null && bomberPrefab.TryGetComponent(out Enemies.BomberAttack bomberAttack))
            {
                var so = new SerializedObject(bomberAttack);
                ok &= Check(so.FindProperty("_blastVfxPoolId").intValue == PoolIds.BomberBlastVfx,
                    "BomberBee BomberAttack uses the BomberBlast VFX pool");
            }

            var blastVfx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/BomberBlast.prefab");
            ok &= Check(blastVfx != null, "BomberBlast VFX prefab exists");

            // 4C — Swarmling.
            var swarmling = AssetDatabase.LoadAssetAtPath<Data.EnemyStatsSO>("Assets/Data/Enemies/SwarmlingBee.asset");
            ok &= Check(swarmling != null && swarmling.Rank == 0 && swarmling.Prefab != null
                && swarmling.PoolId == PoolIds.SwarmlingBee,
                "SwarmlingBee stats asset exists (rank 0, prefab + pool id wired)");
            ok &= ValidateVarietyEnemyPrefab<Enemies.SwarmMovement>(
                "Assets/Prefabs/Enemies/SwarmlingBee.prefab", "SwarmlingBee", "SwarmMovement", requireRenderer: false);

            // Pools.
            var bootstrapGo = GameObject.Find("GameBootstrap");
            if (bootstrapGo != null && bootstrapGo.TryGetComponent(out GameBootstrap gb))
            {
                var so = new SerializedObject(gb);
                var pools = so.FindProperty("_pools");
                ok &= Check(HasPoolEntry(pools, PoolIds.SpitterBee), "Pool: SpitterBee registered");
                ok &= Check(HasPoolEntry(pools, PoolIds.BomberBee), "Pool: BomberBee registered");
                ok &= Check(HasPoolEntry(pools, PoolIds.SwarmlingBee), "Pool: SwarmlingBee registered");
                ok &= Check(HasPoolEntry(pools, PoolIds.BomberBlastVfx), "Pool: BomberBlastVfx registered");
            }

            // Wave table entries (swarmlings must arrive as a pack).
            ok &= Check(WaveEntryPackSize(spitter) == 1, "Wave config: SpitterBee entry (single)");
            ok &= Check(WaveEntryPackSize(bomber) == 1, "Wave config: BomberBee entry (single)");
            ok &= Check(WaveEntryPackSize(swarmling) > 1, "Wave config: SwarmlingBee entry (pack > 1)");

            return ok;
        }

        // Shared per-prefab checks for the Phase 4 ranks: rig, status receiver,
        // health bar, and the behavior component with its base wiring.
        private static bool ValidateVarietyEnemyPrefab<TBehavior>(
            string prefabPath, string label, string behaviorName, bool requireRenderer)
            where TBehavior : Component
        {
            bool ok = true;
            ok &= ValidateEnemyRigPrefab(prefabPath);
            ok &= ValidateEnemyStatusReceiver(prefabPath);
            ok &= ValidateEnemyHealthBar(prefabPath);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var behavior = prefab != null ? prefab.GetComponent<TBehavior>() : null;
            ok &= Check(behavior != null, $"{label} has {behaviorName}");
            if (behavior != null)
            {
                var so = new SerializedObject(behavior);
                bool wired = so.FindProperty("_enemyController").objectReferenceValue != null
                    && so.FindProperty("_health").objectReferenceValue != null
                    && (!requireRenderer || so.FindProperty("_renderer").objectReferenceValue != null);
                ok &= Check(wired, $"{label} {behaviorName} wired");
            }

            return ok;
        }

        // Pack size of the wave entry referencing the given stats; -1 = no entry.
        private static int WaveEntryPackSize(Data.EnemyStatsSO stats)
        {
            var waveConfig = AssetDatabase.LoadAssetAtPath<Data.WaveSpawnerConfigSO>("Assets/Data/Waves/BeehiveWaveConfig.asset");
            if (waveConfig == null || stats == null)
            {
                return -1;
            }

            var so = new SerializedObject(waveConfig);
            var entries = so.FindProperty("_entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("enemyStats").objectReferenceValue == stats)
                {
                    return Data.WaveSpawnerConfigSO.ClampPackSize(entry.FindPropertyRelative("packSize").intValue);
                }
            }

            return -1;
        }

        private static bool ValidateEnemyStatusReceiver(string prefabPath)
        {
            bool ok = true;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return Check(false, $"{prefabPath} exists (status receiver)");
            }

            var receiver = prefab.GetComponent<Combat.Status.StatusEffectReceiver>();
            ok &= Check(receiver != null, $"{prefabPath} has StatusEffectReceiver");
            if (receiver != null)
            {
                var so = new SerializedObject(receiver);
                ok &= Check(so.FindProperty("_health").objectReferenceValue != null &&
                    so.FindProperty("_renderer").objectReferenceValue != null,
                    $"{prefabPath} StatusEffectReceiver fully wired");
            }

            var enemyController = prefab.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                var so = new SerializedObject(enemyController);
                ok &= Check(so.FindProperty("_statusReceiver").objectReferenceValue != null,
                    $"{prefabPath} EnemyController._statusReceiver wired");
            }

            var contact = prefab.GetComponent<DamageOnContact>();
            if (contact != null)
            {
                var so = new SerializedObject(contact);
                ok &= Check(so.FindProperty("_statusReceiver").objectReferenceValue != null,
                    $"{prefabPath} DamageOnContact._statusReceiver wired");
            }

            return ok;
        }

        private static bool ValidateActiveSkillGrowth(string assetPath)
        {
            var skill = AssetDatabase.LoadAssetAtPath<Data.ActiveSkillSO>(assetPath);
            if (skill == null)
            {
                return Check(false, $"{assetPath} exists");
            }

            bool ok = Check(skill.MaxLevel >= 5, $"{assetPath} has >= 5 levels (found {skill.MaxLevel})");

            bool damageGrows = true;
            bool cooldownShrinks = true;
            for (int level = 2; level <= skill.MaxLevel; level++)
            {
                Data.ActiveSkillLevelStats previous = skill.GetLevelStats(level - 1);
                Data.ActiveSkillLevelStats current = skill.GetLevelStats(level);
                damageGrows &= current.Damage >= previous.Damage;
                cooldownShrinks &= current.Cooldown <= previous.Cooldown;
            }

            ok &= Check(damageGrows && cooldownShrinks, $"{assetPath} growth table is monotonic");
            return ok;
        }

        private static bool ValidateSkillPrefab(string prefabPath, System.Type componentType)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return Check(prefab != null && prefab.GetComponent(componentType) != null,
                $"{prefabPath} exists with {componentType.Name}");
        }

        private static bool HasPoolEntry(SerializedProperty pools, int poolId)
        {
            for (int i = 0; i < pools.arraySize; i++)
            {
                var entry = pools.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("poolId").intValue == poolId &&
                    entry.FindPropertyRelative("prefab").objectReferenceValue != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ValidateBeeRig(GameObject root, string label)
        {
            bool ok = true;

            var animator = root.GetComponent<Animator>();
            ok &= Check(animator != null && animator.runtimeAnimatorController != null, $"{label} has Animator with controller");
            ok &= Check(root.GetComponent<View.CharacterAnimator>() != null, $"{label} has CharacterAnimator");
            ok &= Check(root.GetComponent<View.HitFlash>() != null, $"{label} has HitFlash");

            Transform body = root.transform.Find("Body");
            ok &= Check(body != null, $"{label} has Body rig child");
            if (body != null)
            {
                var renderer = body.GetComponent<SpriteRenderer>();
                ok &= Check(renderer != null && renderer.sprite != null, $"{label} Body has bee sprite");
                ok &= Check(renderer != null && renderer.sharedMaterial != null &&
                    renderer.sharedMaterial.name.Contains("SpriteFlash"), $"{label} Body uses SpriteFlash material");
                ok &= Check(body.GetComponent<UnityEngine.U2D.Animation.SpriteLibrary>() != null, $"{label} Body has SpriteLibrary");
                ok &= Check(body.GetComponent<UnityEngine.U2D.Animation.SpriteResolver>() != null, $"{label} Body has SpriteResolver");
            }

            return ok;
        }

        private static bool ValidateEnemyRigPrefab(string prefabPath)
        {
            bool ok = true;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            ok &= Check(prefab != null, $"{prefabPath} exists");
            if (prefab == null)
            {
                return ok;
            }

            ok &= ValidateBeeRig(prefab, prefabPath);

            var enemyController = prefab.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                var so = new SerializedObject(enemyController);
                ok &= Check(so.FindProperty("_characterAnimator").objectReferenceValue != null,
                    $"{prefabPath} EnemyController._characterAnimator wired");
            }

            var deathAnimation = prefab.GetComponent<View.DeathAnimation>();
            ok &= Check(deathAnimation != null, $"{prefabPath} has DeathAnimation");
            if (deathAnimation != null)
            {
                var so = new SerializedObject(deathAnimation);
                ok &= Check(so.FindProperty("_resolver").objectReferenceValue != null &&
                    so.FindProperty("_animator").objectReferenceValue != null &&
                    so.FindProperty("_collider").objectReferenceValue != null &&
                    so.FindProperty("_rigidbody").objectReferenceValue != null,
                    $"{prefabPath} DeathAnimation fully wired");
            }

            return ok;
        }

        private static bool ValidatePrefabSprite(string prefabPath, string expectedSpriteName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var renderer = prefab != null ? prefab.GetComponent<SpriteRenderer>() : null;
            bool matches = renderer != null && renderer.sprite != null && renderer.sprite.name == expectedSpriteName;
            return Check(matches, $"{prefabPath} uses sprite '{expectedSpriteName}'");
        }

        private static void CountComponentsRecursive<T>(Transform root, ref int count) where T : Component
        {
            if (root.GetComponent<T>() != null)
            {
                count++;
            }

            foreach (Transform child in root)
            {
                CountComponentsRecursive<T>(child, ref count);
            }
        }

        private static bool ValidateEnemyHealthBar(string prefabPath)
        {
            bool ok = true;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            ok &= Check(prefab != null, $"{prefabPath} exists");
            if (prefab == null)
            {
                return ok;
            }

            Transform barTransform = prefab.transform.Find("HealthBarCanvas");
            ok &= Check(barTransform != null, $"{prefabPath} has HealthBarCanvas child");
            if (barTransform == null)
            {
                return ok;
            }

            var healthBarUi = barTransform.GetComponent<UI.EnemyHealthBarUI>();
            ok &= Check(healthBarUi != null, $"{prefabPath} HealthBarCanvas has EnemyHealthBarUI");
            if (healthBarUi != null)
            {
                var so = new SerializedObject(healthBarUi);
                ok &= Check(so.FindProperty("_fillImage").objectReferenceValue != null, $"{prefabPath} EnemyHealthBarUI._fillImage wired");
                ok &= Check(so.FindProperty("_health").objectReferenceValue != null, $"{prefabPath} EnemyHealthBarUI._health wired");
            }

            return ok;
        }

        private static bool ValidateDamageNumberPrefab(string prefabPath)
        {
            bool ok = true;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            ok &= Check(prefab != null, $"{prefabPath} exists");
            if (prefab == null)
            {
                return ok;
            }

            var popup = prefab.GetComponent<UI.DamageNumberPopup>();
            ok &= Check(popup != null, $"{prefabPath} has DamageNumberPopup");
            if (popup != null)
            {
                var so = new SerializedObject(popup);
                ok &= Check(so.FindProperty("_text").objectReferenceValue != null, $"{prefabPath} DamageNumberPopup._text wired");
            }

            return ok;
        }

        private static bool Check(bool condition, string label)
        {
            if (condition)
            {
                Debug.Log($"[PASS] {label}");
            }
            else
            {
                Debug.LogError($"[FAIL] {label}");
            }

            return condition;
        }

        private static int CountMissingScriptsRecursive(GameObject go)
        {
            int count = 0;
            Component[] components = go.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null)
                {
                    count++;
                }
            }

            foreach (Transform child in go.transform)
            {
                count += CountMissingScriptsRecursive(child.gameObject);
            }

            return count;
        }

        private static GameObject FindChildIncludingInactive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }

                GameObject found = FindChildIncludingInactive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
