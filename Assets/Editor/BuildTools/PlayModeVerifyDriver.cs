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
    /// 2A status-effect visual pass: a ring of guards each under a single
    /// status (distinct tints), then stacked pairs (two-tone pulse, captured
    /// twice to show the alternation), then a hit mid-status (hue-shifted
    /// flash instead of pure white).
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
            EditorSceneManager.OpenScene("Assets/Scenes/Beehive.unity", OpenSceneMode.Single);
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
                // The drive runs with a GUI (no -batchmode), so the batch-mode
                // audio mute doesn't apply — silence the capture run explicitly.
                AudioListener.volume = 0f;
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

                // 3C enhanced-options capture: the settings panels now carry
                // the five feedback-layer toggles. Open the pause settings,
                // flip layers off through the real buttons, show the run
                // without them, re-enable live through the store, and finish
                // on the MainMenu settings panel. Captures get a stage of
                // their own — CaptureScreenshot writes at end of frame, so an
                // action in the same stage would leak into the shot.
                case 1 when elapsed > 1.0:
                    OpenPauseMenu();
                    OpenPauseSettings();
                    AdvanceStage();
                    break;

                // Two-column pause settings, everything ON.
                case 2 when elapsed > 0.8:
                    Capture("options1_pause_settings.png");
                    AdvanceStage();
                    break;

                case 3 when elapsed > 0.4:
                    ClickFeedbackToggle("EnemyHpBarsToggle");
                    ClickFeedbackToggle("DamageNumbersToggle");
                    ClickFeedbackToggle("ScreenShakeToggle");
                    AdvanceStage();
                    break;

                // Same panel with three rows reading ": OFF".
                case 4 when elapsed > 0.6:
                    Capture("options2_toggles_off.png");
                    AdvanceStage();
                    break;

                // Auto-attack only (no skill equip) and a wide worker ring so
                // plenty are still alive for the re-enable shot; the level-up
                // offer is disabled so it can't freeze over the HUD captures.
                case 5 when elapsed > 0.3:
                    ClosePauseMenu();
                    DisableLevelUpOffer();
                    SpawnEnemyCluster("Assets/Data/Enemies/WorkerBee.asset", 20, 7f);
                    AdvanceStage();
                    break;

                // Combat with enemy bars + damage numbers gone.
                case 6 when elapsed > 3.0:
                    Capture("options3_hud_layers_off.png");
                    AdvanceStage();
                    break;

                case 7 when elapsed > 0.3:
                    ReenableFeedbackToggles();
                    AdvanceStage();
                    break;

                // The same fight after the live re-enable: bars + numbers back
                // on the already-pooled enemies, no respawn needed.
                case 8 when elapsed > 1.5:
                    Capture("options4_hud_layers_back.png");
                    AdvanceStage();
                    break;

                case 9 when elapsed > 0.5:
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                    AdvanceStage();
                    break;

                case 10 when elapsed > 1.5:
                    ShowMenuSettings();
                    AdvanceStage();
                    break;

                case 11 when elapsed > 0.8:
                    Capture("options5_menu_settings.png");
                    AdvanceStage();
                    break;

                // The other menus with a (now top-left) BACK button.
                case 12 when elapsed > 0.4:
                    ShowWorldSelect();
                    AdvanceStage();
                    break;

                case 13 when elapsed > 0.6:
                    Capture("options6_world_select.png");
                    AdvanceStage();
                    break;

                case 14 when elapsed > 0.3:
                    HoverLockedDifficulty();
                    AdvanceStage();
                    break;

                // The unlock-task tooltip on the shared mouse-following
                // TooltipUI — sized to its text and clamped on screen.
                case 15 when elapsed > 0.6:
                    Capture("options8_difficulty_tooltip.png");
                    AdvanceStage();
                    break;

                // Switching panels tears the dropdown rows down — their
                // OnDisable must hide the tooltip (no explicit Hide here), so
                // a lingering tooltip would show up in the shop capture.
                case 16 when elapsed > 0.3:
                    BankHoneyAndOpenShop(0);
                    AdvanceStage();
                    break;

                case 17 when elapsed > 0.6:
                    Capture("options7_shop.png");
                    AdvanceStage();
                    break;

                case 18 when elapsed > 1.5:
                    SessionState.SetBool(ActiveFlag, false);
                    Debug.Log("VerifyDriver: options capture complete, exiting.");
                    EditorApplication.Exit(0);
                    break;
            }
        }

        // The ring subjects, in spawn order, tracked so stacking/damage hits
        // the same enemies the single statuses went to.
        private static readonly System.Collections.Generic.List<EnemyController> StatusSubjects
            = new System.Collections.Generic.List<EnemyController>(8);

        private static void CollectRingSubjects()
        {
            StatusSubjects.Clear();
            if (EnemyRegistry.Instance == null || PlayerContextTransform() == null)
            {
                return;
            }

            Vector3 player = PlayerContextTransform().position;
            var enemies = EnemyRegistry.Instance.ActiveEnemies;
            for (int i = 0; i < EnemyRegistry.Instance.ActiveCount; i++)
            {
                // The ring sits at 8u; drip spawns start at 11u+.
                if ((enemies[i].transform.position - player).sqrMagnitude < 81f)
                {
                    StatusSubjects.Add(enemies[i]);
                }
            }

            Debug.Log($"VerifyDriver: {StatusSubjects.Count} ring subjects collected.");
        }

        private static Transform PlayerContextTransform()
        {
            return Player.PlayerContext.Transform;
        }

        private static void ApplySingleStatusesToRing()
        {
            CollectRingSubjects();
            ApplyToSubject(0, StatusEffectType.Burn, 4f, 30f);
            ApplyToSubject(1, StatusEffectType.Poison, 4f, 30f);
            ApplyToSubject(2, StatusEffectType.Slow, 0.4f, 30f);
            ApplyToSubject(3, StatusEffectType.Freeze, 50f, 30f);
            ApplyToSubject(4, StatusEffectType.Stun, 1f, 30f);
            // Subject 5 stays clean as the control.
        }

        private static void StackSecondStatusesOnRing()
        {
            ApplyToSubject(0, StatusEffectType.Slow, 0.4f, 30f);   // burn + slow
            ApplyToSubject(1, StatusEffectType.Stun, 1f, 30f);     // stun + poison
            ApplyToSubject(2, StatusEffectType.Poison, 4f, 30f);   // poison + slow
            ApplyToSubject(3, StatusEffectType.Burn, 4f, 30f);     // freeze + burn
        }

        private static void ApplyToSubject(int index, StatusEffectType type, float potency, float duration)
        {
            if (index >= StatusSubjects.Count || StatusSubjects[index] == null)
            {
                Debug.LogError($"VerifyDriver: status subject {index} missing.");
                return;
            }

            EnemyController subject = StatusSubjects[index];
            if (subject.StatusReceiver != null)
            {
                subject.StatusReceiver.ApplyEffect(type, potency, duration);
                Debug.Log(
                    $"VerifyDriver: applied {type} to subject {index} " +
                    $"({(subject.Stats != null ? subject.Stats.DisplayName : subject.name)}) — " +
                    $"active={subject.StatusReceiver.Buffer.IsActive(type)} " +
                    $"remaining={subject.StatusReceiver.Buffer.GetRemaining(type):F1}s");
            }
        }

        private static void DamageStatusSubject(int index)
        {
            if (index >= StatusSubjects.Count || StatusSubjects[index] == null)
            {
                Debug.LogError($"VerifyDriver: status subject {index} missing for damage.");
                return;
            }

            if (StatusSubjects[index].TryGetComponent(out Health.HealthComponent health))
            {
                health.TakeDamage(1f, Health.DamageType.Physical, null);
                Debug.Log("VerifyDriver: damaged status subject for flash capture.");
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

        // A capture run doesn't want the level-up freeze over its HUD shots —
        // kills still bank EXP, the offer just never opens.
        private static void DisableLevelUpOffer()
        {
            var offer = Object.FindAnyObjectByType<UI.LevelUpUIController>(FindObjectsInactive.Include);
            if (offer != null)
            {
                offer.gameObject.SetActive(false);
                Debug.Log("VerifyDriver: level-up offer disabled for capture.");
            }
        }

        // Clicks a 3C feedback toggle through its real Button (panels may be
        // inactive mid-transition, so search includes inactive objects).
        private static void ClickFeedbackToggle(string buttonName)
        {
            UI.FeedbackToggleUI[] toggles = Object.FindObjectsByType<UI.FeedbackToggleUI>(
                FindObjectsInactive.Include);
            foreach (UI.FeedbackToggleUI toggle in toggles)
            {
                if (toggle.gameObject.name == buttonName)
                {
                    toggle.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    Debug.Log($"VerifyDriver: clicked {buttonName}.");
                    return;
                }
            }

            Debug.LogError($"VerifyDriver: toggle '{buttonName}' not found.");
        }

        // Re-enables every feedback layer through the store — the same push
        // path the UI uses, proving the live mid-run re-enable.
        private static void ReenableFeedbackToggles()
        {
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(
                "Assets/Data/Progression/PersistentMetaProgressionStore.asset");
            if (store == null)
            {
                Debug.LogError("VerifyDriver: persistent store not found.");
                return;
            }

            Persistence.SettingsData settings = store.Settings;
            settings.showEnemyHealthBars = true;
            settings.showDamageNumbers = true;
            settings.screenShake = true;
            store.SaveSettings();
            Debug.Log("VerifyDriver: feedback layers re-enabled via the store.");
        }

        // Opens the dropdown list (spawning the real row clones) and sends a
        // genuine pointer-enter to the first locked row — the exact event path
        // a hovering mouse takes, including DifficultyItemHover's lazy parent
        // lookup on the root-instantiated clone.
        private static void HoverLockedDifficulty()
        {
            TMPro.TMP_Dropdown dropdown = FindDifficultyDropdown();
            if (dropdown == null)
            {
                return;
            }

            dropdown.Show();

            string lockedSuffix = Core.Loc.Get(Core.LocKeys.DifficultyLockedSuffix);
            UI.DifficultyItemHover[] rows = Object.FindObjectsByType<UI.DifficultyItemHover>(
                FindObjectsInactive.Exclude);
            foreach (UI.DifficultyItemHover row in rows)
            {
                var label = row.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (label == null || !label.text.EndsWith(lockedSuffix))
                {
                    continue;
                }

                var pointer = new UnityEngine.EventSystems.PointerEventData(
                    UnityEngine.EventSystems.EventSystem.current);
                UnityEngine.EventSystems.ExecuteEvents.Execute(
                    row.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
                Debug.Log($"VerifyDriver: pointer-enter sent to row '{label.text}'.");
                return;
            }

            Debug.LogWarning("VerifyDriver: no locked difficulty row found in the open list.");
        }

        private static void ShowMenuSettings()
        {
            UI.MainMenuController controller = FindMenuController();
            if (controller != null)
            {
                controller.ShowSettings();
                Debug.Log("VerifyDriver: menu settings opened.");
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
