using SurveHive.Core;
using SurveHive.Currency;
using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Pickups
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PickupItem : MonoBehaviour
    {
        [SerializeField] private int _poolId;
        [SerializeField] private string _targetTag = "Player";
        [SerializeField] private float _attractRadius = 3f;
        [SerializeField] private float _attractSpeed = 8f;

        // Magnet drop: while active, every pickup homes in regardless of radius.
        private static float _vacuumUntilTime;

        private PickupType _type;
        private float _value;
        private PlayerExperience _playerExperience;
        private RunCurrencyWallet _currencyWallet;
        private Transform _playerTransform;

        public static void ActivateVacuum(float durationSeconds)
        {
            _vacuumUntilTime = Time.time + durationSeconds;
        }

        public void Initialize(PickupType type, float value, PlayerExperience playerExperience, RunCurrencyWallet currencyWallet, Transform playerTransform)
        {
            _type = type;
            _value = value;
            _playerExperience = playerExperience;
            _currencyWallet = currencyWallet;
            _playerTransform = playerTransform;
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                return;
            }

            // Nectar Sense passive widens the attract radius via the magnet multiplier.
            float radius = _attractRadius;
            if (Player.PlayerContext.Stats != null)
            {
                radius *= Player.PlayerContext.Stats.MagnetRadiusMultiplier;
            }

            bool vacuumActive = Time.time < _vacuumUntilTime;
            Vector3 toPlayer = _playerTransform.position - transform.position;
            if (!vacuumActive && toPlayer.sqrMagnitude > radius * radius)
            {
                return;
            }

            float step = (vacuumActive ? _attractSpeed * 2.5f : _attractSpeed) * Time.deltaTime;
            transform.position += toPlayer.normalized * Mathf.Min(step, toPlayer.magnitude);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(_targetTag))
            {
                return;
            }

            switch (_type)
            {
                case PickupType.Exp:
                    if (_playerExperience != null)
                    {
                        _playerExperience.AddExperience(_value);
                    }
                    break;
                case PickupType.Currency:
                    if (_currencyWallet != null)
                    {
                        _currencyWallet.AddCurrency(Mathf.RoundToInt(_value));
                    }
                    break;
            }

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Release(_poolId, gameObject);
            }
        }
    }
}
