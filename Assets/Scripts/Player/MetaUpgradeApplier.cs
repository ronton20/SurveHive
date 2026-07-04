using SurveHive.Currency;
using SurveHive.Data;
using SurveHive.Health;
using UnityEngine;

namespace SurveHive.Player
{
    /// <summary>
    /// Applies purchased meta-shop ranks to the player's starting stats at run
    /// start. Order-safe vs HealthComponent.Awake: IncreaseMaxHealth raises max
    /// and current together, and Awake refills current from max either way.
    /// </summary>
    public sealed class MetaUpgradeApplier : MonoBehaviour
    {
        [SerializeField] private MetaProgressionStoreSO _store;
        [SerializeField] private MetaUpgradeSO[] _upgrades;
        [SerializeField] private PlayerStats _stats;
        [SerializeField] private HealthComponent _health;
        [SerializeField] private RunCurrencyWallet _wallet;

        private void Awake()
        {
            if (_store == null || _upgrades == null)
            {
                return;
            }

            for (int i = 0; i < _upgrades.Length; i++)
            {
                MetaUpgradeSO upgrade = _upgrades[i];
                if (upgrade == null)
                {
                    continue;
                }

                int rank = _store.GetUpgradeRank(upgrade.UpgradeId);
                if (rank > 0)
                {
                    ApplyUpgrade(upgrade, upgrade.TotalEffectAtRank(rank));
                }
            }
        }

        private void ApplyUpgrade(MetaUpgradeSO upgrade, float totalEffect)
        {
            switch (upgrade.StatType)
            {
                case MetaStatType.MaxHealth:
                    _stats.IncreaseMaxHealthFlat(totalEffect);
                    _health.IncreaseMaxHealth(totalEffect);
                    break;
                case MetaStatType.AttackDamage:
                    _stats.IncreaseAttackDamagePercent(totalEffect);
                    break;
                case MetaStatType.MoveSpeed:
                    _stats.IncreaseMoveSpeedPercent(totalEffect);
                    break;
                case MetaStatType.AttackSpeed:
                    _stats.IncreaseAttackSpeedPercent(totalEffect);
                    break;
                case MetaStatType.MagnetRadius:
                    _stats.IncreaseMagnetRadiusPercent(totalEffect);
                    break;
                case MetaStatType.CurrencyGain:
                    if (_wallet != null)
                    {
                        _wallet.AddGainPercent(totalEffect);
                    }

                    break;
            }
        }
    }
}
