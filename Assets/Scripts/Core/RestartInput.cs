using UnityEngine.InputSystem;

namespace SurveHive.Core
{
    /// <summary>
    /// Shared end-of-run restart hotkey. Keyboard-only by design: the results
    /// screens have RETRY/HIVE buttons for pointer + touch, and a tap-anywhere
    /// restart would race those buttons (restart fires on press, clicks on
    /// release).
    /// </summary>
    public static class RestartInput
    {
        public static bool WasRequested()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.rKey.wasPressedThisFrame;
        }
    }
}
