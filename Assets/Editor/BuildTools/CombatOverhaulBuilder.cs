using SurveHive.Combat.Skills;
using SurveHive.Combat.Status;
using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Progression;
using SurveHive.Stage;
using SurveHive.UI;
using SurveHive.View;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Combat System Overhaul 2.0 (PLAN.md Phase 1) editor passes. Additive over
    /// Phases 0-5 and idempotent — safe to re-run. Sub-phase 1A adds the lane
    /// banner + element gem to each level-up choice card and wires them to
    /// <see cref="LevelUpUIController"/> (which colors/labels them at runtime from
    /// the picked skill's lane/element).
    /// </summary>
    public static class CombatOverhaulBuilder
    {
        private const string BeehiveScenePath = "Assets/Scenes/Beehive.unity";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";
        private const string SkillsFolder = "Assets/Data/Skills";
        private const string ActivesFolder = SkillsFolder + "/Actives";
        private const string DatabasePath = SkillsFolder + "/SkillDatabase.asset";
        private const int ChoiceCount = 3;

        // One row of an active skill's per-level growth table.
        private struct LevelRow
        {
            public float Damage;
            public float Cooldown;
            public int Count;
            public float Area;
            public float StatusChance;
            public float StatusDuration;
        }

        [MenuItem("SurveHive/Combat 2.0/1A - Power-Up Card Banners")]
        public static void ApplyCardBanners()
        {
            EditorSceneManager.OpenScene(BeehiveScenePath, OpenSceneMode.Single);

            GameObject canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                Debug.LogError("Combat 2.0 1A: Canvas not found in Beehive.unity.");
                return;
            }

            Transform panel = canvasGo.transform.Find("LevelUpPanel");
            if (panel == null)
            {
                Debug.LogError("Combat 2.0 1A: LevelUpPanel not found under Canvas.");
                return;
            }

            var controller = panel.GetComponent<LevelUpUIController>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

            var banners = new TMP_Text[ChoiceCount];
            var bannerBackgrounds = new Image[ChoiceCount];
            var elementGems = new Image[ChoiceCount];
            var laneCounters = new TMP_Text[ChoiceCount];

            for (int i = 0; i < ChoiceCount; i++)
            {
                Transform choice = panel.Find($"Choice{i}");
                if (choice == null)
                {
                    Debug.LogError($"Combat 2.0 1A: Choice{i} not found — run the Phase 1 look pass first.");
                    return;
                }

                BuildCard(choice, font, out banners[i], out bannerBackgrounds[i], out elementGems[i], out laneCounters[i]);
            }

            var serialized = new SerializedObject(controller);
            WireArray(serialized, "_choiceBanners", banners);
            WireArray(serialized, "_choiceBannerBackgrounds", bannerBackgrounds);
            WireArray(serialized, "_choiceElementGems", elementGems);
            WireArray(serialized, "_choiceLaneCounters", laneCounters);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Combat 2.0 1A: power-up card banners built + wired.");
        }

        // Header ribbon (lane) across the card top + a small element gem top-right.
        // Runtime colors/labels come from LevelUpUIController; placeholders here.
        private static void BuildCard(
            Transform choice, TMP_FontAsset font,
            out TMP_Text banner, out Image bannerBackground, out Image elementGem, out TMP_Text laneCounter)
        {
            // Lane banner background: top-stretched ribbon.
            GameObject bannerGo = FindOrCreateChild(choice, "Banner");
            bannerBackground = EnsureImage(bannerGo);
            bannerBackground.color = new Color(0.29f, 0.48f, 0.71f);
            bannerBackground.raycastTarget = false;
            var bannerRect = (RectTransform)bannerGo.transform;
            bannerRect.anchorMin = new Vector2(0f, 1f);
            bannerRect.anchorMax = new Vector2(1f, 1f);
            bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.anchoredPosition = Vector2.zero;
            bannerRect.sizeDelta = new Vector2(0f, 30f);

            // Lane label, centered in the ribbon.
            GameObject labelGo = FindOrCreateChild(bannerGo.transform, "BannerLabel");
            banner = EnsureTmp(labelGo, font, 16f, Color.white, TextAlignmentOptions.Center);
            banner.text = "PASSIVE";
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            // Element gem: small square, top-right corner.
            GameObject gemGo = FindOrCreateChild(choice, "ElementGem");
            elementGem = EnsureImage(gemGo);
            elementGem.color = new Color(0.82f, 0.82f, 0.78f);
            elementGem.raycastTarget = false;
            var gemRect = (RectTransform)gemGo.transform;
            gemRect.anchorMin = new Vector2(1f, 1f);
            gemRect.anchorMax = new Vector2(1f, 1f);
            gemRect.pivot = new Vector2(1f, 1f);
            gemRect.anchoredPosition = new Vector2(-8f, -38f);
            gemRect.sizeDelta = new Vector2(22f, 22f);

            // Lane owned/cap counter, tucked under the banner ribbon (top-left).
            GameObject counterGo = FindOrCreateChild(choice, "LaneCounter");
            laneCounter = EnsureTmp(counterGo, font, 14f, new Color(0.29f, 0.2f, 0.09f), TextAlignmentOptions.Left);
            laneCounter.text = "0/5";
            var counterRect = (RectTransform)counterGo.transform;
            counterRect.anchorMin = new Vector2(0f, 1f);
            counterRect.anchorMax = new Vector2(0f, 1f);
            counterRect.pivot = new Vector2(0f, 1f);
            counterRect.anchoredPosition = new Vector2(10f, -32f);
            counterRect.sizeDelta = new Vector2(60f, 18f);
        }

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Image EnsureImage(GameObject go)
        {
            return go.TryGetComponent(out Image image) ? image : go.AddComponent<Image>();
        }

        private static TMP_Text EnsureTmp(
            GameObject go, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
        {
            if (!go.TryGetComponent(out TextMeshProUGUI tmp))
            {
                tmp = go.AddComponent<TextMeshProUGUI>();
            }

            if (font != null)
            {
                tmp.font = font;
            }

            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            // Normal wrapping (single-word labels won't actually wrap) so these
            // texts satisfy the level-up card wrapping check in the validator.
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void WireArray(SerializedObject serialized, string propertyName, Object[] values)
        {
            SerializedProperty prop = serialized.FindProperty(propertyName);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        // ------------------------------------------------------------------
        // 1C: two new Passive-lane stats — Armor (damage-taken reduction) and
        // Ability Power (scales active-skill damage). Creates the skill assets
        // and registers them in the SkillDatabase. Idempotent.
        // ------------------------------------------------------------------
        [MenuItem("SurveHive/Combat 2.0/1C - Add Armor & Ability Power Passives")]
        public static void AddPassives()
        {
            SkillDefinitionSO armor = EnsureSkill(
                SkillsFolder + "/PassiveArmor.asset", "PassiveArmor", "Waxen Plating",
                "Reduce all damage taken.", SkillEffectType.ArmorPercent,
                PowerUpLane.Passive, SkillElement.Physical, 5f, 8, SkillRarity.Common,
                "Icon_PictoIcon_Defense");

            SkillDefinitionSO abilityPower = EnsureSkill(
                SkillsFolder + "/AbilityPower.asset", "AbilityPower", "Royal Focus",
                "Increase all ability damage.", SkillEffectType.AbilityPowerPercent,
                PowerUpLane.Passive, SkillElement.Physical, 12f, 8, SkillRarity.Rare,
                "Icon_PictoIcon_Energy");

            RegisterInDatabase(armor, abilityPower);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Combat 2.0 1C: Armor + Ability Power passives created and registered.");
        }

        private static SkillDefinitionSO EnsureSkill(
            string assetPath, string id, string displayName, string description,
            SkillEffectType effect, PowerUpLane lane, SkillElement element,
            float magnitude, int maxLevel, SkillRarity rarity, string iconName,
            ActiveSkillSO activeSkill = null)
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(assetPath);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillDefinitionSO>();
                AssetDatabase.CreateAsset(skill, assetPath);
            }

            var so = new SerializedObject(skill);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_effectType").enumValueIndex = (int)effect;
            so.FindProperty("_lane").enumValueIndex = (int)lane;
            so.FindProperty("_element").enumValueIndex = (int)element;
            so.FindProperty("_magnitude").floatValue = magnitude;
            so.FindProperty("_weight").floatValue = 1f;
            so.FindProperty("_maxLevel").intValue = maxLevel;
            so.FindProperty("_rarity").enumValueIndex = (int)rarity;
            so.FindProperty("_icon").objectReferenceValue = FindIcon(iconName);
            so.FindProperty("_activeSkill").objectReferenceValue = activeSkill;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void RegisterInDatabase(params SkillDefinitionSO[] skills)
        {
            var db = AssetDatabase.LoadAssetAtPath<SkillDatabaseSO>(DatabasePath);
            if (db == null)
            {
                Debug.LogError($"Combat 2.0 1C: SkillDatabase not found at {DatabasePath}.");
                return;
            }

            var so = new SerializedObject(db);
            SerializedProperty arr = so.FindProperty("_skills");

            foreach (SkillDefinitionSO skill in skills)
            {
                if (ContainsReference(arr, skill))
                {
                    continue;
                }

                arr.arraySize++;
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = skill;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(db);
        }

        // ------------------------------------------------------------------
        // 1D: Enhancement-lane basic-attack modifiers. Adds Piercing Shot (pierce,
        // less damage) and Ignite (on-hit burn), and retires the Piercing Lance
        // active skill so pierce lives on the basic attack. Idempotent.
        // ------------------------------------------------------------------
        [MenuItem("SurveHive/Combat 2.0/1D - Add Enhancements + retire Piercing Lance")]
        public static void AddEnhancements()
        {
            SkillDefinitionSO pierce = EnsureSkill(
                SkillsFolder + "/PiercingShot.asset", "PiercingStinger", "Piercing Stinger",
                "Attacks pierce through enemies for a small damage penalty. At max level they pierce everything.",
                SkillEffectType.BasicAttackPierceFlat,
                PowerUpLane.Enhancement, SkillElement.Physical, 1f, 3, SkillRarity.Rare,
                "Icon_PictoIcon_Target");

            SkillDefinitionSO burn = EnsureSkill(
                SkillsFolder + "/Ignite.asset", "BurningStinger", "Burning Stinger",
                "Attacks can set enemies ablaze; each level raises the chance and burn damage.",
                SkillEffectType.IgniteChanceFlat,
                PowerUpLane.Enhancement, SkillElement.Fire, 20f, 4, SkillRarity.Rare,
                "Icon_PictoIcon_Fire");

            SkillDefinitionSO poison = EnsureSkill(
                SkillsFolder + "/PoisonStinger.asset", "PoisonStinger", "Poison Stinger",
                "Attacks can poison enemies; each level raises the chance and poison damage.",
                SkillEffectType.PoisonStingerChance,
                PowerUpLane.Enhancement, SkillElement.Poison, 18f, 4, SkillRarity.Rare,
                "Icon_PictoIcon_Skull");

            SkillDefinitionSO frost = EnsureSkill(
                SkillsFolder + "/FrostStinger.asset", "FrostStinger", "Frost Stinger",
                "Attacks have a chance to freeze enemies solid.",
                SkillEffectType.FrostStingerChance,
                PowerUpLane.Enhancement, SkillElement.Frost, 8f, 5, SkillRarity.Epic,
                "Icon_PictoIcon_Star");

            SkillDefinitionSO shock = EnsureSkill(
                SkillsFolder + "/ShockStinger.asset", "ShockStinger", "Shock Stinger",
                "Attacks can bounce to another enemy; each bounce hits softer. Level raises chance and bounces.",
                SkillEffectType.ElectricStingerChance,
                PowerUpLane.Enhancement, SkillElement.Electric, 15f, 4, SkillRarity.Epic,
                "Icon_PictoIcon_Energy");

            RegisterInDatabase(pierce, burn, poison, frost, shock);

            // Retire the Piercing Lance active — pierce is now a basic-attack
            // enhancement, so stop offering the active (asset kept on disk).
            var lance = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(SkillsFolder + "/PiercingLanceCard.asset");
            if (lance != null)
            {
                RemoveFromDatabase(lance);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Combat 2.0 1D: Piercing Shot + Ignite added; Piercing Lance retired.");
        }

        // ------------------------------------------------------------------
        // 1E: Ability-lane expansion. The radial stinger burst now pierces (code,
        // ActiveSkillManager). Adds three abilities with distinct gameplay:
        // Frost Nova (instant 360° slow blast), Ball Lightning (slow bouncing orb,
        // ticking damage), Honey Bomb (scatters slowing honey zones). Idempotent.
        // ------------------------------------------------------------------
        [MenuItem("SurveHive/Combat 2.0/1E - Add Abilities (Frost Nova, Ball Lightning, Honey Bomb)")]
        public static void AddAbilities()
        {
            if (!AssetDatabase.IsValidFolder(ActivesFolder))
            {
                AssetDatabase.CreateFolder(SkillsFolder, "Actives");
            }

            GameObject orbPrefab = EnsureBallLightningPrefab();
            GameObject wavePrefab = EnsureNovaWavePrefab();

            // Frost Nova: a ring expanding outward from the player, slowing each
            // enemy the front reaches. A CC tool — low damage, big radius. Reused
            // fields: Range = start radius, Area = max radius, ZoneDuration = expand
            // time. Radius already bumped ~1.5× and damage kept low.
            ActiveSkillSO frostNova = EnsureActiveSkill(
                ActivesFolder + "/FrostNova.asset", "FrostNova", "Frost Nova",
                ActiveSkillBehavior.Nova, new[]
                {
                    new LevelRow { Damage = 3, Cooldown = 5f, Area = 3.75f, StatusDuration = 2.0f },
                    new LevelRow { Damage = 4, Cooldown = 4.5f, Area = 4.5f, StatusDuration = 2.5f },
                    new LevelRow { Damage = 5, Cooldown = 4.0f, Area = 5.25f, StatusDuration = 3.0f },
                    new LevelRow { Damage = 7, Cooldown = 3.5f, Area = 6.0f, StatusDuration = 3.5f },
                    new LevelRow { Damage = 9, Cooldown = 3.0f, Area = 6.75f, StatusDuration = 4.0f },
                }, 0f, 1.2f, -1, -1, PoolIds.NovaWave, StatusEffectType.Cold, 0.4f, 0f,
                new Color(0.85f, 0.93f, 1f, 0.9f), zoneDuration: 0.5f, zoneTickInterval: 0.4f);

            // Ball Lightning: slow bouncing orb using the new orb pool; damages
            // everything it overlaps each tick. Size + damage scale per level.
            ActiveSkillSO ballLightning = EnsureActiveSkill(
                ActivesFolder + "/BallLightning.asset", "BallLightning", "Ball Lightning",
                ActiveSkillBehavior.BouncingOrb, new[]
                {
                    new LevelRow { Damage = 4, Cooldown = 6f, Area = 1.0f },
                    new LevelRow { Damage = 5, Cooldown = 5.5f, Area = 1.2f },
                    new LevelRow { Damage = 6, Cooldown = 5f, Area = 1.4f },
                    new LevelRow { Damage = 8, Cooldown = 4.5f, Area = 1.6f },
                    new LevelRow { Damage = 10, Cooldown = 4f, Area = 1.9f },
                }, 3f, 0f, PoolIds.BallLightningOrb, -1, -1, StatusEffectType.Stun, 0f, 0f,
                new Color(1f, 0.95f, 0.4f), zoneDuration: 5f, zoneTickInterval: 0.3f);

            // Honey Bomb: scatters Count honey zones at random points around the
            // player, each slowing + damaging. Jars + damage scale per level.
            ActiveSkillSO honeyBomb = EnsureActiveSkill(
                ActivesFolder + "/HoneyBomb.asset", "HoneyBomb", "Honey Bomb",
                ActiveSkillBehavior.ScatterZones, new[]
                {
                    new LevelRow { Damage = 6, Cooldown = 4.0f, Count = 3, Area = 1.3f, StatusChance = 100 },
                    new LevelRow { Damage = 8, Cooldown = 3.7f, Count = 4, Area = 1.4f, StatusChance = 100 },
                    new LevelRow { Damage = 10, Cooldown = 3.4f, Count = 5, Area = 1.5f, StatusChance = 100 },
                    new LevelRow { Damage = 13, Cooldown = 3.1f, Count = 6, Area = 1.6f, StatusChance = 100 },
                    new LevelRow { Damage = 16, Cooldown = 2.8f, Count = 8, Area = 1.7f, StatusChance = 100 },
                }, 8f, 4f, PoolIds.SkillHoneyGlob, PoolIds.HoneySplashVfx, PoolIds.HoneyPuddle,
                StatusEffectType.Slow, 0.35f, 1.5f, new Color(1f, 0.78f, 0.15f),
                zoneDuration: 2.5f, zoneTickInterval: 0.5f);

            SkillDefinitionSO frostCard = EnsureSkill(
                SkillsFolder + "/FrostNovaCard.asset", "FrostNovaCard", "Frost Nova",
                "Unleashes a 360° frost blast that damages and slows nearby enemies. Levels grow the radius and slow.",
                SkillEffectType.ActiveSkill, PowerUpLane.Ability, SkillElement.Frost,
                0f, 5, SkillRarity.Rare, "Icon_PictoIcon_Star", frostNova);

            SkillDefinitionSO ballCard = EnsureSkill(
                SkillsFolder + "/BallLightningCard.asset", "BallLightningCard", "Ball Lightning",
                "Releases a slow orb that pierces enemies, damaging them over time and bouncing off the screen edges.",
                SkillEffectType.ActiveSkill, PowerUpLane.Ability, SkillElement.Electric,
                0f, 5, SkillRarity.Rare, "Icon_PictoIcon_Energy", ballLightning);

            SkillDefinitionSO honeyCard = EnsureSkill(
                SkillsFolder + "/HoneyBombCard.asset", "HoneyBombCard", "Honey Bomb",
                "Scatters honey jars around you that pool into slowing, damaging puddles. Levels add jars.",
                SkillEffectType.ActiveSkill, PowerUpLane.Ability, SkillElement.Honey,
                0f, 5, SkillRarity.Rare, "Icon_PictoIcon_Heart", honeyBomb);

            RegisterInDatabase(frostCard, ballCard, honeyCard);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RegisterPools(orbPrefab, wavePrefab);

            Debug.Log("Combat 2.0 1E: Frost Nova (wave), Ball Lightning (bouncing orb), Honey Bomb (scatter) built.");
        }

        private const string OrbPrefabPath = "Assets/Prefabs/Skills/BallLightningOrb.prefab";

        // Builds the pooled Ball Lightning orb prefab (round sprite, trigger
        // collider, kinematic body, BouncingOrbProjectile). Idempotent.
        private static GameObject EnsureBallLightningPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Skills"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }

                AssetDatabase.CreateFolder("Assets/Prefabs", "Skills");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(OrbPrefabPath) == null)
            {
                var go = new GameObject("BallLightningOrb", typeof(SpriteRenderer), typeof(Rigidbody2D),
                    typeof(CircleCollider2D), typeof(BouncingOrbProjectile));
                PrefabUtility.SaveAsPrefabAsset(go, OrbPrefabPath);
                Object.DestroyImmediate(go);
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(OrbPrefabPath);
            try
            {
                var sr = contents.GetComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/ExpOrb.png");
                sr.color = Color.white;
                sr.sortingOrder = 3;

                var rb = contents.GetComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;

                var col = contents.GetComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.5f;

                var so = new SerializedObject(contents.GetComponent<BouncingOrbProjectile>());
                so.FindProperty("_poolId").intValue = PoolIds.BallLightningOrb;
                so.FindProperty("_targetTag").stringValue = "Enemy";
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, OrbPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(OrbPrefabPath);
        }

        private const string WavePrefabPath = "Assets/Prefabs/Skills/NovaWave.prefab";

        // Builds the pooled Frost Nova wave prefab (a scaling sprite, no collider —
        // it damages via a registry scan). Idempotent.
        private static GameObject EnsureNovaWavePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(WavePrefabPath) == null)
            {
                var go = new GameObject("NovaWave", typeof(SpriteRenderer), typeof(ExpandingWave));
                PrefabUtility.SaveAsPrefabAsset(go, WavePrefabPath);
                Object.DestroyImmediate(go);
            }

            Sprite ring = EnsureFrostRingSprite();

            GameObject contents = PrefabUtility.LoadPrefabContents(WavePrefabPath);
            try
            {
                var sr = contents.GetComponent<SpriteRenderer>();
                sr.sprite = ring;
                sr.color = Color.white; // runtime tint (icy white) drives the look
                sr.sortingOrder = 0;

                var so = new SerializedObject(contents.GetComponent<ExpandingWave>());
                so.FindProperty("_poolId").intValue = PoolIds.NovaWave;
                so.FindProperty("_renderer").objectReferenceValue = sr;
                // Ring outer radius at scale 1: 31px / 16 PPU.
                so.FindProperty("_spriteBaseRadius").floatValue = 31f / 16f;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, WavePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(WavePrefabPath);
        }

        // Generates a hollow white ring sprite (transparent centre, no fill) for
        // the frost wave. 64px @ PPU 16 → ~2u radius at scale 1.
        private static Sprite EnsureFrostRingSprite()
        {
            const string ringPath = "Assets/Sprites/FrostRing.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(ringPath);
            if (existing != null)
            {
                return existing;
            }

            const int diameter = 64;
            const float center = 31.5f;
            const float outer = 31f;
            const float inner = 26f;
            var pixels = new Color32[diameter * diameter];
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy));
                    // Opaque white only within the ring band; fully transparent elsewhere.
                    pixels[(y * diameter) + x] = d <= outer && d >= inner
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            var texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            System.IO.File.WriteAllBytes(ringPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(ringPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(ringPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 16f;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(ringPath);
        }

        // Registers the new orb + nova-wave pools on the Beehive GameBootstrap.
        private static void RegisterPools(GameObject orbPrefab, GameObject wavePrefab)
        {
            EditorSceneManager.OpenScene(BeehiveScenePath, OpenSceneMode.Single);
            GameObject bootstrapGo = GameObject.Find("GameBootstrap");
            if (bootstrapGo == null)
            {
                Debug.LogError("Combat 2.0 1E: GameBootstrap not found — pools not registered.");
                return;
            }

            var so = new SerializedObject(bootstrapGo.GetComponent<SurveHive.Core.GameBootstrap>());
            SerializedProperty pools = so.FindProperty("_pools");
            EnsurePoolEntry(pools, PoolIds.BallLightningOrb, orbPrefab, 2, 6);
            EnsurePoolEntry(pools, PoolIds.NovaWave, wavePrefab, 2, 6);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bootstrapGo);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void EnsurePoolEntry(SerializedProperty pools, int poolId, GameObject prefab, int prewarm, int maxSize)
        {
            for (int i = 0; i < pools.arraySize; i++)
            {
                if (pools.GetArrayElementAtIndex(i).FindPropertyRelative("poolId").intValue == poolId)
                {
                    pools.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue = prefab;
                    return;
                }
            }

            int index = pools.arraySize;
            pools.arraySize = index + 1;
            SerializedProperty entry = pools.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("poolId").intValue = poolId;
            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            entry.FindPropertyRelative("prewarmCount").intValue = prewarm;
            entry.FindPropertyRelative("maxSize").intValue = maxSize;
        }

        private static ActiveSkillSO EnsureActiveSkill(
            string assetPath, string id, string displayName, ActiveSkillBehavior behavior,
            LevelRow[] levels, float projectileSpeed, float range,
            int projectilePoolId, int impactVfxPoolId, int zonePoolId,
            StatusEffectType statusType, float statusPotency, float statusDuration, Color projectileTint,
            float zoneDuration = 3.5f, float zoneTickInterval = 0.5f)
        {
            var skill = AssetDatabase.LoadAssetAtPath<ActiveSkillSO>(assetPath);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<ActiveSkillSO>();
                AssetDatabase.CreateAsset(skill, assetPath);
            }

            var so = new SerializedObject(skill);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_behavior").enumValueIndex = (int)behavior;
            so.FindProperty("_projectileTint").colorValue = projectileTint;
            so.FindProperty("_projectileSpeed").floatValue = projectileSpeed;
            so.FindProperty("_range").floatValue = range;
            so.FindProperty("_projectilePoolId").intValue = projectilePoolId;
            so.FindProperty("_impactVfxPoolId").intValue = impactVfxPoolId;
            so.FindProperty("_zonePoolId").intValue = zonePoolId;
            so.FindProperty("_zoneDuration").floatValue = zoneDuration;
            so.FindProperty("_zoneTickInterval").floatValue = zoneTickInterval;
            so.FindProperty("_appliesStatus").boolValue = true;
            so.FindProperty("_statusType").enumValueIndex = (int)statusType;
            so.FindProperty("_statusPotency").floatValue = statusPotency;
            so.FindProperty("_statusDuration").floatValue = statusDuration;

            SerializedProperty levelsProp = so.FindProperty("_levels");
            levelsProp.arraySize = levels.Length;
            for (int i = 0; i < levels.Length; i++)
            {
                SerializedProperty row = levelsProp.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Damage").floatValue = levels[i].Damage;
                row.FindPropertyRelative("Cooldown").floatValue = levels[i].Cooldown;
                row.FindPropertyRelative("Count").intValue = levels[i].Count;
                row.FindPropertyRelative("Area").floatValue = levels[i].Area;
                row.FindPropertyRelative("StatusChancePercent").floatValue = levels[i].StatusChance;
                row.FindPropertyRelative("StatusDuration").floatValue = levels[i].StatusDuration;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void RemoveFromDatabase(SkillDefinitionSO skill)
        {
            var db = AssetDatabase.LoadAssetAtPath<SkillDatabaseSO>(DatabasePath);
            if (db == null)
            {
                return;
            }

            var so = new SerializedObject(db);
            SerializedProperty arr = so.FindProperty("_skills");
            for (int i = arr.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty element = arr.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != skill)
                {
                    continue;
                }

                // Current Unity removes an object-reference element directly (the
                // old "null then delete again" idiom over-deletes the next entry).
                arr.DeleteArrayElementAtIndex(i);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(db);
        }

        private static bool ContainsReference(SerializedProperty array, Object value)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == value)
                {
                    return true;
                }
            }

            return false;
        }

        private static Sprite FindIcon(string fileNameNoExt)
        {
            string[] guids = AssetDatabase.FindAssets($"{fileNameNoExt} t:Sprite");
            if (guids.Length == 0)
            {
                Debug.LogWarning($"Combat 2.0 1C: icon '{fileNameNoExt}' not found; leaving unassigned.");
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ------------------------------------------------------------------
        // 1F: owned power-ups pause panel. Adds a POWER-UPS button + a panel
        // listing the run's build (grouped by lane) onto the existing PauseRoot.
        // Additive over Phase 4's pause menu; idempotent.
        // ------------------------------------------------------------------
        private const string UiKitPath = "Assets/ThirdParty/PixelUI/UI SIMPLE PIXEL UNSPLIT.png";
        private static readonly Color DeepBrownC = new Color(0.227f, 0.141f, 0.086f);
        private static readonly Color HoneyGoldC = new Color(1f, 0.765f, 0.043f);
        private static readonly Color WaxC = new Color(0.91f, 0.847f, 0.627f);
        private static readonly Color DangerRedC = new Color(0.851f, 0.282f, 0.231f);

        [MenuItem("SurveHive/Combat 2.0/1F - Owned Power-ups Pause Panel")]
        public static void ApplyPowerUpsPanel()
        {
            EditorSceneManager.OpenScene(BeehiveScenePath, OpenSceneMode.Single);

            GameObject canvasGo = GameObject.Find("Canvas");
            Transform pauseRoot = canvasGo != null ? canvasGo.transform.Find("PauseRoot") : null;
            Transform pausePanel = pauseRoot != null ? pauseRoot.Find("PausePanel") : null;
            if (pausePanel == null)
            {
                Debug.LogError("Combat 2.0 1F: PauseRoot/PausePanel not found — run the Phase 4 pause pass first.");
                return;
            }

            var controller = pauseRoot.GetComponent<PauseMenuController>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite panelSprite = LoadUiSprite("PixelPanel");
            Sprite buttonSprite = LoadUiSprite("PixelButton");

            // Make room in the pause panel, then add the POWER-UPS entry.
            SetAnchoredY(pausePanel.Find("ResumeButton"), 180f);
            SetAnchoredY(pausePanel.Find("SettingsButton"), -100f);
            SetAnchoredY(pausePanel.Find("AbandonButton"), -240f);
            Button powerUpsButton = MakeButton(pausePanel, "PowerUpsButton", "POWER-UPS", font, buttonSprite, new Vector2(0f, 40f));

            // Build-list panel.
            GameObject panel = MakePanel(pauseRoot, "PowerUpsPanel", panelSprite, new Vector2(820f, 900f));
            MakeTitle(panel.transform, font);
            TMP_Text listText = MakeListText(panel.transform, font);
            Button backButton = MakeButton(panel.transform, "BackButton", "BACK", font, buttonSprite, new Vector2(0f, -360f));

            if (!panel.TryGetComponent(out OwnedPowerUpsView view))
            {
                view = panel.AddComponent<OwnedPowerUpsView>();
            }

            Transform levelUpPanel = canvasGo.transform.Find("LevelUpPanel");
            LevelUpUIController levelUp = levelUpPanel != null ? levelUpPanel.GetComponent<LevelUpUIController>() : null;
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_levelUp").objectReferenceValue = levelUp;
            viewSo.FindProperty("_text").objectReferenceValue = listText;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(controller);
            so.FindProperty("_powerUpsPanel").objectReferenceValue = panel;
            so.FindProperty("_powerUpsButton").objectReferenceValue = powerUpsButton;
            so.FindProperty("_powerUpsBackButton").objectReferenceValue = backButton;
            so.FindProperty("_powerUpsView").objectReferenceValue = view;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            panel.SetActive(false);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Combat 2.0 1F: owned power-ups pause panel built + wired.");
        }

        private static void SetAnchoredY(Transform t, float y)
        {
            if (t != null)
            {
                var rect = (RectTransform)t;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
            }
        }

        private static Sprite LoadUiSprite(string spriteName)
        {
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(UiKitPath);
            for (int i = 0; i < subAssets.Length; i++)
            {
                if (subAssets[i] is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            Debug.LogError($"Combat 2.0 1F: UI kit sprite '{spriteName}' not found.");
            return null;
        }

        private static GameObject MakePanel(Transform parent, string name, Sprite sprite, Vector2 size)
        {
            GameObject go = FindOrCreateChild(parent, name);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            Image image = EnsureImage(go);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = new Color(DeepBrownC.r, DeepBrownC.g, DeepBrownC.b, 0.97f);
            image.raycastTarget = true;
            return go;
        }

        private static Button MakeButton(
            Transform parent, string name, string label, TMP_FontAsset font, Sprite sprite, Vector2 pos)
        {
            GameObject go = FindOrCreateChild(parent, name);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(620f, 120f);

            Image image = EnsureImage(go);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = HoneyGoldC;

            if (!go.TryGetComponent(out Button button))
            {
                button = go.AddComponent<Button>();
            }

            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.7f);
            colors.pressedColor = new Color(0.9f, 0.7f, 0.2f);
            colors.disabledColor = new Color(0.45f, 0.42f, 0.38f);
            button.colors = colors;

            if (!go.TryGetComponent(out UIClickSfx _))
            {
                go.AddComponent<UIClickSfx>();
            }

            GameObject labelGo = FindOrCreateChild(go.transform, "Label");
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TMP_Text tmp = EnsureTmp(labelGo, font, 40f, DeepBrownC, TextAlignmentOptions.Center);
            tmp.text = label;

            return button;
        }

        private static void MakeTitle(Transform panel, TMP_FontAsset font)
        {
            GameObject go = FindOrCreateChild(panel, "Title");
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -50f);
            rect.sizeDelta = new Vector2(760f, 90f);
            TMP_Text tmp = EnsureTmp(go, font, 60f, HoneyGoldC, TextAlignmentOptions.Center);
            tmp.text = "YOUR BUILD";
        }

        private static TMP_Text MakeListText(Transform panel, TMP_FontAsset font)
        {
            GameObject go = FindOrCreateChild(panel, "BuildList");
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(44f, 185f);
            rect.offsetMax = new Vector2(-44f, -140f);
            TMP_Text tmp = EnsureTmp(go, font, 28f, WaxC, TextAlignmentOptions.TopLeft);
            tmp.text = string.Empty;
            return tmp;
        }

        // ------------------------------------------------------------------
        // Phase 2A: pre-spawn warning banner. Adds an upper-centre banner driven
        // by StageDirector.OnStageWarning (~5s before each strong wave / boss).
        // Idempotent.
        // ------------------------------------------------------------------
        [MenuItem("SurveHive/Combat 2.0/2A - Pre-spawn Warning Banner")]
        public static void ApplyWarningBanner()
        {
            EditorSceneManager.OpenScene(BeehiveScenePath, OpenSceneMode.Single);

            GameObject canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                Debug.LogError("Phase 2A: Canvas not found in Beehive.unity.");
                return;
            }

            GameObject directorGo = GameObject.Find("StageDirector");
            var director = directorGo != null ? directorGo.GetComponent<StageDirector>() : null;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

            // Centre-anchored (upper) so it stays visible in both orientations.
            GameObject bannerGo = FindOrCreateChild(canvasGo.transform, "WaveWarningBanner");
            var rect = (RectTransform)bannerGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 300f);
            rect.sizeDelta = new Vector2(960f, 170f);

            if (!bannerGo.TryGetComponent(out CanvasGroup canvasGroup))
            {
                canvasGroup = bannerGo.AddComponent<CanvasGroup>();
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0f;

            GameObject textGo = FindOrCreateChild(bannerGo.transform, "Text");
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TMP_Text text = EnsureTmp(textGo, font, 54f, DangerRedC, TextAlignmentOptions.Center);
            text.text = string.Empty;

            if (!bannerGo.TryGetComponent(out WaveWarningBanner banner))
            {
                banner = bannerGo.AddComponent<WaveWarningBanner>();
            }

            var so = new SerializedObject(banner);
            so.FindProperty("_director").objectReferenceValue = director;
            so.FindProperty("_text").objectReferenceValue = text;
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(banner);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Phase 2A: pre-spawn warning banner built + wired.");
        }

        // ------------------------------------------------------------------
        // Phase 2C: boss/miniboss death sequence coordinator (slow-mo + invuln +
        // shockwave). Reuses the Royal Bomb nuke VFX as the shockwave. Idempotent.
        // ------------------------------------------------------------------
        [MenuItem("SurveHive/Combat 2.0/2C - Boss Death Sequence")]
        public static void ApplyBossDeathSequence()
        {
            EditorSceneManager.OpenScene(BeehiveScenePath, OpenSceneMode.Single);

            GameObject go = GameObject.Find("BossDeathSequence");
            if (go == null)
            {
                go = new GameObject("BossDeathSequence");
            }

            if (!go.TryGetComponent(out BossDeathSequence sequence))
            {
                sequence = go.AddComponent<BossDeathSequence>();
            }

            var shaker = Object.FindFirstObjectByType<CameraShaker>();
            var so = new SerializedObject(sequence);
            so.FindProperty("_shaker").objectReferenceValue = shaker;
            so.FindProperty("_shockwavePoolId").intValue = PoolIds.NukeVfx;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(go);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Phase 2C: boss death sequence built + wired.");
        }

        // ------------------------------------------------------------------
        // Phase 2B: wire the miniboss reward — BossSpawner needs the player's
        // PlayerExperience to grant the guaranteed-lucky level-up + EXP burst.
        // Idempotent.
        // ------------------------------------------------------------------
        [MenuItem("SurveHive/Combat 2.0/2B - Miniboss Reward Wiring")]
        public static void ApplyMinibossReward()
        {
            EditorSceneManager.OpenScene(BeehiveScenePath, OpenSceneMode.Single);

            GameObject directorGo = GameObject.Find("StageDirector");
            BossSpawner bossSpawner = directorGo != null ? directorGo.GetComponent<BossSpawner>() : null;
            if (bossSpawner == null)
            {
                Debug.LogError("Phase 2B: BossSpawner not found — run the Phase 3 run-structure pass first.");
                return;
            }

            var experience = Object.FindFirstObjectByType<SurveHive.Progression.PlayerExperience>();
            var so = new SerializedObject(bossSpawner);
            so.FindProperty("_playerExperience").objectReferenceValue = experience;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bossSpawner);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Phase 2B: miniboss reward wired.");
        }
    }
}
