namespace Disruptor
{
    /// <summary>
    /// Called by the <see cref="RingBuffer{T}"/> to pre-populate all the events to fill the RingBuffer.
    /// </summary>
    /// <typeparam name="T">event implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public interface IEventFactory<T>//<out T>
    {
        /// <summary>
        /// Implementations should instantiate an event object, with all memory already allocated where possible.
        /// </summary>
        /// <returns></returns>
        T NewInstance();

    }
}
