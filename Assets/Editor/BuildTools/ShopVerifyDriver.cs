using SurveHive.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// One-off visual check for the data-driven Hive Upgrades shop: opens
    /// MainMenu in play mode, shows the shop panel, screenshots the spawned grid,
    /// then quits. Run with a GUI (no -batchmode) so the game view renders:
    /// <c>unity.sh drive SurveHive.BuildTools.ShopVerifyDriver.Run</c>.
    /// Screenshot lands in <c>VerifyShots/shop-grid.png</c>.
    /// </summary>
    [InitializeOnLoad]
    public static class ShopVerifyDriver
    {
        private const string ActiveFlag = "SurveHive.ShopVerifyDriver.Active";
        private const string OutputDir = "VerifyShots";

        private static double _playStartTime = -1;
        private static int _stage;

        static ShopVerifyDriver()
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

            // Give the scene a beat to boot, then open the shop; a few seconds
            // later capture (rows spawn on the panel's first activation), then quit.
            if (_stage == 0 && elapsed > 1.5)
            {
                _stage = 1;
                var controller = Object.FindAnyObjectByType<MainMenuController>();
                if (controller == null)
                {
                    Debug.LogError("ShopVerifyDriver: MainMenuController not found.");
                    Finish();
                    return;
                }

                controller.ShowShop();
                Debug.Log("ShopVerifyDriver: shop panel opened.");
            }
            else if (_stage == 1 && elapsed > 3.5)
            {
                _stage = 2;
                string path = System.IO.Path.Combine(OutputDir, "shop-grid.png");
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log($"ShopVerifyDriver: captured {path}");
            }
            else if (_stage == 2 && elapsed > 5.0)
            {
                Finish();
            }
        }

        private static void Finish()
        {
            SessionState.SetBool(ActiveFlag, false);
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }
    }
}
