using System;
using System.Collections.Generic;
using System.Text;

namespace Disruptor.UnitTest.Support.EventFactorys
{
    public class ByteArrayEventFactory : IEventFactory<byte[]>
    {
        private readonly int _size;
        public ByteArrayEventFactory(int size)
        {
            _size = size;
        }

        public byte[] NewInstance()
        {
            return new byte[_size];
        }

    }
}
