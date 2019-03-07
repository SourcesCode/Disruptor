﻿using Disruptor.UnitTest.Support;

namespace Disruptor.UnitTest.Support
{
    public class EvilEqualsEventHandler : IEventHandler<TestEvent>
    {
        public void OnEvent(TestEvent data, long sequence, bool endOfBatch)
        {
        }

        public override bool Equals(object obj)
        {
            return true;
        }

        public override int GetHashCode()
        {
            return 1;
        }
    }
}