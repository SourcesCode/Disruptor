using Disruptor.UnitTest.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.UnitTest.WaitStrategys
{
    /// <summary>
    /// Variation of the <see cref="TimeoutBlockingWaitStrategyUnitTest"/> that attempts to elide conditional wake-ups
    /// when the lock is uncontended.
    /// </summary>
    public class LiteTimeoutBlockingWaitStrategyUnitTest : BaseUnitTest
    {
        [TestMethod]
        public void ShouldWaitForValue()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(50, new LiteTimeoutBlockingWaitStrategy(50));
        }
    }
}
