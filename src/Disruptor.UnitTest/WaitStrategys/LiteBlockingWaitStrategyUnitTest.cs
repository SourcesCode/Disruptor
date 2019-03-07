using Disruptor.UnitTest.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.UnitTest.WaitStrategys
{
    /// <summary>
    /// Variation of the <see cref="BlockingWaitStrategyUnitTest"/> that attempts to elide conditional wake-ups when
    /// the lock is uncontended.  Shows performance improvements on microbenchmarks.  However this
    /// wait strategy should be considered experimental as I have not full proved the correctness of
    /// the lock elision code.
    /// 
    /// LiteBlockingWaitStrategy 的实现方法也是阻塞等待，但它会减少一些不必要的唤醒。
    /// 从源码的注释上看，这个策略在基准性能测试上是会表现出一些性能提升，
    /// 但是作者还不能完全证明程序的正确性。
    /// </summary>
    public sealed class LiteBlockingWaitStrategyUnitTest : BaseUnitTest
    {
        [TestMethod]
        public void ShouldWaitForValue()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(50, new LiteBlockingWaitStrategy());
        }

    }
}
