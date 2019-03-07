using Disruptor.Core;
using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Blocking strategy that uses a lock and condition variable for <see cref="IEventProcessor"/>s waiting on a barrier.
    /// 
    /// This strategy can be used when throughput and low-latency are not as important as CPU resource.
    /// 默认的等待序列
    /// BlockingWaitStrategy 的实现方法是加锁，阻塞等待策略。
    /// CPU资源紧缺，吞吐量和延迟并不重要的场景。
    /// </summary>
    public sealed class BlockingWaitStrategy : IWaitStrategy
    {
        private static readonly Object mutex = new Object();
        //不要把spinlock定义为readonly 因为在操作锁的时候，他的内部状态必须改变
        //private SpinLock spinLock = new SpinLock(false);
        //private readonly ManualResetEventSlim mres = new ManualResetEventSlim(false);

        /// <summary>
        /// <see cref="IWaitStrategy.WaitFor"/>
        /// </summary>
        public long WaitFor(long sequence, ISequence cursorSequence, ISequence dependentSequence, ISequenceBarrier barrier)
        {
            long availableSequence;
            //如果RingBuffer上当前可用的序列值小于要申请的序列值。
            if (cursorSequence.Get() < sequence)
            {
                //SpinLock 与 Monitor相比是个轻量级的同步原语
                //实际测试下来使用 SpinLock 性能优于Monitor
                //Monitor.Enter(mutex);
                bool lockToken = false;
                Monitor.Enter(mutex, ref lockToken);
                try
                {
                    //再次检测
                    while (cursorSequence.Get() < sequence)
                    {
                        //检查序列栅栏状态(事件处理器是否被关闭)
                        barrier.CheckAlert();
                        //当前线程在processorNotifyCondition条件上等待。
                        //mres.Wait();
                        Monitor.Wait(mutex);
                    }
                }
                finally
                {
                    //if (lockToken)
                    //    spinLock.Exit();
                    if (lockToken)
                        Monitor.Exit(mutex);
                }

            }
            //再次检测，避免事件处理器关闭的情况。
            var spinWait = new AggressiveSpinWait();
            while ((availableSequence = dependentSequence.Get()) < sequence)
            {
                barrier.CheckAlert();
                //ThreadHints.onSpinWait();
                spinWait.SpinOnce();
            }

            return availableSequence;
        }

        /// <summary>
        /// <see cref="IWaitStrategy.SignalAllWhenBlocking"/>
        /// </summary>
        public void SignalAllWhenBlocking()
        {
            //唤醒在processorNotifyCondition条件上等待的处理事件线程。
            //TODO:3.3.0使用的
            //lock (mutex)
            //{
            //    mres.Set();
            //}
            //TODO:2.0使用的
            lock (mutex)
            {
                Monitor.PulseAll(mutex);
            }
        }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override String ToString()
        {
            return "BlockingWaitStrategy{" +
                "mutex=" + mutex +
                '}';
        }

    }
}
