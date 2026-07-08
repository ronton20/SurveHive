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
    /// 1B/1C playtest fixes: difficulty unlock gating (locked rows, the
    /// rejected-pick tooltip with checked/struck tasks, picking an unlocked
    /// Hard) and the shop's visible scrollbar.
    /// Run from the CLI:
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

                // Simulate progress: Beehive cleared on Normal AND Hard, so
                // Hard is open while Extreme stays locked with one task done.
                case 1 when elapsed > 1.0:
                    GrantStageClear("Beehive", 1);
                    GrantStageClear("Beehive", 2);
                    ShowWorldSelect();
                    AdvanceStage();
                    break;

                // Captures ≥1s apart: the batch-launched game view repaints at
                // a few fps, and a second pending screenshot inside one frame
                // drops the first.
                case 2 when elapsed > 1.2:
                    ShowDifficultyDropdown();
                    AdvanceStage();
                    break;

                case 3 when elapsed > 1.2:
                    Capture("shot1_dropdown_extreme_locked.png");
                    AdvanceStage();
                    break;

                // Picking locked EXTREME bounces + pins the task tooltip
                // ([X] struck Hard clear, [ ] open Garden task).
                case 4 when elapsed > 0.5:
                    SelectDifficulty(3);
                    AdvanceStage();
                    break;

                case 5 when elapsed > 1.2:
                    Capture("shot2_locked_pick_tooltip.png");
                    AdvanceStage();
                    break;

                // Hard is genuinely unlocked — the pick sticks.
                case 6 when elapsed > 0.5:
                    SelectDifficulty(2);
                    AdvanceStage();
                    break;

                case 7 when elapsed > 1.2:
                    Capture("shot3_hard_selected.png");
                    AdvanceStage();
                    break;

                // Shop: the scrollbar is visible and programmatic scrolling
                // still reaches the bottom rows.
                case 8 when elapsed > 0.5:
                    BankHoneyAndOpenShop(3000);
                    AdvanceStage();
                    break;

                case 9 when elapsed > 1.2:
                    Capture("shot4_shop_scrollbar.png");
                    AdvanceStage();
                    break;

                case 10 when elapsed > 0.5:
                    ScrollShopToBottom();
                    AdvanceStage();
                    break;

                case 11 when elapsed > 1.2:
                    Capture("shot5_shop_scrolled.png");
                    AdvanceStage();
                    break;

                case 12 when elapsed > 2.0:
                    SessionState.SetBool(ActiveFlag, false);
                    Debug.Log("VerifyDriver: capture complete, exiting.");
                    EditorApplication.Exit(0);
                    break;
            }
        }

        private static void ScrollShopToBottom()
        {
            GameObject shopScroll = GameObject.Find("ShopScroll");
            if (shopScroll != null && shopScroll.TryGetComponent(out UnityEngine.UI.ScrollRect scroll))
            {
                scroll.verticalNormalizedPosition = 0f;
                Debug.Log("VerifyDriver: shop scrolled to bottom.");
            }
            else
            {
                Debug.LogError("VerifyDriver: ShopScroll not found.");
            }
        }

        private static void GrantStageClear(string stageId, int difficulty)
        {
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(
                "Assets/Data/Progression/PersistentMetaProgressionStore.asset");
            if (store != null)
            {
                store.RecordStageClear(stageId, difficulty);
                Debug.Log($"VerifyDriver: recorded {stageId} clear on tier {difficulty}.");
            }
        }

        private static void GrantRerollRank(int rank)
        {
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(
                "Assets/Data/Progression/PersistentMetaProgressionStore.asset");
            if (store != null)
            {
                store.SetUpgradeRank("meta_rerolls", rank);
                Debug.Log($"VerifyDriver: reroll rank set to {rank}.");
            }
        }

        private static void ClickRerollOnFirstCard()
        {
            GameObject panel = GameObject.Find("LevelUpPanel");
            Transform reroll = panel != null ? panel.transform.Find("Choice0/RerollButton") : null;
            if (reroll != null && reroll.gameObject.activeInHierarchy)
            {
                reroll.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Debug.Log("VerifyDriver: rerolled card 0.");
            }
            else
            {
                Debug.LogError("VerifyDriver: Choice0/RerollButton not found or inactive.");
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

        private static TMPro.TMP_Dropdown FindDifficultyDropdown()
        {
            var dropdown = Object.FindAnyObjectByType<TMPro.TMP_Dropdown>();
            if (dropdown == null)
            {
                Debug.LogError("VerifyDriver: DifficultyDropdown not found.");
            }

            return dropdown;
        }

        private static void ShowDifficultyDropdown()
        {
            TMPro.TMP_Dropdown dropdown = FindDifficultyDropdown();
            if (dropdown != null)
            {
                dropdown.Show();
                Debug.Log("VerifyDriver: difficulty dropdown opened.");
            }
        }

        private static void SelectDifficulty(int index)
        {
            TMPro.TMP_Dropdown dropdown = FindDifficultyDropdown();
            if (dropdown != null)
            {
                dropdown.Hide();
                // Real path: fires onValueChanged → DifficultySelectUI.
                dropdown.value = index;
                Debug.Log($"VerifyDriver: difficulty set to option {index}.");
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
