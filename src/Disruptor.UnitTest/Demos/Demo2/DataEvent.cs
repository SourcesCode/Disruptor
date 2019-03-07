using System;
using System.Collections.Generic;
using System.Text;

namespace Disruptor.UnitTest.Demos.Demo2
{
    public class DataEvent<T>
    {
        public T Data { get; set; }

        public T CopyOfData()
        {
            // Copy the data out here.  In this case we have a single reference object, so the pass by
            // reference is sufficient.  But if we were reusing a byte array, then we would need to copy
            // the actual contents.
            return Data;
        }
        void Set(T d)
        {
            Data = d;
        }
    }

    public class DataEventEventFactory<T> : IEventFactory<DataEvent<T>>
    {
        public DataEvent<T> NewInstance()
        {
            return new DataEvent<T>();
        }

    }


}
