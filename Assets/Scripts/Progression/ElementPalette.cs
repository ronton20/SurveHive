using UnityEngine;

namespace SurveHive.Progression
{
    /// <summary>
    /// The one place element display colors live — card gems, the set-tier HUD,
    /// and any future element cue read from here so the language stays coherent.
    /// </summary>
    public static class ElementPalette
    {
        // Rich-text forms ("#F56129"), cached once so hot-ish UI rebuilds never
        // re-format colors.
        private static readonly string[] _hex = BuildHexTable();

        /// <summary>TMP rich-text hex ("#RRGGBB") for the element's color.</summary>
        public static string GetHex(SkillElement element)
        {
            return _hex[(int)element];
        }

        private static string[] BuildHexTable()
        {
            var table = new string[6];
            for (int i = 0; i < table.Length; i++)
            {
                table[i] = "#" + ColorUtility.ToHtmlStringRGB(GetColor((SkillElement)i));
            }

            return table;
        }

        public static Color GetColor(SkillElement element)
        {
            switch (element)
            {
                case SkillElement.Fire:
                    return new Color(0.96f, 0.38f, 0.17f);
                case SkillElement.Poison:
                    return new Color(0.49f, 0.71f, 0.09f);
                case SkillElement.Electric:
                    return new Color(1f, 0.89f, 0.29f);
                case SkillElement.Frost:
                    return new Color(0.35f, 0.78f, 0.91f);
                case SkillElement.Honey:
                    return new Color(1f, 0.76f, 0.04f);
                // Physical: neutral wax/silver.
                default:
                    return new Color(0.82f, 0.82f, 0.78f);
            }
        }
    }
}
