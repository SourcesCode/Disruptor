using Disruptor.Core;
using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Blocking strategy that uses a lock and condition variable for <see cref="IEventProcessor"/>s waiting on a barrier.
    /// However it will periodically wake up if it has been idle for specified period by throwing a
    /// <see cref="TimeoutException"/>. To make use of this, the event handler class should implement the <see cref="ITimeoutHandler"/>,
    /// which the <see cref="BatchEventProcessor{T}"/> will call if the timeout occurs.
    /// 
    /// This strategy can be used when throughput and low-latency are not as important as CPU resource.
    /// 
    /// TimeoutBlockingWaitStrategy 的实现方法是加锁 + 超时限制。
    /// CPU资源紧缺，吞吐量和延迟并不重要的场景。
    /// 阻塞给定的时间，超过时间的话会抛出超时异常。
    /// </summary>
    public sealed class TimeoutBlockingWaitStrategy : IWaitStrategy
    {
        private static readonly Object mutex = new Object();

        private readonly int _timeoutInMilliseconds;

        /// <summary>
        /// TimeoutBlockingWaitStrategy
        /// </summary>
        /// <param name="timeout"></param>
        public TimeoutBlockingWaitStrategy(int timeoutInMilliseconds)
        {
            _timeoutInMilliseconds = timeoutInMilliseconds;
        }

        /// <summary>
        /// LiteTimeoutBlockingWaitStrategy
        /// </summary>
        /// <param name="timeout"></param>
        public TimeoutBlockingWaitStrategy(TimeSpan timeout)
        {
            _timeoutInMilliseconds = (int)timeout.TotalMilliseconds;
        }

        /// <summary>
        /// <see cref="IWaitStrategy.WaitFor"/>
        /// </summary>
        public long WaitFor(long sequence, ISequence cursorSequence, ISequence dependentSequence, ISequenceBarrier barrier)
        {
            int timeoutInMilliseconds = _timeoutInMilliseconds;

            long availableSequence;
            if (cursorSequence.Get() < sequence)
            {
                bool lockToken = false;
                Monitor.Enter(mutex, ref lockToken);
                try
                {
                    while (cursorSequence.Get() < sequence)
                    {
                        barrier.CheckAlert();
                        bool waitFlag = Monitor.Wait(mutex, timeoutInMilliseconds);
                        if (!waitFlag)
                        {
                            throw TimeoutException.INSTANCE;
                        }
                    }
                }
                finally
                {
                    if (lockToken)
                        Monitor.Exit(mutex);
                }
            }
            var spinWait = new AggressiveSpinWait();
            while ((availableSequence = dependentSequence.Get()) < sequence)
            {
                barrier.CheckAlert();
                spinWait.SpinOnce();
            }

            return availableSequence;
        }

        /// <summary>
        /// <see cref="IWaitStrategy.SignalAllWhenBlocking"/>
        /// </summary>
        public void SignalAllWhenBlocking()
        {
            //TODO:3.3.0使用的
            //bool lockToken = false;
            //Monitor.Enter(mutex, ref lockToken);
            //try
            //{
            //    Monitor.PulseAll(mutex);
            //}
            //finally
            //{
            //    if (lockToken)
            //        Monitor.Exit(mutex);
            //}
            lock (mutex)
                Monitor.PulseAll(mutex);
        }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override String ToString()
        {
            return "TimeoutBlockingWaitStrategy{" +
                "mutex=" + mutex +
                ", timeoutInNanos=" + _timeoutInMilliseconds +
                '}';
        }

    }
}
