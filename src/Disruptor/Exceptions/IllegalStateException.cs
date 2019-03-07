using System;

namespace Disruptor
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class IllegalStateException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public IllegalStateException()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public IllegalStateException(string message)
            : base(message)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception"></param>
        public IllegalStateException(Exception exception)
            : base(string.Empty, exception)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public IllegalStateException(string message, Exception exception)
            : base(message, exception)
        { }

    }
}
