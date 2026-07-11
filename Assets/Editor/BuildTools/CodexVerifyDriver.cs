using SurveHive.Data;
using SurveHive.Persistence;
using SurveHive.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// One-off visual check for the codex (PLAN 5A): opens MainMenu in play
    /// mode, captures the home screen (new CODEX button), redirects the save to
    /// a temp file and pre-unlocks a few entries (so both the silhouette and
    /// discovered renderings show), then captures the Power-Ups and Enemies
    /// tabs. The real save is never read from or written to. Run with a GUI
    /// (no -batchmode): <c>unity.sh drive SurveHive.BuildTools.CodexVerifyDriver.Run</c>.
    /// </summary>
    [InitializeOnLoad]
    public static class CodexVerifyDriver
    {
        private const string ActiveFlag = "SurveHive.CodexVerifyDriver.Active";
        private const string OutputDir = "VerifyShots";
        private const string StorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";

        private static double _playStartTime = -1;
        private static int _stage;

        static CodexVerifyDriver()
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
                AudioListener.volume = 0f;
            }

            double elapsed = EditorApplication.timeSinceStartup - _playStartTime;

            if (_stage == 0 && elapsed > 1.5)
            {
                _stage = 1;
                Capture("menu-home.png");
            }
            else if (_stage == 1 && elapsed > 3.0)
            {
                _stage = 2;
                // Sandbox the save, discover a spread of entries, open the codex.
                SaveFileStore.SetPathOverride(
                    System.IO.Path.Combine(Application.temporaryCachePath, "codex_drive_save.json"));
                var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(StorePath);
                var controller = Object.FindAnyObjectByType<MainMenuController>();
                if (store == null || controller == null)
                {
                    Debug.LogError("CodexVerifyDriver: store or MainMenuController not found.");
                    Finish();
                    return;
                }

                store.UnlockCodexEntries(new System.Collections.Generic.List<string>
                {
                    "skill:swift_wings", "skill:thicker_chitin", "set:Fire",
                    "enemy:WorkerBee", "enemy:QueenBee", "item:HoneyJar", "item:RoyalBomb",
                });
                controller.ShowCodex();
                Debug.Log("CodexVerifyDriver: codex panel opened with sandboxed unlocks.");
            }
            else if (_stage == 2 && elapsed > 5.0)
            {
                _stage = 3;
                Capture("codex-powerups.png");
            }
            else if (_stage == 3 && elapsed > 6.0)
            {
                _stage = 4;
                ClickTab(CodexUI.CategoryEnemies);
            }
            else if (_stage == 4 && elapsed > 7.5)
            {
                _stage = 5;
                Capture("codex-enemies.png");
            }
            else if (_stage == 5 && elapsed > 9.0)
            {
                Finish();
            }
        }

        private static void ClickTab(int category)
        {
            GameObject tab = GameObject.Find($"CodexPanel/TabColumn/Tab{category}");
            if (tab == null)
            {
                Debug.LogError($"CodexVerifyDriver: Tab{category} not found.");
                return;
            }

            tab.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        }

        private static void Capture(string fileName)
        {
            string path = System.IO.Path.Combine(OutputDir, fileName);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"CodexVerifyDriver: captured {path}");
        }

        private static void Finish()
        {
            SaveFileStore.SetPathOverride(null);
            SessionState.SetBool(ActiveFlag, false);
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }
    }
}
