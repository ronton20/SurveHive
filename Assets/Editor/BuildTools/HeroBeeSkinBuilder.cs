using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Rigs the PixelLab-generated Hero Bee art (Assets/Sprites/HeroBee/) onto the Player
    /// only, via a dedicated SpriteLibraryAsset. Phase1LookAndFeelBuilder.BuildBeeRig already
    /// takes a libraryPath override, so this just points the Player at a different asset —
    /// enemies keep pointing at the PixelFantasy YellowBee.asset, untouched. Additive pass
    /// over the already-built Beehive scene; idempotent.
    /// </summary>
    public static class HeroBeeSkinBuilder
    {
        private const string Root = "Assets/Sprites/HeroBee";
        private const string LibraryAssetPath = Root + "/HeroBeeGenerated.asset";
        private const string ScenePath = "Assets/Scenes/Beehive.unity";

        // The shared Attack.anim clip resolves 6 distinct labels ("0".."5"), but only 3 real
        // frames were generated (a "lead-jab" template). Pad with a stab-and-retract
        // ping-pong instead of spending more PixelLab generations — reads as a deliberate
        // two-beat stinger jab rather than a stretched 3-frame loop.
        private static readonly int[] AttackFramePadding = { 0, 1, 2, 2, 1, 0 };

        [MenuItem("SurveHive/Apply Hero Bee Skin (Player Only)")]
        public static void Apply()
        {
            ImportFrames("idle", 4);
            ImportFrames("run", 6);
            ImportFrames("attack", 3);
            ImportFrames("die", 7);

            BuildLibrary();

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Material flashMaterial = Phase1LookAndFeelBuilder.EnsureFlashMaterial();

            GameObject playerGo = GameObject.Find("Player");
            if (playerGo == null)
            {
                Debug.LogError("HeroBeeSkinBuilder: Player GameObject not found in Beehive scene.");
                return;
            }

            Phase1LookAndFeelBuilder.BuildBeeRig(playerGo, flashMaterial, 2, LibraryAssetPath);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log($"SurveHive: Hero Bee skin applied to Player via {LibraryAssetPath}. Enemies untouched.");
        }

        private static void ImportFrames(string clip, int count)
        {
            for (int i = 0; i < count; i++)
            {
                string path = $"{Root}/{clip}/{i}.png";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null)
                {
                    Debug.LogError($"HeroBeeSkinBuilder: missing frame {path}.");
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = 16f;
                importer.SaveAndReimport();
            }
        }

        private static void BuildLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(LibraryAssetPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
                AssetDatabase.CreateAsset(library, LibraryAssetPath);
            }

            AddClip(library, "Idle", "idle", 4);
            AddClip(library, "Run", "run", 6);

            for (int label = 0; label < AttackFramePadding.Length; label++)
            {
                Sprite frame = LoadFrame("attack", AttackFramePadding[label]);
                library.AddCategoryLabel(frame, "Attack", label.ToString());
            }

            // Death isn't actually played for the player today — PlayerDeathHandler shows a
            // game-over panel instead of DeathAnimation's sprite sequence, which is
            // enemy-only. Populated anyway so the category is ready if that ever changes.
            // 7 frames were generated for a 6-label category; the 7th is unused.
            AddClip(library, "Death", "die", 6);

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        private static void AddClip(SpriteLibraryAsset library, string category, string folder, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Sprite frame = LoadFrame(folder, i);
                library.AddCategoryLabel(frame, category, i.ToString());
            }
        }

        private static Sprite LoadFrame(string folder, int index)
        {
            string path = $"{Root}/{folder}/{index}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError($"HeroBeeSkinBuilder: could not load sprite at {path}.");
            }

            return sprite;
        }
    }
}
