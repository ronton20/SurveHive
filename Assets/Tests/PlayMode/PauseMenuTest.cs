using System.Collections;
using NUnit.Framework;
using SurveHive.Core;
using SurveHive.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SurveHive.Tests
{
    /// <summary>
    /// In-run pause: opening freezes the game completely (timeScale 0 stops
    /// every spawner/cooldown — they all run on scaled time), settings changes
    /// made while paused apply and persist to the save file, closing resumes,
    /// and the pause menu refuses to open over another pause owner.
    /// </summary>
    public sealed class PauseMenuTest
    {
        private string _savePath;

        [SetUp]
        public void RedirectSaveFile()
        {
            _savePath = System.IO.Path.Combine(Application.temporaryCachePath, "pause_test_save.json");
            if (System.IO.File.Exists(_savePath))
            {
                System.IO.File.Delete(_savePath);
            }

            Persistence.SaveFileStore.SetPathOverride(_savePath);
        }

        [TearDown]
        public void RestoreSaveFile()
        {
            GamePause.SetPaused(false);
            Persistence.SaveFileStore.SetPathOverride(null);
        }

        [UnityTest]
        public IEnumerator Pause_FreezesRun_SettingsPersist_ResumeRestores()
        {
            SceneManager.LoadScene("Beehive");
            yield return null;

            var pause = Object.FindAnyObjectByType<PauseMenuController>();
            Assert.IsNotNull(pause, "PauseMenuController present in the run scene");

            // --- Open: full freeze. ---
            pause.Open();
            Assert.IsTrue(pause.IsOpen, "pause opened");
            Assert.AreEqual(0f, Time.timeScale, "timeScale 0 while paused");
            Assert.IsTrue(GamePause.IsPaused, "central pause owns the freeze");

            // Let the timeScale change settle one frame — the frame straddling
            // Open() can still consume a pre-pause deltaTime.
            yield return null;
            float elapsedBefore = RunSession.Instance.ElapsedSeconds;
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            Assert.AreEqual(elapsedBefore, RunSession.Instance.ElapsedSeconds,
                "run clock frozen while paused");

            // --- Settings changes while paused apply + persist. ---
            var settings = Object.FindAnyObjectByType<SettingsPanelUI>(FindObjectsInactive.Include);
            Assert.IsNotNull(settings, "pause settings panel exists");
            settings.gameObject.SetActive(true);
            yield return null;

            var musicSlider = settings.GetComponentInChildren<UnityEngine.UI.Slider>(true);
            Assert.IsNotNull(musicSlider, "music slider exists");
            musicSlider.value = 0.42f;
            yield return null;

            Persistence.SaveData saved = Persistence.SaveFileStore.Load();
            Assert.IsNotNull(saved, "settings change wrote the save file");
            Assert.AreEqual(0.42f, saved.settings.musicVolume, 0.001f, "music volume persisted");

            // --- Close: resumes. ---
            pause.Close();
            Assert.IsFalse(pause.IsOpen, "pause closed");
            Assert.AreEqual(1f, Time.timeScale, "timeScale restored on resume");

            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }

            Assert.Greater(RunSession.Instance.ElapsedSeconds, elapsedBefore,
                "run clock advances again after resume");

            // --- Never opens over another pause owner. ---
            GamePause.SetPaused(true);
            pause.Open();
            Assert.IsFalse(pause.IsOpen, "pause refuses to open over an existing pause");
            GamePause.SetPaused(false);
        }
    }
}
