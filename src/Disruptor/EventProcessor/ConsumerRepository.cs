using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Disruptor.Dsl
{
    /// <summary>
    /// Provides a repository mechanism to associate <see cref="IEventHandler{T}"/>s with <see cref="IEventProcessor"/>s.
    /// 
    /// ConsumerRepository是消费者仓库，在ConsumerInfo的上层又做了一层封装
    /// </summary>
    /// <typeparam name="T">the type of the {@link EventHandler}</typeparam>
    public class ConsumerRepository<T> : IEnumerable<IConsumerInfo> where T : class
    {
        /// <summary>
        /// 专门用来存放每个EventHandler对应的EventProcessorInfo
        /// </summary>
        private readonly IDictionary<IEventHandler<T>, EventProcessorInfo<T>> eventProcessorInfoByEventHandler;
        //new IdentityHashMap<EventHandler<?>, EventProcessorInfo<T>>();

        /// <summary>
        /// 存放每个Sequence对应的ConsumerInfo，此时可能是EventProcessorInfo或WorkPoolInfo
        /// </summary>
        private readonly IDictionary<ISequence, IConsumerInfo> eventProcessorInfoBySequence;
        //new IdentityHashMap<Sequence, ConsumerInfo>();

        /// <summary>
        /// 用来存放所有的ConsumerInfo
        /// </summary>
        private readonly IList<IConsumerInfo> consumerInfos;

        public ConsumerRepository()
        {
            eventProcessorInfoByEventHandler = new Dictionary<IEventHandler<T>, EventProcessorInfo<T>>(new IdentityComparer<IEventHandler<T>>());
            eventProcessorInfoBySequence = new Dictionary<ISequence, IConsumerInfo>(new IdentityComparer<ISequence>());
            consumerInfos = new List<IConsumerInfo>();
        }

        public void Add(IEventProcessor eventprocessor, IEventHandler<T> handler, ISequenceBarrier barrier)
        {
            EventProcessorInfo<T> consumerInfo = new EventProcessorInfo<T>(eventprocessor, handler, barrier);
            //eventProcessorInfoByEventHandler.Add(handler, consumerInfo);
            //eventProcessorInfoBySequence.Add(eventprocessor.GetSequence(), consumerInfo);
            eventProcessorInfoByEventHandler[handler] = consumerInfo;
            eventProcessorInfoBySequence[eventprocessor.GetSequence()] = consumerInfo;
            consumerInfos.Add(consumerInfo);
        }

        public void Add(IEventProcessor processor)
        {
            EventProcessorInfo<T> consumerInfo = new EventProcessorInfo<T>(processor, null, null);
            //eventProcessorInfoBySequence.Add(processor.GetSequence(), consumerInfo);
            eventProcessorInfoBySequence[processor.GetSequence()] = consumerInfo;
            consumerInfos.Add(consumerInfo);
        }

        public void Add(WorkerPool<T> workerPool, ISequenceBarrier sequenceBarrier)
        {
            WorkerPoolInfo<T> workerPoolInfo = new WorkerPoolInfo<T>(workerPool, sequenceBarrier);
            consumerInfos.Add(workerPoolInfo);
            foreach (Sequence sequence in workerPool.GetWorkerSequences())
            {
                //eventProcessorInfoBySequence.Add(sequence, workerPoolInfo);
                eventProcessorInfoBySequence[sequence] = workerPoolInfo;
            }
        }

        /// <summary>
        /// 获取序列链上的最后一个序列
        /// </summary>
        /// <param name="includeStopped"></param>
        /// <returns></returns>
        public ISequence[] GetLastSequenceInChain(Boolean includeStopped)
        {
            List<ISequence> lastSequence = new List<ISequence>();
            foreach (IConsumerInfo consumerInfo in consumerInfos)
            {
                if ((includeStopped || consumerInfo.IsRunning()) && consumerInfo.IsEndOfChain())
                {
                    ISequence[] sequences = consumerInfo.GetSequences();
                    lastSequence.AddRange(sequences);
                }
            }
            return lastSequence.ToArray();
        }

        /// <summary>
        /// 获取事件处理器
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public IEventProcessor GetEventProcessorFor(IEventHandler<T> handler)
        {
            EventProcessorInfo<T> eventprocessorInfo = GetEventProcessorInfo(handler);
            if (eventprocessorInfo == null)
            {
                throw new ArgumentException("The event handler " + handler + " is not processing events.");
            }

            return eventprocessorInfo.GetEventProcessor();
        }

        public ISequence GetSequenceFor(IEventHandler<T> handler)
        {
            return GetEventProcessorFor(handler).GetSequence();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="barrierEventProcessors"></param>
        public void UnMarkEventProcessorsAsEndOfChain(params ISequence[] barrierEventProcessors)
        {
            if (barrierEventProcessors == null) return;
            foreach (ISequence barrierEventProcessor in barrierEventProcessors)
            {
                GetEventProcessorInfo(barrierEventProcessor)?.MarkAsUsedInBarrier();
            }
        }

        public ISequenceBarrier GetBarrierFor(IEventHandler<T> handler)
        {
            IConsumerInfo consumerInfo = GetEventProcessorInfo(handler);
            return consumerInfo != null ? consumerInfo.GetBarrier() : null;
        }

        public IEnumerator<IConsumerInfo> GetEnumerator()
        {
            return consumerInfos.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private EventProcessorInfo<T> GetEventProcessorInfo(IEventHandler<T> handler)
        {
            EventProcessorInfo<T> result;
            eventProcessorInfoByEventHandler.TryGetValue(handler, out result);
            return result;
        }

        private IConsumerInfo GetEventProcessorInfo(ISequence barrierEventProcessor)
        {
            IConsumerInfo result;
            eventProcessorInfoBySequence.TryGetValue(barrierEventProcessor, out result);
            return result;
        }

        private class IdentityComparer<TKey> : IEqualityComparer<TKey>
        {
            public bool Equals(TKey x, TKey y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(TKey obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }

        }

    }
}
