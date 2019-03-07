using System;

namespace Disruptor.Dsl
{
    /// <summary>
    /// A group of <see cref="IEventProcessor"/>s used as part of the <see cref="Disruptor{T}"/>.
    /// </summary>
    /// <typeparam name="T">the type of entry used by the event processors.</typeparam>
    public class EventHandlerGroup<T> where T : class
    {
        private readonly Disruptor<T> disruptor;
        private readonly ConsumerRepository<T> consumerRepository;
        private readonly ISequence[] sequences;

        internal EventHandlerGroup(
            Disruptor<T> disruptor,
            ConsumerRepository<T> consumerRepository,
            ISequence[] sequences)
        {
            this.disruptor = disruptor;
            this.consumerRepository = consumerRepository;
            //this.sequences = Arrays.copyOf(sequences, sequences.length);
            this.sequences = new Sequence[sequences.Length];
            Array.Copy(sequences, this.sequences, sequences.Length);
        }

        /// <summary>
        /// Create a new event handler group that combines the consumers in this group with <code>otherHandlerGroup</code>.
        /// </summary>
        /// <param name="otherHandlerGroup">the event handler group to combine.</param>
        /// <returns>a new EventHandlerGroup combining the existing and new consumers into a single dependency group.</returns>
        public EventHandlerGroup<T> And(EventHandlerGroup<T> otherHandlerGroup)
        {
            Sequence[] combinedSequences = new Sequence[this.sequences.Length + otherHandlerGroup.sequences.Length];
            Array.Copy(this.sequences, 0, combinedSequences, 0, this.sequences.Length);
            Array.Copy(otherHandlerGroup.sequences, 0, combinedSequences, this.sequences.Length, otherHandlerGroup.sequences.Length);
            return new EventHandlerGroup<T>(disruptor, consumerRepository, combinedSequences);
        }

        /// <summary>
        /// Create a new event handler group that combines the handlers in this group with <code>processors</code>.
        /// </summary>
        /// <param name="processors">the processors to combine.</param>
        /// <returns>a new EventHandlerGroup combining the existing and new processors into a single dependency group.</returns>
        public EventHandlerGroup<T> And(params IEventProcessor[] processors)
        {
            ISequence[] combinedSequences = new Sequence[sequences.Length + processors.Length];

            for (int i = 0; i < processors.Length; i++)
            {
                consumerRepository.Add(processors[i]);
                combinedSequences[i] = processors[i].GetSequence();
            }
            Array.Copy(this.sequences, 0, combinedSequences, processors.Length, this.sequences.Length);

            return new EventHandlerGroup<T>(disruptor, consumerRepository, combinedSequences);
        }

        /// <summary>
        /// Set up batch handlers to consume events from the ring buffer. These handlers will only process events
        /// after every {@link EventProcessor} in this group has processed the event.
        ///
        /// <p>This method is generally used as part of a chain. For example if the handler <code>A</code> must
        /// process events before handler <code>B</code>:</p>
        ///
        /// <pre><code>dw.HandleEventsWith(A).then(B);</code></pre>
        /// </summary>
        /// <param name="handlers">the batch handlers that will process events.</param>
        /// <returns>a <see cref="EventHandlerGroup{T}"/> that can be used to set up a event processor barrier over the created event processors.</returns>
        public EventHandlerGroup<T> Then(params IEventHandler<T>[] handlers)
        {
            return HandleEventsWith(handlers);
        }

        /// <summary>
        /// Set up custom event processors to handle events from the ring buffer. The Disruptor will
        /// automatically start these processors when <see cref="Disruptor{T}.Start"/> is called.
        ///
        /// This method is generally used as part of a chain. For example if the handler <code>A</code> must
        /// process events before handler <code>B</code>:
        /// </summary>
        /// <param name="eventProcessorFactories">the event processor factories to use to create the event processors that will process events.</param>
        /// <returns>a <see cref="EventHandlerGroup{T}"/> that can be used to chain dependencies.</returns>
        public EventHandlerGroup<T> Then(params IEventProcessorFactory<T>[] eventProcessorFactories)
        {
            return HandleEventsWith(eventProcessorFactories);
        }

