using Disruptor.UnitTest.Support;
using Disruptor.WaitStrategys;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.UnitTest
{
    [TestClass]
    public class BlockingSpinWaitWaitStrategyTests
    {
        [TestMethod]
        public void ShouldWaitForValue()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(50, new BlockingSpinWaitWaitStrategy());
        }
    }
}