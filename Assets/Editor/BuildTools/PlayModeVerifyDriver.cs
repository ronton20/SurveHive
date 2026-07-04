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
    /// Verification driver: launches the Beehive scene in Play mode, force-equips
    /// the Phase 2 active skills, triggers a level-up offer, and captures game-view
    /// screenshots along the way, then quits the editor. Run from the CLI:
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
            }

            double elapsed = EditorApplication.timeSinceStartup - _playStartTime;

            // At timeScale 0 (level-up pause) the game view stops repainting and
            // ScreenCapture would grab a stale pre-pause framebuffer — keep the
            // player loop ticking so captures reflect the current UI state.
            EditorApplication.QueuePlayerLoopUpdate();

            switch (_stage)
            {
                // Give the run a moment, then equip all six actives.
                case 0 when elapsed > 4.0:
                    EquipAllActiveSkills(1);
                    _stage++;
                    break;

                // Combat with every skill firing at L1.
                case 1 when elapsed > 8.0:
                    Capture("shot1_all_skills_firing.png");
                    _stage++;
                    break;

                // Level everything up for bigger numbers/areas.
                case 2 when elapsed > 12.0:
                    EquipAllActiveSkills(4);
                    _stage++;
                    break;

                case 3 when elapsed > 16.0:
                    Capture("shot2_skills_leveled.png");
                    _stage++;
                    break;

                // Force a level-up so the rarity/lucky card UI shows.
                case 4 when elapsed > 18.0:
                    ForceLevelUp();
                    _stage++;
                    break;

                case 5 when elapsed > 19.5:
                    Capture("shot3_levelup_cards.png");
                    DumpLevelUpPanelText();
                    _stage++;
                    break;

                case 6 when elapsed > 21.0:
                    SessionState.SetBool(ActiveFlag, false);
                    Debug.Log("VerifyDriver: capture complete, exiting.");
                    EditorApplication.Exit(0);
                    break;
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
