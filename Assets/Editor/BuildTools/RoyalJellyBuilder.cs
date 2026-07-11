using SurveHive.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// PLAN 5B — Royal Jelly premium currency: adds the jelly balance readout to
    /// the shop header (to the right of the honey balance on the same row) and
    /// wires it into <see cref="MetaShopUI"/>, then re-runs the localization
    /// pass so the new 5B keys land in the StringTable. Additive and idempotent
    /// — find-or-create on its own JellyText node, never touches the chrome
    /// owned by Phase4/MetaShopTabs builders.
    /// </summary>
    public static class RoyalJellyBuilder
    {
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string FontAssetPath = "Assets/ThirdParty/Fonts/BoldPixels/Assets/font/BoldPixels SDF.asset";

        // Pearly cream — Royal Jelly's signature color (ASSET_GENERATION §2.8),
        // deliberately paler than the honey balance's Amber.
        private static readonly Color RoyalCream = new Color(0.96f, 0.93f, 0.8f);

        [MenuItem("SurveHive/Add Royal Jelly Counter (Phase 5B)")]
        public static void Apply()
        {
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            // Load assets after the scene switch — pre-switch refs wire as fileID 0.
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

            GameObject canvas = GameObject.Find("Canvas");
            Transform shopPanel = canvas != null ? canvas.transform.Find("ShopPanel") : null;
            var shopUi = shopPanel != null ? shopPanel.GetComponent<MetaShopUI>() : null;
            if (shopUi == null)
            {
                Debug.LogError("RoyalJellyBuilder: ShopPanel/MetaShopUI not found in MainMenu.");
                return;
            }

            TMP_Text jellyText = EnsureJellyText(shopPanel, font);

            var so = new SerializedObject(shopUi);
            so.FindProperty("_jellyText").objectReferenceValue = jellyText;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(shopUi);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            // Append the 5B keys (shop.jelly_prefix / results.jelly_earned) to
            // the StringTable — the localization pass is append-only/idempotent.
            LocalizationBuilder.Apply();

            Debug.Log("SurveHive Royal Jelly counter build complete.");
        }

        // Same row as the honey balance (MetaShopTabsBuilder pins BalanceText to
        // the panel top at -120), offset right so the two balances read as one
        // header line. Style mirrors BalanceText (42pt BoldPixels).
        private static TMP_Text EnsureJellyText(Transform shopPanel, TMP_FontAsset font)
        {
            Transform existing = shopPanel.Find("JellyText");
            GameObject textGo = existing != null ? existing.gameObject : new GameObject("JellyText", typeof(RectTransform));

            var rect = (RectTransform)textGo.transform;
            rect.SetParent(shopPanel, false);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(560f, -120f);
            rect.sizeDelta = new Vector2(500f, 60f);

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = textGo.AddComponent<TextMeshProUGUI>();
            }

            tmp.font = font;
            tmp.fontSize = 42f;
            tmp.color = RoyalCream;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            // Rest-state placeholder; MetaShopUI paints the live glyph+number.
            tmp.text = SurveHive.Core.CurrencyGlyphs.Jelly + "0";
            return tmp;
        }
    }
}
