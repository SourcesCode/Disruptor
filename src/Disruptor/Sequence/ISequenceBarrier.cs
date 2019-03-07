using System;

namespace Disruptor
{
    /// <summary>
    /// Coordination barrier for tracking the cursor for publishers and sequence of
    /// dependent <see cref="IEventProcessor"/>s for processing a data structure.
    /// </summary>
    public interface ISequenceBarrier
    {
        /// <summary>
        /// Wait for the given sequence to be available for consumption.
        /// 等待指定的sequence可用
        /// </summary>
        /// <param name="sequence">sequence to wait for</param>
        /// <exception cref="AlertException">if a status change has occurred for the Disruptor.</exception>
        /// <exception cref="InterruptedException">if the thread needs awaking on a condition variable.</exception>
        /// <exception cref="TimeoutException">if a timeout occurs while waiting for the supplied sequence.</exception>
        /// <returns>the sequence up to which is available.</returns>
        long WaitFor(long sequence);

        /// <summary>
        /// Get the current cursor value that can be read.
        /// 获取生产者的游标位置
        /// </summary>
        /// <returns>value of the cursor for entries that have been published.</returns>
        long GetCursor();

        /// <summary>
        /// The current alert status for the barrier.
        /// 序列栅栏当前的状态
        /// </summary>
        /// <returns>true if in alert otherwise false.</returns>
        Boolean IsAlerted();

        /// <summary>
        /// Alert the <see cref="IEventProcessor"/>s of a status change and stay in this status until cleared.
        /// 报警
        /// </summary>
        void Alert();

        /// <summary>
        /// Clear the current alert status.
        /// 清除所有报警
        /// </summary>
        void ClearAlert();

        /// <summary>
        /// Check if an alert has been raised and throw an <see cref="AlertException"/> if it has.
        /// 检查是否有报警
        /// </summary>
        /// <exception cref="AlertException">if alert has been raised.</exception>
        void CheckAlert();

    }
}
