using SurveHive.Currency;
using SurveHive.Data;
using SurveHive.Health;
using SurveHive.Pickups;
using SurveHive.Progression;
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
        [SerializeField] private PlayerExperience _experience;

        private void Awake()
        {
            // The drop-rate multiplier lives on a static — reset every run so a
            // restart (or a missing rank) never inherits the previous value.
            ItemDrops.SetDropChanceMultiplier(1f);

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
                    // Flat (+N base damage) so early ranks are felt, not a % of
                    // a small base. Must stay in sync with MetaUpgradeSO.IsPercent.
                    _stats.IncreaseAttackDamageFlat(totalEffect);
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
                case MetaStatType.ExpGain:
                    if (_experience != null)
                    {
                        _experience.AddGainPercent(totalEffect);
                    }

                    break;
                case MetaStatType.AbilityPower:
                    _stats.IncreaseAbilityPowerPercent(totalEffect);
                    break;
                case MetaStatType.CooldownReduction:
                    _stats.DecreaseActiveCooldownPercent(totalEffect);
                    break;
                case MetaStatType.CritChance:
                    // Percent points on the 0% base (1A) — the 40% cap is the
                    // upgrade's maxRank * effectPerRank, not a code clamp.
                    _stats.IncreaseCritChanceFlat(totalEffect);
                    break;
                case MetaStatType.CritDamage:
                    _stats.IncreaseCritDamagePercent(totalEffect);
                    break;
                case MetaStatType.ItemDropRate:
                    ItemDrops.SetDropChanceMultiplier(1f + totalEffect / 100f);
                    break;
                // MetaStatType.Rerolls is deliberately absent: per-run reroll
                // stock is read by LevelUpUIController straight from the store.
            }
        }
    }
}
