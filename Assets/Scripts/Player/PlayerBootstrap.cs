using SurveHive.Health;
using SurveHive.Input;
using UnityEngine;

namespace SurveHive.Player
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class PlayerBootstrap : MonoBehaviour
    {
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private PlayerInputController _inputController;
        [SerializeField] private PlayerStats _stats;

        private void Awake()
        {
            _movement.Initialize(_inputController, _stats);
            PlayerContext.Register(_stats, GetComponent<HealthComponent>(), transform);
        }

        private void OnDestroy()
        {
            PlayerContext.Clear();
        }
    }
}
