using System;

namespace Disruptor
{
    /// <summary>
    /// Ring based store of reusable entries containing the data representing
    /// an event being exchanged between event producer and <see cref="IEventProcessor"/>s.
    /// </summary>
    /// <typeparam name="T">implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public sealed class RingBuffer<T> : RingBufferFields<T>, ICursored, IEventSequencer<T>, IEventSink<T>
    {
        //public static readonly long INITIAL_CURSOR_VALUE = Sequence.INITIAL_VALUE;

        private long P9, P10, P11, P12, P13, P14, P15;

        /// <summary>
        /// Construct a RingBuffer with the full option set.
        /// </summary>
        /// <param name="eventFactory">to newInstance entries for filling the RingBuffer.</param>
        /// <param name="sequencer">sequencer to handle the ordering of events moving through the RingBuffer.</param>
        /// <exception cref="IllegalArgumentException">if bufferSize is less than 1 or not a power of 2.</exception>
        public RingBuffer(ISequencer sequencer, IEventFactory<T> eventFactory)
            : base(sequencer, eventFactory)
        {
        }

        /// <summary>
        /// Construct a RingBuffer with a <see cref="MultiProducerSequencer"/> sequencer.
        /// </summary>
        /// <param name="eventFactory"> eventFactory to create entries for filling the RingBuffer</param>
        /// <param name="bufferSize">number of elements to create within the ring buffer.</param>
        public RingBuffer(IEventFactory<T> eventFactory, int bufferSize)
            : this(new MultiProducerSequencer(bufferSize, new BlockingWaitStrategy()), eventFactory)
        {
        }

        #region Method

        /// <summary>
        /// Create a new multiple producer RingBuffer with the specified wait strategy.
        /// 
        /// @param <E> Class of the event stored in the ring buffer.
        /// </summary>
        /// <param name="factory">used to Create the events within the ring buffer.</param>
        /// <param name="bufferSize">number of elements to Create within the ring buffer.</param>
        /// <param name="waitStrategy">used to determine how to wait for new elements to become available.</param>
        /// <exception cref="IllegalArgumentException">if bufferSize is less than 1 or not a power of 2</exception>
        /// <returns>a constructed ring buffer.</returns>
        public static RingBuffer<T> CreateMultiProducer(IEventFactory<T> factory, int bufferSize, IWaitStrategy waitStrategy)
        {
            MultiProducerSequencer sequencer = new MultiProducerSequencer(bufferSize, waitStrategy);

            return new RingBuffer<T>(sequencer, factory);
        }

        /// <summary>
        /// Create a new multiple producer RingBuffer using the default wait strategy  {@link BlockingWaitStrategy}.
        /// 
        /// @param <E> Class of the event stored in the ring buffer.
        /// </summary>
        /// <param name="factory">used to Create the events within the ring buffer.</param>
        /// <param name="bufferSize">number of elements to Create within the ring buffer.</param>
        /// <exception cref="IllegalArgumentException">if bufferSize is less than 1 or not a power of 2</exception>
        /// <returns>a constructed ring buffer.</returns>
        public static RingBuffer<T> CreateMultiProducer(IEventFactory<T> factory, int bufferSize)
        {
            return CreateMultiProducer(factory, bufferSize, new BlockingWaitStrategy());
        }

        /// <summary>
        /// Create a new single producer RingBuffer with the specified wait strategy.
        /// 
        /// @param <E> Class of the event stored in the ring buffer.
        /// </summary>
        /// <param name="factory">used to Create the events within the ring buffer.</param>
        /// <param name="bufferSize">number of elements to Create within the ring buffer.</param>
        /// <param name="waitStrategy">used to determine how to wait for new elements to become available.</param>
        /// <exception cref="IllegalArgumentException">if bufferSize is less than 1 or not a power of 2</exception>
        /// <returns>a constructed ring buffer.</returns>
        public static RingBuffer<T> CreateSingleProducer(IEventFactory<T> factory, int bufferSize, IWaitStrategy waitStrategy)
        {
            SingleProducerSequencer sequencer = new SingleProducerSequencer(bufferSize, waitStrategy);

            return new RingBuffer<T>(sequencer, factory);
        }

        /// <summary>
        /// Create a new single producer RingBuffer using the default wait strategy  {@link BlockingWaitStrategy}.
        /// </summary>
        /// <param name="factory">used to Create the events within the ring buffer.</param>
        /// <param name="bufferSize">number of elements to Create within the ring buffer.</param>
        /// <exception cref="IllegalArgumentException">if bufferSize is less than 1 or not a power of 2</exception>
        /// <returns>a constructed ring buffer.</returns>
        public static RingBuffer<T> CreateSingleProducer(IEventFactory<T> factory, int bufferSize)
        {
            return CreateSingleProducer(factory, bufferSize, new BlockingWaitStrategy());
        }

        /// <summary>
        /// Create a new Ring Buffer with the specified producer type (SINGLE or MULTI)
        /// 
        /// @param <E> Class of the event stored in the ring buffer.
        /// </summary>
        /// <param name="producerType">producer type to use {@link ProducerType}.</param>
        /// <param name="factory">used to Create events within the ring buffer.</param>
        /// <param name="bufferSize"> number of elements to Create within the ring buffer.</param>
        /// <param name="waitStrategy">used to determine how to wait for new elements to become available.</param>
        /// <exception cref="IllegalArgumentException">if bufferSize is less than 1 or not a power of 2</exception>
        /// <returns>a constructed ring buffer.</returns>
        public static RingBuffer<T> Create(ProducerType producerType, IEventFactory<T> factory, int bufferSize, IWaitStrategy waitStrategy)
        {
            switch (producerType)
            {
                case ProducerType.SINGLE:
                    return CreateSingleProducer(factory, bufferSize, waitStrategy);
                case ProducerType.MULTI:
                    return CreateMultiProducer(factory, bufferSize, waitStrategy);
                default:
                    throw new IllegalStateException(producerType.ToString());
            }
        }

        /// <summary>
        /// Resets the cursor to a specific value.  This can be applied at any time, but it is worth noting
        /// that it can cause a data race and should only be used in controlled circumstances.  E.g. during
        /// initialisation. 
        /// </summary>
        /// <param name="sequence">The sequence to reset too.</param>
        /// <exception cref="IllegalStateException">If any gating sequences have already been specified.</exception>
        [Obsolete]
        public void ResetTo(long sequence)
        {
            sequencer.Claim(sequence);
            sequencer.Publish(sequence);
        }

        /// <summary>
        /// Sets the cursor to a specific sequence and returns the preallocated entry that is stored there.  This
        /// can cause a data race and should only be done in controlled circumstances, e.g. during initialisation.
        /// </summary>
        /// <param name="sequence">The sequence to claim.</param>
        /// <returns>The preallocated event.</returns>
        public T ClaimAndGetPreallocated(long sequence)
        {
            sequencer.Claim(sequence);
            return Get(sequence);
        }

        /// <summary>
        /// Determines if a particular entry is available.  Note that using this when not within a context that is
        /// maintaining a sequence barrier, it is likely that using this to determine if you can read a value is likely
        /// to result in a race condition and broken code.
        /// 
        /// @Deprecated Please don't use this method.  It probably won't do what you think that it does.
        /// </summary>
        /// <param name="sequence">The sequence to identify the entry.</param>
        /// <returns>If the value can be read or not.</returns>
        [Obsolete("Please don't use this method. It probably won't do what you think that it does.")]
        public Boolean IsPublished(long sequence)
        {
            return sequencer.IsAvailable(sequence);
        }

        /// <summary>
        /// Add the specified gating sequences to this instance of the Disruptor.
        /// They will safely and atomically added to the list of gating sequences.
        /// </summary>
        /// <param name="gatingSequences">The sequences to add.</param>
        public void AddGatingSequences(params ISequence[] gatingSequences)
        {
            sequencer.AddGatingSequences(gatingSequences);
        }

        /// <summary>
        /// Get the minimum sequence value from all of the gating sequences added to this ringBuffer.
        /// </summary>
        /// <returns>The minimum gating sequence or the cursor sequence if no sequences have been added.</returns>
        public long GetMinimumGatingSequence()
        {
            return sequencer.GetMinimumSequence();
        }

        /// <summary>
        /// Remove the specified sequence from this ringBuffer.
        /// </summary>
        /// <param name="sequence">to be removed.</param>
        /// <returns><code>true</code> if this sequence was found, <code>false</code> otherwise.</returns>
        public Boolean RemoveGatingSequence(ISequence sequence)
        {
            return sequencer.RemoveGatingSequence(sequence);
        }

        /// <summary>
        /// Create a new SequenceBarrier to be used by an EventProcessor to track which messages
        /// are available to be read from the ring buffer given a list of sequences to track.
        /// </summary>
        /// <param name="sequencesToTrack">the additional sequences to track.</param>
        /// <returns>A sequence barrier that will track the specified sequences.</returns>
        public ISequenceBarrier NewBarrier(params ISequence[] sequencesToTrack)
        {
            return sequencer.NewBarrier(sequencesToTrack);
        }

        /// <summary>
        /// Creates an event poller for this ring buffer gated on the supplied sequences.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gatingSequences">to be gated on.</param>
        /// <returns>A poller that will gate on this ring buffer and the supplied sequences.</returns>
        public EventPoller<T> NewPoller(params ISequence[] gatingSequences)
        {
            return sequencer.NewPoller(this, gatingSequences);
        }

        #endregion

        /// <summary>
        /// Get the current cursor value for the ring buffer.  The actual value received
        /// will depend on the type of {@link Sequencer} that is being used.
        /// </summary>
        /// <returns></returns>
        public long GetCursor()
        {
            return sequencer.GetCursor();
        }

        /// <summary>
        /// Get the event for a given sequence in the RingBuffer.
        /// 
        /// This call has 2 uses.
        /// Firstly use this call when publishing to a ring buffer.
        /// After calling {@link RingBuffer#next()} use this call to get hold of the
        /// preallocated event to fill with data before calling {@link RingBuffer#publish(long)}.
        /// 
        /// Secondly use this call when consuming data from the ring buffer.  After calling
        /// {@link SequenceBarrier#waitFor(long)} call this method with any value greater than
        /// that your current consumer sequence and less than or equal to the value returned from
        /// the {@link SequenceBarrier#waitFor(long)} method.
        /// </summary>
        /// <param name="sequence">for the event.</param>
        /// <returns>the event for the given sequence.</returns>
        public T Get(long sequence)
        {
            return ElementAt(sequence);
        }

        #region ISequenced

        /// <summary>
        /// The size of the buffer.
        /// </summary>
        /// <returns></returns>
        public int GetBufferSize()
        {
            return bufferSize;
        }

        /// <summary>
        /// Given specified <paramref name="requiredCapacity"/> determines if that amount of space
        /// is available.  Note, you can not assume that if this method returns <code>true</code>
        /// that a call to <see cref="Next"/> will not block.  Especially true if this
        /// ring buffer is set up to handle multiple producers.
        /// 
        /// </summary>
        /// <param name="requiredCapacity">The capacity to check for.</param>
        /// <returns>true If the specified requiredCapacity is available false if not.</returns>
        public Boolean HasAvailableCapacity(int requiredCapacity)
        {
            return sequencer.HasAvailableCapacity(requiredCapacity);
        }

        /// <summary>
        /// Get the remaining capacity for this ringBuffer.
        /// </summary>
        /// <returns>The number of slots remaining.</returns>
        public long RemainingCapacity()
        {
            return sequencer.RemainingCapacity();
        }

        /// <summary>
        /// Increment and return the next sequence for the ring buffer.  Calls of this
        /// method should ensure that they always publish the sequence afterward.
        /// 
        /// E.g.
        /// <code>
        /// long sequence = ringBuffer.next();
        /// try {
        ///     Event e = ringBuffer.get(sequence);
        ///     // Do some work with the event.
        /// } finally {
        ///     ringBuffer.publish(sequence);
        /// }
        /// </code>
        /// 
        /// </summary>
        /// <returns>The next sequence to publish to.</returns>
        public long Next()
        {
            return sequencer.Next();
        }

        /// <summary>
        /// The same functionality as <see cref="Next"/>, but allows the caller to claim
        /// the next n sequences.
        /// </summary>
        /// <param name="n">number of slots to claim.</param>
        /// <returns>sequence number of the highest slot claimed.</returns>
        public long Next(int n)
        {
            return sequencer.Next(n);
        }

        /// <summary>
        /// Increment and return the next sequence for the ring buffer.  Calls of this
        /// method should ensure that they always publish the sequence afterward.
        /// 
        /// E.g.
        /// <code>
        /// long sequence = ringBuffer.next();
        /// try {
        ///     Event e = ringBuffer.get(sequence);
        ///     // Do some work with the event.
        /// } finally {
        ///     ringBuffer.publish(sequence);
        /// }
        /// </code>
        /// This method will not block if there is not space available in the ring
        /// buffer, instead it will throw an InsufficientCapacityException.
        /// 
        /// </summary>
        /// <exception cref="InsufficientCapacityException">if the necessary space in the ring buffer is not available</exception>
        /// <returns>The next sequence to publish to.</returns>
        public long TryNext()
        {
            return sequencer.TryNext();
        }

        /// <summary>
        /// The same functionality as <see cref="TryNext"/>, but allows the caller to attempt
        /// to claim the next n sequences.
        /// </summary>
        /// <param name="n">number of slots to claim.</param>
        /// <exception cref="InsufficientCapacityException">if the necessary space in the ring buffer is not available</exception>
        /// <returns>sequence number of the highest slot claimed.</returns>
        public long TryNext(int n)
        {
            return sequencer.TryNext(n);
        }

        /// <summary>
        /// Publish the specified sequence.  This action marks this particular
        /// message as being available to be read.
        /// </summary>
        /// <param name="sequence">the sequence to publish.</param>
        public void Publish(long sequence)
        {
            sequencer.Publish(sequence);
        }

        /// <summary>
        /// Publish the specified sequences.  This action marks these particular
        /// messages as being available to be read.
        /// </summary>
        /// <param name="lo">the lowest sequence number to be published.</param>
        /// <param name="hi">the highest sequence number to be published.</param>
        public void Publish(long lo, long hi)
        {
            sequencer.Publish(lo, hi);
        }

        #endregion

        #region IEventSink<T>

        /// <summary>
        /// <see cref="IEventSink{T}.PublishEvent(IEventTranslator{T})"/>
        /// </summary>
        /// <param name="translator">The user specified translation for the event</param>
        public void PublishEvent(IEventTranslator<T> translator)
        {
            long sequence = sequencer.Next();
            TranslateAndPublish(translator, sequence);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.PublishEvent{A}(IEventTranslatorOneArg{T, A}, A)"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator"></param>
        /// <param name="arg0"></param>
        public void PublishEvent<A>(IEventTranslatorOneArg<T, A> translator, A arg0)
        {
            long sequence = sequencer.Next();
            TranslateAndPublish(translator, sequence, arg0);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.PublishEvent{A, B}(IEventTranslatorTwoArg{T, A, B}, A, B)"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        public void PublishEvent<A, B>(IEventTranslatorTwoArg<T, A, B> translator, A arg0, B arg1)
        {
            long sequence = sequencer.Next();
            TranslateAndPublish(translator, sequence, arg0, arg1);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.PublishEvent{A, B, C}(IEventTranslatorThreeArg{T, A, B, C}, A, B, C)"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="translator"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        public void PublishEvent<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, A arg0, B arg1, C arg2)
        {
            long sequence = sequencer.Next();
            TranslateAndPublish(translator, sequence, arg0, arg1, arg2);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.PublishEvent(IEventTranslatorVararg{T}, object[])"/>
        /// </summary>
        /// <param name="translator"></param>
        /// <param name="args"></param>
        public void PublishEvent(IEventTranslatorVararg<T> translator, params Object[] args)
        {
            long sequence = sequencer.Next();
            TranslateAndPublish(translator, sequence, args);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.TryPublishEvent(IEventTranslator{T})"/>
        /// </summary>
        /// <param name="translator"></param>
        /// <returns></returns>
        public Boolean TryPublishEvent(IEventTranslator<T> translator)
        {
            try
            {
                long sequence = sequencer.TryNext();
                TranslateAndPublish(translator, sequence);
                return true;
            }
            catch (InsufficientCapacityException)
            {
                return false;
            }
        }

        /// <summary>
        /// <see cref="IEventSink{T}.TryPublishEvent{A}(IEventTranslatorOneArg{T, A}, A)"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator"></param>
        /// <param name="arg0"></param>
        /// <returns></returns>
        public Boolean TryPublishEvent<A>(IEventTranslatorOneArg<T, A> translator, A arg0)
        {
            try
            {
                long sequence = sequencer.TryNext();
                TranslateAndPublish(translator, sequence, arg0);
                return true;
            }
            catch (InsufficientCapacityException)
            {
                return false;
            }
        }

        /// <summary>
        /// <see cref="IEventSink{T}.TryPublishEvent{A, B}(IEventTranslatorTwoArg{T, A, B}, A, B)"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <returns></returns>
        public Boolean TryPublishEvent<A, B>(IEventTranslatorTwoArg<T, A, B> translator, A arg0, B arg1)
        {
            try
            {
                long sequence = sequencer.TryNext();
                TranslateAndPublish(translator, sequence, arg0, arg1);
                return true;
            }
            catch (InsufficientCapacityException)
            {
                return false;
            }
        }

        /// <summary>
        /// <see cref="IEventSink{T}.TryPublishEvent{A, B, C}(IEventTranslatorThreeArg{T, A, B, C}, A, B, C)"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="translator"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        public Boolean TryPublishEvent<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, A arg0, B arg1, C arg2)
        {
            try
            {
                long sequence = sequencer.TryNext();
                TranslateAndPublish(translator, sequence, arg0, arg1, arg2);
                return true;
            }
            catch (InsufficientCapacityException)
            {
                return false;
            }
        }

        /// <summary>
        /// <see cref="IEventSink{T}.TryPublishEvent(IEventTranslatorVararg{T}, object[])"/>
        /// </summary>
        /// <param name="translator"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public Boolean TryPublishEvent(IEventTranslatorVararg<T> translator, params Object[] args)
        {
            try
            {
                long sequence = sequencer.TryNext();
                TranslateAndPublish(translator, sequence, args);
                return true;
            }
            catch (InsufficientCapacityException)
            {
                return false;
            }
        }

        /// <summary>
        /// <see cref="IEventSink{T}.PublishEvents(IEventTranslator{T}[])"/>
        /// </summary>
        /// <param name="translators"></param>
        public void PublishEvents(IEventTranslator<T>[] translators)
        {
            PublishEvents(translators, 0, translators.Length);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.PublishEvents(IEventTranslator{T}[], int, int)"/>
        /// </summary>
        /// <param name="translators"></param>
        /// <param name="batchStartsAt"></param>
        /// <param name="batchSize"></param>
        public void PublishEvents(IEventTranslator<T>[] translators, int batchStartsAt, int batchSize)
        {
            CheckBounds(translators, batchStartsAt, batchSize);
            long finalSequence = sequencer.Next(batchSize);
            TranslateAndPublishBatch(translators, batchStartsAt, batchSize, finalSequence);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.PublishEvents{A}(IEventTranslatorOneArg{T, A}, A[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator"></param>
        /// <param name="arg0"></param>
        public void PublishEvents<A>(IEventTranslatorOneArg<T, A> translator, A[] arg0)
        {
            PublishEvents(translator, 0, arg0.Length, arg0);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.PublishEvents{A}(IEventTranslatorOneArg{T, A}, int, int, A[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator"></param>
        /// <param name="batchStartsAt"></param>
        /// <param name="batchSize"></param>
        /// <param name="arg0"></param>
        public void PublishEvents<A>(IEventTranslatorOneArg<T, A> translator, int batchStartsAt, int batchSize, A[] arg0)
        {
            CheckBounds(arg0, batchStartsAt, batchSize);
            long finalSequence = sequencer.Next(batchSize);
            TranslateAndPublishBatch(translator, arg0, batchStartsAt, batchSize, finalSequence);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.PublishEvents{A, B}(IEventTranslatorTwoArg{T, A, B}, A[], B[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        public void PublishEvents<A, B>(IEventTranslatorTwoArg<T, A, B> translator, A[] arg0, B[] arg1)
        {
            PublishEvents(translator, 0, arg0.Length, arg0, arg1);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.PublishEvents{A, B}(IEventTranslatorTwoArg{T, A, B}, int, int, A[], B[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator"></param>
        /// <param name="batchStartsAt"></param>
        /// <param name="batchSize"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        public void PublishEvents<A, B>(IEventTranslatorTwoArg<T, A, B> translator, int batchStartsAt, int batchSize, A[] arg0, B[] arg1)
        {
            CheckBounds(arg0, arg1, batchStartsAt, batchSize);
            long finalSequence = sequencer.Next(batchSize);
            TranslateAndPublishBatch(translator, arg0, arg1, batchStartsAt, batchSize, finalSequence);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.PublishEvents{A, B, C}(IEventTranslatorThreeArg{T, A, B, C}, A[], B[], C[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="translator"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        public void PublishEvents<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, A[] arg0, B[] arg1, C[] arg2)
        {
            PublishEvents(translator, 0, arg0.Length, arg0, arg1, arg2);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.PublishEvents{A, B, C}(IEventTranslatorThreeArg{T, A, B, C}, int, int, A[], B[], C[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="translator"></param>
        /// <param name="batchStartsAt"></param>
        /// <param name="batchSize"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        public void PublishEvents<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, int batchStartsAt, int batchSize, A[] arg0, B[] arg1, C[] arg2)
        {
            CheckBounds(arg0, arg1, arg2, batchStartsAt, batchSize);
            long finalSequence = sequencer.Next(batchSize);
            TranslateAndPublishBatch(translator, arg0, arg1, arg2, batchStartsAt, batchSize, finalSequence);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.PublishEvents(IEventTranslatorVararg{T}, object[][])"/>
        /// </summary>
        /// <param name="translator"></param>
        /// <param name="args"></param>
        public void PublishEvents(IEventTranslatorVararg<T> translator, params Object[][] args)
        {
            PublishEvents(translator, 0, args.Length, args);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.PublishEvents(IEventTranslatorVararg{T}, int, int, object[][])"/>
        /// </summary>
        /// <param name="translator"></param>
        /// <param name="batchStartsAt"></param>
        /// <param name="batchSize"></param>
        /// <param name="args"></param>
        public void PublishEvents(IEventTranslatorVararg<T> translator, int batchStartsAt, int batchSize, params Object[][] args)
        {
            CheckBounds(batchStartsAt, batchSize, args);
            long finalSequence = sequencer.Next(batchSize);
            TranslateAndPublishBatch(translator, batchStartsAt, batchSize, finalSequence, args);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.TryPublishEvents(IEventTranslator{T}[])"/>
        /// </summary>
        /// <param name="translators"></param>
        /// <returns></returns>
        public Boolean TryPublishEvents(IEventTranslator<T>[] translators)
        {
            return TryPublishEvents(translators, 0, translators.Length);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.TryPublishEvents(IEventTranslator{T}[], int, int)"/>
        /// </summary>
        /// <param name="translators"></param>
        /// <param name="batchStartsAt"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public Boolean TryPublishEvents(IEventTranslator<T>[] translators, int batchStartsAt, int batchSize)
        {
            CheckBounds(translators, batchStartsAt, batchSize);
            try
            {
                long finalSequence = sequencer.TryNext(batchSize);
                TranslateAndPublishBatch(translators, batchStartsAt, batchSize, finalSequence);
                return true;
            }
            catch (InsufficientCapacityException)
            {
                return false;
            }
        }

        /// <summary>
        /// <see cref="IEventSink{T}.TryPublishEvents{A}(IEventTranslatorOneArg{T, A}, A[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator"></param>
        /// <param name="arg0"></param>
        /// <returns></returns>
        public Boolean TryPublishEvents<A>(IEventTranslatorOneArg<T, A> translator, A[] arg0)
        {
            return TryPublishEvents(translator, 0, arg0.Length, arg0);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.TryPublishEvents{A}(IEventTranslatorOneArg{T, A}, int, int, A[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator"></param>
        /// <param name="batchStartsAt"></param>
        /// <param name="batchSize"></param>
        /// <param name="arg0"></param>
        /// <returns></returns>
        public Boolean TryPublishEvents<A>(IEventTranslatorOneArg<T, A> translator, int batchStartsAt, int batchSize, A[] arg0)
        {
            CheckBounds(arg0, batchStartsAt, batchSize);
            try
            {
                long finalSequence = sequencer.TryNext(batchSize);
                TranslateAndPublishBatch(translator, arg0, batchStartsAt, batchSize, finalSequence);
                return true;
            }
            catch (InsufficientCapacityException)
            {
                return false;
            }
        }

        /// <summary>
        /// <see cref="IEventSink{T}.TryPublishEvents{A, B}(IEventTranslatorTwoArg{T, A, B}, A[], B[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <returns></returns>
        public Boolean TryPublishEvents<A, B>(IEventTranslatorTwoArg<T, A, B> translator, A[] arg0, B[] arg1)
        {
            return TryPublishEvents(translator, 0, arg0.Length, arg0, arg1);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.TryPublishEvents{A, B}(IEventTranslatorTwoArg{T, A, B}, int, int, A[], B[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator"></param>
        /// <param name="batchStartsAt"></param>
        /// <param name="batchSize"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <returns></returns>
        public Boolean TryPublishEvents<A, B>(IEventTranslatorTwoArg<T, A, B> translator, int batchStartsAt, int batchSize, A[] arg0, B[] arg1)
        {
            CheckBounds(arg0, arg1, batchStartsAt, batchSize);
            try
            {
                long finalSequence = sequencer.TryNext(batchSize);
                TranslateAndPublishBatch(translator, arg0, arg1, batchStartsAt, batchSize, finalSequence);
                return true;
            }
            catch (InsufficientCapacityException)
            {
                return false;
            }
        }

        /// <summary>
        /// <see cref="IEventSink{T}.TryPublishEvents{A, B, C}(IEventTranslatorThreeArg{T, A, B, C}, A[], B[], C[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="translator"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        public Boolean TryPublishEvents<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, A[] arg0, B[] arg1, C[] arg2)
        {
            return TryPublishEvents(translator, 0, arg0.Length, arg0, arg1, arg2);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.TryPublishEvents{A, B, C}(IEventTranslatorThreeArg{T, A, B, C}, int, int, A[], B[], C[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="translator"></param>
        /// <param name="batchStartsAt"></param>
        /// <param name="batchSize"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        public Boolean TryPublishEvents<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, int batchStartsAt, int batchSize, A[] arg0, B[] arg1, C[] arg2)
        {
            CheckBounds(arg0, arg1, arg2, batchStartsAt, batchSize);
            try
            {
                long finalSequence = sequencer.TryNext(batchSize);
                TranslateAndPublishBatch(translator, arg0, arg1, arg2, batchStartsAt, batchSize, finalSequence);
                return true;
            }
            catch (InsufficientCapacityException)
            {
                return false;
            }
        }

        /// <summary>
        /// <see cref="IEventSink{T}.TryPublishEvents(IEventTranslatorVararg{T}, object[][])"/>
        /// </summary>
        /// <param name="translator"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public Boolean TryPublishEvents(IEventTranslatorVararg<T> translator, params Object[][] args)
        {
            return TryPublishEvents(translator, 0, args.Length, args);
        }

        /// <summary>
        /// <see cref="IEventSink{T}.TryPublishEvents(IEventTranslatorVararg{T}, int, int, object[][])"/>
        /// </summary>
        /// <param name="translator"></param>
        /// <param name="batchStartsAt"></param>
        /// <param name="batchSize"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public Boolean TryPublishEvents(IEventTranslatorVararg<T> translator, int batchStartsAt, int batchSize, params Object[][] args)
        {
            CheckBounds(args, batchStartsAt, batchSize);
            try
            {
                long finalSequence = sequencer.TryNext(batchSize);
                TranslateAndPublishBatch(translator, batchStartsAt, batchSize, finalSequence, args);
                return true;
            }
            catch (InsufficientCapacityException)
            {
                return false;
            }
        }

        #endregion

        #region Private Method

        private void TranslateAndPublish(IEventTranslator<T> translator, long sequence)
        {
            try
            {
                translator.TranslateTo(Get(sequence), sequence);
            }
            finally
            {
                sequencer.Publish(sequence);
            }
        }

        private void TranslateAndPublish<A>(IEventTranslatorOneArg<T, A> translator, long sequence,
            A arg0)
        {
            try
            {
                translator.TranslateTo(Get(sequence), sequence, arg0);
            }
            finally
            {
                sequencer.Publish(sequence);
            }
        }

        private void TranslateAndPublish<A, B>(IEventTranslatorTwoArg<T, A, B> translator, long sequence,
            A arg0, B arg1)
        {
            try
            {
                translator.TranslateTo(Get(sequence), sequence, arg0, arg1);
            }
            finally
            {
                sequencer.Publish(sequence);
            }
        }

        private void TranslateAndPublish<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, long sequence,
            A arg0, B arg1, C arg2)
        {
            try
            {
                translator.TranslateTo(Get(sequence), sequence, arg0, arg1, arg2);
            }
            finally
            {
                sequencer.Publish(sequence);
            }
        }

        private void TranslateAndPublish(IEventTranslatorVararg<T> translator, long sequence,
            params Object[] args)
        {
            try
            {
                translator.TranslateTo(Get(sequence), sequence, args);
            }
            finally
            {
                sequencer.Publish(sequence);
            }
        }

        private void TranslateAndPublishBatch(IEventTranslator<T>[] translators,
            int batchStartsAt, int batchSize, long finalSequence)
        {
            long initialSequence = finalSequence - (batchSize - 1);
            try
            {
                long sequence = initialSequence;
                int batchEndsAt = batchStartsAt + batchSize;
                for (int i = batchStartsAt; i < batchEndsAt; i++)
                {
                    IEventTranslator<T> translator = translators[i];
                    translator.TranslateTo(Get(sequence), sequence++);
                }
            }
            finally
            {
                sequencer.Publish(initialSequence, finalSequence);
            }
        }

        private void TranslateAndPublishBatch<A>(IEventTranslatorOneArg<T, A> translator,
            A[] arg0,
            int batchStartsAt, int batchSize, long finalSequence)
        {
            long initialSequence = finalSequence - (batchSize - 1);
            try
            {
                long sequence = initialSequence;
                int batchEndsAt = batchStartsAt + batchSize;
                for (int i = batchStartsAt; i < batchEndsAt; i++)
                {
                    translator.TranslateTo(Get(sequence), sequence++, arg0[i]);
                }
            }
            finally
            {
                sequencer.Publish(initialSequence, finalSequence);
            }
        }

        private void TranslateAndPublishBatch<A, B>(IEventTranslatorTwoArg<T, A, B> translator,
            A[] arg0, B[] arg1,
            int batchStartsAt, int batchSize, long finalSequence)
        {
            long initialSequence = finalSequence - (batchSize - 1);
            try
            {
                long sequence = initialSequence;
                int batchEndsAt = batchStartsAt + batchSize;
                for (int i = batchStartsAt; i < batchEndsAt; i++)
                {
                    translator.TranslateTo(Get(sequence), sequence++, arg0[i], arg1[i]);
                }
            }
            finally
            {
                sequencer.Publish(initialSequence, finalSequence);
            }
        }

        private void TranslateAndPublishBatch<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator,
            A[] arg0, B[] arg1, C[] arg2,
            int batchStartsAt, int batchSize, long finalSequence)
        {
            long initialSequence = finalSequence - (batchSize - 1);
            try
            {
                long sequence = initialSequence;
                int batchEndsAt = batchStartsAt + batchSize;
                for (int i = batchStartsAt; i < batchEndsAt; i++)
                {
                    translator.TranslateTo(Get(sequence), sequence++, arg0[i], arg1[i], arg2[i]);
                }
            }
            finally
            {
                sequencer.Publish(initialSequence, finalSequence);
            }
        }

        private void TranslateAndPublishBatch(IEventTranslatorVararg<T> translator,
            int batchStartsAt, int batchSize, long finalSequence,
            params Object[][] args)
        {
            long initialSequence = finalSequence - (batchSize - 1);
            try
            {
                long sequence = initialSequence;
                int batchEndsAt = batchStartsAt + batchSize;
                for (int i = batchStartsAt; i < batchEndsAt; i++)
                {
                    translator.TranslateTo(Get(sequence), sequence++, args[i]);
                }
            }
            finally
            {
                sequencer.Publish(initialSequence, finalSequence);
            }
        }

        private void CheckBounds<A>(A[] arg0, int batchStartsAt, int batchSize)
        {
            CheckBatchSizing(batchStartsAt, batchSize);
            BatchOverRuns(arg0, batchStartsAt, batchSize);
        }

        private void CheckBounds<A, B>(A[] arg0, B[] arg1, int batchStartsAt, int batchSize)
        {
            CheckBatchSizing(batchStartsAt, batchSize);
            BatchOverRuns(arg0, batchStartsAt, batchSize);
            BatchOverRuns(arg1, batchStartsAt, batchSize);
        }

        private void CheckBounds<A, B, C>(A[] arg0, B[] arg1, C[] arg2, int batchStartsAt, int batchSize)
        {
            CheckBatchSizing(batchStartsAt, batchSize);
            BatchOverRuns(arg0, batchStartsAt, batchSize);
            BatchOverRuns(arg1, batchStartsAt, batchSize);
            BatchOverRuns(arg2, batchStartsAt, batchSize);
        }

        private void CheckBounds(int batchStartsAt, int batchSize, Object[][] args)
        {
            CheckBatchSizing(batchStartsAt, batchSize);
            BatchOverRuns(args, batchStartsAt, batchSize);
        }

        //TODO:2.0没有
        //private void CheckBounds(IEventTranslator<T>[] translators, int batchStartsAt, int batchSize)
        //{
        //    CheckBatchSizing(batchStartsAt, batchSize);
        //    BatchOverRuns(translators, batchStartsAt, batchSize);
        //}

        private void CheckBatchSizing(int batchStartsAt, int batchSize)
        {
            if (batchStartsAt < 0 || batchSize < 0)
            {
                throw new IllegalArgumentException("Both batchStartsAt and batchSize must be positive but got: batchStartsAt " + batchStartsAt + " and batchSize " + batchSize);
            }
            else if (batchSize > bufferSize)
            {
                throw new IllegalArgumentException("The ring buffer cannot accommodate " + batchSize + " it only has space for " + bufferSize + " entities.");
            }
        }

        private void BatchOverRuns<A>(A[] arg0, int batchStartsAt, int batchSize)
        {
            if (batchStartsAt + batchSize > arg0.Length)
            {
                throw new IllegalArgumentException(
                    "A batchSize of: " + batchSize +
                        " with batchStatsAt of: " + batchStartsAt +
                        " will overrun the available number of arguments: " + (arg0.Length - batchStartsAt));
            }
        }

        #endregion

        public override String ToString()
        {
            return "RingBuffer{" +
                "bufferSize=" + bufferSize +
                ", sequencer=" + sequencer +
                "}";
        }

    }
}
