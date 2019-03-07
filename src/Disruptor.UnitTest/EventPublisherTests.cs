using Disruptor.UnitTest.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.Tests
{
    [TestClass]
    public class EventPublisherTests : IEventTranslator<TestEvent>
    {
        private const int _bufferSize = 32;
        private const long _valueAdd = 29L;
        private RingBuffer<TestEvent> _ringBuffer;

        /// <summary>
        /// 发布事件
        /// </summary>
        [TestMethod]
        public void ShouldPublishEvent()
        {
            _ringBuffer = RingBuffer<TestEvent>.CreateMultiProducer(new LongEventEventFactory(), _bufferSize);

            _ringBuffer.AddGatingSequences(new NoOpEventProcessor<TestEvent>(_ringBuffer).GetSequence());

            _ringBuffer.PublishEvent(this);
            _ringBuffer.PublishEvent(this);

            Assert.AreEqual(0L + _valueAdd, _ringBuffer.Get(0).Value);
            Assert.AreEqual(1L + _valueAdd, _ringBuffer.Get(1).Value);
        }

        /// <summary>
        /// 尝试发布事件
        /// </summary>
        [TestMethod]
        public void ShouldTryPublishEvent()
        {
            _ringBuffer.AddGatingSequences(new Sequence());

            for (var i = 0; i < _bufferSize; i++)
            {
                Assert.IsTrue(_ringBuffer.TryPublishEvent(this));
            }

            for (var i = 0; i < _bufferSize; i++)
            {
                Assert.AreEqual(_ringBuffer.Get(i).Value, (i + _valueAdd));
            }

            Assert.IsFalse(_ringBuffer.TryPublishEvent(this));
        }

        public void TranslateTo(TestEvent eventData, long sequence)
        {
            eventData.Value = sequence + 29;
        }
    }
}
