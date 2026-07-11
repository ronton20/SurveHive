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
    /// One-off visual check for character customization (PLAN 5C): opens
    /// MainMenu in play mode, captures the home screen (new STYLE button),
    /// redirects the save to a temp file and grants sandbox jelly, captures the
    /// fresh Hive Style panel, then buys + equips a full look (Ruby tint, Honey
    /// Crown, Golden Stinger), recaptures the panel, and finally loads the
    /// Beehive run to capture the dressed-up hero in play. The real save is
    /// never read from or written to. Run with a GUI (no -batchmode):
    /// <c>unity.sh drive SurveHive.BuildTools.CosmeticsVerifyDriver.Run</c>.
    /// </summary>
    [InitializeOnLoad]
    public static class CosmeticsVerifyDriver
    {
        private const string ActiveFlag = "SurveHive.CosmeticsVerifyDriver.Active";
        private const string OutputDir = "VerifyShots";
        private const string StorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";
        private const string CatalogPath = "Assets/Data/Cosmetics/CosmeticCatalog.asset";

        private static double _playStartTime = -1;
        private static int _stage;

        static CosmeticsVerifyDriver()
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
                // Sandbox the save (deleting any previous run's leftovers so
                // the captured balances are deterministic), grant jelly, open
                // the panel untouched.
                string sandboxPath = System.IO.Path.Combine(
                    Application.temporaryCachePath, "cosmetics_drive_save.json");
                if (System.IO.File.Exists(sandboxPath))
                {
                    System.IO.File.Delete(sandboxPath);
                }

                SaveFileStore.SetPathOverride(sandboxPath);
                var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(StorePath);
                var controller = Object.FindAnyObjectByType<MainMenuController>();
                if (store == null || controller == null)
                {
                    Debug.LogError("CosmeticsVerifyDriver: store or MainMenuController not found.");
                    Finish();
                    return;
                }

                store.BankJelly(50);
                controller.ShowCosmetics();
                Debug.Log("CosmeticsVerifyDriver: Hive Style panel opened with 50 sandbox jelly.");
            }
            else if (_stage == 2 && elapsed > 5.0)
            {
                _stage = 3;
                Capture("style-fresh.png");
            }
            else if (_stage == 3 && elapsed > 6.0)
            {
                _stage = 4;
                // Buy + equip a full look, then re-enter the panel so its
                // OnEnable refresh paints owned/equipped/preview state.
                var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(StorePath);
                var catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalogSO>(CatalogPath);
                var controller = Object.FindAnyObjectByType<MainMenuController>();
                BuyAndEquip(store, catalog, CosmeticSlot.Color, "color_ruby");
                BuyAndEquip(store, catalog, CosmeticSlot.Hat, "hat_crown");
                BuyAndEquip(store, catalog, CosmeticSlot.Stinger, "stinger_barb_sapphire");
                controller.ShowMain();
                controller.ShowCosmetics();
                // The sectioned tab: shape headers + the equipped skin's badge.
                ClickTab((int)CosmeticSlot.Stinger);
                Debug.Log("CosmeticsVerifyDriver: bought + equipped ruby/crown/sapphire barb.");
            }
            else if (_stage == 4 && elapsed > 7.5)
            {
                _stage = 5;
                Capture("style-equipped.png");
            }
            else if (_stage == 5 && elapsed > 8.5)
            {
                _stage = 6;
                UnityEngine.SceneManagement.SceneManager.LoadScene("Beehive");
                Debug.Log("CosmeticsVerifyDriver: loading Beehive with the equipped look.");
            }
            else if (_stage == 6 && elapsed > 12.0)
            {
                _stage = 7;
                Capture("run-hero-cosmetics.png");
            }
            else if (_stage >= 7 && _stage <= 10 && elapsed > 14.0 + ((_stage - 7) * 0.3))
            {
                // Burst of combat shots ~0.3s apart — the auto-attack fires
                // about once a second with a short flight time, so at least one
                // frame should catch the skinned projectile mid-flight.
                Capture($"run-hero-cosmetics-{_stage - 5}.png");
                _stage++;
            }
            else if (_stage == 11 && elapsed > 16.5)
            {
                Finish();
            }
        }

        private static void BuyAndEquip(
            PersistentMetaProgressionStoreSO store, CosmeticCatalogSO catalog, CosmeticSlot slot, string id)
        {
            CosmeticSO cosmetic = catalog != null ? catalog.FindById(id) : null;
            if (cosmetic == null || store == null)
            {
                Debug.LogError($"CosmeticsVerifyDriver: cosmetic '{id}' or store missing.");
                return;
            }

            if (!CosmeticShop.TryPurchase(store, cosmetic) || !CosmeticShop.TryEquip(store, slot, id))
            {
                Debug.LogError($"CosmeticsVerifyDriver: buy/equip failed for '{id}'.");
            }
        }

        private static void ClickTab(int slot)
        {
            GameObject tab = GameObject.Find($"HiveStylePanel/TabColumn/Tab{slot}");
            if (tab == null)
            {
                Debug.LogError($"CosmeticsVerifyDriver: Tab{slot} not found.");
                return;
            }

            tab.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        }

        private static void Capture(string fileName)
        {
            string path = System.IO.Path.Combine(OutputDir, fileName);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"CosmeticsVerifyDriver: captured {path}");
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
