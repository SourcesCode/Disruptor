using System;
using System.Threading;

namespace Disruptor.Core
{
    /// <summary>
    /// 生产者的线程工厂
    /// </summary>
    public class ThreadFactory
    {
        /// <summary>
        /// 生产者的线程工厂
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public Thread NewThread(Action command)
        {
            Thread thread = new Thread(() => command());
            return thread;
        }

        /// <summary>
        /// 生产者的线程工厂
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public Thread NewThread(IRunnable command)
        {
            Thread thread = new Thread(() => command.Run());
            return thread;
        }

    }
}
