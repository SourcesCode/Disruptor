using Disruptor.Core;
using System;

namespace Disruptor.Dsl
{
    /// <summary>
    /// WorkerPoolInfo
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class WorkerPoolInfo<T> : IConsumerInfo where T : class
    {
        /// <summary>
        /// 前面已经介绍过WorkerPool类，就是一个消费组，并发消费数据
        /// </summary>
        private readonly WorkerPool<T> workerPool;
        /// <summary>
        /// 与EventProcessorInfo中的barrier功能一样，用来追踪序列
        /// </summary>
        private readonly ISequenceBarrier sequenceBarrier;
        /// <summary>
        /// endOfChain
        /// </summary>
        private Boolean endOfChain = true;

        /// <summary>
        /// WorkerPoolInfo
        /// </summary>
        /// <param name="workerPool"></param>
        /// <param name="sequenceBarrier"></param>
        internal WorkerPoolInfo(WorkerPool<T> workerPool, ISequenceBarrier sequenceBarrier)
        {
            this.workerPool = workerPool;
            this.sequenceBarrier = sequenceBarrier;
        }

        /// <summary>
        /// GetSequences
        /// </summary>
        /// <returns></returns>
        public ISequence[] GetSequences()
        {
            return workerPool.GetWorkerSequences();
        }

        /// <summary>
        /// GetBarrier
        /// </summary>
        /// <returns></returns>
        public ISequenceBarrier GetBarrier()
        {
            return sequenceBarrier;
        }

        /// <summary>
        /// IsEndOfChain
        /// </summary>
        /// <returns></returns>
        public Boolean IsEndOfChain()
        {
            return endOfChain;
        }

        /// <summary>
        /// Start
        /// </summary>
        /// <param name="executor"></param>
        public void Start(IExecutor executor)
        {
            workerPool.Start(executor);
        }

        /// <summary>
        /// Halt
        /// </summary>
        public void Halt()
        {
            workerPool.Halt();
        }

        /// <summary>
        /// MarkAsUsedInBarrier
        /// </summary>
        public void MarkAsUsedInBarrier()
        {
            endOfChain = false;
        }

        /// <summary>
        /// IsRunning
        /// </summary>
        /// <returns></returns>
        public Boolean IsRunning()
        {
            return workerPool.IsRunning();
        }

    }
}
