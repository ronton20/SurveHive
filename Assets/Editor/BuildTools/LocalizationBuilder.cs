using System.Collections.Generic;
using SurveHive.Core;
using SurveHive.Data;
using UnityEditor;
using UnityEngine;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// PLAN 3A — authors the localization <see cref="StringTableSO"/> at
    /// <c>Assets/Resources/StringTable.asset</c> from <see cref="LocDefaults"/>.
    /// Additive and idempotent: it creates the asset + Resources folder if
    /// missing, and only *appends* keys the table doesn't already carry — existing
    /// values (hand-edited or future translations) survive re-runs untouched.
    /// Run via the menu or as an <c>-executeMethod</c> target after adding keys.
    /// </summary>
    public static class LocalizationBuilder
    {
        private const string ResourcesFolder = "Assets/Resources";
        private const string AssetPath = "Assets/Resources/StringTable.asset";

        [MenuItem("SurveHive/Apply Localization Table (Phase 3A)")]
        public static void Apply()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var table = AssetDatabase.LoadAssetAtPath<StringTableSO>(AssetPath);
            bool created = false;
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<StringTableSO>();
                AssetDatabase.CreateAsset(table, AssetPath);
                created = true;
            }

            // Start from the current entries so hand edits/translations are kept,
            // then append any default key the table is still missing.
            var entries = new List<StringTableSO.Entry>();
            var present = new HashSet<string>();
            if (table.Entries != null)
            {
                for (int i = 0; i < table.Entries.Length; i++)
                {
                    StringTableSO.Entry existing = table.Entries[i];
                    entries.Add(existing);
                    if (!string.IsNullOrEmpty(existing.key))
                    {
                        present.Add(existing.key);
                    }
                }
            }

            int appended = 0;
            IReadOnlyList<KeyValuePair<string, string>> defaults = LocDefaults.All;
            for (int i = 0; i < defaults.Count; i++)
            {
                KeyValuePair<string, string> def = defaults[i];
                if (present.Contains(def.Key))
                {
                    continue;
                }

                entries.Add(new StringTableSO.Entry { key = def.Key, value = def.Value });
                present.Add(def.Key);
                appended++;
            }

            table.SetEntries(entries.ToArray());
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"SurveHive localization table (Phase 3A) build complete — " +
                $"{(created ? "created" : "updated")} {AssetPath}, appended {appended} key(s), " +
                $"{entries.Count} total.");
        }
    }
}
