using System;

namespace Disruptor
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class TimeoutException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly TimeoutException INSTANCE = new TimeoutException();

        /// <summary>
        /// 
        /// </summary>
        private TimeoutException()
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
