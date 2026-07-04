using SurveHive.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// Button that loads a scene by name — used by the results screens to route
    /// back to the main menu. Clears any run pause before loading.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class SceneLoadButton : MonoBehaviour
    {
        [SerializeField] private string _sceneName;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(LoadTargetScene);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(LoadTargetScene);
        }

        private void LoadTargetScene()
        {
            GamePause.SetPaused(false);
            SceneManager.LoadScene(_sceneName);
        }
    }
}
