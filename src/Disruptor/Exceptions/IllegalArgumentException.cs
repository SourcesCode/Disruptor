using System;

namespace Disruptor
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class IllegalArgumentException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public IllegalArgumentException()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public IllegalArgumentException(string message)
            : base(message)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception"></param>
        public IllegalArgumentException(Exception exception)
            : base(string.Empty, exception)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public IllegalArgumentException(string message, Exception exception)
            : base(message, exception)
        { }

    }
}
