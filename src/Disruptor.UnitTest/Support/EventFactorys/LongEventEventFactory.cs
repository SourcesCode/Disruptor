namespace Disruptor.UnitTest.Support
{
    public class LongEventEventFactory : IEventFactory<TestEvent>
    {
        public TestEvent NewInstance()
        {
            return new TestEvent();
        }
    }
}
