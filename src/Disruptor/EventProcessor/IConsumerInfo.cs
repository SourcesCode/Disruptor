using Disruptor.Core;
using System;

namespace Disruptor.Dsl
{
    /// <summary>
    /// ConsumerInfo是用来封装每个消费者线程的信息的，
    /// 为了区分Event模式、Work模式，所以有两个实现，
    /// 分别是EventProcessorInfo、WorkerPoolInfo
    /// </summary>
    public interface IConsumerInfo
    {
        ISequence[] GetSequences();

        ISequenceBarrier GetBarrier();

        Boolean IsEndOfChain();

        void Start(IExecutor executor);

        void Halt();

        void MarkAsUsedInBarrier();

        Boolean IsRunning();

    }
}
