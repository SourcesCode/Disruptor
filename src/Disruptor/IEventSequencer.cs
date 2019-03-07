namespace Disruptor
{
    /// <summary>
    /// IEventSequencer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IEventSequencer<T> : IDataProvider<T>, ISequenced//<out T>
    {
    }
}
