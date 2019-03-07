using System;

namespace Disruptor
{
    /// <summary>
    /// Coordinates claiming sequences for access to a data structure while tracking dependent <see cref="Sequence"/>s.
    /// </summary>
    public interface ISequencer : ICursored, ISequenced
    {
        ///// <summary>
        ///// Set to -1 as sequence starting point.
        ///// </summary>
        //long INITIAL_CURSOR_VALUE = -1L;

        /// <summary>
        /// Claim a specific sequence.  Only used if initialising the ring buffer to a specific value.
        /// 
        /// 一般用来初始化RingBuffer为指定的序列
        /// </summary>
        /// <param name="sequence">The sequence to initialise too.</param>
        void Claim(long sequence);

        /// <summary>
        /// Confirms if a sequence is published and the event is available for use; non-blocking.
        /// 
        /// 消费者调用,判断序列是否可以消费
        /// </summary>
        /// <param name="sequence">of the buffer to check.</param>
        /// <returns>true if the sequence is available for use, false if not.</returns>
        Boolean IsAvailable(long sequence);

        /// <summary>
        /// Add the specified gating sequences to this instance of the Disruptor.
        /// They will safely and atomically added to the list of gating sequences.
        /// 
        /// 将给定序列添加到追踪序列组中，生产者在申请序列时，会通过该序列组判断是否追尾
        /// </summary>
        /// <param name="gatingSequences">The sequences to add.</param>
        void AddGatingSequences(params ISequence[] gatingSequences);

        /// <summary>
        /// Remove the specified sequence from this sequencer.
        /// 
        /// 从追踪序列组中移除指定的序列
        /// </summary>
        /// <param name="sequence">to be removed.</param>
        /// <returns>true if this sequence was found, false otherwise.</returns>
        Boolean RemoveGatingSequence(ISequence sequence);

        /// <summary>
        /// Create a new SequenceBarrier to be used by an EventProcessor to track which messages
        /// are available to be read from the ring buffer given a list of sequences to track.
        /// 
        /// 消费者使用,用来追踪RingBuffer中可以用的序列
        /// </summary>
        /// <param name="sequencesToTrack">All of the sequences that the newly constructed barrier will wait on.</param>
        /// <returns>A sequence barrier that will track the specified sequences.</returns>
        ISequenceBarrier NewBarrier(params ISequence[] sequencesToTrack);

        /// <summary>
        /// Get the minimum sequence value from all of the gating sequences added to this ringBuffer.
        /// 
        /// 生产者获取追踪序列中最小的序列
        /// </summary>
        /// <returns>The minimum gating sequence or the cursor sequence if no sequences have been added.</returns>
        long GetMinimumSequence();

        /// <summary>
        /// Get the highest sequence number that can be safely read from the ring buffer.  Depending
        /// on the implementation of the Sequencer this call may need to scan a number of values
        /// in the Sequencer.  The scan will range from nextSequence to availableSequence.  If
        /// there are no available values <code>&gt;= nextSequence</code> the return value will be
        /// <code>nextSequence - 1</code>.  To work correctly a consumer should pass a value that
        /// is 1 higher than the last sequence that was successfully processed.
        /// 
        /// 消费者使用，用来获取从nextSequence到availableSequence之间最大的有效序列，如果没有，则返回nextSequence-1
        /// </summary>
        /// <param name="nextSequence">The sequence to start scanning from.</param>
        /// <param name="availableSequence">The sequence to scan to.</param>
        /// <returns>The highest value that can be safely read, will be at least <code>nextSequence - 1</code>.</returns>
        long GetHighestPublishedSequence(long nextSequence, long availableSequence);

        /// <summary>
        /// Creates an event poller for this sequence that will use the supplied data provider and gating sequences.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="provider"></param>
        /// <param name="gatingSequences"></param>
        /// <returns></returns>
        EventPoller<T> NewPoller<T>(IDataProvider<T> provider, params ISequence[] gatingSequences);

    }
}
