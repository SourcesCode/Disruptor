using Disruptor.Core;
using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Coordinator for claiming sequences for access to a data structure while tracking dependent <see cref="ISequence"/>s.
    /// Suitable for use for sequencing across multiple publisher threads.
    /// 
    /// Note on <see cref="ICursored.GetCursor"/>: With this sequencer the cursor value is updated after the call
    /// to <see cref="ISequenced.Next"/>, to determine the highest available sequence that can be read, then
    /// <see cref="ISequencer.GetHighestPublishedSequence(long, long)"/> should be used.
    /// </summary>
    public sealed class MultiProducerSequencer : AbstractSequencer
    {
        //private static readonly Unsafe UNSAFE = Util.getUnsafe();
        //private static readonly long BASE = UNSAFE.arrayBaseOffset(int[].class);
        //private static readonly long SCALE = UNSAFE.arrayIndexScale(int[].class);
        private const int BASE = 12;
        private const int SCALE = 4;
        private readonly ISequence gatingSequenceCache = new Sequence(Consts.INITIAL_CURSOR_VALUE);

        ///// <summary>
        ///// availableBuffer tracks the state of each ringbuffer slot
        ///// see below for more details on the approach
        ///// Multi模式与Single模式相比，有一个不同的地方就在于availableBuffer这个整型数组，
        ///// 这个数组大小为bufferSize，用来标识RingBuffer中每个槽位的状态，其中存放的值，
        ///// 就是生产者的每个序列当前绕环形数组的圈数，主要是在publish的时候会更新这个值，
        ///// </summary>
        //private readonly int[] availableBuffer;
        private _Volatile.IntegerArray availableBuffer;
        //private readonly int pendingMask;

        private readonly int indexMask;
        private readonly int indexShift;
        //private _Volatile.Boolean isClaim = new _Volatile.Boolean(false);

        /// <summary>
        /// Construct a Sequencer with the selected wait strategy and buffer size.
        /// </summary>
        /// <param name="bufferSize">the size of the buffer that this will sequence over.</param>
        /// <param name="waitStrategy">for those waiting on sequences.</param>
        public MultiProducerSequencer(int bufferSize, IWaitStrategy waitStrategy)
            : base(bufferSize, waitStrategy)
        {
            //availableBuffer = new int[bufferSize];
            availableBuffer = new _Volatile.IntegerArray(bufferSize);
            //pendingMask = bufferSize - 1;

            indexMask = bufferSize - 1;
            indexShift = Util.Log2(bufferSize);

            InitialiseAvailableBuffer();
        }

        /// <summary>
        /// @see Sequencer#claim(long)
        /// </summary>
        /// <param name="sequence"></param>
        public override void Claim(long sequence)
        {
            //isClaim.WriteReleaseFence(true);
            cursor.Set(sequence);
        }

        /// <summary>
        /// @see Sequencer#isAvailable(long)
        /// 而消费者在获取最大的有效序列时，
        /// 也是通过与availableBuffer的对应序列的值进行比较进行判断
        /// </summary>
        /// <param name="lowerBound"></param>
        /// <param name="availableSequence"></param>
        /// <returns></returns>
        public override Boolean IsAvailable(long sequence)
        {
            int index = CalculateIndex(sequence);
            int flag = CalculateAvailabilityFlag(sequence);
            //TODO
            //long bufferAddress = (index * SCALE) + BASE;
            //return UNSAFE.getIntVolatile(availableBuffer, bufferAddress) == flag;
            //TODO:2.0,使用
            //return Volatile.Read(ref availableBuffer[index]) == flag;
            return availableBuffer.ReadFullFence(index) == flag;
        }

        /// <summary>
        /// GetHighestPublishedSequence
        /// </summary>
        /// <param name="lowerBound"></param>
        /// <param name="availableSequence"></param>
        /// <returns></returns>
        public override long GetHighestPublishedSequence(long lowerBound, long availableSequence)
        {
            for (long sequence = lowerBound; sequence <= availableSequence; sequence++)
            {
                if (!IsAvailable(sequence))
                {
                    return sequence - 1;
                }
            }
            return availableSequence;
        }

        /// <summary>
        /// @see Sequencer#hasAvailableCapacity(int)
        /// </summary>
        /// <param name="requiredCapacity"></param>
        /// <returns></returns>
        public override Boolean HasAvailableCapacity(int requiredCapacity)
        {
            //TODO:2.0,使用ReadCompilerOnlyFence
            return HasAvailableCapacity(sequencesRef.ReadFullFence(), requiredCapacity, cursor.Get());
        }

        /// <summary>
        /// @see Sequencer#remainingCapacity()
        /// </summary>
        /// <returns></returns>
        public override long RemainingCapacity()
        {
            //TODO:2.0,使用ReadCompilerOnlyFence
            long consumed = Util.GetMinimumSequence(sequencesRef.ReadFullFence(), cursor.Get());
            long produced = cursor.Get();
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
        /// 与Single模式的next方法大同小异，但是也有一些不同的地方，我们一起看下
        /// 1、生产者当前的序列是通过cursor来获取的，没有Single模式下的nextValue
        /// 2、出现追尾时候时的处理，与Single模式一致
        /// 3、申请序列成功之后，会设置cursor的值为申请后的值
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public override long Next(int n)
        {
            if (n < 1 || n > bufferSize)
            {
                throw new IllegalArgumentException("n must be > 0 and < bufferSize");
            }

            long current;
            long next;

            //:3.3.0,使用SpinWait
            var spinWait = default(AggressiveSpinWait);
            do
            {
                current = cursor.Get();
                next = current + n;

                long wrapPoint = next - bufferSize;
                long cachedGatingSequence = gatingSequenceCache.Get();

                if (wrapPoint > cachedGatingSequence || cachedGatingSequence > current)
                {
                    //TODO:2.0,使用ReadCompilerOnlyFence
                    long gatingSequence = Util.GetMinimumSequence(sequencesRef.ReadFullFence(), current);
                    // ring buffer is full, must wait for consumption to catch up.
                    if (wrapPoint > gatingSequence)
                    {
                        // TODO, should we spin based on the wait strategy?
                        //LockSupport.parkNanos(1);
                        spinWait.SpinOnce();
                        continue;
                    }

                    gatingSequenceCache.Set(gatingSequence);
                }
                else if (cursor.CompareAndSet(current, next))
                {
                    break;
                }
            }
            while (true);

            return next;
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

            long current;
            long next;

            do
            {
                current = cursor.Get();
                next = current + n;
                //TODO:2.0,使用ReadCompilerOnlyFence
                if (!HasAvailableCapacity(sequencesRef.ReadFullFence(), n, current))
                {
                    throw InsufficientCapacityException.INSTANCE;
                }
            }
            while (!cursor.CompareAndSet(current, next));

            return next;
        }

        /// <summary>
        /// @see Sequencer#publish(long)
        /// </summary>
        /// <param name="sequence"></param>
        public override void Publish(long sequence)
        {
            SetAvailable(sequence);
            waitStrategy.SignalAllWhenBlocking();
        }

        /// <summary>
        /// @see Sequencer#publish(long, long)
        /// </summary>
        /// <param name="lo"></param>
        /// <param name="hi"></param>
        public override void Publish(long lo, long hi)
        {
            for (long l = lo; l <= hi; l++)
            {
                SetAvailable(l);
            }
            waitStrategy.SignalAllWhenBlocking();
        }

        /// <summary>
        /// InitialiseAvailableBuffer
        /// </summary>
        private void InitialiseAvailableBuffer()
        {
            for (int i = availableBuffer.Length - 1; i != 0; i--)
            {
                SetAvailableBufferValue(i, -1);
            }

            SetAvailableBufferValue(0, -1);
        }

        /// <summary>
        /// The below methods work on the availableBuffer flag.
        /// <p>
        /// The prime reason is to avoid a shared sequence object between publisher threads.
        /// (Keeping single pointers tracking start and end would require coordination
        /// between the threads).
        /// <p>
        /// --  Firstly we have the constraint that the delta between the cursor and minimum
        /// gating sequence will never be larger than the buffer size (the code in
        /// next/tryNext in the Sequence takes care of that).
        /// -- Given that; take the sequence value and mask off the lower portion of the
        /// sequence as the index into the buffer (indexMask). (aka modulo operator)
        /// -- The upper portion of the sequence becomes the value to check for availability.
        /// ie: it tells us how many times around the ring buffer we've been (aka division)
        /// -- Because we can't wrap without the gating sequences moving forward (i.e. the
        /// minimum gating sequence is effectively our last available position in the
        /// buffer), when we have new data and successfully claimed a slot we can simply
        /// write over the top.
        /// 
        /// The below methods work on the availableBuffer flag.
        /// 我们看到Multi模式与Single模式不一样，发布时没有设置cursor的值，
        /// 而是对availableBuffer对应的槽位进行更新，
        /// 更新的值为 sequence >>> indexShift，其实就是绕环形数组的圈数
        /// </summary>
        /// <param name="sequence"></param>
        private void SetAvailable(long sequence)
        {
            SetAvailableBufferValue(CalculateIndex(sequence), CalculateAvailabilityFlag(sequence));
        }

        private void SetAvailableBufferValue(int index, int flag)
        {
            //long bufferAddress = (index * SCALE) + BASE;
            //UNSAFE.putOrderedInt(availableBuffer, bufferAddress, flag);
            //TODO:2.0,使用WriteCompilerOnlyFence
            availableBuffer.WriteFullFence(index, flag);
        }

        private int CalculateAvailabilityFlag(long sequence)
        {
            //TODO:java版本使用return (int)(sequence >>> indexShift);
            //TODO:3.3.0,使用return (int)(sequence >>= indexShift);
            //TODO:2.0,使用
            return (int)((ulong)sequence >> indexShift);
        }

        private int CalculateIndex(long sequence)
        {
            return ((int)sequence) & indexMask;
        }

        private Boolean HasAvailableCapacity(ISequence[] gatingSequences, int requiredCapacity, long cursorValue)
        {
            long wrapPoint = (cursorValue + requiredCapacity) - bufferSize;
            long cachedGatingSequence = gatingSequenceCache.Get();

            if (wrapPoint > cachedGatingSequence || cachedGatingSequence > cursorValue)
            {
                long minSequence = Util.GetMinimumSequence(gatingSequences, cursorValue);
                gatingSequenceCache.Set(minSequence);

                if (wrapPoint > minSequence)
                {
                    return false;
                }
            }

            return true;
        }

    }
}
