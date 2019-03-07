using System;
using System.Collections.Generic;
using System.Text;

namespace Disruptor.UnitTest.Demos.Demo1
{
    public class ElementEventFactory : IEventFactory<LongEvent>
    {
        public LongEvent NewInstance()
        {
            return new LongEvent();
        }
    }
}
