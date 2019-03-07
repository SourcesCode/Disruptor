using Disruptor.Core;
using System;

namespace Disruptor
{
    /// <summary>
    /// A <see cref="Sequence"/> group that can dynamically have <see cref="Sequence"/>s added and removed while being
    /// thread safe.
    /// 
    /// The <see cref="Get()"/> and <see cref="Set(long)"/> methods are lock free and can be
    /// concurrently be called with the <see cref="Add(ISequence)"/> and <see cref="Remove(ISequence)"/>.</p>
    /// </summary>
    public sealed class SequenceGroup : Sequence
    {
        private _Volatile.AtomicReference<ISequence[]> sequencesAtomicReference;
        /// <summary>
        /// Volatile in the Java version => 
        /// always use Volatile.Read/Write or Interlocked methods to access this field.
        /// </summary>
        private ISequence[] sequences = new ISequence[0];

        /// <summary>
        /// Default Constructor
        /// </summary>
        public SequenceGroup()
            : base(-1L)
        {
            sequencesAtomicReference = new _Volatile.AtomicReference<ISequence[]>(sequences);
        }

        /// <summary>
        /// Get the minimum sequence value for the group.
        /// </summary>
        /// <returns></returns>
        public override long Get()
        {
            return Util.GetMinimumSequence(sequencesAtomicReference.ReadFullFence());
        }

        /// <summary>
        /// Set all <see cref="ISequence"/>s in the group to a given value.
        /// </summary>
        /// <param name="value">value to set the group of sequences to.</param>
        public override void Set(long value)
        {
            ISequence[] sequences = sequencesAtomicReference.ReadFullFence();
            foreach (ISequence sequence in sequences)
            {
                sequence.Set(value);
            }
        }

        /// <summary>
        /// Performs a volatile write of this sequence. The intent is a Store/Store barrier between this write and any previous
        /// write and a Store/Load barrier between this write and any subsequent volatile read.
        /// </summary>
        /// <param name="value"></param>
        public override void SetVolatile(long value)
        {
            ISequence[] sequences = sequencesAtomicReference.ReadFullFence();
            foreach (ISequence sequence in sequences)
            {
                sequence.SetVolatile(value);
            }
        }

        /// <summary>
        /// Add a <see cref="ISequence"/> into this aggregate. This should only be used during
        /// initialisation. Use <see cref="AddWhileRunning(ICursored, ISequence)"/>.
        /// </summary>
        /// <param name="sequence">to be added to the aggregate.</param>
        public void Add(ISequence sequence)
        {
            ISequence[] oldSequences;
            ISequence[] newSequences;
            do
            {
                oldSequences = sequencesAtomicReference.ReadFullFence();
                int oldSize = oldSequences.Length;
                newSequences = new ISequence[oldSize + 1];
                Array.Copy(oldSequences, 0, newSequences, 0, oldSize);
                newSequences[oldSize] = sequence;
            }
            while (!sequencesAtomicReference.AtomicCompareExchange(newSequences, oldSequences));
        }

        /// <summary>
        /// Remove the first occurrence of the <see cref="ISequence"/> from this aggregate.
        /// </summary>
        /// <param name="sequence">to be removed from this aggregate.</param>
        /// <returns>true if the sequence was removed otherwise false.</returns>
        public Boolean Remove(ISequence sequence)
        {
            return SequenceGroups.RemoveSequence(this, sequencesAtomicReference, sequence);
        }

        /// <summary>
        /// Get the size of the group.
        /// </summary>
        public int Size()
        {
            return sequencesAtomicReference.ReadFullFence().Length;
        }

        /// <summary>
        /// Adds a sequence to the sequence group after threads have started to Publish to
        /// the Disruptor. It will set the sequences to cursor value of the ringBuffer
        /// just after adding them.  This should prevent any nasty rewind/wrapping effects.
        /// </summary>
        /// <param name="cursored">The data structure that the owner of this sequence group will be pulling it's events from.</param>
        /// <param name="sequence">The sequence to add.</param>
        public void AddWhileRunning(ICursored cursored, ISequence sequence)
        {
            SequenceGroups.AddSequences(this, sequencesAtomicReference, cursored, sequence);
        }

    }
}
