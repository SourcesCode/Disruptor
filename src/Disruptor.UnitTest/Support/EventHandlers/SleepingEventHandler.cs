using Disruptor.UnitTest.Support;
using System.Threading;

namespace Disruptor.UnitTest.Support
{
    public class SleepingEventHandler : IEventHandler<TestEvent>
    {
        public void OnEvent(TestEvent @event, long sequence, bool endOfBatch)
        {
            Thread.Sleep(1000);
        }
    }
}
