using System;

namespace Disruptor
{
    /// <summary>
    /// Set of common functions used by the Disruptor.
    /// </summary>
    public sealed class Util
    {
        /// <summary>
        /// Calculate the next power of 2, greater than or equal to x.
        /// 
        /// From Hacker's Delight, Chapter 3, Harry S. Warren Jr.
        /// </summary>
        /// <param name="x">Value to round up</param>
        /// <returns>The next power of 2 from x inclusive</returns>
        public static int CeilingNextPowerOfTwo(int x)
        {
            //return 1 << (32 - Integer.numberOfLeadingZeros(x - 1));
            int result = 2;

            while (result < x)
            {
                result <<= 1;
            }

            return result;
        }

        /// <summary>
        /// Get the minimum sequence from an array of <see cref="Sequence"/>s.
        /// </summary>
        /// <param name="sequences">to compare.</param>
        /// <returns>the minimum sequence found or Long.MAX_VALUE if the array is empty.</returns>
        public static long GetMinimumSequence(ISequence[] sequences)
        {
            return GetMinimumSequence(sequences, long.MaxValue);
        }

        /// <summary>
        /// Get the minimum sequence from an array of <see cref="Sequence"/>s.
        /// </summary>
        /// <param name="sequences">to compare.</param>
        /// <param name="minimum">an initial default minimum.  If the array is empty this value will be returned.</param>
        /// <returns>the smaller of minimum sequence value found in {@code sequences} and {@code minimum};{@code minimum} if {@code sequences} is empty.</returns>
        public static long GetMinimumSequence(ISequence[] sequences, long minimum)
        {
            for (int i = 0, n = sequences.Length; i < n; i++)
            {
                long value = sequences[i].Get();
                minimum = Math.Min(minimum, value);
            }

            return minimum;
        }

        /// <summary>
        /// Get an array of <see cref="Sequence"/>s for the passed <see cref="IEventProcessor"/>s.
        /// </summary>
        /// <param name="processors">for which to get the sequences.</param>
        /// <returns>the array of <see cref="Sequence"/>s.</returns>
        public static ISequence[] GetSequencesFor(params IEventProcessor[] processors)
        {
            ISequence[] sequences = new ISequence[processors.Length];
            for (int i = 0; i < sequences.Length; i++)
            {
                sequences[i] = processors[i].GetSequence();
            }

            return sequences;
        }

        //private static final Unsafe THE_UNSAFE;

        //static
        //{
        //try
        //{
        //    final PrivilegedExceptionAction<Unsafe> action = new PrivilegedExceptionAction<Unsafe>()
        //    {
        //        public Unsafe run() throws Exception
        //{
        //    Field theUnsafe = Unsafe.class.getDeclaredField("theUnsafe");
        //theUnsafe.setAccessible(true);
        //            return (Unsafe) theUnsafe.get(null);
        //}
        //};

        //THE_UNSAFE = AccessController.doPrivileged(action);
        //}
        //catch (Exception e)
        //{
        //    throw new RuntimeException("Unable to load unsafe", e);
        //}
        //}

        ///**
        //* Get a handle on the Unsafe instance, used for accessing low-level concurrency
        //* and memory constructs.
        //*
        //* @return The Unsafe
        //*/
        //public static Unsafe getUnsafe()
        //{
        //return THE_UNSAFE;
        //}

        /// <summary>
        /// Calculate the log base 2 of the supplied integer, essentially reports the location of the highest bit.
        /// </summary>
        /// <param name="i">Value to calculate log2 for.</param>
        /// <returns>The log2 value.</returns>
        public static int Log2(int i)
        {
            int r = 0;
            while ((i >>= 1) != 0)
            {
                ++r;
            }
            return r;
        }

        /// <summary>
        /// CopyToNewArray
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="original"></param>
        /// <param name="newLength"></param>
        /// <returns></returns>
        public static T[] CopyToNewArray<T>(T[] original, int newLength)
        {
            T[] newArray = typeof(T) == typeof(object) ? new object[newLength] as T[]
                            : Array.CreateInstance(typeof(T), newLength) as T[];

            var copyLen = Math.Min(original.Length, newLength);

            Array.Copy(original, 0, newArray, 0, copyLen);
            return newArray;
        }

        /// <summary>
        /// AwaitNanos
        /// </summary>
        /// <param name="mutex"></param>
        /// <param name="timeoutNanos"></param>
        /// <returns></returns>
        public static long AwaitNanos(Object mutex, long timeoutNanos)
        {
            //long millis = timeoutNanos / 1_000_000;
            //long nanos = timeoutNanos % 1_000_000;

            //long t0 = System.nanoTime();
            //mutex.wait(millis, (int)nanos);
            //long t1 = System.nanoTime();

            //return timeoutNanos - (t1 - t0);
            return 0;
        }

    }
}
