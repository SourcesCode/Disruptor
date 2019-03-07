using System;

namespace Disruptor
{
    /// <summary>
    /// Callback interface to be implemented for processing events as they become available in the <see cref="RingBuffer{T}"/>.
    /// </summary>
    /// <typeparam name="T">event implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    /// <remarks>See <see cref="BatchEventProcessor{T}.SetExceptionHandler"/> if you want to handle exceptions propagated out of the handler.</remarks>
    public interface IEventHandler<T>//<in T>
    {
        /// <summary>
        /// Called when a publisher has published an event to the <see cref="RingBuffer{T}"/>.
        /// 
        /// The <see cref="BatchEventProcessor{T}"/> will
        /// read messages from the <see cref="RingBuffer{T}"/> in batches, where a batch is all of the events available to be
        /// processed without having to wait for any new event to arrive.  This can be useful for event handlers that need
        /// to do slower operations like I/O as they can group together the data from multiple events into a single
        /// operation.  Implementations should ensure that the operation is always performed when endOfBatch is true as
        /// the time between that message an the next one is inderminate.
        /// </summary>
        /// <param name="event">published to the <see cref="RingBuffer{T}"/></param>
        /// <param name="sequence">of the event being processed</param>
        /// <param name="endOfBatch">flag to indicate if this is the last event in a batch from the <see cref="RingBuffer{T}"/></param>
        /// <exception cref="Exception">if the EventHandler would like the exception handled further up the chain.</exception>
        void OnEvent(T @event, long sequence, Boolean endOfBatch);

    }
}
