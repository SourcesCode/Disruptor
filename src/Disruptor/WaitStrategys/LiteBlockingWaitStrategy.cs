using Disruptor.Core;
using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Variation of the <see cref="BlockingWaitStrategy"/> that attempts to elide conditional wake-ups when
    /// the lock is uncontended.  Shows performance improvements on microbenchmarks.  However this
    /// wait strategy should be considered experimental as I have not full proved the correctness of
    /// the lock elision code.
    /// 
    /// LiteBlockingWaitStrategy 的实现方法也是阻塞等待，但它会减少一些不必要的唤醒。
    /// 从源码的注释上看，这个策略在基准性能测试上是会表现出一些性能提升，
    /// 但是作者还不能完全证明程序的正确性。
    /// </summary>
    public sealed class LiteBlockingWaitStrategy : IWaitStrategy
    {
        private static readonly Object mutex = new Object();
        private _Volatile.Boolean signalNeeded = new _Volatile.Boolean(false);

        /// <summary>
        /// <see cref="IWaitStrategy.WaitFor"/>
        /// </summary>
        public long WaitFor(long sequence, ISequence cursorSequence, ISequence dependentSequence, ISequenceBarrier barrier)
        {
            long availableSequence;
            if (cursorSequence.Get() < sequence)
            {
                bool lockToken = false;
                Monitor.Enter(mutex, ref lockToken);
                try
                {
                    do
                    {
                        //signalNeeded.getAndSet(true);
                        //TODO:3.3.0使用的signalNeeded.WriteFullFence(true);
                        //TODO:2.0使用的
                        signalNeeded.AtomicExchange(true);
                        if (cursorSequence.Get() >= sequence)
                        {
                            break;
                        }

                        barrier.CheckAlert();
                        Monitor.Wait(mutex);
                    }
                    while (cursorSequence.Get() < sequence);
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
            //TODO:UnitTest
            //if (signalNeeded.getAndSet(false))
            //TODO:3.3.0使用的if (signalNeeded.AtomicCompareExchange(false, true))
            if (signalNeeded.AtomicExchange(false) == true)
            {
                lock (mutex)
                {
                    //TODO:3.3.0使用的Monitor.Pulse(mutex);
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
            return "LiteBlockingWaitStrategy{" +
                "mutex=" + mutex +
                ", signalNeeded=" + signalNeeded +
                '}';
        }

    }
}
