using NUnit.Framework;
using SurveHive.Data;
using SurveHive.Stage;

namespace SurveHive.Tests
{
    public sealed class StageTimelineTests
    {
        private static StageTimelineEvent[] BuildEvents(params float[] times)
        {
            var events = new StageTimelineEvent[times.Length];
            for (int i = 0; i < times.Length; i++)
            {
                events[i] = new StageTimelineEvent { NormalizedTime = times[i] };
            }

            return events;
        }

        [Test]
        public void Crossing_FiresEventOnce()
        {
            StageTimelineEvent[] events = BuildEvents(0.25f, 0.5f, 0.75f, 1f);
            int[] results = new int[8];

            Assert.AreEqual(0, StageTimeline.CollectNewlyFired(events, -1f, 0.2f, results));

            Assert.AreEqual(1, StageTimeline.CollectNewlyFired(events, 0.2f, 0.3f, results));
            Assert.AreEqual(0, results[0]);

            // Same window again: nothing new.
            Assert.AreEqual(0, StageTimeline.CollectNewlyFired(events, 0.3f, 0.3f, results));
        }

        [Test]
        public void LargeStep_FiresAllCrossedEventsInOrder()
        {
            StageTimelineEvent[] events = BuildEvents(0.25f, 0.5f, 0.75f, 1f);
            int[] results = new int[8];

            int fired = StageTimeline.CollectNewlyFired(events, 0.2f, 0.8f, results);
            Assert.AreEqual(3, fired);
            Assert.AreEqual(0, results[0]);
            Assert.AreEqual(1, results[1]);
            Assert.AreEqual(2, results[2]);
        }

        [Test]
        public void EndOfStage_FiresFinalEventExactlyAtOne()
        {
            StageTimelineEvent[] events = BuildEvents(1f);
            int[] results = new int[8];

            Assert.AreEqual(0, StageTimeline.CollectNewlyFired(events, 0.5f, 0.999f, results));
            Assert.AreEqual(1, StageTimeline.CollectNewlyFired(events, 0.999f, 1f, results));
            // Progress clamps at 1: no refire on later frames.
            Assert.AreEqual(0, StageTimeline.CollectNewlyFired(events, 1f, 1f, results));
        }

        [Test]
        public void ZeroTimeEvent_FiresOnFirstAdvanceFromSentinel()
        {
            StageTimelineEvent[] events = BuildEvents(0f, 0.5f);
            int[] results = new int[8];

            int fired = StageTimeline.CollectNewlyFired(events, -1f, 0f, results);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(0, results[0]);
        }

        [Test]
        public void ResultBufferLimit_IsRespected()
        {
            StageTimelineEvent[] events = BuildEvents(0.1f, 0.2f, 0.3f, 0.4f);
            int[] results = new int[2];

            Assert.AreEqual(2, StageTimeline.CollectNewlyFired(events, 0f, 1f, results));
        }
    }
}
