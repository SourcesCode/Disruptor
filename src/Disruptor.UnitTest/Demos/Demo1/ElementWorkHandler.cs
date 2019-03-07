using System;
using System.Collections.Generic;
using System.Text;

namespace Disruptor.UnitTest.Demos.Demo1
{
    /// <summary>
    /// 类型2：多个消费者，每个消费者竞争消费不同数据。
    /// 也就是说每个消费者竞争数据，竞争到消费，其他消费者没有机会。
    /// 使用handleEventsWithWorkerPool方法。
    /// disruptor.HandleEventsWithWorkerPool(new ElementWorkHandler(1),new ElementWorkHandler(2));
    /// </summary>
    public class ElementWorkHandler : IWorkHandler<LongEvent>
    {
        private int _id;
        public ElementWorkHandler()
            : this(1)
        {
        }

        public ElementWorkHandler(int id)
        {
            this._id = id;
        }

        public void OnEvent(LongEvent @event)
        {
            Console.WriteLine($"HandlerId: {_id},Element: {@event.get()}");
        }

    }
}
