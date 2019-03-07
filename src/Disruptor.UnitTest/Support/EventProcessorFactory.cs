using Disruptor.Dsl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Disruptor.UnitTest.Support
{
    public class EventProcessorFactory : IEventProcessorFactory<TestEvent>
    {
        private readonly Disruptor<TestEvent> _disruptor;
        private readonly IEventHandler<TestEvent> _eventHandler;
        private readonly int _sequenceLength;

        public EventProcessorFactory(Disruptor<TestEvent> disruptor, IEventHandler<TestEvent> eventHandler, int sequenceLength)
        {
            _disruptor = disruptor;
            _eventHandler = eventHandler;
            _sequenceLength = sequenceLength;
        }

        public IEventProcessor CreateEventProcessor(RingBuffer<TestEvent> ringBuffer, ISequence[] barrierSequences)
        {
            Assert.AreEqual(_sequenceLength, barrierSequences.Length, "Should not have had any barrier sequences");
            var processor = new BatchEventProcessor<TestEvent>(_disruptor.GetRingBuffer(), ringBuffer.NewBarrier(barrierSequences), _eventHandler);
            return processor;
        }
    }
}
