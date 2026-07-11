using NUnit.Framework;
using SurveHive.Currency;
using SurveHive.Data;
using SurveHive.Persistence;
using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Tests
{
    /// <summary>
    /// Royal Jelly premium currency (PLAN 5B): the awards table, the separate
    /// banked pool with its spend bookkeeping, and the run wallet's jelly path
    /// staying unaffected by the honey gain multipliers.
    /// </summary>
    public sealed class RoyalJellyTests
    {
        [Test]
        public void Awards_FirstClear_ScalesWithTier()
        {
            Assert.AreEqual(10, RoyalJellyAwards.FirstClear(DifficultyTier.Easy));
            Assert.AreEqual(15, RoyalJellyAwards.FirstClear(DifficultyTier.Normal));
            Assert.AreEqual(20, RoyalJellyAwards.FirstClear(DifficultyTier.Hard));
            Assert.AreEqual(25, RoyalJellyAwards.FirstClear(DifficultyTier.Extreme));
        }

        [Test]
        public void Awards_FirstClear_ClampsOutOfRangeTiers()
        {
            Assert.AreEqual(
                RoyalJellyAwards.FirstClear(DifficultyTier.Easy),
                RoyalJellyAwards.FirstClear((DifficultyTier)(-2)));
            Assert.AreEqual(
                RoyalJellyAwards.FirstClear(DifficultyTier.Extreme),
                RoyalJellyAwards.FirstClear((DifficultyTier)99));
        }

        [Test]
        public void Awards_BossKills_FinalOutpaysMiniboss()
        {
            Assert.Greater(RoyalJellyAwards.MinibossKill, 0);
            Assert.Greater(RoyalJellyAwards.FinalBossKill, RoyalJellyAwards.MinibossKill);
        }

        [Test]
        public void State_BankAndSpendJelly_Bookkeeping()
        {
            var state = new MetaProgressionState();

            state.BankJelly(5);
            state.BankJelly(3);
            state.BankJelly(-4);
            Assert.AreEqual(8, state.BankedJelly);

            Assert.IsTrue(state.TrySpendJelly(6));
            Assert.AreEqual(2, state.BankedJelly);
            Assert.IsFalse(state.TrySpendJelly(3), "overdraw must fail");
            Assert.IsFalse(state.TrySpendJelly(0), "zero spend must fail");
            Assert.AreEqual(2, state.BankedJelly);
        }

        [Test]
        public void State_JellyPool_IsSeparateFromHoney()
        {
            var state = new MetaProgressionState();
            state.Bank(100);
            state.BankJelly(5);

            Assert.IsFalse(state.TrySpendJelly(50), "honey must not cover a jelly spend");
            Assert.IsTrue(state.TrySpend(100));
            Assert.AreEqual(5, state.BankedJelly);
        }

        [Test]
        public void State_SaveRoundTrip_PreservesJelly()
        {
            var state = new MetaProgressionState();
            state.BankJelly(12);

            var data = new SaveData();
            state.WriteTo(data);
            var reloaded = new MetaProgressionState();
            reloaded.LoadFrom(data);

            Assert.AreEqual(12, reloaded.BankedJelly);
        }

        [Test]
        public void Wallet_AddJelly_IgnoresHoneyGainMultipliers()
        {
            var walletGo = new GameObject("WalletTest");
            try
            {
                var wallet = walletGo.AddComponent<RunCurrencyWallet>();
                wallet.AddGainPercent(100f);
                wallet.SetDifficultyGainMultiplier(2.25f);

                int eventTotal = 0;
                wallet.OnJellyChanged += total => eventTotal = total;

                wallet.AddJelly(3);
                wallet.AddJelly(-1);
                wallet.AddJelly(0);

                Assert.AreEqual(3, wallet.TotalJelly, "jelly must not scale with honey multipliers");
                Assert.AreEqual(3, eventTotal);

                wallet.AddCurrency(10);
                Assert.AreEqual(45, wallet.TotalCurrency, "honey still scales (10 * 2 * 2.25)");
                Assert.AreEqual(3, wallet.TotalJelly);
            }
            finally
            {
                Object.DestroyImmediate(walletGo);
            }
        }
    }
}
