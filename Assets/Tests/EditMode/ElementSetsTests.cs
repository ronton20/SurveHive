using NUnit.Framework;
using SurveHive.Combat.Status;
using SurveHive.Data;
using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// Phase 3C elemental set effects: tier thresholds on SetBonusSO and the
    /// ElementSets service's status routing / multiplier math.
    /// </summary>
    public sealed class ElementSetsTests
    {
        private SetBonusSO _fire;
        private SetBonusSO _frost;
        private SetBonusSO _physical;

        [SetUp]
        public void CreateConfigs()
        {
            _fire = ScriptableObject.CreateInstance<SetBonusSO>();
            _fire.Configure(SkillElement.Fire, "WILDFIRE", new[]
            {
                new SetBonusTier { PiecesRequired = 2, StatusDurationBonusPercent = 30f },
                new SetBonusTier { PiecesRequired = 3, StatusPotencyBonusPercent = 30f, StatusDurationBonusPercent = 30f },
                new SetBonusTier { PiecesRequired = 4, StatusPotencyBonusPercent = 60f, StatusDurationBonusPercent = 60f },
            });

            _frost = ScriptableObject.CreateInstance<SetBonusSO>();
            _frost.Configure(SkillElement.Frost, "DEEP CHILL", new[]
            {
                new SetBonusTier { PiecesRequired = 2, StatusDurationBonusPercent = 50f },
            });

            _physical = ScriptableObject.CreateInstance<SetBonusSO>();
            _physical.Configure(SkillElement.Physical, "SHARP STINGERS", new[]
            {
                new SetBonusTier { PiecesRequired = 2, AttackDamageBonusPercent = 6f },
                new SetBonusTier { PiecesRequired = 3, AttackDamageBonusPercent = 12f },
                new SetBonusTier { PiecesRequired = 4, AttackDamageBonusPercent = 20f },
            });

            ElementSets.Initialize(new[] { _fire, _frost, _physical });
        }

        [TearDown]
        public void ResetService()
        {
            // ElementSets is static run-scoped state — never leak into other tests.
            ElementSets.Initialize(null);
            Object.DestroyImmediate(_fire);
            Object.DestroyImmediate(_frost);
            Object.DestroyImmediate(_physical);
        }

        private static int[] Pieces(SkillElement element, int count)
        {
            var pieces = new int[ElementSets.ElementCount];
            pieces[(int)element] = count;
            return pieces;
        }

        [Test]
        public void GetTierIndex_ThresholdBoundaries()
        {
            Assert.AreEqual(-1, _fire.GetTierIndex(0));
            Assert.AreEqual(-1, _fire.GetTierIndex(1));
            Assert.AreEqual(0, _fire.GetTierIndex(2));
            Assert.AreEqual(1, _fire.GetTierIndex(3));
            Assert.AreEqual(2, _fire.GetTierIndex(4));
            Assert.AreEqual(2, _fire.GetTierIndex(9), "counts past the top tier clamp to it");
        }

        [Test]
        public void NoPieces_AllMultipliersNeutral()
        {
            Assert.AreEqual(1f, ElementSets.GetStatusPotencyMultiplier(StatusEffectType.Burn));
            Assert.AreEqual(1f, ElementSets.GetStatusDurationMultiplier(StatusEffectType.Burn));
            Assert.AreEqual(1f, ElementSets.AttackDamageMultiplier);
            Assert.AreEqual(-1, ElementSets.GetTierIndex(SkillElement.Fire));
        }

        [Test]
        public void FireTiers_ScaleBurnOnly()
        {
            ElementSets.UpdateCounts(Pieces(SkillElement.Fire, 2));
            Assert.AreEqual(1f, ElementSets.GetStatusPotencyMultiplier(StatusEffectType.Burn), 0.0001f);
            Assert.AreEqual(1.3f, ElementSets.GetStatusDurationMultiplier(StatusEffectType.Burn), 0.0001f);
            Assert.AreEqual(1f, ElementSets.GetStatusDurationMultiplier(StatusEffectType.Poison), 0.0001f,
                "fire set must not touch poison");

            ElementSets.UpdateCounts(Pieces(SkillElement.Fire, 3));
            Assert.AreEqual(1.3f, ElementSets.GetStatusPotencyMultiplier(StatusEffectType.Burn), 0.0001f);

            ElementSets.UpdateCounts(Pieces(SkillElement.Fire, 5));
            Assert.AreEqual(1.6f, ElementSets.GetStatusPotencyMultiplier(StatusEffectType.Burn), 0.0001f);
            Assert.AreEqual(1.6f, ElementSets.GetStatusDurationMultiplier(StatusEffectType.Burn), 0.0001f);
        }

        [Test]
        public void FrostSet_CoversFreezeAndCold()
        {
            ElementSets.UpdateCounts(Pieces(SkillElement.Frost, 2));
            Assert.AreEqual(1.5f, ElementSets.GetStatusDurationMultiplier(StatusEffectType.Freeze), 0.0001f);
            Assert.AreEqual(1.5f, ElementSets.GetStatusDurationMultiplier(StatusEffectType.Cold), 0.0001f);
            Assert.AreEqual(1f, ElementSets.GetStatusDurationMultiplier(StatusEffectType.Slow), 0.0001f,
                "slow belongs to honey, not frost");
        }

        [Test]
        public void PhysicalSet_ScalesAttackDamage()
        {
            ElementSets.UpdateCounts(Pieces(SkillElement.Physical, 2));
            Assert.AreEqual(1.06f, ElementSets.AttackDamageMultiplier, 0.0001f);

            ElementSets.UpdateCounts(Pieces(SkillElement.Physical, 4));
            Assert.AreEqual(1.2f, ElementSets.AttackDamageMultiplier, 0.0001f);

            Assert.AreEqual(1f, ElementSets.GetStatusPotencyMultiplier(StatusEffectType.Burn), 0.0001f,
                "physical set must not touch statuses");
        }

        [Test]
        public void OnChanged_FiresOnlyOnRealChanges()
        {
            int fired = 0;
            System.Action handler = () => fired++;
            ElementSets.OnChanged += handler;
            try
            {
                ElementSets.UpdateCounts(Pieces(SkillElement.Fire, 2));
                Assert.AreEqual(1, fired);

                ElementSets.UpdateCounts(Pieces(SkillElement.Fire, 2));
                Assert.AreEqual(1, fired, "identical counts must not re-fire");

                ElementSets.UpdateCounts(Pieces(SkillElement.Fire, 3));
                Assert.AreEqual(2, fired);
            }
            finally
            {
                ElementSets.OnChanged -= handler;
            }
        }

        [Test]
        public void Initialize_ResetsCountsAndConfigs()
        {
            ElementSets.UpdateCounts(Pieces(SkillElement.Fire, 4));
            Assert.AreEqual(2, ElementSets.GetTierIndex(SkillElement.Fire));

            ElementSets.Initialize(new[] { _fire });
            Assert.AreEqual(0, ElementSets.GetPieces(SkillElement.Fire), "re-init clears counts");
            Assert.AreEqual(-1, ElementSets.GetTierIndex(SkillElement.Fire));
            Assert.AreEqual(1f, ElementSets.AttackDamageMultiplier);
        }
    }
}
