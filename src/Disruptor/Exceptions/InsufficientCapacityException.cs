using System;

namespace Disruptor
{
    /// <summary>
    /// Exception thrown when it is not possible to insert a value into
    /// the ring buffer without it wrapping the consuming sequences.
    /// Used specifically when claiming with the <see cref="RingBuffer{T}.TryNext()"/> call.
    /// 
    /// For efficiency this exception will not have a stack trace.
    /// </summary>
    public sealed class InsufficientCapacityException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly InsufficientCapacityException INSTANCE = new InsufficientCapacityException();

        /// <summary>
        /// 
        /// </summary>
        private InsufficientCapacityException()
        {
            // Singleton
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Exception FillInStackTrace()
        {
            return this;
        }

    }
}
