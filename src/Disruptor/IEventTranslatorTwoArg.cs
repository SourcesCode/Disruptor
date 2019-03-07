﻿namespace Disruptor
{
    /// <summary>
    /// Implementations translate another data representations into events claimed from the <see cref="RingBuffer{T}"/>.
    /// </summary>
    /// <typeparam name="T">event implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    /// <typeparam name="A"></typeparam>
    /// <typeparam name="B"></typeparam>
    public interface IEventTranslatorTwoArg<T, A, B>//<in T, in A, in B>
    {
        /// <summary>
        /// Translate a data representation into fields set in given event.
        /// </summary>
        /// <param name="event">into which the data should be translated.</param>
        /// <param name="sequence">that is assigned to event.</param>
        /// <param name="arg0">The first user specified argument to the translator</param>
        /// <param name="arg1">The second user specified argument to the translator</param>
        void TranslateTo(T @event, long sequence, A arg0, B arg1);

    }
}
