using System;
using Disruptor.Dsl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Disruptor.UnitTest.Support;
using Disruptor.UnitTest.Support;
using Disruptor.UnitTest.Support;

namespace Disruptor.UnitTest.Dsl
{
    [TestClass]
    public class ConsumerRepositoryTest
    {
        private readonly ConsumerRepository<TestEvent> _repository;
        private readonly IEventProcessor _processor1;
        private readonly IEventProcessor _processor2;
        private readonly SleepingEventHandler _handler1;
        private readonly SleepingEventHandler _handler2;
        private readonly ISequenceBarrier _barrier1;
        private readonly ISequenceBarrier _barrier2;

        public ConsumerRepositoryTest()
        {
            _repository = new ConsumerRepository<TestEvent>();
            _processor1 = new DummyEventProcessor();
            _processor2 = new DummyEventProcessor();

            _processor1.Run();
            _processor2.Run();

            _handler1 = new SleepingEventHandler();
            _handler2 = new SleepingEventHandler();

            _barrier1 = new DummySequenceBarrier();
            _barrier2 = new DummySequenceBarrier();
        }

        [TestMethod]
        public void ShouldGetBarrierByHandler()
        {
            _repository.Add(_processor1, _handler1, _barrier1);
            var barrier2 = _repository.GetBarrierFor(_handler1);

            Assert.Equals(barrier2, _barrier1);
        }

        [TestMethod]
        public void ShouldReturnNullForBarrierWhenHandlerIsNotRegistered()
        {
            Assert.IsNull(_repository.GetBarrierFor(_handler1));
        }

        [TestMethod]
        public void ShouldGetLastEventProcessorsInChain()
        {
            _repository.Add(_processor1, _handler1, _barrier1);
            _repository.Add(_processor2, _handler2, _barrier2);

            _repository.UnMarkEventProcessorsAsEndOfChain(_processor2.GetSequence());
            var sequences = _repository.GetLastSequenceInChain(true);

            //Assert.Single(sequences);
            Assert.Equals(sequences[0], _processor1.GetSequence());
        }

        [TestMethod]
        public void ShouldRetrieveEventProcessorForHandler()
        {
            _repository.Add(_processor1, _handler1, _barrier1);
            var processor = _repository.GetEventProcessorFor(_handler1);

            Assert.Equals(processor, _processor1);
        }

        [TestMethod]
        public void ShouldThrowExceptionWhenHandlerIsNotRegistered()
        {
            Assert.ThrowsException<ArgumentException>(() => _repository.GetEventProcessorFor(_handler1));
        }

        [TestMethod]
        public void ShouldIterateAllEventProcessors()
        {
            _repository.Add(_processor1, _handler1, _barrier1);
            _repository.Add(_processor2, _handler2, _barrier2);

            var seen1 = false;
            var seen2 = false;
            foreach (var entry in _repository)
            {
                var eventProcessorInfo = (EventProcessorInfo<TestEvent>)entry;
                if (!seen1 &&
                    eventProcessorInfo.GetEventProcessor() == _processor1 &&
                    eventProcessorInfo.GetHandler() == _handler1)
                {
                    seen1 = true;
                }
                else if (!seen2 &&
                    eventProcessorInfo.GetEventProcessor() == _processor2 &&
                    eventProcessorInfo.GetHandler() == _handler2)
                {
                    seen2 = true;
                }
                else
                {
                    throw new Exception("Unexpected eventProcessor info: " + eventProcessorInfo);
                }
            }

            Assert.IsTrue(seen1);
            Assert.IsTrue(seen2);
        }
    }
}
