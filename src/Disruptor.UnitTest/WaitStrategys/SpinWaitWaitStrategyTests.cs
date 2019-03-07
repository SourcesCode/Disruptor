using Disruptor.Tests.Support;
using Disruptor.UnitTest.Support;
using Disruptor.WaitStrategys;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.Tests
{
    [TestClass]
    public class SpinWaitWaitStrategyTests
    {
        [TestMethod]
        public void ShouldWaitForValue()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(50, new SpinWaitWaitStrategy());
        }
    }
}