using UnityEngine;

namespace SurveHive.Health
{
    public interface IDamageable
    {
        bool IsDead { get; }

        void TakeDamage(float amount, GameObject instigator);
    }
}
