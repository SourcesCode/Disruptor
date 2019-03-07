using System.Threading;

namespace Disruptor.UnitTest.Support
{
    public class EventHandlerStub<T> : IEventHandler<T>
    {
        private readonly CountdownEvent _countDownLatch;

        public EventHandlerStub(CountdownEvent countDownLatch)
        {
            _countDownLatch = countDownLatch;
        }

        public void OnEvent(T @event, long sequence, bool endOfBatch)
        {
            _countDownLatch.Signal();
        }
    }
}