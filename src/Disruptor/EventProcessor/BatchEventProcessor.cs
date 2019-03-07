using Disruptor.Core;
using System;

namespace Disruptor
{
    /// <summary>
    /// Convenience class for handling the batching semantics of consuming entries from a <see cref="RingBuffer{T}"/>
    /// and delegating the available events to an <see cref="IEventHandler{T}"/>.
    /// 
    /// If the <see cref="IEventHandler{T}"/> also implements <see cref="ILifecycleAware"/> it will be notified just after the thread
    /// is started and just before the thread is shutdown.
    /// 
    /// BatchEventProcessor用来批量单线程处理消息
    /// 1、获取可处理的有效序列是通过SequenceBarrier的waitFor方法，其实最终调用的是waitStrategy的waitFor方法，当是TimeoutBlockingWaitStrategy等待策略时，有可能抛出超时异常，此时eventHandler如果实现了TimeoutHandler，则会进行超时处理；
    /// 2、注意里面的notifyStart和notifyShutdown两个方法的调用，如果eventHandler实现了LifecycleAware接口，则在EventProcessor在处理第一条消息之前，会调用onStart方法，在处理完最后一条消息，会调用onShutdown方法，利用这个接口可以做一些事情，比如初始化、资源回收，或者通知等
    /// 3、数据的真正处理是在EventHandler接口的onEvnet方法中
    /// </summary>
    /// <typeparam name="T">event implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public sealed class BatchEventProcessor<T> : IEventProcessor where T : class
    {
        private static readonly int IDLE = 0;
        private static readonly int HALTED = IDLE + 1;
        private static readonly int RUNNING = HALTED + 1;

        private _Volatile.Integer running = new _Volatile.Integer(IDLE);
        private IExceptionHandler<T> exceptionHandler = new FatalExceptionHandler();
        /// <summary>
        /// 就是RingBuffer
        /// </summary>
        private readonly IDataProvider<T> dataProvider;
        /// <summary>
        /// 就是ProcessorSequenceBarrier
        /// </summary>
        private readonly ISequenceBarrier sequenceBarrier;
        /// <summary>
        /// 真正处理数据的地方
        /// </summary>
        private readonly IEventHandler<T> eventHandler;
        /// <summary>
        /// 自己的Sequence对象，用来存放当前处理到的序列
        /// </summary>
        private readonly ISequence sequence = new Sequence(Consts.INITIAL_CURSOR_VALUE);
        private readonly ITimeoutHandler timeoutHandler;
        private readonly IBatchStartAware _batchStartAware;
        private readonly ILifecycleAware _lifecycleAware;

        /// <summary>
        /// Construct a BatchEventProcessor that will automatically track the progress by updating its sequence when
        /// the <see cref="IEventHandler{T}.OnEvent(T, long, bool)"/> method returns.
        /// </summary>
        /// <param name="dataProvider">to which events are published.</param>
        /// <param name="sequenceBarrier">on which it is waiting.</param>
        /// <param name="eventHandler">is the delegate to which events are dispatched.</param>
        public BatchEventProcessor(
              IDataProvider<T> dataProvider,
              ISequenceBarrier sequenceBarrier,
              IEventHandler<T> eventHandler)
        {
            this.dataProvider = dataProvider;
            this.sequenceBarrier = sequenceBarrier;
            this.eventHandler = eventHandler;

            if (eventHandler is ISequenceReportingEventHandler<T>)
            {
                ((ISequenceReportingEventHandler<T>)eventHandler).SetSequenceCallback(sequence);
            }

            timeoutHandler = (eventHandler is ITimeoutHandler) ? (ITimeoutHandler)eventHandler : null;

            if (eventHandler is IBatchStartAware batchStartAware)
            {
                _batchStartAware = batchStartAware;
            }
            if (eventHandler is ILifecycleAware lifecycleAware)
            {
                _lifecycleAware = lifecycleAware;
            }

        }

        /// <summary>
        /// Set a new <see cref="IExceptionHandler{T}"/> for handling exceptions propagated out of the <see cref="BatchEventProcessor{T}"/>.
        /// </summary>
        /// <param name="exceptionHandler">to replace the existing exceptionHandler.</param>
        /// <exception cref="ArgumentNullException">exceptionHandler is null.</exception>
        public void SetExceptionHandler(IExceptionHandler<T> exceptionHandler)
        {
            if (null == exceptionHandler)
            {
                throw new ArgumentNullException("exceptionHandler is null");
            }

            this.exceptionHandler = exceptionHandler;
        }

        /// <summary>
        /// <see cref="IEventProcessor.GetSequence"/>
        /// </summary>
        public ISequence GetSequence()
        {
            return sequence;
        }

        public void Halt()
        {
            //running.set(HALTED);
            //TODO:2.0,是不是可以考虑WriteCompilerOnlyFence方法直接获取
            running.WriteFullFence(HALTED);
            sequenceBarrier.Alert();
        }

        /// <summary>
        /// It is ok to have another thread rerun this method after a halt().
        /// </summary>
        /// <exception cref="IllegalStateException">if this object instance is already running in a thread.</exception>
        public void Run()
        {
            if (running.AtomicCompareExchange(RUNNING, IDLE))
            {
                //清除sequenceBarrier的报警
                sequenceBarrier.ClearAlert();
                //注意这里的notifyStart
                NotifyStart();
                try
                {
                    //TODO:2.0,是不是可以考虑ReadCompilerOnlyFence方法直接获取
                    if (running.ReadFullFence() == RUNNING)
                    {
                        ProcessEvents();
                    }
                }
                finally
                {
                    //注意这里的notifyShutdown
                    NotifyShutdown();
                    //running.set(IDLE);
                    //TODO:2.0,是不是可以考虑WriteCompilerOnlyFence方法直接获取
                    running.WriteFullFence(IDLE);
                }
            }
            else
            {
                // This is a little bit of guess work.  The running state could of changed to HALTED by
                // this point.  However, Java does not have compareAndExchange which is the only way
                // to get it exactly correct.
                //TODO:2.0,是不是可以考虑ReadCompilerOnlyFence方法直接获取
                if (running.ReadFullFence() == RUNNING)
                {
                    throw new IllegalStateException("Thread is already running");
                }
                else
                {
                    EarlyExit();
                }
            }
        }

        public Boolean IsRunning()
        {
            //return running.get() != IDLE;
            //TODO:2.0,是不是可以考虑ReadCompilerOnlyFence方法直接获取
            return running.ReadFullFence() != IDLE;
        }

        #region Private Methods

        private void ProcessEvents()
        {
            T @event = default(T);
            long nextSequence = sequence.Get() + 1L;

            while (true)
            {
                try
                {
                    //通过SequenceBarrier的waitFor方法申请下一个序列，该方法会返回最大的有效序列，有可能会抛出超时异常
                    //只有在使用TimeoutBlockingWaitStrategy这个等待策略时才会抛出超时异常
                    long availableSequence = sequenceBarrier.WaitFor(nextSequence);
                    if (_batchStartAware != null)
                    {
                        _batchStartAware.OnBatchStart(availableSequence - nextSequence + 1);
                    }

                    while (nextSequence <= availableSequence)
                    {
                        @event = dataProvider.Get(nextSequence);
                        //真正数据处理的地方
                        eventHandler.OnEvent(@event, nextSequence, nextSequence == availableSequence);
                        nextSequence++;
                    }
                    //最后将sequence设置为最新处理到的序列
                    sequence.Set(availableSequence);
                }
                catch (TimeoutException)
                {
                    //等待策略是TimeoutBlockingWaitStrategy时，会抛出超时异常
                    NotifyTimeout(sequence.Get());
                }
                catch (AlertException)
                {
                    //TODO:2.0,是不是可以考虑ReadCompilerOnlyFence方法直接获取
                    if (running.ReadFullFence() != RUNNING)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    exceptionHandler?.HandleEventException(ex, nextSequence, @event);
                    sequence.Set(nextSequence);
                    nextSequence++;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void EarlyExit()
        {
            NotifyStart();
            NotifyShutdown();
        }

        /// <summary>
        /// 
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
                exceptionHandler?.HandleEventException(ex, availableSequence, null);
            }
        }

        /// <summary>
        /// Notifies the EventHandler when this processor is starting up
        /// </summary>
        private void NotifyStart()
        {
            try
            {
                _lifecycleAware?.OnStart();
            }
            catch (Exception ex)
            {
                exceptionHandler?.HandleOnStartException(ex);
            }
        }

        /// <summary>
        ///  Notifies the EventHandler immediately prior to this processor shutting down
        /// </summary>
        private void NotifyShutdown()
        {
            try
            {
                _lifecycleAware?.OnShutdown();
            }
            catch (Exception ex)
            {
                exceptionHandler?.HandleOnShutdownException(ex);
            }
        }

        #endregion

    }
}
