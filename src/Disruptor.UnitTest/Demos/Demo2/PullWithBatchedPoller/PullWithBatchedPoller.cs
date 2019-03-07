using Disruptor.UnitTest.Demos.Demo2;
using System;

namespace Disruptor.Tests.Example.PullWithBatchedPoller
{
    public class PullWithBatchedPoller
    {
        public void TestMain()
        {
            var batchSize = 40;
            var ringBuffer = RingBuffer<DataEvent<object>>.CreateMultiProducer(new DataEventEventFactory<object>(), 1024);

            var poller = new BatchedPoller<object>(ringBuffer, batchSize);

            var value = poller.Poll();

            // Value could be null if no events are available.
            if (null != value)
            {
                // Process value.
            }
        }
    }
}
