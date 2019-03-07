using System;
using Disruptor.UnitTest.Support;

namespace Disruptor.UnitTest.Support
{
    public class ExceptionThrowingEventHandler : IEventHandler<TestEvent>
    {
        private readonly Exception _applicationException;

        public ExceptionThrowingEventHandler(Exception applicationException)
        {
            _applicationException = applicationException;
        }

        public void OnEvent(TestEvent data, long sequence, bool endOfBatch)
        {
            throw _applicationException;
        }
    }
}