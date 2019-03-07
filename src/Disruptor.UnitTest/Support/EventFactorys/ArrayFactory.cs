using System;
using System.Collections.Generic;
using System.Text;

namespace Disruptor.UnitTest.Support.EventFactorys
{
    public class ArrayFactory : IEventFactory<object[]>
    {
        private readonly int _size;
        public ArrayFactory(int size)
        {
            _size = size;
        }

        public object[] NewInstance()
        {
            return new object[_size];
        }
    }
}
