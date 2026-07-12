using SurveHive.Data;
using SurveHive.Persistence;
using SurveHive.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// One-off visual check for the rotating cosmetics shop (PLAN 5E): opens
    /// MainMenu in play mode, captures the home screen (new DEALS button in the
    /// stack), redirects the save to a temp file and grants sandbox jelly,
    /// opens the Daily Deals panel and captures the three date-seeded deal cards
    /// (icons, struck-through list price → deal price, countdown), then buys the
    /// first deal and recaptures so its card reads SOLD. The real save is never
    /// touched. Run with a GUI (no -batchmode):
    /// <c>unity.sh drive SurveHive.BuildTools.DailyDealsVerifyDriver.Run</c>.
    /// </summary>
    [InitializeOnLoad]
    public static class DailyDealsVerifyDriver
    {
        private const string ActiveFlag = "SurveHive.DailyDealsVerifyDriver.Active";
        private const string OutputDir = "VerifyShots";
        private const string StorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";

        private static double _playStartTime = -1;
        private static int _stage;

        static DailyDealsVerifyDriver()
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
                Capture("deals-menu-home.png");
            }
            else if (_stage == 1 && elapsed > 3.0)
            {
                _stage = 2;
                // Sandbox the save (deleting any leftovers so the captured
                // balance + deals are deterministic), grant jelly, open the
                // panel untouched.
                string sandboxPath = System.IO.Path.Combine(
                    Application.temporaryCachePath, "deals_drive_save.json");
                if (System.IO.File.Exists(sandboxPath))
                {
                    System.IO.File.Delete(sandboxPath);
                }

                SaveFileStore.SetPathOverride(sandboxPath);
                var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(StorePath);
                var controller = Object.FindAnyObjectByType<MainMenuController>();
                if (store == null || controller == null)
                {
                    Debug.LogError("DailyDealsVerifyDriver: store or MainMenuController not found.");
                    Finish();
                    return;
                }

                store.BankJelly(50);
                controller.ShowDeals();
                Debug.Log("DailyDealsVerifyDriver: Daily Deals panel opened with 50 sandbox jelly.");
            }
            else if (_stage == 2 && elapsed > 5.0)
            {
                _stage = 3;
                Capture("deals-fresh.png");
            }
            else if (_stage == 3 && elapsed > 6.0)
            {
                _stage = 4;
                // Buy the first deal, then re-enter the panel so its OnEnable
                // refresh paints the SOLD card + reduced balance.
                GameObject buy = GameObject.Find("DailyDealsPanel/DealCard0/BuyButton");
                var controller = Object.FindAnyObjectByType<MainMenuController>();
                if (buy != null)
                {
                    buy.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                }

                if (controller != null)
                {
                    controller.ShowMain();
                    controller.ShowDeals();
                }

                Debug.Log("DailyDealsVerifyDriver: bought the first deal.");
            }
            else if (_stage == 4 && elapsed > 7.5)
            {
                _stage = 5;
                Capture("deals-after-buy.png");
            }
            else if (_stage == 5 && elapsed > 8.5)
            {
                Finish();
            }
        }

        private static void Capture(string fileName)
        {
            string path = System.IO.Path.Combine(OutputDir, fileName);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"DailyDealsVerifyDriver: captured {path}");
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
