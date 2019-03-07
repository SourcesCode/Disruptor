using Disruptor.Core;
using System;

namespace Disruptor
{
    /// <summary>
    /// Convenience implementation of an exception handler that using Console.WriteLine to log
    /// the exception as <see cref="Level.INFO"/>.
    /// </summary>
    public sealed class IgnoreExceptionHandler : IExceptionHandler<Object>
    {
        //private static final Logger LOGGER = Logger.getLogger(IgnoreExceptionHandler.class.getName());
        private readonly ILogger logger;

        /// <summary>
        /// IgnoreExceptionHandler
        /// </summary>
        public IgnoreExceptionHandler()
        {
            //this.logger = LOGGER;
        }

        /// <summary>
        /// IgnoreExceptionHandler
        /// </summary>
        /// <param name="logger"></param>
        public IgnoreExceptionHandler(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// HandleEventException
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="sequence"></param>
        /// <param name="event"></param>
        public void HandleEventException(Exception ex, long sequence, Object @event)
        {
            logger.Log(Level.INFO, "Exception processing: " + sequence + " " + @event, ex);
        }

        /// <summary>
        /// HandleOnStartException
        /// </summary>
        /// <param name="ex"></param>
        public void HandleOnStartException(Exception ex)
        {
            logger.Log(Level.INFO, "Exception during onStart()", ex);
        }

        /// <summary>
        /// HandleOnShutdownException
        /// </summary>
        /// <param name="ex"></param>
        public void HandleOnShutdownException(Exception ex)
        {
            logger.Log(Level.INFO, "Exception during onShutdown()", ex);
        }

    }
}
