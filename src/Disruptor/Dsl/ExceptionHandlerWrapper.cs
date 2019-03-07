using System;

namespace Disruptor.Dsl
{
    /// <summary>
    /// ExceptionHandlerWrapper
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ExceptionHandlerWrapper<T> : IExceptionHandler<T> where T : class
    {
        private IExceptionHandler<T> _delegate = new FatalExceptionHandler();

        public void SwitchTo(IExceptionHandler<T> exceptionHandler)
        {
            _delegate = exceptionHandler;
        }


        public void HandleEventException(Exception ex, long sequence, T @event)
        {
            _delegate.HandleEventException(ex, sequence, @event);
        }


        public void HandleOnStartException(Exception ex)
        {
            _delegate.HandleOnStartException(ex);
        }


        public void HandleOnShutdownException(Exception ex)
        {
            _delegate.HandleOnShutdownException(ex);
        }

    }
}
