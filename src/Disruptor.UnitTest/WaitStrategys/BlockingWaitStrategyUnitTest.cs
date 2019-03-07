using Disruptor.UnitTest.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.UnitTest.WaitStrategys
{
    /// <summary>
    /// Blocking strategy that uses a lock and condition variable for <see cref="IEventProcessor"/>s waiting on a barrier.
    /// 
    /// This strategy can be used when throughput and low-latency are not as important as CPU resource.
    /// 默认的等待序列
    /// BlockingWaitStrategy的实现方法是阻塞等待。
    /// 当要求节省CPU资源，而不要求高吞吐量和低延迟的时候使用这个策略。
    /// </summary>
    public sealed class BlockingWaitStrategyUnitTest : BaseUnitTest
    {
        [TestMethod]
        public void ShouldWaitForValue()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(50, new BlockingWaitStrategy());
        }
    }
}
