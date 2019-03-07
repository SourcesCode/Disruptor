using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Yielding strategy that uses a Thread.yield() for <see cref="IEventProcessor"/>s waiting on a barrier
    /// after an initially spinning.
    /// This strategy will use 100% CPU, but will more readily give up the CPU than a busy spin strategy if other threads
    /// require CPU resource.
    /// 
    /// YieldingWaitStrategy 的实现方法是自旋 + yield + 自旋。
    /// 先自旋(100次)，不行再临时让出调度(yield)不行再自旋。
    /// 和SleepingWaitStrategy一样也是一种高性能与CPU资源之间取舍的折中方案，
    /// 但这个策略不会带来显著的延迟波动。
    /// </summary>
    public sealed class YieldingWaitStrategy : IWaitStrategy
    {
        private static readonly int SPIN_TRIES = 100;

        /// <summary>
        /// <see cref="IWaitStrategy.WaitFor"/>
        /// </summary>
        public long WaitFor(long sequence, ISequence cursor, ISequence dependentSequence, ISequenceBarrier barrier)
        {
            long availableSequence;
            int counter = SPIN_TRIES;

            while ((availableSequence = dependentSequence.Get()) < sequence)
            {
                counter = ApplyWaitMethod(barrier, counter);
            }

            return availableSequence;
        }

        /// <summary>
        /// <see cref="IWaitStrategy.SignalAllWhenBlocking"/>
        /// </summary>
        public void SignalAllWhenBlocking()
        {
        }

        /// <summary>
        /// ApplyWaitMethod
        /// </summary>
        /// <param name="barrier"></param>
        /// <param name="counter"></param>
        /// <returns></returns>
        private int ApplyWaitMethod(ISequenceBarrier barrier, int counter)
        {
            barrier.CheckAlert();

            if (0 == counter)
            {
                Thread.Yield();
            }
            else
            {
                --counter;
            }

            return counter;
        }

    }
}
