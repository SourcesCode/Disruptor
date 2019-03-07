using Disruptor.UnitTest.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Disruptor.Tests
{
    [TestClass]
    public class SequenceBarrierTests
    {
        private RingBuffer<StubEvent> _ringBuffer;


        public void SetUp()
        {
            _ringBuffer = RingBuffer<StubEvent>.CreateMultiProducer(StubEvent.EventFactory, 64);
            _ringBuffer.AddGatingSequences(new NoOpEventProcessor<StubEvent>(_ringBuffer).GetSequence());
        }

        [TestMethod]
        public void ShouldWaitForWorkCompleteWhereCompleteWorkThresholdIsAhead()
        {
            const int expectedNumberMessages = 10;
            const int expectedWorkSequence = 9;
            FillRingBuffer(expectedNumberMessages);

            var sequence1 = new Sequence(expectedNumberMessages);
            var sequence2 = new Sequence(expectedWorkSequence);
            var sequence3 = new Sequence(expectedNumberMessages);

            var sequenceBarrier = _ringBuffer.NewBarrier(sequence1, sequence2, sequence3);

            var completedWorkSequence = sequenceBarrier.WaitFor(expectedWorkSequence);
            Assert.IsTrue(completedWorkSequence >= expectedWorkSequence);
        }

        [TestMethod]
        public void ShouldWaitForWorkCompleteWhereAllWorkersAreBlockedOnRingBuffer()
        {
            const long expectedNumberMessages = 10;
            FillRingBuffer(expectedNumberMessages);

            var workers = new StubEventProcessor[3];
            for (var i = 0; i < workers.Length; i++)
            {
                workers[i] = new StubEventProcessor(expectedNumberMessages - 1);
            }

            var dependencyBarrier = _ringBuffer.NewBarrier(Util.GetSequencesFor(workers));

            Task.Run(() =>
                    {
                        var sequence = _ringBuffer.Next();
                        _ringBuffer.Get(sequence).Value = (int)sequence;
                        _ringBuffer.Publish(sequence);

                        foreach (var stubWorker in workers)
                        {
                            stubWorker.GetSequence().Set(sequence);
                        }
                    });

            const long expectedWorkSequence = expectedNumberMessages;
            var completedWorkSequence = dependencyBarrier.WaitFor(expectedNumberMessages);
            Assert.IsTrue(completedWorkSequence >= expectedWorkSequence);
        }

        [TestMethod]
        public void ShouldInterruptDuringBusySpin()
        {
            const long expectedNumberMessages = 10;
            FillRingBuffer(expectedNumberMessages);

            var signal = new CountdownEvent(3);
            var sequence1 = new CountDownEventSequence(8L, signal);
            var sequence2 = new CountDownEventSequence(8L, signal);
            var sequence3 = new CountDownEventSequence(8L, signal);

            var sequenceBarrier = _ringBuffer.NewBarrier(sequence1, sequence2, sequence3);

            var alerted = false;
            var t = Task.Run(() =>
                            {
                                try
                                {
                                    sequenceBarrier.WaitFor(expectedNumberMessages - 1);
                                }
                                catch (AlertException)
                                {
                                    alerted = true;
                                }
                            });

            signal.Wait(TimeSpan.FromSeconds(3));
            sequenceBarrier.Alert();
            t.Wait();

            Assert.AreEqual(alerted, true, "Thread was not interrupted");
        }

        [TestMethod]
        public void ShouldWaitForWorkCompleteWhereCompleteWorkThresholdIsBehind()
        {
            const long expectedNumberMessages = 10;
            FillRingBuffer(expectedNumberMessages);

            var eventProcessors = new StubEventProcessor[3];
            for (var i = 0; i < eventProcessors.Length; i++)
            {
                eventProcessors[i] = new StubEventProcessor(expectedNumberMessages - 2);
            }

            var eventProcessorBarrier = _ringBuffer.NewBarrier(Util.GetSequencesFor(eventProcessors));

            Task.Factory.StartNew(() =>
                                  {
                                      foreach (var stubWorker in eventProcessors)
                                      {
                                          stubWorker.GetSequence().Set(stubWorker.GetSequence().Get() + 1);
                                      }
                                  }).Wait();

            const long expectedWorkSequence = expectedNumberMessages - 1;
            var completedWorkSequence = eventProcessorBarrier.WaitFor(expectedWorkSequence);
            Assert.IsTrue(completedWorkSequence >= expectedWorkSequence);
        }

        [TestMethod]
        public void ShouldSetAndClearAlertStatus()
        {
            var sequenceBarrier = _ringBuffer.NewBarrier();
            Assert.IsFalse(sequenceBarrier.IsAlerted());

            sequenceBarrier.Alert();
            Assert.IsTrue(sequenceBarrier.IsAlerted());

            sequenceBarrier.ClearAlert();
            Assert.IsFalse(sequenceBarrier.IsAlerted());
        }

        private void FillRingBuffer(long expectedNumberEvents)
        {
            for (var i = 0; i < expectedNumberEvents; i++)
            {
                var sequence = _ringBuffer.Next();
                var @event = _ringBuffer.Get(sequence);
                @event.Value = i;
                _ringBuffer.Publish(sequence);
            }
        }

        private class StubEventProcessor : IEventProcessor
        {
            private volatile int _running;
            private readonly Sequence _sequence = new Sequence();

            public StubEventProcessor(long sequence)
            {
                _sequence.Set(sequence);
            }

            public void Run()
            {
                if (Interlocked.Exchange(ref _running, 1) != 0)
                    throw new InvalidOperationException("Already running");
            }

            public void Halt()
            {
                _running = 0;
            }

            public ISequence GetSequence()
            {
                return _sequence;
            }

            public bool IsRunning()
            {
                return _running == 1;
            }

        }

        private class CountDownEventSequence : ISequence
        {
            private readonly CountdownEvent _signal;
            private readonly ISequence _sequenceImplementation;

            public CountDownEventSequence(long initialValue, CountdownEvent signal)
            {
                _sequenceImplementation = new Sequence(initialValue);
                _signal = signal;
            }

            public long Get()
            {
                if (_signal.CurrentCount > 0)
                    _signal.Signal();

                return _sequenceImplementation.Get();
            }

            public void Set(long value)
            {
                _sequenceImplementation.Set(value);
            }

            public void SetVolatile(long value)
            {
                _sequenceImplementation.SetVolatile(value);
            }

            public bool CompareAndSet(long expectedSequence, long nextSequence)
            {
                return _sequenceImplementation.CompareAndSet(expectedSequence, nextSequence);
            }

            public long IncrementAndGet()
            {
                return _sequenceImplementation.IncrementAndGet();
            }

            public long AddAndGet(long value)
            {
                return _sequenceImplementation.AddAndGet(value);
            }


        }
    }
}