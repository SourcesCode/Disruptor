using System.Threading;
using Disruptor.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Disruptor.UnitTest.Support;
using Disruptor.UnitTest.Core;

namespace Disruptor.Tests
{
    [TestClass]
    public class LifecycleAwareTests
    {
        private readonly ManualResetEvent _startSignal = new ManualResetEvent(false);
        private readonly ManualResetEvent _shutdownSignal = new ManualResetEvent(false);

        private readonly RingBuffer<StubEvent> _ringBuffer = RingBuffer<StubEvent>.CreateMultiProducer(StubEvent.EventFactory, 16);

        private ISequenceBarrier _sequenceBarrier;
        private LifecycleAwareEventHandler _eventHandler;
        private BatchEventProcessor<StubEvent> _batchEventProcessor;


        public LifecycleAwareTests()
        {
            _sequenceBarrier = _ringBuffer.NewBarrier();
            _eventHandler = new LifecycleAwareEventHandler(_startSignal, _shutdownSignal);
            _batchEventProcessor = BatchEventProcessorFactory.Create(_ringBuffer, _sequenceBarrier, _eventHandler);
        }

        /// <summary>
        /// 接收批处理生命周期通知
        /// </summary>
        [TestMethod]
        public void ShouldNotifyOfBatchProcessorLifecycle()
        {
            new Thread(_batchEventProcessor.Run).Start();

            _startSignal.WaitOne();
            _batchEventProcessor.Halt();

            _shutdownSignal.WaitOne();

            Assert.AreEqual(_eventHandler.StartCounter, 1);
            Assert.AreEqual(_eventHandler.ShutdownCounter, 1);
        }

        private sealed class LifecycleAwareEventHandler : IEventHandler<StubEvent>, ILifecycleAware
        {
            private readonly ManualResetEvent _startSignal;
            private readonly ManualResetEvent _shutdownSignal;

            public int StartCounter { get; private set; }

            public int ShutdownCounter { get; private set; }

            public LifecycleAwareEventHandler(ManualResetEvent startSignal, ManualResetEvent shutdownSignal)
            {
                _startSignal = startSignal;
                _shutdownSignal = shutdownSignal;
            }

            public void OnEvent(StubEvent data, long sequence, bool endOfBatch)
            {
            }

            public void OnStart()
            {
                ++StartCounter;
                _startSignal.Set();
            }

            public void OnShutdown()
            {
                ++ShutdownCounter;
                _shutdownSignal.Set();
            }
        }
    }
}
