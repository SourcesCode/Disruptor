using Disruptor.Core;
using System;

namespace Disruptor
{
    /// <summary>
    /// A <see cref="WorkProcessor{T}"/> wraps a single <see cref="IWorkHandler{T}"/>,
    /// effectively consuming the sequence and ensuring appropriate barriers.
    /// 
    /// Generally, this will be used as part of a <see cref="WorkerPool{T}"/>.
    /// 
    /// WorkProcessor用来多线程处理消息
    /// </summary>
    /// <typeparam name="T">implementation storing the details for the work to processed.</typeparam>
    public sealed class WorkProcessor<T> : IEventProcessor where T : class
    {
        private _Volatile.Boolean running = new _Volatile.Boolean(false);
        private readonly ISequence sequence = new Sequence(Consts.INITIAL_CURSOR_VALUE);
        private readonly RingBuffer<T> ringBuffer;
        private readonly ISequenceBarrier sequenceBarrier;
        /// <summary>
        /// 与BatchEventProcessor不同的地方在于多了一个字段workSequence，
        /// 这个字段是多个WorkProcessor实例共用的一个追踪RingBuffer的序列。
        /// </summary>
        private readonly IWorkHandler<T> workHandler;
        private readonly IExceptionHandler<T> exceptionHandler;
        private readonly ISequence workSequence;
        private readonly IEventReleaser eventReleaser;
        private readonly ITimeoutHandler timeoutHandler;
        private readonly ILifecycleAware _lifecycleAware;

        /// <summary>
        /// Construct a <see cref="WorkProcessor{T}"/>
        /// </summary>
        /// <param name="ringBuffer">to which events are published.</param>
        /// <param name="sequenceBarrier">on which it is waiting.</param>
        /// <param name="workHandler">is the delegate to which events are dispatched.</param>
        /// <param name="exceptionHandler">to be called back when an error occurs</param>
        /// <param name="workSequence">from which to claim the next event to be worked on.  It should always be initialised as <see cref="Consts.INITIAL_CURSOR_VALUE"/></param>
        public WorkProcessor(
            RingBuffer<T> ringBuffer,
            ISequenceBarrier sequenceBarrier,
            IWorkHandler<T> workHandler,
            IExceptionHandler<T> exceptionHandler,
            ISequence workSequence)
        {
            this.ringBuffer = ringBuffer;
            this.sequenceBarrier = sequenceBarrier;
            this.workHandler = workHandler;
            this.exceptionHandler = exceptionHandler;
            this.workSequence = workSequence;
            this.eventReleaser = new EventReleaser(this.sequence);

            if (this.workHandler is IEventReleaseAware)
            {
                ((IEventReleaseAware)this.workHandler).SetEventReleaser(eventReleaser);
            }

            timeoutHandler = (workHandler is ITimeoutHandler) ? (ITimeoutHandler)workHandler : null;
            _lifecycleAware = (workHandler is ILifecycleAware) ? (ILifecycleAware)workHandler : null;

        }

        /// <summary>
        /// GetSequence
        /// </summary>
        /// <returns></returns>
        public ISequence GetSequence()
        {
            return sequence;
        }

        /// <summary>
        /// Halt
        /// </summary>
        public void Halt()
        {
            //TODO:2.0,Interlocked.Exchange(ref _running, 0);
            running.WriteFullFence(false);
            sequenceBarrier.Alert();
        }

