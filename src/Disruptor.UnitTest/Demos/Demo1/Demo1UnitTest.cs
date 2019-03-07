using Disruptor.Core;
using Disruptor.Dsl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Disruptor.UnitTest.Demos.Demo1
{
    /// <summary>
    /// 每10ms向disruptor中插入一个元素，消费者读取数据，并打印到终端。
    /// </summary>
    [TestClass]
    public class Demo1UnitTest
    {
        [TestMethod]
        public void TestPublishEvent()
        {
            Disruptor<LongEvent> disruptor = GetDisruptor();

            //PublishEvent1(disruptor);
            PublishEvent2(disruptor);


        }

        private Disruptor<LongEvent> GetDisruptor()
        {
            // 生产者的线程工厂
            ThreadFactory threadFactory = new ThreadFactory();
            // RingBuffer生产工厂,初始化RingBuffer的时候使用
            ElementEventFactory elementEventFactory = new ElementEventFactory();
            // 处理Event的handler
            ElementEventHandler elementEventHandler = new ElementEventHandler();
            // 阻塞策略
            BlockingWaitStrategy strategy = new BlockingWaitStrategy();
            // 指定RingBuffer的大小
            int bufferSize = 16;
            // 创建disruptor，采用单生产者模式
            Disruptor<LongEvent> disruptor = new Disruptor<LongEvent>(elementEventFactory, bufferSize, threadFactory, ProducerType.SINGLE, strategy);

            // 设置EventHandler
            disruptor.HandleEventsWith(elementEventHandler);
            //disruptor.HandleEventsWith(new ElementEventHandler(1), new ElementEventHandler(2));
            //disruptor.HandleEventsWithWorkerPool(new ElementWorkHandler(1), new ElementWorkHandler(2));


            // 启动disruptor的线程
            disruptor.Start();

            return disruptor;
        }

        private void PublishEvent1(Disruptor<LongEvent> disruptor)
        {

            RingBuffer<LongEvent> ringBuffer = disruptor.GetRingBuffer();

            for (int l = 0; true; l++)
            {
                // 获取下一个可用位置的下标
                long sequence = ringBuffer.Next();
                try
                {
                    // 返回可用位置的元素
                    LongEvent @event = ringBuffer.Get(sequence);
                    // 设置该位置元素的值
                    @event.set(l);
                }
                finally
                {
                    ringBuffer.Publish(sequence);
                }
                Thread.Sleep(10);
            }

            //切记：一定要在设置值的地方加上
            //否则如果数据发布不成功，最后数据会逐渐填满ringbuffer,最后后面来的数据根本没有办法调用可用空间，导致方法阻塞，占用CPU和内存，无法释放资源，最后导致服务器死机
            //注意，最后的 ringBuffer.publish 方法必须包含在 finally 中以确保必须得到调用；如果某个请求的 sequence 未被提交，将会堵塞后续的发布操作或者其它的 producer。

        }

        public void PublishEvent2(Disruptor<LongEvent> disruptor)
        {
            Translator translator = new Translator();
            // 发布事件；
            RingBuffer<LongEvent> ringBuffer = disruptor.GetRingBuffer();
            long data = 1;//getEventDataxxxx();//获取要通过事件传递的业务数据；    
            ringBuffer.PublishEvent(translator, data);
        }

    }
}
