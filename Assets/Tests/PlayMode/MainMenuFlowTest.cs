using System.Collections;
using NUnit.Framework;
using SurveHive.Core;
using SurveHive.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SurveHive.Tests
{
    /// <summary>
    /// Menu flow end-to-end: boots the MainMenu scene, walks home → world
    /// select → starts a Beehive run, and separately exercises the Hive
    /// Upgrades shop panel (buy button disabled at zero balance, purchase
    /// works once honey is banked and raises the player's starting power).
    /// </summary>
    public sealed class MainMenuFlowTest
    {
        // Phase 4: menus talk to the persistent save — redirect it to a fresh
        // temp file so tests never touch the developer's real save.
        private string _savePath;

        [SetUp]
        public void RedirectSaveFile()
        {
            _savePath = System.IO.Path.Combine(Application.temporaryCachePath, "menuflow_test_save.json");
            if (System.IO.File.Exists(_savePath))
            {
                System.IO.File.Delete(_savePath);
            }

            Persistence.SaveFileStore.SetPathOverride(_savePath);
        }

        [TearDown]
        public void RestoreSaveFile()
        {
            Persistence.SaveFileStore.SetPathOverride(null);
        }

        [UnityTest]
        public IEnumerator MenuFlow_HomeToWorldSelectToRun_AndShopPurchase()
        {
            SceneManager.LoadScene("MainMenu");
            yield return null;

            var controller = Object.FindAnyObjectByType<MainMenuController>();
            Assert.IsNotNull(controller, "MainMenuController present");
            Assert.AreEqual(1f, Time.timeScale, "menu clears any leftover pause");

            GameObject mainPanel = GameObject.Find("MainPanel");
            Assert.IsNotNull(mainPanel, "MainPanel active on boot");

            // --- Shop: balance zero, every buy button disabled. ---
            controller.ShowShop();
            yield return null;

            var shop = Object.FindAnyObjectByType<MetaShopUI>();
            Assert.IsNotNull(shop, "MetaShopUI present on the shop panel");
            var rows = shop.GetComponentsInChildren<MetaShopCardUI>();
            Assert.AreEqual(13, rows.Length, "thirteen shop rows (Phase 1C expansion)");
            foreach (MetaShopCardUI row in rows)
            {
                Assert.IsFalse(row.BuyButton.interactable, $"{row.name} buy disabled at zero balance");
            }

            // --- Bank honey (as a finished run would), reopen, buy a rank. ---
            var store = FindStore(rows[0]);
            store.BankRunCurrency(500);
            controller.ShowMain();
            controller.ShowShop();
            yield return null;

            MetaShopCardUI healthRow = FindRow(rows, "meta_max_health");
            Assert.IsNotNull(healthRow, "max-health shop row exists");
            float healthPerRank = healthRow.Upgrade.EffectPerRank;
            Assert.IsTrue(healthRow.BuyButton.interactable, "buy enabled once affordable");
            int balanceBefore = store.BankedCurrency;
            healthRow.BuyButton.onClick.Invoke();
            yield return null;

            Assert.AreEqual(1, store.GetUpgradeRank("meta_max_health"), "purchase granted rank 1");
            Assert.Less(store.BankedCurrency, balanceBefore, "purchase spent honey");

            // --- World select → start the run. ---
            controller.ShowMain();
            controller.ShowWorldSelect();
            yield return null;

            GameObject worldPanel = GameObject.Find("WorldSelectPanel");
            Assert.IsNotNull(worldPanel, "WorldSelectPanel shown");
            Button beehiveButton = worldPanel.transform.Find("BeehiveButton").GetComponent<Button>();
            beehiveButton.onClick.Invoke();

            // Scene loads async; give it a few frames.
            for (int i = 0; i < 10 && SceneManager.GetActiveScene().name != "Beehive"; i++)
            {
                yield return null;
            }

            Assert.AreEqual("Beehive", SceneManager.GetActiveScene().name, "run scene loaded from menu");
            yield return null;

            Assert.IsNotNull(RunSession.Instance, "RunSession up after menu-started run");

            // The purchased max-health rank applies at run start (base 100).
            GameObject player = GameObject.FindWithTag("Player");
            var health = player.GetComponent<Health.HealthComponent>();
            Assert.AreEqual(100f + healthPerRank, health.MaxHealth, 0.01f,
                "meta max-health rank raised starting HP");
        }

        private static Core.IMetaProgressionStore FindStore(MetaShopCardUI row)
        {
            var shop = row.GetComponentInParent<MetaShopUI>(true);
            var field = typeof(MetaShopUI).GetField("_store",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (Core.IMetaProgressionStore)field.GetValue(shop);
        }

        private static MetaShopCardUI FindRow(MetaShopCardUI[] rows, string upgradeId)
        {
            foreach (MetaShopCardUI row in rows)
            {
                if (row.Upgrade != null && row.Upgrade.UpgradeId == upgradeId)
                {
                    return row;
                }
            }

            return null;
        }
    }
}
