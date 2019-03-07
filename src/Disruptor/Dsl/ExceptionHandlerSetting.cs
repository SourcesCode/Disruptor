﻿namespace Disruptor.Dsl
{
    /// <summary>
    /// A support class used as part of setting an exception handler for a specific event handler.
    /// 
    /// For example:
    /// <pre><code>disruptorWizard.handleExceptionsIn(eventHandler).with(exceptionHandler);</code></pre>
    /// </summary>
    /// <typeparam name="T">the type of event being handled.</typeparam>
    public class ExceptionHandlerSetting<T> where T : class
    {
        private readonly IEventHandler<T> eventHandler;
        private readonly ConsumerRepository<T> consumerRepository;

        internal ExceptionHandlerSetting(
            IEventHandler<T> eventHandler,
            ConsumerRepository<T> consumerRepository)
        {
            this.eventHandler = eventHandler;
            this.consumerRepository = consumerRepository;
        }

        /// <summary>
        /// Specify the <see cref="IExceptionHandler{T}"/> to use with the event handler.
        /// </summary>
        /// <param name="exceptionHandler">the exception handler to use.</param>
        public void With(IExceptionHandler<T> exceptionHandler)
        {
            IEventProcessor eventProcessor = consumerRepository.GetEventProcessorFor(eventHandler);
            if (eventProcessor is BatchEventProcessor<T>)
            {
                ((BatchEventProcessor<T>)eventProcessor).SetExceptionHandler(exceptionHandler);
                consumerRepository.GetBarrierFor(eventHandler).Alert();
            }
            else
            {
                throw new RuntimeException(
                    "EventProcessor: " + eventProcessor + " is not a BatchEventProcessor " +
                    "and does not support exception handlers");
            }
        }

    }
}
