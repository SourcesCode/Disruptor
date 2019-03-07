using System;
using System.Linq;

namespace Disruptor
{
    /// <summary>
    /// Hides a group of Sequences behind a single Sequence.
    /// </summary>
    public sealed class FixedSequenceGroup : Sequence
    {
        /// <summary>
        /// sequences
        /// </summary>
        private readonly ISequence[] sequences;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sequences">the list of sequences to be tracked under this sequence group</param>
        public FixedSequenceGroup(ISequence[] sequences)
        {
            this.sequences = new ISequence[sequences.Length];
            Array.Copy(sequences, this.sequences, sequences.Length);
            //Arrays.copyOf(sequences, sequences.length);
            //this.sequences = sequences.ToArray();
        }

        /// <summary>
        /// Get the minimum sequence value for the group.
        /// </summary>
        /// <returns></returns>
        public override long Get()
        {
            return Util.GetMinimumSequence(sequences);
        }

        /// <summary>
        /// Set
        /// </summary>
        /// <param name="value"></param>
        public override void Set(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// SetVolatile
        /// </summary>
        /// <param name="value"></param>
        public override void SetVolatile(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// CompareAndSet
        /// </summary>
        /// <param name="expectedValue"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public override Boolean CompareAndSet(long expectedValue, long newValue)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// IncrementAndGet
        /// </summary>
        /// <returns></returns>
        public override long IncrementAndGet()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// AddAndGet
        /// </summary>
        /// <param name="increment"></param>
        /// <returns></returns>
        public override long AddAndGet(long increment)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override String ToString()
        {
            //return Arrays.toString(sequences);
            return string.Join(", ", sequences.Select(t => t.ToString()));
        }

    }
}
