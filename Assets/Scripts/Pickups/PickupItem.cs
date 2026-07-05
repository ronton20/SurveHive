using System.Collections.Generic;
using SurveHive.Core;
using SurveHive.Currency;
using SurveHive.Data;
using SurveHive.Progression;
using UnityEngine;

namespace SurveHive.Pickups
{
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class PickupItem : MonoBehaviour
    {
        // Drops landing this close to an existing EXP orb merge into it.
        public const float ExpMergeRadius = 1.2f;

        // Live EXP orbs, for merge lookups. Fixed capacity, no per-frame work.
        private static readonly List<PickupItem> ActiveExpOrbs = new List<PickupItem>(256);

        [SerializeField] private int _poolId;
        [SerializeField] private string _targetTag = "Player";

        private SpriteRenderer _renderer;
        private PickupType _type;
        private float _value;
        private PlayerExperience _playerExperience;
        private RunCurrencyWallet _currencyWallet;
        private Transform _playerTransform;
        private bool _registeredAsExpOrb;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(PickupType type, float value, PlayerExperience playerExperience, RunCurrencyWallet currencyWallet, Transform playerTransform)
        {
            _type = type;
            _value = value;
            _playerExperience = playerExperience;
            _currencyWallet = currencyWallet;
            _playerTransform = playerTransform;

            if (_type == PickupType.Exp)
            {
                if (!_registeredAsExpOrb)
                {
                    ActiveExpOrbs.Add(this);
                    _registeredAsExpOrb = true;
                }

                ApplyExpStyle();
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }

        private void OnDisable()
        {
            if (_registeredAsExpOrb)
            {
                ActiveExpOrbs.Remove(this);
                _registeredAsExpOrb = false;
            }
        }

        /// <summary>
        /// Folds <paramref name="amount"/> into an existing EXP orb near
        /// <paramref name="position"/> (restyling it for its new value).
        /// Returns false when no orb is close enough and a new one is needed.
        /// </summary>
        public static bool TryMergeExp(Vector3 position, float amount)
        {
            for (int i = 0; i < ActiveExpOrbs.Count; i++)
            {
                PickupItem orb = ActiveExpOrbs[i];
                if (orb == null)
                {
                    continue;
                }

                if ((orb.transform.position - position).sqrMagnitude <= ExpMergeRadius * ExpMergeRadius)
                {
                    orb._value += amount;
                    orb.ApplyExpStyle();
                    return true;
                }
            }

            return false;
        }

        // Size + tint communicate how much EXP the orb holds (see ExpOrbTiers).
        private void ApplyExpStyle()
        {
            int tier = ExpOrbTiers.GetTier(_value);
            transform.localScale = Vector3.one * ExpOrbTiers.GetScale(tier);

            if (_renderer != null)
            {
                _renderer.color = ExpOrbTiers.GetColor(tier);
            }
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                return;
            }

            PickupMotion.Step(transform, _playerTransform);
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

                    // Exp orbs are excluded: dozens collect per second at peak
                    // horde (esp. with Magnet), which would flood the SFX pool.
                    if (AudioService.Instance != null)
                    {
                        AudioService.Instance.PlaySfx(SfxId.Pickup);
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
