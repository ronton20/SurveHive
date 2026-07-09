using System.Collections.Generic;
using System.Text;
using SurveHive.Core;
using SurveHive.Data;
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

            AppendLane(Loc.Get(LocKeys.LanePassivePlural), PassiveHex, PowerUpLane.Passive);
            AppendLane(Loc.Get(LocKeys.LaneEnhancementPlural), EnhancementHex, PowerUpLane.Enhancement);
            AppendLane(Loc.Get(LocKeys.LaneAbilityPlural), AbilityHex, PowerUpLane.Ability);
            AppendSets();

            if (_builder.Length == 0)
            {
                _builder.Append(Loc.Get(LocKeys.OwnedEmpty));
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
                _builder.Append(p.Name).Append("</color>  <color=#9C8B6E>").Append(Loc.Get(LocKeys.OwnedLevelPrefix))
                    .Append(p.Level).Append("</color>\n");
            }

            _builder.Append('\n');
        }

        private static string ElementHex(SkillElement element)
        {
            return ElementPalette.GetHex(element);
        }

        // Phase 3C: every element the player has pieces in, with the active tier's
        // grant and the next threshold — the strategic view of the set system.
        private void AppendSets()
        {
            bool any = false;
            for (int i = 0; i < ElementSets.ElementCount; i++)
            {
                var element = (SkillElement)i;
                int pieces = ElementSets.GetPieces(element);
                SetBonusSO bonus = ElementSets.GetBonus(element);
                if (pieces <= 0 || bonus == null)
                {
                    continue;
                }

                if (!any)
                {
                    any = true;
                    _builder.Append("<b><color=#C9A227>").Append(Loc.Get(LocKeys.SetBonusesHeader))
                        .Append("</color></b>\n");
                }

                int tier = bonus.GetTierIndex(pieces);
                _builder.Append("  <color=").Append(ElementHex(element)).Append('>');
                _builder.Append(bonus.SetName).Append("</color>  <color=#9C8B6E>").Append(pieces)
                    .Append(Loc.Get(LocKeys.SetPiecesSuffix)).Append("</color>");

                if (tier >= 0)
                {
                    _builder.Append("  ").Append(bonus.GetTier(tier).Description);
                }

                if (tier + 1 < bonus.TierCount)
                {
                    SetBonusTier next = bonus.GetTier(tier + 1);
                    _builder.Append("  <color=#9C8B6E>").Append(Loc.Get(LocKeys.SetAtOpen)).Append(next.PiecesRequired)
                        .Append(": ").Append(next.Description).Append(")</color>");
                }

                _builder.Append('\n');
            }
        }
    }
}
