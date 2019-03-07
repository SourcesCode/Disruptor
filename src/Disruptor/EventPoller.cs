using System;

namespace Disruptor
{
    /// <summary>
    /// Experimental poll-based interface for the Disruptor.
    /// </summary>
    /// <typeparam name="T">Event</typeparam>
    public class EventPoller<T>
    {
        private readonly IDataProvider<T> dataProvider;
        private readonly ISequencer sequencer;
        private readonly ISequence sequence;
        private readonly ISequence gatingSequence;

        /// <summary>
        /// IHandler
        /// </summary>
        /// <typeparam name="TT"></typeparam>
        public interface IHandler<TT>//<in T>
        {
            Boolean OnEvent(TT @event, long sequence, Boolean endOfBatch);
        }

        /// <summary>
        /// PollState
        /// </summary>
        public enum PollState
        {
            PROCESSING, GATING, IDLE
        }

        /// <summary>
        /// EventPoller
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <param name="sequencer"></param>
        /// <param name="sequence"></param>
        /// <param name="gatingSequence"></param>
        public EventPoller(
            IDataProvider<T> dataProvider,
            ISequencer sequencer,
            ISequence sequence,
            ISequence gatingSequence)
        {
            this.dataProvider = dataProvider;
            this.sequencer = sequencer;
            this.sequence = sequence;
            this.gatingSequence = gatingSequence;
        }

        /// <summary>
        /// Poll
        /// </summary>
        /// <param name="eventHandler"></param>
        /// <returns></returns>
        public PollState Poll(IHandler<T> eventHandler)
        {
            long currentSequence = sequence.Get();
            long nextSequence = currentSequence + 1;
            long availableSequence = sequencer.GetHighestPublishedSequence(nextSequence, gatingSequence.Get());

            if (nextSequence <= availableSequence)
            {
                Boolean processNextEvent;
                long processedSequence = currentSequence;

                try
                {
                    do
                    {
                        T @event = dataProvider.Get(nextSequence);
                        processNextEvent = eventHandler.OnEvent(@event, nextSequence, nextSequence == availableSequence);
                        processedSequence = nextSequence;
                        nextSequence++;
                    }
                    while (nextSequence <= availableSequence & processNextEvent);
                }
                finally
                {
                    sequence.Set(processedSequence);
                }

                return PollState.PROCESSING;
            }
            else if (sequencer.GetCursor() >= nextSequence)
            {
                return PollState.GATING;
            }
            else
            {
                return PollState.IDLE;
            }
        }

        /// <summary>
        /// NewInstance
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <param name="sequencer"></param>
        /// <param name="sequence"></param>
        /// <param name="cursorSequence"></param>
        /// <param name="gatingSequences"></param>
        /// <returns></returns>
        public static EventPoller<T> NewInstance(
            IDataProvider<T> dataProvider,
            ISequencer sequencer,
            ISequence sequence,
            ISequence cursorSequence,
            params ISequence[] gatingSequences)
        {
            ISequence gatingSequence;
            if (gatingSequences.Length == 0)
            {
                gatingSequence = cursorSequence;
            }
            else if (gatingSequences.Length == 1)
            {
                gatingSequence = gatingSequences[0];
            }
            else
            {
                gatingSequence = new FixedSequenceGroup(gatingSequences);
            }

            return new EventPoller<T>(dataProvider, sequencer, sequence, gatingSequence);
        }

        /// <summary>
        /// GetSequence
        /// </summary>
        /// <returns></returns>
        public ISequence GetSequence()
        {
            return sequence;
        }

    }
}
