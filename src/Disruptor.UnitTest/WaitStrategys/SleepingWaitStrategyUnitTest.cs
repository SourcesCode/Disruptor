using Disruptor.UnitTest.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.UnitTest.WaitStrategys
{
    [TestClass]
    public sealed class SleepingWaitStrategyUnitTest : BaseUnitTest
    {
        [TestMethod]
        public void ShouldWaitForValue()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(50, new SleepingWaitStrategy());
        }

    }
}
