using System;

namespace Disruptor.Core
{
    /// <summary>
    /// ILogger
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Log
        /// </summary>
        /// <param name="level"></param>
        /// <param name="msg"></param>
        /// <param name="ex"></param>
        void Log(Level level, String msg, Exception ex);
    }

    /// <summary>
    /// Level
    /// </summary>
    public enum Level
    {
        SEVERE,
        INFO
    }

}
