using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// PLAN 6C — additive, idempotent "magic honey" glow pass. Three parts, all
    /// re-runnable:
    ///   1. Tunes the global <see cref="Bloom"/> override on the URP default volume
    ///      profile to a HIGH threshold, so only deliberately-bright (HDR) pixels
    ///      bloom and the pixel art stays crisp.
    ///   2. Enables post-processing on the Beehive Main Camera — the missing gate:
    ///      the camera had a PixelPerfectCamera but no
    ///      <see cref="UniversalAdditionalCameraData"/>, so bloom rendered nothing.
    ///   3. Pushes a curated set of honey/magic VFX into HDR by brightening their
    ///      renderer/particle colors past 1.0, so they (and only they) cross the
    ///      bloom threshold and glow.
    ///
    /// Idempotency: bloom params are set to fixed values; HDR brightening is guarded
    /// on "already &gt; 1.05" so a second run is a no-op (never re-multiplies).
    /// It does NOT rebuild the scene or regenerate any asset.
    /// </summary>
    public static class BloomGlowBuilder
    {
        private const string ProfilePath = "Assets/Settings/DefaultVolumeProfile.asset";
        private const string BeehiveScenePath = "Assets/Scenes/Beehive.unity";

        // High threshold: base sprites (LDR, <=1) never bloom; only the HDR VFX below do.
        private const float BloomThreshold = 1.0f;
        private const float BloomIntensity = 1.0f;
        private const float BloomScatter = 0.6f;
        private static readonly Color BloomTint = new Color(1f, 0.96f, 0.86f); // warm honey

        // Guard: a color is "already glowing" once any RGB channel clears this.
        private const float HdrGuard = 1.05f;
        private const float SpriteGlowFactor = 1.7f;   // uniform brighten of the sprite's own palette
        private const float ParticleGlowFactor = 1.9f;

        // Curated honey/magic sprite VFX: the auto-firing active-skill projectiles + zones.
        private static readonly string[] SpriteVfxPrefabs =
        {
            "Assets/Prefabs/Skills/EmberBolt.prefab",
            "Assets/Prefabs/Skills/BallLightningOrb.prefab",
            "Assets/Prefabs/Skills/ZapArc.prefab",
            "Assets/Prefabs/Skills/HoneyGlobProjectile.prefab",
            "Assets/Prefabs/Skills/HoneyPuddle.prefab",
            "Assets/Prefabs/Skills/NovaWave.prefab",
            "Assets/Prefabs/Skills/SkillLance.prefab",
            "Assets/Prefabs/Skills/SkillStinger.prefab",
        };

        // Curated honey/magic particle VFX bursts.
        private static readonly string[] ParticleVfxPrefabs =
        {
            "Assets/Prefabs/VFX/HoneySplash.prefab",
            "Assets/Prefabs/VFX/EmberExplosion.prefab",
            "Assets/Prefabs/VFX/RoyalNuke.prefab",
        };

        [MenuItem("SurveHive/Phase 6C — Bloom Glow Pass")]
        public static void Apply()
        {
            TuneBloom();
            BrightenSpriteVfx();
            BrightenParticleVfx();
            EnableBeehivePostProcessing();

            AssetDatabase.SaveAssets();
            Debug.Log("[BloomGlowBuilder] bloom tuned, camera post-processing on, VFX pushed to HDR.");
        }

        // ------------------------------------------------------------------
        // 1. Bloom override on the global default volume profile.
        // ------------------------------------------------------------------
        private static void TuneBloom()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                Debug.LogError($"[BloomGlowBuilder] volume profile not found at {ProfilePath}");
                return;
            }

            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }

            bloom.active = true;
            SetOverride(bloom.threshold, BloomThreshold);
            SetOverride(bloom.intensity, BloomIntensity);
            SetOverride(bloom.scatter, BloomScatter);
            SetOverride(bloom.tint, BloomTint);

            EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(profile);
        }

        private static void SetOverride(ClampedFloatParameter p, float value)
        {
            p.overrideState = true;
            p.value = value;
        }

        private static void SetOverride(MinFloatParameter p, float value)
        {
            p.overrideState = true;
            p.value = value;
        }

        private static void SetOverride(ColorParameter p, Color value)
        {
            p.overrideState = true;
            p.value = value;
        }

        // ------------------------------------------------------------------
        // 2. Enable post-processing on the Beehive Main Camera.
        // ------------------------------------------------------------------
        private static void EnableBeehivePostProcessing()
        {
            EditorSceneManager.OpenScene(BeehiveScenePath, OpenSceneMode.Single);

            GameObject camGo = GameObject.FindWithTag("MainCamera");
            if (camGo == null)
            {
                Debug.LogError("[BloomGlowBuilder] Main Camera not found in Beehive scene.");
                return;
            }

            var cam = camGo.GetComponent<Camera>();
            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            EditorUtility.SetDirty(data);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // ------------------------------------------------------------------
        // 3a. Sprite VFX → HDR (uniform brighten of the sprite's own palette).
        // ------------------------------------------------------------------
        private static void BrightenSpriteVfx()
        {
            foreach (string path in SpriteVfxPrefabs)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;

                foreach (SpriteRenderer sr in contents.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    Color c = sr.color;
                    if (MaxRgb(c) <= HdrGuard)
                    {
                        sr.color = ScaleRgb(c, SpriteGlowFactor);
                        changed = true;
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }

                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // ------------------------------------------------------------------
        // 3b. Particle VFX → HDR (brighten the burst's start color, hue-preserved).
        // ------------------------------------------------------------------
        private static void BrightenParticleVfx()
        {
            foreach (string path in ParticleVfxPrefabs)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;

                foreach (ParticleSystem ps in contents.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ParticleSystem.MainModule main = ps.main;
                    ParticleSystem.MinMaxGradient start = main.startColor;
                    if (GradientMaxRgb(start) <= HdrGuard)
                    {
                        main.startColor = ScaleGradient(start, ParticleGlowFactor);
                        changed = true;
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }

                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // ------------------------------------------------------------------
        // Color helpers.
        // ------------------------------------------------------------------
        private static float MaxRgb(Color c) => Mathf.Max(c.r, Mathf.Max(c.g, c.b));

        private static Color ScaleRgb(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, c.a);

        private static float GradientMaxRgb(ParticleSystem.MinMaxGradient g)
        {
            switch (g.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return MaxRgb(g.color);
                case ParticleSystemGradientMode.TwoColors:
                    return Mathf.Max(MaxRgb(g.colorMin), MaxRgb(g.colorMax));
                case ParticleSystemGradientMode.Gradient:
                    return GradientAssetMaxRgb(g.gradient);
                case ParticleSystemGradientMode.TwoGradients:
                    return Mathf.Max(GradientAssetMaxRgb(g.gradientMin), GradientAssetMaxRgb(g.gradientMax));
                default:
                    return 0f;
            }
        }

        private static ParticleSystem.MinMaxGradient ScaleGradient(ParticleSystem.MinMaxGradient g, float f)
        {
            switch (g.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(ScaleRgb(g.color, f));
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(ScaleRgb(g.colorMin, f), ScaleRgb(g.colorMax, f))
                    {
                        mode = ParticleSystemGradientMode.TwoColors
                    };
                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(ScaleGradientAsset(g.gradient, f));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(
                        ScaleGradientAsset(g.gradientMin, f), ScaleGradientAsset(g.gradientMax, f))
                    {
                        mode = ParticleSystemGradientMode.TwoGradients
                    };
                default:
                    return g;
            }
        }

        private static float GradientAssetMaxRgb(Gradient g)
        {
            if (g == null)
            {
                return 0f;
            }

            float max = 0f;
            GradientColorKey[] keys = g.colorKeys;
            for (int i = 0; i < keys.Length; i++)
            {
                max = Mathf.Max(max, MaxRgb(keys[i].color));
            }

            return max;
        }

        private static Gradient ScaleGradientAsset(Gradient g, float f)
        {
            if (g == null)
            {
                return null;
            }

            GradientColorKey[] keys = g.colorKeys;
            var scaled = new GradientColorKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                scaled[i] = new GradientColorKey(ScaleRgb(keys[i].color, f), keys[i].time);
            }

            var result = new Gradient { mode = g.mode };
            result.SetKeys(scaled, g.alphaKeys);
            return result;
        }
    }
}
