using System.Threading;

namespace Disruptor.Core
{
    /// <summary>
    /// LockSupport
    /// </summary>
    public class LockSupport
    {
        /// <summary>
        /// 阻塞多少纳秒(ns)
        /// </summary>
        /// <param name="nanoSeconds"></param>
        public static void ParkNanos(int nanoSeconds)//AggressiveSpinWait.SpinOnce
        {
            //Thread.Sleep(nanoSeconds);

        }

    }
}
