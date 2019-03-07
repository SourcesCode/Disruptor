using System.Threading;
using Disruptor.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Disruptor.UnitTest.Support;
using Disruptor.UnitTest.Core;

namespace Disruptor.Tests
{
    [TestClass]
    public class SequenceReportingCallbackTests
    {
        private readonly ManualResetEvent _callbackSignal = new ManualResetEvent(false);
        private readonly ManualResetEvent _onEndOfBatchSignal = new ManualResetEvent(false);

        [TestMethod]
        public void ShouldReportProgressByUpdatingSequenceViaCallback()
        {
            var ringBuffer = RingBuffer<StubEvent>.CreateMultiProducer(StubEvent.EventFactory, 16);
            var sequenceBarrier = ringBuffer.NewBarrier();
            ISequenceReportingEventHandler<StubEvent> handler = new TestSequenceReportingEventHandler(_callbackSignal, _onEndOfBatchSignal);
            var batchEventProcessor = BatchEventProcessorFactory.Create(ringBuffer, sequenceBarrier, handler);
            ringBuffer.AddGatingSequences(batchEventProcessor.GetSequence());

            var thread = new Thread(batchEventProcessor.Run) { IsBackground = true };
            thread.Start();

            Assert.AreEqual(-1L, batchEventProcessor.GetSequence().Get());
            ringBuffer.Publish(ringBuffer.Next());

            _callbackSignal.WaitOne();
            Assert.AreEqual(0L, batchEventProcessor.GetSequence().Get());

            _onEndOfBatchSignal.Set();
            Assert.AreEqual(0L, batchEventProcessor.GetSequence().Get());

            batchEventProcessor.Halt();
            thread.Join();
        }

        private class TestSequenceReportingEventHandler : ISequenceReportingEventHandler<StubEvent>
        {
            private ISequence _sequenceCallback;
            private readonly ManualResetEvent _callbackSignal;
            private readonly ManualResetEvent _onEndOfBatchSignal;

            public TestSequenceReportingEventHandler(ManualResetEvent callbackSignal, ManualResetEvent onEndOfBatchSignal)
            {
                _callbackSignal = callbackSignal;
                _onEndOfBatchSignal = onEndOfBatchSignal;
            }

            public void SetSequenceCallback(ISequence sequenceTrackerCallback)
            {
                _sequenceCallback = sequenceTrackerCallback;
            }

            public void OnEvent(StubEvent evt, long sequence, bool endOfBatch)
            {
                _sequenceCallback.Set(sequence);
                _callbackSignal.Set();

                if (endOfBatch)
                {
                    _onEndOfBatchSignal.WaitOne();
                }
            }
        }
    }
}
