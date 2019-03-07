using System;
using System.Collections.Generic;
using System.Text;

namespace Disruptor.UnitTest.Demos.Demo1
{
    /// <summary>
    /// 类型1：多个消费者每个消费者都有机会消费相同数据，
    /// 使用handleEventsWith方法。
    /// disruptor.HandleEventsWith(new ElementEventHandler(1),new ElementEventHandler(2));
    /// </summary>
    public class ElementEventHandler : IEventHandler<LongEvent>
    {
        private int _id;
        public ElementEventHandler()
            : this(1)
        {
        }

        public ElementEventHandler(int id)
        {
            this._id = id;
        }

        public void OnEvent(LongEvent @event, long sequence, bool endOfBatch)
        {
            Console.WriteLine($"HandlerId: {_id},Element: {@event.get()}");
        }

    }
}
