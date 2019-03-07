using Disruptor.UnitTest.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.UnitTest.WaitStrategys
{
    /// <summary>
    /// Phased wait strategy for waiting <see cref="IEventProcessor"/>s on a barrier.
    /// 
    /// This strategy can be used when throughput and low-latency are not as important as CPU resource.
    /// Spins, then yields, then waits using the configured fallback WaitStrategy.
    /// 
    /// PhasedBackoffWaitStrategy 的实现方法是先自旋(10000次)，
    /// 不行再临时让出调度(yield)，不行再使用其他的策略进行等待。
    /// 可以根据具体场景自行设置自旋时间、yield时间和备用等待策略。
    /// </summary>
    public sealed class PhasedBackoffWaitStrategyUnitTest : BaseUnitTest
    {
        [TestMethod]
        public void ShouldWaitForValue()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(50, new PhasedBackoffWaitStrategy(100, 100, new BlockingWaitStrategy()));
        }

    }
}
