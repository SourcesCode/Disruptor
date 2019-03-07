namespace Disruptor.Tests.Support
{
    public class DummyDataProvider<T> : IDataProvider<T>
    {
        //public T this[long sequence] => default(T);

        public T Get(long sequence)
        {
            return default(T);
        }
    }
}
