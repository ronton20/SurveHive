using System.Collections.Generic;
using System.Text;
using SurveHive.Progression;
using TMPro;
using UnityEngine;

namespace SurveHive.UI
{
    /// <summary>
    /// Renders the player's current run build (owned power-ups grouped by lane,
    /// each with its level) into a single text block for the pause menu
    /// (Combat 2.0 1F). Refreshed on demand when the panel opens.
    /// </summary>
    public sealed class OwnedPowerUpsView : MonoBehaviour
    {
        [SerializeField] private LevelUpUIController _levelUp;
        [SerializeField] private TMP_Text _text;

        private readonly List<OwnedPowerUp> _owned = new List<OwnedPowerUp>(16);
        private readonly StringBuilder _builder = new StringBuilder(512);

        // Lane header colours (match the offer-card banners).
        private const string PassiveHex = "#4A7BB5";
        private const string EnhancementHex = "#F5A524";
        private const string AbilityHex = "#7B2E8C";

        public void Refresh()
        {
            if (_levelUp == null || _text == null)
            {
                return;
            }

            _levelUp.GetOwnedPowerUps(_owned);
            _builder.Clear();

            AppendLane("PASSIVES", PassiveHex, PowerUpLane.Passive);
            AppendLane("ENHANCEMENTS", EnhancementHex, PowerUpLane.Enhancement);
            AppendLane("ABILITIES", AbilityHex, PowerUpLane.Ability);

            if (_builder.Length == 0)
            {
                _builder.Append("No power-ups yet.");
            }

            _text.text = _builder.ToString();
        }

        private void AppendLane(string title, string colorHex, PowerUpLane lane)
        {
            int owned = 0;
            for (int i = 0; i < _owned.Count; i++)
            {
                if (_owned[i].Lane == lane)
                {
                    owned++;
                }
            }

            _builder.Append("<b><color=").Append(colorHex).Append('>');
            _builder.Append(title).Append("  ").Append(owned).Append('/').Append(_levelUp.GetLaneCap(lane));
            _builder.Append("</color></b>\n");

            for (int i = 0; i < _owned.Count; i++)
            {
                OwnedPowerUp p = _owned[i];
                if (p.Lane != lane)
                {
                    continue;
                }

                _builder.Append("  <color=").Append(ElementHex(p.Element)).Append('>');
                _builder.Append(p.Name).Append("</color>  <color=#9C8B6E>Lv ").Append(p.Level).Append("</color>\n");
            }

            _builder.Append('\n');
        }

        private static string ElementHex(SkillElement element)
        {
            switch (element)
            {
                case SkillElement.Fire:
                    return "#F56129";
                case SkillElement.Poison:
                    return "#7DB517";
                case SkillElement.Electric:
                    return "#FFE34A";
                case SkillElement.Frost:
                    return "#5AC7E8";
                case SkillElement.Honey:
                    return "#FFC20A";
                default:
                    return "#E8D8A0";
            }
        }
    }
}
