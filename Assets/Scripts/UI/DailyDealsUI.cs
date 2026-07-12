using System;
using System.Collections.Generic;
using System.Text;
using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SurveHive.UI
{
    /// <summary>
    /// The rotating cosmetics shop panel (PLAN 5E). Each local day features up
    /// to three not-yet-owned cosmetics at <see cref="RotatingShop"/>'s
    /// discount; the picks are date-seeded and frozen into the save on first
    /// sight, so they hold across restarts and buying one never re-rolls the
    /// rest (a bought deal shows SOLD until rollover). A once-per-second
    /// countdown shows time to the next rotation and re-picks live when
    /// midnight passes with the panel open. Buying spends Royal Jelly through
    /// <see cref="CosmeticShop"/> at the deal price and auto-equips, exactly
    /// like Hive Style. Menu-only path, so refresh-time string/UI work is fine.
    /// </summary>
    public sealed class DailyDealsUI : MonoBehaviour
    {
        [SerializeField] private MetaProgressionStoreSO _store;
        [SerializeField] private CosmeticCatalogSO _catalog;
        [SerializeField] private TMP_Text _jellyText;
        [SerializeField] private TMP_Text _timerText;
        // Shown instead of cards once the whole catalog is owned.
        [SerializeField] private TMP_Text _allOwnedText;
        [SerializeField] private DailyDealCardUI[] _cards;
        // White square used as the swatch icon for color entries.
        [SerializeField] private Sprite _swatchSprite;

        private readonly List<CosmeticSO> _deals = new List<CosmeticSO>();
        private readonly List<string> _pickBuffer = new List<string>();
        private readonly StringBuilder _timerBuilder = new StringBuilder(32);
        private UnityAction[] _buyHandlers;
        private string _timerPrefix;
        private string _buyLabel;
        private string _soldLabel;
        private int _boundDay = -1;
        private int _lastShownSeconds = -1;
        private float _nextTimerTick;

        private void Awake()
        {
            _timerPrefix = Loc.Get(LocKeys.DealsTimerPrefix);
            _buyLabel = Loc.Get(LocKeys.DealsBuy);
            _soldLabel = Loc.Get(LocKeys.DealsSold);

            _buyHandlers = new UnityAction[_cards != null ? _cards.Length : 0];
            for (int i = 0; i < _buyHandlers.Length; i++)
            {
                if (_cards[i] == null || _cards[i].BuyButton == null)
                {
                    continue;
                }

                int captured = i;
                _buyHandlers[i] = () => Buy(captured);
                _cards[i].BuyButton.onClick.AddListener(_buyHandlers[i]);
            }
        }

        private void OnDestroy()
        {
            if (_buyHandlers == null)
            {
                return;
            }

            for (int i = 0; i < _buyHandlers.Length; i++)
            {
                if (_buyHandlers[i] != null && _cards[i] != null && _cards[i].BuyButton != null)
                {
                    _cards[i].BuyButton.onClick.RemoveListener(_buyHandlers[i]);
                }
            }
        }

        private void OnEnable()
        {
            // OnEnable can fire before Awake on the very first activation; guard it.
            if (_buyHandlers == null)
            {
                return;
            }

            EnsureDeals();
            RefreshAll();
            _lastShownSeconds = -1;
            _nextTimerTick = 0f;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextTimerTick)
            {
                return;
            }

            _nextTimerTick = Time.unscaledTime + 0.25f;
            RefreshTimer();
        }

        // Picks (and persists) fresh deals when the saved rotation is from
        // another day, then resolves the frozen ids against the catalog.
        private void EnsureDeals()
        {
            _deals.Clear();
            if (_store == null || _catalog == null)
            {
                return;
            }

            int day = RotatingShop.DayStamp(DateTime.Now);
            _boundDay = day;

            if (_store.GetDailyDealDay() != day)
            {
                _pickBuffer.Clear();
                CosmeticSO[] cosmetics = _catalog.Cosmetics;
                var candidates = new List<string>(cosmetics != null ? cosmetics.Length : 0);
                if (cosmetics != null)
                {
                    for (int i = 0; i < cosmetics.Length; i++)
                    {
                        if (cosmetics[i] != null && !string.IsNullOrEmpty(cosmetics[i].CosmeticId)
                            && !_store.IsCosmeticOwned(cosmetics[i].CosmeticId))
                        {
                            candidates.Add(cosmetics[i].CosmeticId);
                        }
                    }
                }

                RotatingShop.Pick(candidates, day, _pickBuffer);
                _store.SetDailyDeals(day, _pickBuffer);
            }

            string[] dealIds = _store.GetDailyDealIds();
            for (int i = 0; i < dealIds.Length; i++)
            {
                CosmeticSO cosmetic = _catalog.FindById(dealIds[i]);
                if (cosmetic != null)
                {
                    _deals.Add(cosmetic);
                }
            }
        }

        private void RefreshAll()
        {
            if (_jellyText != null && _store != null)
            {
                _jellyText.text = CurrencyGlyphs.Jelly + _store.BankedJelly;
            }

            if (_cards != null)
            {
                for (int i = 0; i < _cards.Length; i++)
                {
                    if (_cards[i] == null)
                    {
                        continue;
                    }

                    if (i >= _deals.Count)
                    {
                        _cards[i].gameObject.SetActive(false);
                        continue;
                    }

                    _cards[i].gameObject.SetActive(true);
                    BindCard(_cards[i], _deals[i]);
                }
            }

            if (_allOwnedText != null)
            {
                _allOwnedText.gameObject.SetActive(_deals.Count == 0);
            }
        }

        private void BindCard(DailyDealCardUI card, CosmeticSO cosmetic)
        {
            int price = RotatingShop.DealPrice(cosmetic.JellyCost);
            string priceLine = price < cosmetic.JellyCost
                ? CurrencyGlyphs.Jelly + "<s>" + cosmetic.JellyCost + "</s>  " + price
                : CurrencyGlyphs.Jelly + cosmetic.JellyCost;

            card.Bind(IconFor(cosmetic), IconTintFor(cosmetic), cosmetic.DisplayName,
                cosmetic.Description, priceLine);

            bool sold = _store != null && _store.IsCosmeticOwned(cosmetic.CosmeticId);
            bool affordable = _store != null && _store.BankedJelly >= price;
            card.SetState(sold, affordable, _buyLabel, _soldLabel);
        }

        // Icon rules mirror CosmeticsUI: colors show their tint on the swatch;
        // stinger skins show their color tint over the neutral shape sprite.
        private Sprite IconFor(CosmeticSO cosmetic)
        {
            if (cosmetic.Slot == CosmeticSlot.Color || cosmetic.Sprite == null)
            {
                return _swatchSprite;
            }

            return cosmetic.Sprite;
        }

        private static Color IconTintFor(CosmeticSO cosmetic)
        {
            return cosmetic.Slot != CosmeticSlot.Hat ? cosmetic.Tint : Color.white;
        }

        private void Buy(int cardIndex)
        {
            if (cardIndex >= _deals.Count || _store == null)
            {
                return;
            }

            CosmeticSO cosmetic = _deals[cardIndex];
            if (CosmeticShop.TryPurchase(_store, cosmetic, RotatingShop.DealPrice(cosmetic.JellyCost)))
            {
                // A fresh deal is worn straight out of the shop (5C behavior).
                CosmeticShop.TryEquip(_store, cosmetic.Slot, cosmetic.CosmeticId);
            }

            RefreshAll();
        }

        private void RefreshTimer()
        {
            DateTime now = DateTime.Now;
            if (RotatingShop.DayStamp(now) != _boundDay)
            {
                // Midnight passed with the panel open — rotate live.
                EnsureDeals();
                RefreshAll();
            }

            if (_timerText == null)
            {
                return;
            }

            int seconds = RotatingShop.SecondsUntilRollover(now);
            if (seconds == _lastShownSeconds)
            {
                return;
            }

            _lastShownSeconds = seconds;
            _timerBuilder.Clear();
            _timerBuilder.Append(_timerPrefix);
            AppendTwoDigits(_timerBuilder, seconds / 3600);
            _timerBuilder.Append(':');
            AppendTwoDigits(_timerBuilder, (seconds % 3600) / 60);
            _timerBuilder.Append(':');
            AppendTwoDigits(_timerBuilder, seconds % 60);
            _timerText.SetText(_timerBuilder);
        }

        private static void AppendTwoDigits(StringBuilder builder, int value)
        {
            if (value < 10)
            {
                builder.Append('0');
            }

            builder.Append(value);
        }
    }
}
