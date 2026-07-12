using SurveHive.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Rigs the humanoid Hero Bee-Person art (Assets/Sprites/HeroBeePerson/) onto the
    /// Player. Idle/Hit/Death are sourced from the character's south rotation, Run/Attack
    /// from west — two different facings from one approved design (user's own choice of
    /// which angle reads each motion best). The shared Attack.anim/Death.anim clips only
    /// resolve 6 distinct SpriteResolver labels each, so the 9-frame generated attack is
    /// down-sampled to 6 evenly spread frames (keeping the first wind-up and last release
    /// frame exact) and the 7-frame death drops its last frame. Sets
    /// CharacterAnimator._idleUsesMirroredFacing = false on the Player so Idle's
    /// front-facing (south) pose doesn't mirror based on stale movement direction —
    /// Run/Attack's side-facing (west) pose still mirrors normally. Additive, idempotent.
    /// </summary>
    public static class HeroBeePersonSkinBuilder
    {
        private const string Root = "Assets/Sprites/HeroBeePerson";
        private const string LibraryAssetPath = Root + "/HeroBeePersonGenerated.asset";
        private const string ScenePath = "Assets/Scenes/Beehive.unity";

        // Attack.anim's SpriteResolver curve resolves exactly 6 distinct labels ("0".."5").
        // 9 frames were generated (curl -> extend -> release); down-sample to 6, evenly
        // spread, always keeping the first (wind-up start) and last (release) frame exact.
        private static readonly int[] AttackFrameSelection = { 0, 2, 3, 5, 6, 8 };

        [MenuItem("SurveHive/Apply Hero Bee-Person Skin (Player Only)")]
        public static void Apply()
        {
            ImportFrames("idle", 4);
            ImportFrames("hit", 6);
            ImportFrames("death", 7);
            ImportFrames("run", 6);
            ImportFrames("attack", 9);

            BuildLibrary();

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Material flashMaterial = Phase1LookAndFeelBuilder.EnsureFlashMaterial();

            GameObject playerGo = GameObject.Find("Player");
            if (playerGo == null)
            {
                Debug.LogError("HeroBeePersonSkinBuilder: Player GameObject not found in Beehive scene.");
                return;
            }

            Phase1LookAndFeelBuilder.BuildBeeRig(playerGo, flashMaterial, 2, LibraryAssetPath);

            if (playerGo.TryGetComponent(out CharacterAnimator characterAnimator))
            {
                var serialized = new SerializedObject(characterAnimator);
                serialized.FindProperty("_idleUsesMirroredFacing").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log($"SurveHive: Hero Bee-Person skin applied to Player via {LibraryAssetPath}. Enemies untouched.");
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
                    Debug.LogError($"HeroBeePersonSkinBuilder: missing frame {path}.");
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

            for (int label = 0; label < AttackFrameSelection.Length; label++)
            {
                Sprite frame = LoadFrame("attack", AttackFrameSelection[label]);
                library.AddCategoryLabel(frame, "Attack", label.ToString());
            }

            // Death isn't actually played for the player today — PlayerDeathHandler shows a
            // game-over panel instead of DeathAnimation's sprite sequence, which is
            // enemy-only. Populated anyway so the category is ready if that ever changes.
            AddClip(library, "Death", "death", 6);

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
                Debug.LogError($"HeroBeePersonSkinBuilder: could not load sprite at {path}.");
            }

            return sprite;
        }
    }
}
