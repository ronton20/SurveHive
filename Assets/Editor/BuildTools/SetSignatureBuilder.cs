using SurveHive.Data;
using UnityEditor;
using UnityEngine;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// PLAN 2B / TODO #27 — authors the top-tier (4-piece) signature effect onto
    /// each existing elemental set bonus asset. Additive and idempotent: it only
    /// writes a set that still has <see cref="SetSignatureType.None"/>, so the
    /// hand-tuned tiers from 3C and any inspector tuning of the signature numbers
    /// survive re-runs. Does NOT touch tiers, element, or set name.
    /// </summary>
    public static class SetSignatureBuilder
    {
        private const string SetFolder = "Assets/Data/SetBonuses";

        private struct SignatureSpec
        {
            public string AssetFile;
            public SetSignatureType Signature;
            public float Radius;
            public float Potency;
            public float Duration;
            public string Description;
        }

        private static readonly SignatureSpec[] Specs =
        {
            new SignatureSpec
            {
                AssetFile = "FireSet.asset", Signature = SetSignatureType.BurnSpread,
                Radius = 3f, Potency = 0f, Duration = 3f,
                Description = "Burns spread to a nearby enemy on death",
            },
            new SignatureSpec
            {
                AssetFile = "FrostSet.asset", Signature = SetSignatureType.FrostShatter,
                Radius = 2.5f, Potency = 25f, Duration = 0f,
                Description = "Chilled enemies shatter for 25% max HP AoE on death",
            },
            new SignatureSpec
            {
                AssetFile = "ElectricSet.asset", Signature = SetSignatureType.StunChain,
                Radius = 3.5f, Potency = 0f, Duration = 1f,
                Description = "Stun arcs to a nearby enemy on death",
            },
            new SignatureSpec
            {
                AssetFile = "PoisonSet.asset", Signature = SetSignatureType.PoisonPool,
                Radius = 2f, Potency = 8f, Duration = 4f,
                Description = "Poisoned enemies leave a toxic pool on death",
            },
            new SignatureSpec
            {
                AssetFile = "HoneySet.asset", Signature = SetSignatureType.HoneySlick,
                Radius = 2.5f, Potency = 0.5f, Duration = 4f,
                Description = "Slowed enemies leave a sticky slow zone on death",
            },
            new SignatureSpec
            {
                AssetFile = "PhysicalSet.asset", Signature = SetSignatureType.Execute,
                Radius = 0f, Potency = 15f, Duration = 0f,
                Description = "Basic attacks execute enemies below 15% HP",
            },
        };

        [MenuItem("SurveHive/Apply Set Signatures (Phase 2B)")]
        public static void Apply()
        {
            int written = 0;
            for (int i = 0; i < Specs.Length; i++)
            {
                SignatureSpec spec = Specs[i];
                string path = $"{SetFolder}/{spec.AssetFile}";
                var bonus = AssetDatabase.LoadAssetAtPath<SetBonusSO>(path);
                if (bonus == null)
                {
                    Debug.LogError($"SetSignatureBuilder: {path} not found.");
                    continue;
                }

                if (bonus.Signature != SetSignatureType.None)
                {
                    continue;
                }

                bonus.ConfigureSignature(spec.Signature, spec.Radius, spec.Potency, spec.Duration, spec.Description);
                EditorUtility.SetDirty(bonus);
                written++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"SurveHive set signatures (Phase 2B) build complete — wrote {written} set(s).");
        }
    }
}
