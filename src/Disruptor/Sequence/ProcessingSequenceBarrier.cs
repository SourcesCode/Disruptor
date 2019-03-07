using System;
using System.Runtime.CompilerServices;

namespace Disruptor
{
    /// <summary>
    /// <see cref="ISequenceBarrier"/> handed out for gating <see cref="IEventProcessor"/>s on a cursor sequence and optional dependent <see cref="IEventProcessor"/>(s), using the given WaitStrategy.
    /// 
    /// 生产者序列。
    /// 写入 Ring Buffer 的过程涉及到两阶段提交 (two-phase commit)。
    /// 首先，你的生产者需要申请 buffer 里的下一个节点。
    /// 然后，当生产者向节点写完数据，它将会调用 ProducerBarrier 的 commit 方法。
    /// 
    /// <see cref="ISequenceBarrier"/> handed out for gating <see cref="IEventProcessor"/>s on a cursor sequence
    /// and optional dependent <see cref="IEventProcessor"/>(s), using the given WaitStrategy.
    /// 定义如下,有等待策略、生产者的sequencer、以及生产者的游标，还有依赖的其他消费者的Sequence序列组
    /// </summary>
    public sealed class ProcessingSequenceBarrier : ISequenceBarrier
    {
        private readonly ISequencer sequencer;
        private readonly IWaitStrategy waitStrategy;
        private readonly ISequence cursorSequence;
        private readonly ISequence dependentSequence;

        private volatile Boolean alerted = false;

        public ProcessingSequenceBarrier(
            ISequencer sequencer,
            IWaitStrategy waitStrategy,
            ISequence cursorSequence,
            ISequence[] dependentSequences)
        {
            this.sequencer = sequencer;
            this.waitStrategy = waitStrategy;
            this.cursorSequence = cursorSequence;
            if (0 == dependentSequences?.Length)
            {
                dependentSequence = cursorSequence;
            }
            else
            {
                dependentSequence = new FixedSequenceGroup(dependentSequences);
            }
        }

        public long WaitFor(long sequence)
        {
            CheckAlert();

            long availableSequence = waitStrategy.WaitFor(sequence, cursorSequence, dependentSequence, this);

            if (availableSequence < sequence)
            {
                return availableSequence;
            }

            return sequencer.GetHighestPublishedSequence(sequence, availableSequence);
        }


        public long GetCursor()
        {
            return dependentSequence.Get();
        }


        public Boolean IsAlerted()
        {
            return alerted;
        }


        public void Alert()
        {
            alerted = true;
            waitStrategy.SignalAllWhenBlocking();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearAlert()
        {
            alerted = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CheckAlert()
        {
            if (alerted)
            {
                throw AlertException.INSTANCE;
            }
        }

    }
}
