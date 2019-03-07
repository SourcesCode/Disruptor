using System;
using Disruptor.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Disruptor.UnitTest.Support;

namespace Disruptor.Tests
{
    [TestClass]
    public class LiteTimeoutBlockingWaitStrategyTests
    {
        /// <summary>
        /// 等待超时异常
        /// </summary>
        [TestMethod]
        public void ShouldTimeoutWaitFor()
        {
            var sequenceBarrier = new DummySequenceBarrier();

            var theTimeout = TimeSpan.FromMilliseconds(500);
            var waitStrategy = new LiteTimeoutBlockingWaitStrategy(theTimeout);
            Sequence cursor = new Sequence(5);
            Sequence dependent = cursor;

            var t0 = DateTime.UtcNow;

            try
            {
                waitStrategy.WaitFor(6, cursor, dependent, sequenceBarrier);
                Assert.Fail("TimeoutException should have been thrown");
            }
            catch (TimeoutException)
            {
            }

            var t1 = DateTime.UtcNow;

            var timeWaiting = t1 - t0;

            Assert.IsTrue(timeWaiting >= theTimeout);
        }
    }
}