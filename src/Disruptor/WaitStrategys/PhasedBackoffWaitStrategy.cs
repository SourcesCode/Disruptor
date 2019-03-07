using System;
using System.Diagnostics;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Phased wait strategy for waiting <see cref="IEventProcessor"/>s on a barrier.
    /// 
    /// This strategy can be used when throughput and low-latency are not as important as CPU resource.
    /// Spins, then yields, then waits using the configured fallback WaitStrategy.
    /// 
    /// PhasedBackoffWaitStrategy 的实现方法是自旋 + yield + 自定义策略。
    /// CPU资源紧缺，吞吐量和延迟并不重要的场景。
    /// 先自旋(10000次)，不行再临时让出调度(yield)，不行再使用其他的策略进行等待。
    /// 可以根据具体场景自行设置自旋时间、yield时间和备用等待策略。
    /// </summary>
    public sealed class PhasedBackoffWaitStrategy : IWaitStrategy
    {
        private static readonly int SPIN_TRIES = 10000;
        //private readonly TimeSpan _spinTimeoutNanos;
        //private readonly TimeSpan _yieldTimeoutNanos;
        private readonly long _spinTimeoutTicks;
        private readonly long _yieldTimeoutTicks;
        private readonly IWaitStrategy fallbackStrategy;

        /// <summary>
        /// PhasedBackoffWaitStrategy
        /// </summary>
        /// <param name="spinTimeout"></param>
        /// <param name="yieldTimeout"></param>
        /// <param name="fallbackStrategy"></param>
        public PhasedBackoffWaitStrategy(long spinTimeoutTicks, long yieldTimeoutTicks, IWaitStrategy fallbackStrategy)
        {
            //this._spinTimeoutNanos = spinTimeout;
            //this._yieldTimeoutNanos = spinTimeoutNanos.Add(yieldTimeout);
            _spinTimeoutTicks = spinTimeoutTicks;
            _yieldTimeoutTicks = spinTimeoutTicks + yieldTimeoutTicks;
            this.fallbackStrategy = fallbackStrategy;
        }

        /// <summary>
        /// PhasedBackoffWaitStrategy
        /// </summary>
        /// <param name="spinTimeout"></param>
        /// <param name="yieldTimeout"></param>
        /// <param name="fallbackStrategy"></param>
        public PhasedBackoffWaitStrategy(TimeSpan spinTimeout, TimeSpan yieldTimeout, IWaitStrategy fallbackStrategy)
        {
            //this._spinTimeoutNanos = spinTimeout;
            //this._yieldTimeoutNanos = spinTimeoutNanos.Add(yieldTimeout);
            _spinTimeoutTicks = spinTimeout.Ticks;
            _yieldTimeoutTicks = spinTimeout.Ticks + yieldTimeout.Ticks;
            this.fallbackStrategy = fallbackStrategy;
        }

        /// <summary>
        /// Construct {@link PhasedBackoffWaitStrategy} with fallback to {@link BlockingWaitStrategy}
        /// </summary>
        /// <param name="spinTimeout">The maximum time in to busy spin for.</param>
        /// <param name="yieldTimeout">The maximum time in to yield for.</param>
        /// <returns>The constructed wait strategy.</returns>
        public static PhasedBackoffWaitStrategy WithLock(TimeSpan spinTimeout, TimeSpan yieldTimeout)
        {
            return new PhasedBackoffWaitStrategy(spinTimeout, yieldTimeout, new BlockingWaitStrategy());
        }

        /// <summary>
        /// Construct {@link PhasedBackoffWaitStrategy} with fallback to {@link LiteBlockingWaitStrategy}
        /// </summary>
        /// <param name="spinTimeout">The maximum time in to busy spin for.</param>
        /// <param name="yieldTimeout">The maximum time in to yield for.</param>
        /// <returns>The constructed wait strategy.</returns>
        public static PhasedBackoffWaitStrategy WithLiteLock(TimeSpan spinTimeout, TimeSpan yieldTimeout)
        {
            return new PhasedBackoffWaitStrategy(spinTimeout, yieldTimeout, new LiteBlockingWaitStrategy());
        }

        /// <summary>
        /// Construct {@link PhasedBackoffWaitStrategy} with fallback to {@link SleepingWaitStrategy}
        /// </summary>
        /// <param name="spinTimeout">The maximum time in to busy spin for.</param>
        /// <param name="yieldTimeout">The maximum time in to yield for.</param>
        /// <returns>The constructed wait strategy.</returns>
        public static PhasedBackoffWaitStrategy WithSleep(TimeSpan spinTimeout, TimeSpan yieldTimeout)
        {
            return new PhasedBackoffWaitStrategy(spinTimeout, yieldTimeout, new SleepingWaitStrategy(0));
        }

        /// <summary>
        /// <see cref="IWaitStrategy.WaitFor"/>
        /// </summary>
        public long WaitFor(long sequence, ISequence cursor, ISequence dependentSequence, ISequenceBarrier barrier)
        {
            long availableSequence;
            long startTime = 0;
            int counter = SPIN_TRIES;
            //var stopWatch = Stopwatch.StartNew();
            do
            {
                if ((availableSequence = dependentSequence.Get()) >= sequence)
                {
                    return availableSequence;
                }

                if (0 == --counter)
                {
                    if (0 == startTime)
                    {
                        //startTime = System.nanoTime();
                        //TODO:3.3.0使用的startTime = stopWatch.ElapsedTicks;
                        startTime = DateTime.Now.Ticks;
                    }
                    else
                    {
                        //long timeDelta = System.nanoTime() - startTime;
                        //TODO:3.3.0使用的var timeDelta = stopWatch.Elapsed;
                        var timeDelta = DateTime.Now.Ticks - startTime;
                        if (timeDelta > _yieldTimeoutTicks)
                        {
                            return fallbackStrategy.WaitFor(sequence, cursor, dependentSequence, barrier);
                        }
                        else if (timeDelta > _spinTimeoutTicks)
                        {
                            //TODO:3.3.0使用的Thread.Sleep(0);
                            Thread.Yield();
                        }
                    }
                    counter = SPIN_TRIES;
                }
            }
            while (true);
        }

        /// <summary>
        /// <see cref="IWaitStrategy.SignalAllWhenBlocking"/>
        /// </summary>
        public void SignalAllWhenBlocking()
        {
            fallbackStrategy.SignalAllWhenBlocking();
        }

    }
}
