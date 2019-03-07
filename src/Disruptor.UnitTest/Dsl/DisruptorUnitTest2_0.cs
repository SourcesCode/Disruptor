using Disruptor.Dsl;
using Disruptor.UnitTest.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Disruptor.UnitTest.Dsl
{
    [TestClass]
    public class DisruptorTests
    {
        private const int _timeoutInSeconds = 2;
        private Disruptor<TestEvent> _disruptor;
        private StubExecutor _executor;

        private readonly List<DelayedEventHandler> _delayedEventHandlers;
        private readonly List<TestWorkHandler> _testWorkHandlers;

        private RingBuffer<TestEvent> _ringBuffer;
        private TestEvent _lastPublishedEvent;

        public DisruptorTests()
        {
            _executor = new StubExecutor();
            _disruptor = new Disruptor<TestEvent>(TestEvent.EventFactory, 4, _executor);

            _delayedEventHandlers = new List<DelayedEventHandler>();
            _testWorkHandlers = new List<TestWorkHandler>();

            _ringBuffer = null;
            _lastPublishedEvent = null;

        }

        [TestMethod]
        public void ShouldProcessMessagesPublishedBeforeStartIsCalled()
        {
            var eventCounter = new CountdownEvent(2);
            _disruptor.HandleEventsWith(new EventHandler(eventCounter));

            _disruptor.PublishEvent(new EventTranslator(_lastPublishedEvent));

            _disruptor.Start();

            _disruptor.PublishEvent(new EventTranslator(_lastPublishedEvent));

            if (!eventCounter.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new Exception($"Did not process event published before start was called. Missed events: {eventCounter.CurrentCount}");
            }
        }

        [TestMethod]
        public void ShouldBatchOfEvents()
        {
            var eventCounter = new CountdownEvent(2);
            _disruptor.HandleEventsWith(new EventHandler(eventCounter));

            _disruptor.Start();

            _disruptor.PublishEvents(new EventTranslatorOneArg(_lastPublishedEvent), new object[] { "a", "b" });

            if (!eventCounter.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new Exception($"Did not process event published before start was called. Missed events: {eventCounter.CurrentCount}");
            }
        }

        [TestMethod]
        public void ShouldProcessMessagesPublishedBeforeStartIsCalled3()
        {
            var eventCounter = new CountdownEvent(2);
            _disruptor.HandleEventsWith(new ActionEventHandler<TestEvent>(e =>
            {
                eventCounter.Signal();
            }));

            _disruptor.PublishEvent(new ActionEventTranslator<TestEvent>(e => _lastPublishedEvent = e));

            _disruptor.Start();

            _disruptor.PublishEvent(new ActionEventTranslator<TestEvent>(e => _lastPublishedEvent = e));

            if (!eventCounter.Wait(TimeSpan.FromSeconds(5)))
                Assert.Fail("Did not process event published before start was called. Missed events: " + eventCounter.CurrentCount);
        }

        [TestMethod]
        public void ShouldAddEventProcessorsAfterPublishing()
        {
            var rb = _disruptor.GetRingBuffer();

            var b1 = new BatchEventProcessor<TestEvent>(rb, rb.NewBarrier(), new SleepingEventHandler());
            var b2 = new BatchEventProcessor<TestEvent>(rb, rb.NewBarrier(b1.GetSequence()), new SleepingEventHandler());
            var b3 = new BatchEventProcessor<TestEvent>(rb, rb.NewBarrier(b2.GetSequence()), new SleepingEventHandler());

            Assert.Equals(b1.GetSequence().Get(), -1L);
            Assert.Equals(b2.GetSequence().Get(), -1L);
            Assert.Equals(b3.GetSequence().Get(), -1L);

            rb.Publish(rb.Next());
            rb.Publish(rb.Next());
            rb.Publish(rb.Next());
            rb.Publish(rb.Next());
            rb.Publish(rb.Next());
            rb.Publish(rb.Next());

            _disruptor.HandleEventsWith(b1, b2, b3);

            Assert.Equals(5L, b1.GetSequence().Get());
            Assert.Equals(5L, b2.GetSequence().Get());
            Assert.Equals(5L, b3.GetSequence().Get());
        }

        [TestMethod]
        public void ShouldSetSequenceForHandlerIfAddedAfterPublish()
        {
            var rb = _disruptor.GetRingBuffer();

            var h1 = new SleepingEventHandler();
            var h2 = new SleepingEventHandler();
            var h3 = new SleepingEventHandler();

            rb.Publish(rb.Next());
            rb.Publish(rb.Next());
            rb.Publish(rb.Next());
            rb.Publish(rb.Next());
            rb.Publish(rb.Next());
            rb.Publish(rb.Next());

            _disruptor.HandleEventsWith(h1, h2, h3);

            Assert.Equals(5L, _disruptor.GetSequenceValueFor(h1));
            Assert.Equals(5L, _disruptor.GetSequenceValueFor(h2));
            Assert.Equals(5L, _disruptor.GetSequenceValueFor(h3));
        }

        [TestMethod]
        public void ShouldSetSequenceForWorkProcessorIfAddedAfterPublish()
        {
            var rb = _disruptor.GetRingBuffer();

            var w1 = CreateTestWorkHandler();
            var w2 = CreateTestWorkHandler();
            var w3 = CreateTestWorkHandler();

            rb.Publish(rb.Next());
            rb.Publish(rb.Next());
            rb.Publish(rb.Next());
            rb.Publish(rb.Next());
            rb.Publish(rb.Next());
            rb.Publish(rb.Next());

            _disruptor.HandleEventsWithWorkerPool(w1, w2, w3);

            Assert.Equals(5L, rb.GetMinimumGatingSequence());
        }

        [TestMethod]
        public void ShouldCreateEventProcessorGroupForFirstEventProcessors()
        {
            _executor.IgnoreExecutions();

            var eventHandler1 = new SleepingEventHandler();
            var eventHandler2 = new SleepingEventHandler();

            var eventHandlerGroup = _disruptor.HandleEventsWith(eventHandler1, eventHandler2);

            _disruptor.Start();

            Assert.IsNotNull(eventHandlerGroup);
            Assert.Equals(2, _executor.GetExecutionCount());
        }

        [TestMethod]
        public void ShouldMakeEntriesAvailableToFirstHandlersImmediately()
        {
            var countDownLatch = new CountdownEvent(2);
            var eventHandler = new EventHandlerStub<TestEvent>(countDownLatch);

            _disruptor.HandleEventsWith(CreateDelayedEventHandler(), eventHandler);

            EnsureTwoEventsProcessedAccordingToDependencies(countDownLatch);
        }

        [TestMethod]
        public void ShouldWaitUntilAllFirstEventProcessorsProcessEventBeforeMakingItAvailableToDependentEventProcessors()
        {
            var eventHandler1 = CreateDelayedEventHandler();

            var countDownLatch = new CountdownEvent(2);
            IEventHandler<TestEvent> eventHandler2 = new EventHandlerStub<TestEvent>(countDownLatch);

            _disruptor.HandleEventsWith(eventHandler1).Then(eventHandler2);

            EnsureTwoEventsProcessedAccordingToDependencies(countDownLatch, eventHandler1);
        }

        [TestMethod]
        public void ShouldAllowSpecifyingSpecificEventProcessorsToWaitFor()
        {
            var handler1 = CreateDelayedEventHandler();
            var handler2 = CreateDelayedEventHandler();

            var countDownLatch = new CountdownEvent(2);
            IEventHandler<TestEvent> handlerWithBarrier = new EventHandlerStub<TestEvent>(countDownLatch);

            _disruptor.HandleEventsWith(handler1, handler2);
            _disruptor.After(handler1, handler2).HandleEventsWith(handlerWithBarrier);

            EnsureTwoEventsProcessedAccordingToDependencies(countDownLatch, handler1, handler2);
        }

        [TestMethod]
        public void ShouldWaitOnAllProducersJoinedByAnd()

        {
            var handler1 = CreateDelayedEventHandler();
            var handler2 = CreateDelayedEventHandler();

            var countDownLatch = new CountdownEvent(2);
            IEventHandler<TestEvent> handlerWithBarrier = new EventHandlerStub<TestEvent>(countDownLatch);

            _disruptor.HandleEventsWith(handler1);
            var handler2Group = _disruptor.HandleEventsWith(handler2);
            _disruptor.After(handler1).And(handler2Group).HandleEventsWith(handlerWithBarrier);

            EnsureTwoEventsProcessedAccordingToDependencies(countDownLatch, handler1, handler2);
        }

        [TestMethod]
        public void ShouldThrowExceptionIfHandlerIsNotAlreadyConsuming()
        {
            Assert.ThrowsException<ArgumentException>(() => _disruptor.After(CreateDelayedEventHandler()).HandleEventsWith(CreateDelayedEventHandler()));
        }

        [TestMethod]
        public void ShouldTrackEventHandlersByIdentityNotEquality()
        {
            var handler1 = new EvilEqualsEventHandler();
            var handler2 = new EvilEqualsEventHandler();

            _disruptor.HandleEventsWith(handler1);

            // handler2.equals(handler1) but it hasn't yet been registered so should throw exception.
            Assert.ThrowsException<ArgumentException>(() => _disruptor.After(handler2));
        }

        [TestMethod]
        public void ShouldSupportSpecifyingAExceptionHandlerForEventProcessors()
        {
            var eventHandled = new AtomicReference<Exception>();
            IExceptionHandler<object> exceptionHandler = new StubExceptionHandler(eventHandled);
            var testException = new Exception();
            var handler = new ExceptionThrowingEventHandler(testException);

            _disruptor.HandleExceptionsWith(exceptionHandler);
            _disruptor.HandleEventsWith(handler);

            PublishEvent();

            var actualException = WaitFor(eventHandled);
            Assert.AreSame(testException, actualException);
        }

        [TestMethod]
        public void ShouldOnlyApplyExceptionsHandlersSpecifiedViaHandleExceptionsWithOnNewEventProcessors()
        {
            var eventHandled = new AtomicReference<Exception>();
            IExceptionHandler<object> exceptionHandler = new StubExceptionHandler(eventHandled);
            var testException = new Exception();
            var handler = new ExceptionThrowingEventHandler(testException);

            _disruptor.HandleExceptionsWith(exceptionHandler);
            _disruptor.HandleEventsWith(handler);
            _disruptor.HandleExceptionsWith(new FatalExceptionHandler());

            PublishEvent();

            var actualException = WaitFor(eventHandled);
            Assert.AreSame(testException, actualException);
        }

        [TestMethod]
        public void ShouldSupportSpecifyingADefaultExceptionHandlerForEventProcessors()
        {
            var eventHandled = new AtomicReference<Exception>();
            IExceptionHandler<object> exceptionHandler = new StubExceptionHandler(eventHandled);
            var testException = new Exception();
            var handler = new ExceptionThrowingEventHandler(testException);

            _disruptor.SetDefaultExceptionHandler(exceptionHandler);
            _disruptor.HandleEventsWith(handler);

            PublishEvent();

            var actualException = WaitFor(eventHandled);
            Assert.AreSame(testException, actualException);
        }

        [TestMethod]
        public void ShouldApplyDefaultExceptionHandlerToExistingEventProcessors()
        {
            var eventHandled = new AtomicReference<Exception>();
            IExceptionHandler<object> exceptionHandler = new StubExceptionHandler(eventHandled);
            var testException = new Exception();
            var handler = new ExceptionThrowingEventHandler(testException);

            _disruptor.HandleEventsWith(handler);
            _disruptor.SetDefaultExceptionHandler(exceptionHandler);

            PublishEvent();

            var actualException = WaitFor(eventHandled);
            Assert.AreSame(testException, actualException);
        }

        [TestMethod]
        public void ShouldBlockProducerUntilAllEventProcessorsHaveAdvanced()
        {
            var delayedEventHandler = CreateDelayedEventHandler();
            _disruptor.HandleEventsWith(delayedEventHandler);

            var ringBuffer = _disruptor.Start();
            delayedEventHandler.AwaitStart();

            var stubPublisher = new StubPublisher(ringBuffer);
            try
            {
                _executor.Execute(stubPublisher);

                AssertProducerReaches(stubPublisher, 4, true);

                delayedEventHandler.ProcessEvent();
                delayedEventHandler.ProcessEvent();
                delayedEventHandler.ProcessEvent();
                delayedEventHandler.ProcessEvent();
                delayedEventHandler.ProcessEvent();

                AssertProducerReaches(stubPublisher, 5, false);
            }
            finally
            {
                stubPublisher.Halt();
            }
        }

        [TestMethod]
        public void ShouldBeAbleToOverrideTheExceptionHandlerForAEventProcessor()
        {
            var testException = new Exception();
            var eventHandler = new ExceptionThrowingEventHandler(testException);
            _disruptor.HandleEventsWith(eventHandler);

            var reference = new AtomicReference<Exception>();
            var exceptionHandler = new StubExceptionHandler(reference);
            _disruptor.HandleExceptionsFor(eventHandler).With(exceptionHandler);

            PublishEvent();

            WaitFor(reference);
        }

        [TestMethod]
        public void ShouldThrowExceptionWhenAddingEventProcessorsAfterTheProducerBarrierHasBeenCreated()
        {
            _executor.IgnoreExecutions();
            _disruptor.HandleEventsWith(new SleepingEventHandler());
            _disruptor.Start();

            Assert.ThrowsException<InvalidOperationException>(() => _disruptor.HandleEventsWith(new SleepingEventHandler()));
        }

        [TestMethod]
        public void ShouldThrowExceptionIfStartIsCalledTwice()
        {
            _executor.IgnoreExecutions();
            _disruptor.HandleEventsWith(new SleepingEventHandler());
            _disruptor.Start();

            Assert.ThrowsException<InvalidOperationException>(() => _disruptor.Start());
        }

        [TestMethod]
        public void ShouldSupportCustomProcessorsAsDependencies()
        {
            var ringBuffer = _disruptor.GetRingBuffer();

            var delayedEventHandler = CreateDelayedEventHandler();

            var countDownLatch = new CountdownEvent(2);
            IEventHandler<TestEvent> handlerWithBarrier = new EventHandlerStub<TestEvent>(countDownLatch);

            var processor = new BatchEventProcessor<TestEvent>(ringBuffer, ringBuffer.NewBarrier(), delayedEventHandler);
            _disruptor.HandleEventsWith(processor).Then(handlerWithBarrier);

            EnsureTwoEventsProcessedAccordingToDependencies(countDownLatch, delayedEventHandler);
        }

        [TestMethod]
        public void ShouldSupportHandlersAsDependenciesToCustomProcessors()
        {
            var delayedEventHandler = CreateDelayedEventHandler();
            _disruptor.HandleEventsWith(delayedEventHandler);

            var ringBuffer = _disruptor.GetRingBuffer();
            var countDownLatch = new CountdownEvent(2);
            IEventHandler<TestEvent> handlerWithBarrier = new EventHandlerStub<TestEvent>(countDownLatch);

            var sequenceBarrier = _disruptor.After(delayedEventHandler).AsSequenceBarrier();
            var processor = new BatchEventProcessor<TestEvent>(ringBuffer, sequenceBarrier, handlerWithBarrier);
            _disruptor.HandleEventsWith(processor);

            EnsureTwoEventsProcessedAccordingToDependencies(countDownLatch, delayedEventHandler);
        }

        [TestMethod]
        public void ShouldSupportCustomProcessorsAndHandlersAsDependencies()
        {
            var delayedEventHandler1 = CreateDelayedEventHandler();
            var delayedEventHandler2 = CreateDelayedEventHandler();
            _disruptor.HandleEventsWith(delayedEventHandler1);

            var ringBuffer = _disruptor.GetRingBuffer();
            var countDownLatch = new CountdownEvent(2);
            IEventHandler<TestEvent> handlerWithBarrier = new EventHandlerStub<TestEvent>(countDownLatch);

            var sequenceBarrier = _disruptor.After(delayedEventHandler1).AsSequenceBarrier();
            var processor = new BatchEventProcessor<TestEvent>(ringBuffer, sequenceBarrier, delayedEventHandler2);

            _disruptor.After(delayedEventHandler1).And(processor).HandleEventsWith(handlerWithBarrier);

            EnsureTwoEventsProcessedAccordingToDependencies(countDownLatch, delayedEventHandler1, delayedEventHandler2);
        }

        [TestMethod]
        public void ShouldProvideEventsToWorkHandlers()
        {
            var workHandler1 = CreateTestWorkHandler();
            var workHandler2 = CreateTestWorkHandler();
            _disruptor.HandleEventsWithWorkerPool(workHandler1, workHandler2);

            PublishEvent();
            PublishEvent();

            workHandler1.ProcessEvent();
            workHandler2.ProcessEvent();
        }

        [TestMethod]
        public void ShouldProvideEventsMultipleWorkHandlers()
        {
            var workHandler1 = CreateTestWorkHandler();
            var workHandler2 = CreateTestWorkHandler();
            var workHandler3 = CreateTestWorkHandler();
            var workHandler4 = CreateTestWorkHandler();
            var workHandler5 = CreateTestWorkHandler();
            var workHandler6 = CreateTestWorkHandler();
            var workHandler7 = CreateTestWorkHandler();
            var workHandler8 = CreateTestWorkHandler();

            _disruptor
                .HandleEventsWithWorkerPool(workHandler1, workHandler2)
                .ThenHandleEventsWithWorkerPool(workHandler3, workHandler4);
            _disruptor
                .HandleEventsWithWorkerPool(workHandler5, workHandler6)
                .ThenHandleEventsWithWorkerPool(workHandler7, workHandler8);
        }

        [TestMethod]
        public void ShouldSupportUsingWorkerPoolAsDependency()
        {
            var workHandler1 = CreateTestWorkHandler();
            var workHandler2 = CreateTestWorkHandler();
            var delayedEventHandler = CreateDelayedEventHandler();
            _disruptor.HandleEventsWithWorkerPool(workHandler1, workHandler2).Then(delayedEventHandler);

            PublishEvent();
            PublishEvent();


            Assert.Equals(_disruptor.GetBarrierFor(delayedEventHandler).GetCursor(), -1L);


            workHandler2.ProcessEvent();
            workHandler1.ProcessEvent();

            delayedEventHandler.ProcessEvent();
        }

        [TestMethod]
        public void ShouldSupportUsingWorkerPoolAsDependencyAndProcessFirstEventAsSoonAsItIsAvailable()
        {
            var workHandler1 = CreateTestWorkHandler();
            var workHandler2 = CreateTestWorkHandler();
            var delayedEventHandler = CreateDelayedEventHandler();
            _disruptor.HandleEventsWithWorkerPool(workHandler1, workHandler2).Then(delayedEventHandler);

            PublishEvent();
            PublishEvent();

            workHandler1.ProcessEvent();
            delayedEventHandler.ProcessEvent();

            workHandler2.ProcessEvent();
            delayedEventHandler.ProcessEvent();
        }

        [TestMethod]
        public void ShouldSupportUsingWorkerPoolWithADependency()
        {
            var workHandler1 = CreateTestWorkHandler();
            var workHandler2 = CreateTestWorkHandler();
            var delayedEventHandler = CreateDelayedEventHandler();
            _disruptor.HandleEventsWith(delayedEventHandler).ThenHandleEventsWithWorkerPool(workHandler1, workHandler2);

            PublishEvent();
            PublishEvent();

            delayedEventHandler.ProcessEvent();
            delayedEventHandler.ProcessEvent();

            workHandler1.ProcessEvent();
            workHandler2.ProcessEvent();
        }

        [TestMethod]
        public void ShouldSupportCombiningWorkerPoolWithEventHandlerAsDependencyWhenNotPreviouslyRegistered()

        {
            var workHandler1 = CreateTestWorkHandler();
            var delayedEventHandler1 = CreateDelayedEventHandler();
            var delayedEventHandler2 = CreateDelayedEventHandler();
            _disruptor.HandleEventsWith(delayedEventHandler1).And(_disruptor.HandleEventsWithWorkerPool(workHandler1)).Then(
                delayedEventHandler2);

            PublishEvent();
            PublishEvent();

            delayedEventHandler1.ProcessEvent();
            delayedEventHandler1.ProcessEvent();

            workHandler1.ProcessEvent();
            delayedEventHandler2.ProcessEvent();

            workHandler1.ProcessEvent();
            delayedEventHandler2.ProcessEvent();
        }

        [TestMethod]
        public void ShouldThrowTimeoutExceptionIfShutdownDoesNotCompleteNormally()
        {
            //Given
            var delayedEventHandler = CreateDelayedEventHandler();
            _disruptor.HandleEventsWith(delayedEventHandler);
            PublishEvent();

            //When
            Assert.ThrowsException<TimeoutException>(() => _disruptor.Shutdown(TimeSpan.FromSeconds(1)));
        }

        [TestMethod]
        public void ShouldTrackRemainingCapacity()
        {
            long[] remainingCapacity = { -1 };
            //Given
            IEventHandler<TestEvent> eventHandler = new TestEventHandler(_disruptor, remainingCapacity);

            _disruptor.HandleEventsWith(eventHandler);

            //When
            PublishEvent();

            //Then
            while (remainingCapacity[0] == -1)
            {
                Thread.Sleep(100);
            }
            Assert.Equals(remainingCapacity[0], _ringBuffer.GetBufferSize() - 1L);
            Assert.Equals(_disruptor.GetRingBuffer().RemainingCapacity(), _ringBuffer.GetBufferSize() - 0L);
        }

        [TestMethod]
        public void ShouldAllowEventHandlerWithSuperType()
        {
            var latch = new CountdownEvent(2);
            IEventHandler<TestEvent> objectHandler = new EventHandlerStub<TestEvent>(latch);

            _disruptor.HandleEventsWith(objectHandler);

            EnsureTwoEventsProcessedAccordingToDependencies(latch);
        }

        [TestMethod]
        public void ShouldAllowChainingEventHandlersWithSuperType()
        {
            var latch = new CountdownEvent(2);
            var delayedEventHandler = CreateDelayedEventHandler();
            IEventHandler<TestEvent> objectHandler = new EventHandlerStub<TestEvent>(latch);

            _disruptor.HandleEventsWith(delayedEventHandler).Then(objectHandler);

            EnsureTwoEventsProcessedAccordingToDependencies(latch, delayedEventHandler);
        }

        [TestMethod]
        public void ShouldMakeEntriesAvailableToFirstCustomProcessorsImmediately()
        {
            var countDownLatch = new CountdownEvent(2);
            IEventHandler<TestEvent> eventHandler = new EventHandlerStub<TestEvent>(countDownLatch);

            _disruptor.HandleEventsWith(new EventProcessorFactory(_disruptor, eventHandler, 0));

            EnsureTwoEventsProcessedAccordingToDependencies(countDownLatch);
        }

        [TestMethod]
        public void ShouldHonourDependenciesForCustomProcessors()
        {
            var countDownLatch = new CountdownEvent(2);
            IEventHandler<TestEvent> eventHandler = new EventHandlerStub<TestEvent>(countDownLatch);
            var delayedEventHandler = CreateDelayedEventHandler();

            _disruptor.HandleEventsWith(delayedEventHandler).Then(new EventProcessorFactory(_disruptor, eventHandler, 1));

            EnsureTwoEventsProcessedAccordingToDependencies(countDownLatch, delayedEventHandler);
        }

        private TestWorkHandler CreateTestWorkHandler()
        {
            var testWorkHandler = new TestWorkHandler();
            _testWorkHandlers.Add(testWorkHandler);
            return testWorkHandler;
        }

        private void EnsureTwoEventsProcessedAccordingToDependencies(CountdownEvent countDownLatch, params DelayedEventHandler[] dependencies)
        {
            PublishEvent();
            PublishEvent();

            foreach (var dependency in dependencies)
            {
                AssertThatCountDownLatchEquals(countDownLatch, 2L);
                dependency.ProcessEvent();
                dependency.ProcessEvent();
            }

            AssertThatCountDownLatchIsZero(countDownLatch);
        }

        private TestEvent PublishEvent()
        {
            if (_ringBuffer == null)
            {
                _ringBuffer = _disruptor.Start();
                foreach (var eventHandler in _delayedEventHandlers)
                {
                    eventHandler.AwaitStart();
                }
            }

            _disruptor.PublishEvent(new EventTranslator(_lastPublishedEvent));

            return _lastPublishedEvent;
        }

        private static void AssertProducerReaches(StubPublisher stubPublisher, int expectedPublicationCount, bool strict)
        {
            var loopStart = DateTime.UtcNow;
            while (stubPublisher.GetPublicationCount() < expectedPublicationCount && DateTime.UtcNow - loopStart < TimeSpan.FromMilliseconds(5))
            {
                Thread.Yield();
            }

            if (strict)
            {
                Assert.Equals(stubPublisher.GetPublicationCount(), expectedPublicationCount);
            }
            else
            {
                var actualPublicationCount = stubPublisher.GetPublicationCount();
                Assert.IsTrue(actualPublicationCount >= expectedPublicationCount, "Producer reached unexpected count. Expected at least " + expectedPublicationCount + " but only reached " + actualPublicationCount);
            }
        }

        private static Exception WaitFor(AtomicReference<Exception> reference)
        {
            while (reference.Read() == null)
            {
                Thread.Yield();
            }

            return reference.Read();
        }

        private DelayedEventHandler CreateDelayedEventHandler()
        {
            var delayedEventHandler = new DelayedEventHandler();
            _delayedEventHandlers.Add(delayedEventHandler);
            return delayedEventHandler;
        }

        private class EventHandler : IEventHandler<TestEvent>
        {
            private readonly CountdownEvent _counter;
            public EventHandler(CountdownEvent counter)
            {
                _counter = counter;
            }

            public void OnEvent(TestEvent @event, long sequence, bool endOfBatch)
            {
                _counter.Signal();
            }
        }

        private class EventTranslator : IEventTranslator<TestEvent>
        {
            private TestEvent _event;

            public EventTranslator(TestEvent lastPublishedEvent)
            {
                _event = lastPublishedEvent;
            }

            public void TranslateTo(TestEvent @event, long sequence)
            {
                _event = @event;
            }
        }

        public class EventTranslatorOneArg : IEventTranslatorOneArg<TestEvent, object>
        {
            private TestEvent _event;

            public EventTranslatorOneArg(TestEvent lastPublishedEvent)
            {
                _event = lastPublishedEvent;
            }

            public void TranslateTo(TestEvent @event, long sequence, object arg)
            {
                _event = @event;
            }
        }







        private void AssertThatCountDownLatchEquals(CountdownEvent countDownLatch, long expectedCountDownValue)
        {
            Assert.Equals(countDownLatch.CurrentCount, expectedCountDownValue);
        }

        private void AssertThatCountDownLatchIsZero(CountdownEvent countDownLatch)
        {
            var released = countDownLatch.Wait(TimeSpan.FromSeconds(_timeoutInSeconds));
            Assert.IsTrue(released, "Batch handler did not receive entries: " + countDownLatch.CurrentCount);
        }

    }
}
