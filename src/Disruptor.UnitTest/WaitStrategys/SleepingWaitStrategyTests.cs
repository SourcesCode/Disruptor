using Disruptor.Tests.Support;
using Disruptor.UnitTest.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.Tests
{
    [TestClass]
    public class SleepingWaitStrategyTests
    {
        [TestMethod]
        public void ShouldWaitForValue()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(50, new SleepingWaitStrategy());
        }
    }
}