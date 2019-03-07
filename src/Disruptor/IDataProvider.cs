namespace Disruptor
{
    /// <summary>
    /// IDataProvider
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDataProvider<T>//<out T>
    {
        /// <summary>
        /// Get
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        T Get(long sequence);

    }
}
