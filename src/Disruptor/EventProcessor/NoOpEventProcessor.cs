using Disruptor.Core;
using System;

namespace Disruptor
{
    /// <summary>
    /// No operation version of a <see cref="IEventProcessor"/> that simply tracks a <see cref="ISequence"/>.
    /// 
    /// This is useful in tests or for pre-filling a <see cref="RingBuffer{T}"/> from a publisher.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class NoOpEventProcessor<T> : IEventProcessor
    {
        private readonly SequencerFollowingSequence sequence;
        private _Volatile.Boolean running = new _Volatile.Boolean(false);

        /// <summary>
        /// Construct a <see cref="IEventProcessor"/> that simply tracks a <see cref="ISequence"/> object.
        /// </summary>
        /// <param name="sequencer">to track.</param>
        public NoOpEventProcessor(RingBuffer<T> sequencer)
        {
            sequence = new SequencerFollowingSequence(sequencer);
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
            //TODO:2.0方式
            //running.set(false);
            //Interlocked.Exchange(ref _running, 0);
            running.WriteFullFence(false);
        }

        /// <summary>
        /// Run
        /// </summary>
        public void Run()
        {
            //if (!running.compareAndSet(false, true))
            //TODO:2.0方式
            //if (Interlocked.CompareExchange(ref _running, 0, 1) == 1)
            if (!running.AtomicCompareExchange(true, false))
            {
                throw new IllegalStateException("Thread is already running");
            }
        }

        /// <summary>
        /// IsRunning
        /// </summary>
        /// <returns></returns>
        public Boolean IsRunning()
        {
            //return running.get();
            return running.ReadFullFence();
        }

        /// <summary>
        /// Sequence that follows (by wrapping) another sequence.
        /// </summary>
        private sealed class SequencerFollowingSequence : Sequence
        {
            private readonly RingBuffer<T> _sequencer;

            /// <summary>
            /// SequencerFollowingSequence
            /// </summary>
            /// <param name="sequencer"></param>
            public SequencerFollowingSequence(RingBuffer<T> sequencer)
                : base(Consts.INITIAL_CURSOR_VALUE)
            {
                _sequencer = sequencer;
            }

            /// <summary>
            /// Get
            /// </summary>
            /// <returns></returns>
            public override long Get()
            {
                return _sequencer.GetCursor();
            }

        }

    }
}
