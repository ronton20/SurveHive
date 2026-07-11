using System.Text;
using NUnit.Framework;
using SurveHive.Data;
using SurveHive.Progression;
using UnityEditor;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// Codex per-level breakdown (playtest follow-up 2026-07-11): the menu-safe
    /// formatter that lists what each level of a power-up grants.
    /// </summary>
    public sealed class CodexSkillLevelsTests
    {
        private static SkillDefinitionSO CreateSkill(
            SkillEffectType effect, int maxLevel, float magnitude, float[] perLevel = null,
            ActiveSkillSO active = null)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinitionSO>();
            var so = new SerializedObject(skill);
            so.FindProperty("_id").stringValue = "test_skill";
            so.FindProperty("_displayName").stringValue = "Test Skill";
            so.FindProperty("_effectType").enumValueIndex = (int)effect;
            so.FindProperty("_maxLevel").intValue = maxLevel;
            so.FindProperty("_magnitude").floatValue = magnitude;
            so.FindProperty("_activeSkill").objectReferenceValue = active;

            SerializedProperty table = so.FindProperty("_magnitudePerLevel");
            table.arraySize = perLevel != null ? perLevel.Length : 0;
            for (int i = 0; perLevel != null && i < perLevel.Length; i++)
            {
                table.GetArrayElementAtIndex(i).floatValue = perLevel[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return skill;
        }

        private static string Format(SkillDefinitionSO skill)
        {
            var sb = new StringBuilder(256);
            CodexSkillLevels.AppendLevels(sb, skill);
            return sb.ToString();
        }

        [Test]
        public void PassiveWithMagnitudeTable_ListsPerLevelIncrements()
        {
            SkillDefinitionSO skill = CreateSkill(
                SkillEffectType.CritChanceFlat, 5, 5f, new[] { 5f, 5f, 5f, 5f, 10f });

            string text = Format(skill);

            StringAssert.Contains("Lv 1 — +5% Crit Chance", text);
            StringAssert.Contains("Lv 5 — +10% Crit Chance", text);
            Assert.AreEqual(5, CountLines(text));
        }

        [Test]
        public void CooldownEffects_ReadAsReductions()
        {
            SkillDefinitionSO skill = CreateSkill(SkillEffectType.ActiveCooldownPercent, 3, 8f);

            string text = Format(skill);

            StringAssert.Contains("Lv 1 — -8% Skill Cooldowns", text);
        }

        [Test]
        public void FlatEffects_OmitThePercentSign()
        {
            SkillDefinitionSO skill = CreateSkill(SkillEffectType.MaxHealthFlat, 2, 25f);

            string text = Format(skill);

            StringAssert.Contains("Lv 1 — +25 Max HP", text);
            StringAssert.DoesNotContain("%", text);
        }

        [Test]
        public void UncappedSkill_CollapsesToOnePerLevelLine()
        {
            SkillDefinitionSO skill = CreateSkill(SkillEffectType.MoveSpeedPercent, 0, 6f);

            string text = Format(skill);

            StringAssert.Contains("Per level — +6% Move Speed", text);
            Assert.AreEqual(1, CountLines(text));
        }

        [Test]
        public void PierceSkill_ShowsCountAndAllAtMax()
        {
            SkillDefinitionSO skill = CreateSkill(SkillEffectType.BasicAttackPierceFlat, 4, 1f);

            string text = Format(skill);

            StringAssert.Contains("Lv 1 — Pierce ", text);
            StringAssert.Contains("Pierce ALL", text);
        }

        [Test]
        public void ActiveSkill_ListsGrowthTableStats()
        {
            var active = ScriptableObject.CreateInstance<ActiveSkillSO>();
            var so = new SerializedObject(active);
            so.FindProperty("_appliesStatus").boolValue = true;
            so.FindProperty("_statusType").enumValueIndex =
                (int)Combat.Status.StatusEffectType.Stun;

            SerializedProperty levels = so.FindProperty("_levels");
            levels.arraySize = 2;
            SetLevel(levels.GetArrayElementAtIndex(0), 10f, 6f, 3, 0f, 20f);
            SetLevel(levels.GetArrayElementAtIndex(1), 14f, 5f, 4, 0f, 30f);
            so.ApplyModifiedPropertiesWithoutUndo();

            SkillDefinitionSO skill = CreateSkill(SkillEffectType.ActiveSkill, 0, 0f, null, active);

            string text = Format(skill);

            StringAssert.Contains("Lv 1 — DMG 10 · x3 · CD 6s · Stun 20%", text);
            StringAssert.Contains("Lv 2 — DMG 14 · x4 · CD 5s · Stun 30%", text);
            Assert.AreEqual(2, CountLines(text));

            Object.DestroyImmediate(active);
        }

        private static void SetLevel(
            SerializedProperty level, float damage, float cooldown, int count, float area, float statusChance)
        {
            level.FindPropertyRelative("Damage").floatValue = damage;
            level.FindPropertyRelative("Cooldown").floatValue = cooldown;
            level.FindPropertyRelative("Count").intValue = count;
            level.FindPropertyRelative("Area").floatValue = area;
            level.FindPropertyRelative("StatusChancePercent").floatValue = statusChance;
        }

        private static int CountLines(string text)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    count++;
                }
            }

            return count;
        }
    }
}
