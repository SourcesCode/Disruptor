using Disruptor.Dsl;
using System;
using System.Collections.Generic;
using System.Text;

namespace Disruptor.UnitTest.Support
{
    public class TestEventHandler : IEventHandler<TestEvent>
    {
        private readonly Disruptor<TestEvent> _disruptor;
        private readonly long[] _remainingCapacity;

        public TestEventHandler(Disruptor<TestEvent> disruptor, long[] remainingCapacity)
        {
            _disruptor = disruptor;
            _remainingCapacity = remainingCapacity;
        }

        public void OnEvent(TestEvent data, long sequence, bool endOfBatch)
        {
            _remainingCapacity[0] = _disruptor.GetRingBuffer().RemainingCapacity();
        }
    }
}
