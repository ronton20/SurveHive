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
    /// Phase 4B/4C enemies: start a run, spawn a swarmling pack (wobbling
    /// cluster) and bombers, and capture the pack closing in, the bomber
    /// fuse pulse, and the AoE blast. Run from the CLI:
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
        private static double _stageStartTime = -1;
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
                _stageStartTime = _playStartTime;
            }

            // Per-stage clock: a slow scene load or shader-compile hitch must
            // not eat the whole budget and fire every remaining stage (and the
            // editor exit) on back-to-back updates — async screenshot writes
            // need real frames between captures to flush.
            double elapsed = EditorApplication.timeSinceStartup - _stageStartTime;

            // At timeScale 0 (level-up pause) the game view stops repainting and
            // ScreenCapture would grab a stale pre-pause framebuffer — keep the
            // player loop ticking so captures reflect the current UI state.
            EditorApplication.QueuePlayerLoopUpdate();

            // Kills during the staged spawns level the player; the offer pause
            // (timeScale 0) would freeze every enemy mid-capture. Click it away
            // like the PlayMode smoke test does. (Only in-run: stage 2+.)
            if (_stage >= 2 && Mathf.Approximately(Time.timeScale, 0f))
            {
                ClickFirstLevelUpChoice();
                return;
            }

            switch (_stage)
            {
                // Redirect the save to a temp file BEFORE anything banks or
                // buys — the driver must never touch the real save. Set after
                // play starts so the domain reload can't wipe the override.
                case 0 when elapsed > 1.0:
                    Persistence.SaveFileStore.SetPathOverride(
                        System.IO.Path.Combine(Application.temporaryCachePath, "verify_driver_save.json"));
                    AdvanceStage();
                    break;

                // Start the run through the real button path.
                case 1 when elapsed > 1.0:
                    ClickWorldSelectBeehive();
                    AdvanceStage();
                    break;

                // Swarmling pack from one side — the wobble should fan the
                // cluster out as it closes.
                case 2 when elapsed > 5.0:
                    SpawnEnemyCluster("Assets/Data/Enemies/SwarmlingBee.asset", 8, 7f);
                    AdvanceStage();
                    break;

                case 3 when elapsed > 1.2:
                    Capture("shot1_swarm_pack_closing.png");
                    AdvanceStage();
                    break;

                // Bombers rush at 3.3 u/s from 5u: fuse (~1s in, 0.55s pulse),
                // blast at ~1.6s.
                case 4 when elapsed > 0.5:
                    SpawnEnemyRing("Assets/Data/Enemies/BomberBee.asset", 3, 5f);
                    AdvanceStage();
                    break;

                // Captures ≥1s apart: the batch-launched game view repaints at
                // a few fps, and a second pending screenshot inside one frame
                // drops the first.
                case 5 when elapsed > 1.2:
                    Capture("shot2_bomber_fuse.png");
                    AdvanceStage();
                    break;

                case 6 when elapsed > 1.0:
                    Capture("shot3_bomber_blast.png");
                    AdvanceStage();
                    break;

                case 7 when elapsed > 1.5:
                    Capture("shot4_aftermath.png");
                    AdvanceStage();
                    break;

                case 8 when elapsed > 2.0:
                    SessionState.SetBool(ActiveFlag, false);
                    Debug.Log("VerifyDriver: capture complete, exiting.");
                    EditorApplication.Exit(0);
                    break;
            }
        }

        private static void AdvanceStage()
        {
            _stage++;
            _stageStartTime = EditorApplication.timeSinceStartup;
        }

        // Dismisses the offer through the real button path (also unpauses).
        private static void ClickFirstLevelUpChoice()
        {
            GameObject panel = GameObject.Find("LevelUpPanel");
            if (panel == null)
            {
                Debug.LogError("VerifyDriver: LevelUpPanel not found to click.");
                return;
            }

            var buttons = panel.GetComponentsInChildren<UnityEngine.UI.Button>(false);
            if (buttons.Length > 0)
            {
                buttons[0].onClick.Invoke();
                Debug.Log("VerifyDriver: clicked first level-up choice.");
            }
        }

        private static void OpenPauseMenu()
        {
            var pause = Object.FindAnyObjectByType<UI.PauseMenuController>();
            if (pause != null)
            {
                pause.Open();
                Debug.Log("VerifyDriver: pause menu opened.");
            }
            else
            {
                Debug.LogError("VerifyDriver: PauseMenuController not found.");
            }
        }

        private static void OpenPauseSettings()
        {
            var pause = Object.FindAnyObjectByType<UI.PauseMenuController>();
            if (pause != null)
            {
                pause.ShowSettings();
                Debug.Log("VerifyDriver: pause settings opened.");
            }
        }

        private static void ClosePauseMenu()
        {
            var pause = Object.FindAnyObjectByType<UI.PauseMenuController>();
            if (pause != null)
            {
                pause.Close();
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

        // Ring-spawns a given rank around the player so its behavior is on
        // screen within seconds.
        private static void SpawnEnemyRing(string statsPath, int count, float radius)
        {
            var spawner = Object.FindAnyObjectByType<Spawning.EnemySpawner>();
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(statsPath);
            if (spawner == null || stats == null)
            {
                Debug.LogError($"VerifyDriver: spawner or stats missing ({statsPath}).");
                return;
            }

            Transform player = spawner.Player;
            float step = 360f / count;
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = Quaternion.Euler(0f, 0f, step * i + 20f) * Vector2.right;
                spawner.SpawnAt(stats, player.position + (Vector3)(direction * radius));
            }

            Debug.Log($"VerifyDriver: spawned {count}x {stats.DisplayName}.");
        }

        // Spawns a tight cluster to the player's right — how the wave table's
        // packSize delivers swarms.
        private static void SpawnEnemyCluster(string statsPath, int count, float distance)
        {
            var spawner = Object.FindAnyObjectByType<Spawning.EnemySpawner>();
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(statsPath);
            if (spawner == null || stats == null)
            {
                Debug.LogError($"VerifyDriver: spawner or stats missing ({statsPath}).");
                return;
            }

            Vector3 center = spawner.Player.position + new Vector3(distance, 0f, 0f);
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Random.insideUnitCircle * 1.2f;
                spawner.SpawnAt(stats, center + offset);
            }

            Debug.Log($"VerifyDriver: spawned cluster of {count}x {stats.DisplayName}.");
        }

        private static void KillPlayer()
        {
            if (Player.PlayerContext.Health != null)
            {
                Player.PlayerContext.Health.TakeDamage(999999f, Health.DamageType.Physical, null);
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
