using System;
using System.Threading;

namespace Disruptor.Core
{
    public class AtomicBoolean
    {
        //private static readonly unsafe _unsafe = new unsafe ();

        private volatile int value;
        public AtomicBoolean()
        {

        }
        public AtomicBoolean(Boolean initialValue)
        {
            value = initialValue ? 1 : 0;
        }
        public Boolean get()
        {
            return value != 0;
        }

        /**
        * Atomically sets to the given value and returns the previous value.
        *
        * @param newValue the new value
        * @return the previous value
        */
        public Boolean getAndSet(Boolean newValue)
        {
            Boolean prev;
            do
            {
                prev = get();
            } while (!compareAndSet(prev, newValue));
            return prev;
        }

        /**
        * Atomically sets the value to the given updated value
        * if the current value {@code ==} the expected value.
        *
        * @param expect the expected value
        * @param update the new value
        * @return {@code true} if successful. False return indicates that
        * the actual value was not equal to the expected value.
        */
        public Boolean compareAndSet(Boolean expect, Boolean update)
        {
            int e = expect ? 1 : 0;
            int u = update ? 1 : 0;

            return Interlocked.CompareExchange(ref value, e, u) == u;

            //return unsafe.compareAndSwapInt(this, valueOffset, e, u);
        }

        /**
        * Unconditionally sets to the given value.
        *
        * @param newValue the new value
        */
        public void set(Boolean newValue)
        {
            value = newValue ? 1 : 0;
        }



    }
}
