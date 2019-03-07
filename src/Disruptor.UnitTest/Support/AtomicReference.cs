using System;
using System.Collections.Generic;
using System.Text;

namespace Disruptor.UnitTest.Support
{
    public class AtomicReference<T> where T : class
    {
        private volatile T _reference;

        public AtomicReference(T reference = null)
        {
            _reference = reference;
        }

        public T Read()
        {
            return _reference;
        }

        public void Write(T value)
        {
            _reference = value;
        }
    }
}
