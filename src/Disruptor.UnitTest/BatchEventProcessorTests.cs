using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Disruptor.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Disruptor.UnitTest.Support;

namespace Disruptor.Tests
{
    //[TestFixture(BatchEventProcessorType.Legacy)]
    //[TestFixture(BatchEventProcessorType.Optimized)]
    public class BatchEventProcessorTests
    {
        //private readonly BatchEventProcessorType _targetType;
        private readonly RingBuffer<StubEvent> _ringBuffer;
        private readonly ISequenceBarrier _sequenceBarrier;
        private readonly IExceptionHandler<StubEvent> _exceptionHandler;

        public BatchEventProcessorTests()
        {
            // _targetType = targetType;

            //_ringBuffer = new RingBuffer<StubEvent>(() => new StubEvent(-1), 16);
            _ringBuffer = RingBuffer<StubEvent>.CreateMultiProducer(StubEvent.EventFactory, 16);
            _sequenceBarrier = _ringBuffer.NewBarrier();
            //_exceptionHandler = provider.Services.GetService<IExceptionHandler<StubEvent>>();

        }


        private BatchEventProcessor<T> CreateBatchEventProcessor<T>(IDataProvider<T> dataProvider, ISequenceBarrier sequenceBarrier, IEventHandler<T> eventHandler)
            where T : class
        {
            var processor = new BatchEventProcessor<T>(dataProvider, sequenceBarrier, eventHandler);
            return processor;
            //switch (_targetType)
            //{
            //    case BatchEventProcessorType.Legacy:
            //        return new BatchEventProcessor<T>(dataProvider, sequenceBarrier, eventHandler);

            //    case BatchEventProcessorType.Optimized:
            //        return BatchEventProcessorFactory.Create(dataProvider, sequenceBarrier, eventHandler);

            //    default:
            //        throw new NotSupportedException();
            //}
        }

        /// <summary>
        /// ���ÿ�ExceptionHandler��Ӧ���׳������쳣��
        /// </summary>
        [TestMethod]
        public void ShouldThrowExceptionOnSettingNullExceptionHandler()
        {
            var eventHandler = new ActionEventHandler<StubEvent>(x => throw new NullReferenceException());
            var batchEventProcessor = CreateBatchEventProcessor(_ringBuffer, _sequenceBarrier, eventHandler);

            Assert.ThrowsException<ArgumentNullException>(() => batchEventProcessor.SetExceptionHandler(null));
        }

        /// <summary>
        /// �����������¼���Ӧ�õ���OnEvent()������
        /// </summary>
        [TestMethod]
        public void ShouldCallMethodsInLifecycleOrderForBatch()
        {
            var eventSignal = new CountdownEvent(3);
            var eventHandler = new ActionEventHandler<StubEvent>(x => eventSignal.Signal());
            var batchEventProcessor = CreateBatchEventProcessor(_ringBuffer, _sequenceBarrier, eventHandler);

            _ringBuffer.AddGatingSequences(batchEventProcessor.GetSequence());

            _ringBuffer.Publish(_ringBuffer.Next());
            _ringBuffer.Publish(_ringBuffer.Next());
            _ringBuffer.Publish(_ringBuffer.Next());

            var task = Task.Run(() => batchEventProcessor.Run());

            Assert.IsTrue(eventSignal.Wait(TimeSpan.FromSeconds(2)));

            batchEventProcessor.Halt();

            Assert.IsTrue(task.Wait(500));
        }

        /// <summary>
        /// �����쳣����������δ֪�쳣��
        /// </summary>
        [TestMethod]
        public void ShouldCallExceptionHandlerOnUncaughtException()
        {
            var exceptionSignal = new CountdownEvent(1);
            var exceptionHandler = new ActionExceptionHandler<StubEvent>(x => exceptionSignal.Signal());
            var eventHandler = new ActionEventHandler<StubEvent>(x => throw new NullReferenceException());
            var batchEventProcessor = CreateBatchEventProcessor(_ringBuffer, _sequenceBarrier, eventHandler);
            _ringBuffer.AddGatingSequences(batchEventProcessor.GetSequence());

            batchEventProcessor.SetExceptionHandler(exceptionHandler);

            var task = Task.Run(() => batchEventProcessor.Run());

            _ringBuffer.Publish(_ringBuffer.Next());

            Assert.IsTrue(exceptionSignal.Wait(TimeSpan.FromSeconds(2)));

            batchEventProcessor.Halt();

            Assert.IsTrue(task.Wait(500));
        }

