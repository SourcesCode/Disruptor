using Disruptor.Core;
using System;

namespace Disruptor
{
    /// <summary>
    /// Convenience implementation of an exception handler that using standard Console.Writeline to log
    /// the exception as <see cref="Level.SEVERE"/> and re-throw it wrapped in a <see cref="RuntimeException"/>.
    /// </summary>
    public sealed class FatalExceptionHandler : IExceptionHandler<Object>
    {
        //private static final Logger LOGGER = Logger.getLogger(FatalExceptionHandler.class.getName());
        private readonly ILogger logger;

        /// <summary>
        /// FatalExceptionHandler
        /// </summary>
        public FatalExceptionHandler()
        {
            //this.logger = LOGGER;
        }

        /// <summary>
        /// FatalExceptionHandler
        /// </summary>
        /// <param name="logger"></param>
        public FatalExceptionHandler(ILogger logger)
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
            logger.Log(Level.SEVERE, "Exception processing: " + sequence + " " + @event, ex);
            throw new RuntimeException(ex);
        }

        /// <summary>
        /// HandleOnStartException
        /// </summary>
        /// <param name="ex"></param>
        public void HandleOnStartException(Exception ex)
        {
            logger.Log(Level.SEVERE, "Exception during onStart()", ex);
        }

        /// <summary>
        /// HandleOnShutdownException
        /// </summary>
        /// <param name="ex"></param>
        public void HandleOnShutdownException(Exception ex)
        {
            logger.Log(Level.SEVERE, "Exception during onShutdown()", ex);
        }

    }
}
