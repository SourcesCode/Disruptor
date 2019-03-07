using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Access to a ThreadFactory instance. All threads are created with setDaemon(true).
    /// </summary>
    public class DaemonThreadFactory
    {
        //public Thread newThread(final Runnable r)
        //{
        //    Thread t = new Thread(r);
        //    t.setDaemon(true);
        //    return t;
        //}

        /// <summary>
        /// NewThread
        /// </summary>
        /// <param name="action"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public Thread NewThread(Action action, bool start = false)
        {
            var th = new Thread(() => action()) { IsBackground = true };
            if (start)
            {
                th.Start();
                return th;
            }
            else
            {
                return th;
            }
        }

    }
}
