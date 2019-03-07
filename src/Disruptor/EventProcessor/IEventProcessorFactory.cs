﻿namespace Disruptor.Dsl
{
    /// <summary>
    /// A factory interface to make it possible to include custom event processors in a chain:
    /// 
    /// <code>
    /// disruptor.handleEventsWith(handler1).then((ringBuffer, barrierSequences) -> new CustomEventProcessor(ringBuffer, barrierSequences));
    /// </code>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEventProcessorFactory<T> where T : class
    {
        /// <summary>
        /// Create a new event processor that gates on <paramref name="barrierSequences"/>.
        /// </summary>
        /// <param name="ringBuffer">the ring buffer to receive events from.</param>
        /// <param name="barrierSequences">the sequences to gate on.</param>
        /// <returns>a new <see cref="IEventProcessor"/> that gates on <paramref name="barrierSequences"/> before processing events.</returns>
        IEventProcessor CreateEventProcessor(RingBuffer<T> ringBuffer, ISequence[] barrierSequences);

    }
}
