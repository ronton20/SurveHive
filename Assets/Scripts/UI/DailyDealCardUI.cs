using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// One daily-deal card (PLAN 5E): cosmetic icon + name + description, the
    /// list price struck through next to the discounted deal price, and a BUY
    /// button. Purely a view — <see cref="DailyDealsUI"/> binds it and owns the
    /// transaction; a bought deal shows SOLD for the rest of the day.
    /// Menu-only path, so bind-time string work is fine.
    /// </summary>
    public sealed class DailyDealCardUI : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private Button _buyButton;
        [SerializeField] private TMP_Text _buyLabel;

        public Button BuyButton => _buyButton;

        public void Bind(Sprite icon, Color iconTint, string displayName, string description, string priceLine)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.color = iconTint;
                _iconImage.enabled = icon != null;
            }

            if (_nameText != null)
            {
                _nameText.text = displayName;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = description;
            }

            if (_priceText != null)
            {
                _priceText.text = priceLine;
            }
        }

        /// <summary>Sold deals grey out; unaffordable ones keep the BUY label but disable.</summary>
        public void SetState(bool sold, bool affordable, string buyLabel, string soldLabel)
        {
            if (_buyLabel != null)
            {
                _buyLabel.text = sold ? soldLabel : buyLabel;
            }

            if (_buyButton != null)
            {
                _buyButton.interactable = !sold && affordable;
            }
        }
    }
}
