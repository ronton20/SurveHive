using UnityEngine;

namespace SurveHive.Core
{
    /// <summary>
    /// Silences all game audio in automated/headless runs (batch-mode tests,
    /// builder passes) — the editor still routes sound to the speakers there,
    /// and unattended runs shouldn't be audible. Normal editor play and player
    /// builds are unaffected.
    /// </summary>
    public static class HeadlessAudioMute
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void MuteInBatchMode()
        {
            if (Application.isBatchMode)
            {
                AudioListener.volume = 0f;
            }
        }
    }
}
