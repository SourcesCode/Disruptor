using Disruptor.Core;
using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Variation of the <see cref="TimeoutBlockingWaitStrategy"/> that attempts to elide conditional wake-ups
    /// when the lock is uncontended.
    /// </summary>
    public class LiteTimeoutBlockingWaitStrategy : IWaitStrategy
    {
        private static readonly Object mutex = new Object();
        private _Volatile.Boolean signalNeeded = new _Volatile.Boolean(false);
        private readonly int _timeoutInMilliseconds;

        /// <summary>
        /// LiteTimeoutBlockingWaitStrategy
        /// </summary>
        /// <param name="timeout"></param>
        public LiteTimeoutBlockingWaitStrategy(int timeoutInMilliseconds)
        {
            _timeoutInMilliseconds = timeoutInMilliseconds;
        }

        /// <summary>
        /// LiteTimeoutBlockingWaitStrategy
        /// </summary>
        /// <param name="timeout"></param>
        public LiteTimeoutBlockingWaitStrategy(TimeSpan timeout)
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
                lock (mutex)
                {
                    do
                    {
                        //signalNeeded.getAndSet(true);
                        signalNeeded.AtomicExchange(true);
                        if (cursorSequence.Get() >= sequence)
                        {
                            break;
                        }
                        barrier.CheckAlert();
                        bool waitFlag = Monitor.Wait(mutex, timeoutInMilliseconds);
                        if (!waitFlag)
                        {
                            throw TimeoutException.INSTANCE;
                        }
                    } while (cursorSequence.Get() < sequence);
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
            //if (signalNeeded.getAndSet(false))
            if (signalNeeded.AtomicExchange(false) == true)
            {
                lock (mutex)
                {
                    Monitor.PulseAll(mutex);
                }
            }
        }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override String ToString()
        {
            return "LiteTimeoutBlockingWaitStrategy{" +
                "mutex=" + mutex +
                ", signalNeeded=" + signalNeeded +
                ", timeoutInNanos=" + _timeoutInMilliseconds +
                '}';
        }

    }
}
