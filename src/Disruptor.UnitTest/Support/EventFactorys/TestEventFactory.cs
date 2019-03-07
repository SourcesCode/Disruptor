namespace Disruptor.UnitTest.Support
{
    public class TestEventFactory : IEventFactory<TestEvent>
    {
        public TestEvent NewInstance()
        {
            return new TestEvent();
        }
    }
}
