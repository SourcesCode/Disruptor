using System;

namespace Disruptor
{
    /// <summary>
    /// Used to alert <see cref="IEventProcessor"/>s waiting at a <see cref="ISequenceBarrier"/> of status changes.
    /// 
    /// It does not fill in a stack trace for performance reasons.
    /// </summary>
    public sealed class AlertException : Exception
    {
        /// <summary>
        /// Pre-allocated exception to avoid garbage generation
        /// </summary>
        public static readonly AlertException INSTANCE = new AlertException();

        /// <summary>
        /// Private constructor so only a single instance exists.
        /// </summary>
        private AlertException()
        {
        }

        /// <summary>
        /// Overridden so the stack trace is not filled in for this exception for performance reasons.
        /// </summary>
        /// <returns>this instance.</returns>
        public Exception FillInStackTrace()
        {
            return this;
        }

        /// <summary>
        /// throw an exception.
        /// </summary>
        public static void Throw()
        {
            throw INSTANCE;
        }

    }
}
