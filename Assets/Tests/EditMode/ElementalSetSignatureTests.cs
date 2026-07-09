using NUnit.Framework;
using SurveHive.Combat.Skills;
using SurveHive.Data;
using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// PLAN 2B set signatures: the computable pieces — shatter damage, and the
    /// top-tier gating that decides whether a signature (and the Execute
    /// threshold) is active. Target selection / zone spawning is scene-bound and
    /// covered by the play-mode drive.
    /// </summary>
    public sealed class ElementalSetSignatureTests
    {
        private static SetBonusTier[] Tiers()
        {
            return new[]
            {
                new SetBonusTier { PiecesRequired = 2, Description = "t1" },
                new SetBonusTier { PiecesRequired = 3, Description = "t2" },
                new SetBonusTier { PiecesRequired = 4, Description = "t3" },
            };
        }

        private static SetBonusSO MakeSet(
            SkillElement element, SetSignatureType signature, float potency = 0f)
        {
            var so = ScriptableObject.CreateInstance<SetBonusSO>();
            so.Configure(element, element.ToString(), Tiers());
            so.ConfigureSignature(signature, radius: 3f, potency: potency, duration: 3f, description: "sig");
            return so;
        }

        private static int[] Pieces(SkillElement element, int count)
        {
            var pieces = new int[ElementSets.ElementCount];
            pieces[(int)element] = count;
            return pieces;
        }

        [Test]
        public void ShatterDamage_IsPercentOfVictimMaxHealth()
        {
            Assert.AreEqual(250f, ElementalSetSignatures.ShatterDamage(1000f, 25f), 0.001f);
        }

        [Test]
        public void ShatterDamage_ClampsToAtLeastOne()
        {
            Assert.AreEqual(1f, ElementalSetSignatures.ShatterDamage(0f, 25f), 0.001f);
        }

        [Test]
        public void Signature_ActiveOnlyAtTopTier()
        {
            SetBonusSO fire = MakeSet(SkillElement.Fire, SetSignatureType.BurnSpread);
            ElementSets.Initialize(new[] { fire });

            ElementSets.UpdateCounts(Pieces(SkillElement.Fire, 4));
            Assert.IsTrue(ElementSets.IsTopTierActive(SkillElement.Fire));
            Assert.AreEqual(SetSignatureType.BurnSpread, ElementSets.GetSignature(SkillElement.Fire));
        }

        [Test]
        public void Signature_InactiveBelowTopTier()
        {
            SetBonusSO fire = MakeSet(SkillElement.Fire, SetSignatureType.BurnSpread);
            ElementSets.Initialize(new[] { fire });

            ElementSets.UpdateCounts(Pieces(SkillElement.Fire, 3));
            Assert.IsFalse(ElementSets.IsTopTierActive(SkillElement.Fire));
            Assert.AreEqual(SetSignatureType.None, ElementSets.GetSignature(SkillElement.Fire));
        }

        [Test]
        public void ExecuteThreshold_ReflectsPhysicalTopTierSignature()
        {
            SetBonusSO physical = MakeSet(SkillElement.Physical, SetSignatureType.Execute, potency: 15f);
            ElementSets.Initialize(new[] { physical });

            ElementSets.UpdateCounts(Pieces(SkillElement.Physical, 4));
            Assert.AreEqual(0.15f, ElementSets.ExecuteThresholdFraction, 0.0001f);

            ElementSets.UpdateCounts(Pieces(SkillElement.Physical, 3));
            Assert.AreEqual(0f, ElementSets.ExecuteThresholdFraction, 0.0001f);
        }

        [TearDown]
        public void ResetElementSets()
        {
            // Static service: clear our configs so later tests start clean.
            ElementSets.Initialize(null);
        }
    }
}
