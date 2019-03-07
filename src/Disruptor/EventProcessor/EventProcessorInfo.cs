using Disruptor.Core;
using System;

namespace Disruptor.Dsl
{
    /// <summary>
    /// Wrapper class to tie together a particular event processing stage.
    /// Tracks the event processor instance, the event handler instance, and sequence barrier which the stage is attached to.
    /// </summary>
    /// <typeparam name="T">the type of the configured <see cref="IEventHandler{T}"/>.</typeparam>
    public class EventProcessorInfo<T> : IConsumerInfo
    {
        /// <summary>
        /// 数据处理线程
        /// </summary>
        private readonly IEventProcessor eventprocessor;
        /// <summary>
        /// 真是数据处理的地方
        /// </summary>
        private readonly IEventHandler<T> handler;
        /// <summary>
        /// 序列栅栏，用来追踪生产者的序列，以及依赖的前一个消费组的序列组
        /// </summary>
        private readonly ISequenceBarrier barrier;
        /// <summary>
        /// 
        /// </summary>
        private Boolean endOfChain = true;

        /// <summary>
        /// EventProcessorInfo
        /// </summary>
        /// <param name="eventprocessor"></param>
        /// <param name="handler"></param>
        /// <param name="barrier"></param>
        public EventProcessorInfo(IEventProcessor eventprocessor, IEventHandler<T> handler, ISequenceBarrier barrier)
        {
            this.eventprocessor = eventprocessor;
            this.handler = handler;
            this.barrier = barrier;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEventProcessor GetEventProcessor()
        {
            return eventprocessor;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEventHandler<T> GetHandler()
        {
            return handler;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ISequence[] GetSequences()
        {
            return new ISequence[] { eventprocessor.GetSequence() };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ISequenceBarrier GetBarrier()
        {
            return barrier;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Boolean IsEndOfChain()
        {
            return endOfChain;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="executor"></param>
        public void Start(IExecutor executor)
        {
            executor.Execute(eventprocessor);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Halt()
        {
            eventprocessor.Halt();
        }

        /// <summary>
        /// 
        /// </summary>
        public void MarkAsUsedInBarrier()
        {
            endOfChain = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Boolean IsRunning()
        {
            return eventprocessor.IsRunning();
        }

    }
}
