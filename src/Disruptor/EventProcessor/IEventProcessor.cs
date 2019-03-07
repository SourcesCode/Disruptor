using Disruptor.Core;
using System;

namespace Disruptor
{
    /// <summary>
    /// An EventProcessor needs to be an implementation of a runnable that will poll for events from the <see cref="RingBuffer{T}"/>
    /// using the appropriate wait strategy.  It is unlikely that you will need to implement this interface yourself.
    /// Look at using the <see cref="IEventHandler{T}"/> interface along with the pre-supplied BatchEventProcessor in the first
    /// instance.
    /// 
    /// An EventProcessor will generally be associated with a Thread for execution.
    /// </summary>
    public interface IEventProcessor : IRunnable
    {
        /// <summary>
        /// Get a reference to the <see cref="ISequence"/> being used by this <see cref="IEventProcessor"/>.
        /// </summary>
        /// <returns>reference to the <see cref="ISequence"/> for this <see cref="IEventProcessor"/></returns>
        ISequence GetSequence();

        /// <summary>
        /// Signal that this <see cref="IEventProcessor"/> should stop when it has finished consuming at the next clean break.
        /// It will call <see cref="ISequenceBarrier.Alert"/> to notify the thread to check status.
        /// </summary>
        void Halt();

        /// <summary>
        /// Gets if the processor is running.
        /// </summary>
        /// <returns></returns>
        Boolean IsRunning();

    }
}
