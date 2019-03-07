﻿using System;

namespace Disruptor.Tests.Support
{
    public class ActionExceptionHandler<T> : IExceptionHandler<T>
    {
        private readonly Action<T> _action;

        public ActionExceptionHandler(Action<T> action)
        {
            _action = action;
        }

        public void HandleEventException(Exception ex, long sequence, T evt)
        {
            _action.Invoke(evt);
        }

        public void HandleOnStartException(Exception ex)
        {
        }

        public void HandleOnShutdownException(Exception ex)
        {
        }
    }
}
