using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// This class captures possible hints that may be used by some
    /// runtimes to improve code performance. It is intended to capture hinting
    /// behaviours that are implemented in or anticipated to be spec'ed under the
    /// <see cref="Thread"/> class in some Java SE versions, but missing in prior
    /// versions.
    /// </summary>
    public sealed class ThreadHints
    {
        private static readonly RuntimeMethodHandle ON_SPIN_WAIT_METHOD_HANDLE;

        /// <summary>
        /// ThreadHints
        /// </summary>
        static ThreadHints()
        {
            //MethodHandles.Lookup lookup = MethodHandles.lookup();

            //MethodHandle methodHandle = null;
            RuntimeMethodHandle methodHandle = default(RuntimeMethodHandle);
            try
            {
                //methodHandle = lookup.findStatic(Thread.class, "onSpinWait", methodType(void.class));
                methodHandle = new RuntimeMethodHandle();
            }
            catch (Exception)
            {
            }

            ON_SPIN_WAIT_METHOD_HANDLE = methodHandle;
        }

        /// <summary>
        /// ThreadHints
        /// </summary>
        private ThreadHints()
        {
        }

        /// <summary>
        /// Indicates that the caller is momentarily unable to progress, until the
        /// occurrence of one or more actions on the part of other activities.  By
        /// invoking this method within each iteration of a spin-wait loop construct,
        /// the calling thread indicates to the runtime that it is busy-waiting. The runtime
        /// may take action to improve the performance of invoking spin-wait loop constructions.
        /// </summary>
        public static void OnSpinWait()
        {
            // Call java.lang.Thread.onSpinWait() on Java SE versions that support it. Do nothing otherwise.
            // This should optimize away to either nothing or to an inlining of java.lang.Thread.onSpinWait()
            if (null != ON_SPIN_WAIT_METHOD_HANDLE)
            {
                try
                {
                    //ON_SPIN_WAIT_METHOD_HANDLE.invokeExact();
                }
                catch (Exception)
                {
                }
            }
        }

    }
}
