using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SurveHive.Core;
using SurveHive.Data;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// PLAN 3A localization seam: the <see cref="Loc"/> resolver's fallback chain,
    /// the <see cref="StringTableSO"/> lookup, and the invariant that every
    /// <see cref="LocKeys"/> const has a <see cref="LocDefaults"/> entry (so a new
    /// key can never ship without its English text).
    /// </summary>
    public sealed class LocalizationTests
    {
        [TearDown]
        public void TearDown()
        {
            Loc.ResetForTests();
        }

        private static IEnumerable<string> AllKeys()
        {
            FieldInfo[] fields = typeof(LocKeys).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (FieldInfo field in fields)
            {
                if (field.IsLiteral && field.FieldType == typeof(string))
                {
                    yield return (string)field.GetRawConstantValue();
                }
            }
        }

        [Test]
        public void EveryKey_HasADefault()
        {
            foreach (string key in AllKeys())
            {
                Assert.IsTrue(LocDefaults.TryGet(key, out string value),
                    $"LocKeys.{key} has no LocDefaults entry");
                Assert.IsNotEmpty(value, $"LocKeys.{key} default is empty");
            }
        }

        [Test]
        public void DefaultKeys_AreUnique()
        {
            var seen = new HashSet<string>();
            foreach (KeyValuePair<string, string> entry in LocDefaults.All)
            {
                Assert.IsTrue(seen.Add(entry.Key), $"Duplicate default key: {entry.Key}");
            }
        }

        [Test]
        public void Get_KnownKey_ReturnsDefaultText_WithoutAnAsset()
        {
            Loc.ResetForTests();
            // No table injected and (in a test) no Resources asset guaranteed — the
            // code-level default must still resolve.
            Assert.AreEqual("LEVEL UP!", Loc.Get(LocKeys.LevelUpTitle));
            Assert.AreEqual("Lv. ", Loc.Get(LocKeys.LevelPrefix));
        }

        [Test]
        public void Get_UnknownKey_ReturnsTheKeyItself()
        {
            Loc.ResetForTests();
            Assert.AreEqual("no.such.key", Loc.Get("no.such.key"));
        }

        [Test]
        public void InjectedTable_OverridesDefaults()
        {
            var table = ScriptableObject.CreateInstance<StringTableSO>();
            table.SetEntries(new[]
            {
                new StringTableSO.Entry { key = LocKeys.LevelUpTitle, value = "NIVEAU !" },
            });
            Loc.SetTable(table);

            // Overridden key comes from the table; a key the table lacks still
            // falls through to the English default.
            Assert.AreEqual("NIVEAU !", Loc.Get(LocKeys.LevelUpTitle));
            Assert.AreEqual("MAX", Loc.Get(LocKeys.Max));

            Object.DestroyImmediate(table);
        }

        [Test]
        public void StringTable_TryGet_MissingKey_IsFalse()
        {
            var table = ScriptableObject.CreateInstance<StringTableSO>();
            table.SetEntries(new[]
            {
                new StringTableSO.Entry { key = "a.b", value = "AB" },
            });

            Assert.IsTrue(table.TryGet("a.b", out string hit));
            Assert.AreEqual("AB", hit);
            Assert.IsFalse(table.TryGet("x.y", out _));

            Object.DestroyImmediate(table);
        }
    }
}
