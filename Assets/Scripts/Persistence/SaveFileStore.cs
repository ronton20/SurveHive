using System;
using System.IO;
using UnityEngine;

namespace SurveHive.Persistence
{
    /// <summary>
    /// File IO for the save: safe-write (temp file, then atomic swap) so a crash
    /// mid-write never leaves a truncated save, and null on any read failure so
    /// callers start fresh. Path is overridable for tests.
    /// </summary>
    public static class SaveFileStore
    {
        private const string FileName = "survehive_save.json";

        private static string _pathOverride;

        /// <summary>
        /// Raised when the save path changes. Cached-state holders (the
        /// persistent store SO) must drop their cache and re-read — otherwise a
        /// test's redirected state would survive into the next consumer.
        /// </summary>
        public static event Action PathChanged;

        public static string SavePath =>
            _pathOverride ?? Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Redirects reads/writes (tests); null restores the default path.</summary>
        public static void SetPathOverride(string path)
        {
            if (_pathOverride == path)
            {
                return;
            }

            _pathOverride = path;
            PathChanged?.Invoke();
        }

        public static SaveData Load()
        {
            try
            {
                string path = SavePath;
                if (!File.Exists(path))
                {
                    return null;
                }

                return SaveDataSerializer.FromJson(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveFileStore: failed to read save, starting fresh. {e.Message}");
                return null;
            }
        }

        public static bool Save(SaveData data)
        {
            if (data == null)
            {
                return false;
            }

            try
            {
                string path = SavePath;
                string tempPath = path + ".tmp";
                File.WriteAllText(tempPath, SaveDataSerializer.ToJson(data));

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(tempPath, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveFileStore: failed to write save. {e.Message}");
                return false;
            }
        }
    }
}
