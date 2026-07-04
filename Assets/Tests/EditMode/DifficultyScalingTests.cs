using System.Text;
using NUnit.Framework;
using SurveHive.Data;
using SurveHive.Player;
using SurveHive.Progression;
using UnityEditor;
using UnityEngine;

namespace SurveHive.Tests
{
    public sealed class DifficultyScalingTests
    {
        [Test]
        public void HealthMultiplier_IsExactlyOne_DuringFirstMinute()
        {
            var config = ScriptableObject.CreateInstance<WaveSpawnerConfigSO>();

            // Anywhere inside minute 0 a 20 HP worker must die to two 10s.
            Assert.AreEqual(1f, config.HealthMultiplierAt(0f));
            Assert.AreEqual(1f, config.HealthMultiplierAt(30f));
            Assert.AreEqual(1f, config.HealthMultiplierAt(59.9f));

            // Steps up on the minute, not continuously.
            Assert.Greater(config.HealthMultiplierAt(60f), 1f);
            Assert.AreEqual(config.HealthMultiplierAt(60f), config.HealthMultiplierAt(119f));

            Object.DestroyImmediate(config);
        }

        [Test]
        public void StatPreview_ShowsExactBeforeAfterValues()
        {
            var go = new GameObject("StatsProbe");
            try
            {
                var stats = go.AddComponent<PlayerStats>();
                var sb = new StringBuilder();

                // Max HP flat: 100 -> 120.
                var healthSkill = MakeSkill(SkillEffectType.MaxHealthFlat, 20f);
                SkillStatPreview.AppendUpgradeLines(sb, healthSkill, 1, 1, stats, 0);
                StringAssert.Contains("Max HP 100", sb.ToString());
                StringAssert.Contains("120", sb.ToString());

                // Damage percent mirrors PlayerStats rounding (10 * 1.10 -> 11).
                sb.Clear();
                var damageSkill = MakeSkill(SkillEffectType.AttackDamagePercent, 10f);
                SkillStatPreview.AppendUpgradeLines(sb, damageSkill, 1, 1, stats, 0);
                StringAssert.Contains("Basic Attack DMG 10", sb.ToString());
                StringAssert.Contains("11", sb.ToString());

                // Projectile count clamps at the max (default cap 5).
                sb.Clear();
                var projectileSkill = MakeSkill(SkillEffectType.ProjectileCountFlat, 1f);
                SkillStatPreview.AppendUpgradeLines(sb, projectileSkill, 1, 99, stats, 0);
                StringAssert.Contains("Projectiles 1", sb.ToString());
                StringAssert.Contains("5", sb.ToString());

                Object.DestroyImmediate(healthSkill);
                Object.DestroyImmediate(damageSkill);
                Object.DestroyImmediate(projectileSkill);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static SkillDefinitionSO MakeSkill(SkillEffectType effectType, float magnitude)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinitionSO>();
            var serialized = new SerializedObject(skill);
            serialized.FindProperty("_effectType").intValue = (int)effectType;
            serialized.FindProperty("_magnitude").floatValue = magnitude;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return skill;
        }
    }
}
