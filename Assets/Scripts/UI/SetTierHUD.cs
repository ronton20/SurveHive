using System.Text;
using SurveHive.Data;
using SurveHive.Progression;
using TMPro;
using UnityEngine;

namespace SurveHive.UI
{
    /// <summary>
    /// Active elemental set tiers with what each grants, one element-colored
    /// line per set ("WILDFIRE I — Burns last 30% longer"). Lives on the
    /// level-up offer panel (set state only matters when choosing picks).
    /// Empty while no set is active; rebuilds only on
    /// <see cref="ElementSets.OnChanged"/> — i.e. on a pick, never per frame.
    /// </summary>
    public sealed class SetTierHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        private readonly StringBuilder _builder = new StringBuilder(96);

        private void OnEnable()
        {
            ElementSets.OnChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            ElementSets.OnChanged -= Refresh;
        }

        private void Refresh()
        {
            _builder.Clear();

            for (int i = 0; i < ElementSets.ElementCount; i++)
            {
                var element = (SkillElement)i;
                int tierIndex = ElementSets.GetTierIndex(element);
                SetBonusSO bonus = ElementSets.GetBonus(element);
                // No config = nothing to describe; never show an unconfigured set.
                if (tierIndex < 0 || bonus == null)
                {
                    continue;
                }

                if (_builder.Length > 0)
                {
                    _builder.Append('\n');
                }

                _builder.Append("<color=");
                _builder.Append(ElementPalette.GetHex(element));
                _builder.Append('>');
                _builder.Append(bonus.SetName);
                _builder.Append(' ');
                _builder.Append(RomanTier(tierIndex));
                _builder.Append("</color> — ");
                _builder.Append(bonus.GetTier(tierIndex).Description);
            }

            _text.text = _builder.ToString();
        }

        private static string RomanTier(int tierIndex)
        {
            switch (tierIndex)
            {
                case 0:
                    return "I";
                case 1:
                    return "II";
                default:
                    return "III";
            }
        }
    }
}
