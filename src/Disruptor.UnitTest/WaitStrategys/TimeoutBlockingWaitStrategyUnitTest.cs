using Disruptor.UnitTest.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Disruptor.UnitTest.WaitStrategys
{
    [TestClass]
    public sealed class TimeoutBlockingWaitStrategyUnitTest : BaseUnitTest
    {
        /// <summary>
        /// 应该抛出等待超时异常
        /// </summary>
        [TestMethod]
        public void ShouldTimeoutWaitFor()
        {
            var sequenceBarrier = new DummySequenceBarrier();

            var theTimeout = TimeSpan.FromMilliseconds(500);
            TimeoutBlockingWaitStrategy waitStrategy = new TimeoutBlockingWaitStrategy(theTimeout);
            Sequence cursor = new Sequence(5);
            Sequence dependent = cursor;

            var t0 = DateTime.UtcNow;

            Exception ex;
            try
            {
                waitStrategy.WaitFor(6, cursor, dependent, sequenceBarrier);
                throw new ApplicationException("TimeoutException should have been thrown");
            }
            catch (TimeoutException e)
            {
                ex = e;
            }
            catch (Exception e)
            {
                ex = e;
            }

            var t1 = DateTime.UtcNow;

            var timeWaiting = t1 - t0;

            //Assert.AreEqual(timeWaiting, Is.GreaterThanOrEqualTo(theTimeout));
            Assert.IsTrue(timeWaiting >= theTimeout);

        }
    }
}
