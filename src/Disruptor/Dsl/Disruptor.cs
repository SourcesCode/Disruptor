using Disruptor.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Disruptor.Dsl
{
    /// <summary>
    /// A DSL-style API for setting up the disruptor pattern around a ring buffer (aka the Builder pattern).
    /// 
    /// A simple example of setting up the disruptor with two event handlers that must process events in order:
    /// <code>
    /// Disruptor&lt;MyEvent&gt; disruptor = new Disruptor&lt;MyEvent&gt;(MyEvent.FACTORY, 32, Executors.newCachedThreadPool());
    /// EventHandler&lt;MyEvent&gt; handler1 = new EventHandler&lt;MyEvent&gt;() { ... };
    /// EventHandler&lt;MyEvent&gt; handler2 = new EventHandler&lt;MyEvent&gt;() { ... };
    /// disruptor.handleEventsWith(handler1);
    /// disruptor.after(handler1).handleEventsWith(handler2);
    /// 
    /// RingBuffer ringBuffer = disruptor.start();
    /// </code>
    /// 
    /// </summary>
    /// <typeparam name="T">the type of event used.</typeparam>
    public class Disruptor<T> where T : class
    {
        private readonly RingBuffer<T> ringBuffer;
        private readonly IExecutor executor;
        private readonly ConsumerRepository<T> consumerRepository;
        private IExceptionHandler<T> exceptionHandler;
        private _Volatile.Boolean started = new _Volatile.Boolean(false);

        #region Constructors

        /// <summary>
        /// Create a new Disruptor.
        /// Will default to <see cref="BlockingWaitStrategy"/> and <see cref="ProducerType.MULTI"/>.
        /// 
        /// @deprecated Use a {@link ThreadFactory} instead of an {@link Executor} as a the ThreadFactory
        /// is able to report errors when it is unable to construct a thread to run a producer.
        /// 
        /// </summary>
        /// <param name="eventFactory">the factory to create events in the ring buffer.</param>
        /// <param name="ringBufferSize">the size of the ring buffer, must be power of 2.</param>
        /// <param name="executor">an <see cref="IExecutor"/> to execute event processors.</param>
        public Disruptor(IEventFactory<T> eventFactory, int ringBufferSize, IExecutor executor)
            : this(RingBuffer<T>.CreateMultiProducer(eventFactory, ringBufferSize), executor)
        {
        }

        /// <summary>
        /// Create a new Disruptor.
        /// 
        /// @deprecated Use a {@link ThreadFactory} instead of an {@link Executor} as a the ThreadFactory
        /// is able to report errors when it is unable to construct a thread to run a producer.
        /// 
        /// </summary>
        /// <param name="eventFactory">the factory to create events in the ring buffer.</param>
        /// <param name="ringBufferSize">the size of the ring buffer, must be power of 2.</param>
        /// <param name="executor">an <see cref="IExecutor"/> to execute event processors.</param>
        /// <param name="producerType">the claim strategy to use for the ring buffer.</param>
        /// <param name="waitStrategy">the wait strategy to use for the ring buffer.</param>
        public Disruptor(IEventFactory<T> eventFactory, int ringBufferSize, IExecutor executor,
            ProducerType producerType, IWaitStrategy waitStrategy)
            : this(RingBuffer<T>.Create(producerType, eventFactory, ringBufferSize, waitStrategy), executor)
        {
        }

        /// <summary>
        /// Create a new Disruptor.
        /// Will default to <see cref="BlockingWaitStrategy"/> and <see cref="ProducerType.MULTI"/>.
        /// 
        /// @deprecated Use a {@link ThreadFactory} instead of an {@link Executor} as a the ThreadFactory
        /// is able to report errors when it is unable to construct a thread to run a producer.
        /// 
        /// </summary>
        /// <param name="eventFactory">the factory to create events in the ring buffer.</param>
        /// <param name="ringBufferSize">the size of the ring buffer, must be power of 2.</param>
        /// <param name="threadFactory">a <see cref="ThreadFactory"/> to create threads for processors.</param>
        public Disruptor(IEventFactory<T> eventFactory, int ringBufferSize, ThreadFactory threadFactory)
            : this(RingBuffer<T>.CreateMultiProducer(eventFactory, ringBufferSize), new BasicExecutor(threadFactory))
        {
        }

        /// <summary>
        /// Create a new Disruptor.
        /// </summary>
        /// <param name="eventFactory">the factory to create events in the ring buffer.</param>
        /// <param name="ringBufferSize">the size of the ring buffer, must be power of 2.</param>
        /// <param name="threadFactory">a <see cref="ThreadFactory"/> to create threads for processors.</param>
        /// <param name="producerType">the claim strategy to use for the ring buffer.</param>
        /// <param name="waitStrategy">the wait strategy to use for the ring buffer.</param>
        public Disruptor(IEventFactory<T> eventFactory, int ringBufferSize, ThreadFactory threadFactory,
            ProducerType producerType, IWaitStrategy waitStrategy)
            : this(RingBuffer<T>.Create(producerType, eventFactory, ringBufferSize, waitStrategy), new BasicExecutor(threadFactory))
        {
        }


        /// <summary>
        /// Create a new Disruptor.
        /// Will default to <see cref="BlockingWaitStrategy"/> and <see cref="ProducerType.MULTI"/>.
        /// 
        /// @deprecated Use a {@link ThreadFactory} instead of an {@link Executor} as a the ThreadFactory
        /// is able to report errors when it is unable to construct a thread to run a producer.
        /// 
        /// </summary>
        /// <param name="eventFactory">the factory to create events in the ring buffer.</param>
        /// <param name="ringBufferSize">the size of the ring buffer, must be power of 2.</param>
        /// <param name="threadFactory">a <see cref="TaskScheduler"/> to create threads for processors.</param>
        public Disruptor(IEventFactory<T> eventFactory, int ringBufferSize, TaskScheduler taskScheduler)
            : this(RingBuffer<T>.CreateMultiProducer(eventFactory, ringBufferSize), new BasicExecutor(taskScheduler))
        {
        }

        /// <summary>
        /// Create a new Disruptor.
        /// </summary>
        /// <param name="eventFactory">the factory to create events in the ring buffer.</param>
        /// <param name="ringBufferSize">the size of the ring buffer, must be power of 2.</param>
        /// <param name="threadFactory">a <see cref="TaskScheduler"/> to create threads for processors.</param>
        /// <param name="producerType">the claim strategy to use for the ring buffer.</param>
        /// <param name="waitStrategy">the wait strategy to use for the ring buffer.</param>
        public Disruptor(IEventFactory<T> eventFactory, int ringBufferSize, TaskScheduler taskScheduler,
            ProducerType producerType, IWaitStrategy waitStrategy)
            : this(RingBuffer<T>.Create(producerType, eventFactory, ringBufferSize, waitStrategy), new BasicExecutor(taskScheduler))
        {
        }

        /// <summary>
        /// Private constructor helper
        /// </summary>
        /// <param name="ringBuffer"></param>
        /// <param name="executor"></param>
        private Disruptor(RingBuffer<T> ringBuffer, IExecutor executor)
        {
            this.ringBuffer = ringBuffer;
            this.executor = executor;
            consumerRepository = new ConsumerRepository<T>();
            exceptionHandler = new ExceptionHandlerWrapper<T>();
        }

        #endregion

        /// <summary>
        /// Set up event handlers to handle events from the ring buffer. These handlers will process events
        /// as soon as they become available, in parallel.
        /// 
        /// <p>This method can be used as the start of a chain. For example if the handler <code>A</code> must
        /// process events before handler <code>B</code>:</p>
        /// <pre><code>dw.HandleEventsWith(A).then(B);</code></pre>
        /// 
        /// This call is additive, but generally should only be called once when setting up the Disruptor instance.
        /// </summary>
        /// <param name="handlers">the event handlers that will process events.</param>
        /// <returns>a <see cref="EventHandlerGroup{T}"/> that can be used to chain dependencies.</returns>
        public EventHandlerGroup<T> HandleEventsWith(params IEventHandler<T>[] handlers)
        {
            return CreateEventProcessors(new Sequence[0], handlers);
        }

        /// <summary>
        /// Set up custom event processors to handle events from the ring buffer.
        /// The Disruptor will automatically start these processors when {@link #start()} is called.
        /// 
        /// <p>This method can be used as the start of a chain. For example if the handler <code>A</code> must
        /// process events before handler <code>B</code>:</p>
        /// <pre><code>dw.handleEventsWith(A).then(B);</code></pre>
        /// 
        /// <p>Since this is the start of the chain, the processor factories will always be passed an empty <code>Sequence</code>
        /// array, so the factory isn't necessary in this case. This method is provided for consistency with
        /// {@link EventHandlerGroup#handleEventsWith(EventProcessorFactory...)} and {@link EventHandlerGroup#then(EventProcessorFactory...)}
        /// which do have barrier sequences to provide.</p>
        /// 
        /// This call is additive, but generally should only be called once when setting up the Disruptor instance.
        /// </summary>
        /// <param name="eventProcessorFactories">the event processor factories to use to create the event processors that will process events.</param>
        /// <returns>a <see cref="EventHandlerGroup{T}"/> that can be used to chain dependencies.</returns>
        public EventHandlerGroup<T> HandleEventsWith(params IEventProcessorFactory<T>[] eventProcessorFactories)
        {
            Sequence[] barrierSequences = new Sequence[0];
            return CreateEventProcessors(barrierSequences, eventProcessorFactories);
        }

        /// <summary>
        /// Set up custom event processors to handle events from the ring buffer.
        /// The Disruptor will automatically start this processors when <see cref="Start()"/> is called.
        /// 
        /// <p>This method can be used as the start of a chain. For example if the processor <code>A</code> must
        /// process events before handler <code>B</code>:</p>
        /// <pre><code>dw.handleEventsWith(A).then(B);</code></pre>
        /// 
        /// </summary>
        /// <param name="processors">the event processors that will process events.</param>
        /// <returns>a <see cref="EventHandlerGroup{T}"/> that can be used to chain dependencies.</returns>
        public EventHandlerGroup<T> HandleEventsWith(params IEventProcessor[] processors)
        {
            foreach (IEventProcessor processor in processors)
            {
                consumerRepository.Add(processor);
            }

            //TODO:3.3.0没有
            ISequence[] sequences = new ISequence[processors.Length];
            for (int i = 0; i < processors.Length; i++)
            {
                sequences[i] = processors[i].GetSequence();
            }

            ringBuffer.AddGatingSequences(sequences);
            return new EventHandlerGroup<T>(this, consumerRepository, Util.GetSequencesFor(processors));
        }

        /// <summary>
        /// Set up a <see cref="WorkerPool{T}"/> to distribute an event to one of a pool of work handler threads.
        /// Each event will only be processed by one of the work handlers.
        /// The Disruptor will automatically start this processors when <see cref="Start()"/> is called.
        /// </summary>
        /// <param name="workHandlers">the work handlers that will process events.</param>
        /// <returns>a <see cref="EventHandlerGroup{T}"/> that can be used to chain dependencies.</returns>
        public EventHandlerGroup<T> HandleEventsWithWorkerPool(params IWorkHandler<T>[] workHandlers)
        {
            return CreateWorkerPool(new Sequence[0], workHandlers);
        }

        /// <summary>
        /// Specify an exception handler to be used for any future event handlers.
        /// Note that only event handlers set up after calling this method will use the exception handler.
        /// 
        /// @deprecated This method only applies to future event handlers. Use setDefaultExceptionHandler instead which applies to existing and new event handlers.
        /// </summary>
        /// <param name="exceptionHandler">the exception handler to use for any future <see cref="IEventProcessor"/>.</param>
        public void HandleExceptionsWith(IExceptionHandler<T> exceptionHandler)
        {
            this.exceptionHandler = exceptionHandler;
        }

        /// <summary>
        /// Specify an exception handler to be used for event handlers and worker pools created by this Disruptor.
        /// The exception handler will be used by existing and future event handlers and worker pools created by this Disruptor instance.
        /// </summary>
        /// <param name="exceptionHandler">the exception handler to use.</param>
        public void SetDefaultExceptionHandler(IExceptionHandler<T> exceptionHandler)
        {
            CheckNotStarted();
            if (!(this.exceptionHandler is ExceptionHandlerWrapper<T>))
            {
                throw new IllegalStateException("setDefaultExceptionHandler can not be used after handleExceptionsWith");
            }
            ((ExceptionHandlerWrapper<T>)this.exceptionHandler).SwitchTo(exceptionHandler);
        }

        /// <summary>
        /// Override the default exception handler for a specific handler.
        /// <code>disruptor.handleExceptionsIn(eventHandler).with(exceptionHandler);</code>
        /// </summary>
        /// <param name="eventHandler">the event handler to set a different exception handler for.</param>
        /// <returns>an ExceptionHandlerSetting dsl object - intended to be used by chaining the with method call.</returns>
        public ExceptionHandlerSetting<T> HandleExceptionsFor(IEventHandler<T> eventHandler)
        {
            return new ExceptionHandlerSetting<T>(eventHandler, consumerRepository);
        }

        /// <summary>
        /// Create a group of event handlers to be used as a dependency.
        /// 
        /// For example if the handler <code>A</code> must process events before handler <code>B</code>:
        /// <pre><code>dw.after(A).HandleEventsWith(B);</code></pre>
        /// </summary>
        /// <param name="handlers">the event handlers, previously set up with <see cref="HandleEventsWith(IEventHandler{T}[])"/>,
        /// that will form the barrier for subsequent handlers or processors.</param>
        /// <returns>an <see cref="EventHandlerGroup{T}"/> that can be used to setup a dependency barrier over the specified event handlers.</returns>
        public EventHandlerGroup<T> After(params IEventHandler<T>[] handlers)
        {
            ISequence[] sequences = new ISequence[handlers.Length];
            for (int i = 0, handlersLength = handlers.Length; i < handlersLength; i++)
            {
                sequences[i] = consumerRepository.GetSequenceFor(handlers[i]);
            }
            return new EventHandlerGroup<T>(this, consumerRepository, sequences);
        }

        /// <summary>
        /// Create a group of event processors to be used as a dependency.
        /// </summary>
        /// <param name="processors">the event processors, previously set up with <see cref="HandleEventsWith(IEventProcessor[]) "/>,
        /// that will form the barrier for subsequent handlers or processors.</param>
        /// <returns>an <see cref="EventHandlerGroup{T}"/> that can be used to setup a <see cref="ISequenceBarrier"/> over the specified event processors.</returns>
        public EventHandlerGroup<T> After(params IEventProcessor[] processors)
        {
            foreach (IEventProcessor processor in processors)
            {
                consumerRepository.Add(processor);
            }

            return new EventHandlerGroup<T>(this, consumerRepository, Util.GetSequencesFor(processors));
        }

        /// <summary>
        /// Publish an event to the ring buffer.
        /// </summary>
        /// <param name="eventTranslator">the translator that will load data into the event.</param>
        public void PublishEvent(IEventTranslator<T> eventTranslator)
        {
            ringBuffer.PublishEvent(eventTranslator);
        }

        /// <summary>
        /// Publish an event to the ring buffer.
        /// </summary>
        /// <typeparam name="A">Class of the user supplied argument.</typeparam>
        /// <param name="eventTranslator">the translator that will load data into the event.</param>
        /// <param name="arg">A single argument to load into the event.</param>
        public void PublishEvent<A>(IEventTranslatorOneArg<T, A> eventTranslator, A arg)
        {
            ringBuffer.PublishEvent(eventTranslator, arg);
        }

        /// <summary>
        /// Publish an event to the ring buffer.
        /// </summary>
        /// <typeparam name="A">Class of the user supplied argument.</typeparam>
        /// <typeparam name="B">Class of the user supplied argument.</typeparam>
        /// <param name="eventTranslator">the translator that will load data into the event.</param>
        /// <param name="arg0">The first argument to load into the event.</param>
        /// <param name="arg1">The second argument to load into the event.</param>
        public void PublishEvent<A, B>(IEventTranslatorTwoArg<T, A, B> eventTranslator, A arg0, B arg1)
        {
            ringBuffer.PublishEvent(eventTranslator, arg0, arg1);
        }

        /// <summary>
        /// Publish an event to the ring buffer.
        /// </summary>
        /// <typeparam name="A">Class of the user supplied argument.</typeparam>
        /// <typeparam name="B">Class of the user supplied argument.</typeparam>
        /// <typeparam name="C">Class of the user supplied argument.</typeparam>
        /// <param name="eventTranslator">the translator that will load data into the event.</param>
        /// <param name="arg0">The first argument to load into the event.</param>
        /// <param name="arg1">The second argument to load into the event.</param>
        /// <param name="arg2">The third argument to load into the event.</param>
        public void PublishEvent<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> eventTranslator, A arg0, B arg1, C arg2)
        {
            ringBuffer.PublishEvent(eventTranslator, arg0, arg1, arg2);
        }

        /// <summary>
        /// Publish a batch of events to the ring buffer.
        /// </summary>
        /// <typeparam name="A">Class of the user supplied argument.</typeparam>
        /// <param name="eventTranslator">the translator that will load data into the event.</param>
        /// <param name="args">An array single arguments to load into the events. One Per event.</param>
        public void PublishEvents<A>(IEventTranslatorOneArg<T, A> eventTranslator, A[] arg)
        {
            ringBuffer.PublishEvents(eventTranslator, arg);
        }

        /// <summary>
        /// Starts the event processors and returns the fully configured ring buffer.
        /// 
        /// The ring buffer is set up to prevent overwriting any entry that is yet to
        /// be processed by the slowest event processor.
        /// 
        /// This method must only be called once after all event processors have been added.
        /// </summary>
        /// <returns>the configured ring buffer.</returns>
        public RingBuffer<T> Start()
        {
            //var gatingSequences = consumerRepository.GetLastSequenceInChain(true);
            //ringBuffer.AddGatingSequences(gatingSequences);

            CheckOnlyStartedOnce();
            foreach (IConsumerInfo consumerInfo in consumerRepository)
            {
                consumerInfo.Start(executor);
            }

            return ringBuffer;
        }

        /// <summary>
        /// Calls <see cref="IEventProcessor.Halt()"/> on all of the event processors created via this disruptor.
        /// </summary>
        public void Halt()
        {
            foreach (IConsumerInfo consumerInfo in consumerRepository)
            {
                consumerInfo.Halt();
            }
        }

        /// <summary>
        /// Waits until all events currently in the disruptor have been processed by all event processors
        /// and then halts the processors. It is critical that publishing to the ring buffer has stopped
        /// before calling this method, otherwise it may never return.
        /// 
        /// This method will not shutdown the executor, nor will it await the final termination of the processor threads.
        /// </summary>
        public void Shutdown()
        {
            try
            {
                //shutdown(-1, TimeUnit.MILLISECONDS);
                Shutdown(Timeout.InfiniteTimeSpan);
                //shutdown(TimeSpan.FromSeconds(3), true);
            }
            catch (TimeoutException e)
            {
                exceptionHandler.HandleOnShutdownException(e);
            }
        }

        /// <summary>
        /// Waits until all events currently in the disruptor have been processed by all event processors and then halts the processors.
        /// </summary>
        /// <remarks>
        /// This method will not shutdown the executor, nor will it await the final termination of the processor threads.
        /// </remarks>
        /// <param name="timeout">the amount of time to wait for all events to be processed.
        /// <code>-1</code> will give an infinite timeout</param>
        /// <exception cref="TimeoutException">if a timeout occurs before shutdown completes.</exception>
        public void Shutdown(TimeSpan timeout)//(long timeout, TimeUnit timeUnit)
        {
            if (timeout == default(TimeSpan))
            {
                System.Threading.SpinWait.SpinUntil(() => !HasBacklog());
            }
            else
            {
                if (!System.Threading.SpinWait.SpinUntil(() => !HasBacklog(), timeout))
                {
                    throw TimeoutException.INSTANCE;
                }
            }
            Halt();
        }

        #region
        //public void shutdown(long timeout, TimeUnit timeUnit)
        //{
        //    long timeOutAt = System.currentTimeMillis() + timeUnit.toMillis(timeout);
        //    while (hasBacklog())
        //    {
        //        if (timeout >= 0 && System.currentTimeMillis() > timeOutAt)
        //        {
        //            throw TimeoutException.INSTANCE;
        //        }
        //        // Busy spin
        //    }
        //    halt();
        //}
        #endregion

        /// <summary>
        /// The <see cref="RingBuffer{T}"/> used by this Disruptor.
        /// This is useful for creating custom event processors if the behaviour of <see cref="BatchEventProcessor{T}"/> is not suitable.
        /// </summary>
        /// <returns>the ring buffer used by this Disruptor.</returns>
        public RingBuffer<T> GetRingBuffer()
        {
            return ringBuffer;
        }

        /// <summary>
        /// Get the value of the cursor indicating the published sequence.
        /// </summary>
        /// <returns>value of the cursor for events that have been published.</returns>
        public long GetCursor()
        {
            return ringBuffer.GetCursor();
        }

        /// <summary>
        /// The capacity of the data structure to hold entries.
        /// </summary>
        /// <returns>the size of the RingBuffer.</returns>
        public long GetBufferSize()
        {
            return ringBuffer.GetBufferSize();
        }

        /// <summary>
        /// Get the event for a given sequence in the RingBuffer.
        /// </summary>
        /// <param name="sequence">for the event.</param>
        /// <returns>event for the sequence.</returns>
        public T Get(long sequence)
        {
            return ringBuffer.Get(sequence);
        }

        /// <summary>
        /// Get the <see cref="ISequenceBarrier"/> used by a specific handler.
        /// Note that the <see cref="ISequenceBarrier"/> may be shared by multiple event handlers.
        /// </summary>
        /// <param name="handler">the handler to get the barrier for.</param>
        /// <returns>the SequenceBarrier used by handler.</returns>
        public ISequenceBarrier GetBarrierFor(IEventHandler<T> handler)
        {
            return consumerRepository.GetBarrierFor(handler);
        }

        /// <summary>
        /// Gets the sequence value for the specified event handlers.
        /// </summary>
        /// <param name="handler">eventHandler to get the sequence for.</param>
        /// <returns>eventHandler's sequence</returns>
        public long GetSequenceValueFor(IEventHandler<T> handler)
        {
            return consumerRepository.GetSequenceFor(handler).Get();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="barrierSequences"></param>
        /// <param name="processorFactories"></param>
        /// <returns></returns>
        public EventHandlerGroup<T> CreateEventProcessors(ISequence[] barrierSequences, IEventProcessorFactory<T>[] processorFactories)
        {
            IEventProcessor[] eventProcessors = new IEventProcessor[processorFactories.Length];
            for (int i = 0; i < processorFactories.Length; i++)
            {
                eventProcessors[i] = processorFactories[i].CreateEventProcessor(ringBuffer, barrierSequences);
            }

            return HandleEventsWith(eventProcessors);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="barrierSequences"></param>
        /// <param name="eventHandlers"></param>
        /// <returns></returns>
        public EventHandlerGroup<T> CreateEventProcessors(ISequence[] barrierSequences, IEventHandler<T>[] eventHandlers)
        {
            CheckNotStarted();

            ISequence[] processorSequences = new ISequence[eventHandlers.Length];
            ISequenceBarrier barrier = ringBuffer.NewBarrier(barrierSequences);

            for (int i = 0, eventHandlersLength = eventHandlers.Length; i < eventHandlersLength; i++)
            {
                IEventHandler<T> eventHandler = eventHandlers[i];

                BatchEventProcessor<T> batchEventProcessor = new BatchEventProcessor<T>(ringBuffer, barrier, eventHandler);

                if (exceptionHandler != null)
                {
                    batchEventProcessor.SetExceptionHandler(exceptionHandler);
                }

                consumerRepository.Add(batchEventProcessor, eventHandler, barrier);
                processorSequences[i] = batchEventProcessor.GetSequence();
            }

            UpdateGatingSequencesForNextInChain(barrierSequences, processorSequences);

            return new EventHandlerGroup<T>(this, consumerRepository, processorSequences);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="barrierSequences"></param>
        /// <param name="workHandlers"></param>
        /// <returns></returns>
        public EventHandlerGroup<T> CreateWorkerPool(ISequence[] barrierSequences, IWorkHandler<T>[] workHandlers)
        {
            ISequenceBarrier sequenceBarrier = ringBuffer.NewBarrier(barrierSequences);
            WorkerPool<T> workerPool = new WorkerPool<T>(ringBuffer, sequenceBarrier, exceptionHandler, workHandlers);

            consumerRepository.Add(workerPool, sequenceBarrier);

            ISequence[] workerSequences = workerPool.GetWorkerSequences();

            UpdateGatingSequencesForNextInChain(barrierSequences, workerSequences);

            return new EventHandlerGroup<T>(this, consumerRepository, workerSequences);
        }

        private void CheckNotStarted()
        {
            if (started.ReadFullFence())
            {
                throw new IllegalStateException("All event handlers must be added before calling starts.");
            }
        }

        private void CheckOnlyStartedOnce()
        {
            if (!started.AtomicCompareExchange(true, false))
            {
                throw new IllegalStateException("Disruptor.start() must only be called once.");
            }
        }

        private void UpdateGatingSequencesForNextInChain(ISequence[] barrierSequences, ISequence[] processorSequences)
        {
            if (processorSequences.Length > 0)
            {
                //TODO:3.3.0没有
                ringBuffer.AddGatingSequences(processorSequences);
                foreach (Sequence barrierSequence in barrierSequences)
                {
                    ringBuffer.RemoveGatingSequence(barrierSequence);
                }
                consumerRepository.UnMarkEventProcessorsAsEndOfChain(barrierSequences);
            }
        }

        /// <summary>
        /// Confirms if all messages have been consumed by all event processors.
        /// </summary>
        /// <returns></returns>
        private Boolean HasBacklog()
        {
            long cursor = ringBuffer.GetCursor();
            foreach (Sequence consumer in consumerRepository.GetLastSequenceInChain(false))
            {
                if (cursor > consumer.Get())
                {
                    return true;
                }
            }
            return false;
        }

        public override String ToString()
        {
            return "Disruptor{" +
                "ringBuffer=" + ringBuffer +
                ", started=" + started +
                ", executor=" + executor +
                '}';
        }

    }
}
