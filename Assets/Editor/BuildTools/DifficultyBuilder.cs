using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Spawning;
using SurveHive.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// PLAN.md Phase 1B — working stage difficulty. Creates the difficulty
    /// tier table asset, un-fixes the Phase-4B dropdown seam on the world
    /// select panel (4 tiers + placeholder icons + a DifficultySelectUI
    /// controller), and wires the Beehive scene's spawner/session to the tier
    /// table. Additive and idempotent over the existing scenes/assets — and it
    /// never overwrites an existing 4-row tier table, so balance tuning done
    /// in the inspector survives re-runs.
    /// </summary>
    public static class DifficultyBuilder
    {
        private const string DifficultyAssetPath = "Assets/Data/Progression/DifficultySettings.asset";
        private const string PersistentStorePath = "Assets/Data/Progression/PersistentMetaProgressionStore.asset";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string RunScenePath = "Assets/Scenes/Beehive.unity";
        private const string IconFolder = "Assets/ThirdParty/IconsTemp/Icons/PictoIcon_128";

        // Menu palette, matching Phase4MetaAndMenusBuilder.
        private static readonly Color HoneyGold = new Color(1f, 0.765f, 0.043f);
        private static readonly Color CombBrown = new Color(0.549f, 0.353f, 0.169f);
        private static readonly Color DeepBrown = new Color(0.227f, 0.141f, 0.086f);

        [MenuItem("SurveHive/Apply Difficulty (Phase 1B)")]
        public static void Apply()
        {
            EnsureDifficultyAsset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ApplyMenuSceneChanges();
            ApplyRunSceneChanges();

            Debug.Log("SurveHive difficulty (Phase 1B) build complete.");
        }

        // ------------------------------------------------------------------
        // Tier table asset. Baseline values: enemy toughness up, honey up as
        // compensation (Easy trades honey away for survival). Tunable data —
        // only written when the table isn't already 4 rows.
        // ------------------------------------------------------------------
        private static void EnsureDifficultyAsset()
        {
            var difficulty = AssetDatabase.LoadAssetAtPath<DifficultySO>(DifficultyAssetPath);
            if (difficulty == null)
            {
                difficulty = ScriptableObject.CreateInstance<DifficultySO>();
                AssetDatabase.CreateAsset(difficulty, DifficultyAssetPath);
            }

            var so = new SerializedObject(difficulty);
            SerializedProperty tiers = so.FindProperty("_tiers");
            if (tiers.arraySize != 4)
            {
                tiers.arraySize = 4;
                WriteTier(tiers.GetArrayElementAtIndex(0), DifficultyTier.Easy, "EASY",
                    "Icon_PictoIcon_Feather.Png", health: 0.75f, damage: 0.75f, spawnRate: 1f, honey: 0.75f);
                WriteTier(tiers.GetArrayElementAtIndex(1), DifficultyTier.Normal, "NORMAL",
                    "Icon_PictoIcon_Star.Png", health: 1f, damage: 1f, spawnRate: 1f, honey: 1f);
                WriteTier(tiers.GetArrayElementAtIndex(2), DifficultyTier.Hard, "HARD",
                    "Icon_PictoIcon_Sword.Png", health: 1.5f, damage: 1.4f, spawnRate: 1.15f, honey: 1.5f);
                WriteTier(tiers.GetArrayElementAtIndex(3), DifficultyTier.Extreme, "EXTREME",
                    "Icon_PictoIcon_Skull.Png", health: 2.25f, damage: 1.9f, spawnRate: 1.3f, honey: 2.25f);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(difficulty);
            }
        }

        private static void WriteTier(
            SerializedProperty row, DifficultyTier tier, string displayName, string iconFile,
            float health, float damage, float spawnRate, float honey)
        {
            row.FindPropertyRelative("tier").intValue = (int)tier;
            row.FindPropertyRelative("displayName").stringValue = displayName;
            row.FindPropertyRelative("icon").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/{iconFile}");
            row.FindPropertyRelative("enemyHealthMultiplier").floatValue = health;
            row.FindPropertyRelative("enemyDamageMultiplier").floatValue = damage;
            row.FindPropertyRelative("spawnRateMultiplier").floatValue = spawnRate;
            row.FindPropertyRelative("honeyGainMultiplier").floatValue = honey;
        }

        // ------------------------------------------------------------------
        // MainMenu scene: unlock the dropdown seam, give its template room for
        // 4 icon rows, and drop the DifficultySelectUI controller on the panel.
        // ------------------------------------------------------------------
        private static void ApplyMenuSceneChanges()
        {
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            // Loaded AFTER OpenScene: the single-mode scene switch unloads
            // unreferenced assets, and wiring an unloaded (fake-null) instance
            // serializes as fileID 0 without erroring.
            DifficultySO difficulty = LoadDifficultyAsset();
            if (difficulty == null)
            {
                return;
            }

            GameObject canvas = GameObject.Find("Canvas");
            Transform worldPanel = canvas != null ? canvas.transform.Find("WorldSelectPanel") : null;
            Transform dropdownTransform = worldPanel != null ? worldPanel.Find("DifficultyDropdown") : null;
            if (dropdownTransform == null)
            {
                Debug.LogError("DifficultyBuilder: WorldSelectPanel/DifficultyDropdown not found in MainMenu.");
                return;
            }

            var dropdown = dropdownTransform.GetComponent<TMP_Dropdown>();
            dropdown.interactable = true;

            // Bake the options (DifficultySelectUI re-fills them at runtime;
            // baking keeps the serialized scene readable and validator-checkable).
            dropdown.options.Clear();
            for (int i = 0; i < difficulty.TierCount; i++)
            {
                DifficultySO.TierSettings tier = difficulty.GetTierAt(i);
                dropdown.options.Add(new TMP_Dropdown.OptionData(tier.displayName, tier.icon, Color.white));
            }

            dropdown.SetValueWithoutNotify((int)DifficultyTier.Normal);

            StyleDropdownTemplate(dropdown);

            DifficultySelectUI select = worldPanel.GetComponentInChildren<DifficultySelectUI>(true);
            if (select == null)
            {
                select = worldPanel.gameObject.AddComponent<DifficultySelectUI>();
            }

            var store = AssetDatabase.LoadAssetAtPath<PersistentMetaProgressionStoreSO>(PersistentStorePath);
            var selectSo = new SerializedObject(select);
            selectSo.FindProperty("_dropdown").objectReferenceValue = dropdown;
            selectSo.FindProperty("_difficulty").objectReferenceValue = difficulty;
            selectSo.FindProperty("_store").objectReferenceValue = store;
            selectSo.ApplyModifiedPropertiesWithoutUndo();

            dropdown.RefreshShownValue();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // The TMP default template is sized for 20px text-only rows; grow it to
        // four 76px rows and add icon slots (item + caption) that TMP_Dropdown
        // fills from OptionData.image.
        private static void StyleDropdownTemplate(TMP_Dropdown dropdown)
        {
            var template = (RectTransform)dropdown.transform.Find("Template");
            template.sizeDelta = new Vector2(template.sizeDelta.x, 316f);
            template.GetComponent<Image>().color = DeepBrown;

            var item = (RectTransform)template.Find("Viewport/Content/Item");
            item.sizeDelta = new Vector2(item.sizeDelta.x, 76f);
            var content = (RectTransform)item.parent;
            content.sizeDelta = new Vector2(content.sizeDelta.x, 76f);

            var itemBackground = (RectTransform)item.Find("Item Background");
            itemBackground.GetComponent<Image>().color = CombBrown;

            var checkmark = (RectTransform)item.Find("Item Checkmark");
            checkmark.sizeDelta = new Vector2(32f, 32f);
            checkmark.anchoredPosition = new Vector2(28f, 0f);
            checkmark.GetComponent<Image>().color = HoneyGold;

            RectTransform itemIcon = EnsureIconSlot(item, "Item Icon", new Vector2(78f, 0f));
            dropdown.itemImage = itemIcon.GetComponent<Image>();

            var itemLabel = (RectTransform)item.Find("Item Label");
            itemLabel.offsetMin = new Vector2(114f, itemLabel.offsetMin.y);

            RectTransform captionIcon = EnsureIconSlot((RectTransform)dropdown.transform, "CaptionIcon", new Vector2(48f, 0f));
            dropdown.captionImage = captionIcon.GetComponent<Image>();

            var captionLabel = (RectTransform)dropdown.transform.Find("Label");
            captionLabel.offsetMin = new Vector2(84f, captionLabel.offsetMin.y);
        }

        private static RectTransform EnsureIconSlot(RectTransform parent, string name, Vector2 anchoredPosition)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return (RectTransform)existing;
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(48f, 48f);
            rect.anchoredPosition = anchoredPosition;

            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            return rect;
        }

        // ------------------------------------------------------------------
        // Beehive scene: point the spawner and session at the tier table.
        // ------------------------------------------------------------------
        private static void ApplyRunSceneChanges()
        {
            EditorSceneManager.OpenScene(RunScenePath, OpenSceneMode.Single);

            // See ApplyMenuSceneChanges — must load after the scene switch.
            DifficultySO difficulty = LoadDifficultyAsset();
            if (difficulty == null)
            {
                return;
            }

            var spawner = Object.FindAnyObjectByType<EnemySpawner>(FindObjectsInactive.Include);
            var session = Object.FindAnyObjectByType<RunSession>(FindObjectsInactive.Include);
            if (spawner == null || session == null)
            {
                Debug.LogError("DifficultyBuilder: EnemySpawner or RunSession not found in Beehive scene.");
                return;
            }

            WireDifficulty(spawner, difficulty);
            WireDifficulty(session, difficulty);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static DifficultySO LoadDifficultyAsset()
        {
            var difficulty = AssetDatabase.LoadAssetAtPath<DifficultySO>(DifficultyAssetPath);
            if (difficulty == null)
            {
                Debug.LogError("DifficultyBuilder: DifficultySettings asset failed to load.");
            }

            return difficulty;
        }

        private static void WireDifficulty(Component target, DifficultySO difficulty)
        {
            var so = new SerializedObject(target);
            so.FindProperty("_difficulty").objectReferenceValue = difficulty;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
