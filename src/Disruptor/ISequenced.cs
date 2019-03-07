using System;

namespace Disruptor
{
    /// <summary>
    /// 用来给生产者使用，用于申请序列，发布序列的。
    /// </summary>
    public interface ISequenced
    {
        /// <summary>
        /// The capacity of the data structure to hold entries.
        /// 获取环形队列的大小
        /// </summary>
        /// <returns>the size of the RingBuffer.</returns>
        int GetBufferSize();

        /// <summary>
        /// Has the buffer got capacity to allocate another sequence.  This is a concurrent
        /// method so the response should only be taken as an indication of available capacity.
        /// 
        /// 判断队列中是否还有可用的容量       
        /// </summary>
        /// <param name="requiredCapacity">in the buffer</param>
        /// <returns>true if the buffer has the capacity to allocate the next sequence otherwise false.</returns>
        Boolean HasAvailableCapacity(int requiredCapacity);

        /// <summary>
        /// Get the remaining capacity for this sequencer.
        /// 
        /// 获取队列中剩余的有效容量
        /// </summary>
        /// <returns>The number of slots remaining.</returns>
        long RemainingCapacity();

        /// <summary>
        /// Claim the next event in sequence for publishing.
        /// 
        /// 申请下一个序列,用于生产者发布数据
        /// </summary>
        /// <returns>the claimed sequence value</returns>
        long Next();

        /// <summary>
        /// Claim the next n events in sequence for publishing.  This is for batch event producing.  Using batch producing
        /// requires a little care and some math.
        /// 
        /// 申请n个序列,用于批量发布数据
        /// <code>
        /// int n = 10;
        /// long hi = sequencer.next(n);
        /// long lo = hi - (n - 1);
        /// for (long sequence = lo; sequence &lt;= hi; sequence++) {
        ///     // Do work.
        /// }
        /// sequencer.publish(lo, hi);
        /// </code>
        /// </summary>
        /// <param name="n">the number of sequences to claim</param>
        /// <returns>the highest claimed sequence value</returns>
        long Next(int n);

        /// <summary>
        /// Attempt to claim the next event in sequence for publishing.
        /// 
        /// Will return the number of the slot if there is at least <code>requiredCapacity</code> slots available. 
        /// 
        /// 非阻塞版本
        /// </summary>
        /// <returns>the claimed sequence value</returns>
        /// <exception cref="InsufficientCapacityException">thrown if there is no space available in the ring buffer.</exception>
        long TryNext();

        /// <summary>
        /// Attempt to claim the next n events in sequence for publishing.
        /// 
        /// Will return the highest numbered slot if there is at least <code>requiredCapacity</code> slots available.  
        /// Have a look at {@link Sequencer#next()} for a description on how to use this method.
        /// 
        /// 非阻塞版本
        /// </summary>
        /// <param name="n">the number of sequences to claim</param>
        /// <returns> the claimed sequence value</returns>
        /// <exception cref="InsufficientCapacityException">thrown if there is no space available in the ring buffer.</exception>
        long TryNext(int n);

        /// <summary>
        /// Publishes a sequence. Call when the event has been filled.
        /// 
        /// 生产者将数据组装好之后,发布序列
        /// </summary>
        /// <param name="sequence">the sequence to be published.</param>
        void Publish(long sequence);

        /// <summary>
        /// Batch publish sequences. Called when all of the events have been filled.
        /// 
        /// 披露发布序列
        /// </summary>
        /// <param name="lo">first sequence number to publish</param>
        /// <param name="hi">last sequence number to publish</param>
        void Publish(long lo, long hi);

    }
}
