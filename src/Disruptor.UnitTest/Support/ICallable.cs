namespace Disruptor.UnitTest.Support
{
    public interface ICallable<TResult>
    {
        TResult Call();
    }
}
