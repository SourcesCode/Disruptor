namespace Disruptor.Core
{
    /// <summary>
    /// IExecutor
    /// </summary>
    public interface IExecutor
    {
        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="command"></param>
        void Execute(IRunnable command);

    }
}
