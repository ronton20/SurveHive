using SurveHive.Core;
using SurveHive.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Phase 5A (PLAN.md §7): audio service. Builds the <see cref="AudioLibrarySO"/>
    /// asset from the CC0 clips in <c>Assets/Audio/</c> (see CREDITS.md) and adds
    /// a scene-scoped <see cref="AudioService"/> (pooled SFX sources + one music
    /// source) to both MainMenu.unity and Beehive.unity, wired to the library and
    /// the persistent settings store. Additive over Phases 0-4; idempotent.
    /// </summary>
    public static class Phase5AudioBuilder
    {
        private const string LibraryFolder = "Assets/Data/Audio";
        private const string LibraryPath = LibraryFolder + "/AudioLibrary.asset";
        private const string SfxFolder = "Assets/Audio/Sfx/";
        private const string MusicFolder = "Assets/Audio/Music/";
        private const string PersistentStorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string BeehiveScenePath = "Assets/Scenes/Beehive.unity";
        private const int SfxSourceCount = 8;

        [MenuItem("SurveHive/Apply Phase 5A Audio Service")]
        public static void Apply()
        {
            ConfigureMusicImportSettings();
            EnsureAudioLibrary();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ApplyToScene(MenuScenePath, MusicId.Menu);
            ApplyToScene(BeehiveScenePath, MusicId.Beehive);

            Debug.Log("SurveHive Phase 5A audio service build complete.");
        }

        // Streamed rather than decompressed-into-memory: music tracks are a few
        // MB decoded, which matters on the mobile-ready target (PLAN.md §7).
        private static void ConfigureMusicImportSettings()
        {
            ConfigureStreaming(MusicFolder + "menu_loop.ogg");
            ConfigureStreaming(MusicFolder + "beehive_loop.mp3");
        }

        private static void ConfigureStreaming(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
            {
                return;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.loadType == AudioClipLoadType.Streaming)
            {
                return;
            }

            settings.loadType = AudioClipLoadType.Streaming;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------
        // Library asset.
        // ------------------------------------------------------------------
        private static void EnsureAudioLibrary()
        {
            if (!AssetDatabase.IsValidFolder(LibraryFolder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Audio");
            }

            var library = AssetDatabase.LoadAssetAtPath<AudioLibrarySO>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<AudioLibrarySO>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            var serialized = new SerializedObject(library);
            SerializedProperty sfx = serialized.FindProperty("_sfx");
            sfx.arraySize = 15;
            // Procedural theme-fitting clips (bee-buzz combat, chiptune positives,
            // distinct skill gestures) generated in-repo — see Assets/Audio/CREDITS.md.
            // Hit/Kill carry a min-interval throttle so an AoE hitting a whole horde
            // reads as a texture rather than a machine-gun wall.
            int i = 0;
            SetSfx(sfx, i++, SfxId.Hit, 0.5f, 0.92f, 1.08f, 0.05f, "hit_00.wav", "hit_01.wav", "hit_02.wav");
            SetSfx(sfx, i++, SfxId.Kill, 0.7f, 0.95f, 1.05f, 0.04f, "kill_00.wav", "kill_01.wav");
            SetSfx(sfx, i++, SfxId.Pickup, 0.5f, 0.95f, 1.12f, 0f, "pickup_00.wav", "pickup_01.wav");
            SetSfx(sfx, i++, SfxId.LevelUp, 0.8f, 1f, 1f, 0f, "levelup_00.wav");
            SetSfx(sfx, i++, SfxId.PlayerHurt, 0.85f, 0.96f, 1.04f, 0f, "playerhurt_00.wav");
            SetSfx(sfx, i++, SfxId.PlayerDeath, 0.9f, 1f, 1f, 0f, "playerdeath_00.wav");
            SetSfx(sfx, i++, SfxId.Victory, 0.85f, 1f, 1f, 0f, "victory_00.wav");
            SetSfx(sfx, i++, SfxId.UIClick, 0.55f, 1f, 1f, 0f, "uiclick_00.wav");
            SetSfx(sfx, i++, SfxId.BossStinger, 0.85f, 0.97f, 1.03f, 0f, "bossstinger_00.wav", "bossstinger_01.wav");
            SetSfx(sfx, i++, SfxId.SkillStingerBarrage, 0.55f, 0.92f, 1.08f, 0f,
                "skillstingerbarrage_00.wav", "skillstingerbarrage_01.wav");
            SetSfx(sfx, i++, SfxId.SkillPiercingLance, 0.6f, 0.95f, 1.05f, 0f,
                "skillpiercinglance_00.wav", "skillpiercinglance_01.wav");
            SetSfx(sfx, i++, SfxId.SkillHoneySplash, 0.6f, 0.95f, 1.08f, 0f,
                "skillhoneysplash_00.wav", "skillhoneysplash_01.wav");
            SetSfx(sfx, i++, SfxId.SkillPollenCloud, 0.5f, 0.95f, 1.05f, 0f,
                "skillpollencloud_00.wav", "skillpollencloud_01.wav");
            SetSfx(sfx, i++, SfxId.SkillStaticWings, 0.6f, 0.95f, 1.08f, 0f,
                "skillstaticwings_00.wav", "skillstaticwings_01.wav");
            SetSfx(sfx, i, SfxId.SkillEmberSting, 0.7f, 0.95f, 1.05f, 0f,
                "skillembersting_00.wav", "skillembersting_01.wav");

            SerializedProperty music = serialized.FindProperty("_music");
            music.arraySize = 2;
            SetMusic(music, 0, MusicId.Menu, 0.5f, MusicFolder + "menu_loop.ogg");
            SetMusic(music, 1, MusicId.Beehive, 0.45f, MusicFolder + "beehive_loop.mp3");

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
        }

        private static void SetSfx(
            SerializedProperty sfxArray, int index, SfxId id, float volume, float pitchMin, float pitchMax,
            float minInterval, params string[] clipFileNames)
        {
            SerializedProperty entry = sfxArray.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").intValue = (int)id;
            entry.FindPropertyRelative("volume").floatValue = volume;
            entry.FindPropertyRelative("pitchMin").floatValue = pitchMin;
            entry.FindPropertyRelative("pitchMax").floatValue = pitchMax;
            entry.FindPropertyRelative("minInterval").floatValue = minInterval;

            SerializedProperty clips = entry.FindPropertyRelative("clips");
            clips.arraySize = clipFileNames.Length;
            for (int j = 0; j < clipFileNames.Length; j++)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxFolder + clipFileNames[j]);
                if (clip == null)
                {
                    Debug.LogError($"Phase5: SFX clip not found: {SfxFolder}{clipFileNames[j]}");
                }

                clips.GetArrayElementAtIndex(j).objectReferenceValue = clip;
            }
        }

        private static void SetMusic(SerializedProperty musicArray, int index, MusicId id, float volume, string clipPath)
        {
            SerializedProperty entry = musicArray.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").intValue = (int)id;
            entry.FindPropertyRelative("volume").floatValue = volume;

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null)
            {
                Debug.LogError($"Phase5: music clip not found: {clipPath}");
            }

            entry.FindPropertyRelative("clip").objectReferenceValue = clip;
        }

        // ------------------------------------------------------------------
        // Scene wiring: one AudioService per scene (additive on the already
        // -built scene file, matching how Phases 1-4 extend Beehive.unity).
        // ------------------------------------------------------------------
        private static void ApplyToScene(string scenePath, MusicId autoPlayMusicId)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject cameraGo = GameObject.FindWithTag("MainCamera");
            if (cameraGo != null && !cameraGo.TryGetComponent(out AudioListener _))
            {
                cameraGo.AddComponent<AudioListener>();
            }

            var library = AssetDatabase.LoadAssetAtPath<AudioLibrarySO>(LibraryPath);
            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);

            GameObject serviceGo = GameObject.Find("AudioService");
            if (serviceGo == null)
            {
                serviceGo = new GameObject("AudioService");
            }

            if (!serviceGo.TryGetComponent(out AudioService service))
            {
                service = serviceGo.AddComponent<AudioService>();
            }

            AudioSource musicSource = FindOrCreateSource(serviceGo.transform, "MusicSource", loop: true);

            var sfxSources = new AudioSource[SfxSourceCount];
            for (int i = 0; i < SfxSourceCount; i++)
            {
                sfxSources[i] = FindOrCreateSource(serviceGo.transform, $"SfxSource{i}", loop: false);
            }

            var serialized = new SerializedObject(service);
            serialized.FindProperty("_library").objectReferenceValue = library;
            serialized.FindProperty("_store").objectReferenceValue = store;
            serialized.FindProperty("_musicSource").objectReferenceValue = musicSource;
            serialized.FindProperty("_autoPlayMusic").boolValue = true;
            serialized.FindProperty("_autoPlayMusicId").intValue = (int)autoPlayMusicId;

            SerializedProperty sfxProp = serialized.FindProperty("_sfxSources");
            sfxProp.arraySize = sfxSources.Length;
            for (int i = 0; i < sfxSources.Length; i++)
            {
                sfxProp.GetArrayElementAtIndex(i).objectReferenceValue = sfxSources[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(serviceGo);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static AudioSource FindOrCreateSource(Transform parent, string name, bool loop)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            go.transform.SetParent(parent, false);

            if (!go.TryGetComponent(out AudioSource source))
            {
                source = go.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
