using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurveHive.Data
{
    /// <summary>
    /// PLAN 3A localization seam — a flat key→string table for user-facing UI
    /// chrome. The <c>LocalizationBuilder</c> authors one asset under
    /// <c>Assets/Resources/StringTable.asset</c> (from <c>LocDefaults</c>);
    /// <c>Loc</c> loads it once and resolves keys against it, falling back to the
    /// code defaults. Translation is deferred: this asset is where a future locale
    /// pass swaps the English values without touching call sites.
    ///
    /// Zero-GC: the dictionary is built once on first access; lookups return the
    /// cached string reference with no allocation, so callers can resolve at bind
    /// time and reuse the result.
    /// </summary>
    [CreateAssetMenu(fileName = "StringTable", menuName = "SurveHive/String Table")]
    public sealed class StringTableSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string key;
            [TextArea]
            public string value;
        }

        [SerializeField] private Entry[] _entries;

        private Dictionary<string, string> _lookup;

        public Entry[] Entries => _entries;

        /// <summary>Editor/builder authoring hook — replaces the table contents.</summary>
        public void SetEntries(Entry[] entries)
        {
            _entries = entries;
            _lookup = null;
        }

        public bool TryGet(string key, out string value)
        {
            if (_lookup == null)
            {
                Build();
            }

            return _lookup.TryGetValue(key, out value);
        }

        private void Build()
        {
            int count = _entries != null ? _entries.Length : 0;
            _lookup = new Dictionary<string, string>(count, StringComparer.Ordinal);
            for (int i = 0; i < count; i++)
            {
                Entry entry = _entries[i];
                if (!string.IsNullOrEmpty(entry.key))
                {
                    // Last write wins; a duplicate key is a data error caught by the validator.
                    _lookup[entry.key] = entry.value;
                }
            }
        }
    }
}
