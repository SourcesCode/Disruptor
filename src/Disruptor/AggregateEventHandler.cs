using System;

namespace Disruptor
{
    /// <summary>
    /// An aggregate collection of <see cref="IEventHandler{T}"/>s that get called in sequence for each event.
    /// </summary>
    /// <typeparam name="T">event implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public sealed class AggregateEventHandler<T> : IEventHandler<T>, ILifecycleAware
    {
        private readonly IEventHandler<T>[] eventHandlers;

        /// <summary>
        /// Construct an aggregate collection of <see cref="IEventHandler{T}"/>s to be called in sequence.
        /// </summary>
        /// <param name="eventHandlers">to be called in sequence.</param>
        public AggregateEventHandler(params IEventHandler<T>[] eventHandlers)
        {
            this.eventHandlers = eventHandlers;
        }

        /// <summary>
        /// Called once on thread start before first event is available.
        /// </summary>
        public void OnStart()
        {
            foreach (IEventHandler<T> eventHandler in eventHandlers)
            {
                if (eventHandler is ILifecycleAware)
                {
                    ((ILifecycleAware)eventHandler).OnStart();
                }
            }
        }

        /// <summary>
        /// Called once just before the thread is shutdown.
        /// </summary>
        public void OnShutdown()
        {
            foreach (IEventHandler<T> eventHandler in eventHandlers)
            {
                if (eventHandler is ILifecycleAware)
                {
                    ((ILifecycleAware)eventHandler).OnShutdown();
                }
            }
        }

        /// <summary>
        /// Called when a publisher has published an event to the <see cref="RingBuffer{T}"/>.
        /// </summary>
        /// <param name="event">published to the <see cref="RingBuffer{T}"/></param>
        /// <param name="sequence">of the event being processed</param>
        /// <param name="endOfBatch">flag to indicate if this is the last event in a batch from the <see cref="RingBuffer{T}"/></param>
        public void OnEvent(T @event, long sequence, Boolean endOfBatch)
        {
            foreach (IEventHandler<T> eventHandler in eventHandlers)
            {
                eventHandler.OnEvent(@event, sequence, endOfBatch);
            }

        }

    }
}
