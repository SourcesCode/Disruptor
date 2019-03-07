using Disruptor.UnitTest.Demos.Demo2;

namespace Disruptor.Tests.Example
{
    public class PullWithPoller
    {
        public void TestMain()
        {
            var ringBuffer = RingBuffer<DataEvent<object>>.CreateMultiProducer(new DataEventEventFactory<object>(), 1024);
            var poller = ringBuffer.NewPoller();

            var value = GetNextValue(poller);

            // Value could be null if no events are available.
            if (null != value)
            {
                // Process value.
            }
        }

        private static object GetNextValue(EventPoller<DataEvent<object>> poller)
        {
            var output = new object[1];
            poller.Poll(new DataEventHandler(output));
            return output[0];
        }

        private class DataEventHandler : EventPoller<DataEvent<object>>.IHandler<DataEvent<object>>
        {
            private readonly object[] _output;
            public DataEventHandler(object[] output)
            {
                _output = output;
            }

            public bool OnEvent(DataEvent<object> @event, long sequence, bool endOfBatch)
            {
                _output[0] = @event.Data;
                return false;
            }
        }


    }
}