using Disruptor.UnitTest.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.UnitTest.WaitStrategys
{
    [TestClass]
    public class BusySpinWaitStrategyUnitTest : BaseUnitTest
    {
        /// <summary>
        /// 应该等待
        /// </summary>
        [TestMethod]
        public void ShouldWaitForValue()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(50, new BusySpinWaitStrategy());
        }

    }

}
