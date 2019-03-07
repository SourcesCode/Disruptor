using System;
using System.Threading;
using System.Threading.Tasks;
using Disruptor.Dsl;
using Disruptor.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Disruptor.UnitTest.Support;

namespace Disruptor.Tests
{
    //[TestFixture(ProducerType.Single)]
    //[TestFixture(ProducerType.Multi)]
    public class SequencerTests
    {
        private const int _bufferSize = 16;
        private readonly ProducerType _producerType;
        private ISequencer _sequencer;
        private Sequence _gatingSequence;

        public SequencerTests(ProducerType producerType)
        {
            _producerType = producerType;
        }

        private ISequencer NewProducer(ProducerType producerType, int bufferSize, IWaitStrategy waitStrategy)
        {
            switch (producerType)
            {
                case ProducerType.SINGLE:
                    return new SingleProducerSequencer(bufferSize, waitStrategy);
                case ProducerType.MULTI:
                    return new MultiProducerSequencer(bufferSize, waitStrategy);
                default:
                    throw new ArgumentOutOfRangeException(nameof(producerType), producerType, null);
            }
        }


        public void SetUp()
        {
            _gatingSequence = new Sequence();
            _sequencer = NewProducer(_producerType, _bufferSize, new BlockingWaitStrategy());
        }

        [TestMethod]
        public void ShouldStartWithInitialValue()
        {
            Assert.AreEqual(0, _sequencer.Next());
        }

        [TestMethod]
        public void ShouldBatchClaim()
        {
            Assert.AreEqual(3, _sequencer.Next(4));
        }

        [TestMethod]
        public void ShouldIndicateHasAvailableCapacity()
        {
            _sequencer.AddGatingSequences(_gatingSequence);

            Assert.IsTrue(_sequencer.HasAvailableCapacity(1));
            Assert.IsTrue(_sequencer.HasAvailableCapacity(_bufferSize));
            Assert.IsFalse(_sequencer.HasAvailableCapacity(_bufferSize + 1));

            _sequencer.Publish(_sequencer.Next());

            Assert.IsTrue(_sequencer.HasAvailableCapacity(_bufferSize - 1));
            Assert.IsFalse(_sequencer.HasAvailableCapacity(_bufferSize));
        }

        [TestMethod]
        public void ShouldIndicateNoAvailableCapacity()
        {
            _sequencer.AddGatingSequences(_gatingSequence);

            var sequence = _sequencer.Next(_bufferSize);
            _sequencer.Publish(sequence - (_bufferSize - 1), sequence);

            Assert.IsFalse(_sequencer.HasAvailableCapacity(1));
        }

        [TestMethod]
        public void ShouldHoldUpPublisherWhenBufferIsFull()
        {
            _sequencer.AddGatingSequences(_gatingSequence);
            var sequence = _sequencer.Next(_bufferSize);
            _sequencer.Publish(sequence - (_bufferSize - 1), sequence);

            var waitingSignal = new ManualResetEvent(false);
            var doneSignal = new ManualResetEvent(false);

            var expectedFullSequence = Sequence.INITIAL_VALUE + _sequencer.GetBufferSize();
            Assert.AreEqual(_sequencer.GetCursor(), (expectedFullSequence));

            Task.Run(() =>
            {
                waitingSignal.Set();

                var next = _sequencer.Next();
                _sequencer.Publish(next);

                doneSignal.Set();
            });

            waitingSignal.WaitOne(TimeSpan.FromMilliseconds(500));
            Assert.AreEqual(_sequencer.GetCursor(), (expectedFullSequence));

            _gatingSequence.Set(Sequence.INITIAL_VALUE + 1L);

            doneSignal.WaitOne(TimeSpan.FromMilliseconds(500));
            Assert.AreEqual(_sequencer.GetCursor(), (expectedFullSequence + 1L));
        }

        [TestMethod]
        public void ShouldReturnFalseWhenSequencerIsFull()
        {
            _sequencer.AddGatingSequences(_gatingSequence);

            for (var i = 0; i < _bufferSize; i++)
            {
                _sequencer.Next();
            }
            var sequence = _sequencer.TryNext();
            //Assert.IsFalse();
        }

        [TestMethod]
        public void ShouldCalculateRemainingCapacity()
        {
            _sequencer.AddGatingSequences(_gatingSequence);

            Assert.AreEqual(_sequencer.RemainingCapacity(), (_bufferSize));

            for (var i = 1; i < _bufferSize; i++)
            {
                _sequencer.Next();
                Assert.AreEqual(_sequencer.RemainingCapacity(), (_bufferSize - i));
            }
        }

