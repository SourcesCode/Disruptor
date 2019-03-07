using Disruptor.Core;

namespace Disruptor
{
    /// <summary>
    /// 抽象类是用来做缓存行填充的
    /// </summary>
    public abstract class SingleProducerSequencerPad : AbstractSequencer
    {
        protected long P1, P2, P3, P4, P5, P6, P7;

        /// <summary>
        /// SingleProducerSequencerPad
        /// </summary>
        /// <param name="bufferSize"></param>
        /// <param name="waitStrategy"></param>
        protected SingleProducerSequencerPad(int bufferSize, IWaitStrategy waitStrategy)
            : base(bufferSize, waitStrategy)
        {
        }
    }

    /// <summary>
    /// 抽象类是真正定义有效字段的地方，主要是两个字段：
    /// nextValue——生产者申请到的下一个位置序列
    /// cachedValue——消费者上次消费到的位置序列
    /// </summary>
    public abstract class SingleProducerSequencerFields : SingleProducerSequencerPad
    {
        /// <summary>
        /// SingleProducerSequencerFields
        /// </summary>
        /// <param name="bufferSize"></param>
        /// <param name="waitStrategy"></param>
        protected SingleProducerSequencerFields(int bufferSize, IWaitStrategy waitStrategy)
            : base(bufferSize, waitStrategy)
        {
        }

        /// <summary>
        /// Set to -1 as sequence starting point.
        /// 生产者申请到的下一个位置序列
        /// </summary>
        protected long nextValue = Consts.INITIAL_CURSOR_VALUE;//Sequence.INITIAL_VALUE;
        /// <summary>
        /// Set to -1 as sequence starting point.
        /// 消费者上次消费到的位置序列
        /// </summary>
        protected long cachedValue = Consts.INITIAL_CURSOR_VALUE;//Sequence.INITIAL_VALUE;
    }

}
