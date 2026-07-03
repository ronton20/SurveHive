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

                var pic = player.GetComponent<PlayerInputController>();
                ok &= Check(pic != null, "Player has PlayerInputController");
                if (pic != null)
                {
                    var so = new SerializedObject(pic);
                    ok &= Check(so.FindProperty("_actionsAsset").objectReferenceValue != null, "PlayerInputController._actionsAsset wired");
                    ok &= Check(so.FindProperty("_joystickUi").objectReferenceValue != null, "PlayerInputController._joystickUi wired");
                    ok &= Check(so.FindProperty("_worldCamera").objectReferenceValue != null, "PlayerInputController._worldCamera wired");
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
                    ok &= Check(pools.arraySize == 6, $"GameBootstrap._pools has 6 entries (found {pools.arraySize})");
                    bool hasDamageNumberPool = false;
                    for (int i = 0; i < pools.arraySize; i++)
                    {
                        var entry = pools.GetArrayElementAtIndex(i);
                        ok &= Check(entry.FindPropertyRelative("prefab").objectReferenceValue != null, $"Pool entry {i} prefab wired");
                        if (entry.FindPropertyRelative("poolId").intValue == PoolIds.DamageNumber)
                        {
                            hasDamageNumberPool = true;
                        }
                    }
                    ok &= Check(hasDamageNumberPool, "GameBootstrap._pools includes DamageNumber pool");
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
                }
            }

            var canvasGo = GameObject.Find("Canvas");
            ok &= Check(canvasGo != null, "Canvas GameObject exists");

            GameObject levelUpPanel = canvasGo != null ? FindChildIncludingInactive(canvasGo.transform, "LevelUpPanel") : null;
            ok &= Check(levelUpPanel != null, "LevelUpPanel exists");
            ok &= Check(levelUpPanel != null && !levelUpPanel.activeSelf, "LevelUpPanel starts inactive");
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
                }
            }

            var eventSystemGo = GameObject.Find("EventSystem");
            ok &= Check(eventSystemGo != null, "EventSystem GameObject exists");

            ok &= ValidateEnemyHealthBar("Assets/Prefabs/Enemies/WorkerBee.prefab");
            ok &= ValidateEnemyHealthBar("Assets/Prefabs/Enemies/WarriorBee.prefab");
            ok &= ValidateDamageNumberPrefab("Assets/Prefabs/UI/DamageNumber.prefab");

            Debug.Log(ok ? "SurveHive Beehive scene validation PASSED." : "SurveHive Beehive scene validation FAILED - see errors above.");
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
