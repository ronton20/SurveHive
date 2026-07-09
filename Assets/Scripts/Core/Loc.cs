using SurveHive.Data;
using UnityEngine;

namespace SurveHive.Core
{
    /// <summary>
    /// PLAN 3A localization accessor. Resolves a <see cref="LocKeys"/> key to its
    /// display string: the authored <c>StringTable</c> Resources asset wins, then
    /// the code-level <see cref="LocDefaults"/>, then the raw key (visible but
    /// non-crashing if a key is unknown). The table is loaded lazily once and
    /// cached, so <see cref="Get"/> is allocation-free after the first call and
    /// safe to call from any scene (menu or run) without wiring.
    ///
    /// Resolve at bind time and reuse the result — never call per-frame in a hot
    /// loop, though a stray call is still zero-GC (dictionary lookup returning a
    /// cached string).
    /// </summary>
    public static class Loc
    {
        // Loaded from Assets/Resources/StringTable.asset. The asset is optional:
        // the LocDefaults fallback keeps the game readable if it's missing.
        private const string ResourcePath = "StringTable";

        private static StringTableSO _table;
        private static bool _loadAttempted;

        /// <summary>Inject a table directly (tests, or a locale swap at runtime).</summary>
        public static void SetTable(StringTableSO table)
        {
            _table = table;
            _loadAttempted = true;
        }

        /// <summary>Clears cached state so the next <see cref="Get"/> reloads (tests).</summary>
        public static void ResetForTests()
        {
            _table = null;
            _loadAttempted = false;
        }

        public static string Get(string key)
        {
            if (_table == null && !_loadAttempted)
            {
                _loadAttempted = true;
                _table = Resources.Load<StringTableSO>(ResourcePath);
            }

            if (_table != null && _table.TryGet(key, out string value))
            {
                return value;
            }

            if (LocDefaults.TryGet(key, out string fallback))
            {
                return fallback;
            }

            return key;
        }
    }
}
