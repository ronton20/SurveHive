using System.Text;
using SurveHive.Data;
using SurveHive.Progression;
using TMPro;
using UnityEngine;

namespace SurveHive.UI
{
    /// <summary>
    /// HUD line showing the active elemental set tiers ("FIRE II · HONEY I",
    /// element-colored via rich text). Hidden while no set is active. Rebuilds
    /// only on <see cref="ElementSets.OnChanged"/> — i.e. on a level-up pick,
    /// never per frame.
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
                if (tierIndex < 0)
                {
                    continue;
                }

                SetBonusSO bonus = ElementSets.GetBonus(element);
                if (_builder.Length > 0)
                {
                    _builder.Append("  ");
                }

                Color color = ElementPalette.GetColor(element);
                _builder.Append("<color=#");
                AppendHex(color.r);
                AppendHex(color.g);
                AppendHex(color.b);
                _builder.Append('>');
                _builder.Append(bonus != null && !string.IsNullOrEmpty(bonus.SetName)
                    ? bonus.SetName
                    : element.ToString());
                _builder.Append(' ');
                _builder.Append(RomanTier(tierIndex));
                _builder.Append("</color>");
            }

            _text.text = _builder.ToString();
        }

        private void AppendHex(float channel)
        {
            int value = Mathf.Clamp(Mathf.RoundToInt(channel * 255f), 0, 255);
            _builder.Append(HexDigit(value >> 4));
            _builder.Append(HexDigit(value & 0xF));
        }

        private static char HexDigit(int nibble)
        {
            return (char)(nibble < 10 ? '0' + nibble : 'A' + (nibble - 10));
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
