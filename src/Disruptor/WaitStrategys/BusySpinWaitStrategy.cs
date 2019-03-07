using Disruptor.Core;

namespace Disruptor
{
    /// <summary>
    /// Busy Spin strategy that uses a busy spin loop for <see cref="IEventProcessor"/>s waiting on a barrier.
    /// 
    /// This strategy will use CPU resource to avoid syscalls which can introduce latency jitter.
    /// It is best used when threads can be bound to specific CPU cores.
    /// 
    /// BusySpinWaitStrategy 的实现方法是自旋，自旋等待策略。
    /// 
    /// 这种策略会利用CPU资源来避免系统调用带来的延迟抖动，
    /// 当线程可以绑定到指定CPU(核)的时候可以使用这个策略。
    /// 
    /// 通过不断重试，减少切换线程导致的系统调用，而降低延迟。
    /// 推荐在线程绑定到固定的CPU的场景下使用。
    /// </summary>
    public sealed class BusySpinWaitStrategy : IWaitStrategy
    {
        /// <summary>
        /// <see cref="IWaitStrategy.WaitFor"/>
        /// </summary>
        public long WaitFor(long sequence, ISequence cursor, ISequence dependentSequence, ISequenceBarrier barrier)
        {
            long availableSequence;
            //var spinWait = default(AggressiveSpinWait);

            while ((availableSequence = dependentSequence.Get()) < sequence)
            {
                barrier.CheckAlert();
                //自旋
                //spinWait.SpinOnce();
            }

            return availableSequence;
        }

        /// <summary>
        /// <see cref="IWaitStrategy.SignalAllWhenBlocking"/>
        /// </summary>
        public void SignalAllWhenBlocking()
        {
        }

    }
}