        [TestMethod]
        public void ShoundNotBeAvailableUntilPublished()
        {
            var next = _sequencer.Next(6);

            for (var i = 0; i <= 5; i++)
            {
                Assert.AreEqual(_sequencer.IsAvailable(i), false);
            }

            _sequencer.Publish(next - (6 - 1), next);

            for (var i = 0; i <= 5; i++)
            {
                Assert.AreEqual(_sequencer.IsAvailable(i), true);
            }

            Assert.AreEqual(_sequencer.IsAvailable(6), false);
        }

        [TestMethod]
        public void ShouldNotifyWaitStrategyOnPublish()
        {
            var waitStrategy = new DummyWaitStrategy();
            var sequencer = NewProducer(_producerType, _bufferSize, waitStrategy);

            sequencer.Publish(sequencer.Next());

            Assert.AreEqual(waitStrategy.SignalAllWhenBlockingCalls, (1));
        }

        [TestMethod]
        public void ShouldNotNotifyNonBlockingWaitStrategyOnPublish()
        {
            var waitStrategy = new DummyNonBlockingWaitStrategy();
            var sequencer = NewProducer(_producerType, _bufferSize, waitStrategy);

            sequencer.Publish(sequencer.Next());

            Assert.AreEqual(waitStrategy.SignalAllWhenBlockingCalls, (0));
        }

        [TestMethod]
        public void ShouldNotifyWaitStrategyOnPublishBatch()
        {
            var waitStrategy = new DummyWaitStrategy();
            var sequencer = NewProducer(_producerType, _bufferSize, waitStrategy);

            var next = _sequencer.Next(4);
            sequencer.Publish(next - (4 - 1), next);

            Assert.AreEqual(waitStrategy.SignalAllWhenBlockingCalls, (1));
        }

        [TestMethod]
        public void ShouldNotNotifyNonBlockingWaitStrategyOnPublishBatch()
        {
            var waitStrategy = new DummyNonBlockingWaitStrategy();
            var sequencer = NewProducer(_producerType, _bufferSize, waitStrategy);

            var next = _sequencer.Next(4);
            sequencer.Publish(next - (4 - 1), next);

            Assert.AreEqual(waitStrategy.SignalAllWhenBlockingCalls, (0));
        }

        [TestMethod]
        public void ShouldWaitOnPublication()
        {
            var barrier = _sequencer.NewBarrier();

            var next = _sequencer.Next(10);
            var lo = next - (10 - 1);
            var mid = next - 5;

            for (var l = lo; l < mid; l++)
            {
                _sequencer.Publish(l);
            }

            Assert.AreEqual(barrier.WaitFor(-1), (mid - 1));

            for (var l = mid; l <= next; l++)
            {
                _sequencer.Publish(l);
            }
            Assert.AreEqual(barrier.WaitFor(-1), (next));
        }

        [TestMethod]
        public void ShouldTryNextOut()
        {
            _sequencer.AddGatingSequences(_gatingSequence);

            for (int i = 0; i < _bufferSize; i++)
            {
                var sequence = _sequencer.TryNext();
                //Assert.AreEqual(succeeded, true);
                Assert.AreEqual(sequence, (i));

                _sequencer.Publish(i);
            }
            var temp = _sequencer.TryNext();
            //Assert.AreEqual(_sequencer.TryNext(out var _), false);
        }

        [TestMethod]
        public void ShouldTryNextNOut()
        {
            _sequencer.AddGatingSequences(_gatingSequence);

            for (int i = 0; i < _bufferSize; i += 2)
            {
                var sequence = _sequencer.TryNext(2);
                //Assert.AreEqual(succeeded, true);
                Assert.AreEqual(sequence, (i + 1));

                _sequencer.Publish(i);
                _sequencer.Publish(i + 1);
            }
            var temp = _sequencer.TryNext(1);
            //Assert.AreEqual(_sequencer.TryNext(1, out var _), false);
        }

        [TestMethod]
        public void ShouldClaimSpecificSequence()
        {
            long sequence = 14L;

            _sequencer.Claim(sequence);
            _sequencer.Publish(sequence);
            Assert.AreEqual(_sequencer.Next(), (sequence + 1));
        }

        [TestMethod]
        public void ShouldNotAllowBulkNextLessThanZero()
        {
            Assert.ThrowsException<ArgumentException>(() => _sequencer.Next(-1));
        }

        [TestMethod]
        public void ShouldNotAllowBulkNextOfZero()
        {
            Assert.ThrowsException<ArgumentException>(() => _sequencer.Next(0));
        }

        [TestMethod]
        public void ShouldNotAllowBulkTryNextOutLessThanZero()
        {
            Assert.ThrowsException<ArgumentException>(() => _sequencer.TryNext(-1));
        }

        [TestMethod]
        public void ShouldNotAllowBulkTryNextOutOfZero()
        {
            Assert.ThrowsException<ArgumentException>(() => _sequencer.TryNext(0));
        }
    }
}