        /// <summary>
        /// It is ok to have another thread re-run this method after a halt().
        /// </summary>
        /// <exception cref="IllegalStateException">if this processor is already running</exception>
        public void Run()
        {
            if (!running.AtomicCompareExchange(true, false))
            {
                throw new IllegalStateException("Thread is already running");
            }
            sequenceBarrier.ClearAlert();

            NotifyStart();

            Boolean processedSequence = true;
            //cacheeAvailableSequence用来标识当前RingBuffer中可以操作的有效序列
            long cachedAvailableSequence = long.MinValue;
            long nextSequence = sequence.Get();
            T @event = default(T);
            while (true)
            {
                try
                {
                    // if previous sequence was processed - fetch the next sequence and set
                    // that we have successfully processed the previous sequence
                    // typically, this will be true
                    // this prevents the sequence getting too far forward if an exception
                    // is thrown from the WorkHandler
                    //processedSequence用来标识当前申请到的序列是否正常处理完成，如果正常处理完成，则开始申请下一个序列
                    //通过对workSequence进行循环申请的方法，知道申请到下一个序列为止，这里是由于有多个线程同时在通过
                    //workSequence进行下一个序列的申请，所以通过CAS的方式实现并发处理
                    if (processedSequence)
                    {
                        processedSequence = false;
                        do
                        {
                            nextSequence = workSequence.Get() + 1L;
                            sequence.Set(nextSequence - 1L);
                        }
                        while (!workSequence.CompareAndSet(nextSequence - 1L, nextSequence));
                    }
                    //当成功申请到下一个序列之后，此时cachedAvailableSequence大于等于申请到的序列，则进行数据处理
                    //如果小于申请到的序列nextSequence，则通过sequenceBarrier去获取ringBuffer上的有效序列
                    if (cachedAvailableSequence >= nextSequence)
                    {
                        @event = ringBuffer.Get(nextSequence);
                        workHandler.OnEvent(@event);
                        processedSequence = true;
                    }
                    else
                    {
                        cachedAvailableSequence = sequenceBarrier.WaitFor(nextSequence);
                    }
                }
                catch (TimeoutException)
                {
                    NotifyTimeout(sequence.Get());
                }
                catch (AlertException)
                {
                    //TODO:2.0,使用ReadCompilerOnlyFence
                    if (!running.ReadFullFence())
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // handle, mark as processed, unless the exception handler threw an exception
                    exceptionHandler.HandleEventException(ex, nextSequence, @event);
                    //TODO:2.0没有,需要确认
                    processedSequence = true;
                }
            }

            NotifyShutdown();
            //TODO:2.0,使用WriteCompilerOnlyFence
            running.WriteFullFence(false);
        }

        /// <summary>
        /// IsRunning
        /// </summary>
        /// <returns></returns>
        public Boolean IsRunning()
        {
            //TODO:2.0,使用ReadCompilerOnlyFence
            return running.ReadFullFence();
        }

        /// <summary>
        /// NotifyTimeout
        /// </summary>
        /// <param name="availableSequence"></param>
        private void NotifyTimeout(long availableSequence)
        {
            try
            {
                timeoutHandler?.OnTimeout(availableSequence);
            }
            catch (Exception ex)
            {
                exceptionHandler.HandleEventException(ex, availableSequence, null);
            }
        }

        /// <summary>
        /// NotifyStart
        /// </summary>
        private void NotifyStart()
        {
            try
            {
                _lifecycleAware?.OnStart();
            }
            catch (Exception ex)
            {
                exceptionHandler.HandleOnStartException(ex);
            }
        }

        /// <summary>
        /// NotifyShutdown
        /// </summary>
        private void NotifyShutdown()
        {
            try
            {
                _lifecycleAware?.OnShutdown();
            }
            catch (Exception ex)
            {
                exceptionHandler.HandleOnShutdownException(ex);
            }
        }

        /// <summary>
        /// EventReleaser
        /// </summary>
        private class EventReleaser : IEventReleaser
        {
            private readonly ISequence _sequence;
            /// <summary>
            /// EventReleaser
            /// </summary>
            /// <param name="sequence"></param>
            public EventReleaser(ISequence sequence)
            {
                _sequence = sequence;
            }

            /// <summary>
            /// Release
            /// </summary>
            public void Release()
            {
                //_sequence.LazySet(long.MaxValue);
                _sequence.Set(long.MaxValue);
            }

        }

    }
}
