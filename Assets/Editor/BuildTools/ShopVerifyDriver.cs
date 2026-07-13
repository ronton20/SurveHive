using SurveHive.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// One-off visual check for the Hive Upgrades menus: opens MainMenu in play
    /// mode, screenshots the home screen, then the shop panel, then quits. Run with
    /// a GUI (no -batchmode) so the game view renders:
    /// <c>unity.sh drive SurveHive.BuildTools.ShopVerifyDriver.Run</c>.
    /// Screenshots land in <c>VerifyShots/menu-home.png</c> + <c>shop-grid.png</c>.
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

            // Give the scene a beat to boot, capture the home screen, then open the
            // shop; a few seconds later capture it (rows spawn on the panel's first
            // activation), then quit.
            if (_stage == 0 && elapsed > 1.5)
            {
                _stage = 1;
                Capture("menu-home.png");
            }
            else if (_stage == 1 && elapsed > 3.0)
            {
                _stage = 2;
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
            else if (_stage == 2 && elapsed > 5.0)
            {
                _stage = 3;
                Capture("shop-grid.png");
            }
            else if (_stage == 3 && elapsed > 6.0)
            {
                // Select an unmaxed upgrade so the BUY button shows its price
                // (glyph + cost) instead of MAX.
                _stage = 4;
                SelectUnmaxedIcon();
            }
            else if (_stage == 4 && elapsed > 7.5)
            {
                _stage = 5;
                Capture("shop-detail-price.png");
            }
            else if (_stage == 5 && elapsed > 9.0)
            {
                Finish();
            }
        }

        private static void SelectUnmaxedIcon()
        {
            var icons = Object.FindObjectsByType<MetaShopIconUI>(FindObjectsInactive.Exclude);
            var persistentStore = UnityEditor.AssetDatabase.LoadAssetAtPath<SurveHive.Data.PersistentMetaProgressionStoreSO>(
                "Assets/Data/Progression/PersistentMetaProgressionStore.asset");
            for (int i = 0; i < icons.Length; i++)
            {
                bool unmaxed = icons[i].Upgrade != null && persistentStore != null
                    && persistentStore.GetUpgradeRank(icons[i].Upgrade.UpgradeId) < icons[i].Upgrade.MaxRank;
                if (icons[i].isActiveAndEnabled && unmaxed)
                {
                    icons[i].Button.onClick.Invoke();
                    Debug.Log($"ShopVerifyDriver: selected unmaxed upgrade '{icons[i].Upgrade.name}'.");
                    return;
                }
            }

            Debug.Log("ShopVerifyDriver: no unmaxed upgrade icon visible.");
        }

        private static void Capture(string fileName)
        {
            string path = System.IO.Path.Combine(OutputDir, fileName);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"ShopVerifyDriver: captured {path}");
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
