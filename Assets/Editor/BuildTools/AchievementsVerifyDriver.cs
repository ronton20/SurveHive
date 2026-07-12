using SurveHive.Data;
using SurveHive.Persistence;
using SurveHive.Progression;
using SurveHive.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// One-off visual check for achievements (PLAN 5D): opens MainMenu in play
    /// mode, captures the home screen (new AWARDS button), redirects the save
    /// to a temp file, captures the fresh all-locked panel, grants the two
    /// clear-based achievements (leaving the kill ones locked), recaptures the
    /// unlocked rows, then loads the Beehive run where the first kill should
    /// pop the First Sting toast — captured in a short burst. The real save is
    /// never read from or written to. Run with a GUI (no -batchmode):
    /// <c>unity.sh drive SurveHive.BuildTools.AchievementsVerifyDriver.Run</c>.
    /// </summary>
    [InitializeOnLoad]
    public static class AchievementsVerifyDriver
    {
        private const string ActiveFlag = "SurveHive.AchievementsVerifyDriver.Active";
        private const string OutputDir = "VerifyShots";
        private const string StorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";
        private const string CatalogPath = "Assets/Data/Achievements/AchievementCatalog.asset";

        private static double _playStartTime = -1;
        private static int _stage;

        static AchievementsVerifyDriver()
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
                Capture("menu-home-awards.png");
            }
            else if (_stage == 1 && elapsed > 3.0)
            {
                _stage = 2;
                // Sandbox the save so the panel starts all-locked and nothing
                // this drive grants touches real progress.
                string sandboxPath = System.IO.Path.Combine(
                    Application.temporaryCachePath, "achievements_drive_save.json");
                if (System.IO.File.Exists(sandboxPath))
                {
                    System.IO.File.Delete(sandboxPath);
                }

                SaveFileStore.SetPathOverride(sandboxPath);
                var controller = Object.FindAnyObjectByType<MainMenuController>();
                if (controller == null)
                {
                    Debug.LogError("AchievementsVerifyDriver: MainMenuController not found.");
                    Finish();
                    return;
                }

                controller.ShowAchievements();
                Debug.Log("AchievementsVerifyDriver: Achievements panel opened on a fresh save.");
            }
            else if (_stage == 2 && elapsed > 4.5)
            {
                _stage = 3;
                Capture("awards-fresh.png");
            }
            else if (_stage == 3 && elapsed > 5.5)
            {
                _stage = 4;
                // Grant only run-end (clear) achievements so the in-run kill
                // toast below still has First Sting to unlock. Apex also pays
                // out the Honey Crown — its row should show the cosmetic line.
                var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(StorePath);
                var controller = Object.FindAnyObjectByType<MainMenuController>();
                Grant(store, "queenslayer");
                Grant(store, "apex_of_the_hive");
                controller.ShowMain();
                controller.ShowAchievements();
                Debug.Log("AchievementsVerifyDriver: granted queenslayer + apex_of_the_hive.");
            }
            else if (_stage == 4 && elapsed > 7.0)
            {
                _stage = 5;
                Capture("awards-unlocked.png");
            }
            else if (_stage == 5 && elapsed > 8.0)
            {
                _stage = 6;
                UnityEngine.SceneManagement.SceneManager.LoadScene("Beehive");
                Debug.Log("AchievementsVerifyDriver: loading Beehive to catch the First Sting toast.");
            }
            else if (_stage >= 6 && _stage <= 11 && elapsed > 13.0 + ((_stage - 6) * 1.5))
            {
                // The first kill usually lands within a few seconds; the toast
                // holds ~3s, so shots every 1.5s across ~9s should catch it.
                Capture($"run-toast-{_stage - 5}.png");
                _stage++;
            }
            else if (_stage == 12 && elapsed > 23.0)
            {
                Finish();
            }
        }

        private static void Grant(PersistentMetaProgressionStoreSO store, string id)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AchievementCatalogSO>(CatalogPath);
            AchievementSO achievement = null;
            if (catalog != null)
            {
                foreach (AchievementSO entry in catalog.Achievements)
                {
                    if (entry != null && entry.AchievementId == id)
                    {
                        achievement = entry;
                        break;
                    }
                }
            }

            if (store == null || achievement == null || !AchievementRules.TryGrant(store, achievement))
            {
                Debug.LogError($"AchievementsVerifyDriver: grant failed for '{id}'.");
            }
        }

        private static void Capture(string fileName)
        {
            string path = System.IO.Path.Combine(OutputDir, fileName);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"AchievementsVerifyDriver: captured {path}");
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
