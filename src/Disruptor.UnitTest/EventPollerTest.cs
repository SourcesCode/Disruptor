using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using Disruptor.UnitTest.Support.EventFactorys;

namespace Disruptor.Tests
{
    [TestClass]
    public class EventPollerTest
    {
        /// <summary>
        /// 测试EventPoller的轮询状态
        /// </summary>
        [TestMethod]
        public void ShouldPollForEvents()
        {
            var gatingSequence = new Sequence();
            var sequencer = new SingleProducerSequencer(16, new BusySpinWaitStrategy());

            //bool Handler(object e, long s, bool b) => false;
            var Handler = new EventPollerHandler();

            var data = new object[16];
            var provider = new DataProvider(data);

            var poller = sequencer.NewPoller(provider, gatingSequence);
            var evt = new object();
            data[0] = evt;

            Assert.AreEqual(poller.Poll(Handler), (EventPoller<object>.PollState.IDLE));

            // Publish Event.
            sequencer.Publish(sequencer.Next());
            Assert.AreEqual(poller.Poll(Handler), (EventPoller<object>.PollState.GATING));

            gatingSequence.IncrementAndGet();

            Assert.AreEqual(poller.Poll(Handler), (EventPoller<object>.PollState.PROCESSING));
        }

        /// <summary>
        /// 成功轮询处理RingBuffer里的所有事件
        /// </summary>
        [TestMethod]
        public void ShouldSuccessfullyPollWhenBufferIsFull()
        {
            var events = new List<byte[]>();

            ByteArrayEventFactory Factory = new ByteArrayEventFactory(1);

            //bool Handler(byte[] data, long sequence, bool endOfBatch)
            //{
            //    events.Add(data);
            //    return !endOfBatch;
            //}

            var Handler = new DummyEventPollerHandler(events);

            var ringBuffer = RingBuffer<byte[]>.CreateMultiProducer(Factory, 4, new SleepingWaitStrategy());

            var poller = ringBuffer.NewPoller();
            ringBuffer.AddGatingSequences(poller.GetSequence());

            const int count = 4;

            for (byte i = 1; i <= count; ++i)
            {
                var next = ringBuffer.Next();
                ringBuffer.Get(next)[0] = i;
                ringBuffer.Publish(next);
            }

            // think of another thread
            poller.Poll(Handler);

            Assert.AreEqual(events.Count, (4));
        }

        private class DataProvider : IDataProvider<object>
        {
            private readonly object[] _data;

            public DataProvider(object[] data)
            {
                _data = data;
            }

            public object Get(long sequence)
            {
                return _data[sequence];
            }

        }

        private class EventPollerHandler : EventPoller<object>.IHandler<object>
        {
            public bool OnEvent(object @event, long sequence, bool endOfBatch)
            {
                return false;
            }
        }
        private class DummyEventPollerHandler : EventPoller<byte[]>.IHandler<byte[]>
        {
            private readonly List<byte[]> _events;

            public DummyEventPollerHandler(List<byte[]> events)
            {
                _events = events;
            }

            public bool OnEvent(byte[] @event, long sequence, bool endOfBatch)
            {
                _events.Add(@event);
                return !endOfBatch;
            }
        }

    }
}
