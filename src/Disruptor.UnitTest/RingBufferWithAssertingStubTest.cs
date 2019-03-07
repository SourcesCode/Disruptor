using Disruptor.Tests.Support;
using Disruptor.UnitTest.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.Tests
{
    [TestClass]
    public class RingBufferWithAssertingStubTest
    {
        private readonly RingBuffer<StubEvent> _ringBuffer;
        private readonly ISequencer _sequencer;

        public RingBufferWithAssertingStubTest()
        {
            _sequencer = new AssertingSequencer(16);
            _ringBuffer = new RingBuffer<StubEvent>(_sequencer, StubEvent.EventFactory);
        }

        [TestMethod]
        public void ShouldDelegateNextAndPublish()
        {
            _ringBuffer.Publish(_ringBuffer.Next());
        }

        [TestMethod]
        public void ShouldDelegateTryNextAndPublish()
        {
            var sequence = _ringBuffer.TryNext();
            _ringBuffer.Publish(sequence);
        }

        [TestMethod]
        public void ShouldDelegateNextNAndPublish()
        {
            long hi = _ringBuffer.Next(10);
            _ringBuffer.Publish(hi - 9, hi);
        }

        [TestMethod]
        public void ShouldDelegateTryNextNAndPublish()
        {
            long hi = _ringBuffer.TryNext(10);
            _ringBuffer.Publish(hi - 9, hi);
        }


        private class AssertingSequencer : ISequencer
        {
            private readonly int _size;
            private long _lastBatchSize = -1;
            private long _lastValue = -1;
            public AssertingSequencer(int size)
            {
                _size = size;
            }

            public long GetCursor()
            {
                return _lastValue;
            }

            public int GetBufferSize()
            {
                return _size;
            }

            public bool HasAvailableCapacity(int requiredCapacity)
            {
                //requiredCapacity <= _size;
                return requiredCapacity < _size;
            }

            public long RemainingCapacity()
            {
                return _size;
            }

            public long Next()
            {
                _lastValue = ThreadLocalRandom.Current.Next(0, 1000000);
                _lastBatchSize = 1;
                return _lastValue;
            }

            public long Next(int n)
            {
                _lastValue = ThreadLocalRandom.Current.Next(n, 1000000);
                _lastBatchSize = n;
                return _lastValue;
            }

            public long TryNext()
            {
                var sequence = Next();
                return sequence;
            }

            public long TryNext(int n)
            {
                var sequence = Next(n);
                return sequence;
            }

            public void Publish(long sequence)
            {
                Assert.AreEqual(sequence, _lastValue);
                Assert.AreEqual(1L, _lastBatchSize);
            }

            public void Publish(long lo, long hi)
            {
                Assert.AreEqual(hi, _lastValue);
                Assert.AreEqual((hi - lo) + 1L, _lastBatchSize);
            }

            public void Claim(long sequence)
            {

            }
            public bool IsAvailable(long sequence)
            {
                return false;
            }


            public void AddGatingSequences(params ISequence[] gatingSequences)
            {

            }

            public bool RemoveGatingSequence(ISequence sequence)
            {
                return false;
            }

            public ISequenceBarrier NewBarrier(params ISequence[] sequencesToTrack)
            {
                return null;
            }

            public long GetMinimumSequence()
            {
                return 0;
            }

            public long GetHighestPublishedSequence(long nextSequence, long availableSequence)
            {
                return 0;
            }

            public EventPoller<T> NewPoller<T>(IDataProvider<T> provider, params ISequence[] gatingSequences)
            {
                return null;
            }


        }
    }
}
