using Disruptor.Core;
using System;
using System.Linq;

namespace Disruptor
{
    /// <summary>
    /// Base class for the various sequencer types (single/multi).
    /// Provides common functionality like the management of gating sequences (add/remove) and ownership of the current cursor.
    /// 
    /// Sequencer 是 Disruptor 的真正核心。
    /// 此接口有两个实现类 SingleProducerSequencer、MultiProducerSequencer ，它们定义在生产者和消费者之间快速、正确地传递数据的并发算法。
    /// </summary>
    public abstract class AbstractSequencer : ISequencer
    {
        //protected readonly long INITIAL_CURSOR_VALUE = -1L;

        /// <summary>
        /// 用来对gatingSequences这个field做原子操作的
        /// </summary>
        protected _Volatile.AtomicReference<ISequence[]> sequencesRef;
        /// <summary>
        /// 用来表示环形数组的大小
        /// </summary>
        protected readonly int bufferSize;
        /// <summary>
        /// 用来标识消费者追上生产者时所使用的等待策略
        /// </summary>
        protected readonly IWaitStrategy waitStrategy;
        /// <summary>
        /// 用来标识生产者的当前序列
        /// </summary>
        protected readonly ISequence cursor = new Sequence(Consts.INITIAL_CURSOR_VALUE);
        /// <summary>
        /// volatile in the Java version => always use Volatile.Read/Write or Interlocked methods to access this field
        /// </summary>
        private ISequence[] gatingSequences = new Sequence[0];

        /// <summary>
        /// Create with the specified buffer size and wait strategy.
        /// </summary>
        /// <param name="bufferSize">The total number of entries, must be a positive power of 2.</param>
        /// <param name="waitStrategy">The wait strategy used by this sequencer.</param>
        public AbstractSequencer(int bufferSize, IWaitStrategy waitStrategy)
        {
            if (bufferSize < 1)
            {
                throw new IllegalArgumentException("bufferSize must not be less than 1");
            }

            // if (!bufferSize.IsPowerOf2_330())
            if (IntExtension.BitCount(bufferSize) != 1)
            {
                throw new IllegalArgumentException("bufferSize must be a power of 2");
            }

            this.bufferSize = bufferSize;
            this.waitStrategy = waitStrategy;
            sequencesRef = new _Volatile.AtomicReference<ISequence[]>(gatingSequences);
        }

        #region ISequencer

        public abstract void Claim(long sequence);

        public abstract bool IsAvailable(long sequence);

        public virtual void AddGatingSequences(params ISequence[] gatingSequences)
        {
            SequenceGroups.AddSequences(this, sequencesRef, this, gatingSequences);
        }

        public virtual Boolean RemoveGatingSequence(ISequence sequence)
        {
            return SequenceGroups.RemoveSequence(this, sequencesRef, sequence);
        }

        /// <summary>
        /// @see Sequencer#newBarrier(Sequence...)
        /// 返回一个SequenceBarrier的实现类，目前只有ProcessingSequenceBarrier
        /// </summary>
        /// <param name="sequencesToTrack"></param>
        /// <returns></returns>
        public virtual ISequenceBarrier NewBarrier(params ISequence[] sequencesToTrack)
        {
            return new ProcessingSequenceBarrier(this, waitStrategy, cursor, sequencesToTrack);
        }

        public virtual long GetMinimumSequence()
        {
            return Util.GetMinimumSequence(sequencesRef.ReadFullFence(), cursor.Get());
        }

        public abstract long GetHighestPublishedSequence(long nextSequence, long availableSequence);

        /// <summary>
        /// Creates an event poller for this sequence that will use the supplied data provider and gating sequences.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataProvider">The data source for users of this event poller</param>
        /// <param name="gatingSequences">Sequence to be gated on.</param>
        /// <returns>A poller that will gate on this ring buffer and the supplied sequences.</returns>
        public virtual EventPoller<T> NewPoller<T>(IDataProvider<T> dataProvider, params ISequence[] gatingSequences)
        {
            return EventPoller<T>.NewInstance(dataProvider, this, new Sequence(), cursor, gatingSequences);
        }

        #endregion

        /// <summary>
        /// <see cref="ICursored.GetCursor"/>
        /// Sequencer#getCursor()
        /// </summary>
        /// <returns></returns>
        public virtual long GetCursor()
        {
            return cursor.Get();
        }

        #region ISequencer

        /// <summary>
        /// <see cref="ISequenced.GetBufferSize"/>
        /// Sequencer#getBufferSize()
        /// </summary>
        /// <returns></returns>
        public virtual int GetBufferSize()
        {
            return bufferSize;
        }

        public abstract bool HasAvailableCapacity(int requiredCapacity);

        public abstract long RemainingCapacity();

        public abstract long Next();

        public abstract long Next(int n);

        public abstract long TryNext();

        public abstract long TryNext(int n);

        public abstract void Publish(long sequence);

        public abstract void Publish(long lo, long hi);

        #endregion

        public override String ToString()
        {
            return "AbstractSequencer{" +
                "waitStrategy=" + waitStrategy +
                ", cursor=" + cursor +
                ", gatingSequences=" + string.Join(",", gatingSequences.Select(t => t.ToString())) +
                '}';
        }

    }
}
