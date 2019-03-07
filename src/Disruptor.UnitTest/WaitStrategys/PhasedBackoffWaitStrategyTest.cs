using Disruptor.UnitTest.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Disruptor.Tests
{
    [TestClass]
    public class PhasedBackoffWaitStrategyTest
    {
        /// <summary>
        /// 当序号变化时，立即处理
        /// </summary>
        [TestMethod]
        public void ShouldHandleImmediateSequenceChange()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(0, PhasedBackoffWaitStrategy.WithLock(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(0, PhasedBackoffWaitStrategy.WithSleep(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
        }

        /// <summary>
        /// 当序号变化时，延迟1毫秒再处理
        /// </summary>
        [TestMethod]
        public void ShouldHandleSequenceChangeWithOneMillisecondDelay()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(1, PhasedBackoffWaitStrategy.WithLock(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(1, PhasedBackoffWaitStrategy.WithSleep(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
        }

        /// <summary>
        /// 当序号变化时，延迟2毫秒再处理
        /// </summary>
        [TestMethod]
        public void ShouldHandleSequenceChangeWithTwoMillisecondDelay()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(2, PhasedBackoffWaitStrategy.WithLock(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(2, PhasedBackoffWaitStrategy.WithSleep(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
        }

        /// <summary>
        /// 当序号变化时，延迟10毫秒再处理
        /// </summary>
        [TestMethod]
        public void ShouldHandleSequenceChangeWithTenMillisecondDelay()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(10, PhasedBackoffWaitStrategy.WithLock(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(10, PhasedBackoffWaitStrategy.WithSleep(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
        }

    }
}