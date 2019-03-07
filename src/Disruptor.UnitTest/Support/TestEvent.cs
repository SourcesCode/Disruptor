namespace Disruptor.UnitTest.Support
{
    /// <summary>
    /// 队列中的元素
    /// </summary>
    public class TestEvent
    {
        public long Value { get; set; }
        public static TestEventFactory EventFactory = new TestEventFactory();
        //public override string ToString() => "Test Event";

    }
}
