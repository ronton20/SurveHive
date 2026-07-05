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
        private const string DatabasePath = SkillsFolder + "/SkillDatabase.asset";
        private const int ChoiceCount = 3;

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
            float magnitude, int maxLevel, SkillRarity rarity, string iconName)
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
