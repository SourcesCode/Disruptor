using System;

namespace Disruptor
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RuntimeException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public RuntimeException()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public RuntimeException(string message)
            : base(message)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception"></param>
        public RuntimeException(Exception exception)
            : base(string.Empty, exception)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public RuntimeException(string message, Exception exception)
            : base(message, exception)
        { }

    }
}
