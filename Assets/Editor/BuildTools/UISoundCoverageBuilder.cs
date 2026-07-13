using SurveHive.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Additive, idempotent pass: guarantees every UI <see cref="Button"/> in both
    /// scenes carries a <see cref="UIClickSfx"/>, so it has click + hover audio.
    ///
    /// The MainMenu scene was authored before the <c>UIClickSfx</c> line was added
    /// to the menu builder's button factory and was (correctly) never rebuilt from
    /// scratch — so most of its buttons were silent. Rather than re-run the
    /// clobber-prone Phase4 builder, this sweeps the already-built scenes and fills
    /// the gaps. It only ever *adds* the component where missing; nothing is removed,
    /// reordered, or re-wired, so it's safe to re-run.
    /// </summary>
    public static class UISoundCoverageBuilder
    {
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Beehive.unity",
        };

        [MenuItem("SurveHive/Fix UI Sound Coverage")]
        public static void Apply()
        {
            foreach (string scenePath in Scenes)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                int added = 0;
                Button[] buttons = Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include);
                foreach (Button button in buttons)
                {
                    if (!button.TryGetComponent(out UIClickSfx _))
                    {
                        button.gameObject.AddComponent<UIClickSfx>();
                        EditorUtility.SetDirty(button.gameObject);
                        added++;
                    }
                }

                // Dropdowns aren't Buttons, so UIClickSfx can't ride them — give them
                // (and their option-list template item) the selectable-agnostic SFX.
                int dropdowns = 0;
                TMP_Dropdown[] dds = Object.FindObjectsByType<TMP_Dropdown>(
                    FindObjectsInactive.Include);
                foreach (TMP_Dropdown dd in dds)
                {
                    dropdowns += EnsureSelectableSfx(dd.gameObject);

                    // The spawned option items are clones of this template item.
                    if (dd.template != null)
                    {
                        Toggle item = dd.template.GetComponentInChildren<Toggle>(true);
                        if (item != null)
                        {
                            EnsureSelectableSfx(item.gameObject);
                        }
                    }

                    EditorUtility.SetDirty(dd.gameObject);
                }

                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log($"UISoundCoverageBuilder: {scenePath} — added UIClickSfx to {added} button(s), " +
                    $"UISelectableSfx to {dropdowns} dropdown(s).");
            }
        }

        // Adds UISelectableSfx if absent; returns 1 when it added one, else 0.
        private static int EnsureSelectableSfx(GameObject go)
        {
            if (go.TryGetComponent(out UISelectableSfx _))
            {
                return 0;
            }

            go.AddComponent<UISelectableSfx>();
            return 1;
        }
    }
}
