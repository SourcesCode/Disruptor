using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Disruptor.Dsl;
using Disruptor.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Disruptor.UnitTest.Support;

namespace Disruptor.Tests
{
    [TestClass]
    public class BatchingTest
    {
        public static IEnumerable<object[]> GenerateData()
        {
            yield return new object[] { ProducerType.MULTI };
            yield return new object[] { ProducerType.SINGLE };
        }

        private class ParallelEventHandler : IEventHandler<TestEvent>
        {
            private readonly long _mask;
            private readonly long _ordinal;
            private const int _batchSize = 10;

            public long EventCount;
            public long BatchCount;
            public long PublishedValue;
            public long TempValue;
            public long Processed;

            public ParallelEventHandler(long mask, long ordinal)
            {
                _mask = mask;
                _ordinal = ordinal;
            }

            public void OnEvent(TestEvent @event, long sequence, bool endOfBatch)
            {
                if ((sequence & _mask) == _ordinal)
                {
                    EventCount++;
                    TempValue = @event.Value;
                }

                if (endOfBatch || ++BatchCount >= _batchSize)
                {
                    PublishedValue = TempValue;
                    BatchCount = 0;
                }
                else
                {
                    Thread.Sleep(0); // LockSupport.parkNanos(1);
                }
                //_processed = sequence;
                Volatile.Write(ref Processed, sequence);
            }
        }

        //[TestCaseSource(nameof(GenerateData))]
        public void ShouldBatch(ProducerType producerType)
        {
            var d = new Disruptor<TestEvent>(TestEvent.EventFactory, 2048, TaskScheduler.Current, producerType, new SleepingWaitStrategy());

            var handler1 = new ParallelEventHandler(1, 0);
            var handler2 = new ParallelEventHandler(1, 1);

            d.HandleEventsWith(handler1, handler2);

            var buffer = d.Start();

            IEventTranslator<TestEvent> translator = new EventTranslator<TestEvent>();

            const int eventCount = 10000;
            for (var i = 0; i < eventCount; i++)
            {
                buffer.PublishEvent(translator);
            }

            while (Volatile.Read(ref handler1.Processed) != eventCount - 1 ||
                   Volatile.Read(ref handler2.Processed) != eventCount - 1)
            {
                Thread.Sleep(1);
            }

            Assert.AreEqual(handler1.PublishedValue, ((long)eventCount - 2));
            Assert.AreEqual(handler1.EventCount, ((long)eventCount / 2));
            Assert.AreEqual(handler2.PublishedValue, ((long)eventCount - 1));
            Assert.AreEqual(handler2.EventCount, ((long)eventCount / 2));
        }

        private class EventTranslator<T> : IEventTranslator<TestEvent>
        {
            public void TranslateTo(TestEvent eventData, long sequence)
            {
                eventData.Value = sequence;
            }
        }
    }
}
