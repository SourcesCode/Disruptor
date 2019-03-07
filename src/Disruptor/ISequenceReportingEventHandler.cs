﻿namespace Disruptor
{
    /// <summary>
    /// Used by the <see cref="BatchEventProcessor{T}"/> to set a callback allowing the <see cref="IEventHandler{T}"/> to notify
    /// when it has finished consuming an event if this happens after the <see cref="IEventHandler{T}.OnEvent(T, long, bool)"/> call.
    /// 
    /// Typically this would be used when the handler is performing some sort of batching operation such as writing to an IO
    /// device; after the operation has completed, the implementation should call <see cref="Sequence.Set(long)"/> to update the
    /// sequence and allow other processes that are dependent on this handler to progress.
    /// </summary>
    /// <typeparam name="T">event implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public interface ISequenceReportingEventHandler<T> : IEventHandler<T>//<in T> : EventHandler<T>
    {
        /// <summary>
        /// Call by the <see cref="BatchEventProcessor{T}"/> to setup the callback.
        /// </summary>
        /// <param name="sequenceCallback">callback on which to notify the <see cref="BatchEventProcessor{T}"/> that the sequence has progressed.</param>
        void SetSequenceCallback(ISequence sequenceCallback);

    }
}
