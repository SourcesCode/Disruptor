using System;
using System.Collections.Generic;
using System.Text;

namespace Disruptor.UnitTest.Demos.Demo1
{
    /// <summary>
    /// 队列中的元素
    /// </summary>
    public class LongEvent
    {

        private long value;

        public long get()
        {
            return value;
        }

        public void set(long value)
        {
            this.value = value;
        }

    }
}
