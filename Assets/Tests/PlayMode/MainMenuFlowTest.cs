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

            // --- Shop (3B tabbed layout): balance zero, buy disabled for every
            //     upgrade shown in the detail pane. ---
            controller.ShowShop();
            yield return null;

            var shop = Object.FindAnyObjectByType<MetaShopUI>();
            Assert.IsNotNull(shop, "MetaShopUI present on the shop panel");
            var icons = shop.GetComponentsInChildren<MetaShopIconUI>(true);
            Assert.AreEqual(13, icons.Length, "thirteen shop upgrade icons");
            var detail = shop.GetComponentInChildren<MetaShopDetailUI>(true);
            Assert.IsNotNull(detail, "shop detail pane present");
            foreach (MetaShopIconUI icon in icons)
            {
                // Selecting an icon binds it into the detail pane; at zero honey
                // its BUY must be disabled.
                icon.Button.onClick.Invoke();
                Assert.IsFalse(detail.BuyButton.interactable, $"{icon.name} buy disabled at zero balance");
            }

            // --- Bank honey (as a finished run would), reopen, buy a rank. ---
            var store = FindStore(shop);
            store.BankRunCurrency(500);
            controller.ShowMain();
            controller.ShowShop();
            yield return null;

            MetaShopIconUI healthIcon = FindIcon(icons, "meta_max_health");
            Assert.IsNotNull(healthIcon, "max-health upgrade icon exists");
            float healthPerRank = healthIcon.Upgrade.EffectPerRank;
            healthIcon.Button.onClick.Invoke(); // select it into the detail pane
            Assert.IsTrue(detail.BuyButton.interactable, "buy enabled once affordable");
            int balanceBefore = store.BankedCurrency;
            detail.BuyButton.onClick.Invoke();
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

        private static Core.IMetaProgressionStore FindStore(MetaShopUI shop)
        {
            var field = typeof(MetaShopUI).GetField("_store",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (Core.IMetaProgressionStore)field.GetValue(shop);
        }

        private static MetaShopIconUI FindIcon(MetaShopIconUI[] icons, string upgradeId)
        {
            foreach (MetaShopIconUI icon in icons)
            {
                if (icon.Upgrade != null && icon.Upgrade.UpgradeId == upgradeId)
                {
                    return icon;
                }
            }

            return null;
        }
    }
}
