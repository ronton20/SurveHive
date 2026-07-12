using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// PLAN 6C visual proof: opens the Beehive run, then spawns exactly the VFX
    /// the bloom pass pushed to HDR — the three magic particle bursts
    /// (EmberExplosion / HoneySplash / RoyalNuke) and a few HDR skill-projectile
    /// sprites — around the player, and captures game-view frames so the glow (and
    /// the still-crisp base art) can be eyeballed. Deterministic: it does not wait
    /// for a fresh run to happen to fire these effects.
    /// Run (no -batchmode, GUI must render):
    /// <c>Unity -projectPath . -executeMethod SurveHive.BuildTools.BloomGlowVerifyDriver.Run</c>
    /// Screenshots land in <c>VerifyShots/</c>.
    /// </summary>
    [InitializeOnLoad]
    public static class BloomGlowVerifyDriver
    {
        private const string ActiveFlag = "SurveHive.BloomGlowDriver.Active";
        private const string OutputDir = "VerifyShots";

        private static readonly string[] ParticleVfx =
        {
            "Assets/Prefabs/VFX/EmberExplosion.prefab",
            "Assets/Prefabs/VFX/HoneySplash.prefab",
            "Assets/Prefabs/VFX/RoyalNuke.prefab",
        };

        private static readonly string[] SpriteVfx =
        {
            "Assets/Prefabs/Skills/EmberBolt.prefab",
            "Assets/Prefabs/Skills/BallLightningOrb.prefab",
            "Assets/Prefabs/Skills/HoneyGlobProjectile.prefab",
            "Assets/Prefabs/Skills/NovaWave.prefab",
        };

        private static double _playStart = -1;
        private static bool _spawned;
        private static int _shots;

        static BloomGlowVerifyDriver()
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

            if (_playStart < 0)
            {
                _playStart = EditorApplication.timeSinceStartup;
                AudioListener.volume = 0f;
            }

            double elapsed = EditorApplication.timeSinceStartup - _playStart;

            // Let the scene finish booting the run before spawning the showcase.
            if (!_spawned && elapsed > 1.5)
            {
                SpawnShowcase();
                _spawned = true;
                return;
            }

            // Two frames a beat apart so the particle bursts are mid-life and the
            // async screenshot writes flush between grabs.
            if (_spawned && _shots == 0 && elapsed > 2.0)
            {
                Capture("6c_glow_1.png");
                _shots = 1;
                return;
            }

            if (_spawned && _shots == 1 && elapsed > 2.6)
            {
                Capture("6c_glow_2.png");
                _shots = 2;
                return;
            }

            if (_shots >= 2 && elapsed > 3.2)
            {
                SessionState.SetBool(ActiveFlag, false);
                EditorApplication.update -= OnEditorUpdate;
                EditorApplication.Exit(0);
            }
        }

        private static void SpawnShowcase()
        {
            Vector3 center = Vector3.zero;
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                center = player.transform.position;
            }

            // Magic particle bursts, spread in a row above the player.
            for (int i = 0; i < ParticleVfx.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ParticleVfx[i]);
                if (prefab == null)
                {
                    continue;
                }

                var pos = center + new Vector3((i - 1) * 2.2f, 1.6f, 0f);
                Object.Instantiate(prefab, pos, Quaternion.identity);
            }

            // HDR skill sprites in a row below the player. Disable their gameplay
            // scripts (movement/lifetime) so they just sit and render the sprite.
            for (int i = 0; i < SpriteVfx.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpriteVfx[i]);
                if (prefab == null)
                {
                    continue;
                }

                var pos = center + new Vector3((i - 1.5f) * 1.6f, -1.6f, 0f);
                GameObject go = Object.Instantiate(prefab, pos, Quaternion.identity);
                go.transform.localScale *= 1.8f;
                foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    mb.enabled = false;
                }
            }
        }

        private static void Capture(string fileName)
        {
            string path = System.IO.Path.Combine(OutputDir, fileName);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"BloomGlowVerifyDriver: captured {path}");
        }
    }
}
