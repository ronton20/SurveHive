using SurveHive.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// HUD stage progress bar: fill tracks the stage director's normalized
    /// progress. Event markers (siren/skull/crown) are static children placed
    /// along the bar by the scene builder, so the player sees what's coming.
    /// </summary>
    public sealed class StageProgressBarUI : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private StageDirector _director;

        private void Update()
        {
            if (_director == null)
            {
                return;
            }

            UIBarFiller.SetFill(_fillImage, _director.Progress);
        }
    }
}
