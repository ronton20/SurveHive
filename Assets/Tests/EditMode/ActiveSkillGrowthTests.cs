using NUnit.Framework;
using SurveHive.Data;
using UnityEditor;

namespace SurveHive.Tests
{
    /// <summary>
    /// Validates the authored growth tables of every active skill asset:
    /// 5 levels minimum, damage never regresses, cooldown never grows.
    /// </summary>
    public sealed class ActiveSkillGrowthTests
    {
        private static readonly string[] ActiveSkillPaths =
        {
            "Assets/Data/Skills/Actives/StingerBarrage.asset",
            "Assets/Data/Skills/Actives/PiercingLance.asset",
            "Assets/Data/Skills/Actives/HoneySplash.asset",
            "Assets/Data/Skills/Actives/PollenCloud.asset",
            "Assets/Data/Skills/Actives/StaticWings.asset",
            "Assets/Data/Skills/Actives/EmberSting.asset",
            "Assets/Data/Skills/Actives/FrostNova.asset",
            "Assets/Data/Skills/Actives/BallLightning.asset",
            "Assets/Data/Skills/Actives/HoneyBomb.asset",
        };

        [Test]
        public void AllActiveSkills_ExistWithFiveLevelTables()
        {
            foreach (string path in ActiveSkillPaths)
            {
                var skill = AssetDatabase.LoadAssetAtPath<ActiveSkillSO>(path);
                Assert.IsNotNull(skill, $"Active skill asset missing: {path}");
                Assert.GreaterOrEqual(skill.MaxLevel, 5, $"{path} should have at least 5 levels");
            }
        }

        [Test]
        public void AllActiveSkills_GrowthIsMonotonic()
        {
            foreach (string path in ActiveSkillPaths)
            {
                var skill = AssetDatabase.LoadAssetAtPath<ActiveSkillSO>(path);
                Assert.IsNotNull(skill, $"Active skill asset missing: {path}");

                for (int level = 2; level <= skill.MaxLevel; level++)
                {
                    ActiveSkillLevelStats previous = skill.GetLevelStats(level - 1);
                    ActiveSkillLevelStats current = skill.GetLevelStats(level);

                    Assert.GreaterOrEqual(current.Damage, previous.Damage,
                        $"{path} L{level} damage regressed");
                    Assert.LessOrEqual(current.Cooldown, previous.Cooldown,
                        $"{path} L{level} cooldown grew");
                    Assert.GreaterOrEqual(current.Count, previous.Count,
                        $"{path} L{level} count regressed");
                    Assert.GreaterOrEqual(current.Area, previous.Area,
                        $"{path} L{level} area regressed");
                    Assert.GreaterOrEqual(current.StatusChancePercent, previous.StatusChancePercent,
                        $"{path} L{level} status chance regressed");
                }
            }
        }

        [Test]
        public void GetLevelStats_ClampsOutOfRangeLevels()
        {
            var skill = AssetDatabase.LoadAssetAtPath<ActiveSkillSO>(ActiveSkillPaths[0]);
            Assert.IsNotNull(skill);

            ActiveSkillLevelStats first = skill.GetLevelStats(1);
            ActiveSkillLevelStats belowRange = skill.GetLevelStats(0);
            Assert.AreEqual(first.Damage, belowRange.Damage);

            ActiveSkillLevelStats last = skill.GetLevelStats(skill.MaxLevel);
            ActiveSkillLevelStats aboveRange = skill.GetLevelStats(skill.MaxLevel + 5);
            Assert.AreEqual(last.Damage, aboveRange.Damage);
        }

        [Test]
        public void SkillDatabase_IsPopulated_WithRarityAndLaneCoverage()
        {
            var database = AssetDatabase.LoadAssetAtPath<SkillDatabaseSO>("Assets/Data/Skills/SkillDatabase.asset");
            Assert.IsNotNull(database);
            // Minimum, not exact: the roster grows across Combat 2.0 sub-phases.
            Assert.GreaterOrEqual(database.Skills.Length, 18);

            int common = 0, rare = 0, epic = 0;
            bool hasPassive = false, hasEnhancement = false, hasAbility = false;
            foreach (SkillDefinitionSO skill in database.Skills)
            {
                Assert.IsNotNull(skill, "Database contains a null skill entry");
                switch (skill.Rarity)
                {
                    case Progression.SkillRarity.Common: common++; break;
                    case Progression.SkillRarity.Rare: rare++; break;
                    case Progression.SkillRarity.Epic: epic++; break;
                }

                switch (skill.Lane)
                {
                    case Progression.PowerUpLane.Passive: hasPassive = true; break;
                    case Progression.PowerUpLane.Enhancement: hasEnhancement = true; break;
                    case Progression.PowerUpLane.Ability: hasAbility = true; break;
                }
            }

            Assert.Greater(common, 0, "No common skills");
            Assert.Greater(rare, 0, "No rare skills");
            Assert.Greater(epic, 0, "No epic skills");
            Assert.IsTrue(hasPassive, "No Passive-lane skills");
            Assert.IsTrue(hasEnhancement, "No Enhancement-lane skills");
            Assert.IsTrue(hasAbility, "No Ability-lane skills");
        }
    }
}
