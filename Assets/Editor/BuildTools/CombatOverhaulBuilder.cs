using SurveHive.Combat.Status;
using SurveHive.Data;
using SurveHive.Progression;
using SurveHive.UI;
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

            for (int i = 0; i < ChoiceCount; i++)
            {
                Transform choice = panel.Find($"Choice{i}");
                if (choice == null)
                {
                    Debug.LogError($"Combat 2.0 1A: Choice{i} not found — run the Phase 1 look pass first.");
                    return;
                }

                BuildCard(choice, font, out banners[i], out bannerBackgrounds[i], out elementGems[i]);
            }

            var serialized = new SerializedObject(controller);
            WireArray(serialized, "_choiceBanners", banners);
            WireArray(serialized, "_choiceBannerBackgrounds", bannerBackgrounds);
            WireArray(serialized, "_choiceElementGems", elementGems);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Combat 2.0 1A: power-up card banners built + wired.");
        }

        // Header ribbon (lane) across the card top + a small element gem top-right.
        // Runtime colors/labels come from LevelUpUIController; placeholders here.
        private static void BuildCard(
            Transform choice, TMP_FontAsset font,
            out TMP_Text banner, out Image bannerBackground, out Image elementGem)
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
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
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
        // ActiveSkillManager). Adds three abilities that reuse existing pools:
        // Frost Nova (radial freeze), Ball Lightning (radial stun), Honey Bomb
        // (homing explosion + slow). Idempotent.
        // ------------------------------------------------------------------
        [MenuItem("SurveHive/Combat 2.0/1E - Add Abilities (Frost Nova, Ball Lightning, Honey Bomb)")]
        public static void AddAbilities()
        {
            if (!AssetDatabase.IsValidFolder(ActivesFolder))
            {
                AssetDatabase.CreateFolder(SkillsFolder, "Actives");
            }

            // Reused pools: 8 = stinger (radial), 11 = ember bolt, 14 = ember blast VFX.
            ActiveSkillSO frostNova = EnsureActiveSkill(
                ActivesFolder + "/FrostNova.asset", "FrostNova", "Frost Nova",
                ActiveSkillBehavior.RadialVolley, new[]
                {
                    new LevelRow { Damage = 6, Cooldown = 4.5f, Count = 6, StatusChance = 20 },
                    new LevelRow { Damage = 8, Cooldown = 4.2f, Count = 7, StatusChance = 25 },
                    new LevelRow { Damage = 10, Cooldown = 3.9f, Count = 8, StatusChance = 30 },
                    new LevelRow { Damage = 12, Cooldown = 3.6f, Count = 10, StatusChance = 35 },
                    new LevelRow { Damage = 15, Cooldown = 3.3f, Count = 12, StatusChance = 40 },
                }, 9f, 7f, 8, -1, -1, StatusEffectType.Freeze, 12f, 1.2f);

            ActiveSkillSO ballLightning = EnsureActiveSkill(
                ActivesFolder + "/BallLightning.asset", "BallLightning", "Ball Lightning",
                ActiveSkillBehavior.RadialVolley, new[]
                {
                    new LevelRow { Damage = 7, Cooldown = 4f, Count = 5, StatusChance = 15 },
                    new LevelRow { Damage = 9, Cooldown = 3.7f, Count = 6, StatusChance = 18 },
                    new LevelRow { Damage = 11, Cooldown = 3.4f, Count = 7, StatusChance = 22 },
                    new LevelRow { Damage = 14, Cooldown = 3.1f, Count = 8, StatusChance = 26 },
                    new LevelRow { Damage = 17, Cooldown = 2.8f, Count = 10, StatusChance = 30 },
                }, 9f, 7f, 8, -1, -1, StatusEffectType.Stun, 0f, 1f);

            ActiveSkillSO honeyBomb = EnsureActiveSkill(
                ActivesFolder + "/HoneyBomb.asset", "HoneyBomb", "Honey Bomb",
                ActiveSkillBehavior.HomingBolt, new[]
                {
                    new LevelRow { Damage = 12, Cooldown = 3.5f, Count = 1, Area = 2f, StatusChance = 60 },
                    new LevelRow { Damage = 16, Cooldown = 3.2f, Count = 1, Area = 2.3f, StatusChance = 65 },
                    new LevelRow { Damage = 20, Cooldown = 2.9f, Count = 1, Area = 2.6f, StatusChance = 70 },
                    new LevelRow { Damage = 25, Cooldown = 2.6f, Count = 1, Area = 2.9f, StatusChance = 80 },
                    new LevelRow { Damage = 31, Cooldown = 2.3f, Count = 1, Area = 3.2f, StatusChance = 90 },
                }, 7f, 11f, 11, 14, -1, StatusEffectType.Slow, 0.4f, 2.5f);

            SkillDefinitionSO frostCard = EnsureSkill(
                SkillsFolder + "/FrostNovaCard.asset", "FrostNovaCard", "Frost Nova",
                "Blasts a ring of frost shards that can freeze enemies solid.",
                SkillEffectType.ActiveSkill, PowerUpLane.Ability, SkillElement.Frost,
                0f, 5, SkillRarity.Rare, "Icon_PictoIcon_Star", frostNova);

            SkillDefinitionSO ballCard = EnsureSkill(
                SkillsFolder + "/BallLightningCard.asset", "BallLightningCard", "Ball Lightning",
                "Hurls a ring of crackling orbs that can stun enemies.",
                SkillEffectType.ActiveSkill, PowerUpLane.Ability, SkillElement.Electric,
                0f, 5, SkillRarity.Rare, "Icon_PictoIcon_Energy", ballLightning);

            SkillDefinitionSO honeyCard = EnsureSkill(
                SkillsFolder + "/HoneyBombCard.asset", "HoneyBombCard", "Honey Bomb",
                "Lobs a homing honey bomb that explodes and slows everything caught in it.",
                SkillEffectType.ActiveSkill, PowerUpLane.Ability, SkillElement.Honey,
                0f, 5, SkillRarity.Rare, "Icon_PictoIcon_Heart", honeyBomb);

            RegisterInDatabase(frostCard, ballCard, honeyCard);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Combat 2.0 1E: Frost Nova + Ball Lightning + Honey Bomb added; radial burst pierces.");
        }

        private static ActiveSkillSO EnsureActiveSkill(
            string assetPath, string id, string displayName, ActiveSkillBehavior behavior,
            LevelRow[] levels, float projectileSpeed, float range,
            int projectilePoolId, int impactVfxPoolId, int zonePoolId,
            StatusEffectType statusType, float statusPotency, float statusDuration)
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
            so.FindProperty("_projectileSpeed").floatValue = projectileSpeed;
            so.FindProperty("_range").floatValue = range;
            so.FindProperty("_projectilePoolId").intValue = projectilePoolId;
            so.FindProperty("_impactVfxPoolId").intValue = impactVfxPoolId;
            so.FindProperty("_zonePoolId").intValue = zonePoolId;
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
    }
}
