using System;
using System.Threading;

namespace Disruptor.WaitStrategy
{
    /// <summary>
    /// IBlockingStrategy
    /// </summary>
    public interface IBlockingStrategy
    {
        long WaitOnLock(long sequence,
            ISequence cursorSequence,
            ISequence dependentSequence, ISequenceBarrier barrier, TimeSpan timeOut);


        void SignalAllWhenBlocking();
    }

    /// <summary>
    /// LockBlockingStrategy
    /// </summary>
    public class LockBlockingStrategy : IBlockingStrategy
    {
        private volatile int _numWaiters = 0;
        private readonly object _gate = new object();

        public long WaitOnLock(long sequence,
            ISequence cursorSequence,
            ISequence dependentSequence, ISequenceBarrier barrier, TimeSpan timeOut)
        {
            var availableSequence = cursorSequence.Get(); // volatile read
            if (availableSequence < sequence)
            {
                Monitor.Enter(_gate);
                try
                {
                    ++_numWaiters;
                    while ((availableSequence = cursorSequence.Get()) < sequence) // volatile read
                    {
                        barrier.CheckAlert();
                        Monitor.Wait(_gate, timeOut);
                    }
                }
                finally
                {
                    --_numWaiters;
                    Monitor.Exit(_gate);
                }
            }
            while ((availableSequence = dependentSequence.Get()) < sequence)
            {
                barrier.CheckAlert();
            }
            return availableSequence;
        }

        public void SignalAllWhenBlocking()
        {
            if (0 != _numWaiters)
                lock (_gate)
                    Monitor.PulseAll(_gate);
        }
    }

    /// <summary>
    /// SleepBlockingStrategy
    /// </summary>
    public class SleepBlockingStrategy : IBlockingStrategy
    {
        public long WaitOnLock(long sequence,
                    ISequence cursorSequence,
                    ISequence dependentSequence, ISequenceBarrier barrier, TimeSpan timeOut)
        {
            long availableSequence;
            var spinWait = default(SpinWait);

            while ((availableSequence = dependentSequence.Get()) < sequence)
            {
                spinWait.SpinOnce();
            }

            return availableSequence;
        }

        public void SignalAllWhenBlocking()
        {

        }

    }

}