        [TestMethod]
        public void ReportAccurateBatchSizesAtBatchStartTime()
        {
            var batchSizes = new List<long>();
            var signal = new CountdownEvent(6);

            var batchEventProcessor = CreateBatchEventProcessor(_ringBuffer, _sequenceBarrier, new LoopbackEventHandler(_ringBuffer, batchSizes, signal));

            _ringBuffer.Publish(_ringBuffer.Next());
            _ringBuffer.Publish(_ringBuffer.Next());
            _ringBuffer.Publish(_ringBuffer.Next());

            var task = Task.Run(() => batchEventProcessor.Run());
            signal.Wait();

            batchEventProcessor.Halt();

            Assert.IsTrue(task.Wait(500));
            Assert.AreEqual(batchSizes, (new List<long> { 3, 2, 1 }));
        }

        private class LoopbackEventHandler : IEventHandler<StubEvent>, IBatchStartAware
        {
            private readonly List<long> _batchSizes;
            private readonly RingBuffer<StubEvent> _ringBuffer;
            private readonly CountdownEvent _signal;

            public LoopbackEventHandler(RingBuffer<StubEvent> ringBuffer, List<long> batchSizes, CountdownEvent signal)
            {
                _batchSizes = batchSizes;
                _ringBuffer = ringBuffer;
                _signal = signal;
            }

            public void OnBatchStart(long batchSize) => _batchSizes.Add(batchSize);

            public void OnEvent(StubEvent data, long sequence, bool endOfBatch)
            {
                if (!endOfBatch)
                {
                    _ringBuffer.Publish(_ringBuffer.Next());
                }

                _signal.Signal();
            }
        }

        [TestMethod]
        public void ShouldAlwaysHalt()
        {
            var waitStrategy = new BusySpinWaitStrategy();
            var sequencer = new SingleProducerSequencer(8, waitStrategy);
            var barrier = ProcessingSequenceBarrierFactory.Create(sequencer, waitStrategy, new Sequence(-1), new Sequence[0]);
            var dp = new DummyDataProvider<object>();

            var h1 = new LifeCycleHandler();
            var p1 = CreateBatchEventProcessor(dp, barrier, h1);

            var t1 = new Thread(p1.Run);
            p1.Halt();
            t1.Start();

            Assert.IsTrue(h1.WaitStart(TimeSpan.FromSeconds(2)));
            Assert.IsTrue(h1.WaitShutdown(TimeSpan.FromSeconds(2)));

            for (int i = 0; i < 1000; i++)
            {
                var h2 = new LifeCycleHandler();
                var p2 = CreateBatchEventProcessor(dp, barrier, h2);
                var t2 = new Thread(p2.Run);

                t2.Start();
                p2.Halt();

                Assert.IsTrue(h2.WaitStart(TimeSpan.FromSeconds(2)));
                Assert.IsTrue(h2.WaitShutdown(TimeSpan.FromSeconds(2)));
            }

            for (int i = 0; i < 1000; i++)
            {
                var h2 = new LifeCycleHandler();
                var p2 = CreateBatchEventProcessor(dp, barrier, h2);

                var t2 = new Thread(p2.Run);
                t2.Start();
                Thread.Yield();
                p2.Halt();

                Assert.IsTrue(h2.WaitStart(TimeSpan.FromSeconds(2)));
                Assert.IsTrue(h2.WaitShutdown(TimeSpan.FromSeconds(2)));
            }
        }

        private class LifeCycleHandler : IEventHandler<object>, ILifecycleAware
        {
            private readonly ManualResetEvent _startedSignal = new ManualResetEvent(false);
            private readonly ManualResetEvent _shutdownSignal = new ManualResetEvent(false);

            public void OnEvent(object data, long sequence, bool endOfBatch)
            {
            }

            public void OnStart()
            {
                _startedSignal.Set();
            }

            public void OnShutdown()
            {
                _shutdownSignal.Set();
            }

            public bool WaitStart(TimeSpan timeSpan)
            {
                return _startedSignal.WaitOne(timeSpan);
            }

            public bool WaitShutdown(TimeSpan timeSpan)
            {
                return _shutdownSignal.WaitOne(timeSpan);
            }
        }
    }
}
