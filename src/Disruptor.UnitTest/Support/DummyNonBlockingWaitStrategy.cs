using Disruptor.WaitStrategys;

namespace Disruptor.UnitTest.Support
{
    public class DummyNonBlockingWaitStrategy : INonBlockingWaitStrategy
    {
        public int SignalAllWhenBlockingCalls { get; private set; }

        public long WaitFor(long sequence, ISequence cursor, ISequence dependentSequence, ISequenceBarrier barrier)
        {
            return 0;
        }

        public void SignalAllWhenBlocking()
        {
            SignalAllWhenBlockingCalls++;
        }
    }
}
