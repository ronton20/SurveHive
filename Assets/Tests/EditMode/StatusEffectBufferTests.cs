using NUnit.Framework;
using SurveHive.Combat.Status;

namespace SurveHive.Tests
{
    public sealed class StatusEffectBufferTests
    {
        private const float Epsilon = 0.0001f;

        [Test]
        public void Burn_TicksDamageOverTime()
        {
            var buffer = new StatusEffectBuffer();
            buffer.Apply(StatusEffectType.Burn, 4f, 2f);

            buffer.Tick(StatusEffectBuffer.DotTickInterval);
            Assert.IsTrue(buffer.TryConsumeTickDamage(out float burn, out float poison));
            Assert.AreEqual(4f * StatusEffectBuffer.DotTickInterval, burn, Epsilon);
            Assert.AreEqual(0f, poison, Epsilon);
        }

        [Test]
        public void Burn_ReapplyRefreshesDuration_AndKeepsStrongestPotency()
        {
            var buffer = new StatusEffectBuffer();
            buffer.Apply(StatusEffectType.Burn, 6f, 2f);
            buffer.Tick(1.5f);
            Assert.AreEqual(0.5f, buffer.GetRemaining(StatusEffectType.Burn), Epsilon);

            buffer.Apply(StatusEffectType.Burn, 3f, 2f);
            Assert.AreEqual(2f, buffer.GetRemaining(StatusEffectType.Burn), Epsilon);
            Assert.AreEqual(6f, buffer.GetPotency(StatusEffectType.Burn), Epsilon);
        }

        [Test]
        public void Burn_ExpiresAfterDuration()
        {
            var buffer = new StatusEffectBuffer();
            buffer.Apply(StatusEffectType.Burn, 4f, 1f);
            buffer.Tick(1.01f);
            Assert.IsFalse(buffer.IsActive(StatusEffectType.Burn));
        }

        [Test]
        public void Poison_StacksIncreaseDamage()
        {
            var buffer = new StatusEffectBuffer();
            buffer.Apply(StatusEffectType.Poison, 2f, 3f);
            buffer.Apply(StatusEffectType.Poison, 2f, 3f);
            Assert.AreEqual(2, buffer.GetStacks(StatusEffectType.Poison));

            buffer.Tick(StatusEffectBuffer.DotTickInterval);
            buffer.TryConsumeTickDamage(out float _, out float poisonDamage);
            // dps = potency * stacks -> 2 * 2 = 4; one tick = 4 * 0.5 = 2.
            Assert.AreEqual(2f, poisonDamage, Epsilon);
        }

        [Test]
        public void Poison_StacksAreCapped()
        {
            var buffer = new StatusEffectBuffer();
            for (int i = 0; i < StatusEffectBuffer.PoisonMaxStacks + 4; i++)
            {
                buffer.Apply(StatusEffectType.Poison, 2f, 3f);
            }

            Assert.AreEqual(StatusEffectBuffer.PoisonMaxStacks, buffer.GetStacks(StatusEffectType.Poison));
        }

        [Test]
        public void Slow_ReducesMoveSpeed_AndExpires()
        {
            var buffer = new StatusEffectBuffer();
            buffer.Apply(StatusEffectType.Slow, 0.4f, 1.2f);
            Assert.AreEqual(0.6f, buffer.MoveSpeedMultiplier, Epsilon);

            buffer.Tick(1.21f);
            Assert.AreEqual(1f, buffer.MoveSpeedMultiplier, Epsilon);
        }

        [Test]
        public void Slow_StrongestWins()
        {
            var buffer = new StatusEffectBuffer();
            buffer.Apply(StatusEffectType.Slow, 0.5f, 2f);
            buffer.Apply(StatusEffectType.Slow, 0.2f, 3f);
            Assert.AreEqual(0.5f, buffer.MoveSpeedMultiplier, Epsilon);
            Assert.AreEqual(3f, buffer.GetRemaining(StatusEffectType.Slow), Epsilon);
        }

        [Test]
        public void Freeze_HardStops_AndBreaksOnDamageThreshold()
        {
            var buffer = new StatusEffectBuffer();
            buffer.Apply(StatusEffectType.Freeze, 10f, 5f);
            Assert.AreEqual(0f, buffer.MoveSpeedMultiplier, Epsilon);
            Assert.IsTrue(buffer.IsAttackDisabled);

            buffer.NotifyDamageTaken(5f);
            Assert.IsTrue(buffer.IsActive(StatusEffectType.Freeze));

            buffer.NotifyDamageTaken(5f);
            Assert.IsFalse(buffer.IsActive(StatusEffectType.Freeze));
            Assert.AreEqual(1f, buffer.MoveSpeedMultiplier, Epsilon);
        }

        [Test]
        public void Stun_DiminishingReturns_HalvesRepeatedStuns()
        {
            var buffer = new StatusEffectBuffer();
            buffer.SetDiminishingStuns(true);

            buffer.Apply(StatusEffectType.Stun, 0f, 1f);
            Assert.AreEqual(1f, buffer.GetRemaining(StatusEffectType.Stun), Epsilon);

            // Let the first stun run out, then reapply inside the DR window.
            buffer.Tick(1.01f);
            Assert.IsFalse(buffer.IsActive(StatusEffectType.Stun));

            buffer.Apply(StatusEffectType.Stun, 0f, 1f);
            Assert.AreEqual(StatusEffectBuffer.StunDiminishFactor, buffer.GetRemaining(StatusEffectType.Stun), Epsilon);
        }

        [Test]
        public void Stun_DiminishingReturns_ResetAfterWindow()
        {
            var buffer = new StatusEffectBuffer();
            buffer.SetDiminishingStuns(true);

            buffer.Apply(StatusEffectType.Stun, 0f, 1f);
            buffer.Tick(StatusEffectBuffer.StunDiminishWindowSeconds + 0.1f);

            buffer.Apply(StatusEffectType.Stun, 0f, 1f);
            Assert.AreEqual(1f, buffer.GetRemaining(StatusEffectType.Stun), Epsilon);
        }

        [Test]
        public void Stun_NoDiminishingReturnsForTrash()
        {
            var buffer = new StatusEffectBuffer();
            buffer.SetDiminishingStuns(false);

            buffer.Apply(StatusEffectType.Stun, 0f, 1f);
            buffer.Tick(1.01f);
            buffer.Apply(StatusEffectType.Stun, 0f, 1f);
            Assert.AreEqual(1f, buffer.GetRemaining(StatusEffectType.Stun), Epsilon);
        }

        [Test]
        public void Reset_ClearsEverything()
        {
            var buffer = new StatusEffectBuffer();
            buffer.Apply(StatusEffectType.Burn, 4f, 2f);
            buffer.Apply(StatusEffectType.Stun, 0f, 1f);
            buffer.Tick(0.6f);

            buffer.Reset();

            Assert.IsFalse(buffer.AnyActive);
            Assert.AreEqual(1f, buffer.MoveSpeedMultiplier, Epsilon);
            Assert.IsFalse(buffer.TryConsumeTickDamage(out float _, out float _));
        }
    }
}
