namespace Disruptor
{
    /// <summary>
    /// ITimeoutHandler
    /// </summary>
    public interface ITimeoutHandler
    {
        /// <summary>
        /// OnTimeout
        /// </summary>
        /// <param name="sequence"></param>
        void OnTimeout(long sequence);

    }
}
