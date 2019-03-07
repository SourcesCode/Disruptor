using Disruptor.Core;
using System;

namespace Disruptor
{
    /// <summary>
    /// Provides static methods for managing a <see cref="SequenceGroup"/> object.
    /// 用来实现对追踪序列组gatingSequences进行原子操作，实现增加、删减，主要都是利用SEQUENCE_UNDATER变量实现。
    /// </summary>
    internal class SequenceGroups
    {
        /// <summary>
        /// AddSequences
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="holder"></param>
        /// <param name="updater"></param>
        /// <param name="cursor"></param>
        /// <param name="sequencesToAdd"></param>
        internal static void AddSequences<T>(
            T holder,
            _Volatile.AtomicReference<ISequence[]> updater,
            ICursored cursor,
            params ISequence[] sequencesToAdd)
        {
            long cursorSequence;
            ISequence[] updatedSequences;
            ISequence[] currentSequences;

            do
            {
                currentSequences = updater.ReadFullFence();
                updatedSequences = Util.CopyToNewArray(currentSequences, currentSequences.Length + sequencesToAdd.Length);
                cursorSequence = cursor.GetCursor();

                int index = currentSequences.Length;
                foreach (Sequence sequence in sequencesToAdd)
                {
                    sequence.Set(cursorSequence);
                    updatedSequences[index++] = sequence;
                }
            }
            while (!updater.AtomicCompareExchange(updatedSequences, currentSequences));
            cursorSequence = cursor.GetCursor();
            foreach (ISequence sequence in sequencesToAdd)
            {
                sequence.Set(cursorSequence);
            }
        }

        /// <summary>
        /// RemoveSequence
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="holder"></param>
        /// <param name="sequenceUpdater"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        internal static Boolean RemoveSequence<T>(
            T holder,
            _Volatile.AtomicReference<ISequence[]> sequenceUpdater,
            ISequence sequence)
        {
            int numToRemove;
            ISequence[] oldSequences;
            ISequence[] newSequences;

            do
            {
                oldSequences = sequenceUpdater.ReadFullFence();
                numToRemove = CountMatching(oldSequences, sequence);

                if (0 == numToRemove)
                {
                    break;
                }

                int oldSize = oldSequences.Length;
                newSequences = new ISequence[oldSize - numToRemove];

                for (int i = 0, pos = 0; i < oldSize; i++)
                {
                    ISequence testSequence = oldSequences[i];
                    if (sequence != testSequence)
                    {
                        newSequences[pos++] = testSequence;
                    }
                }
            }
            while (!sequenceUpdater.AtomicCompareExchange(newSequences, oldSequences));

            return numToRemove != 0;
        }

        /// <summary>
        /// CountMatching
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="toMatch"></param>
        /// <returns></returns>
        private static int CountMatching<T>(T[] values, T toMatch)
        {
            int numToRemove = 0;
            foreach (T value in values)
            {
                //if (value == toMatch) // Specifically uses identity
                if (value.Equals(toMatch))
                {
                    numToRemove++;
                }
            }
            return numToRemove;
        }

    }
}
