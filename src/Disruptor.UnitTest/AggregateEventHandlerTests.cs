using Disruptor.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.Tests
{
    [TestClass]
    public class AggregateEventHandlerTests
    {
        private readonly DummyEventHandler<int[]> _eh1 = new DummyEventHandler<int[]>();
        private readonly DummyEventHandler<int[]> _eh2 = new DummyEventHandler<int[]>();
        private readonly DummyEventHandler<int[]> _eh3 = new DummyEventHandler<int[]>();

        /// <summary>
        /// 调用OnEvent
        /// </summary>
        [TestMethod]
        public void ShouldCallOnEventInSequence()
        {
            var evt = new[] { 7 };
            const long sequence = 3L;
            const bool endOfBatch = true;

            var aggregateEventHandler = new AggregateEventHandler<int[]>(_eh1, _eh2, _eh3);

            aggregateEventHandler.OnEvent(evt, sequence, endOfBatch);

            AssertLastEvent(evt, sequence, _eh1, _eh2, _eh3);
        }

        /// <summary>
        /// 调用OnStart
        /// </summary>
        [TestMethod]
        public void ShouldCallOnStartInSequence()
        {
            var aggregateEventHandler = new AggregateEventHandler<int[]>(_eh1, _eh2, _eh3);

            aggregateEventHandler.OnStart();

            AssertStartCalls(1, _eh1, _eh2, _eh3);
        }

        /// <summary>
        /// 调用OnShutdown
        /// </summary>
        [TestMethod]
        public void ShouldCallOnShutdownInSequence()
        {
            var aggregateEventHandler = new AggregateEventHandler<int[]>(_eh1, _eh2, _eh3); ;

            aggregateEventHandler.OnShutdown();

            AssertShutdownCalls(1, _eh1, _eh2, _eh3);
        }

        /// <summary>
        /// AggregateEventHandler应该能处理空EventHandlers调用
        /// </summary>
        [TestMethod]
        public void ShouldHandleEmptyListOfEventHandlers()
        {
            var aggregateEventHandler = new AggregateEventHandler<int[]>();

            aggregateEventHandler.OnEvent(new[] { 7 }, 0L, true);
            aggregateEventHandler.OnStart();
            aggregateEventHandler.OnShutdown();
        }

        private static void AssertLastEvent(int[] evt, long sequence, params DummyEventHandler<int[]>[] handlers)
        {
            foreach (var handler in handlers)
            {
                Assert.AreSame(handler.LastEvent, evt);
                Assert.AreEqual(handler.LastSequence, sequence);
            }
        }

        private static void AssertStartCalls(int startCalls, params DummyEventHandler<int[]>[] handlers)
        {
            foreach (var handler in handlers)
            {
                Assert.AreEqual(handler.StartCalls, startCalls);
            }
        }

        private static void AssertShutdownCalls(int shutdownCalls, params DummyEventHandler<int[]>[] handlers)
        {
            foreach (var handler in handlers)
            {
                Assert.AreEqual(handler.ShutdownCalls, shutdownCalls);
            }
        }
    }
}
