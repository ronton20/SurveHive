using SurveHive.Combat.Skills;
using SurveHive.Combat.Status;
using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Enemies;
using SurveHive.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Verification driver: plays through the current change under test and
    /// captures game-view screenshots, then quits the editor. The staged
    /// switch below is rewritten per verification target — currently the
    /// Phase 4B menu flow: home → shop (with honey banked into a redirected
    /// temp save) → world select → run start → death results with the new
    /// RETRY/HIVE buttons. Run from the CLI:
    /// <c>Unity -projectPath . -executeMethod SurveHive.BuildTools.PlayModeVerifyDriver.Run</c>
    /// (no -batchmode: the game view must render). Screenshots land in
    /// <c>VerifyShots/</c> under the project root.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeVerifyDriver
    {
        private const string ActiveFlag = "SurveHive.VerifyDriver.Active";
        private const string OutputDir = "VerifyShots";

        private static double _playStartTime = -1;
        private static int _stage;

        static PlayModeVerifyDriver()
        {
            if (SessionState.GetBool(ActiveFlag, false))
            {
                EditorApplication.update += OnEditorUpdate;
            }
        }

        public static void Run()
        {
            System.IO.Directory.CreateDirectory(OutputDir);
            SessionState.SetBool(ActiveFlag, true);
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.isPlaying = true;
        }

        private static void OnEditorUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_playStartTime < 0)
            {
                _playStartTime = EditorApplication.timeSinceStartup;
            }

            double elapsed = EditorApplication.timeSinceStartup - _playStartTime;

            // At timeScale 0 (level-up pause) the game view stops repainting and
            // ScreenCapture would grab a stale pre-pause framebuffer — keep the
            // player loop ticking so captures reflect the current UI state.
            EditorApplication.QueuePlayerLoopUpdate();

            switch (_stage)
            {
                // Redirect the save to a temp file BEFORE anything banks or
                // buys — the driver must never touch the real save. Set after
                // play starts so the domain reload can't wipe the override.
                case 0 when elapsed > 1.0:
                    Persistence.SaveFileStore.SetPathOverride(
                        System.IO.Path.Combine(Application.temporaryCachePath, "verify_driver_save.json"));
                    _stage++;
                    break;

                case 1 when elapsed > 2.0:
                    Capture("shot1_menu_home.png");
                    _stage++;
                    break;

                // Bank honey so the shop shows enabled buy buttons, open it.
                case 2 when elapsed > 3.0:
                    BankHoneyAndOpenShop(300);
                    _stage++;
                    break;

                case 3 when elapsed > 4.5:
                    Capture("shot2_menu_shop.png");
                    _stage++;
                    break;

                case 4 when elapsed > 5.5:
                    ShowWorldSelect();
                    _stage++;
                    break;

                case 5 when elapsed > 7.0:
                    Capture("shot3_menu_world_select.png");
                    _stage++;
                    break;

                // Start the run through the real button path.
                case 6 when elapsed > 8.0:
                    ClickWorldSelectBeehive();
                    _stage++;
                    break;

                case 7 when elapsed > 13.0:
                    Capture("shot4_run_started_from_menu.png");
                    _stage++;
                    break;

                // Kill the player: the death results screen must show the new
                // RETRY / HIVE buttons.
                case 8 when elapsed > 14.0:
                    KillPlayer();
                    _stage++;
                    break;

                case 9 when elapsed > 16.0:
                    Capture("shot5_death_results_buttons.png");
                    _stage++;
                    break;

                case 10 when elapsed > 17.5:
                    SessionState.SetBool(ActiveFlag, false);
                    Debug.Log("VerifyDriver: capture complete, exiting.");
                    EditorApplication.Exit(0);
                    break;
            }
        }

        private static UI.MainMenuController FindMenuController()
        {
            var controller = Object.FindAnyObjectByType<UI.MainMenuController>();
            if (controller == null)
            {
                Debug.LogError("VerifyDriver: MainMenuController not found.");
            }

            return controller;
        }

        private static void BankHoneyAndOpenShop(int amount)
        {
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(
                "Assets/Data/Progression/PersistentMetaProgressionStore.asset");
            if (store != null)
            {
                store.BankRunCurrency(amount);
            }

            UI.MainMenuController controller = FindMenuController();
            if (controller != null)
            {
                controller.ShowShop();
            }

            Debug.Log($"VerifyDriver: banked {amount} honey, shop open.");
        }

        private static void ShowWorldSelect()
        {
            UI.MainMenuController controller = FindMenuController();
            if (controller != null)
            {
                controller.ShowMain();
                controller.ShowWorldSelect();
            }
        }

        private static void ClickWorldSelectBeehive()
        {
            UI.MainMenuController controller = FindMenuController();
            if (controller != null)
            {
                controller.StartBeehiveRun();
                Debug.Log("VerifyDriver: Beehive run started from menu.");
            }
        }

        private static void KillPlayer()
        {
            if (Player.PlayerContext.Health != null)
            {
                Player.PlayerContext.Health.TakeDamage(999999f, null);
                Debug.Log("VerifyDriver: player killed for results screen.");
            }
            else
            {
                Debug.LogError("VerifyDriver: no player health to kill.");
            }
        }

        private static void EquipAllActiveSkills(int targetLevel)
        {
            var manager = Object.FindAnyObjectByType<ActiveSkillManager>();
            if (manager == null)
            {
                Debug.LogError("VerifyDriver: no ActiveSkillManager in scene.");
                return;
            }

            string[] paths =
            {
                "Assets/Data/Skills/Actives/StingerBarrage.asset",
                "Assets/Data/Skills/Actives/PiercingLance.asset",
                "Assets/Data/Skills/Actives/HoneySplash.asset",
                "Assets/Data/Skills/Actives/PollenCloud.asset",
                "Assets/Data/Skills/Actives/StaticWings.asset",
                "Assets/Data/Skills/Actives/EmberSting.asset",
            };

            foreach (string path in paths)
            {
                var skill = AssetDatabase.LoadAssetAtPath<ActiveSkillSO>(path);
                while (skill != null && manager.GetLevel(skill) < targetLevel)
                {
                    manager.AddOrLevelUp(skill);
                }
            }

            // Also demonstrate a visible slow on whatever is closest.
            if (EnemyRegistry.Instance != null && EnemyRegistry.Instance.ActiveCount > 0)
            {
                EnemyController enemy = EnemyRegistry.Instance.ActiveEnemies[0];
                if (enemy.StatusReceiver != null)
                {
                    enemy.StatusReceiver.ApplyEffect(StatusEffectType.Slow, 0.5f, 5f);
                }
            }

            Debug.Log($"VerifyDriver: skills equipped to L{targetLevel}.");
        }

        private static void ForceLevelUp()
        {
            var experience = Object.FindAnyObjectByType<PlayerExperience>();
            if (experience != null)
            {
                experience.AddExperience(100000f);
            }
        }

        // Diagnostic: log every TMP under the level-up panel with its layout
        // state so text placement bugs can be pinned to a component.
        private static void DumpLevelUpPanelText()
        {
            GameObject panel = GameObject.Find("LevelUpPanel");
            if (panel == null)
            {
                Debug.LogError("VerifyDriver: LevelUpPanel not found for dump.");
                return;
            }

            var texts = panel.GetComponentsInChildren<TMPro.TMP_Text>(true);
            foreach (TMPro.TMP_Text text in texts)
            {
                var rect = (RectTransform)text.transform;
                Debug.Log(
                    $"VerifyDriver TMPDUMP name={text.gameObject.name} parent={text.transform.parent.name} " +
                    $"type={text.GetType().Name} font={(text.font != null ? text.font.name : "null")} size={text.fontSize} " +
                    $"wrap={text.textWrappingMode} align={text.alignment} worldPos={rect.position} " +
                    $"anchoredPos={rect.anchoredPosition} sizeDelta={rect.sizeDelta} rectSize={rect.rect.size} " +
                    $"text='{text.text.Replace('\n', '|')}'");
            }
        }

        private static void Capture(string fileName)
        {
            string path = System.IO.Path.Combine(OutputDir, fileName);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"VerifyDriver: captured {path}");
        }
    }
}