        /// <summary>
        /// Set up a worker pool to handle events from the ring buffer. The worker pool will only process events
        /// after every <see cref="IEventProcessor"/> in this group has processed the event.
        /// Each event will be processed by one of the work handler instances.
        ///
        /// This method is generally used as part of a chain. For example if the handler <code>A</code> must
        /// process events before the worker pool with handlers <code>B, C</code>:
        ///
        /// <pre><code>dw.HandleEventsWith(A).thenHandleEventsWithWorkerPool(B, C);</code></pre>
        /// </summary>
        /// <param name="handlers">the work handlers that will process events. Each work handler instance will provide an extra thread in the worker pool.</param>
        /// <returns>a <see cref="EventHandlerGroup{T}"/> that can be used to set up a event processor barrier over the created event processors.</returns>
        public EventHandlerGroup<T> ThenHandleEventsWithWorkerPool(params IWorkHandler<T>[] handlers)
        {
            return HandleEventsWithWorkerPool(handlers);
        }

        /// <summary>
        /// Set up batch handlers to handle events from the ring buffer. These handlers will only process events
        /// after every <see cref="IEventProcessor"/> in this group has processed the event.
        ///
        /// This method is generally used as part of a chain. For example if the handler <code>A</code> must
        /// process events before handler <code>B</code>:
        ///
        /// <pre><code>dw.after(A).HandleEventsWith(B);</code></pre>
        /// </summary>
        /// <param name="handlers">the batch handlers that will process events.</param>
        /// <returns>a <see cref="EventHandlerGroup{T}"/> that can be used to set up a event processor barrier over the created event processors.</returns>
        public EventHandlerGroup<T> HandleEventsWith(params IEventHandler<T>[] handlers)
        {
            return disruptor.CreateEventProcessors(sequences, handlers);
        }

        /// <summary>
        /// Set up custom event processors to handle events from the ring buffer. The Disruptor will
        /// automatically start these processors when <see cref="Disruptor{T}.Start"/> is called.
        /// 
        /// This method is generally used as part of a chain. For example if <code>A</code> must
        /// process events before <code>B</code>:
        ///
        /// <pre><code>dw.after(A).handleEventsWith(B);</code></pre>
        /// </summary>
        /// <param name="eventProcessorFactories">eventProcessorFactories the event processor factories to use to create the event processors that will process events.</param>
        /// <returns>a <see cref="EventHandlerGroup{T}"/> that can be used to chain dependencies.</returns>
        public EventHandlerGroup<T> HandleEventsWith(params IEventProcessorFactory<T>[] eventProcessorFactories)
        {
            return disruptor.CreateEventProcessors(sequences, eventProcessorFactories);
        }

        /// <summary>
        /// Set up a worker pool to handle events from the ring buffer. The worker pool will only process events
        /// after every {@link EventProcessor} in this group has processed the event. Each event will be processed
        /// by one of the work handler instances.
        ///
        /// <p>This method is generally used as part of a chain. For example if the handler <code>A</code> must
        /// process events before the worker pool with handlers <code>B, C</code>:</p>
        ///
        /// <pre><code>dw.after(A).handleEventsWithWorkerPool(B, C);</code></pre>
        /// </summary>
        /// <param name="handlers">the work handlers that will process events. Each work handler instance will provide an extra thread in the worker pool.</param>
        /// <returns>a <see cref="EventHandlerGroup{T}"/> that can be used to set up a event processor barrier over the created event processors.</returns>
        public EventHandlerGroup<T> HandleEventsWithWorkerPool(params IWorkHandler<T>[] handlers)
        {
            return disruptor.CreateWorkerPool(sequences, handlers);
        }

        /// <summary>
        /// Create a dependency barrier for the processors in this group.
        /// This allows custom event processors to have dependencies on
        /// <see cref="BatchEventProcessor{T}"/>s created by the disruptor.
        /// </summary>
        /// <returns>a <see cref="ISequenceBarrier"/> including all the processors in this group.</returns>
        public ISequenceBarrier AsSequenceBarrier()
        {
            return disruptor.GetRingBuffer().NewBarrier(sequences);
        }

    }
}
