﻿using System.Threading;

namespace Disruptor.WaitStrategys
{
    /// <summary>
    /// Blocking strategy that uses a lock and condition variable for <see cref="IEventProcessor"/>s waiting on a barrier.
    /// This strategy uses a <see cref="SpinWait"/> when waiting for the dependent sequence to prevent excessive CPU usage.
    /// This strategy should be used when performance and low-latency are not as important as CPU resource.
    /// </summary>
    public sealed class BlockingSpinWaitWaitStrategy : IWaitStrategy
    {
        private readonly object _gate = new object();

        /// <summary>
        /// <see cref="IWaitStrategy.WaitFor"/>
        /// </summary>
        public long WaitFor(long sequence, ISequence cursor, ISequence dependentSequence, ISequenceBarrier barrier)
        {
            if (cursor.Get() < sequence)
            {
                lock (_gate)
                {
                    while (cursor.Get() < sequence)
                    {
                        barrier.CheckAlert();
                        Monitor.Wait(_gate);
                    }
                }
            }

            var spinWait = new SpinWait();
            long availableSequence;
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
            lock (_gate)
            {
                Monitor.PulseAll(_gate);
            }
        }

    }
}
