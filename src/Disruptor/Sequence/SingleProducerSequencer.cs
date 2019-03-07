using Disruptor.Core;
using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Coordinator for claiming sequences for access to a data structure while tracking dependent <see cref="ISequence"/>s.
    /// Generally not safe for use from multiple threads as it does not implement any barriers.
    /// 
    /// </summary>
    /// <remarks>
    /// Note on <see cref="ICursored.GetCursor"/>: With this sequencer the cursor value is updated after the call
    /// to <see cref="ISequenced.Publish(long)"/> is made.
    /// </remarks>
    public class SingleProducerSequencer : SingleProducerSequencerFields
    {
        /// <summary>
        /// P8, P9, P10, P11, P12, P13, P14
        /// </summary>
        protected long P8, P9, P10, P11, P12, P13, P14;

        /// <summary>
        /// Construct a Sequencer with the selected wait strategy and buffer size.
        /// </summary>
        /// <param name="bufferSize">the size of the buffer that this will sequence over.</param>
        /// <param name="waitStrategy">for those waiting on sequences.</param>
        public SingleProducerSequencer(int bufferSize, IWaitStrategy waitStrategy)
            : base(bufferSize, waitStrategy)
        {
        }

        /// <summary>
        /// @see Sequencer#claim(long)
        /// </summary>
        /// <param name="sequence"></param>
        public override void Claim(long sequence)
        {
            this.nextValue = sequence;
        }

        /// <summary>
        /// @see Sequencer#isAvailable(long)
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public override Boolean IsAvailable(long sequence)
        {
            return sequence <= cursor.Get();
        }

        /// <summary>
        /// GetHighestPublishedSequence
        /// </summary>
        /// <param name="lowerBound"></param>
        /// <param name="availableSequence"></param>
        /// <returns></returns>
        public override long GetHighestPublishedSequence(long lowerBound, long availableSequence)
        {
            return availableSequence;
        }

        /// <summary>
        /// @see Sequencer#hasAvailableCapacity(int)
        /// </summary>
        /// <param name="requiredCapacity"></param>
        /// <returns></returns>
        public override Boolean HasAvailableCapacity(int requiredCapacity)
        {
            return HasAvailableCapacity(requiredCapacity, false);
        }

        /// <summary>
        /// @see Sequencer#remainingCapacity()
        /// </summary>
        /// <returns></returns>
        public override long RemainingCapacity()
        {
            long nextValue = this.nextValue;

            long consumed = Util.GetMinimumSequence(sequencesRef.ReadCompilerOnlyFence(), nextValue);
            long produced = nextValue;
            return GetBufferSize() - (produced - consumed);
        }

        /// <summary>
        /// @see Sequencer#next()
        /// </summary>
        /// <returns></returns>
        public override long Next()
        {
            return Next(1);
        }

        /// <summary>
        /// @see Sequencer#next(int)
        /// 从逻辑来看，当生产者想申请某一个序列时，
        /// 需要保证不会绕一圈之后，对消费者追尾；
        /// 同时需要保证消费者上一次的消费最小序列没有对生产者追尾。
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public override long Next(int n)
        {
            if (n < 1 || n > bufferSize)
            {
                throw new IllegalArgumentException("n must be > 0 and < bufferSize");
            }

            //复制上次成功申请的序列
            long nextValue = this.nextValue;
            //加上n后，得到本次需要申请的序列
            long nextSequence = nextValue + n;
            //本次申请的序列减去环形数组的长度，得到绕一圈后的序列
            long wrapPoint = nextSequence - bufferSize;
            //复制消费者上次消费到的序列位置
            long cachedGatingSequence = this.cachedValue;

            //如果本次申请的序列，绕一圈后，从消费者后面追上，或者消费者上次消费的序列大于生产者上次申请的序列，则说明发生追尾了，需要进一步处理
            if (wrapPoint > cachedGatingSequence || cachedGatingSequence > nextValue)
            {
                cursor.SetVolatile(nextValue);  // StoreLoad fence

                long minSequence;
                var spinWait = default(AggressiveSpinWait);
                //循环判断生产者绕一圈之后，没有追上消费者的最小序列，如果还是追尾，则等待1纳秒，目前就是简单的等待，看注释是想在以后通过waitStrategy来等待
                while (wrapPoint > (minSequence = Util.GetMinimumSequence(sequencesRef.ReadCompilerOnlyFence(), nextValue)))
                {
                    // TODO: Use waitStrategy to spin?
                    //LockSupport.parkNanos(1);
                    spinWait.SpinOnce();
                }
                //循环退出后，将获取的消费者最小序列，赋值给cachedValue
                this.cachedValue = minSequence;
            }
            //将成功申请到的nextSequence赋值给nextValue
            this.nextValue = nextSequence;

            return nextSequence;
        }

        /// <summary>
        /// @see Sequencer#tryNext()
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InsufficientCapacityException"></exception>
        public override long TryNext()
        {
            return TryNext(1);
        }

        /// <summary>
        /// @see Sequencer#tryNext(int)
        /// 利用了hasAvailableCapacity，非阻塞的。
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        /// <exception cref="InsufficientCapacityException"></exception>
        public override long TryNext(int n)
        {
            if (n < 1)
            {
                throw new IllegalArgumentException("n must be > 0");
            }

            if (!HasAvailableCapacity(n, true))
            {
                throw InsufficientCapacityException.INSTANCE;
            }

            long nextSequence = this.nextValue += n;

            return nextSequence;
        }

        /// <summary>
        /// @see Sequencer#publish(long)
        /// </summary>
        /// <param name="sequence"></param>
        public override void Publish(long sequence)
        {
            //首先更新cursor的值
            cursor.Set(sequence);
            //然后通过waitStrategy换新在等待的线程。
            waitStrategy.SignalAllWhenBlocking();
        }

        /// <summary>
        /// @see Sequencer#publish(long, long)
        /// </summary>
        /// <param name="lo"></param>
        /// <param name="hi"></param>
        public override void Publish(long lo, long hi)
        {
            Publish(hi);
        }

        /// <summary>
        /// HasAvailableCapacity
        /// </summary>
        /// <param name="requiredCapacity"></param>
        /// <param name="doStore"></param>
        /// <returns></returns>
        private Boolean HasAvailableCapacity(int requiredCapacity, Boolean doStore)
        {
            long nextValue = this.nextValue;

            long wrapPoint = (nextValue + requiredCapacity) - bufferSize;
            long cachedGatingSequence = this.cachedValue;

            if (wrapPoint > cachedGatingSequence || cachedGatingSequence > nextValue)
            {
                if (doStore)
                {
                    cursor.SetVolatile(nextValue);  // StoreLoad fence
                }

                long minSequence = Util.GetMinimumSequence(sequencesRef.ReadCompilerOnlyFence(), nextValue);
                this.cachedValue = minSequence;

                if (wrapPoint > minSequence)
                {
                    return false;
                }
            }

            return true;
        }

    }
}
