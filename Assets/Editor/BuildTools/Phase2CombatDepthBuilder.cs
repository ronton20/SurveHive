using System.Collections.Generic;
using SurveHive.Combat;
using SurveHive.Combat.Skills;
using SurveHive.Combat.Status;
using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Enemies;
using SurveHive.Health;
using SurveHive.Player;
using SurveHive.Progression;
using SurveHive.UI;
using SurveHive.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Phase 2 (PLAN.md): status effects, the 6-skill active arsenal, 10 passives,
    /// and rarity/lucky level-up offering. Generates all data assets, projectile/
    /// zone/VFX prefabs and scene wiring. Additive over Phases 0/1; idempotent.
    /// </summary>
    public static class Phase2CombatDepthBuilder
    {
        private const string ScenePath = "Assets/Scenes/Beehive.unity";
        private const string ActiveSkillFolder = "Assets/Data/Skills/Actives";
        private const string SkillFolder = "Assets/Data/Skills";
        private const string SkillPrefabFolder = "Assets/Prefabs/Skills";
        private const string VfxPrefabFolder = "Assets/Prefabs/VFX";
        private const string IconFolder = "Assets/ThirdParty/IconsTemp/Icons/PictoIcon_128";
        private const string SpriteEffectsRoot = "Assets/ThirdParty/SpriteEffects";

        private static readonly Color HoneyGold = new Color(1f, 0.765f, 0.043f);

        // ------------------------------------------------------------------
        // Generated pixel art (k = outline, g = body, w = highlight).
        // ------------------------------------------------------------------
        private static readonly string[] LanceDartPixels =
        {
            ".kkkkkkkkkkkk...",
            "kgggggggggggwk..",
            "kgwwwwwwwwwwwgk.",
            "kgggggggggggwk..",
            ".kkkkkkkkkkkk...",
        };

        private static readonly string[] HoneyGlobPixels =
        {
            "..kkkk..",
            ".kggggk.",
            "kggwwggk",
            "kgwwwggk",
            "kggggggk",
            "kggggggk",
            ".kggggk.",
            "..kkkk..",
        };

        private static readonly string[] EmberBoltPixels =
        {
            "...kkkkkkk.",
            ".kkggggggwk",
            "kgggwwwwwwk",
            "kgggwwwwwwk",
            ".kkggggggwk",
            "...kkkkkkk.",
        };

        private static readonly string[] ZapSegmentPixels =
        {
            "gwwggwwggwwggwwg",
            "wggwwggwwggwwggw",
            "gwwggwwggwwggwwg",
        };

        [MenuItem("SurveHive/Apply Phase 2 Combat Depth")]
        public static void Apply()
        {
            EnsureFolder(ActiveSkillFolder);
            EnsureFolder(SkillPrefabFolder);

            // 1. Generated sprites.
            Color32 outline = new Color32(58, 36, 22, 255);
            Sprite lanceSprite = CreatePixelSprite("LanceDart", LanceDartPixels,
                outline, new Color32(245, 166, 35, 255), new Color32(255, 240, 180, 255));
            Sprite globSprite = CreatePixelSprite("HoneyGlob", HoneyGlobPixels,
                outline, new Color32(255, 195, 11, 255), new Color32(255, 240, 180, 255));
            Sprite emberSprite = CreatePixelSprite("EmberBolt", EmberBoltPixels,
                outline, new Color32(217, 72, 59, 255), new Color32(255, 220, 120, 255));
            Sprite zapSprite = CreatePixelSprite("ZapSegment", ZapSegmentPixels,
                new Color32(0, 0, 0, 0), new Color32(120, 200, 255, 255), new Color32(255, 255, 255, 255));
            Sprite puddleSprite = CreateCircleSprite("HoneyPuddleZone", 32,
                new Color32(255, 195, 11, 110), new Color32(140, 90, 43, 200));
            Sprite auraSprite = CreateCircleSprite("PollenAuraZone", 32,
                new Color32(124, 181, 24, 70), new Color32(124, 181, 24, 150));
            Sprite stingerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Stinger.png");

            // 2. Pooled prefabs.
            EnsureSkillProjectilePrefab("SkillStinger", stingerSprite, PoolIds.SkillStinger, 0.25f,
                new Color(1f, 0.85f, 0.55f));
            EnsureSkillProjectilePrefab("SkillLance", lanceSprite, PoolIds.SkillLance, 0.3f, Color.white);
            EnsureSkillProjectilePrefab("HoneyGlobProjectile", globSprite, PoolIds.SkillHoneyGlob, 0.2f, Color.white);
            EnsureSkillProjectilePrefab("EmberBolt", emberSprite, PoolIds.SkillEmberBolt, 0.25f, Color.white);
            EnsureHoneyPuddlePrefab(puddleSprite);
            EnsureZapArcPrefab(zapSprite);
            GameObject emberExplosion = EnsurePackVfxWrapper(
                "EmberExplosion", "Explosion_normal", PoolIds.EmberExplosionVfx, 0.6f, false, Color.clear);
            GameObject honeySplash = EnsurePackVfxWrapper(
                "HoneySplash", "Water_Splash_06_round", PoolIds.HoneySplashVfx, 0.6f, true, HoneyGold);

            // 3. Active skill data assets.
            ActiveSkillSO stingerBarrage = EnsureStingerBarrage();
            ActiveSkillSO piercingLance = EnsurePiercingLance();
            ActiveSkillSO honeySplashSkill = EnsureHoneySplash();
            ActiveSkillSO pollenCloud = EnsurePollenCloud();
            ActiveSkillSO staticWings = EnsureStaticWings();
            ActiveSkillSO emberSting = EnsureEmberSting();

            // 4. Skill cards: 6 active unlock cards + 6 new passives, and rarity
            // on the 4 existing passives.
            var databaseSkills = new List<SkillDefinitionSO>(16);

            databaseSkills.Add(UpdateExistingSkillRarity("SwiftWings", SkillRarity.Common));
            databaseSkills.Add(UpdateExistingSkillRarity("ThickerChitin", SkillRarity.Common));
            databaseSkills.Add(UpdateExistingSkillRarity("LongerStinger", SkillRarity.Common));
            databaseSkills.Add(UpdateExistingSkillRarity("TwinStingers", SkillRarity.Epic));

            databaseSkills.Add(EnsurePassiveSkill("NectarSense", "Nectar Sense",
                "Pickups drift to you from 25% further away.",
                SkillEffectType.MagnetRadiusPercent, 25f, 5, SkillRarity.Common, "Icon_PictoIcon_Magnetic.Png"));
            databaseSkills.Add(EnsurePassiveSkill("PotentVenom", "Potent Venom",
                "All damage you deal is increased by 10%.",
                SkillEffectType.AttackDamagePercent, 10f, 6, SkillRarity.Common, "Icon_PictoIcon_Flask_01.Png"));
            databaseSkills.Add(EnsurePassiveSkill("KeenEye", "Keen Eye",
                "+5% chance to land a critical hit for bonus damage.",
                SkillEffectType.CritChanceFlat, 5f, 6, SkillRarity.Rare, "Icon_PictoIcon_Show.Png"));
            databaseSkills.Add(EnsurePassiveSkill("NectarDrain", "Nectar Drain",
                "Heal for 2% of all damage you deal.",
                SkillEffectType.LifestealFlat, 2f, 5, SkillRarity.Rare, "Icon_PictoIcon_Heart.Png"));
            databaseSkills.Add(EnsurePassiveSkill("HyperMetabolism", "Hyper Metabolism",
                "Active skills recharge 8% faster.",
                SkillEffectType.ActiveCooldownPercent, 8f, 5, SkillRarity.Rare, "Icon_PictoIcon_Stopwatch.Png"));
            databaseSkills.Add(EnsurePassiveSkill("DeadlyPrecision", "Deadly Precision",
                "Critical hits deal +25% more damage.",
                SkillEffectType.CritDamagePercent, 25f, 4, SkillRarity.Epic, "Icon_PictoIcon_Star.Png"));

            databaseSkills.Add(EnsureActiveSkillCard("StingerBarrageCard", "Stinger Barrage",
                "Blasts a ring of stingers in all directions.",
                stingerBarrage, SkillRarity.Common, "Icon_PictoIcon_Asterisk.Png"));
            databaseSkills.Add(EnsureActiveSkillCard("PiercingLanceCard", "Piercing Lance",
                "Fires a high-speed lance that pierces through every enemy in a line.",
                piercingLance, SkillRarity.Rare, "Icon_PictoIcon_Send_1.Png"));
            databaseSkills.Add(EnsureActiveSkillCard("HoneySplashCard", "Honey Splash",
                "Lobs a honey glob that leaves a sticky puddle, damaging and slowing enemies.",
                honeySplashSkill, SkillRarity.Rare, "Icon_PictoIcon_Water.Png"));
            databaseSkills.Add(EnsureActiveSkillCard("PollenCloudCard", "Pollen Cloud",
                "A toxic pollen aura surrounds you, poisoning everything inside.",
                pollenCloud, SkillRarity.Rare, "Icon_PictoIcon_Cloud.Png"));
            databaseSkills.Add(EnsureActiveSkillCard("StaticWingsCard", "Static Wings",
                "Static discharge arcs between nearby enemies, with a chance to stun.",
                staticWings, SkillRarity.Epic, "Icon_PictoIcon_Thunder.Png"));
            databaseSkills.Add(EnsureActiveSkillCard("EmberStingCard", "Ember Sting",
                "A fiery homing bolt that explodes on impact and sets enemies ablaze.",
                emberSting, SkillRarity.Epic, "Icon_PictoIcon_Fire.Png"));

            UpdateSkillDatabase(databaseSkills);

            // 5. Enemy prefabs gain status-effect receivers.
            EnsureEnemyStatusReceiver("Assets/Prefabs/Enemies/WorkerBee.prefab");
            EnsureEnemyStatusReceiver("Assets/Prefabs/Enemies/WarriorBee.prefab");
            EnsureEnemyStatusReceiver("Assets/Prefabs/Enemies/QueensGuard.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 6. Scene wiring.
            ApplySceneChanges(auraSprite);

            Debug.Log("SurveHive Phase 2 combat depth build complete.");
        }

        // ------------------------------------------------------------------
        // Active skill data (growth tables per PLAN.md §4.2).
        // ------------------------------------------------------------------
        private struct LevelRow
        {
            public float Damage;
            public float Cooldown;
            public int Count;
            public float Area;
            public float StatusChance;

            public LevelRow(float damage, float cooldown, int count, float area, float statusChance)
            {
                Damage = damage;
                Cooldown = cooldown;
                Count = count;
                Area = area;
                StatusChance = statusChance;
            }
        }

        private static ActiveSkillSO EnsureStingerBarrage()
        {
            return EnsureActiveSkill("StingerBarrage", "Stinger Barrage", ActiveSkillBehavior.RadialVolley,
                projectileSpeed: 9f, range: 7f,
                projectilePoolId: PoolIds.SkillStinger, impactVfxPoolId: -1,
                zonePoolId: -1, zoneDuration: 0f, zoneTickInterval: 0.5f,
                appliesStatus: false, statusType: StatusEffectType.Burn, statusPotency: 0f, statusDuration: 0f,
                new[]
                {
                    new LevelRow(8f, 2.6f, 6, 0f, 0f),
                    new LevelRow(10f, 2.4f, 7, 0f, 0f),
                    new LevelRow(12f, 2.2f, 8, 0f, 0f),
                    new LevelRow(14f, 2.0f, 10, 0f, 0f),
                    new LevelRow(17f, 1.8f, 12, 0f, 0f),
                });
        }

        private static ActiveSkillSO EnsurePiercingLance()
        {
            return EnsureActiveSkill("PiercingLance", "Piercing Lance", ActiveSkillBehavior.PiercingShot,
                projectileSpeed: 16f, range: 12f,
                projectilePoolId: PoolIds.SkillLance, impactVfxPoolId: -1,
                zonePoolId: -1, zoneDuration: 0f, zoneTickInterval: 0.5f,
                appliesStatus: false, statusType: StatusEffectType.Burn, statusPotency: 0f, statusDuration: 0f,
                new[]
                {
                    new LevelRow(14f, 3.2f, 1, 0f, 0f),
                    new LevelRow(18f, 3.0f, 1, 0f, 0f),
                    new LevelRow(22f, 2.8f, 1, 0f, 0f),
                    new LevelRow(26f, 2.5f, 1, 0f, 0f),
                    new LevelRow(31f, 2.2f, 1, 0f, 0f),
                });
        }

        private static ActiveSkillSO EnsureHoneySplash()
        {
            return EnsureActiveSkill("HoneySplash", "Honey Splash", ActiveSkillBehavior.LobbedPuddle,
                projectileSpeed: 8f, range: 7f,
                projectilePoolId: PoolIds.SkillHoneyGlob, impactVfxPoolId: PoolIds.HoneySplashVfx,
                zonePoolId: PoolIds.HoneyPuddle, zoneDuration: 3.5f, zoneTickInterval: 0.5f,
                appliesStatus: true, statusType: StatusEffectType.Slow, statusPotency: 0.4f, statusDuration: 1.2f,
                new[]
                {
                    new LevelRow(4f, 5.0f, 1, 1.6f, 100f),
                    new LevelRow(5f, 4.6f, 1, 1.8f, 100f),
                    new LevelRow(6f, 4.2f, 1, 2.0f, 100f),
                    new LevelRow(7f, 3.9f, 1, 2.3f, 100f),
                    new LevelRow(9f, 3.5f, 1, 2.6f, 100f),
                });
        }

        private static ActiveSkillSO EnsurePollenCloud()
        {
            return EnsureActiveSkill("PollenCloud", "Pollen Cloud", ActiveSkillBehavior.Aura,
                projectileSpeed: 0f, range: 4f,
                projectilePoolId: -1, impactVfxPoolId: -1,
                zonePoolId: -1, zoneDuration: 0f, zoneTickInterval: 0.5f,
                appliesStatus: true, statusType: StatusEffectType.Poison, statusPotency: 2f, statusDuration: 3f,
                new[]
                {
                    new LevelRow(3f, 0.8f, 1, 2.2f, 35f),
                    new LevelRow(4f, 0.8f, 1, 2.5f, 40f),
                    new LevelRow(5f, 0.8f, 1, 2.8f, 45f),
                    new LevelRow(6f, 0.8f, 1, 3.1f, 50f),
                    new LevelRow(7f, 0.8f, 1, 3.4f, 60f),
                });
        }

        private static ActiveSkillSO EnsureStaticWings()
        {
            return EnsureActiveSkill("StaticWings", "Static Wings", ActiveSkillBehavior.ChainArc,
                projectileSpeed: 0f, range: 7f,
                projectilePoolId: -1, impactVfxPoolId: PoolIds.ZapArcVfx,
                zonePoolId: -1, zoneDuration: 0f, zoneTickInterval: 0.5f,
                appliesStatus: true, statusType: StatusEffectType.Stun, statusPotency: 0f, statusDuration: 0.8f,
                new[]
                {
                    new LevelRow(10f, 3.6f, 3, 3.5f, 20f),
                    new LevelRow(13f, 3.3f, 4, 3.5f, 25f),
                    new LevelRow(16f, 3.0f, 5, 3.5f, 30f),
                    new LevelRow(19f, 2.8f, 6, 3.5f, 35f),
                    new LevelRow(22f, 2.6f, 7, 3.5f, 40f),
                });
        }

        private static ActiveSkillSO EnsureEmberSting()
        {
            return EnsureActiveSkill("EmberSting", "Ember Sting", ActiveSkillBehavior.HomingBolt,
                projectileSpeed: 7f, range: 11f,
                projectilePoolId: PoolIds.SkillEmberBolt, impactVfxPoolId: PoolIds.EmberExplosionVfx,
                zonePoolId: -1, zoneDuration: 0f, zoneTickInterval: 0.5f,
                appliesStatus: true, statusType: StatusEffectType.Burn, statusPotency: 3f, statusDuration: 3f,
                new[]
                {
                    new LevelRow(12f, 3.4f, 1, 1.5f, 50f),
                    new LevelRow(15f, 3.2f, 1, 1.6f, 55f),
                    new LevelRow(18f, 3.0f, 1, 1.8f, 60f),
                    new LevelRow(22f, 2.7f, 1, 2.0f, 70f),
                    new LevelRow(26f, 2.4f, 1, 2.2f, 80f),
                });
        }

        private static ActiveSkillSO EnsureActiveSkill(
            string assetName, string displayName, ActiveSkillBehavior behavior,
            float projectileSpeed, float range, int projectilePoolId, int impactVfxPoolId,
            int zonePoolId, float zoneDuration, float zoneTickInterval,
            bool appliesStatus, StatusEffectType statusType, float statusPotency, float statusDuration,
            LevelRow[] levels)
        {
            string path = $"{ActiveSkillFolder}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ActiveSkillSO>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ActiveSkillSO>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_id").stringValue = assetName;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_behavior").intValue = (int)behavior;
            serialized.FindProperty("_projectileSpeed").floatValue = projectileSpeed;
            serialized.FindProperty("_range").floatValue = range;
            serialized.FindProperty("_projectilePoolId").intValue = projectilePoolId;
            serialized.FindProperty("_impactVfxPoolId").intValue = impactVfxPoolId;
            serialized.FindProperty("_zonePoolId").intValue = zonePoolId;
            serialized.FindProperty("_zoneDuration").floatValue = zoneDuration;
            serialized.FindProperty("_zoneTickInterval").floatValue = zoneTickInterval;
            serialized.FindProperty("_appliesStatus").boolValue = appliesStatus;
            serialized.FindProperty("_statusType").intValue = (int)statusType;
            serialized.FindProperty("_statusPotency").floatValue = statusPotency;
            serialized.FindProperty("_statusDuration").floatValue = statusDuration;

            SerializedProperty levelsProp = serialized.FindProperty("_levels");
            levelsProp.arraySize = levels.Length;
            for (int i = 0; i < levels.Length; i++)
            {
                SerializedProperty row = levelsProp.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Damage").floatValue = levels[i].Damage;
                row.FindPropertyRelative("Cooldown").floatValue = levels[i].Cooldown;
                row.FindPropertyRelative("Count").intValue = levels[i].Count;
                row.FindPropertyRelative("Area").floatValue = levels[i].Area;
                row.FindPropertyRelative("StatusChancePercent").floatValue = levels[i].StatusChance;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        // ------------------------------------------------------------------
        // Skill cards (SkillDefinitionSO) for the level-up offering.
        // ------------------------------------------------------------------
        private static SkillDefinitionSO UpdateExistingSkillRarity(string assetName, SkillRarity rarity)
        {
            string path = $"{SkillFolder}/{assetName}.asset";
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(path);
            if (skill == null)
            {
                Debug.LogError($"Phase2: expected existing skill at {path}.");
                return null;
            }

            var serialized = new SerializedObject(skill);
            serialized.FindProperty("_rarity").intValue = (int)rarity;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillDefinitionSO EnsurePassiveSkill(
            string assetName, string displayName, string description,
            SkillEffectType effectType, float magnitude, int maxLevel, SkillRarity rarity, string iconFileName)
        {
            SkillDefinitionSO skill = EnsureSkillDefinition(assetName);

            var serialized = new SerializedObject(skill);
            serialized.FindProperty("_id").stringValue = assetName;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_description").stringValue = description;
            serialized.FindProperty("_effectType").intValue = (int)effectType;
            serialized.FindProperty("_magnitude").floatValue = magnitude;
            serialized.FindProperty("_maxLevel").intValue = maxLevel;
            serialized.FindProperty("_rarity").intValue = (int)rarity;
            serialized.FindProperty("_icon").objectReferenceValue = LoadIconSprite(iconFileName);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillDefinitionSO EnsureActiveSkillCard(
            string assetName, string displayName, string description,
            ActiveSkillSO activeSkill, SkillRarity rarity, string iconFileName)
        {
            SkillDefinitionSO skill = EnsureSkillDefinition(assetName);

            var serialized = new SerializedObject(skill);
            serialized.FindProperty("_id").stringValue = assetName;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_description").stringValue = description;
            serialized.FindProperty("_effectType").intValue = (int)SkillEffectType.ActiveSkill;
            serialized.FindProperty("_magnitude").floatValue = 0f;
            serialized.FindProperty("_maxLevel").intValue = activeSkill.MaxLevel;
            serialized.FindProperty("_rarity").intValue = (int)rarity;
            serialized.FindProperty("_icon").objectReferenceValue = LoadIconSprite(iconFileName);
            serialized.FindProperty("_activeSkill").objectReferenceValue = activeSkill;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillDefinitionSO EnsureSkillDefinition(string assetName)
        {
            string path = $"{SkillFolder}/{assetName}.asset";
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(path);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillDefinitionSO>();
                AssetDatabase.CreateAsset(skill, path);
            }

            return skill;
        }

        private static void UpdateSkillDatabase(List<SkillDefinitionSO> skills)
        {
            var database = AssetDatabase.LoadAssetAtPath<SkillDatabaseSO>($"{SkillFolder}/SkillDatabase.asset");
            var serialized = new SerializedObject(database);
            SerializedProperty skillsProp = serialized.FindProperty("_skills");
            skillsProp.arraySize = skills.Count;
            for (int i = 0; i < skills.Count; i++)
            {
                skillsProp.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        // ------------------------------------------------------------------
        // Prefabs.
        // ------------------------------------------------------------------
        private static void EnsureSkillProjectilePrefab(
            string name, Sprite sprite, int poolId, float colliderRadius, Color tint)
        {
            string path = $"{SkillPrefabFolder}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                var go = new GameObject(name);
                go.AddComponent<SpriteRenderer>();
                var collider = go.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                go.AddComponent<SkillProjectile>();
                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var renderer = contents.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = tint;
                renderer.sortingOrder = 1;

                var collider = contents.GetComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = colliderRadius;

                var projectile = contents.GetComponent<SkillProjectile>();
                var serialized = new SerializedObject(projectile);
                serialized.FindProperty("_poolId").intValue = poolId;
                serialized.FindProperty("_targetTag").stringValue = "Enemy";
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void EnsureHoneyPuddlePrefab(Sprite sprite)
        {
            string path = $"{SkillPrefabFolder}/HoneyPuddle.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                var go = new GameObject("HoneyPuddle");
                go.AddComponent<SpriteRenderer>();
                go.AddComponent<AreaEffectZone>();
                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var renderer = contents.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = Color.white;
                // Ground layer: under enemies and the player.
                renderer.sortingOrder = -1;

                var zone = contents.GetComponent<AreaEffectZone>();
                var serialized = new SerializedObject(zone);
                serialized.FindProperty("_poolId").intValue = PoolIds.HoneyPuddle;
                serialized.FindProperty("_renderer").objectReferenceValue = renderer;
                serialized.FindProperty("_spriteBaseRadius").floatValue = 1f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void EnsureZapArcPrefab(Sprite sprite)
        {
            string path = $"{SkillPrefabFolder}/ZapArc.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                var go = new GameObject("ZapArc");
                go.AddComponent<SpriteRenderer>();
                go.AddComponent<ZapArcVfx>();
                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var renderer = contents.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = Color.white;
                renderer.sortingOrder = 3;

                var arc = contents.GetComponent<ZapArcVfx>();
                var serialized = new SerializedObject(arc);
                serialized.FindProperty("_poolId").intValue = PoolIds.ZapArcVfx;
                serialized.FindProperty("_renderer").objectReferenceValue = renderer;
                serialized.FindProperty("_lifetime").floatValue = 0.18f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // Wraps a sprite-effects pack particle prefab in a pooled one-shot,
        // mirroring the Phase 1 DeathPoof approach.
        private static GameObject EnsurePackVfxWrapper(
            string name, string sourcePrefabName, int poolId, float scale, bool tintParticles, Color tint)
        {
            string path = $"{VfxPrefabFolder}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                string sourcePath = FindPackPrefabPath(sourcePrefabName);
                if (sourcePath == null)
                {
                    Debug.LogError($"Phase2: pack VFX prefab '{sourcePrefabName}' not found.");
                    return null;
                }

                var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                var root = new GameObject(name);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                instance.transform.SetParent(root.transform, false);
                instance.transform.localScale = Vector3.one * scale;

                ParticleSystem rootSystem = instance.GetComponentInChildren<ParticleSystem>();
                PooledVfx pooledVfx = root.AddComponent<PooledVfx>();
                var serialized = new SerializedObject(pooledVfx);
                serialized.FindProperty("_poolId").intValue = poolId;
                serialized.FindProperty("_rootSystem").objectReferenceValue = rootSystem;
                serialized.FindProperty("_maxLifetime").floatValue = 2f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ParticleSystem[] systems = contents.GetComponentsInChildren<ParticleSystem>(true);
                for (int i = 0; i < systems.Length; i++)
                {
                    ParticleSystem.MainModule main = systems[i].main;
                    main.loop = false;
                    if (tintParticles)
                    {
                        main.startColor = tint;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static string FindPackPrefabPath(string prefabName)
        {
            string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab", new[] { SpriteEffectsRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == prefabName)
                {
                    return path;
                }
            }

            return null;
        }

        // ------------------------------------------------------------------
        // Enemy prefab: StatusEffectReceiver wiring.
        // ------------------------------------------------------------------
        private static void EnsureEnemyStatusReceiver(string prefabPath)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (!contents.TryGetComponent(out StatusEffectReceiver receiver))
                {
                    receiver = contents.AddComponent<StatusEffectReceiver>();
                }

                Transform body = contents.transform.Find("Body");
                var receiverSerialized = new SerializedObject(receiver);
                receiverSerialized.FindProperty("_health").objectReferenceValue = contents.GetComponent<HealthComponent>();
                receiverSerialized.FindProperty("_renderer").objectReferenceValue =
                    body != null ? body.GetComponent<SpriteRenderer>() : contents.GetComponentInChildren<SpriteRenderer>();
                receiverSerialized.ApplyModifiedPropertiesWithoutUndo();

                var controllerSerialized = new SerializedObject(contents.GetComponent<EnemyController>());
                controllerSerialized.FindProperty("_statusReceiver").objectReferenceValue = receiver;
                controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

                var contactSerialized = new SerializedObject(contents.GetComponent<DamageOnContact>());
                contactSerialized.FindProperty("_statusReceiver").objectReferenceValue = receiver;
                contactSerialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // ------------------------------------------------------------------
        // Scene wiring.
        // ------------------------------------------------------------------
        private static void ApplySceneChanges(Sprite auraSprite)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Post-open reloads (fake-null hazard after scene loads).
            auraSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/PollenAuraZone.png");

            GameObject playerGo = GameObject.Find("Player");

            // Aura visual child.
            Transform auraTransform = playerGo.transform.Find("PollenAura");
            GameObject auraGo;
            if (auraTransform == null)
            {
                auraGo = new GameObject("PollenAura");
                auraGo.transform.SetParent(playerGo.transform, false);
            }
            else
            {
                auraGo = auraTransform.gameObject;
            }

            if (!auraGo.TryGetComponent(out SpriteRenderer auraRenderer))
            {
                auraRenderer = auraGo.AddComponent<SpriteRenderer>();
            }

            auraRenderer.sprite = auraSprite;
            auraRenderer.sortingOrder = -1;
            auraRenderer.enabled = false;

            // Active skill manager.
            if (!playerGo.TryGetComponent(out ActiveSkillManager skillManager))
            {
                skillManager = playerGo.AddComponent<ActiveSkillManager>();
            }

            var managerSerialized = new SerializedObject(skillManager);
            managerSerialized.FindProperty("_stats").objectReferenceValue = playerGo.GetComponent<PlayerStats>();
            managerSerialized.FindProperty("_targeter").objectReferenceValue =
                playerGo.GetComponent<Combat.NearestEnemyTargeter>();
            managerSerialized.FindProperty("_auraVisual").objectReferenceValue = auraRenderer;
            managerSerialized.ApplyModifiedPropertiesWithoutUndo();

            // Pools for every new pooled prefab.
            GameObject bootstrapGo = GameObject.Find("GameBootstrap");
            var bootstrapSerialized = new SerializedObject(bootstrapGo.GetComponent<GameBootstrap>());
            SerializedProperty pools = bootstrapSerialized.FindProperty("_pools");
            EnsurePoolEntry(pools, PoolIds.SkillStinger, LoadPrefab("SkillStinger"), 24, 64);
            EnsurePoolEntry(pools, PoolIds.SkillLance, LoadPrefab("SkillLance"), 6, 16);
            EnsurePoolEntry(pools, PoolIds.SkillHoneyGlob, LoadPrefab("HoneyGlobProjectile"), 4, 12);
            EnsurePoolEntry(pools, PoolIds.SkillEmberBolt, LoadPrefab("EmberBolt"), 6, 16);
            EnsurePoolEntry(pools, PoolIds.HoneyPuddle, LoadPrefab("HoneyPuddle"), 4, 10);
            EnsurePoolEntry(pools, PoolIds.ZapArcVfx, LoadPrefab("ZapArc"), 16, 40);
            EnsurePoolEntry(pools, PoolIds.EmberExplosionVfx,
                AssetDatabase.LoadAssetAtPath<GameObject>($"{VfxPrefabFolder}/EmberExplosion.prefab"), 6, 20);
            EnsurePoolEntry(pools, PoolIds.HoneySplashVfx,
                AssetDatabase.LoadAssetAtPath<GameObject>($"{VfxPrefabFolder}/HoneySplash.prefab"), 4, 12);
            bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

            // Level-up controller: active-skill manager hookup.
            GameObject levelUpPanel = GameObject.Find("LevelUpPanel");
            if (levelUpPanel != null)
            {
                var controllerSerialized = new SerializedObject(levelUpPanel.GetComponent<LevelUpUIController>());
                controllerSerialized.FindProperty("_activeSkillManager").objectReferenceValue = skillManager;
                controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

                // Phase 1 created these TMPs with the project default NoWrap;
                // Phase 2's longer skill descriptions need real word wrapping.
                var texts = levelUpPanel.GetComponentsInChildren<TMPro.TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i].textWrappingMode != TMPro.TextWrappingModes.Normal)
                    {
                        texts[i].textWrappingMode = TMPro.TextWrappingModes.Normal;
                        EditorUtility.SetDirty(texts[i]);
                    }
                }
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static GameObject LoadPrefab(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{SkillPrefabFolder}/{name}.prefab");
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

        // ------------------------------------------------------------------
        // Sprite generation (same conventions as the Phase 1 builder).
        // ------------------------------------------------------------------
        private static Sprite CreatePixelSprite(
            string assetName, string[] rows, Color32 outline, Color32 body, Color32 highlight)
        {
            int height = rows.Length;
            int width = rows[0].Length;
            var pixels = new Color32[width * height];
            Color32 clear = new Color32(0, 0, 0, 0);

            for (int y = 0; y < height; y++)
            {
                string row = rows[y];
                for (int x = 0; x < width; x++)
                {
                    // rows[] is authored top-to-bottom; textures are bottom-up.
                    int targetY = height - 1 - y;
                    Color32 color = clear;
                    switch (row[x])
                    {
                        case 'k': color = outline; break;
                        case 'g': color = body; break;
                        case 'w': color = highlight; break;
                    }

                    pixels[(targetY * width) + x] = color;
                }
            }

            return SavePixelPng(assetName, pixels, width, height);
        }

        // Filled translucent circle with a solid rim, for zone/aura visuals.
        // 32px @ PPU 16 = 2 world units in diameter = radius 1 at scale 1.
        private static Sprite CreateCircleSprite(string assetName, int diameter, Color32 fill, Color32 rim)
        {
            var pixels = new Color32[diameter * diameter];
            float center = (diameter - 1) / 2f;
            float outerRadius = diameter / 2f - 0.5f;
            float rimInnerRadius = outerRadius - 1.5f;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));

                    Color32 color = new Color32(0, 0, 0, 0);
                    if (distance <= rimInnerRadius)
                    {
                        color = fill;
                    }
                    else if (distance <= outerRadius)
                    {
                        color = rim;
                    }

                    pixels[(y * diameter) + x] = color;
                }
            }

            return SavePixelPng(assetName, pixels, diameter, diameter);
        }

        private static Sprite SavePixelPng(string assetName, Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            string pngPath = $"Assets/Sprites/{assetName}.png";
            System.IO.File.WriteAllBytes(pngPath, png);
            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 16f;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        }

        private static Sprite LoadIconSprite(string iconFileName)
        {
            string iconPath = $"{IconFolder}/{iconFileName}";
            var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Phase2: skill icon not found at {iconPath}.");
                return null;
            }

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(assetFolderPath)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(assetFolderPath);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
