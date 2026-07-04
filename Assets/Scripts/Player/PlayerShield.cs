using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Player
{
    /// <summary>
    /// Wax Shield charges: absorbs the next N hits entirely. Registers itself
    /// as the player's damage absorber; shows a ring visual while charged.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public sealed class PlayerShield : MonoBehaviour, IDamageAbsorber
    {
        public static PlayerShield Instance { get; private set; }

        [SerializeField] private HealthComponent _health;
        [SerializeField] private SpriteRenderer _shieldVisual;

        private int _charges;

        public int Charges => _charges;

        private void Awake()
        {
            Instance = this;
            _health.SetDamageAbsorber(this);
            RefreshVisual();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AddCharges(int amount)
        {
            _charges += amount;
            RefreshVisual();
        }

        public bool TryAbsorb(float amount)
        {
            if (_charges <= 0)
            {
                return false;
            }

            _charges--;
            RefreshVisual();
            return true;
        }

        private void RefreshVisual()
        {
            if (_shieldVisual != null)
            {
                _shieldVisual.enabled = _charges > 0;
            }
        }
    }
}
