using Disruptor.Core;
using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// WorkerPool contains a pool of <see cref="WorkProcessor{T}"/>s that will consume sequences
    /// so jobs can be farmed out across a pool of workers.
    /// Each of the <see cref="WorkProcessor{T}"/>s manage and calls a <see cref="IWorkHandler{T}"/> to process the events.
    /// 
    /// WorkerPool 是对WorkProcessor多线程并发处理的封装，类定义如下
    /// </summary>
    /// <typeparam name="T">event to be processed by a pool of workers.</typeparam>
    public sealed class WorkerPool<T> where T : class
    {
        /// <summary>
        /// 
        /// </summary>
        private _Volatile.Boolean started = new _Volatile.Boolean(false);
        /// <summary>
        /// 就是workProcessor中的workSequence
        /// </summary>
        private readonly ISequence workSequence = new Sequence(Consts.INITIAL_CURSOR_VALUE);
        /// <summary>
        /// 环形数组
        /// </summary>
        private readonly RingBuffer<T> ringBuffer;
        /// <summary>
        /// WorkProcessors are created to wrap each of the provided WorkHandlers.
        /// 
        /// workProcessor的数组，并发处理数据，共用workSequence类构造的时候，
        /// 会通过传入的WorkHandler的个数来创建workProcessors数组
        /// </summary>
        private readonly WorkProcessor<T>[] workProcessors;

        /// <summary>
        /// Create a worker pool to enable an array of <see cref="IWorkHandler{T}"/>s to consume published sequences.
        /// 
        /// This option requires a pre-configured <see cref="RingBuffer{T}"/> which must have <see cref="RingBuffer{T}.AddGatingSequences(ISequence[])"/>
        /// called before the work pool is started.
        /// </summary>
        /// <param name="ringBuffer">of events to be consumed.</param>
        /// <param name="sequenceBarrier">on which the workers will depend.</param>
        /// <param name="exceptionHandler">to callback when an error occurs which is not handled by the <see cref="IWorkHandler{T}"/>s.</param>
        /// <param name="workHandlers">to distribute the work load across.</param>
        public WorkerPool(
            RingBuffer<T> ringBuffer,
            ISequenceBarrier sequenceBarrier,
            IExceptionHandler<T> exceptionHandler,
            params IWorkHandler<T>[] workHandlers)
        {
            this.ringBuffer = ringBuffer;
            int numWorkers = workHandlers.Length;
            workProcessors = new WorkProcessor<T>[numWorkers];

            for (int i = 0; i < numWorkers; i++)
            {
                workProcessors[i] = new WorkProcessor<T>(
                    ringBuffer,
                    sequenceBarrier,
                    workHandlers[i],
                    exceptionHandler,
                    workSequence);
            }
        }

        /// <summary>
        /// Construct a work pool with an internal <see cref="RingBuffer{T}"/> for convenience.
        /// 
        /// This option does not require <see cref="RingBuffer{T}.AddGatingSequences(ISequence[])"/>
        /// to be called before the work pool is started.
        /// </summary>
        /// <param name="eventFactory">for filling the <see cref="RingBuffer{T}"/></param>
        /// <param name="exceptionHandler">to callback when an error occurs which is not handled by the <see cref="IWorkHandler{T}"/>s.</param>
        /// <param name="workHandlers">to distribute the work load across.</param>
        public WorkerPool(
            IEventFactory<T> eventFactory,
            IExceptionHandler<T> exceptionHandler,
            params IWorkHandler<T>[] workHandlers)
        {
            ringBuffer = RingBuffer<T>.CreateMultiProducer(eventFactory, 1024, new BlockingWaitStrategy());
            ISequenceBarrier barrier = ringBuffer.NewBarrier();
            int numWorkers = workHandlers.Length;
            workProcessors = new WorkProcessor<T>[numWorkers];

            for (int i = 0; i < numWorkers; i++)
            {
                workProcessors[i] = new WorkProcessor<T>(
                    ringBuffer,
                    barrier,
                    workHandlers[i],
                    exceptionHandler,
                    workSequence);
            }

            ringBuffer.AddGatingSequences(GetWorkerSequences());
        }

        /// <summary>
        /// Get an array of <see cref="ISequence"/>s representing the progress of the workers.
        /// </summary>
        /// <returns>an array of <see cref="ISequence"/>s representing the progress of the workers.</returns>
        public ISequence[] GetWorkerSequences()
        {
            ISequence[] sequences = new ISequence[workProcessors.Length + 1];
            for (int i = 0, size = workProcessors.Length; i < size; i++)
            {
                sequences[i] = workProcessors[i].GetSequence();
            }
            sequences[sequences.Length - 1] = workSequence;

            return sequences;
        }

        /// <summary>
        /// Start the worker pool processing events in sequence.
        /// </summary>
        /// <param name="taskScheduler">providing threads for running the workers.</param>
        /// <returns>the <see cref="RingBuffer"/> used for the work queue.</returns>
        /// <exception cref="IllegalStateException">if the pool has already been started and not halted yet.</exception>
        public RingBuffer<T> Start(IExecutor executor)
        {
            if (!started.AtomicCompareExchange(true, false))
            {
                throw new IllegalStateException("WorkerPool has already been started and cannot be restarted until halted.");
            }

            long cursor = ringBuffer.GetCursor();
            workSequence.Set(cursor);

            foreach (WorkProcessor<T> processor in workProcessors)
            {
                processor.GetSequence().Set(cursor);
                executor.Execute(processor);
            }

            return ringBuffer;
        }

        /// <summary>
        /// Wait for the <see cref="RingBuffer{T}"/> to drain of published events then halt the workers.
        /// </summary>
        public void DrainAndHalt()
        {
            ISequence[] workerSequences = GetWorkerSequences();
            while (ringBuffer.GetCursor() > Util.GetMinimumSequence(workerSequences))
            {
                //TODO:2.0,使用
                //Thread.Yield();
                //TODO:3.3.0
                Thread.Sleep(0);
            }

            foreach (WorkProcessor<T> processor in workProcessors)
            {
                processor.Halt();
            }
            //TODO:2.0,使用WriteCompilerOnlyFence
            started.WriteFullFence(false);
        }

        /// <summary>
        /// Halt all workers immediately at the end of their current cycle.
        /// </summary>
        public void Halt()
        {
            foreach (WorkProcessor<T> processor in workProcessors)
            {
                processor.Halt();
            }
            //TODO:2.0,使用WriteCompilerOnlyFence
            started.WriteFullFence(false);
        }

        /// <summary>
        /// IsRunning
        /// </summary>
        /// <returns></returns>
        public Boolean IsRunning()
        {
            //TODO:2.0,使用ReadCompilerOnlyFence
            return started.ReadFullFence();
        }

    }
}
