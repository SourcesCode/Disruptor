﻿namespace Disruptor.Tests.Support
{
    public class DummyWaitStrategy : IWaitStrategy
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
