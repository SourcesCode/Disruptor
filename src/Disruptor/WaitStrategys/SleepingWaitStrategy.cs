using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Sleeping strategy that initially spins, then uses a Thread.yield(), and
    /// eventually sleep(<code>Thread.Sleep(0)</code>) for the minimum
    /// number of milliseconds the OS and CLR will allow while the
    /// <see cref="IEventProcessor"/>s are waiting on a barrier.
    /// This strategy is a good compromise between performance and CPU resource.
    /// Latency spikes can occur after quiet periods. It will also reduce the impact
    /// on the producing thread as it will not need signal any conditional variables
    /// to wake up the event handling thread.
    /// 
    /// SleepingWaitStrategy 的实现方法是自旋 + yield + sleep。
    /// 先自旋，不行再临时让出调度(yield)，不行再短暂的阻塞等待。
    /// 对于既想取得高性能，又不想太浪费CPU资源的场景，
    /// 这个策略是一种比较好的折中方案(性能和CPU资源之间)。
    /// 使用这个方案可能会出现延迟波动。
    /// </summary>
    public sealed class SleepingWaitStrategy : IWaitStrategy
    {
        private const int DEFAULT_RETRIES = 200;
        private const int DEFAULT_SLEEP = 100;

        private readonly int retries;
        private readonly int sleepMilliseconds;
        //TODO:3.3.0使用的private SpinWait spinWait = default(SpinWait);

        public SleepingWaitStrategy()
            : this(DEFAULT_RETRIES, DEFAULT_SLEEP)
        {
        }

        public SleepingWaitStrategy(int retries)
            : this(retries, DEFAULT_SLEEP)
        {
        }

        public SleepingWaitStrategy(int retries, int sleepMilliseconds)
        {
            this.retries = retries;
            this.sleepMilliseconds = sleepMilliseconds;
        }

        /// <summary>
        /// <see cref="IWaitStrategy.WaitFor"/>
        /// </summary>
        public long WaitFor(long sequence, ISequence cursor, ISequence dependentSequence, ISequenceBarrier barrier)
        {
            long availableSequence;
            int counter = retries;

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

        private int ApplyWaitMethod(ISequenceBarrier barrier, int counter)
        {
            barrier.CheckAlert();
            //从指定的重试次数(默认是200)重试到剩下100次，这个过程是自旋。
            if (counter > 100)
            {
                --counter;
            }
            else if (counter > 0) //然后尝试100次让出处理器动作。
            {
                --counter;
                //TODO:3.3.0使用的Thread.Sleep(0);
                Thread.Yield();
            }
            else//然后尝试阻塞1纳秒。
            {
                //TODO:3.3.0使用的spinWait.SpinOnce();
                //LockSupport.parkNanos(sleepMilliseconds);
                Thread.Sleep(sleepMilliseconds);
            }

            return counter;
        }

    }
}
